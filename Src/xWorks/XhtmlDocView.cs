// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Gecko;
using Palaso.UI.WindowsForms.HtmlBrowser;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class handles the display of configured xhtml for a particular publication in a dynamically loadable XWorksView.
	/// </summary>
	internal class XhtmlDocView : XWorksViewBase, IFindAndReplaceContext, IPostLayoutInit
	{
		private XWebBrowser m_mainView;
		private DictionaryPublicationDecorator m_pubDecorator;
		internal string m_configObjectName;

		public override void Init(Mediator mediator, XmlNode configurationParameters)
		{
			m_mediator = mediator;
			m_configurationParameters = configurationParameters;
			m_mainView = new XWebBrowser(XWebBrowser.BrowserType.GeckoFx);
			m_mainView.Dock = DockStyle.Fill;
			m_mainView.Location = new Point(0, 0);
			m_mainView.IsWebBrowserContextMenuEnabled = false;
			ReadParameters();
			// Use update helper to help with optimizations and special cases for list loading
			using(var luh = new RecordClerk.ListUpdateHelper(Clerk, Clerk.ListLoadingSuppressed))
			{
				m_mediator.AddColleague(this);
				Clerk.UpdateOwningObjectIfNeeded();
			}
			Controls.Add(m_mainView);

			var browser = m_mainView.NativeBrowser as GeckoWebBrowser;
			if (browser != null)
			{
				var clerk = XmlUtils.GetOptionalAttributeValue(configurationParameters, "clerk");
				if (clerk == "entries" || clerk == "AllReversalEntries")
				{
					browser.DomClick += OnDomClick;
					browser.DomKeyPress += OnDomKeyPress;
				}
			}
		}

		/// <summary>
		/// Handle a key press in the web browser displaying the xhtml. [TODO: LT-xxxxx]
		/// </summary>
		private void OnDomKeyPress(object sender, DomKeyEventArgs e)
		{
			System.Diagnostics.Debug.WriteLine(String.Format(@"DEBUG: OnDomKeyPress({0}, {1})", sender, e));
		}

		/// <summary>
		/// Handle a mouse click in the web browser displaying the xhtml.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "element does NOT need to be disposed locally!")]
		private void OnDomClick(object sender, DomMouseEventArgs e)
		{
			CloseContextMenuIfOpen();
			var browser = m_mainView.NativeBrowser as GeckoWebBrowser;
			if (browser == null)
				return;
			var element = browser.DomDocument.ElementFromPoint(e.ClientX, e.ClientY);
			if (element == null || element.TagName == "html")
				return;
			if (e.Button == GeckoMouseButton.Left)
			{
				// Select the entry represented by the current element.  [TODO: LT-xxxxx]
				e.Handled = true;
			}
			else if (e.Button == GeckoMouseButton.Right)
			{
				HandleDomRightClick(browser, e, element, m_mediator, m_configObjectName);
			}
		}

		/// <summary>
		/// Pop up a menu to allow the user to start the document configuration dialog, and
		/// start the dialog at the configuration node indicated by the current element.
		/// </summary>
		/// <remarks>
		/// This is static so that the method can be shared with XhtmlRecordDocView.
		/// </remarks>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "ToolStripMenuItem gets added to m_contextMenu.Items; ContextMenuStrip is disposed in DisposeContextMenu()")]
		internal static void HandleDomRightClick(GeckoWebBrowser browser, DomMouseEventArgs e,
			GeckoElement element, Mediator mediator, string configObjectName)
		{
			var classList = new List<string>();
			for (var elem = element; elem != null; elem = elem.ParentElement)
			{
				if (elem.TagName == "body" || elem.TagName == "html")
					break;
				var className = elem.GetAttribute("class");
				if (!String.IsNullOrEmpty(className))
				{
					if (className == "letHead")
						return;
					classList.Insert(0, className);
				}
			}
			var label = String.Format(xWorksStrings.ksConfigure, configObjectName);
			s_contextMenu = new ContextMenuStrip();
			var item = new ToolStripMenuItem(label);
			s_contextMenu.Items.Add(item);
			item.Click += RunConfigureDialogAt;
			item.Tag = new object[] { mediator, classList };
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
			if (s_contextMenu != null)
			{
				s_contextMenu.Close();
				DisposeContextMenu(null, null);
			}
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

		// Context menu exists just for one invocation (until idle).
		private static ContextMenuStrip s_contextMenu;

		private static void RunConfigureDialogAt(object sender, EventArgs e)
		{
			var item = (ToolStripMenuItem)sender;
			var tagObjects = item.Tag as object[];
			var mediator = tagObjects[0] as Mediator;
			var classList = tagObjects[1] as List<string>;
			using (var dlg = new DictionaryConfigurationDlg(mediator))
			{
				var clerk = mediator.PropertyTable.GetValue("ActiveClerk", null) as RecordClerk;
				var controller = new DictionaryConfigurationController(dlg, mediator, clerk != null ? clerk.CurrentObject : null);
				controller.SetStartingNode(classList);
				dlg.Text = String.Format(xWorksStrings.ConfigureTitle, DictionaryConfigurationListener.GetDictionaryConfigurationType(mediator));
				dlg.HelpTopic = DictionaryConfigurationListener.GetConfigDialogHelpTopic(mediator);
				dlg.ShowDialog(mediator.PropertyTable.GetValue("window") as IWin32Window);
			}
			mediator.SendMessage("MasterRefresh", null);
		}

		public override int Priority
		{
			get { return (int)ColleaguePriority.High; }
		}

		/// <summary>
		/// We wait until containing controls are laid out to try to set the content
		/// </summary>
		public void PostLayoutInit()
		{
			// Tell the Clerk it is active so it will update the list of entries. Pass false as we have no toolbar to update.
			Clerk.ActivateUI(false);
			// Update the entry list if necessary
			if(!Clerk.ListLoadingSuppressed && Clerk.RequestedLoadWhileSuppressed)
			{
				Clerk.UpdateList(true, true);
			}
			// Grab the selected publication and make sure that we grab a valid configuration for it.
			// In some cases (e.g where a user reset their local settings) the stored configuration may no longer
			// exist on disk.
			var pubName = GetCurrentPublication();
			var currentConfig = GetCurrentConfiguration();
			var validConfiguration = GetValidConfigurationForPublication(pubName);
			if(validConfiguration != currentConfig)
			{
				m_mediator.PropertyTable.SetProperty("DictionaryPublicationLayout", validConfiguration, true);
			}
			UpdateContent(PublicationDecorator, validConfiguration);
		}

		/// <summary>
		/// Populate the list of publications for the first dictionary titlebar menu.
		/// </summary>
		/// <returns></returns>
		public bool OnDisplayPublications(object parameter, ref UIListDisplayProperties display)
		{
			List<string> inConfig;
			List<string> notInConfig;
			SplitPublicationsByConfiguration(Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS,
														GetCurrentConfiguration(),
														out inConfig, out notInConfig);
			foreach(var pub in inConfig)
			{
				display.List.Add(pub, pub, null, null);
			}
			if(notInConfig.Any())
			{
				display.List.Add(new SeparatorItem());
				foreach(var pub in notInConfig)
				{
					display.List.Add(pub, pub, null, null);
				}
			}
			return true;
		}

		/// <summary>
		/// Populate the list of dictionary configuration views for the second dictionary titlebar menu.
		/// </summary>
		/// <remarks>The areaconfiguration.xml defines the "Configurations" menu and the XWorksViews event handling calls this</remarks>
		public bool OnDisplayConfigurations(object parameter, ref UIListDisplayProperties display)
		{
			IDictionary<string, string> hasPub;
			IDictionary<string, string> doesNotHavePub;
			var allConfigurations = GatherBuiltInAndUserConfigurations();
			SplitConfigurationsByPublication(allConfigurations,
														GetCurrentPublication(),
														out hasPub, out doesNotHavePub);
			// Add menu items that display the configuration name and send PropChanges with
			// the configuration path.
			foreach(var config in hasPub)
			{
				display.List.Add(config.Key, config.Value, null, null);
			}
			if(doesNotHavePub.Count > 0)
			{
				display.List.Add(new SeparatorItem());
				foreach(var config in doesNotHavePub)
				{
					display.List.Add(config.Key, config.Value, null, null);
				}
			}
			return true;
		}

		/// <summary>
		/// Read in the parameters to determine which sequence/collection we are editing.
		/// </summary>
		protected override void ReadParameters()
		{
			base.ReadParameters();
			var backColorName = XmlUtils.GetOptionalAttributeValue(m_configurationParameters,
				"backColor", "Window");
			BackColor = Color.FromName(backColorName);
			m_configObjectName = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "configureObjectName", null);
		}

		/// <summary>
		/// We aren't using the record clerk for this view so we override this method to do nothing.
		/// </summary>
		protected override void SetupDataContext()
		{
		}

		/// <summary>
		/// Stores the configuration name as the key, and the file path as the value
		/// User configuration files with the same name as a shipped configuration will trump the shipped
		/// </summary>
		/// <seealso cref="DictionaryConfigurationController.ListDictionaryConfigurationChoices()"/>
		/// <returns></returns>
		internal SortedDictionary<string, string> GatherBuiltInAndUserConfigurations()
		{
			var configurations = new SortedDictionary<string, string>();
			var defaultConfigs = Directory.EnumerateFiles(Path.Combine(FwDirectoryFinder.DefaultConfigurations, m_configObjectName),
																			"*" + DictionaryConfigurationModel.FileExtension);
			// for every configuration file in the DefaultConfigurations folder add an entry
			AddOrOverrideConfiguration(defaultConfigs, configurations);
			var projectConfigPath = Path.Combine(FdoFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder),
																m_configObjectName);
			if(Directory.Exists(projectConfigPath))
			{
				var projectConfigs = Directory.EnumerateFiles(projectConfigPath, "*" + DictionaryConfigurationModel.FileExtension);
				// for every configuration in the projects configurations folder either override a shipped configuration or add an entry
				AddOrOverrideConfiguration(projectConfigs, configurations);
			}
			return configurations;
		}

		/// <summary>
		/// Reads just the configuration name out of each configuration file and either adds it to the configurations
		/// dictionary by name or overwrites a previous entry with a new file location.
		/// </summary>
		private static void AddOrOverrideConfiguration(IEnumerable<string> configFiles,
																	  IDictionary<string, string> configurations)
		{
			foreach(var configFile in configFiles)
			{
				using(var fileStream = new FileStream(configFile, FileMode.Open, FileAccess.Read))
				using(var reader = XmlReader.Create(fileStream))
				{
					do
					{
						reader.Read();
					} while(reader.NodeType != XmlNodeType.Element);
					// Get the root xml element to grab the "name" value
					var configName = reader["name"];
					if(configName == null)
						throw new InvalidDataException(String.Format("{0} is an invalid configuration file",
																					configFile));
					configurations[configName] = configFile;
				}
			}
		}

		/// <summary>
		/// Handle the 'Edit Publications' menu item click (defined in the Lexicon areaConfiguration.xml)
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public virtual bool OnJumpToTool(object commandObject)
		{
			var coreCommand = commandObject as Command;
			if(coreCommand != null)
			{
				var tool = XmlUtils.GetManditoryAttributeValue(coreCommand.Parameters[0], "tool");
				if(tool != "publicationsEdit")
					return false;

				var fwLink = new FwLinkArgs(tool, Guid.Empty);
				m_mediator.PostMessage("FollowLink", fwLink);
				return true;
			}
			return false;
		}

		/// <summary>
		/// All publications which the given configuration apply to will be placed in the inConfig collection.
		/// All publications which the configuration does not apply to will be placed in the notInConfig collection.
		/// </summary>
		internal void SplitPublicationsByConfiguration(IFdoOwningSequence<ICmPossibility> publications,
																	  string configurationPath,
																	  out List<string> inConfig,
																	  out List<string> notInConfig)
		{
			inConfig = new List<string>();
			notInConfig = new List<string>();
			var config = new DictionaryConfigurationModel(configurationPath, Cache);
			foreach(var pub in publications)
			{
				var name = pub.Name.UserDefaultWritingSystem.Text;
				if(config.AllPublications || config.Publications.Contains(name))
				{
					inConfig.Add(name);
				}
				else
				{
					notInConfig.Add(name);
				}
			}
		}

		/// <summary>
		/// All the configurations which apply to the given publication will be placed in the hasPub dictionary.
		/// All that do not apply to it will be placed in the doesNotHavePub dictionary.
		/// </summary>
		internal void SplitConfigurationsByPublication(IDictionary<string, string> configurations,
																	  string publication,
																	  out IDictionary<string, string> hasPub,
																	  out IDictionary<string, string> doesNotHavePub)
		{
			hasPub = new SortedDictionary<string, string>();
			doesNotHavePub = new SortedDictionary<string, string>();
			foreach(var config in configurations)
			{
				var model = new DictionaryConfigurationModel(config.Value, Cache);
				if(model.AllPublications || publication.Equals(xWorksStrings.AllEntriesPublication)
												 || model.Publications.Contains(publication))
				{
					hasPub[config.Key] = config.Value;
				}
				else
				{
					doesNotHavePub[config.Key] = config.Value;
				}
			}
		}

		internal string GetValidPublicationForConfiguration(string configuration)
		{
			var currentPub = GetCurrentPublication();
			List<string> inConfinguration;
			List<string> notInConfinguration;
			SplitPublicationsByConfiguration(Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS,
														configuration,
														out inConfinguration, out notInConfinguration);
			return inConfinguration.Contains(currentPub) ? currentPub : inConfinguration.FirstOrDefault();
		}

		internal string GetValidConfigurationForPublication(string publication)
		{
			if(publication == xWorksStrings.AllEntriesPublication)
				return GetCurrentConfiguration();
			var currentConfig = GetCurrentConfiguration();
			var allConfigurations = GatherBuiltInAndUserConfigurations();
			IDictionary<string, string> hasPub;
			IDictionary<string, string> doesNotHavePub;
			SplitConfigurationsByPublication(allConfigurations,
														publication,
														out hasPub, out doesNotHavePub);
			// If the current configuration is already valid use it otherwise return
			// the first valid one or null if no configurations have the publication.
			if(hasPub.Values.Contains(currentConfig))
			{
				return currentConfig;
			}
			return hasPub.Count > 0 ? hasPub.First().Value : null;
		}

		/// <summary>
		/// Receives the broadcast message "PropertyChanged"
		/// </summary>
		public void OnPropertyChanged(string name)
		{
			switch(name)
			{
				case "SelectedPublication":
					var pubDecorator = PublicationDecorator;
					var pubName = GetCurrentPublication();
					var currentConfiguration = GetCurrentConfiguration();
					var validConfiguration = GetValidConfigurationForPublication(pubName);
					if(validConfiguration != currentConfiguration)
					{
						m_mediator.PropertyTable.SetProperty("DictionaryPublicationLayout", validConfiguration, true);
					}
					UpdateContent(pubDecorator, validConfiguration);
					break;
				case "DictionaryPublicationLayout":
				case "ReversalIndexPublicationLayout":
					var currentConfig = GetCurrentConfiguration();
					currentConfig = GetCurrentConfigForReversalIndex(name, currentConfig);
					var currentPublication = GetCurrentPublication();
					var validPublication = GetValidPublicationForConfiguration(currentConfig) ?? xWorksStrings.AllEntriesPublication;
					if(validPublication != currentPublication)
					{
						m_mediator.PropertyTable.SetProperty("SelectedPublication", validPublication, true);
					}
					SetReversalIndexOnPropertyDlg();
					UpdateContent(PublicationDecorator, currentConfig);
					break;
				default:
					// Not sure what other properties might change, but I'm not doing anything.
					break;
			}
		}

		/// <summary>
		/// Method to handle the reversalIndex selection from the Pane-Bar combo box, It is special scenario for Reversal Index
		/// </summary>
		/// <param name="name">Name of the property which affected say "ReversalIndexPublicationLayout"</param>
		/// <param name="currentConfig">Configuration which is from DictionaryPublicationLayout, Which may be default</param>
		/// <returns></returns>
		private string GetCurrentConfigForReversalIndex(string name, string currentConfig)
		{
			if (name != "ReversalIndexPublicationLayout") return currentConfig;
			var allConfigurations = GatherBuiltInAndUserConfigurations();
			var reversalIndexGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(m_mediator, "ReversalIndexGuid");
			var currentReversalIndex = Cache.ServiceLocator.GetObject(reversalIndexGuid) as IReversalIndex;
			if (currentReversalIndex != null && allConfigurations.Keys.Contains(currentReversalIndex.ShortName))
			{
				currentConfig = allConfigurations[currentReversalIndex.ShortName];
				m_mediator.PropertyTable.SetProperty("DictionaryPublicationLayout", currentConfig, true);
				SetReversalIndexOnPropertyDlg();
			}
			return currentConfig;
		}


		/// <summary>
		/// Method which set the current writing system when selected in ConfigureReversalIndexDialog
		/// </summary>
		private void SetReversalIndexOnPropertyDlg()
		{
			var currWsPath = m_mediator.PropertyTable.GetStringProperty("DictionaryPublicationLayout", string.Empty);
			var currWsName = Path.GetFileNameWithoutExtension(currWsPath);
			var currentAnalysisWsList = Cache.LanguageProject.CurrentAnalysisWritingSystems;
			foreach (var wsObj in currentAnalysisWsList.Where(wsObj => wsObj.DisplayLabel == currWsName))
			{
				if (wsObj == null && wsObj.DisplayLabel.ToLower().IndexOf("audio", StringComparison.Ordinal) >= 0) return;

				var riRepo = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
				var mHvoRevIdx = riRepo.FindOrCreateIndexForWs(wsObj.Handle).Hvo;
				Guid revGuid = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(mHvoRevIdx).Guid;
				m_mediator.PropertyTable.SetProperty("ReversalIndexGuid", revGuid.ToString());
				m_mediator.PropertyTable.SetPropertyPersistence("ReversalIndexGuid", true);
				break;
			}
		}

		public void OnMasterRefresh(object sender)
		{
			var currentConfig = GetCurrentConfiguration();
			var currentPublication = GetCurrentPublication();
			var validPublication = GetValidPublicationForConfiguration(currentConfig) ?? xWorksStrings.AllEntriesPublication;
			if (validPublication != currentPublication)
			{
				m_mediator.PropertyTable.SetProperty("SelectedPublication", validPublication, true);
			}
			UpdateContent(PublicationDecorator, currentConfig);
		}

		public virtual bool OnDisplayShowAllEntries(object commandObject, ref UIItemDisplayProperties display)
		{
			var pubName = GetCurrentPublication();
			display.Enabled = true;
			display.Checked = (xWorksStrings.AllEntriesPublication == pubName);
			return true;
		}

		public bool OnShowAllEntries(object args)
		{
			m_mediator.PropertyTable.SetProperty("SelectedPublication", xWorksStrings.AllEntriesPublication);
			return true;
		}

		private void UpdateContent(DictionaryPublicationDecorator publicationDecorator, string configurationFile)
		{
			SetInfoBarText();
			if(String.IsNullOrEmpty(configurationFile))
			{
				m_mainView.DocumentText = String.Format("<html><body>{0}</body></html>", xWorksStrings.NoConfigsMatchPub);
				return;
			}
			var configuration = new DictionaryConfigurationModel(configurationFile, Cache);
			publicationDecorator.Refresh();
			var entriesToPublish = publicationDecorator.GetEntriesToPublish(m_mediator, Clerk.VirtualFlid);
			var baseName = MakeFilenameSafeForHtml(Path.GetFileNameWithoutExtension(configurationFile));
			var basePath = Path.Combine(Path.GetTempPath(), "DictionaryPreview", baseName);
			Directory.CreateDirectory(Path.GetDirectoryName(basePath));
			var xhtmlPath = basePath + ".xhtml";
			var cssPath = basePath + ".css";
			ConfiguredXHTMLGenerator.SavePublishedHtmlWithStyles(entriesToPublish, publicationDecorator, configuration, m_mediator, xhtmlPath, cssPath);
			m_mainView.Url = new Uri(xhtmlPath);
			m_mainView.Refresh(WebBrowserRefreshOption.Completely);
		}

		/// <summary>
		/// Interpreting the xhtml, gecko doesn't load css files with a # character in it.  (probably because it carries meaning in a URL)
		/// It's probably safe to assume that : and ? characters would also cause problems.
		/// </summary>
		public static string MakeFilenameSafeForHtml(string name)
		{
			if (name == null)
				return String.Empty;
			return name.Replace('#', '-').Replace('?', '-').Replace(':', '-');
		}

		private string GetCurrentPublication()
		{
			// Returns the current publication and use '$$all_entries$$' if none has yet been set
			return m_mediator.PropertyTable.GetStringProperty("SelectedPublication",
																			  xWorksStrings.AllEntriesPublication);
		}

		private string GetCurrentConfiguration()
		{
			return DictionaryConfigurationListener.GetCurrentConfiguration(m_mediator);
		}

		public DictionaryPublicationDecorator PublicationDecorator
		{
			get
			{
				if(m_pubDecorator == null)
				{
					m_pubDecorator = new DictionaryPublicationDecorator(Cache, Clerk.VirtualListPublisher, Clerk.VirtualFlid);
				}
				var pubName = GetCurrentPublication();
				if(xWorksStrings.AllEntriesPublication == pubName)
				{
					// A null publication means show everything
					m_pubDecorator.Publication = null;
				}
				else
				{
					// look up the publication object

					var pub = (from item in Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS
									where item.Name.UserDefaultWritingSystem.Text == pubName
									select item).FirstOrDefault();
					if(pub != null && pub != m_pubDecorator.Publication)
					{
						// change the publication if it is different from the current one
						m_pubDecorator.Publication = pub;
					}
				}
				return m_pubDecorator;
			}
		}

		private void SetConfigViewTitle()
		{
			var maxViewWidth = Width/2 - kSpaceForMenuButton;
			var allConfigurations = GatherBuiltInAndUserConfigurations();
			string curViewName;
			var currentConfig = GetCurrentConfiguration();
			if(allConfigurations.ContainsValue(currentConfig))
			{
				curViewName = allConfigurations.First(item => item.Value == currentConfig).Key;
			}
			else
			{
				curViewName = allConfigurations.First().Key;
			}
			// Limit length of View title to remaining available width
			curViewName = TrimToMaxPixelWidth(Math.Max(2, maxViewWidth), curViewName);
			ResetSpacer(maxViewWidth, curViewName);
		}

		/// <summary>
		/// Sets the text on the info bar that appears above the main view panel.
		/// The xml configuration file that this view is based on can configure the text in several ways.
		/// </summary>
		protected override void SetInfoBarText()
		{
			if(m_informationBar == null)
				return;
			var titleStr = GetBaseTitleStringFromConfig();
			var isReversalIndex = DictionaryConfigurationListener.GetDictionaryConfigurationBaseType(m_mediator) == "Reversal Index";
			if (isReversalIndex && Clerk.OwningObject.ShortName != null)
				titleStr = Clerk.OwningObject.ShortName;
			if (titleStr == string.Empty)
			{
				base.SetInfoBarText();
				return;
			}
			if (!isReversalIndex)
			{
				// Set the configuration part of the title
				SetConfigViewTitle();
				//Set the publication part of the title
				var pubNameTitlePiece = GetCurrentPublication();
				if (pubNameTitlePiece == xWorksStrings.AllEntriesPublication)
					pubNameTitlePiece = xWorksStrings.ksAllEntries;
				titleStr = pubNameTitlePiece + " " + titleStr;
			}
			((IPaneBar)m_informationBar).Text = titleStr;
		}

		private const int kSpaceForMenuButton = 26;


		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			SetInfoBarText();
		}

		public string FindTabHelpId { get; private set; }
	}
}
