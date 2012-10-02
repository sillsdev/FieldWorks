// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExportRtfDialog.cs
// Responsibility: Edge
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ExportRtfDialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ExportRtfDialog : Form, IFWDisposable
	{
		private System.Windows.Forms.TextBox m_txtOutputFile;
		private System.Windows.Forms.Button m_btnHelp;
		private RegistryStringSetting m_rtfFolder;
		private TeImportExportFileDialog m_fileDialog;
		private IHelpTopicProvider m_helpTopicProvider;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private CheckBox chkAutoOpen;

		private IScripture m_scr;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ExportRtfDialog"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public ExportRtfDialog(FdoCache cache, IHelpTopicProvider helpTopicProvider)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			// If the current settings are for arabic digits then don't show the option
			// to export them as arabic.
			m_scr = cache.LangProject.TranslatedScriptureOA;
			m_helpTopicProvider = helpTopicProvider;

			// Set default export folder.
			m_rtfFolder = new RegistryStringSetting(
				Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				"ExportFolderForRTF", FwSubKey.TE);
			string fileName = m_rtfFolder.Value;

			m_fileDialog = new TeImportExportFileDialog(cache.ProjectId.Name, FileType.RTF);

			// Append a filename if it was set to just a directory
			if (Directory.Exists(fileName))
				fileName = Path.Combine(fileName, m_fileDialog.DefaultFileName);
			m_txtOutputFile.Text = fileName;
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
					components.Dispose();
				if (m_fileDialog != null)
				{
					m_fileDialog.Dispose();
					m_fileDialog = null;
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
			System.Windows.Forms.Label label1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExportRtfDialog));
			System.Windows.Forms.Button btnBrowse;
			System.Windows.Forms.Button btnOk;
			System.Windows.Forms.Button btnCancel;
			this.m_txtOutputFile = new System.Windows.Forms.TextBox();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.chkAutoOpen = new System.Windows.Forms.CheckBox();
			label1 = new System.Windows.Forms.Label();
			btnBrowse = new System.Windows.Forms.Button();
			btnOk = new System.Windows.Forms.Button();
			btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// btnBrowse
			//
			resources.ApplyResources(btnBrowse, "btnBrowse");
			btnBrowse.Name = "btnBrowse";
			btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
			//
			// btnOk
			//
			resources.ApplyResources(btnOk, "btnOk");
			btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			btnOk.Name = "btnOk";
			btnOk.Click += new System.EventHandler(this.btnOk_Click);
			//
			// btnCancel
			//
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			btnCancel.Name = "btnCancel";
			//
			// m_txtOutputFile
			//
			resources.ApplyResources(this.m_txtOutputFile, "m_txtOutputFile");
			this.m_txtOutputFile.Name = "m_txtOutputFile";
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// chkAutoOpen
			//
			resources.ApplyResources(this.chkAutoOpen, "chkAutoOpen");
			this.chkAutoOpen.Name = "chkAutoOpen";
			this.chkAutoOpen.UseVisualStyleBackColor = true;
			//
			// ExportRtfDialog
			//
			this.AcceptButton = btnOk;
			resources.ApplyResources(this, "$this");
			this.CancelButton = btnCancel;
			this.Controls.Add(this.chkAutoOpen);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(btnCancel);
			this.Controls.Add(btnOk);
			this.Controls.Add(btnBrowse);
			this.Controls.Add(this.m_txtOutputFile);
			this.Controls.Add(label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "ExportRtfDialog";
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event on the Ok button.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnOk_Click(object sender, EventArgs e)
		{
			// Save default export folder location to registry, if it exists.
			string directoryName = MiscUtils.GetFolderName(m_txtOutputFile.Text);
			if (directoryName != string.Empty)
				m_rtfFolder.Value = directoryName;

			// If all that was typed in was a directory, then add a filename to it.
			if (directoryName == m_txtOutputFile.Text)
			{
				m_txtOutputFile.Text = Path.Combine(directoryName,
					m_scr.Cache.ProjectId.Name + ".rtf");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Browse button to indicate an output file name.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnBrowse_Click(object sender, System.EventArgs e)
		{
			if (m_fileDialog.ShowSaveDialog(m_txtOutputFile.Text, this) == DialogResult.OK)
				m_txtOutputFile.Text = m_fileDialog.FileName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the Help button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpExportRTF");
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the output file name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string FileName
		{
			get
			{
				CheckDisposed();
				return m_txtOutputFile.Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the file will be automatically opened after export.
		/// </summary>
		/// <value><c>true</c> if automatically open file; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool AutoOpenFile
		{
			get
			{
				CheckDisposed();
				return chkAutoOpen.Checked;
			}
		}
		#endregion
	}
}
