// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.TE
{
	partial class ImportedBooks
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
			if (disposing)
			{
				if (lstImportedBooks != null)
				{
					foreach (ListViewItem item in lstImportedBooks.Items)
					{
						var disposable = item.Tag as IDisposable;
						if (disposable != null)
							disposable.Dispose();
					}
				}
				if (components != null)
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportedBooks));
			this.label1 = new System.Windows.Forms.Label();
			this.lstImportedBooks = new System.Windows.Forms.ListView();
			this.colBook = new System.Windows.Forms.ColumnHeader();
			this.colRange = new System.Windows.Forms.ColumnHeader();
			this.colStatus = new System.Windows.Forms.ColumnHeader();
			this.m_imageListSmall = new System.Windows.Forms.ImageList(this.components);
			this.btnCompare = new System.Windows.Forms.Button();
			this.btnClose = new System.Windows.Forms.Button();
			this.btnHelp = new System.Windows.Forms.Button();
			this.btnOverwrite = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// lstImportedBooks
			//
			resources.ApplyResources(this.lstImportedBooks, "lstImportedBooks");
			this.lstImportedBooks.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.colBook,
			this.colRange,
			this.colStatus});
			this.lstImportedBooks.FullRowSelect = true;
			this.lstImportedBooks.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.lstImportedBooks.HideSelection = false;
			this.lstImportedBooks.MultiSelect = false;
			this.lstImportedBooks.Name = "lstImportedBooks";
			this.lstImportedBooks.ShowGroups = false;
			this.lstImportedBooks.SmallImageList = this.m_imageListSmall;
			this.lstImportedBooks.UseCompatibleStateImageBehavior = false;
			this.lstImportedBooks.View = System.Windows.Forms.View.Details;
			this.lstImportedBooks.SelectedIndexChanged += new System.EventHandler(this.lstBooksToMerge_SelectedIndexChanged);
			this.lstImportedBooks.DoubleClick += new System.EventHandler(this.btnCompare_Click);
			//
			// colBook
			//
			resources.ApplyResources(this.colBook, "colBook");
			//
			// colRange
			//
			resources.ApplyResources(this.colRange, "colRange");
			//
			// colStatus
			//
			resources.ApplyResources(this.colStatus, "colStatus");
			//
			// m_imageListSmall
			//
			this.m_imageListSmall.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_imageListSmall.ImageStream")));
			this.m_imageListSmall.TransparentColor = System.Drawing.Color.Magenta;
			this.m_imageListSmall.Images.SetKeyName(0, "ConfirmTransparent16x16.gif");
			this.m_imageListSmall.Images.SetKeyName(1, "Delete_Project.bmp");
			//
			// btnCompare
			//
			resources.ApplyResources(this.btnCompare, "btnCompare");
			this.btnCompare.Name = "btnCompare";
			this.btnCompare.UseVisualStyleBackColor = true;
			this.btnCompare.Click += new System.EventHandler(this.btnCompare_Click);
			//
			// btnClose
			//
			resources.ApplyResources(this.btnClose, "btnClose");
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnClose.Name = "btnClose";
			this.btnClose.UseVisualStyleBackColor = true;
			this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.UseVisualStyleBackColor = true;
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// btnOverwrite
			//
			resources.ApplyResources(this.btnOverwrite, "btnOverwrite");
			this.btnOverwrite.Name = "btnOverwrite";
			this.btnOverwrite.UseVisualStyleBackColor = true;
			this.btnOverwrite.Click += new System.EventHandler(this.btnOverwrite_Click);
			//
			// ImportedBooks
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnClose;
			this.Controls.Add(this.btnOverwrite);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.btnClose);
			this.Controls.Add(this.btnCompare);
			this.Controls.Add(this.lstImportedBooks);
			this.Controls.Add(this.label1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ImportedBooks";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		/// <summary></summary>
		protected System.Windows.Forms.ListView lstImportedBooks;
		private System.Windows.Forms.ColumnHeader colBook;
		private System.Windows.Forms.ColumnHeader colStatus;
		private System.Windows.Forms.ImageList m_imageListSmall;
		private System.Windows.Forms.Button btnCompare;
		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Button btnHelp;
		private System.Windows.Forms.Button btnOverwrite;
		private System.Windows.Forms.ColumnHeader colRange;
	}
}