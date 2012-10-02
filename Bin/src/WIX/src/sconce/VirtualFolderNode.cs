//-------------------------------------------------------------------------------------------------
// <copyright file="VirtualFolderNode.cs" company="Microsoft">
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
// A virtual folder node within a Solution Explorer hierarchy.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Represents a virtual folder within a Solution Explorer hierarchy, virtual in the sense that
	/// there is not an underlying OS folder.
	/// </summary>
	public class VirtualFolderNode : FolderNode
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(VirtualFolderNode);
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public VirtualFolderNode(Hierarchy hierarchy, string absolutePath) : base(hierarchy, absolutePath)
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		public override bool IsVirtual
		{
			get { return true; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		#endregion
	}
}
