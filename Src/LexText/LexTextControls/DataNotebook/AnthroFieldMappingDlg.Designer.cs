// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AnthroFieldMappingDlg.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	partial class AnthroFieldMappingDlg
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AnthroFieldMappingDlg));
			this.m_groupContents = new System.Windows.Forms.GroupBox();
			this.m_lblContents = new System.Windows.Forms.Label();
			this.m_lvContents = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.m_lblDestination = new System.Windows.Forms.Label();
			this.m_cbDestination = new System.Windows.Forms.ComboBox();
			this.m_groupOptions = new System.Windows.Forms.GroupBox();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.m_btnAddCustom = new System.Windows.Forms.Button();
			this.m_groupContents.SuspendLayout();
			this.SuspendLayout();
			//
			// m_groupContents
			//
			resources.ApplyResources(this.m_groupContents, "m_groupContents");
			this.m_groupContents.Controls.Add(this.m_lblContents);
			this.m_groupContents.Controls.Add(this.m_lvContents);
			this.m_groupContents.Name = "m_groupContents";
			this.m_groupContents.TabStop = false;
			//
			// m_lblContents
			//
			resources.ApplyResources(this.m_lblContents, "m_lblContents");
			this.m_lblContents.Name = "m_lblContents";
			//
			// m_lvContents
			//
			resources.ApplyResources(this.m_lvContents, "m_lvContents");
			this.m_lvContents.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader1});
			this.m_lvContents.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.m_lvContents.MultiSelect = false;
			this.m_lvContents.Name = "m_lvContents";
			this.m_toolTip.SetToolTip(this.m_lvContents, resources.GetString("m_lvContents.ToolTip"));
			this.m_lvContents.UseCompatibleStateImageBehavior = false;
			this.m_lvContents.View = System.Windows.Forms.View.Details;
			this.m_lvContents.SizeChanged += new System.EventHandler(this.m_lvContents_SizeChanged);
			//
			// columnHeader1
			//
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			//
			// m_lblDestination
			//
			resources.ApplyResources(this.m_lblDestination, "m_lblDestination");
			this.m_lblDestination.Name = "m_lblDestination";
			//
			// m_cbDestination
			//
			resources.ApplyResources(this.m_cbDestination, "m_cbDestination");
			this.m_cbDestination.FormattingEnabled = true;
			this.m_cbDestination.Name = "m_cbDestination";
			this.m_toolTip.SetToolTip(this.m_cbDestination, resources.GetString("m_cbDestination.ToolTip"));
			this.m_cbDestination.SelectedIndexChanged += new System.EventHandler(this.m_cbDestination_SelectedIndexChanged);
			//
			// m_groupOptions
			//
			resources.ApplyResources(this.m_groupOptions, "m_groupOptions");
			this.m_groupOptions.Name = "m_groupOptions";
			this.m_groupOptions.TabStop = false;
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
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
			// m_btnAddCustom
			//
			resources.ApplyResources(this.m_btnAddCustom, "m_btnAddCustom");
			this.m_btnAddCustom.Name = "m_btnAddCustom";
			this.m_toolTip.SetToolTip(this.m_btnAddCustom, resources.GetString("m_btnAddCustom.ToolTip"));
			this.m_btnAddCustom.UseVisualStyleBackColor = true;
			this.m_btnAddCustom.Click += new System.EventHandler(this.m_btnAddCustom_Click);
			//
			// AnthroFieldMappingDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.ControlBox = false;
			this.Controls.Add(this.m_groupContents);
			this.Controls.Add(this.m_btnAddCustom);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_groupOptions);
			this.Controls.Add(this.m_cbDestination);
			this.Controls.Add(this.m_lblDestination);
			this.Name = "AnthroFieldMappingDlg";
			this.m_groupContents.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox m_groupContents;
		private System.Windows.Forms.ListView m_lvContents;
		private System.Windows.Forms.Label m_lblContents;
		private System.Windows.Forms.Label m_lblDestination;
		private System.Windows.Forms.ComboBox m_cbDestination;
		private System.Windows.Forms.GroupBox m_groupOptions;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.ToolTip m_toolTip;
		private System.Windows.Forms.Button m_btnAddCustom;
		private System.Windows.Forms.ColumnHeader columnHeader1;
	}
}