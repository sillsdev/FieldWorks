//--------------------------------------------------------------------------------------------------
// <copyright file="ProjectNodeProperties.cs" company="Microsoft">
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
// Properties for a root (project) node within a Solution Explorer hierarchy.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.ComponentModel;
	using System.Runtime.InteropServices;

	/// <summary>
	/// Represents properties for a root (project) node in a Solution Explorer hierarchy
	/// that will be shown in the Properties window.
	/// </summary>
	public class ProjectNodeProperties : NodeProperties
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
		/// Initializes a new instance of the <see cref="ProjectNodeProperties"/> class.
		/// </summary>
		public ProjectNodeProperties(ProjectNode node) : base(node)
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the name that is displayed on the property page dialog title bar and also on the
		/// Project/Properties menu item. This must be named "Name" for VS to use it.
		/// </summary>
		[Browsable(false)]
		public string Name
		{
			get { return this.Node.Caption; }
		}

		/// <summary>
		/// Gets the <see cref="ProjectNode"/> that this properties object is attached to.
		/// </summary>
		[Browsable(false)]
		public new ProjectNode Node
		{
			get { return (ProjectNode)base.Node; }
		}

		/// <summary>
		/// Gets or sets the file name (caption) of the attached node.
		/// </summary>
		[LocalizedCategory(SconceStrings.StringId.CategoryMisc)]
		[LocalizedDescription(SconceStrings.StringId.ProjectPropertiesProjectFileDescription)]
		[LocalizedDisplayName(SconceStrings.StringId.ProjectPropertiesProjectFile)]
		public string ProjectFile
		{
			get { return this.Node.Caption; }
			set { this.Node.SetCaption(value); }
		}

		/// <summary>
		/// Gets the full path to the file.
		/// </summary>
		[LocalizedCategory(SconceStrings.StringId.CategoryMisc)]
		[LocalizedDescription(SconceStrings.StringId.ProjectPropertiesProjectFolderDescription)]
		[LocalizedDisplayName(SconceStrings.StringId.ProjectPropertiesProjectFolder)]
		public string ProjectFolder
		{
			get { return this.Node.AbsoluteDirectory; }
		}

		/// <summary>
		/// Gets a value indicating whether selecting "Properties" from the context menu should
		/// trigger the property pages to be shown. Normally only the project node should return true.
		/// </summary>
		[Browsable(false)]
		protected override bool ShouldTriggerPropertyPages
		{
			get { return true; }
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
			return SconceStrings.ProjectPropertiesClassName;
		}
		#endregion
	}
}
