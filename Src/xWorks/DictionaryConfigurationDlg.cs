// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gecko;
using Gecko.DOM;
using SIL.FieldWorks.Common.FwUtils;
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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "element does NOT need to be disposed locally!")]
		public void HighlightContent(ConfigurableDictionaryNode configNode)
		{
			if (m_preview.IsDisposed)
				return;
			if (_highlightedElements != null)
			{
				foreach (var element in _highlightedElements)
					element.SetAttribute("style", "");
				_highlightedElements = null;
			}
			if (configNode != null)
			{
				var browser = (GeckoWebBrowser)m_preview.NativeBrowser;
				// Surprisingly, xpath does not work for xml documents in geckofx, so we need to search manually for the node we want.
				_highlightedElements = FindConfiguredItem(configNode, browser);
				foreach (var element in _highlightedElements)
					element.SetAttribute("style", "background-color:Yellow");	// LightYellow isn't really bold enough marking to my eyes for this feature.
			}

		}

		private List<GeckoElement> FindConfiguredItem(ConfigurableDictionaryNode configNode, GeckoWebBrowser browser)
		{
			var elements = new List<GeckoElement>();
			var classNames = new List<string>();
			for (var node = configNode; node != null; node = node.Parent)
				classNames.Insert(0, CssGenerator.GetClassAttributeForConfig(node));
			var body = browser.Document.DocumentElement.GetElementsByTagName("body").FirstOrDefault();
			if (body != null)
			{
				foreach (var div in body.GetElementsByTagName("div"))
				{
					if (div.GetAttribute("class") == classNames[0])
						elements.AddRange(FindMatchingSpan(div, classNames, 1));
				}
			}
			return elements;
		}

		private List<GeckoElement> FindMatchingSpan(GeckoElement parent, List<string> classNames, int idxClass)
		{
			var elements = new List<GeckoElement>();
			if (idxClass >= classNames.Count)
			{
				elements.Add(parent);
				return elements;
			}
			foreach (var span in parent.GetElementsByTagName("span"))
			{
				if (span.GetAttribute("class") == classNames[idxClass])
					elements.AddRange(FindMatchingSpan(span, classNames, idxClass + 1));
			}
			return elements;
		}

		private void m_linkManageConfigurations_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
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
