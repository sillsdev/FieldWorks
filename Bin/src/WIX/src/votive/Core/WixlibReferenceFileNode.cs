//-------------------------------------------------------------------------------------------------
// <copyright file="WixlibReferenceFileNode.cs" company="Microsoft">
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
// A wixlib file node within a Solution Explorer hierarchy.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;

	/// <summary>
	/// A WiX library (wixlib) file node within the Solution Explorer hierarchy.
	/// </summary>
	internal sealed class WixlibReferenceFileNode : ReferenceFileNode
	{
		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public WixlibReferenceFileNode(Hierarchy hierarchy, string absolutePath) : base(hierarchy, absolutePath)
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		#endregion
	}
}
