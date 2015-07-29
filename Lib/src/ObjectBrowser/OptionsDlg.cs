// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.Media;

namespace SIL.ObjectBrowser
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class OptionsDlg : Form
	{
		private Color m_clrText = SystemColors.WindowText;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="OptionsDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public OptionsDlg()
		{
			InitializeComponent();
			chkShade.CheckedChanged += chkShade_CheckedChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="OptionsDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public OptionsDlg(Color clr) : this()
		{
			clrPicker.SelectedColor = clr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the color.
		/// </summary>
		/// <value>The color.</value>
		/// ------------------------------------------------------------------------------------
		public Color SelectedColor
		{
			get { return clrPicker.SelectedColor; }
			set
			{
				clrPicker.SelectedColor = value;
				txtRGB.Text = string.Format("{0:X6}", value.ToArgb() & 0x00FFFFFF);
				lblSample.Invalidate();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether shading is enabled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShadingEnabled
		{
			get { return chkShade.Checked; }
			set
			{
				chkShade.CheckedChanged -= chkShade_CheckedChanged;
				chkShade.Checked = value;
				grpShadeColor.Enabled = value;
				chkShade.CheckedChanged += chkShade_CheckedChanged;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Paint event of the lblSample control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void lblSample_Paint(object sender, PaintEventArgs e)
		{
			TextFormatFlags flags = TextFormatFlags.HorizontalCenter |
				TextFormatFlags.VerticalCenter;

			using (SolidBrush br = new SolidBrush(SelectedColor))
				e.Graphics.FillRectangle(br, lblSample.ClientRectangle);

			TextRenderer.DrawText(e.Graphics, lblSample.Text, lblSample.Font,
				lblSample.ClientRectangle, m_clrText, flags);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnColor control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnColor_Click(object sender, EventArgs e)
		{
			clrDlg.Color = clrPicker.SelectedColor;
			if (clrDlg.ShowDialog(this) == DialogResult.OK)
				SelectedColor = clrDlg.Color;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the chkShade control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void chkShade_CheckedChanged(object sender, EventArgs e)
		{
			grpShadeColor.Enabled = chkShade.Checked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the user clicking on a color in the color picker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void clrPicker_ColorPicked(object sender, Color clrPicked)
		{
			SelectedColor = clrPicked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the KeyPress event of the txtRGB control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtRGB_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Enter)
			{
				e.Handled = true;
				e.KeyChar = (char)0;
				txtRGB_Validated(null, null);
				txtRGB.SelectAll();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Validated event of the txtRGB control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void txtRGB_Validated(object sender, EventArgs e)
		{
			uint rgb;
			if (uint.TryParse(txtRGB.Text, NumberStyles.HexNumber, null, out rgb))
			{
				txtRGB.Text = txtRGB.Text.ToUpper(CultureInfo.InvariantCulture);
				rgb |= 0xFF000000;
				SelectedColor = Color.FromArgb((int)rgb);
			}
			else
			{
				SystemSounds.Beep.Play();
				txtRGB.SelectAll();
				txtRGB.Focus();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnOK control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnOK_Click(object sender, EventArgs e)
		{
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnCancel control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnCancel_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
