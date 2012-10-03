#define TEMPFIX
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	public class GenerateFwTargets : Task
	{
		public override bool Execute()
		{
			var gen = new FwBuildTasks.CollectTargets(Log);
			gen.Generate();
			return true;
		}
	}

	/// <summary>
	/// Collect projects from the FieldWorks repository tree, and generate a targets file
	/// for MSBuild (Mono xbuild).
	/// </summary>
	public class CollectTargets
	{
		string m_fwroot;
		Dictionary<string, string> m_mapProjFile = new Dictionary<string, string>();
		Dictionary<string, List<string>> m_mapProjDepends = new Dictionary<string, List<string>>();

		public CollectTargets()
		{
			// Get the parent directory of the running program.  We assume that
			// this is the root of the FieldWorks repository tree.
			var fwrt = BuildUtils.GetAssemblyFolder();
			while (!Directory.Exists(Path.Combine(fwrt, "Build")) || !Directory.Exists(Path.Combine(fwrt, "Src")))
			{
				fwrt = Path.GetDirectoryName(fwrt);
				if (fwrt == null)
				{
					Console.WriteLine("Error pulling the working folder from the running assembly.");
					break;
				}
			}
			m_fwroot = fwrt;
		}

		public CollectTargets(TaskLoggingHelper log)
		{
			// Get the parent directory of the running program.  We assume that
			// this is the root of the FieldWorks repository tree.
			var fwrt = BuildUtils.GetAssemblyFolder();
			while (!Directory.Exists(Path.Combine(fwrt, "Build")) || !Directory.Exists(Path.Combine(fwrt, "Src")))
			{
				fwrt = Path.GetDirectoryName(fwrt);
				if (fwrt == null)
				{
					log.LogError("Error pulling the working folder from the running assembly.");
					break;
				}
			}
			m_fwroot = fwrt;
		}

		/// <summary>
		/// Scan all the known csproj files under FWROOT for references, and then
		/// create msbuild target files accordingly.
		/// </summary>
		public void Generate()
		{
			var infoSrc = new DirectoryInfo(Path.Combine(m_fwroot, "Src"));
			CollectInfo(infoSrc);
			// These projects from Lib had nant targets.  They really should be under Src.
			var infoSilUtil = new DirectoryInfo(Path.Combine(m_fwroot, "Lib/src/SilUtils"));
			CollectInfo(infoSilUtil);
			var infoEth = new DirectoryInfo(Path.Combine(m_fwroot, "Lib/src/Ethnologue"));
			CollectInfo(infoEth);
			var infoScr = new DirectoryInfo(Path.Combine(m_fwroot, "Lib/src/SharedScrControls"));
			CollectInfo(infoScr);
			var infoScr2 = new DirectoryInfo(Path.Combine(m_fwroot, "Lib/src/SharedScrUtils"));
			CollectInfo(infoScr2);
			var infoScr3 = new DirectoryInfo(Path.Combine(m_fwroot, "Lib/src/ScrChecks"));
			CollectInfo(infoScr3);
			var infoPhr = new DirectoryInfo(Path.Combine(m_fwroot, "Lib/src/PhraseTranslationHelper"));
			CollectInfo(infoPhr);
			var infoObj = new DirectoryInfo(Path.Combine(m_fwroot, "Lib/src/ObjectBrowser"));
			CollectInfo(infoObj);
			WriteTargetFiles();
		}

		/// <summary>
		/// Recursively scan the directory for csproj files.
		/// </summary>
		private void CollectInfo(DirectoryInfo dirInfo)
		{
			if (dirInfo == null || !dirInfo.Exists)
				return;
			foreach (var fi in dirInfo.GetFiles())
			{
				if (fi.Name.EndsWith(".csproj") && fi.Exists)
					ProcessCsProjFile(fi.FullName);
			}
			foreach (var diSub in dirInfo.GetDirectories())
				CollectInfo(diSub);
		}

		/// <summary>
		/// Extract the reference information from a csproj file.
		/// </summary>
		private void ProcessCsProjFile(string filename)
		{
			if (filename.Contains("Src/LexText/Extensions/") || filename.Contains("Src\\LexText\\Extensions\\"))
				return;		// Skip the extensions -- they're either obsolete or nonstandard.
			var project = Path.GetFileNameWithoutExtension(filename);
			if (project == "ICSharpCode.SharpZLib")
				return;
#if TEMPFIX
			// These projects are obsolete, but still exist in the Linux branches.
			if (project == "MenuExtender" ||			// independent target in nant build files
				project == "MenuExtenderTests" ||		// implied target in nant build files
				project == "ParserWatcher" ||			// independent target in nant build files
				project == "RunAddConverter" ||			// independent target in nant build files
				project == "ProgressBarTest" ||
				project == "P4Helper" ||
				project == "FixUp" ||
				project == "RemoteReport" ||
				project == "VwGraphicsReplayer" ||
				project == "XCoreSample" ||
				project == "SilSidePaneTestApp" ||
				project == "SfmStats" ||
				project == "ConvertSFM" ||
				project == "XSLTTester")
			{
				return;
			}
#endif
			if (m_mapProjFile.ContainsKey(project) || m_mapProjDepends.ContainsKey(project))
			{
				Console.WriteLine("Project '{0}' has already been found elsewhere!", project);
				return;
			}
			m_mapProjFile.Add(project, filename);
			List<string> dependencies = new List<string>();
			using (var reader = new StreamReader(filename))
			{
				while (!reader.EndOfStream)
				{
					var line = reader.ReadLine().Trim();
					if (line.Contains("<Reference Include="))
					{
						var ref0 = line.Substring(line.IndexOf('"')+1);
						var ref1 = ref0.Substring(0, ref0.IndexOf('"'));
						var i0 = ref1.IndexOf(',');
						if (i0 >= 0)
							ref1 = ref1.Substring(0, i0);
						//Console.WriteLine("{0} [R]: ref0 = '{1}'; ref1 = '{2}'", filename, ref0, ref1);
						dependencies.Add(ref1);
					}
					else if (line.Contains("<ProjectReference Include="))
					{
						var ref0 = line.Substring(line.IndexOf('"') + 1);
						var ref1 = ref0.Substring(0, ref0.IndexOf('"'));
						var i0 = ref1.LastIndexOfAny(new[] { '\\', '/' });
						if (i0 >= 0)
							ref1 = ref1.Substring(i0 + 1);
						ref1 = ref1.Replace(".csproj", "");
						//Console.WriteLine("{0} [PR]: ref0 = '{1}'; ref1 = '{2}'", filename, ref0, ref1);
						dependencies.Add(ref1);
					}
				}
				reader.Close();
			}
			m_mapProjDepends.Add(project, dependencies);
		}

		/// <summary>
		/// Used the collected information to write the needed target files.
		/// </summary>
		private void WriteTargetFiles()
		{
			// Write all the C# targets and their dependencies.

			using (var writer = new StreamWriter(Path.Combine(m_fwroot, "Build/FieldWorks.targets")))
			{
				writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
				writer.WriteLine("<!-- This file is automatically generated by the Setup target.  DO NOT EDIT! -->");
				writer.WriteLine("<!-- Unfortunately, the new one is generated after the old one has been read. -->");
				writer.WriteLine("<!-- 'msbuild /t:refreshTargets' generates this file and does nothing else. -->");
				writer.WriteLine("<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\" ToolsVersion=\"4.0\">");
				writer.WriteLine();
				foreach (var project in m_mapProjFile.Keys)
				{
					writer.Write("\t<Target Name=\"{0}\"", project);
					var bldr = new StringBuilder();
					bldr.Append("Initialize");	// ensure the output directories and version files exist.
					if (project == "COMInterfaces")
						bldr.Append(";mktlbs");
					// The xWorksTests require us to have built on of the adapters, typically FlexUIAdapter.dll.
					// However, we don't discover that dependency, because it is invoked by reflection (in xCore) and neither xWorks
					// nor xCore references it. Nor can we fix it by adding a reference, because (a) this would break the aimed-for
					// independence of xCore from a particular adapter, and (b) for some bizarre historical reason, the project that
					// builds FlexUIAdapter.dll is called XCoreAdapterSilSidePane. We may eventually get around to fixing the latter
					// problem and decide to sacrifice the independence of xCore, but for now, it's simplest to patch the target generation.
					if (project == "xWorksTests")
						bldr.Append(";XCoreAdapterSilSidePane");
					var dependencies = m_mapProjDepends[project];
					foreach (var dep in dependencies)
					{
						if (m_mapProjFile.ContainsKey(dep))
							bldr.AppendFormat(";{0}", dep);
					}
					writer.Write(" DependsOnTargets=\"{0}\"", bldr.ToString());
					if (project == "MigrateSqlDbs")
					{
						writer.Write(" Condition=\"'$(OS)'=='Windows_NT'\"");
					}
					if (project.StartsWith("LinuxSmoke") ||
						project.StartsWith("ManagedLgIcuCollator") ||
						project.StartsWith("ManagedVwWindow"))
					{
						writer.Write(" Condition=\"'$(OS)'=='Unix'\"");
					}
					writer.WriteLine(">");
					writer.WriteLine("\t\t<MSBuild Projects=\"{0}\"", m_mapProjFile[project].Replace(m_fwroot, "$(fwrt)"));
					var projectSubDir = Path.GetDirectoryName(m_mapProjFile[project]);
					projectSubDir = projectSubDir.Substring(m_fwroot.Length);
					projectSubDir = projectSubDir.Replace("\\", "/");
					if (projectSubDir.StartsWith("/Src/"))
						projectSubDir = projectSubDir.Substring(5);
					else if (projectSubDir.StartsWith("/Lib/src/"))
						projectSubDir = projectSubDir.Substring(9);
					else if (projectSubDir.StartsWith("/"))
						projectSubDir = projectSubDir.Substring(1);
					if (Path.DirectorySeparatorChar != '/')
						projectSubDir = projectSubDir.Replace('/', Path.DirectorySeparatorChar);
					writer.WriteLine("\t\t         Targets=\"$(msbuild-target)\"");
					writer.WriteLine("\t\t         Properties=\"$(msbuild-props);IntermediateOutputPath=$(dir-fwobj){0}{1}{0}\"",
						Path.DirectorySeparatorChar, projectSubDir);
					writer.WriteLine("\t\t         ToolsVersion=\"4.0\"/>");
					if (project.EndsWith("Tests"))
					{
						writer.WriteLine("\t\t<NUnit Condition=\"'$(action)'=='test'\"");
						writer.WriteLine("\t\t       Assemblies=\"$(dir-outputBase)/{0}.dll\"", project);
						writer.WriteLine("\t\t       ToolPath=\"$(fwrt)/Bin/NUnit/bin\"");
						writer.WriteLine("\t\t       WorkingDirectory=\"$(dir-outputBase)\"");
						writer.WriteLine("\t\t       OutputXmlFile=\"$(dir-outputBase)/{0}.dll-nunit-output.xml\"", project);
						writer.WriteLine("\t\t       Force32Bit=\"$(useNUnit-x86)\"");
						writer.WriteLine("\t\t       ExcludeCategory=\"$(excludedCategories)\"");
						writer.WriteLine("\t\t       ContinueOnError=\"true\" />");
						writer.WriteLine("\t\t<Message Text=\"Finished building {0}.\" Condition=\"'$(action)'!='test'\"/>", project);
						writer.WriteLine("\t\t<Message Text=\"Finished building {0} and running tests.\" Condition=\"'$(action)'=='test'\"/>", project);
					}
					else
					{
						writer.WriteLine("\t\t<Message Text=\"Finished building {0}.\"/>", project);
					}
					writer.WriteLine("\t</Target>");
					writer.WriteLine();
				}
				writer.Write("\t<Target Name=\"allCsharp\" DependsOnTargets=\"");
				bool first = true;
				foreach (var project in m_mapProjFile.Keys)
				{
					if (project.StartsWith("SharpViews") ||		// These projects are experimental.
						project == "FxtExe" ||					// These projects weren't built by nant normally.
						project == "FixFwData" ||
						project.StartsWith("LinuxSmokeTest"))
					{
						continue;
					}
					if (first)
						writer.Write(project);
					else
						writer.Write(";{0}", project);
					first = false;
				}
				writer.WriteLine("\"/>");
				writer.WriteLine();
				writer.Write("\t<Target Name=\"allCsharpNoTests\" DependsOnTargets=\"");
				first = true;
				foreach (var project in m_mapProjFile.Keys)
				{
					if (project.StartsWith("SharpViews") ||		// These projects are experimental.
						project == "FxtExe" ||					// These projects weren't built by nant normally.
						project == "FixFwData" ||
						project.StartsWith("LinuxSmokeTest") ||
						project.EndsWith("Tests"))				// These are tests.
					{
						continue;
					}
					if (first)
						writer.Write(project);
					else
						writer.Write(";{0}", project);
					first = false;
				}
				writer.WriteLine("\"/>");
				writer.WriteLine();
				writer.WriteLine("</Project>");
				writer.Flush();
				writer.Close();
			}
			Console.WriteLine("Created {0}", Path.Combine(m_fwroot, "Build/FieldWorks.targets"));
		}
	}
}
