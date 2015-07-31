// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using SIL.FieldWorks.Common.Controls;

namespace HelloWorldSideBar
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
		private SIL.FieldWorks.Common.Controls.SideBar sideBar1;
		private System.Windows.Forms.MainMenu mainMenu1;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem menuItem2;
		private System.Windows.Forms.MenuItem menuItem3;
		private System.Windows.Forms.ImageList imageListSmall;
		private System.Windows.Forms.ImageList imageListLarge;
		private SIL.FieldWorks.Common.Controls.SideBarTab sideBarTab1;
		private SIL.FieldWorks.Common.Controls.SideBarButton sideBarButton1;
		private SIL.FieldWorks.Common.Controls.SideBarButton sideBarButton2;
		private SIL.FieldWorks.Common.Controls.SideBarButton sideBarButton3;
		private SIL.FieldWorks.Common.Controls.SideBarTab sideBarTab2;
		private SIL.FieldWorks.Common.Controls.SideBarTab sideBarTab3;
		private SIL.FieldWorks.Common.Controls.SideBarButton sideBarButton4;
		private SIL.FieldWorks.Common.Controls.SideBarButton sideBarButton5;
		private SIL.FieldWorks.Common.Controls.SideBarButton sideBarButton6;
		private System.Windows.Forms.ContextMenu contextMenu1;
		private System.Windows.Forms.MenuItem menuItem4;
		private System.Windows.Forms.MenuItem menuItem5;
		private System.Windows.Forms.MenuItem menuItem6;
		private System.Windows.Forms.MenuItem menuItem7;
		private System.Windows.Forms.MenuItem menuItem8;
		private System.Windows.Forms.MenuItem menuItem9;
		private System.Windows.Forms.MenuItem menuItem10;
		private System.Windows.Forms.MenuItem menuItem11;
		private System.Windows.Forms.MenuItem menuItem12;
		private System.Windows.Forms.ContextMenu contextMenu2;
		private System.Windows.Forms.MenuItem menuItem13;
		private System.Windows.Forms.ContextMenu contextMenu3;
		private System.Windows.Forms.MenuItem menuItem14;
		private System.Windows.Forms.MenuItem menuItem15;
		private System.Windows.Forms.MenuItem menuItem16;
		private System.Windows.Forms.MenuItem menuItem17;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.Splitter splitter1;
		private System.Windows.Forms.MenuItem mnuOtherForm;
		private System.ComponentModel.IContainer components;

		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//

			sideBarButton4.ContextMenu = contextMenu1;
			sideBarButton5.ContextMenu = contextMenu2;
			sideBarButton6.ContextMenu = contextMenu3;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(Form1));
			this.sideBar1 = new SIL.FieldWorks.Common.Controls.SideBar();
			this.sideBarTab1 = new SIL.FieldWorks.Common.Controls.SideBarTab();
			this.sideBarButton1 = new SIL.FieldWorks.Common.Controls.SideBarButton();
			this.imageListLarge = new System.Windows.Forms.ImageList(this.components);
			this.imageListSmall = new System.Windows.Forms.ImageList(this.components);
			this.sideBarButton2 = new SIL.FieldWorks.Common.Controls.SideBarButton();
			this.sideBarButton3 = new SIL.FieldWorks.Common.Controls.SideBarButton();
			this.sideBarTab2 = new SIL.FieldWorks.Common.Controls.SideBarTab();
			this.sideBarTab3 = new SIL.FieldWorks.Common.Controls.SideBarTab();
			this.sideBarButton4 = new SIL.FieldWorks.Common.Controls.SideBarButton();
			this.sideBarButton5 = new SIL.FieldWorks.Common.Controls.SideBarButton();
			this.sideBarButton6 = new SIL.FieldWorks.Common.Controls.SideBarButton();
			this.contextMenu1 = new System.Windows.Forms.ContextMenu();
			this.menuItem4 = new System.Windows.Forms.MenuItem();
			this.menuItem5 = new System.Windows.Forms.MenuItem();
			this.menuItem6 = new System.Windows.Forms.MenuItem();
			this.menuItem7 = new System.Windows.Forms.MenuItem();
			this.menuItem8 = new System.Windows.Forms.MenuItem();
			this.menuItem9 = new System.Windows.Forms.MenuItem();
			this.menuItem10 = new System.Windows.Forms.MenuItem();
			this.menuItem11 = new System.Windows.Forms.MenuItem();
			this.menuItem12 = new System.Windows.Forms.MenuItem();
			this.mainMenu1 = new System.Windows.Forms.MainMenu();
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.menuItem2 = new System.Windows.Forms.MenuItem();
			this.menuItem3 = new System.Windows.Forms.MenuItem();
			this.contextMenu2 = new System.Windows.Forms.ContextMenu();
			this.menuItem13 = new System.Windows.Forms.MenuItem();
			this.contextMenu3 = new System.Windows.Forms.ContextMenu();
			this.menuItem14 = new System.Windows.Forms.MenuItem();
			this.menuItem15 = new System.Windows.Forms.MenuItem();
			this.menuItem17 = new System.Windows.Forms.MenuItem();
			this.menuItem16 = new System.Windows.Forms.MenuItem();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.button3 = new System.Windows.Forms.Button();
			this.splitter1 = new System.Windows.Forms.Splitter();
			this.mnuOtherForm = new System.Windows.Forms.MenuItem();
			this.sideBar1.SuspendLayout();
			this.SuspendLayout();
			//
			// sideBar1
			//
			this.sideBar1.Controls.Add(this.sideBarTab1);
			this.sideBar1.Controls.Add(this.sideBarTab2);
			this.sideBar1.Controls.Add(this.sideBarTab3);
			this.sideBar1.DockPadding.All = 2;
			this.sideBar1.ImageListLarge = this.imageListLarge;
			this.sideBar1.ImageListSmall = this.imageListSmall;
			this.sideBar1.Location = new System.Drawing.Point(0, 0);
			this.sideBar1.Name = "sideBar1";
			this.sideBar1.Persist = false;
			this.sideBar1.Size = new System.Drawing.Size(85, 273);
			this.sideBar1.TabIndex = 0;
			this.sideBar1.Tabs.AddRange(new SIL.FieldWorks.Common.Controls.SideBarTab[] {
																							this.sideBarTab1,
																							this.sideBarTab2,
																							this.sideBarTab3});
			//
			// sideBarTab1
			//
			this.sideBarTab1.Buttons.AddRange(new SIL.FieldWorks.Common.Controls.SideBarButton[] {
																									 this.sideBarButton1,
																									 this.sideBarButton2,
																									 this.sideBarButton3});
			this.sideBarTab1.Enabled = true;
			this.sideBarTab1.Name = "sideBarTab1";
			this.sideBarTab1.Title = "Static Buttons";
			this.sideBarTab1.ButtonClickEvent += new System.EventHandler(this.OnStaticButtonClicked);
			//
			// sideBarButton1
			//
			this.sideBarButton1.ImageIndex = 0;
			this.sideBarButton1.ImageListLarge = this.imageListLarge;
			this.sideBarButton1.ImageListSmall = this.imageListSmall;
			this.sideBarButton1.Name = "sideBarButton1";
			this.sideBarButton1.Text = "sideBarButton1";
			//
			// imageListLarge
			//
			this.imageListLarge.ImageSize = new System.Drawing.Size(32, 32);
			this.imageListLarge.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListLarge.ImageStream")));
			this.imageListLarge.TransparentColor = System.Drawing.Color.Magenta;
			//
			// imageListSmall
			//
			this.imageListSmall.ImageSize = new System.Drawing.Size(16, 16);
			this.imageListSmall.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageListSmall.ImageStream")));
			this.imageListSmall.TransparentColor = System.Drawing.Color.Magenta;
			//
			// sideBarButton2
			//
			this.sideBarButton2.ImageIndex = 1;
			this.sideBarButton2.ImageListLarge = this.imageListLarge;
			this.sideBarButton2.ImageListSmall = this.imageListSmall;
			this.sideBarButton2.Name = "sideBarButton2";
			this.sideBarButton2.Text = "sideBarButton2";
			//
			// sideBarButton3
			//
			this.sideBarButton3.ImageIndex = 2;
			this.sideBarButton3.ImageListLarge = this.imageListLarge;
			this.sideBarButton3.ImageListSmall = this.imageListSmall;
			this.sideBarButton3.Name = "sideBarButton3";
			this.sideBarButton3.Text = "sideBarButton3";
			//
			// sideBarTab2
			//
			this.sideBarTab2.Enabled = true;
			this.sideBarTab2.Name = "sideBarTab2";
			this.sideBarTab2.Title = "Dynamic Buttons";
			//
			// sideBarTab3
			//
			this.sideBarTab3.Buttons.AddRange(new SIL.FieldWorks.Common.Controls.SideBarButton[] {
																									 this.sideBarButton4,
																									 this.sideBarButton5,
																									 this.sideBarButton6});
			this.sideBarTab3.Enabled = true;
			this.sideBarTab3.Name = "sideBarTab3";
			this.sideBarTab3.Title = "Context Menus";
			//
			// sideBarButton4
			//
			this.sideBarButton4.ImageIndex = 3;
			this.sideBarButton4.ImageListLarge = this.imageListLarge;
			this.sideBarButton4.ImageListSmall = this.imageListSmall;
			this.sideBarButton4.Name = "sideBarButton4";
			this.sideBarButton4.Text = "sideBarButton4";
			//
			// sideBarButton5
			//
			this.sideBarButton5.ImageIndex = 4;
			this.sideBarButton5.ImageListLarge = this.imageListLarge;
			this.sideBarButton5.ImageListSmall = this.imageListSmall;
			this.sideBarButton5.Name = "sideBarButton5";
			this.sideBarButton5.Text = "sideBarButton5";
			//
			// sideBarButton6
			//
			this.sideBarButton6.ImageIndex = 0;
			this.sideBarButton6.ImageListLarge = this.imageListLarge;
			this.sideBarButton6.ImageListSmall = this.imageListSmall;
			this.sideBarButton6.Name = "sideBarButton6";
			this.sideBarButton6.Text = "sideBarButton6";
			//
			// contextMenu1
			//
			this.contextMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						 this.menuItem4,
																						 this.menuItem5,
																						 this.menuItem6,
																						 this.menuItem7,
																						 this.menuItem8,
																						 this.menuItem9,
																						 this.menuItem10,
																						 this.menuItem11,
																						 this.menuItem12});
			//
			// menuItem4
			//
			this.menuItem4.Index = 0;
			this.menuItem4.Text = "This";
			//
			// menuItem5
			//
			this.menuItem5.Index = 1;
			this.menuItem5.Text = "is";
			//
			// menuItem6
			//
			this.menuItem6.Index = 2;
			this.menuItem6.Text = "a";
			//
			// menuItem7
			//
			this.menuItem7.Index = 3;
			this.menuItem7.Text = "different";
			//
			// menuItem8
			//
			this.menuItem8.Index = 4;
			this.menuItem8.Text = "context ";
			//
			// menuItem9
			//
			this.menuItem9.Index = 5;
			this.menuItem9.Text = "menu ";
			//
			// menuItem10
			//
			this.menuItem10.Index = 6;
			this.menuItem10.Text = "for";
			//
			// menuItem11
			//
			this.menuItem11.Index = 7;
			this.menuItem11.Text = "this";
			//
			// menuItem12
			//
			this.menuItem12.Index = 8;
			this.menuItem12.Text = "tab";
			//
			// mainMenu1
			//
			this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.menuItem1});
			//
			// menuItem1
			//
			this.menuItem1.Index = 0;
			this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.menuItem2,
																					  this.mnuOtherForm,
																					  this.menuItem3});
			this.menuItem1.Text = "All";
			//
			// menuItem2
			//
			this.menuItem2.Index = 0;
			this.menuItem2.Text = "&Toggle Side Bar";
			this.menuItem2.Click += new System.EventHandler(this.OnToggleSideBar);
			//
			// menuItem3
			//
			this.menuItem3.Index = 2;
			this.menuItem3.Text = "&Exit";
			this.menuItem3.Click += new System.EventHandler(this.OnClose);
			//
			// contextMenu2
			//
			this.contextMenu2.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						 this.menuItem13});
			//
			// menuItem13
			//
			this.menuItem13.Index = 0;
			this.menuItem13.Text = "Click here!";
			this.menuItem13.Click += new System.EventHandler(this.OnClickHere);
			//
			// contextMenu3
			//
			this.contextMenu3.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						 this.menuItem14,
																						 this.menuItem15,
																						 this.menuItem17,
																						 this.menuItem16});
			//
			// menuItem14
			//
			this.menuItem14.Index = 0;
			this.menuItem14.Text = "Large Icons";
			this.menuItem14.Click += new System.EventHandler(this.OnShowLargeIcons);
			//
			// menuItem15
			//
			this.menuItem15.Index = 1;
			this.menuItem15.Text = "Small Icons";
			this.menuItem15.Click += new System.EventHandler(this.OnShowSmallIcons);
			//
			// menuItem17
			//
			this.menuItem17.Index = 2;
			this.menuItem17.Text = "-";
			//
			// menuItem16
			//
			this.menuItem16.Index = 3;
			this.menuItem16.Text = "Hello World";
			this.menuItem16.Click += new System.EventHandler(this.OnHelloWorld);
			//
			// button1
			//
			this.button1.Location = new System.Drawing.Point(168, 16);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(96, 32);
			this.button1.TabIndex = 1;
			this.button1.Text = "Add button";
			this.button1.Click += new System.EventHandler(this.OnAddButton);
			//
			// button2
			//
			this.button2.Location = new System.Drawing.Point(168, 56);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(96, 32);
			this.button2.TabIndex = 2;
			this.button2.Text = "Clear Buttons";
			this.button2.Click += new System.EventHandler(this.OnRemoveButtons);
			//
			// button3
			//
			this.button3.Location = new System.Drawing.Point(168, 104);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(96, 32);
			this.button3.TabIndex = 2;
			this.button3.Text = "Remove 1st Button";
			this.button3.Click += new System.EventHandler(this.OnRemoveFirstButton);
			//
			// splitter1
			//
			this.splitter1.Location = new System.Drawing.Point(85, 0);
			this.splitter1.MinSize = 50;
			this.splitter1.Name = "splitter1";
			this.splitter1.Size = new System.Drawing.Size(3, 273);
			this.splitter1.TabIndex = 3;
			this.splitter1.TabStop = false;
			//
			// mnuOtherForm
			//
			this.mnuOtherForm.Index = 1;
			this.mnuOtherForm.Text = "Other Form...";
			this.mnuOtherForm.Click += new System.EventHandler(this.OnOtherForm);
			//
			// Form1
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(292, 273);
			this.Controls.Add(this.splitter1);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.sideBar1);
			this.Controls.Add(this.button3);
			this.Menu = this.mainMenu1;
			this.Name = "Form1";
			this.Text = "Hello World";
			this.sideBar1.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.Run(new Form1());
		}

		private void OnClickHere(object sender, System.EventArgs e)
		{
			System.Windows.Forms.MessageBox.Show("You clicked 'Click here!'");
		}

		private void OnHelloWorld(object sender, System.EventArgs e)
		{
			System.Windows.Forms.MessageBox.Show("Hello World!");
		}

		private void OnShowLargeIcons(object sender, System.EventArgs e)
		{
			sideBarTab3.LargeIconsShowing = true;
		}

		private void OnShowSmallIcons(object sender, System.EventArgs e)
		{
			sideBarTab3.LargeIconsShowing = false;
		}

		private void OnStaticButtonClicked(object sender, System.EventArgs e)
		{
			System.Diagnostics.Debug.Assert(sender is SideBarButton);
			System.Windows.Forms.MessageBox.Show(string.Format("Clicked button {0}",
				((SideBarButton)sender).Name));
		}

		private void OnClose(object sender, System.EventArgs e)
		{
			Close();
		}

		private void OnMethod1(object sender, System.EventArgs e)
		{
			System.Windows.Forms.MessageBox.Show("Called method 1");
		}

		private void OnMethod2(object sender, System.EventArgs e)
		{
			System.Windows.Forms.MessageBox.Show("Called method 2");
		}

		private void OnMethod3(object sender, System.EventArgs e)
		{
			System.Windows.Forms.MessageBox.Show(string.Format("Called method 3; sender is {0}",
				((SideBarButton)sender).Text));
		}

		private int m_nAddedButtons = 0;
		private void OnAddButton(object sender, System.EventArgs e)
		{
			m_nAddedButtons++;
			SideBarButton btn = new SideBarButton();
			btn.ImageIndex = m_nAddedButtons % 5;
			btn.Text = string.Format("Button {0}", m_nAddedButtons);
			switch (m_nAddedButtons % 5)
			{
				case 0:
					btn.ContextMenu = contextMenu1;
					btn.Click += new System.EventHandler(OnMethod1);
					break;
				case 1:
					btn.ContextMenu = contextMenu2;
					btn.Click += new System.EventHandler(OnMethod2);
					break;
				case 2:
					btn.ContextMenu = contextMenu3;
					btn.Click += new System.EventHandler(OnMethod3);
					break;
				default:
					btn.Click += new System.EventHandler(OnMethod3);
					break;
			}

			sideBarTab2.Buttons.Add(btn);
			sideBar1.Invalidate();
		}

		private void OnRemoveButtons(object sender, System.EventArgs e)
		{
			sideBarTab2.Buttons.Clear();
		}

		private void OnRemoveFirstButton(object sender, System.EventArgs e)
		{
			if (sideBarTab2.Buttons.Count > 0)
				sideBarTab2.Buttons.RemoveAt(0);
		}

		private void OnToggleSideBar(object sender, System.EventArgs e)
		{
			sideBar1.Visible = !sideBar1.Visible;
		}

		private void OnOtherForm(object sender, System.EventArgs e)
		{
			Form2 form = new Form2(imageListLarge, imageListSmall);
			form.ShowDialog();
		}
	}
}
