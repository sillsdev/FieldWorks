using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	#region Embedded Objects
	/// <summary>
	/// BookLabel structure is for GetBookNames() below
	/// </summary>
	public class BookLabel
	{
		/// <summary></summary>
		public string Label;
		/// <summary></summary>
		public int BookNum;

		/// <summary>
		///
		/// </summary>
		/// <param name="sLabel"></param>
		/// <param name="nBookNum"></param>
		public BookLabel(string sLabel, int nBookNum)
		{
			Label = sLabel;
			BookNum = nBookNum;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return Label;
		}
	}
	#endregion

	/// <summary>
	/// Interface for FieldWorks' MultilingScrBooks
	/// A MultilingScrBooks object may be implemented to represent either
	/// a) a full list of scripture book names in many languages, or
	/// b) book names and book contents present in a scripture database, e.g. FW
	/// </summary>
	public interface IMultilingScrBooks
	{
		#region Properties

		/// <summary>
		/// RequestedEncodings property
		/// The first writing system in the array is called the primary
		/// writing system by the spec. The array may also optionally include a list of secondary
		/// encodings, which are used by MultilingScrBooks if the primary writing system does not
		/// suffice for some reason. The caller must ensure that the UI writing system is included
		/// in the array.
		/// </summary>
		List<int> RequestedEncodings { get;set;}

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

		// SetBookNames(ws, BookNameInfo[] bniNames);

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
		/// Returns false, if hooked to a scr database and the book text does not exist.
		/// </summary>
		/// <param name="nBook">one-based index of the book (Genesis = 1).</param>
		/// <returns>In implementation (a): always return true.
		/// In implementation (b) (hooked to a scr database): return true if book text exists.</returns>
		/// ------------------------------------------------------------------------------------
		bool IsBookAvailableInDb(int nBook);

		// AreNamesAvailableForEnc
		//  true if any names are available in given writing system
	//	bool AreNamesAvailableForEnc(int ws);

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
		/// If name of one book is not available in the first (primary) writing system,
		/// the info in the next available (secondary) writing system is substituted.
		/// If not available in any given writing system, the 3-letter SIL/UBS book code is returned.
		/// </summary>
		/// <param name="fAvailable">The output array only includes available books if fAvailable is true.</param>
		/// <returns>An array of BookLabel objects in the requested primary writing system,
		/// as far as possible.</returns>
		/// ------------------------------------------------------------------------------------
		BookLabel[] GetBookNames(bool fAvailable);

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the object with the proper writing system factory, which also initializes
		/// some standard writing system ids.
		/// </summary>
		/// <param name="wsf">the writing system factory</param>
		/// <returns>nothing</returns>
		/// ------------------------------------------------------------------------------------
		void InitializeWritingSystems(ILgWritingSystemFactory wsf);
		#endregion
	}
}
