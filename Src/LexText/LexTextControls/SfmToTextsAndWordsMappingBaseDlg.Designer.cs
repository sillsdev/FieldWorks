namespace SIL.FieldWorks.LexText.Controls
{
	partial class SfmToTextsAndWordesMappingBaseDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SfmToTextsAndWordesMappingBaseDlg));
			this.m_destinationLabel = new System.Windows.Forms.Label();
			this.m_destinationsListBox = new System.Windows.Forms.ListBox();
			this.label1 = new System.Windows.Forms.Label();
			this.m_writingSystemCombo = new System.Windows.Forms.ComboBox();
			this.m_addWritingSystemButton = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.m_converterCombo = new System.Windows.Forms.ComboBox();
			this.m_okButton = new System.Windows.Forms.Button();
			this.m_cancelButton = new System.Windows.Forms.Button();
			this.m_helpButton = new System.Windows.Forms.Button();
			this.m_addConverterButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_destinationLabel
			//
			resources.ApplyResources(this.m_destinationLabel, "m_destinationLabel");
			this.m_destinationLabel.Name = "m_destinationLabel";
			//
			// m_destinationsListBox
			//
			resources.ApplyResources(this.m_destinationsListBox, "m_destinationsListBox");
			this.m_destinationsListBox.FormattingEnabled = true;
			this.m_destinationsListBox.MinimumSize = new System.Drawing.Size(251, 160);
			this.m_destinationsListBox.Name = "m_destinationsListBox";
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// m_writingSystemCombo
			//
			resources.ApplyResources(this.m_writingSystemCombo, "m_writingSystemCombo");
			this.m_writingSystemCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_writingSystemCombo.FormattingEnabled = true;
			this.m_writingSystemCombo.Name = "m_writingSystemCombo";
			//
			// m_addWritingSystemButton
			//
			resources.ApplyResources(this.m_addWritingSystemButton, "m_addWritingSystemButton");
			this.m_addWritingSystemButton.Name = "m_addWritingSystemButton";
			this.m_addWritingSystemButton.UseVisualStyleBackColor = true;
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// m_converterCombo
			//
			resources.ApplyResources(this.m_converterCombo, "m_converterCombo");
			this.m_converterCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_converterCombo.FormattingEnabled = true;
			this.m_converterCombo.Name = "m_converterCombo";
			//
			// m_okButton
			//
			resources.ApplyResources(this.m_okButton, "m_okButton");
			this.m_okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_okButton.Name = "m_okButton";
			this.m_okButton.UseVisualStyleBackColor = true;
			this.m_okButton.Click += new System.EventHandler(this.m_okButton_Click);
			//
			// m_cancelButton
			//
			resources.ApplyResources(this.m_cancelButton, "m_cancelButton");
			this.m_cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_cancelButton.Name = "m_cancelButton";
			this.m_cancelButton.UseVisualStyleBackColor = true;
			//
			// m_helpButton
			//
			resources.ApplyResources(this.m_helpButton, "m_helpButton");
			this.m_helpButton.Name = "m_helpButton";
			this.m_helpButton.UseVisualStyleBackColor = true;
			this.m_helpButton.Click += new System.EventHandler(this.m_helpButton_Click);
			//
			// m_addConverterButton
			//
			resources.ApplyResources(this.m_addConverterButton, "m_addConverterButton");
			this.m_addConverterButton.Name = "m_addConverterButton";
			this.m_addConverterButton.UseVisualStyleBackColor = true;
			this.m_addConverterButton.Click += new System.EventHandler(this.m_addConverterButton_Click);
			//
			// SfmInterlinearMappingDlg
			//
			this.AcceptButton = this.m_okButton;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_cancelButton;
			this.Controls.Add(this.m_addConverterButton);
			this.Controls.Add(this.m_helpButton);
			this.Controls.Add(this.m_cancelButton);
			this.Controls.Add(this.m_okButton);
			this.Controls.Add(this.m_converterCombo);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.m_addWritingSystemButton);
			this.Controls.Add(this.m_writingSystemCombo);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_destinationsListBox);
			this.Controls.Add(this.m_destinationLabel);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SfmToTextsAndWordesMappingBaseDlg";
			this.ShowIcon = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_destinationLabel;
		private System.Windows.Forms.ListBox m_destinationsListBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox m_writingSystemCombo;
		private System.Windows.Forms.Button m_addWritingSystemButton;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ComboBox m_converterCombo;
		private System.Windows.Forms.Button m_okButton;
		private System.Windows.Forms.Button m_cancelButton;
		private System.Windows.Forms.Button m_helpButton;
		private System.Windows.Forms.Button m_addConverterButton;
	}
}