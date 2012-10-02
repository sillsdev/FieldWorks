//-------------------------------------------------------------------------------------------------
// <copyright file="RowCollection.cs" company="Microsoft">
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
// Array collection of rows.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Array collection of rows.
	/// </summary>
	public class RowCollection : ArrayCollectionBase
	{
		/// <summary>
		/// Adds a row to the collection.
		/// </summary>
		/// <param name="row">Row to add to collection.</param>
		public void Add(Row row)
		{
			this.collection.Add(row);
		}
	}
}
