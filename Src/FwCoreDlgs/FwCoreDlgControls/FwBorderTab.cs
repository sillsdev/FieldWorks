// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwBorderTab.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Diagnostics;

using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Control for the Border tab on the styles dialog
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FwBorderTab : UserControl, IFWDisposable, IStylesTab
	{
		#region Data Members
		/// <summary>
		/// Fires when a change is made on the font tab to an unspecified state.
		/// </summary>
		public event EventHandler ChangedToUnspecified;

		private int dyxGapBetweenLeftCheckboxAndPreviewPane;
		private bool m_dontUpdateInheritance = true;
		private bool m_DefaultTextDirectionRtoL = false;
		private bool m_fShowBiDiLabels = false;
		private bool m_fIgnoreCascadingEvents;
		private static int[] s_borderSizes = new int[] { 0, 250, 500, 750, 1000, 1500, 2250, 3000, 4500, 6000 };

		private StyleInfo m_currentStyleInfo;
		#endregion

		#region Construction and demolition
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwBorderTab"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwBorderTab()
		{
			InitializeComponent();
			m_cboColor.ColorPicked += new EventHandler(m_cboColor_ColorPicked);
			dyxGapBetweenLeftCheckboxAndPreviewPane = m_pnlBorderPreview.Left - m_chkLeft.Right;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}
		#endregion

		#region Custom draw methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Paint event of the m_pnlBorderPreview control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.PaintEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_pnlBorderPreview_Paint(object sender, PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			int borderWidth = CalcBorderWidth(m_cboWidth.AdjustedSelectedIndex, g);

			Rectangle drawRect = m_pnlBorderPreview.ClientRectangle;

			// shrink the rectangle to make some border space in the control
			drawRect.Inflate(-7, -7);

			// top left tick mark
			const int tickSize = 7;
			DrawTickMarks(drawRect, tickSize, g);
			drawRect.Inflate(-(tickSize - 1), -(tickSize - 1));

			// Draw the borders only if a width is specified
			DrawBorders(ref drawRect, borderWidth, g);

			// leave some margin space between the border and the preview lines
			drawRect.Inflate(-3, -3);

			// Draw "text" lines in the remaining space
			DrawTextLines(drawRect, g);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw the background of the items in the border width combo box
		/// </summary>
		/// <remarks></remarks>
		/// ------------------------------------------------------------------------------------
		private void m_cboWidth_DrawItemBackground(object sender, DrawItemEventArgs e)
		{
			// fill the background
			e.Graphics.FillRectangle(SystemBrushes.Window, e.Bounds);

			// If needed, draw a selection box around the item
			if ((e.State & DrawItemState.Selected) != 0)
				DrawRectangle(e.Graphics, e.Bounds, SystemPens.Highlight, 2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw the foreground of the items in the border width combo box
		/// </summary>
		/// <remarks></remarks>
		/// ------------------------------------------------------------------------------------
		private void m_cboWidth_DrawItemForeground(object sender, DrawItemEventArgs e)
		{
			Rectangle drawRect = e.Bounds;

			// draw the text representation
			string text = (string)m_cboWidth.Items[e.Index];
			Color textColor = e.ForeColor;
			if ((e.State & DrawItemState.Selected) != 0)
			{
				textColor = ((e.State & DrawItemState.ComboBoxEdit) == 0) ?
					SystemColors.WindowText : m_cboWidth.ForeColor;
			}

			RectangleF textRect = new RectangleF(drawRect.X + 1, drawRect.Y + 1, drawRect.Width - 2, drawRect.Height - 2);
			e.Graphics.DrawString(text, e.Font, new SolidBrush(textColor), textRect);

			// draw the graphic line representing the width. Don't draw for
			// the "unspecified" index
			int index = m_currentStyleInfo.Inherits ? e.Index : e.Index + 1;
			if (index != 0)
			{
				int sampleHeight = CalcBorderWidth(index, e.Graphics);
				e.Graphics.FillRectangle(new SolidBrush(m_cboColor.ColorValue),
					e.Bounds.X + (e.Bounds.Width * 4 / 10), e.Bounds.Y + (e.Bounds.Height - sampleHeight) / 2,
					(e.Bounds.Width * 6 / 10) - 3, sampleHeight);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw the None and All buttons
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void NoneAll_Paint(object sender, PaintEventArgs e)
		{
			Button button = sender as Button;
			Debug.Assert(button != null);

			Rectangle drawRect = button.ClientRectangle;
			Graphics g = e.Graphics;

			// draw the border and background of the button to look like
			// a text box.
			if (Application.RenderWithVisualStyles)
			{
				VisualStyleElement element = VisualStyleElement.TextBox.TextEdit.Normal;
				VisualStyleRenderer renderer = new VisualStyleRenderer(element);
				renderer.DrawBackground(g, drawRect);
			}
			else
			{
				g.FillRectangle(SystemBrushes.Window, drawRect);
				ControlPaint.DrawBorder3D(g, drawRect, Border3DStyle.Sunken);
			}

			// Allow some border space.
			drawRect.Inflate(-2, -2);

			// If the panel has focus then draw the focus rectangle
			if (button.Focused)
				ControlPaint.DrawFocusRectangle(g, drawRect);
			drawRect.Inflate(-2, -2);

			// If the button is selected then draw a selection rectangle
			if (ButtonSelected(button))
				DrawRectangle(g, drawRect, SystemPens.Highlight, 2);
			drawRect.Inflate(-3, -3);

			// draw a border box on the "all" button
			if (button == m_btnAll)
				DrawRectangle(g, drawRect, SystemPens.WindowText, 1);
			drawRect.Inflate(-3, -3);

			// draw some text lines to fill in the remaining space
			DrawTextLines(drawRect, g);
		}
		#endregion

		#region Public Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether default text direction is Right-toLeft or
		/// not. When this value changes, the preview and certain controls are adjusted
		/// accordingly.
		/// </summary>
		/// <remarks>Typically this is the default direction of the view from which this dialog
		/// is invoked.</remarks>
		/// ------------------------------------------------------------------------------------
		public bool DefaultTextDirectionRtoL
		{
			get
			{
				CheckDisposed();
				if (m_currentStyleInfo == null)
					return m_DefaultTextDirectionRtoL;

				return m_currentStyleInfo.DirectionIsRightToLeft == TriStateBool.triNotSet ?
					m_DefaultTextDirectionRtoL :
					m_currentStyleInfo.DirectionIsRightToLeft == TriStateBool.triTrue;
			}
			set
			{
				CheckDisposed();
				m_DefaultTextDirectionRtoL = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether to show labels that are meaningful for both left-to-right and
		/// right-to-left. If this value is false, then simple "Left" and "Right" labels will be
		/// used in the display, rather than "Leading" and "Trailing".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShowBiDiLabels
		{
			set
			{
				CheckDisposed();
				m_fShowBiDiLabels = value;
				ChangeDirectionLabels(value);
			}
		}
		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes the direction labels to show leading/trailing or left/right.
		/// </summary>
		/// <param name="fShowLeadingTrailing"><c>true</c> to show leading/trailing,
		/// <c>false</c> to show left/right</param>
		/// ------------------------------------------------------------------------------------
		private void ChangeDirectionLabels(bool fShowLeadingTrailing)
		{
			if (fShowLeadingTrailing)
			{
				m_chkLeft.Text = FwCoreDlgControls.kstidLeadingCheck;
				m_chkRight.Text = FwCoreDlgControls.kstidTrailingCheck;
			}
			else
			{
				m_chkLeft.Text = FwCoreDlgControls.kstidLeftCheck;
				m_chkRight.Text = FwCoreDlgControls.kstidRightCheck;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Position the checkboxes on either side the preview pane so the labels don't overlap
		/// the preview and the original amount of spacing between these controls is preserved.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AdjustCheckboxPositionsToPreserveGap()
		{
			// Determine which checkbox is currently located on each side of the preview panel
			// If right-to-left, then the trailing (i.e., "right") checkbox is actually on the
			// left.
			CheckBox chkLeftSide = (DefaultTextDirectionRtoL) ? m_chkRight : m_chkLeft;
			CheckBox chkRightSide = (DefaultTextDirectionRtoL) ? m_chkLeft : m_chkRight;

			chkLeftSide.Left = m_pnlBorderPreview.Left -
				dyxGapBetweenLeftCheckboxAndPreviewPane - chkLeftSide.Width;
			chkRightSide.Left = m_pnlBorderPreview.Right +
				dyxGapBetweenLeftCheckboxAndPreviewPane;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the color to use for painting the foreground of a control which displays an
		/// inheritable property value.
		/// </summary>
		/// <param name="prop">The inheritable property.</param>
		/// <returns>The system gray color if the property is inherited; otherwise the normal
		/// window text color.</returns>
		/// ------------------------------------------------------------------------------------
		private Color GetCtrlForeColorForProp<T>(InheritableStyleProp<T> prop)
		{
			return (prop.IsInherited && m_currentStyleInfo.Inherits) ?
				SystemColors.GrayText : SystemColors.WindowText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates the width of the border based on the selected index of the width combo.
		/// This is based on the actual thickness of the line, but we add 1 because the small
		/// point sizes will round to 0.
		/// </summary>
		/// <param name="index">The index of the selected item in the width combo</param>
		/// <param name="g">graphics object to use for DPI info</param>
		/// <returns>border width in pixels</returns>
		/// ------------------------------------------------------------------------------------
		private int CalcBorderWidth(int index, Graphics g)
		{
			if (index < 0)
				index = 0;
			return (int)(s_borderSizes[index] * (int)g.DpiY / 72000) + 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the text lines inside the borders
		/// </summary>
		/// <param name="drawRect">Rectangle to draw in</param>
		/// <param name="g">graphics object to draw with</param>
		/// ------------------------------------------------------------------------------------
		private void DrawTextLines(Rectangle drawRect, Graphics g)
		{
			const int lineHeight = 5;
			const int lineSpacing = 2;
			bool firstLine = true;
			while (drawRect.Height > lineHeight)
			{
				Rectangle lineRect = new Rectangle(drawRect.X, drawRect.Y, drawRect.Width, lineHeight);

				// for the first line, indent the left edge
				if (firstLine)
				{
					lineRect.X += 10;
					lineRect.Width -= 10;
					firstLine = false;
				}

				// for the last line, indent the right edge
				if (drawRect.Height <= (lineHeight * 2) + lineSpacing)
					lineRect.Width -= 10;

				g.FillRectangle(SystemBrushes.GrayText, lineRect);
				drawRect.Y += lineHeight + lineSpacing;
				drawRect.Height -= lineHeight + lineSpacing;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the tick marks on the border preview panel.
		/// </summary>
		/// <param name="drawRect">The draw rect.</param>
		/// <param name="tickSize">Size of the tick.</param>
		/// <param name="g">The g.</param>
		/// ------------------------------------------------------------------------------------
		private void DrawTickMarks(Rectangle drawRect, int tickSize, Graphics g)
		{
			// draw the top left tick mark
			g.DrawLine(SystemPens.WindowText,
				drawRect.X + tickSize - 1, drawRect.Y,
				drawRect.X + tickSize - 1, drawRect.Y + tickSize - 1);
			g.DrawLine(SystemPens.WindowText,
				drawRect.X, drawRect.Y + tickSize - 1,
				drawRect.X + tickSize - 1, drawRect.Y + tickSize - 1);

			// draw the top right tick mark
			g.DrawLine(SystemPens.WindowText,
				drawRect.Right - tickSize, drawRect.Y,
				drawRect.Right - tickSize, drawRect.Y + tickSize - 1);
			g.DrawLine(SystemPens.WindowText,
				drawRect.Right - tickSize, drawRect.Y + tickSize - 1,
				drawRect.Right, drawRect.Y + tickSize - 1);

			// draw the bottom left tick mark
			g.DrawLine(SystemPens.WindowText,
				drawRect.X + tickSize - 1, drawRect.Bottom,
				drawRect.X + tickSize - 1, drawRect.Bottom - tickSize);
			g.DrawLine(SystemPens.WindowText,
				drawRect.X, drawRect.Bottom - tickSize,
				drawRect.X + tickSize - 1, drawRect.Bottom - tickSize);

			// draw the bottom right tick mark
			g.DrawLine(SystemPens.WindowText,
				drawRect.Right - tickSize, drawRect.Bottom,
				drawRect.Right - tickSize, drawRect.Bottom - tickSize);
			g.DrawLine(SystemPens.WindowText,
				drawRect.Right - tickSize, drawRect.Bottom - tickSize,
				drawRect.Right, drawRect.Bottom - tickSize);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw the borders
		/// </summary>
		/// <param name="drawRect">Rectangle to draw the borders in. It will be adjusted to
		/// remove the border space as each border is drawn.</param>
		/// <param name="borderWidth">Width of the border.</param>
		/// <param name="g">graphics object to use</param>
		/// ------------------------------------------------------------------------------------
		private void DrawBorders(ref Rectangle drawRect, int borderWidth, Graphics g)
		{
			using (SolidBrush brush = new SolidBrush(m_cboColor.ColorValue))
			{
				bool fLeadingBorderOn = (m_chkLeft.CheckState == CheckState.Checked ||
					(m_currentStyleInfo.BorderThickness.Value.Leading > 0 &&
					m_chkLeft.CheckState == CheckState.Indeterminate));
				bool fTrailingBorderOn = (m_chkRight.CheckState == CheckState.Checked ||
						(m_currentStyleInfo.BorderThickness.Value.Trailing > 0 &&
						m_chkRight.CheckState == CheckState.Indeterminate));

				// Draw the left border
				if ((fLeadingBorderOn && !DefaultTextDirectionRtoL) ||
						(fTrailingBorderOn && DefaultTextDirectionRtoL))
				{
					g.FillRectangle(brush, drawRect.X, drawRect.Y, borderWidth, drawRect.Height);
					drawRect.X += borderWidth;
					drawRect.Width -= borderWidth;
				}

				// Draw the top border
				if (m_chkTop.CheckState == CheckState.Checked ||
						(m_currentStyleInfo.BorderThickness.Value.Top > 0 &&
						m_chkTop.CheckState == CheckState.Indeterminate))
				{
					g.FillRectangle(brush, drawRect.X, drawRect.Y, drawRect.Width, borderWidth);
					drawRect.Y += borderWidth;
					drawRect.Height -= borderWidth;
				}

				// Draw the right border
				if ((fLeadingBorderOn && DefaultTextDirectionRtoL) ||
						(fTrailingBorderOn && !DefaultTextDirectionRtoL))
				{
					g.FillRectangle(brush, drawRect.Right - borderWidth, drawRect.Y, borderWidth, drawRect.Height);
					drawRect.Width -= borderWidth;
				}

				// Draw the bottom border
				if (m_chkBottom.CheckState == CheckState.Checked ||
						(m_currentStyleInfo.BorderThickness.Value.Bottom > 0 &&
						m_chkBottom.CheckState == CheckState.Indeterminate))
				{
					g.FillRectangle(brush, drawRect.X, drawRect.Bottom - borderWidth, drawRect.Width, borderWidth);
					drawRect.Height -= borderWidth;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw a rectangle. The Graphics.DrawRect method does not draw at the exact location
		/// so this method fixes that.
		/// </summary>
		/// <param name="g">Graphics object to use</param>
		/// <param name="drawRect">rect to draw</param>
		/// <param name="pen">The pen to draw with</param>
		/// <param name="width">The width of the rectangle</param>
		/// ------------------------------------------------------------------------------------
		private void DrawRectangle(Graphics g, Rectangle drawRect, Pen pen, int width)
		{
			while (width-- > 0)
			{
				g.DrawRectangle(pen, drawRect.X, drawRect.Y, drawRect.Width - 1, drawRect.Height - 1);
				drawRect.Inflate(-1, -1);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the button is selected. This is determined based on the checked state
		/// of the border check boxes.
		/// </summary>
		/// <param name="button">button</param>
		/// <returns>true if it is selected, else false</returns>
		/// ------------------------------------------------------------------------------------
		private bool ButtonSelected(Button button)
		{
			if (button == m_btnAll)
			{
				return m_chkBottom.CheckState == CheckState.Checked &&
					m_chkLeft.CheckState == CheckState.Checked &&
					m_chkRight.CheckState == CheckState.Checked &&
					m_chkTop.CheckState == CheckState.Checked;
			}

			return m_chkBottom.CheckState == CheckState.Unchecked &&
				m_chkLeft.CheckState == CheckState.Unchecked &&
				m_chkRight.CheckState == CheckState.Unchecked &&
				m_chkTop.CheckState == CheckState.Unchecked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified control value is inherited.
		/// </summary>
		/// <param name="c">The control</param>
		/// <returns>true if the specified control is inherited; otherwise, false</returns>
		/// ------------------------------------------------------------------------------------
		private bool IsInherited(Control c)
		{
			if (!m_currentStyleInfo.Inherits)
				return false;

			if (c == m_cboWidth)
				return m_cboWidth.IsInherited;

			return c.ForeColor.ToArgb() != SystemColors.WindowText.ToArgb();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the controls.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateControls(object sender)
		{
			if (!m_dontUpdateInheritance && sender != null)
			{
				((Control)sender).ForeColor = SystemColors.WindowText;

				if (IsInherited((Control)sender) && ChangedToUnspecified != null)
					ChangedToUnspecified(this, EventArgs.Empty);
			}

			m_pnlBorderPreview.Refresh();
			m_btnAll.Refresh();
			m_btnNone.Refresh();
			m_cboWidth.Refresh();
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the form based on a style being selected.
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// ------------------------------------------------------------------------------------
		public void UpdateForStyle(StyleInfo styleInfo)
		{
			CheckDisposed();

			m_dontUpdateInheritance = true;
			m_fIgnoreCascadingEvents = true;
			m_currentStyleInfo = styleInfo;

			m_cboColor.ForeColor = GetCtrlForeColorForProp(styleInfo.IBorderColor);
			m_cboColor.IsInherited = styleInfo.Inherits;

			m_cboColor.ColorValue = styleInfo.BorderColor;

			bool fWidthInherited = styleInfo.BorderThickness.IsInherited && styleInfo.Inherits;
			m_chkTop.ThreeState = fWidthInherited;
			m_chkRight.ThreeState = fWidthInherited;
			m_chkBottom.ThreeState = fWidthInherited;
			m_chkLeft.ThreeState = fWidthInherited;
			if (fWidthInherited)
			{
				m_chkBottom.CheckState = CheckState.Indeterminate;
				m_chkTop.CheckState = CheckState.Indeterminate;
				m_chkLeft.CheckState = CheckState.Indeterminate;
				m_chkRight.CheckState = CheckState.Indeterminate;
			}
			else
			{
				m_chkTop.CheckState = (styleInfo.BorderTop == 0) ? CheckState.Unchecked : CheckState.Checked;
				m_chkBottom.CheckState = (styleInfo.BorderBottom == 0) ? CheckState.Unchecked : CheckState.Checked;
				m_chkLeft.CheckState = (styleInfo.BorderLeading == 0) ? CheckState.Unchecked : CheckState.Checked;
				m_chkRight.CheckState = (styleInfo.BorderTrailing == 0) ? CheckState.Unchecked : CheckState.Checked;
			}

			m_cboWidth.SetInheritableProp(styleInfo.BorderThickness);
			m_cboWidth.ShowingInheritedProperties = styleInfo.Inherits;
			int maxWidth = styleInfo.BorderWidth;
			if (maxWidth == 0)
			{
				// 1/2 pt is the default value to display, even though 0 is the default value
				maxWidth = 500;
			}
			// select the border width in the combobox
			m_cboWidth.AdjustedSelectedIndex = Array.IndexOf<int>(s_borderSizes, maxWidth);

			// Change the left and right check boxes if the check boxes need to change
			// places (because the paragraph direction is different)
			if ((m_chkRight.Left < m_chkLeft.Left) != DefaultTextDirectionRtoL)
			{
				System.Drawing.ContentAlignment saveChkAlign = m_chkLeft.CheckAlign;
				m_chkLeft.CheckAlign = m_chkRight.CheckAlign;
				m_chkRight.CheckAlign = saveChkAlign;

				AdjustCheckboxPositionsToPreserveGap();
			}

			// Change the labels to show leading/trailing or left/right depending on the
			// paragraph direction.
			ChangeDirectionLabels(styleInfo.IRightToLeftStyle.Value == TriStateBool.triNotSet ?
				m_fShowBiDiLabels : styleInfo.IRightToLeftStyle.Value == TriStateBool.triTrue);

			m_dontUpdateInheritance = false;
			m_fIgnoreCascadingEvents = false;
			m_pnlBorderPreview.Refresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves border info into a StyleInfo
		/// </summary>
		/// <param name="styleInfo">styleInfo to save into</param>
		/// ------------------------------------------------------------------------------------
		public void SaveToInfo(StyleInfo styleInfo)
		{
			CheckDisposed();

			if (styleInfo.IsCharacterStyle)
			{
				Debug.Assert(false, "Somehow, the Border tab has been asked to write its data to a character-based style [" + styleInfo.Name + "].");
				return;
			}

			// Save the border widths
			bool newInherit = m_cboWidth.IsInherited;
			BorderThicknesses newThickness = new BorderThicknesses();
			int width = s_borderSizes[m_cboWidth.AdjustedSelectedIndex];
			newThickness.Bottom = (m_chkBottom.CheckState == CheckState.Checked) ? width : 0;
			newThickness.Top = (m_chkTop.CheckState == CheckState.Checked) ? width : 0;
			newThickness.Leading = (m_chkLeft.CheckState == CheckState.Checked) ? width : 0;
			newThickness.Trailing = (m_chkRight.CheckState == CheckState.Checked) ? width : 0;
			if (styleInfo.BorderThickness.Save(newInherit, newThickness))
				styleInfo.Dirty = true;

			// save the border color
			if (styleInfo.IBorderColor.Save(IsInherited(m_cboColor), m_cboColor.ColorValue))
				styleInfo.Dirty = true;
		}
		#endregion

		#region Events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles clicking on the all or none buttons
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnAllNone_Click(object sender, EventArgs e)
		{
			m_chkBottom.CheckState = m_chkTop.CheckState = m_chkLeft.CheckState =
				m_chkRight.CheckState = (sender == m_btnAll) ? CheckState.Checked :
				CheckState.Unchecked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_cboWidth control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_cboWidth_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_fIgnoreCascadingEvents)
				return;

			m_fIgnoreCascadingEvents = true;
			if (m_cboWidth.AdjustedSelectedIndex != 0)
			{
				if (m_chkTop.ThreeState)
				{
					// The user went from unspecified to something else
					m_chkTop.ThreeState = m_chkBottom.ThreeState = m_chkLeft.ThreeState =
						m_chkRight.ThreeState = false;
					m_chkTop.CheckState = m_chkBottom.CheckState = m_chkLeft.CheckState =
						m_chkRight.CheckState = CheckState.Unchecked;
				}
				// otherwise we already were something else
			}
			else
			{
				// The user selected "unspecified"
				m_chkTop.ThreeState = m_chkBottom.ThreeState = m_chkLeft.ThreeState =
					m_chkRight.ThreeState = true;
				m_chkTop.CheckState = m_chkBottom.CheckState = m_chkLeft.CheckState =
					m_chkRight.CheckState = CheckState.Indeterminate;
			}

			UpdateControls(sender);
			m_fIgnoreCascadingEvents = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the top/bottom/left/right check boxes change or the selected index of the
		/// width combo changes, we need to refresh the preview control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckedChanged(object sender, EventArgs e)
		{
			if (m_fIgnoreCascadingEvents)
				return;

			if (m_cboWidth.IsInherited)
				m_cboWidth_SelectedIndexChanged(m_cboWidth, EventArgs.Empty);

			UpdateControls(sender);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ColorPicked event of the m_cboColor control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_cboColor_ColorPicked(object sender, EventArgs e)
		{
			UpdateControls(sender);
		}
		#endregion
	}
}
