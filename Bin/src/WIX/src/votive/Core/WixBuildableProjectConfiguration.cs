//-------------------------------------------------------------------------------------------------
// <copyright file="WixBuildableProjectConfiguration.cs" company="Microsoft">
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
// Provides configuration information to the Visual Studio shell about a buildable WiX project.
// </summary>
//-------------------------------------------------------------------------------------------------

// TODO: Localize the build event output text (to the output pane).

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Globalization;
	using System.IO;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;
	using Microsoft.VisualStudio.Shell.Interop;

	internal sealed class WixBuildableProjectConfiguration : BuildableProjectConfiguration
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(WixBuildableProjectConfiguration);
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public WixBuildableProjectConfiguration(WixProjectConfiguration projectConfiguration) : base(projectConfiguration)
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		public new WixProject Project
		{
			get { return (WixProject)base.Project; }
		}

		public new WixProjectConfiguration ProjectConfiguration
		{
			get { return (WixProjectConfiguration)base.ProjectConfiguration; }
		}

		/// <summary>
		/// Gets a value indicating whether the project needs to be built.
		/// </summary>
		protected override bool IsUpToDate
		{
			get
			{
				// See if the output is even built
				if (!File.Exists(this.Light.AbsoluteOutputFilePath))
				{
					return false;
				}

				// Check the source files
				string[] outOfDateFiles = this.GetOutOfDateSourceFiles();
				bool upToDate = (outOfDateFiles.Length == 0);

				if (upToDate)
				{
					// Check the localization files
					outOfDateFiles = this.GetOutOfDateLocalizationFiles();
					upToDate = (outOfDateFiles.Length == 0);

					if (upToDate)
					{
						// Check the wixlib reference files
						outOfDateFiles = this.GetOutOfDateReferenceFiles();
						upToDate = (outOfDateFiles.Length == 0);
					}
				}

				return upToDate;
			}
		}

		/// <summary>
		/// Gets the candle.exe settings.
		/// </summary>
		private CandleSettings Candle
		{
			get { return this.ProjectConfiguration.CandleSettings; }
		}

		/// <summary>
		/// Gets the light.exe settings.
		/// </summary>
		private LightSettings Light
		{
			get { return this.ProjectConfiguration.LightSettings; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Performs an incremental build.
		/// </summary>
		/// <param name="outputPane">The window to output message to.</param>
		/// <returns>true if the build succeeded; otherwise, false.</returns>
		protected override bool BuildInternal(IVsOutputWindowPane outputPane)
		{
			bool successful = true;

			if (IsUpToDate)
			{
				this.WriteLineToOutputWindow("Build up to date. No need to compile or link.");
			}
			else
			{
				// First compile, then link
				successful = this.Compile(outputPane);
				successful = successful && this.TickBuild();
				successful = successful && this.Link(outputPane);
			}

			if (successful)
			{
				this.WriteLineToOutputWindow("Build completed successfully");
			}
			else
			{
				this.WriteLineToOutputWindow("Build completed with errors");
			}
			this.WriteLineToOutputWindow();

			return successful;
		}

		/// <summary>
		/// Performs a clean by removing all of the wixobj compiler output and the linker output (MSI/MSM/wixlib).
		/// </summary>
		/// <param name="outputPane">The window to output message to.</param>
		/// <returns>true if the build succeeded; otherwise, false.</returns>
		protected override bool CleanInternal(IVsOutputWindowPane outputPane)
		{
			// Tell the user we're starting to clean.
			string message = WixStrings.OutputWindowClean(this.Project.Name, this.ProjectConfiguration.Name);
			Tracer.WriteLineInformation(classType, "CleanInternal", message);
			this.WriteLineToOutputWindow(message);
			this.WriteLineToOutputWindow();

			// Tick once and see if we should continue.
			if (!this.TickBuild())
			{
				return false;
			}

			// Clean the candle and light output by deleting the wixobj and MSI files.
			string[] sourceFiles = this.GetSourceFiles();
			this.CleanCompileOutput(sourceFiles);
			this.CleanLinkOutput();

			return true;
		}

		/// <summary>
		/// Cleans the output from a compilation.
		/// </summary>
		/// <param name="sourceFiles">An array of source files that generates the compilation output.</param>
		private void CleanCompileOutput(string[] sourceFiles)
		{
			try
			{
				// Delete the output files from candle if they exist.
				string[] wixobjFiles = this.Candle.GetOutputFiles(sourceFiles);
				foreach (string wixobjFile in wixobjFiles)
				{
					if (File.Exists(wixobjFile))
					{
						File.Delete(wixobjFile);
					}
				}
			}
			catch (IOException e)
			{
				// We don't care if we get an IOException because we're cleaning anyway. It's non-fatal.
				// Let's log it, though.
				Tracer.WriteLine(classType, "CleanCompileOutput", Tracer.Level.Warning, "Exception when trying to access/delete the output files for a clean compile: {0}", e.Message);
			}
		}

		/// <summary>
		/// Deletes the output file (MSI) from a linking step.
		/// </summary>
		private void CleanLinkOutput()
		{
			try
			{
				// Delete the output file (MSI) from light if it exists.
				string msiFile = this.Light.AbsoluteOutputFilePath;
				if (File.Exists(msiFile))
				{
					File.Delete(msiFile);
				}
			}
			catch (IOException e)
			{
				// We don't care if we get an IOException because we're cleaning anyway. It's non-fatal.
				// Let's log it, though.
				Tracer.WriteLine(classType, "CleanLinkOutput", Tracer.Level.Warning, "Exception when trying to access/delete the output files for a clean link: {0}", e.Message);
			}
		}

		/// <summary>
		/// Runs candle.exe on the source files to generate a list of wixobj intermediate files.
		/// </summary>
		/// <param name="outputPane">The window to which to output build messages.</param>
		/// <returns><see langword="true"/> if successful; otherwise, <see langword="false"/>.</returns>
		private bool Compile(IVsOutputWindowPane outputPane)
		{
			Tracer.VerifyNonNullArgument(outputPane, "outputPane");

			string projectRootDirectory = this.Project.RootDirectory;

			// Get the list of source files that should be built
			string[] sourceFiles = this.GetOutOfDateSourceFiles();

			// If we don't have anything to compile, then just show a message indicating that
			if (sourceFiles.Length == 0)
			{
				this.WriteLineToOutputWindow("Compile targets are up to date.");
				this.WriteLineToOutputWindow();
				return true;
			}

			string[] objectFiles = this.Candle.GetOutputFiles(sourceFiles);
			string candleParams = this.Candle.ConstructCommandLineParameters(projectRootDirectory, sourceFiles);
			string toolsPath = WixPackage.Instance.Context.Settings.ToolsDirectory;

			// Do not quote the path here. It will be quoted in the LaunchPad
			string candleExePath = PackageUtility.CanonicalizeFilePath(Path.Combine(toolsPath, "candle.exe"));

			// See if candle exists
			if (!File.Exists(candleExePath))
			{
				this.WriteLineToOutputWindow("Error: Cannot find candle.exe at '{0}'.", candleExePath);
				this.WriteLineToOutputWindow();
				return false;
			}

			// Create the launch pad used for compilation.
			LaunchPad candleLaunchPad = new LaunchPad(candleExePath, candleParams);
			candleLaunchPad.WorkingDirectory = projectRootDirectory;

			// Output the candle command line to the build output window.
			Tracer.WriteLineInformation(classType, "Compile", "Performing main compilation...");
			this.WriteLineToOutputWindow("Performing main compilation...");
			this.WriteLineToOutputWindow(candleLaunchPad.CommandLine);
			this.WriteLineToOutputWindow();

			// Tick once and see if we should continue.
			if (!this.TickBuild())
			{
				return false;
			}

			// Make sure the output directory exists.
			string outputDir = this.Candle.AbsoluteOutputDirectory;
			if (!Directory.Exists(outputDir))
			{
				Directory.CreateDirectory(outputDir);
			}

			// Delete the existing .wixobj files if they exist.
			this.CleanCompileOutput(sourceFiles);

			// Execute candle.exe piping the output to the output build window and the task pane.
			bool successful = (candleLaunchPad.ExecuteCommand(outputPane, this) == 0);

			// Clean up the temporary candle files after the compile has completed.
			this.Candle.CleanTemporaryFiles();

			return successful;
		}

		/// <summary>
		/// Runs light.exe on the compiled wixobj files to generate the final MSI file.
		/// </summary>
		/// <param name="outputPane">The window to which to output build messages.</param>
		/// <returns><see langword="true"/> if successful; otherwise, <see langword="false"/>.</returns>
		private bool Link(IVsOutputWindowPane outputPane)
		{
			Tracer.VerifyNonNullArgument(outputPane, "outputPane");

			string projectRootDirectory = this.Project.RootDirectory;
			string[] sourceFiles = this.GetSourceFiles();
			string[] objectFiles = this.Candle.GetOutputFiles(sourceFiles);
			string[] localizationFiles = this.GetLocalizationFiles();
			string[] referenceFiles = this.GetReferenceFiles();
			string lightParams = this.Light.ConstructCommandLineParameters(projectRootDirectory, objectFiles, localizationFiles, referenceFiles);
			string toolsPath = WixPackage.Instance.Context.Settings.ToolsDirectory;

			// Do not quote the path here. It will be quoted in the LaunchPad
			string lightExePath = PackageUtility.CanonicalizeFilePath(Path.Combine(toolsPath, "light.exe"));

			// See if light.exe exists.
			if (!File.Exists(lightExePath))
			{
				this.WriteLineToOutputWindow("Error: Cannot find light.exe at '{0}'.", lightExePath);
				this.WriteLineToOutputWindow();
				return false;
			}

			// Create the launch pad used for linking.
			LaunchPad lightLaunchPad = new LaunchPad(lightExePath, lightParams);
			lightLaunchPad.WorkingDirectory = projectRootDirectory;

			// Output the light command line to the build output window.
			Tracer.WriteLineInformation(classType, "Link", "Linking...");
			this.WriteLineToOutputWindow();
			this.WriteLineToOutputWindow("Linking...");
			this.WriteLineToOutputWindow(lightLaunchPad.CommandLine);
			this.WriteLineToOutputWindow();

			// Tick once and see if we should continue.
			if (!this.TickBuild())
			{
				return false;
			}

			// Delete the existing .msi file if it exists.
			this.CleanLinkOutput();

			// Execute light.exe piping the output to the output build window and the task pane.
			bool successful = (lightLaunchPad.ExecuteCommand(outputPane, this) == 0);

			return successful;
		}


		/// <summary>
		/// Returns an array of absolute file paths to all of the .wxl localization files in the project.
		/// </summary>
		/// <returns>.</returns>
		private string[] GetLocalizationFiles()
		{
			StringCollection localizationFiles = new StringCollection();
			string[] rawFiles = this.GetAllCompileBuildActionSourceFiles();

			foreach (string localizationFile in rawFiles)
			{
				if (LightSettings.IsLocalizationFile(localizationFile))
				{
					localizationFiles.Add(localizationFile);
				}
			}

			return ToStringArray(localizationFiles);
		}


		/// <summary>
		/// Returns an array of absolute file paths of all of the .wxl localization files in the project that are newer than the project output file.
		/// </summary>
		/// <returns>.</returns>
		private string[] GetOutOfDateLocalizationFiles()
		{
			StringCollection outOfDateFiles = new StringCollection();
			string[] localizationFiles = this.GetLocalizationFiles();
			string outputFile = this.ProjectConfiguration.LightSettings.AbsoluteOutputFilePath;
			bool outputExists = File.Exists(outputFile);
			DateTime outputLastMod = (outputExists ? File.GetLastWriteTime(outputFile) : DateTime.MinValue);

			foreach (string localizationFile in localizationFiles)
			{
				// The file is out of date if it is newer than the output file
				if (!outputExists || !File.Exists(localizationFile) || File.GetLastWriteTime(localizationFile) > outputLastMod)
				{
					outOfDateFiles.Add(localizationFile);
				}
			}

			return ToStringArray(outOfDateFiles);
		}

		/// <summary>
		/// Returns an array of absolute file paths of all of the .wixlib files in the project that are newer than the project output file.
		/// </summary>
		/// <returns>.</returns>
		private string[] GetOutOfDateReferenceFiles()
		{
			StringCollection outOfDateFiles = new StringCollection();
			string[] referenceFiles = this.GetReferenceFiles();
			string outputFile = this.ProjectConfiguration.LightSettings.AbsoluteOutputFilePath;
			bool outputExists = File.Exists(outputFile);
			DateTime outputLastMod = (outputExists ? File.GetLastWriteTime(outputFile) : DateTime.MinValue);

			foreach (string referenceFile in referenceFiles)
			{
				// The file is out of date if it is newer than the output file
				if (!outputExists || !File.Exists(referenceFile) || File.GetLastWriteTime(referenceFile) > outputLastMod)
				{
					outOfDateFiles.Add(referenceFile);
				}
			}

			return ToStringArray(outOfDateFiles);
		}

		/// <summary>
		/// Returns an array of absolute file paths of all of the source files in the project that need to be build.
		/// </summary>
		/// <returns>.</returns>
		private string[] GetOutOfDateSourceFiles()
		{
			StringCollection outOfDateFiles = new StringCollection();
			string[] sourceFiles = this.GetAllCompileBuildActionSourceFiles();

			foreach (string sourceFile in sourceFiles)
			{
				if (CandleSettings.IsCompileableFile(sourceFile))
				{
					string outputFile = this.Candle.GetOutputFile(sourceFile);

					// The source file needs to be built if the output file does not exist or
					// if the source file is newer than the output file. If the source file
					// doesn't exist, add it to the list. Candle will generate an appropriate error.
					if (!File.Exists(outputFile) || !File.Exists(sourceFile) || File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(outputFile))
					{
						outOfDateFiles.Add(sourceFile);
					}
				}
			}

			return ToStringArray(outOfDateFiles);
		}

		/// <summary>
		/// Returns an array of absolute files paths of all of the referenced .wixlib libraries.
		/// </summary>
		/// <returns>.</returns>
		private string[] GetReferenceFiles()
		{
			StringCollection referenceFiles = new StringCollection();
			WixlibReferenceFileNode[] referenceNodes = (WixlibReferenceFileNode[])this.Project.ReferencesNode.Children.ToArray(typeof(WixlibReferenceFileNode));

			foreach (WixlibReferenceFileNode referenceNode in referenceNodes)
			{
				referenceFiles.Add(referenceNode.AbsolutePath);
			}

			return ToStringArray(referenceFiles);
		}

		/// <summary>
		/// Returns an array of absolute file paths of all of the files in the project that should be compiled.
		/// </summary>
		/// <returns>.</returns>
		private string[] GetSourceFiles()
		{
			StringCollection sourceFiles = new StringCollection();
			string[] rawFiles = this.GetAllCompileBuildActionSourceFiles();

			foreach (string sourceFile in rawFiles)
			{
				if (CandleSettings.IsCompileableFile(sourceFile))
				{
					sourceFiles.Add(sourceFile);
				}
			}

			return ToStringArray(sourceFiles);
		}

		/// <summary>
		/// Returns an array of absolute file paths of all of the files in the project where the
		/// BuildAction = Compile.
		/// </summary>
		/// <returns>.</returns>
		private string[] GetAllCompileBuildActionSourceFiles()
		{
			StringCollection sourceFiles = new StringCollection();
			ArrayList nodesToProcess = new ArrayList(this.Project.RootNode.Children);
			while (nodesToProcess.Count > 0)
			{
				Node node = (Node)nodesToProcess[0];
				FolderNode folderNode = node as FolderNode;
				string sourceFile = node.AbsolutePath;

				// Remove the node that we are processing.
				nodesToProcess.RemoveAt(0);

				// If we have a sub-folder (that's not the library folder), then add all of its children to the process array.
				if (folderNode != null)
				{
					if (!(folderNode is ReferenceFolderNode))
					{
						nodesToProcess.InsertRange(0, folderNode.Children);
					}
				}
				else if (node.BuildAction == BuildAction.Compile)
				{
					sourceFiles.Add(sourceFile);
				}
			}

			return ToStringArray(sourceFiles);
		}

		/// <summary>
		/// Converts the specified <see cref="StringCollection"/> to a string array.
		/// </summary>
		/// <param name="collection">The collection to convert.</param>
		/// <returns>A corresponding string array.</returns>
		private string[] ToStringArray(StringCollection collection)
		{
			// Copy the collection to an array
			string[] array = new string[collection.Count];
			collection.CopyTo(array, 0);

			return array;
		}
		#endregion
	}
}
