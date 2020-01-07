// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class ExpandCollapseButton : Button
	{
		private bool m_opened;

		public ExpandCollapseButton()
		{
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			BackColor = SystemColors.Window;
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			base.Dispose(disposing);
		}

		protected override Size DefaultSize => new Size(PreferredWidth, PreferredHeight);

		public bool IsOpened
		{
			get
			{
				return m_opened;
			}

			set
			{
				if (m_opened == value)
				{
					return;
				}
				m_opened = value;
				Invalidate();
			}
		}

		private VisualStyleRenderer Renderer => !Application.RenderWithVisualStyles ? null : new VisualStyleRenderer(m_opened ? VisualStyleElement.TreeView.Glyph.Opened : VisualStyleElement.TreeView.Glyph.Closed);

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);
			e.Graphics.FillRectangle(new SolidBrush(BackColor), ClientRectangle);
			var renderer = Renderer;
			if (renderer != null)
			{
				if (renderer.IsBackgroundPartiallyTransparent())
				{
					renderer.DrawParentBackground(e.Graphics, ClientRectangle, this);
				}
				renderer.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);
			}
			else
			{
				using (var boxLinePen = new Pen(SystemColors.ControlDark, 1))
				{
					e.Graphics.DrawRectangle(boxLinePen, ClientRectangle);
					var ctrY = ClientRectangle.Y + (ClientRectangle.Height / 2);
					// Draw the minus sign.
					e.Graphics.DrawLine(boxLinePen, ClientRectangle.X + 2, ctrY, ClientRectangle.X + ClientRectangle.Width - 2, ctrY);
					if (!m_opened)
					{
						// Draw the vertical part of the plus, if we are collapsed.
						var ctrX = ClientRectangle.X + (ClientRectangle.Width / 2);
						e.Graphics.DrawLine(boxLinePen, ctrX, ClientRectangle.Y + 4, ctrX, ClientRectangle.Y + ClientRectangle.Height - 4);
					}
				}
			}
		}

		public int PreferredHeight
		{
			get
			{
				var renderer = Renderer;
				if (renderer != null)
				{
					using (var g = CreateGraphics())
					{
						return renderer.GetPartSize(g, ThemeSizeType.True).Height;
					}
				}
				return 5;
			}
		}

		public int PreferredWidth
		{
			get
			{
				var renderer = Renderer;
				if (renderer != null)
				{
					using (var g = CreateGraphics())
					{
						return renderer.GetPartSize(g, ThemeSizeType.True).Width;
					}
				}
				return 11;
			}
		}

		public override void NotifyDefault(bool value)
		{
			base.NotifyDefault(false);
		}

		protected override bool ShowFocusCues => false;
	}
}