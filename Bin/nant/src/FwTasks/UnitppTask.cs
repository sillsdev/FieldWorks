// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UnitppTask.cs
// Responsibility: EberhardB
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using NAnt.Core;
using NAnt.Core.Attributes;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Poor man's solution to get the output of unit++ tests in a xUnit XML file so that CI
	/// server can report failed tests correctly.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TaskName("unitpp")]
	public class UnitppTask: Task
	{
		private StreamReader m_StdError;
		private StreamReader m_StdOut;
		private TextWriter m_OutputWriter;
		private TextWriter m_ErrorWriter;
		private MemoryStream m_MemoryStream;
		private TextWriter m_MemoryWriter;
		private XmlDocument m_Doc;
		private XmlElement m_Message;
		private StringBuilder m_MsgBldr;

		/// <summary>
		/// Will be used to ensure thread-safe operations.
		/// </summary>
		private static readonly object LockObject = new object();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name and path of the unit++ executable
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("path")]
		public string FixturePath { get; set; }

		/// <summary>
		/// The maximum amount of time the application is allowed to execute,
		/// expressed in milliseconds.  Defaults to no time-out.
		/// </summary>
		[TaskAttribute("timeout")]
		[Int32Validator]
		public int TimeOut { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UnitppTask"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public UnitppTask()
		{
			TimeOut = Int32.MaxValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Starts the process and handles errors.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual Process StartProcess()
		{
			var process = new Process
							{
								StartInfo =
									{
										FileName = FixturePath,
										//Arguments = CommandLine,
										Arguments = "-v -l",
										RedirectStandardOutput = true,
										RedirectStandardError = true,
										//required to allow redirects
										UseShellExecute = false,
										// do not start process in new window
										CreateNoWindow = true,
										WorkingDirectory = Path.GetFullPath(Path.GetDirectoryName(FixturePath))
									}
							};
			try
			{
				var msg = string.Format("Starting program: {1} ({2}) in {0}",
					process.StartInfo.WorkingDirectory,
					process.StartInfo.FileName,
					process.StartInfo.Arguments);

				Log(Level.Verbose, msg);

				process.Start();
				return process;
			}
			catch (Exception ex)
			{
				throw new BuildException(string.Format("Got exception starting {0}",
					process.StartInfo.FileName), Location, ex);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the task.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			Log(Level.Info, "Running {0}", Path.GetFileName(FixturePath));

			Thread outputThread = null;
			Thread errorThread = null;

			try
			{
				// Start the external process
				var process = StartProcess();
				outputThread = new Thread(StreamReaderThread_Output);
				errorThread = new Thread(StreamReaderThread_Error);

				m_StdOut = process.StandardOutput;
				m_StdError = process.StandardError;

				outputThread.Start();
				errorThread.Start();

				// Wait for the process to terminate
				process.WaitForExit(TimeOut);

				// Wait for the threads to terminate
				outputThread.Join(2000);
				errorThread.Join(2000);

				if (!process.HasExited)
				{
					try
					{
						process.Kill();
					}
					catch
					{
						// ignore possible exceptions that are thrown when the
						// process is terminated
					}

					throw new BuildException(
						String.Format("{0} did not finish in {1} milliseconds.",
						FixturePath,
						TimeOut),
						Location);
				}

				MemoryStream.Position = 0;
				WriteXmlResultFile();
				MemoryWriter.Close();
				MemoryStream.Close();

				if (process.ExitCode != 0)
				{
					throw new BuildException(
						String.Format("{0} returned with exit code {1}",
						FixturePath,
						process.ExitCode),
						Location);
				}
			}
			catch (BuildException e)
			{
				if (FailOnError)
				{
					throw;
				}
				else
				{
					Log(Level.Error, e.Message);
				}
			}
			catch (Exception e)
			{
				throw new BuildException(
					string.Format(CultureInfo.InvariantCulture,
					"{0}: {1} had errors. ", GetType(), FixturePath),
					Location,
					e);
			}
			finally
			{
				// ensure outputThread is always aborted
				if (outputThread != null && outputThread.IsAlive)
				{
					outputThread.Abort();
				}
				// ensure errorThread is always aborted
				if (errorThread != null && errorThread.IsAlive)
				{
					errorThread.Abort();
				}
			}
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
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes the output.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void WriteXmlResultFile()
		{
			Log(Level.Verbose, "Processing test results for writing XML result file");
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
					var logLine = false; // wether to output line on Console
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
						if (!Verbose)
							Log(Level.Info, line);
						break;
					}
					else
					{
						logLine = true;
						if (m_MsgBldr != null)
							m_MsgBldr.AppendLine(line);
					}
					if (logLine && !Verbose)
						Log(Level.Info, line);
				}
			}
			FinishTestCase();
			root.SetAttribute("total", (ok + fail + error).ToString());
			root.SetAttribute("failures", fail.ToString());
			root.SetAttribute("errors", error.ToString());
			root.SetAttribute("not-run", notRun.ToString());
			Log(Level.Verbose, "Writing XML result file: {0}", ResultFileName);
			m_Doc.Save(ResultFileName);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the <see cref="TextWriter"/> to which standard output
		/// messages of the external program will be written.
		/// </summary>
		/// <remarks>
		/// By default, standard output messages wil be written to the build log
		/// with level <see cref="Level.Info"/>.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual TextWriter OutputWriter
		{
			get
			{
				if (m_OutputWriter == null)
				{
					m_OutputWriter = new LogWriter(this, Level.Info,
						CultureInfo.InvariantCulture);
				}
				return TextWriter.Synchronized(m_OutputWriter);
			}
			set { m_OutputWriter = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the <see cref="TextWriter"/> to which error output
		/// of the external program will be written.
		/// </summary>
		/// <value>The error writer.</value>
		/// <remarks>
		/// By default, error output wil be written to the build log with level
		/// <see cref="Level.Warning"/>.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual TextWriter ErrorWriter
		{
			get
			{
				if (m_ErrorWriter == null)
				{
					m_ErrorWriter = new LogWriter(this, Level.Warning,
						CultureInfo.InvariantCulture);
				}
				return TextWriter.Synchronized(m_ErrorWriter);
			}
			set { m_ErrorWriter = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the memory stream.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private MemoryStream MemoryStream
		{
			get
			{
				if (m_MemoryStream == null)
					m_MemoryStream = new MemoryStream();
				return m_MemoryStream;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the memory writer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private TextWriter MemoryWriter
		{
			get
			{
				if (m_MemoryWriter == null)
					m_MemoryWriter = new StreamWriter(MemoryStream);
				return TextWriter.Synchronized(m_MemoryWriter);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads from the stream until the external program is ended.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StreamReaderThread_Output()
		{
			try
			{
				var reader = m_StdOut;

				while (true)
				{
					var logContents = reader.ReadLine();
					if (logContents == null)
					{
						break;
					}

					// ensure only one thread writes to the log at any time
					lock (LockObject)
					{
						if (Verbose)
							OutputWriter.WriteLine(logContents);
						MemoryWriter.WriteLine(logContents);
					}
				}

				lock (LockObject)
				{
					OutputWriter.Flush();
					MemoryWriter.Flush();
				}
			}
			catch (Exception)
			{
				// just ignore any errors
			}
		}

		/// <summary>
		/// Reads from the stream until the external program is ended.
		/// </summary>
		private void StreamReaderThread_Error()
		{
			try
			{
				var reader = m_StdError;

				while (true)
				{
					var logContents = reader.ReadLine();
					if (logContents == null)
					{
						break;
					}

					// ensure only one thread writes to the log at any time
					lock (LockObject)
					{
						if (Verbose)
							ErrorWriter.WriteLine(logContents);
						MemoryWriter.WriteLine(logContents);
					}
				}
				lock (LockObject)
				{
					ErrorWriter.Flush();
					MemoryWriter.Flush();
				}
			}
			catch (Exception)
			{
				// just ignore any errors
			}
		}
	}
}
