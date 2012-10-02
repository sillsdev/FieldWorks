//-------------------------------------------------------------------------------------------------
// <copyright file="FileNode.cs" company="Microsoft">
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
// A file node within a Solution Explorer hierarchy.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Drawing;
	using System.IO;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.Shell.Interop;

	public class FileNode : Node
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(FileNode);
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public FileNode(Hierarchy hierarchy, string absolutePath) : base(hierarchy, absolutePath)
		{
		}

		public FileNode(Hierarchy hierarchy, string absolutePath, BuildAction buildAction) :
			base(hierarchy, absolutePath, buildAction)
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		public override Image Image
		{
			get
			{
				// If we return null, the OS will use the image of the registered file extension.
				return null;
			}
		}

		public override bool IsFile
		{
			get { return true; }
		}

		public override bool IsFolder
		{
			get { return false; }
		}

		public override bool IsVirtual
		{
			get { return false; }
		}

		/// <summary>
		/// Gets the node's properties to show in the Property window.
		/// </summary>
		public override NodeProperties Properties
		{
			get { return new FileNodeProperties(this); }
		}

		public override VsMenus.ContextMenuId VisualStudioContextMenuId
		{
			get { return VsMenus.ContextMenuId.IDM_VS_CTXT_ITEMNODE; }
		}

		public override Guid VisualStudioTypeGuid
		{
			get { return NativeMethods.GUID_ItemType_PhysicalFile; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Closes the document that this node represents. Does not save the document before closing.
		/// </summary>
		public override void Close()
		{
			DocumentInfo docInfo = Context.RunningDocumentTable.FindByPath(this.AbsolutePath);
			// We only want to close the file if it's open and our hierarchy owns it.
			if (docInfo != null && docInfo.IsOpen && docInfo.VisualStudioHierarhcy == this.Hierarchy)
			{
				// We have to retrieve the window frame so we can close it. We do that through
				// querying IVsUIShellOpenDocument.IsDocumentOpen.
				IVsUIShellOpenDocument shellOpenDoc = (IVsUIShellOpenDocument)this.Hierarchy.ServiceProvider.GetServiceOrThrow(typeof(SVsUIShellOpenDocument), typeof(IVsUIShellOpenDocument), classType, "Close");

				// These are all of the out parameters to the shell call.
				Guid logicalView = Guid.Empty;
				IVsUIHierarchy openDocUIHierarchy;
				uint[] openDocHierarchyId = new uint[1];
				IVsWindowFrame openDocWindowFrame;
				int isOpen;

				// Make the shell call to ultimately get the window frame.
				int hr = shellOpenDoc.IsDocumentOpen(this.Hierarchy, this.HierarchyId, this.AbsolutePath, ref logicalView, 0, out openDocUIHierarchy, openDocHierarchyId, out openDocWindowFrame, out isOpen);
				NativeMethods.ThrowOnFailure(hr);

				// Close the window frame.
				if (openDocWindowFrame != null)
				{
					hr = openDocWindowFrame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
					NativeMethods.ThrowOnFailure(hr);
				}
			}
			base.Close();
		}

		/// <summary>
		/// Occurs when a double click on the node or when Enter is pressed when the node is selected.
		/// </summary>
		public override void DoDefaultAction()
		{
			this.Open();
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
				case VsCommand.ViewCode:
					this.Open(VsLogicalView.Code);
					break;

				case VsCommand.Open:
					this.Open();
					break;

				default:
					supported = base.ExecuteStandard97Command(command);
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
			string oldCaption = this.Caption;
			string oldPath = this.AbsolutePath;
			string oldRelativePath = this.RelativePath;
			bool updatedWindowCaptions = false;
			bool removedNode = false;
			Node newNode = null;

			// If we are currently selected, cache the value so we can restore the selection
			// after the addition.
			bool wasSelected = this.Selected;

			// Tell the environment to stop listening to file change events on the old file.
			using (FileChangeNotificationSuspender notificationSuspender = new FileChangeNotificationSuspender(oldPath))
			{
				// Make sure the environment says we can start the rename.
				if (!this.Hierarchy.AttachedProject.Tracker.CanRenameFile(oldPath, newPath))
				{
					return;
				}

				// Move the file on the file system to match the new name.
				if (!this.IsVirtual && File.Exists(oldPath) && !File.Exists(newPath))
				{
					Tracer.WriteLineInformation(classType, "MoveNodeOnCaptionChange", "Renaming the file '{0}' to '{1}'.", oldPath, newPath);
					string newDirectoryName = Path.GetDirectoryName(newPath);
					if (!Directory.Exists(newDirectoryName))
					{
						Directory.CreateDirectory(newDirectoryName);
					}
					File.Move(oldPath, newPath);
				}

				try
				{
					// Update all of the windows that currently have this file opened in an editor.
					this.UpdateOpenWindowCaptions(newCaption);
					updatedWindowCaptions = true;

					// We have to remove the node and re-add it so that we can have the sorting preserved.
					// Also, if the extension has changed then we'll have to recreate a new type-specific
					// FileNode. The easy way is to remove ourself from the project then tell the project
					// to add an existing file.

					string newRelativePath = PackageUtility.MakeRelative(this.Hierarchy.RootNode.AbsoluteDirectory, newPath);

					// Remove ourself from the hierarchy.
					this.Parent.Children.Remove(this);
					removedNode = true;

					// We have now been removed from the hierarchy. Do NOT call any virtual methods or
					// methods that depend on our state after this point.

					// Re-add ourself as a new incarnation (different object). Our life ends here.
					newNode = this.Hierarchy.AddExistingFile(newRelativePath, true);

					if (newNode != null)
					{
						// We need to set our hierarchy Id to match the new hierachy Id in case Visual Studio
						// calls back into us for something.
						this.SetHierarchyId(newNode.HierarchyId);

						// Select the new node if we were previously selected.
						if (wasSelected)
						{
							newNode.Select();
						}

						// Tell the RDT to rename the document.
						Context.RunningDocumentTable.RenameDocument(oldPath, newPath, newNode.HierarchyId);
					}
				}
				catch (Exception e)
				{
					if (ErrorUtility.IsExceptionUnrecoverable(e))
					{
						throw;
					}

					// Rollback the file move
					Tracer.WriteLineWarning(classType, "MoveNodeOnCaptionChange", "There was an error in renaming the document. Exception: {0}", e);
					File.Move(newPath, oldPath);

					// Remove the node that we just added.
					if (newNode != null)
					{
						newNode.RemoveFromProject();
					}

					// Re-add a new node since we've already removed the old node.
					if (removedNode || this.Parent == null)
					{
						newNode = this.Hierarchy.AddExistingFile(oldRelativePath, true);
						this.SetHierarchyId(newNode.HierarchyId);
						if (wasSelected)
						{
							newNode.Select();
						}
					}

					// Rollback the caption update on open windows
					if (updatedWindowCaptions)
					{
						this.UpdateOpenWindowCaptions(oldCaption);
					}

					// Rethrow the exception
					throw;
				}

				// Tell the environment that we're done renaming the document.
				this.Hierarchy.AttachedProject.Tracker.OnFileRenamed(oldPath, newPath);

				// Update the property browser.
				IVsUIShell vsUIShell = (IVsUIShell)this.Hierarchy.ServiceProvider.GetServiceOrThrow(typeof(SVsUIShell), typeof(IVsUIShell), classType, "MoveNodeOnCaptionChange");
				vsUIShell.RefreshPropertyBrowser(0);
			}
		}

		/// <summary>
		/// Opens the standard editor for this file type in Visual Studio.
		/// </summary>
		/// <returns>The <see cref="IVsWindowFrame"/> object that contains the opened document.</returns>
		public virtual IVsWindowFrame Open()
		{
			return this.Open(VsLogicalView.Primary);
		}

		/// <summary>
		/// Opens the standard editor for this file type in Visual Studio.
		/// </summary>
		/// <param name="logicalView">The type of view in which to open the document.</param>
		/// <returns>The <see cref="IVsWindowFrame"/> object that contains the opened document.</returns>
		public IVsWindowFrame Open(VsLogicalView logicalView)
		{
			return this.OpenWithStandardEditor(logicalView, NativeMethods.DOCDATAEXISTING_Unknown);
		}

		/// <summary>
		/// Opens the standard editor for this file type in Visual Studio.
		/// </summary>
		/// <param name="logicalView">The type of view in which to open the document.</param>
		/// <param name="existingDocumentData">
		/// Passed through to the IVsUIShellOpenDocument.OpenStandardEditor or OpenSpecificEditor, which
		/// will then determine if the document is already opened and reused the open window.
		/// </param>
		/// <param name="physicalView">
		/// Name of the physical view if we're opening with a specific editor. Not used if opening with a standard editor.
		/// </param>
		/// <param name="specificEditor">The GUID of the specific registered editor to use to open this node.</param>
		/// <returns>The <see cref="IVsWindowFrame"/> object that contains the opened document.</returns>
		public IVsWindowFrame OpenWithSpecificEditor(VsLogicalView logicalView, IntPtr existingDocumentData, string physicalView, Guid specificEditor)
		{
			return this.Open(logicalView, existingDocumentData, specificEditor, physicalView);
		}

		/// <summary>
		/// Opens the standard editor for this file type in Visual Studio.
		/// </summary>
		/// <param name="logicalView">The type of view in which to open the document.</param>
		/// <param name="existingDocumentData">
		/// Passed through to the IVsUIShellOpenDocument.OpenStandardEditor or OpenSpecificEditor, which
		/// will then determine if the document is already opened and reused the open window.
		/// </param>
		/// <returns>The <see cref="IVsWindowFrame"/> object that contains the opened document.</returns>
		public IVsWindowFrame OpenWithStandardEditor(VsLogicalView logicalView, IntPtr existingDocumentData)
		{
			return this.Open(logicalView, existingDocumentData, Guid.Empty, null);
		}

		/// <summary>
		/// Saves the document associated with this node if it is dirty.
		/// </summary>
		public override void Save()
		{
			Context.RunningDocumentTable.SaveIfDirty(this.AbsolutePath);
		}

		/// <summary>
		/// Updates all of the open windows that are editing this node's files to update their captions.
		/// </summary>
		/// <param name="newCaption">The new caption.</param>
		protected void UpdateOpenWindowCaptions(string newCaption)
		{
			// Get the environment's UI shell in preparation for enumerating the window frames.
			IVsUIShell uiShell = this.ServiceProvider.GetVsUIShell(classType, "UpdateOpenWindowCaptions");
			IEnumWindowFrames enumerator;
			NativeMethods.ThrowOnFailure(uiShell.GetDocumentWindowEnum(out enumerator));
			IVsWindowFrame[] windowFrames = new IVsWindowFrame[1];
			uint fetchCount;

			// Get the document data for this node (don't find by cookie because the document cookie
			// could have changed on a rename).
			DocumentInfo docInfo = Package.Instance.Context.RunningDocumentTable.FindByPath(this.AbsolutePath);

			if (docInfo == null)
			{
				// There's no need to rename any captions if the document isn't open.
				return;
			}

			IntPtr docData = Marshal.GetIUnknownForObject(docInfo.DocumentData);

			try
			{
				// Tell all of the windows to update their caption to the new file name.
				while (enumerator.Next(1, windowFrames, out fetchCount) == NativeMethods.S_OK && fetchCount == 1)
				{
					IVsWindowFrame windowFrame = windowFrames[0];
					object documentDataObject;
					NativeMethods.ThrowOnFailure(windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out documentDataObject));
					IntPtr pUnknownDocData = Marshal.GetIUnknownForObject(documentDataObject);
					try
					{
						// We found a window frame that contains the document to rename.
						if (pUnknownDocData == docData)
						{
							NativeMethods.ThrowOnFailure(windowFrame.SetProperty((int)__VSFPROPID.VSFPROPID_OwnerCaption, newCaption));
						}
					}
					finally
					{
						if (pUnknownDocData != IntPtr.Zero)
						{
							Marshal.Release(pUnknownDocData);
						}
					}
				}
			}
			finally
			{
				if (docData != IntPtr.Zero)
				{
					Marshal.Release(docData);
				}
			}
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

			// See if the user is changing the extension. If so, ask if he really wants to do that.
			// TODO: When I add automation support, then we need to check to see if we're in automation mode before showing a dialog.
			string oldPath = this.AbsolutePath;
			string oldExtension = Path.GetExtension(oldPath);
			string newExtension = Path.GetExtension(newPath);
			if (!PackageUtility.FileStringEquals(oldExtension, newExtension))
			{
				// If the user decides that he doesn't want to change the extension, then we just don't do anything.
				if (!Context.PromptYesNo(null, SconceStrings.PromptChangeExtension, OLEMSGICON.OLEMSGICON_INFO))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Opens the standard editor for this file type in Visual Studio.
		/// </summary>
		/// <param name="logicalView">The type of view in which to open the document.</param>
		/// <param name="existingDocumentData">
		/// Passed through to the IVsUIShellOpenDocument.OpenStandardEditor or OpenSpecificEditor, which
		/// will then determine if the document is already opened and reused the open window.
		/// </param>
		/// <param name="physicalView">
		/// Name of the physical view if we're opening with a specific editor. Not used if opening with a standard editor.
		/// </param>
		/// <param name="specificEditor">The GUID of the specific registered editor to use to open this node.</param>
		/// <returns>The <see cref="IVsWindowFrame"/> object that contains the opened document.</returns>
		private IVsWindowFrame Open(VsLogicalView logicalView, IntPtr existingDocumentData, Guid specificEditor, string physicalView)
		{
			Tracer.VerifyNonNullArgument(logicalView, "logicalView");

			// Check to see if the file exists before we try to open it.
			if (!File.Exists(this.AbsolutePath))
			{
				Context.ShowErrorMessageBox(SconceStrings.FileDoesNotExist(this.AbsolutePath));
				return null;
			}

			IVsWindowFrame windowFrame;
			Guid logicalViewGuid = logicalView.Value;
			Guid editorTypeGuid = specificEditor;
			bool useSpecificEditor = (specificEditor != Guid.Empty);
			int hr;

			// Get a IVsUIShellOpenDocument object so that we can use it to open the document.
			IVsUIShellOpenDocument vsUIShellOpenDocument = (IVsUIShellOpenDocument)this.ServiceProvider.GetServiceOrThrow(typeof(SVsUIShellOpenDocument), typeof(IVsUIShellOpenDocument), classType, "Open");

			// Open the document.
			if (useSpecificEditor)
			{
				hr = vsUIShellOpenDocument.OpenSpecificEditor(
					0,
					this.CanonicalName,
					ref editorTypeGuid,
					physicalView,
					ref logicalViewGuid,
					this.Caption,
					(IVsUIHierarchy)this.Hierarchy,
					this.HierarchyId,
					existingDocumentData,
					(Microsoft.VisualStudio.OLE.Interop.IServiceProvider)Package.Instance,
					out windowFrame);
			}
			else
			{
				hr = vsUIShellOpenDocument.OpenStandardEditor(
					unchecked((uint)__VSOSEFLAGS.OSE_ChooseBestStdEditor),
					this.CanonicalName,
					ref logicalViewGuid,
					this.Caption,
					(IVsUIHierarchy)this.Hierarchy,
					this.HierarchyId,
					existingDocumentData,
					(Microsoft.VisualStudio.OLE.Interop.IServiceProvider)Package.Instance,
					out windowFrame);
			}

			string editorTypeName = useSpecificEditor ? "specific" : "standard";
			if (NativeMethods.Succeeded(hr))
			{
				Tracer.WriteLineInformation(classType, "Open", "Succeeded in opening '{0}' with a {1} editor.", this.AbsolutePath, editorTypeName);
				if (windowFrame != null)
				{
					// Get the document cookie and cache it.
					object pvar;
					hr = windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocCookie, out pvar);
					NativeMethods.ThrowOnFailure(hr);
					// pvar is an int, but we need a uint. We get an error if we try to immediately cast to uint
					// without first casting to an int.
					uint cookie = unchecked((uint)(int)pvar);
					this.SetDocumentCookie(cookie);
					Tracer.WriteLineInformation(classType, "Open", "Document '{0}' has a cookie value of {1}", this.AbsolutePath, cookie);

					// Show the window frame of the open document. The documentation says we don't need to do this, but the reality is different.
					hr = windowFrame.Show();
					Tracer.Assert(NativeMethods.Succeeded(hr), "Error in IVsWindowFrame.Show(): 0x{0:x}", hr);

					// Trace the running documents.
					VsHelperMethods.TraceRunningDocuments();
				}
				else
				{
					Tracer.Fail("Open succeeded but we were returned a null IVsWindowFrame so we can't show the document.");
				}
			}
			else if (hr == NativeMethods.OLE_E_PROMPTSAVECANCELLED)
			{
				Tracer.WriteLineInformation(classType, "Open", "The user canceled out of the open dialog box.");
			}
			else
			{
				Tracer.Fail("Failed to open '{0}' with a {1} editor. Hr=0x{2:x}", this.AbsolutePath, editorTypeName, hr);
				NativeMethods.ThrowOnFailure(hr);
			}

			return windowFrame;
		}
		#endregion
	}
}
