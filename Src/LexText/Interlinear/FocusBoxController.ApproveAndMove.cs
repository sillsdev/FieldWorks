// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: InterlinDocForAnalysis.cs
// Responsibility: pyle
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;
using SIL.FieldWorks.Common.FwUtils;
using SIL.CoreImpl;

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
			UpdateRealFromSandbox(undoRedoText, true, SelectedOccurrence);
		}

		internal virtual void ApproveAndMoveNext(ICommandUndoRedoText undoRedoText)
		{
			var navigator = new SegmentServices.StTextAnnotationNavigator(SelectedOccurrence);
			var nextWordform = navigator.GetNextWordformOrDefault(SelectedOccurrence);
			UpdateRealFromSandbox(undoRedoText, true, nextWordform);
			// do the move.
			InterlinDoc.SelectOccurrence(nextWordform);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="undoRedoText">Approving the state of the FocusBox can be associated with
		/// different user actions (ie. UOW)</param>
		/// <param name="fSaveGuess"></param>
		/// <param name="nextWordform"></param>
		internal void UpdateRealFromSandbox(ICommandUndoRedoText undoRedoText, bool fSaveGuess,
			AnalysisOccurrence nextWordform)
		{
			if (!ShouldCreateAnalysisFromSandbox(fSaveGuess))
				return;

			var origWordform = SelectedOccurrence;
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
					Cache.ActionHandlerAccessor, () => ApproveAnalysisAndMove(fSaveGuess, nextWordform));

			}
			finally
			{
				InterlinDoc.SuspendResettingAnalysisCache = false;
			}
			var newAnalysis = SelectedOccurrence.Analysis;
			InterlinDoc.UpdatingOccurrence(oldAnalysis, newAnalysis);
			var newWag = new AnalysisTree(origWordform.Analysis);
			var wordforms = new HashSet<IWfiWordform>();
			wordforms.Add(origWag.Wordform);
			wordforms.Add(newWag.Wordform);
			InterlinDoc.UpdateGuesses(InterlinDocRootSiteBase.HasMatchingWordformsNeedingAnalysis, wordforms);
		}

		protected virtual bool ShouldCreateAnalysisFromSandbox(bool fSaveGuess)
		{
			if (SelectedOccurrence == null)
				return false;
			if (InterlinWordControl == null || !InterlinWordControl.ShouldSave(fSaveGuess))
				return false;
			return true;
		}


		protected virtual void ApproveAnalysisAndMove(bool fSaveGuess, AnalysisOccurrence nextWordform)
		{
			using (new UndoRedoApproveAndMoveHelper(this, SelectedOccurrence, nextWordform))
				ApproveAnalysis(fSaveGuess);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fSaveGuess"></param>
		protected virtual void ApproveAnalysis(bool fSaveGuess)
		{
			IWfiAnalysis obsoleteAna;
			AnalysisTree newAnalysisTree = InterlinWordControl.GetRealAnalysis(fSaveGuess, out obsoleteAna);
			// if we've made it this far, might as well try to go the whole way through the UOW.
			SaveAnalysisForAnnotation(SelectedOccurrence, newAnalysisTree);
			FinishSettingAnalysis(newAnalysisTree, InitialAnalysis);
			if (obsoleteAna != null)
				obsoleteAna.Delete();
		}

		private void FinishSettingAnalysis(AnalysisTree newAnalysisTree, AnalysisTree oldAnalysisTree)
		{
			if (newAnalysisTree.Analysis == oldAnalysisTree.Analysis)
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

			// In case the wordform we point at has a form that doesn't match, we may need to set up an overidden form for the annotation.
			IWfiWordform targetWordform = newAnalysisTree.Wordform;
			if (targetWordform != null)
			{
				TryCacheRealWordForm(occurrence);
			}

			// It's possible if the new analysis is a different case form that the old wordform is now
			// unattested and should be removed.
			if (wfToTryDeleting != null && wfToTryDeleting != occurrence.Analysis.Wordform)
				wfToTryDeleting.DeleteIfSpurious();
		}

		private static bool BaselineFormDiffersFromAnalysisWord(AnalysisOccurrence occurrence, out ITsString baselineForm)
		{
			baselineForm = occurrence.BaselineText; // Review JohnT: does this work if the text might have changed??
			var wsBaselineForm = StringUtils.GetWsAtOffset(baselineForm, 0);
			// We've updated the annotation to have InstanceOf set to the NEW analysis, so what we now derive from
			// that is the NEW wordform.
			var wfNew = occurrence.Analysis as IWfiWordform;
			if (wfNew == null)
				return false; // punctuation variations not significant.
			var tssWfNew = wfNew.Form.get_String(wsBaselineForm);
			return !baselineForm.Equals(tssWfNew);
		}

		private void TryCacheRealWordForm(AnalysisOccurrence occurrence)
		{
			ITsString tssBaselineCbaForm;
			if (BaselineFormDiffersFromAnalysisWord(occurrence, out tssBaselineCbaForm))
			{
				//m_fdoCache.VwCacheDaAccessor.CacheStringProp(hvoAnnotation,
				//									 InterlinVc.TwficRealFormTag(m_fdoCache),
				//									 tssBaselineCbaForm);
			}
		}

		// <summary>
		// If hvoOldAnalysis is 'unattested', get rid of it.
		// It is 'unattested' if
		//		- No CmBaseAnnotation of type 'Wordform in context' has it as its InstanceOf
		//		- It is not the target of a CmAgentEvaluation owned by the parser with accepted = true.
		//		- It's owner is not parser-approved (this is relevant for WfiGlosses)
		//		- It doesn't own any WfiGlosses (relevant for WfiAnalyses)
		//		- It doesn't own any WfiAnalyses (relevant for WfiWordform)
		// </summary>
		// <param name="hvoOldAnalyis"></param>
		//void DeleteIfUnattested(int hvoOldAnalysis)
		//{
		//	// (LT-7457) Make sure this data wasn't deleted in another screen. If it
		//	// was, trying to delete it here will crash.
		//	if (m_fdoCache.IsValidObject(hvoOldAnalysis))
		//	{
		//		int hvoParserAgent = m_fdoCache.LangProject.DefaultParserAgent.Hvo;
		//		int hvoAnnDefnTwfic = CmAnnotationDefn.Twfic(m_fdoCache).Hvo;
		//		int hvoOwner = m_fdoCache.GetOwnerOfObject(hvoOldAnalysis);
		//		// This query counts the number of times any of the things that make it 'attested' occur.
		//		string sql = string.Format("select " +
		//		   "	(select count(*) from CmBaseAnnotation_ cba where cba.AnnotationType={0} and cba.InstanceOf={1})" +
		//		   " + (select count(*) from CmAgentEvaluation_ where owner$={2} and target={1} and Accepted=1)" +
		//		   " + (select count(*) from CmAgentEvaluation_ where owner$={2} and target={3} and Accepted=1)" +
		//		   " + (select count(*) from WfiGloss_ where owner$={1})" +
		//		   " + (select count(*) from WfiAnalysis_ where owner$={1})",
		//		   hvoAnnDefnTwfic, hvoOldAnalysis, hvoParserAgent, hvoOwner);
		//		int cAttestation;
		//		DbOps.ReadOneIntFromCommand(m_fdoCache, sql, null, out cAttestation);
		//		if (cAttestation != 0)
		//			return; // attested!
		//		ICmObject obj = CmObject.CreateFromDBObject(m_fdoCache, hvoOldAnalysis, false);
		//		bool fShouldTryOwner = obj is WfiAnalysis || obj is WfiGloss;
		//		obj.DeleteUnderlyingObject();
		//		// If we deleted this object, we may have made its owner unattested, unless the thing
		//		// we're deleting is s wordform...the lexical database is always attested!
		//		if (fShouldTryOwner)
		//			DeleteIfUnattested(hvoOwner);
		//	}
		//}

		internal class UndoRedoApproveAndMoveHelper : FwDisposableBase
		{
			internal UndoRedoApproveAndMoveHelper(FocusBoxController focusBox,
				AnalysisOccurrence occBeforeApproveAndMove, AnalysisOccurrence occAfterApproveAndMove)
			{
				Cache = focusBox.Cache;
				FocusBox = focusBox;
				OccurrenceBeforeApproveAndMove = occBeforeApproveAndMove;
				OccurrenceAfterApproveAndMove = occAfterApproveAndMove;

				// add the undo action
				AddUndoRedoAction(OccurrenceBeforeApproveAndMove, null);
			}

			FdoCache Cache { get; set; }
			FocusBoxController FocusBox { get; set; }
			AnalysisOccurrence OccurrenceBeforeApproveAndMove { get; set; }
			AnalysisOccurrence OccurrenceAfterApproveAndMove { get; set; }


			private UndoRedoApproveAnalysis AddUndoRedoAction(AnalysisOccurrence currentAnnotation, AnalysisOccurrence newAnnotation)
			{
				if (Cache.ActionHandlerAccessor != null && currentAnnotation != newAnnotation)
				{
					var undoRedoAction = new UndoRedoApproveAnalysis(FocusBox.InterlinDoc,
						currentAnnotation, newAnnotation);
					Cache.ActionHandlerAccessor.AddAction(undoRedoAction);
					return undoRedoAction;
				}
				return null;
			}

			protected override void DisposeManagedResources()
			{
				// add the redo action
				if (OccurrenceBeforeApproveAndMove != OccurrenceAfterApproveAndMove)
					AddUndoRedoAction(null, OccurrenceAfterApproveAndMove);
			}

			protected override void DisposeUnmanagedResources()
			{
				FocusBox = null;
				OccurrenceBeforeApproveAndMove = null;
				OccurrenceAfterApproveAndMove = null;
			}




		}
		/// <summary>
		/// This class allows smarter UndoRedo for ApproveAnalysis, so that the FocusBox can move appropriately.
		/// </summary>
		internal class UndoRedoApproveAnalysis : UndoActionBase
		{
			readonly FdoCache m_cache;
			readonly InterlinDocForAnalysis m_interlinDoc;
			readonly AnalysisOccurrence m_oldOccurrence;
			AnalysisOccurrence m_newOccurrence;

			internal UndoRedoApproveAnalysis(InterlinDocForAnalysis interlinDoc, AnalysisOccurrence oldAnnotation,
				AnalysisOccurrence newAnnotation)
			{
				m_interlinDoc = interlinDoc;
				m_cache = m_interlinDoc.Cache;
				m_oldOccurrence = oldAnnotation;
				m_newOccurrence = newAnnotation;
			}

			#region Overrides of UndoActionBase

			private bool IsUndoable()
			{
				return m_oldOccurrence != null && m_oldOccurrence.IsValid;
			}

			public override bool Redo()
			{
				if (m_newOccurrence != null && m_newOccurrence.IsValid)
				{
					m_interlinDoc.SelectOccurrence(m_newOccurrence);
				}
				else
				{
					m_interlinDoc.TryHideFocusBoxAndUninstall();
				}

				return true;
			}

			public override bool Undo()
			{
				if (IsUndoable())
				{
					m_interlinDoc.SelectOccurrence(m_oldOccurrence);
				}
				else
				{
					m_interlinDoc.TryHideFocusBoxAndUninstall();
				}

				return true;
			}

			#endregion
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
		/// Move to the next bundle in the direction indicated by fForward. If fSaveGuess is true, save guesses in the current position,
		/// using Undo  text from the command. If skipFullyAnalyzedWords is true, move to the next item needing analysis, otherwise, the immediate next.
		/// If fMakeDefaultSelection is true, make the default selection within the moved focus box.
		/// </summary>
		public void OnNextBundle(ICommandUndoRedoText undoRedoText, bool fSaveGuess, bool skipFullyAnalyzedWords,
			bool fMakeDefaultSelection, bool fForward)
		{
			int currentLineIndex = -1;
			if (InterlinWordControl!= null)
				currentLineIndex = InterlinWordControl.GetLineOfCurrentSelection();
			var nextOccurrence = GetNextOccurrenceToAnalyze(fForward, skipFullyAnalyzedWords);
			InterlinDoc.TriggerAnalysisSelected(nextOccurrence, fSaveGuess, fMakeDefaultSelection);
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

			foreach (InterlinLineSpec spec in m_lineChoices)
			{
				// see if the information required for this linespec is present.
				switch (spec.Flid)
				{
					case InterlinLineChoices.kflidWord:
						int ws = spec.GetActualWs(wf.Cache, wf.Hvo, StringUtils.GetWsOfRun(occ.Segment.Paragraph.Contents, 0));
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
		/// Using the current focus box content, approve it and apply it to all unanalyzed matching
		/// wordforms in the text.  See LT-8833.
		/// </summary>
		/// <returns></returns>
		public void ApproveGuessOrChangesForWholeTextAndMoveNext(Command cmd)
		{
			// Go through the entire text looking for matching analyses that can be set to the new
			// value.
			var oldWf = SelectedOccurrence.Analysis.Wordform;
			if (SelectedOccurrence == null)
				return;
			var stText = SelectedOccurrence.Paragraph.Owner as IStText;
			if (stText == null || stText.ParagraphsOS.Count == 0)
				return; // paranoia, we should be in one of its paragraphs.
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
									IWfiAnalysis obsoleteAna;
									AnalysisTree newAnalysisTree = InterlinWordControl.GetRealAnalysis(true, out obsoleteAna);
									var wf = newAnalysisTree.Wordform;
									if (newAnalysisTree.Analysis == wf)
									{
										// nothing significant to confirm, so move on
										// (return means get out of this lambda expression, not out of the method).
										return;
									}
									SaveAnalysisForAnnotation(SelectedOccurrence, newAnalysisTree);
									// determine if we confirmed on a sentence initial wordform to its lowercased form
									bool fIsSentenceInitialCaseChange = oldWf != wf;
									if (wf != null)
									{
										ApplyAnalysisToInstancesOfWordform(newAnalysisTree.Analysis, oldWf, wf);
									}
									// don't try to clean up the old analysis until we've finished walking through
									// the text and applied all our changes, otherwise we could delete a wordform
									// that is referenced by dummy annotations in the text, and thus cause the display
									// to treat them like pronunciations, and just show an unanalyzable text (LT-9953)
									FinishSettingAnalysis(newAnalysisTree, InitialAnalysis);
									if (obsoleteAna != null)
										obsoleteAna.Delete();
								});
					});
			// This should not make any data changes, since we're telling it not to save and anyway
			// we already saved the current annotation. And it can't correctly place the focus box
			// until the change we just did are completed and PropChanged sent. So keep this outside the UOW.
			OnNextBundle(cmd, false, false, false, true);
		}

		// Caller must create UOW
		private void ApplyAnalysisToInstancesOfWordform(IAnalysis newAnalysis, IWfiWordform oldWordform, IWfiWordform newWordform)
		{
			var navigator = new SegmentServices.StTextAnnotationNavigator(SelectedOccurrence);
			foreach (var occ in navigator.GetAnalysisOccurrencesAdvancingInStText().ToList())
			{
				// We certainly want to update any occurrence that exactly matches the wordform of the analysis we are confirming.
				// If oldWordform is different, we are confirming a different case form from what occurred in the text,
				// and we only confirm these if SelectedOccurrence and occ are both sentence-initial.
				// We want to do that only for sentence-initial occurrences.
				if (occ.Analysis == newWordform || (occ.Analysis == oldWordform && occ.Index == 0 && SelectedOccurrence.Index == 0))
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
			OnNextBundle(cmd as Command, true, false, false, true);
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

		public bool OnDisplayBrowseMoveNextSameLine(object commandObject, ref UIItemDisplayProperties display)
		{
			return OnDisplayBrowseMoveNext(commandObject, ref display);
		}

		public bool OnBrowseMoveNextSameLine(object cmd)
		{
			OnNextBundle(cmd as Command, false, false, false, true);
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
			OnNextBundle(cmd as Command, false, false, true, true);
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
			OnNextBundle(undoRedoText, fSaveGuess, false, true, !m_fRightToLeft);
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
			OnNextBundle(cmd as ICommandUndoRedoText, true, false, true, m_fRightToLeft);
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
			OnNextBundle(cmd as ICommandUndoRedoText, false, false, true, m_fRightToLeft);
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
		/// Move to next bundle needing analysis (and confirm current).
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnNextIncompleteBundle(object cmd)
		{
			OnNextBundle(cmd as ICommandUndoRedoText, true, true, true, true);
			return true;
		}

		/// <summary>
		/// Move to next bundle needing analysis (and confirm current).
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnNextIncompleteBundleNc(object cmd)
		{
			OnNextBundle(cmd as ICommandUndoRedoText, false, true, true, true);
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
