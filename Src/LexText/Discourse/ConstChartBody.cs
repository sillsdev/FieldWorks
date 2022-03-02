// Copyright (c) 2015-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// The main body of the chart, containing the actual view of the charted words.
	/// </summary>
	public class ConstChartBody : RootSite
	{
		private InterlinLineChoices m_lineChoices;
		private int m_hvoChart;
		private ICmPossibility[] m_AllColumns;
		private int m_dxNumColWidth = 25000; // millipoints
		private readonly ConstituentChart m_chart;
		private Button m_hoverButton;

		/// <summary>The fragment to display</summary>
		/// <summary>
		/// The context menu displayed for a cell.
		/// </summary>
		private ContextMenuStrip m_cellContextMenu;
		/// <summary>
		/// Flag that we've detected some bad in the chart data (such as a deleted or
		/// moved column).
		/// </summary>
		private bool m_fBadChart;
		private long m_ticksWhenContextMenuClosed;

		/// <summary>
		/// Make one.
		/// </summary>
		public ConstChartBody(ConstituentChartLogic logic, ConstituentChart chart)
			: base(null)
		{
			Logic = logic;
			Logic.RowModifiedEvent += m_logic_RowModifiedEvent;
			m_chart = chart;
			IsRightToLeft = m_chart.ChartIsRtL;
		}

		internal ConstChartVc Vc { get; private set; }

		/// <summary>
		/// Gets the logical column index from the display column index.
		/// (If the chart is for a RTL script, the 2 column orders are swapped.)
		/// </summary>
		/// <param name="icol"></param>
		/// <returns></returns>
		internal int LogicalFromDisplay(int icol)
		{
			return Logic.LogicalColumnIndexFromDisplay(icol);
		}

		void m_logic_RowModifiedEvent(object sender, RowModifiedEventArgs e)
		{
			var row = e.Row;
			if (row == null)
				return;
			if (row == Logic.LastRow)
				ScrollToEnd();
			else
			{
				var sel = MakeRowSelection(row, false);
				ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoBoth);
			}
		}

		private IVwSelection MakeRowSelection(IConstChartRow row, bool fInstall)
		{
			var rgvsli = new SelLevInfo[1];
			rgvsli[0].ihvo = row.IndexInOwner; // specifies which row.
			rgvsli[0].tag = DsConstChartTags.kflidRows;
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

			if (m_chart == null || Logic.Chart.RowsOS.Count < 1)
				return; // nothing to do (and leave the bookmark alone)

			// Gets the wordform that is closest to the bookmark in the text
			var occurrence = Logic.FindWordformAtBookmark(bookmark);
			SelectAndScrollToAnalysisOccurrence(occurrence);
		}

		/// <summary>
		/// Select and scroll to Chart location closest to an AnalysisOccurrence.
		/// Takes into account ChOrphs by finding nearest charted location.
		/// </summary>
		/// <param name="occurrence"></param>
		internal void SelectAndScrollToAnalysisOccurrence(AnalysisOccurrence occurrence)
		{
			if (occurrence == null)
			{
				Debug.Assert(occurrence != null, "Unable to find occurrence close to bookmark");
				return;
			}
			var chartLoc = Logic.FindChartLocOfWordform(occurrence);
			if (chartLoc != null && chartLoc.IsValidLocation)
			{
				SelectAndScrollToLoc(chartLoc, true);
				return;
			}
			// Otherwise, Bookmark is for an occurrence that is not yet charted.
			m_chart.ScrollToEndOfChart();
		}

		/// <summary>
		/// Selects and scrolls to the bookmarked location in the constituent chart. This version
		/// assumes the bookmarked location has been charted, since the location is passed as a parameter.
		/// </summary>
		/// <param name="chartLoc">A ChartLocation object, created by CCLogic.FindChartLocOfWordform().</param>
		/// <param name="fbookmark">true if called for a bookmark, false if called for ChOrph highlighting</param>
		internal void SelectAndScrollToLoc(ChartLocation chartLoc, bool fbookmark)
		{
			Debug.Assert(m_chart != null);
			Debug.Assert(chartLoc != null);
			Debug.Assert(chartLoc.Row != null);

			if (Height == 0)
			{
				// This doesn't work (because the root box can't be laid out properly) until we have a non-zero height.
				// So hold that thought until we do.
				m_pendingChartLoc = chartLoc;
				m_pendingChartLocIsBookmark = fbookmark;
			}
			else
			{
				m_pendingChartLoc = null;
			}

			// The following will select the row of the bookmark
			var row = chartLoc.Row;
			IVwSelection sel = MakeRowSelection(row, true);
			if (fbookmark)
				ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoTop);
			else
				ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoNearTop);
			//Update(); ScrollSelectionIntoView() does this, I believe.
		}

		private ChartLocation m_pendingChartLoc;
		private bool m_pendingChartLocIsBookmark;

		protected override void OnPaint(PaintEventArgs e)
		{
			// Surely by the time we come to paint it should be possible to scroll to the position we want??
			if (m_pendingChartLoc != null && Height != 0)
			{
				SelectAndScrollToLoc(m_pendingChartLoc, m_pendingChartLocIsBookmark);
				Invalidate();
				return;
			}
			base.OnPaint(e);
		}

		/// <summary>
		/// Width of the number column (in millipoints).
		/// </summary>
		internal int NumColWidth
		{
			get { return m_dxNumColWidth; }
		}

		internal ConstituentChartLogic Logic { get; }

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			m_hoverButton = new Button();
			var pullDown = ResourceHelper.BlueCircleDownArrowForView;
			m_hoverButton.Image = pullDown;
			m_hoverButton.Height = pullDown.Height + 4;
			m_hoverButton.Width = pullDown.Width + 4;
			m_hoverButton.FlatStyle = FlatStyle.Flat;
			m_hoverButton.ForeColor = BackColor;
			m_hoverButton.BackColor = BackColor;
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			if (Controls.Contains(m_hoverButton))
				Controls.Remove(m_hoverButton);
			base.OnMouseLeave(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			int icol;
			int irow;
			if (GetCellInfo(e, out icol, out irow))
			{
				var info = new SelLevInfo[1];
				info[0].ihvo = irow;
				info[0].tag = DsConstChartTags.kflidRows;
				info[0].cpropPrevious = 0;
				// Makes a selection near the start of the row.
				var sel = RootBox.MakeTextSelInObj(0, 1, info, 0, null, true, false, false, false, false);
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
					SetHoverButtonLocation(rcPrimary, icol);
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

		private void SetHoverButtonLocation(Rect cellRect, int columnIndex)
		{
			var horizPosition = CalculateHoverButtonHorizPosition(columnIndex, m_chart.ChartIsRtL);
			var result = new Point(horizPosition, cellRect.top);
			m_hoverButton.Location = result;
		}

		private int CalculateHoverButtonHorizPosition(int columnIndex, bool fRtl)
		{
			const int extraColumnLeft = 1;
			const int margin = 4;
			// If chart is Left to Right, we start with right border of cell and subtract button width and margin.
			// If chart is Right to Left, we start with left border of cell and add margin.
			var fudgeFactor = fRtl ? margin : -margin - m_hoverButton.Width;
			var horizPosition = m_chart.ColumnPositions[columnIndex + extraColumnLeft + (fRtl ? -1 : 1) + (m_chart.NotesColumnOnRight ? 0 : 1)] + fudgeFactor;
			return horizPosition;
		}

		public void SetColWidths(int[] widths)
		{
			var ccol = widths.Length;
			var lengths = new VwLength[ccol];
			for (var icol = 0; icol < ccol; icol++)
			{
				var len = new VwLength();
				len.nVal = widths[icol];
				len.unit = VwUnit.kunPoint1000;
				lengths[icol] = len;
			}
			// We seem to need to tweak the first width to make things line up,
			// possibly because of the left border.
			lengths[0].nVal -= 1000;
			RootBox?.SetTableColWidths(lengths, ccol);
			// TODO: fix this properly - why is m_vc null?
			Debug.Assert(Vc != null);
			Vc?.SetColWidths(lengths);
		}

		internal InterlinLineChoices LineChoices
		{
			get { return m_lineChoices; }
			set
			{
				m_lineChoices = value;

				if (Vc != null)
					Vc.LineChoices = value;
			}
		}

		public override void MakeRoot()
		{
			CheckDisposed();

			base.MakeRoot();

			Vc = new ConstChartVc(this);
			Vc.LineChoices = m_lineChoices;
			// may be needed..normally happens when the VC displays a top-level paragraph.
			//SetupRealVernWsForDisplay(m_cache.LangProject.ActualWs(LangProject.kwsVernInParagraph,
			//	hvo, (int)StText.StTextTags.kflidParagraphs));

			m_rootb.DataAccess = Cache.MainCacheAccessor;
			m_rootb.SetRootObject(m_hvoChart, Vc, ConstChartVc.kfragChart, StyleSheet);
			//m_rootb.Activate(VwSelectionState.vssOutOfFocus); // Makes selection visible even before ever got focus.
		}

		/// <summary>
		/// Change the root chart.
		/// </summary>
		/// <param name="hvoChart"></param>
		/// <param name="allColumns"></param>
		/// <param name="fRightToLeft"></param>
		public void SetRoot(int hvoChart, ICmPossibility[] allColumns, bool fRightToLeft)
		{
			if (m_hvoChart == hvoChart && m_AllColumns == allColumns)
				return;
			IsRightToLeft = fRightToLeft;
			SetRoot(hvoChart, allColumns);
		}

		/// <summary>
		/// Change the root chart.
		/// </summary>
		/// <param name="hvoChart"></param>
		/// <param name="allColumns"></param>
		public void SetRoot(int hvoChart, ICmPossibility[] allColumns)
		{
			if (m_hvoChart == hvoChart && m_AllColumns == allColumns)
				return;
			m_fBadChart = false;	// new chart, new possibilities for problems...
			m_hvoChart = hvoChart;
			m_AllColumns = allColumns;

			if (RootBox == null)
				MakeRoot();
			if (RootBox != null)
				ChangeOrMakeRoot(m_hvoChart, Vc, ConstChartVc.kfragChart, StyleSheet);
		}

		/// <summary>
		/// Change the root chart. This version takes the actual chart object.
		/// </summary>
		/// <param name="chart"></param>
		/// <param name="allColumns"></param>
		public void SetRoot(IDsConstChart chart, ICmPossibility[] allColumns)
		{
			SetRoot(chart.Hvo, allColumns);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (m_cellContextMenu != null)
					m_cellContextMenu.Dispose();
				if (Vc != null)
					Vc.Dispose();
				if (m_hoverButton != null)
				{
					if (Controls.Contains(m_hoverButton))
						Controls.Remove(m_hoverButton);
					m_hoverButton.Dispose();
				}
			}
			m_cellContextMenu = null;
			Vc = null;
			m_hoverButton = null;
			base.Dispose(disposing);
		}

		/// <summary>
		/// All the columns we're displaying.
		/// </summary>
		public ICmPossibility[] AllColumns
		{
			get { return m_AllColumns; }
		}

		protected internal bool IsRightToLeft { get; set; }

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right || m_hoverButton.Bounds.Contains(e.Location))
			{
				// Bring up the menu if the user right clicks or clicks on the menu hover button.
				int irow, icol;
				IDsConstChart chart =
					Cache.ServiceLocator.GetInstance<IDsConstChartRepository>().GetObject(m_hvoChart);
				ChartLocation cell;
				if (GetCellInfo(e, out icol, out irow))
				{
					icol = LogicalFromDisplay(icol);
					cell = new ChartLocation(chart.RowsOS[irow], icol);
					m_cellContextMenu = Logic.MakeCellContextMenu(cell);
					m_cellContextMenu.Closed += m_cellContextMenu_Closed;
					m_cellContextMenu.Show(this, e.X, e.Y);
					return; // Don't call the base method, we don't want to make a selection.
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
		/// <param name="icol">This needs to include the 'logical' column index.</param>
		/// <param name="irow"></param>
		/// <returns>true if it is a template column, or false if some other column (Notes?)</returns>
		private bool GetCellInfo(MouseEventArgs e, out int icol, out int irow)
		{
			icol = -1; // in case of premature 'return'
			irow = -1;
			if (m_hvoChart == 0 || AllColumns == null || e.Y > RootBox.Height || e.X > RootBox.Width)
				return false;
			Point pt;
			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			using (new HoldGraphics(this))
			{
				pt = PixelToView(new Point(e.X, e.Y));
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				var sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
				if (sel == null)
					return false;
				var info = new TextSelInfo(sel);
				if (info.Levels(false) < 2)
					return false;
				irow = GetIndexOfTopLevelObject(info, false);
				var chart = Cache.ServiceLocator.GetInstance<IDsConstChartRepository>().GetObject(m_hvoChart);
				if (irow < 0 || chart.RowsOS.Count <= irow)
					return false;
				icol = Logic.GetColumnFromPosition(e.X, m_chart.ColumnPositions) - 1;
				icol += (m_chart.ChartIsRtL && m_chart.NotesColumnOnRight) ? 1 :
					(!m_chart.ChartIsRtL && !m_chart.NotesColumnOnRight) ? -1 : 0;
				// return true if we clicked on a valid template column (other than notes)
				// return false if we clicked on an 'other' column, like notes or row number?
				return -1 < icol && icol < AllColumns.Length;
			}
		}

		protected override void GetPrintInfo(out int hvo, out IVwViewConstructor vc, out int frag, out IVwStylesheet ss)
		{
			base.GetPrintInfo(out hvo, out vc, out _, out ss);
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
		/// Give access to the "bad chart" flag.
		/// </summary>
		internal bool BadChart
		{
			get { return m_fBadChart; }
			set { m_fBadChart = value; }
		}

		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// ConstChartBody
			//
			this.AccessibleName = "Chart Body";
			this.Name = "ConstChartBody";
			this.ResumeLayout(false);

		}

		public ConstituentChart Chart
		{
			get { return m_chart; }
		}
	}
}
