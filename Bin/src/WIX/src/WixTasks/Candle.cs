//-------------------------------------------------------------------------------------------------
// <copyright file="CandleTask.cs" company="Microsoft">
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
// Build task to execute the compiler of the Windows Installer Xml toolset.
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
	/// An MSBuild task to run the WiX compiler.
	/// </summary>
	public sealed class Candle : ToolTask
	{
		private const string CandleToolName = "candle.exe";
		private ITaskItem[] sourceFiles;
		private ITaskItem outputFile;
		private string[] defineConstants;
		private string[] includeSearchPaths;
		private ITaskItem[] extensions;
		private bool noLogo;
		private bool preprocessToStdOut;
		private string preprocessToFile;
		private int warningLevel = CommandLineHelper.Unspecified;
		private bool treatWarningsAsErrors;
		private bool suppressSchemaValidation;
		private bool useSmallTableDefinitions;
		private bool showSourceTrace;
		private bool suppressAllWarnings;
		private bool verboseOutput;
		private int verboseOutputLevel = CommandLineHelper.Unspecified;

		[Required]
		public ITaskItem[] SourceFiles
		{
			get { return this.sourceFiles; }
			set { this.sourceFiles = value; }
		}

		public ITaskItem OutputFile
		{
			get { return this.outputFile; }
			set { this.outputFile = value; }
		}

		public string[] DefineConstants
		{
			get { return this.defineConstants; }
			set { this.defineConstants = value; }
		}

		public string[] IncludeSearchPaths
		{
			get { return this.includeSearchPaths; }
			set { this.includeSearchPaths = value; }
		}

		public ITaskItem[] Extensions
		{
			get { return this.extensions; }
			set { this.extensions = value; }
		}

		public bool NoLogo
		{
			get { return this.noLogo; }
			set { this.noLogo = value; }
		}

		public bool PreprocessToStdOut
		{
			get { return this.preprocessToStdOut; }
			set { this.preprocessToStdOut = value; }
		}

		public string PreprocessToFile
		{
			get { return this.preprocessToFile; }
			set { this.preprocessToFile = value; }
		}

		public bool SuppressSchemaValidation
		{
			get { return this.suppressSchemaValidation; }
			set { this.suppressSchemaValidation = value; }
		}

		public bool UseSmallTableDefinitions
		{
			get { return this.useSmallTableDefinitions; }
			set { this.useSmallTableDefinitions = value; }
		}

		public bool ShowSourceTrace
		{
			get { return this.showSourceTrace; }
			set { this.showSourceTrace = value; }
		}

		public bool SuppressAllWarnings
		{
			get { return this.suppressAllWarnings; }
			set { this.suppressAllWarnings = value; }
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
		/// <remarks>The ToolName is used with the ToolPath to get the location of candle.exe.</remarks>
		/// <value>The name of the executable.</value>
		protected override string ToolName
		{
			get { return CandleToolName; }
		}

		/// <summary>
		/// Get the path to the executable.
		/// </summary>
		/// <remarks>GetFullPathToTool is only called when the ToolPath property is not set (see the ToolName remarks above).</remarks>
		/// <returns>The full path to the executable or simply candle.exe if it's expected to be in the system path.</returns>
		protected override string GenerateFullPathToTool()
		{
			// If there's not a ToolPath specified, it has to be in the system path.
			if (String.IsNullOrEmpty(this.ToolPath))
			{
				return CandleToolName;
			}

			return Path.Combine(Path.GetFullPath(this.ToolPath), CandleToolName);
		}

		/// <summary>
		/// Generate the command line from the properties.
		/// </summary>
		/// <returns>Command line string.</returns>
		protected override string GenerateCommandLineCommands()
		{
			CommandLineBuilder commandLine = new CommandLineBuilder();

			CommandLineHelper.AppendArrayIfNotNull(commandLine, "-d", this.defineConstants);
			CommandLineHelper.AppendIfTrue(commandLine, "-p", this.preprocessToStdOut);
			commandLine.AppendSwitchIfNotNull("-p", this.preprocessToFile);
			CommandLineHelper.AppendArrayIfNotNull(commandLine, "-I", this.includeSearchPaths);
			CommandLineHelper.AppendIfTrue(commandLine, "-nologo", this.noLogo);
			commandLine.AppendSwitchIfNotNull("-out ", outputFile.ItemSpec);
			CommandLineHelper.AppendIfTrue(commandLine, "-ss", this.suppressSchemaValidation);
			CommandLineHelper.AppendIfTrue(commandLine, "-ust", this.useSmallTableDefinitions);
			CommandLineHelper.AppendIfTrue(commandLine, "-trace", this.showSourceTrace);
			CommandLineHelper.AppendExtensions(commandLine, this.extensions, this.Log);
			CommandLineHelper.AppendIfTrue(commandLine, "-sw", this.suppressAllWarnings);
			CommandLineHelper.AppendIfTrue(commandLine, "-wx", this.treatWarningsAsErrors);
			CommandLineHelper.AppendIfSpecified(commandLine, "-w", this.warningLevel);
			CommandLineHelper.AppendIfTrue(commandLine, "-v", this.verboseOutput);
			CommandLineHelper.AppendIfSpecified(commandLine, "-v", this.verboseOutputLevel);
			commandLine.AppendFileNamesIfNotNull(this.sourceFiles, " ");

			return commandLine.ToString();
		}
	}

	/// <summary>
	/// Helper class for appending the command line arguments.
	/// </summary>
	internal static class CommandLineHelper
	{
		internal const int Unspecified = -1;

		/// <summary>
		/// Append a switch to the command line if the value has been specified.
		/// </summary>
		/// <param name="commandLine">Command line builder.</param>
		/// <param name="switchName">Switch to append.</param>
		/// <param name="value">Value specified by the user.</param>
		internal static void AppendIfSpecified(CommandLineBuilder commandLine, string switchName, int value)
		{
			if (value != Unspecified)
			{
				commandLine.AppendSwitchIfNotNull(switchName, value.ToString(CultureInfo.InvariantCulture));
			}
		}

		/// <summary>
		/// Append a switch to the command line if the condition is true.
		/// </summary>
		/// <param name="commandLine">Command line builder.</param>
		/// <param name="switchName">Switch to append.</param>
		/// <param name="condition">Condition specified by the user.</param>
		internal static void AppendIfTrue(CommandLineBuilder commandLine, string switchName, bool condition)
		{
			if (condition)
			{
				commandLine.AppendSwitch(switchName);
			}
		}

		/// <summary>
		/// Append a switch to the command line if any values in the array have been specified.
		/// </summary>
		/// <param name="commandLine">Command line builder.</param>
		/// <param name="switchName">Switch to append.</param>
		/// <param name="values">Values specified by the user.</param>
		internal static void AppendArrayIfNotNull(CommandLineBuilder commandLine, string switchName, ITaskItem[] values)
		{
			if (values != null)
			{
				foreach (ITaskItem value in values)
				{
					commandLine.AppendSwitchIfNotNull(switchName, value);
				}
			}
		}

		/// <summary>
		/// Append a switch to the command line if any values in the array have been specified.
		/// </summary>
		/// <param name="commandLine">Command line builder.</param>
		/// <param name="switchName">Switch to append.</param>
		/// <param name="values">Values specified by the user.</param>
		internal static void AppendArrayIfNotNull(CommandLineBuilder commandLine, string switchName, string[] values)
		{
			if (values != null)
			{
				for (int i = 0; i < values.Length; i++)
				{
					commandLine.AppendSwitchIfNotNull(switchName, values[i]);
				}
			}
		}

		/// <summary>
		/// Append each of the define constants to the command line.
		/// </summary>
		/// <param name="commandLine">Command line builder.</param>
		internal static void AppendExtensions(CommandLineBuilder commandLine, ITaskItem[] extensions, TaskLoggingHelper log)
		{
			if (extensions == null)
			{
				// No items
				return;
			}

			for (int i = 0; i < extensions.Length; i++)
			{
				string className = extensions[i].GetMetadata("Class");
				if (String.IsNullOrEmpty(className))
				{
					log.LogError(String.Format("Missing the required property 'Class' for the extension {0}", extensions[i].ItemSpec));
				}

				commandLine.AppendSwitchUnquotedIfNotNull("-ext ", String.Concat(className, ",", extensions[i].ItemSpec));
			}
		}
	}
}
