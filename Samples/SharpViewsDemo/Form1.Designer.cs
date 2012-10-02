namespace SharpViewsDemo
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
			this.whichView = new System.Windows.Forms.ComboBox();
			this.theSharpView = new SIL.FieldWorks.SharpViews.SharpView();
			this.SuspendLayout();
			//
			// whichView
			//
			this.whichView.FormattingEnabled = true;
			this.whichView.Items.AddRange(new object[] {
			"Red Box",
			"Several boxes",
			"Simple Text Para",
			"Echo Para",
			"MultiPara",
			"Styled text",
			"Long text"});
			this.whichView.Location = new System.Drawing.Point(80, 12);
			this.whichView.Name = "whichView";
			this.whichView.Size = new System.Drawing.Size(121, 21);
			this.whichView.TabIndex = 1;
			this.whichView.SelectedIndexChanged += new System.EventHandler(this.whichView_SelectedIndexChanged);
			//
			// theSharpView
			//
			this.theSharpView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.theSharpView.Location = new System.Drawing.Point(0, 53);
			this.theSharpView.Name = "theSharpView";
			this.theSharpView.Root = null;
			this.theSharpView.Size = new System.Drawing.Size(277, 209);
			this.theSharpView.TabIndex = 0;
			//
			// Form1
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 264);
			this.Controls.Add(this.whichView);
			this.Controls.Add(this.theSharpView);
			this.Name = "Form1";
			this.Text = "Form1";
			this.ResumeLayout(false);

		}

		#endregion

		private SIL.FieldWorks.SharpViews.SharpView theSharpView;
		private System.Windows.Forms.ComboBox whichView;
	}
}
