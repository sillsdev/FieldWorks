using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using Microsoft.Win32;
using SIL.FieldWorks.Common.Utils;

namespace TeInstallerCopier
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	internal class Form1 : System.Windows.Forms.Form
	{
		FolderBrowserDialog m_dlg = new FolderBrowserDialog();

		private System.Windows.Forms.TextBox txtSource;
		private System.Windows.Forms.TextBox txtTarget;
		private System.Windows.Forms.Label lblSource;
		private System.Windows.Forms.Label lblTarget;
		private System.Windows.Forms.Button btnSourceBrowse;
		private System.Windows.Forms.Button btnTargetBrowse;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#region Constructor/Destructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Form1()
		{
			InitializeComponent();
			txtSource.Text = TEInstallerCopier.GetSource;
			txtTarget.Text = TEInstallerCopier.GetTarget;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.txtSource = new System.Windows.Forms.TextBox();
			this.txtTarget = new System.Windows.Forms.TextBox();
			this.lblSource = new System.Windows.Forms.Label();
			this.lblTarget = new System.Windows.Forms.Label();
			this.btnSourceBrowse = new System.Windows.Forms.Button();
			this.btnTargetBrowse = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// txtSource
			//
			this.txtSource.Location = new System.Drawing.Point(64, 20);
			this.txtSource.Name = "txtSource";
			this.txtSource.Size = new System.Drawing.Size(264, 20);
			this.txtSource.TabIndex = 1;
			this.txtSource.Text = "textBox1";
			//
			// txtTarget
			//
			this.txtTarget.Location = new System.Drawing.Point(64, 60);
			this.txtTarget.Name = "txtTarget";
			this.txtTarget.Size = new System.Drawing.Size(264, 20);
			this.txtTarget.TabIndex = 4;
			this.txtTarget.Text = "textBox2";
			//
			// lblSource
			//
			this.lblSource.AutoSize = true;
			this.lblSource.Location = new System.Drawing.Point(16, 24);
			this.lblSource.Name = "lblSource";
			this.lblSource.Size = new System.Drawing.Size(43, 16);
			this.lblSource.TabIndex = 0;
			this.lblSource.Text = "Source:";
			//
			// lblTarget
			//
			this.lblTarget.AutoSize = true;
			this.lblTarget.Location = new System.Drawing.Point(16, 64);
			this.lblTarget.Name = "lblTarget";
			this.lblTarget.Size = new System.Drawing.Size(40, 16);
			this.lblTarget.TabIndex = 3;
			this.lblTarget.Text = "Target:";
			//
			// btnSourceBrowse
			//
			this.btnSourceBrowse.Location = new System.Drawing.Point(336, 17);
			this.btnSourceBrowse.Name = "btnSourceBrowse";
			this.btnSourceBrowse.Size = new System.Drawing.Size(64, 24);
			this.btnSourceBrowse.TabIndex = 2;
			this.btnSourceBrowse.Text = "Browse...";
			this.btnSourceBrowse.Click += new System.EventHandler(this.btnSourceBrowse_Click);
			//
			// btnTargetBrowse
			//
			this.btnTargetBrowse.Location = new System.Drawing.Point(336, 58);
			this.btnTargetBrowse.Name = "btnTargetBrowse";
			this.btnTargetBrowse.Size = new System.Drawing.Size(64, 24);
			this.btnTargetBrowse.TabIndex = 5;
			this.btnTargetBrowse.Text = "Browse...";
			this.btnTargetBrowse.Click += new System.EventHandler(this.btnTargetBrowse_Click);
			//
			// btnOK
			//
			this.btnOK.Location = new System.Drawing.Point(135, 92);
			this.btnOK.Name = "btnOK";
			this.btnOK.TabIndex = 6;
			this.btnOK.Text = "OK";
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(215, 92);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.TabIndex = 7;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			//
			// Form1
			//
			this.AcceptButton = this.btnOK;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(424, 126);
			this.ControlBox = false;
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnTargetBrowse);
			this.Controls.Add(this.btnSourceBrowse);
			this.Controls.Add(this.lblTarget);
			this.Controls.Add(this.lblSource);
			this.Controls.Add(this.txtTarget);
			this.Controls.Add(this.txtSource);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Form1";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Copy TE Installer to FW Build Machine";
			this.ResumeLayout(false);

		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnSourceBrowse_Click(object sender, System.EventArgs e)
		{
			m_dlg.Description = "Specify the Source location.";
			GetPath(txtSource);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnTargetBrowse_Click(object sender, System.EventArgs e)
		{
			m_dlg.Description = "Specify the Target location.";
			GetPath(txtTarget);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="txt"></param>
		/// ------------------------------------------------------------------------------------
		private void GetPath(TextBox txt)
		{
			m_dlg.ShowNewFolderButton = false;

			if (txt.Text.Trim() != string.Empty)
				m_dlg.SelectedPath = txt.Text.Trim();

			if (m_dlg.ShowDialog(this) == DialogResult.OK)
				txt.Text = m_dlg.SelectedPath;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnOK_Click(object sender, System.EventArgs e)
		{
			RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks\TEInstallerCopier");
			key.SetValue("Source", txtSource.Text);

			key = Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks\TEInstallerCopier");
			key.SetValue("Target", txtTarget.Text);

			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnCancel_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length == 1 && args[0] == "/copy")
			{
				TEInstallerCopier copier = new TEInstallerCopier();
				copier.CopyFiles();
			}
			else
				Application.Run(new Form1());
		}
	}

	internal class TEInstallerCopier
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static internal string GetSource
		{
			get
			{
				RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks\TEInstallerCopier");
				return (string)key.GetValue("Source", string.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static internal string GetTarget
		{
			get
			{
				RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks\TEInstallerCopier");
				return (string)key.GetValue("Target", string.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CopyFiles()
		{
			CopyBuildType("Debug");
			CopyBuildType("Release");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="buildType"></param>
		/// ------------------------------------------------------------------------------------
		private void CopyBuildType(string buildType)
		{
			DateTime date;
			string folder;

			// Start with the current date and try to find a folder containing today's build.
			// If today's can't be found go backward one day at a time for 5 days. The first
			// date found while stepping back in time (within those 5 days) is the one from
			// which the copy is made.
			for (int i = 0; i < 5; i++)
			{
				date = DateTime.Now.Subtract(TimeSpan.FromDays(i));
				folder = date.Year.ToString("0000") + "-" + date.Month.ToString("00") +
					"-" + date.Day.ToString("00");

				string filepath = GetSource + "\\" + folder + @"\install\" + buildType + "\\TE.msi";

				if (File.Exists(filepath))
				{
					File.Copy(filepath, GetTarget + "\\" + folder + "-" + buildType + "-" + "TE.msi", true);
					return;
				}
			}
		}
	}
}
