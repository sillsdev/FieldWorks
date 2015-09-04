// SilSidePane, Copyright 2009 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using SIL.CoreImpl.Properties;
using SIL.Utils;

namespace LanguageExplorer.Controls.SilSidePane
{
	/// <summary>
	/// Area to hold items. An OutlookButtonPanel holds items in a Tab.
	/// </summary>
	internal class OutlookButtonPanel : ToolStrip
	{
		private Size m_buttonSize;
		private int m_buttonVerticalMargin = 3;
		private IContainer components = new Container();

		/// <summary></summary>
		public OutlookButtonPanel()
		{
			AutoSize = false;
			GripStyle = ToolStripGripStyle.Hidden;
			LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
			Padding = new Padding(0, 15, 0, 5);
			ImageScalingSize = new Size(32, 32);
			Renderer = new OutlookBarPanelRenderer();

			m_buttonSize = new Size(100, 47 + Font.Height * 2);

			OverflowButton.DropDownOpening += OverflowButton_DropDownOpening;
		}

		/// <summary></summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "***** Missing Dispose() call for " + GetType() + ". *******");

			if (disposing && !IsDisposed)
			{
				if (components != null)
					components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary></summary>
		private void OverflowButton_DropDownOpening(object sender, EventArgs e)
		{
			OverflowButton.DropDown = null;
			var cmnu = components.ContextMenuStrip("contextMenu");
			foreach (ToolStripButton button in Items)
			{
				if (button.IsOnOverflow)
				{
					var mnu = new ToolStripMenuItem(button.Text)
					{
						Checked = button.Checked,
						Tag = button
					};
					mnu.Click += HandleOverflowMenuClick;
					cmnu.Items.Add(mnu);
				}
			}

			Point pt = OverflowButton.Bounds.Location;
			pt.X += OverflowButton.Width;
			pt = PointToScreen(pt);
			cmnu.Show(pt);
		}

		/// <summary></summary>
		private void HandleOverflowMenuClick(object sender, EventArgs e)
		{
			ToolStripMenuItem mnu = sender as ToolStripMenuItem;
			if (mnu != null && mnu.Tag is ToolStripButton)
				((ToolStripButton)mnu.Tag).PerformClick();
		}

		/// <summary>
		/// Gets or sets a value indicating how much margin to include above and below each
		/// sub button.
		/// </summary>
		public int SubButtonVerticalMargin
		{
			get { return m_buttonVerticalMargin; }
			set { m_buttonVerticalMargin = value; }
		}

		/// <summary></summary>
		protected override void OnItemAdded(ToolStripItemEventArgs e)
		{
			ToolStripButton button = e.Item as ToolStripButton;
			if (button != null)
			{
				button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
				button.ImageAlign = ContentAlignment.MiddleCenter;
				button.TextAlign = ContentAlignment.BottomCenter;
				button.TextImageRelation = TextImageRelation.ImageAboveText;
				button.ImageScaling = ToolStripItemImageScaling.None;
				button.AutoSize = false;
				button.Size = m_buttonSize;
				Padding margin = button.Margin;
				button.Margin = new Padding(margin.Left, margin.Top,
					margin.Right, m_buttonVerticalMargin);
			}
		}

		/// <summary></summary>
		protected override void OnItemClicked(ToolStripItemClickedEventArgs e)
		{
			base.OnItemClicked(e);

			ToolStripButton clickedButton = e.ClickedItem as ToolStripButton;
			foreach (ToolStripButton button in Items)
			{
				if (clickedButton != button)
					button.Checked = false;
			}

			if (!clickedButton.Checked)
				clickedButton.Checked = true;
		}
	}

	#region OutlookBarPanelRenderer class
	/// <summary></summary>
	internal class OutlookBarPanelRenderer : ToolStripRenderer
	{
		/// <summary></summary>
		protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
		{
			ToolStripButton button = e.Item as ToolStripButton;
			if (button == null)
				return;

			Brush br = null;
			Pen pen = null;
			Rectangle rc = button.ContentRectangle;
			try
			{
			if (button.Pressed)
			{
				pen = new Pen(ProfessionalColors.ButtonPressedBorder);
				br = new LinearGradientBrush(rc, ProfessionalColors.ButtonPressedGradientBegin,
					ProfessionalColors.ButtonPressedGradientEnd, LinearGradientMode.Vertical);
			}
			else if (button.Checked)
			{
				pen = new Pen(ProfessionalColors.ButtonCheckedHighlightBorder);
				br = new LinearGradientBrush(rc, ProfessionalColors.ButtonCheckedGradientBegin,
					ProfessionalColors.ButtonCheckedGradientEnd, LinearGradientMode.Vertical);
			}
			else if (button.Selected)
			{
				pen = new Pen(ProfessionalColors.ButtonSelectedBorder);
				br = new LinearGradientBrush(rc, ProfessionalColors.ButtonSelectedGradientBegin,
					ProfessionalColors.ButtonSelectedGradientEnd, LinearGradientMode.Vertical);
			}
			else
			{
				return;
			}

			// Fill the item's backgroud
			e.Graphics.FillRectangle(br, rc);

			// Draw the item's border
			rc.Width--;
			rc.Height--;
			e.Graphics.DrawRectangle(pen, rc);
			}
			finally
			{
				if (br != null)
			br.Dispose();
				if (pen != null)
			pen.Dispose();
		}
		}

		/// <summary></summary>
		protected override void OnRenderOverflowButtonBackground(ToolStripItemRenderEventArgs e)
		{
			using (Image arrow = Resources.DropDown2003.ToBitmap())
			{
			Rectangle rc = Rectangle.Empty;
			rc.Width = arrow.Width + 12;
			rc.Height = e.Item.Height;
			rc.X = e.Item.Width - rc.Width - 2;
			rc.Y = (int)(((decimal)e.Item.Height - rc.Height) / 2);

			if (e.Item.Selected)
			{
				using (LinearGradientBrush br = new LinearGradientBrush(rc,
					ProfessionalColors.ButtonSelectedGradientBegin,
					ProfessionalColors.ButtonSelectedGradientEnd, LinearGradientMode.Vertical))
				{
					e.Graphics.FillRectangle(br, rc);
				}
			}

			e.Graphics.DrawImageUnscaled(arrow, rc.X + 6, rc.Y + 2);
		}
		}

		/// <summary></summary>
		protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
		{
			try
			{
				Rectangle rc = e.Item.ContentRectangle;
				rc.X = (int)((decimal)(rc.Width - e.Image.Width) / 2);
				rc.Y += 5;
				rc.Width = e.Image.Width;
				rc.Height = e.Image.Height;
				e.Graphics.DrawImageUnscaled(e.Image, rc);
			}
			catch (Exception exception)
			{
				Debug.WriteLine(String.Format("Warning: Exception ignored in OutlookBarPanelRenderer.OnRenderItemImage: " + exception));
			}
		}

		/// <summary></summary>
		protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
		{
			e.TextFormat |= TextFormatFlags.EndEllipsis;
			e.TextFormat |= TextFormatFlags.VerticalCenter;
			e.TextFormat |= TextFormatFlags.WordBreak;
			e.TextFormat &= ~TextFormatFlags.SingleLine;

			if (e.Item.Image != null)
			{
				Rectangle rc = e.TextRectangle;
				rc.Y = e.Item.Image.Height + 10;
				rc.Height = e.Item.Font.Height * 2; // e.Item.ContentRectangle.Height - rc.Y;
				e.TextRectangle = rc;
			}

			base.OnRenderItemText(e);
		}

		/// <summary></summary>
		protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
		{
			using (LinearGradientBrush br = new LinearGradientBrush(e.ToolStrip.ClientRectangle,
				ProfessionalColors.ToolStripGradientBegin,
				ProfessionalColors.ToolStripGradientEnd, LinearGradientMode.Vertical))
			{
				e.Graphics.FillRectangle(br, e.ToolStrip.ClientRectangle);
			}
		}

		/// <summary></summary>
		protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
		{
			Rectangle rc = e.ToolStrip.ClientRectangle;
			rc.Width--;
			rc.Height--;

			using (Pen pen = new Pen(ProfessionalColors.ToolStripBorder))
				e.Graphics.DrawRectangle(pen, rc);
		}
	}

	#endregion
}
