
using System.Windows.Forms;

namespace SIL.WordWorks.GAFAWS.FWConverter
{
	partial class FWConverterDlg
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
			this.m_source = new TIData.NetworkSelect.NetworkSelect();
			this.label1 = new System.Windows.Forms.Label();
			this.m_tvPOS = new System.Windows.Forms.TreeView();
			this.label2 = new System.Windows.Forms.Label();
			this.m_btnOk = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_cbIncludeSubcategories = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			//
			// m_source
			//
			this.m_source.Location = new System.Drawing.Point(12, 30);
			this.m_source.Name = "m_source";
			this.m_source.Size = new System.Drawing.Size(237, 275);
			this.m_source.TabIndex = 0;
			//
			// label1
			//
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 11);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(74, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Select Source";
			//
			// m_tvPOS
			//
			this.m_tvPOS.Location = new System.Drawing.Point(272, 30);
			this.m_tvPOS.Name = "m_tvPOS";
			this.m_tvPOS.Size = new System.Drawing.Size(247, 275);
			this.m_tvPOS.TabIndex = 2;
			this.m_tvPOS.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.m_tvPOS_AfterSelect);
			//
			// label2
			//
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(272, 11);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(111, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Select Part of Speech";
			//
			// m_btnOk
			//
			this.m_btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOk.Enabled = false;
			this.m_btnOk.Location = new System.Drawing.Point(162, 338);
			this.m_btnOk.Name = "m_btnOk";
			this.m_btnOk.Size = new System.Drawing.Size(75, 23);
			this.m_btnOk.TabIndex = 4;
			this.m_btnOk.Text = "Convert";
			this.m_btnOk.UseVisualStyleBackColor = true;
			//
			// m_btnCancel
			//
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Location = new System.Drawing.Point(286, 338);
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.Size = new System.Drawing.Size(75, 23);
			this.m_btnCancel.TabIndex = 5;
			this.m_btnCancel.Text = "Cancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			//
			// m_cbIncludeSubcategories
			//
			this.m_cbIncludeSubcategories.AutoSize = true;
			this.m_cbIncludeSubcategories.Enabled = false;
			this.m_cbIncludeSubcategories.Location = new System.Drawing.Point(272, 312);
			this.m_cbIncludeSubcategories.Name = "m_cbIncludeSubcategories";
			this.m_cbIncludeSubcategories.Size = new System.Drawing.Size(130, 17);
			this.m_cbIncludeSubcategories.TabIndex = 6;
			this.m_cbIncludeSubcategories.Text = "Include subcategories";
			this.m_cbIncludeSubcategories.UseVisualStyleBackColor = true;
			//
			// FWConverterDlg
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(534, 367);
			this.Controls.Add(this.m_cbIncludeSubcategories);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOk);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.m_tvPOS);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_source);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FWConverterDlg";
			this.Text = "FieldWorks Converter";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private TIData.NetworkSelect.NetworkSelect m_source;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TreeView m_tvPOS;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button m_btnOk;
		private System.Windows.Forms.Button m_btnCancel;
		private CheckBox m_cbIncludeSubcategories;
	}
}