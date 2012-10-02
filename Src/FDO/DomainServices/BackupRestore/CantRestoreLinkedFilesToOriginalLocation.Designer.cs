using System.Windows.Forms;

namespace SIL.FieldWorks.FDO.DomainServices.BackupRestore
{
	partial class CantRestoreLinkedFilesToOriginalLocation
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CantRestoreLinkedFilesToOriginalLocation));
			this.button_OK = new System.Windows.Forms.Button();
			this.button_Cancel = new System.Windows.Forms.Button();
			this.button_Help = new System.Windows.Forms.Button();
			this.label_message = new System.Windows.Forms.Label();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.radio_NoThanks = new System.Windows.Forms.RadioButton();
			this.radio_Thanks = new System.Windows.Forms.RadioButton();
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
			// label_message
			//
			resources.ApplyResources(this.label_message, "label_message");
			this.label_message.Name = "label_message";
			//
			// pictureBox1
			//
			this.pictureBox1.Image = global::SIL.FieldWorks.FDO.Properties.Resources.question;
			resources.ApplyResources(this.pictureBox1, "pictureBox1");
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.TabStop = false;
			//
			// radio_NoThanks
			//
			resources.ApplyResources(this.radio_NoThanks, "radio_NoThanks");
			this.radio_NoThanks.Name = "radio_NoThanks";
			this.radio_NoThanks.TabStop = true;
			this.radio_NoThanks.UseVisualStyleBackColor = true;
			//
			// radio_Thanks
			//
			resources.ApplyResources(this.radio_Thanks, "radio_Thanks");
			this.radio_Thanks.Checked = true;
			this.radio_Thanks.Name = "radio_Thanks";
			this.radio_Thanks.TabStop = true;
			this.radio_Thanks.UseVisualStyleBackColor = true;
			//
			// CantRestoreLinkedFilesToOriginalLocation
			//
			this.AcceptButton = this.button_OK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.button_Cancel;
			this.Controls.Add(this.radio_Thanks);
			this.Controls.Add(this.radio_NoThanks);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.label_message);
			this.Controls.Add(this.button_Help);
			this.Controls.Add(this.button_Cancel);
			this.Controls.Add(this.button_OK);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "CantRestoreLinkedFilesToOriginalLocation";
			this.ShowIcon = false;
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button button_OK;
		private System.Windows.Forms.Button button_Cancel;
		private System.Windows.Forms.Button button_Help;
		private System.Windows.Forms.Label label_message;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.RadioButton radio_NoThanks;
		private RadioButton radio_Thanks;
	}
}