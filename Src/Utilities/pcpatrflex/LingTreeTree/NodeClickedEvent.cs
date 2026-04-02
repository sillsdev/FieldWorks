using System;
using System.ComponentModel;

namespace LingTree
{
	/// <summary>
	/// Summary description for LingTreeNodeClickedEvent.
	/// </summary>
	public class LingTreeNodeClickedEventArgs : EventArgs
	{
		private LingTreeNode m_node;

		public LingTreeNodeClickedEventArgs(LingTreeNode node)
		{
			m_node = node;
		}

		public LingTreeNode Node
		{
			get { return m_node; }
			set { m_node = value; }
		}
	}

	// Delegate declaration.
	//
	public delegate void LingTreeNodeClickedEventHandler(
		object sender,
		LingTreeNodeClickedEventArgs e
	);
}
