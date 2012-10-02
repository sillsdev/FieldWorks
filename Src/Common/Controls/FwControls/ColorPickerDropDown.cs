using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ColorPickerDropDown : ToolStripDropDown, IFWDisposable
	{
		/// <summary></summary>
		public event EventHandler ColorPicked;

		private ToolStripButton m_autoItem;
		private ToolStripMenuItem m_moreItem;
		private ColorPickerMatrix m_colorMatrix;
		private Color m_currColor;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Encapsulates a color picker drop-down almost just like Word 2003's.
		/// </summary>
		/// <param name="fShowUnspecified">if set to <c>true</c> control will include a button
		/// for the "automatic" choice (i.e., not explicitly specified).</param>
		/// <param name="selectedColor">Initial color to select.</param>
		/// ------------------------------------------------------------------------------------
		public ColorPickerDropDown(bool fShowUnspecified, Color selectedColor)
		{
			LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;

			if (fShowUnspecified)
			{
				// Add the "Automatic" button.
				m_autoItem = new ToolStripButton(ColorPickerStrings.kstidUnspecifiedText);
				m_autoItem.TextAlign = ContentAlignment.MiddleCenter;
				m_autoItem.Click += new EventHandler(m_autoItem_Click);
				m_autoItem.Margin = new Padding(1, m_autoItem.Margin.Top,
					m_autoItem.Margin.Right, m_autoItem.Margin.Bottom);

				Items.Add(m_autoItem);
			}

			// Add all the colored squares.
			m_colorMatrix = new ColorPickerMatrix();
			m_colorMatrix.ColorPicked += new EventHandler(m_colorMatrix_ColorPicked);
			ToolStripControlHost host = new ToolStripControlHost(m_colorMatrix);
			host.AutoSize = false;
			host.Size = new Size(m_colorMatrix.Width + 6, m_colorMatrix.Height + 6);
			host.Padding = new Padding(3);
			Items.Add(host);

			// Add the "More Colors..." button.
			m_moreItem = new ToolStripMenuItem(ColorPickerStrings.kstidMoreColors);
			m_moreItem.TextAlign = ContentAlignment.MiddleCenter;
			m_moreItem.Click += new EventHandler(m_moreItem_Click);
			Items.Add(m_moreItem);

			CurrentColor = selectedColor;
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
		/// Gets or sets the drop-down's current color. Color.Empty is equivalent to the
		/// automatic value.
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
				m_colorMatrix.CurrentColor = value;
				if (m_autoItem != null)
					m_autoItem.Checked = (value == Color.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a color change from clicking on one of the colored squares.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void m_colorMatrix_ColorPicked(object sender, EventArgs e)
		{
			m_currColor = m_colorMatrix.CurrentColor;

			Hide();

			if (ColorPicked != null)
				ColorPicked(this, EventArgs.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the color dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void m_moreItem_Click(object sender, EventArgs e)
		{
			Hide();

			using (ColorDialog dlg = new ColorDialog())
			{
				dlg.FullOpen = true;

				if (dlg.ShowDialog() == DialogResult.OK)
				{
					CurrentColor = dlg.Color;
					if (ColorPicked != null)
						ColorPicked(this, EventArgs.Empty);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void m_autoItem_Click(object sender, EventArgs e)
		{
			CurrentColor = Color.Empty;

			Hide();

			if (ColorPicked != null)
				ColorPicked(this, EventArgs.Empty);
		}
	}
}
