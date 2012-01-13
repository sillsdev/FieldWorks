using System;
using System.Windows.Forms;
using System.Drawing;
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
			this.Font = new System.Drawing.Font("Tahoma", 13F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.Location = new System.Drawing.Point(576, 2);
			this.Name = "panelEx1";
			this.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.Size = new System.Drawing.Size(120, 20);

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
			XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass)this.Tag;
			UIItemDisplayProperties display = choice.GetDisplayProperties();

			const int checkBoxWidth = 17;
			string s = display.Text.Replace("_", "&");
			this.Text = s;

			using (Graphics g = this.CreateGraphics())
			{
				int labelWidth = (int)(g.MeasureString(s + "_", this.Font).Width);
				this.Width = labelWidth;
			}

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


			if(display.ImageLabel != null && display.ImageLabel !="" && display.ImageLabel !="default")
			{

				PanelEx p = new PanelEx();
				Image i = m_images.GetImage(display.ImageLabel);
				p.BackgroundImage = i;
				p.BackgroundImageLayout = ImageLayout.Center;
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
			using (new WaitCursor(Form.ActiveForm))
			{
				XCore.ChoiceBase c = (XCore.ChoiceBase )this.Tag;
				c.OnClick(this, null);
			}
		}

		private void panelButton_MouseEnter(object sender, EventArgs e)
		{
			mouseOverControl = true;

			XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass) this.Tag;
			UIItemDisplayProperties display = choice.GetDisplayProperties();


			this.Refresh();
		}

		private void panelButton_MouseDown(object sender, MouseEventArgs e)
		{
			XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass) this.Tag;
			UIItemDisplayProperties display = choice.GetDisplayProperties();


			this.Refresh();
		}

		private void panelButton_MouseLeave(object sender, EventArgs e)
		{
			mouseOverControl = false;

			XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass) this.Tag;
			UIItemDisplayProperties display = choice.GetDisplayProperties();


			this.Refresh();
		}
	}
}
