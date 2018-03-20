// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using LanguageExplorer.LcmUi;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
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
		XmlNode m_configurationParameters;
		private ShowSpaceDecorator m_showSpaceDa;
		private bool m_fClickInsertsZws; // true for the special mode where click inserts a zero-width space
		private int m_lastWidth;

		private IVwStylesheet m_flexStylesheet;
		private IVwStylesheet m_teStylesheet;

		public RawTextPane() : base(null)
		{
			BackColor = Color.FromKnownColor(KnownColor.Window);
			DoSpellCheck = true;
			AcceptsTab = false;
		}

		internal int RootHvo { get; private set; }

		internal RawTextVc Vc { get; private set; }

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
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
			m_configurationParameters = null;
		}

		#endregion IDisposable override

		#region implemention of IChangeRootObject

		public virtual void SetRoot(int hvo)
		{
			if (hvo != RootHvo || Vc == null)
			{
				SetStyleSheet(hvo);
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
				if (InterlinMaster.HasParagraphNeedingParse(RootObject))
				{
					NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
												   () =>
													   { InterlinMaster.LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(RootObject, false); });
				}
			}

		/// <summary>
		/// We can't set the style for Scripture...that has to follow some very specific rules implemented in TE.
		/// </summary>
		public override bool CanApplyStyle => base.CanApplyStyle && !ScriptureServices.ScriptureIsResponsibleFor(m_rootObj);

		private void SetStyleSheet(int hvo)
		{
			var text = hvo == 0 ? null : (IStText)Cache.ServiceLocator.GetObject(hvo);
			var wantedStylesheet = m_styleSheet;
			if (ScriptureServices.ScriptureIsResponsibleFor(text))
			{
				// Use the Scripture stylesheet
				if (m_teStylesheet == null)
				{
					m_flexStylesheet = m_styleSheet; // remember the default.
					var stylesheet = new LcmStyleSheet();
					stylesheet.Init(Cache, Cache.LangProject.TranslatedScriptureOA.Hvo, ScriptureTags.kflidStyles);
					m_teStylesheet = stylesheet;
				}
				wantedStylesheet = m_teStylesheet;
			}
			else if (m_flexStylesheet != null)
			{
				wantedStylesheet = m_flexStylesheet;
			}

			if (wantedStylesheet == m_styleSheet)
			{
				return;
			}
				m_styleSheet = wantedStylesheet;
			if (m_styleSheet == m_flexStylesheet)
			{
				// Only do it for Flex styles, since Scripture text styles cannot be used in Flex (cf. "CanApplyStyle" property, above).
				// This will allow for character & paragraph styles to be in the combobox.
				Publisher.Publish("ResetStyleSheet", m_styleSheet);
			}
		}

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
			if (ClickInvisibleSpace)
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
				return false; // don't interfere with right clicks or shifr-clicks.
			}
			var helper = SelectionHelper.Create(sel, this);
			var text = helper.GetTss(SelectionHelper.SelLimitType.Anchor).Text;
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			// We test for space (rather than zwsp) because when in this mode, the option to make the ZWS's visible
			// is always on, which means they are spaces in the string we retrieve.
			// If we don't want to suppress inserting one next to a regular space, we'll need to check the chararacter properties
			// to distinguish the magic spaces from regular ones.
			var ich = helper.GetIch(SelectionHelper.SelLimitType.Anchor);
			if (ich > 0 && ich <= text.Length && text[ich - 1] == ' ')
			{
				return false; // don't insert second ZWS following existing one (or normal space).
			}
			if (ich < text.Length && text[ich] == ' ')
			{
				return false; // don't insert second ZWS before existing one (or normal space).
			}
			int nVar;
			var ws = helper.GetSelProps(SelectionHelper.SelLimitType.Anchor).GetIntPropValues((int) FwTextPropType.ktptWs, out nVar);
			if (ws != 0)
			{
				UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoInsertInvisibleSpace, ITextStrings.ksRedoInsertInvisibleSpace,
					Cache.ActionHandlerAccessor,
					() => sel.ReplaceWithTsString(TsStringUtils.MakeString(AnalysisOccurrence.KstrZws, ws)));
			}
			helper.SetIch(SelectionHelper.SelLimitType.Anchor, ich + 1);
			helper.SetIch(SelectionHelper.SelLimitType.End, ich + 1);
			helper.SetSelection(true, true);
			return true; // we already made an appropriate selection.
		}

		protected override void  OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == (int) Keys.Escape)
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
			if (!ClickInvisibleSpace)
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

#if RANDYTODO
		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		public virtual bool OnDisplayShowInvisibleSpaces(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = true; // If this class is a current colleague we want the command
			bool isTextPresent = RootBox != null && RootBox.Selection != null;
			if (isTextPresent) //well, the rootbox is at least there, test it for text.
			{
				ITsString tss;
				int ichLim, hvo, tag, ws;
				bool fAssocPrev;
				RootBox.Selection.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag,
					out ws);
				if (ichLim == 0 && tss.Length == 0) //nope, no text.
					isTextPresent = false;
			}
			display.Enabled = isTextPresent;
			return true; //we've handled this
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		public virtual bool OnDisplayClickInvisibleSpace(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = true; // If this class is a current colleague we want the command
			bool isTextPresent = RootBox != null && RootBox.Selection != null;
			if (isTextPresent) //well, the rootbox is at least there, test it for text.
			{
				ITsString tss;
				int ichLim, hvo, tag, ws;
				bool fAssocPrev;
				RootBox.Selection.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag,
					out ws);
				if (ichLim == 0 && tss.Length == 0) //nope, no text.
					isTextPresent = false;
			}
			display.Enabled = isTextPresent;
			return true; //we've handled this
		}
#endif

		/// <summary>
		/// Receives the broadcast message "PropertyChanged"
		/// </summary>
		public void OnPropertyChanged(string name)
			{
			bool newVal; // used in two cases below
			switch (name)
			{
				case "ShowInvisibleSpaces":
					newVal = ShowInvisibleSpaces;
					if (newVal != m_showSpaceDa.ShowSpaces)
					{
						m_showSpaceDa.ShowSpaces = newVal;
						var saveSelection = SelectionHelper.Create(this);
						m_rootb.Reconstruct();
						saveSelection.SetSelection(true);
					}
					if (!newVal && ClickInvisibleSpace)
						TurnOffClickInvisibleSpace();
					break;
				case "ClickInvisibleSpace":
					newVal = ClickInvisibleSpace;
					if (newVal == m_fClickInsertsZws)
					{
						return;
					}
					m_fClickInsertsZws = newVal;
					if (newVal && !ShowInvisibleSpaces)
					{
						TurnOnShowInvisibleSpaces();
					}
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Handle "WritingSystemHvo" message.
		/// </summary>
		protected override void ReallyHandleWritingSystemHvo_Changed(object newValue)
		{
			var wsBefore = 0;
			if (RootObject != null && m_rootb != null && m_rootb.Selection.IsValid)
		{
				// We want to know below whether a base class changed the ws or not.
				wsBefore = SelectionHelper.GetWsOfEntireSelection(m_rootb.Selection);
		}

			base.ReallyHandleWritingSystemHvo_Changed(newValue);

			if (RootObject == null || m_rootb == null || !m_rootb.Selection.IsValid)
		{
				return;
			}
			var ws = SelectionHelper.GetWsOfEntireSelection(m_rootb.Selection);
			if (ws == wsBefore)
			{
				// No change, so bail out.
				return;
			}
			int hvo;
			int tag;
			int ichMin;
			int ichLim;
			if (!GetSelectedWordPos(m_rootb.Selection, out hvo, out tag, out ws, out ichMin, out ichLim) || tag != StTxtParaTags.kflidContents)
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
			PropertyTable.SetProperty("ShowInvisibleSpaces", true, true, true);
		}

		private void TurnOffClickInvisibleSpace()
		{
			PropertyTable.SetProperty("ClickInvisibleSpace", false, true, true);
		}

		private bool ShowInvisibleSpaces => PropertyTable.GetValue<bool>("ShowInvisibleSpaces");

		private bool ClickInvisibleSpace => PropertyTable.GetValue<bool>("ClickInvisibleSpace");

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
			Vc = new RawTextVc(m_rootb, m_cache, wsFirstPara);
			SetupVc();

			m_showSpaceDa = new ShowSpaceDecorator(m_cache.GetManagedSilDataAccess())
			{
				ShowSpaces = ShowInvisibleSpaces
			};
			m_rootb.DataAccess = m_showSpaceDa;

			m_rootb.SetRootObject(RootHvo, Vc, (int)StTextFrags.kfrText, m_styleSheet);
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

			var firstPara = ((IStTxtPara) txt.ParagraphsOS[0]);
			return firstPara.Contents.get_WritingSystem(0);
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
			if (m_configurationParameters == null)
			{
				return;
			}
			Vc.Editable = XmlUtils.GetOptionalBooleanAttributeValue(m_configurationParameters, "editable", true);
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
			if (helper != null && helper.GetTextPropId(SelectionHelper.SelLimitType.Anchor) == RawTextVc.kTagUserPrompt)
			{
				vwselNew.ExtendToStringBoundaries();
				EditingHelper.SetKeyboardForSelection(vwselNew);
			}
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (string.IsNullOrEmpty(Parent?.Text) || m_lastWidth == Parent.Width)
			{
				// width is meaningless or has already been calculated; no point in doing extra work
				return;
			}
			// In a tab page this panel occupies the whole thing, so layout is wasted until
			// our size is adjusted to match.
			if (Parent is TabPage && (Parent.Width - Parent.Padding.Horizontal) != this.Width)
			{
				return;
			}
			//Save width avoid extra layout calls
			m_lastWidth = Parent.Width;
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
			if (m_rootb != null && (m_dxdLayoutWidth > 0) && !DesignMode)
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
		/// <returns></returns>
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
				if (GetSelectedWordform(m_rootb.Selection, out wordform))
				{
					ui = CmObjectUi.MakeUi(Cache, wordform.Hvo);
					ui.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				}
#if RANDYTODO
				mainWind.ShowContextMenu("mnuIText-RawText", new Point(Cursor.Position.X, Cursor.Position.Y),
					tempColleague, null);
#endif

				return true;
			}
			finally
			{
				ui?.Dispose();
			}
		}

		internal IRecordList ActiveRecordList => RecordList.ActiveRecordListRepository.ActiveRecordList;

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
				RootBox.MakeTextSelection(0, rgsli.Length, rgsli,
					StTxtParaTags.kflidContents, 0, ichMin,
					ichLim, ws,
					false, // Range, arbitrary assoc prev.
					ihvoEnd,
					null, // don't set any special text props for typing
					true); // install it
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
		/// Return a word selection based on the beginning of the current selection.
		/// Here the "beginning" of the selection is the offset corresponding to word order,
		/// not the selection anchor.
		/// </summary>
		/// <returns>null if we couldn't handle the selection</returns>
		private IVwSelection SelectionBeginningGrowToWord(IVwSelection sel)
		{
			if (sel == null)
			{
				return null;
			}
			// REVISIT (EricP) Need to check if Ws is IsRightToLeft?
			var sel2 = sel.EndBeforeAnchor ? sel.EndPoint(true) : sel.EndPoint(false);
			var sel3 = sel2?.GrowToWord();
			return sel3;
		}

		/// <summary>
		/// Look up the selected wordform in the dictionary and display its lexical entry.
		/// </summary>
		public bool OnLexiconLookup(object argument)
		{
			int ichMin, ichLim, hvo, tag, ws;
			if (GetSelectedWordPos(m_rootb.Selection, out hvo, out tag, out ws, out ichMin, out ichLim))
			{
				LexEntryUi.DisplayOrCreateEntry(m_cache, hvo, tag, ws, ichMin, ichLim, this,
					PropertyTable, Publisher, Subscriber, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), "UserHelpFile");
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
			var sel = m_rootb?.Selection;
			if (sel == null || !sel.IsValid)
			{
				return false;
			}
			// out variables for GetSelectedWordPos
			int hvo, tag, ws, ichMin, ichLim;
			// We just need to see if it's possible
			return GetSelectedWordPos(sel, out hvo, out tag, out ws, out ichMin, out ichLim);
		}

		private bool GetSelectedWordPos(IVwSelection sel, out int hvo, out int tag, out int ws, out int ichMin, out int ichLim)
		{
			var wordsel = SelectionBeginningGrowToWord(sel);
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

		private void ReparseParagraph(IStTxtPara para)
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

#if RANDYTODO
		public bool OnDisplayGuessWordBreaks(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Visible = true;
			bool isTextPresent = RootBox != null && RootBox.Selection != null;
			if(isTextPresent) //well, the rootbox is at least there, test it for text.
			{
				ITsString tss;
				int ichLim, hvo, tag, ws;
				bool fAssocPrev;
				RootBox.Selection.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag,
					out ws);
				if (ichLim == 0 && tss.Length == 0) //nope, no text.
					isTextPresent = false;
			}
			display.Enabled = isTextPresent;
			return true;
		}
#endif

		private static void Swap(ref int first, ref int second)
		{
			var temp = first;
			first = second;
			second = temp;
		}

		/// <summary>
		/// Guess where we can break words.
		/// </summary>
		public void OnGuessWordBreaks(object argument)
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
		}

		#endregion
	}
}
