// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.XWorks;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This class provides the RecordList for the InterlinearTextsRecordsClerk. It exists to provide a pre-filtered
	/// list of Wordforms and to prevent jarring reloads of the Wordlist when the user is indirectly modifying the
	/// lists content. (i.e. typing in the baseline pane in the Text & Words\Word Concordance view)
	///
	/// The contents of this list are a result of parsing the texts and passing the results through a decorator.
	///
	/// </summary>
	class ConcordanceWordList : RecordList
	{
		//the ReloadList() on the RecordList class will trigger if this is true
		//set when the index in the list is changed
		private bool selectionChanged = true;
		//This indicates that a reload has been requested,
		private bool reloadRequested;

		public override void OnChangeFilter(FilterChangeEventArgs args)
		{
			RequestRefresh();
			base.OnChangeFilter(args);
		}

		/// <summary>
		/// provide a means by which the clerk can indicate that we should really refresh our contents.
		/// (used for handling events that the clerk processes, like the refresh.
		/// </summary>
		public void RequestRefresh()
		{
			//indicate that a refresh is desired so ReloadList would be triggered by an index change
			reloadRequested = true;
			//indicate that the selection has changed, ReloadList will now actually reload the list
			selectionChanged = true;
		}

		/// <summary>
		/// Returns the value that indicates if a reload has been requested (and ignored) by the list
		/// If you want to force a re-load call RequestRefresh
		/// </summary>
		public bool ReloadRequested { get { return reloadRequested; } }

		/// <summary>
		/// We want to reload the list on an index change if a reload has been
		/// requested (due to an add or remove)
		/// </summary>
		public override int CurrentIndex
		{
			get
			{
				return base.CurrentIndex;
			}
			set
			{
				selectionChanged = true;
				base.CurrentIndex = value;
				//if noone has actually asked for the list to be reloaded it would be a waste to do so
				if (reloadRequested)
					ReloadList();
			}
		}

		/// <summary>
		/// This is used in situations like switching views. In such cases we should force a reload.
		/// </summary>
		/// <returns></returns>
		protected override bool NeedToReloadList()
		{
			return base.NeedToReloadList() || reloadRequested;
		}

		public override void ChangeSorter(RecordSorter sorter)
		{
			RequestRefresh();
			base.ChangeSorter(sorter);
		}

		public override void ForceReloadList()
		{
			RequestRefresh();
			base.ForceReloadList();
		}

		/// <summary>
		/// overridden to prevent reloading the list unless it is specifically desired.
		/// </summary>
		public override void ReloadList()
		{
			if (selectionChanged || CurrentIndex == -1)
			{
				reloadRequested = selectionChanged = false; // BEFORE base call, which could set CurrentIndex and cause stack overflow otherwise
				base.ReloadList();
			}
			else
			{
				reloadRequested = true;
			}
		}

		// If the source (unfiltered, unsorted) list of objects is being maintained in a private list in the decorator, update it.
		// If this cannot be done at once and the Reload needs to be completed later, return true.
		protected override bool UpdatePrivateList()
		{
			if (m_flid != ObjectListPublisher.OwningFlid)
				return false; // we are not involved in the reload process.

			if (((IActionHandlerExtensions)Cache.ActionHandlerAccessor).CanStartUow)
				ParseAndUpdate(); // do it now
			else if (Cache.ActionHandlerAccessor.IsUndoOrRedoInProgress)
			{
				// we're doing an undo or redo action; because of the code below, the UnitOfWork
				// already knows we need to reload the list after we get done with PropChanged.
				// But for the same reasons as in the original action, we aren't ready to do it now.
				return true;
			} else // do it as soon as possible. (We might be processing PropChanged)
			{
				// Enhance JohnT: is there some way we can be sure only one of these tasks gets added?
				((IActionHandlerExtensions)Cache.ActionHandlerAccessor).DoAtEndOfPropChangedAlways(RecordList_PropChangedCompleted);
				return true;
			}
			return false;
		}

		private void ParseAndUpdate()
		{
			var publisher = (VirtualListPublisher as ObjectListPublisher);
			publisher.SetOwningPropInfo(WfiWordformTags.kClassId, "WordformInventory", "Wordforms");
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, ParseInterestingTexts);
			publisher.SetOwningPropValue(
				(from wf in Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances() select wf.Hvo).ToArray());
		}

		/// <summary>
		/// Parse (if necessary...ParseIsCurrent will be checked to see) the texts we want in the concordance.
		/// </summary>
		private void ParseInterestingTexts()
		{
			// Also it should be forced to be empty if FwUtils.IsOkToDisplayScriptureIfPresent returns false.
			IEnumerable<IStText> scriptureTexts = Cache.LangProject.TranslatedScriptureOA == null ? new IStText[0] :
				from aText in Cache.LangProject.TranslatedScriptureOA.StTexts
				where IsInterestingScripture(aText)
				select aText;
			// Enhance JohnT: might eventually want to be more selective here, perhaps a genre filter.
			IEnumerable<IStText> vernacularTexts = from st in Cache.LangProject.Texts select st.ContentsOA;
			// Filtered list that excludes IScrBookAnnotations.
			var texts = vernacularTexts.Concat(scriptureTexts).Where(x => x != null).ToList();
			int count = (from text in texts from para in text.ParagraphsOS select para).Count();
			int done = 0;
			using (var progress = FwXWindow.CreateSimpleProgressState(m_propertyTable))
			{
				progress.SetMilestone(ITextStrings.ksParsing);
				foreach (var text in texts)
				{
					foreach (IStTxtPara para in text.ParagraphsOS)
					{
						done++;
						int newPercentDone = done * 100 / count;
						if (newPercentDone != progress.PercentDone)
						{
							progress.PercentDone = newPercentDone;
							progress.Breath();
						}
						if (para.ParseIsCurrent) continue;

						ParagraphParser.ParseParagraph(para);
					}
				}
			}
		}

		private bool IsInterestingScripture(IStText text)
		{
			// Typically this question only arises where we have a ConcDecorator involved.
			if (VirtualListPublisher is DomainDataByFlidDecoratorBase)
			{
				var concDecorator = ((DomainDataByFlidDecoratorBase)VirtualListPublisher).BaseSda as ConcDecorator;
				if (concDecorator != null)
					return concDecorator.IsInterestingText(text);
			}
			return true; // if by any chance this is used without a conc decorator, assume all Scripture is interesting.
		}

		// This is invoked when there have been significant changes but we can't reload immediately
		// because we need to finish handling PropChanged first. We want to really reload the list,
		// not just record that it might be a nice idea sometime.
		// Enhance: previously we were calling ReloadList. That could leave the list in an invalid
		// state and the UI displaying deleted objects and lead to crashes (LT-18976), since
		// there was no guarantee of ever doing a real reload. (For example, closing the change
		// spelling dialog forces a real reload by calling MasterRefresh; but undoing that change
		// ended up doing no reload at all, even though this method was called.)
		// However, it's possible that using ForceReloadList here
		// will cause more reloads than are needed, slowing things down. If so, one thing to
		// investigate would be making sure that a request to call this is only added once per UnitOfWork.
		// (See the one use of this function.)
		// It's also possible that the MasterRefresh on closing the change spelling dialog
		// can be removed now we're doing a real reload here.
		// I'm not risking either of those changes at a time when we're trying to stabilize for release.
		void RecordList_PropChangedCompleted()
		{
			ForceReloadList();
		}
	}
}
