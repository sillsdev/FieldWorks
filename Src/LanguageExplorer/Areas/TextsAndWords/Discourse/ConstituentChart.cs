// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Xml;
using SIL.Utils;
using SIL.FieldWorks.IText;
using System.Diagnostics.CodeAnalysis;
using SIL.CoreImpl;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
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
	/// <remarks>
	/// This class is created by Reflection by ITextDll.dll's  SIL.FieldWorks.IText.InterlinMaster.
	/// </remarks>
	public partial class ConstituentChart : InterlinDocChart, IInterlinearTabControl, IHandleBookmark, IFlexComponent, IStyleSheet
	{
		#region Member Variables
		private InterlinRibbon m_ribbon;
		private ConstChartBody m_body;
		private List<Button> m_MoveHereButtons = new List<Button>();
		// Buttons for moving ribbon text into a specific column
		private List<Button> m_ContextMenuButtons = new List<Button>();
		// Popups associated with each 'MoveHere' button
		private bool m_fContextMenuButtonsEnabled;
		private IDsConstChart m_chart;
		private ICmPossibility m_template;
		private ICmPossibility[] m_allColumns;
		private ConstituentChartLogic m_logic;
		private Panel m_buttonRow;
		private Panel m_bottomStuff;
		// m_buttonRow above m_ribbon
		private ChartHeaderView m_headerMainCols;
		private Panel m_topStuff;
		// top panel has header groups, headerMainCols, and main chart
		private int[] m_columnWidths;
		// width of each table cell in millipoints
		private float m_dxpInch;
		// DPI when m_columnWidths was computed.
		// left of each column in pixels. First is zero. Count is one MORE than number
		// of columns, so last position is width of window (right of last column).
		private int[] m_columnPositions;
		private ToolTip m_toolTip;
		// controls the popup help items for the Constituent Chart Form
		private InterAreaBookmark m_bookmark;
		// To keep track of where we are in the text between panes (and areas)
		internal FdoCache m_cache;
		private IFdoServiceLocator m_serviceLocator;
		private XmlNode m_configurationParameters;
		#endregion

		/// <summary>
		/// Make one. Usually called by reflection.
		/// </summary>
		public ConstituentChart(FdoCache cache) : this(cache, new ConstituentChartLogic(cache))
		{
		}

		/// <summary>
		/// Make one. This variant is used in testing (to plug in a known logic class).
		/// </summary>
		internal ConstituentChart(FdoCache cache, ConstituentChartLogic logic)
		{
			m_cache = cache;
			m_serviceLocator = m_cache.ServiceLocator;
			m_logic = logic;

			BuildUIComponents();
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			FlexComponentCheckingService.CheckInitializationValues(propertyTable, publisher, subscriber, PropertyTable, Publisher, Subscriber);

			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;

			m_logic.Init(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"));
			var lineChoices = GetLineChoices();
			m_body.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
			m_body.LineChoices = lineChoices;
			m_ribbon.LineChoices = lineChoices;
		}

		#endregion

		private void BuildUIComponents()
		{
			SuspendLayout();

			BuildBottomStuffUI();
			BuildTopStuffUI();
			Controls.AddRange(new Control[] { m_topStuff, m_bottomStuff });

			Dock = DockStyle.Fill;

			AccessibleNameCreator.AddNames(this);

			ResumeLayout();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			// We don't want to know about column width changes until after we're initialized and have restored original widths.
			m_headerMainCols.ColumnWidthChanged += m_headerMainCols_ColumnWidthChanged;
			m_headerMainCols.ColumnWidthChanging += m_headerMainCols_ColumnWidthChanging;
		}

		private void BuildTopStuffUI()
		{
			m_body = new ConstChartBody(m_logic, this) { Cache = m_cache, Dock = DockStyle.Fill };

			// Seems to be right (cf BrowseViewer) but not ideal.
			m_headerMainCols = new ChartHeaderView(this) { Dock = DockStyle.Top,
				View = View.Details, Height = 22, Scrollable = false,
				AllowColumnReorder = false };
			m_headerMainCols.Layout += m_headerMainCols_Layout;
			m_headerMainCols.SizeChanged += m_headerMainCols_SizeChanged;

			m_topStuff = new Panel { Dock = DockStyle.Fill };
			m_topStuff.Controls.AddRange(new Control[] { m_body, m_headerMainCols });
		}

		private void BuildBottomStuffUI()
		{
			// fills the 'bottom stuff'
			m_ribbon = new InterlinRibbon(m_cache, 0) { Dock = DockStyle.Fill };
			m_logic.Ribbon = m_ribbon;
			m_logic.Ribbon_Changed += m_logic_Ribbon_Changed;

			// Holds tooltip help for 'Move Here' buttons.
			// Set up the delays for the ToolTip.
			// Force the ToolTip text to be displayed whether or not the form is active.
			m_toolTip = new ToolTip { AutoPopDelay = 5000, InitialDelay = 1000, ReshowDelay = 500, ShowAlways = true };

			m_bottomStuff = new Panel { Height = 100, Dock = DockStyle.Bottom };
			m_bottomStuff.SuspendLayout();

			m_buttonRow = new Panel { Height = new Button().Height, Dock = DockStyle.Top, BackColor = Color.FromKnownColor(KnownColor.ControlLight) };
			m_fContextMenuButtonsEnabled = true;
			m_buttonRow.Layout += m_buttonRow_Layout;

			m_bottomStuff.Controls.AddRange(new Control[] { m_ribbon, m_buttonRow });
			m_bottomStuff.ResumeLayout();
		}

		FdoCache IInterlinearTabControl.Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		private const int kmaxWordforms = 20;

		protected int GetWidth(string text, Font fnt)
		{
			int width;
			using (var g = Graphics.FromHwnd(Handle))
			{
				width = (int)g.MeasureString(text, fnt).Width + 1;
			}
			return width;
		}

		/// <summary>
		/// Return the left of each column, starting with zero for the first, and containing
		/// one extra value for the extreme right.
		/// N.B. This is a display thing, so RTL script will make it logically backwards from LTR script.
		/// </summary>
		internal int[] ColumnPositions
		{
			get { return m_columnPositions; }
		}

		bool m_fInColWidthChanged = false;

#if RANDYTODO
		/// <summary>
		/// Enable the command and make visible when relevant
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayRepeatLastMoveLeft(object commandObject, ref UIItemDisplayProperties display)
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
			return true;
			//we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// Enable the command and make visible when relevant
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayRepeatLastMoveRight(object commandObject, ref UIItemDisplayProperties display)
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
			return true;
			//we handled this, no need to ask anyone else.
		}
#endif

		/// <summary>
		/// Implements repeat move left.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public virtual bool OnRepeatLastMoveLeft(object args)
		{
			if (ChartIsRtL)
				m_logic.RepeatLastMoveForward();
			else
				m_logic.RepeatLastMoveBack();
			return true;
		}

		/// <summary>
		/// Implements repeat move right.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public virtual bool OnRepeatLastMoveRight(object args)
		{
			if (ChartIsRtL)
				m_logic.RepeatLastMoveBack();
			else
				m_logic.RepeatLastMoveForward();
			return true;
		}

		// padding (pixels) to autoresize column width to prevent wrapping
		private const int kColPadding = 4;

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "changingColHdr is a reference")]
		internal void m_headerMainCols_ColumnAutoResize(int icolChanged)
		{
			var maxWidth = MaxUseableWidth();
			// Determine content width and try to set column to that width
			var changingColHdr = m_headerMainCols.Columns[icolChanged];
			int colWidth = m_body.GetColumnContentsWidth(icolChanged);
			// "real" column width
			if (colWidth == 0)
				// no content in this column, resize to header
				m_headerMainCols.AutoResizeColumn(icolChanged, ColumnHeaderAutoResizeStyle.HeaderSize);
			else
			{
				colWidth += kColPadding;
				int cLimit = maxWidth / 2;
				// limit resize to half of available width
				changingColHdr.Width = (colWidth > cLimit) ? cLimit : colWidth;
			}
		}

		/// <summary>
		/// Whether or not Layout events for m_headerMainCols should be ignored. See FWNX-945.
		/// </summary>
		bool m_fIgnoreLayout;

		void m_headerMainCols_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
		{
			m_fIgnoreLayout = true;
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
			GetColumnWidths();
			// Transfer from header to variables.
			PersistColumnWidths();
			// Now adjust everything else
			ComputeButtonWidths();
			m_body.SetColWidths(m_columnWidths);
			m_fIgnoreLayout = false;
		}

		void m_headerMainCols_SizeChanged(object sender, EventArgs e)
		{
			SetHeaderColAndButtonWidths();
		}

		void m_headerMainCols_Layout(object sender, LayoutEventArgs e)
		{
			// Unlike .NET, Mono fires Layout during column resizing. Ignore it. FWNX-945.
			if (m_fIgnoreLayout)
				return;

			SetHeaderColAndButtonWidths();
		}

		/// <summary/>
		protected virtual void SetHeaderColAndButtonWidths()
		{
			if (m_columnPositions != null)
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
			}
			ComputeButtonWidths();
			if (m_columnWidths != null)
				m_body.SetColWidths(m_columnWidths);
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
			if (m_dxpInch != 0)
				return;

			using (var g = m_buttonRow.CreateGraphics())
			{
				m_dxpInch = g.DpiX;
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			if (m_columnWidths != null && m_chart != null && !HasPersistantColWidths)
			{
				SetDefaultColumnWidths();
				SetHeaderColAndButtonWidths();
			}
			base.OnSizeChanged(e);
		}

		private void SetDefaultColumnWidths()
		{
			if (ChartIsRtL)
			{
				SetDefaultColumnWidthsRtL();
				return;
			}
			int numColWidthMp = m_body.NumColWidth;
			int numColWidth = MpToPixelX(numColWidthMp);
			m_columnWidths[0] = numColWidthMp;
			m_columnPositions[0] = 0;
			m_columnPositions[1] = numColWidth + 1;
			int maxWidth = MaxUseableWidth();

			int remainingWidth = maxWidth - numColWidth;
			// Evenly space all but the row number column.
			int remainingCols = m_allColumns.Length + ConstituentChartLogic.NumberOfExtraColumns - 1;
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

		private void SetDefaultColumnWidthsRtL()
		{
			// Same as SetDefaultColumnWidths(), but for Right to Left scripts
			var numColWidthMp = m_body.NumColWidth;
			var numColWidth = MpToPixelX(numColWidthMp);
			m_columnPositions[0] = 0;
			var maxWidth = MaxUseableWidth();

			var remainingWidth = maxWidth - numColWidth;
			var totalColumns = m_allColumns.Length + ConstituentChartLogic.NumberOfExtraColumns;
			// Evenly space all but the row number column.
			var remainingCols = totalColumns - 1;
			var icol1 = -1;
			while (remainingCols > 0)
			{
				icol1++;
				int colWidth = remainingWidth / remainingCols;
				remainingWidth -= colWidth;
				remainingCols--;
				m_columnWidths[icol1] = PixelToMpX(colWidth);
				m_columnPositions[icol1 + 1] = m_columnPositions[icol1] + colWidth;
			}
			// Set row number column width
			icol1++;
			m_columnWidths[icol1] = numColWidthMp;
			m_columnPositions[icol1 + 1] = m_columnPositions[icol1] + numColWidth;
		}

		private int MaxUseableWidth()
		{
			var maxUsableWidth = Width;
			if (VerticalScroll.Visible)
				maxUsableWidth -= SystemInformation.VerticalScrollBarWidth;
			return maxUsableWidth;
		}

		/// Compute (or eventually retrieve from persistence) column widths,
		/// if not already known.
		void GetColumnWidths()
		{
			if (m_allColumns == null)
				return; // no cols, can't do anything useful.

			if (m_headerMainCols != null && m_headerMainCols.Columns.Count == m_allColumns.Length + ConstituentChartLogic.NumberOfExtraColumns)
			{
				// Take it from the headers if we have them set up already.
				m_columnWidths = new int[m_allColumns.Length + ConstituentChartLogic.NumberOfExtraColumns];
				m_columnPositions = new int[m_allColumns.Length + ConstituentChartLogic.NumberOfExtraColumns + 1];
				var ccol = m_headerMainCols.Columns.Count;
				for (var icol = 0; icol < ccol; icol++)
				{
					var width = m_headerMainCols.Columns[icol].Width;
					// The column seems to be really one pixel wider than the column width of the header,
					// possibly because of the boundary line width.
					m_columnPositions[icol + 1] = m_columnPositions[icol] + width + 0;
					m_columnWidths[icol] = PixelToMpX(width);
				}
			}
		}

		/// <summary>
		/// Temporary layout thing until we make it align properly with the chart.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void m_buttonRow_Layout(object sender, LayoutEventArgs e)
		{
			ComputeButtonWidths();
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "c and c2 are references")]
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
				Control c = m_buttonRow.Controls[ipair * 2];
				// main button
				c.Left = m_columnPositions[ipair + 1] + 2;
				// skip number column, fine tune
				c.Width = m_columnPositions[ipair + 2] - m_columnPositions[ipair + 1] - widthBtnContextMenu;
				// Redo button name in case some won't (or now will!) fit on the button
				c.Text = GetBtnName(m_headerMainCols.Columns[ipair + 1].Text, c.Width - ((c as Button).Image.Width * 2));
				Control c2 = m_buttonRow.Controls[ipair * 2 + 1];
				// pull-down
				c2.Left = c.Right;
				c2.Width = widthBtnContextMenu;
				ipair++;
			}
		}

		private int m_hvoRoot;

		protected internal IStText RootStText
		{
			get;
			set;
		}

		protected internal bool ChartIsRtL
		{
			get
			{
				if (RootStText == null || !RootStText.IsValidObject)
					return false;
				var defWs = m_cache.ServiceLocator.WritingSystemManager.Get(RootStText.MainWritingSystem);
				return defWs.RightToLeftScript;
			}
		}

		/// <summary>
		/// Set the root object.
		/// </summary>
		public void SetRoot(int hvo)
		{
			int oldTemplateHvo = 0;
			if (m_template != null)
				oldTemplateHvo = m_template.Hvo;
			// does it already have a chart? If not make one.
			m_chart = null;
			// in case of previous call.
			if (m_cache.LangProject.DiscourseDataOA == null)
			{
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () => { m_template = m_cache.LangProject.GetDefaultChartTemplate(); });
			}
			m_hvoRoot = hvo;
			if (m_hvoRoot == 0)
				RootStText = null;
			else
				RootStText = (IStText)m_cache.ServiceLocator.ObjectRepository.GetObject(hvo);
			if (m_hvoRoot > 0)
			{
				DetectAndReportTemplateProblem();

				// Make sure text is parsed!
				if (InterlinMaster.HasParagraphNeedingParse(RootStText))
				{
					NonUndoableUnitOfWorkHelper.Do(
						RootStText.Cache.ActionHandlerAccessor,
						() => InterlinMaster.LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(RootStText, false));
				}

				// We need to make or set the chart before calling NextUnusedInput.
				FindAndCleanUpMyChart(m_hvoRoot);
				// Sets m_chart if it finds one for hvoStText
				if (m_chart == null)
				{
					CreateChartInNonUndoableUOW();
				}
				m_logic.Chart = m_chart;
				var unchartedWordforms = m_logic.NextUnchartedInput(RootStText, kmaxWordforms).ToList();
				m_ribbon.CacheRibbonItems(unchartedWordforms);
				// Don't need PropChanged here, CacheRibbonItems handles it.
				if (m_logic.StTextHvo != 0 && m_hvoRoot != m_logic.StTextHvo)
				{
					EnableAllContextButtons();
					EnableAllMoveHereButtons();
					m_logic.ResetRibbonLimits();
					m_logic.CurrHighlightCells = null;
					// Should reset highlighting (w/PropChanged)
				}
				// Tell the ribbon whether it needs to display and select words Right to Left or not
				m_ribbon.SetRoot(m_hvoRoot);
				if (m_chart.TemplateRA == null)
					// LT-8700: if original template is deleted we might need this
					m_chart.TemplateRA = m_cache.LangProject.GetDefaultChartTemplate();
				m_template = m_chart.TemplateRA;
				m_logic.StTextHvo = m_hvoRoot;
				m_allColumns = m_logic.AllColumns(m_chart.TemplateRA).ToArray();
			}
			else
			{
				// no text, so no chart
				m_ribbon.SetRoot(0);
				m_logic.Chart = null;
				m_logic.StTextHvo = 0;
				m_allColumns = new ICmPossibility[0];
			}
			if (m_template != null && m_template.Hvo != oldTemplateHvo)
			{
				m_fInColWidthChanged = true;
				try
				{
					m_logic.MakeMainHeaderCols(m_headerMainCols);
					if (m_allColumns == new ICmPossibility[0])
						return;
					int ccolsWanted = m_allColumns.Length + ConstituentChartLogic.NumberOfExtraColumns;
					m_columnWidths = new int[ccolsWanted];
					m_columnPositions = new int[ccolsWanted + 1];
					// one extra for after the last column
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
				m_body.SetRoot(m_chart.Hvo, m_allColumns, ChartIsRtL);

				GetAndScrollToBookmark();
			}

			else
				m_body.SetRoot(0, null, false);

			// If necessary adjust number of buttons
			if (m_MoveHereButtons.Count != m_allColumns.Length && hvo > 0)
			{
				SetupMoveHereButtonsToMatchTemplate();
			}
			SetHeaderColAndButtonWidths();
		}

		/// <summary>
		/// Try to get the bookmark from InterlinMaster, if there are rows in the chart.
		/// </summary>
		private void GetAndScrollToBookmark()
		{
			if (m_chart.RowsOS.Count <= 0)
			{
				// Reset bookmark to prevent LT-12666
				if (m_bookmark != null)
					m_bookmark.Reset(m_chart.BasedOnRA.IndexInOwner);
				return;
			}
			// no rows in chart; no selection necessary
			m_bookmark = GetAncestorBookmark(this, m_chart.BasedOnRA);
			m_logic.RaiseRibbonChgEvent();
			// This will override bookmark if there is a ChOrph to be inserted first.
			if (m_logic.IsChOrphActive)
				return;
			if (m_bookmark != null && m_bookmark.IndexOfParagraph >= 0)
				m_body.SelectAndScrollToBookmark(m_bookmark);
			else if (!m_logic.IsChartComplete)
				ScrollToEndOfChart();
			// Hopefully the 'otherwise' will automatically display chart at top.
		}

		/// <summary>
		/// Sets up Move Here buttons and also determines ChOrph status by
		/// raising Ribbon Changed event.
		/// </summary>
		private void SetupMoveHereButtonsToMatchTemplate()
		{
			m_buttonRow.SuspendLayout();
			while (m_MoveHereButtons.Count > m_allColumns.Length)
			{
				// Remove MoveHere button
				var lastButton = m_MoveHereButtons[m_MoveHereButtons.Count - 1];
				lastButton.Click -= new EventHandler(btnMoveHere_Click);
				m_buttonRow.Controls.Remove(lastButton);
				m_MoveHereButtons.Remove(lastButton);

				// Remove Context Menu button
				var lastBtnContextMenu = m_ContextMenuButtons[m_ContextMenuButtons.Count - 1];
				lastBtnContextMenu.Click -= new EventHandler(btnContextMenu_Click);
				m_buttonRow.Controls.Remove(lastBtnContextMenu);
				m_ContextMenuButtons.Remove(lastBtnContextMenu);
			}

			while (m_MoveHereButtons.Count < m_allColumns.Length)
			{
				// Install MoveHere button
				var newButton = new Button();
				newButton.Click += new EventHandler(btnMoveHere_Click);
				var sColName = m_logic.GetColumnLabel(m_MoveHereButtons.Count);
				// Holds column name while setting buttons
				m_buttonRow.Controls.Add(newButton);
				// Enhance GordonM: This should deal in pixel length, not character length.
				// And column width needs to be known!
				newButton.Image = SIL.FieldWorks.Resources.ResourceHelper.MoveUpArrowIcon;
				newButton.ImageAlign = ContentAlignment.MiddleRight;

				// useable space is button width less (icon width * 2) because of centering
				var btnSpace = newButton.Width - (newButton.Image.Size.Width * 2);
				// useable pixel length on button
				newButton.TextAlign = ContentAlignment.MiddleCenter;
				newButton.Text = GetBtnName(sColName, btnSpace);

				// Set up the ToolTip text for the Button.
				m_toolTip.SetToolTip(newButton, String.Format(DiscourseStrings.ksMoveHereToolTip, sColName));

				m_MoveHereButtons.Add(newButton);

				// Install context menu button
				var newBtnContextMenu = new Button();
				newBtnContextMenu.Click += new EventHandler(btnContextMenu_Click);
				newBtnContextMenu.Image = SIL.FieldWorks.Resources.ResourceHelper.ButtonMenuArrowIcon;
				m_buttonRow.Controls.Add(newBtnContextMenu);
				m_ContextMenuButtons.Add(newBtnContextMenu);
			}
			// To handle Refresh problem where buttons aren't set to match ChOrph state,
			// raise Ribbon changed event again here
			m_fContextMenuButtonsEnabled = true;
			// the newly added buttons will be enabled
			m_logic.RaiseRibbonChgEvent();
			m_buttonRow.ResumeLayout();
		}

		private void CreateChartInNonUndoableUOW()
		{
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () => { m_chart = m_serviceLocator.GetInstance<IDsConstChartFactory>().Create(m_cache.LangProject.DiscourseDataOA, RootStText, m_cache.LangProject.GetDefaultChartTemplate()); });
		}

		private void DetectAndReportTemplateProblem()
		{
			var templates = m_cache.LangProject.DiscourseDataOA.ConstChartTemplOA.PossibilitiesOS;
			if (templates.Count == 0 || templates[0].SubPossibilitiesOS.Count == 0)
			{
				MessageBox.Show(this, DiscourseStrings.ksNoColumns, DiscourseStrings.ksWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			if (templates.Count != 1)
			{
				MessageBox.Show(this, DiscourseStrings.ksOnlyOneTemplateAllowed, DiscourseStrings.ksWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		/// <summary>
		/// Main Chart part of preparation. Calls Chart Logic part.
		/// Scroll to ChartOrphan, highlight cell insert possibilities, disable ineligible MoveHere buttons
		/// </summary>
		/// <param name="iPara"></param>
		/// <param name="offset"></param>
		private void PrepareForChOrphInsert(int iPara, int offset)
		{
			IConstChartRow rowPrec;
			var goodCols = m_logic.PrepareForChOrphInsert(iPara, offset, out rowPrec);
			// disable ineligible MoveHere buttons
			SetEligibleButtons(goodCols);
			// disable dropdown context buttons (next to MoveHere buttons)
			DisableAllContextButtons();
			// create a ChartLocation for scrolling and scroll to first row
			m_body.SelectAndScrollToLoc(new ChartLocation(rowPrec, 0), false);
			bool fExactMatch;
			var occurrenceToMark = SegmentServices.FindNearestAnalysis(GetTextParagraphByIndex(iPara),
				offset, offset, out fExactMatch);
			m_bookmark.Save(occurrenceToMark, false, m_bookmark.TextIndex); // bookmark this location, but don't persist.
		}

		private IStTxtPara GetTextParagraphByIndex(int iPara)
		{
			return m_chart.BasedOnRA[iPara];
		}

		/// <summary>
		/// Disable all MoveHere buttons whose column corresponds to a false entry in the parameter bool array.
		/// </summary>
		/// <param name="goodColumns"></param>
		private void SetEligibleButtons(bool[] goodColumns)
		{
			if (m_MoveHereButtons.Count <= 0)
				return;
			Debug.Assert(m_MoveHereButtons.Count == goodColumns.Length);
			for (var icol = 0; icol < goodColumns.Length; icol++)
				m_MoveHereButtons[icol].Enabled = goodColumns[icol];
		}

		internal void ScrollToEndOfChart()
		{
			// Scroll to LastRow of chart
			var row = m_logic.LastRow;
			if (row == null)
				return;
			var icol = 0;
			//var wordGroup = ConstituentChartLogic.FindLastWordGroup(ConstituentChartLogic.CellPartsInRow(row));
			//if (wordGroup != null)
			//    icol = m_logic.IndexOfColumnForCellPart(wordGroup.Hvo);
			m_body.SelectAndScrollToLoc(new ChartLocation(row, icol), true);
		}

		private static InterAreaBookmark GetAncestorBookmark(Control curLevelControl, IStText basedOnRa)
		{
			object myParent = curLevelControl.Parent;
			if (myParent == null)
				return null;
			if (myParent is InterlinMaster)
			{
				string tool = (myParent as InterlinMaster).CurrentTool;
				return InterlinMaster.m_bookmarks[new Tuple<string, Guid>(tool, basedOnRa.Guid)];
			}
			return GetAncestorBookmark(myParent as Control, basedOnRa);
		}

		public void SelectOccurrence(AnalysisOccurrence point)
		{
			Body.SelectAndScrollToAnalysisOccurrence(point);
		}

		/// <summary>
		/// This public version enables call by reflection from InterlinMaster of the internal CCBody
		/// method that selects (and scrolls to) the bookmarked location in the constituent chart.
		/// </summary>
		/// <param name="bookmark"></param>
		public void SelectBookmark(IStTextBookmark bookmark)
		{
			m_body.SelectAndScrollToBookmark(bookmark as InterAreaBookmark);
		}

		/// <summary>
		/// This public method enables call by reflection from InterlinMaster of internal Logic method
		/// that retrieves a 'bookmarkable' Wordform from the Ribbon.
		/// </summary>
		public AnalysisOccurrence GetUnchartedWordForBookmark()
		{
			// Enhance GordonM: We don't actually want to save a bookmark, if the user hasn't
			// changed anything in the Chart or clicked in the Ribbon. Perhaps we need to save
			// the first uncharted word when coming into this tab and check here to see
			// if it has changed? (use OnVisibleChanged?)
			// Check here because this is a Control.
			return m_logic.GetUnchartedWordForBookmark();
		}

		private void FindAndCleanUpMyChart(int hvoStText)
		{
			foreach (var chart in m_cache.LangProject.DiscourseDataOA.ChartsOC.Cast<IDsConstChart>().Where(chart => chart.BasedOnRA != null && chart.BasedOnRA.Hvo == hvoStText))
			{
				m_chart = chart;
				m_logic.Chart = m_chart;
				m_logic.CleanupInvalidChartCells();
				// Enhance GordonM: Eventually we may have to allow > 1 chart per text
				// Then we'll need to take out this break.
				break;
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
			if (pxUseable < GetWidth(strName.Substring(0, 1), Font))
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
			get { return PropertyTable.GetValue<string>(ColWidthId()) != null; }
		}

		/// <summary>
		/// Restore column widths if any are persisted for this chart
		/// </summary>
		/// <returns>true if it found a valid set of widths.</returns>
		bool RestoreColumnWidths()
		{
			if (PropertyTable == null)
				return false;
			string savedCols = PropertyTable.GetValue<string>(ColWidthId());
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

			if (doc.DocumentElement == null || doc.DocumentElement.ChildNodes.Count != m_columnWidths.Length)
				return false; // prevents crash on deleting a chart-internal template column.

			int i = 0;
			m_columnPositions[0] = 0;
			foreach (XmlNode node in doc.DocumentElement.ChildNodes)
			{
				int width = XmlUtils.GetMandatoryIntegerAttributeValue(node, "width");
				m_columnPositions[i + 1] = m_columnPositions[i] + MpToPixelX(width);
				if (i < m_columnWidths.Length)
					m_columnWidths[i++] = width;
				else
					return false;
			}
			return i == m_columnWidths.Length;
			// succeed only if exact expected number.
		}

		/// <summary>
		/// Save the current column widths in the mediator's property table.
		/// </summary>
		void PersistColumnWidths()
		{
			var colList = new StringBuilder();
			colList.Append("<root>");
			foreach (int val in m_columnWidths)
			{
				colList.Append("<col width=\"" + val + "\"/>");
			}
			colList.Append("</root>");
			var cwId = ColWidthId();
			PropertyTable.SetProperty(cwId, colList.ToString(), true, true);
		}

		private string ColWidthId()
		{
			return "ConstChartColWidths" + (m_chart == null ? Guid.Empty : m_chart.Guid);
		}

		void btnMoveHere_Click(object sender, EventArgs e)
		{
			// find the index in the button row.
			var btn = sender as Button;
			var icol = GetColumnOfButton(btn);
			m_logic.MoveToColumnInUOW(icol);
		}

		private int GetColumnOfButton(Button btn)
		{
			// each column corresponds to a pair of MoveHereButtons and ContextMenuButtons in the buttonRow.
			int icol = btn.Parent.Controls.IndexOf(btn) / 2;
			if (ChartIsRtL)
				icol = m_logic.ConvertColumnIndexToFromRtL(icol, m_logic.AllMyColumns.Length - 1);
			return icol;
		}

		// Event handler to run if Ribbon changes
		void m_logic_Ribbon_Changed(object sender, EventArgs e)
		{
			int iPara, offset;
			// 'out' vars for NextInputIsChOrph()
			// Tests ribbon contents
			if (m_logic.NextInputIsChOrph(out iPara, out offset))
			{
				Debug.Assert(m_bookmark != null, "Hit null bookmark. Why?");
				if (m_bookmark != null)
					m_bookmark.Reset(m_bookmark.TextIndex);
				// Resetting of highlight is done in the array setter now.
				PrepareForChOrphInsert(iPara, offset);
				// scroll to ChOrph, highlight cell possibilities, set bookmark etc.
			}
			else
			{
				// Got past the last ChOrph, now reset for normal charting
				if (m_logic.IsChOrphActive)
				{
					EnableAllContextButtons();
					EnableAllMoveHereButtons();
					m_logic.ResetRibbonLimits();
					m_logic.CurrHighlightCells = null;
					// Should reset highlighting (w/PropChanged)
					// Where should we go next? End or top of chart depending on whether chart is complete
					if (!m_logic.IsChartComplete)
						ScrollToEndOfChart();
					else
					{
						// create a ChartLocation for scrolling and scroll to first row
						m_body.SelectAndScrollToLoc(new ChartLocation(m_chart.RowsOS[0], 0), false);
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
			DisposeContextMenu(this, new EventArgs());
			m_contextMenuStrip = m_logic.MakeContextMenu(icol);
			m_contextMenuStrip.Closed += contextMenuStrip_Closed; // dispose when no longer needed (but not sooner! needed after this returns)
			m_contextMenuStrip.Show(btn, new Point(0, btn.Height));
		}
		private ContextMenuStrip m_contextMenuStrip;

		void contextMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			// It's apparently still needed by the menu handling code in .NET.
			// So we can't dispose it yet.
			// But we want to eventually (Eberhard says if it has a Dispose we MUST call it to make Mono happy)
			Application.Idle += DisposeContextMenu;
		}

		void DisposeContextMenu(object sender, EventArgs e)
		{
			Application.Idle -= DisposeContextMenu;
			if (m_contextMenuStrip != null && !m_contextMenuStrip.IsDisposed)
			{
				m_contextMenuStrip.Dispose();
				m_contextMenuStrip = null;
			}
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			m_ribbon.Focus();
			// Enhance: decide which one should have focus.
		}

#if RANDYTODO
		/// <summary>
		///  If this control is a colleague, export Discourse should be available.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayExportDiscourse(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = m_chart != null;
			// in concordance we may have no chart if no text selected.
			display.Visible = true;
			return true;
		}
#endif

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
			using (var dlg = new DiscourseExportDialog(m_chart.Hvo, m_body.Vc, m_logic.WsLineNumber))
			{
				dlg.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
				dlg.ShowDialog(this);
			}

			return true; // we handled this
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
				var oldStyles = m_ribbon.StyleSheet;
				m_ribbon.StyleSheet = value;
				if (oldStyles != value)
					m_ribbon.SelectFirstOccurence();
				// otherwise, selection disappears.
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
			var result = new InterlinLineChoices(m_cache.LangProject, m_cache.DefaultVernWs, m_cache.DefaultAnalWs);
			string persist = null;
			if (PropertyTable != null)
			{
				persist = PropertyTable.GetValue<string>(ConfigPropName, SettingsGroup.LocalSettings);
			}
			InterlinLineChoices lineChoices = null;
			if (persist != null)
			{
				lineChoices = InterlinLineChoices.Restore(persist, m_cache.ServiceLocator.GetInstance<ILgWritingSystemFactory>(), m_cache.LangProject, m_cache.DefaultVernWs, m_cache.DefaultAnalWs);
			}
			GetLineChoice(result, lineChoices, InterlinLineChoices.kflidWord);
			GetLineChoice(result, lineChoices, InterlinLineChoices.kflidWordGloss);
			return result;
		}

		/// <summary>
		/// Make sure there is SOME lineChoice for the specified flid in m_lineChoices.
		/// If lineChoices is non-null and contains one for the right flid, choose the first.
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="source"></param>
		/// <param name="flid"></param>
		private static void GetLineChoice(InterlinLineChoices dest, InterlinLineChoices source, int flid)
		{
			if (source != null)
			{
				var index = source.IndexOf(flid);
				if (index >= 0)
				{
					dest.Add(source[index]);
					return;
				}
			}
			// Last resort.
			dest.Add(flid);
		}
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
				case HDN_DIVIDERDBLCLICKW:
					// double-click on line between column headers.
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
	}
}
