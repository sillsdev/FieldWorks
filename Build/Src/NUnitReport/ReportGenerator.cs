using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FwBuildTasks;
using Microsoft.Build.Framework;

namespace NUnitReport
{
	internal class ReportGenerator : ITaskHost
	{
		private const string s_output = "Output";
		private const string s_debug = "Debug";
		private const string s_fileSuffix = ".dll-nunit-output.xml";
		private const string s_unitppSuffix = "-results.xml";
		private LoggerVerbosity m_verbosity = LoggerVerbosity.Minimal;

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
				// Check project name
				var completeFileName = BuildCompleteTestReportName(argument);
				if (!File.Exists(completeFileName))
				{
					completeFileName = BuildCompleteUnitppReportName(argument);
					if (!File.Exists(completeFileName))
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
			return Path.Combine(OutputDir, projName + s_fileSuffix);
		}

		string BuildCompleteUnitppReportName(string projName)
		{
			return Path.Combine(OutputDir, projName + s_unitppSuffix);
		}

		List<string> CollectAllMatchingFiles()
		{
			var result = new List<string>();
			foreach (var path in Directory.EnumerateFiles(OutputDir, "*" + s_fileSuffix))
			{
				var target = Path.GetFileName(path);
				result.Add(target.Replace(s_fileSuffix, ""));
			}
			foreach (var path in Directory.EnumerateFiles(OutputDir, "test*" + s_unitppSuffix))
			{
				var target = Path.GetFileName(path);
				result.Add(target.Replace(s_unitppSuffix, ""));
			}
			return result;
		}

		public void GenerateReport()
		{
			var reportTask = new GenerateNUnitReports();
			reportTask.HostObject = this;
			reportTask.BuildEngine = new StubBuildEngine(m_verbosity);
			reportTask.Log.InitializeLifetimeService();
			reportTask.ReportFiles = DelimitedStringFromListOfProjects();
			reportTask.Execute();
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
	}
}
