using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GenerateFwTargets
{
	class GenerateTargets
	{
		string m_fwroot;
		Dictionary<string, string> m_mapProjFile = new Dictionary<string, string>();
		Dictionary<string, List<string>> m_mapProjDepends = new Dictionary<string, List<string>>();

		public GenerateTargets(string root)
		{
			m_fwroot = root;
		}

		/// <summary>
		/// Scan all the known csproj files under FWROOT for references, and then
		/// create msbuild target files accordingly.
		/// </summary>
		public void Generate()
		{
			var infoSrc = new DirectoryInfo(Path.Combine(m_fwroot, "Src"));
			CollectInfo(infoSrc);
			// These two projects from Lib appear to have been built through nant regularly.
			var infoSilUtil = new DirectoryInfo(Path.Combine(m_fwroot, "Lib/src/SilUtils"));
			CollectInfo(infoSilUtil);
			var infoEth = new DirectoryInfo(Path.Combine(m_fwroot, "Lib/src/Ethnologue"));
			CollectInfo(infoEth);
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
				return;		// Skip this project -- it should be under Lib, not Src!
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
			// Write all the targets and their dependencies.
			using (var writer = new StreamWriter(Path.Combine(m_fwroot, "Build/Fieldworks.targets")))
			{
				writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
				writer.WriteLine("<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\" ToolsVersion=\"4.0\">");
				writer.WriteLine("\t<UsingTask TaskName=\"RunNUnit\" AssemblyFile=\"FwBuildTasks.dll\"/>");
				writer.WriteLine();
				foreach (var project in m_mapProjFile.Keys)
				{
					writer.Write("\t<Target Name=\"{0}\"", project);
					StringBuilder bldr = new StringBuilder();
					List<string> dependencies = m_mapProjDepends[project];
					foreach (var dep in dependencies)
					{
						if (m_mapProjFile.ContainsKey(dep))
						{
							if (bldr.Length == 0)
								bldr.Append(dep);
							else
								bldr.AppendFormat(";{0}", dep);
						}
					}
					if (bldr.Length > 0)
						writer.Write(" DependsOnTargets=\"{0}\"", bldr.ToString());
					writer.WriteLine(">");
					writer.WriteLine("\t\t<MSBuild Projects=\"{0}\" Targets=\"$(msbuild-target)\" Properties=\"$(msbuild-props)\"/>",
						m_mapProjFile[project]);
					if (project.EndsWith("Tests"))
						writer.WriteLine("\t\t<RunNUnit Targets=\"{0}\" Condition=\"'$(action)'=='test'\"/>", project);
					writer.WriteLine("\t</Target>");
					writer.WriteLine();
				}
				writer.Write("\t<Target Name=\"everything\" DependsOnTargets=\"");
				bool first = true;
				foreach (var project in m_mapProjFile.Keys)
				{
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
		}
	}
}
