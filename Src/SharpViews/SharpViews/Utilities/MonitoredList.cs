// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;

namespace SIL.FieldWorks.SharpViews.Utilities
{
	/// <summary>
	/// A monitored list is a simple wrapper around a regular List, with the additional behavior of raising
	/// a Changed event whenever the list is modified. Commonly a MonitoredList is used as the implementation
	/// of a property X, an an event XChanged is created which is raised whenever the Changed event of X
	/// is raised. This pattern works well with the automatic event hooking of ViewBuilder.
	/// </summary>
	public class MonitoredList<T> : IList<T>
	{
		List<T> m_list = new List<T>();

		public event EventHandler<EventArgs> Changed;

		void RaiseChanged()
		{
			if (Changed != null)
				Changed(this, new EventArgs());
		}

		public IEnumerator<T> GetEnumerator()
		{
			return m_list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)m_list).GetEnumerator();
		}

		public void Add(T item)
		{
			m_list.Add(item);
			RaiseChanged();
		}

		public void Clear()
		{
			m_list.Clear();
			RaiseChanged();
		}

		public bool Contains(T item)
		{
			return m_list.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			m_list.CopyTo(array, arrayIndex);
		}

		public bool Remove(T item)
		{
			if (m_list.Remove(item))
			{
				RaiseChanged();
				return true;
			}
			return false;
		}

		public int Count
		{
			get { return m_list.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public int IndexOf(T item)
		{
			return m_list.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			m_list.Insert(index, item);
			RaiseChanged();
		}

		public void RemoveAt(int index)
		{
			m_list.RemoveAt(index);
			RaiseChanged();
		}

		public T this[int index]
		{
			get { return m_list[index]; }
			set
			{
				m_list[index] = value;
				RaiseChanged();
			}
		}
	}
}
