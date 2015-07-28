// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// WSChooser is only a mockup at this point!!!!!!!!!!!
	/// </summary>
	public class WSChooser : UserControl, IFWDisposable
	{
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.ContextMenu WritingSystems;
		private System.Windows.Forms.MenuItem menuItem1;
		private System.Windows.Forms.MenuItem menuItem2;
		private System.Windows.Forms.MenuItem menuItem3;
		private System.Windows.Forms.MenuItem menuItem4;
		private System.Windows.Forms.MenuItem menuItem5;
		private System.Windows.Forms.MenuItem menuItem6;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>
		///
		/// </summary>
		public WSChooser()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WSChooser));
			this.button1 = new System.Windows.Forms.Button();
			this.WritingSystems = new System.Windows.Forms.ContextMenu();
			this.menuItem4 = new System.Windows.Forms.MenuItem();
			this.menuItem5 = new System.Windows.Forms.MenuItem();
			this.menuItem6 = new System.Windows.Forms.MenuItem();
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.menuItem2 = new System.Windows.Forms.MenuItem();
			this.menuItem3 = new System.Windows.Forms.MenuItem();
			this.SuspendLayout();
			//
			// button1
			//
			this.button1.ContextMenu = this.WritingSystems;
			resources.ApplyResources(this.button1, "button1");
			this.button1.Name = "button1";
			this.button1.Click += new System.EventHandler(this.button1_Click_1);
			this.button1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.button1_MouseDown);
			this.button1.Paint += new System.Windows.Forms.PaintEventHandler(this.button1_Paint);
			//
			// WritingSystems
			//
			this.WritingSystems.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
			this.menuItem4,
			this.menuItem5,
			this.menuItem6});
			//
			// menuItem4
			//
			this.menuItem4.Index = 0;
			resources.ApplyResources(this.menuItem4, "menuItem4");
			//
			// menuItem5
			//
			this.menuItem5.Index = 1;
			resources.ApplyResources(this.menuItem5, "menuItem5");
			//
			// menuItem6
			//
			this.menuItem6.Index = 2;
			resources.ApplyResources(this.menuItem6, "menuItem6");
			//
			// menuItem1
			//
			this.menuItem1.Index = -1;
			resources.ApplyResources(this.menuItem1, "menuItem1");
			//
			// menuItem2
			//
			this.menuItem2.Index = -1;
			resources.ApplyResources(this.menuItem2, "menuItem2");
			//
			// menuItem3
			//
			this.menuItem3.Index = -1;
			resources.ApplyResources(this.menuItem3, "menuItem3");
			//
			// WSChooser
			//
			this.Controls.Add(this.button1);
			this.Name = "WSChooser";
			resources.ApplyResources(this, "$this");
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.WSChooser_Paint);
			this.ResumeLayout(false);

		}
		#endregion

		private void button1_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			//"erase" the border of the button
			using (Pen p = new Pen(this.BackColor, 2))
			{
				e.Graphics.DrawRectangle(p, this.button1.Left, this.button1.Top, this.button1.Width, this.button1.Height);
			}
		}

		private void WSChooser_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{

		}

		private void button1_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			button1.ContextMenu.Show(button1, new Point(button1.Left,button1.Bottom));
		}

		private void button1_Click_1(object sender, System.EventArgs e)
		{

		}
	}
}
