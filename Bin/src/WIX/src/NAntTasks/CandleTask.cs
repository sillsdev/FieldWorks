//--------------------------------------------------------------------------------------------------
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
// NAnt task for the candle compiler.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.NAntTasks
{
	using System;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using System.IO;
	using System.Globalization;

	using NAnt.Core;
	using NAnt.Core.Attributes;
	using NAnt.Core.Types;
	using NAnt.Core.Util;

	/// <summary>
	/// Represents the NAnt task for the &lt;candle&gt; element in a NAnt script.
	/// </summary>
	[TaskName("candle")]
	public class CandleTask : WixTask
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private OptionCollection defines = new OptionCollection();
		private string suppressWarnings = string.Empty;
		private FileSet includeDirs = new FileSet();
		private string outputPath;
		private StringCollection outOfDateSources = new StringCollection();
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleTask"/> class.
		/// </summary>
		public CandleTask() : base("candle.exe")
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the preprocessor variable definitions.
		/// </summary>
		[BuildElementCollection("defines", "define")]
		public OptionCollection Defines
		{
			get { return this.defines; }
		}

		/// <summary>
		/// Sets the suppress warning command-line option.
		/// </summary>
		[TaskAttribute("suppresswarning")]
		public string SuppressWarnings
		{
			get { return this.suppressWarnings; }
			set { this.suppressWarnings = value; }
		}

		/// <summary>
		/// Gets or sets the directories to include in the search path.
		/// </summary>
		[BuildElement("includedirs")]
		public FileSet IncludeDirs
		{
			get { return this.includeDirs; }
			set { this.includeDirs = value; }
		}

		/// <summary>
		/// Gets or sets the output file or directory (-out).
		/// </summary>
		[StringValidator(AllowEmpty = false)]
		[TaskAttribute("out", Required = true)]
		public string OutputPath
		{
			get { return this.outputPath; }
			set { this.outputPath = value; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Logs any build start messages.
		/// </summary>
		protected override void LogBuildStart()
		{
			string startMessage = Strings.BuildingFiles(this.outOfDateSources.Count, this.OutputPath);
			this.Log(Level.Info, startMessage);
		}

		/// <summary>
		/// Returns a value indicating whether the output of this tool needs to be rebuilt (i.e. if
		/// the tool needs to be run).
		/// </summary>
		/// <returns>true if the output of this tool needs to be rebuilt; otherwise, false.</returns>
		protected override bool NeedsRebuilding()
		{
			// Check the base class.
			if (base.NeedsRebuilding())
			{
				return true;
			}

			// We need to rebuild if we have any source files that are newer than the targets.
			this.CalculateOutOfDateSources();
			return (this.outOfDateSources.Count > 0);
		}

		/// <summary>
		/// Writes all of the command-line parameters for the tool to a response file, one parameter per line.
		/// </summary>
		/// <param name="writer">The output writer.</param>
		protected override void WriteOptions(TextWriter writer)
		{
			base.WriteOptions(writer);

			// Write the -out parameter
			writer.WriteLine("-out " + Utility.QuotePathIfNeeded(this.OutputPath));

			// Write out the include directories
			foreach (string directoryName in this.IncludeDirs.DirectoryNames)
			{
				writer.WriteLine("-I\"" + directoryName + "\"");
			}

			// Write out suppress warnings:
			foreach (string warning in suppressWarnings.Split(','))
			{
				writer.WriteLine(" -sw" + warning);
			}

			// Write out the definitions
			foreach (Option define in this.Defines)
			{
				string commandLine = String.Format(
					CultureInfo.InvariantCulture,
					"-d{0}=\"{1}\"",
					define.OptionName,
					define.Value);
				writer.WriteLine(commandLine);
			}
		}

		/// <summary>
		/// Writes all of the source files to the response file, one source file per line.
		/// </summary>
		/// <param name="writer">The output writer.</param>
		protected override void WriteSourceFiles(TextWriter writer)
		{
			foreach (string fileName in this.outOfDateSources)
			{
				writer.WriteLine(Utility.QuotePathIfNeeded(fileName));
			}
		}

		/// <summary>
		/// Calculates which source files need to be rebuilt.
		/// </summary>
		private void CalculateOutOfDateSources()
		{
			this.outOfDateSources.Clear();

			// If the output path is a directory, then we need to check all of wixobj files to
			// see if any of them are older than their wxs counterparts.
			Debug.Assert(!StringUtils.IsNullOrEmpty(this.OutputPath));
			if (StringUtils.EndsWith(this.OutputPath, Path.DirectorySeparatorChar))
			{
				foreach (string wxsFile in this.Sources.FileNames)
				{
					Debug.Assert(File.Exists(wxsFile), "The FileSet should have filtered out non-existing files.");
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(wxsFile);
					string wixobjFile = Path.Combine(this.OutputPath, fileNameWithoutExtension + ".wixobj");

					// See if the wixobj file exists or if it's out of date.
					if (!File.Exists(wixobjFile))
					{
						this.Log(Level.Verbose, Strings.OutputFileDoesNotExist(wixobjFile));
						this.outOfDateSources.Add(wxsFile);
					}
					else if (File.GetLastWriteTime(wxsFile) > File.GetLastWriteTime(wixobjFile))
					{
						this.Log(Level.Verbose, Strings.FileHasBeenUpdated(wxsFile));
						this.outOfDateSources.Add(wxsFile);
					}
				}
			}
			// If the output is just a file then it's a little easier to check if we're out of date.
			else
			{
				bool rebuild = false;

				// If the output file doesn't exist or if there is a source file that is newer than the target, then we're out of date.
				if (!File.Exists(this.OutputPath))
				{
					this.Log(Level.Verbose, Strings.OutputFileDoesNotExist(this.OutputPath));
					rebuild = true;
				}
				else
				{
					string changedFileName = FileSet.FindMoreRecentLastWriteTime(this.Sources.FileNames, File.GetLastWriteTime(this.OutputPath));
					if (changedFileName != null)
					{
						this.Log(Level.Verbose, Strings.FileHasBeenUpdated(changedFileName));
						rebuild = true;
					}
				}

				if (rebuild)
				{
					// Add all of the sources to the out of date collection.
					string[] sourcesArray = new string[this.Sources.FileNames.Count];
					this.Sources.FileNames.CopyTo(sourcesArray, 0);
					this.outOfDateSources.AddRange(sourcesArray);
				}
			}
		}
		#endregion
	}
}