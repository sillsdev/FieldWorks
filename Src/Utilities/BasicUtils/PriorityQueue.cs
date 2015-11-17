// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SIL.Utils
{
	/// <summary>
	/// This priority queue implementation works well when there are a small set of possible
	/// priority values, such as an enumeration. The queue is indexed by the values so that
	/// <c>Remove</c> and <c>Contains</c> are close to O(1).
	/// </summary>
	public class PriorityQueue<P, T> : ICollection<T>
	{
		private struct IndexEntry
		{
			public P Priority
			{
				get; set;
			}

			public LinkedListNode<T> Node
			{
				get; set;
			}
		}

		private readonly SortedDictionary<P, LinkedList<T>> m_queues;
		private readonly Dictionary<T, List<IndexEntry>> m_index;

		/// <summary>
		/// Initializes a new instance of the <see cref="PriorityQueue&lt;P, T&gt;"/> class.
		/// </summary>
		public PriorityQueue()
			: this (Comparer<P>.Default, EqualityComparer<T>.Default)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PriorityQueue&lt;P, T&gt;"/> class.
		/// </summary>
		/// <param name="priorityComparer">The priority comparer.</param>
		/// <param name="itemComparer">The item comparer.</param>
		public PriorityQueue(IComparer<P> priorityComparer, IEqualityComparer<T> itemComparer)
		{
			m_queues = new SortedDictionary<P, LinkedList<T>>(priorityComparer);
			m_index = new Dictionary<T, List<IndexEntry>>(itemComparer);
		}

		/// <summary>
		/// Gets the priority comparer.
		/// </summary>
		/// <value>The priority comparer.</value>
		public IComparer<P> PriorityComparer
		{
			get
			{
				return m_queues.Comparer;
			}
		}

		/// <summary>
		/// Gets the item comparer.
		/// </summary>
		/// <value>The item comparer.</value>
		public IEqualityComparer<T> ItemComparer
		{
			get
			{
				return m_index.Comparer;
			}
		}

		/// <summary>
		/// Enqueues the specified item with the specified priority.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <param name="item">The item.</param>
		public void Enqueue(P priority, T item)
		{
			Enqueue(priority, item, true);
		}

		/// <summary>
		/// Enqueues the specified item with the specified priority.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <param name="item">The item.</param>
		/// <param name="update">if set to <c>true</c> and the item is already in the queue, it will be updated.</param>
		public void Enqueue(P priority, T item, bool update)
		{
			bool enqueue = true;
			List<IndexEntry> entries;
			if (m_index.TryGetValue(item, out entries))
			{
				// the item is already in the queue
				if (update)
				{
					// try to update it
					for (int i = entries.Count - 1; i >= 0; i--)
					{
						if (m_queues.Comparer.Compare(priority, entries[i].Priority) >= 0)
						{
							// the existing item is already at or higher than the specified priority, so just replace it
							var queue = entries[i].Node.List;
							var node = queue.AddAfter(entries[i].Node, item);
							queue.Remove(entries[i].Node);
							entries[i] = new IndexEntry {Priority = entries[i].Priority, Node = node};
							// do not add it to the queue, since it is already at the correct priority
							enqueue = false;
						}
						else
						{
							// the existing item is at a lower priority than the specified priority, so remove it
							RemoveItem(entries, i);
						}
					}
				}
			}
			else
			{
				entries = new List<IndexEntry>();
				m_index[item] = entries;
			}

			if (enqueue)
			{
				LinkedList<T> queue;
				if (!m_queues.TryGetValue(priority, out queue))
				{
					// create the queue for the specified priority if it doesn't exist
					queue = new LinkedList<T>();
					m_queues.Add(priority, queue);
				}
				// add to the queue
				queue.AddLast(item);

				// add the item to the index
				entries.Add(new IndexEntry {Priority = priority, Node = queue.Last});
			}
		}

		/// <summary>
		/// Dequeues the next item in the queue.
		/// </summary>
		/// <returns></returns>
		public T Dequeue()
		{
			P priority;
			return Dequeue(out priority);
		}

		/// <summary>
		/// Dequeues the next item in the queue.
		/// </summary>
		/// <returns></returns>
		public T Dequeue(out P priority)
		{
			// get the first item in the first queue
			var priorityQueuePair = m_queues.First();
			var item = priorityQueuePair.Value.First.Value;
			priority = priorityQueuePair.Key;

			List<IndexEntry> entries = m_index[item];
			int entryIndex = 0;
			for (int i = 0; i < entries.Count; i++)
			{
				if (entries[i].Node == priorityQueuePair.Value.First)
				{
					entryIndex = i;
					break;
				}
			}

			RemoveItem(entries, entryIndex);

			return item;
		}

		/// <summary>
		/// Returns the next item in the queue, but does not remove it.
		/// </summary>
		/// <returns></returns>
		public T Peek()
		{
			P priority;
			return Peek(out priority);
		}

		/// <summary>
		/// Returns the next item in the queue, but does not remove it.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <returns></returns>
		public T Peek(out P priority)
		{
			var priorityQueuePair = m_queues.First();
			priority = priorityQueuePair.Key;
			return priorityQueuePair.Value.First.Value;
		}

		/// <summary>
		/// Gets a value indicating whether the queue is empty
		/// </summary>
		/// <value><c>true</c> if the queue is empty; otherwise, <c>false</c>.</value>
		public bool IsEmpty
		{
			get
			{
				return m_queues.Count == 0;
			}
		}

		/// <summary>
		/// Gets the number of elements with the specified priority.
		/// </summary>
		/// <param name="priority">The priority.</param>
		/// <returns></returns>
		public int GetPriorityCount(P priority)
		{
			LinkedList<T> queue;
			if (m_queues.TryGetValue(priority, out queue))
				return queue.Count;
			return 0;
		}

		private void RemoveItem(List<IndexEntry> entries, int entryIndex)
		{
			var queue = entries[entryIndex].Node.List;
			// remove the item from the queue
			queue.Remove(entries[entryIndex].Node);
			if (queue.Count == 0)
				// clean up queues
				m_queues.Remove(entries[entryIndex].Priority);

			var item = entries[entryIndex].Node.Value;
			// remove the item from the index
			entries.RemoveAt(entryIndex);
			if (entries.Count == 0)
				// clean up index
				m_index.Remove(item);
		}

		#region Implementation of IEnumerable

		/// <summary>
		/// Returns an enumerator that iterates through the queue.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the queue.
		/// </returns>
		/// <filterpriority>1</filterpriority>
		public IEnumerator<T> GetEnumerator()
		{
			foreach (var queue in m_queues.Values)
			{
				foreach (var item in queue)
					yield return item;
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "We're returning an object")]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region Implementation of ICollection<T>

		/// <summary>
		/// Adds an item to the queue with the default priority.
		/// </summary>
		/// <param name="item">The object to add.</param>
		public void Add(T item)
		{
			Enqueue(default(P), item);
		}

		/// <summary>
		/// Removes all items from the queue.
		/// </summary>
		public void Clear()
		{
			m_queues.Clear();
			m_index.Clear();
		}

		/// <summary>
		/// Determines whether the queue contains a specific value.
		/// </summary>
		/// <returns>
		/// true if <paramref name="item"/> is found in the queue; otherwise, false.
		/// </returns>
		/// <param name="item">The object to locate in the queue.</param>
		public bool Contains(T item)
		{
			return m_index.ContainsKey(item);
		}

		/// <summary>
		/// Copies the elements of the queue to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
		/// <exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.
		///                     -or-
		///						<paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.
		///                     -or-
		///                     The number of elements in the source queue is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
		/// </exception>
		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException("array");
			if (arrayIndex < 0)
				throw new ArgumentOutOfRangeException("arrayIndex");
#if !__MonoCS__ // TODO-Linux FWNX-163: workaround non thread safety.
			if (arrayIndex >= array.Length || Count > array.Length - arrayIndex)
				throw new ArgumentException("arrayIndex");
#endif
			if (array.Rank > 1)
				throw new ArgumentException("array");

			foreach (var item in this)
			{
#if __MonoCS__ // TODO-Linux FWNX-163: workaround non thread safety.
				if (arrayIndex >= array.Length)
					break;
#endif
				array[arrayIndex++] = item;
			}
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the queue.
		/// </summary>
		/// <returns>
		/// true if <paramref name="item"/> was successfully removed from the queue; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </returns>
		/// <param name="item">The object to remove.</param>
		public bool Remove(T item)
		{
			List<IndexEntry> entries;
			// if the queue doesn't contain this item, just return false
			if (!m_index.TryGetValue(item, out entries))
				return false;

			int bestIndex = -1;
			for (int i = 0; i < entries.Count; i++)
			{
				if (bestIndex == -1 || m_queues.Comparer.Compare(entries[i].Priority, entries[bestIndex].Priority) < 0)
					bestIndex = i;
			}

			RemoveItem(entries, bestIndex);

			return true;
		}

		/// <summary>
		/// Gets the number of elements contained in the queue.
		/// </summary>
		/// <returns>
		/// The number of elements contained in the queue.
		/// </returns>
		public int Count
		{
			get
			{
				int count = 0;
				foreach (var queue in m_queues.Values)
					count += queue.Count;
				return count;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the queue is read-only.
		/// </summary>
		/// <returns>
		/// true if the queue is read-only; otherwise, false.
		/// </returns>
		public bool IsReadOnly
		{
			get { return false; }
		}

		#endregion
	}
}
