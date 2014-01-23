// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ExportTranslatedListsDlg.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.XWorks
{
	partial class ExportTranslatedListsDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExportTranslatedListsDlg));
			this.m_tbFilepath = new System.Windows.Forms.TextBox();
			this.m_btnBrowse = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.m_lvLists = new System.Windows.Forms.ListView();
			this.label3 = new System.Windows.Forms.Label();
			this.m_lvWritingSystems = new System.Windows.Forms.ListView();
			this.m_btnExport = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_columnLists = new System.Windows.Forms.ColumnHeader();
			this.m_btnSelectAll = new System.Windows.Forms.Button();
			this.m_btnClearAll = new System.Windows.Forms.Button();
			this.m_columnWs = new System.Windows.Forms.ColumnHeader();
			this.SuspendLayout();
			//
			// m_tbFilepath
			//
			resources.ApplyResources(this.m_tbFilepath, "m_tbFilepath");
			this.m_tbFilepath.Name = "m_tbFilepath";
			this.m_tbFilepath.TextChanged += new System.EventHandler(this.m_tbFilepath_TextChanged);
			//
			// m_btnBrowse
			//
			resources.ApplyResources(this.m_btnBrowse, "m_btnBrowse");
			this.m_btnBrowse.Name = "m_btnBrowse";
			this.m_btnBrowse.UseVisualStyleBackColor = true;
			this.m_btnBrowse.Click += new System.EventHandler(this.m_btnBrowse_Click);
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// m_lvLists
			//
			resources.ApplyResources(this.m_lvLists, "m_lvLists");
			this.m_lvLists.CheckBoxes = true;
			this.m_lvLists.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.m_columnLists});
			this.m_lvLists.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.m_lvLists.Name = "m_lvLists";
			this.m_lvLists.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.m_lvLists.UseCompatibleStateImageBehavior = false;
			this.m_lvLists.View = System.Windows.Forms.View.Details;
			this.m_lvLists.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.m_lvLists_ItemChecked);
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// m_lvWritingSystems
			//
			resources.ApplyResources(this.m_lvWritingSystems, "m_lvWritingSystems");
			this.m_lvWritingSystems.CheckBoxes = true;
			this.m_lvWritingSystems.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.m_columnWs});
			this.m_lvWritingSystems.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.m_lvWritingSystems.Name = "m_lvWritingSystems";
			this.m_lvWritingSystems.UseCompatibleStateImageBehavior = false;
			this.m_lvWritingSystems.View = System.Windows.Forms.View.Details;
			this.m_lvWritingSystems.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.m_lvWritingSystems_ItemChecked);
			//
			// m_btnExport
			//
			resources.ApplyResources(this.m_btnExport, "m_btnExport");
			this.m_btnExport.Name = "m_btnExport";
			this.m_btnExport.UseVisualStyleBackColor = true;
			this.m_btnExport.Click += new System.EventHandler(this.m_btnExport_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_columnLists
			//
			resources.ApplyResources(this.m_columnLists, "m_columnLists");
			//
			// m_btnSelectAll
			//
			resources.ApplyResources(this.m_btnSelectAll, "m_btnSelectAll");
			this.m_btnSelectAll.Name = "m_btnSelectAll";
			this.m_btnSelectAll.UseVisualStyleBackColor = true;
			this.m_btnSelectAll.Click += new System.EventHandler(this.m_btnSelectAll_Click);
			//
			// m_btnClearAll
			//
			resources.ApplyResources(this.m_btnClearAll, "m_btnClearAll");
			this.m_btnClearAll.Name = "m_btnClearAll";
			this.m_btnClearAll.UseVisualStyleBackColor = true;
			this.m_btnClearAll.Click += new System.EventHandler(this.m_btnClearAll_Click);
			//
			// m_columnWs
			//
			resources.ApplyResources(this.m_columnWs, "m_columnWs");
			//
			// ExportTranslatedListsDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_btnClearAll);
			this.Controls.Add(this.m_btnSelectAll);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnExport);
			this.Controls.Add(this.m_lvWritingSystems);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.m_lvLists);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_btnBrowse);
			this.Controls.Add(this.m_tbFilepath);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ExportTranslatedListsDlg";
			this.ShowIcon = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_tbFilepath;
		private System.Windows.Forms.Button m_btnBrowse;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ListView m_lvLists;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ListView m_lvWritingSystems;
		private System.Windows.Forms.Button m_btnExport;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.ColumnHeader m_columnLists;
		private System.Windows.Forms.Button m_btnSelectAll;
		private System.Windows.Forms.Button m_btnClearAll;
		private System.Windows.Forms.ColumnHeader m_columnWs;
	}
}