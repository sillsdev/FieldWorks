using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.VisualStudio.CommandBars;
using Extensibility;
using EnvDTE;
using EnvDTE80;
using NAnt.Core;

namespace FwNantAddin2
{
	/// <summary>
	///   The object for implementing an Add-in.
	/// </summary>
	/// <seealso class='IDTExtensibility2' />
	[GuidAttribute("2AAF1CAE-DD6F-4432-9B8F-897240EB3EE7"), ProgId("Fw_NantAddIn.Connect")]
	public partial class Connect : IDTExtensibility2
	{
		private EnvDTE.SolutionEvents m_solutionEvents;
		private EnvDTE.BuildEvents m_buildEvents;
		private Hashtable m_commandHandler = new Hashtable();
		private DTE2 m_dte;
		private AddIn m_addInInstance;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implements the constructor for the Add-in object.
		/// Place your initialization code within this method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Connect()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the DTE.
		/// </summary>
		/// <value>The DTE.</value>
		/// ------------------------------------------------------------------------------------
		public DTE2 DTE
		{
			get { return m_dte; }
		}

		#region IDTExtensibility2 methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implements the OnConnection method of the IDTExtensibility2 interface.
		/// Receives notification that the Add-in is being loaded.
		/// </summary>
		/// <param name="application">The application.</param>
		/// <param name="connectMode">The connect mode.</param>
		/// <param name="addInInst">The add in inst.</param>
		/// <param name="custom">The custom.</param>
		/// <seealso class="IDTExtensibility2"/>
		/// ------------------------------------------------------------------------------------
		public void OnConnection(object application, Extensibility.ext_ConnectMode connectMode,
			object addInInst, ref System.Array custom)
		{
			try
			{
				CheckForUpdates();

				m_dte = (DTE2)application;
				m_nantCommands = new CmdHandler();
				m_nantCommands.DTE = m_dte;

				m_addInInstance = (AddIn)addInInst;

				EnvDTE.Events events = DTE.Events;
				OutputWindow outputWindow = (OutputWindow)DTE.Windows.Item(Constants.vsWindowKindOutput).Object;

				m_solutionEvents = (EnvDTE.SolutionEvents)events.SolutionEvents;

				// Get the commands of the Build group
				RegisterCommandHandler(882, // build solution
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildSolution));
				RegisterCommandHandler(883, // rebuild solution
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildSolution));
				RegisterCommandHandler(886, // build project
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildProject));
				RegisterCommandHandler(887, // rebuild project
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildProject));
				RegisterCommandHandler(890, // Build Cancel
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildCancel));
				RegisterCommandHandler(892, // build project (from context menu)
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildProject));
				RegisterCommandHandler(893, // rebuild project (from context menu)
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildProject));
				// 979-988 build project (from menu when no project item is selected)
				// 989-998 rebuild project (from menu when no project item is selected)
				for (int i = 979; i < 999; i++)
				{
					RegisterCommandHandler(i,
						new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildProject));
				}
				RegisterCommandHandler(295, // Debug Start
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnDebugStart));
				RegisterCommandHandler(356, // Debug/Start new instance (from context menu)
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnDebugStart));
				RegisterCommandHandler(368, // Debug Start without debugging
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnDebugStartWithoutDebugging));
//				RegisterCommandHandler(248, // Debug Step into
//					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnDebugStepInto));
//				RegisterCommandHandler(357, // Debug Step into (from context menu)
//					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnDebugStepInto));
//				RegisterCommandHandler(249, // Debug Step over
//					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnDebugStepOver));
//				RegisterCommandHandler(251, // Debug Run to cursor
//					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnDebugRunToCursor));
				// TODO: need to handle 891 Batch build!
				m_buildEvents = (EnvDTE.BuildEvents)events.BuildEvents;

				m_solutionEvents.Opened += new _dispSolutionEvents_OpenedEventHandler(Opened);
				m_solutionEvents.ProjectAdded += new _dispSolutionEvents_ProjectAddedEventHandler(OnProjectAdded);
				m_buildEvents.OnBuildBegin += new _dispBuildEvents_OnBuildBeginEventHandler(OnBuildBegin);

				// try to add the commands
				AddToolbar();
			}
			catch(Exception e)
			{
				System.Diagnostics.Debug.WriteLine("Got exception: " + e.Message);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implements the OnDisconnection method of the IDTExtensibility2 interface.
		/// Receives notification that the Add-in is being unloaded.
		/// </summary>
		/// <param name="disconnectMode">The disconnect mode.</param>
		/// <param name="custom">unused</param>
		/// <seealso class="IDTExtensibility2"/>
		/// ------------------------------------------------------------------------------------
		public void OnDisconnection(Extensibility.ext_DisconnectMode disconnectMode,
			ref System.Array custom)
		{
			RemoveToolbar();

			try
			{
				UnregisterCommandHandler(882, // build solution
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildSolution));
				UnregisterCommandHandler(883, // rebuild solution
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildSolution));
				UnregisterCommandHandler(886, // build project
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildProject));
				UnregisterCommandHandler(887, // rebuild project
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildProject));
				UnregisterCommandHandler(890, // Build Cancel
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildCancel));
				UnregisterCommandHandler(892, // build project (from context menu)
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildProject));
				UnregisterCommandHandler(893, // rebuild project (from context menu)
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildProject));
				// 979-988 build project (from menu when no project item is selected)
				// 989-998 rebuild project (from menu when no project item is selected)
				for (int i = 979; i < 999; i++)
				{
					UnregisterCommandHandler(i,
						new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnBuildProject));
				}
				UnregisterCommandHandler(295, // Debug Start
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnDebugStart));
				UnregisterCommandHandler(356, // Debug/Start new instance (from context menu)
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnDebugStart));
				UnregisterCommandHandler(368, // Debug Start without debugging
					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnDebugStartWithoutDebugging));
//				UnregisterCommandHandler(248, // Debug Step into
//					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnDebugStepInto));
//				UnregisterCommandHandler(357, // Debug Step into (from context menu)
//					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnDebugStepInto));
//				UnregisterCommandHandler(249, // Debug Step over
//					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnDebugStepOver));
//				UnregisterCommandHandler(251, // Debug Run to cursor
//					new _dispCommandEvents_BeforeExecuteEventHandler(m_nantCommands.OnDebugRunToCursor));

				if(m_solutionEvents != null)
				{
					m_solutionEvents.Opened -= new _dispSolutionEvents_OpenedEventHandler(Opened);
					m_solutionEvents.ProjectAdded -= new _dispSolutionEvents_ProjectAddedEventHandler(OnProjectAdded);
					m_solutionEvents = null;
				}
				if(m_buildEvents != null)
				{
					m_buildEvents.OnBuildBegin -= new _dispBuildEvents_OnBuildBeginEventHandler(OnBuildBegin);
					m_buildEvents = null;
				}
				m_commandHandler.Clear();
			}
			catch(Exception e)
			{
				System.Diagnostics.Debug.WriteLine(string.Format("Got exception: {0}", e.Message));
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implements the OnAddInsUpdate method of the IDTExtensibility2 interface.
		/// Receives notification that the collection of Add-ins has changed.
		/// </summary>
		/// <param name="custom">An empty array that you can use to pass host-specific data
		/// for use in the add-in.</param>
		/// <seealso class="IDTExtensibility2"/>
		/// ------------------------------------------------------------------------------------
		public void OnAddInsUpdate(ref System.Array custom)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implements the OnStartupComplete method of the IDTExtensibility2 interface.
		/// Receives notification that the host application has completed loading.
		/// </summary>
		/// <param name="custom">An empty array that you can use to pass host-specific data for
		/// use when the add-in loads.</param>
		/// <seealso class="IDTExtensibility2"/>
		/// ------------------------------------------------------------------------------------
		public void OnStartupComplete(ref System.Array custom)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implements the OnBeginShutdown method of the IDTExtensibility2 interface.
		/// Receives notification that the host application is being unloaded.
		/// </summary>
		/// <param name="custom">An empty array that you can use to pass host-specific data for
		/// use in the add-in.</param>
		/// <seealso class="IDTExtensibility2"/>
		/// ------------------------------------------------------------------------------------
		public void OnBeginShutdown(ref System.Array custom)
		{
			//			OnDisconnection(Extensibility.ext_DisconnectMode.ext_dm_SolutionClosed,
			//				ref custom);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Registers the command handler.
		/// </summary>
		/// <param name="nCmdId">The n CMD id.</param>
		/// <param name="cmdHandler">The CMD handler.</param>
		/// ------------------------------------------------------------------------------------
		private void RegisterCommandHandler(int nCmdId,
			_dispCommandEvents_BeforeExecuteEventHandler cmdHandler)
		{
			Events events = DTE.Events;
			CommandEvents cmd = (CommandEvents)events.get_CommandEvents(
				"{5EFC7975-14BC-11CF-9B2B-00AA00573819}", nCmdId);
			cmd.BeforeExecute += cmdHandler;
			m_commandHandler.Add(nCmdId, cmd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unregisters the command handler.
		/// </summary>
		/// <param name="nCmdId">The command id.</param>
		/// <param name="cmdHandler">The command handler.</param>
		/// ------------------------------------------------------------------------------------
		private void UnregisterCommandHandler(int nCmdId,
			_dispCommandEvents_BeforeExecuteEventHandler cmdHandler)
		{
			((CommandEvents)m_commandHandler[nCmdId]).BeforeExecute -= cmdHandler;
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Checks for new updates.
		/// </summary>
		/// ------------------------------------------------------------------------------------------
		private void CheckForUpdates()
		{
			using (AddinOptions options = new AddinOptions())
			{
				// find the update file
				if (options.BaseDirectories == null)
					return;

				int i = 0;
				string updater = string.Empty;
				for (; i < options.BaseDirectories.Length; i++)
				{
					updater = Path.Combine(options.BaseDirectories[0], @"bin\VS Addins\FwVsUpdateChecker.exe");
					if (File.Exists(updater))
						break;
				}
				if (i >= options.BaseDirectories.Length)
					return;

				System.Diagnostics.Process process = new System.Diagnostics.Process();
				process.StartInfo.FileName = updater;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.WorkingDirectory = Path.GetDirectoryName(updater);
				if (Settings.Default.FirstTime)
					process.StartInfo.Arguments = "/first " + options.BaseDirectories[i];
				else
					process.StartInfo.Arguments = options.BaseDirectories[i];
				process.Start();

				if (Settings.Default.FirstTime)
				{
					Settings.Default.FirstTime = false;
					Settings.Default.Save();
				}
			}
		}
	}
}