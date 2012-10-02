// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ConfiguredExport.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Xml;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Summary description for ConfiguredExport.
	/// </summary>
	public class ConfiguredExport : CollectorEnv
	{
		private TextWriter m_writer = null;
		private FdoCache m_cache = null;
		private TextWriterStream m_strm = null;
		private string m_sFormat = null;
		private bool m_fUseRFC4646 = false;
		private StringCollection m_rgElementTags = new StringCollection();
		private StringCollection m_rgClassNames = new StringCollection();

		enum CurrentContext
		{
			unknown = 0,
			insideObject = 1,
			insideProperty = 2,
			insideLink = 3,
		};
		private CurrentContext m_cc = CurrentContext.unknown;
		private string m_sTimeField = null;

		Dictionary<int, string> m_dictWsStr = new Dictionary<int,string>();
		/// <summary>The current lead (sort) character being written.</summary>
		private string m_schCurrent = String.Empty;
		/// <summary>
		/// Map from a writing system to its set of digraphs (or multigraphs) used in sorting.
		/// </summary>
		Dictionary<string, Set<string>> m_mapWsDigraphs = new Dictionary<string, Set<string>>();
		/// <summary>
		/// Map from a writing system to its map of equivalent graphs/multigraphs used in sorting.
		/// </summary>
		Dictionary<string, Dictionary<string, string>> m_mapWsMapChars = new Dictionary<string, Dictionary<string, string>>();

		private string m_sWsVern = null;
		private string m_sWsRevIdx = null;
		Dictionary<int, string> m_dictCustomUserLabels = new Dictionary<int, string>();
		string m_sActiveParaStyle;
		Dictionary<XmlNode, string> m_mapXnToCssClass = new Dictionary<XmlNode, string>();
		private XhtmlHelper m_xhtml;

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
		/// Initializes a new instance of the <see cref="T:ConfiguredExport"/> class.
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
		/// <param name="cache">The cache.</param>
		/// <param name="w">The w.</param>
		/// <param name="sDataType">Type of the s data.</param>
		/// <param name="sFormat">The s format.</param>
		/// <param name="sOutPath">The s out path.</param>
		/// ------------------------------------------------------------------------------------
		public void Initialize(FdoCache cache, TextWriter w, string sDataType, string sFormat,
			string sOutPath)
		{
			m_writer = w;
			m_strm = new TextWriterStream(w);
			m_cache = cache;
			m_sFormat = sFormat.ToLowerInvariant();
			m_fUseRFC4646 = m_sFormat == "xhtml" || m_sFormat == "lift";
			m_xhtml = new XhtmlHelper(w, cache);
			if (m_sFormat == "xhtml")
			{
				m_xhtml.WriteXhtmlHeading(sOutPath, null, "dicBody");
			}
			else
			{
				w.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
				w.WriteLine("<{0}>", sDataType);
			}
		}
		#endregion

		#region IVwEnv methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the obj.
		/// </summary>
		/// <param name="hvoItem">The hvo item.</param>
		/// <param name="vc">The vc.</param>
		/// <param name="frag">The frag.</param>
		/// ------------------------------------------------------------------------------------
		public override void AddObj(int hvoItem, IVwViewConstructor vc, int frag)
		{
			CurrentContext ccOld = WriteClassStartTag(hvoItem);

			base.AddObj(hvoItem, vc, frag);

			WriteClassEndTag(hvoItem, ccOld);

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

			WriteFieldEndTag(tag, ccOld);
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

			WriteFieldEndTag(tag, ccOld);
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

				WriteClassEndTag(hvoItem, ccPrev);
				CloseTheObject();
				if (Finished)
					break;
				if (m_fCancel)
					throw new CancelException(XMLViewsStrings.ConfiguredExportHasBeenCancelled);
			}

			CloseProp();
			WriteFieldEndTag(tag, ccOld);
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

			WriteFieldEndTag(tag, ccOld);
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
			int cchIndent = TabsToIndent() * 4;
			tss.WriteAsXmlExtended(m_strm, m_cache.LanguageWritingSystemFactoryAccessor, cchIndent, 0,
				false, m_fUseRFC4646);

			WriteFieldEndTag(tag, ccOld);
		}

		private string WritingSystemId(int ws)
		{
			if (ws == 0)
				return String.Empty;
			string sWs;
			if (m_dictWsStr.TryGetValue(ws, out sWs))
				return sWs;
			if (m_fUseRFC4646)
			{
				ILgWritingSystem lgws = LgWritingSystem.CreateFromDBObject(m_cache, ws);
				sWs = lgws.RFC4646bis;
			}
			else
			{
				sWs = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
			}
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
			// Need to ensure that sText is NFC for export.
			Icu.InitIcuDataDir();
			if (!Icu.IsNormalized(sText, Icu.UNormalizationMode.UNORM_NFC))
				sText = Icu.Normalize(sText, Icu.UNormalizationMode.UNORM_NFC);
			string sWs = WritingSystemId(ws);
			IndentLine();
			if (String.IsNullOrEmpty(sWs))
				m_writer.WriteLine("<Uni>{0}</Uni>", XmlUtils.MakeSafeXml(sText));
			else
				m_writer.WriteLine("<AUni ws=\"{0}\">{1}</AUni>",
					sWs, XmlUtils.MakeSafeXml(sText));
			WriteFieldEndTag(tag, ccOld);
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
				int cchIndent = TabsToIndent() * 4;
				tss.WriteAsXmlExtended(m_strm, m_cache.LanguageWritingSystemFactoryAccessor, cchIndent,
					ws, false, m_fUseRFC4646);

			WriteFieldEndTag(tag, ccOld);
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

			int n = m_cache.MainCacheAccessor.get_IntProp(CurrentObject(), tag);
			IndentLine();
			m_writer.WriteLine("<Integer val=\"{0}\"/>", n);

			WriteFieldEndTag(tag, ccOld);
		}

		/// <summary>
		/// This implementation depend on the details of how XmlVc.cs handles "datetime" type data
		/// in ProcessFrag().
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="flags"></param>
		public override void AddTimeProp(int tag, uint flags)
		{
			string sField = m_cache.MetaDataCacheAccessor.GetFieldName((uint)tag);
			m_sTimeField = GetFieldXmlElementName(sField, (uint)tag/1000);
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
			IndentLine();
			m_writer.WriteLine("<{0}>", sElement);

			int cchIndent = (TabsToIndent() + 1) * 4;
			tss.WriteAsXmlExtended(m_strm, m_cache.LanguageWritingSystemFactoryAccessor, cchIndent, 0,
				false, m_fUseRFC4646);

			IndentLine();
			m_writer.WriteLine("</{0}>", sElement);
		}

		/// <summary>
		/// Mark the end of a paragraph.
		/// </summary>
		public override void CloseParagraph()
		{
			m_writer.WriteLine("</Paragraph>");
		}

		/// <summary>
		/// Mark the beginning of a paragraph.
		/// </summary>
		public override void OpenParagraph()
		{
			m_writer.WriteLine("<Paragraph>");
		}
		#endregion

		#region Other CollectorEnv methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Optionally apply an XSLT to the output file.
		/// </summary>
		/// <param name="sXsltFile">The s XSLT file.</param>
		/// <param name="sOutputFile">The s output file.</param>
		/// <param name="iPass">The i pass.</param>
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
				File.Delete(sTempFile);
			}
		}
		#endregion

		#region internal methods

		private int TabsToIndent()
		{
			int cTabs = 0;
			for (int i = 0; i < m_rgElementTags.Count; ++i)
			{
				string s = m_rgElementTags[i];
				if (s != null && s.Length > 0)
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

			int clid = m_cache.GetClassOfObject(hvoItem);
			string sClass = m_cache.MetaDataCacheAccessor.GetClassName((uint)clid);
			IndentLine();
			if (m_cc == CurrentContext.insideLink)
			{
				sClass = sClass + "Link";
				m_writer.WriteLine("<{0} target=\"hvo{1}\">", sClass, hvoItem);
			}
			else
			{
				if (clid == LexEntry.kclsidLexEntry && m_sFormat == "xhtml")
					WriteEntryLetterHeadIfNeeded(hvoItem);
				else if (clid == ReversalIndexEntry.kclsidReversalIndexEntry && m_sFormat == "xhtml")
					WriteReversalLetterHeadIfNeeded(hvoItem);
				m_writer.WriteLine("<{0} id=\"hvo{1}\">", sClass, hvoItem);
			}
			m_rgElementTags.Add(sClass);
			m_rgClassNames.Add(sClass);
			return ccOld;
		}

		private void WriteEntryLetterHeadIfNeeded(int hvoItem)
		{
			bool fExclude = m_cache.GetBoolProperty(hvoItem, (int)LexEntry.LexEntryTags.kflidExcludeAsHeadword);
			if (fExclude)
				return;
			string sEntry = LexEntry.ShortName1Static(m_cache, hvoItem);
			if (String.IsNullOrEmpty(sEntry))
				return;
			if (m_sWsVern == null)
				m_sWsVern = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(m_cache.DefaultVernWs);
			WriteLetterHeadIfNeeded(sEntry, m_sWsVern);
		}

		private void WriteLetterHeadIfNeeded(string sEntry, string sWs)
		{
			string sLower = GetLeadChar(Icu.Normalize(sEntry, Icu.UNormalizationMode.UNORM_NFD), sWs);
			string sTitle = Icu.ToTitle(sLower, sWs);
			if (sTitle != m_schCurrent)
			{
				if (m_schCurrent.Length > 0)
					m_writer.WriteLine("</div>");	// for letData
				m_writer.WriteLine("<div class=\"letHead\">");
				StringBuilder sb = new StringBuilder();
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
			}
		}

		/// <summary>
		/// Get the lead character, either a single character or a composite matching something
		/// in the sort rules.  (We need to support multi-graph letters.  See LT-9244.)
		/// </summary>
		private string GetLeadChar(string sEntryNFD, string sWs)
		{
			if (string.IsNullOrEmpty(sEntryNFD))
				return "";
			string sEntry = Icu.ToLower(sEntryNFD, sWs);
			Dictionary<string, string> mapChars;
			Set<string> sortChars = GetDigraphs(sWs, out mapChars);
			string sEntryT = sEntry;
			bool fChanged = false;
			do
			{
				foreach (string key in mapChars.Keys)
					sEntry = sEntry.Replace(key, mapChars[key]);
				fChanged = sEntryT != sEntry;
				sEntryT = sEntry;
			} while (fChanged);
			string sFirst = sEntry.Substring(0, 1);
			foreach (string sChar in sortChars)
			{
				if (sEntry.StartsWith(sChar))
				{
					if (sFirst.Length < sChar.Length)
						sFirst = sChar;
				}
			}
			// We don't want sFirst for an ignored first character or digraph.
			ILgCollatingEngine lce = LgIcuCollatorClass.Create();
			lce.Open(sWs);
			byte[] ka = (byte[])lce.get_SortKeyVariant(sFirst, LgCollatingOptions.fcoDefault);
			if (ka[0] == 1)
			{
				string sT = sEntry.Substring(sFirst.Length);
				return GetLeadChar(sT, sWs);
			}
			return sFirst;
		}

		/// <summary>
		/// Get the set of significant digraphs (multigraphs) for the writing system.  At the
		/// moment, these are derived from ICU sorting rules associated with the writing system.
		/// </summary>
		private Set<string> GetDigraphs(string sWs, out Dictionary<string, string> mapChars)
		{
			Set<string> digraphs = null;
			if (m_mapWsDigraphs.TryGetValue(sWs, out digraphs))
			{
				mapChars = m_mapWsMapChars[sWs];
				return digraphs;
			}
			digraphs = new Set<string>();
			mapChars = new Dictionary<string, string>();
			int ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(sWs);
			IWritingSystem wsX = null;
			ICollation coll = null;
			string sIcuRules = null;
			if (ws > 0)
			{
				wsX = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws);
				if (wsX.CollationCount > 0)
				{
					coll = wsX.get_Collation(0);
					sIcuRules = coll.IcuRules;
					if (String.IsNullOrEmpty(sIcuRules))
					{
						// The ICU rules may not be loaded for built-in languages, but are
						// still helpful for our purposes here.
						string sIcuOrig = sIcuRules;
						coll.LoadIcuRules(sWs);
						sIcuRules = coll.IcuRules;
						coll.IcuRules = sIcuOrig;	// but we don't want to actually change anything!
					}
				}
			}
			if (!String.IsNullOrEmpty(sIcuRules) && sIcuRules.Contains("&"))
			{
				string[] rgsRules = sIcuRules.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < rgsRules.Length; ++i)
				{
					string sRule = rgsRules[i];
					// This is a valid rule that specifies that the digraph aa should be ignored
					// [last tertiary ignorable] = \u02bc = aa
					// but the code here will ignore this. YAGNI the chances of a user specifying a digraph
					// as ignorable may never happen.
					if (sRule.Contains("["))
						sRule = sRule.Substring(0, sRule.IndexOf("["));
					if (String.IsNullOrEmpty(sRule.Trim()))
						continue;
					sRule = sRule.Replace("<<<", "=");
					sRule = sRule.Replace("<<", "=");
					if (sRule.Contains("<"))
					{
						// "&N<ng<<<Ng<ny<<<Ny" => "&N<ng=Ng<ny=Ny"
						// "&N<ñ<<<Ñ" => "&N<ñ=Ñ"
						// There are other issues we are not handling proplerly such as the next line
						// &N<\u006e\u0067
						string[] rgsPieces = sRule.Split(new char[] { '<', '=' }, StringSplitOptions.RemoveEmptyEntries);
						for (int j = 0; j < rgsPieces.Length; ++j)
						{
							string sGraph = rgsPieces[j];
							sGraph = sGraph.Trim();
							if (String.IsNullOrEmpty(sGraph))
								continue;
							sGraph = Icu.Normalize(sGraph, Icu.UNormalizationMode.UNORM_NFD);
							if (sGraph.Length > 1)
							{
								sGraph = Icu.ToLower(sGraph, sWs);
								if (!digraphs.Contains(sGraph))
									digraphs.Add(sGraph);
							}
						}
					}
					else if (sRule.Contains("="))
					{
						// "&ae<<æ<<<Æ" => "&ae=æ=Æ"
						string[] rgsPieces = sRule.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
						string sGraphPrimary = rgsPieces[0].Trim();
						Debug.Assert(!String.IsNullOrEmpty(sGraphPrimary));
						sGraphPrimary = Icu.ToLower(sGraphPrimary, sWs);
						for (int j = 1; j < rgsPieces.Length; ++j)
						{
							string sGraph = rgsPieces[j];
							sGraph = sGraph.Trim();
							if (String.IsNullOrEmpty(sGraph))
								continue;
							sGraph = Icu.Normalize(sGraph, Icu.UNormalizationMode.UNORM_NFD);
							sGraph = Icu.ToLower(sGraph, sWs);
							if (sGraph != sGraphPrimary)
							{
								if (!mapChars.ContainsKey(sGraph))
									mapChars.Add(sGraph, sGraphPrimary);
							}
						}
					}
				}
			}
			m_mapWsDigraphs.Add(sWs, digraphs);
			m_mapWsMapChars.Add(sWs, mapChars);
			return digraphs;
		}

		private void WriteReversalLetterHeadIfNeeded(int hvoItem)
		{
			int hvoOwner = m_cache.GetOwnerOfObject(hvoItem);
			int clidOwner = m_cache.GetClassOfObject(hvoOwner);
			if (clidOwner != ReversalIndex.kclsidReversalIndex)
				return;		// subentries shouldn't trigger letter head change!
			int ws = m_cache.GetIntProperty(hvoOwner, (int)ReversalIndex.ReversalIndexTags.kflidWritingSystem);
			string sEntry = m_cache.GetMultiUnicodeAlt(hvoItem,
				(int)ReversalIndexEntry.ReversalIndexEntryTags.kflidReversalForm,
				ws,
				"ReversalIndexEntry_ReversalForm");
			if (String.IsNullOrEmpty(sEntry))
				return;
			if (String.IsNullOrEmpty(m_sWsRevIdx))
				m_sWsRevIdx = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
			WriteLetterHeadIfNeeded(sEntry, m_sWsRevIdx);
		}

		private void WriteClassEndTag(int hvoItem, CurrentContext ccOld)
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
				int cpt = m_cache.MetaDataCacheAccessor.GetFieldType((uint)flid);
				switch (cpt)
				{
				case (int)CellarModuleDefns.kcptReferenceAtom:
				case (int)CellarModuleDefns.kcptReferenceCollection:
				case (int)CellarModuleDefns.kcptReferenceSequence:
					m_cc = CurrentContext.insideLink;
					break;
				default:
					m_cc = CurrentContext.insideProperty;
					break;
				}
				string sField = m_cache.MetaDataCacheAccessor.GetFieldName((uint)flid);
				sXml = GetFieldXmlElementName(sField, (uint)flid/1000);
				if (sXml == "_" + sField && ccOld == CurrentContext.insideLink && m_cc == CurrentContext.insideProperty)
				{
					AddMissingObjectLink();
					sXml = GetFieldXmlElementName(sField, (uint)flid / 1000);
				}
				IndentLine();
				string sUserLabel = null;
				if (sField.StartsWith("custom"))
				{
					if (!m_dictCustomUserLabels.TryGetValue(flid, out sUserLabel))
					{
						IOleDbCommand odc = null;
						try
						{
							odc = DbOps.MakeRowSet(m_cache,
								String.Format("SELECT UserLabel FROM Field$ WHERE Id={0}", flid), null);
							bool fMoreRows;
							odc.NextRow(out fMoreRows);
							if (fMoreRows)
								sUserLabel = XmlUtils.MakeSafeXmlAttribute(DbOps.ReadString(odc, 0));
						}
						finally
						{
							if (odc != null)
								DbOps.ShutdownODC(ref odc);
						}
						m_dictCustomUserLabels.Add(flid, sUserLabel);
					}
				}
				if (String.IsNullOrEmpty(sUserLabel))
					m_writer.WriteLine("<{0}>", sXml);
				else
					m_writer.WriteLine("<{0} userlabel=\"{1}\">", sXml, sUserLabel);
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
				sClass = m_cache.GetClassName((uint)m_cache.GetClassOfObject(hvo)) + "Link";
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

		private string GetFieldXmlElementName(string sField, uint clid)
		{
			string sClass = null;
			if (clid > 0 && clid < 10000)
			{
				try
				{
					sClass = m_cache.MetaDataCacheAccessor.GetClassName(clid);
				}
				catch
				{
					sClass = null;
				}
			}
			string sXml;
			string sClass2 = String.Empty;
			int iTopClass = m_rgClassNames.Count - 1;
			if (iTopClass >= 0)
				sClass2 = m_rgClassNames[iTopClass];
			if (sClass != null && sClass.Length != 0)
				sXml = String.Format("{0}_{1}", sClass, sField);
			else
				sXml = String.Format("{0}_{1}", sClass2, sField);
			return sXml;
		}

		private void WriteFieldEndTag(int flid, CurrentContext ccOld)
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
				if (m_cache.MetaDataCacheAccessor.get_IsVirtual((uint)flid))
					return String.Empty;

				string sDestClass = m_cache.MetaDataCacheAccessor.GetDstClsName((uint)flid);
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
			m_strm = null;
			m_writer = null;
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
						string sRef = XmlUtils.GetManditoryAttributeValue(frag, "ref");
						if (sRef == "$child")
							sb.Append(XmlUtils.GetOptionalAttributeValue(frag, "label", String.Empty));
						else
							sb.Append(sRef);
						sb.Replace(" ", "--");
						cssClass = sb.ToString();
					}
				}
				string sDup = XmlUtils.GetOptionalAttributeValue(frag, "dup");
				if (!String.IsNullOrEmpty(sDup))
					cssClass += sDup;
				m_mapXnToCssClass.Add(frag, cssClass);
				if (!String.IsNullOrEmpty(cssClass) && !cssClass.StartsWith("$fwstyle="))
					m_xhtml.MapCssClassToXmlNode(cssClass, frag);
			}
			if (!String.IsNullOrEmpty(cssClass))
			{
				if (cssClass.StartsWith("$fwstyle="))
				{
					m_sActiveParaStyle = cssClass.Substring(9);
				}
				else
				{
					string flowType = XmlUtils.GetOptionalAttributeValue(frag, "flowType", null);
					if (flowType == "div" || flowType == "para")
						m_writer.WriteLine("<div class=\"{0}\">", cssClass);
					else
						m_writer.WriteLine("<span class=\"{0}\">", cssClass);
				}
			}
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
					string flowType = XmlUtils.GetOptionalAttributeValue(frag, "flowType", null);
					if (flowType == "div" || flowType == "para")
						m_writer.WriteLine("</div><!--class=\"{0}\"-->", CommentProtect(cssClass));
					else
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
		/// <param name="vss"></param>
		public void WriteCssFile(string sOutputFile, IVwStylesheet vss)
		{
			m_xhtml.WriteCssFile(sOutputFile, vss, XhtmlHelper.CssType.Dictionary, null);
		}

		/// <summary>
		/// Set Cancel flag to asynchronously cancel exporting.
		/// </summary>
		public void Cancel()
		{
			m_fCancel = true;
		}

		#endregion
	}
}
