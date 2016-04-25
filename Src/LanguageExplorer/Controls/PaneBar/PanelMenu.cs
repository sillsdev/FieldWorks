// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;

namespace LanguageExplorer.Controls.PaneBar
{
	/// <summary>
	/// PaneBar control that displays a context menu.
	/// </summary>
	internal class PanelMenu : PanelExtension
	{
		public PanelMenu()
		{
			Dock = DockStyle.Right;
			Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
			Location = new Point(576, 2);
			Name = "panelMenu";
			Anchor = AnchorStyles.None;
			Size = new Size(16, 16);

			Click += PanelMenu_Click;
			TabIndex = 0;
		}

		private void PanelMenu_Click(object sender, EventArgs e)
		{
			ContextMenuStrip.Show(this, new Point(Location.X, Location.Y + Height));
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Click -= PanelMenu_Click;
			}

			base.Dispose(disposing);
		}
	}
}