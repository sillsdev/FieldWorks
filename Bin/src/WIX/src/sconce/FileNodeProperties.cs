//--------------------------------------------------------------------------------------------------
// <copyright file="FileNodeProperties.cs" company="Microsoft">
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
// Properties for a file node within a Solution Explorer hierarchy.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.ComponentModel;
	using System.Runtime.InteropServices;

	/// <summary>
	/// Represents properties for a file node in a Solution Explorer hierarchy that will be shown
	/// in the Properties window.
	/// </summary>
	public class FileNodeProperties : NodeProperties
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
		/// Initializes a new instance of the <see cref="FileNodeProperties"/> class.
		/// </summary>
		public FileNodeProperties(FileNode node) : base(node)
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets or sets the build action for the attached node.
		/// </summary>
		[LocalizedCategory(SconceStrings.StringId.CategoryAdvanced)]
		[LocalizedDescription(SconceStrings.StringId.FilePropertiesBuildActionDescription)]
		[LocalizedDisplayName(SconceStrings.StringId.FilePropertiesBuildAction)]
		public BuildAction BuildAction
		{
			get { return this.Node.BuildAction; }
			set { this.Node.BuildAction = value; }
		}

		/// <summary>
		/// Gets or sets the file name (caption) of the attached node.
		/// </summary>
		[LocalizedCategory(SconceStrings.StringId.CategoryMisc)]
		[LocalizedDescription(SconceStrings.StringId.FilePropertiesFileNameDescription)]
		[LocalizedDisplayName(SconceStrings.StringId.FilePropertiesFileName)]
		public string FileName
		{
			get { return this.Node.Caption; }
			set { this.Node.SetCaption(value); }
		}

		/// <summary>
		/// Gets the full path to the file.
		/// </summary>
		[LocalizedCategory(SconceStrings.StringId.CategoryMisc)]
		[LocalizedDescription(SconceStrings.StringId.FilePropertiesFullPathDescription)]
		[LocalizedDisplayName(SconceStrings.StringId.FilePropertiesFullPath)]
		public string FullPath
		{
			get { return this.Node.AbsolutePath; }
		}

		/// <summary>
		/// Gets the <see cref="FileNode"/> that this properties object is attached to.
		/// </summary>
		[Browsable(false)]
		public new FileNode Node
		{
			get { return (FileNode)base.Node; }
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
			return SconceStrings.FilePropertiesClassName;
		}
		#endregion
	}
}
