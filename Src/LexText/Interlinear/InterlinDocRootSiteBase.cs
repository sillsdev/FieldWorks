using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using XCore;
using System.Linq;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Ideally this would be an abstract class, but Designer does not handle abstract classes.
	/// </summary>
	public partial class InterlinDocRootSiteBase : RootSite,
		IInterlinearTabControl, IVwNotifyChange, IHandleBookmark, ISelectOccurrence, IStyleSheet, ISetupLineChoices
	{
		private ISilDataAccess m_sda;
		protected internal int m_hvoRoot; // IStText
		protected InterlinVc m_vc;
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
		private long m_ticksWhenContextMenuClosed = 0;

		private readonly HashSet<IWfiWordform> m_wordformsToUpdate;

		public InterlinDocRootSiteBase()
		{
			m_wordformsToUpdate = new HashSet<IWfiWordform>();
			InitializeComponent();
		}

		public override void MakeRoot()
		{
			if (m_fdoCache == null || DesignMode)
				return;

			MakeRootInternal();

			base.MakeRoot();
		}

		protected virtual void MakeRootInternal()
		{
			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			// Setting this result too low can result in moving a cursor from an editable field
			// to a non-editable field (e.g. with Control-Right and Control-Left cursor
			// commands).  Normally we could set this to only a few (e.g. 4). but in
			// Interlinearizer we may want to jump from one sentence annotation to the next over
			// several read-only paragraphs  contained in a word bundle.  Make sure that
			// procedures that use this limit do not move the cursor from an editable to a
			// non-editable field.
			m_rootb.MaxParasToScan = 2000;

			EnsureVc();

			// We want to get notified when anything changes.
			m_sda = m_fdoCache.MainCacheAccessor;
			m_sda.AddNotification(this);

			m_vc.ShowMorphBundles = m_mediator.PropertyTable.GetBoolProperty("ShowMorphBundles", true);
			m_vc.LineChoices = LineChoices;
			m_vc.ShowDefaultSense = true;

			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;
			m_rootb.SetRootObject(m_hvoRoot, m_vc, InterlinVc.kfragStText, m_styleSheet);
			m_objRepo = m_fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>();
		}

		public bool OnDisplayExportInterlinear(object commandObject, ref UIItemDisplayProperties display)
		{
			if (m_hvoRoot != 0)
				display.Enabled = true;
			else
				display.Enabled = false;
			display.Visible = true;
			return true;
		}

		public bool OnExportInterlinear(object argument)
		{
			// If the currently selected text is from Scripture, then we need to give the dialog
			// the list of Scripture texts that have been selected for interlinearization.
			var parent = this.Parent;
			while (parent != null && !(parent is InterlinMaster))
				parent = parent.Parent;
			var master = parent as InterlinMaster;
			var selectedObjs = new List<ICmObject>();
			IBookImporter bookImporter = null;
			if (master != null)
			{
				bookImporter = master.Clerk as IBookImporter;
				var clerk = master.Clerk as InterlinearTextsRecordClerk;
				if (clerk != null)
				{
					foreach (int hvo in clerk.GetScriptureIds())
						selectedObjs.Add(m_objRepo.GetObject(hvo));
				}
			}
			//AnalysisOccurrence analOld = OccurrenceContainingSelection();
			bool fFocusBox = TryHideFocusBoxAndUninstall();
			ICmObject objRoot = m_objRepo.GetObject(m_hvoRoot);
			using (var dlg = new InterlinearExportDialog(m_mediator, objRoot, m_vc, bookImporter))
			{
				dlg.ShowDialog(this);
			}
			if (fFocusBox)
			{
				CreateFocusBox();
				//int hvoAnalysis = m_fdoCache.MainCacheAccessor.get_ObjectProp(oldAnnotation, CmAnnotationTags.kflidInstanceOf);
				//TriggerAnnotationSelected(oldAnnotation, hvoAnalysis, false);
			}

			return true; // we handled this
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

		/// <summary>
		///
		/// </summary>
		protected virtual void MakeVc()
		{
			throw new NotImplementedException();
		}

		private void EnsureVc()
		{
			if (m_vc == null)
				MakeVc();
		}

		#region ISelectOccurrence

		/// <summary>
		/// This base version is used by 'read-only' tabs that need to select an
		/// occurrence in IText from the analysis occurrence. Override for Sandbox-type selections.
		/// </summary>
		/// <param name="point"></param>
		public virtual void SelectOccurrence(AnalysisOccurrence point)
		{
			if (point == null)
				return;
			Debug.Assert(point.HasWordform,
				"Given annotation type should have wordform but was " + point + ".");

			// The following will select the occurrence, ... I hope!
			// Scroll to selection into view
			var sel = SelectOccurrenceInIText(point);
			if (sel == null)
				return;
			//sel.Install();
			m_rootb.Activate(VwSelectionState.vssEnabled);
			// Don't steal the focus from another window.  See FWR-1795.
			if (!Focused && ParentForm == Form.ActiveForm)
			{
				if (CanFocus)
					Focus();
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
			if (CanFocus)
			{
				// It's possible that a focus box has been set up since we added this event handler.
				// If so we prefer to focus that.
				// But don't steal the focus from another window.  See FWR-1795.
				if (ParentForm == Form.ActiveForm)
				{
					var focusBox = (from Control c in Controls where c is FocusBoxController select c).FirstOrDefault();
					if (focusBox != null)
						focusBox.Focus();
					else
						Focus();
				}
				VisibleChanged -= FocusWhenVisible;
			}
		}

		/// <summary>
		/// Selects the specified AnalysisOccurrence in the interlinear text.
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
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
		/// <param name="rgvsli"></param>
		/// <returns></returns>
		protected virtual IVwSelection MakeWordformSelection(SelLevInfo[] rgvsli)
		{
			// top prop is atomic, leave index 0. Specifies displaying the contents of the Text.
			IVwSelection sel;
			try
			{
				// InterlinPrintChild and InterlinTaggingChild have no Sandbox,
				// so they need a "real" interlinear text selection.
				sel = RootBox.MakeTextSelInObj(0, rgvsli.Length, rgvsli, 0, null,
											   false, false, false, true, true);
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
			if (m_rootb == null)
				return null;

			// This works fine for non-Sandbox panes,
			// Sandbox panes' selection may be in the Sandbox.
			var sel = m_rootb.Selection;
			return sel == null ? null : GetAnalysisFromSelection(sel);
		}

		protected AnalysisOccurrence GetAnalysisFromSelection(IVwSelection sel)
		{
			AnalysisOccurrence result = null;

			var cvsli = sel.CLevels(false);
			cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.

			// Out variables for AllTextSelInfo.
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ws;
			bool fAssocPrev;
			int ihvoEnd;
			ITsTextProps ttpBogus;
			// Main array of information retrieved from sel.
			var rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli,
						   out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
						   out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);

			if (rgvsli.Length > 1)
			{
				// Need to loop backwards until we get down to index 1 or index produces a valid Segment.
				var i = rgvsli.Length - 1;
				ISegment seg = null;
				for (; i > 0; i--)
				{
					// get the container for whatever is selected at this level.
					ICmObject container;
					if (!m_objRepo.TryGetObject(rgvsli[i].hvo, out container))
						return null; // may fail, e.g., trying to get bookmark for text just deleted.

					seg = container as ISegment;
					if (seg != null)
						break;
				}
				if (seg != null && i > 0) // This checks the case where there is no Segment in the selection at all
				{
					// Make a new AnalysisOccurrence
					var selObject = m_objRepo.GetObject(rgvsli[i-1].hvo);
					if (selObject is IAnalysis)
					{
						var indexInContainer = rgvsli[i-1].ihvo;
						result = new AnalysisOccurrence(seg, indexInContainer);
					}
					if (result == null || !result.IsValid)
						result = new AnalysisOccurrence(seg, 0);
				}
				else
				{
					// TODO: other possibilities?!
					Debug.Assert(false, "Reached 'other' situation in OccurrenceContainingSelection().");
				}
			}
			return result;
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			m_contextButton = new BlueCircleButton();
			m_contextButton.ForeColor = BackColor;
			m_contextButton.BackColor = BackColor;
			m_contextButton.Click += m_contextButton_Click;
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			RemoveContextButtonIfPresent();
			var ilineChoice = -1;
			var sel = GrabMousePtSelectionToTest(e);
			if (UserClickedOnLabels(sel, out ilineChoice))
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
			if (DateTime.Now.Ticks - m_ticksWhenContextMenuClosed > 50000) // 5ms!
			{
				m_labelContextMenu = MakeContextMenu(ilineChoice);
				m_labelContextMenu.Closed += m_labelContextMenu_Closed;
				m_labelContextMenu.Show(this, menuLocation.X, menuLocation.Y);
			}
		}

		private void SetContextButtonPosition(IVwSelection sel, int ilineChoice)
		{
			Debug.Assert(sel != null || !sel.IsValid, "No selection!");
			//sel.GrowToWord();
			Rect rcPrimary;
			Rectangle rcSrcRoot;
			using (new HoldGraphics(this))
			{
				Rect rcSec;
				bool fSplit, fEndBeforeAnchor;
				Rectangle rcDstRoot;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary,
					out rcSec, out fSplit, out fEndBeforeAnchor);
			}
			CalculateHorizContextButtonPosition(rcPrimary, rcSrcRoot);
			m_iLineChoice = ilineChoice;
			if (!Controls.Contains(m_contextButton))
				Controls.Add(m_contextButton);
		}

		private void CalculateHorizContextButtonPosition(Rect rcPrimary, Rect rcSrcRoot)
		{
			// Enhance GJM: Not perfect for RTL script, but I can't figure out how to
			// do it right just now.
			var horizPosition = TextIsRightToLeft ? rcPrimary.left : rcSrcRoot.left;
			m_contextButton.Location = new Point(horizPosition, rcPrimary.top);
		}

		protected bool TextIsRightToLeft
		{
			get
			{
				var rootWs = RootStText.MainWritingSystem;
				var wsEngine = Cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(rootWs);
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
		/// <param name="e"></param>
		/// <returns></returns>
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
				selTest = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
			}
			catch
			{

			}
			return selTest;
		}

		protected int GetIndexOfLineChoice(IVwSelection selTest)
		{
			var helper = SelectionHelper.Create(selTest, this);
			if (helper == null)
				return -1;

			var props = helper.SelProps;
			int dummyvar;
			return props.GetIntPropValues((int)FwTextPropType.ktptBulNumStartAt, out dummyvar);
		}

		#region Label Context Menu stuff

		void m_contextButton_Click(object sender, EventArgs e)
		{
			Debug.Assert(m_iLineChoice > -1, "Why isn't this variable set?");
			if (m_iLineChoice > -1)
			{
				ShowContextMenuIfNotClosing(((Control)sender).Location, m_iLineChoice);
			}
		}

		private ContextMenuStrip MakeContextMenu(int ilineChoice)
		{
			var menu = new ContextMenuStrip();
			// Menu items:
			// 1) Hide [name of clicked line]
			// 2) Add Writing System > (submenu of other wss for this line)
			// 3) Move Up
			// 4) Move Down
			// (separator)
			// 5) Add Line > (submenu of currently hidden lines)
			// 6) Configure Interlinear...

			if (m_vc != null && m_vc.LineChoices != null) // just to be safe; shouldn't happen
			{
				var curLineChoices = m_vc.LineChoices.Clone() as InterlinLineChoices;
				if (curLineChoices == null)
					return menu;

				// 1) Hide [name of clicked line]
				if (curLineChoices.OkToRemove(ilineChoice))
					AddHideLineMenuItem(menu, curLineChoices, ilineChoice);

				// 2) Add Writing System > (submenu of other wss for this line)
				var addWsSubMenu = new ToolStripMenuItem(ITextStrings.ksAddWS);
				AddAdditionalWsMenuItem(addWsSubMenu, curLineChoices, ilineChoice);
				if (addWsSubMenu.DropDownItems.Count > 0)
					menu.Items.Add(addWsSubMenu);

				// 3) Move Up
				if (curLineChoices.OkToMoveUp(ilineChoice))
					AddMoveUpMenuItem(menu, ilineChoice);

				// 4) Move Down
				if (curLineChoices.OkToMoveDown(ilineChoice))
					AddMoveDownMenuItem(menu, ilineChoice);

				// Add menu separator here
				menu.Items.Add(new ToolStripSeparator());

				// 5) Add Line > (submenu of currently hidden lines)
				var addLineSubMenu = new ToolStripMenuItem(ITextStrings.ksAddLine);
				AddNewLineMenuItem(addLineSubMenu, curLineChoices);
				if (addLineSubMenu.DropDownItems.Count > 0)
					menu.Items.Add(addLineSubMenu);
			}

			// 6) Last, but not least, add a link to the Configure Interlinear dialog
			var configLink = new ToolStripMenuItem(ITextStrings.ksConfigureLinkText);
			configLink.Click += new EventHandler(configLink_Click);
			menu.Items.Add(configLink);

			return menu;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ToolStripMenuItem added to menu.Items collection and disposed there")]
		private void AddHideLineMenuItem(ContextMenuStrip menu,
			InterlinLineChoices curLineChoices, int ilineChoice)
		{
			var lineLabel = GetAppropriateLineLabel(curLineChoices, ilineChoice);
			var hideItem = new ToolStripMenuItem(String.Format(ITextStrings.ksHideLine, lineLabel));
			hideItem.Click += new EventHandler(hideItem_Click);
			hideItem.Tag = ilineChoice;
			menu.Items.Add(hideItem);
		}

		private string GetAppropriateLineLabel(InterlinLineChoices curLineChoices, int ilineChoice)
		{
			var curSpec = curLineChoices[ilineChoice];
			var result = curLineChoices.LabelFor(curSpec.Flid);
			if (curLineChoices.RepetitionsOfFlid(curSpec.Flid) > 1)
				result += "(" + curSpec.WsLabel(Cache).Text + ")";
			return result;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="menuItem added to addSubMenu.DropDownItems collection and disposed there")]
		private void AddAdditionalWsMenuItem(ToolStripMenuItem addSubMenu,
			InterlinLineChoices curLineChoices, int ilineChoice)
		{
			var curSpec = curLineChoices[ilineChoice];
			var choices = GetWsComboItems(curSpec);
			var curFlidDisplayedWss = curLineChoices.OtherWritingSystemsForFlid(curSpec.Flid, 0);
			var curRealWs = GetRealWsFromSpec(curSpec);
			if (!curFlidDisplayedWss.Contains(curRealWs))
				curFlidDisplayedWss.Add(curRealWs);
			var lgWsAcc = Cache.LanguageWritingSystemFactoryAccessor;
			foreach (var item in choices)
			{
				var itemRealWs = lgWsAcc.GetWsFromStr(item.Id);
				// Skip 'Magic' wss and ones that are already displayed
				if (itemRealWs == 0 || curFlidDisplayedWss.Contains(itemRealWs))
					continue;
				var menuItem = new AddWritingSystemMenuItem(curSpec.Flid, itemRealWs);
				menuItem.Text = item.ToString();
				menuItem.Click += new EventHandler(addWsToFlidItem_Click);
				addSubMenu.DropDownItems.Add(menuItem);
			}
		}

		private IEnumerable<WsComboItem> GetWsComboItems(InterlinLineSpec curSpec)
		{
			using (var dummyCombobox = new ComboBox())
			{
				var dummyCachedBoxes = new Dictionary<ColumnConfigureDialog.WsComboContent, ComboBox.ObjectCollection>();
				var comboObjects = ConfigureInterlinDialog.WsComboItemsInternal(
				Cache, dummyCombobox, dummyCachedBoxes, curSpec.ComboContent);
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
			if (spec.WritingSystem == WritingSystemServices.kwsFirstAnal)
				return Cache.LangProject.DefaultAnalysisWritingSystem.Handle;
			if (spec.WritingSystem == WritingSystemServices.kwsVernInParagraph)
				return Cache.LangProject.DefaultVernacularWritingSystem.Handle;
			int ws = -50;
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

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ToolStripMenuItem gets added to menu.Items collection and disposed there")]
		private void AddMoveUpMenuItem(ContextMenuStrip menu, int ilineChoice)
		{
			var moveUpItem = new ToolStripMenuItem(ITextStrings.ksMoveUp) { Tag = ilineChoice };
			moveUpItem.Click += moveUpItem_Click;
			menu.Items.Add(moveUpItem);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ToolStripMenuItem gets added to menu.Items collection and disposed there")]
		private void AddMoveDownMenuItem(ContextMenuStrip menu, int ilineChoice)
		{
			var moveDownItem = new ToolStripMenuItem(ITextStrings.ksMoveDown) { Tag = ilineChoice };
			moveDownItem.Click += moveDownItem_Click;
			menu.Items.Add(moveDownItem);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="AddLineMenuItem gets added to addLineSubMenu.DropDownItems collection and disposed there")]
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
			var optionsUsed = curLineChoices.ItemsWithFlids(
				allOptions.Select(lineOption => lineOption.Flid).ToArray());
			return allOptions.Where(option => !optionsUsed.Any(
				spec => spec.Flid == option.Flid)).ToList();
		}

		#region Menu Event Handlers

		private void hideItem_Click(object sender, EventArgs e)
		{
			var ilineToHide = (int) (((ToolStripMenuItem) sender).Tag);
			var newLineChoices = m_vc.LineChoices.Clone() as InterlinLineChoices;
			if (newLineChoices != null)
			{
				newLineChoices.Remove(newLineChoices[ilineToHide]);
				UpdateForNewLineChoices(newLineChoices);
			}
			RemoveContextButtonIfPresent(); // it will still have a spurious choice to hide the line we just hid; clicking may crash.
		}

		private void addWsToFlidItem_Click(object sender, EventArgs e)
		{
			var menuItem = sender as AddWritingSystemMenuItem;
			if (menuItem == null)
				return; // Impossible?

			var flid = menuItem.Flid;
			var wsToAdd = menuItem.Ws;
			var newLineChoices = m_vc.LineChoices.Clone() as InterlinLineChoices;
			if (newLineChoices != null)
			{
				newLineChoices.Add(flid, wsToAdd);
				UpdateForNewLineChoices(newLineChoices);
			}
		}

		private void moveUpItem_Click(object sender, EventArgs e)
		{
			var ilineToHide = (int)(((ToolStripMenuItem) sender).Tag);
			var newLineChoices = m_vc.LineChoices.Clone() as InterlinLineChoices;
			if (newLineChoices != null)
			{
				newLineChoices.MoveUp(ilineToHide);
				UpdateForNewLineChoices(newLineChoices);
			}
		}

		private void moveDownItem_Click(object sender, EventArgs e)
		{
			var ilineToHide = (int)(((ToolStripMenuItem) sender).Tag);
			var newLineChoices = m_vc.LineChoices.Clone() as InterlinLineChoices;
			if (newLineChoices != null)
			{
				newLineChoices.MoveDown(ilineToHide);
				UpdateForNewLineChoices(newLineChoices);
			}
		}

		private void addLineItem_Click(object sender, EventArgs e)
		{
			var menuItem = sender as AddLineMenuItem;
			if (menuItem == null)
				return; // Impossible?

			var flid = menuItem.Flid;
			var newLineChoices = m_vc.LineChoices.Clone() as InterlinLineChoices;
			if (newLineChoices != null)
			{
				newLineChoices.Add(flid);
				UpdateForNewLineChoices(newLineChoices);
			}
		}

		private void configLink_Click(object sender, EventArgs e)
		{
			OnConfigureInterlinear(null);
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

		/// <summary>
		/// </summary>
		/// <param name="lineConfigPropName">the key used to store/restore line configuration settings.</param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public InterlinLineChoices SetupLineChoices(string lineConfigPropName, InterlinLineChoices.InterlinMode mode)
		{
			ConfigPropName = lineConfigPropName;
			InterlinLineChoices lineChoices;
			if (!TryRestoreLineChoices(out lineChoices))
			{
				if (ForEditing)
				{
					lineChoices = EditableInterlinLineChoices.DefaultChoices(m_fdoCache.LangProject,
						WritingSystemServices.kwsVernInParagraph, WritingSystemServices.kwsAnal);
					lineChoices.Mode = mode;
					if (mode == InterlinLineChoices.InterlinMode.Gloss ||
						mode == InterlinLineChoices.InterlinMode.GlossAddWordsToLexicon)
						lineChoices.SetStandardGlossState();
					else
						lineChoices.SetStandardState();
				}
				else
				{
					lineChoices = InterlinLineChoices.DefaultChoices(m_fdoCache.LangProject,
						WritingSystemServices.kwsVernInParagraph, WritingSystemServices.kwsAnal, mode);
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
		protected internal InterlinLineChoices LineChoices { get; set; }

		/// <summary>
		/// Tries to restore the LineChoices saved in the ConfigPropName property in the property table.
		/// </summary>
		/// <param name="lineChoices"></param>
		/// <returns></returns>
		internal bool TryRestoreLineChoices(out InterlinLineChoices lineChoices)
		{
			lineChoices = null;
			var persist = m_mediator.PropertyTable.GetStringProperty(ConfigPropName, null, PropertyTable.SettingsGroup.LocalSettings);
			if (persist != null)
			{
				lineChoices = InterlinLineChoices.Restore(persist, m_fdoCache.LanguageWritingSystemFactoryAccessor,
					m_fdoCache.LangProject, WritingSystemServices.kwsVernInParagraph, m_fdoCache.DefaultAnalWs);
			}
			return persist != null && lineChoices != null;
		}

		/// <summary>
		///  Launch the Configure interlinear dialog and deal with the results
		/// </summary>
		/// <param name="argument"></param>
		public bool OnConfigureInterlinear(object argument)
		{
			using (var dlg = new ConfigureInterlinDialog(m_fdoCache, m_mediator.HelpTopicProvider,
				m_vc.LineChoices.Clone() as InterlinLineChoices))
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
			m_vc.LineChoices = newChoices;
			LineChoices = newChoices;

			PersistAndDisplayChangedLineChoices();
		}

		internal void PersistAndDisplayChangedLineChoices()
		{
			m_mediator.PropertyTable.SetProperty(ConfigPropName,
				m_vc.LineChoices.Persist(m_fdoCache.LanguageWritingSystemFactoryAccessor),
				PropertyTable.SettingsGroup.LocalSettings);
			UpdateDisplayForNewLineChoices();
		}

		/// <summary>
		/// Do whatever is necessary to display new line choices.
		/// </summary>
		private void UpdateDisplayForNewLineChoices()
		{
			if (m_rootb == null)
				return;
			m_rootb.Reconstruct();
		}

		/// <summary>
		/// delegate for determining whether a paragraph should be updated according to occurrences based upon
		/// the given wordforms.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="wordforms"></param>
		/// <returns></returns>
		internal delegate bool UpdateGuessesCondition(IStTxtPara para, HashSet<IWfiWordform> wordforms);

		/// <summary>
		/// Update any necessary guesses when the specified wordforms change.
		/// </summary>
		/// <param name="wordforms"></param>
		internal virtual void UpdateGuesses(HashSet<IWfiWordform> wordforms)
		{
			UpdateGuesses(wordforms, true);
		}

		private void UpdateGuesses(HashSet<IWfiWordform> wordforms, bool fUpdateDisplayWhereNeeded)
		{
			// now update the guesses for the paragraphs.
			var pdut = new ParaDataUpdateTracker(m_vc.GuessServices, m_vc.Decorator);
			foreach (IStTxtPara para in RootStText.ParagraphsOS)
				pdut.LoadAnalysisData(para, wordforms);
			if (fUpdateDisplayWhereNeeded)
			{
				// now update the display with the affected annotations.
				foreach (var changed in pdut.ChangedAnnotations)
					UpdateDisplayForOccurrence(changed);
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
				return;

			// Simluate replacing the wordform in the relevant segment with itself. This lets the VC Display method run again, this
			// time possibly getting a different answer about whether hvoAnnotation is the current annotation, or about the
			// size of the Sandbox.
			m_rootb.PropChanged(occurrence.Segment.Hvo, SegmentTags.kflidAnalyses, occurrence.Index, 1, 1);
		}

		internal InterlinMaster GetMaster()
		{
			for (Control parentControl = Parent; parentControl != null; parentControl = parentControl.Parent)
			{
				if (parentControl is InterlinMaster)
					return parentControl as InterlinMaster;
			}
			return null;
		}

		#region implemention of IChangeRootObject
		public void SetRoot(int hvo)
		{
			EnsureVc();
			if (LineChoices != null)
				m_vc.LineChoices = LineChoices;

			SetRootInternal(hvo);
			AddDecorator();
			RemoveContextButtonIfPresent(); // Don't want to keep the context button for a different text!
		}

		/// <summary>
		/// Returns the rootbox of this object, or null if not applicable
		/// </summary>
		/// <returns></returns>
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
			if (m_rootb != null)
				m_rootb.DataAccess = m_vc.Decorator;
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
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
						InterlinMaster.LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(RootStText, true));
				// Sync Guesses data before we redraw anything.
				UpdateGuessData();
			}
			// FWR-191: we don't need to reconstruct the display if we didn't need to reload annotations
			// but until we detect that condition, we need to redisplay just in case, to keep things in sync.
			// especially if someone edited the baseline.
			ChangeOrMakeRoot(m_hvoRoot, m_vc, InterlinVc.kfragStText, m_styleSheet);
			m_vc.RootSite = this;
		}

		#region IVwNotifyChange Members

		internal bool SuspendResettingAnalysisCache;

		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			//If the RootStText is null we are either in a place that doesn't care about parser related updates
			// or we are not yet completely displaying the text, so we should be fine, I hope? (LT-12493)
			if (SuspendResettingAnalysisCache || RootStText == null)
				return;

			switch (tag)
			{
				case WfiAnalysisTags.kflidEvaluations:
					IWfiAnalysis analysis = m_fdoCache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().GetObject(hvo);
					if (analysis.HasWordform && RootStText.UniqueWordforms().Contains(analysis.Wordform))
					{
						m_wordformsToUpdate.Add(analysis.Wordform);
						m_mediator.IdleQueue.Add(IdleQueuePriority.High, PostponedUpdateWordforms);
					}
					break;
				case WfiWordformTags.kflidAnalyses:
					IWfiWordform wordform = m_fdoCache.ServiceLocator.GetInstance<IWfiWordformRepository>().GetObject(hvo);
					if (RootStText.UniqueWordforms().Contains(wordform))
					{
						m_wordformsToUpdate.Add(wordform);
						m_mediator.IdleQueue.Add(IdleQueuePriority.High, PostponedUpdateWordforms);
					}
					break;
			}
		}

		private bool PostponedUpdateWordforms(object parameter)
		{
			if (IsDisposed)
				return true;

			m_vc.GuessServices.ClearGuessData();
			UpdateWordforms(m_wordformsToUpdate);
			m_wordformsToUpdate.Clear();
			return true;
		}

		protected virtual void UpdateWordforms(HashSet<IWfiWordform> wordforms)
		{
			UpdateGuesses(wordforms, true);
		}

		#endregion
		public void UpdatingOccurrence(IAnalysis oldAnalysis, IAnalysis newAnalysis)
		{
			m_vc.UpdatingOccurrence(oldAnalysis, newAnalysis);
		}

		protected internal IStText RootStText
		{
			get;
			set;
		}

		static internal int GetParagraphIndexForAnalysis(AnalysisOccurrence point)
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
		/// <param name="bookmark"></param>
		/// <returns></returns>
		internal AnalysisOccurrence ConvertBookmarkToAnalysis(IStTextBookmark bookmark)
		{
			bool fDummy;
			return ConvertBookmarkToAnalysis(bookmark, out fDummy);
		}

		/// <summary>
		/// Returns an AnalysisOccurrence at least close to the given bookmark.
		/// If we can't, we return null. This version reports whether we found an exact match or not.
		/// </summary>
		/// <param name="bookmark"></param>
		/// <param name="fExactMatch"></param>
		/// <returns></returns>
		internal AnalysisOccurrence ConvertBookmarkToAnalysis(IStTextBookmark bookmark, out bool fExactMatch)
		{
			fExactMatch = false;
			if (RootStText == null || RootStText.ParagraphsOS.Count == 0
				|| bookmark.IndexOfParagraph < 0 || bookmark.BeginCharOffset < 0 || bookmark.IndexOfParagraph >= RootStText.ParagraphsOS.Count)
				return null;
			var para = RootStText.ParagraphsOS[bookmark.IndexOfParagraph] as IStTxtPara;
			if (para == null)
				return null;

			var point = SegmentServices.FindNearestAnalysis(para,
				bookmark.BeginCharOffset, bookmark.EndCharOffset, out fExactMatch);
			if (point != null && point.Analysis is IPunctuationForm)
			{
				// Don't want to return punctuation! Wordform or null!
				fExactMatch = false;
				if (point.Index > 0)
					return point.PreviousWordform();
				return point.NextWordform();
			}
			return point;
		}

		#endregion
	}

	/// <summary>
	/// Used for Interlinear context menu items to Add a new WritingSystem
	/// for a flid that is already visible.
	/// </summary>
	public class AddWritingSystemMenuItem : ToolStripMenuItem
	{
		private readonly int m_flid;
		private readonly int m_ws;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddWritingSystemMenuItem"/> class
		/// used for context (right-click) menus.
		/// </summary>
		/// <param name="flid">
		/// 	The flid of the InterlinLineSpec we might add.
		/// </param>
		/// <param name="ws">
		/// 	The writing system int id of the InterlinLineSpec we might add.
		/// </param>
		public AddWritingSystemMenuItem(int flid, int ws)
		{
			m_flid = flid;
			m_ws = ws;
		}

		public int Flid
		{
			get { return m_flid; }
		}

		public int Ws
		{
			get { return m_ws; }
		}
	}

	/// <summary>
	/// Used for Interlinear context menu items to Add a new InterlinLineSpec
	/// for a flid that is currently hidden.
	/// </summary>
	public class AddLineMenuItem : ToolStripMenuItem
	{
		private readonly int m_flid;

		/// <summary>
		/// Initializes a new instance of the <see cref="AddLineMenuItem"/> class
		/// used for context (right-click) menus.
		/// </summary>
		/// <param name="flid">
		/// 	The flid of the InterlinLineSpec we might add.
		/// </param>
		public AddLineMenuItem(int flid)
		{
			m_flid = flid;
		}

		public int Flid
		{
			get { return m_flid; }
		}
	}

	public interface ISelectOccurrence
	{
		void SelectOccurrence(AnalysisOccurrence occurrence);
	}

	public interface ISetupLineChoices
	{
		/// <summary>
		/// True if we will be doing editing (display sandbox, restrict field order choices, etc.).
		/// </summary>
		bool ForEditing { get; set; }
		InterlinLineChoices SetupLineChoices(string lineConfigPropName,
			InterlinLineChoices.InterlinMode mode);
	}

	/// <summary>
	/// This interface helps to identify a control that can be used in InterlinMaster tab pages.
	/// In the future, we may not want to force any such control to implement all of these
	/// interfaces, but for now, this works.
	/// </summary>
	public interface IInterlinearTabControl : IChangeRootObject
	{
		FdoCache Cache { get; set; }
	}

	public interface IStyleSheet
	{
		IVwStylesheet StyleSheet { get; set; }
	}
}
