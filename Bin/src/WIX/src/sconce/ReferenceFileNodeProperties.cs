//-------------------------------------------------------------------------------------------------
// <copyright file="ReferenceFileNodeProperties.cs" company="Microsoft">
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
// Properties for a library file node within a Solution Explorer hierarchy.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.ComponentModel;
	using System.Runtime.InteropServices;

	/// <summary>
	/// Represents properties for a library file node in a Solution Explorer hierarchy
	/// that will be shown in the Properties window.
	/// </summary>
	public class ReferenceFileNodeProperties : NodeProperties
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
		/// Initializes a new instance of the <see cref="ReferenceFileNodeProperties"/> class.
		/// </summary>
		public ReferenceFileNodeProperties(ReferenceFileNode node) : base(node)
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the name (caption) of the attached node.
		/// </summary>
		[LocalizedCategory(SconceStrings.StringId.CategoryMisc)]
		[LocalizedDescription(SconceStrings.StringId.ReferenceFilePropertiesNameDescription)]
		[LocalizedDisplayName(SconceStrings.StringId.ReferenceFilePropertiesName)]
		public string Name
		{
			get { return this.Node.Caption; }
		}

		/// <summary>
		/// Gets the <see cref="ReferenceFileNode"/> that this properties object is attached to.
		/// </summary>
		[Browsable(false)]
		public new ReferenceFileNode Node
		{
			get { return (ReferenceFileNode)base.Node; }
		}

		/// <summary>
		/// Gets the absolute path of the reference.
		/// </summary>
		[LocalizedCategory(SconceStrings.StringId.CategoryMisc)]
		[LocalizedDescription(SconceStrings.StringId.ReferenceFilePropertiesPathDescription)]
		[LocalizedDisplayName(SconceStrings.StringId.ReferenceFilePropertiesPath)]
		public string Path
		{
			get { return this.Node.AbsolutePath; }
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
			return SconceStrings.ReferenceFilePropertiesClassName;
		}
		#endregion
	}
}
