//-------------------------------------------------------------------------------------------------
// <copyright file="FileMediaInformationCollection.cs" company="Microsoft">
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
// Collection of file media information objects.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Collections;

	/// <summary>
	/// Collection of file media information objects.
	/// </summary>
	public class FileMediaInformationCollection : ICollection
	{
		private Hashtable hashTable;
		private ArrayList arrayList;

		/// <summary>
		/// Creates a new collection.
		/// </summary>
		public FileMediaInformationCollection()
		{
			this.hashTable = new Hashtable();
			this.arrayList = new ArrayList();
		}

		/// <summary>
		/// Gets the number of elements in the collection.
		/// </summary>
		/// <value>Number of elements in collection.</value>
		public int Count
		{
			get { return this.hashTable.Count; }
		}

		/// <summary>
		/// Gets if this collection has been synchronized.
		/// </summary>
		/// <value>true if collection has been synchronized.</value>
		public bool IsSynchronized
		{
			get { return this.hashTable.IsSynchronized && this.arrayList.IsSynchronized; }
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
		/// Get the FileMediaInformation for the given File Id.
		/// </summary>
		/// <param name="fileId">File Id of the row to get.</param>
		/// <value>FileMediaInformation for the given File Id.</value>
		public FileMediaInformation this[string fileId]
		{
			get { return (FileMediaInformation)this.hashTable[fileId]; }
		}

		/// <summary>
		/// Adds a file media information object to collection.
		/// </summary>
		/// <param name="fileMediaInfo">File media information to add to the collection.</param>
		public void Add(FileMediaInformation fileMediaInfo)
		{
			if (null == fileMediaInfo)
			{
				throw new ArgumentNullException("fileMediaInfo");
			}

			this.hashTable.Add(fileMediaInfo.FileId, fileMediaInfo);
			this.arrayList.Add(fileMediaInfo);
		}

		/// <summary>
		/// Copies collection to array.
		/// </summary>
		/// <param name="array">Array to copy collection into.</param>
		/// <param name="index">Index to start copying at.</param>
		public void CopyTo(System.Array array, int index)
		{
			this.arrayList.CopyTo(array, index);
		}

		/// <summary>
		/// Gets an enumerator for the collection.
		/// </summary>
		/// <returns>Enumerator for collection.</returns>
		public virtual IEnumerator GetEnumerator()
		{
			return this.arrayList.GetEnumerator();
		}

		/// <summary>
		/// Sorts the elements in the collection.
		/// </summary>
		public void Sort()
		{
			this.arrayList.Sort();
		}
	}
}