//-------------------------------------------------------------------------------------------------
// <copyright file="OutputRowCollection.cs" company="Microsoft">
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
// Array collection of output rows.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Array collection of output rows.
	/// </summary>
	public class OutputRowCollection : ArrayCollectionBase
	{
		/// <summary>
		/// Creates a new collection.
		/// </summary>
		public OutputRowCollection()
		{
		}

		/// <summary>
		/// Adds an output row to the collection.
		/// </summary>
		/// <param name="row">Row to add to the collection.</param>
		public void Add(OutputRow row)
		{
			this.collection.Add(row);
		}

		/// <summary>
		/// Adds a collection of output rows to this collection.
		/// </summary>
		/// <param name="rows">Collection of rows to add to this collection.</param>
		/// <remarks>This method does a shallow copy.</remarks>
		public void Add(OutputRowCollection rows)
		{
			foreach (OutputRow row in rows)
			{
				this.collection.Add(row);
			}
		}
	}
}
