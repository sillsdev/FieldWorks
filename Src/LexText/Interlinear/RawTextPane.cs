using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.XWorks;
using XCore;
using System.Xml;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// RawTextPane displays an StText using the standard VC, except that if it is empty altogether,
	/// we display a message. (Eventually.)
	/// </summary>
	public class RawTextPane : RootSite
	{
		int m_hvoRoot; // The Text.
		RawTextVc m_vc;
		System.Xml.XmlNode m_configurationParameters;
		private int m_lastFoundAnnotationHvo = 0;
		/// <summary>
		/// this is the clerk, if any, that determines the text for our control.
		/// </summary>
		RecordClerk m_clerk;

		public RawTextPane() : base(null)
		{
			this.BackColor = Color.FromKnownColor(KnownColor.Window);
			// EditingHelper.PasteFixTssEvent += new FwPasteFixTssEventHandler(OnPasteFixWs);
			this.DoSpellCheck = true;
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_vc != null)
					m_vc.Dispose();
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


			if (hvo != m_hvoRoot)
			{
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
				bool fDidParse;
				InterlinMaster.ParseText(Cache, m_hvoRoot, Mediator, out fDidParse);
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
						m_rootObj = new StText(Cache, m_hvoRoot);
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

			int wsFirstPara = Cache.LangProject.ActualWs(LangProject.kwsVernInParagraph, m_hvoRoot,
				(int)StText.StTextTags.kflidParagraphs);
			m_vc = new RawTextVc(m_rootb, m_fdoCache, wsFirstPara);
			SetupVc();

			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;

			m_rootb.SetRootObject(m_hvoRoot, m_vc, (int)StTextFrags.kfrText, m_styleSheet);

			base.MakeRoot();
		}

		private void SetupVc()
		{
			if (m_vc == null || m_hvoRoot == 0)
				return;
			int wsFirstPara = Cache.LangProject.ActualWs(LangProject.kwsVernInParagraph, m_hvoRoot,
				(int)StText.StTextTags.kflidParagraphs);
			if (wsFirstPara == -1)
			{
				// The paragraph's first character has no valid writing system...this seems to be possible
				// when it consists entirely of a picture. Rather than crashing, presume the default.
				wsFirstPara = Cache.DefaultVernWs;
			}
			m_vc.SetupVernWsForText(wsFirstPara);
			StText stText = new StText(Cache, m_hvoRoot);
			if (m_configurationParameters != null)
			{
				m_vc.Editable = SIL.Utils.XmlUtils.GetOptionalBooleanAttributeValue(
					m_configurationParameters, "editable", true) && !Scripture.IsResponsibleFor(stText);
			}
		}

		public override void SelectionChanged(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			base.SelectionChanged(rootb, vwselNew);

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
			if (Parent is TabPage && Parent.Width != this.Width)
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

		public override bool HandleTabAsControl
		{
			get
			{
				return true;
			}
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

		/// <summary>
		/// Overridden to adjust annotations as our text is edited.
		/// </summary>
		public override EditingHelper EditingHelper
		{
			get
			{
				if (m_editingHelper == null)
					m_editingHelper = new AnnotatedTextEditingHelper(Cache, this);
				return m_editingHelper;
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
			if (tssWord != null && !string.IsNullOrEmpty(tssWord.Text))
			{
				// We can have a WfiWordformUi as an additional colleague to handle more menu items.
				// The temporaray colleague handles adding and removing it.
				int form = WfiWordform.FindOrCreateWordform(Cache, tssWord, true);

				CmObjectUi ui = CmObjectUi.MakeUi(Cache, form);
				ui.Mediator = m_mediator;
				tempColleague = new TemporaryColleagueParameter(m_mediator, ui, false);
			}

			mainWind.ShowContextMenu("mnuIText-RawText", new Point(Cursor.Position.X, Cursor.Position.Y),
				tempColleague, null);
			return true;
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

		internal void SelectAnnotation(int hvoStText, int hvoPara, int hvoAnn)
		{
			CheckDisposed();

			// get the annotation properties
			ISilDataAccess sda = Cache.MainCacheAccessor;
			int annBeginOffset = sda.get_IntProp(hvoAnn, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
			int annEndOffset = sda.get_IntProp(hvoAnn, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidEndOffset);
			int annWSid = sda.get_ObjectProp(hvoAnn, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidWritingSystem);

			// Now select the annotated word.
			int ihvoPara = GetParagraphIndexForPara(Cache, hvoStText, hvoPara);
			MakeTextSelectionAndScrollToView(annBeginOffset, annEndOffset, annWSid, ihvoPara);
		}

		public void MakeTextSelectionAndScrollToView(int ichMin, int ichLim, int ws, int ihvoPara)
		{
			MakeTextSelectionAndScrollToView(ichMin, ichLim, ws, ihvoPara, -1);// end in same prop
		}

		protected void MakeTextSelectionAndScrollToView(int ichMin, int ichLim, int ws, int ihvoPara, int ihvoEnd)
		{
			SelLevInfo[] rgsli = new SelLevInfo[1];
			// entry 0 says which StTextPara
			rgsli[0].ihvo = ihvoPara;
			rgsli[0].tag = (int)StText.StTextTags.kflidParagraphs;
			// entry 1 says to use the Contents of the Text.
			//rgsli[1].tag = (int)FDO.Ling.Text.TextTags.kflidContents;
			try
			{
				RootBox.MakeTextSelection(0, rgsli.Length, rgsli,
					(int)StTxtPara.StTxtParaTags.kflidContents, 0, ichMin,
					ichLim, ws,
					false, // Range, arbitrary assoc prev.
					ihvoEnd,
					null, // don't set any special text props for typing
					true); // install it
				Focus();
				// Scroll this selection into View.
				IVwSelection sel = this.RootBox.Selection;
				this.ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
				Update();
			}
			catch (Exception)
			{
			}
		}

		static internal int GetParagraphIndexForAnnotation(FdoCache cache, int annHvo)
		{
			int hvoPara = cache.MainCacheAccessor.get_ObjectProp(annHvo, (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			int hvoStText = cache.GetOwnerOfObject(hvoPara);
			return GetParagraphIndexForPara(cache, hvoStText, hvoPara);
		}

		static internal int GetParagraphIndexForPara(FdoCache cache, int hvoStText, int paraHvo)
		{
			ISilDataAccess sda = cache.MainCacheAccessor;
			int cpara = sda.get_VecSize(hvoStText, (int)StText.StTextTags.kflidParagraphs);
			for (int ihvoPara = 0; ihvoPara < cpara; ihvoPara++)
			{
				if (sda.get_VecItem(hvoStText, (int)StText.StTextTags.kflidParagraphs, ihvoPara) == paraHvo)
					return ihvoPara;
			}
			return -1;
		}

		/// <summary>
		/// Returns hvo of the CmBaseAnnotation corresponding to the given bookmark location in the text.
		/// </summary>
		/// <param name="bookmark"></param>
		/// <param name="fOnlyTWFIC">If true, we only search in TWFIC annotations, otherwise we
		/// search for a match in TextSegments also.</param>
		/// <returns></returns>
		static public int AnnotationHvo(FdoCache cache, IStText stText, InterAreaBookmark bookmark, bool fOnlyTWFIC)
		{
			if (bookmark.IndexOfParagraph < 0 || stText == null ||
				bookmark.IndexOfParagraph >= stText.ParagraphsOS.Count)
				return 0;
			IStTxtPara para = stText.ParagraphsOS[bookmark.IndexOfParagraph] as IStTxtPara;
			bool fExactMatch = false;
			int hvoAnn = FindAnnotationHvoForStTxtPara(cache, para.Hvo,
					bookmark.BeginCharOffset, bookmark.EndCharOffset, fOnlyTWFIC, out fExactMatch);
			return hvoAnn;
		}

		/// <summary>
		/// Return the hvo for the text-word-in-context (twfic) CmBaseAnnotation that
		/// matches the given boundaries in an StTxtPara object.
		/// If no exact match is found, return the nearest segment (preferably the following word).
		/// </summary>
		/// <param name="hvoStTxtPara"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="fExactMatch"> true if we return an exact match, false otherwise.</param>
		/// <returns>hvo is 0 if not found.</returns>
		private int FindTwficHvoForStTxtPara(int hvoStTxtPara, int ichMin, int ichLim, out bool fExactMatch)
		{
			return FindAnnotationHvoForStTxtPara(Cache, hvoStTxtPara, ichMin, ichLim, true, out fExactMatch);
		}

		static private ICmBaseAnnotation FindClosestAnnotation(FdoCache cache, int[] annotations, int hvoAnnotationType, int ichMin, int ichLim, out bool fExactMatch)
		{
			ICmBaseAnnotation cbaClosest = null;
			fExactMatch = false;	// default
			ICmBaseAnnotation cba = null;
			for (int iann = 0; iann < annotations.Length; ++iann)
			{
				cba = (ICmBaseAnnotation)CmObject.CreateFromDBObject(cache, annotations[iann], false);
				if (hvoAnnotationType != 0 && cba.AnnotationTypeRAHvo != hvoAnnotationType)
					continue;
				if (cba.BeginOffset == ichMin && cba.EndOffset == ichLim)
				{
					fExactMatch = true;
					cbaClosest = cba;
					break;
				}
				else
				{
					if (ichMin >= cba.BeginOffset && ichLim <= cba.EndOffset)
					{
						// this annotation contains the requested annotation. Return it.
						cbaClosest = cba;
						break;
					}
					else if (cbaClosest == null ||
						Math.Abs(cba.BeginOffset - ichMin) < Math.Abs(cbaClosest.BeginOffset - ichMin))
					{
						cbaClosest = cba;
					}
				}
			}
			return cbaClosest;
		}

		/// <summary>
		/// Return the hvo for theCmBaseAnnotation that
		/// matches the given boundaries in an StTxtPara object.
		/// If no exact match is found, return the nearest segment (preferably the following word).
		/// </summary>
		/// <param name="hvoStTxtPara"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichLim"></param>
		/// <param name="fOnlyTWFIC">true, if restricted to TWFIC</param>
		/// <param name="fExactMatch"> true if we return an exact match, false otherwise.</param>
		/// <returns>hvo is 0 if not found.</returns>
		static internal int FindAnnotationHvoForStTxtPara(FdoCache cache, int hvoStTxtPara, int ichMin, int ichLim, bool fOnlyTWFIC, out bool fExactMatch)
		{
			int twficType = CmAnnotationDefn.Twfic(cache).Hvo;
			int textSegType = CmAnnotationDefn.TextSegment(cache).Hvo;
			fExactMatch = false;
			int clsid = cache.GetClassOfObject(hvoStTxtPara);
			if (clsid != StTxtPara.kClassId)
			{
				Debug.Assert(clsid != StTxtPara.kClassId, "hvoStTxtPara should be of class StTxtPara.");
				return 0;
			}
			int kflidParaSegment = InterlinVc.ParaSegmentTag(cache);
			ISilDataAccess sda = cache.MainCacheAccessor;
			// first find the closest segment.
			int[] segments = cache.GetVectorProperty(hvoStTxtPara, kflidParaSegment, true);
			ICmBaseAnnotation cbaClosestSeg = FindClosestAnnotation(cache, segments, textSegType, ichMin, ichLim, out fExactMatch);
			if (cbaClosestSeg == null)
				return 0;
			// if it was an exact match for a segment, return it.
			if (cbaClosestSeg != null && fExactMatch && !fOnlyTWFIC)
				return cbaClosestSeg.Hvo;
			// otherwise, see if we can find a closer wordform
			int[] segmentForms = cache.GetVectorProperty(cbaClosestSeg.Hvo, InterlinVc.SegmentFormsTag(cache), true);
			ICmBaseAnnotation cbaClosestWf = FindClosestAnnotation(cache, segmentForms, twficType, ichMin, ichLim, out fExactMatch);
			if (cbaClosestWf == null)
			{
				return fOnlyTWFIC ? 0 : cbaClosestSeg.Hvo;
			}
			return cbaClosestWf.Hvo;
		}

		internal void SelectBookMark(InterAreaBookmark bookmark)
		{
			CheckDisposed();
			MakeTextSelectionAndScrollToView(bookmark.BeginCharOffset, bookmark.EndCharOffset, 0, bookmark.IndexOfParagraph);
		}

		/// <summary>
		/// Return annotation Hvo of the closest word-segment to the beginning of the current selection.
		/// </summary>
		/// <returns></returns>
		public int AnnotationHvoClosestToSelection()
		{
			CheckDisposed();

			IVwSelection sel = SelectionBeginningGrowToWord(m_rootb.Selection);	// find segment related to current selection
			if (sel == null)
				return 0;
			ITsString tss;
			int ichMin, ichLim, hvoStTxtPara, tag, ws;
			bool fAssocPrev;
			sel.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvoStTxtPara, out tag,
				out ws);
			sel.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvoStTxtPara, out tag,
				out ws);

			bool fExactMatch;
			int hvoAnnotation = FindTwficHvoForStTxtPara(hvoStTxtPara, ichMin, ichLim, out fExactMatch);
			m_lastFoundAnnotationHvo = hvoAnnotation;
			return hvoAnnotation;
		}

		/// <summary>
		/// Return a word selection based on the beginning of the current selection.
		/// Here the "beginning" of the selection is the offset corresponding to word order,
		/// not the selection anchor.
		/// </summary>
		/// <returns>null if we couldn't handle the selection</returns>
		static public IVwSelection SelectionBeginningGrowToWord(IVwSelection sel)
		{
			if (sel == null)
				return null;
			// REVISIT (EricP) Need to check if Ws is IsRightToLeft?
			IVwSelection sel2 = sel.EndBeforeAnchor ? sel.EndPoint(true) : sel.EndPoint(false);
			if (sel2 == null)
				return null;
			IVwSelection sel3 = sel2.GrowToWord();
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
				// TODO (TimS): need to provide help to the dialog (last 2 params)
				LexEntryUi.DisplayOrCreateEntry(m_fdoCache, hvo, tag, ws, ichMin, ichLim, this,
					m_mediator, FwApp.App, "UserHelpFile");
			}

//			LexEntryUi leui = LexEntryUi.FindEntryForWordform(m_fdoCache, hvo, tag, ichMin, ichLim);
//			if (leui == null)
//			{
//				MessageBox.Show(this, "Sorry...could not find a matching entry in the lexicon");
//				return false;
//			}
//			leui.ShowSummaryDialog(this);
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

		/// <summary>
		/// Handle fixing the writing system values inside pasted text.  Eventually, we may
		/// want some form of UI to guide the process, but for now we just bash in the
		/// default vernacular ws wherever any other ws occurs.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void OnPasteFixWs(EditingHelper sender, FwPasteFixTssEventArgs args)
		{
			if (args.EventHandled)
				return;					// only one delegate gets to play!
			bool fFixed = false;
			int wsVern = Cache.LangProject.ActualWs(LangProject.kwsVernInParagraph, m_hvoRoot, (int)StText.StTextTags.kflidParagraphs);
			ITsStrBldr tsb = args.TsString.GetBldr();
			int crun = tsb.RunCount;
			for (int irun = 0; irun < crun; ++irun)
			{
				ITsTextProps ttp = tsb.get_Properties(irun);
				int var;
				int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
				if (ws != wsVern)
				{
					fFixed = true;
					int ichMin;
					int ichLim;
					tsb.GetBoundsOfRun(irun, out ichMin, out ichLim);
					tsb.SetIntPropValues(ichMin, ichLim,
						(int)FwTextPropType.ktptWs, var, wsVern);
				}
			}
			if (fFixed)
				args.TsString = tsb.GetString();
			args.EventHandled = true;
		}

		private void Swap (ref int first, ref int second)
		{
			int temp = first;
			first = second;
			second = temp;
		}

		public bool OnDisplayGuessWordBreaks(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Visible = true;
			display.Enabled = RootBox != null && RootBox.Selection != null;
			return true;
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
				ISilDataAccess sda =  m_fdoCache.MainCacheAccessor;
				int hvoStText = m_hvoRoot;
				int cpara = sda.get_VecSize(hvoStText, (int) StText.StTextTags.kflidParagraphs);
				for (int i = 0; i < cpara; i++)
				{
					int hvoPara = sda.get_VecItem(hvoStText, (int) StText.StTextTags.kflidParagraphs, i);
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
			LgWritingSystem defWs = new LgWritingSystem(Cache, wsVern);
			RightToLeft = defWs.RightToLeft;
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_rootb = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		// This evaluates a paragraph to find out whether to display a user prompt, and if so,
		// inserts one.
		protected override bool InsertParaContentsUserPrompt(IVwEnv vwenv, int hvo)
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
		/// <param name="hvoPara">The hvo para.</param>
		/// ------------------------------------------------------------------------------------
		protected override void SetupWsAndDirectionForPara(IVwEnv vwenv, int hvoPara)
		{
			base.SetupWsAndDirectionForPara(vwenv, hvoPara);

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
			CheckDisposed();

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
			StTxtPara para = new StTxtPara(Cache, hvo);
//			ITsTextProps props = StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs);
//
//			// set string info based on the para info
//			ITsStrBldr bldr = (ITsStrBldr)tssVal.GetBldr();
//			bldr.SetProperties(0, bldr.Length, props);
//			tssVal = bldr.GetString();

			// Add the text the user just typed to the paragraph - this destroys the selection
			// because we replace the user prompt.
			para.Contents.UnderlyingTsString = tssVal;

			// now restore the selection
			m_rootb.MakeTextSelection(ihvoRoot, cvsli, rgvsli,
				(int)StTxtPara.StTxtParaTags.kflidContents, cpropPrevious, ichAnchor, ichEnd,
				Cache.DefaultVernWs, fAssocPrev, ihvoEnd, null, true);

			return tssVal;
		}

		/// <summary>
		/// We only use this to generate our empty text prompt.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="tag"></param>
		/// <param name="v"></param>
		/// <param name="frag"></param>
		/// <returns></returns>
		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, object v, int frag)
		{
			CheckDisposed();

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
				CheckDisposed();
				ITsPropsBldr bldr = TsPropsBldrClass.Create();
				bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Dictionary-Pictures");
				bldr.SetIntPropValues((int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum,
					(int)TptEditable.ktptNotEditable);
				return bldr.GetTextProps();
			}
		}
	}

	/// <summary>
	/// This class handles adjustments to annotations into a paragraph as the user edits the text.
	/// </summary>
	public class AnnotatedTextEditingHelper : FwEditingHelper
	{
		AnnotationAdjuster m_annotationAdjuster = null;
		// During a key press an annotion move helper is in existence.
		private RawTextPane.AnnotationMoveHelper m_amh;

		public AnnotatedTextEditingHelper(FdoCache cache, IEditingCallbacks callbacks)
			: base(cache, callbacks)
		{
			m_annotationAdjuster = new RawTextPaneAnnotationAdjuster(cache, this);
		}

		/// <summary>
		/// Let the annotation adjuster decide whether we can afford to handle multiple keystrokes as a unit.
		/// Currently the answer is always "no".
		/// </summary>
		public override bool KeepCollectingInput(int nextChar)
		{
			return m_annotationAdjuster.KeepCollectingInput(nextChar);
		}

		/// <summary>
		/// Allow more precise monitoring of text edits and resulting prop changes,
		/// so our AnnotatdTextEditingHelper can adjust annotations appropriately.
		/// </summary>
		/// <value></value>
		public override bool MonitorTextEdits
		{
			get { return true; }
		}

		/// <summary>
		/// overriden to handle the special case where user hits ENTER in a paragraph.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="modifiers"></param>
		/// <param name="graphics"></param>
		public override void OnKeyPress(KeyPressEventArgs e, Keys modifiers, IVwGraphics graphics)
		{
			RawTextPane rtp = Callbacks as RawTextPane;
			if (rtp == null)
				return;
			try
			{
				using (m_amh = new RawTextPane.AnnotationMoveHelper(rtp, e))
				{
					if (m_amh.CanEdit())
						m_annotationAdjuster.StartKeyPressed(e, modifiers);
					base.OnKeyPress(e, modifiers, graphics);
					// in general we don't want to reload the primary clerk
					// while we are editing, since that can be expensive.
					if (rtp.Clerk != null && rtp.Clerk.IsPrimaryClerk)
					{
						m_amh.TriggerPendingReloadOnDispose = false;
						// in some cases we may also want to do Clerk.RemoveInvalidItems()
						// to help prevent the user from crashing when they click on it.
					}
					if (m_amh.CanEdit())
						m_annotationAdjuster.EndKeyPressed();
				}
			}
			finally
			{
				m_amh = null;
			}
		}

		/// <summary>
		/// Give the annotation adjuster the chance to save what it cares about before the edit starts.
		/// </summary>
		public override void OnAboutToEdit()
		{
			base.OnAboutToEdit();
			m_annotationAdjuster.OnAboutToEdit();
		}

		/// <summary>
		/// triggered in Disposing DataUpdateMonitor
		/// </summary>
		public override void OnFinishedEdit()
		{
			base.OnFinishedEdit();
			m_annotationAdjuster.OnFinishedEdit();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (m_annotationAdjuster != null)
					m_annotationAdjuster.Dispose();
			}

			base.Dispose(disposing);

			m_annotationAdjuster = null;
		}

		#region IVwObjDelNotification Members
		public override void AboutToDelete(SelectionHelper selHelper, int hvoObject, int hvoOwner, int tag, int ihvo, bool fMergeNext)
		{
			//base.AboutToDelete(selHelper, hvoObject, hvoOwner, tag, ihvo, fMergeNext);
			m_annotationAdjuster.AboutToDelete(selHelper.Selection, selHelper.RootSite.RootBox, hvoObject, hvoOwner, tag, ihvo, fMergeNext);
		}

		#endregion





	}


}
