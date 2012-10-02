//--------------------------------------------------------------------------------------------------
// <copyright file="RunningDocumentTable.cs" company="Microsoft">
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
// Wrapper class around the Visual Studio environment's RDT (Running Document Table).
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Provides useful wrapper methods around the Visual Studio environment's RDT (Running Document Table).
	/// </summary>
	public class RunningDocumentTable
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(RunningDocumentTable);
		private static readonly IntPtr HierarchyDontChange = new IntPtr(-1);

		private ServiceProvider serviceProvider;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="RunningDocumentTable"/> class.
		/// </summary>
		/// <param name="serviceProvider">The <see cref="ServiceProvider"/> to use for getting services from the environment.</param>
		public RunningDocumentTable(ServiceProvider serviceProvider)
		{
			Tracer.VerifyNonNullArgument(serviceProvider, "serviceProvider");
			this.serviceProvider = serviceProvider;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets a <see cref="IVsRunningDocumentTable"/> pointer from the environment.
		/// </summary>
		private IVsRunningDocumentTable Rdt
		{
			get
			{
				return (IVsRunningDocumentTable)this.serviceProvider.GetServiceOrThrow(typeof(SVsRunningDocumentTable), typeof(IVsRunningDocumentTable), classType, "Rdt");
			}
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Attempts to find the document information in the Running Document Table from the specified cookie.
		/// </summary>
		/// <param name="documentCookie">The cookie of the document to search for.</param>
		/// <returns>A <see cref="DocumentInfo"/> object if the file was found; otherwise, null.</returns>
		public DocumentInfo FindByCookie(uint documentCookie)
		{
			DocumentInfo docInfo = null;

			if (documentCookie == DocumentInfo.NullCookie)
			{
				return null;
			}

			// Get the document info.
			uint rdtFlags, readLocks, editLocks;
			string path;
			IVsHierarchy vsHierarchy;
			uint hierarchyId;
			IntPtr punkDocData;
			int hr = this.Rdt.GetDocumentInfo(documentCookie, out rdtFlags, out readLocks, out editLocks, out path, out vsHierarchy, out hierarchyId, out punkDocData);
			NativeMethods.ThrowOnFailure(hr);

			if (punkDocData != IntPtr.Zero)
			{
				try
				{
					object docData = Marshal.GetObjectForIUnknown(punkDocData);
					Tracer.Assert(docData != null, "We should be getting something for punkDocData instead of null.");
					if (docData != null)
					{
						// Create the new object.
						docInfo = new DocumentInfo(path, vsHierarchy, hierarchyId, docData, documentCookie);
					}
				}
				finally
				{
					Marshal.Release(punkDocData);
				}
			}

			return docInfo;
		}

		/// <summary>
		/// Attempts to find the document information for the specified file.
		/// </summary>
		/// <param name="absolutePath">The absolute path of the file to search for.</param>
		/// <returns>A <see cref="DocumentInfo"/> object if the file was found; otherwise, null.</returns>
		public DocumentInfo FindByPath(string absolutePath)
		{
			Tracer.VerifyStringArgument(absolutePath, "absolutePath");

			DocumentInfo documentInfo = null;

			// Make the call to IVsRunningDocumentTable.FindAndLockDocument to try to find the document.
			uint lockType = unchecked((uint)_VSRDTFLAGS.RDT_NoLock);
			IVsHierarchy vsHierarchy;
			uint hierarchyId;
			IntPtr punkDocData;
			uint cookie;
			int hr = this.Rdt.FindAndLockDocument(lockType, absolutePath, out vsHierarchy, out hierarchyId, out punkDocData, out cookie);
			NativeMethods.ThrowOnFailure(hr);

			if (punkDocData != IntPtr.Zero)
			{
				try
				{
					object docData = Marshal.GetObjectForIUnknown(punkDocData);
					Tracer.Assert(docData != null, "We should be getting something for punkDocData instead of null.");
					if (docData != null)
					{
						documentInfo = new DocumentInfo(absolutePath, vsHierarchy, hierarchyId, docData, cookie);
					}
				}
				finally
				{
					Marshal.Release(punkDocData);
				}
			}

			return documentInfo;
		}

		/// <summary>
		/// Renames and/or changes the ownership of a document.
		/// </summary>
		/// <param name="oldFilePath">Absolute path to the previous document.</param>
		/// <param name="newFilePath">Absolute path to the current document.</param>
		/// <param name="newHierarchyId">The hierarchy identifier of the current document or 0 if no change.</param>
		public void RenameDocument(string oldFilePath, string newFilePath, uint newHierarchyId)
		{
			int hr;

			Tracer.VerifyStringArgument(oldFilePath, "oldFilePath");
			Tracer.VerifyStringArgument(newFilePath, "newFilePath");
			if (newHierarchyId == NativeMethods.VSITEMID_NIL)
			{
				throw new ArgumentException("Cannot specify VSITEMID_NIL for the new hierarchy id.", "newHierarchyId");
			}

			// See if the document needs to be renamed (if it's in the RDT).
			DocumentInfo docInfo = this.FindByPath(oldFilePath);
			if (docInfo == null)
			{
				return;
			}

			// Get an IUnknown pointer for the new hierarchy.
			IntPtr punkHierarchy = Marshal.GetIUnknownForObject(docInfo.VisualStudioHierarhcy);
			if (punkHierarchy != IntPtr.Zero)
			{
				try
				{
					// Get an IVsHierarchy pointer. We have to do this two-step process of getting an IUnknown
					// and then querying for an IVsHierarchy because in the nested hierarchy case we could get
					// different pointers.
					IntPtr pvsHierarchy = IntPtr.Zero;
					Guid vsHierarchyGuid = typeof(IVsHierarchy).GUID;
					NativeMethods.ThrowOnFailure(Marshal.QueryInterface(punkHierarchy, ref vsHierarchyGuid, out pvsHierarchy));

					try
					{
						hr = this.Rdt.RenameDocument(oldFilePath, newFilePath, pvsHierarchy, newHierarchyId);
						NativeMethods.ThrowOnFailure(hr);
					}
					finally
					{
						Marshal.Release(pvsHierarchy);
					}
				}
				finally
				{
					Marshal.Release(punkHierarchy);
				}
			}
		}

		/// <summary>
		/// Saves the document if it is dirty.
		/// </summary>
		/// <param name="filePath">The absolute path to the file.</param>
		public void SaveIfDirty(string filePath)
		{
			DocumentInfo docInfo = this.FindByPath(filePath);
			if (docInfo != null && docInfo.IsDirty && docInfo.SupportsInterface(typeof(IVsPersistDocData)))
			{
				string newPath;
				int saveCanceled;
				IVsPersistDocData persistDocData = docInfo.DocumentData as IVsPersistDocData;
				Tracer.Assert(persistDocData != null, "DocumentInfo.SupportsInterface returned true when it shouldn't have.");
				if (persistDocData != null)
				{
					int hr = persistDocData.SaveDocData(VSSAVEFLAGS.VSSAVE_SilentSave, out newPath, out saveCanceled);
					Tracer.WriteLineIf(classType, "SaveIfDirty", Tracer.Level.Information, NativeMethods.Succeeded(hr), "Successfully saved '{0}'.", filePath);
					NativeMethods.ThrowOnFailure(hr);
				}
			}
		}
		#endregion
	}
}