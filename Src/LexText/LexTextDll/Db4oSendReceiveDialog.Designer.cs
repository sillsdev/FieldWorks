namespace SIL.FieldWorks.XWorks.LexText
{
	partial class Db4oSendReceiveDialog
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Db4oSendReceiveDialog));
			this.m_OKButton = new System.Windows.Forms.Button();
			this.linkLabel1 = new System.Windows.Forms.LinkLabel();
			this.m_dialogText = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			//
			// m_OKButton
			//
			resources.ApplyResources(this.m_OKButton, "m_OKButton");
			this.m_OKButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_OKButton.Name = "m_OKButton";
			this.m_OKButton.UseVisualStyleBackColor = true;
			//
			// linkLabel1
			//
			resources.ApplyResources(this.linkLabel1, "linkLabel1");
			this.linkLabel1.Name = "linkLabel1";
			this.linkLabel1.TabStop = true;
			this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
			//
			// m_dialogText
			//
			this.m_dialogText.BackColor = System.Drawing.SystemColors.Control;
			this.m_dialogText.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.m_dialogText, "m_dialogText");
			this.m_dialogText.Name = "m_dialogText";
			this.m_dialogText.ReadOnly = true;
			this.m_dialogText.TabStop = false;
			//
			// Db4oSendReceiveDialog
			//
			this.AcceptButton = this.m_OKButton;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_OKButton;
			this.Controls.Add(this.m_dialogText);
			this.Controls.Add(this.linkLabel1);
			this.Controls.Add(this.m_OKButton);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Db4oSendReceiveDialog";
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button m_OKButton;
		private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.TextBox m_dialogText;
	}
}