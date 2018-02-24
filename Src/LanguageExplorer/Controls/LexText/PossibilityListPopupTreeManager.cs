// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This class implements PopupTreeManager for a possibility list.
	/// </summary>
	public class PossibilityListPopupTreeManager : PopupTreeManager
	{
		public PossibilityListPopupTreeManager(TreeCombo treeCombo, LcmCache cache, IPropertyTable propertyTable, IPublisher publisher, ICmPossibilityList list, int ws, bool useAbbr, Form parent)
			: base(treeCombo, cache, propertyTable, publisher, list, ws, useAbbr, parent)
		{
		}

		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{
			return AddPossibilityListItems(popupTree, hvoTarget) ?? AppendAdditionalItems(popupTree, hvoTarget);
		}
	}
}