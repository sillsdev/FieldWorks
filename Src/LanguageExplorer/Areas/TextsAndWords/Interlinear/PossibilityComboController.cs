// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.LexText;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class PossibilityComboController : POSPopupTreeManager
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public PossibilityComboController(TreeCombo treeCombo, LcmCache cache, ICmPossibilityList list, int ws, bool useAbbr, IPropertyTable propertyTable, IPublisher publisher, Form parent) :
			base(treeCombo, cache, list, ws, useAbbr, propertyTable, publisher, parent)
		{
			Sorted = true;
		}

		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{
			var tagName = UseAbbr ? CmPossibilityTags.kflidAbbreviation : CmPossibilityTags.kflidName;
			popupTree.Sorted = Sorted;
			TreeNode match = null;
			if (List != null)
			{
				match = AddNodes(popupTree.Nodes, List.Hvo, CmPossibilityListTags.kflidPossibilities, hvoTarget, tagName);
			}
			var empty = AddAnyItem(popupTree);
			if (hvoTarget == 0)
			{
				match = empty;
			}
			return match;
		}

		public bool Sorted { get; set; }
	}
}