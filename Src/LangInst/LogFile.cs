// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LogFile.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using Microsoft.Win32;			// registry types

namespace InstallLanguage
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for LogFile.
	/// </summary>
	/// 	/// ----------------------------------------------------------------------------------------
	public class LogFile
	{
		#region Static methods to interact with the logging
		public static LogFile GetLogFile()
		{
			if (m_LogFile == null)
			{
				m_LogFile = new LogFile();
				AddLine("----- LogFile Object Created -----");
			}

			return m_LogFile;
		}

		public static void AddErrorLine(string line)
		{
			GetLogFile().AddLineX(line, true);
		}

		public static void AddLine(string line)
		{
			GetLogFile().AddLineX(line, false);
		}

		public static void AddVerboseLine(string line)
		{
			if (GetLogFile().m_VerboseLoging)
				GetLogFile().AddLineX("    (" + line + ")", false);
		}

		public static bool IsLogging()
		{
			return GetLogFile().m_bLogging;
		}

		public static void Release()
		{
			if (m_LogFile == null)
				return;

			AddLine("----- LogFile Object Released -----");
			m_LogFile.Shutdown();
			m_LogFile = null;
		}

		#endregion

		#region private member variables
		private static LogFile m_LogFile = null;	// = new LogFile();
		private string m_FileName = "";
		private bool m_bLogging = false;
		private bool m_VerboseLoging = false;
		private StreamWriter m_File;
		#endregion

		#region private methods to do the work
		private LogFile()
		{
			m_bLogging = false;
			m_File = null;

			m_FileName = "";
			try
			{
				// Try to find the key.
				RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\SIL\FieldWorks");

				if (regKey != null)
				{
					string useLogFile = (string)regKey.GetValue("InstallLanguageUseLog");

					if (useLogFile != null)
					{
						if (useLogFile.Substring(0,1).ToUpperInvariant() == "T" ||	// true
							useLogFile.Substring(0,1).ToUpperInvariant() == "Y" ||	// yes
							useLogFile == "1")								// 1
						{
							m_bLogging = true;
							m_VerboseLoging = false;
						}

						if (useLogFile.Substring(0,1).ToUpperInvariant() == "V")		// verbose
						{
							m_bLogging = true;
							m_VerboseLoging = true;
						}

						if (m_bLogging)	// logging is enabled
						{
							m_FileName = (string)regKey.GetValue("InstallLanguageLog");
							if (m_FileName != null)
								m_File = new StreamWriter(m_FileName, true);
							else
							{
								Console.WriteLine(
									@"Need to specify InstallLanguageLog in HKLM\SOFTWARE\SIL\FieldWorks");
								m_bLogging = false;
							}
						}
					}
					regKey.Close();
				}
			}
			catch(Exception e)
			{
				Console.WriteLine("An error occurred: '{0}'", e);
				m_FileName = "";	// can't log with exception somewhere...
				m_bLogging = false;
			}
		}

		private void AddLineX(string line, bool echoToStdError)
		{
			string dateStamp = "[" + DateTime.Now.ToString() + "] ";

//			// always log to the debug output window
//			System.Diagnostics.Debug.Write(dateStamp, "Log");
			System.Diagnostics.Debug.WriteLine(line);

			if (!m_bLogging)
				return;

			m_File.Write(dateStamp);
			m_File.WriteLine(line);

			if (echoToStdError)
			{
				System.Console.Error.Write(dateStamp);
				System.Console.Error.WriteLine(line);
			}
		}

		private void Shutdown()
		{
			if (m_bLogging)
				m_File.Close();
		}

		#endregion
	}
}
