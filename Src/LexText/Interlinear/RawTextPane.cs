using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.XWorks;
using XCore;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.Widgets;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// RawTextPane displays an StText using the standard VC, except that if it is empty altogether,
	/// we display a message. (Eventually.)
	/// </summary>
	public class RawTextPane : RootSite, IInterlinearTabControl, IHandleBookmark
	{
		int m_hvoRoot; // The Text.
		RawTextVc m_vc;
		XmlNode m_configurationParameters;
		private int m_lastFoundAnnotationHvo = 0;
		/// <summary>
		/// this is the clerk, if any, that determines the text for our control.
		/// </summary>
		RecordClerk m_clerk;

		private IVwStylesheet m_flexStylesheet;
		private IVwStylesheet m_teStylesheet;

		public RawTextPane() : base(null)
		{
			BackColor = Color.FromKnownColor(KnownColor.Window);
			// EditingHelper.PasteFixTssEvent += new FwPasteFixTssEventHandler(OnPasteFixWs);
			DoSpellCheck = true;
			AcceptsTab = false;
		}

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
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_clerk = null;
			m_vc = null;
			m_configurationParameters = null;
		}

		#endregion IDisposable override

		#region implemention of IChangeRootObject

		public virtual void SetRoot(int hvo)
		{
			CheckDisposed();


			if (hvo != m_hvoRoot || m_vc == null)
			{
				SetStyleSheet(hvo);
				m_hvoRoot = hvo;
				SetupVc();
				ChangeOrMakeRoot(m_hvoRoot, m_vc, (int)StTextFrags.kfrText, m_styleSheet);
			}
			this.BringToFront();
			if (m_hvoRoot == 0)
				return;
			// if editable, parse the text to make sure annotations are in a valid initial state
			// with respect to the text so AnnotatedTextEditingHelper can make the right changes
			// to annotations effected by MonitorTextsEdits being true;
			if (m_vc != null && m_vc.Editable)
			{
				if (InterlinMaster.HasParagraphNeedingParse(RootObject))
				{
					NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
												   () =>
													   { InterlinMaster.LoadParagraphAnnotationsAndGenerateEntryGuessesIfNeeded(RootObject, false); });
				}
			}
		}

		/// <summary>
		/// We can't set the style for Scripture...that has to follow some very specific rules implemented in TE.
		/// </summary>
		public override bool CanApplyStyle
		{
			get { return base.CanApplyStyle && !ScriptureServices.ScriptureIsResponsibleFor(m_rootObj); }
		}


		private void SetStyleSheet(int hvo)
		{
			var text = hvo == 0 ? null : (IStText)Cache.ServiceLocator.GetObject(hvo);

			IVwStylesheet wantedStylesheet = m_styleSheet;
			if (text != null && ScriptureServices.ScriptureIsResponsibleFor(text))
			{
				// Use the Scripture stylesheet
				if (m_teStylesheet == null)
				{
					m_flexStylesheet = m_styleSheet; // remember the default.
					var stylesheet = new FwStyleSheet();
					stylesheet.Init(Cache, Cache.LangProject.TranslatedScriptureOA.Hvo, ScriptureTags.kflidStyles);
					m_teStylesheet = stylesheet;
				}
				wantedStylesheet = m_teStylesheet;
			}
			else if (m_flexStylesheet != null)
			{
				wantedStylesheet = m_flexStylesheet;
			}
			if (wantedStylesheet != m_styleSheet)
			{
				m_styleSheet = wantedStylesheet;
				// Todo: set up the comobo; set the main window one.
			}
		}

		#endregion


		/// <summary>
		/// this is the clerk, if any, that determines the text for our control.
		/// </summary>
		internal RecordClerk Clerk
		{
			get { return m_clerk; }
		}

		IStText m_rootObj = null;
		public IStText RootObject
		{
			get
			{
				if (m_rootObj == null || m_rootObj.Hvo != m_hvoRoot)
				{
					if (m_hvoRoot != 0)
						m_rootObj = Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(m_hvoRoot);
					else
						m_rootObj = null;
				}
				return m_rootObj;
			}
		}

		internal int LastFoundAnnotationHvo
		{
			get
			{
				CheckDisposed();
				return m_lastFoundAnnotationHvo;
			}
		}


		#region Overrides of RootSite
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the root box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_fdoCache == null || DesignMode || m_hvoRoot == 0)
				return;

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);

			int wsFirstPara = GetWsOfFirstWordOfFirstTextPara();
			m_vc = new RawTextVc(m_rootb, m_fdoCache, wsFirstPara);
			SetupVc();

			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;

			m_rootb.SetRootObject(m_hvoRoot, m_vc, (int)StTextFrags.kfrText, m_styleSheet);

			base.MakeRoot();
		}

		/// <summary>
		/// Returns WS of first character of first paragraph of m_hvoRoot text.
		/// It defaults to DefaultVernacularWs in case of a problem.
		/// </summary>
		/// <returns></returns>
		private int GetWsOfFirstWordOfFirstTextPara()
		{
			Debug.Assert(m_hvoRoot > 0, "No StText Hvo!");
			int wsFirstPara = Cache.DefaultVernWs;
			var txt = Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(m_hvoRoot);
			if (txt.ParagraphsOS == null || txt.ParagraphsOS.Count == 0)
				return wsFirstPara;

			var firstPara = ((IStTxtPara) txt.ParagraphsOS[0]);
			return firstPara.Contents.get_WritingSystem(0);
		}

		private void SetupVc()
		{
			if (m_vc == null || m_hvoRoot == 0)
				return;
			int wsFirstPara = -1;
			wsFirstPara = GetWsOfFirstWordOfFirstTextPara();
			if (wsFirstPara == -1)
			{
				// The paragraph's first character has no valid writing system...this seems to be possible
				// when it consists entirely of a picture. Rather than crashing, presume the default.
				wsFirstPara = Cache.DefaultVernWs;
			}
			m_vc.SetupVernWsForText(wsFirstPara);
			IStText stText = Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(m_hvoRoot);
			if (m_configurationParameters != null)
			{
				m_vc.Editable = SIL.Utils.XmlUtils.GetOptionalBooleanAttributeValue(
					m_configurationParameters, "editable", true);
				m_vc.Editable &= !ScriptureServices.ScriptureIsResponsibleFor(stText);
			}
		}

		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			base.HandleSelectionChange(rootb, vwselNew);

			// JohnT: it's remotely possible that the base, in calling commit, made this
			// selection no longer useable.
			if (!vwselNew.IsValid)
				return;

			SelectionHelper helper = SelectionHelper.Create(vwselNew, this);
			if (helper != null && helper.GetTextPropId(SelectionHelper.SelLimitType.Anchor) == RawTextVc.kTagUserPrompt)
			{
				vwselNew.ExtendToStringBoundaries();
				EditingHelper.SetKeyboardForSelection(vwselNew);
			}
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (Parent == null)
				return; // width is meaningless, no point in doing extra work
			// In a tab page this panel occupies the whole thing, so layout is wasted until
			// our size is adjusted to match.
			if (Parent is TabPage && (Parent.Width - Parent.Padding.Horizontal) != this.Width)
				return;
			base.OnLayout (levent);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The user has attempted to delete something which the system does not inherently
		/// know how to delete. The dpt argument indicates the type of problem.
		/// </summary>
		/// <param name="sel">The selection</param>
		/// <param name="dpt">Problem type</param>
		/// <returns>response value</returns>
		/// ------------------------------------------------------------------------------------
		public override VwDelProbResponse OnProblemDeletion(IVwSelection sel,
			VwDelProbType dpt)
		{
			CheckDisposed();

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
		/// <param name="e"></param>
		protected override void Draw(PaintEventArgs e)
		{
			if (m_rootb != null && (m_dxdLayoutWidth > 0) && !DesignMode)
			{
				base.Draw(e);
			}
			else
			{
				e.Graphics.FillRectangle(SystemBrushes.Window, this.ClientRectangle);
			}
		}

		public void HandleKeyDownAndKeyPress(Keys key)
		{
			KeyEventArgs kea = new KeyEventArgs(key);
			if (EditingHelper.HandleOnKeyDown(kea))
				return;
			OnKeyDown(kea);
			// for some reason OnKeyPress does not handle Delete key
			// In FLEX, OnKeyPress does not even get called for Delete key.
			if (key != Keys.Delete)
				OnKeyPress(new KeyPressEventArgs((char)kea.KeyValue));
		}

		/// <summary>
		/// Handle a right mouse up, invoking an appropriate context menu.
		/// </summary>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// <returns></returns>
		protected override bool DoContextMenu(IVwSelection sel, Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			// Allow base method to handle spell check problems, if any.
			if (base.DoContextMenu(sel, pt, rcSrcRoot, rcDstRoot))
				return true;

			XWindow mainWind = this.ParentForm as XWindow;
			if (mainWind == null || sel == null)
				return false;
			ITsString tssWord;

			sel.GrowToWord().GetSelectionString(out tssWord, " ");
			TemporaryColleagueParameter tempColleague = null;
			CmObjectUi ui = null;
			try
			{
				if (tssWord != null && !string.IsNullOrEmpty(tssWord.Text))
				{
					// We can have a WfiWordformUi as an additional colleague to handle more menu items.
					// The temporaray colleague handles adding and removing it.
					// Specifically this is needed for the "Show in Word Analyses" and "Show Wordform in Concordance" menu items.
					// See FWR-958 and be SURE to test this if you think about removing this.

					IWfiWordform form = null;

					NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor,
										() => { form = WfiWordformServices.FindOrCreateWordform(Cache, tssWord); });

					ui = CmObjectUi.MakeUi(Cache, form.Hvo);
					ui.Mediator = m_mediator;
					tempColleague = new TemporaryColleagueParameter(m_mediator, ui, false);
				}

				mainWind.ShowContextMenu("mnuIText-RawText", new Point(Cursor.Position.X, Cursor.Position.Y),
					tempColleague, null);
				return true;
			}
			finally
			{
				if (ui != null)
					ui.Dispose();
			}
		}

		internal RecordClerk ActiveClerk
		{
			get
			{
				if (m_mediator != null)
					return m_mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk;
				else
					return null;
			}
		}

		/// <summary>
		/// Currently detects whether we've inserted a paragraph break (with the Enter key)
		/// and move annotations into the new paragraph.
		/// </summary>
		internal class AnnotationMoveHelper : RecordClerk.ListUpdateHelper
		{
			RawTextPane m_rootSite;

			internal AnnotationMoveHelper(RawTextPane site, KeyPressEventArgs e)
				: base(site.Clerk)
			{
				m_rootSite = site;
				if (!CanEdit())
					return;
				SkipShowRecord = true;
			}

			internal bool CanEdit()
			{
				return m_rootSite.m_hvoRoot != 0 && m_rootSite != null && !m_rootSite.IsDisposed && !m_rootSite.ReadOnlyView && m_rootSite.m_vc.Editable;
			}
		}

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
			//rgsli[1].tag = (int)FDO.Ling.Text.TextTags.kflidContents;
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
					Focus();
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
			CheckDisposed();
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
				return null;
			// REVISIT (EricP) Need to check if Ws is IsRightToLeft?
			var sel2 = sel.EndBeforeAnchor ? sel.EndPoint(true) : sel.EndPoint(false);
			if (sel2 == null)
				return null;
			var sel3 = sel2.GrowToWord();
			return sel3;
		}

		/// <summary>
		/// Look up the selected wordform in the dictionary and display its lexical entry.
		/// </summary>
		/// <param name="argument"></param>
		public bool OnLexiconLookup(object argument)
		{
			CheckDisposed();

			IVwSelection wordsel = SelectionBeginningGrowToWord(m_rootb.Selection);
			if (wordsel == null)
				return false;
			ITsString tss;
			int ichMin, ichLim, hvo, tag, ws;
			bool fAssocPrev;
			wordsel.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvo, out tag,
				out ws);
			wordsel.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag,
				out ws);

			if (ichLim > 0)
			{
				LexEntryUi.DisplayOrCreateEntry(m_fdoCache, hvo, tag, ws, ichMin, ichLim, this,
					m_mediator, m_mediator.HelpTopicProvider, "UserHelpFile");
			}
			return true;
		}

		/// <summary>
		/// Returns true if there's anything to select.  This is needed so that the toolbar
		/// button is disabled when there's nothing to select and look up.  Otherwise, crashes
		/// can result when it's clicked but there's nothing there to process!  It's misleading
		/// to the user if nothing else.  It would be nice if the processing could be minimized,
		/// but this seems to be minimal.
		/// </summary>
		/// <returns>true</returns>
		public bool LexiconLookupEnabled()
		{
			CheckDisposed();

			if (m_rootb == null)
				return false;
			IVwSelection sel = m_rootb.Selection;
			if (sel == null)
				return false;
			IVwSelection sel2 = sel.EndPoint(false);
			if (sel2 == null)
				return false;
			IVwSelection sel3 = sel2.GrowToWord();
			if (sel3 == null)
				return false;
			ITsString tss;
			int ichLim, hvo, tag, ws;
			bool fAssocPrev;
			sel3.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag,
				out ws);
			if (ichLim == 0)
				return false;
			// We're not disqualified, so we must be enabled!
			return true;
		}

		public bool OnDisplayGuessWordBreaks(object commandObject,
	ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Visible = true;
			display.Enabled = RootBox != null && RootBox.Selection != null;
			return true;
		}

		void Swap(ref int first, ref int second)
		{
			int temp = first;
			first = second;
			second = temp;
		}

		/// <summary>
		/// Guess where we can break words.
		/// </summary>
		/// <param name="argument"></param>
		public void OnGuessWordBreaks(object argument)
		{
			CheckDisposed();

			IVwSelection sel = RootBox.Selection;
			ITsString tss;
			int ichMin, hvoStart, ichLim, hvoEnd, tag, ws;
			bool fAssocPrev;
			sel.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvoStart,
				out tag, out ws);
			sel.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvoEnd, out tag, out ws);
			if (sel.EndBeforeAnchor)
			{
				Swap(ref ichMin, ref ichLim);
				Swap(ref hvoStart, ref hvoEnd);
			}
			WordBreakGuesser guesser = new WordBreakGuesser(m_fdoCache, hvoStart);
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
				bool fProcessing = false;
				ISilDataAccess sda = m_fdoCache.MainCacheAccessor;
				int hvoStText = m_hvoRoot;
				int cpara = sda.get_VecSize(hvoStText, StTextTags.kflidParagraphs);
				for (int i = 0; i < cpara; i++)
				{
					int hvoPara = sda.get_VecItem(hvoStText, StTextTags.kflidParagraphs, i);
					if (hvoPara == hvoStart)
						fProcessing = true;
					else if (hvoPara == hvoEnd)
						break;
					else if (fProcessing)
					{
						guesser.Guess(0, -1, hvoPara);
					}
				}
				guesser.Guess(0, ichLim, hvoEnd);
			}
		}

		/// <summary>
		/// Save the configuration parameters in case we want to use them locally.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		public override void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			CheckDisposed();

			base.Init (mediator, configurationParameters);
			m_configurationParameters = configurationParameters;
			m_clerk = ToolConfiguration.FindClerk(m_mediator, m_configurationParameters);
			m_styleSheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
		}
	}

	// Raw text VC extracts displays the Contents of a Text using the regular StVc.
	class RawTextVc : StVc
	{
		public const int kTagUserPrompt = 1000009879; // very large number prevents auto-load.

		IVwRootBox m_rootb;

		public RawTextVc(IVwRootBox rootb, FdoCache cache, int wsFirstPara) : base("Normal", wsFirstPara)
		{
			m_rootb = rootb;
			Cache = cache;
			// This is normally done in the Cache setter, but not if the default WS is already set.
			// I'm not sure why not, but rather than mess with a shared base class, we'll just
			// fix it here.
			SetupVernWsForText(m_wsDefault);
			this.Lazy = true;
		}

		internal void SetupVernWsForText(int wsVern)
		{
			m_wsDefault = wsVern;
			IWritingSystem defWs = Cache.ServiceLocator.WritingSystemManager.Get(wsVern);
			RightToLeft = defWs.RightToLeftScript;
		}

		// This evaluates a paragraph to find out whether to display a user prompt, and if so,
		// inserts one.
		protected override bool InsertParaContentsUserPrompt(IVwEnv vwenv, int paraHvo)
		{
			// The only easy solution for LT-1437 "Pasting in a text produces unequal results"
			// is to not have the user prompt!
			return false;
			//ISilDataAccess sda = vwenv.DataAccess;
			// If our hvo is not the first and only paragraph of an owning StText, it isn't
			// interesting.
			//int hvoOwner = sda.get_ObjectProp(hvo,
			//	(int)CmObjectFields.kflidCmObject_Owner);
			//if (sda.get_VecItem(hvoOwner, (int) StText.StTextTags.kflidParagraphs, 0) != hvo)
			//	return false;
			//if (sda.get_VecSize(hvoOwner, (int) StText.StTextTags.kflidParagraphs) > 1)
			//	return false;
			// Also if it isn't empty.
			//if (sda.get_StringProp(hvo, (int)StTxtPara.StTxtParaTags.kflidContents).
			//	Length > 0)
			//{
			//	return false;
			//}
			//vwenv.NoteDependency(new int[] { hvo},
			//	new int[] { (int)StTxtPara.StTxtParaTags.kflidContents}, 1);
			//vwenv.AddProp(kTagUserPrompt, this, 1);
			//return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to set the base WS and direction according to the
		/// first run in the paragraph contents.
		/// </summary>
		/// <value>
		/// 	<c>true</c> to base the direction on para contents; <c>false</c> to use the
		/// 	default writing system of the view constructor.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public override bool BaseDirectionOnParaContents
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the BaseWs and RightToLeft properties for the paragraph that is being laid out.
		/// These are computed (if possible) from the current paragraph; otherwise, use the
		/// default as set on the view contructor for the whole text. This override also sets
		/// the alignment (which presumably overrides the alignment set in the stylesheet?).
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="paraHvo">The HVO of the paragraph.</param>
		/// ------------------------------------------------------------------------------------
		protected override void SetupWsAndDirectionForPara(IVwEnv vwenv, int paraHvo)
		{
			base.SetupWsAndDirectionForPara(vwenv, paraHvo);

			vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
				(int)FwTextPropVar.ktpvEnum,
				RightToLeft ? (int)FwTextAlign.ktalRight : (int)FwTextAlign.ktalLeft);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override ITsString UpdateProp(IVwSelection vwsel, int hvo, int tag, int frag, ITsString tssVal)
		{
			Debug.Assert(tag == kTagUserPrompt, "Got an unexpected tag");

			// Get information about current selection
			int cvsli = vwsel.CLevels(false);
			cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ihvoEnd;
			bool fAssocPrev;
			int ws;
			ITsTextProps ttp;
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(vwsel, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttp);

			// get para info
			IStTxtPara para = Cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(hvo);
//			ITsTextProps props = StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs);
//
//			// set string info based on the para info
//			ITsStrBldr bldr = (ITsStrBldr)tssVal.GetBldr();
//			bldr.SetProperties(0, bldr.Length, props);
//			tssVal = bldr.GetString();

			// Add the text the user just typed to the paragraph - this destroys the selection
			// because we replace the user prompt.
			para.Contents = tssVal;

			// now restore the selection
			m_rootb.MakeTextSelection(ihvoRoot, cvsli, rgvsli,
				StTxtParaTags.kflidContents, cpropPrevious, ichAnchor, ichEnd,
				Cache.DefaultVernWs, fAssocPrev, ihvoEnd, null, true);

			return tssVal;
		}

		/// <summary>
		/// We only use this to generate our empty text prompt.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="tag"></param>
		/// <param name="frag"></param>
		/// <returns></returns>
		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, int frag)
		{
			string userPrompt = ITextStrings.ksEnterOrPasteHere;

			ITsPropsBldr ttpBldr = TsPropsBldrClass.Create();
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault, Color.LightGray.ToArgb());
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, Cache.DefaultUserWs);
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, userPrompt, ttpBldr.GetTextProps());
			// Begin the prompt with a zero-width space in the vernacular writing system (with
			// no funny colors).  This ensures anything the user types (or pastes from a non-FW
			// clipboard) is put in that WS.
			// 200B == zero-width space.
			ITsPropsBldr ttpBldr2 = TsPropsBldrClass.Create();
			ttpBldr2.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, Cache.DefaultVernWs);
			bldr.Replace(0, 0, "\u200B", ttpBldr2.GetTextProps());
			return bldr.GetString();
		}

		public override ITsTextProps CaptionProps
		{
			get
			{
				ITsPropsBldr bldr = TsPropsBldrClass.Create();
				bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Dictionary-Pictures");
				bldr.SetIntPropValues((int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum,
					(int)TptEditable.ktptNotEditable);
				return bldr.GetTextProps();
			}
		}
	}
}
