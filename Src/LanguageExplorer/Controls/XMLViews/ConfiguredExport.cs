// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Xml.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel.Utils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.WritingSystems;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Summary description for ConfiguredExport.
	/// </summary>
	public class ConfiguredExport : CollectorEnv, ICollectPicturePathsOnly
	{
		private TextWriter m_writer;
		private LcmCache m_cache;
		private LcmStyleSheet m_stylesheet;
		private string m_sFormat;
		private StringCollection m_rgElementTags = new StringCollection();
		private StringCollection m_rgClassNames = new StringCollection();

		private enum CurrentContext
		{
			unknown = 0,
			insideObject = 1,
			insideProperty = 2,
			insideLink = 3,
		};
		private CurrentContext m_cc = CurrentContext.unknown;
		private string m_sTimeField;

		Dictionary<int, string> m_dictWsStr = new Dictionary<int,string>();
		/// <summary>The current lead (sort) character being written.</summary>
		private string m_schCurrent = String.Empty;
		/// <summary>
		/// Map from a writing system to its set of digraphs (or multigraphs) used in sorting.
		/// </summary>
		Dictionary<string, ISet<string>> m_mapWsDigraphs = new Dictionary<string, ISet<string>>();
		/// <summary>
		/// Map from a writing system to its map of equivalent graphs/multigraphs used in sorting.
		/// </summary>
		Dictionary<string, Dictionary<string, string>> m_mapWsMapChars = new Dictionary<string, Dictionary<string, string>>();
		/// <summary>
		/// Map of characters to ignore for writing systems
		/// </summary>
		Dictionary<string, ISet<string>> m_mapWsIgnorables = new Dictionary<string, ISet<string>>();

		private string m_sWsVern;
		private string m_sWsRevIdx;
		Dictionary<int, string> m_dictCustomUserLabels = new Dictionary<int, string>();
		string m_sActiveParaStyle;
		Dictionary<XElement, string> m_mapXnToCssClass = new Dictionary<XElement, string>();
		private XhtmlHelper m_xhtml;
		private CssType m_cssType = CssType.Dictionary;

		private bool m_fCancel;

		/// <summary />
		public event ProgressHandler UpdateProgress;

		#region construction and initialization
		/// <summary>
		/// Initializes a new instance of the <see cref="ConfiguredExport"/> class.
		/// </summary>
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
		public void Initialize(LcmCache cache, IPropertyTable propertyTable, TextWriter w, string sDataType,
			string sFormat, string sOutPath, string sBodyClass)
		{
			m_writer = w;
			m_cache = cache;
			m_stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);
			m_mdc = cache.MetaDataCacheAccessor;
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

			WriteClassEndTag(hvoItem, ccOld);

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

			WriteFieldEndTag(tag, ccOld);
		}

		/// <summary>
		/// Adds the obj vec.
		/// </summary>
		public override void AddObjVec(int tag, IVwViewConstructor vc, int frag)
		{
			var ccOld = WriteFieldStartTag(tag);

			base.AddObjVec(tag, vc, frag);

			WriteFieldEndTag(tag, ccOld);
		}

		/// <summary>
		/// Adds the obj vec items.
		/// </summary>
		public override void AddObjVecItems(int tag, IVwViewConstructor vc, int frag)
		{
			var ccOld = WriteFieldStartTag(tag);
			OpenProp(tag);
			var cobj = DataAccess.get_VecSize(CurrentObject(),tag);

			for (var i = 0; i < cobj; i++)
			{
				var hvoItem = DataAccess.get_VecItem(CurrentObject(), tag, i);
				OpenTheObject(hvoItem, i);
				var ccPrev = WriteClassStartTag(hvoItem);

				vc.Display(this, hvoItem, frag);

				WriteClassEndTag(hvoItem, ccPrev);
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
			WriteFieldEndTag(tag, ccOld);
		}

		/// <summary>
		/// Adds the prop.
		/// </summary>
		public override void AddProp(int tag, IVwViewConstructor vc, int frag)
		{
			var ccOld = WriteFieldStartTag(tag);

			base.AddProp(tag, vc, frag);

			WriteFieldEndTag(tag, ccOld);
		}

		/// <summary>
		/// Member AddStringProp
		/// </summary>
		public override void AddStringProp(int tag, IVwViewConstructor _vwvc)
		{
			var ccOld = WriteFieldStartTag(tag);

			var tss = DataAccess.get_StringProp(CurrentObject(), tag);
			WriteTsString(tss, TabsToIndent());

			WriteFieldEndTag(tag, ccOld);
		}

		private string WritingSystemId(int ws)
		{
			if (ws == 0)
			{
				return string.Empty;
			}
			string sWs;
			if (m_dictWsStr.TryGetValue(ws, out sWs))
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
			// Need to ensure that sText is NFC for export.
			if (!Icu.IsNormalized(sText, Icu.UNormalizationMode.UNORM_NFC))
			{
				sText = Icu.Normalize(sText, Icu.UNormalizationMode.UNORM_NFC);
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
			WriteFieldEndTag(tag, ccOld);
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
				var style = tss.get_StringProperty(irun, (int) FwTextPropType.ktptNamedStyle);
				var wsRun = tss.get_WritingSystem(irun);
				switch (style)
				{
					case "Sense-Reference-Number":
						m_xhtml?.MapCssToLang("xsensexrefnumber", m_cache.ServiceLocator.WritingSystemManager.Get(wsRun).Id);
						break;
				}
			}

			WriteFieldEndTag(tag, ccOld);
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

			WriteFieldEndTag(tag, ccOld);
		}

		/// <summary>
		/// This implementation depend on the details of how XmlVc.cs handles "datetime" type data
		/// in ProcessFrag().
		/// </summary>
		public override void AddTimeProp(int tag, uint flags)
		{
			var sField = m_sda.MetaDataCache.GetFieldName(tag);
			m_sTimeField = GetFieldXmlElementName(sField, tag/1000);
		}

		/// <summary>
		/// Write a TsString.  This is used in writing out a GenDate.
		/// </summary>
		public override void AddTsString(ITsString tss)
		{
			var cpt = (CellarPropertyType)m_mdc.GetFieldType(m_tagCurrent);
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
			WriteStringBody(sElement, "", tss);
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
			if (String.IsNullOrEmpty(m_stylePara))
				m_writer.WriteLine("<Paragraph>");
			else
				m_writer.WriteLine("<Paragraph style=\"{0}\">", XmlUtils.MakeSafeXmlAttribute(m_stylePara));
		}

		private string m_stylePara;

		/// <summary>
		/// Set either a paragraph or a character property.
		/// </summary>
		public override void set_StringProperty(int sp, string bstrValue)
		{
			if (sp != (int) FwTextPropType.ktptNamedStyle)
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
			for (var i = 0; i < m_rgElementTags.Count; ++i)
			{
				var s = m_rgElementTags[i];
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
			for (int i = 0; i < cTabs; ++i)
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
			var sClass = m_sda.MetaDataCache.GetClassName(clid);
			IndentLine();
			if (m_cc == CurrentContext.insideLink)
			{
				sClass = sClass + "Link";
				var targetItem = hvoItem;
				if (obj is ILexSense)
				{
					// We want the link to go to the containing lex entry.
					// This has two advantages: first, the user can see the whole entry, rather than part of it
					// being scrolled off the top of the screen;
					// Secondly, some senses (e.g., of variants) may not be shown in the HTML at all, resulting in bad links (LT-11099)
					targetItem = ((ILexSense) obj).Entry.Hvo;
				}
				m_writer.WriteLine("<{0} target=\"hvo{1}\">", sClass, targetItem);
			}
			else
			{
				if (clid == LexEntryTags.kClassId && m_sFormat == "xhtml")
				{
					WriteEntryLetterHeadIfNeeded(hvoItem);
				}
				else if (clid == ReversalIndexEntryTags.kClassId && m_sFormat == "xhtml")
				{
					WriteReversalLetterHeadIfNeeded(hvoItem);
				}
				m_writer.WriteLine("<{0} id=\"hvo{1}\">", sClass, hvoItem);
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
			}
			if (m_sWsVern == null)
			{
				m_sWsVern = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			}
			WriteLetterHeadIfNeeded(sEntry, m_sWsVern);
		}

		private void WriteLetterHeadIfNeeded(string sEntry, string sWs)
		{
			var sLower = GetLeadChar(Icu.Normalize(sEntry, Icu.UNormalizationMode.UNORM_NFD), sWs);
			var sTitle = Icu.ToTitle(sLower, sWs);
			if (sTitle == m_schCurrent)
			{
				return;
			}
			if (m_schCurrent.Length > 0)
			{
				m_writer.WriteLine("</div>");	// for letData
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
		public string GetLeadChar(string sEntryNFD, string sWs)
		{
			return GetLeadChar(sEntryNFD, sWs, m_mapWsDigraphs, m_mapWsMapChars, m_mapWsIgnorables, m_cache);
		}

		/// <summary>
		/// Get the lead character, either a single character or a composite matching something
		/// in the sort rules.  (We need to support multi-graph letters.  See LT-9244.)
		/// </summary>
		/// <param name="sEntryNFD">The headword to be written next</param>
		/// <param name="sWs">Name of the writing system</param>
		/// <param name="wsDigraphMap">Map of writing system to digraphs already discovered for that ws</param>
		/// <param name="wsCharEquivalentMap">Map of writing system to already discovered character equivalences for that ws</param>
		/// <param name="wsIgnorableCharMap">Map of writing system to ignorable characters for that ws </param>
		/// <param name="cache"></param>
		/// <returns>The character sEntryNFD is being sorted under in the dictionary.</returns>
		public static string GetLeadChar(string sEntryNFD, string sWs,
													Dictionary<string, ISet<string>> wsDigraphMap,
													Dictionary<string, Dictionary<string, string>> wsCharEquivalentMap,
													Dictionary<string, ISet<string>> wsIgnorableCharMap,
													LcmCache cache)
		{
			if (string.IsNullOrEmpty(sEntryNFD))
			{
				return string.Empty;
			}
			var sEntryPre = Icu.ToLower(sEntryNFD, sWs);
			Dictionary<string, string> mapChars;
			// List of characters to ignore in creating letter heads.
			ISet<string> chIgnoreList;
			var sortChars = GetDigraphs(sWs, wsDigraphMap, wsCharEquivalentMap, wsIgnorableCharMap, cache, out mapChars, out chIgnoreList);
			var sEntry = string.Empty;
			if (chIgnoreList != null) // this list was built in GetDigraphs()
			{
				foreach (var ch in sEntryPre)
				{
					if (!chIgnoreList.Contains(ch.ToString(CultureInfo.InvariantCulture)))
					{
						sEntry += ch;
					}
				}
			}
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
			// This loop replaces each occurance of equivalent characters in sEntry
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
						{
							if (kvp.Value.Contains(key1))
							{
								map.Remove(kvp.Key);
								break;
							}
						}
					}
				}
				sEntryT = sEntry;
			} while (fChanged);
			var cnt = GetFirstLetterLength(sEntry);
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

			IntPtr col;
			try
			{
				var icuLocale = Icu.GetName(sWs);
				col = Icu.OpenCollator(icuLocale);
			}
			catch (IcuException)
			{
				return sFirst;
			}
			try
			{
				var ka = Icu.GetSortKey(col, sFirst);
				if (ka.Length > 0 && ka[0] == 1)
				{
					var sT = sEntry.Substring(sFirst.Length);
					return GetLeadChar(sT, sWs, wsDigraphMap, wsCharEquivalentMap, wsIgnorableCharMap, cache);
				}
			}
			finally
			{
				Icu.CloseCollator(col);
			}
			return sFirst;
		}

		/// <returns>
		/// 2 if the first letter in the string is composed of a Surrogate Pair; 1 otherwise
		/// </returns>
		internal static int GetFirstLetterLength(string sEntry)
		{
			return char.IsSurrogatePair(sEntry, 0) ? 2 : 1;
		}

		/// <summary>
		/// Get the set of significant digraphs (multigraphs) for the writing system. At the
		/// moment, these are derived from ICU sorting rules associated with the writing system.
		/// </summary>
		/// <param name="sWs">Name of writing system</param>
		/// <param name="mapChars">Set of character equivalences</param>
		/// <param name="chIgnoreSet">Set of characters to ignore</param>
		/// <returns></returns>
		internal ISet<string> GetDigraphs(string sWs, out Dictionary<string, string> mapChars, out ISet<string> chIgnoreSet)
		{
			return GetDigraphs(sWs, m_mapWsDigraphs, m_mapWsMapChars, m_mapWsIgnorables, m_cache, out mapChars, out chIgnoreSet);
		}

		/// <summary>
		/// Get the set of significant digraphs (multigraphs) for the writing system. At the
		/// moment, these are derived from ICU sorting rules associated with the writing system.
		/// </summary>
		/// <param name="sWs">Name of writing system</param>
		/// <param name="wsDigraphMap">Map of writing system to digraphs already discovered for that ws</param>
		/// <param name="wsCharEquivalentMap">Map of writing system to already discovered character equivalences for that ws</param>
		/// <param name="wsIgnorableCharMap">Map of writing system to ignorable characters for that ws </param>
		/// <param name="cache"></param>
		/// <param name="mapChars">Set of character equivalences</param>
		/// <param name="chIgnoreSet">Set of characters to ignore</param>
		/// <returns></returns>
		internal static ISet<string> GetDigraphs(string sWs,
			Dictionary<string, ISet<string>> wsDigraphMap,
			Dictionary<string, Dictionary<string, string>> wsCharEquivalentMap,
			Dictionary<string, ISet<string>> wsIgnorableCharMap,
			LcmCache cache,
			out Dictionary<string, string> mapChars,
			out ISet<string> chIgnoreSet)
		{
			// Collect the digraph and character equivalence maps and the ignorable character set
			// the first time through. There after, these maps and lists are just retrieved.
			chIgnoreSet = new HashSet<string>(); // if ignorable chars get through they can become letter heads! LT-11172
			ISet<string> digraphs;
			// Are the maps and ignorables already setup for the taking?
			if (wsDigraphMap.TryGetValue(sWs, out digraphs))
			{
				// knows about ws, so already knows character equivalence classes
				mapChars = wsCharEquivalentMap[sWs];
				chIgnoreSet = wsIgnorableCharMap[sWs];
				return digraphs;
			}
			digraphs = new HashSet<string>();
			mapChars = new Dictionary<string, string>();
			var ws = cache.ServiceLocator.WritingSystemManager.Get(sWs);

			wsDigraphMap[sWs] = digraphs;

			var simpleCollation = ws.DefaultCollation as SimpleRulesCollationDefinition;
			if (simpleCollation != null)
			{
				if (!string.IsNullOrEmpty(simpleCollation.SimpleRules))
				{
					var rules = simpleCollation.SimpleRules.Replace(" ", "=");
					var primaryParts = rules.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
					foreach (var part in primaryParts)
					{
						BuildDigraphSet(part, sWs, wsDigraphMap);
						MapRuleCharsToPrimary(part, sWs, wsCharEquivalentMap);
					}
				}
			}
			else
			{
				// is this a custom ICU collation?
				var icuCollation = ws.DefaultCollation as IcuRulesCollationDefinition;
				if (!string.IsNullOrEmpty(icuCollation?.IcuRules))
				{
					// prime with empty ws in case all the rules affect only the ignore set
					wsCharEquivalentMap[sWs] = mapChars;
					var individualRules = icuCollation.IcuRules.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var individualRule in individualRules)
					{
						string[] primaryParts;
						var rule = individualRule;
						RemoveICUEscapeChars(ref rule);
						// This is a valid rule that specifies that the digraph aa should be ignored
						// [last tertiary ignorable] = \u02bc = aa
						// This may never happen, but some single characters should be ignored or they will
						// will be confused for digraphs with following characters.)))
						if (rule.Contains("["))
						{
							RemoveICUEscapeChars(ref rule);
							// This is a valid rule that specifies that the digraph aa should be ignored
							// [last tertiary ignorable] = \u02bc = aa
							// This may never happen, but some single characters should be ignored or they will
							// will be confused for digraphs with following characters.)))
							if (rule.Contains("["))
							{
								rule = ProcessAdvancedSyntacticalElements(chIgnoreSet, rule);
							}
							if (string.IsNullOrEmpty(rule.Trim()))
							{
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
									switch (rule.Substring(ruleIndex, 1))
									{
										case "/":
											isExpansion = true;
											break;
										case "=":
										case "<":
											isExpansion = false;
											break;
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
							// There are other issues we are not handling proplerly such as the next line
							// &N<\u006e\u0067
							primaryParts = rule.Split('<');
							foreach (var part in primaryParts)
							{
								if (rule.Contains("<"))
								{
									BuildDigraphSet(part, sWs, wsDigraphMap);
								}
								MapRuleCharsToPrimary(part, sWs, wsCharEquivalentMap);
							}
						}
						if (string.IsNullOrEmpty(rule.Trim()))
						{
							continue;
						}
						rule = rule.Replace("<<<", "=");
						rule = rule.Replace("<<", "=");
						// "&N<ng<<<Ng<ny<<<Ny" => "&N<ng=Ng<ny=Ny"
						// "&N<�<<<�" => "&N<�=�"
						// There are other issues we are not handling proplerly such as the next line
						// &N<\u006e\u0067
						primaryParts = rule.Split('<');
						foreach (var part in primaryParts)
						{
							BuildDigraphSet(part, sWs, wsDigraphMap);
							MapRuleCharsToPrimary(part, sWs, wsCharEquivalentMap);
						}
					}
				}
			}

			// This at least prevents null reference and key not found exceptions.
			// Possibly we should at least map the ASCII LC letters to UC.
			if (!wsCharEquivalentMap.TryGetValue(sWs, out mapChars))
			{
				wsCharEquivalentMap[sWs] = mapChars = new Dictionary<string, string>();
			}

			wsIgnorableCharMap.Add(sWs, chIgnoreSet);
			return digraphs;
		}

		private static string ProcessAdvancedSyntacticalElements(ISet<string> chIgnoreSet, string rule)
		{
			const string ignorableEndMarker = "ignorable] = ";
			const string beforeBegin = "[before ";
			// parse out the ignorables and add them to the ignore list
			var ignorableBracketEnd = rule.IndexOf(ignorableEndMarker);
			if(ignorableBracketEnd > -1)
			{
				ignorableBracketEnd += ignorableEndMarker.Length; // skip over the search target
				var chars = rule.Substring(ignorableBracketEnd).Split(new[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
				if(chars.Length > 0)
				{
					foreach (var ch in chars)
					{
						chIgnoreSet.Add(ch);
					}
				}
				// the ignorable section could be at the end of other parts of a rule so strip it off the end
				rule = rule.Substring(0, rule.IndexOf("["));
			}
			// check for before rules
			var beforeBeginLoc = rule.IndexOf(beforeBegin);
			if(beforeBeginLoc != -1)
			{
				const string primaryBeforeEnd = "1]";
				// [before 1] is for handling of primary characters which this code is concerned with
				// so we just strip it off and let the rest of the rule get processed.
				var beforeEndLoc = rule.IndexOf(primaryBeforeEnd, beforeBeginLoc);
				if(beforeEndLoc == -1)
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
				sGraph = Icu.Normalize(sGraph, Icu.UNormalizationMode.UNORM_NFD);
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

		private static void BuildDigraphSet(string part, string ws, Dictionary<string, ISet<string>> wsDigraphsMap)
		{
			foreach (var character in part.Split('='))
			{
				var sGraph = character.Trim();
				if (string.IsNullOrEmpty(sGraph))
				{
					continue;
				}
				sGraph = Icu.Normalize(sGraph, Icu.UNormalizationMode.UNORM_NFD);
				if (sGraph.Length <= 1)
				{
					continue;
				}
				sGraph = Icu.ToLower(sGraph, ws);
				if (!wsDigraphsMap.ContainsKey(ws))
				{
					wsDigraphsMap.Add(ws, new HashSet<string> { sGraph });
				}
				else
				{
					if (!wsDigraphsMap[ws].Contains(sGraph))
					{
						wsDigraphsMap[ws].Add(sGraph);
					}
				}
			}
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
			sRule = Regex.Replace(sRule,@"\\u(?<Value>[a-zA-Z0-9]{4})", m => ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString(CultureInfo.InvariantCulture));
		}

		private void WriteReversalLetterHeadIfNeeded(int hvoItem)
		{
			var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem);
			var objOwner = obj.Owner;
			if (!(objOwner is IReversalIndex))
			{
				return;		// subentries shouldn't trigger letter head change!
			}

			var entry = (IReversalIndexEntry) obj;
			var idx = (IReversalIndex) objOwner;
			var ws = m_cache.ServiceLocator.WritingSystemManager.Get(idx.WritingSystem);
			var sEntry = entry.ReversalForm.get_String(ws.Handle).Text;
			if (string.IsNullOrEmpty(sEntry))
			{
				return;
			}

			if (string.IsNullOrEmpty(m_sWsRevIdx))
			{
				m_sWsRevIdx = ws.Id;
			}
			WriteLetterHeadIfNeeded(sEntry, m_sWsRevIdx);
		}

		private void WriteClassEndTag(int hvoItem, CurrentContext ccOld)
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
				var mdc = m_sda.GetManagedMetaDataCache();
				var cpt = (CellarPropertyType)mdc.GetFieldType(flid);
				var sField = mdc.GetFieldName((int)flid);
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
				sXml = GetFieldXmlElementName(sField, flid/1000);
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
							sUserLabel = m_sda.MetaDataCache.GetFieldLabel((int)flid);
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
				sClass = m_sda.MetaDataCache.GetClassName(clid);
			}
			string sXml;
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
		/// <param name="input"></param>
		/// <returns></returns>
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

		private string MakeValueForXmlElement(Char c)
		{
			return c == ' ' ? "_" : $"{Convert.ToInt32(c):x}";
		}

		private void WriteFieldEndTag(int flid, CurrentContext ccOld)
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
				if (m_sda.MetaDataCache.get_IsVirtual((int) flid))
				{
					return string.Empty;
				}

				var sDestClass = m_sda.MetaDataCache.GetDstClsName((int)flid);
				if (m_cc == CurrentContext.insideLink)
				{
					sDestClass = sDestClass + "Link";
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
		/// <param name="sDataType"></param>
		public void Finish(string sDataType)
		{
			if (m_sFormat == "xhtml")
			{
				if (m_schCurrent.Length > 0)
				{
					m_writer.WriteLine("</div>");	// for letData
				}
				m_xhtml.WriteXhtmlEnding();
			}
			else
			{
				m_writer.WriteLine("</{0}>", sDataType);
			}
			m_writer.Close();
			m_writer = null;
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
			var tag = XmlUtils.GetOptionalAttributeValue(delimitNode, "number"); // optional normally, but then this is not called
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
			var before = tag.Substring(0, ich); // arbitratily it is all 'before' if no %.
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

		private string m_delayedItemNumberClass;
		private ITsString m_delayedItemNumberValue;

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
			string cssClass;
			if (!m_mapXnToCssClass.TryGetValue(frag, out cssClass))
			{
				cssClass = XmlUtils.GetOptionalAttributeValue(frag, "css", null);
				// Note that an emptry string for cssClass means we explicitly don't want to output anything.
				if (cssClass == null && frag.Parent != null && frag.Parent.Name == "layout" &&
					(XmlUtils.GetOptionalAttributeValue(frag, "before") != null ||
					 XmlUtils.GetOptionalAttributeValue(frag, "after") != null ||
					 XmlUtils.GetOptionalAttributeValue(frag, "sep") != null ||
					 XmlUtils.GetOptionalAttributeValue(frag, "number") != null ||
					 XmlUtils.GetOptionalAttributeValue(frag, "style") != null))
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
					XElement oldNode;
					if (m_xhtml.TryGetNodeFromCssClass(cssClass, out oldNode))
					{
						// Trouble: we have some other node using the same style. This can legitimately happen
						// if we output the same part with different writing systems selected, so deal with that.
						// Generate a (hopefully) unique cssClass.
						var wsOld = XmlUtils.GetOptionalAttributeValue(oldNode, "ws");
						var wsNew = XmlUtils.GetOptionalAttributeValue(frag, "ws");
						if (!string.IsNullOrEmpty(wsOld) && !string.IsNullOrEmpty(wsNew) && wsOld != wsNew)
						{
							var tryCssClass = cssClass + "-" + wsNew;
							if (m_xhtml.TryGetNodeFromCssClass(tryCssClass, out oldNode))
							{
								// another level of duplicate!
								throw new FwConfigurationException("Two distinct XML nodes are using the same cssClass (" + cssClass
									+ ") and writing system (" + wsNew + ") in the same export:"
									+ Environment.NewLine + oldNode + Environment.NewLine + frag);
							}
							cssClass = tryCssClass; // This is a unique key we will use as the css class for these items.
						}
						else
						{
							throw new FwConfigurationException("Two distinct XML nodes are using the same cssClass (" + cssClass
								+ ") in the same export with the same (or no) writing system:"
								+ Environment.NewLine + oldNode + Environment.NewLine + frag);
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
					if (flowType == "div" || flowType == "para")
					{
						m_writer.WriteLine("<div class=\"{0}\">", m_xhtml.GetValidCssClassName(cssClass));
					}
					else if (flowType != "divInPara")
					{
						m_writer.WriteLine("<span class=\"{0}\">", m_xhtml.GetValidCssClassName(cssClass));
					}
				}
			}
		}

		private static string GetFlowType(XElement frag)
		{
			var flowType = XmlUtils.GetOptionalAttributeValue(frag, "flowType", null);
			if (flowType != null)
			{
				return flowType;
			}
			var fShowAsPara = XmlUtils.GetOptionalBooleanAttributeValue(frag, "showasindentedpara", false);
			return fShowAsPara ? "para" : null;
		}

		/// <summary>
		/// Make this string safe to place inside an XML comment.
		/// </summary>
		private string CommentProtect(string str)
		{
			return str.Replace("-", "\\-");
		}

		internal void EndCssClassIfNeeded(XElement frag)
		{
			if (m_sFormat != "xhtml")
			{
				return;
			}
			string cssClass;
			if (!m_mapXnToCssClass.TryGetValue(frag, out cssClass) || string.IsNullOrEmpty(cssClass))
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
				List<string> envirs;
				if (m_xhtml.MapCssToStyleEnv(cssClass, out envirs))
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
		/// <param name="sOutputFile"></param>
		/// <param name="allowDictionaryParagraphIndent">See comments on this property of XhtmlHelper</param>
		/// <param name="vss"></param>
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
	}

	/// <summary />
	public delegate void ProgressHandler(object sender);
}
