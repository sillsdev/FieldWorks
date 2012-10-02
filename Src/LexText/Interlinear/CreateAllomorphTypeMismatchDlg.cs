using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.Utils;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Summary description for CreateAllomorphTypeMismatchDlg.
	/// </summary>
	public class CreateAllomorphTypeMismatchDlg : Form, IFWDisposable
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button m_btnYes;
		private System.Windows.Forms.Button m_btnNo;
		private System.Windows.Forms.Button m_btnCreateNew;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Label m_lblMessage1_Warning;
		private System.Windows.Forms.Label m_lblMessage2_Question;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>
		/// Set the text of the top message.
		/// </summary>
		public string Warning
		{
			set
			{
				CheckDisposed();
				m_lblMessage1_Warning.Text = value;
			}
		}

		/// <summary>
		/// Set the text of the bottom message.
		/// </summary>
		public string Question
		{
			set
			{
				CheckDisposed();
				m_lblMessage2_Question.Text = value;
			}
		}

		public CreateAllomorphTypeMismatchDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;
			this.pictureBox1.Image = System.Drawing.SystemIcons.Warning.ToBitmap();
			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateAllomorphTypeMismatchDlg));
			this.label1 = new System.Windows.Forms.Label();
			this.m_lblMessage1_Warning = new System.Windows.Forms.Label();
			this.m_lblMessage2_Question = new System.Windows.Forms.Label();
			this.m_btnYes = new System.Windows.Forms.Button();
			this.m_btnNo = new System.Windows.Forms.Button();
			this.m_btnCreateNew = new System.Windows.Forms.Button();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// m_lblMessage1_Warning
			//
			resources.ApplyResources(this.m_lblMessage1_Warning, "m_lblMessage1_Warning");
			this.m_lblMessage1_Warning.Name = "m_lblMessage1_Warning";
			//
			// m_lblMessage2_Question
			//
			resources.ApplyResources(this.m_lblMessage2_Question, "m_lblMessage2_Question");
			this.m_lblMessage2_Question.Name = "m_lblMessage2_Question";
			//
			// m_btnYes
			//
			resources.ApplyResources(this.m_btnYes, "m_btnYes");
			this.m_btnYes.DialogResult = System.Windows.Forms.DialogResult.Yes;
			this.m_btnYes.Name = "m_btnYes";
			//
			// m_btnNo
			//
			resources.ApplyResources(this.m_btnNo, "m_btnNo");
			this.m_btnNo.DialogResult = System.Windows.Forms.DialogResult.No;
			this.m_btnNo.Name = "m_btnNo";
			//
			// m_btnCreateNew
			//
			resources.ApplyResources(this.m_btnCreateNew, "m_btnCreateNew");
			this.m_btnCreateNew.DialogResult = System.Windows.Forms.DialogResult.Retry;
			this.m_btnCreateNew.Name = "m_btnCreateNew";
			//
			// pictureBox1
			//
			resources.ApplyResources(this.pictureBox1, "pictureBox1");
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.TabStop = false;
			//
			// CreateAllomorphTypeMismatchDlg
			//
			this.AcceptButton = this.m_btnYes;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnNo;
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.m_btnCreateNew);
			this.Controls.Add(this.m_btnNo);
			this.Controls.Add(this.m_btnYes);
			this.Controls.Add(this.m_lblMessage2_Question);
			this.Controls.Add(this.m_lblMessage1_Warning);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "CreateAllomorphTypeMismatchDlg";
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
