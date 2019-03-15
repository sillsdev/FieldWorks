// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
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
	/// RawTextPane displays an StText using the standard VC, except that if it is empty altogether,
	/// we display a message. (Eventually.)
	/// </summary>
	public class RawTextPane : RootSite, IInterlinearTabControl, IHandleBookmark
	{
		XElement _configurationParameters;
		private ShowSpaceDecorator _showSpaceDa;
		private bool _clickInsertsZws; // true for the special mode where click inserts a zero-width space
		private bool _isCurrentTabForInterlineMaster;
		private bool _showInvisibleSpaces;
		private bool _clickInvisibleSpace;

		internal MajorFlexComponentParameters MyMajorFlexComponentParameters { get; set; }

		public RawTextPane()
			: base(null)
		{
			BackColor = Color.FromKnownColor(KnownColor.Window);
			DoSpellCheck = true;
			AcceptsTab = false;
		}

		internal int RootHvo { get; private set; }

		internal RawTextVc Vc { get; private set; }

		internal XElement ConfigurationParameters
		{
			set { _configurationParameters = value; }
		}

		internal bool IsCurrentTabForInterlineMaster
		{
			get { return _isCurrentTabForInterlineMaster; }
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
					// Set Check on two space menus.
					var currentMenuItem = (ToolStripMenuItem)MyMajorFlexComponentParameters.UiWidgetController.InsertMenuDictionary[Command.ClickInvisibleSpace];
					currentMenuItem.Checked = _clickInvisibleSpace;
					currentMenuItem = (ToolStripMenuItem)MyMajorFlexComponentParameters.UiWidgetController.ViewMenuDictionary[Command.ShowInvisibleSpaces];
					currentMenuItem.Checked = _showInvisibleSpaces;
					// Add handler stuff.
					var insertMenuHandler = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>
					{
						{Command.CmdGuessWordBreaks, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdGuessWordBreaks_Click, () => CanCmdGuessWordBreaks) },
						{Command.ClickInvisibleSpace, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ClickInvisibleSpace_Click, () => CanClickInvisibleSpace) }
					};
					var viewMenuHandler = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>
					{
						{Command.ShowInvisibleSpaces, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ShowInvisibleSpaces_Click, () => CanShowInvisibleSpaces) }
					};
					var userController = new UserControlUiWidgetParameterObject(this);
					userController.MenuItemsForUserControl.Add(MainMenu.View, viewMenuHandler);
					userController.MenuItemsForUserControl.Add(MainMenu.Insert, insertMenuHandler);
					MyMajorFlexComponentParameters.UiWidgetController.AddHandlers(userController);

				}
				else
				{
					// remove handler stuff.
					MyMajorFlexComponentParameters.UiWidgetController.RemoveUserControlHandlers(this);
				}
			}
		}

		#region IDisposable override

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			MyRecordList = null;
			Vc = null;
			_configurationParameters = null;
		}

		#endregion IDisposable override

		#region implemention of IChangeRootObject

		public virtual void SetRoot(int hvo)
		{
			if (hvo != RootHvo || Vc == null)
			{
				RootHvo = hvo;
				SetupVc();
				ChangeOrMakeRoot(RootHvo, Vc, (int)StTextFrags.kfrText, m_styleSheet);
			}
			BringToFront();
			if (RootHvo == 0)
			{
				return;
			}
			// if editable, parse the text to make sure annotations are in a valid initial state
			// with respect to the text so AnnotatedTextEditingHelper can make the right changes
			// to annotations effected by MonitorTextsEdits being true;
			if (Vc == null || !Vc.Editable)
			{
				return;
			}
			if (RootObject.HasParagraphNeedingParse())
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					RootObject.LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(false);
				});
			}
		}

		/// <summary>
		/// We can't set the style for Scripture...that has to follow some very specific rules implemented in TE.
		/// </summary>
		public override bool CanApplyStyle => base.CanApplyStyle && !ScriptureServices.ScriptureIsResponsibleFor(m_rootObj);

		#endregion

		/// <summary>
		/// This is the record list, if any, that determines the text for our control.
		/// </summary>
		internal IRecordList MyRecordList { get; set; }

		IStText m_rootObj;
		public IStText RootObject
		{
			get
			{
				if (m_rootObj != null && m_rootObj.Hvo == RootHvo)
				{
					return m_rootObj;
				}
				m_rootObj = RootHvo != 0 ? Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(RootHvo) : null;
				return m_rootObj;
			}
		}

		internal int LastFoundAnnotationHvo { get; } = 0;

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (_clickInvisibleSpace)
			{
				if (InsertInvisibleSpace(e))
				{
					return;
				}
			}
			base.OnMouseDown(e);
		}

		// Insert an invisible space at the place clicked. Return true to suppress normal MouseDown processing.
		private bool InsertInvisibleSpace(MouseEventArgs e)
		{
			var sel = GetSelectionAtViewPoint(e.Location, false);
			if (sel == null)
			{
				return false;
			}
			if (e.Button == MouseButtons.Right || (ModifierKeys & Keys.Shift) == Keys.Shift)
			{
				return false; // don't interfere with right clicks or shift+clicks.
			}
			var helper = SelectionHelper.Create(sel, this);
			var text = helper.GetTss(SelLimitType.Anchor).Text;
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			// We test for space (rather than zwsp) because when in this mode, the option to make the ZWS's visible
			// is always on, which means they are spaces in the string we retrieve.
			// If we don't want to suppress inserting one next to a regular space, we'll need to check the character properties
			// to distinguish the magic spaces from regular ones.
			var ich = helper.GetIch(SelLimitType.Anchor);
			if (ich > 0 && ich <= text.Length && text[ich - 1] == ' ')
			{
				return false; // don't insert second ZWS following existing one (or normal space).
			}
			if (ich < text.Length && text[ich] == ' ')
			{
				return false; // don't insert second ZWS before existing one (or normal space).
			}
			int nVar;
			var ws = helper.GetSelProps(SelLimitType.Anchor).GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
			if (ws != 0)
			{
				UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoInsertInvisibleSpace, ITextStrings.ksRedoInsertInvisibleSpace, Cache.ActionHandlerAccessor,
					() => sel.ReplaceWithTsString(TsStringUtils.MakeString(AnalysisOccurrence.KstrZws, ws)));
			}
			helper.SetIch(SelLimitType.Anchor, ich + 1);
			helper.SetIch(SelLimitType.End, ich + 1);
			helper.SetSelection(true, true);
			return true; // we already made an appropriate selection.
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == (int)Keys.Escape)
			{
				TurnOffClickInvisibleSpace();
			}
			base.OnKeyPress(e);
			Cursor.Current = Cursors.IBeam;
		}

		Cursor m_invisibleSpaceCursor;

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (!_clickInvisibleSpace)
			{
				return;
			}
			if (m_invisibleSpaceCursor == null)
			{
				m_invisibleSpaceCursor = new Cursor(GetType(), "InvisibleSpaceCursor.cur");
			}
			Cursor = m_invisibleSpaceCursor;
		}

		protected override void OnLostFocus(EventArgs e)
		{
			TurnOffClickInvisibleSpace();
			base.OnLostFocus(e);
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		private Tuple<bool, bool> CanShowInvisibleSpaces
		{
			get
			{
				var isTextPresent = RootBox?.Selection != null;
				if (isTextPresent) //well, the rootbox is at least there, test it for text.
				{
					ITsString tss;
					int ichLim, hvo, tag, ws;
					bool fAssocPrev;
					RootBox.Selection.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);
					if (ichLim == 0 && tss.Length == 0) //nope, no text.
					{
						isTextPresent = false;
					}
				}
				return new Tuple<bool, bool>(true, isTextPresent);
			}
		}

		private void ShowInvisibleSpaces_Click(object sender, EventArgs e)
		{
			var senderAsMenuItem = (ToolStripMenuItem)sender;
			if (senderAsMenuItem.Checked == _showInvisibleSpaces)
			{
				// Nothing to do.
				return;
			}
			var newVal = senderAsMenuItem.Checked;
			if (newVal != _showSpaceDa.ShowSpaces)
			{
				_showSpaceDa.ShowSpaces = newVal;
				var saveSelection = SelectionHelper.Create(this);
				RootBox.Reconstruct();
				saveSelection.SetSelection(true);
			}
			if (!newVal && _clickInvisibleSpace)
			{
				TurnOffClickInvisibleSpace();
				// Set Checked for the other the menu and run its event handler.
				var clickInvisibleSpace = (ToolStripMenuItem)MyMajorFlexComponentParameters.UiWidgetController.ViewMenuDictionary[Command.ClickInvisibleSpace];
				clickInvisibleSpace.Checked = false;
				clickInvisibleSpace.PerformClick();
			}
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		private Tuple<bool, bool> CanClickInvisibleSpace
		{
			get
			{
				var isTextPresent = RootBox?.Selection != null;
				if (isTextPresent) //well, the rootbox is at least there, test it for text.
				{
					ITsString tss;
					int ichLim, hvo, tag, ws;
					bool fAssocPrev;
					RootBox.Selection.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);
					if (ichLim == 0 && tss.Length == 0) //nope, no text.
					{
						isTextPresent = false;
					}
				}
				return new Tuple<bool, bool>(true, isTextPresent);
			}
		}

		private void ClickInvisibleSpace_Click(object sender, EventArgs e)
		{
			var senderAsMenuItem = (ToolStripMenuItem)sender;
			var newVal = senderAsMenuItem.Checked;
			if (newVal == _clickInvisibleSpace || newVal == _clickInsertsZws)
			{
				// Nothing to do.
				return;
			}
			_clickInsertsZws = newVal;
			if (newVal && !_showInvisibleSpaces)
			{
				TurnOnShowInvisibleSpaces();
				// Set Checked for the other the menu and run its event handler.
				var showInvisibleSpacesMenu = (ToolStripMenuItem)MyMajorFlexComponentParameters.UiWidgetController.ViewMenuDictionary[Command.ShowInvisibleSpaces];
				showInvisibleSpacesMenu.Checked = true;
				showInvisibleSpacesMenu.PerformClick();
			}
		}

		/// <summary>
		/// Handle "WritingSystemHvo" message.
		/// </summary>
		protected override void ReallyHandleWritingSystemHvo_Changed(object newValue)
		{
			var wsBefore = 0;
			if (RootObject != null && RootBox != null && RootBox.Selection.IsValid)
			{
				// We want to know below whether a base class changed the ws or not.
				wsBefore = SelectionHelper.GetWsOfEntireSelection(RootBox.Selection);
			}

			base.ReallyHandleWritingSystemHvo_Changed(newValue);

			if (RootObject == null || RootBox == null || !RootBox.Selection.IsValid)
			{
				return;
			}
			var ws = SelectionHelper.GetWsOfEntireSelection(RootBox.Selection);
			if (ws == wsBefore)
			{
				// No change, so bail out.
				return;
			}
			int hvo;
			int tag;
			int ichMin;
			int ichLim;
			if (!GetSelectedWordPos(RootBox.Selection, out hvo, out tag, out ws, out ichMin, out ichLim) || tag != StTxtParaTags.kflidContents)
			{
				return;
			}
			// Force this paragraph to recognize it might need reparsing.
			var para = m_cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(hvo);
			if (Cache.ActionHandlerAccessor.CurrentDepth > 0)
			{
				para.ParseIsCurrent = false;
			}
			else
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => para.ParseIsCurrent = false);
			}
		}

		private void TurnOnShowInvisibleSpaces()
		{
			_showInvisibleSpaces = true;
			PropertyTable.SetProperty("ShowInvisibleSpaces", true, true);
		}

		private void TurnOffClickInvisibleSpace()
		{
			_clickInvisibleSpace = false;
			PropertyTable.SetProperty("ClickInvisibleSpace", false, true);
		}

		#region Overrides of RootSite
		/// <summary>
		/// Make the root box.
		/// </summary>
		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode || RootHvo == 0)
			{
				return;
			}

			base.MakeRoot();

			var wsFirstPara = GetWsOfFirstWordOfFirstTextPara();
			Vc = new RawTextVc(RootBox, m_cache, wsFirstPara);
			SetupVc();
			_showSpaceDa = new ShowSpaceDecorator(m_cache.GetManagedSilDataAccess())
			{
				ShowSpaces = _showInvisibleSpaces
			};
			RootBox.DataAccess = _showSpaceDa;
			RootBox.SetRootObject(RootHvo, Vc, (int)StTextFrags.kfrText, m_styleSheet);
		}

		/// <summary>
		/// Returns WS of first character of first paragraph of m_hvoRoot text.
		/// It defaults to DefaultVernacularWs in case of a problem.
		/// </summary>
		private int GetWsOfFirstWordOfFirstTextPara()
		{
			Debug.Assert(RootHvo > 0, "No StText Hvo!");
			var wsFirstPara = Cache.DefaultVernWs;
			var txt = Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(RootHvo);
			if (txt.ParagraphsOS == null || txt.ParagraphsOS.Count == 0)
			{
				return wsFirstPara;
			}
			return ((IStTxtPara)txt.ParagraphsOS[0]).Contents.get_WritingSystem(0);
		}

		private void SetupVc()
		{
			if (Vc == null || RootHvo == 0)
			{
				return;
			}
			var wsFirstPara = GetWsOfFirstWordOfFirstTextPara();
			if (wsFirstPara == -1)
			{
				// The paragraph's first character has no valid writing system...this seems to be possible
				// when it consists entirely of a picture. Rather than crashing, presume the default.
				wsFirstPara = Cache.DefaultVernWs;
			}
			Vc.SetupVernWsForText(wsFirstPara);
			var stText = Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(RootHvo);
			if (_configurationParameters == null)
			{
				return;
			}
			Vc.Editable = XmlUtils.GetOptionalBooleanAttributeValue(_configurationParameters, "editable", true);
			Vc.Editable &= !ScriptureServices.ScriptureIsResponsibleFor(stText);
		}

		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			base.HandleSelectionChange(rootb, vwselNew);

			// JohnT: it's remotely possible that the base, in calling commit, made this
			// selection no longer useable.
			if (!vwselNew.IsValid)
			{
				return;
			}
			IWfiWordform wordform;
			if (!GetSelectedWordform(vwselNew, out wordform))
			{
				wordform = null;
			}
			Publisher.Publish("TextSelectedWord", wordform);
			var helper = SelectionHelper.Create(vwselNew, this);
			if (helper != null && helper.GetTextPropId(SelLimitType.Anchor) == RawTextVc.kTagUserPrompt)
			{
				vwselNew.ExtendToStringBoundaries();
				EditingHelper.SetKeyboardForSelection(vwselNew);
			}
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (Parent == null && string.IsNullOrEmpty(levent.AffectedProperty))
			{
				// width is meaningless, no point in doing extra work
				return;
			}
			// In a tab page this panel occupies the whole thing, so layout is wasted until
			// our size is adjusted to match.
			if (Parent is TabPage && (Parent.Width - Parent.Padding.Horizontal) != this.Width)
			{
				return;
			}
			base.OnLayout(levent);
		}

		/// <summary>
		/// The user has attempted to delete something which the system does not inherently
		/// know how to delete. The dpt argument indicates the type of problem.
		/// </summary>
		public override VwDelProbResponse OnProblemDeletion(IVwSelection sel, VwDelProbType dpt)
		{
			switch (dpt)
			{
				case VwDelProbType.kdptBsAtStartPara:
				case VwDelProbType.kdptDelAtEndPara:
				case VwDelProbType.kdptNone:
					return VwDelProbResponse.kdprDone;
				case VwDelProbType.kdptBsReadOnly:
				case VwDelProbType.kdptComplexRange:
				case VwDelProbType.kdptDelReadOnly:
				case VwDelProbType.kdptReadOnly:
					return VwDelProbResponse.kdprFail;
			}
			return VwDelProbResponse.kdprAbort;
		}

		/// <summary>
		/// Draw to the given clip rectangle.  This is overridden to *NOT* write the
		/// default message for an uninitialized rootsite.
		/// </summary>
		protected override void Draw(PaintEventArgs e)
		{
			if (RootBox != null && (m_dxdLayoutWidth > 0) && !DesignMode)
			{
				base.Draw(e);
			}
			else
			{
				e.Graphics.FillRectangle(SystemBrushes.Window, ClientRectangle);
			}
		}

		public void HandleKeyDownAndKeyPress(Keys key)
		{
			var kea = new KeyEventArgs(key);
			if (EditingHelper.HandleOnKeyDown(kea))
			{
				return;
			}
			OnKeyDown(kea);
			// for some reason OnKeyPress does not handle Delete key
			// In FLEX, OnKeyPress does not even get called for Delete key.
			if (key != Keys.Delete)
			{
				OnKeyPress(new KeyPressEventArgs((char)kea.KeyValue));
			}
		}

		/// <summary>
		/// Handle a right mouse up, invoking an appropriate context menu.
		/// </summary>
		protected override bool DoContextMenu(IVwSelection sel, Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			// Allow base method to handle spell check problems, if any.
			if (base.DoContextMenu(sel, pt, rcSrcRoot, rcDstRoot))
			{
				return true;
			}
			var mainWind = ParentForm as IFwMainWnd;
			if (mainWind == null || sel == null)
			{
				return false;
			}
			CmObjectUi ui = null;
			try
			{
				IWfiWordform wordform;
				if (GetSelectedWordform(RootBox.Selection, out wordform))
				{
					ui = CmObjectUi.MakeLcmModelUiObject(Cache, wordform.Hvo);
					ui.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				}
#if RANDYTODO
// The original code sent it to the window, who then passed it on as in:
// ((IUIMenuAdapter)m_menuBarAdapter).ShowContextMenu(group, location, temporaryColleagueParam, sequencer, adjustMenu);
// The optional TemporaryColleagueParameter could then be added as temporary colleagues who could actually handle the message (along with any others the Mediator knew about, if any.)
// In this case "ui" was the temp colleague:
// tempColleague = new TemporaryColleagueParameter(m_mediator, ui, false);
				mainWind.ShowContextMenu("mnuIText-RawText", new Point(Cursor.Position.X, Cursor.Position.Y), tempColleague, null);
#endif
				/*
				    <menu id="mnuIText-RawText">
				      <item command="CmdCut" />
				      <item command="CmdCopy" />
				      <item command="CmdPaste" />
				      <item label="-" translate="do not translate" />
				      <item command="CmdLexiconLookup" />
				      <item command="CmdWordformJumpToAnalyses" defaultVisible="false" />
				      <item command="CmdWordformJumpToConcordance" defaultVisible="false" />
				    </menu>
				*/
				return true;
			}
			finally
			{
				ui?.Dispose();
			}
		}

		internal IRecordList ActiveRecordList => PropertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList;

		#endregion Overrides of RootSite

		public void MakeTextSelectionAndScrollToView(int ichMin, int ichLim, int ws, int ipara)
		{
			MakeTextSelectionAndScrollToView(ichMin, ichLim, ws, ipara, -1);// end in same prop
		}

		protected void MakeTextSelectionAndScrollToView(int ichMin, int ichLim, int ws, int ipara, int ihvoEnd)
		{
			var rgsli = new SelLevInfo[1];
			// entry 0 says which StTextPara
			rgsli[0].ihvo = ipara;
			rgsli[0].tag = StTextTags.kflidParagraphs;
			// entry 1 says to use the Contents of the Text.
			try
			{
				RootBox.MakeTextSelection(0, rgsli.Length, rgsli, StTxtParaTags.kflidContents, 0, ichMin, ichLim, ws, false, ihvoEnd, null, true);
				// Don't steal the focus from another window.  See FWR-1795.
				if (ParentForm == Form.ActiveForm)
				{
					Focus();
				}
				// Scroll this selection into View.
				var sel = RootBox.Selection;
				ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
				Update();
			}
			catch (Exception)
			{
			}
		}

		#region IHandleBookMark

		public void SelectBookmark(IStTextBookmark bookmark)
		{
			MakeTextSelectionAndScrollToView(bookmark.BeginCharOffset, bookmark.EndCharOffset, 0, bookmark.IndexOfParagraph);
		}

		#endregion

		/// <summary>
		/// Look up the selected wordform in the dictionary and display its lexical entry.
		/// </summary>
		public bool OnLexiconLookup(object argument)
		{
			int ichMin, ichLim, hvo, tag, ws;
			if (GetSelectedWordPos(RootBox.Selection, out hvo, out tag, out ws, out ichMin, out ichLim))
			{
				LexEntryUi.DisplayOrCreateEntry(m_cache, hvo, tag, ws, ichMin, ichLim, this, PropertyTable, Publisher, Subscriber, PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), "UserHelpFile");
			}
			return true;
		}

		/// <summary>
		/// Returns true if there's anything to select.  This is needed so that the toolbar
		/// button is disabled when there's nothing to select and look up.  Otherwise, crashes
		/// can result when it's clicked but there's nothing there to process!  It's misleading
		/// to the user if nothing else.  It would be nice if the processing could be minimized,
		/// but this seems to be minimal. (GJM - 23 Feb 2012 Is that better? LT-12726)
		/// </summary>
		public bool LexiconLookupEnabled()
		{
			var sel = RootBox?.Selection;
			if (sel == null || !sel.IsValid)
			{
				return false;
			}
			int hvoDummy, tagDummy, wsDummy, ichMinDummy, ichLimDummy;
			// We just need to see if it's possible
			return GetSelectedWordPos(sel, out hvoDummy, out tagDummy, out wsDummy, out ichMinDummy, out ichLimDummy);
		}

		private static bool GetSelectedWordPos(IVwSelection sel, out int hvo, out int tag, out int ws, out int ichMin, out int ichLim)
		{
			IVwSelection wordsel = null;
			if (sel != null)
			{
				var sel2 = sel.EndBeforeAnchor ? sel.EndPoint(true) : sel.EndPoint(false);
				wordsel = sel2?.GrowToWord();
			}
			if (wordsel == null)
			{
				hvo = tag = ws = 0;
				ichMin = ichLim = -1;
				return false;
			}
			ITsString tss;
			bool fAssocPrev;
			wordsel.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvo, out tag, out ws);
			wordsel.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);
			return ichLim > 0;
		}

		private bool GetSelectedWordform(IVwSelection sel, out IWfiWordform wordform)
		{
			wordform = null;
			int ichMin, ichLim, hvo, tag, ws;
			if (!GetSelectedWordPos(sel, out hvo, out tag, out ws, out ichMin, out ichLim))
			{
				return false;
			}
			if (tag != StTxtParaTags.kflidContents)
			{
				return false;
			}
			var para = m_cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(hvo);
			if (!para.ParseIsCurrent)
			{
				ReparseParaInUowIfNeeded(para);
			}
			var anal = FindClosestWagParsed(para, ichMin, ichLim);
			if (!para.ParseIsCurrent)
			{
				// Something is wrong! The attempt to find the word detected an inconsistency.
				// Fix the paragraph and try again.
				ReparseParaInUowIfNeeded(para);
				anal = FindClosestWagParsed(para, ichMin, ichLim);
			}
			if (anal != null && anal.HasWordform)
			{
				wordform = anal.Wordform;
				return true;
			}
			return false;
		}

		private void ReparseParaInUowIfNeeded(IStTxtPara para)
		{
			if (Cache.ActionHandlerAccessor.CurrentDepth > 0)
			{
				ReparseParagraph(para);
			}
			else
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => ReparseParagraph(para));
			}
		}

		private static void ReparseParagraph(IStTxtPara para)
		{
			using (var parser = new ParagraphParser(para))
			{
				parser.Parse(para);
			}
		}

		private static IAnalysis FindClosestWagParsed(IStTxtPara para, int ichMin, int ichLim)
		{
			IAnalysis anal = null;
			foreach (var seg in para.SegmentsOS)
			{
				if (seg.BeginOffset > ichMin || seg.EndOffset < ichLim)
				{
					continue;
				}
				bool exact;
				var occurrence = seg.FindWagform(ichMin - seg.BeginOffset, ichLim - seg.BeginOffset, out exact);
				if (occurrence != null)
				{
					anal = occurrence.Analysis;
				}
				break;
			}
			return anal;
		}

		private static void Swap(ref int first, ref int second)
		{
			var temp = first;
			first = second;
			second = temp;
		}

		private Tuple<bool, bool> CanCmdGuessWordBreaks
		{
			get
			{
				var isTextPresent = RootBox?.Selection != null;
				if (isTextPresent) //well, the rootbox is at least there, test it for text.
				{
					ITsString tss;
					int ichLim, hvo, tag, ws;
					bool fAssocPrev;
					RootBox.Selection.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);
					if (ichLim == 0 && tss.Length == 0) //nope, no text.
					{
						isTextPresent = false;
					}
				}
				return new Tuple<bool, bool>(true, isTextPresent);
			}
		}

		/// <summary>
		/// Guess where we can break words.
		/// </summary>
		private void CmdGuessWordBreaks_Click(object sender, EventArgs e)
		{
			var sel = RootBox.Selection;
			ITsString tss;
			int ichMin, hvoStart, ichLim, hvoEnd, tag, ws;
			bool fAssocPrev;
			sel.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvoStart, out tag, out ws);
			sel.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvoEnd, out tag, out ws);
			if (sel.EndBeforeAnchor)
			{
				Swap(ref ichMin, ref ichLim);
				Swap(ref hvoStart, ref hvoEnd);
			}
			var guesser = new WordBreakGuesser(m_cache, hvoStart);
			if (hvoStart == hvoEnd)
			{
				if (ichMin == ichLim)
				{
					ichMin = 0;
					ichLim = -1; // do the whole paragraph for an IP.
				}
				guesser.Guess(ichMin, ichLim, hvoStart);
			}
			else
			{
				guesser.Guess(ichMin, -1, hvoStart);
				var fProcessing = false;
				var sda = m_cache.MainCacheAccessor;
				var hvoStText = RootHvo;
				var cpara = sda.get_VecSize(hvoStText, StTextTags.kflidParagraphs);
				for (var i = 0; i < cpara; i++)
				{
					var hvoPara = sda.get_VecItem(hvoStText, StTextTags.kflidParagraphs, i);
					if (hvoPara == hvoStart)
					{
						fProcessing = true;
					}
					else if (hvoPara == hvoEnd)
					{
						break;
					}
					else if (fProcessing)
					{
						guesser.Guess(0, -1, hvoPara);
					}
				}
				guesser.Guess(0, ichLim, hvoEnd);
			}
			TurnOnShowInvisibleSpaces();
		}

		#region Overrides of SimpleRootSite

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);
			m_styleSheet = FwUtils.StyleSheetFromPropertyTable(PropertyTable);
			_showInvisibleSpaces = PropertyTable.GetValue<bool>("ShowInvisibleSpaces");
			_clickInvisibleSpace = PropertyTable.GetValue<bool>("ClickInvisibleSpace");
		}

		#endregion
	}
}