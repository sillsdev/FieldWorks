// ---------------------------------------------------------------------------------------------
// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IScrProjMetaDataProvider.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

namespace SILUBS.SharedScrUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a scripture reference
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IScrProjMetaDataProvider
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current versification scheme
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ScrVers Versification { get;}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string (typically a single character) that is used to separate the chapter
		/// number from the verse number in a well-formed Scripture reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ChapterVerseSepr { get; }
	}
}
