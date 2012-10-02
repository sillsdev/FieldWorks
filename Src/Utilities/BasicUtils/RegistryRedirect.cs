// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RegistryRedirect.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace SIL.FieldWorks.Utilities.BasicUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Allows to redirect HKCR to HKLM/Software/Classes
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class RegistryRedirect
	{
		private static readonly UIntPtr HKEY_CLASSES_ROOT = new UIntPtr(0x80000000);
		private static readonly UIntPtr HKEY_CURRENT_USER = new UIntPtr(0x80000001);
		//private static readonly UIntPtr HKEY_LOCAL_MACHINE = new UIntPtr(0x80000002);

		[DllImport("Advapi32.dll")]
		private extern static int RegOverridePredefKey(UIntPtr hKey, UIntPtr hNewKey);

		[DllImport("Advapi32.dll")]
		private extern static int RegCreateKey(UIntPtr hKey, string lpSubKey, out UIntPtr phkResult);

		[DllImport("Advapi32.dll")]
		private extern static int RegCloseKey(UIntPtr hKey);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Redirect HKCR path to the per user path HKCU/Software/Classes. This allows
		/// registering COM objects for users with limited permissions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void InitializeRegistry()
		{
			UIntPtr hKey;
			RegCreateKey(HKEY_CURRENT_USER, @"Software\Classes", out hKey);
			RegOverridePredefKey(HKEY_CLASSES_ROOT, hKey);
			RegCloseKey(hKey);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restores the normal HKCR path
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void ResetRegistry()
		{
			RegOverridePredefKey(HKEY_CLASSES_ROOT, UIntPtr.Zero);
		}
	}
}
