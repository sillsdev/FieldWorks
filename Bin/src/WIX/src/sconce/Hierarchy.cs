//-------------------------------------------------------------------------------------------------
// <copyright file="Hierarchy.cs" company="Microsoft">
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
// Visual Studio hierarchy support for the Solution Explorer.
// </summary>
//-------------------------------------------------------------------------------------------------

// TODO: Wrap all public entry points with a try/catch block and call Context.NotifyInternalError

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using System.Drawing;
	using System.IO;
	using System.Globalization;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.OLE.Interop;
	using Microsoft.VisualStudio.Shell.Interop;

	using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
	using ResId = ResourceId;

	public abstract class Hierarchy :
		DirtyableObject,
		IVsHierarchy,
		IVsUIHierarchy,
		IOleCommandTarget,
		IVsPersistHierarchyItem,
		IVsPersistHierarchyItem2,
		IVsHierarchyDeleteHandler,
		IVsComponentUser
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(Hierarchy);

		private VsHierarchyEventListenerCollection eventListeners = new VsHierarchyEventListenerCollection();
		private ProjectNode rootNode;
		private ServiceProvider serviceProvider;

		// These are used to keep track of our property gets/sets so that we can write a summary
		// to the trace log and not log every single one. We get over 10,000 trace log lines when
		// simply opening a new project when we're logging every get/set. These hashtables are
		// keyed by Guid/Id and contain a count of how many times the property was requested.
		private Hashtable supportedGets = new Hashtable();
		private Hashtable supportedSets = new Hashtable();
		private Hashtable unsupportedGets = new Hashtable();
		private Hashtable unsupportedSets = new Hashtable();
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="Hierarchy"/> class.
		/// </summary>
		public Hierarchy()
		{
			// Listen to the Tracer's WritingSummary event so that we can write our summary information.
			Tracer.WritingSummarySection += new EventHandler(WriteSummary);

			this.serviceProvider = Package.Instance.Context.ServiceProvider;
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
		/// Gets the <see cref="Project"/> that is using this hierarchy.
		/// </summary>
		public abstract Project AttachedProject { get; }

		/// <summary>
		/// Gets a value indicating whether this hierarchy is currently selected.
		/// </summary>
		/// <value>true if this hierarchy is currently selected; otherwise, false.</value>
		public bool IsSelected
		{
			get
			{
				IntPtr pHierarchy;
				uint itemId;
				IVsMultiItemSelect mis;
				IntPtr pSC;
				IVsMonitorSelection monitorSelection = (IVsMonitorSelection)this.ServiceProvider.GetServiceOrThrow(typeof(IVsMonitorSelection), typeof(IVsMonitorSelection), classType, "IsSelected");
				int hr = monitorSelection.GetCurrentSelection(out pHierarchy, out itemId, out mis, out pSC);
				if (NativeMethods.Failed(hr))
				{
					Tracer.Fail("Cannot get the current selection: 0x{0:x}", hr);
					return false;
				}
				IVsUIHierarchy vsUIHierarchy = (IVsUIHierarchy)Marshal.GetObjectForIUnknown(pHierarchy);
				bool isHierarchySelected = Object.ReferenceEquals(vsUIHierarchy, (IVsUIHierarchy)this);

				return isHierarchySelected;
			}
		}

		/// <summary>
		/// Gets the one and only library folder node.
		/// </summary>
		public ReferenceFolderNode ReferencesNode
		{
			get { return this.RootNode.ReferencesNode; }
		}

		/// <summary>
		/// Gets the root node of the hierarchy.
		/// </summary>
		public ProjectNode RootNode
		{
			get
			{
				if (this.rootNode == null)
				{
					// Create the root node. The absolute path will be set when the project is loaded.
					this.rootNode = this.CreateProjectNode();
				}
				return this.rootNode;
			}
		}

		public Node SelectedNode
		{
			get
			{
				Node node = null;

				// Ask Visual Studio for the currently selected hierarchy and item with the hierarchy.
				IntPtr pHierarchy = IntPtr.Zero;
				uint itemId;
				IVsMultiItemSelect mis;
				IntPtr pSelectionContainer = IntPtr.Zero;
				IVsMonitorSelection monitorSelection = (IVsMonitorSelection)this.ServiceProvider.GetServiceOrThrow(typeof(IVsMonitorSelection), typeof(IVsMonitorSelection), classType, "SelectedNode");

				try
				{
					int hr = monitorSelection.GetCurrentSelection(out pHierarchy, out itemId, out mis, out pSelectionContainer);
					if (NativeMethods.Failed(hr))
					{
						Tracer.Fail("Cannot get the current selection: 0x{0:x}", hr);
						return null;
					}

					IVsUIHierarchy vsUIHierarchy = (IVsUIHierarchy)Marshal.GetObjectForIUnknown(pHierarchy);
					bool isHierarchySelected = PackageUtility.IsSameComObject(this, vsUIHierarchy);
					Tracer.Assert(itemId != NativeMethods.VSITEMID_SELECTION, "The currently selected hierarchy item should not be VSITEMID_SELECTION.");
					if (isHierarchySelected && itemId != NativeMethods.VSITEMID_SELECTION)
					{
						node = this.GetNode(itemId, true);
					}
				}
				finally
				{
					if (pHierarchy != IntPtr.Zero)
					{
						Marshal.Release(pHierarchy);
					}

					if (pSelectionContainer != IntPtr.Zero)
					{
						Marshal.Release(pSelectionContainer);
					}
				}

				return node;
			}
		}

		/// <summary>
		/// Gets the service provider for this hierarchy.
		/// </summary>
		public ServiceProvider ServiceProvider
		{
			get { return this.serviceProvider; }
		}

		/// <summary>
		/// Gets the filter used in the Add Reference dialog box.
		/// </summary>
		protected virtual string AddReferenceDialogFilter
		{
			get { return SconceStrings.AddReferenceDialogFilter; }
		}

		/// <summary>
		/// Gets the initial directory for the Add Reference dialog box.
		/// </summary>
		protected virtual string AddReferenceDialogInitialDirectory
		{
			get { return Directory.GetCurrentDirectory(); }
		}

		/// <summary>
		/// Gets the title used in the Add Reference dialog box.
		/// </summary>
		protected virtual string AddReferenceDialogTitle
		{
			get { return SconceStrings.AddReferenceDialogTitle; }
		}

		/// <summary>
		/// Returns a value indicating whether one or more contained <see cref="IDirtyable"/> objects
		/// are dirty.
		/// </summary>
		protected override bool AreContainedObjectsDirty
		{
			get { return (base.AreContainedObjectsDirty || this.RootNode.IsDirty); }
		}

		/// <summary>
		/// Gets the native resource manager for the package.
		/// </summary>
		protected NativeResourceManager NativeResources
		{
			get { return Package.Instance.Context.NativeResources; }
		}
		#endregion

		#region IVsHierarchy Members
		//==========================================================================================
		// IVsHierarchy Members
		//==========================================================================================

		int IVsHierarchy.AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out uint pdwCookie)
		{
			return ((IVsUIHierarchy)this).AdviseHierarchyEvents(pEventSink, out pdwCookie);
		}

		int IVsHierarchy.Close()
		{
			return ((IVsUIHierarchy)this).Close();
		}

		int IVsHierarchy.GetCanonicalName(uint itemid, out string pbstrName)
		{
			return ((IVsUIHierarchy)this).GetCanonicalName(itemid, out pbstrName);
		}

		int IVsHierarchy.GetGuidProperty(uint itemid, int propid, out Guid pguid)
		{
			return ((IVsUIHierarchy)this).GetGuidProperty(itemid, propid, out pguid);
		}

		int IVsHierarchy.GetNestedHierarchy(uint itemid, ref Guid iidHierarchyNested, out IntPtr ppHierarchyNested, out uint pitemidNested)
		{
			return ((IVsUIHierarchy)this).GetNestedHierarchy(itemid, ref iidHierarchyNested, out ppHierarchyNested, out pitemidNested);
		}

		int IVsHierarchy.GetProperty(uint itemid, int propid, out object pvar)
		{
			return ((IVsUIHierarchy)this).GetProperty(itemid, propid, out pvar);
		}

		int IVsHierarchy.GetSite(out IOleServiceProvider ppSP)
		{
			return ((IVsUIHierarchy)this).GetSite(out ppSP);
		}

		int IVsHierarchy.ParseCanonicalName(string pszName, out uint pitemid)
		{
			return ((IVsUIHierarchy)this).ParseCanonicalName(pszName, out pitemid);
		}

		int IVsHierarchy.QueryClose(out int pfCanClose)
		{
			return ((IVsUIHierarchy)this).QueryClose(out pfCanClose);
		}

		int IVsHierarchy.SetGuidProperty(uint itemid, int propid, ref Guid rguid)
		{
			return ((IVsUIHierarchy)this).SetGuidProperty(itemid, propid, ref rguid);
		}

		int IVsHierarchy.SetProperty(uint itemid, int propid, object var)
		{
			return ((IVsUIHierarchy)this).SetProperty(itemid, propid, var);
		}

		int IVsHierarchy.SetSite(IOleServiceProvider psp)
		{
			return ((IVsUIHierarchy)this).SetSite(psp);
		}

		int IVsHierarchy.UnadviseHierarchyEvents(uint dwCookie)
		{
			return ((IVsUIHierarchy)this).UnadviseHierarchyEvents(dwCookie);
		}

		int IVsHierarchy.Unused0()
		{
			return ((IVsUIHierarchy)this).Unused0();
		}

		int IVsHierarchy.Unused1()
		{
			return ((IVsUIHierarchy)this).Unused1();
		}

		int IVsHierarchy.Unused2()
		{
			return ((IVsUIHierarchy)this).Unused2();
		}

		int IVsHierarchy.Unused3()
		{
			return ((IVsUIHierarchy)this).Unused3();
		}

		int IVsHierarchy.Unused4()
		{
			return ((IVsUIHierarchy)this).Unused4();
		}
		#endregion

		#region IVsUIHierarchy Members
		//==========================================================================================
		// IVsUIHierarchy Members
		//==========================================================================================

		int IVsUIHierarchy.AdviseHierarchyEvents(IVsHierarchyEvents pEventSink, out uint pdwCookie)
		{
			pdwCookie = this.eventListeners.Add(pEventSink);
			return NativeMethods.S_OK;
		}

		int IVsUIHierarchy.Close()
		{
			// Save the root node, which will save its children recursively. A save is only done if
			// the document associated with the node is dirty.
			this.RootNode.Save();

			// Now close the root, which will close it's children recursively.
			this.RootNode.Close();

			return NativeMethods.S_OK;
		}

		int IVsUIHierarchy.ExecCommand(uint itemid, ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			bool supported = true;

			// Attempt to find the node in our hierarchy (throw if we can't find it).
			Node node = this.GetNode(itemid, true);

			if (pguidCmdGroup == VsMenus.VsUIHierarchyWindowCmds)
			{
				Tracer.Assert(node != null, "Huh? We should have thrown on a null value.");
				VsMenus.VsUIHierarchyWindowCmdId uiHierarchyCmdId = (VsMenus.VsUIHierarchyWindowCmdId)nCmdID;
				switch (uiHierarchyCmdId)
				{
					case VsMenus.VsUIHierarchyWindowCmdId.DoubleClick:
					case VsMenus.VsUIHierarchyWindowCmdId.EnterKey:
						node.DoDefaultAction();
						break;

					case VsMenus.VsUIHierarchyWindowCmdId.RightClick:
						node.ShowContextMenu();
						break;

					default:
						supported = false;
						break;
				}
			}
			else if (pguidCmdGroup == VsMenus.StandardCommandSet97)
			{
				supported = this.ExecuteStandard97Command(node, (VsCommand)nCmdID);
			}
			else if (pguidCmdGroup == VsMenus.StandardCommandSet2K)
			{
				supported = this.ExecuteStandard2KCommand(node, (VsCommand2K)nCmdID);
			}
			else
			{
				supported = false;
			}

			return (supported ? NativeMethods.S_OK : NativeMethods.OLECMDERR_E_NOTSUPPORTED);
		}

		int IVsUIHierarchy.GetCanonicalName(uint itemid, out string pbstrName)
		{
			Node node = this.GetNode(itemid, false);
			pbstrName = (node == null ? null : node.CanonicalName);
			return NativeMethods.S_OK;
		}

		int IVsUIHierarchy.GetGuidProperty(uint itemid, int propid, out Guid pguid)
		{
			int hr = NativeMethods.E_NOTIMPL;
			pguid = Guid.Empty;
			object propertyValue;
			__VSHPROPID vsPropId = (__VSHPROPID)propid; // This is explicitly declared to see the cast value in debugging.
			bool supported = this.GetProperty(itemid, vsPropId, out propertyValue);

			if (supported)
			{
				hr = NativeMethods.S_OK;
				pguid = (Guid)propertyValue;
			}
			return hr;
		}

		int IVsUIHierarchy.GetNestedHierarchy(uint itemid, ref Guid iidHierarchyNested, out IntPtr ppHierarchyNested, out uint pitemidNested)
		{
			ppHierarchyNested = IntPtr.Zero;
			pitemidNested = 0;
			return NativeMethods.E_NOTIMPL;
		}

		int IVsUIHierarchy.GetProperty(uint itemid, int propid, out object pvar)
		{
			int hr = NativeMethods.DISP_E_MEMBERNOTFOUND;
			__VSHPROPID vsPropId = (__VSHPROPID)propid; // This is explicitly declared to see the cast value in debugging.
			bool supported = this.GetProperty(itemid, vsPropId, out pvar);

			if (supported)
			{
				hr = NativeMethods.S_OK;
			}
			return hr;
		}

		int IVsUIHierarchy.GetSite(out IOleServiceProvider ppSP)
		{
			ppSP = this.serviceProvider.OleServiceProvider;
			return NativeMethods.S_OK;
		}

		int IVsUIHierarchy.ParseCanonicalName(string pszName, out uint pitemid)
		{
			Node node = this.GetNodeFromName(pszName);
			pitemid = (node == null ? NativeMethods.VSITEMID_NIL : node.HierarchyId);
			return NativeMethods.S_OK;
		}

		int IVsUIHierarchy.QueryClose(out int pfCanClose)
		{
			pfCanClose = 1;
			return NativeMethods.S_OK;
		}

		int IVsUIHierarchy.QueryStatusCommand(uint itemid, ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			int hr = NativeMethods.S_OK;

			if (pguidCmdGroup == Guid.Empty)
			{
				Tracer.WriteLineVerbose(classType, "IVsUIHierarchy.QueryStatusCommand", "Command group is an empty GUID.");
				hr = NativeMethods.OLECMDERR_E_NOTSUPPORTED;
			}
			else
			{
				// Attempt to find the node in our hierarchy. We call this from IOleCommandTarget.QueryStatusCommand
				// which does not have an itemId, so we don't want to throw an exception.
				Node node = this.GetNode(itemid, false);
				if (node == null)
				{
					node = this.SelectedNode;
				}

				Tracer.Assert((cCmds > 0 && prgCmds != null && prgCmds.Length == cCmds) || cCmds == 0, "The cCmds argument does not match the actual length of the array.");
				for (int i = 0; i < cCmds; i++)
				{
					if (prgCmds == null || i >= prgCmds.Length)
					{
						break;
					}

					hr = this.QuerySingleStatusCommand(node, pguidCmdGroup, prgCmds[i].cmdID, out prgCmds[i].cmdf);
					if (NativeMethods.Failed(hr))
					{
						break;
					}
				}
			}

			return hr;
		}

		int IVsUIHierarchy.SetGuidProperty(uint itemid, int propid, ref Guid rguid)
		{
			__VSHPROPID vsPropId = (__VSHPROPID)propid; // This is explicitly declared to see the cast value in debugging.
			int hr = this.SetProperty(itemid, vsPropId, rguid);
			return hr;
		}

		int IVsUIHierarchy.SetProperty(uint itemid, int propid, object var)
		{
			__VSHPROPID vsPropId = (__VSHPROPID)propid; // This is explicitly declared to see the cast value in debugging.
			int hr = this.SetProperty(itemid, vsPropId, var);
			return hr;
		}

		int IVsUIHierarchy.SetSite(IOleServiceProvider psp)
		{
			try
			{
				Tracer.VerifyNonNullArgument(psp, "psp");

				// We don't want to create another service provider if the argument is a pointer
				// to the one currently stored in the PackageContext (we already initialized our
				// pointer to the PackageContext's service provider in the constructor).
				if (!Object.ReferenceEquals(psp, this.serviceProvider))
				{
					this.serviceProvider = new ServiceProvider(psp);
				}
			}
			catch (Exception e)
			{
				Tracer.Fail("Unexpected exception: {0}\n{1}", e.Message, e);
				throw;
			}

			return NativeMethods.S_OK;
		}

		int IVsUIHierarchy.UnadviseHierarchyEvents(uint dwCookie)
		{
			this.eventListeners.Remove(dwCookie);
			return NativeMethods.S_OK;
		}

		int IVsUIHierarchy.Unused0()
		{
			return NativeMethods.S_OK;
		}

		int IVsUIHierarchy.Unused1()
		{
			return NativeMethods.S_OK;
		}

		int IVsUIHierarchy.Unused2()
		{
			return NativeMethods.S_OK;
		}

		int IVsUIHierarchy.Unused3()
		{
			return NativeMethods.S_OK;
		}

		int IVsUIHierarchy.Unused4()
		{
			return NativeMethods.S_OK;
		}
		#endregion

		#region IOleCommandTarget Members
		//==========================================================================================
		// IOleCommandTarget Members
		//==========================================================================================

		int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			bool supported = false;

			if (pguidCmdGroup == VsMenus.StandardCommandSet97)
			{
				VsCommand command = (VsCommand)nCmdID;
				supported = this.ExecuteStandard97Command(this.SelectedNode, command);
			}
			else if (pguidCmdGroup == VsMenus.StandardCommandSet2K)
			{
				VsCommand2K command = (VsCommand2K)nCmdID;
				supported = this.ExecuteStandard2KCommand(this.SelectedNode, command);
			}

			return (supported ? NativeMethods.S_OK : NativeMethods.OLECMDERR_E_NOTSUPPORTED);
		}

		int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			return ((IVsUIHierarchy)this).QueryStatusCommand(NativeMethods.VSITEMID_NIL, ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
		}
		#endregion

		#region IVsPersistHierarchyItem Members
		//==========================================================================================
		// IVsPersistHierarchyItem Members
		//==========================================================================================

		int IVsPersistHierarchyItem.IsItemDirty(uint itemid, System.IntPtr punkDocData, out int pfDirty)
		{
			return ((IVsPersistHierarchyItem2)this).IsItemDirty(itemid, punkDocData, out pfDirty);
		}

		int IVsPersistHierarchyItem.SaveItem(Microsoft.VisualStudio.Shell.Interop.VSSAVEFLAGS dwSave, string pszSilentSaveAsName, uint itemid, System.IntPtr punkDocData, out int pfCanceled)
		{
			return ((IVsPersistHierarchyItem2)this).SaveItem(dwSave, pszSilentSaveAsName, itemid, punkDocData, out pfCanceled);
		}
		#endregion

		#region IVsPersistHierarchyItem2 Members
		//==========================================================================================
		// IVsPersistHierarchyItem2 Members
		//==========================================================================================

		int IVsPersistHierarchyItem2.IgnoreItemFileChanges(uint itemid, int fIgnore)
		{
			// TODO:  Add IVsPersistHierarchyItem2.IgnoreItemFileChanges implementation
			return NativeMethods.E_NOTIMPL;
		}

		int IVsPersistHierarchyItem2.IsItemDirty(uint itemid, IntPtr punkDocData, out int pfDirty)
		{
			pfDirty = 0;

			// The punkDocData is really a IVsPersistDocData object, or at least it should be.
			IVsPersistDocData docData = Marshal.GetObjectForIUnknown(punkDocData) as IVsPersistDocData;
			if (docData == null)
			{
				Tracer.Fail("The environment should be passing us an IVsPersistDocData object.");
				throw new ArgumentException("Expected IVsPersistDocData object.", "punkDocData");
			}

			// Call into the IVsPersistDocData object to see if the item is dirty.
			int hr = docData.IsDocDataDirty(out pfDirty);

			return hr;
		}

		int IVsPersistHierarchyItem2.IsItemReloadable(uint itemid, out int pfReloadable)
		{
			pfReloadable = 0;
			// TODO:  Add IVsPersistHierarchyItem2.IsItemReloadable implementation
			return NativeMethods.S_OK;
		}

		int IVsPersistHierarchyItem2.ReloadItem(uint itemid, uint dwReserved)
		{
			// TODO:  Add IVsPersistHierarchyItem2.ReloadItem implementation
			return NativeMethods.S_OK;
		}

		/// <summary>
		/// Saves the hierarchy item to disk.
		/// </summary>
		/// <param name="dwSave">Flags whose values are taken from the <see cref="VSSAVEFLAGS"/> enumeration.</param>
		/// <param name="pszSilentSaveAsName">File name to be applied when <paramref name="dwSave"/> is set to VSSAVE_SilentSave.</param>
		/// <param name="itemid">Item identifier of the hierarchy item saved from VSITEMID.</param>
		/// <param name="punkDocData">Pointer to the <b>IUnknown</b> interface of the hierarchy item saved.</param>
		/// <param name="pfCanceled">TRUE if the save action was canceled.</param>
		/// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
		/// <remarks>
		/// <para>The caller of this method is responsible for determining whether the document is
		/// in the Running Document Table and should pass in the correct punkDocData parameter. It
		/// is not necessary for the implementer of this method to call the IVsRunningDocumentTable::FindAndLockDocument
		/// method when punkDocData is NULL.</para>
		/// <para>When a document is saved, this method is called to enable the owning hierarchy
		/// to establish control. Then the hierarchy can use any private mechanism to persist the
		/// document. For hierarchies that use standard editors, the implementation of SaveItem
		/// method is to call the following:</para>
		/// <list type="bullet">
		/// <item>For VSSAVE_Save and VSSAVE_SaveAs, it will Query Interface (QI) for IVsPersistDocData
		/// on the DocData object and call IVsPersistDocData2::SaveDocData.</item>
		/// <item>For VSSAVE_SilentSave, it will QI for interface IPersistFileFormat on the DocData
		/// object and use this interface in a call to the method IVsUIShell::SaveDocDataToFile passing
		/// the parameters VSSAVE_SilentSave, pPersistFile, pszSilentSaveAsName lpstrUntitledPath,
		/// &amp;bstrDocumentNew, and &amp;fCanceled).</item>
		/// </list>
		/// </remarks>
		int IVsPersistHierarchyItem2.SaveItem(VSSAVEFLAGS dwSave, string pszSilentSaveAsName, uint itemid, IntPtr punkDocData, out int pfCanceled)
		{
			if (punkDocData == IntPtr.Zero)
			{
				Tracer.Fail("Invalid parameter 'punkDocData'.");
				throw new ArgumentException("The caller is responsible for determining whether the document is in the Running Document Table and should pass in the correct parameter.", "punkDocData");
			}

			pfCanceled = 0;
			string newFileName;
			bool silentSave = ((dwSave & VSSAVEFLAGS.VSSAVE_SilentSave) == VSSAVEFLAGS.VSSAVE_SilentSave);
			int hr;

			if (silentSave)
			{
				// For VSSAVE_SilentSave we should have an IPersistFileFormat object.
				IPersistFileFormat persistFileFormat = Marshal.GetObjectForIUnknown(punkDocData) as IPersistFileFormat;
				if (persistFileFormat == null)
				{
					Tracer.Fail("The environment should be passing us an IPersistFileFormat object.");
					throw new ArgumentException("Expected IPersistFileFormat object.", "punkDocData");
				}

				// Save the document.
				IVsUIShell vsUIShell = (IVsUIShell)this.ServiceProvider.GetServiceOrThrow(typeof(SVsUIShell), typeof(IVsUIShell), classType, "IVsPersistHierarchyItem2.SaveItem");
				hr = vsUIShell.SaveDocDataToFile(VSSAVEFLAGS.VSSAVE_SilentSave, persistFileFormat, pszSilentSaveAsName, out newFileName, out pfCanceled);
				NativeMethods.ThrowOnFailure(hr);
			}
			else
			{
				IVsPersistDocData persistDocData = Marshal.GetObjectForIUnknown(punkDocData) as IVsPersistDocData;
				if (persistDocData == null)
				{
					Tracer.Fail("The environment should be passing us an IVsPersistDocData object.");
					throw new ArgumentException("Expected IVsPersistDocData object.", "punkDocData");
				}

				// Save the document.
				hr = persistDocData.SaveDocData(dwSave, out newFileName, out pfCanceled);
			}

			return NativeMethods.S_OK;
		}
		#endregion

		#region IVsHierarchyDeleteHandler Members
		//==========================================================================================
		// IVsHierarchyDeleteHandler Members
		//==========================================================================================

		int IVsHierarchyDeleteHandler.DeleteItem(uint dwDelItemOp, uint itemid)
		{
			// We should find the node. Throw if we don't.
			Node node = this.GetNode(itemid, true);

			// The node can support removing itself from the project, deleting itself from disk, or both.
			__VSDELETEITEMOPERATION deleteOperation = (__VSDELETEITEMOPERATION)dwDelItemOp;
			if (deleteOperation == __VSDELETEITEMOPERATION.DELITEMOP_RemoveFromProject)
			{
				node.RemoveFromProject();
			}
			else if (deleteOperation == __VSDELETEITEMOPERATION.DELITEMOP_DeleteFromStorage)
			{
				node.Delete();
			}
			else
			{
				Tracer.Fail("The environment is passing us a __VSDELETEITEMOPERATION that we don't understand: {0}.", deleteOperation);
			}
			return NativeMethods.S_OK;
		}

		int IVsHierarchyDeleteHandler.QueryDeleteItem(uint dwDelItemOp, uint itemid, out int pfCanDelete)
		{
			// We should find the node. Throw if we don't.
			Node node = this.GetNode(itemid, true);

			// The node can support removing itself from the project, deleting itself from disk, or both.
			__VSDELETEITEMOPERATION deleteOperation = (__VSDELETEITEMOPERATION)dwDelItemOp;
			if (deleteOperation == __VSDELETEITEMOPERATION.DELITEMOP_RemoveFromProject)
			{
				pfCanDelete = Convert.ToInt32(node.CanRemoveFromProject);
			}
			else if (deleteOperation == __VSDELETEITEMOPERATION.DELITEMOP_DeleteFromStorage)
			{
				pfCanDelete = Convert.ToInt32(node.CanDelete);
			}
			else
			{
				Tracer.Fail("The environment is passing us a __VSDELETEITEMOPERATION that we don't understand: {0}.", deleteOperation);
				pfCanDelete = 0;
			}
			return NativeMethods.S_OK;
		}
		#endregion

		#region IVsComponentUser Members
		//==========================================================================================
		// IVsComponentUser Members
		//==========================================================================================

		/// <summary>
		/// This is called in response to the Visual Studio "Add Reference" dialog and where we add
		/// all of the references that the user specified.
		/// </summary>
		/// <param name="dwAddCompOperation">Specifies the type of add component operation.</param>
		/// <param name="cComponents">The count of the components to add.</param>
		/// <param name="rgpcsdComponents">An array of <see cref="VSCOMPONENTSELECTORDATA"/> objects to add.</param>
		/// <param name="hwndPickerDlg">The HWND of the "Add Reference" dialog.</param>
		/// <param name="pResult">Specifies the result of the operation.</param>
		/// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
		/// <remarks>
		/// The contents of <paramref name="pResult"/> determine whether the dialog closes. If the
		/// component add succeeds or is cancelled by the user, the dialog is closed. If the
		/// component add operation fails, the dialog remains open.
		/// </remarks>
		int IVsComponentUser.AddComponent(VSADDCOMPOPERATION dwAddCompOperation, uint cComponents, IntPtr[] rgpcsdComponents, IntPtr hwndPickerDlg, VSADDCOMPRESULT[] pResult)
		{
			StringCollection referenceFiles = new StringCollection();
			bool sawProjectTypes = false;

			// Instead of adding each reference in this loop, we'll just gather the files that we
			// should add and detect if we have any project references. We do this so we can show
			// the dialog saying the project references aren't supported yet before we actually do
			// the work of adding any references.
			for (int i = 0; i < cComponents; i++)
			{
				IntPtr pSelectorData = rgpcsdComponents[i];
				VSCOMPONENTSELECTORDATA selectorData = (VSCOMPONENTSELECTORDATA)Marshal.PtrToStructure(pSelectorData, typeof(VSCOMPONENTSELECTORDATA));

				switch (selectorData.type)
				{
					case VSCOMPONENTTYPE.VSCOMPONENTTYPE_File:
						referenceFiles.Add(selectorData.bstrFile);
						break;

					case VSCOMPONENTTYPE.VSCOMPONENTTYPE_Project:
						sawProjectTypes = true;
						break;

					default:
						Tracer.WriteLineWarning(classType, "IVsComponentUser.AddComponent", "We should not be getting {0} for the component type since we don't show those tabs in the Add Reference dialog.", selectorData.type);
						break;
				}
			}

			// If we saw some project types, then kindly tell the user that we don't support it yet.
			if (sawProjectTypes)
			{
				Context.ShowMessageBox("Sorry, project references are not yet supported in Votive. If you want to add the code, by all means! :)", OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_INFO);
				pResult[0] = VSADDCOMPRESULT.ADDCOMPRESULT_Failure;
			}
			else
			{
				try
				{
					// Loop through the reference files, adding each one.
					foreach (string referenceFile in referenceFiles)
					{
						this.AddReference(referenceFile, true);
					}
				}
				catch
				{
					// We catch here because we're calling a virtual function that could be outside
					// of our assembly (3rd party).
				}
				pResult[0] = VSADDCOMPRESULT.ADDCOMPRESULT_Success;
			}

			return NativeMethods.S_OK;
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Adds a file to the hierarchy by copying it from its source path to the destination path.
		/// If the source file happens to reside in the same directory, then it is just added to the hierarchy.
		/// </summary>
		/// <param name="sourcePath">The source path of the file to copy.</param>
		/// <param name="destinationPath">The destination path of the file to add.</param>
		/// <param name="canceled">Indicates whether the user canceled the operation if there were prompts.</param>
		/// <returns>The <see cref="Node"/> of the file was copied and added to the hierarchy successfully; otherwise, null.</returns>
		public Node AddCopyOfFile(string sourcePath, string destinationPath, out bool canceled)
		{
			Tracer.VerifyStringArgument(sourcePath, "sourcePath");
			Tracer.VerifyStringArgument(destinationPath, "destinationPath");

			// Set the out parameters.
			canceled = false;

			// Canonicalize the paths
			sourcePath = PackageUtility.CanonicalizeFilePath(sourcePath);
			destinationPath = PackageUtility.CanonicalizeFilePath(destinationPath);

			// Check to make sure the source file exists.
			if (!File.Exists(sourcePath))
			{
				string message = SconceStrings.FileDoesNotExist(sourcePath);
				Context.ShowErrorMessageBox(message);
				Tracer.Fail("The source file '{0}' does not exist. Not adding to the hierarchy.", sourcePath);
				return null;
			}
			string destRelPath = PackageUtility.MakeRelative(this.RootNode.AbsoluteDirectory, destinationPath);

			// Check to see if the source file is the same as the destination. If not, then we'll copy the file.
			if (!PackageUtility.FileStringEquals(sourcePath, destinationPath))
			{
				bool overwrite = false;

				// Try to find the file in the project.
				Node childNode = this.GetNodeFromName(destinationPath);
				if (childNode != null || File.Exists(destinationPath))
				{
					// The file already exists in the project/on disk. Ask the user if he/she wants to overwrite.
					string message = this.NativeResources.GetString(ResId.IDS_FILEALREADYEXISTS, Path.GetFileName(destinationPath));
					OLEMSGBUTTON buttons = OLEMSGBUTTON.OLEMSGBUTTON_YESNOCANCEL;
					OLEMSGDEFBUTTON defaultButton = OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND;
					OLEMSGICON icon = OLEMSGICON.OLEMSGICON_WARNING;
					VsMessageBoxResult result = Context.ShowMessageBox(message, buttons, defaultButton, icon);
					if (result == VsMessageBoxResult.Cancel)
					{
						canceled = true;
						Tracer.WriteLineInformation(classType, "AddCopyOfFile", "The user canceled the add copy of file operation.");
						return null;
					}
					else
					{
						overwrite = (result == VsMessageBoxResult.Yes);
					}
				}

				if (childNode == null || overwrite)
				{
					Tracer.Assert(!File.Exists(destinationPath) || overwrite, "The file '{0}' shouldn't exist!", destinationPath);
					try
					{
						if (File.Exists(destinationPath))
						{
							Tracer.WriteLineInformation(classType, "AddCopyOfFile", "Overwriting the existing file at '{0}' with '{1}'.", destinationPath, sourcePath);
						}
						else
						{
							Tracer.WriteLineInformation(classType, "AddCopyOfFile", "Copying '{0}' to '{1}'.", sourcePath, destinationPath);
						}
						// Copy the file to the right location.
						File.Copy(sourcePath, destinationPath, true);
						// Make the file writable.
						FileInfo destFile = new FileInfo(destinationPath);
						destFile.Attributes &= ~FileAttributes.ReadOnly;
					}
					catch (Exception e)
					{
						ResId stringId = (e is UnauthorizedAccessException ? ResId.IDS_E_COPYFILE_UNAUTHORIZED : ResId.IDS_E_COPYFILE);
						string message = this.NativeResources.GetString(stringId, sourcePath, destRelPath);
						Context.ShowErrorMessageBox(message);
						Tracer.Fail("Error in copying '{0}' to '{1}': {2}", sourcePath, destinationPath, e.ToString());
						return null;
					}
				}
			}

			// Add the node to the hierarchy.
			Node addedNode = this.AddExistingFile(destRelPath, true);

			return addedNode;
		}

		/// <summary>
		/// Adds a file to the hierarchy by copying it from its source path to the directory specified
		/// by the parent node. If the source file happens to reside in the same directory, then it
		/// is just added to the hierarchy.
		/// </summary>
		/// <param name="parentNode">The node that will contain the new file node.</param>
		/// <param name="sourcePath">The source path of the file to copy.</param>
		/// <param name="canceled">Indicates whether the user canceled the operation if there were prompts.</param>
		/// <returns>The <see cref="Node"/> of the file was copied and added to the hierarchy successfully; otherwise, null.</returns>
		public Node AddCopyOfFile(FolderNode parentNode, string sourcePath, out bool canceled)
		{
			Tracer.VerifyNonNullArgument(parentNode, "parentNode");
			Tracer.VerifyStringArgument(sourcePath, "sourcePath");

			string destinationPath = Path.Combine(parentNode.AbsoluteDirectory, Path.GetFileName(sourcePath));
			Node addedNode = this.AddCopyOfFile(sourcePath, destinationPath, out canceled);

			return addedNode;
		}

		/// <summary>
		/// Adds a project-relative file to the hierarchy. Also adds all of the sub-folders to the
		/// hierarchy if the file resides in a sub-directory off of the project root directory.
		/// </summary>
		/// <param name="relativePath">The relative path of the file to add.</param>
		/// <param name="mustExist">
		/// Indicates whether the file must exist in order to add it. If true and the file does
		/// not exist, an error message is shown to the user.
		/// </param>
		/// <returns>The <see cref="Node"/> of the file was added to the hierarchy successfully; otherwise, null.</returns>
		/// <remarks>The file is not copied before it is added.</remarks>
		public Node AddExistingFile(string relativePath, bool mustExist)
		{
			Tracer.VerifyStringArgument(relativePath, "relativePath");

			FolderNode parentNode = this.RootNode;
			string absolutePath = Path.Combine(this.RootNode.AbsoluteDirectory, relativePath);

			// Canonicalize both the absolute and relative path.
			absolutePath = PackageUtility.CanonicalizeFilePath(absolutePath);
			relativePath = PackageUtility.MakeRelative(this.RootNode.AbsoluteDirectory, absolutePath);

			// We can't add a non-existant file, so check for it.
			if (!File.Exists(absolutePath))
			{
				if (mustExist)
				{
					string message = SconceStrings.FileDoesNotExist(absolutePath);
					Context.ShowErrorMessageBox(message);
					Tracer.WriteLine(classType, "AddExistingFile", Tracer.Level.Information, "Trying to add a file that doesn't exist :'{0}'.", relativePath);
					return null;
				}
				else
				{
					// TODO: Change the status of the icon to show that the file does not exist.
				}
			}

			// Add all of the folders that are in the relative path if they're not already in the project.
			if (relativePath.IndexOf(Path.DirectorySeparatorChar) >= 0)
			{
				// Get an array of sub-folders from the root.
				string[] folderNames = relativePath.Split(Path.DirectorySeparatorChar);
				// The last element is the file name itself.
				for (int i = 0; i < folderNames.Length - 1; i++)
				{
					// This folder node will become the parent of the next folder node.
					parentNode = this.EnsureFolder(parentNode, folderNames[i]);
				}
			}

			// If the file isn't already in the hierarchy, then add it.
			Node node = this.GetNodeFromName(absolutePath);
			if (node == null)
			{
				node = this.CreateFileNodeFromExtension(absolutePath);
				parentNode.Children.Add(node);
			}

			return node;
		}

		/// <summary>
		/// Adds a new reference file to this folder. If the reference already exists, nothing happens.
		/// </summary>
		/// <param name="absolutePath">The absolute path to the reference file.</param>
		/// <param name="mustExist">
		/// Indicates whether the file must exist in order to add it. If true and the file does
		/// not exist, an error message is shown to the user.
		/// </param>
		public void AddReference(string absolutePath, bool mustExist)
		{
			// Check to see if the node is already in our children.
			if (this.ReferencesNode.Children.Contains(absolutePath))
			{
				return;
			}

			// Check to see if the file exists.
			bool fileExists = File.Exists(absolutePath);
			Tracer.WriteLineIf(classType, "AddReference", Tracer.Level.Information, !fileExists, "The specified reference file '{0}' does not exist.", absolutePath);

			if (!fileExists && mustExist)
			{
				Context.ShowErrorMessageBox(SconceStrings.FileDoesNotExist(absolutePath));
			}
			else
			{
				// Create the new node and add it to our collection.
				ReferenceFileNode newNode = this.CreateReferenceFileNode(absolutePath);
				this.ReferencesNode.Children.Add(newNode);

				if (!fileExists)
				{
					// TODO: Change the status of the icon to show that the file does not exist.
				}
			}
		}

		/// <summary>
		/// Creates a new file system folder and adds it to the hierarchy.
		/// </summary>
		/// <param name="parentNode">The parent node on which to add the new folder node.</param>
		/// <param name="name">The name of the new folder, which must be unique.</param>
		/// <returns>The <see cref="FolderNode"/> that was added to the hierarchy.</returns>
		/// <remarks>Call <see cref="FolderNode.GenerateUniqueName"/> to find a unique name.</remarks>
		public FolderNode CreateAndAddFolder(FolderNode parentNode, string name)
		{
			Tracer.VerifyNonNullArgument(parentNode, "parentNode");
			Tracer.VerifyStringArgument(name, "name");

			string absolutePath = Path.Combine(parentNode.AbsoluteDirectory, name);

			// Create the physical directory if it doesn't already exist.
			if (!Directory.Exists(absolutePath))
			{
				Directory.CreateDirectory(absolutePath);
			}

			// Add a new folder node to our hierarchy.
			FolderNode folderNode = new FolderNode(this, absolutePath);
			parentNode.Children.Add(folderNode);

			return folderNode;
		}

		/// <summary>
		/// Creates the most specific file node type from the file's extension.
		/// </summary>
		/// <param name="absolutePath">The path to the file.</param>
		/// <returns>The most specific <see cref="FileNode"/> object for the OS file.</returns>
		public virtual FileNode CreateFileNodeFromExtension(string absolutePath)
		{
			Tracer.VerifyStringArgument(absolutePath, "absolutePath");
			FileNode node = new FileNode(this, absolutePath);
			return node;
		}

		/// <summary>
		/// Creates a new <see cref="ReferenceFileNode"/>. Allows subclasses to create
		/// type-specific reference file nodes.
		/// </summary>
		/// <param name="absolutePath">The absolute path to the reference file.</param>
		/// <returns>A new <see cref="ReferenceFileNode"/>.</returns>
		public virtual ReferenceFileNode CreateReferenceFileNode(string absolutePath)
		{
			return new ReferenceFileNode(this, absolutePath);
		}

		/// <summary>
		/// Ensures that the folder node is in the hierarchy and that it exists on disk.
		/// </summary>
		/// <param name="parentNode">The parent node that should contain the folder.</param>
		/// <param name="folderName">The name of the folder.</param>
		/// <returns>Either the existing folder node or the newly created folder node.</returns>
		public FolderNode EnsureFolder(FolderNode parentNode, string folderName)
		{
			Tracer.VerifyNonNullArgument(parentNode, "parentNode");
			Tracer.VerifyStringArgument(folderName, "folderName");

			// See if there is already a node in the hierarchy with this name.
			string folderPath = PackageUtility.CanonicalizeDirectoryPath(Path.Combine(parentNode.AbsoluteDirectory, folderName));
			Node foundNode = this.GetNodeFromName(folderPath);
			FolderNode folderNode = foundNode as FolderNode;
			if (foundNode != null && folderNode == null)
			{
				Tracer.WriteLine(classType, "EnsureFolder", Tracer.Level.Warning, "There is already a non-folder node {0} in the hierarchy.", foundNode);
			}
			else if (foundNode == null || folderNode == null)
			{
				// We need to add the folder node to the hierarchy.
				folderNode = new FolderNode(this, folderPath);
				parentNode.Children.Add(folderNode);
			}

			// Make sure the folder exists.
			if (!Directory.Exists(folderPath))
			{
				Tracer.WriteLineInformation(classType, "EnsureFolder", "Creating directory '{0}' in the file system.", folderPath);
				Directory.CreateDirectory(folderPath);
			}

			return folderNode;
		}

		/// <summary>
		/// Finds the node with the specified hierarchy identifier.
		/// </summary>
		/// <param name="hierarchyId">The hierarchy identifier of the node to find.</param>
		/// <param name="throwOnNotFound">Indicates whether an exception is thrown if the node is not found.</param>
		/// <returns>The node with the specified hierarchy identifier, or null if the node is not found.</returns>
		public Node GetNode(uint hierarchyId, bool throwOnNotFound)
		{
			Node node = null;

			if (hierarchyId == NativeMethods.VSITEMID_ROOT)
			{
				node = this.RootNode;
			}
			else if (hierarchyId == NativeMethods.VSITEMID_NIL)
			{
				node = null;
			}
			else if (hierarchyId == NativeMethods.VSITEMID_SELECTION)
			{
				node = this.SelectedNode;
			}
			else
			{
				node = this.RootNode.FindById(hierarchyId, true);
			}

			if (throwOnNotFound && node == null)
			{
				string message = PackageUtility.SafeStringFormatInvariant("Cannot find node {0} in our hierarchy!", hierarchyId);
				Tracer.Fail(message);
				Marshal.ThrowExceptionForHR(NativeMethods.DISP_E_MEMBERNOTFOUND);
			}
			return node;
		}

		public Node GetNodeFromName(string canonicalName)
		{
			return this.RootNode.FindByName(canonicalName, true);
		}

		/// <summary>
		/// Gets the value of the specified property.
		/// </summary>
		/// <param name="hierarchyId">The Id of the hierarchy node from which to retrieve the property.</param>
		/// <param name="propertyId">The Id of the property to retrieve.</param>
		/// <param name="propertyValue">The value of the specified property, or null if the property is not supported.</param>
		/// <returns>true if the property is supported; otherwise false.</returns>
		public virtual bool GetProperty(uint hierarchyId, __VSHPROPID propertyId, out object propertyValue)
		{
			bool supported = true;
			Node node = null;
			propertyValue = null;

			// First we'll catch any properties that we're interested in.
			switch (propertyId)
			{
				case __VSHPROPID.VSHPROPID_Root:
					propertyValue = this.RootNode.HierarchyId;
					break;

				default:
					supported = false;
					break;
			}

			// If we didn't catch the property, then let's let the node have a chance to deal with it.
			if (!supported)
			{
				// Get the hierarchy node.
				if (hierarchyId == NativeMethods.VSITEMID_NIL)
				{
					// We can't return a property on a null node. We won't do anything here,
					// but we'll make sure that we don't do anything on our null node below.
				}
				else if (hierarchyId == NativeMethods.VSITEMID_SELECTION)
				{
					node = this.SelectedNode;
				}
				else
				{
					// Attempt to find the node in our hierarchy (don't throw if we can't find it).
					node = this.GetNode(hierarchyId, false);
				}

				// Let the node handle the property.
				if (node != null)
				{
					supported = node.GetProperty(propertyId, out propertyValue);
				}
			}

			// Increment our counters that are used in the trace summary.
			if (supported)
			{
				this.IncrementSupportedGetProperty(propertyId);
			}
			else
			{
				this.IncrementUnsupportedGetProperty(propertyId);
			}

			return supported;
		}

		/// <summary>
		/// Gets a service from the environment using the cached service provider.
		/// </summary>
		/// <param name="serviceType">An object that specifies the type of service object to get.</param>
		/// <returns>
		/// A service object of type <paramref name="serviceType"/>, or null if there is no service
		/// object of type <paramref name="serviceType"/>.
		/// </returns>
		public object GetService(Type serviceType)
		{
			return this.serviceProvider.GetService(serviceType);
		}

		public void OnInvalidateItems()
		{
			this.OnInvalidateItems(this.RootNode);
		}

		public void OnInvalidateItems(Node parent)
		{
			if (parent == null)
			{
				parent = this.RootNode;
			}
			this.eventListeners.OnInvalidateItems(parent);
		}

		public void OnItemAdded(Node node)
		{
			// Let all of our listeners know that an item was added.
			this.eventListeners.OnItemAdded(node);
			// Refresh the hierarchy display.
			this.OnInvalidateItems(node.Parent);
		}

		public void OnItemDeleted(Node node)
		{
			// Let all of our listeners know that an item was deleted.
			this.eventListeners.OnItemDeleted(node);
			// Refresh the hierarchy display.
			this.OnInvalidateItems(node.Parent);
		}

		public void OnPropertyChanged(Node node, __VSHPROPID propertyId)
		{
			// Let all of our listeners know that an item was changed.
			this.eventListeners.OnPropertyChanged(node, propertyId);
		}

		/// <summary>
		/// Sets the value of the specified property.
		/// </summary>
		/// <param name="hierarchyId">The Id of the hierarchy node on which to set the property.</param>
		/// <param name="propertyId">The Id of the property to set.</param>
		/// <param name="value">The value to set.</param>
		/// <returns>
		/// An HRESULT that is the result of the operation. Usually S_OK indicates that the property was set.
		/// If the property is not supported, returns DISP_E_MEMBERNOTFOUND.
		/// </returns>
		public virtual int SetProperty(uint hierarchyId, __VSHPROPID propertyId, object value)
		{
			int hr;
			Node node = null;

			// Get the hierarchy node.
			if (hierarchyId == NativeMethods.VSITEMID_NIL)
			{
				this.IncrementUnsupportedSetProperty(propertyId);
				return NativeMethods.DISP_E_MEMBERNOTFOUND;
			}
			else if (hierarchyId == NativeMethods.VSITEMID_SELECTION)
			{
				node = this.SelectedNode;
			}
			else
			{
				// Attempt to find the node in our hierarchy (throw if we can't find it).
				node = this.GetNode(hierarchyId, true);
			}
			Tracer.Assert(node != null, "node should not have been null by this point.");
			hr = node.SetProperty(propertyId, value);

			if (NativeMethods.Succeeded(hr))
			{
				this.IncrementSupportedSetProperty(propertyId);
			}
			else
			{
				this.IncrementUnsupportedSetProperty(propertyId);
			}

			return hr;
		}

		/// <summary>
		/// Shows the standard Visual Studio "Add New File" or "Add Existing File" dialog.
		/// </summary>
		/// <param name="parent">The parent in which to add the file.</param>
		/// <param name="dialogType">Specifies whether the "Add New File" or "Add Existing File" dialog is shown.</param>
		public void ShowAddFileDialogBox(FolderNode parent, AddFileDialogType dialogType)
		{
			Tracer.VerifyNonNullArgument(parent, "parent");

			// Get an instance of the dialog from Visual Studio.
			IVsAddProjectItemDlg dialog = (IVsAddProjectItemDlg)this.ServiceProvider.GetServiceOrThrow(typeof(SVsAddProjectItemDlg), typeof(IVsAddProjectItemDlg), classType, "ShowAddFileDialogBox");

			uint flags = 0;
			switch (dialogType)
			{
				case AddFileDialogType.AddNew:
					flags = (uint)(__VSADDITEMFLAGS.VSADDITEM_AddNewItems | __VSADDITEMFLAGS.VSADDITEM_SuggestTemplateName);
					break;

				case AddFileDialogType.AddExisting:
					flags = (uint)(__VSADDITEMFLAGS.VSADDITEM_AddExistingItems | __VSADDITEMFLAGS.VSADDITEM_AllowMultiSelect | __VSADDITEMFLAGS.VSADDITEM_AllowStickyFilter);
					break;

				default:
					flags = 0;
					Tracer.Fail("Shouldn't be hitting here.");
					break;
			}

			IVsProject projectInterface = (IVsProject)this.AttachedProject;
			Guid projectTypeGuid = this.AttachedProject.ProjectTypeGuid;
			string browseLocation = parent.AbsoluteDirectory;
			string filter = this.NativeResources.GetString(ResId.IDS_OPENFILES_FILTER);
			int dontShowAgain;

			// TODO: Respect the sticky filter and "Don't show again" flag.

			int hr = dialog.AddProjectItemDlg(parent.HierarchyId, ref projectTypeGuid, projectInterface, flags, null, null, ref browseLocation, ref filter, out dontShowAgain);
			NativeMethods.ThrowOnFailure(hr, NativeMethods.OLE_E_PROMPTSAVECANCELLED);
		}

		/// <summary>
		/// Shows the Visual Studio standard "Add Reference" dialog. The dialog calls into
		/// IVsComponentUser.AddComponent, which is implemented in this class.
		/// </summary>
		public virtual void ShowAddReferenceDialog()
		{
			Guid emptyGuid = Guid.Empty;
			Guid showOnlyThisTabGuid = Guid.Empty;
			Guid startOnThisTabGuid = Guid.Empty;
			string helpTopic = String.Empty;
			string machineName = String.Empty;
			string browseFilters = this.AddReferenceDialogFilter;
			string browseLocation = this.AddReferenceDialogInitialDirectory;

			// Initialize the structure that we have to pass into the dialog call.
			uint tabInitializesArrayLength = 0;
			VSCOMPONENTSELECTORTABINIT[] tabInitializers = new VSCOMPONENTSELECTORTABINIT[1];
			tabInitializers[0].dwSize = (uint)Marshal.SizeOf(typeof(VSCOMPONENTSELECTORTABINIT));
			tabInitializers[0].guidTab = Guid.Empty;
			tabInitializers[0].varTabInitInfo = 0;

			// Initialize the flags to control the dialog.
			// TODO: Support project references.
			__VSCOMPSELFLAGS flags = __VSCOMPSELFLAGS.VSCOMSEL_HideCOMClassicTab |
				__VSCOMPSELFLAGS.VSCOMSEL_HideCOMPlusTab |
				__VSCOMPSELFLAGS.VSCOMSEL_IgnoreMachineName |
				__VSCOMPSELFLAGS.VSCOMSEL_MultiSelectMode;

			// Get the dialog service from the environment.
			IVsComponentSelectorDlg dialog = (IVsComponentSelectorDlg)this.GetService(typeof(SVsComponentSelectorDlg));

			// Show the dialog.
			int hr = dialog.ComponentSelectorDlg(
				(uint)flags,
				(IVsComponentUser)this,
				this.AddReferenceDialogTitle,
				helpTopic,
				ref showOnlyThisTabGuid,
				ref startOnThisTabGuid,
				machineName,
				tabInitializesArrayLength,
				tabInitializers,
				browseFilters,
				ref browseLocation);

			if (NativeMethods.Failed(hr))
			{
				Tracer.WriteLineWarning(classType, "ShowAddReferenceDialog", "The Add Reference dialog failed to show. Hr=0x{0:x}", hr);
				return;
			}
		}

		/// <summary>
		/// Clears the dirty flag for this hierarchy and for all of its nodes.
		/// </summary>
		protected override void ClearDirtyOnContainedObjects()
		{
			base.ClearDirtyOnContainedObjects();
			this.RootNode.ClearDirty();
		}

		/// <summary>
		/// Creates a new <see cref="ProjectNode"/> object. Allows subclasses to create a
		/// type-specific root node.
		/// </summary>
		/// <returns>A new <see cref="ProjectNode"/> object.</returns>
		protected virtual ProjectNode CreateProjectNode()
		{
			return new ProjectNode(this);
		}

		protected virtual bool ExecuteStandard97Command(Node node, VsCommand command)
		{
			Tracer.VerifyNonNullArgument(node, "node");

			// Give the node first dibs on executing the command.
			bool supported = node.ExecuteStandard97Command(command);
			Tracer.WriteLineIf(classType, "ExecuteStandard97Command", Tracer.Level.Verbose, !supported, "Not executing the command '{0}'", command);
			return supported;
		}

		protected virtual bool ExecuteStandard2KCommand(Node node, VsCommand2K command)
		{
			Tracer.VerifyNonNullArgument(node, "node");

			bool supported = node.ExecuteStandard2KCommand(command);
			Tracer.WriteLineIf(classType, "ExecuteStandard2KCommand", Tracer.Level.Verbose, !supported, "Not executing the command '{0}'", command);
			return supported;
		}

		protected void IncrementSupportedGetProperty(__VSHPROPID propertyId)
		{
			this.IncrementPropertyCount(propertyId, this.supportedGets);
		}

		protected void IncrementSupportedSetProperty(__VSHPROPID propertyId)
		{
			this.IncrementPropertyCount(propertyId, this.supportedSets);
		}

		protected void IncrementUnsupportedGetProperty(__VSHPROPID propertyId)
		{
			this.IncrementPropertyCount(propertyId, this.unsupportedGets);
		}

		protected void IncrementUnsupportedSetProperty(__VSHPROPID propertyId)
		{
			this.IncrementPropertyCount(propertyId, this.unsupportedSets);
		}

		private void IncrementPropertyCount(__VSHPROPID propertyId, Hashtable hashtable)
		{
			if (hashtable.ContainsKey(propertyId))
			{
				int count = (int)hashtable[propertyId];
				hashtable[propertyId] = ++count;
			}
			else
			{
				hashtable.Add(propertyId, 1);
			}
		}

		private int QuerySingleStatusCommand(Node node, Guid commandGroup, uint command, out uint commandFlags)
		{
			int hr = NativeMethods.S_OK;
			CommandStatus commandStatus = CommandStatus.NotSupportedOrEnabled;

			if (commandGroup == VsMenus.StandardCommandSet97)
			{
				VsCommand vsCommand = (VsCommand)command;
				if (node != null)
				{
					commandStatus = node.QueryStandard97CommandStatus(vsCommand);
				}
				Tracer.WriteLine(classType, "QuerySingleStatusCommand", Tracer.Level.Verbose, "Command '{0}' is {1} on item {2}", vsCommand, commandStatus, node);
			}
			else if (commandGroup == VsMenus.StandardCommandSet2K)
			{
				VsCommand2K command2K = (VsCommand2K)command;
				if (node != null)
				{
					commandStatus = node.QueryStandard2KCommandStatus(command2K);
				}
				Tracer.WriteLine(classType, "QuerySingleStatusCommand", Tracer.Level.Verbose, "Command '{0}' is {1} on item {2}", command2K, commandStatus, node);
			}
			else
			{
				hr = NativeMethods.OLECMDERR_E_UNKNOWNGROUP;
				Tracer.WriteLine(classType, "QuerySingleStatusCommand", Tracer.Level.Verbose, "Command group {0} is not supported on item {1}.", commandGroup.ToString("B"), node);
			}

			if (commandStatus == CommandStatus.Unhandled)
			{
				commandStatus = CommandStatus.NotSupportedOrEnabled;
				hr = NativeMethods.OLECMDERR_E_NOTSUPPORTED;
			}
			commandFlags = unchecked((uint)commandStatus);
			return hr;
		}

		private void WritePropertySummary(Hashtable hashtable, string description)
		{
			Tracer.WriteLine(classType, "WritePropertySummary", Tracer.Level.Summary, description);
			Tracer.Indent();
			foreach (DictionaryEntry entry in hashtable)
			{
				__VSHPROPID vsPropId = (__VSHPROPID)entry.Key;
				int count = (int)entry.Value;
				string propertyName;
				// There are some special values, mainly duplicates that we want to print out a
				// little differently. Check them here.
				switch (vsPropId)
				{
					case __VSHPROPID.VSHPROPID_FIRST:
						propertyName = "VSHPROPID_DefaultEnableDeployProjectCfg";
						break;

					case __VSHPROPID.VSHPROPID_LAST:
						propertyName = "VSHPROPID_Parent";
						break;

					case __VSHPROPID.VSHPROPID_ProjectName:
						propertyName = "VSHPROPID_Name (VSHPROPID_ProjectName)";
						break;

					case __VSHPROPID.VSHPROPID_TypeName:
						propertyName = "VSHPROPID_TypeName (VSHPROPID_ProjectType)";
						break;

					default:
						propertyName = vsPropId.ToString();
						break;
				}
				Tracer.WriteLine(classType, "WritePropertySummary", Tracer.Level.Summary, "{0} was requested {1} times", propertyName, count);
			}
			Tracer.Unindent();
		}

		private void WriteSummary(object sender, EventArgs e)
		{
			string projectEnd = " for project '" + this.RootNode.Caption + "':";
			WritePropertySummary(this.supportedGets, "Summary of supported get properties" + projectEnd);
			WritePropertySummary(this.unsupportedGets, "Summary of unsupported get properties" + projectEnd);
			WritePropertySummary(this.supportedSets, "Summary of supported set properties" + projectEnd);
			WritePropertySummary(this.unsupportedSets, "Summary of unsupported set properties" + projectEnd);
		}
		#endregion
	}
}
