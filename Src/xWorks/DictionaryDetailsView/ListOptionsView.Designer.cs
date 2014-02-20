// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	partial class ListOptionsView
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.listView = new System.Windows.Forms.ListView();
			this.buttonUp = new System.Windows.Forms.Button();
			this.buttonDown = new System.Windows.Forms.Button();
			this.checkBoxDisplayOption = new System.Windows.Forms.CheckBox();
			this.labelListView = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// listView
			// 
			this.listView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listView.Location = new System.Drawing.Point(3, 20);
			this.listView.Name = "listView";
			this.listView.Size = new System.Drawing.Size(253, 95);
			this.listView.TabIndex = 0;
			this.listView.UseCompatibleStateImageBehavior = false;
			this.listView.View = System.Windows.Forms.View.Details;
			// 
			// buttonUp
			// 
			this.buttonUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonUp.Location = new System.Drawing.Point(262, 20);
			this.buttonUp.Name = "buttonUp";
			this.buttonUp.Size = new System.Drawing.Size(26, 23);
			this.buttonUp.TabIndex = 1;
			this.buttonUp.Text = "▲";
			this.buttonUp.UseVisualStyleBackColor = true;
			// 
			// buttonDown
			// 
			this.buttonDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonDown.Location = new System.Drawing.Point(262, 49);
			this.buttonDown.Name = "buttonDown";
			this.buttonDown.Size = new System.Drawing.Size(26, 23);
			this.buttonDown.TabIndex = 1;
			this.buttonDown.Text = "▼";
			this.buttonDown.UseVisualStyleBackColor = true;
			// 
			// checkBoxDisplayOption
			// 
			this.checkBoxDisplayOption.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxDisplayOption.AutoSize = true;
			this.checkBoxDisplayOption.Location = new System.Drawing.Point(4, 121);
			this.checkBoxDisplayOption.Name = "checkBoxDisplayOption";
			this.checkBoxDisplayOption.Size = new System.Drawing.Size(200, 17);
			this.checkBoxDisplayOption.TabIndex = 2;
			this.checkBoxDisplayOption.Text = "Display Writing System Abbreviations";
			this.checkBoxDisplayOption.UseVisualStyleBackColor = true;
			// 
			// labelListView
			// 
			this.labelListView.AutoSize = true;
			this.labelListView.Location = new System.Drawing.Point(4, 4);
			this.labelListView.Name = "labelListView";
			this.labelListView.Size = new System.Drawing.Size(85, 13);
			this.labelListView.TabIndex = 3;
			this.labelListView.Text = "Writing Systems:";
			// 
			// ListOptionsView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.labelListView);
			this.Controls.Add(this.checkBoxDisplayOption);
			this.Controls.Add(this.buttonDown);
			this.Controls.Add(this.buttonUp);
			this.Controls.Add(this.listView);
			this.MinimumSize = new System.Drawing.Size(300, 100);
			this.Name = "ListOptionsView";
			this.Size = new System.Drawing.Size(300, 140);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListView listView;
		private System.Windows.Forms.Button buttonUp;
		private System.Windows.Forms.Button buttonDown;
		private System.Windows.Forms.CheckBox checkBoxDisplayOption;
		private System.Windows.Forms.Label labelListView;

	}
}
