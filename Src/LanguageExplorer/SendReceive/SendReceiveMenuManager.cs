// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorer.SendReceive
{
	/// <summary>
	/// Handle the menus on the global Send/Receive menu.
	/// </summary>
	internal sealed class SendReceiveMenuManager : IFlexComponent
	{
		private readonly Dictionary<string, IBridge> _bridges = new Dictionary<string, IBridge>(2);
		private IdleQueue IdleQueue { get; set; }
		private IFwMainWnd MainWindow { get; set; }
		private IFlexApp FlexApp { get; set; }
		private LcmCache Cache { get; set; }
		private ToolStripMenuItem MainSendReceiveToolStripMenuItem { get; set; }
		private ToolStripButton ToolStripButtonFlexLiftBridge { get; set; }
		private ToolStripMenuItem _helpChorusMenu;
		private ToolStripMenuItem _checkForFlexBridgeUpdatesMenu;
		private ToolStripMenuItem _helpAboutFLEXBridgeMenu;
		/// <summary>
		/// This is the file that our Message slice is configured to look for in the root project folder.
		/// The actual Lexicon.fwstub doesn't contain anything.
		/// Lexicon.fwstub.ChorusNotes contains notes about lexical entries.
		/// </summary>
		public const string FakeLexiconFileName = "Lexicon.fwstub";
		/// <summary>
		/// This is the file that actually holds the chorus notes for the lexicon.
		/// </summary>
		public const string FlexLexiconNotesFileName = FakeLexiconFileName + CommonBridgeServices.kChorusNotesExtension;

		/// <summary />
		internal SendReceiveMenuManager(IdleQueue idleQueue, IFwMainWnd mainWindow, IFlexApp flexApp, LcmCache cache, ToolStripMenuItem mainSendReceiveToolStripMenuItem, ToolStripButton toolStripButtonFlexLiftBridge)
		{
			Guard.AgainstNull(idleQueue, nameof(idleQueue));
			Guard.AgainstNull(mainWindow, nameof(mainWindow));
			Guard.AgainstNull(flexApp, nameof(flexApp));
			Guard.AgainstNull(cache, nameof(cache));
			Guard.AgainstNull(mainSendReceiveToolStripMenuItem, nameof(mainSendReceiveToolStripMenuItem));
			Guard.AgainstNull(toolStripButtonFlexLiftBridge, nameof(toolStripButtonFlexLiftBridge));

			IdleQueue = idleQueue;
			MainWindow = mainWindow;
			FlexApp = flexApp;
			Cache = cache;
			MainSendReceiveToolStripMenuItem = mainSendReceiveToolStripMenuItem;
			ToolStripButtonFlexLiftBridge = toolStripButtonFlexLiftBridge;

			_bridges.Add(CommonBridgeServices.FLExBridge, new FlexBridge(Cache, FlexApp));
			_bridges.Add(CommonBridgeServices.LiftBridge, new LiftBridge(Cache, MainWindow, FlexApp));
			_bridges.Add(CommonBridgeServices.NoBridgeUsedYet, null);
		}

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

		#region Implentation of IFlexComponent

		/// <inheritdoc />
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			_bridges[CommonBridgeServices.FLExBridge].InitializeFlexComponent(flexComponentParameters);
			_bridges[CommonBridgeServices.LiftBridge].InitializeFlexComponent(flexComponentParameters);

			Setup();
		}

		#endregion

		private void Setup()
		{
			MainSendReceiveToolStripMenuItem.Enabled = FLExBridgeHelper.IsFlexBridgeInstalled();
			ToolStripButtonFlexLiftBridge.Enabled = MainSendReceiveToolStripMenuItem.Enabled;
			if (!MainSendReceiveToolStripMenuItem.Enabled)
			{
				return;
			}
			MainSendReceiveToolStripMenuItem.DropDownOpening += MainSendReceiveToolStripMenuItem_DropDownOpening;
			// Add all of the menus to MainSendReceiveToolStripMenuItem.
			var flexBridge = _bridges[CommonBridgeServices.FLExBridge];
			var liftBridge = _bridges[CommonBridgeServices.LiftBridge];
			flexBridge.InstallMenus(BridgeMenuInstallRound.One, MainSendReceiveToolStripMenuItem);
			liftBridge.InstallMenus(BridgeMenuInstallRound.One, MainSendReceiveToolStripMenuItem);
			MainSendReceiveToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
			flexBridge.InstallMenus(BridgeMenuInstallRound.Two, MainSendReceiveToolStripMenuItem);
			liftBridge.InstallMenus(BridgeMenuInstallRound.Two, MainSendReceiveToolStripMenuItem);
			MainSendReceiveToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
			flexBridge.InstallMenus(BridgeMenuInstallRound.Three, MainSendReceiveToolStripMenuItem);
			liftBridge.InstallMenus(BridgeMenuInstallRound.Three, MainSendReceiveToolStripMenuItem);
			MainSendReceiveToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
			AddCommonMenuItems();


			// Add standard toolbar item's event handler and enable/disable it.
			ToolStripButtonFlexLiftBridge.Enabled = MainSendReceiveToolStripMenuItem.Enabled; // If 'true' it may be reset to false, below.
			if (ToolStripButtonFlexLiftBridge.Enabled)
			{
				var lastBridgeUsed = GetLastBridge();
				if (lastBridgeUsed == null)
				{
					ToolStripButtonFlexLiftBridge.Enabled = false;
				}
				else
				{
					switch (lastBridgeUsed.Name)
					{
						case CommonBridgeServices.FLExBridge:
							// If Fix it app does not exist, then disable main FLEx S/R, since FB needs to call it, after a merge.
							// If !IsConfiguredForSR (failed the first time), disable the button and hotkey
							ToolStripButtonFlexLiftBridge.Enabled = ToolStripButtonFlexLiftBridge.Enabled && FLExBridgeHelper.FixItAppExists && CommonBridgeServices.IsConfiguredForSR(Cache.ProjectId.ProjectFolder);
							break;
						case CommonBridgeServices.LiftBridge:
							// If !IsConfiguredForLiftSR (failed first time), disable the button and hotkey
							ToolStripButtonFlexLiftBridge.Enabled = ToolStripButtonFlexLiftBridge.Enabled && IsConfiguredForLiftSR(Cache.ProjectId.ProjectFolder);
							break;
						case CommonBridgeServices.NoBridgeUsedYet: // Fall through. This isn't really needed, but it is a clearer that it covers the case.
						default:
							ToolStripButtonFlexLiftBridge.Enabled = false;
							break;
					}
				}
			}
			// Re-check, since it may have been disabled in the above code.
			if (ToolStripButtonFlexLiftBridge.Enabled)
			{
				ToolStripButtonFlexLiftBridge.Click += Flex_Or_Lift_Bridge_Clicked;
			}
		}

		private void AddCommonMenuItems()
		{
			_helpChorusMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(MainSendReceiveToolStripMenuItem, HelpChorus_Click, SendReceiveResources.HelpChorus, SendReceiveResources.HelpChorusToolTip);

			if (!MiscUtils.IsUnix)
			{
				_checkForFlexBridgeUpdatesMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(MainSendReceiveToolStripMenuItem, CheckForFlexBridgeUpdates_Click, SendReceiveResources.CheckForFlexBridgeUpdates, SendReceiveResources.CheckForFlexBridgeUpdatesToolTip);
			}

			_helpAboutFLEXBridgeMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(MainSendReceiveToolStripMenuItem, HelpAboutFLEXBridge_Click, SendReceiveResources.HelpAboutFLEXBridge, SendReceiveResources.HelpAboutFLEXBridgeToolTip);
		}

		private void HelpAboutFLEXBridge_Click(object sender, EventArgs e)
		{
			bool dummy1;
			string dummy2;
			FLExBridgeHelper.LaunchFieldworksBridge(
				CommonBridgeServices.GetFullProjectFileName(Cache),
				CommonBridgeServices.SendReceiveUser,
				FLExBridgeHelper.AboutFLExBridge,
				null, LcmCache.ModelVersion, CommonBridgeServices.LiftModelVersion, null, null,
				out dummy1, out dummy2);
		}

		private void CheckForFlexBridgeUpdates_Click(object sender, EventArgs e)
		{
			bool dummy1;
			string dummy2;
			FLExBridgeHelper.LaunchFieldworksBridge(
				CommonBridgeServices.GetFullProjectFileName(Cache),
				CommonBridgeServices.SendReceiveUser,
				FLExBridgeHelper.CheckForUpdates,
				null, LcmCache.ModelVersion, CommonBridgeServices.LiftModelVersion, null, null,
				out dummy1, out dummy2);
		}

		private void HelpChorus_Click(object sender, EventArgs eventArgs)
		{
			var chorusHelpPathname = Path.Combine(Path.GetDirectoryName(FLExBridgeHelper.FullFieldWorksBridgePath()), "Chorus_Help.chm");
			try
			{
				// When the help window is closed it will return focus to the window that opened it (see MSDN
				// documentation for HtmlHelp()). We don't want to use the main window as the parent, because if
				// a modal dialog is visible, it will still return focus to the main window, allowing the main window
				// to perform some behaviors (such as refresh by pressing F5) while the modal dialog is visible,
				// which can be bad. So, we just create a dummy control and pass that in as the parent.
				using (var dummyParent = new Control())
				{
					Help.ShowHelp(dummyParent, chorusHelpPathname);
				}
			}
			catch (Exception)
			{
				MessageBox.Show((Form)MainWindow, string.Format(LanguageExplorerResources.ksCannotLaunchX, chorusHelpPathname), LanguageExplorerResources.ksError);
			}
		}

		private void MainSendReceiveToolStripMenuItem_DropDownOpening(object sender, EventArgs eventArgs)
		{
			_bridges[CommonBridgeServices.FLExBridge].SetEnabledStatus();
			_bridges[CommonBridgeServices.LiftBridge].SetEnabledStatus();
		}

		private IBridge GetLastBridge()
		{
			return _bridges[PropertyTable.GetValue(CommonBridgeServices.LastBridgeUsed, SettingsGroup.LocalSettings, CommonBridgeServices.NoBridgeUsedYet)];
		}

		private void Flex_Or_Lift_Bridge_Clicked(object sender, EventArgs e)
		{
			IBridge lastBridgeRun = GetLastBridge();
			// Process the event for the toolbar button that does S/R for the last repo that was done.
			if (MiscUtils.IsMono)
			{
				// This is a horrible workaround for a nasty bug in Mono. The toolbar button captures the mouse,
				// and does not release it before calling this event handler. If we proceed to run the bridge,
				// which freezes our UI thread until FlexBridge returns, the mouse stays captured...and the whole
				// system UI is frozen, for all applications.
				IdleQueue.Add(IdleQueuePriority.High, obj =>
				{
					lastBridgeRun.RunBridge();
					return true; // IdleQueue requires the function to have a boolean return value.
				});
			}
			else
			{
				// on windows we can safely do it right away.
				lastBridgeRun.RunBridge();
			}
		}

		internal static bool IsConfiguredForLiftSR(string folder)
		{
			var otherRepoPath = Path.Combine(folder, LcmFileHelper.OtherRepositories);
			if (!Directory.Exists(otherRepoPath))
			{
				return false;
			}
			var liftFolder = Directory.EnumerateDirectories(otherRepoPath, "*_LIFT").FirstOrDefault();
			return !string.IsNullOrEmpty(liftFolder) && CommonBridgeServices.IsConfiguredForSR(liftFolder);
		}

		#region IDisposable
		private bool _isDisposed;

		~SendReceiveMenuManager()
		{
			Dispose(false);
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (_isDisposed)
			{
				return;
			}

			if (disposing)
			{
				_helpChorusMenu.Click -= HelpChorus_Click;
				_helpChorusMenu.Dispose();
				if (_checkForFlexBridgeUpdatesMenu != null)
				{
					_checkForFlexBridgeUpdatesMenu.Click -= CheckForFlexBridgeUpdates_Click;
					_checkForFlexBridgeUpdatesMenu.Dispose();
				}
				_helpAboutFLEXBridgeMenu.Click -= HelpAboutFLEXBridge_Click;
				_helpAboutFLEXBridgeMenu.Dispose();

				foreach (var bridge in _bridges.Values)
				{
					// "NoBridgeUsedYet" key will have  null value in the dictionary, so skip it.
					bridge?.Dispose();
				}
				_bridges.Clear();
			}

			IdleQueue = null;
			MainWindow = null;
			FlexApp = null;
			Cache = null;
			MainSendReceiveToolStripMenuItem = null;
			ToolStripButtonFlexLiftBridge = null;
			_helpChorusMenu = null;
			_checkForFlexBridgeUpdatesMenu = null;
			_helpAboutFLEXBridgeMenu = null;

			_isDisposed = true;
		}
		#endregion
	}
}
