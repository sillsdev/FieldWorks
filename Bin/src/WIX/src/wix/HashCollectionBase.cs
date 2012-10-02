//-------------------------------------------------------------------------------------------------
// <copyright file="HashCollectionBase.cs" company="Microsoft">
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
// Abstract base class for all hash collections.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;

	/// <summary>
	/// Base class for collections.
	/// Simply provides the default implementation for ICollection, and IEnumerable
	/// This class is not intended for use by users of the library.
	/// </summary>
	public abstract class HashCollectionBase : ICollection, IEnumerable
	{
		protected Hashtable collection;

		/// <summary>
		/// Creates a new base hash collection.
		/// </summary>
		protected HashCollectionBase()
		{
			this.collection = new Hashtable();
		}

		/// <summary>
		/// Creates a new base hash with the specified collection.
		/// </summary>
		/// <param name="collection">Collection to use at the core of this hash collection.</param>
		protected HashCollectionBase(Hashtable collection)
		{
			this.collection = collection;
		}

		/// <summary>
		/// Gets the number of elements in the collection.
		/// </summary>
		/// <value>Number of elements in collection.</value>
		public int Count
		{
			get { return this.collection.Count; }
		}

		/// <summary>
		/// Gets the keys of the hash table.
		/// </summary>
		/// <value>Collection of keys.</value>
		public ICollection Keys
		{
			get { return this.collection.Keys; }
		}

		/// <summary>
		/// Gets if this collection has been synchronized.
		/// </summary>
		/// <value>true if collection has been synchronized.</value>
		public bool IsSynchronized
		{
			get { return this.collection.IsSynchronized; }
		}

		/// <summary>
		/// Gets the synchronization object for this collection.
		/// </summary>
		/// <value>Object for synchronization.</value>
		public object SyncRoot
		{
			get { return this; }
		}

		/// <summary>
		/// Checks if collection contains object.
		/// </summary>
		/// <param name="item">Item to check in collection.</param>
		/// <returns>true if collection contains item.</returns>
		public bool Contains(object item)
		{
			return this.collection.Contains(item);
		}

		/// <summary>
		/// Copies collection to array.
		/// </summary>
		/// <param name="array">Array to copy collection into.</param>
		/// <param name="index">Index to start copying at.</param>
		public void CopyTo(System.Array array, int index)
		{
			this.collection.CopyTo(array, index);
		}

		/// <summary>
		/// Gets an enumerator for the collection.
		/// </summary>
		/// <returns>Enumerator for collection.</returns>
		public virtual IEnumerator GetEnumerator()
		{
			return this.collection.Values.GetEnumerator();
		}
	}
}
