using System;
using System.Collections.Generic;
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
			var coveredAssemblies = 0;
			var coveredStatements = 0;
			var totalStatements = 0;
			var coverageMap = new SortedDictionary<string, string>();
			foreach (var projName in Projects)
			{
				var coverResultsFile = BuildCoverReportName(projName);
				var doc = XDocument.Load(coverResultsFile);
				var assemblyElement = doc.Root.Descendants("Assembly").FirstOrDefault();
				if (assemblyElement == null) // some results files have no assembly elements in them
					continue;
				coveredStatements += int.Parse(assemblyElement.Attribute("CoveredStatements").Value);
				totalStatements += int.Parse(assemblyElement.Attribute("TotalStatements").Value);
				var assemblyKey = string.Format("{0, 2}{1}", int.Parse(assemblyElement.Attribute("CoveragePercent").Value),
					assemblyElement.Attribute("Name").Value);
				coverageMap[assemblyKey] = assemblyElement.ToString();
				++coveredAssemblies;
			}
			var commentLine = string.Format("<!-- Coverage stats for {0} Assemblies -->", coveredAssemblies);
			var fullReport = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + Environment.NewLine + commentLine + Environment.NewLine
				+ string.Format("<Root CoveredStatements=\"{0}\" TotalStatements=\"{1}\" CoveragePercent=\"{2}\" ReportType=\"Xml\" DotCoverVersion=\"10.0.1\">",
					coveredStatements, totalStatements, (int)(coveredStatements * 100.0 / totalStatements)) + Environment.NewLine + GenerateSortedAssemblyReports(coverageMap) + Environment.NewLine + "</Root>";
			var fullDoc = XDocument.Parse(fullReport);
			var coverageReportPath = Path.Combine(OutputDir, "CoverageReport.xml");
			fullDoc.Save(coverageReportPath);
			Console.WriteLine("Coverage report generated to: {0}", coverageReportPath);
		}

		private string GenerateSortedAssemblyReports(SortedDictionary<string, string> coverageMap)
		{
			var fullReport = "";
			foreach (var assemblyReport in coverageMap.Values)
			{
				fullReport += assemblyReport;
				fullReport += Environment.NewLine;
			}
			return fullReport;
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
