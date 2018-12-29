// SilSidePane, Copyright 2008-2019 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LanguageExplorer.Controls.SilSidePane
{
	/// <summary />
	internal class OutlookBarPanelRenderer : ToolStripRenderer
	{
		/// <summary />
		protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
		{
			var button = e.Item as ToolStripButton;
			if (button == null)
			{
				return;
			}
			Brush br = null;
			Pen pen = null;
			var rc = button.ContentRectangle;
			try
			{
				if (button.Pressed)
				{
					pen = new Pen(ProfessionalColors.ButtonPressedBorder);
					br = new LinearGradientBrush(rc, ProfessionalColors.ButtonPressedGradientBegin, ProfessionalColors.ButtonPressedGradientEnd, LinearGradientMode.Vertical);
				}
				else if (button.Checked)
				{
					pen = new Pen(ProfessionalColors.ButtonCheckedHighlightBorder);
					br = new LinearGradientBrush(rc, ProfessionalColors.ButtonCheckedGradientBegin, ProfessionalColors.ButtonCheckedGradientEnd, LinearGradientMode.Vertical);
				}
				else if (button.Selected)
				{
					pen = new Pen(ProfessionalColors.ButtonSelectedBorder);
					br = new LinearGradientBrush(rc, ProfessionalColors.ButtonSelectedGradientBegin, ProfessionalColors.ButtonSelectedGradientEnd, LinearGradientMode.Vertical);
				}
				else
				{
					return;
				}
				// Fill the item's background
				e.Graphics.FillRectangle(br, rc);
				// Draw the item's border
				rc.Width--;
				rc.Height--;
				e.Graphics.DrawRectangle(pen, rc);
			}
			finally
			{
				br?.Dispose();
				pen?.Dispose();
			}
		}

		/// <summary />
		protected override void OnRenderOverflowButtonBackground(ToolStripItemRenderEventArgs e)
		{
			using (Image arrow = LanguageExplorerResources.DropDown2003.ToBitmap())
			{
				var rc = Rectangle.Empty;
				rc.Width = arrow.Width + 12;
				rc.Height = e.Item.Height;
				rc.X = e.Item.Width - rc.Width - 2;
				rc.Y = (int)(((decimal)e.Item.Height - rc.Height) / 2);
				if (e.Item.Selected)
				{
					using (var br = new LinearGradientBrush(rc, ProfessionalColors.ButtonSelectedGradientBegin, ProfessionalColors.ButtonSelectedGradientEnd, LinearGradientMode.Vertical))
					{
						e.Graphics.FillRectangle(br, rc);
					}
				}
				e.Graphics.DrawImageUnscaled(arrow, rc.X + 6, rc.Y + 2);
			}
		}

		/// <summary />
		protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
		{
			try
			{
				var rc = e.Item.ContentRectangle;
				rc.X = (int)((decimal)(rc.Width - e.Image.Width) / 2);
				rc.Y += 5;
				rc.Width = e.Image.Width;
				rc.Height = e.Image.Height;
				e.Graphics.DrawImageUnscaled(e.Image, rc);
			}
			catch (Exception exception)
			{
				Debug.WriteLine($"Warning: Exception ignored in OutlookBarPanelRenderer.OnRenderItemImage: {exception}");
			}
		}

		/// <summary />
		protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
		{
			e.TextFormat |= TextFormatFlags.EndEllipsis;
			e.TextFormat |= TextFormatFlags.VerticalCenter;
			e.TextFormat |= TextFormatFlags.WordBreak;
			e.TextFormat &= ~TextFormatFlags.SingleLine;
			if (e.Item.Image != null)
			{
				var rc = e.TextRectangle;
				rc.Y = e.Item.Image.Height + 10;
				rc.Height = e.Item.Font.Height * 2;
				e.TextRectangle = rc;
			}
			base.OnRenderItemText(e);
		}

		/// <summary />
		protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
		{
			using (var br = new LinearGradientBrush(e.ToolStrip.ClientRectangle, ProfessionalColors.ToolStripGradientBegin, ProfessionalColors.ToolStripGradientEnd, LinearGradientMode.Vertical))
			{
				e.Graphics.FillRectangle(br, e.ToolStrip.ClientRectangle);
			}
		}

		/// <summary />
		protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
		{
			var rc = e.ToolStrip.ClientRectangle;
			rc.Width--;
			rc.Height--;
			using (var pen = new Pen(ProfessionalColors.ToolStripBorder))
			{
				e.Graphics.DrawRectangle(pen, rc);
			}
		}
	}
}