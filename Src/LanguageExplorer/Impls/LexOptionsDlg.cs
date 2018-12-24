// Copyright (c) 2007-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Globalization;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.LexText;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;

namespace LanguageExplorer.Impls
{
	public partial class LexOptionsDlg : Form, IFwExtension, IFormReplacementNeeded
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
			_optionsTooltip.SetToolTip(updateGlobalWS, LexTextControls.ksUpdateGlobalWsTooltip);
			_optionsTooltip.SetToolTip(groupBox1, LexTextControls.ksUserInterfaceTooltip);
		}

		/// <summary>
		/// We have to set the checkbox here because the mediator (needed to get the App)
		/// is not set yet in the dialog's constructor.
		/// </summary>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			m_autoOpenCheckBox.Checked = AutoOpenLastProject;
			var appSettings = m_propertyTable.GetValue<IFwApplicationSettings>("AppSettings");
			m_okToPingCheckBox.Checked = appSettings.Reporting.OkToPingBasicUsageData;
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			var appSettings = m_propertyTable.GetValue<IFwApplicationSettings>("AppSettings");
			appSettings.Reporting.OkToPingBasicUsageData = m_okToPingCheckBox.Checked;
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
				CoreWritingSystemDefinition ws;
				m_cache.ServiceLocator.WritingSystemManager.GetOrSet(NewUserWs, out ws);
				m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem = ws;
				// Reload the string table with the appropriate language data.
				StringTable.Table.Reload(NewUserWs);
			}
			appSettings.UpdateGlobalWSStore = !updateGlobalWS.Checked;
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
				var app = m_propertyTable.GetValue<IFlexApp>(LanguageExplorerConstants.App);
				return app.RegistrySettings.AutoOpenLastEditedProject;
			}
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
			var appSettings = m_propertyTable.GetValue<IFwApplicationSettings>("AppSettings");
			updateGlobalWS.Checked = !appSettings.UpdateGlobalWSStore;
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

		private void updateGlobalWS_MouseHover(object sender, EventArgs e)
		{ }
	}
}
