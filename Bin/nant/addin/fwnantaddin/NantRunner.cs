// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: NantRunner.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace FwNantAddin2
{
	/// <summary>
	/// Start NAnt
	/// </summary>
	internal class NantRunner
	{
		private string m_ProgramFileName;
		private string m_CommandLine;
		private string m_WorkingDirectory;
		private AddinLogListener m_Log;
		private Hashtable m_htThreadStream = new Hashtable();
		private Thread m_nantThread;
		private bool m_fThreadRunning = false;
		private Process m_process = null;

		internal delegate void BuildStatusHandler(bool fFinished);
		internal event BuildStatusHandler BuildStatusChange;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="NantRunner"/> class.
		/// </summary>
		/// <param name="fileName">The filename and path of NAnt</param>
		/// <param name="commandLine">Command line</param>
		/// <param name="workingDirectory">Working directory</param>
		/// <param name="log">Log listener</param>
		/// <param name="handler">Build status handler</param>
		/// -----------------------------------------------------------------------------------
		internal NantRunner(string fileName, string commandLine, string workingDirectory,
			AddinLogListener log, BuildStatusHandler handler)
		{
			m_ProgramFileName = fileName;
			m_CommandLine = commandLine;
			m_WorkingDirectory = workingDirectory;
			m_Log = log;
			BuildStatusChange += handler;
		}

		/// <summary>
		/// Sets the StartInfo Options and returns a new Process that can be run.
		/// </summary>
		/// <returns>new Process with information about programs to run, etc.</returns>
		protected virtual void PrepareProcess(ref Process process)
		{
			// create process (redirect standard output to temp buffer)
			process.StartInfo.FileName = m_ProgramFileName;
			process.StartInfo.Arguments = m_CommandLine;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			//required to allow redirects
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WorkingDirectory = m_WorkingDirectory;
		}

		//Starts the process and handles errors.
		protected virtual Process StartProcess()
		{
			Process p = new Process();
			PrepareProcess(ref p);
			try
			{
				string msg = string.Format(
					CultureInfo.InvariantCulture,
					"Starting '{1} ({2})' in '{0}'",
					p.StartInfo.WorkingDirectory,
					p.StartInfo.FileName,
					p.StartInfo.Arguments);

				m_Log.WriteLine(msg);

				p.Start();
			}
			catch (Exception e)
			{
				string msg = string.Format("{0} failed to start.", p.StartInfo.FileName);
				m_Log.WriteLine(msg);
				throw e;
			}
			return p;
		}

		public void Run()
		{
			try
			{
				m_nantThread = new Thread(new ThreadStart(StartNant));
				m_nantThread.Name = "NAnt";

				m_nantThread.Start();
				m_fThreadRunning = true;
			}
			catch (Exception e)
			{
				string msg = string.Format("{0} had errors: {1}", m_ProgramFileName, e.Message);
				m_Log.WriteLine(msg);
				throw new Exception(msg, e);
			}
		}

		public int RunSync()
		{
			StartNant();
			return m_process.ExitCode;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Wait for NAnt to exit.
		/// </summary>
		/// <returns>NAnt exit code, i.e. <c>0</c> if no build errors. Returns <c>-1</c>
		/// if NAnt not run.</returns>
		/// ------------------------------------------------------------------------------------
		public int WaitExit()
		{
			// Wait for NAnt to finish
			if (m_nantThread != null)
			{
				m_process.WaitForExit();
				m_nantThread.Join();
				if (m_process != null)
					return m_process.ExitCode;
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="fFinished"></param>
		/// ------------------------------------------------------------------------------------
		protected void OnBuildStatusChange(bool fFinished)
		{
			if (BuildStatusChange != null)
				BuildStatusChange(fFinished);

			if (fFinished)
				m_fThreadRunning = false;
			else
				m_fThreadRunning = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StartNant()
		{
			Thread outputThread = null;
			Thread errorThread = null;
			try
			{
				OnBuildStatusChange(false);

				// Start the external process
				m_process = StartProcess();
				outputThread = new Thread( new ThreadStart( StreamReaderThread_Output ) );
				outputThread.Name = "Output";
				errorThread = new Thread( new ThreadStart( StreamReaderThread_Error ) );
				errorThread.Name = "Error";
				m_htThreadStream[ outputThread.Name ] = m_process.StandardOutput;
				m_htThreadStream[ errorThread.Name ] = m_process.StandardError;

				outputThread.Start();
				errorThread.Start();

				// Wait for the process to terminate
				m_process.WaitForExit();
				// Wait for the threads to terminate
				outputThread.Join();
				errorThread.Join();

				if (m_process.ExitCode != 0)
				{
					string msg = string.Format(
						"External Program Failed: {0} (return code was {1})",
						m_ProgramFileName, m_process.ExitCode);
					m_Log.WriteLine(msg);
				}
			}
			catch (ThreadAbortException tae)
			{
				try
				{
					if (outputThread != null)
						outputThread.Abort();
					if (errorThread != null)
						errorThread.Abort();
					m_Log.WriteLine("Build canceled");
				}
				catch(Exception e)
				{
					string msg = string.Format("{0} had errors: {1}", m_ProgramFileName, e.Message);
					m_Log.WriteLine(msg);
				}

				throw tae;
			}
			catch (Exception e)
			{
				string msg = string.Format("{0} had errors: {1}", m_ProgramFileName, e.Message);
				m_Log.WriteLine(msg);
				throw new Exception(msg, e);
			}
			finally
			{
				m_htThreadStream.Clear();
				m_Log.WriteLine("---------------------- Done ----------------------");

				OnBuildStatusChange(true);
			}
		}

		/// <summary>
		/// Reads from the stream until the external program is ended.
		/// </summary>
		private void StreamReaderThread_Output()
		{
			StreamReader reader = ( StreamReader )m_htThreadStream[ Thread.CurrentThread.Name ];
			while ( true )
			{
				string strLogContents = reader.ReadLine();
				if ( strLogContents == null )
					break;
				// Ensure only one thread writes to the log at any time
				lock ( m_htThreadStream )
				{

					m_Log.WriteLine(strLogContents);

//					if (OutputFile != null && OutputFile != "")
//					{
//						StreamWriter writer = new StreamWriter(OutputFile, OutputAppend);
//						writer.Write(strLogContents);
//						writer.Close();
//					}
				}
			}
		}

		/// <summary>
		/// Reads from the stream until the external program is ended.
		/// </summary>
		private void StreamReaderThread_Error()
		{
			StreamReader reader = ( StreamReader )m_htThreadStream[ Thread.CurrentThread.Name ];
			while ( true )
			{
				string strLogContents = reader.ReadLine();
				if ( strLogContents == null )
					break;
				// Ensure only one thread writes to the log at any time
				lock ( m_htThreadStream )
				{

					m_Log.WriteLine(strLogContents);

//					if (OutputFile != null && OutputFile != "")
//					{
//						StreamWriter writer = new StreamWriter(OutputFile, OutputAppend);
//						writer.Write(strLogContents);
//						writer.Close();
//					}
				}
			}
		}

		internal void Abort()
		{
			try
			{
				if (m_fThreadRunning)
				{
					m_process.Kill();
					m_nantThread.Abort();
				}
			}
			catch(Exception e)
			{
				string msg = string.Format("{0} had errors: {1}", m_ProgramFileName, e.Message);
				m_Log.WriteLine(msg);
			}
		}

		internal bool IsRunning
		{
			get { return m_fThreadRunning; }
		}
	}
}
