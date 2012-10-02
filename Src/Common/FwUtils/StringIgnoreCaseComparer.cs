// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StringIgnoreCaseComparer.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwUtils
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
