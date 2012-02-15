using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Utility methods for FLExBridge interaction
	/// </summary>
	public class FLExBridgeHelper
	{
		//public constants for the various views/actions (the -v param)
		/// <summary>
		/// constant for launching the bridge in Send and Receive mode
		/// </summary>
		public const string SendReceive = @"send_receive";
		/// <summary>
		/// constant for launching the bridge in Obtain project mode
		/// </summary>
		public const string Obtain = @"obtain";
		/// <summary>
		/// constant for launching the bridge in the Conflict\Notes Viewer mode
		/// </summary>
		public const string ConflictViewer = "view_notes";

		private const string FLExBridgeName = @"FieldWorksBridge.exe";

		/// <summary>
		/// Launches the FLExBridge application with the given commands
		/// </summary>
		/// <param name="projectFolder">optional</param>
		/// <param name="userName"></param>
		/// <param name="command"></param>
		public static void LaunchFieldworksBridge(string projectFolder, string userName, string command)
		{
			string args = "";
			if (userName != null)
			{
				AddArg(ref args, "-u", userName);
			}
			if (projectFolder != null)
			{
				AddArg(ref args, "-p", projectFolder);
			}
			AddArg(ref args, "-v", command);
			Process.Start(FullFieldWorksBridgePath(), args);
		}

		private static void AddArg(ref string extant, string flag, string value)
		{
			if (!string.IsNullOrEmpty(extant))
			{
				extant += " ";
			}
			extant += flag;
			if (!string.IsNullOrEmpty(value))
			{
				bool hasWhitespace;
				if (value.Any(Char.IsWhiteSpace))
				{
					extant += " \"" + value + "\"";
				}
				else
				{
					extant += " " + value;
				}
			}
		}

		/// <summary>
		/// Returns the full path and filename of the FieldWorksBridge executable
		/// </summary>
		/// <returns></returns>
		public static string FullFieldWorksBridgePath()
		{
			return Path.Combine(DirectoryFinder.FWCodeDirectory, FLExBridgeName);
		}
	}
}
