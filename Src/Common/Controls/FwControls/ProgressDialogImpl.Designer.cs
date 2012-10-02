// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RealProgressDialog.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.Common.Controls
{
	partial class ProgressDialogImpl
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
			System.Diagnostics.Debug.WriteLine(string.Format("Start of ProgressDialogImpl.Dispose({0}) ({1})",
				disposing, System.Threading.Thread.CurrentThread.Name));
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
			System.Diagnostics.Debug.WriteLine(string.Format("End of ProgressDialogImpl.Dispose({0}) ({1} - {2:x})",
				disposing, System.Threading.Thread.CurrentThread.Name,
				SIL.FieldWorks.Common.Utils.Win32.GetCurrentThreadId()));
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProgressDialogImpl));
			this.lblStatusMessage = new System.Windows.Forms.Label();
			this.progressBar = new System.Windows.Forms.ProgressBar();
			this.btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// lblStatusMessage
			//
			this.lblStatusMessage.AutoEllipsis = true;
			resources.ApplyResources(this.lblStatusMessage, "lblStatusMessage");
			this.lblStatusMessage.Name = "lblStatusMessage";
			//
			// progressBar
			//
			resources.ApplyResources(this.progressBar, "progressBar");
			this.progressBar.Name = "progressBar";
			this.progressBar.Step = 1;
			//
			// btnCancel
			//
			this.btnCancel.Cursor = System.Windows.Forms.Cursors.Arrow;
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			//
			// ProgressDialogImpl
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ControlBox = false;
			this.Controls.Add(this.lblStatusMessage);
			this.Controls.Add(this.progressBar);
			this.Controls.Add(this.btnCancel);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ProgressDialogImpl";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label lblStatusMessage;
		private System.Windows.Forms.ProgressBar progressBar;
		protected System.Windows.Forms.Button btnCancel;
	}
}