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
using System.Globalization;

namespace System.Net.Battlefield3
{
    /// <summary>
    /// Contains extension methods.
    /// </summary>
    public static class Extensions
    {
        static CultureInfo c = CultureInfo.GetCultureInfo("en-US");

        /// <summary>
        /// Determines if a string has a value.
        /// </summary>
        /// <param name="str">The string that is being tested.</param>
        /// <returns>True if the string is not null or empty.</returns>
        internal static bool HasValue(this string str)
        {
            return !string.IsNullOrEmpty(str);
        }

        /// <summary>
        /// Converts a byte array into a hexadecimal string.
        /// </summary>
        /// <param name="bytes">An array of bytes to convert.</param>
        /// <returns>A hexadecimal string containing converted from the byte array.</returns>
        internal static string ToHexString(this byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i ++)
            {
                //fxcop says to supply an iformatprovider
                sb.Append(bytes[i].ToString("x2", c));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts a string into an int.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>The int.</returns>
        internal static int ToInt32(this string str)
        {
            return str == null ? -1 : Convert.ToInt32(str, c);
        }

        /// <summary>
        /// Determines if the string is case-insensitively equal to the other.
        /// </summary>
        /// <param name="str">First string.</param>
        /// <param name="str2">Other strings.</param>
        /// <returns>I don't even have to document internal code :D</returns>
        internal static bool CaseInsensitiveEquals(this string str, params string[] str2)
        {
            foreach (var s in str2)
            {
                if (string.Equals(str, s, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        internal static string Format2(this string str, params object[] args)
        {
            return string.Format(c, str, args);
        }

        internal static string ToStringG(this int i)
        {
            return i.ToString(c);
        }

        /// <summary>
        /// Determine whether the <see cref="Packet"/> request was a success.
        /// </summary>
        /// <param name="packet">The <see cref="Packet"/> that will be tested.</param>
        /// <returns>True if the <paramref name="packet"/> isn't null and the first word is "OK" for responses.</returns>
        /// <remarks>
        /// Since this is an extension method, <paramref name="packet"/> can be null (Nothing in Visual Basic).
        /// This means that this method will work on <see cref="RconClient.SendRequest"/> if the request times out, 
        /// where a timed out request returns false.
        /// </remarks>
        public static bool Success(this Packet packet)
        {
            return packet != null && (packet.IsResponse ? (packet.Words.Count > 0 && packet.Words[0] == "OK") : true);
        }
    }
}
