//--------------------------------------------------------------------------------------------------
// <copyright file="SingleOutputWixTask.cs" company="Microsoft">
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
// Base class for all Wix-related NAnt tasks that have a single output file (lit, light).
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
	/// Abstract base class for all Wix-related NAnt tasks that have a single output file (lit, light).
	/// </summary>
	public abstract class SingleOutputWixTask : WixTask
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private FileInfo outputFile;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="SingleOutputWixTask"/> class.
		/// </summary>
		protected SingleOutputWixTask(string exeName) : base(exeName)
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets or sets the output file (-out).
		/// </summary>
		[TaskAttribute("out", Required = true)]
		public FileInfo OutputFile
		{
			get { return this.outputFile; }
			set { this.outputFile = value; }
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
			string startMessage = Strings.BuildingFiles(this.Sources.FileNames.Count, this.OutputFile.FullName);
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

			// If the output file does not exist, rebuild.
			if (!this.OutputFile.Exists)
			{
				this.Log(Level.Verbose, Strings.OutputFileDoesNotExist(this.OutputFile.FullName));
				return true;
			}

			// See if one of the source files has changed since we built last.
			string changedFileName = FileSet.FindMoreRecentLastWriteTime(this.Sources.FileNames, this.OutputFile.LastWriteTime);
			if (changedFileName != null)
			{
				this.Log(Level.Verbose, Strings.FileHasBeenUpdated(changedFileName));
				return true;
			}

			// At this point we've determined that the tool does not need to rebuild anything.
			return false;
		}

		/// <summary>
		/// Writes all of the command-line parameters for the tool to a response file, one parameter per line.
		/// </summary>
		/// <param name="writer">The output writer.</param>
		protected override void WriteOptions(TextWriter writer)
		{
			base.WriteOptions(writer);
			writer.WriteLine("-out " + Utility.QuotePathIfNeeded(this.OutputFile.FullName));
		}
		#endregion
	}
}