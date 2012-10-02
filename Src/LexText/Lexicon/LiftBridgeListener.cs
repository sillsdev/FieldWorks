using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using LiftBridgeCore;
using LiftIO.Migration;
using LiftIO.Parsing;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.FXT;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	public class LiftBridgeListener : IxCoreColleague
	{
		private const string LiftBridgeDll = "LiftBridge.dll";
		private Mediator _mediator;
		private string _databaseName;
		private Form _parentForm;
		private FdoCache _cache;
		private IProgress _progressDlg;
		private string _liftPathname;
		private bool m_fRefreshNeeded;

		#region Implementation of IxCoreColleague

		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			_mediator = mediator;
			_mediator.AddColleague(this);
			_mediator.PropertyTable.SetProperty("LiftBridgeListener", this);
			_mediator.PropertyTable.SetPropertyPersistence("LiftBridgeListener", false);
			_cache = (FdoCache)_mediator.PropertyTable.GetValue("cache");
			_databaseName = _cache.ProjectId.Name;
			_parentForm = (Form)_mediator.PropertyTable.GetValue("window");
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] { this };
		}

		public bool ShouldNotCall
		{
			get { return false; }
		}

		#endregion

		#region XCore message handlers

		/// <summary>
		/// Called (by xcore) to control display params of the Lift Send/Receive menu.
		/// </summary>
		public bool OnDisplayLiftBridge(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = File.Exists(LiftBridgeDll);
			display.Visible = display.Enabled;

			return true; // We dealt with it.
		}

		/// <summary>
		/// Called (by xcore) to control display params of the Lift Send/Receive menu.
		/// </summary>
		public bool OnLiftBridge(object argument)
		{
			m_fRefreshNeeded = false;
			var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var assembly = Assembly.LoadFrom(Path.Combine(baseDir, LiftBridgeDll));
			var lbType = (assembly.GetTypes().Where(typeof(ILiftBridge).IsAssignableFrom)).First();
			var constInfo = lbType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
			using (var liftBridge = (ILiftBridge)constInfo.Invoke(BindingFlags.NonPublic, null, null, null))
			{
				liftBridge.BasicLexiconImport += LiftBridgeBasicLexiconImport;
				liftBridge.ImportLexicon += LiftBridgeImportLexicon;
				liftBridge.ExportLexicon += LiftBridgeExportLexicon;

				try
				{
					liftBridge.DoSendReceiveForLanguageProject(_parentForm, _databaseName);
				}
				finally
				{
					liftBridge.BasicLexiconImport -= LiftBridgeBasicLexiconImport;
					liftBridge.ImportLexicon -= LiftBridgeImportLexicon;
					liftBridge.ExportLexicon -= LiftBridgeExportLexicon;
				}
			}
			if (m_fRefreshNeeded)
				_mediator.BroadcastMessage("MasterRefresh", null);
			return true; // We dealt with it.
		}

		#endregion XCore message handlers

		#region Event handlers from ILiftBridge

		void LiftBridgeExportLexicon(object sender, LiftBridgeEventArgs e)
		{
			_liftPathname = e.LiftPathname;
			e.Cancel = !ExportLexicon();
		}

		void LiftBridgeImportLexicon(object sender, LiftBridgeEventArgs e)
		{
			_liftPathname = e.LiftPathname;
			e.Cancel = !ImportCommon(FlexLiftMerger.MergeStyle.MsKeepOnlyNew);
		}

		void LiftBridgeBasicLexiconImport(object sender, LiftBridgeEventArgs e)
		{
			_liftPathname = e.LiftPathname;
			e.Cancel = !ImportCommon(FlexLiftMerger.MergeStyle.MsKeepBoth);
		}

		#endregion Event handlers from ILiftBridge

		#region Helper methods for ILiftBridge event handlers

		void OnDumperSetProgressMessage(object sender, XDumper.MessageArgs e)
		{
			if (_progressDlg == null)
				return;
			var message = ResourceHelper.GetResourceString(e.MessageId);
			if (!string.IsNullOrEmpty(message))
				_progressDlg.Message = message;
			_progressDlg.Minimum = 0;
			_progressDlg.Maximum = e.Max;
		}

		void OnDumperUpdateProgress(object sender)
		{
			if (_progressDlg == null)
				return;

			var nMax = _progressDlg.Maximum;
			if (_progressDlg.Position >= nMax)
				_progressDlg.Position = 0;
			_progressDlg.Step(1);
			if (_progressDlg.Position > nMax)
				_progressDlg.Position = _progressDlg.Position % nMax;
		}

		/// <summary>
		/// Export the contents of the lexicon to the given file (first and only parameter).
		/// </summary>
		/// <returns>the name of the exported LIFT file if successful, or null if an error occurs.</returns>
		private object ExportLexicon(IProgress progressDialog, params object[] parameters)
		{
			try
			{
				var outPath = (string)parameters[0];
				progressDialog.Message = String.Format(ResourceHelper.GetResourceString("kstidExportingEntries"),
					_cache.LangProject.LexDbOA.Entries.Count());
				var dumper = new XDumper(_cache);
				dumper.UpdateProgress += OnDumperUpdateProgress;
				dumper.SetProgressMessage += OnDumperSetProgressMessage;
				// Don't bother writing out the range information in the export.
				dumper.SetTestVariable("SkipRanges", true);
				dumper.SkipAuxFileOutput = true;
				progressDialog.Minimum = 0;
				progressDialog.Maximum = dumper.GetProgressMaximum();
				progressDialog.Position = 0;
				var basePath = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\Export Templates");
				var fxtPath = Path.Combine(basePath, "LIFT.fxt.xml");
				using (TextWriter w = new StreamWriter(outPath))
				{
					dumper.ExportPicturesAndMedia = true;	// useless without Pictures directory...
					dumper.Go(_cache.LangProject, fxtPath, w);
				}
				return outPath;
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Import the LIFT file into FieldWorks.
		/// </summary>
		/// <returns>the name of the exported LIFT file if successful, or null if an error occurs.</returns>
		private object ImportLexicon(IProgress progressDialog, params object[] parameters)
		{
			var liftPathname = parameters[0].ToString();
			var mergeStyle = (FlexLiftMerger.MergeStyle)parameters[1];
			if (_progressDlg == null)
				_progressDlg = progressDialog;
			progressDialog.Minimum = 0;
			progressDialog.Maximum = 100;
			progressDialog.Position = 0;
			string sLogFile = null;

			NonUndoableUnitOfWorkHelper.Do(_cache.ActionHandlerAccessor, () =>
			{
				try
				{
					string sFilename;
					var fMigrationNeeded = Migrator.IsMigrationNeeded(liftPathname);
					if (fMigrationNeeded)
					{
						var sOldVersion = LiftIO.Validation.Validator.GetLiftVersion(liftPathname);
						progressDialog.Message = String.Format(ResourceHelper.GetResourceString("kstidLiftVersionMigration"),
															   sOldVersion, LiftIO.Validation.Validator.LiftVersion);
						sFilename = Migrator.MigrateToLatestVersion(liftPathname);
					}
					else
					{
						sFilename = liftPathname;
					}
					progressDialog.Message = ResourceHelper.GetResourceString("kstidLoadingListInfo");
					var flexImporter = new FlexLiftMerger(_cache, mergeStyle, true);
					var parser = new LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>(flexImporter);
					parser.SetTotalNumberSteps += ParserSetTotalNumberSteps;
					parser.SetStepsCompleted += ParserSetStepsCompleted;
					parser.SetProgressMessage += ParserSetProgressMessage;
					flexImporter.LiftFile = liftPathname;
					var cEntries = parser.ReadLiftFile(sFilename);

					if (fMigrationNeeded)
					{
						// Try to move the migrated file to the temp directory, even if a copy of it
						// already exists there.
						var sTempMigrated = Path.Combine(Path.GetTempPath(),
														 Path.ChangeExtension(Path.GetFileName(sFilename), "." + LiftIO.Validation.Validator.LiftVersion + ".lift"));
						if (File.Exists(sTempMigrated))
							File.Delete(sTempMigrated);
						File.Move(sFilename, sTempMigrated);
					}
					progressDialog.Message = ResourceHelper.GetResourceString("kstidFixingRelationLinks");
					flexImporter.ProcessPendingRelations(progressDialog);
					sLogFile = flexImporter.DisplayNewListItems(liftPathname, cEntries);
				}
				catch (Exception error)
				{
					// TODO: SteveMc (RandyR): JohnH isn't excited about this approach to reporting an import error. It appears to be an analyst issue to sort out.
					//var sMsg = String.Format(Resources.kProblemImportWhileMerging,
					//                         liftPathname);
					//try
					//{
					//    var bldr = new StringBuilder();
					//    bldr.AppendFormat(Resources.kProblem, liftPathname);
					//    bldr.AppendLine();
					//    bldr.AppendLine(error.Message);
					//    bldr.AppendLine();
					//    bldr.AppendLine(error.StackTrace);
					//    if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
					//        ClipboardUtils.SetDataObject(bldr.ToString(), true);
					//}
					//catch
					//{
					//}
					//MessageBox.Show(sMsg, Resources.kProblemMerging,
					//                MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			});
			m_fRefreshNeeded = true;
			return sLogFile;
		}

		void ParserSetTotalNumberSteps(object sender, LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.StepsArgs e)
		{
			_progressDlg.Maximum = e.Steps;
			_progressDlg.Position = 0;
		}

		void ParserSetProgressMessage(object sender, LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.MessageArgs e)
		{
			_progressDlg.Position = 0;
			_progressDlg.Message = e.Message;
		}

		void ParserSetStepsCompleted(object sender, LiftParser<LiftObject, LiftEntry, LiftSense, LiftExample>.ProgressEventArgs e)
		{
			var nMax = _progressDlg.Maximum;
			_progressDlg.Position = e.Progress > nMax ? e.Progress % nMax : e.Progress;
		}

		private bool ImportCommon(FlexLiftMerger.MergeStyle mergeStyle)
		{
			using (new WaitCursor(_parentForm))
			{
				using (var progressDlg = new ProgressDialogWithTask(_parentForm))
				{
					_progressDlg = progressDlg;
					progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;
					try
					{
						progressDlg.Title = ResourceHelper.GetResourceString("kstidImportLiftlexicon");
						var logFile = (string)progressDlg.RunTask(true, ImportLexicon, new object[] { _liftPathname, mergeStyle });
						return logFile != null;
					}
					catch
					{
						return false;
					}
					finally
					{
						_progressDlg = null;
					}
				}
			}
		}

		/// <summary>
		/// Export the FieldWorks lexicon into the LIFT file.
		/// The file may, or may not, exist.
		/// </summary>
		/// <returns>True, if successful, otherwise false.</returns>
		public bool ExportLexicon()
		{
			using (new WaitCursor(_parentForm))
			{
				using (var progressDlg = new ProgressDialogWithTask(_parentForm))
				{
					_progressDlg = progressDlg;
					progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;
					try
					{
						progressDlg.Title = ResourceHelper.GetResourceString("kstidExportLiftLexicon");
						var outPath = Path.GetTempFileName();
						outPath = (string)progressDlg.RunTask(true, ExportLexicon, outPath);
						if (outPath == null)
							return false;

						// Copy temp file to real LIFT file.
						File.Copy(outPath, _liftPathname, true);
						File.Delete(outPath); // Delete temp file.
						return true;
					}
					catch
					{
						return false;
					}
					finally
					{
						_progressDlg = null;
					}
				}
			}
		}

		#endregion Helper methods for ILiftBridge event handlers
	}
}