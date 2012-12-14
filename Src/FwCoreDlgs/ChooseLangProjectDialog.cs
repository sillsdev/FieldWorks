// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ChooseLangProjectDialog.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.Utils.FileDialog;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary></summary>
	public partial class ChooseLangProjectDialog : Form
	{
		#region Member variables
		private const string LastSelectedHostKey = "LastSelectedHost";

		// Stores a mapping between an ip address, and a host looked up via DNS.
		private readonly IDictionary<string, string> m_hostIpAddressMap = new Dictionary<string, string>();
		private readonly TreeNode m_networkNeighborhood = new TreeNode(FwCoreDlgs.ksNetworkNeighborhood);
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private TreeNode m_localhostNode;
		private readonly Rectangle m_initialBounds = Rectangle.Empty;
		private readonly int m_initialSplitterPosition = -1;
		#endregion

		#region LanguageProjectInfo class
		/// <summary>type that is inserted in the Language Projects Listbox.</summary>
		internal class LanguageProjectInfo
		{
			public string FullName { get; private set; }

			public bool ShowExtenstion
			{
				get { return m_showExtenstion; }
				set
				{
					m_showExtenstion = value;
					m_displayName = m_showExtenstion ? Path.GetFileName(FullName) : Path.GetFileNameWithoutExtension(FullName);
				}
			}

			protected string m_displayName;

			protected bool m_showExtenstion;

			public LanguageProjectInfo(string filename)
			{
				FullName = filename;
				m_displayName = Path.GetFileNameWithoutExtension(filename);
			}

			public override string ToString()
			{
				return m_displayName;
			}
		}
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ChooseLangProjectDialog"/> class.
		/// Used for the designer
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ChooseLangProjectDialog()
		{
			InitializeComponent();
			//hide the FLExBridge related link and image if unavailable
			m_linkOpenBridgeProject.Visible = File.Exists(FLExBridgeHelper.FullFieldWorksBridgePath());
			pictureBox1.Visible = File.Exists(FLExBridgeHelper.FullFieldWorksBridgePath());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ChooseLangProjectDialog"/> class.
		/// </summary>
		/// <param name="bounds">The initial client bounds of the dialog.</param>
		/// <param name="splitterPosition">The initial splitter position.</param>
		/// ------------------------------------------------------------------------------------
		public ChooseLangProjectDialog(Rectangle bounds, int splitterPosition) : this(null, false)
		{
			m_initialBounds = bounds;
			m_initialSplitterPosition = splitterPosition;

			StartPosition = (m_initialBounds == Rectangle.Empty ?
				FormStartPosition.CenterParent : FormStartPosition.Manual);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ChooseLangProjectDialog"/> class.
		/// </summary>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="openToAssosiateFwProject">If set to <c>true</c> the dialog will be
		/// used to assosiate a FieldWorks project with another application (e.g. Paratext).
		/// </param>
		/// ------------------------------------------------------------------------------------
		public ChooseLangProjectDialog(IHelpTopicProvider helpTopicProvider,
			bool openToAssosiateFwProject) : this()
		{
			m_helpTopicProvider = helpTopicProvider;

#if __MonoCS__
			// TODO-Linux: FWNX-606: remove workaround when mono bug is fixed.
			m_tblLayoutOuter.LayoutSettings.SetColumnSpan(m_splitContainer, 4);
#endif
			if (helpTopicProvider == null)
				m_btnHelp.Enabled = false;

			if (openToAssosiateFwProject)
				Text = FwCoreDlgs.kstidOpenToAssociateFwProj;

			m_lblAddNetworkComp.Font = SystemFonts.IconTitleFont;
			m_lblChoosePrj.Font = SystemFonts.IconTitleFont;
			m_lblLookIn.Font = SystemFonts.IconTitleFont;
			m_linkOpenFwDataProject.Font = SystemFonts.IconTitleFont;
			m_linkOpenBridgeProject.Font = SystemFonts.IconTitleFont;
			m_lstLanguageProjects.Font = SystemFonts.IconTitleFont;
			m_hostsTreeView.Font = SystemFonts.IconTitleFont;
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Project name (for local projects, this will be a file name)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Project { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remote host name (or null if local)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Server { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the splitter position.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int SplitterPosition
		{
			get { return m_splitContainer.SplitterDistance; }
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Starts the thread to search for network servers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnShown(EventArgs e)
		{
			if (m_initialBounds != Rectangle.Empty)
				Bounds = m_initialBounds;

			if (m_initialSplitterPosition > 0)
				m_splitContainer.SplitterDistance = m_initialSplitterPosition;

			// Ensure dialog completely painted before calling methods that may do network operations.
			// This should not be needed. If painting is not happening, then there is a problem somewhere.
			// Any accessing of the network should be done on a separate thread.
			//Application.DoEvents();

			m_hostsTreeView.AfterSelect += AfterHostNodeSelected;

			string localhostName = Dns.GetHostName();

			// Ensure the localhost is always added, and it's a branch by itself.
			m_localhostNode = new TreeNode(localhostName);
			m_hostsTreeView.Nodes.Add(m_localhostNode);
			m_hostIpAddressMap[localhostName] = Dns.GetHostAddresses(localhostName)[0].ToString();

			// Add the branch where the Remote servers are added to.
			m_hostsTreeView.Nodes.Add(m_networkNeighborhood);

			// Ensure localhost is the selected Node.
			m_hostsTreeView.SelectedNode = m_localhostNode;
			m_localhostNode.TreeView.FullRowSelect = true;

			// Add any servers the user has requested manually. These should work even if Firewall settings
			// don't allow adding automatically.
			if (File.Exists(HostsFileName))
			{
				using (var reader = new StreamReader(HostsFileName))
				{
					while (!reader.EndOfStream)
					{
						AddHost(reader.ReadLine());
					}
				}
			}

			// Asynchronously search for other Servers.
			ClientServerServices.Current.BeginFindServers(AddHost1);

			m_lstLanguageProjects.SelectedIndexChanged += LanguageProjectsListSelectedIndexChanged;

			base.OnShown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensure that all threads are closed when Dialog exits
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			ClientServerServices.Current.ForceEndFindServers();
			ClientServerServices.Current.ForceEndFindProjects();
			base.OnClosing(e);
		}

		// We need a version of this that returns void.
		internal void AddHost1(string ipAddress)
		{
			AddHost(ipAddress);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a entry to the hostsTreeView List if its not in there already.
		/// This should be the only place that items are added to hostsTreeView Network Neighborhood branch.
		/// When the first entry is added to the Host list, ensure that
		/// it is selected.
		/// </summary>
		/// <param name="ipAddress">The ip address.</param>
		/// <returns>true if able to connect successfully to the host.</returns>
		/// ------------------------------------------------------------------------------------
		internal bool AddHost(string ipAddress)
		{
			if (String.IsNullOrEmpty(ipAddress))
				return false;

			IPHostEntry entry;

			try
			{
				entry = Dns.GetHostEntry(ipAddress);
			}
			catch (SocketException)
			{
				return false;
			}

			// store the HostName -> ipaddress mapping.
			m_hostIpAddressMap[entry.HostName] = ipAddress;

			AddHostInternal(entry);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cross-thread protected, as this method affects the UI.
		/// Call AddHost() instead of this method.
		/// TODO: possible we don't want local host added to Network Neighborhood
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddHostInternal(IPHostEntry entry)
		{
			if (InvokeRequired)
			{
				BeginInvoke((Action<IPHostEntry>)AddHostInternal, entry);
				return;
			}

			// Simple check for host already present by name.
			if (IsDisposed || m_networkNeighborhood.Nodes.ContainsKey((entry.HostName)))
				return;

			// if list of associated addresses in entry matches list of associated addresses of any item in hostsTreeView
			// then ignore as its the same host.
			try
			{
				var checkDnsHostLookup = Dns.GetHostEntry(entry.HostName); //LT-13898, the DNS server was configured wrong so entry.HostName was host.wins.sil.org
															//Calling GetHostEntry with a bad domain name will throw. The catch statement allows us to handle this
															//SocketException gracefully.
				foreach (TreeNode host in m_networkNeighborhood.Nodes.Cast<TreeNode>())
				{
					//If entry.HostName == host.Text then it is already in m_networkNeighborhood.Nodes, therefore return.
					if (Dns.GetHostEntry(host.Text).AddressList.Intersect(entry.AddressList).Count() > 0)
						return;
				}
			}
			catch (SocketException e)
			{
				//This is to gracefully handle broken DNS servers.
				return;
			}

			var node = new TreeNode(entry.HostName);
			m_networkNeighborhood.Nodes.Add(node);

			// It's annoying to auto-select the local host in the network neighborhood list, since it offers fewer choices.
			if (LastSelectedHost == entry.HostName && LastSelectedHost != m_localhostNode.Text)
				m_hostsTreeView.SelectedNode = node;
		}

		/// <summary>
		/// Persist host to regisitry.
		/// </summary>
		private static string LastSelectedHost
		{
			set
			{
				FwRegistryHelper.FieldWorksRegistryKey.SetValue(LastSelectedHostKey, value);
			}

			get
			{
				return (string)FwRegistryHelper.FieldWorksRegistryKey.GetValue(LastSelectedHostKey, String.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an entry to the languageProjectsList if it is not there already.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddProject(string projectFile)
		{
			if (InvokeRequired)
			{
				BeginInvoke((Action<string>)AddProject, projectFile);
				return;
			}
			if (IsDisposed) return;

			var languageProjectInfo = new LanguageProjectInfo(projectFile);

			// Show file extensions for duplicate projects.
			LanguageProjectInfo existingItem = m_lstLanguageProjects.Items.
				Cast<LanguageProjectInfo>().
				Where(item => item.ToString() == languageProjectInfo.ToString()).
				FirstOrDefault();

			if (existingItem != null)
			{
				m_lstLanguageProjects.Items.Remove(existingItem);
				existingItem.ShowExtenstion = true;
				m_lstLanguageProjects.Items.Add(existingItem);
				languageProjectInfo.ShowExtenstion = true;
			}

			m_lstLanguageProjects.Items.Add(languageProjectInfo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles any exceptions thrown in the thread that looks for projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleProjectFindingExceptions(Exception e)
		{
			if (InvokeRequired)
			{
				Invoke((Action<Exception>)HandleProjectFindingExceptions, e);
				return;
			}

			if (e is SocketException || e is RemotingException)
			{
				Logger.WriteError(string.Format(
					"Got {0} populating language projects list - ignored", e.GetType().Name), e);
				MessageBox.Show(ActiveForm, FwCoreDlgs.ksCannotConnectToServer, FwCoreDlgs.ksWarning,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			else if (e is DirectoryNotFoundException)
			{
				MessageBox.Show(ActiveForm, e.Message, FwCoreDlgs.ksWarning, MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
			else
				throw e;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Queries a given host for avaiable Projects on a separate thread
		/// </summary>
		/// <param name="host">The host.</param>
		/// <param name="showLocalProjects">true if we want to show local fwdata projects</param>
		/// ------------------------------------------------------------------------------------
		internal void PopulateLanguageProjectsList(string host, bool showLocalProjects)
		{
			// Need to end the previous project finder if the user clicks another host while
			// searching the current host.
			ClientServerServices.Current.ForceEndFindProjects();

			m_btnOk.Enabled = false;
			m_lstLanguageProjects.Items.Clear();
			m_lstLanguageProjects.Enabled = true;

			ClientServerServices.Current.BeginFindProjects(host, AddProject,
				HandleProjectFindingExceptions, showLocalProjects);
		}

		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the languageProjectLists contains a valid selection enable the dialogs ok button
		/// else disable it
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LanguageProjectsListSelectedIndexChanged(object sender, EventArgs e)
		{
			m_btnOk.Enabled = m_lstLanguageProjects.SelectedItem != null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populate Language Project List to reflect new Host selection.
		/// Persist the host selection if the user made the selection.
		/// Ignore if selection is Network Neightborhood branch.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AfterHostNodeSelected(object sender, TreeViewEventArgs e)
		{
			if (m_hostsTreeView.SelectedNode == m_networkNeighborhood || m_hostsTreeView.SelectedNode == null)
			{
				m_lstLanguageProjects.Items.Clear();
				m_btnOk.Enabled = false;
				return;
			}

			// If selection wasn't programatic persist the last selected host.
			if (e.Action != TreeViewAction.Unknown)
				LastSelectedHost = m_hostsTreeView.SelectedNode.Text;

			PopulateLanguageProjectsList(m_hostsTreeView.SelectedNode.Text, m_hostsTreeView.SelectedNode == m_localhostNode);
		}

		/// ------------------------------------------------------------------------------------
		private void HandleDoubleClickOnProjectList(object sender, MouseEventArgs e)
		{
			if (m_lstLanguageProjects.IndexFromPoint(e.Location) >= 0)
				m_btnOk.PerformClick();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the click event for the Open button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OkButtonClick(object sender, EventArgs e)
		{
			if (m_hostsTreeView.SelectedNode == null)
				return;

			if (m_lstLanguageProjects.SelectedItem == null)
				return;

			if (m_hostsTreeView.SelectedNode == m_localhostNode)
			{
				Project = ((LanguageProjectInfo)m_lstLanguageProjects.SelectedItem).FullName;
				if (Project.EndsWith(FwFileExtensions.ksFwDataFallbackFileExtension))
				{
					// The user chose a .bak file, only possible when the fwdata file is missing.
					// Rename it and open it.
					string bakFileName = Project;
					Project = Path.ChangeExtension(bakFileName, FwFileExtensions.ksFwDataXmlFileExtension);
					try
					{
						File.Move(bakFileName, Project);
					}
					catch (IOException)
					{
						// Source does not exist, or destination does. Maybe the user reinstated it? Go ahead and try to open it.
						return;
					}
					catch (UnauthorizedAccessException)
					{
						Project = null;
						MessageBox.Show(this, string.Format(FwCoreDlgs.ksUnauthorizedRenameBakFile, bakFileName),
							FwCoreDlgs.ksError, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}
				Server = null;
			}
			else
			{
				Project = m_lstLanguageProjects.SelectedItem.ToString();
				Server = m_hostIpAddressMap[m_hostsTreeView.SelectedNode.Text];
			}
		}

		private string HostsFileName
		{
			get { return Path.Combine(DirectoryFinder.ProjectsDirectory, "HostsManuallyConnected.txt"); }
		}

		private void AddHostButtonClick(object sender, EventArgs e)
		{
			if (AddHost(m_txtAddHost.Text))
			{
				// This is a good host...remember it to try in future.
				using(var stream = new StreamWriter(HostsFileName, true))
					stream.WriteLine(m_txtAddHost.Text);
			}
		}

		private void OpenFwDataProjectLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			// Use 'el cheapo' .Net dlg to find LangProj.
			using (var dlg = new OpenFileDialogAdapter())
			{
				Hide();
				dlg.CheckFileExists = true;
				dlg.InitialDirectory = DirectoryFinder.ProjectsDirectory;
				dlg.RestoreDirectory = true;
				dlg.Title = FwCoreDlgs.ksChooseLangProjectDialogTitle;
				dlg.ValidateNames = true;
				dlg.Multiselect = false;
				dlg.Filter = ResourceHelper.FileFilter(FileFilterType.FieldWorksProjectFiles);
				DialogResult = dlg.ShowDialog(Owner);
				Project = dlg.FileName;
			}
		}

		private void OpenBridgeProjectLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			bool dummy;
			string projectName;
			var success = FLExBridgeHelper.LaunchFieldworksBridge(DirectoryFinder.ProjectsDirectory, null, FLExBridgeHelper.Obtain, null,
				out dummy, out projectName);
			if (!success)
			{
				ReportDuplicateBridge();
				Project = "";
			}
			if (string.IsNullOrWhiteSpace(projectName))
				return; // user canceled.
			if (projectName.EndsWith("lift"))
			{
				Project = CreateProjectFromLift(projectName);
			}
			else
			{
				Project = projectName;
			}
			Server = null;
			if (String.IsNullOrEmpty(Project))
				return; // Don't close the Open project dialog yet (LT-13187)
			// Apparently setting the DialogResult to something other than 'None' is what tells
			// the model dialog that it can close.
			DialogResult =  DialogResult.OK;
			OkButtonClick(null, null);
		}


		/// <summary>
		/// Create a new Fieldworks project and import a lift file into it. Return the .fwdata path.
		/// </summary>
		/// <param name="liftPath"></param>
		public string CreateProjectFromLift(string liftPath)
		{
			var projectName = Path.GetFileNameWithoutExtension(liftPath);
			var parentFolder = Path.GetDirectoryName(liftPath);
			string flexFolderName = Path.Combine(DirectoryFinder.ProjectsDirectory, projectName);
			if (!Directory.Exists(flexFolderName))
				throw new ArgumentException("The lift file is not in a folder within the corresponding Project folder");
			string projectPath;
			FdoCache cache;
			using (var helper = new ThreadHelper())
			using (var progressDlg = new ProgressDialogWithTask(this, helper))
			{
				progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;
				progressDlg.Title = FwCoreDlgs.ksCreatingLiftProject;
				var cacheReceiver = new FdoCache[1]; // a clumsy way of handling an out parameter, consistent with RunTask
				projectPath = (string)progressDlg.RunTask(true, CreateProjectTask, new object[] { liftPath, projectName, helper, cacheReceiver });
				cache = cacheReceiver[0];
			}

			// this is a horrible way to invoke this, but current project organization does not allow us to reference
			// the LexEdDll project, nor is there any straightforward way to move the code we need into some project we can
			// reference, or any obviously suitable project to move it to without creating other References loops.
			// One nasty reflection call seems less technical debt than creating an otherwise unnecessary project.
			// (It puts up its own progress dialog.)
			ReflectionHelper.CallStaticMethod(@"LexEdDll.dll", @"SIL.FieldWorks.XWorks.LexEd.FLExBridgeListener",
				@"ImportObtainedLexicon", cache, liftPath, this);

			ProjectLockingService.UnlockCurrentProject(cache); // finish all saves and completely write the file so we can proceed to open it
			cache.Dispose();

			return projectPath;
		}

		/// <summary>
		/// Method with signature required by ProgressDialogWithTask.RunTask to create the project (and a cache for it)
		/// as a background task while showing the dialog.
		/// </summary>
		/// <param name="progress"></param>
		/// <param name="parameters">A specific list is required...see the first few lines of the method.</param>
		/// <returns></returns>
		private static object CreateProjectTask(IThreadedProgress progress, object[] parameters)
		{
			// Get required parameters. Ideally these would just be the signature of the method, but RunTask requires object[].
			string liftPath = (string) parameters[0];
			string projectName = (string) parameters[1];
			var helper = (ThreadHelper)parameters[2];
			var cacheReceiver = (FdoCache[]) parameters[3];

			IWritingSystem wsVern, wsAnalysis;
			RetrieveDefaultWritingSystemsFromLift(liftPath, out wsVern, out wsAnalysis);

			string projectPath = FdoCache.CreateNewLangProj(progress, projectName, helper, wsAnalysis, wsVern);

			// This is a temporary cache, just to do the import, and AFAIK we have no access to the current
			// user WS. So create it as "English". Put it in the array to return to the caller.
			cacheReceiver[0] = FdoCache.CreateCacheFromLocalProjectFile(projectPath, "en", progress);
			return projectPath;
		}

		private static void RetrieveDefaultWritingSystemsFromLift(string liftPath, out IWritingSystem wsVern,
			out IWritingSystem wsAnalysis)
		{
			var liftReader = new StreamReader(liftPath, Encoding.UTF8);
			string vernWsId, analysisWsId;
			using (var reader = XmlReader.Create(liftReader))
				RetrieveDefaultWritingSystemIdsFromLift(reader, out vernWsId, out analysisWsId);
			var wsManager =
				new PalasoWritingSystemManager(new GlobalFileWritingSystemStore(DirectoryFinder.GlobalWritingSystemStoreDirectory));
			wsManager.GetOrSet(vernWsId, out wsVern);
			wsManager.GetOrSet(analysisWsId, out wsAnalysis);
		}

		/// <summary>
		/// Figure out the best default vernacular and analysis writing systems to use for the input, presumed to represent a LIFT file.
		/// We get the vernacular WS from the first form element nested in a lexical-unit with a lang attribute,
		/// and the analysis WS form the first form element nested in a definition or gloss with a lang attribute.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="vernWs"></param>
		/// <param name="analysisWs"></param>
		/// <returns></returns>
		public static void RetrieveDefaultWritingSystemIdsFromLift(XmlReader reader, out string vernWs, out string analysisWs)
		{
			vernWs = analysisWs = null;
			bool inLexicalUnit = false;
			bool inDefnOrGloss = false;
			while (reader.Read())
			{
				switch (reader.NodeType)
				{
					case XmlNodeType.Element:
						switch (reader.Name)
						{
							case "lexical-unit":
								inLexicalUnit = true;
								break;
							case "definition":
							case "gloss":
								inDefnOrGloss = true;
								break;
							case "form":
								if (inLexicalUnit && string.IsNullOrWhiteSpace(vernWs))
									vernWs = reader.GetAttribute("lang"); // pathologically may leave it null, if so keep trying.
								if (inDefnOrGloss && string.IsNullOrWhiteSpace(analysisWs))
									analysisWs = reader.GetAttribute("lang"); // pathologically may leave it null, if so keep trying.
								if (!string.IsNullOrWhiteSpace(vernWs) && !string.IsNullOrWhiteSpace(analysisWs))
									return; // got all we need, skip rest of file.
							break;
						}
						break;
						case XmlNodeType.EndElement:
						switch (reader.Name)
						{
							case "lexical-unit":
								inLexicalUnit = false;
								break;
							case "definition":
							case "gloss":
								inDefnOrGloss = false;
								break;
						}
						break;
				}
			}
			if (string.IsNullOrWhiteSpace(vernWs))
				vernWs = "fr"; // Arbitrary default (consistent with default creation of new project) if we don't find an entry
			if (string.IsNullOrWhiteSpace(analysisWs))
				analysisWs = "en"; // Arbitrary default if we don't find a sense
		}

		/// <summary>
		/// Reports to the user that a copy of FLExBridge is already running.
		/// NB. Also used by LexTextDll.FLExBridgeListener.
		/// </summary>
		public static void ReportDuplicateBridge()
		{
			MessageBox.Show(FWCoreDlgsErrors.kBridgeAlreadyRunning, FWCoreDlgsErrors.kFlexBridge,
				MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}

		private void HelpButtonClick(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpChooseLangProjectDialog");
		}

		private void m_txtAddHost_TextChanged(object sender, EventArgs e)
		{
			m_btnAddHost.Enabled = !String.IsNullOrEmpty(m_txtAddHost.Text);
		}

		private void ChooseLangProjectDialog_Load(object sender, EventArgs e)
		{
			// If the FLExBridge image is not displayed, collapse its panel.
			OpenBridgeProjectContainer.Panel1Collapsed = !pictureBox1.Visible;
		}
		#endregion
	}
}
