//-------------------------------------------------------------------------------------------------
// <copyright file="WixProject.cs" company="Microsoft">
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
// A Wix project (either a product or a module).
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.Runtime.InteropServices;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;

	/// <summary>
	/// Represents a WiX Visual Studio project.
	/// </summary>
	[Guid("A49CE20D-CE64-4A08-9F24-92A6443D6699")]
	internal sealed class WixProject : Project
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(WixProject);
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="WixProject"/> class.
		/// </summary>
		public WixProject(WixProjectSerializer serializer) : base(serializer, new WixBuildSettings())
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		public new WixBuildSettings BuildSettings
		{
			get { return (WixBuildSettings)base.BuildSettings; }
			set { base.BuildSettings = value; }
		}

		/// <summary>
		/// Gets an array of property page GUIDs that are common, or not dependent upon the configuration.
		/// </summary>
		public override Guid[] CommonPropertyPageGuids
		{
			get
			{
				return new Guid[] { typeof(GeneralPropertyPage).GUID };
			}
		}

		/// <summary>
		/// Gets an array of property page GUIDs that are configuration dependent.
		/// </summary>
		public override Guid[] ConfigurationDependentPropertyPageGuids
		{
			get { return null; }
		}

		/// <summary>
		/// Gets the one and only library folder node.
		/// </summary>
		public new WixlibReferenceFolderNode ReferencesNode
		{
			get { return (WixlibReferenceFolderNode)base.ReferencesNode; }
		}

		public new WixProjectSerializer Serializer
		{
			get { return (WixProjectSerializer)base.Serializer; }
		}

		/// <summary>
		/// Gets the filter used in the open file dialog box.
		/// </summary>
		protected override string AddReferenceDialogFilter
		{
			get { return WixStrings.AddReferenceDialogFilter; }
		}

		/// <summary>
		/// Gets the initial directory for the open library reference dialog box.
		/// </summary>
		protected override string AddReferenceDialogInitialDirectory
		{
			get
			{
				string toolsDirectory = WixPackage.Instance.Context.Settings.ToolsDirectory;
				if (toolsDirectory == null || toolsDirectory.Length == 0 || !Directory.Exists(toolsDirectory))
				{
					return base.AddReferenceDialogInitialDirectory;
				}
				return PackageUtility.CanonicalizeDirectoryPath(toolsDirectory);
			}
		}

		/// <summary>
		/// Gets the title used in the open file dialog box.
		/// </summary>
		protected override string AddReferenceDialogTitle
		{
			get { return WixStrings.AddReferenceDialogTitle; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Creates the most specific file node type from the file's extension.
		/// </summary>
		/// <param name="absolutePath">The path to the file.</param>
		/// <returns>The most specific <see cref="FileNode"/> object for the OS file.</returns>
		public override FileNode CreateFileNodeFromExtension(string absolutePath)
		{
			Tracer.VerifyStringArgument(absolutePath, "absolutePath");
			FileNode node;

			string extension = Path.GetExtension(absolutePath).ToLower(CultureInfo.InvariantCulture);
			switch (extension)
			{
				case ".wixlib":
					node = new WixlibReferenceFileNode(this, absolutePath);
					break;

				case ".wxs":
				case ".wxi":
				case ".wxl":
				case ".wixout":
					node = new WixFileNode(this, absolutePath);
					break;

				default:
					node = base.CreateFileNodeFromExtension(absolutePath);
					break;
			}

			return node;
		}

		/// <summary>
		/// Creates a new <see cref="WixlibReferenceFileNode"/>.
		/// </summary>
		/// <param name="absolutePath">The absolute path to the library file.</param>
		/// <returns>A new <see cref="WixlibReferenceFileNode"/>.</returns>
		public override ReferenceFileNode CreateReferenceFileNode(string absolutePath)
		{
			return new WixlibReferenceFileNode(this, absolutePath);
		}

		/// <summary>
		/// Creates a new <see cref="WixRootHierarchyNode"/> object. Allows subclasses to create a
		/// type-specific root node.
		/// </summary>
		/// <returns>A new <see cref="WixRootHierarchyNode"/> object.</returns>
		protected override ProjectNode CreateProjectNode()
		{
			return new WixProjectNode(this);
		}
		#endregion
	}
}
