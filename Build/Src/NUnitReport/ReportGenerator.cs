// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using FwBuildTasks;
using Microsoft.Build.Framework;

namespace NUnitReport
{
	internal class ReportGenerator : ITaskHost
	{
		private const string s_output = "Output";
		private const string s_debug = "Debug";
		private const string s_nunitSuffix = ".dll-nunit-output.xml";
		private const string s_coverageSuffix = ".coverage.xml";
		private const string s_unitppSuffix = "-results.xml";
		private LoggerVerbosity m_verbosity = LoggerVerbosity.Minimal;
		private ReportType m_reportType = ReportType.NUnit;

		public List<string> Projects { get; set; }

		public string OutputDir
		{
			get;
			private set;
		}

		public ReportGenerator(string[] args)
		{
			OutputDir = SetOutputDirectory();
			Projects = ParseArgs(args);
		}

		private static string SetOutputDirectory()
		{
			var curDir = Directory.GetCurrentDirectory();
			var root = Directory.GetDirectoryRoot(curDir);
			var outDir = Path.DirectorySeparatorChar + s_output + Path.DirectorySeparatorChar + s_debug;
			var topDir = GetRightLevelDirectory(root, curDir, outDir);
			return topDir + outDir;
		}

		private static string GetRightLevelDirectory(string root, string curDir, string outDir)
		{
			while (Directory.GetParent(curDir).FullName != root)
			{
				if (Directory.Exists(curDir + outDir))
					return curDir;
				curDir = Directory.GetParent(curDir).FullName;
			}
			return curDir; // Hopefully this one has an Output/Debug subdir!
		}

		private List<string> ParseArgs(string[] args)
		{
			var result = new List<string>();
			if (args.Length == 0 || args[0] == "/?" || args[0] == "/help")
				return result;

			List<string> wildResults = null;

			foreach (string argument in args)
			{
				if (argument.StartsWith("/v"))
				{
					SetVerbosity(argument.Split(':').Last());
					continue;
				}
				if (argument == "/a")
				{
					// Collect all matching files.
					wildResults = CollectAllMatchingFiles();
					continue;
				}
				if (argument.StartsWith("/t"))
				{
					SetReportType(argument.Split(':').Last());
					continue;
				}
				// Check project name
				if (!File.Exists(BuildCompleteTestReportName(argument))
					&& !File.Exists(BuildCompleteUnitppReportName(argument))
					&& !File.Exists(BuildCoverReportName(argument)))
				{
					// No results of any type were found for the given project
					continue;
				}
				result.Add(argument);
			}
			if (wildResults != null)
			{
				if (result.Count > 0)
				{
					Console.WriteLine("The /a switch cannot be combined with explicit test fixture names.  It is being ignored.");
					return result;
				}
				return wildResults;
			}
			return result;
		}

		private void SetReportType(string reportType)
		{
			var rt = reportType.ToLowerInvariant();
			// defaults to NUnit
			switch (rt)
			{
				case "dotcover":
					m_reportType = ReportType.DotCover;
					break;
				case "nunit":
					m_reportType = ReportType.NUnit;
					break;
				default:
					Console.WriteLine("Unknown type {0} used. Defaulting to NUnit", reportType);
					break;
			}
		}

		private void SetVerbosity(string verbosityArg)
		{
			// defaults to minimal
			switch (verbosityArg)
			{
				case "":
				case "d":
				case "detailed":
					m_verbosity = LoggerVerbosity.Detailed;
					break;
				case "diagnostic":
					m_verbosity = LoggerVerbosity.Diagnostic;
					break;
				case "n":
				case "normal":
					m_verbosity = LoggerVerbosity.Normal;
					break;
				default:
					m_verbosity = LoggerVerbosity.Minimal; // 'quiet' will end up here too.
					break;
			}
		}

		private string BuildCompleteTestReportName(string projName)
		{
			return Path.Combine(OutputDir, projName + s_nunitSuffix);
		}

		string BuildCompleteUnitppReportName(string projName)
		{
			return Path.Combine(OutputDir, projName + s_unitppSuffix);
		}

		List<string> CollectAllMatchingFiles()
		{
			var result = new List<string>();
			foreach (var path in Directory.EnumerateFiles(OutputDir, "*" + s_nunitSuffix))
			{
				var target = Path.GetFileName(path);
				result.Add(target.Replace(s_nunitSuffix, ""));
			}
			foreach (var path in Directory.EnumerateFiles(OutputDir, "test*" + s_unitppSuffix))
			{
				var target = Path.GetFileName(path);
				result.Add(target.Replace(s_unitppSuffix, ""));
			}
			foreach (var path in Directory.EnumerateFiles(OutputDir, "*" + s_coverageSuffix))
			{
				var target = Path.GetFileName(path);
				result.Add(target.Replace(s_coverageSuffix, ""));
			}
			return result;
		}

		public void GenerateReport()
		{
			switch (m_reportType)
			{
				case ReportType.NUnit:
				{
					var reportTask = new GenerateNUnitReports();
					reportTask.HostObject = this;
					reportTask.BuildEngine = new StubBuildEngine(m_verbosity);
					reportTask.Log.InitializeLifetimeService();
					reportTask.ReportFiles = DelimitedStringFromListOfProjects();
					reportTask.Execute();
					break;
				}
				case ReportType.DotCover:
				{
					CompileDotCoverReport();
					break;
				}
			}
		}

		private string DelimitedStringFromListOfProjects()
		{
			var result = new StringBuilder();
			foreach (var projName in Projects)
			{
				if (result.Length > 0)
					result.Append(";");
				var completeFileName = BuildCompleteTestReportName(projName);
				if (!File.Exists(completeFileName))
					completeFileName = BuildCompleteUnitppReportName(projName);
				result.Append(completeFileName);
			}
			return result.ToString();
		}

		private void CompileDotCoverReport()
		{
			string mergeCoverageSettings;
			var coverageReportPath = CreateMergeCoverageSettingsFile(out mergeCoverageSettings);
			RunDotCover(mergeCoverageSettings, coverageReportPath);
		}

		private string CreateMergeCoverageSettingsFile(out string mergeCoverageSettings)
		{
			var mergeCoverageXml = new XDocument();
			var root = new XElement("ReportParams");
			mergeCoverageXml.Add(root);
			foreach (var projName in Projects)
			{
				var source = new XElement("Source");
				root.Add(source);
				var coverResultsFile = BuildCoverReportName(projName);
				source.Add(new XText(coverResultsFile));
			}
			var coverageReportPath = Path.Combine(OutputDir, "MergedCoverageReports.xml");
			var output = new XElement("Output");
			root.Add(output);
			output.Add(new XText(coverageReportPath));
			mergeCoverageSettings = Path.Combine(OutputDir, "MergeProjectSettings.xml");
			mergeCoverageXml.Save(mergeCoverageSettings);
			return coverageReportPath;
		}

		private string CreateMergedReportSettingsFile(string mergeCoverageSettings)
		{
			var coverageReportSettings = new XDocument();
			var root = new XElement("ReportParams");
			coverageReportSettings.Add(root);
			var source = new XElement("Source");
			root.Add(source);
			source.Add(new XText(mergeCoverageSettings));
			var coverageReportPath = Path.Combine(OutputDir, "CoverageResults.xml");
			var output = new XElement("Output");
			root.Add(output);
			output.Add(new XText(coverageReportPath));
			var coverageResults = Path.Combine(OutputDir, "MergedProjectReportSettings.xml");
			coverageReportSettings.Save(coverageResults);
			return coverageResults;
		}

		private void RunDotCover(string mergeFile, string resultsFile)
		{
			var process = new Process
			{
				StartInfo =
				{
					FileName = "dotcover",
					Arguments = "merge " + mergeFile,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					//required to allow redirects
					UseShellExecute = false,
					// do not start process in new window
					CreateNoWindow = true,
					WorkingDirectory = Path.GetDirectoryName(mergeFile)
				}
			};
			try
			{
				process.Start();
				process.WaitForExit(30 * 60 * 1000);
			}
			catch (Exception ex)
			{
				throw new Exception(String.Format("Got exception starting {0}", process.StartInfo.FileName), ex);
			}
			Console.WriteLine("Finished first process, beginning the second.");
			var combinedReportFile = CreateMergedReportSettingsFile(resultsFile);
			process = new Process
			{
				StartInfo =
				{
					FileName = "dotcover",
					Arguments = "r " + combinedReportFile,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					//required to allow redirects
					UseShellExecute = false,
					// do not start process in new window
					CreateNoWindow = true,
					WorkingDirectory = Path.GetDirectoryName(mergeFile)
				}
			};
			try
			{
				process.Start();
				process.WaitForExit(30 * 60 * 1000);
			}
			catch (Exception ex)
			{
				throw new Exception(String.Format("Got exception starting {0}", process.StartInfo.FileName), ex);
			}
		}

		private string BuildCoverReportName(string projName)
		{
			return Path.Combine(OutputDir, projName + s_coverageSuffix);
		}
	}

	internal enum ReportType
	{
		NUnit,
		DotCover
	}
}
