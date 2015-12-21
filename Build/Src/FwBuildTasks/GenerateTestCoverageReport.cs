using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	public class GenerateTestCoverageReport : Task
	{
		/// <summary>
		/// Gets or sets the full path to the NUnit assemblies (test DLLs).
		/// </summary>
		[Required]
		public ITaskItem[] Assemblies { get; set; }

		/// <summary>
		/// Gets or sets the output XML file.
		/// </summary>
		public string OutputXmlFile { get; set; }

		/// <summary>
		/// Gets or sets the working directory.
		/// </summary>
		public string WorkingDirectory { get; set; }

		[Required]
		public string DotCoverExe { get; set; }

		[Required]
		public string NUnitConsoleExe { get; set; }

		public bool Aggragate { get; set; }

		public override bool Execute()
		{
			if (!File.Exists(DotCoverExe) || !File.Exists(NUnitConsoleExe))
				return false;
				var coverPath = GenerateDotCoverAnalysisXml(Assemblies);
				RunDotCover(coverPath);
			return true;
		}

		private string GetWorkingDirectory()
		{
			if (!String.IsNullOrEmpty(WorkingDirectory))
			{
				return WorkingDirectory;
			}
			else
			{
				return Path.GetFullPath(Path.GetDirectoryName(Assemblies[0].ItemSpec));
			}
		}

		private void RunDotCover(string coverPath)
		{
			var msg = string.Format("Running {0} for {1}", DotCoverExe, coverPath);
			Log.LogMessage(MessageImportance.Normal, msg);
			var process = new Process
			{
				StartInfo =
				{
					FileName = DotCoverExe,
					Arguments = "analyse " +  coverPath,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					//required to allow redirects
					UseShellExecute = false,
					// do not start process in new window
					CreateNoWindow = true,
					WorkingDirectory = GetWorkingDirectory()
				}
			};
			try
			{
				msg = string.Format("Starting program: {1} ({2}) in {0}",
					process.StartInfo.WorkingDirectory,
					process.StartInfo.FileName,
					process.StartInfo.Arguments);

				Log.LogMessage(MessageImportance.Low, msg);

				if (process.Start())
					process.WaitForExit(30 * 60 * 000);
			}
			catch (Exception ex)
			{
				throw new Exception(String.Format("Got exception starting {0}", process.StartInfo.FileName), ex);
			}
		}

		private string GenerateDotCoverAnalysisXml(ITaskItem[] assemblies)
		{
			var firstAssembly = assemblies[0];
			var coverFile = @"<?xml version='1.0' encoding='utf-8'?>
			<AnalyseParams>
			  <TargetExecutable>" + NUnitConsoleExe + @"</TargetExecutable>
			  <TargetArguments>" + GenerateDotCoverTargetArguments(assemblies) + @"</TargetArguments>
			  <TargetWorkingDir>" + Path.GetDirectoryName(firstAssembly.ItemSpec) + @"</TargetWorkingDir>
			  <Output>" + OutputXmlFile + @"</Output>
			  <ReportType>XML</ReportType>
			  <!-- Coverage filters. It's possible to use asterisks as wildcard symbols. -->
			  <Filters>
				<IncludeFilters>" + GenerateDotCoverIncludeFilters(assemblies) + @"</IncludeFilters>
			  </Filters>
			</AnalyseParams>";

			var doc = XDocument.Parse(coverFile);
			var configFilePath = Path.Combine(Path.GetDirectoryName(firstAssembly.ItemSpec),
				(assemblies.Length == 1 ? Path.GetFileNameWithoutExtension(firstAssembly.ItemSpec) : "MultipleFWProjects") + ".coversettings.xml");
			doc.Save(configFilePath);
			return configFilePath;
		}

		private string GenerateDotCoverIncludeFilters(ITaskItem[] assemblies)
		{
			var includeFilters = "";
			foreach (var assembly in assemblies)
			{
				var testAssemblyName = assembly.ItemSpec;
				if (!File.Exists(testAssemblyName))
				{
					Log.LogMessage(MessageImportance.High, "Could not cover {0} because it was not built", testAssemblyName);
					continue;
				}
				var mainAssembly = testAssemblyName.IndexOf("Tests") > 0 ? testAssemblyName.Substring(0, testAssemblyName.IndexOf("Tests")) : testAssemblyName;
				includeFilters += string.Format("\t<FilterEntry>\n\t\t<ModuleMask>{0}</ModuleMask>\n\t</FilterEntry>", Path.GetFileNameWithoutExtension(mainAssembly));
			}
			return includeFilters;
		}

		private object GenerateDotCoverTargetArguments(ITaskItem[] assemblies)
		{
			return string.Join(" ", assemblies.TakeWhile(x => File.Exists(x.ItemSpec)).Select(x => x.ItemSpec));
		}
	}
}
