// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class BasicFindDialog
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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
			this._searchTextbox = new TextBox();
			this._notificationLabel = new System.Windows.Forms.Label();
			this._findNext = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// _searchTextbox
			// 
			this._searchTextbox.Location = new System.Drawing.Point(13, 13);
			this._searchTextbox.Name = "_searchTextbox";
			this._searchTextbox.Size = new System.Drawing.Size(303, 20);
			this._searchTextbox.TabIndex = 0;
			this._searchTextbox.TextChanged += new System.EventHandler(this._searchTextbox_TextChanged);
			this._searchTextbox.KeyDown += new System.Windows.Forms.KeyEventHandler(this._searchTextbox_KeyDown);
			// 
			// _notificationLabel
			// 
			this._notificationLabel.AutoSize = true;
			this._notificationLabel.Location = new System.Drawing.Point(13, 51);
			this._notificationLabel.Name = "_notificationLabel";
			this._notificationLabel.Size = new System.Drawing.Size(0, 13);
			this._notificationLabel.TabIndex = 1;
			// 
			// _findNext
			// 
			this._findNext.Enabled = false;
			this._findNext.Location = new System.Drawing.Point(240, 51);
			this._findNext.Name = "_findNext";
			this._findNext.Size = new System.Drawing.Size(75, 23);
			this._findNext.TabIndex = 2;
			this._findNext.Text = "Find Next";
			this._findNext.UseVisualStyleBackColor = true;
			this._findNext.Click += new System.EventHandler(this._findNext_Click);
			// 
			// BasicFindDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(326, 86);
			this.Controls.Add(this._findNext);
			this.Controls.Add(this._notificationLabel);
			this.Controls.Add(this._searchTextbox);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(360, 150);
			this.MinimizeBox = false;
			this.Name = "BasicFindDialog";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.Text = "Find";
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private TextBox _searchTextbox;
		private System.Windows.Forms.Label _notificationLabel;
		private System.Windows.Forms.Button _findNext;
	}
}