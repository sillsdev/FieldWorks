using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FXT;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.XWorks;

using LiftIO;
using LiftIO.Merging;

namespace SIL.FieldWorks.LexText.FlexChorus
{
	public partial class FlexChorusDlg : Form
	{
		protected FdoCache m_cache;
		protected IAdvInd4 m_progressDlg;

		public FlexChorusDlg(FdoCache cache)
		{
			m_cache = cache;
			InitializeComponent();
		}

		private void m_btnBackup_Click(object sender, EventArgs e)
		{
			DIFwBackupDb backupSystem = FwBackupClass.Create();
			backupSystem.Init(FwApp.App, Handle.ToInt32());
			int nBkResult;
			nBkResult = backupSystem.UserConfigure(FwApp.App, false);
			backupSystem.Close();
		}

		private void m_btnMerge_Click(object sender, EventArgs e)
		{
			using (new SIL.FieldWorks.Common.Utils.WaitCursor(this))
			{
				using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(this))
				{
					progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;
					m_progressDlg = progressDlg as IAdvInd4;
					try
					{
						progressDlg.Title = "Chorus Merge Process";
						// 1. export lexicon
						string outPath = Path.GetTempFileName();
						outPath = (string)progressDlg.RunTask(true, new BackgroundTaskInvoker(ExportLexicon), outPath);
						if (outPath == null)
						{
							// TODO: some sort of error report?
							return;
						}

						// 2. merge via chorus
						string inPath = (string)progressDlg.RunTask(true, new BackgroundTaskInvoker(ChorusMerge), outPath);
						if (inPath == null)
						{
							// TODO: some sort of error report?
							return;
						}
						// 3. re-import lexicon, overwriting current contents.
						string logFile = (string)progressDlg.RunTask(true, new BackgroundTaskInvoker(ImportLexicon), inPath);
						if (logFile == null)
						{
							// TODO: some sort of error report?
							return;
						}
						else
						{
						}
					}
					catch
					{
					}
					finally
					{
					}
				}
			}
			DialogResult = DialogResult.OK;
			Close();
		}

		/// <summary>
		/// Export the contents of the lexicon to the given file (first and only parameter).
		/// </summary>
		/// <returns>the name of the exported LIFT file if successful, or null if an error occurs.</returns>
		protected object ExportLexicon(IAdvInd4 progressDialog, params object[] parameters)
		{
			try
			{
				if (m_progressDlg == null)
					m_progressDlg = progressDialog;
				string outPath = (string)parameters[0];
				progressDialog.Message = String.Format(FlexChorusStrings.ksExportingEntries,
					m_cache.LangProject.LexDbOA.Entries.Count());
				using (XDumper dumper = new XDumper(m_cache))
				{
					dumper.UpdateProgress += new XDumper.ProgressHandler(OnDumperUpdateProgress);
					dumper.SetProgressMessage += new EventHandler<XDumper.MessageArgs>(OnDumperSetProgressMessage);
					// Don't bother writing out the range information in the export.
					dumper.SetTestVariable("SkipRanges", true);
					dumper.SkipAuxFileOutput = true;
					progressDialog.SetRange(0, dumper.GetProgressMaximum());
					progressDialog.Position = 0;
					// TODO: get better output/input filename?
					string p = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\Export Templates");
					string fxtPath = Path.Combine(p, "LIFT.fxt.xml");
					using (TextWriter w = new StreamWriter(outPath))
					{
						dumper.ExportPicturesAndMedia = true;	// useless without Pictures directory...
						dumper.Go(m_cache.LangProject as CmObject, fxtPath, w);
					}
					// TODO: validate output?
					return outPath;
				}
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Do the modifications/merging/whatever to the LIFT file given by the first (and only)
		/// parameter.
		/// </summary>
		/// <returns>the name of the modified file (may be the same as the input), or null if an error occurs.</returns>
		protected object ChorusMerge(IAdvInd4 progressDialog, params object[] parameters)
		{
			// TODO: implement this!
			if (m_progressDlg == null)
				m_progressDlg = progressDialog;
			progressDialog.Message = "Merging via Chorus";
			progressDialog.SetRange(0, 100);
			progressDialog.Position = 0;
			string outPath = parameters[0].ToString();
			progressDialog.Position = 100;
			return outPath;
		}

		/// <summary>
		/// Import the modified LIFT file given by the first (and only) parameter.
		/// </summary>
		/// <returns>the name of the log file for the import, or null if a major error occurs.</returns>
		protected object ImportLexicon(IAdvInd4 progressDialog, params object[] parameters)
		{
			if (m_progressDlg == null)
				m_progressDlg = progressDialog;
			progressDialog.SetRange(0, 100);
			progressDialog.Position = 0;
			string inPath = parameters[0].ToString();
			string sLogFile = null;
			PropChangedHandling oldPropChg = m_cache.PropChangedHandling;
			try
			{
				m_cache.PropChangedHandling = PropChangedHandling.SuppressAll;
				string sFilename;
				bool fMigrationNeeded = LiftIO.Migration.Migrator.IsMigrationNeeded(inPath);
				if (fMigrationNeeded)
				{
					string sOldVersion = LiftIO.Validation.Validator.GetLiftVersion(inPath);
					progressDialog.Message = String.Format("Migrating from LIFT version {0} to version {1}",
						sOldVersion, LiftIO.Validation.Validator.LiftVersion);
					sFilename = LiftIO.Migration.Migrator.MigrateToLatestVersion(inPath);
				}
				else
				{
					sFilename = inPath;
				}
				// TODO: validate input file?
				progressDialog.Message = "Loading various lists for lookup during import";
				FlexLiftMerger flexImporter = new FlexLiftMerger(m_cache, FlexLiftMerger.MergeStyle.msKeepOnlyNew, true);
				LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample> parser =
					new LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>(flexImporter);
				parser.SetTotalNumberSteps += new EventHandler<LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.StepsArgs>(parser_SetTotalNumberSteps);
				parser.SetStepsCompleted += new EventHandler<LiftIO.Parsing.LiftParser<LiftObject,LiftEntry,LiftSense,LiftExample>.ProgressEventArgs>(parser_SetStepsCompleted);
				parser.SetProgressMessage += new EventHandler<LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.MessageArgs>(parser_SetProgressMessage);
				flexImporter.LiftFile = inPath;

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
				progressDialog.Message = "Fixing relation links between imported entries";
				flexImporter.ProcessPendingRelations();
				sLogFile = flexImporter.DisplayNewListItems(inPath, cEntries);
			}
			catch (Exception error)
			{
				string sMsg = String.Format("Something went wrong trying to import {0} while merging...",
					inPath, error.Message);
				try
				{
					StringBuilder bldr = new StringBuilder();
					// leave in English for programmer's sake...
					bldr.AppendFormat("Something went wrong while FieldWorks was attempting to import {0}.",
						inPath);
					bldr.AppendLine();
					bldr.AppendLine(error.Message);
					bldr.AppendLine();
					bldr.AppendLine(error.StackTrace);
					if (System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.STA)
						ClipboardUtils.SetDataObject(bldr.ToString(), true);
				}
				catch
				{
				}
				MessageBox.Show(sMsg, "Problem Merging",
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			finally
			{
				m_cache.PropChangedHandling = oldPropChg;
			}
			return sLogFile;
		}

		void parser_SetTotalNumberSteps(object sender, LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.StepsArgs e)
		{
			m_progressDlg.SetRange(0, e.Steps);
			m_progressDlg.Position = 0;
		}

		void parser_SetProgressMessage(object sender, LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.MessageArgs e)
		{
			m_progressDlg.Position = 0;
			m_progressDlg.Message = e.Message;
		}

		void parser_SetStepsCompleted(object sender, LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.ProgressEventArgs e)
		{
			int nMin, nMax;
			m_progressDlg.GetRange(out nMin, out nMax);
			if (e.Progress > nMax)
				m_progressDlg.Position = e.Progress % nMax;
			else
				m_progressDlg.Position = e.Progress;
		}

		void OnDumperSetProgressMessage(object sender, XDumper.MessageArgs e)
		{
			if (m_progressDlg == null)
				return;
			Debug.WriteLine(String.Format("OnDumperSetProgressMessage(\"{0}\")", e.MessageId));
			string sMsg = FlexChorusStrings.ResourceManager.GetString(e.MessageId, FlexChorusStrings.Culture);
			if (!String.IsNullOrEmpty(sMsg))
				m_progressDlg.Message = sMsg;
			m_progressDlg.SetRange(0, e.Max);
		}

		void OnDumperUpdateProgress(object sender)
		{
			if (m_progressDlg == null)
				return;
			int nMin, nMax;
			m_progressDlg.GetRange(out nMin, out nMax);
			if (m_progressDlg.Position >= nMax)
				m_progressDlg.Position = 0;
			m_progressDlg.Step(1);
			if (m_progressDlg.Position > nMax)
				m_progressDlg.Position = m_progressDlg.Position % nMax;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			MessageBox.Show("No help is yet available", "Chorus Merge");
		}
	}
}
