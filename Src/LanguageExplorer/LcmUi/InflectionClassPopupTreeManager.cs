// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// Handles a TreeCombo control for use in selecting inflection classes.
	/// </summary>
	public class InflectionClassPopupTreeManager : PopupTreeManager
	{
		/// <summary />
		public InflectionClassPopupTreeManager(TreeCombo treeCombo, LcmCache cache, IPropertyTable propertyTable, IPublisher publisher, bool useAbbr, Form parent, int wsDisplay)
			: base(treeCombo, cache, propertyTable, publisher, cache.LanguageProject.PartsOfSpeechOA, wsDisplay, useAbbr, parent)
		{
		}

		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{
			var tagNamePOS = UseAbbr ? CmPossibilityTags.kflidAbbreviation : CmPossibilityTags.kflidName;
			var relevantPartsOfSpeech = new List<HvoTreeNode>();
			ControlServices.GatherPartsOfSpeech(Cache, PartOfSpeechTags.kflidInflectionClasses, tagNamePOS, WritingSystem, relevantPartsOfSpeech);
			relevantPartsOfSpeech.Sort();
			var tagNameClass = UseAbbr ? MoInflClassTags.kflidAbbreviation : MoInflClassTags.kflidName;
			TreeNode match = null;
			foreach (var item in relevantPartsOfSpeech)
			{
				popupTree.Nodes.Add(item);
				var match1 = AddNodes(item.Nodes, item.Hvo, PartOfSpeechTags.kflidInflectionClasses, MoInflClassTags.kflidSubclasses, hvoTarget, tagNameClass);
				if (match1 != null)
				{
					match = match1;
				}
			}
			return match;
		}
	}
}