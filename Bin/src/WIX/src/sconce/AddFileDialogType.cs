//--------------------------------------------------------------------------------------------------
// <copyright file="AddFileDialogType.cs" company="Microsoft">
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
// Enumerates the different types of Add File dialogs that can be shown.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;

	/// <summary>
	/// Enumerates the different types of Add File dialogs that can be shown.
	/// </summary>
	public enum AddFileDialogType
	{
		/// <summary>
		/// Represents the standard "Add New File" Visual Studio dialog.
		/// </summary>
		AddNew,

		/// <summary>
		/// Represents the standard "Add Existing File" Visual Studio dialog.
		/// </summary>
		AddExisting,
	}
}