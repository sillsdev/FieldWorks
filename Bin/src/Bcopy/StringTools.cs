using System;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DeanJackson.Tools
{

	public class StringTools
	{
		public StringTools()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public static void LogToEventLog(string applicationName, string message, EventLogEntryType entryType, int number)
		{
			try
			{
				EventLog.WriteEntry(applicationName, message, entryType, number);
			}
			catch
			{}
		}

		public static void LogErrorToFile(string filePath, string fileArchivePath, int number, string source, string message, string notes)
		{
			try
			{
				bool WriteHeaders = false;

				// Archive log if it's getting big and also determine if this is a new
				// file, where we need to write the header information at the top.
				long LogSize = 0;

				if (File.Exists("Bcopy_log.txt"))
				{
					FileInfo LogFile = new FileInfo("Bcopy_log.txt");
					LogSize = LogFile.Length;

					if (LogSize > 175000)
					{
						if (!Directory.Exists("BCopy Logs"))
							Directory.CreateDirectory("BCopy Logs");
						File.Move("Bcopy_log.txt", "BCopy Logs\\BCopy_" + DateTime.Now.ToString("yyyyMMdd_hhmmsst") + ".txt");
						WriteHeaders = true;
					}
				}
				else
					WriteHeaders = true;


				// log the error
				using (StreamWriter LogFile = File.AppendText("Bcopy_log.txt"))
				{
					if (WriteHeaders)
					{
						LogFile.WriteLine("*** Log Format ***");
						LogFile.WriteLine("[Date and Time] Number Procedure() - Message {parameter notes}");
						LogFile.WriteLine();
						LogFile.WriteLine("*** Log Entries ***");
						LogFile.Flush();
					}

					LogFile.WriteLine("[" + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt") + "] " + number + " " + source + " - " +
						StringTools.Replace(message, "\r\n", "") + " {" + notes + "}");

					LogFile.Flush();
				}
			}
			catch
			{}
		}

		public static string Replace(string source, string findPattern, string replacePattern)
		{
			// *******
			// This procedure uses "regular expressions" to replace a string pattern
			// located in a given string, with the desired replacement string.
			// *******

			try
			{
				return Regex.Replace(source, @"(?:" + findPattern + ")", replacePattern);
			}
			catch (Exception e)
			{
				throw e;
			}
		}

		public static short ParseShort(string stringToParse)
		{
			// *******
			// This procedure parses out a short number from a string. It could
			// handle integers or larger, but we just need a short.
			// *******

			double result;

			if (stringToParse.Length == 0)
				return 0;

			if (Double.TryParse(stringToParse, System.Globalization.NumberStyles.Integer,
				System.Globalization.CultureInfo.CurrentCulture, out result) == true)

				if (result > short.MaxValue || result < short.MinValue)
					return 0;
				else
					return (short)result;
			else
				return 0;
		}

		public static byte ParseByte(string stringToParse)
		{
			// *******
			// This procedure parses out a byte number from a string. It could
			// handle integers or larger, but we just need a byte.
			// *******

			double result;

			if (stringToParse.Length == 0)
				return 0;

			if (Double.TryParse(stringToParse, System.Globalization.NumberStyles.Integer,
				System.Globalization.CultureInfo.CurrentCulture, out result) == true)

				if (result > Byte.MaxValue || result < Byte.MinValue)
					return 0;
				else
					return (byte)result;
			else
				return 0;
		}
	}
}
