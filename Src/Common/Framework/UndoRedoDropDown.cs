// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UndoRedoDropDown.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Drop down list box that shows the undoable/redoable actions
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UndoRedoDropDown : UserControl, IFWDisposable
	{
		#region Data members
		private System.Windows.Forms.Label m_NumberOfUndoes;
		private readonly string m_NumberOfActionsSingle;
		private readonly string m_NumberOfActionsPlural;
		private ScrollListBox m_Actions;
		private readonly string m_Cancel;
		private bool m_fIgnoreNextMouseMoved = false;
		#endregion

		#region Delegates and Events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Represents the method that will handle the event that occures when the user clicks
		/// an item in the list box.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="iClicked">The zero-based index of the clicked item.</param>
		/// ------------------------------------------------------------------------------------
		public delegate void ClickEventHandler(object sender, int iClicked);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Occurs when the user clicks an item in the list box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public event ClickEventHandler ItemClick;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UndoRedoDropDown"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public UndoRedoDropDown()
		{
			InitializeComponent();

			m_NumberOfUndoes.BackColor = Color.FromArgb(200, m_NumberOfUndoes.BackColor);
			this.AccessibilityObject.Name = "UndoRedoDropDown";

			// Give the label at the bottom of the list slightly different background from
			// the background color of the rest of the list.
			Color clr = m_NumberOfUndoes.BackColor;
			clr = Color.FromArgb(Math.Abs(clr.R - 15), Math.Abs(clr.G - 15), Math.Abs(clr.B - 15));
			m_NumberOfUndoes.BackColor = clr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UndoRedoDropDown"/> class.
		/// </summary>
		/// <param name="numberOfActionsSingle">A string describing the number of selected
		/// actions when there is only one, e.g. "Undo 1 action" (will have no formatting
		/// instructions)</param>
		/// <param name="numberOfActionsPlural">A string describing the number of selected
		/// actions when there are more than one, e.g. "Undo {0} actions" (will typically have
		/// a placeholder for the number)</param>
		/// <param name="cancel">The string that's shown to cancel the undo/redo</param>
		/// ------------------------------------------------------------------------------------
		public UndoRedoDropDown(string numberOfActionsSingle, string numberOfActionsPlural,
			string cancel): this()
		{
			m_NumberOfActionsPlural = numberOfActionsPlural;
			m_NumberOfActionsSingle = numberOfActionsSingle;
			m_Cancel = cancel;
		}
		#endregion // Constructors

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

		#region Designer generated code
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(UndoRedoDropDown));
			this.m_NumberOfUndoes = new System.Windows.Forms.Label();
			this.m_Actions = new SIL.FieldWorks.Common.Controls.ScrollListBox();
			this.SuspendLayout();
			//
			// m_NumberOfUndoes
			//
			this.m_NumberOfUndoes.AccessibleDescription = resources.GetString("m_NumberOfUndoes.AccessibleDescription");
			this.m_NumberOfUndoes.AccessibleName = resources.GetString("m_NumberOfUndoes.AccessibleName");
			this.m_NumberOfUndoes.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("m_NumberOfUndoes.Anchor")));
			this.m_NumberOfUndoes.AutoSize = ((bool)(resources.GetObject("m_NumberOfUndoes.AutoSize")));
			this.m_NumberOfUndoes.BackColor = System.Drawing.SystemColors.Window;
			this.m_NumberOfUndoes.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("m_NumberOfUndoes.Dock")));
			this.m_NumberOfUndoes.Enabled = ((bool)(resources.GetObject("m_NumberOfUndoes.Enabled")));
			this.m_NumberOfUndoes.Font = ((System.Drawing.Font)(resources.GetObject("m_NumberOfUndoes.Font")));
			this.m_NumberOfUndoes.Image = ((System.Drawing.Image)(resources.GetObject("m_NumberOfUndoes.Image")));
			this.m_NumberOfUndoes.ImageAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("m_NumberOfUndoes.ImageAlign")));
			this.m_NumberOfUndoes.ImageIndex = ((int)(resources.GetObject("m_NumberOfUndoes.ImageIndex")));
			this.m_NumberOfUndoes.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("m_NumberOfUndoes.ImeMode")));
			this.m_NumberOfUndoes.Location = ((System.Drawing.Point)(resources.GetObject("m_NumberOfUndoes.Location")));
			this.m_NumberOfUndoes.Name = "m_NumberOfUndoes";
			this.m_NumberOfUndoes.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("m_NumberOfUndoes.RightToLeft")));
			this.m_NumberOfUndoes.Size = ((System.Drawing.Size)(resources.GetObject("m_NumberOfUndoes.Size")));
			this.m_NumberOfUndoes.TabIndex = ((int)(resources.GetObject("m_NumberOfUndoes.TabIndex")));
			this.m_NumberOfUndoes.Text = resources.GetString("m_NumberOfUndoes.Text");
			this.m_NumberOfUndoes.TextAlign = ((System.Drawing.ContentAlignment)(resources.GetObject("m_NumberOfUndoes.TextAlign")));
			this.m_NumberOfUndoes.Visible = ((bool)(resources.GetObject("m_NumberOfUndoes.Visible")));
			//
			// m_Actions
			//
			this.m_Actions.AccessibleDescription = resources.GetString("m_Actions.AccessibleDescription");
			this.m_Actions.AccessibleName = resources.GetString("m_Actions.AccessibleName");
			this.m_Actions.Anchor = ((System.Windows.Forms.AnchorStyles)(resources.GetObject("m_Actions.Anchor")));
			this.m_Actions.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("m_Actions.BackgroundImage")));
			this.m_Actions.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_Actions.ColumnWidth = ((int)(resources.GetObject("m_Actions.ColumnWidth")));
			this.m_Actions.Dock = ((System.Windows.Forms.DockStyle)(resources.GetObject("m_Actions.Dock")));
			this.m_Actions.Enabled = ((bool)(resources.GetObject("m_Actions.Enabled")));
			this.m_Actions.Font = ((System.Drawing.Font)(resources.GetObject("m_Actions.Font")));
			this.m_Actions.HandleScrolling = false;
			this.m_Actions.HorizontalExtent = ((int)(resources.GetObject("m_Actions.HorizontalExtent")));
			this.m_Actions.HorizontalScrollbar = ((bool)(resources.GetObject("m_Actions.HorizontalScrollbar")));
			this.m_Actions.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("m_Actions.ImeMode")));
			this.m_Actions.IntegralHeight = ((bool)(resources.GetObject("m_Actions.IntegralHeight")));
			this.m_Actions.Location = ((System.Drawing.Point)(resources.GetObject("m_Actions.Location")));
			this.m_Actions.Name = "m_Actions";
			this.m_Actions.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("m_Actions.RightToLeft")));
			this.m_Actions.ScrollAlwaysVisible = ((bool)(resources.GetObject("m_Actions.ScrollAlwaysVisible")));
			this.m_Actions.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
			this.m_Actions.Size = ((System.Drawing.Size)(resources.GetObject("m_Actions.Size")));
			this.m_Actions.TabIndex = ((int)(resources.GetObject("m_Actions.TabIndex")));
			this.m_Actions.Visible = ((bool)(resources.GetObject("m_Actions.Visible")));
			this.m_Actions.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnKeyDown);
			this.m_Actions.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnMouseMoveOrDown);
			this.m_Actions.VScroll += new System.Windows.Forms.ScrollEventHandler(this.OnScroll);
			this.m_Actions.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnMouseUp);
			this.m_Actions.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnMouseMoveOrDown);
			//
			// UndoRedoDropDown
			//
			this.AccessibleDescription = resources.GetString("$this.AccessibleDescription");
			this.AccessibleName = resources.GetString("$this.AccessibleName");
			this.AutoScroll = ((bool)(resources.GetObject("$this.AutoScroll")));
			this.AutoScrollMargin = ((System.Drawing.Size)(resources.GetObject("$this.AutoScrollMargin")));
			this.AutoScrollMinSize = ((System.Drawing.Size)(resources.GetObject("$this.AutoScrollMinSize")));
			this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
			this.Controls.Add(this.m_Actions);
			this.Controls.Add(this.m_NumberOfUndoes);
			this.Enabled = ((bool)(resources.GetObject("$this.Enabled")));
			this.Font = ((System.Drawing.Font)(resources.GetObject("$this.Font")));
			this.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("$this.ImeMode")));
			this.Location = ((System.Drawing.Point)(resources.GetObject("$this.Location")));
			this.Name = "UndoRedoDropDown";
			this.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("$this.RightToLeft")));
			this.Size = ((System.Drawing.Size)(resources.GetObject("$this.Size")));
			this.ResumeLayout(false);

		}
		#endregion

		#region Public methods and properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the height of the list. This should be called after all the items are
		/// added.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AdjustHeight()
		{
			CheckDisposed();

			// Adjust the height of the drop down. We want to show max. 10 items
			int nMaxItems = Math.Min(m_Actions.Items.Count, 10);
			Height = nMaxItems * m_Actions.ItemHeight + m_NumberOfUndoes.Height
				+ 2 * SystemInformation.BorderSize.Height;

			// Always select the first undo/redo action
			if (m_Actions.Items.Count > 0)
				SelectListItems(0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the collection of actions in the list box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ListBox.ObjectCollection Actions
		{
			get
			{
				CheckDisposed();
				return m_Actions.Items;
			}
		}
		#endregion // Public methods and properties

		#region Event handler
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Select all actions to the cursor
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnMouseMoveOrDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (m_fIgnoreNextMouseMoved)
			{
				m_fIgnoreNextMouseMoved = false;
				return;
			}

			int iLastSelected = m_Actions.IndexFromPoint(e.X, e.Y);
			if (iLastSelected > -1)
			{
				SelectListItems(iLastSelected, m_Actions.TopIndex);
			}
			else if (e.Button == MouseButtons.Left)
			{	// User moved mouse outside the list box while holding down left mouse button
				if (m_Actions.DisplayRectangle.Contains(new Point(e.X, m_Actions.Top)))
				{	// above or below list box - select everything that's showing
					if (e.Y < m_Actions.Top)
					{	// scroll up
						m_Actions.VerticalScroll(ScrollEventType.SmallDecrement);
					}
					else
					{	// scroll down
						m_Actions.VerticalScroll(ScrollEventType.SmallIncrement);
					}
					Refresh();
				}
				else
				{	// left or right of list box - cancel
					SelectListItems(-1, m_Actions.TopIndex); // unselect all
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle scroll actions. Update the selection too.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnScroll(object sender, System.Windows.Forms.ScrollEventArgs e)
		{
			if (e.Type == ScrollEventType.EndScroll || e.Type == ScrollEventType.ThumbPosition)
				return;

			int iLastSelected;
			if (e.NewValue == m_Actions.TopIndex)
			{	// nothing changed
				return;
			}
			else if (e.NewValue < m_Actions.TopIndex)
			{	// scroll up
				iLastSelected = e.NewValue;
			}
			else
			{	// scroll down
				iLastSelected = e.NewValue + m_Actions.ItemsPerPage - 1;
				if (iLastSelected >= m_Actions.Items.Count)
					iLastSelected = m_Actions.Items.Count - 1;
			}

			SelectListItems(iLastSelected, e.NewValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User clicked an item
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnMouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left)
				return;

			int iLastSelected = m_Actions.IndexFromPoint(e.X, e.Y);

			Hide();

			if (iLastSelected > -1)
			{
				if (ItemClick != null)
					ItemClick(this, iLastSelected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle cursor up and down keys
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Down:
				{
					if (m_Actions.SelectedIndices.Count >= m_Actions.Items.Count)
						return;

					int iLastSelected = m_Actions.SelectedIndices.Count;
					if (iLastSelected > m_Actions.TopIndex + m_Actions.ItemsPerPage - 1)
					{
						m_Actions.VerticalScroll(ScrollEventType.SmallIncrement);

					}
					else
						SelectListItems(iLastSelected, m_Actions.TopIndex);

					// for some reason changing the selection here causes a MouseMove message
					// to happen which resets the selection. Therefore we set a flag that the
					// next MouseMove event will be ignored.
					if (m_Actions.DisplayRectangle.Contains(
						m_Actions.PointToClient(MousePosition)))
					{
						m_fIgnoreNextMouseMoved = true;
					}
					break;
				}
				case Keys.Up:
				{
					if (m_Actions.SelectedIndices.Count <= 1) // always keep first one selected
						return;

					int topIndex = m_Actions.TopIndex;
					int iLastSelected = m_Actions.SelectedIndices.Count - 2;
					if (iLastSelected < topIndex)
						m_Actions.VerticalScroll(ScrollEventType.SmallDecrement);
					else
						SelectListItems(iLastSelected, topIndex);

					// for some reason changing the selection here causes a MouseMove message
					// to happen which resets the selection. Therefore we set a flag that the
					// next MouseMove event will be ignored.
					if (m_Actions.DisplayRectangle.Contains(
						m_Actions.PointToClient(MousePosition)))
					{
						m_fIgnoreNextMouseMoved = true;
					}
					break;
				}
				case Keys.PageDown:
				{
					// want to select to end of page
					int iLastSelected = m_Actions.SelectedIndices.Count + m_Actions.ItemsPerPage - 2;
					if (iLastSelected > m_Actions.TopIndex + m_Actions.ItemsPerPage - 1)
						m_Actions.VerticalScroll(ScrollEventType.LargeIncrement);
					else
						SelectListItems(iLastSelected, m_Actions.TopIndex);
					break;
				}
				case Keys.PageUp:
				{
					if (m_Actions.SelectedIndices.Count <= 1) // always keep first one selected
						return;

					int topIndex = m_Actions.TopIndex;
					int iLastSelected = m_Actions.SelectedIndices.Count - m_Actions.ItemsPerPage;
					if (iLastSelected < topIndex)
						m_Actions.VerticalScroll(ScrollEventType.LargeDecrement);
					else
						SelectListItems(iLastSelected, topIndex);
					break;
				}
				case Keys.Escape:
					Hide();
					break;
				default:
					break;
			}
		}
		#endregion // Event Handler

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Select the list items from zero through iLastSelected, and update the list text.
		/// </summary>
		/// <param name="iLastSelected"></param>
		/// <param name="topIndex">Current or new top index</param>
		/// ------------------------------------------------------------------------------------
		private void SelectListItems(int iLastSelected, int topIndex)
		{
			if (m_Actions.SelectedIndices.Count == iLastSelected + 1)
				return; // nothing changed - still on the same item

			m_Actions.BeginUpdate();

			// select appropriate items
			if (iLastSelected >= m_Actions.SelectedIndices.Count)
			{
				for (int i = m_Actions.SelectedIndices.Count; i <= iLastSelected; i++)
					m_Actions.SetSelected(i, true);
			}
			else
			{
				for (int i = m_Actions.SelectedIndices.Count - 1; i > iLastSelected; i--)
					m_Actions.SetSelected(i, false);
			}

			// update status text
			if (iLastSelected < 0)
				m_NumberOfUndoes.Text = m_Cancel;
			else if (iLastSelected == 0)
				m_NumberOfUndoes.Text = m_NumberOfActionsSingle;
			else
			{
				m_NumberOfUndoes.Text =
					string.Format(m_NumberOfActionsPlural, iLastSelected + 1);
			}

			m_Actions.EndUpdate();
			m_Actions.TopIndex = topIndex;
		}
		#endregion
	}
}
