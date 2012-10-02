//-------------------------------------------------------------------------------------------------
// <copyright file="ConnectToFeatureCollection.cs" company="Microsoft">
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
// Hash collection of connect to feature objects.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Hash collection of connect to feature objects.
	/// </summary>
	public class ConnectToFeatureCollection : HashCollectionBase
	{
		/// <summary>
		/// Gets a feature connection by child id.
		/// </summary>
		/// <param name="childId">Identifier of child to locate.</param>
		public ConnectToFeature this[string childId]
		{
			get { return (ConnectToFeature)this.collection[childId]; }
		}

		/// <summary>
		/// Adds a feature connection to the collection.
		/// </summary>
		/// <param name="connection">Feature connection to add.</param>
		public void Add(ConnectToFeature connection)
		{
			if (null == connection)
			{
				throw new ArgumentNullException("connection");
			}

			this.collection.Add(connection.ChildId, connection);
		}
	}
}
