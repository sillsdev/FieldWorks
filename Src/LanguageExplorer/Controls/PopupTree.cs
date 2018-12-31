// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.PlatformUtilities;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// PopupTree is a form containing a TreeView, designed to pop up like the list of a combo box
	/// to allow a one-time choice from a list.
	/// Although this is currently implemented using the standard TreeView class, it is possible
	/// that a need will arise for items to be rendered using View code, either to support styled
	/// text in items, or to support Graphite rendering. For this reason the TreeView is not
	/// made public, and only a limited subset of its capabilities is currently exposed.
	/// </summary>
	public class PopupTree : Form, IDropDownBox
	{
		/// <summary />
		public event TreeViewEventHandler AfterSelect;
		/// <summary />
		public event TreeViewCancelEventHandler BeforeSelect;
		/// <summary />
		public event TreeViewEventHandler PopupTreeClosed;
		private TreeViewAction m_selectedNodeAction;
		private bool m_fClientSpecifiedAction;

		private FwTreeView m_treeView;
		private FwPopupMessageFilter m_fwPopupMessageFilter;
		// Store the node indicated by a MouseDown event.
		private TreeNode m_tnMouseDown;
		private Container components = null;
		private bool m_fShown; // true after Show() completes, prevents spurious Hide during spurious AfterSelect.

		/// <summary />
		public PopupTree()
		{
			InitializeComponent();
			StartPosition = FormStartPosition.Manual; // allows us to use Location to position it exactly.
			Size = DefaultSize; // Defeat extraordinary side effect of setting StartPosition.
			EnableAfterAndBeforeSelectHandling(true);
			m_treeView.AccessibleName = "PopupTreeTree";
			if (!Platform.IsMono)
			{
				// FWNX-399 - on mono this sidestep/workaround causes problems because it relies on the order event handlers
				// which on mono can be registered in a difference order.
				// possibly a better way to do this workaround if it is still necessary is to
				// override OnNodeMouseClick.
				// The following two event handlers are needed to sidestep a bug in the AfterSelect
				// processing: the builtin TreeView code does not allow the user to select the
				// highlighted item in the tree, even if it is not really selected, and the TreeView
				// code forces something to be highlighted even when nothing is selected.  So we
				// simulate the Select operation for exactly those conditions: nothing yet selected,
				// or trying to select what has already been selected.
				m_treeView.MouseDown += m_treeView_MouseDown;
				m_treeView.MouseUp += m_treeView_MouseUp;
			}
			m_treeView.KeyDown += m_treeView_KeyDown;
			AccessibleName = "PopupTreeForm";
		}

		/// <summary>
		/// Enables or disables the handlers for the AfterSelect and BeforeSelect events
		/// </summary>
		public void EnableAfterAndBeforeSelectHandling(bool enable)
		{
			if (enable)
			{
				m_treeView.AfterSelect += m_treeView_AfterSelect;
				m_treeView.BeforeSelect += m_treeView_BeforeSelect;
			}
			else
			{
				m_treeView.AfterSelect -= m_treeView_AfterSelect;
				m_treeView.BeforeSelect -= m_treeView_BeforeSelect;
			}
		}

		/// <summary>
		/// Gets the drop down form.
		/// </summary>
		public Form Form => this;

		/// <summary>
		/// Gets or sets the form the PopupTree is launched from.
		/// </summary>
		public Form LaunchingForm { get; set; }

		/// <summary>
		/// Hide the window that shows the popup tree (and activate the parent window, if known).
		/// </summary>
		public void HideForm()
		{
			HideForm(true);
		}

		/// <summary>
		/// Hide the window that shows the popup tree (and activate the parent window, if requested).
		/// </summary>
		public void HideForm(bool activateParent)
		{
			m_tnMouseDown = null;
			if (Visible && m_fShown)
			{
				Hide();
				m_fShown = false;
				RemoveFilter();
				if (activateParent)
				{
					LaunchingForm?.Activate();
				}
				OnHide();   // notify owners of this event.
			}
		}

		// notify our owners that we've hidden the PopupTree.
		private void OnHide()
		{
			PopupTreeClosed?.Invoke(this, new TreeViewEventArgs(m_treeView.SelectedNode, TreeViewAction.Unknown));
		}

		/// <summary>
		/// JohnT: I don't know why we need this, but for some obscure reason, after everything else
		/// happens and we are hidden, we get activated again. Then the main window is not active,
		/// and can't get keyboard events. AArgh! This is a horrible kludge but the best I can find.
		/// </summary>
		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			if (!Visible)
			{
				LaunchingForm?.Activate();
			}
		}

		/// <summary>
		/// Cycle the selection to the next node that begins with the specified text.
		/// (Case insensitive.)
		/// </summary>
		/// <param name="start">the search key</param>
		public void SelectNodeStartingWith(string start)
		{
			var iPreviousSelected = -1;
			int iStarting;
			// If the new start key matches the start key for the current selection,
			// we'll start our search from there.
			if (m_treeView.SelectedNode != null && m_treeView.SelectedNode.Text.ToLower().StartsWith(start.ToLower()))
			{
				// save the current position as our previous index
				iPreviousSelected = m_treeView.SelectedNode.Index;
				// set our search index to one after the currently selected node
				iStarting = iPreviousSelected + 1;
			}
			else
			{
				// start our search from the beginning
				iStarting = 0;
			}
			var iEnding = m_treeView.Nodes.Count - 1;
			var fFound = FindAndSelectNodeStartingWith(iStarting, iEnding, start);
			if (!fFound && iStarting != 0)
			{
				// Cycle from the beginning and see if we find a match before our
				// previous (i.e. current) selection.
				iStarting = 0;
				iEnding = iPreviousSelected - 1;
				FindAndSelectNodeStartingWith(iStarting, iEnding, start);
			}
		}

		/// <summary>
		/// Select the node whose text starts with the given startKey string (if node exists).
		/// </summary>
		private bool FindAndSelectNodeStartingWith(int iStarting, int iEnding, string startKey)
		{
			var fFound = false;
			for (var i = iStarting; i <= iEnding; ++i)
			{
				if (m_treeView.Nodes[i].Text.ToLower().StartsWith(startKey.ToLower()))
				{
					// Select this node
					m_treeView.SelectedNode = m_treeView.Nodes[i];
					fFound = true;
					break;
				}
			}
			return fFound;
		}

		/// <summary>
		/// Begin update on the tree...when you want to add multiple nodes.
		/// </summary>
		public void BeginUpdate()
		{
			m_treeView.BeginUpdate();
		}

		/// <summary>
		/// End update started with BeginUpdate.
		/// </summary>
		public void EndUpdate()
		{
			m_treeView.EndUpdate();
		}

		/// <summary>
		/// Make sure we clean out the message filter if not already done.
		/// </summary>
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			if (!Visible)
			{
				RemoveFilter();
			}
		}

		// Remove your message filter, typically prior to hiding, or because we got hidden.
		internal void RemoveFilter()
		{
			if (m_fwPopupMessageFilter != null)
			{
				Application.RemoveMessageFilter(m_fwPopupMessageFilter);
				m_fwPopupMessageFilter.Dispose();
				m_fwPopupMessageFilter = null;
			}
		}

		/// <summary />
		protected override Size DefaultSize => new Size(120, 200);

		/// <summary>
		/// Get/Set the selected node.
		/// </summary>
		public TreeNode SelectedNode
		{
			get
			{
				return m_treeView.SelectedNode;
			}
			set
			{
				if (Platform.IsMono)
				{
					// FWNX-267: forcing the creation of the handle to ensure setting SelectedNode generates the AfterSelect event.
					var h = m_treeView.Handle;
				}
				m_treeView.SelectedNode = value;
				if (value != null)
				{
					value.EnsureVisible(); // Make sure it is visible.
				}
				else
				{
					m_tnMouseDown = null;
				}
			}
		}

		/// <summary>
		/// Get/Set the selected node.
		/// </summary>
		public bool Sorted
		{
			get
			{
				return m_treeView.Sorted;
			}
			set
			{
				m_treeView.Sorted = value;
			}
		}

		/// <summary>
		/// Client can force PopupTree Selection to be based upon this TreeViewAction.
		/// This setting will be disabled after the selection.
		/// </summary>
		public TreeViewAction SelectByAction
		{
			set
			{
				m_fClientSpecifiedAction = true;
				m_selectedNodeAction = value;
			}
		}

		/// <summary>
		/// Get the main collection of Nodes. Manipulating this is the main way of
		/// adding and removing items from the popup tree.
		/// </summary>
		public TreeNodeCollection Nodes => m_treeView.Nodes;

		/// <summary>
		/// This will be the Control used for tabbing to the next control, since in the context of a TreeCombo
		/// the PopupTree gets created on a separate form than its sibling ComboTextBox which is on the same form
		/// as the other tabstops.
		/// </summary>
		internal Control TabStopControl { get; set; }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				RemoveFilter(); // Disposes m_fwPopupMessageFilter, among other things.
				if (m_treeView != null)
				{
					EnableAfterAndBeforeSelectHandling(false);
					if (!Platform.IsMono)
					{
						// FWNX-399
						m_treeView.MouseDown -= m_treeView_MouseDown;
						m_treeView.MouseUp -= m_treeView_MouseUp;
					}
					m_treeView.KeyDown -= m_treeView_KeyDown;
					if (!Controls.Contains(m_treeView))
						m_treeView.Dispose();
				}
				components?.Dispose();
			}
			m_fwPopupMessageFilter = null;
			m_treeView = null;
			m_tnMouseDown = null;
			TabStopControl = null;

			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_treeView = new FwTreeView();
			this.SuspendLayout();
			//
			// m_treeView
			//
			this.m_treeView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_treeView.ImageIndex = -1;
			//this.m_treeView.Location = new System.Drawing.Point(0, 0);
			this.m_treeView.Name = "m_treeView";
			this.m_treeView.SelectedImageIndex = -1;
			//this.m_treeView.Size = new System.Drawing.Size(292, 262);
			this.m_treeView.TabIndex = 0;
			//
			// PopupTree
			//
			//this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			//this.ClientSize = new System.Drawing.Size(292, 262);
			this.Controls.Add(this.m_treeView);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PopupTree";
			this.ShowInTaskbar = false;
			this.Text = "PopupTree";
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// pass on the event to clients.
		/// </summary>
		private void m_treeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
		{
			BeforeSelect?.Invoke(this, m_fClientSpecifiedAction ? new TreeViewCancelEventArgs(e.Node, e.Cancel, m_selectedNodeAction) : e);
		}

		// Pass on the AfterSelect event from the embedded treeview to our own clients.
		// Automatically close the form.
		private void m_treeView_AfterSelect(object sender, TreeViewEventArgs e)
		{
			// TreeViewAction.Unknown will happen if the window is not showing,
			// but the client code selected a node.
			// In the context of the Create entry dlg,
			// I (RandyR) do need the event to go out,
			// since that is what will fill the combo box's (TreeCombo) text box.
			if (AfterSelect != null)
			{
				if (m_fClientSpecifiedAction)
				{
					AfterSelect(this, new TreeViewEventArgs(e.Node, m_selectedNodeAction));
					// disable the TreeViewAction override after selection
					m_fClientSpecifiedAction = false;
				}
				else
				{
					AfterSelect(this, e);
				}
			}
		}

		/// <summary>
		/// Launch the PopupTree.
		/// Typical usage, where 'this' is a control that the list should appear below:
		/// 		m_listBox.Launch(Parent.RectangleToScreen(Bounds), Screen.GetWorkingArea(this));
		/// Or, where rect is a rectangle in the client area of control 'this':
		///			m_listBox.Launch(RectangleToScreen(rect), Screen.GetWorkingArea(this);
		///	(Be sure to set the height and width of the PopupTree first.)
		/// </summary>
		/// <param name="launcherBounds">A rectangle in 'screen' coordinates indicating where to display the list. Typically, as shown
		/// above, the location of something the user clicked to make the list display. It's significance is that
		/// the tree will usually be shown with its top left just to the right of the bottom left of the rectangle, and
		/// (if the tree width has not already been set explicitly) its width will match the rectangle. If there is not
		/// room to display the tree below this rectangle, it will be displayed above instead.</param>
		/// <param name="screenBounds">A rectangle in 'screen' coordinates indicating the location of the actual screen
		/// that the tree is to appear on.</param>
		public void Launch(Rectangle launcherBounds, Rectangle screenBounds)
		{
			//Figure where to put it. First try right below the main combo box.
			// Pathologically the list box may be bigger than the available height. If so shrink it.
			var maxListHeight = Math.Max(launcherBounds.Top - screenBounds.Top, screenBounds.Bottom - launcherBounds.Bottom);
			if (Height > maxListHeight)
			{
				Height = maxListHeight;
			}
			// This is the default position right below the launcherBounds.
			var popupBounds = new Rectangle(launcherBounds.Left, launcherBounds.Bottom, this.Width, this.Height);
			if (screenBounds.Bottom < popupBounds.Bottom)
			{
				// extends below the bottom of the screen. Use a rectangle above instead.
				// We already made sure it will fit in one place or the other.
				popupBounds = new Rectangle(launcherBounds.Left, launcherBounds.Top - Height, Width, Height);
			}
			if (screenBounds.Right < popupBounds.Right)
			{
				// Extends too far to the right; adjust (amount is negative to move left).
				popupBounds.Offset(screenBounds.Right - popupBounds.Right, 0);
			}
			if (screenBounds.Left > popupBounds.Left)
			{
				// Extends too far to the left; adjust (amount is positive to move right).
				popupBounds.Offset(screenBounds.Left - popupBounds.Left, 0);
			}
			Location = new Point(popupBounds.Left, popupBounds.Top);
			// Once the launching form has been set, it should never need to be changed.
			// See FWNX-748 for an example of things going wrong (at least on Mono).
			if (LaunchingForm == null)
			{
				LaunchingForm = ActiveForm;
			}
			Debug.Assert(LaunchingForm != this);
			if (Platform.IsMono)
			{
				// FWNX-520: avoid a weird mono problem
				Show(LaunchingForm);
			}
			else
			{
				Show();
			}
			m_fShown = true;
			var selNode = m_treeView.SelectedNode;
			selNode?.EnsureVisible();
			m_fwPopupMessageFilter = new FwPopupMessageFilter(this);
			Application.AddMessageFilter(m_fwPopupMessageFilter);
			m_treeView.Focus();
			// Enhance JohnT: maybe should do something to ensure that, if there's a
			// selected node, things expand and scroll to show it.
		}

		/// <summary>
		/// Handle a MouseDown event, recording the selected node if it seems likely that the
		/// TreeView Select operation will ignore it.
		/// </summary>
		/// <remarks>Method is only used on Windows</remarks>
		private void m_treeView_MouseDown(object sender, MouseEventArgs e)
		{
			Debug.Assert(!Platform.IsMono, "Method only needed on Windows (FWNX-399)");
			var tn = m_treeView.GetNodeAt(e.X, e.Y);
			if (tn != null && e.X >= tn.Bounds.X && e.X <= tn.Bounds.X + tn.Bounds.Width && e.Y >= tn.Bounds.Y && e.Y <= tn.Bounds.Y + tn.Bounds.Height)
			{
				m_tnMouseDown = tn;
			}
			else
			{
				m_tnMouseDown = null;
			}
		}

		/// <summary>
		/// Handle a MouseUp event, selecting the node recorded earlier for MouseDown if it is
		/// the same node in the TreeView control.
		/// </summary>
		/// <remarks>Method is only used on Windows</remarks>
		private void m_treeView_MouseUp(object sender, MouseEventArgs e)
		{
			Debug.Assert(!Platform.IsMono, "Method only needed on Windows (FWNX-399)");
			var tn = m_treeView.GetNodeAt(e.X, e.Y);
			if (tn != null && tn == m_tnMouseDown && e.X >= tn.Bounds.X && e.X <= tn.Bounds.X + tn.Bounds.Width && e.Y >= tn.Bounds.Y && e.Y <= tn.Bounds.Y + tn.Bounds.Height)
			{
				tn = m_treeView.SelectedNode;
				if (tn == null || tn == m_tnMouseDown || (tn == null && m_tnMouseDown != null))
				{
					// set the selected node to null, so that the TreeView will think that the
					// selection has changed, and go ahead and fire the BeforeSelect and AfterSelect
					// events for the node that was clicked
					m_treeView.SelectedNode = null;
				}
			}
			m_tnMouseDown = null;
		}

		private void HandleTreeItemSelect()
		{
			if (!m_fShown)
			{
				return; // spurious one during load form.
			}
			// We want to effectively treat this like a mouse click.
			AfterSelect?.Invoke(this, new TreeViewEventArgs(m_treeView.SelectedNode, TreeViewAction.ByMouse));
			HideForm();
		}

		// Alt-down (or alt-up) arrow or Return selects the entry and closes the box.
		// ESC closes the box without selecting it.
		// Tab key selects item and jumpts to the next/previous field.
		private void m_treeView_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyData)
			{
				case Keys.Up | Keys.Alt:
				case Keys.Down | Keys.Alt:
				case Keys.Return:
					HandleTreeItemSelect();
					e.Handled = true;
					break;
				case Keys.Tab:
				case Keys.Tab | Keys.Shift:
					HandleTreeItemSelect();
					if (TabStopControl != null)
					{
						// Send tab key to the TabStopControl so that it can set the focus to the
						// next control.
						Win32.PostMessage(TabStopControl.Handle, Win32.WinMsgs.WM_KEYDOWN, (int)e.KeyCode, 0);
					}
					e.Handled = true;
					break;
				case Keys.Escape:
				case Keys.Alt | Keys.F4:
					// If we're in a ComboBox, we must handle the Escape key here, otherwise
					// IMessageFilter.PreFilterMessage in FwInnerTextBox will handle it
					// inadvertently forcing the parent dialog to close (cf. LT-2280).
					//
					// LT-9250 Crash while closing the PopupTree by pressing Alt+F4 keys and
					// then trying to open it again. If this is not handled here then
					// this.Dispose(true) is called which leads to the crash.
					HideForm();
					e.Handled = true;
					break;
			}
		}

		/// <summary>
		/// Find the node for the specified Hvo and select it.
		/// Note that this DOES trigger the AfterSelect event.
		/// Enhance JohnT: currently, if the hvo isn't found, it still fires off the AfterSelect event.
		///  - should we (will it work?) set m_treeView.SelectedNode to null so nothing is selected?
		///  - if it's an exception not to find the node, do we need a mechanism...maybe passing zero?...
		///  to explicitly set it to 'nothing selected' (if that's possible).
		/// None of that is needed yet, so we haven't worried about it.
		/// </summary>
		public void SelectObj(int hvo)
		{
			var node = FindNode(Nodes, hvo);
			if (node != null)
			{
				// the normal after select handler will not pass this on because it wasn't from
				// a mouse event, but we want it passed on.
				SelectByAction = TreeViewAction.ByMouse;
				m_treeView.SelectedNode = node;
			}
		}

		/// <summary>
		/// Find the node for the specified Hvo and select it.
		/// Note that this does NOT trigger the BeforeSelect or AfterSelect events.
		/// </summary>
		public void SelectObjWithoutTriggeringBeforeAfterSelects(int hvo)
		{
			var node = FindNode(Nodes, hvo);
			if (node != null)
			{
				// the normal after select handler should not pass this on
				// because it will have been turned off by the caller.
				SelectByAction = TreeViewAction.ByMouse;
				EnableAfterAndBeforeSelectHandling(false);
				m_treeView.SelectedNode = node;
				EnableAfterAndBeforeSelectHandling(true);
			}
		}

		/// <summary>
		/// Assuming a node collection containing HvoTreeNodes, return the one
		/// whose HVO is the specified one. Null if not found.
		/// </summary>
		private TreeNode FindNode(TreeNodeCollection nodes, int hvo)
		{
			TreeNode retVal = null;
			foreach (HvoTreeNode node in nodes)
			{
				if (node.Hvo == hvo)
				{
					retVal = node;
					break;
				}
				retVal = FindNode(node.Nodes, hvo);
				if (retVal != null)
				{
					break;
				}
			}
			return retVal;
		}

		/// <summary>
		/// Find the width that will display the full width of all items.
		/// Note that if the height is set to less than the natural height,
		/// some additional space may be wanted for a scroll bar.
		/// </summary>
		public int NaturalWidth => Width;

		/// <summary>
		/// Find the height that will display the full height of all items.
		/// </summary>
		public int NaturalHeight => Height;

		/// <summary>Message filter for detecting events that may turn off
		/// the insert verse numbers mode</summary>
		private sealed class FwPopupMessageFilter : IMessageFilter, IDisposable
		{
			private PopupTree m_popupTree;

			/// <summary />
			public FwPopupMessageFilter(PopupTree popupTree)
			{
				m_popupTree = popupTree;
			}

			#region IDisposable & Co. implementation
			/// <summary>
			/// See if the object has been disposed.
			/// </summary>
			private bool IsDisposed { get; set; }

			/// <summary>
			/// Finalizer, in case client doesn't dispose it.
			/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
			/// </summary>
			/// <remarks>
			/// In case some clients forget to dispose it directly.
			/// </remarks>
			~FwPopupMessageFilter()
			{
				Dispose(false);
				// The base class finalizer is called automatically.
			}

			/// <summary />
			public void Dispose()
			{
				Dispose(true);
				// This object will be cleaned up by the Dispose method.
				// Therefore, you should call GC.SuppressFinalize to
				// take this object off the finalization queue
				// and prevent finalization code for this object
				// from executing a second time.
				GC.SuppressFinalize(this);
			}

			/// <summary>
			/// Executes in two distinct scenarios.
			///
			/// 1. If disposing is true, the method has been called directly
			/// or indirectly by a user's code via the Dispose method.
			/// Both managed and unmanaged resources can be disposed.
			///
			/// 2. If disposing is false, the method has been called by the
			/// runtime from inside the finalizer and you should not reference (access)
			/// other managed objects, as they already have been garbage collected.
			/// Only unmanaged resources can be disposed.
			/// </summary>
			/// <param name="disposing"></param>
			/// <remarks>
			/// If any exceptions are thrown, that is fine.
			/// If the method is being done in a finalizer, it will be ignored.
			/// If it is thrown by client code calling Dispose,
			/// it needs to be handled by fixing the bug.
			/// </remarks>
			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (disposing)
				{
					// Dispose managed resources here.
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_popupTree = null; // It is disposed of elsewhere.

				IsDisposed = true;
			}

			#endregion IDisposable & Co. implementation

			/// <summary />
			/// <returns>true if the message is consumed, false to pass it on.</returns>
			public bool PreFilterMessage(ref Message m)
			{
				switch ((Win32.WinMsgs)m.Msg)
				{
					case Win32.WinMsgs.WM_NCLBUTTONDOWN:
					case Win32.WinMsgs.WM_NCLBUTTONUP:
					case Win32.WinMsgs.WM_LBUTTONDOWN:
						{
							// Make sure the popuptree hasn't been disposed of with out setting
							// the m_popupTree variable to null:
							if (m_popupTree.IsDisposed)
							{
								return false;   // default case
							}
							// Handle any mouse left button activity.
							// Non-client areas include the title bar, menu bar, window borders,
							// and scroll bars. But the only one in our combo is the scroll bar.
							var c = FromHandle(m.HWnd);
							// Clicking anywhere in an FwListBox, including it's scroll bar,
							// behaves normally.
							if (c == m_popupTree.m_treeView)
							{
								return false;
							}
							// On Mono clicking on the FwListBox Scrollbar causes return from Control.FromHandle
							// to be a ImplicitScrollBar which is a child of the FwListBox.
							if (c is ScrollBar && c.Parent == m_popupTree.m_treeView)
							{
								return false;
							}
							// Any other click is captured and causes the list box to go away.
							// Only do this if the popup tree is visible
							if (m_popupTree.Visible)
							{
								m_popupTree.HideForm();
								return true;
							}
							return false;
						}
					default:
						return false;
				}
			}
		}

		/// <summary>
		/// We need to subclass TreeView in order to override IsInputChar(), otherwise
		/// TreeView will not try to handle TAB keys (cf. LT-2190).
		/// </summary>
		private sealed class FwTreeView : TreeView
		{
			/// <summary />
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}

				base.Dispose(disposing);
			}

			/// <summary>
			/// We need to be able to handle the TAB key.
			/// Requires IsInputKey() == true.
			/// </summary>
			protected override bool IsInputChar(char charCode)
			{
				return charCode == '\t' || base.IsInputChar(charCode);
			}

			/// <summary>
			/// We need to be able to handle the TAB key. IsInputKey() must be true
			/// for IsInputChar() to be called.
			/// </summary>
			protected override bool IsInputKey(Keys keyData)
			{
				return keyData == Keys.Tab || keyData == (Keys.Tab | Keys.Shift) || base.IsInputKey(keyData);
			}

			protected override void WndProc(ref Message m)
			{
				// don't try to handle WM_CHAR in TreeView
				// it causes an annoying beep LT-16007
				const int wmCharMsg = 258;
				if (m.Msg == wmCharMsg)
				{
					return;
				}
				base.WndProc(ref m);
			}
		}
	}
}