// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	public partial class SemanticDomainsChooser : Form
	{
		private IVwStylesheet m_stylesheet;
		private HashSet<ICmObject> m_selectedItems = new HashSet<ICmObject>();
		private bool m_searchIconSet = true;
		private SearchTimer m_SearchTimer;
		private ICmSemanticDomainRepository m_semdomRepo;
		private string m_helpTopic = "khtpSemanticDomainsChooser";
		private IPublisher m_publisher;

		public IEnumerable<ICmObject> SemanticDomains => m_selectedItems;

		public IHelpTopicProvider HelpTopicProvider { private get; set; }

		public ILexSense Sense { private get; set; }

		public LcmCache Cache { private get; set; }

		public string DisplayWs { get; set; }

		public SemanticDomainsChooser()
		{
			InitializeComponent();
			btnCancelSearch.Init();
			editDomainsLinkPic.Image = DetailControlsStrings.gotoLinkPic;
		}

		public void Initialize(IEnumerable<ObjectLabel> labels, IEnumerable<ICmObject> selectedItems, IPropertyTable propertyTable, IPublisher publisher)
		{
			m_publisher = publisher;
			m_semdomRepo = Cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			m_stylesheet = FwUtils.StyleSheetFromPropertyTable(propertyTable);
			selectedDomainsList.Font = FontHeightAdjuster.GetFontForNormalStyle(Cache.DefaultAnalWs, m_stylesheet, Cache);
			m_selectedItems.UnionWith(selectedItems);
			UpdateDomainTreeAndListLabels(labels);
			searchTextBox.WritingSystemFactory = Cache.LanguageWritingSystemFactoryAccessor;
			searchTextBox.AdjustForStyleSheet(m_stylesheet);
			m_SearchTimer = new SearchTimer(this, 500, SearchSemanticDomains, new List<Control> {domainTree, domainList});
			searchTextBox.TextChanged += OnSearchTextChanged;
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			// Make sure cursor is in the search box
			searchTextBox.Select();
		}

		public bool SearchInProgress { get; set; }

		public void OnSearchTextChanged(object sender, EventArgs e)
		{
			SearchInProgress = true;
			m_SearchTimer.OnSearchTextChanged(sender, e);
		}

		private void UpdateDomainTreeAndListLabels(IEnumerable<ObjectLabel> labels)
		{
			domainTree.BeginUpdate();
			SemanticDomainSelectionServices.UpdateDomainTreeLabels(labels, displayUsageCheckBox.Checked, domainTree, m_stylesheet, m_selectedItems);
			foreach (var selectedItem in m_selectedItems)
			{
				selectedDomainsList.Items.Add(SemanticDomainSelectionServices.CreateLabelListItem(selectedItem, m_stylesheet, true, false));
			}
			domainTree.EndUpdate();
		}

		private void SearchSemanticDomains()
		{
			// The FindDomainsThatMatch method returns IEnumerable<ICmSemanticDomain>
			// based on the search string we give it.
			var searchString = TrimmedSearchBoxText;
			if (!string.IsNullOrEmpty(searchString))
			{
				btnCancelSearch.SearchIsActive = true;
				domainList.ItemChecked -= OnDomainListChecked;
				var semDomainsToShow = m_semdomRepo.FindDomainsThatMatch(searchString);
				SemanticDomainSelectionServices.UpdateDomainListLabels(ObjectLabel.CreateObjectLabels(Cache, semDomainsToShow, string.Empty, DisplayWs), m_stylesheet, domainList, displayUsageCheckBox.Checked);
				domainTree.Visible = false;
				domainList.Visible = true;
				domainList.ItemChecked += OnDomainListChecked;
			}
			else
			{
				domainTree.Visible = true;
				domainList.Visible = false;
				btnCancelSearch.SearchIsActive = false;
			}
			SearchInProgress = false;
		}

		private string TrimmedSearchBoxText => (searchTextBox.Tss.Text ?? string.Empty).Trim();

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			if (SearchInProgress)
			{
				e.Cancel = true; // Currently searching... don't respond to <Enter> or OK click.
			}
			base.OnClosing(e);
		}

		private void OnOk(object sender, EventArgs e)
		{
			if (SearchInProgress)
			{
				return; // Currently searching... don't respond to <Enter> or OK click.
			}
			m_selectedItems.Clear();
			foreach(ListViewItem selectedDomain in selectedDomainsList.Items)
			{
				var hvo = (int)selectedDomain.Tag;
				if (selectedDomain.Checked && hvo > 0)
				{
					m_selectedItems.Add(m_semdomRepo.GetObject(hvo));
				}
			}
		}

		private void OnSuggestClicked(object sender, EventArgs e)
		{
			IEnumerable<ICmSemanticDomain> partialMatches;
			var semDomainsToShow = m_semdomRepo.FindDomainsThatMatchWordsIn(Sense, out partialMatches);
			foreach (var domain in semDomainsToShow)
			{
				SemanticDomainSelectionServices.AdjustSelectedDomainList(domain, m_stylesheet, true, selectedDomainsList);
			}
			// Add all the partial matches to the list also, but do not check them by default
			foreach (var domainMatch in partialMatches)
			{
				SemanticDomainSelectionServices.AdjustSelectedDomainList(domainMatch, m_stylesheet, false, selectedDomainsList);
			}
		}

		private void OnSelectedDomainItemChecked(object sender, ItemCheckedEventArgs e)
		{
			SemanticDomainSelectionServices.AdjustTreeAndListView(e.Item.Tag, e.Item.Checked, domainTree, domainList);
		}

		private void OnDomainListChecked(object sender, ItemCheckedEventArgs e)
		{
			SemanticDomainSelectionServices.AdjustSelectedDomainList(m_semdomRepo.GetObject((int)e.Item.Tag), m_stylesheet, e.Item.Checked, selectedDomainsList);
		}

		private void OnDomainTreeCheck(object sender, TreeViewEventArgs e)
		{
			if (e.Action == TreeViewAction.Unknown)
			{
				return;
			}
			SemanticDomainSelectionServices.AdjustSelectedDomainList(((ObjectLabel)e.Node.Tag).Object, m_stylesheet, e.Node.Checked, selectedDomainsList);
		}

		private void OnDisplayUsageCheckedChanged(object sender, EventArgs e)
		{
			using (new WaitCursor(this))
			{
				domainTree.BeginUpdate();
				domainList.BeginUpdate();
				var stack = new Stack<LabelNode>(domainTree.Nodes.Cast<LabelNode>());
				while (stack.Any())
				{
					var node = stack.Pop();
					node.DisplayUsage = displayUsageCheckBox.Checked;
					foreach (TreeNode childNode in node.Nodes)
					{
						var labelNode = childNode as LabelNode;
						if (labelNode != null)
						{
							stack.Push(labelNode);
						}
					}
				}
				foreach (ListViewItem item in domainList.Items)
				{
					var domain = m_semdomRepo.GetObject((int)item.Tag);
					item.Text = SemanticDomainSelectionServices.CreateLabelListItem(domain, m_stylesheet, item.Checked, displayUsageCheckBox.Checked).Text;
				}
				domainTree.EndUpdate();
				domainList.EndUpdate();
			}
		}

		private void OnEditDomainsLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			var toolName = XmlUtils.GetOptionalAttributeValue(LinkNode, "tool");
			var commands = new List<string>
			{
				"AboutToFollowLink",
				"FollowLink"
			};
			var parms = new List<object>
			{
				null,
				new FwLinkArgs(toolName, new Guid())
			};
			m_publisher.Publish(commands, parms);
			btnCancel.PerformClick();
		}

		public XElement LinkNode { get; set; }

		private void btnCancelSearch_Click(object sender, EventArgs e)
		{
			searchTextBox.Text = string.Empty;
		}

		private void buttonHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(HelpTopicProvider, "UserHelpFile", m_helpTopic);
		}
	}
}