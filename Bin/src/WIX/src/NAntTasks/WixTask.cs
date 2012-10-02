//--------------------------------------------------------------------------------------------------
// <copyright file="WixTask.cs" company="Microsoft">
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
// Base class for all Wix-related NAnt tasks.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.NAntTasks
{
	using System;
	using System.IO;
	using System.Xml;

	using NAnt.Core;
	using NAnt.Core.Attributes;
	using NAnt.Core.Tasks;
	using NAnt.Core.Types;

	/// <summary>
	/// Abstract base class for all Wix-related NAnt tasks.
	/// </summary>
	public abstract class WixTask : ExternalProgramBase
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private DirectoryInfo exeDirectory;
		private bool forceRebuild;
		private string responseFile;
		private FileSet sources;
		private bool warningsAsErrors;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="WixTask"/> class.
		/// </summary>
		protected WixTask(string exeName) : base()
		{
			Utility.VerifyStringArgument(exeName, "exeName");
			this.ExeName = exeName;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets or sets the directory to the tool executable.
		/// </summary>
		[TaskAttribute("exedir")]
		public DirectoryInfo ExeDirectory
		{
			get { return this.exeDirectory; }
			set { this.exeDirectory = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether to rebuild the output file regardless of file
		/// time stamps.
		/// </summary>
		[BooleanValidator]
		[TaskAttribute("rebuild")]
		public bool ForceRebuild
		{
			get { return this.forceRebuild; }
			set { this.forceRebuild = value; }
		}

		/// <summary>
		/// Gets the arguments to pass to the executable.
		/// </summary>
		public override string ProgramArguments
		{
			get { return "@\"" + this.responseFile + "\""; }
		}

		/// <summary>
		/// Gets the full path to the executable.
		/// </summary>
		public override string ProgramFileName
		{
			get
			{
				// If the exedir attribute is not provided, then we'll assume that the exe is on the path.
				if (this.ExeDirectory == null || !this.ExeDirectory.Exists)
				{
					this.Log(Level.Verbose, Strings.ExeDirMissing);
					return this.ExeName;
				}

				return Path.Combine(this.ExeDirectory.FullName, this.ExeName);
			}
		}

		/// <summary>
		/// Gets or sets the source files to compile.
		/// </summary>
		[BuildElement("sources", Required = true)]
		public FileSet Sources
		{
			get { return this.sources; }
			set { this.sources = value; }
		}

		/// <summary>
		/// Gets or sets the option to treat warnings as errors.
		/// </summary>
		[TaskAttribute("warningsaserrors")]
		public bool WarningsAsErrors
		{
			get { return this.warningsAsErrors; }
			set { this.warningsAsErrors = value; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Creates the response file with all of the program arguments and then runs the process.
		/// </summary>
		protected override void ExecuteTask()
		{
			// If we don't need to rebuild, then we can quit here.
			if (!this.NeedsRebuilding())
			{
				return;
			}

			try
			{
				// Set the base directory for the source file section if it hasn't been set already.
				if (this.Sources.BaseDirectory == null)
				{
					this.Sources.BaseDirectory = new DirectoryInfo(this.Project.BaseDirectory);
				}

				// Let subclasses log build start messages.
				this.LogBuildStart();

				this.responseFile = Path.GetTempFileName();
				using (StreamWriter writer = new StreamWriter(this.responseFile))
				{
					this.WriteOptions(writer);
					this.WriteSourceFiles(writer);
				}

				// If we're showing verbose output, then show the response file.
				if (this.Verbose)
				{
					this.Log(Level.Info, Strings.ContentsOfResponseFile(this.responseFile));
					using (StreamReader reader = new StreamReader(this.responseFile))
					{
						this.Log(Level.Info, reader.ReadToEnd());
					}
				}

				// Let the base class actually run the executable.
				base.ExecuteTask();
			}
			finally
			{
				if (this.responseFile != null)
				{
					File.Delete(this.responseFile);
					this.responseFile = null;
				}
			}
		}

		/// <summary>
		/// Logs any build start messages.
		/// </summary>
		protected virtual void LogBuildStart()
		{
		}

		/// <summary>
		/// Returns a value indicating whether the output of this tool needs to be rebuilt (i.e. if
		/// the tool needs to be run).
		/// </summary>
		/// <returns>true if the output of this tool needs to be rebuilt; otherwise, false.</returns>
		protected virtual bool NeedsRebuilding()
		{
			// If the 'rebuild' attribute is set to true, then we have to rebuild.
			if (this.ForceRebuild)
			{
				this.Log(Level.Verbose, Strings.RebuildAttributeSetToTrue);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Writes all of the source files to the response file, one source file per line.
		/// </summary>
		/// <param name="writer">The output writer.</param>
		protected virtual void WriteSourceFiles(TextWriter writer)
		{
			foreach (string fileName in this.Sources.FileNames)
			{
				writer.WriteLine(Utility.QuotePathIfNeeded(fileName));
			}
		}

		/// <summary>
		/// Writes all of the command-line parameters for the tool to a response file, one parameter per line.
		/// </summary>
		/// <param name="writer">The output writer.</param>
		protected virtual void WriteOptions(TextWriter writer)
		{
			writer.WriteLine("-nologo");

			if (this.warningsAsErrors)
			{
				writer.WriteLine("-wx");
			}
		}
		#endregion
	}
}