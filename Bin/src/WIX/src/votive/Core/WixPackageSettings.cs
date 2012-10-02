//-------------------------------------------------------------------------------------------------
// <copyright file="WixPackageSettings.cs" company="Microsoft">
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
// Contains all of the various registry settings for the WiX package.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using System.Globalization;
	using Microsoft.VisualStudio.Shell.Interop;
	using Microsoft.Win32;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;

	/// <summary>
	/// Helper class for setting and retrieving registry settings for the package. All machine
	/// settings are cached on first use, so only one registry read is performed.
	/// </summary>
	internal sealed class WixPackageSettings : PackageSettings
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(WixPackageSettings);
		private const string MachineSettingsRegKey = @"InstalledProducts\WiX";

		// Machine settings
		private MachineSettingString toolsDirectory;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="WixPackageSettings"/> class.
		/// </summary>
		/// <param name="serviceProvider">The <see cref="ServiceProvider"/> to use.</param>
		public WixPackageSettings(ServiceProvider serviceProvider)
			: base(serviceProvider, MachineSettingsRegKey)
		{
			// Initialize all of the machine settings.
			this.toolsDirectory = new MachineSettingString(this.MachineRootPath, KeyNames.ToolsDirectory, System.String.Empty);
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the path to the directory where the WiX tools reside.
		/// </summary>
		public string ToolsDirectory
		{
			get { return this.toolsDirectory.Value; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		#endregion

		#region Classes
		//==========================================================================================
		// Classes
		//==========================================================================================

		/// <summary>
		/// Names of the various registry keys that store our settings.
		/// </summary>
		private sealed class KeyNames
		{
			public static readonly string ToolsDirectory = "ToolsDirectory";
		}
		#endregion
	}
}
