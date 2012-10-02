//-------------------------------------------------------------------------------------------------
// <copyright file="Light.cs" company="Microsoft">
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
// Build task to execute the linker of the Windows Installer Xml toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
	using System;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Text;

	using Microsoft.Build.Framework;
	using Microsoft.Build.Utilities;

	/// <summary>
	/// An MSBuild task to run the WiX linker.
	/// </summary>
	public sealed class Light : ToolTask
	{
		private const string LightToolName = "Light.exe";
		private ITaskItem[] objectFiles;
		private string baseInputPath;
		private string cabinetCache;
		private ITaskItem[] extensions;
		private string baseUncompressedImagesOutputPath;
		private ITaskItem[] localizationFiles;
		private bool noLogo;
		private bool leaveTemporaryFiles;
		private bool reuseCabinetCache;
		private ITaskItem outputFile;
		private bool outputAsXml;
		private bool suppressDefaultAdminSequenceActions;
		private bool suppressDefaultAdvSequenceActions;
		private bool suppressAssemblies;
		private bool suppressFiles;
		private bool suppressLayout;
		private bool suppressSchemaValidation;
		private bool suppressDefaultUISequenceActions;
		private bool suppressIntermediateFileVersionMatching;
		private bool suppressAllWarnings;
		private bool useSmallTableDefinitions;
		private bool treatWarningsAsErrors;
		private int warningLevel = CommandLineHelper.Unspecified;
		private bool verboseOutput;
		private int verboseOutputLevel = CommandLineHelper.Unspecified;

		[Required]
		public ITaskItem[] ObjectFiles
		{
			get { return this.objectFiles; }
			set { this.objectFiles = value; }
		}

		public string BaseInputPath
		{
			get { return this.baseInputPath; }
			set { this.baseInputPath = value; }
		}

		public string CabinetCache
		{
			get { return this.CabinetCache; }
			set { this.cabinetCache = value; }
		}

		public ITaskItem[] Extensions
		{
			get { return this.extensions; }
			set { this.extensions = value; }
		}

		public string BaseUncompressedImagesOutputPath
		{
			get { return this.baseUncompressedImagesOutputPath; }
			set { this.baseUncompressedImagesOutputPath = value; }
		}

		public ITaskItem[] LocalizationFiles
		{
			get { return this.localizationFiles; }
			set { this.localizationFiles = value; }
		}

		public bool NoLogo
		{
			get { return this.noLogo; }
			set { this.noLogo = value; }
		}

		public bool LeaveTemporaryFiles
		{
			get { return this.leaveTemporaryFiles; }
			set { this.leaveTemporaryFiles = value; }
		}

		public bool ReuseCabinetCache
		{
			get { return this.reuseCabinetCache; }
			set { this.reuseCabinetCache = value; }
		}

		public ITaskItem OutputFile
		{
			get { return this.outputFile; }
			set { this.outputFile = value; }
		}

		public bool OutputAsXml
		{
			get { return this.outputAsXml; }
			set { this.outputAsXml = value; }
		}

		public bool SuppressDefaultAdminSequenceActions
		{
			get { return this.suppressDefaultAdminSequenceActions; }
			set { this.suppressDefaultAdminSequenceActions = value; }
		}

		public bool SuppressDefaultAdvSequenceActions
		{
			get { return this.suppressDefaultAdvSequenceActions; }
			set { this.suppressDefaultAdvSequenceActions = value; }
		}

		public bool SuppressAssemblies
		{
			get { return this.suppressAssemblies; }
			set { this.suppressAssemblies = value; }
		}

		public bool SuppressFiles
		{
			get { return this.suppressFiles; }
			set { this.suppressFiles = value; }
		}

		public bool SuppressLayout
		{
			get { return this.suppressLayout; }
			set { this.suppressLayout = value; }
		}

		public bool SuppressSchemaValidation
		{
			get { return this.suppressSchemaValidation; }
			set { this.suppressSchemaValidation = value; }
		}

		public bool SuppressDefaultUISequenceActions
		{
			get { return this.suppressDefaultUISequenceActions; }
			set { this.suppressDefaultUISequenceActions = value; }
		}

		public bool SuppressIntermediateFileVersionMatching
		{
			get { return this.suppressIntermediateFileVersionMatching; }
			set { this.suppressIntermediateFileVersionMatching = value; }
		}

		public bool SuppressAllWarnings
		{
			get { return this.suppressAllWarnings; }
			set { this.suppressAllWarnings = value; }
		}

		public bool UseSmallTableDefinitions
		{
			get { return this.useSmallTableDefinitions; }
			set { this.useSmallTableDefinitions = value; }
		}

		public bool TreatWarningsAsErrors
		{
			get { return this.treatWarningsAsErrors; }
			set { this.treatWarningsAsErrors = value; }
		}

		public int WarningLevel
		{
			get { return this.warningLevel; }
			set { this.warningLevel = value; }
		}

		public bool VerboseOutput
		{
			get { return this.verboseOutput; }
			set { this.verboseOutput = value; }
		}

		public int VerboseOutputLevel
		{
			get { return this.verboseOutputLevel; }
			set { this.verboseOutputLevel = value; }
		}

		/// <summary>
		/// Get the name of the executable.
		/// </summary>
		/// <remarks>The ToolName is used with the ToolPath to get the location of light.exe.</remarks>
		/// <value>The name of the executable.</value>
		protected override string ToolName
		{
			get { return LightToolName; }
		}

		/// <summary>
		/// Get the path to the executable.
		/// </summary>
		/// <remarks>GetFullPathToTool is only called when the ToolPath property is not set (see the ToolName remarks above).</remarks>
		/// <returns>The full path to the executable or simply light.exe if it's expected to be in the system path.</returns>
		protected override string GenerateFullPathToTool()
		{
			// If there's not a ToolPath specified, it has to be in the system path.
			if (String.IsNullOrEmpty(this.ToolPath))
			{
				return LightToolName;
			}

			return Path.Combine(Path.GetFullPath(this.ToolPath), LightToolName);
		}

		/// <summary>
		/// Generate the command line from the properties.
		/// </summary>
		/// <returns>Command line string.</returns>
		protected override string GenerateCommandLineCommands()
		{
			CommandLineBuilder commandLine = new CommandLineBuilder();

			commandLine.AppendSwitchIfNotNull("-b ", this.baseInputPath);
			commandLine.AppendSwitchIfNotNull("-cc ", this.cabinetCache);
			CommandLineHelper.AppendExtensions(commandLine, this.extensions, this.Log);
			commandLine.AppendSwitchIfNotNull("-i ", this.baseUncompressedImagesOutputPath);
			CommandLineHelper.AppendArrayIfNotNull(commandLine, "-loc ", this.LocalizationFiles);
			CommandLineHelper.AppendIfTrue(commandLine, "-nologo", this.noLogo);
			CommandLineHelper.AppendIfTrue(commandLine, "-notidy", this.leaveTemporaryFiles);
			CommandLineHelper.AppendIfTrue(commandLine, "-reusecab", this.reuseCabinetCache);
			commandLine.AppendSwitchIfNotNull("-out ", outputFile.ItemSpec);
			CommandLineHelper.AppendIfTrue(commandLine, "-xo", this.outputAsXml);
			CommandLineHelper.AppendIfTrue(commandLine, "-sadmin", this.suppressDefaultAdminSequenceActions);
			CommandLineHelper.AppendIfTrue(commandLine, "-sadv", this.suppressDefaultAdvSequenceActions);
			CommandLineHelper.AppendIfTrue(commandLine, "-sa", this.suppressAssemblies);
			CommandLineHelper.AppendIfTrue(commandLine, "-sf", this.suppressFiles);
			CommandLineHelper.AppendIfTrue(commandLine, "-sl", this.suppressLayout);
			CommandLineHelper.AppendIfTrue(commandLine, "-ss", this.suppressSchemaValidation);
			CommandLineHelper.AppendIfTrue(commandLine, "-sui", this.suppressDefaultUISequenceActions);
			CommandLineHelper.AppendIfTrue(commandLine, "-sv", this.suppressIntermediateFileVersionMatching);
			CommandLineHelper.AppendIfTrue(commandLine, "-sw", this.suppressAllWarnings);
			CommandLineHelper.AppendIfTrue(commandLine, "-ust", this.useSmallTableDefinitions);
			CommandLineHelper.AppendIfTrue(commandLine, "-wx", this.treatWarningsAsErrors);
			CommandLineHelper.AppendIfSpecified(commandLine, "-w", this.warningLevel);
			CommandLineHelper.AppendIfTrue(commandLine, "-v", this.verboseOutput);
			CommandLineHelper.AppendIfSpecified(commandLine, "-v", this.verboseOutputLevel);
			commandLine.AppendFileNamesIfNotNull(this.objectFiles, " ");

			return commandLine.ToString();
		}
	}
}
