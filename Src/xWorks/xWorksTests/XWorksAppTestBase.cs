// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.FwUtils;

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

		public MockFwXWindow(FwXApp application, string configFile)
			:base(application)
		{
		}

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

			PropTable.SetProperty("WindowConfiguration", m_windowConfigurationNode, true);
			PropTable.SetPropertyPersistence("WindowConfiguration", false);

			LoadDefaultProperties(m_windowConfigurationNode.SelectSingleNode("defaultProperties"));

			PropTable.SetProperty("window", this, true);
			PropTable.SetPropertyPersistence("window", false);

			CommandSet commandset = new CommandSet(m_mediator);
			commandset.Init(m_windowConfigurationNode);
			m_mediator.Initialize(commandset);

			var st = StringTable.Table; // Force loading it.

			RestoreWindowSettings(false);
			m_mediator.AddColleague(this);

			m_menusChoiceGroupCollection = new ChoiceGroupCollection(m_mediator, m_propertyTable, null, m_windowConfigurationNode);
			m_sidebarChoiceGroupCollection = new ChoiceGroupCollection(m_mediator, m_propertyTable, null, m_windowConfigurationNode);
			m_toolbarsChoiceGroupCollection = new ChoiceGroupCollection(m_mediator, m_propertyTable, null, m_windowConfigurationNode);

			var handle = Handle; // create's a window handle for this form to allow processing broadcasted items.
		}

		protected override void XWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!m_mediator.IsDisposed)
			{
				m_mediator.ProcessMessages = false;
				PropTable.SetProperty("windowState", WindowState, false);
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			//((Form)this).Close();
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
			PropTable.SetProperty("currentContentControlParameters", configurationNode.SelectSingleNode("control"), true);
			PropTable.SetPropertyPersistence("currentContentControlParameters", false);
			PropTable.SetProperty("currentContentControl", toolName, true);
			PropTable.SetPropertyPersistence("currentContentControl", false);
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

		public void ClearReplacements()
		{
			m_replacements.Clear();
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
			get { return PropTable.GetValue<RecordClerk>("ActiveClerk"); }
		}

		/// <summary>
		/// invoke the given XCore command.
		/// </summary>
		/// <param name="idCommand"></param>
		public void InvokeCommand(string idCommand)
		{
			var cmd = m_mediator.CommandSet[idCommand] as Command;
			cmd.InvokeCommand();
			ProcessPendingItems();
		}

		/// <summary>
		/// simulate master refresh, by doing refresh on the mock window and its cache.
		/// </summary>
		/// <param name="sender"></param>
		public override void OnMasterRefresh(object sender)
		{
			CheckDisposed();
			ProcessPendingItems();
			this.PrepareToRefresh();
			ProcessPendingItems();
			ProcessPendingItems();
			// Refresh it last, so its saved settings get restored.
			//this.FinishRefresh();
			Refresh();
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

	public class MockFwManager : IFieldWorksManager
	{

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shutdowns the specified application. The application will be disposed of immediately.
		/// If no other applications are running, then FieldWorks will also be shutdown.
		/// </summary>
		/// <param name="app">The application to shut down.</param>
		/// ------------------------------------------------------------------------------------
		public void ShutdownApp(FwApp app)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes the specified method asynchronously. The method will typically be called
		/// when the the Application.Run() loop regains control or the next call to
		/// Application.DoEvents() at some unspecified time in the future.
		/// </summary>
		/// <param name="action">The action to execute</param>
		/// <param name="param1">The first parameter of the action.</param>
		/// ------------------------------------------------------------------------------------
		public void ExecuteAsync<T>(Action<T> action, T param1)
		{
			try
			{
				action(param1);
			}
			catch
			{
				Assert.Fail(String.Format("This action caused an exception in MockFwManager. Action={0}, Param={1}",
					action, param1));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens a new main window for the specified application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void OpenNewWindowForApp()
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets the user chooses a language project and opens it. If the project is already
		/// open in a FieldWorks process, then the request is sent to the running FieldWorks
		/// process and a new window is opened for that project. Otherwise a new FieldWorks
		/// process is started to handle the project request.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ChooseLangProject()
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets the user create a new language project and opens it. If the project is already
		/// open in a FieldWorks process, then the request is sent to the running FieldWorks
		/// process and a new window is opened for that project. Otherwise a new FieldWorks
		/// process is started to handle the new project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CreateNewProject()
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets the user delete any FW databases that are not currently open
		/// </summary>
		/// <param name="app">The application.</param>
		/// <param name="dialogOwner">The owner of the dialog</param>
		/// ------------------------------------------------------------------------------------
		public void DeleteProject(FwApp app, Form dialogOwner)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lets the user backup any FW databases that are not currently open
		/// </summary>
		/// <param name="app">The application.</param>
		/// <param name="dialogOwner">The owner of the dialog</param>
		/// ------------------------------------------------------------------------------------
		public string BackupProject(Form dialogOwner)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restore a project.
		/// </summary>
		/// <param name="fwApp">The FieldWorks application.</param>
		/// <param name="dialogOwner">The dialog owner.</param>
		/// ------------------------------------------------------------------------------------
		public void RestoreProject(FwApp fwApp, Form dialogOwner)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Reopens the given FLEx project. This may be necessary if some external process modified the project data.
		/// Currently used when FLExBridge modifies our project during a Send/Receive
		/// </summary>
		/// <param name="project">The project name to re-open</param>
		/// <param name="app"></param>
		public FwApp ReopenProject(string project, FwAppArgs app)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///
		/// </summary>
		public void FileProjectLocation(FwApp fwApp, Form dialogOwner)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Rename the project used by this FieldWorks to the specified new name.
		/// </summary>
		/// <param name="newName">The new name</param>
		/// <returns>True if the rename was successful, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool RenameProject(string newName)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles a link request. This is expected to handle determining the correct
		/// application to start up on the correct project and passing the link to any newly
		/// started application.
		/// </summary>
		/// <param name="link">The link.</param>
		/// ------------------------------------------------------------------------------------
		public void HandleLinkRequest(FwAppArgs link)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Archive selected project files using RAMP
		/// </summary>
		/// <param name="fwApp">The FieldWorks application</param>
		/// <param name="dialogOwner">The owner of the dialog</param>
		/// <returns>The list of the files to archive, or <c>null</c> if the user cancels the
		/// archive dialog</returns>
		/// ------------------------------------------------------------------------------------
		public List<string> ArchiveProjectWithRamp(FwApp fwApp, Form dialogOwner)
		{
			throw new NotImplementedException();
		}
	}

	public class MockFwXApp : FwXApp
	{
		public MockFwXApp(IFieldWorksManager fwManager, IHelpTopicProvider helpTopicProvider, FwAppArgs appArgs)
			: base(fwManager, helpTopicProvider, appArgs)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a main FLEx window.
		/// </summary>
		/// <param name="progressDlg">The progress DLG.</param>
		/// <param name="isNewCache">if set to <c>true</c> [is new cache].</param>
		/// <param name="wndCopyFrom">The WND copy from.</param>
		/// <param name="fOpeningNewProject">if set to <c>true</c> [f opening new project].</param>
		/// ------------------------------------------------------------------------------------
		public override Form NewMainAppWnd(IProgress progressDlg, bool isNewCache,
			Form wndCopyFrom, bool fOpeningNewProject)
		{
			if (progressDlg != null)
				progressDlg.Message = String.Format("Creating window for MockFwXApp {0}", Cache.ProjectId.Name);
			Form form = base.NewMainAppWnd(progressDlg, isNewCache, wndCopyFrom, fOpeningNewProject);

			if (form is FwXWindow)
			{
				FwXWindow wnd = (FwXWindow)form;

				m_activeMainWindow = form;
			}
			return form;
		}

		/// <summary>
		/// Provides a hook for initializing the cache in application-specific ways.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <returns>True if the initialization was successful, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public override bool InitCacheForApp(IThreadedProgress progressDlg)
		{
			return true;
		}

		public override void RemoveWindow(IFwMainWnd fwMainWindow)
		{
			//base.RemoveWindow(fwMainWindow); We never added it.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path of the product executable filename
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ProductExecutableFile
		{
			get { throw new NotImplementedException(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ApplicationName
		{
			get { return "FLEx"; }
		}

		protected override string SettingsKeyName
		{
			get
			{
				return FwSubKey.LexText;
			}
		}
	}

	[TestFixture]
	public abstract class XWorksAppTestBase : MemoryOnlyBackendProviderTestBase
	{
		protected FwXWindow m_window; // defined here but created and torn down in subclass?
		protected FwXApp m_application;
		protected string m_configFilePath;

		private ITsStrFactory m_tssFact;
		private ICmPossibilityFactory m_possFact;
		private ICmPossibilityRepository m_possRepo;
		private IPartOfSpeechFactory m_posFact;
		private IPartOfSpeechRepository m_posRepo;
		private ILexEntryFactory m_entryFact;
		private ILexSenseFactory m_senseFact;
		private IMoStemAllomorphFactory m_stemFact;
		private IMoAffixAllomorphFactory m_affixFact;

		protected XWorksAppTestBase()
		{
			m_application =null;
		}

		//this needs to set the m_application and be called separately from the constructor because nunit runs the
		//default constructor on all of the fixtures before showing anything...
		//and since multiple fixtures will start Multiple FieldWorks applications,
		//this shows multiple splash screens before we have done anything, and
		//runs afoul of the code which enforces only one FieldWorks application defined in the process
		//at any one time.
		abstract protected void Init();


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Instantiate a TestXCoreApp object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureInit()
		{
			FwRegistrySettings.Init();
			SetupEverythingButBase();
			Init(); // subclass version must create and set m_application

			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, @"Language Explorer", @"Configuration", @"Main.xml");

			// Setup for possibility loading [GetPossibilityOrCreateOne()]
			// and test data creation
			SetupFactoriesAndRepositories();

			/* note that someday, when we write a test to test the persistence function,
			 * set "TestRestoringFromTestSettings" the second time the application has run in order to pick up
			 * the settings from the first run. The code for this is already in xWindow.
			 */

			//m_window.Show(); Why?
			Application.DoEvents();//without this, tests may fail non-deterministically
		}

		private void SetupFactoriesAndRepositories()
		{
			Assert.True(Cache != null, "No cache yet!?");
			var servLoc = Cache.ServiceLocator;
			m_tssFact = Cache.TsStrFactory;
			m_possFact = servLoc.GetInstance<ICmPossibilityFactory>();
			m_possRepo = servLoc.GetInstance<ICmPossibilityRepository>();
			m_posFact = servLoc.GetInstance<IPartOfSpeechFactory>();
			m_posRepo = servLoc.GetInstance<IPartOfSpeechRepository>();
			m_entryFact = servLoc.GetInstance<ILexEntryFactory>();
			m_senseFact = servLoc.GetInstance<ILexSenseFactory>();
			m_stemFact = servLoc.GetInstance<IMoStemAllomorphFactory>();
			m_affixFact = servLoc.GetInstance<IMoAffixAllomorphFactory>();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the TestXCoreApp object is destroyed.
		/// Especially since the splash screen it puts up needs to be closed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void FixtureCleanUp()
		{
			m_application.Dispose();
			if (m_window != null)
			{
				m_window.Close();
				m_window.Dispose();
				m_window = null;
			}
			m_application = null;
			FwRegistrySettings.Release();
		}

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

		#region Data Setup methods

		/// <summary>
		/// Will find a morph type (if one exists) with the given (analysis ws) name.
		/// If not found, will create the morph type in the Lexicon MorphTypes list.
		/// </summary>
		/// <param name="morphTypeName"></param>
		/// <returns></returns>
		protected IMoMorphType GetMorphTypeOrCreateOne(string morphTypeName)
		{
			Assert.IsNotNull(m_possFact, "Fixture Initialization is not complete.");
			Assert.IsNotNull(m_window, "No window.");
			var poss = m_possRepo.AllInstances().Where(
				someposs => someposs.Name.AnalysisDefaultWritingSystem.Text == morphTypeName).FirstOrDefault();
			if (poss != null)
				return poss as IMoMorphType;
			var owningList = Cache.LangProject.LexDbOA.MorphTypesOA;
			Assert.IsNotNull(owningList, "No MorphTypes property on Lexicon object.");
			var ws = Cache.DefaultAnalWs;
			poss = m_possFact.Create(new Guid(), owningList);
			poss.Name.set_String(ws, morphTypeName);
			return poss as IMoMorphType;
		}

		/// <summary>
		/// Will find a variant entry type (if one exists) with the given (analysis ws) name.
		/// If not found, will create the variant entry type in the Lexicon VariantEntryTypes list.
		/// </summary>
		/// <param name="variantTypeName"></param>
		/// <returns></returns>
		protected ILexEntryType GetVariantTypeOrCreateOne(string variantTypeName)
		{
			Assert.IsNotNull(m_possFact, "Fixture Initialization is not complete.");
			Assert.IsNotNull(m_window, "No window.");
			var poss = m_possRepo.AllInstances().Where(
				someposs => someposs.Name.AnalysisDefaultWritingSystem.Text == variantTypeName).FirstOrDefault();
			if (poss != null)
				return poss as ILexEntryType;
			// shouldn't get past here; they're already defined.
			var owningList = Cache.LangProject.LexDbOA.VariantEntryTypesOA;
			Assert.IsNotNull(owningList, "No VariantEntryTypes property on Lexicon object.");
			var ws = Cache.DefaultAnalWs;
			poss = m_possFact.Create(new Guid(), owningList);
			poss.Name.set_String(ws, variantTypeName);
			return poss as ILexEntryType;
		}

		/// <summary>
		/// Will find a complex entry type (if one exists) with the given (analysis ws) name.
		/// If not found, will create the complex entry type in the Lexicon ComplexEntryTypes list.
		/// </summary>
		/// <param name="complexTypeName"></param>
		/// <returns></returns>
		protected ILexEntryType GetComplexTypeOrCreateOne(string complexTypeName)
		{
			Assert.IsNotNull(m_possFact, "Fixture Initialization is not complete.");
			Assert.IsNotNull(m_window, "No window.");
			var poss = m_possRepo.AllInstances().Where(
				someposs => someposs.Name.AnalysisDefaultWritingSystem.Text == complexTypeName).FirstOrDefault();
			if (poss != null)
				return poss as ILexEntryType;
			// shouldn't get past here; they're already defined.
			var owningList = Cache.LangProject.LexDbOA.ComplexEntryTypesOA;
			Assert.IsNotNull(owningList, "No ComplexEntryTypes property on Lexicon object.");
			var ws = Cache.DefaultAnalWs;
			poss = m_possFact.Create(new Guid(), owningList);
			poss.Name.set_String(ws, complexTypeName);
			return poss as ILexEntryType;
		}

		/// <summary>
		/// Will find a grammatical category (if one exists) with the given (analysis ws) name.
		/// If not found, will create a category as a subpossibility of a grammatical category.
		/// </summary>
		/// <param name="catName"></param>
		/// <param name="owningCategory"></param>
		/// <returns></returns>
		protected IPartOfSpeech GetGrammaticalCategoryOrCreateOne(string catName, IPartOfSpeech owningCategory)
		{
			return GetGrammaticalCategoryOrCreateOne(catName, null, owningCategory);
		}

		/// <summary>
		/// Will find a grammatical category (if one exists) with the given (analysis ws) name.
		/// If not found, will create the grammatical category in the owning list.
		/// </summary>
		/// <param name="catName"></param>
		/// <param name="owningList"></param>
		/// <returns></returns>
		protected IPartOfSpeech GetGrammaticalCategoryOrCreateOne(string catName, ICmPossibilityList owningList)
		{
			return GetGrammaticalCategoryOrCreateOne(catName, owningList, null);
		}

		/// <summary>
		/// Will find a grammatical category (if one exists) with the given (analysis ws) name.
		/// If not found, will create a grammatical category either as a possibility of a list,
		/// or as a subpossibility of a category.
		/// </summary>
		/// <param name="catName"></param>
		/// <param name="owningList"></param>
		/// <param name="owningCategory"></param>
		/// <returns></returns>
		protected IPartOfSpeech GetGrammaticalCategoryOrCreateOne(string catName, ICmPossibilityList owningList,
			IPartOfSpeech owningCategory)
		{
			Assert.True(m_posFact != null, "Fixture Initialization is not complete.");
			Assert.True(m_window != null, "No window.");
			var category = m_posRepo.AllInstances().Where(
				someposs => someposs.Name.AnalysisDefaultWritingSystem.Text == catName).FirstOrDefault();
			if (category != null)
				return category;
			var ws = Cache.DefaultAnalWs;
			if (owningList == null)
			{
				if (owningCategory == null)
					throw new ArgumentException(
						"Grammatical category not found and insufficient information given to create one.");
				category = m_posFact.Create(new Guid(), owningCategory);
			}
			else
				category = m_posFact.Create(new Guid(), owningList);
			category.Name.set_String(ws, catName);
			return category;
		}

		protected ILexEntry AddLexeme(List<ICmObject> addList, string lexForm, string citationForm,
			IMoMorphType morphTypePoss, string gloss, IPartOfSpeech catPoss)
		{
			var ws = Cache.DefaultVernWs;
			var le = AddLexeme(addList, lexForm, morphTypePoss, gloss, catPoss);
			le.CitationForm.set_String(ws, citationForm);
			return le;
		}

		protected ILexEntry AddLexeme(List<ICmObject> addList, string lexForm, IMoMorphType morphTypePoss,
			string gloss, IPartOfSpeech categoryPoss)
		{
			var msa = new SandboxGenericMSA { MainPOS = categoryPoss };
			var comp = new LexEntryComponents { MorphType = morphTypePoss, MSA = msa };
			comp.GlossAlternatives.Add(m_tssFact.MakeString(gloss, Cache.DefaultAnalWs));
			comp.LexemeFormAlternatives.Add(m_tssFact.MakeString(lexForm, Cache.DefaultVernWs));
			var entry = m_entryFact.Create(comp);
			addList.Add(entry);
			return entry;
		}

		protected ILexEntry AddVariantLexeme(List<ICmObject> addList, IVariantComponentLexeme origLe,
			string lexForm, IMoMorphType morphTypePoss, string gloss, IPartOfSpeech categoryPoss,
			ILexEntryType varType)
		{
			Assert.IsNotNull(varType, "Need a variant entry type!");
			var msa = new SandboxGenericMSA { MainPOS = categoryPoss };
			var comp = new LexEntryComponents { MorphType = morphTypePoss, MSA = msa };
			comp.GlossAlternatives.Add(m_tssFact.MakeString(gloss, Cache.DefaultAnalWs));
			comp.LexemeFormAlternatives.Add(m_tssFact.MakeString(lexForm, Cache.DefaultVernWs));
			var entry = m_entryFact.Create(comp);
			entry.MakeVariantOf(origLe, varType);
			addList.Add(entry);
			return entry;
		}

		protected ILexSense AddSenseToEntry(List<ICmObject> addList, ILexEntry le, string gloss,
			IPartOfSpeech catPoss)
		{
			var msa = new SandboxGenericMSA();
			msa.MainPOS = catPoss;
			var sense = m_senseFact.Create(le, msa, gloss);
			addList.Add(sense);
			return sense;
		}

		protected ILexSense AddSubSenseToSense(List<ICmObject> addList, ILexSense ls, string gloss,
			IPartOfSpeech catPoss)
		{
			var msa = new SandboxGenericMSA();
			msa.MainPOS = catPoss;
			var sense = m_senseFact.Create(new Guid(), ls);
			sense.SandboxMSA = msa;
			sense.Gloss.set_String(Cache.DefaultAnalWs, gloss);
			addList.Add(sense);
			return sense;
		}

		protected void AddStemAllomorphToEntry(List<ICmObject> addList, ILexEntry le, string alloName,
			IPhEnvironment env)
		{
			var allomorph = m_stemFact.Create();
			le.AlternateFormsOS.Add(allomorph);
			if (env != null)
				allomorph.PhoneEnvRC.Add(env);
			allomorph.Form.set_String(Cache.DefaultVernWs, alloName);
			addList.Add(allomorph);
		}

		protected void AddAffixAllomorphToEntry(List<ICmObject> addList, ILexEntry le, string alloName,
			IPhEnvironment env)
		{
			var allomorph = m_affixFact.Create();
			le.AlternateFormsOS.Add(allomorph);
			if (env != null)
				allomorph.PhoneEnvRC.Add(env);
			allomorph.Form.set_String(Cache.DefaultVernWs, alloName);
			addList.Add(allomorph);
		}

		#endregion
	}
}
