// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas
{
	public class WfiRecordFilterListProvider : IRecordFilterListProvider
	{
		/// <summary />
		protected LcmCache _cache;

		/// <summary />
		public WfiRecordFilterListProvider()
		{
			Filters = new ArrayList();
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <inheritdoc />
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <inheritdoc />
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <inheritdoc />
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			_cache = PropertyTable.GetValue<LcmCache>(FwUtils.cache);
			ReLoad();
		}

		#endregion

		/// <summary>
		/// Reload the data items
		/// </summary>
		public void ReLoad()
		{
			Filters.Clear();
			foreach (var words in _cache.LangProject.MorphologicalDataOA.TestSetsOC)
			{
				Filters.Add(new WordSetFilter(words));
			}
		}

		/// <inheritdoc />
		public ArrayList Filters { get; }

		/// <inheritdoc />
		public RecordFilter GetFilter(string filterName)
		{
			foreach (RecordFilter filter in Filters)
			{
				if (filter.id == filterName)
				{
					return filter;
				}
			}
			return null;
		}

		/// <inheritdoc />
		public bool OnAdjustFilterSelection(RecordFilter argument)
		{
			if (Filters.Count == 0)
			{
				return false;   // we aren't providing any items.
			}
			var currentFilter = argument;
			RecordFilter matchingFilter;
			if (ContainsOurFilter(currentFilter, out matchingFilter))
			{
				// we found a match. if we don't already have a WordSetNullFilter, add it now.
				if (!(Filters[0] is WordSetNullFilter))
				{
					Filters.Insert(0, new WordSetNullFilter());
				}
				// make sure the current filter has a valid set of wordform references.
				var wordSetFilter = matchingFilter as WordSetFilter;
				wordSetFilter.ReloadWordSet(_cache);
			}
			else if (Filters[0] is WordSetNullFilter)
			{
				// the current filter doesn't have one of our filters, so remove the WordSetNullFilter.
				Filters.RemoveAt(0);
			}
			// allow others to handle this message.
			return false;
		}

		/// <summary />
		private bool ContainsOurFilter(RecordFilter filter, out RecordFilter matchingFilter)
		{
			matchingFilter = null;
			if (filter == null)
			{
				return false;
			}
			for (var i = 0; i < Filters.Count; ++i)
			{
				if (filter.Contains(Filters[i] as RecordFilter))
				{
					matchingFilter = (RecordFilter)Filters[i];
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Used for disabling WordSet filters
		/// </summary>
		private sealed class WordSetNullFilter : NullFilter
		{
			public WordSetNullFilter()
			{
				Name = "Clear Wordform-Set Filter";
			}
		}
	}
}