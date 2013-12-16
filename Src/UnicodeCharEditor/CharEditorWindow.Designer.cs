// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Form1.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	partial class CharEditorWindow
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CharEditorWindow));
			this.m_lvCharSpecs = new System.Windows.Forms.ListView();
			this.m_hdrCode = new System.Windows.Forms.ColumnHeader();
			this.m_hdrName = new System.Windows.Forms.ColumnHeader();
			this.m_hdrCateg = new System.Windows.Forms.ColumnHeader();
			this.m_hdrCombin = new System.Windows.Forms.ColumnHeader();
			this.m_hdrBidi = new System.Windows.Forms.ColumnHeader();
			this.m_hdrDecomp = new System.Windows.Forms.ColumnHeader();
			this.m_hdrMirrored = new System.Windows.Forms.ColumnHeader();
			this.m_hdrUpper = new System.Windows.Forms.ColumnHeader();
			this.m_hdrLower = new System.Windows.Forms.ColumnHeader();
			this.m_hdrTitle = new System.Windows.Forms.ColumnHeader();
			this.m_btnAdd = new System.Windows.Forms.Button();
			this.m_btnEdit = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnClose = new System.Windows.Forms.Button();
			this.m_btnSave = new System.Windows.Forms.Button();
			this.m_btnDelete = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_lvCharSpecs
			//
			resources.ApplyResources(this.m_lvCharSpecs, "m_lvCharSpecs");
			this.m_lvCharSpecs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.m_hdrCode,
			this.m_hdrName,
			this.m_hdrCateg,
			this.m_hdrCombin,
			this.m_hdrBidi,
			this.m_hdrDecomp,
			this.m_hdrMirrored,
			this.m_hdrUpper,
			this.m_hdrLower,
			this.m_hdrTitle});
			this.m_lvCharSpecs.FullRowSelect = true;
			this.m_lvCharSpecs.MultiSelect = false;
			this.m_lvCharSpecs.Name = "m_lvCharSpecs";
			this.m_lvCharSpecs.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.m_lvCharSpecs.UseCompatibleStateImageBehavior = false;
			this.m_lvCharSpecs.View = System.Windows.Forms.View.Details;
			this.m_lvCharSpecs.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.m_lvCharSpecs_MouseDoubleClick);
			//
			// m_hdrCode
			//
			resources.ApplyResources(this.m_hdrCode, "m_hdrCode");
			//
			// m_hdrName
			//
			resources.ApplyResources(this.m_hdrName, "m_hdrName");
			//
			// m_hdrCateg
			//
			resources.ApplyResources(this.m_hdrCateg, "m_hdrCateg");
			//
			// m_hdrCombin
			//
			resources.ApplyResources(this.m_hdrCombin, "m_hdrCombin");
			//
			// m_hdrBidi
			//
			resources.ApplyResources(this.m_hdrBidi, "m_hdrBidi");
			//
			// m_hdrDecomp
			//
			resources.ApplyResources(this.m_hdrDecomp, "m_hdrDecomp");
			//
			// m_hdrMirrored
			//
			resources.ApplyResources(this.m_hdrMirrored, "m_hdrMirrored");
			//
			// m_hdrUpper
			//
			resources.ApplyResources(this.m_hdrUpper, "m_hdrUpper");
			//
			// m_hdrLower
			//
			resources.ApplyResources(this.m_hdrLower, "m_hdrLower");
			//
			// m_hdrTitle
			//
			resources.ApplyResources(this.m_hdrTitle, "m_hdrTitle");
			//
			// m_btnAdd
			//
			resources.ApplyResources(this.m_btnAdd, "m_btnAdd");
			this.m_btnAdd.Name = "m_btnAdd";
			this.m_btnAdd.UseVisualStyleBackColor = true;
			this.m_btnAdd.Click += new System.EventHandler(this.m_btnAdd_Click);
			//
			// m_btnEdit
			//
			resources.ApplyResources(this.m_btnEdit, "m_btnEdit");
			this.m_btnEdit.Name = "m_btnEdit";
			this.m_btnEdit.UseVisualStyleBackColor = true;
			this.m_btnEdit.Click += new System.EventHandler(this.m_btnEdit_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnClose
			//
			resources.ApplyResources(this.m_btnClose, "m_btnClose");
			this.m_btnClose.Name = "m_btnClose";
			this.m_btnClose.UseVisualStyleBackColor = true;
			this.m_btnClose.Click += new System.EventHandler(this.m_btnClose_Click);
			//
			// m_btnSave
			//
			resources.ApplyResources(this.m_btnSave, "m_btnSave");
			this.m_btnSave.Name = "m_btnSave";
			this.m_btnSave.UseVisualStyleBackColor = true;
			this.m_btnSave.Click += new System.EventHandler(this.m_btnSave_Click);
			//
			// m_btnDelete
			//
			resources.ApplyResources(this.m_btnDelete, "m_btnDelete");
			this.m_btnDelete.Name = "m_btnDelete";
			this.m_btnDelete.UseVisualStyleBackColor = true;
			this.m_btnDelete.Click += new System.EventHandler(this.m_btnDelete_Click);
			//
			// CharEditorWindow
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_btnDelete);
			this.Controls.Add(this.m_btnClose);
			this.Controls.Add(this.m_btnSave);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnEdit);
			this.Controls.Add(this.m_btnAdd);
			this.Controls.Add(this.m_lvCharSpecs);
			this.Name = "CharEditorWindow";
			this.ShowIcon = false;
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListView m_lvCharSpecs;
		private System.Windows.Forms.ColumnHeader m_hdrCode;
		private System.Windows.Forms.ColumnHeader m_hdrName;
		private System.Windows.Forms.ColumnHeader m_hdrCateg;
		private System.Windows.Forms.ColumnHeader m_hdrCombin;
		private System.Windows.Forms.ColumnHeader m_hdrBidi;
		private System.Windows.Forms.ColumnHeader m_hdrDecomp;
		private System.Windows.Forms.ColumnHeader m_hdrMirrored;
		private System.Windows.Forms.ColumnHeader m_hdrUpper;
		private System.Windows.Forms.ColumnHeader m_hdrLower;
		private System.Windows.Forms.ColumnHeader m_hdrTitle;
		private System.Windows.Forms.Button m_btnAdd;
		private System.Windows.Forms.Button m_btnEdit;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.Button m_btnClose;
		private System.Windows.Forms.Button m_btnSave;
		private System.Windows.Forms.Button m_btnDelete;


	}
}
