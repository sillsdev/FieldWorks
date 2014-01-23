// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PhonologicalFeaturesTreeView.cs
// Responsibility: Andy Black

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;


namespace SIL.FieldWorks.LexText.Controls.MGA
{
	/// <summary>
	/// Summary description for PhonologicalFeaturesTreeView.
	/// </summary>
	public class PhonologicalFeaturesTreeView : GlossListTreeView
	{
		protected override TreeNode CreateNewNode(XmlNode currentNode, string sType, StringBuilder sbNode, string sTerm)
		{
			TreeNode newNode;
			// we always use a check box
			GlossListTreeView.ImageKind ik = ImageKind.checkBox;
			newNode = new TreeNode(TsStringUtils.NormalizeToNFC(sbNode.ToString()), (int)ik, (int)ik);
			MasterPhonologicalFeature mpf = new MasterPhonologicalFeature(currentNode, ik, sTerm);
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
		private void ToggleCheckBoxImage(TreeNode tn)
		{
			if (tn.Checked)
				tn.ImageIndex = tn.SelectedImageIndex = (int)ImageKind.checkedBox;
			else
				tn.ImageIndex = tn.SelectedImageIndex = (int)ImageKind.checkBox;
		}

		private void SetCheckedValueOfAllDaughterNodes(TreeNode tn)
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
