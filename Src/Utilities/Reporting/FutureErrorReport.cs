using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

namespace SIL.Utils
{
	/// <summary>
	/// Summary description for FutureErrorReport.
	/// </summary>
	public class FutureErrorReport : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox m_comments;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.RadioButton radYes;
		private System.Windows.Forms.TextBox m_details;
		private System.Windows.Forms.RadioButton radSelf;
		private System.Windows.Forms.Button btnContinue;
		private System.Windows.Forms.Button btnExit;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox m_fromEmail;
		private System.Windows.Forms.TextBox m_notification;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public FutureErrorReport()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.label2 = new System.Windows.Forms.Label();
			this.m_comments = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.radYes = new System.Windows.Forms.RadioButton();
			this.m_details = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.radSelf = new System.Windows.Forms.RadioButton();
			this.btnContinue = new System.Windows.Forms.Button();
			this.btnExit = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.m_fromEmail = new System.Windows.Forms.TextBox();
			this.m_notification = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			//
			// label2
			//
			this.label2.Location = new System.Drawing.Point(16, 296);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(320, 23);
			this.label2.TabIndex = 19;
			this.label2.Text = "Enter any comments about what made this problem appear.";
			//
			// m_comments
			//
			this.m_comments.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.m_comments.Location = new System.Drawing.Point(16, 320);
			this.m_comments.Multiline = true;
			this.m_comments.Name = "m_comments";
			this.m_comments.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.m_comments.Size = new System.Drawing.Size(496, 88);
			this.m_comments.TabIndex = 1;
			this.m_comments.Text = "comments";
			//
			// label3
			//
			this.label3.Location = new System.Drawing.Point(16, 88);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(216, 16);
			this.label3.TabIndex = 17;
			this.label3.Text = "Details:";
			//
			// radYes
			//
			this.radYes.Location = new System.Drawing.Point(64, 448);
			this.radYes.Name = "radYes";
			this.radYes.Size = new System.Drawing.Size(392, 16);
			this.radYes.TabIndex = 2;
			this.radYes.Text = "&Send this the next time I\'m connected to the Internet.";
			//
			// m_details
			//
			this.m_details.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.m_details.BackColor = System.Drawing.SystemColors.InactiveCaptionText;
			this.m_details.Enabled = false;
			this.m_details.Location = new System.Drawing.Point(16, 112);
			this.m_details.Multiline = true;
			this.m_details.Name = "m_details";
			this.m_details.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.m_details.Size = new System.Drawing.Size(496, 104);
			this.m_details.TabIndex = 0;
			this.m_details.Text = "details";
			//
			// label1
			//
			this.label1.Location = new System.Drawing.Point(16, 424);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(424, 23);
			this.label1.TabIndex = 9;
			this.label1.Text = "May we have permission to email this report to the development team?";
			//
			// radSelf
			//
			this.radSelf.Location = new System.Drawing.Point(64, 472);
			this.radSelf.Name = "radSelf";
			this.radSelf.Size = new System.Drawing.Size(456, 16);
			this.radSelf.TabIndex = 3;
			this.radSelf.Text = "&Copy this message to the clipboard and I will paste it into an e-mail message my" +
				"self.";
			//
			// btnContinue
			//
			this.btnContinue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnContinue.Location = new System.Drawing.Point(448, 520);
			this.btnContinue.Name = "btnContinue";
			this.btnContinue.TabIndex = 0;
			this.btnContinue.Text = "&Continue";
			//
			// btnExit
			//
			this.btnExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnExit.Location = new System.Drawing.Point(320, 520);
			this.btnExit.Name = "btnExit";
			this.btnExit.Size = new System.Drawing.Size(112, 23);
			this.btnExit.TabIndex = 5;
			this.btnExit.Text = "E&xit the application";
			//
			// label4
			//
			this.label4.Location = new System.Drawing.Point(16, 248);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(112, 16);
			this.label4.TabIndex = 20;
			this.label4.Text = "Your email address:";
			//
			// m_fromEmail
			//
			this.m_fromEmail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.m_fromEmail.Location = new System.Drawing.Point(120, 248);
			this.m_fromEmail.Name = "m_fromEmail";
			this.m_fromEmail.Size = new System.Drawing.Size(392, 20);
			this.m_fromEmail.TabIndex = 21;
			this.m_fromEmail.Text = "textBox2";
			//
			// m_notification
			//
			this.m_notification.BackColor = System.Drawing.Color.Azure;
			this.m_notification.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_notification.Enabled = false;
			this.m_notification.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.m_notification.Location = new System.Drawing.Point(16, 16);
			this.m_notification.Multiline = true;
			this.m_notification.Name = "m_notification";
			this.m_notification.Size = new System.Drawing.Size(488, 56);
			this.m_notification.TabIndex = 22;
			this.m_notification.Text = "notification msg";
			//
			// FutureErrorReport
			//
			this.AcceptButton = this.btnContinue;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.BackColor = System.Drawing.Color.Azure;
			this.ClientSize = new System.Drawing.Size(552, 558);
			this.ControlBox = false;
			this.Controls.Add(this.m_notification);
			this.Controls.Add(this.m_fromEmail);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.btnExit);
			this.Controls.Add(this.btnContinue);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.m_comments);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.radYes);
			this.Controls.Add(this.m_details);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.radSelf);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FutureErrorReport";
			this.Text = "An error has occurred";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
