// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: LCIDHelper.cs
// Responsibility: EberhardB
// Last reviewed:
//
// <remarks>
// Implementation of LcidHelper. The class contains only static methods. In the legacy code
// these were defined as macros in "winnt.h".
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;

namespace SIL.Utils
{
	/// <summary>
	/// LCID helper methods (were defined as macros in "winnth.h")
	/// </summary>
	public class LcidHelper
	{
		private LcidHelper()
		{
			// no need to construct a object, because it only contains static methods!
		}

		// The following methods come from winnt.h
		/// <summary>
		/// Get the language Id from a LCID
		/// </summary>
		/// <param name="lcid">the locale culture identifier</param>
		/// <returns>Language id</returns>
		static public short LangIdFromLCID(int lcid)
		{
			return (short)lcid;
		}

		/// <summary>
		/// Get the sort id from a LCID
		/// </summary>
		/// <param name="lcid">the locale culture identifier</param>
		/// <returns>Sort identifier</returns>
		static public short SortIdFromLCID(int lcid)
		{
			return (short)((lcid >> 16) & 0xf);
		}

		/// <summary>
		/// Get the sort version from a LCID
		/// </summary>
		/// <param name="lcid">the locale culture identifier</param>
		/// <returns>Sort version</returns>
		static public short SortVersionFromLCID(int lcid)
		{
			return (short)((lcid >> 20) & 0xf);
		}

		/// <summary>
		/// Create a LCID from a language id and a sort id
		/// </summary>
		/// <param name="lgid">The language id</param>
		/// <param name="srtid">The sort id</param>
		/// <returns>LCID</returns>
		static public int MakeLCID(short lgid, short srtid)
		{
			return (srtid << 16) | (ushort)lgid;
		}

		/// <summary>
		/// Create a LCID from a language id, a sort id and sort version
		/// </summary>
		/// <param name="lgid">The language id</param>
		/// <param name="srtid">The sort id</param>
		/// <param name="ver">Sort version</param>
		/// <returns>LCID</returns>
		static public int MakeSortLCID(short lgid, short srtid, short ver)
		{
			return MakeLCID(lgid, srtid) | (ver << 20);
		}
	}

}
