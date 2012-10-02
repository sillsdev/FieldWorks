// ------------------------------------------------------------------------------
// <copyright from='1997' to='2001' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright>
// ------------------------------------------------------------------------------
//
namespace MySpace {
	using System;
	using System.Collections;
	using YourSpace;


	/// <summary>
	///     <para>
	///       A collection that stores <see cref='MySpace.Customer'/> objects.
	///    </para>
	/// </summary>
	/// <seealso cref='MySpace.CustomerCollection'/>
	[Serializable()]
	public class CustomerCollection : CollectionBase {

		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='MySpace.CustomerCollection'/>.
		///    </para>
		/// </summary>
		public CustomerCollection() {
		}

		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='MySpace.CustomerCollection'/> based on another <see cref='MySpace.CustomerCollection'/>.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A <see cref='MySpace.CustomerCollection'/> from which the contents are copied
		/// </param>
		public CustomerCollection(CustomerCollection value) {
			this.AddRange(value);
		}

		/// <summary>
		///     <para>
		///       Initializes a new instance of <see cref='MySpace.CustomerCollection'/> containing any array of <see cref='MySpace.Customer'/> objects.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///       A array of <see cref='MySpace.Customer'/> objects with which to intialize the collection
		/// </param>
		public CustomerCollection(Customer[] value) {
			this.AddRange(value);
		}

		/// <summary>
		/// <para>Represents the entry at the specified index of the <see cref='MySpace.Customer'/>.</para>
		/// </summary>
		/// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
		/// <value>
		///    <para> The entry at the specified index of the collection.</para>
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		public Customer this[int index] {
			get {
				return ((Customer)(List[index]));
			}
			set {
				List[index] = value;
			}
		}

		/// <summary>
		///    <para>Adds a <see cref='MySpace.Customer'/> with the specified value to the
		///    <see cref='MySpace.CustomerCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='MySpace.Customer'/> to add.</param>
		/// <returns>
		///    <para>The index at which the new element was inserted.</para>
		/// </returns>
		/// <seealso cref='MySpace.CustomerCollection.AddRange'/>
		public int Add(Customer value) {
			return List.Add(value);
		}

		/// <summary>
		/// <para>Copies the elements of an array to the end of the <see cref='MySpace.CustomerCollection'/>.</para>
		/// </summary>
		/// <param name='value'>
		///    An array of type <see cref='MySpace.Customer'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='MySpace.CustomerCollection.Add'/>
		public void AddRange(Customer[] value) {
			for (int i = 0; (i < value.Length); i = (i + 1)) {
				this.Add(value[i]);
			}
		}

		/// <summary>
		///     <para>
		///       Adds the contents of another <see cref='MySpace.CustomerCollection'/> to the end of the collection.
		///    </para>
		/// </summary>
		/// <param name='value'>
		///    A <see cref='MySpace.CustomerCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <seealso cref='MySpace.CustomerCollection.Add'/>
		public void AddRange(CustomerCollection value) {
			for (int i = 0; (i < value.Count); i = (i + 1)) {
				this.Add(value[i]);
			}
		}

		/// <summary>
		/// <para>Gets a value indicating whether the
		///    <see cref='MySpace.CustomerCollection'/> contains the specified <see cref='MySpace.Customer'/>.</para>
		/// </summary>
		/// <param name='value'>The <see cref='MySpace.Customer'/> to locate.</param>
		/// <returns>
		/// <para><see langword='true'/> if the <see cref='MySpace.Customer'/> is contained in the collection;
		///   otherwise, <see langword='false'/>.</para>
		/// </returns>
		/// <seealso cref='MySpace.CustomerCollection.IndexOf'/>
		public bool Contains(Customer value) {
			return List.Contains(value);
		}

		/// <summary>
		/// <para>Copies the <see cref='MySpace.CustomerCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		///    specified index.</para>
		/// </summary>
		/// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='MySpace.CustomerCollection'/> .</para></param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <returns>
		///   <para>None.</para>
		/// </returns>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='MySpace.CustomerCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		public void CopyTo(Customer[] array, int index) {
			List.CopyTo(array, index);
		}

		/// <summary>
		///    <para>Returns the index of a <see cref='MySpace.Customer'/> in
		///       the <see cref='MySpace.CustomerCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='MySpace.Customer'/> to locate.</param>
		/// <returns>
		/// <para>The index of the <see cref='MySpace.Customer'/> of <paramref name='value'/> in the
		/// <see cref='MySpace.CustomerCollection'/>, if found; otherwise, -1.</para>
		/// </returns>
		/// <seealso cref='MySpace.CustomerCollection.Contains'/>
		public int IndexOf(Customer value) {
			return List.IndexOf(value);
		}

		/// <summary>
		/// <para>Inserts a <see cref='MySpace.Customer'/> into the <see cref='MySpace.CustomerCollection'/> at the specified index.</para>
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='MySpace.Customer'/> to insert.</param>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='MySpace.CustomerCollection.Add'/>
		public void Insert(int index, Customer value) {
			List.Insert(index, value);
		}

		/// <summary>
		///    <para>Returns an enumerator that can iterate through
		///       the <see cref='MySpace.CustomerCollection'/> .</para>
		/// </summary>
		/// <returns><para>None.</para></returns>
		/// <seealso cref='System.Collections.IEnumerator'/>
		public new CustomerEnumerator GetEnumerator() {
			return new CustomerEnumerator(this);
		}

		/// <summary>
		///    <para> Removes a specific <see cref='MySpace.Customer'/> from the
		///    <see cref='MySpace.CustomerCollection'/> .</para>
		/// </summary>
		/// <param name='value'>The <see cref='MySpace.Customer'/> to remove from the <see cref='MySpace.CustomerCollection'/> .</param>
		/// <returns><para>None.</para></returns>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		public void Remove(Customer value) {
			List.Remove(value);
		}

		public class CustomerEnumerator : object, IEnumerator {

			private IEnumerator baseEnumerator;

			private IEnumerable temp;

			public CustomerEnumerator(CustomerCollection mappings) {
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}

			public Customer Current {
				get {
					return ((Customer)(baseEnumerator.Current));
				}
			}

			object IEnumerator.Current {
				get {
					return baseEnumerator.Current;
				}
			}

			public bool MoveNext() {
				return baseEnumerator.MoveNext();
			}

			bool IEnumerator.MoveNext() {
				return baseEnumerator.MoveNext();
			}

			public void Reset() {
				baseEnumerator.Reset();
			}

			void IEnumerator.Reset() {
				baseEnumerator.Reset();
			}
		}
	}
}
