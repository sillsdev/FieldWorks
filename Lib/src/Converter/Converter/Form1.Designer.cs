namespace Converter
{
	partial class Form1
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
			this.InputText = new System.Windows.Forms.TextBox();
			this.OutputText = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.label2 = new System.Windows.Forms.Label();
			this.BrowseBtn = new System.Windows.Forms.Button();
			this.ConvertBtn = new System.Windows.Forms.Button();
			this.ExitBtn = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// InputText
			//
			this.InputText.Location = new System.Drawing.Point(191, 12);
			this.InputText.Name = "InputText";
			this.InputText.Size = new System.Drawing.Size(393, 20);
			this.InputText.TabIndex = 0;
			//
			// OutputText
			//
			this.OutputText.Location = new System.Drawing.Point(191, 42);
			this.OutputText.Name = "OutputText";
			this.OutputText.Size = new System.Drawing.Size(393, 20);
			this.OutputText.TabIndex = 1;
			//
			// label1
			//
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(32, 19);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(125, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Input XML file (Version 6)";
			//
			// openFileDialog
			//
			this.openFileDialog.FileName = "openFileDialog";
			//
			// label2
			//
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(32, 45);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(133, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Output XML file (Version 7)";
			//
			// BrowseBtn
			//
			this.BrowseBtn.Location = new System.Drawing.Point(590, 11);
			this.BrowseBtn.Name = "BrowseBtn";
			this.BrowseBtn.Size = new System.Drawing.Size(61, 21);
			this.BrowseBtn.TabIndex = 4;
			this.BrowseBtn.Text = "Browse...";
			this.BrowseBtn.UseVisualStyleBackColor = true;
			this.BrowseBtn.Click += new System.EventHandler(this.BrowseBtn_Click);
			//
			// ConvertBtn
			//
			this.ConvertBtn.Location = new System.Drawing.Point(486, 99);
			this.ConvertBtn.Name = "ConvertBtn";
			this.ConvertBtn.Size = new System.Drawing.Size(75, 23);
			this.ConvertBtn.TabIndex = 5;
			this.ConvertBtn.Text = "Convert";
			this.ConvertBtn.UseVisualStyleBackColor = true;
			this.ConvertBtn.Click += new System.EventHandler(this.ConvertBtn_Click);
			//
			// ExitBtn
			//
			this.ExitBtn.Location = new System.Drawing.Point(576, 99);
			this.ExitBtn.Name = "ExitBtn";
			this.ExitBtn.Size = new System.Drawing.Size(75, 23);
			this.ExitBtn.TabIndex = 6;
			this.ExitBtn.Text = "Cancel";
			this.ExitBtn.UseVisualStyleBackColor = true;
			this.ExitBtn.Click += new System.EventHandler(this.ExitBtn_Click);
			//
			// Form1
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(663, 134);
			this.Controls.Add(this.ExitBtn);
			this.Controls.Add(this.ConvertBtn);
			this.Controls.Add(this.BrowseBtn);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.OutputText);
			this.Controls.Add(this.InputText);
			this.Name = "Form1";
			this.Text = "Convert XML file from Version 6 to 7";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox InputText;
		private System.Windows.Forms.TextBox OutputText;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button BrowseBtn;
		private System.Windows.Forms.Button ConvertBtn;
		private System.Windows.Forms.Button ExitBtn;
	}
}
