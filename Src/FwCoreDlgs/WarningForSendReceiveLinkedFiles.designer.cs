using System.Diagnostics;

namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class WarningForSendReceiveLinkedFiles
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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
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
			this.btn_yes = new System.Windows.Forms.Button();
			this.btn_no = new System.Windows.Forms.Button();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.btn_help = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			//
			// btn_yes
			//
			this.btn_yes.AllowDrop = true;
			this.btn_yes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btn_yes.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.btn_yes.DialogResult = System.Windows.Forms.DialogResult.Yes;
			this.btn_yes.Location = new System.Drawing.Point(33, 157);
			this.btn_yes.Name = "btn_yes";
			this.btn_yes.Size = new System.Drawing.Size(85, 23);
			this.btn_yes.TabIndex = 1;
			this.btn_yes.Text = "Yes";
			this.btn_yes.UseVisualStyleBackColor = true;
			//
			// btn_no
			//
			this.btn_no.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btn_no.DialogResult = System.Windows.Forms.DialogResult.No;
			this.btn_no.Location = new System.Drawing.Point(137, 157);
			this.btn_no.Name = "btn_no";
			this.btn_no.Size = new System.Drawing.Size(75, 23);
			this.btn_no.TabIndex = 2;
			this.btn_no.Text = "No";
			this.btn_no.UseVisualStyleBackColor = true;
			//
			// pictureBox1
			//
			this.pictureBox1.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.Warning;
			this.pictureBox1.Location = new System.Drawing.Point(12, 1);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(38, 39);
			this.pictureBox1.TabIndex = 5;
			this.pictureBox1.TabStop = false;
			//
			// label2
			//
			this.label2.Location = new System.Drawing.Point(9, 43);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(317, 54);
			this.label2.TabIndex = 6;
			this.label2.Text = "The Linked Files location for this project is not set to the default. This means " +
	"that the linked files in this project will not be shared by Send/Receive.";
			//
			// label1
			//
			this.label1.Location = new System.Drawing.Point(9, 108);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(317, 39);
			this.label1.TabIndex = 7;
			this.label1.Text = "Would you like to set the Linked Files folder back to the default location in ord" +
	"er to enable sharing via Send/Receive? ";
			//
			// btn_help
			//
			this.btn_help.Location = new System.Drawing.Point(231, 157);
			this.btn_help.Name = "btn_help";
			this.btn_help.Size = new System.Drawing.Size(75, 23);
			this.btn_help.TabIndex = 8;
			this.btn_help.Text = "Help";
			this.btn_help.UseVisualStyleBackColor = true;
			this.btn_help.Click += new System.EventHandler(this.btn_help_Click);
			//
			// WarningForSendReceiveLinkedFiles
			//
			this.AcceptButton = this.btn_yes;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btn_no;
			this.ClientSize = new System.Drawing.Size(334, 203);
			this.ControlBox = false;
			this.Controls.Add(this.btn_help);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.btn_no);
			this.Controls.Add(this.btn_yes);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "WarningForSendReceiveLinkedFiles";
			this.Text = "Custom Linked Files folder not supported by Send/Receive";
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btn_yes;
		private System.Windows.Forms.Button btn_no;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button btn_help;
	}
}
