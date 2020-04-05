// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This class implements PopupTreeManager for a possibility list.
	/// </summary>
	internal sealed class PossibilityListPopupTreeManager : PopupTreeManager
	{
		internal PossibilityListPopupTreeManager(TreeCombo treeCombo, LcmCache cache, FlexComponentParameters flexComponentParameters, ICmPossibilityList list, int ws, bool useAbbr, Form parent)
			: base(treeCombo, cache, flexComponentParameters, list, ws, useAbbr, parent)
		{
		}

		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{
			return AddPossibilityListItems(popupTree, hvoTarget) ?? AppendAdditionalItems(popupTree, hvoTarget);
		}
	}
}