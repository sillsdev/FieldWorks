// Copyright (c) 2014-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Gecko;
using LanguageExplorer.DictionaryConfiguration;
using LanguageExplorer.DictionaryConfiguration.DictionaryDetailsView;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.Windows.Forms;

namespace LanguageExplorer.Areas.Lexicon.DictionaryConfiguration
{
	public partial class DictionaryConfigurationDlg : Form, IDictionaryConfigurationView
	{
		/// <summary>
		/// When manage views is clicked tell the controller to launch the dialog where different
		/// dictionary configurations (or views) are managed.
		/// </summary>
		public event EventHandler ManageConfigurations;

		public event SwitchConfigurationEvent SwitchConfiguration;
		IPropertyTable m_propertyTable;
		private string m_helpTopic;
		private readonly HelpProvider m_helpProvider;
		private IHelpTopicProvider m_helpTopicProvider;

		/// <summary>
		/// When OK or Apply are clicked tell anyone who is listening to do their save.
		/// </summary>
		public event EventHandler SaveModel;

		/// <summary>
		/// Create the dlg and show it.
		/// </summary>
		/// <returns>'true', if caller needs to refresh the main window, otherwise 'false'.</returns>
		internal static bool ShowDialog(FlexComponentParameters flexComponentParameters, Form mainWindow, ICmObject currentObject, string helpTopic, string titleCore, List<string> classList = null)
		{
			bool refreshNeeded;
			using (var dlg = new DictionaryConfigurationDlg(flexComponentParameters.PropertyTable))
			{
				var controller = new DictionaryConfigurationController(dlg, currentObject);
				controller.InitializeFlexComponent(flexComponentParameters);
				if (classList != null)
				{
					controller.SetStartingNode(classList);
				}
				dlg.Text = string.Format(LexiconResources.ConfigureTitle, titleCore);
				dlg.HelpTopic = helpTopic;
				dlg.ShowDialog(mainWindow);
				refreshNeeded = controller.MasterRefreshRequired;
			}
			return refreshNeeded;
		}

		private DictionaryConfigurationDlg(IPropertyTable propertyTable)
		{
			m_propertyTable = propertyTable;
			InitializeComponent();

			m_preview.Dock = DockStyle.Fill;
			m_preview.Location = new Point(0, 0);
			previewDetailSplit.Panel1.Controls.Add(m_preview);
			manageConfigs_treeDetailButton_split.IsSplitterFixed = true;
			treeDetail_Button_Split.IsSplitterFixed = true;
			MinimumSize = new Size(m_grpConfigurationManagement.Width + 3, manageConfigs_treeDetailButton_split.Height);

			m_helpTopicProvider = propertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider);
			m_helpProvider = new HelpProvider { HelpNamespace = m_helpTopicProvider.HelpFile };
			m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(HelpTopic));
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			m_helpProvider.SetShowHelp(this, true);

			// Restore the location and size from last time we called this dialog.
			if (m_propertyTable == null)
			{
				return;
			}
			var locWnd = m_propertyTable.GetValue<object>("DictionaryConfigurationDlg_Location");
			var szWnd = m_propertyTable.GetValue<object>("DictionaryConfigurationDlg_Size");
			if (locWnd == null || szWnd == null)
			{
				return;
			}
			var rect = new Rectangle((Point)locWnd, (Size)szWnd);
			ScreenHelper.EnsureVisibleRect(ref rect);
			DesktopBounds = rect;
			StartPosition = FormStartPosition.Manual;
		}

		internal string HelpTopic
		{
			get
			{
				if (string.IsNullOrEmpty(m_helpTopic))
				{
					m_helpTopic = "khtpConfigureDictionary";
				}
				return m_helpTopic;
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					return;
				}
				m_helpTopic = value;
				m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(HelpTopic));
			}
		}

		public DictionaryConfigurationTreeControl TreeControl => treeControl;

		public IDictionaryDetailsView DetailsView
		{
			set
			{
				if (detailsView != null)
				{
					return;
				}
				detailsView = (DetailsView)value;
				previewDetailSplit.Panel2.Controls.Add(detailsView);
				detailsView.Dock = DockStyle.Fill;
				detailsView.Location = new Point(0, 0);
			}
		}

		public string PreviewData
		{
			set
			{
				// Set the preview content when all else has settled, this is really here so that the preview displays properly on the
				// initial dialog load. The GeckoWebBrowser is supposed to handle setting the content before it becomes visible, but it
				// doesn't work
				EventHandler refreshDelegate = null;
				refreshDelegate = delegate
				{
					// Since we are handling this delayed the dialog may have been closed before we get around to it
					if (m_preview.IsDisposed)
					{
						return;
					}
					var browser = (GeckoWebBrowser)m_preview.NativeBrowser;
					// Workaround to prevent the Gecko browser from stealing focus each time we set the PreviewData
					browser.WebBrowserFocus.Deactivate();
					// The second parameter is used only if the string data in the first parameter is unusable,
					// but it must be set to a valid Uri
					browser.LoadContent(value, "file:///c:/MayNotExist/doesnotmatter.html", "application/xhtml+xml");
					m_preview.Refresh();
					Application.Idle -= refreshDelegate;
				};
				Application.Idle += refreshDelegate;
			}
		}

		public void Redraw()
		{
			Invalidate(true);
		}

		public void SetChoices(IEnumerable<DictionaryConfigurationModel> choices)
		{
			m_cbDictConfig.Items.Clear();
			if (choices == null)
			{
				return;
			}
			foreach (var choice in choices)
			{
				m_cbDictConfig.Items.Add(choice);
			}
		}

		public void ShowPublicationsForConfiguration(string publications)
		{
			m_txtPubsForConfig.Text = publications;
		}

		public void SelectConfiguration(DictionaryConfigurationModel configuration)
		{
			m_cbDictConfig.SelectedItem = configuration;
			if (treeControl.Tree.Nodes.Count > 0)
			{
				treeControl.Tree.Nodes[0].Expand();
			}
		}

		/// <summary>
		/// Remember which elements are highlighted so that we can turn the highlighting off later.
		/// </summary>
		private List<GeckoElement> _highlightedElements;
		private const string HighlightStyle = "background-color:Yellow ";   // LightYellow isn't really bold enough marking to my eyes for this feature.

		public void HighlightContent(ConfigurableDictionaryNode configNode, IFwMetaDataCacheManaged metaDataCacheAccessor)
		{
			if (m_preview.IsDisposed)
			{
				return;
			}
			if (_highlightedElements != null)
			{
				foreach (var element in _highlightedElements)
				{
					// remove the background-color added earlier.  any other style setting is unchanged.
					var style = element.GetAttribute("style");
					style = style.Replace(HighlightStyle, "");
					element.SetAttribute("style", style);
				}
				_highlightedElements = null;
			}

			if (configNode == null)
			{
				return;
			}
			var browser = (GeckoWebBrowser)m_preview.NativeBrowser;
			// Surprisingly, xpath does not work for xml documents in geckofx, so we need to search manually for the node we want.
			_highlightedElements = FindConfiguredItem(configNode, browser, metaDataCacheAccessor);
			foreach (var element in _highlightedElements)
			{
				// add background-color to the style, preserving any existing style.  (See LT-17222.)
				var style = element.GetAttribute("style");
				if (string.IsNullOrEmpty(style))
				{
					style = HighlightStyle;
				}
				else
				{
					style = HighlightStyle + style; // note trailing space in string constant
				}
				element.SetAttribute("style", style);
			}
		}

		private static List<GeckoElement> FindConfiguredItem(ConfigurableDictionaryNode selectedConfigNode, GeckoWebBrowser browser, IFwMetaDataCacheManaged metaDataCacheAccessor)
		{
			var elements = new List<GeckoElement>();
			var body = browser.Document.Body;
			if (body == null || selectedConfigNode == null) // Sanity check
			{
				return elements;
			}

			var topLevelConfigNode = GetTopLevelNode(selectedConfigNode);
			var topLevelClass = CssGenerator.GetClassAttributeForConfig(topLevelConfigNode);
			foreach (var div in body.GetElementsByTagName("div"))
			{
				if (Equals(div.ParentElement, body) && div.GetAttribute("class") == topLevelClass)
				{
					elements.AddRange(FindMatchingSpans(selectedConfigNode, div, topLevelConfigNode, metaDataCacheAccessor));
				}
			}
			return elements;
		}

		/// <summary>
		/// Returns the top-level ancestor of the given node
		/// </summary>
		private static ConfigurableDictionaryNode GetTopLevelNode(ConfigurableDictionaryNode childNode)
		{
			while (childNode.Parent != null)
			{
				childNode = childNode.Parent;
			}
			return childNode;
		}

		private static bool DoesGeckoElementOriginateFromConfigNode(ConfigurableDictionaryNode configNode, GeckoElement element, ConfigurableDictionaryNode topLevelNode)
		{
			Guid dummyGuid;
			GeckoElement dummyElement;
			var classListForGeckoElement = DictionaryConfigurationServices.GetClassListFromGeckoElement(element, out dummyGuid, out dummyElement);
			classListForGeckoElement.RemoveAt(0); // don't need the top level class
			var nodeToMatch = DictionaryConfigurationController.FindConfigNode(topLevelNode, classListForGeckoElement);
			return Equals(nodeToMatch, configNode);
		}

		private static IEnumerable<GeckoElement> FindMatchingSpans(ConfigurableDictionaryNode selectedNode, GeckoElement parent, ConfigurableDictionaryNode topLevelNode, IFwMetaDataCacheManaged metaDataCacheAccessor)
		{
			var desiredClass = CssGenerator.GetClassAttributeForConfig(selectedNode);
			if (ConfiguredXHTMLGenerator.IsCollectionNode(selectedNode, metaDataCacheAccessor))
			{
				desiredClass = CssGenerator.GetClassAttributeForCollectionItem(selectedNode);
			}
			return parent.GetElementsByTagName("span").Where(span => span.GetAttribute("class") != null
			                                                         && span.GetAttribute("class").Split(' ')[0] == desiredClass
																	 && DoesGeckoElementOriginateFromConfigNode(selectedNode, span, topLevelNode)).ToList();
		}

		private void m_buttonManageConfigurations_Click(object sender, EventArgs e)
		{
			ManageConfigurations?.Invoke(sender, e);
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			SaveModel?.Invoke(sender, e);
			Close();
		}

		private void applyButton_Click(object sender, EventArgs e)
		{
			SaveModel?.Invoke(sender, e);
		}

		private void helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_propertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), m_helpTopic);
		}

		private void OnConfigurationChanged(object sender, EventArgs e)
		{
			SwitchConfiguration?.Invoke(sender, new SwitchConfigurationEventArgs
			{
				ConfigurationPicked = (DictionaryConfigurationModel)m_cbDictConfig.SelectedItem
			});
		}

		/// <summary>
		/// Save the location and size for next time.
		/// </summary>
		protected override void OnClosing(CancelEventArgs e)
		{
			if (m_propertyTable != null)
			{
				m_propertyTable.SetProperty("DictionaryConfigurationDlg_Location", Location, doBroadcastIfChanged: true);
				m_propertyTable.SetProperty("DictionaryConfigurationDlg_Size", Size, doBroadcastIfChanged: true);
			}
			base.OnClosing(e);
		}
	}
}