// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Gecko;
using Gecko.DOM;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.Lexicon.DictionaryConfiguration;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// Group of dictionary/reversal index configuration utility functions that may be useful in XhtmlDocView
	/// and in the Bulk Edit Reversal Entries area.
	/// </summary>
	public static class DictionaryConfigurationUtils
	{
		private static ContextMenuStrip s_contextMenu;

		/// <summary>
		/// Stores the configuration name as the key, and the file path as the value
		/// User configuration files with the same name as a shipped configuration will trump the shipped
		/// </summary>
		public static SortedDictionary<string, string> GatherBuiltInAndUserConfigurations(LcmCache cache, string configObjectName)
		{
			var configurations = new SortedDictionary<string, string>();
			var defaultConfigs = Directory.EnumerateFiles(Path.Combine(FwDirectoryFinder.DefaultConfigurations, configObjectName), "*" + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
			// for every configuration file in the DefaultConfigurations folder add an entry
			AddOrOverrideConfiguration(defaultConfigs, configurations);
			var projectConfigPath = Path.Combine(LcmFileHelper.GetConfigSettingsDir(cache.ProjectId.ProjectFolder), configObjectName);
			if (Directory.Exists(projectConfigPath))
			{
				var projectConfigs = Directory.EnumerateFiles(projectConfigPath, "*" + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
				// for every configuration in the projects configurations folder either override a shipped configuration or add an entry
				AddOrOverrideConfiguration(projectConfigs, configurations);
			}
			return configurations;
		}

		/// <summary>
		/// Reads just the configuration name out of each configuration file and either adds it to the configurations
		/// dictionary by name or overwrites a previous entry with a new file location.
		/// </summary>
		private static void AddOrOverrideConfiguration(IEnumerable<string> configFiles, IDictionary<string, string> configurations)
		{
			foreach (var configFile in configFiles)
			{
				using (var fileStream = new FileStream(configFile, FileMode.Open, FileAccess.Read))
				using (var reader = XmlReader.Create(fileStream))
				{
					do
					{
						reader.Read();
					} while (reader.NodeType != XmlNodeType.Element);
					// Get the root xml element to grab the "name" value
					var configName = reader["name"];
					if (configName == null)
					{
						throw new InvalidDataException($"{configFile} is an invalid configuration file");
					}
					configurations[configName] = configFile;
				}
			}
		}

		/// <summary>
		/// When a user selects a Reversal Index configuration in the Reversal Indexes area
		/// to describe how to arrange and show their
		/// reversal index entries, or in the
		/// Bulk Edit Reversal Entries area, we also need to set the Reversal Index Guid property to set which
		/// set of reversal index entries should be shown in the XhtmlDocView.
		/// Do that.
		/// </summary>
		public static void SetReversalIndexGuidBasedOnReversalIndexConfiguration(IPropertyTable propertyTable, LcmCache cache)
		{
			var reversalIndexConfiguration = propertyTable.GetValue("ReversalIndexPublicationLayout", String.Empty);
			if (String.IsNullOrEmpty(reversalIndexConfiguration))
			{
				return;
			}
			var model = new DictionaryConfigurationModel(reversalIndexConfiguration, cache);
			var reversalIndexConfigWritingSystemLanguage = model.WritingSystem;
			var currentAnalysisWsList = cache.LanguageProject.AnalysisWritingSystems;
			var wsObj = currentAnalysisWsList.FirstOrDefault(ws => ws.Id == reversalIndexConfigWritingSystemLanguage);
			if (wsObj == null || wsObj.DisplayLabel.ToLower().Contains("audio"))
			{
				return;
			}
			var riRepo = cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			var mHvoRevIdx = riRepo.FindOrCreateIndexForWs(wsObj.Handle).Hvo;
			var revGuid = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(mHvoRevIdx).Guid;
			propertyTable.SetProperty(LanguageExplorerConstants.ReversalIndexGuid, revGuid.ToString(), true, true, SettingsGroup.LocalSettings);
		}

		/// <summary>
		/// Handle the user left clicking on the document view by jumping to an entry, playing a media element, or adjusting the view
		/// </summary>
		/// <remarks>internal so that it can be re-used by the XhtmlRecordDocView</remarks>
		internal static void HandleDomLeftClick(IRecordList recordList, ICmObjectRepository objectRepository, DomMouseEventArgs e, GeckoElement element)
		{
			var topLevelGuid = DictionaryConfigurationServices.GetHrefFromGeckoDomElement(element);
			if (topLevelGuid == Guid.Empty)
			{
				GeckoElement dummy;
				DictionaryConfigurationServices.GetClassListFromGeckoElement(element, out topLevelGuid, out dummy);
			}
			if (topLevelGuid != Guid.Empty)
			{
				var currentObj = recordList.CurrentObject;
				if (currentObj != null && currentObj.Guid == topLevelGuid)
				{
					// don't need to jump, we're already here...
					// unless this is a video link
					if (element is GeckoAnchorElement)
					{
						return; // don't handle the click; gecko will jump to the link
					}
				}
				else
				{
					ICmObject obj;
					if (objectRepository.TryGetObject(topLevelGuid, out obj))
					{
						recordList.JumpToRecord(obj.Hvo);
					}
				}
			}
			e.Handled = true;
		}

		/// <summary>
		/// Pop up a menu to allow the user to start the document configuration dialog, and
		/// start the dialog at the configuration node indicated by the current element.
		/// </summary>
		/// <remarks>
		/// This is static so that the method can be shared with XhtmlRecordDocView.
		/// </remarks>
		internal static void HandleDomRightClick(GeckoWebBrowser browser, DomMouseEventArgs e, GeckoElement element, FlexComponentParameters flexComponentParameters, string configObjectName, LcmCache cache, IRecordList activeRecordList)
		{
			Guid topLevelGuid;
			GeckoElement entryElement;
			var classList = DictionaryConfigurationServices.GetClassListFromGeckoElement(element, out topLevelGuid, out entryElement);
			var localizedName = DictionaryConfigurationServices.GetDictionaryConfigurationType(flexComponentParameters.PropertyTable);
			var label = String.Format(AreaResources.ksConfigure, localizedName);
			s_contextMenu?.Dispose();
			s_contextMenu = new ContextMenuStrip();
			var item = new DisposableToolStripMenuItem(label);
			s_contextMenu.Items.Add(item);
			item.Click += RunConfigureDialogAt;
			item.Tag = new object[] { flexComponentParameters.PropertyTable, flexComponentParameters.Publisher, classList, topLevelGuid, flexComponentParameters.Subscriber, cache, activeRecordList };
			if (e.CtrlKey) // show hidden menu item for tech support
			{
				item = new DisposableToolStripMenuItem(AreaResources.ksInspect);
				s_contextMenu.Items.Add(item);
				item.Click += RunDiagnosticsDialogAt;
				item.Tag = new object[] { flexComponentParameters.PropertyTable, entryElement, topLevelGuid };
			}

			s_contextMenu.Show(browser, new Point(e.ClientX, e.ClientY));
			s_contextMenu.Closed += m_contextMenu_Closed;
			e.Handled = true;
		}

		/// <summary>
		/// Close and delete the context menu if it exists.  This appears to be needed (at least on Linux)
		/// if the user clicks somewhere in the browser other than the menu.
		/// </summary>
		internal static void CloseContextMenuIfOpen()
		{
			if (s_contextMenu == null)
			{
				return;
			}

			s_contextMenu.Close();
			DisposeContextMenu(null, null);
		}

		private static void m_contextMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			Application.Idle += DisposeContextMenu;
		}

		private static void DisposeContextMenu(object sender, EventArgs e)
		{
			Application.Idle -= DisposeContextMenu;
			if (s_contextMenu != null)
			{
				s_contextMenu.Dispose();
				s_contextMenu = null;
			}
		}

		private static void RunConfigureDialogAt(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem)sender;
			var tagObjects = (object[])item.Tag;
			var propertyTable = (IPropertyTable)tagObjects[0];
			var publisher = (IPublisher)tagObjects[1];
			var classList = tagObjects[2] as List<string>;
			var guid = (Guid)tagObjects[3];
			// 4 is used further down
			var cache = (LcmCache)tagObjects[5];
			var activeRecordList = (RecordList)tagObjects[6];
			var mainWindow = propertyTable.GetValue<IFwMainWnd>(FwUtils.window);
			ICmObject current = null;
			if (guid != Guid.Empty && cache != null && cache.ServiceLocator.ObjectRepository.IsValidObjectId(guid))
			{
				current = cache.ServiceLocator.GetObject(guid);
			}
			else if (activeRecordList != null)
			{
				current = activeRecordList.CurrentObject;
			}
			if (DictionaryConfigurationDlg.ShowDialog(new FlexComponentParameters(propertyTable, publisher, (ISubscriber)tagObjects[4]), (Form)mainWindow, current, DictionaryConfigurationServices.GetConfigDialogHelpTopic(propertyTable), DictionaryConfigurationServices.GetDictionaryConfigurationType(propertyTable), classList))
			{
				mainWindow.RefreshAllViews();
			}
		}

		private static void RunDiagnosticsDialogAt(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem)sender;
			var tagObjects = (object[])item.Tag;
			var propTable = (IPropertyTable)tagObjects[0];
			var element = (GeckoElement)tagObjects[1];
			var guid = (Guid)tagObjects[2];
			using (var dlg = new XmlDiagnosticsDlg(element, guid))
			{
				dlg.ShowDialog(propTable.GetValue<IWin32Window>(FwUtils.window));
			}
		}
	}
}