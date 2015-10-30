// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.Utils; // for Win32 message defns.

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// PopupTree is a form containing a TreeView, designed to pop up like the list of a combo box
	/// to allow a one-time choice from a list.
	/// Although this is currently implemented using the standard TreeView class, it is possible
	/// that a need will arise for items to be rendered using View code, either to support styled
	/// text in items, or to support Graphite rendering. For this reason the TreeView is not
	/// made public, and only a limited subset of its capabilities is currently exposed.
	/// </summary>
	public class PopupTree : Form, IFWDisposable, IDropDownBox
	{
		/// <summary></summary>
		public event TreeViewEventHandler AfterSelect;
		/// <summary></summary>
		public event TreeViewCancelEventHandler BeforeSelect;
		/// <summary></summary>
		public event TreeViewEventHandler PopupTreeClosed;
		private TreeViewAction m_selectedNodeAction;
		private bool m_fClientSpecifiedAction = false;

		internal FwTreeView m_treeView;
		private Control m_tabStopControl = null;	// See comment on TabStopControl property
		private FwPopupMessageFilter m_fwPopupMessageFilter;
		private TreeNode m_tnMouseDown = null;		// Store the node indicated by a MouseDown event.
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private Form m_launchForm;
		private bool m_fShown; // true after Show() completes, prevents spurious Hide during spurious AfterSelect.

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PopupTree()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			this.StartPosition = FormStartPosition.Manual; // allows us to use Location to position it exactly.
			this.Size = this.DefaultSize; // Defeat extraordinary side effect of setting StartPosition.
			EnableAfterAndBeforeSelectHandling(true);
			m_treeView.AccessibleName = "PopupTreeTree";
#if !__MonoCS__ // FWNX-399 - on mono this sidestep/workaround causes problems because it relies on the order event handlers
				// which on mono can be registered in a difference order.
				// possibly a better way to do this workaround if it is still neccessary is to
				// override OnNodeMouseClick.
			// The following two event handlers are needed to sidestep a bug in the AfterSelect
			// processing: the builtin TreeView code does not allow the user to select the
			// highlighted item in the tree, even if it is not really selected, and the TreeView
			// code forces something to be highlighted even when nothing is selected.  So we
			// simulate the Select operation for exactly those conditions: nothing yet selected,
			// or trying to select what has already been selected.
			m_treeView.MouseDown += new MouseEventHandler(m_treeView_MouseDown);
			m_treeView.MouseUp += new MouseEventHandler(m_treeView_MouseUp);
#endif
			m_treeView.KeyDown += new KeyEventHandler(m_treeView_KeyDown);
			this.AccessibleName = "PopupTreeForm";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enables or disables the handlers for the AfterSelect and BeforeSelect events
		/// </summary>
		/// ------------------------------------------------------------------------------------
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
		/// <value>The form.</value>
		public Form Form
		{
			get
			{
				return this;
			}
		}

		/// <summary>
		/// Gets or sets the form the PopupTree is launched from.
		/// </summary>
		public Form LaunchingForm
		{
			get { return m_launchForm; }
			set { m_launchForm = value; }
		}

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
			CheckDisposed();

			m_tnMouseDown = null;
			if (Visible && m_fShown)
			{
				Hide();
				m_fShown = false;
				RemoveFilter();
				if (activateParent && m_launchForm != null)
					m_launchForm.Activate();
				OnHide();	// notify owners of this event.
			}
		}

		// notify our owners that we've hidden the PopupTree.
		private void OnHide()
		{
			if (PopupTreeClosed != null)
				PopupTreeClosed(this, new TreeViewEventArgs(m_treeView.SelectedNode,
															TreeViewAction.Unknown));
		}

		/// <summary>
		/// JohnT: I don't know why we need this, but for some obscure reason, after everything else
		/// happens and we are hidden, we get activated again. Then the main window is not active,
		/// and can't get keyboard events. AArgh! This is a horrible kludge but the best I can find.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			if (!this.Visible)
			{
				if (m_launchForm != null)
					m_launchForm.Activate();
			}
		}

		/// <summary>
		/// Cycle the selection to the next node that begins with the specified text.
		/// (Case insensitive.)
		/// </summary>
		/// <param name="start">the search key</param>
		public void SelectNodeStartingWith(string start)
		{
			CheckDisposed();

			int iPreviousSelected = -1;
			int iStarting = -1;
			int iEnding = -1;
			bool fFound = false;

			// If the new start key matches the start key for the current selection,
			// we'll start our search from there.
			if (m_treeView.SelectedNode != null &&
				m_treeView.SelectedNode.Text.ToLower().StartsWith(start.ToLower()))
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

			iEnding = m_treeView.Nodes.Count - 1;
			fFound = FindAndSelectNodeStartingWith(iStarting, iEnding, start);

			if (!fFound && iStarting != 0)
			{
				// Cycle from the beginning and see if we find a match before our
				// previous (i.e. current) selection.
				iStarting = 0;
				iEnding = iPreviousSelected - 1;
				fFound = FindAndSelectNodeStartingWith(iStarting, iEnding, start);
				// if we don't find a match, return quietly. Nothing else to select.
			}
			return;
		}

		/// <summary>
		/// Select the node whose text starts with the given startKey string (if node exists).
		/// </summary>
		/// <param name="iStarting">starting index</param>
		/// <param name="iEnding">ending index</param>
		/// <param name="startKey">search key</param>
		/// <returns></returns>
		private bool FindAndSelectNodeStartingWith(int iStarting, int iEnding, string startKey)
		{
			bool fFound = false;
			for (int i = iStarting; i <= iEnding; ++i)
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
			CheckDisposed();

			m_treeView.BeginUpdate();
		}

		/// <summary>
		/// End update started with BeginUpdate.
		/// </summary>
		public void EndUpdate()
		{
			CheckDisposed();

			m_treeView.EndUpdate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure we clean out the message filter if not already done.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged (e);
			if (!this.Visible)
				RemoveFilter();
		}

		// Remove your message filter, typically prior to hiding, or because we got hidden.
		internal void RemoveFilter()
		{
			CheckDisposed();

			if (m_fwPopupMessageFilter != null)
			{
				Application.RemoveMessageFilter(m_fwPopupMessageFilter);
				m_fwPopupMessageFilter.Dispose();
				m_fwPopupMessageFilter = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override Size DefaultSize
		{
			get
			{
				return new Size(120, 200);
			}
		}

		/// <summary>
		/// Get/Set the selected node.
		/// </summary>
		public TreeNode SelectedNode
		{
			get
			{
				CheckDisposed();
				return m_treeView.SelectedNode;
			}
			set
			{
				CheckDisposed();

#if __MonoCS__
				// FWNX-267: forcing the creation of the handle to ensure setting SelectedNode generates the AfterSelect event.
				var h = m_treeView.Handle;
#endif
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
				CheckDisposed();
				return m_treeView.Sorted;
			}
			set
			{
				CheckDisposed();

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
				CheckDisposed();

				m_fClientSpecifiedAction = true;
				m_selectedNodeAction = value;
			}
		}

		/// <summary>
		/// Get the main collection of Nodes. Manipulating this is the main way of
		/// adding and removing items from the popup tree.
		/// </summary>
		public TreeNodeCollection Nodes
		{
			get
			{
				CheckDisposed();
				return m_treeView.Nodes;
			}
		}

		/// <summary>
		/// This will be the Control used for tabbing to the next control, since in the context of a TreeCombo
		/// the PopupTree gets created on a separate form than its sibling ComboTextBox which is on the same form
		/// as the other tabstops.
		/// </summary>
		internal Control TabStopControl
		{
			get
			{
				CheckDisposed();
				return m_tabStopControl;
			}
			set
			{
				CheckDisposed();
				m_tabStopControl = value;
			}
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				RemoveFilter(); // Disposes m_fwPopupMessageFilter, among other things.
				if (m_treeView != null)
				{
					EnableAfterAndBeforeSelectHandling(false);
#if !__MonoCS__ // FWNX-399
					m_treeView.MouseDown -= new MouseEventHandler(m_treeView_MouseDown);
					m_treeView.MouseUp -= new MouseEventHandler(m_treeView_MouseUp);
#endif
					m_treeView.KeyDown -= new KeyEventHandler(m_treeView_KeyDown);
					if (!Controls.Contains(m_treeView))
						m_treeView.Dispose();
				}

				if(components != null)
				{
					components.Dispose();
				}
			}
			m_fwPopupMessageFilter = null;
			m_treeView = null;
			m_tnMouseDown = null;
			// m_selectedNodeAction = null; // Can't null it, since it is a value type.
			m_tabStopControl = null;

			base.Dispose( disposing );
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
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_treeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
		{
			if (BeforeSelect != null)
			{
				if (m_fClientSpecifiedAction == true)
					BeforeSelect(this, new TreeViewCancelEventArgs(e.Node, e.Cancel, m_selectedNodeAction));
				else
					BeforeSelect(this, e);
			}
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
				if (m_fClientSpecifiedAction == true)
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
			CheckDisposed();

			//Figure where to put it. First try right below the main combo box.
			// Pathologically the list box may be bigger than the available height. If so shrink it.
			int maxListHeight = Math.Max(launcherBounds.Top - screenBounds.Top,
				screenBounds.Bottom - launcherBounds.Bottom);
			if (Height > maxListHeight)
				Height = maxListHeight;
			// This is the default position right below the launcherBounds.
			Rectangle popupBounds = new Rectangle(launcherBounds.Left, launcherBounds.Bottom, this.Width, this.Height);
			if (screenBounds.Bottom < popupBounds.Bottom)
			{
				// extends below the bottom of the screen. Use a rectangle above instead.
				// We already made sure it will fit in one place or the other.
				popupBounds = new Rectangle(launcherBounds.Left, launcherBounds.Top - this.Height,
					this.Width, this.Height);
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
			this.Location = new Point(popupBounds.Left, popupBounds.Top);
			// Once the launching form has been set, it should never need to be changed.
			// See FWNX-748 for an example of things going wrong (at least on Mono).
			if (m_launchForm == null)
				m_launchForm = Form.ActiveForm;
			Debug.Assert(m_launchForm != this);

#if __MonoCS__ // FWNX-520: avoid a weird mono problem
			this.Show(m_launchForm);
#else
			this.Show();
#endif
			m_fShown = true;
			TreeNode selNode = m_treeView.SelectedNode;
			if (selNode != null)
				selNode.EnsureVisible();

			m_fwPopupMessageFilter = new FwPopupMessageFilter(this);
			Application.AddMessageFilter(m_fwPopupMessageFilter);
			m_treeView.Focus();
			// Enhance JohnT: maybe should do something to ensure that, if there's a
			// selected node, things expand and scroll to show it.
		}

#if !__MonoCS__ // FWNX-399
		/// <summary>
		/// Handle a MouseDown event, recording the selected node if it seems likely that the
		/// TreeView Select operation will ignore it.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_treeView_MouseDown(object sender, MouseEventArgs e)
		{
			TreeNode tn = m_treeView.GetNodeAt(e.X, e.Y);
			if (tn != null  &&
				(e.X >= tn.Bounds.X && e.X <= tn.Bounds.X + tn.Bounds.Width) &&
				(e.Y >= tn.Bounds.Y && e.Y <= tn.Bounds.Y + tn.Bounds.Height))
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
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void m_treeView_MouseUp(object sender, MouseEventArgs e)
		{
			TreeNode tn = m_treeView.GetNodeAt(e.X, e.Y);
			if (tn != null &&
				tn == m_tnMouseDown &&
				(e.X >= tn.Bounds.X && e.X <= tn.Bounds.X + tn.Bounds.Width) &&
				(e.Y >= tn.Bounds.Y && e.Y <= tn.Bounds.Y + tn.Bounds.Height))
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
#endif

		private void HandleTreeItemSelect()
		{
			if (!m_fShown)
				return; // spurious one during load form.
			if (AfterSelect != null)
			{
				// We want to effectively treat this like a mouse click.
				AfterSelect(this, new TreeViewEventArgs(m_treeView.SelectedNode, TreeViewAction.ByMouse));
			}
			HideForm();
		}

		// Alt-down (or alt-up) arrow or Return selects the entry and closes the box.
		// ESC closes the box without selecting it.
		// Tab key selects item and jumpts to the next/previous field.
		private void m_treeView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyData == (Keys.Up | Keys.Alt) ||
				e.KeyData == (Keys.Down | Keys.Alt) ||
				e.KeyData == Keys.Return)
			{
				HandleTreeItemSelect();
				e.Handled = true;
			}
			else if (e.KeyData == Keys.Tab || e.KeyData == (Keys.Tab | Keys.Shift))
			{
				HandleTreeItemSelect();
				if (TabStopControl != null)
				{
					// Send tab key to the TabStopControl so that it can set the focus to the
					// next control.
					Win32.PostMessage(TabStopControl.Handle, (int)Win32.WinMsgs.WM_KEYDOWN,
						(uint)e.KeyCode, 0);
				}
				e.Handled = true;
			}
			else if ( e.KeyData == Keys.Escape ||
					  e.KeyData == (Keys.Alt | Keys.F4) )
			{
				// If we're in a ComboBox, we must handle the Escape key here, otherwise
				// IMessageFilter.PreFilterMessage in FwInnerTextBox will handle it
				// inadvertently forcing the parent dialog to close (cf. LT-2280).
				//
				// LT-9250 Crash while closing the PopupTree by pressing Alt+F4 keys and
				// then trying to open it again. If this is not handled here then
				// this.Dispose(true) is called which leads to the crash.
				HideForm();
				e.Handled = true;
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
		/// <param name="hvo"></param>
		public void SelectObj(int hvo)
		{
			CheckDisposed();

			TreeNode node = FindNode(Nodes, hvo);
			if (node != null)
			{
				// the normal after select handler will not pass this on because it wasn't from
				// a mouse event, but we want it passed on.
				this.SelectByAction = TreeViewAction.ByMouse;
				m_treeView.SelectedNode = node;
			}
		}

		/// <summary>
		/// Find the node for the specified Hvo and select it.
		/// Note that this does NOT trigger the BeforeSelect or AfterSelect events.
		/// </summary>
		/// <param name="hvo"></param>
		public void SelectObjWithoutTriggeringBeforeAfterSelects(int hvo)
		{
			CheckDisposed();

			TreeNode node = FindNode(Nodes, hvo);
			if (node != null)
			{
				// the normal after select handler should not pass this on
				// because it will have been turned off by the caller.
				this.SelectByAction = TreeViewAction.ByMouse;
				EnableAfterAndBeforeSelectHandling(false);
				m_treeView.SelectedNode = node;
				EnableAfterAndBeforeSelectHandling(true);
			}
		}

		/// <summary>
		/// Assuming a node collection containing HvoTreeNodes, return the one
		/// whose HVO is the specified one. Null if not found.
		/// </summary>
		/// <param name="nodes"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		private TreeNode FindNode(TreeNodeCollection nodes, int hvo)
		{
			TreeNode retVal = null;
			foreach(HvoTreeNode node in nodes)
			{
				if (node.Hvo == hvo)
				{
					retVal = node;
					break;
				}
				retVal = FindNode(node.Nodes, hvo);
				if (retVal != null)
					break;
			}
			return retVal;
		}

		/// <summary>
		/// Find the width that will display the full width of all items.
		/// Note that if the height is set to less than the natural height,
		/// some additional space may be wanted for a scroll bar.
		/// </summary>
		public int NaturalWidth
		{
			get
			{
				return this.Width;
			}
		}

		/// <summary>
		/// Find the height that will display the full height of all items.
		/// </summary>
		public int NaturalHeight
		{
			get
			{
				return this.Height;
			}
		}
	}

	/// <summary>
	/// We need to subclass TreeView in order to override IsInputChar(), otherwise
	/// TreeView will not try to handle TAB keys (cf. LT-2190).
	/// </summary>
	internal class FwTreeView : TreeView, IFWDisposable
	{

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// We need to be able to handle the TAB key.
		/// Requires IsInputKey() == true.
		/// </summary>
		/// <param name="charCode"></param>
		/// <returns></returns>
		protected override bool IsInputChar(char charCode)
		{
			if (charCode == '\t')
				return true;
			else
				return base.IsInputChar(charCode);
		}

		/// <summary>
		/// We need to be able to handle the TAB key. IsInputKey() must be true
		/// for IsInputChar() to be called.
		/// </summary>
		/// <param name="keyData"></param>
		/// <returns></returns>
		protected override bool IsInputKey(Keys keyData)
		{
			if (keyData == Keys.Tab || keyData == (Keys.Tab | Keys.Shift))
				return true;
			else
				return base.IsInputKey(keyData);
		}
	}

	/// ------------------------------------------------------------------------------------
	/// <summary>Message filter for detecting events that may turn off
	/// the insert verse numbers mode</summary>
	/// ------------------------------------------------------------------------------------
	internal class FwPopupMessageFilter : IMessageFilter, IFWDisposable
	{
		private PopupTree m_popupTree;

		/// <summary>Constructor for filter object</summary>
		public FwPopupMessageFilter(PopupTree popupTree)
		{
			m_popupTree = popupTree;
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

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

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
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
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_popupTree = null; // It is disposed of elsewhere.

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// --------------------------------------------------------------------------------
		/// <summary></summary>
		/// <param name="m">message to be filtered</param>
		/// <returns>true if the message is consumed, false to pass it on.</returns>
		/// --------------------------------------------------------------------------------
		public bool PreFilterMessage(ref Message m)
		{
			CheckDisposed();

			switch ((Win32.WinMsgs)m.Msg)
			{

			case Win32.WinMsgs.WM_NCLBUTTONDOWN:
			case Win32.WinMsgs.WM_NCLBUTTONUP:
			case Win32.WinMsgs.WM_LBUTTONDOWN:
			{
				// Make sure the popuptree hasn't been disposed of with out setting
				// the m_popupTree variable to null:
				if (m_popupTree.IsDisposed)
					return false;	// default case
				// Handle any mouse left button activity.
				// Non-client areas include the title bar, menu bar, window borders,
				// and scroll bars. But the only one in our combo is the scroll bar.
				Control c = Control.FromHandle(m.HWnd);
				// Clicking anywhere in an FwListBox, including it's scroll bar,
				// behaves normally.
				if ((c == m_popupTree.m_treeView))
					return false;

				// On Mono clicking on the FwListBox Scrollbar causes return from Control.FromHandle
				// to be a ImplicitScrollBar which is a child of the FwListBox.
				if (c is ScrollBar && c.Parent == m_popupTree.m_treeView)
					return false;

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
}
