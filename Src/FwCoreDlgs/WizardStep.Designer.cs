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
			resources.ApplyResources(this._optionalIndicator, "_optionalIndicator");
			this._stepLayout.SetColumnSpan(this._optionalIndicator, 3);
			this._optionalIndicator.Name = "_optionalIndicator";
			// 
			// _stepName
			// 
			resources.ApplyResources(this._stepName, "_stepName");
			this._stepLayout.SetColumnSpan(this._stepName, 3);
			this._stepName.Name = "_stepName";
			// 
			// _statusImage
			// 
			resources.ApplyResources(this._statusImage, "_statusImage");
			this._statusImage.Name = "_statusImage";
			this._statusImage.TabStop = false;
			// 
			// _lineToNextImage
			// 
			resources.ApplyResources(this._lineToNextImage, "_lineToNextImage");
			this._lineToNextImage.Name = "_lineToNextImage";
			this._lineToNextImage.TabStop = false;
			// 
			// _lineToPreviousImage
			// 
			resources.ApplyResources(this._lineToPreviousImage, "_lineToPreviousImage");
			this._lineToPreviousImage.Name = "_lineToPreviousImage";
			this._lineToPreviousImage.TabStop = false;
			// 
			// _stepLayout
			// 
			resources.ApplyResources(this._stepLayout, "_stepLayout");
			this._stepLayout.Controls.Add(this._lineToPreviousImage, 0, 0);
			this._stepLayout.Controls.Add(this._optionalIndicator, 0, 2);
			this._stepLayout.Controls.Add(this._stepName, 0, 1);
			this._stepLayout.Controls.Add(this._lineToNextImage, 2, 0);
			this._stepLayout.Controls.Add(this._statusImage, 1, 0);
			this._stepLayout.Name = "_stepLayout";
			// 
			// WizardStep
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this._stepLayout);
			this.Name = "WizardStep";
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
