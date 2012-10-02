//-------------------------------------------------------------------------------------------------
// <copyright file="DocumentInfo.cs" company="Microsoft">
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
// Represents an entry in the environment's RDT (Running Document Table)
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Globalization;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Contains a thin wrapper around the Visual Studio RDT (Running Document Table) functions
	/// to get information about currently opened documents.
	/// </summary>
	public class DocumentInfo
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		public static readonly Type classType = typeof(DocumentInfo);

		/// <summary>
		/// Represents a null document cookie value (VSDOCCOOKIE_NIL).
		/// </summary>
		public static readonly uint NullCookie = unchecked((uint)Microsoft.VisualStudio.Shell.Interop.Constants.VSDOCCOOKIE_NIL);

		private string absolutePath;
		private uint cookie = NullCookie;
		private object documentData;
		private uint hierarchyId = NativeMethods.VSITEMID_NIL;
		private bool isOpen;
		private IVsHierarchy visualStudioHierarchy;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentInfo"/> class.
		/// </summary>
		public DocumentInfo(string absolutePath, IVsHierarchy vsHierarchy, uint hierarchyId, object documentData, uint cookie)
		{
			Tracer.VerifyStringArgument(absolutePath, "absolutePath");
			Tracer.VerifyNonNullArgument(documentData, "documentData");

			this.absolutePath = absolutePath;
			this.visualStudioHierarchy = vsHierarchy;
			this.hierarchyId = hierarchyId;
			this.documentData = documentData;
			this.cookie = cookie;

			// The document is open if it has a hierarchy and a cookie.
			this.isOpen = (vsHierarchy != null && cookie != NullCookie);
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the absolute path of the document.
		/// </summary>
		public string AbsolutePath
		{
			get { return this.absolutePath; }
		}

		/// <summary>
		/// Gets the document cookie.
		/// </summary>
		public uint Cookie
		{
			get { return this.cookie; }
		}

		/// <summary>
		/// Gets the document data object, which could be a myriad of different interfaces depending
		/// on the context.
		/// </summary>
		public object DocumentData
		{
			get { return this.documentData; }
		}

		/// <summary>
		/// Gets the hierarchy identifier of the document.
		/// </summary>
		public uint HierarchyId
		{
			get { return this.hierarchyId; }
		}

		/// <summary>
		/// Gets a flag indicating whether the document is dirty and needs to be saved.
		/// </summary>
		public bool IsDirty
		{
			get
			{
				int dirty;

				// First try casting our documentData to a IVsPersistDocData to see if we can use
				// that interface to find out if the document is dirty.
				IVsPersistDocData vsPersistDocData = this.DocumentData as IVsPersistDocData;
				if (vsPersistDocData != null)
				{
					Tracer.WriteLineVerbose(classType, "get_IsDirty", "Succeeded in casting this.documentData to a IVsPersistDocData object.");
					int hr = vsPersistDocData.IsDocDataDirty(out dirty);
					NativeMethods.ThrowOnFailure(hr);
				}
				else
				{
					// Ok, now let's try IVsPersistHierarchyItem since that also has a way to
					// check if the document is dirty. We do that by seeing if our IVsHierarcy
					// pointer can be cast to a IVsPersistHierarchyItem.
					IVsPersistHierarchyItem vsPersistHierarchyItem = this.VisualStudioHierarhcy as IVsPersistHierarchyItem;
					if (vsPersistHierarchyItem != null)
					{
						Tracer.WriteLineVerbose(classType, "get_IsDirty", "Succeeded in casting this.VisualStudioHierarchy to a IVsPersistHierarchyItem object.");
						IntPtr docDataPtr = Marshal.GetIUnknownForObject(this.documentData);
						try
						{
							int hr = vsPersistHierarchyItem.IsItemDirty(this.HierarchyId, docDataPtr, out dirty);
							NativeMethods.ThrowOnFailure(hr);
						}
						finally
						{
							Marshal.Release(docDataPtr);
						}
					}
					else
					{
						// Don't know what else to do at this point but show an internal error message box.
						string message = PackageUtility.SafeStringFormatInvariant("We could not succeed in finding out if the item '{0}' is dirty.", this.AbsolutePath);
						Tracer.WriteLine(classType, "get_IsDirty", Tracer.Level.Critical, message);
						Package.Instance.Context.NotifyInternalError(message);
						dirty = 0;
					}
				}

				return (dirty != 0);
			}
		}

		/// <summary>
		/// Gets a flag indicating whether the document is open.
		/// </summary>
		public bool IsOpen
		{
			get { return this.isOpen; }
		}

		/// <summary>
		/// Gets the <see cref="IVsHierarchy"/> that owns this document.
		/// </summary>
		public IVsHierarchy VisualStudioHierarhcy
		{
			get { return this.visualStudioHierarchy; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Returns a value indicating whether the <see cref="DocumentData"/> supports the specified
		/// Visual Studio interface.
		/// </summary>
		/// <param name="visualStudioInterface">An interface to check.</param>
		/// <returns>true if <see cref="DocumentData"/> supports the interface; otherwise, false.</returns>
		public bool SupportsInterface(Type visualStudioInterface)
		{
			return visualStudioInterface.IsAssignableFrom(this.DocumentData.GetType());
		}
		#endregion
	}
}
