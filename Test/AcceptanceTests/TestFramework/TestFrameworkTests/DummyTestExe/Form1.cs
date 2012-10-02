// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Form1.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using SIL.FieldWorks.Common.Controls;

namespace DummyTestExe
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.RichTextBox richTextBox1;
		private MainMenu mainMenu1;
		private MenuItem menuItem1;
		private MenuItem menuItem2;
		private MenuItem menuItem3;
		private MenuItem menuItem4;
		private MenuItem menuItem6;
		private MenuItem menuItem5;
		private MenuItem menuItem7;
		private MenuItem menuItem8;
		private MenuItem menuItem9;
		private MenuItem menuItem10;
		private MenuItem menuWhatsThisHelp;
		private MenuItem menuItem11;
		private MenuItem menuItem12;
		private MenuItem menuItem13;
		private MenuItem menuItem14;
		private MenuItem menuItem15;
		private MenuItem menuItem16;
		private MenuItem menuItem17;
		private MenuItem menuItem18;
		private MenuItem menuItem19;
		private MenuItem menuItem20;
		private StatusBar statusBar1;
		private StatusBarPanel statusBarPanel1;
		private MenuExtender menuExtender1;
		private MenuItem menuItem21;
		private System.ComponentModel.IContainer components;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Form1"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
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
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.label1 = new System.Windows.Forms.Label();
			this.richTextBox1 = new System.Windows.Forms.RichTextBox();
			this.mainMenu1 = new System.Windows.Forms.MainMenu();
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.menuItem2 = new System.Windows.Forms.MenuItem();
			this.menuItem3 = new System.Windows.Forms.MenuItem();
			this.menuItem4 = new System.Windows.Forms.MenuItem();
			this.menuItem11 = new System.Windows.Forms.MenuItem();
			this.menuItem12 = new System.Windows.Forms.MenuItem();
			this.menuItem13 = new System.Windows.Forms.MenuItem();
			this.menuItem14 = new System.Windows.Forms.MenuItem();
			this.menuItem6 = new System.Windows.Forms.MenuItem();
			this.menuItem5 = new System.Windows.Forms.MenuItem();
			this.menuItem7 = new System.Windows.Forms.MenuItem();
			this.menuWhatsThisHelp = new System.Windows.Forms.MenuItem();
			this.menuItem8 = new System.Windows.Forms.MenuItem();
			this.menuItem15 = new System.Windows.Forms.MenuItem();
			this.menuItem17 = new System.Windows.Forms.MenuItem();
			this.menuItem18 = new System.Windows.Forms.MenuItem();
			this.menuItem21 = new System.Windows.Forms.MenuItem();
			this.menuItem16 = new System.Windows.Forms.MenuItem();
			this.menuItem9 = new System.Windows.Forms.MenuItem();
			this.menuItem10 = new System.Windows.Forms.MenuItem();
			this.menuItem19 = new System.Windows.Forms.MenuItem();
			this.menuItem20 = new System.Windows.Forms.MenuItem();
			this.statusBar1 = new System.Windows.Forms.StatusBar();
			this.statusBarPanel1 = new System.Windows.Forms.StatusBarPanel();
			this.menuExtender1 = new SIL.FieldWorks.Common.Controls.MenuExtender(this.components);
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanel1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.menuExtender1)).BeginInit();
			this.SuspendLayout();
			//
			// label1
			//
			this.label1.AccessibleName = "informationBar";
			this.label1.BackColor = System.Drawing.Color.IndianRed;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label1.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(280, 24);
			this.label1.TabIndex = 0;
			this.label1.Text = "Information Bar Text";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// richTextBox1
			//
			this.richTextBox1.AccessibleName = "draftView";
			this.richTextBox1.AccessibleRole = System.Windows.Forms.AccessibleRole.Client;
			this.richTextBox1.Location = new System.Drawing.Point(8, 40);
			this.richTextBox1.Name = "richTextBox1";
			this.richTextBox1.Size = new System.Drawing.Size(280, 136);
			this.richTextBox1.TabIndex = 1;
			this.richTextBox1.Text = "";
			//
			// mainMenu1
			//
			this.mainMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.menuItem1,
																					  this.menuItem7,
																					  this.menuItem15,
																					  this.menuItem17,
																					  this.menuItem16,
																					  this.menuItem9,
																					  this.menuItem19,
																					  this.menuItem20});
			//
			// menuItem1
			//
			this.menuItem1.Index = 0;
			this.menuItem1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.menuItem2,
																					  this.menuItem3,
																					  this.menuItem4,
																					  this.menuItem6,
																					  this.menuItem5});
			this.menuItem1.Text = "File";
			//
			// menuItem2
			//
			this.menuExtender1.SetImageIndex(this.menuItem2, -1);
			this.menuItem2.Index = 0;
			this.menuItem2.Shortcut = System.Windows.Forms.Shortcut.CtrlN;
			this.menuExtender1.SetStatusMessage(this.menuItem2, "New project");
			this.menuItem2.Text = "New";
			this.menuItem2.Click += new System.EventHandler(this.menuItem2_Click);
			//
			// menuItem3
			//
			this.menuExtender1.SetImageIndex(this.menuItem3, -1);
			this.menuItem3.Index = 1;
			this.menuItem3.Shortcut = System.Windows.Forms.Shortcut.CtrlO;
			this.menuExtender1.SetStatusMessage(this.menuItem3, "Open project");
			this.menuItem3.Text = "open";
			//
			// menuItem4
			//
			this.menuExtender1.SetImageIndex(this.menuItem4, -1);
			this.menuItem4.Index = 2;
			this.menuItem4.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.menuItem11,
																					  this.menuItem12,
																					  this.menuItem13,
																					  this.menuItem14});
			this.menuItem4.Text = "Print preview...";
			//
			// menuItem11
			//
			this.menuExtender1.SetImageIndex(this.menuItem11, -1);
			this.menuItem11.Index = 0;
			this.menuItem11.Text = "&Fun";
			//
			// menuItem12
			//
			this.menuExtender1.SetImageIndex(this.menuItem12, -1);
			this.menuItem12.Index = 1;
			this.menuItem12.Text = "&Not Fun";
			//
			// menuItem13
			//
			this.menuExtender1.SetImageIndex(this.menuItem13, -1);
			this.menuItem13.Index = 2;
			this.menuItem13.Text = "-";
			//
			// menuItem14
			//
			this.menuExtender1.SetImageIndex(this.menuItem14, -1);
			this.menuItem14.Index = 3;
			this.menuItem14.Text = "&More";
			//
			// menuItem6
			//
			this.menuExtender1.SetImageIndex(this.menuItem6, -1);
			this.menuItem6.Index = 3;
			this.menuItem6.Text = "-";
			//
			// menuItem5
			//
			this.menuExtender1.SetImageIndex(this.menuItem5, -1);
			this.menuItem5.Index = 4;
			this.menuItem5.Text = "Exit";
			//
			// menuItem7
			//
			this.menuItem7.Index = 1;
			this.menuItem7.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.menuWhatsThisHelp,
																					  this.menuItem8});
			this.menuItem7.Text = "H&elp";
			//
			// menuWhatsThisHelp
			//
			this.menuExtender1.SetImageIndex(this.menuWhatsThisHelp, -1);
			this.menuWhatsThisHelp.Index = 0;
			this.menuWhatsThisHelp.Shortcut = System.Windows.Forms.Shortcut.ShiftF1;
			this.menuWhatsThisHelp.Text = "Wh&at\'s This?";
			//
			// menuItem8
			//
			this.menuExtender1.SetImageIndex(this.menuItem8, -1);
			this.menuItem8.Index = 1;
			this.menuItem8.Text = "&About";
			//
			// menuItem15
			//
			this.menuItem15.Index = 2;
			this.menuItem15.Text = "&Empty";
			//
			// menuItem17
			//
			this.menuItem17.Index = 3;
			this.menuItem17.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					   this.menuItem18,
																					   this.menuItem21});
			this.menuItem17.Text = "B&ongo";
			//
			// menuItem18
			//
			this.menuExtender1.SetImageIndex(this.menuItem18, -1);
			this.menuItem18.Index = 0;
			this.menuItem18.Text = "&Styles";
			//
			// menuItem21
			//
			this.menuExtender1.SetImageIndex(this.menuItem21, -1);
			this.menuItem21.Index = 1;
			this.menuItem21.Shortcut = System.Windows.Forms.Shortcut.F1;
			this.menuItem21.Text = "&Help...";
			//
			// menuItem16
			//
			this.menuItem16.Index = 4;
			this.menuItem16.Text = "&Im Empty";
			//
			// menuItem9
			//
			this.menuItem9.Index = 5;
			this.menuItem9.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																					  this.menuItem10});
			this.menuItem9.Text = "Last Menu Item";
			//
			// menuItem10
			//
			this.menuExtender1.SetImageIndex(this.menuItem10, -1);
			this.menuItem10.Index = 0;
			this.menuItem10.Shortcut = System.Windows.Forms.Shortcut.CtrlN;
			this.menuItem10.Text = "Nothing";
			//
			// menuItem19
			//
			this.menuItem19.Index = 6;
			this.menuItem19.Text = "O&blong";
			//
			// menuItem20
			//
			this.menuItem20.Index = 7;
			this.menuItem20.Text = "For&mat";
			//
			// statusBar1
			//
			this.statusBar1.Location = new System.Drawing.Point(0, 163);
			this.statusBar1.Name = "statusBar1";
			this.statusBar1.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
																						  this.statusBarPanel1});
			this.statusBar1.ShowPanels = true;
			this.statusBar1.Size = new System.Drawing.Size(392, 22);
			this.statusBar1.TabIndex = 2;
			this.statusBar1.Text = "statusBar1";
			//
			// statusBarPanel1
			//
			this.statusBarPanel1.Text = "statusBarPanel1";
			//
			// menuExtender1
			//
			this.menuExtender1.DefaultHelpText = null;
			this.menuExtender1.Parent = null;
			this.menuExtender1.StatusBar = this.statusBarPanel1;
			//
			// Form1
			//
			this.AccessibleName = "dummyTestForm";
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(392, 185);
			this.Controls.Add(this.statusBar1);
			this.Controls.Add(this.richTextBox1);
			this.Controls.Add(this.label1);
			this.Menu = this.mainMenu1;
			this.Name = "Form1";
			this.Text = "Dummy Test Form";
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanel1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.menuExtender1)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[STAThread]
		static void Main()
		{
			Application.Run(new Form1());
		}

		private void menuItem2_Click(object sender, System.EventArgs e)
		{
			MessageBox.Show("MenuItem NEW");
		}
	}
}
