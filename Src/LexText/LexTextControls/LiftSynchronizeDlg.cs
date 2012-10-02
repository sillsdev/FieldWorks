using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FXT;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using XCore;
using System.IO;
using System.Diagnostics;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class LiftSynchronizeDlg : Form, IFWDisposable
	{
		FdoCache m_cache;
		Mediator m_mediator;
		private XDumper m_dumper;
		private ProgressDialogWithTask m_progressDlg;
		bool m_fTempFiles = false;

		public LiftSynchronizeDlg(FdoCache cache, Mediator mediator)
		{
			InitializeComponent();

			m_tbSynchSource.Enabled = false;
			m_btnBrowse2.Enabled = false;

			m_cache = cache;
			m_mediator = mediator;
		}

		private void m_btnBrowse_Click(object sender, EventArgs e)
		{
			m_saveFileDialog.CheckPathExists = true;
			m_saveFileDialog.ValidateNames = true;
			m_saveFileDialog.Filter = ResourceHelper.BuildFileFilter(new FileFilterType[] {
				FileFilterType.LIFT, FileFilterType.XML, FileFilterType.AllFiles});
			m_saveFileDialog.FilterIndex = 1;
			m_saveFileDialog.FileName = m_tbLiftFile.Text;
			if (m_saveFileDialog.ShowDialog(this) == DialogResult.OK)
				m_tbLiftFile.Text = m_saveFileDialog.FileName;
		}

		private void m_btnSynch_Click(object sender, EventArgs e)
		{
			Run();
			this.Close();
		}

		private void m_btnBrowse2_Click(object sender, EventArgs e)
		{
			MessageBox.Show("THIS IS NOT YET IMPLEMENTED.", "PLEASE BE PATIENT");
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			MessageBox.Show("SORRY, NO HELP YET!");
		}

		private void Run()
		{
			string outPath = GetOutputPathname();
			try
			{
				// 1. Export or update external lift file.
				ExportLiftData(outPath);
				// 2. Run Chorus (just run a stub for now until Chorus is real) which will
				//    a. talk to the user if there is no version control system, asking where
				//       the file to merge is, else just check-in the work, grab work from
				//       teammates, and then
				//    b. do the smart merge
				SynchronizeWithExternalSource();
				// 3. Import the lift file, wiping out anything that differs from it
				ImportNewLiftData(outPath);
			}
			catch (WorkerThreadException e)
			{
				if (e.InnerException is CancelException)
				{
					MessageBox.Show(e.InnerException.Message);
					return;
				}
				else if (m_dumper != null)
				{
					MessageBox.Show("error exporting");
				}
			}
			finally
			{
				m_dumper = null;
				m_progressDlg = null;
				if (m_fTempFiles && File.Exists(outPath))
				{
					File.Delete(outPath);
					string x = Path.ChangeExtension(outPath, "lift-ranges");
					if (File.Exists(x))
						File.Delete(x);
					string y = x.Replace(".lift-ranges", "-ImportLog.htm");
					if (File.Exists(y))
						File.Delete(y);
				}
			}

		}

		private void SynchronizeWithExternalSource()
		{
			MessageBox.Show("THIS IS NOT YET IMPLEMENTED.", "PLEASE BE PATIENT");
		}

		private string GetOutputPathname()
		{
			string outPath = m_tbLiftFile.Text.Trim();
			if (String.IsNullOrEmpty(outPath))
			{
				m_fTempFiles = true;
				return Path.GetTempFileName();
			}
			else
			{
				return outPath;
			}
		}

		private void ExportLiftData(string outPath)
		{
			// TODO: if file exists, read it to obtain the list of entries it contains, including
			// deleted entries.  Then use this information in exporting to identify deleted entries
			// in the LIFT file.  This will require something other than FXT to export LIFT files.
			using (m_progressDlg = new ProgressDialogWithTask(this))
			{
				m_progressDlg.CancelButtonVisible = true;
				m_progressDlg.Restartable = true;
				m_progressDlg.Cancel += new CancelHandler(OnProgressDlgCancel);
				m_progressDlg.Message = "Exporting data in preparation for synchronization";
				using (m_dumper = new SIL.FieldWorks.Common.FXT.XDumper(m_cache))
				{
					m_dumper.UpdateProgress += new XDumper.ProgressHandler(OnDumperUpdateProgress);
					m_progressDlg.SetRange(0, m_dumper.GetProgressMaximum());
					m_progressDlg.RunTask(true, new BackgroundTaskInvoker(ExportLift), outPath);
				}
			}
			m_progressDlg = null;
		}

		private void ImportNewLiftData(string outPath)
		{
			using (m_progressDlg = new ProgressDialogWithTask(this))
			{
				m_progressDlg.CancelButtonVisible = true;
				m_progressDlg.Restartable = true;
				m_progressDlg.Cancel += new CancelHandler(OnProgressDlgCancel);
				m_progressDlg.RunTask(true, new BackgroundTaskInvoker(ImportLift), outPath);
			}
			m_progressDlg = null;
		}

		void OnProgressDlgCancel(object sender)
		{
			if (m_dumper != null)
				m_dumper.Cancel();
		}

		void OnDumperUpdateProgress(object sender)
		{
			Debug.Assert(m_progressDlg != null);
			m_progressDlg.Step(0);
		}

		private object ExportLift(IAdvInd4 progressDialog, params object[] parameters)
		{
			Debug.Assert(parameters.Length == 1);
			string p = Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.FWCodeDirectory,
				@"Language Explorer\Export Templates");
			Debug.Assert(Directory.Exists(p));
			string fxtPath = Path.Combine(p, "LIFT.fxt.xml");
			using (TextWriter w = new StreamWriter((string)parameters[0]))
			{
				m_dumper.Go(m_cache.LangProject as CmObject, fxtPath, w);
			}
			return null;
		}

		private object ImportLift(IAdvInd4 progressDlg, params object[] parameters)
		{
			Debug.Assert(parameters.Length == 1);
			m_progressDlg.Message = LexTextControls.ksLoadingVariousLists;
			m_progressDlg.Value = 0;
			FlexLiftMerger flexImporter = new FlexLiftMerger(m_cache, FlexLiftMerger.MergeStyle.msKeepNew, true);
			LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample> parser =
				new LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>(flexImporter);
			parser.SetTotalNumberSteps += new EventHandler<LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.StepsArgs>(parser_SetTotalNumberSteps);
			parser.SetStepsCompleted += new EventHandler<LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.ProgressEventArgs>(parser_SetStepsCompleted);
			parser.SetProgressMessage += new EventHandler<LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.MessageArgs>(parser_SetProgressMessage);
			flexImporter.LiftFile = (string)parameters[0];
			int cEntries = parser.ReadLiftFile((string)parameters[0]);
			m_progressDlg.Message = LexTextControls.ksFixingRelationLinks;
			flexImporter.ProcessPendingRelations();
			return flexImporter.DisplayNewListItems((string)parameters[0], cEntries);
		}

		void parser_SetProgressMessage(object sender, LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.MessageArgs e)
		{
			if (m_progressDlg != null)
				m_progressDlg.Message = e.Message;
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
		}

		void parser_SetTotalNumberSteps(object sender, LiftIO.Parsing.LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.StepsArgs e)
		{
			if (m_progressDlg != null)
				m_progressDlg.SetRange(0, e.Steps);
		}

		#region IFWDisposable Members

		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion
	}
}
