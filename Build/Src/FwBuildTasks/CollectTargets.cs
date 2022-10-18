// Copyright (c) 2012-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using FwBuildTasks;
using Microsoft.Build.Utilities;

namespace SIL.FieldWorks.Build.Tasks
{
	/// <summary>
	/// Collect projects from the FieldWorks repository tree, and generate a targets file
	/// for MSBuild (Mono xbuild).
	/// </summary>
	public class CollectTargets
	{
		public class StopTaskException : Exception
		{
			public StopTaskException(Exception innerException) : base(null, innerException)
			{
			}
		}

		private readonly string m_fwroot;
		private readonly Dictionary<string, string> m_mapProjFile = new Dictionary<string, string>();
		private readonly Dictionary<string, List<string>> m_mapProjDepends = new Dictionary<string, List<string>>();
		private TaskLoggingHelper Log { get; }
		private XmlDocument m_csprojFile;
		private XmlNamespaceManager m_namespaceMgr;
		private Dictionary<string, int> m_timeoutMap;

		private readonly string m_TimeoutValuesFilePath;
		private readonly string m_NUnitConsolePath;

		public CollectTargets(TaskLoggingHelper log, string timeoutValuesFilePath, string nUnitConsolePath)
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
			m_TimeoutValuesFilePath = timeoutValuesFilePath;
			m_NUnitConsolePath = nUnitConsolePath;
		}

		/// <summary>
		/// Scan all the known csproj files under FWROOT for references, and then
		/// create msbuild target files accordingly.
		/// </summary>
		public void Generate()
		{
			CollectInfo(new DirectoryInfo(Path.Combine(m_fwroot, "Src")));
			WriteTargetFiles();
		}

		/// <summary>
		/// Recursively scan the directory for csproj files.
		/// </summary>
		private void CollectInfo(DirectoryInfo dirInfo)
		{
			if (!dirInfo.Exists)
				return;
			foreach (var fi in dirInfo.GetFiles("*.csproj", SearchOption.AllDirectories))
			{
					ProcessCsProjFile(fi.FullName);
			}
		}

		/// <summary>
		/// Extract the reference information from a csproj file.
		/// </summary>
		private void ProcessCsProjFile(string filename)
		{
			var project = Path.GetFileNameWithoutExtension(filename);
			if (m_mapProjFile.ContainsKey(project) || m_mapProjDepends.ContainsKey(project))
			{
				Log.LogWarning("Project '{0}' has already been found elsewhere!", project);
				return;
			}
			m_mapProjFile.Add(project, filename);
			var dependencies = new List<string>();
			using (var reader = new StreamReader(filename))
			{
				var lineNumber = 0;
				while (!reader.EndOfStream)
				{
					lineNumber++;
					var line = reader.ReadLine()?.Trim();
					try
					{
						if (line == null)
							continue;
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
							var i0 = projectName.LastIndexOfAny(new[] {'\\', '/'});
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
							$"Error reading project references from {Path.GetFileName(filename)}. Invalid XML file?{Environment.NewLine}{e}");
						throw new StopTaskException(e);
					}
				}
				reader.Close();
			}
			m_mapProjDepends.Add(project, dependencies);
		}

		private void LoadProjectFile(string projectFile)
		{
			try
			{
				m_csprojFile = new XmlDocument();
				m_csprojFile.Load(projectFile);
				m_namespaceMgr = new XmlNamespaceManager(m_csprojFile.NameTable);
				m_namespaceMgr.AddNamespace("c", "http://schemas.microsoft.com/developer/msbuild/2003");
			}
			catch (XmlException e)
			{
				Log.LogError("GenerateFwTargets", null, null, projectFile, 0, 0, 0, 0,
					$"Error reading project name table from {Path.GetFileName(projectFile)}. Invalid XML file? {e.Message}");

				throw new StopTaskException(e);
			}
		}

		/// <summary>
		/// Gets the name of the assembly as defined in the .csproj file.
		/// </summary>
		/// <param name="projectName">The name of the project</param>
		/// <returns>The assembly name with extension.</returns>
		private string GetAssemblyName(string projectName)
		{
			var nameNode = m_csprojFile.SelectSingleNode("/c:Project/c:PropertyGroup/c:AssemblyName",
					m_namespaceMgr);
			var typeNode = m_csprojFile.SelectSingleNode("/c:Project/c:PropertyGroup/c:OutputType",
					m_namespaceMgr);
			var name = nameNode != null ? nameNode.InnerText : projectName;
			var extension = ".dll";
			if (typeNode != null && (typeNode.InnerText == "WinExe" || typeNode.InnerText == "Exe"))
					extension = ".exe";
			return name + extension;
		}

		/// <summary>
		/// Gets property groups for the different configurations from the project file
		/// </summary>
		private XmlNodeList ConfigNodes =>
			m_csprojFile.SelectNodes("/c:Project/c:PropertyGroup[c:DefineConstants]",
					m_namespaceMgr);

		private string GetProjectSubDir(string project)
		{
			var projectSubDir = Path.GetDirectoryName(m_mapProjFile[project]);
			projectSubDir = projectSubDir.Substring(m_fwroot.Length);
			projectSubDir = projectSubDir.Replace("\\", "/");
			if (projectSubDir.StartsWith("/Src/"))
				projectSubDir = projectSubDir.Substring(5);
			else if (projectSubDir.StartsWith("/"))
				projectSubDir = projectSubDir.Substring(1);
			if (Path.DirectorySeparatorChar != '/')
				projectSubDir = projectSubDir.Replace('/', Path.DirectorySeparatorChar);
			return projectSubDir;
		}

		private static bool IsMono
		{
			get
			{
				return Type.GetType("Mono.Runtime") != null;
			}
		}

		[DllImport("__Internal", EntryPoint = "mono_get_runtime_build_info")]
		private static extern string GetMonoVersion();

		/// <summary>
		/// Gets the version of the currently running Mono (e.g.
		/// "5.0.1.1 (2017-02/5077205 Thu May 25 09:16:53 UTC 2017)"), or the empty string
		/// on Windows.
		/// </summary>
		private static string MonoVersion => IsMono ? GetMonoVersion() : string.Empty;

		/// <summary>
		/// Used the collected information to write the needed target files.
		/// </summary>
		private void WriteTargetFiles()
		{
			var targetsFile = Path.Combine(m_fwroot, "Build/FieldWorks.targets");
			try
			{
				// Write all the C# targets and their dependencies.
				using (var writer = new StreamWriter(targetsFile))
				{
					writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
					writer.WriteLine("<!-- This file is automatically generated by the Setup target.  DO NOT EDIT! -->");
					writer.WriteLine("<!-- Unfortunately, the new one is generated after the old one has been read. -->");
					writer.WriteLine("<!-- 'msbuild /t:refreshTargets' generates this file and does nothing else. -->");
					writer.WriteLine("<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">");
					writer.WriteLine();
					foreach (var project in m_mapProjFile.Keys)
					{
						LoadProjectFile(m_mapProjFile[project]);

						var isTestProject = project.EndsWith("Tests") || project == "TestManager" || project == "ProjectUnpacker";

						var projectProperty = project.Replace(".", string.Empty);
						if (ConfigNodes.Count > 0)
						{
							// <Choose> to define DefineConstants
							writer.WriteLine("\t<Choose>");
							var otherwiseBldr = new StringBuilder();
							var otherwiseAdded = false;
							var configs = new Dictionary<string, string>();
							foreach (XmlNode node in ConfigNodes)
							{
								var condition = node.Attributes["Condition"].InnerText;
								var tmp = condition.Substring(condition.IndexOf("==") + 2).Trim()
									.Trim('\'');
								var configuration = tmp.Substring(0, tmp.IndexOf("|"));

								// Add configuration only once even if same configuration is contained
								// for multiple platforms, e.g. for AnyCpu and x64.
								if (configs.ContainsKey(configuration))
								{
									if (configs[configuration] != node
										.SelectSingleNode("c:DefineConstants", m_namespaceMgr)
										.InnerText.Replace(";", " "))
									{
										Log.LogError(
											"Configuration {0} for project {1} is defined several times " +
											"but contains differing values for DefineConstants.",
											configuration, project);
									}

									continue;
								}

								configs.Add(configuration,
									node.SelectSingleNode("c:DefineConstants", m_namespaceMgr)
										.InnerText.Replace(";", " "));

								writer.WriteLine(
									"\t\t<When Condition=\" '$(config-capital)' == '{0}' \">",
									configuration);
								writer.WriteLine("\t\t\t<PropertyGroup>");
								writer.WriteLine($"\t\t\t\t<{projectProperty}Defines>{configs[configuration]} CODE_ANALYSIS</{projectProperty}Defines>");
								writer.WriteLine("\t\t\t</PropertyGroup>");
								writer.WriteLine("\t\t</When>");
								if (condition.Contains("Debug") && !otherwiseAdded)
								{
									otherwiseBldr.AppendLine("\t\t<Otherwise>");
									otherwiseBldr.AppendLine("\t\t\t<PropertyGroup>");
									otherwiseBldr.AppendLine(string.Format(
										"\t\t\t\t<{0}Defines>{1} CODE_ANALYSIS</{0}Defines>",
										projectProperty,
										node.SelectSingleNode("c:DefineConstants", m_namespaceMgr)
											.InnerText.Replace(";", " ")));
									otherwiseBldr.AppendLine("\t\t\t</PropertyGroup>");
									otherwiseBldr.AppendLine("\t\t</Otherwise>");
									otherwiseAdded = true;
								}
							}

							writer.Write(otherwiseBldr.ToString());
							writer.WriteLine("\t</Choose>");
						}
						else
						{
							writer.WriteLine("\t<PropertyGroup>");
							writer.WriteLine($"\t\t<{projectProperty}Defines>CODE_ANALYSIS</{projectProperty}Defines>");
							writer.WriteLine("\t</PropertyGroup>");
						}
						writer.WriteLine();

						writer.Write("\t<Target Name=\"{0}\"", projectProperty);
						var bldr = new StringBuilder();
						bldr.Append("Initialize"); // ensure the output directories and version files exist.
						if (project == "ParatextImportTests" || project == "FwCoreDlgsTests")
						{
							// The ParatextImportTests and FwCoreDlgsTests require that the ScrChecks.dll be in DistFiles/Editorial Checks.
							// We don't discover that dependency because it's not a reference (LT-13777).
							bldr.Append(";ScrChecks");
						}
						var dependencies = m_mapProjDepends[project];
						dependencies.Sort();
						foreach (var dep in dependencies)
						{
							if (m_mapProjFile.ContainsKey(dep))
								bldr.AppendFormat(";{0}", dep.Replace(".", string.Empty));
						}
						writer.Write($" DependsOnTargets=\"{bldr}\"");

						if (project == "MigrateSqlDbs")
						{
							writer.Write(" Condition=\"'$(OS)'=='Windows_NT'\"");
						}
						if (project.StartsWith("ManagedVwWindow"))
						{
							writer.Write(" Condition=\"'$(OS)'=='Unix'\"");
						}
						writer.WriteLine(">");

						// <MsBuild> task
						writer.WriteLine($"\t\t<MSBuild Projects=\"{m_mapProjFile[project].Replace(m_fwroot, "$(fwrt)")}\"");
						writer.WriteLine("\t\t\tTargets=\"$(msbuild-target)\"");
						writer.WriteLine("\t\t\tProperties=\"$(msbuild-props);IntermediateOutputPath=$(dir-fwobj){0}{1}{0};DefineConstants=$({2}Defines);$(warningsAsErrors);WarningLevel=4\"/>",
							Path.DirectorySeparatorChar, GetProjectSubDir(project), projectProperty);
						// <Clouseau> verification task
						writer.WriteLine($"\t\t<Clouseau Condition=\"'$(Configuration)' == 'Debug'\" AssemblyPathname=\"$(dir-outputBaseFramework)/{GetAssemblyName(project)}\"/>");

						if (isTestProject)
						{
							// <NUnit> task
							writer.WriteLine($"\t\t<Message Text=\"Running unit tests for {project}\" />");
							writer.WriteLine("\t\t<NUnit3 Condition=\"'$(action)'=='test'\"");
							writer.WriteLine($"\t\t\tAssemblies=\"$(dir-outputBaseFramework)/{project}.dll\"");
							writer.WriteLine($"\t\t\tToolPath=\"{m_NUnitConsolePath}\"");
							writer.WriteLine("\t\t\tWorkingDirectory=\"$(dir-outputBaseFramework)\"");
							writer.WriteLine($"\t\t\tOutputXmlFile=\"$(dir-outputBaseFramework)/{project}.dll-nunit-output.xml\"");
							writer.WriteLine("\t\t\tForce32Bit=\"$(useNUnit-x86)\"");
							writer.WriteLine("\t\t\tExcludeCategory=\"$(excludedCategories)\"");
							// Don't continue on error. NUnit returns 0 even if there are failed tests.
							// A non-zero return code means a configuration error or that NUnit crashed
							// - we shouldn't ignore those.
							//writer.WriteLine("\t\t\tContinueOnError=\"true\"");
							writer.WriteLine("\t\t\tFudgeFactor=\"$(timeoutFudgeFactor)\"");
							writer.WriteLine($"\t\t\tTimeout=\"{TimeoutForProject(project)}\">");
							writer.WriteLine("\t\t\t<Output TaskParameter=\"FailedSuites\" ItemName=\"FailedSuites\"/>");
							writer.WriteLine("\t\t</NUnit3>");
							writer.WriteLine($"\t\t<Message Text=\"Finished building {project}.\" Condition=\"'$(action)'!='test'\"/>");
							writer.WriteLine($"\t\t<Message Text=\"Finished building {project} and running tests.\" Condition=\"'$(action)'=='test'\"/>");
							// Generate dotCover task
							GenerateDotCoverTask(writer, new[] {project}, $"{project}.coverage.xml");
						}
						else
						{
							writer.WriteLine($"\t\t<Message Text=\"Finished building {project}.\"/>");
						}
						writer.WriteLine("\t</Target>");
						writer.WriteLine();
					}
					writer.Write("\t<Target Name=\"allCsharp\" DependsOnTargets=\"");
					var targets = new StringBuilder();
					foreach (var project in m_mapProjFile.Keys)
					{
						if (project == "FxtExe")
							continue;

						if (targets.Length > 0)
							targets.Append(";");
						targets.Append(project.Replace(".", string.Empty));
					}
					writer.Write(targets);
					writer.WriteLine("\"/>");
					writer.WriteLine();
					writer.Write("\t<Target Name=\"allCsharpNoTests\" DependsOnTargets=\"");
					targets.Clear();
					foreach (var project in m_mapProjFile.Keys)
					{
						if (project == "FxtExe" ||
							project.EndsWith("Tests") ||  // These are tests.
							project == "ProjectUnpacker") // This is only used in tests.
						{
							continue;
						}

						if (targets.Length > 0)
							targets.Append(";");
						targets.Append(project.Replace(".", string.Empty));
					}
					writer.Write(targets);
					writer.WriteLine("\"/>");

					writer.WriteLine("</Project>");
					writer.Flush();
					writer.Close();
				}
				Console.WriteLine("Created {0}", targetsFile);
			}
			catch (Exception e)
			{
				var badFile = targetsFile + ".bad";
				File.Move(targetsFile, badFile);
				Console.WriteLine("Failed to Create FieldWorks.targets bad result stored in {0}{1}{2}", badFile, Environment.NewLine, e);
				throw new StopTaskException(e);
			}
		}

		private void GenerateDotCoverTask(TextWriter writer, IEnumerable<string> projects, string outputXml)
		{
			var assemblyList = projects.Aggregate("", (current, proj) => current + $"$(dir-outputBaseFramework)/{proj}.dll;");
			writer.WriteLine($"\t\t<Message Text=\"Running coverage analysis for {string.Join(", ", projects)}\" Condition=\"'$(action)'=='cover'\"/>");
			writer.WriteLine( "\t\t<GenerateTestCoverageReport Condition=\"'$(action)'=='cover'\"");
			writer.WriteLine($"\t\t\tAssemblies=\"{assemblyList}\"");
			writer.WriteLine( $"\t\t\tNUnitConsoleExe=\"{m_NUnitConsolePath}nunit3-console.exe\"");
			writer.WriteLine( "\t\t\tDotCoverExe=\"$(DOTCOVER_HOME)/dotcover.exe\"");
			writer.WriteLine( "\t\t\tWorkingDirectory=\"$(dir-outputBaseFramework)\"");
			writer.WriteLine($"\t\t\tOutputXmlFile=\"$(dir-outputBaseFramework)/{outputXml}\"/>");
		}

		/// <summary>
		/// Return the timeout for running the tests in the given test project.
		/// </summary>
		private int TimeoutForProject(string project)
		{
			if (m_timeoutMap == null)
			{
				var timeoutDocument = XDocument.Load(m_TimeoutValuesFilePath);
				m_timeoutMap = new Dictionary<string, int>();
				var testTimeoutValuesElement = timeoutDocument.Root;
				m_timeoutMap["default"] = int.Parse(testTimeoutValuesElement.Attribute("defaultTimeLimit").Value);
				foreach (var timeoutElement in timeoutDocument.Root.Descendants("TimeoutGroup"))
				{
					var timeout = int.Parse(timeoutElement.Attribute("timeLimit").Value);
					foreach (var projectElement in timeoutElement.Descendants("Project"))
					{
						m_timeoutMap[projectElement.Attribute("name").Value] = timeout;
					}
				}
			}
			var timeoutTime = (m_timeoutMap.ContainsKey(project) ? m_timeoutMap[project] : m_timeoutMap["default"]) * 1000;
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				timeoutTime = timeoutTime * 2;
			}
			return timeoutTime;
		}
	}
}
