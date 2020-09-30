// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Ideally this would be an abstract class, but Designer does not handle abstract classes.
	/// </summary>
	internal partial class InterlinDocRootSiteBase : RootSite, IVwNotifyChange, IHandleBookmark, ISelectOccurrence, IStyleSheet, ISetupLineChoices, IInterlinConfigurable, IInterlinearConfigurator
	{
		private ISilDataAccess m_sda;
		/// <summary>
		/// HVO of some IStText
		/// </summary>
		protected internal int m_hvoRoot;
		protected ICmObjectRepository m_objRepo;
		/// <summary>
		/// Context menu for use when user right-clicks on Interlinear segment labels.
		/// </summary>
		private ContextMenuStrip m_labelContextMenu;
		/// <summary>
		/// Blue circle button to alert user to the presence of the Configure Interlinear context menu.
		/// </summary>
		private BlueCircleButton m_contextButton;
		/// <summary>
		/// Index of Interlinear line clicked on to generate above blue button.
		/// Allows context menu to be context-sensitive.
		/// </summary>
		private int m_iLineChoice;
		/// <summary>
		/// Helps determine if a rt-click is opening or closing the context menu.
		/// </summary>
		private long m_ticksWhenContextMenuClosed;
		private readonly HashSet<IWfiWordform> m_wordformsToUpdate;
		private bool _isCurrentTabForInterlineMaster;

		internal MajorFlexComponentParameters MyMajorFlexComponentParameters { get; set; }

		internal InterlinVc Vc { get; set; }
		public IVwRootBox Rootb { get; set; }

		internal InterlinDocRootSiteBase()
		{
			m_wordformsToUpdate = new HashSet<IWfiWordform>();
			InitializeComponent();
		}

		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode)
			{
				return;
			}
			base.MakeRoot();
			MakeRootInternal();
		}

		protected virtual void MakeRootInternal()
		{
			// Setting this result too low can result in moving a cursor from an editable field
			// to a non-editable field (e.g. with Control-Right and Control-Left cursor
			// commands).  Normally we could set this to only a few (e.g. 4). but in
			// Interlinearizer we may want to jump from one sentence annotation to the next over
			// several read-only paragraphs  contained in a word bundle.  Make sure that
			// procedures that use this limit do not move the cursor from an editable to a
			// non-editable field.
			RootBox.MaxParasToScan = 2000;
			EnsureVc();
			// We want to get notified when anything changes.
			m_sda = m_cache.MainCacheAccessor;
			m_sda.AddNotification(this);
			Vc.LineChoices = LineChoices;
			Vc.ShowDefaultSense = true;
			RootBox.DataAccess = m_cache.MainCacheAccessor;
			RootBox.SetRootObject(m_hvoRoot, Vc, InterlinVc.kfragStText, m_styleSheet);
			m_objRepo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
		}

		internal bool IsCurrentTabForInterlineMaster
		{
			get => _isCurrentTabForInterlineMaster;
			set
			{
				if (_isCurrentTabForInterlineMaster == value)
				{
					// Same value, so skip the work.
					return;
				}
				_isCurrentTabForInterlineMaster = value;
				if (_isCurrentTabForInterlineMaster)
				{
					var userController = new UserControlUiWidgetParameterObject(this);
					// Add handler stuff from this class and possibly from subclasses.
					SetupUiWidgets(userController);
					MyMajorFlexComponentParameters.UiWidgetController.AddHandlers(userController);
				}
				else
				{
					// remove handler stuff.
					TearDownUiWidgets();
				}
			}
		}

		protected virtual void SetupUiWidgets(UserControlUiWidgetParameterObject userControlUiWidgetParameterObject)
		{
			userControlUiWidgetParameterObject.MenuItemsForUserControl[MainMenu.File].Add(Command.CmdExportInterlinear, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ExportInterlinear_Click, () => CanShowExportMenu));
		}

		protected virtual void TearDownUiWidgets()
		{
			MyMajorFlexComponentParameters.UiWidgetController.RemoveUserControlHandlers(this);
		}

		private Tuple<bool, bool> CanShowExportMenu => new Tuple<bool, bool>(true, IsCurrentTabForInterlineMaster && m_hvoRoot != 0);

		private void ExportInterlinear_Click(object sender, EventArgs e)
		{
			// If the currently selected text is from Scripture, then we need to give the dialog
			// the list of Scripture texts that have been selected for interlinearization.
			var parent = Parent;
			while (parent != null && !(parent is InterlinMaster))
			{
				parent = parent.Parent;
			}
			if (parent is InterlinMaster master)
			{
				var recordList = master.MyRecordList as InterlinearTextsRecordList;
				recordList?.GetScriptureIds(); // initialize the InterestingTextList to include Scripture (prevent a crash trying later)
			}
			var fFocusBox = TryHideFocusBoxAndUninstall();
			var objRoot = m_objRepo.GetObject(m_hvoRoot);
			using (var dlg = new InterlinearExportDialog(objRoot, Vc))
			{
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				dlg.ShowDialog(this);
			}
			if (fFocusBox)
			{
				CreateFocusBox();
			}
		}
		/// <summary>
		/// Hides the sandbox and removes it from the controls.
		/// </summary>
		/// <returns>true, if it could hide the sandbox. false, if it was not installed.</returns>
		internal virtual bool TryHideFocusBoxAndUninstall()
		{
			return false; // by default it never exists.
		}

		/// <summary>
		/// Placeholder for a routine which creates the focus box in InterlinDocForAnalysis.
		/// </summary>
		internal virtual void CreateFocusBox()
		{
		}

		/// <summary />
		protected virtual void MakeVc()
		{
			throw new NotSupportedException();
		}

		protected void EnsureVc()
		{
			if (Vc == null)
			{
				MakeVc();
			}
		}

		#region ISelectOccurrence

		/// <summary>
		/// This base version is used by 'read-only' tabs that need to select an
		/// occurrence in IText from the analysis occurrence. Override for Sandbox-type selections.
		/// </summary>
		public virtual void SelectOccurrence(AnalysisOccurrence point)
		{
			if (point == null)
			{
				return;
			}
			Debug.Assert(point.HasWordform, $"Given annotation type should have wordform but was {point}.");
			// The following will select the occurrence, ... I hope!
			// Scroll to selection into view
			var sel = SelectOccurrenceInIText(point);
			if (sel == null)
			{
				return;
			}
			RootBox.Activate(VwSelectionState.vssEnabled);
			// Don't steal the focus from another window.  See FWR-1795.
			if (!Focused && ParentForm == Form.ActiveForm)
			{
				if (CanFocus)
				{
					Focus();
				}
				else
				{
					// For some reason as we switch to a tab containing this it isn't visible
					// at the point where this is called. And that suppresses Focus().
					// Arrange to get focus when we can.
					VisibleChanged += FocusWhenVisible;
				}
			}
			ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoTop);
			Update();
		}

		protected void FocusWhenVisible(object sender, EventArgs e)
		{
			if (!CanFocus)
			{
				return;
			}
			// It's possible that a focus box has been set up since we added this event handler.
			// If so we prefer to focus that.
			// But don't steal the focus from another window.  See FWR-1795.
			if (ParentForm == Form.ActiveForm)
			{
				var focusBox = (Controls.Cast<Control>().Where(c => c is FocusBoxController)).FirstOrDefault();
				if (focusBox != null)
				{
					focusBox.Focus();
				}
				else
				{
					Focus();
				}
			}
			VisibleChanged -= FocusWhenVisible;
		}

		/// <summary>
		/// Selects the specified AnalysisOccurrence in the interlinear text.
		/// </summary>
		protected internal IVwSelection SelectOccurrenceInIText(AnalysisOccurrence point)
		{
			Debug.Assert(point != null);
			Debug.Assert(m_hvoRoot != 0);
			var rgvsli = new SelLevInfo[3];
			rgvsli[0].ihvo = point.Index; // 0 specifies where wf is in segment.
			rgvsli[0].tag = SegmentTags.kflidAnalyses;
			rgvsli[1].ihvo = point.Segment.IndexInOwner; // 1 specifies where segment is in para
			rgvsli[1].tag = StTxtParaTags.kflidSegments;
			rgvsli[2].ihvo = point.Segment.Paragraph.IndexInOwner; // 2 specifies were para is in IStText.
			rgvsli[2].tag = StTextTags.kflidParagraphs;
			return MakeWordformSelection(rgvsli);
		}

		/// <summary>
		/// Get overridden for subclasses needing a Sandbox.
		/// </summary>
		protected virtual IVwSelection MakeWordformSelection(SelLevInfo[] rgvsli)
		{
			// top prop is atomic, leave index 0. Specifies displaying the contents of the Text.
			IVwSelection sel;
			try
			{
				// InterlinPrintChild and InterlinTaggingChild have no Sandbox,
				// so they need a "real" interlinear text selection.
				sel = RootBox.MakeTextSelInObj(0, rgvsli.Length, rgvsli, 0, null, false, false, false, true, true);
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.StackTrace);
				return null;
			}
			return sel;
		}

		#endregion

		internal virtual AnalysisOccurrence OccurrenceContainingSelection()
		{
			// This works fine for non-Sandbox panes,
			// Sandbox panes' selection may be in the Sandbox.
			var sel = RootBox?.Selection;
			return sel == null ? null : GetAnalysisFromSelection(sel);
		}

		protected AnalysisOccurrence GetAnalysisFromSelection(IVwSelection sel)
		{
			AnalysisOccurrence result = null;
			var cvsli = sel.CLevels(false);
			cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			// Main array of information retrieved from sel.
			var rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli, out _, out _, out _, out _, out _, out _, out _, out _, out _);
			if (rgvsli.Length <= 1)
			{
				return null;
			}
			// Need to loop backwards until we get down to index 1 or index produces a valid Segment.
			var i = rgvsli.Length - 1;
			ISegment seg = null;
			for (; i > 0; i--)
			{
				// get the container for whatever is selected at this level.
				if (!m_objRepo.TryGetObject(rgvsli[i].hvo, out var container))
				{
					return null; // may fail, e.g., trying to get bookmark for text just deleted.
				}
				seg = container as ISegment;
				if (seg != null)
				{
					break;
				}
			}
			if (seg != null && i > 0) // This checks the case where there is no Segment in the selection at all
			{
				// Make a new AnalysisOccurrence
				var selObject = m_objRepo.GetObject(rgvsli[i - 1].hvo);
				if (selObject is IAnalysis)
				{
					var indexInContainer = rgvsli[i - 1].ihvo;
					result = new AnalysisOccurrence(seg, indexInContainer);
				}
				if (result == null || !result.IsValid)
				{
					result = new AnalysisOccurrence(seg, 0);
				}
			}
			else
			{
				// TODO: other possibilities?!
				Debug.Assert(false, "Reached 'other' situation in OccurrenceContainingSelection().");
			}
			return result;
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			m_contextButton = new BlueCircleButton
			{
				ForeColor = BackColor,
				BackColor = BackColor
			};
			m_contextButton.Click += m_contextButton_Click;
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			RemoveContextButtonIfPresent();
			var sel = GrabMousePtSelectionToTest(e);
			if (UserClickedOnLabels(sel, out var ilineChoice))
			{
				SetContextButtonPosition(sel, ilineChoice);
			}
			if (e.Button == MouseButtons.Right)
			{
				ilineChoice = GetIndexOfLineChoice(sel);
				if (ilineChoice < 0)
				{
					base.OnMouseDown(e);
					return;
				}
				ShowContextMenuIfNotClosing(new Point(e.X, e.Y), ilineChoice);
			}
			base.OnMouseDown(e);
		}

		private void ShowContextMenuIfNotClosing(Point menuLocation, int ilineChoice)
		{
			// LT-4622 Make Configure Interlinear more accessible
			// User clicked on interlinear labels, so I need to
			// make a context menu and show it, if I'm not just closing one!
			// This time test seems to be the only way to find out whether this click closed the last one.
			if (DateTime.Now.Ticks - m_ticksWhenContextMenuClosed <= 50000)
			{
				// 5 ms
				return;
			}
			m_labelContextMenu = MakeContextMenu(ilineChoice);
			m_labelContextMenu.Closed += m_labelContextMenu_Closed;
			m_labelContextMenu.Show(this, menuLocation.X, menuLocation.Y);
		}

		private void SetContextButtonPosition(IVwSelection sel, int ilineChoice)
		{
			Debug.Assert(sel != null || !sel.IsValid, "No selection!");
			Rect rcPrimary;
			Rectangle rcSrcRoot;
			using (new HoldGraphics(this))
			{
				GetCoordRects(out rcSrcRoot, out var rcDstRoot);
				sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary, out _, out _, out _);
			}
			CalculateHorizContextButtonPosition(rcPrimary, rcSrcRoot);
			m_iLineChoice = ilineChoice;
			if (!Controls.Contains(m_contextButton))
			{
				Controls.Add(m_contextButton);
			}
		}

		private void CalculateHorizContextButtonPosition(Rect rcPrimary, Rect rcSrcRoot)
		{
			// Enhance GJM: Not perfect for RTL script, but I can't figure out how to
			// do it right just now.
			m_contextButton.Location = new Point(TextIsRightToLeft ? rcPrimary.left : rcSrcRoot.left, rcPrimary.top);
		}

		protected bool TextIsRightToLeft
		{
			get
			{
				var wsEngine = Cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(RootStText.MainWritingSystem);
				return wsEngine != null && wsEngine.RightToLeftScript;
			}
		}

		internal void RemoveContextButtonIfPresent()
		{
			m_iLineChoice = -1;
			if (Controls.Contains(m_contextButton))
			{
				Controls.Remove(m_contextButton);
			}
		}

		protected bool UserClickedOnLabels(IVwSelection selTest, out int ilineChoice)
		{
			ilineChoice = GetIndexOfLineChoice(selTest);
			return ilineChoice > -1;
		}

		/// <summary>
		/// Takes a mouse click point and makes an invisible selection for testing.
		/// Exceptions caused by selection problems are caught, but not dealt with.
		/// In case of an exception, the selection returned will be null.
		/// </summary>
		protected IVwSelection GrabMousePtSelectionToTest(MouseEventArgs e)
		{
			IVwSelection selTest = null;
			try
			{
				Point pt;
				Rectangle rcSrcRoot;
				Rectangle rcDstRoot;
				using (new HoldGraphics(this))
				{
					pt = PixelToView(new Point(e.X, e.Y));
					GetCoordRects(out rcSrcRoot, out rcDstRoot);
				}
				// Make an invisible selection to see if we are in editable text.
				selTest = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
			}
			catch
			{

			}
			return selTest;
		}

		protected int GetIndexOfLineChoice(IVwSelection selTest)
		{
			var helper = SelectionHelper.Create(selTest, this);
			if (helper?.SelProps == null)
			{
				return -1;
			}
			var props = helper.SelProps;
			return props.GetIntPropValues((int)FwTextPropType.ktptBulNumStartAt, out _);
		}

		#region Label Context Menu stuff

		private void m_contextButton_Click(object sender, EventArgs e)
		{
			Debug.Assert(m_iLineChoice > -1, "Why isn't this variable set?");
			if (m_iLineChoice > -1)
			{
				ShowContextMenuIfNotClosing(((Control)sender).Location, m_iLineChoice);
			}
		}

		protected virtual ContextMenuStrip MakeContextMenu(int ilineChoice)
		{
			var menu = new ContextMenuStrip();
			var isRibbonMenu = Vc.ToString() == "RibbonVc";
			// Menu items:
			// 1) Hide [name of clicked line]
			// 2) Add Writing System > (submenu of other wss for this line)
			// 3) Move Up
			// 4) Move Down
			// (separator)
			// 5) Add Line > (submenu of currently hidden lines)
			// 6) Configure Interlinear...
			if (Vc?.LineChoices != null && !isRibbonMenu) // just to be safe; shouldn't happen
			{
				if (!(Vc.LineChoices.Clone() is InterlinLineChoices curLineChoices))
				{
					return menu;
				}
				// 1) Hide [name of clicked line]
				if (curLineChoices.OkToRemove(ilineChoice))
				{
					AddHideLineMenuItem(menu, curLineChoices, ilineChoice);
				}
				// 2) Add Writing System > (submenu of other wss for this line)
				var addWsSubMenu = new ToolStripMenuItem(LanguageExplorerResources.ksAddWS);
				AddAdditionalWsMenuItem(addWsSubMenu, curLineChoices, ilineChoice);
				if (addWsSubMenu.DropDownItems.Count > 0)
				{
					menu.Items.Add(addWsSubMenu);
				}
				// 3) Move Up
				if (curLineChoices.OkToMoveUp(ilineChoice))
				{
					AddMoveUpMenuItem(menu, ilineChoice);
				}
				// 4) Move Down
				if (curLineChoices.OkToMoveDown(ilineChoice))
				{
					AddMoveDownMenuItem(menu, ilineChoice);
				}
				// Add menu separator here
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(menu);
				// 5) Add Line > (submenu of currently hidden lines)
				var addLineSubMenu = new ToolStripMenuItem(LanguageExplorerResources.ksAddLine);
				AddNewLineMenuItem(addLineSubMenu, curLineChoices);
				if (addLineSubMenu.DropDownItems.Count > 0)
				{
					menu.Items.Add(addLineSubMenu);
				}
			}
			// 6) Last, but not least, add a link to the Configure Interlinear dialog
			var configLink = new ToolStripMenuItem(LanguageExplorerResources.ksConfigureLinkText);
			configLink.Click += configLink_Click; // TODO: Figure out how to pass more parameters
			menu.Items.Add(configLink);
			return menu;
		}

		private void AddHideLineMenuItem(ContextMenuStrip menu, InterlinLineChoices curLineChoices, int ilineChoice)
		{
			var hideItem = new ToolStripMenuItem(string.Format(LanguageExplorerResources.ksHideLine, GetAppropriateLineLabel(curLineChoices, ilineChoice)));
			hideItem.Click += hideItem_Click;
			hideItem.Tag = ilineChoice;
			menu.Items.Add(hideItem);
		}

		private string GetAppropriateLineLabel(InterlinLineChoices curLineChoices, int ilineChoice)
		{
			var curSpec = curLineChoices[ilineChoice];
			var result = curLineChoices.LabelFor(curSpec.Flid);
			if (curLineChoices.RepetitionsOfFlid(curSpec.Flid) > 1)
			{
				result += "(" + curSpec.WsLabel(Cache).Text + ")";
			}
			return result;
		}

		private void AddAdditionalWsMenuItem(ToolStripMenuItem addSubMenu, InterlinLineChoices curLineChoices, int ilineChoice)
		{
			var curSpec = curLineChoices[ilineChoice];
			var choices = GetWsComboItems(curSpec);
			var curFlidDisplayedWss = curLineChoices.OtherWritingSystemsForFlid(curSpec.Flid, 0);
			var curRealWs = GetRealWsFromSpec(curSpec);
			if (!curFlidDisplayedWss.Contains(curRealWs))
			{
				curFlidDisplayedWss.Add(curRealWs);
			}
			var lgWsAcc = Cache.LanguageWritingSystemFactoryAccessor;
			foreach (var item in choices)
			{
				var itemRealWs = lgWsAcc.GetWsFromStr(item.Id);
				// Skip 'Magic' wss and ones that are already displayed
				if (itemRealWs == 0 || curFlidDisplayedWss.Contains(itemRealWs))
				{
					continue;
				}
				var menuItem = new AddWritingSystemMenuItem(curSpec.Flid, itemRealWs)
				{
					Text = item.ToString()
				};
				menuItem.Click += addWsToFlidItem_Click;
				addSubMenu.DropDownItems.Add(menuItem);
			}
		}

		private IEnumerable<WsComboItem> GetWsComboItems(InterlinLineSpec curSpec)
		{
			using (var dummyCombobox = new ComboBox())
			{
				var dummyCachedBoxes = new Dictionary<WsComboContent, ComboBox.ObjectCollection>();
				var comboObjects = ConfigureInterlinDialog.WsComboItemsInternal(Cache, dummyCombobox, dummyCachedBoxes, curSpec.ComboContent);
				var choices = new WsComboItem[comboObjects.Count];
				comboObjects.CopyTo(choices, 0);
				return choices;
			}
		}

		private int GetRealWsFromSpec(InterlinLineSpec spec)
		{
			if (!spec.IsMagicWritingSystem)
			{
				return spec.WritingSystem;
			}
			// special case, the only few we support so far (and only for a few fields).
			switch (spec.WritingSystem)
			{
				case WritingSystemServices.kwsFirstAnal:
					return Cache.LangProject.DefaultAnalysisWritingSystem.Handle;
				case WritingSystemServices.kwsVernInParagraph:
					return Cache.LangProject.DefaultVernacularWritingSystem.Handle; // REVIEW (Hasso) 2018.01: this is frequently the case, but not always
			}
			var ws = -50;
			try
			{
				ws = WritingSystemServices.InterpretWsLabel(Cache, spec.WsLabel(Cache).Text, null, 0, 0, null);
			}
			catch
			{
				Debug.Assert(ws != -50, "InterpretWsLabel was not able to interpret the Ws Label.  The most likely cause for this is that a magic ws was passed in.");
			}
			return ws;
		}

		private void AddMoveUpMenuItem(ContextMenuStrip menu, int ilineChoice)
		{
			var moveUpItem = new ToolStripMenuItem(LanguageExplorerResources.ksMoveUp) { Tag = ilineChoice };
			moveUpItem.Click += moveUpItem_Click;
			menu.Items.Add(moveUpItem);
		}

		private void AddMoveDownMenuItem(ContextMenuStrip menu, int ilineChoice)
		{
			var moveDownItem = new ToolStripMenuItem(LanguageExplorerResources.ksMoveDown) { Tag = ilineChoice };
			moveDownItem.Click += moveDownItem_Click;
			menu.Items.Add(moveDownItem);
		}

		private void AddNewLineMenuItem(ToolStripMenuItem addLineSubMenu, InterlinLineChoices curLineChoices)
		{
			// Add menu options to add lines of flids that are in default list, but don't currently appear.
			var unusedSpecs = GetUnusedSpecs(curLineChoices);
			foreach (var specToAdd in unusedSpecs)
			{
				var menuItem = new AddLineMenuItem(specToAdd.Flid) { Text = specToAdd.ToString() };
				menuItem.Click += addLineItem_Click;
				addLineSubMenu.DropDownItems.Add(menuItem);
			}
		}

		private static IEnumerable<LineOption> GetUnusedSpecs(InterlinLineChoices curLineChoices)
		{
			var allOptions = curLineChoices.LineOptions();
			var optionsUsed = curLineChoices.ItemsWithFlids(allOptions.Select(lineOption => lineOption.Flid).ToArray());
			return allOptions.Where(option => optionsUsed.All(spec => spec.Flid != option.Flid)).ToList();
		}

		#region Menu Event Handlers

		private void hideItem_Click(object sender, EventArgs e)
		{
			var ilineToHide = (int)(((ToolStripMenuItem)sender).Tag);
			if (Vc.LineChoices.Clone() is InterlinLineChoices newLineChoices)
			{
				newLineChoices.Remove(newLineChoices[ilineToHide]);
				UpdateForNewLineChoices(newLineChoices);
			}
			RemoveContextButtonIfPresent(); // it will still have a spurious choice to hide the line we just hid; clicking may crash.
		}

		private void addWsToFlidItem_Click(object sender, EventArgs e)
		{
			if (!(sender is AddWritingSystemMenuItem menuItem))
			{
				return; // Impossible?
			}
			var flid = menuItem.Flid;
			var wsToAdd = menuItem.Ws;
			if (Vc.LineChoices.Clone() is InterlinLineChoices newLineChoices)
			{
				newLineChoices.Add(flid, wsToAdd);
				UpdateForNewLineChoices(newLineChoices);
			}
		}

		private void moveUpItem_Click(object sender, EventArgs e)
		{
			var ilineToHide = (int)((ToolStripMenuItem)sender).Tag;
			if (Vc.LineChoices.Clone() is InterlinLineChoices newLineChoices)
			{
				newLineChoices.MoveUp(ilineToHide);
				UpdateForNewLineChoices(newLineChoices);
			}
		}

		private void moveDownItem_Click(object sender, EventArgs e)
		{
			var ilineToHide = (int)(((ToolStripMenuItem)sender).Tag);
			if (Vc.LineChoices.Clone() is InterlinLineChoices newLineChoices)
			{
				newLineChoices.MoveDown(ilineToHide);
				UpdateForNewLineChoices(newLineChoices);
			}
		}

		private void addLineItem_Click(object sender, EventArgs e)
		{
			var menuItem = sender as AddLineMenuItem;
			if (menuItem == null)
			{
				return; // Impossible?
			}
			var flid = menuItem.Flid;
			if (Vc.LineChoices.Clone() is InterlinLineChoices newLineChoices && m_cache.GetManagedMetaDataCache().FieldExists(flid))
			{
				newLineChoices.Add(flid);
				UpdateForNewLineChoices(newLineChoices);
			}
		}

		private void configLink_Click(object sender, EventArgs e)
		{
			((IInterlinearConfigurator)this).ConfigureInterlinear();
		}

		private void m_labelContextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			m_ticksWhenContextMenuClosed = DateTime.Now.Ticks;
		}

		#endregion

		#endregion

		/// <summary>
		/// True if we will be doing editing (display sandbox, restrict field order choices, etc.).
		/// </summary>
		public bool ForEditing { get; set; }

		/// <summary>
		/// The property table key storing InterlinLineChoices used by our display.
		/// Parent controls (e.g. InterlinMaster) should pass in their own property
		/// to configure for contexts it knows about.
		/// </summary>
		private string ConfigPropName { get; set; }

		/// <summary />
		/// <param name="lineConfigPropName">the key used to store/restore line configuration settings.</param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public InterlinLineChoices SetupLineChoices(string lineConfigPropName, InterlinMode mode)
		{
			ConfigPropName = lineConfigPropName;
			if (!TryRestoreLineChoices(out var lineChoices))
			{
				if (ForEditing)
				{
					lineChoices = EditableInterlinLineChoices.DefaultChoices(m_cache.LangProject, WritingSystemServices.kwsVernInParagraph, WritingSystemServices.kwsAnal);
					lineChoices.Mode = mode;
					if (mode == InterlinMode.Gloss || mode == InterlinMode.GlossAddWordsToLexicon)
					{
						lineChoices.SetStandardGlossState();
					}
					else
					{
						lineChoices.SetStandardState();
					}
				}
				else
				{
					lineChoices = InterlinLineChoices.DefaultChoices(m_cache.LangProject, WritingSystemServices.kwsVernInParagraph, WritingSystemServices.kwsAnal, mode);
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

		/// <summary>
		/// This is for setting m_vc.LineChoices even before we have a valid vc.
		/// </summary>
		internal InterlinLineChoices LineChoices { get; set; }

		/// <summary>
		/// Tries to restore the LineChoices saved in the ConfigPropName property in the property table.
		/// </summary>
		internal bool TryRestoreLineChoices(out InterlinLineChoices lineChoices)
		{
			lineChoices = null;
			var persist = PropertyTable.GetValue<string>(ConfigPropName, SettingsGroup.LocalSettings);
			if (persist != null)
			{
				lineChoices = InterlinLineChoices.Restore(persist, m_cache.LanguageWritingSystemFactoryAccessor, m_cache.LangProject, WritingSystemServices.kwsVernInParagraph, m_cache.DefaultAnalWs, InterlinMode.Analyze, PropertyTable, ConfigPropName);
			}
			return persist != null && lineChoices != null;
		}

		/// <summary>
		///  Launch the Configure interlinear dialog and deal with the results
		/// </summary>
		void IInterlinearConfigurator.ConfigureInterlinear()
		{
			using (var dlg = new ConfigureInterlinDialog(m_cache, PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), Vc.LineChoices.Clone() as InterlinLineChoices))
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					UpdateForNewLineChoices(dlg.Choices);
				}
			}
		}

		/// <summary>
		/// Persist the new line choices and
		/// Reconstruct the document based on the given newChoices for interlinear lines.
		/// </summary>
		internal virtual void UpdateForNewLineChoices(InterlinLineChoices newChoices)
		{
			Vc.LineChoices = newChoices;
			LineChoices = newChoices;

			PersistAndDisplayChangedLineChoices();
		}

		internal void PersistAndDisplayChangedLineChoices()
		{
			PropertyTable.SetProperty(ConfigPropName, Vc.LineChoices.Persist(m_cache.LanguageWritingSystemFactoryAccessor), true, true, SettingsGroup.LocalSettings);
			UpdateDisplayForNewLineChoices();
		}

		/// <summary>
		/// Do whatever is necessary to display new line choices.
		/// </summary>
		private void UpdateDisplayForNewLineChoices()
		{
			RootBox?.Reconstruct();
		}

		/// <summary>
		/// Update any necessary guesses when the specified wordforms change.
		/// </summary>
		internal virtual void UpdateGuesses(HashSet<IWfiWordform> wordforms)
		{
			UpdateGuesses(wordforms, true);
		}

		private void UpdateGuesses(HashSet<IWfiWordform> wordforms, bool fUpdateDisplayWhereNeeded)
		{
			// now update the guesses for the paragraphs.
			var pdut = new ParaDataUpdateTracker(Vc.GuessServices, Vc.Decorator);
			foreach (IStTxtPara para in RootStText.ParagraphsOS)
			{
				pdut.LoadAnalysisData(para, wordforms);
			}
			if (fUpdateDisplayWhereNeeded)
			{
				// now update the display with the affected annotations.
				foreach (var changed in pdut.ChangedAnnotations)
				{
					UpdateDisplayForOccurrence(changed);
				}
			}
		}

		/// <summary>
		/// Update all the guesses in the interlinear doc.
		/// </summary>
		internal virtual void UpdateGuessData()
		{
			UpdateGuesses(null, false);
		}

		protected void UpdateDisplayForOccurrence(AnalysisOccurrence occurrence)
		{
			if (occurrence == null)
			{
				return;
			}
			// Simulate replacing the wordform in the relevant segment with itself. This lets the VC Display method run again, this
			// time possibly getting a different answer about whether hvoAnnotation is the current annotation, or about the
			// size of the Sandbox.
			RootBox.PropChanged(occurrence.Segment.Hvo, SegmentTags.kflidAnalyses, occurrence.Index, 1, 1);
		}

		internal InterlinMaster GetMaster()
		{
			for (var parentControl = Parent; parentControl != null; parentControl = parentControl.Parent)
			{
				if (parentControl is InterlinMaster master)
				{
					return master;
				}
			}
			return null;
		}

		#region implemention of IChangeRootObject
		public virtual void SetRoot(int hvo)
		{
			EnsureVc();
			if (LineChoices != null)
			{
				Vc.LineChoices = LineChoices;
			}
			SetRootInternal(hvo);
			AddDecorator();
			RemoveContextButtonIfPresent(); // Don't want to keep the context button for a different text!
		}

		/// <summary>
		/// Returns the rootbox of this object, or null if not applicable
		/// </summary>
		public IVwRootBox GetRootBox()
		{
			return RootBox;
		}

		#endregion
		/// <summary>
		/// Allows InterlinTaggingChild to add a DomainDataByFlid decorator to the rootbox.
		/// </summary>
		protected virtual void AddDecorator()
		{
			// by default, just use the InterinVc decorator.
			if (RootBox != null)
			{
				RootBox.DataAccess = Vc.Decorator;
			}
		}

		protected virtual void SetRootInternal(int hvo)
		{
			// since we are rebuilding the display, reset our sandbox mask.
			m_hvoRoot = hvo;
			if (hvo != 0)
			{
				RootStText = Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(hvo);
				// Here we force the text to be reparsed even if we think it is already parsed.
				// This is partly for safety, but mainly so we can guess phrases.
				// AnalysisAdjuster does not try to guess phrases, so an analysis it has adjusted
				// may miss a phrase that has come into existence because of an edit.
				// Even if we could fix that, a paragraph that was once correctly parsed and has
				// not changed since could be missing some possible phrase guesses, if those phrases
				// have been created since the previous parse.
				// Enhance JohnT: The problem is that this can be slow! Especially when using this
				// as a display view in a concordance. Should we detect that somehow, and in that
				// case only parse paragraphs that need it?
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => RootStText.LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(true));
				// Sync Guesses data before we redraw anything.
				UpdateGuessData();
			}
			// FWR-191: we don't need to reconstruct the display if we didn't need to reload annotations
			// but until we detect that condition, we need to redisplay just in case, to keep things in sync.
			// especially if someone edited the baseline.
			ChangeOrMakeRoot(m_hvoRoot, Vc, InterlinVc.kfragStText, m_styleSheet);
			Vc.RootSite = this;
		}

		#region IVwNotifyChange Members

		internal bool SuspendResettingAnalysisCache;

		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			//If the RootStText is null we are either in a place that doesn't care about parser related updates
			// or we are not yet completely displaying the text, so we should be fine, I hope? (LT-12493)
			if (SuspendResettingAnalysisCache || RootStText == null)
			{
				return;
			}

			switch (tag)
			{
				case WfiAnalysisTags.kflidEvaluations:
					var analysis = m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().GetObject(hvo);
					if (analysis.HasWordform && RootStText.UniqueWordforms().Contains(analysis.Wordform))
					{
						m_wordformsToUpdate.Add(analysis.Wordform);
						PropertyTable.GetValue<IFwMainWnd>(FwUtilsConstants.window).IdleQueue.Add(IdleQueuePriority.High, PostponedUpdateWordforms);
					}
					break;
				case WfiWordformTags.kflidAnalyses:
					var wordform = m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().GetObject(hvo);
					if (RootStText.UniqueWordforms().Contains(wordform))
					{
						m_wordformsToUpdate.Add(wordform);
						PropertyTable.GetValue<IFwMainWnd>(FwUtilsConstants.window).IdleQueue.Add(IdleQueuePriority.High, PostponedUpdateWordforms);
					}
					break;
			}
		}

		private bool PostponedUpdateWordforms(object parameter)
		{
			if (IsDisposed)
			{
				throw new InvalidOperationException("Thou shalt not call methods after I am disposed!");
			}
			Vc.GuessServices.ClearGuessData();
			UpdateWordforms(m_wordformsToUpdate);
			m_wordformsToUpdate.Clear();
			return true;
		}

		protected virtual void UpdateWordforms(HashSet<IWfiWordform> wordforms)
		{
			UpdateGuesses(wordforms, true);
		}

		#endregion
		internal void UpdatingOccurrence(IAnalysis oldAnalysis, IAnalysis newAnalysis)
		{
			Vc.UpdatingOccurrence(oldAnalysis, newAnalysis);
		}

		protected internal IStText RootStText { get; set; }

		internal static int GetParagraphIndexForAnalysis(AnalysisOccurrence point)
		{
			return point.Segment.Paragraph.IndexInOwner;
		}

		#region IHandleBookmark Members

		public void SelectBookmark(IStTextBookmark bookmark)
		{
			SelectOccurrence(ConvertBookmarkToAnalysis(bookmark));
		}

		/// <summary>
		/// Returns an AnalysisOccurrence at least close to the given bookmark.
		/// If we can't, we return null.
		/// </summary>
		internal AnalysisOccurrence ConvertBookmarkToAnalysis(IStTextBookmark bookmark)
		{
			return ConvertBookmarkToAnalysis(bookmark, out _);
		}

		/// <summary>
		/// Returns an AnalysisOccurrence at least close to the given bookmark.
		/// If we can't, we return null. This version reports whether we found an exact match or not.
		/// </summary>
		internal AnalysisOccurrence ConvertBookmarkToAnalysis(IStTextBookmark bookmark, out bool fExactMatch)
		{
			fExactMatch = false;
			if (RootStText == null || RootStText.ParagraphsOS.Count == 0 || bookmark.IndexOfParagraph < 0 || bookmark.BeginCharOffset < 0 || bookmark.IndexOfParagraph >= RootStText.ParagraphsOS.Count)
			{
				return null;
			}
			var para = RootStText.ParagraphsOS[bookmark.IndexOfParagraph] as IStTxtPara;
			if (para == null)
			{
				return null;
			}
			var point = SegmentServices.FindNearestAnalysis(para, bookmark.BeginCharOffset, bookmark.EndCharOffset, out fExactMatch);
			if (point == null || !(point.Analysis is IPunctuationForm))
			{
				return point;
			}
			// Don't want to return punctuation! Wordform or null!
			fExactMatch = false;
			return point.Index > 0 ? point.PreviousWordform() : point.NextWordform();
		}

		#endregion

		/// <summary>
		/// Updates the paragraphs interlinear data and collects which annotations
		/// have been affected so we can update the display appropriately.
		/// </summary>
		private sealed class ParaDataUpdateTracker : InterlinViewCacheLoader
		{
			private HashSet<AnalysisOccurrence> m_annotationsChanged = new HashSet<AnalysisOccurrence>();
			private AnalysisOccurrence m_currentAnnotation;
			private HashSet<int> m_analysesWithNewGuesses = new HashSet<int>();

			internal ParaDataUpdateTracker(AnalysisGuessServices guessServices, InterlinViewDataCache sdaDecorator) :
				base(guessServices, sdaDecorator)
			{
			}

			protected override void NoteCurrentAnnotation(AnalysisOccurrence occurrence)
			{
				m_currentAnnotation = occurrence;
				base.NoteCurrentAnnotation(occurrence);
			}

			private void MarkCurrentAnnotationAsChanged()
			{
				// something has changed in the cache for the annotation or its analysis,
				// so mark it as changed.
				m_annotationsChanged.Add(m_currentAnnotation);
			}

			/// <summary>
			/// the annotations that have changed, or their analysis, in the cache
			/// and for which we need to do propchanges to update the display
			/// </summary>
			internal IList<AnalysisOccurrence> ChangedAnnotations => m_annotationsChanged.ToList();

			protected override void SetObjProp(int hvo, int flid, int newObjValue)
			{
				var oldObjValue = Decorator.get_ObjectProp(hvo, flid);
				if (oldObjValue != newObjValue)
				{
					base.SetObjProp(hvo, flid, newObjValue);
					m_analysesWithNewGuesses.Add(hvo);
					MarkCurrentAnnotationAsChanged();
					return;
				}
				// If we find more than one occurrence of the same analysis, only the first time
				// will its guess change. But all of them need to be updated! So any occurrence whose
				// guess has changed needs to be marked as changed.
				if (m_currentAnnotation != null && m_currentAnnotation.Analysis != null && m_analysesWithNewGuesses.Contains(m_currentAnnotation.Analysis.Hvo))
				{
					MarkCurrentAnnotationAsChanged();
				}
			}

			protected override void SetInt(int hvo, int flid, int newValue)
			{
				var oldValue = Decorator.get_IntProp(hvo, flid);
				if (oldValue == newValue)
				{
					return;
				}
				base.SetInt(hvo, flid, newValue);
				MarkCurrentAnnotationAsChanged();
			}
		}

		/// <summary>
		/// Used for Interlinear context menu items to Add a new InterlinLineSpec
		/// for a flid that is currently hidden.
		/// </summary>
		private sealed class AddLineMenuItem : ToolStripMenuItem
		{
			/// <summary />
			internal AddLineMenuItem(int flid)
			{
				Flid = flid;
			}

			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
				base.Dispose(disposing);
			}

			internal int Flid { get; }
		}

		/// <summary>
		/// Used for Interlinear context menu items to Add a new WritingSystem
		/// for a flid that is already visible.
		/// </summary>
		private sealed class AddWritingSystemMenuItem : ToolStripMenuItem
		{
			/// <summary />
			internal AddWritingSystemMenuItem(int flid, int ws)
			{
				Flid = flid;
				Ws = ws;
			}

			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
				base.Dispose(disposing);
			}

			internal int Flid { get; }

			internal int Ws { get; }
		}
	}
}