// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

namespace LanguageExplorer.Controls.PaneBar
{
	internal class PanelButton : PanelExtension
	{
		private bool mouseOverControl = false;

		public PanelButton()
		{
			Dock = DockStyle.Right;
			Font = new Font("Tahoma", 13F, FontStyle.Regular, GraphicsUnit.Point, 0);
			Location = new Point(576, 2);
			Name = "panelEx1";
			Anchor = AnchorStyles.None;
			Size = new Size(120, 20);

			MouseEnter += panelButton_MouseEnter;
			MouseLeave += panelButton_MouseLeave;
			MouseDown += panelButton_MouseDown;
			Click += PanelButton_Click;
			TabIndex = 0;

			//Tag = choice;
			SetLabel();
		}

		/// <summary>
		/// Is what we are displaying affected by Property: <paramref name="name"/>?
		/// </summary>
		/// <param name="name">Property name to be checked.</param>
		/// <returns></returns>
		public bool IsRelatedProperty(string name)
		{
#if RANDYTODO
			//for now, only handles Boolean properties
			BoolPropertyChoice choice = this.Tag as XCore.BoolPropertyChoice;
			if (choice == null)
				return false;

			return choice.BoolPropertyName == name;
#else
			return false;
#endif
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Controls added to collection")]
		private void SetLabel()
		{
#if RANDYTODO
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
			if (mouseOverControl)
				this.panelButton_MouseEnter(null, null);
			else
				this.panelButton_MouseLeave(null, null);

			this.Controls.Clear(); // Clear out any previous checkboxes and images

			// Add in a checkbox that reflects the "checked" status of the button
			CheckBox checkBox = new CheckBox();
			checkBox.Checked = display.Checked;
			checkBox.Click += new EventHandler(PanelButton_Click);
			checkBox.Location = new Point(0, 0);
			checkBox.Anchor = System.Windows.Forms.AnchorStyles.Left;
			checkBox.Dock = System.Windows.Forms.DockStyle.Left;
			checkBox.Width = checkBoxWidth;
			checkBox.MouseEnter += new EventHandler(panelButton_MouseEnter);
			checkBox.MouseLeave += new EventHandler(panelButton_MouseLeave);
			checkBox.MouseDown += new MouseEventHandler(panelButton_MouseDown);
			checkBox.BackColor = Color.Transparent;
			this.Controls.Add(checkBox);

			this.Width += checkBox.Width;


			if (display.ImageLabel != null && display.ImageLabel != "" && display.ImageLabel != "default")
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
#endif
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
#if RANDYTODO
			using (new WaitCursor(Form.ActiveForm))
			{
				XCore.ChoiceBase c = (XCore.ChoiceBase)this.Tag;
				c.OnClick(this, null);
			}
#endif
		}

		private void panelButton_MouseEnter(object sender, EventArgs e)
		{
			mouseOverControl = true;

#if RANDYTODO
			XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass)this.Tag;
			UIItemDisplayProperties display = choice.GetDisplayProperties();
#endif

			Refresh();
		}

		private void panelButton_MouseDown(object sender, MouseEventArgs e)
		{
#if RANDYTODO
			XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass)this.Tag;
			UIItemDisplayProperties display = choice.GetDisplayProperties();
#endif

			Refresh();
		}

		private void panelButton_MouseLeave(object sender, EventArgs e)
		{
			mouseOverControl = false;

#if RANDYTODO
			XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass)this.Tag;
			UIItemDisplayProperties display = choice.GetDisplayProperties();
#endif

			Refresh();
		}
	}
}