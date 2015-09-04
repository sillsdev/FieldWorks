// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace LanguageExplorer.Controls.PaneBar
{
	/// <summary>
	/// Extends the Panel class for use by PaneBar.
	/// </summary>
	internal class PanelExtension : Panel
	{
		private int? m_widthOfLeftDockedControls = null;

		/// <summary>
		/// Constructor.
		/// </summary>
		internal PanelExtension()
		{
			Text = string.Empty;
			ControlAdded += HandleControlAdded;
		}

		private void HandleControlAdded(object sender, ControlEventArgs e)
		{
			m_widthOfLeftDockedControls = null;
		}

		/// <summary>
		/// sets m_widthOfLeftDockedControls
		/// </summary>
		void CalculateWidthOfLeftDockedControls()
		{
			m_widthOfLeftDockedControls = 0;

			foreach (var control in Controls.Cast<Control>()
					.Where(c => (c.Dock & DockStyle.Left) != 0)
					.Where(c => m_widthOfLeftDockedControls < c.Left + c.Width))
			{
				m_widthOfLeftDockedControls = control.Left + control.Width;
			}
		}

		/// <summary />
		/// <param name="e"></param>
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			base.OnPaintBackground (e);

			var rectangleToPaint = ClientRectangle;
			if (rectangleToPaint.Width <= 0 || rectangleToPaint.Height <= 0)
			{
				return; // can't draw anything, and will crash if we try
}
			var beginColor = Color.FromArgb(0x58, 0x80, 0xd0);
			var endColor = Color.FromArgb(0x08, 0x40, 0x98);

			using (var brush = new LinearGradientBrush(rectangleToPaint, beginColor, endColor, LinearGradientMode.Vertical))
			{
				e.Graphics.FillRectangle(brush, rectangleToPaint);
			}

			// Draw a background image if we have one set.
			// This code assumes it is always centered.

			var backgroundImage = BackgroundImage;
			if (backgroundImage == null)
			{
				return;
			}
			var drawRect = new Rectangle
			{
				Location = new Point(ClientSize.Width / 2 - backgroundImage.Width / 2, ClientSize.Height / 2 - backgroundImage.Height / 2),
				Size = backgroundImage.Size
			};
			e.Graphics.DrawImage(backgroundImage, drawRect);
		}

		/// <summary />
		/// <param name="e"></param>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate(); // need to redraw in the full new width
		}

		/// <summary />
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			using(var brush = new SolidBrush(Color.White))
			{
				if (m_widthOfLeftDockedControls == null)
				{
					CalculateWidthOfLeftDockedControls();
				}

				e.Graphics.DrawString(Text, Font, brush, (int)m_widthOfLeftDockedControls + 2, 0);
				base.OnPaint(e);
			}
		}
	}
}
