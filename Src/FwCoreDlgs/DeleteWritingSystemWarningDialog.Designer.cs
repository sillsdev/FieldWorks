namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class DeleteWritingSystemWarningDialog
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DeleteWritingSystemWarningDialog));
			this.cancelButton = new System.Windows.Forms.Button();
			this.deleteButton = new System.Windows.Forms.Button();
			this.warningIconBox = new System.Windows.Forms.PictureBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.mainMessage = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.warningIconBox)).BeginInit();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			//
			// cancelButton
			//
			resources.ApplyResources(this.cancelButton, "cancelButton");
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.UseVisualStyleBackColor = true;
			//
			// deleteButton
			//
			resources.ApplyResources(this.deleteButton, "deleteButton");
			this.deleteButton.DialogResult = System.Windows.Forms.DialogResult.Yes;
			this.deleteButton.Name = "deleteButton";
			this.deleteButton.UseVisualStyleBackColor = true;
			//
			// warningIconBox
			//
			resources.ApplyResources(this.warningIconBox, "warningIconBox");
			this.warningIconBox.Name = "warningIconBox";
			this.warningIconBox.TabStop = false;
			//
			// panel1
			//
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.BackColor = System.Drawing.SystemColors.Control;
			this.panel1.Controls.Add(this.mainMessage);
			this.panel1.Controls.Add(this.warningIconBox);
			this.panel1.Name = "panel1";
			//
			// mainMessage
			//
			resources.ApplyResources(this.mainMessage, "mainMessage");
			this.mainMessage.Name = "mainMessage";
			//
			// DeleteWritingSystemWarningDialog
			//
			this.AcceptButton = this.deleteButton;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.ControlBox = false;
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.deleteButton);
			this.Controls.Add(this.cancelButton);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DeleteWritingSystemWarningDialog";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			((System.ComponentModel.ISupportInitialize)(this.warningIconBox)).EndInit();
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button deleteButton;
		private System.Windows.Forms.PictureBox warningIconBox;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label mainMessage;
	}
}
