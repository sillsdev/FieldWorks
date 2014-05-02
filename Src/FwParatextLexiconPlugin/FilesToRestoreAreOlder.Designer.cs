namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	partial class FilesToRestoreAreOlder
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FilesToRestoreAreOlder));
			this.label_message = new System.Windows.Forms.Label();
			this.label_Question = new System.Windows.Forms.Label();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.radio_Overwrite = new System.Windows.Forms.RadioButton();
			this.radio_Keep = new System.Windows.Forms.RadioButton();
			this.button_Cancel = new System.Windows.Forms.Button();
			this.button_OK = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// label_message
			// 
			resources.ApplyResources(this.label_message, "label_message");
			this.label_message.Name = "label_message";
			// 
			// label_Question
			// 
			resources.ApplyResources(this.label_Question, "label_Question");
			this.label_Question.Name = "label_Question";
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = global::SIL.FieldWorks.ParatextLexiconPlugin.Properties.Resources.question;
			resources.ApplyResources(this.pictureBox1, "pictureBox1");
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.TabStop = false;
			// 
			// radio_Overwrite
			// 
			resources.ApplyResources(this.radio_Overwrite, "radio_Overwrite");
			this.radio_Overwrite.Name = "radio_Overwrite";
			this.radio_Overwrite.TabStop = true;
			this.radio_Overwrite.UseVisualStyleBackColor = true;
			// 
			// radio_Keep
			// 
			resources.ApplyResources(this.radio_Keep, "radio_Keep");
			this.radio_Keep.Checked = true;
			this.radio_Keep.Name = "radio_Keep";
			this.radio_Keep.TabStop = true;
			this.radio_Keep.UseVisualStyleBackColor = true;
			// 
			// button_Cancel
			// 
			this.button_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.button_Cancel, "button_Cancel");
			this.button_Cancel.Name = "button_Cancel";
			this.button_Cancel.UseVisualStyleBackColor = true;
			this.button_Cancel.Click += new System.EventHandler(this.button_Cancel_Click);
			// 
			// button_OK
			// 
			resources.ApplyResources(this.button_OK, "button_OK");
			this.button_OK.Name = "button_OK";
			this.button_OK.UseVisualStyleBackColor = true;
			this.button_OK.Click += new System.EventHandler(this.button_OK_Click);
			// 
			// FilesToRestoreAreOlder
			// 
			this.AcceptButton = this.button_OK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.button_Cancel;
			this.ControlBox = false;
			this.Controls.Add(this.radio_Overwrite);
			this.Controls.Add(this.radio_Keep);
			this.Controls.Add(this.button_Cancel);
			this.Controls.Add(this.button_OK);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.label_Question);
			this.Controls.Add(this.label_message);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FilesToRestoreAreOlder";
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label_message;
		private System.Windows.Forms.Label label_Question;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.RadioButton radio_Overwrite;
		private System.Windows.Forms.RadioButton radio_Keep;
		private System.Windows.Forms.Button button_Cancel;
		private System.Windows.Forms.Button button_OK;
	}
}