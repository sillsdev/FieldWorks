//-------------------------------------------------------------------------------------------------
// <copyright file="ArrayCollectionBase.cs" company="Microsoft">
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
// Abstract base class for all array base collections.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;

	/// <summary>
	/// Abstract base class for array based collections and simply provides
	/// the default implementation for ICollection, and IEnumerable
	/// </summary>
	public abstract class ArrayCollectionBase : ICollection, IEnumerable
	{
		protected ArrayList collection;

		/// <summary>
		/// Creates a new base collection.
		/// </summary>
		protected ArrayCollectionBase()
		{
			this.collection = new ArrayList();
		}

		/// <summary>
		/// Gets the number of items in the collection.
		/// </summary>
		/// <value>Number of items in collection.</value>
		public int Count
		{
			get { return this.collection.Count; }
		}

		/// <summary>
		/// Gets if the collection has been synchronized.
		/// </summary>
		/// <value>True if the collection has been synchronized.</value>
		public bool IsSynchronized
		{
			get { return this.collection.IsSynchronized; }
		}

		/// <summary>
		/// Gets the object used to synchronize the collection.
		/// </summary>
		/// <value>Oject used the synchronize the collection.</value>
		public object SyncRoot
		{
			get { return this; }
		}

		/// <summary>
		/// Copies the collection into an array.
		/// </summary>
		/// <param name="array">Array to copy the collection into.</param>
		/// <param name="index">Index to start copying from.</param>
		public void CopyTo(System.Array array, int index)
		{
			this.collection.CopyTo(array, index);
		}

		/// <summary>
		/// Checks if the collection contains item.
		/// </summary>
		/// <param name="item">Item to check in collection.</param>
		/// <returns>True if collection contains item.</returns>
		public bool Contains(object item)
		{
			return this.collection.Contains(item);
		}

		/// <summary>
		/// Gets enumerator for the collection.
		/// </summary>
		/// <returns>Enumerator for the collection.</returns>
		public virtual IEnumerator GetEnumerator()
		{
			return this.collection.GetEnumerator();
		}
	}
}
