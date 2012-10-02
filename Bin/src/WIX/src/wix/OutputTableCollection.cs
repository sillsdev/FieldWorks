//-------------------------------------------------------------------------------------------------
// <copyright file="OutputTableCollection.cs" company="Microsoft">
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
//     Hash table collection of output tables.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Hash table collection of output tables.
	/// </summary>
	public class OutputTableCollection : HashCollectionBase
	{
		/// <summary>
		/// Gets an output table by name.
		/// </summary>
		/// <param name="tableName">Table name to find.</param>
		public OutputTable this[string tableName]
		{
			get { return (OutputTable)this.collection[tableName]; }
		}

		/// <summary>
		/// Adds an output table to the collection
		/// </summary>
		/// <param name="table">Table to add.</param>
		public void Add(OutputTable table)
		{
			if (null == table)
			{
				throw new ArgumentNullException("table");
			}

			this.collection.Add(table.Name, table);
		}
	}
}
