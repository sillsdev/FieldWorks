using System;
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
		protected StreamReader m_StdError;
		protected StreamReader m_StdOut;
		protected MemoryStream m_MemoryStream;
		protected TextWriter m_MemoryWriter;

		/// <summary>
		/// Used to ensure thread-safe operations.
		/// </summary>
		private static readonly object LockObject = new object();

		public TestTask()
		{
		}

		/// <summary>
		/// Gets or sets the full path to the unit++ executable (test program).
		/// </summary>
		[Required]
		public string FixturePath { get; set; }

		/// <summary>
		/// Gets or sets the maximum amount of time the test is allowed to execute,
		/// expressed in milliseconds.  The default is essentially no time-out.
		/// </summary>
		public int Timeout { get; set; }

		public override bool Execute()
		{
			if (Timeout == Int32.MaxValue)
				Log.LogMessage(MessageImportance.Normal, "Running {0}", Path.GetFileName(FixturePath));
			else
				Log.LogMessage(MessageImportance.Normal, "Running {0} (timeout = {1} seconds)", Path.GetFileName(FixturePath), ((double)Timeout/1000.0).ToString("F1"));

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

				bool fTimedOut = false;
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
					fTimedOut = true;
				}

				TimeSpan delta = DateTime.Now - dtStart;
				Log.LogMessage(MessageImportance.Normal, "Total time for running {0} = {1}", Path.GetFileName(FixturePath), delta);

				try
				{
					MemoryStream.Position = 0;
					ProcessOutput(fTimedOut, delta);
					MemoryWriter.Close();
					MemoryStream.Close();
				}
				catch
				{
				}

				if (fTimedOut)
				{
					Log.LogError("The tests in {0} did not finish in {1} milliseconds.", FixturePath, Timeout);
					return false;
				}
				if (process.ExitCode != 0)
				{
					Log.LogError("{0} returned with exit code {1}", FixturePath, process.ExitCode);
					return false;
				}
			}
			catch (Exception e)
			{
				Log.LogErrorFromException(e, true);
				return false;
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
			return true;
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
							WorkingDirectory = Path.GetFullPath(Path.GetDirectoryName(FixturePath))
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

		/// <summary>
		/// Gets the memory stream, creating it if necessary.
		/// </summary>
		protected MemoryStream MemoryStream
		{
			get
			{
				if (m_MemoryStream == null)
					m_MemoryStream = new MemoryStream();
				return m_MemoryStream;
			}
		}

		/// <summary>
		/// Gets the memory writer, creating it if necessary.
		/// </summary>
		protected TextWriter MemoryWriter
		{
			get
			{
				if (m_MemoryWriter == null)
					m_MemoryWriter = new StreamWriter(MemoryStream);
				return TextWriter.Synchronized(m_MemoryWriter);
			}
		}

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
						MemoryWriter.WriteLine(logContents);
					}
				}

				lock (LockObject)
				{
					MemoryWriter.Flush();
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
						MemoryWriter.WriteLine(logContents);
					}
				}
				lock (LockObject)
				{
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
