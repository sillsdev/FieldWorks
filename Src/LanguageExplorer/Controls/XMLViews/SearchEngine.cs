// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.ObjectModel;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// An abstract class for performing indexing and searching asynchronously.
	/// </summary>
	public abstract class SearchEngine : DisposableBase, IVwNotifyChange
	{
		/// <summary>
		/// Gets the search engine.
		/// </summary>
		public static SearchEngine Get(IPropertyTable propertyTable, string propName, Func<SearchEngine> searchEngineFactory)
		{
			var searchEngine = propertyTable.GetValue<SearchEngine>(propName);
			if (searchEngine == null)
			{
				searchEngine = searchEngineFactory();
				// Don't persist it, and if anyone ever cares about hearing that it changed,
				// then create a new override of this method that feeds the last bool parameter in as 'true'.
				// This default method can then feed that override 'false'.
				propertyTable.SetProperty(propName, searchEngine, false, false);
				propertyTable.SetPropertyDispose(propName, true);
			}
			return searchEngine;
		}

		private readonly StringSearcher<int> m_searcher;

		private IList<ICmObject> m_searchableObjs;
		private readonly Dictionary<Tuple<int, int>, int> m_indexObjPos;
		private readonly ConsumerThread<int, SearchField[]> m_thread;
		private readonly object m_syncRoot;
		private readonly SynchronizationContext m_synchronizationContext;

		/// <summary>
		/// Occurs when the search is completed.
		/// </summary>
		public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

		/// <summary>
		/// Initializes a new instance of the <see cref="SearchEngine"/> class.
		/// </summary>
		protected SearchEngine(LcmCache cache, SearchType type)
		{
			Cache = cache;
			m_searcher = new StringSearcher<int>(type, Cache.ServiceLocator.WritingSystemManager);
			m_thread = new ConsumerThread<int, SearchField[]>(HandleWork);
			m_synchronizationContext = SynchronizationContext.Current;
			m_syncRoot = new object();
			m_indexObjPos = new Dictionary<Tuple<int, int>, int>();

			Cache.DomainDataByFlid.AddNotification(this);

			m_thread.Start();
		}

		/// <summary>
		/// Override to dispose managed resources.
		/// </summary>
		protected override void DisposeManagedResources()
		{
			Cache.DomainDataByFlid.RemoveNotification(this);

			m_thread.Stop();
			m_thread.Dispose();
		}

		/// <summary>
		/// Gets the searchable strings of an object.
		/// </summary>
		protected abstract IEnumerable<ITsString> GetStrings(SearchField field, ICmObject obj);

		/// <summary>
		/// Gets the searchable objects.
		/// </summary>
		protected abstract IList<ICmObject> GetSearchableObjects();

		/// <summary>
		/// Determines if an index reset is required.
		/// </summary>
		protected abstract bool IsIndexResetRequired(int hvo, int flid);

		/// <summary>
		/// Determines if the specified field is a multi-string or multi-unicode field.
		/// </summary>
		protected abstract bool IsFieldMultiString(SearchField field);

		/// <summary>
		/// Gets the cache.
		/// </summary>
		protected LcmCache Cache { get; }

		/// <summary>
		/// Searches the specified fields asynchronously.
		/// </summary>
		public void SearchAsync(IEnumerable<SearchField> fields)
		{
			m_thread.EnqueueWork(fields.ToArray());
		}

		/// <summary>
		/// Searches the specified fields.
		/// N.B. This version is currently only used in testing.
		/// </summary>
		public IEnumerable<int> Search(IEnumerable<SearchField> fields)
		{
			return FilterResults(PerformSearch(fields.ToArray(), () => false));
		}

		/// <summary>
		/// Gets a value indicating whether the search engine is searching.
		/// </summary>
		public bool IsBusy => !m_thread.IsIdle;

		/// <summary>
		/// Builds the search index.
		/// </summary>
		private int BuildIndex(int i, SearchField field, Func<bool> isSearchCanceled)
		{
			if (m_searchableObjs == null)
			{
				m_searchableObjs = GetSearchableObjects();
			}

			for (; i < m_searchableObjs.Count; i++)
			{
				if (isSearchCanceled())
				{
					break;
				}
				foreach (var tss in GetStrings(field, m_searchableObjs[i]))
				{
					m_searcher.Add(m_searchableObjs[i].Hvo, field.Flid, tss);
				}
			}
			return i;
		}

		private static bool IsSearchCanceled(IQueueAccessor<int, SearchField[]> queue)
		{
			return queue.StopRequested || queue.HasWork;
		}

		private void HandleWork(IQueueAccessor<int, SearchField[]> queue)
		{
			var work = queue.GetAllWorkItems().Last();

			if (IsSearchCanceled(queue))
			{
				return;
			}

			var results = PerformSearch(work, () => IsSearchCanceled(queue));

			if (results == null || IsSearchCanceled(queue))
			{
				return;
			}

			m_synchronizationContext.Post(OnSearchCompleted, new SearchCompletedEventArgs(work, FilterResults(results)));
		}

		/// <summary>
		/// If some objects need to be filtered out of the results (for instance the item we started from in the merge dialog)
		/// then this function can be used to do it.
		/// </summary>
		protected virtual IEnumerable<int> FilterResults(IEnumerable<int> results)
		{
			return results;
		}

		private IEnumerable<int> PerformSearch(IList<SearchField> fields, Func<bool> isSearchCanceled)
		{
			var results = new HashSet<int>();
			lock (m_syncRoot)
			{
				foreach (var field in fields)
				{
					if (isSearchCanceled())
					{
						return null;
					}

					var key = IsFieldMultiString(field) ? Tuple.Create(field.Flid, field.String.get_WritingSystemAt(0)) : Tuple.Create(field.Flid, 0);
					int pos;
					if (!m_indexObjPos.TryGetValue(key, out pos))
					{
						pos = 0;
					}

					if (m_searchableObjs == null || pos < m_searchableObjs.Count)
					{
						// only use the IWorkerThreadReadHandler if we are executing on the worker thread
						if (SynchronizationContext.Current == m_synchronizationContext)
						{
							pos = BuildIndex(pos, field, isSearchCanceled);
						}
						else
						{
							using (new WorkerThreadReadHelper(Cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
								pos = BuildIndex(pos, field, isSearchCanceled);
						}
						m_indexObjPos[key] = pos;
					}

				}

				foreach (var field in fields)
				{
					if (isSearchCanceled())
					{
						return null;
					}
					results.UnionWith(m_searcher.Search(field.Flid, field.String));
				}
			}

			return isSearchCanceled() ? null : results;
		}

		void IVwNotifyChange.PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (!IsIndexResetRequired(hvo, tag))
			{
				return;
			}
			lock (m_syncRoot)
			{
				m_searcher.Clear();
				m_indexObjPos.Clear();
				m_searchableObjs = null;
			}
		}

		private void OnSearchCompleted(object e)
		{
			SearchCompleted?.Invoke(this, (SearchCompletedEventArgs) e);
		}
	}
}
