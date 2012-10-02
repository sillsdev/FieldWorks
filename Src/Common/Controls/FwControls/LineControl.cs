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
// File: LineControl.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Control that draws a line
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class LineControl : UserControl
	{
		private Color m_foreColor2 = Color.Transparent;
		private Brush m_brush;
		private LinearGradientMode m_gradientMode = LinearGradientMode.Horizontal;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LineControl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LineControl()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the brush.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void UpdateBrush()
		{
			if (m_brush != null)
				m_brush.Dispose();

			m_brush = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the foreground color of the control.
		/// </summary>
		/// <value></value>
		/// <returns>The foreground <see cref="T:System.Drawing.Color"></see> of the control.
		/// The default is the value of the
		/// <see cref="P:System.Windows.Forms.Control.DefaultForeColor"></see> property.</returns>
		/// <PermissionSet><IPermission class="System.Security.Permissions.FileIOPermission,
		/// mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
		/// version="1" Unrestricted="true"/></PermissionSet>
		/// ------------------------------------------------------------------------------------
		public override Color ForeColor
		{
			get { return base.ForeColor; }
			set
			{
				base.ForeColor = value;
				UpdateBrush();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the fore color2.
		/// </summary>
		/// <value>The fore color2.</value>
		/// ------------------------------------------------------------------------------------
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the linear gradient mode.
		/// </summary>
		/// <value>The linear gradient mode.</value>
		/// ------------------------------------------------------------------------------------
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the width for brush.
		/// </summary>
		/// <value>The width for brush.</value>
		/// ------------------------------------------------------------------------------------
		protected virtual int WidthForBrush
		{
			get { return Width; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height for brush.
		/// </summary>
		/// <value>The height for brush.</value>
		/// ------------------------------------------------------------------------------------
		protected virtual int HeightForBrush
		{
			get { return Height; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the brush.
		/// </summary>
		/// <value>The brush.</value>
		/// ------------------------------------------------------------------------------------
		protected Brush Brush
		{
			get
			{
				if (m_brush == null)
				{
					if (m_foreColor2 == Color.Transparent)
						m_brush = new SolidBrush(ForeColor);
					else
						m_brush = new LinearGradientBrush(
							new Rectangle(0, 0, WidthForBrush, HeightForBrush),
							ForeColor, ForeColor2, m_gradientMode);
					Invalidate();
				}
				return m_brush;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paints the foreground.
		/// </summary>
		/// <param name="e">The <see cref="T:System.Windows.Forms.PaintEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void PaintForeground(PaintEventArgs e)
		{
			if (m_gradientMode == LinearGradientMode.Vertical)
			{
				using (Pen linePen = new Pen(Brush, Width))
				{
					e.Graphics.DrawLine(linePen, new Point(Width / 2, 0),
						new Point(Width / 2, Height));
				}
			}
			else
			{
				using (Pen linePen = new Pen(Brush, Height))
				{
					e.Graphics.DrawLine(linePen, new Point(0, Height / 2),
						new Point(Width, Height / 2));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Paint"></see> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"></see> that
		/// contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			PaintForeground(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the resize event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the
		/// event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			UpdateBrush();
		}
	}
}
