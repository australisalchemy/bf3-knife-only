/*
Bf3Rcon.NET, provides a .NET implementation of BF3's RCON interface.
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

namespace System.Net.Battlefield3
{
    /// <summary>
    /// Provides data for the <see cref="RconClient.PlayerChat"/> event.
    /// </summary>
    /// <remarks>
    /// If the message wasn't sent by a <see cref="Battlefield3.Player"/>, <see cref="PlayerEventArgs.Player"/> 
    /// will be null (Nothing in Visual Basic).
    /// </remarks>
    [Serializable]
    public class PlayerChatEventArgs : PlayerEventArgs
    {
        /// <summary>
        /// Gets the source of the message, as represented by a string.
        /// </summary>
        /// <value>
        /// If the message came from a <see cref="Player"/>, this will return its <see cref="Player.Name"/>. 
        /// Otherwise, this will return some other source, such as Server.
        /// </value>
        public string Source { get; private set; }

        /// <summary>
        /// Gets the message that was sent.
        /// </summary>
        /// <value>The message that was sent.</value>
        public string Message { get; private set; }

        ///// <summary>
        ///// Gets the target of the <see cref="Message"/>.
        ///// </summary>
        ///// <value>The target of the <see cref="Message"/>.</value>
        ///// <remarks>
        ///// Since there is no way to send the inherited <see cref="PlayerSubset"/>, you'll have to determine this by
        ///// trying a series of conversions or looking at the <see cref="PlayerSubset.Name"/>.<para />
        ///// The <see cref="PlayerSubset.Values"/> only contains limited information, so it is recommended that you
        ///// use a specific <see cref="PlayerSubset"/>. The four provided <see cref="PlayerSubset">PlayerSubsets</see> are <see cref="AllPlayerSubset"/>, 
        ///// <see cref="TeamPlayerSubset"/>, <see cref="SquadPlayerSubset"/>, <see cref="NamePlayerSubset"/>.
        ///// 
        ///// <seealso cref="AllPlayerSubset"/><seealso cref="TeamPlayerSubset"/><seealso cref="SquadPlayerSubset"/><seealso cref="NamePlayerSubset"/>
        ///// </remarks>
        //public PlayerSubset Target { get; private set; }

        internal PlayerChatEventArgs(Player player, string source, string message) : base(player)
        {
            Source = source;
            Message = message;
        }
    }
}
