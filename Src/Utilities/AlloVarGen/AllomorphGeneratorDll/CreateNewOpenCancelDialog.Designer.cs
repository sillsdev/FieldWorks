namespace SIL.AllomorphGenerator
{
	partial class CreateNewOpenCancelDialog
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateNewOpenCancelDialog));
			this.btnCreate = new System.Windows.Forms.Button();
			this.btnOpen = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.lbPrompt = new System.Windows.Forms.Label();
			this.lbChoose = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// btnCreate
			// 
			this.btnCreate.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.btnCreate, "btnCreate");
			this.btnCreate.Name = "btnCreate";
			this.btnCreate.UseVisualStyleBackColor = true;
			// 
			// btnOpen
			// 
			this.btnOpen.DialogResult = System.Windows.Forms.DialogResult.Yes;
			resources.ApplyResources(this.btnOpen, "btnOpen");
			this.btnOpen.Name = "btnOpen";
			this.btnOpen.UseVisualStyleBackColor = true;
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// lbPrompt
			// 
			resources.ApplyResources(this.lbPrompt, "lbPrompt");
			this.lbPrompt.Name = "lbPrompt";
			// 
			// lbChoose
			// 
			resources.ApplyResources(this.lbChoose, "lbChoose");
			this.lbChoose.Name = "lbChoose";
			// 
			// CreateNewOpenCancelDialog
			// 
			this.AcceptButton = this.btnCreate;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.lbChoose);
			this.Controls.Add(this.lbPrompt);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOpen);
			this.Controls.Add(this.btnCreate);
			this.Name = "CreateNewOpenCancelDialog";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnCreate;
		private System.Windows.Forms.Button btnOpen;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Label lbPrompt;
		private System.Windows.Forms.Label lbChoose;
	}
}