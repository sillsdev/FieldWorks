using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualStudio.CommandBars;
using Extensibility;
using EnvDTE;
using EnvDTE80;
//using NAnt.Core;

namespace FwNantAddin2
{
	/// <summary>
	/// Summary description for CmdHandler.
	/// </summary>
	public class CmdHandler: IDisposable
	{
		/// <summary>The order in which the toolbar buttons appear</summary>
		private enum ButtonOrder
		{
			EnableAddin = 1,
			Cancel,
			Clean,
			Tests,
			ForceTests,
			NoDep
		}

		private DTE2 m_dte;
		private bool m_fClean = false;
		private bool m_fTest = false;
		private bool m_fNoDep = true;
		private bool m_fForceTests = false;
		private bool m_fAddinEnabled = false;
		internal CommandBarComboBox m_cmbBuild = null;
		private Hashtable m_comboList = new Hashtable();
		private NAntBuild m_nantBuild = new NAntBuild();
		/// <summary>For limiting build to one at a time</summary>
		private Semaphore m_Semaphore = new Semaphore(1, 1);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CmdHandler"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CmdHandler()
		{
			m_nantBuild.BuildStatusChange += new NantRunner.BuildStatusHandler(BuildStatusChange);
		}

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
		/// <see cref="T:FwNantAddin2.CmdHandler"/> is reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~CmdHandler()
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
				m_nantBuild.Dispose();
			}
			m_nantBuild = null;
		}
		#endregion

		#region Properties
		internal CommandBarButton EnableAddinBtn
		{
			get
			{
				CommandBar cmdBar = ((CommandBars)DTE.CommandBars)[Connect.kCmdBarName];
				return (CommandBarButton)cmdBar.Controls[ButtonOrder.EnableAddin];
			}
		}

		internal CommandBarButton CancelBtn
		{
			get
			{
				CommandBar cmdBar = ((CommandBars)DTE.CommandBars)[Connect.kCmdBarName];
				CommandBarButton btnCancel = (CommandBarButton)cmdBar.Controls[ButtonOrder.Cancel];
				btnCancel.Enabled = m_nantBuild.IsRunning;
				return btnCancel;
			}
		}

		internal CommandBarButton CleanBtn
		{
			get
			{
				CommandBar cmdBar = ((CommandBars)DTE.CommandBars)[Connect.kCmdBarName];
				return (CommandBarButton)cmdBar.Controls[ButtonOrder.Clean];
			}
		}

		internal CommandBarButton TestBtn
		{
			get
			{
				CommandBar cmdBar = ((CommandBars)DTE.CommandBars)[Connect.kCmdBarName];
				return (CommandBarButton)cmdBar.Controls[ButtonOrder.Tests];
			}
		}

		internal CommandBarButton ForceTestsBtn
		{
			get
			{
				CommandBar cmdBar = ((CommandBars)DTE.CommandBars)[Connect.kCmdBarName];
				return (CommandBarButton)cmdBar.Controls[ButtonOrder.ForceTests];
			}
		}

		internal CommandBarButton NoDepBtn
		{
			get
			{
				CommandBar cmdBar = ((CommandBars)DTE.CommandBars)[Connect.kCmdBarName];
				return (CommandBarButton)cmdBar.Controls[ButtonOrder.NoDep];
			}
		}

		public bool EnableAddin
		{
			get { return m_fAddinEnabled; }
			set
			{
				m_fAddinEnabled = value;
				PressButton(EnableAddinBtn, m_fAddinEnabled);
			}
		}

		public bool Clean
		{
			get { return m_fClean; }
			set
			{
				m_fClean = value;
				PressButton(CleanBtn, m_fClean);
			}
		}
		public bool Test
		{
			get { return m_fTest; }
			set
			{
				m_fTest = value;
				PressButton(TestBtn, m_fTest);
			}
		}
		public bool NoDep
		{
			get { return m_fNoDep; }
			set
			{
				m_fNoDep = value;
				PressButton(NoDepBtn, m_fNoDep);
			}
		}
		public bool ForceTests
		{
			get { return m_fForceTests; }
			set
			{
				m_fForceTests = value;
				PressButton(ForceTestsBtn, m_fForceTests);
			}
		}

		public DTE2 DTE
		{
			get
			{
				return m_dte;
			}
			set
			{
				m_dte = value;
				m_nantBuild.DTE = m_dte;
			}
		}
		#endregion

		#region Command Handler
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build the entire solution.
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="id"></param>
		/// <param name="customIn"></param>
		/// <param name="customOut"></param>
		/// <param name="fCancelDefault"></param>
		/// ------------------------------------------------------------------------------------
		public void OnBuildSolution(string guid, int id, object customIn, object customOut,
			ref bool fCancelDefault)
		{
			if (!m_fAddinEnabled)
				return;

			if (m_nantBuild.IsRunning)
			{
				fCancelDefault = true;
				return;
			}

			try
			{
				fCancelDefault = false;
				bool fRebuild = (id != 882);
				EnvDTE.SolutionConfiguration config =
					DTE.Solution.SolutionBuild.ActiveConfiguration;

				int nProjects = DTE.Solution.SolutionBuild.ActiveConfiguration.SolutionContexts.Count;
				string[] projects = new string[nProjects];
				for (int i = 0; i < nProjects; i++)
				{
					SolutionContext prj = DTE.Solution.SolutionBuild.ActiveConfiguration.SolutionContexts.Item(i+1);
					projects[i] = prj.ProjectName;
				}

				fCancelDefault = m_nantBuild.BuildSolution(Modifiers, fRebuild, DTE.Solution.FullName,
					config.Name, string.Empty, projects);
				ResetFlags();
			}
			catch(Exception e)
			{
				Debug.WriteLine("OnBuildSolution: Got exception: " + e.Message);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build one project
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="id"></param>
		/// <param name="customIn"></param>
		/// <param name="customOut"></param>
		/// <param name="fCancelDefault"></param>
		/// ------------------------------------------------------------------------------------
		public void OnBuildProject(string guid, int id, object customIn, object customOut,
			ref bool fCancelDefault)
		{
			if (!m_fAddinEnabled)
				return;

			if (m_nantBuild.IsRunning)
			{
				fCancelDefault = true;
				return;
			}
			try
			{
				fCancelDefault = false;
				bool fRebuild = !(id == 886 || id == 892 || (id >= 979 && id <= 988));

				EnvDTE.SolutionConfiguration config =
					DTE.Solution.SolutionBuild.ActiveConfiguration;

				Array activeProjects = (Array)DTE.ActiveSolutionProjects;
				EnvDTE.Project vsProject = (EnvDTE.Project)activeProjects.GetValue(0);

				fCancelDefault = m_nantBuild.BuildProject(Modifiers, fRebuild, vsProject.Name,
					config.Name, config.SolutionContexts.Item(1).PlatformName,
					new string[] { vsProject.FullName }, false);
				ResetFlags();
			}
			catch(Exception e)
			{
				Debug.WriteLine("OnBuildProject: Got exception: " + e.Message);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cancel the build - does not work
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="id"></param>
		/// <param name="customIn"></param>
		/// <param name="customOut"></param>
		/// <param name="fCancelDefault"></param>
		/// ------------------------------------------------------------------------------------
		public void OnBuildCancel(string guid, int id, object customIn, object customOut,
			ref bool fCancelDefault)
		{
			System.Diagnostics.Debug.WriteLine("Build canceled");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start debugging.
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="id"></param>
		/// <param name="customIn"></param>
		/// <param name="customOut"></param>
		/// <param name="fCancelDefault"></param>
		/// ------------------------------------------------------------------------------------
		public void OnDebugStart(string guid, int id, object customIn, object customOut,
			ref bool fCancelDefault)
		{
			if (!m_fAddinEnabled)
				return;

#if SINGLE_THREAD
			StartDebug(null);
#else
			if (!m_nantBuild.IsRunning)
				ThreadPool.QueueUserWorkItem(new WaitCallback(StartDebug));
#endif
			fCancelDefault = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Run the program without attaching a debugger
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="id"></param>
		/// <param name="customIn"></param>
		/// <param name="customOut"></param>
		/// <param name="fCancelDefault"></param>
		/// ------------------------------------------------------------------------------------
		public void OnDebugStartWithoutDebugging(string guid, int id, object customIn,
			object customOut, ref bool fCancelDefault)
		{
			if (!m_fAddinEnabled)
				return;

			if (m_nantBuild.IsRunning)
			{
				fCancelDefault = true;
				return;
			}
			bool fCancel = false;
			if (DTE.Debugger.CurrentMode == dbgDebugMode.dbgDesignMode)
				fCancel = BuildForDebugging();
			//DTE.Debugger.Go(false);
			//fCancelDefault = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Step into the method
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="id"></param>
		/// <param name="customIn"></param>
		/// <param name="customOut"></param>
		/// <param name="fCancelDefault"></param>
		/// ------------------------------------------------------------------------------------
		public void OnDebugStepInto(string guid, int id, object customIn, object customOut,
			ref bool fCancelDefault)
		{
			if (!m_fAddinEnabled)
				return;

#if SINGLE_THREAD
			DebugStepInto(null);
#else
			if (!m_nantBuild.IsRunning)
				ThreadPool.QueueUserWorkItem(new WaitCallback(DebugStepInto));
#endif
			fCancelDefault = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Step over the method
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="id"></param>
		/// <param name="customIn"></param>
		/// <param name="customOut"></param>
		/// <param name="fCancelDefault"></param>
		/// ------------------------------------------------------------------------------------
		public void OnDebugStepOver(string guid, int id, object customIn, object customOut,
			ref bool fCancelDefault)
		{
			if (!m_fAddinEnabled)
				return;

#if SINGLE_THREAD
			DebugStepOver(null);
#else
			if (!m_nantBuild.IsRunning)
				ThreadPool.QueueUserWorkItem(new WaitCallback(DebugStepOver));
#endif
			fCancelDefault = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Run to cursor
		/// </summary>
		/// <param name="guid"></param>
		/// <param name="id"></param>
		/// <param name="customIn"></param>
		/// <param name="customOut"></param>
		/// <param name="fCancelDefault"></param>
		/// ------------------------------------------------------------------------------------
		public void OnDebugRunToCursor(string guid, int id, object customIn, object customOut,
			ref bool fCancelDefault)
		{
			if (!m_fAddinEnabled)
				return;

#if SINGLE_THREAD
			DebugRunToCursor(null);
#else
			if (!m_nantBuild.IsRunning)
				ThreadPool.QueueUserWorkItem(new WaitCallback(DebugRunToCursor));
#endif
			fCancelDefault = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Turns the addin on or off
		/// </summary>
		/// <param name="btn"></param>
		/// <param name="fCancel"></param>
		/// ------------------------------------------------------------------------------------
		public void OnEnableAddin(CommandBarButton btn, ref bool fCancel)
		{
			EnableAddin = !EnableAddin;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cancel the current build
		/// </summary>
		/// <param name="btn"></param>
		/// <param name="fCancel"></param>
		/// ------------------------------------------------------------------------------------
		public void OnCancelBuild(CommandBarButton btn, ref bool fCancel)
		{
			m_nantBuild.CancelBuild();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="btn"></param>
		/// <param name="fCancel"></param>
		/// ------------------------------------------------------------------------------------
		public void OnCleanBuild(CommandBarButton btn, ref bool fCancel)
		{
			Clean = !Clean;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="btn"></param>
		/// <param name="fCancel"></param>
		/// ------------------------------------------------------------------------------------
		public void OnEnableTests(CommandBarButton btn, ref bool fCancel)
		{
			Test = !Test;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="btn"></param>
		/// <param name="fCancel"></param>
		/// ------------------------------------------------------------------------------------
		public void OnForceTests(CommandBarButton btn, ref bool fCancel)
		{
			ForceTests = !ForceTests;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="btn"></param>
		/// <param name="fCancel"></param>
		/// ------------------------------------------------------------------------------------
		public void OnNoDep(CommandBarButton btn, ref bool fCancel)
		{
			NoDep = !NoDep;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start a build with the arguments given in the combo box
		/// </summary>
		/// <param name="btn"></param>
		/// <param name="fCancel"></param>
		/// ------------------------------------------------------------------------------------
		public void OnStartBuild(CommandBarButton btn, ref bool fCancel)
		{
			string cmdLine = m_cmbBuild.Text;
			if (cmdLine == string.Empty || m_nantBuild.IsRunning)
				return;

			AddItemToCombo(cmdLine);

			m_nantBuild.RunNant(cmdLine);
		}

		#endregion

		#region Internal command handling thread methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start debugging
		/// </summary>
		/// <param name="o"></param>
		/// ------------------------------------------------------------------------------------
		private void StartDebug(object o)
		{
			if (!m_Semaphore.WaitOne(100, true))
				return; // must already be dealing with a build

			try
			{
				bool fCancel = false;
				try
				{
					dbgDebugMode debugMode = DTE.Debugger.CurrentMode;
					switch (debugMode)
					{
						case dbgDebugMode.dbgRunMode:
							// already running, so we shouldn't do anything
							return;
						case dbgDebugMode.dbgBreakMode:
							// we're stopped at a breakpoint, so just continue
							DTE.Debugger.Go(false);
							return;
						case dbgDebugMode.dbgDesignMode:
							fCancel = BuildForDebugging();
							break;
					}
				}
				catch (TargetException)
				{
					// This gives an exception right now - don't know why.
					//		Array activeProjects = (Array)DTE.ActiveSolutionProjects;
					//		EnvDTE.Project vsProject = (EnvDTE.Project)activeProjects.GetValue(0);
					//
					//		DTE.Solution.SolutionBuild.BuildProject(
					//			DTE.Solution.SolutionBuild.ActiveConfiguration.Name,
					//			Path.GetFileNameWithoutExtension(vsProject.Name), true);
					fCancel = false;
				}
				try
				{
					if (!fCancel)
					{
						Array activeProjects = (Array)DTE.ActiveSolutionProjects;
						Project vsProject = (EnvDTE.Project)activeProjects.GetValue(0);
						Configuration config = vsProject.ConfigurationManager.ActiveConfiguration;
						Property startAction = (Property)config.Properties.Item("StartAction");
						Property startProgram = (Property)config.Properties.Item("StartProgram");
						Property outputType = (Property)vsProject.Properties.Item("OutputType");
						//Property assemblyName = (Property)vsProject.Properties.Item("AssemblyName");
						//FileInfo outputFile = new FileInfo(Path.Combine((string)startProgram.Value,
						//    (string)assemblyName.Value + ".dll"));
						FileInfo startFile = null;
						if (startProgram.Value != null && ((string)startProgram.Value).Length > 0)
							startFile = new FileInfo((string)startProgram.Value);
						if ((int)outputType.Value == 2 && (int)startAction.Value == 0)
						{
							// We can't directly start a Class Library project
							MessageBox.Show("Can't start a class library without executable specified. Please verify your Project properties",
								"Can't start debugger");
						}
						else if ((int)outputType.Value == 2 &&
							((int)startAction.Value == 1 && (startFile == null || !startFile.Exists)))
						{
							// Can't find executable
							MessageBox.Show("Can't find executable specified for class library. Please verify your Project properties",
								"Can't start debugger");
						}
						else
							DTE.Debugger.Go(false);

						//foreach (Property prop in config.Properties)
						//{
						//    System.Diagnostics.Debug.WriteLine(prop.Name + "=" + prop.Value);
						//    if (prop.Name == "StartAction")
						//        System.Diagnostics.Debug.WriteLine("\n");
						//}
						//foreach (Property prop in vsProject.Properties)
						//{
						//    System.Diagnostics.Debug.WriteLine("Project:" + prop.Name);
						//    if (prop.Name == "OutputType")
						//        System.Diagnostics.Debug.WriteLine("\n");
						//}
					}
				}
				catch
				{
				}
			}
			finally
			{
				m_Semaphore.Release();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Step into
		/// </summary>
		/// <param name="o"></param>
		/// ------------------------------------------------------------------------------------
		private void DebugStepInto(object o)
		{
			if (!m_Semaphore.WaitOne(100, true))
				return; // must already be dealing with a build

			try
			{
				bool fCancel = false;
				try
				{
					if (DTE.Debugger.CurrentMode == dbgDebugMode.dbgDesignMode)
						fCancel = BuildForDebugging();
				}
				catch (TargetException)
				{
					fCancel = false;
				}
				try
				{
					if (!fCancel)
						DTE.Debugger.StepInto(false);
				}
				catch
				{
				}
			}
			finally
			{
				m_Semaphore.Release();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Step over
		/// </summary>
		/// <param name="o"></param>
		/// ------------------------------------------------------------------------------------
		private void DebugStepOver(object o)
		{
			bool fCancel = false;
			try
			{
				if (DTE.Debugger.CurrentMode == dbgDebugMode.dbgDesignMode)
					fCancel = BuildForDebugging();
			}
			catch(TargetException)
			{
				fCancel = false;
			}
			try
			{
				if (!fCancel)
					DTE.Debugger.StepOver(false);
			}
			catch
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Run to cursor
		/// </summary>
		/// <param name="o"></param>
		/// ------------------------------------------------------------------------------------
		private void DebugRunToCursor(object o)
		{
			if (!m_Semaphore.WaitOne(100, true))
				return; // must already be dealing with a build

			try
			{
				bool fCancel = false;
				try
				{
					if (DTE.Debugger.CurrentMode == dbgDebugMode.dbgDesignMode)
						fCancel = BuildForDebugging();
				}
				catch (TargetException)
				{
					fCancel = false;
				}
				try
				{
					if (!fCancel)
						DTE.Debugger.RunToCursor(false);
				}
				catch
				{
				}
			}
			finally
			{
				m_Semaphore.Release();
			}
		}
		#endregion

		#region Other methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Press or release the button
		/// </summary>
		/// <param name="btn"></param>
		/// <param name="fPress"></param>
		/// ------------------------------------------------------------------------------------
		public void PressButton(CommandBarButton btn, bool fPress)
		{
			if (fPress)
				btn.State = MsoButtonState.msoButtonDown;
			else
				btn.State = MsoButtonState.msoButtonUp;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add an item to the combo list
		/// </summary>
		/// <param name="item"></param>
		/// ------------------------------------------------------------------------------------
		public void AddItemToCombo(string item)
		{
			if (m_comboList.Contains(item))
			{
				// remove item and re-add as first item
				m_cmbBuild.RemoveItem(m_cmbBuild.ListIndex);
			}
			else
			{
				// add as first item
				m_comboList.Add(item, item);
			}
			m_cmbBuild.AddItem(item, 1);
			m_cmbBuild.ListIndex = 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the modifier keys
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private NAntBuild.Modifiers Modifiers
		{
			get
			{
				return new NAntBuild.Modifiers(Clean, Test, NoDep, ForceTests);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reset the flags after a build. NoDep remains in its state, all others get reset to
		/// default value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ResetFlags()
		{
			Clean = false;
			Test = false;
			ForceTests = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Callback for build status
		/// </summary>
		/// <param name="fFinished"></param>
		/// ------------------------------------------------------------------------------------
		private void BuildStatusChange(bool fFinished)
		{
			if (fFinished)
				CancelBtn.Enabled = false;
			else
				CancelBtn.Enabled = true;
			Application.DoEvents();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do a build when debugging.
		/// </summary>
		/// <returns><c>true</c> if debug needs to be canceled, otherwise <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool BuildForDebugging()
		{
			bool fOk = false;
			EnvDTE.SolutionConfiguration config =
				DTE.Solution.SolutionBuild.ActiveConfiguration;

			Array activeProjects = (Array)DTE.ActiveSolutionProjects;
			EnvDTE.Project vsProject = (EnvDTE.Project)activeProjects.GetValue(0);

			ArrayList projects = new ArrayList();
			projects.Add(vsProject.FullName);
			if (DTE.Solution.FullName != null && DTE.Solution.FullName.Length > 0)
			{
				string firstProject = Path.GetFileName(vsProject.FullName);
				Array startupProjects = (Array)DTE.Solution.SolutionBuild.StartupProjects;
				string solutionPath = Path.GetDirectoryName(DTE.Solution.FullName);
				foreach (string prjName in startupProjects)
				{
					if (Path.GetFileName(prjName) != firstProject)
						projects.Add(Path.Combine(solutionPath, prjName));
				}
			}

			foreach(string prj in projects)
				Debug.WriteLine("build project=" + prj);

			string[] strProjects = new string[projects.Count];
			projects.CopyTo(strProjects);
			try
			{
				fOk = m_nantBuild.BuildProject(Modifiers, false, vsProject.Name,
					config.Name, config.SolutionContexts.Item(1).PlatformName,
					strProjects, true);
				ResetFlags();
				if (!fOk)
				{
					DialogResult dlgResult = MessageBox.Show("Build errors. Do you want to debug anyway?",
						"Build errors", MessageBoxButtons.YesNo);
					return dlgResult == DialogResult.No;
				}
			}
			catch(TargetException e)
			{
				// handled in outer method
				throw e;
			}
			catch(Exception)
			{
				fOk = false;
			}
			return !fOk;
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Called when project is opened.
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		public void OnProjectOpened()
		{
			m_nantBuild.OnProjectOpened();
		}
		#endregion
	}
}
