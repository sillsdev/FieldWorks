//-------------------------------------------------------------------------------------------------
// <copyright file="VsOpenFileDialogFlags.cs" company="Microsoft">
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
// Contains Visual Studio open file dialog flags.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;

	/// <summary>
	/// Visual Studio message box result values.
	/// </summary>
	[Flags]
	public enum VsOpenFileDialogFlags
	{
		/// <summary>OFN_READONLY</summary>
		ReadOnly = 0x00000001,

		/// <summary>OFN_OVERWRITEPROMPT</summary>
		OverwritePrompt = 0x00000002,

		/// <summary>OFN_HIDEREADONLY</summary>
		HideReadOnly = 0x00000004,

		/// <summary>OFN_NOCHANGEDIR</summary>
		NoChangeDir = 0x00000008,

		/// <summary>OFN_SHOWHELP</summary>
		ShowHelp = 0x00000010,

		/// <summary>OFN_ENABLEHOOK</summary>
		EnableHook = 0x00000020,

		/// <summary>OFN_ENABLETEMPLATE</summary>
		EnableTemplate = 0x00000040,

		/// <summary>OFN_ENABLETEMPLATEHANDLE</summary>
		EnableTemplateHandle = 0x00000080,

		/// <summary>OFN_NOVALIDATE</summary>
		NoValidate = 0x00000100,

		/// <summary>OFN_ALLOWMULTISELECT</summary>
		AllowMultiSelect = 0x00000200,

		/// <summary>OFN_EXTENSIONDIFFERENT</summary>
		ExtensionDifferent = 0x00000400,

		/// <summary>OFN_PATHMUSTEXIST</summary>
		PathMustExist = 0x00000800,

		/// <summary>OFN_FILEMUSTEXIST</summary>
		FileMustExist = 0x00001000,

		/// <summary>OFN_CREATEPROMPT</summary>
		CreatePrompt = 0x00002000,

		/// <summary>OFN_SHAREAWARE</summary>
		ShareAware = 0x00004000,

		/// <summary>OFN_NOREADONLYRETURN</summary>
		NoReadOnly = 0x00008000,

		/// <summary>OFN_NOTESTFILECREATE</summary>
		NoTestFileCreate = 0x00010000,

		/// <summary>OFN_NONETWORKBUTTON</summary>
		NoNetworkButton = 0x00020000,

		/// <summary>OFN_NOLONGNAMES</summary>
		NoLongNames = 0x00040000,

		/// <summary></summary>
		Explorer = 0x00080000,

		/// <summary></summary>
		NoDereferenceLinks = 0x00100000,

		/// <summary></summary>
		LongNames = 0x00200000,

		/// <summary></summary>
		EnableIncludeNotify = 0x00400000,

		/// <summary></summary>
		EnableSizing = 0x00800000,

		/// <summary></summary>
		DontAddToRecent = 0x02000000,

		/// <summary></summary>
		ForceShowHidden = 0x10000000,
	}
}
