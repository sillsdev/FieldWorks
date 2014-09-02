namespace SIL.FieldWorks.FdoUi.Dialogs
{
	partial class ConflictingSaveDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConflictingSaveDlg));
			this.m_okButton = new System.Windows.Forms.Button();
			this.m_refreshButton = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			//
			// m_okButton
			//
			this.m_okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_okButton.Location = new System.Drawing.Point(400, 165);
			this.m_okButton.Name = "m_okButton";
			this.m_okButton.Size = new System.Drawing.Size(75, 23);
			this.m_okButton.TabIndex = 0;
			this.m_okButton.Text = "OK";
			this.m_okButton.UseVisualStyleBackColor = true;
			//
			// m_refreshButton
			//
			this.m_refreshButton.DialogResult = System.Windows.Forms.DialogResult.Yes;
			this.m_refreshButton.Location = new System.Drawing.Point(265, 165);
			this.m_refreshButton.Name = "m_refreshButton";
			this.m_refreshButton.Size = new System.Drawing.Size(93, 23);
			this.m_refreshButton.TabIndex = 1;
			this.m_refreshButton.Text = "Refresh Now";
			this.m_refreshButton.UseVisualStyleBackColor = true;
			//
			// label1
			//
			this.label1.Location = new System.Drawing.Point(79, 33);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(396, 109);
			this.label1.TabIndex = 2;
			this.label1.Text = resources.GetString("label1.Text");
			//
			// pictureBox1
			//
			this.pictureBox1.Location = new System.Drawing.Point(23, 45);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(33, 30);
			this.pictureBox1.TabIndex = 3;
			this.pictureBox1.TabStop = false;
			//
			// ConflictingSaveDlg
			//
			this.AcceptButton = this.m_okButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(494, 206);
			this.ControlBox = false;
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_refreshButton);
			this.Controls.Add(this.m_okButton);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConflictingSaveDlg";
			this.ShowIcon = false;
			this.Text = "Cannot Save";
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button m_okButton;
		private System.Windows.Forms.Button m_refreshButton;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.PictureBox pictureBox1;
	}
}