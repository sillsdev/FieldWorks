//-------------------------------------------------------------------------------------------------
// <copyright file="SortedCollection.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Abstract base class for a sorted collection.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;
	using System.ComponentModel;

	/// <summary>
	/// Abstract base class for a sorted collection that contains no keys (less storage) and has
	/// change events.
	/// </summary>
	public abstract class SortedCollection : CloneableCollection, IList
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(SortedCollection);

		private IComparer comparer;
		private ArrayList list = new ArrayList();
		private bool readOnly;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="SortedCollection"/> class.
		/// </summary>
		/// <param name="comparer">An <see cref="IComparer"/> to use for the sorting.</param>
		protected SortedCollection(IComparer comparer) : base(new ArrayList())
		{
			Tracer.VerifyNonNullArgument(comparer, "comparer");
			this.comparer = comparer;
			this.list = (ArrayList)this.InnerCollection;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		bool IList.IsFixedSize
		{
			get { return false; }
		}

		public virtual bool IsReadOnly
		{
			get { return this.readOnly; }
			set { this.readOnly = value; }
		}

		protected IComparer Comparer
		{
			get { return this.comparer; }
		}

		protected IList InnerList
		{
			get { return this; }
		}
		#endregion

		#region Events
		//==========================================================================================
		// Events
		//==========================================================================================

		public event CollectionChangeEventHandler CollectionChanged;
		#endregion

		#region Indexers
		//==========================================================================================
		// Indexers
		//==========================================================================================

		object IList.this[int index]
		{
			get { return this.list[index]; }
			set { throw new NotSupportedException("Cannot set on the list because it is sorted."); }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		int IList.Add(object value)
		{
			if (this.IsReadOnly)
			{
				Tracer.Fail("The collection is read-only.");
				throw new NotSupportedException("Cannot add to a read-only collection.");
			}
			this.ValidateType(value);

			this.OnAdd(value);

			// Find the spot to insert the node, or the index if it already exists.
			int index = this.list.BinarySearch(value, this.comparer);
			if (index >= 0)
			{
				throw new ArgumentException("value is already in the collection", "value");
			}

			// Find the location to insert the new item by taking the bitwise complement of the
			// negative number returned from BinarySearch.
			index = ~index;
			this.list.Insert(index, value);
			this.OnAddComplete(index, value);

			// Raise the CollectionChanged event.
			this.OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Add, value));

			return index;
		}

		bool IList.Contains(object value)
		{
			this.ValidateType(value);
			return (this.InnerList.IndexOf(value) >= 0);
		}

		int IList.IndexOf(object value)
		{
			this.ValidateType(value);
			int index = this.list.BinarySearch(value, this.comparer);
			if (index < 0)
			{
				index = -1;
			}
			return index;
		}

		void IList.Insert(int index, object value)
		{
			throw new NotSupportedException("Cannot insert on the list because it is sorted.");
		}

		void IList.Remove(object value)
		{
			this.ValidateType(value);

			// Remove the node from our list.
			this.RemoveAt(this.InnerList.IndexOf(value));
		}

		public void Clear()
		{
			if (this.IsReadOnly)
			{
				throw new NotSupportedException("Cannot clear a read-only collection.");
			}

			while (this.Count > 0)
			{
				this.RemoveAt(0);
			}
		}

		/// <summary>
		/// Removes the element at the specified index after <see cref="OnRemove"/> is called but
		/// before <see cref="OnRemoveComplete"/> is called.
		/// </summary>
		/// <param name="index">A zero-based index of the item to remove.</param>
		public void RemoveAt(int index)
		{
			if (this.IsReadOnly)
			{
				Tracer.Fail("The collection is read-only.");
				throw new NotSupportedException("Cannot remove from a read-only collection.");
			}
			object value = this.InnerList[index];
			this.OnRemove(index, value);

			this.list.RemoveAt(index);
			this.OnRemoveComplete(index, value);

			// Raise the CollectionChanged event after completing the remove.
			this.OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Remove, value));
		}

		/// <summary>
		/// Called right before the value is added to the collection.
		/// </summary>
		/// <param name="value">The value about to be added to the collection.</param>
		protected virtual void OnAdd(object value)
		{
		}

		/// <summary>
		/// Called right after the value is added to the collection.
		/// </summary>
		/// <param name="index">The index of the item added to the collection.</param>
		/// <param name="value">The value just added to the collection.</param>
		protected virtual void OnAddComplete(int index, object value)
		{
			Tracer.WriteLineVerbose(classType, "OnAddComplete", "Added '{0}' to the collection.", value.ToString());
		}

		/// <summary>
		/// Raises the <see cref="CollectionChanged"/> event.
		/// </summary>
		/// <param name="e">The <see cref="CollectionChangeEventArgs"/> that contains more information about the event.</param>
		protected virtual void OnCollectionChanged(CollectionChangeEventArgs e)
		{
			this.MakeDirty();
			if (this.CollectionChanged != null)
			{
				this.CollectionChanged(this, e);
			}
		}

		/// <summary>
		/// Called right before the value is removed from the collection.
		/// </summary>
		/// <param name="index">The index of the item about to be removed from the collection.</param>
		/// <param name="value">The value about to be removed from the collection.</param>
		protected virtual void OnRemove(int index, object value)
		{
		}

		/// <summary>
		/// Called right after the value is removed from the collection.
		/// </summary>
		/// <param name="index">The index of the item removed from the collection.</param>
		/// <param name="value">The value just removed from the collection.</param>
		protected virtual void OnRemoveComplete(int index, object value)
		{
			Tracer.WriteLineVerbose(classType, "OnRemoveComplete", "Removed '{0}' from the collection.", value.ToString());
		}

		/// <summary>
		/// Validates the type to make sure it adheres to the strongly typed collection.
		/// Null values are not accepted by default.
		/// </summary>
		/// <param name="value">The value to verify.</param>
		protected virtual void ValidateType(object value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
		}
		#endregion
	}
}
