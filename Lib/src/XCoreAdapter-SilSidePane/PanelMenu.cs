// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Drawing;
#if USE_DOTNETBAR
using DevComponents.DotNetBar;
#endif
using SIL.Utils; // for ImageCollection


namespace XCore
{

	class PanelMenu : PanelEx
	{
		private ImageCollection m_images;
		private IUIMenuAdapter m_menuBarAdapter;
		private XCore.ChoiceGroup m_group;


		public PanelMenu(XCore.ChoiceGroup group, ImageCollection images, XCore.IUIMenuAdapter menuBarAdapter):base()
		{
			m_group = group;
			m_images = images;
			m_menuBarAdapter = menuBarAdapter;

			this.Dock = System.Windows.Forms.DockStyle.Right;
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.Location = new System.Drawing.Point(576, 2);
#if USE_DOTNETBAR
			this.Name = "panelEx1";
#else
			this.Name = "panel1";
#endif
			this.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.Size = new System.Drawing.Size(16, 16);
#if USE_DOTNETBAR
			this.Style.BackgroundImagePosition = eBackgroundImagePosition.Center;
#endif

//			this.StyleMouseOver.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemHotBackground;
//			this.StyleMouseOver.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemHotBackground2;
//			this.StyleMouseOver.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemHotText;

#if USE_DOTNETBAR
			this.StyleMouseOver.Border = DevComponents.DotNetBar.eBorderType.SingleLine;
			this.StyleMouseOver.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelText;
			this.StyleMouseOver.BorderWidth = 1;


			this.Style.Alignment = System.Drawing.StringAlignment.Center;
			//			this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground;
			//			this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground2;
			//			this.Style.Border = DevComponents.DotNetBar.eBorderType.SingleLine;
			//			this.Style.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBorder;
			//			this.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelText;
			this.StyleMouseOver.Alignment = StringAlignment.Center;
			this.Style.GradientAngle = 90;
#endif
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

		private void Display()
		{
			UIItemDisplayProperties display = m_group.GetDisplayProperties();
			System.Diagnostics.Debug.Assert(display.ImageLabel != null,"need an image for this menu");
			Image i = m_images.GetImage(display.ImageLabel);
#if USE_DOTNETBAR
			this.Style.BackgroundImage = i;
#else
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
			Point location = this.Parent.PointToScreen(this.Location);
			location.Y += Height;
			m_menuBarAdapter.ShowContextMenu(m_group,
				location,
				null, // Don't have an XCore colleague,
				null); // or MessageSequencer

		}
	}
}
