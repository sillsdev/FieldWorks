using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
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

		public InterlinDocRootSiteBase()
		{
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
			if (master != null)
			{
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
			using (var dlg = new InterlinearExportDialog(m_mediator, objRoot, m_vc, selectedObjs))
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
					var container = m_objRepo.GetObject(rgvsli[i].hvo);

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
		protected InterlinLineChoices LineChoices { get; set; }

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
		/// <param name="NeedsGuessesUpdated"></param>
		/// <param name="wordforms"></param>
		internal virtual void UpdateGuesses(UpdateGuessesCondition NeedsGuessesUpdated, HashSet<IWfiWordform> wordforms)
		{
			UpdateGuesses(NeedsGuessesUpdated, wordforms, true);
		}

		private void UpdateGuesses(UpdateGuessesCondition NeedsGuessesUpdated, HashSet<IWfiWordform> wordforms, bool fUpdateDisplayWhereNeeded)
		{
			// now update the guesses for the paragraphs.
			ParaDataUpdateTracker pdut = new ParaDataUpdateTracker(m_vc.GuessServices, m_vc.Decorator);
			foreach (IStTxtPara para in RootStText.ParagraphsOS)
			{
				if (NeedsGuessesUpdated(para, wordforms))
					pdut.LoadAnalysisData(para);
				//pdut.LoadParaData(hvoPara); //This also loads all annotations which never affect guesses and take 3 times the number of queries
			}
			if (fUpdateDisplayWhereNeeded)
			{
				// now update the display with the affected annotations.
				foreach (var changed in pdut.ChangedAnnotations)
					UpdateDisplayForOccurrence(changed);
			}
		}

		/// <summary>
		/// Indicates whether we need to reload the guess cache. (after ResetAnalysisCache)
		/// </summary>
		internal bool GuessDataNeedsReload = false;

		/// <summary>
		/// Update all the guesses in the interlinear doc.
		/// </summary>
		internal virtual void UpdateGuessData()
		{
			if (!GuessDataNeedsReload)
				return;
			UpdateGuesses(ForceUpdate, null, false);
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

		internal static bool ForceUpdate(IStTxtPara para, HashSet<IWfiWordform> wordforms)
		{
			return true;
		}

		/// <summary>
		/// Return true if the specified paragraph needs its guesses updated when we've changed something about the analyses
		/// or occurrenes of analyses of one of the specified wordforms.
		/// </summary>
		internal static bool HasMatchingWordformsNeedingAnalysis(IStTxtPara para, HashSet<IWfiWordform> wordforms)
		{
			// If we haven't already figured the segments of a paragraph, we don't need to update it; the guesses will
			// get made when scrolling makes the paragraph visible.
			if (para.SegmentsOS.Count == 0)
				return false;
			foreach (var occurrence in SegmentServices.StTextAnnotationNavigator.GetWordformOccurrencesAdvancingInPara(para))
			{
				var wag = new AnalysisTree(occurrence.Analysis);
				if (wag.Gloss != null)
					continue; // fully glossed, no need to update.
				if (wordforms.Contains(wag.Wordform))
					return true; // This paragraph IS linked to one of the interesting wordforms; needs guesses updated
			}
			return false; // no Wordforms that might be affected.
		}

		internal InterlinMaster GetMaster()
		{
			for (Control parentControl = this.Parent; parentControl != null; parentControl = parentControl.Parent)
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
		}

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
			}
			// Sync Guesses data before we redraw anything.
			UpdateGuessData();
			// FWR-191: we don't need to reconstruct the display if we didn't need to reload annotations
			// but until we detect that condition, we need to redisplay just in case, to keep things in sync.
			// especially if someone edited the baseline.
			ChangeOrMakeRoot(m_hvoRoot, m_vc, InterlinVc.kfragStText, m_styleSheet);
			m_vc.RootSite = this;
			RootBoxNeedsUpdate = false;
		}

		/// <summary>
		/// Do this to force a change/update of the rootbox, even if the root text object is the same.
		/// This is important to do when we edit the text in one (Edit) tab and switch to the next tab.
		/// </summary>
		internal void InvalidateRootBox()
		{
			RootBoxNeedsUpdate = true;
		}

		private bool RootBoxNeedsUpdate { get; set; }

		#endregion IChangeRootObject

		#region IVwNotifyChange Members

		internal bool SuspendResettingAnalysisCache;

		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (SuspendResettingAnalysisCache)
				return;
			//if (tag == CmAnnotationTags.kflidInstanceOf
			//	|| tag == CmAgentEvaluationTags.kflidTarget
			//	|| tag == CmAgentEvaluationTags.kflidAccepted
			//	|| tag == CmAgentTags.kflidEvaluations)
			// Review JohnT: not sure exactly what we're doing here; seems we want to catch changes
			// in what analyses are used or how they are evaluated. The two below would cover this.
			if (tag == WfiAnalysisTags.kflidEvaluations
				|| tag == SegmentTags.kflidAnalyses)
			{
				m_vc.ResetAnalysisCache();
				GuessDataNeedsReload = true;
			}
		}

		#endregion
		public void UpdatingOccurrence(IAnalysis oldAnalysis, IAnalysis newAnalysis)
		{
			if (m_vc.UpdatingOccurrence(oldAnalysis, newAnalysis))
				GuessDataNeedsReload = true;
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
