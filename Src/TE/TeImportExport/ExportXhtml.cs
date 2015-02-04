// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ExportXhtml.cs
// Responsibility:
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml.Xsl;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using System.Diagnostics;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.FwUtils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides export of data to an XHTML format file (together with a corresponding CSS file)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ExportXhtml
	{
		/// <summary>the pathname of the output XML file</summary>
		private string m_fileName;
		/// <summary>where we get the data from</summary>
		private FdoCache m_cache;
		/// <summary>stores the list of books to export</summary>
		private FilteredScrBooks m_bookFilter;
		/// <summary>what to export: everything, filtered list of books, or a single book</summary>
		private ExportWhat m_what;
		/// <summary>if single book, number of the book to export; otherwise meaningless</summary>
		private int m_nBookSingle;
		/// <summary>if single book, index of the first section to export; otherwise meaningless</summary>
		private int m_iFirstSection;
		/// <summary>if single book, index of the last section to export; otherwise meaningless</summary>
		private int m_iLastSection;
		/// <summary>description of this work edited by user</summary>
		private string m_sDescription;
		/// <summary>the style sheet used for editing Scripture</summary>
		private FwStyleSheet m_styleSheet;
		/// <summary>current Publication</summary>
		private IPublication m_pub;
		private IApp m_app;
		/// <summary>the basic Scripture object</summary>
		private IScripture m_scr;
		/// <summary>object that provides common services for XHTML export</summary>
		XhtmlHelper m_xhtml;
		/// <summary>file output object</summary>
		TextWriter m_writer;
		/// <summary>map from writing system hvo to its RFC4646bis code</summary>
		private Dictionary<int, string> m_mapWsToRFC = new Dictionary<int, string>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a new instance of the <see cref="ExportXhtml"/> class.
		/// </summary>
		/// <param name="fileName">pathname of the XHTML file to create</param>
		/// <param name="cache">data source</param>
		/// <param name="filter">lists the books to export</param>
		/// <param name="what">tells what to export: everything, filtered list, or single book</param>
		/// <param name="nBook">if single book, number of the book to export</param>
		/// <param name="iFirstSection">if single book, index of first section to export</param>
		/// <param name="iLastSection">if single book, index of last section to export</param>
		/// <param name="sDescription">The s description.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="pub">The pub.</param>
		/// <param name="app">The application</param>
		/// ------------------------------------------------------------------------------------
		public ExportXhtml(string fileName, FdoCache cache, FilteredScrBooks filter,
			ExportWhat what, int nBook, int iFirstSection, int iLastSection, string sDescription,
			FwStyleSheet styleSheet, IPublication pub, IApp app)
		{
			m_fileName = fileName;
			m_cache = cache;
			m_bookFilter = filter;
			m_what = what;
			m_nBookSingle = nBook;
			m_iFirstSection = iFirstSection;
			m_iLastSection = iLastSection;
			m_sDescription = sDescription;
			m_styleSheet = styleSheet;
			m_pub = pub;
			m_app = app;

			m_scr = cache.LangProject.TranslatedScriptureOA;
		}

		#region Public Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Run the export
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Run(Form dialogOwner)
		{
			// Check whether we're about to overwrite an existing file.
			if (FileUtils.IsFileReadable(m_fileName))
			{
				string sFmt = DlgResources.ResourceString("kstidAlreadyExists");
				string sMsg = String.Format(sFmt, m_fileName);
				string sCaption = DlgResources.ResourceString("kstidExportXHTML");
				if (MessageBoxUtils.Show(dialogOwner, sMsg, sCaption, MessageBoxButtons.YesNo,
					MessageBoxIcon.Warning) == DialogResult.No)
				{
					return;
				}
			}
			try
			{
				try
				{
					m_writer = FileUtils.OpenFileForWrite(m_fileName, Encoding.UTF8);
					m_xhtml = new XhtmlHelper(m_writer, m_cache);
				}
				catch (Exception e)
				{
					MessageBoxUtils.Show(dialogOwner, e.Message, m_app.ApplicationName,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
				ExportTE(dialogOwner);
			}
			catch (Exception e)
			{
				Exception inner = e.InnerException ?? e;
				if (inner is IOException)
				{
					MessageBoxUtils.Show(dialogOwner, inner.Message, m_app.ApplicationName,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
				else
					throw inner;
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

		private void ExportTE(Form dialogOwner)
		{
			m_xhtml.WriteXhtmlHeading(m_fileName, m_sDescription, "scrBody");
			ExportScripture(dialogOwner);
			m_xhtml.WriteXhtmlEnding();
			m_writer.Close();

			string sXsltFile = Path.Combine(FwDirectoryFinder.TeFolder, "XhtmlExport.xsl");
			string sTempFile1 = m_fileName + "-1";
			if (FileUtils.IsFileReadable(sTempFile1))
				FileUtils.Delete(sTempFile1);
			FileUtils.Move(m_fileName, sTempFile1);
			XslCompiledTransform xsl = new XslCompiledTransform();
			xsl.Load(sXsltFile);
			xsl.Transform(sTempFile1, m_fileName);
			FileUtils.Delete(sTempFile1);

			string sTempFile2 = m_fileName + "-2";
			if (FileUtils.IsFileReadable(sTempFile2))
				FileUtils.Delete(sTempFile2);
			FileUtils.Move(m_fileName, sTempFile2);
			m_xhtml.FinalizeXhtml(m_fileName, sTempFile2);
			FileUtils.Delete(sTempFile2);
			m_xhtml.WriteCssFile(Path.ChangeExtension(m_fileName, ".css"), m_styleSheet,
				XhtmlHelper.CssType.Scripture, m_pub);
		}
		#endregion

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

				progressDlg.RunTask(ExportScripture);
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
			switch (m_what)
			{
				case ExportWhat.AllBooks:
					// Export all of the Scripture books in the project.
					for (int i = 0; i < m_scr.ScriptureBooksOS.Count && !progressDlg.Canceled; ++i)
						ExportBook(m_scr.ScriptureBooksOS[i], progressDlg);
					break;
				case ExportWhat.FilteredBooks:
					// Export all of the Scripture books in the filter
					for (int bookIndex = 0; bookIndex < m_bookFilter.BookCount && !progressDlg.Canceled; bookIndex++)
						ExportBook(m_bookFilter.GetBook(bookIndex), progressDlg);
					break;
				case ExportWhat.SingleBook:
					// Export a single book.
					ExportBook(m_scr.FindBook(m_nBookSingle), progressDlg);
					break;
			}
			return null;
		}

		private void ExportBook(IScrBook book, IThreadedProgress progressDlg)
		{
			m_writer.WriteLine("<div class=\"scrBook\">");
			m_xhtml.MapCssToLang("scrBook", LanguageCode(m_cache.DefaultVernWs));
			ExportBookTitle(book);
			int iFirst = 0;
			int iLim = book.SectionsOS.Count;
			if (m_what == ExportWhat.SingleBook)
			{
				iFirst = m_iFirstSection;
				iLim = m_iLastSection + 1;
			}
			bool fColumnOutput = false;
			for (int i = iFirst; i < iLim; ++i)
			{
				if (!book.SectionsOS[i].IsIntro && !fColumnOutput)
				{
					m_writer.WriteLine("<div class=\"columns\">");
					m_xhtml.MapCssToLang("columns", LanguageCode(m_cache.DefaultVernWs));
					fColumnOutput = true;
				}
				ExportBookSection(book.SectionsOS[i]);
				if (progressDlg.Canceled)
					break;
				progressDlg.Step(0);
			}
			if (fColumnOutput)
				m_writer.WriteLine("</div>");	// matches <div class="columns">
			m_writer.WriteLine("</div>");		// matches <div class="scrBook">
		}

		private void ExportBookTitle(IScrBook book)
		{
			ITsString tssName = book.Name.VernacularDefaultWritingSystem;
			string sLang = LanguageCode(m_cache.DefaultVernWs);
			m_writer.WriteLine("<span class=\"scrBookName\" lang=\"{0}\">{1}</span>",
				XmlUtils.MakeSafeXmlAttribute(sLang), XmlUtils.MakeSafeXml(tssName.Text));
			m_writer.WriteLine("<span class=\"scrBookCode\" lang=\"{0}\">{1}</span>",
				XmlUtils.MakeSafeXmlAttribute(sLang), XmlUtils.MakeSafeXml(book.BookId));
			foreach (IStTxtPara para in book.TitleOA.ParagraphsOS)
				ExportParagraph(para, "BookTitle");
		}

		private void ExportBookSection(IScrSection section)
		{
			if (section.IsIntro)
			{
				m_writer.WriteLine("<div class=\"scrIntroSection\">");
				m_xhtml.MapCssToLang("scrIntroSection", LanguageCode(m_cache.DefaultVernWs));
			}
			else
			{
				m_writer.WriteLine("<div class=\"scrSection\">");
				m_xhtml.MapCssToLang("scrSection", LanguageCode(m_cache.DefaultVernWs));
			}
			foreach (IStTxtPara para in section.HeadingOA.ParagraphsOS)
				ExportParagraph(para, "SectionHeading");
			foreach (IStTxtPara para in section.ContentOA.ParagraphsOS)
				ExportParagraph(para, "Paragraph");
			m_writer.WriteLine("</div>");
		}

		private void ExportParagraph(IStTxtPara para, string sDefaultStyle)
		{
			string sStyle = para.StyleName;
			if (String.IsNullOrEmpty(sStyle))
				sStyle = sDefaultStyle;
			if (!String.IsNullOrEmpty(sStyle))
				sStyle = m_xhtml.GetValidCssClassName(sStyle);
			ITsString tssPara = para.Contents;
			m_writer.WriteLine("<div class=\"{0}\">", sStyle);
			m_xhtml.MapCssToLang(sStyle, LanguageCode(TsStringUtils.GetWsAtOffset(tssPara, 0)));
			// TODO/REVIEW:  export pictures exactly in place?
			ExportPictures(tssPara);
			WriteTsStringAsXml(tssPara, 4);
			m_writer.WriteLine("</div>");
		}

		/// <summary>
		/// Write the ITsString out.
		/// </summary>
		/// <param name="tssPara"></param>
		/// <param name="cchIndent"></param>
		/// <remarks>TODO: this maybe should go into a generic method somewhere?
		/// Except that part of it is specific to XHTML output...</remarks>
		private void WriteTsStringAsXml(ITsString tssPara, int cchIndent)
		{
			// First, build the indentation.
			string sIndent = string.Empty.PadLeft(cchIndent, ' ');
			StringBuilder bldr = new StringBuilder(sIndent);
			bldr.Append("  ");
			string sIndent2 = bldr.ToString();

			m_writer.WriteLine("{0}<Str>", sIndent);
			int crun = tssPara.RunCount;
			string sField = null;
			for (int irun = 0; irun < crun; ++irun)
			{
				TsRunInfo tri;
				ITsTextProps ttpRun = tssPara.FetchRunInfo(irun, out tri);
				int ctip = ttpRun.IntPropCount;
				int ctsp = ttpRun.StrPropCount;
				string sFieldRun = ttpRun.GetStrPropValue((int)FwTextPropType.ktptFieldName);
				if (sFieldRun != sField)
				{
					if (!String.IsNullOrEmpty(sField))
						m_writer.WriteLine("{0}</Field>", sIndent2);
					if (!String.IsNullOrEmpty(sFieldRun))
						m_writer.WriteLine("{0}<Field name=\"{1}\">", sIndent2, sFieldRun);
					sField = sFieldRun;
					sFieldRun = null;
				}
				bool fMarkItem;
				int tpt;
				string sVal;
				bool fSkipText = false;
				int nVar;
				int nVal = ttpRun.GetIntPropValues((int)FwTextPropType.ktptMarkItem, out nVar);
				if (nVal == (int)FwTextToggleVal.kttvForceOn && nVar == (int)FwTextPropVar.ktpvEnum)
				{
					m_writer.Write("{0}<Item><Run", sIndent2);
					fMarkItem = true;
				}
				else
				{
					m_writer.Write("{0}<Run", sIndent2);
					fMarkItem = false;
				}
				for (int itip = 0; itip < ctip; ++itip)
				{
					nVal = ttpRun.GetIntProp(itip, out tpt, out nVar);
					if (tpt == (int)FwTextPropType.ktptWs || tpt == (int)FwTextPropType.ktptBaseWs)
					{
						if (nVal != 0)
						{
							WritingSystem wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(nVal);
							m_writer.Write(" {0}=\"{1}\"",
								tpt == (int)FwTextPropType.ktptWs ? "ws" : "wsBase", wsObj.Id);
						}
					}
					else if (tpt != (int)FwTextPropType.ktptMarkItem)
					{
						WriteIntTextProp(m_writer, m_cache.LanguageWritingSystemFactoryAccessor, tpt, nVar, nVal);
					}
				}
				string sRun = GetRunString(tssPara.get_RunText(irun));
				Guid guidFootnote = Guid.Empty;
				for (int itsp = 0; itsp < ctsp; ++itsp)
				{
					sVal = ttpRun.GetStrProp(itsp, out tpt);
					Debug.Assert(tpt != (int)FwTextPropType.ktptBulNumFontInfo);
					Debug.Assert(tpt != (int)FwTextPropType.ktptWsStyle);
					WriteStrTextProp(m_writer, tpt, sVal);
					if (sRun != null && sRun.Length == 1 && sRun[0] == StringUtils.kChObject)
					{
						Debug.Assert(tpt == (int)FwTextPropType.ktptObjData);
						fSkipText = true;
						if ((int)sVal[0] == (int)FwObjDataTypes.kodtOwnNameGuidHot)
							guidFootnote = MiscUtils.GetGuidFromObjData(sVal.Substring(1));
					}
				}
				m_writer.Write(">");
				if (!fSkipText)
				{
					int cch = tri.ichLim - tri.ichMin;
					if (cch > 0)
					{
						sRun = sRun.Normalize();
						m_writer.Write(XmlUtils.MakeSafeXml(sRun));
					}
				}
				if (fMarkItem)
					m_writer.WriteLine("</Run></Item>");
				else
					m_writer.WriteLine("</Run>");
				if (guidFootnote != Guid.Empty)
				{
					IScrFootnote footnote;
					if (m_cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().TryGetFootnote(guidFootnote, out footnote))
						ExportFootnote(footnote);
				}
			}
			if (!String.IsNullOrEmpty(sField))
				m_writer.WriteLine("{0}</Field>", sIndent2);
			m_writer.WriteLine("{0}</Str>", sIndent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text from a run without any hard-line breaks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string GetRunString(string runText)
		{
			StringBuilder sRunBldr = new StringBuilder(runText);
			while (sRunBldr.ToString().Contains(StringUtils.kChHardLB.ToString()))
			{
				string text = sRunBldr.ToString();
				int iHardLB = text.IndexOf(StringUtils.kChHardLB);
				if (iHardLB != -1)
				{
					if ((iHardLB > 0 && !Char.IsWhiteSpace(text[iHardLB - 1])) &&
						(iHardLB < text.Length - 1 && !char.IsWhiteSpace(text[iHardLB + 1])))
					{
						// The hard-line break is not at the start or end of a text run and does
						// not already have whitespace before or after it so we want to add a space
						// when we remove the hard-line break.
						sRunBldr.Replace(StringUtils.kChHardLB, ' ', iHardLB, 1);
					}
					else
						sRunBldr.Remove(iHardLB, 1);
				}
			}

			return sRunBldr.ToString();
		}

		/// <summary>
		/// Write the integer text property as an XML attribute.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="wsf"></param>
		/// <param name="tpt"></param>
		/// <param name="nVar"></param>
		/// <param name="nVal"></param>
		public void WriteIntTextProp(TextWriter writer, ILgWritingSystemFactory wsf,
			int tpt, int nVar, int nVal)
		{
			Debug.Assert(writer != null);
			Debug.Assert(wsf != null);

			switch (tpt)
			{
				case (int)FwTextPropType.ktptBackColor:
					writer.Write(" backcolor=\"{0}\"", ColorName(nVal));
					break;
				case (int)FwTextPropType.ktptBold:
					writer.Write(" bold=\"{0}\"", ToggleValueName((byte)nVal));
					break;
				case (int)FwTextPropType.ktptWs:
					if (nVal != 0)
					{
						string sWs = wsf.GetStrFromWs(nVal);
						if (String.IsNullOrEmpty(sWs))
							throw new Exception("Invalid writing system for <Run ws=\"...\">.");
						writer.Write(" ws=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(sWs));
					}
					break;
				case (int)FwTextPropType.ktptBaseWs:
					if (nVal != 0)
					{
						string sWs = wsf.GetStrFromWs(nVal);
						if (String.IsNullOrEmpty(sWs))
							throw new Exception("Invalid writing system for <Run wsBase=\"...\">.");
						writer.Write(" wsBase=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(sWs));
					}
					break;
				case (int)FwTextPropType.ktptFontSize:
					writer.Write(" fontsize=\"{0}\"", nVal);
					if (nVar != (int)FwTextPropVar.ktpvDefault)
						writer.Write(" fontsizeUnit=\"{0}\"", PropVarName(nVar));
					break;
				case (int)FwTextPropType.ktptForeColor:
					writer.Write(" forecolor=\"{0}\"", ColorName(nVal));
					break;
				case (int)FwTextPropType.ktptItalic:
					writer.Write(" italic=\"{0}\"", ToggleValueName((byte)nVal));
					break;
				case (int)FwTextPropType.ktptOffset:
					writer.Write(" offset=\"{0}\"", nVal);
					writer.Write(" offsetUnit=\"{0}\"",	PropVarName(nVar));
					break;
				case (int)FwTextPropType.ktptSuperscript:
					writer.Write(" superscript=\"{0}\"", SuperscriptValName((byte)nVal));
					break;
				case (int)FwTextPropType.ktptUnderColor:
					writer.Write(" undercolor=\"{0}\"", ColorName(nVal));
					break;
				case (int)FwTextPropType.ktptUnderline:
					writer.Write(" underline=\"{0}\"", UnderlineTypeName((byte)nVal));
					break;
				case (int)FwTextPropType.ktptSpellCheck:
					writer.Write(" spellcheck=\"{0}\"", SpellingModeName((byte)nVal));
					break;

				// Paragraph level properties.

				case (int)FwTextPropType.ktptAlign:
					writer.Write(" align=\"{0}\"", AlignmentTypeName((byte)nVal));
					break;
				case (int)FwTextPropType.ktptFirstIndent:
					writer.Write(" firstIndent=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptLeadingIndent:
					writer.Write(" leadingIndent=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptTrailingIndent:
					writer.Write(" trailingIndent=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptSpaceBefore:
					writer.Write(" spaceBefore=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptSpaceAfter:
					writer.Write(" spaceAfter=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptTabDef:
					writer.Write(" tabDef=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptLineHeight:
					writer.Write(" lineHeight=\"{0}\"", Math.Abs(nVal));
					writer.Write(" lineHeightUnit=\"{0}\"",	PropVarName(nVar));
				Debug.Assert(nVal >= 0 || nVar == (int)FwTextPropVar.ktpvMilliPoint);
					if (nVar == (int)FwTextPropVar.ktpvMilliPoint)
					{
						// negative means "exact" internally.  See FWC-20.
						writer.Write(" lineHeightType=\"{0}\"", nVal < 0 ? "exact" : "atLeast");
					}
					break;
				case (int)FwTextPropType.ktptParaColor:
					writer.Write(" paracolor=\"{0}\"", ColorName(nVal));
					break;

				//	Properties from the views subsystem:

				case (int)FwTextPropType.ktptRightToLeft:
					writer.Write(" rightToLeft=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptPadLeading:
					writer.Write(" padLeading=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptPadTrailing:
					writer.Write(" padTrailing=\"{0}\"", nVal);
					break;
				// Not the other margins: they are duplicated by FirstIndent etc.
				case (int)FwTextPropType.ktptMarginTop:
					writer.Write(" MarginTop=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptPadTop:
					writer.Write(" padTop=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptPadBottom:
					writer.Write(" padBottom=\"{0}\"", nVal);
					break;

				case (int)FwTextPropType.ktptBorderTop:
					writer.Write(" borderTop=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptBorderBottom:
					writer.Write(" borderBottom=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptBorderLeading:
					writer.Write(" borderLeading=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptBorderTrailing:
					writer.Write(" borderTrailing=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptBorderColor:
					writer.Write(" borderColor=\"{0}\"", ColorName(nVal));
					break;

				case (int)FwTextPropType.ktptBulNumScheme:
					writer.Write(" bulNumScheme=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptBulNumStartAt:
					writer.Write(" bulNumStartAt=\"{0}\"", nVal == Int32.MinValue ? 0 : nVal);
					break;

				case (int)FwTextPropType.ktptDirectionDepth:
					writer.Write(" directionDepth=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptKeepWithNext:
					writer.Write(" keepWithNext=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptKeepTogether:
					writer.Write(" keepTogether=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptHyphenate:
					writer.Write(" hyphenate=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptWidowOrphanControl:
					writer.Write(" widowOrphan=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptMaxLines:
					writer.Write(" maxLines=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptCellBorderWidth:
					writer.Write(" cellBorderWidth=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptCellSpacing:
					writer.Write(" cellSpacing=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptCellPadding:
					writer.Write(" cellPadding=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptEditable:
					writer.Write(" editable=\"{0}\"", EditableName(nVal));
					break;
				case (int)FwTextPropType.ktptSetRowDefaults:
					writer.Write(" setRowDefaults=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptRelLineHeight:
					writer.Write(" relLineHeight=\"{0}\"", nVal);
					break;
				case (int)FwTextPropType.ktptTableRule:
					// Ignore this view-only property.
					break;
				default:
					break;
			}
		}


		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Interpret the color value as a character string.  We need to be compatible in
		/// hexadecimal values with the XHTML standard.  The version 1.0 "strict" DTD has the
		/// following information:
		///
		/// White  = #FFFFFF		== white
		/// Black  = #000000		== black
		/// Red    = #FF0000		== red
		/// Lime   = #00FF00		== green
		/// Blue   = #0000FF		== blue
		/// Yellow = #FFFF00		== yellow
		/// Fuchsia= #FF00FF		== magenta
		/// Aqua   = #00FFFF		== cyan
		///
		/// Green  = #008000
		/// Silver = #C0C0C0
		/// Gray   = #808080
		/// Maroon = #800000
		/// Purple = #800080
		/// Olive  = #808000
		/// Navy   = #000080
		/// Teal   = #008080
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static string ColorName(int nColor)
		{
			switch (nColor)
			{
				case (int)FwTextColor.kclrWhite:		return "white";
				case (int)FwTextColor.kclrBlack:		return "black";
				case (int)FwTextColor.kclrRed:			return "red";
				case (int)FwTextColor.kclrGreen:		return "green";
				case (int)FwTextColor.kclrBlue:			return "blue";
				case (int)FwTextColor.kclrYellow:		return "yellow";
				case (int)FwTextColor.kclrMagenta:		return "magenta";
				case (int)FwTextColor.kclrCyan:			return "cyan";
				case (int)FwTextColor.kclrTransparent:	return "transparent";
			}
			int nBlue = (nColor >> 16) & 0xFF;
			int nGreen = (nColor >> 8) & 0xFF;
			int nRed = nColor & 0xFF;
			return String.Format("{0:X2}{1:X2}{2:X2}", nRed, nGreen, nBlue);
		}

		///-------------------------------------------------------------------------------------
		/// <summary>Interpret the FwTextToggleVal enum value as a character string.</summary>
		///-------------------------------------------------------------------------------------
		private static string ToggleValueName(byte ttv)
		{
			switch (ttv)
			{
				case (int)FwTextToggleVal.kttvOff:		return "off";
				case (int)FwTextToggleVal.kttvForceOn:	return "on";
				case (int)FwTextToggleVal.kttvInvert:	return "invert";
				default:								return String.Format("{0:U}", ttv);
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>Interpret the FwTextPropVar enum value as a character string.</summary>
		///-------------------------------------------------------------------------------------
		private static string PropVarName(int tpv)
		{
			switch (tpv)
			{
				case (int)FwTextPropVar.ktpvMilliPoint:	return "mpt";
				case (int)FwTextPropVar.ktpvRelative:	return "rel";
				default:								return String.Format("{0:U}", tpv);
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>Interpret the FwSuperscriptVal enum value as a character string.</summary>
		///-------------------------------------------------------------------------------------
		private static string SuperscriptValName(byte ssv)
		{
			switch (ssv)
			{
				case (int)FwSuperscriptVal.kssvOff:		return "off";
				case (int)FwSuperscriptVal.kssvSuper:	return "super";
				case (int)FwSuperscriptVal.kssvSub:		return "sub";
				default:								return String.Format("{0:U}", ssv);
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>Interpret the FwUnderlineType enum value as a character string.</summary>
		///-------------------------------------------------------------------------------------
		private static string UnderlineTypeName(byte unt)
		{
			switch (unt)
			{
				case (int)FwUnderlineType.kuntNone:				return "none";
				case (int)FwUnderlineType.kuntDotted:			return "dotted";
				case (int)FwUnderlineType.kuntDashed:			return "dashed";
				case (int)FwUnderlineType.kuntStrikethrough:	return "strikethrough";
				case (int)FwUnderlineType.kuntSingle:			return "single";
				case (int)FwUnderlineType.kuntDouble:			return "double";
				case (int)FwUnderlineType.kuntSquiggle:			return "squiggle";
				default:										return String.Format("{0:U}", unt);
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>Interpret the SpellingMode enum value as a character string.</summary>
		///-------------------------------------------------------------------------------------
		private static string SpellingModeName(byte sm)
		{
			switch (sm)
			{
				case (int)SpellingModes.ksmNormalCheck:	return "normal";
				case (int)SpellingModes.ksmDoNotCheck:	return "doNotCheck";
				case (int)SpellingModes.ksmForceCheck:	return "forceCheck";
				default:								return String.Format("{0:U}", sm);
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>Interpret the FwTextAlign enum value as a character string.</summary>
		///-------------------------------------------------------------------------------------
		private static string AlignmentTypeName(byte tal)
		{
			switch (tal)
			{
				case (int)FwTextAlign.ktalLeading:	return "leading";
				case (int)FwTextAlign.ktalLeft:		return "left";
				case (int)FwTextAlign.ktalCenter:	return "center";
				case (int)FwTextAlign.ktalRight:	return "right";
				case (int)FwTextAlign.ktalTrailing:	return "trailing";
				case (int)FwTextAlign.ktalJustify:	return "justify";
				default:							return String.Format("{0:U}", tal);
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>Interpret the TptEditable enum value as a character string.</summary>
		///-------------------------------------------------------------------------------------
		private static string EditableName(int ted)
		{
			switch (ted)
			{
				case (int)TptEditable.ktptNotEditable:	return "not";
				case (int)TptEditable.ktptIsEditable:	return "is";
				case (int)TptEditable.ktptSemiEditable:	return "semi";
				default:								return String.Format("{0:U}", ted);
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Write the string text property as an XML attribute.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="tpt"></param>
		/// <param name="sVal"></param>
		///-------------------------------------------------------------------------------------
		public void WriteStrTextProp(TextWriter writer, int tpt, string sVal)
		{
			Debug.Assert(writer != null);

			Guid guid;
			switch (tpt)
			{
				case (int)FwTextPropType.ktptCharStyle:
					if (!String.IsNullOrEmpty(sVal))
						sVal = m_xhtml.GetValidCssClassName(sVal);
					writer.Write(" charStyle=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(sVal));
					break;
				case (int)FwTextPropType.ktptFontFamily:
					writer.Write(" fontFamily=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(sVal));
					break;

				case (int)FwTextPropType.ktptObjData:
					if (sVal.Length >= 1)
					{
						switch ((int)sVal[0])
						{
						case (int)FwObjDataTypes.kodtPictEvenHot:
						case (int)FwObjDataTypes.kodtPictOddHot:
							// type="chars" is the default value for this attribute.
							// The caller is responsible for writing out the picture data.  (This is
							// an antique kludge that isn't really used in practice, but some of our
							// test data still exercises it.)
							writer.Write(" type=\"picture\"");
							break;

						case (int)FwObjDataTypes.kodtNameGuidHot:
							guid = MiscUtils.GetGuidFromObjData(sVal.Substring(1));
							writer.Write(" link=\"{0}\"", guid);
							break;

						case (int)FwObjDataTypes.kodtExternalPathName:
							writer.Write(" externalLink=\"{0}\"",
								XmlUtils.MakeSafeXmlAttribute(sVal.Substring(1)));
							break;

						case (int)FwObjDataTypes.kodtOwnNameGuidHot:
							guid = MiscUtils.GetGuidFromObjData(sVal.Substring(1));
							writer.Write(" ownlink=\"{0}\"", guid);
							break;

						case (int)FwObjDataTypes.kodtEmbeddedObjectData:
							// used ONLY in the clipboard...code won't know what to do with it
							// elsewhere!
							break;

						case (int)FwObjDataTypes.kodtContextString:
							// This is a generated context-sensitive string.  The next 8 characters give a
							// GUID, which is from to a known set of GUIDs that have special meaning to a
							// view contructor.
							guid = MiscUtils.GetGuidFromObjData(sVal.Substring(1));
							writer.Write(" contextString=\"{0}\"", guid);
							break;

						case (int)FwObjDataTypes.kodtGuidMoveableObjDisp:
							// This results in a call-back to the VC, with a new VwEnv, to create any
							// display it wants of the object specified by the Guid (see
							// IVwViewConstructor.DisplayEmbeddedObject).  The display will typically
							// occur immediately following the paragraph line that contains the ORC,
							// which functions as an anchor, but may be moved down past following text
							// to improve page breaking.
							guid = MiscUtils.GetGuidFromObjData(sVal.Substring(1));
							writer.Write(" moveableObj=\"{0}\"", guid);
							break;

						default:
							// This forces an assert when a new enum value is added and used.
							Debug.Assert((int)sVal[0] == (int)FwObjDataTypes.kodtExternalPathName);
							break;
						}
					}
					break;

				//	Properties from the views subsystem:

				case (int)FwTextPropType.ktptNamedStyle:
					if (!String.IsNullOrEmpty(sVal))
						sVal = m_xhtml.GetValidCssClassName(sVal);
					writer.Write(" namedStyle=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(sVal));
					break;
				case (int)FwTextPropType.ktptBulNumTxtBef:
					writer.Write(" bulNumTxtBef=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(sVal));
					break;
				case (int)FwTextPropType.ktptBulNumTxtAft:
					writer.Write(" bulNumTxtAft=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(sVal));
					break;
				case (int)FwTextPropType.ktptBulNumFontInfo:
					// BulNumFontInfo is written separately.
					break;
				case (int)FwTextPropType.ktptWsStyle:
					//	WsStyles are written separately
					break;
				case (int)FwTextPropType.ktptFontVariations:		// string, giving variation names specific to font.
					writer.Write(" fontVariations=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(sVal));
					break;
				case (int)FwTextPropType.ktptParaStyle:
					writer.Write(" paraStyle=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(sVal));
					break;
				case (int)FwTextPropType.ktptTabList:
					writer.Write(" tabList=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(sVal));
					break;
				case (int)FwTextPropType.ktptTags:
					// prop holds sequence of 8-char items, each the memcpy-equivalent of a GUID
					Debug.Assert(sVal.Length % 8 == 0);
					writer.Write(" tags=\"");
					for (int ich = 0; ich < sVal.Length; ich += 8)
					{
						guid = MiscUtils.GetGuidFromObjData(sVal.Substring(ich, 8));
						if (ich > 0)
							writer.Write(" ");
						writer.Write("I{0}", guid);
					}
					writer.Write("\"");
					break;
/*
  ktptFieldName:	// Fake string valued text property used for exporting.
*/
			default:
				break;
			}
		}

		private void ExportPictures(ITsString tssPara)
		{
			int crun = tssPara.RunCount;
			for (int i = 0; i < crun; ++i)
			{
				string runText = tssPara.get_RunText(i);
				if (runText != null && runText.Length == 1 && runText[0] == StringUtils.kChObject)
				{
					ITsTextProps ttp = tssPara.get_Properties(i);
					string objData = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);
					if (!String.IsNullOrEmpty(objData) &&
						objData[0] == (char)FwObjDataTypes.kodtGuidMoveableObjDisp)
					{
						Guid pictureGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
						ICmPicture picture;
						if (m_cache.ServiceLocator.GetInstance<ICmPictureRepository>().TryGetObject(pictureGuid, out picture))
							ExportPicture(picture);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write the first paragraph in the given footnote.  (TE allows only 1 paragraph in
		/// footnotes!)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ExportFootnote(IScrFootnote foot)
		{
			IStTxtPara para = foot.ParagraphsOS[0] as IStTxtPara;
			string sStyle = para.StyleName;
			if (String.IsNullOrEmpty(sStyle))
				sStyle = "scrFootnote";
			else
				sStyle = m_xhtml.GetValidCssClassName(sStyle);
			ITsStrBldr bldr = foot.MakeFootnoteMarker(m_cache.DefaultVernWs);
			string sFootnoteMarker = bldr.Text;
			ITsString tssPara = para.Contents;
			m_writer.WriteLine("<span class=\"{0}\" id=\"{1}\" title=\"{2}\">",
				sStyle, "F" + foot.Guid.ToString().ToUpperInvariant(), sFootnoteMarker);
			m_xhtml.MapCssToLang(sStyle, LanguageCode(TsStringUtils.GetWsAtOffset(tssPara, 0)));
			WriteTsStringAsXml(tssPara, 4);
			m_writer.WriteLine("</span>");
			// The following class is inserted by the XSLT processing.
			m_xhtml.MapCssToLang("scrFootnoteMarker", LanguageCode(m_cache.DefaultVernWs));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write out the picture with the caption as a &lt;div&gt; element.
		/// </summary>
		/// <param name="pict"></param>
		/// ------------------------------------------------------------------------------------
		private void ExportPicture(ICmPicture pict)
		{
			ITsString tssCaption = pict.Caption.VernacularDefaultWritingSystem;
			string sCaption;
			if (tssCaption.Length > 0 && !tssCaption.Equals(pict.Caption.NotFoundTss))
				sCaption = tssCaption.Text.Normalize();
			else
				sCaption = null;
			m_writer.WriteLine("<div class=\"pictureCenter\">");
			m_xhtml.MapCssToLang("pictureCenter", LanguageCode(m_cache.DefaultVernWs));
			m_writer.WriteLine("<img id=\"{0}\" class=\"picture\" src=\"{1}\" alt=\"{1}\"/>",
				"P" + pict.Guid.ToString().ToUpperInvariant(),
				XmlUtils.MakeSafeXmlAttribute(pict.PictureFileRA.InternalPath).Replace('\\', '/'));
			m_xhtml.MapCssToLang("picture", LanguageCode(m_cache.DefaultVernWs));
			if (sCaption != null)
			{
				m_writer.WriteLine("<div class=\"pictureCaption\">");
				m_xhtml.MapCssToLang("pictureCaption", LanguageCode(m_cache.DefaultVernWs));
				WriteTsStringAsXml(tssCaption, 4);
				m_writer.WriteLine("</div>");
			}
			m_writer.WriteLine("</div>");
		}

		private string LanguageCode(int ws)
		{
			string sLang;
			if (!m_mapWsToRFC.TryGetValue(ws, out sLang))
			{
				// "magic" (and other invalid) codes shouldn't sneak through to here, but if
				// they do, we don't want to crash!  See TE-7708 and TE-7727.
				int wsReal;
				switch (ws)
				{
					case WritingSystemServices.kwsAnal:
					case WritingSystemServices.kwsAnals:
					case WritingSystemServices.kwsAnalVerns:
					case WritingSystemServices.kwsFirstAnal:
					case WritingSystemServices.kwsFirstAnalOrVern:
						wsReal = m_cache.DefaultAnalWs;
						break;
					case WritingSystemServices.kwsVern:
					case WritingSystemServices.kwsVerns:
					case WritingSystemServices.kwsVernAnals:
					case WritingSystemServices.kwsFirstVern:
					case WritingSystemServices.kwsFirstVernOrAnal:
					case WritingSystemServices.kwsVernInParagraph:
					case WritingSystemServices.kwsFirstVernOrNamed:
						wsReal = m_cache.DefaultVernWs;
						break;
					case WritingSystemServices.kwsPronunciation:
					case WritingSystemServices.kwsFirstPronunciation:
					case WritingSystemServices.kwsPronunciations:
						wsReal = m_cache.DefaultPronunciationWs;
						break;
					default:
						// Anything invalid that we don't recognize is treated as vernacular.
						if (ws <= 0)
							wsReal = m_cache.DefaultVernWs;
						else
							wsReal = ws;
						break;
				}
				WritingSystem wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(wsReal);
				sLang = wsObj.Id;
				m_mapWsToRFC.Add(ws, sLang);
			}
			return sLang;
		}
	}
}
