namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class WizardStep
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WizardStep));
			this._optionalIndicator = new System.Windows.Forms.Label();
			this._stepName = new System.Windows.Forms.Label();
			this._statusImage = new System.Windows.Forms.PictureBox();
			this._lineToNextImage = new System.Windows.Forms.PictureBox();
			this._lineToPreviousImage = new System.Windows.Forms.PictureBox();
			this._stepLayout = new System.Windows.Forms.TableLayoutPanel();
			((System.ComponentModel.ISupportInitialize)(this._statusImage)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._lineToNextImage)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._lineToPreviousImage)).BeginInit();
			this._stepLayout.SuspendLayout();
			this.SuspendLayout();
			// 
			// _optionalIndicator
			// 
			this._optionalIndicator.AutoSize = true;
			this._stepLayout.SetColumnSpan(this._optionalIndicator, 3);
			this._optionalIndicator.Dock = System.Windows.Forms.DockStyle.Fill;
			this._optionalIndicator.Location = new System.Drawing.Point(3, 66);
			this._optionalIndicator.Name = "_optionalIndicator";
			this._optionalIndicator.Size = new System.Drawing.Size(118, 16);
			this._optionalIndicator.TabIndex = 0;
			this._optionalIndicator.Text = "(Optional)";
			this._optionalIndicator.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// _stepName
			// 
			this._stepName.AutoSize = true;
			this._stepLayout.SetColumnSpan(this._stepName, 3);
			this._stepName.Dock = System.Windows.Forms.DockStyle.Fill;
			this._stepName.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this._stepName.Location = new System.Drawing.Point(3, 28);
			this._stepName.Name = "_stepName";
			this._stepName.Size = new System.Drawing.Size(118, 38);
			this._stepName.TabIndex = 1;
			this._stepName.Text = "Step Name";
			this._stepName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// _statusImage
			// 
			this._statusImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._statusImage.InitialImage = ((System.Drawing.Image)(resources.GetObject("_statusImage.InitialImage")));
			this._statusImage.Location = new System.Drawing.Point(50, 4);
			this._statusImage.Margin = new System.Windows.Forms.Padding(0);
			this._statusImage.Name = "_statusImage";
			this._statusImage.Size = new System.Drawing.Size(24, 24);
			this._statusImage.TabIndex = 2;
			this._statusImage.TabStop = false;
			// 
			// _lineToNextImage
			// 
			this._lineToNextImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._lineToNextImage.Location = new System.Drawing.Point(74, 4);
			this._lineToNextImage.Margin = new System.Windows.Forms.Padding(0);
			this._lineToNextImage.Name = "_lineToNextImage";
			this._lineToNextImage.Size = new System.Drawing.Size(50, 24);
			this._lineToNextImage.TabIndex = 3;
			this._lineToNextImage.TabStop = false;
			// 
			// _lineToPreviousImage
			// 
			this._lineToPreviousImage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this._lineToPreviousImage.Location = new System.Drawing.Point(0, 4);
			this._lineToPreviousImage.Margin = new System.Windows.Forms.Padding(0);
			this._lineToPreviousImage.Name = "_lineToPreviousImage";
			this._lineToPreviousImage.Size = new System.Drawing.Size(50, 24);
			this._lineToPreviousImage.TabIndex = 4;
			this._lineToPreviousImage.TabStop = false;
			// 
			// _stepLayout
			// 
			this._stepLayout.ColumnCount = 3;
			this._stepLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
			this._stepLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 24F));
			this._stepLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
			this._stepLayout.Controls.Add(this._lineToPreviousImage, 0, 0);
			this._stepLayout.Controls.Add(this._optionalIndicator, 0, 2);
			this._stepLayout.Controls.Add(this._stepName, 0, 1);
			this._stepLayout.Controls.Add(this._lineToNextImage, 2, 0);
			this._stepLayout.Controls.Add(this._statusImage, 1, 0);
			this._stepLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this._stepLayout.Location = new System.Drawing.Point(0, 0);
			this._stepLayout.Margin = new System.Windows.Forms.Padding(0);
			this._stepLayout.Name = "_stepLayout";
			this._stepLayout.RowCount = 3;
			this._stepLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
			this._stepLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
			this._stepLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 16F));
			this._stepLayout.Size = new System.Drawing.Size(124, 80);
			this._stepLayout.TabIndex = 5;
			// 
			// WizardStep
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._stepLayout);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.Name = "WizardStep";
			this.Size = new System.Drawing.Size(124, 80);
			((System.ComponentModel.ISupportInitialize)(this._statusImage)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this._lineToNextImage)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this._lineToPreviousImage)).EndInit();
			this._stepLayout.ResumeLayout(false);
			this._stepLayout.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label _optionalIndicator;
		private System.Windows.Forms.TableLayoutPanel _stepLayout;
		private System.Windows.Forms.PictureBox _lineToPreviousImage;
		private System.Windows.Forms.Label _stepName;
		private System.Windows.Forms.PictureBox _lineToNextImage;
		private System.Windows.Forms.PictureBox _statusImage;
	}
}
