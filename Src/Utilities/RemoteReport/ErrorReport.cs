using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

namespace SIL.Utils
{
	/// <summary>
	/// Summary description for ErrorReport.
	/// </summary>
	public class ErrorReport : System.Windows.Forms.Form
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

		public ErrorReport()
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ErrorReport));
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
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// m_comments
			//
			resources.ApplyResources(this.m_comments, "m_comments");
			this.m_comments.Name = "m_comments";
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// radYes
			//
			resources.ApplyResources(this.radYes, "radYes");
			this.radYes.Name = "radYes";
			//
			// m_details
			//
			resources.ApplyResources(this.m_details, "m_details");
			this.m_details.BackColor = System.Drawing.SystemColors.InactiveCaptionText;
			this.m_details.Name = "m_details";
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// radSelf
			//
			resources.ApplyResources(this.radSelf, "radSelf");
			this.radSelf.Name = "radSelf";
			//
			// btnContinue
			//
			resources.ApplyResources(this.btnContinue, "btnContinue");
			this.btnContinue.Name = "btnContinue";
			//
			// btnExit
			//
			resources.ApplyResources(this.btnExit, "btnExit");
			this.btnExit.Name = "btnExit";
			//
			// label4
			//
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			//
			// m_fromEmail
			//
			resources.ApplyResources(this.m_fromEmail, "m_fromEmail");
			this.m_fromEmail.Name = "m_fromEmail";
			//
			// m_notification
			//
			this.m_notification.BackColor = System.Drawing.Color.Azure;
			this.m_notification.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.m_notification, "m_notification");
			this.m_notification.Name = "m_notification";
			//
			// ErrorReport
			//
			this.AcceptButton = this.btnContinue;
			resources.ApplyResources(this, "$this");
			this.BackColor = System.Drawing.Color.Azure;
			this.CancelButton = this.btnContinue;
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
			this.Name = "ErrorReport";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
