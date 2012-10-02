using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace dbmt
{
	/// <summary>
	/// Summary description for frmAbout.
	/// </summary>
	public class frmAbout : System.Windows.Forms.Form
	{
		private System.Windows.Forms.PictureBox picIcon;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label lblVersion;
		private System.Windows.Forms.Button cmdOK;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public frmAbout()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.picIcon = new System.Windows.Forms.PictureBox();
			this.cmdOK = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.lblVersion = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// picIcon
			//
			this.picIcon.Location = new System.Drawing.Point(8, 8);
			this.picIcon.Name = "picIcon";
			this.picIcon.Size = new System.Drawing.Size(32, 32);
			this.picIcon.TabIndex = 0;
			this.picIcon.TabStop = false;
			//
			// cmdOK
			//
			this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cmdOK.Location = new System.Drawing.Point(192, 80);
			this.cmdOK.Name = "cmdOK";
			this.cmdOK.Size = new System.Drawing.Size(64, 24);
			this.cmdOK.TabIndex = 3;
			this.cmdOK.Text = "&OK";
			//
			// label1
			//
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.label1.Location = new System.Drawing.Point(48, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(216, 24);
			this.label1.TabIndex = 0;
			this.label1.Text = "Database Maintenance Tool";
			//
			// label2
			//
			this.label2.Location = new System.Drawing.Point(48, 56);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(192, 16);
			this.label2.TabIndex = 2;
			this.label2.Text = "Copyright © 2005 by Darrell Zook";
			//
			// lblVersion
			//
			this.lblVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.lblVersion.Location = new System.Drawing.Point(48, 32);
			this.lblVersion.Name = "lblVersion";
			this.lblVersion.Size = new System.Drawing.Size(208, 16);
			this.lblVersion.TabIndex = 1;
			this.lblVersion.Text = "Version: 1.0.0";
			//
			// frmAbout
			//
			this.AcceptButton = this.cmdOK;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.cmdOK;
			this.ClientSize = new System.Drawing.Size(266, 111);
			this.Controls.Add(this.lblVersion);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.cmdOK);
			this.Controls.Add(this.picIcon);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "frmAbout";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "About Database Maintenance Tool";
			this.Load += new System.EventHandler(this.frmAbout_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void frmAbout_Load(object sender, System.EventArgs e)
		{
			this.Icon = Globals.MainForm.Icon;
			picIcon.Image = Globals.MainForm.Icon.ToBitmap();

			lblVersion.Text = "Version: " +
				System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
		}
	}
}
