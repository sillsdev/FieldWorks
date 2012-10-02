//-------------------------------------------------------------------------------------------------
// <copyright file="ColumnDefinitionCollection.cs" company="Microsoft">
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
// Array collection of definitions.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Array collection of definitions.
	/// </summary>
	public class ColumnDefinitionCollection : ArrayCollectionBase
	{
		/// <summary>
		/// Gets a column definition by index.
		/// </summary>
		/// <param name="index">Index into array.</param>
		/// <value>Column definition at index location.</value>
		public ColumnDefinition this[int index]
		{
			get { return (ColumnDefinition)this.collection[index]; }
		}

		/// <summary>
		/// Adds a column definition to the collection.
		/// </summary>
		/// <param name="columnDefinition">Column definition to add to array.</param>
		public void Add(ColumnDefinition columnDefinition)
		{
			this.collection.Add(columnDefinition);
		}
	}
}
