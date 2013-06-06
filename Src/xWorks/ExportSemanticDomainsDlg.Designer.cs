namespace SIL.FieldWorks.XWorks
{
	partial class ExportSemanticDomainsDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExportSemanticDomainsDlg));
			this.label1 = new System.Windows.Forms.Label();
			this.m_writingSystemsListBox = new System.Windows.Forms.ListBox();
			this.m_EnglishInRedCheckBox = new System.Windows.Forms.CheckBox();
			this.m_okButton = new System.Windows.Forms.Button();
			this.m_cancelButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// m_writingSystemsListBox
			// 
			this.m_writingSystemsListBox.FormattingEnabled = true;
			resources.ApplyResources(this.m_writingSystemsListBox, "m_writingSystemsListBox");
			this.m_writingSystemsListBox.Name = "m_writingSystemsListBox";
			this.m_writingSystemsListBox.SelectedIndexChanged += new System.EventHandler(this.m_writingSystemsListBox_SelectedIndexChanged);
			// 
			// m_EnglishInRedCheckBox
			// 
			resources.ApplyResources(this.m_EnglishInRedCheckBox, "m_EnglishInRedCheckBox");
			this.m_EnglishInRedCheckBox.Name = "m_EnglishInRedCheckBox";
			this.m_EnglishInRedCheckBox.UseVisualStyleBackColor = true;
			// 
			// m_okButton
			// 
			this.m_okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.m_okButton, "m_okButton");
			this.m_okButton.Name = "m_okButton";
			this.m_okButton.UseVisualStyleBackColor = true;
			// 
			// m_cancelButton
			// 
			this.m_cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_cancelButton, "m_cancelButton");
			this.m_cancelButton.Name = "m_cancelButton";
			this.m_cancelButton.UseVisualStyleBackColor = true;
			// 
			// ExportSemanticDomainsDlg
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_cancelButton);
			this.Controls.Add(this.m_okButton);
			this.Controls.Add(this.m_EnglishInRedCheckBox);
			this.Controls.Add(this.m_writingSystemsListBox);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ExportSemanticDomainsDlg";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ListBox m_writingSystemsListBox;
		private System.Windows.Forms.CheckBox m_EnglishInRedCheckBox;
		private System.Windows.Forms.Button m_okButton;
		private System.Windows.Forms.Button m_cancelButton;
	}
}