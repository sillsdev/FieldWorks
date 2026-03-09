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
			this.btnCreate.Location = new System.Drawing.Point(45, 131);
			this.btnCreate.Name = "btnCreate";
			this.btnCreate.Size = new System.Drawing.Size(355, 58);
			this.btnCreate.TabIndex = 0;
			this.btnCreate.Text = "Create a new operations file";
			this.btnCreate.UseVisualStyleBackColor = true;
			// 
			// btnOpen
			// 
			this.btnOpen.DialogResult = System.Windows.Forms.DialogResult.Yes;
			this.btnOpen.Location = new System.Drawing.Point(45, 218);
			this.btnOpen.Name = "btnOpen";
			this.btnOpen.Size = new System.Drawing.Size(355, 58);
			this.btnOpen.TabIndex = 1;
			this.btnOpen.Text = "Open existing operations file";
			this.btnOpen.UseVisualStyleBackColor = true;
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(45, 305);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(355, 58);
			this.btnCancel.TabIndex = 2;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// lbPrompt
			// 
			this.lbPrompt.AutoSize = true;
			this.lbPrompt.Location = new System.Drawing.Point(45, 36);
			this.lbPrompt.Name = "lbPrompt";
			this.lbPrompt.Size = new System.Drawing.Size(361, 20);
			this.lbPrompt.TabIndex = 3;
			this.lbPrompt.Text = "Apparently this utility tool has not been run before.";
			// 
			// lbChoose
			// 
			this.lbChoose.AutoSize = true;
			this.lbChoose.Location = new System.Drawing.Point(45, 84);
			this.lbChoose.Name = "lbChoose";
			this.lbChoose.Size = new System.Drawing.Size(138, 20);
			this.lbChoose.TabIndex = 4;
			this.lbChoose.Text = "Choose an option:";
			// 
			// CreateNewOpenCancelDialog
			// 
			this.AcceptButton = this.btnCreate;
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(471, 395);
			this.Controls.Add(this.lbChoose);
			this.Controls.Add(this.lbPrompt);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOpen);
			this.Controls.Add(this.btnCreate);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "CreateNewOpenCancelDialog";
			this.Text = "Allomorph Generator";
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