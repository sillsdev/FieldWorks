// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.FieldWorks.FwCoreDlgs.BackupRestore;
using SIL.FieldWorks.Resources;
using SIL.Lift;
using SIL.Lift.Migration;
using SIL.Lift.Parsing;
using SIL.Lift.Validation;
using SIL.Reporting;
using SIL.LCModel.Utils;
using SIL.Utils;
using XCore;
using Ionic.Zip;
using System.Linq;

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class CombineImportDlg : Form, IFwExtension
	{
		private LcmCache m_cache;
		private Mediator m_mediator;
		private XCore.PropertyTable m_propertyTable;
		private IThreadedProgress m_progressDlg;
		string m_sLogFile;      // name of HTML log file (if successful).

		private FlexLiftMerger.MergeStyle m_msImport = FlexLiftMerger.MergeStyle.MsKeepOnlyNew;

		public CombineImportDlg()
		{
			InitializeComponent();
			openFileDialog1.Title = LexTextControls.openFileDialog1_Title;
			openFileDialog1.Filter = FileUtils.FileDialogFilterCaseInsensitiveCombinations(
				LexTextControls.openFileDialog1_Zip_Filter);
		}

		/// <summary>
		/// From IFwExtension
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		void IFwExtension.Init(LcmCache cache, Mediator mediator, XCore.PropertyTable propertyTable)
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
		}

		private string FilePropertyName
		{
			get { return "Combine-ImportFile"; }
		}

		private string MergeStylePropertyName
		{
			get { return "Combine-MergeStyle"; }
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
		private string PrepareImport(string importZipFile)
		{
			if (string.IsNullOrEmpty(importZipFile))
			{
				return null;
			}

			try
			{
				using (var zip = new ZipFile(importZipFile))
				{
					var tmpPath = Path.GetTempPath();
					var combineLiftFile = zip.SelectEntries("*.lift").First();
					combineLiftFile.Extract(tmpPath, ExtractExistingFileAction.OverwriteSilently);
					return tmpPath + combineLiftFile.FileName;
				}
			}
			catch (Exception error)
			{
				string sMsg = String.Format(LexTextControls.ksLIFTImportProblem,
					importZipFile, error.Message);
				MessageBox.Show(sMsg, LexTextControls.ksProblemImporting,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
				throw;
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
			string sOrigFile = (string)parameters[0];
			try
			{
				// Create a temporary directory %temp%\TempForCombineImport. Migrate as necessary and import from this
				// directory. Directory is left after import is done in case it is needed, but will be deleted next time
				// if it exists.
				var sCombinefolder = Path.GetDirectoryName(sOrigFile);
				var sCombinetempFolder = Path.Combine(Path.GetTempPath(), "TempForCombineImport");
				var sTempFileName = Path.Combine(sCombinetempFolder, sOrigFile.Substring(sCombinefolder.Length + 1));
				if (Directory.Exists(sCombinetempFolder))
					Directory.Delete(sCombinetempFolder, true);
				Directory.CreateDirectory(sCombinetempFolder);

				LdmlFileBackup.CopyFile(sOrigFile, sTempFileName);

				var sTempLiftFile = PrepareImport(sTempFileName);

				string sFilename = sTempLiftFile;
				//Validate the Combine file.
				if (!Validate(sFilename, sTempLiftFile))
					return null;

				//Import the Combine file and ranges file.
				m_progressDlg.Message = LexTextControls.ksLoadingVariousLists;
				var flexImporter = new FlexLiftMerger(m_cache, m_msImport, m_chkTrustModTimes.Checked);
				var parser = new LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>(flexImporter);
				parser.SetTotalNumberSteps += parser_SetTotalNumberSteps;
				parser.SetStepsCompleted += parser_SetStepsCompleted;
				parser.SetProgressMessage += parser_SetProgressMessage;

				flexImporter.LiftFile = sTempLiftFile;

				//Before imporing the Combine files ensure the LDML (language definition files) have the correct writing system codes.
				flexImporter.LdmlFilesMigration(sCombinetempFolder, sFilename, sTempLiftFile + "-ranges");
				//Import the Ranges file.
				flexImporter.LoadLiftRanges(sTempLiftFile + "-ranges");	// temporary (?) fix for FWR-3869.
				//Import the Combine data file.
				int cEntries = parser.ReadLiftFile(sFilename);

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
					{
						progressDlg.SynchronizeInvoke.Invoke(() => ClipboardUtils.SetDataObject(bldr.ToString(), true));
						Logger.WriteEvent(bldr.ToString());
					}
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
				string sProducer = GetCombineProducer(sOrigFile);
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

		private string GetCombineProducer(string sOrigFile)
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
