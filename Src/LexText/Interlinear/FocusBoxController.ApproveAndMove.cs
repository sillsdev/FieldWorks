// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using System.Diagnostics;
using XCore;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.ObjectModel;
using System.Windows.Forms;

namespace SIL.FieldWorks.IText
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FocusBoxController
	{
		internal void ApproveAndStayPut(ICommandUndoRedoText undoRedoText)
		{
			// don't navigate, just save.
			UpdateRealFromSandbox(undoRedoText, true);
		}

		/// <summary>
		/// Approves an analysis and moves the selection to the next wordform or the
		/// next Interlinear line.
		/// Normally, this is invoked as a result of pressing the <Enter> key
		/// or clicking the "Approve and Move Next" green check in an analysis.
		/// </summary>
		internal void ApproveAndMoveNext(ICommandUndoRedoText cmd)
		{
			if (!PreCheckApprove())
				return;

			UndoableUnitOfWorkHelper.Do(cmd.UndoText, cmd.RedoText, Cache.ActionHandlerAccessor,
				() =>
				{
					ApproveAnalysis(SelectedOccurrence, false, true);
				});

			// This should not make any data changes, since we're telling it not to save and anyway
			// we already saved the current annotation. And it can't correctly place the focus box
			// until the change we just did are completed and PropChanged sent. So keep this outside the UOW.
			OnNextBundle(false, false, false, true);
		}

		/// <summary>
		/// Approves an analysis (if there are edits or if fSaveGuess is true and there is a guess) and
		/// moves the selection to target.
		/// </summary>
		/// <param name="target">The occurrence to move to.</param>
		/// <param name="parent">If the FocusBox parent is not set, then use this value to set it.</param>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		/// <param name="fMakeDefaultSelection">true to make the default selection within the new sandbox.</param>
		internal void ApproveAndMoveTarget(AnalysisOccurrence target, InterlinDocForAnalysis parent, bool fSaveGuess, bool fMakeDefaultSelection)
		{
			if (!PreCheckApprove())
				return;

			if (Parent == null)
			{
				Parent = parent;
			}

			UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoApproveAnalysis, ITextStrings.ksRedoApproveAnalysis, Cache.ActionHandlerAccessor,
				() =>
				{
					ApproveAnalysis(SelectedOccurrence, false, fSaveGuess);
				});

			// This should not make any data changes, since we're telling it not to save and anyway
			// we already saved the current annotation. And it can't correctly place the focus box
			// until the change we just did are completed and PropChanged sent. So keep this outside the UOW.
			TargetBundle(target, false, fMakeDefaultSelection);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="undoRedoText">Approving the state of the FocusBox can be associated with
		/// different user actions (ie. UOW)</param>
		/// <param name="fSaveGuess"></param>
		internal void UpdateRealFromSandbox(ICommandUndoRedoText undoRedoText, bool fSaveGuess)
		{
			if (!ShouldCreateAnalysisFromSandbox(fSaveGuess))
				return;

			var origWordform = SelectedOccurrence;
			if (!origWordform.IsValid)
				return; // something (editing elsewhere?) has put things in a bad state; cf LTB-1665.
			var origWag = new AnalysisTree(origWordform.Analysis);
			var undoText = undoRedoText != null ? undoRedoText.UndoText : ITextStrings.ksUndoApproveAnalysis;
			var redoText = undoRedoText != null ? undoRedoText.RedoText : ITextStrings.ksRedoApproveAnalysis;
			var oldAnalysis = SelectedOccurrence.Analysis;
			try
			{
				// Updating one of a segment's analyses would normally reset the analysis cache.
				// And we may have to: UpdatingOccurrence will figure out whether to do it or not.
				// But we don't want it to happen as an automatic side effect of the PropChanged.
				InterlinDoc.SuspendResettingAnalysisCache = true;
				UndoableUnitOfWorkHelper.Do(undoText, redoText,
					Cache.ActionHandlerAccessor, () => ApproveAnalysis(SelectedOccurrence, false, fSaveGuess));
			}
			finally
			{
				InterlinDoc.SuspendResettingAnalysisCache = false;
			}
			var newAnalysis = SelectedOccurrence.Analysis;
			InterlinDoc.UpdatingOccurrence(oldAnalysis, newAnalysis);
			var newWag = new AnalysisTree(origWordform.Analysis);
			var wordforms = new HashSet<IWfiWordform> { origWag.Wordform, newWag.Wordform };
			InterlinDoc.UpdateGuesses(wordforms);
		}

		protected virtual bool ShouldCreateAnalysisFromSandbox(bool fSaveGuess)
		{
			if (SelectedOccurrence == null)
				return false;
			if (InterlinWordControl == null || !InterlinWordControl.ShouldSave(fSaveGuess))
				return false;
			return true;
		}

		private void FinishSettingAnalysis(AnalysisTree newAnalysisTree, IAnalysis oldAnalysis)
		{
			if (newAnalysisTree.Analysis == oldAnalysis)
				return;
			List<int> msaHvoList = new List<int>();
			// Collecting for the new analysis is probably overkill, since the MissingEntries combo will only have MSAs
			// that are already referenced outside of the focus box (namely by the Senses). It's unlikely, therefore,
			// that we could configure the Focus Box in such a state as to remove the last link to an MSA in the
			// new analysis.  But just in case it IS possible...
			IWfiAnalysis newWa = newAnalysisTree.WfiAnalysis;
			if (newWa != null)
			{
				// Make sure this analysis is marked as user-approved (green check mark)
				Cache.LangProject.DefaultUserAgent.SetEvaluation(newWa, Opinions.approves);
			}
		}

		private void SaveAnalysisForAnnotation(AnalysisOccurrence occurrence, AnalysisTree newAnalysisTree)
		{
			Debug.Assert(occurrence != null);
			// Record the old wordform before we alter InstanceOf.
			IWfiWordform oldWf = occurrence.Analysis.Wordform;

			var wfToTryDeleting = occurrence.Analysis as IWfiWordform;

			// This is the property that each 'in context' object has that points at one of the WfiX classes as the
			// analysis of the word.
			occurrence.Analysis = newAnalysisTree.Analysis;

			// It's possible if the new analysis is a different case form that the old wordform is now
			// unattested and should be removed.
			if (wfToTryDeleting != null && wfToTryDeleting != occurrence.Analysis.Wordform)
				wfToTryDeleting.DeleteIfSpurious();
		}

		/// <summary>
		/// We can navigate from one bundle to another if the focus box controller is
		/// actually visible. (Earlier versions of this method also checked it was in the right tool, but
		/// that was when the sandbox included this functionality. The controller is only shown when navigation
		/// is possible.)
		/// </summary>
		protected bool CanNavigateBundles
		{
			get
			{
				return Visible;
			}
		}

		/// <summary>
		/// Move to the next bundle in the direction indicated by fForward. If fSaveGuess is true, save guesses in the current position.
		/// If skipFullyAnalyzedWords is true, move to the next item needing analysis, otherwise, the immediate next.
		/// If fMakeDefaultSelection is true, make the default selection within the moved focus box.
		/// </summary>
		public void OnNextBundle(bool fSaveGuess, bool skipFullyAnalyzedWords, bool fMakeDefaultSelection, bool fForward)
		{
			var nextOccurrence = GetNextOccurrenceToAnalyze(fForward, skipFullyAnalyzedWords);
			// If we are at the end of a segment we should move to the first Translation or note line (if any)
			if(nextOccurrence.Segment != SelectedOccurrence.Segment || nextOccurrence == SelectedOccurrence)
			{
				if (InterlinDoc.SelectFirstTranslationOrNote())
				{
					// We moved to a translation or note line, exit
					return;
				}
			}
			TargetBundle(nextOccurrence, fSaveGuess, fMakeDefaultSelection);
		}

		public void OnNextBundleSkipTranslationOrNoteLine(bool fSaveGuess)
		{
			var nextOccurrence = GetNextOccurrenceToAnalyze(true, true);

			TargetBundle(nextOccurrence, fSaveGuess, true);
		}

		/// <summary>
		/// Move to the target bundle.
		/// </summary>
		/// <param name="target">The occurrence to move to.</param>
		/// <param name="fSaveGuess">if true, saves guesses in the current position; if false, skips guesses but still saves edits.</param>
		/// <param name="fMakeDefaultSelection">true to make the default selection within the moved focus box.</param>
		public void TargetBundle(AnalysisOccurrence target, bool fSaveGuess, bool fMakeDefaultSelection)
		{
			int currentLineIndex = -1;
			if (InterlinWordControl != null)
				currentLineIndex = InterlinWordControl.GetLineOfCurrentSelection();
			InterlinDoc.TriggerAnalysisSelected(target, fSaveGuess, fMakeDefaultSelection);
			if (!fMakeDefaultSelection && currentLineIndex >= 0 && InterlinWordControl != null)
				InterlinWordControl.SelectOnOrBeyondLine(currentLineIndex, 1);
		}

		// It would be nice to have more of this logic in the StTextAnnotationNavigator, but the definition of FullyAnalyzed
		// is dependent on what lines we are displaying.
		private AnalysisOccurrence GetNextOccurrenceToAnalyze(bool fForward, bool skipFullyAnalyzedWords)
		{
			var navigator = new SegmentServices.StTextAnnotationNavigator(SelectedOccurrence);
			var options = fForward
							  ? navigator.GetWordformOccurrencesAdvancingIncludingStartingOccurrence()
							  : navigator.GetWordformOccurrencesBackwardsIncludingStartingOccurrence();
			if (options.First() == SelectedOccurrence)
				options = options.Skip(1);
			if (skipFullyAnalyzedWords)
				options = options.Where(analysis => !IsFullyAnalyzed(analysis));
			return options.DefaultIfEmpty(SelectedOccurrence).FirstOrDefault();
		}

		bool IsFullyAnalyzed(AnalysisOccurrence occ)
		{
			var analysis = occ.Analysis;
			// I don't think we're ever passed punctuation, but if so, it doesn't need any further analysis.
			if (analysis is IPunctuationForm)
				return true;
			// Wordforms always need more (I suppose pathologically they might not if all analysis fields are turned off, but in that case,
			// nothing needs analysis).
			if (analysis is IWfiWordform)
				return false;
			var wf = analysis.Wordform;
			// analysis is either a WfiAnalysis or WfiGloss; find the actual analysis.
			var wa = (IWfiAnalysis)(analysis is IWfiAnalysis ? analysis : analysis.Owner);

			foreach (InterlinLineSpec spec in m_lineChoices.EnabledLineSpecs)
			{
				// see if the information required for this linespec is present.
				switch (spec.Flid)
				{
					case InterlinLineChoices.kflidWord:
						int ws = spec.GetActualWs(wf.Cache, wf.Hvo, TsStringUtils.GetWsOfRun(occ.Segment.Paragraph.Contents, 0));
						if (wf.Form.get_String(ws).Length == 0)
							return false; // bizarre, but for completeness...
						break;
					case InterlinLineChoices.kflidLexEntries:
						if (!CheckPropSetForAllMorphs(wa, WfiMorphBundleTags.kflidMorph))
							return false;
						break;
					case InterlinLineChoices.kflidMorphemes:
						if (!CheckPropSetForAllMorphs(wa, WfiMorphBundleTags.kflidMorph))
							return false;
						break;
					case InterlinLineChoices.kflidLexGloss:
						if (!CheckPropSetForAllMorphs(wa, WfiMorphBundleTags.kflidSense))
							return false;
						break;
					case InterlinLineChoices.kflidLexPos:
						if (!CheckPropSetForAllMorphs(wa, WfiMorphBundleTags.kflidMsa))
							return false;
						break;
					case InterlinLineChoices.kflidWordGloss:
						// If it isn't a WfiGloss the user needs a chance to supply a word gloss.
						if (!(analysis is IWfiGloss))
							return false;
						// If it is empty for the (possibly magic) ws specified here, it needs filling in.
						if (((IWfiGloss)analysis).Form.get_String(spec.GetActualWs(wf.Cache, analysis.Hvo, wf.Cache.DefaultAnalWs)).Length == 0)
							return false;
						break;
					case InterlinLineChoices.kflidWordPos:
						if (wa.CategoryRA == null)
							return false;
						break;
					case InterlinLineChoices.kflidFreeTrans:
					case InterlinLineChoices.kflidLitTrans:
					case InterlinLineChoices.kflidNote:
					default:
						// unrecognized or non-word-level annotation, nothing required.
						break;
				}
			}
			return true; // If we can't find anything to complain about, it's fully analyzed.
		}

		// Check that the specified WfiAnalysis includes at least one morpheme bundle, and that all morpheme
		// bundles have the specified property set. Return true if all is well.
		private static bool CheckPropSetForAllMorphs(IWfiAnalysis wa, int flid)
		{
			if (wa.MorphBundlesOS.Count == 0)
				return false;
			return wa.MorphBundlesOS.All(bundle => wa.Cache.DomainDataByFlid.get_ObjectProp(bundle.Hvo, flid) != 0);
		}

		/// <summary>
		/// Common pre-checks used for some of the Approve workflows.
		/// </summary>
		/// <returns>true: passed all pre-checks.</returns>
		public bool PreCheckApprove()
		{
			if (SelectedOccurrence == null)
				return false;

			if (!SelectedOccurrence.IsValid)
			{
				// Can happen (at least) when the text we're analyzing got deleted in another window
				SelectedOccurrence = null;
				InterlinDoc.TryHideFocusBoxAndUninstall();
				return false;
			}

			var stText = SelectedOccurrence.Paragraph.Owner as IStText;
			if (stText == null || stText.ParagraphsOS.Count == 0)
				return false; // paranoia, we should be in one of its paragraphs.

			return true;
		}

		/// <summary>
		/// Using the current focus box content, approve it and apply it to all unanalyzed matching
		/// wordforms in the text.  See LT-8833.
		/// </summary>
		/// <returns></returns>
		public void ApproveGuessOrChangesForWholeTextAndMoveNext(Command cmd)
		{
			if (!PreCheckApprove())
				return;

			// Go through the entire text looking for matching analyses that can be set to the new
			// value.

			// We don't need to discard existing guesses, even though we will modify Segment.Analyses,
			// since guesses for other wordforms will not be affected, and there will be no remaining
			// guesses for the word we're confirming everywhere. (This needs to be outside the block
			// for the UOW, since what we are suppressing happens at the completion of the UOW.)
			InterlinDoc.SuppressResettingGuesses(
				() =>
					{
						// Needs to include GetRealAnalysis, since it might create a new one.
						UndoableUnitOfWorkHelper.Do(cmd.UndoText, cmd.RedoText, Cache.ActionHandlerAccessor,
							() =>
							{
								ApproveAnalysis(SelectedOccurrence, true, true);
							});
					});
			// This should not make any data changes, since we're telling it not to save and anyway
			// we already saved the current annotation. And it can't correctly place the focus box
			// until the change we just did are completed and PropChanged sent. So keep this outside the UOW.
			OnNextBundle(false, false, false, true);
		}

		/// <summary>
		/// Common code intended to be used for all analysis approval workflows.
		/// </summary>
		/// <param name="occ">The occurrence to approve.</param>
		/// <param name="allOccurrences">if true, approve all occurrences; if false, only approve occ </param>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		public virtual void ApproveAnalysis(AnalysisOccurrence occ, bool allOccurrences, bool fSaveGuess)
		{
			IAnalysis oldAnalysis = occ.Analysis;
			IWfiWordform oldWf = occ.Analysis.Wordform;

			IWfiAnalysis obsoleteAna;
			AnalysisTree newAnalysisTree = InterlinWordControl.GetRealAnalysis(fSaveGuess, out obsoleteAna);
			var wf = newAnalysisTree.Wordform;
			if (newAnalysisTree.Analysis == wf)
			{
				// nothing significant to confirm, so move on
				return;
			}
			SaveAnalysisForAnnotation(occ, newAnalysisTree);
			if (wf != null)
			{
				if (allOccurrences)
				{
					ApplyAnalysisToInstancesOfWordform(occ, newAnalysisTree.Analysis, oldWf, wf);
				}
				else
				{
					occ.Segment.AnalysesRS[occ.Index] = newAnalysisTree.Analysis;
				}
			}
			// don't try to clean up the old analysis until we've finished walking through
			// the text and applied all our changes, otherwise we could delete a wordform
			// that is referenced by dummy annotations in the text, and thus cause the display
			// to treat them like pronunciations, and just show an unanalyzable text (LT-9953)
			FinishSettingAnalysis(newAnalysisTree, oldAnalysis);
			if (obsoleteAna != null)
				obsoleteAna.Delete();
		}

		// Caller must create UOW
		private void ApplyAnalysisToInstancesOfWordform(AnalysisOccurrence occurrence, IAnalysis newAnalysis, IWfiWordform oldWordform, IWfiWordform newWordform)
		{
			var navigator = new SegmentServices.StTextAnnotationNavigator(occurrence);
			foreach (var occ in navigator.GetAnalysisOccurrencesAdvancingInStText().ToList())
			{
				// We certainly want to update any occurrence that exactly matches the wordform of the analysis we are confirming.
				// If oldWordform is different, we are confirming a different case form from what occurred in the text.
				if (occ.Analysis == newWordform || occ.Analysis == oldWordform)
					occ.Segment.AnalysesRS[occ.Index] = newAnalysis;
			}
		}
	}

	/// <summary>
	/// This is a subclass of FocusBoxController. I (JohnT) am not sure why it was extracted, but it appears to be responsible
	/// for the menu commands that move the focus box. Or at least the part of the class in this file is.
	/// </summary>
	public partial class FocusBoxControllerForDisplay
	{
		// Set by the constructor, this determines whether 'move right' means 'move next' or 'move previous' and similar things.
		private bool m_fRightToLeft;

		public bool OnDisplayApproveAndStayPut(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		public bool OnApproveAndStayPut(object cmd)
		{
			ApproveAndStayPut(cmd as ICommandUndoRedoText);
			return true;
		}
		/// <summary>
		/// Enable the "Approve Analysis And" submenu, if we can.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayApproveAnalysisMovementMenu(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true;
		}

		public bool OnDisplayApproveAndMoveNextSameLine(object commandObject, ref UIItemDisplayProperties display)
		{
			return OnDisplayApproveAndMoveNext(commandObject, ref display);
		}

		public bool OnApproveAndMoveNextSameLine(object cmd)
		{
			OnNextBundle(true, true, true, true);
			return true;
		}

		public bool OnDisplayApproveForWholeTextAndMoveNext(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		public bool OnApproveForWholeTextAndMoveNext(object cmd)
		{
			ApproveGuessOrChangesForWholeTextAndMoveNext(cmd as Command);
			return false;
		}

		public bool OnDisplayApproveAll(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		public bool OnDisplayBrowseMoveNextSameLine(object commandObject, ref UIItemDisplayProperties display)
		{
			return OnDisplayBrowseMoveNext(commandObject, ref display);
		}

		public bool OnBrowseMoveNextSameLine(object cmd)
		{
			OnNextBundle(false, false, false, true);
			return true;
		}

		public bool OnDisplayBrowseMoveNext(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		public bool OnBrowseMoveNext(object cmd)
		{
			OnNextBundle(false, false, true, true);
			return true;
		}

		public bool OnDisplayApproveAndMoveNext(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		internal void OnApproveAndMoveNext()
		{
			OnApproveAndMoveNext(m_mediator.CommandSet["CmdApproveAndMoveNext"] as Command);
		}

		public bool OnApproveAndMoveNext(object cmd)
		{

			ApproveAndMoveNext(cmd as ICommandUndoRedoText);
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// LT-14588: this one was missing! Ctrl+Left doesn't work without it!
		/// </summary>
		public virtual bool OnDisplayMoveFocusBoxLeft(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		public virtual bool OnDisplayMoveFocusBoxRight(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		/// <summary>
		/// Enable the "Disregard Analysis And" submenu, if we can.
		/// </summary>
		public virtual bool OnDisplayBrowseMovementMenu(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true;
		}

		/// <summary>
		/// Move to the next word.
		/// </summary>
		public bool OnMoveFocusBoxRight(object cmd)
		{
			OnMoveFocusBoxRight(cmd as ICommandUndoRedoText, true);
			return true;
		}

		/// <summary>
		/// Move to next bundle to the right, after approving changes (and guesses if fSaveGuess is true).
		/// </summary>
		public void OnMoveFocusBoxRight(ICommandUndoRedoText undoRedoText, bool fSaveGuess)
		{
			// Move in the literal direction (LT-3706)
			OnNextBundle(fSaveGuess, false, true, !m_fRightToLeft);
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayMoveFocusBoxRightNc(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		/// <summary>
		/// Move to next bundle with no confirm
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnMoveFocusBoxRightNc(object cmd)
		{
			OnMoveFocusBoxRight(cmd as ICommandUndoRedoText, false);
			return true;
		}

		/// <summary>
		/// Move to the next word to the left (and confirm current).
		/// </summary>
		public bool OnMoveFocusBoxLeft(object cmd)
		{
			OnNextBundle(true, false, true, m_fRightToLeft);
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayMoveFocusBoxLeftNc(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}
		/// <summary>
		/// Move to the previous word (don't confirm current).
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnMoveFocusBoxLeftNc(object cmd)
		{
			OnNextBundle(false, false, true, m_fRightToLeft);
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayNextIncompleteBundle(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		/// <summary>
		/// The NextIncompleteBundle (without confirming current--Nc) command is not visible by default.
		/// Therefore this method, which is called by the mediator using reflection, must be implemented
		/// if the command is ever to be enabled and visible. It must have this exact name and signature,
		/// which are determined by the 'message' in its command element. This command is enabled and visible
		/// exactly when the version that DOES confirm the current choice is, so delegate to that.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayNextIncompleteBundleNc(object commandObject, ref UIItemDisplayProperties display)
		{
			return OnDisplayNextIncompleteBundle(commandObject, ref display);
		}

		/// <summary>
		/// Move to next bundle needing analysis (and confirm current).
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnNextIncompleteBundle(object cmd)
		{
			OnNextBundleSkipTranslationOrNoteLine(true);
			return true;
		}

		/// <summary>
		/// Move to next bundle needing analysis (without confirm current).
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnNextIncompleteBundleNc(object cmd)
		{
			OnNextBundleSkipTranslationOrNoteLine(false);
			return true;
		}

		/// <summary>
		/// whether or not to display the Make phrase icon and menu item.
		/// </summary>
		/// <remarks>OnJoinWords is in the base class because used by icon</remarks>
		public bool OnDisplayJoinWords(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles && ShowLinkWordsIcon;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		/// <summary>
		/// whether or not to display the Break phrase icon and menu item.
		/// </summary>
		/// <remarks>OnBreakPhrase is in the base class because used by icon</remarks>
		public bool OnDisplayBreakPhrase(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles && ShowBreakPhraseIcon;
			display.Visible = display.Enabled;
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayLastBundle(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}
		/// <summary>
		/// Move to the last bundle
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		public bool OnLastBundle(object arg)
		{
			var navigator = new SegmentServices.StTextAnnotationNavigator(SelectedOccurrence);
			IEnumerable<AnalysisOccurrence> options = navigator.GetWordformOccurrencesAdvancingIncludingStartingOccurrence();
			InterlinDoc.TriggerAnalysisSelected(options.Last(), true, true);
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayFirstBundle(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}
		/// <summary>
		/// Move to the first bundle
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		public bool OnFirstBundle(object arg)
		{
			var navigator = new SegmentServices.StTextAnnotationNavigator(SelectedOccurrence);
			IEnumerable<AnalysisOccurrence> options = navigator.GetWordformOccurrencesBackwardsIncludingStartingOccurrence();
			InterlinDoc.TriggerAnalysisSelected(options.Last(), true, true);
			return true;
		}
	}
}
