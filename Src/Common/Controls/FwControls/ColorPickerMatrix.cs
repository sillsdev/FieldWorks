// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2006' company='SIL International'>
//    Copyright (c) 2006, SIL International. All Rights Reserved.
// </copyright>
//
// File: ColorPickerMatrix.cs
// Responsibility: TeTeam
// Last reviewed:
//
// Implementation of ColorPickerMatrix
// --------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Resources;
using System.Windows.Forms;

using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ColorPickerMatrix : UserControl, IFWDisposable
	{
		/// <summary></summary>
		public event EventHandler ColorPicked;

		private const int kColorSquareSize = 18;
		private const int kNumberOfCols = 8;
		private string m_currentColorName;

		private Dictionary<XButton, Color> m_clrButtons = new Dictionary<XButton, Color>();
		private Color m_currColor = Color.Empty;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ColorPickerMatrix()
		{
			InitializeComponent();

			DoubleBuffered = true;

			m_toolTip = new ToolTip();

			int row = 0;
			int col = 0;

			for (int i = 0; i < ColorUtil.kNumberOfColors; i++)
			{
				// Get the entry from the resources that has the color name and RGB value.
				Color color = ColorUtil.ColorAtIndex(i);
				if (color == Color.Empty)
					continue;

				XButton btn = new XButton();
				btn.CanBeChecked = true;
				btn.DrawEmpty = true;
				btn.Size = new Size(kColorSquareSize, kColorSquareSize);
				btn.BackColor = BackColor;
				btn.Location = new Point(col * kColorSquareSize, row * kColorSquareSize);
				btn.Paint += new PaintEventHandler(btn_Paint);
				btn.Click += new EventHandler(btn_Click);
				Controls.Add(btn);

				// Store the name in the tooltip and create a color from the RGB values.
				m_toolTip.SetToolTip(btn, ColorUtil.ColorNameAtIndex(i));
				m_clrButtons[btn] = color;

				col++;
				if (col == kNumberOfCols)
				{
					col = 0;
					row++;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the name of a color
		/// </summary>
		/// <param name="color">The color.</param>
		/// <returns>The name</returns>
		/// ------------------------------------------------------------------------------------
		internal static string ColorToName(Color color)
		{
			if (color == Color.Empty)
				return ColorPickerStrings.kstidUnspecifiedSettingText;

			return ColorUtil.ColorToName(color);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the control's current color.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Color CurrentColor
		{
			get
			{
				CheckDisposed();
				return m_currColor;
			}
			set
			{
				CheckDisposed();
				m_currColor = value;

				foreach (KeyValuePair<XButton, Color> square in m_clrButtons)
					square.Key.Checked = (value == square.Value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the current color.
		/// </summary>
		/// <value>The name of the current color.</value>
		/// ------------------------------------------------------------------------------------
		public string CurrentColorName
		{
			get
			{
				CheckDisposed();
				return m_currentColorName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void btn_Click(object sender, EventArgs e)
		{
			XButton btn = sender as XButton;
			if (btn != null && m_clrButtons.ContainsKey(btn) && m_clrButtons[btn] != m_currColor)
			{
				m_currentColorName = m_toolTip.GetToolTip(btn);
				CurrentColor = m_clrButtons[btn];
				if (ColorPicked != null)
					ColorPicked(this, EventArgs.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void btn_Paint(object sender, PaintEventArgs e)
		{
			XButton btn = sender as XButton;
			if (btn == null) // || btn.Parent == null)
				return;

			Rectangle rc = btn.ClientRectangle;

			using (SolidBrush br = new SolidBrush(btn.BackColor))
			{
				e.Graphics.FillRectangle(br, rc);

				br.Color = Color.Gray;
				rc.Inflate(-3, -3);
				e.Graphics.FillRectangle(br, rc);

				br.Color = m_clrButtons[btn];
				rc.Inflate(-1, -1);
				e.Graphics.FillRectangle(br, rc);
			}
		}
	}
}
