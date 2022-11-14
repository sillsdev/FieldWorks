// Copyright (c) 2007-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Globalization;
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
		private const string s_helpTopic = "khtpLexOptions";
		private HelpProvider _helpProvider;
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
					// TODO (before sending to users): appSettings.Update = new UpdateSettings { Behavior = UpdateSettings.Behaviors.DoNotCheck };
					appSettings.Update = new UpdateSettings { Channel = UpdateSettings.Channels.Nightly };
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
			var appSettings = m_propertyTable.GetValue<IFwApplicationSettings>(FwUtilsConstants.AppSettings);
			appSettings.Reporting.OkToPingBasicUsageData = m_okToPingCheckBox.Checked;
			if (Platform.IsWindows)
			{
				appSettings.Update.Behavior = m_okToAutoupdate.Checked ? UpdateSettings.Behaviors.Download : UpdateSettings.Behaviors.DoNotCheck;
				appSettings.Update.Channel = (UpdateSettings.Channels)Enum.Parse(typeof(UpdateSettings.Channels), m_cbUpdateChannel.Text);
			}
			NewUserWs = m_userInterfaceChooser.NewUserWs;
			if (m_sUserWs != NewUserWs)
			{
				var ci = MiscUtils.GetCultureForWs(NewUserWs);
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
				FwRegistryHelper.FieldWorksRegistryKey.SetValue(FwRegistryHelper.UserLocaleValueName, NewUserWs);
				//The writing system the user selects for the user interface may not be loaded yet into the project
				//database. Therefore we need to check this first and if it is not we need to load it.
				m_cache.ServiceLocator.WritingSystemManager.GetOrSet(NewUserWs, out var ws);
				m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem = ws;
				// Reload the string table with the appropriate language data.
				StringTable.Table.Reload(NewUserWs);
			}
			appSettings.Save();
			AutoOpenLastProject = m_autoOpenCheckBox.Checked;
			DialogResult = DialogResult.OK;
		}

		private bool AutoOpenLastProject
		{
			// If set to true and there is a last edited project name stored, FieldWorks will
			// open that project automatically instead of displaying the usual Welcome dialog.
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
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
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
			_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
			_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
		}

		#endregion

		public string NewUserWs { get; private set; }

		private void PrivacyLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			using (Process.Start(llPrivacy.Text)) { }
		}

		/// <remarks>REVIEW (Hasso) 2021.07: is there a better secret handshake for adding "Nightly"? Considering:
		/// * Ctrl-Shift-Click (L10nSharp uses Alt-Shift-Click; this avoids collisions)
		/// * Ctrl-(Shift-)N (for Nightly)
		/// * Type "nightly" (would require some keeping trackâ€”ugh)
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
