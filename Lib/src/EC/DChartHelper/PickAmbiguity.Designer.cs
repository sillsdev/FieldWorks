namespace DChartHelper
{
	partial class PickAmbiguity
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
			this.listBoxAmbiguousWords = new System.Windows.Forms.ListBox();
			this.SuspendLayout();
			//
			// listBoxAmbiguousWords
			//
			this.listBoxAmbiguousWords.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBoxAmbiguousWords.FormattingEnabled = true;
			this.listBoxAmbiguousWords.Location = new System.Drawing.Point(0, 0);
			this.listBoxAmbiguousWords.Name = "listBoxAmbiguousWords";
			this.listBoxAmbiguousWords.Size = new System.Drawing.Size(292, 264);
			this.listBoxAmbiguousWords.TabIndex = 0;
			this.listBoxAmbiguousWords.SelectedIndexChanged += new System.EventHandler(this.listBoxAmbiguousWords_SelectedIndexChanged);
			//
			// PickAmbiguity
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(292, 266);
			this.Controls.Add(this.listBoxAmbiguousWords);
			this.Name = "PickAmbiguity";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Pick Ambiguity";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListBox listBoxAmbiguousWords;
	}
}