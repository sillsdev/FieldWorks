//-------------------------------------------------------------------------------------------------
// <copyright file="IntermediateCollection.cs" company="Microsoft">
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
// Container class for a set of intermediates.
// </summary>
//-------------------------------------------------------------------------------------------------
namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Text;

	/// <summary>
	/// Collection of intermediates.
	/// </summary>
	public class IntermediateCollection : ArrayCollectionBase
	{
		/// <summary>
		/// Add a new intermediate to the collection.
		/// </summary>
		/// <param name="intermediate">Intermediate to add to the collection.</param>
		public void Add(Intermediate intermediate)
		{
			collection.Add(intermediate);
		}
	}
}
