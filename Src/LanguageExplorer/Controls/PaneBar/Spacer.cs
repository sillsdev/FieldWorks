// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.PaneBar
{
	internal class Spacer : PanelExtension
	{
		public Spacer()
		{
			Dock = System.Windows.Forms.DockStyle.Right;
			//this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			Location = new System.Drawing.Point(545, 2);
			Name = "spacer";
			Size = new System.Drawing.Size(16, 20);
			Anchor = System.Windows.Forms.AnchorStyles.None;
			TabIndex = 0;
		}
	}
}
