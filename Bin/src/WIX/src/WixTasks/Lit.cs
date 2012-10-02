//-------------------------------------------------------------------------------------------------
// <copyright file="Lit.cs" company="Microsoft">
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
// Build task to execute the lib tool of the Windows Installer Xml toolset.
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
	/// An MSBuild task to run the WiX lib tool.
	/// </summary>
	public sealed class Lit : ToolTask
	{
		private const string LitToolName = "lit.exe";
		private ITaskItem[] objectFiles;
		private bool noLogo;
		private ITaskItem outputFile;
		private ITaskItem[] extensions;
		private bool suppressSchemaValidation;
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

		public bool NoLogo
		{
			get { return this.noLogo; }
			set { this.noLogo = value; }
		}

		public ITaskItem OutputFile
		{
			get { return this.outputFile; }
			set { this.outputFile = value; }
		}

		public ITaskItem[] Extensions
		{
			get { return this.extensions; }
			set { this.extensions = value; }
		}

		public bool SuppressSchemaValidation
		{
			get { return this.suppressSchemaValidation; }
			set { this.suppressSchemaValidation = value; }
		}

		public bool SuppressIntermediateFileVersionMatching
		{
			get { return this.suppressIntermediateFileVersionMatching; }
			set { this.suppressIntermediateFileVersionMatching = true; }
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
		/// <remarks>The ToolName is used with the ToolPath to get the location of lit.exe</remarks>
		/// <value>The name of the executable.</value>
		protected override string ToolName
		{
			get { return LitToolName; }
		}

		/// <summary>
		/// Get the path to the executable.
		/// </summary>
		/// <remarks>GetFullPathToTool is only called when the ToolPath property is not set (see the ToolName remarks above).</remarks>
		/// <returns>The full path to the executable or simply lit.exe if it's expected to be in the system path.</returns>
		protected override string GenerateFullPathToTool()
		{
			// If there's not a ToolPath specified, it has to be in the system path.
			if (String.IsNullOrEmpty(this.ToolPath))
			{
				return LitToolName;
			}

			return Path.Combine(Path.GetFullPath(this.ToolPath), LitToolName);
		}

		/// <summary>
		/// Generate the command line from the properties.
		/// </summary>
		/// <returns>Command line string.</returns>
		protected override string GenerateCommandLineCommands()
		{
			CommandLineBuilder commandLine = new CommandLineBuilder();

			CommandLineHelper.AppendIfTrue(commandLine, "-nologo", this.noLogo);
			commandLine.AppendSwitchIfNotNull("-out ", outputFile.ItemSpec);
			CommandLineHelper.AppendExtensions(commandLine, this.extensions, this.Log);
			CommandLineHelper.AppendIfTrue(commandLine, "-ss", this.suppressSchemaValidation);
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
