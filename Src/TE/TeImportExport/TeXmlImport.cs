// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeXmlImport.cs
// Responsibility:
//
// <remarks>
// Implementation of TeXmlImporter, a subclass of TeImporter
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This subclass handles importing from an XML (Open XML for Editing Scripture) file.
	/// </summary>
	/// <remarks>
	/// Note: this also runs on a background thread. It can't call any UI methods directly!
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	public class TeXmlImporter : TeImporter
	{
		#region Variable, enumeration, and internal class declarations
		/// <summary>pathname of the OXES file for XML import</summary>
		private string m_sOXESFile = null;
		// TODO WS: what should we do with this directory?
		/// <summary>directory containing the OXES file</summary>
		private string m_sOXESDir;
		/// <summary>Text props for verse number character style</summary>
		private ITsTextProps m_ttpVerseNumber;
		/// <summary>Text props for verse number character style in footnotes</summary>
		private ITsTextProps m_ttpVerseNumberInNote;
		/// <summary>Text props for back-trans verse number character styles</summary>
		private Dictionary<int, ITsTextProps> m_ttpBTVerseNumbers = new Dictionary<int, ITsTextProps>();
		/// <summary>Text props for back-trans verse number character styles in footnotes</summary>
		private Dictionary<int, ITsTextProps> m_ttpBTVerseNumberInNotes = new Dictionary<int, ITsTextProps>();
		/// <summary>Text props for back-trans chapter number character styles</summary>
		private Dictionary<int, ITsTextProps> m_ttpBTChapterNumbers = new Dictionary<int, ITsTextProps>();
		/// <summary>
		/// Additional context provided by a stack of section type attribute values
		/// </summary>
		private Stack<string> m_stackSectionType = new Stack<string>();
		/// <summary>
		/// Additional context provided by certain XML elements (like &lt;embedded&gt; or &lt;table&gt;)
		/// </summary>
		private Stack<string> m_stackXmlContext = new Stack<string>();
		/// <summary>
		/// String builders to construct back-trans paragraph strings containing chapter and verse
		/// numbers.
		/// </summary>
		private Dictionary<int, ITsStrBldr> m_BTNumberStrBldrs = new Dictionary<int, ITsStrBldr>();
		/// <summary>
		/// Saved values for m_BTStrBldrs while processing a subunit (footnote/picture)
		/// </summary>
		private Dictionary<int, ITsString> m_SavedBTStrs = new Dictionary<int, ITsString>();
		/// <summary>
		/// A mapping from the footnote specified in the OXES file to the Guid of the associated footnote
		/// created during the import process
		/// </summary>
		private Dictionary<string, Guid> m_footnoteMapping = new Dictionary<string, Guid>();
		/// <summary>
		/// Saved values for m_BTNumberStrBldrs while processing a subunit (footnote/picture)
		/// </summary>
		private Dictionary<int, ITsString> m_SavedBTNumberStrs = new Dictionary<int, ITsString>();
		/// <summary>Status values for the current set of back translations</summary>
		private Dictionary<int, string> m_BTStatus = new Dictionary<int, string>();
		/// <summary>Saved values for m_BTStatus while processing a subunit (footnote)</summary>
		private Dictionary<int, string> m_SavedBTStatus = new Dictionary<int, string>();
		/// <summary>Number of times the progress bar indicator has been stepped.</summary>
		private int m_cSteps = 0;
		/// <summary>We to create ITsString objects, so we need a factory.</summary>
		/// <summary>the XML reader used to read through the input file</summary>
		private XmlTextReader m_reader;
		/// <summary>description read from the OXES file</summary>
		private string m_sRevDescription = null;

		// the following strings are used as markers in the rgmapwstssBT list.
		private ITsString m_tssChapterStartKey;
		private ITsString m_tssVerseStartKey;
		private ITsString m_tssLabelTrKey;
		private ITsString m_tssNoteKey;
		private ITsString m_tssFigureKey;

		#endregion Variable, enumeration, and internal class declarations

		#region Constructor (private)
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor used with XML (OXES) based import.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="styleSheet"></param>
		/// <param name="sOXESFile">name of the XML (OXES) file</param>
		/// <param name="undoManager"></param>
		/// <param name="importCallbacks"></param>
		/// -----------------------------------------------------------------------------------
		protected TeXmlImporter(FdoCache cache, FwStyleSheet styleSheet, string sOXESFile,
			UndoImportManager undoManager, TeImportUi importCallbacks)
		{
			Debug.Assert(cache != null);
			Debug.Assert(styleSheet != null);
			Debug.Assert(sOXESFile != null && sOXESFile.Length > 0);

			m_cache = cache;
			m_styleSheet = styleSheet;
			m_sOXESFile = sOXESFile;

			// Allow tests to pass in a bogus filename.
			m_sOXESDir = (m_sOXESFile.Trim().Length > 0 ? Path.GetDirectoryName(m_sOXESFile) : null);
			m_undoManager = undoManager;
			m_importCallbacks = importCallbacks;
			m_importCallbacks.Importer = this;
			int cChapters = CountChapters();
			if (cChapters > 0)
				m_importCallbacks.Maximum = cChapters;

			m_tssChapterStartKey = StringUtils.MakeTss("chapterStart", m_cache.DefaultUserWs);
			m_tssVerseStartKey = StringUtils.MakeTss("verseStart", m_cache.DefaultUserWs);
			m_tssLabelTrKey = StringUtils.MakeTss("labelTr", m_cache.DefaultUserWs);
			m_tssNoteKey = StringUtils.MakeTss("note", m_cache.DefaultUserWs);
			m_tssFigureKey = StringUtils.MakeTss("figure", m_cache.DefaultUserWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Counts the chapters.
		/// </summary>
		/// <returns>The number of chapters to be imported</returns>
		/// ------------------------------------------------------------------------------------
		private int CountChapters()
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			int cChapters = 0;
			try
			{
				using (StreamReader rdr = new StreamReader(m_sOXESFile))
				{
					string sLine;
					while ((sLine = rdr.ReadLine()) != null)
					{
						if (sLine.IndexOf("<chapterStart") >= 0)
							++cChapters;
					}
				}
#if DEBUG
				TimeSpan span = new TimeSpan(DateTime.Now.Ticks - start.Ticks);
				string msg = String.Format("Time to count {0} chapters: {1} hours, {2} minutes, {3} seconds, {4} millseconds ({5} ticks).",
					cChapters, span.Hours, span.Minutes, span.Seconds, span.Milliseconds, span.Ticks);
				Debug.WriteLine(msg);
#endif
			}
			catch
			{
				cChapters = 0;
			}
			return cChapters;
		}
		#endregion Constructor (private)

		/// <summary>
		/// Make a better default description for the OXES import using the file name.
		/// </summary>
		protected override string DefaultSvDescription
		{
			get
			{
				// Although the two format strings are the same in English, they might differ in other
				// languages since one refers to a filename and the other to a descriptive phrase or sentence.
				if (String.IsNullOrEmpty(m_sRevDescription))
					return string.Format(TeResourceHelper.GetResourceString("kstidSavedVersionDescriptionImport"),
						Path.GetFileName(m_sOXESFile));
				else
					return string.Format(TeResourceHelper.GetResourceString("kstidOxesImportOf"),
						m_sRevDescription);
			}
		}

		#region IDisposable & Co. implementation
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Dispose method override.
		/// </summary>
		/// <param name="disposing"></param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (m_isDisposed)
				return;
			m_sOXESFile = null;
			m_ttpVerseNumber = null;
			m_ttpVerseNumberInNote = null;
			if (m_stackSectionType != null)
			{
				m_stackSectionType.Clear();
				m_stackSectionType = null;
			}
			if (m_stackXmlContext != null)
			{
				m_stackXmlContext.Clear();
				m_stackXmlContext = null;
			}
			if (m_BTNumberStrBldrs != null)
			{
				m_BTNumberStrBldrs.Clear();
				m_BTNumberStrBldrs = null;
			}
			if (m_ttpBTVerseNumbers != null)
			{
				m_ttpBTVerseNumbers.Clear();
				m_ttpBTVerseNumbers = null;
			}
			if (m_ttpBTVerseNumberInNotes != null)
			{
				m_ttpBTVerseNumberInNotes.Clear();
				m_ttpBTVerseNumberInNotes = null;
			}
			if (m_ttpBTChapterNumbers != null)
			{
				m_ttpBTChapterNumbers.Clear();
				m_ttpBTChapterNumbers = null;
			}
			base.Dispose(disposing);
		}
		#endregion IDisposable & Co. implementation

		#region Static method for importing XML (OXES) files
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Call this static method to import Scripture from an OXES (XML) file.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="styleSheet"></param>
		/// <param name="sOXESFile"></param>
		/// <param name="undoManager"></param>
		/// <param name="importUi"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public static object Import(FdoCache cache, FwStyleSheet styleSheet, string sOXESFile,
			UndoImportManager undoManager, TeImportUi importUi)
		{
#if DEBUG
			DateTime start = DateTime.Now;
#endif
			using (TeXmlImporter importer = new TeXmlImporter(cache, styleSheet, sOXESFile, undoManager,
				importUi))
			{
				importer.ImportXml();
#if DEBUG
				TimeSpan span = new TimeSpan(DateTime.Now.Ticks - start.Ticks);
				string msg = String.Format("Time to import {0}: {1} hours, {2} minutes, {3} seconds, {4} millseconds ({5} ticks).",
					sOXESFile, span.Hours, span.Minutes, span.Seconds, span.Milliseconds, span.Ticks);
				Debug.WriteLine(msg);
#endif
				return importer.m_firstImportedRef;
			}
		}
		#endregion Static method for importing XML (OXES) files

		#region Methods for XML (OXES) import

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Import the Scriptures from an OXES (Open XML for Editing Scriptures) file.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void ImportXml()
		{
			DateTime beg = DateTime.Now;
			// make sure we don't try to listen to all the changes we are making to the
			// view during import.
			try
			{
				StyleProxyListManager.Initialize(m_styleSheet);
				Initialize();
				m_fFoundABook = false;

				// Use XmlTextReader instead of XmlReader because we want access to
				// line number and position for error messages.
				using (m_reader = new XmlTextReader(m_sOXESFile))
				{
					try
					{
						m_reader.ReadStartElement("oxes");
						if (m_reader.IsStartElement("oxesText"))
						{
							if (!ProcessOxesTextAttributes(m_reader))
								return; // cannot continue due to improper OXES file.
							m_reader.ReadStartElement("oxesText");
							ProcessHeader(m_reader);
							ProcessTitlePage(m_reader);
							ProcessCanons(m_reader);
						}
					}
					catch (UnknownPalasoWsException e)
					{
						string wsFile = WritingSystemFile(e.WsIdentifier);
						string strDetailsTemplate =
							TeResourceHelper.GetResourceString("kstidUndefinedWritingSystemDetails");
						string strUknownWsDetails = Environment.NewLine + string.Format(
							strDetailsTemplate, e.IcuLocale, Path.GetDirectoryName(wsFile), Path.GetFileName(wsFile));

						UnknownPalasoWsRunException runEx = (e as UnknownPalasoWsRunException);
						string runText = null;
						int lineNumber = -1;
						SUE_ErrorCode errorCode = SUE_ErrorCode.UndefinedWritingSystem;
						if (runEx != null)
						{
							// if the exception is on a specific run of text, get the run of text and line number
							runText = runEx.RunText;
							lineNumber = m_reader.LineNumber;
							errorCode = SUE_ErrorCode.UndefinedWritingSystemInRun;
						}

						throw new ScriptureUtilsException(errorCode, strUknownWsDetails,
							m_sOXESFile, lineNumber, runText, m_currentRef);
					}
				}
				m_reader = null;
				// If no book was imported then tell the user that the books they specified didn't
				// import properly.
				if (!m_fFoundABook)
				{
					if (!MiscUtils.RunningTests) // If we're not running the tests
					{
						string message = string.Format(TeResourceHelper.GetResourceString(
							"kstidImportNoOxesBookError"),
							"GEN", "REV");
						m_importCallbacks.ErrorMessage(message);
					}
					else
					{
						throw new ArgumentException("Found no book to import during testing.");
					}
				}
			}
			finally
			{
				ScrNoteImportManager.Cleanup();
				StyleProxyListManager.Cleanup();
				m_importCallbacks.Position = m_importCallbacks.Maximum;
			}
			Debug.WriteLine("import time: " + (DateTime.Now - beg));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the global writing system file.
		/// </summary>
		/// <param name="wsIdentifier">The RFC identifier for the writing system.</param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		private string WritingSystemFile(string wsIdentifier)
		{
			return Path.Combine(DirectoryFinder.GlobalWritingSystemStoreDirectory,
				wsIdentifier + ".ldml");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process the contents of the &lt;header&gt; element.  At the moment, this skips over
		/// the contents without storing any information after the first &lt;revisionDesc&gt;.
		/// </summary>
		/// <param name="reader"></param>
		/// -----------------------------------------------------------------------------------
		private void ProcessHeader(XmlReader reader)
		{
			if (reader.IsStartElement("header"))
			{
				XmlNode xnHeader = ReadCurrentXmlNode(reader);
				foreach (XmlNode node in xnHeader.ChildNodes)
				{
					if (node.Name == "revisionDesc")
					{
						foreach (XmlNode xn in node.ChildNodes)
						{
							if (xn.Name == "para")
							{
								m_sRevDescription = xn.InnerText;
								return;
							}
						}
					}
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process the contents of the &lt;titlePage&gt; element.  At the moment, this skips
		/// over the contents without storing any information.
		/// </summary>
		/// <param name="reader"></param>
		/// -----------------------------------------------------------------------------------
		private void ProcessTitlePage(XmlReader reader)
		{
			if (reader.IsStartElement("titlePage"))
			{
				XmlNode xnTitlePage = ReadCurrentXmlNode(reader);
#pragma warning disable 219
				foreach (XmlNode xn in xnTitlePage.ChildNodes)
				{
					// TODO: do something with the title page stuff
				}
#pragma warning restore 219
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process the contents of the &lt;canon&gt; elements.  These consists of the actual
		/// scripture data, organized by books and sections.  Each canon contains books from
		/// a specific set: Old Testament ("ot"), Deutero-Canonical ("dt"), or New Testament
		/// ("nt").  TE doesn't handle Deutero-Canonical books.
		/// </summary>
		/// <param name="reader"></param>
		/// -----------------------------------------------------------------------------------
		private void ProcessCanons(XmlReader reader)
		{
			while (reader.IsStartElement("canon"))
			{
				Dictionary<string, string> attrs = ReadXmlAttributes(reader);
				bool fOk = false;
				string id;
				if (attrs.TryGetValue("ID", out id))
				{
					// REVIEW: What about handling deuteroCanonical stuff?
					if (id == "dc")
						MessageBox.Show(TeResourceHelper.GetResourceString("kstidNoDeuteroCanonical"),
							"", MessageBoxButtons.OK, MessageBoxIcon.Information);
					else if (id == "ot" || id == "nt")
						fOk = true;
				}
				if (!fOk)
				{
					reader.ReadToNextSibling("canon");
					continue;
				}
				reader.ReadStartElement();

				while (reader.IsStartElement("book"))
				{
					if (!StartXmlBook(reader))
					{
						// Skip the rest of this element.
						reader.ReadToNextSibling("book");
						continue;
					}
					m_fFoundABook = true;
					m_fInBookTitle = false;
					m_fCurrentSectionIsIntro = false;
					m_fInScriptureText = false;
					while (reader.IsStartElement())
					{
						CheckPause();
						if (reader.IsStartElement("titleGroup"))
						{
							m_fInBookTitle = true;
							ProcessBookTitle(reader);				// swallows </titleGroup>
							m_fInBookTitle = false;
						}
						else if (reader.IsStartElement("introduction"))
						{
							m_fCurrentSectionIsIntro = true;
							ProcessBookIntroduction(reader);			// swallows </introduction>
							m_fCurrentSectionIsIntro = false;
						}
						else if (reader.IsStartElement("section"))
						{
							m_fInScriptureText = true;
							while (reader.IsStartElement("section"))
							{
								ProcessBookSection(reader, true);	// swallows </section>
							}
							m_fInScriptureText = false;
						}
						else
						{
							throw new Exception("OXES validation failed to detect an unexpected tag in the <book> tag.");
						}
					}
					reader.ReadEndElement();	// should be </book>
				}
				reader.ReadEndElement();	// should be </canon>
			}
			if (m_fFoundABook)
				FinalizePrevSection();	// handle final section of final book
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set everything up for reading a book from the XML data.  The reader is pointing to
		/// the &lt;book&gt; element when this is called, and (if successful) pointing to the
		/// next (first child) element when this finishes.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns>true if successful, false if the book cannot be identified</returns>
		/// -----------------------------------------------------------------------------------
		private bool StartXmlBook(XmlReader reader)
		{
			FinalizePrevSection();
			StartingNewBook();
			m_scrBook = null;
			//m_currSection = null; // Done with the previous section
			//m_iCurrSection = -1;

			int iBookNum = 0;
			string sBookCode;
			Dictionary<string, string> attrs = ReadXmlAttributes(reader);
			if (attrs.TryGetValue("ID", out sBookCode))
			{
				m_scrBook = m_scr.FindBook(sBookCode);
				iBookNum = (m_scrBook == null ? BCVRef.BookToNumber(sBookCode) : m_scrBook.CanonicalNum);
			}

			if (iBookNum == 0)
				throw new Exception("OXES validation failed to find a valid ID tag for the book.");

			ScrNoteImportManager.Initialize(m_scr, iBookNum);
			m_nBookNumber = iBookNum;
			m_prevRef = new BCVRef(m_currentRef);
			m_currentRef = new BCVRef(m_nBookNumber, 1, 0);

			reader.ReadStartElement();
			PrepareToImportNewBook();
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process the &lt;titleGroup&gt; element that is a child of a &lt;book&gt; element.
		/// </summary>
		/// <param name="reader"></param>
		/// -----------------------------------------------------------------------------------
		private void ProcessBookTitle(XmlReader reader)
		{
			m_currSection = null;
			// set paragraph style property for new para
			m_ParaBldr.ParaStylePropsProxy = StyleProxyListManager.GetXmlParaStyleProxy(
				ScrStyleNames.MainBookTitle, ContextValues.Title, m_wsVern);
			StartBookTitle();
			XmlNode xnTitle = ReadCurrentXmlNode(reader);
			string sName = XmlUtils.GetAttributeValue(xnTitle, "short");
			if (!String.IsNullOrEmpty(sName))
				m_scrBook.Name.VernacularDefaultWritingSystem =
					StringUtils.MakeTss(sName, m_cache.DefaultVernWs);
			ProcessTitle(xnTitle, ScrStyleNames.MainBookTitle, m_Title);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process a &lt;title&gt; element, creating a paragraph in the right style at the
		/// right place.
		/// </summary>
		/// <param name="xnTitle"></param>
		/// <param name="sStyle">the default paragraph style for this title</param>
		/// <param name="title"></param>
		/// -----------------------------------------------------------------------------------
		private void ProcessTitle(XmlNode xnTitle, string sStyle, IStText title)
		{
			ClearParagraphStringBuilders();
			Dictionary<int, int> dictBTLengths = new Dictionary<int, int>();
			List<IScrScriptureNote> annotationsForPara = new List<IScrScriptureNote>();
			List<BTSegment> segmentBTsForPara = new List<BTSegment>();
			foreach (XmlNode node in xnTitle.ChildNodes)
			{
				if (node.Name == "title")
				{
					string sType = XmlUtils.GetAttributeValue(node, "type");
					int ichMin = m_ParaBldr.Length;
					foreach (int ws in m_BTStrBldrs.Keys)
						dictBTLengths[ws] = m_BTStrBldrs[ws].Length;
					ReadXmlParagraphData(node, false, null, annotationsForPara, segmentBTsForPara);
					if (sType == "secondary" || sType == "tertiary")
					{
						FixTitleRunStyle(sType, ichMin, m_ParaBldr.StringBuilder);
						foreach (int ws in m_BTStrBldrs.Keys)
						{
							int ich;
							if (!dictBTLengths.TryGetValue(ws, out ich))
								ich = 0;
							FixTitleRunStyle(sType, ich, m_BTStrBldrs[ws]);
						}
					}
					else
					{
						sStyle = GetParagraphStyleForElement(node);
					}
				}
			}
			StoreXmlParagraphData(title, sStyle, annotationsForPara, segmentBTsForPara);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set the character style for a secondary or tertiary title element.
		/// </summary>
		/// <param name="sType">type of book title (secondary, tertiary)</param>
		/// <param name="ichMin">starting character offset</param>
		/// <param name="tsbTitle">ITsStrBldr containing text of title</param>
		/// -----------------------------------------------------------------------------------
		private void FixTitleRunStyle(string sType, int ichMin, ITsStrBldr tsbTitle)
		{
			int ichLim = tsbTitle.Length;
			if (ichMin < ichLim)
			{
				ITsTextProps ttp = StyleUtils.CharStyleTextProps(sType == "secondary" ?
					ScrStyleNames.SecondaryBookTitle : ScrStyleNames.TertiaryBookTitle, m_wsVern);
				// Don't overwrite the properties for any embedded footnotes.
				string sT = tsbTitle.Text;
				int ich = sT.IndexOf(StringUtils.kChObject, ichMin);
				while (ich > 0)
				{
					if (ichMin < ich)
						tsbTitle.SetProperties(ichMin, ich, ttp);
					ichMin = ich + 1;
					ich = sT.IndexOf(StringUtils.kChObject, ichMin);
				}
				if (ichMin < ichLim)
					tsbTitle.SetProperties(ichMin, ichLim, ttp);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store the information for one segment of a segmented back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal class BTSegment
		{
			private Dictionary<int, ITsString> m_mapWsTss = new Dictionary<int, ITsString>();
			private List<IScrScriptureNote> m_rgAnns = new List<IScrScriptureNote>();

			internal BTSegment(int ichBegin, int ichEnd, ITsString tssData)
			{
				BeginOffset = ichBegin;
				EndOffset = ichEnd;
				Text = tssData;
			}

			internal int BeginOffset
			{
				get; set;
			}

			internal int EndOffset
			{
				get; set;
			}

			internal ITsString Text
			{
				get; set;
			}

			internal void SetBackTransForWs(int ws, ITsString tss)
			{
				ITsString tss1;
				if (m_mapWsTss.TryGetValue(ws, out tss1))
					m_mapWsTss[ws] = tss;
				else
					m_mapWsTss.Add(ws, tss);
			}

			internal ITsString GetBackTransForWs(int ws)
			{
				ITsString tss;
				if (m_mapWsTss.TryGetValue(ws, out tss))
					return tss;
				return null;
			}

			internal List<int> AvailableBackTranslations
			{
				get
				{
					List<int> rgws = new List<int>(m_mapWsTss.Keys.Count);
					foreach (int ws in m_mapWsTss.Keys)
					{
						if (ws != 0)
							rgws.Add(ws);
					}
					return rgws;
				}
			}

			internal List<IScrScriptureNote> Annotations
			{
				get { return m_rgAnns; }
				set
				{
					m_rgAnns.Clear();
					m_rgAnns.AddRange(value);
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Read in one paragraph's worth of data.  This includes both the translated data and
		/// the back translated data, as well as any footnotes or annotations encountered along
		/// the way.  The footnotes and annotations are created, but the translated data and
		/// back translated data are left waiting for a call to StoreXmlParagraphData().
		/// </summary>
		/// <param name="xnPara">XML node with paragraph information</param>
		/// <param name="fNew"><c>true</c> to start a new paragraph; <c>false</c> to continue
		/// adding content to an existing paragraph</param>
		/// <param name="sect">The section of which this paragraph is a part</param>
		/// <param name="annotationsForPara">List of any annotations added or modified for this
		/// paragraph (needed so that the begin/end object properties can be set once the para
		/// is actually created)</param>
		/// <param name="segmentBTsForPara"></param>
		/// <returns>true if any data was read, false otherwise</returns>
		/// -----------------------------------------------------------------------------------
		private bool ReadXmlParagraphData(XmlNode xnPara, bool fNew, IScrSection sect,
			List<IScrScriptureNote> annotationsForPara, List<BTSegment> segmentBTsForPara)
		{
			if (fNew)
				ClearParagraphStringBuilders();
			bool fData = false;
			Dictionary<int, ITsString> mapwstss = new Dictionary<int, ITsString>();
			List<IScrScriptureNote> rgnotesForSegment = new List<IScrScriptureNote>();
			bool fImportSegmentedBT = true;
			foreach (XmlNode node in xnPara.ChildNodes)
			{
				int ichBeginTR = NormalizedLength(m_ParaBldr.StringBuilder);
				mapwstss.Clear();
				if (node.Name == "trGroup")
				{
					foreach (XmlNode xn in node.ChildNodes)
					{
						string sLang = XmlUtils.GetOptionalAttributeValue(xn, "xml:lang", null);
						int ws;
						if (String.IsNullOrEmpty(sLang))
							ws = xn.Name == "bt" ? m_wsAnal : m_wsVern;
						else
							ws = GetWsForLang(sLang);
						if (xn.Name == "tr")
						{
							ReadXmlStringElement(xn, ws, null, m_ParaBldr.StringBuilder);
							fData = true;
						}
						else if (xn.Name == "bt")
						{
							fImportSegmentedBT &= XmlUtils.GetOptionalBooleanAttributeValue(xn, "segmented", false);
							ITsStrBldr tsb = GetBackTransStringBuilder(ws);
							ITsStrBldr tsbNumber = GetBackTransNumberStringBuilder(ws);
							if (tsbNumber.Length > 0)
							{
								tsb.ReplaceTsString(tsb.Length, tsb.Length, tsbNumber.GetString());
								tsbNumber.Clear();
							}
							int cchBegin = NormalizedLength(tsb);
							ReadXmlStringElement(xn, ws, null, tsb);
							string status = XmlUtils.GetOptionalAttributeValue(xn, "status");
							string statusT;
							if (!String.IsNullOrEmpty(status) && !m_BTStatus.TryGetValue(ws, out statusT))
								m_BTStatus.Add(ws, status);
							fData = true;
							mapwstss.Add(ws, NormalizedSegment(tsb, cchBegin));
							foreach (XmlNode xnChild in xn.ChildNodes)
							{
								if (xnChild.Name == "annotation")
								{
									IScrScriptureNote ann =
										XmlScrNote.Deserialize(xnChild, m_scr, m_styleSheet);

									if (ann != null)
										rgnotesForSegment.Add(ann);
								}
							}
						}
						else if (xn is XmlElement)
						{
							throw new Exception("OXES validation failed to detect an unexpected tag in the <trGroup>.");
						}
					}
					int ichEndTR = NormalizedLength(m_ParaBldr.StringBuilder);
					if (ichEndTR > ichBeginTR || rgnotesForSegment.Count > 0)
					{
						BTSegment bts = new BTSegment(ichBeginTR, ichEndTR,
							NormalizedSegment(m_ParaBldr.StringBuilder, ichBeginTR));
						foreach (int ws in mapwstss.Keys)
							bts.SetBackTransForWs(ws, mapwstss[ws]);
						if (rgnotesForSegment.Count > 0)
						{
							bts.Annotations = rgnotesForSegment;
							rgnotesForSegment.Clear();
						}
						segmentBTsForPara.Add(bts);
					}
				}
				else if (node.Name == "chapterStart" || node.Name == "chapterEnd")
				{
					ProcessChapterNumber(node);
					if (m_ParaBldr.Length > 0)
						fData = true;
					AddOrAppendToSegIfNeeded(ichBeginTR, segmentBTsForPara, m_tssChapterStartKey, m_tssChapterStartKey);
				}
				else if (node.Name == "verseStart" || node.Name == "verseEnd")
				{
					ProcessVerseNumber(node);
					if (m_ParaBldr.Length > 0)
						fData = true;
					AddOrAppendToSegIfNeeded(ichBeginTR, segmentBTsForPara, m_tssVerseStartKey, m_tssVerseStartKey);
				}
				else if (node.Name == "note")
				{
					if (XmlUtils.GetOptionalAttributeValue(node, "Id") != null)
					{
						ProcessFootnote(node);
						int ichEndTR = NormalizedLength(m_ParaBldr.StringBuilder);
						if (ichEndTR > ichBeginTR)
						{
							BTSegment bts = new BTSegment(ichBeginTR, ichEndTR,
								NormalizedSegment(m_ParaBldr.StringBuilder, ichBeginTR));
							bts.SetBackTransForWs(0, m_tssNoteKey);
							segmentBTsForPara.Add(bts);
						}
					}
					else
					{
						Debug.Fail("Need to handle note references.");
						//Debug.Assert(XmlUtils.GetOptionalAttributeValue(node, "Ref") != null,
						//    "Did not find expected reference to a footnote");
						//ProcessFootnoteRef(node);
					}
				}
				else if (node.Name == "annotation")
				{
					IScrScriptureNote ann = XmlScrNote.Deserialize(node, m_scr, m_styleSheet);
					if (ann != null)
						annotationsForPara.Add(ann);
				}
				else if (node.Name == "figure")
				{
					ProcessFigure(node);
					int ichEndTR = NormalizedLength(m_ParaBldr.StringBuilder);
					if (ichEndTR > ichBeginTR)
					{
						BTSegment bts = new BTSegment(ichBeginTR, ichEndTR,
							NormalizedSegment(m_ParaBldr.StringBuilder, ichBeginTR));
						bts.SetBackTransForWs(0, m_tssFigureKey);
						segmentBTsForPara.Add(bts);
					}
				}
				else if (node.Name == "p")
				{
					if (m_stackXmlContext.Count == 0)
						throw new Exception("OXES validation failed to detect an unexpected paragraph element");
					string sContext = m_stackXmlContext.Peek();
					if ((sContext == ScrStyleNames.ListItem1 || sContext == ScrStyleNames.ListItem2) &&
						sect != null)
					{
						if (fData)
							StoreXmlParagraphData(sect.ContentOA, sContext, annotationsForPara, segmentBTsForPara);
						string sStyle = (sContext == ScrStyleNames.ListItem1) ?
							ScrStyleNames.ListItem1Additional : ScrStyleNames.ListItem2Additional;
						if (ReadXmlParagraphData(node, true, sect, annotationsForPara, segmentBTsForPara))
							StoreXmlParagraphData(sect.ContentOA, sStyle, annotationsForPara, segmentBTsForPara);
						fData = false;
					}
					else if (node is XmlElement)
					{
						throw new Exception("OXES validation failed to detect a <p> tag within a <p> tag.");
					}
				}
				else if (node.Name == "labelTr")
				{
					ReadXmlStringElement(node, m_wsVern, null, m_ParaBldr.StringBuilder);
					fData = true;
					AddOrAppendToSegIfNeeded(ichBeginTR, segmentBTsForPara, m_tssLabelTrKey, m_tssLabelTrKey);
				}
				else if (node is XmlElement)
				{
					throw new Exception("OXES validation failed to find a valid tag within a <p> tag.");
				}
			}
			if (!fImportSegmentedBT)
				segmentBTsForPara.Clear();
			return fData;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Adds text to a new segment or append text to an existing segment if needed.
		/// </summary>
		/// <param name="ichBeginTR">The beginning character offset of the segment.</param>
		/// <param name="segmentBTsForPara">List of segments for the para.</param>
		/// <param name="tssAppendKey">The TSS append key.</param>
		/// <param name="tssAddKey">The TSS add key.</param>
		/// --------------------------------------------------------------------------------
		private void AddOrAppendToSegIfNeeded(int ichBeginTR, List<BTSegment> segmentBTsForPara,
			ITsString tssAppendKey, ITsString tssAddKey)
		{
			int ichEndTR = NormalizedLength(m_ParaBldr.StringBuilder);
			if (ichEndTR <= ichBeginTR)
				return; // no paragraph contents to add or append

			int iseg = segmentBTsForPara.Count - 1;
			if (iseg >= 0 &&
				segmentBTsForPara[iseg] != null &&
				(segmentBTsForPara[iseg].GetBackTransForWs(0) == m_tssChapterStartKey ||
				segmentBTsForPara[iseg].GetBackTransForWs(0) == m_tssLabelTrKey ||
				segmentBTsForPara[iseg].GetBackTransForWs(0) == m_tssVerseStartKey) &&
				segmentBTsForPara[iseg].Text != null)
			{
				// Found a previous segment to which we need to append
				BTSegment bts = segmentBTsForPara[iseg];
				bts.EndOffset = ichEndTR;
				ITsStrBldr tsb = bts.Text.GetBldr();
				tsb.Append(NormalizedSegment(m_ParaBldr.StringBuilder, ichBeginTR));
				bts.Text = tsb.GetString();
				bts.SetBackTransForWs(0, tssAppendKey);
			}
			else
			{
				// Need to create a new segment
				BTSegment bts = new BTSegment(ichBeginTR, ichEndTR,
					NormalizedSegment(m_ParaBldr.StringBuilder, ichBeginTR));
				bts.SetBackTransForWs(0, tssAddKey);
				segmentBTsForPara.Add(bts);
			}
		}

		private ITsString NormalizedSegment(ITsStrBldr tsb, int ichBegin)
		{
			if (tsb == null)
				return null;
			if (tsb.Length == 0)
				return tsb.GetString();
			ITsStrBldr tsbT = tsb.GetString().get_NormalizedForm(FwNormalizationMode.knmNFD).GetBldr();
			if (ichBegin > tsbT.Length)
				ichBegin = tsbT.Length;
			tsbT.ReplaceTsString(0, ichBegin, null);
			return tsbT.GetString();
		}

		private int NormalizedLength(ITsStrBldr tsb)
		{
			if (tsb == null || tsb.Length == 0)
				return 0;
			return tsb.Text.Normalize(NormalizationForm.FormD).Length;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process a &lt;figure&gt; element, which encapsulates a picture.
		/// </summary>
		/// <param name="node"></param>
		/// -----------------------------------------------------------------------------------
		private void ProcessFigure(XmlNode node)
		{
			SaveParagraphStringBuilders();
			m_ParaBldr = new StTxtParaBldr(m_cache);
			m_ParaBldr.ParaStylePropsProxy = StyleProxyListManager.GetXmlParaStyleProxy(
				ScrStyleNames.Label, ContextValues.Internal, m_wsVern);

			// TODO (TE-7756): Support OXES export and import of new properties that have been
			// added to the CmPicture model

			string src = XmlUtils.GetAttributeValue(node, "src");
			string oxesRef = XmlUtils.GetAttributeValue(node, "oxesRef");
			string alt = XmlUtils.GetAttributeValue(node, "alt");
			List<IScrScriptureNote> annotationsForPara = new List<IScrScriptureNote>();
			List<BTSegment> segmentBTsForPara = new List<BTSegment>();
			foreach (XmlNode xn in node.ChildNodes)
				ReadXmlParagraphData(xn, false, null, annotationsForPara, segmentBTsForPara);
			Guid pictureGuid = StoreFigureData(src, oxesRef, alt, annotationsForPara, segmentBTsForPara);
			RestoreParagraphStringBuilders();
			ITsPropsBldr tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsVern);
			string sGuid = MiscUtils.GetObjDataFromGuid(pictureGuid);
			string sObj = String.Format("{0}{1}", (char)FwObjDataTypes.kodtGuidMoveableObjDisp, sGuid);
			tpb.SetStrPropValue((int)FwTextPropType.ktptObjData, sObj);
			m_ParaBldr.AppendRun(StringUtils.kChObject.ToString(), tpb.GetTextProps());
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create the CmPicture object for the given data.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="oxesRef"></param>
		/// <param name="alt"></param>
		/// <returns>the guid of the CmPicture object, to use in the object reference property</returns>
		/// <param name="annotationsForPara">List of any annotations added or modified for this
		/// figure</param>
		/// <param name="segmentBTsForPara"></param>
		/// -----------------------------------------------------------------------------------
		private Guid StoreFigureData(string src, string oxesRef, string alt,
			List<IScrScriptureNote> annotationsForPara, List<BTSegment> segmentBTsForPara)
		{
			// TODO (TE-7539): Set Object ref for any annotations in annotationsForPara
			// TODO: figure out a way to use an existing CmPicture if one appears to be pointing
			// to the same file.
			// TODO: Figure out how to use segmented back translation information?

			// First try to find the picture in a standard subdirectory next to the OXES file.
			// If that fails, try to link to a matching picture file already stored in our
			// internal subdirectory.  If these both fail, then the picture will show as an
			// empty box.
			string sPath = Path.Combine(m_sOXESDir,
				String.Format("pictures{0}{1}", Path.DirectorySeparatorChar, src));
			ICmPicture pict;
			if (File.Exists(sPath))
			{
				pict = m_cache.ServiceLocator.GetInstance<ICmPictureFactory>().
					Create(sPath, m_ParaBldr.StringBuilder.GetString(), EditingHelper.DefaultPictureFolder);
			}
			else
			{
				if (Path.IsPathRooted(src))
				{
					sPath = src;
				}
				else
				{
					sPath = String.Format("Pictures{0}{1}", Path.DirectorySeparatorChar, src);
					sPath = Path.Combine(m_cache.LangProject.LinkedFilesRootDir, sPath);
				}
				pict = m_cache.ServiceLocator.GetInstance<ICmPictureFactory>().
					Create(sPath, m_ParaBldr.StringBuilder.GetString(), EditingHelper.DefaultPictureFolder);
				sPath = String.Format("Pictures{0}{1}", Path.DirectorySeparatorChar, src);
				if (File.Exists(Path.Combine(m_cache.LangProject.LinkedFilesRootDir, sPath)))
					pict.PictureFileRA.InternalPath = sPath;
			}
			foreach (int ws in m_BTStrBldrs.Keys)
				pict.Caption.set_String(ws, m_BTStrBldrs[ws].GetString());
			return pict.Guid;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process a published note, which must be a footnote of some sort.
		/// </summary>
		/// <param name="node"></param>
		/// -----------------------------------------------------------------------------------
		private void ProcessFootnote(XmlNode node)
		{
			Debug.Assert(!m_fInFootnote);
			// remember that we are now processing a footnote
			m_fInFootnote = true;

			string sType = XmlUtils.GetOptionalAttributeValue(node, "type");
			string sParaStyle;
			switch (sType)
			{
				default:
				case "general":
					sParaStyle = ScrStyleNames.NormalFootnoteParagraph;
					break;
				case "crossReference":
					sParaStyle = ScrStyleNames.CrossRefFootnoteParagraph;
					break;
			}
			// When importing a whole book, we can call this version of InsertFootnoteAt and save
			// the time of trying to look for the previous footnote each time and checking to see
			// if we need to resequence following footnotes.
			// TODO (TE-920 and/or TE-431): [copied from TeImport.cs]
			m_CurrFootnote = m_scrBook.InsertFootnoteAt(m_iCurrFootnote, m_ParaBldr.StringBuilder,
				m_ParaBldr.Length);
			m_footnoteMapping.Add(XmlUtils.GetAttributeValue(node, "noteID"), m_CurrFootnote.Guid);
			//// Set the footnote marker into any appropriate back translations as well.
			//string sBTMarked = XmlUtils.GetOptionalAttributeValue(node, "markerExistsInBT");
			//int[] rgwsBTMarked = GetMarkedBTWs(sBTMarked);
			//foreach (int ws in rgwsBTMarked)
			//{
			//    ITsStrBldr tsb = GetBackTransStringBuilder(ws);
			//    // First empty out any chapter or verse numbers in the back translations?
			//    ITsStrBldr tsbNumber;
			//    if (m_BTNumberStrBldrs.TryGetValue(ws, out tsbNumber) && tsbNumber.Length > 0)
			//    {
			//        tsb.ReplaceTsString(tsb.Length, tsb.Length, tsbNumber.GetString());
			//        m_BTNumberStrBldrs[ws].Clear();
			//    }
			//    m_CurrFootnote.InsertOwningORCIntoPara(tsb, tsb.Length, ws);
			//}
			// Set up the paragraph builder for the footnote, first saving the existing paragraph builders.
			SaveParagraphStringBuilders();
			m_ParaBldr = new StTxtParaBldr(m_cache);
			m_ParaBldr.ParaStylePropsProxy = StyleProxyListManager.GetXmlParaStyleProxy(
				sParaStyle, ContextValues.Note, m_wsVern);

			List<IScrScriptureNote> annotationsForPara = new List<IScrScriptureNote>();
			List<BTSegment> segmentBTsForPara = new List<BTSegment>();
			ReadXmlParagraphData(node, true, null, annotationsForPara, segmentBTsForPara);
			// Create the paragraph even if the contents are empty; otherwise user will have no
			// way to enter contents later.
			StoreXmlParagraphData(m_CurrFootnote, sParaStyle, annotationsForPara, segmentBTsForPara);

			m_iCurrFootnote++; // increment for next footnote
			RestoreParagraphStringBuilders();
			m_CurrFootnote = null;
			m_fInFootnote = false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process a reference to a footnote which should be contained in a back translation.
		/// </summary>
		/// <param name="node">The note node.</param>
		/// <param name="ws">The writing system for which the footnote should be added.</param>
		/// -----------------------------------------------------------------------------------
		private void ProcessFootnoteRef(XmlNode node, int ws)
		{
			string footnoteId = XmlUtils.GetAttributeValue(node, "noteRef");
			if (!m_footnoteMapping.ContainsKey(footnoteId))
			{
				Debug.Fail("Footnote id not found.");
				return;
			}
			ITsStrBldr tsb = GetBackTransStringBuilder(ws);
			// First empty out any chapter or verse numbers in this back translation
			ITsStrBldr tsbNumber;
			if (m_BTNumberStrBldrs.TryGetValue(ws, out tsbNumber) &&
				tsbNumber != null && tsbNumber.Length > 0)
			{
				tsb.ReplaceTsString(tsb.Length, tsb.Length, tsbNumber.GetString());
				m_BTNumberStrBldrs[ws].Clear();
			}
			// Get footnote
			IScrFootnote footnote;
			if (m_cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().TryGetObject(
				m_footnoteMapping[footnoteId], out footnote))
			{
				// Insert the caller into the back translation for this footnote.
				footnote.InsertRefORCIntoTrans(tsb, tsb.Length, ws);
			}
		}

		///// -----------------------------------------------------------------------------------
		///// <summary>
		///// Convert a comma delimited list of language codes into an array of ws hvos.
		///// </summary>
		///// <param name="sBTMarked"></param>
		///// <returns></returns>
		///// -----------------------------------------------------------------------------------
		//private int[] GetMarkedBTWs(string sBTMarked)
		//{
		//    if (String.IsNullOrEmpty(sBTMarked))
		//        return new int[0];
		//    string[] rgsBTMarked = sBTMarked.Split(new char[] { ',' });
		//    Set<int> setWs = new Set<int>();
		//    for (int i = 0; i < rgsBTMarked.Length; ++i)
		//    {
		//        string sLang = rgsBTMarked[i];
		//        if (!String.IsNullOrEmpty(sLang))
		//        {
		//            int ws = GetWsForLang(sLang);
		//            if (ws > 0)
		//                setWs.Add(ws);
		//        }
		//    }
		//    return setWs.ToArray();
		//}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clear all the string builders for the paragraph data in preparation for reading in
		/// a new paragraph.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void ClearParagraphStringBuilders()
		{
			m_ParaBldr.StringBuilder.Clear();
			m_BTStrBldrs.Clear();
			m_BTNumberStrBldrs.Clear();
			m_BTStatus.Clear();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Save the variable values for restoring later.  Only one level is implemented,
		/// it's not a stack!
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void SaveParagraphStringBuilders()
		{
			m_SavedParaBldr = m_ParaBldr;
			m_SavedBTStrs.Clear();
			foreach (int ws in m_BTStrBldrs.Keys)
				m_SavedBTStrs.Add(ws, GetString(m_BTStrBldrs[ws], ws));

			m_BTStrBldrs.Clear();
			m_SavedBTNumberStrs.Clear();
			foreach (int ws in m_BTNumberStrBldrs.Keys)
				m_SavedBTNumberStrs.Add(ws, GetString(m_BTNumberStrBldrs[ws], ws));

			m_BTNumberStrBldrs.Clear();
			m_SavedBTStatus.Clear();
			foreach (int ws in m_BTStatus.Keys)
				m_SavedBTStatus.Add(ws, m_BTStatus[ws]);
			m_BTStatus.Clear();
		}

		private ITsString GetString(ITsStrBldr bldr, int ws)
		{
			if (bldr.Length > 0)
				return bldr.GetString();
			return StringUtils.MakeTss("", ws);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Restore the saved variable values.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void RestoreParagraphStringBuilders()
		{
			m_ParaBldr = m_SavedParaBldr;
			m_BTStrBldrs.Clear();
			m_BTNumberStrBldrs.Clear();
			m_BTStatus.Clear();
			foreach (int ws in m_SavedBTStrs.Keys)
				m_BTStrBldrs.Add(ws, m_SavedBTStrs[ws].GetBldr());
			foreach (int ws in m_SavedBTNumberStrs.Keys)
				m_BTNumberStrBldrs.Add(ws, m_SavedBTNumberStrs[ws].GetBldr());
			foreach (int ws in m_SavedBTStatus.Keys)
				m_BTStatus.Add(ws, m_SavedBTStatus[ws]);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find or create the string builder for the back translation in the given writing
		/// system.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		private ITsStrBldr GetBackTransStringBuilder(int ws)
		{
			ITsStrBldr tsb;
			if (!m_BTStrBldrs.TryGetValue(ws, out tsb))
			{
				tsb = TsStrBldrClass.Create();
				tsb.Replace(0, 0, string.Empty, StyleUtils.CharStyleTextProps(null, ws));
				m_BTStrBldrs.Add(ws, tsb);
			}
			return tsb;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find or create the string builder for the back translation in the given writing
		/// system.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		private ITsStrBldr GetBackTransNumberStringBuilder(int ws)
		{
			ITsStrBldr tsb;
			if (!m_BTNumberStrBldrs.TryGetValue(ws, out tsb))
			{
				tsb = TsStrBldrClass.Create();
				m_BTNumberStrBldrs.Add(ws, tsb);
			}
			return tsb;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find or create the text properties for the Chapter Number style for the back
		/// translation in the given writing system.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		private ITsTextProps GetBackTransChapterNumberProps(int ws)
		{
			ITsTextProps ttp;
			if (!m_ttpBTChapterNumbers.TryGetValue(ws, out ttp))
			{
				ttp = StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, ws);
				m_ttpBTChapterNumbers.Add(ws, ttp);
			}
			return ttp;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find or create the text properties for the Verse Number style for the back
		/// translation in the given writing system.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		private ITsTextProps GetBackTransVerseNumberProps(int ws)
		{
			ITsTextProps ttp;
			if (m_fInFootnote)
			{
				if (!m_ttpBTVerseNumberInNotes.TryGetValue(ws, out ttp))
				{
					ttp = StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumberInNote, ws);
					m_ttpBTVerseNumberInNotes.Add(ws, ttp);
				}
			}
			else
			{
				if (!m_ttpBTVerseNumbers.TryGetValue(ws, out ttp))
				{
					ttp = StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, ws);
					m_ttpBTVerseNumbers.Add(ws, ttp);
				}
			}
			return ttp;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process a &lt;chapter&gt; element, storing the chapter number in the text for the
		/// start marker.
		/// </summary>
		/// <param name="node"></param>
		/// -----------------------------------------------------------------------------------
		private void ProcessChapterNumber(XmlNode node)
		{
			if (node.Name == "chapterStart")
			{
				if (m_cSteps % m_importCallbacks.Maximum == 0)
					m_importCallbacks.Position = 0;
				m_importCallbacks.Step(0);
				++m_cSteps;
			}
			// Chapter Number			<chapterStart ID="MRK.10" n="10"/>...<chapterEnd eID="MRK.10"/>
			// Chapter Number Alternate	<chapterStart ID="PSA.10" aID="PSA.9b" n="10"/>
			string sID = XmlUtils.GetOptionalAttributeValue(node, "ID");
			string aID = XmlUtils.GetOptionalAttributeValue(node, "aID");
			string sChap = XmlUtils.GetOptionalAttributeValue(node, "n");
			if (!String.IsNullOrEmpty(sID) && !String.IsNullOrEmpty(sChap) && String.IsNullOrEmpty(aID))
			{
				string sTmp;
				int nChapter = StringUtils.strtol(sChap, out sTmp);
				m_currentRef.Chapter = nChapter;
				m_currentRef.Verse = 1;

				if (m_ttpChapterNumber == null)
					m_ttpChapterNumber = StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, m_wsVern);
				m_ParaBldr.AppendRun(sChap, m_ttpChapterNumber);
				// Insert chapter number into back translation number string builders.
				foreach (int ws in GetBTWritingSystems(node.ParentNode))
				{
					ITsTextProps ttp = GetBackTransChapterNumberProps(ws);
					ITsStrBldr tsb = GetBackTransNumberStringBuilder(ws);
					tsb.Replace(0, tsb.Length, m_currentRef.Chapter.ToString(), ttp);
				}
			}
			// TODO: handle Chapter Number Alternate!
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Scan the XML node for bt elements, and return all the writing system codes used
		/// for those elements (in the xml:lang attribute).
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		private Set<int> GetBTWritingSystems(XmlNode node)
		{
			Set<int> wsBT = new Set<int>();
			wsBT.Add(m_wsAnal);
			if (node != null)
			{
				foreach (XmlNode xn in node.ChildNodes)
				{
					if (xn.Name == "bt")
					{
						AddBTWritingSystem(wsBT, xn);
					}
					else if (xn.Name == "trGroup")
					{
						foreach (XmlNode xn1 in xn.ChildNodes)
							AddBTWritingSystem(wsBT, xn1);
					}
				}
			}
			return wsBT;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Add the writing system of the back translation node to the given set.
		/// </summary>
		/// <param name="wsBT">Collection of writing systems IDs used for back translations</param>
		/// <param name="xn"></param>
		/// -----------------------------------------------------------------------------------
		private void AddBTWritingSystem(Set<int> wsBT, XmlNode xn)
		{
			string sLang = XmlUtils.GetOptionalAttributeValue(xn, "xml:lang");
			if (!String.IsNullOrEmpty(sLang))
			{
				int ws = GetWsForLang(sLang);
				wsBT.Add(ws);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process a &lt;verse&gt; element, storing the verse number in the text for the start
		/// marker.
		/// </summary>
		/// <param name="node"></param>
		/// -----------------------------------------------------------------------------------
		private void ProcessVerseNumber(XmlNode node)
		{
			// Verse Number				<verse ID="MRK.6.5-6a" n="5-6a"/>...<verse eID="MRK.6.5-6a"/>
			// Verse Number Alternate	<verse ID="PSA.14.1" aID="PSA.14.2" n="1"/>
			// Verse Number In Note		context="note"			<verse ID="MRK.12.2.note" n="2">
			string sID = XmlUtils.GetOptionalAttributeValue(node, "ID");
			string aID = XmlUtils.GetOptionalAttributeValue(node, "aID");
			string sVerse = XmlUtils.GetOptionalAttributeValue(node, "n");
			if (!String.IsNullOrEmpty(sID) && !String.IsNullOrEmpty(sVerse) && String.IsNullOrEmpty(aID))
			{
				string sTmp;
				m_currentRef.Verse = StringUtils.strtol(sVerse, out sTmp);
				string sVerseBT;
				int endVerse = m_currentRef.Verse;
				while (!String.IsNullOrEmpty(sTmp))
				{
					sTmp = sTmp.Substring(1);		// skip the dash character(s)
					if (String.IsNullOrEmpty(sTmp))
						break;
					string sTmp2;
					int x = StringUtils.strtol(sTmp, out sTmp2);
					if (String.IsNullOrEmpty(sTmp2))
					{
						if (x > endVerse)
							endVerse = x;
						break;
					}
				}
				if (endVerse > m_currentRef.Verse)
					sVerseBT = String.Format("{0}-{1}", m_currentRef.Verse, endVerse);
				else
					sVerseBT = m_currentRef.Verse.ToString();
				if (m_ttpVerseNumber == null)
					m_ttpVerseNumber = StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, m_wsVern);
				if (m_ttpVerseNumberInNote == null)
					m_ttpVerseNumberInNote = StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumberInNote, m_wsVern);
				ITsTextProps ttpVerse = m_fInFootnote ? m_ttpVerseNumberInNote : m_ttpVerseNumber;
				int cch = m_ParaBldr.Length;
				if (cch > 0)
				{
					// ensure that verse numbers are separated by at least a space.
					ITsTextProps ttp = m_ParaBldr.StringBuilder.get_PropertiesAt(cch);
					if (ttp == m_ttpVerseNumber || ttp == m_ttpVerseNumber)
						m_ParaBldr.AppendRun(" ", StyleUtils.CharStyleTextProps(null, m_wsVern));
				}
				m_ParaBldr.AppendRun(sVerse, ttpVerse);
				// Insert verse number into back translation number string builders.
				foreach (int ws in GetBTWritingSystems(node.ParentNode))
				{
					ITsTextProps ttp = GetBackTransVerseNumberProps(ws);
					ITsStrBldr tsb = GetBackTransNumberStringBuilder(ws);
					cch = tsb.Length;
					int ichMin = cch;
					int crun = tsb.RunCount;
					if (crun > 1)
					{
						// Must have both a chapter number and a verse number.  Lose both. (?)
						int ich0 = tsb.get_RunAt(0);
						int ich1 = tsb.get_RunAt(1);
						Debug.WriteLine(String.Format("Run 0 => {0}; Run 1 => {1}", ich0, ich1));
						ichMin = 0;
					}
					else if (crun == 1)
					{
						// If a verse number already exists in the "empty" string builder,
						// start over.
						ITsTextProps ttpOld = tsb.get_Properties(0);
						if (ttpOld == ttp)
							ichMin = 0;
					}
					tsb.Replace(ichMin, cch, sVerseBT, ttp);
				}
			}
			// TODO: handle Verse Number Alternate!
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Store the data that has accumulated in m_ParaBldr and any back translation string
		/// builders.
		/// </summary>
		/// <param name="text">The owning StText</param>
		/// <param name="sStyle">The name of the paragraph style</param>
		/// <param name="annotationsForPara">List of any annotations added or modified for this
		/// paragraph</param>
		/// <param name="segmentBTsForPara"></param>
		/// -----------------------------------------------------------------------------------
		private void StoreXmlParagraphData(IStText text, string sStyle,
			List<IScrScriptureNote> annotationsForPara, List<BTSegment> segmentBTsForPara)
		{
			m_ParaBldr.ParaStylePropsProxy = StyleProxyListManager.GetXmlParaStyleProxy(
				sStyle, ContextValues.General, m_wsVern);
			// Need to create empty string marked with vernacular WS if the paragraph is empty - most
			// likely to occur in title or section heading.
			if (m_ParaBldr.Length == 0)
				m_ParaBldr.StringBuilder.SetIntPropValues(0, 0, (int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, m_wsVern);
			IStTxtPara para = m_ParaBldr.CreateParagraph(text);
			if (segmentBTsForPara != null && segmentBTsForPara.Count > 0)
				ProcessSegmentInformation(para, segmentBTsForPara);
			ICmTranslation trans = null;
			foreach (int ws in m_BTStrBldrs.Keys)
			{
				ITsStrBldr tsb = m_BTStrBldrs[ws];
				if (tsb.Length > 0)
				{
					if (trans == null)
						trans = para.GetOrCreateBT();
					trans.Translation.set_String(ws, tsb.GetString());
				}
			}
			foreach (int ws in m_BTStatus.Keys)
			{
				string status = m_BTStatus[ws];
				if (!String.IsNullOrEmpty(status))
				{
					if (trans == null)
						trans = para.GetOrCreateBT();
					// handle possible (probable even) upper/lowercase shifting of status values.  See TE-7136.
					string statusLow = status.ToLowerInvariant();
					if (statusLow == BackTranslationStatus.Checked.ToString().ToLowerInvariant())
						status = BackTranslationStatus.Checked.ToString();
					else if (statusLow == BackTranslationStatus.Finished.ToString().ToLowerInvariant())
						status = BackTranslationStatus.Finished.ToString();
					else if (statusLow == BackTranslationStatus.Unfinished.ToString().ToLowerInvariant())
						status = BackTranslationStatus.Unfinished.ToString();
					trans.Status.set_String(ws, StringUtils.MakeTss(status, ws));
				}
			}

			// TODO (TE-7538): Annotation should be linked to the BT if quote references the
			// BT rather than the vernacular. Also (not sure if this should be done here or
			// elsewhere), the offset should be set based on the quoted text.
			if (annotationsForPara != null)
			{
				foreach (IScrScriptureNote ann in annotationsForPara)
				{
					ann.BeginObjectRA = para;
					ann.EndObjectRA = para;
				}
				annotationsForPara.Clear();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Check whether this paragraph has the data for a segmented back translation, and if
		/// it does, create it.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="segmentBTsForPara"></param>
		/// <returns>true if a segmented back translation is created, false otherwise</returns>
		/// -----------------------------------------------------------------------------------
		private bool ProcessSegmentInformation(IStTxtPara para, List<BTSegment> segmentBTsForPara)
		{
			ITsString tssPara = para.Contents.get_NormalizedForm(FwNormalizationMode.knmNFD);
			IFdoOwningSequence<ISegment> segs = para.SegmentsOS;
			bool fMatchingSegs = false;
			if (segs.Count == segmentBTsForPara.Count)
			{
				fMatchingSegs = true;
				for (int i = 0; i < segs.Count; ++i)
				{
					if (segs[i].BeginOffset != segmentBTsForPara[i].BeginOffset ||
						segs[i].EndOffset != segmentBTsForPara[i].EndOffset ||
						!segs[i].BaselineText.Equals(segmentBTsForPara[i].Text))
					{
						fMatchingSegs = false;
						break;
					}
				}
			}
			if (fMatchingSegs)
			{
				CreateSegmentedBackTranslation(para, segmentBTsForPara);
				return true;
			}
			else
			{
				Debug.Fail("OXES segments don't match back translation.");
				return false;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a segmented back translation for this paragraph.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="segmentBTsForPara"></param>
		/// -----------------------------------------------------------------------------------
		private void CreateSegmentedBackTranslation(IStTxtPara para, List<BTSegment> segmentBTsForPara)
		{
			int[] paraSegs;
			int wsBt = m_cache.DefaultAnalWs;	// this only affects caching...
			Debug.Assert(segmentBTsForPara.Count == para.SegmentsOS.Count);
			for (int i = 0; i < segmentBTsForPara.Count; ++i)
			{
				BTSegment bts = segmentBTsForPara[i];
				ISegment paraSeg = para.SegmentsOS[i];
				Debug.Assert(paraSeg.BeginOffset == bts.BeginOffset);
				foreach (int ws in bts.AvailableBackTranslations)
					paraSeg.FreeTranslation.set_String(ws, bts.GetBackTransForWs(ws));
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Append the text contents of the given XmlNode to the string builder, setting
		/// styles appropriately as internal elements are encountered.  At this point, nested
		/// styles do not create a combination style, but just temporarily replace the outer
		/// style.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="ws"></param>
		/// <param name="sStyle"></param>
		/// <param name="tsb"></param>
		/// -----------------------------------------------------------------------------------
		private void ReadXmlStringElement(XmlNode node, int ws, string sStyle, ITsStrBldr tsb)
		{
			ITsTextProps ttp = StyleUtils.CharStyleTextProps(sStyle, ws);
			if (node.HasChildNodes)
			{
				foreach (XmlNode xn in node.ChildNodes)
				{
					if (xn is XmlText || xn is XmlWhitespace)
					{
						string textToAdd = MiscUtils.CleanupXmlString(xn.InnerText);
						tsb.Replace(tsb.Length, tsb.Length, textToAdd, ttp);
					}
					else if (xn.Name == "annotation")
					{
						// handled by caller.
					}
					else if (xn.Name == "foreign")
					{
						// foreign element is used to both mark a WS run that is not the default vernacular
						// and the foreign style. If a run has both foreign style and a WS, nested elements
						// are exported - top element with no WS and inner with the WS.
						string sLang = XmlUtils.GetOptionalAttributeValue(xn, "xml:lang");
						if (!String.IsNullOrEmpty(sLang))
						{
							int wsNew = GetWsForLang(sLang);
							ReadXmlStringElement(xn, wsNew, sStyle, tsb);	// recurse!
						}
						else
						{
							string sStyleNew = GetCharacterStyleForElement(xn);
							ReadXmlStringElement(xn, ws, sStyleNew, tsb);	// recurse so we don't lose data.
						}
					}
					else if (xn.Name == "note")
					{
						if (XmlUtils.GetOptionalAttributeValue(xn, "noteID") != null)
							ProcessFootnote(xn); // footnote in vernacular
						else
						{
							Debug.Assert(XmlUtils.GetOptionalAttributeValue(xn, "noteRef") != null);
							ProcessFootnoteRef(xn, ws); // footnote in back translation
						}
					}
					else if (xn.Name == "figure")
					{
						ProcessFigure(xn);
					}
					else if (xn is XmlElement)
					{
						string sStyleNew = GetCharacterStyleForElement(xn);
						ReadXmlStringElement(xn, ws, sStyleNew, tsb);	// recurse!
					}
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find the character style implied by the current XML node.
		/// </summary>
		/// <param name="node"></param>
		/// <returns>character style name, or null if nothing fits</returns>
		/// -----------------------------------------------------------------------------------
		private string GetCharacterStyleForElement(XmlNode node)
		{
			OxesInfo xinfo = OxesInfo.GetOxesInfoForCharNode(node);
			CreateCustomStyleIfNeeded(xinfo, false);
			return xinfo.StyleName;		// may well be null!
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the given style if it doesn't already exist in the style sheet.
		/// </summary>
		/// <param name="xinfo">The OXES info.</param>
		/// <param name="fParaStyle">if set to <c>true</c> [f para style].</param>
		/// ------------------------------------------------------------------------------------
		private void CreateCustomStyleIfNeeded(OxesInfo xinfo, bool fParaStyle)
		{
			string sCustom = xinfo.StyleName;
			if (String.IsNullOrEmpty(sCustom))
				return;
			IStStyle sty = m_styleSheet.FindStyle(sCustom);
			if (sty == null)
			{
				Debug.WriteLine(String.Format("Creating {0} style \"{1}\"",
					fParaStyle ? "paragraph" : "character", sCustom));
				int hvoNewStyle = m_styleSheet.MakeNewStyle();
				IStStyle newStyle =
					m_cache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(hvoNewStyle);
				newStyle.Name = sCustom;
				if (fParaStyle)
				{
					newStyle.Type = StyleType.kstParagraph;
					switch (xinfo.Context)
					{
						case OxesContext.IntroSection:
							newStyle.BasedOnRA = m_styleSheet.FindStyle(ScrStyleNames.IntroSectionHead);
							break;
						case OxesContext.Introduction:
							newStyle.BasedOnRA = m_styleSheet.FindStyle(ScrStyleNames.IntroParagraph);
							break;
						case OxesContext.NormalSection:
							newStyle.BasedOnRA = m_styleSheet.FindStyle(ScrStyleNames.SectionHead);
							break;
						default:
							newStyle.BasedOnRA = m_styleSheet.FindStyle(ScrStyleNames.NormalParagraph);
							break;
					}
				}
				else
				{
					newStyle.Type = StyleType.kstCharacter;
				}
				string sUsage = String.Format(TeResourceHelper.GetResourceString("kstidStyleFromImport"), m_sOXESFile);
				int basedOn = newStyle.BasedOnRA != null ? newStyle.BasedOnRA.Hvo : 0;
				m_styleSheet.PutStyle(sCustom, sUsage, newStyle.Hvo, basedOn, fParaStyle ? newStyle.Hvo : 0,
					(int)newStyle.Type, false, false, null);
				sty = m_styleSheet.FindStyle(sCustom);
				Debug.Assert(sty != null);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find the paragraph style implied by the given XML node in the current environment.
		/// </summary>
		/// <param name="node"></param>
		/// <returns>paragraph style name, or null if nothing fits</returns>
		/// -----------------------------------------------------------------------------------
		private string GetParagraphStyleForElement(XmlNode node)
		{
			OxesInfo xinfo = OxesInfo.GetOxesInfoForParaNode(node,
				m_fCurrentSectionIsIntro, CurrentSectionType(), CurrentXmlContext());
			CreateCustomStyleIfNeeded(xinfo, true);
			return xinfo.StyleName;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return the top of the section type stack, or null if the stack is empty.
		/// </summary>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		private string CurrentSectionType()
		{
			return m_stackSectionType.Count > 0 ? m_stackSectionType.Peek() : null;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return the top of the XML element context stack, or null if the stack is empty.
		/// </summary>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		private string CurrentXmlContext()
		{
			return m_stackXmlContext.Count > 0 ? m_stackXmlContext.Peek() : null;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find or create the writing system code for the given RFC-5646 language tag or ICU
		/// locale. If it's not in either the list of vernacular writing systems or the list of
		/// analysis writing systems, add it to the list of analysis writing systems.
		/// </summary>
		/// <param name="sLang"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		private int GetWsForLang(string sLang)
		{
			IWritingSystem ws;
			WritingSystemServices.FindOrCreateWritingSystem(m_cache, LangTagUtils.ToLangTag(sLang),
				true, false, out ws);
			return ws.Handle;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Read the current element into an XmlNode object.  (This assumes that we're at a
		/// small enough chunk of the file to warrant using a DOM approach instead of sticking
		/// to the sequential reader.)
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		internal static XmlNode ReadCurrentXmlNode(XmlReader reader)
		{
			string sXml = reader.ReadOuterXml();
			XmlDocument document = new XmlDocument();
			document.PreserveWhitespace = true;
			document.LoadXml(sXml);
			return document.FirstChild;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process the &lt;introduction&gt; element that is a child of a &lt;book&gt; element.
		/// </summary>
		/// <param name="reader"></param>
		/// -----------------------------------------------------------------------------------
		private void ProcessBookIntroduction(XmlReader reader)
		{
			reader.ReadStartElement("introduction");
			while (reader.IsStartElement())
			{
				if (reader.IsStartElement("section"))
				{
					ProcessBookSection(reader, true);
				}
				else
				{
					Debug.Assert(reader.IsStartElement("section"));
					XmlNode node = ReadCurrentXmlNode(reader);
				}
			}
			reader.ReadEndElement();	// </introduction>
			FinalizePrevSection();
			m_currSection = null; // clear out current section so it is not used in Scripture processing.
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process a section of a book.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="fCreateNew"></param>
		/// -----------------------------------------------------------------------------------
		private void ProcessBookSection(XmlReader reader, bool fCreateNew)
		{
			Dictionary<string, string> attrsSection = ReadXmlAttributes(reader);
			string sType;
			if (!attrsSection.TryGetValue("type", out sType))
				sType = "";
			string sScope;
			if (!attrsSection.TryGetValue("scope", out sScope))
				sScope = "";
			m_stackSectionType.Push(sType);
			reader.ReadStartElement();
			if (fCreateNew)
				MakeSection();
			while (reader.IsStartElement())
			{
				if (reader.IsStartElement("section"))
				{
					// If no paragraphs of content have been read, then assume we'll
					// just have another heading for a lower level section.  But if we
					// have content, then start a new section (recursively).
					ProcessBookSection(reader, m_currSection.ContentOA.ParagraphsOS.Count > 0);
				}
				else
				{
					XmlNode xnPara = ReadCurrentXmlNode(reader);
					ProcessParagraphNode(xnPara, m_currSection);
				}
			}
			m_stackSectionType.Pop();
			reader.ReadEndElement();	// </section>
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process an element inside a &lt;section&gt; element, which is usually a paragraph
		/// of some sort.
		/// </summary>
		/// <param name="xnPara"></param>
		/// <param name="sect"></param>
		/// -----------------------------------------------------------------------------------
		private void ProcessParagraphNode(XmlNode xnPara, IScrSection sect)
		{
			if (m_importCallbacks.CancelImport)
				throw new CancelException("Import canceled by user.");

			if (xnPara is XmlWhitespace)
				return;
			Debug.Assert(xnPara is XmlElement);
			string sNodeName = xnPara.Name;
			if (sNodeName == "table" || sNodeName == "speech" || sNodeName == "embedded")
			{
				// These elements serve only as containers to give context to their contents.
				m_stackXmlContext.Push(sNodeName);
				foreach (XmlNode node in xnPara.ChildNodes)
					ProcessParagraphNode(node, sect);
				m_stackXmlContext.Pop();
			}
			else
			{
				string sStyle = GetParagraphStyleForElement(xnPara);
				// List Item1 Additional		text/body		<item level="1">...<p>
				// List Item2 Additional		text/body		<item level="2">...<p>
				// Parallel Passage Reference	text/heading	<title type="parallelPassage"><reference>(Mateos 19:13-15; Lukas 18:15-17)</reference>
				if (sNodeName == "reference" && sStyle == ScrStyleNames.ParallelPassageReference)
				{
					m_stackXmlContext.Push(sStyle);
					List<IScrScriptureNote> annotationsForPara = new List<IScrScriptureNote>();
					List<BTSegment> segmentBTsForPara = new List<BTSegment>();
					ReadXmlParagraphData(xnPara, true, null, annotationsForPara, segmentBTsForPara);
					StoreParagraphData(sect, sNodeName, sStyle, annotationsForPara, segmentBTsForPara);
					m_stackXmlContext.Pop();
				}
				else if (sStyle == ScrStyleNames.ListItem1 || sStyle == ScrStyleNames.ListItem2)
				{
					m_stackXmlContext.Push(sStyle);
					List<IScrScriptureNote> annotationsForPara = new List<IScrScriptureNote>();
					List<BTSegment> segmentBTsForPara = new List<BTSegment>();
					if (ReadXmlParagraphData(xnPara, true, sect, annotationsForPara, segmentBTsForPara))
						StoreXmlParagraphData(sect.ContentOA, sStyle, annotationsForPara, segmentBTsForPara);
					m_stackXmlContext.Pop();
				}
				else if (sStyle == ScrStyleNames.TableRow)
				{
					// Store an empty "Table Row" "paragraph", then recurse and store the embedded
					// header and cell "paragraphs".
					ClearParagraphStringBuilders();
					StoreParagraphData(sect, sNodeName, sStyle, null, null);
					m_stackXmlContext.Push(sNodeName);
					foreach (XmlNode node in xnPara.ChildNodes)
						ProcessParagraphNode(node, sect);
					m_stackXmlContext.Pop();
				}
				else
				{
					List<IScrScriptureNote> annotationsForPara = new List<IScrScriptureNote>();
					List<BTSegment> segmentBTsForPara = new List<BTSegment>();
					ReadXmlParagraphData(xnPara, true, null, annotationsForPara, segmentBTsForPara);
					StoreParagraphData(sect, sNodeName, sStyle, annotationsForPara, segmentBTsForPara);
				}
			}
			m_footnoteMapping.Clear();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Store the accumlated paragraph data in the right place, either the Heading or the
		/// Contents of the ScrSection object.
		/// </summary>
		/// <param name="sect"></param>
		/// <param name="sNodeName"></param>
		/// <param name="sStyle">The name of the paragraph style</param>
		/// <param name="annotationsForPara">List of any annotations added or modified for this
		/// paragraph</param>
		/// <param name="segmentBTsForPara"></param>
		/// -----------------------------------------------------------------------------------
		private void StoreParagraphData(IScrSection sect, string sNodeName, string sStyle,
			List<IScrScriptureNote> annotationsForPara, List<BTSegment> segmentBTsForPara)
		{
			bool fHeading = sect.ContentOA.ParagraphsOS.Count == 0 &&
							(sNodeName == "sectionHead" || sNodeName == "chapterHead" ||
							 CurrentXmlContext() == ScrStyleNames.ParallelPassageReference ||
							 (CurrentXmlContext() == "speech" && sStyle == ScrStyleNames.SpeechSpeaker));
			if (fHeading)
				StoreXmlParagraphData(sect.HeadingOA, sStyle, annotationsForPara, segmentBTsForPara);
			else
				StoreXmlParagraphData(sect.ContentOA, sStyle, annotationsForPara, segmentBTsForPara);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Read the attributes of the current element in the XmlReader into a Dictionary for
		/// easier access.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		internal static Dictionary<string, string> ReadXmlAttributes(XmlReader reader)
		{
			Dictionary<string, string> attrs = new Dictionary<string, string>();
			if (reader.HasAttributes)
			{
				while (reader.MoveToNextAttribute())
					attrs.Add(reader.Name, reader.Value);
				reader.MoveToElement();
			}
			return attrs;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Process the attributes on the &lt;oxesWork&gt; element.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		private bool ProcessOxesTextAttributes(XmlReader reader)
		{
			//Dictionary<string, string> attrs = ReadXmlAttributes(reader);
			//string sValue;
			//if (attrs.TryGetValue("xml:lang", out sValue))
			//{
			//     This check is currently done in the dialog when selecting a file.
			//    // Verify that the vernacular language matches the OXES file.
			//    ILgWritingSystem lgws = LgWritingSystem.CreateFromDBObject(m_cache, m_wsVern);
			//    if (sValue != lgws.RFC4646bis)
			//    {
			//        string sMsg = String.Format("The project's vernacular language ({0}) does not match the OXES file's data language ({1}).  Should import continue?",
			//            lgws.RFC4646bis, sValue);
			//        return MessageBox.Show(sMsg, "Import Language Mismatch", MessageBoxButtons.YesNo) == DialogResult.Yes;
			//    }
			//}
			//if (attrs.TryGetValue("type", out sValue))
			//{
			//}
			//if (attrs.TryGetValue("oxesIDWork", out sValue))
			//{
			//}
			//if (attrs.TryGetValue("canonical", out sValue))
			//{
			//}
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Perform some basic initialization.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void Initialize()
		{
			m_wsAnal = m_cache.DefaultAnalWs;
			m_wsVern = m_cache.DefaultVernWs;
			m_scr = m_cache.LangProject.TranslatedScriptureOA;

			m_ScrSectionHeadParaProxy = new ImportStyleProxy(ScrStyleNames.SectionHead,
				StyleType.kstParagraph, m_wsVern, ContextValues.Text, m_styleSheet);
			m_DefaultIntroSectionHeadParaProxy = new ImportStyleProxy(ScrStyleNames.IntroSectionHead,
				StyleType.kstParagraph, m_wsVern, ContextValues.Intro, m_styleSheet);
			m_DefaultScrParaProxy = new ImportStyleProxy(ScrStyleNames.NormalParagraph,
				StyleType.kstParagraph, m_wsVern, ContextValues.Text, m_styleSheet);
			m_DefaultIntroParaProxy = new ImportStyleProxy(ScrStyleNames.IntroParagraph,
				StyleType.kstParagraph, m_wsVern, ContextValues.Intro, m_styleSheet);

			// Make a paragraph builder. We will keep re-using this every time we build a paragraph.
			m_ParaBldr = new StTxtParaBldr(m_cache);
		}
		#endregion Methods for XML (OXES) import
	}
}
