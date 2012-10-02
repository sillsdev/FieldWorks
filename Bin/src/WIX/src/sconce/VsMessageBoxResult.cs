//-------------------------------------------------------------------------------------------------
// <copyright file="VsMessageBoxResult.cs" company="Microsoft">
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
// Contains Visual Studio message box result values.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;

	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Visual Studio message box result values.
	/// </summary>
	public enum VsMessageBoxResult
	{
		OK      = 1,
		Cancel  = 2,
		Abort   = 3,
		Retry   = 4,
		Ignore  = 5,
		Yes     = 6,
		No      = 7,
	}
}
