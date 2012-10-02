//-------------------------------------------------------------------------------------------------
// <copyright file="Project.cs" company="Microsoft">
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
// A Visual Studio project.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using System.Text;
	using Microsoft.VisualStudio.OLE.Interop;
	using Microsoft.VisualStudio.Shell.Interop;

	using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
	using ResId = ResourceId;

	/// <summary>
	/// Represents a Visual Studio project.
	/// </summary>
	[Guid("70622390-10A6-4ECB-B8A9-2FA7B3F7DAF5")]
	public class Project :
		Hierarchy,
		IVsProject,
		IVsProject2,
		IVsProject3,
		IPersistFileFormat,
		IVsGetCfgProvider//,
//        IVsSccManager2,
//        IVsProjectStartupServices,
//        IVsUIHierWinClipboardHelper,
//        IVsHierarchyDropDataSource2,
//        IVsHierarchyDropDataTarget
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(Project);
		private static Guid projectTypeGuid = Guid.Empty;

		private Guid projectGuid = Guid.NewGuid();
		private BuildSettings buildSettings;
		private ConfigurationProvider configurationProvider;
		private ProjectSerializer serializer;
		private ProjectDocumentsTracker tracker;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="Project"/> class.
		/// </summary>
		/// <param name="serializer">The serializer to use for saving the project.</param>
		public Project(ProjectSerializer serializer) : this(serializer, new BuildSettings())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Project"/> class.
		/// </summary>
		/// <param name="serializer">The serializer to use for saving the project.</param>
		/// <param name="buildSettings">Contains build-related settings.</param>
		protected Project(ProjectSerializer serializer, BuildSettings buildSettings)
		{
			Tracer.VerifyNonNullArgument(serializer, "serializer");
			Tracer.VerifyNonNullArgument(buildSettings, "buildSettings");
			this.serializer = serializer;
			this.buildSettings = buildSettings;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the <see cref="Project"/> that is attached to the hierarchy, which is this object.
		/// </summary>
		public override Project AttachedProject
		{
			get { return this; }
		}

		/// <summary>
		/// Gets or sets the build settings for this project.
		/// </summary>
		public virtual BuildSettings BuildSettings
		{
			get { return this.buildSettings; }
			set
			{
				Tracer.VerifyNonNullArgument(value, "BuildSettings");
				if (this.BuildSettings != value)
				{
					this.buildSettings = value;
					this.MakeDirty();
				}
			}
		}

		/// <summary>
		/// Gets an array of property page GUIDs that are common, or not dependent upon the configuration.
		/// </summary>
		public virtual Guid[] CommonPropertyPageGuids
		{
			get
			{
				return new Guid[] { typeof(GeneralPropertyPage).GUID };
			}
		}

		/// <summary>
		/// Gets an array of property page GUIDs that are configuration dependent.
		/// </summary>
		public virtual Guid[] ConfigurationDependentPropertyPageGuids
		{
			get { return null; }
		}

		/// <summary>
		/// Gets or sets the project's <see cref="ConfigurationProvider"/>.
		/// </summary>
		public virtual ConfigurationProvider ConfigurationProvider
		{
			get
			{
				if (this.configurationProvider == null)
				{
					this.configurationProvider = new ConfigurationProvider(this);
				}
				return this.configurationProvider;
			}

			set
			{
				Tracer.VerifyNonNullArgument(value, "ConfigurationProvider");
				if (this.ConfigurationProvider != value)
				{
					this.configurationProvider = value;
					this.MakeDirty();
				}
			}
		}

		public string FilePath
		{
			get { return this.RootNode.AbsolutePath; }
			set { this.RootNode.AbsolutePath = value; }
		}

		public string Name
		{
			get { return Path.GetFileNameWithoutExtension(this.FilePath); }
		}

		/// <summary>
		/// Gets or sets the GUID for the project, which is used in project serializing.
		/// </summary>
		public Guid ProjectGuid
		{
			get { return this.projectGuid; }
			set
			{
				if (this.ProjectGuid != value)
				{
					this.projectGuid = value;
					this.MakeDirty();
				}
			}
		}

		/// <summary>
		/// Gets the project type GUID, which is registered with Visual Studio.
		/// </summary>
		public virtual Guid ProjectTypeGuid
		{
			get
			{
				if (projectTypeGuid == Guid.Empty)
				{
					// Read the GuidAttribute from this object, but don't look for
					// inherited attributes. Each project should have a distinct
					// GUID that is registered with Visual Studio.
					object[] attributes = this.GetType().GetCustomAttributes(typeof(GuidAttribute), false);
					if (attributes == null || attributes.Length == 0)
					{
						Tracer.Fail("{0} needs to define a GuidAttribute on the class.", this.GetType().FullName);
					}
					else
					{
						projectTypeGuid = new Guid(((GuidAttribute)attributes[0]).Value);
					}
				}
				return projectTypeGuid;
			}
		}

		public string RootDirectory
		{
			get { return this.RootNode.AbsoluteDirectory; }
		}

		public virtual ProjectSerializer Serializer
		{
			get { return this.serializer; }
		}

		/// <summary>
		/// Gets the wrapper around the IVsTrackProjectDocuments2 interface.
		/// </summary>
		public ProjectDocumentsTracker Tracker
		{
			get
			{
				if (this.tracker == null)
				{
					this.tracker = new ProjectDocumentsTracker(this);
				}
				return this.tracker;
			}
		}

		public bool Unavailable
		{
			get { return this.RootNode.Unavailable; }
			set { this.RootNode.Unavailable = value; }
		}

		/// <summary>
		/// Returns a value indicating whether one or more contained <see cref="IDirtyable"/> objects
		/// are dirty.
		/// </summary>
		protected override bool AreContainedObjectsDirty
		{
			get { return (base.AreContainedObjectsDirty || this.BuildSettings.IsDirty || this.ConfigurationProvider.IsDirty); }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		#region IVsProject Members
		int IVsProject.IsDocumentInProject(string pszMkDocument, out int pfFound, VSDOCUMENTPRIORITY[] pdwPriority, out uint pitemid)
		{
			return ((IVsProject3)this).IsDocumentInProject(pszMkDocument, out pfFound, pdwPriority, out pitemid);
		}

		int IVsProject.GetMkDocument(uint itemid, out string pbstrMkDocument)
		{
			return ((IVsProject3)this).GetMkDocument(itemid, out pbstrMkDocument);
		}

		int IVsProject.OpenItem(uint itemid, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
		{
			return ((IVsProject3)this).OpenItem(itemid, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame);
		}

		int IVsProject.GetItemContext(uint itemid, out IOleServiceProvider ppSP)
		{
			return ((IVsProject3)this).GetItemContext(itemid, out ppSP);
		}

		int IVsProject.GenerateUniqueItemName(uint itemidLoc, string pszExt, string pszSuggestedRoot, out string pbstrItemName)
		{
			return ((IVsProject3)this).GenerateUniqueItemName(itemidLoc, pszExt, pszSuggestedRoot, out pbstrItemName);
		}

		int IVsProject.AddItem(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, VSADDRESULT[] pResult)
		{
			return ((IVsProject3)this).AddItem(itemidLoc, dwAddItemOperation, pszItemName, cFilesToOpen, rgpszFilesToOpen, hwndDlgOwner, pResult);
		}
		#endregion

		#region IVsProject2 Members
		int IVsProject2.IsDocumentInProject(string pszMkDocument, out int pfFound, VSDOCUMENTPRIORITY[] pdwPriority, out uint pitemid)
		{
			return ((IVsProject3)this).IsDocumentInProject(pszMkDocument, out pfFound, pdwPriority, out pitemid);
		}

		int IVsProject2.GetMkDocument(uint itemid, out string pbstrMkDocument)
		{
			return ((IVsProject3)this).GetMkDocument(itemid, out pbstrMkDocument);
		}

		int IVsProject2.OpenItem(uint itemid, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
		{
			return ((IVsProject3)this).OpenItem(itemid, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame);
		}

		int IVsProject2.GetItemContext(uint itemid, out IOleServiceProvider ppSP)
		{
			return ((IVsProject3)this).GetItemContext(itemid, out ppSP);
		}

		int IVsProject2.GenerateUniqueItemName(uint itemidLoc, string pszExt, string pszSuggestedRoot, out string pbstrItemName)
		{
			return ((IVsProject3)this).GenerateUniqueItemName(itemidLoc, pszExt, pszSuggestedRoot, out pbstrItemName);
		}

		int IVsProject2.AddItem(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, VSADDRESULT[] pResult)
		{
			return ((IVsProject3)this).AddItem(itemidLoc, dwAddItemOperation, pszItemName, cFilesToOpen, rgpszFilesToOpen, hwndDlgOwner, pResult);
		}

		int IVsProject2.RemoveItem(uint dwReserved, uint itemid, out int pfResult)
		{
			return ((IVsProject3)this).RemoveItem(dwReserved, itemid, out pfResult);
		}

		int IVsProject2.ReopenItem(uint itemid, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, System.IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
		{
			return ((IVsProject3)this).ReopenItem(itemid, ref rguidEditorType, pszPhysicalView, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame);
		}
		#endregion

		#region IVsProject3 Members
		int IVsProject3.AddItem(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, VSADDRESULT[] pResult)
		{
			bool canceled = false;
			bool wereErrors = false;
			pResult[0] = VSADDRESULT.ADDRESULT_Failure;

			// Get the parent node to which it should be added.
			FolderNode parentNode = this.GetNode(itemidLoc, true) as FolderNode;
			if (parentNode == null)
			{
				string message = this.NativeResources.GetString(ResId.IDS_E_ADDITEMTOPROJECT, Path.GetFileName(this.FilePath));
				Context.ShowErrorMessageBox(message);
				Tracer.Fail("The specified parent {0} is not a FolderNode so we can't add an item to it.", itemidLoc);
				return NativeMethods.E_UNEXPECTED;
			}

			// Loop through the files that are to be added and add them, one by one.
			foreach (string sourcePath in rgpszFilesToOpen)
			{
				string destPath = null;
				Node addedNode = null;

				switch (dwAddItemOperation)
				{
					case VSADDITEMOPERATION.VSADDITEMOP_CLONEFILE:
						destPath = Path.Combine(parentNode.AbsoluteDirectory, pszItemName);
						addedNode = this.AddCopyOfFile(sourcePath, destPath, out canceled);
						break;

					case VSADDITEMOPERATION.VSADDITEMOP_OPENFILE:
						if (PackageUtility.IsRelative(this.RootDirectory, sourcePath))
						{
							destPath = PackageUtility.MakeRelative(this.RootDirectory, sourcePath);
							addedNode = this.AddExistingFile(destPath, true);
						}
						else
						{
							destPath = Path.Combine(parentNode.AbsoluteDirectory, Path.GetFileName(sourcePath));
							addedNode = this.AddCopyOfFile(sourcePath, destPath, out canceled);
						}
						break;

					case VSADDITEMOPERATION.VSADDITEMOP_LINKTOFILE:
						Tracer.Fail("NYI: VSADDITEMOPERATION.VSADDITEMOP_LINKTOFILE.");
						throw new NotImplementedException("Linking to files is not supported yet.");

					case VSADDITEMOPERATION.VSADDITEMOP_RUNWIZARD:
						Tracer.Fail("NYI: VSADDITEMOPERATION.VSADDITEMOP_RUNWIZARD.");
						throw new NotImplementedException("Running a wizard is not supported yet.");

					default:
						Tracer.Fail("Unknown VSADDDITEMOPERATION '{0}'", dwAddItemOperation);
						throw new ArgumentException(PackageUtility.SafeStringFormatInvariant("The dwAddItemOperation contains an unknown and unsupported value '{0}'.", dwAddItemOperation), "dwAddItemOperation");
				}

				// There were errors if the node is still null at this point.
				wereErrors = (addedNode == null);
			}

			pResult[0] = (canceled ? VSADDRESULT.ADDRESULT_Cancel : (wereErrors ? VSADDRESULT.ADDRESULT_Failure : VSADDRESULT.ADDRESULT_Success));
			return NativeMethods.S_OK;
		}

		int IVsProject3.AddItemWithSpecific(uint itemidLoc, VSADDITEMOPERATION dwAddItemOperation, string pszItemName, uint cFilesToOpen, string[] rgpszFilesToOpen, IntPtr hwndDlgOwner, uint grfEditorFlags, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, VSADDRESULT[] pResult)
		{
			int hr = ((IVsProject3)this).AddItem(itemidLoc, dwAddItemOperation, pszItemName, cFilesToOpen, rgpszFilesToOpen, hwndDlgOwner, pResult);
			// TODO: Open the items once they've been added.
			return hr;
		}

		int IVsProject3.GenerateUniqueItemName(uint parentHierarchyId, string extension, string suggestedRoot, out string itemName)
		{
			FolderNode parentNode = this.GetNode(parentHierarchyId, true) as FolderNode;
			Tracer.Assert(parentNode != null, "The parent of the unique name better be a folder.");
			if (parentNode != null)
			{
				bool isFolder = (extension == null || extension.Length == 0);
				itemName = parentNode.GenerateUniqueName(suggestedRoot, extension, isFolder);
			}
			else
			{
				itemName = String.Empty;
			}
			return NativeMethods.S_OK;
		}

		int IVsProject3.GetItemContext(uint itemid, out Microsoft.VisualStudio.OLE.Interop.IServiceProvider ppSP)
		{
			// From the documentation, we should just pass back null:
			//   This method allows a project to provide project context services to a document editor.
			//   If the project does not need to provide special services to its items, then it should
			//   return NULL. Under no circumstances should you return the IServiceProvider pointer
			//   that was passed to the package from the environment through IVsPackage::SetSite. The
			//   global services will automatically be made available to editors.
			ppSP = null;
			return NativeMethods.S_OK;
		}

		int IVsProject3.GetMkDocument(uint itemid, out string pbstrMkDocument)
		{
			Node node = this.GetNode(itemid, false);
			pbstrMkDocument = (node != null ? node.CanonicalName : null);
			return NativeMethods.S_OK;
		}

		int IVsProject3.IsDocumentInProject(string pszMkDocument, out int pfFound, VSDOCUMENTPRIORITY[] pdwPriority, out uint pitemid)
		{
			pfFound = 0;
			pitemid = 0;
			if (pdwPriority != null)
			{
				pdwPriority[0] = VSDOCUMENTPRIORITY.DP_Unsupported;
			}

			// Try to find the item in our hierarchy.
			Node node = this.GetNodeFromName(pszMkDocument);
			if (node != null)
			{
				pfFound = 1;
				pitemid = node.HierarchyId;
				if (pdwPriority != null)
				{
					pdwPriority[0] = VSDOCUMENTPRIORITY.DP_Standard;
				}
			}

			return NativeMethods.S_OK;
		}

		int IVsProject3.OpenItem(uint itemid, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
		{
			Guid emptyGuid = Guid.Empty;
			// If we call our own implementation of OpenItemWithSpecific and pass in to "use view" it will just
			// open the item with the standard editor, which is what we want for this method.
			int hr = ((IVsProject3)this).OpenItemWithSpecific(itemid, unchecked((uint)__VSSPECIFICEDITORFLAGS.VSSPECIFICEDITOR_UseView), ref emptyGuid, null, ref rguidLogicalView, punkDocDataExisting, out ppWindowFrame);
			return hr;
		}

		int IVsProject3.OpenItemWithSpecific(uint itemid, uint grfEditorFlags, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
		{
			ppWindowFrame = null;

			FileNode node = this.GetNode(itemid, false) as FileNode;
			if (node == null)
			{
				Tracer.Fail("The framework is calling us in an unexpected way because we report that we don't own the item '{0}' but Visual Studio thinks we do.", itemid);
				return NativeMethods.E_UNEXPECTED;
			}

			// Map the raw logical view guid to one of our predetermined ones.
			VsLogicalView view;
			if (!VsLogicalView.TryFromGuid(rguidLogicalView, out view))
			{
				Tracer.WriteLine(classType, "IVsProject3.OpenItemWithSpecific", Tracer.Level.Warning, "We're getting a logical view that we don't understand: '{0}'. Using the primary view instead.", rguidLogicalView.ToString("B"));
				view = VsLogicalView.Primary;
			}

			// Do we open with the standard or a specific editor?
			bool openWithSpecificEditor = ((((__VSSPECIFICEDITORFLAGS)grfEditorFlags) & __VSSPECIFICEDITORFLAGS.VSSPECIFICEDITOR_UseEditor) == __VSSPECIFICEDITORFLAGS.VSSPECIFICEDITOR_UseEditor);

			// Tell the node to open itself.
			if (openWithSpecificEditor)
			{
				ppWindowFrame = node.OpenWithSpecificEditor(view, punkDocDataExisting, pszPhysicalView, rguidEditorType);
			}
			else
			{
				ppWindowFrame = node.OpenWithStandardEditor(view, punkDocDataExisting);
			}

			return NativeMethods.S_OK;
		}

		int IVsProject3.RemoveItem(uint dwReserved, uint itemid, out int pfResult)
		{
			// TODO:  Add Project.RemoveItem implementation
			pfResult = 0;
			return 0;
		}

		int IVsProject3.ReopenItem(uint itemid, ref Guid rguidEditorType, string pszPhysicalView, ref Guid rguidLogicalView, System.IntPtr punkDocDataExisting, out IVsWindowFrame ppWindowFrame)
		{
			// TODO:  Add Project.ReopenItem implementation
			ppWindowFrame = null;
			return 0;
		}

		int IVsProject3.TransferItem(string pszMkDocumentOld, string pszMkDocumentNew, IVsWindowFrame punkWindowFrame)
		{
			// TODO:  Add Project.TransferItem implementation
			return 0;
		}
		#endregion

		#region IPersist Members
		int IPersist.GetClassID(out Guid pClassID)
		{
			return ((IPersistFileFormat)this).GetClassID(out pClassID);
		}
		#endregion

		#region IPersistFileFormat Members
		int IPersistFileFormat.InitNew(uint nFormatIndex)
		{
			// We don't have to do anything in this method because we'll never be called. A new
			// project is created in a different way than through this interface.
			Tracer.Fail("IPersistFileFormat.InitNew should never get called.");
			return NativeMethods.S_OK;
		}

		int IPersistFileFormat.IsDirty(out int pfIsDirty)
		{
			pfIsDirty = Convert.ToInt32(this.IsDirty);
			return NativeMethods.S_OK;
		}

		int IPersistFileFormat.GetClassID(out Guid pClassID)
		{
			// Make sure this is the ProjectTypeGuid, not the ProjectGuid. This fixes SourceForge Bug #1122482,
			// which is that when adding a Wix project to an existing solution the solution will fail to load
			// the project when opening the solution the next time. The reason is that the solution file was
			// using the project GUID instead of the project type GUID (Wix Project) that is registered with
			// Visual Studio.
			pClassID = this.ProjectTypeGuid;
			return NativeMethods.S_OK;
		}

		int IPersistFileFormat.GetCurFile(out string ppszFilename, out uint pnFormatIndex)
		{
			ppszFilename = this.FilePath;
			pnFormatIndex = 0;
			return NativeMethods.S_OK;
		}

		int IPersistFileFormat.GetFormatList(out string ppszFormatList)
		{
			ppszFormatList = this.NativeResources.GetString(ResId.IDS_PROJECT_SAVEAS_FILTER);
			return NativeMethods.S_OK;
		}

		int IPersistFileFormat.Load(string pszFilename, uint grfMode, int fReadOnly)
		{
			// TODO: Add IPersistFileFormat.Load implementation.
			return NativeMethods.S_OK;
		}

		int IPersistFileFormat.Save(string pszFilename, int fRemember, uint nFormatIndex)
		{
			// Check the file name.
			if (!PackageUtility.FileStringEquals(pszFilename, this.FilePath))
			{
				// TODO: Show the following error message to the user: "The project file can only be saved into the project location '{0}'." where 0=the AbsoluteDirectory.
				string message = PackageUtility.SafeStringFormatInvariant("Cannot perform a Save As operation on the project file to another location. Filename={0}", pszFilename);
				Tracer.Fail(message);
				throw new ArgumentException(message, "pszFilename");
			}

			Encoding encoding;
			switch (nFormatIndex)
			{
				case 0:
					encoding = Encoding.UTF8;
					break;

				case 1:
					encoding = Encoding.Default;
					break;

				case 2:
					encoding = Encoding.Unicode;
					break;

				default:
					Tracer.Fail("Unknown format index {0}", nFormatIndex);
					encoding = Encoding.Default;
					break;
			}

			// Save the project if it's dirty.
			this.Serializer.Save(encoding, false);

			return NativeMethods.S_OK;
		}

		int IPersistFileFormat.SaveCompleted(string pszFilename)
		{
			return NativeMethods.S_OK;
		}
		#endregion

		#region IVsGetCfgProvider Members
		int IVsGetCfgProvider.GetCfgProvider(out IVsCfgProvider ppCfgProvider)
		{
			ppCfgProvider = this.ConfigurationProvider;
			return NativeMethods.S_OK;
		}
		#endregion

		/// <summary>
		/// Gets the value of the specified property.
		/// </summary>
		/// <param name="hierarchyId">The Id of the hierarchy node from which to retrieve the property.</param>
		/// <param name="propertyId">The Id of the property to retrieve.</param>
		/// <param name="propertyValue">The value of the specified property, or null if the property is not supported.</param>
		/// <returns>true if the property is supported; otherwise false.</returns>
		public override bool GetProperty(uint hierarchyId, __VSHPROPID propertyId, out object propertyValue)
		{
			bool supported = true;
			propertyValue = null;

			switch (propertyId)
			{
				case __VSHPROPID.VSHPROPID_ConfigurationProvider:
					propertyValue = this.ConfigurationProvider;
					break;

				case __VSHPROPID.VSHPROPID_DefaultEnableBuildProjectCfg:
					// Specifies whether "Build" should be initially checked by default in the solution cfg.
					// Normally "Build" is checked by default if the project supports IVsBuildableProjectCfg.
					propertyValue = true;
					break;

				case __VSHPROPID.VSHPROPID_ProjectDir:
					propertyValue = this.RootDirectory;
					break;

				case __VSHPROPID.VSHPROPID_ProjectIDGuid:
					propertyValue = this.ProjectGuid;
					break;

				case __VSHPROPID.VSHPROPID_TypeName:
					propertyValue = this.NativeResources.GetString(ResId.IDS_OFFICIALNAME);
					break;

				default:
					supported = false;
					break;
			}

			if (supported)
			{
				this.IncrementSupportedGetProperty(propertyId);
			}
			else
			{
				// Let the base class have a chance to get the property.
				supported = base.GetProperty(hierarchyId, propertyId, out propertyValue);
			}

			return supported;
		}

		/// <summary>
		/// Sets the value of the specified property on this node.
		/// </summary>
		/// <param name="hierarchyId">The unique identifier of the node.</param>
		/// <param name="propertyId">The Id of the property to set.</param>
		/// <param name="value">The value to set.</param>
		/// <returns>
		/// An HRESULT that is the result of the operation. Usually S_OK indicates that the property was set.
		/// If the property is not supported, returns DISP_E_MEMBERNOTFOUND.
		/// </returns>
		public override int SetProperty(uint hierarchyId, __VSHPROPID propertyId, object value)
		{
			int hr = NativeMethods.S_OK;

			switch (propertyId)
			{
				case __VSHPROPID.VSHPROPID_ProjectIDGuid:
					this.ProjectGuid = (Guid)value;
					break;

				default:
					hr = NativeMethods.DISP_E_MEMBERNOTFOUND;
					break;
			}

			if (NativeMethods.Succeeded(hr))
			{
				this.IncrementSupportedSetProperty(propertyId);
			}
			else
			{
				// Let the base class do its stuff.
				hr = base.SetProperty(hierarchyId, propertyId, value);
			}

			return hr;
		}

		/// <summary>
		/// Starts the specified build operation on the project for the currently selected project configuration.
		/// </summary>
		/// <param name="operation">The operation to perform.</param>
		public void StartBuild(BuildOperation operation)
		{
			Tracer.VerifyEnumArgument((int)operation, "operation", typeof(BuildOperation));

			// We have to verify that the environment is not busy right now
			if (Package.Instance.Context.IsSolutionBuilding)
			{
				Tracer.WriteLineVerbose(classType, "StartBuild", "The build manager is busy right now.");
				return;
			}

			// Get the build manager from VS
			IVsSolutionBuildManager solutionBuildMgr = this.ServiceProvider.GetServiceOrThrow(typeof(SVsSolutionBuildManager), typeof(IVsSolutionBuildManager), classType, "StartBuild") as IVsSolutionBuildManager;

			// Convert the enum to one of the VS flags
			VSSOLNBUILDUPDATEFLAGS flags = VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_NONE;

			switch (operation)
			{
				case BuildOperation.Clean:
					flags |= VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_CLEAN;
					break;

				case BuildOperation.Build:
					flags |= VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD;
					break;

				case BuildOperation.Rebuild:
					flags |= VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_CLEAN | VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD;
					break;

				default:
					Tracer.Fail("Unknown BuildOperation '{0}'", operation.ToString());
					return;
			}


			NativeMethods.ThrowOnFailure(solutionBuildMgr.StartSimpleUpdateProjectConfiguration((IVsHierarchy)this, null, null, (uint)flags, 0, 0));
		}

		/// <summary>
		/// Clears the dirty flag for any contained <see cref="IDirtyable"/> objects.
		/// </summary>
		protected override void ClearDirtyOnContainedObjects()
		{
			base.ClearDirtyOnContainedObjects();
			this.BuildSettings.ClearDirty();
			this.ConfigurationProvider.ClearDirty();
		}
		#endregion
	}
}
