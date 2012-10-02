//-------------------------------------------------------------------------------------------------
// <copyright file="VsMenus.cs" company="Microsoft">
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
// Contains Visual Studio menu GUIDs and constants.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;

	/// <summary>
	///     Contains Visual Studio menu GUIDs and constants.
	/// </summary>
	public sealed class VsMenus
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		// Menu command GUIDs
		public static Guid StandardCommandSet97 = new Guid("5efc7975-14bc-11cf-9b2b-00aa00573819");
		public static Guid StandardCommandSet2K = new Guid("1496A755-94DE-11D0-8C3F-00C04FC2AAE2");
		public static Guid VsVbaPkg = new Guid(0xa659f1b3, 0xad34, 0x11d1, 0xab, 0xad, 0x0, 0x80, 0xc7, 0xb8, 0x9c, 0x95);
		public static Guid SHLMainMenu = new Guid(0xd309f791, 0x903f, 0x11d0, 0x9e, 0xfc, 0x00, 0xa0, 0xc9, 0x11, 0x00, 0x4f);
		public static Guid VSUISet = new Guid("60481700-078b-11d1-aaf8-00a0c9055a90");
		public static Guid CciSet = new Guid("2805D6BD-47A8-4944-8002-4e29b9ac2269");
		public static Guid VsUIHierarchyWindowCmds = new Guid("60481700-078B-11D1-AAF8-00A0C9055A90");

		// Special Menus.
		public const int IDM_VS_CTXT_CODEWIN = 0x040D;
		public const int IDM_VS_CTXT_ITEMNODE = 0x0430;
		public const int IDM_VS_CTXT_PROJNODE = 0x0402;
		public const int IDM_VS_CTXT_REFERENCEROOT = 0x0450;
		public const int IDM_VS_CTXT_REFERENCE = 0x0451;
		public const int IDM_VS_CTXT_FOLDERNODE = 0x0431;
		public const int IDM_VS_CTXT_NOCOMMANDS = 0x041A;
		public const int VSCmdOptQueryParameterList = 1;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		///     Prevent direct instantiation of this class.
		/// </summary>
		private VsMenus()
		{
		}
		#endregion

		#region Enums
		//==========================================================================================
		// Enums
		//==========================================================================================

		/// <summary>
		///     The following commands are special commands that only apply to the UIHierarchyWindow.
		///     They are defined as part of the command group GUID: GUID_VsUIHierarchyWindowCmds.
		/// </summary>
		public enum VsUIHierarchyWindowCmdId
		{
			RightClick        = 1,
			DoubleClick       = 2,
			EnterKey          = 3,
			StartLabelEdit    = 4,
			CommitLabelEdit   = 5,
			CancelLabelEdit   = 6
		}

		/// <summary>
		///     Encapsulates the IDM_VS_CTXT_ context menu ids.
		/// </summary>
		public enum ContextMenuId
		{
			IDM_VS_CTXT_CODEWIN = 0x040D,
			IDM_VS_CTXT_ITEMNODE = 0x0430,
			IDM_VS_CTXT_PROJNODE = 0x0402,
			IDM_VS_CTXT_REFERENCEROOT = 0x0450,
			IDM_VS_CTXT_REFERENCE = 0x0451,
			IDM_VS_CTXT_FOLDERNODE = 0x0431,
			IDM_VS_CTXT_NOCOMMANDS = 0x041A,
		}
		#endregion
	}
}
