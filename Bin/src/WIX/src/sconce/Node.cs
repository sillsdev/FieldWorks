//-------------------------------------------------------------------------------------------------
// <copyright file="Node.cs" company="Microsoft">
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
// A node within a Solution Explorer hierarchy.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Drawing;
	using System.Globalization;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Windows.Forms;
	using Microsoft.VisualStudio.Shell.Interop;

	using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
	using ResId = ResourceId;

	/// <summary>
	/// Abstract base class representing a node in a Solution Explorer hierarchy that has a parent,
	/// but no children.
	/// </summary>
	public abstract class Node : DirtyableObject
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(Node);
		private static uint nextHierarchyId = 0;

		private string absolutePath;
		private BuildAction buildAction = BuildAction.None;
		private uint documentCookie = DocumentInfo.NullCookie;
		private Hierarchy hierarchy;
		private uint hierarchyId;
		private FolderNode parent;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="Node"/> class.
		/// </summary>
		/// <param name="hierarchy">The parent <see cref="Hierarchy"/> object.</param>
		/// <param name="absolutePath">The absolute path to the node.</param>
		protected Node(Hierarchy hierarchy, string absolutePath) : this(hierarchy, absolutePath, BuildAction.None)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Node"/> class.
		/// </summary>
		/// <param name="hierarchy">The parent <see cref="Hierarchy"/> object.</param>
		/// <param name="absolutePath">The absolute path to the node.</param>
		/// <param name="buildAction">The action that should be taken for this node when building the project.</param>
		protected Node(Hierarchy hierarchy, string absolutePath, BuildAction buildAction)
		{
			this.hierarchy = hierarchy;
			this.hierarchyId = nextHierarchyId;
			nextHierarchyId++;
			this.absolutePath = this.CanonicalizePath(absolutePath);
			this.BuildAction = buildAction;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the singleton <see cref="PackageContext"/> object.
		/// </summary>
		public static PackageContext Context
		{
			get { return Package.Instance.Context; }
		}

		/// <summary>
		/// Gets the next hierarchy identifier.
		/// </summary>
		public static uint NextHierarchyId
		{
			get { return nextHierarchyId; }
		}

		/// <summary>
		/// Gets the absolute directory in which this node resides.
		/// </summary>
		public virtual string AbsoluteDirectory
		{
			get
			{
				if (this.IsFolder)
				{
					return this.AbsolutePath;
				}
				return PackageUtility.CanonicalizeDirectoryPath(Path.GetDirectoryName(this.AbsolutePath));
			}
		}

		public virtual string AbsolutePath
		{
			get { return this.absolutePath; }
			set
			{
				if (this.AbsolutePath != value)
				{
					this.absolutePath = this.CanonicalizePath(value);
					if (!this.IsVirtual && this.IsFile)
					{
						this.OnPropertyChanged(__VSHPROPID.VSHPROPID_SaveName);
					}
				}
			}
		}

		public BuildAction BuildAction
		{
			get { return this.buildAction; }
			set
			{
				if (!Enum.IsDefined(typeof(BuildAction), value))
				{
					throw new System.ComponentModel.InvalidEnumArgumentException("BuildAction", (int)value, typeof(BuildAction));
				}

				if (this.BuildAction != value)
				{
					this.buildAction = value;
					this.MakeDirty();
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether this node can be deleted from disk.
		/// </summary>
		/// <remarks>By default everything except for virtual nodes can be deleted.</remarks>
		public virtual bool CanDelete
		{
			get { return !this.IsVirtual; }
		}

		/// <summary>
		/// Gets a value indicating whether this node can be removed from the project.
		/// </summary>
		/// <remarks>By default everything except for virtual folders can be removed.</remarks>
		public virtual bool CanRemoveFromProject
		{
			get { return !(this.IsFolder && this.IsVirtual); }
		}

		public string CanonicalName
		{
			get { return this.AbsolutePath; }
		}

		public virtual string Caption
		{
			get
			{
				// Strip off the last '\' on folder nodes so that we can get the correct caption.
				string strippedPath = PackageUtility.StripTrailingChar(this.AbsolutePath, Path.DirectorySeparatorChar);
				return Path.GetFileName(strippedPath);
			}
		}

		public virtual bool CaptionEditable
		{
			get { return true; }
		}

		/// <summary>
		/// The document cookie is an index into the environment's RDT (Running Document Table).
		/// </summary>
		public virtual uint DocumentCookie
		{
			get { return this.documentCookie; }
		}

		public virtual bool Expandable
		{
			get { return false; }
		}

		public virtual bool ExpandByDefault
		{
			get { return false; }
			set { }
		}

		public virtual bool Expanded
		{
			get { return false; }
			set { }
		}

		public virtual Node FirstChild
		{
			get { return null; }
			set { }
		}

		public Hierarchy Hierarchy
		{
			get { return this.hierarchy; }
		}

		public virtual uint HierarchyId
		{
			get { return this.hierarchyId; }
		}

		public virtual Image Image
		{
			get { return null; }
		}

		public abstract bool IsFile { get; }
		public abstract bool IsFolder { get; }

		/// <summary>
		/// Gets a value indicating whether the node is not a file system object.
		/// </summary>
		public abstract bool IsVirtual { get; }

		/// <summary>
		/// Gets the next sibling of this node if it has one or null if it's the last sibling.
		/// </summary>
		public virtual Node NextSibling
		{
			get
			{
				if (this.Parent == null)
				{
					return null;
				}

				NodeCollection siblings = this.Parent.Children;
				int thisIndex = siblings.IndexOf(this.HierarchyId);
				if (thisIndex + 1 < siblings.Count)
				{
					return siblings[thisIndex + 1];
				}
				return null;
			}
		}

		/// <summary>
		/// Gets or sets the parent of this node. Returns null for the root node.
		/// </summary>
		/// <remarks>
		/// This is breaking the rules of object oriented programming slightly. Base classes should
		/// normally not know about subclasses. However, there is really no other elegant way around
		/// this. In order for the node to remove itself from it's parent, it needs to get to the
		/// children collection. We could make it part of the base class, but we really want all
		/// child-related functionalty in the FolderNode class.
		/// </remarks>
		public FolderNode Parent
		{
			get { return this.parent; }
			set
			{
				if (this.Parent != value)
				{
					this.parent = value;
					this.OnPropertyChanged(__VSHPROPID.VSHPROPID_Parent);
				}
			}
		}

		/// <summary>
		/// Gets the previous sibling of this node if there is one, or null if this is the first sibling.
		/// </summary>
		public Node PreviousSibling
		{
			get
			{
				if (this.Parent == null)
				{
					return null;
				}

				NodeCollection siblings = this.Parent.Children;
				int thisIndex = siblings.IndexOf(this.HierarchyId);
				if (thisIndex > 0)
				{
					return siblings[thisIndex - 1];
				}
				return null;
			}
		}

		/// <summary>
		/// Gets the node's properties to show in the Property window.
		/// </summary>
		public virtual NodeProperties Properties
		{
			get { return null; }
		}

		public string RelativePath
		{
			get { return PackageUtility.MakeRelative(this.Hierarchy.RootNode.AbsoluteDirectory, this.AbsolutePath); }
		}

		public string SaveName
		{
			get { return this.Caption; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the node is selected in the hierarchy.
		/// </summary>
		public bool Selected
		{
			get
			{
				Node selectedNode = this.Hierarchy.SelectedNode;
				return (selectedNode.HierarchyId == this.HierarchyId);
			}
			set { this.Select(); }
		}

		public abstract Guid VisualStudioTypeGuid { get; }

		public virtual VsMenus.ContextMenuId VisualStudioContextMenuId
		{
			get { return VsMenus.ContextMenuId.IDM_VS_CTXT_NOCOMMANDS; }
		}

		protected ServiceProvider ServiceProvider
		{
			get { return Package.Instance.Context.ServiceProvider; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Gets a service from the environment using the global package context's service provider.
		/// </summary>
		/// <param name="serviceType">An object that specifies the type of service object to get.</param>
		/// <returns>
		/// A service object of type <paramref name="serviceType"/>, or null if there is no service
		/// object of type <paramref name="serviceType"/>.
		/// </returns>
		public static object GetService(Type serviceType)
		{
			return Context.ServiceProvider.GetService(serviceType);
		}

		/// <summary>
		/// Closes this node and any of its children. Note that the node will not be saved before closing.
		/// </summary>
		public virtual void Close()
		{
			// Reset the cached cookie.
			this.SetDocumentCookie(DocumentInfo.NullCookie);
		}

		/// <summary>
		/// Deletes the file or folder from the disk. Folders will be recursively deleted.
		/// </summary>
		public virtual void Delete()
		{
			// Don't delete if we're not supposed to.
			if (!this.CanDelete)
			{
				return;
			}

			// First ask the user if he really wants to delete the item.
			PackageContext context = Package.Instance.Context;
			NativeResourceManager resources = context.NativeResources;
			string title = resources.GetString(ResourceId.IDS_DELETECONFIRMATION_TITLE, this.Caption);
			string message = resources.GetString(ResourceId.IDS_DELETECONFIRMATION, this.Caption);
			VsMessageBoxResult result = context.ShowMessageBox(title, message, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND, OLEMSGICON.OLEMSGICON_WARNING);
			if (result == VsMessageBoxResult.No)
			{
				return;
			}

			// Close the node first.
			this.Close();

			// Remove the node from the project.
			this.RemoveFromProject();

			// Attempt the delete the node from disk.
			try
			{
				if (this.IsFolder && Directory.Exists(this.AbsolutePath))
				{
					Directory.Delete(this.AbsolutePath, true);
				}
				else if (this.IsFile && File.Exists(this.AbsolutePath))
				{
					// Make sure the file is not read only, hidden, etc.
					File.SetAttributes(this.AbsolutePath, FileAttributes.Normal);
					File.Delete(this.AbsolutePath);
				}
			}
			catch (Exception e)
			{
				title = resources.GetString(ResourceId.IDS_E_DELETEFROMPROJECT_TITLE);
				message = resources.GetString(ResourceId.IDS_E_DELETEFROMPROJECT, e.Message);
				context.ShowErrorMessageBox(title, message);
			}
		}

		/// <summary>
		/// Occurs when a double click on the node or when Enter is pressed when the node is selected.
		/// </summary>
		public abstract void DoDefaultAction();

		/// <summary>
		/// Makes sure the node is visible in the Solution Explorer by expanding all of it's parents.
		/// </summary>
		public void EnsureVisible()
		{
			// Get the Solution Explorer window.
			IVsUIHierarchyWindow solutionExplorer = Package.Instance.Context.SolutionExplorer;

			// Make sure the node is visible by expanding all of its parents.
			EXPANDFLAGS flags = EXPANDFLAGS.EXPF_ExpandParentsToShowItem;
			NativeMethods.ThrowOnFailure(solutionExplorer.ExpandItem(this.Hierarchy, this.HierarchyId, flags));
		}

		/// <summary>
		/// Executes a command from the standard Visual Studio 97 command set.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <returns>true if the command is supported; otherwise, false.</returns>
		public virtual bool ExecuteStandard97Command(VsCommand command)
		{
			bool supported = true;

			if (command != VsCommand.SolutionCfg && command != VsCommand.SearchCombo)
			{
				Tracer.Assert(true, "Put breakpoint here if you want to debug a specific menu command or find out what menu commands Visual Studio is sending.");
			}

			switch (command)
			{
				// TODO: Implement cut, copy and paste.
				case VsCommand.Cut:
					break;

				case VsCommand.Copy:
					break;

				case VsCommand.Paste:
					break;

				case VsCommand.Delete:
					this.Delete();
					break;

				case VsCommand.Rename:
					this.StartNodeEdit();
					break;

				default:
					supported = false;
					break;
			}

			return supported;
		}

		/// <summary>
		/// Executes a command from the standard Visual Studio 2000 command set.
		/// </summary>
		/// <param name="command">The command to execute.</param>
		/// <returns>true if the command is supported; otherwise, false.</returns>
		public virtual bool ExecuteStandard2KCommand(VsCommand2K command)
		{
			bool supported = true;

			switch (command)
			{
				case VsCommand2K.EXCLUDEFROMPROJECT:
					this.RemoveFromProject();
					break;

				default:
					supported = false;
					break;
			}

			return supported;
		}

		/// <summary>
		/// Gets the value of the specified property.
		/// </summary>
		/// <param name="propertyId">The Id of the property to retrieve.</param>
		/// <param name="propertyValue">The value of the specified property, or null if the property is not supported.</param>
		/// <returns>true if the property is supported; otherwise false.</returns>
		public virtual bool GetProperty(__VSHPROPID propertyId, out object propertyValue)
		{
			propertyValue = null;
			bool supported = true;

			// Get the property from the node.
			switch (propertyId)
			{
				case __VSHPROPID.VSHPROPID_BrowseObject:
					if (this.Properties != null)
					{
						propertyValue = new DispatchWrapper(this.Properties);
					}
					break;

				case __VSHPROPID.VSHPROPID_Caption:
					propertyValue = this.Caption;
					break;

				case __VSHPROPID.VSHPROPID_ItemDocCookie:
					// We cast it to an IntPtr because some callers expect a VT_INT
					propertyValue = (IntPtr)this.DocumentCookie;
					break;

				case __VSHPROPID.VSHPROPID_EditLabel:
					if (this.CaptionEditable)
					{
						propertyValue = this.Caption;
					}
					else
					{
						supported = false;
					}
					break;

				case __VSHPROPID.VSHPROPID_Expandable:
					propertyValue = this.Expandable;
					break;

				case __VSHPROPID.VSHPROPID_ExpandByDefault:
					propertyValue = this.ExpandByDefault;
					break;

				case __VSHPROPID.VSHPROPID_Expanded:
					propertyValue = this.Expanded;
					break;

				case __VSHPROPID.VSHPROPID_FirstChild:
				case __VSHPROPID.VSHPROPID_FirstVisibleChild:
					propertyValue = (this.FirstChild != null ? this.FirstChild.HierarchyId : NativeMethods.VSITEMID_NIL);
					break;

				case __VSHPROPID.VSHPROPID_IconHandle:
					propertyValue = this.GetImageHandle(this.Image);
					if ((IntPtr)propertyValue == IntPtr.Zero)
					{
						supported = false;
						propertyValue = null;
					}
					break;

				case __VSHPROPID.VSHPROPID_Name:
					propertyValue = this.Caption;
					break;

				case __VSHPROPID.VSHPROPID_NextSibling:
				case __VSHPROPID.VSHPROPID_NextVisibleSibling:
					propertyValue = (this.NextSibling != null ? this.NextSibling.HierarchyId : NativeMethods.VSITEMID_NIL);
					break;

				case __VSHPROPID.VSHPROPID_Parent:
					propertyValue = (this.Parent != null ? this.Parent.HierarchyId : NativeMethods.VSITEMID_NIL);
					break;

				case __VSHPROPID.VSHPROPID_SaveName:
					propertyValue = this.SaveName;
					break;

				case __VSHPROPID.VSHPROPID_TypeGuid:
					propertyValue = this.VisualStudioTypeGuid;
					break;

				default:
					supported = false;
					break;
			}

			return supported;
		}

		/// <summary>
		/// Does the actual work of changing the caption after all of the verifications have been done
		/// that it's Ok to move the file or folder.
		/// </summary>
		/// <param name="newCaption">The new caption.</param>
		/// <param name="newPath">The new absolute path.</param>
		public virtual void MoveNodeOnCaptionChange(string newCaption, string newPath)
		{
			throw new NotSupportedException("If your node supports a caption change, please override this method");
		}

		/// <summary>
		/// Queries the state of a command from the standard Visual Studio 2000 command set on this node.
		/// </summary>
		/// <param name="command">The command to query.</param>
		/// <returns>One of the <see cref="CommandStatus"/> values if the node handles the command;
		/// otherwise <see cref="CommandStatus.Unhandled"/>.</returns>
		public virtual CommandStatus QueryStandard2KCommandStatus(VsCommand2K command)
		{
			CommandStatus status = CommandStatus.Unhandled;

			switch (command)
			{
				case VsCommand2K.EXCLUDEFROMPROJECT:
					if (this.CanRemoveFromProject)
					{
						status = CommandStatus.SupportedAndEnabled;
					}
					else
					{
						status = CommandStatus.NotSupportedOrEnabled;
					}
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
		public virtual CommandStatus QueryStandard97CommandStatus(VsCommand command)
		{
			CommandStatus status = CommandStatus.Unhandled;

			switch (command)
			{
				case VsCommand.Cut:
				case VsCommand.Copy:
					if (!this.IsVirtual)
					{
						status = CommandStatus.SupportedAndEnabled;
					}
					break;

				case VsCommand.Delete:
					if (this.CanDelete)
					{
						status = CommandStatus.SupportedAndEnabled;
					}
					else
					{
						status = CommandStatus.NotSupportedOrEnabled;
					}
					break;

				case VsCommand.NewFolder:
				case VsCommand.AddNewItem:
				case VsCommand.AddExistingItem:
				case VsCommand.Paste:
					if (this.IsFolder)
					{
						status = CommandStatus.SupportedAndEnabled;
					}
					break;

				case VsCommand.Open:
				case VsCommand.OpenWith:
					if (this.IsFile)
					{
						status = CommandStatus.SupportedAndEnabled;
					}
					break;

				case VsCommand.Rename:
					if (this.CaptionEditable)
					{
						status = CommandStatus.SupportedAndEnabled;
					}
					break;
			}

			return status;
		}

		/// <summary>
		/// Removes the node from the project.
		/// </summary>
		/// <remarks>By default, this does nothing.</remarks>
		public virtual void RemoveFromProject()
		{
			// Don't do anything if we can't remove the node.
			if (!this.CanRemoveFromProject)
			{
				return;
			}

			FolderNode parentFolder = this.Parent;

			// We'd better have a parent.
			if (parentFolder == null)
			{
				Tracer.Fail("Cannot remove the root node from the project.");
				return;
			}

			// Our parent better have children.
			if (parentFolder.Children == null)
			{
				Tracer.Fail("This node's ({0}) parent ({1}) should have a non-null Children collection.", this.ToString(), parentFolder.ToString());
				return;
			}

			// Before we remove ourself from the hierachy, make sure that we're not selected.
			// If we have to, we'll move the selection to the previous sibling.
			Node nodeToSelect = null;
			if (this.Selected)
			{
				// If the previous sibling is null, then select the root node.
				if (this.PreviousSibling != null)
				{
					nodeToSelect = this.PreviousSibling;
				}
				else
				{
					nodeToSelect = this.Hierarchy.RootNode;
				}
			}

			// Remove ourself from the parent.
			parentFolder.Children.Remove(this);

			// Now select the node. We have to do it here because the removal causes a refresh
			// on the hierarchy, which will select the root node by default.
			if (nodeToSelect != null)
			{
				nodeToSelect.Select();
			}
		}

		/// <summary>
		/// Saves the document associated with this node if it is dirty.
		/// </summary>
		public virtual void Save()
		{
		}

		/// <summary>
		/// Selects the node in the Solution Explorer.
		/// </summary>
		public void Select()
		{
			// Get the Solution Explorer window.
			IVsUIHierarchyWindow solutionExplorer = Package.Instance.Context.SolutionExplorer;

			// Select the node in the Solution Explorer.
			EXPANDFLAGS flags = EXPANDFLAGS.EXPF_SelectItem;
			NativeMethods.ThrowOnFailure(solutionExplorer.ExpandItem(this.Hierarchy, this.HierarchyId, flags));
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
		public virtual int SetCaption(string value)
		{
			if (String.IsNullOrEmpty(value))
			{
				throw new ArgumentException(SconceStrings.ErrorBlankCaption, "value");
			}

			string directory = Path.GetDirectoryName(PackageUtility.StripTrailingChar(this.AbsolutePath, Path.DirectorySeparatorChar));
			string newPath = this.CanonicalizePath(Path.Combine(directory, value));

			// Do a bunch of verifications before we do anything serious.
			if (!this.VerifyCaption(value, newPath))
			{
				return NativeMethods.S_OK;
			}

			this.MoveNodeOnCaptionChange(value, newPath);

			// At this point we know we've removed the old node and re-added a new one.
			// We need to return S_FALSE here, which tells Visual Studio to flush the old hierarchy id.
			return NativeMethods.S_FALSE;
		}

		/// <summary>
		/// Tells the Solution Explorer to allow the user to start editing the node.
		/// </summary>
		public void StartNodeEdit()
		{
			// Get the Solution Explorer window.
			IVsUIHierarchyWindow solutionExplorer = Package.Instance.Context.SolutionExplorer;

			// Once we have the new name, we'll go into edit label mode on the node. Checks for
			// validity will happen in the SetCaption method.

			// First make sure the node is visible.
			this.EnsureVisible();

			// Select the node in the Solution Explorer.
			this.Select();

			// Edit the node's caption.
			NativeMethods.ThrowOnFailure(solutionExplorer.ExpandItem(this.Hierarchy, this.HierarchyId, EXPANDFLAGS.EXPF_EditItemLabel));
		}

		/// <summary>
		/// Sets the value of the specified property on this node.
		/// </summary>
		/// <param name="propertyId">The Id of the property to set.</param>
		/// <param name="value">The value to set.</param>
		/// <returns>
		/// An HRESULT that is the result of the operation. Usually S_OK indicates that the property was set.
		/// If the property is not supported, returns DISP_E_MEMBERNOTFOUND.
		/// </returns>
		public virtual int SetProperty(__VSHPROPID propertyId, object value)
		{
			int hr = NativeMethods.S_OK;

			// Get the property from the node.
			switch (propertyId)
			{
				case __VSHPROPID.VSHPROPID_Caption:
				case __VSHPROPID.VSHPROPID_EditLabel:
					if (this.CaptionEditable)
					{
						hr = this.SetCaption((string)value);
					}
					break;

				case __VSHPROPID.VSHPROPID_Expanded:
					this.Expanded = (bool)value;
					break;

				case __VSHPROPID.VSHPROPID_ItemDocCookie:
					this.SetDocumentCookie((uint)value);
					break;

				default:
					hr = NativeMethods.DISP_E_MEMBERNOTFOUND;
					break;
			}

			return hr;
		}

		/// <summary>
		/// Shows the context menu when a user right-mouse clicks on the node.
		/// </summary>
		public virtual void ShowContextMenu()
		{
			int menuId = (int)this.VisualStudioContextMenuId;

			if (menuId != VsMenus.IDM_VS_CTXT_NOCOMMANDS)
			{
				// Tell the Visual Studio shell to show the context menu.
				Point menuLocation = Cursor.Position;
				POINTS[] vsPoints = new POINTS[1];
				vsPoints[0].x = (short)menuLocation.X;
				vsPoints[0].y = (short)menuLocation.Y;
				Guid activeMenuGuid = VsMenus.SHLMainMenu;
				IVsUIShell uiShell = this.ServiceProvider.GetVsUIShell(classType, "ShowContextMenu");
				int hr = uiShell.ShowContextMenu(0, ref activeMenuGuid, menuId, vsPoints, this.Hierarchy);
				if (NativeMethods.Failed(hr))
				{
					Tracer.Fail("Error in showing the context menu: 0x{0:x}", hr);
					NativeMethods.ThrowOnFailure(hr);
				}
			}
		}

		/// <summary>
		/// Returns the node's caption.
		/// </summary>
		/// <returns>The node's caption.</returns>
		public override string ToString()
		{
			return this.Caption;
		}

		/// <summary>
		/// Canonicalizes the specified path as a file path. If it should be canonicalized as a folder path
		/// then the subclass should override.
		/// </summary>
		/// <param name="absolutePath">The path to canonicalize.</param>
		/// <returns>The canonicalized path.</returns>
		protected virtual string CanonicalizePath(string absolutePath)
		{
			return PackageUtility.CanonicalizeFilePath(absolutePath);
		}

		/// <summary>
		/// Gets a handle to the specified image.
		/// </summary>
		/// <param name="image"><see cref="Image"/> object from which to retrieve a handle. Can be null.</param>
		/// <returns>A handle to <paramref name="image"/> or <see cref="IntPtr.Zero"/> if
		/// <paramref name="image"/> is null.</returns>
		protected IntPtr GetImageHandle(Image image)
		{
			IntPtr handle = IntPtr.Zero;
			Bitmap bitmap = image as Bitmap;
			if (bitmap != null)
			{
				handle = bitmap.GetHicon();
			}
			return handle;
		}

		/// <summary>
		/// Calls the <b>Hierarchy.OnPropertyChanged</b> method.
		/// </summary>
		/// <param name="propId">The property Id that changed.</param>
		protected void OnPropertyChanged(__VSHPROPID propId)
		{
			this.MakeDirty();
			this.Hierarchy.OnPropertyChanged(this, propId);
		}

		/// <summary>
		/// Sets the document cookie, which is an index into the environment's RDT (Running Document Table).
		/// </summary>
		/// <param name="value">The document cookie.</param>
		protected void SetDocumentCookie(uint value)
		{
			this.documentCookie = value;
		}

		/// <summary>
		/// Sets the unique identifier for the node. This should only be used during a node caption change.
		/// </summary>
		/// <param name="value">The new hiearchy id value.</param>
		protected void SetHierarchyId(uint value)
		{
			this.hierarchyId = value;
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
		protected virtual bool VerifyCaption(string newCaption, string newPath)
		{
			if (!this.CaptionEditable)
			{
				Tracer.Fail("SetCaption should not be called when the caption is not editable.");
				return false;
			}

			// If the caption is exactly the same, then there's no need to do anything.
			bool differInCaseOnly;
			if (PackageUtility.FileStringEquals(this.Caption, newCaption, out differInCaseOnly) && !differInCaseOnly)
			{
				return false;
			}

			// Make sure the new caption is a valid file name.
			if (!PackageUtility.IsValidFileOrFolderName(newCaption))
			{
				throw new InvalidOperationException(SconceStrings.ErrorInvalidFileOrFolderName);
			}

			// If the old and the new caption differ in just case, then we won't do any
			// file moving since the file system is case-insensitive. We do want to allow
			// users to change the case on their captions, though.
			if (differInCaseOnly)
			{
				this.OnPropertyChanged(__VSHPROPID.VSHPROPID_Caption);
				return false;
			}

			// Make sure the file doesn't already exist in the hierarchy or the file system.
			this.VerifyCaptionDoesNotExist(newCaption, newPath);

			// We don't want to do anything if we don't own the document.
			if (this.DocumentCookie != DocumentInfo.NullCookie && this.DocumentCookie != this.Hierarchy.RootNode.DocumentCookie)
			{
				DocumentInfo docInfo = Context.RunningDocumentTable.FindByCookie(this.DocumentCookie);
				if (docInfo.VisualStudioHierarhcy == null || !PackageUtility.IsSameComObject(docInfo.VisualStudioHierarhcy, (Hierarchy)this.Hierarchy))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Checks whether the proposed caption already exists, meaning that there is not an existing file
		/// or folder at the root path or that there is not already a sibling hierarchy item with the same name.
		/// </summary>
		/// <param name="newCaption">The proposed caption.</param>
		/// <param name="newPath">The proposed new absolute file path.</param>
		/// <remarks>The method throws an exception if the caption already exists.</remarks>
		private void VerifyCaptionDoesNotExist(string newCaption, string newPath)
		{
			bool valid = true;

			// Make sure there isn't already a sibling with the same caption. The root node has no siblings.
			if (this.Parent != null)
			{
				foreach (Node sibling in this.Parent.Children)
				{
					bool thisIsSibling = Object.ReferenceEquals(sibling, this);
					bool captionsEqual = (PackageUtility.FileStringEquals(newCaption, sibling.Caption));
					// We can have a file system node that is the same name as a virtual node.
					// For example, we can name a file/folder "Library References" if we want,
					// even though that is already in the hierarchy.
					bool isExactlyOneVirtual = ((this.IsVirtual && !sibling.IsVirtual) || (!this.IsVirtual && sibling.IsVirtual));
					if (!thisIsSibling && captionsEqual && !isExactlyOneVirtual)
					{
						valid = false;
						break;
					}
				}
			}

			if (valid)
			{
				// Now check to see if the file system already contains a file/folder by the same name.
				valid = ((this.IsFile && !File.Exists(newPath)) || (this.IsFolder && !Directory.Exists(newPath)));
			}

			if (!valid)
			{
				Tracer.WriteLineInformation(classType, "VerifyCaption", "An existing file or folder named '{0}' already exists on the disk.", newCaption);
				throw new InvalidOperationException(SconceStrings.ErrorItemAlreadyExistsOnDisk(newCaption));
			}
		}
		#endregion
	}
}
