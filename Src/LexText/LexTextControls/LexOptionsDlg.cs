// Copyright (c) 2007-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// <remarks>
// This implements the "Tools/Options" command dialog for Language Explorer.
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;
using SIL.LCModel;
using SIL.PlatformUtilities;
using SIL.Settings;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class LexOptionsDlg : Form, IFwExtension
	{
		private Mediator m_mediator;
		private XCore.PropertyTable m_propertyTable;
		private LcmCache m_cache = null;
		private string m_sUserWs = null;
		private string m_sNewUserWs = null;
		private bool m_pluginsUpdated = false;
		private Dictionary<string, bool> m_plugins = new Dictionary<string, bool>();
		private const string s_helpTopic = "khtpLexOptions";
		private HelpProvider helpProvider;
		private IHelpTopicProvider m_helpTopicProvider;
		private ToolTip optionsTooltip;

		public LexOptionsDlg()
		{
			InitializeComponent();
			optionsTooltip = new ToolTip { AutoPopDelay = 6000, InitialDelay = 400, ReshowDelay = 500, IsBalloon = true };
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
			var appSettings = m_propertyTable.GetValue<FwApplicationSettingsBase>("AppSettings");
			m_okToPingCheckBox.Checked = appSettings.Reporting.OkToPingBasicUsageData;
			if (Platform.IsWindows)
			{
				if (appSettings.Update == null)
				{
					// REVIEW (Hasso) 2021.07: we could default to Notify as soon as we implement it, but our low-bandwidth
					// users wouldn't appreciate automatic downloads of hundreds of megabytes w/o express consent.
					appSettings.Update = new UpdateSettings
					{
						Behavior = UpdateSettings.Behaviors.DoNotCheck,
						Channel = UpdateSettings.Channels.Stable
					};
				}
				m_okToAutoupdate.Checked = appSettings.Update.Behavior != UpdateSettings.Behaviors.DoNotCheck;

				m_cbUpdateChannel.Items.AddRange(new object[]
				{
					UpdateSettings.Channels.Stable, UpdateSettings.Channels.Beta, UpdateSettings.Channels.Alpha
				});
				// Enable the nightly channel if it is already selected or if this is a tester machine (testers must set the FEEDBACK env var)
				if (appSettings.Update.Channel == UpdateSettings.Channels.Nightly || Environment.GetEnvironmentVariable("FEEDBACK") != null)
				{
					m_cbUpdateChannel.Items.Add(UpdateSettings.Channels.Nightly);
				}
				m_cbUpdateChannel.SelectedItem = appSettings.Update.Channel;
			}
			else
			{
				m_tabUpdates.Visible = false;
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			var appSettings = m_propertyTable.GetValue<FwApplicationSettingsBase>("AppSettings");
			appSettings.Reporting.OkToPingBasicUsageData = m_okToPingCheckBox.Checked;

			if (Platform.IsWindows)
			{
				appSettings.Update.Behavior = m_okToAutoupdate.Checked ? UpdateSettings.Behaviors.Download : UpdateSettings.Behaviors.DoNotCheck;
				appSettings.Update.Channel = (UpdateSettings.Channels)Enum.Parse(typeof(UpdateSettings.Channels), m_cbUpdateChannel.Text);
			}

			m_sNewUserWs = m_userInterfaceChooser.NewUserWs;
			if (m_sUserWs != m_sNewUserWs)
			{
				var ci = MiscUtils.GetCultureForWs(m_sNewUserWs);
				if (ci != null)
				{
					FormLanguageSwitchSingleton.Instance.ChangeCurrentThreadUICulture(ci);
					FormLanguageSwitchSingleton.Instance.ChangeLanguage(this);

					if (Platform.IsMono)
					{
						// Mono leaves the wait cursor on, unlike .Net itself.
						Cursor.Current = Cursors.Default;
					}
				}
				// This needs to be consistent with Common/FieldWorks/FieldWorks.SetUICulture().
				FwRegistryHelper.FieldWorksRegistryKey.SetValue(FwRegistryHelper.UserLocaleValueName, m_sNewUserWs);
				//The writing system the user selects for the user interface may not be loaded yet into the project
				//database. Therefore we need to check this first and if it is not we need to load it.
				CoreWritingSystemDefinition ws;
				m_cache.ServiceLocator.WritingSystemManager.GetOrSet(m_sNewUserWs, out ws);
				m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem = ws;
				// Reload the mediator's string table with the appropriate language data.
				StringTable.Table.Reload(m_sNewUserWs);
			}

			// Handle installing/uninstalling plugins.
			if (m_lvPlugins.Items.Count > 0)
			{
				var pluginsToInstall = new List<XmlDocument>();
				var pluginsToUninstall = new List<XmlDocument>();
				foreach (ListViewItem lvi in m_lvPlugins.Items)
				{
					var name = lvi.Text;
					var managerDoc = lvi.Tag as XmlDocument;
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
				var basePluginPath = FwDirectoryFinder.GetCodeSubDirectory(@"Language Explorer\Configuration\Available Plugins");
				// The extension XML files should be stored in the data area, not in the code area.
				// This reduces the need for users to have administrative privileges.
				var baseExtensionPath = Path.Combine(FwDirectoryFinder.DataDirectory, @"Language Explorer\Configuration");
				// Really do the install now.
				foreach (var managerDoc in pluginsToInstall)
				{
					var managerNode = managerDoc.SelectSingleNode("/manager");
					var srcDir = Path.Combine(basePluginPath, managerNode.Attributes["name"].Value);
					var configfilesNode = managerNode.SelectSingleNode("configfiles");
					var extensionPath = Path.Combine(baseExtensionPath, configfilesNode.Attributes["targetdir"].Value);
					Directory.CreateDirectory(extensionPath);
					foreach (XmlNode fileNode in configfilesNode.SelectNodes("file"))
					{
						var filename = fileNode.Attributes["name"].Value;
						var extensionPathname = Path.Combine(extensionPath, filename);
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
					var fwInstallDir = FwDirectoryFinder.CodeDirectory;
					foreach (XmlNode dllNode in managerNode.SelectNodes("dlls/file"))
					{
						var filename = dllNode.Attributes["name"].Value;
						var dllPathname = Path.Combine(fwInstallDir, filename);
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
				foreach (var managerDoc in pluginsToUninstall)
				{
					var managerNode = managerDoc.SelectSingleNode("/manager");
					var shutdownMsg = XmlUtils.GetOptionalAttributeValue(managerNode, "shutdown");
					if (!String.IsNullOrEmpty(shutdownMsg))
						m_mediator.SendMessage(shutdownMsg, null);
					var configfilesNode = managerNode.SelectSingleNode("configfiles");
					var extensionPath = Path.Combine(baseExtensionPath, configfilesNode.Attributes["targetdir"].Value);
					Directory.Delete(extensionPath, true);
					// Leave any dlls in place since they may be shared, or in use for the moment.
				}
			}
			appSettings.Save();
			AutoOpenLastProject = m_autoOpenCheckBox.Checked;
			DialogResult = DialogResult.OK;
		}

		private bool AutoOpenLastProject
		{
			// If set to true and there is a last edited project name stored, FieldWorks will
			// open that project automatically instead of displaying the usual Welcome dialog.
			get
			{
				var app = m_propertyTable.GetValue<FwApp>("App");
				return app.RegistrySettings.AutoOpenLastEditedProject;
			}
			set
			{
				var app = m_propertyTable.GetValue<FwApp>("App");
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

		void IFwExtension.Init(LcmCache cache, Mediator mediator, PropertyTable propertyTable)
		{
			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_cache = cache;
			m_helpTopicProvider = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
			m_sUserWs = m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem.Id;
			m_sNewUserWs = m_sUserWs;
			m_userInterfaceChooser.Init(m_sUserWs);

			// Populate Plugins tab page list.
			var baseConfigPath = FwDirectoryFinder.GetCodeSubDirectory(
				Path.Combine("Language Explorer", "Configuration"));
			var basePluginPath = Path.Combine(baseConfigPath, "Available Plugins");
			// The extension XML files should be stored in the data area, not in the code area.
			// This reduces the need for users to have administrative privileges.
			var baseExtensionPath = Path.Combine(FwDirectoryFinder.DataDirectory,
				Path.Combine("Language Explorer", "Configuration"));
			foreach (var dir in Directory.GetDirectories(basePluginPath))
			{
				Debug.WriteLine(dir);
				// Currently not offering Concorder plugin in FW7, therefore, we
				// can remove the feature until we need to implement. (FWNX-755)
				if (Platform.IsUnix && dir == Path.Combine(basePluginPath, "Concorder"))
					continue;
				var managerPath = Path.Combine(dir, "ExtensionManager.xml");
				if (File.Exists(managerPath))
				{
					var managerDoc = new XmlDocument();
					managerDoc.Load(managerPath);
					var managerNode = managerDoc.SelectSingleNode("/manager");
					m_lvPlugins.SuspendLayout();
					var lvi = new ListViewItem();
					lvi.Tag = managerDoc;
					lvi.Text = managerNode.Attributes["name"].Value;
					lvi.SubItems.Add(managerNode.Attributes["description"].Value);
					// See if it is installed and check the lvi if it is.
					var configfilesNode = managerNode.SelectSingleNode("configfiles");
					var extensionPath = Path.Combine(baseExtensionPath, configfilesNode.Attributes["targetdir"].Value);
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

		private void PrivacyLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			using (Process.Start(llPrivacy.Text)) { }
		}

		/// <remarks>REVIEW (Hasso) 2021.07: is there a better secret handshake for adding "Nightly"? Considering:
		/// * Ctrl-Shift-Click (L10nSharp uses Alt-Shift-Click; this avoids collisions)
		/// * Ctrl-(Shift-)N (for Nightly)
		/// * Type "nightly" (would require some keeping track—ugh)
		/// * Set some environment variable (FEEDBACK=QA_CHANNEL or NIGHTLYPATCHES=ON)
		/// </remarks>
		private void m_cbUpdateChannel_KeyPress(object sender, KeyPressEventArgs e)
		{
			// If the user pressed Ctrl+Shift+N, add and select the Nightly channel
			if (e.KeyChar == 14 /* ASCII 14 is ^N */ && (ModifierKeys & Keys.Shift) == Keys.Shift)
			{
				if (!m_cbUpdateChannel.Items.Contains(UpdateSettings.Channels.Nightly))
				{
					m_cbUpdateChannel.Items.Add(UpdateSettings.Channels.Nightly);
				}
				m_cbUpdateChannel.SelectedItem = UpdateSettings.Channels.Nightly;
			}
		}
	}
}
