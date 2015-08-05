using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.LexText.Controls;
using XCore;

namespace SIL.FieldWorks.IText
{
	internal class PossibilityComboController : POSPopupTreeManager
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public PossibilityComboController(TreeCombo treeCombo, FdoCache cache, ICmPossibilityList list, int ws, bool useAbbr, Mediator mediator, IPropertyTable propertyTable, Form parent) :
			base(treeCombo, cache, list, ws, useAbbr, mediator, propertyTable, parent)
		{
			Sorted = true;
		}

		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{
			int tagName = UseAbbr ?
				CmPossibilityTags.kflidAbbreviation :
				CmPossibilityTags.kflidName;
			popupTree.Sorted = Sorted;
			TreeNode match = null;
			if (List != null)
				match = AddNodes(popupTree.Nodes, List.Hvo,
									CmPossibilityListTags.kflidPossibilities, hvoTarget, tagName);
			var empty = AddAnyItem(popupTree);
			if (hvoTarget == 0)
				match = empty;
			return match;
		}

		public bool Sorted { get; set; }
	}
}
