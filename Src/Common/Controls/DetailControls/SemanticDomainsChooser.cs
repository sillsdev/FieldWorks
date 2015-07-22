using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;
using System.Diagnostics.CodeAnalysis;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public partial class SemanticDomainsChooser : Form
	{
		private IVwStylesheet m_stylesheet;
		private HashSet<ICmObject> m_selectedItems = new HashSet<ICmObject>();
		private bool m_searchIconSet = true;
		private SearchTimer m_SearchTimer;
		private ICmSemanticDomainRepository m_semdomRepo;
		private String m_helpTopic = "khtpSemanticDomainsChooser";

		public IEnumerable<ICmObject> SemanticDomains
		{
			get { return m_selectedItems; }}

		public IHelpTopicProvider HelpTopicProvider { private get; set; }

		public ILexSense Sense { private get; set; }

		public Mediator Mediator { private get; set; }

		public FdoCache Cache { private get; set; }

		public string DisplayWs { get; set; }

		public SemanticDomainsChooser()
		{
			InitializeComponent();
			btnCancelSearch.Init();
			editDomainsLinkPic.Image = DetailControlsStrings.gotoLinkPic;
		}

		public void Initialize(IEnumerable<ObjectLabel> labels, IEnumerable<ICmObject> selectedItems, PropertyTable propertyTable)
		{
			m_semdomRepo = Cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			m_stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(propertyTable);
			selectedDomainsList.Font = FontHeightAdjuster.GetFontForNormalStyle(
				Cache.DefaultAnalWs, m_stylesheet, Cache);
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
			SemanticDomainSelectionUtility.UpdateDomainTreeLabels(labels, displayUsageCheckBox.Checked, domainTree, m_stylesheet, m_selectedItems);
			foreach (var selectedItem in m_selectedItems)
			{
				selectedDomainsList.Items.Add(SemanticDomainSelectionUtility.CreateLabelListItem(selectedItem, m_stylesheet, true, false));
			}
			domainTree.EndUpdate();
		}

		private void SearchSemanticDomains()
		{
			IEnumerable<ObjectLabel> labels = new List<ObjectLabel>();

			// The FindDomainsThatMatch method returns IEnumerable<ICmSemanticDomain>
			// based on the search string we give it.
			var searchString = TrimmedSearchBoxText;
			if (!string.IsNullOrEmpty(searchString))
			{
				btnCancelSearch.SearchIsActive = true;
				domainList.ItemChecked -= OnDomainListChecked;
				var semDomainsToShow = m_semdomRepo.FindDomainsThatMatch(searchString);
				SemanticDomainSelectionUtility.UpdateDomainListLabels(
					ObjectLabel.CreateObjectLabels(Cache, semDomainsToShow, string.Empty, DisplayWs),
					m_stylesheet, domainList, displayUsageCheckBox.Checked);
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

		private string TrimmedSearchBoxText
		{
			get
			{
				return (searchTextBox.Tss.Text ?? string.Empty).Trim();
			}
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			if (SearchInProgress)
				e.Cancel = true; // Currently searching... don't respond to <Enter> or OK click.
			base.OnClosing(e);
		}

		void OnOk(object sender, EventArgs e)
		{
			if (SearchInProgress)
				return; // Currently searching... don't respond to <Enter> or OK click.
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
				SemanticDomainSelectionUtility.AdjustSelectedDomainList(domain, m_stylesheet, true, selectedDomainsList);
			}
			// Add all the partial matches to the list also, but do not check them by default
			foreach (var domainMatch in partialMatches)
			{
				SemanticDomainSelectionUtility.AdjustSelectedDomainList(domainMatch, m_stylesheet, false, selectedDomainsList);
			}
		}

		private void OnSelectedDomainItemChecked(object sender, ItemCheckedEventArgs e)
		{
			SemanticDomainSelectionUtility.AdjustTreeAndListView(e.Item.Tag, e.Item.Checked, domainTree, domainList);
		}

		private void OnDomainListChecked(object sender, ItemCheckedEventArgs e)
		{
			var domain = m_semdomRepo.GetObject((int)e.Item.Tag);
			SemanticDomainSelectionUtility.AdjustSelectedDomainList(domain, m_stylesheet, e.Item.Checked, selectedDomainsList);
		}

		private void OnDomainTreeCheck(object sender, TreeViewEventArgs e)
		{
			if(e.Action != TreeViewAction.Unknown)
			{
				var domain = (e.Node.Tag as ObjectLabel).Object;
				SemanticDomainSelectionUtility.AdjustSelectedDomainList(domain, m_stylesheet, e.Node.Checked, selectedDomainsList);
			}
		}

		private void OnDisplayUsageCheckedChanged(object sender, EventArgs e)
		{
			using (new WaitCursor(this))
			{
				domainTree.BeginUpdate();
				domainList.BeginUpdate();
				var stack = new Stack<LabelNode>(domainTree.Nodes.Cast<LabelNode>());
				while (stack.Count > 0)
				{
					LabelNode node = stack.Pop();
					node.DisplayUsage = displayUsageCheckBox.Checked;
					foreach (TreeNode childNode in node.Nodes)
					{
						var labelNode = childNode as LabelNode;
						if (labelNode != null)
							stack.Push(labelNode);
					}
				}
				foreach (ListViewItem item in domainList.Items)
				{
					var domain = m_semdomRepo.GetObject((int)item.Tag);
					item.Text = SemanticDomainSelectionUtility.CreateLabelListItem(domain, m_stylesheet,
						item.Checked, displayUsageCheckBox.Checked).Text;
				}
				domainTree.EndUpdate();
				domainList.EndUpdate();
			}
		}

		private void OnEditDomainsLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			var toolName = XmlUtils.GetAttributeValue(LinkNode, "tool");
			Mediator.PostMessage("FollowLink", new FwUtils.FwLinkArgs(toolName, new Guid()));
			btnCancel.PerformClick();
		}

		public System.Xml.XmlNode LinkNode { get; set; }

		private void btnCancelSearch_Click(object sender, EventArgs e)
		{
			searchTextBox.Text = "";
		}

		private void buttonHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(HelpTopicProvider, "UserHelpFile", m_helpTopic);
		}
	}

	/// <summary>
	/// This class contains methods that can be used for displaying the Semantic Domains in a TreeView and ListView.
	/// These views are used in FLEX for allowing the user to select Semantic Domains.
	/// </summary>
	public static class SemanticDomainSelectionUtility
	{
		/// <summary>
		/// Creates a ListViewItem for the given ICmObject
		/// </summary>
		/// <param name="semDom">A Semantic Domain</param>
		/// <param name="stylesheet"></param>
		/// <param name="createChecked"></param>
		/// <param name="displayUsage"></param>
		/// <returns></returns>
		public static ListViewItem CreateLabelListItem(ICmObject semDom, IVwStylesheet stylesheet, bool createChecked, bool displayUsage)
		{
			var semanticDomainItem = semDom as ICmSemanticDomain;
			if (semanticDomainItem == null)
			{
				return new ListViewItem(DetailControlsStrings.ksSemanticDomainInvalid);
			}
			var strbldr = new StringBuilder(semanticDomainItem.AbbrAndName);
			if (semanticDomainItem.OwningPossibility != null)
			{
				var parentName = semanticDomainItem.OwningPossibility.Name.BestAnalysisAlternative.Text;
				strbldr.AppendFormat(" [{0}]", parentName);
			}
			if (displayUsage)
			{
				var count = SenseReferenceCount(semanticDomainItem);
				if (count > 0)
					strbldr.AppendFormat(" ({0})", count);
			}

			var item = new ListViewItem(strbldr.ToString()) { Checked = createChecked, Tag = semanticDomainItem.Hvo };
			var cache = semDom.Cache;
			item.Font = FontHeightAdjuster.GetFontForNormalStyle(cache.DefaultAnalWs, stylesheet, cache);
			return item;
		}

		/// <summary>
		/// Don't count references to Semantic Domains from other Semantic Domains.
		/// The user only cares about how many times in the lexicon the Semantic Domain is used.
		/// </summary>
		/// <param name="domain"></param>
		/// <returns></returns>
		public static int SenseReferenceCount(ICmSemanticDomain domain)
		{
			int count = 0;
			if (domain.ReferringObjects != null)
				count = (from item in domain.ReferringObjects
						 where item is ILexSense
						 select item).Count();
			return count;
		}

		/// <summary>
		/// Find the item in the selectedDomainsList if it is there and
		/// set the checkmark accordingly, or add it and check it.
		/// </summary>
		/// <param name="domain"></param>
		/// <param name="stylesheet"></param>
		/// <param name="check"></param>
		/// <param name="selectedDomainsList"></param>
		public static void AdjustSelectedDomainList(ICmObject domain, IVwStylesheet stylesheet, bool check, ListView selectedDomainsList)
		{
			ListViewItem checkedItem = null;
			foreach (ListViewItem item in selectedDomainsList.Items)
			{
				if ((int)item.Tag == domain.Hvo)
				{
					checkedItem = item;
					item.Checked = check;
					break;
				}
			}
			if (checkedItem == null)
			{
				selectedDomainsList.Items.Add(CreateLabelListItem(domain, stylesheet, check, false));
			}
		}

		/// <summary>
		/// Creates the label node.
		/// </summary>
		/// <param name="label">The label.</param>
		/// <param name="stylesheet"></param>
		/// <param name="selectedItems"></param>
		/// <param name="displayUsage">if set to <c>true</c> [display usage].</param>
		/// <returns></returns>
		private static DomainNode CreateLabelNode(ObjectLabel label, IVwStylesheet stylesheet, IEnumerable<ICmObject> selectedItems, bool displayUsage)
		{
			var node = new DomainNode(label, stylesheet, displayUsage);
			node.AddChildren(true, selectedItems);
			if (selectedItems.Contains(label.Object))
			{
				node.Checked = true;
			}
			return node;
		}

		/// <summary>
		/// Adjust the checkbox for the nodes in the TreeView and ListView according to the 'tag' and 'check'
		/// parameter values.
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="check"></param>
		/// <param name="domainTree"></param>
		/// <param name="domainList"></param>
		public static void AdjustTreeAndListView(object tag, bool check, TreeView domainTree, ListView domainList)
		{
			foreach (DomainNode node in domainTree.Nodes)
			{
				if (RecursivelyAdjustTreeNode(node, tag, check))
				{
					break;
				}
			}
			foreach (ListViewItem listViewItem in domainList.Items)
			{
				if (listViewItem.Tag.Equals(tag))
				{
					listViewItem.Checked = check;
					break;
				}
			}
		}

		/// <summary>
		/// Check/Uncheck the TreeView node and recursively do this to any children nodes if the node
		/// matches the 'tag' object.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="tag"></param>
		/// <param name="check"></param>
		/// <returns></returns>
		private static bool RecursivelyAdjustTreeNode(DomainNode node, object tag, bool check)
		{
			if ((node.Tag as ObjectLabel).Object == tag)
			{
				node.Checked = check;
				return true;
			}
			return node.Nodes.Cast<DomainNode>().Any(child => RecursivelyAdjustTreeNode(child, tag, check));
		}

		/// <summary>
		/// Clear the ListView and add createObjectLabels to it. 'displayUsage' determines if the items are checked/unchecked.
		/// </summary>
		/// <param name="createObjectLabels"></param>
		/// <param name="stylesheet"></param>
		/// <param name="domainList"></param>
		/// <param name="displayUsage"></param>
		public static void UpdateDomainListLabels(IEnumerable<ObjectLabel> createObjectLabels, IVwStylesheet stylesheet, ListView domainList, bool displayUsage)
		{
			domainList.BeginUpdate();	// Mono is extremely bad about redundant redrawing.  See FWNX-973 and FWNX-1043.
			domainList.Items.Clear();
			if (createObjectLabels.Any())
				domainList.Font = GetFontForFormFromObjectLabels(createObjectLabels, stylesheet);

			foreach (var selectedItem in createObjectLabels)
			{
				domainList.Items.Add(CreateLabelListItem(selectedItem.Object, stylesheet, false, displayUsage));
			}
			domainList.EndUpdate();
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "cache is a reference")]
		private static Font GetFontForFormFromObjectLabels(IEnumerable<ObjectLabel> labelList, IVwStylesheet stylesheet)
		{
			var cache = labelList.First().Object.Cache;
			return FontHeightAdjuster.GetFontForNormalStyle(cache.DefaultAnalWs, stylesheet, cache);
		}

		/// <summary>
		/// Populate the TreeView with the labels and check/uncheck according to the selectedItems and displayUsage
		/// parameters.
		/// </summary>
		/// <param name="labels"></param>
		/// <param name="displayUsage"></param>
		/// <param name="domainTree"></param>
		/// <param name="stylesheet"></param>
		/// <param name="selectedItems"></param>
		public static void UpdateDomainTreeLabels(IEnumerable<ObjectLabel> labels, bool displayUsage, TreeView domainTree,
			IVwStylesheet stylesheet, HashSet<ICmObject> selectedItems)
		{
			domainTree.BeginUpdate();	// Mono is extremely bad about redundant redrawing.  See FWNX-973 and FWNX-1043.
			domainTree.Nodes.Clear();
			if (labels.Any())
				domainTree.Font = GetFontForFormFromObjectLabels(labels, stylesheet);

			foreach (var label in labels)
			{
				var x = CreateLabelNode(label, stylesheet, selectedItems, displayUsage);
				domainTree.Nodes.Add(x);
			}
			domainTree.EndUpdate();
		}

		/// <summary>
		/// This class extends the LabelNode class to provide a customized display for the semantic
		/// domain.
		/// </summary>
		private class DomainNode : LabelNode
		{
			public DomainNode(ObjectLabel label, IVwStylesheet stylesheet, bool displayUsage)
				: base(label, stylesheet, displayUsage)
			{
			}

			protected override string BasicNodeString { get { return (Label.Object as ICmSemanticDomain).AbbrAndName; } }

			protected override int CountUsages()
			{
				return SenseReferenceCount(Label.Object as ICmSemanticDomain);
			}

			protected override LabelNode Create(ObjectLabel nol, IVwStylesheet stylesheet, bool displayUsage)
			{
				return new DomainNode(nol, stylesheet, displayUsage);
			}
		}
	}
}
