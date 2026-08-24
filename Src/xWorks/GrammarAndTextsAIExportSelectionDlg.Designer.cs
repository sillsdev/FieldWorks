// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
namespace SIL.FieldWorks.XWorks
{
	partial class GrammarAndTextsAIExportSelectionDlg
	{
		private System.ComponentModel.IContainer components = null;
		private System.Windows.Forms.ListView m_textListView;
		private System.Windows.Forms.ColumnHeader m_columnText;
		private System.Windows.Forms.ColumnHeader m_columnWords;
		private System.Windows.Forms.ColumnHeader m_columnAnalyses;
		private System.Windows.Forms.Button m_btnOk;
		private System.Windows.Forms.Button m_btnCancel;

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
				components.Dispose();
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			this.m_textListView = new System.Windows.Forms.ListView();
			this.m_columnText = new System.Windows.Forms.ColumnHeader();
			this.m_columnWords = new System.Windows.Forms.ColumnHeader();
			this.m_columnAnalyses = new System.Windows.Forms.ColumnHeader();
			this.m_btnOk = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_textListView
			//
			this.m_textListView.CheckBoxes = true;
			this.m_textListView.View = System.Windows.Forms.View.Details;
			this.m_textListView.FullRowSelect = true;
			this.m_textListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
				this.m_columnText, this.m_columnWords, this.m_columnAnalyses});
			this.m_textListView.Dock = System.Windows.Forms.DockStyle.Top;
			this.m_textListView.Height = 340;
			this.m_columnText.Text = xWorksStrings.ksAIExportColumnText;
			this.m_columnText.Width = 260;
			this.m_columnWords.Text = xWorksStrings.ksAIExportColumnWords;
			this.m_columnWords.Width = 80;
			this.m_columnWords.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this.m_columnAnalyses.Text = xWorksStrings.ksAIExportColumnAnalyses;
			this.m_columnAnalyses.Width = 80;
			this.m_columnAnalyses.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			//
			// m_btnOk
			//
			this.m_btnOk.Text = xWorksStrings.ksOK;
			this.m_btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOk.Location = new System.Drawing.Point(320, 350);
			this.m_btnOk.Click += new System.EventHandler(this.m_btnOk_Click);
			//
			// m_btnCancel
			//
			this.m_btnCancel.Text = xWorksStrings.ksCancel;
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Location = new System.Drawing.Point(405, 350);
			//
			// GrammarAndTextsAIExportSelectionDlg
			//
			this.AcceptButton = this.m_btnOk;
			this.CancelButton = this.m_btnCancel;
			this.ClientSize = new System.Drawing.Size(500, 390);
			this.Controls.Add(this.m_textListView);
			this.Controls.Add(this.m_btnOk);
			this.Controls.Add(this.m_btnCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = xWorksStrings.ksSelectTextsForAIExportTitle;
			this.ResumeLayout(false);
		}
	}
}
