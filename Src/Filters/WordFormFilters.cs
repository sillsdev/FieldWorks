// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Linq;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.Xml;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// A filter for selecting a group of wordform spaced on a "IWfiWordSet"
	/// </summary>
	public class WordSetFilter : RecordFilter
	{
		/// <summary />
		protected int[] m_hvos;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:WordSetFilter"/> class.
		/// </summary>
		public WordSetFilter(IWfiWordSet wordSet)
		{
			id = wordSet.Hvo.ToString();
			Name = wordSet.Name.AnalysisDefaultWritingSystem.Text;
			LoadCases(wordSet);
		}

		private void LoadCases(IWfiWordSet wordSet)
		{
			m_hvos = wordSet.CasesRC.ToHvoArray();
		}

		/// <summary>
		/// Sync the word references to the state of the word list in the database.
		/// This is what we need to do when restoring our Filter from xml to make sure
		/// the ids are valid.
		/// </summary>
		internal void ReloadWordSet(LcmCache cache)
		{
			var hvo = int.Parse(id);
			var wordSet = cache.ServiceLocator.GetObject(hvo) as IWfiWordSet;
			LoadCases(wordSet);
		}

		/// <summary>
		/// Default constructor for IPersistAsXml
		/// </summary>
		public WordSetFilter()
		{
		}

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml (node);
			XmlUtils.SetAttribute(node, "id", id);
			XmlUtils.SetAttribute(node, "wordlist", XmlUtils.MakeStringFromList(m_hvos.ToList()));
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml (node);
			id = XmlUtils.GetMandatoryAttributeValue(node, "id");
			m_hvos = XmlUtils.GetMandatoryIntegerListAttributeValue(node, "wordlist");
		}

		/// <summary>
		/// Test to see if this filter matches the other filter.
		/// </summary>
		public override bool SameFilter(RecordFilter other)
		{
			return other is WordSetFilter && other.id == id && other.Name == Name;
		}

		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		public override bool Accept (IManyOnePathSortItem item)
		{
			var hvo = item.KeyObject;

			for (var i = m_hvos.Length-1; i>=0; i--)
			{
				if (m_hvos[i] == hvo)
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// This is always set by the user.
		/// </summary>
		public override bool IsUserVisible => true;
	}

	/// <summary>
	/// Used for disabling WordSet filters
	/// </summary>
	public class WordSetNullFilter : NullFilter
	{
		public WordSetNullFilter()
		{
			Name="Clear Wordform-Set Filter";
		}
	}

	public  class WfiRecordFilterListProvider : RecordFilterListProvider
	{
		/// <summary />
		protected LcmCache m_cache;
		/// <summary>Collection of WordSetFilter objects.</summary>
		protected ArrayList m_filters;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:WfiRecordFilterListProvider"/> class.
		/// </summary>
		public WfiRecordFilterListProvider()
		{
			m_filters = new ArrayList();
		}

		/// <summary>
		/// Initialize the filter list. this is called because we are an IFlexComponent
		/// </summary>
		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			m_cache = PropertyTable.GetValue<LcmCache>("cache");
			ReLoad();
		}

		/// <summary>
		/// reload the data items
		/// </summary>
		public override void ReLoad()
		{
			m_filters.Clear();

			foreach(var words in m_cache.LangProject.MorphologicalDataOA.TestSetsOC)
			{
				m_filters.Add(new WordSetFilter(words));
			}
		}

		/// <summary>
		/// the list of filters.
		/// </summary>
		public override ArrayList Filters => m_filters;

		/// <summary>
		/// Gets the filter.
		/// </summary>
		public override object GetFilter(string id)
		{
			foreach (RecordFilter filter in m_filters)
			{
				if (filter.id == id)
				{
					return filter;
				}
			}
			return null;
		}

		/// <summary>
		/// if current filter contains one of our items, then make sure we have
		/// a WordSetNullFilter() in our Filters, otherwise remove our WordSetNullFilter();
		/// We also make sure the current filter has a valid set of word references loaded.
		/// </summary>
		public override bool OnAdjustFilterSelection(object argument)
		{
			if (Filters.Count == 0)
			{
				return false;	// we aren't providing any items.
			}
			var currentFilter = argument as RecordFilter;
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
				wordSetFilter.ReloadWordSet(m_cache);
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
	}
}
