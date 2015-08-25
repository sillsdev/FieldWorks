using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.XWorks;

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
			else // do it as soon as possible. (we might be processing PropChanged.)
			{
				// REVIEW (FWR-1906): Do we need to do this reload only the first time, or also (as here) when the prop change is from undo or redo?
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
#if RANDYTODO
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
#endif
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

		void RecordList_PropChangedCompleted()
		{
			ReloadList();
		}
	}
}
