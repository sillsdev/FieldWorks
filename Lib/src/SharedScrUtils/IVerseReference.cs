// ---------------------------------------------------------------------------------------------
// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IVerseReference.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

namespace SILUBS.SharedScrUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a scripture reference
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IVerseReference
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the verse reference as a string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ToString();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets the current versification scheme
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ScrVers Versification { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses Scripture reference string.
		/// </summary>
		/// <param name="sTextToBeParsed">Reference string the user types in.</param>
		/// <remarks>This method is pretty similar to MultilingScrBooks.ParseRefString, but
		/// it deals only with SIL codes.</remarks>
		/// ------------------------------------------------------------------------------------
		void Parse(string sTextToBeParsed);
	}
}
