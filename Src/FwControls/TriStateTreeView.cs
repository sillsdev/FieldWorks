// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// A tree view with tri-state check boxes - Unchecked, Checked, and grayed out
	/// </summary>
	/// <remarks>
	/// REVIEW: If we want to have icons in addition to the check boxes, we probably have to
	/// set the icons for the check boxes in a different way. The windows tree view control
	/// can have a separate image list for states.
	/// </remarks>
	public class TriStateTreeView : TreeView
	{
		private ImageList m_TriStateImages;
		private IContainer components;
		/// <summary>Fired when a node's check box is changed</summary>
		public event EventHandler NodeCheckChanged;

		private const int kGrayCheckImageIndex = 0;
		private const int kUncheckedImageIndex = 1;
		private const int kCheckedImageIndex = 2;

		#region Redefined Win-API structs and methods
		/// <summary></summary>
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct TV_HITTESTINFO
		{
			/// <summary>Client coordinates of the point to test.</summary>
			public Point pt;
			/// <summary>Variable that receives information about the results of a hit test.</summary>
			public TVHit flags;
			/// <summary>Handle to the item that occupies the point.</summary>
			public IntPtr hItem;
		}

		/// <summary>Hit tests for tree view</summary>
		[Flags]
		public enum TVHit
		{
			/// <summary>In the client area, but below the last item.</summary>
			NoWhere = 0x0001,
			/// <summary>On the bitmap associated with an item.</summary>
			OnItemIcon = 0x0002,
			/// <summary>On the label (string) associated with an item.</summary>
			OnItemLabel = 0x0004,
			/// <summary>In the indentation associated with an item.</summary>
			OnItemIndent = 0x0008,
			/// <summary>On the button associated with an item.</summary>
			OnItemButton = 0x0010,
			/// <summary>In the area to the right of an item. </summary>
			OnItemRight = 0x0020,
			/// <summary>On the state icon for a tree-view item that is in a user-defined state.</summary>
			OnItemStateIcon = 0x0040,
			/// <summary>On the bitmap or label associated with an item. </summary>
			OnItem = (OnItemIcon | OnItemLabel | OnItemStateIcon),
			/// <summary>Above the client area. </summary>
			Above = 0x0100,
			/// <summary>Below the client area.</summary>
			Below = 0x0200,
			/// <summary>To the right of the client area.</summary>
			ToRight = 0x0400,
			/// <summary>To the left of the client area.</summary>
			ToLeft = 0x0800
		}

		/// <summary>
		/// TreeView messages
		/// </summary>
		public enum TreeViewMessages
		{
			/// <summary />
			TV_FIRST = 0x1100,
			/// <summary />
			TVM_HITTEST = (TV_FIRST + 17),
		}

		/// <summary />
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int SendMessage(IntPtr hWnd, TreeViewMessages msg, int wParam, ref TV_HITTESTINFO lParam);
		#endregion

		#region Constructor and destructor

		/// <summary />
		public TriStateTreeView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			if (Application.RenderWithVisualStyles)
			{
				using (var bmp = new Bitmap(m_TriStateImages.ImageSize.Width, m_TriStateImages.ImageSize.Height))
				{
					var rc = new Rectangle(0, 0, bmp.Width, bmp.Height);
					using (var graphics = Graphics.FromImage(bmp))
					{
						var renderer = new VisualStyleRenderer(VisualStyleElement.Button.CheckBox.CheckedDisabled);
						renderer.DrawBackground(graphics, rc, rc);
						m_TriStateImages.Images[0] = bmp;

						renderer.SetParameters(VisualStyleElement.Button.CheckBox.UncheckedNormal);
						renderer.DrawBackground(graphics, rc, rc);
						m_TriStateImages.Images[1] = bmp;

						renderer.SetParameters(VisualStyleElement.Button.CheckBox.CheckedNormal);
						renderer.DrawBackground(graphics, rc, rc);
						m_TriStateImages.Images[2] = bmp;
					}
				}
			}

			var index = GetCheckImageIndex(TriStateTreeViewCheckState.Unchecked);
			ImageList = m_TriStateImages;
			ImageIndex = index;
			SelectedImageIndex = index;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
				m_TriStateImages?.Dispose();
			}

			m_TriStateImages = null;

			base.Dispose(disposing);
		}

		#endregion

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TriStateTreeView));
			this.m_TriStateImages = new System.Windows.Forms.ImageList(this.components);
			this.SuspendLayout();
			//
			// m_TriStateImages
			//
			this.m_TriStateImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_TriStateImages.ImageStream")));
			this.m_TriStateImages.TransparentColor = System.Drawing.Color.Magenta;
			this.m_TriStateImages.Images.SetKeyName(0, "");
			this.m_TriStateImages.Images.SetKeyName(1, "");
			this.m_TriStateImages.Images.SetKeyName(2, "");
			this.ResumeLayout(false);

		}
		#endregion

		#region Hide no longer appropriate properties from Designer

		/// <summary />
		[Browsable(false)]
		public new bool CheckBoxes
		{
			get
			{
				return base.CheckBoxes;
			}
			set
			{
				base.CheckBoxes = value;
			}
		}

		/// <summary />
		[Browsable(false)]
		public new int ImageIndex
		{
			get
			{
				return base.ImageIndex;
			}
			set
			{
				base.ImageIndex = value;
			}
		}

		/// <summary />
		[Browsable(false)]
		public new ImageList ImageList
		{
			get
			{
				return base.ImageList;
			}
			set
			{
				base.ImageList = value;
			}
		}

		/// <summary />
		[Browsable(false)]
		public new int SelectedImageIndex
		{
			get
			{
				return base.SelectedImageIndex;
			}
			set
			{
				base.SelectedImageIndex = value;
			}
		}
		#endregion

		#region Overrides

		/// <inheritdoc />
		protected override void OnClick(EventArgs e)
		{
			base.OnClick(e);

			if (Platform.IsMono)
			{
				// The SendMessage determines whether we've hit the node proper or the
				// +/- box to expand or collapse.  We'll try to check this by looking
				// at the X location ourselves.  (See FWNX-468.)
				var pt = PointToClient(MousePosition);
				var node = GetNodeAt(pt); // This uses only the Y location.
				if (node != null)
				{
					var bounds = node.Bounds; // This gives the text area of the node.
					if (pt.X >= bounds.X - 20 && pt.X < bounds.X)
					{
						ChangeNodeState(node);
					}
				}
			}
			else
			{
				var hitTestInfo = new TV_HITTESTINFO
				{
					pt = PointToClient(MousePosition)
				};
				SendMessage(Handle, TreeViewMessages.TVM_HITTEST, 0, ref hitTestInfo);
				if ((hitTestInfo.flags & TVHit.OnItemIcon) == TVHit.OnItemIcon)
				{
					var node = GetNodeAt(hitTestInfo.pt);
					if (node != null)
					{
						ChangeNodeState(node);
					}
				}
			}
		}

		/// <inheritdoc />
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			if (e.KeyCode == Keys.Space)
			{
				ChangeNodeState(SelectedNode);
			}
		}
		#endregion

		#region Private methods

		/// <summary>
		/// Checks or unchecks all children
		/// </summary>
		protected bool CheckNode(TreeNode node, TriStateTreeViewCheckState state)
		{
			if (!InternalSetChecked(node, state))
			{
				return false;
			}
			foreach (TreeNode child in node.Nodes)
			{
				CheckNode(child, state);
			}
			return true;
		}

		/// <summary>
		/// Called after a node changed its state. Has to go through all direct children and
		/// set state based on children's state.
		/// </summary>
		protected void ChangeParent(TreeNode node)
		{
			if (node == null)
			{
				return;
			}
			var state = GetChecked(node.FirstNode);
			foreach (TreeNode child in node.Nodes)
			{
				state |= GetChecked(child);
			}
			if (InternalSetChecked(node, state))
			{
				ChangeParent(node.Parent);
			}
		}

		/// <summary>
		/// Handles changing the state of a node
		/// </summary>
		protected void ChangeNodeState(TreeNode node)
		{
			if (node == null)
			{
				return;
			}
			BeginUpdate();
			try
			{
				var currState = GetCheckStateFromImageIndex(node.ImageIndex);
				var newState = (currState == TriStateTreeViewCheckState.Unchecked ? TriStateTreeViewCheckState.Checked : TriStateTreeViewCheckState.Unchecked);

				if (!CheckNode(node, newState))
				{
					return;
				}
				ChangeParent(node.Parent);
			}
			finally
			{
				EndUpdate();
			}

			// Tell our listeners that one of our nodes changed.
			NodeCheckChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Sets the checked state of a node, but doesn't deal with children or parents
		/// </summary>
		/// <param name="node">Node</param>
		/// <param name="state">The new checked state</param>
		/// <returns><c>true</c> if checked state was set to the requested state, otherwise
		/// <c>false</c>.</returns>
		private bool InternalSetChecked(TreeNode node, TriStateTreeViewCheckState state)
		{
			var args = new TreeViewCancelEventArgs(node, false, TreeViewAction.Unknown);

			OnBeforeCheck(args);

			if (args.Cancel)
			{
				return false;
			}
			var index = GetCheckImageIndex(state);
			node.ImageIndex = index;
			node.SelectedImageIndex = index;

			OnAfterCheck(new TreeViewEventArgs(node, TreeViewAction.Unknown));
			return GetChecked(node) == state;
		}

		/// <summary>
		/// Gets the image index of the specified check state.
		/// </summary>
		private int GetCheckImageIndex(TriStateTreeViewCheckState state)
		{
			switch (state)
			{
				case TriStateTreeViewCheckState.Checked: return kCheckedImageIndex;
				case TriStateTreeViewCheckState.Unchecked: return kUncheckedImageIndex;
				default: return kGrayCheckImageIndex;
			}
		}

		/// <summary>
		/// Gets the checks state for the specified image index.
		/// </summary>
		private TriStateTreeViewCheckState GetCheckStateFromImageIndex(int index)
		{
			switch (index)
			{
				case kCheckedImageIndex: return TriStateTreeViewCheckState.Checked;
				case kGrayCheckImageIndex: return TriStateTreeViewCheckState.GrayChecked;
				default: return TriStateTreeViewCheckState.Unchecked;
			}
		}

		/// <summary>
		/// Build a list of all of the tag data for checked items in the tree.
		/// </summary>
		private void BuildTagDataList(TreeNode node, ArrayList list)
		{
			if (GetChecked(node) == TriStateTreeViewCheckState.Checked && node.Tag != null)
			{
				list.Add(node.Tag);
				FillInMissingChildren(node);
			}
			foreach (TreeNode child in node.Nodes)
			{
				BuildTagDataList(child, list);
			}
		}

		/// <summary>
		/// This should be filled in if using lazy evaluation to create nodes, to fill in any needed
		/// children of a checked node when obtaining all the checked ones.
		/// </summary>
		protected virtual void FillInMissingChildren(TreeNode node)
		{
		}

		/// <summary>
		/// Look through the tree nodes to find the node that has given tag data and check it.
		/// </summary>
		private void FindAndCheckNode(TreeNode node, object tag, TriStateTreeViewCheckState state)
		{
			if (node.Tag != null && node.Tag.Equals(tag))
			{
				SetChecked(node, state);
				return;
			}
			foreach (TreeNode child in node.Nodes)
			{
				FindAndCheckNode(child, tag, state);
			}
		}
		#endregion

		#region Public methods

		/// <summary>
		/// Gets the checked state of a node
		/// </summary>
		public TriStateTreeViewCheckState GetChecked(TreeNode node)
		{
			return GetCheckStateFromImageIndex(node.ImageIndex);
		}

		/// <summary>
		/// Sets the checked state of a node
		/// </summary>
		public void SetChecked(TreeNode node, TriStateTreeViewCheckState state)
		{
			if (CheckNode(node, state))
			{
				ChangeParent(node.Parent);
			}
		}

		/// <summary>
		/// Find a node in the tree that matches the given tag data and set its checked state
		/// </summary>
		public void CheckNodeByTag(object tag, TriStateTreeViewCheckState state)
		{
			if (tag == null)
			{
				return;
			}
			FillInIfHidden(tag);

			foreach (TreeNode node in Nodes)
			{
				FindAndCheckNode(node, tag, state);
			}
		}

		/// <summary>
		/// If using lazy initialization, fill this in to make sure the relevant part of the tree is built
		/// so that the specified tag can be checked.
		/// </summary>
		protected virtual void FillInIfHidden(object tag)
		{
		}

		/// <summary>
		/// Return a list of the tag data for all of the checked items in the tree
		/// </summary>
		public ArrayList GetCheckedTagData()
		{
			var list = new ArrayList();
			foreach (TreeNode node in Nodes)
			{
				BuildTagDataList(node, list);
			}

			return list;
		}

		/// <summary>
		/// Return a list of nodes having the specified state (or states, since CheckState
		/// is a Flags enum).
		/// </summary>
		public TreeNode[] GetNodesWithState(TriStateTreeViewCheckState state)
		{
			var list = new List<TreeNode>();

			foreach (TreeNode node in Nodes)
			{
				BuildCheckedNodeList(node, state, list);
			}

			return list.ToArray();
		}

		/// <summary>
		/// Build a list of all of the checked nodes.
		/// </summary>
		private void BuildCheckedNodeList(TreeNode node, TriStateTreeViewCheckState state, List<TreeNode> list)
		{
			// If we're seeking nodes having either state (i.e., GreyChecked), then add them to
			// the list unconditionally. (Currently, this is only needed for a test and is not
			// likely to have any practical application.)
			BuildCheckedNodeList(node, state, true, list);
		}

		/// <summary>
		/// Build a list of all of the checked nodes.
		/// </summary>
		private void BuildCheckedNodeList(TreeNode node, TriStateTreeViewCheckState state, bool useGrey, List<TreeNode> list)
		{
			if ((useGrey && state == TriStateTreeViewCheckState.GrayChecked) || GetChecked(node) == state)
			{
				list.Add(node);
			}
			foreach (TreeNode child in node.Nodes)
			{
				BuildCheckedNodeList(child, state, list);
			}
		}

		/// <summary>
		/// Build a list of all of the checked nodes.
		/// </summary>
		public List<TreeNode> GetCheckedNodeList()
		{
			var list = new List<TreeNode>();
			foreach (TreeNode node in Nodes)
			{
				BuildCheckedNodeList(node, TriStateTreeViewCheckState.Checked, false, list);
			}
			return list;
		}

		/// <summary>
		/// Return a list of nodes having the specified state (or states, since CheckState
		/// is a Flags enum).
		/// </summary>
		public TreeNode[] GetNodesOfTypeWithState(Type nodeType, TriStateTreeViewCheckState state)
		{
			var list = new List<TreeNode>(GetNodesWithState(state));
			for (var i = list.Count - 1; i >= 0; i--)
			{
				// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
				// is marked with [MonoTODO] and might not work as expected in 4.0.
				if (list[i].GetType() != nodeType)
				{
					list.RemoveAt(i);
				}
			}

			return list.ToArray();
		}

		#endregion
	}
}