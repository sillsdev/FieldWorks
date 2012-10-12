/* Copyright © 2012 by SIL International. */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Win32;

namespace FwBuildTasks
{
	/// <summary>
	/// Run NUnit on a test assembly.
	/// </summary>
	public class NUnit : TestTask
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public NUnit()
		{
			Timeout = Int32.MaxValue;
		}

		/// <summary>
		/// Gets or sets the categories to include.
		/// </summary>
		/// <remarks>Multiple values are separated by a comma ","</remarks>
		public string IncludeCategory { get; set; }

		/// <summary>
		/// Gets or sets the categories to exclude.
		/// </summary>
		/// <remarks>Multiple values are separated by a comma ","</remarks>
		public string ExcludeCategory { get; set; }

		/// <summary>
		/// Gets or sets the fixture.
		/// </summary>
		public string Fixture { get; set; }

		/// <summary>
		/// Gets or sets the XSLT transform file.
		/// </summary>
		public string XsltTransformFile { get; set; }

		/// <summary>
		/// Gets or sets the output XML file.
		/// </summary>
		public string OutputXmlFile { get; set; }

		/// <summary>
		/// The file to receive test error details.
		/// </summary>
		public string ErrorOutputFile { get; set; }

		/// <summary>
		/// Gets or sets the working directory.
		/// </summary>
		public string WorkingDirectory { get; set; }

		/// <summary>
		/// Determines whether assemblies are copied to a shadow folder during testing.
		/// </summary>
		public bool DisableShadowCopy { get; set; }

		/// <summary>
		/// The project configuration to run.
		/// </summary>
		public string ProjectConfiguration { get; set; }

		// make this nullable so we have a third state, not set
		private bool? _testInNewThread = null;

		/// <summary>
		/// Allows tests to be run in a new thread, allowing you to take advantage of ApartmentState and ThreadPriority settings in the config file.
		/// </summary>
		public bool TestInNewThread
		{
			get { return _testInNewThread.HasValue ? _testInNewThread.Value : true; }
			set { _testInNewThread = value; }
		}

		/// <summary>
		/// Determines whether the tests are run in a 32bit process on a 64bit OS.
		/// </summary>
		public bool Force32Bit { get; set; }

		/// <summary>
		/// Determines the framework to run aganist.
		/// </summary>
		public string Framework { get; set; }

		/// <summary>
		/// Gets or sets the path to the NUnit executable assembly.
		/// </summary>
		public string ToolPath { get; set; }

		protected override string ProgramName()
		{
			var mono = Environment.GetEnvironmentVariable("MONO_RUNTIME_EXECUTABLE_PATH");
			if (!String.IsNullOrEmpty(mono))
				return mono;
			EnsureToolPath();
			if (Force32Bit)
				return Path.Combine(ToolPath, "nunit-console-x86.exe");
			else
				return Path.Combine(ToolPath, "nunit-console.exe");
		}

		protected override string ProgramArguments()
		{
			var bldr = new StringBuilder();
			var mono = Environment.GetEnvironmentVariable("MONO_RUNTIME_EXECUTABLE_PATH");
			if (!String.IsNullOrEmpty(mono))
			{
				EnsureToolPath();
				if (Force32Bit)
					bldr.Append(Path.Combine(ToolPath, "nunit-console-x86.exe"));
				else
					bldr.Append(Path.Combine(ToolPath, "nunit-console.exe"));
			}
			if (bldr.Length == 0)
				bldr.Append(FixturePath);
			else
				bldr.AppendFormat(" {0}", FixturePath);
			var switchChar = '/';
			if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
				switchChar = '-';
			bldr.AppendFormat(" {0}nologo", switchChar);
			if (DisableShadowCopy)
				bldr.AppendFormat(" {0}noshadow", switchChar);
			if (_testInNewThread.HasValue && !_testInNewThread.Value)
				bldr.AppendFormat(" {0}nothread", switchChar);
			if (!String.IsNullOrEmpty(ProjectConfiguration))
				bldr.AppendFormat(" {0}config={1}", switchChar, ProjectConfiguration);
			if (!String.IsNullOrEmpty(Fixture))
				bldr.AppendFormat(" {0}fixture={1}", switchChar, Fixture);
			if (!String.IsNullOrEmpty(IncludeCategory))
				bldr.AppendFormat(" {0}include={1}", switchChar, IncludeCategory);
			if (!String.IsNullOrEmpty(ExcludeCategory))
				bldr.AppendFormat(" {0}exclude={1}", switchChar, ExcludeCategory);
			if (!String.IsNullOrEmpty(XsltTransformFile))
				bldr.AppendFormat(" \"{0}transform={1}\"", switchChar, XsltTransformFile);
			if (!String.IsNullOrEmpty(OutputXmlFile))
				bldr.AppendFormat(" \"{0}xml={1}\"", switchChar, OutputXmlFile);
			if (!String.IsNullOrEmpty(ErrorOutputFile))
				bldr.AppendFormat(" \"{0}err={1}\"", switchChar, ErrorOutputFile);
			if (!String.IsNullOrEmpty(Framework))
				bldr.AppendFormat(" {0}framework={1}", switchChar, Framework);
			bldr.AppendFormat(" {0}labels", switchChar);
			return bldr.ToString();
		}

		private void EnsureToolPath()
		{
			if (!String.IsNullOrEmpty(ToolPath) &&
				File.Exists(Path.Combine(ToolPath, "nunit-console.exe")))
			{
				return;
			}
			foreach (var dir in Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator))
			{
				if (File.Exists(Path.Combine(dir, "nunit-console.exe")))
				{
					ToolPath = dir;
					return;
				}
			}
			foreach (var dir in Directory.EnumerateDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)))
			{
				if (dir.StartsWith("NUnit"))
				{
					if (File.Exists(Path.Combine(dir, Path.Combine("bin", "nunit-console.exe"))))
					{
						ToolPath = dir;
						return;
					}
				}
			}
			var keySoftware = Registry.CurrentUser.OpenSubKey("Software");
			if (keySoftware != null && Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				var keyNUnitOrg = keySoftware.OpenSubKey("nunit.org");
				if (keyNUnitOrg != null)
				{
					var keyNUnit = keyNUnitOrg.OpenSubKey("Nunit");
					if (keyNUnit != null)
					{
						foreach (var verName in keyNUnit.GetSubKeyNames())
						{
							var keyVer = keyNUnit.OpenSubKey(verName);
							if (keyVer != null)
							{
								var path = keyVer.GetValue("InstallDir").ToString();
								if (!String.IsNullOrEmpty(path) &&
									File.Exists(Path.Combine(path, "nunit-console.exe")))
								{
									ToolPath = path;
									return;
								}
							}
						}
					}
				}
			}
			ToolPath = ".";
		}

		protected override void ProcessOutput(bool fTimedOut, TimeSpan delta)
		{
			var lines = new List<string>();
			using (var reader = new StreamReader(MemoryStream))
			{
				while (!reader.EndOfStream)
				{
					var line = reader.ReadLine();
					if (line.StartsWith("***** "))
						lines.Add(line);
					else
						Log.LogMessage(MessageImportance.Normal, line);
				}
			}
			if (fTimedOut)
			{
				if (File.Exists(OutputXmlFile))
				{
					FileInfo fi = new FileInfo(OutputXmlFile);
					if (fi.Length > 0)
						File.Move(OutputXmlFile, OutputXmlFile + "-partial");
					else
						File.Delete(OutputXmlFile);
				}
				using (StreamWriter writer = new StreamWriter(OutputXmlFile))
				{
					var num = lines.Count;
					writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
					writer.WriteLine("<test-results name=\"{0}\" total=\"{1}\" errors=\"{2}\" failures=\"{3}\" not-run=\"{4}\" inconclusive=\"{5}\" ignored=\"{6}\" skipped=\"{7}\" invalid=\"{8}\" date=\"{9}\" time=\"{10}\">",
									 FixturePath, num+1, 0, 1, 0, num, 0, 0, 0, DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString());
					writer.WriteLine("  <test-suite type=\"Assembly\" name=\"{0}\" executed=\"True\" result=\"Timeout\" success=\"False\" time=\"{1}\">",
									 FixturePath, delta.TotalSeconds.ToString("F3"));
					writer.WriteLine("    <results>");
					writer.WriteLine("      <test-suite name=\"Timeout\">");
					writer.WriteLine("        <results>");
					writer.WriteLine("          <test-case name=\"Timeout\" success=\"False\" time=\"{0}\" asserts=\"0\"/>", ((double)Timeout/1000.0).ToString("F3"));
					writer.WriteLine("        </results>");
					writer.WriteLine("      </test-suite>");
					writer.WriteLine("    </results>");
					writer.WriteLine("  </test-suite>");
					writer.WriteLine("<!-- tests tried before time ran out:");
					foreach (var line in lines)
						writer.WriteLine(line.Substring(6));
					writer.WriteLine("-->");
					writer.WriteLine("</test-results>");
				}
			}
		}
	}
}
