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
using System.Linq;
using System.Xml;
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
		private PropertyTable m_propertyTable;
		private LcmCache m_cache;
		private string m_sUserWs;
		private string m_sNewUserWs;
		private bool m_pluginsUpdated;
		private readonly Dictionary<string, bool> m_plugins = new Dictionary<string, bool>();
		private readonly Dictionary<UpdateSettings.Channels, UpdateChannelMenuItem> m_channels;
		private readonly Dictionary<UpdateSettings.Channels, UpdateChannelMenuItem> m_QaChannels;
		private const string HelpTopic = "khtpLexOptions";
		private IHelpTopicProvider m_helpTopicProvider;

		private FwApplicationSettingsBase m_settings;
		private FwApp App => m_propertyTable?.GetValue<FwApp>("App") ?? m_helpTopicProvider as FwApp;

		public LexOptionsDlg()
		{
			InitializeComponent();
			var optionsTooltip = new ToolTip { AutoPopDelay = 6000, InitialDelay = 400, ReshowDelay = 500, IsBalloon = true };
			optionsTooltip.SetToolTip(groupBox1, LexTextControls.ksUserInterfaceTooltip);
			m_channels = new Dictionary<UpdateSettings.Channels, UpdateChannelMenuItem>
			{
				[UpdateSettings.Channels.Stable] = new UpdateChannelMenuItem(UpdateSettings.Channels.Stable,
					LexTextControls.UpdatesStable, LexTextControls.UpdatesStableDescription),
				[UpdateSettings.Channels.Beta] = new UpdateChannelMenuItem(UpdateSettings.Channels.Beta,
					LexTextControls.UpdatesBeta, LexTextControls.UpdatesBetaDescription),
				[UpdateSettings.Channels.Alpha] = new UpdateChannelMenuItem(UpdateSettings.Channels.Alpha,
					LexTextControls.UpdatesAlpha, LexTextControls.UpdatesAlphaDescription)
			};
			m_QaChannels = new Dictionary<UpdateSettings.Channels, UpdateChannelMenuItem>
			{
				[UpdateSettings.Channels.Nightly] = new UpdateChannelMenuItem(UpdateSettings.Channels.Nightly, "Nightly",
					"DO NOT select this option unless you are an official FieldWorks tester. You might not be able to access your data tomorrow."),
				[UpdateSettings.Channels.Testing] = new UpdateChannelMenuItem(UpdateSettings.Channels.Testing, "Test Model Change",
					"This option is only for testing related to model changes - This will not install a real FieldWorks update")
			};
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
			m_okToPingCheckBox.Checked = m_settings.Reporting.OkToPingBasicUsageData;
			if (Platform.IsWindows)
			{
				if (m_settings.Update == null)
				{
					// REVIEW (Hasso) 2021.07: we could default to Notify as soon as we implement it, but our low-bandwidth
					// users wouldn't appreciate automatic downloads of hundreds of megabytes w/o express consent.
					m_settings.Update = new UpdateSettings { Behavior = UpdateSettings.Behaviors.DoNotCheck };
				}
				gbUpdateChannel.Visible = m_okToAutoupdate.Checked = m_settings.Update.Behavior != UpdateSettings.Behaviors.DoNotCheck;

				m_cbUpdateChannel.Items.AddRange(m_channels.Values.ToArray());
				// Enable the nightly channel only if it is already selected
				if (m_settings.Update.Channel == UpdateSettings.Channels.Nightly || m_settings.Update.Channel == UpdateSettings.Channels.Testing)
				{
					m_cbUpdateChannel.Items.AddRange(m_QaChannels.Values.ToArray());
					m_cbUpdateChannel.SelectedItem = m_QaChannels[m_settings.Update.Channel];
				}
				else
				{
					m_cbUpdateChannel.SelectedItem = m_channels[m_settings.Update.Channel];
				}
			}
			else
			{
				tabControl1.TabPages.Remove(m_tabUpdates);
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			var restartRequired = false;
			if(m_settings.Reporting.OkToPingBasicUsageData != m_okToPingCheckBox.Checked)
			{
				m_settings.Reporting.OkToPingBasicUsageData = m_okToPingCheckBox.Checked;
				restartRequired = true;
			}

			if (Platform.IsWindows)
			{
				var updateSettings = m_settings.Update;
				var oldBehavior = updateSettings.Behavior;
				var oldChannel = updateSettings.Channel;
				updateSettings.Behavior = m_okToAutoupdate.Checked ? UpdateSettings.Behaviors.Download : UpdateSettings.Behaviors.DoNotCheck;
				updateSettings.Channel = ((UpdateChannelMenuItem)m_cbUpdateChannel.SelectedItem).Channel;
				// If the mediator is null, we aren't finished starting yet, so we haven't initiated the check for updates.
				// When we initiate the check, these new settings will already have been saved, so they will be used.
				// If the mediator is not null, the user will need to restart to either stop the download or to check the new channel.
				// Paratext has a way of pausing or checking immediately when the user changes these settings, but FLEx does not yet.
				if (m_mediator != null && (oldBehavior != updateSettings.Behavior || oldChannel != updateSettings.Channel))
				{
					restartRequired = true;
				}
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
				if (m_cache != null)
				{
					//The writing system the user selects for the user interface may not be loaded yet into the project
					//database. Therefore we need to check this first and if it is not we need to load it.
					m_cache.ServiceLocator.WritingSystemManager.GetOrSet(m_sNewUserWs, out var ws);
					m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem = ws;
				}
				// Reload the mediator's string table with the appropriate language data.
				StringTable.Table.Reload(m_sNewUserWs);
				restartRequired = true;
			}

			// Handle installing/uninstalling plugins.
			if (m_lvPlugins.Items.Count > 0 && m_mediator != null)
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
					if (!string.IsNullOrEmpty(shutdownMsg))
						m_mediator.SendMessage(shutdownMsg, null);
					var configfilesNode = managerNode.SelectSingleNode("configfiles");
					var extensionPath = Path.Combine(baseExtensionPath, configfilesNode.Attributes["targetdir"].Value);
					Directory.Delete(extensionPath, true);
					// Leave any dlls in place since they may be shared, or in use for the moment.
				}
			}
			m_settings.Save();
			AutoOpenLastProject = m_autoOpenCheckBox.Checked;
			DialogResult = DialogResult.OK;
			Close();
			if(restartRequired)
			{
				MessageBox.Show(Owner, LexTextControls.RestartToForSettingsToTakeEffect_Content, LexTextControls.RestartToForSettingsToTakeEffect_Title);
			}
		}

		/// <summary>
		/// If this is true and there is a last edited project name stored, FieldWorks will
		/// open that project automatically instead of displaying the usual Welcome dialog.
		/// </summary>
		private bool AutoOpenLastProject
		{
			get => App?.RegistrySettings.AutoOpenLastEditedProject ?? false;
			set
			{
				var app = App;
				if (app != null)
					app.RegistrySettings.AutoOpenLastEditedProject = value;
			}
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, HelpTopic);
		}

		#region IFwExtension Members

		void IFwExtension.Init(LcmCache cache, Mediator mediator, PropertyTable propertyTable)
		{
			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_cache = cache;
			m_helpTopicProvider = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
			m_settings = m_propertyTable.GetValue<FwApplicationSettingsBase>("AppSettings");
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
				var helpProvider = new FlexHelpProvider { HelpNamespace = m_helpTopicProvider.HelpFile };
				helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(HelpTopic));
				helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			}
		}

		#endregion

		/// <remarks>For use only when <c>IFwExtension.Init</c> cannot be called (when the Mediator and PropertyTable don't exist)</remarks>
		public void InitBareBones(IHelpTopicProvider helpTopicProvider)
		{
			m_helpTopicProvider = helpTopicProvider;
			m_settings = new FwApplicationSettings();
			m_sUserWs = FwRegistryHelper.FieldWorksRegistryKey.GetValue(FwRegistryHelper.UserLocaleValueName, "en") as string;
			m_sNewUserWs = m_sUserWs;
			m_userInterfaceChooser.Init(m_sUserWs);

			// The Plugins tab requires the Mediator
			m_labelPluginBlurb.Text = LexTextControls.OpenAProjectBeforeConfiguringPlugins;
			m_labelPluginRights.Visible = false;
			m_lvPlugins.Visible = false;
		}

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

		private void m_okToAutoupdate_CheckedChanged(object sender, EventArgs e)
		{
			gbUpdateChannel.Visible = m_okToAutoupdate.Checked;
		}

		private void m_cbUpdateChannel_SelectedIndexChanged(object sender, EventArgs args)
		{
			m_textChannelDescription.Text = ((UpdateChannelMenuItem)m_cbUpdateChannel.SelectedItem).Description;
		}

		/// <summary>secret handshake for adding "Nightly": If the user pressed Ctrl+Shift+N, add and select the Nightly channel</summary>
		private void m_cbUpdateChannel_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == 14 /* ASCII 14 is ^N */ && (ModifierKeys & Keys.Shift) == Keys.Shift)
			{
				if (!m_cbUpdateChannel.Items.Contains(m_QaChannels[UpdateSettings.Channels.Nightly]))
				{
					m_cbUpdateChannel.Items.AddRange(m_QaChannels.Values.ToArray());
				}
				m_cbUpdateChannel.SelectedItem = m_QaChannels[UpdateSettings.Channels.Nightly];
			}
		}

		private class UpdateChannelMenuItem
		{
			public UpdateSettings.Channels Channel { get; }
			private readonly string m_name;
			public string Description { get; }

			public UpdateChannelMenuItem(UpdateSettings.Channels channel, string name, string description)
			{
				Channel = channel;
				m_name = name;
				Description = description;
			}

			public override string ToString()
			{
				return m_name;
			}
		}
	}
}
