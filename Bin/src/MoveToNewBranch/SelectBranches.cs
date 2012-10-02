// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Form1.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;

namespace MoveBranch
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for SelectBranches.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SelectBranches : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
		private System.Windows.Forms.TextBox m_oldPath;
		private System.Windows.Forms.Button btnOldPathSelect;
		private System.Windows.Forms.Button btnNewPathSelect;
		private System.Windows.Forms.TextBox m_newPath;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label lblProgress;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Form1"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SelectBranches()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
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

		#region Windows Form Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SelectBranches));
			this.label1 = new System.Windows.Forms.Label();
			this.m_oldPath = new System.Windows.Forms.TextBox();
			this.btnOldPathSelect = new System.Windows.Forms.Button();
			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.btnNewPathSelect = new System.Windows.Forms.Button();
			this.m_newPath = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.lblProgress = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// label1
			//
			this.label1.Location = new System.Drawing.Point(8, 136);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(280, 23);
			this.label1.TabIndex = 0;
			this.label1.Text = "Old Perforce Branch Directory (e.g. c:\\fw):";
			//
			// m_oldPath
			//
			this.m_oldPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_oldPath.Location = new System.Drawing.Point(8, 160);
			this.m_oldPath.Name = "m_oldPath";
			this.m_oldPath.Size = new System.Drawing.Size(248, 20);
			this.m_oldPath.TabIndex = 1;
			//
			// btnOldPathSelect
			//
			this.btnOldPathSelect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOldPathSelect.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnOldPathSelect.Location = new System.Drawing.Point(264, 160);
			this.btnOldPathSelect.Name = "btnOldPathSelect";
			this.btnOldPathSelect.Size = new System.Drawing.Size(24, 23);
			this.btnOldPathSelect.TabIndex = 2;
			this.btnOldPathSelect.Text = "...";
			this.btnOldPathSelect.Click += new System.EventHandler(this.OnClickOldPath);
			//
			// folderBrowserDialog
			//
			this.folderBrowserDialog.RootFolder = System.Environment.SpecialFolder.MyComputer;
			this.folderBrowserDialog.SelectedPath = "c:\\";
			this.folderBrowserDialog.ShowNewFolderButton = false;
			//
			// btnNewPathSelect
			//
			this.btnNewPathSelect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnNewPathSelect.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnNewPathSelect.Location = new System.Drawing.Point(264, 216);
			this.btnNewPathSelect.Name = "btnNewPathSelect";
			this.btnNewPathSelect.Size = new System.Drawing.Size(24, 23);
			this.btnNewPathSelect.TabIndex = 2;
			this.btnNewPathSelect.Text = "...";
			this.btnNewPathSelect.Click += new System.EventHandler(this.OnClickNewPath);
			//
			// m_newPath
			//
			this.m_newPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_newPath.Location = new System.Drawing.Point(8, 216);
			this.m_newPath.Name = "m_newPath";
			this.m_newPath.Size = new System.Drawing.Size(248, 20);
			this.m_newPath.TabIndex = 1;
			//
			// label2
			//
			this.label2.Location = new System.Drawing.Point(8, 192);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(280, 23);
			this.label2.TabIndex = 0;
			this.label2.Text = "New Perforce Branch Directory (e.g. c:\\fw31):";
			//
			// btnOk
			//
			this.btnOk.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnOk.Location = new System.Drawing.Point(64, 248);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 3;
			this.btnOk.Text = "OK";
			this.btnOk.Click += new System.EventHandler(this.OnOk);
			//
			// btnCancel
			//
			this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnCancel.Location = new System.Drawing.Point(152, 248);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 4;
			this.btnCancel.Text = "Cancel";
			//
			// label3
			//
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label3.Location = new System.Drawing.Point(8, 8);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(280, 72);
			this.label3.TabIndex = 5;
			this.label3.Text = resources.GetString("label3.Text");
			//
			// lblProgress
			//
			this.lblProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.lblProgress.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.lblProgress.Location = new System.Drawing.Point(8, 80);
			this.lblProgress.Name = "lblProgress";
			this.lblProgress.Size = new System.Drawing.Size(280, 48);
			this.lblProgress.TabIndex = 6;
			//
			// SelectBranches
			//
			this.AcceptButton = this.btnOk;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(292, 278);
			this.Controls.Add(this.lblProgress);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.btnOldPathSelect);
			this.Controls.Add(this.m_oldPath);
			this.Controls.Add(this.m_newPath);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.btnNewPathSelect);
			this.Controls.Add(this.label2);
			this.Name = "SelectBranches";
			this.Text = "Move to a new branch";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[STAThread]
		static void Main()
		{
			Form form = new SelectBranches();
			form.ShowDialog();
		}

		private void OnClickOldPath(object sender, System.EventArgs e)
		{
			if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
				m_oldPath.Text = folderBrowserDialog.SelectedPath;
		}

		private void OnClickNewPath(object sender, System.EventArgs e)
		{
			if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
				m_newPath.Text = folderBrowserDialog.SelectedPath;
		}

		private void OnOk(object sender, System.EventArgs e)
		{
			if (m_oldPath.Text.Length == 0 || m_newPath.Text.Length == 0)
				return;

			if (!Directory.Exists(m_oldPath.Text) || !Directory.Exists(m_newPath.Text))
				return;

			Cursor oldCursor = Cursor;
			try
			{
				Cursor = Cursors.WaitCursor;
				string oldPath = Path.GetFullPath(m_oldPath.Text);
				string newPath = Path.GetFullPath(m_newPath.Text);

				if (!oldPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
					oldPath += Path.DirectorySeparatorChar;
				if (!newPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
					newPath += Path.DirectorySeparatorChar;

				DirectoryInfo dirInfo = new DirectoryInfo(oldPath);

				ProcessDirectory(dirInfo, oldPath, newPath);
			}
			finally
			{
				Cursor = oldCursor;
			}

			MessageBox.Show("Finished!");
		}

		private void ProcessDirectory(DirectoryInfo dirInfo, string oldPath, string newPath)
		{
			lblProgress.Text = "Processing " + dirInfo.FullName;
			Application.DoEvents();

			foreach (FileInfo fileInfo in dirInfo.GetFiles("*.user"))
				ProcessFile(fileInfo, oldPath, newPath);

			foreach (DirectoryInfo childDirInfo in dirInfo.GetDirectories())
				ProcessDirectory(childDirInfo, oldPath, newPath);
		}

		private void ProcessFile(FileInfo fileInfo, string oldPath, string newPath)
		{
			string newFilename = Path.Combine(newPath, fileInfo.FullName.Substring(
				oldPath.Trim('\\').Length + 1));
			if (File.Exists(newFilename))
				return;

			StreamWriter writer = null;
			StreamReader reader = null;
			try
			{
				writer = new StreamWriter(newFilename, false);
				reader = fileInfo.OpenText();
				for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
				{
					string lowerPath = oldPath.ToLower();
					for (int nPos = line.ToLower().IndexOf(lowerPath); nPos > -1;
						nPos = line.ToLower().IndexOf(lowerPath))
					{
						line = line.Substring(0, nPos) + newPath + line.Substring(nPos + oldPath.Length);
					}
					writer.WriteLine(line);
				}
			}
			catch
			{
			}
			finally
			{
				if (reader != null)
					reader.Close();
				if (writer != null)
					writer.Close();
			}
		}
	}
}
