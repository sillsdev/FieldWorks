// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorer.SendReceive
{
	/// <summary>
	/// Handle the menus on the global Send/Receive menu.
	/// </summary>
	internal sealed class SendReceiveMenuManager : IFlexComponent, IDisposable
	{
		private UiWidgetController _uiWidgetController;
		private readonly Dictionary<string, IBridge> _bridges = new Dictionary<string, IBridge>(2);
		private IdleQueue IdleQueue { get; set; }
		private IFwMainWnd MainWindow { get; set; }
		private IFlexApp FlexApp { get; set; }
		private LcmCache Cache { get; set; }
		private ToolStripMenuItem MainSendReceiveToolStripMenuItem { get; set; }
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
		internal SendReceiveMenuManager(IdleQueue idleQueue, IFwMainWnd mainWindow, IFlexApp flexApp, LcmCache cache, UiWidgetController uiWidgetController, ToolStripMenuItem mainSendReceiveToolStripMenuItem)
		{
			Guard.AgainstNull(idleQueue, nameof(idleQueue));
			Guard.AgainstNull(mainWindow, nameof(mainWindow));
			Guard.AgainstNull(flexApp, nameof(flexApp));
			Guard.AgainstNull(cache, nameof(cache));
			Guard.AgainstNull(uiWidgetController, nameof(uiWidgetController));

			IdleQueue = idleQueue;
			MainWindow = mainWindow;
			FlexApp = flexApp;
			Cache = cache;
			_uiWidgetController = uiWidgetController;
			MainSendReceiveToolStripMenuItem = mainSendReceiveToolStripMenuItem;
			_bridges.Add(CommonBridgeServices.FLExBridge, new FlexBridge(Cache, FlexApp));
			_bridges.Add(CommonBridgeServices.LiftBridge, new LiftBridge(Cache, MainWindow, FlexApp));
			_bridges.Add(CommonBridgeServices.NoBridgeUsedYet, null);
		}

		private Tuple<bool, bool> CanDoCmdHelpChorus => new Tuple<bool, bool>(true, true);

		private Tuple<bool, bool> CanDoCmdCheckForFlexBridgeUpdates => new Tuple<bool, bool>(!MiscUtils.IsUnix, !MiscUtils.IsUnix);

		private Tuple<bool, bool> CanDoCmdHelpAboutFLEXBridge => new Tuple<bool, bool>(true, true);

		private Tuple<bool, bool> CanDoCmdFLExLiftBridge
		{
			get
			{
				var enabled = MainSendReceiveToolStripMenuItem.Enabled;
				if (enabled)
				{
					var lastBridgeUsed = GetLastBridge();
					if (lastBridgeUsed == null)
					{
						enabled = false;
					}
					else
					{
						switch (lastBridgeUsed.Name)
						{
							case CommonBridgeServices.FLExBridge:
								// If Fix it app does not exist, then disable main FLEx S/R, since FB needs to call it, after a merge.
								// If !IsConfiguredForSR (failed the first time), disable the button and hotkey
								enabled = FLExBridgeHelper.FixItAppExists && CommonBridgeServices.IsConfiguredForSR(Cache.ProjectId.ProjectFolder);
								break;
							case CommonBridgeServices.LiftBridge:
								// If !IsConfiguredForLiftSR (failed first time), disable the button and hotkey
								enabled = IsConfiguredForLiftSR(Cache.ProjectId.ProjectFolder);
								break;
							case CommonBridgeServices.NoBridgeUsedYet: // Fall through. This isn't really needed, but it is clearer that it covers the case.
							default:
								enabled = false;
								break;
						}
					}
				}
				return new Tuple<bool, bool>(true, enabled);
			}
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
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			var currentBridge = _bridges[CommonBridgeServices.FLExBridge];
			currentBridge.InitializeFlexComponent(flexComponentParameters);
			currentBridge.RegisterHandlers(_uiWidgetController);
			currentBridge = _bridges[CommonBridgeServices.LiftBridge];
			currentBridge.InitializeFlexComponent(flexComponentParameters);
			currentBridge.RegisterHandlers(_uiWidgetController);
			// Common to Project and LIFT S/R.
			var globalSendReceiveMenuHandlers = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>
			{
				{ Command.CmdHelpChorus, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(HelpChorus_Click, ()=> CanDoCmdHelpChorus) },
				{ Command.CmdCheckForFlexBridgeUpdates, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CheckForFlexBridgeUpdates_Click, ()=> CanDoCmdCheckForFlexBridgeUpdates) },
				{ Command.CmdHelpAboutFLEXBridge, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(HelpAboutFLEXBridge_Click, ()=> CanDoCmdHelpAboutFLEXBridge) }
			};
			var globalMenuData = new Dictionary<MainMenu, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>
			{
				{MainMenu.SendReceive,  globalSendReceiveMenuHandlers}
			};
			var globalToolBarHandlers = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>
			{
				{ Command.CmdFLExLiftBridge, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Flex_Or_Lift_Bridge_Clicked, ()=> CanDoCmdFLExLiftBridge) }
			};
			var globalToolBarData = new Dictionary<ToolBar, Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>>
			{
				{ ToolBar.Standard, globalToolBarHandlers }
			};
			_uiWidgetController.AddGlobalHandlers(globalMenuData, globalToolBarData);
		}

		#endregion

		private void HelpAboutFLEXBridge_Click(object sender, EventArgs e)
		{
			bool dummy1;
			string dummy2;
			FLExBridgeHelper.LaunchFieldworksBridge(CommonBridgeServices.GetFullProjectFileName(Cache), CommonBridgeServices.SendReceiveUser, FLExBridgeHelper.AboutFLExBridge,
				null, LcmCache.ModelVersion, CommonBridgeServices.LiftModelVersion, null, null, out dummy1, out dummy2);
		}

		private void CheckForFlexBridgeUpdates_Click(object sender, EventArgs e)
		{
			bool dummy1;
			string dummy2;
			FLExBridgeHelper.LaunchFieldworksBridge(CommonBridgeServices.GetFullProjectFileName(Cache), CommonBridgeServices.SendReceiveUser, FLExBridgeHelper.CheckForUpdates,
				null, LcmCache.ModelVersion, CommonBridgeServices.LiftModelVersion, null, null, out dummy1, out dummy2);
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

		private IBridge GetLastBridge()
		{
			return _bridges[PropertyTable.GetValue(CommonBridgeServices.LastBridgeUsed, CommonBridgeServices.NoBridgeUsedYet, SettingsGroup.LocalSettings)];
		}

		private void Flex_Or_Lift_Bridge_Clicked(object sender, EventArgs e)
		{
			var lastBridgeRun = GetLastBridge();
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
			Dispose(true);
			// The base class finalizer is called automatically.
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (_isDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
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
			_uiWidgetController = null;
			MainSendReceiveToolStripMenuItem = null;

			_isDisposed = true;
		}
		#endregion
	}
}