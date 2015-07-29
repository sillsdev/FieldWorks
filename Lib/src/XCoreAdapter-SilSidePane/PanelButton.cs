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

	class PanelButton : PanelEx
	{
		private ImageCollection m_images;
		private bool mouseOverControl = false;

		public PanelButton(XCore.ChoiceBase choice, ImageCollection images):base()
		{
			m_images = images;

			this.Dock = System.Windows.Forms.DockStyle.Right;
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.Location = new System.Drawing.Point(576, 2);
			this.Name = "panelEx1";
			this.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.Size = new System.Drawing.Size(120, 20);
#if USE_DOTNETBAR
			this.Style.Alignment = System.Drawing.StringAlignment.Center;
			this.Style.GradientAngle = 90;
#endif

			this.MouseEnter += new EventHandler(panelButton_MouseEnter);
			this.MouseLeave += new EventHandler(panelButton_MouseLeave);
			this.MouseDown += new MouseEventHandler(panelButton_MouseDown);

			this.Click += new EventHandler(PanelButton_Click);
			this.TabIndex = 0;

			this.Tag = choice;
			SetLabel();
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

		private void SetLabel()
		{
			XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass) this.Tag;
			UIItemDisplayProperties display = choice.GetDisplayProperties();

			const int checkBoxWidth = 17;
			string s = display.Text.Replace("_", "&");
			this.Text = s;
			Graphics g = this.CreateGraphics();
			int labelWidth = (int)(g.MeasureString(s + "_", this.Font).Width);
			this.Width = labelWidth;
#if USE_DOTNETBAR
			this.Style.Alignment =  System.Drawing.StringAlignment.Center;

			if(display.Checked)
			{
				this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedBackground;
				this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedBackground2;
				this.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedText;
			}
			else
			{
				this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground;
				this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground2;
				this.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelText;
			}
#endif

			// Simulate a mouse enter or leave event to get the correct highlighting
			if(mouseOverControl)
				this.panelButton_MouseEnter(null, null);
			else
				this.panelButton_MouseLeave(null, null);

			this.Controls.Clear(); // Clear out any previous checkboxes and images

			// Add in a checkbox that reflects the "checked" status of the button
			CheckBox checkBox = new CheckBox();
			checkBox.Checked = display.Checked;
			checkBox.Click +=new EventHandler(PanelButton_Click);
			checkBox.Location = new Point(0,0);
			checkBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			checkBox.Dock = System.Windows.Forms.DockStyle.Left;
			checkBox.Width = checkBoxWidth;
			checkBox.MouseEnter += new EventHandler(panelButton_MouseEnter);
			checkBox.MouseLeave += new EventHandler(panelButton_MouseLeave);
			checkBox.MouseDown += new MouseEventHandler(panelButton_MouseDown);
			checkBox.BackColor = Color.Transparent;
			this.Controls.Add(checkBox);

			this.Width += checkBox.Width;

#if USE_DOTNETBAR
			this.Style.Alignment =  System.Drawing.StringAlignment.Far;
#endif

			if(display.ImageLabel != null && display.ImageLabel !="" && display.ImageLabel !="default")
			{

				PanelEx p = new PanelEx();
				Image i = m_images.GetImage(display.ImageLabel);
#if USE_DOTNETBAR
				p.Style.BackgroundImage = i;
				p.Style.BackgroundImagePosition = eBackgroundImagePosition.Center;
#else
				p.BackgroundImage = i;
				p.BackgroundImageLayout = ImageLayout.Center;
#endif
				p.Location = new Point(checkBox.Width, 0);
				p.Anchor = System.Windows.Forms.AnchorStyles.Left;
				p.Dock = System.Windows.Forms.DockStyle.None;
				p.Size = new Size(17, this.Height);
				this.Width += p.Size.Width;
				this.Controls.Add(p);
				p.Click += new EventHandler(PanelButton_Click);
				p.MouseEnter += new EventHandler(panelButton_MouseEnter);
				p.MouseLeave += new EventHandler(panelButton_MouseLeave);
				p.MouseDown += new MouseEventHandler(panelButton_MouseDown);
			}
			this.Refresh();
		}

		/// <summary>
		///
		/// </summary>
		public void UpdateDisplay()
		{
			SetLabel();
		}

		private void PanelButton_Click(object sender, EventArgs e)
		{
			Cursor.Current = Cursors.WaitCursor;
			XCore.ChoiceBase c = (XCore.ChoiceBase )this.Tag;
			c.OnClick(this, null);
			Cursor.Current = Cursors.Default;
		}

		private void panelButton_MouseEnter(object sender, EventArgs e)
		{
			mouseOverControl = true;

			XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass) this.Tag;
			UIItemDisplayProperties display = choice.GetDisplayProperties();

			if(display.Checked)
			{
#if USE_DOTNETBAR
				this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedBackground2;
				this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedBackground;
#endif
			}
			else
			{
#if USE_DOTNETBAR
				this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemHotBackground;
				this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemHotBackground2;
				this.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemHotText;
#endif
			}

			this.Refresh();
		}

		private void panelButton_MouseDown(object sender, MouseEventArgs e)
		{
			XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass) this.Tag;
			UIItemDisplayProperties display = choice.GetDisplayProperties();

#if USE_DOTNETBAR
			this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemPressedBackground;
			this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemPressedBackground2;
#endif

			this.Refresh();
		}

		private void panelButton_MouseLeave(object sender, EventArgs e)
		{
			mouseOverControl = false;

			XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass) this.Tag;
			UIItemDisplayProperties display = choice.GetDisplayProperties();

			if(display.Checked)
			{
#if USE_DOTNETBAR
				this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedBackground;
				this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedBackground2;
#endif
			}
			else
			{
#if USE_DOTNETBAR
				this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground;
				this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground2;
				this.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelText;
#endif
			}

			this.Refresh();
		}
	}
}
