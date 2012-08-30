using System;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	public class Make : ToolTask
	{
		public Make()
		{
		}

		/// <summary>
		/// Gets or sets the path to the makefile.
		/// </summary>
		[Required]
		public string Makefile { get; set; }

		/// <summary>
		/// Gets or sets the build configuration (Debug, Release, Profile, Bounds).
		/// </summary>
		[Required]
		public string Configuration { get; set; }

		/// <summary>
		/// Gets or sets the root directory of the build (repository) tree.
		/// </summary>
		[Required]
		public string BuildRoot { get; set; }

		/// <summary>
		/// Gets or sets the target inside the Makefile.
		/// </summary>
		public string Target { get; set; }

		/// <summary>
		/// Gets the build "type" (d, r, p, b).
		/// This can be derived from the build configuration.
		/// </summary>
		private string BuildType
		{
			get
			{
				switch (Configuration)
				{
					case "Debug":
						return "d";
					case "Release":
						return "r";
					case "Profile":
						return "p";
					case "Bounds":
						return "b";
					default:
						return "d";
				}
			}
		}

		/// <summary>
		/// Gets or sets the working directory.
		/// </summary>
		/// <value>The working directory.</value>
		/// <returns>
		/// The directory in which to run the executable file, or a null reference (Nothing in Visual Basic) if the executable file should be run in the current directory.
		/// </returns>
		public string WorkingDirectory { get; set; }

		#region Task Overrides
		protected override string ToolName
		{
			get
			{
				if (Environment.OSVersion.Platform == PlatformID.Unix)
					return "make";
				else
					return "nmake.exe";
			}
		}

		private void CheckToolPath()
		{
		   string makePath = ToolPath == null ? String.Empty : ToolPath.Trim();
			if (!String.IsNullOrEmpty(makePath) && File.Exists(Path.Combine(makePath, ToolName)))
			{
				ToolPath = makePath;
				return;
			}
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				ToolPath = "/usr/bin";
				if (File.Exists(Path.Combine(ToolPath, ToolName)))
					return;
			}
			string path = Environment.GetEnvironmentVariable("PATH");
			string[] splitPath = path.Split(new[] {Path.DirectorySeparatorChar});
			foreach (var dir in splitPath)
			{
				if (File.Exists(Path.Combine(dir, ToolName)))
				{
					ToolPath = dir;
					return;
				}
			}
			// Blind guess since we can't figure it out.
			ToolPath = "C:\\Program Files\\Microsoft Visual Studio 10.0\\VC\\bin";
		}

		protected override string GenerateFullPathToTool()
		{
			CheckToolPath();
			return Path.Combine(ToolPath, ToolName);
		}

		protected override string GenerateCommandLineCommands()
		{
			var bldr = new CommandLineBuilder();
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				bldr.AppendSwitchIfNotNull("BUILD_CONFIG=", Configuration);
				bldr.AppendSwitchIfNotNull("BUILD_TYPE=", BuildType);
				bldr.AppendSwitchIfNotNull("BUILD_ROOT=", BuildRoot);
				bldr.AppendSwitchIfNotNull("-C", Path.GetDirectoryName(Makefile));
				if (string.IsNullOrEmpty(Target))
					bldr.AppendSwitch("all");
				else
					bldr.AppendSwitch(Target);
			}
			else
			{
				bldr.AppendSwitch("/nologo");
				bldr.AppendSwitchIfNotNull("BUILD_CONFIG=", Configuration);
				bldr.AppendSwitchIfNotNull("BUILD_TYPE=", BuildType);
				bldr.AppendSwitchIfNotNull("BUILD_ROOT=", BuildRoot);
				bldr.AppendSwitchIfNotNull("/f ", Makefile);
				if (string.IsNullOrEmpty(Target))
					bldr.AppendSwitch("all");
				else
					bldr.AppendSwitch(Target);
			}
			return bldr.ToString();
		}

		/// <summary>
		/// Returns the directory in which to run the executable file, or a null reference if the executable file should be run in the current directory.
		/// </summary>
		protected override string GetWorkingDirectory()
		{
			return string.IsNullOrEmpty(WorkingDirectory) ? base.GetWorkingDirectory() : WorkingDirectory;
		}
		#endregion
	}
}
