//-------------------------------------------------------------------------------------------------
// <copyright file="TableCollection.cs" company="Microsoft">
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
// Hash table collection for tables.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Hash table collection for tables.
	/// </summary>
	public class TableCollection : HashCollectionBase
	{
		/// <summary>
		/// Gets a table by name.
		/// </summary>
		/// <param name="tableName">Name of table to locate.</param>
		public Table this[string tableName]
		{
			get { return (Table)this.collection[tableName]; }
		}

		/// <summary>
		/// Adds a table to the collection.
		/// </summary>
		/// <param name="table">Table to add to the collection.</param>
		/// <remarks>Indexes the table by name.</remarks>
		public void Add(Table table)
		{
			if (null == table)
			{
				throw new ArgumentNullException("table");
			}

			this.collection.Add(table.Name, table);
		}
	}
}
