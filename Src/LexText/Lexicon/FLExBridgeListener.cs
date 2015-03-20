using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using Palaso.Lift;
using Palaso.Lift.Migration;
using Palaso.Lift.Parsing;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.XWorks.LexText;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	[MediatorDispose]
	[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule",
		Justification="_mediator is a reference")]
	sealed class FLExBridgeListener : IxCoreColleague, IFWDisposable
	{
		private Mediator _mediator;
		private Form _parentForm;
		private string _liftPathname;
		private IProgress _progressDlg;
		private FdoCache Cache { get; set; }

		/// <summary>
		/// On static initialization the OldLiftBridgeProjects is populated from the mapping file if it is present.
		/// Things are never removed because that isn't important since the only purpose is to enable a menu item
		/// which will remain enabled after the project is migrated to the new flexbridge location.
		/// </summary>
		private static readonly List<string> OldLiftBridgeProjects;

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		static FLExBridgeListener()
		{
			OldLiftBridgeProjects = new List<string>();
			var repoMapFile = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LiftBridge"),
													 "LanguageProject_Repository_Map.xml");
			// look for old liftbridge repo info in path similar to C:\Users\<user>\AppData\Local\LiftBridge\LanguageProject_Repository_Map.xml
			if(File.Exists(repoMapFile))
			{
				var repoMapDoc = new XmlDocument();
				repoMapDoc.Load(repoMapFile);
				var mappingNodes = repoMapDoc.SelectNodes("//Mapping");
				if(mappingNodes != null)
				{
					foreach(XmlElement mappingNode in mappingNodes)
					{
						OldLiftBridgeProjects.Add(mappingNode.Attributes["projectguid"].Value);
					}
				}
			}
		}

		#region IxCoreColleague Implementation

		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] { this };
		}

		public void Init(Mediator mediator, XmlNode configurationParameters)
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

		#region FLExLiftBridge Toolbar messages
		/// <summary>
		/// Determine whether or not to enable the S/R toolbar icon and its hotkey.
		/// The button and hotkey always disabled before the first successful S/R.
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayFLExLiftBridge(object parameters, ref UIItemDisplayProperties display)
		{
			CheckForFlexBridgeInstalledAndSetMenuItemProperties(display);
			var bridgeLastUsed = _mediator.PropertyTable.GetStringProperty(
				"LastBridgeUsed", "NoBridgeUsedYet", PropertyTable.SettingsGroup.LocalSettings);
			if (bridgeLastUsed == "FLExBridge")
			{
				// If Fix it app does not exist, then disable main FLEx S/R, since FB needs to call it, after a merge.
				// If !IsConfiguredForSR (failed the first time), disable the button and hotkey
				display.Enabled = display.Enabled && FLExBridgeHelper.FixItAppExists && FLExBridgeHelper.DoesProjectHaveFlexRepo(Cache.ProjectId);
			}
			else if (bridgeLastUsed == "LiftBridge")
			{
				// If !IsConfiguredForLiftSR (failed first time), disable the button and hotkey
				display.Enabled = display.Enabled && FLExBridgeHelper.DoesProjectHaveLiftRepo(Cache.ProjectId);
			}
			else // there was no LastBridgeUsed (this is the first time), disable the button and hotkey
			{
				display.Enabled = false;
			}

			return true; // We dealt with it.
		}

		/// <summary>
		/// This is the button on the toolbar for FlexBridge and doing the last type of S/R (Flex or Lift)
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnFLExLiftBridge(object commandObject)
		{
			if (MiscUtils.IsMono)
			{
				// This is a horrible workaround for a nasty bug in Mono. The toolbar button captures the mouse,
				// and does not release it before calling this event handler. If we proceed to run the bridge,
				// which freezes our UI thread until FlexBridge returns, the mouse stays captured...and the whole
				// system UI is frozen, for all applications.
				_mediator.IdleQueue.Add(IdleQueuePriority.High,
					obj =>
						{
							RunFLExLiftBridge(commandObject);
							return true; // task is complete, don't try again, remove from idle queue.
						});
			}
			else
				RunFLExLiftBridge(commandObject); // on windows we can safely do it right away.
			return true; // we handled it
		}

		private void RunFLExLiftBridge(object commandObject)
		{
			var bridgeLastUsed = _mediator.PropertyTable.GetStringProperty("LastBridgeUsed", "FLExBridge", PropertyTable.SettingsGroup.LocalSettings);
			if (bridgeLastUsed == "FLExBridge")
				OnFLExBridge(commandObject);

			else if (bridgeLastUsed == "LiftBridge")
				OnLiftBridge(commandObject);
		}
		#endregion FLExLiftBridge Toolbar messages

		#region Obtain a Flex or Lift repo and create a new FW project

		/// <summary>
		/// Determine whether or not to show/enable the Send/Receive "_Get Project from Colleague" menu item.
		/// </summary>
		public bool OnDisplayObtainAnyFlexBridgeProject(object parameters, ref UIItemDisplayProperties display)
		{
			CheckForFlexBridgeInstalledAndSetMenuItemProperties(display);

			return true; // We dealt with it.
		}

		/// <summary>
		/// Handle the S/R "_Get Project from Colleague" menu option.
		/// </summary>
		public bool OnObtainAnyFlexBridgeProject(object commandObject)
		{
			ObtainedProjectType obtainedProjectType;
			var newprojectPathname = ObtainProjectMethod.ObtainProjectFromAnySource(_parentForm, _mediator.HelpTopicProvider,
				out obtainedProjectType);
			if (string.IsNullOrEmpty(newprojectPathname))
				return true; // We dealt with it.
			_mediator.PropertyTable.SetProperty("LastBridgeUsed", obtainedProjectType == ObtainedProjectType.Lift ? "LiftBridge" : "FLExBridge",
				PropertyTable.SettingsGroup.LocalSettings);

			FieldWorks.OpenNewProject(new ProjectId(newprojectPathname));

			return true;
		}

		#endregion Obtain a Flex or Lift repo and create a new FW project

		#region Obtain a Lift repo and do merciful import into current project

		/// <summary>
		/// Determine whether or not to show the Send/Receive "Get and _Merge Lexicon with this Project" menu item.
		/// </summary>
		public bool OnDisplayObtainLiftProject(object parameters, ref UIItemDisplayProperties display)
		{
			CheckForFlexBridgeInstalledAndSetMenuItemProperties(display);
			if (!display.Enabled)
				return true; // Flex Bridge isn't installed, so the rest doesn't matter.

			// Disable, if current project already has a lift repo.
			var liftProjectFolder = GetLiftRepositoryFolderFromFwProjectFolder(Cache.ProjectId.ProjectFolder);
			display.Enabled = !Directory.Exists(liftProjectFolder);

			return true; // We dealt with it.
		}

		/// <summary>
		/// Handles the "Get and _Merge Lexicon with this Project" menu item.
		/// </summary>
		public bool OnObtainLiftProject(object commandObject)
		{
			if (Directory.Exists(GetLiftRepositoryFolderFromFwProjectFolder(Cache.ProjectId.ProjectFolder)))
			{
				MessageBox.Show(((FwApp)_mediator.PropertyTable.GetValue("App")).ActiveMainWindow, LexEdStrings.kProjectAlreadyHasLiftRepo, LexEdStrings.kCannotDoGetAndMergeAgain, MessageBoxButtons.OK);
				return true;
			}

			StopParser();
			bool dummy;
			var success = FLExBridgeHelper.LaunchFieldworksBridge(Cache.ProjectId.ProjectFolder, null, FLExBridgeHelper.ObtainLift, null, FDOBackendProvider.ModelVersion, "0.13",
				null, out dummy, out _liftPathname);

			if (!success || string.IsNullOrEmpty(_liftPathname))
			{
				_liftPathname = null;
				return true;
			}
			 // Do merciful import.
			ImportLiftCommon(FlexLiftMerger.MergeStyle.MsKeepBoth);
			_mediator.PropertyTable.SetProperty("LastBridgeUsed", "LiftBridge", PropertyTable.SettingsGroup.LocalSettings);
			_mediator.BroadcastMessage("MasterRefresh", null);

			return true;
		}

		#endregion Obtain a Lift repo and do merciful import into current project

		#region FLExBridge S/R messages

		/// <summary>
		/// Determine whether or not to show the Send/Receive "_Project (with other FLEx users)" menu item.
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayFLExBridge(object parameters, ref UIItemDisplayProperties display)
		{
			CheckForFlexBridgeInstalledAndSetMenuItemProperties(display);
			if (!display.Enabled)
				return true; // Flex Bridge isn't installed, so the rest doesn't matter.

			// If Fix it app does not exist, then disable main FLEx S/R, since FB needs to call it, after a merge.
			display.Enabled = FLExBridgeHelper.DoesProjectHaveFlexRepo(Cache.ProjectId) && FLExBridgeHelper.FixItAppExists;

			return true; // We dealt with it.
		}

		/// <summary>
		/// The method/delegate that gets invoked when Send/Receive "_Project (with other FLEx users)" menu is clicked.
		/// </summary>
		/// <param name="commandObject">Includes the XML command element of the OnFLExBridge message</param>
		/// <returns>true if the message was handled, false if there was an error or the call was deemed inappropriate.</returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "newAppWindow is a reference")]
		public bool OnFLExBridge(object commandObject)
		{
			if (!LinkedFilesLocationIsDefault())
			{
				using (var dlg = new FwCoreDlgs.WarningNotUsingDefaultLinkedFilesLocation(_mediator.HelpTopicProvider))
				{
					var result = dlg.ShowDialog();
					if (result == DialogResult.Yes)
					{
						var app = (LexTextApp)_mediator.PropertyTable.GetValue("App");
						var sLinkedFilesRootDir = app.Cache.LangProject.LinkedFilesRootDir;
						NonUndoableUnitOfWorkHelper.Do(app.Cache.ActionHandlerAccessor, () =>
						{
							app.Cache.LangProject.LinkedFilesRootDir = FdoFileHelper.GetDefaultLinkedFilesDir(
								app.Cache.ProjectId.ProjectFolder);
						});
						app.UpdateExternalLinks(sLinkedFilesRootDir);
					}
				}
			}
			// Make sure that there aren't multiple applications accessing the project
			// It is possible for a user to start up an application that accesses the
			// project after this check, but the application should not interfere with
			// the S/R operation.
			while (SharedBackendServices.AreMultipleApplicationsConnected(Cache))
			{
				if (ThreadHelper.ShowMessageBox(null, LexEdStrings.ksSendReceiveNotPermittedMultipleAppsText,
					LexEdStrings.ksSendReceiveNotPermittedMultipleAppsCaption, MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
				{
					return true;
				}
			}
			StopParser();
			SaveAllDataToDisk();
			_mediator.PropertyTable.SetProperty("LastBridgeUsed", "FLExBridge", PropertyTable.SettingsGroup.LocalSettings);

			if (ChangeProjectNameIfNeeded())
				return true;

			string url;
			var projectFolder = Cache.ProjectId.ProjectFolder;
			var savedState = PrepareToDetectMainConflicts(projectFolder);
			string dummy;
			var fullProjectFileName = Path.Combine(projectFolder, Cache.ProjectId.Name + FdoFileHelper.ksFwDataXmlFileExtension);
			bool dataChanged;
			var success = FLExBridgeHelper.LaunchFieldworksBridge(fullProjectFileName, SendReceiveUser,
																  FLExBridgeHelper.SendReceive,
																  null, FDOBackendProvider.ModelVersion, "0.13", Cache.LangProject.DefaultVernacularWritingSystem.Id,
																  out dataChanged, out dummy);
			if (!success)
			{
				ReportDuplicateBridge();
				ProjectLockingService.LockCurrentProject(Cache);
				return true;
			}

			if (dataChanged)
			{
				bool conflictOccurred = DetectMainConflicts(projectFolder, savedState);
				var app = (LexTextApp)_mediator.PropertyTable.GetValue("App");
				var newAppWindow = RefreshCacheWindowAndAll(app, fullProjectFileName);
				if (conflictOccurred)
				{
					// Send a message for the reopened instance to display the message viewer (used to be conflict report),
					// we have been disposed by now
					newAppWindow.Mediator.SendMessage("ViewMessages", null);
				}
			}
			else //Re-lock project if we aren't trying to close the app
			{
				ProjectLockingService.LockCurrentProject(Cache);
			}
			return true;
		}

		private bool LinkedFilesLocationIsDefault()
		{
			var defaultLinkedFilesFolder = FdoFileHelper.GetDefaultLinkedFilesDir(Cache.ServiceLocator.DataSetup.ProjectId.ProjectFolder);
			if (!defaultLinkedFilesFolder.Equals(Cache.LanguageProject.LinkedFilesRootDir))
				return false;
			else
				return true;
		}
		#endregion FLExBridge S/R messages

		#region LiftBridge S/R messages

		/// <summary>
		/// Called to setup Send/Receive by LiftBridge the first time. Shows a brief instructional message,
		/// then, if the user is ready, calls through to OnLiftBridge.
		/// </summary>
		public bool OnFirstLiftBridge(object commandObject)
		{
			if (ShowMessageBeforeFirstSendReceive_IsUserReady())
				OnLiftBridge(commandObject);
			return true;
		}

		/// <summary>
		/// Called (by xcore) to control display params of the Send/Receive "_Lexicon (with programs that use WeSay)" menu.
		/// </summary>
		public bool OnDisplayFirstLiftBridge(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckForFlexBridgeInstalledAndSetMenuItemProperties(display);
			if (!display.Enabled)
				return true; // Flex Bridge isn't installed, so the rest doesn't matter.

			display.Enabled = !OldLiftBridgeProjects.Contains(Cache.LangProject.Guid.ToString())
				&& !FLExBridgeHelper.DoesProjectHaveLiftRepo(Cache.ProjectId);
			return true; // We dealt with it.
		}

		/// <summary>
		/// Called (by xcore) to control display params of the Send/Receive Project menu.
		/// </summary>
		public bool OnDisplayFirstFLExBridge(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckForFlexBridgeInstalledAndSetMenuItemProperties(display);
			if (!display.Enabled)
				return true; // Flex Bridge isn't installed, so the rest doesn't matter.

			display.Enabled = !FLExBridgeHelper.DoesProjectHaveFlexRepo(Cache.ProjectId);
			return true; // We dealt with it.
		}

		/// <summary>
		/// Called to setup Send/Receive by FLExBridge the first time. Shows a brief instructional message,
		/// then, if the user is ready, calls through to OnFLExBridge.
		/// </summary>
		public bool OnFirstFLExBridge(object commandObject)
		{
			if (ShowMessageBeforeFirstSendReceive_IsUserReady())
				OnFLExBridge(commandObject);
			return true;
		}

		/// <summary>
		/// Called (by xcore) to control display params of the Send/Receive "_Lexicon (with programs that use WeSay)" menu.
		/// </summary>
		public bool OnDisplayLiftBridge(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckForFlexBridgeInstalledAndSetMenuItemProperties(display);
			if (!display.Enabled)
				return true; // Flex Bridge isn't installed, so the rest doesn't matter.

				display.Enabled = OldLiftBridgeProjects.Contains(Cache.LangProject.Guid.ToString()) || FLExBridgeHelper.DoesProjectHaveLiftRepo(Cache.ProjectId);
			return true; // We dealt with it.
		}

		/// <summary>
		/// The method/delegate that gets invoked when Send/Receive "_Lexicon (with programs that use LIFT)" menu is clicked.
		/// </summary>
		/// <param name="argument">Includes the XML command element of the OnLiftBridge message</param>
		/// <returns>true if the message was handled, false if there was an error or the call was deemed inappropriate, or somebody should also try to handle the message.</returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "newAppWindow is a reference")]
		public bool OnLiftBridge(object argument)
		{
			SaveAllDataToDisk();
			_mediator.PropertyTable.SetProperty("LastBridgeUsed", "LiftBridge", PropertyTable.SettingsGroup.LocalSettings);

			// Step 0. Try to move an extant lift repo from old location to new.
			if (!MoveOldLiftRepoIfNeeded())
				return true;

			// Step 1. If notifier exists, re-try import (brutal or merciful, depending on contents of it).
			if (RepeatPriorFailedImportIfNeeded())
				return true;

			// Step 2. Export lift file. If fails, then call into bridge with undo_export_lift and quit.
			if (!ExportLiftLexicon())
			{
				MessageBox.Show(_parentForm, LexEdStrings.FLExBridgeListener_UndoExport_Error_exporting_LIFT, LexEdStrings.FLExBridgeListener_UndoExport_LIFT_Export_failed_Title,
								MessageBoxButtons.OK, MessageBoxIcon.Error);
				return true;
			}

			// Step 3. Have Flex Bridge do the S/R.
			// after saving the state enough to detect if conflicts are created.
			var liftFolder = GetLiftRepositoryFolderFromFwProjectFolder(Cache.ProjectId.ProjectFolder);
			var savedState = PrepareToDetectLiftConflicts(liftFolder);
			var fullProjectFileName = GetFullProjectFileName();
			bool dataChanged;
			if (!DoSendReceiveForLift(fullProjectFileName, out dataChanged))
				return true; // Bail out, since the S/R failed for some reason.

			// Step 4. Import lift file. If fails, then add the notifier file.
			if (!DoMercilessLiftImport(dataChanged))
				return true;

			if (dataChanged)
			{
				bool conflictOccurred = DetectLiftConflicts(liftFolder, savedState);
				var app = (LexTextApp)_mediator.PropertyTable.GetValue("App");
				var newAppWindow = RefreshCacheWindowAndAll(app, fullProjectFileName);
				if (conflictOccurred)
				{
					// Send a message for the reopened instance to display the message viewer (used to be conflict report),
					// we have been disposed by now
					newAppWindow.Mediator.SendMessage("ViewLiftMessages", null);
				}
			}

			return true; // We dealt with it.
		}

		public bool OnWarnUserAboutFailedLiftImportIfNecessary(object param)
		{
			var liftFolder = GetLiftRepositoryFolderFromFwProjectFolder(Cache.ProjectId.ProjectFolder);
			if(LiftImportFailureServices.GetFailureStatus(liftFolder) != ImportFailureStatus.NoImportNeeded)
			{
				MessageBox.Show(LexEdStrings.LiftSRFailureDetectedOnStartupMessage,
									 LexEdStrings.LiftSRFailureDetectedOnStartupTitle,
									 MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			return true;
		}

		private string GetFullProjectFileName()
		{
			return Path.Combine(Cache.ProjectId.ProjectFolder, Cache.ProjectId.Name + FdoFileHelper.ksFwDataXmlFileExtension);
		}

		private void SaveAllDataToDisk()
		{
			//Give all forms the opportunity to save any uncommitted data
			//(important for analysis sandboxes)
			var activeForm = _mediator.PropertyTable.GetValue("window") as Form;
			if (activeForm != null)
			{
				activeForm.ValidateChildren(ValidationConstraints.Enabled);
			}
			//Commit all the data in the cache and save to disk
			ProjectLockingService.UnlockCurrentProject(Cache);
		}

		#endregion LiftBridge S/R messages

		#region CheckForFlexBridgeUpdates messages

		/// <summary>
		/// Called (by xcore) to control display params of the Send/Receive->"Check for _Updates..." menu.
		/// </summary>
		public bool OnDisplayCheckForFlexBridgeUpdates(object commandObject, ref UIItemDisplayProperties display)
		{
			if (MiscUtils.IsUnix)
			{
				display.Visible = false;
				display.Enabled = false;
			}
			else
			{
				CheckForFlexBridgeInstalledAndSetMenuItemProperties(display);
			}

			return true; // We dealt with it.
		}

		/// <summary>
		/// The method/delegate that gets invoked when Send/Receive->"Check for _Updates..." menu is clicked.
		/// </summary>
		/// <param name="argument">Includes the XML command element of the OnAboutFlexBridge message</param>
		/// <returns>true if the message was handled, false if there was an error or the call was deemed inappropriate, or somebody shoudl also try to handle the message.</returns>
		public bool OnCheckForFlexBridgeUpdates(object argument)
		{
			bool dummy1;
			string dummy2;
			FLExBridgeHelper.LaunchFieldworksBridge(
				GetFullProjectFileName(),
				SendReceiveUser,
				FLExBridgeHelper.CheckForUpdates,
				null, FDOBackendProvider.ModelVersion, "0.13", null,
				out dummy1, out dummy2);

			return true;
		}

		#endregion CheckForFlexBridgeUpdates messages

		#region AboutFlexBridge messages

		/// <summary>
		/// Called (by xcore) to control display params of the Send/Receive->"About" menu.
		/// </summary>
		public bool OnDisplayAboutFlexBridge(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckForFlexBridgeInstalledAndSetMenuItemProperties(display);

			return true; // We dealt with it.
		}

		/// <summary>
		/// The method/delegate that gets invoked when Send/Receive->"About" menu is clicked.
		/// </summary>
		/// <param name="argument">Includes the XML command element of the OnAboutFlexBridge message</param>
		/// <returns>true if the message was handled, false if there was an error or the call was deemed inappropriate, or somebody shoudl also try to handle the message.</returns>
		public bool OnAboutFlexBridge(object argument)
		{
			bool dummy1;
			string dummy2;
			FLExBridgeHelper.LaunchFieldworksBridge(
				GetFullProjectFileName(),
				SendReceiveUser,
				FLExBridgeHelper.AboutFLExBridge,
				null, FDOBackendProvider.ModelVersion, "0.13", null,
				out dummy1, out dummy2);

			return true;
		}

		#endregion AboutFlexBridge messages

		#region ViewMessages (for full FLEx data only) messages

		/// <summary>
		/// Determine whether or not to display the Send/Receive->"View Messages" menu item.
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayViewMessages(object parameters, ref UIItemDisplayProperties display)
		{
			CheckForFlexBridgeInstalledAndSetMenuItemProperties(display);
			if (!display.Enabled)
				return true; // Flex Bridge isn't installed, so the rest doesn't matter.

			display.Enabled = NotesFileIsPresent(Cache, false);

			return true;
		}

		/// <summary>
		/// The method/delegate that gets invoked when Send/Receive->View Messages is clicked
		/// </summary>
		/// <param name="commandObject">Includes the XML command element of the OnViewMessages message</param>
		/// <returns>true if the message was handled, false if there was an error or the call was deemed inappropriate.</returns>
		/// <remarks>If you change the name of this method, you need to check for calls to SendMessage("ViewMessages").</remarks>
		public bool OnViewMessages(object commandObject)
		{
			bool dummy1;
			string dummy2;
			FLExBridgeHelper.FLExJumpUrlChanged += JumpToFlexObject;
			var success = FLExBridgeHelper.LaunchFieldworksBridge(Path.Combine(Cache.ProjectId.ProjectFolder, Cache.ProjectId.Name + FdoFileHelper.ksFwDataXmlFileExtension),
								   SendReceiveUser,
								   FLExBridgeHelper.ConflictViewer,
								   null, FDOBackendProvider.ModelVersion, "0.13", null,
								   out dummy1, out dummy2);
			if (!success)
			{
				FLExBridgeHelper.FLExJumpUrlChanged -= JumpToFlexObject;
				ReportDuplicateBridge();
			}
			return true;
		}

		#endregion View Messages (for full FLEx data only) messages

		#region View Lexicon Messages (for Lift data only) messages

		/// <summary>
		/// Determine whether or not to display the Send/Receive->"View Lexicon Messages" menu item.
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayViewLiftMessages(object parameters, ref UIItemDisplayProperties display)
		{
			CheckForFlexBridgeInstalledAndSetMenuItemProperties(display);
			if (!display.Enabled)
				return true; // Flex Bridge isn't installed, so the rest doesn't matter.

			display.Enabled = NotesFileIsPresent(Cache, true);

			return true;
		}

		/// <summary>
		/// The method/delegate that gets invoked when Send/Receive->"View Lexicon Messages" is clicked
		/// </summary>
		/// <param name="commandObject">Includes the XML command element of the OnViewLiftMessages message</param>
		/// <returns>true if the message was handled, false if there was an error or the call was deemed inappropriate.</returns>
		public bool OnViewLiftMessages(object commandObject)
		{
			bool dummy1;
			string dummy2;
			FLExBridgeHelper.FLExJumpUrlChanged += JumpToFlexObject;
			var success = FLExBridgeHelper.LaunchFieldworksBridge(
				GetFullProjectFileName(),
				SendReceiveUser,
				FLExBridgeHelper.LiftConflictViewer,
				null, FDOBackendProvider.ModelVersion, "0.13", null,
				out dummy1, out dummy2);
			if (!success)
			{
				FLExBridgeHelper.FLExJumpUrlChanged -= JumpToFlexObject;
				ReportDuplicateBridge();
			}
			return true;
		}

		#endregion View Messages (for full FLEx data only) messages

		#region Chorus Help messages

		/// <summary>
		/// Checks if the Send/Receive->"_Help..." menu is to be enabled.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayShowChorusHelp(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckForFlexBridgeInstalledAndSetMenuItemProperties(display);

			display.Enabled = display.Enabled && File.Exists(ChorusHelpFile);

			return true; // We dealt with it.
		}

		/// <summary>
		/// Handles the OnShowChorusHelp Mediator message for the Send/Receive->"_About..." menu.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnShowChorusHelp(object argument)
		{
			try
			{
				// When the help window is closed it will return focus to the window that opened it (see MSDN
				// documentation for HtmlHelp()). We don't want to use the main window as the parent, because if
				// a modal dialog is visible, it will still return focus to the main window, allowing the main window
				// to perform some behaviors (such as refresh by pressing F5) while the modal dialog is visible,
				// which can be bad. So, we just create a dummy control and pass that in as the parent.
				Help.ShowHelp(new Control(), ChorusHelpFile);
			}
			catch (Exception)
			{
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotLaunchX, ChorusHelpFile), LexTextStrings.ksError);
			}

			return true; // We dealt with it.
		}

		#endregion Chorus Help messages

		#endregion

		#region Lift methods

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
			var projectFolder = Cache.ProjectId.ProjectFolder;
			var liftProjectDir = GetLiftRepositoryFolderFromFwProjectFolder(projectFolder);
			// It is fine to try the repo move if the liftProjectDir exists, but *only* if it is completely empty.
			// Mercurial can't do a clone into a folder that has contents of any sort.
			if (Directory.Exists(liftProjectDir) && (Directory.GetDirectories(liftProjectDir).Length > 0 || Directory.GetFiles(liftProjectDir).Length > 0))
			{
				return true;
			}

			bool dummyDataChanged;
			// flexbridge -p <path to fwdata file> -u <username> -v move_lift -g Langprojguid
			var success = FLExBridgeHelper.LaunchFieldworksBridge(
				GetFullProjectFileName(),
				SendReceiveUser,
				FLExBridgeHelper.MoveLift,
				Cache.LanguageProject.Guid.ToString().ToLowerInvariant(), FDOBackendProvider.ModelVersion, "0.13", null,
				out dummyDataChanged, out _liftPathname); // _liftPathname will be null, if no repo was moved.
			if (!success)
			{
				ReportDuplicateBridge();
				_liftPathname = null;
				return false;
			}

			return true;
		}

		/// <summary>
		/// Reregisters an import failure, if needed, otherwise clears the token.
		/// </summary>
		/// <returns>'true' if the import failure continues, otherwise 'false'.</returns>
		internal bool RepeatPriorFailedImportIfNeeded()
		{
			var projectFolder = Cache.ProjectId.ProjectFolder;
			var liftProjectDir = GetLiftRepositoryFolderFromFwProjectFolder(projectFolder);
			if (!Directory.Exists(liftProjectDir))
				return false;

			_liftPathname = GetLiftPathname(liftProjectDir);
			if (_liftPathname == null)
				return false;

			var previousImportStatus = LiftImportFailureServices.GetFailureStatus(liftProjectDir);
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

		private bool DoMercilessLiftImport(bool dataChanged)
		{
			var projectFolder = Cache.ProjectId.ProjectFolder;
			var liftProjectDir = GetLiftRepositoryFolderFromFwProjectFolder(projectFolder);
			if (dataChanged)
			{
				if (!ImportLiftCommon(FlexLiftMerger.MergeStyle.MsKeepOnlyNew))
				{
					return false;
				}

				/* TODO: What to do with Lift conflicts?
								var conflictOccurred = DetectMainConflicts(projectFolder, savedState);
								var app = (LexTextApp)m_mediator.PropertyTable.GetValue("App");
								var newAppWindow = RefreshCacheWindowAndAll(app, m_liftPathname);

								if (conflictOccurred)
								{
									// TODO: send a message for the reopened instance to display the message report, we have been disposed by now
									// TODO: Need a new message for Lift conflicts.
									// TODO: Even more importantly, the URLs in the lift notes files aren't compatible with what comes in for regular FW conflict reports
									//newAppWindow.Mediator.SendMessage("ViewLiftMessages", null);
								}
				*/
			}

			return true;
		}

		/// <summary>
		/// The user that we want FLExBridge (and MessageSlice) to consider to be the current user,
		/// for the purposes of identifying the source of Send/Receive changes and Notes.
		/// (Note that FlexBridge may override this with the name the user said to use for S/R.
		/// We don't have easy access to that information, which is stored in the Mercurial INI file.
		/// This unmodified version is an initial default, and also the only value available to MessageSlice.
		/// We may be able to reunite the two notions when FlexBridge is merged.)
		/// </summary>
		public static string SendReceiveUser
		{
			get { return Environment.UserName; }
		}

		/// <summary>
		/// We don't want the parser running, and perhaps making changes in the background, during any kind of S/R.
		/// </summary>
		private void StopParser()
		{
			if (_mediator != null)
				_mediator.SendMessage("StopParser", null);
		}

		/// <summary>
		/// Do the S/R. This *may* actually create the Lift repository, if it doesn't exist, or it may do a more normal S/R
		/// </summary>
		/// <returns>'true' if the S/R succeed, otherwise 'false'.</returns>
		private bool DoSendReceiveForLift(string fullProjectFileName, out bool dataChanged)
		{
			StopParser();
			var projectFolder = Cache.ProjectId.ProjectFolder;
			var liftProjectDir = GetLiftRepositoryFolderFromFwProjectFolder(projectFolder);
			if (!Directory.Exists(liftProjectDir))
			{
				Directory.CreateDirectory(liftProjectDir);
			}
			_liftPathname = GetLiftPathname(liftProjectDir);
			var savedState = PrepareToDetectLiftConflicts(liftProjectDir);
			string dummy;
			// flexbridge -p <path to fwdata/fwdb file> -u <username> -v send_receive_lift
			var success = FLExBridgeHelper.LaunchFieldworksBridge(
				fullProjectFileName,
				SendReceiveUser,
				FLExBridgeHelper.SendReceiveLift, // May create a new lift repo in the process of doing the S/R. Or, it may just use the extant lift repo.
				null, FDOBackendProvider.ModelVersion, "0.13", Cache.LangProject.DefaultVernacularWritingSystem.Id,
				out dataChanged, out dummy);
			if (!success)
			{
				ReportDuplicateBridge();
				dataChanged = false;
				_liftPathname = null;
				return false;
			}

			_liftPathname = GetLiftPathname(liftProjectDir);

			if (_liftPathname == null)
			{
				dataChanged = false; // If there is no lift file, there cannot be any new data.
				return false;
			}

			return true;
		}

		internal static string GetLiftRepositoryFolderFromFwProjectFolder(string projectFolder)
		{
			var otherDir = Path.Combine(projectFolder, FdoFileHelper.OtherRepositories);
			if (Directory.Exists(otherDir))
			{
				var extantOtherFolders = Directory.GetDirectories(otherDir);
				var extantLiftFolder = extantOtherFolders.FirstOrDefault(folder => folder.EndsWith("_LIFT"));
				if (extantLiftFolder != null)
					return extantLiftFolder; // Reuse the old one, no matter what the new project dir name is.
			}

			var flexProjName = Path.GetFileName(projectFolder);
			return Path.Combine(projectFolder, FdoFileHelper.OtherRepositories, flexProjName + '_' + FLExBridgeHelper.LIFT);
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
			using (var importer = new FLExBridgeListener())
			{
				importer.Cache = cache;
				importer._liftPathname = liftPath;
				importer._parentForm = parentForm;
				return importer.ImportLiftCommon(FlexLiftMerger.MergeStyle.MsKeepBoth); // should be a new project
			}
		}

		private static string GetLiftPathname(string liftBaseDir)
		{
			// Part 2 of the LT-14809 fix is to test for the existance of the lift folder.
			// FB will delete it if the S/R was cancelled and Flex had just created the lift folder and file.
			// So don't crash if the folder no longer exists.
			return Directory.Exists(liftBaseDir) ? Directory.GetFiles(liftBaseDir, "*.lift").FirstOrDefault() : null;
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
				using (var progressDlg = new ProgressDialogWithTask(_parentForm))
				{
					_progressDlg = progressDlg;
					try
					{
						if(mergeStyle == FlexLiftMerger.MergeStyle.MsKeepBoth)
						{
							LiftImportFailureServices.RegisterBasicImportFailure(Path.GetDirectoryName(_liftPathname));
						}
						else
						{
							LiftImportFailureServices.RegisterStandardImportFailure(Path.GetDirectoryName(_liftPathname));
						}
						progressDlg.Title = ResourceHelper.GetResourceString("kstidImportLiftlexicon");
						var logFile = (string)progressDlg.RunTask(true, ImportLiftLexicon, new object[] { _liftPathname, mergeStyle });
						if(logFile != null)
						{
							LiftImportFailureServices.ClearImportFailure(Path.GetDirectoryName(_liftPathname));
							return true;
						}
						LiftImportFailureServices.DisplayLiftFailureNoticeIfNecessary(_parentForm, _liftPathname);
						return false;
					}
					catch (WorkerThreadException error)
					{
						// It appears to be an analyst issue to sort out how we should report this.
						// LT-12340 however says we must report it somehow.
						var sMsg = String.Format(LexEdStrings.kProblemImportWhileMerging, _liftPathname, error.InnerException.Message);

						MessageBoxUtils.Show(sMsg, LexEdStrings.kProblemMerging, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
		/// This is the file that our Message slice is configured to look for in the root project folder.
		/// The actual Lexicon.fwstub doesn't contain anything.
		/// Lexicon.fwstub.ChorusNotes contains notes about lexical entries.
		/// </summary>
		public const string FakeLexiconFileName = "Lexicon.fwstub";
		/// <summary>
		/// This is the file that actually holds the chorus notes for the lexicon.
		/// </summary>
		public const string FlexLexiconNotesFileName = FakeLexiconFileName + "." + kChorusNotesExtension;
		public string FlexNotesPath
		{
			get { return Path.Combine(Cache.ProjectId.ProjectFolder, FlexLexiconNotesFileName); }
		}
		public string LiftNotesPath
		{
			get { return _liftPathname + "." + kChorusNotesExtension; }
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

			if (File.Exists(LiftNotesPath))
			{

				using (var reader = new StreamReader(LiftNotesPath, Encoding.UTF8))
				using (var writer = new StreamWriter(FlexNotesPath, false, Encoding.UTF8))
				{
					ConvertLiftNotesToFlex(reader, writer, Path.GetFileName(_liftPathname));
				}
			}

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
				using (var progressDlg = new ProgressDialogWithTask(_parentForm))
				{
					_progressDlg = progressDlg;
					try
					{
						progressDlg.Title = ResourceHelper.GetResourceString("kstidExportLiftLexicon");
						var outPath = (string)progressDlg.RunTask(true, ExportLiftLexicon, null);
						var retval = (!String.IsNullOrEmpty(outPath));
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
		}

		internal const string kChorusNotesExtension = "ChorusNotes";
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
				var projectFolder = Cache.ProjectId.ProjectFolder;
				var liftProjectDir = GetLiftRepositoryFolderFromFwProjectFolder(projectFolder);
				if (!Directory.Exists(liftProjectDir))
				{
					Directory.CreateDirectory(liftProjectDir);
				}
				if (String.IsNullOrEmpty(_liftPathname))
				{
					_liftPathname = Path.Combine(liftProjectDir, Cache.ProjectId.Name + ".lift");
				}
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
				using (TextWriter textWriter = new StreamWriter(_liftPathname))
				{
					exporter.ExportLift(textWriter, Path.GetDirectoryName(_liftPathname));
				}
				LiftSorter.SortLiftFile(_liftPathname);

				//Output the Ranges file
				var outPathRanges = Path.ChangeExtension(_liftPathname, @"lift-ranges");
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

		/// <summary>
		/// Convert FLEx ChorusNotes file referencing lex entries to LIFT notes by adjusting the "ref" attributes.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="writer"></param>
		/// <param name="liftFileName"></param>
		static internal void ConvertFlexNotesToLift(TextReader reader, TextWriter writer, string liftFileName)
		{
			// Typical input is something like
			// silfw://localhost/link?app=flex&amp;database=current&amp;server=&amp;tool=default&amp;guid=bab7776e-531b-4ce1-997f-fa638c09e381&amp;tag=&amp;id=bab7776e-531b-4ce1-997f-fa638c09e381&amp;label=Entry &quot;pintu&quot;
			var reRef = new Regex(".*guid=([^&]*)&.*label=(.*)");
			//produce: lift://John.lift?type=entry&amp;label=fox&amp;id=f3093b9b-ea2f-422b-86b6-0defaa4646fe
			var outputTemplate = "lift://{0}?type=entry&amp;label={1}&amp;id={2}";
			ConvertRefAttrs(reader, writer, liftFileName, outputTemplate);
		}

		/// <summary>
		/// Convert LIFT ChorusNotes file to FLEx notes by adjusting the "ref" attributes.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="writer"></param>
		/// <param name="liftFileName"></param>
		static internal void ConvertLiftNotesToFlex(TextReader reader, TextWriter writer, string liftFileName)
		{
			// Typical input is something like lift://John.lift?type=entry&amp;label=fox&amp;id=f3093b9b-ea2f-422b-86b6-0defaa4646fe
			var reRef = new Regex(".*id=([^&]*)&.*label=(.*)");
			//produce:
			// silfw://localhost/link?app=flex&amp;database=current&amp;server=&amp;tool=default&amp;guid=bab7776e-531b-4ce1-997f-fa638c09e381&amp;tag=&amp;id=bab7776e-531b-4ce1-997f-fa638c09e381&amp;label=Entry &quot;pintu&quot;
			var outputTemplate = "silfw://localhost/link?app=flex&amp;database=current&amp;server=&amp;tool=default&amp;guid={2}&amp;tag=&amp;id={2}&amp;label={1}";
			ConvertRefAttrs(reader, writer, "", outputTemplate);
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

		private bool CanUndoLiftExport
		{
			get
			{
				var liftProjectFolder = GetLiftRepositoryFolderFromFwProjectFolder(Cache.ProjectId.ProjectFolder);
				return Directory.Exists(liftProjectFolder) && Directory.Exists(Path.Combine(liftProjectFolder, ".hg"));
			}
		}

		private void UndoExport()
		{
			bool dataChanged;
			string dummy;
			// Have FLEx Bridge do its 'undo'
			// flexbridge -p <project folder name> #-u username -v undo_export_lift)
			FLExBridgeHelper.LaunchFieldworksBridge(Cache.ProjectId.ProjectFolder, SendReceiveUser,
				FLExBridgeHelper.UndoExportLift, null, FDOBackendProvider.ModelVersion, "0.13", null,
				out dataChanged, out dummy);
		}

		#endregion

		private string ChorusHelpFile
		{
			get
			{
				return Path.Combine(Path.GetDirectoryName(FLExBridgeHelper.FullFieldWorksBridgePath()), "Chorus_Help.chm");
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="tempWindow is a reference")]
		private bool ChangeProjectNameIfNeeded()
		{
			// Enhance GJM: When Hg is upgraded to work with non-Ascii filenames, this section can be removed.
			if (Unicode.CheckForNonAsciiCharacters(Cache.ProjectId.Name))
			{
				var revisedProjName = Unicode.RemoveNonAsciiCharsFromString(Cache.ProjectId.Name);
				if (revisedProjName == string.Empty)
				{
					// The whole pre-existing project name is non-Ascii characters!
					DisplayAllNonAsciiComplaint();
					return true;
				}
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
					var fullProjectFileName = Path.Combine(projectFolder, revisedProjName + FdoFileHelper.ksFwDataXmlFileExtension);
					var tempWindow = RefreshCacheWindowAndAll(app, fullProjectFileName);
					tempWindow.Mediator.SendMessageDefered("FLExBridge", null);
					// to hopefully come back here after resetting things
				}
				return true;
			}
			return false;
		}

		/// <summary>
		/// Returns true if there are any Chorus Notes to view in the main FW repo or in the Lift repo.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="checkForLiftNotes">
		/// When 'false', then don't consider any Lift notes files in considering those present.
		/// When 'true', then skip any Flex notes, and only consider the Lift notes.
		/// </param>
		/// <returns>'true' if there are any Chorus Notes files at the given level. Otherwise, it returns 'false'.</returns>
		private static bool NotesFileIsPresent(FdoCache cache, bool checkForLiftNotes)
		{
			// Default to look for notes in the main FW repo.
			var folderToSearchIn = cache.ProjectId.ProjectFolder;
			var liftFolder = GetLiftRepositoryFolderFromFwProjectFolder(folderToSearchIn);
			if (checkForLiftNotes)
			{
				if (!Directory.Exists(liftFolder))
					return false; // If the folder doesn't even exist, there can't be any lift notes.

				// Switch to look for note files in the Lift repo.
				folderToSearchIn = liftFolder;
			}

			if (!Directory.Exists(Path.Combine(folderToSearchIn, ".hg")))
				return false; // No repo, so there can be no notes files.

			foreach (string notesPathname in Directory.GetFiles(folderToSearchIn, "*.ChorusNotes", SearchOption.AllDirectories))
			{
				if (!NotesFileHasContent(notesPathname))
					continue; // Skip ones with no content.

				if (checkForLiftNotes)
					return true;

				if (!notesPathname.Contains(liftFolder)) // Skip any lift ones down in a nested repo.
					return true;

				// Must be a nested lift one to get here, so try another one.
			}
			return false;
		}

		private static bool NotesFileHasContent(string chorusNotesPathname)
		{
			var doc = XDocument.Load(chorusNotesPathname);
			return doc.Root.HasElements; // Files with no notes (e.g., "Lexicon.fwstub.ChorusNotes") are not interesting.
		}

		private static void ReportDuplicateBridge()
		{
			ObtainProjectMethod.ReportDuplicateBridge();
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

		private void DisplayAllNonAsciiComplaint()
		{
			MessageBox.Show(LexEdStrings.ksAllNonAsciiProjectNameWarning, LexEdStrings.ksWarning,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private static bool CheckForExistingFileName(string projectFolder, string revisedFileName)
		{
			if (File.Exists(Path.Combine(projectFolder, revisedFileName + FdoFileHelper.ksFwDataXmlFileExtension)))
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

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "FwApp is a reference I guess")]
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

		private static bool DetectLiftConflicts(string path, Dictionary<string, long> savedState)
		{
			foreach (var file in Directory.GetFiles(path, "*.ChorusNotes", SearchOption.AllDirectories))
			{
				long oldLength;
				savedState.TryGetValue(file, out oldLength);
				if (new FileInfo(file).Length == oldLength)
					continue; // no new notes in this file.
				return true; // Review JohnT: do we need to look in the file to see if what was added is a conflict?
			}
			return false; // no conflicts added.
		}

		private static bool DetectMainConflicts(string path, Dictionary<string, long> savedState)
		{
			foreach (var file in Directory.GetFiles(path, "*.ChorusNotes", SearchOption.AllDirectories))
			{
				// TODO: Test to see if one conflict tool can do both FLEx and LIFT conflicts.
				if (file.Contains(FdoFileHelper.OtherRepositories))
					continue; // Skip them, since they are part of some other repository.

				long oldLength;
				savedState.TryGetValue(file, out oldLength);
				if (new FileInfo(file).Length == oldLength)
					continue; // no new notes in this file.
				return true; // Review JohnT: do we need to look in the file to see if what was added is a conflict?
			}
			return false; // no conflicts added.
		}

		/// <summary>
		/// This is only used for the main FW repo, so it excludes any notes in a lower level repo.
		/// </summary>
		/// <param name="projectFolder"></param>
		/// <returns></returns>
		private static Dictionary<string, long> PrepareToDetectMainConflicts(string projectFolder)
		{
			var result = new Dictionary<string, long>();
			foreach (var file in Directory.GetFiles(projectFolder, "*.ChorusNotes", SearchOption.AllDirectories))
			{
				if (file.Contains(FdoFileHelper.OtherRepositories))
					continue; // Skip them, since they are part of some other repository.

				result[file] = new FileInfo(file).Length;
			}
			return result;
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
				return result;
			foreach (var file in Directory.GetFiles(liftPath, "*.ChorusNotes", SearchOption.AllDirectories))
			{
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

		private bool ShowMessageBeforeFirstSendReceive_IsUserReady()
		{
			using (var FirstTimeDlg = new FLExBridgeFirstSendReceiveInstructionsDlg(_mediator.HelpTopicProvider))
			{
				return DialogResult.OK == FirstTimeDlg.ShowDialog();
			}
		}

		private static void CheckForFlexBridgeInstalledAndSetMenuItemProperties(UIItemDisplayProperties display)
		{
			display.Enabled = FLExBridgeHelper.IsFlexBridgeInstalled();
			display.Visible = true; // Always visible. Cf. LT-13885
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

			if (IsDisposed)
				return;

			if (fDisposing)
			{
				// dispose managed and unmanaged objects
				FLExBridgeHelper.FLExJumpUrlChanged -= JumpToFlexObject;
				if (_mediator != null) // Fixes LT-14201
					_mediator.RemoveColleague(this);
			}
			_liftPathname = null;
			_mediator = null;
			_parentForm = null;
			_progressDlg = null;

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
