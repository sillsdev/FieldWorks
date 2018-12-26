// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace LanguageExplorer.Controls
{
	internal class FeatureTreeNode : TreeNode, IComparable
	{
		public FeatureTreeNode(string sName, int i, int iSel, int iHvo, FeatureTreeNodeKind eKind) : base(sName, i, iSel)
		{
			Tag = new FeatureTreeNodeInfo(iHvo, eKind);
		}

		public int CompareTo(object obj)
		{
			var node = obj as TreeNode;
			return node == null ? 0 : Text.CompareTo(node.Text);
		}

		/// <summary>
		/// Gets/sets whether the node has been chosen by the user
		/// </summary>
		/// <remarks>For some reason, using the Checked property of TreeNode did not work.
		/// I could set Checked to true when loading a feature structure, but when the dialog closed,
		/// the value would always be false.</remarks>
		public bool Chosen { get; set; }

		/// <summary>
		/// Hvo associated with the node
		/// </summary>
		public int Hvo
		{
			get
			{
				var info = Tag as FeatureTreeNodeInfo;
				return info?.iHvo ?? 0;
			}
		}

		/// <summary>
		/// Type of node
		/// </summary>
		public FeatureTreeNodeKind Kind
		{
			get
			{
				var info = Tag as FeatureTreeNodeInfo;
				return info?.eKind ?? FeatureTreeNodeKind.Other;
			}
		}

		private sealed class FeatureTreeNodeInfo
		{
			public FeatureTreeNodeKind eKind;

			public int iHvo;

			public FeatureTreeNodeInfo(int hvo, FeatureTreeNodeKind kind)
			{
				iHvo = hvo;
				eKind = kind;
			}
		}
	}
}