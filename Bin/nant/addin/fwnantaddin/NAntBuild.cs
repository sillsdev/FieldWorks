using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using EnvDTE;
using EnvDTE80;
//using NAnt.Core;

namespace FwNantAddin2
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for NAntBuild.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class NAntBuild: IDisposable
	{
		#region Modifiers struct
		public struct Modifiers
		{
// ReSharper disable InconsistentNaming
			public bool fClean;
			public bool fTest;
			public bool fNoDep;
			public bool fForceTests;
// ReSharper restore InconsistentNaming

			public Modifiers(bool clean, bool test, bool noDep, bool forceTests)
			{
				fClean = clean;
				fTest = test;
				fNoDep = noDep;
				fForceTests = forceTests;
			}
		}
		#endregion
		#region Private class TargetValue
		private class TargetValue
		{
			public TargetValue(string bf, string pn)
			{
				Buildfile = bf;
				ProjectName = pn;
			}
			public readonly string Buildfile;
			public readonly string ProjectName;
		}
		#endregion

#if DEBUG
		private PaneWriter m_outputWindowPane;
#endif
		private PaneWriter m_outputBuild;
		private NantRunner m_nantRunner;
		internal event NantRunner.BuildStatusHandler BuildStatusChange;
		private readonly Hashtable m_Targets = new Hashtable();
		private string m_buildFile;

		private const string Nant = "nant.exe";
		private const string m_BuildPaneName = "NAnt build";
#if DEBUG
		private const string m_DebugPaneName = "NAnt Debug Information";
#endif

		#region IDisposable Members & Co.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="T:FwNantAddin2.NAntBuild"/> is reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~NAntBuild()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disposes the specified disposing.
		/// </summary>
		/// <param name="disposing">if set to <c>true</c> [disposing].</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Dispose managed resources here
				if (m_outputBuild != null)
					m_outputBuild.Dispose();
#if DEBUG
				if (m_outputWindowPane != null)
					m_outputWindowPane.Dispose();
#endif
			}
			m_outputBuild = null;
#if DEBUG
			m_outputWindowPane = null;
#endif
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the build pane.
		/// </summary>
		/// <value>The name of the build pane.</value>
		/// ------------------------------------------------------------------------------------
		public string BuildPaneName
		{
			get { return m_BuildPaneName; }
		}

#if DEBUG
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the debug pane.
		/// </summary>
		/// <value>The name of the debug pane.</value>
		/// ------------------------------------------------------------------------------------
		public string DebugPaneName
		{
			get { return m_DebugPaneName; }
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the DTE.
		/// </summary>
		/// <value>The DTE.</value>
		/// ------------------------------------------------------------------------------------
		public DTE2 DTE { get; set; }

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Use this PaneWriter for all output messages.
		/// </summary>
		/// <value>The output build.</value>
		/// ------------------------------------------------------------------------------------------
		public PaneWriter OutputBuild
		{
			get
			{
				if (m_outputBuild == null)
				{
					m_outputBuild = new PaneWriter(DTE, m_BuildPaneName);
				}
				return m_outputBuild;
			}
		}

#if DEBUG
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this PaneWriter for all output messages.
		/// </summary>
		/// <value>The output build debug.</value>
		/// ------------------------------------------------------------------------------------
		public PaneWriter OutputBuildDebug
		{
			get
			{
				if (m_outputWindowPane == null)
				{
					m_outputWindowPane = new DebugPaneWriter(DTE, m_DebugPaneName);
				}
				return m_outputWindowPane;
			}
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path and name of the NAnt executable.
		/// </summary>
		/// <value>The path and name.</value>
		/// ------------------------------------------------------------------------------------
		private static string NAnt
		{
			get
			{
				string nant = Path.Combine(Settings.Default.NantPath, Nant);
				if (!File.Exists(nant))
					return Nant; // not at specified location - so try in path
				return nant;
			}
		}
		#endregion

		#region Methods for dealing with NAnt
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Figure out the target name from the project path and name.
		/// </summary>
		/// <param name="mods">The modification button states</param>
		/// <param name="prjPath">Path and name of project</param>
		/// <returns>Target name</returns>
		/// <remarks>Get the file name of the project. If it ends in 'Tests' look at the
		/// base directory. If it is of the format PROJECT\PROJECTTests\PROJECTTests.csproj
		/// then the NAnt build system will build the tests when building PROJECT, so the
		/// desired target name is PROJECT.
		/// If m_fNoDep is <c>true</c> then add '-nodep' to the target name.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		private static string GetTargetName(Modifiers mods, string prjPath)
		{
			if (string.IsNullOrEmpty(prjPath))
				return string.Empty;

			string dir = Path.GetDirectoryName(prjPath);
			string filename = Path.GetFileNameWithoutExtension(prjPath);
			string target = filename;
			if (filename.EndsWith("Tests"))
			{
				string basePart = filename.Substring(0, filename.LastIndexOf("Tests"));
				string parentDir = Path.GetFullPath(Path.Combine(dir, ".."));
				if (parentDir.ToLower().EndsWith(basePart.ToLower()))
					target = basePart;
			}
			if (mods.fNoDep)
				target += "-nodep";
			return target;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the build. This involves activating the output window, saving all files
		/// and printing a message.
		/// </summary>
		/// <param name="msg"></param>
		/// ------------------------------------------------------------------------------------
		private void StartBuild(string msg)
		{
			OutputBuild.Clear();
			DTE.Windows.Item(Constants.vsWindowKindOutput).Activate();
			DTE.ExecuteCommand("File.SaveAll", string.Empty);
			OutputBuild.Write(msg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tries to find the buildfile and fwroot based on the passed in path
		/// </summary>
		/// <param name="path">Path</param>
		/// <param name="buildFile">The build file.</param>
		/// <param name="fwroot">[out] fwroot path</param>
		/// ------------------------------------------------------------------------------------
		private static void RetrieveBuildFile(string path, out string buildFile, out string fwroot)
		{
			// try to find the right base directory based on the project path
			using (var options = new AddinOptions())
			{
				buildFile = string.Empty;
				fwroot = string.Empty;
				foreach(string baseDir in options.BaseDirectories)
				{
					string dirToTest = baseDir + "\\";
					if (path.ToLower().StartsWith(dirToTest.ToLower()))
					{
						buildFile = Path.GetFullPath(Path.Combine(baseDir, options.Buildfile));
						fwroot = baseDir;
						break;
					}
				}

				// no success, so take first base directory that we have, or just build file
				if (buildFile == string.Empty)
				{
					if (options.BaseDirectories.Length > 0)
					{
						fwroot = options.BaseDirectories[0];
						buildFile = Path.GetFullPath(Path.Combine(fwroot, options.Buildfile));
					}
					else
					{
						buildFile = Path.GetFullPath(options.Buildfile);
						fwroot = Path.GetDirectoryName(buildFile);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform the build with NAnt
		/// </summary>
		/// <param name="mods">The modification buttons states.</param>
		/// <param name="projectName">Name(s) of the target</param>
		/// <param name="config">Configuration (Debug/Release...)</param>
		/// <param name="projectPath">Full path to the project</param>
		/// <param name="fWait"><c>true</c> to wait for the build to finish</param>
		/// <returns><c>true</c> if the build was successful</returns>
		/// <exception cref="TargetException">If target doesn't exist, so that a NAnt build
		/// can't be performed.</exception>
		/// ------------------------------------------------------------------------------------
		private bool InternalBuildProject(Modifiers mods, string projectName, string config,
			string projectPath, bool fWait)
		{
			try
			{
				string buildFile;
				string fwroot;
				RetrieveBuildFile(projectPath, out buildFile, out fwroot);

				FindTarget(ref buildFile, ref projectName, fwroot, projectPath);
				m_buildFile = buildFile;

				//string cmdLine = string.Format("-buildfile:\"c:\\fwtexp\\bin\\FieldWorks.build\" VSCompile -D:sln=\"{0}\"",
				string action;
				if (mods.fClean)
					action = "clean";
				else if (mods.fTest)
				{
					action = mods.fForceTests ? "forcetests test" : "test";
				}
				else
					action = "buildtest";
				string cmdLine = string.Format(
					"-e+ -buildfile:\"{0}\" {1} {2} {3} {4}",
					buildFile, TargetFramework, action, config, projectName);
				string workingDirectory = Path.GetFullPath(Path.GetDirectoryName(buildFile));

				OutputBuild.Activate();
				m_nantRunner = new NantRunner(NAnt, cmdLine, workingDirectory,
					new AddinLogListener(this, false),
					BuildStatusChange);
				if (fWait)
				{
					if (m_nantRunner.RunSync() != 0)
					{
						return false;
					}
				}
				else
					m_nantRunner.Run();
			}
			catch(TargetException)
			{
				throw;
			}
			catch(ThreadAbortException)
			{
				throw;
			}
			catch (Exception e)
			{
				if (m_nantRunner != null && m_nantRunner.IsRunning)
					m_nantRunner.Abort();
				OutputBuild.Write("\nINTERNAL ERROR\n\t");
				OutputBuild.WriteLine(e.Message);
				return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the target framework.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string TargetFramework
		{
			get
			{
				string targetFramework;
				if (string.IsNullOrEmpty(Settings.Default.TargetFramework))
					targetFramework = string.Empty;
				else
					targetFramework = "-t:" + Settings.Default.TargetFramework;
				return targetFramework;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the target.
		/// </summary>
		/// <param name="buildFile">The build file.</param>
		/// <param name="projectName">Name of the project.</param>
		/// <param name="fwroot">The fwroot.</param>
		/// <param name="projectPath">The project path.</param>
		/// ------------------------------------------------------------------------------------
		private void FindTarget(ref string buildFile, ref string projectName, string fwroot,
			string projectPath)
		{
			lock(m_Targets)
			{
				string originalProjectName = projectName;
				bool fTargetExists = true;
				if (m_Targets.Contains(originalProjectName))
				{
					var val = (TargetValue)m_Targets[originalProjectName];
					if (val == null)
						fTargetExists = false;
					else
					{
						projectName = val.ProjectName;
						buildFile = val.Buildfile;
					}
				}
				else
				{
					// If the target exists in the build file, we compile that.
					// If it doesn't exist, we look for a file '*.build' in the project's
					// directory and build with that.
					// If even that doesn't exist, we quit and let Visual Studio do the compile.
					// If the buildFile is the same as what we had before, we speed up things
					// and don't perform the check
					if ((buildFile == m_buildFile && Path.GetExtension(projectPath) == ".csproj")
						|| TargetExists(buildFile, projectName, fwroot))
					{
						// we already have the correct build file
					}
					else
					{
						if (!string.IsNullOrEmpty(projectPath))
						{
							var dirInfo = new DirectoryInfo(Path.GetDirectoryName(projectPath));
							FileInfo[] buildfiles = dirInfo.GetFiles("*.build");
							string tmpBuildfile = string.Empty;
							if (buildfiles.Length == 1)
							{	// there is exactly one *.build file
								tmpBuildfile = buildfiles[0].FullName;
							}
							else if (buildfiles.Length > 1)
							{
								foreach (FileInfo fileInfo in buildfiles)
								{
									if (fileInfo.Name == "build.build")
										tmpBuildfile = fileInfo.FullName;
								}
							}
							if (tmpBuildfile != string.Empty)
							{
								OutputBuild.WriteLine(string.Format("Target \"{2}\" not found in {0}, using {1} instead",
									buildFile, tmpBuildfile, projectName));

								fwroot = Path.GetDirectoryName(Path.GetFullPath(projectPath));
								if (!TargetExists(tmpBuildfile, projectName, fwroot))
									projectName = "all";
								buildFile = tmpBuildfile;
								// projectName = string.Format("{0} -D:fwroot=\"{1}\"", projectName, fwroot);
							}
							else
							{
								fTargetExists = false;
							}
						}
						else
						{
							fTargetExists = false;
						}
					}
				}

				if (fTargetExists)
				{
					m_Targets[originalProjectName] = new TargetValue(buildFile, projectName);
				}
				else
				{
					m_Targets[originalProjectName] = null;
					System.Diagnostics.Debug.WriteLine("Target doesn't exist");
					OutputBuild.WriteLine(string.Format("Target \"{1}\" not found in {0}, performing VS build",
						buildFile, projectName));
					throw new TargetException("Target doesn't exist");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if at least one of the targets exists in the build file.
		/// </summary>
		/// <param name="buildfile">The name of the build file</param>
		/// <param name="projectNames">The target(s)</param>
		/// <param name="baseDir">The base directory</param>
		/// <returns><c>true</c> if the target exists in the build file.</returns>
		/// ------------------------------------------------------------------------------------
		private bool TargetExists(string buildfile, string projectNames, string baseDir)
		{
			string addinDir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
			string cmdLine = string.Format(
				"-nologo -buildfile:\"{0}\\addin.build.xml\" -D:possibleTargets=\"{1}\" " +
				"-D:buildfile=\"{2}\" -D:fwroot=\"{3}\"",
				addinDir, projectNames, buildfile, baseDir);
			var runner = new NantRunner(NAnt, cmdLine, addinDir,
				new AddinLogListener(
#if DEBUG
					this,
#else
					null,
#endif
					true),
				BuildStatusChange);
			return runner.RunSync() == 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Runs Nant.
		/// </summary>
		/// <param name="cmdLine">The CMD line.</param>
		/// ------------------------------------------------------------------------------------
		public void RunNant(string cmdLine)
		{
			try
			{
				string buildFile;
				string fwroot;
				RetrieveBuildFile(DTE.Solution.FullName, out buildFile, out fwroot);

				cmdLine = string.Format("-e+ -buildfile:\"{0}\" {1} {2}", buildFile, TargetFramework, cmdLine);
				string workingDir = Path.GetFullPath(Path.GetDirectoryName(buildFile));

				StartBuild(string.Format("------ Build started: {0} ------\n", cmdLine));
				OutputBuild.Activate();
				m_nantRunner = new NantRunner(NAnt, cmdLine, workingDir,
					new AddinLogListener(this, false),
					BuildStatusChange);
				m_nantRunner.Run();
			}
			catch(ThreadAbortException)
			{
				throw;
			}
			catch (Exception e)
			{
				if (m_nantRunner != null && m_nantRunner.IsRunning)
					m_nantRunner.Abort();
				OutputBuild.Write("\nINTERNAL ERROR\n\t");
				OutputBuild.WriteLine(e.Message);
			}
		}

		#endregion

		#region Build methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build the solution
		/// </summary>
		/// <param name="mods">Modifiers</param>
		/// <param name="fRebuild"><c>true</c> to do a rebuild</param>
		/// <param name="solutionName">Name of the solution</param>
		/// <param name="configName">Name of the configuration</param>
		/// <param name="unused">Not used</param>
		/// <param name="projects">Array of project names</param>
		/// <returns>
		/// 	<c>true</c> if successful, otherwise <c>false</c>
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool BuildSolution(Modifiers mods, bool fRebuild, string solutionName,
			string configName, string unused, string[] projects)
		{
			string msg = string.Format(
				"------ {0} started: Solution: {1}, Configuration: {2} ------\n",
				fRebuild ? "Rebuild" : "Build", solutionName, configName);
			StartBuild(msg);

			var prjBldr = new StringBuilder();
			foreach (string prj in projects)
			{
				prjBldr.Append(GetTargetName(mods, prj));
				prjBldr.Append(" ");
			}

			bool fRet = true;
			try
			{
				InternalBuildProject(mods, prjBldr.ToString(), configName,
					solutionName, false);
			}
			catch(TargetException)
			{
				fRet = false;
			}

			return fRet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build a project
		/// </summary>
		/// <param name="mods">Modifiers</param>
		/// <param name="fRebuild"><c>true</c> to do a rebuild</param>
		/// <param name="projectName">Name of the project</param>
		/// <param name="configName">Name of the configuration</param>
		/// <param name="platformName">Name of the platform</param>
		/// <param name="projects">Array of project names</param>
		/// <param name="fWait"><c>true</c> to wait for the build to finish</param>
		/// <returns>
		/// 	<c>true</c> if successful, otherwise <c>false</c>
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool BuildProject(Modifiers mods, bool fRebuild, string projectName,
			string configName, string platformName, string[] projects, bool fWait)
		{
			System.Diagnostics.Debug.Assert(projects.Length > 0);

			if (projects[0] == null || projects[0] == string.Empty)
				return true;

			// eliminate duplicate projects (e.g. TeDll and TeDllTests)
			var targets = new Hashtable();
			foreach(string prj in projects)
			{
				string target = GetTargetName(mods, prj);
				if (target == string.Empty)
					continue;
				System.Diagnostics.Debug.WriteLine("Project=" + prj + ", target=" + target);
				if (targets.ContainsValue(target))
				{
					if (!targets.ContainsKey(prj))
						targets.Add(prj, null);
				}
				else
					targets.Add(prj, target);
			}
			bool fRet = true;
			try
			{
				string msg = string.Format(
					"------ {0} started: Project: {1}, Configuration: {2} {3} ------\n",
					fRebuild ? "Rebuild" : "Build",
					projectName, configName, platformName);
				StartBuild(msg);

				for (int i = 0; i < projects.Length; i++)
				{
					var target = (string)targets[projects[i]];
					if (target != null)
					{
						bool fOk = InternalBuildProject(mods, target, configName,
							projects[i], fWait);
						if (!fOk)
						{
							fRet = false;
							break;
						}
					}
				}
			}
			catch(ThreadAbortException)
			{
				CancelBuild();
				throw;
			}

			return fRet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cancel the current build
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CancelBuild()
		{
			if (IsRunning)
				m_nantRunner.Abort();
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Returns <c>true</c> if a build is currently running
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is running; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------------
		public bool IsRunning
		{
			get { return m_nantRunner != null && m_nantRunner.IsRunning; }
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a new project gets opened.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OnProjectOpened()
		{
			lock(m_Targets)
			{
				m_Targets.Clear();
			}
		}
	}
}
