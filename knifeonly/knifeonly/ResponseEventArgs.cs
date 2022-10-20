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
using System.Collections.ObjectModel;

namespace System.Net.Battlefield3
{
    /// <summary>
    /// Provides data for the <see cref="RconClient.Response"/> event.
    /// </summary>
    [Serializable]
    public class ResponseEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the sequence number of the server's response.
        /// </summary>
        /// <value>An integer containing the sequence number of the packet.</value>
        public int Sequence { get; private set; }

        /// <summary>
        /// Gets the command (first word) the <see cref="RconClient"/> originally sent.
        /// </summary>
        /// <value>A string containing the command/first word the <see cref="RconClient"/> originally sent to the server.</value>
        public string ClientCommand { get; private set; }

        /// <summary>
        /// Gets the words the server sent.
        /// </summary>
        /// <value>A read-only string collection containing all of the words the server sent.</value>
        public ReadOnlyCollection<string> Words { get; private set; }
        

        internal ResponseEventArgs(int sequence, string clientCommand, ReadOnlyCollection<string> words)
        {
            Sequence = sequence;
            ClientCommand = clientCommand;
            Words = words;
        }
    }
}
