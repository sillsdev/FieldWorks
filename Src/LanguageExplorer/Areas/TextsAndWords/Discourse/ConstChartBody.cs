// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.Resources;
using Rect = SIL.FieldWorks.Common.ViewsInterfaces.Rect;
using WaitCursor = SIL.FieldWorks.Common.FwUtils.WaitCursor;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// The main body of the chart, containing the actual view of the charted words.
	/// </summary>
	public class ConstChartBody : RootSite
	{
		private int m_hvoChart;
		private ConstChartVc m_vc;
		private ConstituentChart m_chart;
		private Button m_hoverButton;
		/// <summary>
		/// The context menu displayed for a cell.
		/// </summary>
		private ContextMenuStrip m_cellContextMenu;

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

		internal IVwViewConstructor Vc => m_vc;

		/// <summary>
		/// Right-to-Left Mark; for flipping individual characters.
		/// </summary>
		internal char RLM = '\x200F';

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
				using (var g = Graphics.FromHwnd(Handle))
				{
					// get a best estimate to determine row needing the greatest column width.
					var env = new MaxStringWidthForChartColumn(m_vc, m_styleSheet, Cache.MainCacheAccessor, m_hvoChart, g, icolChanged);
					Vc.Display(env, m_hvoChart, ConstChartVc.kfragChart);
					return env.MaxStringWidth;
				}
			}
		}

		/// <summary>
		/// Gets the logical column index from the display column index.
		/// (If the chart is for a RTL script, the 2 column orders are swapped.)
		/// </summary>
		internal int LogicalFromDisplay(int icol)
		{
			return Logic.LogicalColumnIndexFromDisplay(icol);
		}

		void m_logic_RowModifiedEvent(object sender, RowModifiedEventArgs e)
		{
			var row = e.Row;
			if (row == null)
			{
				return;
			}

			if (row == Logic.LastRow)
			{
				ScrollToEnd();
			}
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
		internal void SelectAndScrollToBookmark(InterAreaBookmark bookmark)
		{
			CheckDisposed();

			Debug.Assert(bookmark != null);
			Debug.Assert(bookmark.IndexOfParagraph >= 0);

			if (m_chart == null || Logic.Chart.RowsOS.Count < 1)
			{
				return; // nothing to do (and leave the bookmark alone)
			}

			// Gets the wordform that is closest to the bookmark in the text
			var occurrence = Logic.FindWordformAtBookmark(bookmark);
			SelectAndScrollToAnalysisOccurrence(occurrence);
		}

		/// <summary>
		/// Select and scroll to Chart location closest to an AnalysisOccurrence.
		/// Takes into account ChOrphs by finding nearest charted location.
		/// </summary>
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
			ScrollSelectionIntoView(MakeRowSelection(chartLoc.Row, true), fbookmark ? VwScrollSelOpts.kssoTop : VwScrollSelOpts.kssoNearTop);
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
		internal int NumColWidth { get; } = 25000;

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
			{
				Controls.Remove(m_hoverButton);
			}
			base.OnMouseLeave(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			ChartLocation cell;
			int irow;
			if (GetCellInfo(e, out cell, out irow))
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
						sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary, out rcSec, out fSplit, out fEndBeforeAnchor);
					}
					SetHoverButtonLocation(rcPrimary, cell.ColIndex);
					if (!Controls.Contains(m_hoverButton))
					{
						Controls.Add(m_hoverButton);
					}
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
			var horizPosition = m_chart.ColumnPositions[columnIndex + extraColumnLeft + (fRtl ? 0 : 1)] + fudgeFactor;
			return horizPosition;
		}

		public void SetColWidths(int[] widths)
		{
			var ccol = widths.Length;
			var lengths = new VwLength[ccol];
			for (var icol = 0; icol < ccol; icol++)
			{
				var len = new VwLength
				{
					nVal = widths[icol],
					unit = VwUnit.kunPoint1000
				};
				lengths[icol] = len;
			}
			// We seem to need to tweak the first width to make things line up,
			// possibly because of the left border.
			lengths[0].nVal -= 1000;
			RootBox?.SetTableColWidths(lengths, ccol);
			// TODO: fix this properly - why is m_vc null?
			m_vc?.SetColWidths(lengths);
		}

		internal InterlinLineChoices LineChoices { get; set; }

		public override void MakeRoot()
		{
			CheckDisposed();

			base.MakeRoot();

			m_vc = new ConstChartVc(this)
			{
				LineChoices = LineChoices
			};
			m_rootb.DataAccess = Cache.MainCacheAccessor;
			m_rootb.SetRootObject(m_hvoChart, m_vc, ConstChartVc.kfragChart, StyleSheet);
		}

		/// <summary>
		/// Change the root chart.
		/// </summary>
		public void SetRoot(int hvoChart, ICmPossibility[] allColumns, bool fRightToLeft)
		{
			if (m_hvoChart == hvoChart && AllColumns == allColumns)
			{
				return;
			}
			IsRightToLeft = fRightToLeft;
			SetRoot(hvoChart, allColumns);
		}

		/// <summary>
		/// Change the root chart.
		/// </summary>
		public void SetRoot(int hvoChart, ICmPossibility[] allColumns)
		{
			if (m_hvoChart == hvoChart && AllColumns == allColumns)
			{
				return;
			}
			BadChart = false;	// new chart, new possibilities for problems...
			m_hvoChart = hvoChart;
			AllColumns = allColumns;

			if (RootBox == null)
			{
				MakeRoot();
			}

			if (RootBox != null)
			{
				ChangeOrMakeRoot(m_hvoChart, Vc, ConstChartVc.kfragChart, this.StyleSheet);
			}
		}

		/// <summary>
		/// Change the root chart. This version takes the actual chart object.
		/// </summary>
		public void SetRoot(IDsConstChart chart, ICmPossibility[] allColumns)
		{
			SetRoot(chart.Hvo, allColumns);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				m_cellContextMenu?.Dispose();
				m_vc?.Dispose();
				if (m_hoverButton != null)
				{
					if (Controls.Contains(m_hoverButton))
					{
						Controls.Remove(m_hoverButton);
					}
					m_hoverButton.Dispose();
				}
			}
			m_cellContextMenu = null;
			m_vc = null;
			m_hoverButton = null;
			base.Dispose(disposing);
		}

		/// <summary>
		/// All the columns we're displaying.
		/// </summary>
		public ICmPossibility[] AllColumns { get; private set; }

		protected internal bool IsRightToLeft { get; set; }

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (DateTime.Now.Ticks - m_ticksWhenContextMenuClosed > 50000) // 5ms!
			{
				// Consider bringing up another menu only if we weren't already showing one.
				// The above time test seems to be the only way to find out whether this click closed the last one.
				ChartLocation cell;
				int irow;
				if (GetCellInfo(e, out cell, out irow))
				{
					m_cellContextMenu = Logic.MakeCellContextMenu(cell);
					m_cellContextMenu.Closed += m_cellContextMenu_Closed;
					m_cellContextMenu.Show(this, e.X, e.Y);
					return; // Don't call the base method, we don't want to make a selection.
				}
				if (cell != null && cell.IsValidLocation && cell.ColIndex >= AllColumns.Length)
				{
					// Click in Notes...make sure it has one.
					if (cell.Row.Notes == null || cell.Row.Notes.Length == 0)
					{
						NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
							{
								cell.Row.Notes = TsStringUtils.EmptyString(Cache.DefaultAnalWs);
							});
					}
				}
			}
			base.OnMouseDown(e);
		}

		private void m_cellContextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			m_ticksWhenContextMenuClosed = System.DateTime.Now.Ticks;
		}

		/// <summary>
		/// Get info about which cell the user clicked in.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="clickedCell">This needs to include the 'logical' column index.</param>
		/// <param name="irow"></param>
		/// <returns>true if it is a template column, or false if some other column (Notes?)</returns>
		private bool GetCellInfo(MouseEventArgs e, out ChartLocation clickedCell, out int irow)
		{
			clickedCell = null; // in case of premature 'return'
			irow = -1;
			if (m_hvoChart == 0 || AllColumns == null || e.Y > RootBox.Height || e.X > RootBox.Width)
			{
				return false;
			}
			using (new HoldGraphics(this))
			{
				var pt = PixelToView(new Point(e.X, e.Y));
				Rectangle rcSrcRoot;
				Rectangle rcDstRoot;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				var sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
				if (sel == null)
				{
					return false;
				}
				var info = new TextSelInfo(sel);
				if (info.Levels(false) < 2)
				{
					return false;
				}
				irow = GetIndexOfTopLevelObject(info, false);
				var chart = Cache.ServiceLocator.GetInstance<IDsConstChartRepository>().GetObject(m_hvoChart);
				if (irow < 0 || chart.RowsOS.Count <= irow)
				{
					return false;
				}
				var icol = Logic.GetColumnFromPosition(e.X, m_chart.ColumnPositions) - 1;
				if (-1 < icol && icol < AllColumns.Length && e.Clicks > 0)
				{
					icol = LogicalFromDisplay(icol); // if this is just a mouse move, use 'display' column
				}
				clickedCell = new ChartLocation(chart.RowsOS[irow], icol);
				// return true if we clicked on a valid template column (other than notes)
				// return false if we clicked on an 'other' column, like notes or row number?
				return -1 < icol && icol < AllColumns.Length;
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
		protected override void AdjustPrintDialog(PrintDialog dlg)
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
		internal bool BadChart { get; set; }

		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// ConstChartBody
			//
			this.AccessibleName = "MainChartBody";
			this.Name = "ConstChartBody";
			this.ResumeLayout(false);

		}
	}
}