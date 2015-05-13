using System;
using System.Collections;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// A filter for selecting a group of wordform spaced on a "IWfiWordSet"
	/// </summary>
	public class WordSetFilter : RecordFilter
	{
		//protected IWfiWordSet m_wordSet;
		//protected FdoReferenceCollection m_cases;
		//protected FdoCache m_cache;
		/// <summary></summary>
		protected int[] m_hvos;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:WordSetFilter"/> class.
		/// </summary>
		/// <param name="wordSet">The word set.</param>
		/// ------------------------------------------------------------------------------------
		public WordSetFilter(IWfiWordSet wordSet)
		{
//			m_wordSet = wordSet;
//			m_cases = m_wordSet.CasesRC;
			this.m_id = wordSet.Hvo.ToString();
			m_name = wordSet.Name.AnalysisDefaultWritingSystem.Text;
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
		/// <param name="cache"></param>
		internal void ReloadWordSet(FdoCache cache)
		{
			int hvo = Int32.Parse(m_id);
			IWfiWordSet wordSet = cache.ServiceLocator.GetObject(hvo) as IWfiWordSet;
			LoadCases(wordSet);
		}

		/// <summary>
		/// Default constructor for IPersistAsXml
		/// </summary>
		public WordSetFilter()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void PersistAsXml(XmlNode node)
		{
			base.PersistAsXml (node);
			XmlUtils.AppendAttribute(node, "id", m_id);
			XmlUtils.AppendAttribute(node, "wordlist", XmlUtils.MakeIntegerListValue(m_hvos));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void InitXml(XmlNode node)
		{
			base.InitXml (node);
			m_id = XmlUtils.GetManditoryAttributeValue(node, "id");
			m_hvos = XmlUtils.GetMandatoryIntegerListAttributeValue(node, "wordlist");
		}

		/// <summary>
		/// Test to see if this filter matches the other filter.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool SameFilter(RecordFilter other)
		{
			return (other != null && other is WordSetFilter &&
					other.id == this.id && other.Name == this.Name);
		}

		///// <summary>
		///// Initialize the filter
		///// </summary>
		///// <param name="cache"></param>
		///// <param name="configuration"></param>
//		public override void Init(FdoCache cache,XmlNode filterNode)
//		{
////			m_cache = cache;
////			int hvo = m_cache.LangProject.MorphologicalDataOA.TestSetsOC.HvoArray[0];
////			m_wordSet= IWfiWordSet.CreateFromDBObject(m_cache, hvo);
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns>true if the object should be included</returns>
		/// ------------------------------------------------------------------------------------
		public override bool Accept (IManyOnePathSortItem item)
		{
			int hvo = item.KeyObject;

			for(int i = m_hvos.Length-1; i>=0; i--)
			{
				if(m_hvos[i] == hvo)
					return true;
			}

			return false;
		}

		/// <summary>
		/// This is always set by the user.
		/// </summary>
		public override bool IsUserVisible
		{
			get { return true; }
		}
	}
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------

	/// <summary>
	/// Used for disabling WordSet filters
	/// </summary>
	public class WordSetNullFilter : NullFilter
	{
		public WordSetNullFilter()
		{
			m_name="Clear Wordform-Set Filter";
		}
	}

	public  class WfiRecordFilterListProvider : RecordFilterListProvider
	{
		/// <summary></summary>
		protected FdoCache m_cache;
		// Collection of WordSetFilter objects.
		/// <summary></summary>
		protected ArrayList m_filters;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:WfiRecordFilterListProvider"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public WfiRecordFilterListProvider()
		{
			m_filters = new ArrayList();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter list. this is called because we are an IxCoreColleague
		/// </summary>
		/// <param name="mediator">The mediator.</param>
		/// <param name="propertyTable"></param>
		/// <param name="configuration">The configuration.</param>
		/// ------------------------------------------------------------------------------------
		public override void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configuration)
		{
			base.Init(mediator, propertyTable, configuration);
			m_mediator.AddColleague(this);
			m_cache = m_propertyTable.GetValue<FdoCache>("cache");
			ReLoad();
		}

		/// <summary>
		/// reload the data items
		/// </summary>
		public override void ReLoad()
		{
			m_filters.Clear();

			foreach(IWfiWordSet words in m_cache.LangProject.MorphologicalDataOA.TestSetsOC)
			{
				WordSetFilter f = new WordSetFilter(words);
				m_filters.Add(f);
			}
		}

		/// <summary>
		/// the list of filters.
		/// </summary>
		public override ArrayList Filters
		{
			get
			{
				return m_filters;
			}
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the filter.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------------
		public override object GetFilter(string id)
		{
			foreach(RecordFilter filter in m_filters)
			{
				if (filter.id == id)
					return filter;
			}
			return null;
		}

		/// <summary>
		/// if current filter contains one of our items, then make sure we have
		/// a WordSetNullFilter() in our Filters, otherwise remove our WordSetNullFilter();
		/// We also make sure the current filter has a valid set of word references loaded.
		/// </summary>
		/// <param name="argument">current filter</param>
		/// <returns>true if handled.</returns>
		public override bool OnAdjustFilterSelection(object argument)
		{
			if (Filters.Count == 0)
				return false;	// we aren't providing any items.

			RecordFilter currentFilter = argument as RecordFilter;
			RecordFilter matchingFilter;
			if (ContainsOurFilter(currentFilter, out matchingFilter))
			{
				// we found a match. if we don't already have a WordSetNullFilter, add it now.
				if (!(Filters[0] is WordSetNullFilter))
					Filters.Insert(0, new WordSetNullFilter());
				// make sure the current filter has a valid set of wordform references.
				WordSetFilter wordSetFilter = matchingFilter as WordSetFilter;
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

		/// <summary>
		///
		/// </summary>
		/// <param name="filter"></param>
		/// <returns>True if one of our filters can be found in the given filter.</returns>
		private bool ContainsOurFilter(RecordFilter filter, out RecordFilter matchingFilter)
		{
			matchingFilter = null;
			if (filter == null)
				return false;
			for (int i = 0; i < Filters.Count; ++i)
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
