using System;
using System.Collections.Generic;
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
		}

		public void Initialize(IEnumerable<ObjectLabel> labels, IEnumerable<ICmObject> selectedItems)
		{
			m_semdomRepo = Cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			m_stylesheet = FontHeightAdjuster.StyleSheetFromMediator(Mediator);
			m_selectedItems.UnionWith(selectedItems);
			UpdateDomainTreeAndListLabels(labels);
			searchTextBox.WritingSystemFactory = Cache.LanguageWritingSystemFactoryAccessor;
			searchTextBox.AdjustForStyleSheet(m_stylesheet);
			m_SearchTimer = new SearchTimer(this, 500, SearchSemanticDomains, new List<Control> {domainTree, domainList});
			searchTextBox.TextChanged += m_SearchTimer.OnSearchTextChanged;
		}

		private void UpdateDomainTreeAndListLabels(IEnumerable<ObjectLabel> labels)
		{
			domainTree.BeginUpdate();
			SemanticDomainSelectionUtility.UpdateDomainTreeLabels(labels, displayUsageCheckBox.Checked, domainTree, m_stylesheet, m_selectedItems);
			foreach (var selectedItem in m_selectedItems)
			{
				selectedDomainsList.Items.Add(SemanticDomainSelectionUtility.CreateLabelListItem(selectedItem, true, false));
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
				if (m_searchIconSet)
				{
					// When disabled, we use a Background image rather than a regular image so that it
					// does not gray out. When showing the search icon, the button is never enabled,
					// so it is a shame to have it grey-out our pretty magnifying glass. The X however
					// can work as a normal button image (which avoids needing to make it larger
					// than the button etc. in order to avoid repeating it as wallpaper, which is how
					// BackgroundImage works.)
					btnCancelSearch.Image = FieldWorks.Resources.Images.X;
					btnCancelSearch.BackgroundImage = null;
					m_searchIconSet = false;
					btnCancelSearch.Enabled = true;
				}
				domainList.ItemChecked -= OnDomainListChecked;
				var semDomainsToShow = m_semdomRepo.FindDomainsThatMatch(searchString);
				SemanticDomainSelectionUtility.UpdateDomainListLabels(ObjectLabel.CreateObjectLabels(Cache, semDomainsToShow, string.Empty, DisplayWs), domainList, displayUsageCheckBox.Checked);
				domainTree.Visible = false;
				domainList.Visible = true;
				domainList.ItemChecked += OnDomainListChecked;
			}
			else
			{
				domainTree.Visible = true;
				domainList.Visible = false;
				if (!m_searchIconSet)
				{
					btnCancelSearch.BackgroundImage = FieldWorks.Resources.Images.Search;
					btnCancelSearch.Image = null;
					m_searchIconSet = true;
					btnCancelSearch.Enabled = false;
				}
			}
		}

		private string TrimmedSearchBoxText
		{
			get
			{
				return (searchTextBox.Tss.Text ?? string.Empty).Trim();
			}
		}

		void OnOk(object sender, EventArgs e)
		{
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
				SemanticDomainSelectionUtility.AdjustSelectedDomainList(domain, true, selectedDomainsList);
			}
			// Add all the partial matches to the list also, but do not check them by default
			foreach (var domainMatch in partialMatches)
			{
				SemanticDomainSelectionUtility.AdjustSelectedDomainList(domainMatch, false, selectedDomainsList);
			}
		}

		private void OnSelectedDomainItemChecked(object sender, ItemCheckedEventArgs e)
		{
			SemanticDomainSelectionUtility.AdjustTreeAndListView(e.Item.Tag, e.Item.Checked, domainTree, domainList);
		}

		private void OnDomainListChecked(object sender, ItemCheckedEventArgs e)
		{
			var domain = m_semdomRepo.GetObject((int)e.Item.Tag);
			SemanticDomainSelectionUtility.AdjustSelectedDomainList(domain, e.Item.Checked, selectedDomainsList);
		}

		private void OnDomainTreeCheck(object sender, TreeViewEventArgs e)
		{
			if(e.Action != TreeViewAction.Unknown)
			{
				var domain = (e.Node.Tag as ObjectLabel).Object;
				SemanticDomainSelectionUtility.AdjustSelectedDomainList(domain, e.Node.Checked, selectedDomainsList);
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
					item.Text = SemanticDomainSelectionUtility.CreateLabelListItem(item.Tag as ICmObject,
													item.Checked,
													displayUsageCheckBox.Checked).Text;
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
		/// <param name="createChecked"></param>
		/// <param name="displayUsage"></param>
		/// <returns></returns>
		public static ListViewItem CreateLabelListItem(ICmObject semDom, bool createChecked, bool displayUsage)
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
				// Don't count the reference from an overlay, since we have no way to tell
				// how many times that overlay has been used.  See FWR-1050.
				int count = 0;
				if (semanticDomainItem.ReferringObjects != null)
				{
					count = semanticDomainItem.ReferringObjects.Count;
					foreach (ICmObject x in semanticDomainItem.ReferringObjects)
					{
						if (x is ICmOverlay)
							--count;
					}
				}
				if (count > 0)
				{
					strbldr.AppendFormat(" ({0})", count);
				}
			}

			return new ListViewItem(strbldr.ToString()) { Checked = createChecked, Tag = semanticDomainItem.Hvo };
		}

		/// <summary>
		/// Find the item in the selectedDomainsList if it is there and
		/// set the checkmark accordingly, or add it and check it.
		/// </summary>
		/// <param name="domain"></param>
		/// <param name="check"></param>
		/// <param name="selectedDomainsList"></param>
		public static void AdjustSelectedDomainList(ICmObject domain, bool check, ListView selectedDomainsList)
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
				selectedDomainsList.Items.Add(CreateLabelListItem(domain, check, false));
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
		/// <param name="domainList"></param>
		/// <param name="displayUsage"></param>
		public static void UpdateDomainListLabels(IEnumerable<ObjectLabel> createObjectLabels, ListView domainList, bool displayUsage)
		{
			domainList.BeginUpdate();	// Mono is extremely bad about redundant redrawing.  See FWNX-973 and FWNX-1043.
			domainList.Items.Clear();
			foreach (var selectedItem in createObjectLabels)
			{
				domainList.Items.Add(CreateLabelListItem(selectedItem.Object, false, displayUsage));
			}
			domainList.EndUpdate();
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

			protected override LabelNode Create(ObjectLabel nol, IVwStylesheet stylesheet, bool displayUsage)
			{
				return new DomainNode(nol, stylesheet, displayUsage);
			}
		}
	}
}
