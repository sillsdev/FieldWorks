// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
using System.Xml.Linq;
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
using System.Xml;
using System.Diagnostics;
=======
using System.Xml;
using System.Diagnostics;
using System.Linq;
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
using Icu.Collation;
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.FwCoreDlgs;
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel.Utils;
=======
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Utils;
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;
using SIL.WritingSystems;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	internal class ConfiguredExport : CollectorEnv, ICollectPicturePathsOnly
	{
		private TextWriter m_writer;
		private LcmCache m_cache;
		private LcmStyleSheet m_stylesheet;
		private string m_sFormat;
		private StringCollection m_rgElementTags = new StringCollection();
		private StringCollection m_rgClassNames = new StringCollection();
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs

		enum CurrentContext
		{
			unknown = 0,
			insideObject = 1,
			insideProperty = 2,
			insideLink = 3,
		};
=======

		enum CurrentContext
		{
			unknown = 0,
			insideObject = 1,
			insideProperty = 2,
			insideLink = 3,
		};

		/// <summary>
		/// The level of the ICU rule that defined a digraph.
		/// </summary>
		public enum CollationLevel
		{
			/// <summary>
			/// Either secondary or tertiary level. It would be extra work to determine
			/// if it is secondary or tertiary and it's currently not needed.
			/// </summary>
			notPrimary = 0,

			/// <summary>
			/// First level
			/// </summary>
			primary = 1
		}

>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
		private CurrentContext m_cc = CurrentContext.unknown;
		private string m_sTimeField;
		private Dictionary<int, string> m_dictWsStr = new Dictionary<int, string>();
		/// <summary>The current lead (sort) character being written.</summary>
		private string m_schCurrent = string.Empty;
		/// <summary>
		/// Map from a writing system to its set of digraphs (or multigraphs) used in sorting.
		/// </summary>
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
		private Dictionary<string, ISet<string>> m_mapWsDigraphs = new Dictionary<string, ISet<string>>();
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
		Dictionary<string, ISet<string>> m_mapWsDigraphs = new Dictionary<string, ISet<string>>();
=======
		Dictionary<string, Dictionary<string, CollationLevel>> m_mapWsDigraphs = new Dictionary<string, Dictionary<string, CollationLevel>>();
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
		/// <summary>
		/// Map from a writing system to its map of equivalent graphs/multigraphs used in sorting.
		/// </summary>
		private Dictionary<string, Dictionary<string, string>> m_mapWsMapChars = new Dictionary<string, Dictionary<string, string>>();
		/// <summary>
		/// Map of characters to ignore for writing systems
		/// </summary>
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
		private Dictionary<string, ISet<string>> m_mapWsIgnorables = new Dictionary<string, ISet<string>>();
		private string m_sWsVern;
		private string m_sWsRevIdx;
		private Dictionary<int, string> m_dictCustomUserLabels = new Dictionary<int, string>();
		private string m_sActiveParaStyle;
		private Dictionary<XElement, string> m_mapXnToCssClass = new Dictionary<XElement, string>();
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
		Dictionary<string, ISet<string>> m_mapWsIgnorables = new Dictionary<string, ISet<string>>();

		private string m_sWsVern = null;
		private string m_sWsRevIdx = null;
		Dictionary<int, string> m_dictCustomUserLabels = new Dictionary<int, string>();
		string m_sActiveParaStyle;
		Dictionary<XmlNode, string> m_mapXnToCssClass = new Dictionary<XmlNode, string>();
=======
		Dictionary<string, ISet<string>> m_mapWsIgnorables = new Dictionary<string, ISet<string>>();

		private CoreWritingSystemDefinition m_wsVern;
		private CoreWritingSystemDefinition m_wsRevIdx;
		Dictionary<int, string> m_dictCustomUserLabels = new Dictionary<int, string>();
		string m_sActiveParaStyle;
		Dictionary<XmlNode, string> m_mapXnToCssClass = new Dictionary<XmlNode, string>();
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
		private XhtmlHelper m_xhtml;
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
		private CssType m_cssType = CssType.Dictionary;
		private bool m_fCancel;
		private string m_stylePara;
		private string m_delayedItemNumberClass;
		private ITsString m_delayedItemNumberValue;
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
		private XhtmlHelper.CssType m_cssType = XhtmlHelper.CssType.Dictionary;
=======
		private XhtmlHelper.CssType m_cssType = XhtmlHelper.CssType.Dictionary;
		private Dictionary<string, Collator> m_wsCollators = new Dictionary<string, Collator>();
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs

		/// <summary />
		public event ProgressHandler UpdateProgress;

		#region construction and initialization
		/// <summary />
		/// <param name="baseEnv">The base env.</param>
		/// <param name="sda">Data access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display, if m_baseEnv is null.
		/// If baseEnv is not null, hvoRoot is ignored.</param>
		public ConfiguredExport(IVwEnv baseEnv, ISilDataAccess sda, int hvoRoot)
			: base(baseEnv, sda, hvoRoot)
		{
		}

		/// <summary>
		/// Initialize the object with some useful information, and write the initial
		/// element start tag to the output.
		/// </summary>
		public void Initialize(LcmCache cache, IPropertyTable propertyTable, TextWriter w, string sDataType, string sFormat, string sOutPath, string sBodyClass)
		{
			m_writer = w;
			m_cache = cache;
			m_stylesheet = FwUtils.StyleSheetFromPropertyTable(propertyTable);
			MetaDataCache = cache.MetaDataCacheAccessor;
			m_sFormat = sFormat.ToLowerInvariant();
			if (m_sFormat == "xhtml")
			{
				m_xhtml = new XhtmlHelper(w, cache);
				m_xhtml.WriteXhtmlHeading(sOutPath, null, sBodyClass);
			}
			else
			{
				w.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
				w.WriteLine("<{0}>", sDataType);
			}
			m_cssType = (sBodyClass == "notebookBody") ? CssType.Notebook : CssType.Dictionary;
		}
		#endregion

		#region IVwEnv methods

		/// <summary>
		/// Adds the obj.
		/// </summary>
		public override void AddObj(int hvoItem, IVwViewConstructor vc, int frag)
		{
			var ccOld = WriteClassStartTag(hvoItem);
			WriteDelayedItemNumber();
			base.AddObj(hvoItem, vc, frag);
			WriteClassEndTag(ccOld);
			if (m_fCancel)
			{
				throw new CancelException(XMLViewsStrings.ConfiguredExportHasBeenCancelled);
			}
		}

		/// <summary>
		/// Adds the obj prop.
		/// </summary>
		public override void AddObjProp(int tag, IVwViewConstructor vc, int frag)
		{
			var ccOld = WriteFieldStartTag(tag);
			WriteDestClassStartTag(tag);
			base.AddObjProp(tag, vc, frag);
			WriteDestClassEndTag(tag);
			WriteFieldEndTag(ccOld);
		}

		/// <summary>
		/// Adds the obj vec.
		/// </summary>
		public override void AddObjVec(int tag, IVwViewConstructor vc, int frag)
		{
			var ccOld = WriteFieldStartTag(tag);
			base.AddObjVec(tag, vc, frag);
			WriteFieldEndTag(ccOld);
		}

		/// <summary>
		/// Adds the obj vec items.
		/// </summary>
		public override void AddObjVecItems(int tag, IVwViewConstructor vc, int frag)
		{
			var ccOld = WriteFieldStartTag(tag);
			OpenProp(tag);
			var cobj = DataAccess.get_VecSize(CurrentObject(), tag);
			for (var i = 0; i < cobj; i++)
			{
				var hvoItem = DataAccess.get_VecItem(CurrentObject(), tag, i);
				OpenTheObject(hvoItem, i);
				var ccPrev = WriteClassStartTag(hvoItem);
				vc.Display(this, hvoItem, frag);
				WriteClassEndTag(ccPrev);
				CloseTheObject();
				if (Finished)
				{
					break;
				}
				if (m_fCancel)
				{
					throw new CancelException(XMLViewsStrings.ConfiguredExportHasBeenCancelled);
				}
			}
			CloseProp();
			WriteFieldEndTag(ccOld);
		}

		/// <summary>
		/// Adds the prop.
		/// </summary>
		public override void AddProp(int tag, IVwViewConstructor vc, int frag)
		{
			var ccOld = WriteFieldStartTag(tag);
			base.AddProp(tag, vc, frag);
			WriteFieldEndTag(ccOld);
		}

		/// <summary>
		/// Member AddStringProp
		/// </summary>
		public override void AddStringProp(int tag, IVwViewConstructor vwvc)
		{
			var ccOld = WriteFieldStartTag(tag);
			var tss = DataAccess.get_StringProp(CurrentObject(), tag);
			WriteTsString(tss, TabsToIndent());
			WriteFieldEndTag(ccOld);
		}

		private string WritingSystemId(int ws)
		{
			if (ws == 0)
			{
				return string.Empty;
			}
			if (m_dictWsStr.TryGetValue(ws, out var sWs))
			{
				return sWs;
			}
			sWs = m_cache.WritingSystemFactory.GetStrFromWs(ws);
			sWs = XmlUtils.MakeSafeXmlAttribute(sWs);
			m_dictWsStr.Add(ws, sWs);
			return sWs;
		}

		/// <summary>
		/// Member AddUnicodeProp
		/// </summary>
		public override void AddUnicodeProp(int tag, int ws, IVwViewConstructor _vwvc)
		{
			var ccOld = WriteFieldStartTag(tag);
			var sText = DataAccess.get_UnicodeProp(CurrentObject(), tag);
			var icuNormalizer = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFC);
			// Need to ensure that sText is NFC for export.
			if (!icuNormalizer.IsNormalized(sText))
			{
				sText = icuNormalizer.Normalize(sText);
			}
			var sWs = WritingSystemId(ws);
			IndentLine();
			if (string.IsNullOrEmpty(sWs))
			{
				m_writer.WriteLine("<Uni>{0}</Uni>", XmlUtils.MakeSafeXml(sText));
			}
			else
			{
				m_writer.WriteLine("<AUni ws=\"{0}\">{1}</AUni>", sWs, XmlUtils.MakeSafeXml(sText));
			}
			WriteFieldEndTag(ccOld);
		}

		/// <summary>
		/// Member AddStringAltMember
		/// </summary>
		public override void AddStringAltMember(int tag, int ws, IVwViewConstructor _vwvc)
		{
			var ccOld = WriteFieldStartTag(tag);
			var tss = DataAccess.get_MultiStringAlt(CurrentObject(), tag, ws);
			WriteTsString(tss, TabsToIndent());
			// See if the string uses any styles that require us to export some more data.
			for (var irun = 0; irun < tss.RunCount; irun++)
			{
				var style = tss.get_StringProperty(irun, (int)FwTextPropType.ktptNamedStyle);
				var wsRun = tss.get_WritingSystem(irun);
				if (style == "Sense-Reference-Number")
				{
					m_xhtml?.MapCssToLang("xsensexrefnumber", m_cache.ServiceLocator.WritingSystemManager.Get(wsRun).Id);
				}
			}
			WriteFieldEndTag(ccOld);
		}

		/// <summary>
		/// Member AddIntProp
		/// </summary>
		public override void AddIntProp(int tag)
		{
			var ccOld = WriteFieldStartTag(tag);
			var n = m_cache.DomainDataByFlid.get_IntProp(CurrentObject(), tag);
			IndentLine();
			m_writer.WriteLine("<Integer val=\"{0}\"/>", n);
			WriteFieldEndTag(ccOld);
		}

		/// <summary>
		/// This implementation depend on the details of how XmlVc.cs handles "datetime" type data
		/// in ProcessFrag().
		/// </summary>
		public override void AddTimeProp(int tag, uint flags)
		{
			m_sTimeField = GetFieldXmlElementName(DataAccess.MetaDataCache.GetFieldName(tag), tag / 1000);
		}

		/// <summary>
		/// Write a TsString.  This is used in writing out a GenDate.
		/// </summary>
		public override void AddTsString(ITsString tss)
		{
			var cpt = (CellarPropertyType)MetaDataCache.GetFieldType(CurrentPropTag);
			if (cpt == CellarPropertyType.GenDate)
			{
				WriteTsString(tss, TabsToIndent());
			}
			else
			{
				base.AddTsString(tss);
			}
		}

		/// <summary>
		/// Adds the string.
		/// </summary>
		public override void AddString(ITsString tss)
		{
			string sElement;
			if (!string.IsNullOrEmpty(m_sTimeField))
			{
				sElement = m_sTimeField;
				m_sTimeField = null;
			}
			else
			{
				sElement = "LiteralString";
			}
			WriteStringBody(sElement, string.Empty, tss);
		}

		private void WriteStringBody(string sElement, string attrs, ITsString tss)
		{
			IndentLine();
			m_writer.WriteLine("<{0}{1}>", sElement, attrs);
			WriteTsString(tss, TabsToIndent() + 1);
			IndentLine();
			m_writer.WriteLine("</{0}>", sElement);
		}

		private void WriteTsString(ITsString tss, int tabs)
		{
			var xml = TsStringSerializer.SerializeTsStringToXml(tss, m_cache.WritingSystemFactory, writeObjData: false, indent: true);
			using (var reader = new StringReader(xml))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
				{
					m_writer.Write(new string(' ', tabs * 4));
					m_writer.WriteLine(line);
				}
			}
		}

		/// <summary>
		/// Mark the end of a paragraph.
		/// </summary>
		public override void CloseParagraph()
		{
			m_writer.WriteLine("</Paragraph>");
			m_stylePara = null;
		}

		/// <summary>
		/// Mark the beginning of a paragraph.
		/// </summary>
		public override void OpenParagraph()
		{
			if (string.IsNullOrEmpty(m_stylePara))
			{
				m_writer.WriteLine("<Paragraph>");
			}
			else
			{
				m_writer.WriteLine("<Paragraph style=\"{0}\">", XmlUtils.MakeSafeXmlAttribute(m_stylePara));
			}
		}

		/// <summary>
		/// Set either a paragraph or a character property.
		/// </summary>
		public override void set_StringProperty(int sp, string bstrValue)
		{
			if (sp != (int)FwTextPropType.ktptNamedStyle)
			{
				return;
			}
			var style = m_stylesheet?.FindStyle(bstrValue);
			if (style?.Type == StyleType.kstParagraph)
			{
				m_stylePara = bstrValue;
			}
		}
		#endregion

		#region Other CollectorEnv methods

		/// <summary>
		/// Optionally apply an XSLT to the output file.
		/// </summary>
		public override void PostProcess(string sXsltFile, string sOutputFile, int iPass)
		{
			if (!string.IsNullOrEmpty(sXsltFile))
			{
				base.PostProcess(sXsltFile, sOutputFile, iPass);
			}
			else if (m_sFormat == "xhtml")
			{
				var sTempFile = RenameOutputToPassN(sOutputFile, iPass);
				m_xhtml.FinalizeXhtml(sOutputFile, sTempFile);
				FileUtils.MoveFileToTempDirectory(sTempFile, "FieldWorks-Export");
			}
		}
		#endregion

		#region internal methods

		private int TabsToIndent()
		{
			var cTabs = 0;
			foreach (var s in m_rgElementTags)
			{
				if (!string.IsNullOrEmpty(s))
				{
					++cTabs;
				}
			}
			return cTabs;
		}

		private void IndentLine()
		{
			var cTabs = TabsToIndent();
			for (var i = 0; i < cTabs; ++i)
			{
				m_writer.Write("    ");
			}
		}

		private CurrentContext WriteClassStartTag(int hvoItem)
		{
			var ccOld = m_cc;
			if (m_cc != CurrentContext.insideLink)
			{
				m_cc = CurrentContext.insideObject;
			}
			var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem);
			var clid = obj.ClassID;
			var sClass = DataAccess.MetaDataCache.GetClassName(clid);
			IndentLine();
			var objGuid = obj.Guid; // LT-19976 XHTML export has been changed to use guid instead of hvo
			if (m_cc == CurrentContext.insideLink)
			{
				sClass = sClass + "Link";
				var targetHvo = hvoItem;
				var targetGuid = objGuid;
				if (obj is ILexSense lexSense)
				{
					// We want the link to go to the containing lex entry.
					// This has two advantages: first, the user can see the whole entry, rather than part of it
					// being scrolled off the top of the screen;
					// Secondly, some senses (e.g., of variants) may not be shown in the HTML at all, resulting in bad links (LT-11099)
					if (m_sFormat == "xhtml")
					{
						targetGuid = lexSense.Entry.Guid;
					}
					else
					{
						targetHvo = lexSense.Entry.Hvo;
					}
				}
				m_writer.WriteLine("<{0} target=\"{1}{2}\">",
					sClass,
					m_sFormat == "xhtml" ? "g" : "hvo",
					m_sFormat == "xhtml" ? targetGuid.ToString() : targetHvo.ToString());
			}
			else
			{
				if (m_sFormat == "xhtml")
				{
					switch (clid) // "default" case drops through on purpose
					{
						case LexEntryTags.kClassId:
							WriteEntryLetterHeadIfNeeded(hvoItem);
							break;
						case ReversalIndexEntryTags.kClassId:
							WriteReversalLetterHeadIfNeeded(hvoItem);
							break;
					}
					m_writer.WriteLine("<{0} id=\"g{1}\">", sClass, objGuid);
				}
				else
				{
					m_writer.WriteLine("<{0} id=\"hvo{1}\">", sClass, hvoItem);
				}
			}
			m_rgElementTags.Add(sClass);
			m_rgClassNames.Add(sClass);
			return ccOld;
		}

		private void WriteEntryLetterHeadIfNeeded(int hvoItem)
		{
			var sEntry = StringServices.ShortName1Static(m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(hvoItem));
			if (string.IsNullOrEmpty(sEntry))
			{
				return;
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
			}
			if (m_sWsVern == null)
			{
				m_sWsVern = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			}
			WriteLetterHeadIfNeeded(sEntry, m_sWsVern);
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			if (m_sWsVern == null)
				m_sWsVern = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			WriteLetterHeadIfNeeded(sEntry, m_sWsVern);
=======
			if (m_wsVern == null)
				m_wsVern = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			WriteLetterHeadIfNeeded(sEntry, m_wsVern);
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
		}

		private void WriteLetterHeadIfNeeded(string sEntry, CoreWritingSystemDefinition ws)
		{
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
			var sLower = GetLeadChar(CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(sEntry), sWs);
			var sTitle = Icu.UnicodeString.ToTitle(sLower, sWs);
			if (sTitle == m_schCurrent)
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			string sLower = GetLeadChar(CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(sEntry), sWs);
			string sTitle = Icu.UnicodeString.ToTitle(sLower, sWs);
			if (sTitle != m_schCurrent)
=======
			string sLower = GetLeadChar(CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(sEntry), ws.Id);
			string sTitle = new CaseFunctions(ws).ToTitle(sLower);
			if (sTitle != m_schCurrent)
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			{
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
				return;
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
				if (m_schCurrent.Length > 0)
					m_writer.WriteLine("</div>");	// for letData
				m_writer.WriteLine("<div class=\"letHead\">");
				var sb = new StringBuilder();
				if (!String.IsNullOrEmpty(sTitle) && sTitle != sLower)
				{
					sb.Append(sTitle.Normalize());
					sb.Append(' ');
				}
				if (!String.IsNullOrEmpty(sLower))
					sb.Append(sLower.Normalize());
				m_writer.WriteLine("<div class=\"letter\">{0}</div>", XmlUtils.MakeSafeXml(sb.ToString()));
				m_writer.WriteLine("</div>");
				m_writer.WriteLine("<div class=\"letData\">");
				m_schCurrent = sTitle;
=======
				if (m_schCurrent.Length > 0)
					m_writer.WriteLine("</div>");	// for letData
				m_writer.WriteLine("<div class=\"letHead\">");
				var sb = new StringBuilder();
				if (!string.IsNullOrEmpty(sTitle) && sTitle != sLower)
				{
					sb.Append(sTitle.Normalize());
					sb.Append(' ');
				}
				if (!string.IsNullOrEmpty(sLower))
					sb.Append(sLower.Normalize());
				m_writer.WriteLine("<div class=\"letter\">{0}</div>", XmlUtils.MakeSafeXml(sb.ToString()));
				m_writer.WriteLine("</div>");
				m_writer.WriteLine("<div class=\"letData\">");
				m_schCurrent = sTitle;
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			}
			if (m_schCurrent.Length > 0)
			{
				m_writer.WriteLine("</div>");   // for letData
			}
			m_writer.WriteLine("<div class=\"letHead\">");
			var sb = new StringBuilder();
			if (!string.IsNullOrEmpty(sTitle) && sTitle != sLower)
			{
				sb.Append(sTitle.Normalize());
				sb.Append(' ');
			}
			if (!string.IsNullOrEmpty(sLower))
			{
				sb.Append(sLower.Normalize());
			}
			m_writer.WriteLine("<div class=\"letter\">{0}</div>", XmlUtils.MakeSafeXml(sb.ToString()));
			m_writer.WriteLine("</div>");
			m_writer.WriteLine("<div class=\"letData\">");
			m_schCurrent = sTitle;
		}

		/// <summary>
		/// Get the lead character, either a single character or a composite matching something
		/// in the sort rules.  (We need to support multi-graph letters.  See LT-9244.)
		/// </summary>
		public string GetLeadChar(string headwordNFD, string sWs)
		{
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
			return GetLeadChar(sEntryNFD, sWs, m_mapWsDigraphs, m_mapWsMapChars, m_mapWsIgnorables, m_cache);
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			return GetLeadChar(sEntryNFD, sWs, m_mapWsDigraphs, m_mapWsMapChars, m_mapWsIgnorables,
									 m_cache);
=======
			var sortKeyCollator = GetCollator(sWs);
			return GetLeadChar(headwordNFD, sWs, m_mapWsDigraphs, m_mapWsMapChars, m_mapWsIgnorables, sortKeyCollator,
									 m_cache);
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
		}

		private Collator GetCollator(string sWs)
		{
			Collator col;
			if (m_wsCollators.TryGetValue(sWs, out col))
			{
				return col;
			}

			col = FwUtils.FwUtils.GetCollatorForWs(sWs);

			m_wsCollators[sWs] = col;
			return col;
		}

		/// <summary>
		/// Get the lead character, either a single character or a composite matching something
		/// in the sort rules.  (We need to support multi-graph letters.  See LT-9244.)
		/// </summary>
		/// <param name="headwordNFD">The headword to be written next</param>
		/// <param name="sWs">Name of the writing system</param>
		/// <param name="wsDigraphMap">Map of writing system to digraphs already discovered for that ws</param>
		/// <param name="wsCharEquivalentMap">Map of writing system to already discovered character equivalences for that ws</param>
		/// <param name="wsIgnorableCharMap">Map of writing system to ignorable characters for that ws </param>
		/// <param name="sortKeyCollator">A collator for the writing system to use to find sort keys</param>
		/// <param name="cache"></param>
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
		/// <returns>The character sEntryNFD is being sorted under in the dictionary.</returns>
		public static string GetLeadChar(string sEntryNFD, string sWs, Dictionary<string, ISet<string>> wsDigraphMap, Dictionary<string, Dictionary<string, string>> wsCharEquivalentMap,
													Dictionary<string, ISet<string>> wsIgnorableCharMap, LcmCache cache)
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
		/// <returns>The character sEntryNFD is being sorted under in the dictionary.</returns>
		public static string GetLeadChar(string sEntryNFD, string sWs,
													Dictionary<string, ISet<string>> wsDigraphMap,
													Dictionary<string, Dictionary<string, string>> wsCharEquivalentMap,
													Dictionary<string, ISet<string>> wsIgnorableCharMap,
													LcmCache cache)
=======
		/// <returns>The character headwordNFD is being sorted under in the dictionary.</returns>
		public static string GetLeadChar(string headwordNFD, string sWs,
													Dictionary<string, Dictionary<string, CollationLevel>> wsDigraphMap,
													Dictionary<string, Dictionary<string, string>> wsCharEquivalentMap,
													Dictionary<string, ISet<string>> wsIgnorableCharMap,
													Collator sortKeyCollator,
													LcmCache cache)
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
		{
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
			if (string.IsNullOrEmpty(sEntryNFD))
			{
				return string.Empty;
			}
			var sEntryPre = Icu.UnicodeString.ToLower(sEntryNFD, sWs);
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			if (string.IsNullOrEmpty(sEntryNFD))
				return "";
			string sEntryPre = Icu.UnicodeString.ToLower(sEntryNFD, sWs);
			Dictionary<string, string> mapChars;
=======
			if (string.IsNullOrEmpty(headwordNFD))
				return "";
			var ws = cache.ServiceLocator.WritingSystemManager.Get(sWs);
			var cf = new CaseFunctions(ws);
			var headwordLC = cf.ToLower(headwordNFD);
			Dictionary<string, string> mapChars;
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			// List of characters to ignore in creating letter heads.
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
			var sortChars = GetDigraphs(sWs, wsDigraphMap, wsCharEquivalentMap, wsIgnorableCharMap, cache, out var mapChars, out var chIgnoreList);
			var sEntry = string.Empty;
			if (chIgnoreList != null) // this list was built in GetDigraphs()
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			ISet<string> chIgnoreList;
			ISet<string> sortChars = GetDigraphs(sWs, wsDigraphMap, wsCharEquivalentMap, wsIgnorableCharMap, cache, out mapChars, out chIgnoreList);
			string sEntry = String.Empty;
			if (chIgnoreList != null) // this list was built in GetDigraphs()
=======
			ISet<string> chIgnoreList;
			ISet<string> sortChars = GetDigraphs(ws, wsDigraphMap, wsCharEquivalentMap, wsIgnorableCharMap, out mapChars, out chIgnoreList);
			if (chIgnoreList != null && chIgnoreList.Any()) // this list was built in GetDigraphs()
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			{
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
				foreach (var ch in sEntryPre)
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
				foreach (char ch in sEntryPre)
=======
				// sort the ignorable set with the longest first to avoid edge case where one ignorable
				// string starts with a shorter ignorable string.
				// eg. 'a' and 'aa'
				var ignorablesLongToShort = from s in chIgnoreList.ToList()
					orderby s.Length descending
					select s;
				foreach (var ignorableString in ignorablesLongToShort)
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
				{
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
					if (!(chIgnoreList.Contains(ch.ToString(CultureInfo.InvariantCulture))))
					{
						sEntry += ch;
					}
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
					if(!(chIgnoreList.Contains(ch.ToString(CultureInfo.InvariantCulture))))
						sEntry += ch;
=======
					// if the headword starts with the ignorable chop it off.
					if (headwordLC.StartsWith(ignorableString))
					{
						headwordLC = headwordLC.Substring(ignorableString.Length);
						break;
					}
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
				}
			}
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
			else
			{
				sEntry = sEntryPre;
			}
			if (string.IsNullOrEmpty(sEntry))
			{
				return string.Empty; // check again
			}
			var sEntryT = sEntry;
			bool fChanged;
			var map = mapChars;
			// This loop replaces each occurence of equivalent characters in sEntry
			// with the representative of its equivalence class// replace subsorting chars by their main sort char. a << 'a << ^a, etc. are replaced by a.
			do
			{
				foreach (var key in map.Keys)
				{
					sEntry = sEntry.Replace(key, map[key]);
				}
				fChanged = sEntryT != sEntry;
				if (sEntry.Length > sEntryT.Length && map == mapChars)
				{
					// Rules like a -> a' repeat infinitely! To truncate this eliminate any rule whose output contains an input.
					map = new Dictionary<string, string>(mapChars);
					foreach (var kvp in mapChars)
					{
						foreach (var key1 in mapChars.Keys)
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			else
				sEntry = sEntryPre;
			if (string.IsNullOrEmpty(sEntry))
				return ""; // check again
			string sEntryT = sEntry;
			bool fChanged = false;
			var map = mapChars;
			do  // This loop replaces each occurance of equivalent characters in sEntry
				// with the representative of its equivalence class
			{   // replace subsorting chars by their main sort char. a << 'a << ^a, etc. are replaced by a.
				foreach (string key in map.Keys)
					sEntry = sEntry.Replace(key, map[key]);
				fChanged = sEntryT != sEntry;
				if (sEntry.Length > sEntryT.Length && map == mapChars)
				{   // Rules like a -> a' repeat infinitely! To truncate this eliminate any rule whose output contains an input.
					map = new Dictionary<string, string>(mapChars);
					foreach (var kvp in mapChars)
					{
						foreach (var key1 in mapChars.Keys)
=======
			if (string.IsNullOrEmpty(headwordLC))
				return ""; // check again

			// If the headword begins with a primary digraph then use that as the first character without doing any replacement.
			string firstChar = null;
			foreach (var primaryDigraph in wsDigraphMap[ws.Id].Where(digraph => digraph.Value == CollationLevel.primary))
			{
				if (headwordLC.StartsWith(cf.ToLower(primaryDigraph.Key)))
					firstChar = cf.ToLower(primaryDigraph.Key);
			}

			// Replace equivalent characters.
			if (firstChar == null)
			{
				var headwordBeforeEquivalence = headwordLC;
				bool changed;
				var map = mapChars;
				do // This loop replaces each occurrence of equivalent characters in headwordLC
					// with the representative of its equivalence class
				{   // replace subsorting chars by their main sort char. a << 'a << ^a, etc. are replaced by a.
					foreach (string key in map.Keys)
						headwordLC = headwordLC.Replace(key, map[key]);
					changed = headwordBeforeEquivalence != headwordLC;
					if (headwordLC.Length > headwordBeforeEquivalence.Length && map == mapChars)
					{   // Rules like a -> a' repeat infinitely! To truncate this eliminate any rule whose output contains an input.
						map = new Dictionary<string, string>(mapChars);
						foreach (var kvp in mapChars)
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
						{
							foreach (var key1 in mapChars.Keys)
							{
								if (kvp.Value.Contains(key1))
								{
									map.Remove(kvp.Key);
									break;
								}
							}
						}
					}
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
				}
				sEntryT = sEntry;
			} while (fChanged);
			var cnt = GetLetterLengthAt(sEntry, 0);
			var sFirst = sEntry.Substring(0, cnt);
			foreach (var sChar in sortChars)
			{
				if (sEntry.StartsWith(sChar))
				{
					if (sFirst.Length < sChar.Length)
					{
						sFirst = sChar;
					}
				}
			}
			// We don't want sFirst for an ignored first character or digraph.
			Collator col;
			try
			{
				var icuLocale = new Icu.Locale(sWs).Name;
				col = Collator.Create(icuLocale);
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
				}
				sEntryT = sEntry;
			} while (fChanged);
			int cnt = GetLetterLengthAt(sEntry, 0);
			string sFirst = sEntry.Substring(0, cnt);
			foreach (string sChar in sortChars)
			{
				if (sEntry.StartsWith(sChar))
				{
					if (sFirst.Length < sChar.Length)
						sFirst = sChar;
				}
			}
			// We don't want sFirst for an ignored first character or digraph.

			Collator col;
			try
			{
				string icuLocale = new Icu.Locale(sWs).Name;
				col = Collator.Create(icuLocale);
=======

					headwordBeforeEquivalence = headwordLC;
				} while (changed);

				var cnt = GetLetterLengthAt(headwordLC, 0);
				firstChar = headwordLC.Substring(0, cnt);
				foreach (var sortChar in sortChars)
				{
					if (headwordLC.StartsWith(sortChar))
					{
						if (firstChar.Length < sortChar.Length)
							firstChar = sortChar;
					}
				}
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			}

			// We don't want firstChar for an ignored first character or digraph.
			if (sortKeyCollator != null)
			{
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
				return sFirst;
			}
			try
			{
				var ka = col.GetSortKey(sFirst).KeyData;
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
				return sFirst;
			}
			try
			{
				byte[] ka = col.GetSortKey(sFirst).KeyData;
=======
				byte[] ka = sortKeyCollator.GetSortKey(firstChar).KeyData;
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
				if (ka.Length > 0 && ka[0] == 1)
				{
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
					var sT = sEntry.Substring(sFirst.Length);
					return GetLeadChar(sT, sWs, wsDigraphMap, wsCharEquivalentMap, wsIgnorableCharMap, cache);
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
					string sT = sEntry.Substring(sFirst.Length);
					return GetLeadChar(sT, sWs, wsDigraphMap, wsCharEquivalentMap, wsIgnorableCharMap, cache);
=======
					return GetLeadChar(headwordLC.Substring(firstChar.Length), sWs, wsDigraphMap, wsCharEquivalentMap, wsIgnorableCharMap, sortKeyCollator, cache);
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
				}
			}
			return firstChar;
		}

		/// <returns>
		/// 2 if the letter at the index in the string is composed of a Surrogate Pair; 1 otherwise
		/// </returns>
		public static int GetLetterLengthAt(string sEntry, int ich)
		{
			return char.IsSurrogatePair(sEntry, ich) ? 2 : 1;
		}

		/// <summary>
		/// Get the set of significant digraphs (multigraphs) for the writing system. At the
		/// moment, these are derived from ICU sorting rules associated with the writing system.
		/// </summary>
		/// <param name="ws"/>
		/// <param name="mapChars">Set of character equivalences</param>
		/// <param name="chIgnoreSet">Set of characters to ignore</param>
		/// <returns></returns>
		internal ISet<string> GetDigraphs(CoreWritingSystemDefinition ws,
			out Dictionary<string, string> mapChars, out ISet<string> chIgnoreSet)
		{
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
			return GetDigraphs(sWs, m_mapWsDigraphs, m_mapWsMapChars, m_mapWsIgnorables, m_cache, out mapChars, out chIgnoreSet);
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			return GetDigraphs(sWs, m_mapWsDigraphs, m_mapWsMapChars, m_mapWsIgnorables, m_cache, out mapChars,
									 out chIgnoreSet);
=======
			return GetDigraphs(ws, m_mapWsDigraphs, m_mapWsMapChars, m_mapWsIgnorables, out mapChars,
									 out chIgnoreSet);
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
		}

		/// <summary>
		/// Get the set of significant digraphs (multigraphs) for the writing system. At the
		/// moment, these are derived from ICU sorting rules associated with the writing system.
		/// </summary>
		/// <param name="ws"/>
		/// <param name="wsDigraphMap">Map of writing system to digraphs already discovered for that ws</param>
		/// <param name="wsCharEquivalentMap">Map of writing system to already discovered character equivalences for that ws</param>
		/// <param name="wsIgnorableCharMap">Map of writing system to ignorable characters for that ws </param>
		/// <param name="mapChars">Set of character equivalences</param>
		/// <param name="chIgnoreSet">Set of characters to ignore</param>
		/// <returns></returns>
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
		internal static ISet<string> GetDigraphs(string sWs, Dictionary<string, ISet<string>> wsDigraphMap, Dictionary<string, Dictionary<string, string>> wsCharEquivalentMap,
			Dictionary<string, ISet<string>> wsIgnorableCharMap, LcmCache cache, out Dictionary<string, string> mapChars, out ISet<string> chIgnoreSet)
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
		internal static ISet<string> GetDigraphs(string sWs,
			Dictionary<string, ISet<string>> wsDigraphMap,
			Dictionary<string, Dictionary<string, string>> wsCharEquivalentMap,
			Dictionary<string, ISet<string>> wsIgnorableCharMap,
			LcmCache cache,
			out Dictionary<string, string> mapChars,
			out ISet<string> chIgnoreSet)
=======
		internal static ISet<string> GetDigraphs(CoreWritingSystemDefinition ws,
			Dictionary<string, Dictionary<string, CollationLevel>> wsDigraphMap,
			Dictionary<string, Dictionary<string, string>> wsCharEquivalentMap,
			Dictionary<string, ISet<string>> wsIgnorableCharMap,
			out Dictionary<string, string> mapChars,
			out ISet<string> chIgnoreSet)
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
		{
			var sWs = ws.Id;
			// Collect the digraph and character equivalence maps and the ignorable character set
			// the first time through. There after, these maps and lists are just retrieved.
			chIgnoreSet = new HashSet<string>(); // if ignorable chars get through they can become letter heads! LT-11172
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			ISet<string> digraphs;
=======
			Dictionary<string, CollationLevel> digraphs;
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			// Are the maps and ignorables already setup for the taking?
			if (wsDigraphMap.TryGetValue(sWs, out var digraphs))
			{
				// knows about ws, so already knows character equivalence classes
				mapChars = wsCharEquivalentMap[sWs];
				chIgnoreSet = wsIgnorableCharMap[sWs];
				return new HashSet<string>(digraphs.Keys);
			}
			digraphs = new Dictionary<string, CollationLevel>();
			mapChars = new Dictionary<string, string>();
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
			var ws = cache.ServiceLocator.WritingSystemManager.Get(sWs);
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			CoreWritingSystemDefinition ws = cache.ServiceLocator.WritingSystemManager.Get(sWs);

=======

>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			wsDigraphMap[sWs] = digraphs;
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
			if (ws.DefaultCollation is SimpleRulesCollationDefinition simpleCollation)
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs

			var simpleCollation = ws.DefaultCollation as SimpleRulesCollationDefinition;
			if (simpleCollation != null)
=======

			switch (ws.DefaultCollation)
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			{
				case SimpleRulesCollationDefinition simpleCollation:
				{
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
					var rules = simpleCollation.SimpleRules.Replace(" ", "=");
					var primaryParts = rules.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var part in primaryParts)
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
					string rules = simpleCollation.SimpleRules.Replace(" ", "=");
					string[] primaryParts = rules.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
					foreach (var part in primaryParts)
=======
					if (!string.IsNullOrEmpty(simpleCollation.SimpleRules))
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
					{
						string rules = simpleCollation.SimpleRules.Replace(" ", "=");
						string[] primaryParts = rules.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
						foreach (var part in primaryParts)
						{
							BuildDigraphSet(part, ws, wsDigraphMap);
							MapRuleCharsToPrimary(part, sWs, wsCharEquivalentMap);
						}
					}
					break;
				}
				// is this a custom ICU collation?
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
				var icuCollation = ws.DefaultCollation as IcuRulesCollationDefinition;
				if (!string.IsNullOrEmpty(icuCollation?.IcuRules))
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
				var icuCollation = ws.DefaultCollation as IcuRulesCollationDefinition;
				if (icuCollation != null && !string.IsNullOrEmpty(icuCollation.IcuRules))
=======
				case IcuRulesCollationDefinition icuCollation when !string.IsNullOrEmpty(icuCollation.IcuRules):
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
				{
					// prime with empty ws in case all the rules affect only the ignore set
					wsCharEquivalentMap[sWs] = mapChars;
					var individualRules = icuCollation.IcuRules.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var individualRule in individualRules)
					{
						// prepare rule for parsing by dropping certain whitespace and handling ICU Escape chars
						var rule = individualRule;
						NormalizeRule(ref rule);
						// This is a valid rule that specifies that the digraph aa should be ignored
						// [last tertiary ignorable] = \u02bc = aa
						// This may never happen, but some single characters should be ignored or they will
						// will be confused for digraphs with following characters.)))
						if (rule.Contains("["))
						{
							rule = ProcessAdvancedSyntacticalElements(chIgnoreSet, rule);
						}
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
						if (string.IsNullOrEmpty(rule.Trim()))
						{
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
						if (String.IsNullOrEmpty(rule.Trim()))
=======
						if (string.IsNullOrEmpty(rule.Trim()))
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
							continue;
						}
						rule = rule.Replace("<<<", "=");
						rule = rule.Replace("<<", "=");
						// If the rule contains one or more expansions ('/') remove the expansion portions
						if (rule.Contains("/"))
						{
							var isExpansion = false;
							var newRule = new StringBuilder();
							for (var ruleIndex = 0; ruleIndex <= rule.Length - 1; ruleIndex++)
							{
								if (rule.Substring(ruleIndex, 1) == "/")
								{
									isExpansion = true;
								}
								else if (rule.Substring(ruleIndex, 1) == "=" ||
										 rule.Substring(ruleIndex, 1) == "<")
								{
									isExpansion = false;
								}
								if (!isExpansion)
								{
									newRule.Append(rule.Substring(ruleIndex, 1));
								}
							}
							rule = newRule.ToString();
						}
						// "&N<ng<<<Ng<ny<<<Ny" => "&N<ng=Ng<ny=Ny"
						// "&N<�<<<�" => "&N<�=�"
						// There are other issues we are not handling properly such as the next line
						// &N<\u006e\u0067
						var primaryParts = rule.Split('<');
						foreach (var part in primaryParts)
						{
							if (rule.Contains("<"))
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
							{
								BuildDigraphSet(part, sWs, wsDigraphMap);
							}
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
								BuildDigraphSet(part, sWs, wsDigraphMap);
=======
								BuildDigraphSet(part, ws, wsDigraphMap);
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
							MapRuleCharsToPrimary(part, sWs, wsCharEquivalentMap);
						}
					}
					break;
				}
			}
			// This at least prevents null reference and key not found exceptions.
			// Possibly we should at least map the ASCII LC letters to UC.
			if (!wsCharEquivalentMap.TryGetValue(sWs, out mapChars))
			{
				wsCharEquivalentMap[sWs] = mapChars = new Dictionary<string, string>();
			}
			wsIgnorableCharMap.Add(sWs, chIgnoreSet);
			return new HashSet<string>(digraphs.Keys);
		}

		private static string ProcessAdvancedSyntacticalElements(ISet<string> chIgnoreSet, string rule)
		{
			const string ignorableEndMarker = "ignorable]";
			const string beforeBegin = "[before ";
			// parse out the ignorables and add them to the ignore list
			var ignorableBracketEnd = rule.IndexOf(ignorableEndMarker);
			if (ignorableBracketEnd > -1)
			{
				ignorableBracketEnd += ignorableEndMarker.Length; // skip over the search target
				var charsToIgnore = rule.Substring(ignorableBracketEnd).Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
				if (charsToIgnore.Length > 0)
				{
					foreach (var ch in charsToIgnore)
					{
						chIgnoreSet.Add(ch);
					}
				}
				// the ignorable section could be at the end of other parts of a rule so strip it off the end
				rule = rule.Substring(0, rule.IndexOf("["));
			}
			// check for before rules
			var beforeBeginLoc = rule.IndexOf(beforeBegin);
			if (beforeBeginLoc != -1)
			{
				const string primaryBeforeEnd = "1]";
				// [before 1] is for handling of primary characters which this code is concerned with
				// so we just strip it off and let the rest of the rule get processed.
				var beforeEndLoc = rule.IndexOf(primaryBeforeEnd, beforeBeginLoc);
				if (beforeEndLoc == -1)
				{
					// Either [before 2|3] which don't affect primary charactors or it's an invalid rule.
					// In each case we should ignore this rule
					return string.Empty;
				}
				rule = rule.Substring(0, beforeBeginLoc) + rule.Substring(beforeEndLoc + primaryBeforeEnd.Length);
			}
			return rule;
		}

		private static void MapRuleCharsToPrimary(string part, string ws, Dictionary<string, Dictionary<string, string>> wsToCharMap)
		{
			string primaryPart = null;
			foreach (var character in part.Split('='))
			{
				var sGraph = character.Trim();
				if (string.IsNullOrEmpty(sGraph))
				{
					continue;
				}
				sGraph = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(sGraph);
				if (primaryPart == null)
				{
					primaryPart = sGraph;
					continue;
				}
				if (!wsToCharMap.ContainsKey(ws))
				{
					wsToCharMap.Add(ws, new Dictionary<string, string>());
				}
				var mapToPrimary = wsToCharMap[ws];
				if (!mapToPrimary.ContainsKey(sGraph)) //This should never be, but don't crash
				{
					mapToPrimary.Add(sGraph, primaryPart);
				}
			}
		}

		private static void BuildDigraphSet(string part, CoreWritingSystemDefinition ws, Dictionary<string, Dictionary<string, CollationLevel>> wsDigraphsMap)
		{
			var sWs = ws.Id;
			var cf = new CaseFunctions(ws);
			var collationLevel = CollationLevel.primary;
			foreach (var character in part.Split('='))
			{
				var sGraph = character.Trim();
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
				if (string.IsNullOrEmpty(sGraph))
				{
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
				if (String.IsNullOrEmpty(sGraph))
=======
				if (string.IsNullOrEmpty(sGraph))
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
					continue;
				}
				sGraph = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(sGraph);
				if (sGraph.Length > 1)
				{
					sGraph = cf.ToLower(sGraph);
					if (!wsDigraphsMap.ContainsKey(sWs))
					{
						wsDigraphsMap.Add(sWs, new Dictionary<string, CollationLevel> { {sGraph, collationLevel } });
					}
					else
					{
						if (!wsDigraphsMap[sWs].Keys.Contains(sGraph))
						{
							wsDigraphsMap[sWs].Add(sGraph, collationLevel);
						}
					}
				}

				collationLevel = CollationLevel.notPrimary;
			}
		}

		private static void NormalizeRule(ref string rule)
		{
			// drop carriage returns and spaces around '='
			rule = rule.TrimEnd('\r', '\n');
			rule = rule.Replace(" =", "=").Replace("= ", "=");
			RemoveICUEscapeChars(ref rule);
		}

		private static void RemoveICUEscapeChars(ref string sRule)
		{
			const string quoteEscape = @"!@#quote#@!";
			const string slashEscape = @"!@#slash#@!";
			sRule = sRule.Replace(@"''", quoteEscape);
			sRule = sRule.Replace(@"'", string.Empty);
			sRule = sRule.Replace(quoteEscape, @"'");
			sRule = sRule.Replace(@"\\\\", slashEscape);
			sRule = sRule.Replace(@"\\", string.Empty);
			sRule = sRule.Replace(slashEscape, @"\\");
			ReplaceICUUnicodeEscapeChars(ref sRule);
		}

		private static void ReplaceICUUnicodeEscapeChars(ref string sRule)
		{
			sRule = Regex.Replace(sRule, @"\\u(?<Value>[a-zA-Z0-9]{4})", m => ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString(CultureInfo.InvariantCulture));
		}

		private void WriteReversalLetterHeadIfNeeded(int hvoItem)
		{
			var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem);
			var objOwner = obj.Owner;
			if (!(objOwner is IReversalIndex))
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
			{
				return;     // subentries shouldn't trigger letter head change!
			}
			var entry = (IReversalIndexEntry)obj;
			var idx = (IReversalIndex)objOwner;
			var ws = m_cache.ServiceLocator.WritingSystemManager.Get(idx.WritingSystem);
			var sEntry = entry.ReversalForm.get_String(ws.Handle).Text;
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs
				return;		// subentries shouldn't trigger letter head change!

			var entry = (IReversalIndexEntry) obj;
			var idx = (IReversalIndex) objOwner;
			CoreWritingSystemDefinition ws = m_cache.ServiceLocator.WritingSystemManager.Get(idx.WritingSystem);
			string sEntry = entry.ReversalForm.get_String(ws.Handle).Text;
=======
				return;		// subentries shouldn't trigger letter head change!

			var entry = (IReversalIndexEntry) obj;
			var idx = (IReversalIndex) objOwner;
			if (m_wsRevIdx == null)
				m_wsRevIdx = m_cache.ServiceLocator.WritingSystemManager.Get(idx.WritingSystem);
			string sEntry = entry.ReversalForm.get_String(m_wsRevIdx.Handle).Text;
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
			if (string.IsNullOrEmpty(sEntry))
			{
				return;
<<<<<<< HEAD:Src/LanguageExplorer/Controls/XMLViews/ConfiguredExport.cs
			}
			if (string.IsNullOrEmpty(m_sWsRevIdx))
			{
				m_sWsRevIdx = ws.Id;
			}
			WriteLetterHeadIfNeeded(sEntry, m_sWsRevIdx);
||||||| f013144d5:Src/Common/Controls/XMLViews/ConfiguredExport.cs

			if (string.IsNullOrEmpty(m_sWsRevIdx))
				m_sWsRevIdx = ws.Id;
			WriteLetterHeadIfNeeded(sEntry, m_sWsRevIdx);
=======

			WriteLetterHeadIfNeeded(sEntry, m_wsRevIdx);
>>>>>>> develop:Src/Common/Controls/XMLViews/ConfiguredExport.cs
		}

		private void WriteClassEndTag(CurrentContext ccOld)
		{
			m_cc = ccOld;
			var iTop = m_rgElementTags.Count - 1;
			var sClass = m_rgElementTags[iTop];
			m_rgElementTags.RemoveAt(iTop);
			IndentLine();
			m_writer.WriteLine("</{0}>", sClass);
			iTop = m_rgClassNames.Count - 1;
			m_rgClassNames.RemoveAt(iTop);
			if (UpdateProgress != null && m_rgClassNames.Count == 0)
			{
				UpdateProgress(this);
			}
		}

		private CurrentContext WriteFieldStartTag(int flid)
		{
			var ccOld = m_cc;
			string sXml;
			try
			{
				var mdc = DataAccess.GetManagedMetaDataCache();
				var cpt = (CellarPropertyType)mdc.GetFieldType(flid);
				var sField = mdc.GetFieldName(flid);
				switch (cpt)
				{
					case CellarPropertyType.ReferenceAtomic:
						// Don't treat the Self property as starting a link (or a property).
						// View it as just a continuation of the current state.  (See FWR-1673.)
						if (sField != "Self" || !mdc.get_IsVirtual(flid))
						{
							m_cc = CurrentContext.insideLink;
						}
						break;
					case CellarPropertyType.ReferenceCollection:
					case CellarPropertyType.ReferenceSequence:
						m_cc = CurrentContext.insideLink;
						break;
					default:
						m_cc = CurrentContext.insideProperty;
						break;
				}
				sXml = GetFieldXmlElementName(sField, flid / 1000);
				if (sXml == "_")
				{
					sXml = string.Empty;
				}
				else
				{
					if (sXml == "_" + sField && ccOld == CurrentContext.insideLink && m_cc == CurrentContext.insideProperty)
					{
						AddMissingObjectLink();
						sXml = GetFieldXmlElementName(sField, flid / 1000);
					}
					IndentLine();
					string sUserLabel = null;
					if (mdc.IsCustom(flid))
					{
						// REVIEW: NOT SURE userlabel attribute is useful (or needed) in the
						// new system since the field name serves as the label.  On the other
						// hand, the field name might have spaces in it, which is a no-no for
						// XML element tags.
						if (!m_dictCustomUserLabels.TryGetValue(flid, out sUserLabel))
						{
							sUserLabel = DataAccess.MetaDataCache.GetFieldLabel(flid);
							m_dictCustomUserLabels.Add(flid, sUserLabel);
						}
						sXml = sXml.Replace(' ', '.');
					}
					if (string.IsNullOrEmpty(sUserLabel))
					{
						m_writer.WriteLine("<{0}>", sXml);
					}
					else
					{
						m_writer.WriteLine("<{0} userlabel=\"{1}\">", sXml, sUserLabel);
					}
				}
			}
			catch
			{
				sXml = string.Empty;
			}
			m_rgElementTags.Add(sXml);
			return ccOld;
		}

		private void AddMissingObjectLink()
		{
			if (m_sFormat == "xhtml")
			{
				return;
			}
			var iTopClass = m_rgClassNames.Count - 1;
			var iTopElem = m_rgElementTags.Count - 1;
			if (iTopClass < 0 || iTopElem < 0)
			{
				return;
			}
			var sClass = m_rgClassNames[iTopClass];
			var sElement = m_rgElementTags[iTopElem];
			if (sClass != sElement)
			{
				return;
			}
			if (!string.IsNullOrEmpty(sClass))
			{
				return;
			}
			var hvo = CurrentObject();
			if (hvo == 0)
			{
				return;
			}
			try
			{
				sClass = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo).ClassName + "Link";
				var sOut = $"<{sClass} target=\"hvo{hvo}\">";
				IndentLine();
				m_writer.WriteLine(sOut);
				m_rgClassNames[iTopClass] = sClass;
				m_rgElementTags[iTopElem] = sClass;
			}
			catch
			{
			}
		}

		private string GetFieldXmlElementName(string sField, int clid)
		{
			string sClass = null;
			if (clid > 0 && clid < 10000)
			{
				sClass = DataAccess.MetaDataCache.GetClassName(clid);
			}

			var sClass2 = string.Empty;
			var iTopClass = m_rgClassNames.Count - 1;
			if (iTopClass >= 0)
			{
				sClass2 = m_rgClassNames[iTopClass];
			}
			var safeField = MakeStringValidXmlElement(sField);
			return !string.IsNullOrEmpty(sClass) ? $"{sClass}_{safeField}" : $"{sClass2}_{safeField}";
		}

		/// <summary>
		/// Replace any necessary characters in the string to make it a valid continuation (not the first character) of an
		/// XML element name. According to http://www.xml.com/pub/a/2001/07/25/namingparts.html, it has to conform to
		/// Letter | Digit | '.' | '-' | '_' | ':' | CombiningChar | Extender. more precisely, the RE here is based on
		/// http://www.w3.org/TR/REC-xml/#NT-Name. I have left out the range [#x10000-#xEFFFF] as this involves
		/// surrogate pairs and I'm not sure how this RE handles them. Characters in this range will be converted.
		/// </summary>
		private string MakeStringValidXmlElement(string input)
		{
			// Anything followed by one illegal character followed by anything; must match whole string.
			var pattern = new Regex(@"^(.*)([^\-:_.A-Za-z0-9\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u02FF\u0370-\u037D\u037f-\u1FFF"
									+ @"\u200C-\u200D\u2070-\u218F\u2C00-\u2FEF\u3001-\uD7FF\uF900-\uFDCF\uFDF0-\uFFFD"
									+ @"\u00B7\u0300-\u036F\u203F-\u2040])"
									+ @"(.*)$");
			var result = input;
			for (; ;)
			{
				var match = pattern.Match(result);
				if (!match.Success)
				{
					return result; // We can't find an illegal character, string is good.
				}
				result = match.Groups[1].Value + MakeValueForXmlElement(match.Groups[2].Value[0]) + match.Groups[3].Value;
			}
		}

		private static string MakeValueForXmlElement(char c)
		{
			if (c == ' ')
			{
				return "_"; // friendlier than hex.
}
			return string.Format("{0:x}", Convert.ToInt32(c));
		}

		private void WriteFieldEndTag(CurrentContext ccOld)
		{
			m_cc = ccOld;
			var iTop = m_rgElementTags.Count - 1;
			var sField = m_rgElementTags[iTop];
			m_rgElementTags.RemoveAt(iTop);
			if (!string.IsNullOrEmpty(sField))
			{
				IndentLine();
				m_writer.WriteLine("</{0}>", sField);
			}
		}

		private void WriteDestClassStartTag(int flid)
		{
			var sDestClass = WriteDestClassTag(flid, "<{0}>");
			m_rgElementTags.Add(sDestClass);
			m_rgClassNames.Add(sDestClass);
		}

		private void WriteDestClassEndTag(int flid)
		{
			var iTop = m_rgElementTags.Count - 1;
			var sElem = m_rgElementTags[iTop];
			m_rgElementTags.RemoveAt(iTop);
			var sDestClass = WriteDestClassTag(flid, "</{0}>");
			if (string.IsNullOrEmpty(sDestClass) && !string.IsNullOrEmpty(sElem))
			{
				// Make up for trickiness of AddMissingObjectLink().
				IndentLine();
				m_writer.WriteLine("</{0}>", sElem);
			}
			else
			{
				Debug.Assert(sDestClass == sElem);
			}
			iTop = m_rgClassNames.Count - 1;
			var sClass = m_rgClassNames[iTop];
			m_rgClassNames.RemoveAt(iTop);
			Debug.Assert(sClass == sElem);
		}

		private string WriteDestClassTag(int flid, string sFmt)
		{
			try
			{
				if (DataAccess.MetaDataCache.get_IsVirtual(flid))
				{
					return string.Empty;
				}
				var sDestClass = DataAccess.MetaDataCache.GetDstClsName(flid);
				if (m_cc == CurrentContext.insideLink)
				{
					sDestClass = $"{sDestClass}Link";
				}
				IndentLine();
				m_writer.WriteLine(sFmt, sDestClass);
				return sDestClass;
			}
			catch
			{
				return string.Empty;
			}
		}
		#endregion

		#region other public methods

		/// <summary>
		/// Write the closing element end tag to the output.
		/// </summary>
		public void Finish(string sDataType)
		{
			if (m_sFormat == "xhtml")
			{
				if (m_schCurrent.Length > 0)
				{
					m_writer.WriteLine("</div>");   // for letData
				}
				m_xhtml.WriteXhtmlEnding();
			}
			else
			{
				m_writer.WriteLine("</{0}>", sDataType);
			}
			m_writer.Close();
			m_writer = null;
			// Dispose of any collators that we needed during this export
			foreach (var collator in m_wsCollators.Values)
			{
				collator?.Dispose();
			}
			m_wsCollators.Clear();
		}

		/// <summary>
		/// The view wants to output the given tss, which is the generated number of an item in a sequence.
		/// We are passed both the composite string that a real view would display, and the XML node from
		/// which we got the spec that indicated we should number this sequence.
		/// </summary>
		internal void OutputItemNumber(ITsString tss, XElement delimitNode)
		{
			if (m_sFormat != "xhtml")
			{
				AddString(tss);
				return;
			}
			var cssClass = XmlUtils.GetOptionalAttributeValue(delimitNode, "cssNumber");
			if (cssClass == null)
			{
				AddString(tss);
				return;
			}
			var tag = delimitNode.Attribute("number").Value; // optional normally, but then this is not called
			int ich;
			for (ich = 0; ich < tag.Length; ich++)
			{
				if (tag[ich] == '%')
				{
					if (ich == tag.Length - 1 || tag[ich + 1] != '%')
					{
						break;
					}
				}
			}
			var before = tag.Substring(0, ich); // arbitrarily it is all 'before' if no %.
			var after = ich < tag.Length - 1 ? tag.Substring(ich + 2) : "";
			if (!m_xhtml.NumberStyles.ContainsKey(cssClass))
			{
				m_xhtml.NumberStyles[cssClass] = new Tuple<string, string>(before, after);
			}
			// Strip of the literal text from the string and make a special element out of it.
			var bldr = tss.GetBldr();
			if (before.Length > 0)
			{
				bldr.Replace(0, before.Length, "", null);
			}
			if (after.Length > 0)
			{
				bldr.Replace(bldr.Length - after.Length, bldr.Length, "", null);
			}
			// We want the number to be part of the item. However, the VC outputs it just before the item.
			// So we postpone outputting the number until the AddObj call. Yuck.
			m_delayedItemNumberClass = cssClass;
			m_delayedItemNumberValue = bldr.GetString();
		}

		private void WriteDelayedItemNumber()
		{
			if (m_delayedItemNumberValue == null)
			{
				return;
			}
			WriteStringBody("ItemNumber", " class=\"" + m_xhtml.GetValidCssClassName(m_delayedItemNumberClass) + "\"", m_delayedItemNumberValue);
			m_delayedItemNumberValue = null;
			m_delayedItemNumberClass = null;
		}

		internal void BeginCssClassIfNeeded(XElement frag)
		{
			if (m_sFormat != "xhtml")
			{
				return;
			}
			if (!m_mapXnToCssClass.TryGetValue(frag, out var cssClass))
			{
				cssClass = XmlUtils.GetOptionalAttributeValue(frag, "css", null);
				// Note that an empty string for cssClass means we explicitly don't want to output anything.
				if (cssClass == null && frag.Parent != null && frag.Parent.Name == "layout"
				    && (XmlUtils.GetOptionalAttributeValue(frag, "before") != null
						|| XmlUtils.GetOptionalAttributeValue(frag, "after") != null
						|| XmlUtils.GetOptionalAttributeValue(frag, "sep") != null
						|| XmlUtils.GetOptionalAttributeValue(frag, "number") != null
						|| XmlUtils.GetOptionalAttributeValue(frag, "style") != null))
				{
					var sb = new StringBuilder(XmlUtils.GetOptionalAttributeValue(frag.Parent, "class", string.Empty));
					if (sb.Length > 0)
					{
						sb.Append("-");
						sb.Append(XmlUtils.GetOptionalAttributeValue(frag.Parent, "name", string.Empty));
						sb.Append("-");
						var sRef = XmlUtils.GetMandatoryAttributeValue(frag, "ref");
						sb.Append(sRef == "$child" ? XmlUtils.GetOptionalAttributeValue(frag, "label", string.Empty) : sRef);
						sb.Replace(" ", "--");
						cssClass = sb.ToString();
					}
				}
				var sDup = XmlUtils.GetOptionalAttributeValue(frag, "dup");
				if (!string.IsNullOrEmpty(cssClass) && !string.IsNullOrEmpty(sDup))
				{
					cssClass += sDup;
				}
				if (!string.IsNullOrEmpty(cssClass) && !cssClass.StartsWith("$fwstyle="))
				{
					if (m_xhtml.TryGetNodeFromCssClass(cssClass, out var oldNode))
					{
						// Trouble: we have some other node using the same style. This can legitimately happen
						// if we output the same part with different writing systems selected, so deal with that.
						// Generate a (hopefully) unique cssClass.
						var wsOld = XmlUtils.GetOptionalAttributeValue(oldNode, "ws");
						var wsNew = XmlUtils.GetOptionalAttributeValue(frag, "ws");
						if (!string.IsNullOrEmpty(wsOld) && !string.IsNullOrEmpty(wsNew) && wsOld != wsNew)
						{
							var tryCssClass = $"{cssClass}-{wsNew}";
							if (m_xhtml.TryGetNodeFromCssClass(tryCssClass, out oldNode))
							{
								// another level of duplicate!
								throw new FwConfigurationException($"Two distinct XML nodes are using the same cssClass ({cssClass}) and writing system ({wsNew}) in the same export:"
								                                   + Environment.NewLine + oldNode.GetOuterXml() + Environment.NewLine + frag.GetOuterXml());
							}
							cssClass = tryCssClass; // This is a unique key we will use as the css class for these items.
						}
						else
						{
							throw new FwConfigurationException($"Two distinct XML nodes are using the same cssClass ({cssClass}) in the same export with the same (or no) writing system:"
								+ Environment.NewLine + oldNode.GetOuterXml() + Environment.NewLine + frag.GetOuterXml());
						}
					}
					m_xhtml.MapCssClassToXElement(cssClass, frag);
				}
				m_mapXnToCssClass.Add(frag, cssClass);
			}
			if (!string.IsNullOrEmpty(cssClass))
			{
				if (cssClass.StartsWith("$fwstyle="))
				{
					m_sActiveParaStyle = cssClass.Substring(9);
				}
				else
				{
					var flowType = GetFlowType(frag);
					switch (flowType)
					{
						case "div":
						case "para":
							m_writer.WriteLine("<div class=\"{0}\">", m_xhtml.GetValidCssClassName(cssClass));
							break;
						default:
						{
							if (flowType != "divInPara")
							{
								m_writer.WriteLine("<span class=\"{0}\">", m_xhtml.GetValidCssClassName(cssClass));
							}
							break;
						}
					}
				}
			}
		}

		private static string GetFlowType(XElement frag)
		{
			var flowType = XmlUtils.GetOptionalAttributeValue(frag, "flowType", null);
			if (flowType == null)
			{
				var fShowAsPara = XmlUtils.GetOptionalBooleanAttributeValue(frag, "showasindentedpara", false);
				if (fShowAsPara)
				{
					return "para";
				}
			}
			return flowType;
		}

		/// <summary>
		/// Make this string safe to place inside an XML comment.
		/// </summary>
		private static string CommentProtect(string str)
		{
			return str.Replace("-", "\\-");
		}

		internal void EndCssClassIfNeeded(XElement frag)
		{
			if (m_sFormat != "xhtml")
			{
				return;
			}
			if (!m_mapXnToCssClass.TryGetValue(frag, out var cssClass) || string.IsNullOrEmpty(cssClass))
			{
				return;
			}
			if (cssClass.StartsWith("$fwstyle="))
			{
				m_sActiveParaStyle = null;
			}
			else
			{
				var flowType = GetFlowType(frag);
				if (flowType == "div" || flowType == "para")
				{
					m_writer.WriteLine("</div><!--class=\"{0}\"-->", CommentProtect(cssClass));
				}
				else if (flowType != "divInPara")
				{
					m_writer.WriteLine("</span><!--class=\"{0}\"-->", CommentProtect(cssClass));
				}
				if (string.IsNullOrEmpty(m_sActiveParaStyle))
				{
					return;
				}
				if (m_xhtml.MapCssToStyleEnv(cssClass, out var envirs))
				{
					if (!envirs.Contains(m_sActiveParaStyle))
					{
						envirs.Add(m_sActiveParaStyle);
					}
				}
				else
				{
					envirs = new List<string>
					{
						m_sActiveParaStyle
					};
					m_xhtml.MapCssToStyleEnv(cssClass, envirs);
				}
			}
		}

		internal void BeginMultilingualAlternative(int ws)
		{
			if (m_sFormat == "xhtml")
			{
				m_writer.WriteLine("<Alternative ws=\"{0}\">", WritingSystemId(ws));
			}
		}

		internal void EndMultilingualAlternative()
		{
			if (m_sFormat == "xhtml")
			{
				m_writer.WriteLine("</Alternative>");
			}
		}

		/// <summary>
		/// Write a Cascading Style Sheet file based on the accumulated layouts
		/// and the given stylesheet.
		/// </summary>
		public void WriteCssFile(string sOutputFile, IVwStylesheet vss, bool allowDictionaryParagraphIndent)
		{
			m_xhtml.AllowDictionaryParagraphIndent = allowDictionaryParagraphIndent;
			m_xhtml.WriteCssFile(sOutputFile, vss, m_cssType, null);
		}

		/// <summary>
		/// Set Cancel flag to asynchronously cancel exporting.
		/// </summary>
		public void Cancel()
		{
			m_fCancel = true;
		}

		#endregion

		/// <summary>
		/// This class does not care about pictures being added at all. It implements the interface just to
		/// save memory by not having the caller create the pictures.
		/// </summary>
		public void APictureIsBeingAdded()
		{
		}

		private enum CurrentContext
		{
			unknown = 0,
			insideObject = 1,
			insideProperty = 2,
			insideLink = 3,
		}
	}
}
