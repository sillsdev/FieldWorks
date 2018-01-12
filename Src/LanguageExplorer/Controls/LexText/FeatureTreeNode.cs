// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace LanguageExplorer.Controls.LexText
{
	internal class FeatureTreeNode : TreeNode, IComparable
	{
		protected bool m_fChosen;
		public FeatureTreeNode(string sName, int i, int iSel, int iHvo, FeatureTreeNodeInfo.NodeKind eKind) : base(sName, i, iSel)
		{
			FeatureTreeNodeInfo info = new FeatureTreeNodeInfo(iHvo, eKind);
			Tag = info;
		}
		public int CompareTo(object obj)
		{
			TreeNode node = obj as TreeNode;
			if (node == null)
				return 0; // not sure what else to do...
			return Text.CompareTo(node.Text);
		}
		/// <summary>
		/// Gets/sets whether the node has been chosen by the user
		/// </summary>
		/// <remarks>For some reason, using the Checked property of TreeNode did not work.
		/// I could set Checked to true when loading a feature structure, but when the dialog closed,
		/// the value would always be false.</remarks>
		public bool Chosen
		{
			get
			{
				return m_fChosen;
			}
			set
			{
				m_fChosen = value;
			}
		}
		/// <summary>
		/// Hvo associated with the node
		/// </summary>
		public int Hvo
		{
			get
			{
				FeatureTreeNodeInfo info = Tag as FeatureTreeNodeInfo;
				if (info == null)
					return 0;
				else
					return info.iHvo;
			}
		}
		/// <summary>
		/// Type of node
		/// </summary>
		public FeatureTreeNodeInfo.NodeKind Kind
		{
			get
			{
				FeatureTreeNodeInfo info = Tag as FeatureTreeNodeInfo;
				if (info == null)
					return FeatureTreeNodeInfo.NodeKind.Other;
				else
					return info.eKind;
			}
		}


	}
}