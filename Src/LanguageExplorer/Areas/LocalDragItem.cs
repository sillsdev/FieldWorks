// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Used to represent a drag from one place in the tree view to another. This is the only kind of drag
	/// currently supported.
	/// </summary>
	internal sealed class LocalDragItem
	{
		public LocalDragItem(ITreeBarHandler treeBarHandler, TreeNode sourceTreeNode)
		{
			TreeBarHandler = treeBarHandler;
			SourceNode = sourceTreeNode;
		}
		public ITreeBarHandler TreeBarHandler { get; }

		public TreeNode SourceNode { get; }
	}
}