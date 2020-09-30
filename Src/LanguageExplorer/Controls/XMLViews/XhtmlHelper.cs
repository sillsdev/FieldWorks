// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class contains method that are useful for XHTML export in both Flex and TE.
	/// </summary>
	public class XhtmlHelper
	{
		private StyleInfoTable m_styleTable;
		// The keys in these three dictionaries are VALID CSS class names, that is, typically the output
		// of calling GetValidCssClassName. (This is not necessary with fixed strings known not to contain
		// illegal characters).
		private SortedDictionary<string, List<string>> m_dictClassData = new SortedDictionary<string, List<string>>();
		private SortedDictionary<string, XElement> m_mapCssClassToElement = new SortedDictionary<string, XElement>();
		private SortedDictionary<string, List<string>> m_mapCssToStyleEnv = new SortedDictionary<string, List<string>>();
		/// <summary>
		/// Map from the specified class to its base FieldWorks style name.  This is used for
		/// paragraph styles used for publishing sections of a definition as a paragraph.
		/// </summary>
		private Dictionary<string, string> m_mapClassToStyle = new Dictionary<string, string>();
		/// <summary>
		/// This dictionary is used to keep track of sequences that specify sequence numbering with 'number' and 'cssNumber' attributes.
		/// </summary>
		public Dictionary<string, Tuple<string, string>> NumberStyles = new Dictionary<string, Tuple<string, string>>();
		private TextWriter m_writer;
		private LcmCache m_cache;
		private bool m_fRTL;
		private int m_cColumns = 2;     // default for dictionary output.
		private readonly Dictionary<string, string> m_mapFwStyleToCssClass = new Dictionary<string, string>();
		private readonly Dictionary<string, string> m_mapCssClassToFwStyle = new Dictionary<string, string>();
		private static readonly Guid PageNumberGuid = new Guid("644DF48A-3B60-45f4-80C7-739BE6E56A96");
		private static readonly Guid FirstReferenceGuid = new Guid("397F43AE-E2B2-4f20-928A-1DF193C07674");
		private static readonly Guid LastReferenceGuid = new Guid("85EE15C6-0799-46c6-8769-F9B3CE313AE2");
		private static readonly Guid TotalPagesGuid = new Guid("E0EF9EDA-E4E2-4fcf-8720-5BC361BCE110");
		private static readonly Guid PrintDateGuid = new Guid("C4556A21-41A8-4675-A74D-59B2C1A7E2B8");
		private static readonly Guid DivisionNameGuid = new Guid("2277B85F-47BB-45c9-BC7A-7232E26E901C");
		private static readonly Guid PublicationTitleGuid = new Guid("C8136D98-6957-43bd-BEA9-7DCE35200900");
		private static readonly Guid PageReferenceGuid = new Guid("8978089A-8969-424e-AE54-B94C554F882D");
		private static readonly Guid ProjectNameGuid = new Guid("5610D086-635F-4ae2-8E85-A95896F3D62D");
		private static readonly Guid BookNameGuid = new Guid("48C0E5E3-C909-42e1-8F82-3489E3DE96FA");

		/// <summary />
		public XhtmlHelper(TextWriter w, LcmCache cache)
		{
			m_writer = w;
			m_cache = cache;
			var ws = cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			m_fRTL = ws.RightToLeftScript;
		}

		/// <summary>
		/// Test-only constructor.
		/// </summary>
		internal XhtmlHelper()
		{
		}

		/// <summary />
		public void MapCssClassToXElement(string cssClass, XElement node)
		{
			m_mapCssClassToElement.Add(GetValidCssClassName(cssClass), node);
		}

		/// <summary />
		public bool TryGetNodeFromCssClass(string cssClass, out XElement node)
		{
			return m_mapCssClassToElement.TryGetValue(GetValidCssClassName(cssClass), out node);
		}

		/// <summary>
		/// This method adds the given List of envirs strings with the key of cssClass, throws on duplicate key
		/// </summary>
		public void MapCssToStyleEnv(string cssClass, List<string> envirs)
		{
			m_mapCssToStyleEnv.Add(cssClass, envirs);
		}

		/// <summary />
		public bool MapCssToStyleEnv(string cssClass, out List<string> envirs)
		{
			return m_mapCssToStyleEnv.TryGetValue(cssClass, out envirs);
		}

		/// <summary>
		/// This method will take a List of languages(string representation) and map this list to a given
		/// cssClass string. This will throw an exception if the cssClass string has already been used in the dictionary.
		/// </summary>
		public void MapCssToLangs(string cssClass, List<string> rgsLangs)
		{
			m_dictClassData.Add(GetValidCssClassName(cssClass), rgsLangs);
		}

		/// <summary>
		/// This method will take a single string representing a language, and add it to the member dictionary using the given
		/// cssClass string. If the cssClass has already been used it will add the language to the existing language list. If no list exists
		/// one will be created.
		/// </summary>
		public void MapCssToLang(string cssClass, string sLang)
		{
			var key = GetValidCssClassName(cssClass);
			if (!m_dictClassData.TryGetValue(key, out var rgsLangs))
			{
				rgsLangs = new List<string>();
				m_dictClassData.Add(key, rgsLangs);
			}
			if (!rgsLangs.Contains(sLang))
			{
				rgsLangs.Add(sLang);
			}
		}

		/// <summary>
		/// Get the list of languages from the given cssClass if possible. Returns false if there is no associated list.
		/// </summary>
		/// <param name="cssClass">the cssClass string to search with</param>
		/// <param name="rgsLangs">out parameter to be filled in with the list of languages</param>
		/// <returns>true if value was found, false otherwise</returns>
		public bool TryGetLangsFromCss(string cssClass, out List<string> rgsLangs)
		{
			return m_dictClassData.TryGetValue(GetValidCssClassName(cssClass), out rgsLangs);
		}

		/// <summary>
		/// Write the initial lines of an XHTML output file.
		/// </summary>
		public void WriteXhtmlHeading(string sOutPath, string sDescription, string sBodyClass)
		{
			var sOutFile = Path.GetFileName(sOutPath);
			m_writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			m_writer.WriteLine("<html xml:lang=\"utf-8\" lang=\"utf-8\">");
			WriteWritingSystemInfo();
			m_writer.WriteLine("<head>");
			m_writer.WriteLine("<title/>");
			m_writer.WriteLine("<link rel=\"stylesheet\" href=\"{0}\" type=\"text/css\"/>", XmlUtils.MakeSafeXmlAttribute(Path.ChangeExtension(sOutFile, ".css")));
			m_writer.WriteLine("<meta name=\"linkedFilesRootDir\" content=\"{0}\"/>", XmlUtils.MakeSafeXmlAttribute(m_cache.LangProject.LinkedFilesRootDir));
			if (!string.IsNullOrEmpty(sDescription))
			{
				m_writer.WriteLine("<meta name=\"description\" content=\"{0}\"/>", XmlUtils.MakeSafeXmlAttribute(sDescription));
			}
			m_writer.WriteLine("<meta name=\"filename\" content=\"{0}\"/>", XmlUtils.MakeSafeXmlAttribute(sOutFile));
			m_writer.WriteLine("</head>");
			m_writer.WriteLine("<body class=\"{0}\">", sBodyClass);
		}

		/// <summary>
		/// Write the final lines of an XHTML output file.
		/// </summary>
		public void WriteXhtmlEnding()
		{
			m_writer.WriteLine("</body>");
			m_writer.WriteLine("</html>");
		}

		private void WriteWritingSystemInfo()
		{
			foreach (var ws in m_cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				m_writer.Write("<WritingSystemInfo lang=\"{0}\" dir=\"{1}\"", ws.Id, ws.RightToLeftScript ? "rtl" : "ltr");
				WriteWsListTag(ws, m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems, "vernTag");
				WriteWsListTag(ws, m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems, "analTag");
				WriteWsListTag(ws, m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems, "pronTag");
				if (!string.IsNullOrEmpty(ws.DefaultFontName))
				{
					m_writer.Write(" font=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(ws.DefaultFontName));
				}
				if (!string.IsNullOrEmpty(ws.LanguageName))
				{
					m_writer.Write(" name=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(ws.DisplayLabel));
				}
				m_writer.WriteLine("/>");
			}
		}

		private void WriteWsListTag(CoreWritingSystemDefinition ws, IEnumerable<CoreWritingSystemDefinition> wss, string sAttr)
		{
			var i = 0;
			foreach (var curWs in wss)
			{
				if (ws == curWs)
				{
					var sTag = string.Empty;
					if (i > 0)
					{
						sTag = $"_L{i + 1}";
					}
					m_writer.Write(" {0}=\"{1}\"", sAttr, sTag);
				}
				i++;
			}
		}

		/// <summary>
		/// Set true currently only for classified dictionary, to allow entries to indent from domain headings.
		/// Used in WriteParaStyleInfoToCss, it controls how paragraph styles are exported in WriteParaStyleInfoToCss.
		/// A comment there says that LT-12658 calls for suppressing indentation inside paragraphs (entries).
		/// This is not obvious to me (JohnT) reading the issue, but I wanted to change things minimally
		/// so added this property which is set only for the new export style.
		/// </summary>
		public bool AllowDictionaryParagraphIndent { get; set; }

		/// <summary>
		/// Write a Cascading Style Sheet file based on the layouts accumulated in
		/// m_dictCssXnode and the given stylesheet.
		/// </summary>
		public void WriteCssFile(string sOutputFile, IVwStylesheet vss, CssType type, IPublication pub)
		{
			// Read all the style information from the database.
			ReadStyles(vss);
			// Write the Cascading Style Sheet.
			using (m_writer = FileUtils.OpenFileForWrite(sOutputFile, Encoding.UTF8))
			{
				m_writer.WriteLine("/* Cascading Style Sheet generated by {0} version {1} on {2} {3} */",
					Assembly.GetEntryAssembly().GetName().Name,
					Assembly.GetEntryAssembly().GetName().Version,
					DateTime.Now.ToLongDateString(), DateTime.Now.ToShortTimeString());
				m_writer.WriteLine("@page {");
				m_writer.WriteLine("    counter-increment: page;");
				if (pub != null)
				{
					if (pub.PaperHeight != 0 && pub.PaperWidth != 0)
					{
						m_writer.WriteLine("    height: {0}pt;", ConvertMptToPt(pub.PaperHeight));
						m_writer.WriteLine("    width: {0}pt;", ConvertMptToPt(pub.PaperWidth));
					}
					else if (pub.IsLandscape)
					{
						m_writer.WriteLine("    height: 8.5in;");
						m_writer.WriteLine("    width: 11in;");
					}
					else
					{
						m_writer.WriteLine("    height: 11in;");
						m_writer.WriteLine("    width: 8.5in;");
					}
					if (pub.DivisionsOS.Count > 0)
					{
						m_writer.WriteLine("    margin: {0}pt {1}pt {2}pt {3}pt;",
							ConvertMptToPt(pub.DivisionsOS[0].PageLayoutOA.MarginTop),
							ConvertMptToPt(pub.IsLeftBound ? pub.DivisionsOS[0].PageLayoutOA.MarginOutside : pub.DivisionsOS[0].PageLayoutOA.MarginInside),
							ConvertMptToPt(pub.DivisionsOS[0].PageLayoutOA.MarginBottom),
							ConvertMptToPt(pub.IsLeftBound ? pub.DivisionsOS[0].PageLayoutOA.MarginInside : pub.DivisionsOS[0].PageLayoutOA.MarginOutside));
						var sInside = pub.IsLeftBound ? "left" : "right";
						var sOutside = pub.IsLeftBound ? "right" : "left";
						WriteHeaderFooterBlock("top-" + sInside, pub.DivisionsOS[0].HFSetOA.DefaultHeaderOA.InsideAlignedText);
						WriteHeaderFooterBlock("top-center", pub.DivisionsOS[0].HFSetOA.DefaultHeaderOA.CenteredText);
						WriteHeaderFooterBlock("top-" + sOutside, pub.DivisionsOS[0].HFSetOA.DefaultHeaderOA.OutsideAlignedText);
						WriteHeaderFooterBlock("bottom-" + sInside, pub.DivisionsOS[0].HFSetOA.DefaultFooterOA.InsideAlignedText);
						WriteHeaderFooterBlock("bottom-center", pub.DivisionsOS[0].HFSetOA.DefaultFooterOA.CenteredText);
						WriteHeaderFooterBlock("bottom-" + sOutside, pub.DivisionsOS[0].HFSetOA.DefaultFooterOA.OutsideAlignedText);
						WriteFootnotesBlock(type);
						if (pub.DivisionsOS[0].DifferentEvenHF)
						{
							m_writer.WriteLine("}");
							m_writer.WriteLine(pub.IsLeftBound ? "@page :left {" : "@page :right {");
							WriteHeaderFooterBlock("top-" + sInside, pub.DivisionsOS[0].HFSetOA.EvenHeaderOA.OutsideAlignedText);
							WriteHeaderFooterBlock("top-center", pub.DivisionsOS[0].HFSetOA.EvenHeaderOA.CenteredText);
							WriteHeaderFooterBlock("top-" + sOutside, pub.DivisionsOS[0].HFSetOA.EvenHeaderOA.InsideAlignedText);
							WriteHeaderFooterBlock("bottom-" + sInside, pub.DivisionsOS[0].HFSetOA.EvenFooterOA.OutsideAlignedText);
							WriteHeaderFooterBlock("bottom-center", pub.DivisionsOS[0].HFSetOA.EvenFooterOA.CenteredText);
							WriteHeaderFooterBlock("bottom-" + sOutside, pub.DivisionsOS[0].HFSetOA.EvenFooterOA.InsideAlignedText);
						}
						if (pub.DivisionsOS[0].DifferentFirstHF)
						{
							m_writer.WriteLine("}");
							m_writer.WriteLine("@page :first {");
							WriteHeaderFooterBlock("top-" + sInside, pub.DivisionsOS[0].HFSetOA.FirstHeaderOA.InsideAlignedText);
							WriteHeaderFooterBlock("top-center", pub.DivisionsOS[0].HFSetOA.FirstHeaderOA.CenteredText);
							WriteHeaderFooterBlock("top-" + sOutside, pub.DivisionsOS[0].HFSetOA.FirstHeaderOA.OutsideAlignedText);
							WriteHeaderFooterBlock("bottom-" + sInside, pub.DivisionsOS[0].HFSetOA.FirstFooterOA.InsideAlignedText);
							WriteHeaderFooterBlock("bottom-center", pub.DivisionsOS[0].HFSetOA.FirstFooterOA.CenteredText);
							WriteHeaderFooterBlock("bottom-" + sOutside, pub.DivisionsOS[0].HFSetOA.FirstFooterOA.OutsideAlignedText);
						}
						m_cColumns = pub.DivisionsOS[0].NumColumns;
					}
				}
				else
				{
					switch (type)
					{
						case CssType.Dictionary:
							m_writer.WriteLine("    margin: 2cm 2cm 2cm 2cm;");
							m_writer.WriteLine("    @top-left {");
							m_writer.WriteLine("        content: string(guideword, first);");
							WriteSansSerifFontFamilyForWs(m_cache.DefaultVernWs, true);
							m_writer.WriteLine("        font-weight: bold;");
							m_writer.WriteLine("        font-size: 12pt;");
							m_writer.WriteLine("        margin-top: 1em;");
							m_writer.WriteLine("    }");
							m_writer.WriteLine("    @top-center {");
							m_writer.WriteLine("        content: counter(page);");
							m_writer.WriteLine("        margin-top: 1em");
							m_writer.WriteLine("    }");
							m_writer.WriteLine("    @top-right {");
							m_writer.WriteLine("        content: string(guideword, last);");
							WriteSansSerifFontFamilyForWs(m_cache.DefaultVernWs, true);
							m_writer.WriteLine("        font-weight: bold;");
							m_writer.WriteLine("        font-size: 12pt;");
							m_writer.WriteLine("        margin-top: 1em;");
							m_writer.WriteLine("    }");
							WriteFootnotesBlock(type);
							m_writer.WriteLine("}");
							m_writer.WriteLine("@page :first {");
							m_writer.WriteLine("    @top-left { content: ''; }");
							m_writer.WriteLine("    @top-center { content: ''; }");
							m_writer.WriteLine("    @top-right { content: ''; }");
							break;
						case CssType.Scripture:
							m_writer.WriteLine("    margin: 2cm 2cm 2cm 2cm;");
							m_writer.WriteLine("    counter-increment: page;");
							WriteFootnotesBlock(type);
							break;
						case CssType.Notebook:
							m_writer.WriteLine("    margin: 2cm 2cm 2cm 2cm;");
							m_writer.WriteLine("    counter-increment: page;");
							break;
					}
				}
				m_writer.WriteLine("}");
				m_writer.WriteLine();
				var langProj = m_cache.LangProject;
				foreach (var ws in langProj.CurrentAnalysisWritingSystems.Union(langProj.CurrentPronunciationWritingSystems).Union(langProj.CurrentVernacularWritingSystems))
				{
					var dir = ws.RightToLeftScript;
					m_writer.WriteLine(":lang(" + ws.IcuLocale + ") {direction:" + (dir ? "rtl" : "ltr") + "}");
				}
				m_writer.WriteLine();
				switch (type)
				{
					case CssType.Dictionary:
						ProcessDictionaryTypeClasses();
						break;
					case CssType.Scripture:
						foreach (var sClass in m_dictClassData.Keys)
						{
							ProcessScriptureCssStyle(sClass, m_dictClassData[sClass]);
						}
						break;
					case CssType.Notebook:
						ProcessNotebookTypeClasses();
						break;
				}
				m_writer.Close();
			}
			m_writer = null;
		}

		private void WriteFootnotesBlock(CssType type)
		{
			if (type != CssType.Scripture)
			{
				return;
			}
			m_writer.WriteLine("    @footnotes {");     // Prince XML
			m_writer.WriteLine("        border-top: thin solid black;");
			m_writer.WriteLine("        padding: 0.3em 0;");
			m_writer.WriteLine("        margin-top: 0.6em;");
			m_writer.WriteLine("        margin-left: 2pi;");
			m_writer.WriteLine("    }");
			m_writer.WriteLine("    @footnote {");      // CSS3
			m_writer.WriteLine("        border-top: thin solid black;");
			m_writer.WriteLine("        padding: 0.3em 0;");
			m_writer.WriteLine("        margin-top: 0.6em;");
			m_writer.WriteLine("        margin-left: 2pi;");
			m_writer.WriteLine("    }");
		}

		private void WriteHeaderFooterBlock(string sBlockName, ITsString tss)
		{
			if (tss == null)
			{
				return;
			}
			var bldr = new StringBuilder();
			var crun = tss.RunCount;
			for (var i = 0; i < crun; ++i)
			{
				var sRun = tss.get_RunText(i);
				if (string.IsNullOrEmpty(sRun))
				{
					continue;
				}
				var ttp = tss.get_Properties(i);
				if (sRun.Length == 1 && sRun[0] == StringUtils.kChObject)
				{
					var objData = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);
					if (string.IsNullOrEmpty(objData) || objData[0] != (char)FwObjDataTypes.kodtContextString)
					{
						continue;
					}
					var guid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
					if (guid == PageNumberGuid)
					{
						bldr.Append("counter(page) ");
					}
					else if (guid == FirstReferenceGuid)
					{
						bldr.AppendFormat("string(bookname, first) ' ' string(chapter, first) '{0}' string(verse, first) ", m_cache.LangProject.TranslatedScriptureOA.ChapterVerseSepr);
					}
					else if (guid == LastReferenceGuid)
					{
						bldr.AppendFormat("string(bookname, last) ' ' string(chapter, last) '{0}' string(verse, last) ", m_cache.LangProject.TranslatedScriptureOA.ChapterVerseSepr);
					}
					else if (guid == TotalPagesGuid)
					{
						bldr.Append("totalpagecount() ");
					}
					else if (guid == PrintDateGuid)
					{
						bldr.AppendFormat("'{0}' ", DateTime.Now.ToShortDateString());
					}
					else if (guid == DivisionNameGuid)
					{
						bldr.Append("string(divisionname) ");
					}
					else if (guid == PublicationTitleGuid)
					{
						bldr.Append("string(title)");
					}
					else if (guid == PageReferenceGuid)
					{
						bldr.Append("string(reference, page) ");
					}
					else if (guid == ProjectNameGuid)
					{
						bldr.AppendFormat("'{0}' ", m_cache.ProjectId.Name);
					}
					else if (guid == BookNameGuid)
					{
						bldr.Append("string(bookname)");
					}
				}
				else
				{
					bldr.AppendFormat("'{0}' ", sRun);
				}
			}
			if (bldr.Length > 0)
			{
				m_writer.WriteLine("    @{0} {{", sBlockName);
				m_writer.WriteLine("        content: {0};", bldr);
				WriteSansSerifFontFamilyForWs(m_cache.DefaultVernWs, true);
				/*
			font-weight: bold;
			font-size: 12pt;
			margin-top: 1em;
				 */
				m_writer.WriteLine("    }");
			}
			else
			{
				m_writer.WriteLine("    @{0} {{ content: ''; }}", sBlockName);
			}
		}

		private void ProcessScriptureCssStyle(string sClass, List<string> langs)
		{
			switch (sClass)
			{
				case "scrBody":
					WriteCssEmptyDefinition("scrBody");
					break;
				case "scrBook":
					WriteCssScrBook();
					break;
				case "scrBookName":
					WriteCssScrBookName();
					break;
				case "scrIntroSection":
					WriteCssScrIntroSection();
					break;
				case "scrSection":
					WriteCssScrSection();
					break;
				case "picture":
					WriteCssPicture();
					break;
				case "pictureRight":
					WriteCssPictureRight();
					break;
				case "pictureCenter":
					WriteCssPictureCenter();
					break;
				case "pictureCaption":
					WriteCssEmptyDefinition("pictureCaption");
					break;
				case "scrFootnote":
					WriteCssEmptyDefinition("scrFootnote"); // shouldn't be used, but just in case...
					break;
				case "scrFootnoteMarker":
					WriteCssScrFootnoteMarker();
					break;
				default:
					WriteScriptureCssStyle(sClass, langs);
					break;
			}
		}

		private void WriteScriptureCssStyle(string sStyle, List<string> langs)
		{
			var sCssStyleName = GetValidCssClassName(sStyle);
			if (!m_mapCssClassToFwStyle.TryGetValue(sCssStyleName, out var sStyleName))
			{
				sStyleName = sStyle.Replace('_', ' ');      // just in case...
			}
			m_writer.WriteLine(".{0} {{", sCssStyleName);
			var esi = WriteFontInfoToCss(m_cache.DefaultVernWs, sStyleName, null);
			if (esi != null)
			{
				WriteParaStyleInfoToCss(esi);
			}
			switch (sStyleName)
			{
				case "Chapter Number":
					m_writer.WriteLine(m_fRTL ? "    float: right;" : "    float: left;");
					m_writer.WriteLine("    string-set: chapter content();");
					break;
				case "Verse Number":
					m_writer.WriteLine("    string-set: verse content();");
					break;
				case "Note General Paragraph":
				case "Note Cross-Reference Paragraph":
					m_writer.WriteLine("    display: inline;");
					m_writer.WriteLine("    display: footnote;");
					m_writer.WriteLine("    display: prince-footnote;");
					m_writer.WriteLine("    position: footnote;");
					m_writer.WriteLine("    list-style-position: inside;");
					break;
				case "columns":
					m_writer.WriteLine("    column-count: 2; -moz-column-count: 2;");
					m_writer.WriteLine("    column-gap: .5cm; -moz-column-gap: .5cm;");
					break;
			}
			m_writer.WriteLine("}");
			foreach (var sLang in langs)
			{
				var ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(sLang);
				if (ws == 0 || ws == m_cache.DefaultVernWs)
				{
					continue;
				}
				m_writer.WriteLine(".{0}[lang='{1}'] {{", sCssStyleName, sLang);
				esi = WriteFontInfoToCss(ws, sStyleName, null);
				if (esi != null)
				{
					WriteParaStyleInfoToCss(esi);
				}
				m_writer.WriteLine("}");
			}
			if (sStyleName != "Note General Paragraph" && sStyleName != "Note Cross-Reference Paragraph")
			{
				return;
			}
			m_writer.WriteLine(".{0}::footnote-call {{", sCssStyleName);
			m_writer.WriteLine("    color: purple;");
			m_writer.WriteLine("    content: attr(title);");
			m_writer.WriteLine("    font-size: 6pt;");
			m_writer.WriteLine("    vertical-align: super;");
			m_writer.WriteLine("    line-height: none;");
			m_writer.WriteLine("}");
			m_writer.WriteLine(".{0}::footnote-marker {{", sCssStyleName);
			m_writer.WriteLine("    font-size: 10pt;");
			m_writer.WriteLine("    font-weight: bold;");
			m_writer.WriteLine("    content: string(chapter) ':' string(verse) ' = ';");
			m_writer.WriteLine("    /* content: string(footnoteMarker); font-size: 6pt; vertical-align: super; color: purple; */");
			m_writer.WriteLine("    text-align: left;");
			m_writer.WriteLine("}");
		}

		/// <summary>
		/// Convert a FieldWorks Style name into a valid CSS class name.
		/// This is also used for other names that need to be valid CSS classes, such as internal names that might
		/// be modified by adding #ClassN, since # is an invalid character that breaks browser behavior.
		/// When in doubt, if you want a safe class name, call it. It never modifies a class name that it outputs, so
		/// repeated applications are harmless. They are also fairly fast, because previously-seen inputs
		/// are remembered.
		/// </summary>
		public string GetValidCssClassName(string sFwStyle)
		{
			if (m_mapFwStyleToCssClass.TryGetValue(sFwStyle, out var sCssClassName))
			{
				return sCssClassName;
			}
			sCssClassName = sFwStyle.Replace(' ', '_');
			var rgchStyleName = sCssClassName.ToCharArray();
			for (var i = 0; i < rgchStyleName.Length; ++i)
			{
				var ch1 = rgchStyleName[i];
				int ch32;
				string sChar;
				if (Surrogates.IsLeadSurrogate(ch1))
				{
					if (++i < rgchStyleName.Length)
					{
						var ch2 = rgchStyleName[i];
						ch32 = Surrogates.Int32FromSurrogates(ch1, ch2);
						sChar = ch1 + ch2.ToString();
					}
					else
					{
						break;  // invalid data, but we can't fix it!
					}
				}
				else
				{
					ch32 = ch1;
					sChar = ch1.ToString();
				}
				if (ch32 != '_' && ch32 != '-' && !Icu.Character.IsAlphabetic(ch32) && !Icu.Character.IsNumeric(ch32) && !Icu.Character.IsDiacritic(ch32))
				{
					var sCharName = Icu.Character.GetCharName(ch32);
					sCharName = sCharName.Replace('-', '_');
					sCharName = sCharName.Replace(' ', '_');
					sCssClassName = sCssClassName.Replace(sChar, sCharName);
				}
			}
			var validStarts = new Regex("^[a-zA-Z]"); // standard allows _, -, but these are reserved for browser specials and have other conditions.
			if (!validStarts.IsMatch(sCssClassName))
			{
				sCssClassName = 'X' + sCssClassName;
			}
			m_mapFwStyleToCssClass.Add(sFwStyle, sCssClassName);
			if (!m_mapCssClassToFwStyle.TryGetValue(sCssClassName, out _))
			{
				m_mapCssClassToFwStyle.Add(sCssClassName, sFwStyle);
			}
			return sCssClassName;
		}

		private void WriteCssScrFootnoteMarker()
		{
			m_writer.WriteLine(".scrFootnoteMarker {");
			m_writer.WriteLine("}");
		}

		private void WriteCssScrIntroSection()
		{
			m_writer.WriteLine(".scrIntroSection {");
			m_writer.WriteLine("    text-align: {0};", m_fRTL ? "right" : "left");
			m_writer.WriteLine("}");
		}

		private void WriteCssScrSection()
		{
			m_writer.WriteLine(".scrSection {");
			if (m_cColumns > 1)
			{
				m_writer.WriteLine("    column-count: {0};   -moz-column-count: {0};", m_cColumns);
				m_writer.WriteLine("    column-gap: 1.5em; -moz-column-gap: 1.5em;");
				m_writer.WriteLine("    column-fill: auto;");
			}
			m_writer.WriteLine("    text-align: {0};", m_fRTL ? "right" : "left");
			m_writer.WriteLine("}");
		}

		private void WriteCssScrBook()
		{
			m_writer.WriteLine(".scrBook {");
			m_writer.WriteLine("    column-count: 1;");
			m_writer.WriteLine("    clear: both;");
			m_writer.WriteLine("}");
		}

		private void WriteCssScrBookName()
		{
			m_writer.WriteLine(".scrBookName {");
			m_writer.WriteLine("    string-set: bookname content();");
			m_writer.WriteLine("    display: none;");
			m_writer.WriteLine("}");
		}

		private void ProcessDictionaryTypeClasses()
		{
			if (m_dictClassData.ContainsKey("headref"))
			{
				WriteCssHeadref();
				m_dictClassData.Remove("headref");
			}
			if (m_dictClassData.ContainsKey("headref-before"))
			{
				WriteCssHeadrefBefore();
				m_dictClassData.Remove("headref-before");
			}
			if (m_dictClassData.ContainsKey("headref-after"))
			{
				WriteCssHeadrefAfter();
				m_dictClassData.Remove("headref-after");
			}
			if (m_dictClassData.ContainsKey("dicBody"))
			{
				WriteCssEmptyDefinition("dicBody");
				m_dictClassData.Remove("dicBody");
			}
			if (m_dictClassData.ContainsKey("letHead"))
			{
				WriteCssLetHead();
				m_dictClassData.Remove("letHead");
			}
			if (m_dictClassData.ContainsKey("letter"))
			{
				WriteCssLetter();
				m_dictClassData.Remove("letter");
			}
			if (m_dictClassData.ContainsKey("letData"))
			{
				WriteCssLetData();
				m_dictClassData.Remove("letData");
			}
			if (m_dictClassData.ContainsKey("entry"))
			{
				WriteCssEntry();
				m_dictClassData.Remove("entry");
			}
			if (m_dictClassData.ContainsKey("subentry"))
			{
				WriteCssSubentry();
				m_dictClassData.Remove("subentry");
			}
			if (m_dictClassData.ContainsKey("xhomographnumber"))
			{
				WriteCssXHomographNumber();
				m_dictClassData.Remove("xhomographnumber");
			}
			if (m_dictClassData.ContainsKey("homographnumberrev"))
			{
				WriteCssHomographNumberRev();
				m_dictClassData.Remove("homographnumberrev");
			}
			if (m_dictClassData.ContainsKey("sense"))
			{
				WriteCssSense();
				m_dictClassData.Remove("sense");
			}
			if (m_dictClassData.ContainsKey("sensepara"))
			{
				WriteCssSensePara();
				m_dictClassData.Remove("sensepara");
			}
			if (m_dictClassData.ContainsKey("bulletpara"))
			{
				WriteCssBulletPara();
				m_dictClassData.Remove("bulletpara");
			}
			if (m_dictClassData.ContainsKey("xsensenumber"))
			{
				WriteCssXSenseNumber();
				m_dictClassData.Remove("xsensenumber");
			}
			/*
			 * This is a kludgy yet simple fix for LT-14606.
			 * We are actually writing class="xsensenumber bold" to the XHTML
			 * which are two distinct Css classes: ".xsensenumber" and ".bold".
			 * However, GetValidCssClassName() joins these together in m_dictClassData dictionary
			 * as "xsensenumber_bold". Oh well, we know this means two classes.
			 */
			if (m_dictClassData.ContainsKey("xsensenumber_bold"))
			{
				WriteCssXSenseNumber();
				WriteCssBold();
				m_dictClassData.Remove("xsensenumber_bold");
			}
			if (m_dictClassData.ContainsKey("xsensexrefnumber"))
			{
				WriteCssXSenseXrefNumber();
				m_dictClassData.Remove("xsensexrefnumber");
			}
			if (m_dictClassData.ContainsKey("xsensenumber-sub"))
			{
				WriteCssXSenseNumberSub();
				m_dictClassData.Remove("xsensenumber-sub");
			}
			if (m_dictClassData.ContainsKey("sensenumberref"))
			{
				WriteCssSenseNumberRef();
				m_dictClassData.Remove("sensenumberref");
			}
			if (m_dictClassData.ContainsKey("pictureRight"))
			{
				WriteCssPictureRight();
				m_dictClassData.Remove("pictureRight");
			}
			if (m_dictClassData.ContainsKey("picture"))
			{
				WriteCssPicture();
				m_dictClassData.Remove("picture");
			}
			if (m_dictClassData.ContainsKey("pictureCaption"))
			{
				WriteCssEmptyDefinition("pictureCaption");
				m_dictClassData.Remove("pictureCaption");
			}
			if (m_dictClassData.ContainsKey("xlanguagetag"))
			{
				WriteCssXLanguageTag();
				m_dictClassData.Remove("xlanguagetag");
			}
			if (m_dictClassData.ContainsKey("customtext"))
			{
				WriteCssCustomText();
				m_dictClassData.Remove("customtext");
			}
			if (m_dictClassData.ContainsKey("complexform"))
			{
				WriteCssComplexForm();
				m_dictClassData.Remove("complexform");
			}
			if (m_dictClassData.ContainsKey("minorentry"))
			{
				WriteCssMinorEntry();
				m_dictClassData.Remove("minorentry");
			}
			if (m_dictClassData.TryGetValue("xitem", out var rgsLangs))
			{
				WriteCssXItem(rgsLangs);
				m_dictClassData.Remove("xitem");
			}
			m_writer.WriteLine();
			var xnDummy = new XElement("dummy", new XAttribute("style", string.Empty));
			foreach (var sClass in m_dictClassData.Keys)
			{
				// Check for actual FW style names (possibly munged) that appear in the
				// exported data, but aren't mapped to an XmlNode containing necessary
				// information.  Map them simply to the FW style.
				if (!m_mapCssClassToElement.TryGetValue(sClass, out _))
				{
					// We probably need to put this out anyway if it's an actual FW style name.
					string sStyle = null;
					if (m_styleTable.ContainsKey(sClass))
					{
						sStyle = sClass;
					}
					else if (m_styleTable.ContainsKey(sClass.Replace('_', ' ')))
					{
						sStyle = sClass.Replace('_', ' ');
					}
					if (!string.IsNullOrEmpty(sStyle))
					{
						xnDummy.Attribute("style").Value = sStyle;
						m_mapCssClassToElement.Add(sClass, xnDummy);
					}
				}
				ProcessDictionaryCssStyle(sClass);
			}
			WriteNumberStyles();
		}

		private void WriteCssBulletPara()
		{
			m_writer.WriteLine(".bulletpara {");
			m_writer.WriteLine("    display: block;");
			m_writer.WriteLine("}");

			m_writer.WriteLine(".bulletpara:before {");
			m_writer.WriteLine("    content: counter(sense, disc) \" \";");
			m_writer.WriteLine("    counter-increment: div;");
			m_writer.WriteLine("}");
		}

		private void WriteCssLetHead()
		{
			m_writer.WriteLine(".letHead {");
			m_writer.WriteLine("    column-count: 1;");
			m_writer.WriteLine("    clear: both;");
			m_writer.WriteLine("}");
		}

		private void WriteCssLetter()
		{
			m_writer.WriteLine(".letter {");
			m_writer.WriteLine("    text-align: center;");
			m_writer.WriteLine("    width: 100%;");
			m_writer.WriteLine("    margin-top: 18pt;");
			m_writer.WriteLine("    margin-bottom: 18pt;");
			WriteFontFamilyForWs(m_cache.DefaultVernWs, true);
			m_writer.WriteLine("    font-weight: bold;");
			m_writer.WriteLine("    font-size: 24pt;");
			m_writer.WriteLine("}");
		}

		private void WriteCssLetData()
		{
			m_writer.WriteLine(".letData {");
			m_writer.WriteLine("    column-count: 2;   -moz-column-count: 2;");
			m_writer.WriteLine("    column-gap: 1.5em; -moz-column-gap: 1.5em;");
			m_writer.WriteLine("    column-fill: balance;");
			m_writer.WriteLine("    text-align: {0};", m_fRTL ? "right" : "left");
			m_writer.WriteLine("}");
		}

		private void WriteCssEntry()
		{
			m_writer.WriteLine(".entry {");
			var esi = WriteFontInfoToCss(m_cache.DefaultAnalWs, "Dictionary-Normal", "entry");
			WriteParaStyleInfoToCss(esi, true, true); // LT-12658 allow to indent
			m_writer.WriteLine("    counter-reset: sense;");
			m_writer.WriteLine("}");
		}

		private void WriteCssComplexForm()
		{
			var fShowAsIndentedPara = false;
			if (m_mapCssClassToElement.TryGetValue("complexformrefs", out var xn))
			{
				fShowAsIndentedPara = XmlUtils.GetOptionalBooleanAttributeValue(xn, "showasindentedpara", false);
			}
			if (!fShowAsIndentedPara)
			{
				m_writer.WriteLine(".complexform {");
				var esi = WriteFontInfoToCss(m_cache.DefaultAnalWs, "Dictionary-Normal", "complexform");
				WriteParaStyleInfoToCss(esi);
				m_writer.WriteLine("}");
				return;
			}
			if (!m_mapCssClassToFwStyle.TryGetValue("complexformrefs", out var style))
			{
				style = XmlUtils.GetOptionalAttributeValue(xn, "style");
			}
			m_dictClassData.TryGetValue("complexformrefs", out var rgsLangs);
			m_mapCssClassToElement.Remove("complexformrefs");
			if (m_mapCssClassToFwStyle.ContainsKey("complexformrefs"))
			{
				m_mapCssClassToFwStyle.Remove("complexformrefs");
			}
			if (m_dictClassData.ContainsKey("complexformrefs"))
			{
				m_dictClassData.Remove("complexformrefs");
			}
			m_writer.WriteLine(".complexformrefs {");
			m_writer.WriteLine("    counter-reset: complexformpara;");
			m_writer.WriteLine("}");
			m_dictClassData["complexform"] = rgsLangs;
			m_mapCssClassToFwStyle["complexform"] = style;
			m_mapCssClassToElement["complexform"] = xn;
			ProcessDictionaryCssStyle("complexform");
		}

		private void WriteCssSubentry()
		{
			m_writer.WriteLine(".subentry {");
			string style = null;
			if (m_mapCssClassToElement.TryGetValue("subentries", out var xnSub))
			{
				style = XmlUtils.GetOptionalAttributeValue(xnSub, "parastyle");
			}
			if (string.IsNullOrEmpty(style))
			{
				style = "Dictionary-Subentry";
			}
			var esi = WriteFontInfoToCss(m_cache.DefaultAnalWs, style, "subentry");
			WriteParaStyleInfoToCss(esi);
			m_writer.WriteLine("}");
			WriteParaBulletInfoToCss(style, "subentry", "subentryCounter");
		}

		private void WriteCssMinorEntry()
		{
			m_writer.WriteLine(".minorentry {");
			string style = null;
			if (m_mapCssClassToElement.TryGetValue("minorentries", out var xnSub))
			{
				style = XmlUtils.GetOptionalAttributeValue(xnSub, "parastyle");
			}
			if (string.IsNullOrEmpty(style))
			{
				style = "Dictionary-Minor";
			}
			var esi = WriteFontInfoToCss(m_cache.DefaultAnalWs, style, "minorentry");
			WriteParaStyleInfoToCss(esi, true, true); // LT-12658 allow to indent
			m_writer.WriteLine("}");
			WriteParaBulletInfoToCss(style, "minorentry", "minorentryCounter");
			// Todo: more style details; see LT-12184
		}

		private void WriteCssXHomographNumber()
		{
			m_writer.WriteLine(".xhomographnumber {");
			WriteFontInfoToCss(m_cache.DefaultAnalWs, "Homograph-Number", "xhomographnumber");
			m_writer.WriteLine("}");
		}

		// Makes a distinct CSS style with the same properties as xhomographnumber; but the user can make it
		// different if desired. This is used for homograph number in a reference in a reversal entry.
		private void WriteCssHomographNumberRev()
		{
			m_writer.WriteLine(".homographnumberrev {");
			WriteFontInfoToCss(m_cache.DefaultAnalWs, "Homograph-Number", "homographnumberrev");
			m_writer.WriteLine("}");
		}

		private void WriteCssSense()
		{
			m_writer.WriteLine(".sense {");
			m_writer.WriteLine("    font-weight: normal;");
			m_writer.WriteLine("}");
		}

		private void WriteCssSensePara()
		{
			m_writer.WriteLine(".sensepara {");
			if (m_mapClassToStyle.TryGetValue("sensepara", out var style))
			{
				var esi = WriteFontInfoToCss(m_cache.DefaultAnalWs, style, "sensepara");
				WriteParaStyleInfoToCss(esi);
			}
			m_writer.WriteLine("}");
			WriteParaBulletInfoToCss(style, "sensepara", "sense");
		}

		private void WriteCssXLanguageTag()
		{
			m_writer.WriteLine(".xlanguagetag {");
			if (WriteFontInfoToCss(-1, "Writing System Abbreviation", "xlanguagetag") == null)
			{
				m_writer.WriteLine("    color: green;");
				m_writer.WriteLine("    font-size: x-small;");
			}
			m_writer.WriteLine("}");
		}

		private void WriteCssXItem(List<string> rgsLangs)
		{
			m_writer.WriteLine(".xitem {");
			m_writer.WriteLine("    /* placeholder for adding list separators */");
			m_writer.WriteLine("}");
			foreach (var sLang in rgsLangs)
			{
				var ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(sLang);
				if (ws != m_cache.DefaultAnalWs && ws != m_cache.DefaultVernWs)
				{
					m_writer.WriteLine(".xitem[lang='{0}'] {{", sLang);
					WriteFontInfoToCss(ws, "Dictionary-Normal", null);
					m_writer.WriteLine("}");
				}
			}
		}

		private void WriteCssXSenseNumber()
		{
			m_writer.WriteLine(".xsensenumber {");
			WriteFontInfoToCss(m_cache.DefaultAnalWs, "Sense-Reference-Number", "xsensenumber");
			m_writer.WriteLine("}");
		}

		private void WriteCssBold()
		{
			m_writer.WriteLine(".bold { font-weight:bold; }");
		}

		private void WriteCssXSenseXrefNumber()
		{
			m_writer.WriteLine(".xsensexrefnumber {");
			WriteFontInfoToCss(m_cache.DefaultAnalWs, "Sense-Reference-Number", "xsensexrefnumber");
			m_writer.WriteLine("}");
		}

		private void WriteCssXSenseNumberSub()
		{
			m_writer.WriteLine(".xsensenumber-sub {");
			WriteFontInfoToCss(m_cache.DefaultAnalWs, "Sense-Reference-Number", "xsensenumber-sub");
			m_writer.WriteLine("}");
		}

		private void WriteCssSenseNumberRef()
		{
			m_writer.WriteLine(".sensenumberref {");
			WriteFontInfoToCss(m_cache.DefaultAnalWs, "Sense-Reference-Number", "sensenumberref");
			m_writer.WriteLine("}");
		}

		private void WriteCssHeadref()
		{
			m_writer.WriteLine(".headref {");
			WriteFontInfoToCss(m_cache.DefaultAnalWs, "Dictionary-Headword", "headref");
			m_writer.WriteLine("}");
		}

		private void WriteCssHeadrefBefore()
		{
			m_writer.Write(".headref:before {content:\"");
			m_dictClassData.TryGetValue("headref-before", out var texts);
			m_writer.Write(texts[0]); // only one text in the list
			m_writer.WriteLine("\"}");
		}

		private void WriteCssHeadrefAfter()
		{
			m_writer.Write(".headref:after {content:\"");
			m_dictClassData.TryGetValue("headref-after", out var texts);
			m_writer.Write(texts[0]); // only one text in the list
			m_writer.WriteLine("\"}");
		}

		private void WriteCssCustomText()
		{
			m_writer.WriteLine(".customtext {");
			m_writer.WriteLine("    counter-reset: custompara;");
			m_writer.WriteLine("}");
		}

		private void WriteCssEmptyDefinition(string sClass)
		{
			m_writer.WriteLine(".{0} {{", sClass.Replace(' ', '_'));
			m_writer.WriteLine("}");
		}

		private void WriteCssPicture()
		{
			m_writer.WriteLine(".picture {");
			// limiting height works better than limiting width for multiple pictures
			m_writer.WriteLine("    height: 1.0in;");
			m_writer.WriteLine("}");
		}

		private void WriteCssPictureCenter()
		{
			m_writer.WriteLine(".pictureCenter {");
			m_writer.WriteLine("    float: center;");
			m_writer.WriteLine("    margin: 0pt 0pt 4pt 4pt;");
			m_writer.WriteLine("    padding: 2pt;");
			m_writer.WriteLine("    text-indent: 0pt;");
			m_writer.WriteLine("    text-align: center;");
			m_writer.WriteLine("}");
		}

		private void WriteCssPictureRight()
		{
			m_writer.WriteLine(".pictureRight {");
			m_writer.WriteLine("    float: right;");
			m_writer.WriteLine("    margin: 0pt 0pt 4pt 4pt;");
			m_writer.WriteLine("    padding: 2pt;");
			m_writer.WriteLine("    text-indent: 0pt;");
			m_writer.WriteLine("    text-align: center;");
			m_writer.WriteLine("}");
		}

		private void WriteSansSerifFontFamilyForWs(int ws, bool fWriteDirection)
		{
			var wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
			if (fWriteDirection)
			{
				m_writer.WriteLine("        direction: {0};", wsObj.RightToLeftScript ? "rtl" : "ltr");
			}
			if (!string.IsNullOrEmpty(wsObj.DefaultFontName))
			{
				m_writer.WriteLine("        font-family: \"{0}\", sans-serif;   /* default Serif font */", wsObj.DefaultFontName);
			}
		}

		private void WriteFontFamilyForWs(int ws, bool fWriteDirection)
		{
			var wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
			if (fWriteDirection)
			{
				m_writer.WriteLine("    direction: {0};", wsObj.RightToLeftScript ? "rtl" : "ltr");
			}
			if (!string.IsNullOrEmpty(wsObj.DefaultFontName))
			{
				m_writer.WriteLine("    font-family: \"{0}\", serif;	/* default Serif font */", wsObj.DefaultFontName);
			}
		}

		private void ReadStyles(IVwStylesheet vss)
		{
			var normalStyleName = vss.GetDefaultBasedOnStyleName();
			m_styleTable = new StyleInfoTable(normalStyleName, m_cache.ServiceLocator.WritingSystemManager);
			var cStyles = vss.CStyles;
			for (var i = 0; i < cStyles; ++i)
			{
				var hvo = vss.get_NthStyle(i);
				var sty = m_cache.ServiceLocator.GetInstance<IStStyleRepository>().GetObject(hvo);
				// CSS does not implement the kind of inheritance our styles use. To get the style
				// definitions we want in the CSS, we must create these styles using the 'net effect' of
				// each style and all the ones it is based on. Happily the VwStyleSheet knows exactly
				// how to do this.
				var props = vss.GetStyleRgch(sty.Name.Length, sty.Name);
				var exportStyleInfo = new ExportStyleInfo(sty, props);
				m_styleTable.Add(GetValidCssClassName(sty.Name), exportStyleInfo);
			}
		}

		private void ProcessDictionaryCssStyle(string sClass)
		{
			string sBaseClass;
			if (!m_mapCssClassToElement.TryGetValue(GetValidCssClassName(sClass), out var xn))
			{
				var idx = sClass.IndexOf("_L", StringComparison.Ordinal);
				if (idx <= 0)
				{
					return;
				}
				sBaseClass = sClass.Substring(0, idx);
				if (!m_mapCssClassToElement.TryGetValue(GetValidCssClassName(sBaseClass), out xn))
				{
					return;
				}
			}
			else
			{
				sBaseClass = sClass;
			}
			var sSep = XmlUtils.GetOptionalAttributeValue(xn, "sep");
			var sStyle = XmlUtils.GetOptionalAttributeValue(xn, "style");
			var fShowAsIndentedPara = XmlUtils.GetOptionalBooleanAttributeValue(xn, "showasindentedpara", false);
			var ws = 0;
			string sLang = null;
			if (m_dictClassData.TryGetValue(GetValidCssClassName(sClass), out var rgsLangs) && rgsLangs.Count > 0)
			{
				sLang = rgsLangs[0];
				ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(sLang);
			}
			if (ws == 0)
			{
				var sWs = StringServices.GetWsSpecWithoutPrefix(XmlUtils.GetOptionalAttributeValue(xn, "ws"));
				var sWsType = XmlUtils.GetOptionalAttributeValue(xn, "wsType");
				switch (sWs)
				{
					case "analysis":
						ws = m_cache.DefaultAnalWs;
						break;
					case "vernacular":
						ws = m_cache.DefaultVernWs;
						break;
					case "pronunciation":
						ws = m_cache.DefaultPronunciationWs;
						break;
					case "user":
						ws = m_cache.DefaultUserWs;
						break;
					case "all analysis":
						ws = m_cache.DefaultAnalWs;
						break;
					case "all vernacular":
						ws = m_cache.DefaultVernWs;
						break;
					case "":
					case null:
						ws = m_cache.DefaultUserWs; // need something!
						break;
					default:
						var rgsWs = sWs.Split(',');
						if (rgsWs.Length > 0)
						{
							ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(rgsWs[0]);
						}
						if (ws == 0)
						{
							switch (sWsType)
							{
								case "analysis":
									ws = m_cache.DefaultAnalWs;
									break;
								case "vernacular":
									ws = m_cache.DefaultVernWs;
									break;
								case "pronunciation":
									ws = m_cache.DefaultPronunciationWs;
									break;
								default:
									ws = m_cache.DefaultUserWs;     // need something!
									break;
							}
						}
						break;
				}
			}
			m_writer.WriteLine(".{0} {{", sClass.Replace(' ', '_'));
			if (!string.IsNullOrEmpty(sLang))
			{
				m_writer.Write("    /* lang = '{0}'", sLang);
				for (var i = 1; i < rgsLangs.Count; ++i)
				{
					m_writer.Write(", '{0}'", rgsLangs[i]);
				}
				m_writer.WriteLine(" */");
			}
			if (!string.IsNullOrEmpty(sStyle))
			{
				m_writer.WriteLine("    /* explicit FieldWorks style = '{0}' */", sStyle);
				var esi = WriteFontInfoToCss(ws, sStyle, sBaseClass);
				if (esi != null)
				{
					WriteParaStyleInfoToCss(esi);
				}
			}
			else if (ws != m_cache.DefaultAnalWs && m_mapCssToStyleEnv.TryGetValue(sBaseClass, out var rgsStyles) && rgsStyles.Count > 0)
			{
				m_writer.WriteLine("    /* cascaded environment FieldWorks style = '{0}' */", rgsStyles[0]);
				var esi = WriteFontInfoToCss(ws, rgsStyles[0], null);
				if (esi != null)
				{
					WriteParaStyleInfoToCss(esi);
				}
			}
			else if (xn != null && XmlUtils.GetOptionalBooleanAttributeValue(xn, "indent", false))
			{
				var parastyle = XmlUtils.GetOptionalAttributeValue(xn, "parastyle");
				if (!string.IsNullOrEmpty(parastyle))
				{
					if (m_styleTable.TryGetValue(GetValidCssClassName(parastyle), out var bsi))
					{
						if (bsi is ExportStyleInfo esi)
						{
							WriteHorizontalPaddingValues(esi, true, out _, out _); // LT-12658 allow to indent
						}
					}
				}
			}
			if (sClass == "subentries")
			{
				m_writer.WriteLine("    counter-reset: subentryCounter;");
			}
			var sClassLowered = sClass.ToLowerInvariant();
			if (sClassLowered.Contains("headword") && !sClassLowered.Contains("sub"))
			{
				m_writer.WriteLine("    string-set: guideword content();");
			}
			m_writer.WriteLine("}");
			if (!fShowAsIndentedPara)
			{
				if (!string.IsNullOrEmpty(sSep))
				{
					switch (sClass)
					{
						case "senses":
						case "senses-minor":
						case "senses-sub":
						case "subsenses":
						case "subsenses-minor":
						case "subsenses-sub":
							m_writer.WriteLine(".{0}>.sense + .sense:before {{ content: \"{1}\" }}", sClass, EscapeCharsForCss(sSep));
							break;
						default:
							m_writer.WriteLine(".{0}>.xitem + .xitem:before {{ content: \"{1}\" }}", sClass, EscapeCharsForCss(sSep));
							break;
					}
				}
			}
			if (!string.IsNullOrEmpty(sStyle))
			{
				var sCounter = "custompara";
				if (sClass == "complexform")
				{
					sCounter = "complexformpara";
				}
				WriteParaBulletInfoToCss(sStyle, sClass, sCounter);
			}
		}

		private void WriteParaBulletInfoToCss(string sStyle, string sClass, string sCounter)
		{
			if (!m_styleTable.TryGetValue(GetValidCssClassName(sStyle), out var bsi))
			{
				return;
			}
			if (bsi.IsCharacterStyle)
			{
				return;
			}
			if (!(bsi is ExportStyleInfo esi))
			{
				return;
			}
			var scheme = esi.NumberScheme;
			if (scheme < VwBulNum.kvbnNumberBase || scheme > VwBulNum.kvbnBulletMax)
			{
				return;
			}
			var type = string.Empty;
			string bullet = null;
			switch (scheme)
			{
				case VwBulNum.kvbnArabic: type = ", decimal"; break;
				case VwBulNum.kvbnArabic01: type = ", decimal-leading-zero"; break;
				case VwBulNum.kvbnRomanLower: type = ", lower-roman"; break;
				case VwBulNum.kvbnRomanUpper: type = ", upper-roman"; break;
				case VwBulNum.kvbnLetterLower: type = ", lower-latin"; break;
				case VwBulNum.kvbnLetterUpper: type = ", upper-latin"; break;
				case VwBulNum.kvbnBulletBase + 0: bullet = "\\00B7"; break;     // MIDDLE DOT
				case VwBulNum.kvbnBulletBase + 1: bullet = "\\2022"; break;     // BULLET (note: in a list item, consider using 'disc' somehow?)
				case VwBulNum.kvbnBulletBase + 2: bullet = "\\25CF"; break;     // BLACK CIRCLE
				case VwBulNum.kvbnBulletBase + 3: bullet = "\\274D"; break;     // SHADOWED WHITE CIRCLE
				case VwBulNum.kvbnBulletBase + 4: bullet = "\\25AA"; break;     // BLACK SMALL SQUARE (note: in a list item, consider using 'square' somehow?)
				case VwBulNum.kvbnBulletBase + 5: bullet = "\\25A0"; break;     // BLACK SQUARE
				case VwBulNum.kvbnBulletBase + 6: bullet = "\\25AB"; break;     // WHITE SMALL SQUARE
				case VwBulNum.kvbnBulletBase + 7: bullet = "\\25A1"; break;     // WHITE SQUARE
				case VwBulNum.kvbnBulletBase + 8: bullet = "\\2751"; break;     // LOWER RIGHT SHADOWED WHITE SQUARE
				case VwBulNum.kvbnBulletBase + 9: bullet = "\\2752"; break;     // UPPER RIGHT SHADOWED WHITE SQUARE
				case VwBulNum.kvbnBulletBase + 10: bullet = "\\2B27"; break;    // BLACK MEDIUM LOZENGE
				case VwBulNum.kvbnBulletBase + 11: bullet = "\\29EB"; break;    // BLACK LOZENGE
				case VwBulNum.kvbnBulletBase + 12: bullet = "\\25C6"; break;    // BLACK DIAMOND
				case VwBulNum.kvbnBulletBase + 13: bullet = "\\2756"; break;    // BLACK DIAMOND MINUS WHITE X
				case VwBulNum.kvbnBulletBase + 14: bullet = "\\2318"; break;    // PLACE OF INTEREST SIGN
				case VwBulNum.kvbnBulletBase + 15: bullet = "\\261E"; break;    // WHITE RIGHT POINTING INDEX
				case VwBulNum.kvbnBulletBase + 16: bullet = "\\271D"; break;    // LATIN CROSS
				case VwBulNum.kvbnBulletBase + 17: bullet = "\\271E"; break;    // SHADOWED WHITE LATIN CROSS
				case VwBulNum.kvbnBulletBase + 18: bullet = "\\2730"; break;    // SHADOWED WHITE STAR
				case VwBulNum.kvbnBulletBase + 19: bullet = "\\27A2"; break;    // THREE-D TOP-LIGHTED RIGHTWARDS ARROWHEAD
				case VwBulNum.kvbnBulletBase + 20: bullet = "\\27B2"; break;    // CIRCLED HEAVY WHITE RIGHTWARDS ARROW
				case VwBulNum.kvbnBulletBase + 21: bullet = "\\2794"; break;    // HEAVY WIDE-HEADED RIGHTWARDS ARROW
				case VwBulNum.kvbnBulletBase + 22: bullet = "\\2794"; break;    // HEAVY WIDE-HEADED RIGHTWARDS ARROW
				case VwBulNum.kvbnBulletBase + 23: bullet = "\\21E8"; break;    // RIGHTWARDS WHITE ARROW
				case VwBulNum.kvbnBulletBase + 24: bullet = "\\2713"; break;    // CHECK MARK
				default:
					if (scheme >= VwBulNum.kvbnBulletBase && scheme <= VwBulNum.kvbnBulletMax)
					{
						bullet = "*";
					}
					break;
			}
			m_writer.WriteLine(".{0}:before {{", sClass.Replace(' ', '_'));
			if (bullet != null)
			{
				m_writer.WriteLine("    content: \"{0}  \";", bullet);
			}
			else
			{
				m_writer.WriteLine("    content: counter({0}{1}) \" \";", sCounter, type);
			}
			m_writer.WriteLine("    counter-increment: {0};", sCounter);
			m_writer.WriteLine("}");
		}

		private ExportStyleInfo WriteFontInfoToCss(int ws, string sStyle, string sCss)
		{
			if (!m_styleTable.TryGetValue(GetValidCssClassName(sStyle), out var bsi))
			{
				return null;
			}
			var esi = (ExportStyleInfo)bsi;
			if (!WriteFontAttr(ws, "font-family", esi, sCss, true) && ws != -1)
			{
				if (!WriteFontAttr(-1, "font-family", esi, sCss, true))
				{
					WriteFontFamilyForWs(ws, false);
				}
			}
			if (!WriteFontAttr(ws, "font-size", esi, sCss, true) && ws != -1)
			{
				if (!WriteFontAttr(-1, "font-size", esi, sCss, true))
				{
					switch (sStyle)
					{
						case "Verse Number":
							m_writer.WriteLine("    font-size: smaller;");
							break;
						case "Chapter Number":
							m_writer.WriteLine("    font-size: 200%;");
							break;
					}
				}
			}
			if (!WriteFontAttr(ws, "font-weight", esi, sCss, true) && ws != -1)
			{
				if (!WriteFontAttr(-1, "font-weight", esi, sCss, true))
				{
					if (sStyle == "Chapter Number")
					{
						m_writer.WriteLine("    font-weight: bolder;");
					}
				}
			}
			if (!WriteFontAttr(ws, "font-style", esi, sCss, true) && ws != -1)
			{
				WriteFontAttr(-1, "font-style", esi, sCss, true);
			}
			if (!WriteFontAttr(ws, "color", esi, sCss, true) && ws != -1)
			{
				WriteFontAttr(-1, "color", esi, sCss, true);
			}
			if (!WriteFontAttr(ws, "background-color", esi, sCss, true) && ws != -1)
			{
				WriteFontAttr(-1, "background-color", esi, sCss, true);
			}
			if (!WriteFontAttr(ws, "vertical-align", esi, sCss, true) && ws != -1)
			{
				if (!WriteFontAttr(-1, "vertical-align", esi, sCss, true))
				{
					switch (sStyle)
					{
						case "Verse Number":
							m_writer.WriteLine("    vertical-align: super;");
							break;
						case "Chapter Number":
							m_writer.WriteLine("    vertical-align: top;");
							break;
					}
				}
			}
			if (!WriteFontAttr(ws, "text-decoration", esi, sCss, true) && ws != -1)
			{
				WriteFontAttr(-1, "text-decoration", esi, sCss, true);
			}
			if (esi.DirectionIsRightToLeft != TriStateBool.triNotSet)
			{
				m_writer.WriteLine("    direction: {0};",
					esi.DirectionIsRightToLeft == TriStateBool.triTrue ? "rtl" : "ltr");
			}
			return bsi as ExportStyleInfo; // in case someone else needs it.

		}

		private bool WriteFontAttr(int ws, string sAttr, ExportStyleInfo esi, string sCss, bool fTopLevel)
		{
			var fi = esi.FontInfoForWs(ws);
			var fInherited = false;
			var sInheritance = string.Empty;
			if (sCss == null && !fTopLevel)
			{
				sInheritance = "\t/* inherited through cascaded environment */";
			}
			if (sCss == null)
			{
				sInheritance = "\t/* cascaded environment */";
			}
			else if (!fTopLevel)
			{
				sInheritance = "\t/* inherited */";
			}
			switch (sAttr)
			{
				case "font-family":
					if (fi.m_fontName.ValueIsSet)
					{
						var sFontName = esi.RealFontNameForWs(ws);
						m_writer.WriteLine("    font-family: \"{0}\", serif;{1}", string.IsNullOrEmpty(sFontName) ? fi.m_fontName.Value : sFontName, sInheritance);
						return true;
					}
					fInherited = fi.m_fontName.IsInherited;
					break;
				case "font-size":
					var superSub = fi.m_superSub.ValueIsSet && fi.m_superSub.Value != FwSuperscriptVal.kssvOff;
					if (fi.m_fontSize.ValueIsSet)
					{
						var pointSize = fi.m_fontSize.Value;
						if (superSub)
						{
							pointSize = pointSize * 55 / 100;
						}
						m_writer.WriteLine("    font-size: {0}pt;{1}", ConvertMptToPt(pointSize), sInheritance);
						return true;
					}
					if (superSub)
					{
						m_writer.WriteLine("    font-size: 55%;{0}", sInheritance);
						return true;
					}
					fInherited = fi.m_fontSize.IsInherited;
					break;
				case "font-weight":
					if (fi.m_bold.ValueIsSet)
					{
						m_writer.WriteLine("    font-weight: {0};{1}", fi.m_bold.Value ? "bold" : "normal", sInheritance);
						return true;
					}
					fInherited = fi.m_bold.IsInherited;
					break;
				case "font-style":
					if (fi.m_italic.ValueIsSet)
					{
						m_writer.WriteLine("    font-style: {0};{1}", fi.m_italic.Value ? "italic" : "normal", sInheritance);
						return true;
					}
					fInherited = fi.m_italic.IsInherited;
					break;
				case "color":
					if (fi.m_fontColor.ValueIsSet)
					{
						//Black as a font color is a default setting, we will ignore it (LT-10891)
						//however, if it is explicitly set we need to include it.
						if (fi.m_fontColor.Value != Color.Black || fi.m_fontColor.IsExplicit)
						{
							m_writer.WriteLine("    color: rgb({0},{1},{2});{3}",
								fi.m_fontColor.Value.R,
								fi.m_fontColor.Value.G,
								fi.m_fontColor.Value.B,
								sInheritance);
						}
						return true;
					}
					fInherited = fi.m_fontColor.IsInherited;
					break;
				case "background-color":
					if (fi.m_backColor.ValueIsSet)
					{
						//White as a background color is a default setting, we will ignore it (LT-10891)
						//however, if it is explicitly set we need to include it.
						if (fi.m_backColor.Value != Color.White || fi.m_fontColor.IsExplicit)
						{
							m_writer.WriteLine("    background-color: rgb({0},{1},{2});{3}",
								fi.m_backColor.Value.R,
								fi.m_backColor.Value.G,
								fi.m_backColor.Value.B,
								sInheritance);
						}
						return true;
					}
					fInherited = fi.m_backColor.IsInherited;
					break;
				case "vertical-align":
					if (fi.m_superSub.ValueIsSet)
					{
						m_writer.WriteLine("    vertical-align: {0};{1}", GetVerticalAlign(fi.m_superSub.Value), sInheritance);
						return true;
					}
					if (fi.m_offset.ValueIsSet)
					{
						m_writer.WriteLine("    vertical-align: {0}pt;{1}", ConvertMptToPt(fi.m_offset.Value), sInheritance);
						return true;
					}
					fInherited = fi.m_offset.IsInherited || fi.m_superSub.IsInherited;
					break;
				case "text-decoration":
					if (fi.m_underline.ValueIsSet)
					{
						m_writer.WriteLine("    text-decoration: {0};{1}", fi.m_underline.Value == FwUnderlineType.kuntNone ? "none" : "underline", sInheritance);
						return true;
					}
					fInherited = fi.m_underline.IsInherited;
					break;
			}
			if (fInherited)
			{
				var sBaseStyle = esi.InheritsFrom;
				if (!string.IsNullOrEmpty(sBaseStyle) && m_styleTable.TryGetValue(GetValidCssClassName(sBaseStyle), out var bsiBase))
				{
					if (WriteFontAttr(ws, sAttr, bsiBase as ExportStyleInfo, sCss, false))
					{
						return true;
					}
				}
				if (!string.IsNullOrEmpty(sCss))
				{
					if (m_mapCssToStyleEnv.TryGetValue(sCss, out var rgsStyles))
					{
						foreach (var sParaStyle in rgsStyles)
						{
							if (m_styleTable.TryGetValue(GetValidCssClassName(sParaStyle), out var bsiPara) && WriteFontAttr(ws, sAttr, bsiPara as ExportStyleInfo, null, false))
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		private void WriteParaStyleInfoToCss(ExportStyleInfo esi)
		{
			WriteParaStyleInfoToCss(esi, AllowDictionaryParagraphIndent);
		}

		private void WriteParaStyleInfoToCss(ExportStyleInfo esi, bool hangingIndent, bool entryOrMinorEntry = false)
		{
			if (esi == null)
			{
				return; // If the style was not defined in our stylesheet, we can't write anything for it.
			}
			WriteHorizontalPaddingValues(esi, hangingIndent, out var sLeading, out var sTrailing);
			if (esi.HasAlignment)
			{
				switch (esi.Alignment)
				{
					case FwTextAlign.ktalCenter:
						m_writer.WriteLine("    text-align: center;");
						break;
					case FwTextAlign.ktalJustify:
						m_writer.WriteLine("    text-align: justify;");
						break;
					case FwTextAlign.ktalLeft:
						m_writer.WriteLine("    text-align: left;");
						break;
					case FwTextAlign.ktalRight:
						m_writer.WriteLine("    text-align: right;");
						break;
					case FwTextAlign.ktalLeading:
						m_writer.WriteLine("    /*text-align: leading;*/");
						m_writer.WriteLine("    text-align: {0};", sLeading);
						break;
					case FwTextAlign.ktalTrailing:
						m_writer.WriteLine("    /*text-align: trailing;*/");
						m_writer.WriteLine("    text-align: {0};", sTrailing);
						break;
				}
			}
			if (esi.HasBorder)
			{
				m_writer.WriteLine("    border-style: solid;");
				m_writer.WriteLine("    border-top-width: {0}pt;", ConvertMptToPt(esi.BorderTop));
				m_writer.WriteLine("    border-bottom-width: {0}pt;", ConvertMptToPt(esi.BorderBottom));
				m_writer.WriteLine("    border-left-width: {0}pt;", ConvertMptToPt(esi.BorderLeading));
				m_writer.WriteLine("    border-right-width: {0}pt;", ConvertMptToPt(esi.BorderTrailing));
			}
			if (esi.HasBorderColor)
			{
				m_writer.WriteLine("    border-color: rgb({0},{1},{2});", esi.BorderColor.R, esi.BorderColor.G, esi.BorderColor.B);
			}
			if (hangingIndent)
			{
				// Indent is allowed, write it out if specified; otherwise, allow it to be inherited.
				if (esi.HasFirstLineIndent)
				{
					var firstLineIndentAdjusted = esi.FirstLineIndent;
					if (entryOrMinorEntry) //LT-14757 Need to reduce indent for cell phone devices.
					{
						firstLineIndentAdjusted = firstLineIndentAdjusted / 3;
					}
					m_writer.WriteLine("    text-indent: {0}pt;", ConvertMptToPt(firstLineIndentAdjusted));
					m_writer.Write("    margin-{0}: ", sLeading);
					if (esi.FirstLineIndent < 0)
					{
						m_writer.WriteLine("{0}pt;", ConvertMptToPt(-firstLineIndentAdjusted));
					}
					else
					{
						m_writer.WriteLine("0pt;");
					}
				}
			}
			else
			{
				// LT-12658 suppress indentation (even inherited) inside paragraphs (entries).
				m_writer.WriteLine("    text-indent: 0pt;");
				m_writer.WriteLine("    margin-{0}: 0pt;", sLeading);
			}
			if (esi.HasLineSpacing)
			{
				var lhi = esi.LineSpacing;
				if (lhi.m_relative)
				{
					m_writer.WriteLine("    line-height: {0}%;", lhi.m_lineHeight / 100);
				}
				else if (lhi.m_lineHeight < 0)
				{
					m_writer.WriteLine("    line-height: {0}pt;", ConvertMptToPt(-lhi.m_lineHeight));
				}
				else
				{
					m_writer.WriteLine("    line-height: normal;"); // "at least" semantics??
				}
			}
			if (esi.HasSpaceAfter)
			{
				m_writer.WriteLine("    padding-bottom: {0}pt;", ConvertMptToPt(esi.SpaceAfter));
			}
			if (esi.HasSpaceBefore)
			{
				m_writer.WriteLine("    padding-top: {0}pt;", ConvertMptToPt(esi.SpaceBefore));
			}
			if (esi.HasWidowOrphanControl)
			{
				m_writer.WriteLine("    orphans: {0};", esi.WidowOrphanControl ? "2" : "1");
				m_writer.WriteLine("    widows: {0};", esi.WidowOrphanControl ? "2" : "1");
			}
		}

		private void WriteHorizontalPaddingValues(ExportStyleInfo esi, bool hangingIndent, out string sLeading, out string sTrailing)
		{
			sLeading = esi.DirectionIsRightToLeft == TriStateBool.triTrue ? "right" : "left";
			sTrailing = esi.DirectionIsRightToLeft == TriStateBool.triTrue ? "left" : "right";
			if (esi.HasLeadingIndent && hangingIndent) // LT-12658 suppress indentation inside paragraphs
			{
				m_writer.WriteLine("    padding-{0}: {1}pt;", sLeading, ConvertMptToPt(esi.LeadingIndent));
			}
			if (esi.HasTrailingIndent)
			{
				m_writer.WriteLine("    padding-{0}: {1}pt;", sTrailing, ConvertMptToPt(esi.TrailingIndent));
			}
		}

		private static string GetVerticalAlign(FwSuperscriptVal super)
		{
			switch (super)
			{
				case FwSuperscriptVal.kssvSub:
					return "sub";
				case FwSuperscriptVal.kssvSuper:
					return "super";
				default:
					return "baseline";
			}
		}

		private static string ConvertMptToPt(int mpt)
		{
			var pt = mpt / 1000;
			var frac = Math.Abs(mpt) % 1000;
			return frac == 0 ? pt.ToString() : $"{pt}.{frac:d3}";
		}

		private static string EscapeCharsForCss(string sBefore)
		{
			return sBefore.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\00000d").Replace("\n", "\\00000a");
		}

		private void ProcessNotebookTypeClasses()
		{
			if (m_dictClassData.ContainsKey("notebookBody"))
			{
				WriteCssEmptyDefinition("notebookBody");
				m_dictClassData.Remove("notebookBody");
			}
			// entry
			if (m_dictClassData.ContainsKey("entry"))
			{
				WriteCssNotebookEntry();
				m_dictClassData.Remove("entry");
			}
			if (m_dictClassData.ContainsKey("xlanguagetag"))
			{
				WriteCssXLanguageTag();
				m_dictClassData.Remove("xlanguagetag");
			}
			List<string> rgsLangs;
			if (m_dictClassData.TryGetValue("xitem", out rgsLangs))
			{
				WriteCssXItem(rgsLangs);
				m_dictClassData.Remove("xitem");
			}
			m_writer.WriteLine();
			var styleAttr = new XAttribute("style", string.Empty);
			var xnDummy = new XElement("dummy", styleAttr);
			foreach (var sClass in m_dictClassData.Keys)
			{
				// Check for actual FW style names (possibly munged) that appear in the
				// exported data, but aren't mapped to an XmlNode containing necessary
				// information.  Map them simply to the FW style.
				if (m_dictClassData[sClass].Count > 0 && !m_mapCssClassToElement.TryGetValue(sClass, out _))
				{
					// We probably need to put this out anyway if it's an actual FW style name.
					string sStyle = null;
					if (m_styleTable.ContainsKey(sClass))
					{
						sStyle = sClass;
					}
					else if (m_styleTable.ContainsKey(sClass.Replace('_', ' ')))
					{
						sStyle = sClass.Replace('_', ' ');
					}
					if (!string.IsNullOrEmpty(sStyle))
					{
						styleAttr.Value = sStyle;
						m_mapCssClassToElement.Add(sClass, xnDummy);
					}
				}
				ProcessNotebookCssStyle(sClass);
			}
		}

		/// <summary>
		/// We don't really have a specific style to use here, so just copy something innocuous.
		/// </summary>
		private void WriteCssNotebookEntry()
		{
			m_writer.WriteLine(".entry {");
			var esi = WriteFontInfoToCss(m_cache.DefaultAnalWs, "Normal", "entry");
			WriteParaStyleInfoToCss(esi);
			m_writer.WriteLine("}");

		}

		private void ProcessNotebookCssStyle(string sClass)
		{
			string sBaseClass;
			if (!m_mapCssClassToElement.TryGetValue(GetValidCssClassName(sClass), out var xn))
			{
				var idx = sClass.IndexOf("_L", StringComparison.Ordinal);
				if (idx <= 0)
				{
					return;
				}
				sBaseClass = sClass.Substring(0, idx);
				if (!m_mapCssClassToElement.TryGetValue(GetValidCssClassName(sBaseClass), out xn))
				{
					return;
				}
			}
			else
			{
				sBaseClass = sClass;
			}
			var sBefore = XmlUtils.GetOptionalAttributeValue(xn, "before");
			var sAfter = XmlUtils.GetOptionalAttributeValue(xn, "after");
			var sSep = XmlUtils.GetOptionalAttributeValue(xn, "sep");
			var sStyle = XmlUtils.GetOptionalAttributeValue(xn, "style");
			var ws = 0;
			string sLang = null;
			if (m_dictClassData.TryGetValue(GetValidCssClassName(sClass), out var rgsLangs) && rgsLangs.Count > 0)
			{
				sLang = rgsLangs[0];
				ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(sLang);
			}
			if (ws == 0)
			{
				var sWs = StringServices.GetWsSpecWithoutPrefix(XmlUtils.GetOptionalAttributeValue(xn, "ws"));
				var sWsType = XmlUtils.GetOptionalAttributeValue(xn, "wsType");
				switch (sWs)
				{
					case "analysis":
						ws = m_cache.DefaultAnalWs;
						break;
					case "vernacular":
						ws = m_cache.DefaultVernWs;
						break;
					case "pronunciation":
						ws = m_cache.DefaultPronunciationWs;
						break;
					case "user":
						ws = m_cache.DefaultUserWs;
						break;
					case "all analysis":
						ws = m_cache.DefaultAnalWs;
						break;
					case "all vernacular":
						ws = m_cache.DefaultVernWs;
						break;
					case "":
					case null:
						ws = m_cache.DefaultUserWs; // need something!
						break;
					default:
						var rgsWs = sWs.Split(',');
						if (rgsWs.Length > 0)
						{
							ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(rgsWs[0]);
						}
						if (ws == 0)
						{
							switch (sWsType)
							{
								case "analysis":
									ws = m_cache.DefaultAnalWs;
									break;
								case "vernacular":
									ws = m_cache.DefaultVernWs;
									break;
								case "pronunciation":
									ws = m_cache.DefaultPronunciationWs;
									break;
								default:
									ws = m_cache.DefaultUserWs;     // need something!
									break;
							}
						}
						break;
				}
			}
			m_writer.WriteLine(".{0} {{", sClass.Replace(' ', '_'));
			if (!string.IsNullOrEmpty(sLang))
			{
				m_writer.Write("    /* lang = '{0}'", sLang);
				for (var i = 1; i < rgsLangs.Count; ++i)
				{
					m_writer.Write(", '{0}'", rgsLangs[i]);
				}
				m_writer.WriteLine(" */");
			}
			if (!string.IsNullOrEmpty(sStyle))
			{
				m_writer.WriteLine("    /* explicit FieldWorks style = '{0}' */", sStyle);
				WriteFontInfoToCss(ws, sStyle, sBaseClass);
			}
			else if (ws != m_cache.DefaultAnalWs && m_mapCssToStyleEnv.TryGetValue(sBaseClass, out var rgsStyles) && rgsStyles.Count > 0)
			{
				m_writer.WriteLine("    /* cascaded environment FieldWorks style = '{0}' */", rgsStyles[0]);
				WriteFontInfoToCss(ws, rgsStyles[0], null);
			}
			var sClassLowered = sClass.ToLowerInvariant();
			if (sClassLowered.Contains("headword") && !sClassLowered.Contains("sub"))
			{
				m_writer.WriteLine("    string-set: guideword content();");
			}
			m_writer.WriteLine("}");
			if (!string.IsNullOrEmpty(sBefore))
			{
				m_writer.WriteLine(".{0}:before {{ content: \"{1}\" }}", sClass, EscapeCharsForCss(sBefore));
			}
			if (!string.IsNullOrEmpty(sAfter))
			{
				m_writer.WriteLine(".{0}:after {{ content: \"{1}\" }}", sClass, EscapeCharsForCss(sAfter));
			}
			if (!string.IsNullOrEmpty(sSep))
			{
				m_writer.WriteLine(".{0}>.xitem + .xitem:before {{ content: \"{1}\" }}", sClass, EscapeCharsForCss(sSep));
			}
			else if (sClass == "subentries")
			{
				// We need to do something to cause indentation.
				m_writer.WriteLine(".{0}>.entry + .entry {{ text-indent: 6% }}", sClass);
			}
		}

		private void WriteNumberStyles()
		{
			foreach (var numberStyle in NumberStyles)
			{
				var sClass = numberStyle.Key;
				var sBefore = numberStyle.Value.Item1;
				var sAfter = numberStyle.Value.Item2;
				if (!string.IsNullOrEmpty(sBefore))
				{
					m_writer.WriteLine(".{0}:before {{ content: \"{1}\" }}", sClass, EscapeCharsForCss(sBefore));
				}
				if (!string.IsNullOrEmpty(sAfter))
				{
					m_writer.WriteLine(".{0}:after {{ content: \"{1}\" }}", sClass, EscapeCharsForCss(sAfter));
				}

			}
		}
		/// <summary>
		/// XSLT processors do too much with xmlns attributes, so we need to add it here to the
		/// html element.  We also need to retrieve the class and lang attributes that result
		/// from the XSLT processing, so we do that as well.
		/// </summary>
		public void FinalizeXhtml(string sOutputFile, string sTempFile)
		{
			using (var rdr = FileUtils.OpenFileForRead(sTempFile, Encoding.UTF8))
			using (var wtr = FileUtils.OpenFileForWrite(sOutputFile, Encoding.UTF8))
			{
				var sLine = rdr.ReadLine();
				while (sLine != null)
				{
					// Users expect both these characters to cause a break before the next element, but embedding
					// them in HTML is considered bad practice (LT-13592) so we replace with a break.
					sLine = sLine.Replace("\u2028", "<br/>");
					sLine = sLine.Replace("\u2029", "<br/>");
					var idxClass = -1;
					while ((idxClass = sLine.IndexOf(" class=\"", idxClass + 1, StringComparison.Ordinal)) >= 0)
					{
						var idxMin = idxClass + 8;
						var idxLim = sLine.IndexOf('"', idxMin);
						if (idxLim > idxMin)
						{
							var sClass = sLine.Substring(idxMin, idxLim - idxMin);
							string sLang = null;
							if (sLine.Substring(idxLim)
								.StartsWith("\" lang=\"", StringComparison.Ordinal))
							{
								var idxLang = idxLim + 8;
								var idxLangLim = sLine.IndexOf('"', idxLang);
								if (idxLangLim > idxLang)
								{
									sLang = sLine.Substring(idxLang, idxLangLim - idxLang);
								}
							}
							else if (sLine.Substring(idxLim).StartsWith("\"><span ") &&
									 idxClass - 4 >= 0 && sLine.Substring(idxClass - 4)
										 .StartsWith("<div "))
							{
								var piece = sLine.Substring(idxLim + 2);
								var idx = piece.IndexOf(">", StringComparison.Ordinal);
								if (idx > 0)
								{
									piece = piece.Remove(idx + 1);
									idx = piece.IndexOf(" lang=\"", StringComparison.Ordinal);
									if (idx > 0)
									{
										var idxLang = idx + 7;
										var idxLangLim = piece.IndexOf('"', idxLang);
										if (idxLangLim > idxLang)
										{
											sLang = piece.Substring(idxLang,
												idxLangLim - idxLang);
										}
									}
								}
							}
							else if (idxClass - 4 >= 0 &&
									 sLine.Substring(idxClass - 4).StartsWith("<div ") &&
									 sLine.Substring(idxLim).StartsWith("\" id=\""))
							{
								var idx = sLine.IndexOf(">", idxLim, StringComparison.Ordinal);
								if (idx > 0)
								{
									var piece = sLine.Substring(idxLim, idx - idxLim);
									if (piece.Contains(" style=\""))
									{
										idx = sLine.IndexOf(" style=\"", idxLim,
											StringComparison.Ordinal);
										var idxStyle = idx + 8;
										var idxStyleLim = sLine.IndexOf('"', idxStyle);
										var style = sLine.Substring(idx + 8,
											idxStyleLim - idxStyle);
										sLine = sLine.Remove(idx, idxStyleLim - idx + 1);
										if (!m_mapClassToStyle.ContainsKey(sClass))
										{
											m_mapClassToStyle.Add(sClass, style);
										}
									}
								}
							}
							if (!m_dictClassData.TryGetValue(GetValidCssClassName(sClass),
								out var rgsLangs))
							{
								// Many TE styles have spaces in their names, which are
								// replaced by underscores.  Try finding the original name
								// before inserting a new value into the table.
								if (!m_mapCssClassToFwStyle.TryGetValue(sClass, out var sFwStyle))
								{
									sFwStyle = sClass.Replace('_', ' '); // just in case...
								}

								if (!m_dictClassData.TryGetValue(GetValidCssClassName(sFwStyle),
									out rgsLangs))
								{
									var before = ExtractText(sLine, idxLim, " before", "\"");
									var after = ExtractText(sLine, idxLim, " after", "\"");
									if (!string.IsNullOrEmpty(before) ||
										!string.IsNullOrEmpty(after))
									{
										if (!string.IsNullOrEmpty(before))
										{
											// sorry, literal text not a language
											MapCssToLangs(sClass, new List<string> { before });
										}

										if (!string.IsNullOrEmpty(after))
										{
											// sorry, literal text not a language
											MapCssToLangs(sClass, new List<string> { after });
										}
									}
									else
									{
										rgsLangs = new List<string>();
										MapCssToLangs(sClass, rgsLangs);
									}
								}
							}
							if (!string.IsNullOrEmpty(sLang) && !rgsLangs.Contains(sLang))
							{
								rgsLangs.Add(sLang);
							}
						}
					}
					sLine = InsertHtmlNamespace(sLine);
					wtr.WriteLine(sLine);
					sLine = rdr.ReadLine();
				}
			}
		}

		/// <summary>
		/// If this line contains the opening html attribute, add the required namespace info.
		/// XSLT processors do too much with xmlns attributes, so we generally add it after the final xslt.
		/// </summary>
		public static string InsertHtmlNamespace(string sLine)
		{
			var idxHtml = sLine.IndexOf("<html ", StringComparison.Ordinal);
			if (idxHtml >= 0)
			{
				sLine = sLine.Insert(idxHtml + 5, " xmlns=\"http://www.w3.org/1999/xhtml\"");
			}
			return sLine;
		}

		/// <summary>
		/// Extracts labeled text between two delimiters
		/// ex:  str = 'stuff ... attrname = "delimited text" ... '
		/// use: delimitedText = ExtractText(str, idxLim, "attrname", "\"");
		/// </summary>
		private static string ExtractText(string str, int fromHere, string label, string delimiter)
		{
			var horizon = 30;
			if (fromHere > str.Length - 1)
			{
				// nothing to look for
				return null;
			}
			if (fromHere + horizon > str.Length - 1)
			{
				horizon = str.Length - fromHere - 1;
			}
			var start = str.IndexOf(label, fromHere, horizon, StringComparison.Ordinal);
			if (start < 0)
			{
				// no label found
				return null;
			}
			start += label.Length; // one char past label
			var delim = str.IndexOf(delimiter, start, StringComparison.Ordinal); // pos of 1st delimiter
			if (delim < 0)
			{
				// no text between delimiters
				return null;
			}
			start = delim + delimiter.Length; // start of text between delimiters
			var end = str.IndexOf(delimiter, start, StringComparison.Ordinal);
			return end < 0 ? null : str.Substring(start, end - start);
		}
	}
}