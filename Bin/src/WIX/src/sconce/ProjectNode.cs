//-------------------------------------------------------------------------------------------------
// <copyright file="ProjectNode.cs" company="Microsoft">
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
// The root node within a Solution Explorer hierarchy.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Drawing;
	using System.IO;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Represents the root node within a Solution Explorer hierarchy.
	/// </summary>
	public class ProjectNode : VirtualFolderNode
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		public static readonly uint RootHierarchyId = NativeMethods.VSITEMID_ROOT;

		private static readonly string defaultProjectPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Project.proj");
		private static readonly Type classType = typeof(ProjectNode);

		private ReferenceFolderNode referencesNode;
		private bool unavailable = false;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectNode"/> class.
		/// </summary>
		/// <param name="hierarchy">The parent <see cref="Hierarchy"/> object.</param>
		/// <remarks>
		/// The caption will get set by Visual Studio during initialization. The absolute path
		/// will be set when the project is loaded, so we'll just set the absolute path to some
		/// dummy value for now.
		/// </remarks>
		public ProjectNode(Hierarchy hierarchy) : base(hierarchy, defaultProjectPath)
		{
			this.ExpandByDefault = true;
			this.Expanded = true;
			this.AddReferenceFolder();
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the absolute directory in which this node resides.
		/// </summary>
		/// <remarks>We're overriding because the root node is a folder and a file, which is not the norm.</remarks>
		public override string AbsoluteDirectory
		{
			get { return PackageUtility.CanonicalizeDirectoryPath(Path.GetDirectoryName(this.AbsolutePath)); }
		}

		public override string AbsolutePath
		{
			get { return base.AbsolutePath; }
			set
			{
				base.AbsolutePath = value;
				// Change the absolute path of the library folder node.
				if (this.referencesNode != null)
				{
					this.referencesNode.AbsolutePath = Path.Combine(this.AbsoluteDirectory, this.referencesNode.Caption);
				}
			}
		}

		public override string Caption
		{
			get
			{
				string caption = Path.GetFileNameWithoutExtension(this.AbsolutePath);
				if (this.Unavailable)
				{
					caption = SconceStrings.UnavailableCaption(caption);
				}
				return caption;
			}
		}

		public override bool CaptionEditable
		{
			get { return !this.Unavailable; }
		}

		/// <summary>
		/// The document cookie is an index into the environment's RDT (Running Document Table).
		/// </summary>
		public override uint DocumentCookie
		{
			get
			{
				// The root node has a document cookie, but we don't know it initially. If our cached document
				// cookie is a null cookie then we'll query the environment for it and then cache it.
				if (base.DocumentCookie == DocumentInfo.NullCookie)
				{
					DocumentInfo thisInfo = Context.RunningDocumentTable.FindByPath(this.AbsolutePath);
					if (thisInfo != null)
					{
						this.SetDocumentCookie(thisInfo.Cookie);
					}
				}
				return base.DocumentCookie;
			}
		}

		public override uint HierarchyId
		{
			get { return RootHierarchyId; }
		}

		/// <summary>
		/// Gets the image for the project node based on the state (if it's available vs. unavailable).
		/// </summary>
		/// <remarks>
		/// Subclasses should override <see cref="ProjectImage"/> and <see cref="UnavailableImage"/>
		/// instead of overriding this property.
		/// </remarks>
		public override Image Image
		{
			get
			{
				if (this.Unavailable)
				{
					return this.UnavailableImage;
				}
				return this.ProjectImage;
			}
		}

		/// <summary>
		/// Gets the one and only reference folder node.
		/// </summary>
		public ReferenceFolderNode ReferencesNode
		{
			get
			{
				if (this.referencesNode == null)
				{
					this.AddReferenceFolder();
				}
				return this.referencesNode;
			}
		}

		public override Image OpenImage
		{
			get { return this.Image; }
		}

		/// <summary>
		/// Gets the image for the project node when the project is available (the normal image).
		/// </summary>
		public virtual Image ProjectImage
		{
			get { return HierarchyImages.Project; }
		}

		/// <summary>
		/// Gets the node's properties to show in the Property window.
		/// </summary>
		public override NodeProperties Properties
		{
			get { return new ProjectNodeProperties(this); }
		}

		public bool Unavailable
		{
			get { return this.unavailable; }
			set
			{
				if (this.Unavailable != value)
				{
					this.unavailable = value;

					// If we're toggling between available/unavailable, then we want to clear
					// all of our children and start over.
					this.Children.IsReadOnly = false;
					this.Children.Clear();

					if (this.unavailable)
					{
						// Add a text node as the only child.
						string textNodeCaption = SconceStrings.ProjectUnavailable;
						TextNode textNode = new TextNode(this.Hierarchy, this.AbsoluteDirectory, textNodeCaption);
						this.Children.Add(textNode);
						this.referencesNode = null;
						this.Children.IsReadOnly = true;
					}
					else
					{
						// Re-add the library folder.
						this.AddReferenceFolder();
					}

					// The caption will change if we've become available/unavailable.
					this.OnPropertyChanged(__VSHPROPID.VSHPROPID_Caption);
					// The image will also change.
					this.OnPropertyChanged(__VSHPROPID.VSHPROPID_IconHandle);
					this.OnPropertyChanged(__VSHPROPID.VSHPROPID_OpenFolderIconHandle);

					// Set the dirty flag to false to prevent saving an unavailable hierarchy.
					this.Hierarchy.ClearDirty();

					// Clear our cached document cookie. We'll get it again when we need it.
					this.SetDocumentCookie(DocumentInfo.NullCookie);
				}
			}
		}

		/// <summary>
		/// Gets the image for the project node when the project is unavailable (the dimmed image).
		/// </summary>
		public virtual Image UnavailableImage
		{
			get { return HierarchyImages.UnavailableProject; }
		}

		public override VsMenus.ContextMenuId VisualStudioContextMenuId
		{
			get { return VsMenus.ContextMenuId.IDM_VS_CTXT_PROJNODE; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Executes a command from the standard Visual Studio 2000 command set.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <returns>true if the command is supported; otherwise, false.</returns>
		public override bool ExecuteStandard2KCommand(VsCommand2K command)
		{
			bool supported = true;

			switch (command)
			{
				case VsCommand2K.ADDREFERENCE:
					this.Hierarchy.ShowAddReferenceDialog();
					break;

				default:
					supported = base.ExecuteStandard2KCommand(command);
					break;
			}

			return supported;
		}

		/// <summary>
		/// Executes a command from the standard Visual Studio 97 command set.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <returns>true if the command is supported; otherwise, false.</returns>
		public override bool ExecuteStandard97Command(VsCommand command)
		{
			bool supported = true;

			switch (command)
			{
				case VsCommand.BuildCtx:
				case VsCommand.BuildSel:
					this.Hierarchy.AttachedProject.StartBuild(BuildOperation.Build);
					break;

				case VsCommand.RebuildCtx:
				case VsCommand.RebuildSel:
					this.Hierarchy.AttachedProject.StartBuild(BuildOperation.Rebuild);
					break;

				case VsCommand.CleanCtx:
				case VsCommand.CleanSel:
					this.Hierarchy.AttachedProject.StartBuild(BuildOperation.Clean);
					break;

				default:
					supported = false;
					break;
			}

			return supported;
		}

		/// <summary>
		/// Does the actual work of changing the caption after all of the verifications have been done
		/// that it's Ok to move the file.
		/// </summary>
		/// <param name="newCaption">The new caption.</param>
		/// <param name="newPath">The new absolute path.</param>
		public override void MoveNodeOnCaptionChange(string newCaption, string newPath)
		{
			string oldPath = this.AbsolutePath;
			IVsUIShell vsUIShell = (IVsUIShell)this.Hierarchy.ServiceProvider.GetServiceOrThrow(typeof(SVsUIShell), typeof(IVsUIShell), classType, "MoveNodeOnCaptionChange");

			// Tell the environment to stop listening to file change events on the old file.
			using (FileChangeNotificationSuspender notificationSuspender = new FileChangeNotificationSuspender(oldPath))
			{
				// Make sure the environment says we can start the rename.
				if (!this.Hierarchy.AttachedProject.Tracker.CanRenameProject(oldPath, newPath))
				{
					// If the user chose to not check out the solution file, then we want to throw the
					// save cancelled HRESULT.
					throw new COMException("User cancelled the solution file check out.", NativeMethods.OLE_E_PROMPTSAVECANCELLED);
				}

				// Move the file on the file system to match the new name.
				if (File.Exists(oldPath) && !File.Exists(newPath))
				{
					Tracer.WriteLineInformation(classType, "MoveNodeOnCaptionChange", "Renaming the project file '{0}' to '{1}'.", oldPath, newPath);
					File.Move(oldPath, newPath);
				}

				// Tell the environment that we're done renaming the document.
				this.Hierarchy.AttachedProject.Tracker.OnProjectRenamed(oldPath, newPath);

				// Update the property browser.
				vsUIShell.RefreshPropertyBrowser(0);
			}
		}

		/// <summary>
		/// Queries the state of a command from the standard Visual Studio 2000 command set on this node.
		/// </summary>
		/// <param name="command">The command to query.</param>
		/// <returns>One of the <see cref="CommandStatus"/> values if the node handles the command;
		/// otherwise <see cref="CommandStatus.Unhandled"/>.</returns>
		public override CommandStatus QueryStandard2KCommandStatus(VsCommand2K command)
		{
			if (this.Unavailable)
			{
				return CommandStatus.Unhandled;
			}

			CommandStatus status = CommandStatus.Unhandled;

			switch (command)
			{
				case VsCommand2K.ADDREFERENCE:
					status = this.ReferencesNode.QueryStandard2KCommandStatus(command);
					break;

				default:
					status = base.QueryStandard2KCommandStatus(command);
					break;
			}

			return status;
		}

		/// <summary>
		/// Queries the state of a command from the standard Visual Studio 97 command set on this node.
		/// </summary>
		/// <param name="command">The command to query.</param>
		/// <returns>One of the <see cref="CommandStatus"/> values if the node handles the command;
		/// otherwise <see cref="CommandStatus.Unhandled"/>.</returns>
		public override CommandStatus QueryStandard97CommandStatus(VsCommand command)
		{
			if (this.Unavailable)
			{
				return CommandStatus.Unhandled;
			}

			CommandStatus status = CommandStatus.Unhandled;

			switch (command)
			{
				case VsCommand.Cut:
				case VsCommand.Copy:
				case VsCommand.Delete:
					status = CommandStatus.NotSupportedOrEnabled;
					break;

				case VsCommand.Remove:
					status = CommandStatus.SupportedAndEnabled;
					break;

				case VsCommand.BuildSel:
				case VsCommand.BuildCtx:
				case VsCommand.RebuildSel:
				case VsCommand.RebuildCtx:
				case VsCommand.CleanSel:
				case VsCommand.CleanCtx:
					if (!Package.Instance.Context.IsSolutionBuilding)
					{
						status = CommandStatus.SupportedAndEnabled;
					}
					else
					{
						status = CommandStatus.Supported;
					}
					break;

				case VsCommand.ProjectDependencies:
				case VsCommand.BuildOrder:
				case VsCommand.ProjectSettings:
					status = CommandStatus.SupportedAndEnabled;
					break;

				default:
					status = base.QueryStandard97CommandStatus(command);
					break;
			}

			return status;
		}

		/// <summary>
		/// Sets the node's caption.
		/// </summary>
		/// <param name="value">The new caption.</param>
		/// <returns>
		/// <list type="bullet">
		/// <item>S_OK if the node's caption did not need to be changed, or it was changed but not renamed (different casing)</item>
		/// <item>S_FALSE if the node's caption was renamed (a new node was created and the old node deleted).
		/// Visual Studio will flush the old hierarchy ID if it sees S_FALSE.</item>
		/// <item>An error code</item>
		/// </list>
		/// </returns>
		/// <remarks>
		/// Note that exceptions are thrown rather than showing a message box. This is because this method
		/// will be called from one of the <see cref="NodeProperties"/> classes (the user is changing the
		/// value from the property pane) as well as during an automation call. We should not be showing
		/// message boxes in these scenarios. Visual Studio will show a message box for us if its appropriate
		/// when we raise an exception here.
		/// </remarks>
		public override int SetCaption(string value)
		{
			if (String.IsNullOrEmpty(value))
			{
				throw new ArgumentException(SconceStrings.ErrorBlankCaption, "value");
			}

			// Make sure the new path has the correct extension.
			string projectExtension = Path.GetExtension(this.AbsolutePath);
			string extension = Path.GetExtension(value);
			if (!PackageUtility.FileStringEquals(extension, projectExtension))
			{
				value += projectExtension;
			}

			return base.SetCaption(value);
		}

		/// <summary>
		/// Performs initial verifications on the new caption before anything is changed. This includes
		/// making sure that it's a valid file name and that the user wants to change the extension.
		/// </summary>
		/// <param name="newCaption">The new caption value.</param>
		/// <param name="newPath">The new absolute path.</param>
		/// <returns>
		/// true if processing should continue after the method returns; false if the caller should return S_OK.
		/// Note that on errors an exception is thrown.
		/// </returns>
		protected override bool VerifyCaption(string newCaption, string newPath)
		{
			if (!base.VerifyCaption(newCaption, newPath))
			{
				return false;
			}

			// Make sure there is a file name and that it doesn't start with a dot.
			string fileNameNoExt = Path.GetFileNameWithoutExtension(newCaption);
			if (String.IsNullOrEmpty(fileNameNoExt) || fileNameNoExt[0] == '.')
			{
				throw new InvalidOperationException(SconceStrings.ErrorFileNameCannotContainLeadingPeriod);
			}

			return true;
		}

		/// <summary>
		/// Canonicalizes the specified path as a directory path.
		/// </summary>
		/// <param name="absolutePath">The path to canonicalize.</param>
		/// <returns>The canonicalized path.</returns>
		protected override string CanonicalizePath(string absolutePath)
		{
			return PackageUtility.CanonicalizeFilePath(absolutePath);
		}

		/// <summary>
		/// Creates a new <see cref="ReferenceFolderNode"/>. Allows subclasses to create
		/// type-specific library folder nodes.
		/// </summary>
		/// <returns>A new <see cref="ReferenceFolderNode"/> object.</returns>
		protected virtual ReferenceFolderNode CreateReferenceFolderNode()
		{
			return new ReferenceFolderNode(this.Hierarchy, this.AbsoluteDirectory);
		}

		/// <summary>
		/// Adds the library folder node to the hierarchy.
		/// </summary>
		private void AddReferenceFolder()
		{
			if (this.referencesNode == null)
			{
				this.referencesNode = this.CreateReferenceFolderNode();
				this.Children.Add(this.referencesNode);
			}
		}
		#endregion
	}
}
