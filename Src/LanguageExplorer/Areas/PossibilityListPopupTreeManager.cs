// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// This class implements PopupTreeManager for a possibility list.
	/// </summary>
	public class PossibilityListPopupTreeManager : PopupTreeManager
	{
		public PossibilityListPopupTreeManager(TreeCombo treeCombo, LcmCache cache, FlexComponentParameters flexComponentParameters, ICmPossibilityList list, int ws, bool useAbbr, Form parent)
			: base(treeCombo, cache, flexComponentParameters, list, ws, useAbbr, parent)
		{
		}

		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{
			return AddPossibilityListItems(popupTree, hvoTarget) ?? AppendAdditionalItems(popupTree, hvoTarget);
		}
	}
}