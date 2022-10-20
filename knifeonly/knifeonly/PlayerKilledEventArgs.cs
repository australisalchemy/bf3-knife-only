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

namespace System.Net.Battlefield3
{
    /// <summary>
    /// Provides data for the <see cref="RconClient.PlayerKilled"/> event.
    /// </summary>
    [Serializable]
    public class PlayerKilledEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="Player"/> that was killed.
        /// </summary>
        /// <value>The <see cref="Player"/> that got killed.</value>
        public Player Victim { get; private set; }

        /// <summary>
        /// Gets the <see cref="Player"/> that did the killing.
        /// </summary>
        /// <value>The <see cref="Player"/> that did the killing or null, in some cases.</value>
        public Player Attacker { get; private set; }

        /// <summary>
        /// Gets the weapon that was used to killed the <see cref="Victim"/>.
        /// </summary>
        /// <value>The weapon that killed the <see cref="Victim"/>.</value>
        public string Weapon { get; private set; }

        /// <summary>
        /// Gets whether or not the kill was a headshot.
        /// </summary>
        /// <value>True if the kill was a headshot.</value>
        public bool Headshot { get; private set; }

        internal PlayerKilledEventArgs(Player victim, Player attacker, string weapon, bool headshot)
        {
            Victim = victim;
            Attacker = attacker;
            Weapon = weapon;
            Headshot = headshot;
        }
    }
}
