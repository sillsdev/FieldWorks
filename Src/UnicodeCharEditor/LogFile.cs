// Copyright (c) 2010-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// <summary />
	internal static class LogFile
	{
		#region Static methods to interact with the logging
		/// <summary />
		private static LogFileImpl GetLogFile()
		{
			return SingletonsContainer.Get<LogFileImpl>();
		}

		/// <summary />
		internal static void AddErrorLine(string line)
		{
			GetLogFile().AddLineX(line, true);
		}

		/// <summary />
		public static void AddLine(string line)
		{
			GetLogFile().AddLineX(line, false);
		}

		/// <summary />
		internal static void AddVerboseLine(string line)
		{
			if (GetLogFile().VerboseLogging)
			{
				GetLogFile().AddLineX("    (" + line + ")", false);
			}
		}

		/// <summary />
		internal static bool IsLogging
		{
			get => GetLogFile().Logging;
			set => GetLogFile().Logging = value;
		}

		/// <summary>
		/// Property to set or check verbose logging
		/// </summary>
		internal static bool IsVerbose
		{
			get => GetLogFile().VerboseLogging;
			set => GetLogFile().VerboseLogging = value;
		}

		/// <summary />
		internal static void Release()
		{
			if (!SingletonsContainer.Contains<LogFileImpl>())
			{
				return;
			}
			AddLine("----- LogFile Object Released -----");
			SingletonsContainer.Get<LogFileImpl>().Shutdown();
			SingletonsContainer.Remove(SingletonsContainer.Get<LogFileImpl>());
		}

		/// <summary/>
		public static string LogPath => GetLogFile().LogFilePath;

		#endregion

		private sealed class LogFileImpl : IDisposable
		{
			private StreamWriter m_file;

			internal bool Logging { get; set; }
			internal bool VerboseLogging { get; set; }
			internal string LogFilePath { get; }

			#region Internal methods to do the work
			public LogFileImpl()
			{
				Logging = true;
				VerboseLogging = true;

				LogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "UnicodeCharEditorLog.txt");
				try
				{
					m_file = new StreamWriter(LogFilePath, true) { AutoFlush = true };
					AddLineX("----- LogFile Object Created -----", false);
				}
				catch (Exception e)
				{
					Console.WriteLine(@"An error occurred: '{0}'", e);
					LogFilePath = string.Empty;   // can't log with exception somewhere...
					Logging = false;
				}
			}

			internal void AddLineX(string line, bool echoToStdError)
			{
				var dateStamp = $"[{DateTime.Now.ToString(CultureInfo.InvariantCulture)}] ";
				if (Platform.IsWindows)
				{
					// TODO-Linux: this breaks unit test: InstallLanguageTests.IcuTests.TestInstallLanguage_argumentParser
					// since System.Diagnostics.Debug goes to StdOut on Linux.
					Debug.WriteLine(line);
				}
				if (!Logging)
				{
					return;
				}
				m_file.Write(dateStamp);
				m_file.WriteLine(line);

				if (echoToStdError)
				{
					Console.Error.Write(dateStamp);
					Console.Error.WriteLine(line);
				}
			}

			internal void Shutdown()
			{
				if (Logging)
				{
					m_file.Close();
				}
				Dispose();
			}

			#region Disposable stuff
			/// <summary />
			~LogFileImpl()
			{
				Dispose(false);
			}

			/// <summary />
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary />
			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " *******");

				if (disposing)
				{
					// dispose managed objects
					m_file?.Dispose();
				}
				m_file = null;
			}
			#endregion
			#endregion
		}
	}
}