using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using XCore;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using System.Xml;
using SIL.Utils;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// A constituent chart is used to organize words (and perhaps eventally somehow morphemes)
	/// into a table where rows roughtly correspond to clauses and columns to key parts of a clause.
	/// A typical chart has two pre-nuclear columns, three or four nuclear ones (SVO and perhaps indirect
	/// object) and one or two post-nuclear ones.
	///
	/// Currently the constituent chart is displayed as a tab in the interlinear window. It is created
	/// by reflection because it needs to refer to the interlinear assembly (in order to display words
	/// in interlinear mode), so the interlinear assembly can't know about this one.
	/// </summary>
	public partial class ConstituentChart : UserControl, IxCoreColleague
	{

		#region Member Variables
		InterlinRibbon m_ribbon;
		ConstChartBody m_body;
		List<Button> m_MoveHereButtons = new List<Button>(); // Buttons for moving ribbon text into a specific column
		List<Button> m_ContextMenuButtons = new List<Button>(); // Popups associated with each 'MoveHere' button
		bool m_fContextMenuButtonsEnabled;
		DsConstChart m_chart;
		ICmPossibility m_template;
		int[] m_allColumns;
		ConstituentChartLogic m_logic;
		Panel m_buttonRow;
		Panel m_bottomStuff; // m_buttonRow above m_ribbon
		//ChartHeaderView m_headerGroups;
		ChartHeaderView m_headerMainCols;
		Panel m_topStuff; // top panel has header groups, headerMainCols, and main chart
		int[] m_columnWidths; // width of each table cell in millipoints
		float m_dxpInch; // DPI when m_columnWidths was computed.
		// left of each column in pixels. First is zero. Count is one MORE than number
		// of columns, so last position is width of window (right of last column).
		int[] m_columnPositions;
		ToolTip m_toolTip; // controls the popup help items for the Constituent Chart Form

		InterAreaBookmark m_bookmark; // To keep track of where we are in the text between panes (and areas)

		FdoCache m_cache;
		System.Xml.XmlNode m_configurationParameters;

		Mediator m_mediator;
		#endregion

		/// <summary>
		/// Make one. Usually called by reflection.
		/// </summary>
		public ConstituentChart(FdoCache cache)
			: this(cache, new ConstituentChartLogic(cache))
		{

		}

		/// <summary>
		/// Make one. This variant is used in testing (to plug in a known logic class).
		/// </summary>
		internal ConstituentChart(FdoCache cache, ConstituentChartLogic logic)
		{
			m_cache = cache;
			m_logic = logic;
			this.SuspendLayout();
			m_ribbon = new InterlinRibbon(m_cache, 0);
			m_ribbon.Dock = DockStyle.Fill; // fills the 'bottom stuff'
			m_logic.Ribbon = m_ribbon as IInterlinRibbon;
			m_toolTip = new ToolTip(); // Holds tooltip help for 'Move Here' buttons.
			// Set up the delays for the ToolTip.
			m_toolTip.AutoPopDelay = 5000;
			m_toolTip.InitialDelay = 1000;
			m_toolTip.ReshowDelay = 500;
			// Force the ToolTip text to be displayed whether or not the form is active.
			m_toolTip.ShowAlways = true;

			m_bottomStuff = new Panel();
			m_bottomStuff.SuspendLayout();
			m_bottomStuff.Height = 100; // Enhance: figure based on contents or at least number of rows.
			m_bottomStuff.Dock = DockStyle.Bottom;

			m_buttonRow = new Panel();
			m_buttonRow.Height = new Button().Height; // grab the default height of a button; don't insert any yet.
			m_buttonRow.Dock = DockStyle.Top;
			m_buttonRow.Layout += new LayoutEventHandler(m_buttonRow_Layout);

			m_bottomStuff.Controls.AddRange(new Control[] { m_ribbon, m_buttonRow });
			m_fContextMenuButtonsEnabled = true;
			m_bottomStuff.ResumeLayout();

			m_body = new ConstChartBody(m_logic, this);
			m_body.Cache = m_cache;
			m_body.Dock = DockStyle.Fill;

			//m_headerGroups = new ChartHeaderView();
			m_headerMainCols = new ChartHeaderView(this);
			m_headerMainCols.Dock = DockStyle.Top;
			m_headerMainCols.Layout += new LayoutEventHandler(m_headerMainCols_Layout);
			m_headerMainCols.SizeChanged += new EventHandler(m_headerMainCols_SizeChanged);
			m_headerMainCols.View = System.Windows.Forms.View.Details;
			m_headerMainCols.Height = 22; // Seems to be right (cf BrowseViewer) but not ideal.
			m_headerMainCols.Scrollable = false;
			m_headerMainCols.AllowColumnReorder = false;
			m_headerMainCols.ColumnWidthChanged += new ColumnWidthChangedEventHandler(m_headerMainCols_ColumnWidthChanged);
			//m_headerGroups.Layout += new LayoutEventHandler(m_headerGroups_Layout);

			m_logic.Ribbon_Changed += new EventHandler(m_logic_Ribbon_Changed);

			m_topStuff = new Panel();
			m_topStuff.Dock = DockStyle.Fill;
			m_topStuff.Controls.AddRange(new Control[] { m_body, m_headerMainCols /*, m_headerGroups */});

			this.Controls.AddRange(new Control[] { m_topStuff, m_bottomStuff });

			this.ResumeLayout();
		}

		#region kflid Constants
		const int kflidRows = (int)DsConstChart.DsConstChartTags.kflidRows;
		const int kflidAppliesTo = (int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo;
		const int kflidParagraphs = (int)StText.StTextTags.kflidParagraphs;
		const int kflidAnnotationType = (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType;
		const int kflidBeginObject = (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject;
		#endregion

		protected int GetWidth(string text, Font fnt)
		{
			int width = 0;
			using (Graphics g = Graphics.FromHwnd(Handle))
			{
				width = (int)g.MeasureString(text, fnt).Width + 1;
			}
			return width;
		}

		/// <summary>
		/// Return the left of each column, starting with zero for the first, and containing
		/// one extra value for the extreme right.
		/// </summary>
		internal int[] ColumnPositions
		{
			get { return m_columnPositions; }
		}

		bool m_fInColWidthChanged = false;

		/// <summary>
		/// Enable the command and make visible when relevant
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayRepeatLastMoveLeft(object commandObject,
			ref UIItemDisplayProperties display)
		{
			if (m_logic.CanRepeatLastMove)
			{
				display.Visible = true;
				display.Enabled = true;
			}
			else
			{
				display.Visible = true;
				display.Enabled = false;
			}
			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// Enable the command and make visible when relevant
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayRepeatLastMoveRight(object commandObject,
			ref UIItemDisplayProperties display)
		{
			if (m_logic.CanRepeatLastMove)
			{
				display.Visible = true;
				display.Enabled = true;
			}
			else
			{
				display.Visible = true;
				display.Enabled = false;
			}
			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// Implements repeat move left.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public virtual bool OnRepeatLastMoveLeft(object args)
		{
			m_logic.RepeatLastMoveBack(); // Enhance JohnT(RTL)
			return true;
		}

		/// <summary>
		/// Implements repeat move right.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public virtual bool OnRepeatLastMoveRight(object args)
		{
			m_logic.RepeatLastMoveForward(); // Enhance JohnT(RTL)
			return true;
		}

		// padding (pixels) to autoresize column width to prevent wrapping
		private const int kColPadding = 4;

		internal void m_headerMainCols_ColumnAutoResize(int icolChanged)
		{
			int maxWidth = MaxUseableWidth();
			// Determine content width and try to set column to that width
			ColumnHeader changingColHdr = m_headerMainCols.Columns[icolChanged];
			int colWidth = m_body.GetColumnContentsWidth(icolChanged); // "real" column width
			if (colWidth == 0) // no content in this column, resize to header
				m_headerMainCols.AutoResizeColumn(icolChanged, ColumnHeaderAutoResizeStyle.HeaderSize);
			else
			{
				colWidth += kColPadding;
				int cLimit = maxWidth / 2; // limit resize to half of available width
				changingColHdr.Width = (colWidth > cLimit) ? cLimit : colWidth;
			}
		}

		void m_headerMainCols_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
		{
			if (m_fInColWidthChanged)
				return;
			m_fInColWidthChanged = true;
			try
			{
				int icolChanged = e.ColumnIndex;
				int ccol = m_headerMainCols.Columns.Count;
				int totalWidth = 0;
				int maxWidth = MaxUseableWidth();
				foreach (ColumnHeader ch in m_headerMainCols.Columns)
					totalWidth += ch.Width + 0;
				if (totalWidth > maxWidth)
				{
					int delta = totalWidth - maxWidth;
					int remainingCols = ccol - icolChanged - 1;
					int icolAdjust = icolChanged + 1;
					while (remainingCols > 0)
					{
						int deltaThis = delta / remainingCols;
						m_headerMainCols.Columns[icolAdjust].Width -= deltaThis;
						delta -= deltaThis;
						icolAdjust++;
						remainingCols--;
					}
				}
				if (m_columnWidths == null)
					m_columnWidths = new int[m_allColumns.Length + 1];
			}
			finally
			{
				m_fInColWidthChanged = false;
			}
			GetColumnWidths(); // Transfer from header to variables.
			PersistColumnWidths();
			// Now adjust everything else
			ComputeButtonWidths();
			m_body.SetColWidths(m_columnWidths);
		}

		void m_headerMainCols_SizeChanged(object sender, EventArgs e)
		{
			SetHeaderColWidths();
		}

		void m_headerMainCols_Layout(object sender, LayoutEventArgs e)
		{
			SetHeaderColWidths();
		}

		private void SetHeaderColWidths()
		{
			m_fInColWidthChanged = true;
			try
			{
				//GetColumnWidths();
				for (int i = 0; i < m_headerMainCols.Columns.Count; i++)
				{
					int width = m_columnPositions[i + 1] - m_columnPositions[i];
					if (m_headerMainCols.Columns[i].Width != width)
						m_headerMainCols.Columns[i].Width = width;
				}
			}
			finally
			{
				m_fInColWidthChanged = false;
			}
			ComputeButtonWidths();
			if (m_columnWidths != null)
				m_body.SetColWidths(m_columnWidths);
		}

		void m_headerGroups_Layout(object sender, LayoutEventArgs e)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		int MpToPixelX(int dxmp)
		{
			EnsureDpiX();
			return (int)(dxmp * m_dxpInch / 72000);
		}
		int PixelToMpX(int dx)
		{
			EnsureDpiX();
			return (int)(dx * 72000 / m_dxpInch);
		}

		private void EnsureDpiX()
		{
			if (m_dxpInch == 0)
			{
				using (Graphics g = m_buttonRow.CreateGraphics())
				{
					m_dxpInch = g.DpiX;
				}
			}
		}

		override protected void OnSizeChanged(EventArgs e)
		{
			if (m_mediator != null && m_columnWidths != null && m_chart != null && !HasPersistantColWidths)
			{
				SetDefaultColumnWidths();
				SetHeaderColWidths();
			}
			base.OnSizeChanged(e);
		}

		private void SetDefaultColumnWidths()
		{
			int numColWidthMp = m_body.NumColWidth;
			int numColWidth = MpToPixelX(numColWidthMp);
			m_columnWidths[0] = numColWidthMp;
			m_columnPositions[0] = 0;
			m_columnPositions[1] = numColWidth + 1;
			int maxWidth = MaxUseableWidth();

			int remainingWidth = maxWidth - numColWidth;
			// Evenly space all but the row number column.
			int remainingCols = m_allColumns.Length + ConstituentChartLogic.NumberOfExtraColumns
				- 1;
			int icol1 = 0;
			while (remainingCols > 0)
			{
				icol1++;
				int colWidth = remainingWidth / remainingCols;
				remainingWidth -= colWidth;
				remainingCols--;
				m_columnWidths[icol1] = PixelToMpX(colWidth);
				m_columnPositions[icol1 + 1] = m_columnPositions[icol1] + colWidth;
			}
		}

		private int MaxUseableWidth()
		{
			return this.Width - SystemInformation.VerticalScrollBarWidth;
		}

		/// Compute (or eventually retrieve from persistence) column widths,
		/// if not already known.
		void GetColumnWidths()
		{
			if (m_allColumns == null)
				return; // no cols, can't do anything useful.
			if (m_headerMainCols != null && m_headerMainCols.Columns.Count == m_allColumns.Length +
				ConstituentChartLogic.NumberOfExtraColumns)
			{
				// Take it from the headers if we have them set up already.
				m_columnWidths = new int[m_allColumns.Length + ConstituentChartLogic.NumberOfExtraColumns];
				m_columnPositions = new int[m_allColumns.Length + ConstituentChartLogic.NumberOfExtraColumns + 1];
				int ccol = m_headerMainCols.Columns.Count;
				for (int icol = 0; icol < ccol; icol++)
				{
					int width = m_headerMainCols.Columns[icol].Width;
					// The column seems to be really one pixel wider than the column width of the header,
					// possibly because of the boundary line width.
					m_columnPositions[icol + 1] = m_columnPositions[icol] + width + 0;
					m_columnWidths[icol] = PixelToMpX(width);
				}
			}
		}

		/// <summary>
		///  temporary layout thing until we make it align properly with the chart.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_buttonRow_Layout(object sender, LayoutEventArgs e)
		{
			ComputeButtonWidths();
		}

		private void ComputeButtonWidths()
		{
			//GetColumnWidths();
			int cPairs = m_buttonRow.Controls.Count / 2;
			if (cPairs == 0)
				return;
			int widthBtnContextMenu = SIL.FieldWorks.Resources.ResourceHelper.ButtonMenuArrowIcon.Width + 10;
			int ipair = 0;
			while (ipair < cPairs)
			{
				Control c = m_buttonRow.Controls[ipair * 2]; // main button
				c.Left = m_columnPositions[ipair + 1] + 2; // skip number column, fine tune
				c.Width = m_columnPositions[ipair + 2] - m_columnPositions[ipair + 1] - widthBtnContextMenu;
				// Redo button name in case some won't (or now will!) fit on the button
				c.Text = GetBtnName(m_headerMainCols.Columns[ipair + 1].Text, c.Width - ((c as Button).Image.Width * 2));
				Control c2 = m_buttonRow.Controls[ipair * 2 + 1]; // pull-down
				c2.Left = c.Right;
				c2.Width = widthBtnContextMenu;
				ipair++;
			}
		}
		//void m_buttonRow_Layout(object sender, LayoutEventArgs e)
		//{
		//    int numColWidth;
		//    using (Graphics g = m_buttonRow.CreateGraphics())
		//    {
		//        numColWidth = (int)(m_body.NumColWidth * g.DpiX / 72000);
		//    }
		//    int remainingWidth = m_buttonRow.Width - numColWidth;
		//    int remainingButtons = m_buttonRow.Controls.Count;
		//    if (remainingButtons == 0)
		//        return;
		//    // compute column width
		//    int colWidth = remainingWidth / m_allColumns.Length;
		//    int arrowIconWidth = 10;
		//    int widthBtnContextMenu = arrowIconWidth + 10;
		//    int widthBtnMoveHere = colWidth - widthBtnContextMenu;
		//    int ibutton = 0;
		//    int left = numColWidth;
		//    while (remainingButtons > 0)
		//    {
		//        Control c = m_buttonRow.Controls[ibutton];
		//        int widthBtn = 0;
		//        // MoveHere buttons are even, Context Menu's are odd.
		//        if (remainingButtons % 2 == 0)
		//            widthBtn = widthBtnMoveHere;
		//        else
		//            widthBtn = widthBtnContextMenu;
		//        c.Width = widthBtn;
		//        c.Left = left;
		//        left += widthBtn;
		//        remainingWidth -= widthBtn;
		//        ibutton++;
		//        remainingButtons--;
		//    }
		//}

		/// <summary>
		/// Set the root object. This is called by reflection when the InterlinMaster determines that
		/// the root object has changed.
		/// </summary>
		/// <param name="hvoStText"></param>
		public void SetRoot(int hvoStText)
		{
			int oldTemplateHvo = 0;
			if (m_template != null)
				oldTemplateHvo = m_template.Hvo;
			// does it already have a chart? If not make one.
			m_chart = null; // in case of previous call.
			if (m_cache.LangProject.DiscourseDataOA == null)
			{
				m_template = m_cache.LangProject.GetDefaultChartTemplate();
			}
			string sColName; // Holds column name while setting buttons
			if (hvoStText != 0)
			{
				FdoOwningSequence<ICmPossibility> templates = m_cache.LangProject.DiscourseDataOA.ConstChartTemplOA.PossibilitiesOS;
				if (templates.Count == 0 || templates[0].SubPossibilitiesOS.Count == 0)
				{
					MessageBox.Show(this, DiscourseStrings.ksNoColumns, DiscourseStrings.ksWarning,
						MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				if (templates.Count != 1)
				{
					MessageBox.Show(this, DiscourseStrings.ksOnlyOneTemplateAllowed, DiscourseStrings.ksWarning,
						MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				// Text should already have been parsed. However, for the ribbon to work right,
				// we need all the annotations to be real.
				MakeAllAnnotationsReal(hvoStText);
				// We need to make or set the chart before calling NextUnusedInput.
				FindAndCleanUpMyChart(hvoStText); // Sets m_chart if it finds one for hvoStText
				if (m_chart == null)
				{
					m_chart = new DsConstChart();
					m_cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_chart);
					m_chart.BasedOnRAHvo = hvoStText;
					// set a template.
					m_chart.TemplateRA = m_cache.LangProject.GetDefaultChartTemplate();
				}
				m_logic.Chart = m_chart;
				int[] unchartedAnnotations = DiscourseDbOps.NextUnusedInput(m_cache, hvoStText, 20, m_chart.Hvo);
				m_cache.VwCacheDaAccessor.CacheVecProp(hvoStText, m_ribbon.AnnotationListId, unchartedAnnotations,
					unchartedAnnotations.Length);
				// Don't need PropChanged here, ribbon will reconsruct. That is safer, since clearing
				// cache may have changed old object count.
				if (m_logic.StTextHvo != 0 && hvoStText != m_logic.StTextHvo)
				{
					EnableAllContextButtons();
					EnableAllMoveHereButtons();
					m_logic.ResetRibbonLimits();
					m_logic.CurrHighlightCells = null; // Should reset highlighting (w/PropChanged)
				}
			}
			m_ribbon.SetRoot(hvoStText);
			if (hvoStText != 0)
			{
				if (m_chart.TemplateRA == null) // LT-8700: if original template is deleted we might need this
					m_chart.TemplateRA = m_cache.LangProject.GetDefaultChartTemplate();
				m_template = m_chart.TemplateRA;
				m_logic.StTextHvo = hvoStText;
				m_allColumns = m_logic.AllColumns(m_chart.TemplateRA).ToArray();
			}
			else
			{
				// no text, so no chart
				m_logic.Chart = null;
				m_logic.StTextHvo = 0;
				m_allColumns = new int[0];
			}
			if (m_template != null && m_template.Hvo != oldTemplateHvo)
			{
				m_fInColWidthChanged = true;
				try
				{
					m_logic.MakeMainHeaderCols(m_headerMainCols);
					if (m_allColumns == null)
						return;
					int ccolsWanted = m_allColumns.Length + ConstituentChartLogic.NumberOfExtraColumns;
					m_columnWidths = new int[ccolsWanted];
					m_columnPositions = new int[ccolsWanted + 1]; // one extra for right of last column
					if (!RestoreColumnWidths())
					{
						SetDefaultColumnWidths();
					}
				}
				finally
				{
					m_fInColWidthChanged = false;
				}
			}
			if (m_chart != null)
			{
				m_body.SetRoot(m_chart.Hvo, m_allColumns);

				// Try to get the bookmark from InterlinMaster, if there are rows in the chart.
				if (m_chart.RowsRS.Count > 0)
				{
					m_bookmark = GetAncestorBookmark(this);
					m_logic.RaiseRibbonChgEvent(); // This will override bookmark if there is a ChOrph to be inserted first.
					if (!m_logic.IsChOrphActive)
					{
						if (m_bookmark != null && m_bookmark.IndexOfParagraph >= 0)
						{
							m_body.SelectAndScrollToBookmark(m_bookmark);
						}
						else if (!m_logic.IsChartComplete)
							ScrollToEndOfChart(); // Hopefully the 'otherwise' will automatically display chart at top.
					}
				} // else = no rows in chart; no selection necessary
			} else
				m_body.SetRoot(0, null);

			// If necessary adjust number of buttons
			if (m_MoveHereButtons.Count != m_allColumns.Length)
			{
				m_buttonRow.SuspendLayout();
				while (m_MoveHereButtons.Count > m_allColumns.Length)
				{
					// Remove MoveHere button
					Button lastButton = m_MoveHereButtons[m_MoveHereButtons.Count - 1];
					lastButton.Click -= new EventHandler(btnMoveHere_Click);
					m_buttonRow.Controls.Remove(lastButton);
					m_MoveHereButtons.Remove(lastButton);

					// Remove Context Menu button
					Button lastBtnContextMenu = m_ContextMenuButtons[m_ContextMenuButtons.Count - 1];
					lastBtnContextMenu.Click -= new EventHandler(btnContextMenu_Click);
					m_buttonRow.Controls.Remove(lastBtnContextMenu);
					m_ContextMenuButtons.Remove(lastBtnContextMenu);
				}
				int btnSpace; // useable pixel length on button

				while (m_MoveHereButtons.Count < m_allColumns.Length)
				{
					// Install MoveHere button
					Button newButton = new Button();
					newButton.Click += new EventHandler(btnMoveHere_Click);
					sColName = m_logic.GetColumnLabel(m_MoveHereButtons.Count);
					m_buttonRow.Controls.Add(newButton);
					// Enhance GordonM: This should deal in pixel length, not character length.
					// And column width needs to be known!
					newButton.Image = SIL.FieldWorks.Resources.ResourceHelper.MoveUpArrowIcon;
					newButton.ImageAlign = ContentAlignment.MiddleRight;

					// useable space is button width less (icon width * 2) because of centering
					btnSpace = newButton.Width - (newButton.Image.Size.Width * 2);
					newButton.TextAlign = ContentAlignment.MiddleCenter;
					newButton.Text = GetBtnName(sColName, btnSpace);

					// Set up the ToolTip text for the Button.
					m_toolTip.SetToolTip(newButton, String.Format(DiscourseStrings.ksMoveHereToolTip, sColName));

					m_MoveHereButtons.Add(newButton);

					// Install context menu button
					Button newBtnContextMenu = new Button();
					newBtnContextMenu.Click += new EventHandler(btnContextMenu_Click);
					newBtnContextMenu.Image = SIL.FieldWorks.Resources.ResourceHelper.ButtonMenuArrowIcon;
					m_buttonRow.Controls.Add(newBtnContextMenu);
					m_ContextMenuButtons.Add(newBtnContextMenu);
				}
				// To handle Refresh problem where buttons aren't set to match ChOrph state,
				// raise Ribbon changed event again here
				m_fContextMenuButtonsEnabled = true; // the newly added buttons will be enabled
				m_logic.RaiseRibbonChgEvent();
				m_buttonRow.ResumeLayout();
			}
			SetHeaderColWidths();
		}

		/// <summary>
		/// Main Chart part of preparation. Calls Chart Logic part.
		/// Scroll to ChartOrphan, highlight cell insert possibilities, disable ineligible MoveHere buttons
		/// </summary>
		/// <param name="iPara"></param>
		/// <param name="offset"></param>
		private void PrepareForChOrphInsert(int iPara, int offset)
		{
			ICmIndirectAnnotation rowPrec;
			bool[] goodCols = m_logic.PrepareForChOrphInsert(iPara, offset, out rowPrec);
			// disable ineligible MoveHere buttons
			SetEligibleButtons(goodCols);
			// disable dropdown context buttons (next to MoveHere buttons)
			DisableAllContextButtons();
			// create a ChartLocation for scrolling and scroll to first row
			m_body.SelectAndScrollToLoc(new ChartLocation(0, rowPrec), false);
		}

		/// <summary>
		/// Disable all MoveHere buttons whose column corresponds to a false entry in the parameter bool array.
		/// </summary>
		/// <param name="goodColumns"></param>
		private void SetEligibleButtons(bool[] goodColumns)
		{
			if (m_MoveHereButtons.Count > 0)
			{
				Debug.Assert(m_MoveHereButtons.Count == goodColumns.Length);
				for (int icol = 0; icol < goodColumns.Length; icol++)
					m_MoveHereButtons[icol].Enabled = goodColumns[icol];
			}
		}

		internal void ScrollToEndOfChart()
		{
			// Scroll to LastRow of chart
			ICmIndirectAnnotation row = m_logic.LastRow;
			if (row == null)
				return;
			ICmIndirectAnnotation cca = m_logic.FindLastCcaWithWfics(m_logic.CcasInRow(row));
			int icol = 0;
			if (cca != null)
				icol = m_logic.IndexOfColumnForCca(cca.Hvo);
			m_body.SelectAndScrollToLoc(new ChartLocation(icol, row), true);
		}

		private InterAreaBookmark GetAncestorBookmark(Control curLevelControl)
		{
			object myParent = curLevelControl.Parent;
			if (myParent == null)
				return null;
			if (myParent is InterlinMaster)
			{
				return (myParent as InterlinMaster).m_bookmark;
			}
			return GetAncestorBookmark(myParent as Control);
		}

		/// <summary>
		/// This public version enables call by reflection from InterlinMaster of the internal CCBody
		/// method that selects (and scrolls to) the bookmarked location in the constituent chart.
		/// </summary>
		/// <param name="bookmark"></param>
		public void SelectAndScrollToBookmark(InterAreaBookmark bookmark)
		{
			m_body.SelectAndScrollToBookmark(bookmark);
		}

		/// <summary>
		/// This public method enables call by reflection from InterlinMaster of internal Logic method
		/// that retrieves a 'bookmarkable' annotation from the Ribbon.
		/// </summary>
		/// <param name="bookmark"></param>
		public int GetUnchartedAnnForBookmark()
		{
			// Enhance GordonM: We don't actually want to save a bookmark, if the user hasn't
			// changed anything in the Chart or clicked in the Ribbon. Perhaps we need to save
			// the first uncharted annotation when coming into this tab and check here to see
			// if it has changed? (use OnVisibleChanged?)
			// Check here because this is a Control.
			return m_logic.GetUnchartedAnnForBookmark();
		}

		private void FindAndCleanUpMyChart(int hvoStText)
		{
			// Enhance JohnT: could use SQL here and follow backref.
			foreach (DsConstChart chart in m_cache.LangProject.DiscourseDataOA.ChartsOC)
			{
				if (chart.BasedOnRAHvo == hvoStText)
				{
					m_chart = chart;
					CleanupInvalidWfics();
					// Enhance GordonM: Eventually we may have allow > 1 chart per text
					// Then we'll need to take out this break.
					break;
				}
			}
		}

		/// <summary>
		/// Enhance JohnT/EricP: note that we are gradually developing mechanisms to prevent this happening.
		/// It may eventually be unnecessary. See for example CmBaseAnnotatin.CollectLinkedItemsForDeletion and ReserveAnnotations.
		/// (However, that CollectLinkedItems isn't currently called when annotations are bulk deleted,
		/// for example during parsing paragraphs, and ReserveAnnotations doesn't do all this yet.)
		/// </summary>
		private void CleanupInvalidWfics()
		{
			using (SuppressSubTasks supressActionHandler = new SuppressSubTasks(m_cache, true))
			{
				Set<int> hvosToDelete = new Set<int>();
				bool fReported = false;
				bool fDeletedCcasOrRows = false;
				// Clobber any deleted words, etc.
				int crows = m_chart.RowsRS.Count;
				int crowsOriginal = crows; // Save for PropChanged after fixing chart.
				for (int irow = 0; irow < crows; irow++ ) // not foreach here, as we may delete some as we go
				{
					int hvoRow = m_cache.GetVectorItem(m_chart.Hvo, kflidRows, irow);
					int citems = m_cache.GetVectorSize(hvoRow, kflidAppliesTo);
					// If there are already no items, it's presumably an empty row the user inserted manually
					// and plans to put something into, not a sign of a corrupted chart. So we don't want
					// to throw up the error message. Just skip it. See LT-7861.
					if (citems == 0)
						continue;
					for (int icca = 0; icca < citems; icca++) // not foreach here, as we may delete some as we go
					{
						int hvoCca = m_cache.GetVectorItem(hvoRow, kflidAppliesTo, icca);
						if (!ConstituentChartLogic.IsWficGroup(m_cache, hvoCca))
						{
							// Check to see if this is a MovedText marker and make sure Target feature is set
							if (!m_logic.CheckForUnsetMovedTextOrInvalidMrkr(hvoCca))
							{
								// This is the invalid MovedText Marker case. We need to delete the CCA.
								ReportWarningAndUpdateCount(ref fReported, hvoRow, kflidAppliesTo, ref icca, ref citems);
								hvosToDelete.Add(hvoCca);
								fDeletedCcasOrRows = true;
							}
							continue; // in any case, this isn't a Wfic group (dataCca), so skip to next.
						}
						int cwfic = m_cache.GetVectorSize(hvoCca, kflidAppliesTo);
						for (int iwfic = 0; iwfic < cwfic; iwfic++)
						{
							int hvoWfic = m_cache.GetVectorItem(hvoCca, kflidAppliesTo, iwfic);
							if (m_cache.GetObjProperty(hvoWfic, kflidBeginObject) == 0)
							{
								// It's an obsolete annotation left over after someone deleted the word from the text.
								// auto-correct by taking wfic out of cca.
								ReportWarningAndUpdateCount(ref fReported, hvoCca, kflidAppliesTo, ref iwfic, ref cwfic);
								// No need to delete the Wfic, it's saved for possible reuse.
								//hvosToDelete.Add(hvoWfic);
							}
						}
						if (cwfic == 0)
						{
							// CCA is now empty, take it out of row
							ReportWarningAndUpdateCount(ref fReported, hvoRow, kflidAppliesTo, ref icca, ref citems);
							hvosToDelete.Add(hvoCca);
							fDeletedCcasOrRows = true;
						}
					}
					if (citems == 0)
					{
						// row is now empty, take it out of chart
						ReportWarningAndUpdateCount(ref fReported, m_chart.Hvo, kflidRows, ref irow, ref crows);
						hvosToDelete.Add(hvoRow);
						fDeletedCcasOrRows = true;
					}
				}
				if (fDeletedCcasOrRows)
				{
					RemoveTargetlessPlaceholders(ref hvosToDelete);
				}
				CmObject.DeleteObjects(hvosToDelete, m_cache, false);
				// We've been bypassing propchanged, because we might already be showing the chart,
				// and PropChanged will redraw before we've fixed enough. But now, if we did anything,
				// and if we're showing it, we'd better update.
				if (fReported)
					m_cache.PropChanged(m_chart.Hvo, kflidRows, 0, crows, crowsOriginal);
			}
		}

		private void RemoveTargetlessPlaceholders(ref Set<int> hvosToDelete)
		{
			// Check for placeholders that lost their target.
			foreach (ICmIndirectAnnotation row in m_chart.RowsRS)
			{
				int citems = row.AppliesToRS.Count;
				for (int icca = 0; icca < citems; icca++) // not foreach here, as we may delete some as we go
				{
					int hvoCca = m_cache.GetVectorItem(row.Hvo, kflidAppliesTo, icca);
					if (m_cache.GetClassOfObject(hvoCca) == CmIndirectAnnotation.kclsidCmIndirectAnnotation)
					{
						int ctarget = m_cache.GetVectorSize(hvoCca, kflidAppliesTo);
						if (ctarget == 0 || hvosToDelete.Contains(
							m_cache.GetVectorItem(hvoCca, kflidAppliesTo, 0)))
						{
							// CCA is now empty, probably a defunct moved-text placeholder, get rid of it
							// It might also be a dep clause one, and there might be surviving clauses;
							// get rid of it anyway. This will leave the surviving clauses marked dependent;
							// but we warned the user the result might not be perfect...
							row.AppliesToRS.RemoveAt(icca);
							citems--;
							icca--;
							hvosToDelete.Add(hvoCca);
						}
					}
				}
			}
		}

		private void ReportWarningAndUpdateCount(ref bool fReported, int hvoObj, int kflid, ref int index, ref int count)
		{
			if (!fReported)
			{
				MessageBox.Show(this, DiscourseStrings.ksTextEditWarning, DiscourseStrings.ksWarning,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				fReported = true;
			}
			m_cache.MainCacheAccessor.Replace(hvoObj, kflid, index, index + 1, null, 0);
			index--;
			count--;
		}

		private void MakeAllAnnotationsReal(int hvoStText)
		{
			int ktagParaSegments = StTxtPara.SegmentsFlid(m_cache);
			int ktagSegmentForms = DummyVirtualHandler.InstallDummyHandler(m_cache.VwCacheDaAccessor,
				"CmBaseAnnotation", "SegmentForms", (int)CellarModuleDefns.kcptReferenceSequence).Tag;
			using (SuppressSubTasks supressActionHandler = new SuppressSubTasks(m_cache, true))
			{
				int hvoWficDefn = CmAnnotationDefn.Twfic(m_cache).Hvo;
				int cpara = m_cache.GetVectorSize(hvoStText, kflidParagraphs);
				for (int ipara = 0; ipara < cpara; ipara++)
				{
					int hvoPara = m_cache.GetVectorItem(hvoStText, kflidParagraphs, ipara);
					int cseg = m_cache.GetVectorSize(hvoPara, ktagParaSegments);
					for (int iseg = 0; iseg < cseg; iseg++)
					{
						int hvoSeg = m_cache.GetVectorItem(hvoPara, ktagParaSegments, iseg);
						int cform = m_cache.GetVectorSize(hvoSeg, ktagSegmentForms);
						for (int iform = 0; iform < cform; iform++)
						{
							int hvoAnnotation = m_cache.GetVectorItem(hvoSeg, ktagSegmentForms, iform);
							// Don't try to convert punctuation...we don't use it and converting it doesn't work anyway.
							if (m_cache.IsDummyObject(hvoAnnotation) &&
								m_cache.GetObjProperty(hvoAnnotation, kflidAnnotationType) == hvoWficDefn)
							{
								hvoAnnotation = CmObject.ConvertDummyToReal(m_cache, hvoAnnotation).Hvo;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Figure out what substring of the column name to put on the button.
		/// </summary>
		/// <param name="strName">The name of the column.</param>
		/// <param name="pxUseable">The useable space on the button in pixels.</param>
		/// <returns>Some substring of the column name (possibly the whole).</returns>
		private string GetBtnName(string strName, int pxUseable)
		{
			if (pxUseable >= GetWidth(strName, Font))
				return strName;
			if (pxUseable < GetWidth(strName.Substring(0,1), Font))
				return "";
			for (int i = 0; i < strName.Length; i++)
			{
				if (GetWidth(strName.Substring(0, i + 1), Font) > pxUseable)
					return strName.Substring(0, i);
			}
			// Shouldn't ever get here.
			return strName;
		}

		bool HasPersistantColWidths
		{
			get { return m_mediator.PropertyTable.GetStringProperty(ColWidthId(), null) != null; }
		}
		/// <summary>
		/// Restore column widths if any are persisted for this chart
		/// </summary>
		/// <returns>true if it found a valid set of widths.</returns>
		bool RestoreColumnWidths()
		{
			if (m_mediator == null)
				return false;
			string savedCols = m_mediator.PropertyTable.GetStringProperty(ColWidthId(), null);
			if (savedCols == null)
				return false;
			XmlDocument doc = new XmlDocument();
			try
			{
				doc = new XmlDocument();
				doc.LoadXml(savedCols);
			}
			catch (Exception)
			{
				// If anything is wrong with the saved data, ignore it.
				return false;
			}
			int i = 0;
			m_columnPositions[0] = 0;
			foreach (XmlNode node in doc.DocumentElement.ChildNodes	)
			{
				int width = XmlUtils.GetMandatoryIntegerAttributeValue(node, "width");
				m_columnPositions[i + 1] = m_columnPositions[i] + MpToPixelX(width);
				if (i < m_columnWidths.Length)
					m_columnWidths[i++] = width;
				else
					return false;
			}
			return i == m_columnWidths.Length; // succeed only if exact expected number.
		}

		/// <summary>
		/// Save the current column widths in the mediator's property table.
		/// </summary>
		void PersistColumnWidths()
		{
			StringBuilder colList = new StringBuilder();
			colList.Append("<root>");
			foreach (int val in m_columnWidths)
			{
				colList.Append("<col width=\"" + val + "\"/>");
			}
			colList.Append("</root>");
			m_mediator.PropertyTable.SetProperty(ColWidthId(), colList.ToString());
		}

		private string ColWidthId()
		{
			return "ConstChartColWidths" + (m_chart == null ? Guid.Empty : m_chart.Guid);
		}

		void btnMoveHere_Click(object sender, EventArgs e)
		{
			// find the index in the button row.
			Button btn = sender as Button;
			int icol = GetColumnOfButton(btn);
			m_logic.MoveToColumn(icol);
		}

		private static int GetColumnOfButton(Button btn)
		{
			// each column corresponds to a pair of MoveHereButtons and ContextMenuButtons in the buttonRow.
			int icol = btn.Parent.Controls.IndexOf(btn) / 2;
			return icol;
		}

		// Event handler to run if Ribbon changes
		void m_logic_Ribbon_Changed(object sender, EventArgs e)
		{
			int iPara, offset; // 'out' vars for NextInputIsChOrph()
			if (m_logic.NextInputIsChOrph(out iPara, out offset)) // Tests ribbon contents
			{
				m_bookmark.Reset();
				// Resetting of highlight is done in the array setter now.
				PrepareForChOrphInsert(iPara, offset); // scroll to ChOrph, highlight cell possibilities, etc.
			}
			else
			{
				if (m_logic.IsChOrphActive) // Got past the last ChOrph, now reset for normal charting
				{
					EnableAllContextButtons();
					EnableAllMoveHereButtons();
					m_logic.ResetRibbonLimits();
					m_logic.CurrHighlightCells = null; // Should reset highlighting (w/PropChanged)

					// Where should we go next? End or top of chart depending on whether chart is complete
					if (!m_logic.IsChartComplete)
						ScrollToEndOfChart();
					else
					{
						// create a ChartLocation for scrolling and scroll to first row
						m_body.SelectAndScrollToLoc(new ChartLocation(0, m_chart.RowsRS[0]), false);
					}
				}
			}
		}

		/// <summary>
		/// Shuts off all the little down-arrow buttons next to the MoveHere buttons.
		/// For use when the next input is a ChOrph.
		/// </summary>
		protected internal void DisableAllContextButtons()
		{
			if (m_fContextMenuButtonsEnabled && m_ContextMenuButtons.Count > 0)
			{
				foreach (Button btnContext in m_ContextMenuButtons)
				{
					btnContext.Enabled = false;
					btnContext.Image = null;
				}
			}
			m_fContextMenuButtonsEnabled = false;
		}

		/// <summary>
		/// Turns back on all the little down-arrow buttons next to the MoveHere buttons.
		/// For use when the next input is no longer a ChOrph.
		/// </summary>
		protected internal void EnableAllContextButtons()
		{
			if (!m_fContextMenuButtonsEnabled && m_ContextMenuButtons.Count > 0)
			{
				foreach (Button btnContext in m_ContextMenuButtons)
				{
					btnContext.Enabled = true;
					btnContext.Image = SIL.FieldWorks.Resources.ResourceHelper.ButtonMenuArrowIcon;
				}
			}
			m_fContextMenuButtonsEnabled = true;
		}

		private void EnableAllMoveHereButtons()
		{
			if (m_chart != null && m_MoveHereButtons.Count > 0)
			{
				Debug.Assert(m_MoveHereButtons.Count == m_logic.AllMyColumns.Length);
				for (int icol = 0; icol < m_logic.AllMyColumns.Length; icol++)
					m_MoveHereButtons[icol].Enabled = true;
			}
		}

		/// <summary>
		/// Handles clicking of the down arrow button beside a column button.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		void btnContextMenu_Click(object sender, EventArgs e)
		{
			// find the index in the button row.
			Button btn = sender as Button;
			int icol = GetColumnOfButton(btn);
			ContextMenuStrip menu = m_logic.MakeContextMenu(icol);
			menu.Show(btn, new Point(0, btn.Height));
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			m_ribbon.Focus(); // Enhance: decide which one should have focus.
		}

		/// <summary>
		///  If this control is a colleague, export Discourse should be available.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayExportDiscourse(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = this.m_chart != null; // in concordance we may have no chart if no text selected.
			display.Visible = true;
			return true;
		}

		/// <summary>
		/// Implement export of discourse material.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnExportDiscourse(object argument)
		{
			// guards against LT-8309, though I could not reproduce all cases.
			if (m_chart == null || m_body == null || m_logic == null)
				return false;
			using (DiscourseExportDialog dlg = new DiscourseExportDialog(m_mediator, this.m_chart.Hvo, m_body.Vc,
				m_logic.WsLineNumber))
			{
				dlg.ShowDialog(this);
			}

			return true; // we handled this
		}

		#region IxCoreColleague Members

		/// <summary>
		/// Get things that would like to receive commands. The main chart would like to receive
		/// Print and Edit commands.
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] { m_body, this };
		}

		/// <summary>
		/// Basic initialization.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			m_mediator = mediator;
			m_configurationParameters = configurationParameters;
			InterlinLineChoices lineChoices = GetLineChoices();
			m_body.Init(mediator, m_configurationParameters);
			m_body.LineChoices = lineChoices;
			m_ribbon.LineChoices = lineChoices;
		}

		/// <summary>
		/// Set/get the style sheet.
		/// </summary>
		public IVwStylesheet StyleSheet
		{
			get { return m_body.StyleSheet; }
			set
			{
				m_body.StyleSheet = value;
				IVwStylesheet oldStyles = m_ribbon.StyleSheet;
				m_ribbon.StyleSheet = value;
				if (oldStyles != value)
					m_ribbon.SelectFirstAnnotation();	// otherwise, selection disappears.
			}
		}

		/// <summary>
		/// For testing.
		/// </summary>
		internal ConstChartBody Body
		{
			get { return m_body; }
		}
		/// <summary>
		/// This means it copies settings from the edit tab (in the same view).
		/// Perversely these settings are saved with the 'doc' name.
		/// </summary>
		private static string ConfigPropName
		{
			get { return "InterlinConfig_Doc"; }
		}
		private InterlinLineChoices GetLineChoices()
		{
			InterlinLineChoices result = new InterlinLineChoices(0, m_cache.DefaultAnalWs, m_cache.LangProject);
			string persist = null;
			if (m_mediator != null)
				persist = m_mediator.PropertyTable.GetStringProperty(ConfigPropName, null, PropertyTable.SettingsGroup.LocalSettings);
			InterlinLineChoices lineChoices = null;
			if (persist != null)
			{
				lineChoices = InterlinLineChoices.Restore(persist, m_cache.LanguageWritingSystemFactoryAccessor,
					0, m_cache.DefaultAnalWs, m_cache.LangProject);
			}
			GetLineChoice(result, lineChoices, InterlinLineChoices.kflidWord);
			GetLineChoice(result, lineChoices, InterlinLineChoices.kflidWordGloss);
			return result;
		}

		/// <summary>
		/// Make sure there is SOME lineChoice for the specified flid in m_lineChoices.
		/// If lineChoices is non-null and contains one for the right flid, choose the first.
		/// </summary>
		/// <param name="lineChoices"></param>
		/// <param name="flid"></param>
		private void GetLineChoice(InterlinLineChoices dest, InterlinLineChoices source, int flid)
		{
			if (source != null)
			{
				int index = source.IndexOf(flid);
				if (index >= 0)
				{
					dest.Add(source[index]);
					return;
				}
			}
			// Last resort.
			dest.Add(flid);
		}

		#endregion
	} // End Constituent Chart class

	/// <summary>
	/// This subclass of ListView is used to make the column headers for a Constituent Chart.
	/// It's main function is to handle double-clicks on column boundaries so the chart (which is neither
	/// a ListView nor a BrowseViewer) can resize its columns.
	/// </summary>
	public class ChartHeaderView : ListView, IFWDisposable
	{
		private ConstituentChart m_chart;

		/// <summary>
		/// Create one and set the chart it belongs to.
		/// </summary>
		/// <param name="chart"></param>
		public ChartHeaderView(ConstituentChart chart)
		{
			m_chart = chart;
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="T:System.Windows.Forms.ListView"/> and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
			}
			m_chart = null;

			base.Dispose(disposing);
		}

		const int WM_NOTIFY = 0x004E;
		const int HDN_FIRST = -300;
		const int HDN_DIVIDERDBLCLICKW = (HDN_FIRST - 25);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides <see cref="M:System.Windows.Forms.Control.WndProc(System.Windows.Forms.Message@)"/>.
		/// </summary>
		/// <param name="m">The Windows <see cref="T:System.Windows.Forms.Message"/> to process.</param>
		/// ------------------------------------------------------------------------------------
		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case WM_NOTIFY:
					Win32.NMHEADER nmhdr = (Win32.NMHEADER)m.GetLParam(typeof(Win32.NMHEADER));
					switch (nmhdr.hdr.code)
					{
						case HDN_DIVIDERDBLCLICKW: // double-click on line between column headers.
							// adjust width of column to match item of greatest length.
							m_chart.m_headerMainCols_ColumnAutoResize(nmhdr.iItem);
							break;
						default:
							base.WndProc(ref m);
							break;
					}
					break;
				default:
					base.WndProc(ref m);
					break;
			}
		}

		// Todo JohnT: this could be added to the many overloads of SendMessage in Win32Wrappers.
		[DllImport("user32", EntryPoint = "SendMessage")]
		static extern IntPtr SendMessage2(IntPtr Handle, Int32 msg, IntPtr wParam, ref HDITEM lParam);

		// This possibly could also...though the current version of this overload is specified to return bool.
		[DllImport("user32")]
		static extern IntPtr SendMessage(IntPtr Handle, Int32 msg, IntPtr wParam, IntPtr lParam);

		// Todo JohnT: These could possibly move to Win32Wrappers.
		[StructLayout(LayoutKind.Sequential)]
		private struct HDITEM
		{
			public Int32 mask;
			public Int32 cxy;
			[MarshalAs(UnmanagedType.LPTStr)]
			public String pszText;
			public IntPtr hbm;
			public Int32 cchTextMax;
			public Int32 fmt;
			public Int32 lParam;
			public Int32 iImage;
			public Int32 iOrder;
		};
	}
}
