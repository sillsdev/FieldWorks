//-------------------------------------------------------------------------------------------------
// <copyright file="BuildSettings.cs" company="Microsoft">
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
// Contains the BuildSettings class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.ComponentModel;

	/// <summary>
	/// Provides configuration information to the Visual Studio shell about a project.
	/// </summary>
	[DefaultProperty("OutputName")]
	public class BuildSettings : PropertyPageSettings
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(BuildSettings);

		private string outputName;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="BuildSettings"/> class.
		/// </summary>
		public BuildSettings()
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the base part of the file name (without an extension).
		/// </summary>
		[Browsable(true)]
		[LocalizedCategory(SconceStrings.StringId.CategoryApplication)]
		[LocalizedDescription(SconceStrings.StringId.BuildSettingsOutputNameDescription)]
		[LocalizedDisplayName(SconceStrings.StringId.BuildSettingsOutputNameDisplayName)]
		public string OutputName
		{
			get { return this.outputName; }
			set
			{
				Tracer.VerifyStringArgument(value, "OutputName");
				if (this.outputName != value)
				{
					this.outputName = value;
					this.OnPropertyChanged(new PropertyChangedEventArgs("OutputName"));
				}
			}
		}

		/// <summary>
		/// Gets the output extension (including the initial '.'), which depends on the type of output that is being generated.
		/// </summary>
		[Browsable(false)]
		public virtual string OutputExtension
		{
			get { return ".exe"; }
		}

		/// <summary>
		/// Gets the full name plus extension of the output file.
		/// </summary>
		[Browsable(true)]
		[LocalizedCategory(SconceStrings.StringId.CategoryProject)]
		[LocalizedDescription(SconceStrings.StringId.BuildSettingsOutputFileDescription)]
		[LocalizedDisplayName(SconceStrings.StringId.BuildSettingsOutputFileDisplayName)]
		public string OutputFileName
		{
			get
			{
				string fileName = this.OutputName;
				string extension = this.OutputExtension;
				if (!String.IsNullOrEmpty(extension))
				{
					if (extension[0] != '.')
					{
						fileName += "." + extension;
					}
					else
					{
						fileName += extension;
					}
				}
				return fileName;
			}
		}
		#endregion
	}
}
