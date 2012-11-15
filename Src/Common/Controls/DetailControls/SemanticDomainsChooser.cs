using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public partial class SemanticDomainsChooser : Form
	{
		private IVwStylesheet _stylesheet;
		private HashSet<ICmObject> _selectedItems = new HashSet<ICmObject>();
		private readonly Timer myTimer = new Timer();
		private bool myTimerTickSet;

		public IEnumerable<ICmObject> SemanticDomains
		{
			get { return _selectedItems; }
		}

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
			_selectedItems.UnionWith(selectedItems);
			UpdateDomainTreeAndListLabels(labels);
			searchTextBox.WritingSystemFactory = Cache.LanguageWritingSystemFactoryAccessor;
			searchTextBox.AdjustForStyleSheet(FontHeightAdjuster.StyleSheetFromMediator(Mediator));
		}

		private void UpdateDomainTreeAndListLabels(IEnumerable<ObjectLabel> labels)
		{
			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromMediator(Mediator);
			domainTree.BeginUpdate();
			domainTree.Nodes.Clear();
			foreach (var label in labels)
			{
				var x = CreateLabelNode(label, _stylesheet, _selectedItems, displayUsageCheckBox.Checked);
				domainTree.Nodes.Add(x);
			}
			foreach (var selectedItem in _selectedItems)
			{
				selectedDomainsList.Items.Add(CreateLabelListItem(selectedItem, true, false));
			}
			domainTree.EndUpdate();
		}

		/// <summary>
		/// Creates a ListViewItem for the given ICmObject
		/// </summary>
		/// <param name="label">A Semantic Domain</param>
		/// <param name="createChecked"></param>
		/// <param name="displayUsage"></param>
		/// <returns></returns>
		private static ListViewItem CreateLabelListItem(ICmObject label, bool createChecked, bool displayUsage)
		{
			var semanticDomainItem = label as ICmSemanticDomain;
			if(semanticDomainItem == null)
			{
				return new ListViewItem(DetailControlsStrings.ksSemanticDomainInvalid);
			}
			var text = semanticDomainItem.AbbrAndName;
			if (displayUsage)
			{
				// Don't count the reference from an overlay, since we have no way to tell
				// how many times that overlay has been used.  See FWR-1050.
				int count = 0;
				if (label != null && label.ReferringObjects != null)
				{
					count = label.ReferringObjects.Count;
					foreach (ICmObject x in label.ReferringObjects)
					{
						if (x is ICmOverlay)
							--count;
					}
				}
				if (count > 0)
					text += " (" + count + ")";
			}

			var item = new ListViewItem(text) {Checked = createChecked, Tag = label};
			return item;
		}

		/// <summary>
		/// Creates the label node.
		/// </summary>
		/// <param name="label">The label.</param>
		/// <param name="stylesheet"></param>
		/// <param name="selectedItems"></param>
		/// <param name="displayUsage">if set to <c>true</c> [display usage].</param>
		/// <returns></returns>
		static DomainNode CreateLabelNode(ObjectLabel label, IVwStylesheet stylesheet, IEnumerable<ICmObject> selectedItems, bool displayUsage)
		{
			var node = new DomainNode(label, stylesheet, displayUsage);
			node.AddChildren(true, selectedItems);
			if(selectedItems.Contains(label.Object))
			{
				node.Checked = true;
			}
			return node;
		}

		private void AdjustTreeAndListView(object tag, bool check)
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
				if(listViewItem.Tag == tag)
				{
					listViewItem.Checked = check;
					break;
				}
			}
		}

		private bool RecursivelyAdjustTreeNode(DomainNode node, object tag, bool check)
		{
			if ((node.Tag as ObjectLabel).Object == tag)
			{
				node.Checked = check;
				return true;
			}
			return node.Nodes.Cast<DomainNode>().Any(child => RecursivelyAdjustTreeNode(child, tag, check));
		}

		private void TimerEventProcessor(object sender, EventArgs eventArgs)
		{
			var oldCursor = Cursor;
			Cursor = Cursors.WaitCursor;
			myTimer.Tick -= TimerEventProcessor;
			myTimerTickSet = false;
			domainTree.Enabled = domainList.Enabled = false;
			SearchSemDomSelection();
			domainTree.Enabled = domainList.Enabled = true;
			Cursor = oldCursor;
		}

		private void SearchSemDomSelection()
		{
			IEnumerable<ObjectLabel> labels = new List<ObjectLabel>();

			//Gordon will write a method that will return IEnumerable<ICmSemanticDomain>
			//based on the search string we give it.
			var searchString = searchTextBox.Tss;
			if (!string.IsNullOrEmpty(searchString.Text))
			{
				domainList.ItemChecked -= OnDomainListChecked;
				var semDomRepo = Cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
				var semDomainsToShow = semDomRepo.FindDomainsThatMatch(searchString.Text);
				UpdateDomainListLabels(ObjectLabel.CreateObjectLabels(Cache, semDomainsToShow, "", DisplayWs));
				domainTree.Visible = false;
				domainList.Visible = true;
				domainList.ItemChecked += OnDomainListChecked;
			}
			else
			{
				domainTree.Visible = true;
				domainList.Visible = false;
			}
		}

		private void UpdateDomainListLabels(IEnumerable<ObjectLabel> createObjectLabels)
		{
			domainList.Items.Clear();
			foreach (var selectedItem in createObjectLabels)
			{
				domainList.Items.Add(CreateLabelListItem(selectedItem.Object, false, displayUsageCheckBox.Checked));
			}
		}

		/// <summary>
		/// When the user types in this text box seach through the Semantic Domains to find matches
		/// and display them in the TreeView.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void OnSearchTextChanged(object sender, EventArgs e)
		{
			if (myTimerTickSet == false)
			{
				// Sets the timer interval to 1/2 seconds.
				myTimer.Interval = 500;
				myTimer.Start();
				myTimer.Enabled = true;
				myTimerTickSet = true;
				myTimer.Tick += new EventHandler(TimerEventProcessor);
			}
			else
			{
				//myTimer.Tick += new EventHandler(TimerEventProcessor);
				myTimer.Stop();
				myTimer.Enabled = true;
			}
		}

		void OnOk(object sender, System.EventArgs e)
		{
			_selectedItems.Clear();
			foreach(ListViewItem selectedDomain in selectedDomainsList.Items)
			{
				var item = selectedDomain.Tag as ICmObject;
				if(selectedDomain.Checked && item != null)
				{
					_selectedItems.Add(item);
				}
			}
		}

		private void OnSuggestClicked(object sender, EventArgs e)
		{

			var semDomRepo = Cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			IEnumerable<ICmSemanticDomain> partialMatches;
			var semDomainsToShow = semDomRepo.FindDomainsThatMatchWordsIn(Sense, out partialMatches);
			foreach (var domain in semDomainsToShow)
			{
				AdjustSelectedDomainList(domain, true);
			}
			// Add all the partial matches to the list also, but do not check them by default
			foreach (var domainMatch in partialMatches)
			{
				AdjustSelectedDomainList(domainMatch, false);
			}
		}

		private void OnSelectedDomainItemChecked(object sender, ItemCheckedEventArgs e)
		{
			AdjustTreeAndListView(e.Item.Tag, e.Item.Checked);
		}


		private void OnDomainListChecked(object sender, ItemCheckedEventArgs e)
		{
			AdjustSelectedDomainList(e.Item.Tag as ICmObject, e.Item.Checked);
		}

		private void OnDomainTreeCheck(object sender, TreeViewEventArgs e)
		{
			if(e.Action != TreeViewAction.Unknown)
			{
				var domain = (e.Node.Tag as ObjectLabel).Object;
				AdjustSelectedDomainList(domain, e.Node.Checked);
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
					item.Text = CreateLabelListItem(item.Tag as ICmObject,
													item.Checked,
													displayUsageCheckBox.Checked).Text;
				}
				domainTree.EndUpdate();
				domainList.EndUpdate();
			}
		}

		/// <summary>
		/// Find the item in the selectedDomainsList if it is there and
		/// set the checkmark accordingly, or add it and check it.
		/// </summary>
		/// <param name="domain"></param>
		/// <param name="check"></param>
		private void AdjustSelectedDomainList(ICmObject domain, bool check)
		{
			ListViewItem checkedItem = null;
			foreach (ListViewItem item in selectedDomainsList.Items)
			{
				if (item.Tag == domain)
				{
					checkedItem = item;
					item.Checked = check;
					break;
				}
			}
			if(checkedItem == null)
			{
				selectedDomainsList.Items.Add(CreateLabelListItem(domain, check, false));
			}
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

		private void OnEditDomainsLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			var toolName = XmlUtils.GetAttributeValue(LinkNode, "tool");
			Mediator.PostMessage("FollowLink", new FwUtils.FwLinkArgs(toolName, new Guid()));
			btnCancel.PerformClick();
		}

		public System.Xml.XmlNode LinkNode { get; set; }
	}
}
