//-------------------------------------------------------------------------------------------------
// <copyright file="CandleSettings.cs" company="Microsoft">
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
// Contains the CandleSettings class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Globalization;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Text;
	using Microsoft.Tools.WindowsInstallerXml;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;
	using Microsoft.VisualStudio.Shell.Interop;

#if !USE_NET20_FRAMEWORK
	using String = Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure.String;
#endif

	/// <summary>
	/// Options for the WiX candle compiler.
	/// </summary>
	internal sealed class CandleSettings : DirtyableObject, ICloneable
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(CandleSettings);

		private string absoluteOutputDirectory;
		private string projectVariablesResponseFilePath;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleSettings"/> class.
		/// </summary>
		public CandleSettings()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleSettings"/> class.
		/// </summary>
		/// <param name="absoluteOutputDirectory">The absolute path to the output directory.</param>
		public CandleSettings(string absoluteOutputDirectory)
		{
			Tracer.VerifyStringArgument(absoluteOutputDirectory, "absoluteOutputDirectory");
			this.absoluteOutputDirectory = PackageUtility.CanonicalizeDirectoryPath(absoluteOutputDirectory);
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets or sets the absolute path to the output directory.
		/// </summary>
		public string AbsoluteOutputDirectory
		{
			get { return this.absoluteOutputDirectory; }
			set
			{
				if (this.AbsoluteOutputDirectory != value)
				{
					this.absoluteOutputDirectory = PackageUtility.CanonicalizeDirectoryPath(value);
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
		/// Returns true if the file extension is one that candle will recognize.
		/// </summary>
		/// <param name="path">The absolute file path to test.</param>
		/// <returns>true if the file extension is one that candle will recognize; otherwise false.</returns>
		public static bool IsCompileableFile(string path)
		{
			string extension = PackageUtility.StripLeadingChar(Path.GetExtension(path), '.');
			return PackageUtility.FileStringEquals(extension, "wxs");
		}

		/// <summary>
		/// Cleans up the temporary files created when building, namely the response files.
		/// </summary>
		public void CleanTemporaryFiles()
		{
			if (this.projectVariablesResponseFilePath != null && this.projectVariablesResponseFilePath.Length > 0)
			{
				if (File.Exists(this.projectVariablesResponseFilePath))
				{
					File.Delete(this.projectVariablesResponseFilePath);
				}
			}
			this.projectVariablesResponseFilePath = null;
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
		/// Constructs the command line parameters to pass to candle.exe based on the current options.
		/// </summary>
		/// <returns>A string that can be passed to candle.exe.</returns>
		public string ConstructCommandLineParameters(string rootDirectory, string[] sourceFilePaths)
		{
			StringBuilder commandLine = new StringBuilder(256);
			string relativeOutputFileDir = PackageUtility.MakeRelative(rootDirectory, this.AbsoluteOutputDirectory);

			// Get the response file for all of the project variables.
			this.GenerateProjectVariablesResponseFile();
			commandLine.Append("@");
			commandLine.Append(PackageUtility.QuoteString(this.projectVariablesResponseFilePath));

			// -out parameter. Since it usually ends in a backslash (since it's a directory) and it could be quoted,
			// we have to make sure to add another quote to the end of the path so the command interpreter will
			// parse the command correctly.
			string outParam = PackageUtility.QuoteString(relativeOutputFileDir);
			if (PackageUtility.EndsWith(outParam, @"\""", StringComparison.Ordinal))
			{
				outParam += "\"";
			}
			commandLine.AppendFormat(" -out {0}", outParam);

			// Source file list.
			foreach (string sourceFile in sourceFilePaths)
			{
				if (!IsCompileableFile(sourceFile))
				{
					continue;
				}

				string relativeSourceFile = PackageUtility.MakeRelative(rootDirectory, sourceFile);
				commandLine.Append(" ");
				commandLine.Append(PackageUtility.QuoteString(relativeSourceFile));
			}

			return commandLine.ToString();
		}

		/// <summary>
		/// Generates an absolute output file from the specified input file.
		/// </summary>
		/// <param name="sourceFilePath">An absolute path to the source file to map.</param>
		/// <returns>The generated output file path, or <see cref="System.String.Empty">String.Empty</see> if the source file is not compileable.</returns>
		public string GetOutputFile(string sourceFilePath)
		{
			if (!IsCompileableFile(sourceFilePath))
			{
				return String.Empty;
			}

			string fileWithoutExtension = Path.GetFileNameWithoutExtension(sourceFilePath);
			string outputFile = fileWithoutExtension + ".wixobj";
			string absoluteOutputFile = Path.Combine(this.AbsoluteOutputDirectory, outputFile);

			return absoluteOutputFile;
		}

		/// <summary>
		/// Generates a list of output files from the specified list of input files. The output list
		/// is in absolute paths. The returned array's length could be different than the length
		/// of <paramref name="sourceFilePaths"/> if one or more of the source files is not compileable.
		/// </summary>
		/// <param name="sourceFilePaths">An array of absolute paths to the source files to map.</param>
		/// <returns>A list of output files from the specified list of input files.</returns>
		public string[] GetOutputFiles(string[] sourceFilePaths)
		{
			StringCollection outputFiles = new StringCollection();

			foreach (string sourceFile in sourceFilePaths)
			{
				string absoluteOutputFile = GetOutputFile(sourceFile);

				if (!String.IsNullOrEmpty(absoluteOutputFile))
				{
					outputFiles.Add(absoluteOutputFile);
				}
			}

			// Copy the string collection to an array so we can return it.
			string[] outputFileArr = new string[outputFiles.Count];
			((ICollection)outputFiles).CopyTo(outputFileArr, 0);

			return outputFileArr;
		}

		/// <summary>
		/// Encodes the specified file name into an all uppercase 8.3 DOS file name.
		/// </summary>
		/// <param name="fileName">The file name to encode.</param>
		/// <returns>The encoded 8.3 DOS file name.</returns>
		private string EncodeDosFileName(string fileName)
		{
			string dosFileName;

			// Get the first 4 characters of the extension (1 for the .) and upper case them.
			string extension = PackageUtility.EnsureLeadingChar(Path.GetExtension(fileName), '.');
			string dosExtension;
			if (extension.Length > 4)
			{
				dosExtension = extension.Substring(0, 4).ToUpper(CultureInfo.InvariantCulture);
			}
			else
			{
				dosExtension = extension.ToUpper(CultureInfo.InvariantCulture);
			}

			// Get the first 8 characters of the file name and upper case them.
			string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
			string dosFileNameWithoutExt;
			if (fileNameWithoutExt.Length > 8)
			{
				dosFileNameWithoutExt = fileNameWithoutExt.Substring(0, 8).ToUpper(CultureInfo.InvariantCulture);
			}
			else
			{
				dosFileNameWithoutExt = fileNameWithoutExt.ToUpper(CultureInfo.InvariantCulture);
			}

			// If there's an extension, then append it to the file name.
			if (dosExtension.Length > 1)
			{
				dosFileName = dosFileNameWithoutExt + dosExtension;
			}
			else
			{
				dosFileName = dosFileNameWithoutExt;
			}

			return dosFileName;
		}

		/// <summary>
		/// Strips out any illegal characters from the specified project name and returns the
		/// encoded project name.
		/// </summary>
		/// <param name="projectName">The project name to encode.</param>
		/// <returns>An encoded project name with '_' replacing spaces and dots.</returns>
		private string EncodeProjectName(string projectName)
		{
			string name = projectName.Replace(' ', '_');
			name = name.Replace('.', '_');

			return name;
		}

		/// <summary>
		/// Generates a response file that is passed in via the candle command line containing
		/// a list of preprocessor definitions for all of the project/solution directories.
		/// </summary>
		private void GenerateProjectVariablesResponseFile()
		{
			// Clear the cached file.
			this.CleanTemporaryFiles();

			// Get the name/value pair for all of the project variables.
			NameValueCollection variables = this.GetSolutionVariables();
			if (variables == null)
			{
				return;
			}

			// Generate a temporary file.
			string fileName = Path.GetTempFileName();

			// Open the file and write a line per entry in the variables collection.
			using (StreamWriter writer = new StreamWriter(fileName, false))
			{
				foreach (string defineName in variables.Keys)
				{
					string path = PackageUtility.QuoteString(variables[defineName]);
					writer.WriteLine("-d{0}={1}", defineName, path);
				}
			}

			this.projectVariablesResponseFilePath = fileName;
		}

		/// <summary>
		/// Gets the variables and directories for the specified project. Variables will be in
		/// the form <c>ProjectName.VariableName</c>.
		/// </summary>
		/// <param name="variables">The <see cref="NameValueCollection"/> to add the variables to.</param>
		/// <param name="hierarchy">The <see cref="IVsHierarchy"/> (project) from which to retrieve the variables.</param>
		private void GetProjectVariables(NameValueCollection variables, IVsHierarchy hierarchy)
		{
			// ProjectX variables
			try
			{
				int hr = NativeMethods.S_OK;

				// Get the project name and directory from the hierarchy.
				object projectNameObj;
				object projectDirObj;
				hr = hierarchy.GetProperty(NativeMethods.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectName, out projectNameObj);
				hr = hierarchy.GetProperty(NativeMethods.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectDir, out projectDirObj);

				// To get the project file path, we have to get an IPersistFileFormat interface from the hierarchy.
				IPersistFileFormat persistFileFormat = hierarchy as IPersistFileFormat;

				// Sometimes the hierarchy doesn't always support all of these requested properties.
				// We have to have all three to properly get all of the project properties.
				if (projectNameObj == null || projectDirObj == null || persistFileFormat == null)
				{
					Tracer.WriteLine(classType, "GetProjectVariables", Tracer.Level.Warning, "The hierarchy '{0}' does not support one of the VSHPROPID_ProjectName, VSHPROPID_ProjectDir, or IPersistFileFormat properties. Skipping the project variables.", projectNameObj);
					return;
				}

				// Get the project path.
				string projectPath;
				uint formatIndex;
				NativeMethods.ThrowOnFailure(persistFileFormat.GetCurFile(out projectPath, out formatIndex));

				// Construct the ProjectX variables from the ones retrieved from the hierarchy.
				string projectName = (string)projectNameObj;
				string projectDir = PackageUtility.StripTrailingChar((string)projectDirObj, Path.DirectorySeparatorChar);
				string projectFileName = Path.GetFileName(projectPath);
				string projectExt = PackageUtility.EnsureLeadingChar(Path.GetExtension(projectPath), '.');
				string projectDosFileName = this.EncodeDosFileName(projectFileName);

				// The variable name will be in the form ProjectName.VariableName. We have to strip out
				// any illegal characters from the project name since this will be a preprocessor definition.
				string projectPrefix = this.EncodeProjectName(projectName) + ".";

				// Add the ProjectX variables to the collection.
				variables.Add(projectPrefix + "ProjectDir", projectDir);
				variables.Add(projectPrefix + "ProjectDosFileName", projectDosFileName);
				variables.Add(projectPrefix + "ProjectExt", projectExt);
				variables.Add(projectPrefix + "ProjectFileName", projectFileName);
				variables.Add(projectPrefix + "ProjectName", projectName);
				variables.Add(projectPrefix + "ProjectPath", projectPath);

				// TargetX variables
				this.GetTargetVariables(variables, hierarchy, projectPrefix);
			}
			catch (Exception e)
			{
				if (ErrorUtility.IsExceptionUnrecoverable(e))
				{
					throw;
				}

				Tracer.WriteLineWarning(classType, "GetProjectVariables", "There was an error while trying to get the project variables. Skipping the ProjectX variables. Exception: {0}", e);
			}
		}

		/// <summary>
		/// Gets the directory variables from the solution and its projects.
		/// </summary>
		/// <returns>
		/// A <see cref="NameValueCollection"/> containing the definition and the path for the
		/// key and value respectively.
		/// </returns>
		private NameValueCollection GetSolutionVariables()
		{
			NameValueCollection variables = new NameValueCollection();

			// We're going to be using the ServiceProvider a bit so let's cache it.
			ServiceProvider serviceProvider = Package.Instance.Context.ServiceProvider;
			IVsSolution solution = serviceProvider.GetVsSolution(classType, "GetSolutionVariables");

			// Get the solution properties which hang off the IVsSolution interface.
			object solutionDirObj;
			object solutionFileNameObj;
			object solutionNameObj;
			NativeMethods.ThrowOnFailure(solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionDirectory, out solutionDirObj));
			NativeMethods.ThrowOnFailure(solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out solutionFileNameObj));
			NativeMethods.ThrowOnFailure(solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionBaseName, out solutionNameObj));

			string solutionDir = PackageUtility.StripTrailingChar((string)solutionDirObj, Path.DirectorySeparatorChar);
			string solutionFileName = Path.GetFileName((string)solutionFileNameObj);
			string solutionDosFileName = this.EncodeDosFileName(solutionFileName);
			string solutionName = (string)solutionNameObj;
			string solutionPath = Path.Combine(solutionDir, solutionFileName);
			string solutionExt = PackageUtility.EnsureLeadingChar(Path.GetExtension(solutionPath), '.');

			// Add the solution properties.
			variables.Add("SolutionDir", solutionDir);
			variables.Add("SolutionDosFileName", solutionDosFileName);
			variables.Add("SolutionExt", solutionExt);
			variables.Add("SolutionFileName", solutionFileName);
			variables.Add("SolutionName", solutionName);
			variables.Add("SolutionPath", solutionPath);

			// Loop through all of the non-virtual projects in the solution (including our own) and add
			// the project's variables.
			Guid emptyGuid = Guid.Empty;
			IEnumHierarchies enumHierarchies;
			NativeMethods.ThrowOnFailure(solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_ALLINSOLUTION, ref emptyGuid, out enumHierarchies));
			uint numberFetched;
			IVsHierarchy[] hierarchyArray = new IVsHierarchy[1];
			NativeMethods.ThrowOnFailure(enumHierarchies.Next(1, hierarchyArray, out numberFetched));
			while (numberFetched == 1)
			{
				IVsHierarchy hierarchy = hierarchyArray[0];
				Tracer.Assert(hierarchy != null, "We shouldn't be getting back a null IVsHierarchy from the IEnumHierarchies.Next function.");
				if (hierarchy != null)
				{
					this.GetProjectVariables(variables, hierarchy);
				}

				NativeMethods.ThrowOnFailure(enumHierarchies.Next(1, hierarchyArray, out numberFetched));
			}

			return variables;
		}

		/// <summary>
		/// Gets the variables and directories for the specified project. Variables will be in
		/// the form <c>ProjectName.VariableName</c>.
		/// </summary>
		/// <param name="variables">The <see cref="NameValueCollection"/> to add the variables to.</param>
		/// <param name="hierarchy">The <see cref="IVsHierarchy"/> (project) from which to retrieve the variables.</param>
		private void GetTargetVariables(NameValueCollection variables, IVsHierarchy hierarchy, string projectPrefix)
		{
			try
			{
				int hr = NativeMethods.S_OK;

				// Now we need to get a IVsProjectCfg2 object to get the TargetX variables. We do this
				// by querying the environment for the active configuration of the specified project.
				IVsSolutionBuildManager solutionBuildManager = Package.Instance.GetService(typeof(IVsSolutionBuildManager)) as IVsSolutionBuildManager;
				if (solutionBuildManager == null)
				{
					Tracer.WriteLine(classType, "GetTargetVariables", Tracer.Level.Warning, "Cannot get an instance of IVsSolutionBuildManager from the environment. Skipping the project's TargetX variables.");
					return;
				}
				IVsProjectCfg[] projectCfgArray = new IVsProjectCfg[1];
				hr = solutionBuildManager.FindActiveProjectCfg(IntPtr.Zero, IntPtr.Zero, hierarchy, projectCfgArray);
				if (NativeMethods.Failed(hr))
				{
					Tracer.WriteLineWarning(classType, "GetTargetVariables", "One of the projects in the solution does not support project configurations. Skipping the project's TargetX variables.");
					return;
				}
				IVsProjectCfg2 projectCfg2 = projectCfgArray[0] as IVsProjectCfg2;
				if (projectCfg2 == null)
				{
					Tracer.WriteLine(classType, "GetTargetVariables", Tracer.Level.Warning, "The IVsSolutionBuildManager.FindActiveProjectCfg returned a null object or an object that doesn't support IVsProjectCfg2. Skipping the project's TargetX variables.");
					return;
				}

				// Get the ConfigurationName and add it to the variables.
				string configurationName;
				NativeMethods.ThrowOnFailure(projectCfg2.get_DisplayName(out configurationName));
				variables.Add(projectPrefix + "ConfigurationName", configurationName);

				// We need to get the Built output group from the list of project output groups.
				IVsOutputGroup outputGroup;
				NativeMethods.ThrowOnFailure(projectCfg2.OpenOutputGroup("Built", out outputGroup));
				if (outputGroup == null)
				{
					Tracer.WriteLine(classType, "GetTargetVariables", Tracer.Level.Warning, "The project configuration '{0}' does not support the 'Built' output group. Skipping the TargetX variables.", configurationName);
					return;
				}

				// Get the key output canonical name from the Built output group.
				string keyOutputCanonicalName;
				NativeMethods.ThrowOnFailure(outputGroup.get_KeyOutput(out keyOutputCanonicalName));

				// Search through the outputs until we find the key output. We have to call get_Outputs
				// twice: once to get the number of outputs (we do this by passing in 0 as the number
				// requested), and then once to get the actual outputs.
				uint numberRequested = 0;
				IVsOutput2[] outputArray = new IVsOutput2[numberRequested];
				uint[] numberFetchedArray = new uint[1];
				NativeMethods.ThrowOnFailure(outputGroup.get_Outputs(numberRequested, outputArray, numberFetchedArray));

				// We should have the number of elements in the output array now, so get them.
				numberRequested = numberFetchedArray[0];
				outputArray = new IVsOutput2[numberRequested];
				NativeMethods.ThrowOnFailure(outputGroup.get_Outputs(numberRequested, outputArray, numberFetchedArray));
				IVsOutput2 keyOutput = null;
				for (int i = 0; i < numberFetchedArray[0]; i++)
				{
					if (outputArray.Length <= i)
					{
						break;
					}

					IVsOutput2 output = outputArray[i];
					string outputCanonicalName;
					NativeMethods.ThrowOnFailure(output.get_CanonicalName(out outputCanonicalName));
					if (outputCanonicalName == keyOutputCanonicalName)
					{
						keyOutput = output;
						break;
					}
				}

				// Check to make sure that we found the key output.
				if (keyOutput == null)
				{
					Tracer.WriteLine(classType, "GetTargetVariables", Tracer.Level.Warning, "We identified the key output from configuration '{0}' as '{1}', but when we iterated through the outputs we couldn't find the key output. Skipping the TargetX variables.", configurationName, keyOutputCanonicalName);
					return;
				}

				// Now that we have the key output, we can finally create the TargetX variables from
				// the key output's deploy source URL.
				string deploySourceUrl;
				NativeMethods.ThrowOnFailure(keyOutput.get_DeploySourceURL(out deploySourceUrl));

				// By convention, the deploy source URL starts with file:/// for file-based outputs.
				// Strip it off if it's there.
				if (deploySourceUrl.StartsWith("file:///"))
				{
					deploySourceUrl = deploySourceUrl.Substring("file:///".Length);
				}

				// Parse the TargetX variables from the deploy source URL.
				string targetPath = deploySourceUrl;
				string targetFileName = Path.GetFileName(targetPath);
				string targetDosFileName = this.EncodeDosFileName(targetFileName);
				string targetName = Path.GetFileNameWithoutExtension(targetFileName);
				string targetExt = PackageUtility.EnsureLeadingChar(Path.GetExtension(targetPath), '.');
				string targetDir = PackageUtility.StripTrailingChar(Path.GetDirectoryName(targetPath), Path.DirectorySeparatorChar);

				// Add the TargetX variables to the collection.
				variables.Add(projectPrefix + "TargetDir", targetDir);
				variables.Add(projectPrefix + "TargetDosFileName", targetDosFileName);
				variables.Add(projectPrefix + "TargetExt", targetExt);
				variables.Add(projectPrefix + "TargetFileName", targetFileName);
				variables.Add(projectPrefix + "TargetName", targetName);
				variables.Add(projectPrefix + "TargetPath", targetPath);
			}
			catch (Exception e)
			{
				if (ErrorUtility.IsExceptionUnrecoverable(e))
				{
					throw;
				}

				Tracer.WriteLineWarning(classType, "GetTargetVariables", "The project does not correctly implement all of its required IVsProjectCfg2 interfaces. Skipping the TargetX variables. Exception: {0}", e);
			}
		}
		#endregion
	}
}
