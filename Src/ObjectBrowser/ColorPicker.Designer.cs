namespace SIL.ObjectBrowser
{
	partial class ColorPicker
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

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.tsColors = new System.Windows.Forms.ToolStrip();
			this.SuspendLayout();
			//
			// tsColors
			//
			this.tsColors.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tsColors.AutoSize = false;
			this.tsColors.CanOverflow = false;
			this.tsColors.Dock = System.Windows.Forms.DockStyle.None;
			this.tsColors.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.tsColors.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
			this.tsColors.Location = new System.Drawing.Point(0, 0);
			this.tsColors.Name = "tsColors";
			this.tsColors.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.tsColors.Size = new System.Drawing.Size(121, 133);
			this.tsColors.TabIndex = 0;
			//
			// ColorPicker
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.tsColors);
			this.Name = "ColorPicker";
			this.Size = new System.Drawing.Size(121, 164);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ToolStrip tsColors;
	}
}
