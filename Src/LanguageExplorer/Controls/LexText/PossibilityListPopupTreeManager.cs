// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class implements PopupTreeManager for a possibility list.
	/// </summary>
	public class PossibilityListPopupTreeManager : PopupTreeManager
	{
		public PossibilityListPopupTreeManager(TreeCombo treeCombo, LcmCache cache,
			IPropertyTable propertyTable, IPublisher publisher, ICmPossibilityList list, int ws, bool useAbbr, Form parent)
			: base(treeCombo, cache, propertyTable, publisher, list, ws, useAbbr, parent)
		{
		}

		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{
			TreeNode match1 = AddPossibilityListItems(popupTree, hvoTarget);
			TreeNode match2 = AppendAdditionalItems(popupTree, hvoTarget);
			return match1 ?? match2;
		}
	}
}