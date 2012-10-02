// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Form2.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;

namespace HelloWorldSideBar
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for Form2.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class Form2 : System.Windows.Forms.Form
	{
		private SIL.FieldWorks.Common.Controls.SideBar m_sideBar;
		private SideBarButton m_btn1;
		private SideBarButton m_btn2;
		private SideBarButton m_btn3;
		private SideBarButton m_btn4;
		private SideBarButton m_btn5;
		private System.Windows.Forms.Label m_selectedButtons;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Form2"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Form2(): this(null, null)
		{
		}

		public Form2(ImageList largeImages, ImageList smallImages)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			// Set properties for sidebar
			m_sideBar.ImageListLarge = largeImages;
			m_sideBar.ImageListSmall = smallImages;
			m_sideBar.Persist = false;

			// Create first side bar tab - each button has own click handler method
			SideBarTab tab = new SideBarTab();
			m_sideBar.Tabs.Add(tab);
			tab.Title = "Tab";

			// First button on first tab
			m_btn1 = new SideBarButton();
			m_btn1.Text = "First button";
			m_btn1.Click += new EventHandler(OnButton1Click);
			m_btn1.ImageIndex = 1;
			tab.Buttons.Add(m_btn1);

			// Second button on first tab
			m_btn2 = new SideBarButton();
			m_btn2.Text = "Second button";
			m_btn2.Click += new EventHandler(OnButton2Click);
			m_btn2.ImageIndex = 2;
			tab.Buttons.Add(m_btn2);

			// Second side bar tab - one click handler method for all buttons
			// This tab supports multiple selections with an off button
			tab = new SideBarTab();
			m_sideBar.Tabs.Add(tab);
			tab.Title = "Other tab";
			tab.MultipleSelections = true;
			tab.FirstButtonExclusive = true;
			tab.ButtonClickEvent += new EventHandler(OnGenericButtonClick);

			// First button on second tab
			m_btn3 = new SideBarButton();
			m_btn3.Text = "Off";
			m_btn3.ImageIndex = 3;
			tab.Buttons.Add(m_btn3);

			// Second button on second tab
			m_btn4 = new SideBarButton();
			m_btn4.Text = "Button 2";
			m_btn4.ImageIndex = 4;
			tab.Buttons.Add(m_btn4);

			// Third button on second tab
			m_btn5 = new SideBarButton();
			m_btn5.Text = "Button 3";
			m_btn5.ImageIndex = 5;
			tab.Buttons.Add(m_btn5);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.m_sideBar = new SIL.FieldWorks.Common.Controls.SideBar();
			this.m_selectedButtons = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// m_sideBar
			//
			this.m_sideBar.DockPadding.All = 2;
			this.m_sideBar.Location = new System.Drawing.Point(0, 0);
			this.m_sideBar.Name = "m_sideBar";
			this.m_sideBar.Size = new System.Drawing.Size(85, 266);
			this.m_sideBar.TabIndex = 0;
			//
			// m_selectedButtons
			//
			this.m_selectedButtons.Location = new System.Drawing.Point(104, 72);
			this.m_selectedButtons.Name = "m_selectedButtons";
			this.m_selectedButtons.Size = new System.Drawing.Size(168, 88);
			this.m_selectedButtons.TabIndex = 1;
			//
			// Form2
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(292, 266);
			this.Controls.Add(this.m_selectedButtons);
			this.Controls.Add(this.m_sideBar);
			this.Name = "Form2";
			this.ShowInTaskbar = false;
			this.Text = "Form2";
			this.ResumeLayout(false);

		}
		#endregion

		private void OnButton1Click(object sender, EventArgs e)
		{
			MessageBox.Show("First button clicked");
		}
		private void OnButton2Click(object sender, EventArgs e)
		{
			MessageBox.Show("2nd button clicked");
		}

		private void OnGenericButtonClick(object sender, EventArgs e)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.Append("Clicked button: ");
			bldr.Append(((SideBarButton)sender).Text);
			bldr.Append("; Selected buttons: ");
			foreach (SideBarButton btn in m_sideBar.Tabs[1].Selection)
			{
				bldr.Append(btn.Text);
				bldr.Append("; ");
			}
			m_selectedButtons.Text = bldr.ToString();
		}
	}
}
