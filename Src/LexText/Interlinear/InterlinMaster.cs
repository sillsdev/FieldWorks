using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using XCore;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;
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
	public partial class InterlinMaster : InterlinMasterBase
	{
		// Controls
		IVwStylesheet m_styleSheet;
		protected InfoPane m_infoPane; // Parent is m_tpInfo.

		public InterAreaBookmark m_bookmark;
		private bool m_fParsedTextDuringSave = false;
		//private bool m_fSkipNextParse = false;

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
			int width = 0;
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
				return m_mediator.PropertyTable.GetBoolProperty(InterlinDocForAnalysis.ksPropertyAddWordsToLexicon, false) ?
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
					string sTitle = StringTbl.GetString(sAltTitle, "AlternativeTitles");
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
			m_styleSheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
		}

		#region IxCoreCtrlTabProvider implementation

		//public override Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		//{
		//	if (targetCandidates == null)
		//		throw new ArgumentNullException("'targetCandidates' is null.");

		//	Control ctrlHasFocus = this;
		//	if (m_tcPane != null && m_tcPane.Visible && !m_tcPane.ReadOnlyView)
		//	{
		//		targetCandidates.Add(m_tcPane);
		//		if (m_tcPane.ContainsFocus)
		//			ctrlHasFocus = m_tcPane;
		//	}
		//	if (m_tabCtrl != null)
		//	{
		//		if (m_tabCtrl.ContainsFocus)
		//			ctrlHasFocus = m_tabCtrl;
		//		targetCandidates.Add(m_tabCtrl);
		//	}
		//	return ContainsFocus ? ctrlHasFocus : null;
		//}

		#endregion  IxCoreCtrlTabProvider implementation

		/// <summary>
		/// Sets m_bookmark to what is currently selected and persists it.
		/// </summary>
		internal void SaveBookMark()
		{
			CheckDisposed();

			if (m_tabCtrl.SelectedIndex == ktpsInfo || CurrentInterlinearTabControl == null)
				return; // nothing to save...for now, don't overwrite existing one.

			if (RootStText == null)
			{
				// No text active, so nothing to save here, just reset.
				m_bookmark.Reset();
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
					if (curAnalysis == null || !curAnalysis.IsValid)
						// This result means the Chart doesn't want to save a bookmark
						return;
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

			if (!fSaved)
				m_bookmark.Save(curAnalysis, true);
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
				var para = Cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(hvoParaAnchor);
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
			if (iPara >= 0)
			{
				m_bookmark.Save(iPara, Math.Min(ichAnchor, ichEnd), Math.Max(ichAnchor, ichEnd), true);
				return true;
			}
			return false;
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (m_styleSheet == null)
				return;		// cannot display properly without style sheet, so don't try.
			base.OnLayout(levent);
			if (m_tcPane != null)
			{
				// we adjust the height of the title pane after we layout, so that it can use the correct width
				if (m_tcPane.AdjustHeight())
					// if the title pane changed height, we need to relayout
					base.OnLayout(levent);
			}
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

		internal protected IStText RootStText { get; private set; }

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
						if (!m_infoPane.IsInitialized)
						{
							m_infoPane.Initialize(Cache, m_mediator, Clerk);
							m_infoPane.Dock = DockStyle.Fill;
						}
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
			(m_constChartPane as IxCoreColleague).Init(m_mediator, m_configurationParameters);
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
		/// <param name="configurationParameters"></param>
		public override void Init(XCore.Mediator mediator,
			XmlNode configurationParameters)
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

			// Making the tab control currently requires this first...
			if (!fHideTitlePane)
			{
				m_tcPane.StyleSheet = m_styleSheet;
				m_tcPane.Visible = true;
			}
			FinishInitTabPages(configurationParameters);
			SetInitialTabPage();
			// Do NOT do this, it raises an exception.
			//base.Init (mediator, configurationParameters);
			// Instead do this.
			InitBase(mediator, configurationParameters);
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

			// let's save our position before going further. Otherwise we might loose our annotation/analysis information altogether.
			// even if PrepareToGoAway creates a new analysis or annotation, it still should be at the same location.
			this.SuspendLayout();

			//LT-6904 : exposed this case where the m_bookmark is null
			if (m_bookmark != null)
				m_bookmark.Save();
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

		/// <summary>
		/// Determine if this is the correct place for handling the 'New Text' command.
		/// NOTE: in PrepareToGoAway(), the mediator may have been switched to a new area.
		/// </summary>
		internal bool InTextsArea
		{
			get
			{
				CheckDisposed();

				const string desiredArea = "textsWords";
				var areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice",
					null);
				return areaChoice != null && areaChoice == desiredArea;
			}
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
					(site as IxCoreColleague).Init(m_mediator, m_configurationParameters);
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

		protected override void ShowRecord()
		{
			SaveWorkInProgress();
			base.ShowRecord();
			if (Clerk.SuspendLoadingRecordUntilOnJumpToRecord)
				return;
			if (m_bookmark == null)
				m_bookmark = new InterAreaBookmark(this, m_mediator, Cache);

			// It's important not to do this if there is a filter, as there's a good chance the new
			// record doesn't pass the filter and we get into an infinite loop. Also, if the user
			// is filtering, he probably just wants to see that there are no matching texts, not
			// make a new one.
			if (Clerk is InterlinearTextsRecordClerk &&
				Clerk.CurrentObjectHvo == 0 && !m_fSuppressAutoCreate && !Clerk.ShouldNotModifyList
				&& Clerk.Filter == null)
			{
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
						(Clerk as InterlinearTextsRecordClerk).AddNewTextNonUndoable();
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
				if (Clerk is IAnalysisOccurrenceFromHvo)
				{
					var point = (Clerk as IAnalysisOccurrenceFromHvo).OccurrenceFromHvo(Clerk.CurrentObjectHvo).BestOccurrence;
					if (!m_fRefreshOccurred)
						m_bookmark.Save(point, false);
					if (point != null && point.IsValid)
					{
						var para = point.Segment.Paragraph;
						hvoRoot = para.Owner.Hvo;
					}
				}
			}
			else
			{
				var stText = Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(hvoRoot);
				if (stText.ParagraphsOS.Count == 0)
				{
					NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
						(Clerk as InterlinearTextsRecordClerk).CreateFirstParagraph(stText, Cache.DefaultVernWs));
				}
				if (stText.ParagraphsOS.Count == 1 && (stText.ParagraphsOS[0] as IStTxtPara).Contents.Length == 0)
				{
					// since we have a new text, we should switch to the Baseline tab.
					// ShowTabView() will adjust the tab control appropriately.
					InterlinearTab = TabPageSelection.RawText;
					// Don't steal the focus from another window.  See FWR-1795.
					if (ParentForm == Form.ActiveForm)
						m_rtPane.Focus();
				}
				if (RootStText == null)
				{
					// we've just now entered the area, so try to restore a bookmark.
					m_bookmark.Restore();
				}
				else if (RootStText.Hvo != hvoRoot)
				{
					// we've switched texts, so reset our bookmark.
					m_bookmark.Reset();
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

		private void SwitchText(int hvoRoot)
		{
			// We've switched text, so clear the Undo stack redisplay it.
			// This method will clear the Undo stack UNLESS we're changing record
			// because we inserted or deleted one, which ought to be undoable.
			Clerk.SaveOnChangeRecord();
			if (hvoRoot != 0)
				RootStText = Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(hvoRoot);
			else
				RootStText = null;
			// one way or another it's the Text by now.
			if (RootStText == null)
				m_bookmark.Reset();
			else if (m_tcPane != null)
				SetupInterlinearTabControlForStText(m_tcPane);
			ShowTabView();
		}

		// If the Clerk's object is an annotation, select the corresponding thing in whatever pane
		// is active. Or, if we have a bookmark, restore it.
		private void SelectAnnotation()
		{
			if (Clerk.CurrentObjectHvo == 0 || Clerk.SuspendLoadingRecordUntilOnJumpToRecord)
				return;
			// Use a bookmark, if we've set one.
			if (m_bookmark.IndexOfParagraph >= 0 && CurrentInterlinearTabControl is IHandleBookmark)
				(CurrentInterlinearTabControl as IHandleBookmark).SelectBookmark(m_bookmark);
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

			string toolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl", "");
			bool fVisible = m_rtPane != null && (m_tabCtrl.SelectedIndex == (int)TabPageSelection.RawText) && InTextsArea
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
			IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
			if (app != null && !display.Enabled)
				app.RemoveFindReplaceDialog();
			return true;
		}

		public void OnFindAndReplaceText(object argument)
		{
			CheckDisposed();

			IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
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

			IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
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
				if (m_mediator == null || m_mediator.PropertyTable == null)
					return TabPageSelection.RawText;
				string val = m_mediator.PropertyTable.GetStringProperty("InterlinearTab", TabPageSelection.RawText.ToString());
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
				m_mediator.PropertyTable.SetProperty("InterlinearTab", value.ToString());
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
				if (m_mediator == null || m_mediator.PropertyTable == null)
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
				string toolName =
					m_mediator.PropertyTable.GetStringProperty("currentContentControl", "");
				Guid guid = Guid.Empty;
				if (Clerk.CurrentObject != null)
					guid = Clerk.CurrentObject.Guid;
				FdoCache cache = Cache;
				// Not sure what will happen with guid == Guid.Empty on the link...
				FwLinkArgs link = new FwLinkArgs(toolName, guid, InterlinearTab.ToString());
				link.PropertyTableEntries.Add(new XCore.Property("InterlinearTab",
					InterlinearTab.ToString()));
				m_mediator.SendMessage("AddContextToHistory", link, false);
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
				string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
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
			var toolChoice = m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_textsWords", null);
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

			if (m_bookmark != null) // This is out here to save bookmarks set in Chart, Print and Edit views too.
			{
				//At this point m_tabCtrl.SelectedIndex is set to the value of the tabPage
				//we are leaving.
				m_bookmark.Save();
			}
		}
		}

	public class InterlinearTextsRecordClerk : RecordClerk
	{
		// The following is used in the process of selecting the ws for a new text.  See LT-6692.
		private int m_wsPrevText = 0;
		public int PrevTextWs
		{
			get { return m_wsPrevText; }
			set { m_wsPrevText = value; }
		}

		public override void Init(Mediator mediator, XmlNode viewConfiguration)
		{
			base.Init(mediator, viewConfiguration);
			CanAccessScriptureIds();	// cache ability to access scripture ids.
		}

		/// <summary>
		/// Get the list of currently selected Scripture section ids.
		/// </summary>
		/// <returns></returns>
		public List<int> GetScriptureIds()
		{
			return (from st in GetInterestingTextList().ScriptureTexts select st.Hvo).ToList();
		}

		/// <summary>
		/// The current object in this view is either a WfiWordform or an StText, and if we can delete
		/// an StText at all, we want to delete its owning Text.
		/// </summary>
		/// <param name="currentObject"></param>
		/// <returns></returns>
		protected override ICmObject GetObjectToDelete(ICmObject currentObject)
		{
			if (currentObject is IWfiWordform)
				return currentObject;
			return currentObject.Owner;
		}

		/// <summary>
		/// We can only delete Texts in this view, not scripture sections.
		/// </summary>
		/// <returns></returns>
		protected override bool CanDelete()
		{
			if (CurrentObject is IWfiWordform)
				return true;
			return CurrentObject.Owner is FDO.IText;
		}

		protected override void ReportCannotDelete()
		{
			if (CurrentObject is IWfiWordform)
				MessageBox.Show(Form.ActiveForm, ITextStrings.ksCannotDeleteWordform, ITextStrings.ksError,
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			else
				MessageBox.Show(Form.ActiveForm, ITextStrings.ksCannotDeleteScripture, ITextStrings.ksError,
					MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		protected override bool AddItemToList(int hvoItem)
		{

			IStText stText;
			if (!Cache.ServiceLocator.GetInstance<IStTextRepository>().TryGetObject(hvoItem, out stText))
			{
				// Not an StText; we have no idea how to add it (possibly a WfiWordform?).
				return base.AddItemToList(hvoItem);
			}
			var interestingTexts = GetInterestingTextList();
			return interestingTexts.AddChapterToInterestingTexts(stText);
		}

		/// <summary>
		/// Enable the "Add Scripture" command if TE is installed.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayAddScripture(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			display.Enabled = IsActiveClerk && CanAccessScriptureIds();
			display.Visible = display.Enabled;
			return true;
		}

		/// <summary>
		/// Indicated whether TE is installed or not;
		/// </summary>
		bool m_fCanAccessScripture = false;
		bool m_fCanAccessScriptureCached = false;
		private bool CanAccessScriptureIds()
		{
			if (!m_fCanAccessScriptureCached)
			{
				m_fCanAccessScripture = FwUtils.IsTEInstalled;
				m_fCanAccessScriptureCached = true;
			}
			return m_fCanAccessScripture;
		}

		protected bool OnAddScripture(object args)
		{
			CheckDisposed();
			// get saved scripture choices
			var interestingTextsList = GetInterestingTextList();
			var scriptureTexts = interestingTextsList.ScriptureTexts.ToArray();

			IFilterScrSectionDialog<IStText> dlg = null;
			try
			{
				dlg = (IFilterScrSectionDialog<IStText>)DynamicLoader.CreateObject(
					"ScrControls.dll", "SIL.FieldWorks.Common.Controls.FilterScrSectionDialog",
					Cache, scriptureTexts, m_mediator.HelpTopicProvider);
				if (dlg.ShowDialog() == DialogResult.OK)
					interestingTextsList.SetScriptureTexts(dlg.GetListOfIncludedScripture());
			}
			finally
			{
				if (dlg != null)
					((IDisposable)dlg).Dispose();
			}

			return true;
		}

		private InterestingTextList GetInterestingTextList()
		{
			return InterestingTextsDecorator.GetInterestingTextList(m_mediator, Cache.ServiceLocator);
		}

		/// <summary>
		/// Always enable the 'InsertInterlinText' command by default for this class, but allow
		/// subclasses to override this behavior.
		/// </summary>
		public virtual bool OnDisplayInsertInterlinText(object commandObject,
														ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Visible = IsActiveClerk && InDesiredArea("textsWords");
			if (!display.Visible)
			{
				display.Enabled = false;
				return true; // or should we just say, we don't know? But this command definitely should only be possible when this IS active.
			}

			RecordClerk clrk = m_mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk;
			if (clrk != null && clrk.Id == "interlinearTexts")
			{
				display.Enabled = true;
				return true;
			}
			display.Enabled = false;
			return true;
		}

		/// <summary>
		/// We use a unique method name for inserting a text, which could otherwise be handled simply
		/// by letting the Clerk handle InsertItemInVector, because after it is inserted we may
		/// want to switch tools.
		/// The argument should be the XmlNode for <parameters className="Text"/>.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnInsertInterlinText(object argument)
		{
			if (!IsActiveClerk || !InDesiredArea("textsWords"))
				return false;
			return AddNewText(argument as Command);
		}

		/// <summary>
		/// Add a new text (but don't make it undoable)
		/// </summary>
		/// <returns></returns>
		internal bool AddNewTextNonUndoable()
		{
			return AddNewText(null);
		}

		private bool AddNewText(Command command)
		{
			// Get the default writing system for the new text.  See LT-6692.
			m_wsPrevText = Cache.DefaultVernWs;
			if (CurrentObject != null && Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Count > 1)
			{
				m_wsPrevText = WritingSystemServices.ActualWs(Cache, WritingSystemServices.kwsVernInParagraph,
													 CurrentObject.Hvo, StTextTags.kflidParagraphs);
			}
			if (m_list.Filter != null)
			{
				// Tell the user we're turning off the filter, and then do it.
				MessageBox.Show(ITextStrings.ksTurningOffFilter, ITextStrings.ksNote, MessageBoxButtons.OK);
				m_mediator.SendMessage("RemoveFilters", this);
				m_activeMenuBarFilter = null;
			}
			SaveOnChangeRecord(); // commit any changes before we create a new text.
			RecordList.ICreateAndInsert<IStText> createAndInsertMethodObj;
			if (command != null)
				createAndInsertMethodObj = new UndoableCreateAndInsertStText(Cache, command, this);
			else
				createAndInsertMethodObj = new NonUndoableCreateAndInsertStText(Cache, this);
			m_list.DoCreateAndInsert(createAndInsertMethodObj);
			if (CurrentObject == null || CurrentObject.Hvo == 0)
				return false;
			if (!InDesiredTool("interlinearEdit"))
				m_mediator.SendMessage("FollowLink", new FwLinkArgs("interlinearEdit", CurrentObject.Guid));
			// This is a workable alternative (where link is the one created above), but means this code has to know about the FwXApp class.
			//(FwXApp.App as FwXApp).OnIncomingLink(link);
			// This alternative does NOT work; it produces a deadlock...I think the remote code is waiting for the target app
			// to return to its message loop, but it never does, because it is the same app that is trying to send the link, so it is busy
			// waiting for 'Activate' to return!
			//link.Activate();
			return true;
		}

		internal abstract class CreateAndInsertStText : RecordList.ICreateAndInsert<IStText>
		{
			internal CreateAndInsertStText(FdoCache cache, InterlinearTextsRecordClerk clerk)
			{
				Cache = cache;
				Clerk = clerk;
			}

			protected InterlinearTextsRecordClerk Clerk;
			protected FdoCache Cache;
			protected IStText NewStText;

			#region ICreateAndInsert<IStText> Members

			public abstract IStText Create();

			/// <summary>
			/// updates NewStText
			/// </summary>
			protected void CreateNewTextWithEmptyParagraph(int wsText)
			{
				var newText =
					Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
				Cache.LangProject.TextsOC.Add(newText);
				NewStText =
					Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				newText.ContentsOA = NewStText;
				Clerk.CreateFirstParagraph(NewStText, wsText);
				InterlinMaster.LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(NewStText, false);
			}

			#endregion
		}

		internal class UndoableCreateAndInsertStText : CreateAndInsertStText
		{
			internal UndoableCreateAndInsertStText(FdoCache cache, Command command, InterlinearTextsRecordClerk clerk)
				: base(cache, clerk)
			{
				CommandArgs = command;
			}
			private Command CommandArgs;

			public override IStText Create()
			{
				// don't inline this, it launches a dialog and should be done BEFORE starting the UOW.
				int wsText = Clerk.GetWsForNewText();

				UndoableUnitOfWorkHelper.Do(CommandArgs.UndoText, CommandArgs.RedoText, Cache.ActionHandlerAccessor,
											()=> CreateNewTextWithEmptyParagraph(wsText));
				return NewStText;
			}
		}

		internal class NonUndoableCreateAndInsertStText : CreateAndInsertStText
		{
			internal NonUndoableCreateAndInsertStText(FdoCache cache, InterlinearTextsRecordClerk clerk)
				: base(cache, clerk)
			{
			}

			public override IStText Create()
			{
				// don't inline this, it launches a dialog and should be done BEFORE starting the UOW.
				int wsText = Clerk.GetWsForNewText();

				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor,
											() => CreateNewTextWithEmptyParagraph(wsText));
				return NewStText;
			}
		}

		/// <summary>
		/// Establish the writing system of the new text by filling its first paragraph with
		/// an empty string in the proper writing system.
		/// </summary>
		/// <param name="stText"></param>
		internal void CreateFirstParagraph(IStText stText, int wsText)
		{
			var txtPara = stText.AddNewTextPara(null);
			txtPara.Contents = StringUtils.MakeTss(string.Empty, wsText);
		}

		private int GetWsForNewText()
		{
			int wsText = PrevTextWs;
			if (wsText != 0)
			{
				if (Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Count > 1)
				{
					using (var dlg = new ChooseTextWritingSystemDlg())
					{
						dlg.Initialize(Cache, m_mediator.HelpTopicProvider, wsText);
						dlg.ShowDialog(Form.ActiveForm);
						wsText = dlg.TextWs;
					}
				}
				PrevTextWs = 0;
			}
			else
			{
				wsText = Cache.DefaultVernWs;
			}
			return wsText;
		}
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


	/// <summary>
	/// Helper for keeping track of our location in the text when switching from and back to the
	/// Texts area (cf. LT-1543).  It also serves to keep our place when switching between
	/// RawTextPane (Baseline), GlossPane, AnalyzePane(Interlinearizer), TaggingPane, PrintPane and ConstChartPane.
	/// </summary>
	public class InterAreaBookmark : IStTextBookmark
	{
		InterlinMaster m_interlinMaster;
		XCore.Mediator m_mediator;
		FdoCache m_cache = null;
		bool m_fInTextsArea = false;
		int m_iParagraph = -1;
		int m_BeginOffset = -1;
		int m_EndOffset = -1;

		internal InterAreaBookmark()
		{
		}

		internal InterAreaBookmark(InterlinMaster interlinMaster, Mediator mediator, FdoCache cache)	// For restoring
		{
			Init(interlinMaster, mediator, cache);
			this.Restore();
		}

		internal void Init(InterlinMaster interlinMaster, Mediator mediator, FdoCache cache)
		{
			Debug.Assert(interlinMaster != null);
			Debug.Assert(mediator != null);
			Debug.Assert(cache != null);
			m_interlinMaster = interlinMaster;
			m_mediator = mediator;
			m_cache = cache;
			m_fInTextsArea = m_interlinMaster.InTextsArea;
			if (m_fInTextsArea)
				return;
			// We may be switching areas to the Texts from somewhere else, which isn't yet
			// reflected in the value of "areaChoice", but is reflected in the value of
			// "currentContentControlParameters" if we dig a little bit.
			var x = m_mediator.PropertyTable.GetValue("currentContentControlParameters", null) as XmlElement;
			if (x == null)
				return;
			var xnl = x.GetElementsByTagName("parameters");
			foreach (var xn in xnl)
			{
				var xe = xn as XmlElement;
				if (xe == null)
					continue;
				var sVal = xe.GetAttribute("area");
				if (sVal == null || sVal != "textsWords")
					continue;
					m_fInTextsArea = true;
					break;
				}
			}

		/// <summary>
		/// Saves and persists the current selected annotation (or string) in the InterlinMaster.
		/// </summary>
		public void Save()
		{
			m_interlinMaster.SaveBookMark();
		}

		/// <summary>
		/// Saves the given AnalysisOccurrence in the InterlinMaster.
		/// </summary>
		/// <param name="point"></param>
		/// <param name="fPersistNow">if true, this annotation will persist.</param>
		public void Save(AnalysisOccurrence point, bool fPersistNow)
		{
			if (point == null || !point.IsValid)
			{
				Reset(); // let's just reset for an empty location.
				return;
			}
			var iParaInText = point.Segment.Paragraph.IndexInOwner;
			var begOffset = point.Segment.GetAnalysisBeginOffset(point.Index);
			var endOffset = point.HasWordform ? begOffset + point.BaselineText.Length : begOffset;

			Save(iParaInText, begOffset, endOffset, fPersistNow);
		}

		/// <summary>
		/// Saves the current selected annotation in the InterlinMaster.
		/// </summary>
		/// <param name="fPersistNow">if true, this annotation will persist.</param>
		public void Save(bool fPersistNow)
		{
			if (fPersistNow)
				this.SavePersisted();
		}

		internal void Save(int paragraphIndex, int beginCharOffset, int endCharOffset, bool fPersistNow)
		{
			m_iParagraph = paragraphIndex;
			m_BeginOffset = beginCharOffset;
			m_EndOffset = endCharOffset;

			this.Save(fPersistNow);
		}

		private string BookmarkNamePrefix
		{
			get
			{
				return "ITexts-Bookmark-";
			}
		}

		internal string RecordIndexBookmarkName
		{
			get
			{
				return BookmarkPropertyName("IndexOfRecord");
			}
		}

		private string BookmarkPropertyName(string attribute)
		{
			return BookmarkNamePrefix + attribute;
		}

		private void SavePersisted()
		{
			// Currently, we only support persistence for the Texts area, since the Words
			// area Record Clerk keeps track of the current CmBaseAnnotation for us.
			// This will help prevent us from saving over or loading something persisted
			// for another area.  We should make this class inherit from IPersistAsXml if we want
			// to store information for identifying which record clerk we are saving for.
			if (!m_fInTextsArea)
				return;
			Debug.Assert(m_mediator != null);
			// TODO: store clerk identifier in property. For now, just do the index.
			// to make this more strict, we could match on the title, but let's do that later.
			int recordIndex = m_interlinMaster.IndexOfTextRecord;
			// string recordTitle = m_interlinMaster.TitleOfTextRecord;
			// m_mediator.PropertyTable.SetProperty(pfx + "Title", recordTitle, false);
			m_mediator.PropertyTable.SetProperty(RecordIndexBookmarkName, recordIndex, false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetProperty(BookmarkPropertyName("IndexOfParagraph"), m_iParagraph, false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetProperty(BookmarkPropertyName("CharBeginOffset"), m_BeginOffset, false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetProperty(BookmarkPropertyName("CharEndOffset"), m_EndOffset, false, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence(RecordIndexBookmarkName, true, PropertyTable.SettingsGroup.LocalSettings);
			// m_mediator.PropertyTable.SetPropertyPersistence(pfx + "Title", true);
			m_mediator.PropertyTable.SetPropertyPersistence(BookmarkPropertyName("IndexOfParagraph"), true, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence(BookmarkPropertyName("CharBeginOffset"), true, PropertyTable.SettingsGroup.LocalSettings);
			m_mediator.PropertyTable.SetPropertyPersistence(BookmarkPropertyName("CharEndOffset"), true, PropertyTable.SettingsGroup.LocalSettings);
		}

		/// <summary>
		/// Restore the InterlinMaster bookmark to its previously saved state.
		/// </summary>
		public void Restore()
		{
			// Currently, we only support persistence for the Texts area, since the Words
			// area Record Clerk keeps track of the current CmBaseAnnotation for us.
			// This will help prevent us from us from saving over or loading something persisted
			// for another area.  We should make this class inherit from IPersistAsXml if we want
			// to store information for identifying which record clerk we are saving for.
			if (!m_fInTextsArea)
				return;
			Debug.Assert(m_mediator != null);
			// verify we're restoring to the right text. Is there a better way to verify this?
			int restoredRecordIndex = m_mediator.PropertyTable.GetIntProperty(RecordIndexBookmarkName, -1, PropertyTable.SettingsGroup.LocalSettings);
			if (m_interlinMaster.IndexOfTextRecord != restoredRecordIndex)
				return;
			m_iParagraph = m_mediator.PropertyTable.GetIntProperty(BookmarkPropertyName("IndexOfParagraph"), -1, PropertyTable.SettingsGroup.LocalSettings);
			m_BeginOffset = m_mediator.PropertyTable.GetIntProperty(BookmarkPropertyName("CharBeginOffset"), -1, PropertyTable.SettingsGroup.LocalSettings);
			m_EndOffset = m_mediator.PropertyTable.GetIntProperty(BookmarkPropertyName("CharEndOffset"), -1, PropertyTable.SettingsGroup.LocalSettings);
		}

		/// <summary>
		/// Reset the bookmark to its default values.
		/// </summary>
		public void Reset()
		{
			m_iParagraph = -1;
			m_BeginOffset = -1;
			m_EndOffset = -1;

			this.SavePersisted();
		}

		#region IStTextBookmark
		public int IndexOfParagraph { get { return m_iParagraph; } }
		public int BeginCharOffset { get { return m_BeginOffset; } }
		public int EndCharOffset { get { return m_EndOffset; } }
		#endregion
	}
}
