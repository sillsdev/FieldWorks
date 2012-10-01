using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	public class GenerateNUnitReports : Task
	{
		private int m_totalSucceed;
		private int m_totalIgnore;
		private int m_totalFailure;
		private double m_totalElapsedTestTime;
		private int m_totalTestProjects;
		private List<Tuple<string, int>> m_projectFailures;

		private TaskLoggingHelper m_log;

		public override bool Execute()
		{
			//Debug.Fail("Attach debugger here."); // Attach debugger to MSBuild process here.
			m_log = Log;
			var reportFiles = ReportFiles.Split(';');
			if (reportFiles.Length == 0)
				return true; // Shouldn't happen...

			GenerateReports(reportFiles);
			return m_totalFailure == 0;
		}

		[Required]
		public string ReportFiles { get; set; }

		private void GenerateReports(IEnumerable<string> reportFiles)
		{
			m_totalElapsedTestTime = 0.0;
			m_projectFailures = new List<Tuple<string, int>>();
			foreach (string fileName in reportFiles)
			{
				GenerateReportFor(fileName);
				m_totalTestProjects++;
			}
			GenerateSummaryReport();
		}

		private void GenerateReportFor(string fileName)
		{
			// Read XML file
			var doc = new XmlDocument();
			doc.Load(fileName);

			// Report header for one test project
			var shortProjName = GetShortProjectName(fileName);
			LogProjectHeader(shortProjName);

			// Grab test-results node and parse totals
			var resultsNode = doc.SelectSingleNode("test-results");
			var secsToRun = resultsNode.SelectSingleNode("test-suite").Attributes["time"].Value;
			var projFails = Convert.ToInt32(resultsNode.Attributes["failures"].Value);
			projFails += Convert.ToInt32(resultsNode.Attributes["errors"].Value);
			var projIgnores = Convert.ToInt32(resultsNode.Attributes["skipped"].Value);
			projIgnores += Convert.ToInt32(resultsNode.Attributes["ignored"].Value);
			var projPasses = Convert.ToInt32(resultsNode.Attributes["total"].Value) - projFails - projIgnores;
			m_totalFailure += projFails;
			m_totalIgnore += projIgnores;
			m_totalSucceed += projPasses;
			m_totalElapsedTestTime += Convert.ToDouble(secsToRun);
			if (projFails > 0)
				m_projectFailures.Add(new Tuple<string, int>(shortProjName, projFails));

			ImportantMessage("Failures: {0}    Ignored: {1}    Passed: {2}", projFails, projIgnores, projPasses);
			NormalMessage("     Elapsed time: {0}", secsToRun);
			ImportantMessage("*************************************************");

			// Parse out TestCases
			var cases = resultsNode.SelectNodes("//test-case"); // does this get all of them?
			foreach (XmlNode testCaseNode in cases)
			{
				var testResult = testCaseNode.Attributes["result"].Value;
				var completeTestCaseName = testCaseNode.Attributes["name"].Value;
				var testCaseName = completeTestCaseName.Split('.').Last();
				// Log Successes as importance Low, Ignored as Medium and Failures as High
				switch (testResult)
				{
					case "Success":
						var secsToPass = testCaseNode.Attributes["time"].Value;
						VerboseMessage("{0} passed in {1} secs.", testCaseName, secsToPass);
						break;
					case "Skipped": // two names for same case
					case "Ignored":
						var reason = ParseCdataNode(testCaseNode.SelectSingleNode("reason/message"));
						NormalMessage("Ignored testcase {0}.", testCaseName);
						NormalMessage("     Reason: {0}", reason);
						break;
					case "Failure": // two names for similar case
					case "Error":
						GenerateFailureReport(testCaseName, testCaseNode);
						break;
					default:
						throw new ApplicationException("Unimplemented NUnit testresult");
				}
			}
		}

		private void GenerateFailureReport(string testCaseName, XmlNode testCaseNode)
		{
			var secsToFail = testCaseNode.Attributes["time"].Value;
			string message = ParseCdataNode(testCaseNode.SelectSingleNode("failure/message"));
			string stack = ParseCdataNode(testCaseNode.SelectSingleNode("failure/stack-trace"));
			ImportantMessage("*************************************************");
			ImportantMessage("{0} FAILED in {1} secs.", testCaseName, secsToFail);
			ImportantMessage("*************************************************");
			ImportantMessage(message);
			ImportantMessage(stack);
		}

		private string ParseCdataNode(XmlNode messageNode)
		{
			if (messageNode == null)
				return string.Empty;
			return messageNode.InnerText;
		}

		private string GetShortProjectName(string fileName)
		{
			// filename should be of the form "Path/project.dll-nunit-output.xml"
			// we want the "project.dll" part
			return Path.GetFileNameWithoutExtension(fileName).Split('-').First();
		}

		private void LogProjectHeader(string projName)
		{
			ImportantMessage(" ");
			ImportantMessage("*************************************************");
			ImportantMessage("NUnit report for {0}:", projName);
			ImportantMessage("*************************************************");
		}

		private void GenerateSummaryReport()
		{
			ImportantMessage(" ");
			ImportantMessage("*********************************************************");
			ImportantMessage("NUnit Summary report:");
			ImportantMessage("{0} test project(s)   Passed: {1}  Ignored: {2}  Failures: {3} in {4} seconds.",
				m_totalTestProjects, m_totalSucceed, m_totalIgnore, m_totalFailure, m_totalElapsedTestTime);
			foreach (var failedProject in m_projectFailures)
			{
				ImportantMessage("Project {0}", failedProject.Item1);
				ImportantMessage("    had {0} test failures.", failedProject.Item2);
			}
			ImportantMessage("*********************************************************");
		}

		private void ImportantMessage(string msg, params object[] msgParams)
		{
			m_log.LogMessage(MessageImportance.High, msg, msgParams);
		}

		private void NormalMessage(string msg, params object[] msgParams)
		{
			m_log.LogMessage(msg, msgParams);
		}

		private void VerboseMessage(string msg, params object[] msgParams)
		{
			m_log.LogMessage(MessageImportance.Low, msg, msgParams);
		}
	}
}
