// SilSidePane, Copyright 2009 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

namespace SIL.SilSidePane
{
	partial class NavPaneOptionsDlg
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.Label1 = new System.Windows.Forms.Label();
			this.tabListBox = new System.Windows.Forms.CheckedListBox();
			this.btn_Up = new System.Windows.Forms.Button();
			this.btn_Down = new System.Windows.Forms.Button();
			this.btn_Reset = new System.Windows.Forms.Button();
			this.btn_OK = new System.Windows.Forms.Button();
			this.btn_Cancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// Label1
			//
			this.Label1.Location = new System.Drawing.Point(12, 9);
			this.Label1.Name = "Label1";
			this.Label1.Size = new System.Drawing.Size(144, 23);
			this.Label1.TabIndex = 6;
			this.Label1.Text = SilSidePane.DisplayButtonsInThisOrder;
			//
			// tabListBox
			//
			this.tabListBox.FormattingEnabled = true;
			this.tabListBox.Location = new System.Drawing.Point(12, 35);
			this.tabListBox.Name = "tabListBox";
			this.tabListBox.Size = new System.Drawing.Size(197, 109);
			this.tabListBox.TabIndex = 5;
			//
			// btn_Up
			//
			this.btn_Up.Location = new System.Drawing.Point(215, 35);
			this.btn_Up.Name = "btn_Up";
			this.btn_Up.Size = new System.Drawing.Size(75, 23);
			this.btn_Up.TabIndex = 4;
			this.btn_Up.Text = SilSidePane.MoveUp;
			this.btn_Up.UseVisualStyleBackColor = true;
			this.btn_Up.Click += new System.EventHandler(this.btn_Up_Click);
			//
			// btn_Down
			//
			this.btn_Down.Location = new System.Drawing.Point(215, 64);
			this.btn_Down.Name = "btn_Down";
			this.btn_Down.Size = new System.Drawing.Size(75, 23);
			this.btn_Down.TabIndex = 3;
			this.btn_Down.Text = SilSidePane.MoveDown;
			this.btn_Down.UseVisualStyleBackColor = true;
			this.btn_Down.Click += new System.EventHandler(this.btn_Down_Click);
			//
			// btn_Reset
			//
			this.btn_Reset.Location = new System.Drawing.Point(215, 93);
			this.btn_Reset.Name = "btn_Reset";
			this.btn_Reset.Size = new System.Drawing.Size(75, 23);
			this.btn_Reset.TabIndex = 2;
			this.btn_Reset.Text = SilSidePane.Reset;
			this.btn_Reset.UseVisualStyleBackColor = true;
			this.btn_Reset.Click += new System.EventHandler(this.btn_Reset_Click);
			//
			// btn_OK
			//
			this.btn_OK.Location = new System.Drawing.Point(134, 158);
			this.btn_OK.Name = "btn_OK";
			this.btn_OK.Size = new System.Drawing.Size(75, 23);
			this.btn_OK.TabIndex = 1;
			this.btn_OK.Text = SilSidePane.OK;
			this.btn_OK.UseVisualStyleBackColor = true;
			this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
			//
			// btn_Cancel
			//
			this.btn_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btn_Cancel.Location = new System.Drawing.Point(215, 158);
			this.btn_Cancel.Name = "btn_Cancel";
			this.btn_Cancel.Size = new System.Drawing.Size(75, 23);
			this.btn_Cancel.TabIndex = 0;
			this.btn_Cancel.Text = SilSidePane.Cancel;
			this.btn_Cancel.UseVisualStyleBackColor = true;
			this.btn_Cancel.Click += new System.EventHandler(this.btn_Cancel_Click);
			//
			// NavPaneOptionsDlg
			//
			this.AcceptButton = this.btn_OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btn_Cancel;
			this.ClientSize = new System.Drawing.Size(302, 193);
			this.Controls.Add(this.btn_Cancel);
			this.Controls.Add(this.btn_OK);
			this.Controls.Add(this.btn_Reset);
			this.Controls.Add(this.btn_Down);
			this.Controls.Add(this.btn_Up);
			this.Controls.Add(this.tabListBox);
			this.Controls.Add(this.Label1);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "NavPaneOptionsDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = SilSidePane.NavigationPaneOptions;
			this.ResumeLayout(false);

		}

		internal System.Windows.Forms.Label Label1;
		internal System.Windows.Forms.CheckedListBox tabListBox;
		internal System.Windows.Forms.Button btn_Up;
		internal System.Windows.Forms.Button btn_Down;
		internal System.Windows.Forms.Button btn_Reset;
		internal System.Windows.Forms.Button btn_OK;
		internal System.Windows.Forms.Button btn_Cancel;

		#endregion

	}
}
