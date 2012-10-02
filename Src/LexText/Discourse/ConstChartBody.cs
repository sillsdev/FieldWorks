using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.Common.Utils;
using System.Diagnostics;
using XCore;
using System.IO;
using SIL.Utils;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// The main body of the chart, containing the actual view of the charted words.
	/// </summary>
	public partial class ConstChartBody : RootSite
	{
		InterlinLineChoices m_lineChoices;
		int m_hvoChart;
		int[] m_AllColumns;
		ConstChartVc m_vc;
		int m_dxNumColWidth = 25000; // millipoints
		ConstituentChartLogic m_logic;
		ConstituentChart m_chart;
		Button m_hoverButton;
		/// <summary>
		/// The context menu displayed for a cell.
		/// </summary>
		ContextMenuStrip m_cellContextMenu;
		/// <summary>
		/// Flag that we've detected some bad in the chart data (such as a deleted or
		/// moved column).
		/// </summary>
		bool m_fBadChart = false;
		long m_ticksWhenContextMenuClosed = 0;

		/// <summary>
		/// Make one.
		/// </summary>
		public ConstChartBody(ConstituentChartLogic logic, ConstituentChart chart)
			: base(null)
		{
			m_logic = logic;
			m_logic.RowModifiedEvent += new RowModifiedEventHandler(m_logic_RowModifiedEvent);
			m_chart = chart;
			//this.ReadOnlyView = true;
		}

		internal IVwViewConstructor Vc
		{
			get { return m_vc; }
		}

		/// <summary>
		/// measures the width of the strings built by the display of a column and
		/// returns the maximumn width found.
		/// NOTE: you may need to add a small (e.g. under 10-pixel) margin to prevent wrapping in most cases.
		/// </summary>
		/// <returns>width in pixels</returns>
		public int GetColumnContentsWidth(int icolChanged)
		{
			// Review: This WaitCursor doesn't seem to work. Anyone know why?
			using (new WaitCursor())
			{
				using (Graphics g = Graphics.FromHwnd(Handle))
				{
					// get a best  estimate to determine row needing the greatest column width.
					MaxStringWidthForChartColumn env = new MaxStringWidthForChartColumn(m_vc, m_styleSheet, Cache.MainCacheAccessor,
																						m_hvoChart, g, icolChanged);
					Cache.EnableBulkLoadingIfPossible(true);
					try
					{
						Vc.Display(env, m_hvoChart, ConstChartVc.kfragChart);
					}
					finally
					{
						Cache.EnableBulkLoadingIfPossible(false);
					}
					return env.MaxStringWidth;
				}
			}
		}

		void m_logic_RowModifiedEvent(object sender, RowModifiedEventArgs e)
		{
			ICmIndirectAnnotation row = e.Row;
			if (row == null)
				return;
			IVwSelection sel = MakeRowSelection(row, false);
			this.ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
			//this.ScrollToEnd();
		}

		private IVwSelection MakeRowSelection(ICmIndirectAnnotation row, bool fInstall)
		{
			SelLevInfo[] rgvsli = new SelLevInfo[1];
			rgvsli[0].ihvo = m_logic.IndexOfRow(row.Hvo); // specifies which row.
			rgvsli[0].tag = (int)DsConstChart.DsConstChartTags.kflidRows;
			IVwSelection sel = null;
			try
			{
				sel = RootBox.MakeTextSelInObj(0, 1, rgvsli, 0, null, false, false, false, true, fInstall);
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.StackTrace);
				return null;
			}
			return sel;
		}

		/// <summary>
		/// Selects and scrolls to the bookmarked location in the constituent chart.
		/// </summary>
		/// <param name="bookmark"></param>
		internal void SelectAndScrollToBookmark(InterAreaBookmark bookmark)
		{
			CheckDisposed();

			Debug.Assert(bookmark != null);
			Debug.Assert(bookmark.IndexOfParagraph >= 0);

			if (m_chart == null || m_logic.Chart.RowsRS.Count < 1)
				return; // nothing to do (and leave the bookmark alone)

			// Gets the Twfic annotation that is closest to the bookmark in the text
			int annHvo = m_logic.FindAnnAtBookmark(bookmark);
			if (annHvo < 1)
			{
				Debug.Assert(annHvo > 0, "Unable to find annotation close to bookmark");
				return;
			}
			ChartLocation chartLoc = m_logic.FindChartLocOfWfic(annHvo);
			if (chartLoc != null && chartLoc.IsValidLocation)
			{
				SelectAndScrollToLoc(chartLoc, true);
				return;
			}
			// Otherwise, Bookmark is for a Twfic that is not yet charted.
			m_chart.ScrollToEndOfChart();
		}

		/// <summary>
		/// Selects and scrolls to the bookmarked location in the constituent chart. This version
		/// assumes the bookmarked location has been charted, since the location is passed as a parameter.
		/// </summary>
		/// <param name="chartLoc">A ChartLocation object, created by CCLogic.FindChartLocOfWfic().</param>
		/// <param name="fbookmark">true if called for a bookmark, false if called for ChOrph highlighting</param>
		internal void SelectAndScrollToLoc(ChartLocation chartLoc, bool fbookmark)
		{
			Debug.Assert(m_chart != null);
			Debug.Assert(chartLoc != null);
			Debug.Assert(chartLoc.RowAnn != null);

			// The following will select the row of the bookmark
			ICmIndirectAnnotation row = chartLoc.RowAnn;
			IVwSelection sel = MakeRowSelection(row, true);
			if (fbookmark)
				ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoTop);
			else
				ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoNearTop);
			//Update(); ScrollSelectionIntoView() does this, I believe.
		}

		/// <summary>
		/// Width of the number column (in millipoints).
		/// </summary>
		internal int NumColWidth
		{
			get { return m_dxNumColWidth; }
		}

		internal ConstituentChartLogic Logic
		{
			get { return m_logic; }
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			m_hoverButton = new Button();
			Image pullDown = SIL.FieldWorks.Resources.ResourceHelper.BlueCircleDownArrowForView;
			m_hoverButton.Image = pullDown;
			m_hoverButton.Height = pullDown.Height + 4;
			m_hoverButton.Width = pullDown.Width + 4;
			m_hoverButton.FlatStyle = FlatStyle.Flat;
			m_hoverButton.ForeColor = this.BackColor;
			m_hoverButton.BackColor = this.BackColor;
			m_hoverButton.Click += new EventHandler(m_hoverButton_Click);
		}

		void m_hoverButton_Click(object sender, EventArgs e)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			if (Controls.Contains(m_hoverButton))
				Controls.Remove(m_hoverButton);
			base.OnMouseLeave(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			ChartLocation cell;
			int irow;
			if (GetCellInfo(e, out cell, out irow))
			{
				SelLevInfo[] info = new SelLevInfo[1];
				info[0].ihvo = irow;
				info[0].tag = (int)DsConstChart.DsConstChartTags.kflidRows;
				info[0].cpropPrevious = 0;
				// Makes a selection near the start of the row.
				IVwSelection sel = RootBox.MakeTextSelInObj(0, 1, info, 0, null, true, false, false, false, false);
				if (sel != null)
				{
					Rect rcPrimary;
					using (new HoldGraphics(this))
					{
						Rectangle rcSrcRoot, rcDstRoot;
						Rect rcSec;
						bool fSplit, fEndBeforeAnchor;
						GetCoordRects(out rcSrcRoot, out rcDstRoot);
						sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary,
							out rcSec, out fSplit, out fEndBeforeAnchor);
					}
					m_hoverButton.Location = new Point(m_chart.ColumnPositions[cell.ColIndex + 2] - 4 - m_hoverButton.Width,
						rcPrimary.top);
					if (!Controls.Contains(m_hoverButton))
						Controls.Add(m_hoverButton);
				}
			}
			else if (Controls.Contains(m_hoverButton))
			{
				Controls.Remove(m_hoverButton);
			}
			base.OnMouseMove(e);
		}

		public void SetColWidths(int[] widths)
		{
			int ccol = widths.Length;
			VwLength[] lengths = new VwLength[ccol];
			for (int icol = 0; icol < ccol; icol++)
			{
				VwLength len = new VwLength();
				len.nVal = widths[icol];
				len.unit = VwUnit.kunPoint1000;
				lengths[icol] = len;
			}
			// We seem to need to tweak the first width to make things line up,
			// possibly because of the left border.
			lengths[0].nVal -= 1000;
			if (RootBox != null)
				RootBox.SetTableColWidths(lengths, ccol);
			m_vc.SetColWidths(lengths);
		}

		internal InterlinLineChoices LineChoices
		{
			get { return m_lineChoices; }
			set { m_lineChoices = value; }
		}
		public override void MakeRoot()
		{
			CheckDisposed();

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);

			m_vc = new ConstChartVc(this);
			m_vc.LineChoices = m_lineChoices;
			// may be needed..normally happens when the VC displays a top-level paragraph.
			//SetupRealVernWsForDisplay(m_cache.LangProject.ActualWs(LangProject.kwsVernInParagraph,
			//    hvo, (int)StText.StTextTags.kflidParagraphs));

			m_rootb.DataAccess = Cache.MainCacheAccessor;
			m_rootb.SetRootObject(m_hvoChart, m_vc, ConstChartVc.kfragChart, this.StyleSheet);

			base.MakeRoot();
			//m_rootb.Activate(VwSelectionState.vssOutOfFocus); // Makes selection visible even before ever got focus.
		}

		/// <summary>
		/// Change the root chart.
		/// </summary>
		/// <param name="hvoStText"></param>
		public void SetRoot(int hvoChart, int[] allColumns)
		{
			if (m_hvoChart == hvoChart && m_AllColumns == allColumns)
				return;
			m_fBadChart = false;	// new chart, new possibilities for problems...
			m_hvoChart = hvoChart;
			m_AllColumns = allColumns;
			if (RootBox != null)
				ChangeOrMakeRoot(m_hvoChart, m_vc, ConstChartVc.kfragChart, this.StyleSheet);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (m_cellContextMenu != null)
				{
					m_cellContextMenu.Dispose();
					m_cellContextMenu = null;
				}
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// All the columns we're displaying.
		/// </summary>
		public int[] AllColumns
		{
			get { return m_AllColumns; }
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (System.DateTime.Now.Ticks - m_ticksWhenContextMenuClosed > 50000) // 5ms!
			{
				// Consider bringing up another menu only if we weren't already showing one.
				// The above time test seems to be the only way to find out whether this click closed the last one.
				ChartLocation cell;
				int irow;
				if (GetCellInfo(e, out cell, out irow))
				{
					m_cellContextMenu = m_logic.MakeCellContextMenu(cell);
					m_cellContextMenu.Closed += new ToolStripDropDownClosedEventHandler(m_cellContextMenu_Closed);
					m_cellContextMenu.Show(this, e.X, e.Y);
					return; // Don't call the base method, we don't want to make a selection.
				}
				else if (cell != null && cell.IsValidLocation && cell.ColIndex >= m_AllColumns.Length)
				{
					// Click in Notes...make sure it has one.
					if (cell.RowAnn.TextOAHvo == 0)
					{
						IStText newText = cell.RowAnn.TextOA = new StText();
						newText.ParagraphsOS.Append(new StTxtPara());
						// Somehow the system doesn't seem to notice the new objects,maybe because there's initially
						// nothing in the new StText. This seems to help.
						Cache.PropChanged(cell.HvoRow, (int)CmAnnotation.CmAnnotationTags.kflidText, 0, 1, 1);
					}
				}
			}
			base.OnMouseDown(e);
		}

		void m_cellContextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			m_ticksWhenContextMenuClosed = System.DateTime.Now.Ticks;
		}

		/// <summary>
		/// Get info about which cell the user clicked in.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="clickedCell"></param>
		/// <param name="irow"></param>
		/// <returns>true if it is a template column, or false if some other column (Notes?)</returns>
		private bool GetCellInfo(MouseEventArgs e, out ChartLocation clickedCell, out int irow)
		{
			clickedCell = null; // in case of premature 'return'
			irow = -1;
			int icol = -1;
			if (m_hvoChart == 0 || m_AllColumns == null || e.Y > m_rootb.Height)
				return false;
			Point pt;
			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			using (new HoldGraphics(this))
			{
				pt = PixelToView(new Point(e.X, e.Y));
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				IVwSelection sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
				if (sel == null)
					return false;
				TextSelInfo info = new TextSelInfo(sel);
				if (info.Levels(false) < 2)
					return false;
				irow = GetIndexOfTopLevelObject(info, false);
				IDsConstChart chart = DsConstChart.CreateFromDBObject(Cache, m_hvoChart);
				Debug.Assert(irow >= 0 && irow < chart.RowsRS.Count);
				icol = m_logic.GetColumnFromPosition(e.X, m_chart.ColumnPositions) - 1;
				clickedCell = new ChartLocation(icol, chart.RowsRS[irow]);
				// return true if we clicked on a valid template column (other than notes)
				// return false if we clicked on an 'other' column, like notes or row number?
				return icol > -1 && icol < m_AllColumns.Length;
			}
		}

		protected override void GetPrintInfo(out int hvo, out IVwViewConstructor vc, out int frag, out IVwStylesheet ss)
		{
			base.GetPrintInfo(out hvo, out vc, out frag, out ss);
			frag = ConstChartVc.kfragPrintChart;
		}

		/// <summary>
		/// The constituent chart typically wants to take up all the page it can, in landscape mode!
		/// Enhance JohnT: eventually we should have a page layout diagram that allows at least this
		/// to be controlled.
		/// </summary>
		/// <param name="dlg"></param>
		public override void AdjustPrintDialog(PrintDialog dlg)
		{
			base.AdjustPrintDialog(dlg);
			dlg.Document.DefaultPageSettings.Landscape = true;
			dlg.Document.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(50, 50, 50, 50);
		}

		private static int GetIndexOfTopLevelObject(TextSelInfo info, bool fEndPoint)
		{
			return info.ContainingObjectIndex(info.Levels(fEndPoint) - 1, fEndPoint);
		}

		/// <summary>
		///  Give access to the "bad chart" flag.
		/// </summary>
		internal bool BadChart
		{
			get { return m_fBadChart; }
			set { m_fBadChart = value; }
		}
	}

	internal class ConstChartVc : InterlinVc
	{
		public const int kfragChart = 3000000; // should be distinct from ones used in InterlinVc
		internal const int kfragChartRow = 3000001;
		internal const int kfragCca = 3000002;
		internal const int kfragCcaMoved = 3000003;
		internal const int kfragCcaListItem = 3000004;
		const int kfragPossibility = 3000005;
		const int kfragBundleVec = 3000006;
		internal const int kfragNotesText = 3000007;
		const int kfragNotesPara = 3000008;
		internal const int kfragPrintChart = 3000009;
		const int kfragTemplateHeader = 3000010;
		internal const int kfragColumnGroupHeader = 3000011;
		const int kfragClauseLabels = 3000012;
		internal const int kfragComment = 3000013;
		VwLength[] m_colWidths;
		internal ConstChartBody m_chart;
		Dictionary<string, ITsTextProps> m_formatProps = new Dictionary<string, ITsTextProps>();
		Dictionary<string, string> m_brackets = new Dictionary<string, string>();
		ITsString m_tssSpace;

		public ConstChartVc(ConstChartBody chart)
			: base(chart.Cache)
		{
			m_chart = chart;
			m_cache = m_chart.Cache;
			m_tssSpace = m_cache.MakeAnalysisTss(" ");
			LoadFormatProps();
		}

		const int kflidAppliesTo = (int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo;

		internal ITsString SpaceString
		{
			get { return m_tssSpace; }
		}

		private void LoadFormatProps()
		{
			XmlDocument doc = new XmlDocument();
			string path = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\Configuration\ConstituentChartStyleInfo.xml");
			if (!File.Exists(path))
				return;
			doc.Load(path);
			foreach (XmlNode item in doc.DocumentElement.ChildNodes)
			{
				if (item is XmlComment)
					continue;
				ITsPropsBldr bldr = TsPropsBldrClass.Create();
				string color = XmlUtils.GetOptionalAttributeValue(item, "color", null);
				if (color != null)
					bldr.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
						ColorVal(color.Trim()));
				string underlinecolor = XmlUtils.GetOptionalAttributeValue(item, "underlinecolor", null);
				if (underlinecolor != null)
					bldr.SetIntPropValues((int)FwTextPropType.ktptUnderColor, (int)FwTextPropVar.ktpvDefault,
						ColorVal(underlinecolor.Trim()));
				string underline = XmlUtils.GetOptionalAttributeValue(item, "underline", null);
				if (underline != null)
					bldr.SetIntPropValues((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum,
						InterpretUnderlineType(underline.Trim()));
				string fontsize = XmlUtils.GetOptionalAttributeValue(item, "fontsize", null);
				if (fontsize != null)
				{
					string sval = fontsize.Trim();
					if (sval[sval.Length - 1] == '%')
					{
						sval = sval.Substring(0, sval.Length - 1); // strip %
						int percent = Convert.ToInt32(sval);
						bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvRelative, percent * 100);
					}
					else
					{
						bldr.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint,
							Convert.ToInt32(sval));
					}
				}
				string bold = XmlUtils.GetOptionalAttributeValue(item, "bold", null);
				if (bold == "true")
				{
					bldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum,
						(int)FwTextToggleVal.kttvInvert);
				}
				string italic = XmlUtils.GetOptionalAttributeValue(item, "italic", null);
				if (italic == "true")
				{
					bldr.SetIntPropValues((int)FwTextPropType.ktptItalic, (int)FwTextPropVar.ktpvEnum,
						(int)FwTextToggleVal.kttvInvert);
				}
				string brackets = XmlUtils.GetOptionalAttributeValue(item, "brackets", null);
				if (brackets != null && brackets.Trim().Length == 2)
				{
					m_brackets[item.Name] = brackets.Trim();
				}
				m_formatProps[item.Name] = bldr.GetTextProps();
			}
		}

		/// <summary>
		/// Interpret at color value, which can be one of the KnownColor names or (R, G, B).
		/// The result is what the Views code expects for colors.
		/// </summary>
		/// <param name="val"></param>
		/// <returns></returns>
		static int ColorVal(string val)
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
			Color col = Color.FromName(val);
			return col.R + (col.B * 256 + col.G) * 256;
		}

		/// <summary>
		/// Interpret an underline type string as an FwUnderlineType.
		/// Copied from XmlViews (to avoid yet another reference).
		/// </summary>
		/// <param name="strVal"></param>
		/// <returns></returns>
		static int InterpretUnderlineType(string strVal)
		{
			int val = (int)FwUnderlineType.kuntSingle; // default
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
				default:
					Debug.Assert(false, "Expected value single, none, double, dotted, dashed, or squiggle");
					break;
			}
			return val;
		}

		internal void ApplyFormatting(IVwEnv vwenv, string key)
		{
			ITsTextProps ttp;
			if (m_formatProps.TryGetValue(key, out ttp))
				vwenv.Props = ttp;
		}

		/// <summary>
		/// (Default) width of the number column (in millipoints).
		/// </summary>
		internal int NumColWidth
		{
			get { return m_chart.NumColWidth; }
		}

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
						return;
					PrintColumnGroupHeaders(hvo, vwenv);
					PrintIndividualColumnHeaders(hvo, vwenv);
					// Rest is same as kfragChart
					DisplayChartBody(vwenv);
					break;
				case kfragTemplateHeader: // Display the template as group headers.
					vwenv.AddObjVecItems((int)CmPossibility.CmPossibilityTags.kflidSubPossibilities, this, kfragColumnGroupHeader);
					break;

					// This is only used for printing, the headers in the screen version are a separate control.
				case kfragColumnGroupHeader:
					int ccols = vwenv.DataAccess.get_VecSize(hvo, (int)CmPossibility.CmPossibilityTags.kflidSubPossibilities);
					// If there are no subitems, we still want a blank cell as a placeholder.
					MakeCellsMethod.OpenStandardCell(vwenv, Math.Max(ccols, 1), true);
					if (ccols > 0)
					{
						// It's a group, include its name
						vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum,
							(int)FwTextAlign.ktalCenter);
						vwenv.OpenParagraph();
						vwenv.AddString(CmPossibility.BestAnalysisName(m_cache, hvo));
						vwenv.CloseParagraph();
					}
					vwenv.CloseTableCell();
					break;
				case kfragChart: // the whole chart, a DsConstChart.
					if (hvo == 0)
						return;
					DisplayChartBody(vwenv);
					break;
				case kfragChartRow: // one row, a CmIndirectAnnotation
					{
						MakeTableAndRowWithStdWidths(vwenv, hvo, false);

						MakeCells(vwenv, hvo);
						vwenv.CloseTableRow();
						vwenv.CloseTable();
					}
					break;
				case kfragCca: // a single group of words, the contents of one cell.
					if (ConstituentChartLogic.IsWficGroup(m_cache, hvo))
						vwenv.AddObjVec(kflidAppliesTo, this, kfragBundleVec);
					else
					{
						// it's a moved text or missing-item placeholder.
						int hvoClause;
						if (m_chart.Logic.IsClausePlaceholder(hvo, out hvoClause))
							DisplayClausePlaceholder(vwenv, hvoClause);
						else if (vwenv.DataAccess.get_VecSize(hvo, kflidAppliesTo) == 0)
							DisplayMissingMarker(vwenv);
						else
							DisplayMovedTextTag(hvo, vwenv);
					}
					break;
				case kfragCcaMoved: // a single group of words, the contents of one cell, which is considered moved-within-line.
					// can't be a placeholder.
					string formatTag = m_chart.Logic.MovedTextTag(hvo);
					ApplyFormatting(vwenv, formatTag);
					vwenv.OpenSpan();
					InsertOpenBracket(vwenv, formatTag);
					vwenv.AddObjVec(kflidAppliesTo, this, kfragBundleVec);
					InsertCloseBracket(vwenv, formatTag);
					vwenv.CloseSpan();
					break;
				case kfragCcaListItem: // a single CCA, referring to a list item.
					// can't be a placeholder.
					ApplyFormatting(vwenv, "marker");
					vwenv.OpenSpan();
					InsertOpenBracket(vwenv, "marker");
					vwenv.AddObjProp((int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject,
						this, kfragPossibility);
					InsertCloseBracket(vwenv, "marker");
					vwenv.CloseSpan();
					break;
				case kfragPossibility: // A CmPossibility, show it's abbreviation
					int flid = (int)CmPossibility.CmPossibilityTags.kflidAbbreviation;
					int retWs;
					m_cache.LangProject.GetMagicStringAlt(LangProject.kwsFirstAnal, hvo, flid, false, out retWs);
					// If we didn't find an abbreviation try for the name
					ITsString tss = null;
					if (retWs != 0)
						tss = m_cache.GetMultiStringAlt(hvo, flid, retWs);
					if (tss == null || string.IsNullOrEmpty(tss.Text))
					{
						flid = (int)CmPossibility.CmPossibilityTags.kflidName;
						m_cache.LangProject.GetMagicStringAlt(LangProject.kwsFirstAnal, hvo, flid, false, out retWs);
					}
					// Unless we didn't get anything, go ahead and insert the best option we found.
					if (retWs != 0)
						vwenv.AddStringAltMember(flid, retWs, this);
					// retWS was m_cache.DefaultAnalWs, this fixes LT-7838
					break;
				case kfragBundle: // One annotated word bundle; hvo is CmBaseAnnotation. Overrides behavior of InterlinVc
					{
						SetupForTwfic(hvo);
						// Make an 'inner pile' to contain the wordform and annotations.
						// 10 points below also helps space out the paragraph.
						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
							(int)FwTextPropVar.ktpvMilliPoint, 5000);
						vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum,
							(int)FwTextAlign.ktalLeading);
						vwenv.OpenInnerPile();
						// Get the instanceOf property of the annotation and see whether it exists. If not it is
						// just a punctuation annotation, and we just insert the form.
						vwenv.NoteDependency(new int[] { hvo }, new int[] { InterlinDocChild.TagAnalysis }, 1);
						int hvoInstanceOf = vwenv.DataAccess.get_ObjectProp(hvo, InterlinDocChild.TagAnalysis);
						if (hvoInstanceOf == 0)
						{
							vwenv.AddStringProp(m_flidStringValue, this);
						}
						else
						{
							// It's a full Twfic annotation, display the full bundle.
							vwenv.AddObjProp(InterlinDocChild.TagAnalysis, this, kfragTwficAnalysis);
						}
						//vwenv.AddObjProp(ktagTwficDefault, this, kfragTwficAnalysis);
						vwenv.CloseInnerPile();
						// revert back to the paragraph vernWs.
						SetupForTwfic(0);
					}
					break;
				case kfragNotesText: // notes structured text
					vwenv.AddObjVecItems((int)StText.StTextTags.kflidParagraphs, this, kfragNotesPara);
					break;
				case kfragNotesPara:
					vwenv.AddStringProp((int)StTxtPara.StTxtParaTags.kflidContents, this);
					break;
				case kfragComment: // hvo is a CmAnnotation, a row
					vwenv.AddStringAltMember((int)CmAnnotation.CmAnnotationTags.kflidComment, m_chart.Logic.WsLineNumber, this);
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		private void PrintIndividualColumnHeaders(int hvo, IVwEnv vwenv)
		{
			MakeTableAndRowWithStdWidths(vwenv, hvo, true);
			MakeCellsMethod.OpenRowNumberCell(vwenv); // blank cell under header for row numbers
			vwenv.CloseTableCell();
			for (int icol = 0; icol < m_chart.AllColumns.Length; icol++ )
			{
				MakeCellsMethod.OpenStandardCell(vwenv, 1, m_chart.Logic.GroupEndIndices.Contains(icol));
				vwenv.AddString(m_cache.MakeAnalysisTss(m_chart.Logic.GetColumnLabel(icol)));
				vwenv.CloseTableCell();
			}
			MakeCellsMethod.OpenStandardCell(vwenv, 1, false);  // blank cell below Notes header
			vwenv.CloseTableCell();
			vwenv.CloseTableRow();
			vwenv.CloseTable();
		}

		private void PrintColumnGroupHeaders(int hvo, IVwEnv vwenv)
		{
			MakeTableAndRowWithStdWidths(vwenv, hvo, true);
			MakeCellsMethod.OpenRowNumberCell(vwenv); // header for row numbers
			vwenv.AddString(m_cache.MakeAnalysisTss("#"));
			vwenv.CloseTableCell();
			vwenv.AddObjProp((int)DsConstChart.DsChartTags.kflidTemplate, this, kfragTemplateHeader);
			MakeCellsMethod.OpenStandardCell(vwenv, 1, false);
			vwenv.AddString(m_cache.MakeAnalysisTss(DiscourseStrings.ksNotesColumnHeader));
			vwenv.CloseTableCell();
			vwenv.CloseTableRow();
			vwenv.CloseTable();
		}

		private void DisplayMovedTextTag(int hvo, IVwEnv vwenv)
		{
			string formatTag1 = m_chart.Logic.MovedTextTag(vwenv.DataAccess.get_VecItem(hvo,
																						kflidAppliesTo, 0)) + "Mkr";
			ApplyFormatting(vwenv, formatTag1);
			vwenv.OpenSpan();
			InsertOpenBracket(vwenv, formatTag1);
			vwenv.AddStringAltMember((int)CmAnnotation.CmAnnotationTags.kflidComment,
									 m_cache.DefaultUserWs, this); // Enhance JohnT: what if that has changed??
			InsertCloseBracket(vwenv, formatTag1);
			vwenv.CloseSpan();
		}

		private void DisplayMissingMarker(IVwEnv vwenv)
		{
			vwenv.AddStringAltMember((int)CmAnnotation.CmAnnotationTags.kflidComment,
									 m_cache.DefaultUserWs, this);
		}

		private void DisplayClausePlaceholder(IVwEnv vwenv, int hvoClause)
		{
			string clauseType = GetRowStyleName(vwenv, hvoClause) + "Mkr";
			ApplyFormatting(vwenv, clauseType);
			vwenv.OpenSpan();
			InsertOpenBracket(vwenv, clauseType);
			vwenv.AddObjVec(kflidAppliesTo, this, kfragClauseLabels);
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
			VwLength tableWidth = new VwLength();
			if (m_colWidths == null)
			{
				tableWidth.nVal = 10000; // 100%
				tableWidth.unit = VwUnit.kunPercent100;
			}
			else
			{
				tableWidth.nVal = 0;
				foreach (VwLength w in m_colWidths)
					tableWidth.nVal += w.nVal;
				tableWidth.unit = VwUnit.kunPoint1000;
			}
			if (!fHeader)
				SetRowStyle(vwenv, hvo);

			VwFramePosition fpos = VwFramePosition.kvfpVsides;
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
				if (ihvoRow == vwenv.DataAccess.get_VecSize(hvoOuter, tagOuter) - 1
					|| ConstituentChartLogic.GetFeature(vwenv.DataAccess, hvo, "endPara"))
				{
					fpos = (VwFramePosition)((int)fpos | (int)VwFramePosition.kvfpBelow);
				}
			}
			// We seem to typically inherit a white background as a side effect of setting our stylesheet,
			// but borders on table rows don't show through if backcolor is set to white, because the
			// cells entirely cover the row (LT-9068). So force the back color to be transparent, and allow
			// the row border to show through the cell.
			vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)FwTextColor.kclrTransparent);
			vwenv.OpenTable(m_chart.AllColumns.Length + ConstituentChartLogic.NumberOfExtraColumns,
				tableWidth,
				1500, // borderWidth
				VwAlignment.kvaLeft, // Todo: handle RTL
				fpos,
				VwRule.kvrlNone,
				0, // cell spacing
				2000, // cell padding
				true); // selections limited to one cell.
			if (m_colWidths == null)
			{
				VwLength numColWidth = new VwLength();
				numColWidth.nVal = NumColWidth;
				numColWidth.unit = VwUnit.kunPoint1000;
				vwenv.MakeColumns(1, numColWidth);
				VwLength colWidth = new VwLength();
				colWidth.nVal = 1;
				colWidth.unit = VwUnit.kunRelative;
				int followingCols = ConstituentChartLogic.NumberOfExtraColumns -
					ConstituentChartLogic.IndexOfFirstTemplateColumn;
				vwenv.MakeColumns(m_chart.AllColumns.Length + followingCols, colWidth);
			}
			else
			{
				foreach (VwLength colWidth in m_colWidths)
				{
					vwenv.MakeColumns(1, colWidth);
				}
			}
			// Set row bottom border color and size of table body rows
			if (!fHeader)
			{
				if (ConstituentChartLogic.GetFeature(vwenv.DataAccess, hvo, "endSent"))
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

		private void DisplayChartBody(IVwEnv vwenv)
		{
			vwenv.AddLazyVecItems((int)DsConstChart.DsConstChartTags.kflidRows, this, kfragChartRow);
		}

		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			switch (frag)
			{
				case kfragBundleVec:
					int[] ccas = m_cache.GetVectorProperty(hvo, tag, false);
					for (int i = 0; i < ccas.Length; i++)
					{
					if (i < ccas.Length - 1)
					{
						// Give whatever box we make 5 points of separation from whatever follows.
						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
							(int)FwTextPropVar.ktpvMilliPoint, 5000);
					}
						vwenv.AddObj(ccas[i], this, kfragBundle);
					}
					break;
				case kfragClauseLabels: // hvo is cmIndirectAnnotation pointing at a group of rows (at least one).
					// Enhance JohnT: this assumes it is always a contiguous list.
					ISilDataAccess sda = vwenv.DataAccess;
					int chvo = sda.get_VecSize(hvo, kflidAppliesTo);
					int hvoFirst = sda.get_VecItem(hvo, kflidAppliesTo, 0);
					vwenv.AddObj(hvoFirst, this, kfragComment);
					if (chvo == 1)
						break;
					vwenv.AddString(m_cache.MakeAnalysisTss("-"));
					int hvoLast = sda.get_VecItem(hvo, kflidAppliesTo, chvo - 1);
					vwenv.AddObj(hvoLast, this, kfragComment);
					break;
				default:
					base.DisplayVec(vwenv, hvo, tag, frag);
					break;
			}
		}

		/// <summary>
		/// Makes the cells for a row using the MakeCellsMethod method object.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvoRow"></param>
		private void MakeCells(IVwEnv vwenv, int hvoRow)
		{
			new MakeCellsMethod(this, m_cache, vwenv, hvoRow).Run();
		}

		// In this chart this only gets invoked for the baseline. It is currently always black.
		override protected int LabelRGBFor(int choiceIndex)
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

		readonly int kSpeechColor = (int)ColorUtil.ConvertColorToBGR(Color.Green);
		readonly int kAnnotationColor = (int)ColorUtil.ConvertColorToBGR(Color.DarkGray);
		readonly int kSongColor = (int)ColorUtil.ConvertColorToBGR(Color.Plum);
		/// <summary>
		/// A nasty kludge, but everything green should also be underlined.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="color"></param>
		protected override void SetColor(IVwEnv vwenv, int color)
		{
			base.SetColor(vwenv, color);
			if (color == kAnnotationColor)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum,
					(int)FwUnderlineType.kuntNone);
			}
		}

		internal string GetRowStyleName(IVwEnv vwenv, int hvoRow)
		{
			if (ConstituentChartLogic.GetFeature(vwenv.DataAccess, hvoRow, ConstituentChartLogic.DepClauseFeatureName))
				return "dependent";
			else if (ConstituentChartLogic.GetFeature(vwenv.DataAccess, hvoRow, ConstituentChartLogic.SpeechClauseFeatureName))
				return "speech";
			else if (ConstituentChartLogic.GetFeature(vwenv.DataAccess, hvoRow, ConstituentChartLogic.SongClauseFeatureName))
				return "song";
			return "normal";
		}

		private void SetRowStyle(IVwEnv vwenv, int hvoRow)
		{
			ApplyFormatting(vwenv, GetRowStyleName(vwenv, hvoRow));
		}

		private bool IsDepClause(int hvo)
		{
			return m_chart.Logic.IsDepClause(hvo);
		}

		protected override void GetSegmentLevelTags(FdoCache cache)
		{
			// do nothing (we don't need tags above bundle level).
		}

		internal void InsertOpenBracket(IVwEnv vwenv, string key)
		{
			string bracket;
			if (m_brackets.TryGetValue(key, out bracket))
			{
				vwenv.AddString(m_cache.MakeAnalysisTss(bracket.Substring(0, 1)));
			}
		}
		internal void InsertCloseBracket(IVwEnv vwenv, string key)
		{

			string bracket;
			if (m_brackets.TryGetValue(key, out bracket))
			{
				vwenv.AddString(m_cache.MakeAnalysisTss(bracket.Substring(1, 1)));
			}
		}
	}

	/// <summary>
	/// Implementation of method for making cells in chart row.
	/// </summary>
	class MakeCellsMethod
	{
		IVwEnv m_vwenv;
		int m_hvoRow; // Hvo of the CmIndirectAnnotation representing a row in the chart.
		FdoCache m_cache;
		ConstChartVc m_this; // original 'this' object of the refactored method.
		ConstChartBody m_chart;
		int[] m_ccas;
		int m_hvoCurCellCol = 0; // column for which cell is currently open (initially not for any column)
		int m_iLastColForWhichCellExists = -1; // index of last column for which we have made (at least opened) a cell.
		// index of CCA to insert clause bracket before; gets reset if we find an auto-missing-marker col first.
		int m_iccaOpenClause = -1;
		// index of CCA to insert clause bracket after (unless m_icolLastAutoMissing is a later column).
		int m_iccaCloseClause = -1;
		int m_cCcaCurrentCell = 0; // number of CCAs output in current cell.
		int m_icca = 0;
		// index of last column where automatic missing markers are put.
		int m_icolLastAutoMissing = -1;

		const int kflidAppliesTo = (int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo;

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="baseObj"></param>
		/// <param name="cache"></param>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		public MakeCellsMethod(ConstChartVc baseObj, FdoCache cache, IVwEnv vwenv, int hvo)
		{
			m_this = baseObj;
			m_cache = cache;
			m_vwenv = vwenv;
			m_hvoRow = hvo;
			m_chart = baseObj.m_chart;
		}

		/// <summary>
		/// Main entry point, makes the cells.
		/// </summary>
		public void Run()
		{
			MakeRowLabelCell();
			// If the AppliesTo of the row changes, we need to regenerate.
			m_vwenv.NoteDependency(new int[] { m_hvoRow },
				new int[] { kflidAppliesTo }, 1);

			m_ccas = m_cache.GetVectorProperty(m_hvoRow, kflidAppliesTo, false);
			int icca2 = 0;
			if (ConstituentChartLogic.GetFeature(m_vwenv.DataAccess, m_hvoRow, ConstituentChartLogic.StartDepClauseGroup))
			{
				while (icca2 < m_ccas.Length && !GoesInsideClauseBrackets(m_ccas[icca2]))
					icca2++;
				if (icca2 < m_ccas.Length)
					m_iccaOpenClause = icca2;
				else
					m_iccaOpenClause = 0; // Has to go somewhere on line!
			}
			if (ConstituentChartLogic.GetFeature(m_vwenv.DataAccess, m_hvoRow, ConstituentChartLogic.EndDepClauseGroup))
			{
				icca2 = m_ccas.Length - 1;
				while (icca2 >= 0 && !GoesInsideClauseBrackets(m_ccas[icca2]))
				{
					icca2--;
				}
				if (icca2 >= 0)
					m_iccaCloseClause = icca2;
				else
					m_iccaCloseClause = m_ccas.Length - 1;

				// Find the index of the column with the CCA before the close bracket (plus 1), or if none, start at col 0.
				int icol = 0;
				if (0 <= m_iccaCloseClause && m_iccaCloseClause < m_ccas.Length)
					icol = GetIndexOfColumn(
						m_cache.GetObjProperty(m_ccas[m_iccaCloseClause], (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf)) + 1;
				// starting from there find the last column that has the auto-missing property.
				m_icolLastAutoMissing = -1;
				for (; icol < m_chart.AllColumns.Length; icol++)
					if (m_chart.Logic.ColumnHasAutoMissingMarkers(icol))
						m_icolLastAutoMissing = icol;
				// If we found a subsequent auto-missing column, disable putting the close bracket after the CCA,
				// it will go after the auto-missing-marker instead.
				if (m_icolLastAutoMissing != -1)
					m_iccaCloseClause = -1; // terminate after auto-marker.
			}

			// Main loop over CCAs in this row
			for (m_icca = 0; m_icca < m_ccas.Length; m_icca++)
			{
				int hvoCca = m_ccas[m_icca];
				int hvoColContainingCca = m_cache.GetObjProperty(hvoCca, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
				if (hvoColContainingCca == 0)
				{
					// It doesn't belong to any column! Maybe the template got edited and the column
					// was deleted? Arbitrarily assign it to the first column...logic below
					// may change to the current column if any.
					hvoColContainingCca = m_chart.AllColumns[0];
					ReportAndFixBadCca(hvoCca, hvoColContainingCca);
				}
				if (hvoColContainingCca == m_hvoCurCellCol)
				{
					// same column; just add to the already-open cell
					AddCcaToCell(hvoCca);
					continue;
				}
				int ihvoNewCol = GetIndexOfColumn(hvoColContainingCca);
				if (ihvoNewCol < m_iLastColForWhichCellExists || ihvoNewCol >= m_chart.AllColumns.Length)
				{
					// pathological case...cca is out of order or its column has been deleted.
					// Maybe the user re-ordered the columns??
					// Anyway, we'll let it go into the current cell.
					ReportAndFixBadCca(hvoCca, m_hvoCurCellCol);
					AddCcaToCell(hvoCca);
					continue;
				}

				// changed column (or started first column). Close the current cell if one is open, and figure out
				// how many cells wide the new one needs to be.
				CloseCurrentlyOpenCell();
				int ccolsAvailableUpToCurrent = ihvoNewCol - m_iLastColForWhichCellExists;
				m_hvoCurCellCol = hvoColContainingCca;
				if (MergesBefore(hvoCca))
				{
					// Make one cell covering all the columns not already occupied, up to and including the current one.
					// If in fact merging is occurring, align it in the appropriate cell.
					if (ccolsAvailableUpToCurrent > 1)
					{
						m_vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum,
							(int)FwTextAlign.ktalTrailing);
					}
					MakeDataCell(ccolsAvailableUpToCurrent);
					m_iLastColForWhichCellExists = ihvoNewCol;
				}
				else
				{
					// Not merging left, first fill in any extra, empty cells.
					MakeEmptyCells(ccolsAvailableUpToCurrent - 1);
					// We have created all cells before ihvoNewCol; need to decide how many to merge right.
					int ccolsNext = 1;
					if (MergesAfter(hvoCca))
					{
						// Determine how MANY cells it can use. Find the next CCA in a different column, if any.
						// It's column determines how many cells are empty. If it merges before, consider
						// giving it a column to merge.
						int iNextColumn = m_chart.AllColumns.Length; // by default can use all remaining columns.
						for (int iccaNextCol = m_icca + 1; iccaNextCol < m_ccas.Length; iccaNextCol++)
						{
							int hvoCcaInNextCol = m_ccas[iccaNextCol];
							int hvoColContainingNextCca = m_cache.GetObjProperty(hvoCcaInNextCol,
								(int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
							if (hvoColContainingCca != hvoColContainingNextCca)
							{
								iNextColumn = GetIndexOfColumn(hvoColContainingNextCca);
								// But, if the next column merges before, and there are at least two empty column,
								// give it one of them.
								if (iNextColumn > ihvoNewCol + 2 && MergesBefore(hvoCcaInNextCol))
									iNextColumn--; // use one for the merge before.
								break; // found the first cell in a different column, stop.
							}
						}
						ccolsNext = iNextColumn - ihvoNewCol;
					}
					MakeDataCell(ccolsNext);
					m_iLastColForWhichCellExists = ihvoNewCol + ccolsNext - 1;
				}
				m_cCcaCurrentCell = 0; // none in this cell yet.
				AddCcaToCell(hvoCca);
			}
			CloseCurrentlyOpenCell();
			// Make any leftover empty cells.
			MakeEmptyCells(m_chart.AllColumns.Length - m_iLastColForWhichCellExists - 1);
			OpenNoteCell();
			m_vwenv.AddObjProp((int)CmAnnotation.CmAnnotationTags.kflidText, m_this, ConstChartVc.kfragNotesText);
			m_vwenv.CloseTableCell();
		}

		/// <summary>
		/// Report that a CCA has been detected that has no column, or that is out of order.
		/// We will arbitrarily put it into column hvoCol.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="hvoCol"></param>
		private void ReportAndFixBadCca(int hvo, int hvoCol)
		{
			if (!m_chart.BadChart)
			{
				MessageBox.Show(DiscourseStrings.ksFoundAndFixingInvalidDataCells,
					DiscourseStrings.ksInvalidInternalConstituentChartData,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
				m_chart.BadChart = true;
			}

			// Suppress Undo handling...we may fix lots of these, it doesn't make sense for the user to
			// try to Undo it, since it would just get fixed again when we display the chart again.
			ISilDataAccess sda = m_vwenv.DataAccess;
			IActionHandler oldHandler = sda.GetActionHandler();
			sda.SetActionHandler(null);
			try
			{
				sda.SetObjProp(hvo, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf, hvoCol);
			}
			finally
			{
				sda.SetActionHandler(oldHandler);
			}
		}

		/// <summary>
		/// Answer true if the CCA should go inside the clause bracketing (if any).
		/// </summary>
		/// <param name="hvoCca"></param>
		/// <returns></returns>
		private bool GoesInsideClauseBrackets(int hvoCca)
		{
			if (ConstituentChartLogic.IsWficGroup(m_cache, hvoCca))
				return true;
			int dummy;
			if (m_chart.Logic.IsClausePlaceholder(hvoCca, out dummy))
				return false;
			if (IsListRef(m_vwenv.DataAccess, hvoCca))
				return false;
			return true; // remaining option is pre/postposed marker, which are inside.
		}

		private bool MergesAfter(int hvoCca)
		{
			return ConstituentChartLogic.GetFeature(m_vwenv.DataAccess, hvoCca, ConstituentChartLogic.mergeAfterTag);
		}

		private bool MergesBefore(int hvoCca)
		{
			return ConstituentChartLogic.GetFeature(m_vwenv.DataAccess, hvoCca, ConstituentChartLogic.mergeBeforeTag);
		}

		private void AddCcaToCell(int hvoCca)
		{
			if (m_cCcaCurrentCell != 0)
				m_vwenv.AddString(m_this.SpaceString);
			m_cCcaCurrentCell++;
			if (m_icca == m_iccaOpenClause)
			{
				m_this.InsertOpenBracket(m_vwenv, m_this.GetRowStyleName(m_vwenv, m_hvoRow));
			}
			if (IsMovedText(hvoCca))
				m_vwenv.AddObj(hvoCca, m_this, ConstChartVc.kfragCcaMoved);
			// Is its target a CmPossibility?
			else if (IsListRef(m_vwenv.DataAccess, hvoCca))
			{
				// If we're about to add our first CCA and its a CmPossibility, see if AutoMissingMarker flies.
				if (m_cCcaCurrentCell == 1 && m_chart.Logic.ColumnHasAutoMissingMarkers(m_iLastColForWhichCellExists))
				{
					InsertAutoMissingMarker(m_iLastColForWhichCellExists);
					m_cCcaCurrentCell++;
				}

				m_vwenv.AddObj(hvoCca, m_this, ConstChartVc.kfragCcaListItem);
			}
			else
				m_vwenv.AddObj(hvoCca, m_this, ConstChartVc.kfragCca);
			if (m_icca == m_iccaCloseClause)
			{
				m_this.InsertCloseBracket(m_vwenv, m_this.GetRowStyleName(m_vwenv, m_hvoRow));
			}
		}

		private int GetIndexOfColumn(int hvoCol)
		{
			int ihvoNewCol;
			for (ihvoNewCol = m_iLastColForWhichCellExists + 1; ihvoNewCol < m_chart.AllColumns.Length; ihvoNewCol++)
			{
				if (hvoCol == m_chart.AllColumns[ihvoNewCol])
					break;
			}
			return ihvoNewCol;
		}

		private void CloseCurrentlyOpenCell()
		{
			if (m_hvoCurCellCol != 0)
			{
				m_vwenv.CloseParagraph();
				m_vwenv.CloseTableCell();
			}
		}

		private void MakeRowLabelCell()
		{
			OpenRowNumberCell(m_vwenv);
			m_vwenv.AddStringAltMember((int)CmAnnotation.CmAnnotationTags.kflidComment,
				m_chart.Logic.WsLineNumber, m_this);
			m_vwenv.CloseTableCell();
		}

		static internal void OpenRowNumberCell(IVwEnv vwenv)
		{
			// Row number cell should not be editable [LT-7744].
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing,
				(int)FwTextPropVar.ktpvMilliPoint, 500);
			vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.Black));

			vwenv.OpenTableCell(1, 1);
		}

		private void MakeEmptyCells(int count)
		{
			for (int i = 0; i < count; i++)
			{
				int icol = i + m_iLastColForWhichCellExists + 1;
				OpenStandardCell(icol, 1);
				if (m_chart.Logic.ColumnHasAutoMissingMarkers(icol))
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
			if (m_iccaOpenClause == m_icca)
			{
				m_this.InsertOpenBracket(m_vwenv, m_this.GetRowStyleName(m_vwenv, m_hvoRow));
				m_iccaOpenClause = -1; // suppresses normal open and in any subsequent auto-missing cells.
			}
			m_vwenv.AddString(m_cache.MakeAnalysisTss(DiscourseStrings.ksMissingMarker));
			if (icol == m_icolLastAutoMissing)
				m_this.InsertCloseBracket(m_vwenv, m_this.GetRowStyleName(m_vwenv, m_hvoRow));
		}

		private void MakeDataCell(int ccols)
		{
			int icol = GetIndexOfColumn(m_hvoCurCellCol);
			OpenStandardCell(icol, ccols);
			m_vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvDefault, (int)TptEditable.ktptNotEditable);
			m_vwenv.OpenParagraph();
		}

		private void OpenStandardCell(int icol, int ccols)
		{
			if (m_chart.Logic.IsHighlightedCell(m_chart.Logic.IndexOfRow(m_hvoRow), icol))
			{
				// use m_vwenv.set_IntProperty to set ktptBackColor for cells where the ChOrph could be inserted
				m_vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.LightGreen));
			}
			OpenStandardCell(m_vwenv, ccols, m_chart.Logic.GroupEndIndices.Contains(icol));
		}

		private void OpenNoteCell()
		{
			// LT-8545 remaining niggle; Note shouldn't be formatted.
			// A small change to the XML config file ensures it's not underlined either.
			m_this.ApplyFormatting(m_vwenv, "normal");
			OpenStandardCell(m_vwenv, 1, false);
		}

		static internal void OpenStandardCell(IVwEnv vwenv, int ccols, bool fEndOfGroup)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing,
				(int)FwTextPropVar.ktpvMilliPoint,
				(fEndOfGroup ? 1500 : 500));
			vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(fEndOfGroup ? Color.Black : Color.LightGray));
			vwenv.OpenTableCell(1, ccols);
		}

		/// <summary>
		/// Answer true if hvoCca is a moved text item, by asking if it has the right feature.
		/// </summary>
		/// <param name="hvoCca"></param>
		/// <returns></returns>
		private bool IsMovedText(int hvoCca)
		{
			return ConstituentChartLogic.GetFeature(m_vwenv.DataAccess, hvoCca, ConstituentChartLogic.MovedTextFeatureName);
		}

		/// <summary>
		/// Return true if the CCA is a CmBaseAnnotation (which in a CCA list makes it
		/// a reference to a CmPossibility), also known as a generic marker.
		/// </summary>
		/// <param name="hvoCca"></param>
		/// <returns></returns>
		private static bool IsListRef(ISilDataAccess sda, int hvoCca)
		{
			int clsid = sda.get_IntProp(hvoCca, (int)CmObjectFields.kflidCmObject_Class);
			return clsid == CmBaseAnnotation.kclsidCmBaseAnnotation;
		}
	}
}
