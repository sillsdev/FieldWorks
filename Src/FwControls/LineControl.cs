// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Control that draws a line
	/// </summary>
	public partial class LineControl : UserControl
	{
		private Color m_foreColor2 = Color.Transparent;
		private Brush m_brush;
		private LinearGradientMode m_gradientMode = LinearGradientMode.Horizontal;

		/// <summary />
		public LineControl()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Updates the brush.
		/// </summary>
		protected void UpdateBrush()
		{
			m_brush?.Dispose();

			m_brush = null;
		}

		/// <inheritdoc />
		public override Color ForeColor
		{
			get { return base.ForeColor; }
			set
			{
				base.ForeColor = value;
				UpdateBrush();
			}
		}

		/// <summary>
		/// Gets or sets the fore color2.
		/// </summary>
		[Category("Appearance")]
		[Description("If set the line will display as linear gradient")]
		public Color ForeColor2
		{
			get { return m_foreColor2; }
			set
			{
				m_foreColor2 = value;
				UpdateBrush();
			}
		}

		/// <summary>
		/// Gets or sets the linear gradient mode.
		/// </summary>
		[Category("Appearance")]
		[Description("The linear gradient mode if ForeColor2 is set")]
		public LinearGradientMode LinearGradientMode
		{
			get { return m_gradientMode; }
			set
			{
				m_gradientMode = value;
				UpdateBrush();
			}
		}

		/// <summary>
		/// Gets the width for brush.
		/// </summary>
		protected virtual int WidthForBrush => Width;

		/// <summary>
		/// Gets the height for brush.
		/// </summary>
		protected virtual int HeightForBrush => Height;

		/// <summary>
		/// Gets the brush.
		/// </summary>
		protected Brush Brush
		{
			get
			{
				if (m_brush == null)
				{
					if (m_foreColor2 == Color.Transparent)
					{
						m_brush = new SolidBrush(ForeColor);
					}
					else
					{
						m_brush = new LinearGradientBrush(new Rectangle(0, 0, WidthForBrush, HeightForBrush), ForeColor, ForeColor2, m_gradientMode);
					}

					Invalidate();
				}
				return m_brush;
			}
		}

		/// <summary>
		/// Paints the foreground.
		/// </summary>
		protected virtual void PaintForeground(PaintEventArgs e)
		{
			if (m_gradientMode == LinearGradientMode.Vertical)
			{
				using (var linePen = new Pen(Brush, Width))
				{
					e.Graphics.DrawLine(linePen, new Point(Width / 2, 0), new Point(Width / 2, Height));
				}
			}
			else
			{
				using (var linePen = new Pen(Brush, Height))
				{
					e.Graphics.DrawLine(linePen, new Point(0, Height / 2), new Point(Width, Height / 2));
				}
			}
		}

		/// <inheritdoc />
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			PaintForeground(e);
		}

		/// <inheritdoc />
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			UpdateBrush();
		}
	}
}