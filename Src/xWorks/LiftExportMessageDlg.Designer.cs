using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.XWorks
{
	partial class LiftExportMessageDlg
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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LiftExportMessageDlg));
			this.m_linkWeSay = new System.Windows.Forms.LinkLabel();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_tbMessage = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			//
			// m_linkWeSay
			//
			resources.ApplyResources(this.m_linkWeSay, "m_linkWeSay");
			this.m_linkWeSay.Name = "m_linkWeSay";
			this.m_linkWeSay.TabStop = true;
			this.m_linkWeSay.Tag = "http://www.wesay.org/wiki/ShareWithFLEx ";
			this.m_linkWeSay.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_linkWeSay_LinkClicked);
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
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			//
			// m_tbMessage
			//
			this.m_tbMessage.BackColor = System.Drawing.SystemColors.Control;
			this.m_tbMessage.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.m_tbMessage, "m_tbMessage");
			this.AccessibleName = "LiftExportMessageDlg";
			this.m_tbMessage.Name = "m_tbMessage";
			this.m_tbMessage.ReadOnly = true;
			this.m_tbMessage.TabStop = false;
			//
			// LiftExportMessageDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.ControlBox = false;
			this.Controls.Add(this.m_tbMessage);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_linkWeSay);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MinimizeBox = false;
			this.Name = "LiftExportMessageDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.Tag = "";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.LinkLabel m_linkWeSay;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.TextBox m_tbMessage;

	}
}
