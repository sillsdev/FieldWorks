// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.SharpViews.Hookups;

namespace SIL.FieldWorks.SharpViews.Utilities
{
	/// <summary>
	/// A monitored list is a simple wrapper around a regular List, with the additional behavior of raising
	/// a Changed event whenever the list is modified. Commonly a MonitoredList is used as the implementation
	/// of a property X, an an event XChanged is created which is raised whenever the Changed event of X
	/// is raised. This pattern works well with the automatic event hooking of ViewBuilder.
	/// </summary>
	public class ModifiedMonitoredList<T> : IList<T>
	{
		List<T> m_list = new List<T>();

		public event EventHandler<ObjectSequenceEventArgs> Changed;

		void RaiseChanged(int firstChange, int numberAdded, int numberDeleted)
		{
			if (Changed != null)
				Changed(this, new ObjectSequenceEventArgs(firstChange, numberAdded, numberDeleted));
		}

		public IEnumerator<T> GetEnumerator()
		{
			return m_list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)m_list).GetEnumerator();
		}

		public void SimulateChange(int firstChange, int numberChange)
		{
			RaiseChanged(firstChange, numberChange, numberChange);
		}

		public void Add(T item)
		{
			m_list.Add(item);
			RaiseChanged(Count - 1, 1, 0);
		}

		public void Clear()
		{
			int count = Count;
			m_list.Clear();
			RaiseChanged(0, 0, count);
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
			int index = IndexOf(item);
			if (m_list.Remove(item))
			{
				RaiseChanged(index, 0, 1);
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
			RaiseChanged(index, 1, 0);
		}

		public void RemoveAt(int index)
		{
			m_list.RemoveAt(index);
			RaiseChanged(index, 0, 1);
		}

		public T this[int index]
		{
			get { return m_list[index]; }
			set
			{
				m_list[index] = value;
				RaiseChanged(index, 1, 1);
			}
		}
	}
}
