// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This class contains methods that can be used for displaying the Semantic Domains in a TreeView and ListView.
	/// These views are used in FLEX for allowing the user to select Semantic Domains.
	/// </summary>
	internal static class SemanticDomainSelectionServices
	{
		/// <summary>
		/// Creates a ListViewItem for the given ICmObject
		/// </summary>
		internal static ListViewItem CreateLabelListItem(ICmObject semDom, IVwStylesheet stylesheet, bool createChecked, bool displayUsage)
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
				{
					strbldr.AppendFormat(" ({0})", count);
				}
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
		internal static int SenseReferenceCount(ICmSemanticDomain domain)
		{
			var count = 0;
			if (domain.ReferringObjects != null)
			{
				count = domain.ReferringObjects.Count(item => item is ILexSense);
			}
			return count;
		}

		/// <summary>
		/// Find the item in the selectedDomainsList if it is there and
		/// set the checkmark accordingly, or add it and check it.
		/// </summary>
		internal static void AdjustSelectedDomainList(ICmObject domain, IVwStylesheet stylesheet, bool check, ListView selectedDomainsList)
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
		internal static void AdjustTreeAndListView(object tag, bool check, TreeView domainTree, ListView domainList)
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
		internal static void UpdateDomainListLabels(IEnumerable<ObjectLabel> createObjectLabels, IVwStylesheet stylesheet, ListView domainList, bool displayUsage)
		{
			domainList.BeginUpdate();   // Mono is extremely bad about redundant redrawing.  See FWNX-973 and FWNX-1043.
			domainList.Items.Clear();
			if (createObjectLabels.Any())
			{
				domainList.Font = GetFontForFormFromObjectLabels(createObjectLabels, stylesheet);
			}
			foreach (var selectedItem in createObjectLabels)
			{
				domainList.Items.Add(CreateLabelListItem(selectedItem.Object, stylesheet, false, displayUsage));
			}
			domainList.EndUpdate();
		}

		private static Font GetFontForFormFromObjectLabels(IEnumerable<ObjectLabel> labelList, IVwStylesheet stylesheet)
		{
			var cache = labelList.First().Object.Cache;
			return FontHeightAdjuster.GetFontForNormalStyle(cache.DefaultAnalWs, stylesheet, cache);
		}

		/// <summary>
		/// Populate the TreeView with the labels and check/uncheck according to the selectedItems and displayUsage
		/// parameters.
		/// </summary>
		internal static void UpdateDomainTreeLabels(IEnumerable<ObjectLabel> labels, bool displayUsage, TreeView domainTree, IVwStylesheet stylesheet, HashSet<ICmObject> selectedItems)
		{
			domainTree.BeginUpdate();   // Mono is extremely bad about redundant redrawing.  See FWNX-973 and FWNX-1043.
			domainTree.Nodes.Clear();
			if (labels.Any())
			{
				domainTree.Font = GetFontForFormFromObjectLabels(labels, stylesheet);
			}
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
		private sealed class DomainNode : LabelNode
		{
			public DomainNode(ObjectLabel label, IVwStylesheet stylesheet, bool displayUsage)
				: base(label, stylesheet, displayUsage)
			{
			}

			protected override string BasicNodeString => ((ICmSemanticDomain)Label.Object).AbbrAndName;

			protected override int CountUsages()
			{
				return SenseReferenceCount((ICmSemanticDomain)Label.Object);
			}

			protected override LabelNode Create(ObjectLabel nol, IVwStylesheet stylesheet, bool displayUsage)
			{
				return new DomainNode(nol, stylesheet, displayUsage);
			}
		}
	}
}