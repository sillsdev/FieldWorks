//-------------------------------------------------------------------------------------------------
// <copyright file="WixBuildSettings.cs" company="Microsoft">
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
// Contains the WixBuildSettings class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using System.ComponentModel;
	using System.Globalization;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;

	/// <summary>
	/// Provides configuration information to the Visual Studio shell about a WiX project.
	/// </summary>
	internal sealed class WixBuildSettings : BuildSettings
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(WixBuildSettings);

		private BuildOutputType outputType;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="WixBuildSettings"/> class.
		/// </summary>
		public WixBuildSettings()
		{
		}
		#endregion

		#region Enums
		//==========================================================================================
		// Enums
		//==========================================================================================

		/// <summary>
		/// Enumerates the types of Wix outputs (projects).
		/// </summary>
		public enum BuildOutputType
		{
			/// <summary>
			/// A WiX Product project builds an MSI.
			/// </summary>
			MSI,

			/// <summary>
			/// A WiX Merge Module project builds an MSM.
			/// </summary>
			MSM,

			/// <summary>
			/// A WiX Library project builds a wixlib.
			/// </summary>
			Wixlib,
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the output extension (including the initial '.'), which depends on the type of output that is being generated.
		/// </summary>
		[Browsable(false)]
		public override string OutputExtension
		{
			get
			{
				switch (this.OutputType)
				{
					case BuildOutputType.MSI:
						return ".msi";

					case BuildOutputType.MSM:
						return ".msm";

					case BuildOutputType.Wixlib:
						return ".wixlib";

					default:
						Tracer.Fail("We're missing an output type.");
						return ".unknown";
				}
			}
		}

		/// <summary>
		/// Gets the output type for this project.
		/// </summary>
		[Browsable(true)]
		[LocalizedCategory(SconceStrings.StringId.CategoryApplication)]
		[WixLocalizedDescription(WixStrings.StringId.WixBuildSettingsOutputTypeDescription)]
		[WixLocalizedDisplayName(WixStrings.StringId.WixBuildSettingsOutputTypeDisplayName)]
		public BuildOutputType OutputType
		{
			get { return this.outputType; }
			set
			{
				Tracer.VerifyEnumArgument((int)value, "OutputType", typeof(BuildOutputType));

				if (this.outputType != value)
				{
					this.outputType = value;
					this.OnPropertyChanged(new PropertyChangedEventArgs("OutputType"));
				}
			}
		}
		#endregion
	}
}
