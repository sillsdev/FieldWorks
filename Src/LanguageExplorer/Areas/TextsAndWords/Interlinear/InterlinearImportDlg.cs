// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Utils;
using WaitCursor = SIL.FieldWorks.Common.FwUtils.WaitCursor;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public partial class InterlinearImportDlg : Form, IFwExtension
	{
		private LcmCache m_cache;
		private IPublisher m_publisher;
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
				if (!string.IsNullOrEmpty(m_tbFilename.Text) && !string.IsNullOrEmpty(m_tbFilename.Text.Trim()))
				{
					dlg.FileName = m_tbFilename.Text;
				}
				var res = dlg.ShowDialog();
				if (res == DialogResult.OK)
				{
					m_tbFilename.Text = dlg.FileName;
				}
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			using (var dlg = new ProgressDialogWithTask(this))
			{
				dlg.AllowCancel = false;
				dlg.Minimum = 0;
				dlg.Maximum = 500;
				using (new WaitCursor(this, true))
				{
					var import = new LinguaLinksImport(m_cache, Path.Combine(Path.GetTempPath(), "LanguageExplorer" + Path.DirectorySeparatorChar), Path.Combine(FwDirectoryFinder.CodeDirectory, Path.Combine("Language Explorer", "Import" + Path.DirectorySeparatorChar)))
					{
						NextInput = m_tbFilename.Text
					};
					import.Error += import_Error;
					try
					{
						var fSuccess = (bool) dlg.RunTask(true, import.ImportInterlinear, m_tbFilename.Text);
						if (fSuccess)
						{
							DialogResult = DialogResult.OK; // only 'OK' if not exception
							var firstNewText = import.FirstNewText;
							if (firstNewText != null)
							{
								m_publisher?.Publish("JumpToRecord", firstNewText.Hvo);
							}
						}
						else
						{
							DialogResult = DialogResult.Abort; // unsuccessful import
							var message = ITextStrings.ksInterlinImportFailed + Environment.NewLine + Environment.NewLine;
							message += m_messages.ToString();
							MessageBox.Show(this, message, ITextStrings.ksImportFailed, MessageBoxButtons.OK, MessageBoxIcon.Warning);
						}
						Close();
					}
					catch (WorkerThreadException ex)
					{

						Debug.WriteLine("Error: " + ex.InnerException.Message);
						MessageBox.Show(string.Format(import.ErrorMessage, ex.InnerException.Message), ITextStrings.ksUnhandledError, MessageBoxButtons.OK, MessageBoxIcon.Error);
						DialogResult = DialogResult.Cancel;	// only 'OK' if not exception
						Close();
					}
				}
			}
		}

		private void import_Error(object sender, string message, string caption)
		{
			m_messages.AppendLine(caption + ": " + message);
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			if (m_helpTopicProvider != null)
			{
				ShowHelp.ShowHelpTopic(m_helpTopicProvider, HelpTopic);
			}
		}

		#region IFwExtension Members

		void IFwExtension.Init(LcmCache cache, IPropertyTable propertyTable, IPublisher publisher)
		{
			m_cache = cache;
			m_publisher = publisher;
			if (propertyTable != null)
			{
				m_helpTopicProvider = propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
			}
		}

		#endregion

		private void m_tbFilename_TextChanged(object sender, EventArgs e)
		{
			m_btnOK.Enabled = File.Exists(m_tbFilename.Text);
		}
	}
}