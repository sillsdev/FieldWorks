//-------------------------------------------------------------------------------------------------
// <copyright file="ProjectSerializer.cs" company="Microsoft">
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
// Serializes a project to and from a file.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using System.Xml;

	using ResId = ResourceId;

	/// <summary>
	/// Serializes an XML project file to and from a file.
	/// </summary>
	public class ProjectSerializer
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(ProjectSerializer);

		private Project project;
		private Version schemaVersion = new Version("1.0");
		private bool silentFailures = false;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public ProjectSerializer()
		{
		}
		#endregion

		#region Delegates
		//==========================================================================================
		// Delegates
		//==========================================================================================

		/// <summary>
		/// Delegate to a method that reads individual collection items from the XML.
		/// </summary>
		/// <param name="node">The <see cref="XmlNode"/> to read.</param>
		/// <remarks>
		/// Many of the collection items within the XML have the same pattern: &lt;Items&gt; followed
		/// by one or more &lt;Item&gt; nodes. This delegate allows us to have the general-purpose
		/// <see cref="ReadCollectionNode"/> method which then calls the specific parser for the
		/// collection item types.
		/// </remarks>
		protected delegate bool ReadCollectionItem(XmlNode node);
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the attached project.
		/// </summary>
		public Project Project
		{
			get
			{
				if (this.project == null)
				{
					string message = "Call Load on the serializer to initialize the project.";
					Tracer.Fail(message);
					throw new InvalidOperationException(message);
				}
				return this.project;
			}
		}

		/// <summary>
		/// Gets the highest schema version that this serializer supports.
		/// </summary>
		public Version SchemaVersion
		{
			get { return this.schemaVersion; }
		}

		/// <summary>
		/// Indicates whether to report failures to the user. This is useful for supressing any UI
		/// during automation, for example.
		/// </summary>
		/// <value>true if the reporting of failures to the user should be suppressed; otherwise, false.</value>
		public bool SilentFailures
		{
			get { return this.silentFailures; }
			set { this.silentFailures = value; }
		}

		/// <summary>
		/// Gets the name of a reference XML element.
		/// </summary>
		protected virtual string ReferenceElementName
		{
			get { return ElementNames.Reference; }
		}

		/// <summary>
		/// Gets the name of a references XML element.
		/// </summary>
		protected virtual string ReferencesElementName
		{
			get { return ElementNames.References; }
		}

		/// <summary>
		/// Gets the name of the root project XML element.
		/// </summary>
		protected virtual string ProjectElementName
		{
			get { return ElementNames.Project; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Copies the attached project and all of its referenced files to the destination directory.
		/// </summary>
		/// <param name="destinationPath">The destination path where the project file will be copied to.</param>
		/// <returns>A copy of the attached project rooted at <paramref name="destinationDirectory"/> or null if there were errors.</returns>
		public Project CopyTo(string destinationPath)
		{
			Tracer.VerifyStringArgument(destinationPath, "destinationPath");

			bool successful;

			// Reference the attached project.
			Project sourceProject = this.Project;

			// Create the destination project and a new project serializer for it.
			ProjectSerializer destSerializer = (ProjectSerializer)this.MemberwiseClone();
			Project destProject = this.CreateProject(destSerializer);

			// We have to have a back pointer from the serializer to the project otherwise when we go to
			// save the destination project, we'll have a null project pointer.
			destSerializer.project = destProject;

			// Set the destination project's properties.
			destProject.ProjectGuid = sourceProject.ProjectGuid;
			destProject.FilePath = destinationPath;
			string destDirectory = Path.GetDirectoryName(destProject.FilePath);

			// Create the destination directory if it doesn't already exist.
			if (!Directory.Exists(destDirectory))
			{
				Directory.CreateDirectory(destDirectory);
			}

			// Copy the build settings.
			destProject.BuildSettings = (BuildSettings)sourceProject.BuildSettings.Clone();

			// Copy all of the configurations.
			destProject.ConfigurationProvider = sourceProject.ConfigurationProvider.Clone(destProject);

			// Loop through the files, copying each file to the destination directory.
			// TODO: Change the relative path of linked files.
			successful = this.CopyNodeFiles(sourceProject.RootNode, destProject, destProject.RootNode);

			// Loop through the references, adding them to the project.
			if (successful)
			{
				foreach (ReferenceFileNode referenceFile in sourceProject.ReferencesNode.Children)
				{
					destProject.AddReference(referenceFile.AbsolutePath, false);
				}
			}

			// Now save the destination project.
			if (successful)
			{
				successful = destProject.Serializer.Save();
			}

			return (successful ? destProject : null);
		}

		/// <summary>
		/// Loads a project file from disk.
		/// </summary>
		/// <param name="filePath">The absolute path of the project file to load.</param>
		/// <returns>true if the project file was loaded correctly; otherwise, false.</returns>
		public bool Load(string filePath)
		{
			Tracer.VerifyStringArgument(filePath, "filePath");

			// Create a new project.
			this.project = this.CreateProject(this);

			// Set the project's file path to the one being loaded in. Do this first in case we have
			// to make the project unavailable it can still display the correct caption in Solution Explorer.
			this.Project.FilePath = filePath;

			// Make sure the file exists.
			if (!File.Exists(filePath))
			{
				if (!this.SilentFailures)
				{
					string message = SconceStrings.FileDoesNotExist(filePath);
					Package.Instance.Context.ShowErrorMessageBox(message);
				}
				this.Project.Unavailable = true;
				Tracer.WriteLine(classType, "Load", Tracer.Level.Warning, "The project file '{0}' does not exist.", filePath);
				return false;
			}

			try
			{
				using (StreamReader stream = new StreamReader(filePath))
				{
					XmlTextReader reader = new XmlTextReader(stream);
					reader.WhitespaceHandling = WhitespaceHandling.None;
					XmlDocument xmlDoc = new XmlDocument();
					xmlDoc.Load(reader);
					XmlNode node = xmlDoc.DocumentElement;

					// <VisualStudioProject>
					if (!this.VerifyNode(node, ElementNames.VisualStudioProject))
					{
						this.Project.Unavailable = true;
						return false;
					}
					node = node.FirstChild;

					if (!this.ReadProjectNode(node))
					{
						this.Project.Unavailable = true;
						return false;
					}
				}
			}
			catch (XmlException e)
			{
				if (!this.SilentFailures)
				{
					string projectFileName = Path.GetFileName(filePath);
					string title = Package.Instance.Context.NativeResources.GetString(ResId.IDS_E_INVALIDPROJECTFILE_TITLE);
					string message = Package.Instance.Context.NativeResources.GetString(ResId.IDS_E_INVALIDPROJECTFILE, projectFileName);
					Package.Instance.Context.ShowErrorMessageBox(title, message);
				}
				this.Project.Unavailable = true;
				Tracer.Fail("There was an error parsing '{0}': {1}", filePath, e.ToString());
				return false;
			}

			// Once the project has been loaded, it's not dirty anymore.
			this.Project.ClearDirty();

			return true;
		}

		/// <summary>
		/// Loads a project template file, serializing it into the specified project at the specified destination.
		/// </summary>
		/// <param name="templatePath">The absolute path of the template project file to load.</param>
		/// <param name="destinationPath">The absolute path to the new project file.</param>
		/// <returns>true if the project file was loaded correctly; otherwise, false.</returns>
		public bool LoadFromTemplate(string templatePath, string destinationPath)
		{
			Tracer.VerifyStringArgument(templatePath, "templatePath");
			Tracer.VerifyStringArgument(destinationPath, "destinationPath");

			bool successful = false;

			// Load the template project.
			if (this.Load(templatePath))
			{
				// Copy the loaded template to the real location of the new project.
				this.project = this.CopyTo(destinationPath);
				successful = (this.project != null);
			}

			return successful;
		}

		/// <summary>
		/// Saves the attached project.
		/// </summary>
		/// <returns>true if successful; otherwise, false.</returns>
		public bool Save()
		{
			return this.Save(Encoding.UTF8, false);
		}

		/// <summary>
		/// Saves the attached project in the specified encoding.
		/// </summary>
		/// <param name="encoding">The encoding of the file. If null, <see cref="Encoding.UTF8"/> is used.</param>
		/// <param name="forceSave">Indicates whether to ignore the attached project's dirty flag when determining whether to save.</param>
		/// <returns>true if successful; otherwise, false.</returns>
		public bool Save(Encoding encoding, bool forceSave)
		{
			if (encoding == null)
			{
				encoding = Encoding.UTF8;
			}

			// If a project hasn't been attached yet, there's nothing to save.
			if (this.Project == null)
			{
				return false;
			}

			// Check the dirty state of the project to see if we even need to save.
			if (!this.Project.IsDirty && !forceSave)
			{
				Tracer.WriteLineInformation(classType, "Save", "The project doesn't need to be saved.");
				return true;
			}

			// At this point we know we have to save the project.
			string filePath = this.Project.FilePath;
			try
			{
				using (StreamWriter streamWriter = new StreamWriter(filePath, false, encoding))
				{
					ProjectFileXmlWriter writer = new ProjectFileXmlWriter(streamWriter);
					writer.WriteStartDocument();

					// <VisualStudioProject>
					writer.WriteStartElement(ElementNames.VisualStudioProject);

					// <Project>
					writer.WriteStartElement(this.ProjectElementName);
					this.WriteProjectAttributes(writer);

					// <BuildSettings>
					BuildSettings buildSettings = this.Project.BuildSettings;
					writer.WriteStartElement(ElementNames.BuildSettings);
					this.WriteBuildSettingsAttributes(writer);
					writer.WriteEndElement();

					// <Configurations>
					writer.WriteStartElement(ElementNames.Configurations);
					foreach (ProjectConfiguration config in this.Project.ConfigurationProvider.ProjectConfigurations)
					{
						this.WriteConfigurationNode(writer, config);
					}
					writer.WriteEndElement();

					// <References>
					writer.WriteStartElement(this.ReferencesElementName);
					foreach (ReferenceFileNode reference in this.Project.ReferencesNode.Children)
					{
						writer.WriteStartElement(this.ReferenceElementName);
						writer.WriteAttributeString(AttributeNames.RelativePath, reference.RelativePath);
						writer.WriteEndElement();
					}
					writer.WriteEndElement();

					// <Files>
					writer.WriteStartElement(ElementNames.Files);
					this.WriteFilesInNode(writer, this.Project.RootNode);
					writer.WriteEndElement();

					writer.WriteEndDocument();

					// Clear the project's dirty state.
					this.Project.ClearDirty();
				}
			}
			catch (Exception e)
			{
				if (!this.SilentFailures)
				{
					string title = Package.Instance.Context.NativeResources.GetString(ResId.IDS_E_PROJECTFILESAVE_TITLE, filePath);
					string message = Package.Instance.Context.NativeResources.GetString(ResId.IDS_E_PROJECTFILESAVE, e.Message);
					Package.Instance.Context.ShowErrorMessageBox(title, message);
				}
				Tracer.Fail("There was an error in saving the file {0}: {1}", filePath, e.ToString());
				return false;
			}

			return true;
		}

		/// <summary>
		/// Gives subclasses a chance to create a type-specific project.
		/// </summary>
		/// <param name="serializer">The <see cref="ProjectSerializer"/> to use for saving the project.</param>
		/// <returns>A new <see cref="Project"/> object.</returns>
		protected virtual Project CreateProject(ProjectSerializer serializer)
		{
			return new Project(serializer);
		}

		/// <summary>
		/// Gives subclasses a chance to create a type-specific project configuration object.
		/// </summary>
		/// <param name="project">The parent <see cref="Project"/> of the configuration.</param>
		/// <param name="name">The name of the configuration.</param>
		/// <returns>A newly created <see cref="ProjectConfiguration"/> object.</returns>
		protected virtual ProjectConfiguration CreateProjectConfiguration(Project project, string name)
		{
			return new ProjectConfiguration(project, name);
		}

		/// <summary>
		/// Gets the optional attribute from the current node.
		/// </summary>
		/// <param name="node">The <see cref="XmlNode"/> to read.</param>
		/// <param name="name">The name of the attribute to retrieve.</param>
		/// <param name="defaultValue">The value to use if the attribute is not found.</param>
		/// <returns>The value of the attribute.</returns>
		protected string GetOptionalAttribute(XmlNode node, string name, string defaultValue)
		{
			string value = defaultValue;
			XmlAttribute attribute = node.Attributes[name];

			if (attribute != null)
			{
				value = attribute.Value;
			}

			return value;
		}

		/// <summary>
		/// Gets the required attribute from the current node. If the attribute does not exist
		/// or cannot be converted to the target type, then a message box is shown to the user.
		/// </summary>
		/// <param name="node">The <see cref="XmlNode"/> to read.</param>
		/// <param name="name">The name of the attribute to retrieve.</param>
		/// <param name="value">The value of the attribute.</param>
		/// <returns>true if the attribute exists and the value can be converted to the target
		/// type; otherwise, false.</returns>
		protected bool GetRequiredAttribute(XmlNode node, string name, out string value)
		{
			XmlAttribute attribute = node.Attributes[name];
			if (attribute == null)
			{
				value = null;
				Tracer.WriteLineWarning(classType, "GetRequiredAttribute", "Missing required attribute '{0}' from '{1}'.", name, node.Name);
				if (!this.SilentFailures)
				{
					string message = Package.Instance.Context.NativeResources.GetString(ResId.IDS_E_PROJFILE_MISSINGATTRIBUTE, this.Project.FilePath, name, node.Name);
					Package.Instance.Context.ShowErrorMessageBox(message);
				}
				return false;
			}
			value = attribute.Value;
			return true;
		}

		/// <summary>
		/// Reads the &lt;BuildSettings&gt; node.
		/// </summary>
		/// <param name="node">The <see cref="XmlNode"/> to read.</param>
		/// <returns>true if the node was read correctly; otherwise, false.</returns>
		protected virtual bool ReadBuildSettingsNode(XmlNode node)
		{
			// Make sure we're reading the expected node.
			if (!this.VerifyNode(node, ElementNames.BuildSettings))
			{
				Tracer.Fail("Getting to this point indicates a bug.");
				return false;
			}

			// Get the required attributes.
			string outputName;
			if (!this.GetRequiredAttribute(node, AttributeNames.OutputName, out outputName))
			{
				return false;
			}

			// Set the properties on the BuildSettings object.
			this.Project.BuildSettings.OutputName = outputName;

			return true;
		}

		/// <summary>
		/// Reads a node containing no attributes and all of its children, where each child is of the same type.
		/// </summary>
		/// <param name="node">The <see cref="XmlNode"/> to read.</param>
		/// <param name="nodeName">The name of the collection node.</param>
		/// <param name="childNodeName">The name of the children nodes.</param>
		/// <param name="readItem">A delegate pointing to the method that reads the individual collection items.</param>
		/// <returns>true if the node was read correctly; otherwise, false.</returns>
		protected bool ReadCollectionNode(XmlNode node, string nodeName, string childNodeName, ReadCollectionItem readItem)
		{
			bool successful = true;

			// Make sure we're reading the expected node.
			if (!this.VerifyNode(node, nodeName))
			{
				Tracer.Fail("Getting to this point indicates a bug.");
				return false;
			}

			// Read all of the children.
			foreach (XmlNode childNode in node.ChildNodes)
			{
				if (childNode.Name == childNodeName)
				{
					if (!readItem(childNode))
					{
						successful = false;
						break;
					}
				}
			}

			return successful;
		}

		/// <summary>
		/// Reads the &lt;Configuration&gt; node's children.
		/// </summary>
		/// <param name="configurationNode">The parent &lt;Configuration&gt; <see cref="XmlNode"/> to read.</param>
		/// <param name="projectConfiguration">The <see cref="ProjectConfiguration"/> to use.</param>
		/// <returns>true if the node was read correctly; otherwise, false.</returns>
		protected virtual bool ReadConfigurationChildrenNodes(XmlNode configurationNode, ProjectConfiguration projectConfiguration)
		{
			return true;
		}

		/// <summary>
		/// Reads the &lt;Configuration&gt; node and all of its children.
		/// </summary>
		/// <param name="node">The <see cref="XmlNode"/> to read.</param>
		/// <returns>true if the node was read correctly; otherwise, false.</returns>
		protected bool ReadConfigurationNode(XmlNode node)
		{
			if (!this.VerifyNode(node, ElementNames.Configuration))
			{
				Tracer.Fail("Getting to this point indicates a bug.");
				return false;
			}

			// Read in all of the required attributes.
			string name;
			string outputDirectory;
			if (!this.GetRequiredAttribute(node, AttributeNames.Name, out name) ||
				!this.GetRequiredAttribute(node, AttributeNames.RelativeOutputDirectory, out outputDirectory))
			{
				return false;
			}

			// Read in all of the optional attributes
			string intermediateDirectory = this.GetOptionalAttribute(node, AttributeNames.RelativeIntermediateDirectory, outputDirectory);

			// Create the project configuration and add it to the project.
			ProjectConfiguration projectConfig = this.CreateProjectConfiguration(this.Project, name);
			projectConfig.RelativeOutputDirectory = outputDirectory;
			projectConfig.RelativeIntermediateDirectory = intermediateDirectory;
			this.Project.ConfigurationProvider.ProjectConfigurations.Add(projectConfig);

			// Read the children.
			bool success = this.ReadConfigurationChildrenNodes(node, projectConfig);

			return success;
		}

		/// <summary>
		/// Reads the &lt;File&gt; node.
		/// </summary>
		/// <param name="node">The <see cref="XmlNode"/> to read.</param>
		/// <returns>true if the node was read correctly; otherwise, false.</returns>
		protected bool ReadFileNode(XmlNode node)
		{
			if (!this.VerifyNode(node, ElementNames.File))
			{
				Tracer.Fail("Getting to this point indicates a bug.");
				return false;
			}

			// <File RelativePath="path" />
			string relativePath;
			if (!this.GetRequiredAttribute(node, AttributeNames.RelativePath, out relativePath))
			{
				return false;
			}

			if (this.Project.AddExistingFile(relativePath, false) == null)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Reads the &lt;LibraryReference&gt; node.
		/// </summary>
		/// <param name="node">The <see cref="XmlNode"/> to read.</param>
		/// <returns>true if the node was read correctly; otherwise, false.</returns>
		protected bool ReadLibraryReferenceNode(XmlNode node)
		{
			if (!this.VerifyNode(node, this.ReferenceElementName))
			{
				Tracer.Fail("Getting to this point indicates a bug.");
				return false;
			}

			// <Reference RelativePath="path" />
			string relativePath;
			if (!this.GetRequiredAttribute(node, AttributeNames.RelativePath, out relativePath))
			{
				return false;
			}

			// Make the relative path into an absolute one.
			string absolutePath = Path.Combine(this.Project.RootDirectory, relativePath);
			absolutePath = PackageUtility.CanonicalizeFilePath(absolutePath);

			// Add the reference to the library node.
			this.Project.AddReference(absolutePath, false);

			return true;
		}

		/// <summary>
		/// Reads the &lt;WindowsInstallerXml&gt; node.
		/// </summary>
		/// <param name="node">The <see cref="XmlNode"/> to parse.</param>
		/// <returns>true if the node was read correctly; otherwise, false.</returns>
		protected bool ReadProjectNode(XmlNode node)
		{
			if (!this.VerifyNode(node, this.ProjectElementName))
			{
				return false;
			}

			// Read and validate all of the required attributes.
			// -------------------------------------------------

			// SchemaVersion
			Version fileSchemaVersion = this.SchemaVersion;
			string schemaString = XmlHelperMethods.GetAttributeString(node, AttributeNames.SchemaVersion, this.SchemaVersion.ToString());
			try
			{
				fileSchemaVersion = new Version(schemaString);
			}
			catch (Exception e)
			{
				Tracer.WriteLineWarning(classType, "ReadProjectNode", "Cannot parse the SchemaVersion attribute {0}: {1}.", schemaString, e.ToString());
			}
			if (fileSchemaVersion < this.SchemaVersion)
			{
				// Right now we only support version 1.0 schemas, but if we ever change the schema
				// with new versions then we would need to ask the user if he/she wants to upgrade
				// the project to the new schema version.
			}
			else if (fileSchemaVersion > this.SchemaVersion)
			{
				string projectFileName = Path.GetFileName(this.Project.FilePath);
				string message = Package.Instance.Context.NativeResources.GetString(ResId.IDS_E_PROJECTFILENEWERVERSION, projectFileName);
				Package.Instance.Context.ShowErrorMessageBox(message);
				return false;
			}

			// ProjectGuid
			this.Project.ProjectGuid = XmlHelperMethods.GetAttributeGuid(node, AttributeNames.ProjectGuid, Guid.NewGuid());

			// Read the rest of the nodes in the project file in any order.
			// -----------------------------------------------------------
			foreach (XmlNode childNode in node.ChildNodes)
			{
				bool success = true;

				switch (childNode.Name)
				{
					case ElementNames.BuildSettings:
						success = this.ReadBuildSettingsNode(childNode);
						break;

					case ElementNames.Configurations:
						success = this.ReadCollectionNode(childNode, ElementNames.Configurations, ElementNames.Configuration, new ReadCollectionItem(this.ReadConfigurationNode));
						break;

					case ElementNames.Files:
						success = this.ReadCollectionNode(childNode, ElementNames.Files, ElementNames.File, new ReadCollectionItem(this.ReadFileNode));
						break;
				}

				// We can't have this in the switch block because the reference node is not a constant string value.
				if (childNode.Name == this.ReferencesElementName)
				{
					success = this.ReadCollectionNode(childNode, this.ReferencesElementName, this.ReferenceElementName, new ReadCollectionItem(this.ReadLibraryReferenceNode));
				}

				if (!success)
				{
					this.Project.Unavailable = true;
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Verifies that the current node is named <paramref name="name"/>, showing a message box to the
		/// user if it is not.
		/// </summary>
		/// <param name="node">The <see cref="XmlNode"/> to check.</param>
		/// <param name="name">The expected name of the current node.</param>
		/// <returns>true if the current node is named <paramref name="name"/>; otherwise, false.</returns>
		protected bool VerifyNode(XmlNode node, string name)
		{
			if (node.Name != name)
			{
				Tracer.WriteLineWarning(classType, "VerifyNode", "Missing '{0}' element in the project file '{1}'.", name, this.Project.FilePath);
				if (!this.SilentFailures)
				{
					string message = Package.Instance.Context.NativeResources.GetString(ResId.IDS_E_PROJFILE_MISSINGSECTION, this.Project.FilePath, name);
					Package.Instance.Context.ShowErrorMessageBox(message);
				}
				return false;
			}
			return true;
		}

		/// <summary>
		/// Writes the attributes for the project root node.
		/// </summary>
		/// <param name="writer">The <see cref="ProjectFileXmlWriter"/> to use.</param>
		protected virtual void WriteBuildSettingsAttributes(ProjectFileXmlWriter writer)
		{
			writer.WriteAttributeString(AttributeNames.OutputName, this.Project.BuildSettings.OutputName);
		}

		/// <summary>
		/// Writes the &lt;Configuration&gt; node for the specified configuration.
		/// </summary>
		/// <param name="writer">The <see cref="ProjectFileXmlWriter"/> to use.</param>
		/// <param name="configuration">The project configuration to write.</param>
		protected void WriteConfigurationNode(ProjectFileXmlWriter writer, ProjectConfiguration configuration)
		{
			writer.WriteStartElement(ElementNames.Configuration);
			writer.WriteAttributeString(AttributeNames.Name, configuration.Name);
			writer.WriteAttributeString(AttributeNames.RelativeOutputDirectory, configuration.RelativeOutputDirectory);
			writer.WriteAttributeString(AttributeNames.RelativeIntermediateDirectory, configuration.RelativeIntermediateDirectory);
			writer.WriteEndElement();
		}

		/// <summary>
		/// Recursively writes all of the &lt;File&gt; nodes to the project file, one for each file
		/// in the parent node.
		/// </summary>
		/// <param name="writer">The file to which to write.</param>
		/// <param name="parent">The parent node to write.</param>
		protected void WriteFilesInNode(ProjectFileXmlWriter writer, FolderNode parent)
		{
			foreach (Node node in parent.Children)
			{
				if (node.IsFolder && !node.IsVirtual)
				{
					FolderNode folderNode = (FolderNode)node;
					// Recurse
					this.WriteFilesInNode(writer, folderNode);
				}
				else if (node.IsFile)
				{
					writer.WriteStartElement(ElementNames.File);
					writer.WriteAttributeString(AttributeNames.RelativePath, node.RelativePath);
					writer.WriteEndElement();
				}
			}
		}

		/// <summary>
		/// Writes the attributes for the project root node.
		/// </summary>
		/// <param name="writer">The writer to use.</param>
		protected virtual void WriteProjectAttributes(ProjectFileXmlWriter writer)
		{
			writer.WriteAttributeString(AttributeNames.ProductVersion, Package.Instance.ProductVersion.ToString());
			writer.WriteAttributeString(AttributeNames.ProjectGuid, this.Project.ProjectGuid.ToString("B").ToUpper(CultureInfo.InvariantCulture));
			writer.WriteAttributeString(AttributeNames.SchemaVersion, this.SchemaVersion.ToString());
		}

		/// <summary>
		/// Recursively copies all of the child file nodes from the source parent to the destination parent. Also copies the physical files.
		/// </summary>
		/// <param name="sourceParent">The node to copy from.</param>
		/// <param name="destinationProject">The project to place the copied nodes into.</param>
		/// <param name="destinationParent">The node to copy to.</param>
		private bool CopyNodeFiles(FolderNode sourceParent, Project destinationProject, FolderNode destinationParent)
		{
			bool canceled;
			foreach (Node sourceNode in sourceParent.Children)
			{
				if (sourceNode is FileNode)
				{
					string fileName = Path.GetFileName(sourceNode.AbsolutePath);
					string destinationPath = Path.Combine(destinationParent.AbsoluteDirectory, fileName);
					Node addedNode = destinationProject.AddCopyOfFile(sourceNode.AbsolutePath, destinationPath, out canceled);
					if (addedNode == null || canceled)
					{
						return false;
					}
				}
				else if (sourceNode is FolderNode && !(sourceNode is ReferenceFolderNode))
				{
					FolderNode destNode = destinationProject.CreateAndAddFolder(destinationParent, sourceNode.Caption);
					// Recure
					return this.CopyNodeFiles((FolderNode)sourceNode, destinationProject, destNode);
				}
			}
			return true;
		}
		#endregion

		#region Classes
		//==========================================================================================
		// Classes
		//==========================================================================================

		/// <summary>
		/// Contains the names of all of the different attributes in all of the various elements
		/// in a serialized XML project file.
		/// </summary>
		/// <remarks>See the remarks for <see cref="ElementNames"/> for a discussion on const vs.
		/// static readonly.</remarks>
		protected class AttributeNames
		{
			public const string Name = "Name";
			public const string OutputName = "OutputName";
			public const string ProductVersion = "ProductVersion";
			public const string ProjectGuid = "ProjectGuid";
			public const string RelativeIntermediateDirectory = "RelativeIntermediateDirectory";
			public const string RelativeOutputDirectory = "RelativeOutputDirectory";
			public const string RelativePath = "RelativePath";
			public const string SchemaVersion = "SchemaVersion";
		}

		/// <summary>
		/// Contains the element names for the serialized XML project file.
		/// </summary>
		/// <remarks>These are const instead of static readonly so that we can use them in a
		/// switch statement if we need to. Since the class is private, const is Ok. The difference
		/// between const and static readonly is that the compiler will "burn" the const values
		/// into the caller's code. Static readonly keeps one and only one copy of the string
		/// around. If this class were publicly visible, we should definitely make these static
		/// readonly so that if the tags ever change we wouldn't have to force a recompile of
		/// all of the various assemblies that use this assembly.</remarks>
		protected class ElementNames
		{
			public const string BuildSettings = "BuildSettings";
			public const string Configuration = "Configuration";
			public const string Configurations = "Configurations";
			public const string File = "File";
			public const string Files = "Files";
			public const string Project = "Project";
			public const string Reference = "Reference";
			public const string References = "References";
			public const string VisualStudioProject = "VisualStudioProject";
		}
		#endregion
	}
}
