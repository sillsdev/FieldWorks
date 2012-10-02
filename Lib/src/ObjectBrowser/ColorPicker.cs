using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace SIL.ObjectBrowser
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ColorPicker : UserControl
	{
		/// <summary></summary>
		public delegate void ColorPickedHandler(object sender, Color clrPicked);
		/// <summary></summary>
		public event ColorPickedHandler ColorPicked;

		private List<ToolStripButton> m_buttons;
		private Dictionary<Color, ToolStripButton> m_colorButtons;
		private Color m_clrSelected;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ColorPicker"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ColorPicker()
		{
			InitializeComponent();

			m_buttons = new List<ToolStripButton>();
			tsColors.Height = Height + 2;

			List<Color> colors = new List<Color>();
			foreach (KnownColor kclr in Enum.GetValues(typeof(KnownColor)))
			{
				Color clr = Color.FromKnownColor(kclr);
				if (!clr.IsSystemColor && clr != Color.Transparent)
					colors.Add(clr);
			}

			// Sort by RGB value.
			colors.Sort((c1, c2) => c1.ToArgb().CompareTo(c2.ToArgb()));

			m_colorButtons = new Dictionary<Color, ToolStripButton>();

			foreach (Color clr in colors)
			{
				StringBuilder bldr = new StringBuilder();
				foreach (char c in clr.Name)
				{
					if (char.IsUpper(c))
						bldr.Append(' ');
					bldr.Append(c);
				}

				ToolStripButton btn = new ToolStripButton();
				btn.AutoSize = false;
				btn.DisplayStyle = ToolStripItemDisplayStyle.None;
				btn.Margin = new Padding(0);
				btn.Size = new Size(15, 15);
				btn.ToolTipText = string.Format("{0} ({1:X6})", bldr.ToString().Trim(), clr.ToArgb() & 0x00FFFFFF);
				btn.Tag = clr;
				btn.BackColor = clr;
				btn.Paint += HandleButtonPaint;
				btn.Click += HandleButtonClick;
				tsColors.Items.Add(btn);
				m_colorButtons[clr] = btn;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the list of all colors from which to choose.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<Color> Colors
		{
			get
			{
				List<Color> list = new List<Color>();
				foreach (Color clr in m_colorButtons.Keys)
					list.Add(clr);

				return list;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the current color.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Color SelectedColor
		{
			get { return m_clrSelected; }
			set
			{
				m_clrSelected = value;
				foreach (ToolStripButton btn in m_colorButtons.Values)
					btn.Checked = false;

				ToolStripButton clrBtn;
				if (m_colorButtons.TryGetValue(m_clrSelected, out clrBtn))
					clrBtn.Checked = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the button click.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleButtonClick(object sender, EventArgs e)
		{
			ToolStripButton btn = sender as ToolStripButton;
			SelectedColor = (Color)btn.Tag;

			if (ColorPicked != null)
				ColorPicked(this, SelectedColor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the button paint.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleButtonPaint(object sender, PaintEventArgs e)
		{
			ToolStripButton btn = sender as ToolStripButton;
			Rectangle rc = btn.ContentRectangle;

			if (!btn.Checked && !btn.Selected)
				rc.Inflate(1, 1);
			else if (btn.Selected)
				rc.Inflate(-1, -1);

			using (SolidBrush br = new SolidBrush((Color)btn.Tag))
				e.Graphics.FillRectangle(br, rc);

			if (!btn.Checked && !btn.Selected)
				e.Graphics.DrawRectangle(SystemPens.ControlDark, rc);
		}
	}
}
