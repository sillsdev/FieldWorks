// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: LexOptionsDlg.cs
// Responsibility: Steve McConnel
// Last reviewed:
//
// <remarks>
// This implements the "Tools/Options" command dialog for Language Explorer.
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using SIL.CoreImpl;
using SIL.CoreImpl.Properties;
using SIL.FieldWorks.Common.Framework;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using XCore;
#if !__MonoCS__
using NetSparkle;
#endif

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class LexOptionsDlg : Form, IFwExtension
	{
		private Mediator m_mediator = null;
		private FdoCache m_cache = null;
		private string m_sUserWs = null;
		private string m_sNewUserWs = null;
		private bool m_pluginsUpdated = false;
		private Dictionary<string, bool> m_plugins = new Dictionary<string, bool>();
		private const string s_helpTopic = "khtpLexOptions";
		private HelpProvider helpProvider;
		private IHelpTopicProvider m_helpTopicProvider;
		private ToolTip optionsTooltip;

		internal bool m_failedToConnectToService;

		public LexOptionsDlg()
		{
			InitializeComponent();
#if __MonoCS__
			tabControl1.Controls.Remove(m_tabUpdates);
#endif
			optionsTooltip = new ToolTip { AutoPopDelay = 6000, InitialDelay = 400, ReshowDelay = 500, IsBalloon = true };
			optionsTooltip.SetToolTip(updateGlobalWS, LexTextControls.ksUpdateGlobalWsTooltip);
			optionsTooltip.SetToolTip(groupBox1, LexTextControls.ksUserInterfaceTooltip);
		}

		/// <summary>
		/// We have to set the checkbox here because the mediator (needed to get the App)
		/// is not set yet in the dialog's constructor.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			m_autoOpenCheckBox.Checked = AutoOpenLastProject;
			m_okToPingCheckBox.Checked = Settings.Default.Reporting.OkToPingBasicUsageData;
			checkForUpdatesBox.Checked = Settings.Default.AutoCheckForUpdates;
			includeBetasBox.Checked = Settings.Default.CheckForBetaUpdates;
			includeBetasBox.Enabled = checkForUpdatesBox.Checked;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		private void m_btnOK_Click(object sender, EventArgs e)
		{
			var reportingSettings = Settings.Default.Reporting;
			reportingSettings.OkToPingBasicUsageData = m_okToPingCheckBox.Checked;
			Settings.Default.AutoCheckForUpdates = checkForUpdatesBox.Checked;
			Settings.Default.CheckForBetaUpdates = includeBetasBox.Checked;

			Settings.Default.AutoCheckForUpdates = checkForUpdatesBox.Checked;
			Settings.Default.CheckForBetaUpdates = includeBetasBox.Checked;

#if !__MonoCS__
			var sparkle = SingletonsContainer.Item("Sparkle") as Sparkle;
			if (sparkle != null)
			{
				var appCastUrl = Settings.Default.IsBTE
									? (Settings.Default.CheckForBetaUpdates
										? CoreImpl.Properties.Resources.ResourceManager.GetString("kstidAppcastBteBetasUrl")
										: CoreImpl.Properties.Resources.ResourceManager.GetString("kstidAppcastBteUrl"))
									: (Settings.Default.CheckForBetaUpdates
										? CoreImpl.Properties.Resources.ResourceManager.GetString("kstidAppcastSeBetasUrl")
										: CoreImpl.Properties.Resources.ResourceManager.GetString("kstidAppcastSeUrl"));
				sparkle.AppcastUrl = appCastUrl;
			}
#endif


			Settings.Default.Save();
			m_sNewUserWs = m_userInterfaceChooser.NewUserWs;
			if (m_sUserWs != m_sNewUserWs)
			{
				CultureInfo ci = MiscUtils.GetCultureForWs(m_sNewUserWs);
				if (ci != null)
				{
					FormLanguageSwitchSingleton.Instance.ChangeCurrentThreadUICulture(ci);
					FormLanguageSwitchSingleton.Instance.ChangeLanguage(this);
#if __MonoCS__
					// Mono leaves the wait cursor on, unlike .Net itself.
					Cursor.Current = Cursors.Default;
#endif
				}
				// This needs to be consistent with Common/FieldWorks/FieldWorks.SetUICulture().
				FwRegistryHelper.FieldWorksRegistryKey.SetValue(FwRegistryHelper.UserLocaleValueName, m_sNewUserWs);
				//The writing system the user selects for the user interface may not be loaded yet into the project
				//database. Therefore we need to check this first and if it is not we need to load it.
				IWritingSystem ws;
				m_cache.ServiceLocator.WritingSystemManager.GetOrSet(m_sNewUserWs, out ws);
				m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem = ws;
				// Reload the mediator's string table with the appropriate language data.
				m_mediator.StringTbl.Reload(m_sNewUserWs);
			}

			// Handle installing/uninstalling plugins.
			if (m_lvPlugins.Items.Count > 0)
			{
				List<XmlDocument> pluginsToInstall = new List<XmlDocument>();
				List<XmlDocument> pluginsToUninstall = new List<XmlDocument>();
				foreach (ListViewItem lvi in m_lvPlugins.Items)
				{
					string name = lvi.Text;
					XmlDocument managerDoc = lvi.Tag as XmlDocument;
					if (lvi.Checked && !m_plugins[name])
					{
						// Remember we need to install it.
						pluginsToInstall.Add(managerDoc);
					}
					else if (!lvi.Checked && m_plugins[name])
					{
						// Remember we need to uninstall it.
						pluginsToUninstall.Add(managerDoc);
					}
				}
				m_pluginsUpdated = pluginsToInstall.Count > 0 || pluginsToUninstall.Count > 0;
				string basePluginPath = FwDirectoryFinder.GetCodeSubDirectory(@"Language Explorer\Configuration\Available Plugins");
				// The extension XML files should be stored in the data area, not in the code area.
				// This reduces the need for users to have administrative privileges.
				string baseExtensionPath = Path.Combine(FwDirectoryFinder.DataDirectory, @"Language Explorer\Configuration");
				// Really do the install now.
				foreach (XmlDocument managerDoc in pluginsToInstall)
				{
					XmlNode managerNode = managerDoc.SelectSingleNode("/manager");
					string srcDir = Path.Combine(basePluginPath, managerNode.Attributes["name"].Value);
					XmlNode configfilesNode = managerNode.SelectSingleNode("configfiles");
					string extensionPath = Path.Combine(baseExtensionPath, configfilesNode.Attributes["targetdir"].Value);
					Directory.CreateDirectory(extensionPath);
					foreach (XmlNode fileNode in configfilesNode.SelectNodes("file"))
					{
						string filename = fileNode.Attributes["name"].Value;
						string extensionPathname = Path.Combine(extensionPath, filename);
						try
						{
							File.Copy(
								Path.Combine(srcDir, filename),
								extensionPathname,
								true);
							File.SetAttributes(extensionPathname, FileAttributes.Normal);
						}
						catch
						{
							// Eat copy exception.
						}
					}
					string fwInstallDir = FwDirectoryFinder.CodeDirectory;
					foreach (XmlNode dllNode in managerNode.SelectNodes("dlls/file"))
					{
						string filename = dllNode.Attributes["name"].Value;
						string dllPathname = Path.Combine(fwInstallDir, filename);
						try
						{
							File.Copy(
								Path.Combine(srcDir, filename),
								dllPathname,
								true);
							File.SetAttributes(dllPathname, FileAttributes.Normal);
						}
						catch
						{
							// Eat copy exception.
						}
					}
				}
				// Really do the uninstall now.
				foreach (XmlDocument managerDoc in pluginsToUninstall)
				{
					XmlNode managerNode = managerDoc.SelectSingleNode("/manager");
					string shutdownMsg = XmlUtils.GetOptionalAttributeValue(managerNode, "shutdown");
					if (!String.IsNullOrEmpty(shutdownMsg))
						m_mediator.SendMessage(shutdownMsg, null);
					XmlNode configfilesNode = managerNode.SelectSingleNode("configfiles");
					string extensionPath = Path.Combine(baseExtensionPath, configfilesNode.Attributes["targetdir"].Value);
					Directory.Delete(extensionPath, true);
					// Leave any dlls in place since they may be shared, or in use for the moment.
				}
			}
			CoreImpl.Properties.Settings.Default.UpdateGlobalWSStore = !updateGlobalWS.Checked;
			CoreImpl.Properties.Settings.Default.Save();
			AutoOpenLastProject = m_autoOpenCheckBox.Checked;
			DialogResult = DialogResult.OK;
		}

		private bool AutoOpenLastProject
		{
			// If set to true and there is a last edited project name stored, FieldWorks will
			// open that project automatically instead of displaying the usual Welcome dialog.
			get
			{
				var app = m_mediator.PropertyTable.GetValue("App") as FwApp;
				return app.RegistrySettings.AutoOpenLastEditedProject;
			}
			set
			{
				var app = m_mediator.PropertyTable.GetValue("App") as FwApp;
				if (app != null)
					app.RegistrySettings.AutoOpenLastEditedProject = value;
			}
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			// TODO: Implement.
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		#region IFwExtension Members

		void IFwExtension.Init(FdoCache cache, Mediator mediator)
		{
			updateGlobalWS.Checked = !CoreImpl.Properties.Settings.Default.UpdateGlobalWSStore;
			m_mediator = mediator;
			m_cache = cache;
			m_helpTopicProvider = mediator.HelpTopicProvider;
			m_sUserWs = m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem.Id;
			m_sNewUserWs = m_sUserWs;
			m_userInterfaceChooser.SuppressKeyTermLocalizationLangs = true;
			m_userInterfaceChooser.Init(m_sUserWs);

			// Populate Plugins tab page list.
			var baseConfigPath = FwDirectoryFinder.GetCodeSubDirectory(
				Path.Combine("Language Explorer", "Configuration"));
			string basePluginPath = Path.Combine(baseConfigPath, "Available Plugins");
			// The extension XML files should be stored in the data area, not in the code area.
			// This reduces the need for users to have administrative privileges.
			string baseExtensionPath = Path.Combine(FwDirectoryFinder.DataDirectory,
				Path.Combine("Language Explorer", "Configuration"));
			foreach (string dir in Directory.GetDirectories(basePluginPath))
			{
				Debug.WriteLine(dir);
				// Currently not offering Concorder plugin in FW7, therefore, we
				// can remove the feature until we need to implement. (FWNX-755)
				if(MiscUtils.IsUnix && dir == Path.Combine(basePluginPath, "Concorder"))
					continue;
				string managerPath = Path.Combine(dir, "ExtensionManager.xml");
				if (File.Exists(managerPath))
				{
					XmlDocument managerDoc = new XmlDocument();
					managerDoc.Load(managerPath);
					XmlNode managerNode = managerDoc.SelectSingleNode("/manager");
					m_lvPlugins.SuspendLayout();
					ListViewItem lvi = new ListViewItem();
					lvi.Tag = managerDoc;
					lvi.Text = managerNode.Attributes["name"].Value;
					lvi.SubItems.Add(managerNode.Attributes["description"].Value);
					// See if it is installed and check the lvi if it is.
					XmlNode configfilesNode = managerNode.SelectSingleNode("configfiles");
					string extensionPath = Path.Combine(baseExtensionPath, configfilesNode.Attributes["targetdir"].Value);
					lvi.Checked = Directory.Exists(extensionPath);
					m_plugins.Add(lvi.Text, lvi.Checked); // Remember original installed state.
					m_lvPlugins.Items.Add(lvi);
					m_lvPlugins.ResumeLayout();
				}
			}

			if (m_helpTopicProvider != null) // Will be null when running tests
			{
				helpProvider = new HelpProvider();
				helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
				helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
				helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
		}

		#endregion

		public string NewUserWs
		{
			get { return m_sNewUserWs; }
		}

		public bool PluginsUpdated
		{
			get { return m_pluginsUpdated; }
		}

		private void updateGlobalWS_MouseHover(object sender, EventArgs e)
		{
			;
		}

		private void checkForUpdatesBox_CheckedChanged(object sender, EventArgs e)
		{
			includeBetasBox.Enabled = checkForUpdatesBox.Checked;
		}
	}
}
