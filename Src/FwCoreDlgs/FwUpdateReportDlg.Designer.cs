// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics.CodeAnalysis;
namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class FwUpdateReportDlg
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
		// TODO-Linux: UseEXDialog is not implemented, will always use default dialog
		// (printDialog1.UseEXDialog)
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		private void InitializeComponent()
		{
			System.Windows.Forms.Label spearatorLine;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwUpdateReportDlg));
			this.lvItems = new System.Windows.Forms.ListView();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnPrintRpt = new System.Windows.Forms.Button();
			this.btnSaveRpt = new System.Windows.Forms.Button();
			this.btnHelp = new System.Windows.Forms.Button();
			this.lblWarning = new System.Windows.Forms.Label();
			this.printDialog1 = new System.Windows.Forms.PrintDialog();
			this.printDocument = new System.Drawing.Printing.PrintDocument();
			this.lblImportant = new System.Windows.Forms.Label();
			this.lblProjectName = new System.Windows.Forms.Label();
			spearatorLine = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// spearatorLine
			//
			resources.ApplyResources(spearatorLine, "spearatorLine");
			spearatorLine.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			spearatorLine.Name = "spearatorLine";
			//
			// lvItems
			//
			resources.ApplyResources(this.lvItems, "lvItems");
			this.lvItems.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader1});
			this.lvItems.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.lvItems.Name = "lvItems";
			this.lvItems.UseCompatibleStateImageBehavior = false;
			this.lvItems.View = System.Windows.Forms.View.Details;
			this.lvItems.SizeChanged += new System.EventHandler(this.lvItems_SizeChanged);
			//
			// columnHeader1
			//
			this.columnHeader1.Text = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Name = "btnOK";
			this.btnOK.UseVisualStyleBackColor = true;
			//
			// btnPrintRpt
			//
			resources.ApplyResources(this.btnPrintRpt, "btnPrintRpt");
			this.btnPrintRpt.Name = "btnPrintRpt";
			this.btnPrintRpt.UseVisualStyleBackColor = true;
			this.btnPrintRpt.Click += new System.EventHandler(this.btnPrintRpt_Click);
			//
			// btnSaveRpt
			//
			resources.ApplyResources(this.btnSaveRpt, "btnSaveRpt");
			this.btnSaveRpt.Name = "btnSaveRpt";
			this.btnSaveRpt.UseVisualStyleBackColor = true;
			this.btnSaveRpt.Click += new System.EventHandler(this.btnSaveRpt_Click);
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.UseVisualStyleBackColor = true;
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// lblWarning
			//
			resources.ApplyResources(this.lblWarning, "lblWarning");
			this.lblWarning.Name = "lblWarning";
			//
			// printDialog1
			//
			this.printDialog1.UseEXDialog = true;
			//
			// printDocument
			//
			this.printDocument.DocumentName = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
			this.printDocument.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(this.printDocument_PrintPage);
			this.printDocument.BeginPrint += new System.Drawing.Printing.PrintEventHandler(this.printDocument_BeginPrint);
			//
			// lblImportant
			//
			resources.ApplyResources(this.lblImportant, "lblImportant");
			this.lblImportant.Name = "lblImportant";
			//
			// lblProjectName
			//
			resources.ApplyResources(this.lblProjectName, "lblProjectName");
			this.lblProjectName.Name = "lblProjectName";
			//
			// FwUpdateReportDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(spearatorLine);
			this.Controls.Add(this.lblProjectName);
			this.Controls.Add(this.lblImportant);
			this.Controls.Add(this.lblWarning);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.btnSaveRpt);
			this.Controls.Add(this.btnPrintRpt);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.lvItems);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FwUpdateReportDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnPrintRpt;
		private System.Windows.Forms.Button btnSaveRpt;
		private System.Windows.Forms.Button btnHelp;
		private System.Windows.Forms.PrintDialog printDialog1;
		private System.Windows.Forms.Label lblImportant;
		/// <summary></summary>
		protected System.Windows.Forms.Label lblWarning;
		/// <summary></summary>
		protected System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ListView lvItems;
		/// <summary></summary>
		protected System.Drawing.Printing.PrintDocument printDocument;
		private System.Windows.Forms.Label lblProjectName;
	}
}