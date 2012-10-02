using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Widgets
{
	#region SilButtonColumn class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SilButtonColumn : DataGridViewColumn
	{
		/// <summary></summary>
		public event DataGridViewCellMouseEventHandler ButtonClicked;
		private bool m_showButton = true;
		private string m_buttonText;
		private Font m_buttonFont;
		private int m_buttonWidth = 22;
		private bool m_useComboButtonStyle = false;
		private bool m_drawDefaultComboButtonWidth = true;
		private string m_buttonToolTip;
		private ToolTip m_toolTip;
		private bool m_drawTextWithEllipsisPath = false;
		private bool m_showCellToolTips = true;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SilButtonColumn() : base(new SilButtonCell())
		{
			base.DefaultCellStyle.Font = SystemInformation.MenuFont;
			m_buttonFont = SystemInformation.MenuFont;
			Width = 110;
			HeaderText = string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SilButtonColumn(string name) : this()
		{
			Name = name;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SilButtonColumn(string name, bool showButton) : this()
		{
			Name = name;
			m_showButton = showButton;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the template is always a radion button cell.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override DataGridViewCell CellTemplate
		{
			get { return base.CellTemplate; }
			set
			{
				if (value != null && !value.GetType().IsAssignableFrom(typeof(SilButtonCell)))
					throw new InvalidCastException("Must be a SilButtonCell");

				base.CellTemplate = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Need to save the value of the owning grid's ShowCellToolTips value because we may
		/// change it once in a while.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnDataGridViewChanged()
		{
			base.OnDataGridViewChanged();
			if (DataGridView != null)
				m_showCellToolTips = DataGridView.ShowCellToolTips;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text of the button cells in this column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ButtonText
		{
			get { return m_buttonText; }
			set { m_buttonText = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text of the button cells in this column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Font ButtonFont
		{
			get { return m_buttonFont; }
			set { m_buttonFont = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the width of the button within the column's cells.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ButtonWidth
		{
			get { return m_buttonWidth; }
			set { m_buttonWidth = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to paint a combo box style button
		/// in the column's owned cells. If false, a push button style is drawn.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool UseComboButtonStyle
		{
			get { return m_useComboButtonStyle; }
			set	{ m_useComboButtonStyle = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the combo button's width is
		/// calculated automatically by the system (based on the theme). This value is only
		/// relevant when UseComboButtonStyle is true and visual styles in the OS are
		/// turned on.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DrawDefaultComboButtonWidth
		{
			get { return m_drawDefaultComboButtonWidth; }
			set { m_drawDefaultComboButtonWidth = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to show the button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShowButton
		{
			get { return m_showButton; }
			set
			{
				m_showButton = value;
				if (DataGridView != null)
					DataGridView.InvalidateColumn(Index);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not text in the button cells in the
		/// column will be drawn using ellipsis path string formatting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DrawTextWithEllipsisPath
		{
		  get { return m_drawTextWithEllipsisPath; }
		  set { m_drawTextWithEllipsisPath = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the tooltip text for the buttons in the button cells in this column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ButtonToolTip
		{
			get { return m_buttonToolTip; }
			set
			{
				if (m_toolTip != null)
				{
					m_toolTip.Dispose();
					m_toolTip = null;
				}

				m_buttonToolTip = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void ShowToolTip()
		{
			if ((m_toolTip != null && m_toolTip.Active) ||
				string.IsNullOrEmpty(m_buttonToolTip) ||
				DataGridView == null ||	DataGridView.FindForm() == null)
			{
				return;
			}

			if (m_toolTip == null)
				m_toolTip = new ToolTip();

			DataGridView.ShowCellToolTips = false;
			Size sz = SystemInformation.CursorSize;
			Point pt = DataGridView.FindForm().PointToClient(DataGridView.MousePosition);
			pt.X += (int)(sz.Width * 0.6);
			pt.Y += sz.Height;
			m_toolTip.Active = true;
			m_toolTip.Show(m_buttonToolTip, DataGridView.FindForm(), pt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void HideToolTip()
		{
			DataGridView.ShowCellToolTips = m_showCellToolTips;

			if (m_toolTip != null)
			{
				m_toolTip.Hide(DataGridView);
				m_toolTip.Active = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides a way for an owned cell to fire the button clicked event on the column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void InvokeButtonClick(DataGridViewCellMouseEventArgs e)
		{
			if (ButtonClicked != null)
				ButtonClicked(DataGridView, e);
		}
	}

	#endregion

	#region SilButtonCell class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SilButtonCell : DataGridViewTextBoxCell
	{
		private bool m_mouseOverButton = false;
		private bool m_mouseDownOnButton = false;
		private bool m_enabled = true;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Repaint the cell when it's enabled property changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Enabled
		{
			get { return m_enabled; }
			set
			{
				m_enabled = value;
				DataGridView.InvalidateCell(this);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cell's owning SilButtonColumn.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SilButtonColumn OwningButtonColumn
		{
			get
			{
				if (DataGridView == null || ColumnIndex < 0 || ColumnIndex >=
					DataGridView.Columns.Count)
				{
					return null;
				}

				return (DataGridView.Columns[ColumnIndex] as SilButtonColumn);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the cell's button should be shown.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShowButton
		{
			get { return InternalShowButton(RowIndex); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the cell's button should be shown for the
		/// specified row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool InternalShowButton(int rowIndex)
		{
			bool owningColShowValue =
				(OwningButtonColumn != null && OwningButtonColumn.ShowButton);

			DataGridViewRow row = DataGridView.CurrentRow;

			return (owningColShowValue && rowIndex >= 0 && row != null &&
				row.Index == rowIndex && rowIndex != DataGridView.NewRowIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the specified point is over the cell's
		/// radio button. The relativeToCell flag is true when the specified point's origin
		/// is relative to the upper right corner of the cell. When false, it's assumed the
		/// point's origin is relative to the cell's owning grid control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsPointOverButton(Point pt, bool relativeToCell)
		{
			// Get the rectangle for the radion button area.
			Rectangle rc = DataGridView.GetCellDisplayRectangle(ColumnIndex, RowIndex, false);
			Rectangle rcrb;
			Rectangle rcText;
			GetRectangles(rc, out rcrb, out rcText);

			if (relativeToCell)
			{
				// Set the button's rectangle location
				// relative to the cell instead of the grid.
				rcrb.X -= rc.X;
				rcrb.Y -= rc.Y;
			}

			return rcrb.Contains(pt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseLeave(int rowIndex)
		{
			base.OnMouseLeave(rowIndex);
			m_mouseOverButton = false;
			DataGridView.InvalidateCell(this);
			ManageButtonToolTip();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseMove(DataGridViewCellMouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (!m_enabled)
				return;

			if (!IsPointOverButton(e.Location, true) && m_mouseOverButton)
			{
				m_mouseOverButton = false;
				DataGridView.InvalidateCell(this);
				ManageButtonToolTip();
			}
			else if (IsPointOverButton(e.Location, true) && !m_mouseOverButton)
			{
				m_mouseOverButton = true;
				DataGridView.InvalidateCell(this);
				ManageButtonToolTip();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Monitor when the mouse button goes down over the button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseDown(DataGridViewCellMouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (m_mouseOverButton && !m_mouseDownOnButton)
			{
				m_mouseDownOnButton = true;
				DataGridView.InvalidateCell(this);
				ManageButtonToolTip();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Monitor when the user releases the mouse button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(DataGridViewCellMouseEventArgs e)
		{
			m_mouseDownOnButton = false;
			base.OnMouseUp(e);
			DataGridView.InvalidateCell(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseClick(DataGridViewCellMouseEventArgs e)
		{
			if (!IsPointOverButton(e.Location, true) || !ShowButton || IsInEditMode)
			{
				base.OnMouseClick(e);
				return;
			}

			SilButtonColumn col =
				DataGridView.Columns[ColumnIndex] as SilButtonColumn;

			if (col != null)
				col.InvokeButtonClick(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ManageButtonToolTip()
		{
			if (OwningButtonColumn != null && m_mouseOverButton && !m_mouseDownOnButton)
				OwningButtonColumn.ShowToolTip();
			else
				OwningButtonColumn.HideToolTip();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paint the cell with a radio button and it's text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Paint(Graphics g, Rectangle clipBounds,
			Rectangle bounds, int rowIndex, DataGridViewElementStates state,
			object value, object formattedValue, string errorText, DataGridViewCellStyle style,
			DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts parts)
		{
			bool useEllipsisPath = (OwningButtonColumn != null &&
				OwningButtonColumn.DrawTextWithEllipsisPath);

			if (!InternalShowButton(rowIndex) && !useEllipsisPath)
			{
				base.Paint(g, clipBounds, bounds, rowIndex, state, value,
					formattedValue, errorText, style, advancedBorderStyle, parts);

				return;
			}

			// Draw default everything but text.
			parts &= ~DataGridViewPaintParts.ContentForeground;
			base.Paint(g, clipBounds, bounds, rowIndex, state, value,
				formattedValue, errorText, style, advancedBorderStyle, parts);

			// Get the rectangles for the two parts of the cell.
			Rectangle rcbtn;
			Rectangle rcText;
			GetRectangles(bounds, out rcbtn, out rcText, rowIndex);
			DrawButton(g, rcbtn, rowIndex);
			DrawCellText(g, value as string, style, rcText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the button in the cell.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawButton(Graphics g, Rectangle rcbtn, int rowIndex)
		{
			if (!InternalShowButton(rowIndex))
				return;

			bool paintComboButton = (OwningButtonColumn == null ? false :
				OwningButtonColumn.UseComboButtonStyle);

			VisualStyleElement element = (paintComboButton ?
				GetVisualStyleComboButton() : GetVisualStylePushButton());

			if (PaintingHelper.CanPaintVisualStyle(element))
			{
				VisualStyleRenderer renderer = new VisualStyleRenderer(element);

				if (!OwningButtonColumn.DrawDefaultComboButtonWidth)
					renderer.DrawBackground(g, rcbtn);
				else
				{
					Rectangle rc = rcbtn;
					Size sz = renderer.GetPartSize(g, rc, ThemeSizeType.True);
					rc.Width = sz.Width + 2;
					rc.X = (rcbtn.Right - rc.Width);
					renderer.DrawBackground(g, rc);
				}
			}
			else
			{
				ButtonState state = (m_mouseDownOnButton && m_mouseOverButton && m_enabled ?
					ButtonState.Pushed : ButtonState.Normal);

				if (!m_enabled)
					state |= ButtonState.Inactive;

				if (paintComboButton)
					ControlPaint.DrawComboButton(g, rcbtn, state);
				else
					ControlPaint.DrawButton(g, rcbtn, state);
			}

			string buttonText = (OwningButtonColumn == null ? null : OwningButtonColumn.ButtonText);
			if (string.IsNullOrEmpty(buttonText))
				return;

			Font buttonFont = (OwningButtonColumn == null ?
				SystemInformation.MenuFont : OwningButtonColumn.ButtonFont);

			// Draw text
			TextFormatFlags flags = TextFormatFlags.HorizontalCenter |
				TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
				TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis |
				TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping;

			Color clrText = (m_enabled ? SystemColors.ControlText : SystemColors.GrayText);
			TextRenderer.DrawText(g, buttonText, buttonFont, rcbtn, clrText, flags);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the cell's text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawCellText(Graphics g, string text, DataGridViewCellStyle style,
			Rectangle rcText)
		{
			if (string.IsNullOrEmpty(text))
				return;

			// Determine the text's proper foreground color.
			Color clrText = SystemColors.GrayText;
			if (m_enabled && DataGridView != null)
				clrText = (Selected ? style.SelectionForeColor : style.ForeColor);

			bool useEllipsisPath = (OwningButtonColumn != null &&
				OwningButtonColumn.DrawTextWithEllipsisPath);

			TextFormatFlags flags = TextFormatFlags.LeftAndRightPadding |
				TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
				TextFormatFlags.NoPrefix | (useEllipsisPath ?
				TextFormatFlags.PathEllipsis : TextFormatFlags.EndEllipsis) |
				TextFormatFlags.PreserveGraphicsClipping;

			TextRenderer.DrawText(g, text, style.Font, rcText, clrText, flags);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the correct visual style push button given the state of the cell.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private VisualStyleElement GetVisualStylePushButton()
		{
			VisualStyleElement element = VisualStyleElement.Button.PushButton.Normal;

			if (!m_enabled)
				element = VisualStyleElement.Button.PushButton.Disabled;
			else if (m_mouseOverButton)
			{
				element = (m_mouseDownOnButton ?
					VisualStyleElement.Button.PushButton.Pressed :
					VisualStyleElement.Button.PushButton.Hot);
			}

			return element;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the correct visual style combo button given the state of the cell.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private VisualStyleElement GetVisualStyleComboButton()
		{
			VisualStyleElement element = VisualStyleElement.ComboBox.DropDownButton.Normal;

			if (!m_enabled)
				element = VisualStyleElement.ComboBox.DropDownButton.Disabled;
			else if (m_mouseOverButton)
			{
				element = (m_mouseDownOnButton ?
					VisualStyleElement.ComboBox.DropDownButton.Pressed :
					VisualStyleElement.ComboBox.DropDownButton.Hot);
			}

			return element;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the rectangle for the radio button and the text, given the specified cell
		/// bounds.
		/// </summary>
		/// <param name="bounds">The rectangle of the entire cell.</param>
		/// <param name="rcbtn">The returned rectangle for the button.</param>
		/// <param name="rcText">The returned rectangle for the text.</param>
		/// ------------------------------------------------------------------------------------
		public void GetRectangles(Rectangle bounds, out Rectangle rcbtn, out Rectangle rcText)
		{
			GetRectangles(bounds, out rcbtn, out rcText, RowIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the rectangle for the radio button and the text, given the specified cell
		/// bounds.
		/// </summary>
		/// <param name="bounds">The rectangle of the entire cell.</param>
		/// <param name="rcbtn">The returned rectangle for the button.</param>
		/// <param name="rcText">The returned rectangle for the text.</param>
		/// <param name="rowIndex">The index of the row.</param>
		/// ------------------------------------------------------------------------------------
		public void GetRectangles(Rectangle bounds, out Rectangle rcbtn, out Rectangle rcText, int rowIndex)
		{
			if (!InternalShowButton(rowIndex))
			{
				rcbtn = Rectangle.Empty;
				rcText = bounds;
				return;
			}

			int buttonWidth = (OwningButtonColumn == null ? 22 : OwningButtonColumn.ButtonWidth);
			bool paintComboButton = (OwningButtonColumn == null ? false :
				OwningButtonColumn.UseComboButtonStyle);

			if (paintComboButton)
				buttonWidth += 2;

			rcText = bounds;
			rcText.Width -= buttonWidth;

			rcbtn = bounds;
			rcbtn.Width = buttonWidth;
			rcbtn.X = bounds.Right - buttonWidth - 1;
			rcbtn.Y--;

			if (paintComboButton)
			{
				rcbtn.Width -= 2;
				rcbtn.Height -= 3;
				rcbtn.X++;
				rcbtn.Y += 2;
			}
		}
	}

	#endregion
}
