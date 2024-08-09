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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BasicFindDialog));
			this._searchTextbox = new System.Windows.Forms.TextBox();
			this._notificationLabel = new System.Windows.Forms.Label();
			this._findNext = new System.Windows.Forms.Button();
			this._findPrev = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// _searchTextbox
			// 
			resources.ApplyResources(this._searchTextbox, "_searchTextbox");
			this._searchTextbox.Name = "_searchTextbox";
			this._searchTextbox.TextChanged += new System.EventHandler(this._searchTextbox_TextChanged);
			this._searchTextbox.KeyDown += new System.Windows.Forms.KeyEventHandler(this._searchTextbox_KeyDown);
			// 
			// _notificationLabel
			// 
			resources.ApplyResources(this._notificationLabel, "_notificationLabel");
			this._notificationLabel.Name = "_notificationLabel";
			// 
			// _findNext
			// 
			resources.ApplyResources(this._findNext, "_findNext");
			this._findNext.Name = "_findNext";
			this._findNext.UseVisualStyleBackColor = true;
			this._findNext.Click += new System.EventHandler(this._findNext_Click);
			// 
			// _findPrev
			// 
			resources.ApplyResources(this._findPrev, "_findPrev");
			this._findPrev.Name = "_findPrev";
			this._findPrev.UseVisualStyleBackColor = true;
			this._findPrev.Click += new System.EventHandler(this._findPrev_Click);
			// 
			// BasicFindDialog
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._findPrev);
			this.Controls.Add(this._findNext);
			this.Controls.Add(this._notificationLabel);
			this.Controls.Add(this._searchTextbox);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "BasicFindDialog";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private TextBox _searchTextbox;
		private System.Windows.Forms.Label _notificationLabel;
		private System.Windows.Forms.Button _findNext;
	  private Button _findPrev;
   }
}