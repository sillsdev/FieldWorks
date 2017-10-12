// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using FwBuildTasks;
using Microsoft.Build.Framework;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	public class ProjectLocalizer
	{
		private class ResourceInfo
		{
			public string ProjectFolder;
			public List<string> ResXFiles;
			public string RootNameSpace;
			public string AssemblyName;
		}

		public ProjectLocalizer(string projectFolder, ProjectLocalizerOptions options)
		{
			ProjectFolder = projectFolder;
			Options = options;

			try
			{
				var x = new CultureInfo(Options.Locale);
				LocaleIsSupported = true;
			}
			catch
			{
				Console.WriteLine("Warning: Culture name {0} is not supported.", Options.Locale);
				LocaleIsSupported = false;
			}
		}

		private string ProjectFolder { get; }
		private ProjectLocalizerOptions Options { get; }
		private bool LocaleIsSupported { get; }

		public void ProcessProject()
		{
			Options.LogMessage(MessageImportance.Low, "Processing project {0}", ProjectFolder);

			var resourceInfo = GetResourceInfo(ProjectFolder);
			if (resourceInfo == null || resourceInfo.ResXFiles.Count == 0)
				return; // nothing to localize; in particular we should NOT call al with no inputs.

			if (Options.BuildSource)
				CreateSources(resourceInfo);

			if (Options.BuildBinaries)
				CreateResourceAssemblies(resourceInfo);
		}

		private static ResourceInfo GetResourceInfo(string projectFolder)
		{
			var projectFile = Directory.GetFiles(projectFolder, "*.csproj").First(); // only called if there is exactly one.
			var doc = XDocument.Load(projectFile);
			XNamespace ns = @"http://schemas.microsoft.com/developer/msbuild/2003";

			var resxFiles = GetResXFiles(projectFolder);
			if (resxFiles.Count == 0)
				return null;

			var resourceInfo = new ResourceInfo {
				ProjectFolder = projectFolder,
				ResXFiles = resxFiles,
				RootNameSpace = doc.Descendants(ns + "RootNamespace").First().Value,
				AssemblyName = doc.Descendants(ns + "AssemblyName").First().Value
			};

			return resourceInfo;
		}

		private static List<string> GetResXFiles(string projectFolder)
		{
			var resxFiles = Directory.GetFiles(projectFolder, "*.resx").ToList();
			// include child folders, one level down, which do not have their own .csproj.
			foreach (var childFolder in Directory.GetDirectories(projectFolder))
			{
				if (Directory.GetFiles(childFolder, "*.csproj").Any())
					continue;
				resxFiles.AddRange(Directory.GetFiles(childFolder, "*.resx"));
			}
			return resxFiles;
		}

		private void LocalizeResx(ResourceInfo resourceInfo, string resxPath)
		{
			var localizedResxPath = GetLocalizedResxPath(resourceInfo, resxPath);
			Directory.CreateDirectory(Path.GetDirectoryName(localizedResxPath));
			var stylesheet = Path.Combine(Options.RealBldFolder, "LocalizeResx.xsl");
			var parameters = new List<BuildUtils.XsltParam>();
			parameters.Add(new BuildUtils.XsltParam { Name = "lang", Value = Options.Locale });
			// The output directory that the transform wants is not the one where it will write
			// the file, but the base Output directory, where it expects to find that we have
			// written the XML version of the PO file, [locale].xml.
			parameters.Add(new BuildUtils.XsltParam { Name = "outputdir", Value = Options.OutputFolder });
			//parameters.Add(new XsltParam() { Name = "verbose", Value = "true" });
			BuildUtils.ApplyXslt(stylesheet, resxPath, localizedResxPath, parameters);
		}

		private void CreateSources(ResourceInfo resourceInfo)
		{
			foreach (var resxFile in resourceInfo.ResXFiles)
			{
				Options.LogMessage(MessageImportance.Low, "Creating source for {0}", resxFile);
				LocalizeResx(resourceInfo, resxFile);
				Options.LogMessage(MessageImportance.Low, "Done creating source for {0}", resxFile);
			}
		}

		private string GetLocalizedResxPath(ResourceInfo resourceInfo, string resxPath)
		{
			var resxFileName = Path.GetFileNameWithoutExtension(resxPath);
			var partialDir = Path.GetDirectoryName(resxPath.Substring(Options.SrcFolder.Length + 1));
			var projectPartialDir = resourceInfo.ProjectFolder.Substring(Options.SrcFolder.Length + 1);
			var outputFolder = Path.Combine(Options.OutputFolder, Options.Locale, partialDir);
			// This is the relative path from the project folder to the resx file folder.
			// It needs to go into the file name if not empty, but with a dot instead of folder separator.
			var subFolder = "";
			if (partialDir.Length > projectPartialDir.Length)
				subFolder = Path.GetFileName(partialDir) + ".";
			var fileName = $"{resourceInfo.RootNameSpace}.{subFolder}{resxFileName}.{Options.Locale}.resx";
			return Path.Combine(outputFolder, fileName);
		}

		private void CreateResourceAssemblies(ResourceInfo resourceInfo)
		{
			if (resourceInfo.ResXFiles.Count == 0)
				return; // nothing to localize; in particular we should NOT call al with no inputs.

			var embedResources = new List<EmbedInfo>();
			var errors = 0;
			foreach (var resxFile in resourceInfo.ResXFiles)
			{
				Options.LogMessage(MessageImportance.Low, "Creating assembly for {0}", resxFile);
				var localizedResxPath = GetLocalizedResxPath(resourceInfo, resxFile);
				var localizedResourcePath = Path.ChangeExtension(localizedResxPath, ".resources");
				try
				{
					RunResGen(localizedResourcePath, localizedResxPath, Path.GetDirectoryName(resxFile));
					embedResources.Add(new EmbedInfo(localizedResourcePath, Path.GetFileName(localizedResourcePath)));
				}
				catch (Exception ex)
				{
					Options.LogError(
						$"Error occurred while processing {Path.GetFileName(resourceInfo.ProjectFolder)} for {Options.Locale}: {ex.Message}");
					errors++;
				}
				Options.LogMessage(MessageImportance.Low, "Done creating assembly for {0}", resxFile);
			}
			if (errors == resourceInfo.ResXFiles.Count)
			{
				Options.LogMessage(MessageImportance.Low, $"All resx files for {Options.Locale} had errors; skipping AL");
				return;
			}
			var resourceFileName = resourceInfo.AssemblyName + ".resources.dll";
			var mainDllFolder = Path.Combine(Options.OutputFolder, Options.Config);
			var localDllFolder = Path.Combine(mainDllFolder, Options.Locale);
			var resourceDll = Path.Combine(localDllFolder, resourceFileName);
			var culture = LocaleIsSupported ? Options.Locale : string.Empty;
			try
			{
				Options.LogMessage(MessageImportance.Low, "Running AL for {0}", resourceDll);
				RunAssemblyLinker(resourceDll, culture, Options.FileVersion,
					Options.InformationVersion, Options.Version, embedResources);
				Options.LogMessage(MessageImportance.Low, "Done running AL for {0}", resourceDll);
			}
			catch (Exception ex)
			{
				Options.LogError(
					$"Error occurred while processing {Path.GetFileName(resourceInfo.ProjectFolder)} for {Options.Locale}: {ex.Message}");
			}
		}

		/// <summary>
		/// Run the AssemblyLinker to create a resource DLL with the specified path and other details containing the specified embedded resources.
		/// </summary>
		/// <param name="outputDllPath"></param>
		/// <param name="culture"></param>
		/// <param name="fileversion"></param>
		/// <param name="productVersion"></param>
		/// <param name="version"></param>
		/// <param name="resources"></param>
		protected virtual void RunAssemblyLinker(string outputDllPath, string culture,
			string fileversion, string productVersion, string version, List<EmbedInfo> resources )
		{
			// Run assembly linker with the specified arguments
			Directory.CreateDirectory(Path.GetDirectoryName(outputDllPath)); // make sure the directory in which we want to make it exists.
			var fileName = IsUnix ? "al" : "al.exe";
			var arguments = BuildLinkerArgs(outputDllPath, culture, fileversion,
				productVersion, version, resources);
			var stdOutput = string.Empty;
			var exitCode = RunProcess(fileName, arguments, out stdOutput);
			if (exitCode != 0)
			{
				throw new ApplicationException(
					$"Assembly linker returned error {exitCode} for {outputDllPath}.\n" +
					$"Command line: {fileName} {arguments}\nOutput:\n{stdOutput}");
			}
		}

		private static int RunProcess(string fileName, string arguments, out string stdOutput,
			int timeout = 300000 /* 5 min */, string workdir = null)
		{
			var output = string.Empty;
			using (var outputWaitHandle = new AutoResetEvent(false))
			{
				using (var process = new Process())
				{
					try
					{
						process.StartInfo.UseShellExecute = false;
						process.StartInfo.RedirectStandardOutput = true;
						process.StartInfo.FileName = fileName;
						process.StartInfo.Arguments = arguments;
						if (workdir != null)
							process.StartInfo.WorkingDirectory = workdir;
						process.OutputDataReceived += (sender, e) =>
						{
							if (e.Data == null)
								outputWaitHandle.Set();
							else
								output = e.Data;
						};
						process.Start();

						process.BeginOutputReadLine();
						process.WaitForExit(timeout);
						stdOutput = output;
						return process.ExitCode;
					}
					finally
					{
						outputWaitHandle.WaitOne(timeout);
					}
				}
			}
		}

		private static bool IsUnix => Environment.OSVersion.Platform == PlatformID.Unix;

		protected static string BuildLinkerArgs(string outputDllPath, string culture, string fileversion,
			string productVersion, string version, List<EmbedInfo> resources)
		{
			var builder = new StringBuilder();
			builder.Append($" /out:\"{outputDllPath}\"");
			foreach (var info in resources)
			{
				builder.Append($" /embed:{info.Resource},{info.Name}");
			}
			if (!string.IsNullOrEmpty(culture))
			{
				builder.Append($" /culture:{culture}");
			}
			builder.Append($" /fileversion:{fileversion}");
			builder.Append($" /productversion:\"{productVersion}\"");
				// may be something like "8.4.2 beta 2" (see LT-14436). Test does not really cover this.
			builder.Append($" /version:{version}");
			// Note: the old build process also set \target, but we want the default lib so don't need to be explicit.
			// the old version also had support for controlling verbosity; we can add that if needed.
			// May also want to set /config? The old version did not so I haven't.
			return builder.ToString();
		}

		protected virtual void RunResGen(string outputResourcePath, string localizedResxPath,
			string originalResxFolder)
		{
			var fileName = IsUnix ? "resgen" : "resgen.exe";
			var arguments = $"\"{localizedResxPath}\" \"{outputResourcePath}\"";
			if (!IsUnix)
			{
				// It needs to be able to reference the appropriate System.Drawing.dll and System.Windows.Forms.dll to make the conversion.
				var clrFolder = RuntimeEnvironment.GetRuntimeDirectory();
				var drawingPath = Path.Combine(clrFolder, "System.Drawing.dll");
				var formsPath = Path.Combine(clrFolder, "System.Windows.Forms.dll");

				arguments += $" /r:\"{drawingPath}\" /r:\"{formsPath}\"";
			}
			var stdOutput = string.Empty;
			// Setting the working directory to the folder containing the ORIGINAL resx file allows us to find included files
			// like FDO/Resources/Question.ico that the resx file refers to using relative paths.
			var exitCode = RunProcess(fileName, arguments, out stdOutput, workdir: originalResxFolder);
			if (exitCode != 0)
			{
				throw new ApplicationException($"Resgen returned error {exitCode} for {localizedResxPath}.\n" +
					$"Command line: {fileName} {arguments}\nOutput:\n{stdOutput}");
			}
		}

	}
}
