// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class LiftImportDlg : Form, IFwExtension
	{
		private FdoCache m_cache;
		private Mediator m_mediator;
		IAdvInd4 m_progressDlg = null;
		string m_sLogFile = null;		// name of HTML log file (if successful).

		private FlexLiftMerger.MergeStyle m_msImport = FlexLiftMerger.MergeStyle.msKeepOld;

		public LiftImportDlg()
		{
			InitializeComponent();
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
					case FlexLiftMerger.MergeStyle.msKeepOld:
						m_rbKeepCurrent.Checked = true;
						break;
					case FlexLiftMerger.MergeStyle.msKeepNew:
						m_rbKeepNew.Checked = true;
						break;
					case FlexLiftMerger.MergeStyle.msKeepBoth:
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
			DIFwBackupDb backupSystem = FwBackupClass.Create();
			backupSystem.Init(FwApp.App, Handle.ToInt32());
			int nBkResult;
			nBkResult = backupSystem.UserConfigure(FwApp.App, false);
			backupSystem.Close();
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
			using (new SuppressSubTasks(m_cache, true, true))
			{
				using (new SIL.FieldWorks.Common.Utils.WaitCursor(this))
				{
					using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(this))
					{
						progressDlg.SetRange(0, 100);
						//progressDlg.CancelButtonVisible = true;
						progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;
						progressDlg.Restartable = true;
						progressDlg.Title = String.Format(LexTextControls.ksImportingFrom0, tbPath.Text);
						progressDlg.Cancel += new CancelHandler(progressDlg_Cancel);
						m_fCancelNow = false;
						m_sLogFile = (string)progressDlg.RunTask(true, new BackgroundTaskInvoker(ImportLIFT), tbPath.Text);
					}
				}
			}
		}

		/// <summary>
		/// Record a cancel flag for the next entry handled.
		/// </summary>
		bool m_fCancelNow = false;

		void progressDlg_Cancel(object sender)
		{
			m_fCancelNow = true;
		}

		/// <summary>
		/// Import from a LIFT file.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters: 1) filename</param>
		/// <returns></returns>
		private object ImportLIFT(IAdvInd4 progressDlg, params object[] parameters)
		{
			m_progressDlg = progressDlg;
			Debug.Assert(parameters.Length == 1);
			string sOrigFile = (string)parameters[0];
			string sLogFile = null;
			PropChangedHandling oldPropChg = m_cache.PropChangedHandling;
			try
			{
				m_cache.PropChangedHandling = PropChangedHandling.SuppressAll;
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
					return sLogFile;
				m_progressDlg.Message = LexTextControls.ksLoadingVariousLists;
				FlexLiftMerger flexImporter = new FlexLiftMerger(m_cache, m_msImport, m_chkTrustModTimes.Checked);
				LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample> parser =
					new LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>(flexImporter);
				parser.SetTotalNumberSteps += new EventHandler<LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.StepsArgs>(parser_SetTotalNumberSteps);
				parser.SetStepsCompleted += new EventHandler<LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.ProgressEventArgs>(parser_SetStepsCompleted);
				parser.SetProgressMessage += new EventHandler<LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.MessageArgs>(parser_SetProgressMessage);
				flexImporter.LiftFile = sOrigFile;

				int cEntries = parser.ReadLiftFile(sFilename);

				if (fMigrationNeeded)
				{
					// Try to move the migrated file to the temp directory, even if a copy of it
					// already exists there.
					string sTempMigrated = Path.Combine(Path.GetTempPath(),
						Path.ChangeExtension(Path.GetFileName(sFilename), "." + LiftIO.Validation.Validator.LiftVersion + ".lift"));
					if (File.Exists(sTempMigrated))
						File.Delete(sTempMigrated);
					File.Move(sFilename, sTempMigrated);
				}
				m_progressDlg.Message = LexTextControls.ksFixingRelationLinks;
				flexImporter.ProcessPendingRelations();
				sLogFile = flexImporter.DisplayNewListItems(sOrigFile, cEntries);
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
						Clipboard.SetDataObject(bldr.ToString(), true);
					else
						SIL.Utils.Logger.WriteEvent(bldr.ToString());
				}
				catch
				{
				}
				MessageBox.Show(sMsg, LexTextControls.ksProblemImporting,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			finally
			{
				m_cache.PropChangedHandling = oldPropChg;
			}
			return sLogFile;
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
				SIL.Utils.ErrorReporter.ReportException(new Exception(sMsg, lfe), this, false);
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
				m_progressDlg.SetRange(0, e.Steps);
		}

		void parser_SetStepsCompleted(object sender, LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.ProgressEventArgs e)
		{
			if (m_progressDlg != null)
			{
				int nMin, nMax;
				m_progressDlg.GetRange(out nMin, out nMax);
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
			}
			e.Cancel = m_fCancelNow;
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
				SetMergeStyle(FlexLiftMerger.MergeStyle.msKeepOld);
			}
		}

		private void m_rbKeepNew_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbKeepNew.Checked)
			{
				m_rbKeepCurrent.Checked = false;
				m_rbKeepBoth.Checked = false;
				SetMergeStyle(FlexLiftMerger.MergeStyle.msKeepNew);
			}
		}

		private void m_rbKeepBoth_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rbKeepBoth.Checked)
			{
				m_rbKeepCurrent.Checked = false;
				m_rbKeepNew.Checked = false;
				SetMergeStyle(FlexLiftMerger.MergeStyle.msKeepBoth);
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
			ShowHelp.ShowHelpTopic(FwApp.App, "khtpImportLIFT");
		}
	}
}