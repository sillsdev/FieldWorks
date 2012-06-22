namespace SIL.FieldWorks.IText
{
	partial class WordsSfmImportWizard
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
			this.SuspendLayout();
			//
			// label2
			//
			this.label2.Text = "Using this tool, you can import words and their glosses from lexical entries that" +
				" have been marked up with standard format markers..";
			//
			// WordsSfmImportWizard
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(534, 354);
			this.Name = "WordsSfmImportWizard";
			this.StepNames = new string[] {
		"Import Files",
		"Field Mapping",
		"Ready to import"};
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}