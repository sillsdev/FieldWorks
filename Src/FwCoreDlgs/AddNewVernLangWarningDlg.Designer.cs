namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class AddNewVernLangWarningDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddNewVernLangWarningDlg));
			this._buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.panel1 = new System.Windows.Forms.Panel();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.warningIconBox = new System.Windows.Forms.PictureBox();
			this._helpBtn = new System.Windows.Forms.Button();
			this.button1 = new System.Windows.Forms.Button();
			this._yesButton = new System.Windows.Forms.Button();
			this._buttonPanel.SuspendLayout();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.warningIconBox)).BeginInit();
			this.SuspendLayout();
			// 
			// _buttonPanel
			// 
			resources.ApplyResources(this._buttonPanel, "_buttonPanel");
			this._buttonPanel.Controls.Add(this._helpBtn);
			this._buttonPanel.Controls.Add(this.button1);
			this._buttonPanel.Controls.Add(this._yesButton);
			this._buttonPanel.Name = "_buttonPanel";
			// 
			// panel1
			// 
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.BackColor = System.Drawing.SystemColors.Control;
			this.panel1.Controls.Add(this.textBox1);
			this.panel1.Controls.Add(this.warningIconBox);
			this.panel1.Name = "panel1";
			// 
			// textBox1
			// 
			this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.textBox1, "textBox1");
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			// 
			// warningIconBox
			// 
			resources.ApplyResources(this.warningIconBox, "warningIconBox");
			this.warningIconBox.Name = "warningIconBox";
			this.warningIconBox.TabStop = false;
			// 
			// _helpBtn
			// 
			resources.ApplyResources(this._helpBtn, "_helpBtn");
			this._helpBtn.Name = "_helpBtn";
			this._helpBtn.UseVisualStyleBackColor = true;
			this._helpBtn.Click += new System.EventHandler(this._helpBtn_Click);
			// 
			// button1
			// 
			resources.ApplyResources(this.button1, "button1");
			this.button1.Name = "button1";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// _yesButton
			// 
			resources.ApplyResources(this._yesButton, "_yesButton");
			this._yesButton.Name = "_yesButton";
			this._yesButton.UseVisualStyleBackColor = true;
			this._yesButton.Click += new System.EventHandler(this._yesButton_Click);
			// 
			// AddNewVernLangWarningDlg
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add(this.panel1);
			this.Controls.Add(this._buttonPanel);
			this.Name = "AddNewVernLangWarningDlg";
			this._buttonPanel.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.warningIconBox)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.FlowLayoutPanel _buttonPanel;
		private System.Windows.Forms.Button _helpBtn;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button _yesButton;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.PictureBox warningIconBox;
	}
}