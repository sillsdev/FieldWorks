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
// File: ScriptureUtilsException.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Reflection;
using System.Resources;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Enumeration for ScriptureUtilsException error codes
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum SUE_ErrorCode
	{
		/// <summary>A system file access error has occurred</summary>
		FileError,

		/// <summary>A verse number was found without a chapter number</summary>
		MissingChapterNumber,

//		/// <summary>A chapter number was detected that is out of order</summary>
//		ChapterNumberOutOfOrder,

		/// <summary>The chapter number was invalid</summary>
		InvalidChapterNumber,

		/// <summary>The picture parameters were invalid</summary>
		InvalidPictureParameters,

		/// <summary>The picture filename was invalid</summary>
		InvalidPictureFilename,

		/// <summary>No book was found in the file</summary>
		MissingBook,

		/// <summary>The book ID in the ID line was not valid</summary>
		InvalidBookID,

		/// <summary>The verse number found is not valid</summary>
		InvalidVerseNumber,

		/// <summary>A verse was encountered without a book</summary>
		VerseWithNoBook,

		/// <summary>Unexcluded data was encountered before a \id marker</summary>
		UnexcludedDataBeforeIdLine,

		/// <summary>No chapter field was found in the file</summary>
		NoChapterNumber,

		/// <summary>A chapter was found without a book</summary>
		ChapterWithNoBook,

		/// <summary>A figure entry was found that is incorrectly formed</summary>
		BadFigure,

		/// <summary>A figure entry was found that is incorrectly formed</summary>
		IntroWithinScripture,

		/// <summary>A marker was found with invalid characters</summary>
		InvalidCharacterInMarker,

		/// <summary>
		/// All "Unable to Import XML" errors should follow this.
		/// </summary>
		XmlMin,

		/// <summary>OXES migration from a previous version failed</summary>
		OxesMigrationFailed,

		/// <summary>OXES file could not be validated</summary>
		OxesValidationFailed,

		/// <summary>The writing system in an OXES file is not defined.</summary>
		UndefinedWritingSystem,

		/// <summary>The writing system in an OXES file is not defined for a run of text.</summary>
		UndefinedWritingSystemInRun,

		/// <summary>
		/// End of all "Unable to Import XML" errors
		/// </summary>
		XmlLim,

		/// <summary>
		/// All "Unable to Import Back Translation" errors should follow this
		/// </summary>
		BackTransMin,

		/// <summary>An invalid paragraph style was found for back translation segment</summary>
		BackTransStyleMismatch = BackTransMin,

		/// <summary>A back translation paragraph was found that had no corresponding vernacular
		/// para</summary>
		BackTransParagraphMismatch,

		/// <summary>A back translation text is not part of paragraph</summary>
		BackTransTextNotPartOfParagraph,

		/// <summary>A back translation paragraph was found that did not belong to the immediately
		/// preceeding vernacular paragraph</summary>
		BackTransMissingVernPara,

		/// <summary>No corresponding vernacular book found for back translation</summary>
		BackTransMissingVernBook,

		/// <summary>No corresponding vernacular footnote found for back translation</summary>
		BackTransMissingVernFootnote,

		/// <summary>
		/// A picture caption back translation does not have a corresponding picture in the vernacular
		/// </summary>
		BackTransMissingVernPicture,

		// =====================================================================================
		// NOTE: Any non back translation error codes should be put before the BackTransMin
		// value or the error will NOT result in the import being rolled back when it
		// should. (FWR-2137)
		// All XML import error codes should be included between XmlMin and XmlLim.
		// =====================================================================================
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ErrorCodeType indicates the type of import where an error code occurred
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum ErrorCodeType
	{
		/// <summary>Import error code for standard format.</summary>
		SfmErrorCode,

		/// <summary>Import error code for back translation in standard format.</summary>
		BackTransErrorCode,

		/// <summary>Import error code for XML.</summary>
		XmlErrorCode
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ScriptureUtilsException.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScriptureUtilsException: Exception
	{
		private string m_helpTopic;
		private static ResourceManager s_stringResources = null;
		private string m_message;
		private SUE_ErrorCode m_errorCode;
		private bool m_fInterleavedImport = false;

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a Scripture utils exception. These are used for import errors.
		/// </summary>
		/// <param name="errorCode">numeric error code</param>
		/// <param name="fileName">file name where the error was encountered</param>
		/// <param name="lineNumber">line number in the file where the error occurred</param>
		/// <param name="lineContents">Contents of problematic line (or segment)</param>
		/// <param name="book">3-letter Book ID</param>
		/// <param name="chapter">Chapter number</param>
		/// <param name="verse">Verse number</param>
		/// <param name="fInterleavedImport"></param>
		/// ------------------------------------------------------------------------------------
		public ScriptureUtilsException(SUE_ErrorCode errorCode, string fileName, int lineNumber,
			string lineContents, string book, string chapter, string verse, bool fInterleavedImport)
		{
			bool fIncludeLineInfo;
			ErrorCode = errorCode;
			GetErrorMsgAndHelpTopic(errorCode, out m_message, out m_helpTopic, out fIncludeLineInfo);
			m_message += Environment.NewLine + Environment.NewLine +
				FormatErrorDetails(fileName, fIncludeLineInfo, lineNumber, lineContents,
				book, chapter, verse);
			m_fInterleavedImport = fInterleavedImport;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a Scripture utils exception. These are used for import errors.
		/// </summary>
		/// <param name="errorCode">numeric error code</param>
		/// <param name="fileName">file name where the error was encountered</param>
		/// <param name="lineNumber">line number in the file where the error occurred</param>
		/// <param name="lineContents">Contents of problematic line (or segment)</param>
		/// <param name="book">3-letter Book ID</param>
		/// <param name="chapter">Chapter number</param>
		/// <param name="verse">Verse number</param>
		/// ------------------------------------------------------------------------------------
		public ScriptureUtilsException(SUE_ErrorCode errorCode, string fileName, int lineNumber,
			string lineContents, string book, string chapter, string verse):
			this(errorCode, fileName, lineNumber, lineContents, book, chapter, verse, false)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a Scripture utils exception. These are used for import errors.
		/// </summary>
		/// <param name="errorCode">numeric error code</param>
		/// <param name="fileName">file name where the error was encountered</param>
		/// <param name="lineNumber">line number in the file where the error occurred</param>
		/// <param name="lineContents">Contents of problematic line (or segment)</param>
		/// <param name="scrRef">scripture reference where the error occurred</param>
		/// ------------------------------------------------------------------------------------
		public ScriptureUtilsException(SUE_ErrorCode errorCode, string fileName, int lineNumber,
			string lineContents, BCVRef scrRef):
			this(errorCode, fileName, lineNumber, lineContents, ScrReference.NumberToBookCode(scrRef.Book),
			scrRef.Chapter.ToString(), scrRef.Verse.ToString(), false)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a Scripture utils exception. These are used for import errors.
		/// </summary>
		/// <param name="errorCode">numeric error code</param>
		/// <param name="fileName">file name where the error was encountered</param>
		/// <param name="lineNumber">line number in the file where the error occurred</param>
		/// <param name="lineContents">Contents of problematic line (or segment)</param>
		/// <param name="scrRef">scripture reference where the error occurred</param>
		/// <param name="fInterleavedImport"></param>
		/// ------------------------------------------------------------------------------------
		public ScriptureUtilsException(SUE_ErrorCode errorCode, string fileName, int lineNumber,
			string lineContents, BCVRef scrRef, bool fInterleavedImport) :
			this(errorCode, fileName, lineNumber, lineContents, ScrReference.NumberToBookCode(scrRef.Book),
			 scrRef.Chapter.ToString(), scrRef.Verse.ToString(), fInterleavedImport)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct an import exception whose message contains a description of the error,
		/// including the offending segment, optional additional information, and BCV details.
		/// </summary>
		/// <param name="errorCode">numeric error code</param>
		/// <param name="sParam">Parameter to put into error message if it is a format string
		/// (can be null)</param>
		/// <param name="moreInfo">Details about the specific problem</param>
		/// <param name="book">3-letter Book ID</param>
		/// <param name="chapter">Chapter number</param>
		/// <param name="verse">Verse number</param>
		/// <param name="fInterleavedImport"></param>
		/// ------------------------------------------------------------------------------------
		public ScriptureUtilsException(SUE_ErrorCode errorCode, string sParam, string moreInfo,
			string book, string chapter, string verse, bool fInterleavedImport)
		{
			bool fIncludeLineInfo;
			ErrorCode = errorCode;
			GetErrorMsgAndHelpTopic(errorCode, out m_message, out m_helpTopic, out fIncludeLineInfo);
			if (sParam != null)
				m_message = string.Format(m_message, sParam);
			if (!string.IsNullOrEmpty(moreInfo))
				m_message += Environment.NewLine + moreInfo;

			string sBcvDetails = BCVDetails(book, chapter, verse);
			if (!string.IsNullOrEmpty(sBcvDetails))
				m_message += Environment.NewLine + sBcvDetails;

			m_fInterleavedImport = fInterleavedImport;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct an import exception whose message contains a description of the error,
		/// including the offending segment, optional additional information, and BCV details.
		/// </summary>
		/// <param name="errorCode">numeric error code</param>
		/// <param name="moreInfo">Details about the specific problem</param>
		/// <param name="fileName">file name where the error was encountered</param>
		/// <param name="lineNumber">line number in the file where the error occurred</param>
		/// <param name="lineContents">Contents of problematic line (or segment)</param>
		/// <param name="scrRef">scripture reference where the error occurred</param>
		/// ------------------------------------------------------------------------------------
		public ScriptureUtilsException(SUE_ErrorCode errorCode, string moreInfo,
			string fileName, int lineNumber, string lineContents, BCVRef scrRef)
		{
			bool fIncludeLineInfo;
			ErrorCode = errorCode;
			GetErrorMsgAndHelpTopic(errorCode, out m_message, out m_helpTopic, out fIncludeLineInfo);

			// Add file and Scripture reference information.
			m_message += Environment.NewLine + Environment.NewLine +
				FormatErrorDetails(fileName, fIncludeLineInfo, lineNumber, lineContents,
				BCVRef.NumberToBookCode(scrRef.Book), scrRef.Chapter.ToString(), scrRef.Verse.ToString());

			// Add any details about the error.
			if (!string.IsNullOrEmpty(moreInfo))
				m_message += Environment.NewLine + moreInfo;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptureUtilsException"/> class
		/// appropriate for reporting third-party exceptions that occur during importing.
		/// </summary>
		/// <param name="errorCode">numeric error code</param>
		/// <param name="fileName">file name where the error was encountered</param>
		/// <param name="e">An Exception</param>
		/// -----------------------------------------------------------------------------------
		public ScriptureUtilsException(SUE_ErrorCode errorCode, string fileName, Exception e)
			: base("", e)
		{
			bool fIncludeLineInfo;
			ErrorCode = errorCode;
			GetErrorMsgAndHelpTopic(errorCode, out m_message, out m_helpTopic, out fIncludeLineInfo);
			m_message += Environment.NewLine +
				string.Format(GetResourceString("kstidImportErrorFileDetails"), fileName);

			Exception innerException = InnerException;
			while (innerException != null)
			{
				m_message += Environment.NewLine + innerException.Message;
				innerException = innerException.InnerException;
			}
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the exception message
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string Message
		{
			get { return m_message; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the error code value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SUE_ErrorCode ErrorCode
		{
			get{ return m_errorCode; }
			set{ m_errorCode = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the help topic for the exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string HelpTopic
		{
			get { return m_helpTopic; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of import for the error code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ErrorCodeType ImportErrorCodeType
		{
			get
			{
				if (m_errorCode < SUE_ErrorCode.XmlMin)
					return ErrorCodeType.SfmErrorCode;

				if (m_errorCode < SUE_ErrorCode.XmlLim)
					return ErrorCodeType.XmlErrorCode;

				return ErrorCodeType.BackTransErrorCode;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether this error occurred during an import of an interleaved Back Translation
		/// (needed to determine whether to roll back the import)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool InterleavedImport
		{
			get { return m_fInterleavedImport; }
		}
		#endregion

		#region static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Interpret the error code and return an appropriate error message string and help
		/// topic URL.
		/// </summary>
		/// <param name="error">An error code</param>
		/// <param name="msg">A string containing a user-friendly explanation of the problem
		/// </param>
		/// <param name="topic">the correct help topic for the error</param>
		/// <param name="includeLineInfoInDetails">A flag indicating whether caller should
		/// include the reported line number (if any) when formatting the details for the user.
		/// (For some errors the line number, although reported by TESO, is meaningless.)</param>
		/// ------------------------------------------------------------------------------------
		static public void GetErrorMsgAndHelpTopic(SUE_ErrorCode error, out string msg,
			out string topic, out bool includeLineInfoInDetails)
		{
			includeLineInfoInDetails = true;
			topic = "/Beginning_Tasks/Import_Standard_Format/Unable_to_Import";
			string stidErrorMsg = null;

			switch (error)
			{
				case SUE_ErrorCode.FileError:
					stidErrorMsg = "kstidImportFileError";
					topic += "/File_error_occurred.htm";
					includeLineInfoInDetails = false;
					break;

				case SUE_ErrorCode.MissingBook:
					stidErrorMsg = "kstidImportMissingBookAndChapterMarkers";
					topic += "/No_book_or_chapter_markers_in_file.htm";
					includeLineInfoInDetails = false;
					break;

				case SUE_ErrorCode.MissingChapterNumber:
					stidErrorMsg = "kstidImportVerseNoChapter";
					topic += "/Verse_number_without_a_preceding_chapter_number.htm";
					break;

				case SUE_ErrorCode.ChapterWithNoBook:
					stidErrorMsg = "kstidImportChapterNoBook";
					topic += "/Chapter_not_part_of_a_book.htm";
					break;

				case SUE_ErrorCode.UnexcludedDataBeforeIdLine:
					stidErrorMsg = "kstidImportUnexcludedDataBeforeId";
					topic += "/Unexpected_field_precedes_the_id_identification_field.htm";
					break;

				case SUE_ErrorCode.NoChapterNumber:
					// No chapter marker in a book that requires one
					stidErrorMsg = "kstidImportBooksNoChapters";
					topic += "/Missing_chapter_marker.htm";
					includeLineInfoInDetails = false;
					break;

				case SUE_ErrorCode.InvalidBookID:
					stidErrorMsg = "kstidImportInvalidBook";
					topic += "/Invalid_book_code.htm";
					break;

				case SUE_ErrorCode.InvalidChapterNumber:
					// chapter numbers {vernacular or not} are non-numeric
					stidErrorMsg = "kstidImportInvalidChapterNumber";
					topic += "/Invalid_chapter_number.htm";
					break;

				case SUE_ErrorCode.InvalidVerseNumber:
					// verse numbers {vernacular or not} are non-numeric or
					//  are an invalid verse range
					stidErrorMsg = "kstidImportInvalidVerseNumber";
					topic += "/Invalid_verse_number.htm";
					break;

				case SUE_ErrorCode.InvalidPictureParameters:
					//The picture parameters are invalid
					stidErrorMsg = "kstidImportErrorPictureFileName";
					topic += "/Invalid_figure_parameters.htm";
					break;

				case SUE_ErrorCode.InvalidPictureFilename:
					//The picture filename is invalid
					stidErrorMsg = "kstidImportErrorPictureFileName";
					topic += "/Invalid_figure_file_name_property.htm";
					break;

				case SUE_ErrorCode.VerseWithNoBook:
					// A verse was encountered without a book id
					stidErrorMsg = "kstidImportVerseNotPartOfBook";
					topic += "/Verse_not_part_of_a_book.htm";
					break;

				case SUE_ErrorCode.BadFigure:
					// A badly-formed \fig entry was encountered
					stidErrorMsg = "kstidImportBadFigure";
					topic += "/Invalid_figure_parameters.htm";
					break;

				case SUE_ErrorCode.IntroWithinScripture:
					stidErrorMsg = "kstidImportIntroWithinScripture";
					topic += "/Book_introduction_within_Scripture_text.htm";
					break;

				case SUE_ErrorCode.InvalidCharacterInMarker:
					stidErrorMsg = "kstidImportInvalidCharacterInMarker";
					topic += "/Invalid_character_in_marker.htm";
					break;

				case SUE_ErrorCode.BackTransStyleMismatch:
					stidErrorMsg = "kstidBTStyleMismatch";
					topic += "_Back_Translation/Back_translation_does_not_correspond_to_the_preceding_vernacular_paragraph.htm";
					includeLineInfoInDetails = false;
					break;

				case SUE_ErrorCode.BackTransParagraphMismatch:
					stidErrorMsg = "kstidBTParagraphMismatch";
					topic += "_Back_Translation/Back_translation_does_not_correspond_to_a_vernacular_paragraph.htm";
					includeLineInfoInDetails = false;
					break;

				case SUE_ErrorCode.BackTransTextNotPartOfParagraph:
					stidErrorMsg = "kstidBtTextNotPartofPara";
					topic += "_Back_Translation/Back_translation_not_part_of_a_paragraph.htm";
					includeLineInfoInDetails = false;
					break;

				case SUE_ErrorCode.BackTransMissingVernPara:
					stidErrorMsg = "kstidMissingVernParaForBT";
					topic += "_Back_Translation/Back_translation_precedes_vernacular_fields_in_book.htm";
					break;

				case SUE_ErrorCode.BackTransMissingVernBook:
					stidErrorMsg = "kstidMissingVernBookForBT";
					topic += "_Back_Translation/No_corresponding_vernacular_book_for_back_translation.htm";
					includeLineInfoInDetails = false;
					break;

				case SUE_ErrorCode.BackTransMissingVernFootnote:
					stidErrorMsg = "kstidMissingVernFootnoteForBT";
					topic += "_Back_Translation/Back_translation_does_not_correspond_to_a_vernacular_footnote.htm";
					break;

				case SUE_ErrorCode.BackTransMissingVernPicture:
					stidErrorMsg = "kstidBTNoCorrespondingPicture";
					topic += "_Back_Translation/Back_translation_does_not_correspond_to_a_vernacular_picture.htm";
					break;

				case SUE_ErrorCode.OxesMigrationFailed:
					stidErrorMsg = "kstidOxesMigrationFailed";
					topic = "/Beginning_Tasks/Import_XML/Unable_to_Import_XML/Incompatible_file_version.htm";
					break;

				case SUE_ErrorCode.OxesValidationFailed:
					stidErrorMsg = "kstidOxesValidationFailed";
					topic = "/Beginning_Tasks/Import_XML/Unable_to_Import_XML/Invalid_file.htm";
					break;

				case SUE_ErrorCode.UndefinedWritingSystem:
					stidErrorMsg = "kstidUndefinedWritingSystem";
					topic = "/Beginning_Tasks/Import_XML/Unable_to_Import_XML/Unknown_writing_system.htm";
					includeLineInfoInDetails = false;
					break;

				case SUE_ErrorCode.UndefinedWritingSystemInRun:
					stidErrorMsg = "kstidUndefinedWritingSystem";
					topic = "/Beginning_Tasks/Import_XML/Unable_to_Import_XML/Unknown_writing_system.htm";
					// For an undefined writing system in a run, we have text that we can display so we
					// are able to include line info details.
					break;
			}
			msg = GetResourceString(stidErrorMsg);

			/* Remove from help files before deleting! */
			//				case ECERROR_VerseNoBook:
			//					stidErrorMsg = "kstidImportVerseNotPartOfBook";
			//					topic += "Verse_not_part_of_a_book.htm";
			//					break;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Formats an import error message string with details about the book, chapter and
		/// verse where the error ocurred.
		/// </summary>
		/// <param name="sBook"></param>
		/// <param name="sChapter"></param>
		/// <param name="sVerse"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public string BCVDetails(string sBook, string sChapter, string sVerse)
		{
			string msg = string.Empty;

			if (sBook != null && sBook != string.Empty)
			{
				msg += string.Format(GetResourceString("kstidImportErrorBookDetails"),
					sBook);
			}
			if (sChapter != null && sChapter != string.Empty)
			{
				msg += "  " +
					string.Format(GetResourceString("kstidImportErrorChapterDetails"),
					sChapter);
			}
			if (sVerse != null && sVerse != string.Empty)
			{
				msg += "  " +
					string.Format(GetResourceString("kstidImportErrorVerseDetails"),
					sVerse);
			}
			return msg;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Format error message details.
		/// </summary>
		/// <param name="filename">Path of file</param>
		/// <param name="includeLineInfo"><c>true</c> if line number and contents should be
		/// included in formatted error message</param>
		/// <param name="lineNumber">line number where error occurred in file</param>
		/// <param name="lineContents">Contents of problematic line (or segment)</param>
		/// <param name="book">3-letter Book ID</param>
		/// <param name="chapter">Chapter number</param>
		/// <param name="verse">Verse number</param>
		/// <returns>Formatted error message details</returns>
		/// ------------------------------------------------------------------------------------
		static public string FormatErrorDetails(string filename, bool includeLineInfo,
			int lineNumber,	string lineContents, string book, string chapter, string verse)
		{
			string msg = string.Empty;
			if (includeLineInfo)
			{
				// If we didn't get a filename, no point trying to display the file and line number.
				if (!string.IsNullOrEmpty(filename))
				{
					msg = string.Format(GetResourceString("kstidImportErrorLineAndFileDetails"),
						lineNumber, filename);
				}

				if (lineContents != null && lineContents != string.Empty)
					msg += lineContents;
			}
			else if (filename != null && filename != string.Empty)
			{
				msg = string.Format(GetResourceString("kstidImportErrorFileDetails"), filename);
			}

			string sBcvDetails = BCVDetails(book, chapter, verse);
			if (sBcvDetails != null && sBcvDetails != string.Empty)
				msg += Environment.NewLine + sBcvDetails;
			return msg;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>String</returns>
		/// -----------------------------------------------------------------------------------
		public static string GetResourceString(string stid)
		{
			if (s_stringResources == null)
			{
				s_stringResources = new ResourceManager(
					"SIL.FieldWorks.Common.ScriptureUtils.ScrUtilsStrings",
					Assembly.GetExecutingAssembly());
			}

			return (stid == null ? "NullStringID" : s_stringResources.GetString(stid));
		}
		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Exceptions thrown as a result of an encoding converter error
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class EncodingConverterException: Exception
	{
		private string m_helpTopic;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct an exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EncodingConverterException(string message, string helpTopic): base(message)
		{
			m_helpTopic = helpTopic;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the help topic for the exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string HelpTopic
		{
			get { return m_helpTopic; }
		}
	}
}
