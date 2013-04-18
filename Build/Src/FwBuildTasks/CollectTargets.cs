#define TEMPFIX
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	public class GenerateFwTargets : Task
	{
		public override bool Execute()
		{
			try
			{
				var gen = new CollectTargets(Log);
				gen.Generate();
				return true;
			}
			catch (CollectTargets.StopTaskException)
			{
				return false;
			}
		}
	}

	/// <summary>
	/// Collect projects from the FieldWorks repository tree, and generate a targets file
	/// for MSBuild (Mono xbuild).
	/// </summary>
	public class CollectTargets
	{
		public class StopTaskException: Exception
		{
			public StopTaskException(Exception innerException) : base(null, innerException)
			{
			}
		}

		private string m_fwroot;
		private Dictionary<string, string> m_mapProjFile = new Dictionary<string, string>();
		private Dictionary<string, List<string>> m_mapProjDepends = new Dictionary<string, List<string>>();
		private TaskLoggingHelper Log { get; set; }
		private XmlDocument m_csprojFile;
		private XmlNamespaceManager m_namespaceMgr;

		public CollectTargets(TaskLoggingHelper log)
		{
			Log = log;
			// Get the parent directory of the running program.  We assume that
			// this is the root of the FieldWorks repository tree.
			var fwrt = BuildUtils.GetAssemblyFolder();
			while (!Directory.Exists(Path.Combine(fwrt, "Build")) || !Directory.Exists(Path.Combine(fwrt, "Src")))
			{
				fwrt = Path.GetDirectoryName(fwrt);
				if (fwrt == null)
				{
					Log.LogError("Error pulling the working folder from the running assembly.");
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
				Log.LogWarning("Project '{0}' has already been found elsewhere!", project);
				return;
			}
			m_mapProjFile.Add(project, filename);
			List<string> dependencies = new List<string>();
			using (var reader = new StreamReader(filename))
			{
				int lineNumber = 0;
				while (!reader.EndOfStream)
				{
					lineNumber++;
					var line = reader.ReadLine().Trim();
					try
					{
						if (line.Contains("<Reference Include="))
						{
							// line is similar to
							// <Reference Include="BasicUtils, Version=4.1.1.0, Culture=neutral, processorArchitecture=MSIL">
							var tmp = line.Substring(line.IndexOf('"') + 1);
							// NOTE: we assume that the name of the assembly is the same as the name of the project
							var projectName = tmp.Substring(0, tmp.IndexOf('"'));
							var i0 = projectName.IndexOf(',');
							if (i0 >= 0)
								projectName = projectName.Substring(0, i0);
							//Console.WriteLine("{0} [R]: ref0 = '{1}'; ref1 = '{2}'", filename, ref0, ref1);
							dependencies.Add(projectName);
						}
						else if (line.Contains("<ProjectReference Include="))
						{
							// line is similar to
							// <ProjectReference Include="..\HermitCrab\HermitCrab.csproj">
							var tmp = line.Substring(line.IndexOf('"') + 1);
							// NOTE: we assume that the name of the assembly is the same as the name of the project
							var projectName = tmp.Substring(0, tmp.IndexOf('"'));
							// Unfortunately we can't use File.GetFileNameWithoutExtension(projectName)
							// here: we use the same .csproj file on both Windows and Linux
							// and so it contains backslashes in the name which is a valid
							// character on Linux.
							var i0 = projectName.LastIndexOfAny(new[] { '\\', '/' });
							if (i0 >= 0)
								projectName = projectName.Substring(i0 + 1);
							projectName = projectName.Replace(".csproj", "");
							//Console.WriteLine("{0} [PR]: ref0 = '{1}'; ref1 = '{2}'", filename, ref0, ref1);
							dependencies.Add(projectName);
						}
					}
					catch (ArgumentOutOfRangeException e)
					{
						Log.LogError("GenerateFwTargets", null, null,
							filename, lineNumber, 0, 0, 0,
							"Error reading project references. Invalid XML file?");
						throw new StopTaskException(e);
					}
				}
				reader.Close();
			}
			m_mapProjDepends.Add(project, dependencies);
		}

		private void LoadProjectFile(string projectFile)
		{
			m_csprojFile = new XmlDocument();
			m_csprojFile.Load(projectFile);
			m_namespaceMgr = new XmlNamespaceManager(m_csprojFile.NameTable);
			m_namespaceMgr.AddNamespace("c", "http://schemas.microsoft.com/developer/msbuild/2003");
		}

		/// <summary>
		/// Gets the name of the assembly as defined in the .csproj file.
		/// </summary>
		/// <returns>The assembly name with extension.</returns>
		private string AssemblyName
		{
			get
			{
				var name = m_csprojFile.SelectSingleNode("/c:Project/c:PropertyGroup/c:AssemblyName",
					m_namespaceMgr);
				var type = m_csprojFile.SelectSingleNode("/c:Project/c:PropertyGroup/c:OutputType",
					m_namespaceMgr);
				string extension = ".dll";
				if (type.InnerText == "WinExe" || type.InnerText == "Exe")
					extension = ".exe";
				return name.InnerText + extension;
			}
		}

		/// <summary>
		/// Gets property groups for the different configurations from the project file
		/// </summary>
		private XmlNodeList ConfigNodes
		{
			get
			{
				return m_csprojFile.SelectNodes("/c:Project/c:PropertyGroup[c:DefineConstants]",
					m_namespaceMgr);
			}
		}

		private string GetProjectSubDir(string project)
		{
			var projectSubDir = Path.GetDirectoryName(m_mapProjFile[project]);
			projectSubDir = projectSubDir.Substring(m_fwroot.Length);
			projectSubDir = projectSubDir.Replace("\\", "/");
			if (projectSubDir.StartsWith("/Src/"))
				projectSubDir = projectSubDir.Substring(5);
			else
				if (projectSubDir.StartsWith("/Lib/src/"))
					projectSubDir = projectSubDir.Substring(9);
				else
					if (projectSubDir.StartsWith("/"))
						projectSubDir = projectSubDir.Substring(1);
			if (Path.DirectorySeparatorChar != '/')
				projectSubDir = projectSubDir.Replace('/', Path.DirectorySeparatorChar);
			return projectSubDir;
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
					LoadProjectFile(m_mapProjFile[project]);

					var isTestProject = project.EndsWith("Tests") ||
						project == "TestManager" ||
						project == "ProjectUnpacker";

					// <Choose> to define DefineConstants
					writer.WriteLine("\t<Choose>");
					var otherwiseBldr = new StringBuilder();
					var otherwiseAdded = false;
					var configs = new Dictionary<string, string>();
					foreach (XmlNode node in ConfigNodes)
					{
						var condition = node.Attributes["Condition"].InnerText;
						var tmp = condition.Substring(condition.IndexOf("==") + 2).Trim().Trim('\'');
						var configuration = tmp.Substring(0, tmp.IndexOf("|"));

						// Add configuration only once even if same configuration is contained
						// for multiple platforms, e.g. for AnyCpu and x64.
						if (configs.ContainsKey(configuration))
						{
							if (configs[configuration] !=
								node.SelectSingleNode("c:DefineConstants", m_namespaceMgr).InnerText.Replace(";", " "))
							{
								Log.LogError("Configuration {0} for project {1} is defined several times " +
											"but contains differing values for DefineConstants.", configuration, project);
							}
							continue;
						}
						configs.Add(configuration, node.SelectSingleNode("c:DefineConstants", m_namespaceMgr).InnerText.Replace(";", " "));

						writer.WriteLine("\t\t<When Condition=\" '$(config-capital)' == '{0}' \">", configuration);
						writer.WriteLine("\t\t\t<PropertyGroup>");
						writer.WriteLine("\t\t\t\t<{0}Defines>{1} CODE_ANALYSIS</{0}Defines>",
							project, configs[configuration]);
						writer.WriteLine("\t\t\t</PropertyGroup>");
						writer.WriteLine("\t\t</When>");
						if (condition.Contains("Debug") && !otherwiseAdded)
						{
							otherwiseBldr.AppendLine("\t\t<Otherwise>");
							otherwiseBldr.AppendLine("\t\t\t<PropertyGroup>");
							otherwiseBldr.AppendLine(
								string.Format("\t\t\t\t<{0}Defines>{1} CODE_ANALYSIS</{0}Defines>",
									project,
									node.SelectSingleNode("c:DefineConstants", m_namespaceMgr).InnerText.Replace(";", " ")));
							otherwiseBldr.AppendLine("\t\t\t</PropertyGroup>");
							otherwiseBldr.AppendLine("\t\t</Otherwise>");
							otherwiseAdded = true;
						}
					}
					writer.Write(otherwiseBldr.ToString());
					writer.WriteLine("\t</Choose>");
					writer.WriteLine();

					writer.Write("\t<Target Name=\"{0}\"", project);
					var bldr = new StringBuilder();
					bldr.Append("Initialize");	// ensure the output directories and version files exist.
					switch (project)
					{
						case "COMInterfaces":
							bldr.Append(";mktlbs");
							break;
						case "xWorks":
							// xWorks now references FlexUIAdapter.dll.
							// But, we don't discover that dependency, because for some bizarre
							// historical reason, the project that builds FlexUIAdapter.dll is called XCoreAdapterSilSidePane.
							bldr.Append(";XCoreAdapterSilSidePane");
							break;
						case "TeImportExportTests":
							// The TeImportExportTests require that the ScrChecks.dll is in DistFiles/Editorial Checks.
							// We don't discover that dependency because it's not a reference (LT-13777).
							bldr.Append(";ScrChecks");
							break;
						default:
							break;
					}
					var dependencies = m_mapProjDepends[project];
					dependencies.Sort();
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
						project.StartsWith("ManagedVwWindow"))
					{
						writer.Write(" Condition=\"'$(OS)'=='Unix'\"");
					}
					writer.WriteLine(">");

					// <MsBuild> task
					writer.WriteLine("\t\t<MSBuild Projects=\"{0}\"", m_mapProjFile[project].Replace(m_fwroot, "$(fwrt)"));
					writer.WriteLine("\t\t\tTargets=\"$(msbuild-target)\"");
					writer.WriteLine("\t\t\tProperties=\"$(msbuild-props);IntermediateOutputPath=$(dir-fwobj){0}{1}{0};DefineConstants=$({2}Defines);$(warningsAsErrors);WarningLevel=4\"",
						Path.DirectorySeparatorChar, GetProjectSubDir(project), project);
					writer.WriteLine("\t\t\tToolsVersion=\"4.0\"/>");

					// <Gendarme> verification task
					writer.WriteLine("\t\t<Gendarme ConfigurationFile=\"$(fwrt)/Build/Gendarme.MsBuild/fw-gendarme-rules.xml\"");
					writer.WriteLine("\t\t\tRuleSet=\"$({1})\" Assembly=\"$(dir-outputBase)/{0}\"",
						AssemblyName, isTestProject ? "verifyset-test" : "verifyset");
					writer.WriteLine("\t\t\tLogType=\"Html\" LogFile=\"$(dir-outputBase){0}{1}-gendarme.html\"", Path.DirectorySeparatorChar, project);
					writer.WriteLine("\t\t\tIgnoreFile=\"{0}/gendarme-{1}.ignore\"",
						Path.GetDirectoryName(m_mapProjFile[project].Replace(m_fwroot, "$(fwrt)")),
						project);
					writer.WriteLine("\t\t\tAutoUpdateIgnores=\"$(autoUpdateIgnores)\" VerifyFail=\"$(verifyFail)\"");
					writer.WriteLine("\t\t\tCondition=\"'$(config-capital)'=='Debug'\" />");

					if (isTestProject)
					{
						// <NUnit> task
						writer.WriteLine("\t\t<Message Text=\"Running unit tests for {0}\" />", project);
						writer.WriteLine("\t\t<NUnit Condition=\"'$(action)'=='test'\"");
						writer.WriteLine("\t\t\tAssemblies=\"$(dir-outputBase)/{0}.dll\"", project);
						writer.WriteLine("\t\t\tToolPath=\"$(fwrt)/Bin/NUnit/bin\"");
						writer.WriteLine("\t\t\tWorkingDirectory=\"$(dir-outputBase)\"");
						writer.WriteLine("\t\t\tOutputXmlFile=\"$(dir-outputBase)/{0}.dll-nunit-output.xml\"", project);
						writer.WriteLine("\t\t\tForce32Bit=\"$(useNUnit-x86)\"");
						writer.WriteLine("\t\t\tExcludeCategory=\"$(excludedCategories)\"");
						writer.WriteLine("\t\t\tTimeout=\"{0}\">", TimeoutForProject(project));
						// Don't continue on error. NUnit returns 0 even if there are failed tests.
						// A non-zero return code means a configuration error or that NUnit crashed
						// - we shouldn't ignore those.
						//writer.WriteLine("\t\t\tContinueOnError=\"true\" />");
						writer.WriteLine("\t\t\t<Output TaskParameter=\"FailedSuites\" ItemName=\"FailedSuites\"/>");
						writer.WriteLine("\t\t</NUnit>");
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
						project.EndsWith("Tests") ||			// These are tests.
						project == "TestUtils" ||				// This is a test.
						project == "TestManager" ||				// This is a test.
						project == "ProjectUnpacker")			// This is only used in tests.
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

				ProcessDependencyGraph(writer);

				writer.WriteLine("</Project>");
				writer.Flush();
				writer.Close();
			}
			Console.WriteLine("Created {0}", Path.Combine(m_fwroot, "Build/FieldWorks.targets"));
		}

		/// <summary>
		/// Return the timeout for running the tests in the given test project.
		/// </summary>
		/// <remarks>
		/// This could just use the biggest number for everything, but I prefer a finer grain.
		/// These timings from from a fairly old computer (Dell Precision 390), so should be
		/// safe for a wide variety of systems.
		/// </remarks>
		int TimeoutForProject(string project)
		{
			switch (project)
			{
				case "FwCoreDlgsTests":		// ~122 sec
				case "TeDllTests":			// ~122 sec (Mono 2/8/2013)
					return 240000;
				case "xWorksTests":			// ~244 sec (Mono 2/8/2013)
				case "FDOTests":			// ~143 sec
					return 360000;
				case "DiffViewTests":		// ~55 sec
				case "MGATests":			// ~72 sec
				case "TeImportExportTests":	// ~90 sec
					return 120000;
				case "PrintLayoutTests":	// ~22 sec
				case "ITextDllTests":		// ~26 sec
				case "DiscourseTests":		// ~36 sec
				case "TeEditingTests":		// ~30 sec (Mono 2/8/2013)
				case "SimpleRootSiteTests":	// ~30 sec (Mono 2/8/2013)
				case "FwCoreDlgControlsTests": // ~34 sec (overnight build machine 4/1/2013)
					return 60000;
				case "RootSiteTests":					// ~11 sec
				case "TeDialogsTests":					// ~11 sec
				case "TePrintLayoutTests":				// ~12 sec
				case "FwPrintLayoutComponentsTests":	// ~13 sec
				case "LexTextControlsTests":			// ~15 sec
				case "TePrintLayoutComponentsTests":	// ~17 sec
				case "FwControlsTests":					// ~19 sec
				case "XMLViewsTests":					// ~15 sec (Mono 2/8/2013)
					return 30000;
				case "PhraseTranslationHelperTests":	// ~8 sec
				case "CoreImplTests":					// ~9 sec
				case "TeScrInitializerTests":			// ~9 sec
				case "DetailControlsTests":				// ~10 sec (Mono 2/8/2013)
					return 15000;
				default:
					return 10000;
			}
		}

		void ProcessDependencyGraph(StreamWriter writer)
		{
#if false
			// The parallelized building isn't much faster, and tests don't all work right.
			// ----------------------------------------------------------------------------
			// Filter dependencies for those that are actually built.
			// Also collect all projects that don't depend on any other built projects.
			Dictionary<string, List<string>> mapProjInternalDepends = new Dictionary<string, List<string>>();
			List<HashSet<string>> groupDependencies = new List<HashSet<string>>();
			groupDependencies.Add(new HashSet<string>());
			int cProjects = 0;
			foreach (var project in m_mapProjFile.Keys)
			{
				if (project.StartsWith("SharpViews") ||		// These projects are experimental.
					project == "FxtExe" ||					// These projects weren't built by nant normally.
					project == "FixFwData" ||
					project.StartsWith("LinuxSmokeTest"))
				{
					continue;
				}
				var dependencies = new List<string>();
				foreach (var dep in m_mapProjDepends[project])
				{
					if (m_mapProjFile.ContainsKey(dep))
						dependencies.Add(dep);
				}
				if (project == "xWorksTests" && !dependencies.Contains("XCoreAdapterSilSidePane"))
					dependencies.Add("XCoreAdapterSilSidePane");
				if (dependencies.Count == 0)
					groupDependencies[0].Add(project);
				dependencies.Sort();
				mapProjInternalDepends.Add(project, dependencies);
				++cProjects;
			}
			if (groupDependencies[0].Count == 0)
				return;
			int num = 1;
			HashSet<string> total = new HashSet<string>(groupDependencies[0]);
			// Work through all the dependencies, collecting sets of projects that can be
			// built in parallel.
			while (total.Count < cProjects)
			{
				groupDependencies.Add(new HashSet<string>());
				foreach (var project in mapProjInternalDepends.Keys)
				{
					bool fAlready = false;
					for (int i = 0; i < groupDependencies.Count - 1; ++i)
					{
						if (groupDependencies[i].Contains(project))
						{
							fAlready = true;
							break;
						}
					}
					if (fAlready)
						continue;
					var dependencies = mapProjInternalDepends[project];
					if (total.IsSupersetOf(dependencies))
						groupDependencies[num].Add(project);
				}
				if (groupDependencies[num].Count == 0)
					break;
				foreach (var x in groupDependencies[num])
					total.Add(x);
				++num;
			}
			writer.WriteLine("<!--");
			writer.WriteLine("\tUsing this parallelization gains only 15% for building FieldWorks,");
			writer.WriteLine("\tand possibly nothing for running tests. (Although trials have shown");
			writer.WriteLine("\t1600+ new test failures when trying this parallelized setup!)");
			writer.WriteLine();
			for (int i = 0; i < groupDependencies.Count; ++i)
			{
				var targName = string.Format("cs{0:d03}", i+1);
				writer.Write("\t<Target Name=\"{0}\"", targName);
				var depends = String.Format("cs{0:d03}", i);
				if (i == 0)
					depends = "Initialize";
				if (groupDependencies[i].Contains("COMInterfaces"))
					depends = "mktlbs;" + depends;
				writer.WriteLine(" DependsOnTargets=\"{0}\">", depends);
				bool fIncludesTests = false;
				int count = 0;
				writer.Write("\t\t<MSBuild Projects=\"");
				foreach (var targ in groupDependencies[i])
				{
					if (count > 0)
						writer.Write(";");
					writer.Write(m_mapProjFile[targ].Replace(m_fwroot, "$(fwrt)"));
					++count;
					if (targ.EndsWith("Tests") ||
						targ == "TestManager" ||
						targ == "ProjectUnpacker")
					{
						fIncludesTests = true;
					}
				}
				writer.WriteLine("\"");
				writer.WriteLine("\t\t         Targets=\"$(msbuild-target)\"");
				writer.WriteLine("\t\t         Properties=\"$(msbuild-props)\"");
				writer.WriteLine("\t\t         BuildInParallel=\"true\"");
				writer.WriteLine("\t\t         ToolsVersion=\"4.0\"/>");
				if (fIncludesTests)
				{
					writer.WriteLine("\t\t<NUnit Condition=\"'$(action)'=='test'\"");
					writer.Write("\t\t       Assemblies=\"");
					count = 0;
					int timeout = 0;
					foreach (var targ in groupDependencies[i])
					{
						if (targ.EndsWith("Tests") ||
							targ == "TestManager" ||
							targ == "ProjectUnpacker")
						{
							if (count > 0)
								writer.Write(";");
							writer.Write("$(dir-outputBase)/{0}.dll", targ);
							++count;
							timeout += TimeoutForProject(targ);
						}
					}
					writer.WriteLine("\"");
					writer.WriteLine("\t\t       ToolPath=\"$(fwrt)/Bin/NUnit/bin\"");
					writer.WriteLine("\t\t       WorkingDirectory=\"$(dir-outputBase)\"");
					writer.WriteLine("\t\t       OutputXmlFile=\"$(dir-outputBase)/cs{0:d03}.dll-nunit-output.xml\"", i+1);
					writer.WriteLine("\t\t       Force32Bit=\"$(useNUnit-x86)\"");
					writer.WriteLine("\t\t       ExcludeCategory=\"$(excludedCategories)\"");
					writer.WriteLine("\t\t       Timeout=\"{0}\"", timeout);
					writer.WriteLine("\t\t       ContinueOnError=\"true\" />");
				}
				writer.WriteLine("\t</Target>");
				writer.WriteLine();
			}
			writer.WriteLine("\t<Target Name=\"csAll\" DependsOnTargets=\"cs{0:d03}\"/>", groupDependencies.Count);
			writer.WriteLine("-->");
			writer.WriteLine();
#endif
		}
	}
}
