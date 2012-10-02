// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DateFieldOptions.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	partial class DateFieldOptions
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DateFieldOptions));
			this.m_lblDateFormats = new System.Windows.Forms.Label();
			this.m_lvDateFormats = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.m_btnAddFormat = new System.Windows.Forms.Button();
			this.m_toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.m_btnModifyFormat = new System.Windows.Forms.Button();
			this.m_btnDeleteFormat = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_lblDateFormats
			//
			resources.ApplyResources(this.m_lblDateFormats, "m_lblDateFormats");
			this.m_lblDateFormats.Name = "m_lblDateFormats";
			//
			// m_lvDateFormats
			//
			resources.ApplyResources(this.m_lvDateFormats, "m_lvDateFormats");
			this.m_lvDateFormats.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader1,
			this.columnHeader2});
			this.m_lvDateFormats.Name = "m_lvDateFormats";
			this.m_toolTip.SetToolTip(this.m_lvDateFormats, resources.GetString("m_lvDateFormats.ToolTip"));
			this.m_lvDateFormats.UseCompatibleStateImageBehavior = false;
			this.m_lvDateFormats.View = System.Windows.Forms.View.Details;
			//
			// columnHeader1
			//
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			//
			// columnHeader2
			//
			resources.ApplyResources(this.columnHeader2, "columnHeader2");
			//
			// m_btnAddFormat
			//
			resources.ApplyResources(this.m_btnAddFormat, "m_btnAddFormat");
			this.m_btnAddFormat.Name = "m_btnAddFormat";
			this.m_toolTip.SetToolTip(this.m_btnAddFormat, resources.GetString("m_btnAddFormat.ToolTip"));
			this.m_btnAddFormat.UseVisualStyleBackColor = true;
			this.m_btnAddFormat.Click += new System.EventHandler(this.m_btnAddFormat_Click);
			//
			// m_btnModifyFormat
			//
			resources.ApplyResources(this.m_btnModifyFormat, "m_btnModifyFormat");
			this.m_btnModifyFormat.Name = "m_btnModifyFormat";
			this.m_toolTip.SetToolTip(this.m_btnModifyFormat, resources.GetString("m_btnModifyFormat.ToolTip"));
			this.m_btnModifyFormat.UseVisualStyleBackColor = true;
			this.m_btnModifyFormat.Click += new System.EventHandler(this.m_btnModifyFormat_Click);
			//
			// m_btnDeleteFormat
			//
			resources.ApplyResources(this.m_btnDeleteFormat, "m_btnDeleteFormat");
			this.m_btnDeleteFormat.Name = "m_btnDeleteFormat";
			this.m_toolTip.SetToolTip(this.m_btnDeleteFormat, resources.GetString("m_btnDeleteFormat.ToolTip"));
			this.m_btnDeleteFormat.UseVisualStyleBackColor = true;
			this.m_btnDeleteFormat.Click += new System.EventHandler(this.m_btnDeleteFormat_Click);
			//
			// DateFieldOptions
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_btnDeleteFormat);
			this.Controls.Add(this.m_btnModifyFormat);
			this.Controls.Add(this.m_btnAddFormat);
			this.Controls.Add(this.m_lvDateFormats);
			this.Controls.Add(this.m_lblDateFormats);
			this.Name = "DateFieldOptions";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lblDateFormats;
		private System.Windows.Forms.ListView m_lvDateFormats;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.Button m_btnAddFormat;
		private System.Windows.Forms.ToolTip m_toolTip;
		private System.Windows.Forms.Button m_btnModifyFormat;
		private System.Windows.Forms.Button m_btnDeleteFormat;
	}
}
