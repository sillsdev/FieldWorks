//-------------------------------------------------------------------------------------------------
// <copyright file="SectionCollection.cs" company="Microsoft">
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
// Array collection of sections.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;

	/// <summary>
	/// Array collection of sections.
	/// </summary>
	public class SectionCollection : ArrayCollectionBase
	{
		/// <summary>
		/// Adds a section to the collection.
		/// </summary>
		/// <param name="section">Section to add to collection.</param>
		public void Add(Section section)
		{
			this.collection.Add(section);
		}
	}
}
