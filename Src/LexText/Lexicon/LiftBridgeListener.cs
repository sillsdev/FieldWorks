using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using LiftBridgeCore;
using Palaso.Lift.Migration;
using Palaso.Lift.Parsing;
using Palaso.Xml;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
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
			_mediator.PropertyTable.SetProperty("LiftBridgeListener", this);
			_mediator.PropertyTable.SetPropertyPersistence("LiftBridgeListener", false);
			_cache = (FdoCache)_mediator.PropertyTable.GetValue("cache");
			_databaseName = _cache.ProjectId.Name;
			_parentForm = (Form)_mediator.PropertyTable.GetValue("window");
			_mediator.AddColleague(this);
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] { this };
		}

		public bool ShouldNotCall
		{
			get { return false; }
		}

		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
		}

		#endregion

		#region XCore message handlers

		/// <summary>
		/// Called (by xcore) to control display params of the Lift Send/Receive menu.
		/// </summary>
		public bool OnDisplayLiftBridge(object commandObject, ref UIItemDisplayProperties display)
		{
			// LT-11922 & LT-12053 show that we need the CodeDirectory here and it's better
			// to use the FileUtils version of FileExists for cross-platform reasons.
			var fullDllName = FullLiftBridgeDllPath();
			display.Enabled = FileUtils.FileExists(fullDllName);
			display.Visible = display.Enabled;

			return true; // We dealt with it.
		}

		private static string FullLiftBridgeDllPath()
		{
			return Path.Combine(DirectoryFinder.FWCodeDirectory, LiftBridgeDll);
		}

		/// <summary>
		/// Called (by xcore) to control display params of the Lift Send/Receive menu.
		/// </summary>
		public bool OnLiftBridge(object argument)
		{
			m_fRefreshNeeded = false;
			var assembly = Assembly.LoadFrom(FullLiftBridgeDllPath());
			var lbType = (assembly.GetTypes().Where(typeof(ILiftBridge).IsAssignableFrom)).First();
			var constInfo = lbType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
			using (var liftBridge = (ILiftBridge)constInfo.Invoke(BindingFlags.NonPublic, null, null, null))
			{
				liftBridge.BasicLexiconImport += LiftBridgeBasicLexiconImport;
				liftBridge.ImportLexicon += LiftBridgeImportLexicon;
				liftBridge.ExportLexicon += LiftBridgeExportLexicon;

				try
				{
					// Send LangProj Guid to Lift Bridge.
					var liftBridgeAsNewInterface = liftBridge as ILiftBridge3;
					if (liftBridgeAsNewInterface != null)
						liftBridgeAsNewInterface.LanguageProjectGuid = _cache.LanguageProject.Guid;

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

		void OnDumperSetProgressMessage(object sender, ProgressMessageArgs e)
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
				progressDialog.Minimum = 0;
				progressDialog.Maximum = _cache.ServiceLocator.GetInstance<ILexEntryRepository>().Count;
				progressDialog.Position = 0;

				var exporter = new LiftExporter(_cache);
				exporter.UpdateProgress += OnDumperUpdateProgress;
				exporter.SetProgressMessage += OnDumperSetProgressMessage;
				exporter.ExportPicturesAndMedia = true;
				using (TextWriter w = new StreamWriter(outPath))
				{
					exporter.ExportLift(w, Path.GetDirectoryName(outPath));
				}


				//Output the Ranges file
				//NOTE (RickM): outPath is passed in with the following kind of format with the .tmp at the end.
				//Because changing the service to add another parameter would be an interface breaking change
				//we will continue to hack our way around here but Assert that .lift.tmp will always be the file name.
				//For an outPath = "C:\\Users\\maclean\\AppData\\Local\\LiftBridge\\FolderName\\FileName.lift.tmp"
				//ranges should be "C:\\Users\\maclean\\AppData\\Local\\LiftBridge\\FolderName\\FileName.lift-ranges"
				Debug.Assert(outPath.EndsWith(@".lift.tmp"), @"Unexpected argument format from LiftBridge.");
				if(!outPath.EndsWith(@".lift.tmp"))
					return null; //The liftbridge behavior has changed, we need to change also.
				var pathWithFilename = outPath.Substring(0, outPath.Length - @".lift.tmp".Length);
				var outPathRanges = Path.ChangeExtension(pathWithFilename, @"lift-ranges");
				var stringWriter = new StringWriter(new StringBuilder());
				exporter.ExportLiftRanges(stringWriter);
				using (var xmlWriter = XmlWriter.Create(outPathRanges, CanonicalXmlSettings.CreateXmlWriterSettings()))
				{
					var doc = new XmlDocument();
					doc.LoadXml(stringWriter.ToString());
					doc.WriteContentTo(xmlWriter);
				}
				// At least for now, we won't bother with validation for LiftBridge.
				//progressDialog.Message = String.Format("Validating LIFT file {0}.",
				//        Path.GetFileName(outPath));
				//var prog = new ValidationProgress(progressDialog);
				//Palaso.Lift.Validation.Validator.CheckLiftWithPossibleThrow(outPath, prog);
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
			// If we use true while importing changes from repo it will fail to copy any pix/aud files that have changed.
			var fTrustModTimes = mergeStyle == FlexLiftMerger.MergeStyle.MsKeepOnlyNew ? false : true;
			if (_progressDlg == null)
				_progressDlg = progressDialog;
			progressDialog.Minimum = 0;
			progressDialog.Maximum = 100;
			progressDialog.Position = 0;
			string sLogFile = null;

			NonUndoableUnitOfWorkHelper.Do(_cache.ActionHandlerAccessor, () =>
			{
					string sFilename;
					var fMigrationNeeded = Migrator.IsMigrationNeeded(liftPathname);
					if (fMigrationNeeded)
					{
						var sOldVersion = Palaso.Lift.Validation.Validator.GetLiftVersion(liftPathname);
						progressDialog.Message = String.Format(ResourceHelper.GetResourceString("kstidLiftVersionMigration"),
							sOldVersion, Palaso.Lift.Validation.Validator.LiftVersion);
						sFilename = Migrator.MigrateToLatestVersion(liftPathname);
					}
					else
					{
						sFilename = liftPathname;
					}
					progressDialog.Message = ResourceHelper.GetResourceString("kstidLoadingListInfo");
					var flexImporter = new FlexLiftMerger(_cache, mergeStyle, fTrustModTimes);
					var parser = new LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>(flexImporter);
					parser.SetTotalNumberSteps += ParserSetTotalNumberSteps;
					parser.SetStepsCompleted += ParserSetStepsCompleted;
					parser.SetProgressMessage += ParserSetProgressMessage;
					flexImporter.LiftFile = liftPathname;

					flexImporter.LoadLiftRanges(liftPathname + "-ranges");
					var cEntries = parser.ReadLiftFile(sFilename);

					if (fMigrationNeeded)
					{
						// Try to move the migrated file to the temp directory, even if a copy of it
						// already exists there.
						var sTempMigrated = Path.Combine(Path.GetTempPath(),
														 Path.ChangeExtension(Path.GetFileName(sFilename), "." + Palaso.Lift.Validation.Validator.LiftVersion + ".lift"));
						if (File.Exists(sTempMigrated))
							File.Delete(sTempMigrated);
						File.Move(sFilename, sTempMigrated);
					}
					progressDialog.Message = ResourceHelper.GetResourceString("kstidFixingRelationLinks");
					flexImporter.ProcessPendingRelations(progressDialog);
					sLogFile = flexImporter.DisplayNewListItems(liftPathname, cEntries);
			});
			m_fRefreshNeeded = true;
			return sLogFile;
		}

		void ParserSetTotalNumberSteps(object sender, LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.StepsArgs e)
		{
			_progressDlg.Maximum = e.Steps;
			_progressDlg.Position = 0;
		}

		void ParserSetProgressMessage(object sender, LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MessageArgs e)
		{
			_progressDlg.Position = 0;
			_progressDlg.Message = e.Message;
		}

		void ParserSetStepsCompleted(object sender, LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.ProgressEventArgs e)
		{
			var nMax = _progressDlg.Maximum;
			_progressDlg.Position = e.Progress > nMax ? e.Progress % nMax : e.Progress;
		}

		private bool ImportCommon(FlexLiftMerger.MergeStyle mergeStyle)
		{
			using (new WaitCursor(_parentForm))
			{
				using (var progressDlg = new ProgressDialogWithTask(_parentForm, _cache.ThreadHelper))
				{
					_progressDlg = progressDlg;
					progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;
					try
					{
						progressDlg.Title = ResourceHelper.GetResourceString("kstidImportLiftlexicon");
						var logFile = (string) progressDlg.RunTask(true, ImportLexicon, new object[] {_liftPathname, mergeStyle});
						return logFile != null;
					}
					catch (WorkerThreadException error)
					{
						// It appears to be an analyst issue to sort out how we should report this.
						// LT-12340 however says we must report it somehow.
						var sMsg = String.Format(LexEdStrings.kProblemImportWhileMerging, _liftPathname, error.InnerException.Message);
						// RandyR says JohnH isn't excited about this approach to reporting an import error, that is, copy it to the
						// clipboard (and presumably say something about it in kProblemImportWhileMerging).
						// But it would be nice to get the details if it is a crash.
						//try
						//{
						//    var bldr = new StringBuilder();
						//    bldr.AppendFormat(Resources.kProblem, _liftPathname);
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
						MessageBox.Show(sMsg, LexEdStrings.kProblemMerging,
							MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
				using (var progressDlg = new ProgressDialogWithTask(_parentForm, _cache.ThreadHelper))
				{
					_progressDlg = progressDlg;
					progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;
					try
					{
						progressDlg.Title = ResourceHelper.GetResourceString("kstidExportLiftLexicon");
						var outPath = (string)progressDlg.RunTask(true, ExportLexicon, _liftPathname);
						return (!String.IsNullOrEmpty(outPath));
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
