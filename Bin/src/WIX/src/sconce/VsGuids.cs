//-------------------------------------------------------------------------------------------------
// <copyright file="VsGuids.cs" company="Microsoft">
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
// Contains standard Visual Studio GUIDs.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;

	/// <summary>
	///     Visual Studio standard editor, window frame, and other GUIDs
	/// </summary>
	public sealed class VsGuids
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		public static readonly Guid XmlEditor = new Guid("{C76D83F8-A489-11D0-8195-00A0C91BBEE3}");
		public static readonly Guid SolutionExplorer = new Guid("3AE79031-E1BC-11D0-8F78-00A0C9110057");
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		///     Prevent direct instantiation of this static class.
		/// </summary>
		private VsGuids()
		{
		}
		#endregion
	}
}