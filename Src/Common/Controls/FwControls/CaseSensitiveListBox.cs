// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CaseSensistiveListBox.cs
// Responsibility: TE TEam
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// A list box with treats the items as case sensitive when looking for string matches.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class CaseSensitiveListBox : ListBox
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CaseSensitiveListBox"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public CaseSensitiveListBox()
		{
			Sorted = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the first item in the <see cref="T:System.Windows.Forms.ListBox"/> that starts
		/// with the specified string and matches the case exactly.
		/// </summary>
		/// <param name="s">The text to search for.</param>
		/// <returns>
		/// The zero-based index of the first item found; returns ListBox.NoMatches if no match
		/// is found.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public new int FindString(string s)
		{
			for (int i = 0; i < Items.Count; i++)
			{
				if (Items[i].ToString().StartsWith(s))
					return i;
			}
			return ListBox.NoMatches;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the first item in the <see cref="T:System.Windows.Forms.ListBox"/> that
		/// exactly matches the specified string.
		/// </summary>
		/// <param name="s">The text to search for.</param>
		/// <returns>
		/// The zero-based index of the first item found; returns ListBox.NoMatches if no match
		/// is found.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public new int FindStringExact(string s)
		{
			for (int i = 0; i < Items.Count; i++)
			{
				if (Items[i].ToString().Normalize() == s.Normalize())
					return i;
			}
			return ListBox.NoMatches;
		}
	}
}
