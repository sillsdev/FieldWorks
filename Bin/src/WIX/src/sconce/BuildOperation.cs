//-------------------------------------------------------------------------------------------------
// <copyright file="BuildOperation.cs" company="Microsoft">
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
// Contains the BuildOperation enum.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;

	/// <summary>
	/// Enumerates the different types of build operations that can be performed on a project.
	/// </summary>
	public enum BuildOperation
	{
		/// <summary>
		/// Performs a clean build, which removes any intermediate and project output.
		/// </summary>
		Clean,

		/// <summary>
		/// Performs an incremental build.
		/// </summary>
		Build,

		/// <summary>
		/// Performs a full rebuild, which is a clean followed by a build.
		/// </summary>
		Rebuild,
	}
}
