// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Xml;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.FieldWorks.Common.FwUtils;
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
			var doc = new XmlDocument();
			var path = Path.Combine(FwDirectoryFinder.CodeDirectory, @"Language Explorer/Configuration/ConstituentChartStyleInfo.xml");
			if (!File.Exists(path))
			{
				return;
			}
			doc.Load(path);
			m_formatProps = new Dictionary<string, ITsTextProps>();
			m_brackets = new Dictionary<string, string>();
			foreach (XmlNode item in doc.DocumentElement.ChildNodes)
			{
				if (item is XmlComment)
				{
					continue;
				}
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
					m_brackets[item.Name] = brackets.Trim();
				}
				m_formatProps[item.Name] = bldr.GetTextProps();
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
		/// <param name="widths"></param>
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
						vwenv.set_IntProperty((int) FwTextPropType.ktptAlign, (int) FwTextPropVar.ktpvEnum, (int) FwTextAlign.ktalCenter);
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
				case kfragMovedTextCellPart: // a single group of words (ConstChartWordGroup),
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
				case kfragChartListItem: // a single ConstChartTag, referring to a list item.
					// can't be a placeholder.
					ApplyFormatting(vwenv, "marker");
					vwenv.OpenSpan();
					InsertOpenBracket(vwenv, "marker");
					vwenv.AddObjProp(ConstChartTagTags.kflidTag, this, kfragPossibility);
					InsertCloseBracket(vwenv, "marker");
					vwenv.CloseSpan();
					break;
				case kfragPossibility: // A CmPossibility, show it's abbreviation
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
				case kfragBundle: // One annotated word bundle; hvo is IAnalysis object. Overrides behavior of InterlinVc
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
					vwenv.NoteDependency(new[] {mtt.WordGroupRA.Owner.Hvo}, new int[] {ConstChartRowTags.kflidCells}, 1);
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		private void DisplayWordforms(IVwEnv vwenv, int hvoWordGrp)
		{
			// If the WordGroup reference parameters change, we need to regenerate.
			var wordGrpFlidArray = new[] { ConstChartWordGroupTags.kflidBeginSegment,
				ConstChartWordGroupTags.kflidEndSegment,
				ConstChartWordGroupTags.kflidBeginAnalysisIndex,
				ConstChartWordGroupTags.kflidEndAnalysisIndex};
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
			(vwenv as ChartRowEnvDecorator).IsRtL = m_chart.IsRightToLeft;
			MakeCellsMethod.OpenRowNumberCell(vwenv); // blank cell under header for row numbers
			vwenv.CloseTableCell();
			PrintTemplateColumnHeaders(vwenv, analWs);
			MakeCellsMethod.OpenStandardCell(vwenv, 1, false); // blank cell below Notes header
			vwenv.CloseTableCell();
			(vwenv as ChartRowEnvDecorator).FlushDecorator(); // if RTL, put out headers reversed
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
			vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)FwTextColor.kclrTransparent);
			vwenv.OpenTable(m_chart.AllColumns.Length + ConstituentChartLogic.NumberOfExtraColumns,
				tableWidth,
				1500, // borderWidth
				fRtL ? VwAlignment.kvaRight : VwAlignment.kvaLeft, // Handle RTL
				fpos,
				VwRule.kvrlNone,
				0, // cell spacing
				2000, // cell padding
				true); // selections limited to one cell.
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
				case kfragClauseLabels: // hvo is ConstChartClauseMarker pointing at a group of rows (at least one).
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
			var sbracket = TsStringUtils.MakeString(string.Format(sFormat, bracket.Substring(index, 1)), m_cache.DefaultAnalWs);
			vwenv.AddString(sbracket);
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
			var sbracket = TsStringUtils.MakeString(
				string.Format(sFormat, bracket.Substring(index, 1)), m_cache.DefaultAnalWs);
			vwenv.AddString(sbracket);
		}
	}
}