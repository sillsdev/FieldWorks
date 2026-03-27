// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace SIL.DisambiguateInFLExDB
{
	public class PCPatrInvoker
	{
		const string takeFileName = "PcPatrFLEx.tak";
		const string logFileName = "Invoker.log";
		public string GrammarFile { get; set; }
		public string AnaFile { get; set; }
		public string AndFile { get; set; }
		public string LogFile { get; set; }
		public string BatchFile { get; set; }
		public string RootGlossState { get; set; }
		public Boolean InvocationSucceeded { get; set; }
		public string MaxAmbiguities { get; set; }
		public string TimeLimit { get; set; }

		public PCPatrInvoker(string grammarFile, string anaFile, string rootglossState)
		{
			GrammarFile = grammarFile;
			AnaFile = anaFile;
			RootGlossState = rootglossState;
			LogFile = Path.Combine(Path.GetTempPath(), logFileName);
			MaxAmbiguities = "100";
			TimeLimit = "0";
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern int GetShortPathName(
			string pathName,
			StringBuilder shortName,
			int cbShortName
		);

		private void CreateBatchFile()
		{
			BatchFile = Path.Combine(Path.GetTempPath(), "PcPatrFLEx.bat");
			StringBuilder sbBatchFile = new StringBuilder();
			sbBatchFile.Append("@echo off\n");
			sbBatchFile.Append("cd \"");
			sbBatchFile.Append(Path.GetTempPath());
			sbBatchFile.Append("\"\n\"");
			sbBatchFile.Append(GetPcPatr64ExePath());
			sbBatchFile.Append("\\pcpatr64\" -t ");
			sbBatchFile.Append(takeFileName);
			sbBatchFile.Append("\n");
			Console.Write(sbBatchFile.ToString());
			File.WriteAllText(BatchFile, sbBatchFile.ToString());
		}

		private string GetPcPatr64ExePath()
		{
			Uri uriBase = new Uri(Assembly.GetExecutingAssembly().CodeBase);
			var rootdir = Path.GetDirectoryName(Uri.UnescapeDataString(uriBase.AbsolutePath));
			return rootdir;
		}

		public void Invoke()
		{
			CreateBatchFile();
			CreateTakeFile();

			File.Delete(AndFile);
			File.Delete(LogFile);

			var processInfo = new ProcessStartInfo("cmd.exe", "/c\"" + BatchFile + "\"");
			processInfo.CreateNoWindow = true;
			processInfo.UseShellExecute = false;
			processInfo.RedirectStandardError = true;
			processInfo.RedirectStandardOutput = true;

			using (var process = Process.Start(processInfo))
			{
				process.WaitForExit();
				string error = process.StandardError.ReadToEnd();
				if (error.Contains("ERROR "))
				{
					InvocationSucceeded = false;
				}
				else
				{
					InvocationSucceeded = true;
				}
			}
		}

		private void CreateTakeFile()
		{
			string takeFile = Path.Combine(Path.GetTempPath(), takeFileName);
			StringBuilder sbTakeFileShortPath = new StringBuilder(255);
			int i = GetShortPathName(takeFile, sbTakeFileShortPath, sbTakeFileShortPath.Capacity);
			var sbTake = new StringBuilder();
			sbTake.Append("set comment |\n");
			sbTake.Append("log ");
			sbTake.Append(logFileName);
			sbTake.Append("\n");
			sbTake.Append("load grammar ");
			StringBuilder sbGrammarFileShortPath = new StringBuilder(255);
			i = GetShortPathName(
				GrammarFile,
				sbGrammarFileShortPath,
				sbGrammarFileShortPath.Capacity
			);
			sbTake.Append(sbGrammarFileShortPath.ToString() + "\n");
			sbTake.Append("set timing on\n");
			sbTake.Append("set gloss on\n");
			sbTake.Append("set features all\n");
			HandleRootGloss(sbTake);
			sbTake.Append("set tree xml\n");
			sbTake.Append("set ambiguities ");
			sbTake.Append(MaxAmbiguities);
			sbTake.Append("\n");
			if (!TimeLimit.Equals("0"))
			{
				sbTake.Append("set limit ");
				sbTake.Append(TimeLimit);
				sbTake.Append("\n");
			}
			sbTake.Append("set write-ample-parses on\n");
			// since the batch fle defaults to the temp directory, we just use the invoker files as they are
			sbTake.Append("file disambiguate Invoker.ana Invoker.and\n");
			sbTake.Append("exit\n");
			//Console.Write(sbTake.ToString());
			File.WriteAllText(takeFile, sbTake.ToString());
			AndFile = Path.Combine(Path.GetTempPath(), "Invoker.and");
		}

		private void HandleRootGloss(StringBuilder sbTake)
		{
			if (String.IsNullOrEmpty(RootGlossState))
			{
				return;
			}
			sbTake.Append("set rootgloss ");
			string result;
			result = GetRootGlossStateValue();
			sbTake.Append(result + "\n");
		}

		public string GetRootGlossStateValue()
		{
			string result;
			string sBeginning = RootGlossState.Substring(0, 1).ToLower();
			switch (sBeginning)
			{
				case "l":
					result = "leftheaded";
					break;
				case "r":
					result = "rightheaded";
					break;
				case "a":
					result = "all";
					break;
				default:
					result = "off";
					break;
			}

			return result;
		}
	}
}
