// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Text;
using System.Windows.Forms;
using System.Xml;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.MGA
{
	/// <summary />
	internal class PhonologicalFeaturesTreeView : GlossListTreeView
	{
		protected override TreeNode CreateNewNode(XmlNode currentNode, string sType, StringBuilder sbNode, string sTerm)
		{
			// we always use a check box
			const MGAImageKind imageKind = MGAImageKind.checkBox;
			var newNode = new TreeNode(TsStringUtils.NormalizeToNFC(sbNode.ToString()), (int)imageKind, (int)imageKind);
			var mpf = new MasterPhonologicalFeature(currentNode, imageKind, sTerm);
			newNode.Tag = mpf;
			return newNode;
		}

		protected override void HandleCheckBoxNodes(TreeView tv, TreeNode tn)
		{
			// make all daughters have same check value
			tn.Checked = !tn.Checked;
			ToggleCheckBoxImage(tn);
			SetCheckedValueOfAllDaughterNodes(tn);
		}

		private static void ToggleCheckBoxImage(TreeNode tn)
		{
			if (tn.Checked)
			{
				tn.ImageIndex = tn.SelectedImageIndex = (int)MGAImageKind.checkedBox;
			}
			else
			{
				tn.ImageIndex = tn.SelectedImageIndex = (int)MGAImageKind.checkBox;
			}
		}

		private static void SetCheckedValueOfAllDaughterNodes(TreeNode tn)
		{
			foreach (TreeNode node in tn.Nodes)
			{
				if (node.Checked != tn.Checked)
				{
					node.Checked = tn.Checked;
					ToggleCheckBoxImage(node);
				}
				SetCheckedValueOfAllDaughterNodes(node);
			}
		}
	}
}
