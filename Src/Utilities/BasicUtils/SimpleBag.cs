using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.Utils
{
	/// <summary>
	/// A simple Bag is designed to be embedded in another object (typically a CmObject).
	/// As a struct it takes minimal space, especially if empty. However, be careful of copying them,
	/// which will do a shallow copy of the contents, with unpredictable results if you make changes
	/// to one of the copies.
	/// </summary>
	public struct SimpleBag<T> : IBag<T>
	{
		/// <summary>
		/// May have several values, as follows. Not all yet implemented.
		///     - null: the bag is empty
		///     - T: the bag contains a single occurrence of that one item
		///     - array of T: the bag contains the items listed (possibly with duplicates). Use this up to about 5 items. (Not implemented)
		///     - HashSet of T: the bag contains the items listed, each exactly once.
		///     - Dictionary from T to int: the bag contains the items that are the keys, each as many times as indicated by the count.
		/// </summary>
		private object m_contents;
		/// <summary>
		/// Add an item to the bag.
		/// </summary>
		public void Add(T item)
		{
			if (m_contents == null)
			{
				m_contents = item;
				return;
			}
			if (m_contents is T)
			{
				if (item.Equals((T)m_contents))
				{
					// Direct to duplicates.
					var contents = new Dictionary<T, int>();
					contents[(T)m_contents] = 2;
					m_contents = contents;
				}
				else
				{
					var contents = new HashSet<T>();
					contents.Add((T)m_contents);
					m_contents = contents;
					contents.Add(item);
				}
				return;
			}
			if (m_contents is HashSet<T>)
			{
				var contents = m_contents as HashSet<T>;
				if (contents.Contains(item))
				{
					// Switch to dictionary representation
					var newContents = new Dictionary<T, int>();
					foreach (var x in contents)
						newContents[x] = 1;
					m_contents = newContents;
					AddToDict(newContents, item);
					return;
				}
				contents.Add(item);
				return;
			}
			// Otherwise it must be a dictionary.
			AddToDict(m_contents as Dictionary<T, int>, item);
		}

		/// <summary>
		/// Remove an item from the bag.
		/// </summary>
		public bool Remove(T item)
		{
			if (m_contents is T && item.Equals((T)m_contents))
			{
				m_contents = null;
				return true;
			}
			if (m_contents is HashSet<T>)
			{
				var contents = m_contents as HashSet<T>;
				if (!contents.Contains(item))
					return false;
				contents.Remove(item);
				// In theory, if we are down to just one item, we could go back to a simpler representation;
				// but it's not worth it as this case is vanishingly rare.
				return true;
			}
			// Must be dictionary.
			if (m_contents is Dictionary<T, int>)
			{
				// In theory, if we are down to just one item, we could go back to a simpler representation;
				// but it's not worth it as this case is vanishingly rare.
				return RemoveFromDict(m_contents as Dictionary<T, int>, item);
			}
			return false;
		}

		void AddToDict(Dictionary<T, int> dict, T item)
		{
			int oldCount;
			dict.TryGetValue(item, out oldCount);
			dict[item] = oldCount + 1;
		}

		bool RemoveFromDict(Dictionary<T, int> dict, T item)
		{
			int oldCount;
			if (!dict.TryGetValue(item, out oldCount))
			{
				return false;
			}
			if (oldCount > 1)
				dict[item] = oldCount - 1;
			else
				dict.Remove(item);
			return true;
		}

		/// <summary>
		/// The number of things in the bag. Not the number of distinct items, but the total number.
		/// </summary>
		public int Count
		{
			get
			{
				if (m_contents is T)
					return 1;
				if (m_contents is HashSet<T>)
					return (m_contents as HashSet<T>).Count;
				if (m_contents is Dictionary<T, int>)
				{
					int result = 0;
					foreach (var kvp in (m_contents as Dictionary<T, int>))
					{
						result += kvp.Value;
					}
					return result;
				}
				return 0;
			}
		}

		/// <summary>
		/// Return the number of times the specified item occurs in the bag. Includes zero for items not in at all.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public int Occurrences(T item)
		{
			if (m_contents is T)
				if (item.Equals(m_contents))
					return 1;
				else
					return 0;
			if (m_contents is HashSet<T>)
				return (m_contents as HashSet<T>).Contains(item) ? 1 : 0;
			if (m_contents is Dictionary<T, int>)
			{
				int result;
				(m_contents as Dictionary<T, int>).TryGetValue(item, out result);
				return result;
			}
			return 0;
		}

		/// <summary>
		/// Return some enumeration of the distinct items in the bag (once each).
		/// </summary>
		public IEnumerable<T> Items
		{
			get
			{
				if (m_contents == null)
					return new T[0];
				if (m_contents is T)
					return new T[] { (T)m_contents };
				if (m_contents is HashSet<T>)
					return (HashSet<T>)m_contents;
				return ((Dictionary<T, int>)m_contents).Keys;
			}
		}


		/// <summary>
		/// Implementation of IEnumerable of T"/>
		/// </summary>
		/// <returns></returns>
		public IEnumerator<T> GetEnumerator()
		{
			if (m_contents is T)
				yield return (T)m_contents;
			if (m_contents is HashSet<T>)
				foreach (var item in m_contents as HashSet<T>)
					yield return item;
			if (m_contents is Dictionary<T, int>)
			{
				foreach (var kvp in (m_contents as Dictionary<T, int>))
				{
					for (int i = 0; i < kvp.Value; i++)
						yield return kvp.Key;
				}
			}
		}

		/// <summary>
		/// Implementation of plain IEnumerable.
		/// </summary>
		/// <returns></returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	/// <summary>
	/// An interface for read-only access to a Bag: like a set but tracks how many times each item is present
	/// (that is, how many more times it has been added than deleted).
	/// </summary>
	public interface IBag<T> : IEnumerable<T>
	{
		/// <summary>
		/// Return the number of times the specified item occurs in the bag. Includes zero for items not in at all.
		/// </summary>
		int Occurrences(T item);
		/// <summary>
		/// The number of things in the bag. Not the number of distinct items, but the total number.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Return some enumeration of the distinct items in the bag (once each).
		/// </summary>
		IEnumerable<T> Items { get; }
	}

	/// <summary>
	/// A safe way to provide read-only access to a SimpleBag.
	/// Because it is passed a functor that obtains a copy of the bag on each method call,
	/// it 'sees' changes to the underlying simple bag reliably.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class BagWrapper<T> : IBag<T>
	{
		private Func<IBag<T>> m_bagGetter;

		/// <summary>
		/// Make one. The func should return the simple bag you want to wrap.
		/// </summary>
		public BagWrapper(Func<IBag<T>> bagGetter)
		{
			m_bagGetter = bagGetter;
		}

		/// <summary>
		/// Get the enumerator of the wrapped bag.
		/// </summary>
		public IEnumerator<T> GetEnumerator()
		{
			return m_bagGetter().GetEnumerator();
		}

		/// <summary>
		/// Get the enumerator of the wrapped bag.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_bagGetter().GetEnumerator();
		}

		/// <summary>
		/// Get the occurrences of the wrapped bag.
		/// </summary>
		public int Occurrences(T item)
		{
			return m_bagGetter().Occurrences(item);
		}

		/// <summary>
		/// Get the Count of the wrapped bag.
		/// </summary>
		public int Count
		{
			get { return m_bagGetter().Count; }
		}

		/// <summary>
		/// Get the Items of the wrapped bag.
		/// </summary>
		public IEnumerable<T> Items
		{
			get { return m_bagGetter().Items; }
		}
	}
}
