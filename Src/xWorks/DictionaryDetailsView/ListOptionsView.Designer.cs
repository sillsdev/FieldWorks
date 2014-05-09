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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ListOptionsView));
			this.listView = new System.Windows.Forms.ListView();
			this.invisibleHeaderToSetColWidth = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.buttonUp = new System.Windows.Forms.Button();
			this.buttonDown = new System.Windows.Forms.Button();
			this.checkBoxDisplayOption = new System.Windows.Forms.CheckBox();
			this.labelListView = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// listView
			// 
			resources.ApplyResources(this.listView, "listView");
			this.listView.CheckBoxes = true;
			this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.invisibleHeaderToSetColWidth});
			this.listView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.listView.HideSelection = false;
			this.listView.MultiSelect = false;
			this.listView.Name = "listView";
			this.listView.UseCompatibleStateImageBehavior = false;
			this.listView.View = System.Windows.Forms.View.Details;
			// 
			// invisibleHeaderToSetColWidth
			// 
			resources.ApplyResources(this.invisibleHeaderToSetColWidth, "invisibleHeaderToSetColWidth");
			// 
			// buttonUp
			// 
			resources.ApplyResources(this.buttonUp, "buttonUp");
			this.buttonUp.Name = "buttonUp";
			this.buttonUp.UseVisualStyleBackColor = true;
			// 
			// buttonDown
			// 
			resources.ApplyResources(this.buttonDown, "buttonDown");
			this.buttonDown.Name = "buttonDown";
			this.buttonDown.UseVisualStyleBackColor = true;
			// 
			// checkBoxDisplayOption
			// 
			resources.ApplyResources(this.checkBoxDisplayOption, "checkBoxDisplayOption");
			this.checkBoxDisplayOption.Name = "checkBoxDisplayOption";
			this.checkBoxDisplayOption.UseVisualStyleBackColor = true;
			// 
			// labelListView
			// 
			resources.ApplyResources(this.labelListView, "labelListView");
			this.labelListView.Name = "labelListView";
			// 
			// ListOptionsView
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.checkBoxDisplayOption);
			this.Controls.Add(this.buttonDown);
			this.Controls.Add(this.buttonUp);
			this.Controls.Add(this.listView);
			this.Controls.Add(this.labelListView);
			this.MinimumSize = new System.Drawing.Size(300, 100);
			this.Name = "ListOptionsView";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListView listView;
		private System.Windows.Forms.Button buttonUp;
		private System.Windows.Forms.Button buttonDown;
		private System.Windows.Forms.CheckBox checkBoxDisplayOption;
		private System.Windows.Forms.Label labelListView;
		private System.Windows.Forms.ColumnHeader invisibleHeaderToSetColWidth;

	}
}
