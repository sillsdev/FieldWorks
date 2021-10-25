// Copyright (c) 2008-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
using SIL.LCModel;
using SIL.FieldWorks.FwCoreDlgs.BackupRestore;
using SIL.Lift;
using SIL.Lift.Parsing;
using SIL.Lift.Validation;
using SIL.Reporting;
using SIL.LCModel.Utils;
using SIL.Utils;
using XCore;
using Ionic.Zip;
using System.Linq;
using SIL.FieldWorks.Common.Controls.FileDialog;

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class CombineImportDlg : Form, IFwExtension
	{
		private LcmCache m_cache;
		private PropertyTable m_propertyTable;
		private IThreadedProgress m_progressDlg;
		string m_sLogFile;      // name of HTML log file (if successful).

		private FlexLiftMerger.MergeStyle m_msImport = FlexLiftMerger.MergeStyle.MsTheCombine;

		public CombineImportDlg()
		{
			InitializeComponent();
			openFileDialog = new OpenFileDialogAdapter
			{
				Title = LexTextControls.openFileDialog1_Title,
				Filter = FileUtils.FileDialogFilterCaseInsensitiveCombinations(
					LexTextControls.openFileDialog1_Zip_Filter)
			};
		}

		/// <summary>
		/// From IFwExtension
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		void IFwExtension.Init(LcmCache cache, Mediator mediator, PropertyTable propertyTable)
		{
			m_cache = cache;
			m_propertyTable = propertyTable;
			var previousFileName = m_propertyTable.GetStringProperty(FilePropertyName, null);
			if (!string.IsNullOrEmpty(previousFileName))
			{
				tbPath.Text = previousFileName;
				UpdateButtons();
			}
		}

		private const string FilePropertyName = "Combine-ImportFile";

		/// <summary>
		///     (IFwImportDialog)Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value</returns>
		public new DialogResult Show(IWin32Window owner)
		{
			return ShowDialog(owner);
		}

		private void OkClicked(object sender, EventArgs e)
		{
			UpdateButtons();
			if (!btnOK.Enabled)
				return;
			DoImport();
			DialogResult = DialogResult.OK;
			if (!string.IsNullOrEmpty(m_sLogFile))
			{
				using (Process.Start(m_sLogFile)) // display log file.
				{
				}
			}

			Close();
		}

		private void BackupClicked(object sender, EventArgs e)
		{
			using (var dlg = new BackupProjectDlg(m_cache, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
				dlg.ShowDialog(this);
		}

		private void CancelClicked(object sender, EventArgs e)
		{
			Close();
		}

		private void BrowseClicked(object sender, EventArgs e)
		{
			if (DialogResult.OK != openFileDialog.ShowDialog())
				return;

			tbPath.Text = openFileDialog.FileName;
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
						m_sLogFile = (string)progressDlg.RunTask(true, ImportCombine, tbPath.Text);
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
		/// Prepare this dialog to import from a Combine zip file.
		///
		/// </summary>
		private string PrepareImport(string importZipFile, string originalFile)
		{
			if (string.IsNullOrEmpty(importZipFile))
			{
				return null;
			}

			try
			{
				using (var zip = new ZipFile(importZipFile))
				{
					var tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
					Directory.CreateDirectory(tmpPath);
					var combineLiftFile = zip.SelectEntries("*.lift").First();
					zip.ExtractAll(tmpPath);
					return Path.Combine(tmpPath, combineLiftFile.FileName);
				}
			}
			catch (Exception error)
			{
				var message = string.Format(LexTextControls.ksLIFTCombineImportProblem,
					originalFile, error.Message);
				MessageBox.Show(message, LexTextControls.ksProblemImporting,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return "";
			}
		}

		/// <summary>
		/// Import from a Combine file.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters: 1) filename</param>
		/// <returns></returns>
		private object ImportCombine(IThreadedProgress progressDlg, params object[] parameters)
		{
			m_progressDlg = progressDlg;
			Debug.Assert(parameters.Length == 1);
			string originalFile = (string)parameters[0];
			try
			{
				// Create a temporary directory %temp%\TempForCombineImport. Migrate as necessary and import from this
				// directory. Directory is left after import is done in case it is needed, but will be deleted next time
				// if it exists.
				var combineFolder = Path.GetDirectoryName(originalFile);
				var combineTempFolder = Path.Combine(Path.GetTempPath(), "TempForCombineImport");
				// ReSharper disable once PossibleNullReferenceException - Won't be null, but if it were we'd catch it and fail the import
				var tempFileName = Path.Combine(combineTempFolder, originalFile.Substring(combineFolder.Length + 1));
				if (Directory.Exists(combineTempFolder))
					Directory.Delete(combineTempFolder, true);
				Directory.CreateDirectory(combineTempFolder);

				LdmlFileBackup.CopyFile(originalFile, tempFileName);

				var tempLiftFile = PrepareImport(tempFileName, originalFile);
				if (string.IsNullOrWhiteSpace(tempLiftFile))
					return null;

				var fileName = tempLiftFile;
				// Validate the Combine file.
				if (!Validate(fileName, tempLiftFile))
					return null;

				// Import the Combine file and ranges file.
				m_progressDlg.Message = LexTextControls.ksLoadingVariousLists;
				var flexImporter = new FlexLiftMerger(m_cache, m_msImport, true);
				var parser = new LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>(flexImporter);
				parser.SetTotalNumberSteps += Parser_SetNumberOfSteps;
				parser.SetStepsCompleted += Parser_SetStepsCompleted;
				parser.SetProgressMessage += Parser_SetProgressMessage;

				flexImporter.LiftFile = tempLiftFile;

				// Before importing the Combine files ensure the LDML (language definition files) have the correct writing system codes.
				flexImporter.LdmlFilesMigration(combineTempFolder, fileName, tempLiftFile + "-ranges");
				// Import the Ranges file.
				flexImporter.LoadLiftRanges(tempLiftFile + "-ranges");	// temporary (?) fix for FWR-3869.
				// Import the Combine data file.
				var entryCount = parser.ReadLiftFile(fileName);

				flexImporter.ProcessPendingRelations(m_progressDlg);
				TrackingHelper.TrackImport("lexicon", "Combine", ImportExportStep.Succeeded);
				return flexImporter.DisplayNewListItems(originalFile, entryCount);
			}
			catch (Exception error)
			{
				TrackingHelper.TrackImport("lexicon", "Combine", ImportExportStep.Failed);
				var errorMessage = string.Format(LexTextControls.ksLIFTImportProblem,
					originalFile, error.Message);
				try
				{
					var builder = new StringBuilder();
					// leave in English for programmer's sake...
					builder.AppendFormat("Something went wrong while FieldWorks was attempting to import {0}.",
						originalFile);
					builder.AppendLine();
					builder.AppendLine(error.Message);
					builder.AppendLine();
					builder.AppendLine(error.StackTrace);

					if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
						ClipboardUtils.SetDataObject(builder.ToString(), true);
					else
					{
						progressDlg.SynchronizeInvoke.Invoke(() => ClipboardUtils.SetDataObject(builder.ToString(), true));
						Logger.WriteEvent(builder.ToString());
					}
				}
				catch
				{
					// Exceptions while trying to build a nice error message can be ignored
				}
				MessageBox.Show(errorMessage, LexTextControls.ksProblemImporting,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return null;
			}
		}

		private bool Validate(string fileName, string originalFileName)
		{
			try
			{
				m_progressDlg.Message = LexTextControls.ksValidatingInputFile;
				Validator.CheckLiftWithPossibleThrow(fileName);
				return true;
			}
			catch (LiftFormatException lfe)
			{
				var combineProducer = GetCombineProducer(originalFileName);
				string message;
				if (combineProducer == null)
				{
					message = string.Format(LexTextControls.ksFileNotALIFTFile, originalFileName);
				}
				else if (fileName == originalFileName)
				{
					message = string.Format(LexTextControls.ksInvalidLiftFile, originalFileName, combineProducer);
				}
				else
				{
					message = string.Format(LexTextControls.ksInvalidMigratedLiftFile, originalFileName, combineProducer);
				}
				// Show the pretty yellow semi-crash dialog box, with instructions for the
				// user to report the error. Then ask the user whether to continue.
				IApp app = m_propertyTable.GetValue<IApp>("App");
				ErrorReporter.ReportException(new Exception(message, lfe), app.SettingsKey,
					m_propertyTable.GetValue<IFeedbackInfoProvider>("FeedbackInfoProvider").SupportEmailAddress, this, false);
				return MessageBox.Show(LexTextControls.ksContinueLiftImportQuestion,
					LexTextControls.ksProblemImporting,
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
			}
		}

		private static string GetCombineProducer(string fileName)
		{
			try
			{
				using (var xmlReader = XmlReader.Create(fileName))
				{
					if (xmlReader.IsStartElement() && xmlReader.Name == "lift")
					{
						var producer = xmlReader.GetAttribute("producer");
						if (!string.IsNullOrEmpty(producer))
							return producer;
					}
				}
			}
			catch
			{
				// Crashes discovering the producer can be discarded, real issues with the file
				// will be dealt with elsewhere
			}
			return "Unknown LIFT Producer";
		}

		private void Parser_SetProgressMessage(object sender, LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MessageArgs e)
		{
			if (m_progressDlg != null)
				m_progressDlg.Message = e.Message;
		}

		private void Parser_SetNumberOfSteps(object sender, LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.StepsArgs e)
		{
			if (m_progressDlg != null)
			{
				m_progressDlg.Minimum = 0;
				m_progressDlg.Maximum = e.Steps;
			}
		}

		private void Parser_SetStepsCompleted(object sender, LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.ProgressEventArgs e)
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

		private void HelpClicked(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), "khtpImportTheCombine");
		}
	}
}
