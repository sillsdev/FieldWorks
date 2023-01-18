// Copyright (c) 2010-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Globalization;
using System.IO;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using SIL.Program;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for LogFile.
	/// </summary>
	/// 	/// ----------------------------------------------------------------------------------------
	public static class LogFile
	{
		#region Static methods to interact with the logging
		///<summary>
		///</summary>
		private static LogFileImpl GetLogFile()
		{
			return SingletonsContainer.Get<LogFileImpl>();
		}

		///<summary>
		///</summary>
		///<param name="line"></param>
		public static void AddErrorLine(string line)
		{
			GetLogFile().AddLineX(line, true);
		}

		///<summary>
		///</summary>
		///<param name="line"></param>
		public static void AddLine(string line)
		{
			GetLogFile().AddLineX(line, false);
		}

		///<summary>
		///</summary>
		///<param name="line"></param>
		public static void AddVerboseLine(string line)
		{
			if (GetLogFile().VerboseLogging)
				GetLogFile().AddLineX("    (" + line + ")", false);
		}

		///<summary/>
		public static bool IsLogging
		{
			get => GetLogFile().Logging;
			set => GetLogFile().Logging = value;
		}

		/// <summary>
		/// Property to set or check verbose logging
		/// </summary>
		public static bool IsVerbose
		{
			get => GetLogFile().VerboseLogging;
			set => GetLogFile().VerboseLogging = value;
		}

		///<summary>
		///</summary>
		public static void Release()
		{
			if (!SingletonsContainer.Contains<LogFileImpl>())
				return;

			AddLine("----- LogFile Object Released -----");
			SingletonsContainer.Get<LogFileImpl>().Shutdown();
			SingletonsContainer.Remove(SingletonsContainer.Get<LogFileImpl>());
		}

		/// <summary/>
		public static string LogPath => GetLogFile().LogPath;

		#endregion

		private sealed class LogFileImpl: IDisposable
		{
			#region private member variables
			private readonly string m_sFileName;
			private StreamWriter m_file;
			#endregion

			#region Properties
			public bool Logging { get; set; }
			public bool VerboseLogging { get; set; }
			public string LogPath => m_sFileName;
			#endregion

			#region public methods to do the work
			public LogFileImpl()
			{
				Logging = true;
				VerboseLogging = true;
				m_file = null;

				m_sFileName = Path.Combine(Directory.GetCurrentDirectory(), "UnicodeCharEditorLog.txt");
				try
				{
					if (m_sFileName != null)
						m_file = new StreamWriter(m_sFileName, true) { AutoFlush = true};
					AddLineX("----- LogFile Object Created -----", false);
				}
				catch (Exception e)
				{
					Console.WriteLine(@"An error occurred: '{0}'", e);
					m_sFileName = "";	// can't log with exception somewhere...
					Logging = false;
				}
			}

			public void AddLineX(string line, bool echoToStdError)
			{
				var dateStamp = $"[{DateTime.Now.ToString(CultureInfo.InvariantCulture)}] ";

				//			// always log to the debug output window
				//			System.Diagnostics.Debug.Write(dateStamp, "Log");
				if (Platform.IsWindows)
				{
					// TODO-Linux: this breaks unit test: InstallLanguageTests.IcuTests.TestInstallLanguage_argumentParser
					// since System.Diagnostics.Debug goes to StdOut on Linux.
					System.Diagnostics.Debug.WriteLine(line);
				}

				if (!Logging)
					return;

				m_file.Write(dateStamp);
				m_file.WriteLine(line);

				if (echoToStdError)
				{
					Console.Error.Write(dateStamp);
					Console.Error.WriteLine(line);
				}
			}

			public void Shutdown()
			{
				if (Logging)
					m_file.Close();

				Dispose();
			}

			#region Disposable stuff
			#if DEBUG
			/// <summary/>
			~LogFileImpl()
			{
				Dispose(false);
			}
			#endif

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			private void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
				if (fDisposing)
				{
					// dispose managed and unmanaged objects
					if (m_file != null)
						m_file.Dispose();
				}
				m_file = null;
			}
			#endregion
			#endregion
		}
	}
}
