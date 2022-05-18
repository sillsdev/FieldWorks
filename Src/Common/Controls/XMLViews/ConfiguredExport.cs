// Copyright (c) 2006-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Diagnostics;
using System.Linq;
using Icu.Collation;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Utils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.Common.Framework;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using SIL.Utils;
using SIL.WritingSystems;
using XCore;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Summary description for ConfiguredExport.
	/// </summary>
	public class ConfiguredExport : CollectorEnv, ICollectPicturePathsOnly
	{
		private TextWriter m_writer = null;
		private LcmCache m_cache = null;
		private LcmStyleSheet m_stylesheet;
		private string m_sFormat = null;
		private StringCollection m_rgElementTags = new StringCollection();
		private StringCollection m_rgClassNames = new StringCollection();

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

		private CurrentContext m_cc = CurrentContext.unknown;
		private string m_sTimeField = null;

		Dictionary<int, string> m_dictWsStr = new Dictionary<int,string>();
		/// <summary>The current lead (sort) character being written.</summary>
		private string m_schCurrent = String.Empty;
		/// <summary>
		/// Map from a writing system to its set of digraphs (or multigraphs) used in sorting.
		/// </summary>
		Dictionary<string, Dictionary<string, CollationLevel>> m_mapWsDigraphs = new Dictionary<string, Dictionary<string, CollationLevel>>();
		/// <summary>
		/// Map from a writing system to its map of equivalent graphs/multigraphs used in sorting.
		/// </summary>
		Dictionary<string, Dictionary<string, string>> m_mapWsMapChars = new Dictionary<string, Dictionary<string, string>>();
		/// <summary>
		/// Map of characters to ignore for writing systems
		/// </summary>
		Dictionary<string, ISet<string>> m_mapWsIgnorables = new Dictionary<string, ISet<string>>();

		private CoreWritingSystemDefinition m_wsVern;
		private CoreWritingSystemDefinition m_wsRevIdx;
		Dictionary<int, string> m_dictCustomUserLabels = new Dictionary<int, string>();
		string m_sActiveParaStyle;
		Dictionary<XmlNode, string> m_mapXnToCssClass = new Dictionary<XmlNode, string>();
		private XhtmlHelper m_xhtml;
		private XhtmlHelper.CssType m_cssType = XhtmlHelper.CssType.Dictionary;
		private Dictionary<string, Collator> m_wsCollators = new Dictionary<string, Collator>();

		private bool m_fCancel = false;

		/// <summary>
		///
		/// </summary>
		public delegate void ProgressHandler(object sender);

		/// <summary>
		///
		/// </summary>
		public event ProgressHandler UpdateProgress;

		#region construction and initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ConfiguredExport"/> class.
		/// </summary>
		/// <param name="baseEnv">The base env.</param>
		/// <param name="sda">Data access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display, if m_baseEnv is null.
		/// If baseEnv is not null, hvoRoot is ignored.</param>
		/// ------------------------------------------------------------------------------------
		public ConfiguredExport(IVwEnv baseEnv, ISilDataAccess sda, int hvoRoot)
			: base(baseEnv, sda, hvoRoot)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the object with some useful information, and write the initial
		/// element start tag to the output.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Initialize(LcmCache cache, PropertyTable propertyTable, TextWriter w, string sDataType,
			string sFormat, string sOutPath, string sBodyClass)
		{
			m_writer = w;
			m_cache = cache;
			m_stylesheet = Widgets.FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);
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
			m_cssType = (sBodyClass == "notebookBody") ?
				XhtmlHelper.CssType.Notebook : XhtmlHelper.CssType.Dictionary;
		}
		#endregion

		#region IVwEnv methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the obj.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void AddObj(int hvoItem, IVwViewConstructor vc, int frag)
		{
			CurrentContext ccOld = WriteClassStartTag(hvoItem);

			WriteDelayedItemNumber();

			base.AddObj(hvoItem, vc, frag);

			WriteClassEndTag(ccOld);

			if (m_fCancel)
				throw new CancelException(XMLViewsStrings.ConfiguredExportHasBeenCancelled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the obj prop.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="vc">The vc.</param>
		/// <param name="frag">The frag.</param>
		/// ------------------------------------------------------------------------------------
		public override void AddObjProp(int tag, IVwViewConstructor vc, int frag)
		{
			CurrentContext ccOld = WriteFieldStartTag(tag);

			//m_writer.WriteLine("<!-- AddObjProp: hvo={0}, tag={1}, frag={2} -->", CurrentObject(), tag, frag);
			WriteDestClassStartTag(tag);

			base.AddObjProp(tag, vc, frag);

			WriteDestClassEndTag(tag);

			WriteFieldEndTag(ccOld);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the obj vec.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="vc">The vc.</param>
		/// <param name="frag">The frag.</param>
		/// ------------------------------------------------------------------------------------
		public override void AddObjVec(int tag, IVwViewConstructor vc, int frag)
		{
			CurrentContext ccOld = WriteFieldStartTag(tag);

			base.AddObjVec(tag, vc, frag);

			WriteFieldEndTag(ccOld);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the obj vec items.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="vc">The vc.</param>
		/// <param name="frag">The frag.</param>
		/// ------------------------------------------------------------------------------------
		public override void AddObjVecItems(int tag, IVwViewConstructor vc, int frag)
		{
			CurrentContext ccOld = WriteFieldStartTag(tag);
			OpenProp(tag);
			int cobj = DataAccess.get_VecSize(CurrentObject(),tag);

			for (int i = 0; i < cobj; i++)
			{
				int hvoItem = DataAccess.get_VecItem(CurrentObject(), tag, i);
				OpenTheObject(hvoItem, i);
				CurrentContext ccPrev = WriteClassStartTag(hvoItem);

				vc.Display(this, hvoItem, frag);

				WriteClassEndTag(ccPrev);
				CloseTheObject();
				if (Finished)
					break;
				if (m_fCancel)
					throw new CancelException(XMLViewsStrings.ConfiguredExportHasBeenCancelled);
			}

			CloseProp();
			WriteFieldEndTag(ccOld);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the prop.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="vc">The vc.</param>
		/// <param name="frag">The frag.</param>
		/// ------------------------------------------------------------------------------------
		public override void AddProp(int tag, IVwViewConstructor vc, int frag)
		{
			CurrentContext ccOld = WriteFieldStartTag(tag);

			base.AddProp(tag, vc, frag);

			WriteFieldEndTag(ccOld);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddStringProp
		/// </summary>
		/// <param name="tag">tag</param>
		/// <param name="_vwvc">_vwvc</param>
		/// ------------------------------------------------------------------------------------
		public override void AddStringProp(int tag, IVwViewConstructor _vwvc)
		{
			CurrentContext ccOld = WriteFieldStartTag(tag);

			ITsString tss = DataAccess.get_StringProp(CurrentObject(), tag);
			WriteTsString(tss, TabsToIndent());

			WriteFieldEndTag(ccOld);
		}

		private string WritingSystemId(int ws)
		{
			if (ws == 0)
				return String.Empty;
			string sWs;
			if (m_dictWsStr.TryGetValue(ws, out sWs))
				return sWs;
			sWs = m_cache.WritingSystemFactory.GetStrFromWs(ws);
			sWs = XmlUtils.MakeSafeXmlAttribute(sWs);
			m_dictWsStr.Add(ws, sWs);
			return sWs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddUnicodeProp
		/// </summary>
		/// <param name="tag">tag</param>
		/// <param name="ws">ws</param>
		/// <param name="_vwvc">_vwvc</param>
		/// ------------------------------------------------------------------------------------
		public override void AddUnicodeProp(int tag, int ws, IVwViewConstructor _vwvc)
		{
			CurrentContext ccOld = WriteFieldStartTag(tag);
			string sText = DataAccess.get_UnicodeProp(CurrentObject(), tag);
			var icuNormalizer = CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFC);
			// Need to ensure that sText is NFC for export.
			if (!icuNormalizer.IsNormalized(sText))
				sText = icuNormalizer.Normalize(sText);
			string sWs = WritingSystemId(ws);
			IndentLine();
			if (String.IsNullOrEmpty(sWs))
				m_writer.WriteLine("<Uni>{0}</Uni>", XmlUtils.MakeSafeXml(sText));
			else
				m_writer.WriteLine("<AUni ws=\"{0}\">{1}</AUni>",
					sWs, XmlUtils.MakeSafeXml(sText));
			WriteFieldEndTag(ccOld);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddStringAltMember
		/// </summary>
		/// <param name="tag">tag</param>
		/// <param name="ws">ws</param>
		/// <param name="_vwvc">_vwvc</param>
		/// ------------------------------------------------------------------------------------
		public override void AddStringAltMember(int tag, int ws, IVwViewConstructor _vwvc)
		{
			CurrentContext ccOld = WriteFieldStartTag(tag);

			ITsString tss = DataAccess.get_MultiStringAlt(CurrentObject(), tag, ws);
			WriteTsString(tss, TabsToIndent());
			// See if the string uses any styles that require us to export some more data.
			for (int irun = 0; irun < tss.RunCount; irun++)
			{
				var style = tss.get_StringProperty(irun, (int) FwTextPropType.ktptNamedStyle);
				int wsRun = tss.get_WritingSystem(irun);
				switch (style)
				{
					case "Sense-Reference-Number":
						if (m_xhtml != null)
						{
							m_xhtml.MapCssToLang("xsensexrefnumber", m_cache.ServiceLocator.WritingSystemManager.Get(wsRun).Id);
						}
						break;
				}
			}

			WriteFieldEndTag(ccOld);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Member AddIntProp
		/// </summary>
		/// <param name="tag">tag</param>
		/// ------------------------------------------------------------------------------------
		public override void AddIntProp(int tag)
		{
			CurrentContext ccOld = WriteFieldStartTag(tag);

			int n = m_cache.DomainDataByFlid.get_IntProp(CurrentObject(), tag);
			IndentLine();
			m_writer.WriteLine("<Integer val=\"{0}\"/>", n);

			WriteFieldEndTag(ccOld);
		}

		/// <summary>
		/// This implementation depend on the details of how XmlVc.cs handles "datetime" type data
		/// in ProcessFrag().
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="flags"></param>
		public override void AddTimeProp(int tag, uint flags)
		{
			string sField = m_sda.MetaDataCache.GetFieldName(tag);
			m_sTimeField = GetFieldXmlElementName(sField, tag/1000);
		}

		/// <summary>
		/// Write a TsString.  This is used in writing out a GenDate.
		/// </summary>
		public override void AddTsString(ITsString tss)
		{
			CellarPropertyType cpt = (CellarPropertyType)m_mdc.GetFieldType(m_tagCurrent);
			if (cpt == CellarPropertyType.GenDate)
				WriteTsString(tss, TabsToIndent());
			else
				base.AddTsString(tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the string.
		/// </summary>
		/// <param name="tss">The TSS.</param>
		/// ------------------------------------------------------------------------------------
		public override void AddString(ITsString tss)
		{
			string sElement;
			if (m_sTimeField != null && m_sTimeField.Length != 0)
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
			string xml = TsStringSerializer.SerializeTsStringToXml(tss, m_cache.WritingSystemFactory, writeObjData: false, indent: true);
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
			if (sp != (int)FwTextPropType.ktptNamedStyle)
				return;
			if (m_stylesheet == null)
				return;
			var style = m_stylesheet.FindStyle(bstrValue);
			if (style == null)
				return;
			if (style.Type == StyleType.kstParagraph)
				m_stylePara = bstrValue;
		}
		#endregion

		#region Other CollectorEnv methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Optionally apply an XSLT to the output file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void PostProcess(string sXsltFile, string sOutputFile, int iPass)
		{
			if (!String.IsNullOrEmpty(sXsltFile))
			{
				base.PostProcess(sXsltFile, sOutputFile, iPass);
			}
			else if (m_sFormat == "xhtml")
			{
				string sTempFile = RenameOutputToPassN(sOutputFile, iPass);
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
					++cTabs;
			}
			return cTabs;
		}

		private void IndentLine()
		{
			int cTabs = TabsToIndent();
			for (int i = 0; i < cTabs; ++i)
				m_writer.Write("    ");
		}

		private CurrentContext WriteClassStartTag(int hvoItem)
		{
			CurrentContext ccOld = m_cc;
			if (m_cc != CurrentContext.insideLink)
				m_cc = CurrentContext.insideObject;

			var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem);
			int clid = obj.ClassID;
			string sClass = m_sda.MetaDataCache.GetClassName(clid);
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
						targetGuid = lexSense.Entry.Guid;
					else
						targetHvo = lexSense.Entry.Hvo;
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
					m_writer.WriteLine("<{0} id=\"hvo{1}\">", sClass, hvoItem);
			}
			m_rgElementTags.Add(sClass);
			m_rgClassNames.Add(sClass);
			return ccOld;
		}

		private void WriteEntryLetterHeadIfNeeded(int hvoItem)
		{
			string sEntry = StringServices.ShortName1Static(m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetObject(hvoItem));
			if (string.IsNullOrEmpty(sEntry))
				return;
			if (m_wsVern == null)
				m_wsVern = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			WriteLetterHeadIfNeeded(sEntry, m_wsVern);
		}

		private void WriteLetterHeadIfNeeded(string sEntry, CoreWritingSystemDefinition ws)
		{
			string sLower = GetLeadChar(CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(sEntry), ws.Id);
			string sTitle = new CaseFunctions(ws).ToTitle(sLower);
			if (sTitle != m_schCurrent)
			{
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
			}
		}

		/// <summary>
		/// Get the lead character, either a single character or a composite matching something
		/// in the sort rules.  (We need to support multi-graph letters.  See LT-9244.)
		/// </summary>
		public string GetLeadChar(string headwordNFD, string sWs)
		{
			var sortKeyCollator = GetCollator(sWs);
			return GetLeadChar(headwordNFD, sWs, m_mapWsDigraphs, m_mapWsMapChars, m_mapWsIgnorables, sortKeyCollator,
									 m_cache);
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
		/// <returns>The character headwordNFD is being sorted under in the dictionary.</returns>
		public static string GetLeadChar(string headwordNFD, string sWs,
													Dictionary<string, Dictionary<string, CollationLevel>> wsDigraphMap,
													Dictionary<string, Dictionary<string, string>> wsCharEquivalentMap,
													Dictionary<string, ISet<string>> wsIgnorableCharMap,
													Collator sortKeyCollator,
													LcmCache cache)
		{
			if (string.IsNullOrEmpty(headwordNFD))
				return "";
			var ws = cache.ServiceLocator.WritingSystemManager.Get(sWs);
			var cf = new CaseFunctions(ws);
			var headwordLC = cf.ToLower(headwordNFD);
			Dictionary<string, string> mapChars;
			// List of characters to ignore in creating letter heads.
			ISet<string> chIgnoreList;
			ISet<string> sortChars = GetDigraphs(ws, wsDigraphMap, wsCharEquivalentMap, wsIgnorableCharMap, out mapChars, out chIgnoreList);
			if (chIgnoreList != null && chIgnoreList.Any()) // this list was built in GetDigraphs()
			{
				// sort the ignorable set with the longest first to avoid edge case where one ignorable
				// string starts with a shorter ignorable string.
				// eg. 'a' and 'aa'
				var ignorablesLongToShort = from s in chIgnoreList.ToList()
					orderby s.Length descending
					select s;
				foreach (var ignorableString in ignorablesLongToShort)
				{
					// if the headword starts with the ignorable chop it off.
					if (headwordLC.StartsWith(ignorableString))
					{
						headwordLC = headwordLC.Substring(ignorableString.Length);
						break;
					}
				}
			}
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
			}

			// We don't want firstChar for an ignored first character or digraph.
			if (sortKeyCollator != null)
			{
				byte[] ka = sortKeyCollator.GetSortKey(firstChar).KeyData;
				if (ka.Length > 0 && ka[0] == 1)
				{
					return GetLeadChar(headwordLC.Substring(firstChar.Length), sWs, wsDigraphMap, wsCharEquivalentMap, wsIgnorableCharMap, sortKeyCollator, cache);
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
			return GetDigraphs(ws, m_mapWsDigraphs, m_mapWsMapChars, m_mapWsIgnorables, out mapChars,
									 out chIgnoreSet);
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
		internal static ISet<string> GetDigraphs(CoreWritingSystemDefinition ws,
			Dictionary<string, Dictionary<string, CollationLevel>> wsDigraphMap,
			Dictionary<string, Dictionary<string, string>> wsCharEquivalentMap,
			Dictionary<string, ISet<string>> wsIgnorableCharMap,
			out Dictionary<string, string> mapChars,
			out ISet<string> chIgnoreSet)
		{
			var sWs = ws.Id;
			// Collect the digraph and character equivalence maps and the ignorable character set
			// the first time through. There after, these maps and lists are just retrieved.
			chIgnoreSet = new HashSet<string>(); // if ignorable chars get through they can become letter heads! LT-11172
			Dictionary<string, CollationLevel> digraphs;
			// Are the maps and ignorables already setup for the taking?
			if (wsDigraphMap.TryGetValue(sWs, out digraphs))
			{   // knows about ws, so already knows character equivalence classes
				mapChars = wsCharEquivalentMap[sWs];
				chIgnoreSet = wsIgnorableCharMap[sWs];
				return new HashSet<string>(digraphs.Keys);
			}
			digraphs = new Dictionary<string, CollationLevel>();
			mapChars = new Dictionary<string, string>();

			wsDigraphMap[sWs] = digraphs;

			switch (ws.DefaultCollation)
			{
				case SimpleRulesCollationDefinition simpleCollation:
				{
					if (!string.IsNullOrEmpty(simpleCollation.SimpleRules))
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
				case IcuRulesCollationDefinition icuCollation when !string.IsNullOrEmpty(icuCollation.IcuRules):
				{
					// prime with empty ws in case all the rules affect only the ignore set
					wsCharEquivalentMap[sWs] = mapChars;
					string[] individualRules = icuCollation.IcuRules.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < individualRules.Length; ++i)
					{
						var rule = individualRules[i];
						// prepare rule for parsing by dropping certain whitespace and handling ICU Escape chars
						NormalizeRule(ref rule);
						// This is a valid rule that specifies that the digraph aa should be ignored
						// [last tertiary ignorable] = \u02bc = aa
						// This may never happen, but some single characters should be ignored or they will
						// will be confused for digraphs with following characters.)))
						if (rule.Contains("["))
						{
							rule = ProcessAdvancedSyntacticalElements(chIgnoreSet, rule);
						}
						if (string.IsNullOrEmpty(rule.Trim()))
							continue;
						rule = rule.Replace("<<<", "=");
						rule = rule.Replace("<<", "=");

						// If the rule contains one or more expansions ('/') remove the expansion portions
						if (rule.Contains("/"))
						{
							bool isExpansion = false;
							var newRule = new StringBuilder();
							for (var ruleIndex = 0; ruleIndex <= rule.Length - 1; ruleIndex++)
							{
								if (rule.Substring(ruleIndex, 1) == "/")
									isExpansion = true;
								else if (rule.Substring(ruleIndex, 1) == "=" || rule.Substring(ruleIndex, 1)== "<")
									isExpansion = false;

								if (!isExpansion)
									newRule.Append(rule.Substring(ruleIndex, 1));
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
								BuildDigraphSet(part, ws, wsDigraphMap);
							MapRuleCharsToPrimary(part, sWs, wsCharEquivalentMap);
						}
					}
					break;
				}
			}

			// This at least prevents null reference and key not found exceptions.
			// Possibly we should at least map the ASCII LC letters to UC.
			if (!wsCharEquivalentMap.TryGetValue(sWs, out mapChars))
				wsCharEquivalentMap[sWs] = mapChars = new Dictionary<string, string>();

			wsIgnorableCharMap.Add(sWs, chIgnoreSet);
			return new HashSet<string>(digraphs.Keys);
		}

		private static string ProcessAdvancedSyntacticalElements(ISet<string> chIgnoreSet, string rule)
		{
			const string ignorableEndMarker = "ignorable]";
			const string beforeBegin = "[before ";
			// parse out the ignorables and add them to the ignore list
			int ignorableBracketEnd = rule.IndexOf(ignorableEndMarker);
			if(ignorableBracketEnd > -1)
			{
				ignorableBracketEnd += ignorableEndMarker.Length; // skip over the search target
				var charsToIgnore = rule.Substring(ignorableBracketEnd).Split(new[] { "=" },
					StringSplitOptions.RemoveEmptyEntries);
				if(charsToIgnore.Length > 0)
				{
					foreach(var ch in charsToIgnore)
						chIgnoreSet.Add(ch);
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
				if (String.IsNullOrEmpty(sGraph))
					continue;
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
				if (string.IsNullOrEmpty(sGraph))
					continue;
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
			sRule = sRule.Replace(@"'", String.Empty);
			sRule = sRule.Replace(quoteEscape, @"'");
			sRule = sRule.Replace(@"\\\\", slashEscape);
			sRule = sRule.Replace(@"\\", String.Empty);
			sRule = sRule.Replace(slashEscape, @"\\");
			ReplaceICUUnicodeEscapeChars(ref sRule);
		}

		private static void ReplaceICUUnicodeEscapeChars(ref string sRule)
		{
			sRule = Regex.Replace(sRule,@"\\u(?<Value>[a-zA-Z0-9]{4})",
										 m => ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString(CultureInfo.InvariantCulture));
		}

		private void WriteReversalLetterHeadIfNeeded(int hvoItem)
		{
			var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem);
			var objOwner = obj.Owner;
			if (!(objOwner is IReversalIndex))
				return;		// subentries shouldn't trigger letter head change!

			var entry = (IReversalIndexEntry) obj;
			var idx = (IReversalIndex) objOwner;
			if (m_wsRevIdx == null)
				m_wsRevIdx = m_cache.ServiceLocator.WritingSystemManager.Get(idx.WritingSystem);
			string sEntry = entry.ReversalForm.get_String(m_wsRevIdx.Handle).Text;
			if (string.IsNullOrEmpty(sEntry))
				return;

			WriteLetterHeadIfNeeded(sEntry, m_wsRevIdx);
		}

		private void WriteClassEndTag(CurrentContext ccOld)
		{
			m_cc = ccOld;

			int iTop = m_rgElementTags.Count - 1;
			string sClass = m_rgElementTags[iTop];
			m_rgElementTags.RemoveAt(iTop);
			IndentLine();
			m_writer.WriteLine("</{0}>", sClass);

			iTop = m_rgClassNames.Count - 1;
			m_rgClassNames.RemoveAt(iTop);
			if (UpdateProgress != null && m_rgClassNames.Count == 0)
				UpdateProgress(this);
		}

		private CurrentContext WriteFieldStartTag(int flid)
		{
			CurrentContext ccOld = m_cc;
			string sXml;
			try
			{
				IFwMetaDataCacheManaged mdc = (IFwMetaDataCacheManaged)m_sda.MetaDataCache;
				CellarPropertyType cpt = (CellarPropertyType)mdc.GetFieldType(flid);
				string sField = mdc.GetFieldName((int)flid);
				switch (cpt)
				{
					case CellarPropertyType.ReferenceAtomic:
						// Don't treat the Self property as starting a link (or a property).
						// View it as just a continuation of the current state.  (See FWR-1673.)
						if (sField != "Self" || !mdc.get_IsVirtual(flid))
							m_cc = CurrentContext.insideLink;
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
					sXml = String.Empty;
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
					if (String.IsNullOrEmpty(sUserLabel))
						m_writer.WriteLine("<{0}>", sXml);
					else
						m_writer.WriteLine("<{0} userlabel=\"{1}\">", sXml, sUserLabel);
				}
			}
			catch
			{
				sXml = String.Empty;
			}
			m_rgElementTags.Add(sXml);

			return ccOld;
		}

		private void AddMissingObjectLink()
		{
			if (m_sFormat == "xhtml")
				return;
			int iTopClass = m_rgClassNames.Count - 1;
			int iTopElem = m_rgElementTags.Count - 1;
			if (iTopClass < 0 || iTopElem < 0)
				return;
			string sClass = m_rgClassNames[iTopClass];
			string sElement = m_rgElementTags[iTopElem];
			if (sClass != sElement)
				return;
			if (!String.IsNullOrEmpty(sClass))
				return;
			int hvo = CurrentObject();
			if (hvo == 0)
				return;
			try
			{
				sClass = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo).ClassName + "Link";
				string sOut = String.Format("<{0} target=\"hvo{1}\">", sClass, hvo);
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
				sClass = m_sda.MetaDataCache.GetClassName(clid);
			string sXml;
			string sClass2 = String.Empty;
			int iTopClass = m_rgClassNames.Count - 1;
			if (iTopClass >= 0)
				sClass2 = m_rgClassNames[iTopClass];
			var safeField = MakeStringValidXmlElement(sField);
			if (sClass != null && sClass.Length != 0)
				sXml = String.Format("{0}_{1}", sClass, safeField);
			else
				sXml = String.Format("{0}_{1}", sClass2, safeField);
			return sXml;
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
					return result; // We can't find an illegal character, string is good.
				result = match.Groups[1].Value + MakeValueForXmlElement(match.Groups[2].Value[0]) + match.Groups[3].Value;
			}
		}

		private string MakeValueForXmlElement(Char c)
		{
			if (c == ' ')
				return "_"; // friendlier than hex.
			return string.Format("{0:x}", Convert.ToInt32(c));
		}

		private void WriteFieldEndTag(CurrentContext ccOld)
		{
			m_cc = ccOld;

			int iTop = m_rgElementTags.Count - 1;
			string sField = m_rgElementTags[iTop];
			m_rgElementTags.RemoveAt(iTop);
			if (sField != null && sField.Length > 0)
			{
				IndentLine();
				m_writer.WriteLine("</{0}>", sField);
			}
		}

		private void WriteDestClassStartTag(int flid)
		{
			string sDestClass = WriteDestClassTag(flid, "<{0}>");
			m_rgElementTags.Add(sDestClass);
			m_rgClassNames.Add(sDestClass);
		}

		private void WriteDestClassEndTag(int flid)
		{
			int iTop = m_rgElementTags.Count - 1;
			string sElem = m_rgElementTags[iTop];
			m_rgElementTags.RemoveAt(iTop);

			string sDestClass = WriteDestClassTag(flid, "</{0}>");

			if (String.IsNullOrEmpty(sDestClass) && !String.IsNullOrEmpty(sElem))
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
			string sClass = m_rgClassNames[iTop];
			m_rgClassNames.RemoveAt(iTop);
			Debug.Assert(sClass == sElem);
		}

		private string WriteDestClassTag(int flid, string sFmt)
		{
			try
			{
				if (m_sda.MetaDataCache.get_IsVirtual((int)flid))
					return String.Empty;

				string sDestClass = m_sda.MetaDataCache.GetDstClsName((int)flid);
				if (m_cc == CurrentContext.insideLink)
					sDestClass = sDestClass + "Link";
				IndentLine();
				m_writer.WriteLine(sFmt, sDestClass);
				return sDestClass;
			}
			catch
			{
				return String.Empty;
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
					m_writer.WriteLine("</div>");	// for letData
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
		internal void OutputItemNumber(ITsString tss, XmlNode delimitNode)
		{
			if (m_sFormat != "xhtml")
			{
				AddString(tss);
				return;
			}
			string cssClass = XmlUtils.GetOptionalAttributeValue(delimitNode, "cssNumber");
			if (cssClass == null)
			{
				AddString(tss);
				return;
			}
			var tag = XmlUtils.GetAttributeValue(delimitNode, "number"); // optional normally, but then this is not called
			int ich;
			for (ich = 0; ich < tag.Length; ich++)
			{
				if (tag[ich] == '%')
				{
					if (ich == tag.Length - 1 || tag[ich + 1] != '%')
						break;
				}
			}
			string before = tag.Substring(0, ich); // arbitratily it is all 'before' if no %.
			string after = ich < tag.Length - 1 ? tag.Substring(ich + 2) : "";
			if (!m_xhtml.NumberStyles.ContainsKey(cssClass))
			{
				m_xhtml.NumberStyles[cssClass] = new Tuple<string, string>(before, after);
			}
			// Strip of the literal text from the string and make a special element out of it.
			var bldr = tss.GetBldr();
			if (before.Length > 0)
				bldr.Replace(0, before.Length, "", null);
			if (after.Length > 0)
				bldr.Replace(bldr.Length - after.Length, bldr.Length, "", null);
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
				return;
			WriteStringBody("ItemNumber", " class=\"" + m_xhtml.GetValidCssClassName(m_delayedItemNumberClass) + "\"", m_delayedItemNumberValue);
			m_delayedItemNumberValue = null;
			m_delayedItemNumberClass = null;

		}

		internal void BeginCssClassIfNeeded(XmlNode frag)
		{
			if (m_sFormat != "xhtml")
				return;
			string cssClass;
			if (!m_mapXnToCssClass.TryGetValue(frag, out cssClass))
			{
				cssClass = XmlUtils.GetOptionalAttributeValue(frag, "css", null);
				// Note that an emptry string for cssClass means we explicitly don't want to output anything.
				if (cssClass == null && frag.ParentNode != null && frag.ParentNode.Name == "layout" &&
					(XmlUtils.GetOptionalAttributeValue(frag, "before") != null ||
					 XmlUtils.GetOptionalAttributeValue(frag, "after") != null ||
					 XmlUtils.GetOptionalAttributeValue(frag, "sep") != null ||
					 XmlUtils.GetOptionalAttributeValue(frag, "number") != null ||
					 XmlUtils.GetOptionalAttributeValue(frag, "style") != null))
				{
					StringBuilder sb = new StringBuilder(
						XmlUtils.GetOptionalAttributeValue(frag.ParentNode, "class", String.Empty));
					if (sb.Length > 0)
					{
						sb.Append("-");
						sb.Append(XmlUtils.GetOptionalAttributeValue(frag.ParentNode, "name", String.Empty));
						sb.Append("-");
						string sRef = XmlUtils.GetMandatoryAttributeValue(frag, "ref");
						if (sRef == "$child")
							sb.Append(XmlUtils.GetOptionalAttributeValue(frag, "label", String.Empty));
						else
							sb.Append(sRef);
						sb.Replace(" ", "--");
						cssClass = sb.ToString();
					}
				}
				string sDup = XmlUtils.GetOptionalAttributeValue(frag, "dup");
				if (!String.IsNullOrEmpty(cssClass) && !String.IsNullOrEmpty(sDup))
					cssClass += sDup;
				if (!String.IsNullOrEmpty(cssClass) && !cssClass.StartsWith("$fwstyle="))
				{
					XmlNode oldNode;
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
								throw new ConfigurationException("Two distinct XML nodes are using the same cssClass (" + cssClass
									+ ") and writing system (" + wsNew + ") in the same export:"
									+ Environment.NewLine + oldNode.OuterXml + Environment.NewLine + frag.OuterXml);
							}
							cssClass = tryCssClass; // This is a unique key we will use as the css class for these items.
						}
						else
						{
							throw new ConfigurationException("Two distinct XML nodes are using the same cssClass (" + cssClass
								+ ") in the same export with the same (or no) writing system:"
								+ Environment.NewLine + oldNode.OuterXml + Environment.NewLine + frag.OuterXml);
						}
					}
					m_xhtml.MapCssClassToXmlNode(cssClass, frag);
				}
				m_mapXnToCssClass.Add(frag, cssClass);
			}
			if (!String.IsNullOrEmpty(cssClass))
			{
				if (cssClass.StartsWith("$fwstyle="))
				{
					m_sActiveParaStyle = cssClass.Substring(9);
				}
				else
				{
					var flowType = GetFlowType(frag);
					if (flowType == "div" || flowType == "para")
						m_writer.WriteLine("<div class=\"{0}\">", m_xhtml.GetValidCssClassName(cssClass));
					else if (flowType != "divInPara")
						m_writer.WriteLine("<span class=\"{0}\">", m_xhtml.GetValidCssClassName(cssClass));
				}
			}
		}

		private static string GetFlowType(XmlNode frag)
		{
			string flowType = XmlUtils.GetOptionalAttributeValue(frag, "flowType", null);
			if (flowType == null)
			{
				bool fShowAsPara = XmlUtils.GetOptionalBooleanAttributeValue(frag, "showasindentedpara", false);
				if (fShowAsPara)
					return "para";
			}
			return flowType;
		}

		/// <summary>
		/// Make this string safe to place inside an XML comment.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		private string CommentProtect(string str)
		{
			return str.Replace("-", "\\-");
		}

		internal void EndCssClassIfNeeded(XmlNode frag)
		{
			if (m_sFormat != "xhtml")
				return;
			string cssClass;
			if (m_mapXnToCssClass.TryGetValue(frag, out cssClass) && !String.IsNullOrEmpty(cssClass))
			{
				if (cssClass.StartsWith("$fwstyle="))
				{
					m_sActiveParaStyle = null;
				}
				else
				{
					var flowType = GetFlowType(frag);
					if (flowType == "div" || flowType == "para")
						m_writer.WriteLine("</div><!--class=\"{0}\"-->", CommentProtect(cssClass));
					else if (flowType != "divInPara")
						m_writer.WriteLine("</span><!--class=\"{0}\"-->", CommentProtect(cssClass));

					if (!String.IsNullOrEmpty(m_sActiveParaStyle))
					{
						List<string> envirs;
						if (m_xhtml.MapCssToStyleEnv(cssClass, out envirs))
						{
							if (!envirs.Contains(m_sActiveParaStyle))
								envirs.Add(m_sActiveParaStyle);
						}
						else
						{
							envirs = new List<string>();
							envirs.Add(m_sActiveParaStyle);
							m_xhtml.MapCssToStyleEnv(cssClass, envirs);
						}
					}
				}
			}
		}

		internal void BeginMultilingualAlternative(int ws)
		{
			if (m_sFormat == "xhtml")
				m_writer.WriteLine("<Alternative ws=\"{0}\">", WritingSystemId(ws));
		}

		internal void EndMultilingualAlternative()
		{
			if (m_sFormat == "xhtml")
				m_writer.WriteLine("</Alternative>");
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
}
