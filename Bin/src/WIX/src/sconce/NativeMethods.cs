//-------------------------------------------------------------------------------------------------
// <copyright file="NativeMethods.cs" company="Microsoft">
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
// Contains native Win32 and COM methods and constants.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.OLE.Interop;

	using IServiceProvider = System.IServiceProvider;
	using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

	/// <summary>
	/// Contains COM interfaces and native methods and constants.
	/// </summary>
	public sealed class NativeMethods
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		/// <summary>Max length of full path name.</summary>
		public const int MAX_PATH = 260;

		/// <summary>IID of the <see cref="IObjectWithSite"/> COM interface.</summary>
		public static readonly Guid IID_IObjectWithSite = typeof(IObjectWithSite).GUID;
		/// <summary>IID of the <see cref="IServiceProvider"/> COM interface.</summary>
		public static readonly Guid IID_IOleServiceProvider = typeof(IOleServiceProvider).GUID;
		/// <summary>IID of the <see cref="System.IServiceProvider"/> interface.</summary>
		public static readonly Guid IID_IServiceProvider = typeof(IServiceProvider).GUID;
		/// <summary>IID of the <b>IUnknown</b> COM interface.</summary>
		public static readonly Guid IID_IUnknown = new Guid("{00000000-0000-0000-C000-000000000046}");

		/// <summary>HRESULT for generic success.</summary>
		public const int S_OK = 0x00000000;
		/// <summary>HRESULT for success, but a false return value.</summary>
		public const int S_FALSE = 0x00000001;

		/// <summary>Error HRESULT for when the user canceled out of save dialog.</summary>
		public const int OLE_E_PROMPTSAVECANCELLED = unchecked((int)0x8004000C);
		/// <summary>Error HRESULT for a not supported OLE Command.</summary>
		public const int OLECMDERR_E_NOTSUPPORTED = unchecked((int)0x80040100);
		/// <summary>Error HRESULT for an unknown OLE Command.</summary>
		public const int OLECMDERR_E_UNKNOWNGROUP = unchecked((int)0x80040104);


		/// <summary>Error HRESULT for the request of a not implemented method.</summary>
		public const int E_NOTIMPL = unchecked((int)0x80004001);
		/// <summary>Error HRESULT for the request of a not implemented interface.</summary>
		public const int E_NOINTERFACE = unchecked((int)0x80004002);
		/// <summary>Error HRESULT for a generic error.</summary>
		public const int E_FAIL = unchecked((int)0x80004005);
		/// <summary>Error HRESULT for an unexpected condition.</summary>
		public const int E_UNEXPECTED = unchecked((int)0x8000FFFF);

		/// <summary>Error HRESULT for a member not found.</summary>
		public const int DISP_E_MEMBERNOTFOUND = unchecked((int)0x80020003);

		/// <summary>
		/// Special item identifier that represents the absence of a project item. This value
		/// is used when there is no current selection.
		/// </summary>
		public const uint VSITEMID_NIL = unchecked((uint)-1);
		/// <summary>
		/// Special item identifier that represents the root of a project hierarchy and is used
		/// to identify the entire hierarchy, as opposed to a single item.
		/// </summary>
		public const uint VSITEMID_ROOT = unchecked((uint)-2);
		/// <summary>
		/// Special item identifier that represents the currently selected item or items,
		/// which can include the root of the hierarchy. <b>VSITEMID_SELECTION</b> is returned
		/// by <b>IVsMonitorSelection::GetCurrentSelection</b> to indicate a selection made
		/// up of multiple items.
		/// </summary>
		public const uint VSITEMID_SELECTION = unchecked((uint)-3);

		/// <summary>
		/// Used in calls to IVsUIShellOpenDocument.OpenStandardEditor or OpenSpecificEditor to indicate
		/// that the function should query the RDT (running document table) before opening the document
		/// to see if it's already opened.
		/// </summary>
		public static readonly IntPtr DOCDATAEXISTING_Unknown = new IntPtr(-1);

		// Standard item types, to be returned from VSHPROPID_TypeGuid
		//------------------------------------------------------------
		/// <summary>Physical file on disk or web (IVsProject::GetMkDocument returns a file path).</summary>
		public static readonly Guid GUID_ItemType_PhysicalFile = new Guid("{6bb5f8ee-4483-11d3-8bcf-00c04f8ec28c}");
		/// <summary>Physical folder on disk or web (IVsProject::GetMkDocument returns a directory path).</summary>
		public static readonly Guid GUID_ItemType_PhysicalFolder = new Guid("{6bb5f8ef-4483-11d3-8bcf-00c04f8ec28c}");
		/// <summary>Non-physical folder (folder is logical and not a physical file system directory).</summary>
		public static readonly Guid GUID_ItemType_VirtualFolder = new Guid("{6bb5f8f0-4483-11d3-8bcf-00c04f8ec28c}");
		/// <summary>A nested hierarchy project.</summary>
		public static readonly Guid GUID_ItemType_SubProject = new Guid("{EA6618E8-6E24-4528-94BE-6889FE16485C}");

		// ShowWindow() Commands
		public const int SW_HIDE = 0;
		public const int SW_SHOWNORMAL = 1;
		public const int SW_NORMAL = 1;
		public const int SW_SHOWMINIMIZED = 2;
		public const int SW_SHOWMAXIMIZED = 3;
		public const int SW_MAXIMIZE = 3;
		public const int SW_SHOWNOACTIVATE = 4;
		public const int SW_SHOW = 5;
		public const int SW_MINIMIZE = 6;
		public const int SW_SHOWMINNOACTIVE = 7;
		public const int SW_SHOWNA = 8;
		public const int SW_RESTORE = 9;
		public const int SW_SHOWDEFAULT = 10;
		public const int SW_FORCEMINIMIZE = 11;
		public const int SW_MAX = 11;

		public const int WM_KEYFIRST = 0x0100;
		public const int WM_KEYLAST = 0x0108;
		public const int WM_MOUSEFIRST = 0x0200;
		public const int WM_MOUSELAST = 0x020A;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		///     Prevent direct instantiation of this class.
		/// </summary>
		private NativeMethods()
		{
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		///     Returns a value indicating whether the HRESULT is a success return code.
		/// </summary>
		/// <param name="hr">
		///     The HRESULT to check.
		/// </param>
		/// <returns>
		///     <see langword="true"/> if <paramref name="hr"/> is a success return code; otherwise,
		///     <see langword="false"/>.
		/// </returns>
		public static bool Succeeded(int hr)
		{
			return (hr >= 0);
		}

		/// <summary>
		///     Returns a value indicating whether the HRESULT is an error return code.
		/// </summary>
		/// <param name="hr">
		///     The HRESULT to check.
		/// </param>
		/// <returns>
		///     <see langword="true"/> if <paramref name="hr"/> is an error return code; otherwise,
		///     <see langword="false"/>.
		/// </returns>
		public static bool Failed(int hr)
		{
			return (hr < 0);
		}

		/// <summary>
		///     Checks if the parameter is a success or failure HRESULT and throws an exception
		///     if it is a failure that is not included in the array of well-known failures.
		/// </summary>
		/// <param name="hr">
		///     The HRESULT to test.
		/// </param>
		/// <param name="expectedHRFailure">
		///     Array of well-known and expected failures.
		/// </param>
		public static int ThrowOnFailure(int hr, params int[] expectedHRFailure)
		{
			if (Failed(hr))
			{
				if ((expectedHRFailure == null) || (Array.IndexOf(expectedHRFailure, hr) < 0))
				{
					Marshal.ThrowExceptionForHR(hr);
				}
			}
			return hr;
		}

		/// <summary>
		/// Changes the parent window of the specified child window.
		/// </summary>
		/// <param name="hWnd">Handle to the child window.</param>
		/// <param name="hWndParent">Handle to the new parent window. If this parameter is NULL, the desktop window becomes the new parent window.</param>
		/// <returns>A handle to the previous parent window indicates success. NULL indicates failure.</returns>
		[DllImport("user32.dll")]
		public static extern IntPtr SetParent(IntPtr hWnd, IntPtr hWndParent);

		[DllImport("user32.dll", EntryPoint = "IsDialogMessageA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
		public static extern bool IsDialogMessageA(IntPtr hDlg, ref MSG msg);
		#endregion
	}
}
