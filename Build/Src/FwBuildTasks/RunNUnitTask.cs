using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	/// <summary>
	/// Run NUnit on the specified test code assemblies.
	/// </summary>
	public class RunNUnit : Task
	{
		private StreamReader _stdError;
		private StreamReader _stdOut;
		private int _exitCode = UnknownExitCode;
		private string CommandLine { get; set; }
		/// <summary>
		/// Defines the exit code that will be returned by <see cref="ExitCode" />
		/// if the process could not be started, or did not exit (in time).
		/// </summary>
		public const int UnknownExitCode = -1000;

		public override bool Execute()
		{
			Console.WriteLine("RunNUnit: ExcludedCategories = {0}", ExcludedCategories);
			Console.WriteLine("RunNUnit: UseX86 = {0}", UseX86);
			Console.WriteLine("RunNUnit: Framework = {0}", Framework);
			Console.WriteLine("RunNUnit: Verbose = {0}", Verbose);
			Console.WriteLine("RunNUnit: TimeOut = {0}", TimeOut);
			Console.WriteLine("RunNUnit: TestDll = {0}", TestDll);
			Console.WriteLine("RunNUnit: Log = {0}", Log);
			Console.WriteLine("RunNUnit: HostObject = {0}", HostObject);

			try
			{
				if (TimeOut == 0)
					TimeOut = 15000;
				var bldr = new StringBuilder();
				if (ExcludedCategories != null)
					bldr.AppendFormat(@"-exclude=""{0}"" ", ExcludedCategories);
				bldr.AppendFormat(@"""{0}"" -xml=""{0}-results.xml""", TestDll);
				if (!Verbose)
					bldr.Append(" -nologo");
				if (!string.IsNullOrEmpty(Framework))
					bldr.AppendFormat(" -framework={0}", Framework);
				CommandLine = bldr.ToString();
				InternalExecute();
			}
			catch
			{
				return false;
			}
			return true;
		}

		public string ExcludedCategories { get; set; }
		public bool UseX86 { get; set; }
		public string Framework { get; set; }
		public bool Verbose { get; set; }
		public int TimeOut { get; set; }
		[Required]
		public string TestDll { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the nunit-console executable
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string NUnitName
		{
			get
			{
				if (Environment.OSVersion.Platform != PlatformID.Unix)
					return UseX86 ? "nunit-console-x86.exe" : "nunit-console.exe";
				return UseX86 ? "nunit-console-x86" : "nunit-console";
			}
		}

		private string ProgramFileName
		{
			get
			{
				return NUnitName;
			}
		}


		private void InternalExecute()
		{
			Thread outputThread = null;
			Thread errorThread = null;
			try
			{
				// Start the external process
				Process process = StartProcess();
				outputThread = new Thread(new ThreadStart(StreamReaderThread_Output));
				errorThread = new Thread(new ThreadStart(StreamReaderThread_Error));

				_stdOut = process.StandardOutput;
				_stdError = process.StandardError;

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
					throw new Exception(String.Format("{0} did not finish in {1} msec", ProgramFileName, TimeOut));
				}

				_exitCode = process.ExitCode;

				if (process.ExitCode != 0)
				{
					throw new Exception(String.Format("{0} failed with exit code {1}", ProgramFileName, process.ExitCode));
				}
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

		/// <summary>
		/// Starts the process and handles errors.
		/// </summary>
		/// <returns>The <see cref="Process" /> that was started.</returns>
		private Process StartProcess()
		{
			Process p = new Process();
			PrepareProcess(p);
			try
			{
				string msg = string.Format("Starting {1} in {2} with {3}...",
					p.StartInfo.WorkingDirectory,
					p.StartInfo.FileName,
					p.StartInfo.Arguments);
				p.Start();
				return p;
			}
			catch
			{
				throw new Exception(String.Format("Could not start process for {0}", p.StartInfo.FileName));
			}
		}


		/// <summary>
		/// Updates the <see cref="ProcessStartInfo" /> of the specified
		/// <see cref="Process"/>.
		/// </summary>
		/// <param name="process">The <see cref="Process" /> of which the <see cref="ProcessStartInfo" /> should be updated.</param>
		protected virtual void PrepareProcess(Process process)
		{
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				process.StartInfo.FileName = "mono";
				StringBuilder arguments = new StringBuilder();
				arguments.AppendFormat("\"{0}\" {1}", ProgramFileName, CommandLine);
				process.StartInfo.Arguments = arguments.ToString();
			}
			else
			{
				process.StartInfo.FileName = ProgramFileName;
				process.StartInfo.Arguments = CommandLine;
			}
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			// required to allow redirects and allow environment variables to
			// be set
			process.StartInfo.UseShellExecute = false;
			// do not start process in new window unless we're spawning (if not,
			// the console output of spawned application is not displayed on MS)
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WorkingDirectory = BaseDirectory.FullName;

			// set framework-specific environment variables if executing the
			// external process using the runtime engine of the currently
			// active framework
//            if (executionMode != null)
//			{
//                foreach (EnvironmentVariable environmentVariable in executionMode.Environment.EnvironmentVariables) {
//                    if (environmentVariable.IfDefined && !environmentVariable.UnlessDefined) {
//                        if (environmentVariable.Value == null) {
//                            process.StartInfo.EnvironmentVariables[environmentVariable.VariableName] = "";
//                        } else {
//                            process.StartInfo.EnvironmentVariables[environmentVariable.VariableName] = environmentVariable.Value;
//                        }
//                    }
//                }
//            }
		}

		private bool OutputAppend = false;

		/// <summary>
		/// Will be used to ensure thread-safe operations.
		/// </summary>
		private static object _lockObject = new object();

		/// <summary>
		/// Reads from the stream until the external program is ended.
		/// </summary>
		private void StreamReaderThread_Output()
		{
			StreamReader reader = _stdOut;
			bool doAppend = OutputAppend;

			while (true)
			{
				string logContents = reader.ReadLine();
				if (logContents == null)
				{
					break;
				}

				// ensure only one thread writes to the log at any time
				lock (_lockObject)
				{
					if (Output != null)
					{
						StreamWriter writer = new StreamWriter(Output.FullName, doAppend);
						writer.WriteLine(logContents);
						doAppend = true;
						writer.Close();
					}
					else
					{
						OutputWriter.WriteLine(logContents);
					}
				}
			}

			lock (_lockObject)
			{
				OutputWriter.Flush();
			}
		}

		private DirectoryInfo _basedirectory;

		/// <summary>
		/// Gets the working directory for the application.
		/// </summary>
		/// <value>
		/// The working directory for the application.
		/// </value>
		public virtual DirectoryInfo BaseDirectory {
			get { return _basedirectory; }
			set { _basedirectory = value; }
		}

		private TextWriter _outputWriter;
		private TextWriter _errorWriter;

		/// <summary>
		/// Gets or sets the <see cref="TextWriter" /> to which standard output
		/// messages of the external program will be written.
		/// </summary>
		/// <value>
		/// The <see cref="TextWriter" /> to which standard output messages of
		/// the external program will be written.
		/// </value>
		/// <remarks>
		/// By default, standard output messages wil be written to the build log
		/// with level <see cref="Level.Info" />.
		/// </remarks>
		public virtual TextWriter OutputWriter
		{
			get
			{
				if (_outputWriter == null)
				{
					_outputWriter = new LogWriter(this, LoggerVerbosity.Normal,
						CultureInfo.InvariantCulture);
				}
				return _outputWriter;
			}
			set { _outputWriter = value; }
		}

		/// <summary>
		/// Gets or sets the <see cref="TextWriter" /> to which error output
		/// of the external program will be written.
		/// </summary>
		/// <value>
		/// The <see cref="TextWriter" /> to which error output of the external
		/// program will be written.
		/// </value>
		/// <remarks>
		/// By default, error output wil be written to the build log with level
		/// <see cref="Level.Warning" />.
		/// </remarks>
		public virtual TextWriter ErrorWriter
		{
			get
			{
				if (_errorWriter == null)
				{
					_errorWriter = new LogWriter(this, LoggerVerbosity.Normal,
						CultureInfo.InvariantCulture);
				}
				return _errorWriter;
			}
			set { _errorWriter = value; }
		}

		/// <summary>
		/// Reads from the stream until the external program is ended.
		/// </summary>
		private void StreamReaderThread_Error()
		{
			StreamReader reader = _stdError;
			bool doAppend = OutputAppend;

			while (true)
			{
				string logContents = reader.ReadLine();
				if (logContents == null)
				{
					break;
				}

				// ensure only one thread writes to the log at any time
				lock (_lockObject)
				{
					ErrorWriter.WriteLine(logContents);
					if (Output != null)
					{
						StreamWriter writer = new StreamWriter(Output.FullName, doAppend);
						writer.WriteLine(logContents);
						doAppend = true;
						writer.Close();
					}
				}
			}

			lock (_lockObject) {
				ErrorWriter.Flush();
			}
		}

		private FileInfo _fileinfo;
		/// <summary>
		/// Gets the file to which the standard output should be redirected.
		/// </summary>
		/// <value>
		/// The file to which the standard output should be redirected, or
		/// <see langword="null" /> if the standard output should not be
		/// redirected.
		/// </value>
		/// <remarks>
		/// The default implementation will never allow the standard output
		/// to be redirected to a file.  Deriving classes should override this
		/// property to change this behaviour.
		/// </remarks>
		public FileInfo Output
		{
			get { return _fileinfo; }
			set { _fileinfo = value; }
		}
	}

	public class LogWriter : TextWriter
	{
		private readonly Task _task;
		private readonly LoggerVerbosity _verbosity;
		private string _message = string.Empty;

		public LogWriter(Task task, LoggerVerbosity verbosity, IFormatProvider formatProvider) : base(formatProvider)
		{
			_task = task;
			_verbosity = verbosity;
		}

		#region Override implementation of TextWriter

		/// <summary>
		/// Gets the <see cref="Encoding" /> in which the output is written.
		/// </summary>
		/// <value>
		/// The <see cref="LogWriter" /> always writes output in UTF8
		/// encoding.
		/// </value>
		public override Encoding Encoding
		{
			get { return Encoding.UTF8; }
		}

		/// <summary>
		/// Writes a character array to the buffer.
		/// </summary>
		/// <param name="chars">The character array to write to the text stream.</param>
		public override void Write(char[] chars)
		{
			Write(new string(chars, 0, chars.Length -1));
		}

		/// <summary>
		/// Writes a string to the buffer.
		/// </summary>
		/// <param name="value"></param>
		public override void Write(string value)
		{
			_message += value;
		}

		/// <summary>
		/// Writes an empty string to the logging infrastructure.
		/// </summary>
		public override void WriteLine()
		{
			WriteLine(string.Empty);
		}


		/// <summary>
		/// Writes a string to the logging infrastructure.
		/// </summary>
		/// <param name="value">The string to write. If <paramref name="value" /> is a null reference, only the line termination characters are written.</param>
		public override void WriteLine(string value)
		{
			_message += value;
			_task.Log.LogMessage(_message);
			_message = string.Empty;
		}

		/// <summary>
		/// Writes out a formatted string using the same semantics as
		/// <see cref="M:string.Format(string, object[])" />.
		/// </summary>
		/// <param name="line">The formatting string.</param>
		/// <param name="args">The object array to write into format string.</param>
		public override void WriteLine(string line, params object[] args)
		{
			_message += string.Format(CultureInfo.InvariantCulture, line, args);
			_task.Log.LogMessage(_message);
			_message = string.Empty;
		}

		/// <summary>
		/// Causes any buffered data to be written to the logging infrastructure.
		/// </summary>
		public override void Flush()
		{
			if (_message.Length != 0)
			{
				_task.Log.LogMessage(_message);
				_message = string.Empty;
			}
		}

		/// <summary>
		/// Closes the current writer and releases any system resources
		/// associated with the writer.
		/// </summary>
		public override void Close()
		{
			Flush();
			base.Close();
		}

		#endregion Override implementation of TextWriter
	}
}
