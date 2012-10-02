//-------------------------------------------------------------------------------------------------
// <copyright file="ImportStreamCollection.cs" company="Microsoft">
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
// Hash table collection of import streams.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Hash table collection of import streams.
	/// </summary>
	public class ImportStreamCollection : HashCollectionBase
	{
		/// <summary>
		/// Creates a new collection.
		/// </summary>
		public ImportStreamCollection()
		{
		}

		/// <summary>
		/// Gets an import stream by name.
		/// </summary>
		/// <param name="importStreamName">Name of stream to get.</param>
		public ImportStream this[string importStreamName]
		{
			get { return (ImportStream)this.collection[importStreamName]; }
		}

		/// <summary>
		/// Adds an import stream to the collection.
		/// </summary>
		/// <param name="importStream">Import stream to add to collection.</param>
		/// <remarks>Indexes collection by import stream name.</remarks>
		public void Add(ImportStream importStream)
		{
			if (null == importStream)
			{
				throw new ArgumentNullException("importStream");
			}

			this.collection.Add(importStream.Name, importStream);
		}
	}
}
