using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace SIL.PcPatrBrowser
{
	/// <summary>
	/// Summary description for GoToDialog.
	/// </summary>
	public class GoToDialog : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label lblGoToPrompt;
		private System.Windows.Forms.TextBox tbNumber;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnGo;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public GoToDialog()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Get/set the Go-To prompt
		/// </summary>
		public string GoToPrompt
		{
			get { return lblGoToPrompt.Text; }
			set
			{
				if (value != null)
					lblGoToPrompt.Text = value;
			}
		}

		/// <summary>
		/// Get the number
		/// </summary>
		public int Number
		{
			get
			{
				try
				{
					return Convert.ToInt32(tbNumber.Text);
				}
				catch
				{
					return 1;
				}
			}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(
				typeof(GoToDialog)
			);
			this.lblGoToPrompt = new System.Windows.Forms.Label();
			this.tbNumber = new System.Windows.Forms.TextBox();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnGo = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// lblGoToPrompt
			//
			this.lblGoToPrompt.Location = new System.Drawing.Point(16, 16);
			this.lblGoToPrompt.Name = "lblGoToPrompt";
			this.lblGoToPrompt.Size = new System.Drawing.Size(136, 16);
			this.lblGoToPrompt.TabIndex = 0;
			this.lblGoToPrompt.Text = "&Go to Sentence number:";
			//
			// tbNumber
			//
			this.tbNumber.AcceptsReturn = true;
			this.tbNumber.Location = new System.Drawing.Point(160, 16);
			this.tbNumber.Multiline = true;
			this.tbNumber.Name = "tbNumber";
			this.tbNumber.Size = new System.Drawing.Size(64, 20);
			this.tbNumber.TabIndex = 1;
			this.tbNumber.Text = "1";
			this.tbNumber.TextChanged += new System.EventHandler(this.tbNumber_TextChanged);
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(216, 72);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(72, 24);
			this.btnCancel.TabIndex = 2;
			this.btnCancel.Text = "Cancel";
			//
			// btnGo
			//
			this.btnGo.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnGo.Location = new System.Drawing.Point(136, 72);
			this.btnGo.Name = "btnGo";
			this.btnGo.Size = new System.Drawing.Size(72, 24);
			this.btnGo.TabIndex = 3;
			this.btnGo.Text = "Go";
			//
			// dlgGoTo
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(292, 102);
			this.Controls.Add(this.btnGo);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.tbNumber);
			this.Controls.Add(this.lblGoToPrompt);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "dlgGoTo";
			this.Text = "GoToDialog";
			this.ResumeLayout(false);
		}
		#endregion

		private void tbNumber_TextChanged(object sender, System.EventArgs e)
		{
			if (tbNumber.Text.IndexOf("\r\n") > -1)
			{
				tbNumber.Text = tbNumber.Text.Replace("\r\n", "");
				DialogResult = DialogResult.OK;
				Close();
			}
		}
	}
}
