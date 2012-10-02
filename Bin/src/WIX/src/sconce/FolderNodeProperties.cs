//--------------------------------------------------------------------------------------------------
// <copyright file="FolderNodeProperties.cs" company="Microsoft">
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
// Properties for a folder node within a Solution Explorer hierarchy.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.ComponentModel;
	using System.Runtime.InteropServices;

	/// <summary>
	/// Represents properties for a folder node in a Solution Explorer hierarchy
	/// that will be shown in the Properties window.
	/// </summary>
	public class FolderNodeProperties : NodeProperties
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
		/// Initializes a new instance of the <see cref="FolderNodeProperties"/> class.
		/// </summary>
		public FolderNodeProperties(FolderNode node) : base(node)
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets or sets the folder name (caption) of the attached node.
		/// </summary>
		[LocalizedCategory(SconceStrings.StringId.CategoryMisc)]
		[LocalizedDescription(SconceStrings.StringId.FolderPropertiesFolderNameDescription)]
		[LocalizedDisplayName(SconceStrings.StringId.FolderPropertiesFolderName)]
		public string FolderName
		{
			get { return this.Node.Caption; }
			set { this.Node.SetCaption(value); }
		}

		/// <summary>
		/// Gets the <see cref="FolderNode"/> that this properties object is attached to.
		/// </summary>
		public new FolderNode Node
		{
			get { return (FolderNode)base.Node; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Returns the name that is displayed in the right hand side of the Properties window drop-down combo box.
		/// </summary>
		/// <returns>The class name of the object, or null if the class does not have a name.</returns>
		public override string GetClassName()
		{
			return SconceStrings.FolderPropertiesClassName;
		}
		#endregion
	}
}
