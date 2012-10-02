// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SequenceOptionsDlg.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.TE
{
	partial class SequenceOptionsDlg
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
			System.Windows.Forms.Button btnCancel;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SequenceOptionsDlg));
			System.Windows.Forms.Button btnHelp;
			this.opnRestart = new System.Windows.Forms.RadioButton();
			this.opnContinuous = new System.Windows.Forms.RadioButton();
			this.btnOK = new System.Windows.Forms.Button();
			btnCancel = new System.Windows.Forms.Button();
			btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// btnCancel
			//
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Name = "btnCancel";
			btnCancel.UseVisualStyleBackColor = true;
			//
			// btnHelp
			//
			resources.ApplyResources(btnHelp, "btnHelp");
			btnHelp.Name = "btnHelp";
			btnHelp.UseVisualStyleBackColor = true;
			btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// opnRestart
			//
			resources.ApplyResources(this.opnRestart, "opnRestart");
			this.opnRestart.Name = "opnRestart";
			this.opnRestart.TabStop = true;
			this.opnRestart.UseVisualStyleBackColor = true;
			//
			// opnContinuous
			//
			resources.ApplyResources(this.opnContinuous, "opnContinuous");
			this.opnContinuous.Name = "opnContinuous";
			this.opnContinuous.TabStop = true;
			this.opnContinuous.UseVisualStyleBackColor = true;
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Name = "btnOK";
			this.btnOK.UseVisualStyleBackColor = true;
			//
			// SequenceOptionsDlg
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.Controls.Add(btnHelp);
			this.Controls.Add(btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.opnContinuous);
			this.Controls.Add(this.opnRestart);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SequenceOptionsDlg";
			this.ShowIcon = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.RadioButton opnRestart;
		private System.Windows.Forms.RadioButton opnContinuous;
		private System.Windows.Forms.Button btnOK;
	}
}