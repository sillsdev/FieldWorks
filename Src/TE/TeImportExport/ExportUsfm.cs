// ---------------------------------------------------------------------------------------------
// Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2004' to='2006' company='SIL International'>
//	Copyright (c) 2006, SIL International. All Rights Reserved.
//
//	Distributable under the terms of either the Common Public License or the
//	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
//
// File: ExportUsfm.cs
// Responsibility: TeTeam
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Controls;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.TE
{
	#region MarkupType enumeration
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Values for the SF markup system used for an export.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum MarkupType
	{
		/// <summary>
		/// Paratext markup:
		///	verse text is on the line with the verse marker and number:
		///		\v 1 the verse text goes here
		///	paragraph text will appear on a line with the paragraph marker:
		///		\q paragraph text goes here
		/// </summary>
		Paratext,

		/// <summary>
		/// Toolbox/Shoebox markup:
		///	verse number lines never have text on them:
		///		\v 1
		///		\vt the text goes here
		///	paragraph markers are on a line by themselves, in content paragraphs:
		///		\q
		///		\vt paragraph text goes here
		///	in heading paragraphs, text will appear on a line with the paragraph marker:
		///		\s heading text goes here
		///	Interleaved back translations are allowed as an option
		/// </summary>
		Toolbox
	}
	#endregion

	#region RefElement enumeration
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Values used for controlling export of BT and Notes elements when their corresponding
	/// Scripture references are missing.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum RefElement
	{
		/// <summary>No element</summary>
		None,
		/// <summary>Scripture Book element</summary>
		Book,
		/// <summary>Chapter element</summary>
		Chapter,
		/// <summary>Verse element</summary>
		Verse,
	}
	#endregion

	#region ChapterVerseInfo class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Information about a chapter or verse number found in a back translation ITsString, and
	/// the text that follows the number.
	/// Used by ExportUsfm's ParseBackTranslationVerses method.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ChapterVerseInfo
	{
		/// <summary>text of chapter or verse number string (eg 2 or 2-3 or 2c)</summary>
		public string NumberString;
		/// <summary> min run index of the verse text in the ITsString</summary>
		public int iRunMinText;
		/// <summary> lim run index of the verse text in the ITsString</summary>
		public int iRunLimText;

		private RefElement m_type; //see Type property
		private bool m_matched; //see Matched property

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for a ChapterVerseInfo object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ChapterVerseInfo(RefElement type, string sNumber, int iRunMin, int iRunLim)
		{
			Type = type;
			NumberString = sNumber;
			iRunMinText = iRunMin;
			iRunLimText = iRunLim;
			m_matched = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>The type of chapter/verse info.</summary>
		/// ------------------------------------------------------------------------------------
		public RefElement Type
		{
			get { return m_type; }
			set { m_type = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/Set the matched state. A BT verse info is marked as "matched" when it matches
		/// a chapter or verse run in the vernacular.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Matched
		{
			get { return m_matched; }
			set { m_matched = value; }
		}
	}
	#endregion

	#region FileWriter class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class is used to write the output file. It handles trimming spaces from the
	/// end of lines and possibly other stuff that may be needed. It also will not allow
	/// blank lines.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FileWriter : IFWDisposable
	{
		private StreamWriter m_out = null;
		private StringBuilder m_buffer = new StringBuilder(256);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the output file.
		/// </summary>
		/// <param name="fileName"></param>
		/// ------------------------------------------------------------------------------------
		public virtual void Open(string fileName)
		{
			CheckDisposed();

			m_out = File.CreateText(fileName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Output a composed string to a file.
		/// </summary>
		/// <param name="outString"></param>
		/// ------------------------------------------------------------------------------------
		public virtual void Write(string outString)
		{
			CheckDisposed();

			m_buffer.Append(StringUtils.Compose(outString));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Output a composed string to the file (if outString is not empty) and end the line.
		/// </summary>
		/// <param name="outString">The string to write out</param>
		/// ------------------------------------------------------------------------------------
		public virtual void WriteLine(string outString)
		{
			CheckDisposed();

			WriteLine(StringUtils.Compose(outString), false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes a line of composed text, writing a blank line outsString is empty and if
		/// outputBlank is set to <c>true</c>.
		/// </summary>
		/// <param name="outString">The string to write out</param>
		/// <param name="outputBlank">if set to <c>true</c> output a blank line if outString
		/// is empty</param>
		/// ------------------------------------------------------------------------------------
		public virtual void WriteLine(string outString, bool outputBlank)
		{
			CheckDisposed();

			Write(outString);

			// If outputBlank is false, only write the line if there is data -
			// this will discard empty lines.
			if (m_buffer.Length != 0)
			{
				m_out.WriteLine(m_buffer.ToString().TrimEnd());
				m_buffer = new StringBuilder(256);
			}
			// However, the outputBlank flag is used because sometimes we want to format the
			// output files to be human readable (i.e. put an empty line between marker
			// definitions in paratext .sty files, etc.)
			else if (outputBlank)
			{
				m_out.WriteLine();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// end the file line.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void WriteLine()
		{
			CheckDisposed();

			WriteLine(string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close the file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void Close()
		{
			CheckDisposed();

			// This is copied from the MS StreamWriter class.
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a boolean reflecting if the next output would go at the start of a new line.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool AtStartOfLine
		{
			get
			{
				CheckDisposed();
				return m_buffer.Length == 0;
			}
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~FileWriter()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				// NB: This must NOT be done outside of the disposing == true context,
				// since the required StringBuilder may have been finalized already.
				if (m_out != null) // Finish writing it out, first.
				{
					WriteLine();
					m_out.Dispose();
					m_out = null;
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides export of data to a USFM format file, either Paratext or Toolbox/Shoebox
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ExportUsfm : IFWDisposable
	{
		private enum ExportMode
		{
			VernacularOnly,
			BackTransOnly,
			InterleavedVernAndBt,
		}

		#region Member variables
		private FdoCache m_cache;
		private IScripture m_scr;
		/// <summary></summary>
		protected FilteredScrBooks m_bookFilter;
		bool m_overwriteAll = false;
		private List<IScrScriptureNote> m_annotationList;
		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;
		private IApp m_app;

		private MarkupType m_markupSystem;
		private bool m_exportScripture = false;
		private bool m_exportBackTrans = false;
		private bool m_exportNotes = false;
		private string m_p6ShortName = null;
		private string m_paratextProjFolder = null;
		private bool m_exportParatextProjectFiles = false;
		/// <summary>List of (unique) pathnames to (internal copies) of picture files to copy if
		/// Paratext Project files are being created.</summary>
		protected List<string> m_pictureFilesToCopy;
		/// <summary>The default Analysis WS</summary>
		protected int m_defaultAnalWS;
		/// <summary>
		/// array of requested analysis writing systems for export of BT
		/// </summary>
		protected int[] m_requestedAnalWS;
		// array of ICULocale names for the requested analysis writing systems
		private string[] m_icuLocales;
		/// <summary>
		/// if exporting BT only (i.e. NOT interleaved), the WS we will export
		/// </summary>
		protected int m_nonInterleavedBtWs = -1;

		/// <summary></summary>
		protected FileWriter m_file;
		private FileNameFormat m_fileNameFormat;
		private string m_outputSpec;
		private bool m_splitByBook = false;
		private SortedDictionary<string, string> m_markerMappings;
		private List<string> m_footnoteContentMarkers;
		/// <summary>Used to handle accessing Paratext style (sty) files</summary>
		protected UsfmStyFileAccessor m_UsfmStyFileAccessor;
		/// <summary>Used to handle accessing Paratext project (ssf) files</summary>
		protected ParatextSsfFileAccessor m_ParatextSsfFileAccessor;

		/// <summary>Needs to be a member to support testing</summary>
		protected FileWriter m_fileSty;
		/// <summary>
		/// current book, chapter, and verse: for generating \vref fields, for matching annotations
		/// </summary>
		protected string m_currentBookCode;
		/// <summary>See above</summary>
		protected int m_currentBookOrd;
		/// <summary>The vernacular abbrev of the current book if known; otherwise, the SIL 3-letter code</summary>
		protected string m_currentBookAbbrev;
		/// <summary>current chapter number</summary>
		protected int m_currentChapterRef;
		//		protected int m_currentEndVerseRef;

		/// <summary>
		/// keep track of the last \v verse number field written
		/// m_currentVerseNumString shows bridges, segements, etc; null until verse number run encountered within a chapter
		/// </summary>
		protected string m_currentVerseNumString;
		/// <summary>See above</summary>
		protected int m_lastNumericBeginVerseNum;//not useful
		/// <summary>See above</summary>
		protected int m_lastNumericEndVerseNum;

		// keep track of the last \c chapter number field written
		private int m_lastChapterWritten;
		private string m_lastInvalidChapterWritten; //null if it was a valid chapter num

		/// <summary>keep info about the paragraph now being exported</summary>
		protected bool m_currentSectionIsIntro;
		/// <summary></summary>
		protected bool m_currentParaIsHeading;
		/// <summary>flag: output of real text (not chapter/verse nums) from section content has begun in this section</summary>
		protected bool m_textOutputBegunInCurrentSection;
		/// <summary>keep track of whether an implicit verse 1 should be output when appropriate</summary>
		private bool m_v1NeededForImplicitFirstVerse;

		private const char kRtlMark = '\u200f';
		private const string ksDefaultFootnoteCharacters = "DefaultFootnoteCharacters";

		// Special markers that we use for Toolbox markup
		// use this as the record marker
		private const string kRecordMarker = @"\rcrd ";
		// the verseref marker; this field is used by some checking programs we think
		private const string kVerseRefMarker = @"\vref";
		// kVerseTextMarker is used after \v (or \c) and number, and after char style ends, in body
		private const string kVerseTextMarker = @"\vt";
		// kNoCharStyleMarker is used after char style ends, in heading or title
		private const string kNoCharStyleMarker = @"\vt"; //use vt unless we find something else is better
		// all interleaved back translation fields will use this marker prefix
		private const string kBackTransMarkerPrefix = @"\bt";
		// kBackTransVerseTextMarkeris used after \v (or \c) and number, and after char style ends, in body
		private readonly string kBackTransVerseTextMarker = kBackTransMarkerPrefix + kVerseTextMarker.Substring(1);
		// use this for back translation verse numbers that don't match the vernacular verse numbers
		private const string kBackTransVerseAltMarker = @"\btva";

		// annotations use this marker
		private const string kAnnotationMarker = @"\rem";

		// This must be disposed of properly as a COM object.
		private ILgCharacterPropertyEngine m_cpe = null;

		// Flags so that only the first section of intro and Scripture sections are found.
		private bool m_fFoundFirstIntroSection = false;
		private bool m_fFoundFirstScrSection = false;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ExportUsfm"/> class.
		/// </summary>
		/// <param name="cache">FDO cache to use for export</param>
		/// <param name="filter">book filter to determine which books to export</param>
		/// <param name="outputFolder">Folder name to export to</param>
		/// <param name="app">The application.</param>
		/// <param name="fileNameFormat">The file name format.</param>
		/// ------------------------------------------------------------------------------------
		public ExportUsfm(FdoCache cache, FilteredScrBooks filter, string outputFolder,
			IApp app, FileNameFormat fileNameFormat)
			: this(cache, filter, outputFolder, app)
		{
			m_splitByBook = true;
			m_fileNameFormat = fileNameFormat;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ExportUsfm"/> class.
		/// </summary>
		/// <param name="cache">FDO cache to use for export</param>
		/// <param name="filter">book filter to determine which books to export</param>
		/// <param name="outputFile">File name or folder to export to</param>
		/// <param name="app">The application.</param>
		/// ------------------------------------------------------------------------------------
		public ExportUsfm(FdoCache cache, FilteredScrBooks filter, string outputFile, IApp app)
		{
			m_outputSpec = outputFile;
			m_file = new FileWriter();
			m_cache = cache;
			m_bookFilter = filter;
			m_app = app;
			m_scr = cache.LangProject.TranslatedScriptureOA;

			// By default, only the default analysis writing system will be included in the
			// requested set of back translations.
			m_defaultAnalWS = m_cache.DefaultAnalWs;
			m_icuLocales = new[] { m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.IcuLocale };

			// Read the TeStyles.xml file and create a hash table of style name to
			// USFM markers
			m_markerMappings = new SortedDictionary<string, string>();
			m_footnoteContentMarkers = new List<string>();

			m_ParatextSsfFileAccessor = new ParatextSsfFileAccessor(cache, filter);
		}
		#endregion

		#region Public Methods and Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Run the export
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Run()
		{
			CheckDisposed();

			bool[] writeFiles = new bool[m_bookFilter.BookCount];
			if (m_splitByBook)
			{
				for (int bookIndex = 0; bookIndex < m_bookFilter.BookCount; bookIndex++)
				{
					writeFiles[bookIndex] = true;
					string filename = GetExportFileSpec(bookIndex);

					// if user hasn't indicated they want to overwrite the file and the file exists...
					if (!m_overwriteAll && File.Exists(filename))
					{
						// and there is more than one book to export...
						if (m_bookFilter.BookCount > 1)
						{
							// determine options for overwriting file(s).
							using (FilesOverwriteDialog dlg = new FilesOverwriteDialog(filename, m_app.ApplicationName))
							{
								DialogResult result = dlg.ShowDialog();
								switch (result)
								{
									// overwrite all files ("Yes to all")
									case DialogResult.OK:
										// Don't set m_overwriteAll here since that would also overwrite
										// the files the user already said not to overwrite.
										for (int i = bookIndex; i < m_bookFilter.BookCount; i++)
											writeFiles[i] = true;
										bookIndex = m_bookFilter.BookCount;
										break;
									// overwrite current file
									case DialogResult.Yes:
										writeFiles[bookIndex] = true;
										break;
									// do not overwrite this file
									case DialogResult.No:
										writeFiles[bookIndex] = false;
										break;
									// do not overwrite any file ("No to All")
									case DialogResult.Cancel:
									default:
										for (int i = bookIndex; i < m_bookFilter.BookCount; i++)
											writeFiles[i] = false;
										bookIndex = m_bookFilter.BookCount;
										break;
								}
							}
						}
						else // only one file: use simple confirmation dialog
						{
							DialogResult result = MessageBox.Show(Form.ActiveForm,
								m_outputSpec + Environment.NewLine + TeResourceHelper.GetResourceString("kstidFileExists"),
								"", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
							writeFiles[bookIndex] = (result == DialogResult.Yes);
						}
					}
				}
			}
			else
			{
				if (File.Exists(m_outputSpec))
				{
					// File exists. Determine if user wants to overwrite it.
					DialogResult result = MessageBox.Show(Form.ActiveForm,
						m_outputSpec + Environment.NewLine + TeResourceHelper.GetResourceString("kstidFileExists"),
						"", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
					if (result != DialogResult.Yes)
						return;
				}
			}

			using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(Form.ActiveForm))
			{
				try
				{
					// count up the number of sections in the filtered books so the progress bar
					// can increment once for each section
					int sectionCount = 0;
					for (int i = 0; i < m_bookFilter.BookCount; i++)
					{
						if (writeFiles[i] || !m_splitByBook)
							sectionCount += m_bookFilter.GetBook(i).SectionsOS.Count;
					}
					progressDlg.Minimum = 0;
					progressDlg.Maximum = sectionCount + 1;
					progressDlg.Title = DlgResources.ResourceString("kstidExportUSFMProgress");
					progressDlg.CancelButtonVisible = true;

					progressDlg.RunTask(true, Export, writeFiles);
				}
				catch (WorkerThreadException e)
				{
					if (e.InnerException is InvalidOrMissingStyFileException)
					{
						string msg = string.Format(
							TeResourceHelper.GetResourceString("kstidExportUsfmErrorTryReinstall"),
							e.InnerException.Message);
						MessageBox.Show(msg, m_app.ApplicationName, MessageBoxButtons.OK,
							MessageBoxIcon.Error);
					}
					else if (e.InnerException is UnauthorizedAccessException)
					{
						string msg = string.Format(TeResourceHelper.GetResourceString("kstidExportUsfmFileError"),
							e.InnerException.Message);
						MessageBox.Show(msg, m_app.ApplicationName, MessageBoxButtons.OK,
							MessageBoxIcon.Error);
					}
					else
					{
						throw new ContinuableErrorException("Unknown exception during export.", e);
					}
				}
				finally
				{
					CloseFile();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CloseFile()
		{
			if (m_file != null)
			{
				try
				{
					m_file.Close();
				}
				catch
				{
					// ignore errors on close
				}
			}
			m_file = new FileWriter();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exports to USFM.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters: List of file indexes to write (if we are
		/// splitting by book)</param>
		/// <returns>Always null.</returns>
		/// ------------------------------------------------------------------------------------
		private object Export(IProgress progressDlg, object[] parameters)
		{
			Debug.Assert(parameters.Length == 1);
			bool[] writeFiles = (bool[])parameters[0];
			CreateStyleTables();

			// Export scripture
			ExportScripture(progressDlg, writeFiles);

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether exported files should automatically overwrite
		/// existing files without prompting the user for confirmation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool OverwriteWithoutAsking
		{
			set
			{
				CheckDisposed();
				m_overwriteAll = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/set the markup system for the export: Paratext or Toolbox/Shoebox
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MarkupType MarkupSystem
		{
			get
			{
				CheckDisposed();
				return m_markupSystem;
			}
			set
			{
				CheckDisposed();
				m_markupSystem = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/set the flag to export the Scripture domain.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ExportScriptureDomain
		{
			get
			{
				CheckDisposed();
				return m_exportScripture;
			}
			set
			{
				CheckDisposed();
				m_exportScripture = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/set the flag to export the back translation domain.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ExportBackTranslationDomain
		{
			get
			{
				CheckDisposed();
				return m_exportBackTrans;
			}
			set
			{
				CheckDisposed();
				m_exportBackTrans = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/set the flag to export the notes domain.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ExportNotesDomain
		{
			get
			{
				CheckDisposed();
				return m_exportNotes;
			}
			set
			{
				CheckDisposed();
				m_exportNotes = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/Set the list of requested analysis writing systems that will be used when
		/// exporting back translation data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int[] RequestedAnalysisWss
		{
			get
			{
				CheckDisposed();
				return m_requestedAnalWS;
			}
			set
			{
				CheckDisposed();

				m_requestedAnalWS = value;
				int numWS = m_requestedAnalWS.Length;
				m_icuLocales = new string[numWS];

				for (int i = 0; i < numWS; i++)
				{
					IWritingSystem ws = m_cache.ServiceLocator.WritingSystemManager.Get(m_requestedAnalWS[i]);
					m_icuLocales[i] = ws.IcuLocale;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating the Short Name to use for the Paratext Project. If this
		/// is null, Paratext project files will not be created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ParatextProjectShortName
		{
			set
			{
				CheckDisposed();

				Debug.Assert(value != null);
				Debug.Assert(value.Length >= 3 && value.Length <= 5);
				m_p6ShortName = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating the Paratext Project Folder (where Paratext settings files
		/// get created). If this is null, Paratext project files will not be created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ParatextProjectFolder
		{
			set
			{
				m_paratextProjFolder = value;
				m_exportParatextProjectFiles = (m_paratextProjFolder != null);
				if (m_exportParatextProjectFiles)
					m_pictureFilesToCopy = new List<string>();
			}
		}
		#endregion

		#region other properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Unicode character properties engine.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		private ILgCharacterPropertyEngine UnicodeCharProps
		{
			get
			{
				if (m_cpe == null)
					m_cpe = m_cache.ServiceLocator.UnicodeCharProps;
				return m_cpe;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string representation of the current reference suitable for inclusion in a
		/// picture caption.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string CurrentPictureRef
		{
			get
			{
				return m_currentBookAbbrev + " " + m_currentChapterRef + m_scr.ChapterVerseSepr +
					m_currentVerseNumString;
			}
		}
		#endregion

		#region Core methods: Export Book, Section, Paragraph, Runs
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export all of the scripture books in the filter
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="writeFiles">Indexes of files to write</param>
		/// ------------------------------------------------------------------------------------
		private void ExportScripture(IProgress progressDlg, bool[] writeFiles)
		{
			InitializeStateVariables();

			// Create the output file if all the books are going to one file. Otherwise,
			// the individual files will be created as each book is exported
			if (!m_splitByBook)
				m_file.Open(m_outputSpec);

			// Export all of the scripture books in the filter
			for (int bookIndex = 0; bookIndex < m_bookFilter.BookCount && !progressDlg.Canceled; bookIndex++)
			{
				if (m_splitByBook)
				{
					string filename = GetExportFileSpec(bookIndex);
					if (!m_overwriteAll && !writeFiles[bookIndex])
						continue;
					m_file.Open(filename);
				}

				ExportBook(progressDlg, m_bookFilter.GetBook(bookIndex));

				if (m_splitByBook)
				{
					CloseFile();
				}
			}

			// If we want the Paratext project files, then setup for writing them
			if (m_exportParatextProjectFiles)
				CreateParatextProjectFiles();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the state variables.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void InitializeStateVariables()
		{
			// If we are exporting BT only (i.e. NOT interleaved), establish the WS we
			// will export.
			if (m_exportBackTrans && !m_exportScripture)
			{
				//ENHANCE: handle any or all WS
				m_nonInterleavedBtWs = m_requestedAnalWS[0];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book number to be used in the exported file name. ParaText numbering schemes
		/// begin the New Testament with 41 instead of 40.
		/// </summary>
		/// <param name="bookNum">canonical number of the book.</param>
		/// <param name="markupSystem">whether export is for Toolbox or ParaText</param>
		/// <returns>the number used to represent the book, adjusted for ParaText as needed</returns>
		/// ------------------------------------------------------------------------------------
		protected static int GetExportBookCanonicalNum(int bookNum, MarkupType markupSystem)
		{
			// Increment ParaText book Ids for New Testament books by 1
			if (markupSystem == MarkupType.Paratext && bookNum > 39)
				return bookNum + 1;
			else
				return bookNum;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the file to export to, given the specified book index. This method
		/// is only necessary when splitting exported books into separate files.
		/// </summary>
		/// <param name="bookIndex"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string GetExportFileSpec(int bookIndex)
		{
			IScrBook book = m_bookFilter.GetBook(bookIndex);

			string scheme = string.Empty;

			switch (m_fileNameFormat.m_schemeFormat)
			{
				case FileNameFormat.SchemeFormat.NNBBB:
					scheme = GetExportBookCanonicalNum(book.CanonicalNum, m_markupSystem).ToString("d2")
						+ book.BookId;
					break;
				case FileNameFormat.SchemeFormat.BBB:
					scheme = book.BookId;
					break;
				case FileNameFormat.SchemeFormat.NN:
					scheme = GetExportBookCanonicalNum(book.CanonicalNum, m_markupSystem).ToString("d2");
					break;
			}

			return Path.Combine(m_outputSpec, m_fileNameFormat.m_filePrefix + scheme +
				m_fileNameFormat.m_fileSuffix + "." + m_fileNameFormat.m_fileExtension);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a single book.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="book">The book.</param>
		/// ------------------------------------------------------------------------------------
		protected void ExportBook(IProgress progressDlg, IScrBook book)
		{
			// set the book code and ordinal, used for vref fields and annotations
			m_currentBookCode = book.BookId;
			m_currentBookOrd = book.CanonicalNum;
			m_currentBookAbbrev = book.Abbrev.VernacularDefaultWritingSystem.Text;
			if (string.IsNullOrEmpty(m_currentBookAbbrev))
				m_currentBookAbbrev = m_currentBookCode;
			// reset the last chapter number written
			m_lastChapterWritten = 0;
			m_lastInvalidChapterWritten = null;
			// reset flags for locating the first sections of intro and Scripture sections
			m_fFoundFirstIntroSection = false;
			m_fFoundFirstScrSection = false;

			// Get list of annotations for this book
			CreateAnnotationList(m_currentBookOrd);

			progressDlg.Message = string.Format(DlgResources.ResourceString(
				"kstidExportBookStatus"), book.Name.UserDefaultWritingSystem.Text);

			// Write out the book information.
			WriteRecordMark(m_currentBookCode, 0);
			m_file.Write(@"\id " + m_currentBookCode);
			if (book.IdText != m_currentBookCode)
				m_file.Write(" " + book.IdText);
			m_file.WriteLine();

			// Write out the page header book name
			string bookName = null;
			if (m_exportScripture)
				bookName = book.Name.VernacularDefaultWritingSystem.Text;
			else if (m_exportBackTrans)
				bookName = book.Name.get_String(m_nonInterleavedBtWs).Text;

			if (bookName != null)
				m_file.WriteLine(@"\h " + bookName);

			//set the initial chapter and verse state for book title
			m_currentChapterRef = 1;
			m_lastNumericBeginVerseNum = m_lastNumericEndVerseNum = 0;
			m_currentVerseNumString = null;

			// Write out the title of the book
			m_currentParaIsHeading = true;
			foreach (IStTxtPara para in book.TitleOA.ParagraphsOS)
				ExportParagraph(para);

			// determine if we should explicitly output an implicit chapter number 1 for
			// this book: i.e. if there is more than one chapter in the book.
			ScrReference scrRef = new ScrReference(book.CanonicalNum, 1, 1, m_scr.Versification);
			bool outputImplicitChapter = scrRef.LastChapter > 1;

			// Write out the sections of the book.
			m_currentChapterRef = 0; //reset chapter ref to initial state for sections
			foreach (IScrSection section in book.SectionsOS)
				ExportSection(progressDlg, section, outputImplicitChapter);

			// Finish up any pending annotations for the book.
			ExportNotesForPriorVerse(RefElement.Book, m_currentBookOrd + 1);
			Debug.Assert(!ExportNotesDomain || m_annotationList.Count == 0);//if list exists, should now be empty
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export the heading and contents of the given section.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="section">current section in the book</param>
		/// <param name="outputImplicitChapter">if true, output implicit chapter number</param>
		/// ------------------------------------------------------------------------------------
		protected void ExportSection(IProgress progressDlg, IScrSection section,
			bool outputImplicitChapter)
		{
			m_currentSectionIsIntro = section.IsIntro;
			m_textOutputBegunInCurrentSection = false;
			BCVRef sectionRefStart = section.VerseRefStart;

			// Pre-process the first paragraph of the section. If this first paragraph begins with
			// a chapter number, we need to export the chapter marker now, before the section marker.
			RefElement runTypeFound;
			ITsString tss = ((IStTxtPara)section.ContentOA.ParagraphsOS[0]).Contents;
			ProcessParaStart(tss, section.VerseRefStart, out runTypeFound);

			// If m_currentChapterRef was not set during ProcessParaStart
			//  (i.e. the section does not begin with a chapter number run)...
			if (m_currentChapterRef == 0)
			{
				// Set the current chapter & verse ref state from the section start ref,
				//  so the vref markers and annotation matching will be correct.
				m_currentChapterRef = sectionRefStart.Chapter;
				m_lastNumericBeginVerseNum = m_lastNumericEndVerseNum = sectionRefStart.Verse;
				m_currentVerseNumString = null;
			}

			// Handle the special output needed if there is an implicit chapter 1.
			// If the section ref start indicates chapter 1, and the section is not an introduction, and
			// the first paragraph does not begin with a chapter run and no chapter has been written ...
			if (runTypeFound != RefElement.Chapter && m_lastChapterWritten == 0 &&
				sectionRefStart.Chapter == 1 && !m_currentSectionIsIntro)
			{
				// If we are to output implicit first chapter number...
				if (outputImplicitChapter)
				{
					// output the "\c 1" and record marker.
					// also see if the chapter we just wrote out needs to get an
					//  implicit first verse marker.
					if (WriteChapterTag(1))
						m_v1NeededForImplicitFirstVerse = ParaBeginsWithImplicitFirstVerse(tss, 0);
				}
				else
				{
					// book has only one chapter; output only the record marker, not a \c.
					m_file.WriteLine(); //always start on new line
					WriteRecordMark(m_currentBookCode, 1);
					m_v1NeededForImplicitFirstVerse = ParaBeginsWithImplicitFirstVerse(tss, 0);
				}
			}

			// Write out the section heading paragraphs
			m_currentParaIsHeading = true;
			foreach (IStTxtPara para in section.HeadingOA.ParagraphsOS)
				ExportParagraph(para);

			// Write out the section contents
			m_currentParaIsHeading = false;
			foreach (IStTxtPara para in section.ContentOA.ParagraphsOS)
				ExportParagraph(para);

			progressDlg.Step(0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a paragraph of a title, section heading, or section contents.
		/// </summary>
		/// <param name="para">the given paragraph</param>
		/// ------------------------------------------------------------------------------------
		protected void ExportParagraph(IStTxtPara para)
		{
			ExportMode exportMode = ExportMode.VernacularOnly;
			ITsString paraText;
			if (m_exportScripture)
				paraText = para.Contents;
			else
			{	// get the tss of the requested back translation
				if (!m_exportBackTrans)
					return;
				ICmTranslation trans = para.GetBT();
				paraText = (trans != null ?
					trans.Translation.get_String(m_nonInterleavedBtWs) : null);

				if (paraText == null)
				{
					// No useful translation. However, we still want to export the marker for
					// the paragraph, so create a dummy string so we have something to export.
					ITsStrFactory fact = TsStrFactoryClass.Create();
					paraText = fact.MakeString(string.Empty, m_nonInterleavedBtWs);
				}
				exportMode = ExportMode.BackTransOnly;
			}

			bool isEmptyPara = (paraText.Length == 0);

			// Pre-process this paragraph. If it begins with a chapter number, we need to export
			// the chapter marker before the paragraph marker.
			RefElement runTypeFound;
			ProcessParaStart(paraText, 0, out runTypeFound);

			// Get the paragraph style name.
			// TODO: if para.StyleRules is null, get the default paragraph style
			string styleName = null;
			if (para.StyleRules != null)
			{
				styleName = para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			}
			else
			{
				Debug.Fail("StyleRules should never be null.");
			}

			// If this is a section head style and it is for chapter 1 and there is no
			// text, then we skip it.
			IStStyle style = m_scr.FindStyle(styleName);
			if (style != null && style.Structure == StructureValues.Heading)
			{
				// Only skip the first section in intro or Scripture if it is the very first one.
				IScrSection section = para.OwnerOfClass<IScrSection>();
				if (section.IsIntro && !m_fFoundFirstIntroSection)
				{
					m_fFoundFirstIntroSection = true;
					if (isEmptyPara)
						return;
				}
				else if (!section.IsIntro && !m_fFoundFirstScrSection)
				{
					m_fFoundFirstScrSection = true;
					if (isEmptyPara)
						return;
				}
			}

			// Determine the marker for the paragraph.
			string paraMarker = GetMarkerForStyle(styleName);

			// Get the back translation if it exists and the options are enabled to export it
			//  interleaved with scripture.
			ICmTranslation backTranslation = null;
			if (m_markupSystem == MarkupType.Toolbox && m_exportScripture && m_exportBackTrans)
			{
				backTranslation = para.GetBT(); //may return null if no BT exists
				exportMode = ExportMode.InterleavedVernAndBt;
			}

			// Export the fields of the paragraph,
			//  also the interleaved back translation if appropriate
			ExportParagraphDetails(paraMarker, paraText, backTranslation, para.Hvo, exportMode);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the given paragraph contents begins with a chapter or verse number.
		/// If so, export notes prior to this chapter or verse. If the paragraph starts with
		/// a chapter number, then export the \c field and related stuff.
		/// </summary>
		/// <param name="tss">given paragraph contents</param>
		/// <param name="sectionVerseRefStart"> if we are processing the start of a section,
		/// VerseRefStart information for the section; zero otherwise</param>
		/// <param name="runTypeFound">out: Chapter, Verse, or None</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessParaStart(ITsString tss, int sectionVerseRefStart,
			out RefElement runTypeFound)
		{
			int number;
			bool invalidSyntax;
			string numText;
			CheckParaStartForChapterOrVerse(tss, out runTypeFound, out number, out invalidSyntax,
				out numText);

			// If para begins with a verse, export pending notes now
			if (runTypeFound == RefElement.Verse)
			{
				if (number > 0)
					ExportNotesForPriorVerse(RefElement.Verse, number);
				else
				{
					// Verse is non-numeric, output notes for previous end verse
					if (m_textOutputBegunInCurrentSection)
						//						if (m_lastNumericEndVerseNum > 0) // only if numeric verse previously found
						ExportNotesForPriorVerse(runTypeFound, m_lastNumericEndVerseNum + 1);
				}
			}

			// If the paragraph starts with a chapter number run,
			// the \c field needs to be written now, before the paragraph or section markers.
			if (runTypeFound == RefElement.Chapter)
			{
				bool wroteNumericTag;
				wroteNumericTag = OutputChapterNumberAndRelated(number, invalidSyntax, numText,
					sectionVerseRefStart);

				// See if the chapter we just wrote out needs to output a \v 1 for an
				//  implicit first verse.
				if (wroteNumericTag)
					m_v1NeededForImplicitFirstVerse = ParaBeginsWithImplicitFirstVerse(tss, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks to see if the paragraph begins with a chapter or verse number.
		/// </summary>
		/// <param name="tssPara">The paragraph contents to check.</param>
		/// <param name="runType">out: Chapter, Verse, or None</param>
		/// <param name="number">out: The value of the chapter or verse begin number found in the
		/// first run (even if invalid syntax), or 0 if no digits found.</param>
		/// <param name="invalidSyntax">out: True if chapter or verse number (in numText) does
		/// not meet standard format syntax requirements</param>
		/// <param name="numText">out: the text of the number found, or null if none</param>
		/// ------------------------------------------------------------------------------------
		private void CheckParaStartForChapterOrVerse(ITsString tssPara,
			out RefElement runType, out int number, out bool invalidSyntax, out string numText)
		{
			runType = RefElement.None;
			number = 0;
			invalidSyntax = false;
			numText = null;
			if (tssPara.Length == 0)
				return;

			// Check the first run.
			ITsTextProps runProps = tssPara.get_Properties(0);
			if (runProps.Style() == ScrStyleNames.VerseNumber)
			{
				runType = RefElement.Verse;
				numText = tssPara.get_RunText(0).Trim();
				number = VerseBeginNumToInt(numText, out invalidSyntax);
			}
			else if (runProps.Style() == ScrStyleNames.ChapterNumber)
			{
				runType = RefElement.Chapter;
				numText = tssPara.get_RunText(0).Trim();
				number = ChapterNumStringToInt(numText, out invalidSyntax);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks to see if the given paragraph text, starting at the given run index,
		///  begins with an implicit first verse.
		/// </summary>
		/// <remarks>If the user chooses to enter the text of verse one without a verse number "1"
		/// run preceeding it, we call it an implicit verse one. In SF export, we need to
		/// explicitly output a \v 1 field for such an implicit first verse.</remarks>
		/// <param name="tssPara">The paragraph to check.</param>
		/// <param name="iChapterRun">The index of or just after the run of the chapter number.</param>
		/// <returns>True, if an implicit first verse exists, and \v1 should be added to the output.</returns>
		/// ------------------------------------------------------------------------------------
		private bool ParaBeginsWithImplicitFirstVerse(ITsString tssPara, int iChapterRun)
		{
			// Get the run after the chapter number.
			int indexOfNextRunAfterChapter = iChapterRun;
			if (tssPara.get_Properties(iChapterRun).Style() == ScrStyleNames.ChapterNumber)
				indexOfNextRunAfterChapter = iChapterRun + 1;

			// If there are no more runs after the chapter number then the chapter number
			// is by itself and we should not output an implied verse number
			if (indexOfNextRunAfterChapter >= tssPara.RunCount)
				return false;

			// Check if there is a verse number run directly after the chapter number.
			if (tssPara.Style(indexOfNextRunAfterChapter) == ScrStyleNames.VerseNumber)
				return false; // Real verse number found, so implicit verse not needed.

			// If not, check that there is no verse 1 later on in the paragraph.
			ITsTextProps runProps;
			string charStyleName;
			for (int iRun = iChapterRun; iRun < tssPara.RunCount; iRun++)
			{
				runProps = tssPara.get_Properties(iRun);
				charStyleName = runProps.Style();

				if (charStyleName == ScrStyleNames.VerseNumber)
				{
					int startVerse, endVerse;
					ScrReference.VerseToInt(tssPara.get_RunText(iRun), out startVerse, out endVerse);

					if (startVerse == 1)
						return false; // Explicit verse number found so implicit verse not needed.

					break;
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export the fields of a single paragraph (either vernacular or a back translation),
		/// with either Paratext or Toolbox markup. Typcial scripture markers such as \c \v etc.
		/// are used, plus \vt markers if we're doing Toolbox markup.
		/// Also (only if para is vernacular, not a heading, and doing Toolbox markup) we
		/// optionally interleave its back translation verse-by-verse using special \btxx
		/// markers.
		/// </summary>
		/// <param name="paraMarker">the paragraph marker for this paragraph (or translation)
		/// </param>
		/// <param name="tssPara">the paragraph's (or translation's) structured string to output
		/// </param>
		/// <param name="backTranslation">the back translation, if we are to export it
		/// interleaved, or null if not; if provided, we assume that <c>tssPara</c> is from a
		/// vernacular paragraph.</param>
		/// <param name="paraHvo">Hvo for the vernacular paragraph</param>
		/// <param name="exportMode">The export mode.</param>
		/// <remarks>For heading paragraphs we do not interleave the back translation
		/// verse-by-verse (it has no verses, silly) but rather we output the BT after the
		/// paragraph string.</remarks>
		/// ------------------------------------------------------------------------------------
		private void ExportParagraphDetails(string paraMarker, ITsString tssPara,
			ICmTranslation backTranslation, int paraHvo, ExportMode exportMode)
		{
			// If we are exporting interleaved back translation, we assume that toolbox markers
			//  are necessary and also that we have info on analysis writing systems.
			if (backTranslation != null)
			{
				Debug.Assert(exportMode == ExportMode.InterleavedVernAndBt);
				Debug.Assert(m_markupSystem == MarkupType.Toolbox);
				Debug.Assert(m_requestedAnalWS.Length > 0);
			}

			bool fParaMarkerWritten = false;
			bool fIntro = m_currentSectionIsIntro;

			// In Toolbox markup, we want to enable verse-by-verse interleave of back
			//  translation and annotations for verse text paragraphs only.
			// When enabled, the BT data will be output with
			//  \btvt markers within the current paragraph, ie without a bt paragraph marker.
			// We want to disable verse interleave for titles, headings, and intro paragraphs.
			// When disabled, the back translation data will be output with
			//  its own bt paragraph marker, eg \btmt, \bts, \btip
			//  (i.e. paragraph-by-paragraph interleave).
			// This bool controls verse interleaved output mode for this paragraph
			bool fEnableVerseInterleaved = (MarkupSystem == MarkupType.Toolbox &&
				!m_currentParaIsHeading && !fIntro);

			// Gather back translation info if needed
			List<ChapterVerseInfo>[] btVersesByWs = null;
			if (backTranslation != null && fEnableVerseInterleaved)
				btVersesByWs = ParseBackTranslationVerses(backTranslation);

			string prevCharStyle = null;
			bool fLastRunWasOrc = false;
			// Process all of the runs in the string
			for (int iRun = 0; iRun < tssPara.RunCount; iRun++)
			{
				string runText = tssPara.get_RunText(iRun);
				if (runText == null)
					continue;

				// because TeImport may add a hard line break (e.g. in a title), remove it before export.
				if (runText.Length > 0 && runText[0] == StringUtils.kChHardLB)
				{
					runText = runText.Substring(1);  // remove hard line break character
					if (runText.Length == 0)
						continue;
				}

				// because Toolbox export currently puts each run on its own line, we trim the
				// start; we don't want to see extra spaces between the marker and the run text
				if (MarkupSystem == MarkupType.Toolbox)
					runText = runText.TrimStart();

				// get the run's char style
				ITsTextProps runProps = tssPara.get_Properties(iRun);
				string charStyleName = runProps.GetStrPropValue(
					(int)FwTextPropType.ktptNamedStyle);

				// handle secondary and tertiary title styles as if they were paragraph styles.
				// This may happen before we output the \mt paragraph marker.
				// ENHANCE: If in the future we create distinctive TE styles to handle the
				// paragraph/character distinction properly, we might need to change the logic of
				// this condition.
				if (charStyleName == ScrStyleNames.SecondaryBookTitle ||
					charStyleName == ScrStyleNames.TertiaryBookTitle)
				{
					if (fParaMarkerWritten)
						m_file.WriteLine();
					m_file.WriteLine(GetMarkerForStyle(charStyleName) + " " + runText);
					prevCharStyle = null;
					fLastRunWasOrc = false;
					continue; // done with this run
				}

				// Output the paragraph marker now (if we haven't already)
				if (!fParaMarkerWritten)
				{
					// Toolbox markup normally needs the paragraph marker on a separate line.
					// Except for a title or heading, when the text stays with the paragraph
					// marker (e.g. "\s This is My Section Head").
					if (MarkupSystem == MarkupType.Toolbox && !m_currentParaIsHeading && !fIntro)
						m_file.WriteLine(paraMarker); //on a line by itself
					else
						m_file.Write(paraMarker + " ");
					fParaMarkerWritten = true;
				}

				if (charStyleName == ScrStyleNames.VerseNumber)
				{
					// Handle run with verse number style specially
					OutputVerseNumberRun(runText, fEnableVerseInterleaved, backTranslation,
						btVersesByWs);
					//					m_textOutputBegunInCurrentSection = true; //remember that verse output has begun
					prevCharStyle = null;
					fLastRunWasOrc = false;
				}
				else if (charStyleName == ScrStyleNames.ChapterNumber)
				{
					// Handle run with chapter number style specially
					if (OutputChapterNumberRun(runText, fEnableVerseInterleaved, backTranslation,
						btVersesByWs))
					{
						// See if the chapter we just wrote out needs to get an
						//  implicit first verse marker.
						m_v1NeededForImplicitFirstVerse = ParaBeginsWithImplicitFirstVerse(tssPara, iRun);
					}
					prevCharStyle = null;
					fLastRunWasOrc = false;
				}
				else
				{
					// If verse number one is inferred (was not physically present in the data)
					// we explicitly export verse number 1 before the text is output
					if (!m_currentParaIsHeading && !fIntro && m_v1NeededForImplicitFirstVerse)
					{
						// Note that we do not want to process the back translation text for
						//  this case.
						m_currentVerseNumString = m_scr.ConvertToString(1);
						m_lastNumericBeginVerseNum = m_lastNumericEndVerseNum = 1;
						OutputVerseNumberRun(m_currentVerseNumString, false, null, btVersesByWs);
					}

					if (charStyleName != null)
					{
						if (fLastRunWasOrc && charStyleName != null && charStyleName == prevCharStyle)
							m_file.WriteLine(kVerseTextMarker);
						// Handle run with typical character style
						OutputTypicalCharStyleRun(runText, charStyleName);
						prevCharStyle = charStyleName;
						fLastRunWasOrc = false;
					}
					else
					{
						// Handle run with no character style
						// check to see if the run is an ORC.
						if (runText.Length == 1 && runText[0] == StringUtils.kChObject)
						{
							ExportOrcRun(runProps, exportMode, (exportMode == ExportMode.BackTransOnly) ? 0 : -1);
							fLastRunWasOrc = true;
						}
						else //no character style, and not a valid ORC--export as plain paragraph run
							OutputPlainParagraphRun(runText, (!m_currentParaIsHeading && !fIntro));
					}

					// remember if output of real text (not chapter/verse nums) from section
					//  content has begun in this section
					if (!m_currentParaIsHeading)
						m_textOutputBegunInCurrentSection = true;
				}
			}

			// Close out the last line (for Paratext markup).
			m_file.WriteLine();

			// In case the paragraph string is empty, we still need to output the lone para marker
			if (!fParaMarkerWritten)
				m_file.WriteLine(paraMarker);

			// Finish up the back translation if needed
			if (backTranslation != null)
			{
				if (fEnableVerseInterleaved)
				{
					// output any remaining verse-by-verse back translation fields, within the
					//  current paragraph.
					ExportBackTransForPriorVerse(backTranslation, btVersesByWs, RefElement.None, string.Empty);
				}
				else
				{
					// for a heading or intro paragraph, export its back translation paragraph now
					ExportBackTransForHeadingPara(paraMarker, backTranslation);
				}
			}

			// Finish up remaining annotations that match this paragraph's hvo.
			ExportNotesForThisPara(paraHvo);
		}
		#endregion

		#region Output Verse Numbers, Chapter Numbers, Text Runs, ORC Runs
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Output a run whose character style is the verse number style.
		/// </summary>
		/// <param name="runText">Text run (should just be the verse number)</param>
		/// <param name="fEnableVerseInterleaved">True if we are to enable the output of
		/// verse-by-verse interleaved back translation and notes within the current paragraph.
		/// This would normally be FALSE for titles, section heads, and intro paragraphs.</param>
		/// <param name="backTranslation">the back translation, if we are to export it
		/// interleaved, or null if not; if provided, we assume that <c>runText</c> is from a
		/// vernacular paragraph</param>
		/// <param name="btVersesByWs">Array with an element for each desired writing system.
		/// Each element is a List of ChapterVerseInfo objects.</param>
		/// ------------------------------------------------------------------------------------
		private void OutputVerseNumberRun(string runText, bool fEnableVerseInterleaved,
			ICmTranslation backTranslation, List<ChapterVerseInfo>[] btVersesByWs)
		{
			runText = runText.Trim();

			int beginVerse, endVerse;
			bool invalidSyntax;
			VerseNumParse(runText, out beginVerse, out endVerse, out invalidSyntax);

			// Output any BT strings for previous verses
			// (backTranslation param is provided only for Toolbox markup and when
			//  interleaved Bt is requested)
			if (fEnableVerseInterleaved)
			{
				if (backTranslation != null)
				{
					ExportBackTransForPriorVerse(backTranslation, btVersesByWs, RefElement.Verse,
						BuildArabicString(runText));
				}
			}

			// Export Notes for prior verses
			if (beginVerse >= m_lastNumericEndVerseNum) //note: equal is ok because sequential verse segments all have the same int value, e.g. 2a 2b
				ExportNotesForPriorVerse(RefElement.Verse, beginVerse);
			else
			{
				// since beginVerse is not in order (or zero), use the last verse num to export the annotations
				//  if we have actually processed verse text (i.e. don't do this on first verse in a section
				//   if the verse num is invalid)
				if (m_textOutputBegunInCurrentSection)
					ExportNotesForPriorVerse(RefElement.Verse, m_lastNumericEndVerseNum + 1);
			}

			// Remember the new current verse, for exporting vrefs and annotation refs
			m_currentVerseNumString = runText;
			if (beginVerse > 0) //if beginVerse is non-zero, endVerse is also, by design of VerseNumParse
			{
				m_lastNumericBeginVerseNum = beginVerse;
				m_lastNumericEndVerseNum = endVerse;
			}

			// Finally, output the verse number field
			WriteVerseTag(runText);
			if (MarkupSystem == MarkupType.Toolbox)
				m_file.WriteLine();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Output a run whose character style is the chapter number style.
		/// </summary>
		/// <param name="runText">Text run (should just be the chapter number)</param>
		/// <param name="fEnableVerseInterleaved">True if we are to enable the output of
		/// verse-by-verse interleaved back translation and notes within the current paragraph.
		/// This would normally be FALSE for titles, section heads, and intro paragraphs.</param>
		/// <param name="backTranslation">the back translation, if we are to export it
		/// interleaved, or null if not; if provided, we assume that <c>runText</c> is from a
		/// vernacular paragraph</param>
		/// <param name="btVersesByWs">Array with an element for each desired writing system.
		/// Each element is a List of ChapterVerseInfo objects.</param>
		/// <returns>True if a numeric chapter field is really written.</returns>
		/// ------------------------------------------------------------------------------------
		private bool OutputChapterNumberRun(string runText, bool fEnableVerseInterleaved,
			ICmTranslation backTranslation, List<ChapterVerseInfo>[] btVersesByWs)
		{
			runText = runText.Trim();
			bool invalidChapter;
			int chapterNum = ChapterNumStringToInt(runText, out invalidChapter);

			// Output any BT strings for previous chapters/verses.
			// (backTranslation param is provided only for Toolbox markup and when
			//  interleaved Bt is requested)
			// also note: BT is output here only for chapter nums within a paragraph; there is
			//  no "BT for prior verse" at beginning of a para, because all BT for prior para was
			//  output completly at the end of that prior paragraph.
			if (fEnableVerseInterleaved)
			{
				if (backTranslation != null)
				{
					ExportBackTransForPriorVerse(backTranslation, btVersesByWs, RefElement.Chapter,
						BuildArabicString(runText));
				}
			}

			// Output the chapter number field, and related stuff
			return OutputChapterNumberAndRelated(chapterNum, invalidChapter, runText, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Output a chapter number field and several other fields that need to be output with
		/// the chapter number.
		/// </summary>
		/// <param name="chapterNum">the numeric value of the chapter number (or 0 if no digits)
		/// </param>
		/// <param name="invalidSyntax">true if the chapter number (in numText) has invalid
		/// syntax</param>
		/// <param name="numText">the text of the chapter number run</param>
		/// <param name="sectionVerseRefStart"> if we are processing the start of a section,
		/// VerseRefStart information for the section; zero otherwise</param>
		/// <returns>True if a numeric chapter field is really written.</returns>
		/// ------------------------------------------------------------------------------------
		private bool OutputChapterNumberAndRelated(int chapterNum, bool invalidSyntax, string numText,
			int sectionVerseRefStart)
		{
			// if this chapter number has already been preprocessed, don't do it again.
			if (!invalidSyntax)
			{
				if (chapterNum == m_lastChapterWritten) //valid syntax
					return false;
			}
			else if (m_lastInvalidChapterWritten != null && numText == m_lastInvalidChapterWritten)
				return false;

			// Export Notes for prior chapter
			// if chapter is in-order
			if (chapterNum >= m_lastChapterWritten) // equal should not occur
				ExportNotesForPriorVerse(RefElement.Chapter, chapterNum);
			else
			{
				// since chapterNum is not in order (or is zero), use the last chapter num written to export the annotations
				ExportNotesForPriorVerse(RefElement.Chapter, m_lastChapterWritten + 1);
			}

			// Remember the new current chapter/verse ref state, for exporting vrefs and annotations.
			// if we are now processing the start of a section, use the sectionVerseRefStart
			if (sectionVerseRefStart != 0)
			{
				m_currentChapterRef = BCVRef.GetChapterFromBcv(sectionVerseRefStart);
				m_currentVerseNumString = null;
				m_lastNumericBeginVerseNum = m_lastNumericEndVerseNum =
					BCVRef.GetVerseFromBcv(sectionVerseRefStart);
			}
			// otherwise, use the numeric value of the chapter number
			else if (chapterNum > 0)
			{
				m_currentChapterRef = chapterNum;
				m_currentVerseNumString = null;
				m_lastNumericBeginVerseNum = m_lastNumericEndVerseNum = 0; // zero is ok, because this code is not used at start of section (when section head annotations need to match verse)
			}

			// Finally, output the chapter number field
			bool wroteNumericTag;
			if (!invalidSyntax)
				wroteNumericTag = WriteChapterTag(chapterNum);
			else
				wroteNumericTag = WriteChapterTagInvalid(chapterNum, numText);

			return wroteNumericTag;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a run that has a typical character style applied.
		/// </summary>
		/// <param name="runText">Text run.</param>
		/// <param name="charStyleName">name of the character style applied to run.</param>
		/// ------------------------------------------------------------------------------------
		private void OutputTypicalCharStyleRun(string runText, string charStyleName)
		{
			// look up the style marker and write the run
			OutputCharField(runText, GetMarkerForStyle(charStyleName));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a run as a character style field.
		/// </summary>
		/// <param name="fieldText">Text of run to outpur.</param>
		/// <param name="marker">Standard Format marker.</param>
		/// ------------------------------------------------------------------------------------
		private void OutputCharField(string fieldText, string marker)
		{
			if (MarkupSystem == MarkupType.Toolbox)
			{
				m_file.WriteLine();	// This will ensure the marker gets put on it's own line.
				m_file.WriteLine(marker + " " + fieldText);
			}
			else	// Paratext markup
				m_file.Write(marker + " " + fieldText + marker + "*");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export whatever goes with an ORC (e.g. footnote, picture, etc.)
		/// </summary>
		/// <param name="ttp">Text properties of the run containing the ORC (from which we get
		/// the footnote GUID).</param>
		/// <param name="exportMode">The export mode.</param>
		/// <param name="iWs">index into the available analysis writing systems (should be -1 if
		/// exportMode is ExportMode.VernacularOnly; should  be 0 if exportMode is
		/// ExportMode.BackTransOnly)</param>
		/// ------------------------------------------------------------------------------------
		private void ExportOrcRun(ITsTextProps ttp, ExportMode exportMode, int iWs)
		{
			Debug.Assert(exportMode != ExportMode.VernacularOnly || iWs == -1);
			Debug.Assert(exportMode != ExportMode.BackTransOnly || iWs == 0);
			string objData = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);

			if (objData == null)
			{
				// We encountered a bogus ORC. Ignore it.
				return;
			}

			switch (objData[0])
			{
				case (char)FwObjDataTypes.kodtNameGuidHot:
				case (char)FwObjDataTypes.kodtOwnNameGuidHot:
					{
						Guid footnoteGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
						IScrFootnote footnote;
						if (m_cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().TryGetFootnote(footnoteGuid, out footnote))
							ExportFootnote(footnote, exportMode, iWs);
						break;
					}
				case (char)FwObjDataTypes.kodtGuidMoveableObjDisp:
					{
						Guid pictureGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
						ICmPicture picture;
						if (m_cache.ServiceLocator.GetInstance<ICmPictureRepository>().TryGetObject(pictureGuid, out picture))
						{
							// TODO (TE-3619): Need to pass export mode once we can handle putting picture ORCs in back translations.
							ExportPicture(picture);
						}
						break;
					}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Output a paragraph run that has no character style applied to it.
		/// </summary>
		/// <param name="runText">Text run.</param>
		/// <param name="fInVerseText">true if this run is in verse text; false if it is in
		/// a title, heading, or intro paragraph</param>
		/// ------------------------------------------------------------------------------------
		private void OutputPlainParagraphRun(string runText, bool fInVerseText)
		{
			// Output the run text without any special marker for character style.
			if (MarkupSystem == MarkupType.Toolbox)
			{
				// In most cases for Toolbox markup, AtStartOfLine will be true so the \vt
				// marker will be written out before the run text. However, in cases
				// like headings without a char. style (e.g. "\s This is My Section Head"),
				// AtStartOfLine will be false because the caller will have already
				// written out the paragraph marker (e.g. "\s ").
				// So... if marker hasn't already been output, we need the \vt marker now.
				if (m_file.AtStartOfLine)
				{
					string marker = fInVerseText ? kVerseTextMarker : kNoCharStyleMarker;
					m_file.Write(marker + " ");
				}

				m_file.WriteLine(runText);
			}
			else
			{
				// Paratext markup - we'll just concatenate the run text after the
				//  previously output paragraph marker, \c n, \v n, or delimited text
				m_file.Write(runText);
			}
		}
		#endregion

		#region Back Translation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export all of the back translation fields that occur before the given chapter
		/// or verse number. This is used for Toolbox markup only.
		/// </summary>
		/// <param name="cmTrans">back-translation object (potentially multiple WS's)</param>
		/// <param name="btVersesByWs">Array with an element for each desired writing system.
		/// Each element is a List of ChapterVerseInfo objects.</param>
		/// <param name="type">The type of number we must try to match: Chapter, Verse number,
		/// or None if no match needed and we will write all remaining  back translation fields
		/// </param>
		/// <param name="givenNumber">current chapter or verse number to match. All back
		/// translation fields up until this chapter/verse will be output.</param>
		/// ------------------------------------------------------------------------------------
		private void ExportBackTransForPriorVerse(ICmTranslation cmTrans, List<ChapterVerseInfo>[] btVersesByWs,
			RefElement type, string givenNumber)
		{
			givenNumber = givenNumber.Trim();

			// Process each of the back translation sets by writing system
			for (int iWs = 0; iWs < m_requestedAnalWS.Length; iWs++)
			{
				// get the back translation string for this WS
				ITsString tssBt = cmTrans.Translation.get_String(m_requestedAnalWS[iWs]);

				// If all items are requested then output all
				if (type == RefElement.None)
				{
					foreach (ChapterVerseInfo info in btVersesByWs[iWs])
						OutputBTInfo(info, tssBt, iWs);
					btVersesByWs[iWs].Clear();
				}
				else
				{
					// Look for chapter/verse info that matches the given givenNumber
					ChapterVerseInfo matchInfo = null;
					List<ChapterVerseInfo> verseInfoSet = btVersesByWs[iWs];
					if (verseInfoSet != null)
					{
						foreach (ChapterVerseInfo info in verseInfoSet)
						{
							if (info.Type == type && info.NumberString == givenNumber)
							{
								matchInfo = info;
								// remember that this info has a match in the vernacular
								info.Matched = true;
								break;
							}
						}
					}

					// If a match was found, then output all BT items before the one found
					if (matchInfo != null)
					{
						while (verseInfoSet[0] != matchInfo)
						{
							OutputBTInfo(verseInfoSet[0], tssBt, iWs);
							verseInfoSet.RemoveAt(0);
						}
					}
					else
					{
						// A match was not found for the current chapter/verse number;
						// so output everything that was previously matched.
						while (verseInfoSet.Count > 0 && verseInfoSet[0].Matched)
						{
							OutputBTInfo(verseInfoSet[0], tssBt, iWs);
							verseInfoSet.RemoveAt(0);
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Output a set of back translation fields for a single "verse" in the given writing
		/// system. This is used for Toolbox markup only.
		/// </summary>
		/// <param name="info">Chapter/Verse information object identifying the fields to export
		/// from tssBt</param>
		/// <param name="tssBt">structured string of the paragraph's back translation</param>
		/// <param name="iWs">index of the writing system of this back translation</param>
		/// ------------------------------------------------------------------------------------
		private void OutputBTInfo(ChapterVerseInfo info, ITsString tssBt, int iWs)
		{
			string icuSuffix = GetIcuSuffix(iWs);

			// If this item is a verse and it has not been matched to the vernacular, then
			// we need to put in a \btva and the verse number to mark the back translation
			// verse context.
			if (info.Type == RefElement.Verse && !info.Matched)
				m_file.WriteLine(kBackTransVerseAltMarker + icuSuffix + " " + info.NumberString);

			string prevCharStyle = null;
			bool fLastRunWasOrc = false;
			// output each run in the given range
			for (int iRun = info.iRunMinText; iRun < info.iRunLimText; iRun++)
			{
				ITsTextProps runProps = tssBt.get_Properties(iRun);
				string runText = tssBt.get_RunText(iRun).TrimStart();

				// check to see if the run is an ORC.
				if (runText.Length > 0 && runText[0] == StringUtils.kChObject)
				{
					ExportOrcRun(runProps, ExportMode.InterleavedVernAndBt, iWs);
					fLastRunWasOrc = true;
				}
				else // not an ORC
				{
					string charStyleName = runProps.GetStrPropValue(
						(int)FwTextPropType.ktptNamedStyle);
					if (fLastRunWasOrc && charStyleName != null && charStyleName == prevCharStyle)
						m_file.WriteLine(kBackTransVerseTextMarker);
					// build a back-translation marker from a vernacular marker and output it
					string subMarker = GetMarkerForStyle(charStyleName, kVerseTextMarker);

					m_file.WriteLine(kBackTransMarkerPrefix + subMarker.Substring(1) +
					icuSuffix + " " + runText);
					prevCharStyle = charStyleName;
					fLastRunWasOrc = false;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an icu suffix to place on markers based on the writing system for a BT
		/// </summary>
		/// <param name="iWs">index of the writing system in the list of requested
		/// analysis writing systems</param>
		/// <returns>the icu suffix to append to a marker</returns>
		/// ------------------------------------------------------------------------------------
		private string GetIcuSuffix(int iWs)
		{
			if (m_requestedAnalWS.Length == 1 || m_requestedAnalWS[iWs] == m_defaultAnalWS)
				return string.Empty;
			return @"_" + m_icuLocales[iWs];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Output the fields of a back translation heading paragraph. This is used for Toolbox
		/// markup only, to output the back translation of title or section head after the
		/// vernacular paragraph is already output.
		/// Special \btxx markers are used, including a \btxx paragraph marker.
		/// </summary>
		/// <param name="paraMarker">the paragraph marker that was used for the vernacular</param>
		/// <param name="backTranslation">the back translation structured string
		/// paragraph</param>
		/// <remarks>You might think about refactoring to eliminate this method and instead use
		///	ExportBackTransForPriorVerse(backTranslation, btVersesByWs, RefElement.None,
		///	tring.Empty), but it would have to be enhanced to optionally put out a paraMarker if
		///	given one, handle Secondary Titles specially, and perhaps use kNoCharStyleMarker
		///	instead of kVerseTextMarker for headings. It would probably get ugly.</remarks>
		/// ------------------------------------------------------------------------------------
		private void ExportBackTransForHeadingPara(string paraMarker,
			ICmTranslation backTranslation)
		{
			Debug.Assert(paraMarker != null);

			for (int iWs = 0; iWs < m_requestedAnalWS.Length; iWs++)
			{
				// set flag to give us a new para marker for this ws
				bool fParaMarkerWritten = false;

				// prepare our icuSuffix
				string icuSuffix;
				if (m_requestedAnalWS.Length == 1 || m_requestedAnalWS[iWs] == m_defaultAnalWS)
					icuSuffix = string.Empty;
				else
					icuSuffix = @"_" + m_icuLocales[iWs];

				// Get the TS String for the back translation in the requested WS
				ITsString tssBt = backTranslation.Translation.get_String(m_requestedAnalWS[iWs]);
				if (tssBt.Length == 0)
					continue;

				// Output each run
				for (int iRun = 0; iRun < tssBt.RunCount; iRun++)
				{
					ITsTextProps runProps = tssBt.get_Properties(iRun);
					string charStyleName = runProps.GetStrPropValue(
						(int)FwTextPropType.ktptNamedStyle);
					string runText = tssBt.get_RunText(iRun).TrimStart();
					string markerCharStyle;

					// Handle secondary and tertiary title char styles as if they were paragraph styles.
					// This may happen before we output the \mt paragraph marker.
					if (charStyleName == ScrStyleNames.SecondaryBookTitle ||
						charStyleName == ScrStyleNames.TertiaryBookTitle)
					{
						markerCharStyle = GetMarkerForStyle(charStyleName);
						m_file.WriteLine(kBackTransMarkerPrefix + markerCharStyle.Substring(1) +
							icuSuffix + " " + runText);
						continue; // done with this run
					}

					// Build a back-translation marker(\btxx_ws) from a vernacular marker and output the field
					if (charStyleName != null)
					{	// we have a character style
						markerCharStyle = GetMarkerForStyle(charStyleName);
						// if we haven't yet, we need to get the paragraph marker written first
						if (!fParaMarkerWritten)
							m_file.WriteLine(kBackTransMarkerPrefix + paraMarker.Substring(1) + icuSuffix);
						// now output the char marker and text
						m_file.WriteLine(kBackTransMarkerPrefix + markerCharStyle.Substring(1) +
							icuSuffix + " " + runText);
					}
					else // we have no character style
					{
						// we'll use the paragraph marker, or the kNoCharStyleMarker
						string subMarker = (fParaMarkerWritten) ? kNoCharStyleMarker : paraMarker;
						m_file.WriteLine(kBackTransMarkerPrefix + subMarker.Substring(1) +
							icuSuffix + " " + runText);
					}
					fParaMarkerWritten = true;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parse individual verse information for the given back translation.
		/// This generates an array with an element for each desired writing system.
		/// Each element is a List of ChapterVerseInfo objects.
		/// </summary>
		/// <param name="cmTrans">the back translation object</param>
		/// ------------------------------------------------------------------------------------
		protected List<ChapterVerseInfo>[] ParseBackTranslationVerses(ICmTranslation cmTrans)
		{
			if (cmTrans == null)
				return null;

			// Create the main array, with one element for each desired WS
			List<ChapterVerseInfo>[] btCvInfoByWs = new List<ChapterVerseInfo>[m_requestedAnalWS.Length];
			for (int iWs = 0; iWs < m_requestedAnalWS.Length; iWs++)
			{
				// Create the array list for the back translation in this writing system
				btCvInfoByWs[iWs] = new List<ChapterVerseInfo>();
				ITsString tss = cmTrans.Translation.get_String(m_requestedAnalWS[iWs]);
				if (tss == null || tss.Length == 0)
					continue;

				for (int iRun = 0; iRun < tss.RunCount; )
				{
					// Analyze this set of runs
					ITsTextProps ttp = tss.get_Properties(iRun);
					string styleName =
						ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
					string numberText;
					int iRunMin, iRunLim;

					if (styleName == ScrStyleNames.VerseNumber ||
						styleName == ScrStyleNames.ChapterNumber)
					{
						// get the number text, and trim it
						numberText = tss.get_RunText(iRun).Trim();
						// Get the starting run index for the text following the verse/chapter number
						iRunMin = iRun + 1;
						// advance past the chapter/verse run
						iRun++;
					}
					else
					{
						numberText = string.Empty;
						// The text starts with the beginning of this run
						iRunMin = iRun;
					}
					iRunLim = iRun;

					// Find the end of the text runs until the next chapter/verse number
					while (iRun < tss.RunCount)
					{
						ttp = tss.get_Properties(iRun);
						string nextStyleName =
							ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
						if (nextStyleName == ScrStyleNames.VerseNumber ||
							nextStyleName == ScrStyleNames.ChapterNumber)
						{
							break; //we found the next number
						}
						// advance our run lim to the next run and prepare for the next run
						iRunLim = ++iRun;
					}

					// Create a ChapterVerseInfo element for this set of runs
					RefElement type;
					if (styleName == ScrStyleNames.VerseNumber)
						type = RefElement.Verse;
					else if (styleName == ScrStyleNames.ChapterNumber)
						type = RefElement.Chapter;
					else
						type = RefElement.None;	// text without a chapter/verse number
					btCvInfoByWs[iWs].Add(new ChapterVerseInfo(type, numberText,
						iRunMin, iRunLim));
				}
			}

			return btCvInfoByWs;
		}
		#endregion

		#region Pictures, Footnotes
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a picture object.
		/// </summary>
		/// <param name="picture">the picture to export</param>
		/// ------------------------------------------------------------------------------------
		private void ExportPicture(ICmPicture picture)
		{
			// If we are doing a paratext non-interleaved BT, then just export the BT of the picture
			if (m_markupSystem == MarkupType.Paratext)
				ExportParatextPicture(picture);
			else
				ExportToolboxPicture(picture);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exports the picture in Paratext format.
		/// </summary>
		/// <param name="picture">The picture to export.</param>
		/// ------------------------------------------------------------------------------------
		private void ExportParatextPicture(ICmPicture picture)
		{
			string pictureMarker = GetMarkerForStyle("Figure USFM Parameters", @"\fig");
			if (m_exportBackTrans)
			{
				ExportPictureBT(picture.Caption);
				return;
			}
			else if (m_exportParatextProjectFiles)
			{
				string sFileToCopy = picture.PictureFileRA.AbsoluteInternalPath;
				if (!m_pictureFilesToCopy.Contains(sFileToCopy))
					m_pictureFilesToCopy.Add(sFileToCopy);
			}

			m_file.Write(pictureMarker + " " +
				picture.GetTextRepOfPicture(true, CurrentPictureRef, m_scr as IPictureLocationBridge) +
				pictureMarker + "*");
			return;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exports the picture in Toolbox format.
		/// </summary>
		/// <param name="picture">The picture to export.</param>
		/// ------------------------------------------------------------------------------------
		private void ExportToolboxPicture(ICmPicture picture)
		{
			if (!string.IsNullOrEmpty(picture.EnglishDescriptionAsString))
				m_file.WriteLine(GetMarkerForStyle("Figure Description", @"\figdesc") + " " + picture.EnglishDescriptionAsString);

			if (!string.IsNullOrEmpty(picture.PictureFileRA.AbsoluteInternalPath))
				m_file.WriteLine(GetMarkerForStyle("Figure Filename", @"\figcat") + " " + picture.PictureFileRA.AbsoluteInternalPath);

			if (!string.IsNullOrEmpty(picture.LayoutPosAsString))
				m_file.WriteLine(GetMarkerForStyle("Figure Layout Position", @"\figlaypos") + " " + picture.LayoutPosAsString);

			string sPictureLocation = ((IPictureLocationBridge)m_scr).GetPictureLocAsString(picture);
			if (!string.IsNullOrEmpty(sPictureLocation))
				m_file.WriteLine(GetMarkerForStyle("Figure Location", @"\figrefrng") + " " + sPictureLocation);

			string sCopyright = picture.PictureFileRA.Copyright.VernacularDefaultWritingSystem.Text;
			if (!string.IsNullOrEmpty(sCopyright))
				m_file.WriteLine(GetMarkerForStyle("Figure Copyright", @"\figcopy") + " " + sCopyright);

			string sCaption = picture.Caption.VernacularDefaultWritingSystem.Text;
			if (!string.IsNullOrEmpty(sCaption))
				m_file.WriteLine(GetMarkerForStyle(ScrStyleNames.Figure, @"\figcap") + " " + sCaption);

			if (picture.ScaleFactor > 0)
				m_file.WriteLine(GetMarkerForStyle("Figure Scale", @"\figscale") + " " + picture.ScaleFactor);

			// If there is a back translation of the caption and we are exporting back translations,
			// then export the back translation of the caption now.
			if (m_exportBackTrans)
				ExportPictureBT(picture.Caption);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export the back translation of a picture caption
		/// </summary>
		/// <param name="caption">caption to export</param>
		/// ------------------------------------------------------------------------------------
		private void ExportPictureBT(IMultiStringAccessor caption)
		{
			string pictureMarker = GetMarkerForStyle(ScrStyleNames.Figure, @"\figcap");
			string marker = kBackTransMarkerPrefix + pictureMarker.Substring(1);

			for (int i = 0; i < m_requestedAnalWS.Length; i++)
			{
				int ws = m_requestedAnalWS[i];
				string captionText = caption.get_String(ws).Text;
				if (captionText != null && captionText != string.Empty)
				{
					// make sure the pic starts on a new line
					m_file.WriteLine();
					m_file.WriteLine(marker + GetIcuSuffix(i) + " " + captionText);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a footnote object.
		/// </summary>
		/// <param name="footnote">the footnote</param>
		/// <param name="exportMode">The export mode.</param>
		/// <param name="iWs">index into the available analysis writing systems (should be -1 if
		/// exportMode is ExportMode.VernacularOnly</param>
		/// ------------------------------------------------------------------------------------
		private void ExportFootnote(IScrFootnote footnote, ExportMode exportMode, int iWs)
		{
			int nParas = footnote.ParagraphsOS.Count;
			Debug.Assert(nParas > 0);

			string paraStyleName = null;
			string footnoteMarker = null;
			ITsString tss;
			bool fNormalCharStyle = false; // whether previous run was a normal character style

			// Eventually we might support multiple paragraphs...
			for (int iPara = 0; iPara < nParas; iPara++)
			{
				// Get the footnote paragraph
				IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[iPara];
				if (iWs == -1) // get the vernacular para
					tss = para.Contents;
				else
				{
					// get the specified back translation
					ICmTranslation trans = para.GetBT();
					int wsAlt = m_requestedAnalWS[iWs];
					tss = (trans != null) ? trans.Translation.get_String(wsAlt) : null;
				}

				if (iPara == 0)
				{
					// Map the style name to a marker.
					paraStyleName = footnote.ParaStyleId;

					// Output the opening sf marker for the footnote.
					string fm = GetMarkerForStyle(paraStyleName).Substring(1);
					if (exportMode == ExportMode.InterleavedVernAndBt && iWs != -1)
						footnoteMarker = kBackTransMarkerPrefix + fm + GetIcuSuffix(iWs);
					else
						footnoteMarker = @"\" + fm;
					string footnoteField = footnoteMarker + " ";

					// Output the footnote caller (footnote marker type or literal marker)
					string sCaller;
					switch (m_scr.DetermineFootnoteMarkerType(paraStyleName))
					{
						// output character specified by USFM 2.0 standard
						case FootnoteMarkerTypes.AutoFootnoteMarker:
							sCaller = "+";
							break;
						case FootnoteMarkerTypes.NoFootnoteMarker:
							sCaller = "-";
							break;
						case FootnoteMarkerTypes.SymbolicFootnoteMarker:
						default:
							sCaller = m_scr.DetermineFootnoteMarkerSymbol(paraStyleName);
							break;
					}
					footnoteField += sCaller + " ";
					if (MarkupSystem == MarkupType.Paratext)
						m_file.Write(footnoteField);
					else
					{
						m_file.WriteLine();
						m_file.WriteLine(footnoteField);
					}
					// Output the chapter/verse reference if the footnote displays it.
					// But never output the ref for footnotes in a title or section heading.
					// And never output the ref for footnotes in an intro paragraph (which will
					// be the case when the verse number is zero).
					string reference = footnote.RefAsString;
					if (footnote.DisplayFootnoteReference && reference != null && reference != string.Empty)
					{
						bool dummy = false;
						OutputFootnoteCharStyleRun(footnote.ParaStyleId, true,
							ScrStyleNames.FootnoteTargetRef, reference, exportMode, iWs, ref dummy,
							ref fNormalCharStyle);
					}
					// ENHANCE: First run of footnote paragraph may not be normal footnote text; If it is
					// a general character style (or default para chars), we want to precede it with an \ft;
					// but if it's a footnote character style, we don't. In order to know the difference,
					// we would need to access the stylesheet to get the context, so for now we'll just
					// output the \ft all the time since it doesn't seem to cause any problems in Paratext.
					//if (MarkupSystem == MarkupType.Paratext)
					//    OutputFootnoteCharStyleRun(footnote.ParaStyleId, ksDefaultFootnoteCharacters, string.Empty);
				}

				if (tss == null)
				{
					Debug.Assert(iWs != -1, "tss can only be null if this is a BT export (this footnote never had a BT created)");
					continue;
				}

				// Flag indicating whether we're already in the context of an \ft, \fk, fqa, \xt, etc. field
				bool fInFootnoteContentField = false;

				// Output all of the runs in the footnote string.
				for (int iRun = 0; iRun < tss.RunCount; iRun++)
				{
					ITsTextProps runProps = tss.get_Properties(iRun);
					string runText = tss.get_RunText(iRun);
					if (runText == null)
						continue;
					string styleName = runProps.GetStrPropValue(
						(int)FwTextPropType.ktptNamedStyle);

					// Output the run text within char style markers
					if (styleName == null)
					{
						if (fInFootnoteContentField)
						{
							// If we are in a footnote content field in Paratext format and
							// the previous run was not a standard character style,
							// then we need to insert a marker to indicate the next content field.
							if (MarkupSystem == MarkupType.Paratext && !fNormalCharStyle)
							{
								Debug.Assert(paraStyleName != null);
								Debug.Assert(m_markerMappings.ContainsKey(paraStyleName + "\uffff" +
									ksDefaultFootnoteCharacters));
								m_file.Write(m_markerMappings[paraStyleName + "\uffff" +
									ksDefaultFootnoteCharacters] + " ");
							}
							m_file.Write(runText);
						}
						else
						{
							OutputFootnoteCharStyleRun(footnote.ParaStyleId, true,
								ksDefaultFootnoteCharacters, runText, exportMode, iWs,
								ref fInFootnoteContentField, ref fNormalCharStyle);
						}
					}
					else
					{
						// if the style has a customized mapping based on the type of the
						// note (either General Footnote or Cross-Reference), use it.
						bool fLookupBasedOnParaStyleContext = m_markerMappings.ContainsKey(
							paraStyleName + "\uffff" + styleName);
						OutputFootnoteCharStyleRun(footnote.ParaStyleId, fLookupBasedOnParaStyleContext,
							styleName, runText, exportMode, iWs, ref fInFootnoteContentField,
							ref fNormalCharStyle);
					}
				}
			}

			// if there were no paragraphs for the footnote, bail out, nothing happened
			if (nParas == 0)
				return;

			// Close the footnote - Paratext markup needs an end marker
			if (MarkupSystem == MarkupType.Paratext)
				m_file.Write(footnoteMarker + "*");
		}
		#endregion

		#region Annotations
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export annotations for verse(s) prior to reference represented by the given number.
		/// </summary>
		/// <param name="type">The type of number we must try to match: Chapter, Verse, or Book.
		/// </param>
		/// <param name="nextNumber">current chapter, verse number or book number to match.
		/// All note fields prior to this chapter/verse/book will be output.</param>
		/// <remarks>Annotations are sorted by reference, but annotations to a single reference
		/// are not necessarily in the order they were added.</remarks>
		/// ------------------------------------------------------------------------------------
		private void ExportNotesForPriorVerse(RefElement type, int nextNumber)
		{
			if (!ExportNotesDomain)
				return;

			// Determine the next begin ref
			ScrReference nextBeginRef;
			switch (type)
			{
				case RefElement.Chapter:
					//					if (nextNumber >= m_lastChapterWritten) // equal is okay because chapter num may have been preprocessed in ExportSection or ExportPara
					nextBeginRef = new ScrReference(m_currentBookOrd, nextNumber, 1,
						m_scr.Versification);
					//					else
					//					{
					//						// since chapterNum is not in order (or zero), use the last chapter num to export the annotations
					//						nextBeginRef = new ScrReference(m_currentBookOrd, m_lastChapterWritten + 1, 1);
					//					}
					break;
				case RefElement.Verse:
					int verse = nextNumber;
					//					// if given verse number is not numeric, estimate from last numeric value
					//					if (verse == 0)
					//						verse = m_lastNumericEndVerseNum + 1;
					nextBeginRef = new ScrReference(m_currentBookOrd, m_currentChapterRef,
						verse, m_scr.Versification);
					break;
				case RefElement.Book:
				default:
					// write all remaining notes fields for the current book
					nextBeginRef = new ScrReference(nextNumber, 0, 0, m_scr.Versification);
					break;
			}

			// loop through the relevant annotations
			IScrScriptureNote annotation;
			BCVRef noteBeginRef;
			int annotationIndex = 0;
			while (annotationIndex < m_annotationList.Count)
			{
				annotation = m_annotationList[annotationIndex];
				noteBeginRef = annotation.BeginRef;

				// if note ref is prior to the given next reference...
				if (noteBeginRef < nextBeginRef)
				{
					// write out this annotation
					ExportNote(annotation);
					m_annotationList.RemoveAt(annotationIndex);
				}
				else
					annotationIndex++;

				if (noteBeginRef >= nextBeginRef)
					break; // we are done for now
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export remaining annotations that match this paragraph's hvo, but only if the
		/// verse reference of the annotation matches the current verse reference.
		/// </summary>
		/// <param name="paraHvo">hvo of the vernacular paragraph</param>
		/// ------------------------------------------------------------------------------------
		private void ExportNotesForThisPara(int paraHvo)
		{
			if (!ExportNotesDomain)
				return;

			// determine the current end ref in this paragraph
			BCVRef currentEndRef = new BCVRef(m_currentBookOrd, m_currentChapterRef,
				m_lastNumericEndVerseNum);

			// loop through the relevant annotations
			IScrScriptureNote annotation;
			BCVRef noteBeginRef;
			int annotationIndex = 0;
			while (annotationIndex < m_annotationList.Count)
			{
				annotation = m_annotationList[annotationIndex];
				noteBeginRef = annotation.BeginRef;

				// if note ref is equal (or prior) to the current end reference,
				// and if the note matches the given paragraph hvo
				if (noteBeginRef <= currentEndRef && annotation.BeginObjectRA != null && annotation.BeginObjectRA.Hvo == paraHvo)
				{
					// Found an annotation with the same hvo as the current paragraph
					ExportNote(annotation);
					m_annotationList.RemoveAt(annotationIndex);
					// TODO: When import/export supports notes for multiple paragraphs, we'll need
					//   to export all information about the note's begin and end objects.
				}
				else
					annotationIndex++;

				if (noteBeginRef > currentEndRef)
					break; // we are done for now
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export an annotation object.
		/// </summary>
		/// <param name="annotation">the annotation</param>
		/// ------------------------------------------------------------------------------------
		private void ExportNote(IScrScriptureNote annotation)
		{
			// preceed the annotation with chapter and verse markers, if none have been written
			WriteCVTagsIfNeeded(new ScrReference(annotation.BeginRef, m_scr.Versification));

			m_file.WriteLine(); //always start on a new line
			m_file.Write(kAnnotationMarker + " ");
			// REVIEW TE-3981: What about everything else besides the discussion?
			// Output all paragraphs in the one annotation field.
			foreach (IStTxtPara para in annotation.DiscussionOA.ParagraphsOS)
				m_file.WriteLine(para.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write the chapter/verse fields for a note, if they have not yet been written.
		/// </summary>
		/// <param name="reference">beginning reference of the note</param>
		/// ------------------------------------------------------------------------------------
		private void WriteCVTagsIfNeeded(ScrReference reference)
		{
			// if the given ref is in an intro section or if we are in a heading
			//  no chapter/verse fields needed for the annotation
			if (reference.Verse == 0 || m_currentParaIsHeading)
				return;

			// Chapter needs to be written if it is not yet written and the book has more than
			//  one chapter.
			bool chapterNeeded = false;
			// REVIEW: This is outputting the chapter even when it has no numeric value because of the initial value.
			if (reference.Chapter > m_lastChapterWritten && reference.LastChapter > 1)
				chapterNeeded = true;

			// If the given ref chapter and/or verse is beyond the markers already output...
			if (chapterNeeded || reference.Verse > m_lastNumericEndVerseNum)
			{
				// if chapter hasn't been written and book has more than one chapter...
				if (chapterNeeded)
				{
					// Both chapter and verse must be written.
					WriteChapterTag(reference.Chapter);
					m_currentChapterRef = reference.Chapter;
				}
				// output the verse fields
				m_currentVerseNumString = m_scr.ConvertToString(reference.Verse);
				WriteVerseTag(m_currentVerseNumString);
				m_lastNumericBeginVerseNum = m_lastNumericEndVerseNum = reference.Verse;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create list of Scripture annotations for the given book,
		/// if the notes domain is being exported.
		/// <param name="bookOrd">the given book number (1-based)</param>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateAnnotationList(int bookOrd)
		{
			if (!ExportNotesDomain)
				return;
			IFdoOwningSequence<IScrScriptureNote> notes = m_scr.BookAnnotationsOS[bookOrd - 1].NotesOS;
			m_annotationList = new List<IScrScriptureNote>(notes.Count);
			foreach (IScrScriptureNote ann in notes)
			{
				// Don't bother exporting checking error annotations (TE-6729).
				if (ann.AnnotationType != NoteType.CheckingError)
					m_annotationList.Add(ann);
			}
		}

		#endregion

		#region Chapter and Verse Number Evaluation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build an arabic numeral chapter/verse string that does not have RTL characters,
		/// from the given source string.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string BuildArabicString(string source)
		{
			StringBuilder dest = new StringBuilder();
			foreach (char ch in source)
			{
				// ignore RTL mark characters
				if (ch != kRtlMark)
				{
					// for digit characters, convert them to arabic
					if (UnicodeCharProps.get_IsNumber(ch))
						dest.Append(UnicodeCharProps.get_NumericValue(ch).ToString());
					else
						dest.Append(ch);
				}
			}

			return dest.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a chapter number string to an integer value.
		/// </summary>
		/// <param name="sNumber"></param>
		/// <returns>integer value of the chapter number, or zero if there were no digits</returns>
		/// ------------------------------------------------------------------------------------
		protected int ChapterNumStringToInt(string sNumber)
		{
			bool discard;
			return ChapterNumStringToInt(sNumber, out discard);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a chapter number string to an integer value.
		/// Per import specs: The only valid characters in a chapter number are decimal digits
		/// (as defined in the Unicode Standard).
		/// </summary>
		/// <param name="sNumber">given chapter number string</param>
		/// <param name="invalidChapter">out: true if an invalid syntax found in sNumber</param>
		/// <returns>integer value of the chapter number, or zero if there were no digits</returns>
		/// ------------------------------------------------------------------------------------
		protected int ChapterNumStringToInt(string sNumber, out bool invalidChapter)
		{
			invalidChapter = false;

			if (sNumber == null)
			{
				invalidChapter = true;
				return 0;
			}

			int number = 0;
			foreach (char ch in sNumber)
			{
				if (UnicodeCharProps.get_IsNumber(ch))
					number = (number * 10) + (int)UnicodeCharProps.get_NumericValue(ch);
				else
				{
					//any non-digit means chapter number is invalid--but get any number that we can find.
					invalidChapter = true;
					return VerseBeginNumToInt(sNumber); // TODO: should use ScrReference method
				}
			}

			// The number must be positive to be valid
			if (number <= 0)
				invalidChapter = true;
			return number;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the value of the verse number, or the beginning value of a bridge.
		/// </summary>
		/// <param name="sVerseNumber">given verse number string</param>
		/// <returns>verse number value</returns>
		/// ------------------------------------------------------------------------------------
		protected int VerseBeginNumToInt(string sVerseNumber)
		{
			bool invalidSyntax;
			int beginVerse, endVerse;
			VerseNumParse(sVerseNumber, out beginVerse, out endVerse, out invalidSyntax);
			return beginVerse;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the value of the verse number, or the beginning value of a bridge.
		/// </summary>
		/// <param name="sVerseNumber">given verse number string</param>
		/// <param name="invalidSyntax">out: true if an invalid syntax found in sVerseNumber</param>
		/// <returns>verse number value</returns>
		/// ------------------------------------------------------------------------------------
		protected int VerseBeginNumToInt(string sVerseNumber, out bool invalidSyntax)
		{
			int beginVerse, endVerse;
			VerseNumParse(sVerseNumber, out beginVerse, out endVerse, out invalidSyntax);
			return beginVerse;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the value of the verse number, or the ending value of a bridge.
		/// </summary>
		/// <param name="sVerseNumber">given verse number string</param>
		/// <returns>verse number end value</returns>
		/// ------------------------------------------------------------------------------------
		protected int VerseEndNumToInt(string sVerseNumber)
		{
			bool invalidSyntax;
			int beginVerse, endVerse;
			VerseNumParse(sVerseNumber, out beginVerse, out endVerse, out invalidSyntax);
			return endVerse;
		}

		// TODO: should use some form of ScrReference.VerseToInt method instead of the following method,
		//   so that all TE verse number evaluation is done by common code in one module
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a verse number string to an integer value.  Per import specs:
		/// A verse number field must begin with decimal digits (as defined in the Unicode
		/// Standard). In a verse bridge, the first number must be less than the second number
		/// and a dash must separate the verse numbers. You can use a single word-forming
		/// character to indicate a sub-verse segment.
		/// Some examples of valid verse number strings are: "17", "17-19", "17a".
		/// Note: if any portion of sVerseNumber is not valid, whatever valid numerical value
		/// that can be extracted from the number is taken and invalidSyntax is set to true.
		/// </summary>
		/// <param name="sVerseNumber">given verse number string</param>
		/// <param name="beginVerse">out: beginning verse number int</param>
		/// <param name="endVerse">out: ending verse number int</param>
		/// <param name="invalidSyntax">out: true if an invalid syntax found in sVerseNumber</param>
		/// <returns>integer value of the starting verse number</returns>
		/// ------------------------------------------------------------------------------------
		protected void VerseNumParse(string sVerseNumber, out int beginVerse, out int endVerse,
			out bool invalidSyntax)
		{
			beginVerse = endVerse = 0;
			invalidSyntax = false;

			if (sVerseNumber == null)
			{
				invalidSyntax = true;
				return;
			}

			string sBridge = m_scr.Bridge;
			int bridgeIndex = sVerseNumber.IndexOf(sBridge);

			if (bridgeIndex == -1)
			{
				// evaluate the non-bridged verse number
				beginVerse = endVerse = SimpleVerseNumStringToInt(sVerseNumber, out invalidSyntax);
				return;
			}

			// Evaluate the bridged verse number.

			// if first character of verse number is bridge character
			if (bridgeIndex == 0)
			{
				if (sVerseNumber.Length > 1)
					beginVerse = endVerse = SimpleVerseNumStringToInt(
						sVerseNumber.Substring(1), out invalidSyntax);
				invalidSyntax = true;
				return;
			}

			// if last character of verse number is bridge character
			if (sVerseNumber.Length == bridgeIndex + 1)
			{
				beginVerse = endVerse = SimpleVerseNumStringToInt(
					sVerseNumber.Substring(0, bridgeIndex), out invalidSyntax);
				invalidSyntax = true;
				return;
			}

			// Determine the limits of the verse numbers, adjusting for the left-to-right/
			// right-to-left characters adjacent to the bridge.
			int iEndStartVerse, iStartEndVerse;
			if (sVerseNumber[bridgeIndex - 1] == '\u200f' || sVerseNumber[bridgeIndex - 1] == '\u200e')
			{
				// We found a RTL or a LTR character before the bridge so we expect it after
				// the bridge
				if (sVerseNumber[bridgeIndex + 1] != '\u200f' && sVerseNumber[bridgeIndex + 1] != '\u200e')
				{
					invalidSyntax = true;
					return;
				}
				Debug.Assert(bridgeIndex > 1);
				iEndStartVerse = bridgeIndex - 1;
				iStartEndVerse = bridgeIndex + 2;
			}
			else
			{
				iEndStartVerse = bridgeIndex;
				iStartEndVerse = bridgeIndex + 1;
			}

			// Evaluate the beginning and ending verse value of the verse bridge
			bool beginVerseInvalid;
			beginVerse = SimpleVerseNumStringToInt(
					sVerseNumber.Substring(0, iEndStartVerse), out beginVerseInvalid);
			endVerse = SimpleVerseNumStringToInt(
				sVerseNumber.Substring(iStartEndVerse), out invalidSyntax);

			// if either verse num is invalid, the whole sVerseNumber is declared invalid
			if (beginVerseInvalid)
				invalidSyntax = true;

			// if endVerse is out of range
			if (endVerse > 176)
			{
				invalidSyntax = true;
				if (beginVerse <= 176)
					endVerse = beginVerse;
			}

			// if beginVerse is out of range
			if (beginVerse > 176)
			{
				invalidSyntax = true;
				if (endVerse <= 176)
					beginVerse = endVerse;
			}

			// if out of order
			if (beginVerse > endVerse)
			{
				invalidSyntax = true;
				endVerse = beginVerse; // not sure what's correct; for now use only begin number
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Evaluate a non-bridged verse number.
		/// </summary>
		/// <param name="sVerseNumber">non-bridged verse number string</param>
		/// <param name="invalidSyntax">out: true if an invalid syntax found in sVerseNumber</param>
		/// <returns>value of verse reference without optional following character</returns>
		/// ------------------------------------------------------------------------------------
		private int SimpleVerseNumStringToInt(string sVerseNumber, out bool invalidSyntax)
		{
			invalidSyntax = false;

			// Determine if string contains any digits
			int digitIndex = -1;
			for (int i = 0; i < sVerseNumber.Length; i++)
			{
				char ch = sVerseNumber[i];
				if (UnicodeCharProps.get_IsNumber(ch))
				{
					digitIndex = i;
					break;
				}
			}

			// if no digits found
			if (digitIndex == -1)
			{
				invalidSyntax = true;
				return 0;
			}

			// if there's junk before digits
			if (digitIndex != 0)
				invalidSyntax = true;

			// accumulate our digit values
			int number = 0;
			int nonDigitIndex = -1;
			for (int i = digitIndex; i < sVerseNumber.Length; i++)
			{
				char ch = sVerseNumber[i];
				if (UnicodeCharProps.get_IsNumber(ch))
					number = (number * 10) + (int)UnicodeCharProps.get_NumericValue(ch);
				else
				{
					//quit at the first non-digit
					nonDigitIndex = i;
					break;
				}
			}

			// if only '0' digit(s) found
			if (number == 0)
			{
				invalidSyntax = true;
				return number;
			}

			// if nothing after verse number digits
			if (nonDigitIndex == -1)
				return number;  // we are done!

			// if more than one character after verse digit(s), it's invalid
			if (sVerseNumber.Length > nonDigitIndex + 1)
				invalidSyntax = true;

			// if the char after the digits is not a letter, it's invalid
			if (!UnicodeCharProps.get_IsLetter(sVerseNumber[nonDigitIndex]))
				invalidSyntax = true;

			return number;
		}
		#endregion

		#region Writing Chapter and Verse Fields
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write out a "\c" field for the given chapter number, if it hasn't been written
		/// already.
		/// </summary>
		/// <param name="number">given chapter number</param>
		/// <returns>True if the numeric chapter field is really written.</returns>
		/// ------------------------------------------------------------------------------------
		private bool WriteChapterTag(int number)
		{
			//			// if this \c field has already been written, don't do it again.
			//			if (number == m_lastChapterWritten)
			//				return false;

			m_file.WriteLine(); //always start on new line

			// if chapter is not numeric, don't output a record marker
			//if (m_lastInvalidChapterWritten == null)
			WriteRecordMark(m_currentBookCode, number);

			// If converting to arabic digits then just make an ASCII number string.
			// Otherwise, use the scripture number formatter.
			if (m_scr.ConvertCVDigitsOnExport)
				m_file.WriteLine(@"\c " + number.ToString());
			else
				m_file.WriteLine(@"\c " + m_scr.ConvertToString(number));

			m_lastChapterWritten = number;
			m_lastInvalidChapterWritten = null;

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write out a "\c" field for the given invalid chapter number, if it hasn't been written
		/// already.
		/// </summary>
		/// <param name="number">given chapter number value if it could be extracted from
		/// the invalidChapterNumText, otherwise zero</param>
		/// <param name="invalidChapterNumText">given chapter number string, which has something
		/// wrong in it</param>
		/// <returns>True if a numeric chapter field is really written.</returns>
		/// ------------------------------------------------------------------------------------
		private bool WriteChapterTagInvalid(int number, string invalidChapterNumText)
		{
			//			invalidChapterNumText = invalidChapterNumText.Trim();

			//			// if this \c field has already been written, don't do it again.
			//			if (m_lastInvalidChapterWritten != null &&
			//					invalidChapterNumText == m_lastInvalidChapterWritten)
			//				return false;

			m_file.WriteLine(); //always start on new line

			// current requirements: skip the record marker when chapter num has no numeric value
			if (number > 0)
				WriteRecordMark(m_currentBookCode, number);

			// write the \c field with the invalid text
			m_file.WriteLine(@"\c " + invalidChapterNumText);

			//  update the current ref status
			m_lastInvalidChapterWritten = invalidChapterNumText;
			if (number > 0)
				m_lastChapterWritten = number;

			// return True if a numeric chapter field was written
			return (number > 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write out a record marker, with a key consisting of a book code and sequence number.
		/// </summary>
		/// <param name="bookID">book ID 3-letter code</param>
		/// <param name="recordNumber">record number for the record marker</param>
		/// ------------------------------------------------------------------------------------
		private void WriteRecordMark(string bookID, int recordNumber)
		{
			if (MarkupSystem == MarkupType.Toolbox)
				m_file.WriteLine(kRecordMarker + bookID + " " + recordNumber.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write the given verse number field. It may need to be converted from script numbers
		/// to arabic numbers.
		/// </summary>
		/// <param name="verseString"></param>
		/// ------------------------------------------------------------------------------------
		private void WriteVerseTag(string verseString)
		{
			m_file.WriteLine(); //always start on a new line

			// For toolbox markup, first write a vref marker for the verse
			if (MarkupSystem == MarkupType.Toolbox)
				WriteVerseRef(verseString);

			m_file.Write(MakeVerseTag(verseString, m_scr.ConvertCVDigitsOnExport));
			m_v1NeededForImplicitFirstVerse = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write a verse ref field, for Toolbox markup.
		/// </summary>
		/// <param name="verseString"></param>
		/// ------------------------------------------------------------------------------------
		private void WriteVerseRef(string verseString)
		{
			// if the last chapter num written had invalid syntax
			if (m_lastInvalidChapterWritten != null)
			{
				// If chaper num written has no numeric value, don't bother producing a vref.
				// Reasoning: the chapter info would be bogus for all verses in the chapter;
				// that would be too much of a mess to correct by hand in the standard format.
				if (ChapterNumStringToInt(m_lastInvalidChapterWritten) == 0)
					return;
			}

			m_file.WriteLine(kVerseRefMarker + " " + m_currentBookCode + "." +
				m_currentChapterRef.ToString() + ":" +
				// if verseString syntax is bad, get whatever numeric value we can; if none, zero
				VerseBeginNumToInt(verseString).ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a USFM string for a verse number.
		/// </summary>
		/// <param name="verseString">String representation of a verse number (or bridge)</param>
		/// <param name="convertDigits">Flag indicating whether to convert all digits to Arabic
		/// </param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string MakeVerseTag(string verseString, bool convertDigits)
		{
			StringBuilder newString = new StringBuilder();

			// Look at each character in the number string. Convert numeric digits if needed
			// and copy everything else over except RTL marks.
			foreach (char ch in verseString)
			{
				if (Char.IsDigit(ch) && convertDigits)
					newString.Append(Char.GetNumericValue(ch).ToString());
				else if (ch != kRtlMark)
					newString.Append(ch);
			}

			return @"\v " + newString.ToString() + " ";
		}
		#endregion

		#region Marker/Style Mappings
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write out the given run using an appropriate marker for the given footnote character
		/// style. If doing Toolbox markup, we write out each run on its own line.
		/// </summary>
		/// <param name="containingParaStyleContext">Name of the Paragraph style of the
		/// containing footnote</param>
		/// <param name="fLookupBasedOnParaStyleContext">If <c>true</c>, then the marker
		/// is context-dependent (i.e., based on which type of footnote is being output) and will
		/// be looked up based on the para style supplied.</param>
		/// <param name="styleName">Name of the character style</param>
		/// <param name="runText">The run text.</param>
		/// <param name="exportMode">The export mode.</param>
		/// <param name="iWs">index into the available analysis writing systems (should be -1 if
		/// exportMode is ExportMode.VernacularOnly</param>
		/// <param name="fInFootnoteContentField">Flag indicating whether we're already in the
		/// context of a footnote content field (e.g., \ft, \fk, \fq, \xt, etc.). This is
		/// always false for Toolbox export, which doesn't nest field, but rather outputs every
		/// field on its own line</param>
		/// <param name="fNormalCharStyle">if set to <c>true</c> if normal character style output
		/// in this method.</param>
		/// ------------------------------------------------------------------------------------
		private void OutputFootnoteCharStyleRun(string containingParaStyleContext,
			bool fLookupBasedOnParaStyleContext, string styleName, string runText,
			ExportMode exportMode, int iWs, ref bool fInFootnoteContentField,
			ref bool fNormalCharStyle)
		{
			if (fLookupBasedOnParaStyleContext)
				styleName = containingParaStyleContext + "\uffff" + styleName;
			string marker = GetMarkerForStyle(styleName);

			if (m_footnoteContentMarkers.Contains(marker))
			{
				if (exportMode == ExportMode.InterleavedVernAndBt && iWs != -1)
					marker = kBackTransMarkerPrefix + marker.Substring(1) + GetIcuSuffix(iWs);

				string sfField = marker + " " + runText;
				if (MarkupSystem == MarkupType.Toolbox)
					m_file.WriteLine(sfField);
				else
				{
					m_file.Write(sfField);
					fInFootnoteContentField = true;
				}
				fNormalCharStyle = false;
			}
			else // Normal character style used in a footnote.
			{
				if (!fInFootnoteContentField && MarkupSystem == MarkupType.Paratext)
				{
					OutputFootnoteCharStyleRun(containingParaStyleContext, true,
						ksDefaultFootnoteCharacters, string.Empty, exportMode, iWs,
						ref fInFootnoteContentField, ref fNormalCharStyle);
				}
				OutputCharField(runText, marker);
				fNormalCharStyle = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the appropriate marker for a given style name. The returned marker includes a
		/// leading backslash.
		/// </summary>
		/// <param name="styleName">style name to look up</param>
		/// <returns>USFM marker for the given style, or null if none</returns>
		/// ------------------------------------------------------------------------------------
		private string GetMarkerForStyle(string styleName)
		{
			return GetMarkerForStyle(styleName, @"\missingStyle");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the appropriate marker for a given style name. The returned marker includes a
		/// leading backslash.
		/// </summary>
		/// <param name="styleName">style name to look up</param>
		/// <param name="markerToUseIfNull">The marker to use if the style name is null or
		/// empty.</param>
		/// <returns>
		/// USFM marker for the given style, or markerToUseIfNull if none
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private string GetMarkerForStyle(string styleName, string markerToUseIfNull)
		{
			if (String.IsNullOrEmpty(styleName))
				return markerToUseIfNull;

			string sMarker;
			if (m_markerMappings.TryGetValue(styleName, out sMarker))
				return sMarker;

			// If a marker was not defined for this style, then it must be a user-defined
			// style so we need to generate a marker for it
			string marker = styleName.Replace(" ", "_");
			if (styleName[0] != '\\')
				marker = @"\" + marker;

			return marker;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create hash tables to map TE styles to various style info
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateStyleTables()
		{
			if (m_exportParatextProjectFiles)
			{
				IWritingSystem ws = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
				m_UsfmStyFileAccessor = new UsfmStyFileAccessor(ws.RightToLeftScript,
					m_cache.ServiceLocator.WritingSystemManager);
				ReadUsfmStyFile();
			}
			LoadStyleTables();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create hash tables to map TE styles to various style info
		/// </summary>
		/// <remarks>Made virtual to facilitate testing</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void LoadStyleTables()
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(DirectoryFinder.TeStylesPath);
			foreach (XmlElement elem in doc.SelectNodes("/Styles/markup/tag"))
			{
				string styleName = elem.Attributes.GetNamedItem("id").InnerText.Replace("_", " ");

				bool fStyleMappingSet = false;
				// get the marker from the sfm subnode.
				foreach (XmlNode sfmNode in elem.SelectNodes("sfm"))
				{
					if (sfmNode == null)
						continue;

					SetStyleMapping(styleName, sfmNode.Attributes.GetNamedItem("paraStyleContext"),
						sfmNode.InnerText);
					fStyleMappingSet = true;

					string sContext = elem.Attributes.GetNamedItem("context").InnerText;
					XmlNode function = elem.Attributes.GetNamedItem("use");
					string sFunction = function != null ? function.InnerText : null;
					if ((sContext == "note" || sContext == "psuedoStyle" || sFunction == "footnote") &&
						elem.Attributes.GetNamedItem("type").InnerText == "character")
						m_footnoteContentMarkers.Add(sfmNode.InnerText);
				}

				if (!fStyleMappingSet && m_exportParatextProjectFiles)
				{
					IStStyle style = m_scr.FindStyle(styleName);
					if (style != null && !m_UsfmStyFileAccessor.ContainsKey(styleName))
					{
						UsfmStyEntry entry = new UsfmStyEntry();
						m_UsfmStyFileAccessor.Add(styleName, entry);
						entry.SetPropertiesBasedOnStyle(style);
					}
				}
			}

			// Make sure we have mappings for default footnote characters and cross-ref target references
			// (in case the psuedo-style was deleted from TeStyles.xml).
			if (!m_markerMappings.ContainsKey(ScrStyleNames.NormalFootnoteParagraph + "\uffff" +
				ksDefaultFootnoteCharacters))
			{
				m_markerMappings[ScrStyleNames.NormalFootnoteParagraph + "\uffff" + ksDefaultFootnoteCharacters]
					= @"\ft";
				m_footnoteContentMarkers.Add(@"\ft");
			}
			if (!m_markerMappings.ContainsKey(ScrStyleNames.CrossRefFootnoteParagraph + "\uffff" +
				ksDefaultFootnoteCharacters))
			{
				m_markerMappings[ScrStyleNames.CrossRefFootnoteParagraph + "\uffff" + ksDefaultFootnoteCharacters]
					= @"\xt";
				m_footnoteContentMarkers.Add(@"\xt");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the style mapping.
		/// </summary>
		/// <param name="styleName">Name of the style.</param>
		/// <param name="paraStyleContext">The para style context.</param>
		/// <param name="marker">The SF marker, including the leading backslash.</param>
		/// ------------------------------------------------------------------------------------
		protected void SetStyleMapping(string styleName, XmlNode paraStyleContext, string marker)
		{
			// get the style name and replace any "_" characters with spaces.
			string key;
			if (paraStyleContext != null)
				key = paraStyleContext.Value.Replace("_", " ") + "\uffff" + styleName;
			else
				key = styleName;

			if (m_markerMappings.ContainsKey(key))
			{
				// This can only happen if someone sets up TeStyles.xml wrong, but the DTD can't enforce this kind of checking.
				MessageBox.Show(string.Format(TeResourceHelper.GetResourceString("kstidMultipleStyleExportCodes"),
					styleName, m_markerMappings[key]), m_app.ApplicationName);
			}
			else
			{
				m_markerMappings[key] = marker;

				if (m_exportParatextProjectFiles)
				{
					marker = marker.Substring(1); // strip off the leading backslash
					UsfmStyEntry entry;
					if (m_UsfmStyFileAccessor.ContainsKey(marker))
					{
						entry = (UsfmStyEntry)m_UsfmStyFileAccessor[marker];
						// If this entry is keyed in the table based on the marker, we now need to move it
						// so it will be keyed by TE style name, so that StyleInfoTable.ConnectStyles will
						// be able to find the styles to connect things up correctly.
						// But don't do this for the special case where the key includes a para style context
						// because it would result in conflicts, and those special "styles" are never going
						// to be the based-on or next styles for anything.
						if (marker != styleName && paraStyleContext == null)
						{
							m_UsfmStyFileAccessor.Remove(marker);
							m_UsfmStyFileAccessor.Add(styleName, entry);
						}
					}
					else
					{
						entry = new UsfmStyEntry();
						entry.P6Marker = marker;
						m_UsfmStyFileAccessor.Add(paraStyleContext == null ? styleName : marker, entry);
					}
					IStStyle style = m_scr.FindStyle(styleName);
					if (style != null)
					{
						// Update the values of the entry based on the TE style.
						entry.SetPropertiesBasedOnStyle(style);
					}
				}
			}
		}
		#endregion

		#region Methods for writing Paratext project files
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the file. This is virtual so it can be replaced in testing with an
		/// in-memory file writer.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual FileWriter OpenFile(string fileName)
		{
			FileWriter writer = new FileWriter();
			writer.Open(fileName);
			return writer;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the paratext project files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateParatextProjectFiles()
		{
			SCRIPTUREOBJECTSLib.ISCScriptureText3 paraTextSO;
			try
			{
				paraTextSO = m_cache.ThreadHelper.Invoke(() => new SCRIPTUREOBJECTSLib.SCScriptureTextClass());
			}
			catch
			{
				paraTextSO = null;
			}

			if (!Directory.Exists(m_paratextProjFolder))
				return; // Couldn't have exported to the Paratext project folder, so don't want to save settings

			if (!m_outputSpec.StartsWith(m_paratextProjFolder))
				return; // export wasn't done to the Paratext project folder, don't write settings files


			int ws = m_exportScripture || m_nonInterleavedBtWs < 0 ?
				m_cache.DefaultVernWs : m_nonInterleavedBtWs;
			IWritingSystem wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
			string wsName = wsObj.DisplayLabel;
			string lpName = m_cache.ProjectId.Name;
			string styleFileName = lpName + ".sty";

			// Create the STY file in the settings directory
			m_fileSty = OpenFile(Path.Combine(m_paratextProjFolder, styleFileName));
			m_UsfmStyFileAccessor.SaveStyFile(lpName, m_fileSty,
				m_exportScripture ? m_cache.DefaultVernWs : m_nonInterleavedBtWs);

			// Create the SSF file in the settings directory if export was done by book.
			if (m_fileNameFormat != null)
			{
				string ldsFileName = Path.Combine(m_paratextProjFolder, wsName + ".lds");
				WriteParatextSsfFile(paraTextSO, m_paratextProjFolder, styleFileName, ref ldsFileName);
				WriteParatextLdsFile(ldsFileName,
					m_exportScripture ? m_cache.DefaultVernWs : m_nonInterleavedBtWs);
				// As a final step, attempt to copy picture files (internal copies) to the pictures
				// folder where Paratext expects to find them.
				CopyPictureFilesForParatext();
				CopyVersificationFile();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified string contains text that is not undefined, i.e. "***".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ContainsText(string str)
		{
			return !string.IsNullOrEmpty(str) && !str.Contains("***");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates or updates the paratext SSF file.
		/// </summary>
		/// <param name="paraTextSO">The Paratext Scripture Object.</param>
		/// <param name="folder">The Paratext settings folder.</param>
		/// <param name="styleFileName">Name of the style file.</param>
		/// <param name="ldsFileName">Name of the LDS file.</param>
		/// ------------------------------------------------------------------------------------
		private void WriteParatextSsfFile(SCRIPTUREOBJECTSLib.ISCScriptureText3 paraTextSO,
			string folder, string styleFileName, ref string ldsFileName)
		{
			string ssfFileName = Path.Combine(folder, m_p6ShortName + ".ssf");
			bool fUpdateExisting = File.Exists(ssfFileName);
			int hvoWs = (ExportBackTranslationDomain ? RequestedAnalysisWss[0] : m_cache.DefaultVernWs);
			if (paraTextSO != null)
			{
				string[] shortNames = ParatextHelper.GetParatextShortNames(m_cache.ThreadHelper, paraTextSO);
				if (shortNames != null)
				{
					foreach (string sName in shortNames)
					{
						if (sName == m_p6ShortName)
						{
							fUpdateExisting = true;
							try
							{
								m_cache.ThreadHelper.Invoke(() => paraTextSO.Load(sName));
							}
							catch
							{
								// If Paratext considers the project settings file to be bogus, we just overwrite it.
								fUpdateExisting = false;
							}
							break;
						}
					}
				}
			}

			try
			{
				if (fUpdateExisting)
				{
					string language = m_ParatextSsfFileAccessor.UpdateSsfFile(ssfFileName,
						m_fileNameFormat, m_p6ShortName, styleFileName, m_outputSpec, hvoWs);
					// REVIEW (MikeL): Why was the ldsFileName always re(set)? During back translation export,
					// it was changing the name of the back translation to the vernacular writing system.
					if (ldsFileName == string.Empty)
						ldsFileName = Path.Combine(folder, language + ".lds");
					return;
				}
			}
			catch
			{
				// We only ignore errors during the update attempt if Paratext is not installed. While
				// this may seem bizarre and nearly brain-dead, the reason is that we suspect the
				// failure is probably due to a corrupt file. In that case, we just attempt to overwrite
				// it and get on with life. If Paratext is installed, we assume the file is not corrupt
				// since Paratext successfully loaded it.
				if (paraTextSO != null)
					throw;
			}

			using (FileWriter ssfWriter = OpenFile(ssfFileName))
			{
				m_ParatextSsfFileAccessor.SaveSsfFile(m_fileNameFormat, m_p6ShortName, styleFileName, m_outputSpec,
					ssfWriter, hvoWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes (creates or updates) a Paratext LDS file.
		/// </summary>
		/// <param name="ldsFileName">Name of the LDS file.</param>
		/// <param name="ws">The writing system which the LDS file describes.</param>
		/// ------------------------------------------------------------------------------------
		protected void WriteParatextLdsFile(string ldsFileName, int ws)
		{
			ParatextLdsFileAccessor paratextLdsFileAccessor = new ParatextLdsFileAccessor(m_cache);
			UsfmStyEntry normalStyleEntry = (UsfmStyEntry)m_UsfmStyFileAccessor[ScrStyleNames.Normal];
			paratextLdsFileAccessor.WriteParatextLdsFile(ldsFileName, ws, normalStyleEntry);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the picture files for paratext.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CopyPictureFilesForParatext()
		{
			string sFiguresFolder = Path.Combine(m_outputSpec, "Figures");
			if (!Directory.Exists(sFiguresFolder))
			{
				try
				{
					Directory.CreateDirectory(sFiguresFolder);
				}
				catch
				{
					// If figure folder cannot be created, don't worry about it. User can deal with it later.
					// REVIEW (TimS): Should we tell the user that we couldn't create the directory?
					return;
				}
			}

			foreach (string sPictureFile in m_pictureFilesToCopy)
			{
				string sDestinationFile = Path.Combine(sFiguresFolder, Path.GetFileName(sPictureFile));
				try
				{
					if (!File.Exists(sDestinationFile))
					{
						File.Copy(sPictureFile, sDestinationFile);
					}
				}
				catch
				{
					// If files cannot be copied, don't worry about it. User can deal with it later.
					// REVIEW (TimS): Should we tell the user that we couldn't copy the pictures?
				}
			}
			return;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the versification file to the Paratext project folder if it doesn't already
		/// exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CopyVersificationFile()
		{
			string versificationName =
				VersificationTable.GetFileNameForVersification(m_cache.LangProject.TranslatedScriptureOA.Versification);
			string verseFile = Path.Combine(m_paratextProjFolder, versificationName);
			if (!File.Exists(verseFile))
			{
				string sourceVerseFile = Path.Combine(DirectoryFinder.TeFolder, versificationName);
				try
				{
					File.Copy(sourceVerseFile, verseFile);
				}
				catch
				{
					// ignore errors on copy
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads the usfm sty file. This is virtual so it can be conditional in testing because
		/// some tests expect to start with an empty list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void ReadUsfmStyFile()
		{
			m_UsfmStyFileAccessor.ReadStylesheet(Path.Combine(DirectoryFinder.TeFolder, "usfm.sty"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the accessor for the USFM entries.
		/// </summary>
		/// <value>Accessor to usfm entries.</value>
		/// ------------------------------------------------------------------------------------
		public UsfmStyFileAccessor UsfmEntries
		{
			get
			{
				CheckDisposed();
				return m_UsfmStyFileAccessor;
			}
		}
		#endregion

		#region IDisposable implementation

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}
		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~ExportUsfm()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			// Take yourself off the Finalization queue
			// to prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dispose of unmanaged resources hidden in member variables.
		/// </summary>
		/// <param name="fDisposing">if set to <c>true</c> called from Dispose() method. In this
		/// case it is safe to access our managed member variables. If set to <c>false</c> we
		/// shouldn't access any managed objects because they might have been disposed already.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (m_isDisposed)
			{
				Debug.Assert(m_cpe == null);
				return;
			}

			if (fDisposing)
			{
				// dispose managed objects
				if (m_file != null)
					m_file.Dispose();

				if (m_fileSty != null)
					m_fileSty.Dispose();
			}

			m_file = null;
			m_fileSty = null;

			m_cpe = null;
			m_isDisposed = true;
		}

		#endregion
	}
}
