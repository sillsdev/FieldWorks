//-------------------------------------------------------------------------------------------------
// <copyright file="ISchemaElement.cs" company="Microsoft">
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
// Interface for generated schema elements.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Serialize
{
	using System;
	using System.Xml;

	/// <summary>
	/// Interface for generated schema elements.
	/// </summary>
	public interface ISchemaElement
	{
		/// <summary>
		/// Outputs xml representing this element, including the associated attributes
		/// and any nested elements.
		/// </summary>
		/// <param name="writer">XmlTextWriter to be used when outputting the element.</param>
		void OutputXml(XmlTextWriter writer);
	}
}
