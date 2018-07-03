// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.IText;
using SIL.Utils;
using SIL.Windows.Forms.Widgets;
using XCore;

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
	public partial class ConstituentChart : InterlinDocChart, IHandleBookmark, IxCoreColleague, IStyleSheet
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
		private int m_chartHvo = 0;
		private ICmPossibility m_template;
		private ICmPossibility[] m_allColumns;
		private ConstituentChartLogic m_logic;
		private Panel m_templateSelectionPanel;
		private Panel m_buttonRow;
		private Panel m_bottomStuff;
		private SplitContainer m_topBottomSplit;
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
		private ILcmServiceLocator m_serviceLocator;
		private XmlNode m_configurationParameters;
		#endregion

		/// <summary>
		/// Make one. Usually called by reflection.
		/// </summary>
		public ConstituentChart(LcmCache cache) : this(cache, new ConstituentChartLogic(cache))
		{
		}

		/// <summary>
		/// Make one. This variant is used in testing (to plug in a known logic class).
		/// </summary>
		internal ConstituentChart(LcmCache cache, ConstituentChartLogic logic)
		{
			Cache = cache;
			m_serviceLocator = Cache.ServiceLocator;
			m_logic = logic;
			ForEditing = true;
			Name = "ConstituentChart";
			Vc = new InterlinVc(Cache);

			BuildUIComponents();
		}

		/// <summary>
		/// This is for setting Vc.LineChoices even before we have a valid vc.
		/// </summary>
		protected InterlinLineChoices LineChoices { get; set; }

		/// <summary>
		///  Launch the Configure interlinear dialog and deal with the results.
		/// </summary>
		/// <param name="argument"></param>
		public override bool OnConfigureInterlinear(object argument)
		{
			LineChoices = GetLineChoices();
			Vc.LineChoices = LineChoices;

			using (var dlg = new ConfigureInterlinDialog(this.Cache, this.PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"),
				this.m_ribbon.Vc.LineChoices.Clone() as InterlinLineChoices))
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					UpdateForNewLineChoices(dlg.Choices);
				}

				return true; // We handled this
			}
		}

		/// <summary>
		/// Persist the new line choices and
		/// Reconstruct the document based on the given newChoices for interlinear lines.
		/// </summary>
		/// <param name="newChoices"></param>
		internal virtual void UpdateForNewLineChoices(InterlinLineChoices newChoices)
		{
			LineChoices = newChoices;
			m_ribbon.Vc.LineChoices = newChoices;
			m_body.LineChoices = newChoices;

			PersistAndDisplayChangedLineChoices();
		}

		internal void PersistAndDisplayChangedLineChoices()
		{
			PropertyTable.SetProperty(ConfigPropName,
				m_ribbon.Vc.LineChoices.Persist(Cache.LanguageWritingSystemFactoryAccessor),
				PropertyTable.SettingsGroup.LocalSettings,
				true);
			PropertyTable.SetProperty(ConfigPropName,
				m_body.LineChoices.Persist(Cache.LanguageWritingSystemFactoryAccessor),
				PropertyTable.SettingsGroup.LocalSettings,
				true);
			UpdateDisplayForNewLineChoices();
		}

		/// <summary>
		/// Do whatever is necessary to display new line choices.
		/// </summary>
		private void UpdateDisplayForNewLineChoices()
		{
			if (m_ribbon.RootBox == null || m_body.RootBox == null)
				return;
			m_ribbon.RootBox.Reconstruct();
			m_body.RootBox.Reconstruct();
		}

		private void BuildUIComponents()
		{
			SuspendLayout();

			m_topBottomSplit = new SplitContainer();
			m_topBottomSplit.Layout += SplitLayout;
			BuildBottomStuffUI();
			BuildTopStuffUI();
			m_topBottomSplit.Orientation = Orientation.Horizontal;
			Controls.Add(m_topBottomSplit);

			Dock = DockStyle.Fill;

			ResumeLayout();
		}

		private void SplitLayout(object sender, LayoutEventArgs e)
		{
			var container = sender as SplitContainer;
			container.Width = Width;
			container.Height = Height;
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			// We don't want to know about column width changes until after we're initialized and have restored original widths.
			m_headerMainCols.ColumnWidthChanged += m_headerMainCols_ColumnWidthChanged;
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			//Call SplitLayout here to ensure Mono properly updates Splitter length
			SplitLayout(m_topBottomSplit, e);
			//Mono makes SplitLayout calls while Splitter is moving so set default distance here
			m_topBottomSplit.SplitterDistance = (int) (Height * .9);
			base.OnLayout(e);
		}

		/// <summary>
		/// Method called by Mediator to refresh view after Undoable UOW is completed
		/// Method name is defined by a mediator message posted in ConstituentChartLogic.changeTemplate_Click
		/// </summary>
		public virtual void OnTemplateChanged(string name)
		{
			SetRoot(m_hvoRoot);
		}

		private void BuildTopStuffUI()
		{
			m_body = new ConstChartBody(m_logic, this) { Cache = Cache, Dock = DockStyle.Fill };

			// Seems to be right (cf BrowseViewer) but not ideal.
			m_headerMainCols = new ChartHeaderView(this) { Dock = DockStyle.Top, Height = 22 };

			m_headerMainCols.Layout += m_headerMainCols_Layout;
			m_headerMainCols.SizeChanged += m_headerMainCols_SizeChanged;

			m_templateSelectionPanel = new Panel() { Height = new Button().Height, Dock = DockStyle.Top, Width = 0 };
			m_templateSelectionPanel.Layout += new LayoutEventHandler(TemplateSelectionPanel_Layout);

			m_topStuff = m_topBottomSplit.Panel1;
			m_topStuff.Controls.AddRange(new Control[] { m_body, m_headerMainCols, m_templateSelectionPanel });
		}

		private void TemplateSelectionPanel_Layout(object sender, EventArgs e)
		{
			var panel = sender as Panel;
			if (panel.Controls.Count != 0)
			{
				var templateButton = panel.Controls[0];
				templateButton.SuspendLayout();
				templateButton.Width = new Button().Width * 2;
				templateButton.Left = panel.Width - templateButton.Width;
				templateButton.ResumeLayout();
			}
		}

		private void BuildBottomStuffUI()
		{
			// fills the 'bottom stuff'
			m_ribbon = new InterlinRibbon(Cache, 0) { Dock = DockStyle.Fill };
			m_logic.Ribbon = m_ribbon;
			m_logic.Ribbon_Changed += m_logic_Ribbon_Changed;

			// Holds tooltip help for 'Move Here' buttons.
			// Set up the delays for the ToolTip.
			// Force the ToolTip text to be displayed whether or not the form is active.
			m_toolTip = new ToolTip { AutoPopDelay = 5000, InitialDelay = 1000, ReshowDelay = 500, ShowAlways = true };

			m_bottomStuff = m_topBottomSplit.Panel2;
			m_bottomStuff.Height = 100;
			m_bottomStuff.SuspendLayout();

			m_buttonRow = new Panel { Height = new Button().Height, Dock = DockStyle.Top, BackColor = Color.FromKnownColor(KnownColor.ControlLight) };
			m_fContextMenuButtonsEnabled = true;
			m_buttonRow.Layout += m_buttonRow_Layout;

			m_bottomStuff.Controls.AddRange(new Control[] { m_ribbon, m_buttonRow });
			m_bottomStuff.ResumeLayout();
		}

		/* This is no longer necessary because InterlinDocChart implements
		   IInterlinConfigurable, which implements IInterlinearTabControl
		LcmCache IInterlinearTabControl.Cache
		{
			get { return Cache; }
			set { Cache = value; }
		}*/

		//#region kflid Constants

		//const int kflidRows = DsConstChartTags.kflidRows;
		//const int kflidParagraphs = StTextTags.kflidParagraphs;

		//#endregion

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

		void m_headerMainCols_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
		{
			if (m_fInColWidthChanged)
				return;
			m_fInColWidthChanged = true;
			try
			{
				int icolChanged = e.ColumnIndex;
				int ccol = m_headerMainCols.Controls.Count;
				int totalWidth = 0;
				int maxWidth = MaxUseableWidth();
				foreach (Control ch in m_headerMainCols.Controls)
					totalWidth += ch.Width + 0;
				if (totalWidth > maxWidth)
				{
					int delta = totalWidth - maxWidth;
					int remainingCols = ccol - icolChanged - 1;
					int icolAdjust = icolChanged + 1;
					while (remainingCols > 0)
					{
						int deltaThis = delta / remainingCols;
						m_headerMainCols[icolAdjust].Width -= deltaThis;
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
			m_headerMainCols.UpdatePositions();
		}

		void m_headerMainCols_SizeChanged(object sender, EventArgs e)
		{
			SetHeaderColAndButtonWidths();
		}

		void m_headerMainCols_Layout(object sender, LayoutEventArgs e)
		{
			SetHeaderColAndButtonWidths();
		}

		/// <summary/>
		protected virtual void SetHeaderColAndButtonWidths()
		{
			//Do not change column widths until positions have been updated to represent template change
			//m_columnPositions should be one longer due to fenceposting
			if (m_columnPositions != null && m_columnPositions.Length == m_headerMainCols.Controls.Count + 1)
			{
				m_fInColWidthChanged = true;
				try
				{
					//GetColumnWidths();
					for (int i = 0; i < m_headerMainCols.Controls.Count; i++)
					{
						int width = m_columnPositions[i + 1] - m_columnPositions[i];
						if (m_headerMainCols[i].Width != width)
							m_headerMainCols[i].Width = width;
					}
					m_headerMainCols.UpdatePositions();
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
			if (m_mediator != null && m_columnWidths != null && m_chart != null && !HasPersistantColWidths)
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

			if (m_headerMainCols != null && m_headerMainCols.Controls.Count == m_allColumns.Length + ConstituentChartLogic.NumberOfExtraColumns)
			{
				// Take it from the headers if we have them set up already.
				m_columnWidths = new int[m_allColumns.Length + ConstituentChartLogic.NumberOfExtraColumns];
				m_columnPositions = new int[m_allColumns.Length + ConstituentChartLogic.NumberOfExtraColumns + 1];
				var ccol = m_headerMainCols.Controls.Count;
				for (var icol = 0; icol < ccol; icol++)
				{
					var width = m_headerMainCols[icol].Width;
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
				int offset = NotesColumnOnRight ? 0 : 1;
				offset += ChartIsRtL ? 0 : 1;
				// main button
				c.Left = m_columnPositions[ipair + offset] + 2;
				// skip number column, fine tune
				c.Width = m_columnPositions[ipair + offset + 1] - m_columnPositions[ipair + offset] - widthBtnContextMenu;
				// Redo button name in case some won't (or now will!) fit on the button
				c.Text = GetBtnName(m_headerMainCols[ipair + offset].Text, c.Width - ((c as Button).Image.Width * 2));
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
				var defWs = Cache.ServiceLocator.WritingSystemManager.Get(RootStText.MainWritingSystem);
				return defWs.RightToLeftScript;
			}
		}

		public void RefreshRoot()
		{
			SetRoot(m_hvoRoot);
		}

		/// <summary>
		/// Set the root object.
		/// </summary>
		public override void SetRoot(int hvo)
		{
			int oldTemplateHvo = 0;
			if (m_template != null)
				oldTemplateHvo = m_template.Hvo;
			// does it already have a chart? If not make one.
			m_chart = null;
			m_hvoRoot = hvo;
			if (m_hvoRoot == 0)
				RootStText = null;
			else
				RootStText = (IStText)Cache.ServiceLocator.ObjectRepository.GetObject(hvo);
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
					m_chart.TemplateRA = Cache.LangProject.GetDefaultChartTemplate();
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

			// If necessary adjust number of buttons
			if (m_MoveHereButtons.Count != m_allColumns.Length && hvo > 0)
			{
				SetupMoveHereButtonsToMatchTemplate();
			}
			SetHeaderColAndButtonWidths();

			BuildTemplatePanel();

			if (m_chart != null)
			{
				m_body.SetRoot(m_chart.Hvo, m_allColumns, ChartIsRtL);

				GetAndScrollToBookmark();
			}

			else
				m_body.SetRoot(0, null, false);
		}

		private void BuildTemplatePanel()
		{
			if (m_template == null)
				return;
			if (m_templateSelectionPanel.Controls.Count > 0)
			{
				((ComboBox)m_templateSelectionPanel.Controls[0]).SelectedItem = m_template;
				return;
			}
			ComboBox templateButton = new ComboBox();
			m_templateSelectionPanel.Controls.Add(templateButton);
			templateButton.Layout += TemplateDropDownMenu_Layout;
			templateButton.Left = m_templateSelectionPanel.Width - templateButton.Width;
			templateButton.DropDownStyle = ComboBoxStyle.DropDownList;
			templateButton.SelectionChangeCommitted += TemplateSelectionChanged;
			foreach (var chartTemplate in ((ICmPossibilityList)m_template.Owner).PossibilitiesOS)
			{
				templateButton.Items.Add(chartTemplate);
			}
			templateButton.SelectedItem = m_template;
			templateButton.Items.Add(DiscourseStrings.ksCreateNewTemplate);
		}

		private void TemplateSelectionChanged(object sender, EventArgs e)
		{
			var selection = sender as ComboBox;
			var template = selection.SelectedItem as ICmPossibility;

			//If user chooses to add a new template then navigate them to the Text Constituent Chart Template list view
			if (selection.SelectedItem as string == DiscourseStrings.ksCreateNewTemplate)
			{
				m_mediator.PostMessage("FollowLink", new FwLinkArgs(DiscourseStrings.ksNewTemplateLink, new Guid()));
				selection.SelectedItem = m_template;
				return;
			}

			//Return if user selects current template
			if (template == m_template)
			{
				return;
			}

			//Detect if there is already a chart created for the given text and template
			IDsConstChart selectedChart = null;
			foreach (var chart in Cache.LangProject.DiscourseDataOA.ChartsOC.Cast<IDsConstChart>().Where(chart => chart.BasedOnRA != null && chart.BasedOnRA == RootStText && chart.TemplateRA == template))
			{
				selectedChart = chart;
			}

			//If there is no such chart, then create one
			if (selectedChart == null)
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					selectedChart = m_serviceLocator.GetInstance<IDsConstChartFactory>().Create(
						Cache.LangProject.DiscourseDataOA, RootStText, selection.SelectedItem as ICmPossibility);
				}
				);
			}


			m_chartHvo = selectedChart.Hvo;
			SetRoot(m_hvoRoot);
		}

		private void TemplateDropDownMenu_Layout(object sender, EventArgs e)
		{
			var button = sender as ComboBox;
			button.Left = m_templateSelectionPanel.Width - button.Width;
		}

		/// <summary>
		/// Try to get the bookmark from InterlinMaster, if there are rows in the chart.
		/// </summary>
		private void GetAndScrollToBookmark()
		{
			if (m_chart.RowsOS.Count <= 0)
			{
				// Reset bookmark to prevent LT-12666
				if (m_bookmark != null && m_mediator != null)
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
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => { m_chart = m_serviceLocator.GetInstance<IDsConstChartFactory>().Create(Cache.LangProject.DiscourseDataOA, RootStText, Cache.LangProject.GetDefaultChartTemplate()); });
			m_chartHvo = m_chart.Hvo;
		}

		private void DetectAndReportTemplateProblem()
		{
			var templates = Cache.LangProject.DiscourseDataOA.ConstChartTemplOA.PossibilitiesOS;
			if (templates.Count == 0 || templates[0].SubPossibilitiesOS.Count == 0)
			{
				MessageBox.Show(this, DiscourseStrings.ksNoColumns, DiscourseStrings.ksWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
			//This method is called multiple times and sometimes on the early calls the data does not agree
			//if so, wait until a later call to enable buttons
			if (m_MoveHereButtons.Count != goodColumns.Length)
				return;
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
			foreach (var chart in Cache.LangProject.DiscourseDataOA.ChartsOC.Cast<IDsConstChart>().Where(chart => chart.BasedOnRA != null && chart.BasedOnRA.Hvo == hvoStText))
			{
				m_chart = chart;
				m_logic.Chart = m_chart;
				m_logic.CleanupInvalidChartCells();
				//If a template change requests a specific chart, then use that one, otherwise use the last active chart
				if (m_chart.Hvo == m_chartHvo)
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
			get { return PropertyTable.GetStringProperty(ColWidthId(), null) != null; }
		}

		/// <summary>
		/// Restore column widths if any are persisted for this chart
		/// </summary>
		/// <returns>true if it found a valid set of widths.</returns>
		bool RestoreColumnWidths()
		{
			if (m_mediator == null)
				return false;
			string savedCols = PropertyTable.GetStringProperty(ColWidthId(), null);
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
			PropertyTable.SetProperty(cwId, colList.ToString(), true);
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
				for (int icol = 0; icol < m_MoveHereButtons.Count; icol++)
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
			m_contextMenuStrip = m_logic.InsertIntoChartContextMenu(icol);
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
			using (var dlg = new DiscourseExportDialog(m_mediator, PropertyTable, m_chart.Hvo, m_body.Vc, m_logic.WsLineNumber))
			{
				dlg.ShowDialog(this);
			}

			return true;
			// we handled this
		}

		public bool NotesColumnOnRight
		{
			get { return m_headerMainCols.NotesOnRight; }
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
		/// <param name="propertyTable"></param>
		/// <param name="configurationParameters"></param>
		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			m_mediator = mediator;
			PropertyTable = propertyTable;
			if (PropertyTable != null)
				m_logic.Init(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"));

			m_configurationParameters = configurationParameters;
			InterlinLineChoices lineChoices = GetLineChoices();
			m_body.Init(mediator, propertyTable, m_configurationParameters);
			m_body.LineChoices = lineChoices;
			m_ribbon.Init(mediator, propertyTable, m_configurationParameters);
			m_ribbon.RibbonLineChoices = lineChoices;
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
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
			get;
			set;
		}

		public override InterlinLineChoices SetupLineChoices(string lineConfigPropName, InterlinLineChoices.InterlinMode mode)
		{
			ConfigPropName = lineConfigPropName;
			InterlinLineChoices lineChoices;
			if (!TryRestoreLineChoices(out lineChoices))
			{
				if (ForEditing)
				{
					lineChoices = EditableInterlinLineChoices.DefaultChoices(Cache.LangProject,
						WritingSystemServices.kwsVern, WritingSystemServices.kwsAnal);
					lineChoices.Mode = mode;
					lineChoices.SetStandardChartState();
				}
				else
				{
					lineChoices = InterlinLineChoices.DefaultChoices(Cache.LangProject,
						WritingSystemServices.kwsVern, WritingSystemServices.kwsAnal, mode);
				}
			}
			else if (ForEditing)
			{
				// just in case this hasn't been set for restored lines
				lineChoices.Mode = mode;
			}
			LineChoices = lineChoices;
			return LineChoices;
		}

		internal bool TryRestoreLineChoices(out InterlinLineChoices lineChoices)
		{
			lineChoices = null;
			var persist = PropertyTable.GetStringProperty(ConfigPropName, null, PropertyTable.SettingsGroup.LocalSettings);
			if (persist != null)
			{
				lineChoices = InterlinLineChoices.Restore(persist, Cache.LanguageWritingSystemFactoryAccessor,
					Cache.LangProject, Cache.DefaultVernWs, Cache.DefaultAnalWs);
			}
			return persist != null && lineChoices != null;
		}

		private Mediator m_mediator;
		//private PropertyTable PropertyTable;

		private InterlinLineChoices GetLineChoices()
		{
			var result = new InterlinLineChoices(Cache.LangProject, Cache.DefaultVernWs, Cache.DefaultAnalWs, InterlinLineChoices.InterlinMode.Chart);
			string persist = null;
			if (PropertyTable != null)
			{
				persist = PropertyTable.GetStringProperty(ConfigPropName, null, PropertyTable.SettingsGroup.LocalSettings);
			}
			InterlinLineChoices lineChoices = null;
			if (persist != null)
			{
				lineChoices = InterlinLineChoices.Restore(persist, Cache.ServiceLocator.GetInstance<ILgWritingSystemFactory>(), Cache.LangProject, Cache.DefaultVernWs, Cache.DefaultAnalWs, InterlinLineChoices.InterlinMode.Chart);
			}
			else
			{
				GetLineChoice(result, lineChoices,
					InterlinLineChoices.kflidWord,
					InterlinLineChoices.kflidWordGloss);
				return result;
			}

			return lineChoices;
		}

		/// <summary>
		/// Make sure there is SOME lineChoice for the specified flid in m_lineChoices.
		/// If lineChoices is non-null and contains one for the right flid, choose the first.
		/// </summary>
		/// <param name="dest"></param>
		/// <param name="source"></param>
		/// <param name="flids"></param>
		private static void GetLineChoice(InterlinLineChoices dest, InterlinLineChoices source, params int[] flids)
		{
			foreach (int flid in flids)
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
		}

		public bool NotesDataFromPropertyTable
		{
			get
			{
				return PropertyTable == null || PropertyTable.GetBoolProperty("notesOnRight",
					true, PropertyTable.SettingsGroup.LocalSettings);
			}
			set
			{
				PropertyTable?.SetProperty("notesOnRight", value, PropertyTable.SettingsGroup.LocalSettings, false);
			}
		}

		#endregion

	} // End Constituent Chart class

	/// <summary>
	/// This control is used to make the column headers for a Constituent Chart.
	/// It handles mouse events for resizing columns and
	/// Dragging the notes column to the left or right side of the chart
	/// </summary>
	public class ChartHeaderView : Control
	{
		private ConstituentChart m_chart;
		private bool m_notesOnRight = true;
		private bool m_isDraggingNotes;
		private bool m_isResizingColumn;
		private bool m_notesWasOnRight;
		private int m_origHeaderLeft;
		private int m_origMouseLeft;
		private const int kColMinimumWidth = 5;

		/// <summary>
		/// Create one and set the chart it belongs to.
		/// </summary>
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
		/// Releases the unmanaged resources used by the <see cref="T:System.Windows.Forms.Control"/> and optionally releases the managed resources.
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

		public bool NotesOnRight
		{
			get { return m_notesOnRight; }
		}

		/// <summary>
		/// ControlList[] represents the z index, so in order to keep the draggable notes column at the top of the z order,
		/// This custom [] is designed to be used instead representing the x order of column headers
		/// </summary>
		public Control this[int key]
		{
			get
			{
				if (key < 0 || key >= Controls.Count)
					throw new IndexOutOfRangeException();
				if (!m_notesOnRight)
				{
					return Controls[key];
				}
				return Controls[(key + 1) % Controls.Count];
			}
		}

		/// <summary>
		/// New IndexOf to complement custom []
		/// </summary>
		private int IndexOf(Control c)
		{
			for (int i = 0; i < Controls.Count; i++)
			{
				if (this[i] == c)
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Updates the positions of all the column headers to consecutive order without gaps or overlaps
		/// </summary>
		public void UpdatePositions()
		{
			if (m_isDraggingNotes)
				return;
			if (Controls.Count < 2)
				return;
			this[0].Left = 1;
			for (int i = 1; i < Controls.Count; i++)
			{
				this[i].Left = this[i - 1].Right;
			}
		}

		/// <summary>
		/// Moves all the other column headers to where they should be when the notes column is dropped
		/// </summary>
		private void UpdatePositionsExceptNotes()
		{
			if (m_notesOnRight)
			{
				Controls[1].Left = 1;
			}
			else
			{
				Controls[1].Left = Controls[0].Width;
			}

			for (int i = 2; i < Controls.Count; i++)
			{
				Controls[i].Left = Controls[i - 1].Right;
			}
		}

		public event ColumnWidthChangedEventHandler ColumnWidthChanged;

		/// <summary>
		/// Resizes the identified column to the default width
		/// </summary>
		public void AutoResizeColumn(int iColumnChanged)
		{
			int num = Width / (Controls.Count + 1);
			this[iColumnChanged].Width = num;
			UpdatePositions();
			ColumnWidthChanged(this, new ColumnWidthChangedEventArgs(iColumnChanged));
		}

		/// <summary>
		/// Prepares new Control with the proper mouse events and visual elements
		/// </summary>
		protected override void OnControlAdded(ControlEventArgs e)
		{
			//Get the notes value from the property table once the first column has been added
			if (Controls.Count == 1)
			{
				m_notesOnRight = m_chart.NotesDataFromPropertyTable;
			}
			Control newColumn = e.Control;
			newColumn.Height = 22;
			newColumn.MouseDown += OnColumnMouseDown;
			newColumn.MouseMove += OnColumnMouseMove;
			newColumn.MouseUp += OnColumnMouseUp;
			newColumn.Paint += OnColumnPaint;
			newColumn.DoubleClick += OnColumnDoubleClick;
			if (newColumn is HeaderLabel)
			{
				((HeaderLabel) newColumn).BorderStyle = BorderStyle.None;
			}
		}

		/// <summary>
		/// Handles a column's double click to automatically resize the column to the left of the border clicked
		/// </summary>
		private void OnColumnDoubleClick(object sender, EventArgs e)
		{
			var header = sender as Control;
			if (header.Cursor != Cursors.VSplit)
			{
				return;
			}

			int leftHeader = IndexOf(header);
			if (m_origMouseLeft < 3)
			{
				leftHeader--;
			}
			AutoResizeColumn(leftHeader);
		}

		/// <summary>
		/// Handles a column's mousedown to possibly enter the resizing state and store the initial coordinates
		/// </summary>
		private void OnColumnMouseDown(object sender, MouseEventArgs e)
		{
			Control header = sender as Control;
			if (header.Cursor == Cursors.VSplit)
			{
				m_isResizingColumn = true;
			}

			m_origHeaderLeft = header.Left;
			m_origMouseLeft = e.X;
			m_notesWasOnRight = m_notesOnRight;
			header.SuspendLayout();
			SuspendLayout();
		}

		/// <summary>
		/// Handles a column's mousemove to resize if in the resize state or move the notes column
		/// </summary>
		private void OnColumnMouseMove(object sender, MouseEventArgs e)
		{
			var header = sender as Control;
			if ((e.X < 3 && header != this[0]) || (e.X > header.Width - 3))
			{
				header.Cursor = Cursors.VSplit;
			}
			else
			{
				header.Cursor = DefaultCursor;
			}

			if (e.Button != MouseButtons.Left) return;
			if (m_isResizingColumn)
			{
				ResizeColumn(header, e);
			}
			else
			{
				MoveColumn(header, e);
			}
			Parent.Update();
		}

		/// <summary>
		/// Controls MouseMove event for column header in case we are in the resize state
		/// </summary>
		private void ResizeColumn(Control header, MouseEventArgs e)
		{
			Control prevHeader;
			int X;
			if (m_origMouseLeft < 3)
			{
				prevHeader = this[IndexOf(header) - 1];
				X = e.X + header.Left - prevHeader.Left;
			}
			else
			{
				prevHeader = header;
				X = e.X;
			}

			prevHeader.Width = X;
			if (prevHeader.Width < kColMinimumWidth)
				prevHeader.Width = kColMinimumWidth;
			UpdatePositions();
		}

		/// <summary>
		/// Controls MouseMove event for column header in case we are in the move notes column state
		/// </summary>
		private void MoveColumn(Control header, MouseEventArgs e)
		{
			if (header.Text != DiscourseStrings.ksNotesColumnHeader) return;
			if (header.Left < m_origHeaderLeft - 20)
			{
				m_notesOnRight = false;
			}
			else if (header.Left > m_origHeaderLeft + 20 || m_notesWasOnRight)
			{
				m_notesOnRight = true;
			}
			else
			{
				m_notesOnRight = false;
			}
			UpdatePositionsExceptNotes();
			m_isDraggingNotes = true;
			header.Left += (e.X - m_origMouseLeft);
		}

		/// <summary>
		/// Handles a column's mouseup to remove any state data and finalize changes made in a mousemove
		/// </summary>
		private void OnColumnMouseUp(object sender, MouseEventArgs e)
		{
			var header = sender as Control;
			if (m_isResizingColumn)
			{
				UpdatePositions();
				ColumnWidthChanged?.Invoke(this, new ColumnWidthChangedEventArgs(IndexOf(header)));
			}
			else if (m_isDraggingNotes)
			{
				header.Left = m_origHeaderLeft;
			}

			m_isDraggingNotes = false;
			m_isResizingColumn = false;


			UpdatePositions();
			if (m_notesWasOnRight != NotesOnRight)
			{
				m_chart.NotesDataFromPropertyTable = m_notesOnRight;
				ColumnWidthChanged?.Invoke(this, new ColumnWidthChangedEventArgs(0));
				m_chart.RefreshRoot();
			}

			header.ResumeLayout(false);
			ResumeLayout(false);
		}

		/// <summary>
		/// Draws the border around a column header
		/// </summary>
		private void OnColumnPaint(object sender, PaintEventArgs e)
		{
			var header = sender as Control;
			var topLeft = new Point(0, 0);
			var bottomRight = new Size(header.Width - 1, header.Height - 1);
			e.Graphics.DrawRectangle(new Pen(Color.Black), new Rectangle(topLeft, bottomRight));
		}

		/// <summary>
		/// Handles resizing of the last column if the mouse hangs over its border slightly
		/// </summary>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.X < this[Controls.Count - 1].Right + 3)
			{
				Cursor = Cursors.VSplit;
			}
			else
			{
				Cursor = Cursors.Default;
			}

			if (!m_isResizingColumn)
				return;

			var header = this[Controls.Count - 1];
			header.Width = e.X - header.Left;
			if (header.Width < kColMinimumWidth)
			{
				header.Width = kColMinimumWidth;
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (Cursor == Cursors.VSplit)
			{
				m_isResizingColumn = true;
				SuspendLayout();
				this[Controls.Count - 1].SuspendLayout();
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			m_isResizingColumn = false;
			ResumeLayout(false);
			this[Controls.Count - 1].ResumeLayout(false);
			ColumnWidthChanged(this, new ColumnWidthChangedEventArgs(Controls.Count - 1));
		}
	}
}
