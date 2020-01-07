// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace LCMBrowser
{
	/// <summary />
	public partial class ColorPicker : UserControl
	{
		/// <summary />
		public delegate void ColorPickedHandler(object sender, Color clrPicked);
		/// <summary />
		public event ColorPickedHandler ColorPicked;

		private Dictionary<Color, ToolStripButton> m_colorButtons;
		private Color m_clrSelected;

		/// <summary />
		public ColorPicker()
		{
			InitializeComponent();

			tsColors.Height = Height + 2;

			var colors = new List<Color>();
			foreach (KnownColor kclr in Enum.GetValues(typeof(KnownColor)))
			{
				var clr = Color.FromKnownColor(kclr);
				if (!clr.IsSystemColor && clr != Color.Transparent)
				{
					colors.Add(clr);
				}
			}

			// Sort by RGB value.
			colors.Sort((c1, c2) => c1.ToArgb().CompareTo(c2.ToArgb()));
			m_colorButtons = new Dictionary<Color, ToolStripButton>();

			foreach (var clr in colors)
			{
				var bldr = new StringBuilder();
				foreach (var c in clr.Name)
				{
					if (char.IsUpper(c))
					{
						bldr.Append(' ');
					}
					bldr.Append(c);
				}

				var btn = new ToolStripButton
				{
					AutoSize = false,
					DisplayStyle = ToolStripItemDisplayStyle.None,
					Margin = new Padding(0),
					Size = new Size(15, 15),
					ToolTipText = $"{bldr.ToString().Trim()} ({clr.ToArgb() & 0x00FFFFFF:X6})",
					Tag = clr,
					BackColor = clr
				};
				btn.Paint += HandleButtonPaint;
				btn.Click += HandleButtonClick;
				tsColors.Items.Add(btn);
				m_colorButtons[clr] = btn;
			}
		}

		/// <summary>
		/// Gets the list of all colors from which to choose.
		/// </summary>
		public List<Color> Colors
		{
			get
			{
				var list = new List<Color>();
				foreach (var clr in m_colorButtons.Keys)
				{
					list.Add(clr);
				}
				return list;
			}
		}

		/// <summary>
		/// Gets or sets the current color.
		/// </summary>
		public Color SelectedColor
		{
			get { return m_clrSelected; }
			set
			{
				m_clrSelected = value;
				foreach (var btn in m_colorButtons.Values)
				{
					btn.Checked = false;
				}
				ToolStripButton clrBtn;
				if (m_colorButtons.TryGetValue(m_clrSelected, out clrBtn))
				{
					clrBtn.Checked = true;
				}
			}
		}

		/// <summary>
		/// Handles the button click.
		/// </summary>
		private void HandleButtonClick(object sender, EventArgs e)
		{
			var btn = sender as ToolStripButton;
			SelectedColor = (Color)btn.Tag;
			ColorPicked?.Invoke(this, SelectedColor);
		}

		/// <summary>
		/// Handles the button paint.
		/// </summary>
		private static void HandleButtonPaint(object sender, PaintEventArgs e)
		{
			var btn = sender as ToolStripButton;
			var rc = btn.ContentRectangle;
			if (!btn.Checked && !btn.Selected)
			{
				rc.Inflate(1, 1);
			}
			else if (btn.Selected)
			{
				rc.Inflate(-1, -1);
			}
			using (var br = new SolidBrush((Color)btn.Tag))
			{
				e.Graphics.FillRectangle(br, rc);
			}
			if (!btn.Checked && !btn.Selected)
			{
				e.Graphics.DrawRectangle(SystemPens.ControlDark, rc);
			}
		}
	}
}