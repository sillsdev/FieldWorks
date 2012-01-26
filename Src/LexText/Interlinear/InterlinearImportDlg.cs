using System;
using System.Text;
using System.Windows.Forms;
using System.IO;

using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.FDO;
using XCore;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.Utils.FileDialog;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.IText
{
	public partial class InterlinearImportDlg : Form, IFwExtension
	{
		private FdoCache m_cache;
		private Mediator m_mediator;

		private readonly StringBuilder m_messages = new StringBuilder();

		private IHelpTopicProvider m_helpTopicProvider;
		private const string HelpTopic = "khtpInterlinearImportDlg";

		public InterlinearImportDlg()
		{
			InitializeComponent();
		}

		private void m_btnBrowse_Click(object sender, EventArgs e)
		{
			using (var dlg = new OpenFileDialogAdapter())
			{
				dlg.DefaultExt = "flextext";
				dlg.Filter = ResourceHelper.BuildFileFilter(FileFilterType.FLExText, FileFilterType.XML, FileFilterType.AllFiles);
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
			using (var dlg = new ProgressDialogWithTask(this, m_cache.ThreadHelper))
			{
				dlg.AllowCancel = false;
				dlg.Minimum = 0;
				dlg.Maximum = 500;
				using (new WaitCursor(this, true))
				{
					var import = new LinguaLinksImport(m_cache,
						Path.Combine(Path.GetTempPath(), "LanguageExplorer" + Path.DirectorySeparatorChar),
						Path.Combine(DirectoryFinder.FWCodeDirectory, Path.Combine("Language Explorer", "Import" + Path.DirectorySeparatorChar)));
					import.NextInput = m_tbFilename.Text;
					import.Error += import_Error;
					try
					{
						var fSuccess = (bool) dlg.RunTask(true, import.ImportInterlinear, m_tbFilename.Text);
						if (fSuccess)
							DialogResult = DialogResult.OK;	// only 'OK' if not exception
						else
						{
							DialogResult = DialogResult.Abort; // unsuccessful import
							string message = ITextStrings.ksInterlinImportFailed + Environment.NewLine + Environment.NewLine;
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
						DialogResult = DialogResult.Cancel;	// only 'OK' if not exception
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
			if (m_helpTopicProvider != null)
				ShowHelp.ShowHelpTopic(m_helpTopicProvider, HelpTopic);
		}

		#region IFwExtension Members

		public void Init(FdoCache cache, Mediator mediator)
		{
			m_cache = cache;
			m_mediator = mediator;
			if (m_mediator != null)
				m_helpTopicProvider = m_mediator.HelpTopicProvider;
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
