// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Drawing;
using SIL.Utils; // for ImageCollection


namespace XCore
{

	class PanelMenu : PanelEx
	{
		private IImageCollection m_images;
		private IUIMenuAdapter m_menuBarAdapter;
		private XCore.ChoiceGroup m_group;


		public PanelMenu(XCore.ChoiceGroup group, IImageCollection images, XCore.IUIMenuAdapter menuBarAdapter):base()
		{
			m_group = group;
			m_images = images;
			m_menuBarAdapter = menuBarAdapter;

			this.Dock = System.Windows.Forms.DockStyle.Right;
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.Location = new System.Drawing.Point(576, 2);
			this.Name = "panel1";
			this.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.Size = new System.Drawing.Size(16, 16);


			this.Click +=new EventHandler(PanelMenu_Click);
			this.TabIndex = 0;

			//	this.Click += new EventHandler(m_infoBarButton_Click);
			this.Tag = group;

			Display();
			//	UpdateInfoBarButtonImage();
		}

		/// <summary>
		/// is what we are displaying affected by this XCore property?
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool IsRelatedProperty(string name)
		{
			//for now, only handles Boolean properties
			BoolPropertyChoice choice = this.Tag as XCore.BoolPropertyChoice;
			if (choice == null)
				return false;

			return choice.BoolPropertyName == name;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Image is a reference")]
		private void Display()
		{
			UIItemDisplayProperties display = m_group.GetDisplayProperties();
			System.Diagnostics.Debug.Assert(display.ImageLabel != null,"need an image for this menu");
			Image i = m_images.GetImage(display.ImageLabel);
			this.BackgroundImage = i;
			this.BackgroundImageLayout = ImageLayout.Center;
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
			Point location = this.Parent.PointToScreen(this.Location);
			location.Y += Height;
			m_menuBarAdapter.ShowContextMenu(m_group,
				location,
				null, // Don't have an XCore colleague,
				null); // or MessageSequencer

		}
	}
}
