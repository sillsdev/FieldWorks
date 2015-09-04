// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

namespace LanguageExplorer.Controls.PaneBar
{
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

			//Tag = group;

			Display();
		}

		/// <summary>
		/// Is what we are displaying affected by Property: <paramref name="name"/>?
		/// </summary>
		/// <param name="name">Property name to be checked.</param>
		/// <returns></returns>
		public bool IsRelatedProperty(string name)
		{
#if RANDYTODO
			// for now, only handles Boolean properties
			BoolPropertyChoice choice = this.Tag as XCore.BoolPropertyChoice;
			if (choice == null)
				return false;

			return choice.BoolPropertyName == name;
#else
			return false;
#endif
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Image is a reference")]
		private void Display()
		{
#if RANDYTODO
			// TODO: Replace assert with exception, after display is replaced.
			UIItemDisplayProperties display = m_group.GetDisplayProperties();
			System.Diagnostics.Debug.Assert(display.ImageLabel != null, "need an image for this menu");
			Image i = m_images.GetImage(display.ImageLabel);
			this.BackgroundImage = i;
			this.BackgroundImageLayout = ImageLayout.Center;
#endif
		}

		/// <summary>
		///
		/// </summary>
		public void UpdateDisplay()
		{
			//			Display();
		}

		private void PanelMenu_Click(object sender, EventArgs e)
		{
#if RANDYTODO
			Point location = this.Parent.PointToScreen(this.Location);
			location.Y += Height;
			m_menuBarAdapter.ShowContextMenu(m_group,
				location,
				null, // Don't have an XCore colleague,
				null); // or MessageSequencer
#endif
		}
	}
}