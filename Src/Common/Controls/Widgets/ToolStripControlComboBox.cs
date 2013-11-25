// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ToolStripControlComboBox.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A tool strip item similar to a ToolStripComboBox. However, it is configurable what
	/// control to display when dropped down.
	/// </summary>
	/// <remarks>
	/// <para>In the current implementation the "text box" is always read-only.</para>
	/// <para>The ToolStripComboBox uses the Text property and TextChanged event of the
	/// DropDownControl to retrieve the text to display when collapsed.</para>
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	public class ToolStripControlComboBox: ToolStripDropDownItem
	{
		#region Member variables
		private int m_dropDownButtonWidth;
		private int m_minimumWidth = 100;
		private Dictionary<Color, Brush> m_Brushes = new Dictionary<Color, Brush>();
		/// <summary>Contains formatting information on how to display the text</summary>
		private StringFormat m_stringFormat;
		/// <summary><c>true</c> while we are adding a drop down control</summary>
		private bool m_fAddingDropDownControl;

		/// <summary><c>true</c> if the mouse cursor is over our tool strip item</summary>
		private bool m_fHover;

		private bool m_hideDropDownWhenComboTextChanges = true;
		private int m_dropDownHeight = 200;

		#endregion

		#region Construction and Dispose
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ToolStripControlComboBox"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ToolStripControlComboBox()
		{
			m_dropDownButtonWidth = DefaultDropDownButtonWidth;
			m_stringFormat = new StringFormat(StringFormatFlags.NoWrap);
			m_stringFormat.Trimming = StringTrimming.EllipsisPath;
			m_stringFormat.LineAlignment = StringAlignment.Center;
			AutoToolTip = true;

			DropDown.LayoutStyle = ToolStripLayoutStyle.Table;
			DropDown.AutoSize = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases the unmanaged resources used by the
		/// <see cref="T:System.Windows.Forms.ToolStripDropDownItem"/> and optionally releases
		/// the managed resources.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false
		/// to release only unmanaged resources.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			if (disposing)
			{
				foreach (Brush brush in m_Brushes.Values)
					brush.Dispose();
				m_Brushes.Clear();
			}

			base.Dispose(disposing);
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default width of the drop down button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int DefaultDropDownButtonWidth
		{
			// 11 is what ToolStripSplitButton.DefaultDropDownButtonWidth returns
			get { return 11; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the width of the drop down button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Category("Layout")]
		public int DropDownButtonWidth
		{
			get
			{
				return m_dropDownButtonWidth;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException("DropDownButtonWidth",
						"Button width can't be < 0");
				}

				if (m_dropDownButtonWidth != value)
				{
					m_dropDownButtonWidth = value;
					Invalidate();
					if (Owner != null)
						Owner.Invalidate();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the minimum width.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Category("Layout")]
		[DefaultValue(100)]
		public int MinimumWidth
		{
			get { return m_minimumWidth; }
			set { m_minimumWidth = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the drop-down will be hidden when
		/// the text in the combo box is changed while the drop-down is visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------		[Category("Behavior")]
		[Category("behavior")]
		[DefaultValue(true)]
		public bool HideDropDownWhenComboTextChanges
		{
			get { return m_hideDropDownWhenComboTextChanges; }
			set { m_hideDropDownWhenComboTextChanges = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the height of the drop down portion of the combo box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Category("Layout")]
		[DefaultValue(200)]
		public int DropDownHeight
		{
			get { return m_dropDownHeight; }
			set { m_dropDownHeight = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the drop down style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public ComboBoxStyle DropDownStyle
		{
			get { return ComboBoxStyle.DropDownList; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the drop down control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Control DropDownControl
		{
			private get
			{
				if (DropDown.Items.Count == 0)
					return null;
				Debug.Assert(DropDown.Items.Count == 1);
				return ((ToolStripControlHost)DropDown.Items[0]).Control;
			}
			set
			{
				if (value != null)
					value.Parent = null;

				if (DropDown.Items.Count > 0)
				{
					Control control = DropDownControl;
					control.GotFocus -= OnDropDownGotFocus;
					control.LostFocus -= OnDropDownLostFocus;
					control.ParentChanged -= OnDropDownControlParentChanged;
					control.TextChanged -= OnDropDownTextChanged;
				}

				DropDown.Items.Clear();
				if (value == null)
					return;

				m_fAddingDropDownControl = true;
				try
				{
					ToolStripControlHost controlHost = new ToolStripControlHost(value);
					controlHost.Margin = new Padding();
					controlHost.Padding = new Padding();
					controlHost.AutoSize = false;
					DropDown.Items.Add(controlHost);
				}
				finally
				{
					m_fAddingDropDownControl = false;
				}

				value.GotFocus += OnDropDownGotFocus;
				value.LostFocus += OnDropDownLostFocus;
				value.ParentChanged += OnDropDownControlParentChanged;
				value.TextChanged += OnDropDownTextChanged;
				Text = value.Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.ToolStripDropDownItem.DropDownOpened"></see> event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnDropDownOpened(EventArgs e)
		{
			DropDown.Size = new Size(Width, m_dropDownHeight);

			if (DropDownItems.Count > 0 && DropDownItems[0] is ToolStripControlHost &&
				((ToolStripControlHost)DropDownItems[0]).Control != null)
			{
				Point pt = ((ToolStripControlHost)DropDownItems[0]).Control.Location;
				Rectangle rc = DropDown.ClientRectangle;
				rc.Width -= 2;
				rc.Height -= pt.Y + 1;
				DropDownItems[0].Size = rc.Size;
			}

			base.OnDropDownOpened(e);
		}

		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the brush for the given color.
		/// </summary>
		/// <param name="color">The color.</param>
		/// <returns>The desired brush.</returns>
		/// ------------------------------------------------------------------------------------
		private Brush GetBrush(Color color)
		{
			Brush brush;
			if (!m_Brushes.TryGetValue(color, out brush))
			{
				brush = new SolidBrush(color);
				m_Brushes.Add(color, brush);
			}

			return brush;
		}
		#endregion

		#region Event handler
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the DropDown or DropDownControl gets focus.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnDropDownGotFocus(object sender, EventArgs e)
		{
			if (DropDown.Visible)
			{
				if (!DropDownControl.Capture)
				{
					DropDownControl.Capture = true;
					DropDownControl.MouseUp += OnDropDownControlMouseUp;
				}
			}
			else
				Parent.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the DropDown or DropDownControl lost focus.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnDropDownLostFocus(object sender, EventArgs args)
		{
			// If the user clicked somewhere outside of the drop down we want to close it.
			// We don't want to close it if the user clicked on the scrollbar or expanded an
			// item in the tree view.
			if (DropDown.Visible && !DropDown.ContainsFocus)
				HideDropDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the parent of the DropDown control changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnDropDownControlParentChanged(object sender, EventArgs e)
		{
			Control control = DropDownControl;
			if (control != null)
			{
				control.Capture = false;
				control.GotFocus -= OnDropDownGotFocus;
				control.LostFocus -= OnDropDownLostFocus;
				control.ParentChanged -= OnDropDownControlParentChanged;
				control.MouseUp -= OnDropDownControlMouseUp;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called in response to a MouseUp event on the DropDown control.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.MouseEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnDropDownControlMouseUp(object sender, MouseEventArgs e)
		{
			if (!DropDownControl.Focused)
				HideDropDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the text of the drop down control changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnDropDownTextChanged(object sender, EventArgs e)
		{
			if (DropDown.Visible)
			{
				if (m_hideDropDownWhenComboTextChanges)
					HideDropDown();

				Select();
			}

			Text = DropDownControl.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the layout of the toolstrip is completed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnOwnerLayoutCompleted(object sender, EventArgs e)
		{
#if __MonoCS__
			if (Owner == null)
				return;
#endif

			if (Dock == DockStyle.Fill)
			{
				// Try to increase our width so that we occupy the remaining space between
				// this toolstrip item and the next.
				ToolStripItem nextItem = Owner.GetNextItem(this, ArrowDirection.Right);
				if (nextItem != null)
					Width = nextItem.Bounds.Left - Bounds.Left;
			}
		}

		#endregion

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a generic <see cref="T:System.Windows.Forms.ToolStripDropDown"/> for which
		/// events can be defined.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Windows.Forms.ToolStripDropDown"/>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected override ToolStripDropDown CreateDefaultDropDown()
		{
			ToolStripDropDown dropDown = base.CreateDefaultDropDown();
			dropDown.GotFocus += OnDropDownGotFocus;
			dropDown.LostFocus += OnDropDownLostFocus;
			return dropDown;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the
		/// <see cref="T:System.Windows.Forms.ToolStripDropDownItem"/> has
		/// <see cref="T:System.Windows.Forms.ToolStripDropDown"/> controls associated with it.
		/// </summary>
		/// <returns>true if the <see cref="T:System.Windows.Forms.ToolStripDropDownItem"/> has
		/// <see cref="T:System.Windows.Forms.ToolStripDropDown"/> controls; otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		public override bool HasDropDownItems
		{
			get { return DropDown.Items.Count > 0 || m_fAddingDropDownControl; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the size of a rectangular area into which a control can be fit.
		/// </summary>
		/// <param name="constrainingSize">The custom-sized area for a control.</param>
		/// <returns>
		/// A <see cref="T:System.Drawing.Size"/> ordered pair, representing the width and height
		/// of a rectangle.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override Size GetPreferredSize(Size constrainingSize)
		{
			return new Size(Width, Height);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the screen coordinates, in pixels, of the upper-left corner of the
		/// <see cref="T:System.Windows.Forms.ToolStripDropDownItem"/>.
		/// </summary>
		/// <returns>A Point representing the x and y screen coordinates, in pixels.</returns>
		/// ------------------------------------------------------------------------------------
		protected override Point DropDownLocation
		{
			get
			{
				// The location the base implementation returns is one pixel off for our
				// purposes
				Point location = base.DropDownLocation;
				location.Y += 1;
				return location;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.ToolStripItem.Paint"/> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains
		/// the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (Owner == null)
				return;

			// Note: e.Graphics expects coordinates relativ to the upper left corner of the
			// ToolStripItem (i.e. relative to Bounds)!
			Size borderSize = SystemInformation.BorderSize;
			// Draw border around whole ToolStripItem
			using (Pen pen = new Pen(m_fHover || DropDown.Visible ? ForeColor : BackColor))
			{
				e.Graphics.DrawRectangle(pen, new Rectangle(0, Bounds.Top,
					Bounds.Width - borderSize.Width,
					Bounds.Height - 2 * borderSize.Height));

				// Draw border around button
				e.Graphics.DrawRectangle(pen,
					new Rectangle(Bounds.Width - DropDownButtonWidth - 2 * borderSize.Width,
						Bounds.Top,
						DropDownButtonWidth + 2 * borderSize.Width,
						Bounds.Height - 2 * borderSize.Height));

				// Draw the button
				Owner.Renderer.DrawArrow(new ToolStripArrowRenderEventArgs(e.Graphics, this,
					new Rectangle(Bounds.Width - DropDownButtonWidth - borderSize.Width,
						Bounds.Top + borderSize.Height,
						DropDownButtonWidth,
						Bounds.Height - 3 * borderSize.Height),
					ForeColor, ArrowDirection.Down));

				// Draw the text
				Rectangle textRect = new Rectangle(borderSize.Width,
					Bounds.Top + borderSize.Height,
					Bounds.Width - DropDownButtonWidth - 3 * borderSize.Width,
					Bounds.Height - 3 * borderSize.Height);
				e.Graphics.FillRectangle(GetBrush(BackColor), textRect);
				e.Graphics.DrawString(Text, Font, GetBrush(ForeColor),
					new RectangleF(ContentRectangle.Left, ContentRectangle.Top,
						Bounds.Width - DropDownButtonWidth - 3 * borderSize.Width,
						ContentRectangle.Height),
					m_stringFormat);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.ToolStripItem.MouseEnter"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			m_fHover = true;
			Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.ToolStripItem.MouseLeave"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseLeave(EventArgs e)
		{
			m_fHover = false;
			Invalidate();
			base.OnMouseLeave(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.ToolStripItem.Click"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClick(EventArgs e)
		{
			base.OnClick(e);

			if (DropDown.Visible)
				HideDropDown();
			else
				ShowDropDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.ToolStripDropDownItem.DropDownClosed"/>
		/// event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnDropDownClosed(EventArgs e)
		{
			Control control = DropDownControl;
			control.MouseUp -= OnDropDownControlMouseUp;
			control.Capture = false;

			base.OnDropDownClosed(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.ToolStripItem.OwnerChanged"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnOwnerChanged(EventArgs e)
		{
			base.OnOwnerChanged(e);

			if (Owner == null)
				return;

			Owner.LayoutCompleted += OnOwnerLayoutCompleted;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Layout"/> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.LayoutEventArgs"/> that contains
		/// the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLayout(LayoutEventArgs e)
		{
			base.OnLayout(e);

			if (Dock == DockStyle.Fill)
			{
				// We set the width to the minimum width before we do a layout on the toolstrip.
				// After the toolstrip layout we then use all available space.
				Width = MinimumWidth;
			}
		}
		#endregion

		#region Designer related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the width of the drop down button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual void ResetDropDownButtonWidth()
		{
			DropDownButtonWidth = DefaultDropDownButtonWidth;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shoulds the width of the serialize drop down button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal virtual bool ShouldSerializeDropDownButtonWidth()
		{
			return (DropDownButtonWidth != DefaultDropDownButtonWidth);
		}
		#endregion
	}
}
