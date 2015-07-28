// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.Utils
{
	/// <summary>
	/// A RecentItemsCache maintains a dictionary of recently-computed values, avoiding the computation
	/// if passed duplicate arguments. For now, the value looked up is parameterized, but the input is
	/// always a string. The user specifies the maximum number of items to save.
	/// </summary>
	public class RecentItemsCache<K, T>
	{
		int MaxValuesToSave { get; set; }
		// This is the actual cache.
		private readonly Dictionary<K, T> m_values = new Dictionary<K, T>();
		// Initially, this tracks how frequently an item has been requested.
		// For items that have survivied a discard cycle, it becomes a count of how much more
		// often the item has been requested than any item we have discarded, plus
		// how often it has been requested since the last discard.
		private readonly Dictionary<K, int> m_frequencies = new Dictionary<K, int>();
		private readonly object m_syncRoot = new object();

		/// <summary>
		/// Make one.
		/// </summary>
		public RecentItemsCache(int maxValuesToSave)
		{
			MaxValuesToSave = maxValuesToSave;
		}

		/// <summary>
		/// Get an item, based on the key. If the value is not already cached, compute it using the supplied
		/// functor, and save it for next time.
		/// </summary>
		public T GetItem(K key, Func<K, T> computeValue)
		{
			bool ignore;
			return GetItem(key, computeValue, out ignore);
		}

		/// <summary>
		/// Get an item, based on the key. If the value is not already cached, compute it using the supplied
		/// functor, and save it for next time. Reports whether item was retrieved from the cache rather
		/// than freshly computed.
		/// </summary>
		public T GetItem(K key, Func<K, T> computeValue, out bool wasRetrievedFromCache)
		{
			lock (m_syncRoot)
			{
				T result;
				if (m_values.TryGetValue(key, out result))
				{
					wasRetrievedFromCache = true;
					m_frequencies[key] = m_frequencies[key] + 1;
					return result;
				}
				wasRetrievedFromCache = false;

				if (m_values.Count >= MaxValuesToSave)
					ReduceCounts();
				result = computeValue(key);
				m_values[key] = result;
				m_frequencies[key] = 1;
				return result;
			}
		}

		/// <summary>
		/// This method is responsible to reduce the number of keys in the dictionary.
		/// Currently it reduces the size and counts by 20%.
		/// </summary>
		private void ReduceCounts()
		{
			var numberToRemove = Math.Max(MaxValuesToSave/5, 1);
			var sortedPairs = m_frequencies.ToList();
			sortedPairs.Sort((kvp1, kvp2) => kvp1.Value.CompareTo(kvp2.Value));
			foreach (var kvp in sortedPairs.Take(numberToRemove))
			{
				m_frequencies.Remove(kvp.Key);
				m_values.Remove(kvp.Key);
			}
			// Get the count of the least frequently requested survivor (but at least by 1;
			// it's possible for survivor counts to be reduced to zero).
			int reduceCountsBy = Math.Max(sortedPairs[numberToRemove - 1].Value, 1);
			foreach (var kvp in sortedPairs.Skip(numberToRemove))
				m_frequencies[kvp.Key] = kvp.Value - reduceCountsBy;
		}
	}
}
