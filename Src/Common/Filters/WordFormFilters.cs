using System;
using System.Collections;
using System.Diagnostics;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.FDO.Ling;
using XCore;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// A filter for selecting a group of wordform spaced on a "WfiWordSet"
	/// </summary>
	public class WordSetFilter : RecordFilter
	{
		//protected WfiWordSet m_wordSet;
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
		public WordSetFilter(WfiWordSet wordSet)
		{
//			m_wordSet = wordSet;
//			m_cases = m_wordSet.CasesRC;
			this.m_id = wordSet.Hvo.ToString();
			m_name = wordSet.Name.AnalysisDefaultWritingSystem;
			LoadCases(wordSet);
		}

		private void LoadCases(WfiWordSet wordSet)
		{
			m_hvos = wordSet.CasesRC.HvoArray;
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
			WfiWordSet wordSet = new WfiWordSet(cache, hvo);
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
////			m_wordSet= WfiWordSet.CreateFromDBObject(m_cache, hvo);
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// decide whether this object should be included
		/// </summary>
		/// <param name="item">The item.</param>
		/// <returns>true if the object should be included</returns>
		/// ------------------------------------------------------------------------------------
		public override bool Accept (ManyOnePathSortItem item)
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

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_filters != null)
					m_filters.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_filters = null;
			m_cache = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter list. this is called because we are an IxCoreColleague
		/// </summary>
		/// <param name="mediator">The mediator.</param>
		/// <param name="configuration">The configuration.</param>
		/// ------------------------------------------------------------------------------------
		public override void Init(Mediator mediator, XmlNode configuration)
		{
			CheckDisposed();
			base.Init(mediator, configuration);
			m_mediator.AddColleague(this);
			m_cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			ReLoad();
		}

		/// <summary>
		/// reload the data items
		/// </summary>
		public override void ReLoad()
		{
			CheckDisposed();
			m_filters.Clear();

			foreach(WfiWordSet words in m_cache.LangProject.MorphologicalDataOA.TestSetsOC)
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
				CheckDisposed();
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
			CheckDisposed();
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
			CheckDisposed();
			if (Filters.Count == 0)
				return false;	// we aren't providing any items.

			RecordFilter currentFilter = argument as RecordFilter;
			if (ContainsOurFilter(currentFilter))
			{
				// we found a match. if we don't already have a WordSetNullFilter, add it now.
				if (!(Filters[0] is WordSetNullFilter))
					Filters.Insert(0, new WordSetNullFilter());
				// make sure the current filter has a valid set of wordform references.
				WordSetFilter wordSetFilter = currentFilter as WordSetFilter;
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
		private bool ContainsOurFilter(RecordFilter filter)
		{
			if (filter == null)
				return false;
			for (int i = 0; i < Filters.Count; ++i)
			{
				if (filter.Contains(Filters[i] as RecordFilter))
					return true;
			}
			return false;
		}
	}
}
