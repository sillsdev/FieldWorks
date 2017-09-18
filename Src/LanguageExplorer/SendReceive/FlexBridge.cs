// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.IO;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.SendReceive
{
	internal sealed class FlexBridge : IBridge
	{
		private LcmCache Cache { get; set; }
		private IFlexApp FlexApp { get; set; }
		private ToolStripMenuItem _mainSendReceiveMenu;
		private ToolStripMenuItem _viewMessagesMenu;
		private ToolStripMenuItem _obtainAnyFlexBridgeProjectMenu;
		private ToolStripMenuItem _sendFlexBridgeFirstTimeProjectMenu;

		internal FlexBridge(LcmCache cache, IFlexApp flexApp)
		{
			Guard.AgainstNull(cache, nameof(cache));
			Guard.AgainstNull(flexApp, nameof(flexApp));

			Cache = cache;
			FlexApp = flexApp;
		}

		#region Implementation of IBridge
		/// <inheritdoc />
		public string Name => CommonBridgeServices.FLExBridge;

		/// <inheritdoc />
		public void RunBridge()
		{
			CommonBridgeServices.PrepareForSR(PropertyTable, Publisher, Cache, this);

			if (!LcmFileHelper.GetDefaultLinkedFilesDir(Cache.ServiceLocator.DataSetup.ProjectId.ProjectFolder).Equals(Cache.LanguageProject.LinkedFilesRootDir))
			{
				using (var dlg = new WarningNotUsingDefaultLinkedFilesLocation(FlexApp))
				{
					var result = dlg.ShowDialog();
					if (result == DialogResult.Yes)
					{
						var sLinkedFilesRootDir = Cache.LangProject.LinkedFilesRootDir;
						NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
						{
							Cache.LangProject.LinkedFilesRootDir = LcmFileHelper.GetDefaultLinkedFilesDir(Cache.ProjectId.ProjectFolder);
						});
						FlexApp.UpdateExternalLinks(sLinkedFilesRootDir);
					}
				}
			}
			// Make sure that there aren't multiple applications accessing the project
			// It is possible for a user to start up an application that accesses the
			// project after this check, but the application should not interfere with
			// the S/R operation.
			while (SharedBackendServices.AreMultipleApplicationsConnected(Cache))
			{
				if (ThreadHelper.ShowMessageBox(null, LanguageExplorerResources.ksSendReceiveNotPermittedMultipleAppsText, LanguageExplorerResources.ksSendReceiveNotPermittedMultipleAppsCaption, MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
				{
					return;
				}
			}

			string url;
			var projectFolder = Cache.ProjectId.ProjectFolder;
			var savedState = PrepareToDetectMainConflicts(projectFolder);
			var fullProjectFileName = Path.Combine(projectFolder, Cache.ProjectId.Name + LcmFileHelper.ksFwDataXmlFileExtension);
			bool dataChanged;
			using (CopyDictionaryConfigFileToTemp(projectFolder))
			{
				string dummy;
				var success = FLExBridgeHelper.LaunchFieldworksBridge(fullProjectFileName, CommonBridgeServices.SendReceiveUser, FLExBridgeHelper.SendReceive,
					null, LcmCache.ModelVersion, CommonBridgeServices.LiftModelVersion,
					Cache.LangProject.DefaultVernacularWritingSystem.Id, null,
					out dataChanged, out dummy);
				if (!success)
				{
					CommonBridgeServices.ReportDuplicateBridge();
					ProjectLockingService.LockCurrentProject(Cache);
					return;
				}
			}
			if (dataChanged)
			{
				var conflictOccurred = CommonBridgeServices.DetectMainConflicts(projectFolder, savedState);
				var newAppWindow = CommonBridgeServices.RefreshCacheWindowAndAll(FlexApp, PropertyTable.GetValue("UseVernSpellingDictionary", true), fullProjectFileName);
				if (conflictOccurred)
				{
					// Send a message for the reopened instance to display the message viewer (used to be conflict report),
					// we have been disposed by now
					newAppWindow.Publisher.Publish("ViewMessages", null);
				}
			}
			else //Re-lock project if we aren't trying to close the app
			{
				ProjectLockingService.LockCurrentProject(Cache);
			}
		}

		/// <inheritdoc />
		public void InstallMenus(BridgeMenuInstallRound currentInstallRound, ToolStripMenuItem mainSendReceiveToolStripMenuItem)
		{
			switch (currentInstallRound)
			{
				case BridgeMenuInstallRound.One:
					_mainSendReceiveMenu = ToolStripMenuItemFactory.CreateToolStripMenuItem(mainSendReceiveToolStripMenuItem, int.MaxValue, SendReceiveResources.SendReceiveFlexBridge, SendReceiveResources.SendReceiveFlexBridgeToolTip, S_R_FlexBridge_Click, SendReceiveResources.sendReceive16x16);

					_viewMessagesMenu = ToolStripMenuItemFactory.CreateToolStripMenuItem(mainSendReceiveToolStripMenuItem, int.MaxValue, SendReceiveResources.ViewMessagesFlexBridge, SendReceiveResources.ViewMessagesFlexBridgeTooltip, ViewMessages_FlexBridge_Click);
					break;
				case BridgeMenuInstallRound.Two:
					_mainSendReceiveMenu = ToolStripMenuItemFactory.CreateToolStripMenuItem(mainSendReceiveToolStripMenuItem, int.MaxValue, SendReceiveResources.ObtainAnyFlexBridgeProject, SendReceiveResources.ObtainAnyFlexBridgeProjectToolTip, ObtainAnyFlexBridgeProject_Click, SendReceiveResources.SendReceiveGetArrow16x16);
					break;
				case BridgeMenuInstallRound.Three:
					_mainSendReceiveMenu = ToolStripMenuItemFactory.CreateToolStripMenuItem(mainSendReceiveToolStripMenuItem, int.MaxValue, SendReceiveResources.SendFlexBridgeProjectFirstTime, SendReceiveResources.SendFlexBridgeProjectFirstTimeToolTip, SendFlexBridgeFirstTime_Click, SendReceiveResources.sendReceiveFirst16x16);
					break;
			}
		}

		/// <inheritdoc />
		public void SetEnabledStatus()
		{
			_mainSendReceiveMenu.Enabled = CommonBridgeServices.IsConfiguredForSR(Cache.ProjectId.ProjectFolder) && FLExBridgeHelper.FixItAppExists;
			_viewMessagesMenu.Enabled = CommonBridgeServices.NotesFileIsPresent(Cache, false);
			_sendFlexBridgeFirstTimeProjectMenu.Enabled = !CommonBridgeServices.IsConfiguredForSR(Cache.ProjectId.ProjectFolder);
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

			Subscriber.Subscribe("ViewMessages", ViewMessages);
		}
		#endregion

		#region IDisposable
		private bool _isDisposed;

		~FlexBridge()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
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
				Subscriber.Unsubscribe("ViewMessages", ViewMessages);

				_mainSendReceiveMenu.Click -= S_R_FlexBridge_Click;
				_mainSendReceiveMenu.Dispose();

				_viewMessagesMenu.Click -= ViewMessages_FlexBridge_Click;
				_viewMessagesMenu.Dispose();

				_obtainAnyFlexBridgeProjectMenu.Click -= ObtainAnyFlexBridgeProject_Click;
				_obtainAnyFlexBridgeProjectMenu.Dispose();

				_sendFlexBridgeFirstTimeProjectMenu.Click -= SendFlexBridgeFirstTime_Click;
				_sendFlexBridgeFirstTimeProjectMenu.Dispose();
			}
			_mainSendReceiveMenu = null;
			_viewMessagesMenu = null;
			_obtainAnyFlexBridgeProjectMenu = null;
			_sendFlexBridgeFirstTimeProjectMenu = null;
			Cache = null;
			FlexApp = null;

			_isDisposed = true;
		}
		#endregion

		private void SendFlexBridgeFirstTime_Click(object sender, EventArgs e)
		{
			if (CommonBridgeServices.ShowMessageBeforeFirstSendReceive_IsUserReady(FlexApp))
			{
				RunBridge();
			}
		}

		private void ObtainAnyFlexBridgeProject_Click(object sender, EventArgs e)
		{
			ObtainedProjectType obtainedProjectType;
			var newprojectPathname = ObtainProjectMethod.ObtainProjectFromAnySource(PropertyTable.GetValue<Form>("window"), FlexApp, out obtainedProjectType);
			if (string.IsNullOrEmpty(newprojectPathname))
				return; // We dealt with it.

			PropertyTable.SetProperty(CommonBridgeServices.LastBridgeUsed, obtainedProjectType == ObtainedProjectType.Lift ? CommonBridgeServices.LiftBridge : CommonBridgeServices.FLExBridge, SettingsGroup.LocalSettings, true, false);

			var fieldWorksAssembly = Assembly.Load("FieldWorks.exe");
			var fieldWorksType = fieldWorksAssembly.GetType("SIL.FieldWorks.FieldWorks");
			var methodInfo = fieldWorksType.GetMethod("OpenNewProject", BindingFlags.Static | BindingFlags.Public);
			methodInfo.Invoke(null, new object[] { new ProjectId(newprojectPathname) });
		}

		private void S_R_FlexBridge_Click(object sender, EventArgs e)
		{
			RunBridge();
		}

		private void ViewMessages_FlexBridge_Click(object sender, EventArgs e)
		{
			bool dummy1;
			string dummy2;
			FLExBridgeHelper.FLExJumpUrlChanged += JumpToFlexObject;
			var success = FLExBridgeHelper.LaunchFieldworksBridge(Path.Combine(Cache.ProjectId.ProjectFolder, Cache.ProjectId.Name + LcmFileHelper.ksFwDataXmlFileExtension),
				CommonBridgeServices.SendReceiveUser,
				FLExBridgeHelper.ConflictViewer,
				null, LcmCache.ModelVersion, CommonBridgeServices.LiftModelVersion, null, ()=> CommonBridgeServices.BroadcastMasterRefresh(Publisher),
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
		/// This is only used for the main FW repo, so it excludes any notes in a lower level repo.
		/// </summary>
		/// <param name="projectFolder"></param>
		/// <returns></returns>
		private static Dictionary<string, long> PrepareToDetectMainConflicts(string projectFolder)
		{
			var result = new Dictionary<string, long>();
			foreach (var file in Directory.GetFiles(projectFolder, "*.ChorusNotes", SearchOption.AllDirectories))
			{
				if (file.Contains(LcmFileHelper.OtherRepositories))
					continue; // Skip them, since they are part of some other repository.

				result[file] = new FileInfo(file).Length;
			}
			return result;
		}

		// FlexBridge looks for the schema to validate Dictionary Configuration files in the project's Temp directory.
		private static TempFile CopyDictionaryConfigFileToTemp(string projectFolder)
		{
			const string dictConfigSchemaFileName = "DictionaryConfiguration.xsd";
			var dictConfigSchemaPath = Path.Combine(FwDirectoryFinder.FlexFolder, "Configuration", dictConfigSchemaFileName);
			var projectTempFolder = Path.Combine(projectFolder, "Temp");
			var dictConfigSchemaTempPath = Path.Combine(projectTempFolder, dictConfigSchemaFileName);
			if (!Directory.Exists(projectTempFolder))
				Directory.CreateDirectory(projectTempFolder);
			if (File.Exists(dictConfigSchemaTempPath))
			{
				// We've had difficulties in the past trying to delete this file while it's read-only. This may apply only to early testers' projects.
				File.SetAttributes(dictConfigSchemaTempPath, FileAttributes.Normal);
				File.Delete(dictConfigSchemaTempPath);
			}
			File.Copy(dictConfigSchemaPath, dictConfigSchemaTempPath);
			File.SetAttributes(dictConfigSchemaTempPath, FileAttributes.Normal);
			return new TempFile(dictConfigSchemaTempPath, true);
		}

		private void ViewMessages(object obj)
		{
			_viewMessagesMenu.PerformClick();
		}
	}
}