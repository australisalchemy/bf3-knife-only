/*
BF3Rcon.NET, provides a .NET implementation of BF3's RCON interface.
Copyright (C) 2011 agentwite, Timi, Unseen, AlCapwn

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

You can contact us at http://bf3rcon.codeplex.com/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Collections.ObjectModel;
using System.Threading;
using System.Globalization;
using System.Text.RegularExpressions;
using System.ComponentModel;

//TODO: consider rewriting this class to better support inheritance
///// <summary>
///// Provides the Battlefield 3 RCON client and the other classes it uses.
///// </summary>
namespace System.Net.Battlefield3
{
    /// <summary>
    /// Provides the socket's state object for asynchronous sockets
    /// </summary>
    class ReadState
    {
        internal RconClient Client;
        internal byte[] Buffer = new byte[RconClient.BufferSize];

        internal ReadState(RconClient client)
        {
            this.Client = client;
        }
    }

    /// <summary>
    /// Provides the client's state object for parsing packets in a <see cref="ThreadPool"/> thread.
    /// </summary>
    class ParsePacketState //this is leftover from a version with more params
    {
        internal RconClient Client;
        internal List<Packet> Packets;

        internal ParsePacketState(RconClient client, List<Packet> packets)
        {
            Client = client;
            Packets = packets;
        }
    }

    class SynchronousReadState : IDisposable
    {
        internal ManualResetEventSlim ManualResetEvent = new ManualResetEventSlim(false);
        internal Packet Packet;

        internal SynchronousReadState() { }

        #region IDisposable Members
        private bool disposed = false;

        /// <summary>
        /// Disposes the RconClient object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the RconClient object.
        /// </summary>
        /// <param name="disposing">Whether or not to release managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ManualResetEvent.Dispose();
                }
            }

            disposed = true;
        }

        #endregion
    }

    /// <summary>
    /// Provides an implementation of the Battlefield 3 RCON protocol.
    /// </summary>
    public class RconClient :
        MarshalByRefObject,
        IDisposable
    {
        #region Private Fields/Properties
        internal const int BufferSize = 4096;

        //this is used for the tostrings to satisfy fxcop
        static CultureInfo c = CultureInfo.GetCultureInfo("en-US");

        internal List<byte> LeftoverData = new List<byte>(BufferSize);

        Socket UnderlyingSocket;
        //Thread packetParsingThread = new Thread(new ThreadStart());
        //Queue<Packet> packetQueue = new Queue<Packet>(10); // random number

        //this is used to determine what the server is replying to by
        //tracing it's sequence back to the sequence the <see cref="RconClient"/> used
        Dictionary<int, string> SequenceTracker = new Dictionary<int, string>();

        //this hashset tracks which sequences are sent internally
        HashSet<int> InternalSequences = new HashSet<int>();

        //stores what command will be used to get players
        internal string PlayerInfoCommand = "listPlayers";

        //this will be incremented to provide a unique sequence each time the <see cref="RconClient"/> sends data (not responds)
        int ClientSequence = 0;

        //regex for parsing pb stuff for ips
        //this is written somewhat weirdly because I have no idea what all of these fields mean
        static Regex PunkBusterPlayerListRegex = new Regex(@"^PunkBuster Server: \d+  [0-9a-f]{32}\(-\) (?<ip>\d+\.\d+\.\d+\.\d+):\d+ OK   1 \d+\.\d+ \d+ \(W\) ""(?<name>[^""]+)""", RegexOptions.Compiled);
        static Regex PunkBusterConnectionRegex = new Regex(@"^PunkBuster Server: New Connection \(slot #\d+\) (?<ip>\d+\.\d+\.\d+\.\d+):\d+ \[\?\] ""(?<name>[^""]+)"" \(seq \d+\)", RegexOptions.Compiled);
        internal Dictionary<string, string> PunkBusterIPDictionary = new Dictionary<string, string>(5); //stores IPs of players

        //used for synchronous requests
        Dictionary<int, SynchronousReadState> SynchronousReadTracker = new Dictionary<int, SynchronousReadState>(10);

        //used for pinging the server
        Timer PingTimer;
        //keeps track of last incoming packet
        DateTime LastPacketTime = DateTime.Now;
        #endregion

        #region Ctors
        /// <summary>
        /// Creates a new <see cref="RconClient"/> for remotely communicating with a Battlefield 3 server.
        /// </summary>
        public RconClient()
        {
            Players = new PlayerCollection(this);
            SuppressInternalCommands = true;
            Maps = new MapCollection(this);
            Server = new Server(this);
        }
        #endregion

        #region Properties
        // a tcp client is used because it's connect is sometimes faster than a regular socket's, and this is more flexible
        TcpClient _Socket;

        TcpClient Socket { get { return _Socket; } }

        /// <summary>
        /// Gets or sets the address that this client will <see cref="Connect()"/> to.
        /// </summary>
        /// <value>The address of the server.</value>
        /// <remarks>In order for changes of the <see cref="Address"/> and <see cref="Port"/> to take effect, you must
        /// <see cref="Disconnect()"/> and <see cref="Connect()"/>.</remarks>
        public String Address { get; set; }

        int _Port;
        /// <summary>
        /// Gets or sets the port that this client will <see cref="Connect()"/> to.
        /// </summary>
        /// <value>The port of the server.</value>
        /// <remarks>In order for changes of the <see cref="Address"/> and <see cref="Port"/> to take effect, you must
        /// <see cref="Disconnect()"/> and <see cref="Connect()"/>.</remarks>
        public int Port
        {
            get { return _Port; }
            set
            {
                if (value < 0 || value > 65535) throw new ArgumentOutOfRangeException("value", "The port must be between 0 and 65535.");
                _Port = value;
            }
        }

        /// <summary>
        /// Gets whether or not the <see cref="RconClient"/> is connected to the server.
        /// </summary>
        /// <value>True if the <see cref="RconClient"/> has connected to the server.</value>
        public bool IsConnected
        {
            get { return Socket != null && Socket.Connected; }
        }

        /// <summary>
        /// Gets whether or not the <see cref="RconClient"/> has logged into the server.
        /// </summary>
        /// <value>True if the <see cref="RconClient"/> has logged into the server.</value>
        public bool IsLoggedOn { get; private set; }

        /// <summary>
        /// Gets the <see cref="PlayerCollection"/> that contains all of the <see cref="Player">Players</see> on the server.
        /// </summary>
        /// <value>A <see cref="PlayerCollection"/> object containing the <see cref="Player">Players</see> in the server.</value>
        public PlayerCollection Players { get; private set; }

        /// <summary>
        /// Gets or sets whether or not the <see cref="Response"/> event will be raised for commands sent internally.
        /// </summary>
        /// <value>Whether or not the <see cref="Response"/> event will be raised for commands sent internally.</value>
        public bool SuppressInternalCommands { get; set; }

        /// <summary>
        /// Gets or sets whether or not the events will not check to see if the target object implements <see cref="System.ComponentModel.ISynchronizeInvoke"/>.
        /// </summary>
        /// <value>True if <see cref="RconClient"/> wont check for <see cref="System.ComponentModel.ISynchronizeInvoke"/>.</value>
        /// <remarks>
        /// By default, <see cref="RconClient"/> will check to see if the target object, such as a <see cref="System.Windows.Forms.Form"/>, 
        /// has <see cref="System.ComponentModel.ISynchronizeInvoke.InvokeRequired"/> set to true. If it does, the object's 
        /// <see cref="System.ComponentModel.ISynchronizeInvoke.Invoke"/> method will be called.<para />
        /// This property is used to disable this feature.
        /// </remarks>
        public bool DisableSynchronizeChecking { get; set; }

        int _SynchronousReadTimeout = 10;
        /// <summary>
        /// Gets or sets the timeout for synchronous reads, in seconds.
        /// </summary>
        /// <value>The timeout for synchronous reads, in seconds.</value>
        /// <remarks>
        /// Synchronous reads are reads that request and parse/return data in the same method.<para />
        /// By default, the timeout is 10 seconds.<para /><para />
        /// One example of usage is the <see cref="MapCollection"/> class. When getting the maplist, 
        /// <see cref="MapCollection"/> gets the data of three requests, and is able to return data.
        /// </remarks>
        public int SynchronousReadTimeout
        {
            get { return _SynchronousReadTimeout; }

            set
            {
                if (value < 1 || value > (Int32.MaxValue / 1000)) throw new ArgumentOutOfRangeException(
                    "value", "value must be greater than 0 and less than or equal to Int32.MaxValue / 1000.");
                _SynchronousReadTimeout = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="MapCollection"/> object of the <see cref="RconClient"/>, allowing interaction with the
        /// server's maplist.
        /// </summary>
        /// <value>The <see cref="RconClient">RconClient's</see> <see cref="MapCollection"/>.</value>
        public MapCollection Maps { get; private set; }

        int _PingTimeout = 3;
        /// <summary>
        /// Gets or sets the amount of time, in minutes, the server is allowed to not send data before the <see cref="RconClient"/>
        /// disconnects.
        /// </summary>
        /// <value>The amount of time, in minutes, before the <see cref="RconClient"/> disconnects due to lack of activity from the server.</value>
        /// <remarks>
        /// Every minute, <see cref="RconClient"/> sends a command to ensure connectivity still exists. If too long of a time
        /// has passed, as described by <see cref="PingTimeout"/>, the <see cref="RconClient"/> will disconnect. This
        /// value isn't exact; timeouts will only be checked for every minute.<para />
        /// <see cref="PingTimeout"/> must be greater than 1 because <see cref="RconClient"/> needs enough time to check
        /// for connectivity.
        /// </remarks>
        public int PingTimeout
        {
            get
            {
                return _PingTimeout;
            }

            set
            {
                if (value < 2) throw new ArgumentOutOfRangeException("value", "value must be greater than 1.");

                _PingTimeout = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="System.Net.Battlefield3.Server"/>, which contains various server properties.
        /// </summary>
        /// <value>The <see cref="System.Net.Battlefield3.Server"/> containin server properties.</value>
        public Server Server { get; private set; }
        #endregion

        #region Important Event Notes & Event Invocation Methods
        //note to other devs about events...
        //these events are written to work well with multiple appdomains
        //if an appdomain is unloaded, the event will automatically be removed from the list
        //these events are also designed to work well with forms

        //these methods invoke the events, providing the special features mentioned above
        static void InvokeEvent<TArgs>(EventHandler<TArgs> eventHandler, object sender, TArgs e) where TArgs : EventArgs
        {
            if (eventHandler == null) return;

            foreach (EventHandler<TArgs> ev in eventHandler.GetInvocationList())
            {
                ISynchronizeInvoke target = ev.Target as ISynchronizeInvoke;

                try
                {
                    if (target != null && target.InvokeRequired)
                        target.Invoke(ev, new object[] { sender, e });
                    else
                        ev(sender, e);
                }
                catch (AppDomainUnloadedException)
                {
                    eventHandler -= ev;
                }
            }
        }
        static void InvokeEvent(EventHandler eventHandler, object sender)
        {
            if (eventHandler == null) return;

            foreach (EventHandler ev in eventHandler.GetInvocationList())
            {
                ISynchronizeInvoke target = ev.Target as ISynchronizeInvoke;

                try
                {
                    if (target != null && target.InvokeRequired)
                        target.Invoke(ev, new object[] { sender, EventArgs.Empty });
                    else
                        ev(sender, EventArgs.Empty);
                }
                catch (AppDomainUnloadedException)
                {
                    eventHandler -= ev;
                }
            }
        }

        #endregion

        #region Library Events
        /// <summary>
        /// Occurs when the <see cref="RconClient"/> receives a <see cref="Packet"/>
        /// </summary>
        public event EventHandler<RawReadEventArgs> RawRead;
        /// <summary>
        /// Raises the <see cref="RawRead"/> event of the <see cref="RconClient"/>.
        /// </summary>
        /// <param name="e">A <see cref="RawReadEventArgs"/> that contains the event data.</param>
        protected virtual void OnRawRead(RawReadEventArgs e)
        {
            InvokeEvent<RawReadEventArgs>(RawRead, this, e);
        }

        /// <summary>
        /// Occurs when the server responds to the <see cref="RconClient"/>.
        /// </summary>
        public event EventHandler<ResponseEventArgs> Response;
        /// <summary>
        /// Raises the <see cref="Response"/> event of the <see cref="RconClient"/>.
        /// </summary>
        /// <param name="e">A <see cref="ResponseEventArgs"/> that contains the event data.</param>
        protected virtual void OnResponse(ResponseEventArgs e)
        {
            InvokeEvent<ResponseEventArgs>(Response, this, e);
        }

        /// <summary>
        /// Occurs when the <see cref="RconClient"/> has connected to the server.
        /// </summary>
        public event EventHandler Connected;
        /// <summary>
        /// Raises the <see cref="Connected"/> event of the <see cref="RconClient"/>.
        /// </summary>
        protected virtual void OnConnected()
        {
            InvokeEvent(Connected, this);
        }

        /// <summary>
        /// Occurs when the <see cref="RconClient"/> has encountered an error when trying to connect to the server.
        /// </summary>
        /// <remarks>
        /// This will only be raised when a SocketException occurs in the asynchronous connect method.
        /// </remarks>
        public event EventHandler<ConnectErrorEventArgs> ConnectError;
        /// <summary>
        /// Raises the <see cref="ConnectError"/> event of the <see cref="RconClient"/>.
        /// </summary>
        /// <param name="e">A <see cref="ConnectErrorEventArgs"/> that contains the event data.</param>
        protected virtual void OnConnectError(ConnectErrorEventArgs e)
        {
            InvokeEvent<ConnectErrorEventArgs>(ConnectError, this, e);
        }

        /// <summary>
        /// Occurs when the <see cref="RconClient"/> has disconnected from the server.
        /// </summary>
        public event EventHandler<DisconnectedEventArgs> Disconnected;
        /// <summary>
        /// Raises the <see cref="Disconnected"/> event of the <see cref="RconClient"/>.
        /// </summary>
        /// <param name="e">A <see cref="DisconnectedEventArgs"/> that contains the event data.</param>
        protected virtual void OnDisconnected(DisconnectedEventArgs e)
        {
            InvokeEvent<DisconnectedEventArgs>(Disconnected, this, e);
        }

        /// <summary>
        /// Occurs when the <see cref="RconClient"/> logs into the server.
        /// </summary>
        /// <remarks>The <see cref="RconClient"/> will have limited communication ability while not logged in.</remarks>
        public event EventHandler LoggedOn;
        /// <summary>
        /// Raises the <see cref="LoggedOn"/> event of the <see cref="RconClient"/>.
        /// </summary>
        protected virtual void OnLoggedOn()
        {
            InvokeEvent(LoggedOn, this);
        }
        #endregion

        #region RCON-originating Events
        /// <summary>
        /// Occurs when a <see cref="Player"/> leaves the server.
        /// </summary>
        public event EventHandler<PlayerEventArgs> PlayerLeft;
        /// <summary>
        /// Occurs when a <see cref="Player"/> leaves the server.
        /// </summary>
        /// <summary>
        /// Raises the <see cref="PlayerLeft"/> event of the <see cref="RconClient"/>.
        /// </summary>
        /// <param name="e">A <see cref="PlayerEventArgs"/> that contains the event data.</param>
        protected virtual void OnPlayerLeft(PlayerEventArgs e)
        {
            InvokeEvent<PlayerEventArgs>(PlayerLeft, this, e);
        }

        /// <summary>
        /// Occurs when a <see cref="Player"/> joins the server.
        /// </summary>
        /// <remarks>
        /// This event is raised the first time the <see cref="Player"/> shows up in <see cref="Players"/>.<para />
        /// Since this will be raised when a <see cref="Player"/> first ends up in <see cref="Players"/>, this event
        /// will be raised after the <see cref="RconClient"/> first connects. This behavior will be kept, for now, because
        /// it has some uses.
        /// </remarks>
        public event EventHandler<PlayerEventArgs> PlayerJoined;
        /// <summary>
        /// Raises the <see cref="PlayerJoined"/> event of the <see cref="RconClient"/>.
        /// </summary>
        /// <param name="e">A <see cref="PlayerEventArgs"/> that contains the event data.</param>
        protected virtual void OnPlayerJoined(PlayerEventArgs e)
        {
            InvokeEvent<PlayerEventArgs>(PlayerJoined, this, e);
        }

        /// <summary>
        /// Occurs when a <see cref="Player"/> is joining the server.
        /// </summary>
        /// <remarks>
        /// This event is raised before the player is in <see cref="Players"/>.
        /// </remarks>
        public event EventHandler<PlayerJoiningEventArgs> PlayerJoining;
        /// <summary>
        /// Raises the <see cref="PlayerJoining"/> event of the <see cref="RconClient"/>.
        /// </summary>
        /// <param name="e">A <see cref="PlayerJoiningEventArgs"/> that contains the event data.</param>
        protected virtual void OnPlayerJoining(PlayerJoiningEventArgs e)
        {
            InvokeEvent<PlayerJoiningEventArgs>(PlayerJoining, this, e);
        }

        /// <summary>
        /// Occurs when a <see cref="Player"/> is authenticated.
        /// </summary>
        public event EventHandler<PlayerAuthenticatedEventArgs> PlayerAuthenticated;
        /// <summary>
        /// Raises the <see cref="PlayerAuthenticated"/> event of the <see cref="RconClient"/>.
        /// </summary>
        /// <param name="e">A <see cref="PlayerAuthenticatedEventArgs"/> that contains the event data.</param>
        protected virtual void OnPlayerAuthenticated(PlayerAuthenticatedEventArgs e)
        {
            InvokeEvent<PlayerAuthenticatedEventArgs>(PlayerAuthenticated, this, e);
        }

        /// <summary>
        /// Occurs when a <see cref="Player"/> is killed.
        /// </summary>
        public event EventHandler<PlayerKilledEventArgs> PlayerKilled;
        /// <summary>
        /// Raises the <see cref="PlayerKilled"/> event of the <see cref="RconClient"/>.
        /// </summary>
        /// <param name="e">A <see cref="PlayerKilledEventArgs"/> that contains the event data.</param>
        protected virtual void OnPlayerKilled(PlayerKilledEventArgs e)
        {
            InvokeEvent<PlayerKilledEventArgs>(PlayerKilled, this, e);
        }

        /// <summary>
        /// Occurs when a <see cref="Player"/> spawns.
        /// </summary>
        public event EventHandler<PlayerEventArgs> PlayerSpawned;
        /// <summary>
        /// Raises the <see cref="PlayerSpawned"/> event of the <see cref="RconClient"/>.
        /// </summary>
        /// <param name="e">A <see cref="PlayerEventArgs"/> that contains the event data.</param>
        protected virtual void OnPlayerSpawned(PlayerEventArgs e)
        {
            InvokeEvent<PlayerEventArgs>(PlayerSpawned, this, e);
        }

        /// <summary>
        /// Occurs when a <see cref="Player"/> is moved to a new team, squad, or both.
        /// </summary>
        public event EventHandler<PlayerEventArgs> PlayerMoved;
        /// <summary>
        /// Raises the <see cref="PlayerMoved"/> event of the <see cref="RconClient"/>.
        /// </summary>
        /// <param name="e">A <see cref="PlayerEventArgs"/> that contains the event data.</param>
        protected virtual void OnPlayerMoved(PlayerEventArgs e)
        {
            InvokeEvent<PlayerEventArgs>(PlayerMoved, this, e);
        }

        /// <summary>
        /// Occurs when the round has ended.
        /// </summary>
        public event EventHandler RoundOver;
        /// <summary>
        /// Raises the <see cref="RoundOver"/> event of the <see cref="RconClient"/>.
        /// </summary>
        protected virtual void OnRoundOver()
        {
            InvokeEvent(RoundOver, this);
        }

        /// <summary>
        /// Occurs when a level has been loaded.
        /// </summary>
        public event EventHandler<LevelLoadedEventArgs> LevelLoaded;
        /// <summary>
        /// Raises the <see cref="LevelLoaded"/> event of the <see cref="RconClient"/>.
        /// </summary>
        /// <param name="e">A <see cref="LevelLoadedEventArgs"/> that contains the event data.</param>
        protected virtual void OnLevelLoaded(LevelLoadedEventArgs e)
        {
            InvokeEvent<LevelLoadedEventArgs>(LevelLoaded, this, e);
        }

        /// <summary>
        /// Occurs when a <see cref="Player"/> or the server sends a message.
        /// </summary>
        public event EventHandler<PlayerChatEventArgs> PlayerChat;
        /// <summary>
        /// Raises the <see cref="PlayerChat"/> event of the <see cref="RconClient"/>.
        /// </summary>
        /// <param name="e">A <see cref="PlayerChatEventArgs"/> that contains the event data.</param>
        protected virtual void OnPlayerChat(PlayerChatEventArgs e)
        {
            InvokeEvent<PlayerChatEventArgs>(PlayerChat, this, e);
        }

        //removed as of R3
        //List<EventHandler<PlayerKickedEventArgs>> _PlayerKicked = new List<EventHandler<PlayerKickedEventArgs>>();
        ///// <summary>
        ///// Occurs when a <see cref="Player"/> is kicked from the server.
        ///// </summary>
        //public event EventHandler<PlayerKickedEventArgs> PlayerKicked
        //{
        //    add { _PlayerKicked.Add(value); }

        //    remove { _PlayerKicked.Remove(value); }
        //}
        ///// <summary>
        ///// Raises the <see cref="PlayerKicked"/> event of the <see cref="RconClient"/>.
        ///// </summary>
        ///// <param name="e">A <see cref="PlayerKickedEventArgs"/> that contains the event data.</param>
        //protected virtual void OnPlayerKicked(PlayerKickedEventArgs e)
        //{
        //    foreach (var ev in new List<EventHandler<PlayerKickedEventArgs>>(_PlayerKicked))
        //    {
        //        try
        //        {
        //            ev.Invoke(this, e);
        //        }
        //        catch (AppDomainUnloadedException)
        //        {
        //            _PlayerKicked.Remove(ev);
        //        }
        //    }
        //}

        /// <summary>
        /// Occurs when PunkBuster sends a message.
        /// </summary>
        public event EventHandler<PunkBusterMessageEventArgs> PunkBusterMessage;
        /// <summary>
        /// Raises the <see cref="PunkBusterMessage"/> event of the <see cref="RconClient"/>.
        /// </summary>
        /// <param name="e">A <see cref="PunkBusterMessageEventArgs"/> that contains the event data.</param>
        protected virtual void OnPunkBusterMessage(PunkBusterMessageEventArgs e)
        {
            InvokeEvent<PunkBusterMessageEventArgs>(PunkBusterMessage, this, e);
        }
        #endregion

        #region Async Socket Methods
        static void Connect(RconClient client)
        {
            client.Socket.BeginConnect(client.Address, client.Port, ConnectCallback, client);
        }

        static void ConnectCallback(IAsyncResult ar)
        {
            RconClient client = (RconClient)ar.AsyncState;

            try
            {
                client.Socket.EndConnect(ar);
            }
            catch (SocketException ex)
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(RaiseConnectErrorThread), new object[] { client, ex });
            }

            client.UnderlyingSocket = client.Socket.Client; //set the stream for more simple use by these methods

            //setup the ping timer
            client.PingTimer = new Timer(
                new TimerCallback(state => //since this method will be basic, use lambda because no debugging should be needed
                {
                    RconClient client2 = state as RconClient;

                    if ((DateTime.Now - client2.LastPacketTime).TotalMinutes > client2.PingTimeout)
                        client2.Disconnect("The server failed to send data within RconClient.PingTimeout", SocketError.TimedOut);
                    else
                        client2.InternalSendAsynchronousRequest("ping");
                }),
                client, 60000, 60000);

            //when the Connected event is raised, using LogOn (and future methods that use MRESes) blocks
            //subsequent Receives, and there's no easy way to determine if the MRESs will block the UI thread or
            //socket thread. therefore, this event will be raised in a ThreadPool thread.
            ThreadPool.QueueUserWorkItem(new WaitCallback(RaiseConnectedThread), client);

            Receive(client);
        }

        static void RaiseConnectedThread(object state)
        {
            (state as RconClient).OnConnected();
        }

        //state is an object array where 0 is rconclient and 1 is socketexception
        static void RaiseConnectErrorThread(object state)
        {
            object[] objs = state as object[];
            RconClient client = objs[0] as RconClient;
            SocketException ex = objs[1] as SocketException;

            client.OnConnectError(new ConnectErrorEventArgs(ex.Message, ex.SocketErrorCode));
        }

        static void Receive(RconClient client)
        {
            ReadState state = new ReadState(client);
            try
            {
                client.UnderlyingSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, 0, ReceiveCallback, state);
            }
            catch (InvalidOperationException) //socket is disposed (thru disconnect)
            {
                return;
            }
        }

        static void ReceiveCallback(IAsyncResult ar)
        {
            ReadState state = (ReadState)ar.AsyncState;
            RconClient client = state.Client;

            if (client.disposed || !client.IsConnected) return;

            int bytesRead = 0;
            try
            {
                bytesRead = client.UnderlyingSocket.EndReceive(ar);
            }
            catch (SocketException ex)
            {
                client.Disconnect(ex.Message, ex.SocketErrorCode); //this is probably the one event that can be done here
                return;
            }

            if (bytesRead > 0) //but I'll make use of it anyway
            {
                for (int i = 0; i < bytesRead; i++)
                    client.LeftoverData.Add(state.Buffer[i]);

                //gets all packets here to avoid problems with modifying LeftoverData
                List<Packet> packets = new List<Packet>(5); //random capacity
                while (Packet.ContainsCompletePacket(client.LeftoverData))
                {
                    packets.Add(new Packet(new List<byte>(client.LeftoverData), client.LeftoverData));
                }

                ThreadPool.QueueUserWorkItem(new WaitCallback(HandlePacketReceive),
                    new ParsePacketState(client, packets));
            }
            else
            {
                client.Disconnect();
            }

            Receive(client);
        }

        static void Send(RconClient client, Packet data)
        {
            client.UnderlyingSocket.BeginSend(data.ToByteArray(), 0, data.NumberOfBytes, 0, SendCallback, client);
        }

        static void SendCallback(IAsyncResult ar)
        {
            ((RconClient)ar.AsyncState).UnderlyingSocket.EndSend(ar);
        }
        #endregion

        #region Connect, Send
        /// <summary>
        /// Connects the <see cref="RconClient"/> to the server.
        /// </summary>
        public void Connect()
        {
            _Socket = new TcpClient();
            Connect(this);
        }

        /// <summary>
        /// Disconnects the <see cref="RconClient"/> from the server.
        /// </summary>
        public void Disconnect()
        {
            Socket.Close();
            _Socket = null;

            Reset();

            OnDisconnected(new DisconnectedEventArgs("", SocketError.Success)); //no exception
        }

        //used for when a disconnect is done for socketexceptions
        internal void Disconnect(string message, SocketError error)
        {
            Socket.Close();
            _Socket = new TcpClient(); //prepare a new tcpclient for use

            Reset();

            OnDisconnected(new DisconnectedEventArgs(message, error));
        }

        void Reset()
        {
            //reset other properties specific to the last session
            LeftoverData = null;
            SequenceTracker = new Dictionary<int, string>();
            InternalSequences = new HashSet<int>();
            PingTimer.Dispose(); PingTimer = null;
            PlayerInfoCommand = "listPlayers";
            PunkBusterIPDictionary.Clear();
            SynchronousReadTracker.Clear();
        }

        /// <summary>
        /// Sends a <see cref="Packet"/> to the server.
        /// </summary>
        /// <param name="packet">The <see cref="Packet"/> that is being sent.</param>
        protected void SendPacket(Packet packet)
        {
            if (!IsConnected) return; //consider throwing exception
            if (packet.Words.Count == 0) return;

            Send(this, packet);
        }

        /// <summary>
        /// Sends a <see cref="Packet"/> to the server.
        /// </summary>
        /// <param name="isFromServer">Whether or not the <see cref="Packet"/> originates from the server.</param>
        /// <param name="isResponse">Whether or not the <see cref="Packet"/> is a response to another one.</param>
        /// <param name="sequence">The sequence number of the <see cref="Packet"/>.</param>
        /// <param name="words">The <see cref="Packet">Packet's</see> words.</param>
        protected void SendPacket(bool isFromServer, bool isResponse, int sequence, params string[] words)
        {
            if (words.Length == 0) return;

            SendPacket(new Packet(isFromServer, isResponse, sequence, words));
        }

        /// <summary>
        /// Sends a response to a <see cref="Packet"/> originally sent by the server.
        /// </summary>
        /// <param name="sequence">The sequence number of the original <see cref="Packet"/>.</param>
        /// <param name="words">The words of the <see cref="Packet"/> being sent.</param>
        protected void SendResponse(int sequence, params string[] words)
        {
            if (words.Length == 0) return;

            //not sure why isfromserver is true, but that's the way it is in python
            SendPacket(true, true, sequence, words);
        }

        /// <summary>
        /// Sends a request for information to the server.
        /// </summary>
        /// <param name="words">The words of the <see cref="Packet"/> being sent.</param>
        /// <returns>The sequence number used in the request.</returns>
        public int SendAsynchronousRequest(params string[] words)
        {
            return SendAsynchronousRequest(false, words);
        }

        /// <summary>
        /// Sends a request for information that returns the response <see cref="Packet"/>.
        /// </summary>
        /// <param name="words">The words of the <see cref="Packet"/> being sent.</param>
        /// <returns>The <see cref="Packet"/> set in response to the request or null (Nothing in Visual Basic) if a timeout occurs.</returns>
        /// <remarks>
        /// A timeout will occur when <see cref="SynchronousReadTimeout"/> elapses.
        /// </remarks>
        public Packet SendRequest(params string[] words)
        {
            //state
            SynchronousReadState state = new SynchronousReadState();

            //set in dict
            SynchronousReadTracker[SendAsynchronousRequest(words)] = state;

            //wait for packet or timeout
            if (!state.ManualResetEvent.Wait(SynchronousReadTimeout * 1000)) return null;

            return state.Packet;
        }

        //used for tracking internal sequences
        internal int InternalSendAsynchronousRequest(params string[] words)
        {
            return SendAsynchronousRequest(true, words);
        }

        internal Packet InternalSendRequest(params string[] words)
        {
            //state
            SynchronousReadState state = new SynchronousReadState();

            //set in dict
            SynchronousReadTracker[SendAsynchronousRequest(true, words)] = state;

            //wait for packet or timeout
            if (!state.ManualResetEvent.Wait(SynchronousReadTimeout * 1000)) return null;

            return state.Packet;
        }

        int SendAsynchronousRequest(bool isInternal, params string[] words)
        {
            if (words.Length == 0) throw new ArgumentException("words must have at least 1 word.", "words");
            int sequence = RegisterSequence(words[0], isInternal);

            SendPacket(false, false, sequence, words);
            return sequence;
        }

        int RegisterSequence(string word, bool isInternal)
        {
            int oldSequence;
            lock ("rsLock")
            {
                oldSequence = ClientSequence;

                if (isInternal && SuppressInternalCommands && !InternalSequences.Contains(ClientSequence)) InternalSequences.Add(ClientSequence);

                SequenceTracker[ClientSequence] = word;
                ClientSequence = (ClientSequence + 1) & 0x3fffffff; //increment the sequence
            }

            return oldSequence;
        }

        // to make thing simpler, two MRESs will be used instead of one
        // these are for LogOn
        ManualResetEventSlim getSalt = new ManualResetEventSlim(false);
        ManualResetEventSlim confirmLoggedOn = new ManualResetEventSlim(false);
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        string tempPass = "";
        string salt = "";
        bool attemptingThreadedLogin = false;

        /// <summary>
        /// Logs the <see cref="RconClient"/> onto the server without blocking the thread. 
        /// </summary>
        /// <param name="password">The password for the server's RCON interface.</param>
        /// <remarks>If you'd like to block the thread while logging in, use <see cref="LogOn(string, bool)"/>
        /// <seealso cref="LogOn(string, bool)"/></remarks>
        public void LogOn(string password)
        {
            LogOn(password, true);
        }

        /// <summary>
        /// Logs the <see cref="RconClient"/> onto the server.
        /// </summary>
        /// <param name="password">The password for the server's RCON interface.</param>
        /// <param name="threaded">If true, the method will not block the thread, but you will not know if the login fails.</param>
        /// <returns>True if login was successful or false on a failed login or when <paramref name="threaded"/> is True.</returns>
        /// <remarks>
        /// In order to ensure this method doesn't block the thread when <paramref name="threaded"/> is false,
        /// each point of communication (there are two of them), will wait a period of time before returning false,
        /// as defined by <see cref="SynchronousReadTimeout"/>. In the final point of communication, it is possible that
        /// the login attempt will succeed after timeout, so, while this method would return false, the <see cref="LoggedOn"/>
        /// event would still be raised.
        /// </remarks>
        public bool LogOn(string password, bool threaded)
        {
            if (!IsLoggedOn)
            {
                if (IsConnected && password != null && password.HasValue())
                {
                    if (threaded)
                    {
                        attemptingThreadedLogin = true;
                        tempPass = password;
                        InternalSendAsynchronousRequest("login.hashed");
                        return false;
                    }

                    //reset the MRESs
                    getSalt.Reset();
                    confirmLoggedOn.Reset();

                    InternalSendAsynchronousRequest("login.hashed");

                    if (!getSalt.Wait(SynchronousReadTimeout * 1000)) return false; //timeout

                    //check for salt. if it gets this far, there should be no point in checking for it, tho
                    if (!salt.HasValue()) return false;

                    InternalSendAsynchronousRequest("login.hashed", GeneratePasswordHash(password, salt));

                    if (!confirmLoggedOn.Wait(SynchronousReadTimeout * 1000)) return false;

                    return IsLoggedOn;

                }
                return false;
            }
            return true;
        }

        static string GeneratePasswordHash(string pass, string salt = "")
        {
            string result;
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] passBytes = Encoding.Default.GetBytes(pass);

                int saltByteCount = salt.Length / 2; //hex string
                byte[] allBytes = new byte[passBytes.Length + saltByteCount];

                for (int i = 0; i < salt.Length; i += 2)
                    allBytes[i / 2] = Convert.ToByte(salt.Substring(i, 2), 16);//converts the hex value into a byte

                for (int i = 0; i < passBytes.Length; i++)
                    allBytes[i + saltByteCount] = passBytes[i];

                result = md5.ComputeHash(allBytes).ToHexString();
            }

            return result.ToUpper(c);
        }
        #endregion

        #region Packet Handling
        static void HandlePacketReceive(object state)
        {
            ParsePacketState pState = (state as ParsePacketState);
            RconClient client = pState.Client;
            List<Packet> packets = pState.Packets;

            //first, ensure the timeout tracking is updated
            client.LastPacketTime = DateTime.Now;

            foreach (var packet in packets)
            {
                Header header = packet.Header;
                ReadOnlyCollection<string> words = packet.Words;

                client.OnRawRead(new RawReadEventArgs(packet));

                #region Response Handling
                if (header.IsResponse)
                {
                    //although the logic is inefficient, it ensures the O(n) is used as little as possible
                    if (!client.SuppressInternalCommands)
                        client.OnResponse(
                            new ResponseEventArgs(header.Sequence,
                                client.SequenceTracker.ContainsKey(header.Sequence) ? client.SequenceTracker[header.Sequence] : "",
                                words));
                    else if (!client.InternalSequences.Contains(header.Sequence))
                        client.OnResponse(
                            new ResponseEventArgs(header.Sequence,
                                client.SequenceTracker.ContainsKey(header.Sequence) ? client.SequenceTracker[header.Sequence] : "",
                                words));
                    else client.InternalSequences.Remove(header.Sequence);

                    //handle synchronous reads
                    SynchronousReadState syncState;
                    if (client.SynchronousReadTracker.TryGetValue(header.Sequence, out syncState))
                    {
                        syncState.Packet = packet;
                        syncState.ManualResetEvent.Set();
                        client.SynchronousReadTracker.Remove(header.Sequence);
                        syncState.Dispose();
                    }


                    //handle response to commands
                    if (client.SequenceTracker.ContainsKey(header.Sequence) && words.Count > 0)
                    {
                        string command = client.SequenceTracker[header.Sequence];
                        switch (command)
                        {
                            case "login.hashed":
                                if (words.Count == 2) //means the response has the salt
                                    if (client.attemptingThreadedLogin)
                                    {
                                        client.InternalSendAsynchronousRequest("login.hashed", GeneratePasswordHash(client.tempPass, words[1]));
                                        client.tempPass = "";
                                        client.salt = "";
                                        client.attemptingThreadedLogin = false;
                                    }
                                    else
                                    {
                                        client.salt = words[1];
                                        client.getSalt.Set();
                                    }
                                else
                                {
                                    client.IsLoggedOn = words[0] == "OK";

                                    //send command to receive events, raise loggedon event, and change playerrefreshcommand
                                    if (client.IsLoggedOn)
                                    {

                                        //now get IPs
                                        client.SendAsynchronousRequest("punkBuster.pb_sv_command", "pb_sv_plist");

                                        client.OnLoggedOn();
                                        client.PlayerInfoCommand = "admin.listPlayers";
                                        client.InternalSendAsynchronousRequest("admin.eventsEnabled", "true");
                                    }

                                    client.confirmLoggedOn.Set();
                                }
                                break;

                            case "logout":
                                client.PlayerInfoCommand = "listPlayers";
                                break;
                        } //switch

                        //keep the dictionary from inflating
                        //since the server should respond to all requests, even bad ones, this dict shouldn't grow large
                        client.SequenceTracker.Remove(header.Sequence);
                    }
                }
                #endregion

                #region Event Handling
                else
                {
                    //events, like player.join, will be caught here

                    //get a player here. for player events, the the 1st word (zero-based index) will be a player name.
                    //for non player events, the playercollection indexer will a player that wont be used
                    //check to see that words[1] exists, tho
                    Player p = null;
                    if (words.Count > 1) client.Players.TryGetPlayer(words[1], out p);

                    switch (words[0])
                    {
                        case "player.onLeave":
                            Player suppliedPlayer = new PlayerCollection(client, packet, 2)[0];

                            //raise the event before removing the player
                            client.OnPlayerLeft(new PlayerEventArgs(suppliedPlayer));

                            //remove IP
                            client.PunkBusterIPDictionary.Remove(words[1]);
                            break;

                        case "player.onJoin":

                            //add guid to tempguids
                            //client.tempGuids[words[1]] = words[2];

                            //send a listplayers command and have the event trigger there
                            //this is done here because of the possibility the player update timer
                            //will add the player before player.onjoin does, making this method more efficient
                            //client.SendAsynchronousRequest(client.playerRefreshCommand, "player", words[1]); doesnt work
                            client.OnPlayerJoining(new PlayerJoiningEventArgs(words[1], words[2]));
                            break;

                        case "player.onAuthenticated":
                            //update the player
                            //p.Guid = words[2];

                            client.OnPlayerAuthenticated(new PlayerAuthenticatedEventArgs(p));
                            break;

                        case "player.onKill":
                            Player victim = client.Players[words[2]];

                            client.OnPlayerKilled(new PlayerKilledEventArgs(victim, p, words[3], Convert.ToBoolean(words[4], c)));
                            break;

                        case "player.onSpawn":
                            client.OnPlayerSpawned(new PlayerEventArgs(p));
                            break;

                        case "player.onSquadChange":
                        case "player.onTeamChange":
                            if (p as object == null) break;

                            client.OnPlayerMoved(new PlayerEventArgs(p));
                            break;

                        case "server.onRoundOver":
                            client.OnRoundOver();
                            break;

                        case "server.onRoundOverPlayers":
                            //ParsePlayerInfo(client, words, 1, -1);
                            break;

                        case "server.onLevelLoaded":
                            client.OnLevelLoaded(new LevelLoadedEventArgs(words[1], words[2], words[3].ToInt32(), words[4].ToInt32()));
                            break;

                        case "player.onChat":
                            client.OnPlayerChat(
                                new PlayerChatEventArgs(p, words[1], words[2])
                                );
                            break;

                        //case "player.onKicked":
                        //    //TODO: check to see if this or onleave happens first to improve logic.
                        //    if (p != null)
                        //        client.OnPlayerKicked(new PlayerKickedEventArgs(p, words.Count > 1 ? words[2] : ""));
                        //    break;

                        case "punkBuster.onMessage":
                            client.OnPunkBusterMessage(new PunkBusterMessageEventArgs(words[1]));

                            string name;
                            string ip;
                            Match pbPlayerList = PunkBusterPlayerListRegex.Match(words[1]);
                            if (pbPlayerList.Success) //player list matches only occur the first time the client logs in
                            {
                                name = pbPlayerList.Groups["name"].Value;
                                ip = pbPlayerList.Groups["ip"].Value;

                                client.PunkBusterIPDictionary[name] = ip;
                            }
                            else
                            {
                                Match pbConnect = PunkBusterConnectionRegex.Match(words[1]);
                                if (pbConnect.Success)
                                {
                                    name = pbConnect.Groups["name"].Value;
                                    ip = pbConnect.Groups["ip"].Value;

                                    client.PunkBusterIPDictionary[name] = ip;
                                }

                            }
                            break;
                    }

                    //send the server an OK response. if the docs show a dif response is needed, will have to add logic
                    client.SendResponse(packet.Header.Sequence, "OK");
                }
                #endregion

            }

        }

        static PlayerSubset ParseSubset(RconClient client, ReadOnlyCollection<string> words, int offset)
        {
            switch (words[offset])
            {
                case "all":
                    return new AllPlayerSubset();
                case "team":
                    return new TeamPlayerSubset(words[offset + 1].ToInt32());
                case "squad":
                    return new SquadPlayerSubset(words[offset + 1].ToInt32(), words[offset + 2].ToInt32());
                case "player":
                    return new NamePlayerSubset(client.Players[words[offset + 1]]);
                default:
                    return null;
            }
        }
        #endregion

        #region IDisposable Members
        private bool disposed = false;

        /// <summary>
        /// Disposes the RconClient object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the RconClient object.
        /// </summary>
        /// <param name="disposing">Whether or not to release managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _Socket.Close();
                    getSalt.Dispose();
                    confirmLoggedOn.Dispose();
                    PingTimer.Dispose();
                }
            }

            disposed = true;
        }

        #endregion
    }
}
