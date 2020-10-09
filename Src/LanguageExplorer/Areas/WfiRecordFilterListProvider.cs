// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas
{
	internal sealed class WfiRecordFilterListProvider : IRecordFilterListProvider
	{
		/// <summary />
		private LcmCache _cache;
		private readonly List<IRecordFilter> _filters;
		private IRecordFilterListProvider AsIRecordFilterListProvider => this;

		/// <summary />
		public WfiRecordFilterListProvider()
		{
			_filters = new List<IRecordFilter>();
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

			_cache = PropertyTable.GetValue<LcmCache>(FwUtilsConstants.cache);
			ReLoad();
		}

		#endregion

		#region Implementation of IRecordFilterListProvider
		/// <summary>
		/// Reload the data items
		/// </summary>
		public void ReLoad()
		{
			AsIRecordFilterListProvider.Filters.Clear();
			foreach (var words in _cache.LangProject.MorphologicalDataOA.TestSetsOC)
			{
				AsIRecordFilterListProvider.Filters.Add(new WordSetFilter(words));
			}
		}

		/// <inheritdoc />
		List<IRecordFilter> IRecordFilterListProvider.Filters => _filters;

		/// <inheritdoc />
		IRecordFilter IRecordFilterListProvider.GetFilter(string filterName)
		{
			return AsIRecordFilterListProvider.Filters.FirstOrDefault(filter => filter.Id == filterName);
		}

		/// <inheritdoc />
		bool IRecordFilterListProvider.AdjustFilterSelection(IRecordFilter argument)
		{
			if (AsIRecordFilterListProvider.Filters.Count == 0)
			{
				return false;   // we aren't providing any items.
			}
			var currentFilter = argument;
			if (ContainsOurFilter(currentFilter, out var matchingFilter))
			{
				// we found a match. if we don't already have a WordSetNullFilter, add it now.
				if (!(AsIRecordFilterListProvider.Filters[0] is WordSetNullFilter))
				{
					AsIRecordFilterListProvider.Filters.Insert(0, new WordSetNullFilter());
				}
				// make sure the current filter has a valid set of wordform references.
				var wordSetFilter = matchingFilter as WordSetFilter;
				wordSetFilter.ReloadWordSet(_cache);
			}
			else if (AsIRecordFilterListProvider.Filters[0] is WordSetNullFilter)
			{
				// the current filter doesn't have one of our filters, so remove the WordSetNullFilter.
				AsIRecordFilterListProvider.Filters.RemoveAt(0);
			}
			// allow others to handle this message.
			return false;
		}

		#endregion

		/// <summary />
		private bool ContainsOurFilter(IRecordFilter filter, out IRecordFilter matchingFilter)
		{
			matchingFilter = null;
			if (filter == null)
			{
				return false;
			}
			foreach (var currentFilter in AsIRecordFilterListProvider.Filters)
			{
				if (filter.Contains(currentFilter))
				{
					matchingFilter = currentFilter;
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