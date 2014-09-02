namespace SIL.FieldWorks.FdoUi.Dialogs
{
	partial class RestoreLinkedFilesToProjectsFolder
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RestoreLinkedFilesToProjectsFolder));
			this.button_OK = new System.Windows.Forms.Button();
			this.button_Cancel = new System.Windows.Forms.Button();
			this.button_Help = new System.Windows.Forms.Button();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.labelMessage = new System.Windows.Forms.Label();
			this.labelQuestion = new System.Windows.Forms.Label();
			this.radio_No = new System.Windows.Forms.RadioButton();
			this.radio_Yes = new System.Windows.Forms.RadioButton();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			//
			// button_OK
			//
			resources.ApplyResources(this.button_OK, "button_OK");
			this.button_OK.Name = "button_OK";
			this.button_OK.UseVisualStyleBackColor = true;
			this.button_OK.Click += new System.EventHandler(this.button_OK_Click);
			//
			// button_Cancel
			//
			this.button_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.button_Cancel, "button_Cancel");
			this.button_Cancel.Name = "button_Cancel";
			this.button_Cancel.UseVisualStyleBackColor = true;
			this.button_Cancel.Click += new System.EventHandler(this.button_Cancel_Click);
			//
			// button_Help
			//
			resources.ApplyResources(this.button_Help, "button_Help");
			this.button_Help.Name = "button_Help";
			this.button_Help.UseVisualStyleBackColor = true;
			this.button_Help.Click += new System.EventHandler(this.button_Help_Click);
			//
			// pictureBox1
			//
			this.pictureBox1.Image = global::SIL.FieldWorks.FdoUi.Properties.Resources.question;
			resources.ApplyResources(this.pictureBox1, "pictureBox1");
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.TabStop = false;
			//
			// labelMessage
			//
			resources.ApplyResources(this.labelMessage, "labelMessage");
			this.labelMessage.Name = "labelMessage";
			//
			// labelQuestion
			//
			resources.ApplyResources(this.labelQuestion, "labelQuestion");
			this.labelQuestion.Name = "labelQuestion";
			//
			// radio_No
			//
			resources.ApplyResources(this.radio_No, "radio_No");
			this.radio_No.Name = "radio_No";
			this.radio_No.TabStop = true;
			this.radio_No.UseVisualStyleBackColor = true;
			//
			// radio_Yes
			//
			resources.ApplyResources(this.radio_Yes, "radio_Yes");
			this.radio_Yes.Checked = true;
			this.radio_Yes.Name = "radio_Yes";
			this.radio_Yes.TabStop = true;
			this.radio_Yes.UseVisualStyleBackColor = true;
			//
			// RestoreLinkedFilesToProjectsFolder
			//
			this.AcceptButton = this.button_OK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.button_Cancel;
			this.Controls.Add(this.radio_No);
			this.Controls.Add(this.radio_Yes);
			this.Controls.Add(this.labelQuestion);
			this.Controls.Add(this.labelMessage);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.button_Help);
			this.Controls.Add(this.button_Cancel);
			this.Controls.Add(this.button_OK);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "RestoreLinkedFilesToProjectsFolder";
			this.ShowIcon = false;
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button button_OK;
		private System.Windows.Forms.Button button_Cancel;
		private System.Windows.Forms.Button button_Help;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label labelMessage;
		private System.Windows.Forms.Label labelQuestion;
		private System.Windows.Forms.RadioButton radio_No;
		private System.Windows.Forms.RadioButton radio_Yes;
	}
}