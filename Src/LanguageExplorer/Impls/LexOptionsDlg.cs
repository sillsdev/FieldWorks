// Copyright (c) 2007-2022 SIL International
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
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using SIL.Settings;

namespace LanguageExplorer.Impls
{
	internal partial class LexOptionsDlg : Form, IFwExtension, IFormReplacementNeeded
	{
		private IPropertyTable m_propertyTable;
		private LcmCache m_cache;
		private string m_sUserWs;
		private const string HelpTopic = "khtpLexOptions";
		private HelpProvider _helpProvider;
		private string m_sNewUserWs;
		private readonly Dictionary<UpdateSettings.Channels, UpdateChannelMenuItem> m_channels;
		private readonly UpdateChannelMenuItem m_NightlyChannel;
		private readonly Dictionary<UpdateSettings.Channels, UpdateChannelMenuItem> m_QaChannels;
		private IHelpTopicProvider m_helpTopicProvider;
		private ToolTip _optionsTooltip;

		public LexOptionsDlg()
		{
			InitializeComponent();
			_optionsTooltip = new ToolTip
			{
				AutoPopDelay = 6000,
				InitialDelay = 400,
				ReshowDelay = 500,
				IsBalloon = true
			};
			m_channels = new Dictionary<UpdateSettings.Channels, UpdateChannelMenuItem>
			{
				[UpdateSettings.Channels.Stable] = new UpdateChannelMenuItem(UpdateSettings.Channels.Stable,
					LanguageExplorerControls.UpdatesStable, LanguageExplorerControls.UpdatesStableDescription),
				[UpdateSettings.Channels.Beta] = new UpdateChannelMenuItem(UpdateSettings.Channels.Beta,
					LanguageExplorerControls.UpdatesBeta, LanguageExplorerControls.UpdatesBetaDescription),
				[UpdateSettings.Channels.Alpha] = new UpdateChannelMenuItem(UpdateSettings.Channels.Alpha,
					LanguageExplorerControls.UpdatesAlpha, LanguageExplorerControls.UpdatesAlphaDescription)
			};
			m_NightlyChannel =  new UpdateChannelMenuItem(UpdateSettings.Channels.Nightly, "Nightly",
				"DO NOT select this option unless you are an official FieldWorks tester. You might not be able to access your data tomorrow.");
			m_QaChannels = new Dictionary<UpdateSettings.Channels, UpdateChannelMenuItem>
			{
				[UpdateSettings.Channels.Nightly] = new UpdateChannelMenuItem(UpdateSettings.Channels.Nightly, "Nightly",
					"DO NOT select this option unless you are an official FieldWorks tester. You might not be able to access your data tomorrow."),
				[UpdateSettings.Channels.Testing] = new UpdateChannelMenuItem(UpdateSettings.Channels.Testing, "Test Model Change",
					"This option is only for testing related to model changes - This will not install a real FieldWorks update")
			};

			_optionsTooltip.SetToolTip(groupBox1, LanguageExplorerControls.ksUserInterfaceTooltip);
		}

		/// <summary>
		/// We have to set the checkbox here because the mediator (needed to get the App)
		/// is not set yet in the dialog's constructor.
		/// </summary>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			m_autoOpenCheckBox.Checked = AutoOpenLastProject;
			var appSettings = m_propertyTable.GetValue<IFwApplicationSettings>(FwUtilsConstants.AppSettings);
			m_okToPingCheckBox.Checked = appSettings.Reporting.OkToPingBasicUsageData;
			if (Platform.IsWindows)
			{
				if (appSettings.Update == null)
				{
					// REVIEW (Hasso) 2021.07: we could default to Notify as soon as we implement it, but our low-bandwidth
					// users wouldn't appreciate automatic downloads of hundreds of megabytes w/o express consent.
					appSettings.Update = new UpdateSettings { Behavior = UpdateSettings.Behaviors.DoNotCheck };
				}
				gbUpdateChannel.Visible = m_okToAutoupdate.Checked = appSettings.Update.Behavior != UpdateSettings.Behaviors.DoNotCheck;

				m_cbUpdateChannel.Items.AddRange(m_channels.Values.ToArray());
				// Enable the nightly channel only if it is already selected
				if (appSettings.Update.Channel == UpdateSettings.Channels.Nightly || appSettings.Update.Channel == UpdateSettings.Channels.Testing)
				{
					m_cbUpdateChannel.Items.Add(m_NightlyChannel);
					m_cbUpdateChannel.SelectedItem = m_NightlyChannel;
					m_cbUpdateChannel.Items.AddRange(m_QaChannels.Values.ToArray());
					m_cbUpdateChannel.SelectedItem = m_QaChannels[appSettings.Update.Channel];
				}
				else
				{
					m_cbUpdateChannel.SelectedItem = m_channels[appSettings.Update.Channel];
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
			var appSettings = m_propertyTable.GetValue<IFwApplicationSettings>(FwUtilsConstants.AppSettings);
			appSettings.Reporting.OkToPingBasicUsageData = m_okToPingCheckBox.Checked;
			if(appSettings.Reporting.OkToPingBasicUsageData != m_okToPingCheckBox.Checked)
			{
					appSettings.Reporting.OkToPingBasicUsageData = m_okToPingCheckBox.Checked;
				restartRequired = true;
			}

			if (Platform.IsWindows)
			{
				var updateSettings = appSettings.Update;
				var oldBehavior = updateSettings.Behavior;
				var oldChannel = updateSettings.Channel;
				updateSettings.Behavior = m_okToAutoupdate.Checked ? UpdateSettings.Behaviors.Download : UpdateSettings.Behaviors.DoNotCheck;
				updateSettings.Channel = ((UpdateChannelMenuItem)m_cbUpdateChannel.SelectedItem).Channel;
				// HASSOTODO: review the behavior here now that there is no mediator
				if (oldBehavior != updateSettings.Behavior || oldChannel != updateSettings.Channel)
				{
					restartRequired = true;
				}
			}

			NewUserWs = m_userInterfaceChooser.NewUserWs;
			if (m_sUserWs != NewUserWs)
			{
				var ci = MiscUtils.GetCultureForWs(NewUserWs);

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
				FwRegistryHelper.FieldWorksRegistryKey.SetValue(FwRegistryHelper.UserLocaleValueName, NewUserWs);
				//The writing system the user selects for the user interface may not be loaded yet into the project
				//database. Therefore we need to check this first and if it is not we need to load it.
				m_cache.ServiceLocator.WritingSystemManager.GetOrSet(NewUserWs, out var ws);
				m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem = ws;
				// Reload the string table with the appropriate language data.
				StringTable.Table.Reload(NewUserWs);
				restartRequired = true;
			}
			appSettings.Save();
			AutoOpenLastProject = m_autoOpenCheckBox.Checked;
			DialogResult = DialogResult.OK;
			Close();
			if(restartRequired)
			{
				MessageBox.Show(Owner, LanguageExplorerControls.RestartToForSettingsToTakeEffect_Content, LanguageExplorerControls.RestartToForSettingsToTakeEffect_Title);
			}
		}

		/// <summary>
		/// If this is true and there is a last edited project name stored, FieldWorks will
		/// open that project automatically instead of displaying the usual Welcome dialog.
		/// </summary>
		private bool AutoOpenLastProject
		{
			get => m_propertyTable.GetValue<IFlexApp>(LanguageExplorerConstants.App).RegistrySettings.AutoOpenLastEditedProject;
			set
			{
				var app = m_propertyTable.GetValue<IFlexApp>(LanguageExplorerConstants.App);
				if (app != null)
				{
					app.RegistrySettings.AutoOpenLastEditedProject = value;
				}
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

		void IFwExtension.Init(LcmCache cache, IPropertyTable propertyTable, IPublisher publisher)
		{
			m_propertyTable = propertyTable;
			m_cache = cache;
			m_helpTopicProvider = m_propertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider);
			m_sUserWs = m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem.Id;
			NewUserWs = m_sUserWs;
			m_userInterfaceChooser.Init(m_sUserWs);
			if (m_helpTopicProvider == null)
			{
				return;
			}
			_helpProvider = new HelpProvider
			{
				HelpNamespace = m_helpTopicProvider.HelpFile
			};
			_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(HelpTopic));
			_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
		}

		#endregion

		/// <summary>
		/// Used for situations where a project is not yet loaded, as in the Welcome screen
		/// </summary>
		internal void InitHelpTopicOnly(IHelpTopicProvider provider)
		{
			m_helpTopicProvider = provider;
			_helpProvider = new HelpProvider
			{
				HelpNamespace = m_helpTopicProvider.HelpFile
			};
			_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(HelpTopic));
			_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
		}
		public string NewUserWs { get; private set; }

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
				if (!m_cbUpdateChannel.Items.Contains(m_NightlyChannel))
				if (!m_cbUpdateChannel.Items.Contains(m_QaChannels[UpdateSettings.Channels.Nightly]))
				{
					m_cbUpdateChannel.Items.Add(m_NightlyChannel);
					m_cbUpdateChannel.Items.AddRange(m_QaChannels.Values.ToArray());
				}
				m_cbUpdateChannel.SelectedItem = m_NightlyChannel;
				m_cbUpdateChannel.SelectedItem = m_QaChannels[UpdateSettings.Channels.Nightly];
			}
		}

		internal class UpdateChannelMenuItem
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
