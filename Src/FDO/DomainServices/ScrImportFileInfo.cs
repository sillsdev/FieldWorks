// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScrImportFileInfo.cs
// Responsibility: TE Team

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	#region class ReferenceRange
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Maintains a start and end reference range.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ReferenceRange
	{
		/// <summary></summary>
		public int Book;
		/// <summary></summary>
		public int StartChapter;
		/// <summary></summary>
		public int EndChapter;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a reference range
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ReferenceRange(int book, int startChapter, int endChapter)
		{
			Book = book;
			StartChapter = startChapter;
			EndChapter = endChapter;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert the reference range to a string
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return ScrReference.NumberToBookCode(Book) + " " + StartChapter + "-" + EndChapter;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether this reference range overlaps the range specified by the given
		/// start and end reference
		/// </summary>
		/// <param name="start">Start reference</param>
		/// <param name="end">End Reference</param>
		/// <returns>true if there is any overlap</returns>
		/// ------------------------------------------------------------------------------------
		public bool OverlapsRange(ScrReference start, ScrReference end)
		{
			// If the book number is completely contained by the start and end reference
			// then it definitely overlaps.
			if (Book > start.Book && Book < end.Book)
				return true;

			int startChapter, endChapter;

			if (Book == start.Book)
			{
				startChapter = start.Chapter;
				endChapter = (start.Book == end.Book) ? end.Chapter : start.LastChapter;
			}
			else if (Book == end.Book)
			{
				startChapter = 1;
				endChapter = end.Chapter;
			}
			else
				return false;

			return (StartChapter <= endChapter && EndChapter >= startChapter);
		}
	}
	#endregion

	#region class ScrImportFileInfoFactory
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Factory to facilitate dependency injection for unit tests
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrImportFileInfoFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a <see cref="ScrImportFileInfo"/> based on a file. This is used to build an
		/// in-memory list of files.
		/// </summary>
		/// <param name="fileName">Name of the file whose info this represents</param>
		/// <param name="mappingList">Sorted list of mappings to which newly found mappings
		/// should (and will) be added</param>
		/// <param name="domain">The import domain to which this file belongs</param>
		/// <param name="wsId">The writing system identifier of the source to which this file
		/// belongs (null for Scripture source)</param>
		/// <param name="noteType">The CmAnnotationDefn of the source to which
		/// this file belongs (only used for Note sources)</param>
		/// <param name="scanInlineBackslashMarkers"><c>true</c> to look for backslash markers
		/// in the middle of lines. (Toolbox dictates that fields tagged with backslash markers
		/// must start on a new line, but Paratext considers all backslashes in the data to be
		/// SF markers.)</param>
		/// ------------------------------------------------------------------------------------
		public virtual IScrImportFileInfo Create(string fileName, ScrMappingList mappingList,
			ImportDomain domain, string wsId, ICmAnnotationDefn noteType,
			bool scanInlineBackslashMarkers)
		{
			return new ScrImportFileInfo(fileName, mappingList, domain, wsId, noteType,
				scanInlineBackslashMarkers);
		}
	}
	#endregion

	#region class ScrImportFileInfo
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Maintains information about a file in a project for importing
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrImportFileInfo : IScrImportFileInfo
	{
		#region Data members
		/// <summary></summary>
		protected bool m_doStrictFileChecking;
		private bool m_scanInlineBackslashMarkers;

		/// <summary>link to the CmFile in the project</summary>
		private ICmFile m_file;

		/// <summary>full path name of the file</summary>
		protected string m_fileName;

		/// <summary>The file encoding</summary>
		protected Encoding m_fileEncoding;

		/// <summary>A list of integers representing 1-based canonical book numbers that are
		/// in this file</summary>
		protected List<int> m_booksInFile = new List<int>();

		/// <summary>
		/// List of books (ReferenceRange objects) contained within this file, along with first and last chapter of each
		/// book. This is used for detecting content overlap between files.
		/// </summary>
		protected List<ReferenceRange> m_referenceRangeList = new List<ReferenceRange>(1);

		/// <summary>first reference encountered in the file</summary>
		protected ScrReference m_startRef = new ScrReference();

		private ScrMappingList m_mappingList;
		private ImportDomain m_domain;
		/// <summary>true if the file is accesible and readable</summary>
		protected bool m_isReadable;

		/// <summary>
		/// The writing system identifier of the source to which this file belongs (null for Scripture source)
		/// </summary>
		private string m_wsId;
		/// <summary>
		/// The CmAnnotationDefn of the source to which this file belongs
		/// (only used for Note sources)
		/// </summary>
		private ICmAnnotationDefn m_noteType;

		private char[] m_markerEndChars;
		#endregion

		#region constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a ScrImportFileInfo based on a Toolbox (i.e., non-Paratext 5) filename.
		/// This is used only in tests.
		/// </summary>
		/// <param name="fileName">Name of the file whose info this represents</param>
		/// <param name="mappingList">Sorted list of mappings to which newly found mappings
		/// should (and will) be added</param>
		/// <param name="domain">The import domain to which this file belongs</param>
		/// <param name="wsId">The writing system identifier of the source to which this file
		/// belongs (null for Scripture source)</param>
		/// <param name="noteType">The CmAnnotationDefn of the source to which
		/// this file belongs (only used for Note sources)</param>
		/// ------------------------------------------------------------------------------------
		internal ScrImportFileInfo(string fileName, ScrMappingList mappingList, ImportDomain domain,
			string wsId, ICmAnnotationDefn noteType)
			: this(fileName, mappingList, domain, wsId, noteType, false)
		{

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a ScrImportFileInfo based on a filename. This is used to build an
		/// in-memory list of files.
		/// </summary>
		/// <param name="fileName">Name of the file whose info this represents</param>
		/// <param name="mappingList">Sorted list of mappings to which newly found mappings
		/// should (and will) be added</param>
		/// <param name="domain">The import domain to which this file belongs</param>
		/// <param name="wsId">The writing system identifier of the source to which this file
		/// belongs (null for Scripture source)</param>
		/// <param name="noteType">The CmAnnotationDefn of the source to which
		/// this file belongs (only used for Note sources)</param>
		/// <param name="scanInlineBackslashMarkers"><c>true</c> to look for backslash markers
		/// in the middle of lines. (Toolbox dictates that fields tagged with backslash markers
		/// must start on a new line, but Paratext considers all backslashes in the data to be
		/// SF markers.)</param>
		/// ------------------------------------------------------------------------------------
		public ScrImportFileInfo(string fileName, ScrMappingList mappingList, ImportDomain domain,
			string wsId, ICmAnnotationDefn noteType, bool scanInlineBackslashMarkers)
		{
			m_fileName = fileName;
			m_mappingList = mappingList;
			m_domain = domain;
			m_wsId = wsId;
			m_noteType = noteType;
			m_doStrictFileChecking = false;
			ScanInlineBackslashMarkers = scanInlineBackslashMarkers;
			Initialize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a ScrImportFileInfo from a CmFile. This is used when populating the
		/// file list from the database.
		/// </summary>
		/// <param name="file">The CmFile</param>
		/// <param name="mappingList">List of mappings to which newly found mappings
		/// should (and will) be added</param>
		/// <param name="domain">The import domain to which this file belongs</param>
		/// <param name="wsId">The ICU locale of the source to which this file belongs
		/// (null for Scripture source)</param>
		/// <param name="noteType">The CmAnnotationDefn of the source to which
		/// this file belongs (only used for Note sources)</param>
		/// <param name="scanInlineBackslashMarkers"><c>true</c> to look for backslash markers
		/// in the middle of lines. (Toolbox dictates that fields tagged with backslash markers
		/// must start on a new line, but Paratext considers all backslashes in the data to be
		/// SF markers.)</param>
		/// ------------------------------------------------------------------------------------
		internal ScrImportFileInfo(ICmFile file, ScrMappingList mappingList, ImportDomain domain,
			string wsId, ICmAnnotationDefn noteType, bool scanInlineBackslashMarkers) :
			this(file.AbsoluteInternalPath, mappingList, domain, wsId, noteType,
			scanInlineBackslashMarkers)
		{
			m_file = file;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a ScrImportFileInfo from a CmFile owned by a Scripture source. This is
		/// used when populating the file list from the database.
		/// </summary>
		/// <param name="file">The CmFile</param>
		/// <param name="mappingList">list of mappings to which newly found mappings
		/// should (and will) be added</param>
		/// <param name="domain">The import domain to which this file belongs</param>
		/// <param name="scanInlineBackslashMarkers"><c>true</c> to look for backslash markers
		/// in the middle of lines. (Toolbox dictates that fields tagged with backslash markers
		/// must start on a new line, but Paratext considers all backslashes in the data to be
		/// SF markers.)</param>
		/// ------------------------------------------------------------------------------------
		internal ScrImportFileInfo(ICmFile file, ScrMappingList mappingList, ImportDomain domain,
			bool scanInlineBackslashMarkers) :
			this(file, mappingList, domain, null, null, scanInlineBackslashMarkers)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a ScrImportFileInfo from a CmFile owned by a BT source. This is
		/// used when populating the file list from the database.
		/// </summary>
		/// <param name="file">The CmFile</param>
		/// <param name="mappingList">Sorted list of mappings to which newly found mappings
		/// should (and will) be added</param>
		/// <param name="domain">The import domain to which this file belongs</param>
		/// <param name="wsId">The ICU locale of the source to which this file belongs
		/// (null for Scripture source)</param>
		/// <param name="scanInlineBackslashMarkers"><c>true</c> to look for backslash markers
		/// in the middle of lines. (Toolbox dictates that fields tagged with backslash markers
		/// must start on a new line, but Paratext considers all backslashes in the data to be
		/// SF markers.)</param>
		/// ------------------------------------------------------------------------------------
		internal ScrImportFileInfo(ICmFile file, ScrMappingList mappingList, ImportDomain domain,
			string wsId, bool scanInlineBackslashMarkers) :
			this(file, mappingList, domain, wsId, null, scanInlineBackslashMarkers)
		{
		}
		#endregion

		#region Public Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the starting reference for the file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrReference StartRef
		{
			get { return new ScrReference(m_startRef); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string FileName
		{
			get
			{
				// TE-8713: Need to take into account that NFD internal form may need to be changed
				return FileUtils.ActualFilePath(m_fileName);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a List of integers representing 1-based canonical book numbers that are
		/// in this file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> BooksInFile
		{
			get { return m_booksInFile; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system identifier of the source to which this file belongs (null
		/// for Scripture source)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string WsId
		{
			get { return m_wsId; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the CmAnnotationDefn of the source to which this file belongs
		/// (only used for Note sources)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ICmAnnotationDefn NoteType
		{
			get { return m_noteType; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the file encoding
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Encoding FileEncoding
		{
			get { return m_fileEncoding; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of references for the file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ReferenceRange[] BookReferences
		{
			get
			{
				return m_referenceRangeList.ToArray();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a reference map to a string that indicates the reference range
		/// covered by the map
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ReferenceRangeAsString
		{
			get
			{
				StringBuilder refString = new StringBuilder();
				foreach (ReferenceRange range in BookReferences)
				{
					if (refString.Length > 0)
						refString.Append(", ");
					refString.Append(range.ToString());
				}
				return refString.ToString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this file is still readable.
		/// </summary>
		/// <remarks>Use this property to check accessibility for the first time or to recheck
		/// accessibility of a file that was initially accessible. Use <seealso cref="IsReadable"/>
		/// to access the cached value. Use <seealso cref="RecheckAccessibility"/> to recheck
		/// accessibility of a file that may have been initially determined to be inaccessible.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool IsStillReadable
		{
			get
			{
				m_isReadable = FileUtils.IsFileReadable(FileName);
				if (!m_isReadable)
				{
					// If the file is not accessible then there is no way to know what the
					// encoding is so just assume ASCII.
					m_fileEncoding = Encoding.ASCII;
				}
				return m_isReadable;
			}
		}
		#endregion

		#region Private Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the m_scanInlineBackslashMarkers (sets m_markerEndChars as a side-effect)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ScanInlineBackslashMarkers
		{
			set
			{
				m_scanInlineBackslashMarkers = value;
				if (m_scanInlineBackslashMarkers)
					m_markerEndChars = new char[] { ' ', '*', '\t' };
				else
					m_markerEndChars = new char[] { ' ', '\t' };
			}
		}
		#endregion

		#region Helper methods and properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the file info
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void Initialize()
		{
			// If the file is readable, then determine the encoding and get the mappings.
			if (IsStillReadable)
			{
				try
				{
					GuessFileEncoding();
					// Search the file for markers, add the markers to the project,
					// and build the references for the file.
					GetMappingsFromFile();
				}
				catch (IOException e)
				{
					// Report file errors
					throw new ScriptureUtilsException(SUE_ErrorCode.FileError, m_fileName, e);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Guesses the file encoding of the source file (virtual for testing)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void GuessFileEncoding()
		{
			m_fileEncoding = FileUtils.DetermineSfFileEncoding(FileName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Rechecks the accessibility of a file that might have been initially determined to
		/// be inaccessible. If the file was inaccessible but is now accessible, it will be
		/// properly initialized so all the cached info will be valid.
		/// </summary>
		/// <remarks>Use <seealso cref="IsStillReadable"/> to recheck accessibility of a file
		/// that was initially determined to be accessible. Use <seealso cref="IsReadable"/> to
		/// access the cached value.</remarks>
		/// <returns><c>true</c> if the file is currently accessible</returns>
		/// ------------------------------------------------------------------------------------
		internal bool RecheckAccessibility()
		{
			if (IsReadable)
				return IsStillReadable;
			Initialize();
			return IsReadable;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-scan files for mappings. This is mainly to enable us to look for additional
		/// P5-style in-line markers that were not detected previously when the import type is
		/// changing.
		/// </summary>
		/// <param name="scanInlineBackslashMarkers"><c>true</c> to look for backslash markers
		/// in the middle of lines. (Toolbox dictates that fields tagged with backslash markers
		/// must start on a new line, but Paratext considers all backslashes in the data to be
		/// SF markers.)</param>
		/// ------------------------------------------------------------------------------------
		public void Rescan(bool scanInlineBackslashMarkers)
		{
			ScanInlineBackslashMarkers = scanInlineBackslashMarkers;
			Rescan();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-scan files for mappings. This enables us to look for additional markers that were
		/// not detected previously and to detect bogus markers when m_doStrictScan is true.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void Rescan()
		{
			if (m_isReadable)
			{
				try
				{
					// Search the file for markers, add the markers to the project,
					// and build the references for the file.
					GetMappingsFromFile();
				}
				catch (IOException e)
				{
					// Report file errors
					throw new ScriptureUtilsException(SUE_ErrorCode.FileError, m_fileName, e);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs a strict scan.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PerformStrictScan()
		{
			m_doStrictFileChecking = true;
			Rescan();
			m_doStrictFileChecking = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether the file is readable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsReadable
		{
			get { return m_isReadable; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the file to build mappings of the markers found
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void GetMappingsFromFile()
		{
			using (var reader = GetReader())
			{
				GetMappingsFromStream(reader);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a reader for the file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual TextReader GetReader()
		{
			// ASCII encoding will convert all high-bit characters to a '?'.
			// The magic code page will map all high-bit characters to the same
			// value with a high-byte of 0 in Unicode. For example, character 0x84
			// will end up as 0x0084.
			Encoding useEncoding = (m_fileEncoding == Encoding.ASCII) ?
				Encoding.GetEncoding(EncodingConstants.kMagicCodePage) : m_fileEncoding;
			return FileUtils.OpenFileForRead(FileName, useEncoding);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the file to build mappings of the markers found
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void GetMappingsFromStream(TextReader reader)
		{
			string lineIn;

			int chapter = -1;
			int book = -1;
			int lineCount = 0;

			// book and chapter strings for reporting info in exceptions
			string sBookId = null;
			string sChapter = null;
			string sVerse = null;

			// Keep track of the first reference in the file
			int firstBook = -1;
			int firstChapter = -1;
			int firstVerse = -1;

			ReferenceRange currentRange = null;
			string marker;
			string lineText;
			string nextLineText = null; // used for read-ahead for \fig line when doing strict scanning

			while ((lineIn = reader.ReadLine()) != null)
			{
				lineCount++;
				while (GetNextMarkerFromData(lineIn, out marker, out lineText))
				{
					// Make sure the marker is valid
					if (!IsValidMarker(marker))
					{
						throw new ScriptureUtilsException(SUE_ErrorCode.InvalidCharacterInMarker,
							FileName, lineCount, lineIn, sBookId, sChapter, sVerse);
					}

					ImportMappingInfo markerMapping = GetOrCreateMarkerMapping(ref marker);

					if (marker == ScrMappingList.MarkerBook)
					{
						sBookId = lineText.TrimStart().ToUpperInvariant();

						// save the book number in the list for this file
						book = ScrReference.BookToNumber(sBookId);
						if (book <= 0)
							throw new ScriptureUtilsException(SUE_ErrorCode.InvalidBookID, FileName,
								lineCount, lineIn, sBookId, null, null);
						sBookId = ScrReference.NumberToBookCode(book);

						// Make a new reference range with the book id and
						// start it out with chapter range of 0-0.
						AddRangeToList(currentRange);
						currentRange = new ReferenceRange(book, 0, 0);

						// If this is the first book, remember it
						if (firstBook == -1)
							firstBook = book;
						m_booksInFile.Add(book);
						chapter = -1;
					}
					else
					{
						// make sure that a book has been started before seeing any non-excluded markers
						// This error is a "strict" error because files can be added by a user before there
						// is a chance to exclude markers in the mappings. When the file is added from the settings
						// for import, then strict checking will be on.
						if (book == -1 && m_doStrictFileChecking)
						{
							// if the marker is not excluded then throw an error
							if (markerMapping != null && !markerMapping.IsExcluded)
							{
								throw new ScriptureUtilsException(SUE_ErrorCode.UnexcludedDataBeforeIdLine,
									FileName, lineCount, lineIn, null, null, null);
							}
						}

						if (marker == ScrMappingList.MarkerChapter)
						{
							// If there is no book, then throw an error since chapter numbers
							// are not valid without a book
							if (book == -1)
							{
								throw new ScriptureUtilsException(SUE_ErrorCode.ChapterWithNoBook,
									FileName, lineCount, lineIn, null, null, null);
							}
							try
							{
								sChapter = lineText;
								chapter = ScrReference.ChapterToInt(sChapter);

								// save the chapter number as the last chapter and possibly the first
								// chapter number in the range.
								if (currentRange.StartChapter == 0)
									currentRange.StartChapter = chapter;
								currentRange.EndChapter = chapter;
							}
							catch (ArgumentException)
							{
								throw new ScriptureUtilsException(SUE_ErrorCode.InvalidChapterNumber,
									FileName, lineCount, lineIn, sBookId, sChapter, null);
							}
							// If this is the first chapter, remember it
							if (firstChapter == -1)
								firstChapter = chapter;
						}

						else if (marker == ScrMappingList.MarkerVerse)
						{
							// If a verse is seen without a book, throw an exception
							if (book == -1)
							{
								throw new ScriptureUtilsException(SUE_ErrorCode.VerseWithNoBook,
									FileName, lineCount, lineIn, sBookId, null, lineText);
							}

							BCVRef firstRef = new BCVRef(book, chapter, 0);
							BCVRef lastRef = new BCVRef(book, chapter, 0);

							// check for an invalid verse number
							if (!BCVRef.VerseToScrRef(lineText, ref firstRef, ref lastRef) ||
								firstRef.Verse == 0 || firstRef.Verse > lastRef.Verse)
							{
								throw new ScriptureUtilsException(SUE_ErrorCode.InvalidVerseNumber,
									FileName, lineCount, lineIn, sBookId, sChapter, lineText);
							}

							// If a chapter number has not been seen yet, then throw an exception
							sVerse = firstRef.Verse.ToString();
							if (chapter == -1 && !SingleChapterBook(book))
							{
								throw new ScriptureUtilsException(SUE_ErrorCode.MissingChapterNumber,
									FileName, lineCount, lineIn, sBookId, null, sVerse);
							}

							// If this is the first verse, remember it
							if (firstVerse == -1)
								firstVerse = firstRef.Verse;
						}
						else if (!markerMapping.IsExcluded && m_doStrictFileChecking &&
							markerMapping.MappingTarget == MappingTargetType.Figure)
						{
							// First, we need to consider whether any following lines also need
							// to be read in, since the Figure parameters could be split across
							// lines. (TE-7669)
							Debug.Assert(nextLineText == null);
							int cExtraLinesRead = 0;
							string sTempMarker, sTempLineText;
							if (!GetNextMarkerFromData(lineText, out sTempMarker, out sTempLineText))
							{
								while ((nextLineText = reader.ReadLine()) != null)
								{
									cExtraLinesRead++;
									if (GetNextMarkerFromData(nextLineText, out sTempMarker, out sTempLineText))
									{
										// Normally, we want to break the line right before the first marker.
										int ichMarkerPos = nextLineText.IndexOf(sTempMarker);
										// But if it's a \fig*, break after the marker.
										if (sTempMarker == markerMapping.EndMarker)
											ichMarkerPos += sTempMarker.Length;
										lineText += " " + nextLineText.Substring(0, ichMarkerPos);
										nextLineText = nextLineText.Substring(ichMarkerPos);
										break;
									}
									else
										lineText += " " + nextLineText;
								}
							}

							string figureParams = lineText;
							int endMarkerLength = 0;
							// Validate the tokens for a mapping target (only in strict checking)
							if (!String.IsNullOrEmpty(markerMapping.EndMarker))
							{
								endMarkerLength = markerMapping.EndMarker.Length;
								int ichEnd = figureParams.IndexOf(markerMapping.EndMarker);
								if (ichEnd >= 0)
									figureParams = figureParams.Substring(0, ichEnd);
								else
									endMarkerLength = 0; // end marker is optional and not present
							}
							string[] tokens = figureParams.Split('|');
							if (tokens.Length < 6)
							{
								throw new ScriptureUtilsException(SUE_ErrorCode.BadFigure, FileName,
									lineCount, lineIn, sBookId, sChapter, sVerse);
							}
							lineText = lineText.Substring(figureParams.Length + endMarkerLength);
							lineCount += cExtraLinesRead;
						}
					}
					// Mark this mapping as "in-use" because it was found in the scanned file
					markerMapping.SetIsInUse(m_domain, m_wsId, m_noteType, true);

					if (m_scanInlineBackslashMarkers)
						lineIn = lineText;
					else
					{
						lineIn = nextLineText;
						nextLineText = null;
						if (lineIn == null)
							break;
					}
					if (string.IsNullOrEmpty(lineIn) && !string.IsNullOrEmpty(nextLineText))
					{
						lineIn = nextLineText;
						nextLineText = null;
					}
				}
			}
			// Add the last range to the list
			AddRangeToList(currentRange);

			// If no books were found in the file then throw an exception
			if (book == -1)
				throw new ScriptureUtilsException(SUE_ErrorCode.MissingBook,
					FileName, lineCount, null, null, null, null);


			// If no chapters were found then throw an exception
			if (chapter == -1 && !SingleChapterBook(book))
				throw new ScriptureUtilsException(SUE_ErrorCode.NoChapterNumber,
					FileName, lineCount, null, sBookId, null, null);

			// Store the first reference for the file
			m_startRef.Book = firstBook;
			m_startRef.Chapter = firstChapter == -1 ? 1 : firstChapter;
			m_startRef.Verse = firstVerse == -1 ? 1 : firstVerse;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or creates a mapping for the given marker.
		/// </summary>
		/// <param name="marker">The marker.</param>
		/// ------------------------------------------------------------------------------------
		private ImportMappingInfo GetOrCreateMarkerMapping(ref string marker)
		{
			ImportMappingInfo markerMapping;
			if (m_scanInlineBackslashMarkers)
			{
				bool fMapEndMarker = false;
				if (marker.EndsWith("*"))
				{
					string beginMarker = marker.Substring(0, marker.Length - 1);
					if (m_mappingList[beginMarker] != null)
					{
						marker = beginMarker;
						fMapEndMarker = true;
					}
				}
				else if (m_mappingList[marker + "*"] != null)
				{
					// If the corresponding end marker is present in the mapping list
					// (as a begin marker), we need to remove it and instead map it as
					// an end marker. This is unlikely, but can happen if the data has
					// an errant end marker before the begin marker.
					m_mappingList.Delete(m_mappingList[marker + "*"]);
					fMapEndMarker = true;
				}
				markerMapping = m_mappingList.AddDefaultMappingIfNeeded(marker, m_domain, true);

				if (fMapEndMarker)
					markerMapping.EndMarker = marker + "*";
			}
			else
				markerMapping = m_mappingList.AddDefaultMappingIfNeeded(marker, m_domain, true);
			return markerMapping;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Look for a SF marker on the line. If found, split the line into marker and text.
		/// </summary>
		/// <param name="lineIn">Incoming line</param>
		/// <param name="marker">SF marker (output)</param>
		/// <param name="lineText">Rest of the line</param>
		/// <returns><c>true</c> if a marker is found; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		protected bool GetNextMarkerFromData(string lineIn, out string marker, out string lineText)
		{
			if (lineIn == string.Empty || lineIn[0] != '\\')
			{
				if (m_scanInlineBackslashMarkers)
				{
					int ich = lineIn.IndexOf('\\');
					if (ich >= 0)
					{
						SplitLine(lineIn.Substring(ich), out marker, out lineText);
						return true;
					}
				}
				marker = lineText = null;
				return false;
			}
			SplitLine(lineIn, out marker, out lineText);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Split a line into the marker and the remaining portion
		/// </summary>
		/// <param name="line"></param>
		/// <param name="marker"></param>
		/// <param name="lineText"></param>
		/// ------------------------------------------------------------------------------------
		private void SplitLine(string line, out string marker, out string lineText)
		{
			int ichSplit = line.IndexOfAny(m_markerEndChars);
			if (ichSplit < 0)
				ichSplit = line.Length;
			else if (line[ichSplit] == '*')
				ichSplit++;

			if (ichSplit >= line.Length)
			{
				marker = line;
				lineText = string.Empty;
			}
			else
			{
				marker = line.Substring(0, ichSplit);
				lineText = line.Substring(ichSplit + 1);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a reference range to the list if there is one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddRangeToList(ReferenceRange currentRange)
		{
			if (currentRange != null)
			{
				// If there were no chapters found, then assume chapter 1.
				if (currentRange.StartChapter == 0)
					currentRange.StartChapter = currentRange.EndChapter = 1;
				m_referenceRangeList.Add(currentRange);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the book is one that only has a single chapter
		/// </summary>
		/// <param name="book"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool SingleChapterBook(int book)
		{
			return VersificationTable.Get(ScrVers.English).LastChapter(book) == 1;
		}
		#endregion

		#region Public Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if a marker has only valid characters in it
		/// </summary>
		/// <param name="sMarker">marker to check</param>
		/// <returns>true for valid marker, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsValidMarker(string sMarker)
		{
			foreach (char ch in sMarker)
			{
				if ((int)ch > 126 || (int)ch < 32)
					return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check two ScrImportFileInfo objects for overlap.
		/// </summary>
		/// <param name="map1">first ScrImportFileInfo to compare</param>
		/// <param name="map2">second ScrImportFileInfo to compare</param>
		/// <returns>true if an overlap exists, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool CheckForOverlap(IScrImportFileInfo map1, IScrImportFileInfo map2)
		{
			foreach (ReferenceRange range1 in map1.BookReferences)
			{
				foreach (ReferenceRange range2 in map2.BookReferences)
				{
					if (range1.Book == range2.Book)
					{
						if (range1.StartChapter >= range2.StartChapter && range1.StartChapter <= range2.EndChapter)
							return true;
						if (range2.StartChapter >= range1.StartChapter && range2.StartChapter <= range1.EndChapter)
							return true;
					}
				}
			}
			return false;
		}
		#endregion
	}
	#endregion
}
