// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class handles the menu sensitivity and function for the dictionary configuration items under Tools->Configure
	/// </summary>
	class DictionaryConfigurationListener : IxCoreColleague
	{
		private Mediator m_mediator;

		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			m_mediator = mediator;
			m_mediator.AddColleague(this);
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			var targets = new List<IxCoreColleague> { this };
			return targets.ToArray();
		}

		/// <summary>
		/// The configure dictionary dialog may be launched any time this tool is active.
		/// Its name is derived from the name of the tool.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayConfigureDictionary(object commandObject,
																		 ref UIItemDisplayProperties display)
		{
			var configurationName = GetDictionaryConfigurationType(m_mediator);
			if(InFriendlyArea && configurationName != null)
			{
				display.Enabled = true;
				display.Visible = true;
				// REVIEW: SHOULD THE "..." BE LOCALIZABLE (BY MAKING IT PART OF THE SOURCE FOR display.Text)?
				display.Text = String.Format(display.Text, configurationName+"...");
			}
			else
			{
				display.Enabled = false;
				display.Visible = false;
			}

			return true; //we've handled this
		}

		/// <summary>
		/// The old configure dialog should not be accessable for tools where the new one has been implemented.
		/// This hides the old menu if we are handling the type and passes the menu handling on to the old handlers otherwise.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayConfigureXmlDocView(object commandObject,
																		 ref UIItemDisplayProperties display)
		{
			if(GetDictionaryConfigurationBaseType(m_mediator) != null)
			{
				display.Visible = false;
				return true;
			}
			return false;
		}

		internal static string GetConfigDialogHelpTopic(Mediator mediator)
		{
			return GetDictionaryConfigurationBaseType(mediator) == "Reversal Index"
				? "khtpConfigureReversalIndex" : "khtpConfigureDictionary";
		}

		/// <summary>
		/// Get the base (non-localized) name of the area in FLEx being configured, such as Dictionary or Reversal Index.
		/// </summary>
		internal static string GetDictionaryConfigurationBaseType(Mediator mediator)
		{
			var toolName = mediator.PropertyTable.GetStringProperty("currentContentControl", null);
			switch (toolName)
			{
				case "reversalToolBulkEditReversalEntries":
				case "reversalToolEditComplete":
					return "Reversal Index";
				case "lexiconBrowse":
				case "lexiconDictionary":
				case "lexiconEdit":
					return "Dictionary";
				default:
					return null;
			}
		}

		/// <summary>
		/// Get the localizable name of the area in FLEx being configured, such as Dictionary of Reversal Index.
		/// </summary>
		internal static string GetDictionaryConfigurationType(Mediator mediator)
		{
			var nonLocalizedConfigurationType = GetDictionaryConfigurationBaseType(mediator);
			switch(nonLocalizedConfigurationType)
			{
				case "Reversal Index":
					return xWorksStrings.ReversalIndex;
				case "Dictionary":
					return xWorksStrings.Dictionary;
				default:
					return null;
			}
		}

		/// <summary>
		/// Get the project-specific directory for holding configurations for the part of FLEx the user is
		/// working in, such as Dictionary or Reversal Index.
		/// </summary>
		internal static string GetProjectConfigurationDirectory(Mediator mediator)
		{
			var lastDirectoryPart = GetInnermostConfigurationDirectory(mediator);
			return GetProjectConfigurationDirectory(mediator, lastDirectoryPart);
		}

		/// <remarks>Useful for querying about an area of FLEx that the user is not in.</remarks>
		internal static string GetProjectConfigurationDirectory(Mediator mediator, string area)
		{
			var cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			return area == null ? null : Path.Combine(FdoFileHelper.GetConfigSettingsDir(cache.ProjectId.ProjectFolder), area);
		}

		/// <summary>
		/// Get the directory for the shipped default configurations for the part of FLEx the user is
		/// working in, such as Dictionary or Reversal Index.
		/// </summary>
		internal static string GetDefaultConfigurationDirectory(Mediator mediator)
		{
			var lastDirectoryPart = GetInnermostConfigurationDirectory(mediator);
			return GetDefaultConfigurationDirectory(lastDirectoryPart);
		}

		/// <remarks>Useful for querying about an area of FLEx that the user is not in.</remarks>
		internal static string GetDefaultConfigurationDirectory(string area)
		{
			return area == null ? null : Path.Combine(FwDirectoryFinder.DefaultConfigurations, area);
		}

		internal const string ReversalIndexConfigurationDirectoryName = "ReversalIndex";
		internal const string DictionaryConfigurationDirectoryName = "Dictionary";

		/// <summary>
		/// Get the name of the innermost directory name for configurations for the part of FLEx the user is
		/// working in, such as Dictionary or Reversal Index.
		/// </summary>
		private static string GetInnermostConfigurationDirectory(Mediator mediator)
		{
			switch (mediator.PropertyTable.GetStringProperty("currentContentControl", null))
			{
				case "reversalToolBulkEditReversalEntries":
				case "reversalToolEditComplete":
					return ReversalIndexConfigurationDirectoryName;
				case "lexiconBrowse":
				case "lexiconDictionary":
				case "lexiconEdit":
					return DictionaryConfigurationDirectoryName;
				default:
					return null;
			}
		}

		/// <summary>
		/// Launch the configure dialog.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnConfigureDictionary(object commandObject)
		{
			using (var dlg = new DictionaryConfigurationDlg(m_mediator))
			{
				var clerk = m_mediator.PropertyTable.GetValue("ActiveClerk", null) as RecordClerk;
				var controller = new DictionaryConfigurationController(dlg, m_mediator, clerk != null ? clerk.CurrentObject : null);
				dlg.Text = String.Format(xWorksStrings.ConfigureTitle, GetDictionaryConfigurationType(m_mediator));
				dlg.HelpTopic = GetConfigDialogHelpTopic(m_mediator);
				dlg.ShowDialog(m_mediator.PropertyTable.GetValue("window") as IWin32Window);
			}
			m_mediator.SendMessage("MasterRefresh", null);
			return true; // message handled
		}

		public bool ShouldNotCall { get; private set; }
		public int Priority { get { return (int)ColleaguePriority.High; } }

		/// <summary>
		/// Determine if the current area is relevant for this listener.
		/// </summary>
		/// <remarks>
		/// Dictionary configurations are only relevant in the Lexicon area.
		/// </remarks>
		protected bool InFriendlyArea
		{
			get
			{
				var areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
				return areaChoice == "lexicon";
			}
		}

		/// <summary>
		/// Returns the current Dictionary or ReversalIndex configuration file.  This may cause a redisplay
		/// of the XHTML view.
		/// </summary>
		public static string GetCurrentConfiguration(Mediator mediator, string currentDirectoryPart = null)
		{
			return GetCurrentConfiguration(mediator, true, currentDirectoryPart);
		}

		/// <summary>
		/// Returns the current Dictionary or ReversalIndex configuration file.  If fUpdate is true, this may
		/// cause a redisplay of the XHTML view.
		/// </summary>
		public static string GetCurrentConfiguration(Mediator mediator, bool fUpdate, string currentDirectoryPart = null)
		{
			string currentConfig = null;
			if (currentDirectoryPart == null)
			{
				currentDirectoryPart = GetInnermostConfigurationDirectory(mediator);
			}
			// Since this is used in the display of the title and XWorksViews sometimes tries to display the title
			// before full initialization (if this view is the one being displayed on startup) test the mediator before continuing.
			if(mediator != null && mediator.PropertyTable != null)
			{
				currentConfig = mediator.PropertyTable.GetStringProperty("DictionaryPublicationLayout", String.Empty);
				if (!currentConfig.Contains(currentDirectoryPart))
				{
					// we've got a config from the other tool
					currentConfig = string.Empty;
				}
				if(String.IsNullOrEmpty(currentConfig) || !File.Exists(currentConfig))
				{
					string defaultPublication = currentDirectoryPart == DictionaryConfigurationDirectoryName ? "Root" : "AllReversalIndexes";
					// If no configuration has yet been selected or the previous selection is invalid,
					// and the value is "publishStem" or "publishRoot", the code will default Root / Stem configuration path
					if (currentConfig != null && currentConfig.ToLower().IndexOf("publish", StringComparison.Ordinal) == 0)
					{
						defaultPublication = currentConfig.Replace("publish", string.Empty);
					}
					// select the project's Root configuration if available; otherwise, select the default Root configuration
					currentConfig = Path.Combine(GetProjectConfigurationDirectory(mediator, currentDirectoryPart), defaultPublication + DictionaryConfigurationModel.FileExtension);
					if(!File.Exists(currentConfig))
					{
						currentConfig = Path.Combine(GetDefaultConfigurationDirectory(currentDirectoryPart), defaultPublication + DictionaryConfigurationModel.FileExtension);
					}
					mediator.PropertyTable.SetProperty("DictionaryPublicationLayout", currentConfig, fUpdate);
				}
			}
			return currentConfig;
		}
	}
}
