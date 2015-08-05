// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using XCore;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// InterlinMaster is a master control for the main pane of an interlinear view.
	/// It holds and information bar ("Information"), a TitleContents pane,
	/// another information bar ("Text"/"Interlinear Text") with a label button
	/// ("Show Interlinear"/"Show Raw Text") and then either a RawTextPane or an
	/// InterlinDocChild. Eventually it may also show a SandBox, and perhaps a
	/// segment of a lexicon! This comment is way out-of-date!
	/// </summary>
	public partial class InterlinMaster : InterlinMasterBase, IFocusablePanePortion
	{
		// Controls
		protected IVwStylesheet m_styleSheet;
		protected InfoPane m_infoPane; // Parent is m_tpInfo.

		static public Dictionary<Tuple<string, Guid>, InterAreaBookmark> m_bookmarks;
		private bool m_fParsedTextDuringSave;

		// This flag is normally set during a Refresh. When it is set, we suppress switching the focus box
		// to the current occurrence in a concordance view, which would otherwise happen as a side effect
		// of the Refresh. Instead, as usual the focus box stays wherever it is now. The flag is cleared
		// on the next call to ShowRecord.
		// If we don't have a current record, Refresh won't result in ShowRecord being called, so we don't
		// need to set the flag (and must not, lest it interfere with the next time we move to a different
		// occurrence).
		private bool m_fRefreshOccurred;

		// true (typically used as concordance 3rd pane) to suppress autocreating a text if the
		// clerk has no current object.
		protected bool m_fSuppressAutoCreate;

		private string m_currentTool = "";

		public string CurrentTool
		{
			get { return m_currentTool; }
		}

		/// <summary>
		/// Numbers identifying the main tabs in the interlinear text.
		/// </summary>
		public enum TabPageSelection
		{
			Info = 0,
			RawText = 1,
			Gloss = 2,
			Interlinearizer = 3,
			TaggingView = 4,
			PrintView = 5,
			ConstituentChart = 6
		}

		// These constants allow us to use a switch statement in SaveBookMark()
		const int ktpsInfo = (int)TabPageSelection.Info;
		const int ktpsRawText = (int)TabPageSelection.RawText;
		const int ktpsGloss = (int)TabPageSelection.Gloss;
		const int ktpsAnalyze = (int)TabPageSelection.Interlinearizer;
		const int ktpsTagging = (int)TabPageSelection.TaggingView;
		const int ktpsPrint = (int)TabPageSelection.PrintView;
		const int ktpsCChart = (int)TabPageSelection.ConstituentChart;

		public InterlinMaster()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		internal string BookmarkId
		{
			get { return m_vectorName ?? ""; }
		}

		/// <summary>
		/// Something sometimes insists on giving the tab control focus when switching tabs.
		/// This defeats ctrl-tab to move between tabs.
		/// </summary>
		void m_tabCtrl_GotFocus(object sender, EventArgs e)
		{
			if (m_tabCtrl.SelectedTab == null)
				return;
			var child = (from Control c in m_tabCtrl.SelectedTab.Controls select c).FirstOrDefault();
			if (child != null)
				child.Focus();
		}

		/// <summary>
		/// Called by reflection when the browse view steals the focus.
		/// </summary>
		/// <param name="sender"></param>
		public void OnBrowseViewStoleFocus(object sender)
		{
			m_tabCtrl_GotFocus(sender, new EventArgs());
		}

		internal bool ParsedDuringSave
		{
			get
			{
				CheckDisposed();
				return m_fParsedTextDuringSave;
			}
			set
			{
				CheckDisposed();
				m_fParsedTextDuringSave = value;
			}
		}

		internal TitleContentsPane TitleContentsPane
		{
			get { return m_tcPane; }
			set { m_tcPane = value; }
		}

		protected int GetWidth(string text, Font fnt)
		{
			int width;
			using (Graphics g = Graphics.FromHwnd(Handle))
			{
				width = (int)g.MeasureString(text, fnt).Width + 1;
			}
			return width;
		}

		void SetStyleSheetFor(IStyleSheet site)
		{
			if (m_styleSheet == null)
				SetupStyleSheet();
			if (site != null)
				site.StyleSheet = m_styleSheet;
		}

		IInterlinearTabControl CurrentInterlinearTabControl { get; set; }

		void SetCurrentInterlinearTabControl(IInterlinearTabControl pane)
		{
			CurrentInterlinearTabControl = pane;
			SetupInterlinearTabControlForStText(pane);
		}

		private void SetupInterlinearTabControlForStText(IInterlinearTabControl site)
		{
			InitializeInterlinearTabControl(site);
			//if (site is ISetupLineChoices && m_tabCtrl.SelectedIndex != ktpsCChart)
			if (site is ISetupLineChoices)
			{
				var interlinearView = site as ISetupLineChoices;
				string lineChoicesKey = "InterlinConfig_" + (interlinearView.ForEditing ? "Edit" : "Doc") + "_" + InterlinearTab;
				InterlinLineChoices.InterlinMode mode = GetLineMode();
				interlinearView.SetupLineChoices(lineChoicesKey, mode);
			}
			// Review: possibly need to do SetPaneSizeAndRoot
			if (site != null && site is IChangeRootObject)
			{
				if (site is Control) (site as Control).SuspendLayout();
				(site as IChangeRootObject).SetRoot(RootStTextHvo);
				if (site is Control) (site as Control).ResumeLayout();
			}
		}

		internal InterlinLineChoices.InterlinMode GetLineMode()
		{
			if (m_tabCtrl.SelectedIndex == (int)TabPageSelection.Gloss)
			{
				return m_propertyTable.GetValue(InterlinDocForAnalysis.ksPropertyAddWordsToLexicon, false) ?
					InterlinLineChoices.InterlinMode.GlossAddWordsToLexicon : InterlinLineChoices.InterlinMode.Gloss;
			}
			if (m_tabCtrl.SelectedIndex == (int)TabPageSelection.TaggingView ||
				m_tabCtrl.SelectedIndex == (int)TabPageSelection.ConstituentChart)
				return InterlinLineChoices.InterlinMode.Gloss;
			return InterlinLineChoices.InterlinMode.Analyze;
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			if (m_styleSheet == null)
			{
				SetupStyleSheet();
				if (m_styleSheet != null)
				{
					SetStyleSheetFor(m_tcPane);
					SetStyleSheetFor(CurrentInterlinearTabControl as IStyleSheet);
				}
			}
			base.OnHandleCreated(e);
			// re-select our annotation if we're in the raw text pane, since
			// initialization subsequent to ShowRecord() loses our selection.
			if (m_tabCtrl.SelectedIndex == ktpsRawText)
				this.SelectAnnotation();
		}

		/// <summary>
		/// Override method to add other content to main control.
		/// </summary>
		protected override void AddPaneBar()
		{
			try
			{
				SetupStyleSheet();

				base.AddPaneBar();
			}
			catch (ApplicationException)
			{
				//m_informationBar = new ImageHolder(); //something to show at design time
			}
		}

		protected override void SetInfoBarText()
		{
			if (m_informationBar != null && m_configurationParameters != null)
			{
				string sAltTitle = XmlUtils.GetAttributeValue(m_configurationParameters, "altTitleId");
				if (!String.IsNullOrEmpty(sAltTitle))
				{
					string sTitle = StringTable.Table.GetString(sAltTitle, "AlternativeTitles");
					if (!String.IsNullOrEmpty(sTitle))
					{
						((IPaneBar)m_informationBar).Text = sTitle;
						return;
					}
				}
			}
			base.SetInfoBarText();
		}

		/// <summary>
		/// do any further tabpage related setup based upon the interlinMaster configurationParameters.
		/// </summary>
		/// <param name="configurationParameters">configuration for InterlinMaster</param>
		private void FinishInitTabPages(XmlNode configurationParameters)
		{
			//
			//  Finish defining m_tpRawText.
			//
			bool fEditable = XmlUtils.GetOptionalBooleanAttributeValue(configurationParameters, "editable", true);
			if (!fEditable)
				m_tpRawText.ToolTipText = String.Format(ITextStrings.ksBaseLineNotEditable);
		}

		private void SetupStyleSheet()
		{
			m_styleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
		}

		/// <summary>
		/// Sets m_bookmarks to what is currently selected and persists it.
		/// </summary>
		internal void SaveBookMark()
		{
			CheckDisposed();

			if (m_tabCtrl.SelectedIndex == ktpsInfo || CurrentInterlinearTabControl == null)
				return; // nothing to save...for now, don't overwrite existing one.

			if (RootStText == null)
			{
				return;
			}

			AnalysisOccurrence curAnalysis = null;
			var fSaved = false;
			switch (m_tabCtrl.SelectedIndex)
			{
				case ktpsAnalyze:
					fSaved = SandboxPaneBookmarkSave(m_idcAnalyze, ref curAnalysis);
					break;
				case ktpsGloss:
					fSaved = SandboxPaneBookmarkSave(m_idcGloss, ref curAnalysis);
					break;
				case ktpsCChart:
					if (m_constChartPane == null) // Have added this to designer
						return; // e.g., right after creating a new database, when previous one was open in chart pane.
					// Call CChart.GetUnchartedWordForBookmark() by reflection to see where the chart
					// thinks the bookmark should be.
					var type = m_constChartPane.GetType();
					var info = type.GetMethod("GetUnchartedWordForBookmark");
					Debug.Assert(info != null);
					curAnalysis = (AnalysisOccurrence)info.Invoke(m_constChartPane, null);
					break;
				case ktpsTagging:
					if (m_taggingPane != null)
						curAnalysis = m_taggingPane.OccurrenceContainingSelection();
					break;

				case ktpsPrint:
					if (m_printViewPane != null)
						curAnalysis = m_printViewPane.OccurrenceContainingSelection();
					break;

				case ktpsRawText:
					// Find the analysis we were working on.
					if (m_rtPane != null)
					{
						if (SaveBookmarkFromRootBox(m_rtPane.RootBox))
							return;
					}
					break;
				default:
					Debug.Fail("Unhandled tab index.");
					break;
			}
			if (curAnalysis == null || !curAnalysis.IsValid)
				// This result means the Chart doesn't want to save a bookmark,
				// or that something else went wrong (e.g., we couldn't make a bookmark because we just deleted the text).
				return;

			if (!fSaved)
			{
				InterAreaBookmark mark;
				if(m_bookmarks.TryGetValue(new Tuple<string, Guid>(CurrentTool, RootStText.Guid), out mark))
				{
					//We only want to persist the save if we are in the interlinear edit, not the concordance view
					mark.Save(curAnalysis, CurrentTool.Equals("interlinearTexts"), IndexOfTextRecord);
				}
				else
				{
					mark = new InterAreaBookmark(this, Cache, m_propertyTable);
					mark.Restore(IndexOfTextRecord);
					m_bookmarks.Add(new Tuple<string, Guid>(CurrentTool, RootStText.Guid), mark);
				}
			}
		}

		/// <summary>
		/// Returns true if it already saved a bookmark (from RootBox), false otherwise.
		/// </summary>
		/// <param name="pane"></param>
		/// <param name="curAnalysis">ref var comes out with the location to be saved.</param>
		/// <returns></returns>
		private bool SandboxPaneBookmarkSave(InterlinDocForAnalysis pane, ref AnalysisOccurrence curAnalysis)
		{
			if (pane == null) // Can this really happen? Perhaps if !m_fullyinitialized?
			{
				if (m_rtPane != null) // Not the one, but the other? Odd.
				{
					if (SaveBookmarkFromRootBox(m_rtPane.RootBox))
						return true;
				}
			}
			else
				curAnalysis = pane.OccurrenceContainingSelection();
			return false;
		}

		private bool SaveBookmarkFromRootBox(IVwRootBox rb)
		{
			if (rb == null || rb.Selection == null)
				return false;
			// There may be pictures in the text, and the selection may be on a picture or its
			// caption.  Therefore, getting the TextSelInfo is not enough.  See LT-7906.
			// Unfortunately, the bookmark for a picture or its caption can only put the user
			// back in the same paragraph, it can't fully reestablish the exact same position.
			var iPara = -1;
			var helper = SelectionHelper.GetSelectionInfo(rb.Selection, rb.Site);
			var ichAnchor = helper.IchAnchor;
			var ichEnd = helper.IchEnd;
			var hvoParaAnchor = 0;
			var hvoParaEnd = 0;
			var sliAnchor = helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
			var sliEnd = helper.GetLevelInfo(SelectionHelper.SelLimitType.End);
			if (sliAnchor.Length != sliEnd.Length)
				ichEnd = ichAnchor;
			for (var i = 0; i < sliAnchor.Length; ++i)
			{
				if (sliAnchor[i].tag == StTextTags.kflidParagraphs)
				{
					hvoParaAnchor = sliAnchor[i].hvo;
					break;
				}
			}
			for (var i = 0; i < sliEnd.Length; ++i)
			{
				if (sliEnd[i].tag != StTextTags.kflidParagraphs)
					continue;
				hvoParaEnd = sliEnd[i].hvo;
				break;
			}
			if (hvoParaAnchor != 0)
			{
				IStTxtPara para = null;
				if (Cache.ServiceLocator.GetInstance<IStTxtParaRepository>().TryGetObject(hvoParaAnchor, out para))
				{
					iPara = para.IndexInOwner;
					if (hvoParaAnchor != hvoParaEnd)
						ichEnd = ichAnchor;
					if (ichAnchor == -1)
						ichAnchor = 0;
					if (ichEnd == -1)
						ichEnd = 0;
					if (iPara == ((IStText)para.Owner).ParagraphsOS.Count - 1 && ichAnchor == ichEnd && ichAnchor == para.Contents.Length)
					{
						// Special case, IP at the very end, we probably just typed it or pasted it, select the FIRST word of the text.
						// FWR-723.
						iPara = 0;
						ichAnchor = ichEnd = 0;
					}
				}
			}
			if (iPara >= 0)
			{
				//if there is a bookmark for this text with this tool, then save it, if not some logic error brought us here,
				//but simply not saving a bookmark which doesn't exist seems better than crashing. naylor 3/2012
				var key = new Tuple<string, Guid>(CurrentTool, RootStText.Guid);
				if (m_bookmarks.ContainsKey(key))
				{
					m_bookmarks[key].Save(IndexOfTextRecord, iPara, Math.Min(ichAnchor, ichEnd), Math.Max(ichAnchor, ichEnd), true);
				}

				return true;
			}
			return false;
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (m_styleSheet == null)
				return;		// cannot display properly without style sheet, so don't try.

			// LT-10995: the TitleContentsPane m_tcPane and the TabControl m_tabCtrl used to be
			// docked (definition in InterlinMaster.resx). However, this led to problems if the
			// font size of displayed data got changed. So we are doing the layout ourselves now:
			m_tabCtrl.Width = this.Width; // tab control width = container width

			if (m_tcPane == null)
			{
				// If there is no TitleContentsPane then the TabControl needs to occupy the
				// entire container:
				m_tabCtrl.Location = new Point(0, 0);
				m_tabCtrl.Height = this.Height;
			}
			else
			{
				// If there is a TitleContentsPane then it needs to be at the top of the
				// container, match the container's width, and have its height calculated
				// automatically:
				m_tcPane.Location = new Point(0,0);
				m_tcPane.Width = this.Width;
				m_tcPane.AdjustHeight();

				// And then the TabControl needs to fill the rest of the container below
				// the TitleContentsPane:
				m_tabCtrl.Location = new Point(0, m_tcPane.Height);
				m_tabCtrl.Height = this.Height - m_tcPane.Height;
			}

			base.OnLayout(levent);
		}

		public void OnPropertyChanged(string name)
		{
			CheckDisposed();

			switch (name)
			{
				case "InterlinearTab":
					if (m_tabCtrl.SelectedIndex != (int)InterlinearTab)
						ShowTabView();
					break;
				case "ShowMorphBundles":
					// This helps make sure the notification gets through even if the pane isn't
					// in focus (maybe the Sandbox or TC pane is) and so isn't an xCore target.
					break;
			}
		}

		/// <summary>
		/// Enable if there's anything to select.  This is needed so that the toolbar button is
		/// disabled when there's nothing to look up.  Otherwise, crashes can result when it's
		/// clicked but there's nothing there to process!  It's misleading to the user if
		/// nothing else.  We leave the button visible so that the user doesn't get nauseated
		/// from the buttons appearing and disappearing rapidly.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns>true</returns>
		public bool OnDisplayLexiconLookup(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Visible = true;
			if (m_tabCtrl.SelectedIndex != ktpsRawText)
				display.Enabled = false;
			else
			{
				//LT-6904 : exposed this case where the m_rtPane was null
				// (another case of toolbar processing being done at an unepxected time)
				if (m_rtPane == null)
					display.Enabled = false;
				else
					display.Enabled = m_rtPane.LexiconLookupEnabled();
			}
			return true;
		}

		private int RootStTextHvo
		{
			get
			{
				CheckDisposed();
				return RootStText != null ? RootStText.Hvo : 0;
			}
		}

		/// <remarks>virtual for tests</remarks>
		internal protected virtual IStText RootStText { get; private set; }

		internal int TextListFlid
		{
			get
			{
				CheckDisposed();
				return Clerk.VirtualFlid;
			}
		}

		internal int TextListIndex
		{
			get
			{
				CheckDisposed();
				return Clerk.CurrentIndex;
			}
		}

		bool m_fInShowTabView = false;
		protected void ShowTabView()
		{
			SaveWorkInProgress();
			m_fInShowTabView = true;
			try
			{
				m_tabCtrl.SelectedIndex = (int)InterlinearTab; // set the persisted tab setting.
				if (m_tabCtrl.SelectedIndex == ktpsCChart && m_constChartPane == null)
				{
					// This is the first time on this tab, do lazy creation
					CreateCChart();
				}
				RefreshPaneBar();
				// search through the current tab page controls until we find one implementing IInterlinearTabControl
				var currentTabControl = FindControls<IInterlinearTabControl>(m_tabCtrl.SelectedTab.Controls).FirstOrDefault();
				SetCurrentInterlinearTabControl(currentTabControl as IInterlinearTabControl);
				if (CurrentInterlinearTabControl == null)
					return; // nothing to show.
				switch (m_tabCtrl.SelectedIndex)
				{
					case ktpsRawText:
						if (ParentForm == Form.ActiveForm)
							m_rtPane.Focus();
						if (m_rtPane.RootBox != null && m_rtPane.RootBox.Selection == null && RootStText != null)
							m_rtPane.RootBox.MakeSimpleSel(true, false, false, true);
						break;
					case ktpsCChart:
						if (RootStText == null)
							m_constChartPane.Enabled = false;
						else
						{
							// LT-7733 Warning dialog for Text Chart
							XCore.XMessageBoxExManager.Trigger("TextChartNewFeature");
							m_constChartPane.Enabled = true;
						}
						//SetConstChartRoot(); should be done above in SetCurrentInterlinearTabControl()
						if (ParentForm == Form.ActiveForm)
							m_constChartPane.Focus();
						break;
					case ktpsInfo:
						//We may already be initialized, but this is not very expensive and sometimes
						//the infoPane was initialized with no data and should be re-initialized here
						m_infoPane.Initialize(Cache, m_mediator, m_propertyTable, Clerk);
						m_infoPane.Dock = DockStyle.Fill;

						m_infoPane.Enabled = m_infoPane.CurrentRootHvo != 0;
						if (m_infoPane.Enabled)
						{
							m_infoPane.BackColor = System.Drawing.SystemColors.Control;
							if (ParentForm == Form.ActiveForm)
								m_infoPane.Focus();
						}
						else
						{
							m_infoPane.BackColor = System.Drawing.Color.White;
						}
						break;
					default:
						break;
				}
				SelectAnnotation();
				UpdateContextHistory();
			}
			finally
			{
				m_fInShowTabView = false;
			}
		}

		private void CreateCChart()
		{
			m_constChartPane = (InterlinDocChart)DynamicLoader.CreateObject("Discourse.dll",
																		"SIL.FieldWorks.Discourse.ConstituentChart",
																		new object[] { Cache });
			SetupChartPane();
			m_tpCChart.Controls.Add(m_constChartPane);
			if (m_styleSheet != null)
				m_styleSheet = ((IStyleSheet)m_constChartPane).StyleSheet;
		}

		private void SetupChartPane()
		{
			(m_constChartPane as IxCoreColleague).Init(m_mediator, m_propertyTable, m_configurationParameters);
			m_constChartPane.BackColor = System.Drawing.SystemColors.Window;
			m_constChartPane.Name = "m_constChartPane";
			m_constChartPane.Dock = DockStyle.Fill;
		}

		/// <summary>
		/// Finds the controls implementing TInterfaceMatch
		/// </summary>
		/// <typeparam name="TInterfaceMatch"></typeparam>
		/// <param name="controls"></param>
		/// <returns></returns>
		private static IEnumerable<Control> FindControls<TInterfaceMatch>(ControlCollection controls)
		{
			foreach (Control c in controls)
			{
				if (c is TInterfaceMatch)
					yield return c;

				foreach (var c2 in FindControls<TInterfaceMatch>(c.Controls))
					yield return c2;
			}
		}

		private void RefreshPaneBar()
		{
			// if we're in the context of a PaneBar, refresh the bar so the menu items will
			// reflect the current tab.
			if (MainPaneBar != null && MainPaneBar is UserControl && (MainPaneBar as UserControl).Parent is PaneBarContainer)
				((MainPaneBar as UserControl).Parent as PaneBarContainer).RefreshPaneBar();
		}

		/// <summary>
		/// Determine whether we need to parse any of the texts paragraphs.
		/// </summary>
		/// <param name="stText"></param>
		/// <returns></returns>
		public static bool HasParagraphNeedingParse(IStText stText)
		{
			return stText.ParagraphsOS.Cast<IStTxtPara>().Any(para => !para.ParseIsCurrent);
		}

		/// <summary>
		/// todo: add progress bar.
		/// typically a delegate for NonUndoableUnitOfWorkHelper
		/// </summary>
		/// <param name="stText">
		/// <param name="forceParse">
		/// </param>
		public static void LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(IStText stText, bool forceParse)
		{
			if (stText == null)
				return;

			using (var pp = new ParagraphParser(stText.Cache))
			{
				if (forceParse)
				{
					foreach (var para in stText.ParagraphsOS.Cast<IStTxtPara>())
					{
						pp.ForceParse(para);
					}
				}
				else
				{
					foreach (var para in stText.ParagraphsOS.Cast<IStTxtPara>().Where(
						para => !para.ParseIsCurrent))
					{
						pp.Parse(para);
					}
				}
			}
			var services = new AnalysisGuessServices(stText.Cache);
			services.GenerateEntryGuesses(stText);
		}

		/// <summary>
		/// Required override for RecordView subclass.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="configurationParameters"></param>
		public override void Init(Mediator mediator, IPropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();

			// Do this BEFORE calling InitBase, which calls ShowRecord, whose correct behavior
			// depends on the suppressAutoCreate flag.
			bool fHideTitlePane = XmlUtils.GetBooleanAttributeValue(configurationParameters, "hideTitleContents");
			if (fHideTitlePane)
			{
				// When used as the third pane of a concordance, we don't want the
				// title/contents stuff.
				m_tcPane.Visible = false;
			}
			m_fSuppressAutoCreate = XmlUtils.GetBooleanAttributeValue(configurationParameters,
				"suppressAutoCreate");

			// InitBase will do this, but we need it in place for testing IsPersistedForAnInterlinearTabPage.
			m_mediator = mediator;
			// InitBase will do this, but we need it in place before calling SetInitialTabPage().
			m_propertyTable = propertyTable;

			// Making the tab control currently requires this first...
			if (!fHideTitlePane)
			{
				m_tcPane.StyleSheet = m_styleSheet;
				m_tcPane.Visible = true;
			}
			FinishInitTabPages(configurationParameters);
			SetInitialTabPage();
			m_currentTool = configurationParameters.Attributes["clerk"].Value;
			// Do NOT do this, it raises an exception.
			//base.Init (mediator, configurationParameters);
			// Instead do this.
			InitBase(mediator, propertyTable, configurationParameters);
			m_fullyInitialized = true;
			RefreshPaneBar();
		}

		/// <summary>
		/// Set the appropriate tab index BEFORE calling InitBase, since that calls
		/// RecordView.InitBase, which calls ShowRecord, which calls ShowTabView,
		/// which will unnecessarily create the wrong pane, if the tab index is wrong.
		/// </summary>
		private void SetInitialTabPage()
		{
			// If the Record Clerk has remembered we're IsPersistedForAnInterlinearTabPage,
			// and we haven't already switched to that tab page, do so now.
			if (this.Visible && m_tabCtrl.SelectedIndex != (int)InterlinearTab)
			{
				// Switch to the persisted tab page index.
				m_tabCtrl.SelectedIndex = (int)InterlinearTab;
			}
			else
			{
				m_tabCtrl.SelectedIndex = ktpsRawText;
			}
		}

		/// <summary>
		/// From IxCoreContentControl
		/// </summary>
		/// <returns>true if ok to go away</returns>
		public override bool PrepareToGoAway()
		{
			CheckDisposed();
			SaveBookMark();
			if (!SaveWorkInProgress()) return false;
			return base.PrepareToGoAway();
		}

		private bool SaveWorkInProgress()
		{
			if (m_idcAnalyze != null && m_idcAnalyze.Visible &&  !m_idcAnalyze.PrepareToGoAway())
				return false;
			if (m_idcGloss != null && m_idcGloss.Visible && !m_idcGloss.PrepareToGoAway())
				return false;
			return true;
		}

		public bool OnPrepareToRefresh(object args)
		{
			CheckDisposed();

			// flag that a refresh was triggered (unless we don't have a current record..see var comment).
			if (RootStTextHvo != 0)
				m_fRefreshOccurred = true;
			return false; // other things may wish to prepare too.
		}

		protected override void SetupDataContext()
		{
			base.SetupDataContext();
			InitializeInterlinearTabControl(m_tcPane);
			InitializeInterlinearTabControl(CurrentInterlinearTabControl);
		}


		private void InitializeInterlinearTabControl(IInterlinearTabControl site)
		{
			if (site != null)
			{
				SetStyleSheetFor(site as IStyleSheet);
				site.Cache = Cache;
				if (site is IxCoreColleague)
					(site as IxCoreColleague).Init(m_mediator, m_propertyTable, m_configurationParameters);
			}
		}

		/// <summary>
		/// The record index of the currently selected text.
		/// </summary>
		internal int IndexOfTextRecord
		{
			get
			{
				CheckDisposed();

				if (Clerk.CurrentObjectHvo != 0 && Cache.ServiceLocator.IsValidObjectId(Clerk.CurrentObjectHvo))
				{
					if (Clerk.CurrentObject.ClassID == StTextTags.kClassId)
						return Clerk.CurrentIndex;
				}

				return -1;
			}
		}

		internal string TitleOfTextRecord
		{
			get
			{
				CheckDisposed();

				if (Clerk.CurrentObject != null)
				{
					var rootObj = Clerk.CurrentObject;
					if (rootObj.ClassID == TextTags.kClassId)
					{
						var text = rootObj as FDO.IText;
						return text.Name.AnalysisDefaultWritingSystem.Text;
					}
				}
				return string.Empty;
			}
		}

		protected override void ShowRecord(RecordNavigationInfo rni)
		{
			base.ShowRecord(rni);

			// independent of whether base.ShowRecord(rni) skips ShowRecord()
			// we still want to try to put the focus in our control.
			// (but only if we're in the active window -- see FWR-1795)
			if (ParentForm == Form.ActiveForm)
				this.Focus();
		}

		/// <summary>
		/// This is an attempt to improve scrolling by mouse wheel, by passing on focus when the interlin master gets it.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnGotFocus(EventArgs e)
		{
			if (m_tabCtrl.SelectedTab != null && m_tabCtrl.SelectedTab.Controls[0].CanFocus)
				m_tabCtrl.SelectedTab.Controls[0].Focus();
		}

		/// <summary>
		/// Save any intermediate analysis information on validation requests
		/// note: this is triggered before a Send/Receive operation
		/// </summary>
		/// <param name="e"></param>
		protected override void OnValidating(System.ComponentModel.CancelEventArgs e)
		{
			base.OnValidating(e);
			SaveWorkInProgress();
		}

		protected override void ShowRecord()
		{
			SaveWorkInProgress();
			base.ShowRecord();
			if (Clerk.SuspendLoadingRecordUntilOnJumpToRecord)
				return;
			//This is our very first time trying to show a text, if possible we would like to show the stored text.
			if (m_bookmarks == null)
			{
				m_bookmarks = new Dictionary<Tuple<string, Guid>, InterAreaBookmark>();
			}

			// It's important not to do this if there is a filter, as there's a good chance the new
			// record doesn't pass the filter and we get into an infinite loop. Also, if the user
			// is filtering, he probably just wants to see that there are no matching texts, not
			// make a new one.
			if (Clerk is InterlinearTextsRecordClerk &&
				Clerk.CurrentObjectHvo == 0 && !m_fSuppressAutoCreate && !Clerk.ShouldNotModifyList
				&& Clerk.Filter == null)
			{
				// This is needed in SwitchText(0) to avoid LT-12411 when in Info tab.
				// We'll get a chance to do it later.
				Clerk.SuppressSaveOnChangeRecord = true;
				// first clear the views of their knowledge of the previous text.
				// otherwise they could crash trying to access information that is no longer valid. (LT-10024)
				SwitchText(0);

				// Presumably because there are none..make one.
				// This is invisible to the user so it should not be undoable; that is particularly
				// important if the most recent action was to delete the last text, which will
				// not be undoable if we are now showing 'Undo insert text'.
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					// We don't want to force a Save here if we just deleted the last text;
					// we want to be able to Undo deleting it!
					var options = new RecordClerk.ListUpdateHelper.ListUpdateHelperOptions();
					options.SuppressSaveOnChangeRecord = true;
					using (new RecordClerk.ListUpdateHelper(Clerk, options))
						((InterlinearTextsRecordClerk)Clerk).AddNewTextNonUndoable();
				});
			}
			if (Clerk.CurrentObjectHvo == 0)
			{
				SwitchText(0);		// We no longer have a text.
				return;				// We get another call when there is one.
			}
			var hvoRoot = Clerk.CurrentObjectHvo;
			if (Clerk.CurrentObjectHvo != 0 && !Cache.ServiceLocator.IsValidObjectId(Clerk.CurrentObjectHvo))	// RecordClerk is tracking an analysis
			{
				// This pane, as well as knowing how to work with a record list of Texts, knows
				// how to work with one of fake objects in a concordance, that is, a list of occurrences of
				// a word.
				hvoRoot = SetConcordanceBookmarkAndReturnRoot(hvoRoot);
			}
			else
			{
				var stText = Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(hvoRoot);
				if (stText.ParagraphsOS.Count == 0)
				{
					NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
						((InterlinearTextsRecordClerk)Clerk).CreateFirstParagraph(stText, Cache.DefaultVernWs));
				}
				if (stText.ParagraphsOS.Count == 1 && ((IStTxtPara)stText.ParagraphsOS[0]).Contents.Length == 0)
				{
					// If we have restarted FLEx since this text was created, the WS has been lost and replaced with the global default of English.
					// If this is the case, default to the Default Vernacular WS (LT-15688)
					var globalDefaultWs = Cache.ServiceLocator.WritingSystemManager.Get("en").Handle;
					if(stText.MainWritingSystem == globalDefaultWs)
					{
						NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
							((IStTxtPara)stText.ParagraphsOS[0]).Contents = TsStringUtils.MakeTss(string.Empty, Cache.DefaultVernWs));
					}

					// since we have no text, we should not sit on any of the analyses tabs,
					// the info tab is still useful though.
					if (InterlinearTab != TabPageSelection.Info && InterlinearTab != TabPageSelection.RawText)
					{
						InterlinearTab = TabPageSelection.RawText;
					}
					// Don't steal the focus from another window.  See FWR-1795.
					if (ParentForm == Form.ActiveForm)
						m_rtPane.Focus();
				}
				if (RootStText == null || RootStText.Hvo != hvoRoot)
				{
					// we've just now entered the area, so try to restore a bookmark.
					CreateOrRestoreBookmark(stText);
				}
			}

			if ((RootStText == null || RootStText.Hvo != hvoRoot) &&
				Cache.ServiceLocator.IsValidObjectId(hvoRoot))
			{
				SwitchText(hvoRoot); // sets RootStText
			}
			else
			{
				SelectAnnotation(); // select an annotation in the current text.
			}

			// This takes a lot of time, and the view is never visible by now, and it gets done
			// again when made visible! So don't do it!
			//m_idcPane.SetRoot(hvoRoot);

			// If we're showing the raw text pane make sure it has a selection.
			if (Controls.IndexOf(m_rtPane) >= 0 && m_rtPane.RootBox.Selection == null)
				m_rtPane.RootBox.MakeSimpleSel(true, false, false, true);

			UpdateContextHistory();
			m_fRefreshOccurred = false;	// reset our flag that a refresh occurred.
		}

		private int SetConcordanceBookmarkAndReturnRoot(int hvoRoot)
		{
			if (!CurrentTool.Equals("interlinearTexts"))
			{
				var occurrenceFromHvo = (Clerk as IAnalysisOccurrenceFromHvo).OccurrenceFromHvo(Clerk.CurrentObjectHvo);
				var point = occurrenceFromHvo != null ? occurrenceFromHvo.BestOccurrence : null;
				if (point != null && point.IsValid)
				{
					var para = point.Segment.Paragraph;
					hvoRoot = para.Owner.Hvo;
				}
				ICmObject text;
				Cache.ServiceLocator.ObjectRepository.TryGetObject(hvoRoot, out text);
				if (!m_fRefreshOccurred && m_bookmarks != null && text != null)
				{
					InterAreaBookmark mark;
					if (!m_bookmarks.TryGetValue(new Tuple<string, Guid>(CurrentTool, text.Guid), out mark))
					{
						mark = new InterAreaBookmark(this, Cache, m_propertyTable);
						m_bookmarks.Add(new Tuple<string, Guid>(CurrentTool, text.Guid), mark);
					}

					mark.Save(point, false, IndexOfTextRecord);
				}
			}
			return hvoRoot;
		}

		/// <summary>
		/// Restore the bookmark, or create a new one, but only if we are in the correct area
		/// </summary>
		/// <param name="stText"></param>
		private void CreateOrRestoreBookmark(IStText stText)
		{
			if (stText != null)
			{
				InterAreaBookmark mark;
				if (m_bookmarks.TryGetValue(new Tuple<string, Guid>(CurrentTool, stText.Guid), out mark))
				{
					mark.Restore(IndexOfTextRecord);
				}
				else
				{
					m_bookmarks.Add(new Tuple<string, Guid>(CurrentTool, stText.Guid), new InterAreaBookmark(this, Cache, m_propertyTable));
				}
			}
		}

		private void SwitchText(int hvoRoot)
		{
			// We've switched text, so clear the Undo stack redisplay it.
			// This method will clear the Undo stack UNLESS we're changing record
			// because we inserted or deleted one, which ought to be undoable.
			Clerk.SaveOnChangeRecord();
			SaveBookMark();
			if (hvoRoot != 0)
				RootStText = Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(hvoRoot);
			else
				RootStText = null;
			// one way or another it's the Text by now.
			ShowTabView();
			// I (JohnT) no longer know why we need to update the TC pane (but not any of the others?) even if it
			// is not the current pane. But it IS essential to do so AFTER updating the current one.
			// When deleting the last text, the following call can result in a resize call to the current (e.g., analysis)
			// pane, and if ShowTabView has not already cleared the current pane's knowledge of the deleted text,
			// we can get a crash (e.g., second stack in LT-12401).
			if (m_tcPane != null)
				SetupInterlinearTabControlForStText(m_tcPane);
		}

		// If the Clerk's object is an annotation, select the corresponding thing in whatever pane
		// is active. Or, if we have a bookmark, restore it.
		private void SelectAnnotation()
		{
			if (Clerk.CurrentObjectHvo == 0 || Clerk.SuspendLoadingRecordUntilOnJumpToRecord)
				return;
			// Use a bookmark, if we've set one.
			if (RootStText != null && m_bookmarks.ContainsKey(new Tuple<string, Guid>(CurrentTool, RootStText.Guid)) &&
				m_bookmarks[new Tuple<string, Guid>(CurrentTool, RootStText.Guid)].IndexOfParagraph >= 0 && CurrentInterlinearTabControl is IHandleBookmark)
			{
				(CurrentInterlinearTabControl as IHandleBookmark).SelectBookmark(m_bookmarks[new Tuple<string, Guid>(CurrentTool, RootStText.Guid)]);
			}
		}

		/// <summary>
		/// Most Interlinear document logic in InterlinMaster can be shared between
		/// the these tab pages (Analyze (Interlinearizer) and Gloss).
		/// So you can use this to test whether our tab control is in one of those tabs.
		/// </summary>
		/// <returns></returns>
		internal bool InterlinearTabPageIsSelected()
		{
			return m_tabCtrl.SelectedIndex == ktpsAnalyze ||
					m_tabCtrl.SelectedIndex == ktpsGloss;
		}

		/// <summary>
		/// Make the subpanes message targets, especially so the interlin doc child can enable
		/// the Insert Free Translation menu items.
		/// </summary>
		/// <returns></returns>
		protected override void GetMessageAdditionalTargets(List<IxCoreColleague> collector)
		{
			if (CurrentInterlinearTabControl != null && CurrentInterlinearTabControl is IxCoreColleague)
				collector.Add(CurrentInterlinearTabControl as IxCoreColleague);
			collector.Add(this);
		}

		#region free translation stuff

		public bool OnDisplayFindAndReplaceText(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			string toolName = m_propertyTable.GetValue("currentContentControl", "");
			bool fVisible = m_rtPane != null && (m_tabCtrl.SelectedIndex == (int)TabPageSelection.RawText) && InFriendlyArea
				&& toolName != "wordListConcordance";
			display.Visible = fVisible;

			if (fVisible && m_rtPane.RootBox != null)
			{
				int hvoRoot, frag;
				IVwViewConstructor vc;
				IVwStylesheet ss;
				m_rtPane.RootBox.GetRootObject(out hvoRoot, out vc, out frag, out ss);
				display.Enabled = hvoRoot != 0;
			}
			else
				display.Enabled = false;

			// Although it's a modal dialog, it's dangerous for it to be visible in contexts where it
			// could not be launched, presumably because it doesn't apply to that view, and may do
			// something dangerous to another view (cf LT-7961).
			IApp app = m_propertyTable.GetValue<IApp>("App");
			if (app != null && !display.Enabled)
				app.RemoveFindReplaceDialog();
			return true;
		}

		public void OnFindAndReplaceText(object argument)
		{
			CheckDisposed();

			IApp app = m_propertyTable.GetValue<IApp>("App");
			if (app != null)
				app.ShowFindReplaceDialog(false, m_rtPane);
		}

		/// <summary>
		/// Replace is enabled exactly when Find and Replace is.
		/// </summary>
		public bool OnDisplayReplaceText(object commandObject, ref UIItemDisplayProperties display)
		{
			return OnDisplayFindAndReplaceText(commandObject, ref display);
		}

		public void OnReplaceText(object argument)
		{
			CheckDisposed();

			IApp app = m_propertyTable.GetValue<IApp>("App");
			if (app != null)
				app.ShowFindReplaceDialog(true, m_rtPane);
		}

		/// <summary>
		/// Enable the "Add Note" command if the idcPane is visible and wants to do it.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayAddNote(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = display.Visible = InterlinearTabPageIsSelected();
			return true;
		}


		/// <summary>
		/// Delegate this command to the idcPane. (It isn't enabled unless one exists.)
		/// </summary>
		/// <param name="argument"></param>
		public void OnAddNote(object argument)
		{
			CheckDisposed();
			var command = argument as Command;
			if (m_idcAnalyze != null && m_idcAnalyze.Visible)
				m_idcAnalyze.AddNote(command);
			else if (m_idcGloss != null && m_idcGloss.Visible)
				m_idcGloss.AddNote(command);
		}

		#endregion

		/// <summary>
		/// Gets/Sets the property table state for the selected tab page.
		/// </summary>
		internal TabPageSelection InterlinearTab
		{
			get
			{
				if (m_mediator == null)
					return TabPageSelection.RawText;
				string val = m_propertyTable.GetValue("InterlinearTab", TabPageSelection.RawText.ToString());
				TabPageSelection tabSelection;
				if (string.IsNullOrEmpty(val))
				{
					// This could be done by just catching the exception, but it's annoying when debugging.
					tabSelection = TabPageSelection.RawText;
					InterlinearTab = tabSelection;
					return tabSelection;
				}
				try
				{
					tabSelection = (TabPageSelection)Enum.Parse(typeof(TabPageSelection), val);
				}
				catch
				{
					tabSelection = TabPageSelection.RawText;
					InterlinearTab = tabSelection;
				}
				return tabSelection;
			}

			set
			{
				m_propertyTable.SetProperty("InterlinearTab", value.ToString(), true, true);
			}
		}

		/// <summary>
		/// Enable the "Configure Interlinear" command. Can be done any time this view is a target.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayConfigureInterlinear(object commandObject,
			ref UIItemDisplayProperties display)
		{
			bool fShouldDisplay = (CurrentInterlinearTabControl != null &&
				CurrentInterlinearTabControl is InterlinDocRootSiteBase &&
				!(CurrentInterlinearTabControl is InterlinDocChart));
			display.Visible = fShouldDisplay;
			display.Enabled = fShouldDisplay;
			return true;
		}

		/// <summary>
		///  Launch the Configure interlinear dialog and deal with the results
		/// </summary>
		/// <param name="argument"></param>
		public bool OnConfigureInterlinear(object argument)
		{
			if (CurrentInterlinearTabControl != null && CurrentInterlinearTabControl is InterlinDocRootSiteBase)
				(CurrentInterlinearTabControl as InterlinDocRootSiteBase).OnConfigureInterlinear(argument);

			return true; // We handled this
		}

		/// <summary>
		/// Use this to determine whether the last selected tab page which was
		/// persisted in the PropertyTable, pertains to an interlinear document.
		/// Currently, two tabs share an interlinear document (Gloss and Interlinearizer (Analyze)).
		/// </summary>
		protected bool IsPersistedForAnInterlinearTabPage
		{
			get
			{
				if (m_mediator == null)
					return false; // apparently not quite setup to determine true or false.
				return InterlinearTab == TabPageSelection.Interlinearizer ||
					InterlinearTab == TabPageSelection.Gloss;
			}
		}

		/// <summary>
		/// create and register a URL describing the current context, for use in going backwards
		/// and forwards
		/// </summary>
		/// <remarks> We need an override in order to store the state of the "mode".</remarks>
		protected override void UpdateContextHistory()
		{
			// are we the dominant pane? The thinking here is that if our clerk is controlling
			// the record tree bar, then we are.
			if (Clerk.IsControllingTheRecordTreeBar)
			{
				//add our current state to the history system
				string toolName = m_propertyTable.GetValue("currentContentControl", "");
				Guid guid = Guid.Empty;
				if (Clerk.CurrentObject != null)
					guid = Clerk.CurrentObject.Guid;
				FdoCache cache = Cache;
				// Not sure what will happen with guid == Guid.Empty on the link...
				FwLinkArgs link = new FwLinkArgs(toolName, guid, InterlinearTab.ToString());
				link.PropertyTableEntries.Add(new Property("InterlinearTab",
					InterlinearTab.ToString()));
				Clerk.SelectedRecordChanged(true, true); // make sure we update the record count in the Status bar.
				var linkListener = m_propertyTable.GetValue<LinkListener>("LinkListener");
				linkListener.OnAddContextToHistory(link);
			}
		}

		/// <summary>
		/// determine if this is the correct place [it's the only one that handles the message, and
		/// it defaults to false, so it should be]
		/// </summary>
		protected bool InFriendlyArea
		{
			get
			{
				string desiredArea = "textsWords";

				// see if it's the right area
				string areaChoice = m_propertyTable.GetValue<string>("areaChoice");
				return areaChoice != null && areaChoice == desiredArea;
			}
		}

		/// <summary>
		/// determine if we're in the (given) tool
		/// </summary>
		/// <param name="desiredTool"></param>
		/// <returns></returns>
		protected bool InFriendlyTool(string desiredTool)
		{
			var toolChoice = m_propertyTable.GetValue<string>("ToolForAreaNamed_textsWords");
			return toolChoice != null && toolChoice == desiredTool;
		}

		/// <summary>
		/// Mode for populating the Lexicon with monomorphemic glosses
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		public bool OnDisplayITexts_AddWordsToLexicon(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			var fCanDisplayAddWordsToLexiconPanelBarButton = InterlinearTab == TabPageSelection.Gloss;
			display.Visible = fCanDisplayAddWordsToLexiconPanelBarButton;
			display.Enabled = fCanDisplayAddWordsToLexiconPanelBarButton;
			return true;
		}


		/// <summary>
		/// ShowHiddenFields for Info tab. We use the suffix interlinearEdit here because it is
		/// the toolName when the data tree is initializing the info tab. The name actually applies to
		/// the whole interlinear view, but fortunately so far only the Info tab contains a data tree.
		/// </summary>
		/// <note>This handles enabling the 'menu item' deffined in the menu PaneBar-ITextContent in the interlinear area configuration file.
		/// The property name is actually ShowHiddenFields-interlinearEdit but a trick in Choice.GetDisplayProperties allows us
		/// to have a valid method name with an underscore in it.
		/// If you are thinking of cleaning up this hack, note that various code in DataTree is aware of a multitude of properties
		/// starting with "ShowHiddenFields-", and they are persisted in settings.</note>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		public bool OnDisplayShowHiddenFields_interlinearEdit(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			var fCanDisplayAddWordsToLexiconPanelBarButton = InterlinearTab == TabPageSelection.Info;
			display.Visible = fCanDisplayAddWordsToLexiconPanelBarButton;
			display.Enabled = fCanDisplayAddWordsToLexiconPanelBarButton;
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be displayed
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayShowMorphBundles(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = display.Visible = InFriendlyArea && InFriendlyTool("interlinearEdit");
			return true; //we've handled this
		}

		private void m_tabCtrl_Selected(object sender, TabControlEventArgs e)
		{
			InterlinearTab = (TabPageSelection)m_tabCtrl.SelectedIndex;

			// If we're just starting up (setting it from saved state) we don't need to do anything to change it.
			if (m_rtPane != null || m_infoPane != null || m_idcGloss != null || m_idcAnalyze != null
				|| m_taggingPane != null || m_printViewPane != null || m_constChartPane != null)
			{
				// In order to prevent crashes caused by PropChanges affecting non-visible tabs (e.g. Undo/Redo, LT-9078),
				// dispose of any existing panes including interlinDoc child based controls,
				// and recreate them as needed. Most of the time is spend Reconstructing a display,
				// not initializing the controls.
				// NOTE: (EricP) tried to dispose of the RawTextPane as well, but for some reason
				// we'd lose our cursor when switching texts or from a different tab. Instead,
				// just dispose of the interlinDocChild panes, since those are the ones that
				// are likely to crash during an intermediate state during Undo/Redo.

				// don't want to re-enter ShowTabView if we're changing the index from there.
				if (!m_fInShowTabView)
					ShowTabView();
			}
		}

		private void m_tabCtrl_Deselecting(object sender, TabControlCancelEventArgs e)
		{
			// Switching tabs, usual precaution against being able to Undo things you can no longer see.
			// When we do this because we're switching objects because we deleted one, and the new
			// object is empty, we're inside the ShowRecord where it is suppressed.
			// If the tool is just starting up (in Init, before InitBase is called), we may not
			// have configuration parameters. Usually, then, there won't be a transaction open,
			// but play safe, because the call to get the Clerk will crash if we don't have configuration
			// params.
			if (m_configurationParameters != null /* && !Cache.DatabaseAccessor.IsTransactionOpen() */)
				Clerk.SaveOnChangeRecord();
			bool fParsedTextDuringSave = false;
			// Pane-individual updates; None did anything, I removed them; GJM
			// Is this where we need to hook in reparsing of segments/paras, etc. if RawTextPane is deselected?
			// No. See DomainImpl.AnalysisAdjuster.

			if (m_bookmarks != null) // This is out here to save bookmarks set in Chart, Print and Edit views too.
			{
				//At this point m_tabCtrl.SelectedIndex is set to the value of the tabPage
				//we are leaving.
				if (RootStText != null && m_bookmarks.ContainsKey(new Tuple<string, Guid>(CurrentTool, RootStText.Guid)))
					SaveBookMark();
			}
		}

		/// <summary>
		/// Required member for IFocusablePanePortion. We must implement this so focus can be set here
		/// through XML configuration of a multipane. But we don't need to do anything because MultiPane
		/// doesn't do anything dangerous when it is not the focused pane of a multipane.
		/// </summary>
		public bool IsFocusedPane { get; set; }
	}

	public interface IHandleBookmark
	{
		/// <summary>
		/// makes a selection in a view given the bookmark location.
		/// </summary>
		/// <param name="bookmark"></param>
		void SelectBookmark(IStTextBookmark bookmark);
	}


	/// <summary>
	/// indicates a position in an StText
	/// </summary>
	public interface IStTextBookmark
	{
		int IndexOfParagraph { get; }
		int BeginCharOffset { get; }
		int EndCharOffset { get; }
	}
}
