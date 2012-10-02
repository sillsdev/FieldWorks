//-------------------------------------------------------------------------------------------------
// <copyright file="VotivePPReturnValue.cs" company="Microsoft">
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
// Enumerates all of the possible return values from the VotivePP application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.Tools
{
	using System;

	/// <summary>
	/// Enumerates all of the possible return values from the VotivePP application.
	/// </summary>
	public enum VotivePPReturnValue
	{
		Success = 0,
		UnknownError,
		InvalidParameters,
		InvalidPlaceholderParam,
		SourceFileNotFound,
		FileReadError,
	}
}
