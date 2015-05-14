// --------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MultilingScrBooks.cs
// --------------------------------------------------------------------------------------------
using System.Collections.Generic;

namespace SILUBS.SharedScrUtils
{
	#region class BookLabel
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to associate a book label (name or abbreviation) with a canonical book number.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BookLabel
	{
		/// <summary></summary>
		public string Label;
		/// <summary></summary>
		public int BookNum;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sLabel">The s label.</param>
		/// <param name="nBookNum">The n book num.</param>
		/// ------------------------------------------------------------------------------------
		public BookLabel(string sLabel, int nBookNum)
		{
			Label = sLabel;
			BookNum = nBookNum;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Label;
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for FieldWorks' MultilingScrBooks
	/// A MultilingScrBooks object may be implemented to represent either
	/// a) a full list of scripture book names in many languages, or
	/// b) book names and book contents present in a scripture database, e.g. FW
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IMultilingScrBooks
	{
		#region Properties

		/// <summary>
		/// RequestedEncodings property
		/// The first writing system in the array is called the primary
		/// writing system by the spec. The array may also optionally include a list of secondary
		/// encodings, which are used by MultilingScrBooks if the primary writing system does not
		/// suffice for some reason. Typically the UI writing system should be included in the array.
		/// </summary>
		List<string> RequestedEncodings { get;set;}

		/// <summary>
		/// ProcessDeuteroCanonical property
		/// True if Deutero-canonical refs are to be processed.
		/// Note: an implementation may not support Deutero-canonical references.
		/// </summary>
		bool ProcessDeuteroCanonical {get;set;}

		/// <summary>
		/// ScriptureBooksLim property
		/// The maximum number of Scripture books that may be returned to the caller.
		/// </summary>
		int ScriptureBooksLim {get;}

		/// <summary>
		/// ReferenceFormat property
		/// The specified format for the Scripture reference strings.
		/// e.g. ReferenceFormat of 0 specifies Western format -> "Eph 3:2".
		/// Note: an implementation may not support multiple formats.
		/// </summary>
		short ReferenceFormat {get;set;}

		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the reference string is a valid canonical scripture
		/// reference.
		/// </summary>
		/// <param name="reference">Scripture reference</param>
		/// <returns>True if the reference is valid. Otherwise false... imagine that.</returns>
		/// ------------------------------------------------------------------------------------
		bool IsReferenceValid(string reference);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified book index is within the range of valid
		/// books.
		/// </summary>
		/// <param name="nBook">one-based index of the book (Genesis = 1).</param>
		/// <returns>True if the book is valid. Otherwise false.</returns>
		/// ------------------------------------------------------------------------------------
		bool IsBookValid(int nBook);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the book name for a given book number.
		/// </summary>
		/// <param name="nBook">one-based index of the book (Genesis = 1).</param>
		/// <returns>The book name as a string.</returns>
		/// ------------------------------------------------------------------------------------
		string GetBookName(int nBook);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the book abbreviation for a given book number.
		///  (get Primary writing system abbrev; if not available, get the SIL code)
		/// </summary>
		/// <param name="nBook">one-based index of the book (Genesis = 1).</param>
		/// <returns>The book abbreviation as a string.</returns>
		/// ------------------------------------------------------------------------------------
		string GetBookAbbrev(int nBook);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an array of book labels for the books represented by this instance.
		/// </summary>
		/// <returns>An array of BookLabel objects in the requested primary writing system,
		/// as far as possible.</returns>
		/// ------------------------------------------------------------------------------------
		BookLabel[] BookLabels { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the user typed in string.  Creates and returns an ScrReference object.
		/// </summary>
		/// <param name="sTextToBeParsed">Reference string the user types in.</param>
		/// <returns>The generated scReference object.</returns>
		/// ------------------------------------------------------------------------------------
		ScrReference ParseRefString(string sTextToBeParsed);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the user typed in string.  Creates and returns an ScrReference object.
		/// </summary>
		/// <param name="sTextToBeParsed">Reference string the user types in.</param>
		/// <param name="startingBook">The 0-based index of starting book to consider when
		/// parsing the reference.</param>
		/// <returns>The generated scReference object.</returns>
		/// ------------------------------------------------------------------------------------
		ScrReference ParseRefString(string sTextToBeParsed, int startingBook);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Generates a cannonical text form of the given reference, consisting of
		/// book abbreviation (in primary writing system), chapter nbr, colon, verse nbr.
		/// </summary>
		/// <param name="scRef">The given ScrReference object.</param>
		/// <returns>The generated text string reference.</returns>
		/// ------------------------------------------------------------------------------------
		string GetRefString(ScrReference scRef);
		#endregion
	}
}
