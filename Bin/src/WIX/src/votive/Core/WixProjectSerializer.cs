//-------------------------------------------------------------------------------------------------
// <copyright file="WixProjectSerializer.cs" company="Microsoft">
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
// Serializes a WiX project to and from a file.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using System.Xml;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;

	/// <summary>
	/// Serializes a WiX project file to and from a file.
	/// </summary>
	/// <remarks>
	/// In the future, this class can be subclassed to provide different serializations for future
	/// versions of the schema. The default implementation is for a version 1 schema.
	/// </remarks>
	internal class WixProjectSerializer : ProjectSerializer
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(WixProjectSerializer);
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public WixProjectSerializer()
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the attached project.
		/// </summary>
		public new WixProject Project
		{
			get { return (WixProject)base.Project; }
		}

		/// <summary>
		/// Gets the name of a reference XML element.
		/// </summary>
		protected override string ReferenceElementName
		{
			get { return WixElementNames.WixlibReference; }
		}

		/// <summary>
		/// Gets the name of a references XML element.
		/// </summary>
		protected override string ReferencesElementName
		{
			get { return WixElementNames.WixlibReferences; }
		}

		/// <summary>
		/// Gets the name of the root project XML element.
		/// </summary>
		protected override string ProjectElementName
		{
			get { return WixElementNames.WindowsInstallerXml; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Creates a new <see cref="WixProject"/> object.
		/// </summary>
		/// <param name="serializer">The <see cref="ProjectSerializer"/> to use for saving the project.</param>
		/// <returns>A new <see cref="Project"/> object.</returns>
		protected override Project CreateProject(ProjectSerializer serializer)
		{
			Tracer.Assert(serializer is WixProjectSerializer, "Somehow we're not getting the right type of serializer here.");
			return new WixProject((WixProjectSerializer)serializer);
		}

		/// <summary>
		/// Creates a new <see cref="WixProjectConfiguration"/> object.
		/// </summary>
		/// <param name="project">The parent <see cref="Project"/> of the configuration.</param>
		/// <param name="name">The name of the configuration.</param>
		/// <returns>A newly created <see cref="ProjectConfiguration"/> object.</returns>
		protected override ProjectConfiguration CreateProjectConfiguration(Project project, string name)
		{
			Tracer.Assert(project is WixProject, "Somehow we're not getting the right type of project here.");
			return new WixProjectConfiguration((WixProject)project, name);
		}

		/// <summary>
		/// Reads the &lt;BuildSettings&gt; node.
		/// </summary>
		/// <param name="node">The <see cref="XmlNode"/> to read.</param>
		/// <returns>true if the node was read correctly; otherwise, false.</returns>
		protected override bool ReadBuildSettingsNode(XmlNode node)
		{
			// Let the base class read the standard attributes.
			if (!base.ReadBuildSettingsNode(node))
			{
				return false;
			}

			// Get the required attributes.
			string outputTypeString;
			if (!this.GetRequiredAttribute(node, WixAttributeNames.OutputType, out outputTypeString))
			{
				return false;
			}

			// Try to parse the output type.
			WixBuildSettings.BuildOutputType outputType;
			try
			{
				outputType = (WixBuildSettings.BuildOutputType)Enum.Parse(typeof(WixBuildSettings.BuildOutputType), outputTypeString, true);
			}
			catch (FormatException)
			{
				if (!this.SilentFailures)
				{
					string message = Package.Instance.Context.NativeResources.GetString(ResourceId.IDS_E_PROJFILE_INVALIDATTRIBUTE, this.Project.FilePath, WixAttributeNames.OutputType, node.Name);
					Package.Instance.Context.ShowErrorMessageBox(message);
				}
				Tracer.WriteLineWarning(classType, "ReadBuildSettingsNode", "Invalid required attribute '{0}' from '{1}'.", WixAttributeNames.OutputType, node.Name);
				return false;
			}

			// Set the properties on a new BuildSettings object.
			this.Project.BuildSettings.OutputType = outputType;

			return true;
		}

		/// <summary>
		/// Reads the &lt;Configuration&gt; node's children.
		/// </summary>
		/// <param name="configurationNode">The parent &lt;Configuration&gt; <see cref="XmlNode"/> to read.</param>
		/// <returns>true if the node was read correctly; otherwise, false.</returns>
		protected override bool ReadConfigurationChildrenNodes(XmlNode configurationNode, ProjectConfiguration projectConfiguration)
		{
			// TODO: Read the candle and light settings.
			return base.ReadConfigurationChildrenNodes(configurationNode, projectConfiguration);
		}

		/// <summary>
		/// Writes the attributes for the project root node.
		/// </summary>
		/// <param name="writer">The writer to use.</param>
		protected override void WriteBuildSettingsAttributes(ProjectFileXmlWriter writer)
		{
			base.WriteBuildSettingsAttributes(writer);
			WixBuildSettings buildSettings = this.Project.BuildSettings;
			writer.WriteAttributeString(WixAttributeNames.OutputType, buildSettings.OutputType.ToString());
		}
		#endregion

		#region Classes
		//==========================================================================================
		// Classes
		//==========================================================================================

		/// <summary>
		/// Contains the names of all of the different attributes in all of the various elements
		/// in a serialized WiX project file.
		/// </summary>
		/// <remarks>See the remarks for <see cref="ElementNames"/> for a discussion on const vs.
		/// static readonly.</remarks>
		protected class WixAttributeNames
		{
			public const string OutputType = "OutputType";
		}

		/// <summary>
		/// Contains the element names for the serialized WiX project file.
		/// </summary>
		/// <remarks>
		/// These are const instead of static readonly so that we can use them in a switch statement
		/// if we need to. Since the class is private, const is Ok. The difference between const
		/// and static readonly is that the compiler will "burn" the const values into the caller's
		/// code. Static readonly keeps one and only one copy of the string around. If this class
		/// were publicly visible, we should definitely make these static readonly so that if the
		/// tags ever change we wouldn't have to force a recompile of all of the various assemblies
		/// that use this assembly.
		/// </remarks>
		private class WixElementNames
		{
			public const string WixlibReference = "WixlibReference";
			public const string WixlibReferences = "WixlibReferences";
			public const string WindowsInstallerXml = "WindowsInstallerXML";
		}
		#endregion
	}
}
