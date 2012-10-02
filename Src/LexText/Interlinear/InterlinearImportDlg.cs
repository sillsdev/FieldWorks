using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.FDO;
using XCore;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using System.IO;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.IText
{
	public partial class InterlinearImportDlg : Form, IFwExtension
	{
		FdoCache m_cache;
		Mediator m_mediator;

		StringBuilder m_messages = new StringBuilder();

		private string m_helpTopic = "khtpInterlinearImportDlg";

		public InterlinearImportDlg()
		{
			InitializeComponent();
		}

		private void m_btnBrowse_Click(object sender, EventArgs e)
		{
			using (OpenFileDialog dlg = new OpenFileDialog())
			{
				dlg.DefaultExt = "xml";
				dlg.Filter = ResourceHelper.BuildFileFilter(new FileFilterType[] {
					FileFilterType.XML, FileFilterType.AllFiles });
				dlg.FilterIndex = 1;
				dlg.CheckFileExists = true;
				dlg.Multiselect = false;
				if (!String.IsNullOrEmpty(m_tbFilename.Text) &&
					!String.IsNullOrEmpty(m_tbFilename.Text.Trim()))
				{
					dlg.FileName = m_tbFilename.Text;
				}
				DialogResult res = dlg.ShowDialog();
				if (res == DialogResult.OK)
					m_tbFilename.Text = dlg.FileName;
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			using (ProgressDialogWithTask dlg = new ProgressDialogWithTask(this))
			{
				dlg.CancelButtonVisible = false;
				dlg.SetRange(0, 500);
				using (new WaitCursor(this, true))
				{
					LinguaLinksImport import = new LinguaLinksImport(m_cache,
						Path.Combine(Path.GetTempPath(), "LanguageExplorer\\"),
						Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.FWCodeDirectory, "Language Explorer\\Import\\"));
					import.NextInput = m_tbFilename.Text;
					import.Error += new LinguaLinksImport.ErrorHandler(import_Error);
					dlg.Cancel += new CancelHandler(import.On_ProgressDlg_Cancel);
					try
					{
						bool fSuccess = (bool)dlg.RunTask(true,
							new BackgroundTaskInvoker(import.ImportInterlinear),
							m_tbFilename.Text);
						if (fSuccess)
							this.DialogResult = DialogResult.OK;	// only 'OK' if not exception
						else
						{
							this.DialogResult = DialogResult.Abort; // unsuccessful import
							string message = ITextStrings.ksInterlinImportFailed + "\n\n";
							message += m_messages.ToString();
							MessageBox.Show(this, message, ITextStrings.ksImportFailed, MessageBoxButtons.OK,
											MessageBoxIcon.Warning);
						}
						Close();
					}
					catch (WorkerThreadException ex)
					{
						System.Diagnostics.Debug.WriteLine("Error: " + ex.InnerException.Message);

						MessageBox.Show(String.Format(import.ErrorMessage, ex.InnerException.Message),
							ITextStrings.ksUnhandledError,
							MessageBoxButtons.OK, MessageBoxIcon.Error);
						this.DialogResult = DialogResult.Cancel;	// only 'OK' if not exception
						Close();
					}
				}
			}
		}

		void import_Error(object sender, string message, string caption)
		{
			m_messages.AppendLine(caption + ": " + message);
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, m_helpTopic);
		}

		#region IFwExtension Members

		public void Init(FdoCache cache, Mediator mediator)
		{
			m_cache = cache;
			m_mediator = mediator;
		}

		#endregion

		private void m_tbFilename_TextChanged(object sender, EventArgs e)
		{
			m_btnOK.Enabled = File.Exists(m_tbFilename.Text);
		}

		private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			string path = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\Training\Technical Notes on Interlinear Import.doc");
			try
			{
				System.Diagnostics.Process.Start(path);
			}
			catch (Exception)
			{
				MessageBox.Show(null, String.Format(ITextStrings.ksCannotLaunchX, path),
					ITextStrings.ksError);
			}
		}

	}
}
