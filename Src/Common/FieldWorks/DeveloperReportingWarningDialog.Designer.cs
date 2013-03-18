using System.Diagnostics;

namespace SIL.FieldWorks
{
	partial class DeveloperReportingWarningDialog
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
			Debug.WriteLineIf(!disposing, "********** Missing Dispose() call for " + GetType().Name + ". **********");
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DeveloperReportingWarningDialog));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.m_okButton = new System.Windows.Forms.Button();
			this.m_doNotSendLink = new System.Windows.Forms.LinkLabel();
			this.SuspendLayout();
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// label3
			// 
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			// 
			// m_okButton
			// 
			this.m_okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.m_okButton, "m_okButton");
			this.m_okButton.Name = "m_okButton";
			this.m_okButton.UseVisualStyleBackColor = true;
			// 
			// m_doNotSendLink
			// 
			resources.ApplyResources(this.m_doNotSendLink, "m_doNotSendLink");
			this.m_doNotSendLink.Name = "m_doNotSendLink";
			this.m_doNotSendLink.TabStop = true;
			this.m_doNotSendLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_doNotSendLink_LinkClicked);
			// 
			// DeveloperReportingWarningDialog
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_doNotSendLink);
			this.Controls.Add(this.m_okButton);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DeveloperReportingWarningDialog";
			this.ShowIcon = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button m_okButton;
		private System.Windows.Forms.LinkLabel m_doNotSendLink;
	}
}