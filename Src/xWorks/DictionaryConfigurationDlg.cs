// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Gecko;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.XWorks.DictionaryDetailsView;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	public partial class DictionaryConfigurationDlg : Form, IDictionaryConfigurationView
	{
		/// <summary>
		/// When manage views is clicked tell the controller to launch the dialog where different
		/// dictionary configurations (or views) are managed.
		/// </summary>
		public event EventHandler ManageConfigurations;

		public event SwitchConfigurationEvent SwitchConfiguration;
		Mediator m_mediator;

		private string m_helpTopic;
		private readonly HelpProvider m_helpProvider;
		private IHelpTopicProvider m_helpTopicProvider;

		/// <summary>
		/// When OK or Apply are clicked tell anyone who is listening to do their save.
		/// </summary>
		public event EventHandler SaveModel;

		public DictionaryConfigurationDlg(Mediator mediator)
		{
			m_mediator = mediator;
			InitializeComponent();

			m_preview.Dock = DockStyle.Fill;
			m_preview.Location = new Point(0, 0);
			previewDetailSplit.Panel1.Controls.Add(m_preview);
			manageConfigs_treeDetailButton_split.IsSplitterFixed = true;
			treeDetail_Button_Split.IsSplitterFixed = true;
			this.MinimumSize = new Size(m_grpConfigurationManagement.Width + 3, manageConfigs_treeDetailButton_split.Height);

			m_helpTopicProvider = mediator.HelpTopicProvider;
			m_helpProvider = new HelpProvider { HelpNamespace = m_helpTopicProvider.HelpFile };
			m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(HelpTopic));
			m_helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			m_helpProvider.SetShowHelp(this, true);

			// Restore the location and size from last time we called this dialog.
			if (m_mediator != null && m_mediator.PropertyTable != null)
			{
				object locWnd = m_mediator.PropertyTable.GetValue("DictionaryConfigurationDlg_Location");
				object szWnd = m_mediator.PropertyTable.GetValue("DictionaryConfigurationDlg_Size");
				if (locWnd != null && szWnd != null)
				{
					Rectangle rect = new Rectangle((Point)locWnd, (Size)szWnd);
					ScreenUtils.EnsureVisibleRect(ref rect);
					DesktopBounds = rect;
					StartPosition = FormStartPosition.Manual;
				}
			}
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
					return;
				m_helpTopic = value;
				m_helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(HelpTopic));
			}
		}

		public DictionaryConfigurationTreeControl TreeControl
		{
			get { return treeControl; }
		}

		public IDictionaryDetailsView DetailsView
		{
			set
			{
				if(detailsView == null)
				{
					detailsView = (DetailsView)value;
					previewDetailSplit.Panel2.Controls.Add(detailsView);
					detailsView.Dock = DockStyle.Fill;
					detailsView.Location = new Point(0, 0);
				}
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
				refreshDelegate = delegate(object sender, EventArgs e)
				{
					// Since we are handling this delayed the dialog may have been closed before we get around to it
					if(!m_preview.IsDisposed)
					{
						var browser = (GeckoWebBrowser)m_preview.NativeBrowser;
						// Workaround to prevent the Gecko browser from stealing focus each time we set the PreviewData
						browser.WebBrowserFocus.Deactivate();
						// The second parameter is used only if the string data in the first parameter is unusable,
						// but it must be set to a valid Uri
						browser.LoadContent(value, "file:///c:/MayNotExist/doesnotmatter.html", "application/xhtml+xml");
						m_preview.Refresh();
						Application.Idle -= refreshDelegate;
					}
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
			if(choices != null)
			{
				foreach(var choice in choices)
				{
					m_cbDictConfig.Items.Add(choice);
				}
			}
		}

		public void ShowPublicationsForConfiguration(String publications)
		{
			m_txtPubsForConfig.Text = publications;
		}

		public void SelectConfiguration(DictionaryConfigurationModel configuration)
		{
			m_cbDictConfig.SelectedItem = configuration;
			if(treeControl.Tree.Nodes.Count > 0)
			{
				treeControl.Tree.Nodes[0].Expand();
			}
		}

		/// <summary>
		/// Remember which elements are highlighted so that we can turn the highlighting off later.
		/// </summary>
		private List<GeckoElement> _highlightedElements;
		private const string HighlightStyle = "background-color:Yellow ";	// LightYellow isn't really bold enough marking to my eyes for this feature.

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "element does NOT need to be disposed locally!")]
		public void HighlightContent(ConfigurableDictionaryNode configNode, FdoCache cache)
		{
			if (m_preview.IsDisposed)
				return;
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
				return;
			var browser = (GeckoWebBrowser)m_preview.NativeBrowser;
			// Surprisingly, xpath does not work for xml documents in geckofx, so we need to search manually for the node we want.
			_highlightedElements = FindConfiguredItem(configNode, browser, cache);
			foreach (var element in _highlightedElements)
			{
				// add background-color to the style, preserving any existing style.  (See LT-17222.)
				var style = element.GetAttribute("style");
				if (String.IsNullOrEmpty(style))
					style = HighlightStyle;
				else
					style = HighlightStyle + style;	// note trailing space in string constant
				element.SetAttribute("style", style);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "body is a reference")]
		private static List<GeckoElement> FindConfiguredItem(ConfigurableDictionaryNode selectedConfigNode, GeckoWebBrowser browser, FdoCache cache)
		{
			var elements = new List<GeckoElement>();
			var body = browser.Document.Body;
			if (body == null || selectedConfigNode == null) // Sanity check
				return elements;

			var topLevelConfigNode = GetTopLevelNode(selectedConfigNode);
			var topLevelClass = CssGenerator.GetClassAttributeForConfig(topLevelConfigNode);
			foreach (var div in body.GetElementsByTagName("div"))
			{
				if (Equals(div.ParentElement, body) && div.GetAttribute("class") == topLevelClass)
					elements.AddRange(FindMatchingSpans(selectedConfigNode, div, topLevelConfigNode, cache));
			}
			return elements;
		}

		/// <summary>
		/// Returns the top-level ancestor of the given node
		/// </summary>
		private static ConfigurableDictionaryNode GetTopLevelNode(ConfigurableDictionaryNode childNode)
		{
			while (childNode.Parent != null)
				childNode = childNode.Parent;
			return childNode;
		}

		private static bool DoesGeckoElementOriginateFromConfigNode(ConfigurableDictionaryNode configNode, GeckoElement element,
			ConfigurableDictionaryNode topLevelNode)
		{
			Guid dummyGuid;
			GeckoElement dummyElement;
			var classListForGeckoElement = XhtmlDocView.GetClassListFromGeckoElement(element, out dummyGuid, out dummyElement);
			classListForGeckoElement.RemoveAt(0); // don't need the top level class
			var nodeToMatch = DictionaryConfigurationController.FindStartingConfigNode(topLevelNode, classListForGeckoElement);
			return Equals(nodeToMatch, configNode);
		}

		private static IEnumerable<GeckoElement> FindMatchingSpans(ConfigurableDictionaryNode selectedNode, GeckoElement parent,
			ConfigurableDictionaryNode topLevelNode, FdoCache cache)
		{
			var elements = new List<GeckoElement>();
			var desiredClass = CssGenerator.GetClassAttributeForConfig(selectedNode);
			if (ConfiguredXHTMLGenerator.IsCollectionNode(selectedNode, cache))
				desiredClass = CssGenerator.GetClassAttributeForCollectionItem(selectedNode);
			foreach (var span in parent.GetElementsByTagName("span"))
			{
				if (span.GetAttribute("class") != null && span.GetAttribute("class").Split(' ')[0] == desiredClass &&
					DoesGeckoElementOriginateFromConfigNode(selectedNode, span, topLevelNode))
				{
					elements.Add(span);
				}
			}
			return elements;
		}

		private void m_buttonManageConfigurations_Click(object sender, EventArgs e)
		{
			if (ManageConfigurations != null)
				ManageConfigurations(sender, e);
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			if (SaveModel != null)
				SaveModel(sender, e);
			Close();
		}

		private void applyButton_Click(object sender, EventArgs e)
		{
			if (SaveModel != null)
				SaveModel(sender, e);
		}

		private void helpButton_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, m_helpTopic);
		}

		private void OnConfigurationChanged(object sender, EventArgs e)
		{
			if(SwitchConfiguration != null)
				SwitchConfiguration(sender, new SwitchConfigurationEventArgs
				{
					ConfigurationPicked = (DictionaryConfigurationModel)m_cbDictConfig.SelectedItem
				});
		}

		/// <summary>
		/// Save the location and size for next time.
		/// </summary>
		protected override void OnClosing(CancelEventArgs e)
		{
			if (m_mediator != null)
			{
				m_mediator.PropertyTable.SetProperty("DictionaryConfigurationDlg_Location", Location, false);
				m_mediator.PropertyTable.SetPropertyPersistence("DictionaryConfigurationDlg_Location", true);
				m_mediator.PropertyTable.SetProperty("DictionaryConfigurationDlg_Size", Size, false);
				m_mediator.PropertyTable.SetPropertyPersistence("DictionaryConfigurationDlg_Size", true);
			}
			base.OnClosing(e);
		}
	}
}
