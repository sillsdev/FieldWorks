// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwFontAttributes.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Control that holds the font attributes (bold/italic/colors/underline etc.). This control
	/// is used in the Font tab of the Styles dialog as well as in the stand-alone Font dialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FwFontAttributes : UserControl, IFWDisposable
	{
		#region Data Members
		/// <summary>Occurs when the value of one of the controls has changed.</summary>
		public event EventHandler ValueChanged;

		private bool m_fShowingInheritedProperties;
		private bool m_fAlwaysDisableFontFeatures;

		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwFontAttributes"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwFontAttributes()
		{
			InitializeComponent();
			m_btnFontFeatures.Tag = true; // indicate font features are inherited
		}
		#endregion

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

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the writing system factory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILgWritingSystemFactory WritingSystemFactory
		{
			set { CheckDisposed(); m_btnFontFeatures.WritingSystemFactory = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this control is currently displaying properties for a
		/// style which inherits from another style or for a WS-specific override for a style.
		/// </summary>
		/// <value>
		/// 	<c>false</c> if no specific WS is selected and we're displaying the properties
		/// for the "Normal" style; otherwise, <c>true</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool ShowingInheritedProperties
		{
			get { return m_fShowingInheritedProperties; }
			set { m_fShowingInheritedProperties = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the font features button is active.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool FontFeaturesTag
		{
			get { return (bool)m_btnFontFeatures.Tag; }
			set { m_btnFontFeatures.Tag = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the name of the font.
		/// </summary>
		/// <value>The name of the font.</value>
		/// ------------------------------------------------------------------------------------
		public string FontName
		{
			set { m_btnFontFeatures.FontName = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the controls for super/subscript are enabled or not.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowSuperSubScript
		{
			get { return m_chkSubscript.Enabled; }
			set
			{
				m_chkSubscript.Enabled = value;
				m_chkSuperscript.Enabled = value;
				m_cboFontPosition.Enabled = value;
				m_nudPositionAmount.Enabled = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether to always disable the font features button even
		/// when a Graphite font is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AlwaysDisableFontFeatures
		{
			set
			{
				m_fAlwaysDisableFontFeatures = value;
				if (m_fAlwaysDisableFontFeatures)
				{
					m_btnFontFeatures.Enabled = false;
					m_btnFontFeatures.EnabledChanged += new EventHandler(OnFontFeaturesEnabledChanged);
				}
			}
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the font features button gets enabled or disabled.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnFontFeaturesEnabledChanged(object sender, EventArgs e)
		{
			if (m_fAlwaysDisableFontFeatures)
				m_btnFontFeatures.Enabled = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_cboFontPosition control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_cboFontPosition_SelectedIndexChanged(object sender, EventArgs e)
		{
			OnValueChanged(sender, e);

			m_nudPositionAmount.Enabled = true;
			switch (m_cboFontPosition.AdjustedSelectedIndex)
			{
				case 0: // Unspecified
					m_nudPositionAmount.MeasureValue = 0;
					m_nudPositionAmount.Text = string.Empty;
					break;
				case 1: // Normal
					m_nudPositionAmount.MeasureValue = 0;
					break;
				case 2: // Raised
					if (m_nudPositionAmount.MeasureValue == 0)
						m_nudPositionAmount.MeasureValue = 3000;
					else if (m_nudPositionAmount.MeasureValue < 0)
						m_nudPositionAmount.MeasureValue *= -1;
					break;
				case 3: // Lowered
					if (m_nudPositionAmount.MeasureValue == 0)
						m_nudPositionAmount.MeasureValue = -3000;
					else if (m_nudPositionAmount.MeasureValue > 0)
						m_nudPositionAmount.MeasureValue *= -1;
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Changed event of the m_nudPositionAmount control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_nudPositionAmount_Changed(object sender, EventArgs e)
		{
			OnValueChanged(sender, e);
			SetPositionCombo();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a control's value changes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnValueChanged(object sender, EventArgs e)
		{
			if (ValueChanged != null)
				ValueChanged(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the FontFeatureSelected event of the m_btnFontFeatures control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_btnFontFeatures_FontFeatureSelected(object sender, EventArgs e)
		{
			m_btnFontFeatures.Tag = false; // No longer inherited
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the position combo box value based on the value in the Position Amount control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetPositionCombo()
		{
			if (m_nudPositionAmount.MeasureValue == 0) // Normal
				m_cboFontPosition.AdjustedSelectedIndex = 1;
			else if (m_nudPositionAmount.MeasureValue > 0) // Raised
				m_cboFontPosition.AdjustedSelectedIndex = 2;
			else if (m_nudPositionAmount.MeasureValue < 0) // Lowered
				m_cboFontPosition.AdjustedSelectedIndex = 3;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the check changed event for the superscript and subscript check boxes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SuperSubCheckChanged(object sender, EventArgs e)
		{
			CheckBox thisBox = (CheckBox)sender;
			CheckBox otherBox = (sender == m_chkSubscript) ? m_chkSuperscript : m_chkSubscript;
			// They mustn't both be checked, so turn of the other one if this is on.
			if (thisBox.CheckState == CheckState.Checked)
				otherBox.CheckState = CheckState.Unchecked;
			// If one is indeterminate the other should be too, so if this is, make the other match.
			else if (thisBox.CheckState == CheckState.Indeterminate)
				otherBox.CheckState = CheckState.Indeterminate;
			// Otherwise this is going unchecked. If the other is indeterminate change to off.
			// (However, do NOT turn the other off if it was on!).
			else if (otherBox.CheckState == CheckState.Indeterminate)
				otherBox.CheckState = CheckState.Unchecked;
			OnValueChanged(sender, EventArgs.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the DrawItemForeground event of the m_cboUnderlineStyle control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DrawItemEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_cboUnderlineStyle_DrawItemForeground(object sender, DrawItemEventArgs e)
		{
			// Draw the text or underline style
			using (Pen pen = new Pen(e.ForeColor))
			{
				const int lineMargin = 1;
				switch (e.Index + (ShowingInheritedProperties ? 0 : 1))
				{
					case 0:
					case 1:
					case 6:
						string text = (string)m_cboUnderlineStyle.Items[e.Index];
						e.Graphics.DrawString(text, e.Font, new SolidBrush(e.ForeColor),
						new RectangleF(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height));
						break;

					case 2:
						// single underline
						e.Graphics.DrawLine(pen,
						e.Bounds.X + lineMargin, e.Bounds.Y + e.Bounds.Height / 2,
						e.Bounds.Right - lineMargin, e.Bounds.Y + e.Bounds.Height / 2);
						break;

					case 3:
						// double underline
						e.Graphics.DrawLine(pen,
						e.Bounds.X + lineMargin, e.Bounds.Y + e.Bounds.Height / 2 - 1,
						e.Bounds.Right - lineMargin, e.Bounds.Y + e.Bounds.Height / 2 - 1);
						e.Graphics.DrawLine(pen,
						e.Bounds.X + lineMargin, e.Bounds.Y + e.Bounds.Height / 2 + 1,
						e.Bounds.Right - lineMargin, e.Bounds.Y + e.Bounds.Height / 2 + 1);
						break;

					case 4:
						// dotted underline
						pen.DashStyle = DashStyle.Dot;
						e.Graphics.DrawLine(pen,
						e.Bounds.X + lineMargin, e.Bounds.Y + e.Bounds.Height / 2,
						e.Bounds.Right - lineMargin, e.Bounds.Y + e.Bounds.Height / 2);
						break;

					case 5:
						// dashed underline
						pen.DashStyle = DashStyle.Dash;
						e.Graphics.DrawLine(pen,
						e.Bounds.X + lineMargin, e.Bounds.Y + e.Bounds.Height / 2,
						e.Bounds.Right - lineMargin, e.Bounds.Y + e.Bounds.Height / 2);
						break;
				}
			}
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the CheckState of the bold check box (checked, unchecked or indeterminate).
		/// </summary>
		/// <param name="fIsInherited">set to <c>true</c> if font position is inherited.</param>
		/// ------------------------------------------------------------------------------------
		public bool GetBold(out bool fIsInherited)
		{
			fIsInherited = IsInherited(m_chkBold);
			return m_chkBold.CheckState == CheckState.Checked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the CheckState of the italic check box (checked, unchecked or indeterminate).
		/// </summary>
		/// <param name="fIsInherited">set to <c>true</c> if font position is inherited.</param>
		/// ------------------------------------------------------------------------------------
		public bool GetItalic(out bool fIsInherited)
		{
			fIsInherited = IsInherited(m_chkItalic);
			return m_chkItalic.CheckState == CheckState.Checked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sub/superscript setting.
		/// </summary>
		/// <param name="fIsInherited">set to <c>true</c> if font position is inherited.</param>
		/// ------------------------------------------------------------------------------------
		public FwSuperscriptVal GetSubSuperscript(out bool fIsInherited)
		{
			fIsInherited = IsInherited(m_chkSubscript);
			FwSuperscriptVal superSub;
			if (m_chkSubscript.CheckState == CheckState.Checked)
				superSub = FwSuperscriptVal.kssvSub;
			else if (m_chkSuperscript.CheckState == CheckState.Checked)
				superSub = FwSuperscriptVal.kssvSuper;
			else
				superSub = FwSuperscriptVal.kssvOff;

			return superSub;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the underline.
		/// </summary>
		/// <param name="fIsInherited">set to <c>true</c> if font position is inherited.</param>
		/// ------------------------------------------------------------------------------------
		public FwUnderlineType GetUnderlineType(out bool fIsInherited)
		{
			fIsInherited = IsInherited(m_cboUnderlineStyle);
			FwUnderlineType underlineType = FwUnderlineType.kuntMin; // Init to make compiler happy
			if (!fIsInherited)
			{
				switch (m_cboUnderlineStyle.AdjustedSelectedIndex)
				{
					case 1: underlineType = FwUnderlineType.kuntNone; break;
					case 2: underlineType = FwUnderlineType.kuntSingle; break;
					case 3: underlineType = FwUnderlineType.kuntDouble; break;
					case 4: underlineType = FwUnderlineType.kuntDotted; break;
					case 5: underlineType = FwUnderlineType.kuntDashed; break;
					case 6: underlineType = FwUnderlineType.kuntStrikethrough; break;
					case -1: break; // nothing selected
					default:
						Debug.Assert(false, "Unknown underline style");
						break;
				}
			}
			return underlineType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the color of the font.
		/// </summary>
		/// <value>The color of the font.</value>
		/// <param name="fIsInherited">set to <c>true</c> if font position is inherited.</param>
		/// ------------------------------------------------------------------------------------
		public Color GetFontColor(out bool fIsInherited)
		{
			fIsInherited = IsInherited(m_cboFontColor);
			return m_cboFontColor.ColorValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the background color.
		/// </summary>
		/// <value>The background color combo.</value>
		/// <param name="fIsInherited">set to <c>true</c> if font position is inherited.</param>
		/// ------------------------------------------------------------------------------------
		public Color GetBackgroundColor(out bool fIsInherited)
		{
			fIsInherited = IsInherited(m_cboBackgroundColor);
			return m_cboBackgroundColor.ColorValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the underline color.
		/// </summary>
		/// <param name="fIsInherited">set to <c>true</c> if font position is inherited.</param>
		/// ------------------------------------------------------------------------------------
		public Color GetUnderlineColor(out bool fIsInherited)
		{
			fIsInherited = IsInherited(m_cboUnderlineColor);
			return m_cboUnderlineColor.ColorValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the font features.
		/// </summary>
		/// <param name="fIsInherited">set to <c>true</c> if font position is inherited.</param>
		/// ------------------------------------------------------------------------------------
		public string GetFontFeatures(out bool fIsInherited)
		{
			fIsInherited = IsInherited(m_btnFontFeatures);
			return m_btnFontFeatures.FontFeatures;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the font position.
		/// </summary>
		/// <param name="fIsInherited">set to <c>true</c> if font position is inherited.</param>
		/// ------------------------------------------------------------------------------------
		public int GetFontPosition(out bool fIsInherited)
		{
			fIsInherited = IsInherited(m_cboFontPosition);
			int fontPos = 0;
			switch (m_cboFontPosition.AdjustedSelectedIndex)
			{
				case 2: fontPos = m_nudPositionAmount.MeasureValue; break;
				case 3: fontPos = m_nudPositionAmount.MeasureValue; break;
			}

			return fontPos;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the information on the font tab.
		/// </summary>
		/// <param name="fontInfo">The font info.</param>
		/// ------------------------------------------------------------------------------------
		public void UpdateForStyle(FontInfo fontInfo)
		{
			CheckDisposed();

			// Initialize controls based on whether or not this style inherits from another style.
			InitControlBehavior(ShowingInheritedProperties);

			m_chkBold.CheckState = GetCheckStateFor(fontInfo.m_bold);
			m_chkItalic.CheckState = GetCheckStateFor(fontInfo.m_italic);
			CheckSuperSubBoxes(fontInfo);

			// update color comboboxes
			SetColorComboBoxStates(fontInfo);

			// update the font position combobox and up/down control
			if (!m_cboFontPosition.SetInheritableProp(fontInfo.m_offset))
			{
				m_nudPositionAmount.MeasureValue = fontInfo.m_offset.Value;
				SetPositionCombo();
			}
			m_nudPositionAmount.ForeColor = GetCtrlForeColorForProp(fontInfo.m_offset);

			m_btnFontFeatures.FontFeatures = (fontInfo.m_features.ValueIsSet) ?
				fontInfo.m_features.Value : null;
			m_btnFontFeatures.Tag = fontInfo.m_features.IsInherited;

			// update the font underline combobox
			if (!m_cboUnderlineStyle.SetInheritableProp(fontInfo.m_underline))
			{
				switch (fontInfo.m_underline.Value)
				{
					case FwUnderlineType.kuntNone:
						m_cboUnderlineStyle.AdjustedSelectedIndex = 1; break;
					case FwUnderlineType.kuntSingle:
						m_cboUnderlineStyle.AdjustedSelectedIndex = 2; break;
					case FwUnderlineType.kuntDouble:
						m_cboUnderlineStyle.AdjustedSelectedIndex = 3; break;
					case FwUnderlineType.kuntDotted:
						m_cboUnderlineStyle.AdjustedSelectedIndex = 4; break;
					case FwUnderlineType.kuntDashed:
						m_cboUnderlineStyle.AdjustedSelectedIndex = 5; break;
					case FwUnderlineType.kuntStrikethrough:
						m_cboUnderlineStyle.AdjustedSelectedIndex = 6; break;
					default:
						Debug.Assert(false, "Unknown underline type");
						break;
				}
			}
		}

		/// <summary>
		/// Set the IsInherited value of the color combo boxes to the value from the font info,
		/// also set the forecolor(text color) and color value based off of the font info.
		/// </summary>
		/// <param name="fontInfo"></param>
		private void SetColorComboBoxStates(FontInfo fontInfo)
		{
			m_cboFontColor.ForeColor = GetCtrlForeColorForProp(fontInfo.m_fontColor);
			m_cboFontColor.IsInherited = fontInfo.m_fontColor.IsInherited;
			m_cboFontColor.ColorValue = GetColorToDisplay(fontInfo.m_fontColor);
			m_cboBackgroundColor.ForeColor = GetCtrlForeColorForProp(fontInfo.m_backColor);
			m_cboBackgroundColor.IsInherited = fontInfo.m_backColor.IsInherited;
			m_cboBackgroundColor.ColorValue = GetColorToDisplay(fontInfo.m_backColor);
			m_cboUnderlineColor.ForeColor = GetCtrlForeColorForProp(fontInfo.m_underlineColor);
			m_cboUnderlineColor.IsInherited = fontInfo.m_underlineColor.IsInherited;
			m_cboUnderlineColor.ColorValue = GetColorToDisplay(fontInfo.m_underlineColor);
		}

		#endregion

		#region private methods
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
			return (prop.IsInherited && ShowingInheritedProperties) ?
				SystemColors.GrayText : SystemColors.WindowText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the color to display for a given color prop. If the current style is a
		/// paragraph style, this will always be the ultimate color the user will see for text
		/// displayed with this style (whether the value is explicit, inherited from a based-on
		/// style, or ultimately "inherited" from the system default). If the current style is
		/// a character style, it will be Color.Empty unless this property has an explicit value
		/// or somewhere in the inheritance chain for this this character style there is a
		/// style which has an explicit value for this property.
		/// </summary>
		/// <param name="colorProp">The color prop.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private Color GetColorToDisplay(InheritableStyleProp<Color> colorProp)
		{
			return (colorProp.ValueIsSet) ? colorProp.Value : Color.Empty;
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
			m_chkBold.ThreeState = fInherited;
			m_chkItalic.ThreeState = fInherited;
			m_chkSuperscript.ThreeState = fInherited;
			m_chkSubscript.ThreeState = fInherited;
			m_cboFontPosition.ShowingInheritedProperties = fInherited;
			m_cboUnderlineStyle.ShowingInheritedProperties = fInherited;
			m_cboFontColor.ShowUnspecified = fInherited;
			m_cboBackgroundColor.ShowUnspecified = fInherited;
			m_cboUnderlineColor.ShowUnspecified = fInherited;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified control value is inherited.
		/// </summary>
		/// <param name="c">The control</param>
		/// <returns>true if the specified control is inherited; otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		private bool IsInherited(Control c)
		{
			if (!ShowingInheritedProperties)
				return false;

			if (c is FwInheritablePropComboBox)
				return ((FwInheritablePropComboBox)c).IsInherited;

			if (c is FwColorCombo)
				return ((FwColorCombo)c).IsInherited;

			if (c is CheckBox)
				return ((CheckBox)c).CheckState == CheckState.Indeterminate;

			if (c == m_btnFontFeatures)
				return (bool)m_btnFontFeatures.Tag;

			// REVIEW : using control color to determine this isn't very robust!
			return c.ForeColor.ToArgb() != SystemColors.WindowText.ToArgb();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the super and subscript boxes based on the font info
		/// </summary>
		/// <param name="fontInfo">The font info.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckSuperSubBoxes(FontInfo fontInfo)
		{
			if (fontInfo.m_superSub.IsInherited && ShowingInheritedProperties)
			{
				m_chkSubscript.CheckState = CheckState.Indeterminate;
				m_chkSuperscript.CheckState = CheckState.Indeterminate;
			}
			else
			{
				switch (fontInfo.m_superSub.Value)
				{
					case FwSuperscriptVal.kssvOff:
						m_chkSubscript.CheckState = CheckState.Unchecked;
						m_chkSuperscript.CheckState = CheckState.Unchecked;
						break;

					case FwSuperscriptVal.kssvSub:
						m_chkSubscript.CheckState = CheckState.Checked;
						m_chkSuperscript.CheckState = CheckState.Unchecked;
						break;

					case FwSuperscriptVal.kssvSuper:
						m_chkSubscript.CheckState = CheckState.Unchecked;
						m_chkSuperscript.CheckState = CheckState.Checked;
						break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the check state for the specified boolean inheritable prop.
		/// </summary>
		/// <param name="prop">The prop.</param>
		/// <returns>The check state</returns>
		/// ------------------------------------------------------------------------------------
		private CheckState GetCheckStateFor(InheritableStyleProp<bool> prop)
		{
			if (prop.IsInherited && ShowingInheritedProperties)
				return CheckState.Indeterminate;
			return (prop.Value ? CheckState.Checked : CheckState.Unchecked);
		}
		#endregion
	}
}
