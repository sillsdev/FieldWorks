//-------------------------------------------------------------------------------------------------
// <copyright file="CommandStatus.cs" company="Microsoft">
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
// Enumeration for the different states that a menu command can be in.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using Microsoft.VisualStudio.OLE.Interop;

	/// <summary>
	/// Enumeration for the different states that a menu command can be in.
	/// </summary>
	public enum CommandStatus
	{
		Unhandled = -1,
		NotSupportedOrEnabled = 0,
		Supported = OLECMDF.OLECMDF_SUPPORTED,
		Enabled = OLECMDF.OLECMDF_ENABLED,
		SupportedAndEnabled = OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED,
		Hidden = OLECMDF.OLECMDF_INVISIBLE,
	}
}