// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	/// <summary>
	/// Base class for both our Unitpp and NUnit tasks.
	/// </summary>
	/// <remarks>
	/// The NUnit task borrowed (and fixed slightly) from the network did not properly handle
	/// timeouts.  In fact, anything based on ToolTask (at least in Mono 10.4) didn't handle
	/// timeouts properly in my testing.  This code does handle timeouts properly.
	/// </remarks>
	public abstract class TestTask : Task
	{
		private StreamReader m_StdError;
		private StreamReader m_StdOut;
		protected List<string> m_TestLog = new List<string>();

		/// <summary>
		/// Constructor
		/// </summary>
		public TestTask()
		{
			// more than 24 days should be a high enough value as default :-)
			Timeout = Int32.MaxValue;
			FudgeFactor = 1;
		}

		/// <summary>
		/// Used to ensure thread-safe operations.
		/// </summary>
		private static readonly object LockObject = new object();

		/// <summary>
		/// Gets or sets the maximum amount of time the test is allowed to execute,
		/// expressed in milliseconds.  The default is essentially no time-out.
		/// </summary>
		public int Timeout { get; set; }

		/// <summary>
		/// Factor the timeout will be multiplied by.
		/// </summary>
		public int FudgeFactor { get; set; }

		/// <summary>
		/// Contains the names of failed test suites
		/// </summary>
		[Output]
		public ITaskItem[] FailedSuites { get; protected set; }

		/// <summary>
		/// Contains the names of test suites that got a timeout or that crashed
		/// </summary>
		[Output]
		public ITaskItem[] AbandondedSuites { get; protected set; }

		public override bool Execute()
		{
			if (FudgeFactor >= 0 && Timeout < Int32.MaxValue)
				Timeout *= FudgeFactor;

			bool retVal = true;
			if (Timeout == Int32.MaxValue)
				Log.LogMessage(MessageImportance.Normal, "Running {0}", TestProgramName);
			else
				Log.LogMessage(MessageImportance.Normal, "Running {0} (timeout = {1} seconds)", TestProgramName, ((double)Timeout/1000.0).ToString("F1"));

			Thread outputThread = null;
			Thread errorThread = null;

			var dtStart = DateTime.Now;
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
				process.WaitForExit(Timeout);

				// Wait for the threads to terminate
				outputThread.Join(2000);
				errorThread.Join(2000);

				bool fTimedOut = !process.WaitForExit(0);	// returns false immediately if still running.
				if (fTimedOut)
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
				}

				TimeSpan delta = DateTime.Now - dtStart;
				Log.LogMessage(MessageImportance.Normal, "Total time for running {0} = {1}", TestProgramName, delta);

				try
				{
					ProcessOutput(fTimedOut, delta);
				}
				catch //(Exception e)
				{
					//Console.WriteLine("CAUGHT EXCEPTION: {0}", e.Message);
					//Console.WriteLine("STACK: {0}", e.StackTrace);
				}

				// If the test timed out, it was killed and its ExitCode is not available.
				// So check for a timeout first.
				if (fTimedOut)
				{
					Log.LogError("The tests in {0} did not finish in {1} milliseconds.",
						TestProgramName, Timeout);
					FailedSuites = FailedSuiteNames;
					retVal = false;
				}
				else if (process.ExitCode != 0)
				{
					Log.LogWarning("{0} returned with exit code {1}", TestProgramName,
						process.ExitCode);
					FailedSuites = FailedSuiteNames;
					// Return true in this case - at least NUnit returns non-zero exit code when
					// a test fails, but we don't want to stop the build.
				}
			}
			catch (Exception e)
			{
				Log.LogErrorFromException(e, true);
				retVal = false;
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
			// Return retVal instead of !Log.HasLoggedErrors - if we get test failures we log
			// those but don't want the build to fail. However, if we get an exception from the
			// test or if we get a timeout we want to fail the build.
			return retVal;
		}

		/// <summary>
		/// Starts the process and handles errors.
		/// </summary>
		protected virtual Process StartProcess()
		{
			var process = new Process
				{
					StartInfo =
						{
							FileName = ProgramName(),
							Arguments = ProgramArguments(),
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
				var msg = string.Format("Starting program: {1} ({2}) in {0}",
					process.StartInfo.WorkingDirectory,
					process.StartInfo.FileName,
					process.StartInfo.Arguments);

				Log.LogMessage(MessageImportance.Low, msg);

				process.Start();
				return process;
			}
			catch (Exception ex)
			{
				throw new Exception(String.Format("Got exception starting {0}", process.StartInfo.FileName), ex);
			}
		}

		protected abstract string ProgramName();

		protected abstract string ProgramArguments();

		protected abstract void ProcessOutput(bool fTimedOut, TimeSpan delta);

		protected abstract string GetWorkingDirectory();

		protected abstract string TestProgramName { get; }

		protected abstract ITaskItem[] FailedSuiteNames { get; }

		/// <summary>
		/// Reads from the standard output stream until the external program is ended.
		/// </summary>
		protected void StreamReaderThread_Output()
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
						m_TestLog.Add(logContents);
					}
				}
			}
			catch (Exception)
			{
				// just ignore any errors
			}
		}

		/// <summary>
		/// Reads from the standard error stream until the external program is ended.
		/// </summary>
		protected void StreamReaderThread_Error()
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
						m_TestLog.Add(logContents);
					}
				}
			}
			catch (Exception)
			{
				// just ignore any errors
			}
		}
	}
}
