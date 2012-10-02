//-------------------------------------------------------------------------------------------------
// <copyright file="MediaRowCollection.cs" company="Microsoft">
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
// Hash table collection of specialized media rows.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Hash table collection of specialized media rows.
	/// </summary>
	public class MediaRowCollection : HashCollectionBase
	{
		/// <summary>
		/// Creates a new collection.
		/// </summary>
		public MediaRowCollection()
		{
		}

		/// <summary>
		/// Gets a media row by disk id.
		/// </summary>
		/// <param name="diskId">Disk identifier of media row to locate.</param>
		public MediaRow this[int diskId]
		{
			get { return (MediaRow)this.collection[diskId]; }
		}

		/// <summary>
		/// Adds a media row to the collection.
		/// </summary>
		/// <param name="row">Row to add to the colleciton.</param>
		/// <remarks>Indexes the row by disk id.</remarks>
		public void Add(MediaRow row)
		{
			if (null == row)
			{
				throw new ArgumentNullException("row");
			}

			this.collection.Add(row.DiskId, row);
		}
	}
}
