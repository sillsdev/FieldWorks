namespace SilEncConverters40
{
	partial class TargetFormDisplayControl
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

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.flowLayoutPanelTargetWords = new System.Windows.Forms.FlowLayoutPanel();
			this.buttonAdd = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// flowLayoutPanelTargetWords
			//
			this.flowLayoutPanelTargetWords.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.flowLayoutPanelTargetWords.Location = new System.Drawing.Point(4, 3);
			this.flowLayoutPanelTargetWords.Name = "flowLayoutPanelTargetWords";
			this.flowLayoutPanelTargetWords.Size = new System.Drawing.Size(321, 342);
			this.flowLayoutPanelTargetWords.TabIndex = 1;
			//
			// buttonAdd
			//
			this.buttonAdd.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonAdd.Enabled = false;
			this.buttonAdd.Location = new System.Drawing.Point(193, 351);
			this.buttonAdd.Name = "buttonAdd";
			this.buttonAdd.Size = new System.Drawing.Size(132, 23);
			this.buttonAdd.TabIndex = 3;
			this.buttonAdd.Text = "&Add new translation";
			this.buttonAdd.UseVisualStyleBackColor = true;
			this.buttonAdd.Click += new System.EventHandler(this.buttonAdd_Click);
			//
			// TargetFormDisplayControl
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.buttonAdd);
			this.Controls.Add(this.flowLayoutPanelTargetWords);
			this.Name = "TargetFormDisplayControl";
			this.Size = new System.Drawing.Size(328, 381);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelTargetWords;
		private System.Windows.Forms.Button buttonAdd;
	}
}
