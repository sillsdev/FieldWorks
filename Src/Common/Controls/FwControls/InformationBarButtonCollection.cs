// ------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: InformationBarButtonCollection.cs
// Responsibility: ToddJ
// Last reviewed:
//
// <remarks>Implementation of strongly-typed collection InformationBarButtonCollection</remarks>
// ------------------------------------------------------------------------------
//
namespace SIL.FieldWorks.Common.Controls
{
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.ComponentModel.Design;
	using System.Drawing.Design;
	using System.Windows.Forms;


	/// <summary>
	///     <para>
	///       A collection that stores <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/>
	[ToolboxItem(false)]
	[Serializable()]
	[Editor("System.ComponentModel.Design.CollectionEditor", "System.Drawing.Design.UITypeEditor")]
	public class InformationBarButtonCollection : CollectionBase
	{
		/// <summary>Event signatures</summary>
		public delegate void CollectionChange(int index, object value);

		/// <summary>Event that occures when the button is about to be inserted.</summary>
		public event CollectionChange BeforeInsert;
		/// <summary>
		/// Event that occures after button is inserted
		/// </summary>
		public event CollectionChange AfterInsert;

		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/>.
		///    </para>
		/// </summary>
		public InformationBarButtonCollection()
		{
		}

		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/> based on another <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/> from which the contents are copied
		/// </param>
		public InformationBarButtonCollection(InformationBarButtonCollection value)
		{
			this.AddRange(value);
		}

		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/> containing any array of <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/> objects with which to intialize the collection
		/// </param>
		public InformationBarButtonCollection(InformationBarButton[] value)
		{
			this.AddRange(value);
		}

		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='T:System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public InformationBarButton this[int index]
		{
			get
			{
				return ((InformationBarButton)(List[index]));
			}
			set
			{
				List[index] = value;
			}
		}

		/// <summary>
		///    <para>Adds a <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/> with the specified value to the
		///    <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection.AddRange(SIL.FieldWorks.Common.Controls.InformationBarButton[])'/>
		public int Add(InformationBarButton value)
		{
			return List.Add(value);
		}

		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection.Add'/>
		public void AddRange(InformationBarButton[] value)
		{
			for (int i = 0; (i < value.Length); i = (i + 1))
			{
				this.Add(value[i]);
			}
		}

		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection.Add'/>
		public void AddRange(InformationBarButtonCollection value)
		{
			for (int i = 0; (i < value.Count); i = (i + 1))
			{
				this.Add(value[i]);
			}
		}

		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/> contains the specified <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection.IndexOf'/>
		public bool Contains(InformationBarButton value)
		{
			return List.Contains(value);
		}

		/// <summary>
		/// <para>Copies the <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/> values to a one-dimensional <see cref='T:System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='T:System.Array'/> that is the destination of the values copied from <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='T:System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para>
		///		<para>-or-</para>
		///		<para>The number of elements in the <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/> is greater than the available space between <paramref name='index'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='T:System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='T:System.ArgumentOutOfRangeException'><paramref name='index'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='T:System.Array'/>
		public void CopyTo(InformationBarButton[] array, int index)
		{
			List.CopyTo(array, index);
		}

		/// <summary>
		///    <para>Returns the index of a <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/> in
		///       the <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/> of <paramref name='value'/> in the
		/// <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection.Contains'/>
		public int IndexOf(InformationBarButton value)
		{
			return List.IndexOf(value);
		}

		/// <summary>
		/// <para>Inserts a <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/> into the <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name='value'>The <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection.Add'/>
		public void Insert(int index, InformationBarButton value)
		{
			List.Insert(index, value);
		}

		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='T:System.Collections.IEnumerator'/>
		public new InformationBarButtonEnumerator GetEnumerator()
		{
			return new InformationBarButtonEnumerator(this);
		}

		/// <summary>
		///    <para> Removes a specific <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/> from the
		///    <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButton'/> to remove from the <see cref='T:SIL.FieldWorks.Common.Controls.InformationBarButtonCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='T:System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(InformationBarButton value)
		{
			List.Remove(value);
		}

		/// <summary>
		///
		/// </summary>
		public class InformationBarButtonEnumerator : object, IEnumerator
		{

			private IEnumerator baseEnumerator;

			private IEnumerable temp;

			/// <summary>
			///
			/// </summary>
			/// <param name="mappings"></param>
			public InformationBarButtonEnumerator(InformationBarButtonCollection mappings)
			{
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}

			/// <summary>
			///
			/// </summary>
			public InformationBarButton Current
			{
				get
				{
					return ((InformationBarButton)(baseEnumerator.Current));
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
			///
			/// </summary>
			/// <returns></returns>
			public bool MoveNext()
			{
				return baseEnumerator.MoveNext();
			}

			bool IEnumerator.MoveNext()
			{
				return baseEnumerator.MoveNext();
			}

			/// <summary>
			///
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
		protected override void OnInsert(int index, object value)
		{
			if (BeforeInsert != null)
				BeforeInsert(index, value);
		}

		/// <summary>
		/// Call the event handler to allow performing additional custom processes after
		/// inserting the new element into the collection
		/// </summary>
		protected override void OnInsertComplete(int index, object value)
		{
			if (AfterInsert != null)
				AfterInsert(index, value);
		}
	}
}
