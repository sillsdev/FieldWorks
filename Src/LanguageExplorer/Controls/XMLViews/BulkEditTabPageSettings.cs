// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Reflection;
using System.Xml.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This is the base class that knows common settings used in BulkEdit tabs.
	/// Used to persist those values.
	/// </summary>
	internal class BulkEditTabPageSettings
	{
		#region Member variables

		/// <summary> the bulkEditBar we're getting or settings our values</summary>
		protected BulkEditBar m_bulkEditBar;

		string m_bulkEditBarTabName = string.Empty;
		string m_targetFieldName = string.Empty;

		#endregion Member variables

		/// <summary />
		protected BulkEditTabPageSettings()
		{
		}

		/// <summary>
		/// The tab we expect this class to help load and store its settings.
		/// </summary>
		protected virtual int ExpectedTab => m_bulkEditBar.OperationsTabControl.SelectedIndex;

		/// <summary>
		/// make sure bulkEditBar is in the expected tab state.
		/// </summary>
		protected virtual void CheckExpectedTab()
		{
			if (m_bulkEditBar == null)
			{
				throw new ApplicationException("Expected settings to have initialized m_bulkEditBar.");
			}
			if (m_bulkEditBar.OperationsTabControl.SelectedIndex != ExpectedTab)
			{
				throw new ApplicationException("Expected bulkEditBar to be on tab " + (BulkEditBarTabs) ExpectedTab);
			}
		}

		private bool InExpectedTab()
		{
			return m_bulkEditBar != null && m_bulkEditBar.OperationsTabControl.SelectedIndex == ExpectedTab;
		}

		private bool AreLoaded => GetType().Name != typeof(BulkEditTabPageSettings).Name && m_bulkEditBar != null && TabPageName.Length > 0;

		#region BulkEditBar helper methods

		/// <summary>
		/// Create BulkEditBarTabPage settings for the current tab,
		/// and save them to the property table.
		/// (only effective after initialization (i.e. m_setupOrRestoredBulkEditBarTab)
		/// To restore the settings, use TrySwitchToLastSavedTab() and/or
		/// followed by InitializeSelectedTab().
		/// </summary>
		internal static void CaptureSettingsForCurrentTab(BulkEditBar bulkEditBar)
		{
			// don't capture bulk edit bar settings until we're finished with initialization.
			if (!bulkEditBar.m_setupOrRestoredBulkEditBarTab)
			{
				return;
			}
			var tabPageSettings = GetNewSettingsForSelectedTab(bulkEditBar);
			tabPageSettings.SaveSettings(bulkEditBar);
		}

		internal static BulkEditTabPageSettings GetNewSettingsForSelectedTab(BulkEditBar bulkEditBar)
		{
			BulkEditTabPageSettings tabPageSettings;
			switch (bulkEditBar.OperationsTabControl.SelectedIndex)
			{
				default:
					// by default, just save basic tab info.
					tabPageSettings = new BulkEditTabPageSettings();
					break;
				case (int) BulkEditBarTabs.ListChoice: // list
					tabPageSettings = new ListChoiceTabPageSettings();
					break;
				case (int) BulkEditBarTabs.BulkCopy: // bulk copy
					tabPageSettings = new BulkCopyTabPageSettings();
					break;
				case (int) BulkEditBarTabs.ClickCopy: // click copy
					tabPageSettings = new ClickCopyTabPageSettings();
					break;
				case (int) BulkEditBarTabs.Process: // transduce
					tabPageSettings = new ProcessTabPageSettings();
					break;
				case (int) BulkEditBarTabs.BulkReplace: // find/replace
					tabPageSettings = new BulkReplaceTabPageSettings();
					break;
				case (int) BulkEditBarTabs.Delete: // Delete.
					tabPageSettings = new DeleteTabPageSettings();
					break;
			}
			tabPageSettings.m_bulkEditBar = bulkEditBar;
			return tabPageSettings;
		}

		/// <summary>
		/// Restore last visited BulkEditBar tab index.
		/// After BulkEditBar finishes initializing its state and controls in that tab,
		/// finish restoring the settings in that tab with InitializeSelectedTab()
		/// </summary>
		internal static bool TrySwitchToLastSavedTab(BulkEditBar bulkEditBar)
		{
			// first try to deserialize stored settings.
			var settings = DeserializeLastTabPageSettings(bulkEditBar);
			// get the name of the tab. if we can't get this, no point in continuing to use the settings,
			// because all other settings depend upon the tab.
			if (!settings.AreLoaded)
			{
				return false;
			}
			var fOk = true;
			// try switching to saved tab.
			try
			{
				var tab = (BulkEditBarTabs) Enum.Parse(typeof(BulkEditBarTabs), settings.TabPageName);
				bulkEditBar.OperationsTabControl.SelectedIndex = (int) tab;
			}
			catch
			{
				// something went wrong trying to restore tab, so assume we didn't switch to a saved one.
				fOk = false;
			}
			return fOk;
		}

		/// <summary>
		/// Try to restore settings for selected tab, otherwise use defaults.
		/// </summary>
		internal static void InitializeSelectedTab(BulkEditBar bulkEditBar)
		{
			BulkEditTabPageSettings tabPageSettings;
			if (TryGetSettingsForCurrentTabPage(bulkEditBar, out tabPageSettings))
			{
				// now that we've loaded/setup a tab, restore the settings for that tab.
				try
				{
					tabPageSettings.SetupBulkEditBarTab(bulkEditBar);
				}
				catch
				{
					// oh well, we tried, just continue with what we could setup, if anything.
				}
			}
			else
			{
				// we didn't restore saved settings, but we may want to initialize defaults instead.
				tabPageSettings = GetNewSettingsForSelectedTab(bulkEditBar);
				tabPageSettings.SetupBulkEditBarTab(bulkEditBar);
			}

			tabPageSettings.SetupApplyPreviewButtons();
		}

		private static bool TryGetSettingsForCurrentTabPage(BulkEditBar bulkEditBar, out BulkEditTabPageSettings tabPageSettings)
		{
			var currentTabSettingsKey = BuildCurrentTabSettingsKey(bulkEditBar);
			tabPageSettings = DeserializeTabPageSettings(bulkEditBar, currentTabSettingsKey);
			return tabPageSettings.AreLoaded;
		}

		/// <summary>
		/// Check that we've changed to BulkEditBar to ExpectedTab,
		/// and then set BulkEditBar to those tab settings
		/// </summary>
		protected virtual void SetupBulkEditBarTab(BulkEditBar bulkEditBar)
		{
			if (m_bulkEditBar == null || !AreLoaded)
			{
				return;
			}
			CheckExpectedTab();
			SetTargetCombo();
			// first load target field name. other settings may depend upon this.
			SetTargetField();
		}

		/// <summary>
		/// Update Preview/Clear and Apply Button states.
		/// </summary>
		protected virtual void SetupApplyPreviewButtons()
		{
			m_bulkEditBar.SetupApplyPreviewButtons(true, true, null);
		}

		/// <summary />
		protected virtual void SetTargetField()
		{
			if (m_bulkEditBar.CurrentTargetCombo == null)
			{
				return;
			}
			m_bulkEditBar.CurrentTargetCombo.Text = TargetFieldName;
			if (m_bulkEditBar.CurrentTargetCombo.SelectedIndex == -1)
			{
				// by default select the first item
				if (m_bulkEditBar.CurrentTargetCombo.Items.Count > 0)
				{
					m_bulkEditBar.CurrentTargetCombo.SelectedIndex = 0;
				}
			}
			if (!m_bulkEditBar.m_setupOrRestoredBulkEditBarTab)
			{
				// if we haven't already been setup, we should explicitly trigger to
				// say our target field has changed, in case the RecordList needs to
				// reload accordingly.
				InvokeTargetComboSelectedIndexChanged();
			}
		}

		private void SetTargetCombo()
		{
			m_bulkEditBar.CurrentTargetCombo = TargetComboForTab;
		}

		/// <summary>
		/// the target combo for a particular tab page.
		/// </summary>
		protected virtual FwOverrideComboBox TargetComboForTab => null;

		/// <summary>
		/// this is a hack that explictly triggers the currentTargetCombo.SelectedIndexChange delegates
		/// during initialization, since they do not fire automatically until after everything is setup.
		/// </summary>
		protected virtual void InvokeTargetComboSelectedIndexChanged()
		{
			// EricP. Couldn't figure out how to do this by reflection generally.
			// so override in each tab settings.
		}

		/// <summary>
		/// Construct the property table key based upon the tool using this BulkEditBar.
		/// </summary>
		private static string BuildLastTabSettingsKey(BulkEditBar bulkEditBar)
		{
			var toolId = GetBulkEditBarToolId(bulkEditBar);
			var property = $"{toolId}_LastTabPageSettings";
			return property;
		}

		private static string BuildCurrentTabSettingsKey(BulkEditBar bulkEditBar)
		{
			var toolId = GetBulkEditBarToolId(bulkEditBar);
			var property = $"{toolId}_{GetCurrentTabPageName(bulkEditBar)}_TabPageSettings";
			return property;
		}


		/// <summary />
		internal static string GetBulkEditBarToolId(BulkEditBar bulkEditBar)
		{
			return bulkEditBar.ConfigurationNode.Attribute("toolId").Value;
		}

		/// <summary>
		/// Serialize the settings for a bulk edit bar tab, and store it in the property table.
		/// </summary>
		protected virtual void SaveSettings(BulkEditBar bulkEditBar)
		{
			m_bulkEditBar = bulkEditBar;
			var settingsXml = SerializeSettings();
			// first store current tab settings in the property table.
			var currentTabSettingsKey = BuildCurrentTabSettingsKey(bulkEditBar);
			bulkEditBar.PropertyTable.SetProperty(currentTabSettingsKey, settingsXml, true, settingsGroup: SettingsGroup.LocalSettings);
			// next store the *key* to the current tab settings in the property table.
			var lastTabSettingsKey = BuildLastTabSettingsKey(bulkEditBar);
			bulkEditBar.PropertyTable.SetProperty(lastTabSettingsKey, currentTabSettingsKey, true, settingsGroup: SettingsGroup.LocalSettings);
		}

		private string SerializeSettings()
		{
			return SIL.Xml.XmlSerializationHelper.SerializeToString(this, true);
		}

		/// <summary>
		/// factory returning a tab settings object, if we found them in the property table.
		/// </summary>
		private static BulkEditTabPageSettings DeserializeLastTabPageSettings(BulkEditBar bulkEditBar)
		{
			var lastTabSettingsKey = BuildLastTabSettingsKey(bulkEditBar);
			// the value of LastTabSettings is the key to the tab settings in the property table.
			var tabSettingsKey = bulkEditBar.PropertyTable.GetValue(lastTabSettingsKey, string.Empty, SettingsGroup.LocalSettings);
			return DeserializeTabPageSettings(bulkEditBar, tabSettingsKey);
		}

		private static BulkEditTabPageSettings DeserializeTabPageSettings(BulkEditBar bulkEditBar, string tabSettingsKey)
		{
			var settingsXml = string.Empty;
			if (tabSettingsKey.Length > 0)
			{
				settingsXml = bulkEditBar.PropertyTable.GetValue(tabSettingsKey, string.Empty, SettingsGroup.LocalSettings);
			}
			BulkEditTabPageSettings restoredTabPageSettings = null;
			if (settingsXml.Length > 0)
			{
				// figure out type/class of object to deserialize from xml data.
				var doc = XDocument.Parse(settingsXml);
				var className = doc.Root.Name.LocalName;
				// get the type from the xml itself.
				var assembly = Assembly.GetExecutingAssembly();
				// if we can find an existing class/type, we can try to deserialize to it.
				var basicTabPageSettings = new BulkEditTabPageSettings();
				var pgSettingsType = basicTabPageSettings.GetType();
				var baseClassTypeName = pgSettingsType.FullName.Split('+')[0];
				var targetType = assembly.GetType(baseClassTypeName + "+" + className, false);

				// deserialize
				restoredTabPageSettings = (BulkEditTabPageSettings)XmlSerializationHelper.DeserializeXmlString(settingsXml, targetType);
			}
			if (restoredTabPageSettings == null)
			{
				restoredTabPageSettings = new BulkEditTabPageSettings();
			}
			restoredTabPageSettings.m_bulkEditBar = bulkEditBar;
			return restoredTabPageSettings;
		}

		/// <summary />
		protected bool CanLoadFromBulkEditBar()
		{
			return InExpectedTab();
		}

		/// <summary>
		/// after deserializing, determine if the target combo was able to get
		/// set to the persisted value.
		/// </summary>
		/// <returns></returns>
		protected bool HasExpectedTargetSelected()
		{
			return m_bulkEditBar.CurrentTargetCombo.Text == TargetFieldName;
		}

		#endregion BulkEditBar helper methods

		#region Tab properties to serialize

		/// <summary>
		/// The current tab page. Typically this is used to set the bulk edit bar into the current
		/// tab before SetupBulkEditBarTab adjusts settings for that tab.
		/// </summary>
		private string TabPageName
		{
			get
			{
				if (string.IsNullOrEmpty(m_bulkEditBarTabName) && m_bulkEditBar != null)
				{
					var tabPageName = GetCurrentTabPageName(m_bulkEditBar);
					m_bulkEditBarTabName = tabPageName;
				}
				return m_bulkEditBarTabName ?? (m_bulkEditBarTabName = string.Empty);
			}
		}

		/// <summary />
		protected static string GetCurrentTabPageName(BulkEditBar bulkEditBar)
		{
			var selectedTabIndex = bulkEditBar.OperationsTabControl.SelectedIndex;
			var tab = (BulkEditBarTabs)Enum.Parse(typeof(BulkEditBarTabs), selectedTabIndex.ToString());
			return tab.ToString();
		}


		/// <summary>
		/// The name of item selected in the Target Combo box.
		/// </summary>
		private string TargetFieldName
		{
			get
			{
				if (string.IsNullOrEmpty(m_targetFieldName) && CanLoadFromBulkEditBar() && m_bulkEditBar.CurrentTargetCombo != null)
				{
					m_targetFieldName = m_bulkEditBar.CurrentTargetCombo.Text;
				}
				return m_targetFieldName ?? (m_targetFieldName = string.Empty);
			}
		}

		#endregion Tab properties to serialize
	}
}