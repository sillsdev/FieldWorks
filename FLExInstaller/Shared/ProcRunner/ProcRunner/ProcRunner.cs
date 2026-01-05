using System;
using System.Diagnostics;
using System.IO;

namespace ProcRunner
{
	/// <summary>
	/// ProcRunner runs an installer and then restarts the application so that running processes do not block the installer from updating files.
	/// </summary>
	/// <remarks>
	/// IMPORTANT TODO: Because ProcRunner is running when the installer is running, it cannot be updated. Instead, the new version must be installed
	/// beside the old version. Each time ProcRunner is updated, you must:
	///  * Update the version in ProcRunner.csproj, BaseInstallerBuild/Framework.wxs, and CreateUpdatePatch/AppNoUi.wxs
	///  * Generate a new GUID in AssemblyInfo.cs
	/// </remarks>
	public static class ProcRunner
	{
		[STAThread]
		public static int Main(string[] args)
		{
			if (args.Length != 2)
			{
				Console.WriteLine("Usage: ProcRunner [repair_]path-to-installer path-to-app");
				return 1;
			}

			var updatePath = args[0];
			var callingApplication = args[1];

			var argsParam = "/p";

			if (updatePath.StartsWith("repair_", StringComparison.OrdinalIgnoreCase))
			{
				updatePath = $"{{{updatePath.Substring(7)}}}";
				argsParam = "/f";
			}
			else if (updatePath.EndsWith(".exe"))
			{
				argsParam = "/e";
			}
			else if (updatePath.EndsWith(".msi"))
			{
				argsParam = "/i";
			}

			RunUpdate(updatePath, argsParam);

			RunCallingApplication(callingApplication);

			return 0;
		}

		private static void RunUpdate(string installerFile, string arg)
		{
			if (arg.Equals("/e"))
			{
				var exeProc = new Process();
				var exeInfo = exeProc.StartInfo;
				exeInfo.FileName = installerFile;
				//exeInfo.WorkingDirectory = GetAppFolder();
				exeInfo.UseShellExecute = true;

				exeProc.Start();
				exeProc.WaitForExit();
				return;
			}

			var options = "";
			string verb = null;
			var waitForExit = true;
			if (arg.Equals("/i"))
			{
				options = "AUTOUPDATE=\"True\"";
			}
			else if (arg.Equals("/p"))
			{
				var logfile = Path.Combine(Environment.GetFolderPath(
					Environment.SpecialFolder.LocalApplicationData), "Temp", "PtPatch.log");
				options = $"/qb AUTOUPDATE=\"True\" /l*vx \"{logfile}\"";
			}
			else if (arg.Equals("/f"))
			{
				var logfile = Path.Combine(Environment.GetFolderPath(
					Environment.SpecialFolder.LocalApplicationData), "Temp", "PtRepair.log");
				options = $"/qb AUTOUPDATE=\"True\" /l*vx \"{logfile}\"";
				verb = "runas";
				// when doing repair, ProcRunner needs to exit immediately so user doesn't get a prompt to close ProcRunner
				// or to restart their computer when repair is complete.
				waitForExit = false;
			}

			var proc = new Process();
			var info = proc.StartInfo;
			info.FileName = "msiexec.exe";
			//info.WorkingDirectory = GetAppFolder();
			info.UseShellExecute = true;
			info.Arguments = $"{arg} \"{installerFile}\" {options}";
			if (verb != null)
				info.Verb = verb;

			proc.Start();
			if (waitForExit)
				proc.WaitForExit();
		}

		public static void RunCallingApplication(string callingApp)
		{
			var proc = new Process();
			var info = proc.StartInfo;
			info.FileName = callingApp;
			//info.WorkingDirectory = Path.GetDirectoryName(callingApp);
			info.UseShellExecute = true;

			proc.Start();
		}
	}
}
