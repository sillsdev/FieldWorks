using System;
using System.Diagnostics;
using System.Windows.Forms;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	partial class HCMaxCompoundRulesDlg
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
#pragma warning disable CS0414 // Field is assigned but its value is never used
		private System.ComponentModel.IContainer components = null;
#pragma warning restore CS0414

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_dsCompoundRules != null)
				{
					m_dsCompoundRules.Dispose();
					m_dsCompoundRules = null;
				}
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HCMaxCompoundRulesDlg));
			this.m_dataGrid = new System.Windows.Forms.DataGrid();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.m_dataGrid)).BeginInit();
			this.SuspendLayout();
			//
			// m_dataGrid
			//
			this.m_dataGrid.CaptionText = "Set maximum applications for compound rules";
			this.m_dataGrid.DataMember = "";
			this.m_dataGrid.HeaderForeColor = System.Drawing.SystemColors.ControlText;
			this.m_dataGrid.Location = new System.Drawing.Point(0, 0);
			this.m_dataGrid.Name = "m_dataGrid";
			this.m_dataGrid.Size = new System.Drawing.Size(800, 379);
			this.m_dataGrid.TabIndex = 4;
			//
			// m_btnCancel
			//
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Location = new System.Drawing.Point(368, 400);
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.Size = new System.Drawing.Size(112, 35);
			this.m_btnCancel.TabIndex = 1;
			this.m_btnCancel.Text = "Cancel";
			//
			// m_btnOK
			//
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOK.Location = new System.Drawing.Point(247, 400);
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.Size = new System.Drawing.Size(112, 35);
			this.m_btnOK.TabIndex = 2;
			this.m_btnOK.Text = "OK";
			this.m_btnOK.Click += new System.EventHandler(this.btnOK_Click);
			//
			// m_btnHelp
			//
			this.m_btnHelp.Location = new System.Drawing.Point(489, 400);
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Size = new System.Drawing.Size(112, 35);
			this.m_btnHelp.TabIndex = 3;
			this.m_btnHelp.Text = "Help";
			this.m_btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// HCMaxCompoundRulesDlg
			//
			this.AcceptButton = this.m_btnOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_dataGrid);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "HCMaxCompoundRulesDlg";
			this.Text = "HC Max Compound Rules Applications";
			((System.ComponentModel.ISupportInitialize)(this.m_dataGrid)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnOK;
		private Button m_btnHelp;
	}
}