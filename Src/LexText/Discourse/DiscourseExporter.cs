using System;
using System.IO;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.FwUtils;
using System.Text;
using System.Collections.Generic;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// DiscourseExporter is an IVwEnv implementation which exports discourse data to an XmlWriter.
	/// Make one of these by creating an XmlTextWriter.
	/// Refactoring is probably in order to share more code with InterlinearExporter,
	/// or move common code down to CollectorEnv. This has been postponed in the interests
	/// of being able to release FW 5.2.1 without requiring changes to DLLs other than Discourse.
	/// </summary>
	public class DiscourseExporter : CollectorEnv
	{
		XmlWriter m_writer;
		FdoCache m_cache;
		IVwViewConstructor m_vc = null;
		Set<int> m_usedWritingSystems = new Set<int>();
		int m_wsGloss;
		List<string> m_glossesInCellCollector = new List<string>();
		List<int> m_frags = new List<int>();
		IDsConstChart m_chart;
		enum TitleStage
		{
			ktsStart,
			ktsGotFirstRowGroups,
			ktsGotNotesHeaderCell,
			ktsStartedSecondHeaderRow,
			ktsFinishedHeaders
		}
		// 0 = start, 1 = got first level titles, 2 = opened notes cell, 3 = opened first cell in in 2nd row,
		// 4 = ended that got first real row (and later).
		TitleStage m_titleStage = TitleStage.ktsStart;
		bool m_fNextCellReversed;

		int m_wsLineNumber; // ws to use for line numbers.

		public DiscourseExporter(FdoCache cache, XmlWriter writer, int hvoRoot, IVwViewConstructor vc,
			int wsLineNumber)
			: base(null, cache.MainCacheAccessor, hvoRoot)
		{
			m_cache = cache;
			m_writer = writer;
			m_vc = vc;
			m_wsLineNumber = wsLineNumber;
			m_chart = DsConstChart.CreateFromDBObject(cache, hvoRoot);
		}

		public void ExportDisplay()
		{

			m_writer.WriteStartElement("chart");
			m_writer.WriteStartElement("row"); // first header
			m_writer.WriteAttributeString("type", "title1");
			m_vc.Display(this, this.OpenObject, (int)ConstChartVc.kfragPrintChart);
			m_writer.WriteEndElement();
			WriteLanguages();
		}

		/// <summary>
		/// Write out the languages element. This should be used in InterlinearExporter, too.
		/// </summary>
		void WriteLanguages()
		{
			m_writer.WriteStartElement("languages");
			foreach (int wsActual in m_usedWritingSystems)
			{
				m_writer.WriteStartElement("language");
				// we don't have enough context at this point to get all the possible writing system
				// information we may encounter in the word bundles.
				string icuCode = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(wsActual);
				m_writer.WriteAttributeString("lang", icuCode);
				string fontName = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(wsActual).DefaultSerif;
				m_writer.WriteAttributeString("font", fontName);
				if (m_cache.LangProject.VernWssRC.Contains(wsActual))
				{
					m_writer.WriteAttributeString("vernacular", "true");
				}
				IWritingSystem wsObj = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(wsActual);
				if (wsObj != null && wsObj.RightToLeft)
					m_writer.WriteAttributeString("RightToLeft", "true");
				m_writer.WriteEndElement();
			}
			m_writer.WriteEndElement();	// languages
		}


		public override void AddStringProp(int tag, IVwViewConstructor _vwvc)
		{
			if (tag == (int)StTxtPara.StTxtParaTags.kflidContents)
			{
				WriteStringProp(tag, "note", 0);
			}
		}

		int TopFragment
		{
			get
			{
				if (m_frags.Count == 0)
					return 0;
				return m_frags[m_frags.Count - 1];
			}
		}

		public override void AddStringAltMember(int tag, int ws, IVwViewConstructor _vwvc)
		{
			if (tag == (int)WfiWordform.WfiWordformTags.kflidForm)
			{
				if(m_frags.Contains(ConstChartVc.kfragCcaMoved))
					WriteStringProp(tag, "word", ws, "moved", "true");
				else
					WriteStringProp(tag, "word", ws);
			}
			else if (tag == (int)WfiGloss.WfiGlossTags.kflidForm)
			{
				m_wsGloss = ws;
				string val = m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws).Text;
				if (val == null)
					val = "";
				m_glossesInCellCollector.Add(val);
			}
			else if (tag == (int) CmAnnotation.CmAnnotationTags.kflidComment)
			{
				switch (m_frags[m_frags.Count - 1])
				{
					case ConstChartVc.kfragCca:
						if (m_cache.GetVectorSize(m_hvoCurr,
								(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo) == 0)
						{
							WriteStringProp(tag, "lit", ws); // missing marker.
							break;
						}
						int hvoTarget = m_cache.GetVectorItem(m_hvoCurr,
								(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0); // the cca we refer to
						if (ConstituentChartLogic.HasPreviousMovedItemOnLine(m_chart, hvoTarget))
							WriteStringProp(tag, "moveMkr", ws, "targetFirstOnLine", "false");
						else
							WriteStringProp(tag, "moveMkr", ws);
						break;
					case ConstChartVc.kfragComment:
						WriteStringProp(tag, "clauseMkr", ws, "target", m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws).Text);
						break;
					case ConstChartVc.kfragChartRow:
						WriteStringProp(tag, "rownum", ws);
						break;
				}
			}
			else if (tag == (int)CmPossibility.CmPossibilityTags.kflidAbbreviation)
			{
				// That makes it a list reference
				WriteStringProp(tag, "listRef", ws);
			}
		}

		private void WriteStringProp(int tag, string elementTag, int alt)
		{
			WriteStringProp(tag, elementTag, alt, null, null);
		}

		private void WriteStringProp(int tag, string elementTag, int alt, string extraAttr, string extraAttrVal)
		{
			ITsString tss;
			int ws = alt;
			if (ws == 0)
			{
				tss = m_sda.get_StringProp(m_hvoCurr, tag);
			}
			else
			{
				tss = m_sda.get_MultiStringAlt(m_hvoCurr, tag, alt);
			}
			WriteStringVal(elementTag, ws, tss, extraAttr, extraAttrVal);
		}

		private void WriteStringVal(string elementTag, int ws, ITsString tss, string extraAttr, string extraAttrVal)
		{
			m_writer.WriteStartElement(elementTag);
			if (extraAttr != null)
				m_writer.WriteAttributeString(extraAttr, extraAttrVal);
			WriteLangAndContent(ws, tss);
			m_writer.WriteEndElement();
		}

		private int GetWsFromTsString(ITsString tss)
		{
			ITsTextProps ttp = tss.get_PropertiesAt(0);
			int var;
			return ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
		}

		public override void set_IntProperty(int tpt, int tpv, int nValue)
		{
			if (tpt == (int)FwTextPropType.ktptAlign && nValue == (int)FwTextAlign.ktalTrailing)
				m_fNextCellReversed = true;
			base.set_IntProperty(tpt, tpv, nValue);
		}
		/// <summary>
		/// Write a lang attribute identifying the string, then its content as the body of
		/// an element.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="tss"></param>
		private void WriteLangAndContent(int ws1, ITsString tss)
		{
			int ws = ws1;
			if (ws == 0)
				ws = GetWsFromTsString(tss);
			UpdateWsList(ws);
			string icuCode = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
			m_writer.WriteAttributeString("lang", icuCode);
			m_writer.WriteString(GetText(tss));
		}

		void UpdateWsList(int ws)
		{
			// only add valid actual ws
			if (ws > 0)
			{
				m_usedWritingSystems.Add(ws);
			}
		}

		string GetText(ITsString tss)
		{
			string result = tss.Text;
			if (result == null)
				return "";
			else
				return result.Normalize();
		}

		public override void AddObj(int hvoItem, IVwViewConstructor vc, int frag)
		{
			m_frags.Add(frag);
			base.AddObj (hvoItem, vc, frag);
			m_frags.RemoveAt(m_frags.Count - 1);
		}

		public override void AddString(ITsString tss)
		{
			// Ignore directionality markers on export. Also skip empty strings and single spaces...
			// we handle extra space with the stylesheet.
			string text = tss.Text;
			if (text == "\x200F" || text == "\x200E" || string.IsNullOrEmpty(text) || text == " ")
				return;
			m_writer.WriteStartElement("lit");
			MarkNeedsSpace(tss.Text);

			WriteLangAndContent(GetWsFromTsString(tss), tss);
			m_writer.WriteEndElement();
			base.AddString (tss);
		}

		/// <summary>
		/// Add attributes to an element just started to indicate whether white space is
		/// needed before or after the specified literal.
		/// The current implementation is simplistic, based on the few separators actually
		/// used in the discourse chart.
		/// The default is nothing added, indicating that white space IS needed.
		/// </summary>
		/// <param name="lit"></param>
		private void MarkNeedsSpace(string lit)
		{
			if (lit.StartsWith("]") || lit.StartsWith(")"))
				m_writer.WriteAttributeString("noSpaceBefore", "true");
			if (lit.EndsWith("[") || lit.EndsWith("(") || lit.EndsWith("-"))
				m_writer.WriteAttributeString("noSpaceAfter", "true");
		}

		public override void AddObjProp(int tag, IVwViewConstructor vc, int frag)
		{
			m_frags.Add(frag);
			switch (frag)
			{
				default:
					break;
			}
			base.AddObjProp(tag, vc, frag);
			switch (frag)
			{
				default:
					break;
			}
			m_frags.RemoveAt(m_frags.Count - 1);
		}

		/// <summary>
		/// Here we build the main structure of the chart as a collection of cells. We have to be a bit tricky about
		/// generating the header.
		/// </summary>
		/// <param name="nRowSpan"></param>
		/// <param name="nColSpan"></param>
		public override void OpenTableCell(int nRowSpan, int nColSpan)
		{
			if (m_titleStage == TitleStage.ktsStart && m_frags.Count > 0 && m_frags[m_frags.Count - 1] == ConstChartVc.kfragColumnGroupHeader)
			{
				// got the first group header
				m_titleStage = TitleStage.ktsGotFirstRowGroups;
			}
			else if (m_titleStage == TitleStage.ktsGotFirstRowGroups && m_frags.Count == 0)
			{
				// got the column groups, no longer in that, next thing is the notes header
				m_titleStage = TitleStage.ktsGotNotesHeaderCell;
			}
			else if (m_titleStage == TitleStage.ktsGotNotesHeaderCell)
			{
				// got the one last cell on the very first row, now starting the second row, close first and make a new row.
				m_writer.WriteEndElement();  // terminate the first header row.
				m_writer.WriteStartElement("row"); // second row headers
				m_writer.WriteAttributeString("type", "title2");
				m_titleStage = TitleStage.ktsStartedSecondHeaderRow;
			}
			m_writer.WriteStartElement("cell");
			if (m_fNextCellReversed)
			{
				m_fNextCellReversed = false;
				m_writer.WriteAttributeString("reversed", "true");
			}
			m_writer.WriteAttributeString("cols", nColSpan.ToString());
			m_writer.WriteStartElement("main");
			base.OpenTableCell(nRowSpan, nColSpan);
		}

		public override void CloseTableCell()
		{
			base.CloseTableCell();
			m_writer.WriteEndElement(); // the "main" element
			if (m_glossesInCellCollector.Count > 0)
			{
				m_writer.WriteStartElement("glosses");
				foreach (string gloss in m_glossesInCellCollector)
				{
					m_writer.WriteStartElement("gloss");
					string icuCode = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(m_wsGloss);
					m_writer.WriteAttributeString("lang", icuCode);
					m_writer.WriteString(gloss);
					m_writer.WriteEndElement(); // gloss
				}
				m_writer.WriteEndElement(); // glosses
				// Ready to start collecting for the next cell.
				m_glossesInCellCollector.Clear();
			}

			m_writer.WriteEndElement(); // cell
		}

		/// <summary>
		/// overridden to maintain the frags array.
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="vc"></param>
		/// <param name="frag"></param>
		public override void AddObjVecItems(int tag, IVwViewConstructor vc, int frag)
		{
			m_frags.Add(frag);
			base.AddObjVecItems (tag, vc, frag);
			m_frags.RemoveAt(m_frags.Count - 1);
		}

		/// <summary>
		/// Called whenever we start the display of an object, we currently use it to catch the start of
		/// a row, basedon the frag. Overriding OpenTableRow() might be more natural, but I was trying to
		/// minimize changes to other DLLs, and those routines are not currently virtual in the base class.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="ihvo"></param>
		protected override void OpenTheObject(int hvo, int ihvo)
		{
			int frag = m_frags[m_frags.Count - 1];
			switch (frag)
			{
				case ConstChartVc.kfragChartRow:
					if (m_titleStage == TitleStage.ktsStartedSecondHeaderRow)
					{
						// This is the best way I've found to detect the end of the second header row
						// and terminate it.
						m_titleStage = TitleStage.ktsFinishedHeaders;
						m_writer.WriteEndElement();
					}
					m_writer.WriteStartElement("row");
					if (ConstituentChartLogic.GetFeature(m_cache.MainCacheAccessor, hvo, ConstituentChartLogic.EndParaFeatureName))
						m_writer.WriteAttributeString("endPara", "true");
					else if (ConstituentChartLogic.GetFeature(m_cache.MainCacheAccessor, hvo, ConstituentChartLogic.EndSentFeatureName))
						m_writer.WriteAttributeString("endSent", "true");
					ConstChartVc vc = m_vc as ConstChartVc;
					string clauseType = vc.GetRowStyleName(this, hvo);
					m_writer.WriteAttributeString("type", clauseType);
					string label = m_cache.GetMultiStringAlt(hvo, (int)CmAnnotation.CmAnnotationTags.kflidComment,
						m_wsLineNumber).Text;
					if (!String.IsNullOrEmpty(label))
						m_writer.WriteAttributeString("id", label);
					break;
				default:
					break;
			}
			base.OpenTheObject(hvo, ihvo);
		}

		protected override void CloseTheObject()
		{
			base.CloseTheObject();
			int frag = m_frags[m_frags.Count - 1];
			switch (frag)
			{
				case ConstChartVc.kfragChartRow:
					m_writer.WriteEndElement(); // row
					break;
				default:
					break;
			}
		}

	}
}
