// Copyright (c) 2015-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.IText;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.Discourse
{
	internal class ConstChartVc : InterlinVc
	{
		// ReSharper disable InconsistentNaming - we like our constants to begin with k
		public const int kfragChart = 3000000; // should be distinct from ones used in InterlinVc
		internal const int kfragChartRow = 3000001;
		internal const int kfragCellPart = 3000002;
		internal const int kfragMovedTextCellPart = 3000003;
		internal const int kfragChartListItem = 3000004;
		internal const int kfragPossibility = 3000005;
		internal const int kfragNotesString = 3000007;
		internal const int kfragPrintChart = 3000009;
		internal const int kfragClauseLabels = 3000012;
		internal const int kfragComment = 3000013;
		internal const int kfragMTMarker = 3000014;
		private const int kflidDepClauses = ConstChartClauseMarkerTags.kflidDependentClauses;
		// ReSharper restore InconsistentNaming

		/// <summary>
		/// Right-to-Left Mark; for flipping individual characters.
		/// </summary>
		internal const char RLM = '\x200F';

		private VwLength[] m_colWidths;
		internal ConstChartBody m_body;
		private Dictionary<string, ITsTextProps> m_formatProps;
		private Dictionary<string, string> m_brackets;
		private readonly IConstChartRowRepository m_rowRepo;
		private readonly IConstChartWordGroupRepository m_wordGrpRepo;
		private readonly IConstituentChartCellPartRepository m_partRepo;
		internal ITsString m_sMovedTextBefore;
		internal ITsString m_sMovedTextAfter;
		private bool m_fIsAnalysisWsGraphiteEnabled;

		public ConstChartVc(ConstChartBody body)
			: base(body.Cache)
		{
			m_body = body;
			m_cache = m_body.Cache;
			SpaceString = TsStringUtils.MakeString(" ", m_cache.DefaultAnalWs);
			m_rowRepo = m_cache.ServiceLocator.GetInstance<IConstChartRowRepository>();
			m_wordGrpRepo = m_cache.ServiceLocator.GetInstance<IConstChartWordGroupRepository>();
			m_partRepo = m_cache.ServiceLocator.GetInstance<IConstituentChartCellPartRepository>();
			m_sMovedTextBefore = TsStringUtils.MakeString(DiscourseStrings.ksMovedTextBefore,
				m_cache.DefaultUserWs);
			m_sMovedTextAfter = TsStringUtils.MakeString(DiscourseStrings.ksMovedTextAfter,
				m_cache.DefaultUserWs);
			LoadFormatProps();
		}

		internal ITsString SpaceString { get; }

		private void LoadFormatProps()
		{
			var doc = new XmlDocument();
			var path = Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer/Configuration/ConstituentChartStyleInfo.xml");
			if (!File.Exists(path))
				return;
			doc.Load(path);
			m_formatProps = new Dictionary<string, ITsTextProps>();
			m_brackets = new Dictionary<string, string>();
			// ReSharper disable once PossibleNullReferenceException - the document in question will always have a DocumentElement
			foreach (XmlNode item in doc.DocumentElement.ChildNodes)
			{
				if (item is XmlComment)
					continue;
				ITsPropsBldr bldr = TsStringUtils.MakePropsBldr();
				var color = XmlUtils.GetOptionalAttributeValue(item, "color", null);
				if (color != null)
					bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
						ColorVal(color.Trim()));
				var underlinecolor = XmlUtils.GetOptionalAttributeValue(item, "underlinecolor", null);
				if (underlinecolor != null)
					bldr.SetIntPropValues((int)FwTextPropType.ktptUnderColor, (int)FwTextPropVar.ktpvDefault,
						ColorVal(underlinecolor.Trim()));
				var underline = XmlUtils.GetOptionalAttributeValue(item, "underline", null);
				if (underline != null)
					bldr.SetIntPropValues((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum,
						InterpretUnderlineType(underline.Trim()));
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
						bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint,
							Convert.ToInt32(sval));
					}
				}
				var bold = XmlUtils.GetOptionalAttributeValue(item, "bold", null);
				if (bold == "true")
				{
					bldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum,
						(int)FwTextToggleVal.kttvInvert);
				}
				var italic = XmlUtils.GetOptionalAttributeValue(item, "italic", null);
				if (italic == "true")
				{
					bldr.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum,
						(int)FwTextToggleVal.kttvInvert);
				}
				var brackets = XmlUtils.GetOptionalAttributeValue(item, "brackets", null);
				if (brackets != null && brackets.Trim().Length == 2)
				{
					m_brackets[item.Name] = brackets.Trim();
				}
				m_formatProps[item.Name] = bldr.GetTextProps();
			}
			m_fIsAnalysisWsGraphiteEnabled = m_cache.LanguageProject.DefaultAnalysisWritingSystem.IsGraphiteEnabled;
			if (m_body.IsRightToLeft)
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
				int firstComma = val.IndexOf(',');
				int red = Convert.ToInt32(val.Substring(1, firstComma - 1));
				int secondComma = val.IndexOf(',', firstComma + 1);
				int green = Convert.ToInt32(val.Substring(firstComma + 1, secondComma - firstComma - 1));
				int blue = Convert.ToInt32(val.Substring(secondComma + 1, val.Length - secondComma - 2));
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
			if (m_formatProps.TryGetValue(key, out var ttp))
				vwenv.Props = ttp;
		}

		/// <summary>
		/// (Default) width of the number column (in millipoints).
		/// </summary>
		internal int NumColWidth => m_body.NumColWidth;

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
				case kfragPrintChart: // the whole chart with headers for printing.
					// This is used only for printing and exporting; the on-screen headers use separate code
					if (hvo == 0)
						return;
					for (var headerDepth = 0; headerDepth < m_body.Logic.ColumnsAndGroups.Headers.Count; headerDepth++)
					{
						PrintAnyLevelColumnHeaders(hvo, vwenv, headerDepth);
					}
					// Rest is same as kfragChart
					DisplayChartBody(vwenv);
					break;
				case kfragChart: // the whole chart (except headers), a DsConstChart.
					if (hvo == 0)
						return;
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
					if (m_body.Logic.IsWordGroup(hvo))
						DisplayWordforms(vwenv, hvo);
					else
					{
						// it's a moved text or clause reference placeholder.
						if (m_body.Logic.IsClausePlaceholder(hvo, out var hvoClause))
							DisplayClausePlaceholder(vwenv, hvoClause);
						else
							DisplayMovedTextTag(hvo, vwenv);
					}
					break;
				case kfragMovedTextCellPart: // a single group of words (ConstChartWordGroup),
											 // the contents of one cell, which is considered moved-within-line.
											 // Can't be a placeholder.
					var formatTag = m_body.Logic.MovedTextTag(hvo);
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
						vwenv.AddStringAltMember(flid, retWs, this);
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
					vwenv.NoteDependency(new[] { mtt.WordGroupRA.Owner.Hvo }, new[] { ConstChartRowTags.kflidCells }, 1);
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
				hvoArray[i] = hvoWordGrp;

			vwenv.NoteDependency(hvoArray, wordGrpFlidArray, cArray);
		}

		/// <summary>
		/// Chart version
		/// </summary>
		/// <param name="hvo">the IAnalysis object</param>
		/// <param name="vwenv"></param>
		protected override void AddWordBundleInternal(int hvo, IVwEnv vwenv)
		{
			SetupAndOpenInnerPile(vwenv);
			// we assume we're in the context of a segment with analyses here.
			// we'll need this info down in DisplayAnalysisAndCloseInnerPile()
			vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out var hvoSeg, out _, out var index);
			var analysisOccurrence = new AnalysisOccurrence(m_segRepository.GetObject(hvoSeg), index);
			DisplayAnalysisAndCloseInnerPile(vwenv, analysisOccurrence, false);
		}

		/// <summary>
		/// Setup a box with 5 under and trailing, plus leading alignment, and open the inner pile
		/// </summary>
		protected override void SetupAndOpenInnerPile(IVwEnv vwenv)
		{
			// Make an 'inner pile' to contain the wordform and its interlinear.
			// Give whatever box we make 5 points of separation from whatever follows.
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
				(int)FwTextPropVar.ktpvMilliPoint, 5000);
			// 5 points below also helps space out the paragraph.
			vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
				(int)FwTextPropVar.ktpvMilliPoint, 5000);
			vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum,
				(int)FwTextAlign.ktalLeading);
			vwenv.OpenInnerPile();
		}

		private void PrintAnyLevelColumnHeaders(int chartHvo, IVwEnv vwEnv, int depth)
		{
			var analWs = m_cache.DefaultAnalWs;

			MakeTableAndRowWithStdWidths(vwEnv, chartHvo, true);
			var rtlDecorator = new ChartRowEnvDecorator(vwEnv) { IsRtL = m_body.IsRightToLeft }; // in case this is a RTL chart
			if (!(m_body.Chart.NotesColumnOnRight ^ m_body.IsRightToLeft))
				PrintNotesCellHeader(rtlDecorator, analWs, depth == 0);
			PrintRowNumCellHeader(rtlDecorator); // column header for row numbers
			// for each column or placeholder at this level
			foreach (var header in m_body.Logic.ColumnsAndGroups.Headers[depth])
			{
				MakeCellsMethod.OpenStandardCell(rtlDecorator, header.ColumnCount, header.IsLastInGroup);
				if (header.Label != null)
				{
					rtlDecorator.AddString(header.Label);
				}
				rtlDecorator.CloseTableCell();
			}
			if (m_body.Chart.NotesColumnOnRight ^ m_body.IsRightToLeft)
				PrintNotesCellHeader(rtlDecorator, analWs, depth == 0);
			rtlDecorator.FlushDecorator(); // if RTL, put out headers reversed
			vwEnv.CloseTableRow();
			vwEnv.CloseTable();
		}

		private static void PrintNotesCellHeader(IVwEnv vwenv, int analWs, bool wantLabel)
		{
			MakeCellsMethod.OpenStandardCell(vwenv, 1, true);
			if (wantLabel)
				vwenv.AddString(TsStringUtils.MakeString(DiscourseStrings.ksNotesColumnHeader, analWs));
			vwenv.CloseTableCell();
		}

		private static void PrintRowNumCellHeader(IVwEnv vwenv)
		{
			MakeCellsMethod.OpenRowNumberCell(vwenv);
			vwenv.CloseTableCell();
		}

		private void DisplayMovedTextTag(int hvo, IVwEnv vwenv)
		{
			// hvo is a ConstChartMovedTextMarker
			var mtt = m_partRepo.GetObject(hvo) as IConstChartMovedTextMarker;
			Debug.Assert(mtt != null, "Hvo is not for a MovedText Marker.");
			var formatTag1 = m_body.Logic.MovedTextTag(mtt.WordGroupRA.Hvo) + "Mkr";
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
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="fHeader">true if it is a header; hvo is a chart instead of a row.</param>
		private void MakeTableAndRowWithStdWidths(IVwEnv vwenv, int hvo, bool fHeader)
		{
			IConstChartRow row = null;
			if (!fHeader)
				row = m_rowRepo.GetObject(hvo);
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
					tableWidth.nVal += w.nVal;
				tableWidth.unit = VwUnit.kunPoint1000;
			}
			if (!fHeader)
				SetRowStyle(vwenv, row);

			var framePosition = VwFramePosition.kvfpVsides;
			if (fHeader)
			{
				framePosition = (VwFramePosition)((int)framePosition | (int)VwFramePosition.kvfpAbove);
			}
			else
			{
				vwenv.GetOuterObject(0, out var hvoOuter, out var tagOuter, out var iHvoRow);
				if (iHvoRow == 0)
				{
					framePosition = (VwFramePosition)((int)framePosition | (int)VwFramePosition.kvfpAbove);
				}
				if (iHvoRow == vwenv.DataAccess.get_VecSize(hvoOuter, tagOuter) - 1
					|| row.EndParagraph)
				{
					framePosition = (VwFramePosition)((int)framePosition | (int)VwFramePosition.kvfpBelow);
				}
			}
			// We seem to typically inherit a white background as a side effect of setting our stylesheet,
			// but borders on table rows don't show through if BackColor is set to white, because the
			// cells entirely cover the row (LT-9068). So force the back color to be transparent, and allow
			// the row border to show through the cell.
			var fRtL = m_body.IsRightToLeft;
			vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)FwTextColor.kclrTransparent);
			vwenv.OpenTable(m_body.AllColumns.Length + ConstituentChartLogic.NumberOfExtraColumns,
				tableWidth,
				1500, // borderWidth
				fRtL ? VwAlignment.kvaRight : VwAlignment.kvaLeft, // Handle RTL
				framePosition,
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
				//do not make columns until m_colWidths has been updated for new Template
				if (m_colWidths.Length == m_body.AllColumns.Length + ConstituentChartLogic.NumberOfExtraColumns)
				{
					foreach (var colWidth in m_colWidths)
						vwenv.MakeColumns(1, colWidth);
				}
			}
			// Set row bottom border color and size of table body rows
			if (!fHeader)
			{
				if (row.EndSentence)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor,
						(int)FwTextPropVar.ktpvDefault,
						(int)ColorUtil.ConvertColorToBGR(Color.Black));
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom,
						(int)FwTextPropVar.ktpvMilliPoint, 1000);
				}
				else
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor,
						(int)FwTextPropVar.ktpvDefault,
						(int)ColorUtil.ConvertColorToBGR(Color.LightGray));
					vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom,
						(int)FwTextPropVar.ktpvMilliPoint, 500);
				}
			}

			vwenv.OpenTableRow();
		}

		private void MakeColumnsOtherThanRowNum(IVwEnv vwenv)
		{
			var colWidth = new VwLength { nVal = 1, unit = VwUnit.kunRelative };
			const int followingCols = ConstituentChartLogic.NumberOfExtraColumns -
									  ConstituentChartLogic.indexOfFirstTemplateColumn;
			vwenv.MakeColumns(m_body.AllColumns.Length + followingCols, colWidth);
		}

		private void MakeRowNumColumn(IVwEnv vwenv)
		{
			var numColWidth = new VwLength { nVal = NumColWidth, unit = VwUnit.kunPoint1000 };
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
					var vecSize = sda.get_VecSize(hvo, kflidDepClauses);
					var hvoFirst = sda.get_VecItem(hvo, kflidDepClauses, 0);
					vwenv.AddObj(hvoFirst, this, kfragComment);
					if (vecSize == 1)
						break;
					var sHyphen = TsStringUtils.MakeString("-", m_cache.DefaultAnalWs);
					vwenv.AddString(sHyphen);
					var hvoLast = sda.get_VecItem(hvo, kflidDepClauses, vecSize - 1);
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
			new MakeCellsMethod(this, m_cache, vwenv, hvoRow).Run(m_body.IsRightToLeft);
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
			vwenv.set_IntProperty((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum,
				(int)FwUnderlineType.kuntNone);
			ApplyFormatting(vwenv, "gloss");
		}

		// This used to be kAnnotationColor. I'm a little confused as to its actual meaning here.
		// ReSharper disable InconsistentNaming - kWordformColor is like a constant
		private readonly int kWordformColor = (int)ColorUtil.ConvertColorToBGR(Color.DarkGray);
		// ReSharper restore InconsistentNaming

		/// <summary>
		/// A nasty kludge, but everything gray should also be underlined.
		/// </summary>
		protected override void SetColor(IVwEnv vwenv, int color)
		{
			base.SetColor(vwenv, color);
			if (color == kWordformColor)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum,
					(int)FwUnderlineType.kuntNone);
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
			if (!m_brackets.TryGetValue(key, out var bracket))
				return;
			InsertOpenBracketInternal(vwenv, bracket, false);
		}

		internal void AddRtLOpenBracketWithRLMs(IVwEnv vwenv, string key)
		{
			if (!m_brackets.TryGetValue(key, out var bracket))
				return;
			InsertOpenBracketInternal(vwenv, bracket, true);
		}

		private void InsertOpenBracketInternal(IVwEnv vwenv, string bracket, bool fRtL)
		{
			var index = 0;
			var sFormat = "{0}";
			if (fRtL)
			{
				sFormat = RLM + sFormat + RLM;
				// TODO (Hasso) 2022.03: For RTL prints (not exports), the brackets are in the right place, but facing the wrong way. This is true even when it shouldn't be.
				if (m_fIsAnalysisWsGraphiteEnabled)
					index = 1;
			}
			var sBracket = TsStringUtils.MakeString(string.Format(sFormat, bracket.Substring(index, 1)), m_cache.DefaultAnalWs);
			vwenv.AddString(sBracket);
		}

		internal void InsertCloseBracket(IVwEnv vwenv, string key)
		{
			if (!m_brackets.TryGetValue(key, out var bracket))
				return;
			InsertCloseBracketInternal(vwenv, bracket, false);
		}

		internal void AddRtLCloseBracketWithRLMs(IVwEnv vwenv, string key)
		{
			if (!m_brackets.TryGetValue(key, out var bracket))
				return;
			InsertCloseBracketInternal(vwenv, bracket, true);
		}

		private void InsertCloseBracketInternal(IVwEnv vwenv, string bracket, bool fRtL)
		{
			var index = 1;
			var sFormat = "{0}";
			if (fRtL)
			{
				sFormat = RLM + sFormat + RLM;
				if (m_fIsAnalysisWsGraphiteEnabled)
					index = 0;
			}
			var sBracket = TsStringUtils.MakeString(string.Format(sFormat, bracket.Substring(index, 1)), m_cache.DefaultAnalWs);
			vwenv.AddString(sBracket);
		}
	}
}
