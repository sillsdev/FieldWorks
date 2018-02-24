// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Diagnostics;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// Like ListBox, FwListBox defines an ObjectCollection class for the items
	/// in the list. This class provides a subset of the functionality of the
	/// ArrayList used to implement it. The reason for not using an ArrayList is that
	/// operations that change the contents of the list have to have side effects
	/// on the control.
	/// </summary>
	public class ObjectCollection : IList, IDisposable
	{
		private ArrayList m_list;
		private FwListBox m_owner;

		/// <summary>
		/// Construct empty.
		/// </summary>
		public ObjectCollection(FwListBox owner)
		{
			m_list = new ArrayList();
			m_owner = owner;
		}

		/// <summary>
		/// Construct with supplied initial items.
		/// </summary>
		public ObjectCollection(FwListBox owner, object[] values)
		{
			m_list = new ArrayList(values);
			m_owner = owner;
		}

		#region IDisposable & Co. implementation
		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~ObjectCollection()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary />
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

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
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				ClearAllItems();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_list = null;
			m_owner = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		bool IList.IsFixedSize => false;

		/// <summary>Number of items. </summary>
		public virtual int Count => m_list.Count;

		/// <summary>Always false; we don't support the DataSource approach. </summary>
		public virtual bool IsReadOnly => false;

		/// <summary>
		/// Indexer. Set must modify the display.
		/// </summary>
		public virtual object this[int index]
		{
			get
			{
				return m_list[index];
			}
			set
			{
				var oldText = m_owner.TextOfItem(m_list[index]);
				m_list[index] = value;
				var newText = m_owner.TextOfItem(m_list[index]);
				if (!oldText.Equals(newText))
				{
					var hvo = m_owner.DataAccess.get_VecItem(InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems, index);
					m_owner.DataAccess.SetString(hvo, InnerFwListBox.ktagText, newText);
					if (!m_owner.Updating)
					{
						m_owner.DataAccess.PropChanged(null, (int) PropChangeType.kpctNotifyAll, hvo, InnerFwListBox.ktagText, 0, newText.Length, oldText.Length);
					}
				}
			}
		}

		/// <summary>
		/// Add an item to the collection and the display.
		/// </summary>
		public int Add(object item)
		{
			var index = m_list.Count; // nb index is count BEFORE Add.
			m_list.Add(item);
			InsertItemAtIndex(index, item);
			return index;
		}

		/// <summary>
		/// Add a whole collection of objects.
		/// </summary>
		public void AddRange(IEnumerable items)
		{
			var index = m_list.Count; // nb index is count BEFORE Add.
			var i = 0;
			foreach (var item in items)
			{
				m_list.Add(item);
				var hvoNew = m_owner.DataAccess.MakeNewObject(InnerFwListBox.kclsItem, InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems, index + i);
				m_owner.DataAccess.SetString(hvoNew, InnerFwListBox.ktagText,  m_owner.TextOfItem(item));
				i++;
			}
			if (!m_owner.Updating)
			{
				m_owner.DataAccess.PropChanged(null, (int) PropChangeType.kpctNotifyAll, InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems, index, i, 0);
			}
		}

		/// <summary>
		/// Clear all items.
		/// </summary>
		/// <remarks>
		/// Enhance JohnT: add the version that takes an ObjectCollection.
		/// </remarks>
		public virtual void Clear()
		{
			var citems = m_list.Count;
			ClearAllItems();
			var cda = m_owner.DataAccess as IVwCacheDa;
			if (cda == null)
			{
				return; // This can happen, when this is called when 'disposing' is false.
			}
			cda.CacheVecProp(InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems, new int[0], 0);
			if (!m_owner.Updating)
			{
				m_owner.DataAccess.PropChanged(null, (int) PropChangeType.kpctNotifyAll, InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems, 0, 0, citems);
			}

			m_owner.SelectedIndex = -1;

			Debug.Assert(m_owner.DataAccess.get_VecSize(InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems) == 0);
		}

		private void ClearAllItems()
		{
			foreach (var obj in m_list)
			{
				// Dispose items
				var disposable = obj as IDisposable;
				disposable?.Dispose();
			}
			m_list.Clear();
		}

		/// <summary>
		/// See if the item is present.
		/// </summary>
		public virtual bool Contains(object item)
		{
			return m_list.Contains(item);
		}

		/// <summary>
		/// Copy to a destination array.
		/// </summary>
		void ICollection.CopyTo(Array dest, int arrayIndex)
		{
			m_list.CopyTo(dest, arrayIndex);
		}

		/// <summary>
		/// Syncrhonization is not supported.
		/// </summary>
		object ICollection.SyncRoot => this;

		/// <summary>
		/// Synchronization is not supported.
		/// </summary>
		bool ICollection.IsSynchronized => false;

		/// <summary>
		/// Get an enumerator for the list.
		/// </summary>
		public virtual IEnumerator GetEnumerator()
		{
			return m_list.GetEnumerator();
		}

		/// <summary>
		/// Find the zero-based position of the item, or -1 if not found.
		/// </summary>
		public virtual int IndexOf(object item)
		{
			return m_list.IndexOf(item);
		}

		/// <summary>
		/// Insert the specified item at the specified position.
		/// </summary>
		public virtual void Insert(int index, object item)
		{
			m_list.Insert(index, item);
			InsertItemAtIndex(index, item);
		}

		/// <summary>
		/// Shared function for Insert and Add.
		/// </summary>
		protected void InsertItemAtIndex(int index, object item)
		{
			var hvoNew = m_owner.DataAccess.MakeNewObject(InnerFwListBox.kclsItem, InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems, index);
			m_owner.DataAccess.SetString(hvoNew, InnerFwListBox.ktagText, m_owner.TextOfItem(item));
			if (!m_owner.Updating)
			{
				m_owner.DataAccess.PropChanged(null, (int) PropChangeType.kpctNotifyAll, InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems, index, 1, 0);
			}

			if (m_owner.SelectedIndex >= index)
			{
				m_owner.SelectedIndex = m_owner.SelectedIndex + 1;
			}

			Debug.Assert(m_owner.DataAccess.get_VecSize(InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems) == m_list.Count);
		}

		/// <summary />
		public virtual void Remove(object item)
		{
			var index = m_list.IndexOf(item);
			if (index >= 0)
			{
				RemoveAt(index);
			}
		}

		/// <summary />
		public virtual void RemoveAt(int index)
		{
			m_list.RemoveAt(index);
			var hvoObj = m_owner.DataAccess.get_VecItem(InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems, index);
			m_owner.DataAccess.DeleteObjOwner(InnerFwListBox.khvoRoot, hvoObj, InnerFwListBox.ktagItems, index);
			if (!m_owner.Updating)
			{
				m_owner.DataAccess.PropChanged(null, (int) PropChangeType.kpctNotifyAll, InnerFwListBox.khvoRoot, InnerFwListBox.ktagItems, index, 0, 1);
			}

			if (m_owner.SelectedIndex >= index)
			{
				m_owner.SelectedIndex = m_owner.SelectedIndex - 1;
			}
		}
	}
}