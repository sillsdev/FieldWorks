using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ChooserTreeView : TriStateTreeView
	{
		/// <summary></summary>
		protected List<int> m_initiallySelectedHvos;
		/// <summary></summary>
		protected List<TreeNode> m_initiallyCheckedNodes = new List<TreeNode>();
		/// <summary></summary>
		private Label m_lblSelectedCategories = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides <see cref="M:System.Windows.Forms.Control.OnHandleCreated(System.EventArgs)"/>.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			// Check all the nodes that should be checked and in the process,
			// make sure their parent nodes are expanded.
			foreach (TreeNode node in m_initiallyCheckedNodes)
			{
				SetChecked(node, CheckState.Checked);
				node.EnsureVisible();
			}

			// Now make sure the first node is scrolled into view, if it's not already.
			if (m_initiallyCheckedNodes.Count > 0)
				Nodes[0].EnsureVisible();

			m_initiallyCheckedNodes.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the tree with the specified list of possibilities.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void Load(ICmPossibilityList list, List<int> initiallySelectedHvos)
		{
			Load(list, initiallySelectedHvos, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the tree with the specified list of possibilities.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void Load(ICmPossibilityList list, List<int> initiallySelectedHvos,
			Label lblSelectedCategories)
		{
			m_lblSelectedCategories = lblSelectedCategories;

			Nodes.Clear();
			m_initiallySelectedHvos = (initiallySelectedHvos ?? new List<int>());

			if (list != null)
			{
				foreach (ICmPossibility possibility in list.PossibilitiesOS)
					Nodes.Add(CreateNode(possibility));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of the checked possibility labels.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual List<string> SelectedItems
		{
			get
			{
				List<string> selectedItems = new List<string>();
				foreach (TreeNode node in Nodes)
					GetSelectedItems(node, selectedItems);

				return selectedItems;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selected possibilities.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual IEnumerable<ICmPossibility> SelectedPossibilities
		{
			get
			{
				foreach (TreeNode node in Nodes)
					foreach (ICmPossibility poss in GetSelectedPossibilites(node))
						yield return poss;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selected possibilites for the specified node and its children.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IEnumerable<ICmPossibility> GetSelectedPossibilites(TreeNode node)
		{
			if (IsNodeSelected(node))
			{
				ICmPossibility poss = node.Tag as ICmPossibility;
				if (poss != null)
					yield return poss;
			}

			foreach (TreeNode childNode in node.Nodes)
				foreach (ICmPossibility poss in GetSelectedPossibilites(childNode))
					yield return poss;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selected items for the specified node and its children.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void GetSelectedItems(TreeNode node, List<string> selectedItems)
		{
			if (IsNodeSelected(node))
				selectedItems.Add(node.Text);

			foreach (TreeNode childNode in node.Nodes)
				GetSelectedItems(childNode, selectedItems);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the specified node is selected. For it to
		/// be selected, it has to checked while its parent is not.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool IsNodeSelected(TreeNode node)
		{
			return (GetChecked(node) == CheckState.Checked &&
				(node.Parent == null || GetChecked(node.Parent) != CheckState.Checked));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the text representing the possibility.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual string GetPossibilityText(ICmPossibility possibility)
		{
			return possibility.Name.BestAnalysisAlternative.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a node for a possibility (including populating its child nodes)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual TreeNode CreateNode(ICmPossibility possibility)
		{
			string text = TsStringUtils.NormalizeToNFC(GetPossibilityText(possibility));
			TreeNode node = new TreeNode(text);
			node.Tag = possibility;
			PopulateChildNodes(node, possibility);

			if (m_initiallySelectedHvos.Contains(possibility.Hvo))
				m_initiallyCheckedNodes.Add(node);

			return node;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populate the node with the children of the possibility. (recursive)
		/// </summary>
		/// <param name="node">given node</param>
		/// <param name="possibility">The possibility whose subpossibilities will be added as
		/// the child nodes</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void PopulateChildNodes(TreeNode node, ICmPossibility possibility)
		{
			foreach (ICmPossibility subPossibility in possibility.SubPossibilitiesOS)
				node.Nodes.Add(CreateNode(subPossibility));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateSelectedLabel()
		{
			if (m_lblSelectedCategories != null)
			{
				m_lblSelectedCategories.Text = (SelectedItems.Count == 0 ?
					FwCoreDlgs.Properties.Resources.kstidNoPossibilitySelectedText :
					SelectedItems.ToString(", "));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnAfterCheck(TreeViewEventArgs e)
		{
			UpdateSelectedLabel();
			base.OnAfterCheck(e);
		}
	}
}
