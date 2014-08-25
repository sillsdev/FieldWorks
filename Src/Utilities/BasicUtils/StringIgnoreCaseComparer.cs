// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: StringIgnoreCaseComparer.cs
// Responsibility: mcconnel

using System;
using System.Collections.Generic;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This allows strings to be compared in a case-insensitive manner.  This can be useful for
	/// Dictionary and possibly other collection classes.
	/// </summary>
	public class StringIgnoreCaseComparer : IEqualityComparer<string>
	{
		#region IEqualityComparer<string> Members

		/// <summary>
		/// Determines whether the specified objects are equal.
		/// </summary>
		/// <returns>
		/// true if the specified objects are equal; otherwise, false.
		/// </returns>
		public bool Equals(string x, string y)
		{
			return x.Equals(y, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Returns a hash code for the specified object.
		/// </summary>
		public int GetHashCode(string x)
		{
			return x.ToLowerInvariant().GetHashCode();
		}

		#endregion
	}
}
