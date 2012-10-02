//-------------------------------------------------------------------------------------------------
// <copyright file="ReferenceCollection.cs" company="Microsoft">
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
// A sorted collection of unique references.
// </summary>
//-------------------------------------------------------------------------------------------------
namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;

	/// <summary>
	/// A sorted collection of unique references.
	/// </summary>
	public class ReferenceCollection : ICollection, IEnumerable
	{
		private SortedList collection;

		/// <summary>
		/// Instantiate a new ReferenceCollection.
		/// </summary>
		public ReferenceCollection()
		{
			this.collection = new SortedList();
		}

		/// <summary>
		/// Gets the number of elements actually contained in the ReferenceCollection.
		/// </summary>
		/// <value>The number of elements actually contained in the ReferenceCollection.</value>
		public int Count
		{
			get { return this.collection.Count; }
		}

		/// <summary>
		/// Gets a value indicating whether access to the ReferenceCollection is synchronized (thread-safe).
		/// </summary>
		/// <value>true if access to the ReferenceCollection is synchronized (thread-safe); otherwise, false. The default is false.</value>
		public bool IsSynchronized
		{
			get { return this.collection.IsSynchronized; }
		}

		/// <summary>
		/// Gets an object that can be used to synchronize access to the ReferenceCollection.
		/// </summary>
		/// <value>An object that can be used to synchronize access to the ReferenceCollection.</value>
		public object SyncRoot
		{
			get { return this.collection.SyncRoot; }
		}

		/// <summary>
		/// Add a reference to this ReferenceCollection.
		/// </summary>
		/// <param name="reference">Reference to add to the ReferenceCollection.</param>
		public void Add(Reference reference)
		{
			this.collection[reference] = null;
		}

		/// <summary>
		/// Copies the entire ReferenceCollection to a compatible one-dimensional Array, starting at the specified index of the target array.
		/// </summary>
		/// <param name="array">The one-dimensional Array that is the destination of the elements copied from this ReferenceCollection. The Array must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
		public void CopyTo(System.Array array, int arrayIndex)
		{
			this.collection.Keys.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Determines whether a Reference is in the ReferenceCollection.
		/// </summary>
		/// <param name="item">The Object to locate in the ReferenceCollection. The value can be a null reference.</param>
		/// <returns>true if item is found in the ReferenceCollection; otherwise, false.</returns>
		public bool Contains(object item)
		{
			return this.collection.Contains(item);
		}

		/// <summary>
		/// Returns an enumerator for the entire ReferenceCollection.
		/// </summary>
		/// <returns>An IEnumerator for the entire ReferenceCollection.</returns>
		public virtual IEnumerator GetEnumerator()
		{
			return this.collection.Keys.GetEnumerator();
		}
	}
}
