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

namespace System.Net.Battlefield3
{
    /// <summary>
    /// Defines what the ban will target.
    /// </summary>
    public enum BanTargetType
    {
        /// <summary>
        /// The ban will target the <see cref="Player">Player's</see> IP address, if available.
        /// </summary>
        IPAddress,
        /// <summary>
        /// The ban will target the <see cref="Player">Player's</see> name.
        /// </summary>
        Name,
        /// <summary>
        /// The ban will target the <see cref="Player">Player's</see> GUID.
        /// </summary>
        Guid
    }
}