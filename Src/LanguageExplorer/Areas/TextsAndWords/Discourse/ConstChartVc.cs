// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.Xml;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	internal class ConstChartVc : InterlinVc
	{
		public const int kfragChart = 3000000; // should be distinct from ones used in InterlinVc
		internal const int kfragChartRow = 3000001;
		internal const int kfragCellPart = 3000002;
		internal const int kfragMovedTextCellPart = 3000003;
		internal const int kfragChartListItem = 3000004;
		const int kfragPossibility = 3000005;
		internal const int kfragNotesString = 3000007;
		internal const int kfragPrintChart = 3000009;
		const int kfragTemplateHeader = 3000010;
		internal const int kfragColumnGroupHeader = 3000011;
		const int kfragClauseLabels = 3000012;
		internal const int kfragComment = 3000013;
		internal const int kfragMTMarker = 3000014;
		VwLength[] m_colWidths;
		internal ConstChartBody m_chart;
		Dictionary<string, ITsTextProps> m_formatProps;
		Dictionary<string, string> m_brackets;
		private readonly IConstChartRowRepository m_rowRepo;
		private readonly IConstChartWordGroupRepository m_wordGrpRepo;
		private readonly IConstituentChartCellPartRepository m_partRepo;
		private const int kflidDepClauses = ConstChartClauseMarkerTags.kflidDependentClauses;
		internal ITsString m_sMovedTextBefore;
		internal ITsString m_sMovedTextAfter;
		private bool m_fIsAnalysisWsGraphiteEnabled;

		public ConstChartVc(ConstChartBody chart)
			: base(chart.Cache)
		{
			m_chart = chart;
			m_cache = m_chart.Cache;
			SpaceString = TsStringUtils.MakeString(" ", m_cache.DefaultAnalWs);
			m_rowRepo = m_cache.ServiceLocator.GetInstance<IConstChartRowRepository>();
			m_wordGrpRepo = m_cache.ServiceLocator.GetInstance<IConstChartWordGroupRepository>();
			m_partRepo = m_cache.ServiceLocator.GetInstance<IConstituentChartCellPartRepository>();
			m_sMovedTextBefore = TsStringUtils.MakeString(LanguageExplorerResources.ksMovedTextBefore, m_cache.DefaultUserWs);
			m_sMovedTextAfter = TsStringUtils.MakeString(LanguageExplorerResources.ksMovedTextAfter, m_cache.DefaultUserWs);
			LoadFormatProps();
		}

		internal ITsString SpaceString { get; }

		private void LoadFormatProps()
		{
			m_formatProps = new Dictionary<string, ITsTextProps>();
			m_brackets = new Dictionary<string, string>();
			foreach (var item in XDocument.Parse(TextAndWordsResources.ConstituentChartStyleInfo).Root.Elements())
			{
				var bldr = TsStringUtils.MakePropsBldr();
				var color = XmlUtils.GetOptionalAttributeValue(item, "color", null);
				if (color != null)
				{
					bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, ColorVal(color.Trim()));
				}
				var underlinecolor = XmlUtils.GetOptionalAttributeValue(item, "underlinecolor", null);
				if (underlinecolor != null)
				{
					bldr.SetIntPropValues((int)FwTextPropType.ktptUnderColor, (int)FwTextPropVar.ktpvDefault, ColorVal(underlinecolor.Trim()));
				}
				var underline = XmlUtils.GetOptionalAttributeValue(item, "underline", null);
				if (underline != null)
				{
					bldr.SetIntPropValues((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum, InterpretUnderlineType(underline.Trim()));
				}
				var fontsize = XmlUtils.GetOptionalAttributeValue(item, "fontsize", null);
				if (fontsize != null)
				{
					var sval = fontsize.Trim();
					if (sval[sval.Length - 1] == '%')
					{
						sval = sval.Substring(0, sval.Length - 1); // strip %
						var percent = Convert.ToInt32(sval);
						bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvRelative, percent * 100);
					}
					else
					{
						bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, Convert.ToInt32(sval));
					}
				}
				var bold = XmlUtils.GetOptionalAttributeValue(item, "bold", null);
				if (bold == "true")
				{
					bldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvInvert);
				}
				var italic = XmlUtils.GetOptionalAttributeValue(item, "italic", null);
				if (italic == "true")
				{
					bldr.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvInvert);
				}
				var brackets = XmlUtils.GetOptionalAttributeValue(item, "brackets", null);
				if (brackets != null && brackets.Trim().Length == 2)
				{
					m_brackets[item.Name.LocalName] = brackets.Trim();
				}
				m_formatProps[item.Name.LocalName] = bldr.GetTextProps();
			}
			m_fIsAnalysisWsGraphiteEnabled = m_cache.LanguageProject.DefaultAnalysisWritingSystem.IsGraphiteEnabled;
			if (m_chart.IsRightToLeft)
			{
				SwapMovedTextMarkers();
			}
		}

		private void SwapMovedTextMarkers()
		{
			var temp = m_sMovedTextAfter;
			m_sMovedTextAfter = m_sMovedTextBefore;
			m_sMovedTextBefore = temp;
		}

		/// <summary>
		/// Interpret at color value, which can be one of the KnownColor names or (R, G, B).
		/// The result is what the Views code expects for colors.
		/// </summary>
		private static int ColorVal(string val)
		{
			if (val[0] == '(')
			{
				var firstComma = val.IndexOf(',');
				var red = Convert.ToInt32(val.Substring(1, firstComma - 1));
				var secondComma = val.IndexOf(',', firstComma + 1);
				var green = Convert.ToInt32(val.Substring(firstComma + 1, secondComma - firstComma - 1));
				var blue = Convert.ToInt32(val.Substring(secondComma + 1, val.Length - secondComma - 2));
				return red + (blue * 256 + green) * 256;
			}
			var col = Color.FromName(val);
			return col.R + (col.B * 256 + col.G) * 256;
		}

		/// <summary>
		/// Interpret an underline type string as an FwUnderlineType.
		/// Copied from XmlViews (to avoid yet another reference).
		/// </summary>
		private static int InterpretUnderlineType(string strVal)
		{
			var val = (int)FwUnderlineType.kuntSingle; // default
			switch (strVal)
			{
				case "single":
				case null:
					val = (int)FwUnderlineType.kuntSingle;
					break;
				case "none":
					val = (int)FwUnderlineType.kuntNone;
					break;
				case "double":
					val = (int)FwUnderlineType.kuntDouble;
					break;
				case "dotted":
					val = (int)FwUnderlineType.kuntDotted;
					break;
				case "dashed":
					val = (int)FwUnderlineType.kuntDashed;
					break;
				case "squiggle":
					val = (int)FwUnderlineType.kuntSquiggle;
					break;
				case "strikethrough":
					val = (int)FwUnderlineType.kuntStrikethrough;
					break;
				default:
					Debug.Assert(false, "Expected value single, none, double, dotted, dashed, strikethrough, or squiggle");
					break;
			}
			return val;
		}

		internal void ApplyFormatting(IVwEnv vwenv, string key)
		{
			ITsTextProps ttp;
			if (m_formatProps.TryGetValue(key, out ttp))
			{
				vwenv.Props = ttp;
			}
		}

		/// <summary>
		/// (Default) width of the number column (in millipoints).
		/// </summary>
		internal int NumColWidth => m_chart.NumColWidth;

		/// <summary>
		/// Set the column widths (in millipoints).
		/// </summary>
		public void SetColWidths(VwLength[] widths)
		{
			m_colWidths = widths;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case kfragPrintChart: // the whole chart with headings for printing.
					if (hvo == 0)
					{
						return;
					}
					PrintColumnGroupHeaders(hvo, vwenv);
					PrintIndividualColumnHeaders(hvo, vwenv);
					// Rest is same as kfragChart
					DisplayChartBody(vwenv);
					break;
				case kfragTemplateHeader: // Display the template as group headers.
					vwenv.AddObjVecItems(CmPossibilityTags.kflidSubPossibilities, this, kfragColumnGroupHeader);
					break;
				// This is only used for printing, the headers in the screen version are a separate control.
				case kfragColumnGroupHeader:
					var ccols = vwenv.DataAccess.get_VecSize(hvo, CmPossibilityTags.kflidSubPossibilities);
					// If there are no subitems, we still want a blank cell as a placeholder.
					MakeCellsMethod.OpenStandardCell(vwenv, Math.Max(ccols, 1), true);
					if (ccols > 0)
					{
						// It's a group, include its name
						var possGroup = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(hvo);
						vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalCenter);
						vwenv.OpenParagraph();
						vwenv.AddString(possGroup.Name.BestAnalysisAlternative);
						vwenv.CloseParagraph();
					}
					vwenv.CloseTableCell();
					break;
				case kfragChart: // the whole chart, a DsConstChart.
					if (hvo == 0)
					{
						return;
					}
					DisplayChartBody(vwenv);
					break;
				case kfragChartRow: // one row, a ConstChartRow
					{
						MakeTableAndRowWithStdWidths(vwenv, hvo, false);
						MakeCells(vwenv, hvo);
						vwenv.CloseTableRow();
						vwenv.CloseTable();
					}
					break;
				case kfragCellPart: // a single group of words, the contents of one cell.
					if (m_chart.Logic.IsWordGroup(hvo))
					{
						DisplayWordforms(vwenv, hvo);
					}
					else
					{
						// it's a moved text or clause reference placeholder.
						int hvoClause;
						if (m_chart.Logic.IsClausePlaceholder(hvo, out hvoClause))
						{
							DisplayClausePlaceholder(vwenv, hvoClause);
						}
						else
						{
							DisplayMovedTextTag(hvo, vwenv);
						}
					}
					break;
				case kfragMovedTextCellPart:
					// a single group of words (ConstChartWordGroup),
					// the contents of one cell, which is considered moved-within-line.
					// Can't be a placeholder.
					var formatTag = m_chart.Logic.MovedTextTag(hvo);
					ApplyFormatting(vwenv, formatTag);
					vwenv.OpenSpan();
					InsertOpenBracket(vwenv, formatTag);
					DisplayWordforms(vwenv, hvo);
					InsertCloseBracket(vwenv, formatTag);
					vwenv.CloseSpan();
					break;
				case kfragChartListItem:
					// a single ConstChartTag, referring to a list item.
					// can't be a placeholder.
					ApplyFormatting(vwenv, "marker");
					vwenv.OpenSpan();
					InsertOpenBracket(vwenv, "marker");
					vwenv.AddObjProp(ConstChartTagTags.kflidTag, this, kfragPossibility);
					InsertCloseBracket(vwenv, "marker");
					vwenv.CloseSpan();
					break;
				case kfragPossibility:
					// A CmPossibility, show it's abbreviation
					var flid = CmPossibilityTags.kflidAbbreviation;
					var retWs = WritingSystemServices.ActualWs(m_cache, WritingSystemServices.kwsFirstAnal, hvo, flid);
					if (retWs == 0)
					{
						// No Abbreviation! Switch to Name
						flid = CmPossibilityTags.kflidName;
						retWs = WritingSystemServices.ActualWs(m_cache, WritingSystemServices.kwsFirstAnal, hvo, flid);
					}
					// Unless we didn't get anything, go ahead and insert the best option we found.
					if (retWs != 0)
					{
						vwenv.AddStringAltMember(flid, retWs, this);
					}
					break;
				case kfragBundle:
					// One annotated word bundle; hvo is IAnalysis object. Overrides behavior of InterlinVc
					AddWordBundleInternal(hvo, vwenv);
					break;
				case kfragNotesString: // notes text
					vwenv.AddStringProp(ConstChartRowTags.kflidNotes, this);
					break;
				case kfragComment: // hvo is a ConstChartRow
					vwenv.AddStringProp(ConstChartRowTags.kflidLabel, this);
					break;
				case kfragMTMarker:
					var mtt = m_partRepo.GetObject(vwenv.OpenObject) as IConstChartMovedTextMarker;
					Debug.Assert(mtt != null, "Invalid MovedTextMarker?");
					vwenv.AddString(mtt.Preposed ? m_sMovedTextBefore : m_sMovedTextAfter);
					// Need to regenerate this if the row my WordGroup is in changes.
					vwenv.NoteDependency(new[] { mtt.WordGroupRA.Owner.Hvo }, new int[] { ConstChartRowTags.kflidCells }, 1);
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		private void DisplayWordforms(IVwEnv vwenv, int hvoWordGrp)
		{
			// If the WordGroup reference parameters change, we need to regenerate.
			var wordGrpFlidArray = new[]
			{
				ConstChartWordGroupTags.kflidBeginSegment,
				ConstChartWordGroupTags.kflidEndSegment,
				ConstChartWordGroupTags.kflidBeginAnalysisIndex,
				ConstChartWordGroupTags.kflidEndAnalysisIndex
			};
			NoteWordGroupDependencies(vwenv, hvoWordGrp, wordGrpFlidArray);
			var wordGrp = m_wordGrpRepo.GetObject(hvoWordGrp);
			foreach (var point in wordGrp.GetOccurrences())
			{
				SetupAndOpenInnerPile(vwenv);
				DisplayAnalysisAndCloseInnerPile(vwenv, point, false);
			}
		}

		private static void NoteWordGroupDependencies(IVwEnv vwenv, int hvoWordGrp, int[] wordGrpFlidArray)
		{
			var cArray = wordGrpFlidArray.Length;
			var hvoArray = new int[cArray];
			for (var i = 0; i < cArray; i++)
			{
				hvoArray[i] = hvoWordGrp;
			}
			vwenv.NoteDependency(hvoArray, wordGrpFlidArray, cArray);
		}

		/// <summary>
		/// Chart version
		/// </summary>
		protected override void AddWordBundleInternal(int hvo, IVwEnv vwenv)
		{
			SetupAndOpenInnerPile(vwenv);
			// we assume we're in the context of a segment with analyses here.
			// we'll need this info down in DisplayAnalysisAndCloseInnerPile()
			int hvoSeg;
			int tagDummy;
			int index;
			vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoSeg, out tagDummy, out index);
			var analysisOccurrence = new AnalysisOccurrence(m_segRepository.GetObject(hvoSeg), index);
			DisplayAnalysisAndCloseInnerPile(vwenv, analysisOccurrence, false);
		}

		/// <summary>
		/// Setup a box with 5 under and trailing, plus leading alignment, and open the inner pile
		/// </summary>
		/// <param name="vwenv"></param>
		protected override void SetupAndOpenInnerPile(IVwEnv vwenv)
		{
			// Make an 'inner pile' to contain the wordform and its interlinear.
			// Give whatever box we make 5 points of separation from whatever follows.
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing, (int)FwTextPropVar.ktpvMilliPoint, 5000);
			// 5 points below also helps space out the paragraph.
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom, (int)FwTextPropVar.ktpvMilliPoint, 5000);
			vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalLeading);
			vwenv.OpenInnerPile();
		}

		private void PrintIndividualColumnHeaders(int hvo, IVwEnv vwenv)
		{
			var analWs = m_cache.DefaultAnalWs;
			var oldEnv = vwenv;
			MakeTableAndRowWithStdWidths(vwenv, hvo, true);
			vwenv = new ChartRowEnvDecorator(vwenv); // in case this is a RTL chart
			((ChartRowEnvDecorator)vwenv).IsRtL = m_chart.IsRightToLeft;
			MakeCellsMethod.OpenRowNumberCell(vwenv); // blank cell under header for row numbers
			vwenv.CloseTableCell();
			PrintTemplateColumnHeaders(vwenv, analWs);
			MakeCellsMethod.OpenStandardCell(vwenv, 1, false); // blank cell below Notes header
			vwenv.CloseTableCell();
			((ChartRowEnvDecorator)vwenv).FlushDecorator(); // if RTL, put out headers reversed
			vwenv = oldEnv; // remove Decorator
			vwenv.CloseTableRow();
			vwenv.CloseTable();
		}

		private void PrintTemplateColumnHeaders(IVwEnv vwenv, int analWs)
		{
			for (var icol = 0; icol < m_chart.AllColumns.Length; icol++)
			{
				PrintOneTemplateHeader(vwenv, analWs, icol);
			}
		}

		private void PrintOneTemplateHeader(IVwEnv vwenv, int analWs, int icol)
		{
			MakeCellsMethod.OpenStandardCell(vwenv, 1, m_chart.Logic.GroupEndIndices.Contains(icol));
			vwenv.AddString(TsStringUtils.MakeString(m_chart.Logic.GetColumnLabel(icol), analWs));
			vwenv.CloseTableCell();
		}

		private void PrintColumnGroupHeaders(int hvo, IVwEnv vwenv)
		{
			var analWs = m_cache.DefaultAnalWs;
			var oldEnv = vwenv; // store this for later
			MakeTableAndRowWithStdWidths(vwenv, hvo, true);
			vwenv = new ChartRowEnvDecorator(vwenv); // in case this is a RTL chart
			((ChartRowEnvDecorator)vwenv).IsRtL = m_chart.IsRightToLeft;
			PrintRowNumCellHeader(vwenv, analWs);
			vwenv.AddObjProp(DsChartTags.kflidTemplate, this, kfragTemplateHeader);
			PrintNotesCellHeader(vwenv, analWs);
			((ChartRowEnvDecorator)vwenv).FlushDecorator(); // if it is a RTL chart, put it out reversed.
			vwenv = oldEnv; // remove Decorator
			vwenv.CloseTableRow();
			vwenv.CloseTable();
		}

		private static void PrintNotesCellHeader(IVwEnv vwenv, int analWs)
		{
			MakeCellsMethod.OpenStandardCell(vwenv, 1, false);
			vwenv.AddString(TsStringUtils.MakeString(LanguageExplorerResources.ksNotesColumnHeader, analWs));
			vwenv.CloseTableCell();
		}

		private static void PrintRowNumCellHeader(IVwEnv vwenv, int analWs)
		{
			MakeCellsMethod.OpenRowNumberCell(vwenv); // header for row numbers
			vwenv.AddString(TsStringUtils.MakeString("#", analWs));
			vwenv.CloseTableCell();
		}

		private void DisplayMovedTextTag(int hvo, IVwEnv vwenv)
		{
			// hvo is a ConstChartMovedTextMarker
			var mtt = m_partRepo.GetObject(hvo) as IConstChartMovedTextMarker;
			Debug.Assert(mtt != null, "Hvo is not for a MovedText Marker.");
			var formatTag1 = m_chart.Logic.MovedTextTag(mtt.WordGroupRA.Hvo) + "Mkr";
			ApplyFormatting(vwenv, formatTag1);
			vwenv.OpenSpan();
			InsertOpenBracket(vwenv, formatTag1);
			vwenv.AddObj(hvo, this, kfragMTMarker);
			InsertCloseBracket(vwenv, formatTag1);
			vwenv.CloseSpan();
		}

		private void DisplayClausePlaceholder(IVwEnv vwenv, int hvoClause)
		{
			var clauseType = GetRowStyleName(hvoClause) + "Mkr";
			ApplyFormatting(vwenv, clauseType);
			vwenv.OpenSpan();
			InsertOpenBracket(vwenv, clauseType);
			vwenv.AddObjVec(kflidDepClauses, this, kfragClauseLabels);
			InsertCloseBracket(vwenv, clauseType);
			vwenv.CloseSpan();
		}

		/// <summary>
		/// Make a 'standard' row. Used for both header and body.
		/// </summary>
		private void MakeTableAndRowWithStdWidths(IVwEnv vwenv, int hvo, bool fHeader)
		{
			IConstChartRow row = null;
			if (!fHeader)
			{
				row = m_rowRepo.GetObject(hvo);
			}
			var tableWidth = new VwLength();
			if (m_colWidths == null)
			{
				tableWidth.nVal = 10000; // 100%
				tableWidth.unit = VwUnit.kunPercent100;
			}
			else
			{
				tableWidth.nVal = 0;
				foreach (var w in m_colWidths)
				{
					tableWidth.nVal += w.nVal;
				}
				tableWidth.unit = VwUnit.kunPoint1000;
			}
			if (!fHeader)
			{
				SetRowStyle(vwenv, row);
			}
			var fpos = VwFramePosition.kvfpVsides;
			if (fHeader)
			{
				fpos = (VwFramePosition)((int)fpos | (int)VwFramePosition.kvfpAbove);
			}
			else
			{
				int hvoOuter, tagOuter, ihvoRow;
				vwenv.GetOuterObject(0, out hvoOuter, out tagOuter, out ihvoRow);
				if (ihvoRow == 0)
				{
					fpos = (VwFramePosition)((int)fpos | (int)VwFramePosition.kvfpAbove);
				}
				if (ihvoRow == vwenv.DataAccess.get_VecSize(hvoOuter, tagOuter) - 1 || row.EndParagraph)
				{
					fpos = (VwFramePosition)((int)fpos | (int)VwFramePosition.kvfpBelow);
				}
			}
			// We seem to typically inherit a white background as a side effect of setting our stylesheet,
			// but borders on table rows don't show through if backcolor is set to white, because the
			// cells entirely cover the row (LT-9068). So force the back color to be transparent, and allow
			// the row border to show through the cell.
			var fRtL = m_chart.IsRightToLeft;
			vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, (int)FwTextColor.kclrTransparent);
			vwenv.OpenTable(m_chart.AllColumns.Length + ConstituentChartLogic.NumberOfExtraColumns, tableWidth, 1500, fRtL ? VwAlignment.kvaRight : VwAlignment.kvaLeft, fpos, VwRule.kvrlNone, 0, 2000, true);
			if (m_colWidths == null)
			{
				if (fRtL)
				{
					MakeColumnsOtherThanRowNum(vwenv);
					MakeRowNumColumn(vwenv);
				}
				else
				{
					MakeRowNumColumn(vwenv);
					MakeColumnsOtherThanRowNum(vwenv);
				}
			}
			else
			{
				foreach (var colWidth in m_colWidths)
				{
					vwenv.MakeColumns(1, colWidth);
				}
			}
			// Set row bottom border color and size of table body rows
			if (!fHeader)
			{
				if (row.EndSentence)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Black));
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 1000);
				}
				else
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.LightGray));
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom, (int)FwTextPropVar.ktpvMilliPoint, 500);
				}
			}

			vwenv.OpenTableRow();
		}

		private void MakeColumnsOtherThanRowNum(IVwEnv vwenv)
		{
			var colWidth = new VwLength
			{
				nVal = 1,
				unit = VwUnit.kunRelative
			};
			const int followingCols = ConstituentChartLogic.NumberOfExtraColumns - ConstituentChartLogic.indexOfFirstTemplateColumn;
			vwenv.MakeColumns(m_chart.AllColumns.Length + followingCols, colWidth);
		}

		private void MakeRowNumColumn(IVwEnv vwenv)
		{
			var numColWidth = new VwLength
			{
				nVal = NumColWidth,
				unit = VwUnit.kunPoint1000
			};
			vwenv.MakeColumns(1, numColWidth);
		}

		private void DisplayChartBody(IVwEnv vwenv)
		{
			vwenv.AddLazyVecItems(DsConstChartTags.kflidRows, this, kfragChartRow);
		}

		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			switch (frag)
			{
				case kfragClauseLabels:
					// hvo is ConstChartClauseMarker pointing at a group of rows (at least one).
					// Enhance JohnT: this assumes it is always a contiguous list.
					var sda = vwenv.DataAccess;
					var chvo = sda.get_VecSize(hvo, kflidDepClauses);
					var hvoFirst = sda.get_VecItem(hvo, kflidDepClauses, 0);
					vwenv.AddObj(hvoFirst, this, kfragComment);
					if (chvo == 1)
					{
						break;
					}
					var shyphen = TsStringUtils.MakeString("-", m_cache.DefaultAnalWs);
					vwenv.AddString(shyphen);
					var hvoLast = sda.get_VecItem(hvo, kflidDepClauses, chvo - 1);
					vwenv.AddObj(hvoLast, this, kfragComment);
					break;
				default:
					base.DisplayVec(vwenv, hvo, tag, frag);
					break;
			}
		}

		/// <summary>
		/// Makes the cells for a row using the MakeCellsMethod method object.
		/// Made internal for testing.
		/// </summary>
		internal void MakeCells(IVwEnv vwenv, int hvoRow)
		{
			new MakeCellsMethod(this, m_cache, vwenv, hvoRow).Run(m_chart.IsRightToLeft);
		}

		// In this chart this only gets invoked for the baseline. It is currently always black.
		protected override int LabelRGBFor(int choiceIndex)
		{
			return (int)ColorUtil.ConvertColorToBGR(Color.Black);
		}

		// For the gloss line, make it whatever is called for.
		protected override void FormatGloss(IVwEnv vwenv, int ws)
		{
			// Gloss should not inherit any underline setting from baseline
			vwenv.set_IntProperty((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum, (int)FwUnderlineType.kuntNone);
			ApplyFormatting(vwenv, "gloss");
		}

		// This used to be kAnnotationColor. I'm a little confused as to its actual meaning here.
		readonly int kWordformColor = (int)ColorUtil.ConvertColorToBGR(Color.DarkGray);

		/// <summary>
		/// A nasty kludge, but everything gray should also be underlined.
		/// </summary>
		protected override void SetColor(IVwEnv vwenv, int color)
		{
			base.SetColor(vwenv, color);
			if (color == kWordformColor)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum, (int)FwUnderlineType.kuntNone);
			}
		}

		internal string GetRowStyleName(int hvoRow)
		{
			var row = m_rowRepo.GetObject(hvoRow);
			return GetRowStyleName(row);
		}

		internal static string GetRowStyleName(IConstChartRow row)
		{
			switch (row.ClauseType)
			{
				case ClauseTypes.Dependent:
					return "dependent";
				case ClauseTypes.Speech:
					return "speech";
				case ClauseTypes.Song:
					return "song";
				default:
					return "normal";
			}
		}

		private void SetRowStyle(IVwEnv vwenv, IConstChartRow row)
		{
			ApplyFormatting(vwenv, GetRowStyleName(row));
		}


		protected override void GetSegmentLevelTags(LcmCache cache)
		{
			// do nothing (we don't need tags above bundle level).
		}

		internal void InsertOpenBracket(IVwEnv vwenv, string key)
		{
			string bracket;
			if (!m_brackets.TryGetValue(key, out bracket))
			{
				return;
			}
			InsertOpenBracketInternal(vwenv, bracket, false);
		}

		internal void AddRtLOpenBracketWithRLMs(IVwEnv vwenv, string key)
		{
			string bracket;
			if (!m_brackets.TryGetValue(key, out bracket))
			{
				return;
			}
			InsertOpenBracketInternal(vwenv, bracket, true);
		}

		private void InsertOpenBracketInternal(IVwEnv vwenv, string bracket, bool fRtL)
		{
			var index = 0;
			var sFormat = "{0}";
			if (fRtL)
			{
				sFormat = m_chart.RLM + sFormat + m_chart.RLM;
				if (m_fIsAnalysisWsGraphiteEnabled)
				{
					index = 1;
				}
			}
			vwenv.AddString(TsStringUtils.MakeString(string.Format(sFormat, bracket.Substring(index, 1)), m_cache.DefaultAnalWs));
		}

		internal void InsertCloseBracket(IVwEnv vwenv, string key)
		{
			string bracket;
			if (!m_brackets.TryGetValue(key, out bracket))
			{
				return;
			}
			InsertCloseBracketInternal(vwenv, bracket, false);
		}

		internal void AddRtLCloseBracketWithRLMs(IVwEnv vwenv, string key)
		{
			string bracket;
			if (!m_brackets.TryGetValue(key, out bracket))
			{
				return;
			}
			InsertCloseBracketInternal(vwenv, bracket, true);
		}

		private void InsertCloseBracketInternal(IVwEnv vwenv, string bracket, bool fRtL)
		{
			var index = 1;
			var sFormat = "{0}";
			if (fRtL)
			{
				sFormat = m_chart.RLM + sFormat + m_chart.RLM;
				if (m_fIsAnalysisWsGraphiteEnabled)
				{
					index = 0;
				}
			}
			vwenv.AddString(TsStringUtils.MakeString(string.Format(sFormat, bracket.Substring(index, 1)), m_cache.DefaultAnalWs));
		}

		/// <summary>
		/// Implementation of method for making cells in chart row.
		/// </summary>
		private sealed class MakeCellsMethod
		{
			private readonly ChartRowEnvDecorator m_vwenv;
			private readonly int m_hvoRow; // Hvo of the IConstChartRow representing a row in the chart.
			private readonly IConstChartRow m_row;
			private readonly LcmCache m_cache;
			private readonly ConstChartVc m_this; // original 'this' object of the refactored method.
			private readonly ConstChartBody m_chartBody;
			private int[] m_cellparts;
			/// <summary>
			/// Column for which cell is currently open (initially not for any column)
			/// </summary>
			private int m_hvoCurCellCol;
			/// <summary>
			/// Index (display) of last column for which we have made (at least opened) a cell.
			/// </summary>
			private int m_iLastColForWhichCellExists = -1;
			/// <summary>
			/// Index of cellpart to insert clause bracket before; gets reset if we find an auto-missing-marker col first.
			/// </summary>
			private int m_icellPartOpenClause = -1;
			/// <summary>
			/// Index of cellpart to insert clause bracket after (unless m_icolLastAutoMissing is a later column).
			/// </summary>
			private int m_icellPartCloseClause = -1;
			/// <summary>
			/// Number of cellparts output in current cell.
			/// </summary>
			private int m_cCellPartsInCurrentCell;
			private int m_icellpart = 0;
			/// <summary>
			/// Index of last column where automatic missing markers are put.
			/// </summary>
			private int m_icolLastAutoMissing = -1;
			/// <summary>
			/// Stores the TsString displayed for missing markers (auto or user)
			/// </summary>
			private ITsString m_missMkr;

			#region Repository member variables

			private IConstChartRowRepository m_rowRepo;
			private IConstituentChartCellPartRepository m_partRepo;

			#endregion

			/// <summary />
			internal MakeCellsMethod(ConstChartVc baseObj, LcmCache cache, IVwEnv vwenv, int hvo)
			{
				m_this = baseObj;
				m_cache = cache;
				m_rowRepo = m_cache.ServiceLocator.GetInstance<IConstChartRowRepository>();
				m_partRepo = m_cache.ServiceLocator.GetInstance<IConstituentChartCellPartRepository>();

				// Decorator makes sure that things get put out in the right order if chart is RtL
				m_chartBody = baseObj.m_chart;
				m_vwenv = new ChartRowEnvDecorator(vwenv);

				m_hvoRow = hvo;
				m_row = m_rowRepo.GetObject(m_hvoRow);
			}

			private void SetupMissingMarker()
			{
				m_missMkr = TsStringUtils.MakeString(LanguageExplorerResources.ksMissingMarker, m_cache.DefaultAnalWs);
			}

			/// <summary>
			/// Main entry point, makes the cells.
			/// </summary>
			internal void Run(bool fRtL)
			{
				SetupMissingMarker();
				// If the CellsOS of the row changes, we need to regenerate.
				var rowFlidArray = new[]
				{
					ConstChartRowTags.kflidCells,
					ConstChartRowTags.kflidClauseType,
					ConstChartRowTags.kflidEndParagraph,
					ConstChartRowTags.kflidEndSentence
				};
				NoteRowDependencies(rowFlidArray);

				m_vwenv.IsRtL = fRtL;
				if (!(m_chartBody.Chart.NotesColumnOnRight ^ fRtL))
				{
					MakeNoteCell();
				}
				MakeRowLabelCell();
				MakeMainCellParts(); // Make all the cell parts between row label and note.
				if (m_chartBody.Chart.NotesColumnOnRight ^ fRtL)
				{
					MakeNoteCell();
				}
				FlushDecorator();
			}

			private void FlushDecorator()
			{
				m_vwenv.FlushDecorator();
			}

			private void MakeNoteCell()
			{
				OpenNoteCell();
				m_vwenv.AddStringProp(ConstChartRowTags.kflidNotes, m_this);
				m_vwenv.CloseTableCell();
			}

			private void MakeMainCellParts()
			{
				m_cellparts = m_row.CellsOS.ToHvoArray();
				if (m_row.StartDependentClauseGroup)
				{
					FindCellPartToStartDependentClause();
				}
				if (m_row.EndDependentClauseGroup)
				{
					FindCellPartToEndDependentClause();
				}
				// Main loop over CellParts in this row
				for (m_icellpart = 0; m_icellpart < m_cellparts.Length; m_icellpart++)
				{
					var hvoCellPart = m_cellparts[m_icellpart];
					// If the column or merge properties of the cell changes, we need to regenerate.
					var cellPartFlidArray = new[]
					{
					ConstituentChartCellPartTags.kflidColumn,
					ConstituentChartCellPartTags.kflidMergesBefore,
					ConstituentChartCellPartTags.kflidMergesAfter
				};
					NoteCellDependencies(cellPartFlidArray, hvoCellPart);
					ProcessCurrentCellPart(hvoCellPart);
				}
				CloseCurrentlyOpenCell();
				// Make any leftover empty cells.
				MakeEmptyCells(m_chartBody.AllColumns.Length - m_iLastColForWhichCellExists - 1);
			}

			private void ProcessCurrentCellPart(int hvoCellPart)
			{
				var cellPart = m_partRepo.GetObject(hvoCellPart);
				var hvoColContainingCellPart = cellPart.ColumnRA.Hvo;
				if (hvoColContainingCellPart == 0)
				{
					// It doesn't belong to any column! Maybe the template got edited and the column
					// was deleted? Arbitrarily assign it to the first column...logic below
					// may change to the current column if any.
					hvoColContainingCellPart = m_chartBody.AllColumns[0].Hvo;
					ReportAndFixBadCellPart(hvoCellPart, m_chartBody.AllColumns[0]);
				}
				if (hvoColContainingCellPart == m_hvoCurCellCol)
				{
					// same column; just add to the already-open cell
					AddCellPartToCell(cellPart);
					return;
				}
				//var ihvoNewCol = m_chart.DisplayFromLogical(GetIndexOfColumn(hvoColContainingCellPart));
				var ihvoNewCol = GetIndexOfColumn(hvoColContainingCellPart);
				if (ihvoNewCol < m_iLastColForWhichCellExists || ihvoNewCol >= m_chartBody.AllColumns.Length)
				{
					//Debug.Fail(string.Format("Cell part : {0} Chart AllColumns length is: {1} ihvoNewCol is: {2}", cellPart.Guid, m_chart.AllColumns.Length, ihvoNewCol));
					// pathological case...cell part is out of order or its column has been deleted.
					// Maybe the user re-ordered the columns??
					// Anyway, we'll let it go into the current cell.
					var column = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(m_hvoCurCellCol);
					ReportAndFixBadCellPart(hvoCellPart, column);
					AddCellPartToCell(cellPart);
					return;
				}
				// changed column (or started first column). Close the current cell if one is open, and figure out
				// how many cells wide the new one needs to be.
				CloseCurrentlyOpenCell();
				var ccolsAvailableUpToCurrent = ihvoNewCol - m_iLastColForWhichCellExists;
				m_hvoCurCellCol = hvoColContainingCellPart;
				if (cellPart.MergesBefore)
				{
					// Make one cell covering all the columns not already occupied, up to and including the current one.
					// If in fact merging is occurring, align it in the appropriate cell.
					if (ccolsAvailableUpToCurrent > 1)
					{
						m_vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalTrailing);
					}
					MakeDataCell(ccolsAvailableUpToCurrent);
					m_iLastColForWhichCellExists = ihvoNewCol;
				}
				else
				{
					// Not merging left, first fill in any extra, empty cells.
					MakeEmptyCells(ccolsAvailableUpToCurrent - 1);
					// We have created all cells before ihvoNewCol; need to decide how many to merge right.
					var ccolsNext = 1;
					if (cellPart.MergesAfter)
					{
						// Determine how MANY cells it can use. Find the next CellPart in a different column, if any.
						// It's column determines how many cells are empty. If it merges before, consider
						// giving it a column to merge.
						var iNextColumn = m_chartBody.AllColumns.Length; // by default can use all remaining columns.
						for (var icellPartNextCol = m_icellpart + 1; icellPartNextCol < m_cellparts.Length; icellPartNextCol++)
						{
							var hvoCellPartInNextCol = m_cellparts[icellPartNextCol];
							var nextColCellPart = m_partRepo.GetObject(hvoCellPartInNextCol);
							var hvoColContainingNextCellPart = nextColCellPart.ColumnRA.Hvo;
							if (hvoColContainingCellPart == hvoColContainingNextCellPart)
							{
								continue;
							}
							iNextColumn = GetIndexOfColumn(hvoColContainingNextCellPart);
							// But, if the next column merges before, and there are at least two empty column,
							// give it one of them.
							if (iNextColumn > ihvoNewCol + 2 && nextColCellPart.MergesBefore)
							{
								iNextColumn--; // use one for the merge before.
							}
							break; // found the first cell in a different column, stop.
						}
						ccolsNext = iNextColumn - ihvoNewCol;
					}
					MakeDataCell(ccolsNext);
					m_iLastColForWhichCellExists = ihvoNewCol + ccolsNext - 1;
				}
				m_cCellPartsInCurrentCell = 0; // none in this cell yet.
				AddCellPartToCell(cellPart);
			}

			private void FindCellPartToEndDependentClause()
			{
				var icellPart = m_cellparts.Length - 1;
				while (icellPart >= 0 && !GoesInsideClauseBrackets(m_cellparts[icellPart]))
				{
					icellPart--;
				}
				m_icellPartCloseClause = icellPart >= 0 ? icellPart : m_cellparts.Length - 1;
				// Find the index of the column with the CellPart before the close bracket (plus 1), or if none, start at col 0.
				var icol = 0;
				if (0 <= m_icellPartCloseClause && m_icellPartCloseClause < m_cellparts.Length)
				{
					var cellPart = m_partRepo.GetObject(m_cellparts[m_icellPartCloseClause]);
					icol = GetIndexOfColumn(cellPart.ColumnRA.Hvo) + 1;
				}
				// starting from there find the last column that has the auto-missing property.
				m_icolLastAutoMissing = -1;
				for (; icol < m_chartBody.AllColumns.Length; icol++)
				{
					if (m_chartBody.Logic.ColumnHasAutoMissingMarkers(icol))
					{
						m_icolLastAutoMissing = icol;
					}
				}
				// If we found a subsequent auto-missing column, disable putting the close bracket after the CellPart,
				// it will go after the auto-missing-marker instead.
				if (m_icolLastAutoMissing != -1)
				{
					m_icellPartCloseClause = -1; // terminate after auto-marker.
				}
			}

			private void FindCellPartToStartDependentClause()
			{
				var icellPart = 0;
				while (icellPart < m_cellparts.Length && !GoesInsideClauseBrackets(m_cellparts[icellPart]))
				{
					icellPart++;
				}
				m_icellPartOpenClause = icellPart < m_cellparts.Length ? icellPart : 0;
			}

			private void NoteCellDependencies(int[] cellPartFlidArray, int hvoCellPart)
			{
				var cArray = cellPartFlidArray.Length;
				var hvoArray = new int[cArray];
				for (var i = 0; i < cArray; i++)
				{
					hvoArray[i] = hvoCellPart;
				}

				m_vwenv.NoteDependency(hvoArray, cellPartFlidArray, cArray);
			}

			private void NoteRowDependencies(int[] rowFlidArray)
			{
				var cArray = rowFlidArray.Length;
				var hvoArray = new int[cArray];
				for (var i = 0; i < cArray; i++)
				{
					hvoArray[i] = m_hvoRow;
				}

				m_vwenv.NoteDependency(hvoArray, rowFlidArray, cArray);
			}

			/// <summary>
			/// Report that a CellPart has been detected that has no column, or that is out of order.
			/// We will arbitrarily put it into column hvoCol.
			/// </summary>
			private void ReportAndFixBadCellPart(int hvo, ICmPossibility column)
			{
				if (!m_chartBody.BadChart)
				{
					MessageBox.Show(LanguageExplorerResources.ksFoundAndFixingInvalidDataCells, LanguageExplorerResources.ksInvalidInternalConstituentChartData, MessageBoxButtons.OK, MessageBoxIcon.Information);
					m_chartBody.BadChart = true;
				}

				// Suppress Undo handling...we may fix lots of these, it doesn't make sense for the user to
				// try to Undo it, since it would just get fixed again when we display the chart again.
				var actionHandler = m_cache.ActionHandlerAccessor;
				actionHandler.BeginNonUndoableTask();
				try
				{
					var part = m_partRepo.GetObject(hvo);
					part.ColumnRA = column;
				}
				finally
				{
					actionHandler.EndNonUndoableTask();
				}
			}

			/// <summary>
			/// Answer true if the CellPart should go inside the clause bracketing (if any).
			/// </summary>
			private bool GoesInsideClauseBrackets(int hvoPart)
			{
				if (m_chartBody.Logic.IsWordGroup(hvoPart))
				{
					return true;
				}
				int dummy;
				if (m_chartBody.Logic.IsClausePlaceholder(hvoPart, out dummy))
				{
					return false;
				}
				return !IsListRef(hvoPart);
			}

			private void AddCellPartToCell(IConstituentChartCellPart cellPart)
			{
				var fSwitchBrackets = m_chartBody.IsRightToLeft && !(cellPart is IConstChartWordGroup);
				if (m_cCellPartsInCurrentCell != 0)
				{
					m_vwenv.AddString(m_this.SpaceString);
				}
				m_cCellPartsInCurrentCell++;
				if (m_icellpart == m_icellPartOpenClause && !fSwitchBrackets)
				{
					AddOpenBracketBeforeDepClause();
				}
				// RightToLeft weirdness because non-wordgroup stuff doesn't work right!
				if (m_icellpart == m_icellPartCloseClause && fSwitchBrackets)
				{
					AddCloseBracketAfterDepClause();
				}
				if (ConstituentChartLogic.IsMovedText(cellPart))
				{
					m_vwenv.AddObj(cellPart.Hvo, m_this, ConstChartVc.kfragMovedTextCellPart);
				}
				// Is its target a CmPossibility?
				else if (IsListRef(cellPart))
				{
					// If we're about to add our first CellPart and its a ConstChartTag, see if AutoMissingMarker flies.
					if (m_cCellPartsInCurrentCell == 1 && m_chartBody.Logic.ColumnHasAutoMissingMarkers(m_iLastColForWhichCellExists))
					{
						InsertAutoMissingMarker(m_iLastColForWhichCellExists);
						m_cCellPartsInCurrentCell++;
					}
					m_vwenv.AddObj(cellPart.Hvo, m_this, ConstChartVc.kfragChartListItem);
				}
				// Is its target a user's missing marker (not auto)
				else if (IsMissingMkr(cellPart))
				{
					m_vwenv.AddString(m_missMkr);
				}
				else
				{
					m_vwenv.AddObj(cellPart.Hvo, m_this, ConstChartVc.kfragCellPart);
				}
				if (m_icellpart == m_icellPartCloseClause && !fSwitchBrackets)
				{
					AddCloseBracketAfterDepClause();
				}
				// RightToLeft weirdness because non-wordgroup stuff doesn't work right!
				if (m_icellpart == m_icellPartOpenClause && fSwitchBrackets)
				{
					AddOpenBracketBeforeDepClause();
				}
			}

			private void AddCloseBracketAfterDepClause()
			{
				var key = ConstChartVc.GetRowStyleName(m_row);
				if (m_chartBody.IsRightToLeft)
				{
					m_this.AddRtLCloseBracketWithRLMs(m_vwenv, key);
				}
				else
				{
					m_this.InsertCloseBracket(m_vwenv, key);
				}
			}

			private void AddOpenBracketBeforeDepClause()
			{
				var key = ConstChartVc.GetRowStyleName(m_row);
				if (m_chartBody.IsRightToLeft)
				{
					m_this.AddRtLOpenBracketWithRLMs(m_vwenv, key);
				}
				else
				{
					m_this.InsertOpenBracket(m_vwenv, key);
				}
			}

			/// <summary>
			/// This retrieves logical column index in the RTL case.
			/// </summary>
			private int GetIndexOfColumn(int hvoCol)
			{
				int ihvoNewCol;
				//Enhance: GJM -- This routine used to save time by starting from the last column
				// for which a cell existed. But in the RTL case, things get complicated.
				// For now, I'm just using a generic search through all the columns.
				// If this causes a bottle-neck, we may need to loop in reverse for RTL text.
				var startIndex = m_iLastColForWhichCellExists + 1;
				//var startIndex = 0;
				for (ihvoNewCol = startIndex; ihvoNewCol < m_chartBody.AllColumns.Length; ihvoNewCol++)
				{
					if (hvoCol == m_chartBody.AllColumns[ihvoNewCol].Hvo)
					{
						break;
					}
				}
				return ihvoNewCol;
			}

			private void CloseCurrentlyOpenCell()
			{
				if (m_hvoCurCellCol == 0)
				{
					return;
				}
				m_vwenv.CloseParagraph();
				m_vwenv.CloseTableCell();
			}

			private void MakeRowLabelCell()
			{
				OpenRowNumberCell(m_vwenv);
				m_vwenv.AddStringProp(ConstChartRowTags.kflidLabel, m_this);
				m_vwenv.CloseTableCell();
			}

			internal static void OpenRowNumberCell(IVwEnv vwenv)
			{
				// Row number cell should not be editable [LT-7744].
				vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
				// Row decorator reverses this if chart is RTL.
				vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 500);
				vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Black));
				vwenv.OpenTableCell(1, 1);
			}

			private void MakeEmptyCells(int count)
			{
				for (var i = 0; i < count; i++)
				{
					var icol = i + m_iLastColForWhichCellExists + 1; // display column index
					OpenStandardCell(icol, 1);
					//if (m_chart.Logic.ColumnHasAutoMissingMarkers(m_chart.LogicalFromDisplay(icol)))
					if (m_chartBody.Logic.ColumnHasAutoMissingMarkers(icol))
					{
						m_vwenv.OpenParagraph();
						InsertAutoMissingMarker(icol);
						m_vwenv.CloseParagraph();
					}
					m_vwenv.CloseTableCell();
				}
			}

			private void InsertAutoMissingMarker(int icol)
			{
				// RightToLeft weirdness because non-wordgroup stuff doesn't work right!
				if (icol == m_icolLastAutoMissing && m_chartBody.IsRightToLeft)
				{
					AddCloseBracketAfterDepClause();
				}
				if (m_icellPartOpenClause == m_icellpart && !m_chartBody.IsRightToLeft)
				{
					AddOpenBracketBeforeDepClause();
					m_icellPartOpenClause = -1; // suppresses normal open and in any subsequent auto-missing cells.
				}
				m_vwenv.AddString(m_missMkr);
				if (m_icellPartOpenClause == m_icellpart && m_chartBody.IsRightToLeft)
				{
					AddOpenBracketBeforeDepClause();
					m_icellPartOpenClause = -1; // suppresses normal open and in any subsequent auto-missing cells.
				}
				if (icol == m_icolLastAutoMissing && !m_chartBody.IsRightToLeft)
				{
					AddCloseBracketAfterDepClause();
				}
			}

			private void MakeDataCell(int ccols)
			{
				var icol = GetIndexOfColumn(m_hvoCurCellCol);
				OpenStandardCell(icol, ccols);
				m_vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvDefault, (int)TptEditable.ktptNotEditable);
				m_vwenv.OpenParagraph();
			}

			private void OpenStandardCell(int icol, int ccols)
			{
				if (m_chartBody.Logic.IsHighlightedCell(m_row.IndexInOwner, icol))
				{
					// use m_vwenv.set_IntProperty to set ktptBackColor for cells where the ChOrph could be inserted
					m_vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.LightGreen));
				}
				OpenStandardCell(m_vwenv, ccols, m_chartBody.Logic.GroupEndIndices.Contains(icol));
			}

			private void OpenNoteCell()
			{
				// LT-8545 remaining niggle; Note shouldn't be formatted.
				// A small change to the XML config file ensures it's not underlined either.
				m_vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 1500);
				m_vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Black));
				m_this.ApplyFormatting(m_vwenv, "normal");
				m_vwenv.OpenTableCell(1, 1);
			}

			internal static void OpenStandardCell(IVwEnv vwenv, int ccols, bool fEndOfGroup)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, (fEndOfGroup ? 1500 : 500));
				vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(fEndOfGroup ? Color.Black : Color.LightGray));
				vwenv.OpenTableCell(1, ccols);
			}

			/// <summary>
			/// Return true if the CellPart is a ConstChartTag (which in a CellPart list makes it
			/// a reference to a CmPossibility), also known as a generic marker. But we still
			/// want to return false if the Tag is null, because then its a "Missing" marker.
			/// This version takes the hvo of the CellPart.
			/// </summary>
			private bool IsListRef(int hvoCellPart)
			{
				return IsListRef(m_partRepo.GetObject(hvoCellPart));
			}

			/// <summary>
			/// Return true if the CellPart is a ConstChartTag (which in a CellPart list makes it
			/// a reference to a CmPossibility), also known as a generic marker. But we still
			/// want to return false if the Tag is null, because then its a "Missing" marker.
			/// This version takes the actual CellPart object.
			/// </summary>
			private static bool IsListRef(IConstituentChartCellPart cellPart)
			{
				return (cellPart as IConstChartTag)?.TagRA != null;
			}

			/// <summary>
			/// Return true if the CellPart is a ConstChartTag, but the Tag is null,
			/// because then its a "Missing" marker.
			/// Takes the actual CellPart object.
			/// </summary>
			private static bool IsMissingMkr(IConstituentChartCellPart cellPart)
			{
				var part = cellPart as IConstChartTag;
				return part != null && part.TagRA == null;
			}
		}
	}
}