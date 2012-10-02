// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExportXml.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.IText;
using SILUBS.SharedScrUtils;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides export of data to a XML format file (Open XML for Editing Scripture)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ExportXml
	{
		/// <summary>the object that handles writing XML elements and attributes to a file</summary>
		private XmlTextWriter m_writer;
		/// <summary>where we get the data from</summary>
		private FdoCache m_cache;
		/// <summary>The HVO of the English writing system</summary>
		private int m_wsEnglish;
		/// <summary>stores the list of books to export</summary>
		private FilteredScrBooks m_bookFilter;
		/// <summary>the basic Scripture object</summary>
		private IScripture m_scr;
		/// <summary>the pathname of the output XML file</summary>
		private string m_fileName;
		/// <summary>cancel flag set asynchronously</summary>
		private bool m_cancel;
		/// <summary>maps from ws (LgWritingSystem hvo) to RFC4646 language code for analysis writing systems</summary>
		private Dictionary<int, string> m_dictAnalLangs = new Dictionary<int, string>();
		/// <summary>maps from ws (LgWritingSystem hvo) to RFC4646 language code for all writing systems</summary>
		private Dictionary<int, string> m_mapWsRFC = new Dictionary<int, string>();
		/// <summary>flag whether a translation (&lt;tr&gt;) element is open</summary>
		private bool m_fTranslationElementIsOpen = false;
		/// <summary>the name of the current book</summary>
		private string m_sCurrentBookName;
		/// <summary>the 3-letter id of the current book</summary>
		private string m_sCurrentBookId;
		/// <summary>the current chapter number, as a string</summary>
		private string m_sCurrentChapterNumber;
		/// <summary>the current verse number, as a string (may be something like "1-2")</summary>
		private string m_sCurrentVerseNumber;
		/// <summary>the starting reference of the current section (BBCCCVVV)</summary>
		int m_sectionRefStart = 0;
		/// <summary>the ending reference of the current section (BBCCCVVV)</summary>
		int m_sectionRefEnd = 0;
		/// <summary>1-based index of the current book</summary>
		private int m_iCurrentBook = 0;
		/// <summary>current chapter number</summary>
		private int m_iCurrentChapter = 0;
		/// <summary>current verse number (first verse given in m_sCurrentVerseNumber if that is a range)</summary>
		private int m_iCurrentVerse = 0;
		/// <summary>Values used to keep track of nested sections.</summary>
		enum SectionLevel
		{
			Major = 0,
			Normal = 1,
			Minor = 2,
			Series = 3
		}
		/// <summary>stack used to keep track of section nesting</summary>
		private Stack<SectionLevel> m_stackSectionLevels = new Stack<SectionLevel>();
		//
		/// <summary>Values used to keep track of nested table elements.</summary>
		enum TableElement
		{
			Table = 0,
			Row = 1,
			Cell = 2
		}
		/// <summary>stack used to keep track of table element nesting</summary>
		private Stack<TableElement> m_stackTableElements = new Stack<TableElement>();
		/// <summary>flag whether an &lt;item level="1"&gt; element is open in the normal context</summary>
		bool m_fListItem1Open = false;
		/// <summary>flag whether an &lt;item level="2"&gt; element is open in the normal context</summary>
		bool m_fListItem2Open = false;
		/// <summary>flag whether an &lt;embedded&gt; element is open</summary>
		bool m_fEmbedded = false;
		/// <summary>flag whether a &lt;speech&gt; element is open</summary>
		bool m_fInSpeech = false;
		/// <summary>flag whether a &lt;note type="general" published="true" ...&gt; element is open</summary>
		bool m_fInFootnote = false;
		/// <summary>store verse reference found in footnote to prevent duplicates</summary>
		string m_sFootnoteVerseRef = null;
		/// <summary>number of times a verse reference has been repeated in a footnote</summary>
		int m_cFootnoteVerseRefRepeated = 0;
		/// <summary>flag whether paragraphs from the ScrSection.Heading field are being exported</summary>
		bool m_fInSectionHeader = false;
		/// <summary>flag whether a &lt;trGroup&gt; element is open</summary>
		bool m_fInTrGroup = false;
		/// <summary>stack of OXES elements opened inside a &lt;tr&gt; element, used to handle nesting "styles"</summary>
		Stack<OxesInfo> m_stackTextRunInfo = new Stack<OxesInfo>();
		/// <summary>what to export: everything, filtered list of books, or a single book</summary>
		ExportWhat m_what;
		/// <summary>if single book, number of the book to export; otherwise meaningless</summary>
		int m_nBookSingle;
		/// <summary>if single book, index of the first section to export; otherwise meaningless</summary>
		int m_iFirstSection;
		/// <summary>if single book, index of the last section to export; otherwise meaningless</summary>
		int m_iLastSection;
		/// <summary>description of this work edited by user</summary>
		string m_sDescription;
		/// <summary>flag that we're processing an introductory section</summary>
		bool m_fInIntroduction = false;
		/// <summary>stores list of BT langs with the current footnote marked</summary>
		string m_sBTsWithFootnote = null;
		/// <summary>the database id of the free translation annotation</summary>
		int m_hvoFtSegmentDefn;
		/// <summary>list of notes for the current (upcoming) segmented back translation segments</summary>
		List<IScrScriptureNote> m_rgSegAnn = new List<IScrScriptureNote>();

		// This holds all the hvos of annotations that were exported. The hvos could be
		// stored in a list but we use a dictionary for speed. Therefore, the bool values
		// are useless and not used.
		Dictionary<int, bool> m_exportedAnnotations = new Dictionary<int, bool>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a new instance of the <see cref="ExportXml"/> class.
		/// </summary>
		/// <param name="fileName">pathname of the XML file to create</param>
		/// <param name="cache">data source</param>
		/// <param name="filter">lists the books to export</param>
		/// <param name="what">tells what to export: everything, filtered list, or single book</param>
		/// <param name="nBook">if single book, number of the book to export</param>
		/// <param name="iFirstSection">if single book, index of first section to export</param>
		/// <param name="iLastSection">if single book, index of last section to export</param>
		/// <param name="sDescription"></param>
		/// ------------------------------------------------------------------------------------
		public ExportXml(string fileName, FdoCache cache, FilteredScrBooks filter,
			ExportWhat what, int nBook, int iFirstSection, int iLastSection, string sDescription)
		{
			m_fileName = fileName;
			m_cache = cache;
			m_wsEnglish = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
			m_bookFilter = filter;
			m_what = what;
			m_nBookSingle = nBook;
			m_iFirstSection = iFirstSection;
			m_iLastSection = iLastSection;
			m_sDescription = sDescription;
			m_scr = cache.LangProject.TranslatedScriptureOA;
			m_hvoFtSegmentDefn = cache.GetIdFromGuid(LangProject.kguidAnnFreeTranslation);
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name of the file.
		/// </summary>
		/// <remarks>This is used when the file name needs to be corrected, for example, when
		/// the volume lacks a '\'. When the volume lacks a '\', the file exports fine but
		/// does not validate.</remarks>
		/// ------------------------------------------------------------------------------------
		public string FileName
		{
			get { return m_fileName; }
			set { m_fileName = value; }
		}
		#endregion

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Run the export
		/// </summary>
		/// <returns><c>true</c> if successful, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool Run()
		{
			bool fSuccessful = false;
			// Check whether we're about to overwrite an existing file.
			if (File.Exists(m_fileName))
			{
				string sFmt = DlgResources.ResourceString("kstidAlreadyExists");
				string sMsg = String.Format(sFmt, m_fileName);
				string sCaption = DlgResources.ResourceString("kstidExportOXES");
				if (MessageBox.Show(sMsg, sCaption, MessageBoxButtons.YesNo,
					MessageBoxIcon.Warning) == DialogResult.No)
				{
					return fSuccessful;
				}
			}
			try
			{
				try
				{
					m_writer = new XmlTextWriter(m_fileName, Encoding.UTF8);
				}
				catch (Exception e)
				{
					MessageBox.Show(e.Message, Application.ProductName,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
					return fSuccessful;
				}
				ExportTE();
				fSuccessful = true;
				return fSuccessful;
			}
			catch (Exception e)
			{
				Exception inner = e.InnerException != null ? e.InnerException : e;
				if (inner is IOException)
				{
					MessageBox.Show(inner.Message, Application.ProductName,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
					return fSuccessful;
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

		#region Event Handler(s)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cancel handler to cancel an import through the progress dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OnExportCancel(object sender)
		{
			m_cancel = true;
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export all of the data related to TE.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportTE()
		{
			m_writer.Formatting = Formatting.Indented;
			m_writer.Indentation = 1;
			m_writer.IndentChar = '\t';
			m_writer.WriteStartDocument();
			m_writer.WriteStartElement("oxes");
			m_writer.WriteAttributeString("xmlns", "http://www.wycliffe.net/scripture/namespace/version_1.1.2");
			m_writer.WriteStartElement("oxesText");
			m_writer.WriteAttributeString("type", "Wycliffe-1.1.2");
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
			ExportScripture();

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
			ILgWritingSystem lgwsVern = LgWritingSystem.CreateFromDBObject(m_cache, m_cache.DefaultVernWs);
			string sLang = lgwsVern.RFC4646bis.Normalize();
			foreach (ILgWritingSystem lgws in m_cache.LangProject.CurAnalysisWssRS)
			{
				string sRFC = lgws.RFC4646bis.Normalize();
				m_dictAnalLangs.Add(lgws.Hvo, sRFC);
				m_mapWsRFC.Add(lgws.Hvo, sRFC);
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
			System.Security.Principal.WindowsIdentity whoami = System.Security.Principal.WindowsIdentity.GetCurrent();
			string sWho = whoami.Name.Normalize();
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
		private string AbbreviateUserName(string sWho)
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
		private void ExportScripture()
		{
			int sectionCount = 0;
			for (int i = 0; i < m_bookFilter.BookCount; i++)
				sectionCount += m_bookFilter.GetBook(i).SectionsOS.Count;
			using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(Form.ActiveForm))
			{
				progressDlg.SetRange(0, sectionCount);
				progressDlg.Title = DlgResources.ResourceString("kstidExportXmlProgress");
				progressDlg.CancelButtonVisible = true;
				progressDlg.Cancel += new CancelHandler(OnExportCancel);

				progressDlg.RunTask(true, new BackgroundTaskInvoker(ExportScripture));
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
		private object ExportScripture(IAdvInd4 progressDlg, params object[] parameters)
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
					for (int bookIndex = 0; bookIndex < m_bookFilter.BookCount && !m_cancel; bookIndex++)
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
		private void ExportBook(ref string sCanon, IScrBook book, IAdvInd4 progressDlg)
		{
			// If we're using segmented backtranslations, load the segment information into the cache.
			if (Options.UseInterlinearBackTranslation)
			{
				foreach (int ws in m_cache.LangProject.AnalysisWssRC.HvoArray)
				{
					if (book.TitleOA != null)
						StTxtPara.LoadSegmentFreeTranslations(book.TitleOA.ParagraphsOS.HvoArray, m_cache, ws);
					foreach (IStFootnote fn in book.FootnotesOS)
						StTxtPara.LoadSegmentFreeTranslations(fn.ParagraphsOS.HvoArray, m_cache, ws);
				}
			}
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
					"kstidExportBookStatus"), book.Name.UserDefaultWritingSystem);
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
				if (m_cancel)
					break;
				if (progressDlg != null)
					progressDlg.Step(0);
			}
			if (m_fInIntroduction)
				m_writer.WriteEndElement();	// </introduction>
			FlushSectionElementStack(SectionLevel.Major);
			m_writer.WriteEndElement();		// </book>
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
				int ws = StringUtils.GetWsAtOffset(tssName, 0);
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
			// If we're using segmented backtranslations, load the segment information into the cache.
			if (Options.UseInterlinearBackTranslation)
			{
				foreach (int ws in m_cache.LangProject.AnalysisWssRC.HvoArray)
				{
					if (section.HeadingOA != null)
						StTxtPara.LoadSegmentFreeTranslations(section.HeadingOA.ParagraphsOS.HvoArray, m_cache, ws);
					if (section.ContentOA != null)
						StTxtPara.LoadSegmentFreeTranslations(section.ContentOA.ParagraphsOS.HvoArray, m_cache, ws);
				}
			}
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
				else if (nextSection != null &&
					nextSection.ContentOAHvo != 0 &&
					nextSection.ContentOA.ParagraphsOS.Count > 0)
				{
					nextPara = nextSection.ContentOA.ParagraphsOS[0];
				}
				ExportParagraph((IStTxtPara)para, (IStTxtPara)nextPara, true);
				if (fIntro)
					ExportAnnotationsForPara(para.Hvo);
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
					{
						m_writer.WriteStartElement("sectionHead");
						ExportAnnotationsForPara(para.Hvo);
					}
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
		private bool WriteSectionHead(FdoOwningSequence<IStPara> paras, int i, string style,
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
		private bool HandleSpeechSpeaker(FdoOwningSequence<IStPara> paras, int iPara)
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
		private bool HandleSecondaryTitle(FdoOwningSequence<IStPara> paras, int iPara, string style,
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
			ITsString tss = para.Contents.UnderlyingTsString;
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
			ITsString tss = para.Contents.UnderlyingTsString;
			ITsTextProps ttp = tss.get_Properties(0);
			string styleName = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			return styleName == ScrStyleNames.VerseNumber;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store the information for one segment of a segmented back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal class BTSegment
		{
			private int m_ichBegin;
			private int m_ichEnd;
			private Dictionary<int, ITsString> m_mapWsTss = new Dictionary<int, ITsString>();
			private Dictionary<int, string> m_mapWsStatus = new Dictionary<int, string>();
			private ICmObject m_obj;

			internal BTSegment(FdoCache cache, int hvoSeg, ICmObject obj)
			{
				ICmBaseAnnotation ann = CmObject.CreateFromDBObject(cache, hvoSeg) as ICmBaseAnnotation;
				m_ichBegin = ann.BeginOffset;
				m_ichEnd = ann.EndOffset;
				m_obj = obj;
			}

			internal BTSegment(int ichBegin, int ichEnd, ICmObject obj)
			{
				m_ichBegin = ichBegin;
				m_ichEnd = ichEnd;
				m_obj = obj;
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
				else
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
				else
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
			int m_wsDefaultAnal;
			int m_hvo;

			internal BackTranslationInfo(ICmTranslation backtran)
			{
				foreach (int ws in backtran.Cache.LangProject.AnalysisWssRC.HvoArray)
				{
					ITsString tss = backtran.Translation.GetAlternative(ws).UnderlyingTsString;
					if (tss.Length > 0 && tss != backtran.Translation.NotFoundTss)
						m_mapWsTssTran.Add(ws, tss);
					string status = backtran.Status.GetAlternative(ws);
					if (!String.IsNullOrEmpty(status) && status != backtran.Status.NotFoundTss.Text)
						m_mapWsTssStatus.Add(ws, status);
				}
				m_wsDefaultAnal = backtran.Cache.DefaultAnalWs;
				m_hvo = backtran.Hvo;
			}

			internal BackTranslationInfo(ICmPicture pict)
			{
				foreach (int ws in pict.Cache.LangProject.AnalysisWssRC.HvoArray)
				{
					ITsString tss = pict.Caption.GetAlternative(ws).UnderlyingTsString;
					if (tss.Length > 0 && tss != pict.Caption.NotFoundTss)
						m_mapWsTssTran.Add(ws, tss);
				}
				m_wsDefaultAnal = pict.Cache.DefaultAnalWs;
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
		/// <param name="fNullParaIsEndOfBook">flag whether to treat an empty next paragraph as the end of
		/// the book</param>
		/// ------------------------------------------------------------------------------------
		private void ExportParagraph(IStTxtPara para, IStTxtPara nextPara, bool fNullParaIsEndOfBook)
		{
			Debug.Assert(para.StyleRules != null, "Paragraph StyleRules should never be null");
			string styleName = para.StyleName;
			OxesInfo xinfo = OxesInfo.GetOxesInfoForParaStyle(styleName, m_fInSectionHeader);

			// Use segmented back translation if it exists and is desired.
			List<BTSegment> rgBTSeg = null;		// segmented back translation information.
			BackTranslationInfo backtran = null;		// "normal" back translation.
			if (Options.UseInterlinearBackTranslation)
				rgBTSeg = GetSegmentedBTInfo(para);
			if (rgBTSeg == null || rgBTSeg.Count == 0)
				backtran = GetBackTranslationForPara(para);

			CloseNestingElementsAsNeeded(styleName, xinfo);
			if (OpenNestingElementsAsNeeded(para, styleName, xinfo))
				return;		// some nesting elements (eg "row") have no content.

			if (HandleHeaderParagraph(para, styleName, xinfo, backtran, rgBTSeg))
				return;		// some header paragraphs are handled completely.

			// Special handling for List Item N / List Item N Additional
			bool fKeepOpen = HandleListItemParagraphs(nextPara, styleName);

			m_writer.WriteStartElement(xinfo.XmlTag);
			WriteParagraphAttribute(para, xinfo.XmlTag, xinfo.AttrName, xinfo.AttrValue);
			WriteParagraphAttribute(para, xinfo.XmlTag, xinfo.AttrName2, xinfo.AttrValue2);
			if (m_fInFootnote && xinfo.XmlTag == "note" && xinfo.AttrName == "type" &&
				!String.IsNullOrEmpty(m_sBTsWithFootnote))
			{
				WriteParagraphAttribute(para, xinfo.XmlTag, "markerExistsInBT", m_sBTsWithFootnote);
				m_sBTsWithFootnote = null;
			}
			if (m_fInIntroduction)
			{
				ExportAnnotationsForPara(para.Hvo);
				if (backtran != null)
					ExportAnnotationsForPara(backtran.Hvo);
			}
			ExportParagraphData(para.Hvo, para.Contents.UnderlyingTsString, backtran, rgBTSeg);
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
				Debug.Assert(m_fListItem1Open || m_fListItem2Open);
			}
		}

		private List<BTSegment> GetSegmentedBTInfo(IStTxtPara para)
		{
			List<BTSegment> rgBTSeg = new List<BTSegment>();
			if (para == null || para.Hvo == 0)
				return rgBTSeg;

			// Backtranslation must exist if segmented BT exists
			ICmTranslation backtran = (para as StTxtPara).GetBT();
			if (backtran == null)
				return rgBTSeg;

			// Check to see if there is at least one segmented BT
			bool hasSegmentedBT = false;
			foreach (int ws in m_dictAnalLangs.Keys)
			{
				if (backtran.Translation.GetAlternative(ws).Length > 0)
				{
					if (!(para as StTxtPara).HasNoSegmentBt(ws))
					{
						hasSegmentedBT = true;
						break;
					}
				}
			}

			if (!hasSegmentedBT)
				return rgBTSeg;

			// create list of WS used for back translations. Segment all
			// BTs that aren't currently segmented
			List<int> rgWsBt = new List<int>();
			foreach (int ws in m_dictAnalLangs.Keys)
			{
				if (backtran.Translation.GetAlternative(ws).Length > 0)
				{
					if ((para as StTxtPara).HasNoSegmentBt(ws))
						new BtConverter(para).ConvertCmTransToInterlin(ws);
					rgWsBt.Add(ws);
				}
			}

			if (rgWsBt.Count > 0)
			{
				int kflidSegments = StTxtPara.SegmentsFlid(m_cache);
				int kflidFt = StTxtPara.SegmentFreeTranslationFlid(m_cache);
				ISilDataAccess sda = m_cache.MainCacheAccessor;
				int cseg = sda.get_VecSize(para.Hvo, kflidSegments);
				for (int iseg = 0; iseg < cseg; iseg++)
				{
					int hvoSeg = sda.get_VecItem(para.Hvo, kflidSegments, iseg);
					BTSegment bts = new BTSegment(m_cache, hvoSeg, para);
					int hvoFt = sda.get_ObjectProp(hvoSeg, kflidFt);
					foreach (int ws in rgWsBt)
					{
						ITsString tss = sda.get_MultiStringAlt(hvoFt,
							(int)CmAnnotation.CmAnnotationTags.kflidComment, ws);
						bts.SetTransForWs(ws, tss);
						string status = backtran.Status.GetAlternative(ws);
						if (!String.IsNullOrEmpty(status))
							bts.SetBtStatusForWs(ws, status);
					}
					rgBTSeg.Add(bts);
				}
			} return rgBTSeg;
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
			if (m_stackTableElements.Count > 0 && !m_fInFootnote &&
				xinfo.Context != OxesContext.Table && xinfo.Context != OxesContext.Row)
			{
				FinishTable();
			}
			if (m_fListItem1Open && !m_fInFootnote && styleName != ScrStyleNames.ListItem1Additional)
			{
				m_writer.WriteEndElement();	// </item>
				m_fListItem1Open = false;
			}
			if (m_fListItem2Open && !m_fInFootnote && styleName != ScrStyleNames.ListItem2Additional)
			{
				m_writer.WriteEndElement();	// </item>
				m_fListItem2Open = false;
			}
			if (m_fEmbedded && !m_fInFootnote && xinfo.Context != OxesContext.Embedded)
			{
				m_writer.WriteEndElement();	// </embedded>
				m_fEmbedded = false;
			}
			if (m_fInSpeech && !m_fInFootnote && xinfo.Context != OxesContext.Speech)
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
					ExportParagraphData(para.Hvo, para.Contents.UnderlyingTsString, backtran, rgBTSeg);
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
						int clid = m_cache.GetClassOfObject(para.OwnerHVO);
						if (clid == StText.kclsidStText)
						{
							IStText text = StText.CreateFromDBObject(m_cache, para.OwnerHVO);
							fBaseRTL = text.RightToLeft;
						}
						else if (para.Contents.UnderlyingTsString != null)
						{
							ITsTextProps ttp = para.Contents.UnderlyingTsString.get_PropertiesAt(0);
							nVal = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
							if (nVal > 0)
								fBaseRTL = m_cache.GetBoolProperty(nVal,
									(int)LgWritingSystem.LgWritingSystemTags.kflidRightToLeft);
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
		/// <param name="para"></param>
		/// <param name="backtran"></param>
		/// <param name="fExportAnnotation"></param>
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
			ITsString tssPara = para.Contents.UnderlyingTsString;
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
						m_writer.WriteStartElement("bt");
						m_writer.WriteAttributeString("xml", "lang", null, m_dictAnalLangs[ws]);
						string status;
						if (mapWsStatus.TryGetValue(ws, out status))
							m_writer.WriteAttributeString("status", status.ToLower());
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
					fBackTransOpen = false;
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
				if (tran.TypeRA.Guid == FDO.LangProj.LangProject.kguidTranBackTranslation)
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
			else
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
			else
				return NextParaStartsWithGivenStyle(nextPara, ScrStyleNames.VerseNumber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check whether the given paragraph begins with the given character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static bool NextParaStartsWithGivenStyle(IStTxtPara nextPara, string sStyleWanted)
		{
			ITsString tssPara = nextPara.Contents.UnderlyingTsString;
			int crun = tssPara.RunCount;
			if (crun > 0)
			{
				ITsTextProps ttp = tssPara.get_Properties(0);
				string styleName = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				return styleName == sStyleWanted;
			}
			else
			{
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the embedded object is a footnote or picture, write it out.
		/// </summary>
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
						int hvoFootnote = m_cache.GetIdFromGuid(footnoteGuid);
						if (hvoFootnote != 0)
							ExportFootnote(new ScrFootnote(m_cache, hvoFootnote));
						break;
					case (char)FwObjDataTypes.kodtGuidMoveableObjDisp:
						Guid pictureGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
						int hvoPicture = m_cache.GetIdFromGuid(pictureGuid);
						if (hvoPicture != 0)
							ExportPicture(new CmPicture(m_cache, hvoPicture));
						break;
					case (char)FwObjDataTypes.kodtPictEven:
						m_writer.WriteComment("object type PictEven not handled");
						break;
					case (char)FwObjDataTypes.kodtPictOdd:
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
		/// Write all the paragraphs in the given footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportFootnote(ScrFootnote scrFootnote)
		{
			m_fInFootnote = true;
			// TE allows only one paragraph in a footnote!
			Debug.Assert(scrFootnote.ParagraphsOS.Count < 2);
			foreach (IStTxtPara para in scrFootnote.ParagraphsOS)
				ExportParagraph(para, null, false);
			m_fInFootnote = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write out the picture as a &lt;figure&gt; element.
		/// </summary>
		/// <param name="pict"></param>
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
			string sPath = pict.PictureFileRA.AbsoluteInternalPath.Normalize();
			string sFile = Path.GetFileName(sPath);
			WriteFigureAttributes(sCaption, sFile);
			m_writer.WriteComment(String.Format("path=\"{0}\"", sPath));
			m_writer.WriteStartElement("caption");
			OpenTrGroupIfNeeded();
			List<BTSegment> rgbts = null;
			BackTranslationInfo trans = null;
			if (Options.UseInterlinearBackTranslation)
			{
				rgbts = new List<BTSegment>();
				BTSegment bts = new BTSegment(0, sCaption.Normalize(NormalizationForm.FormD).Length, pict);
				foreach (int ws in m_dictAnalLangs.Keys)
				{
					ITsString tss = pict.Caption.GetAlternative(ws).UnderlyingTsString;
					if (tss.Length > 0 && tss != pict.Caption.NotFoundTss)
						bts.SetTransForWs(ws, tss.get_NormalizedForm(FwNormalizationMode.knmNFC));
				}
				rgbts.Add(bts);
			}
			else
			{
				trans = new BackTranslationInfo(pict);
			}
			if (tssCaption.Length > 0)
				ExportParagraphData(0, tssCaption, trans, rgbts);
			CloseTrGroupIfNeeded();
			m_writer.WriteEndElement();	//</caption>
			m_writer.WriteEndElement();	//</figure>
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
		/// Export the data for a single paragraph.
		/// </summary>
		/// <param name="hvoPara"></param>
		/// <param name="tssPara"></param>
		/// <param name="backtran"></param>
		/// <param name="rgBTSeg">list of segments in a segmented back translation</param>
		/// ------------------------------------------------------------------------------------
		private void ExportParagraphData(int hvoPara, ITsString tssPara, BackTranslationInfo backtran,
			List<BTSegment> rgBTSeg)
		{
			if (rgBTSeg != null && rgBTSeg.Count > 0)
			{
				WriteParagraphWithSegmentedBT(tssPara, rgBTSeg);
			}
			else
			{
				WriteParagraphWithStandardBackTranslation(tssPara, backtran);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write the paragraph using a standard back translation.
		/// </summary>
		/// <param name="tssPara"></param>
		/// <param name="backtran"></param>
		/// ------------------------------------------------------------------------------------
		private void WriteParagraphWithStandardBackTranslation(ITsString tssPara,
			BackTranslationInfo backtran)
		{
			Dictionary<int, ITsString> dictBackTrans;
			Dictionary<int, int> mapWsBackTransRunIndex;
			Dictionary<int, string> mapWsStatus = FillBackTransForPara(backtran,
				out dictBackTrans, out mapWsBackTransRunIndex);

			if (tssPara.Length == 0)
			{
				OpenTrGroupIfNeeded();
				WriteBackTrans(mapWsStatus, dictBackTrans, mapWsBackTransRunIndex, Guid.Empty);
				CloseTrGroupIfNeeded();
			}
			else
			{
				int runCount = tssPara.RunCount;
				List<int> rgws;
				List<OxesInfo> rgInfo = CollectRunInfo(tssPara, out rgws);
				for (int run = 0; run < runCount; ++run)
				{
					string data = tssPara.get_RunText(run);
					if (!String.IsNullOrEmpty(data))
						data = data.Normalize();
					OxesInfo xinfo = rgInfo[run];
					int ws = rgws[run];
					switch (xinfo.StyleName)
					{
						case ScrStyleNames.ChapterNumber:
							ProcessChapterNumber(data, mapWsStatus, dictBackTrans, mapWsBackTransRunIndex);
							break;
						case ScrStyleNames.VerseNumber:
						case ScrStyleNames.VerseNumberInNote:
						case ScrStyleNames.VerseNumberAlternate:
							ProcessVerseNumber(data, mapWsStatus, dictBackTrans, mapWsBackTransRunIndex,
								xinfo.StyleName == ScrStyleNames.VerseNumberAlternate);
							break;
						default:
							if (IsObjectReference(data))
							{
								ITsTextProps props = tssPara.get_Properties(run);
								Guid guidFootnote = GetFootnoteGuidIfAny(props);
								m_sBTsWithFootnote = null;
								if (m_fInTrGroup)
								{
									CloseTranslationElementIfNeededAndWriteBackTrans(mapWsStatus,
										dictBackTrans, mapWsBackTransRunIndex, guidFootnote);
								}

								ExportEmbeddedObject(props);
							}
							else
							{
								// This checks for chapters that do not contain verse one. If that
								// is the case, then we need to make sure annotations associated
								// with the inferred verse one get exported. See TE-7720.
								if (m_iCurrentVerse == 0 && run - 1 >= 0 &&
									rgInfo[run - 1].StyleName == ScrStyleNames.ChapterNumber)
								{
									m_iCurrentVerse = 1;
									ExportAnyRelatedAnnotation();
								}

								OpenTrGroupIfNeeded();
								OpenTranslationElementIfNeeded();
								WriteRunDataWithEmbeddedStyleInfo(rgInfo, rgws, run, m_cache.DefaultVernWs, data);
							}
							break;
					}
				}

				CloseTranslationElementIfNeededAndWriteBackTrans(mapWsStatus,
					dictBackTrans, mapWsBackTransRunIndex, Guid.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write the paragraph using a segmented back translation.
		/// </summary>
		/// <param name="tssPara"></param>
		/// <param name="rgBTSeg"></param>
		/// ------------------------------------------------------------------------------------
		private void WriteParagraphWithSegmentedBT(ITsString tssPara, List<BTSegment> rgBTSeg)
		{
			int cseg = rgBTSeg.Count;
			if (tssPara.Length > 0)
			{
				int crun = tssPara.RunCount;
				List<int> rgws;
				List<OxesInfo> rgInfo = CollectRunInfo(tssPara, out rgws);
				int iseg = 0;
				BTSegment bts = rgBTSeg[0];
				for (int irun = 0; irun < crun; ++irun)
				{
					TsRunInfo tri;
					ITsTextProps ttp = tssPara.FetchRunInfo(irun, out tri);
					if (tri.ichMin >= bts.EndOffset)
					{
						++iseg;
						if (iseg < cseg)
							bts = rgBTSeg[iseg];
					}
					string data = tssPara.get_RunText(irun);
//#if DEBUG
//                    string sTRDebug = String.Format("Run[{0}]   min={1,4}, lim={2,4}; data='{3}'", irun, tri.ichMin, tri.ichLim, data);
//                    Debug.WriteLine(sTRDebug);
//#endif
					string dataNFC = String.Empty;
					if (!String.IsNullOrEmpty(data))
						dataNFC = data.Normalize();
					OxesInfo xinfo = rgInfo[irun];
					int ws = rgws[irun];
					switch (xinfo.StyleName)
					{
						case ScrStyleNames.ChapterNumber:
//#if DEBUG
//                            WriteSegmentedBackTransDebugInfo(bts, iseg);
//                            Debug.Assert(bts.BeginOffset == tri.ichMin);
//#endif
							ProcessChapterNumber(dataNFC, null, null, null);
							break;
						case ScrStyleNames.VerseNumber:
						case ScrStyleNames.VerseNumberInNote:
						case ScrStyleNames.VerseNumberAlternate:
//#if DEBUG
//                            WriteSegmentedBackTransDebugInfo(bts, iseg);
//                            Debug.Assert(bts.EndOffset == tri.ichLim);
//#endif
							ProcessVerseNumber(dataNFC, null, null, null,
								xinfo.StyleName == ScrStyleNames.VerseNumberAlternate);
							break;
						default:
							if (IsObjectReference(dataNFC))
							{
//#if DEBUG
//                                WriteSegmentedBackTransDebugInfo(bts, iseg);
//                                Debug.Assert(bts.EndOffset == tri.ichLim);
//#endif
								Guid guidFootnote = GetFootnoteGuidIfAny(ttp);
								m_sBTsWithFootnote = null;
								if (m_fInTrGroup)
									CloseTranslationElementIfNeededAndWriteBackTrans(bts, tri, guidFootnote);
								ExportEmbeddedObject(ttp);
							}
							else
							{
								// Write all the segments that are fully contained in this run,
								// or that end in this run.
								while (iseg < cseg && bts.EndOffset <= tri.ichLim)
								{
									WritePartialRunWithBTSegment(tssPara, irun, ref tri, rgws,
										rgInfo, iseg, bts);
									++iseg;
									if (iseg < cseg)
										bts = rgBTSeg[iseg];
								}
								// Write any partial segment that starts (or continues) in this
								// run, but finishes in a later run.
								if (iseg < cseg && bts.BeginOffset < tri.ichLim)
								{
									WritePartialRunWithBTSegment(tssPara, irun, ref tri, rgws,
										rgInfo, iseg, bts);
								}
							}
							break;
					}
				}
			}
			else
			{
				for (int iseg = 0; iseg < cseg; ++iseg)
				{
					OpenTrGroupIfNeeded();
					WriteSegmentedBackTrans(rgBTSeg[iseg], iseg);
				}
			}
		}

		private void CloseTranslationElementIfNeededAndWriteBackTrans(BTSegment bts, TsRunInfo tri,
			Guid guidFootnote)
		{
			CloseTranslationElementIfNeeded();
			WriteSegmentedBackTrans(bts, -1);
			CloseTrGroupIfNeeded();
		}

		private void WritePartialRunWithBTSegment(ITsString tssPara, int irun, ref TsRunInfo tri,
			List<int> rgws, List<OxesInfo> rgInfo,
			int iseg, BTSegment bts)
		{
			string dataNFC = GetPartialRunData(tssPara, ref tri, bts);
			OpenTrGroupIfNeeded();
			OpenTranslationElementIfNeeded();
			WriteRunDataWithEmbeddedStyleInfo(rgInfo, rgws, irun, m_cache.DefaultVernWs, dataNFC);
			if (bts.EndOffset <= tri.ichLim)
				WriteSegmentedBackTrans(bts, iseg);
		}

		private string GetPartialRunData(ITsString tssPara, ref TsRunInfo tri, BTSegment bts)
		{
			ITsStrBldr tsb = tssPara.GetBldr();
			int iMin = Math.Max(tri.ichMin, bts.BeginOffset);
			int iLim = Math.Min(tri.ichLim, bts.EndOffset);
			Debug.Assert(iMin <= iLim);
			tsb.ReplaceTsString(iLim, tssPara.Length, null);
			tsb.ReplaceTsString(0, iMin, null);
			string dataSeg = tsb.Text;
			string dataNFC = String.Empty;
			if (dataSeg != null)
				dataNFC = dataSeg.Normalize();
			return dataNFC;
		}

		private void WriteSegmentedBackTrans(BTSegment bts, int iseg)
		{
//#if DEBUG
//            WriteSegmentedBackTransDebugInfo(bts, iseg);
//#endif
			CloseTranslationElementIfNeeded();
			foreach (int ws in bts.AvailableTranslations)
			{
				ITsString tss = bts.GetTransForWs(ws);
				string data = tss.Text;
				if (!String.IsNullOrEmpty(data))
				{
					OpenTrGroupIfNeeded();
					m_writer.WriteStartElement("bt");
					m_writer.WriteAttributeString("xml", "lang", null, m_dictAnalLangs[ws]);
					m_writer.WriteAttributeString("segmented", "true");
					string status = bts.GetBtStatusForWs(ws);
					if (!String.IsNullOrEmpty(status))
						m_writer.WriteAttributeString("status", status.ToLower());
					//  OxesInfo xinfo = OxesInfo.GetOxesInfoForCharStyle(styleName);
					//  if (!String.IsNullOrEmpty(xinfo.XmlTag))
					//  {
					//      m_writer.WriteStartElement(xinfo.XmlTag);
					//      if (!String.IsNullOrEmpty(xinfo.AttrName) && !String.IsNullOrEmpty(xinfo.AttrValue))
					//          m_writer.WriteAttributeString(xinfo.AttrName, xinfo.AttrValue);
					//  }
					m_writer.Formatting = Formatting.None;
					WriteSegmentAnnotations(bts, data, ws);
					m_writer.WriteString(data.Normalize());
					m_writer.WriteEndElement();
					m_writer.Formatting = Formatting.Indented;
				}
			}
			CloseTrGroupIfNeeded();
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
							Debug.Assert(ba.BeginObjectRAHvo == bts.TargetObject.Hvo);
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
			else
			{
				return Guid.Empty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write the current run to the XML file.  If the next style element can be embedded
		/// inside the current one, then the current element is not closed, and its OxesInfo
		/// data is pushed onto a stack.  Otherwise, the current element is closed.
		/// </summary>
		/// <param name="rgInfo"></param>
		/// <param name="run"></param>
		/// <param name="rgws"></param>
		/// <param name="wsDefault"></param>
		/// <param name="data"></param>
		/// ------------------------------------------------------------------------------------
		private void WriteRunDataWithEmbeddedStyleInfo(List<OxesInfo> rgInfo, List<int> rgws,
			int run, int wsDefault, string data)
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
			if (!String.IsNullOrEmpty(xinfo.XmlTag) && !fCurrentEqualsTopInfo)
			{
				m_writer.WriteStartElement(xinfo.XmlTag);
				if (!String.IsNullOrEmpty(xinfo.AttrName) && !String.IsNullOrEmpty(xinfo.AttrValue))
					m_writer.WriteAttributeString(xinfo.AttrName, xinfo.AttrValue);
			}

			bool fForeign = MarkForeignIfNeeded(wsDefault, ws);
			if (!String.IsNullOrEmpty(data))
				m_writer.WriteString(data);
			if (fForeign)
				m_writer.WriteEndElement();	//</foreign>

			if (!String.IsNullOrEmpty(xinfo.XmlTag))
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

		/// <summary>
		/// Flag this run as being in a foreign language if needed, and return true if so
		/// flagged.
		/// </summary>
		/// <param name="wsDefault"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		private bool MarkForeignIfNeeded(int wsDefault, int ws)
		{
			if (ws > 0 && ws != wsDefault)
			{
				m_writer.WriteStartElement("foreign");
				m_writer.WriteAttributeString("xml", "lang", null, GetRFCFromWs(ws));
				return true;
			}
			else
			{
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open a &lt;tr&gt; element if one is not already open, and turn off output formatting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OpenTranslationElementIfNeeded()
		{
			if (!m_fTranslationElementIsOpen)
			{
				m_writer.WriteStartElement("tr");
				m_fTranslationElementIsOpen = true;
				m_writer.Formatting = Formatting.None;
				m_stackTextRunInfo.Clear();		// probably not needed, but safe.
			}
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
				ILgWritingSystem lgws = LgWritingSystem.CreateFromDBObject(m_cache, ws);
				sRFC = lgws.RFC4646bis.Normalize();
				m_mapWsRFC.Add(ws, sRFC);
			}
			return sRFC;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close the &lt;tr&gt; element if one is open, and turn on output formatting.  Also
		/// write any back translation data that is appropriate at this point.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CloseTranslationElementIfNeededAndWriteBackTrans(Dictionary<int, string> mapWsStatus,
			Dictionary<int, ITsString> dictBackTrans, Dictionary<int, int> mapWsBackTransRunIndex,
			Guid guidFootnote)
		{
			CloseTranslationElementIfNeeded();
			WriteBackTrans(mapWsStatus, dictBackTrans, mapWsBackTransRunIndex, guidFootnote);
			CloseTrGroupIfNeeded();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close the tr element if one is open, and turn on output formatting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CloseTranslationElementIfNeeded()
		{
			if (m_fTranslationElementIsOpen)
			{
				while (m_stackTextRunInfo.Count > 0)
				{
					m_writer.WriteEndElement();
					m_stackTextRunInfo.Pop();
				}
				m_writer.WriteEndElement();		// </tr>
				m_fTranslationElementIsOpen = false;
				m_writer.Formatting = Formatting.Indented;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open a &lt;trGroup&gt; element if one is not open.
		/// </summary>
		/// <returns>true if &lt;trGroup&gt; element was written, otherwise false</returns>
		/// ------------------------------------------------------------------------------------
		private bool OpenTrGroupIfNeeded()
		{
			if (!m_fInTrGroup)
			{
				m_writer.WriteStartElement("trGroup");
				m_fInTrGroup = true;
				return true;
			}
			else
			{
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close the &lt;trGroup&gt; element if one is open.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CloseTrGroupIfNeeded()
		{
			if (m_fInTrGroup)
			{
				m_writer.WriteEndElement();	//</trGroup>
				m_fInTrGroup = false;
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
		/// <param name="guidFootnote">GUID of a footnote reference, or Guid.Empty</param>
		/// ------------------------------------------------------------------------------------
		private void WriteBackTrans(Dictionary<int, string> mapWsStatus,
			Dictionary<int, ITsString> dictBackTrans, Dictionary<int,int> mapWsBackTransRunIndex,
			Guid guidFootnote)
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
						{
							if (guidBT == guidFootnote)
							{
								setwsFootnote.Add(ws);
								++irun;		// we've handled this run, so move to next run.
								break;
							}
							else
							{
								// if it doesn't match now, it should match another footnote
								// coming up in the baseline translated text.  if not, we have
								// bad data anyway (which TE tries to protect against)!
								// Come back to this run next time.
								break;
							}
						}
						else
						{
							// don't expect other object types in back translations, so
							// skip over it.
							continue;
						}
					}
					string styleName = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
					string sRemainder;
					if (styleName == ScrStyleNames.ChapterNumber)
					{
						int iChapter = StringUtils.strtol(data, out sRemainder);
						if (fBackTransOpen || iChapter > m_iCurrentChapter)
							break;
						else
							continue;
					}
					else if (styleName == ScrStyleNames.VerseNumber ||
						styleName == ScrStyleNames.VerseNumberAlternate)
					{
						int iVerse = StringUtils.strtol(data, out sRemainder);
						if (fBackTransOpen || iVerse > m_iCurrentVerse)
							break;
						else
							continue;
					}
					else if (styleName == ScrStyleNames.VerseNumberInNote)
					{
						if (fBackTransOpen)
							break;
						else
							continue;
					}
					else
					{
						if (!String.IsNullOrEmpty(data))
						{
							if (OpenTrGroupIfNeeded())
								fCloseTrGroup = true;
							if (!fBackTransOpen)
							{
								m_writer.WriteStartElement("bt");
								m_writer.WriteAttributeString("xml", "lang", null, m_dictAnalLangs[ws]);
								string status;
								if (mapWsStatus.TryGetValue(ws, out status))
									m_writer.WriteAttributeString("status", status.ToLower());
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
				}
				if (fBackTransOpen)
				{
					m_writer.WriteEndElement();
					fBackTransOpen = false;
					m_writer.Formatting = Formatting.Indented;
				}
				mapWsBackTransRunIndex[ws] = irun;
			}
			StringBuilder sbBTMarked = new StringBuilder();
			foreach (int ws in setwsFootnote)
			{
				string sLang;
				if (!m_dictAnalLangs.TryGetValue(ws, out sLang))
				{
					ILgWritingSystem lws = LgWritingSystem.CreateFromDBObject(m_cache, ws);
					sLang = lws.RFC4646bis;
					m_dictAnalLangs.Add(ws, sLang);
				}
				if (sbBTMarked.Length == 0)
					sbBTMarked.Append(sLang);
				else
					sbBTMarked.AppendFormat(",{0}", sLang);
			}
			if (sbBTMarked.Length > 0)
				m_sBTsWithFootnote = sbBTMarked.ToString();
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
			return data != null && data.Length == 1 && data[0] == StringUtils.kchObject;
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
				dictBackTrans, mapWsBackTransRunIndex, Guid.Empty);
			// If the user kept going in verse number style after the verse number, trim the
			// "verse number" at the first space.
			string sVerseNumber = data.Trim().Split(new char[] {' ', '\t', '\n', '\r'})[0];
			if (String.IsNullOrEmpty(sVerseNumber))
				return;
			if (!m_fInFootnote)
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
				if (m_fInFootnote)
					sVerseRef = String.Format("{0}.1.{1}.note", m_sCurrentBookId, sVerseNumber);
				else
					sVerseRef = String.Format("{0}.1.{1}{2}", m_sCurrentBookId, sVerseNumber, m_sVariantVerseRef);
			}
			else
			{
				if (m_fInFootnote)
					sVerseRef = String.Format("{0}.{1}.{2}.note", m_sCurrentBookId, m_sCurrentChapterNumber, sVerseNumber);
				else
					sVerseRef = String.Format("{0}.{1}.{2}{3}", m_sCurrentBookId, m_sCurrentChapterNumber, sVerseNumber, m_sVariantVerseRef);
			}
			if (m_fInFootnote)
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
			if (!m_fInFootnote)
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
				dictBackTrans, mapWsBackTransRunIndex, Guid.Empty);
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
		/// If any annotions refer to the current paragraph, export them.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportAnnotationsForPara(int hvoPara)
		{
			IScrBookAnnotations sba =
				m_cache.LangProject.TranslatedScriptureOA.BookAnnotationsOS[m_iCurrentBook - 1];

			if (sba == null || sba.NotesOS.Count == 0)
				return;

			for (int i = 0; i < sba.NotesOS.Count; ++i)
			{
				if (sba.NotesOS[i].BeginObjectRAHvo == hvoPara)
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
				if (ann.BeginObjectRA.OwningFlid == (int)StTxtPara.StTxtParaTags.kflidTranslations)
				{
					// then we need to include the "languageInFocus" attribute on the para as well.
					if (!m_mapWsRFC.TryGetValue(ann.WsSelector, out sLanguageInFocus))
					{
						IWritingSystem ws = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ann.WsSelector);
						if (ws != null)
							sLanguageInFocus = ws.IcuLocale;
					}
				}
				else if (ann.BeginObjectRA is ICmIndirectAnnotation &&
					(ann.BeginObjectRA as ICmIndirectAnnotation).AnnotationTypeRAHvo == m_hvoFtSegmentDefn)
				{
					// Save this to process later inside the <bt> element.
					m_rgSegAnn.Add(ann);
					return;
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
		private static void AddReferenceToBldr(StringBuilder bldrRef, int reference)
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
		private void ExportNotationParagraphs(IStJournalText text, string sTag)
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
				if (para.Contents.UnderlyingTsString != null)
				{
					ITsTextProps ttp = para.Contents.UnderlyingTsString.get_Properties(0);
					int nVar;
					int wsDefault = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
					if (wsDefault > 0)
						m_writer.WriteAttributeString("xml", "lang", null, GetRFCFromWs(wsDefault));
					for (int iRun = 0; iRun < para.Contents.UnderlyingTsString.RunCount; iRun++)
					{

						ttp = para.Contents.UnderlyingTsString.get_Properties(iRun);
						if (StringUtils.IsHyperlink(ttp))
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
								if (sProp != StStyle.Hyperlink)
									m_writer.WriteAttributeString("type", sProp);
							}
							else if (!StringUtils.WriteHref(tpt, sProp, m_writer))
							{
								throw new Exception("Unexpected string property in annotation field: " +
									sTag + ". FwTextPropType = " + tpt + "; Property = " + sProp ?? "null");
							}
						}
						string runText = para.Contents.UnderlyingTsString.get_RunText(iRun);
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
