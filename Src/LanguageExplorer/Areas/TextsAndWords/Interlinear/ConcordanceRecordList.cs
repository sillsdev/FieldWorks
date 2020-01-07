// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This class provides the RecordList for three Text & Words tools. It exists to provide a pre-filtered
	/// list of Wordforms and to prevent jarring reloads of the Wordlist when the user is indirectly modifying the
	/// lists content. (i.e. typing in the baseline pane in the Text & Words\Word Concordance view)
	///
	/// The contents of this list are a result of parsing the texts and passing the results through a decorator.
	/// </summary>
	internal class ConcordanceRecordList : InterlinearTextsRecordList
	{
		//the ReloadList() on the RecordList class will trigger if this is true
		//set when the index in the list is changed
		private bool _selectionChanged = true;

		/// <summary />
		internal ConcordanceRecordList(StatusBar statusBar, ILangProject languageProject, ConcDecorator decorator)
			: base(TextAndWordsArea.ConcordanceWords, statusBar, decorator, false, new VectorPropertyParameterObject(languageProject, "Wordforms", ObjectListPublisher.OwningFlid), new RecordFilterParameterObject(new WordsUsedOnlyElsewhereFilter(languageProject.Cache)))
		{
			_filterProvider = new WfiRecordFilterListProvider();
		}

		public override void OnChangeFilter(FilterChangeEventArgs args)
		{
			RequestRefresh();
			base.OnChangeFilter(args);
		}

		/// <summary>
		/// Provide a means by which the record list can indicate that we should really refresh our contents.
		/// (used for handling events that the record list processes, like the refresh.
		/// </summary>
		public void RequestRefresh()
		{
			//indicate that a refresh is desired so ReloadList would be triggered by an index change
			ReloadRequested = true;
			//indicate that the selection has changed, ReloadList will now actually reload the list
			_selectionChanged = true;
		}

		/// <summary>
		/// Returns the value that indicates if a reload has been requested (and ignored) by the list
		/// If you want to force a re-load call RequestRefresh
		/// </summary>
		public bool ReloadRequested { get; private set; }

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
				_selectionChanged = true;
				base.CurrentIndex = value;
				// if no one has actually asked for the list to be reloaded it would be a waste to do so
				if (ReloadRequested)
				{
					ReloadList();
				}
			}
		}

		/// <summary>
		/// This method should cause all paragraphs in interesting texts which do not have the ParseIsCurrent flag set
		/// to be Parsed.
		/// </summary>
		internal void ParseInterestingTextsIfNeeded()
		{
			ForceReloadList();
		}

		/// <summary>
		/// This is used in situations like switching views. In such cases we should force a reload.
		/// </summary>
		protected override bool NeedToReloadList()
		{
			return base.NeedToReloadList() || ReloadRequested;
		}

		protected override void ChangeSorter(RecordSorter sorter)
		{
			RequestRefresh();
			base.ChangeSorter(sorter);
		}

		protected override void ForceReloadList()
		{
			RequestRefresh();
			base.ForceReloadList();
		}

		/// <summary>
		/// overridden to prevent reloading the list unless it is specifically desired.
		/// </summary>
		protected override void ReloadList()
		{
			if (_selectionChanged || CurrentIndex == -1)
			{
				ReloadRequested = _selectionChanged = false; // BEFORE base call, which could set CurrentIndex and cause stack overflow otherwise
				base.ReloadList();
			}
			else
			{
				ReloadRequested = true;
			}
		}

		// If the source (unfiltered, unsorted) list of objects is being maintained in a private list in the decorator, update it.
		// If this cannot be done at once and the Reload needs to be completed later, return true.
		protected override bool UpdatePrivateList()
		{
			if (m_flid != ObjectListPublisher.OwningFlid)
			{
				return false; // we are not involved in the reload process.
			}
			if (((IActionHandlerExtensions)m_cache.ActionHandlerAccessor).CanStartUow)
			{
				ParseAndUpdate(); // do it now
			}
			else // do it as soon as possible. (we might be processing PropChanged.)
			{
				// REVIEW (FWR-1906): Do we need to do this reload only the first time, or also (as here) when the prop change is from undo or redo?
				// Enhance JohnT: is there some way we can be sure only one of these tasks gets added?
				((IActionHandlerExtensions)m_cache.ActionHandlerAccessor).DoAtEndOfPropChangedAlways(RecordList_PropChangedCompleted);
				return true;
			}
			return false;
		}

		private void ParseAndUpdate()
		{
			var publisher = (ObjectListPublisher)VirtualListPublisher;
			publisher.SetOwningPropInfo(WfiWordformTags.kClassId, "WordformInventory", "Wordforms");
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, ParseInterestingTexts);
			publisher.SetOwningPropValue((m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances().Select(wf => wf.Hvo)).ToArray());
		}

		/// <summary>
		/// Parse (if necessary...ParseIsCurrent will be checked to see) the texts we want in the concordance.
		/// </summary>
		private void ParseInterestingTexts()
		{
			// Also it should be forced to be empty if FwUtils.IsOkToDisplayScriptureIfPresent returns false.
			var scriptureTexts = m_cache.LangProject.TranslatedScriptureOA?.StTexts.Where(aText => IsInterestingScripture(aText)) ?? new IStText[0];
			// Enhance JohnT: might eventually want to be more selective here, perhaps a genre filter.
			var vernacularTexts = from st in m_cache.LangProject.Texts select st.ContentsOA;
			// Filtered list that excludes IScrBookAnnotations.
			var texts = vernacularTexts.Concat(scriptureTexts).Where(x => x != null).ToList();
			var count = texts.SelectMany(text => text.ParagraphsOS).Count();
			var done = 0;
			using (var progress = PropertyTable.GetValue<IFwMainWnd>(FwUtils.window).CreateSimpleProgressState())
			{
				progress.SetMilestone(ITextStrings.ksParsing);
				foreach (var para in texts.SelectMany(text => text.ParagraphsOS.Cast<IStTxtPara>()))
				{
					done++;
					var newPercentDone = done * 100 / count;
					if (newPercentDone != progress.PercentDone)
					{
						progress.PercentDone = newPercentDone;
						progress.Breath();
					}
					if (para.ParseIsCurrent)
					{
						continue;
					}
					ParagraphParser.ParseParagraph(para);
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
				{
					return concDecorator.IsInterestingText(text);
				}
			}
			return true; // if by any chance this is used without a conc decorator, assume all Scripture is interesting.
		}

		private void RecordList_PropChangedCompleted()
		{
			ReloadList();
		}
	}
}