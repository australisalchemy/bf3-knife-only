﻿/*
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

namespace System.Net.Battlefield3
{
    /// <summary>
    /// Provides data for the <see cref="RconClient.PlayerKicked"/> event.
    /// </summary>
    [Serializable]
    public class PlayerKickedEventArgs : PlayerEventArgs
    {
        /// <summary>
        /// Gets the reason the <see cref="Player"/> was kicked.
        /// </summary>
        /// <value>The reason the <see cref="Player"/> was kicked.</value>
        public string Reason { get; private set; }

        internal PlayerKickedEventArgs(Player player, string reason) : base(player)
        {
            Reason = reason;
        }
    }
}
