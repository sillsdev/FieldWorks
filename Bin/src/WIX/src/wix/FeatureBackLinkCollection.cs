//-------------------------------------------------------------------------------------------------
// <copyright file="FeatureBacklinkCollection.cs" company="Microsoft">
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
// Array based collection of feature backlinks.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Array based collection of feature backlinks.
	/// </summary>
	public sealed class FeatureBacklinkCollection : ArrayCollectionBase
	{
		/// <summary>
		/// Adds a backlink to the collection.
		/// </summary>
		/// <param name="backlink">Backlink to add to the collection.</param>
		public void Add(FeatureBacklink backlink)
		{
			this.collection.Add(backlink);
		}
	}
}
