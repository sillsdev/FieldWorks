//-------------------------------------------------------------------------------------------------
// <copyright file="FolderNode.cs" company="Microsoft">
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
// A folder node within a Solution Explorer hierarchy.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.Drawing;
	using System.Globalization;
	using System.IO;
	using Microsoft.VisualStudio.Shell.Interop;

	public class FolderNode : Node
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(FolderNode);

		private NodeCollection children;
		private bool expandByDefault;
		private bool expanded;
		private FolderNode newNodeOnRename;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public FolderNode(Hierarchy hierarchy, string absolutePath) : base(hierarchy, absolutePath)
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the children collection.
		/// </summary>
		public NodeCollection Children
		{
			get
			{
				if (this.children == null)
				{
					this.children = new NodeCollection(this);
					// Hook up to listen to the collection's events.
					this.children.CollectionChanged += new CollectionChangeEventHandler(this.NodeCollectionChanged);
				}
				return this.children;
			}
		}

		public Image ClosedImage
		{
			get { return this.Image; }
		}

		public override bool Expandable
		{
			get { return this.Children.Count > 0; }
		}

		public override bool ExpandByDefault
		{
			get { return this.expandByDefault; }
			set
			{
				if (this.ExpandByDefault != value)
				{
					this.expandByDefault = value;
					this.Hierarchy.OnPropertyChanged(this, __VSHPROPID.VSHPROPID_ExpandByDefault);
				}
			}
		}

		/// <summary>
		/// Gets or sets the expansion state of the node. The setter does not actually expand
		/// or collapse the node. Call <see cref="Expand"/> or <see cref="Collapse"/> instead.
		/// </summary>
		public override bool Expanded
		{
			get { return this.expanded; }
			set
			{
				if (this.Expanded != value)
				{
					this.expanded = value;
					this.Hierarchy.OnPropertyChanged(this, __VSHPROPID.VSHPROPID_Expanded);
				}
			}
		}

		public override Node FirstChild
		{
			get { return (this.Children.Count > 0 ? this.Children[0] : null); }
		}

		public override Image Image
		{
			get { return HierarchyImages.ClosedFolder; }
		}

		public override bool IsFile
		{
			get { return false; }
		}

		public override bool IsFolder
		{
			get { return true; }
		}

		public override bool IsVirtual
		{
			get { return false; }
		}

		public virtual Image OpenImage
		{
			get { return HierarchyImages.OpenFolder; }
		}

		/// <summary>
		/// Gets the node's properties to show in the Property window.
		/// </summary>
		public override NodeProperties Properties
		{
			get { return new FolderNodeProperties(this); }
		}

		public override Guid VisualStudioTypeGuid
		{
			get
			{
				if (this.IsVirtual)
				{
					return NativeMethods.GUID_ItemType_VirtualFolder;
				}
				return NativeMethods.GUID_ItemType_PhysicalFolder;
			}
		}

		public override VsMenus.ContextMenuId VisualStudioContextMenuId
		{
			get { return VsMenus.ContextMenuId.IDM_VS_CTXT_FOLDERNODE; }
		}

		/// <summary>
		/// Returns a value indicating whether one or more contained <see cref="IDirtyable"/> objects
		/// are dirty.
		/// </summary>
		protected override bool AreContainedObjectsDirty
		{
			get { return (base.AreContainedObjectsDirty || this.Children.IsDirty); }
		}

		/// <summary>
		/// Gets or sets the <see cref="FolderNode"/> that is the new node based on this node on a
		/// rename (move) operation. This is temporary and should only be used during the
		/// <see cref="MoveNodeOnCaptionChange"/> method.
		/// </summary>
		protected FolderNode NewNodeOnRename
		{
			get { return this.newNodeOnRename; }
			set { this.newNodeOnRename = value; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Closes all of the children nodes of this node.
		/// </summary>
		public override void Close()
		{
			foreach (Node node in this.Children)
			{
				node.Close();
			}
			base.Close();
		}

		/// <summary>
		/// Collapses the current node.
		/// </summary>
		public void Collapse()
		{
			// Get the Solution Explorer window.
			IVsUIHierarchyWindow solutionExplorer = Package.Instance.Context.SolutionExplorer;

			// Expand or collapse the node in the Solution Explorer.
			EXPANDFLAGS flags = EXPANDFLAGS.EXPF_CollapseFolder;
			NativeMethods.ThrowOnFailure(solutionExplorer.ExpandItem(this.Hierarchy, this.HierarchyId, flags));
		}

		/// <summary>
		/// Occurs when a double click on the node or when Enter is pressed when the node is selected.
		/// </summary>
		public override void DoDefaultAction()
		{
			// The default action for a folder is to toggle the Expanded property, which the
			// Visual Studio shell will do for us automatically.
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
				case VsCommand.NewFolder:
				{
					// We'll create a new folder, then immediately give the user a chance to rename it.
					// The code in for the Caption setter will make sure to rename the node and directory.
					string folderName = this.GenerateUniqueName("NewFolder", String.Empty, true);
					FolderNode folderNode = this.Hierarchy.CreateAndAddFolder(this, folderName);
					folderNode.StartNodeEdit();
					break;
				}

				case VsCommand.AddNewItem:
					this.Hierarchy.ShowAddFileDialogBox(this, AddFileDialogType.AddNew);
					break;

				case VsCommand.AddExistingItem:
					this.Hierarchy.ShowAddFileDialogBox(this, AddFileDialogType.AddExisting);
					break;

				default:
					supported = base.ExecuteStandard97Command(command);
					break;
			}
			return supported;
		}

		/// <summary>
		/// Expands the current node.
		/// </summary>
		public void Expand()
		{
			// Get the Solution Explorer window.
			IVsUIHierarchyWindow solutionExplorer = Package.Instance.Context.SolutionExplorer;

			// Expand or collapse the node in the Solution Explorer.
			EXPANDFLAGS flags = EXPANDFLAGS.EXPF_ExpandFolder;
			NativeMethods.ThrowOnFailure(solutionExplorer.ExpandItem(this.Hierarchy, this.HierarchyId, flags));
		}

		/// <summary>
		/// Searches our children for a node with the specified hierarchy id.
		/// </summary>
		/// <param name="hierarchyId">The hierarchy identifier to search for.</param>
		/// <param name="recurse">Indicates whether to recursively search the children for the node.</param>
		/// <returns>The <see cref="Node"/> that was found or null if the node was not found.</returns>
		public Node FindById(uint hierarchyId, bool recurse)
		{
			Node node = null;

			// Make sure to check this node.
			if (this.HierarchyId == hierarchyId)
			{
				node = this;
			}
			else if (this.Children.Contains(hierarchyId))
			{
				node = this.Children[hierarchyId];
			}
			else if (recurse)
			{
				foreach (Node child in this.Children)
				{
					FolderNode folderNode = child as FolderNode;
					if (folderNode != null)
					{
						node = folderNode.FindById(hierarchyId, true);
						if (node != null)
						{
							break;
						}
					}
				}
			}
			return node;
		}

		/// <summary>
		/// Searches our children for a node with the specified canonical name.
		/// </summary>
		/// <param name="canonicalName">The canonical name to search for.</param>
		/// <param name="recurse">Indicates whether to recursively search the children for the node.</param>
		/// <returns>The <see cref="Node"/> that was found or null if the node was not found.</returns>
		public Node FindByName(string canonicalName, bool recurse)
		{
			Node foundNode = null;

			if (PackageUtility.FileStringEquals(this.CanonicalName, canonicalName))
			{
				foundNode = this;
			}
			else
			{
				// Do a linear search for the node. We do this for the following reasons:
				// 1) There usually aren't that many nodes in the project. Even if there are hundreds, a linear
				//    search is almost neglible in this case.
				// 2) This isn't actually called that much.
				// 3) We could also have another hashtable lookup keyed by canonical name. However, this means
				//    that we have to keep track of when the canonical names change. This was deemed more of
				//    an overhead than it's worth. If we find the performance is bad we can always optimize
				//    this later.
				foreach (Node node in this.Children)
				{
					FolderNode folderNode = node as FolderNode;
					if (PackageUtility.FileStringEquals(node.CanonicalName, canonicalName))
					{
						foundNode = node;
						break;
					}
					else if (folderNode != null && recurse)
					{
						foundNode = folderNode.FindByName(canonicalName, true);
						if (foundNode != null)
						{
							break;
						}
					}
				}
			}
			return foundNode;
		}

		/// <summary>
		/// Generates a unique document name for a new node under the parent node.
		/// </summary>
		/// <param name="suggestedRoot">The suggested root to use for the unique name.</param>
		/// <param name="extension">The extension to use for the new file or folder (with or without the leading '.'). Can be null or empty.</param>
		/// <param name="isFolder">Indicates whether the new name is intended to be a file or a folder.</param>
		/// <returns>A unique document name for a new node under the this node.</returns>
		public string GenerateUniqueName(string suggestedRoot, string extension, bool isFolder)
		{
			Tracer.VerifyStringArgument(suggestedRoot, "suggestedRoot");

			int suffixNumber = 0;
			bool foundUnique = false;
			string uniqueName = String.Empty;

			// Canonicalize the extension by setting it either to "" or prepend it with a '.'
			extension = ((extension == null || extension.Length == 0) ? String.Empty : PackageUtility.EnsureLeadingChar(extension, '.'));

			// We have to make sure that this item doesn't already exist in the hierarchy and the file system.
			while (!foundUnique)
			{
				if (suffixNumber == 0)
				{
					uniqueName = suggestedRoot + extension;
				}
				else
				{
					uniqueName = suggestedRoot + suffixNumber + extension;
				}

				// Look in the hierarchy to see if there is an existing item with the proposed name.
				foundUnique = true;
				foreach (Node node in this.Children)
				{
					if (PackageUtility.FileStringEquals(uniqueName, node.Caption))
					{
						foundUnique = false;
						break;
					}
				}

				// If the name is unique within the hierarchy, we still need to check the file system.
				if (foundUnique)
				{
					string pathToCheck = Path.Combine(this.AbsoluteDirectory, uniqueName);
					if (isFolder && Directory.Exists(pathToCheck))
					{
						foundUnique = false;
					}
					else if (!isFolder && File.Exists(pathToCheck))
					{
						foundUnique = false;
					}
					else
					{
						// Ok, we found a unique name.
						break;
					}
				}

				// Increment the number to append to the root part of the path.
				suffixNumber++;
			}

			Tracer.WriteLineInformation(classType, "GenerateUniqueName", "Found a unique name for a new node. New name = '{0}'.", uniqueName);
			return uniqueName;
		}

		/// <summary>
		/// Gets the value of the specified property.
		/// </summary>
		/// <param name="propertyId">The Id of the property to retrieve.</param>
		/// <param name="propertyValue">The value of the specified property, or null if the property is not supported.</param>
		/// <returns>true if the property is supported; otherwise false.</returns>
		public override bool GetProperty(__VSHPROPID propertyId, out object propertyValue)
		{
			bool supported = true;
			propertyValue = null;

			// Get the property from the node.
			switch (propertyId)
			{
				case __VSHPROPID.VSHPROPID_OpenFolderIconHandle:
					propertyValue = this.GetImageHandle(this.OpenImage);
					break;

				default:
					supported = base.GetProperty(propertyId, out propertyValue);
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
			bool expandNewNode = this.Expanded;
			bool selected = this.Selected;

			// Make sure the environment says we can start the rename.
			if (!this.Hierarchy.AttachedProject.Tracker.CanRenameDirectory(oldPath, newPath))
			{
				return;
			}

			// Create the new folder before moving any of the children. Getting the parent is tricky
			// because on recursion this.Parent will point to the same parent and not be remapped
			// to the new location yet. Therefore, we have to use this mechanism to get the right parent.
			FolderNode newParent = (this.Parent.NewNodeOnRename == null ? this.Parent : this.Parent.NewNodeOnRename);
			FolderNode newNode = this.Hierarchy.CreateAndAddFolder(newParent, newCaption);
			this.NewNodeOnRename = newNode;

			// Iterate through the children and change their hierarchy/file locations.
			// Do it on a cloned collection, however, since the collection will be
			// changing from underneath us. Note that the directory will automatically
			// be created when the first child is moved.
			ArrayList clone = new ArrayList(this.Children);
			foreach (Node child in clone)
			{
				string newChildPath = Path.Combine(newPath, child.Caption);
				child.MoveNodeOnCaptionChange(child.Caption, newChildPath);
			}

			// Move the rest of the files in the old directory to the new one.
			PackageUtility.XMove(oldPath, newPath);

			// Delete the old directory (it should be empty).
			DirectoryInfo oldDir = new DirectoryInfo(oldPath);
			if (oldDir.Exists && oldDir.GetFileSystemInfos().Length == 0)
			{
				oldDir.Delete(true);
			}

			// Remove us from the hierarchy.
			this.RemoveFromProject();

			// Expand and select the new node if we were expanded.
			if (expandNewNode)
			{
				newNode.Expand();
			}

			if (selected)
			{
				newNode.Select();
			}

			// Tell the environment that we're done renaming the document.
			this.Hierarchy.AttachedProject.Tracker.OnDirectoryRenamed(oldPath, newPath);

			// Update the property browser.
			IVsUIShell vsUIShell = (IVsUIShell)this.Hierarchy.ServiceProvider.GetServiceOrThrow(typeof(SVsUIShell), typeof(IVsUIShell), classType, "MoveNodeOnCaptionChange");
			vsUIShell.RefreshPropertyBrowser(0);
		}

		/// <summary>
		/// Saves all of the children of this node if they are dirty.
		/// </summary>
		public override void Save()
		{
			foreach (Node node in this.Children)
			{
				node.Save();
			}
		}

		/// <summary>
		/// Canonicalizes the specified path as a directory path.
		/// </summary>
		/// <param name="absolutePath">The path to canonicalize.</param>
		/// <returns>The canonicalized path.</returns>
		protected override string CanonicalizePath(string absolutePath)
		{
			return PackageUtility.CanonicalizeDirectoryPath(absolutePath);
		}

		/// <summary>
		/// Clears the dirty flag for any contained <see cref="IDirtyable"/> objects.
		/// </summary>
		protected override void ClearDirtyOnContainedObjects()
		{
			base.ClearDirtyOnContainedObjects();
			this.Children.ClearDirty();
		}

		/// <summary>
		/// Event handler for the <see cref="SortedCollection.CollectionChanged"/> event.
		/// </summary>
		/// <param name="sender">The object that raised the event.</param>
		/// <param name="e">A <see cref="CollectionChangeEventArgs"/> object that contains more data about the event.</param>
		private void NodeCollectionChanged(object sender, CollectionChangeEventArgs e)
		{
			if (e.Action == CollectionChangeAction.Add)
			{
				this.Hierarchy.OnItemAdded((Node)e.Element);
			}
			else if (e.Action == CollectionChangeAction.Remove)
			{
				this.Hierarchy.OnItemDeleted((Node)e.Element);
			}
		}
		#endregion
	}
}