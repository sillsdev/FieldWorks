// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.LexText;
using SIL.Code;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.Lift;
using SIL.Lift.Migration;
using SIL.Lift.Parsing;

namespace LanguageExplorer.SendReceive
{
	internal sealed class LiftBridge : IBridge
	{
		private readonly bool _isInFullBlownDisposeMode;
		private string _liftProjectDir;
		private string _liftPathname;
		private IProgress _progressDlg;
		/// <summary>
		/// The OldLiftBridgeProjects is populated from the mapping file if it is present.
		/// Things are never removed because that isn't important since the only purpose is to enable a menu item
		/// which will remain enabled after the project is migrated to the new flexbridge location.
		/// </summary>
		private readonly List<string> _oldLiftBridgeProjects = new List<string>();
		private LcmCache Cache { get; set; }
		private Form ParentForm { get; set; }
		private IFlexApp FlexApp { get; set; }
		private ToolStripMenuItem _mainSendReceiveMenu;
		private ToolStripMenuItem _viewMessagesMenu;
		private ToolStripMenuItem _obtainLiftBridgeProjectMenu;
		private ToolStripMenuItem _sendLiftBridgeFirstTimeProjectMenu;

		/// <summary>
		/// This is the file that our Message slice is configured to look for in the root project folder.
		/// The actual Lexicon.fwstub doesn't contain anything.
		/// Lexicon.fwstub.ChorusNotes contains notes about lexical entries.
		/// </summary>
		private const string FakeLexiconFileName = "Lexicon.fwstub";
		/// <summary>
		/// This is the file that actually holds the chorus notes for the lexicon.
		/// </summary>
		private const string FlexLexiconNotesFileName = FakeLexiconFileName + CommonBridgeServices.kChorusNotesExtension;
		/// <summary>
		/// Get the Flex notes pathname.
		/// </summary>
		private string FlexNotesPath => Path.Combine(Cache.ProjectId.ProjectFolder, FlexLexiconNotesFileName);
		/// <summary>
		/// Get the Lift notes pathname.
		/// </summary>
		private string LiftNotesPath => _liftPathname + CommonBridgeServices.kChorusNotesExtension;

		internal LiftBridge(LcmCache cache, IFwMainWnd mainWindow, IFlexApp flexApp)
		{
			Guard.AgainstNull(cache, nameof(cache));
			Guard.AgainstNull(mainWindow, nameof(mainWindow));
			Guard.AgainstNull(flexApp, nameof(flexApp));

			Cache = cache;
			ParentForm = (Form)mainWindow;
			FlexApp = flexApp;

			// Set up for handling antique Lift Bridge systems, if they are still around.
			var repoMapFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LiftBridge", "LanguageProject_Repository_Map.xml");
			// look for old liftbridge repo info in path similar to C:\Users\<user>\AppData\Local\LiftBridge\LanguageProject_Repository_Map.xml
			if (File.Exists(repoMapFile))
			{
				var repoMapDoc = XDocument.Load(repoMapFile);
				var mappingNodes = repoMapDoc.Elements("Mapping");
				foreach (var mappingNode in mappingNodes)
				{
					_oldLiftBridgeProjects.Add(mappingNode.Attribute("projectguid").Value);
				}
			}

			_liftProjectDir = CommonBridgeServices.GetLiftRepositoryFolderFromFwProjectFolder(Cache.ProjectId.ProjectFolder);
			_liftPathname = GetLiftPathname();
			_isInFullBlownDisposeMode = true; // Help the dispose method know what to do.
		}

		/// <summary>
		/// NB: This constructor is only to be used by the static method "ImportObtainedLexicon",
		/// which is called from afar via Reflection.
		/// </summary>
		private LiftBridge(LcmCache cache, string liftPath, Form parentForm)
		{
			Guard.AgainstNull(cache, nameof(cache));
			Guard.AgainstNullOrEmptyString(liftPath, nameof(liftPath));
			Guard.AgainstNull(parentForm, nameof(parentForm));

			Cache = cache;
			ParentForm = parentForm;

			_liftProjectDir = CommonBridgeServices.GetLiftRepositoryFolderFromFwProjectFolder(Cache.ProjectId.ProjectFolder);
			_liftPathname = liftPath;

			_isInFullBlownDisposeMode = false; // Help the dispose method know what to *not* do.
		}

		#region Implementation of IBridge
		/// <inheritdoc />
		public string Name => CommonBridgeServices.LiftBridge;

		/// <inheritdoc />
		public void RunBridge()
		{
			CommonBridgeServices.PrepareForSR(PropertyTable, Publisher, Cache, this);

			// Step 0. Try to move an extant lift repo from old location to new.
			if (!MoveOldLiftRepoIfNeeded())
				return;

			// Step 1. If notifier exists, re-try import (brutal or merciful, depending on contents of it).
			if (RepeatPriorFailedImportIfNeeded())
				return;

			// Step 2. Export lift file. If fails, then call into bridge with undo_export_lift and quit.
			if (!ExportLiftLexicon())
			{
				MessageBox.Show(ParentForm, LanguageExplorerResources.FLExBridgeListener_UndoExport_Error_exporting_LIFT, LanguageExplorerResources.FLExBridgeListener_UndoExport_LIFT_Export_failed_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Step 3. Have Flex Bridge do the S/R.
			// after saving the state enough to detect if conflicts are created.
			var fullProjectFileName = CommonBridgeServices.GetFullProjectFileName(Cache);
			bool dataChanged;
			if (!DoSendReceiveForLift(fullProjectFileName, out dataChanged))
			{
				// Bail out, since the S/R failed for some reason.
				return;
			}

			// Step 4. Import lift file. If fails, then add the notifier file.
			if (!DoMercilessLiftImport(dataChanged))
			{
				return;
			}

			if (!dataChanged)
			{
				return;
			}

			var liftFolder = CommonBridgeServices.GetLiftRepositoryFolderFromFwProjectFolder(Cache.ProjectId.ProjectFolder);
			HandlePotentialConflicts(FlexApp, PropertyTable.GetValue("UseVernSpellingDictionary", true), liftFolder, PrepareToDetectLiftConflicts(liftFolder), fullProjectFileName);
		}

		/// <inheritdoc />
		public void InstallMenus(BridgeMenuInstallRound currentInstallRound, ToolStripMenuItem mainSendReceiveToolStripMenuItem)
		{
			switch (currentInstallRound)
			{
				case BridgeMenuInstallRound.One:
					_mainSendReceiveMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(mainSendReceiveToolStripMenuItem, S_R_LiftBridge_Click, SendReceiveResources.LiftBridge, SendReceiveResources.LiftBridgeToolTip, Keys.None, SendReceiveResources.sendReceive16x16);
					_viewMessagesMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(mainSendReceiveToolStripMenuItem, ViewMessages_LiftBridge_Click, SendReceiveResources.ViewLiftMessagesLiftBridge, SendReceiveResources.ViewLiftMessagesLiftBridgeToolTip);
					break;
				case BridgeMenuInstallRound.Two:
					_obtainLiftBridgeProjectMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(mainSendReceiveToolStripMenuItem, ObtainLiftBridgeProject_Click, SendReceiveResources.ObtainLiftProject, SendReceiveResources.ObtainLiftProjectTooltip, Keys.None, SendReceiveResources.SendReceiveGetArrow16x16);
					break;
				case BridgeMenuInstallRound.Three:
					_sendLiftBridgeFirstTimeProjectMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(mainSendReceiveToolStripMenuItem, SendLiftBridgeFirstTime_Click, SendReceiveResources.FirstLiftBridge, SendReceiveResources.FirstLiftBridgeTooltip, Keys.None, SendReceiveResources.sendReceiveFirst16x16);
					break;
			}
		}

		/// <inheritdoc />
		public void SetEnabledStatus()
		{
			var hasOldLiftproject = _oldLiftBridgeProjects.Contains(Cache.LangProject.Guid.ToString());
			var isConfiguredForLiftSR = SendReceiveMenuManager.IsConfiguredForLiftSR(Cache.ProjectId.ProjectFolder);

			_mainSendReceiveMenu.Enabled = hasOldLiftproject || isConfiguredForLiftSR;
			_viewMessagesMenu.Enabled = CommonBridgeServices.NotesFileIsPresent(Cache, true);
			_obtainLiftBridgeProjectMenu.Enabled = !Directory.Exists(CommonBridgeServices.GetLiftRepositoryFolderFromFwProjectFolder(Cache.ProjectId.ProjectFolder));
			_sendLiftBridgeFirstTimeProjectMenu.Enabled = !hasOldLiftproject && !isConfiguredForLiftSR;
		}
		#endregion

		#region Implementation of IPropertyTableProvider
		/// <inheritdoc />
		public IPropertyTable PropertyTable { get; private set; }
		#endregion

		#region Implementation of IPublisherProvider
		/// <inheritdoc />
		public IPublisher Publisher { get; private set; }
		#endregion

		#region Implementation of ISubscriberProvider
		/// <inheritdoc />
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region IFlexComponent

		/// <inheritdoc />
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			Subscriber.Subscribe("ViewLiftMessages", ViewMessages);
		}
		#endregion

		#region IDisposable
		private bool _isDisposed;

		~LiftBridge()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// No need to run it more than once.
			if (_isDisposed)
				return;

			if (disposing)
			{
				if (_isInFullBlownDisposeMode)
				{
					Subscriber.Unsubscribe("ViewLiftMessages", ViewMessages);

					_mainSendReceiveMenu.Click -= S_R_LiftBridge_Click;
					_mainSendReceiveMenu.Dispose();

					_viewMessagesMenu.Click -= ViewMessages_LiftBridge_Click;
					_viewMessagesMenu.Dispose();

					_obtainLiftBridgeProjectMenu.Click -= ObtainLiftBridgeProject_Click;
					_obtainLiftBridgeProjectMenu.Dispose();

					_oldLiftBridgeProjects.Clear();
				}
			}
			_liftProjectDir = null;
			_liftPathname = null;
			_progressDlg = null;
			Cache = null;
			ParentForm = null;
			FlexApp = null;
			_mainSendReceiveMenu = null;
			_viewMessagesMenu = null;
			_obtainLiftBridgeProjectMenu = null;
			_sendLiftBridgeFirstTimeProjectMenu = null;

			_isDisposed = true;
		}
		#endregion

		private static void HandlePotentialConflicts(IFlexApp flexApp, bool useVernSpellingDictionary, string liftFolder, IReadOnlyDictionary<string, long> savedState, string fullProjectFileName)
		{
			var detectedLiftConflicts = false;

			foreach (var file in Directory.GetFiles(liftFolder, "*.ChorusNotes", SearchOption.AllDirectories))
			{
				long oldLength;
				savedState.TryGetValue(file, out oldLength);
				if (new FileInfo(file).Length == oldLength)
				{
					continue; // no new notes in this file.
				}
				detectedLiftConflicts = true; // Review JohnT: do we need to look in the file to see if what was added is a conflict?
			}
			if (!detectedLiftConflicts)
			{
				return;
			}

			var newAppWindow = CommonBridgeServices.RefreshCacheWindowAndAll(flexApp, useVernSpellingDictionary, fullProjectFileName);
			// Send a message for the reopened instance to display the message viewer (used to be conflict report),
			// we have been disposed by now
			newAppWindow.Publisher.Publish("ViewLiftMessages", null);
		}

		private void SendLiftBridgeFirstTime_Click(object sender, EventArgs e)
		{
			if (CommonBridgeServices.ShowMessageBeforeFirstSendReceive_IsUserReady(FlexApp))
			{
				RunBridge();
			}
		}

		private void ObtainLiftBridgeProject_Click(object sender, EventArgs e)
		{
			if (Directory.Exists(CommonBridgeServices.GetLiftRepositoryFolderFromFwProjectFolder(Cache.ProjectId.ProjectFolder)))
			{
				MessageBox.Show(ParentForm, LanguageExplorerResources.kProjectAlreadyHasLiftRepo, LanguageExplorerResources.kCannotDoGetAndMergeAgain, MessageBoxButtons.OK);
				return;
			}

			CommonBridgeServices.StopParser(Publisher);
			bool dummy;
			var success = FLExBridgeHelper.LaunchFieldworksBridge(Cache.ProjectId.ProjectFolder, null, FLExBridgeHelper.ObtainLift, null,
				LcmCache.ModelVersion, CommonBridgeServices.LiftModelVersion, null,
				null, out dummy, out _liftPathname);

			if (!success || string.IsNullOrEmpty(_liftPathname))
			{
				_liftPathname = null;
				return;
			}
			// Do merciful import.
			ImportLiftCommon(FlexLiftMerger.MergeStyle.MsKeepBoth);
			PropertyTable.SetProperty(CommonBridgeServices.LastBridgeUsed, CommonBridgeServices.LiftBridge, SettingsGroup.LocalSettings, true, false);
			Publisher.Publish("MasterRefresh", null);
		}

		private void ViewMessages(object obj)
		{
			_viewMessagesMenu.PerformClick();
		}

		private void S_R_LiftBridge_Click(object sender, EventArgs e)
		{
			RunBridge();
		}

		private void ViewMessages_LiftBridge_Click(object sender, EventArgs e)
		{
			bool dummy1;
			string dummy2;
			FLExBridgeHelper.FLExJumpUrlChanged += JumpToFlexObject;
			var success = FLExBridgeHelper.LaunchFieldworksBridge(
				CommonBridgeServices.GetFullProjectFileName(Cache),
				CommonBridgeServices.SendReceiveUser,
				FLExBridgeHelper.LiftConflictViewer,
				null, LcmCache.ModelVersion, CommonBridgeServices.LiftModelVersion, null, () => CommonBridgeServices.BroadcastMasterRefresh(Publisher),
				out dummy1, out dummy2);
			if (!success)
			{
				CommonBridgeServices.ReportDuplicateBridge();
			}
			FLExBridgeHelper.FLExJumpUrlChanged -= JumpToFlexObject;
		}

		private void JumpToFlexObject(object sender, FLExJumpEventArgs e)
		{
			CommonBridgeServices.PublishHandleLocalHotlinkMessage(Publisher, sender, e);
		}

		/// <summary>
		/// If the repo exists in the foo\OtherRepositories\LIFT folder, then do nothing.
		/// If the repo or the entire folder structure does not yet exist,
		/// then ask FLEx Bridge to move the previous lift repo to the new home,
		/// it is exists.
		/// </summary>
		/// <remarks>
		/// <para>If the call to FLEx Bridge returns the pathname to the lift file (_liftPathname), we know the move took place,
		/// and we have the lift file that is in the repository. That lift file's name may or may not match the FW project name,
		/// but it ought not matter if it does or does not match.</para>
		/// <para>If the call returned null, we know the move did not take place.
		/// In this case the caller of this method will continue on and probably create a new repository,
		///	thus doing the equivalent of the original Lift Bridge code where there FLEx user started a S/R lift system.</para>
		/// </remarks>
		/// <returns>'true' if the move succeeded, or if there was no need to do the move. The caller code will continue its work.
		/// Return 'false', if the calling code should quit its work.</returns>
		private bool MoveOldLiftRepoIfNeeded()
		{
			var liftProjectDir = CommonBridgeServices.GetLiftRepositoryFolderFromFwProjectFolder(Cache.ProjectId.ProjectFolder);
			// It is fine to try the repo move if the liftProjectDir exists, but *only* if it is completely empty.
			// Mercurial can't do a clone into a folder that has contents of any sort.
			if (Directory.Exists(liftProjectDir) && (Directory.GetDirectories(liftProjectDir).Length > 0 || Directory.GetFiles(liftProjectDir).Length > 0))
			{
				return true;
			}

			bool dummyDataChanged;
			// flexbridge -p <path to fwdata file> -u <username> -v move_lift -g Langprojguid
			var success = FLExBridgeHelper.LaunchFieldworksBridge(
				CommonBridgeServices.GetFullProjectFileName(Cache),
				CommonBridgeServices.SendReceiveUser,
				FLExBridgeHelper.MoveLift,
				Cache.LanguageProject.Guid.ToString().ToLowerInvariant(), LcmCache.ModelVersion, "0.13", null, null,
				out dummyDataChanged, out _liftPathname); // _liftPathname will be null, if no repo was moved.
			if (!success)
			{
				CommonBridgeServices.ReportDuplicateBridge();
				_liftPathname = null;
				return false;
			}

			return true;
		}

		/// <summary>
		/// Reregisters an import failure, if needed, otherwise clears the token.
		/// </summary>
		/// <returns>'true' if the import failure continues, otherwise 'false'.</returns>
		private bool RepeatPriorFailedImportIfNeeded()
		{
			if (!Directory.Exists(_liftProjectDir))
				return false;

			if (_liftPathname == null)
				return false;

			var previousImportStatus = LiftImportFailureServices.GetFailureStatus(_liftProjectDir);
			switch (previousImportStatus)
			{
				case ImportFailureStatus.BasicImportNeeded:
					return !ImportLiftCommon(FlexLiftMerger.MergeStyle.MsKeepBoth);
				case ImportFailureStatus.StandardImportNeeded:
					return !ImportLiftCommon(FlexLiftMerger.MergeStyle.MsKeepOnlyNew);
				case ImportFailureStatus.NoImportNeeded:
					// Nothing to do. :-)
					break;
			}
			return false;
		}

		private string GetLiftPathname()
		{
			// Part 2 of the LT-14809 fix is to test for the existence of the lift folder.
			// FB will delete it if the S/R was cancelled and Flex had just created the lift folder and file.
			// So don't crash if the folder no longer exists.
			return Directory.Exists(_liftProjectDir) ? Directory.GetFiles(_liftProjectDir, "*.lift").FirstOrDefault() : null;
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
			using (new WaitCursor(ParentForm))
			using (var progressDlg = new ProgressDialogWithTask(ParentForm))
			{
				_progressDlg = progressDlg;
				try
				{
					if (mergeStyle == FlexLiftMerger.MergeStyle.MsKeepBoth)
					{
						LiftImportFailureServices.RegisterBasicImportFailure(Path.GetDirectoryName(_liftPathname));
					}
					else
					{
						LiftImportFailureServices.RegisterStandardImportFailure(Path.GetDirectoryName(_liftPathname));
					}
					progressDlg.Title = ResourceHelper.GetResourceString("kstidImportLiftlexicon");
					var logFile = (string)progressDlg.RunTask(true, ImportLiftLexicon, _liftPathname, mergeStyle);
					if (logFile != null)
					{
						LiftImportFailureServices.ClearImportFailure(Path.GetDirectoryName(_liftPathname));
						return true;
					}
					LiftImportFailureServices.DisplayLiftFailureNoticeIfNecessary(ParentForm, _liftPathname);
					return false;
				}
				catch (WorkerThreadException error)
				{
					// It appears to be an analyst issue to sort out how we should report this.
					// LT-12340 however says we must report it somehow.
					var sMsg = string.Format(LanguageExplorerResources.kProblemImportWhileMerging, _liftPathname, error.InnerException.Message);
					MessageBoxUtils.Show(sMsg, LanguageExplorerResources.kProblemMerging, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return false;
				}
				finally
				{
					_progressDlg = null;
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
			{
				_progressDlg = progressDialog;
			}
			progressDialog.Minimum = 0;
			progressDialog.Maximum = 100;
			progressDialog.Position = 0;
			string sLogFile = null;

			if (File.Exists(LiftNotesPath))
			{

				using (var reader = new StreamReader(LiftNotesPath, Encoding.UTF8))
				using (var writer = new StreamWriter(FlexNotesPath, false, Encoding.UTF8))
				{
					ConvertLiftNotesToFlex(reader, writer);
				}
			}

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				string sFilename;
				var fMigrationNeeded = Migrator.IsMigrationNeeded(liftPathname);
				if (fMigrationNeeded)
				{
					var sOldVersion = SIL.Lift.Validation.Validator.GetLiftVersion(liftPathname);
					progressDialog.Message = string.Format(ResourceHelper.GetResourceString("kstidLiftVersionMigration"), sOldVersion, SIL.Lift.Validation.Validator.LiftVersion);
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
					var sTempMigrated = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetFileName(sFilename), "." + SIL.Lift.Validation.Validator.LiftVersion + ".lift"));
					if (File.Exists(sTempMigrated))
					{
						File.Delete(sTempMigrated);
					}
					File.Move(sFilename, sTempMigrated);
				}
				progressDialog.Message = ResourceHelper.GetResourceString("kstidFixingRelationLinks");
				flexImporter.ProcessPendingRelations(progressDialog);
				sLogFile = flexImporter.DisplayNewListItems(liftPathname, cEntries);
			});
			return sLogFile;
		}

		private void ParserSetTotalNumberSteps(object sender, LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.StepsArgs e)
		{
			_progressDlg.Maximum = e.Steps;
			_progressDlg.Position = 0;
		}

		private void ParserSetProgressMessage(object sender, LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.MessageArgs e)
		{
			_progressDlg.Position = 0;
			_progressDlg.Message = e.Message;
		}

		private void ParserSetStepsCompleted(object sender, LiftParser<LiftObject, CmLiftEntry, CmLiftSense, CmLiftExample>.ProgressEventArgs e)
		{
			var nMax = _progressDlg.Maximum;
			_progressDlg.Position = e.Progress > nMax ? e.Progress % nMax : e.Progress;
		}

		/// <summary>
		/// Convert FLEx ChorusNotes file referencing lex entries to LIFT notes by adjusting the "ref" attributes.
		/// </summary>
		/// <remarks>
		/// This method is internal, rather than static to let a test call it.
		/// </remarks>
		internal static void ConvertFlexNotesToLift(TextReader reader, TextWriter writer, string liftFileName)
		{
			// Typical input is something like
			// silfw://localhost/link?app=flex&amp;database=current&amp;server=&amp;tool=default&amp;guid=bab7776e-531b-4ce1-997f-fa638c09e381&amp;tag=&amp;id=bab7776e-531b-4ce1-997f-fa638c09e381&amp;label=Entry &quot;pintu&quot;
			// produce: lift://John.lift?type=entry&amp;label=fox&amp;id=f3093b9b-ea2f-422b-86b6-0defaa4646fe
			ConvertRefAttrs(reader, writer, liftFileName, "lift://{0}?type=entry&amp;label={1}&amp;id={2}");
		}

		/// <summary>
		/// Convert LIFT ChorusNotes file to FLEx notes by adjusting the "ref" attributes.
		/// </summary>
		/// <remarks>
		/// This method is internal, rather than static to let a test call it.
		/// </remarks>
		internal static void ConvertLiftNotesToFlex(TextReader reader, TextWriter writer)
		{
			// produce: silfw://localhost/link?app=flex&amp;database=current&amp;server=&amp;tool=default&amp;guid=bab7776e-531b-4ce1-997f-fa638c09e381&amp;tag=&amp;id=bab7776e-531b-4ce1-997f-fa638c09e381&amp;label=Entry &quot;pintu&quot;
			ConvertRefAttrs(reader, writer, string.Empty, "silfw://localhost/link?app=flex&amp;database=current&amp;server=&amp;tool=default&amp;guid={2}&amp;tag=&amp;id={2}&amp;label={1}");
		}

		private static void ConvertRefAttrs(TextReader reader, TextWriter writer, string liftFileName, string outputTemplate)
		{
			// Typical input is something like
			// silfw://localhost/link?app=flex&amp;database=current&amp;server=&amp;tool=default&amp;guid=bab7776e-531b-4ce1-997f-fa638c09e381&amp;tag=&amp;id=bab7776e-531b-4ce1-997f-fa638c09e381&amp;label=Entry &quot;pintu&quot;
			// or: lift://John.lift?type=entry&amp;label=fox&amp;id=f3093b9b-ea2f-422b-86b6-0defaa4646fe
			// both contain id=...&amp; and label=...&amp. One may be at the end without following &amp;.
			// Note that the ? is essential to prevent the greedy match including multiple parameters.
			// A label may contain things like &quot; so we can't just search for [^&]*.
			var reOuter = new Regex("ref=\\\"([^\\\"]*)\"");
			var reLabel = new Regex("label=(.*?)(&amp;|$)");
			var reId = new Regex("id=(.*?)(&amp;|$)");
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				var matchLine = reOuter.Match(line);
				if (matchLine.Success)
				{
					var input = matchLine.Groups[1].Value;
					var matchLabel = reLabel.Match(input);
					var matchId = reId.Match(input);
					if (matchLabel.Success && matchId.Success)
					{
						var guid = matchId.Groups[1].Value;
						var label = matchLabel.Groups[1].Value;
						var output = string.Format(outputTemplate,
							liftFileName, label, guid);
						writer.WriteLine(line.Replace(input, output));
						continue;
					}
				}
				writer.WriteLine(line);
			}
		}

		/// <summary>
		/// Export the FieldWorks lexicon into the LIFT file.
		/// The file may, or may not, exist.
		/// </summary>
		/// <returns>True, if the import successful, otherwise false.</returns>
		/// <remarks>
		/// This method calls an overloaded ExportLiftLexicon, which is run in a thread.
		/// </remarks>
		private bool ExportLiftLexicon()
		{
			using (new WaitCursor(ParentForm))
			using (var progressDlg = new ProgressDialogWithTask(ParentForm))
			{
				_progressDlg = progressDlg;
				try
				{
					progressDlg.Title = ResourceHelper.GetResourceString("kstidExportLiftLexicon");
					var outPath = (string)progressDlg.RunTask(true, ExportLiftLexicon, null);
					var retval = (!string.IsNullOrEmpty(outPath));
					if (!retval && CanUndoLiftExport)
					{
						UndoExport();
					}
					return retval;
				}
				catch
				{
					if (CanUndoLiftExport)
					{
						UndoExport();
					}
					return false;
				}
				finally
				{
					_progressDlg = null;
				}
			}
		}

		private bool CanUndoLiftExport => Directory.Exists(_liftProjectDir) && Directory.Exists(Path.Combine(_liftProjectDir, ".hg"));

		private void UndoExport()
		{
			bool dataChanged;
			string dummy;
			// Have FLEx Bridge do its 'undo'
			// flexbridge -p <project folder name> #-u username -v undo_export_lift)
			FLExBridgeHelper.LaunchFieldworksBridge(Cache.ProjectId.ProjectFolder, CommonBridgeServices.SendReceiveUser,
				FLExBridgeHelper.UndoExportLift, null, LcmCache.ModelVersion, CommonBridgeServices.LiftModelVersion, null, null,
				out dataChanged, out dummy);
		}

		/// <summary>
		/// Export the contents of the lift lexicon.
		/// </summary>
		/// <param name="progressDialog"></param>
		/// <param name="parameters">parameters are not used in this method. This method is called by an invoker,
		/// which requires this signature.</param>
		/// <returns>the name of the exported LIFT file if successful, or null if an error occurs.</returns>
		/// <remarks>
		/// This method is called in a thread, during the export process.
		/// </remarks>
		private object ExportLiftLexicon(IProgress progressDialog, params object[] parameters)
		{
			try
			{
				if (!Directory.Exists(_liftProjectDir))
				{
					Directory.CreateDirectory(_liftProjectDir);
				}
				if (string.IsNullOrEmpty(_liftPathname))
				{
					_liftPathname = Path.Combine(_liftProjectDir, Cache.ProjectId.Name + ".lift");
				}
				progressDialog.Message = string.Format(ResourceHelper.GetResourceString("kstidExportingEntries"),
					Cache.LangProject.LexDbOA.Entries.Count());
				progressDialog.Minimum = 0;
				progressDialog.Maximum = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().Count;
				progressDialog.Position = 0;
				progressDialog.AllowCancel = false;

				var exporter = new LiftExporter(Cache);
				exporter.UpdateProgress += OnDumperUpdateProgress;
				exporter.SetProgressMessage += OnDumperSetProgressMessage;
				exporter.ExportPicturesAndMedia = true;
				using (TextWriter textWriter = new StreamWriter(_liftPathname))
				{
					exporter.ExportLift(textWriter, Path.GetDirectoryName(_liftPathname));
				}
				LiftSorter.SortLiftFile(_liftPathname);

				//Output the Ranges file
				var outPathRanges = Path.ChangeExtension(_liftPathname, "lift-ranges");
				using (var stringWriter = new StringWriter(new StringBuilder()))
				{
					exporter.ExportLiftRanges(stringWriter);
					File.WriteAllText(outPathRanges, stringWriter.ToString());
				}
				LiftSorter.SortLiftRangesFiles(outPathRanges);

				if (File.Exists(FlexNotesPath))
				{

					using (var reader = new StreamReader(FlexNotesPath, Encoding.UTF8))
					using (var writer = new StreamWriter(LiftNotesPath, false, Encoding.UTF8))
					{
						ConvertFlexNotesToLift(reader, writer, Path.GetFileName(_liftPathname));
					}
				}

				return _liftPathname;
			}
			catch
			{
				_liftPathname = null;
				return _liftPathname;
			}
		}

		private void OnDumperSetProgressMessage(object sender, ProgressMessageArgs e)
		{
			if (_progressDlg == null)
				return;
			var message = ResourceHelper.GetResourceString(e.MessageId);
			if (!string.IsNullOrEmpty(message))
				_progressDlg.Message = message;
			_progressDlg.Minimum = 0;
			_progressDlg.Maximum = e.Max;
		}

		private void OnDumperUpdateProgress(object sender)
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

		private bool DoMercilessLiftImport(bool dataChanged)
		{
			if (!dataChanged)
			{
				return true;
			}
			if (!ImportLiftCommon(FlexLiftMerger.MergeStyle.MsKeepOnlyNew))
			{
				return false;
			}

			var liftFolder = CommonBridgeServices.GetLiftRepositoryFolderFromFwProjectFolder(Cache.ProjectId.ProjectFolder);
			HandlePotentialConflicts(FlexApp,
				PropertyTable.GetValue("UseVernSpellingDictionary", true),
				liftFolder,
				PrepareToDetectLiftConflicts(liftFolder),
				CommonBridgeServices.GetFullProjectFileName(Cache));

			return true;
		}

		/// <summary>
		/// Do the S/R. This *may* actually create the Lift repository, if it doesn't exist, or it may do a more normal S/R
		/// </summary>
		/// <returns>'true' if the S/R succeed, otherwise 'false'.</returns>
		private bool DoSendReceiveForLift(string fullProjectFileName, out bool dataChanged)
		{
			if (!Directory.Exists(_liftProjectDir))
			{
				Directory.CreateDirectory(_liftProjectDir);
			}
			_liftPathname = GetLiftPathname();
			PrepareToDetectLiftConflicts(_liftPathname);
			string dummy;
			// flexbridge -p <path to fwdata/fwdb file> -u <username> -v send_receive_lift
			var success = FLExBridgeHelper.LaunchFieldworksBridge(
				fullProjectFileName,
				CommonBridgeServices.SendReceiveUser,
				FLExBridgeHelper.SendReceiveLift, // May create a new lift repo in the process of doing the S/R. Or, it may just use the extant lift repo.
				null, LcmCache.ModelVersion, "0.13", Cache.LangProject.DefaultVernacularWritingSystem.Id, null,
				out dataChanged, out dummy);
			if (!success)
			{
				CommonBridgeServices.ReportDuplicateBridge();
				dataChanged = false;
				_liftPathname = null;
				return false;
			}

			_liftPathname = GetLiftPathname();

			if (_liftPathname == null)
			{
				dataChanged = false; // If there is no lift file, there cannot be any new data.
				return false;
			}

			return true;
		}

		/// <summary>
		/// This is only used for the Lift repo folder.
		/// </summary>
		/// <param name="liftPath"></param>
		/// <returns></returns>
		private static Dictionary<string, long> PrepareToDetectLiftConflicts(string liftPath)
		{
			var result = new Dictionary<string, long>();
			if (!Directory.Exists(liftPath))
			{
				return result;
			}
			foreach (var file in Directory.GetFiles(liftPath, "*.ChorusNotes", SearchOption.AllDirectories))
			{
				result[file] = new FileInfo(file).Length;
			}
			return result;
		}

		/// <summary>
		/// This is invoked by reflection, due to almost insuperable sphaghetti in the relevant project references,
		/// from ChoooseLangProjectDialog.CreateProjectFromLift().
		/// If you are tempted to rename the method, be sure to do so in ChoooseLangProjectDialog.CreateProjectFromLift(), as well.
		/// </summary>
		public static bool ImportObtainedLexicon(LcmCache cache, string liftPath, Form parentForm)
		{
			using (var liftBridge = new LiftBridge(cache, liftPath, parentForm))
			{
				return liftBridge.ImportLiftCommon(FlexLiftMerger.MergeStyle.MsKeepBoth); // should be a new project
			}
		}
	}
}