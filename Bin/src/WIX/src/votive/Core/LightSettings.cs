//-------------------------------------------------------------------------------------------------
// <copyright file="LightSettings.cs" company="Microsoft">
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
// Contains the LightSettings class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.IO;
	using System.Text;
	using Microsoft.Tools.WindowsInstallerXml;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;
	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Options for the WiX light linker.
	/// </summary>
	internal sealed class LightSettings : DirtyableObject, ICloneable
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(LightSettings);

		private string absoluteOutputFilePath;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="LightSettings"/> class.
		/// </summary>
		public LightSettings()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LightSettings"/> class.
		/// </summary>
		/// <param name="absoluteOutputFilePath">The absolute path to the output file.</param>
		public LightSettings(string absoluteOutputFilePath)
		{
			Tracer.VerifyStringArgument(absoluteOutputFilePath, "absoluteOutputFilePath");
			this.absoluteOutputFilePath = PackageUtility.CanonicalizeFilePath(absoluteOutputFilePath);
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets or sets the absolute path to the output file.
		/// </summary>
		public string AbsoluteOutputFilePath
		{
			get { return this.absoluteOutputFilePath; }
			set
			{
				if (this.AbsoluteOutputFilePath != value)
				{
					this.absoluteOutputFilePath = PackageUtility.CanonicalizeFilePath(value);
					this.MakeDirty();
				}
			}
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Returns true if the file is a .wxl localization file.
		/// </summary>
		/// <param name="path">The absolute file path to test.</param>
		/// <returns>true if the file is a .wxl localization file; otherwise false.</returns>
		public static bool IsLocalizationFile(string path)
		{
			string extension = PackageUtility.StripLeadingChar(Path.GetExtension(path), '.');
			return PackageUtility.FileStringEquals(extension, "wxl");
		}

		/// <summary>
		/// Returns a deep copy of the current object.
		/// </summary>
		/// <returns>A deep copy of the current object.</returns>
		public object Clone()
		{
			return this.MemberwiseClone();
		}

		/// <summary>
		/// Constructs the command line parameters to pass to light.exe based on the current options.
		/// </summary>
		/// <param name="rootDirectory">The absolute root directory of the project.</param>
		/// <param name="objectFilePaths">An array of .wixobj files to link.</param>
		/// <param name="localizationFilePaths">An array of .wxl files to pass into light.</param>
		/// <param name="wixlibReferences">An array of .wixlib files to link with.</param>
		/// <returns>A string that can be passed to light.exe.</returns>
		public string ConstructCommandLineParameters(string rootDirectory, string[] objectFilePaths, string[] localizationFilePaths, string[] wixlibReferences)
		{
			StringBuilder commandLine = new StringBuilder(256);
			string relativeOutputFilePath = PackageUtility.MakeRelative(rootDirectory, this.AbsoluteOutputFilePath);

			// -out parameter.
			commandLine.AppendFormat("-out {0}", PackageUtility.QuoteString(relativeOutputFilePath));

			// -loc parameters
			foreach (string locFile in localizationFilePaths)
			{
				string relativeLocFile = PackageUtility.MakeRelative(rootDirectory, locFile);
				commandLine.Append(" -loc ");
				commandLine.Append(PackageUtility.QuoteString(relativeLocFile));
			}

			// Object file list (from the compiled source files).
			foreach (string objectFile in objectFilePaths)
			{
				string relativeObjectFile = PackageUtility.MakeRelative(rootDirectory, objectFile);
				commandLine.Append(" ");
				commandLine.Append(PackageUtility.QuoteString(relativeObjectFile));
			}

			// Add any wixlib references to the list of object files.
			foreach (string wixlibReference in wixlibReferences)
			{
				commandLine.Append(" ");
				commandLine.Append(PackageUtility.QuoteString(wixlibReference));
			}

			return commandLine.ToString();
		}
		#endregion
	}
}
