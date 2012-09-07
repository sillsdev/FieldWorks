using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	/// <summary>
	/// Return the parent directory of the required input attribute.
	/// </summary>
	public class ParentDirectory : Task
	{
		public override bool Execute()
		{
			Value = Path.GetDirectoryName(CurrentDirectory);
			return true;
		}

		[Required]
		public string CurrentDirectory { get; set; }

		[Output]
		public string Value { get; set; }
	}

	/// <summary>
	/// Return the CPU architecture of the current system.  (This is really only used on
	/// linux at the moment.)
	/// </summary>
	public class CpuArchitecture : Task
	{
		public override bool Execute()
		{
			if (Environment.OSVersion.Platform == System.PlatformID.Unix)
			{
				System.Diagnostics.Process proc = new System.Diagnostics.Process();
				proc.StartInfo.UseShellExecute = false;
				proc.StartInfo.RedirectStandardOutput = true;
				proc.StartInfo.FileName = "/usr/bin/arch";
				proc.Start();
				Value = proc.StandardOutput.ReadToEnd().TrimEnd();
				proc.WaitForExit();
			}
			else
			{
				// left as an exercise for later...
				Value = "";
			}
			return true;
		}

		[Output]
		public string Value { get; set; }
	}

	/// <summary>
	/// Mono doesn't implement CombinePaths, so here's a simplified replacement.
	/// </summary>
	public class PathCombine : Task
	{
		public override bool Execute()
		{
			Value = Path.Combine(BasePath, SubPath);
			return true;
		}

		[Required]
		public string BasePath { get; set; }

		[Required]
		public string SubPath { get; set; }

		[Output]
		public string Value { get; set; }
	}

	/// <summary>
	/// Return the name of the current computer.
	/// </summary>
	public class ComputerName : Task
	{
		public override bool Execute()
		{
			Value = Environment.MachineName;
			if (string.IsNullOrEmpty(Value))
				Value = ".";
			return true;
		}

		[Output]
		public string Value { get; set; }
	}

	/// <summary>
	/// Set an environment variable for the current process.
	/// </summary>
	public class SetEnvVar : Task
	{
		[Required]
		public string Variable { get; set; }

		/// <summary>
		/// Value might be empty, so it can't be [Required].
		/// </summary>
		public string Value { get; set; }

		public override bool Execute()
		{
			Environment.SetEnvironmentVariable(Variable, Value);
			return true;
		}
	}

	/// <summary>
	/// Check whether the user has administrative privilege.
	/// </summary>
	public class CheckAdminPrivilege : Task
	{
		[Output]
		public bool UserIsAdmin { get; set; }

		public override bool Execute()
		{
			WindowsIdentity id = WindowsIdentity.GetCurrent();
			WindowsPrincipal p = new WindowsPrincipal(id);
			UserIsAdmin = p.IsInRole(WindowsBuiltInRole.Administrator);
			return true;
		}
	}
}
