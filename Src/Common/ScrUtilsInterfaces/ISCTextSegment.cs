// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ISCTextSegment.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ISCTextSegment.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ISCTextSegment
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Beginning point, a scripture reference indicating where the segment begins.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		BCVRef FirstReference{ get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ending point, an inclusive scripture reference indicating
		/// where the segment ends.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		BCVRef LastReference{ get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the filename from which the segment was read.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string CurrentFileName{	get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the line number from which the segment was read
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int CurrentLineNumber{	get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the literal text from a \v marker
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string LiteralVerseNum{ get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The text of the segment. Contains no line separators.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string Text{ get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A string giving the marker used to mark this text segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string Marker{ get; }
	}
}
