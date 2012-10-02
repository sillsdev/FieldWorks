// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PhonologicalFeaturesTreeView.cs
// Responsibility: Andy Black
// --------------------------------------------------------------------------------------------
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
			newNode = new TreeNode(StringUtils.NormalizeToNFC(sbNode.ToString()), (int)ik, (int)ik);
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
