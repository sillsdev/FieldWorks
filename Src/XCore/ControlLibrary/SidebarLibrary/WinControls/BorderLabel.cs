using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace SidebarLibrary.WinControls
{
	/// <summary>
	/// Summary description for BorderLabel.
	/// </summary>
	public class BorderLabel : Label
	{
		Pen pen = null;
		Pen hoverPen = null;
		bool highlight = false;
		//int width = -1;
		int gap = 0;

		public BorderLabel(Color BorderColor, Color HoverColor): this(BorderColor, HoverColor, 1)
		{
		}

		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="Added REVIEW-Linux comment")]
		public BorderLabel(Color BorderColor, Color HoverColor, int Width)
		{
			highlight = true;
			//width = Width;
			pen = new Pen(BorderColor, Width);
			hoverPen = new Pen(HoverColor, Width);
			// REVIEW-Linux: Libgdiplus doesn't use the Alignment property for rendering.
			pen.Alignment = PenAlignment.Inset;
			hoverPen.Alignment = PenAlignment.Inset;
			if ( Width == 1 )
				gap = 1;
		}

		public BorderLabel(Color BorderColor): this(BorderColor, 1)
		{
		}

		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="Added REVIEW-Linux comment")]
		public BorderLabel(Color BorderColor, int Width)
		{
			highlight = false;
			//width = Width;
			pen = new Pen(BorderColor, Width);
			// REVIEW-Linux: Libgdiplus doesn't use the Alignment property for rendering.
			pen.Alignment = PenAlignment.Inset;
			if ( Width == 1 )
				gap = 1;
		}

		override protected void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			// Draw a border around the label
			Rectangle rc = Bounds;
			e.Graphics.DrawRectangle(pen, 0, 0, rc.Width-gap, rc.Height-gap);
		}

		override protected void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if ( highlight )
			{
				using (Graphics g = CreateGraphics())
				{
					Rectangle rc = Bounds;
					g.DrawRectangle(hoverPen, 0, 0, rc.Width-gap, rc.Height-gap);
				}
			}
		}


		override protected void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			if ( highlight )
			{
				using (Graphics g = CreateGraphics())
				{
					Rectangle rc = Bounds;
					g.DrawRectangle(pen, 0, 0, rc.Width-gap, rc.Height-gap);
				}
			}
		}

	}

}
