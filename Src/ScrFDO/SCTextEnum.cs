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
// File: SCTextEnum.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.Cellar;
using ECInterfaces;
using SilEncConverters31;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.Scripture
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for SCTextEnum.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SCTextEnum: ISCTextEnum
	{
		#region Member Data
		private IScrImportSet m_settings;
		private ImportDomain m_domain;
		private MappingSet m_mappingSet;
		private BCVRef m_startRef;
		private BCVRef m_endRef;
		private BCVRef m_currentStartRef;
		private BCVRef m_currentEndRef;
		private StreamReader m_sfFileReader = null;
		private ScrImportFileInfo m_currentFile;
		private ImportFileSource m_importSource;
		private IEnumerator m_importSourceEnum;
		private bool m_seenIdInFile = false;
		private int m_lineNumber;
		/// <summary></summary>
		protected IEncConverters m_encConverters;
		private Dictionary<string, IEncConverter> m_icuLocaleToConverterMap = new Dictionary<string, IEncConverter>();

		// List of inline markers (begin and end)
		private List<string> m_InlineBeginAndEndMarkers;

		// List of all of the characters that inline markers start with
		private char[] m_InlineMarkerStartChars;

		// This saves the remaining text of a line to be processed.
		private string m_remainingLineText = string.Empty;

		// This saves a paragraph data encoding so it can be used on segments
		// following in-line end markers.
		private IEncConverter m_prevDataEncoding = null;

		/// <summary>
		/// Code page 28591 will map all high-bit characters to the same
		/// value with a high-byte of 0 in unicode. For example, character 0x84
		/// will end up as 0x0084.
		/// </summary>
		public const int kMagicCodePage = 28591;
		#endregion

		#region constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SCTextEnum"/> class.
		/// </summary>
		/// <param name="settings">The import settings</param>
		/// <param name="domain">The source domain</param>
		/// <param name="startRef">first reference to retrieve</param>
		/// <param name="endRef">last reference to retrieve</param>
		/// ------------------------------------------------------------------------------------
		public SCTextEnum(IScrImportSet settings, ImportDomain domain,
			BCVRef startRef, BCVRef endRef)
		{
			m_settings = settings;
			m_domain = domain;
			m_startRef = new BCVRef(startRef);
			m_endRef = new BCVRef(endRef);
			m_mappingSet = ((ScrImportSet)m_settings).GetMappingSetForDomain(domain);

			// Gets the set of encoding converters
			m_encConverters = new EncConverters();

			// make a list of all of the begin and end markers for inline markers
			// Also build the map of encoding converters
			m_InlineBeginAndEndMarkers = new List<string>();
			foreach (ImportMappingInfo mapping in ((ScrImportSet)m_settings).GetMappingListForDomain(domain))
			{
				if (mapping.IsInline || m_settings.ImportTypeEnum == TypeOfImport.Paratext5)
				{
					m_InlineBeginAndEndMarkers.Add(mapping.BeginMarker);
					if (mapping.IsInline)
						m_InlineBeginAndEndMarkers.Add(mapping.EndMarker);
				}
			}

			m_InlineBeginAndEndMarkers.Sort(new LengthComparer());

			// Build a list of all of the characters that inline markers start with
			Set<char> tempCharList = new Set<char>();
			foreach (string marker in m_InlineBeginAndEndMarkers)
				tempCharList.Add(marker[0]); // Set ignores duplicates.

			m_InlineMarkerStartChars = tempCharList.ToArray();
			// Get the import file source that will provide the files to import.
			m_importSource = ((ScrImportSet)settings).GetImportFiles(domain);
			m_importSourceEnum = m_importSource.GetEnumerator();
		}
		#endregion

		#region ISCTextEnum members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the next text segment, null if there are no more segments
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ISCTextSegment Next()
		{
			// If there is text remaining from a previous line, then process it.
			if (m_remainingLineText != string.Empty)
				return ProcessNextSegmentOfText(m_remainingLineText, string.Empty,
					string.Empty);

			while (true)
			{
				// If a file is not open then open the next file
				if (m_sfFileReader == null && !OpenNextSourceFile(m_startRef, m_endRef))
					return null;

				// Get the next line from the file
				string line = ReadNextLine();
				if (line == null)
					continue;

				// Split up the line into marker and contents
				string marker;
				string lineContents;
				string literalVerse = string.Empty;
				SplitLine(line, out marker, out lineContents);

				// Get the first reference for this line and adjust the line contents and
				// literal verse text for the verse number segments.
				GetReferenceForLine(marker, ref lineContents, ref literalVerse);

				// If an ID line has not been encountered yet, skip this line
				if (!m_seenIdInFile)
					continue;

				// If this data is outside the included range of books then skip it
				if (m_currentStartRef.Book < m_startRef.Book || m_currentStartRef.Book > m_endRef.Book)
					continue;

				// Process the next segment from the line. Inline markers will cause the string
				// to be broken into multiple segments.
				return ProcessNextSegmentOfText(lineContents, marker, literalVerse);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the current writing system, or -1 if not known.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int CurrentWs
		{
			get
			{
				string icuLocale = m_currentFile.IcuLocale;
				if (icuLocale == null)
					return -1;
				return Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(icuLocale);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the current note type definition
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int CurrentNoteTypeHvo
		{
			get
			{
				return (m_currentFile != null) ? m_currentFile.NoteTypeHvo : 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleanup when done
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Cleanup()
		{
			// make sure there are no files left open
			if (m_sfFileReader != null)
			{
				m_sfFileReader.Close();
				m_sfFileReader = null;
			}
		}
		#endregion

		#region helpers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a cache from somewhere
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private FdoCache Cache
		{
			get { return m_settings.Cache; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the next set of lines from the source file that associate with a marker
		/// </summary>
		/// <returns>a string for the line text or null if the end of file was reached</returns>
		/// <remarks>standard format text can be broken across lines. Any line that does not
		/// start with a backslash will be appended to the previous line. Any lines at the
		/// beginning of the file that do not start with a backslash will be ignored as
		/// will blank lines.</remarks>
		/// ------------------------------------------------------------------------------------
		private string ReadNextLine()
		{
			string line;

			try
			{
				// Read the next line from the file. Keep reading until a line that
				// starts with a backslash is found.
				do
				{
					line = m_sfFileReader.ReadLine();
					m_lineNumber++;
					if (line == null)
					{
						// end of file - close the stream and quit
						m_sfFileReader.Close();
						m_sfFileReader = null;
						return null;
					}
				} while (line == string.Empty || line[0] != '\\');

				// Append any following lines that do not start with a backslash.
				line += AppendContinuationLines();
			}
			catch (Exception e)
			{
				// If there is a read failure then close the file first before throwing an error.
				if (m_sfFileReader != null)
				{
					m_sfFileReader.Close();
					m_sfFileReader = null;
				}
				throw new ScriptureUtilsException(SUE_ErrorCode.FileError, m_currentFile.FileName, e);
			}

			return line;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Peek at successive lines in the file to see if there are continuation lines.
		/// </summary>
		/// <returns>The text to be appended to the previous line</returns>
		/// ------------------------------------------------------------------------------------
		private string AppendContinuationLines()
		{
			string line;
			StringBuilder accumulation = new StringBuilder();
			while ((char)m_sfFileReader.Peek() != '\\')
			{
				line = m_sfFileReader.ReadLine();
				if (line == null)
				{
					m_sfFileReader.Close();
					m_sfFileReader = null;
					break;
				}

				++m_lineNumber;
				accumulation.Append(' ');
				accumulation.Append(line);
			}
			return accumulation.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the start and end references for the line of text with the given marker. (Sets
		/// m_currentStartRef and m_currentEndRef.)
		/// </summary>
		/// <param name="marker"></param>
		/// <param name="lineContents"></param>
		/// <param name="literalVerse"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private void GetReferenceForLine(string marker, ref string lineContents,
			ref string literalVerse)
		{
			// If this is a marker that will change the reference, then update
			// the reference
			if (marker == ScrImportSet.s_markerBook)
			{
				int book = BCVRef.BookToNumber(lineContents.ToUpper());
				if (book > 0)
				{
					m_currentStartRef.Book = book;
					m_currentStartRef.Chapter = 1;
					m_currentStartRef.Verse = 0;
				}
				m_currentEndRef = new BCVRef(m_currentStartRef);
				m_seenIdInFile = true;
			}
			else if (marker == ScrImportSet.s_markerChapter)
			{
				string chapterText = lineContents;
				try
				{
					m_currentStartRef.Chapter = ScrReference.ChapterToInt(chapterText, out lineContents);
					m_currentStartRef.Verse = 1;
					m_currentEndRef = new BCVRef(m_currentStartRef);
				}
				catch (ArgumentException)
				{
					throw new ScriptureUtilsException(SUE_ErrorCode.InvalidChapterNumber,
						m_currentFile.FileName, m_lineNumber, lineContents,
						BCVRef.NumberToBookCode(m_currentStartRef.Book), chapterText, null);
				}
			}
			else if (marker == ScrImportSet.s_markerVerse)
			{
				BCVRef.VerseToScrRef(lineContents.TrimStart(),
					out literalVerse, out lineContents, ref m_currentStartRef, ref m_currentEndRef);
			}
			else
			{
				if (lineContents == " ")
					lineContents = string.Empty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Split up a line of text into a marker and line contents
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void SplitLine(string line, out string marker, out string lineContents)
		{
			marker = line;
			lineContents = string.Empty;
			int splitPos = line.IndexOfAny(new char[] {' ', '\t'});
			if (splitPos != -1)
			{
				marker = line.Substring(0, splitPos);
				lineContents = line.Substring(splitPos + 1) + " ";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a segment of text. If there are inline markers, then break out the
		/// pieces and return the next segment
		/// </summary>
		/// <param name="source">source text to process</param>
		/// <param name="marker">text of the marker</param>
		/// <param name="literalVerse">literal verse string to use</param>
		/// <returns>the next text segment</returns>
		/// ------------------------------------------------------------------------------------
		private ISCTextSegment ProcessNextSegmentOfText(string source, string marker,
			string literalVerse)
		{
			int startPos;
			string startMarker = FindNextMarker(source, 0, out startPos);
			ImportMappingInfo markerMapping = ((ScrImportSet)m_settings).MappingForMarker(marker, m_mappingSet);

			// If there are no markers, return the entire string as the segment
			if (startMarker == null)
				m_remainingLineText = string.Empty;

			else if (startPos != 0 || marker != string.Empty)
			{
				// If the first marker is not at the start or if this is the very first time
				// through for this line, then save the text from the marker and
				// process the leading text (which may actually be an empty string if this
				// is a line beginning with a paragraph marker followed immediately by an
				// in-line marker).
				m_remainingLineText = source.Substring(startPos);
				source = source.Substring(0, startPos);
			}

			else
			{
				// An inline marker was found at the beginning of the line so process it now
				int endPos;
				string endMarker = FindNextMarker(source, startPos + startMarker.Length, out endPos);
				if (endMarker != null)
				{
					m_remainingLineText = source.Substring(endPos);
					int ichStartOfSegment = startPos + startMarker.Length;
					if (m_settings.ImportTypeEnum == TypeOfImport.Paratext5 && endPos > ichStartOfSegment)
					{
						// P5 in-line begin markers don't include the trailing space (for display
						// purposes), but it is required. Start markers do not end in "*", end
						// markers do.
						if (!startMarker.EndsWith("*"))
						{
							Debug.Assert(source[ichStartOfSegment] == ' ' || source[ichStartOfSegment] == '\t');
							ichStartOfSegment += 1;
						}
					}
					source = source.Substring(ichStartOfSegment, endPos - ichStartOfSegment);
				}
				else
				{
					source = source.Substring(startPos + startMarker.Length);
					if (startMarker == ScrImportSet.s_markerVerse)
					{
						// Found an inline verse number (probably Paratext data).
						BCVRef.VerseToScrRef(source.TrimStart(),
							out literalVerse, out source, ref m_currentStartRef, ref m_currentEndRef);
					}
					m_remainingLineText = string.Empty;
				}

				// For inline markers, get the mapping info
				markerMapping = ((ScrImportSet)m_settings).MappingForMarker(startMarker, m_mappingSet);
				marker = startMarker;
			}

			// Build a segment to return
			return new SCTextSegment(ConvertSource(source, markerMapping),
				marker, literalVerse, m_currentStartRef, m_currentEndRef,
				m_currentFile.FileName, m_lineNumber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an encoding converter for a mapping
		/// </summary>
		/// <param name="icuLocale">An ICU Locale</param>
		/// <param name="marker">The marker whose data is to be converted (used only for
		/// error reporting)</param>
		/// <returns>the encoding converter for the writing system of the mapping's ICU locale
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private IEncConverter GetEncodingConverterForLocale(string icuLocale, string marker)
		{
			// If an entry already exists, then just look it up and return it
			if (m_icuLocaleToConverterMap.ContainsKey(icuLocale))
				return m_icuLocaleToConverterMap[icuLocale];

			// If there is no entry, then find the writing system with the ICU locale, get its
			// converter name and get a converter for that name.
			ILgWritingSystem ws = new LgWritingSystem(Cache,
				Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(icuLocale));
			IEncConverter converter = null;
			if (ws.LegacyMapping != null)
			{
				try
				{
					converter = (IEncConverter)m_encConverters[ws.LegacyMapping];
				}
				catch
				{
					// Something bad happened... Try to carry on.
					// This is a workaround until TE-5068 is fixed.
					converter = null;
				}
				if (converter == null)
				{
					throw new EncodingConverterException(string.Format(ScriptureUtilsException.GetResourceString(
						"kstidEncConvMissingConverter"), m_lineNumber, m_currentFile.FileName,
						ws.LegacyMapping, marker),
						"/Beginning_Tasks/Import_Standard_Format/Unable_to_Import/Encoding_converter_not_found.htm");
				}
			}
			m_icuLocaleToConverterMap[icuLocale] = converter;
			return converter;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the encoding converter for the current domain
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IEncConverter GetEncodingConverterForMarkerDomain(ImportMappingInfo markerMapping)
		{
			Debug.Assert(markerMapping.IcuLocale == null);

			string icuLocale;

			switch (markerMapping.Domain & ~MarkerDomain.Footnote)
			{
				case MarkerDomain.Default:
					icuLocale = m_currentFile.IcuLocale;
					if (icuLocale == null) // Get the converter from default vern WS
					{
						icuLocale = (m_domain == ImportDomain.Main) ?
							Cache.LangProject.DefaultVernacularWritingSystemICULocale :
							Cache.LangProject.DefaultAnalysisWritingSystemICULocale;
					}
					break;

				case MarkerDomain.BackTrans:
				case MarkerDomain.Note:
					// Get the converter from default analysis WS
					icuLocale = (m_domain == ImportDomain.Main) ?
						Cache.LangProject.DefaultAnalysisWritingSystemICULocale :
						m_currentFile.IcuLocale;
					break;

				default:
					throw new ArgumentException(
						"Parameter markerMapping passed to GetEncodingConverterForMarkerDomain had invalid domain");
			}
			return GetEncodingConverterForLocale(icuLocale, markerMapping.BeginMarker);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert the source line into unicode if necessary
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string ConvertSource(string source, ImportMappingInfo markerMapping)
		{
			// If the file is already Unicode then no need to convert it.
			if (m_currentFile.FileEncoding != Encoding.ASCII)
				return source;

			IEncConverter dataEncoding;
			if (markerMapping == null)
			{
				// If the marker is not mapped, use the containing paragraph's encoding.
				dataEncoding = m_prevDataEncoding;
			}
			else
			{
				// book markers are a special case - do not convert them
				if (markerMapping.BeginMarker == ScrImportSet.s_markerBook)
					return source;

				if (markerMapping.IcuLocale != null)
					dataEncoding = GetEncodingConverterForLocale(markerMapping.IcuLocale,
						markerMapping.BeginMarker);
				else
				{
					// For paragraph styles or markers that do not map to existing styles,
					// or verse or chapter numbers or any default para char style that is
					// not inline get the encoder from the current domain. Otherwise use
					// the containing paragraph's encoder
					if (markerMapping.Style == null || markerMapping.IsParagraphStyle ||
						markerMapping.StyleName == ScrStyleNames.VerseNumber ||
						markerMapping.StyleName == ScrStyleNames.ChapterNumber ||
						(!markerMapping.IsInline && markerMapping.StyleName ==
						FdoResources.DefaultParaCharsStyleName))
					{
						dataEncoding = GetEncodingConverterForMarkerDomain(markerMapping);
					}
					else
						dataEncoding = m_prevDataEncoding;
				}

				// If the marker maps to a paragraph style or pertains to a non-default domain,
				// then save the encoder as the current paragraph encoder
				if (m_domain == ImportDomain.Main &&
					((markerMapping.StyleName != FdoResources.DefaultParaCharsStyleName &&
					(markerMapping.Style == null || markerMapping.IsParagraphStyle)) ||
					(markerMapping.Domain & ~MarkerDomain.Footnote) != MarkerDomain.Default))
				{
					m_prevDataEncoding = dataEncoding;
				}
			}

			// If an encoder was not found, then just return the text
			if (dataEncoding == null)
				return source;

			// get a converter and convert the text
			dataEncoding.CodePageInput = kMagicCodePage;
			if (source != string.Empty)
			{
				try
				{
					source = dataEncoding.Convert(source);
				}
				catch (Exception e)
				{
					throw new EncodingConverterException(
						string.Format(ScriptureUtilsException.GetResourceString(
						"kstidEncConverterError"), e.Message, dataEncoding.Name),
						"/Beginning_Tasks/Import_Standard_Format/Unable_to_Import/Encoding_conversion_failed.htm");
				}
			}

			return source;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the next marker in the string.
		/// </summary>
		/// <param name="source">source string to look in</param>
		/// <param name="start">starting position to search at</param>
		/// <param name="foundPos">returns the position where the marker was found (-1
		/// if the marker is not found)</param>
		/// <returns>the marker string found or null if none were found</returns>
		/// ------------------------------------------------------------------------------------
		private string FindNextMarker(string source, int start, out int foundPos)
		{
			// Starting at "start" look for the markers at each location in the
			// string that matches a start character
			int searchLocation = start;
			while (searchLocation < source.Length)
			{
				// Look for the next start character
				int startCharPos = source.IndexOfAny(m_InlineMarkerStartChars, searchLocation);
				if (startCharPos == -1)
					break;

				// make a substring starting at the search position so we can use
				// the StartsWith method to look for markers
				string sub = source.Substring(startCharPos);

				// Look at all of the inline markers to see if any of them occur
				// at this point of the text
				foreach (string marker in m_InlineBeginAndEndMarkers)
				{
					if (sub.StartsWith(marker))
					{
						foundPos = startCharPos;
						return marker;
					}
				}

				// If a marker was not found here, then move past the last found start
				// character to continue looking
				searchLocation = startCharPos + 1;
			}
			foundPos = -1;
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the next source file to read. Skip over any files that do not contain
		/// data in the desired range.
		/// </summary>
		/// <returns>true if a file was opened, else false and there are no more
		/// files available</returns>
		/// ------------------------------------------------------------------------------------
		private bool OpenNextSourceFile(BCVRef startRef, BCVRef endRef)
		{
			// Clear the indicator that tells that an ID line has been encountered
			m_seenIdInFile = false;
			m_lineNumber = 0;

			// If the first file is being opened, then initialize the reference
			if (m_currentFile == null)
			{
				m_currentStartRef = new BCVRef();
				m_currentEndRef = new BCVRef();
			}
			// Find the next book in the project that has books in the import range
			bool fileHasBooksInRange = false;
			while (!fileHasBooksInRange)
			{
				if (!m_importSourceEnum.MoveNext())
					return false;
				m_currentFile = (ScrImportFileInfo)m_importSourceEnum.Current;

				if (m_domain != ImportDomain.Main)
					m_prevDataEncoding = GetEncodingConverterForLocale(m_currentFile.IcuLocale,
						ScrImportSet.s_markerBook);

				// make sure the file contains books in the desired range
				foreach (int book in m_currentFile.BooksInFile)
				{
					if (book >= startRef.Book && book <= endRef.Book)
					{
						fileHasBooksInRange = true;
						break;
					}
				}
			}

			try
			{
				// ASCII encoding will convert all high-bit characters to a '?'.
				// The magic code page will map all high-bit characters to the same
				// value with a high-byte of 0 in unicode. For example, character 0x84
				// will end up as 0x0084.
				Encoding useEncoding = (m_currentFile.FileEncoding == Encoding.ASCII) ?
					Encoding.GetEncoding(kMagicCodePage) : m_currentFile.FileEncoding;

				m_sfFileReader = new StreamReader(m_currentFile.FileName, useEncoding);
			}
			catch (Exception e)
			{
				throw new ScriptureUtilsException(SUE_ErrorCode.FileError, m_currentFile.FileName, e);
			}
			return true;
		}
		#endregion
	}
}
