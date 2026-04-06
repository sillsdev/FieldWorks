namespace SIL.PcPatrFLEx
{
	partial class ExtractAnaFilesFromTextsForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExtractAnaFilesFromTextsForm));
			this.lblTexts = new System.Windows.Forms.Label();
			this.btnExtract = new System.Windows.Forms.Button();
			this.lbTexts = new System.Windows.Forms.ListBox();
			this.SuspendLayout();
			// 
			// lblTexts
			// 
			this.lblTexts.AutoSize = true;
			this.lblTexts.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblTexts.Location = new System.Drawing.Point(9, 43);
			this.lblTexts.Name = "lblTexts";
			this.lblTexts.Size = new System.Drawing.Size(61, 25);
			this.lblTexts.TabIndex = 7;
			this.lblTexts.Text = "Texts";
			// 
			// btnExtract
			// 
			this.btnExtract.Location = new System.Drawing.Point(17, 888);
			this.btnExtract.Name = "btnExtract";
			this.btnExtract.Size = new System.Drawing.Size(343, 38);
			this.btnExtract.TabIndex = 8;
			this.btnExtract.Text = "&Extract Ana files from selected texts";
			this.btnExtract.UseVisualStyleBackColor = true;
			this.btnExtract.Click += new System.EventHandler(this.ExtractAnaFromSelectedTexts_Click);
			// 
			// lbTexts
			// 
			this.lbTexts.FormattingEnabled = true;
			this.lbTexts.ItemHeight = 20;
			this.lbTexts.Location = new System.Drawing.Point(17, 70);
			this.lbTexts.Name = "lbTexts";
			this.lbTexts.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.lbTexts.Size = new System.Drawing.Size(1166, 804);
			this.lbTexts.TabIndex = 9;
			// 
			// ExtractAnaFilesFromTextsForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1228, 947);
			this.Controls.Add(this.lbTexts);
			this.Controls.Add(this.btnExtract);
			this.Controls.Add(this.lblTexts);
			// Following causes a crash; not sure why
			//this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ExtractAnaFilesFromTextsForm";
			this.Text = "Extract Ana Files from FLEx Texts";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Label lblTexts;
		private System.Windows.Forms.Button btnExtract;
		private System.Windows.Forms.ListBox lbTexts;
	}
}

