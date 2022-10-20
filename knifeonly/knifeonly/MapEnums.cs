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
using System.ComponentModel;

namespace System.Net.Battlefield3
{
    /// <summary>
    /// Defines the maps of Battlefield 3.
    /// </summary>
    public enum MapName : int
    {
        /// <summary>
        /// Grand Bazaar
        /// </summary>
        [Description("Grand Bazaar")]
        [RawValue("MP_001")]
        GrandBazaar = 1,

        /// <summary>
        /// Tehran Highway
        /// </summary>
        [Description("Tehran Highway")]
        [RawValue("MP_003")]
        TehranHighway = 3,

        /// <summary>
        /// Caspian Border
        /// </summary>
        [Description("Caspian Border")]
        [RawValue("MP_007")]
        CaspianBorder = 7,

        /// <summary>
        /// Seine Crossing
        /// </summary>
        [Description("Seine Crossing")]
        [RawValue("MP_011")]
        SeineCrossing = 11,

        /// <summary>
        /// Operation Firestorm
        /// </summary>
        [Description("Operation Firestorm")]
        [RawValue("MP_012")]
        OperationFirestorm = 12,

        /// <summary>
        /// Damavand Peak
        /// </summary>
        [Description("Damavand Peak")]
        [RawValue("MP_013")]
        DamavandPeak = 13,

        /// <summary>
        /// Canals
        /// </summary>
        [Description("Canals")]
        [RawValue("MP_017")]
        Canals = 17,

        /// <summary>
        /// Kharg Island
        /// </summary>
        [Description("Kharg Island")]
        [RawValue("MP_018")]
        KhargIsland = 18,

        /// <summary>
        /// Operation Métro
        /// </summary>
        [Description("Operation Métro")]
        [RawValue("MP_Subway")]
        OperationMetro = Int32.MaxValue, //not a numbered map

        /// <summary>
        /// Unknown
        /// </summary>
        [Description("Unknown")]
        [RawValue(null)]
        Unknown = 0
    }

    /// <summary>
    /// Defines the map modes of Battlefield 3.
    /// </summary>
    public enum MapMode : int
    {
        /// <summary>
        /// Unknown
        /// </summary>
        [Description("Unknown")]
        [RawValue(null)]
        Unknown,

        /// <summary>
        /// Conquest64
        /// </summary>
        [Description("Conquest64")]
        [RawValue("ConquestLarge0")]
        Conquest64,

        /// <summary>
        /// Conquest
        /// </summary>
        [Description("Conquest")]
        [RawValue("ConquestSmall0")]
        Conquest,

        /// <summary>
        /// Rush
        /// </summary>
        [Description("Rush")]
        [RawValue("RushLarge0")]
        Rush,

        /// <summary>
        /// Squad Rush
        /// </summary>
        [Description("Squad Rush")]
        [RawValue("SquadRush0")]
        SquadRush,

        /// <summary>
        /// Squad Deathmatch
        /// </summary>
        [Description("Squad Deathmatch")]
        [RawValue("SquadDeathMatch0")]
        SquadDeathmatch,

        /// <summary>
        /// Team Deathmatch
        /// </summary>
        [Description("Team Deathmatch")]
        [RawValue("TeamDeathMatch0")]
        TeamDeathmatch
    }
}
