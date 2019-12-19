// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.TextsAndWords.Discourse;
using LanguageExplorer.Controls.PaneBar;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.FwUtils.MessageBoxEx;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// InterlinMaster is a master control for the main pane of an interlinear view.
	/// It holds and information bar ("Information"), a TitleContents pane,
	/// another information bar ("Text"/"Interlinear Text") with a label button
	/// ("Show Interlinear"/"Show Raw Text") and then either a RawTextPane or an
	/// InterlinDocChild. Eventually it may also show a SandBox, and perhaps a
	/// segment of a lexicon! This comment is way out-of-date!
	/// </summary>
	internal partial class InterlinMaster : InterlinMasterBase, IFocusablePanePortion
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private Dictionary<string, PanelButton> _paneBarButtons;
		// Controls
		protected IVwStylesheet m_styleSheet;
		protected InfoPane m_infoPane; // Parent is m_tpInfo.
		internal static Dictionary<Tuple<string, Guid>, InterAreaBookmark> m_bookmarks;
		/// <summary>
		/// The main constituent chart pane, SIL.FieldWorks.Discourse.ConstituentChart.
		/// Parent is m_tpCChart.
		/// </summary>
		private ConstituentChart m_constChartPane;
		// This flag is normally set during a Refresh. When it is set, we suppress switching the focus box
		// to the current occurrence in a concordance view, which would otherwise happen as a side effect
		// of the Refresh. Instead, as usual the focus box stays wherever it is now. The flag is cleared
		// on the next call to ShowRecord.
		// If we don't have a current record, Refresh won't result in ShowRecord being called, so we don't
		// need to set the flag (and must not, lest it interfere with the next time we move to a different
		// occurrence).
		private bool m_fRefreshOccurred;
		// true (typically used as concordance 3rd pane) to suppress autocreating a text if the
		// record list has no current object.
		protected bool m_fSuppressAutoCreate;
		private bool m_fInShowTabView;
		private ISharedEventHandlers _sharedEventHandlers;
		// These constants allow us to use a switch statement in SaveBookMark()
		private const int ktpsInfo = (int)TabPageSelection.Info;
		private const int ktpsRawText = (int)TabPageSelection.RawText;
		private const int ktpsGloss = (int)TabPageSelection.Gloss;
		private const int ktpsAnalyze = (int)TabPageSelection.Interlinearizer;
		private const int ktpsTagging = (int)TabPageSelection.TaggingView;
		private const int ktpsPrint = (int)TabPageSelection.PrintView;
		private const int ktpsCChart = (int)TabPageSelection.ConstituentChart;

		internal InterlinMaster()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		internal InterlinMaster(XElement configurationParametersElement, MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList, Dictionary<string, PanelButton> paneBarButtons, bool showTitlePane = true)
			: base(configurationParametersElement, majorFlexComponentParameters.LcmCache, recordList)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			Dock = DockStyle.Top;
			m_tcPane.Visible = showTitlePane;
			m_rtPane.MyRecordList = recordList;
			m_rtPane.MyMajorFlexComponentParameters = majorFlexComponentParameters;
			m_rtPane.ConfigurationParameters = configurationParametersElement;
			_majorFlexComponentParameters = majorFlexComponentParameters;
			_paneBarButtons = paneBarButtons;
			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			m_taggingPane.MyMajorFlexComponentParameters = majorFlexComponentParameters;
			m_idcGloss.MyMajorFlexComponentParameters = majorFlexComponentParameters;
			m_idcAnalyze.MyMajorFlexComponentParameters = majorFlexComponentParameters;
			m_printViewPane.MyMajorFlexComponentParameters = majorFlexComponentParameters;
		}

		protected virtual void SetupUiWidgets(UserControlUiWidgetParameterObject userControlUiWidgetParameterObject)
		{
			var editMenuItemsForUserControl = userControlUiWidgetParameterObject.MenuItemsForUserControl[MainMenu.Edit];
			editMenuItemsForUserControl.Add(Command.CmdFindAndReplaceText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(EditFindMenu_Click, () => CanCmdFindAndReplaceText));
			editMenuItemsForUserControl.Add(Command.CmdReplaceText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(EditFindAndReplaceMenu_Click, () => CanCmdReplaceText));
			userControlUiWidgetParameterObject.MenuItemsForUserControl[MainMenu.Insert].Add(Command.CmdAddNote, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdAddNoteClick, () => CanCmdAddNote));
			var insertToolBarButtonsForUserControl = userControlUiWidgetParameterObject.ToolBarItemsForUserControl[ToolBar.Insert];
			insertToolBarButtonsForUserControl.Add(Command.CmdAddNote, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdAddNoteClick, () => CanCmdAddNote));
			insertToolBarButtonsForUserControl.Add(Command.CmdFindAndReplaceText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(EditFindMenu_Click, () => CanCmdFindAndReplaceText));
			var toolsMenuForUserControl = userControlUiWidgetParameterObject.MenuItemsForUserControl[MainMenu.Tools];
			toolsMenuForUserControl.Add(Command.CmdConfigureInterlinear, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ConfigureInterlinear_Clicked, ()=> CanConfigureInterlinear));
			UiWidgetServices.InsertPair(insertToolBarButtonsForUserControl, toolsMenuForUserControl, Command.CmdLexiconLookup,
				new Tuple<EventHandler, Func<Tuple<bool, bool>>>(LexiconLookup_Clicked, ()=> CanCmdLexiconLookup));
		}

		private bool InFriendlyTab => m_tabCtrl.SelectedTab == m_tpRawText;

		private Tuple<bool, bool> CanCmdLexiconLookup => new Tuple<bool, bool>(true, InFriendlyTab);

		private void LexiconLookup_Clicked(object sender, EventArgs e)
		{
			AreaServices.LexiconLookup(Cache, new FlexComponentParameters(PropertyTable, Publisher, Subscriber), m_rtPane);
		}

		/// <summary>
		/// if the XML configuration does not specify the availability of the treebar
		/// (e.g. treeBarAvailability="Required"), then use this.
		/// </summary>
		protected override TreebarAvailability DefaultTreeBarAvailability => TreebarAvailability.NotAllowed;

		internal string BookmarkId => MyRecordList.Id ?? string.Empty;

		/// <summary>
		/// Something sometimes insists on giving the tab control focus when switching tabs.
		/// This defeats ctrl-tab to move between tabs.
		/// </summary>
		private void m_tabCtrl_GotFocus(object sender, EventArgs e)
		{
			if (m_tabCtrl.SelectedTab == null)
			{
				return;
			}
			var child = m_tabCtrl.SelectedTab.Controls.Cast<Control>().Select(c => c).FirstOrDefault();
			child?.Focus();
		}

		/// <summary>
		/// Called by reflection when the browse view steals the focus.
		/// </summary>
		public void OnBrowseViewStoleFocus(object sender)
		{
			m_tabCtrl_GotFocus(sender, new EventArgs());
		}

		internal bool ParsedDuringSave { get; set; }

		internal TitleContentsPane TitleContentsPane
		{
			get { return m_tcPane; }
			set { m_tcPane = value; }
		}

		protected int GetWidth(string text, Font fnt)
		{
			int width;
			using (var g = Graphics.FromHwnd(Handle))
			{
				width = (int)g.MeasureString(text, fnt).Width + 1;
			}
			return width;
		}

		private void SetStyleSheetFor(IStyleSheet site)
		{
			if (m_styleSheet == null)
			{
				SetupStyleSheet();
			}
			if (site != null)
			{
				site.StyleSheet = m_styleSheet;
			}
		}

		private IInterlinearTabControl CurrentInterlinearTabControl { get; set; }

		private void SetCurrentInterlinearTabControl(IInterlinearTabControl pane)
		{
			CurrentInterlinearTabControl = pane;
			SetupInterlinearTabControlForStText(pane);
		}

		private void SetupInterlinearTabControlForStText(IInterlinearTabControl site)
		{
			InitializeInterlinearTabControl(site);
			if (site is ISetupLineChoices)
			{
				var interlinearView = (ISetupLineChoices)site;
				var lineChoicesKey = $"InterlinConfig_{(interlinearView.ForEditing ? "Edit" : "Doc")}_{InterlinearTab}";
				var mode = GetLineMode();
				interlinearView.SetupLineChoices(lineChoicesKey, mode);
			}
			// Review: possibly need to do SetPaneSizeAndRoot
			if (site != null)
			{
				if (site is Control)
				{
					(site as Control).SuspendLayout();
				}
				site.SetRoot(RootStTextHvo);
				if (site is Control)
				{
					(site as Control).ResumeLayout();
				}
			}
		}

		internal InterlinMode GetLineMode()
		{
			switch (m_tabCtrl.SelectedIndex)
			{
				case (int)TabPageSelection.Gloss:
					return PropertyTable.GetValue(TextAndWordsArea.ITexts_AddWordsToLexicon, false) ? InterlinMode.GlossAddWordsToLexicon : InterlinMode.Gloss;
				case (int)TabPageSelection.ConstituentChart:
					return InterlinMode.Chart;
				case (int)TabPageSelection.TaggingView:
					return InterlinMode.Gloss;
				default:
					return InterlinMode.Analyze;
			}
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
			{
				SelectAnnotation();
			}
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
			}
		}

		protected override void SetInfoBarText()
		{
			if (m_informationBar != null && m_configurationParametersElement != null)
			{
				var sAltTitle = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "altTitleId");
				if (!string.IsNullOrEmpty(sAltTitle))
				{
					var sTitle = StringTable.Table.GetString(sAltTitle, StringTable.AlternativeTitles);
					if (!string.IsNullOrEmpty(sTitle))
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
		private void FinishInitTabPages()
		{
			if (!XmlUtils.GetOptionalBooleanAttributeValue(m_configurationParametersElement, "editable", true))
			{
				//  Finish defining m_tpRawText.
				m_tpRawText.ToolTipText = string.Format(ITextStrings.ksBaseLineNotEditable);
			}
		}

		private void SetupStyleSheet()
		{
			m_styleSheet = FwUtils.StyleSheetFromPropertyTable(PropertyTable);
		}

		/// <summary>
		/// Sets m_bookmarks to what is currently selected and persists it.
		/// </summary>
		internal void SaveBookMark()
		{
			if (m_tabCtrl.SelectedIndex == ktpsInfo || CurrentInterlinearTabControl == null)
			{
				return; // nothing to save...for now, don't overwrite existing one.
			}
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
					{
						return; // e.g., right after creating a new database, when previous one was open in chart pane.
					}
					// Call CChart.GetUnchartedWordForBookmark() by reflection to see where the chart
					// thinks the bookmark should be.
					var type = m_constChartPane.GetType();
					var info = type.GetMethod("GetUnchartedWordForBookmark");
					Debug.Assert(info != null);
					curAnalysis = (AnalysisOccurrence)info.Invoke(m_constChartPane, null);
					break;
				case ktpsTagging:
					if (m_taggingPane != null)
					{
						curAnalysis = m_taggingPane.OccurrenceContainingSelection();
					}
					break;
				case ktpsPrint:
					if (m_printViewPane != null)
					{
						curAnalysis = m_printViewPane.OccurrenceContainingSelection();
					}
					break;
				case ktpsRawText:
					// Find the analysis we were working on.
					if (m_rtPane != null)
					{
						m_rtPane.IsCurrentTabForInterlineMaster = true;
						if (SaveBookmarkFromRootBox(m_rtPane.RootBox))
						{
							return;
						}
					}
					break;
				default:
					Debug.Fail("Unhandled tab index.");
					break;
			}
			if (curAnalysis == null || !curAnalysis.IsValid)
			{
				// This result means the Chart doesn't want to save a bookmark,
				// or that something else went wrong (e.g., we couldn't make a bookmark because we just deleted the text).
				return;
			}
			if (fSaved)
			{
				return;
			}
			InterAreaBookmark mark;
			if (m_bookmarks.TryGetValue(new Tuple<string, Guid>(MyRecordList.Id, RootStText.Guid), out mark))
			{
				//We only want to persist the save if we are in the interlinear edit, not the concordance view
				mark.Save(curAnalysis, MyRecordList.Id.Equals(TextAndWordsArea.InterlinearTexts), IndexOfTextRecord);
			}
			else
			{
				mark = new InterAreaBookmark(this, Cache, PropertyTable);
				mark.Restore(IndexOfTextRecord);
				m_bookmarks.Add(new Tuple<string, Guid>(MyRecordList.Id, RootStText.Guid), mark);
			}
		}

		/// <summary>
		/// Returns true if it already saved a bookmark (from RootBox), false otherwise.
		/// </summary>
		/// <param name="pane"></param>
		/// <param name="curAnalysis">ref var comes out with the location to be saved.</param>
		/// <returns></returns>
		private bool SandboxPaneBookmarkSave(InterlinDocRootSiteBase pane, ref AnalysisOccurrence curAnalysis)
		{
			if (pane == null) // Can this really happen? Perhaps if !m_fullyinitialized?
			{
				if (m_rtPane != null) // Not the one, but the other? Odd.
				{
					if (SaveBookmarkFromRootBox(m_rtPane.RootBox))
					{
						return true;
					}
				}
			}
			else
			{
				curAnalysis = pane.OccurrenceContainingSelection();
			}
			return false;
		}

		private bool SaveBookmarkFromRootBox(IVwRootBox rb)
		{
			if (rb?.Selection == null)
			{
				return false;
			}
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
			var sliAnchor = helper.GetLevelInfo(SelLimitType.Anchor);
			var sliEnd = helper.GetLevelInfo(SelLimitType.End);
			if (sliAnchor.Length != sliEnd.Length)
			{
				ichEnd = ichAnchor;
			}
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
				{
					continue;
				}
				hvoParaEnd = sliEnd[i].hvo;
				break;
			}
			if (hvoParaAnchor != 0)
			{
				IStTxtPara para;
				if (Cache.ServiceLocator.GetInstance<IStTxtParaRepository>().TryGetObject(hvoParaAnchor, out para))
				{
					iPara = para.IndexInOwner;
					if (hvoParaAnchor != hvoParaEnd)
					{
						ichEnd = ichAnchor;
					}

					if (ichAnchor == -1)
					{
						ichAnchor = 0;
					}

					if (ichEnd == -1)
					{
						ichEnd = 0;
					}
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
				var key = new Tuple<string, Guid>(MyRecordList.Id, RootStText.Guid);
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
			{
				return;     // cannot display properly without style sheet, so don't try.
			}

			// LT-10995: the TitleContentsPane m_tcPane and the TabControl m_tabCtrl used to be
			// docked (definition in InterlinMaster.resx). However, this led to problems if the
			// font size of displayed data got changed. So we are doing the layout ourselves now:
			m_tabCtrl.Width = Width; // tab control width = container width

			if (m_tcPane == null)
			{
				// If there is no TitleContentsPane then the TabControl needs to occupy the
				// entire container:
				m_tabCtrl.Location = new Point(0, 0);
				m_tabCtrl.Height = Height;
			}
			else
			{
				// If there is a TitleContentsPane then it needs to be at the top of the
				// container, match the container's width, and have its height calculated
				// automatically:
				m_tcPane.Location = new Point(0, 0);
				m_tcPane.Width = Width;
				m_tcPane.AdjustHeight();

				// And then the TabControl needs to fill the rest of the container below
				// the TitleContentsPane:
				m_tabCtrl.Location = new Point(0, m_tcPane.Height);
				m_tabCtrl.Height = Height - m_tcPane.Height;
			}

			base.OnLayout(levent);
		}

		private int RootStTextHvo => RootStText?.Hvo ?? 0;
		/// <remarks>virtual for tests</remarks>
		protected internal virtual IStText RootStText { get; private set; }

		internal int TextListFlid => MyRecordList.VirtualFlid;

		internal int TextListIndex => MyRecordList.CurrentIndex;

		protected void ShowTabView()
		{
			SaveWorkInProgress();
			m_fInShowTabView = true;
			/*
			m_rtPane.IsCurrentTabForInterlineMaster = false;
			m_taggingPane.IsCurrentTabForInterlineMaster = false;
			m_idcGloss.IsCurrentTabForInterlineMaster = false;
			m_idcAnalyze.IsCurrentTabForInterlineMaster = false;
			m_printViewPane.IsCurrentTabForInterlineMaster = false;
			if (_paneBarButtons != null)
			{
				foreach (var panelButton in _paneBarButtons.Values)
				{
					panelButton.Visible = panelButton.Enabled = false;
				}
			}
			if (m_constChartPane != null)
			{
				m_constChartPane.InterlineMasterWantsExportDiscourseChartDiscourseChartMenu = false;
			}
			*/
			try
			{
				m_tabCtrl.SelectedIndex = (int)InterlinearTab; // set the persisted tab setting.
				if (m_tabCtrl.SelectedIndex == ktpsCChart && m_constChartPane == null)
				{
					// This is the first time on this tab, do lazy creation
					m_constChartPane = new ConstituentChart(_majorFlexComponentParameters.LcmCache, _sharedEventHandlers, _majorFlexComponentParameters.UiWidgetController);
					m_constChartPane.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
					m_constChartPane.BackColor = SystemColors.Window;
					m_constChartPane.Name = "m_constChartPane";
					m_constChartPane.Dock = DockStyle.Fill;
					m_tpCChart.Controls.Add(m_constChartPane);
					if (m_styleSheet != null)
					{
						m_styleSheet = ((IStyleSheet)m_constChartPane).StyleSheet;
					}
				}
				// search through the current tab page controls until we find one implementing IInterlinearTabControl
				var currentTabControl = FindControls<IInterlinearTabControl>(m_tabCtrl.SelectedTab.Controls).FirstOrDefault();
				SetCurrentInterlinearTabControl(currentTabControl as IInterlinearTabControl);
				if (CurrentInterlinearTabControl == null)
				{
					return; // nothing to show.
				}

				PanelButton panelButton;
				switch (m_tabCtrl.SelectedIndex)
				{
					case ktpsInfo:
						// It may already be initialized, but this is not very expensive and sometimes
						// the infoPane was initialized with no data and should be re-initialized here
						m_infoPane.Initialize(_sharedEventHandlers, _majorFlexComponentParameters.StatusBar, MyRecordList, _majorFlexComponentParameters.UiWidgetController);
						m_infoPane.Dock = DockStyle.Fill;
						m_infoPane.Enabled = m_infoPane.CurrentRootHvo != 0;
						if (m_infoPane.Enabled)
						{
							m_infoPane.BackColor = SystemColors.Control;
							if (ParentForm == Form.ActiveForm)
							{
								m_infoPane.Focus();
							}
						}
						else
						{
							m_infoPane.BackColor = Color.White;
						}
						if (_paneBarButtons != null && _paneBarButtons.ContainsKey(TextAndWordsArea.ShowHiddenFields_interlinearEdit))
						{
							panelButton = _paneBarButtons[TextAndWordsArea.ShowHiddenFields_interlinearEdit];
							panelButton.Visible = panelButton.Enabled = true;
						}
						break;
					case ktpsRawText:
						m_rtPane.IsCurrentTabForInterlineMaster = true;
						if (ParentForm == Form.ActiveForm)
						{
							m_rtPane.Focus();
						}
						if (m_rtPane.RootBox != null && m_rtPane.RootBox.Selection == null && RootStText != null)
						{
							m_rtPane.RootBox.MakeSimpleSel(true, false, false, true);
						}
						break;
					case ktpsGloss:
						m_idcGloss.IsCurrentTabForInterlineMaster = true;
						if (_paneBarButtons != null && _paneBarButtons.ContainsKey(TextAndWordsArea.ITexts_AddWordsToLexicon))
						{
							panelButton = _paneBarButtons[TextAndWordsArea.ITexts_AddWordsToLexicon];
							panelButton.Visible = panelButton.Enabled = true;
						}
						break;
					case ktpsAnalyze:
						m_idcAnalyze.IsCurrentTabForInterlineMaster = true;
						break;
					case ktpsTagging:
						m_taggingPane.IsCurrentTabForInterlineMaster = true;
						break;
					case ktpsPrint:
						m_printViewPane.IsCurrentTabForInterlineMaster = true;
						break;
					case ktpsCChart:
						if (RootStText == null)
						{
							m_constChartPane.Enabled = false;
							m_constChartPane.InterlineMasterWantsExportDiscourseChartDiscourseChartMenu = false;
						}
						else
						{
							// LT-7733 Warning dialog for Text Chart
							MessageBoxExManager.Trigger("TextChartNewFeature");
							m_constChartPane.Enabled = true;
							m_constChartPane.InterlineMasterWantsExportDiscourseChartDiscourseChartMenu = true;
						}
						//SetConstChartRoot(); should be done above in SetCurrentInterlinearTabControl()
						if (ParentForm == Form.ActiveForm)
						{
							m_constChartPane.Focus();
						}
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

		/// <summary>
		/// Finds the controls implementing TInterfaceMatch
		/// </summary>
		private static IEnumerable<Control> FindControls<TInterfaceMatch>(ControlCollection controls)
		{
			foreach (Control c in controls)
			{
				if (c is TInterfaceMatch)
				{
					yield return c;
				}
				foreach (var c2 in FindControls<TInterfaceMatch>(c.Controls))
				{
					yield return c2;
				}
			}
		}

		#region Overrides of ViewBase

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			m_rtPane.InitializeFlexComponent(flexComponentParameters);
			m_infoPane.InitializeFlexComponent(flexComponentParameters);
			m_idcGloss.InitializeFlexComponent(flexComponentParameters);
			m_idcAnalyze.InitializeFlexComponent(flexComponentParameters);
			m_printViewPane.InitializeFlexComponent(flexComponentParameters);
			m_taggingPane.InitializeFlexComponent(flexComponentParameters);
			// Do this BEFORE calling InitBase, which calls ShowRecord, whose correct behavior
			// depends on the suppressAutoCreate flag.
			m_fSuppressAutoCreate = XmlUtils.GetOptionalBooleanAttributeValue(m_configurationParametersElement, "suppressAutoCreate", false);
		}

		#endregion

		/// <summary>
		/// About to show, so finish initializing.
		/// </summary>
		internal void FinishInitialization()
		{
			var userController = new UserControlUiWidgetParameterObject(this);
			// Add handler stuff from this class and possibly from subclasses.
			SetupUiWidgets(userController);
			_majorFlexComponentParameters.UiWidgetController.AddHandlers(userController);
			if (m_tcPane != null && m_tcPane.Visible)
			{
				// Making the tab control currently requires this first...
				m_tcPane.StyleSheet = m_styleSheet;
				m_tcPane.Visible = true;
			}
			if (m_bookmarks != null && m_bookmarks.Count > 0)
			{
				foreach (var bookmark in m_bookmarks.Values)
				{
					bookmark.Init(this, Cache, PropertyTable);
				}
			}
			FinishInitTabPages();
			SetInitialTabPage();
			InitBase();
			ShowRecord();
			m_fullyInitialized = true;
		}

		/// <summary>
		/// Set the appropriate tab index BEFORE calling InitBase, since that calls
		/// RecordView.InitBase, which calls ShowRecord, which calls ShowTabView,
		/// which will unnecessarily create the wrong pane, if the tab index is wrong.
		/// </summary>
		private void SetInitialTabPage()
		{
			// If we haven't already switched to that tab page, do so now.
			if (Visible && m_tabCtrl.SelectedIndex != (int)InterlinearTab)
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
		/// From IMainContentControl
		/// </summary>
		/// <returns>true if ok to go away</returns>
		public override bool PrepareToGoAway()
		{
			SaveBookMark();
			return SaveWorkInProgress() && base.PrepareToGoAway();
		}

		private bool SaveWorkInProgress()
		{
			if (m_idcAnalyze != null && m_idcAnalyze.Visible && !m_idcAnalyze.PrepareToGoAway())
			{
				return false;
			}
			return m_idcGloss == null || !m_idcGloss.Visible || m_idcGloss.PrepareToGoAway();
		}

		public void PrepareToRefresh()
		{
			// flag that a refresh was triggered (unless we don't have a current record..see var comment).
			if (RootStTextHvo != 0)
			{
				m_fRefreshOccurred = true;
			}
		}

		protected override void SetupDataContext()
		{
			base.SetupDataContext();
			InitializeInterlinearTabControl(m_tcPane);
			InitializeInterlinearTabControl(CurrentInterlinearTabControl);
			m_fullyInitialized = true;
		}


		private void InitializeInterlinearTabControl(IInterlinearTabControl site)
		{
			if (site == null)
			{
				return;
			}
			SetStyleSheetFor(site as IStyleSheet);
			site.Cache = Cache;
			var siteAsFlexComponent = site as IFlexComponent;
			if (siteAsFlexComponent != null && siteAsFlexComponent.PropertyTable == null)
			{
				// Only do it one time.
				siteAsFlexComponent.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			}
		}

		/// <summary>
		/// The record index of the currently selected text.
		/// </summary>
		internal int IndexOfTextRecord
		{
			get
			{
				if (MyRecordList.CurrentObjectHvo != 0 && Cache.ServiceLocator.IsValidObjectId(MyRecordList.CurrentObjectHvo))
				{
					if (MyRecordList.CurrentObject.ClassID == StTextTags.kClassId)
					{
						return MyRecordList.CurrentIndex;
					}
				}
				return -1;
			}
		}

		internal string TitleOfTextRecord
		{
			get
			{
				var rootObj = MyRecordList.CurrentObject;
				if (rootObj?.ClassID != TextTags.kClassId)
				{
					return string.Empty;
				}
				var text = rootObj as IText;
				return text.Name.AnalysisDefaultWritingSystem.Text;
			}
		}

		protected override void ShowRecord(RecordNavigationInfo rni)
		{
			base.ShowRecord(rni);
			// independent of whether base.ShowRecord(rni) skips ShowRecord()
			// we still want to try to put the focus in our control.
			// (but only if we're in the active window -- see FWR-1795)
			if (ParentForm == Form.ActiveForm)
			{
				Focus();
			}
		}

		/// <summary>
		/// This is an attempt to improve scrolling by mouse wheel, by passing on focus when the interlin master gets it.
		/// </summary>
		protected override void OnGotFocus(EventArgs e)
		{
			if (m_tabCtrl.SelectedTab != null && m_tabCtrl.SelectedTab.Controls[0].CanFocus)
			{
				m_tabCtrl.SelectedTab.Controls[0].Focus();
			}
		}

		/// <summary>
		/// Save any intermediate analysis information on validation requests
		/// note: this is triggered before a Send/Receive operation
		/// </summary>
		protected override void OnValidating(System.ComponentModel.CancelEventArgs e)
		{
			base.OnValidating(e);
			SaveWorkInProgress();
		}

		protected override void ShowRecord()
		{
			SaveWorkInProgress();
			base.ShowRecord();
			if (MyRecordList.SuspendLoadingRecordUntilOnJumpToRecord)
			{
				return;
			}
			//This is our very first time trying to show a text, if possible we would like to show the stored text.
			if (m_bookmarks == null)
			{
				m_bookmarks = new Dictionary<Tuple<string, Guid>, InterAreaBookmark>();
			}
			// It's important not to do this if there is a filter, as there's a good chance the new
			// record doesn't pass the filter and we get into an infinite loop. Also, if the user
			// is filtering, he probably just wants to see that there are no matching texts, not
			// make a new one.
			if (MyRecordList is InterlinearTextsRecordList && MyRecordList.CurrentObjectHvo == 0 && !m_fSuppressAutoCreate && !MyRecordList.ShouldNotModifyList && MyRecordList.Filter == null)
			{
				// This is needed in SwitchText(0) to avoid LT-12411 when in Info tab.
				// We'll get a chance to do it later.
				MyRecordList.SuppressSaveOnChangeRecord = true;
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
					var options = new ListUpdateHelperParameterObject
					{
						MyRecordList = MyRecordList,
						SuppressSaveOnChangeRecord = true
					};
					using (new ListUpdateHelper(options))
					{
						((InterlinearTextsRecordList)MyRecordList).AddNewTextNonUndoable();
					}
				});
			}
			if (MyRecordList.CurrentObjectHvo == 0)
			{
				SwitchText(0);      // We no longer have a text.
				return;             // We get another call when there is one.
			}
			var hvoRoot = MyRecordList.CurrentObjectHvo;
			if (MyRecordList.CurrentObjectHvo != 0 && !Cache.ServiceLocator.IsValidObjectId(MyRecordList.CurrentObjectHvo)) // RecordList is tracking an analysis
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
					NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => ((InterlinearTextsRecordList)MyRecordList).CreateFirstParagraph(stText, Cache.DefaultVernWs));
				}
				if (stText.ParagraphsOS.Count == 1 && ((IStTxtPara)stText.ParagraphsOS[0]).Contents.Length == 0)
				{
					// If we have restarted FLEx since this text was created, the WS has been lost and replaced with the global default of English.
					// If this is the case, default to the Default Vernacular WS (LT-15688)
					var globalDefaultWs = Cache.ServiceLocator.WritingSystemManager.Get("en").Handle;
					if (stText.MainWritingSystem == globalDefaultWs)
					{
						NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => ((IStTxtPara)stText.ParagraphsOS[0]).Contents = TsStringUtils.MakeString(string.Empty, Cache.DefaultVernWs));
					}
					// since we have no text, we should not sit on any of the analyses tabs,
					// the info tab is still useful though.
					if (InterlinearTab != TabPageSelection.Info && InterlinearTab != TabPageSelection.RawText)
					{
						InterlinearTab = TabPageSelection.RawText;
					}
					// Don't steal the focus from another window.  See FWR-1795.
					if (ParentForm == Form.ActiveForm)
					{
						m_rtPane.Focus();
					}
				}
				if (RootStText == null || RootStText.Hvo != hvoRoot)
				{
					// we've just now entered the area, so try to restore a bookmark.
					CreateOrRestoreBookmark(stText);
				}
			}
			if ((RootStText == null || RootStText.Hvo != hvoRoot) && Cache.ServiceLocator.IsValidObjectId(hvoRoot))
			{
				SwitchText(hvoRoot); // sets RootStText
			}
			else
			{
				SelectAnnotation(); // select an annotation in the current text.
			}
			// If we're showing the raw text pane make sure it has a selection.
			if (Controls.IndexOf(m_rtPane) >= 0 && m_rtPane.RootBox.Selection == null)
			{
				m_rtPane.RootBox.MakeSimpleSel(true, false, false, true);
			}
			UpdateContextHistory();
			m_fRefreshOccurred = false; // reset our flag that a refresh occurred.
		}

		private int SetConcordanceBookmarkAndReturnRoot(int hvoRoot)
		{
			if (MyRecordList.Id.Equals(TextAndWordsArea.InterlinearTexts))
			{
				return hvoRoot;
			}
			var occurrenceFromHvo = (MyRecordList).OccurrenceFromHvo(MyRecordList.CurrentObjectHvo);
			var point = occurrenceFromHvo?.BestOccurrence;
			if (point != null && point.IsValid)
			{
				var para = point.Segment.Paragraph;
				hvoRoot = para.Owner.Hvo;
			}
			ICmObject text;
			Cache.ServiceLocator.ObjectRepository.TryGetObject(hvoRoot, out text);
			if (m_fRefreshOccurred || m_bookmarks == null || text == null)
			{
				return hvoRoot;
			}
			InterAreaBookmark mark;
			if (!m_bookmarks.TryGetValue(new Tuple<string, Guid>(MyRecordList.Id, text.Guid), out mark))
			{
				mark = new InterAreaBookmark(this, Cache, PropertyTable);
				m_bookmarks.Add(new Tuple<string, Guid>(MyRecordList.Id, text.Guid), mark);
			}
			mark.Save(point, false, IndexOfTextRecord);
			return hvoRoot;
		}

		/// <summary>
		/// Restore the bookmark, or create a new one, but only if we are in the correct area
		/// </summary>
		private void CreateOrRestoreBookmark(IStText stText)
		{
			if (stText == null)
			{
				return;
			}
			InterAreaBookmark mark;
			if (m_bookmarks.TryGetValue(new Tuple<string, Guid>(MyRecordList.Id, stText.Guid), out mark))
			{
				mark.Restore(IndexOfTextRecord);
			}
			else
			{
				m_bookmarks.Add(new Tuple<string, Guid>(MyRecordList.Id, stText.Guid), new InterAreaBookmark(this, Cache, PropertyTable));
			}
		}

		private void SwitchText(int hvoRoot)
		{
			// We've switched text, so clear the Undo stack redisplay it.
			// This method will clear the Undo stack UNLESS we're changing record
			// because we inserted or deleted one, which ought to be undoable.
			MyRecordList.SaveOnChangeRecord();
			SaveBookMark();
			RootStText = hvoRoot != 0 ? Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(hvoRoot) : null;
			// one way or another it's the Text by now.
			ShowTabView();
			// I (JohnT) no longer know why we need to update the TC pane (but not any of the others?) even if it
			// is not the current pane. But it IS essential to do so AFTER updating the current one.
			// When deleting the last text, the following call can result in a resize call to the current (e.g., analysis)
			// pane, and if ShowTabView has not already cleared the current pane's knowledge of the deleted text,
			// we can get a crash (e.g., second stack in LT-12401).
			if (m_tcPane != null)
			{
				SetupInterlinearTabControlForStText(m_tcPane);
			}
		}

		// If the record list's object is an annotation, select the corresponding thing in whatever pane
		// is active. Or, if we have a bookmark, restore it.
		private void SelectAnnotation()
		{
			if (MyRecordList.CurrentObjectHvo == 0 || MyRecordList.SuspendLoadingRecordUntilOnJumpToRecord)
			{
				return;
			}
			// Use a bookmark, if we've set one.
			if (RootStText != null && RootStText.IsValidObject && m_bookmarks.ContainsKey(new Tuple<string, Guid>(MyRecordList.Id, RootStText.Guid))
				&& m_bookmarks[new Tuple<string, Guid>(MyRecordList.Id, RootStText.Guid)].IndexOfParagraph >= 0)
			{
				(CurrentInterlinearTabControl as IHandleBookmark)?.SelectBookmark(m_bookmarks[new Tuple<string, Guid>(MyRecordList.Id, RootStText.Guid)]);
			}
		}

		/// <summary>
		/// Most Interlinear document logic in InterlinMaster can be shared between
		/// the these tab pages (Analyze (Interlinearizer) and Gloss).
		/// So you can use this to test whether our tab control is in one of those tabs.
		/// </summary>
		internal bool InterlinearTabPageIsSelected()
		{
			return m_tabCtrl.SelectedIndex == ktpsAnalyze || m_tabCtrl.SelectedIndex == ktpsGloss && (m_idcAnalyze != null && m_idcAnalyze.Visible || m_idcGloss != null && m_idcGloss.Visible);
		}

		#region free translation stuff

		/// <inheritdoc />
		protected override bool ShowKeyboardCues { get; }

		private Tuple<bool, bool> CanDisplayFindTextMenuOrFindAndReplaceTextMenu
		{
			get
			{
				var toolChoice = PropertyTable.GetValue<string>(AreaServices.ToolChoice);
				var menuIsVisible = m_rtPane != null && m_tabCtrl.SelectedIndex == (int)TabPageSelection.RawText && InFriendlyArea && toolChoice != AreaServices.WordListConcordanceMachineName;
				bool enabled;
				if (menuIsVisible && m_rtPane.RootBox != null)
				{
					int hvoRoot, fragDummy;
					IVwViewConstructor vcDummy;
					IVwStylesheet ssDummy;
					m_rtPane.RootBox.GetRootObject(out hvoRoot, out vcDummy, out fragDummy, out ssDummy);
					enabled = hvoRoot != 0;
				}
				else
				{
					enabled = false;
				}
				// Although it's a modal dialog, it's dangerous for it to be visible in contexts where it
				// could not be launched, presumably because it doesn't apply to that view, and may do
				// something dangerous to another view (cf LT-7961).
				var app = PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App);
				if (app != null && !enabled)
				{
					app.RemoveFindReplaceDialog();
				}
				return new Tuple<bool, bool>(menuIsVisible, enabled);
			}
		}

		private Tuple<bool, bool> CanCmdReplaceText => CanDisplayFindTextMenuOrFindAndReplaceTextMenu;

		private void EditFindMenu_Click(object sender, EventArgs e)
		{
			HandleFindAndReplace(false);
		}

		private Tuple<bool, bool> CanCmdFindAndReplaceText => CanDisplayFindTextMenuOrFindAndReplaceTextMenu;

		private void EditFindAndReplaceMenu_Click(object sender, EventArgs e)
		{
			HandleFindAndReplace(true);
		}

		internal void HandleFindAndReplace(bool doReplace)
		{
			PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App)?.ShowFindReplaceDialog(doReplace, m_rtPane, Cache, PropertyTable.GetValue<Form>(FwUtils.window));
		}

		/// <summary>
		/// Enable the "Add Note" command if the idcPane is visible and wants to do it.
		/// </summary>
		private Tuple<bool, bool> CanCmdAddNote => new Tuple<bool, bool>(InterlinearTabPageIsSelected(), InterlinearTabPageIsSelected());

		/// <summary>
		/// Delegate the handler, if possible.
		/// </summary>
		private void CmdAddNoteClick(object sender, EventArgs e)
		{
			if (m_idcAnalyze != null && m_idcAnalyze.Visible)
			{
				m_idcAnalyze.AddNote();
			}
			else if (m_idcGloss != null && m_idcGloss.Visible)
			{
				m_idcGloss.AddNote();
			}
		}

		#endregion

		/// <summary>
		/// Gets/Sets the property table state for the selected tab page.
		/// </summary>
		internal TabPageSelection InterlinearTab
		{
			get
			{
				var val = PropertyTable.GetValue("InterlinearTab", TabPageSelection.RawText.ToString());
				if (Enum.GetNames(typeof(TabPageSelection)).Contains(val))
				{
					return (TabPageSelection)Enum.Parse(typeof(TabPageSelection), val);
				}
				const TabPageSelection tabSelection = TabPageSelection.RawText;
				InterlinearTab = tabSelection;
				return tabSelection;
			}
			set
			{
				PropertyTable.SetProperty("InterlinearTab", value.ToString(), true);
				if (m_tabCtrl.SelectedIndex != (int)InterlinearTab)
				{
					ShowTabView();
				}
			}
		}

		private Tuple<bool, bool> CanConfigureInterlinear
		{
			get
			{
				var shouldDisplay = CurrentInterlinearTabControl is InterlinDocRootSiteBase && !(CurrentInterlinearTabControl is ConstituentChart);
				return new Tuple<bool, bool>(shouldDisplay, shouldDisplay);
			}
		}

		/// <summary>
		///  Launch the Configure interlinear dialog and deal with the results
		/// </summary>
		private void ConfigureInterlinear_Clicked(object sender, EventArgs e)
		{
			if (CurrentInterlinearTabControl is IInterlinearConfigurator)
			{
				((IInterlinearConfigurator)CurrentInterlinearTabControl).ConfigureInterlinear();
			}
		}

		/// <summary>
		/// create and register a URL describing the current context, for use in going backwards
		/// and forwards
		/// </summary>
		/// <remarks> We need an override in order to store the state of the "mode".</remarks>
		protected override void UpdateContextHistory()
		{
			// are we the dominant pane? The thinking here is that if our record list is controlling
			// the record tree bar, then we are.
			if (!MyRecordList.IsControllingTheRecordTreeBar)
			{
				return;
			}
			//add our current state to the history system
			var guid = Guid.Empty;
			if (MyRecordList.CurrentObject != null)
			{
				guid = MyRecordList.CurrentObject.Guid;
			}
			// Not sure what will happen with guid == Guid.Empty on the link...
			var link = new FwLinkArgs(PropertyTable.GetValue<string>(AreaServices.ToolChoice), guid, InterlinearTab.ToString());
			link.LinkProperties.Add(new LinkProperty("InterlinearTab", InterlinearTab.ToString()));
			MyRecordList.SelectedRecordChanged(true, true); // make sure we update the record count in the Status bar.
			PropertyTable.GetValue<LinkHandler>(LanguageExplorerConstants.LinkHandler).AddLinkToHistory(link);
		}

		/// <summary>
		/// Determine if this is the correct place. [It's the only one that handles the message, and
		/// it defaults to false, so it should be.]
		/// </summary>
		private bool InFriendlyArea => PropertyTable.GetValue<string>(AreaServices.AreaChoice) == AreaServices.TextAndWordsAreaMachineName;

		private void m_tabCtrl_Selected(object sender, TabControlEventArgs e)
		{
			InterlinearTab = (TabPageSelection)m_tabCtrl.SelectedIndex;

			// If we're just starting up (setting it from saved state) we don't need to do anything to change it.
			if (m_rtPane == null && m_infoPane == null && m_idcGloss == null && m_idcAnalyze == null && m_taggingPane == null && m_printViewPane == null && m_constChartPane == null)
			{
				return;
			}
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
			{
				ShowTabView();
			}
		}

		private void m_tabCtrl_Deselecting(object sender, TabControlCancelEventArgs e)
		{
			var deselectedIndex = e.TabPageIndex;
			switch (deselectedIndex)
			{
				case ktpsInfo:
					//m_rtPane.IsCurrentTabForInterlineMaster = false;
					break;
				case ktpsRawText:
					m_rtPane.IsCurrentTabForInterlineMaster = false;
					break;
				case ktpsGloss:
					m_idcGloss.IsCurrentTabForInterlineMaster = false;
					break;
				case ktpsAnalyze:
					m_idcAnalyze.IsCurrentTabForInterlineMaster = false;
					break;
				case ktpsTagging:
					m_taggingPane.IsCurrentTabForInterlineMaster = false;
					break;
				case ktpsPrint:
					m_printViewPane.IsCurrentTabForInterlineMaster = false;
					break;
				case ktpsCChart:
					m_constChartPane.InterlineMasterWantsExportDiscourseChartDiscourseChartMenu = false;
					break;
			}
			if (_paneBarButtons != null)
			{
				foreach (var panelButton in _paneBarButtons.Values)
				{
					panelButton.Visible = panelButton.Enabled = false;
				}
			}
			// Switching tabs, usual precaution against being able to Undo things you can no longer see.
			// When we do this because we're switching objects because we deleted one, and the new
			// object is empty, we're inside the ShowRecord where it is suppressed.
			// If the tool is just starting up (in Init, before InitBase is called), we may not
			// have configuration parameters. Usually, then, there won't be a transaction open,
			// but play safe, because the call to get the record list will crash if we don't have configuration
			// params.
			if (m_configurationParametersElement != null /* && !Cache.DatabaseAccessor.IsTransactionOpen() */)
			{
				MyRecordList.SaveOnChangeRecord();
			}
			var fParsedTextDuringSave = false;
			// Pane-individual updates; None did anything, I removed them; GJM
			// Is this where we need to hook in reparsing of segments/paras, etc. if RawTextPane is deselected?
			// No. See DomainImpl.AnalysisAdjuster.
			if (m_bookmarks == null)
			{
				return;
			}
			//At this point m_tabCtrl.SelectedIndex is set to the value of the tabPage
			//we are leaving.
			if (RootStText != null && m_bookmarks.ContainsKey(new Tuple<string, Guid>(MyRecordList.Id, RootStText.Guid)))
			{
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
}