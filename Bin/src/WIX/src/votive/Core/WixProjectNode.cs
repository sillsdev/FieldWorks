//-------------------------------------------------------------------------------------------------
// <copyright file="WixProjectNode.cs" company="Microsoft">
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
// The WiX root node within a Solution Explorer hierarchy.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using System.Drawing;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;

	/// <summary>
	/// Represents the root node of a WiX project within a Solution Explorer hierarchy.
	/// </summary>
	internal sealed class WixProjectNode : ProjectNode
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="WixProjectNode"/> class.
		/// </summary>
		/// <param name="hierarchy">The parent <see cref="Hierarchy"/> object.</param>
		public WixProjectNode(Hierarchy hierarchy) : base(hierarchy)
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the image for the project node when the project is available (the normal image).
		/// </summary>
		public override Image ProjectImage
		{
			get { return WixHierarchyImages.Project; }
		}

		/// <summary>
		/// Gets the image for the project node when the project is unavailable (the dimmed image).
		/// </summary>
		public override Image UnavailableImage
		{
			get { return WixHierarchyImages.UnavailableProject; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Creates a new <see cref="ReferenceFolderNode"/>. Allows subclasses to create
		/// type-specific library folder nodes.
		/// </summary>
		/// <returns>A new <see cref="ReferenceFolderNode"/> object.</returns>
		protected override ReferenceFolderNode CreateReferenceFolderNode()
		{
			return new WixlibReferenceFolderNode(this.Hierarchy, this.AbsoluteDirectory);
		}
		#endregion
	}
}
