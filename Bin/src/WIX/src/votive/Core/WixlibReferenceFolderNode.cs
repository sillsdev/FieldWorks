//-------------------------------------------------------------------------------------------------
// <copyright file="WixlibReferenceFolderNode.cs" company="Microsoft">
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
// A wixlib reference folder node within a Solution Explorer hierarchy.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using System.Drawing;
	using System.IO;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;

	/// <summary>
	/// Represents the root of the wixlib references in the Solution Explorer hierarchy.
	/// </summary>
	internal sealed class WixlibReferenceFolderNode : ReferenceFolderNode
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(WixlibReferenceFolderNode);
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="WixlibReferenceFolderNode"/> class.
		/// </summary>
		/// <param name="hierarchy">The parent <see cref="Hierarchy"/> object.</param>
		/// <param name="rootDirectory">The absolute path to the folder.</param>
		public WixlibReferenceFolderNode(Hierarchy hierarchy, string rootDirectory)
			: base(hierarchy, rootDirectory, WixStrings.WixlibReferenceFolderCaption)
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
