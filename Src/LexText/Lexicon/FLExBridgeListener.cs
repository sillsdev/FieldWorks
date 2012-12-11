using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Palaso.Lift.Migration;
using Palaso.Lift.Parsing;
using Palaso.Xml;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FixData;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.XWorks.LexText;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule",
		Justification="_mediator is a reference")]
	class FLExBridgeListener : IxCoreColleague, IFWDisposable
	{
		private Mediator _mediator;
		private Form _parentForm;
		private string _liftPathname;
		private IProgress _progressDlg;
		private FdoCache Cache { get; set; }

		#region IxCoreColleague Implementation

		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] { this };
		}

		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			CheckDisposed();

			_mediator = mediator;
			Cache = (FdoCache)_mediator.PropertyTable.GetValue("cache");
			_mediator.PropertyTable.SetProperty("FLExBridgeListener", this);
			_mediator.PropertyTable.SetPropertyPersistence("FLExBridgeListener", false);
			_parentForm = (Form)_mediator.PropertyTable.GetValue("window");
			mediator.AddColleague(this);
		}

		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
		}

		public bool ShouldNotCall
		{
			get { return false; }
		}

		#endregion

		#region XCore message handlers

		#region FLExBridge (proper) messages

		/// <summary>
		/// Determine whether or not to show the Send/Receive Project menu item.
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayFLExBridge(object parameters, ref UIItemDisplayProperties display)
		{
			CheckForFlexBridgeInstalled(display);

			return true; // We dealt with it.
		}

		/// <summary>
		/// The method/delegate that gets invoked when File->Send/Receive Project is clicked
		/// via the OnFLExBridge message
		/// </summary>
		/// <param name="commandObject">Includes the XML command element of the OnFLExBridge message</param>
		/// <returns>true if the message was handled, false if there was an error or the call was deemed inappropriate.</returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "newApp is a reference")]
		public bool OnFLExBridge(object commandObject)
		{
			if (IsDb4oProject)
			{
				var dlg = new Db4oSendReceiveDialog();
				if (dlg.ShowDialog() == DialogResult.Abort)
				{
					// User clicked on link
					_mediator.SendMessage("FileProjectSharingLocation", null);
				}
				return true;
			}

			if (ChangeProjectNameIfNeeded())
				return true;

			string url;
			var projectFolder = Cache.ProjectId.ProjectFolder;
			var savedState = PrepareToDetectConflicts(projectFolder);
			string dummy;
			var fullProjectFileName = Path.Combine(projectFolder, Cache.ProjectId.Name + ".fwdata");
			bool dataChanged;
			var success = FLExBridgeHelper.LaunchFieldworksBridge(fullProjectFileName, Environment.UserName,
								FLExBridgeHelper.SendReceive, out dataChanged, out dummy);
			if (!success)
			{
				ReportDuplicateBridge();
				ProjectLockingService.LockCurrentProject(Cache);
				return true;
			}

			if (dataChanged)
			{
				var fixer = new FwDataFixer(Cache.ProjectId.Path, new StatusBarProgressHandler(null, null), logger);
				fixer.FixErrorsAndSave();
				bool conflictOccurred = DetectConflicts(projectFolder, savedState);
				var app = (LexTextApp)_mediator.PropertyTable.GetValue("App");
				var newAppWindow = RefreshCacheWindowAndAll(app, fullProjectFileName);

				if (conflictOccurred)
				{
					//send a message for the reopened instance to display the conflict report, we have been disposed by now
					newAppWindow.Mediator.SendMessage("ShowConflictReport", null);
				}
			}
			else //Re-lock project if we aren't trying to close the app
			{
				ProjectLockingService.LockCurrentProject(Cache);
			}
			return true;
		}

		#endregion FLExBridge (proper) messages

		#region LiftBridge messages

		/// <summary>
		/// Called (by xcore) to control display params of the Lift Send/Receive menu.
		/// </summary>
		public bool OnDisplayLiftBridge(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckForFlexBridgeInstalled(display);

			return true; // We dealt with it.
		}

		/// <summary>
		/// Called (by xcore) to control display params of the Lift Send/Receive menu.
		/// </summary>
		public bool OnLiftBridge(object argument)
		{
			// Step 1. If notifier exists, re-try import (brutal or merciful, depending on contents of it).
			if (RepeatPriorFailedImportIfNeeded())
				return true;

			// Step 2. Export lift file. If fails, then call into bridge with undo_export_lift and quit.
			if (!ExportLiftLexicon())
				return true;


			// Step 3. Have Flex Bridge do the S/R.
			bool dataChanged;
			if (!DoSendReceiveForLift(out dataChanged))
				return true; // Bail out, since the S/R failed for some reason.

			// Step 4. Import lift file. If fails, then add the notifier file.
			if (!DoMercilessLiftImport(dataChanged))
				return true;

			if (dataChanged)
				_mediator.BroadcastMessage("MasterRefresh", null);

			return true; // We dealt with it.
		}

		#endregion LiftBridge messages

		#region ShowConflictReport (for full FLEx data only) messages

		/// <summary>
		/// Determine whether or not to display the View Conflict Report menu item.
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayShowConflictReport(object parameters, ref UIItemDisplayProperties display)
		{
			if (CheckForFlexBridgeInstalled(display))
			{
				display.Enabled = NotesFileIsPresent(Cache);
				display.Visible = display.Enabled;
			}

			return true;
		}

		/// <summary>
		/// The method/delegate that gets invoked when View->Conflict Report is clicked
		/// via the OnShowConflictReport message
		/// </summary>
		/// <param name="commandObject">Includes the XML command element of the OnShowConflictReport message</param>
		/// <returns>true if the message was handled, false if there was an error or the call was deemed inappropriate.</returns>
		public bool OnShowConflictReport(object commandObject)
		{
			if (IsDb4oProject)
			{
				var dlg = new Db4oSendReceiveDialog();
				if (dlg.ShowDialog() == DialogResult.Abort)
				{
					// User clicked on link
					_mediator.SendMessage("FileProjectSharingLocation", null);
				}
				return true;
			}
			bool dummy1;
			string dummy2;
			FLExBridgeHelper.FLExJumpUrlChanged += JumpToFlexObject;
			var success = FLExBridgeHelper.LaunchFieldworksBridge(Path.Combine(Cache.ProjectId.ProjectFolder, Cache.ProjectId.Name + ".fwdata"),
								   Environment.UserName,
								   FLExBridgeHelper.ConflictViewer, out dummy1, out dummy2);
			if (!success)
			{
				FLExBridgeHelper.FLExJumpUrlChanged -= JumpToFlexObject;
				ReportDuplicateBridge();
			}
			return true;
		}

		#endregion ShowConflictReport (for full FLEx data only) messages

		#endregion

		#region Lift methods

		/// <summary>
		/// Reregisters an inport failure, if needed, otherwise clears the token.
		/// </summary>
		/// <returns>'true' if the import failure continues, otherwise 'false'.</returns>
		private bool RepeatPriorFailedImportIfNeeded()
		{
			var projectFolder = Cache.ProjectId.ProjectFolder;
			var liftProjectDir = GetLiftRepositoryFolderFromFwProjectFolder(projectFolder);
			if (!Directory.Exists(liftProjectDir))
				return false;
			_liftPathname = GetLiftPathname(liftProjectDir);
			var previousImportStatus = LiftImportFailureServices.GetFailureStatus(liftProjectDir);
			switch (previousImportStatus)
			{
				case ImportFailureStatus.BasicImportNeeded:
					if (ImportLiftCommon(FlexLiftMerger.MergeStyle.MsKeepBoth))
						LiftImportFailureServices.ClearImportFailure(_liftPathname);
					else
					{
						LiftImportFailureServices.RegisterBasicImportFailure(_parentForm, liftProjectDir);
						return true;
					}
					break;
				case ImportFailureStatus.StandardImportNeeded:
					if (ImportLiftCommon(FlexLiftMerger.MergeStyle.MsKeepOnlyNew))
						LiftImportFailureServices.ClearImportFailure(_liftPathname);
					else
					{
						LiftImportFailureServices.RegisterStandardImportFailure(_parentForm, liftProjectDir);
						return true;
					}
					break;
				case ImportFailureStatus.NoImportNeeded:
					// Nothing to do. :-)
					break;
			}
			return false;
		}

		private bool DoMercilessLiftImport(bool dataChanged)
		{
			var projectFolder = Cache.ProjectId.ProjectFolder;
			var liftProjectDir = GetLiftRepositoryFolderFromFwProjectFolder(projectFolder);
			if (dataChanged)
			{
				if (ImportLiftCommon(FlexLiftMerger.MergeStyle.MsKeepOnlyNew))
				{
					LiftImportFailureServices.ClearImportFailure(_liftPathname);
				}
				else
				{
					LiftImportFailureServices.RegisterStandardImportFailure(_parentForm, liftProjectDir);
					return false;
				}

				/* TODO: What to do with Lift conflicts?
								var conflictOccurred = DetectConflicts(projectFolder, savedState);
								var app = (LexTextApp)m_mediator.PropertyTable.GetValue("App");
								var newAppWindow = RefreshCacheWindowAndAll(app, m_liftPathname);

								if (conflictOccurred)
								{
									// TODO: send a message for the reopened instance to display the conflict report, we have been disposed by now
									// TODO: Need a new message for Lift conflicts.
									// TODO: Even more importantly, the URLs in the lift notes files aren't compatible with what comes in for regular FW conflict reports
									//newAppWindow.Mediator.SendMessage("ShowConflictReport", null);
								}
				*/
			}

			return true;
		}

		/// <summary>
		/// Do the S/R. This *may* actually create the Lift repository, if it doesn't exist, or it may do a more normal S/R
		/// </summary>
		/// <returns>'true' if the S/R succeed, otherwise 'false'.</returns>
		private bool DoSendReceiveForLift(out bool dataChanged)
		{
			var projectFolder = Cache.ProjectId.ProjectFolder;
			var liftProjectDir = GetLiftRepositoryFolderFromFwProjectFolder(projectFolder);
			if (!Directory.Exists(liftProjectDir))
			{
				Directory.CreateDirectory(liftProjectDir);
			}
			var savedState = PrepareToDetectConflicts(liftProjectDir);
			string dummy;
			// flexbridge -p <path to fwdata file> -u <username> -v send_receive_lift
			var success = FLExBridgeHelper.LaunchFieldworksBridge(
				Path.Combine(projectFolder, Cache.ProjectId.Name + ".fwdata"),
				Environment.UserName,
				FLExBridgeHelper.SendReceiveLift, // May create a new lift repo in the process of doing the S/R. Or, it may just use the extant lift repo.
				out dataChanged, out dummy);
			if (!success)
			{
				ChooseLangProjectDialog.ReportDuplicateBridge();
				dataChanged = false;
				_liftPathname = null;
				return false;
			}

			_liftPathname = GetLiftPathname(liftProjectDir); // There better be one lift file in there.
			return true;
		}

		private static string GetLiftRepositoryFolderFromFwProjectFolder(string projectFolder)
		{
			var liftProjectDir = Path.Combine(projectFolder, FLExBridgeHelper.OtherRepositories,
											  FLExBridgeHelper.LIFT);
			return liftProjectDir;
		}

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

		/// <summary>
		/// This is invoked by reflection, due to almost insuperable sphaghetti in the relevant project references,
		/// from ChoooseLangProjectDialog.CreateProjectFromLift().
		/// If you are tempted to rename the method, be sure to do so in ChoooseLangProjectDialog.CreateProjectFromLift(), as well.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="liftPath"></param>
		/// <param name="parentForm"></param>
		/// <returns></returns>
		public static bool ImportObtainedLexicon(FdoCache cache, string liftPath, Form parentForm)
		{
			var importer = new FLExBridgeListener
				{
					Cache = cache,
					_liftPathname = liftPath,
					_parentForm = parentForm
				};
			return importer.ImportLiftCommon(FlexLiftMerger.MergeStyle.MsKeepNew); // should be a new project
		}

		private static string GetLiftPathname(string liftBaseDir)
		{
			return Directory.GetFiles(liftBaseDir, "*.lift").First();
		}

		/// <summary>
		/// Import the lift file using the given MergeStyle:
		///		FlexLiftMerger.MergeStyle.MsKeepNew (aka 'merciful', in that all entries from lift file and those in FLEx are retained)
		///		FlexLiftMerger.MergeStyle.MsKeepOnlyNew (aka 'merciless',
		///			in that the Flex lexicon ends up with the same entries as in the lift file, even if some need to be deleted in FLEx.)
		/// </summary>
		/// <param name="mergeStyle">FlexLiftMerger.MergeStyle.MsKeepNew or FlexLiftMerger.MergeStyle.MsKeepOnlyNew</param>
		/// <returns>'true' if the import succeeded, otherwise 'false'.</returns>
		private bool ImportLiftCommon(FlexLiftMerger.MergeStyle mergeStyle)
		{
			using (new WaitCursor(_parentForm))
			{
				using (var helper = new ThreadHelper()) // not _cache.ThreadHelper, which might be for a different thread
				using (var progressDlg = new ProgressDialogWithTask(_parentForm, helper))
				{
					_progressDlg = progressDlg;
					progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;
					try
					{
						progressDlg.Title = ResourceHelper.GetResourceString("kstidImportLiftlexicon");
						var logFile = (string)progressDlg.RunTask(true, ImportLiftLexicon, new object[] { _liftPathname, mergeStyle });
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
						//    bldr.AppendFormat(Resources.kProblem, m_liftPathname);
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
		/// Import the LIFT file into FieldWorks.
		/// </summary>
		/// <returns>the name of the exported LIFT file if successful, or null if an error occurs.</returns>
		/// <remarks>
		/// This method is called in a thread, during the export process.
		/// </remarks>
		private object ImportLiftLexicon(IProgress progressDialog, params object[] parameters)
		{
			var liftPathname = parameters[0].ToString();
			var mergeStyle = (FlexLiftMerger.MergeStyle)parameters[1];
			// If we use true while importing changes from repo it will fail to copy any pix/aud files that have changed.
			var fTrustModTimes = mergeStyle != FlexLiftMerger.MergeStyle.MsKeepOnlyNew;
			if (_progressDlg == null)
				_progressDlg = progressDialog;
			progressDialog.Minimum = 0;
			progressDialog.Maximum = 100;
			progressDialog.Position = 0;
			string sLogFile = null;

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
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
				var flexImporter = new FlexLiftMerger(Cache, mergeStyle, fTrustModTimes);
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
			return sLogFile;
		}

		/// <summary>
		/// Export the FieldWorks lexicon into the LIFT file.
		/// The file may, or may not, exist.
		/// </summary>
		/// <returns>True, if the import successful, otherwise false.</returns>
		/// <remarks>
		/// This method calls an overloaded ExportLiftLexicon, which is run in a thread.
		/// </remarks>
		public bool ExportLiftLexicon()
		{
			using (new WaitCursor(_parentForm))
			{
				using (var helper = new ThreadHelper()) // not _cache.ThreadHelper, which might be for a different thread
				using (var progressDlg = new ProgressDialogWithTask(_parentForm, helper))
				{
					_progressDlg = progressDlg;
					progressDlg.ProgressBarStyle = ProgressBarStyle.Continuous;
					try
					{
						progressDlg.Title = ResourceHelper.GetResourceString("kstidExportLiftLexicon");
						var outPath = (string)progressDlg.RunTask(true, ExportLiftLexicon, _liftPathname);
						var retval = (!String.IsNullOrEmpty(outPath));
						if (!retval)
							UndoExport();
						return retval;
					}
					catch
					{
						UndoExport();
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
		/// Export the contents of the lift lexicon.
		/// </summary>
		/// <returns>the name of the exported LIFT file if successful, or null if an error occurs.</returns>
		/// <remarks>
		/// This method is called in a thread, during the export process.
		/// </remarks>
		private object ExportLiftLexicon(IProgress progressDialog, params object[] parameters)
		{
			try
			{
				var projectFolder = Cache.ProjectId.ProjectFolder;
				var liftProjectDir = GetLiftRepositoryFolderFromFwProjectFolder(projectFolder);
				var liftPathname = _liftPathname;
				if (!Directory.Exists(liftProjectDir))
				{
					Directory.CreateDirectory(liftProjectDir);
					liftPathname = Path.Combine(liftProjectDir, Cache.ProjectId.Name + ".lift");
				}
				var outPath = liftPathname + ".tmp";
				progressDialog.Message = String.Format(ResourceHelper.GetResourceString("kstidExportingEntries"),
					Cache.LangProject.LexDbOA.Entries.Count());
				progressDialog.Minimum = 0;
				progressDialog.Maximum = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().Count;
				progressDialog.Position = 0;
				progressDialog.AllowCancel = false;

				var exporter = new LiftExporter(Cache);
				exporter.UpdateProgress += OnDumperUpdateProgress;
				exporter.SetProgressMessage += OnDumperSetProgressMessage;
				exporter.ExportPicturesAndMedia = true;
				using (TextWriter textWriter = new StreamWriter(outPath))
				{
					exporter.ExportLift(textWriter, Path.GetDirectoryName(outPath));
				}

				//Output the Ranges file
				var pathWithFilename = outPath.Substring(0, outPath.Length - @".lift.tmp".Length);
				var outPathRanges = Path.ChangeExtension(pathWithFilename, @"lift-ranges");
				using (var stringWriter = new StringWriter(new StringBuilder()))
				{
					exporter.ExportLiftRanges(stringWriter);
					using (var xmlWriter = XmlWriter.Create(outPathRanges, CanonicalXmlSettings.CreateXmlWriterSettings()))
					{
						var doc = new XmlDocument();
						doc.LoadXml(stringWriter.ToString());
						doc.WriteContentTo(xmlWriter);
					}
					return outPath;
				}
			}
			catch
			{
				return null;
			}
		}

		private void UndoExport()
		{
			bool dataChanged;
			string dummy;
			// Have FLEx Bridge do its 'undo'
			// flexbridge -p <project folder name> #-u username -v undo_export_lift)
			FLExBridgeHelper.LaunchFieldworksBridge(Cache.ProjectId.ProjectFolder, Environment.UserName,
								FLExBridgeHelper.UndoExportLift, out dataChanged, out dummy);
		}

		#endregion

		private bool ChangeProjectNameIfNeeded()
		{
			ProjectLockingService.UnlockCurrentProject(Cache);
			// Enhance GJM: When Hg is upgraded to work with non-Ascii filenames, this section can be removed.
			if (Unicode.CheckForNonAsciiCharacters(Cache.ProjectId.Name))
			{
				var revisedProjName = Unicode.RemoveNonAsciiCharsFromString(Cache.ProjectId.Name);
				if (revisedProjName == string.Empty)
					return true; // The whole pre-existing project name is non-Ascii characters!
				if (DisplayNonAsciiWarning(revisedProjName) == DialogResult.Cancel)
					return true;
				// Rename Project
				var projectFolder = RevisedProjectFolder(Cache.ProjectId.ProjectFolder, revisedProjName);
				if (CheckForExistingFileName(projectFolder, revisedProjName))
					return true;

				var app = (LexTextApp)_mediator.PropertyTable.GetValue("App");
				if (app.FwManager.RenameProject(revisedProjName, app))
				{
					// Continuing straight on from here renames the db on disk, but not in the cache, apparently
					// Try a more indirect approach...
					var fullProjectFileName = Path.Combine(projectFolder, revisedProjName + ".fwdata");
					var tempWindow = RefreshCacheWindowAndAll(app, fullProjectFileName);
					tempWindow.Mediator.SendMessageDefered("FLExBridge", null);
					// to hopefully come back here after resetting things
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Returns true if there is any Chorus Notes to view.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		private static bool NotesFileIsPresent(FdoCache cache)
		{
			return cache.ProjectId.ProjectFolder != null;
		}

		private static void ReportDuplicateBridge()
		{
			ChooseLangProjectDialog.ReportDuplicateBridge();
		}

		// currently duplicated in MorphologyListener, to avoid an assembly dependency.
		private static bool IsVernacularSpellingEnabled(Mediator mediator)
		{
			return mediator.PropertyTable.GetBoolProperty("UseVernSpellingDictionary", true);
		}

		private static DialogResult DisplayNonAsciiWarning(string revisedProjName)
		{
			return MessageBox.Show(string.Format(LexEdStrings.ksNonAsciiProjectNameWarning, revisedProjName), LexEdStrings.ksWarning,
					MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
		}

		private static bool CheckForExistingFileName(string projectFolder, string revisedFileName)
		{
			if(File.Exists(Path.Combine(projectFolder, revisedFileName + ".fwdata")))
			{
				MessageBox.Show(
					LexEdStrings.ksExistingProjectName, LexEdStrings.ksWarning, MessageBoxButtons.OK, MessageBoxIcon.Information);
				return true;
			}
			return false;
		}

		private static string RevisedProjectFolder(string oldProjectFolder, string revisedProjName)
		{
			return Path.Combine(Directory.GetParent(oldProjectFolder).FullName, revisedProjName);
		}

		private static FwXWindow RefreshCacheWindowAndAll(LexTextApp app, string fullProjectFileName)
		{
			var manager = app.FwManager;
			var appArgs = new FwAppArgs(fullProjectFileName);
			var newAppWindow =
				(FwXWindow)manager.ReopenProject(manager.Cache.ProjectId.Name, appArgs).ActiveMainWindow;
			if (IsVernacularSpellingEnabled(newAppWindow.Mediator))
				WfiWordformServices.ConformSpellingDictToWordforms(newAppWindow.Cache);
			//clear out any sort cache files (or whatever else might mess us up) and then refresh
			newAppWindow.ClearInvalidatedStoredData();
			newAppWindow.RefreshDisplay();
			return newAppWindow;
		}

		private bool IsDb4oProject
		{
			get { return Cache.ProjectId.Type == FDOBackendProviderType.kDb4oClientServer; }
		}

		private static bool DetectConflicts(string path, Dictionary<string, long> savedState)
		{
			foreach (var file in Directory.GetFiles(path, "*.ChorusNotes", SearchOption.AllDirectories))
			{
				// TODO: Test to see if one conflict tool can do both FLEx and LIFT conflicts.
				if (file.Contains(FLExBridgeHelper.OtherRepositories))
					continue; // Skip them, since they are part of some other repository.

				long oldLength;
				savedState.TryGetValue(file, out oldLength);
				if (new FileInfo(file).Length == oldLength)
					continue; // no new notes in this file.
				return true; // Review JohnT: do we need to look in the file to see if what was added is a conflict?
			}
			return false; // no conflicts added.
		}

		private static Dictionary<string, long> PrepareToDetectConflicts(string path)
		{
			var result = new Dictionary<string, long>();
			foreach (var file in Directory.GetFiles(path, "*.ChorusNotes", SearchOption.AllDirectories))
			{
				// TODO: Test to see if one conflict tool can do both FLEx and LIFT conflicts.
				if (file.Contains(FLExBridgeHelper.OtherRepositories))
					continue; // Skip them, since they are part of some other repository.

				result[file] = new FileInfo(file).Length;
			}
			return result;
		}

		private void JumpToFlexObject(object sender, FLExJumpEventArgs e)
		{
			// TODO: Test to see if one conflict tool can do both FLEx and LIFT conflicts.
			if (!string.IsNullOrEmpty(e.JumpUrl))
			{
				var args = new LocalLinkArgs { Link = e.JumpUrl };
				if (_mediator != null)
					_mediator.SendMessage("HandleLocalHotlink", args);
			}
		}

		private static bool CheckForFlexBridgeInstalled(UIItemDisplayProperties display)
		{
			var fullName = FLExBridgeHelper.FullFieldWorksBridgePath();
			display.Enabled = FileUtils.FileExists(fullName);
			display.Visible = display.Enabled;
			return display.Visible;
		}

		#region IDisposable implementation

		#if DEBUG
		/// <summary/>
		~FLExBridgeListener()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		private void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				FLExBridgeHelper.FLExJumpUrlChanged -= JumpToFlexObject;
				_mediator.RemoveColleague(this);
			}
			IsDisposed = true;
		}
		#endregion

		#region Implementation of IFWDisposable

		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		///              true.  This is the case where a method or property in an object is being
		///              used but the object itself is no longer valid.
		///              This method should be added to all public properties and methods of this
		///              object and all other objects derived from it (extensive).
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException("FLExBridgeListener already disposed.");
		}

		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		public bool IsDisposed { get; set; }

		#endregion

		private static void logger(string guid, string date, string description)
		{
			Console.WriteLine("Error reported, but not dealt with {0} {1} {2}", guid, date, description);
		}
	}
}
