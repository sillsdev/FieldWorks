// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: LiftImportDlg.cs
// Responsibility: SteveMc (original version by John Hatton as extension)
// Last reviewed:
//
// <remarks>
// </remarks>
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using Palaso.Lift;
using Palaso.Lift.Migration;
using Palaso.Lift.Parsing;
using Palaso.Lift.Validation;
using SIL.CoreImpl;
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
		private PropertyTable m_propertyTable;
		private IThreadedProgress m_progressDlg;
		string m_sLogFile;		// name of HTML log file (if successful).
		private OpenFileDialogAdapter openFileDialog1;

		private FlexLiftMerger.MergeStyle m_msImport = FlexLiftMerger.MergeStyle.MsKeepOld;

		public LiftImportDlg()
		{
			openFileDialog1 = new OpenFileDialogAdapter();
			InitializeComponent();
			openFileDialog1.Title = LexTextControls.openFileDialog1_Title;
			openFileDialog1.Filter = FileUtils.FileDialogFilterCaseInsensitiveCombinations(
				LexTextControls.openFileDialog1_Filter);
		}

		/// <summary>
		/// From IFwExtension
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		void IFwExtension.Init(FdoCache cache, Mediator mediator, PropertyTable propertyTable)
		{
			m_cache = cache;
			m_mediator = mediator;
			m_propertyTable = propertyTable;
			string sPrevFile = m_propertyTable.GetStringProperty(FilePropertyName, null);
			if (!String.IsNullOrEmpty(sPrevFile))
			{
				tbPath.Text = sPrevFile;
				UpdateButtons();
			}
			string sMergeStyle = m_propertyTable.GetStringProperty(MergeStylePropertyName, null);
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
			{
				using (Process.Start(m_sLogFile)) // display log file.
				{
				}
			}
			this.Close();
		}

		private void btnBackup_Click(object sender, EventArgs e)
		{
			using (var dlg = new BackupProjectDlg(m_cache, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
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
				m_propertyTable.SetProperty(FilePropertyName, tbPath.Text, true);
				m_propertyTable.SetPropertyPersistence(FilePropertyName, true);
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
					progressDlg.AllowCancel = true;
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
		private object ImportLIFT(IThreadedProgress progressDlg, params object[] parameters)
		{
			m_progressDlg = progressDlg;
			Debug.Assert(parameters.Length == 1);
			string sOrigFile = (string)parameters[0];
			try
			{
				// Create a temporary directory %temp%\TempForLIFTImport. Migrate as necessary and import from this
				// directory. Directory is left after import is done in case it is needed, but will be deleted next time
				// if it exists.
				var sLIFTfolder = Path.GetDirectoryName(sOrigFile);
				var sLIFTtempFolder = Path.Combine(Path.GetTempPath(), "TempForLIFTImport");
				if (Directory.Exists(sLIFTtempFolder) == true)
					Directory.Delete(sLIFTtempFolder, true);
				LdmlFileBackup.CopyDirectory(sLIFTfolder, sLIFTtempFolder);
				// Older LIFT files had ldml files in root directory. If found, move them to WritingSystem folder.
				if (Directory.GetFiles(sLIFTtempFolder, "*.ldml").Length > 0)
				{
					var sWritingSystems = Path.Combine(sLIFTtempFolder, "WritingSystems");
					if (Directory.Exists(sWritingSystems) == false)
						Directory.CreateDirectory(sWritingSystems);
					foreach (string filePath in Directory.GetFiles(sLIFTtempFolder, "*.ldml"))
					{
						string file = Path.GetFileName(filePath);
						if (!File.Exists(Path.Combine(sWritingSystems, file)))
							File.Move(filePath, Path.Combine(sWritingSystems, file));
					}
				}
				var sTempOrigFile = Path.Combine(sLIFTtempFolder, sOrigFile.Substring(sLIFTfolder.Length + 1));
				string sFilename;
				//Do a LIFT Migration to the current version of LIFT if it is needed.
				bool fMigrationNeeded = Migrator.IsMigrationNeeded(sTempOrigFile);
				if (fMigrationNeeded)
				{
					string sOldVersion = Validator.GetLiftVersion(sTempOrigFile);
					m_progressDlg.Message = String.Format(LexTextControls.ksMigratingLiftFile,
						sOldVersion, Validator.LiftVersion);
					sFilename = Migrator.MigrateToLatestVersion(sTempOrigFile);
				}
				else
				{
					sFilename = sTempOrigFile;
				}
				//Validate the LIFT file.
				if (!Validate(sFilename, sTempOrigFile))
					return null;

				//Import the LIFT file and ranges file.
				m_progressDlg.Message = LexTextControls.ksLoadingVariousLists;
				var flexImporter = new FlexLiftMerger(m_cache, m_msImport, m_chkTrustModTimes.Checked);
				var parser = new LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>(flexImporter);
				parser.SetTotalNumberSteps += parser_SetTotalNumberSteps;
				parser.SetStepsCompleted += parser_SetStepsCompleted;
				parser.SetProgressMessage += parser_SetProgressMessage;

				flexImporter.LiftFile = sTempOrigFile;

				//Before imporing the LIFT files ensure the LDML (language definition files) have the correct writing system codes.
				flexImporter.LdmlFilesMigration(sLIFTtempFolder, sFilename, sTempOrigFile + "-ranges");
				//Import the Ranges file.
				flexImporter.LoadLiftRanges(sTempOrigFile + "-ranges");	// temporary (?) fix for FWR-3869.
				//Import the LIFT data file.
				int cEntries = parser.ReadLiftFile(sFilename);

				if (fMigrationNeeded)
				{
					// Try to move the migrated file to the temp directory, even if a copy of it
					// already exists there.
					string sTempMigrated = Path.Combine(Path.GetTempPath(),
						Path.ChangeExtension(Path.GetFileName(sFilename), "." + Validator.LiftVersion + FwFileExtensions.ksLexiconInterchangeFormat));
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
						progressDlg.SynchronizeInvoke.Invoke(() => ClipboardUtils.SetDataObject(bldr.ToString(), true));
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
				Validator.CheckLiftWithPossibleThrow(sFilename);
				return true;
			}
			catch (LiftFormatException lfe)
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
				IApp app = m_propertyTable.GetValue<IApp>("App");
				ErrorReporter.ReportException(new Exception(sMsg, lfe), app.SettingsKey,
					m_propertyTable.GetValue<IFeedbackInfoProvider>("FeedbackInfoProvider").SupportEmailAddress, this, false);
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

		void parser_SetProgressMessage(object sender, LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MessageArgs e)
		{
			if (m_progressDlg != null)
				m_progressDlg.Message = e.Message;
		}

		void parser_SetTotalNumberSteps(object sender, LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.StepsArgs e)
		{
			if (m_progressDlg != null)
			{
				m_progressDlg.Minimum = 0;
				m_progressDlg.Maximum = e.Steps;
			}
		}

		void parser_SetStepsCompleted(object sender, LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.ProgressEventArgs e)
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
			m_propertyTable.SetProperty(MergeStylePropertyName,
				Enum.GetName(typeof(FlexLiftMerger.MergeStyle), m_msImport),
				true);
			m_propertyTable.SetPropertyPersistence(MergeStylePropertyName, true);
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), "khtpImportLIFT");
		}
	}
}
