using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	public class Unitpp : TestTask
	{
		private XmlDocument m_Doc;
		private XmlElement m_Message;
		private StringBuilder m_MsgBldr;

		/// <summary>
		/// Constructor.
		/// </summary>
		public Unitpp()
		{
			Timeout = Int32.MaxValue;
		}


		protected override string ProgramName()
		{
			return FixturePath;
		}

		protected override string ProgramArguments()
		{
			return "-v -l";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the output into an XML file similar to those produced by NUnit.  Some
		/// lines are also logged in the normal msbuild fashion.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ProcessOutput()
		{
			Log.LogMessage(MessageImportance.Low, "Processing test results for writing XML result file");
			m_Doc = new XmlDocument();
			var root = m_Doc.CreateElement("test-results");
			root.SetAttribute("name", FixturePath);
			m_Doc.AppendChild(root);
			var outerSuite = m_Doc.CreateElement("test-suite");
			outerSuite.SetAttribute("name", FixturePath);
			root.AppendChild(outerSuite);
			var outerResults = m_Doc.CreateElement("results");
			outerSuite.AppendChild(outerResults);
			var ok = 0;
			var fail = 0;
			var error = 0;
			var notRun = 99999; // Indicate a problem if unsuccessful finish
			using (var reader = new StreamReader(MemoryStream))
			{
				XmlElement results = null;
				while (!reader.EndOfStream)
				{
					var logLine = false; // whether to output line on Console
					var line = reader.ReadLine();
					if (line.StartsWith("****** top"))
					{
						// This is the first line, just ignore
					}
					else if (line.StartsWith("****** "))
					{
						// Name of the suite
						var regex = new Regex(@"^\*+ (?<name>[^*]+)");
						var match = regex.Match(line);

						FinishTestCase();
						var suite = m_Doc.CreateElement("test-suite");
						suite.SetAttribute("name", match.Groups["name"].Value.TrimEnd());
						outerResults.AppendChild(suite);
						results = m_Doc.CreateElement("results");
						suite.AppendChild(results);
					}
					else if (line.StartsWith("OK:"))
					{
						// Successful test - grab and report name
						FinishTestCase();
						var name = line.Substring(4); // skip "OK: "
						CreateTestCaseNode(m_Doc, results, name, true);
						++ok;
					}
					else if (line.StartsWith("FAIL:"))
					{
						// Failed test - grab and report name
						FinishTestCase();
						logLine = true;
						var name = line.Substring(6); // skip "FAIL: "
						ReportFailedTestCase(results, name);
						++fail;
					}
					else if (line.Contains(":FAIL:"))
					{
						// Failed test reported with filename and line
						FinishTestCase();
						logLine = true;
						var regex = new Regex("(?<filename>([A-Za-z]:)?[^:]+):(?<lineno>[0-9]+):FAIL: (?<name>[^:]+):(?<message>.+)");
						var match = regex.Match(line);
						var failureNode = ReportFailedTestCase(results, match.Groups["name"].Value.TrimEnd());
						++fail;
// ReSharper disable PossibleNullReferenceException
						m_MsgBldr.AppendLine(match.Groups["message"].Value.TrimEnd());
// ReSharper restore PossibleNullReferenceException
						m_MsgBldr.AppendLine();
						FinishTestCase();
						AppendStackTrace(match, failureNode);
					}
					else if (line.StartsWith("ERROR:"))
					{
						// Failed test - grab and report name
						FinishTestCase();
						logLine = true;
						var name = line.Substring(7); // skip "ERROR: "
						ReportFailedTestCase(results, name);
						++error;
					}
					else if (line.Contains(":ERROR:"))
					{
						// Failed test reported with filename and line
						FinishTestCase();
						logLine = true;
						var regex = new Regex("(?<filename>([A-Za-z]:)?[^:]+):(?<lineno>[0-9]+):ERROR: (?<name>[^:]+):(?<message>.+)");
						var match = regex.Match(line);
						var errorNode = ReportFailedTestCase(results, match.Groups["name"].Value.TrimEnd());
						++error;
// ReSharper disable PossibleNullReferenceException
						m_MsgBldr.AppendLine(match.Groups["message"].Value.TrimEnd());
// ReSharper restore PossibleNullReferenceException
						m_MsgBldr.AppendLine();
						FinishTestCase();
						AppendStackTrace(match, errorNode);
					}
					else if (line.StartsWith("Tests [Ok-Fail-Error]:"))
					{
						// Last line
						FinishTestCase();
						var regex = new Regex(@"Tests \[Ok-Fail-Error\]: \[(?<ok>\d+)-(?<fail>\d+)-(?<error>\d+)\]");
						var match = regex.Match(line);
						ok = Int32.Parse(match.Groups["ok"].Value);
						fail = Int32.Parse(match.Groups["fail"].Value);
						error = Int32.Parse(match.Groups["error"].Value);
						notRun = 0; // we don't keep track of ignored tests
						Log.LogMessage(MessageImportance.Normal, line);
						break;
					}
					else
					{
						logLine = true;
						if (m_MsgBldr != null)
							m_MsgBldr.AppendLine(line);
					}
					if (logLine)
						Log.LogMessage(MessageImportance.Normal, line);
					else
						Log.LogMessage(MessageImportance.Low, line);
				}
			}
			FinishTestCase();
			root.SetAttribute("total", (ok + fail + error).ToString());
			root.SetAttribute("failures", fail.ToString());
			root.SetAttribute("errors", error.ToString());
			root.SetAttribute("not-run", notRun.ToString());
			Log.LogMessage(MessageImportance.Low, "Writing XML result file: {0}", ResultFileName);
			m_Doc.Save(ResultFileName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the result file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string ResultFileName
		{
			get
			{
				return Path.Combine(Path.GetDirectoryName(FixturePath),
									Path.GetFileName(FixturePath) + "-results.xml");
			}
		}

		private void AppendStackTrace(Match match, XmlNode errorNode)
		{
			var stackTrace = m_Doc.CreateElement("stack-trace");
			errorNode.AppendChild(stackTrace);
			stackTrace.AppendChild(m_Doc.CreateCDataSection(string.Format("{0}:{1}",
				match.Groups["filename"].Value.TrimEnd(),
				match.Groups["lineno"].Value.TrimEnd())));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reports a failed test case.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private XmlElement ReportFailedTestCase(XmlNode results, string name)
		{
			var testCase = CreateTestCaseNode(m_Doc, results, name, false);
			var failure = m_Doc.CreateElement("failure");
			testCase.AppendChild(failure);
			m_Message = m_Doc.CreateElement("message");
			failure.AppendChild(m_Message);
			m_MsgBldr = new StringBuilder();
			return failure;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finishes the test case.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void FinishTestCase()
		{
			if (m_Message == null)
				return;

			m_Message.AppendChild(m_Doc.CreateCDataSection(m_MsgBldr.ToString()));

			m_Message = null;
			m_MsgBldr = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a test case node.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static XmlElement CreateTestCaseNode(XmlDocument xmlDoc, XmlNode results,
			string name, bool success)
		{
			var testCase = xmlDoc.CreateElement("test-case");
			testCase.SetAttribute("name", name);
			testCase.SetAttribute("success", success.ToString());
			testCase.SetAttribute("time", "0");
			testCase.SetAttribute("asserts", "0");
			results.AppendChild(testCase);
			return testCase;
		}
	}
}
