namespace SIL.FieldWorks.Discourse
{
	partial class SelectClausesDialog
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SelectClausesDialog));
			this.label1 = new System.Windows.Forms.Label();
			this.m_rowsCombo = new System.Windows.Forms.ComboBox();
			this.m_cancelButton = new System.Windows.Forms.Button();
			this.m_OkButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// m_rowsCombo
			//
			this.m_rowsCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_rowsCombo.FormattingEnabled = true;
			resources.ApplyResources(this.m_rowsCombo, "m_rowsCombo");
			this.m_rowsCombo.Name = "m_rowsCombo";
			//
			// m_cancelButton
			//
			this.m_cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_cancelButton, "m_cancelButton");
			this.m_cancelButton.Name = "m_cancelButton";
			this.m_cancelButton.UseVisualStyleBackColor = true;
			this.m_cancelButton.Click += new System.EventHandler(this.m_cancelButton_Click);
			//
			// m_OkButton
			//
			resources.ApplyResources(this.m_OkButton, "m_OkButton");
			this.m_OkButton.Name = "m_OkButton";
			this.m_OkButton.UseVisualStyleBackColor = true;
			this.m_OkButton.Click += new System.EventHandler(this.m_OkButton_Click);
			//
			// SelectClausesDialog
			//
			this.AcceptButton = this.m_OkButton;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_cancelButton;
			this.Controls.Add(this.m_OkButton);
			this.Controls.Add(this.m_cancelButton);
			this.Controls.Add(this.m_rowsCombo);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SelectClausesDialog";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox m_rowsCombo;
		private System.Windows.Forms.Button m_cancelButton;
		private System.Windows.Forms.Button m_OkButton;
	}
}