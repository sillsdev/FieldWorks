// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2008' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LiftImportDlg.cs
// Responsibility: SteveMc (original version by John Hatton as extension)
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FwCoreDlgs.BackupRestore;
using XCore;
using SIL.Utils;
using SIL.Utils.FileDialog;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class LiftImportDlg : Form, IFwExtension
	{
		private FdoCache m_cache;
		private Mediator m_mediator;
		private IProgress m_progressDlg;
		private string m_sLogFile; // name of HTML log file (if successful).
		private OpenFileDialogAdapter openFileDialog1;


		private FlexLiftMerger.MergeStyle m_msImport = FlexLiftMerger.MergeStyle.MsKeepOld;

		public LiftImportDlg()
		{
			InitializeComponent();
			openFileDialog1 = new OpenFileDialogAdapter();
			openFileDialog1.Title = LexTextControls.openFileDialog1_Title;
			openFileDialog1.Filter = FileUtils.FileDialogFilterCaseInsensitiveCombinations(
				LexTextControls.openFileDialog1_Filter);
		}

		/// <summary>
		/// From IFwExtension
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		void IFwExtension.Init(FdoCache cache, Mediator mediator)
		{
			m_cache = cache;
			m_mediator = mediator;
			string sPrevFile = m_mediator.PropertyTable.GetStringProperty(FilePropertyName, null);
			if (!String.IsNullOrEmpty(sPrevFile))
			{
				tbPath.Text = sPrevFile;
				UpdateButtons();
			}
			string sMergeStyle = m_mediator.PropertyTable.GetStringProperty(MergeStylePropertyName, null);
			if (!String.IsNullOrEmpty(sMergeStyle))
			{
				m_msImport = (FlexLiftMerger.MergeStyle)Enum.Parse(typeof(FlexLiftMerger.MergeStyle), sMergeStyle, true);
				switch (m_msImport)
				{
					case FlexLiftMerger.MergeStyle.MsKeepOld:
						m_rbKeepCurrent.Checked = true;
						break;
					case FlexLiftMerger.MergeStyle.MsKeepNew:
						m_rbKeepNew.Checked = true;
						break;
					case FlexLiftMerger.MergeStyle.MsKeepBoth:
						m_rbKeepBoth.Checked = true;
						break;
					default:
						m_rbKeepCurrent.Checked = true;
						break;
				}
			}
		}

		private string FilePropertyName
		{
			get { return "LIFT-ImportFile"; }
		}

		private string MergeStylePropertyName
		{
			get { return "LIFT-MergeStyle"; }
		}

		/// <summary>
		/// (IFwImportDialog)Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value</returns>
		public new DialogResult Show(IWin32Window owner)
		{
			return this.ShowDialog(owner);
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			UpdateButtons();
			if (!btnOK.Enabled)
				return;
			DoImport();
			this.DialogResult = DialogResult.OK;
			if (!String.IsNullOrEmpty(m_sLogFile))
				Process.Start(m_sLogFile);		// display log file.
			this.Close();
		}

		private void btnBackup_Click(object sender, EventArgs e)
		{
			using(var dlg = new BackupProjectDlg(m_cache, FwUtils.ksFlexAbbrev, m_mediator.HelpTopicProvider))
				dlg.ShowDialog(this);
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void btnBrowse_Click(object sender, EventArgs e)
		{
			if (DialogResult.OK != openFileDialog1.ShowDialog())
				return;

			tbPath.Text = openFileDialog1.FileName;
			UpdateButtons();
			if (btnOK.Enabled)
			{
				m_mediator.PropertyTable.SetProperty(FilePropertyName, tbPath.Text);
				m_mediator.PropertyTable.SetPropertyPersistence(FilePropertyName, true);
			}
		}

		private void UpdateButtons()
		{
			btnOK.Enabled = tbPath.Text.Length > 0 &&
				File.Exists(tbPath.Text);
		}

		private void DoImport()
		{
			using (new WaitCursor(this))
			{
				using (var progressDlg = new ProgressDialogWithTask(this))
				{
					progressDlg.Minimum = 0;
					progressDlg.Maximum = 100;
					progressDlg.CancelButtonVisible = true;
					progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;
					progressDlg.Restartable = true;
					progressDlg.Title = String.Format(LexTextControls.ksImportingFrom0, tbPath.Text);
					try
					{
						m_cache.DomainDataByFlid.BeginNonUndoableTask();
						m_sLogFile = (string)progressDlg.RunTask(true, ImportLIFT, tbPath.Text);
					}
					finally
					{
						// This can indirectly try to access Views code in all the PropChanged
						// handling.  This is why the UOW handling has been moved from ImportLIFT
						// (which executes on a different thread).  See FWR-3057.
						m_cache.DomainDataByFlid.EndNonUndoableTask();
					}
				}
			}
		}

		/// <summary>
		/// Import from a LIFT file.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters: 1) filename</param>
		/// <returns></returns>
		private object ImportLIFT(IProgress progressDlg, params object[] parameters)
		{
			m_progressDlg = progressDlg;
			Debug.Assert(parameters.Length == 1);
			string sOrigFile = (string)parameters[0];
			try
			{
				string sFilename;
				bool fMigrationNeeded = LiftIO.Migration.Migrator.IsMigrationNeeded(sOrigFile);
				if (fMigrationNeeded)
				{
					string sOldVersion = LiftIO.Validation.Validator.GetLiftVersion(sOrigFile);
					m_progressDlg.Message = String.Format(LexTextControls.ksMigratingLiftFile,
						sOldVersion, LiftIO.Validation.Validator.LiftVersion);
					sFilename = LiftIO.Migration.Migrator.MigrateToLatestVersion(sOrigFile);
				}
				else
				{
					sFilename = sOrigFile;
				}
				if (!Validate(sFilename, sOrigFile))
					return null;
				m_progressDlg.Message = LexTextControls.ksLoadingVariousLists;
				FlexLiftMerger flexImporter = new FlexLiftMerger(m_cache, m_msImport, m_chkTrustModTimes.Checked);
				LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample> parser =
					new LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>(flexImporter);
				parser.SetTotalNumberSteps += parser_SetTotalNumberSteps;
				parser.SetStepsCompleted += parser_SetStepsCompleted;
				parser.SetProgressMessage += parser_SetProgressMessage;
				flexImporter.LiftFile = sOrigFile;

				flexImporter.LoadLiftRanges(sOrigFile + "-ranges");	// temporary (?) fix for FWR-3869.
				int cEntries = parser.ReadLiftFile(sFilename);

				if (fMigrationNeeded)
				{
					// Try to move the migrated file to the temp directory, even if a copy of it
					// already exists there.
					string sTempMigrated = Path.Combine(Path.GetTempPath(),
						Path.ChangeExtension(Path.GetFileName(sFilename), "." + LiftIO.Validation.Validator.LiftVersion + FwFileExtensions.ksLexiconInterchangeFormat));
					if (File.Exists(sTempMigrated))
						File.Delete(sTempMigrated);
					File.Move(sFilename, sTempMigrated);
				}
				flexImporter.ProcessPendingRelations(m_progressDlg);
				return flexImporter.DisplayNewListItems(sOrigFile, cEntries);
			}
			catch (Exception error)
			{
				string sMsg = String.Format(LexTextControls.ksLIFTImportProblem,
					sOrigFile, error.Message);
				try
				{
					StringBuilder bldr = new StringBuilder();
					// leave in English for programmer's sake...
					bldr.AppendFormat("Something went wrong while FieldWorks was attempting to import {0}.",
						sOrigFile);
					bldr.AppendLine();
					bldr.AppendLine(error.Message);
					bldr.AppendLine();
					bldr.AppendLine(error.StackTrace);

					if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
						ClipboardUtils.SetDataObject(bldr.ToString(), true);
					else
						SIL.Utils.Logger.WriteEvent(bldr.ToString());
				}
				catch
				{
				}
				MessageBox.Show(sMsg, LexTextControls.ksProblemImporting,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return null;
			}
		}

		private bool Validate(string sFilename, string sOrigFile)
		{
			try
			{
				m_progressDlg.Message = LexTextControls.ksValidatingInputFile;
				ValidationProgress prog = new ValidationProgress(m_progressDlg);
				LiftIO.Validation.Validator.CheckLiftWithPossibleThrow(sFilename, prog);
				return true;
			}
			catch (LiftIO.LiftFormatException lfe)
			{
				string sProducer = GetLiftProducer(sOrigFile);
				string sMsg;
				if (sProducer == null)
				{
					sMsg = String.Format(LexTextControls.ksFileNotALIFTFile, sOrigFile);
				}
				else if (sFilename == sOrigFile)
				{
					sMsg = String.Format(LexTextControls.ksInvalidLiftFile, sOrigFile, sProducer);
				}
				else
				{
					sMsg = String.Format(LexTextControls.ksInvalidMigratedLiftFile, sOrigFile, sProducer);
				}
				// Show the pretty yellow semi-crash dialog box, with instructions for the
				// user to report the bug.  Then ask the user whether to continue.
				IApp app = (IApp)m_mediator.PropertyTable.GetValue("App");
				Utils.ErrorReporter.ReportException(new Exception(sMsg, lfe), app.SettingsKey,
					m_mediator.FeedbackInfoProvider.SupportEmailAddress, this, false);
				return MessageBox.Show(LexTextControls.ksContinueLiftImportQuestion,
					LexTextControls.ksProblemImporting,
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
			}
		}

		private string GetLiftProducer(string sOrigFile)
		{
			try
			{
				using (XmlReader xrdr = XmlReader.Create(sOrigFile))
				{
					if (xrdr.IsStartElement() && xrdr.Name == "lift")
					{
						string sProducer = xrdr.GetAttribute("producer");
						if (String.IsNullOrEmpty(sProducer))
							return "Unknown LIFT Producer";
						else
							return sProducer;
					}
				}
			}
			catch
			{
			}
			return null;
		}

		void parser_SetProgressMessage(object sender, LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.MessageArgs e)
		{
			if (m_progressDlg != null)
				m_progressDlg.Message = e.Message;
		}

		void parser_SetTotalNumberSteps(object sender, LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.StepsArgs e)
		{
			if (m_progressDlg != null)
			{
				m_progressDlg.Minimum = 0;
				m_progressDlg.Maximum = e.Steps;
			}
		}

		void parser_SetStepsCompleted(object sender, LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.ProgressEventArgs e)
		{
			if (m_progressDlg != null)
			{
				int nMin = m_progressDlg.Minimum;
				int nMax = m_progressDlg.Maximum;
				Debug.Assert(nMin < nMax);
				if (nMin >= nMax)
					nMax = nMin + 1;
				int n = e.Progress;
				if (n < nMin)
				{
					n = nMin;
				}
				if (n > nMax)
				{
					while (n > nMax)
						n = nMin + (n - nMax);
				}
				m_progressDlg.Position = n;
				e.Cancel = m_progressDlg.Canceled;
			}
		}

		private void LiftImportDlg_Load(object sender, EventArgs e)
		{
			UpdateButtons();
		}

		private void m_rbKeepCurrent_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbKeepCurrent.Checked)
			{
				m_rbKeepNew.Checked = false;
				m_rbKeepBoth.Checked = false;
				SetMergeStyle(FlexLiftMerger.MergeStyle.MsKeepOld);
			}
		}

		private void m_rbKeepNew_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbKeepNew.Checked)
			{
				m_rbKeepCurrent.Checked = false;
				m_rbKeepBoth.Checked = false;
				SetMergeStyle(FlexLiftMerger.MergeStyle.MsKeepNew);
			}
		}

		private void m_rbKeepBoth_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbKeepBoth.Checked)
			{
				m_rbKeepCurrent.Checked = false;
				m_rbKeepNew.Checked = false;
				SetMergeStyle(FlexLiftMerger.MergeStyle.MsKeepBoth);
			}
		}

		private void SetMergeStyle(FlexLiftMerger.MergeStyle ms)
		{
			m_msImport = ms;
			m_mediator.PropertyTable.SetProperty(MergeStylePropertyName,
				Enum.GetName(typeof(FlexLiftMerger.MergeStyle), m_msImport));
			m_mediator.PropertyTable.SetPropertyPersistence(MergeStylePropertyName, true);
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_mediator.HelpTopicProvider, "khtpImportLIFT");
		}
	}
}
