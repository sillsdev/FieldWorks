// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ExportXml.cs
// Responsibility:
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.OxesIO;
using SIL.Utils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides export of data to a XML format file (Open XML for Editing Scripture)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ExportXml
	{
		#region Enumerations
		/// <summary>Values used to keep track of nested sections.</summary>
		enum SectionLevel
		{
			Major = 0,
			Normal = 1,
			Minor = 2,
			Series = 3
		}
		/// <summary>Values used to keep track of nested table elements.</summary>
		enum TableElement
		{
			Table = 0,
			Row = 1,
			Cell = 2
		}
		#endregion

		#region Member variables
		/// <summary>the object that handles writing XML elements and attributes to a file</summary>
		private XmlTextWriter m_writer;
		/// <summary>where we get the data from</summary>
		private readonly FdoCache m_cache;
		private readonly IApp m_app;
		/// <summary>stores the list of books to export</summary>
		private readonly FilteredScrBooks m_bookFilter;
		/// <summary>the basic Scripture object</summary>
		private readonly IScripture m_scr;
		/// <summary>the pathname of the output XML file</summary>
		private readonly string m_fileName;
		/// <summary>maps from ws (LgWritingSystem hvo) to RFC4646 language code for analysis writing systems</summary>
		private readonly Dictionary<int, string> m_dictAnalLangs = new Dictionary<int, string>();
		/// <summary>maps from ws (LgWritingSystem hvo) to RFC4646 language code for all writing systems</summary>
		private readonly Dictionary<int, string> m_mapWsRFC = new Dictionary<int, string>();
		/// <summary>flag whether a translation (&lt;tr&gt;) element is open</summary>
		private bool m_fTranslationElementIsOpen = false;
		/// <summary>flag whether a translation (&lt;tr&gt;) element is open in a footnote</summary>
		private bool m_fTranslationElementIsOpenInFootnote = false;
		/// <summary>flag whether a translation (&lt;tr&gt;) element is open in a figure</summary>
		private bool m_fTranslationElementIsOpenInFigure = false;
		/// <summary>the name of the current book</summary>
		private string m_sCurrentBookName;
		/// <summary>the 3-letter id of the current book</summary>
		private string m_sCurrentBookId;
		/// <summary>the current chapter number, as a string</summary>
		private string m_sCurrentChapterNumber;
		/// <summary>the current verse number, as a string (may be something like "1-2")</summary>
		private string m_sCurrentVerseNumber;
		/// <summary>the starting reference of the current section (BBCCCVVV)</summary>
		private int m_sectionRefStart = 0;
		/// <summary>the ending reference of the current section (BBCCCVVV)</summary>
		private int m_sectionRefEnd = 0;
		/// <summary>1-based index of the current book</summary>
		private int m_iCurrentBook = 0;
		/// <summary>current chapter number</summary>
		private int m_iCurrentChapter = 0;
		/// <summary>current verse number (first verse given in m_sCurrentVerseNumber if that is a range)</summary>
		private int m_iCurrentVerse = 0;
		/// <summary>stack used to keep track of section nesting</summary>
		private readonly Stack<SectionLevel> m_stackSectionLevels = new Stack<SectionLevel>();
		/// <summary>stack used to keep track of table element nesting</summary>
		private readonly Stack<TableElement> m_stackTableElements = new Stack<TableElement>();
		/// <summary>flag whether an &lt;item level="1"&gt; element is open in the normal context</summary>
		private bool m_fListItem1Open = false;
		/// <summary>flag whether an &lt;item level="2"&gt; element is open in the normal context</summary>
		private bool m_fListItem2Open = false;
		/// <summary>flag whether an &lt;embedded&gt; element is open</summary>
		private bool m_fEmbedded = false;
		/// <summary>flag whether a &lt;speech&gt; element is open</summary>
		private bool m_fInSpeech = false;
		/// <summary>flag whether a &lt;title&gt; element is open</summary>
		private bool m_fInTitle = false;
		/// <summary>flag whether a &lt;figure&gt; element is open</summary>
		private bool m_fInFigure = false;
		/// <summary>1-based index of the current footnote or -1 if not in a footnote</summary>
		private int m_footnoteIndex = -1;
		/// <summary>store verse reference found in footnote to prevent duplicates</summary>
		private string m_sFootnoteVerseRef = null;
		/// <summary>number of times a verse reference has been repeated in a footnote</summary>
		private int m_cFootnoteVerseRefRepeated = 0;
		/// <summary>flag whether paragraphs from the ScrSection.Heading field are being exported</summary>
		private bool m_fInSectionHeader = false;
		/// <summary>flag whether a &lt;trGroup&gt; element is open</summary>
		private bool m_fInTrGroup = false;
		/// <summary>flag whether a &lt;labelTr&gt; element is open</summary>
		private bool m_fInLabelTr = false;
		/// <summary>flag whether a &lt;trGroup&gt; element is open in a footnote</summary>
		private bool m_fInFootnoteTrGroup = false;
		/// <summary>flag whether a &lt;trGroup&gt; element is open in a figure</summary>
		private bool m_fInFigureTrGroup = false;
		/// <summary>stack of OXES elements opened inside a &lt;tr&gt; element, used to handle nesting "styles"</summary>
		private readonly Stack<OxesInfo> m_stackTextRunInfo = new Stack<OxesInfo>();
		/// <summary>what to export: everything, filtered list of books, or a single book</summary>
		private readonly ExportWhat m_what;
		/// <summary>if single book, number of the book to export; otherwise meaningless</summary>
		private readonly int m_nBookSingle;
		/// <summary>if single book, index of the first section to export; otherwise meaningless</summary>
		private readonly int m_iFirstSection;
		/// <summary>if single book, index of the last section to export; otherwise meaningless</summary>
		private readonly int m_iLastSection;
		/// <summary>description of this work edited by user</summary>
		private readonly string m_sDescription;
		/// <summary>flag that we're processing an introductory section</summary>
		private bool m_fInIntroduction = false;
		/// <summary>list of notes for the current (upcoming) segmented back translation segments</summary>
		private readonly List<IScrScriptureNote> m_rgSegAnn = new List<IScrScriptureNote>();

		// This holds all the hvos of annotations that were exported. The hvos could be
		// stored in a list but we use a dictionary for speed. Therefore, the bool values
		// are useless and not used.
		private readonly Dictionary<int, bool> m_exportedAnnotations = new Dictionary<int, bool>();
		private bool m_droppedBtPictureWarningGiven;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a new instance of the <see cref="T:ExportXml"/> class.
		/// </summary>
		/// <param name="fileName">pathname of the XML file to create</param>
		/// <param name="cache">data source</param>
		/// <param name="filter">lists the books to export</param>
		/// <param name="app">The application</param>
		/// <param name="what">tells what to export: everything, filtered list, or single book</param>
		/// <param name="nBook">if single book, number of the book to export</param>
		/// <param name="iFirstSection">if single book, index of first section to export</param>
		/// <param name="iLastSection">if single book, index of last section to export</param>
		/// <param name="sDescription">The s description.</param>
		/// ------------------------------------------------------------------------------------
		public ExportXml(string fileName, FdoCache cache, FilteredScrBooks filter, IApp app,
			ExportWhat what, int nBook, int iFirstSection, int iLastSection, string sDescription)
		{
			m_fileName = fileName;
			m_cache = cache;
			m_bookFilter = filter;
			m_app = app;
			m_what = what;
			m_nBookSingle = nBook;
			m_iFirstSection = iFirstSection;
			m_iLastSection = iLastSection;
			m_sDescription = sDescription;
			m_scr = cache.LangProject.TranslatedScriptureOA;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string FileName
		{
			get { return m_fileName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether we are currently exporting a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool InFootnote
		{
			get { return m_footnoteIndex != -1; }
		}
		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Run the export
		/// </summary>
		/// <returns><c>true</c> if successful, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool Run(Form dialogOwner)
		{
			// Check whether we're about to overwrite an existing file.
			if (FileUtils.FileExists(m_fileName))
			{
				string sFmt = DlgResources.ResourceString("kstidAlreadyExists");
				string sMsg = String.Format(sFmt, m_fileName);
				string sCaption = DlgResources.ResourceString("kstidExportOXES");
				if (MessageBoxUtils.Show(sMsg, sCaption, MessageBoxButtons.YesNo,
					MessageBoxIcon.Warning) == DialogResult.No)
				{
					return false;
				}
			}
			try
			{
				try
				{
					m_writer = new XmlTextWriter(FileUtils.OpenFileForWrite(m_fileName, Encoding.UTF8));
				}
				catch (Exception e)
				{
					MessageBoxUtils.Show(e.Message, m_app.ApplicationName,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
					return false;
				}
				ExportTE(dialogOwner);
				return true;
			}
			catch (Exception e)
			{
				Exception inner = e.InnerException ?? e;
				if (inner is IOException)
				{
					MessageBoxUtils.Show(inner.Message, m_app.ApplicationName,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
					return false;
				}
				else
					throw;
			}
			finally
			{
				if (m_writer != null)
				{
					try
					{
						m_writer.Close();
					}
					catch
					{
						// ignore errors on close
					}
				}
				m_writer = null;
			}
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export all of the data related to TE.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportTE(Form dialogOwner)
		{
			m_writer.Formatting = Formatting.Indented;
			m_writer.Indentation = 1;
			m_writer.IndentChar = '\t';
			m_writer.WriteStartDocument();
			m_writer.WriteStartElement("oxes");
			m_writer.WriteAttributeString("xmlns", "http://www.wycliffe.net/scripture/namespace/version_" + Validator.OxesVersion);
			m_writer.WriteStartElement("oxesText");
			m_writer.WriteAttributeString("type", "Wycliffe-" + Validator.OxesVersion);
			// TODO: Get organization abbreviation from the user? other information for work tag?
			string sOrg = "WBT";
			string sLang = InitializeExportWs();
			m_writer.WriteAttributeString("oxesIDWork", String.Format("{0}.{1}", sOrg, sLang));
			m_writer.WriteAttributeString("xml", "lang", null, sLang);
			m_writer.WriteAttributeString("canonical", "true");

			// Export header information.
			WriteHeader(sOrg, sLang);

			// Export the title page.
			WriteTitlePage();

			// Export scripture
			ExportScripture(dialogOwner);

			m_writer.WriteEndElement();		// </oxesText>
			m_writer.WriteEndElement();		// </oxes>
			m_writer.WriteWhitespace(Environment.NewLine);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the writing systems for export.
		/// </summary>
		/// <returns>name of the vernacular writing system.</returns>
		/// ------------------------------------------------------------------------------------
		private string InitializeExportWs()
		{
			IWritingSystem wsVern = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			string sLang = wsVern.Id.Normalize();
			foreach (IWritingSystem ws in m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
			{
				if (!m_dictAnalLangs.ContainsKey(ws.Handle))
				{
					string sRFC = ws.Id.Normalize();
					m_dictAnalLangs.Add(ws.Handle, sRFC);
					m_mapWsRFC.Add(ws.Handle, sRFC);
				}
			}
			return sLang;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export the header information, at least that which can be obtained readily from
		/// existing information.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void WriteHeader(string sOrg, string sLang)
		{
			m_writer.WriteStartElement("header");
			string sWho;
			using (System.Security.Principal.WindowsIdentity whoami = System.Security.Principal.WindowsIdentity.GetCurrent())
				sWho = whoami.Name.Normalize();
			string sWhoAbbr = AbbreviateUserName(sWho);
			m_writer.WriteStartElement("revisionDesc");
			m_writer.WriteAttributeString("resp", sWhoAbbr);
			m_writer.WriteStartElement("date");
			m_writer.WriteString(String.Format("{0:yyyy.MM.dd}", DateTime.Now));
			m_writer.WriteEndElement();		//</date>
			m_writer.WriteStartElement("para");
			m_writer.WriteAttributeString("xml", "lang", null, GetRFCFromWs(m_cache.DefaultUserWs));
			m_writer.WriteString(m_sDescription);
			m_writer.WriteEndElement();		//</para>
			m_writer.WriteEndElement();		//</revisionDesc>
			m_writer.WriteStartElement("work");
			m_writer.WriteAttributeString("oxesWork", String.Format("{0}.{1}", sOrg, sLang));
			m_writer.WriteStartElement("titleGroup");
			m_writer.WriteStartElement("title");
			m_writer.WriteAttributeString("type", "main");
			OpenTrGroupIfNeeded();
			m_writer.WriteStartElement("tr");
			m_writer.WriteString("TODO: title of New Testament or Bible goes here");
			m_writer.WriteEndElement();		//</tr>
			CloseTrGroupIfNeeded();
			m_writer.WriteEndElement();		//</title>
			m_writer.WriteEndElement();		//</titleGroup>
			m_writer.WriteStartElement("contributor");
			m_writer.WriteAttributeString("role", "Translator");
			m_writer.WriteAttributeString("ID", sWhoAbbr);
			m_writer.WriteString(sWho);
			m_writer.WriteEndElement();		//</contributor>
			m_writer.WriteEndElement();		//</work>
			m_writer.WriteEndElement();		//</header>
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Since we don't have any better source of usernames, we're being passed in the system
		/// login name, which may well have a domain followed by the user.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static string AbbreviateUserName(string sWho)
		{
			string[] rgsDomainUser = sWho.Split(new char[] { '\\' });
			string[] rgsName = rgsDomainUser[rgsDomainUser.Length - 1].Split(new char[] { ' ' });
			if (rgsName.Length > 1)
			{
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < rgsName.Length; ++i)
					sb.Append(rgsName[i].ToCharArray()[0]);
				return sb.ToString().ToLower();
			}
			else
			{
				if (rgsName[0].Length > 3)
					return rgsName[0].Substring(0, 3).ToLower();
				else
					return rgsName[0].ToLower();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export the title page as best we can.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void WriteTitlePage()
		{
			m_writer.WriteStartElement("titlePage");
			m_writer.WriteStartElement("titleGroup");
			m_writer.WriteStartElement("title");
			m_writer.WriteAttributeString("type", "main");
			OpenTrGroupIfNeeded();
			m_writer.WriteStartElement("tr");
			m_writer.WriteString("TODO: Title of New Testament or Bible goes here");
			m_writer.WriteEndElement();
			CloseTrGroupIfNeeded();
			m_writer.WriteEndElement();
			m_writer.WriteEndElement();
			m_writer.WriteEndElement();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export all of scripture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportScripture(Form dialogOwner)
		{
			int sectionCount = 0;
			for (int i = 0; i < m_bookFilter.BookCount; i++)
				sectionCount += m_bookFilter.GetBook(i).SectionsOS.Count;
			using (var progressDlg = new ProgressDialogWithTask(dialogOwner))
			{
				progressDlg.Minimum = 0;
				progressDlg.Maximum = sectionCount;
				progressDlg.Title = DlgResources.ResourceString("kstidExportXmlProgress");
				progressDlg.AllowCancel = true;

				progressDlg.RunTask(true, ExportScripture);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exports the scripture.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters. (ignored)</param>
		/// <returns>always <c>null</c></returns>
		/// ------------------------------------------------------------------------------------
		private object ExportScripture(IThreadedProgress progressDlg, object[] parameters)
		{
			string sCanon = null;
			switch (m_what)
			{
				case ExportWhat.AllBooks:
					// Export all of the Scripture books in the project.
					for (int i = 0; i < m_scr.ScriptureBooksOS.Count; ++i)
						ExportBook(ref sCanon, m_scr.ScriptureBooksOS[i], progressDlg);
					break;
				case ExportWhat.FilteredBooks:
					// Export all of the Scripture books in the filter
					for (int bookIndex = 0; bookIndex < m_bookFilter.BookCount && !progressDlg.Canceled; bookIndex++)
						ExportBook(ref sCanon, m_bookFilter.GetBook(bookIndex), progressDlg);
					break;
				case ExportWhat.SingleBook:
					// Export a single book.
					ExportBook(ref sCanon, m_scr.FindBook(m_nBookSingle), progressDlg);
					break;
			}
			if (String.IsNullOrEmpty(sCanon))
			{
				m_writer.WriteStartElement("canon");
				m_writer.WriteAttributeString("ID", "nt");
			}
			m_writer.WriteEndElement();
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a single book
		/// </summary>
		/// <param name="sCanon">The current section of the Bible ("ot" or "nt")</param>
		/// <param name="book">The book.</param>
		/// <param name="progressDlg">The progress dialog.</param>
		/// ------------------------------------------------------------------------------------
		private void ExportBook(ref string sCanon, IScrBook book, IThreadedProgress progressDlg)
		{
			//m_writer.WriteComment(String.Format(
			//    "Book has {0} title paragraphs and {1} sections",
			//    book.TitleOA.ParagraphsOS.Count, book.SectionsOS.Count));
			if (book.CanonicalNum < 40)	// First 39 books are in the Old Testament.
			{
				if (String.IsNullOrEmpty(sCanon))
				{
					sCanon = "ot";
					m_writer.WriteStartElement("canon");
					m_writer.WriteAttributeString("ID", sCanon);
				}
			}
			else
			{
				if (sCanon != "nt")
				{
					if (!String.IsNullOrEmpty(sCanon))
						m_writer.WriteEndElement();
					sCanon = "nt";
					m_writer.WriteStartElement("canon");
					m_writer.WriteAttributeString("ID", sCanon);
				}
			}
			if (progressDlg != null)
			{
				progressDlg.Message = string.Format(DlgResources.ResourceString(
					"kstidExportBookStatus"), book.Name.UserDefaultWritingSystem.Text);
			}
			m_iCurrentBook = book.CanonicalNum;
			m_sCurrentBookId = book.BookId.Normalize();
			m_sCurrentBookName = GetProperBookName(book.Name.BestVernacularAlternative);
			m_iCurrentChapter = 0;
			m_sCurrentChapterNumber = String.Empty;
			m_iCurrentVerse = 0;
			m_sCurrentVerseNumber = String.Empty;

			m_writer.WriteStartElement("book");
			m_writer.WriteAttributeString("ID", m_sCurrentBookId);

			ExportBookTitle(book);
			m_fInIntroduction = false;
			int iFirst = 0;
			int iLim = book.SectionsOS.Count;
			if (m_what == ExportWhat.SingleBook)
			{
				iFirst = m_iFirstSection;
				iLim = m_iLastSection + 1;
			}
			for (int i = iFirst; i < iLim; ++i)
			{
				IScrSection section = book.SectionsOS[i];
				IScrSection nextSection = (i + 1) < book.SectionsOS.Count ? book.SectionsOS[i + 1] : null;
				ExportBookSection(section, nextSection);
				if (progressDlg != null)
				{
					if (progressDlg.Canceled)
						break;
					progressDlg.Step(0);
				}
			}
			if (m_fInIntroduction)
				m_writer.WriteEndElement();	// </introduction>
			FlushSectionElementStack(SectionLevel.Major);
			m_writer.WriteEndElement();		// </book>
		}

		/// <summary>
		/// Work around for mono bug https://bugzilla.novell.com/show_bug.cgi?id=517855
		/// </summary>
		private string GetProperBookName(object tssName)
		{
			if (!(tssName is ITsString))
				throw new ArgumentException();

			return GetProperBookName((ITsString)tssName);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given the best available vernacular book name, ensure that it's not all caps, and
		/// that it's normalized properly for XML output.
		/// </summary>
		/// <param name="tssName"></param>
		/// <returns></returns>
		/// <remarks>
		/// I don't really think this is really needed -- if the book name is not capitalized
		/// properly, some Scripture Check should detect it and notify the user.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		private string GetProperBookName(ITsString tssName)
		{
			if (tssName.Length == 0)
			{
				return String.Format("{0}{1}", m_sCurrentBookId[0], m_sCurrentBookId.Substring(1).ToLower());
			}
			else
			{
				int ws = TsStringUtils.GetWsAtOffset(tssName, 0);
				string sICU = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
				string sName = tssName.Text.Normalize();
				return Icu.ToTitle(sName, sICU);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export the book title paragraphs
		/// </summary>
		/// <param name="book"></param>
		/// ------------------------------------------------------------------------------------
		private void ExportBookTitle(IScrBook book)
		{
			m_writer.WriteStartElement("titleGroup");
			m_writer.WriteAttributeString("short", m_sCurrentBookName);
			bool fExportAnnotation = true;
			foreach (IStTxtPara para in book.TitleOA.ParagraphsOS)
			{
				BackTranslationInfo backtran = GetBackTranslationForPara(para);
				ExportTitleParagraph(para, backtran, ref fExportAnnotation);
			}
			if (book.TitleOA.ParagraphsOS.Count == 0)
			{
				m_writer.WriteStartElement("title");
				m_writer.WriteAttributeString("type", "main");
				ExportAnyRelatedAnnotation();
				m_writer.WriteStartElement("trGroup");
				m_writer.WriteEndElement();
				m_writer.WriteEndElement();
			}
			CloseAnyLeftoverOpenParagraphElements(true);
			m_writer.WriteEndElement();		// </titleGroup>
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close off a table, if one is open.  Close off any open list items, any open set of
		/// embedded elements, and possibly an open speech as well.
		/// </summary>
		/// <param name="fCloseSpeechIfOpen">flag to close &lt;speech&gt; element if it's open</param>
		/// ------------------------------------------------------------------------------------
		private void CloseAnyLeftoverOpenParagraphElements(bool fCloseSpeechIfOpen)
		{
			FinishTable();
			if (m_fListItem1Open)
			{
				m_writer.WriteEndElement();
				m_fListItem1Open = false;
			}
			if (m_fListItem2Open)
			{
				m_writer.WriteEndElement();
				m_fListItem2Open = false;
			}
			if (m_fEmbedded)
			{
				m_writer.WriteEndElement();
				m_fEmbedded = false;
			}
			if (m_fInSpeech && fCloseSpeechIfOpen)
			{
				m_writer.WriteEndElement();
				m_fInSpeech = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a book section.
		/// </summary>
		/// <param name="section"></param>
		/// <param name="nextSection"></param>
		/// ------------------------------------------------------------------------------------
		private void ExportBookSection(IScrSection section, IScrSection nextSection)
		{
			m_sectionRefStart = section.VerseRefStart;
			m_sectionRefEnd = section.VerseRefEnd;
			// The number for the first chapter exported can be omitted, especially for single chapter
			// books or a partial book exports that don't start at a chapter boundary.
			// Ensure we have a non-zero chapter number.  See TE-6731 & TE-7265
			if (m_iCurrentChapter == 0)
			{
				m_iCurrentChapter = BCVRef.GetChapterFromBcv(m_sectionRefStart);
				if (m_iCurrentChapter != 1)
				{
					// should only be used if no chapter marker is in text and will only be used
					// for the internal verse number references.
					m_sCurrentChapterNumber = m_iCurrentChapter.ToString();
				}
			}

			bool fIntro = section.IsIntro;
			if (fIntro)
			{
				if (!m_fInIntroduction)
					m_writer.WriteStartElement("introduction");
			}
			else
			{
				if (m_fInIntroduction)
				{
					FlushSectionElementStack(SectionLevel.Major);
					m_writer.WriteEndElement();		// </introduction>
				}
			}
			//m_writer.WriteComment(String.Format(
			//    "{0}Section has {1} heading paragraphs and {2} content paragraphs",
			//    fIntro ? "Introduction " : "", section.HeadingOA.ParagraphsOS.Count,
			//    section.ContentOA.ParagraphsOS.Count));
			m_fInIntroduction = fIntro;
			// Write the section heading.
			WriteSectionHeading(section);
			// Write the section contents.
			int cpara = section.ContentOA.ParagraphsOS.Count;
			for (int i = 0; i < cpara; ++i)
			{
				IStPara para = section.ContentOA.ParagraphsOS[i];
				if (para == null)
					continue;
				// Find the next paragraph so that we can detect end-of-verse at the end of the
				// current paragraph by detecting beginning-of-chapter or beginning-of-verse at
				// the beginning of the next paragraph.
				IStPara nextPara = null;
				if (i + 1 < cpara)
				{
					nextPara = section.ContentOA.ParagraphsOS[i + 1];
				}
				else if (nextSection != null &&	nextSection.ContentOA != null &&
					nextSection.ContentOA.ParagraphsOS.Count > 0)
				{
					nextPara = nextSection.ContentOA.ParagraphsOS[0];
				}
				ExportParagraph((IStTxtPara)para, (IStTxtPara)nextPara, true);
				if (fIntro)
					ExportAnnotationsForObj(para.Hvo);
			}
			CloseAnyLeftoverOpenParagraphElements(true);	// close <speech> if it's open
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write any necessary &lt;section&gt; and &lt;title&gt; elements (plus any other)
		/// needed for the paragraphs of the section heading.
		/// </summary>
		/// <param name="section"></param>
		/// ------------------------------------------------------------------------------------
		private void WriteSectionHeading(IScrSection section)
		{
			m_fInSectionHeader = true;
			int cparaHead = section.HeadingOA.ParagraphsOS.Count;
			bool fSectionFromNext = false;
			bool fUnknownStyle = false;
			string stylePrev = null;
			for (int i = 0; i < cparaHead; ++i)
			{
				IStTxtPara para = section.HeadingOA.ParagraphsOS[i] as IStTxtPara;
				string style = para.StyleName;
				bool fTitle = WriteSectionHead(section.HeadingOA.ParagraphsOS, i, style, stylePrev,
					ref fSectionFromNext, out fUnknownStyle);
				if (fTitle)
				{
					if (style == ScrStyleNames.ChapterHead)
						m_writer.WriteStartElement("chapterHead");
					else
						m_writer.WriteStartElement("sectionHead");
				}
				if (style == ScrStyleNames.VariantSectionTail)
					m_writer.WriteAttributeString("type", "tail");	// REVIEW: I think this misunderstands VariantSectionTail!
				else if (style == ScrStyleNames.HebrewTitle)
					m_writer.WriteAttributeString("type", "psalm");
				else if (style == ScrStyleNames.SectionRangeParagraph)
					m_writer.WriteAttributeString("type", "range");
				else if (fUnknownStyle)
				{
					m_writer.WriteAttributeString("type", "userDefined");
					m_writer.WriteAttributeString("subType", style);
				}
				ExportAnnotationsForObj(para.Hvo);
				// It may seem odd to export an empty paragraph, but "Comp Trnr BFB" complained
				// about losing the BT when the vernacular was empty. (which itself is wierd)
				//if (para.Contents.Length > 0)
					ExportParagraph(para, null, false);
				if (fTitle)
					m_writer.WriteEndElement();		// </title>
				if (m_fInSpeech && i < cparaHead - 1)
				{
					// when a Speech Speaker paragraph is written, m_fInSpeech is set to true
					// to indicate that a <speech> element has been left open.  if it's not the
					// last header paragraph, we need to close that element.
					m_writer.WriteEndElement();		// </speech>
					m_fInSpeech = false;
				}
				stylePrev = style;
			}
			if (cparaHead == 0)
			{
				m_writer.WriteStartElement("title");
				m_writer.WriteStartElement("trGroup");
				m_writer.WriteEndElement();
				m_writer.WriteEndElement();
			}
			else
			{
				bool fCloseSpeechIfOpen = true;
				if (m_fInSpeech && section.ContentOA.ParagraphsOS.Count > 0)
				{
					IStPara para = section.ContentOA.ParagraphsOS[0];
					string styleName = para.StyleName;
					if (styleName == ScrStyleNames.SpeechSpeaker ||
						styleName == ScrStyleNames.SpeechLine1 ||
						styleName == ScrStyleNames.SpeechLine2)
					{
						fCloseSpeechIfOpen = false;		// keep the speech open.
					}
				}
				CloseAnyLeftoverOpenParagraphElements(fCloseSpeechIfOpen);
			}
			m_fInSectionHeader = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close any currently open section element that needs to be closed, and open a new
		/// section element if necessary.  (This logic maps from the flat list of sections in
		/// FieldWorks model to the hierarchically embedded sections in the OXES model.)
		/// </summary>
		/// <param name="level"></param>
		/// <param name="sType"></param>
		/// <param name="fFirstPara">Flag indicating whether this is being called in response
		/// to the first paragraph in a section</param>
		/// ------------------------------------------------------------------------------------
		private void FinishAndStartSection(SectionLevel level, string sType, bool fFirstPara)
		{
			if (fFirstPara || m_stackSectionLevels.Count == 0)
			{
				FlushSectionElementStack(level);
				OpenSectionElement(level, sType);
			}
			else
			{
				SectionLevel levelPrev = m_stackSectionLevels.Peek();
				if (levelPrev > level)
				{
					while (m_stackSectionLevels.Count > 0 && m_stackSectionLevels.Peek() > level)
					{
						m_writer.WriteEndElement();		// </section>
						m_stackSectionLevels.Pop();
					}
					if (m_stackSectionLevels.Count == 0)
						OpenSectionElement(level, sType);
				}
				else if (levelPrev < level)
				{
					OpenSectionElement(level, sType);
				}
				// we deliberately stay with the currently open section if the levels are equal.
				// this allows both a <chapterHead> and a <sectionHead> inside a <section>.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open a section element, pushing the given section level onto the stack.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="sType"></param>
		/// ------------------------------------------------------------------------------------
		private void OpenSectionElement(SectionLevel level, string sType)
		{
			m_writer.WriteStartElement("section");
			if (!String.IsNullOrEmpty(sType))
				m_writer.WriteAttributeString("type", sType);
			m_stackSectionLevels.Push(level);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close and open &lt;section&gt; elements as indicated by the paragraph style.  This
		/// is where the linear list of sections in FieldWorks is transformed into a hierarchy
		/// of nested sections in OXES.
		/// </summary>
		/// <param name="paras">the vector of header paragraphs</param>
		/// <param name="i">current index into paras</param>
		/// <param name="style">the style of the current paragraph</param>
		/// <param name="stylePrev"></param>
		/// <param name="fSectionFromNext">flag whether this paragraph has already been handled</param>
		/// <param name="fUnknownStyle"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool WriteSectionHead(IFdoOwningSequence<IStPara> paras, int i, string style,
			string stylePrev, ref bool fSectionFromNext, out bool fUnknownStyle)
		{
			fUnknownStyle = false;
			if (fSectionFromNext)
			{
				fSectionFromNext = false;
				return true;
			}
			switch (style)
			{
				case ScrStyleNames.SectionHeadMajor:
					FinishAndStartSection(SectionLevel.Major, "major", i == 0);
					return true;
				case ScrStyleNames.SectionHead:
				case ScrStyleNames.ChapterHead:
					FinishAndStartSection(SectionLevel.Normal, null, i == 0);
					return true;
				case ScrStyleNames.SectionHeadMinor:
					FinishAndStartSection(SectionLevel.Minor, "minor", i == 0);
					return true;
				case ScrStyleNames.SectionHeadSeries:
					FinishAndStartSection(SectionLevel.Series, "series", i == 0);
					return true;
				case ScrStyleNames.IntroSectionHead:
					FinishAndStartSection(SectionLevel.Normal, null, i == 0);
					return true;
				case ScrStyleNames.VariantSectionHead:
				case ScrStyleNames.VariantSectionTail:	// I don't think this is a heading style!
					FinishAndStartSection(SectionLevel.Normal, "variant", i == 0);
					return true;
				// the remaining styles are more complicated, and may need to look ahead one
				// paragraph to close/open <section> elements properly.
				case ScrStyleNames.SpeechSpeaker:
					fSectionFromNext = HandleSpeechSpeaker(paras, i);
					return false;
				case ScrStyleNames.ParallelPassageReference:
					fSectionFromNext = HandleSecondaryTitle(paras, i, style, stylePrev);
					return false;
				case ScrStyleNames.HebrewTitle:
					fSectionFromNext = HandleSecondaryTitle(paras, i, style, stylePrev);
					return true;
				case ScrStyleNames.SectionRangeParagraph:
					fSectionFromNext = HandleSecondaryTitle(paras, i, style, stylePrev);
					return true;
				default:
					FinishAndStartSection(SectionLevel.Normal, null, i == 0);
					fUnknownStyle = true;
					return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle section nesting for a "Speech Speaker" style paragraph.
		/// </summary>
		/// <param name="paras">sequence of paragraphs</param>
		/// <param name="iPara">index to the current paragraph</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool HandleSpeechSpeaker(IFdoOwningSequence<IStPara> paras, int iPara)
		{
			if (iPara == paras.Count - 1)
			{
				FlushSectionElementStack(SectionLevel.Series);
				m_writer.WriteStartElement("section");
				m_stackSectionLevels.Push(SectionLevel.Series);
			}
			else if (iPara == 0)
			{
				// we need to pump out a <section> element, but not another one from
				// the following header!  (this is probably bad data, but...)
				bool fSkip = false;
				bool fUnknown;
				string styleNext = paras[1].StyleName;
				bool fSectionFromNext = WriteSectionHead(paras, 1, styleNext,
					ScrStyleNames.SpeechSpeaker, ref fSkip, out fUnknown);
				if (!fSectionFromNext)
				{
					// Start a new section at the same level.
					StartNewSection();
				}
				return fSectionFromNext;
			}
			else
			{
				// REVIEW: what to do?
				Debug.Assert(iPara == 0 || iPara == paras.Count - 1, "SpeechSpeaker style is out of place!");
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle section nesting for a "Hebrew Title" or "Parallel Passage Reference" style
		/// paragraph.
		/// </summary>
		/// <param name="paras">the vector of header paragraphs</param>
		/// <param name="iPara">current index into paras</param>
		/// <param name="style">the style of the current paragraph</param>
		/// <param name="stylePrev">The style of the previous paragraph</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool HandleSecondaryTitle(IFdoOwningSequence<IStPara> paras, int iPara, string style,
			string stylePrev)
		{
			if (iPara == 0)
			{
				if (paras.Count == 1)
				{
					// Start a new section at the same level.
					StartNewSection();
				}
				else
				{
					// we need to pump out a <section> element, but not another one from
					// the following header!  (this is probably bad data, but...)
					bool fSkip = false;
					bool fUnknown;
					string styleNext = paras[1].StyleName;
					bool fSectionFromNext = WriteSectionHead(paras, 1, styleNext, style,
						ref fSkip, out fUnknown);
					if (!fSectionFromNext)
					{
						// Start a new section at the same level.
						StartNewSection();
					}
					return fSectionFromNext;
				}
			}
			else
			{
				// If we follow a normal section heading, then we're okay.
				if (stylePrev == ScrStyleNames.SectionHead ||
					stylePrev == ScrStyleNames.ChapterHead ||
					stylePrev == ScrStyleNames.SectionHeadMajor ||
					stylePrev == ScrStyleNames.SectionHeadMinor ||
					stylePrev == ScrStyleNames.SectionHeadSeries)
				{
					return false;	// don't need to do anything.
				}
				else
				{
					// TODO (TE-7289): It's okay for Parallel Passage to be the only para in a section head
					Debug.Assert(iPara == 0, String.Format("{0} style is out of place!", style));
				}
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close the current section and start a new section at the same level as the current one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StartNewSection()
		{
			SectionLevel lev;
			if (m_stackSectionLevels.Count > 0)
				lev = m_stackSectionLevels.Peek();
			else
				lev = SectionLevel.Series;
			FlushSectionElementStack(lev);
			m_writer.WriteStartElement("section");
			m_stackSectionLevels.Push(lev);
			switch (lev)
			{
				case SectionLevel.Major:
					m_writer.WriteAttributeString("type", "major");
					break;
				case SectionLevel.Minor:
					m_writer.WriteAttributeString("type", "minor");
					break;
				case SectionLevel.Series:
					m_writer.WriteAttributeString("type", "series");
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write end elements for however deeply our section/subsection/subsubsection structure
		/// has gotten nested.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FlushSectionElementStack(SectionLevel level)
		{
			while (m_stackSectionLevels.Count > 0)
			{
				SectionLevel topLevel = m_stackSectionLevels.Peek();
				if (level <= topLevel)
				{
					m_writer.WriteEndElement();
					m_stackSectionLevels.Pop();
				}
				else
				{
					return;
				}
			}
		}

#if false // cs169
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check whether this given paragraph begins with a "Chapter Number" character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ParaBeginsChapter(IStTxtPara para)
		{
			if (para == null)
				return false;
			if (para.Contents == null || para.Contents.Length == 0)
				return false;
			ITsString tss = para.Contents;
			ITsTextProps ttp = tss.get_Properties(0);
			string styleName = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			return styleName == ScrStyleNames.ChapterNumber;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check whether the given paragraph begins with a "Verse Number" character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool ParaBeginsVerse(IStTxtPara para)
		{
			if (para == null)
				return false;
			if (para.Contents == null || para.Contents.Length == 0)
				return false;
			ITsString tss = para.Contents;
			ITsTextProps ttp = tss.get_Properties(0);
			string styleName = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			return styleName == ScrStyleNames.VerseNumber;
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store the information for one segment of a segmented back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal class BTSegment
		{
			private readonly int m_ichBegin;
			private readonly int m_ichEnd;
			private readonly Dictionary<int, ITsString> m_mapWsTss = new Dictionary<int, ITsString>();
			private readonly Dictionary<int, string> m_mapWsStatus = new Dictionary<int, string>();
			private readonly ITsString m_text;
			private readonly ICmObject m_obj;
			private readonly bool m_isLabel;

			internal BTSegment(ISegment seg, ICmObject obj)
			{
				m_text = seg.BaselineText;
				m_isLabel = seg.IsLabel;
				m_ichBegin = seg.BeginOffset;
				m_ichEnd = seg.EndOffset;
				m_obj = obj;
			}

			internal BTSegment(ITsString tss, int ichBegin, int ichEnd, ICmObject obj)
			{
				m_text = tss;
				m_isLabel = false;
				m_ichBegin = ichBegin;
				m_ichEnd = ichEnd;
				m_obj = obj;
			}

			internal ITsString Text
			{
				get { return m_text; }
			}

			internal bool IsLabel
			{
				get { return m_isLabel; }
			}

			internal int BeginOffset
			{
				get { return m_ichBegin; }
			}

			internal int EndOffset
			{
				get { return m_ichEnd; }
			}

			internal void SetTransForWs(int ws, ITsString tss)
			{
				ITsString tss1;
				if (m_mapWsTss.TryGetValue(ws, out tss1))
					m_mapWsTss[ws] = tss;
				else
					m_mapWsTss.Add(ws, tss);
			}

			internal ITsString GetTransForWs(int ws)
			{
				ITsString tss;
				if (m_mapWsTss.TryGetValue(ws, out tss))
					return tss;
				return null;
			}

			internal void SetBtStatusForWs(int ws, string status)
			{
				if (m_mapWsStatus.ContainsKey(ws))
					m_mapWsStatus[ws] = status;
				else
					m_mapWsStatus.Add(ws, status);
			}

			internal string GetBtStatusForWs(int ws)
			{
				string status;
				if (m_mapWsStatus.TryGetValue(ws, out status))
					return status;
				return null;
			}

			internal List<int> AvailableTranslations
			{
				get { return new List<int>(m_mapWsTss.Keys); }
			}

			internal ICmObject TargetObject
			{
				get { return m_obj; }
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store the information needed for a back translation, either from an ICmTranslation or
		/// from a ICmPicture.Caption
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal class BackTranslationInfo
		{
			private Dictionary<int, ITsString> m_mapWsTssTran = new Dictionary<int, ITsString>();
			private Dictionary<int, string> m_mapWsTssStatus = new Dictionary<int, string>();
			int m_hvo;

			internal BackTranslationInfo(ICmTranslation backtran)
			{
				foreach (IWritingSystem ws in backtran.Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems)
				{
					ITsString tss = backtran.Translation.get_String(ws.Handle);
					if (tss.Length > 0 && tss != backtran.Translation.NotFoundTss)
						m_mapWsTssTran.Add(ws.Handle, tss);
					string status = backtran.Status.get_String(ws.Handle).Text;
					if (!String.IsNullOrEmpty(status) && status != backtran.Status.NotFoundTss.Text)
						m_mapWsTssStatus.Add(ws.Handle, status);
				}
				m_hvo = backtran.Hvo;
			}

			internal BackTranslationInfo(ICmPicture pict)
			{
				foreach (IWritingSystem ws in pict.Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems)
				{
					ITsString tss = pict.Caption.get_String(ws.Handle);
					if (tss.Length > 0 && tss != pict.Caption.NotFoundTss)
						m_mapWsTssTran.Add(ws.Handle, tss);
				}
				m_hvo = pict.Hvo;
			}

			internal ITsString GetBackTransForWs(int ws)
			{
				ITsString tss;
				return (m_mapWsTssTran.TryGetValue(ws, out tss)) ? tss : null;
			}

			internal string GetStatusForWs(int ws)
			{
				string status;
				return (m_mapWsTssStatus.TryGetValue(ws, out status)) ? status : null;
			}

			internal int Hvo
			{
				get { return m_hvo; }
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a single paragraph.
		/// </summary>
		/// <param name="para">the paragraph</param>
		/// <param name="nextPara">the next paragraph (possibly in the next section) if there
		/// is one</param>
		/// <param name="fNullParaIsEndOfBook">flag whether to treat an empty next paragraph as
		/// the end of the book</param>
		/// ------------------------------------------------------------------------------------
		private void ExportParagraph(IStTxtPara para, IStTxtPara nextPara, bool fNullParaIsEndOfBook)
		{
			Debug.Assert(para.StyleRules != null, "Paragraph StyleRules should never be null");
			string styleName = para.StyleName;
			OxesInfo xinfo = OxesInfo.GetOxesInfoForParaStyle(styleName, m_fInSectionHeader);

			BackTranslationInfo backtran = GetBackTranslationForPara(para); // "normal" back translation.
			List<BTSegment> rgBTSeg = GetSegmentedBTInfo(para);
			Debug.Assert(rgBTSeg.Count > 0 || para.SegmentsOS.Count == 0);

			CloseNestingElementsAsNeeded(styleName, xinfo);
			if (OpenNestingElementsAsNeeded(para, styleName, xinfo))
				return;		// some nesting elements (eg "row") have no content.

			if (HandleHeaderParagraph(para, styleName, xinfo, backtran, rgBTSeg))
				return;		// some header paragraphs are handled completely.

			// Special handling for List Item N / List Item N Additional
			bool fKeepOpen = HandleListItemParagraphs(nextPara, styleName);

			m_writer.WriteStartElement(xinfo.XmlTag);
			if (InFootnote)
				WriteParagraphAttribute(para, xinfo.XmlTag, "noteID", "f" + m_sCurrentBookId + m_footnoteIndex);
			WriteParagraphAttribute(para, xinfo.XmlTag, xinfo.AttrName, xinfo.AttrValue);
			WriteParagraphAttribute(para, xinfo.XmlTag, xinfo.AttrName2, xinfo.AttrValue2);
			if (InFootnote && xinfo.XmlTag == "note" && xinfo.AttrName == "type")
				OpenTrGroupIfNeeded();

			if (m_fInIntroduction)
			{
				ExportAnnotationsForObj(para.Hvo);
				if (backtran != null && rgBTSeg.Count == 0)
					ExportAnnotationsForObj(backtran.Hvo);
			}
			ExportParagraphData(para.Contents, rgBTSeg);

			if (InFootnote && xinfo.XmlTag == "note" && xinfo.AttrName == "type")
				CloseTrGroupIfNeeded();

			if (NextParaStartsChapter(nextPara, fNullParaIsEndOfBook))
				WriteChapterEndMilepostIfNeeded();
			else if (NextParaStartsVerse(nextPara, fNullParaIsEndOfBook))
				WriteVerseEndMilepostIfNeeded();
			if (!fKeepOpen)
			{
				m_writer.WriteEndElement();		// xinfo.XmlTag
			}
			else
			{
				Debug.Assert(m_fListItem1Open || m_fListItem2Open || InFootnote);
			}
		}

		private List<BTSegment> GetSegmentedBTInfo(IStTxtPara para)
		{
			List<BTSegment> rgBTSeg = new List<BTSegment>();
			if (para == null || !para.IsValidObject)
				return rgBTSeg;

			// Backtranslation must exist if segmented BT exists
			ICmTranslation backtran = para.GetBT();
			if (backtran == null)
				return rgBTSeg;

			// create list of WS used for back translations.
			List<int> rgWsBt = m_dictAnalLangs.Keys.ToList();

			// Segment all BTs that aren't currently segmented
			if (rgWsBt.Count > 0)
			{
				foreach (ISegment segment in para.SegmentsOS)
				{
					BTSegment bts = new BTSegment(segment, para);
					foreach (int ws in rgWsBt)
					{
						ITsString tss = segment.FreeTranslation.get_String(ws);
						bts.SetTransForWs(ws, tss);
						string status = backtran.Status.get_String(ws).Text;
						if (!String.IsNullOrEmpty(status))
							bts.SetBtStatusForWs(ws, status);
					}
					rgBTSeg.Add(bts);
				}
			}
			return rgBTSeg;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close any open nesting elements (such as &lt;embedded&gt; or &lt;speech&gt;) that
		/// are no longer required by the context.
		/// </summary>
		/// <param name="styleName"></param>
		/// <param name="xinfo"></param>
		/// ------------------------------------------------------------------------------------
		private void CloseNestingElementsAsNeeded(string styleName, OxesInfo xinfo)
		{
			if (m_stackTableElements.Count > 0 && !InFootnote &&
				xinfo.Context != OxesContext.Table && xinfo.Context != OxesContext.Row)
			{
				FinishTable();
			}
			if (m_fListItem1Open && !InFootnote && styleName != ScrStyleNames.ListItem1Additional)
			{
				m_writer.WriteEndElement();	// </item>
				m_fListItem1Open = false;
			}
			if (m_fListItem2Open && !InFootnote && styleName != ScrStyleNames.ListItem2Additional)
			{
				m_writer.WriteEndElement();	// </item>
				m_fListItem2Open = false;
			}
			if (m_fEmbedded && !InFootnote && xinfo.Context != OxesContext.Embedded)
			{
				m_writer.WriteEndElement();	// </embedded>
				m_fEmbedded = false;
			}
			if (m_fInSpeech && !InFootnote && xinfo.Context != OxesContext.Speech)
			{
				m_writer.WriteEndElement();	// </speech>
				m_fInSpeech = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open any nesting elements (such as &lt;embedded&gt; or &lt;speech&gt;) that are
		/// required by the context.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="styleName"></param>
		/// <param name="xinfo"></param>
		/// <returns>true if opening an element is all that should be done</returns>
		/// ------------------------------------------------------------------------------------
		private bool OpenNestingElementsAsNeeded(IStTxtPara para, string styleName, OxesInfo xinfo)
		{
			switch (xinfo.Context)
			{
				case OxesContext.Embedded:
					StartEmbeddingIfNeeded();
					return false;
				case OxesContext.Table:
					Debug.Assert(styleName == ScrStyleNames.TableRow);
					StartTableRow();
					if (para.Contents.Length > 0)
					{
						// REVIEW: warn user??
					}
					return true;	// this style is not supposed to have any content!
				case OxesContext.Row:
					StartTableAndRowIfNeeded();
					return false;
				case OxesContext.Speech:
					StartSpeechIfNeeded();
					return false;
				default:
					return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Many header paragraphs need special handling.  Some are handled completely by this
		/// method.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="styleName"></param>
		/// <param name="xinfo"></param>
		/// <param name="backtran"></param>
		/// <param name="rgBTSeg">list of segments for a segmented back translation</param>
		/// <returns>true iff the paragraph is completely exported by this method</returns>
		/// ------------------------------------------------------------------------------------
		private bool HandleHeaderParagraph(IStTxtPara para, string styleName, OxesInfo xinfo,
			BackTranslationInfo backtran, List<BTSegment> rgBTSeg)
		{
			if (xinfo.IsHeading)
			{
				if (styleName == ScrStyleNames.MainBookTitle)
				{
					bool fExportAnnotation = false;
					ExportTitleParagraph(para, backtran, ref fExportAnnotation);
					return true;
				}
				else if (styleName == ScrStyleNames.ParallelPassageReference)
				{
					// necessary markup is table driven
					return false;
				}
				else if (m_fInSectionHeader && styleName != ScrStyleNames.SpeechSpeaker)
				{
					// The XML markup for these styles are handled by the caller.
					ExportParagraphData(para.Contents, rgBTSeg);
					return true;
				}
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Since OXES nests paragraphs with style "List Item Continuation N" are nested inside
		/// the preceding paragraphs with style "List Item N" (for N = 1 or 2), special handling
		/// is needed for these types of paragraphs.  This methods sets the flags needed for the
		/// state machine, and may possibly enclosing paragraphs in order to write valid XML
		/// that preserves the styles across export and import.
		/// </summary>
		/// <param name="nextPara"></param>
		/// <param name="styleName"></param>
		/// <returns>
		/// true if the start element should be kept open, false if it should be closed
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool HandleListItemParagraphs(IStTxtPara nextPara, string styleName)
		{
			if (nextPara == null)
				return false;

			if (styleName == ScrStyleNames.ListItem1)
			{
				string nextStyleName = nextPara.StyleName;
				if (nextStyleName == ScrStyleNames.ListItem1Additional)
				{
					m_fListItem1Open = true;
					return true;
				}
			}
			else if (styleName == ScrStyleNames.ListItem1Additional)
			{
				if (!m_fListItem1Open)
				{
					m_fListItem1Open = true;
					m_writer.WriteStartElement("item");
					m_writer.WriteAttributeString("level", "1");
				}
			}
			else if (styleName == ScrStyleNames.ListItem2)
			{
				string nextStyleName = nextPara.StyleName;
				if (nextStyleName == ScrStyleNames.ListItem2Additional)
				{
					m_fListItem2Open = true;
					return true;
				}
			}
			else if (styleName == ScrStyleNames.ListItem2Additional)
			{
				if (!m_fListItem2Open)
				{
					m_fListItem2Open = true;
					m_writer.WriteStartElement("item");
					m_writer.WriteAttributeString("level", "2");
				}
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write the given attribute for the paragraph's XML element, if a valid name and value
		/// both exist.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="sXmlTag"></param>
		/// <param name="sAttrName"></param>
		/// <param name="sAttrValue"></param>
		/// ------------------------------------------------------------------------------------
		private void WriteParagraphAttribute(IStTxtPara para, string sXmlTag, string sAttrName, string sAttrValue)
		{
			if (!String.IsNullOrEmpty(sAttrName))
			{
				if (!String.IsNullOrEmpty(sAttrValue))
				{
					m_writer.WriteAttributeString(sAttrName, sAttrValue);
				}
				else
				{
					string sVal = GetAttributeValue(para, sXmlTag, sAttrName);
					if (!String.IsNullOrEmpty(sVal))
						m_writer.WriteAttributeString(sAttrName, sVal);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The attribute name is given for the element, but not its value.  See if we
		/// can guess what the value should be.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="sXmlTag"></param>
		/// <param name="sAttrName"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private string GetAttributeValue(IStTxtPara para, string sXmlTag, string sAttrName)
		{
			switch (sXmlTag)
			{
				case "cell":
				case "head":
					if (sAttrName == "align")
					{
						int nVar;
						int nVal;
						bool fBaseRTL = false;
						if (para.Owner is IStText)
						{
							fBaseRTL = ((IStText)para.Owner).RightToLeft;
						}
						else if (para.Contents != null)
						{
							ITsTextProps ttp = para.Contents.get_PropertiesAt(0);
							nVal = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
							if (nVal > 0)
								fBaseRTL = m_cache.ServiceLocator.WritingSystemManager.Get(nVal).RightToLeftScript;
						}
						nVal = para.StyleRules.GetIntPropValues((int)FwTextPropType.ktptRightToLeft, out nVar);
						bool fStyleRTL = nVal != 0;
						return (fBaseRTL == fStyleRTL) ? "start" : "end";
					}
					break;
				case "note":
					if (sAttrName == "canonical")
					{
						return m_fInIntroduction ? "false" : "true";
					}
					break;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This enum is used in the state machine for writing title paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private enum TitleType
		{
			kttNone,
			kttMain,
			kttSecondary,
			kttTertiary
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is different than other paragraphs because the Title Secondary and Title Tertiary
		/// character styles must generate outer &lt;title&gt; elements instead of being embedded
		/// inside the &lt;tr&gt; element.  We also don't expect chapter and verse numbers in this
		/// context!  However, we may have footnotes.
		/// </summary>
		/// <param name="para">title paragraph</param>
		/// <param name="backtran">back translation of title paragraph</param>
		/// <param name="fExportAnnotation"><c>true</c> to export annotations associated with
		/// the title; <c>false</c> otherwise</param>
		/// <remarks>
		/// This is even trickier, because the back translation should be grouped with the first
		/// "main" type title section.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		private void ExportTitleParagraph(IStTxtPara para, BackTranslationInfo backtran,
			ref bool fExportAnnotation)
		{
			Dictionary<int, ITsString> dictBackTrans;
			Dictionary<int, int> mapWsBackTransRunIndex;
			Dictionary<int, string> mapWsStatus = FillBackTransForPara(backtran,
				out dictBackTrans, out mapWsBackTransRunIndex);
			ITsString tssPara = para.Contents;
			if (tssPara.Length > 0)
			{
				int runCount = tssPara.RunCount;
				List<int> rgws;
				List<OxesInfo> rgInfo = CollectRunInfo(tssPara, out rgws);
				TitleType ttState = TitleType.kttNone;
				for (int run = 0; run < runCount; ++run)
				{
					string data = tssPara.get_RunText(run);
					if (!String.IsNullOrEmpty(data))
						data = data.Normalize();
					OxesInfo xinfo = rgInfo[run];
					int ws = rgws[run];
					switch (xinfo.StyleName)
					{
						default:
							if (IsObjectReference(data))
							{
								// This is presumably a footnote.
								if (ttState == TitleType.kttNone)
								{
									m_writer.WriteStartElement("title");
									m_writer.WriteAttributeString("type", "main");
									ttState = TitleType.kttMain;
									if (fExportAnnotation)
									{
										ExportAnyRelatedAnnotation();
										fExportAnnotation = false;
									}
								}
								ITsTextProps props = tssPara.get_Properties(run);
								ExportEmbeddedObject(props);
							}
							else
							{
								if (ttState == TitleType.kttSecondary || ttState == TitleType.kttTertiary)
									m_writer.WriteEndElement();		// xinfo.XmlTag (</title>)
								if (ttState != TitleType.kttMain)
								{
									m_writer.WriteStartElement("title");
									m_writer.WriteAttributeString("type", "main");
									ttState = TitleType.kttMain;
									if (fExportAnnotation)
									{
										ExportAnyRelatedAnnotation();
										fExportAnnotation = false;
									}
								}
								OpenTrGroupIfNeeded();
								OpenTranslationElementIfNeeded();
								WriteRunDataWithEmbeddedStyleInfo(rgInfo, rgws, run, m_cache.DefaultVernWs, data);
								CloseTranslationElementIfNeeded();
								WriteTitleBackTrans(mapWsStatus, dictBackTrans, mapWsBackTransRunIndex, ttState);
								CloseTrGroupIfNeeded();
							}
							break;
						case ScrStyleNames.SecondaryBookTitle:
							ttState = WriteSubtitle(dictBackTrans, mapWsBackTransRunIndex, mapWsStatus, ttState, data,
								xinfo, ws, TitleType.kttSecondary, ref fExportAnnotation);
							break;
						case ScrStyleNames.TertiaryBookTitle:
							ttState = WriteSubtitle(dictBackTrans, mapWsBackTransRunIndex, mapWsStatus, ttState, data,
								xinfo, ws, TitleType.kttTertiary, ref fExportAnnotation);
							break;
					}
				}
				if (ttState != TitleType.kttNone)
				{
					WriteTitleBackTrans(mapWsStatus, dictBackTrans, mapWsBackTransRunIndex, ttState);
					m_writer.WriteEndElement();		// </title>
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write out either a secondary or tertiary title segment as a separate &lt;title&gt;
		/// element, taking footnotes into account.
		/// </summary>
		/// <param name="dictBackTrans"></param>
		/// <param name="mapWsBackTransRunIndex"></param>
		/// <param name="mapWsStatus"></param>
		/// <param name="ttState"></param>
		/// <param name="data"></param>
		/// <param name="xinfo"></param>
		/// <param name="ws"></param>
		/// <param name="ttWant"></param>
		/// <param name="fExportAnnotation"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private TitleType WriteSubtitle(Dictionary<int, ITsString> dictBackTrans,
			Dictionary<int, int> mapWsBackTransRunIndex, Dictionary<int, string> mapWsStatus, TitleType ttState,
			string data, OxesInfo xinfo, int ws, TitleType ttWant, ref bool fExportAnnotation)
		{
			if (ttState != ttWant)
			{
				if (ttState != TitleType.kttNone)
					m_writer.WriteEndElement();		// </title>
				m_writer.WriteStartElement(xinfo.XmlTag);
				m_writer.WriteAttributeString(xinfo.AttrName, xinfo.AttrValue);
				if (fExportAnnotation)
				{
					ExportAnyRelatedAnnotation();
					fExportAnnotation = false;
				}
			}
			OpenTrGroupIfNeeded();
			OpenTranslationElementIfNeeded();
			bool fForeign = MarkForeignIfNeeded(m_cache.DefaultVernWs, ws);
			m_writer.WriteString(data);
			if (fForeign)
				m_writer.WriteEndElement();	// </foreign>
			CloseTranslationElementIfNeeded();
			WriteTitleBackTrans(mapWsStatus, dictBackTrans, mapWsBackTransRunIndex, ttWant);
			CloseTrGroupIfNeeded();
			return ttWant;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write out the current back translation segment of the title that matches the given TitleState
		/// (main, secondary, or tertiary).
		/// </summary>
		/// <param name="mapWsStatus"></param>
		/// <param name="dictBackTrans"></param>
		/// <param name="mapWsBackTransRunIndex"></param>
		/// <param name="ttState"></param>
		/// ------------------------------------------------------------------------------------
		private void WriteTitleBackTrans(Dictionary<int, string> mapWsStatus, Dictionary<int, ITsString> dictBackTrans,
			Dictionary<int, int> mapWsBackTransRunIndex, TitleType ttState)
		{
			bool fCloseTrGroup = false;
			foreach (int ws in dictBackTrans.Keys)
			{
				ITsString tss = dictBackTrans[ws];
				int irun = mapWsBackTransRunIndex[ws];
				bool fBackTransOpen = false;
				for (; irun < tss.RunCount; ++irun)
				{
					string data = tss.get_RunText(irun);
					if (!String.IsNullOrEmpty(data))
						data = data.Normalize();
					if (IsObjectReference(data))
					{
						// presumably a footnote.
						if (fBackTransOpen)
							break;		// stop at a trailing footnote.
						else
							continue;	// skip a leading footnote.
					}
					ITsTextProps ttp = tss.get_Properties(irun);
					string styleName = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
					bool fImpliedStyle = false;
					if (styleName == ScrStyleNames.SecondaryBookTitle)
					{
						if (ttState != TitleType.kttSecondary)
							break;
						fImpliedStyle = true;
					}
					else if (styleName == ScrStyleNames.TertiaryBookTitle)
					{
						if (ttState != TitleType.kttTertiary)
							break;
						fImpliedStyle = true;
					}
					else
					{
						if (ttState != TitleType.kttMain)
							break;
					}
					if (OpenTrGroupIfNeeded())
						fCloseTrGroup = true;
					if (!fBackTransOpen)
					{
						string status;
						mapWsStatus.TryGetValue(ws, out status);
						MarkBackTranslation(ws, status, false);
						fBackTransOpen = true;
						m_writer.Formatting = Formatting.None;
					}
					OxesInfo xinfo = OxesInfo.GetOxesInfoForCharStyle(styleName);
					if (!fImpliedStyle && !String.IsNullOrEmpty(xinfo.XmlTag))
					{
						m_writer.WriteStartElement(xinfo.XmlTag);
						if (!String.IsNullOrEmpty(xinfo.AttrName) && !String.IsNullOrEmpty(xinfo.AttrValue))
							m_writer.WriteAttributeString(xinfo.AttrName, xinfo.AttrValue);
					}
					m_writer.WriteString(data);
					if (!fImpliedStyle && !String.IsNullOrEmpty(xinfo.XmlTag))
						m_writer.WriteEndElement();
				}
				if (fBackTransOpen)
				{
					m_writer.WriteEndElement();
					if (!InFootnote) // Don't want to add whitespace if we're in a footnote or picture since they need to be inline.
						m_writer.Formatting = Formatting.Indented;
				}
				mapWsBackTransRunIndex[ws] = irun;
			}
			if (fCloseTrGroup)
				CloseTrGroupIfNeeded();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collect all the character style information for all the runs.  This is needed to
		/// facilitate embedding one character style inside another in the XML.
		/// </summary>
		/// <param name="tssPara"></param>
		/// <param name="rgws"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static List<OxesInfo> CollectRunInfo(ITsString tssPara, out List<int> rgws)
		{
			int runCount = tssPara.RunCount;
			List<OxesInfo> rgInfo = new List<OxesInfo>(runCount);
			rgws = new List<int>(runCount);
			for (int run = 0; run < runCount; ++run)
			{
				ITsTextProps props = tssPara.get_Properties(run);
				string styleName = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				OxesInfo xinfo = OxesInfo.GetOxesInfoForCharStyle(styleName);
				rgInfo.Add(xinfo);
				int nVar;
				int ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
				rgws.Add(ws);
			}
			return rgInfo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We're now in a speech context.  If the state machine variable doesn't already
		/// indicate it, set it and open a &lt;speech&gt; element.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StartSpeechIfNeeded()
		{
			if (!m_fInSpeech)
			{
				m_fInSpeech = true;
				m_writer.WriteStartElement("speech");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We're now in an "embedded" context.  If the state machine variable doesn't already
		/// indicate it, set it and open a &lt;embedded&gt; element.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StartEmbeddingIfNeeded()
		{
			if (!m_fEmbedded)
			{
				m_fEmbedded = true;
				m_writer.WriteStartElement("embedded");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close off a table, if one is open.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FinishTable()
		{
			while (m_stackTableElements.Count > 0)
			{
				m_writer.WriteEndElement();
				m_stackTableElements.Pop();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open up a table, and a row in that table.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StartTableAndRow()
		{
			m_writer.WriteStartElement("table");
			m_stackTableElements.Push(TableElement.Table);
			m_writer.WriteStartElement("row");
			m_stackTableElements.Push(TableElement.Row);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open up a table, and a row in that table, if a table is not yet open.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StartTableAndRowIfNeeded()
		{
			if (m_stackTableElements.Count == 0)
				StartTableAndRow();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open up a row in a table, closing off an existing row if one exists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StartTableRow()
		{
			if (m_stackTableElements.Count == 0)
			{
				StartTableAndRow();
			}
			else
			{
				TableElement topElement = m_stackTableElements.Peek();
				Debug.Assert(topElement == TableElement.Row);
				if (topElement == TableElement.Row)
					m_writer.WriteEndElement();
				m_stackTableElements.Pop();
				Debug.Assert(m_stackTableElements.Count == 1);
				Debug.Assert(m_stackTableElements.Peek() == TableElement.Table);
				m_writer.WriteStartElement("row");
				m_stackTableElements.Push(TableElement.Row);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the first translation for the paragraph that is marked as a back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static BackTranslationInfo GetBackTranslationForPara(IStTxtPara para)
		{
			foreach (ICmTranslation tran in para.TranslationsOC)
			{
				if (tran.TypeRA.Guid == LangProjectTags.kguidTranBackTranslation)
					return new BackTranslationInfo(tran);
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check whether the given paragraph begins with a "Chapter Number" character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool NextParaStartsChapter(IStTxtPara nextPara, bool fNullParaIsEndOfBook)
		{
			if (nextPara == null)
				return fNullParaIsEndOfBook;
			return NextParaStartsWithGivenStyle(nextPara, ScrStyleNames.ChapterNumber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check whether the given paragraph begins with a "Verse Number" character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool NextParaStartsVerse(IStTxtPara nextPara, bool fNullParaIsEndOfBook)
		{
			if (nextPara == null)
				return fNullParaIsEndOfBook;
			return NextParaStartsWithGivenStyle(nextPara, ScrStyleNames.VerseNumber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check whether the given paragraph begins with the given character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static bool NextParaStartsWithGivenStyle(IStTxtPara nextPara, string sStyleWanted)
		{
			ITsString tssPara = nextPara.Contents;
			int crun = tssPara.RunCount;
			if (crun > 0)
			{
				ITsTextProps ttp = tssPara.get_Properties(0);
				string styleName = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				return styleName == sStyleWanted;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the embedded object is a footnote or picture, write it out.
		/// </summary>
		/// <param name="ttp">Properties contain information about the embedded object</param>
		/// ------------------------------------------------------------------------------------
		private void ExportEmbeddedObject(ITsTextProps ttp)
		{
			string objData = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);
			// if ORC doesn't have properties, continue with next run.
			if (!String.IsNullOrEmpty(objData))
			{
				switch (objData[0])
				{
					case (char)FwObjDataTypes.kodtOwnNameGuidHot:
					case (char)FwObjDataTypes.kodtNameGuidHot:
						Guid footnoteGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
						IScrFootnote footnote;
						if (m_cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().TryGetFootnote(footnoteGuid, out footnote))
						{
							ExportFootnote(footnote);
						}
						break;
					case (char)FwObjDataTypes.kodtGuidMoveableObjDisp:
						Guid pictureGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
						ICmPicture picture;
						if (m_cache.ServiceLocator.GetInstance<ICmPictureRepository>().TryGetObject(pictureGuid, out picture))
							ExportPicture(picture);
						break;
					case (char)FwObjDataTypes.kodtPictEvenHot:
						m_writer.WriteComment("object type PictEven not handled");
						break;
					case (char)FwObjDataTypes.kodtPictOddHot:
						m_writer.WriteComment("object type PictOdd not handled");
						break;
					case (char)FwObjDataTypes.kodtExternalPathName:
						m_writer.WriteComment("object type ExternalPathName not handled");
						break;
					case (char)FwObjDataTypes.kodtEmbeddedObjectData:
						m_writer.WriteComment("object type EmbeddedObjectData not handled");
						break;
					case (char)FwObjDataTypes.kodtContextString:
						m_writer.WriteComment("object type ContextString not handled");
						break;
					default:
						m_writer.WriteComment(
							String.Format("unknown object type ({0}) not handled", (int)objData[0]));
						break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write out a reference to a footnote. A reference should only be exported from a back
		/// translation.
		/// </summary>
		/// <param name="ttp">Properties contain information about the embedded object</param>
		/// ------------------------------------------------------------------------------------
		private void ExportRefToEmbeddedObject(ITsTextProps ttp)
		{
			string objData = ttp.GetStrPropValue((int) FwTextPropType.ktptObjData);
			// if ORC doesn't have properties, continue with next run.
			if (!String.IsNullOrEmpty(objData))
			{
				switch (objData[0])
				{
					case (char) FwObjDataTypes.kodtNameGuidHot:
						IScrFootnote footnote;
						if (!GetReferencedFootnote(ttp, out footnote))
							return; // unable to find referenced footnote

						m_writer.WriteStartElement("note");
						m_writer.WriteAttributeString("noteRef", "f" + m_sCurrentBookId +
							(footnote.IndexInOwner + 1));
						m_writer.WriteEndElement(); // end <note> element
						break;
					default:
						// give warning about dropped pictures in BT, but only give it once.
						if (!m_droppedBtPictureWarningGiven)
						{
							MessageBoxUtils.Show(
								"OXES file format does not support pictures in back translation. Location of pictures in BT will be lost.",
								m_app.ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Information);
							m_droppedBtPictureWarningGiven = true;
						}
						break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the referenced footnote given the properties of a back translation text run
		/// containing an Object Replacement Character (ORC).
		/// </summary>
		/// <param name="ttp">The text properties of the footnote.</param>
		/// <param name="footnote">[out] footnote referenced in the text properties or null
		/// if footnote is not found.</param>
		/// <returns><c>true</c> if the footnote was located; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private bool GetReferencedFootnote(ITsTextProps ttp, out IScrFootnote footnote)
		{
			FwObjDataTypes odt;
			Guid footnoteRef = TsStringUtils.GetGuidFromProps(ttp,
				new FwObjDataTypes[] { FwObjDataTypes.kodtNameGuidHot }, out odt);
			Debug.Assert(footnoteRef != Guid.Empty, "Unable to find referenced footnote.");
			return m_cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().TryGetFootnote(
				footnoteRef, out footnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write all the paragraphs in the given footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportFootnote(IScrFootnote scrFootnote)
		{
			m_footnoteIndex = scrFootnote.IndexInOwner + 1;
			// TE allows only one paragraph in a footnote!
			Debug.Assert(scrFootnote.ParagraphsOS.Count < 2);
			foreach (IStTxtPara para in scrFootnote.ParagraphsOS)
				ExportParagraph(para, null, false);
			m_footnoteIndex = -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write out the picture as a &lt;figure&gt; element.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportPicture(ICmPicture pict)
		{
			ITsString tssCaption = pict.Caption.BestVernacularAlternative;
			string sCaption;
			if (tssCaption.Length > 0 && !tssCaption.Equals(pict.Caption.NotFoundTss))
				sCaption = tssCaption.Text.Normalize();
			else
				sCaption = String.Empty;
			m_writer.WriteStartElement("figure");
			m_fInFigure = true;
			try
			{
				string sPath = pict.PictureFileRA.AbsoluteInternalPath.Normalize();
				string sFile = Path.GetFileName(sPath);
				WriteFigureAttributes(sCaption, sFile);
				m_writer.WriteComment(String.Format("path=\"{0}\"", sPath));
				m_writer.WriteStartElement("caption");
				List<BTSegment> rgbts = new List<BTSegment>();
				BTSegment bts = new BTSegment(tssCaption, 0, sCaption.Normalize(NormalizationForm.FormD).Length, pict);
				foreach (int ws in m_dictAnalLangs.Keys)
				{
					ITsString tss = pict.Caption.get_String(ws);
					if (tss.Length > 0 && tss != pict.Caption.NotFoundTss)
						bts.SetTransForWs(ws, tss.get_NormalizedForm(FwNormalizationMode.knmNFC));
				}
				rgbts.Add(bts);

				if (sCaption.Length > 0) // Don't export caption with "NotFoundTss" text
					ExportParagraphData(tssCaption, rgbts);
				m_writer.WriteEndElement();	//</caption>
				m_writer.WriteEndElement();	//</figure>
			}
			finally
			{
				m_fInFigure = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write the src, oxesRef, and alt attributes for a &lt;figure&gt; element.
		/// </summary>
		/// <param name="sCaption"></param>
		/// <param name="sFile"></param>
		/// ------------------------------------------------------------------------------------
		private void WriteFigureAttributes(string sCaption, string sFile)
		{
			m_writer.WriteAttributeString("src", String.Format("{0}", sFile));
			if (!String.IsNullOrEmpty(m_sCurrentBookName))
			{
				if (String.IsNullOrEmpty(m_sCurrentChapterNumber))
				{
					if (String.IsNullOrEmpty(m_sCurrentVerseNumber))
					{
						m_writer.WriteAttributeString("oxesRef", m_sCurrentBookId);
						m_writer.WriteAttributeString("alt", String.Format("{0} ({1})",
							sCaption, m_sCurrentBookName));
					}
					else
					{
						// TODO: what if non-ASCII chapter and verse numbers are being used?
						m_writer.WriteAttributeString("oxesRef", String.Format("{0}.1.{1}",
							m_sCurrentBookId, m_sCurrentVerseNumber));
						m_writer.WriteAttributeString("alt", String.Format("{0} ({1} {2})",
							sCaption, m_sCurrentBookName, m_sCurrentVerseNumber));
					}
				}
				else if (String.IsNullOrEmpty(m_sCurrentVerseNumber))
				{
					m_writer.WriteAttributeString("oxesRef", String.Format("{0}.{1}",
						m_sCurrentBookId, m_sCurrentChapterNumber));
					m_writer.WriteAttributeString("alt", String.Format("{0} ({1} {2})",
						sCaption, m_sCurrentBookName, m_sCurrentChapterNumber));
				}
				else
				{
					m_writer.WriteAttributeString("oxesRef", String.Format("{0}.{1}.{2}",
						m_sCurrentBookId, m_sCurrentChapterNumber, m_sCurrentVerseNumber));
					m_writer.WriteAttributeString("alt", String.Format("{0} ({1} {2}{3}{4})",
						sCaption, m_sCurrentBookName, m_sCurrentChapterNumber,
						m_cache.LangProject.TranslatedScriptureOA.ChapterVerseSepr, m_sCurrentVerseNumber));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill in the back translation dictionaries for the given back translation object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private Dictionary<int, string> FillBackTransForPara(BackTranslationInfo backtran,
			out Dictionary<int, ITsString> dictBackTrans,
			out Dictionary<int, int> mapWsBackTransRunIndex)
		{
			Dictionary<int, string> mapWsStatus = new Dictionary<int, string>();
			// stores current paragraph's back translations, mapping from ws to string
			dictBackTrans = new Dictionary<int, ITsString>();
			// run index for each back translation, mapping from ws to index
			mapWsBackTransRunIndex = new Dictionary<int, int>();
			if (backtran != null)
			{
				foreach (int ws in m_dictAnalLangs.Keys)
				{
					ITsString tss = backtran.GetBackTransForWs(ws);
					if (tss != null && tss.Length > 0)
					{
						dictBackTrans.Add(ws, tss);
						mapWsBackTransRunIndex.Add(ws, 0);
						string status = backtran.GetStatusForWs(ws);
						if (!String.IsNullOrEmpty(status))
							mapWsStatus.Add(ws, status.Normalize());
					}
				}
			}
			return mapWsStatus;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write the paragraph using a segmented back translation.
		/// </summary>
		/// <param name="tssPara">ITsString containing the paragraph's vernacular contents.</param>
		/// <param name="rgBtSeg">a list of information about the segments within the
		/// paragraph and their associated back translations, if any.</param>
		/// ------------------------------------------------------------------------------------
		private void ExportParagraphData(ITsString tssPara, List<BTSegment> rgBtSeg)
		{
			int cseg = rgBtSeg.Count;
			if (tssPara.Length > 0)
			{
				// The para isn't empty so export its segments.
				foreach (BTSegment seg in rgBtSeg)
				{
					WriteVernSegment(seg.Text, seg.IsLabel);
					CloseTranslationElementIfNeeded();
					WriteSegmentedBackTrans(seg);
				}
			}
			else
			{
				// The paragraph is empty. Export the back translation(s), if any.
				foreach (BTSegment seg in rgBtSeg)
					WriteSegmentedBackTrans(seg);
			}
		}

		/// <summary>
		/// Work around for mono bug https://bugzilla.novell.com/show_bug.cgi?id=517855
		/// </summary>
		private void WriteVernSegment(object tssSeg, bool isLabel)
		{
			if (!(tssSeg is ITsString))
				throw new ArgumentException();

			WriteVernSegment(tssSeg as ITsString, isLabel);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes the ITsString of the vernacular segment.
		/// </summary>
		/// <param name="tssSeg">ITsString containing the contents of the vernacular.</param>
		/// <param name="isLabel">if set to <c>true</c> this segment is a label; <c>false</c>
		/// otherwise.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private void WriteVernSegment(ITsString tssSeg, bool isLabel)
		{
			int crun = tssSeg.RunCount;
			List<int> rgws;
			List<OxesInfo> rgInfo = CollectRunInfo(tssSeg, out rgws);
			for (int iRun = 0; iRun < crun; ++iRun)
			{
				TsRunInfo tri;
				ITsTextProps ttp = tssSeg.FetchRunInfo(iRun, out tri);
				string data = tssSeg.get_RunText(iRun);

				string dataNFC = string.IsNullOrEmpty(data) ? string.Empty : data.Normalize();
				OxesInfo xinfo = rgInfo[iRun];
				switch (xinfo.StyleName)
				{
					case ScrStyleNames.ChapterNumber:
						CloseLabelTrIfNeeded();
						ProcessChapterNumber(dataNFC, null, null, null);
						break;
					case ScrStyleNames.VerseNumber:
					case ScrStyleNames.VerseNumberInNote:
					case ScrStyleNames.VerseNumberAlternate:
						CloseLabelTrIfNeeded();
						ProcessVerseNumber(dataNFC, null, null, null,
										   xinfo.StyleName == ScrStyleNames.VerseNumberAlternate);
						break;
					default:
						if (isLabel && dataNFC != StringUtils.kChHardLB.ToString()) // a hard line break should be its own segment
						{
							// dealing with whitespace after a label. Return for export with next run.
							OpenLabelTrIfNeeded();
							WriteRunDataWithEmbeddedStyleInfo(rgInfo, rgws, iRun, m_cache.DefaultVernWs, dataNFC);
						}
						else
						{
							// Exporting Scripture (non-label) text
							Debug.Assert(!m_fInLabelTr);
							OpenTrGroupIfNeeded(); // open the <trGroup> for the paragraph if it isn't already started
							OpenTranslationElementIfNeeded(); // open the <tr> for the paragraph if it isn't already started

							if (IsObjectReference(dataNFC))
								ExportEmbeddedObject(ttp);
							else
								WriteRunDataWithEmbeddedStyleInfo(rgInfo, rgws, iRun, m_cache.DefaultVernWs, dataNFC);
						}
						break;
				}
			}
			CloseLabelTrIfNeeded();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes the ITsString of the back translation segment.
		/// </summary>
		/// <param name="tssSeg">ITsString containing the contents of the back translation.</param>
		/// ------------------------------------------------------------------------------------
		private void WriteBtSegment(ITsString tssSeg)
		{
			int crun = tssSeg.RunCount;
			List<int> rgws;
			List<OxesInfo> rgInfo = CollectRunInfo(tssSeg, out rgws);
			for (int iRun = 0; iRun < crun; ++iRun)
			{
				TsRunInfo tri;
				ITsTextProps ttp = tssSeg.FetchRunInfo(iRun, out tri);
				string data = tssSeg.get_RunText(iRun);

				string dataNFC = String.Empty;
				if (!string.IsNullOrEmpty(data))
					dataNFC = data.Normalize();

				if (IsObjectReference(dataNFC))
					ExportRefToEmbeddedObject(ttp);
				else
					WriteRunDataWithEmbeddedStyleInfo(rgInfo, rgws, iRun, m_cache.DefaultVernWs, dataNFC, true);
			}
		}

		private void WriteSegmentedBackTrans(BTSegment bts)
		{
			CloseTranslationElementIfNeeded();
			foreach (int ws in bts.AvailableTranslations)
			{
				ITsString tssSeg = bts.GetTransForWs(ws);
				string data = tssSeg.Text;
				if (!string.IsNullOrEmpty(tssSeg.Text))
				{
					OpenTrGroupIfNeeded();
					MarkBackTranslation(ws, bts.GetBtStatusForWs(ws), true);
					m_writer.Formatting = Formatting.None;
					WriteSegmentAnnotations(bts, data, ws);
					WriteBtSegment(tssSeg);
					m_writer.WriteEndElement();
					if (!InFootnote) // Don't want to add whitespace if we're in a footnote or picture since they need to be inline.
						m_writer.Formatting = Formatting.Indented;
				}
			}
			CloseTrGroupIfNeeded();

			// Write any annotations for back translation.
			if (bts.TargetObject is IStTxtPara && ((IStTxtPara)bts.TargetObject).TranslationsOC.Count > 0)
			{
				BackTranslationInfo btInfo = GetBackTranslationForPara((IStTxtPara)bts.TargetObject);
				if (btInfo != null)
					ExportAnnotationsForObj(btInfo.Hvo);
			}
		}

		private void WriteSegmentAnnotations(BTSegment bts, string data, int ws)
		{
			if (m_rgSegAnn.Count == 0)
				return;
			List<int> rgws = bts.AvailableTranslations;
			List<IScrScriptureNote> rgWritten = new List<IScrScriptureNote>();
			for (int i = 0; i < m_rgSegAnn.Count; ++i)
			{
				IScrScriptureNote note = m_rgSegAnn[i];
				ICmIndirectAnnotation cia = note.BeginObjectRA as ICmIndirectAnnotation;
				Debug.Assert(cia != null);
				foreach (ICmAnnotation ann in cia.AppliesToRS)
				{
					ICmBaseAnnotation ba = ann as ICmBaseAnnotation;
					Debug.Assert(ba != null);
					if (ba != null)
					{
						//Debug.WriteLine(String.Format("WriteSegmentAnnotations(bts=({0},{1},{2}), data='{3}', ws={4})",
						//    bts.BeginOffset, bts.EndOffset, bts.TargetObject.Hvo, data, ws));
						//Debug.WriteLine(String.Format("    note.BeginOffset={0}, note.EndOffset={1}, note.BeginObjectRAHvo={2}, note.CitedText='{3}'",
						//    note.BeginOffset, note.EndOffset, note.BeginObjectRAHvo, note.CitedText));
						//Debug.WriteLine(String.Format("    ba.BeginOffset={0}, ba.EndOffset={1}, ba.BeginObjectRAHvo={2}",
						//    ba.BeginOffset, ba.EndOffset, ba.BeginObjectRAHvo));
						if ((ba.BeginOffset <= bts.BeginOffset && ba.EndOffset >= bts.EndOffset) ||
							(ba.BeginOffset >= bts.BeginOffset && ba.EndOffset <= bts.EndOffset) ||
							(ba.EndOffset >= bts.BeginOffset && ba.EndOffset <= bts.EndOffset))
						{
							Debug.Assert(ba.BeginObjectRA == bts.TargetObject);
							string sLanguageInFocus = null;
							int idxStart = -1;
							//int idxEnd = -1;
							if (!String.IsNullOrEmpty(data) && !String.IsNullOrEmpty(note.CitedText))
							{
								idxStart = data.IndexOf(note.CitedText);
								if (idxStart >= 0)
								{
									m_mapWsRFC.TryGetValue(ws, out sLanguageInFocus);
									//idxEnd = idxStart + note.CitedText.Length;
								}
							}
							if (idxStart < 0)
							{
								if (ws != rgws[rgws.Count - 1])
									continue;
							}
							//Debug.WriteLine(String.Format("    idxStart={0}, idxEnd={1}, sLang='{2}'",
							//    idxStart, idxEnd, sLanguageInFocus == null ? "<NULL>" : sLanguageInFocus));

							XmlScrNote.Serialize(m_writer, note, sLanguageInFocus);
							rgWritten.Add(note);
						}
					}
				}
			}
			foreach (IScrScriptureNote note in rgWritten)
				m_rgSegAnn.Remove(note);
		}

//#if DEBUG
//        private void WriteSegmentedBackTransDebugInfo(BTSegment bts, int iseg)
//        {
//            foreach (int ws1 in bts.AvailableTranslations)
//            {
//                ITsString tssBT = bts.GetTransForWs(ws1);
//                string sBTDebug = String.Format("Seg[{0}] begin={1,4}, end={2,4}; [{3}]='{4}'",
//                    iseg, bts.BeginOffset, bts.EndOffset,
//                    m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws1),
//                    tssBT == null ? "<NULL>" : tssBT.Text);
//                Debug.WriteLine(sBTDebug);
//            }
//        }
//#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the text props contains a footnote pointer, return the Guid which refers to the
		/// footnote.  Otherwise, return Guid.Empty.
		/// </summary>
		/// <param name="props"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static Guid GetFootnoteGuidIfAny(ITsTextProps props)
		{
			string objData = props.GetStrPropValue((int)FwTextPropType.ktptObjData);
			if (!String.IsNullOrEmpty(objData) &&
				(objData[0] == (char)FwObjDataTypes.kodtOwnNameGuidHot ||
				objData[0] == (char)FwObjDataTypes.kodtNameGuidHot))
			{
				return MiscUtils.GetGuidFromObjData(objData.Substring(1));
			}
			return Guid.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write the current run to the XML file.  If the next style element can be embedded
		/// inside the current one, then the current element is not closed, and its OxesInfo
		/// data is pushed onto a stack.  Otherwise, the current element is closed.
		/// </summary>
		/// <param name="rgInfo">information about each run</param>
		/// <param name="rgws">the writing systems that contain </param>
		/// <param name="run">the index of the run within the ITsString to write</param>
		/// <param name="wsDefault">the default writing system which is used to determine
		/// whether a writing system needs to be marked.</param>
		/// <param name="data"></param>
		/// ------------------------------------------------------------------------------------
		private void WriteRunDataWithEmbeddedStyleInfo(List<OxesInfo> rgInfo, List<int> rgws,
			int run, int wsDefault, string data)
		{
			WriteRunDataWithEmbeddedStyleInfo(rgInfo, rgws, run, wsDefault, data, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write the current run to the XML file.  If the next style element can be embedded
		/// inside the current one, then the current element is not closed, and its OxesInfo
		/// data is pushed onto a stack.  Otherwise, the current element is closed.
		/// </summary>
		/// <param name="rgInfo">information about each run.</param>
		/// <param name="rgws">the writing systems used in each run.</param>
		/// <param name="run">the index of the run within the ITsString to write</param>
		/// <param name="wsDefault">the default writing system which is used to determine
		/// whether a writing system needs to be marked.</param>
		/// <param name="data">The text to export.</param>
		/// <param name="fIsBackTrans">if set to <c>true</c> the data is from a back translation;
		/// <c>false</c> if the data is from the back translation.</param>
		/// ------------------------------------------------------------------------------------
		private void WriteRunDataWithEmbeddedStyleInfo(List<OxesInfo> rgInfo, List<int> rgws,
			int run, int wsDefault, string data, bool fIsBackTrans)
		{
			OxesInfo xinfo = rgInfo[run];
			int ws = rgws[run];

			bool fCurrentEqualsTopInfo;
			if (m_stackTextRunInfo.Count > 0)
			{
				OxesInfo xinfoTop = m_stackTextRunInfo.Peek();
				fCurrentEqualsTopInfo = xinfo.Equals(xinfoTop);
			}
			else
			{
				fCurrentEqualsTopInfo = false;
			}
			if (!string.IsNullOrEmpty(xinfo.XmlTag) && !fCurrentEqualsTopInfo)
			{
				m_writer.WriteStartElement(xinfo.XmlTag);
				if (!string.IsNullOrEmpty(xinfo.AttrName) && !string.IsNullOrEmpty(xinfo.AttrValue))
					m_writer.WriteAttributeString(xinfo.AttrName, xinfo.AttrValue);
			}

			bool fForeign = false;
			if (!fIsBackTrans)
				fForeign = MarkForeignIfNeeded(wsDefault, ws);

			if (!String.IsNullOrEmpty(data))
				m_writer.WriteString(data);
			if (fForeign)
				m_writer.WriteEndElement();	//</foreign>

			if (!string.IsNullOrEmpty(xinfo.XmlTag))
			{
				// check whether the next element can be embedded inside this one: if so,
				// we don't need an end tag here, but do need to ensure that the current
				// tag is pushed onto the stack if it's not already there.
				bool fWriteEndAndPopAsNeeded;
				if (run + 2 < rgInfo.Count)
				{
					OxesInfo yinfo = rgInfo[run + 1];
					OxesInfo zinfo = rgInfo[run + 2];
					fWriteEndAndPopAsNeeded = !(yinfo.CanEmbed && zinfo.Equals(xinfo));
				}
				else
				{
					fWriteEndAndPopAsNeeded = true;
				}
				if (fWriteEndAndPopAsNeeded)
				{
					m_writer.WriteEndElement();
					if (fCurrentEqualsTopInfo)
						m_stackTextRunInfo.Pop();
				}
				else if (!fCurrentEqualsTopInfo)
				{
					m_stackTextRunInfo.Push(xinfo);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Marks the back translation.
		/// </summary>
		/// <param name="ws">The writing system for the back translation.</param>
		/// <param name="status">The status of the back translation.</param>
		/// <param name="fMarkAsSegmented">if set to <c>true</c> include the segmented attribute
		/// in the bt element; if <c>false</c> omit the attribute.</param>
		/// ------------------------------------------------------------------------------------
		private void MarkBackTranslation(int ws, string status, bool fMarkAsSegmented)
		{
			m_writer.WriteStartElement("bt");
			m_writer.WriteAttributeString("xml", "lang", null, GetRFCFromWs(ws));
			if (!InFootnote && fMarkAsSegmented)
				m_writer.WriteAttributeString("segmented", "true");
			if (!string.IsNullOrEmpty(status))
				m_writer.WriteAttributeString("status", status.ToLower());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Flag this run as being in a foreign language if needed, and return true if so
		/// flagged.
		/// </summary>
		/// <param name="wsDefault"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool MarkForeignIfNeeded(int wsDefault, int ws)
		{
			if (ws > 0 && ws != wsDefault)
			{
				m_writer.WriteStartElement("foreign");
				m_writer.WriteAttributeString("xml", "lang", null, GetRFCFromWs(ws));
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the RFC4646 code for the given writing system.
		/// </summary>
		/// <param name="ws"></param>
		/// ------------------------------------------------------------------------------------
		private string GetRFCFromWs(int ws)
		{
			string sRFC;
			if (!m_mapWsRFC.TryGetValue(ws, out sRFC))
			{
				IWritingSystem wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
				sRFC = wsObj.Id.Normalize();
				m_mapWsRFC.Add(ws, sRFC);
			}
			return sRFC;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open a &lt;tr&gt; element if applicable and one is not already open. If so, turn off
		/// output formatting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OpenTranslationElementIfNeeded()
		{
			bool fOpenElement = false;
			if (!m_fTranslationElementIsOpen && !InFootnote && !m_fInFigure)
			{
				// <tr> element not yet opened in an element outside a footnote or figure
				fOpenElement = true;
				m_fTranslationElementIsOpen = true;
			}
			else if (!m_fTranslationElementIsOpenInFootnote && InFootnote && !m_fInFigure)
			{
				// <tr> element not yet opened in a footnote element
				fOpenElement = true;
				m_fTranslationElementIsOpenInFootnote = true;
			}
			else if (!m_fTranslationElementIsOpenInFigure && !InFootnote && m_fInFigure)
			{
				// <tr> element not yet opened in a figure element
				fOpenElement = true;
				m_fTranslationElementIsOpenInFigure = true;
			}

			if (fOpenElement)
			{
				m_writer.WriteStartElement("tr");
				m_writer.Formatting = Formatting.None;
				m_stackTextRunInfo.Clear();		// probably not needed, but safe.
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close the &lt;tr&gt; element if one is open, and turn on output formatting.  Also
		/// write any back translation data that is appropriate at this point.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CloseTranslationElementIfNeededAndWriteBackTrans(Dictionary<int, string> mapWsStatus,
			Dictionary<int, ITsString> dictBackTrans, Dictionary<int, int> mapWsBackTransRunIndex)
		{
			CloseTranslationElementIfNeeded();
			WriteBackTrans(mapWsStatus, dictBackTrans, mapWsBackTransRunIndex);
			CloseTrGroupIfNeeded();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close the tr element if one is open, and turn on output formatting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CloseTranslationElementIfNeeded()
		{
			bool fCloseElement = false;

			if (m_fTranslationElementIsOpen && !InFootnote && !m_fInFigure)
			{
				// <tr> element needs to be closed in an element outside a footnote or figure
				fCloseElement = true;
				m_fTranslationElementIsOpen = false;
			}
			else if (m_fTranslationElementIsOpenInFootnote && InFootnote && !m_fInFigure)
			{
				// <tr> element needs to be closed in a footnote element
				fCloseElement = true;
				m_fTranslationElementIsOpenInFootnote = false;
			}
			else if (m_fTranslationElementIsOpenInFigure && !InFootnote && m_fInFigure)
			{
				// <tr> element needs to be closed in a figure element
				fCloseElement = true;
				m_fTranslationElementIsOpenInFigure = false;
			}

			if (fCloseElement)
			{
				while (m_stackTextRunInfo.Count > 0)
				{
					m_writer.WriteEndElement();
					m_stackTextRunInfo.Pop();
				}
				m_writer.WriteEndElement();		// </tr>
				if (!InFootnote && !m_fInFigure) // Don't add whitespace if in a footnote or figure since they are inline.
					m_writer.Formatting = Formatting.Indented;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open a &lt;trGroup&gt; element if one is not open. Footnotes and figures also
		/// contain trGroup elements which are embedded in the trGroup element of the paragraph
		/// that contains them.
		/// </summary>
		/// <returns>true if &lt;trGroup&gt; element was written, otherwise false</returns>
		/// ------------------------------------------------------------------------------------
		private bool OpenTrGroupIfNeeded()
		{
			if (!m_fInTrGroup && !InFootnote && !m_fInFigure)
			{
				// Just beginning paragraph that is not within a <note> element
				m_writer.WriteStartElement("trGroup");
				m_fInTrGroup = true;
				return true;
			}

			if (!m_fInFootnoteTrGroup && InFootnote && !m_fInFigure)
			{
				// Just beginning paragraph within a <note> element
				m_writer.WriteStartElement("trGroup");
				m_fInFootnoteTrGroup = true;
				return true;
			}

			if (!m_fInFigureTrGroup && !InFootnote && m_fInFigure)
			{
				// Just beginning paragraph within a <figure> element
				m_writer.WriteStartElement("trGroup");
				m_fInFigureTrGroup = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close a &lt;trGroup&gt; element if one is open and should be closed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CloseTrGroupIfNeeded()
		{
			if (m_fInTrGroup && !InFootnote && !m_fInFigure)
			{
				m_writer.WriteEndElement();	//</trGroup>
				m_fInTrGroup = false;
			}
			else if (m_fInFootnoteTrGroup && InFootnote && !m_fInFigure)
			{
				m_writer.WriteEndElement();	//</trGroup>
				m_fInFootnoteTrGroup = false;
			}
			else if (m_fInFigureTrGroup && !InFootnote && m_fInFigure)
			{
				m_writer.WriteEndElement();	//</trGroup>
				m_fInFigureTrGroup = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open a &lt;labelTr&gt; element if one is not open.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OpenLabelTrIfNeeded()
		{
			if (!m_fInLabelTr)
			{
				m_writer.WriteStartElement("labelTr");
				m_fInLabelTr = true;
				m_writer.Formatting = Formatting.None;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the &lt;labelTr&gt; element if one is open.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CloseLabelTrIfNeeded()
		{
			if (m_fInLabelTr)
			{
				m_writer.WriteEndElement();	//</labelTr>
				m_fInLabelTr = false;
				Debug.Assert(!InFootnote);
				m_writer.Formatting = Formatting.Indented;
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write any back translation data that is appropriate at this point.  This checks for
		/// chapter numbers and verse numbers to keep in sync with the main text.  It also checks
		/// for a matching footnote reference if appropriate.
		/// </summary>
		/// <param name="mapWsStatus">status of the back translation of the current paragraph</param>
		/// <param name="dictBackTrans"></param>
		/// <param name="mapWsBackTransRunIndex"></param>
		/// ------------------------------------------------------------------------------------
		private void WriteBackTrans(Dictionary<int, string> mapWsStatus,
			Dictionary<int, ITsString> dictBackTrans, Dictionary<int,int> mapWsBackTransRunIndex)
		{
			if (dictBackTrans == null)
				return;
			bool fCloseTrGroup = false;
			Set<int> setwsFootnote = new Set<int>();
			foreach (int ws in dictBackTrans.Keys)
			{
				ITsString tss = dictBackTrans[ws];
				int irun = mapWsBackTransRunIndex[ws];
				bool fBackTransOpen = false;
				for (; irun < tss.RunCount; ++irun)
				{
					ITsTextProps ttp = tss.get_Properties(irun);
					string data = tss.get_RunText(irun);
					if (!String.IsNullOrEmpty(data))
						data = data.Normalize();
					if (IsObjectReference(data))
					{
						// Check for a footnote.
						Guid guidBT = GetFootnoteGuidIfAny(ttp);
						if (guidBT != Guid.Empty)
							ExportRefToEmbeddedObject(ttp);
						// Go on to the next run (don't export ORC character and ignore unexpected objects).
						continue;
					}
					string styleName = ttp.GetStrPropValue((int) FwTextPropType.ktptNamedStyle);
					string sRemainder;
					if (styleName == ScrStyleNames.ChapterNumber)
					{
						int iChapter = StringUtils.strtol(data, out sRemainder);
						if (fBackTransOpen || iChapter > m_iCurrentChapter)
							break;
						continue;
					}
					if (styleName == ScrStyleNames.VerseNumber ||
						styleName == ScrStyleNames.VerseNumberAlternate)
					{
						int iVerse = StringUtils.strtol(data, out sRemainder);
						if (fBackTransOpen || iVerse > m_iCurrentVerse)
							break;
						continue;
					}
					if (styleName == ScrStyleNames.VerseNumberInNote)
					{
						if (fBackTransOpen)
							break;
						continue;
					}
					if (!String.IsNullOrEmpty(data))
					{
						if (OpenTrGroupIfNeeded())
							fCloseTrGroup = true;
						if (!fBackTransOpen)
						{
							string status;
							mapWsStatus.TryGetValue(ws, out status);
							MarkBackTranslation(ws, status, false);
							fBackTransOpen = true;
							m_writer.Formatting = Formatting.None;
						}
						OxesInfo xinfo = OxesInfo.GetOxesInfoForCharStyle(styleName);
						if (!String.IsNullOrEmpty(xinfo.XmlTag))
						{
							m_writer.WriteStartElement(xinfo.XmlTag);
							if (!String.IsNullOrEmpty(xinfo.AttrName) && !String.IsNullOrEmpty(xinfo.AttrValue))
								m_writer.WriteAttributeString(xinfo.AttrName, xinfo.AttrValue);
						}
						m_writer.WriteString(data);
						if (!String.IsNullOrEmpty(xinfo.XmlTag))
							m_writer.WriteEndElement();
					}
				}
				if (fBackTransOpen)
				{
					m_writer.WriteEndElement();
					if (!InFootnote) // Don't want to add whitespace if we're in a footnote since footnotes need to be inline.
						m_writer.Formatting = Formatting.Indented;
				}
				mapWsBackTransRunIndex[ws] = irun;
			}
			if (fCloseTrGroup)
				CloseTrGroupIfNeeded();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check whether the current data is merely an object reference marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsObjectReference(string data)
		{
			return data != null && data.Length == 1 && data[0] == StringUtils.kChObject;
		}

		/// <summary>
		/// Class to store known variant passages that may cause repeated verse numbers.
		/// </summary>
		public class VariantPassage
		{
			#region data members
			private string m_sBookID;
			private int m_iChapter;
			private int m_iFirstVerse;
			private int m_iLastVerse;
			#endregion
			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="sBookId"></param>
			/// <param name="iChapter"></param>
			/// <param name="iFirstVerse"></param>
			/// <param name="iLastVerse"></param>
			public VariantPassage(string sBookId, int iChapter, int iFirstVerse, int iLastVerse)
			{
				m_sBookID = sBookId;
				m_iChapter = iChapter;
				m_iFirstVerse = iFirstVerse;
				m_iLastVerse = iLastVerse;
			}
			/// <summary>
			/// returns book ID string of the variant passage
			/// </summary>
			public string BookId
			{
				get { return m_sBookID; }
			}
			/// <summary>
			/// returns chapter number of the variant passage
			/// </summary>
			public int Chapter
			{
				get { return m_iChapter; }
			}
			/// <summary>
			/// returns first verse number of the variant passage
			/// </summary>
			public int FirstVerse
			{
				get { return m_iFirstVerse; }
			}
			/// <summary>
			/// returns last verse number of the variant passage
			/// </summary>
			public int LastVerse
			{
				get { return m_iLastVerse; }
			}
		}

		/// <summary>
		/// List of known variant passages that can cause repeated verse numbers.
		/// </summary>
		/// <remarks>
		/// REVIEW: is there a better way to detect variant verse numbers?
		/// TODO: load this from an external file?
		/// </remarks>
		static private VariantPassage[] g_rgVarPassages = new VariantPassage[] {
			new VariantPassage("MRK", 16, 9, 20),
		};
		/// <summary>
		/// flag whether we've detected that we're exporting a variant passage which
		/// needs special marking on the verse numbers.
		/// </summary>
		private bool m_fInVariantRange = false;
		/// <summary>
		/// number of variants of this range of verses that we've already exported
		/// </summary>
		private int m_cVariantRanges = 0;
		/// <summary>
		/// Added to verse reference (non-empty if a variant passage).
		/// </summary>
		private string m_sVariantVerseRef = "";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process the given verse number, writing an end milepost if needed, and writing the
		/// appropriate start milepost.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="mapWsStatus"></param>
		/// <param name="dictBackTrans"></param>
		/// <param name="mapWsBackTransRunIndex"></param>
		/// <param name="fAlternate"></param>
		/// ------------------------------------------------------------------------------------
		private void ProcessVerseNumber(string data, Dictionary<int, string> mapWsStatus,
			Dictionary<int, ITsString> dictBackTrans, Dictionary<int,int> mapWsBackTransRunIndex,
			bool fAlternate)
		{
			CloseTranslationElementIfNeededAndWriteBackTrans(mapWsStatus,
				dictBackTrans, mapWsBackTransRunIndex);
			// If the user kept going in verse number style after the verse number, trim the
			// "verse number" at the first space.
			string sVerseNumber = data.Trim().Split(new char[] {' ', '\t', '\n', '\r'})[0];
			if (String.IsNullOrEmpty(sVerseNumber))
				return;
			if (!InFootnote)
			{
				WriteVerseEndMilepostIfNeeded();
				m_sCurrentVerseNumber = sVerseNumber;
				int iVerse;
				if (!Int32.TryParse(data, out iVerse))
				{
					string sRemainder;
					iVerse = StringUtils.strtol(data, out sRemainder);
					if (sRemainder == data)
						iVerse = m_iCurrentVerse + 1;
				}
				if (iVerse <= m_iCurrentVerse)
				{
					++m_cVariantRanges;
					m_sVariantVerseRef = "";
				}
				bool fInVariantRange = false;
				if (iVerse <= m_iCurrentVerse || m_fInVariantRange)
				{
					for (int i = 0; i < g_rgVarPassages.Length; ++i)
					{
						if (m_sCurrentBookId == g_rgVarPassages[i].BookId &&
							m_iCurrentChapter == g_rgVarPassages[i].Chapter &&
							iVerse >= g_rgVarPassages[i].FirstVerse &&
							iVerse <= g_rgVarPassages[i].LastVerse &&
							m_iCurrentVerse >= g_rgVarPassages[i].FirstVerse &&
							m_iCurrentVerse <= g_rgVarPassages[i].LastVerse)
						{
							fInVariantRange = true;
						}
					}
				}
				m_fInVariantRange = fInVariantRange;
				if (m_fInVariantRange)
				{
					if (String.IsNullOrEmpty(m_sVariantVerseRef))
						m_sVariantVerseRef = String.Format(".variant.{0}", m_cVariantRanges);
				}
				else
				{
					m_cVariantRanges = 0;
					m_sVariantVerseRef = "";
				}
				m_iCurrentVerse = iVerse;
			}
			m_writer.WriteStartElement("verseStart");
			string sVerseRef;
			if (String.IsNullOrEmpty(m_sCurrentChapterNumber))
			{
				// TODO: what if non-ASCII chapter/verse numbers are being used?
				if (InFootnote)
					sVerseRef = String.Format("{0}.1.{1}.note", m_sCurrentBookId, sVerseNumber);
				else
					sVerseRef = String.Format("{0}.1.{1}{2}", m_sCurrentBookId, sVerseNumber, m_sVariantVerseRef);
			}
			else
			{
				if (InFootnote)
					sVerseRef = String.Format("{0}.{1}.{2}.note", m_sCurrentBookId, m_sCurrentChapterNumber, sVerseNumber);
				else
					sVerseRef = String.Format("{0}.{1}.{2}{3}", m_sCurrentBookId, m_sCurrentChapterNumber, sVerseNumber, m_sVariantVerseRef);
			}
			if (InFootnote)
			{
				if (sVerseRef == m_sFootnoteVerseRef)
				{
					char ch = (char)((int)'a' + m_cFootnoteVerseRefRepeated);
					sVerseRef = String.Format("{0}.{1}", sVerseRef, ch);
					++m_cFootnoteVerseRefRepeated;
				}
				else
				{
					m_sFootnoteVerseRef = sVerseRef;
					m_cFootnoteVerseRefRepeated = 0;
				}
			}
			m_writer.WriteAttributeString("ID", sVerseRef);
			if (fAlternate)
				m_writer.WriteAttributeString("aID", sVerseRef);
			m_writer.WriteAttributeString("n", data);
			m_writer.WriteEndElement();
			if (!InFootnote)
				ExportAnyRelatedAnnotation();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process the given chapter number, writing an end milepost if needed, and writing the
		/// appropriate start milepost.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="mapWsStatus"></param>
		/// <param name="dictBackTrans"></param>
		/// <param name="mapWsBackTransRunIndex"></param>
		/// ------------------------------------------------------------------------------------
		private void ProcessChapterNumber(string data, Dictionary<int, string> mapWsStatus,
			Dictionary<int, ITsString> dictBackTrans, Dictionary<int, int> mapWsBackTransRunIndex)
		{
			CloseTranslationElementIfNeededAndWriteBackTrans(mapWsStatus,
				dictBackTrans, mapWsBackTransRunIndex);
			WriteChapterEndMilepostIfNeeded();
			m_sCurrentChapterNumber = data;
			int iChapter;
			if (Int32.TryParse(data, out iChapter))
				m_iCurrentChapter = iChapter;
			else
				++m_iCurrentChapter;
			m_iCurrentVerse = 0;
			m_sCurrentVerseNumber = String.Empty;
			m_writer.WriteStartElement("chapterStart");
			m_writer.WriteAttributeString("ID",
				String.Format("{0}.{1}", m_sCurrentBookId, m_sCurrentChapterNumber));
			m_writer.WriteAttributeString("n", data);
			m_writer.WriteEndElement();
			ExportAnyRelatedAnnotation();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If any annotions refer to the current verse, export them.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportAnyRelatedAnnotation()
		{
			IScrBookAnnotations sba = m_cache.LangProject.TranslatedScriptureOA.BookAnnotationsOS[m_iCurrentBook - 1];
			if (sba == null || sba.NotesOS.Count == 0)
				return;

			int refCurrent = m_iCurrentBook * 1000000 + m_iCurrentChapter * 1000 + m_iCurrentVerse;
			Debug.Assert((m_iCurrentVerse == 0) || (refCurrent >= m_sectionRefStart && refCurrent <= m_sectionRefEnd),
				String.Format("There is probably a bad chapter or verse number for book {0} chapter {1}, verse {2}",
					m_sCurrentBookId, m_iCurrentChapter, m_iCurrentVerse));

			for (int i = 0; i < sba.NotesOS.Count; ++i)
			{
				if (sba.NotesOS[i].BeginRef == refCurrent)
					ExportScriptureNote(sba.NotesOS[i]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If any annotions refer to the current object, export them.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportAnnotationsForObj(int hvoObj)
		{
			IScrBookAnnotations sba =
				m_cache.LangProject.TranslatedScriptureOA.BookAnnotationsOS[m_iCurrentBook - 1];

			if (sba == null || sba.NotesOS.Count == 0)
				return;

			for (int i = 0; i < sba.NotesOS.Count; ++i)
			{
				if (sba.NotesOS[i].BeginObjectRA != null && sba.NotesOS[i].BeginObjectRA.Hvo == hvoObj)
					ExportScriptureNote(sba.NotesOS[i]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export a single annotation, either a translator note or a consultant note.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportScriptureNote(IScrScriptureNote ann)
		{
			// if this note is on a translation...
			string sLanguageInFocus = null;
			if (ann.BeginObjectRA != null)
			{
				if (ann.BeginObjectRA.OwningFlid == StTxtParaTags.kflidTranslations)
				{
					// then we need to include the "languageInFocus" attribute on the para as well.
					sLanguageInFocus = ann.WritingSystem;
				}
			}

			if (!m_exportedAnnotations.ContainsKey(ann.Hvo))
			{
				if (XmlScrNote.Serialize(m_writer, ann, sLanguageInFocus))
					m_exportedAnnotations[ann.Hvo] = true;
			}
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Return the type for an annotation that represents an ignored Scripture check error.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//private string GetTypeOfIgnoredError(Guid guid)
		//{
		//    if (guid == StandardCheckIds.kguidChapterVerse)
		//        return "chapterVerseCheck";
		//    if (guid == StandardCheckIds.kguidCharacters)
		//        return "characterCheck";
		//    if (guid == StandardCheckIds.kguidMatchedPairs)
		//        return "matchedPairsCheck";
		//    if (guid == StandardCheckIds.kguidMixedCapitalization)
		//        return "mixedCapitalizationCheck";
		//    if (guid == StandardCheckIds.kguidPunctuation)
		//        return "punctuationCheck";
		//    if (guid == StandardCheckIds.kguidRepeatedWords)
		//        return "repeatedWordsCheck";
		//    if (guid == StandardCheckIds.kguidCapitalization)
		//        return "capitalizationCheck";

		//    return null;
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a Scripture reference to a string builder.  There may be one or two of these
		/// added to the string builder, with a hyphen inserted between them (if there's two).
		/// </summary>
		/// <param name="bldrRef"></param>
		/// <param name="reference"></param>
		/// ------------------------------------------------------------------------------------
		protected static void AddReferenceToBldr(StringBuilder bldrRef, int reference)
		{
			if (bldrRef.Length > 0)
				bldrRef.Append('-');
			// Converting from book index to book code is a pain.  We have a hammer, so use it!
			BCVRef bcvRef = new BCVRef(reference);
			string[] rgs = bcvRef.AsString.Split(new char[] { ' ' });
			bldrRef.Append(rgs[0]);
			if (bcvRef.Chapter > 0)
			{
				bldrRef.AppendFormat(".{0}", bcvRef.Chapter);
				if (bcvRef.Verse > 0)
					bldrRef.AppendFormat(".{0}", bcvRef.Verse);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export the paragraphs for the given section of an annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ExportNotationParagraphs(IStJournalText text, string sTag)
		{
			// don't write out anything for an empty text.
			if (text.ParagraphsOS.Count == 0)
				return;
			if (text.ParagraphsOS.Count == 1 &&
				(text.ParagraphsOS[0] is IStTxtPara) &&
				(text.ParagraphsOS[0] as IStTxtPara).Contents.Length == 0)
			{
				return;
			}
			m_writer.WriteStartElement(sTag);
			// TODO: write out these as attributes (?) when supported by the UI.
			//text.CreatedByRA;
			//text.DateCreated;
			//text.ModifiedByRA;
			//text.DateModified;
			foreach (IStTxtPara para in text.ParagraphsOS)
			{
				m_writer.WriteStartElement("para");
				if (para.Contents != null)
				{
					ITsTextProps ttp = para.Contents.get_Properties(0);
					int nVar;
					int wsDefault = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
					if (wsDefault > 0)
						m_writer.WriteAttributeString("xml", "lang", null, GetRFCFromWs(wsDefault));
					for (int iRun = 0; iRun < para.Contents.RunCount; iRun++)
					{

						ttp = para.Contents.get_Properties(iRun);
						if (TsStringUtils.IsHyperlink(ttp))
							m_writer.WriteStartElement("a");
						else
							m_writer.WriteStartElement("span");

						if (iRun > 0)
						{
							int wsRun = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
							if (wsRun > 0 && wsRun != wsDefault)
								m_writer.WriteAttributeString("xml", "lang", null, GetRFCFromWs(wsRun));
						}
						int cprop = ttp.StrPropCount;
						int tpt;
						for (int iprop = 0; iprop < cprop; ++iprop)
						{
							string sProp = ttp.GetStrProp(iprop, out tpt);
							if (tpt == (int)FwTextPropType.ktptNamedStyle)
							{
								// Hyperlink style is internal, so it is applied automatically to
								// embedded links (i.e., it can be inferred from the href attribute).
								if (sProp != StyleServices.Hyperlink)
									m_writer.WriteAttributeString("type", sProp);
							}
							else if (!TsStringUtils.WriteHref(tpt, sProp, m_writer))
							{
								throw new Exception("Unexpected string property in annotation field: " +
									sTag + ". FwTextPropType = " + tpt + "; Property = " + sProp ?? "null");
							}
						}
						string runText = para.Contents.get_RunText(iRun);
						if (!string.IsNullOrEmpty(runText))
							m_writer.WriteString(runText.Normalize());
						m_writer.WriteEndElement();
					}
				}
				m_writer.WriteEndElement();
			}
			m_writer.WriteEndElement();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write the chapter (and possibly) verse end mileposts if the starting milepost has
		/// been written.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void WriteChapterEndMilepostIfNeeded()
		{
			if (!String.IsNullOrEmpty(m_sCurrentChapterNumber))
			{
				WriteVerseEndMilepostIfNeeded();
				m_writer.WriteStartElement("chapterEnd");
				m_writer.WriteAttributeString("ID", String.Format("{0}.{1}", m_sCurrentBookId, m_sCurrentChapterNumber));
				m_writer.WriteEndElement();
				m_sCurrentChapterNumber = String.Empty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write the verse end milepost if the starting milepost has been written.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void WriteVerseEndMilepostIfNeeded()
		{
			if (!String.IsNullOrEmpty(m_sCurrentVerseNumber))
			{
				m_writer.WriteStartElement("verseEnd");
				// TODO: what if non-ASCII chapter/verse numbers are being used?
				if (String.IsNullOrEmpty(m_sCurrentChapterNumber))
					m_writer.WriteAttributeString("ID", String.Format("{0}.1.{1}{2}",
						m_sCurrentBookId, m_sCurrentVerseNumber, m_sVariantVerseRef));
				else
					m_writer.WriteAttributeString("ID", String.Format("{0}.{1}.{2}{3}",
						m_sCurrentBookId, m_sCurrentChapterNumber, m_sCurrentVerseNumber, m_sVariantVerseRef));
				m_writer.WriteEndElement();
				m_sCurrentVerseNumber = String.Empty;
			}
		}
		#endregion
	}
}
