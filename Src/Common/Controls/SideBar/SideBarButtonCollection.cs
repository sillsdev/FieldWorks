// ------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: SideBarButtonCollection.cs
// Responsibility: EberhardB
// Last reviewed:
//
// Implementation of strongly-typed collection SideBarButtonCollection
//
// <remarks>Automatically created by CollectionGenerator</remarks>
// ------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{

	/// <summary>
	///     <para>
	///       A collection that stores <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/>
	[Serializable()]
	[Editor("SIL.FieldWorks.Common.Controls.Design.EnhancedCollectionEditor", "System.Drawing.Design.UITypeEditor")]
	public class SideBarButtonCollection : CollectionBase, IFWDisposable
	{
		// Event signaturs
		/// <summary>Represents the method that will handle the <see cref="BeforeInsert"/>,
		/// <see cref="AfterInsert"/>, <see cref="BeforeRemove"/> and <see cref="AfterRemove"/>
		/// events.</summary>
		/// <param name="index">Index of item in collection that will change</param>
		/// <param name="value">New value of object</param>
		public delegate void CollectionChange(int index, object value);
		/// <summary>Represents the method that will handle the <see cref="BeforeClear"/>
		/// and <see cref="AfterClear"/> events.</summary>
		public delegate void CollectionClear();

		// Events
		/// <summary>Occurs before a button is inserted in the collection</summary>
		public event CollectionChange BeforeInsert;
		/// <summary>Occurs after a button is inserted in the collection</summary>
		public event CollectionChange AfterInsert;
		/// <summary>Occurs before a button is removed from the collection</summary>
		public event CollectionChange BeforeRemove;
		/// <summary>Occurs after a button is removed from the collection</summary>
		public event CollectionChange AfterRemove;
		/// <summary>Occurs before a clear on the collection</summary>
		public event CollectionClear  BeforeClear;
		/// <summary>Occurs after a clear on the collection</summary>
		public event CollectionClear  AfterClear;

		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/>.
		///    </para>
		/// </summary>
		public SideBarButtonCollection()
		{
		}

		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/> based on another <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/> from which the contents are copied
		/// </param>
		public SideBarButtonCollection(SideBarButtonCollection value)
		{
			this.AddRange(value);
		}

		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/> containing any array of <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/> objects with which to intialize the collection
		/// </param>
		public SideBarButtonCollection(SideBarButton[] value)
		{
			this.AddRange(value);
		}

		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public SideBarButton this[int index]
		{
			get
			{
				CheckDisposed();

				return ((SideBarButton)(List[index]));
			}
			set
			{
				CheckDisposed();

				List[index] = value;
			}
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~SideBarButtonCollection()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				foreach (SideBarButton btn in List)
					btn.Dispose();
				List.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		///    <para>Adds a <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/> with the specified value to the
		///    <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection.AddRange(SIL.FieldWorks.Common.Controls.SideBarButton[])'/>
		public int Add(SideBarButton value)
		{
			CheckDisposed();

			return List.Add(value);
		}

		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection.Add'/>
		public void AddRange(SideBarButton[] value)
		{
			CheckDisposed();

			for (int i = 0; (i < value.Length); i = (i + 1))
			{
				this.Add(value[i]);
			}
		}

		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection.Add'/>
		public void AddRange(SideBarButtonCollection value)
		{
			CheckDisposed();

			for (int i = 0; (i < value.Count); i = (i + 1))
			{
				this.Add(value[i]);
			}
		}

		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/> contains the specified <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection.IndexOf'/>
		public bool Contains(SideBarButton value)
		{
			CheckDisposed();

			return List.Contains(value);
		}

		/// <summary>
		/// <para>Copies the <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(SideBarButton[] array, int index)
		{
			CheckDisposed();

			List.CopyTo(array, index);
		}

		/// <summary>
		///    <para>Returns the index of a <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/> in
		///       the <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/> of <paramref name='value'/> in the
		/// <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection.Contains'/>
		public int IndexOf(SideBarButton value)
		{
			CheckDisposed();

			return List.IndexOf(value);
		}

		/// <summary>
		/// <para>Inserts a <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/> into the <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection.Add'/>
		public void Insert(int index, SideBarButton value)
		{
			CheckDisposed();

			List.Insert(index, value);
		}

		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new SideBarButtonEnumerator GetEnumerator()
		{
			CheckDisposed();

			return new SideBarButtonEnumerator(this);
		}

		/// <summary>
		///    <para> Removes a specific <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/> from the
		///    <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='SIL.FieldWorks.Common.Controls.SideBarButton'/> to remove from the <see cref='SIL.FieldWorks.Common.Controls.SideBarButtonCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(SideBarButton value)
		{
			CheckDisposed();

			List.Remove(value);
		}

		/// <summary>
		/// Enumerator for SideBarButtons
		/// </summary>
		public class SideBarButtonEnumerator : IEnumerator
		{
			private IEnumerator baseEnumerator;

			private IEnumerable temp;

			/// <summary>
			/// Initializes a new instance of SideBarButtonEnumerator class
			/// </summary>
			/// <param name="mappings"></param>
			public SideBarButtonEnumerator(SideBarButtonCollection mappings)
			{
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}

			/// <summary>
			/// Gets the current element
			/// </summary>
			public SideBarButton Current
			{
				get
				{
					return ((SideBarButton)(baseEnumerator.Current));
				}
			}

			object IEnumerator.Current
			{
				get
				{
					return baseEnumerator.Current;
				}
			}

			/// <summary>
			/// Moves to the next element in the collection
			/// </summary>
			/// <returns>True if next element exists</returns>
			public bool MoveNext()
			{
				return baseEnumerator.MoveNext();
			}

			bool IEnumerator.MoveNext()
			{
				return baseEnumerator.MoveNext();
			}

			/// <summary>
			/// Resets the collection
			/// </summary>
			public void Reset()
			{
				baseEnumerator.Reset();
			}

			void IEnumerator.Reset()
			{
				baseEnumerator.Reset();
			}
		}

		/// <summary>
		/// Call the event handler to allow performing additional custom processes before
		/// inserting a new element into the collection
		/// </summary>
		/// <param name="index">
		/// The zero-based index at which <paramref name="value"/> can be found.
		/// </param>
		/// <param name="value">
		/// The value of the element to remove from <paramref name="index"/>.
		/// </param>
		protected override void OnInsert(int index, object value)
		{
			if (BeforeInsert != null)
				BeforeInsert(index, value);
		}

		/// <summary>
		/// Call the event handler to allow performing additional custom processes after
		/// inserting the new element into the collection
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="value"/> can be found.</param>
		/// <param name="value">The value of the element to remove from <paramref name="index"/>.</param>
		protected override void OnInsertComplete(int index, object value)
		{
			if (AfterInsert != null)
				AfterInsert(index, value);
		}

		/// <summary>
		/// Call the event handler to allow performing additional custom processes before
		/// removing an element from the collection
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="value"/> can be found.</param>
		/// <param name="value">The value of the element to remove from <paramref name="index"/>.</param>
		protected override void OnRemove(int index, object value)
		{
			if (BeforeRemove != null)
				BeforeRemove(index, value);
		}

		/// <summary>
		/// Call the event handler to allow performing additional custom processes after
		/// removing an element from the collection
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="value"/> can be found.</param>
		/// <param name="value">The value of the element to remove from <paramref name="index"/>.</param>
		protected override void OnRemoveComplete(int index, object value)
		{
			if (AfterRemove != null)
				AfterRemove(index, value);
		}

		/// <summary>
		/// Call the event handler to allow performing additional custom processes before
		/// removing all elements from the collection
		/// </summary>
		protected override void OnClear()
		{
			if (BeforeClear != null)
				BeforeClear();
		}

		/// <summary>
		/// Call the event handler to allow performing additional custom processes after
		/// removing all elements from the collection
		/// </summary>
		protected override void OnClearComplete()
		{
			if (AfterClear != null)
				AfterClear();
		}
	}
}
