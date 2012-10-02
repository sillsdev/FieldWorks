// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Test.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
//using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using NUnit.Framework;
//using NMock;
//using NMock.Constraints;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	public struct ControlAssemblyReplacement
	{
		public string m_toolName;
		public string m_controlName;
		public string m_targetAssembly;
		public string m_targetControlClass;
		public string m_newAssembly;
		public string m_newControlClass;
	}

	/// <summary>
	/// This class does the bare minimum of emulating FwXWindow, so that tests can load controls for tools
	/// and process PropertyChanges posted to the mediator.
	/// </summary>
	public class MockFwXWindow : FwXWindow
	{
		private List<ControlAssemblyReplacement> m_replacements = new List<ControlAssemblyReplacement>();

		public void Init(FdoCache cache)
		{
			InitMediatorValues(cache);
		}

		/// <summary>
		/// Do the bare minimum for use in tests
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="configurationPath"></param>
		protected override void LoadUIFromXmlDocument(XmlDocument configuration, string configurationPath)
		{
			m_windowConfigurationNode = configuration.SelectSingleNode("window");
			ReplaceControlAssemblies();

			PropertyTable.SetProperty("WindowConfiguration", m_windowConfigurationNode);
			PropertyTable.SetPropertyPersistence("WindowConfiguration", false);

			LoadDefaultProperties(m_windowConfigurationNode.SelectSingleNode("defaultProperties"));

			m_mediator.PropertyTable.SetProperty("window", this);
			m_mediator.PropertyTable.SetPropertyPersistence("window", false);

			CommandSet commandset = new CommandSet(m_mediator);
			commandset.Init(m_windowConfigurationNode);
			m_mediator.Initialize(commandset);

			LoadStringTableIfPresent(configurationPath);

			RestoreWindowSettings(false);
			m_mediator.AddColleague(this);

			m_menusChoiceGroupCollection = new ChoiceGroupCollection(m_mediator, null, m_windowConfigurationNode);
			m_sidebarChoiceGroupCollection = new ChoiceGroupCollection(m_mediator, null, m_windowConfigurationNode);
			m_toolbarsChoiceGroupCollection = new ChoiceGroupCollection(m_mediator, null, m_windowConfigurationNode);

			IntPtr handle = this.Handle; // create's a window handle for this form to allow processing broadcasted items.
		}

		/// <summary>
		/// Tests can load a subset of virtual handlers from Main.xml, so set those here.
		/// </summary>
		public List<IVwVirtualHandler> InstalledVirtualHandlers
		{
			set { m_installedVirtualHandlers = value; }
		}

		/// <summary>
		/// Activates the controls for the given toolName.
		/// Assumes tool exists only in one area.
		/// </summary>
		/// <param name="toolName"></param>
		/// <returns></returns>
		public XmlNode ActivateTool(string toolName)
		{
			XmlNode configurationNode = GetToolNode(toolName);
			m_mediator.PropertyTable.SetProperty("currentContentControlParameters", configurationNode.SelectSingleNode("control"));
			m_mediator.PropertyTable.SetPropertyPersistence("currentContentControlParameters", false);
			m_mediator.PropertyTable.SetProperty("currentContentControl", toolName);
			m_mediator.PropertyTable.SetPropertyPersistence("currentContentControl", false);
			ProcessPendingItems();
			return configurationNode;
		}

		private XmlNode GetToolNode(string toolName)
		{
			XmlNode configurationNode = m_windowConfigurationNode.SelectSingleNode(String.Format("//item/parameters/tools/tool[@value = '{0}']", toolName));
			return configurationNode;
		}

		/// <summary>
		/// Add a replacement control for whatever is in the big configuration xml document.
		/// The actual replacement will take place in the LoadUIFromXmlDocument method.
		/// </summary>
		/// <param name="replacement"></param>
		public void AddReplacement(ControlAssemblyReplacement replacement)
		{
			m_replacements.Add(replacement);
		}

		/// <summary>
		/// use to override a standard configuration control, with one defined in tests.
		/// </summary>
		private void ReplaceControlAssemblies()
		{
			foreach (ControlAssemblyReplacement replacement in m_replacements)
			{
				XmlNode toolNode = GetToolNode(replacement.m_toolName);
				XmlNode controlNode = toolNode.SelectSingleNode(String.Format(".//control/parameters[@id='{0}']", replacement.m_controlName));
				// <dynamicloaderinfo assemblyPath="ITextDll.dll" class="SIL.FieldWorks.IText.ConcordanceControl"/>
				XmlNode controlAssemblyNode = controlNode.SelectSingleNode(String.Format(".//dynamicloaderinfo[@assemblyPath='{0}' and @class='{1}']",
					replacement.m_targetAssembly, replacement.m_targetControlClass));
				controlAssemblyNode.Attributes["assemblyPath"].Value = replacement.m_newAssembly;
				controlAssemblyNode.Attributes["class"].Value = replacement.m_newControlClass;
			}
		}

		/// <summary>
		/// return the control specified by the given (unique) id.
		/// </summary>
		/// <param name="idControl"></param>
		/// <returns>null, if it couldn't find the control.</returns>
		public Control FindControl(string idControl)
		{
			return XWindow.FindControl(this, idControl);
		}

		/// <summary>
		/// The active record clerk for the currentControlContent context.
		/// </summary>
		public RecordClerk ActiveClerk
		{
			get { return m_mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk; }
		}

		/// <summary>
		/// invoke the given XCore command.
		/// </summary>
		/// <param name="idCommand"></param>
		public void InvokeCommand(string idCommand)
		{
			XCore.Command cmd = m_mediator.CommandSet[idCommand] as XCore.Command;
			cmd.InvokeCommand();
			this.ProcessPendingItems();
		}

		/// <summary>
		/// simulate master refresh, by doing refresh on the mock window and its cache.
		/// </summary>
		/// <param name="sender"></param>
		public void OnMasterRefresh(object sender)
		{
			CheckDisposed();
			ProcessPendingItems();
			this.PrepareToRefresh();
			ProcessPendingItems();
			Cache.ClearAllData();
			ProcessPendingItems();
			// Refresh it last, so its saved settings get restored.
			this.FinishRefresh();
			this.Activate();
		}

		/// <summary>
		/// We need to manually process the mediator jobs when we don't have a window visible to process WndProc messages.
		/// </summary>
		public void ProcessPendingItems()
		{
			m_mediator.BroadcastPendingItems();	// load the jobs.

			while (m_mediator.JobItems > 0)
			{
				m_mediator.ProcessItem();
			}
		}
	}


	[TestFixture]
	public abstract class XWorksAppTestBase
	{
		protected FwXWindow m_window;

		protected FwXApp m_application;
		protected FdoCache LoadCache(string name)
		{
			Dictionary<string, string> cacheOptions = new Dictionary<string, string>();
			cacheOptions.Add("db", name);
			// where does this cache get disposed? Have to explicitly call cache.Dispose() somewhere!
			return FdoCache.Create(cacheOptions);
		}

		public XWorksAppTestBase()
		{
			m_application =null;
		}

		//this needs to set the m_application and be called separately from the constructor because nunit runs the
		//default constructor on all of the fixtures before showing anything...
		//and cents multiple fixtures will start Multiple FieldWorks applications,
		//this shows multiple splash screens before we have done anything, and
		//runs afoul of the code which in forces only one FieldWorks application defined in the process
		//at any one time.
		abstract protected void Init();


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Instantiate a TestXCoreApp object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public virtual void FixtureInit()
		{
			TestManager.ApproveFixture ("WW", "UI");
			//TestManager.ApproveFixture ("WW", "5Min");

			Init();

			//FwXApp app = MakeApplication();
			string configPath = System.IO.Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.FWCodeDirectory, m_application.DefaultConfigurationPathname);
			FwApp.App = m_application;

			//review: this seems weird to me (JH); one would think that the application would deliver the window
			//instead, here, we are passing the application to the new window.
			m_window = new FwXWindow(LoadCache("TestLangProj"), null, null, configPath, true);// (FwXApp)app.MockInstance);

			/* note that someday, when we write a test to test the persistence function,
			 * set "TestRestoringFromTestSettings" the second time the application has run in order to pick up
			 * the settings from the first run. The code for this is already in xWindow.
			 */

			m_window.Show();
			Application.DoEvents();//without this, tests may fail non-deterministically

		}

		//protected abstract FwXApp MakeApplication ();


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the TestXCoreApp object is destroyed.
		/// Especially since the splash screen it puts up needs to be closed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void FixtureCleanUp()
		{
			m_window.Close();
			m_application.Dispose();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
//		[SetUp]
//		public void Init()
//		{
//			Application.DoEvents();
//		}
		protected ITestableUIAdapter Menu
		{
			get
			{
				try
				{
					return (ITestableUIAdapter)this.m_window.MenuAdapter;
				}
				catch (InvalidCastException)
				{
					throw new ApplicationException ("The installed Adapter does not yet ITestableUIAdapter support ");
				}
			}
		}
		protected Command GetCommand (string commandName)
		{
			Command command = (Command)this.m_window.Mediator.CommandSet[commandName];
			if (command == null)
				throw new ApplicationException ("whoops, there is no command with the id " + commandName);
			return command;
		}
		protected void DoCommand (string commandName)
		{
			GetCommand(commandName).InvokeCommand();
			//let the screen redraw
			Application.DoEvents();
		}
		protected PropertyTable Properties
		{
			get
			{
				return m_window.Mediator.PropertyTable;
			}
		}

		protected void SetTool(string toolValueName)
		{
			//use the Tool menu to select the requested tool
			//(and don't specify anything about the view, so we will get the default)
			Menu.ClickItem("Tools", toolValueName);
		}


		protected void DoCommandRepeatedly(string commandName, int times)
		{
			Command command = GetCommand(commandName);
			for(int i=0; i<times; i++)
			{
				command.InvokeCommand();
				//let the screen redraw
				Application.DoEvents();
			}

		}
	}
}