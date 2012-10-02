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
// File: FwParagraphTab.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FwParagraphTab : UserControl, IFWDisposable, IStylesTab
	{
		#region Member Data
		/// <summary>
		/// Fires when a change is made on the paragraph tab to an unspecified state.
		/// </summary>
		public event EventHandler ChangedToUnspecified;

		private const int kLineHeight = 5;
		private const int kLineSpacing = 2;
		private const int kmptPerPixel = 3000;

		// Indices into the line spacing combo box.
		private const int kAtLeastIndex = 4;
		private const int kExactlyIndex = 5;

		private bool m_DefaultTextDirectionRtoL = false;
		private bool m_fShowBiDiLabels = false;
		private bool m_dontUpdateInheritance = true;
		private StyleInfo m_currentStyleInfo;
		#endregion

		#region Construction and demolition
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwParagraphTab"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwParagraphTab()
		{
			InitializeComponent();
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

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a value changes that needs to update the paragraph preview
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ValueChanged(object sender, EventArgs e)
		{
			if (!m_dontUpdateInheritance && sender != null)
			{
				((Control)sender).ForeColor = SystemColors.WindowText;

				if (IsInherited((Control)sender) && ChangedToUnspecified != null)
					ChangedToUnspecified(this, EventArgs.Empty);
			}

			if (sender is UpDownMeasureControl)
			{
				UpDownMeasureControl ctrl = (UpDownMeasureControl)sender;
				if (ctrl.Text == string.Empty)
				{
					// When numerical values in the special indentation and line spacing controls are reset,
					// the values should be set in the associated combobox to unspecified -- this will
					// cause this event handler to fire again and reset both the combo box and the numeric
					// value to the inherited value.
					if (ctrl == m_nudIndentBy)
					{
						m_cboSpecialIndentation.AdjustedSelectedIndex = 0;
						return;
					}
					if (ctrl == m_nudSpacingAt)
					{
						m_cboLineSpacing.AdjustedSelectedIndex = 0;
						return;
					}

					m_dontUpdateInheritance = true;
					if (m_currentStyleInfo.Inherits)
					{
						InheritableStyleProp<int> prop;
						int inheritedValue;
						if (ctrl == m_nudLeftIndentation)
						{
							prop = m_currentStyleInfo.ILeadingIndent;
							inheritedValue = m_currentStyleInfo.BasedOnStyle.LeadingIndent;
						}
						else if (ctrl == m_nudRightIndentation)
						{
							prop = m_currentStyleInfo.ITrailingIndent;
							inheritedValue = m_currentStyleInfo.BasedOnStyle.TrailingIndent;
						}
						else if (ctrl == m_nudBefore)
						{
							prop = m_currentStyleInfo.ISpaceBefore;
							inheritedValue = m_currentStyleInfo.BasedOnStyle.SpaceBefore;
						}
						else if (ctrl == m_nudAfter)
						{
							prop = m_currentStyleInfo.ISpaceAfter;
							inheritedValue = m_currentStyleInfo.BasedOnStyle.SpaceAfter;
						}
						else
							throw new Exception("Somebody added a new nud control");

						prop.ResetToInherited(inheritedValue);
						ctrl.ForeColor = GetCtrlForeColorForProp(prop);
						ctrl.MeasureValue = prop.Value;
					}
					else
					{
						ctrl.MeasureValue = ctrl.MeasureValue;
					}
					m_dontUpdateInheritance = false;
				}
			}
			else if (sender == m_cboLineSpacing)
			{
				if (m_cboLineSpacing.AdjustedSelectedIndex == kAtLeastIndex)
					m_nudSpacingAt.MeasureMin = 0;
				else if (m_cboLineSpacing.AdjustedSelectedIndex == kExactlyIndex)
					m_nudSpacingAt.MeasureMin = 1000;
			}
			else if (sender == m_cboDirection)
			{
				ChangeDirectionLabels(
					(TriStateBool)m_cboDirection.AdjustedSelectedIndex == TriStateBool.triTrue);
			}

			int index = m_cboLineSpacing.AdjustedSelectedIndex;
			m_nudSpacingAt.Enabled = (index == kAtLeastIndex || index == kExactlyIndex) &&
				!IsInherited(m_cboLineSpacing);
			m_nudIndentBy.Enabled = (index != 1) && !IsInherited(m_cboSpecialIndentation);
			m_pnlPreview.Refresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw the paragraph preview panel
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_pnlPreview_Paint(object sender, PaintEventArgs e)
		{
			// Get the rectangle to draw in and shrink it a bit to leave some margin space
			Rectangle drawRect = m_pnlPreview.ClientRectangle;
			e.Graphics.FillRectangle(SystemBrushes.Window, drawRect);
			drawRect.Inflate(-4, -4);

			DrawAdjacentPreview(2, ref drawRect, e.Graphics);
			DrawParaPreview(ref drawRect, e.Graphics);
			DrawAdjacentPreview(3, ref drawRect, e.Graphics);
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

			// Don't allow controls to undo their inherited state while filling in
			m_dontUpdateInheritance = true;
			m_currentStyleInfo = styleInfo;

			// Initialize controls based on whether or not this style inherits from another style.
			InitControlBehavior(styleInfo.Inherits);

			// LTR or RTL
			m_cboDirection.SetInheritableProp(styleInfo.IRightToLeftStyle);
			m_cboDirection.AdjustedSelectedIndex = (int)styleInfo.IRightToLeftStyle.Value;
			ChangeDirectionLabels(styleInfo.IRightToLeftStyle.Value == TriStateBool.triNotSet ||
				m_fShowBiDiLabels ? m_fShowBiDiLabels :
				styleInfo.IRightToLeftStyle.Value == TriStateBool.triTrue);

			// Paragraph Alignment
			m_cboAlignment.SetInheritableProp(styleInfo.IAlignment);
			switch (styleInfo.IAlignment.Value)
			{
				case FwTextAlign.ktalLeading: m_cboAlignment.AdjustedSelectedIndex = 1; break;
				case FwTextAlign.ktalLeft: m_cboAlignment.AdjustedSelectedIndex = 2; break;
				case FwTextAlign.ktalCenter: m_cboAlignment.AdjustedSelectedIndex = 3; break;
				case FwTextAlign.ktalRight: m_cboAlignment.AdjustedSelectedIndex = 4; break;
				case FwTextAlign.ktalTrailing: m_cboAlignment.AdjustedSelectedIndex = 5; break;
				case FwTextAlign.ktalJustify: m_cboAlignment.AdjustedSelectedIndex = 6; break;
			}

			// Special indent
			m_cboSpecialIndentation.SetInheritableProp(styleInfo.IFirstLineIndent);
			if (styleInfo.IFirstLineIndent.Value == 0)
				m_cboSpecialIndentation.AdjustedSelectedIndex = 1;	// none
			else if (styleInfo.IFirstLineIndent.Value > 0)
				m_cboSpecialIndentation.AdjustedSelectedIndex = 2;	// first line
			else
				m_cboSpecialIndentation.AdjustedSelectedIndex = 3;	// hanging
			m_nudIndentBy.ForeColor = GetCtrlForeColorForProp(styleInfo.IFirstLineIndent);
			m_nudIndentBy.MeasureValue = Math.Abs(styleInfo.IFirstLineIndent.Value);

			// update the up/down measure controls
			m_nudLeftIndentation.ForeColor = GetCtrlForeColorForProp(styleInfo.ILeadingIndent);
			m_nudLeftIndentation.MeasureValue = styleInfo.ILeadingIndent.Value;
			m_nudRightIndentation.ForeColor = GetCtrlForeColorForProp(styleInfo.ITrailingIndent);
			m_nudRightIndentation.MeasureValue = styleInfo.ITrailingIndent.Value;
			m_nudBefore.ForeColor = GetCtrlForeColorForProp(styleInfo.ISpaceBefore);
			m_nudBefore.MeasureValue = styleInfo.ISpaceBefore.Value;
			m_nudAfter.ForeColor = GetCtrlForeColorForProp(styleInfo.ISpaceAfter);
			m_nudAfter.MeasureValue = styleInfo.ISpaceAfter.Value;

			LineHeightInfo info = styleInfo.ILineSpacing.Value;
			m_cboLineSpacing.SetInheritableProp(styleInfo.ILineSpacing);
			m_nudSpacingAt.ForeColor = GetCtrlForeColorForProp(styleInfo.ILineSpacing);
			if (!info.m_relative)
			{
				if (info.m_lineHeight < 0)
				{
					// Exact line spacing
					m_cboLineSpacing.AdjustedSelectedIndex = kExactlyIndex;
					m_nudSpacingAt.MeasureMin = 1000;
				}
				else
				{
					// at least line spacing
					m_cboLineSpacing.AdjustedSelectedIndex = kAtLeastIndex;
					m_nudSpacingAt.MeasureMin = 0;
				}

				m_nudSpacingAt.MeasureValue = Math.Abs(info.m_lineHeight);
			}
			else
			{
				switch(info.m_lineHeight)
				{
					case 10000:	// single spacing
						m_cboLineSpacing.AdjustedSelectedIndex = 1;
						break;
					case 15000:	// 1.5 line spacing
						m_cboLineSpacing.AdjustedSelectedIndex = 2;
						break;
					case 20000:	// double spacing
						m_cboLineSpacing.AdjustedSelectedIndex = 3;
						break;
				}
			}

			FontInfo fontInfo = styleInfo.FontInfoForWs(-1); // get default fontInfo
			m_cboBackground.ForeColor = GetCtrlForeColorForProp(fontInfo.m_backColor);
			m_cboBackground.ColorValue = fontInfo.m_backColor.Value;

			m_dontUpdateInheritance = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the paragraph information to the styleInfo
		/// </summary>
		/// <param name="styleInfo">The style info.</param>
		/// ------------------------------------------------------------------------------------
		public void SaveToInfo(StyleInfo styleInfo)
		{
			CheckDisposed();

			// direction
			bool newInherit = IsInherited(m_cboDirection);
			if (styleInfo.IRightToLeftStyle.Save(newInherit, (TriStateBool)m_cboDirection.SelectedIndex))
				styleInfo.Dirty = true;

			// alignment
			newInherit = m_cboAlignment.IsInherited;
			FwTextAlign newAlignment = FwTextAlign.ktalLeading;
			switch (m_cboAlignment.AdjustedSelectedIndex)
			{
				case 1: newAlignment = FwTextAlign.ktalLeading; break;
				case 2: newAlignment = FwTextAlign.ktalLeft; break;
				case 3: newAlignment = FwTextAlign.ktalCenter; break;
				case 4: newAlignment = FwTextAlign.ktalRight; break;
				case 5: newAlignment = FwTextAlign.ktalTrailing; break;
				case 6: newAlignment = FwTextAlign.ktalJustify; break;
			}
			if (styleInfo.IAlignment.Save(newInherit, newAlignment))
				styleInfo.Dirty = true;

			// background color - only save it if the control is visible
			if (m_cboBackground.Visible)
			{
				newInherit = IsInherited(m_cboBackground);
				FontInfo fontInfo = styleInfo.FontInfoForWs(-1); // get default FontInfo
				if (fontInfo.m_backColor.Save(newInherit, m_cboBackground.ColorValue))
					styleInfo.Dirty = true;
			}

			// left indent
			newInherit = IsInherited(m_nudLeftIndentation);
			if (styleInfo.ILeadingIndent.Save(newInherit, m_nudLeftIndentation.MeasureValue))
				styleInfo.Dirty = true;

			// right indent
			newInherit = IsInherited(m_nudRightIndentation);
			if (styleInfo.ITrailingIndent.Save(newInherit, m_nudRightIndentation.MeasureValue))
				styleInfo.Dirty = true;

			// special indent
			newInherit = m_cboSpecialIndentation.IsInherited;
			int newValue = 0;
			switch (m_cboSpecialIndentation.AdjustedSelectedIndex)
			{
				case 2: newValue = m_nudIndentBy.MeasureValue; break;
				case 3: newValue = -m_nudIndentBy.MeasureValue; break;
			}
			if (styleInfo.IFirstLineIndent.Save(newInherit, newValue))
				styleInfo.Dirty = true;

			// spacing before
			newInherit = IsInherited(m_nudBefore);
			if (styleInfo.ISpaceBefore.Save(newInherit, m_nudBefore.MeasureValue))
				styleInfo.Dirty = true;

			// spacing after
			newInherit = IsInherited(m_nudAfter);
			if (styleInfo.ISpaceAfter.Save(newInherit, m_nudAfter.MeasureValue))
				styleInfo.Dirty = true;

			// line spacing
			int index = m_cboLineSpacing.AdjustedSelectedIndex;
			newInherit = m_cboLineSpacing.IsInherited;
			LineHeightInfo newLineHeight = new LineHeightInfo();
			newLineHeight.m_relative = (index <= 3);
			switch (index)
			{
				case 1:  // single spacing
					newLineHeight.m_lineHeight = 10000; break;
				case 2: // 1.5 spacing
					newLineHeight.m_lineHeight = 15000; break;
				case 3: // double spacing
					newLineHeight.m_lineHeight = 20000; break;
				case kAtLeastIndex: // at least
					newLineHeight.m_lineHeight = m_nudSpacingAt.MeasureValue; break;
				case kExactlyIndex: // exactly
					newLineHeight.m_lineHeight = -m_nudSpacingAt.MeasureValue; break;
			}
			if (styleInfo.ILineSpacing.Save(newInherit, newLineHeight))
				styleInfo.Dirty = true;
		}
		#endregion

		#region Public Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// show or hide the control for setting the background color
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShowBackgroundColor
		{
			get
			{
				CheckDisposed();
				return m_cboBackground.Visible;
			}
			set
			{
				CheckDisposed();

				m_cboBackground.Visible = value;
				m_lblBackground.Visible = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether default text direction is Right-toLeft or not.
		/// </summary>
		/// <remarks>Typically this is the default direction of the view from which this dialog
		/// is invoked.</remarks>
		/// ------------------------------------------------------------------------------------
		public bool DefaultTextDirectionRtoL
		{
			set { CheckDisposed(); m_DefaultTextDirectionRtoL = value; }
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the display measurement unit for the "nud" controls that don't use points.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MsrSysType MeasureType
		{
			set
			{
				CheckDisposed();
				m_nudIndentBy.MeasureType = value;
				m_nudLeftIndentation.MeasureType = value;
				m_nudRightIndentation.MeasureType = value;
			}
		}
		#endregion

		#region Private helper properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the direction is right-to-left.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool RtoL
		{
			get
			{
				return ((TriStateBool)m_cboDirection.SelectedIndex == TriStateBool.triNotSet &&
					m_DefaultTextDirectionRtoL) ||
					(TriStateBool)m_cboDirection.SelectedIndex == TriStateBool.triTrue;
			}
		}
		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified control value is inherited.
		/// </summary>
		/// <param name="c">The control</param>
		/// <returns>true if the specified control is inherited; otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		private bool IsInherited(Control c)
		{
			if (!m_currentStyleInfo.Inherits)
				return false;

			if (c is FwInheritablePropComboBox)
				return ((FwInheritablePropComboBox)c).IsInherited;

			// The Direction combo box has index 0 as the unspecified state
			if (c == m_cboDirection && (TriStateBool)m_cboDirection.SelectedIndex == TriStateBool.triNotSet)
				return true;

			if (c == m_cboBackground && m_cboBackground.ColorValue == Color.Empty)
				return true;

			return c.ForeColor.ToArgb() != SystemColors.WindowText.ToArgb();
		}

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
				lblLeft.Text = FwCoreDlgControls.kstidLeadingCheck;
				lblRight.Text = FwCoreDlgControls.kstidTrailingCheck;
			}
			else
			{
				lblLeft.Text = FwCoreDlgControls.kstidLeftCheck;
				lblRight.Text = FwCoreDlgControls.kstidRightCheck;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize controls based on whether or not current style inherits from
		/// another style. If not (i.e., this is the "Normal" style), then controls
		/// should not allow the user to pick "unspecified" as the value.
		/// </summary>
		/// <param name="fInherited">Indicates whether current style is inherited.</param>
		/// ------------------------------------------------------------------------------------
		private void InitControlBehavior(bool fInherited)
		{
			m_cboBackground.ShowUnspecifiedButton = fInherited;
			m_cboAlignment.ShowingInheritedProperties = fInherited;
			m_cboSpecialIndentation.ShowingInheritedProperties = fInherited;
			m_cboLineSpacing.ShowingInheritedProperties = fInherited;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the paragraph representation either before or after the preview paragraph
		/// </summary>
		/// <param name="lineCount">number of lines to draw in the paragraph</param>
		/// <param name="drawRect">rectangle to draw in. This will be updated to remove
		/// the space where the paragraph has been drawn in.</param>
		/// <param name="g">graphics object to draw with</param>
		/// ------------------------------------------------------------------------------------
		private void DrawAdjacentPreview(int lineCount, ref Rectangle drawRect, Graphics g)
		{
			// draw each of the requested lines
			for (int i = 0; i < lineCount; i++)
			{
				Rectangle lineRect = new Rectangle(drawRect.X, drawRect.Y, drawRect.Width, kLineHeight);
				// For the first line, indent the "paragraph"
				if (i == 0)
				{
					if (!RtoL)
						lineRect.X += 10;
					lineRect.Width -= 10;
				}
				g.FillRectangle(SystemBrushes.GrayText, lineRect);
				drawRect.Y += (kLineHeight + kLineSpacing);
				drawRect.Height -= (kLineHeight + kLineSpacing);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws a representation of the paragraph
		/// </summary>
		/// <param name="drawRect">the rectangle to draw the representation in</param>
		/// <param name="g">the graphics object to use</param>
		/// ------------------------------------------------------------------------------------
		private void DrawParaPreview(ref Rectangle drawRect, Graphics g)
		{
			// draw three lines to represent the paragraph
			for (int i = 0; i < 3; i++)
			{
				Rectangle lineRect;

				// Perform first line adjustments
				if (i == 0)
					lineRect = CalculateFirstLineRect(drawRect);
				else
					lineRect = CalculateFollowingLineRect(drawRect, i);

				// Handle the left and right indentation
				int leftIndent = m_nudLeftIndentation.MeasureValue / kmptPerPixel;
				if (!RtoL)
					lineRect.X += leftIndent;
				lineRect.Width -= leftIndent;

				int rightIndent = m_nudRightIndentation.MeasureValue / kmptPerPixel;
				if (RtoL)
					lineRect.X += rightIndent;
				lineRect.Width -= rightIndent;

				// On the last line, we need to add the paragraph trailing space to the background
				// and adjust the drawRect with it too.
				int bottomSpace = 0;
				if (i == 2)
					bottomSpace = m_nudAfter.MeasureValue / kmptPerPixel;

				// If the line spacing is other than single, then adjust the bottom space
				switch (m_cboLineSpacing.AdjustedSelectedIndex)
				{
					case 0: // unspecified
						Debug.Fail("Unspecified should never be selected.");
						break;

					case 1: // single
						break;

					case 2: // 1.5
						bottomSpace += (kLineSpacing / 2);
						break;

					case 3: // double
						bottomSpace += kLineSpacing;
						break;

					case kAtLeastIndex: // at least
					case kExactlyIndex: // exactly
						// only adjust for this at values above 12pt.
						int spaceAt = (m_nudSpacingAt.MeasureValue - 12000) / kmptPerPixel;
						if (spaceAt > 0)
							bottomSpace += spaceAt;
						break;
				}

				// Draw the background and the line
				Rectangle lineBackground = new Rectangle(
					drawRect.X + leftIndent, drawRect.Y, drawRect.Width - leftIndent - rightIndent,
					(lineRect.Bottom - drawRect.Y) + + bottomSpace + ((i < 2) ? kLineSpacing : 0));
				g.FillRectangle(new SolidBrush(m_cboBackground.ColorValue), lineBackground);
				g.FillRectangle(SystemBrushes.WindowText, lineRect);

				// Adjust the drawRect to remove the space for the line just drawn
				int rectAdjust = (lineRect.Bottom + kLineSpacing + bottomSpace) - drawRect.Y;
				drawRect.Y += rectAdjust;
				drawRect.Height -= rectAdjust;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates the rect that the first line will occupy
		/// </summary>
		/// <param name="drawRect">The remaining space in the window to draw in</param>
		/// <returns>the rectangle for the line</returns>
		/// ------------------------------------------------------------------------------------
		private Rectangle CalculateFirstLineRect(Rectangle drawRect)
		{
			Rectangle lineRect = new Rectangle(drawRect.X, drawRect.Y, drawRect.Width, kLineHeight);

			// Adjust it down by the "before" space
			int mpt = m_nudBefore.MeasureValue;
			lineRect.Offset(0, mpt / kmptPerPixel);

			// If "first line" indentation is chosen, then indent the line
			if (m_cboSpecialIndentation.AdjustedSelectedIndex == 2)
			{
				mpt = m_nudIndentBy.MeasureValue;
				if (!RtoL)
					lineRect.X += (mpt / kmptPerPixel);
				lineRect.Width -= (mpt / kmptPerPixel);
			}

			AdjustLineForFudge(ref lineRect, 24);

			return lineRect;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates the rectangle for any lines after the first line.
		/// </summary>
		/// <param name="drawRect">The remaining space in the window to draw in</param>
		/// <param name="lineNumber">The line number.</param>
		/// <returns>the rectangle for the line</returns>
		/// ------------------------------------------------------------------------------------
		private Rectangle CalculateFollowingLineRect(Rectangle drawRect, int lineNumber)
		{
			Rectangle lineRect = new Rectangle(drawRect.X, drawRect.Y, drawRect.Width, kLineHeight);

			// Handle lines other than the first line
			// If "hanging" indentation is chosen, then indent the line
			if (m_cboSpecialIndentation.AdjustedSelectedIndex == 3)
			{
				int mpt = m_nudIndentBy.MeasureValue;
				if (!RtoL)
					lineRect.X += (mpt / kmptPerPixel);
				lineRect.Width -= (mpt / kmptPerPixel);
			}

			if (lineNumber == 2)
				AdjustLineForFudge(ref lineRect, 36);

			return lineRect;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the line length with a fudge amount to make it look unjustified.
		/// </summary>
		/// <param name="lineRect">The line rect.</param>
		/// <param name="lineFudge">The line fudge amount</param>
		/// ------------------------------------------------------------------------------------
		private void AdjustLineForFudge(ref Rectangle lineRect, int lineFudge)
		{
			// Adjust the rect based on the justification
			switch (m_cboAlignment.AdjustedSelectedIndex)
			{
				case 0: // unspecified: Get from the inherited stuff -- this should probably never happen
					break;

				case 1: // leading
					if (RtoL)
						lineRect.X += lineFudge;
					lineRect.Width -= lineFudge;
					break;

				case 2: // left
					lineRect.Width -= lineFudge;
					break;

				case 3: // centered
					lineRect.X += lineFudge / 2;
					lineRect.Width -= lineFudge;
					break;

				case 4: // right
					lineRect.X += lineFudge;
					lineRect.Width -= lineFudge;
					break;

				case 5: // trailing
					if (!RtoL)
						lineRect.X += lineFudge;
					lineRect.Width -= lineFudge;
					break;

				case 6: // justified
					break;
			}
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
		#endregion
	}
}
