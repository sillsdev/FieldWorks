// Copyright (c) 2002-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;
using Microsoft.Win32;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.FwUtils.MessageBoxEx;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.Reporting;
using SIL.LCModel.Utils;
using SIL.Windows.Forms;
using Win32 = SIL.FieldWorks.Common.FwUtils.Win32;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// The main application class for Language Explorer.
	/// </summary>
	/// <remarks>
	/// There is only one of these per process, but it can contain multiple windows.
	/// </remarks>
	[Export(typeof(IFlexApp))]
	internal sealed class FlexApp : IFlexApp
	{
		#region Data Members
		[Import]
		private ExportFactory<IFwMainWnd> WindowFactory { get; set; }
		private List<Tuple<IFwMainWnd, ExportLifetimeContext<IFwMainWnd>>> _windows = new List<Tuple<IFwMainWnd, ExportLifetimeContext<IFwMainWnd>>>();

		private static bool m_fResourceFailed;
		/// <summary>
		///  Web browser to use in Linux
		/// </summary>
		private const string webBrowserProgramLinux = "firefox";
		[Import]
		private IHelpTopicProvider m_helpTopicProvider;
		private bool m_fInitialized;

		/// <summary></summary>
		private int m_nEnableLevel;
		/// <summary>
		/// The FieldWorks manager for dealing with FieldWorks-level stuff.
		/// </summary>
		[Import]
		private IFieldWorksManager m_fwManager;
		/// <summary />
		private FwFindReplaceDlg m_findReplaceDlg;
		private SuppressedCacheInfo m_suppressedCacheInfo;
		/// <summary>
		/// "NotSupressingRefresh" means that we are not suppressing view refreshes.
		/// "SupressingRefreshAndWantRefresh" means we're suppressing and we need to do a refresh when finished.
		/// "SupressingRefreshButDoNotWantRefresh" means we're suppressing, but have no need to do a refresh when finished.
		/// </summary>
		private RefreshInterest DeclareRefreshInterest { get; set; }
		/// <summary>The find patterns for the find/replace dialog, one for each database.</summary>
		/// <remarks>We need one pattern per database (cache). Otherwise it'll crash when we try to
		/// load the previous search term because the new database has different writing system hvos
		/// (TE-5598).</remarks>
		private IVwPattern m_findPattern;
		private IFwMainWnd m_windowToCloseOnIdle;

#endregion Data Members

#region Construction and Initializing

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <remarks>
		/// Called by MEF
		/// </remarks>
		internal FlexApp()
		{
			DeclareRefreshInterest = RefreshInterest.NotSupressingRefresh;
			IsModalDialogOpen = false;
			PictureHolder = new PictureHolder();
			RegistrySettings = new FwRegistrySettings(this)
			{
				LatestAppStartupTime = DateTime.Now.ToUniversalTime().Ticks.ToString()
			};
			RegistrySettings.AddErrorReportingInfo();

			Application.EnterThreadModal += Application_EnterThreadModal;
			Application.LeaveThreadModal += Application_LeaveThreadModal;

			Application.AddMessageFilter(this);
		}

#endregion Construction and Initializing

#region non-interface properties

		/// <summary>
		/// Guid for the application (used for uniquely identifying DB items that "belong" to
		///		this app.
		/// </summary>
		private static Guid AppGuid => new Guid("E716C901-3171-421f-83E1-3E012DEC9489");

		/// <summary>
		/// Gets the registry settings key name for the application.
		/// </summary>
		private static string SettingsKeyName => FwSubKey.LexText;

		/// <summary>
		/// Gets the FwFindReplaceDlg
		/// </summary>
		private FwFindReplaceDlg FindReplaceDialog
		{
			get
			{
				if (m_findReplaceDlg != null && m_findReplaceDlg.IsDisposed)
				{
					RemoveFindReplaceDialog(); // This is a HACK for TE-5974!!!
				}

				return m_findReplaceDlg;
			}
		}

		/// <summary>
		/// Gets the currently active form.
		/// </summary>
		private Form ActiveForm
		{
			get
			{
				var activeForm = Form.ActiveForm;
				if (activeForm != null)
				{
					return activeForm;
				}
				foreach (var tuple in _windows)
				{
					var wnd = (Form)tuple.Item1;
					if (wnd.ContainsFocus)
					{
						// It is focussed, so return it.
						return wnd;
					}
				}
				// None have focus, so get the first one, if there is one.
				if (_windows.Any())
				{
					return (Form)_windows[0].Item1;
				}
				return null;
			}
		}

		/// <summary>
		/// Gets the application's find pattern for the find/replace dialog. (If one does not
		/// already exist, a new one is created.)
		/// </summary>
		private IVwPattern FindPattern => m_findPattern ?? (m_findPattern = VwPatternClass.Create());

#endregion non-interface properties

#region IProjectSpecificSettingsKeyProvider interface implementation

		/// <summary>
		/// Gets the project specific settings key.
		/// </summary>
		public RegistryKey ProjectSpecificSettingsKey
		{
			get
			{
				Debug.Assert(Cache != null, "The app's cache has not been created yet.");
				using (var regKey = SettingsKey)
				{
					return regKey.CreateSubKey(Cache.ProjectId.Name);
				}
			}
		}

#endregion IProjectSpecificSettingsKeyProvider interface implementation

#region IMessageFilter interface implementation

		/// <summary>
		/// Filters out a message before it is dispatched.
		/// </summary>
		/// <param name="m">The message to be dispatched. You cannot modify this message.</param>
		/// <returns>
		/// true to filter the message and stop it from being dispatched; false to allow the message to continue to the next filter or control.
		/// </returns>
		public bool PreFilterMessage(ref Message m)
		{
			if (m.Msg != (int) Win32.WinMsgs.WM_KEYDOWN && m.Msg != (int) Win32.WinMsgs.WM_KEYUP)
			{
				return false;
			}

			var key = ((Keys)(int)m.WParam & Keys.KeyCode);
			// There is a known issue in older versions of Keyman (< 7.1.268) where the KMTip addin sends a 0x88 keystroke
			// in order to communicate changes in state to the Keyman engine. When a button is clicked while a text control
			// has focus, the input language will be switched causing the keystroke to be fired, which in turn disrupts the
			// mouse-click event. In order to workaround this bug for users who have older versions of Keyman, we simply filter
			// out these specific keystroke messages on buttons.
			if (key == Keys.ProcessKey || key == (Keys.Back | Keys.F17) /* 0x88 */)
			{
				var c = Control.FromHandle(m.HWnd);
				if (c is ButtonBase)
				{
					return true;
				}
			}

			return false;
		}
#endregion IMessageFilter interface implementation

#region ISettings interface implementation

		/// <summary>
		/// The RegistryKey for this application.
		/// </summary>
		public RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();
				using (var regKey = FwRegistryHelper.FieldWorksRegistryKey)
				{
					return regKey.CreateSubKey(SettingsKeyName);
				}
			}
		}

		/// <summary>
		/// For now, app has no settings to save. This method is required
		/// as part of ISettings implementation. Note that the SaveSettings() method is the
		/// appropriate one to modify if you really want to save settings. This
		/// one is NEVER called!
		/// </summary>
		/// <exception cref="NotSupportedException">Client is to use "SaveSettings" method, not this one.</exception>
		public void SaveSettingsNow()
		{
			throw new NotSupportedException("'SaveSettingsNow' is not supported. Use 'SaveSettings' method instead.");
		}
#endregion ISettings interface implementation

#region IFeedbackInfoProvider interface implementation
		/// <summary>
		/// E-mail address for bug reports, etc.
		/// </summary>
		public string SupportEmailAddress => LanguageExplorerResources.kstidSupportEmail;

		/// <summary>
		/// E-mail address for feedback reports, kudos, etc.
		/// </summary>
		public string FeedbackEmailAddress => "FLEXUsage@sil.org";

#endregion IFeedbackInfoProvider interface implementation

#region IHelpTopicProvider interface implementation
		/// <summary>
		/// Gets a URL identifying a Help topic.
		/// </summary>
		/// <param name="stid">An identifier for the desired Help topic</param>
		/// <returns>The requested string</returns>
		public string GetHelpString(string stid)
		{
			return m_helpTopicProvider.GetHelpString(stid);
		}

		/// <summary>
		/// The HTML help file (.chm) for the app.
		/// </summary>
		public string HelpFile => m_helpTopicProvider.HelpFile;

#endregion IHelpTopicProvider interface implementation

#region IDisposable interface implementation

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
			}
		}

#region Dispose support

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~FlexApp()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed || BeingDisposed)
			{
				return;
			}

			BeingDisposed = true;

			if (disposing)
			{
				// Dispose managed resources here.
				UpdateAppRuntimeCounter();

				Logger.WriteEvent("Disposing app: " + GetType().Name);
				RegistrySettings.FirstTimeAppHasBeenRun = false;

				// Dispose managed resources here.
				var mainWnds = new List<Tuple<IFwMainWnd, ExportLifetimeContext<IFwMainWnd>>>(_windows); // Use another list, since _windows may change.
				_windows.Clear(); // In fact, just clear the main array, so the windows won't have to worry so much.
				foreach (var tuple in mainWnds)
				{
					var mainWnd = tuple.Item1;
					var wnd = mainWnd as Form;
					if (wnd != null)
					{
						wnd.Closing -= OnClosingWindow;
						wnd.Closed -= OnWindowClosed;
						wnd.Activated -= fwMainWindow_Activated;
						wnd.HandleDestroyed -= fwMainWindow_HandleDestroyed;
						wnd.FormClosing -= FwMainWindowOnFormClosing;
					}
					mainWnd.Dispose();
				}
				m_findReplaceDlg?.Dispose();

				ResourceHelper.ShutdownHelper();

				RegistrySettings?.Dispose();
				MessageBoxExManager.DisposeAllMessageBoxes();

				Application.EnterThreadModal -= Application_EnterThreadModal;
				Application.LeaveThreadModal -= Application_LeaveThreadModal;

				Application.RemoveMessageFilter(this);
				PictureHolder.Dispose();
				DeclareRefreshInterest = RefreshInterest.NotSupressingRefresh;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			_windows = null;
			ActiveMainWindow = null;
			RegistrySettings = null;
			m_findPattern = null;
			m_findReplaceDlg = null;
			m_suppressedCacheInfo = null;
			PictureHolder = null;

			IsDisposed = true;
			BeingDisposed = false;
		}

		/// <summary>
		/// See if the object is being disposed.
		/// </summary>
		private bool BeingDisposed { get; set; }

		private void UpdateAppRuntimeCounter()
		{
			var csec = RegistrySettings.TotalAppRuntime;
			var sStartup = RegistrySettings.LatestAppStartupTime;
			long start;
			if (!string.IsNullOrEmpty(sStartup) && long.TryParse(sStartup, out start))
			{
				var started = new DateTime(start);
				var finished = DateTime.Now.ToUniversalTime();
				var delta = finished - started;
				csec += (int)delta.TotalSeconds;
				RegistrySettings.TotalAppRuntime = csec;
			}
		}

#endregion Dispose support

		/// <summary>
		/// Dispose the object.
		/// </summary>
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
#endregion IDisposable interface implementation

#region IApp interface implementation

		/// <summary>
		/// Return a string from a resource ID
		/// </summary>
		public string ResourceString(string stid)
		{
			CheckDisposed();

			try
			{
				// No need to allocate a different ResourceManager than the one the generated code
				// produces, and it should be more reliable (I hope).
				return (stid == null ? "NullStringID" : LanguageExplorerResources.ResourceManager.GetString(stid));
			}
			catch (Exception e)
			{
				if (!m_fResourceFailed)
				{
					MessageBox.Show(ActiveMainWindow, string.Format(LanguageExplorerResources.ksErrorLoadingResourceStrings, e.Message), LanguageExplorerResources.ksError);
					m_fResourceFailed = true;
				}
				return (stid == null) ? "NullStringID" : null;
			}
		}

		/// <summary>
		/// Gets/Sets the measurement system used in the application
		/// </summary>
		public MsrSysType MeasurementSystem
		{
			get { return (MsrSysType)FwRegistrySettings.MeasurementUnitSetting; }
			set { FwRegistrySettings.MeasurementUnitSetting = (int)value; }
		}

		/// <summary>
		/// Get the active form. This is usually the same as Form.ActiveForm, but sometimes
		/// the official active form is something other than one of our main windows, for
		/// example, a dialog or popup menu. This is always one of our real main windows,
		/// which should be something that has a taskbar icon. It is often useful as the
		/// appropriate parent window for a dialog that otherwise doesn't have one.
		/// </summary>
		public Form ActiveMainWindow { get; private set; }

		/// <summary>
		/// Gets the name of the application.
		/// </summary>
		public string ApplicationName => FwUtils.ksFlexAppName;

		/// <summary>
		/// A picture holder that may be used to retrieve various useful pictures.
		/// </summary>
		public PictureHolder PictureHolder { get; private set; }

		/// <summary>
		/// Refreshes all the views in all of the Main Windows of the app.
		/// </summary>
		public void RefreshAllViews()
		{
			CheckDisposed();

			if (DeclareRefreshInterest != RefreshInterest.NotSupressingRefresh)
			{
				DeclareRefreshInterest = RefreshInterest.SupressingRefreshAndWantRefresh;
			}
			else
			{
				foreach (var wnd in MainWindows)
				{
					wnd.RefreshAllViews();
				}
			}
		}

		/// <summary>
		/// Restart the spell-checking process (e.g. when dictionary changed)
		/// </summary>
		public void RestartSpellChecking()
		{
			foreach (Control wnd in MainWindows)
		{
				RestartSpellChecking(wnd);
			}
		}

		/// <summary>
		/// Cycle through the applications main windows and synchronize them with database
		/// changes.
		/// </summary>
		/// <param name="sync">synchronization information record</param>
		/// <returns><c>true</c> to continue processing; set to <c>false</c> to prevent
		/// processing of subsequent sync messages. </returns>
		public bool Synchronize(SyncMsg sync)
		{
			CheckDisposed();

			if (sync == SyncMsg.ksyncUndoRedo || sync == SyncMsg.ksyncFullRefresh)
			{
				// Susanna asked that refresh affect only the currently active project, which is
				// what the string and List variables below attempt to handle.  See LT-6444.
				var activeWnd = ActiveForm as IFwMainWnd;

				var rgxw = new List<IFwMainWnd>();
				foreach (var wnd in MainWindows)
				{
					wnd.PrepareToRefresh();
					rgxw.Add(wnd);
				}

				if (activeWnd != null)
				{
					rgxw.Remove(activeWnd);
				}

				foreach (var xwnd in rgxw)
				{
					xwnd.FinishRefresh();
				}

				// LT-3963: active window changes as a result of a refresh.
				// Make sure focus doesn't switch to another FLEx application / window also
				// make sure the application focus isn't lost all together.
				// ALSO, after doing a refresh with just a single application / window,
				// the application would loose focus and you'd have to click into it to
				// get that back, this will reset that too.
				if (activeWnd != null)
				{
					// Refresh it last, so its saved settings get restored.
					activeWnd.FinishRefresh();
					var asForm = activeWnd as Form;
					asForm?.Activate();
				}
				return true;
			}

			if (m_suppressedCacheInfo != null)
			{
				var messages = m_suppressedCacheInfo.Queue;
				if (!messages.Contains(sync))
				{
					messages.Enqueue(sync);
				}
				return true;
			}

			if (sync == SyncMsg.ksyncFullRefresh)
			{
				RefreshAllViews();
				return false;
			}

			foreach (var wnd in MainWindows)
			{
				wnd.PreSynchronize(sync);
			}

			if (sync == SyncMsg.ksyncWs)
			{
				// REVIEW TeTeam: AfLpInfo::Synchronize calls AfLpInfo::FullRefresh, which
				// clears the cache, loads the styles, loads ws and updates wsf, load project
				// basics, updates LinkedFiles root, load overlays and refreshes possibility
				// lists. I don't think we need to do any of these here.
				RefreshAllViews();
				return false;
			}

			if (MainWindows.All(wnd => wnd.Synchronize(sync)))
			{
				return true;
			}
			RefreshAllViews();
			return false;
		}

		/// <summary>
		/// This application processes DB sync records.
		/// </summary>
		public Guid SyncGuid
		{
			get
			{
				CheckDisposed();
				return AppGuid;
			}
		}

		/// <summary>
		/// Enable or disable all top-level windows. This allows nesting. In other words,
		/// calling EnableMainWindows(false) twice requires 2 calls to EnableMainWindows(true)
		/// before the top level windows are actually enabled. An example of where not allowing
		/// nesting was a problem was the Tools/Options dialog, which could open a
		/// PossListChooser dialog. Before, when you closed the PossListChooser, you could
		/// select the main window.
		/// </summary>
		/// <param name="fEnable">Enable (true) or disable (false).</param>
		public void EnableMainWindows(bool fEnable)
		{
			CheckDisposed();

			if (!fEnable)
			{
				m_nEnableLevel--;
			}
			else if (++m_nEnableLevel != 0)
			{
				return;
			}

			// TE-1913: Prevent user from accessing windows that are open to the same project.
			// Originally this was used for importing.
			foreach (var fwMainWnd in MainWindows)
			{
				if (fwMainWnd is Form)
				{
					((Form)fwMainWnd).Enabled = fEnable;
				}
			}
		}

		/// <summary>
		/// Close and remove the Find/Replace modeless dialog (result of LT-5702)
		/// </summary>
		public void RemoveFindReplaceDialog()
		{
			if (m_findReplaceDlg == null)
			{
				return;
			}
			// Closing doesn't work as it tries to hide the dlg ..
			// so go for the .. dispose.  It will do it 'base.Dispose()'!
			m_findReplaceDlg.Close();
			m_findReplaceDlg.Dispose();
			m_findReplaceDlg = null;
		}

		/// <summary>
		/// Display the Find/Replace modeless dialog
		/// </summary>
		/// <param name="fReplace"><c>true</c> to make the replace tab active</param>
		/// <param name="rootsite">The view where the find will be conducted</param>
		/// <returns><c>true</c> if the dialog is successfully displayed</returns>
		public bool ShowFindReplaceDialog(bool fReplace, RootSite rootsite)
		{
			CheckDisposed();

			if (rootsite?.RootBox == null)
			{
				return false;
			}

			int hvoRoot, frag;
			IVwViewConstructor vc;
			IVwStylesheet ss;
			rootsite.RootBox.GetRootObject(out hvoRoot, out vc, out frag, out ss);
			if (hvoRoot == 0)
			{
				return false;
			}

			if (FindReplaceDialog == null)
			{
				m_findReplaceDlg = new FwFindReplaceDlg();
			}

			var fOverlay = (rootsite.RootBox.Overlay != null);

			if (m_findReplaceDlg.SetDialogValues(rootsite.Cache, FindPattern, rootsite, fReplace, fOverlay, rootsite.FindForm(), this, this))
			{
				m_findReplaceDlg.Show();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Handles the incoming link, after the right window of the right application on the right
		/// project has been activated.
		/// See the class comment on FwLinkArgs for details on how all the parts of hyperlinking work.
		/// </summary>
		public void HandleIncomingLink(FwLinkArgs link)
		{
			CheckDisposed();

			// Get window that uses the given DB.
			var fwxwnd = _windows.Any() ? _windows[0].Item1 : null;
			if (fwxwnd == null)
			{
				return;
			}
			var commands = new List<string>
			{
				"AboutToFollowLink",
				"FollowLink"
			};
			var parms = new List<object>
			{
				null,
				link
			};
			fwxwnd.Publisher.Publish(commands, parms);
			var asForm = fwxwnd as Form;
			var topmost = asForm.TopMost;
			asForm.TopMost = true;
			asForm.TopMost = topmost;
			asForm.Activate();
		}

		/// <summary>
		/// Handles an outgoing link request from this application.
		/// </summary>
		public void HandleOutgoingLink(FwAppArgs link)
		{
			m_fwManager.HandleLinkRequest(link);
		}

		/// <summary>
		/// Handle changes to the LinkedFiles root directory for a language project.
		/// </summary>
		/// <param name="oldLinkedFilesRootDir">The old LinkedFiles root directory.</param>
		/// <returns></returns>
		/// <remarks>This may not be the best place for this method, but I'm not sure there is a
		/// "best place".</remarks>
		public bool UpdateExternalLinks(string oldLinkedFilesRootDir)
		{
			var lp = Cache.LanguageProject;
			var sNewLinkedFilesRootDir = lp.LinkedFilesRootDir;
			if (FileUtils.PathsAreEqual(sNewLinkedFilesRootDir, oldLinkedFilesRootDir))
			{
				return false;
			}
			var rgFilesToMove = new List<string>();
			// TODO: offer to move or copy existing files.
			foreach (var cf in lp.MediaOC)
			{
				CollectMovableFilesFromFolder(cf, rgFilesToMove, oldLinkedFilesRootDir, sNewLinkedFilesRootDir);
			}

			foreach (var cf in lp.PicturesOC)
			{
				CollectMovableFilesFromFolder(cf, rgFilesToMove, oldLinkedFilesRootDir, sNewLinkedFilesRootDir);
			}
			//Get the files which are pointed to by links in TsStrings
			CollectMovableFilesFromFolder(lp.FilePathsInTsStringsOA, rgFilesToMove, oldLinkedFilesRootDir, sNewLinkedFilesRootDir);

			var hyperlinks = StringServices.GetHyperlinksInFolder(Cache, oldLinkedFilesRootDir);
			foreach (var linkInfo in hyperlinks)
			{
				if (!rgFilesToMove.Contains(linkInfo.RelativePath) && FileUtils.SimilarFileExists(Path.Combine(oldLinkedFilesRootDir, linkInfo.RelativePath)) &&
				    !FileUtils.SimilarFileExists(Path.Combine(sNewLinkedFilesRootDir, linkInfo.RelativePath)))
				{
					rgFilesToMove.Add(linkInfo.RelativePath);
				}
			}

			if (!rgFilesToMove.Any())
			{
				return false;
			}
			FileLocationChoice action;
			using (var dlg = new MoveOrCopyFilesDlg()) // REVIEW (Hasso) 2015.08: should this go in MoveOrCopyFilesController?
			{
				dlg.Initialize(rgFilesToMove.Count, oldLinkedFilesRootDir, sNewLinkedFilesRootDir, this);
				var res = dlg.ShowDialog();
				Debug.Assert(res == DialogResult.OK);
				if (res != DialogResult.OK)
				{
					return false;   // should never happen!
				}
				action = dlg.Choice;
			}
			if (action == FileLocationChoice.Leave) // Expand path
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
					() =>
					{
						foreach (var cf in lp.MediaOC)
						{
							ExpandToFullPath(cf, oldLinkedFilesRootDir, sNewLinkedFilesRootDir);
						}

						foreach (var cf in lp.PicturesOC)
						{
							ExpandToFullPath(cf, oldLinkedFilesRootDir, sNewLinkedFilesRootDir);
						}
					});
				// Hyperlinks are always already full paths.
				return false;
			}
			var rgLockedFiles = new List<string>();
			foreach (var sFile in rgFilesToMove)
			{
				var sOldPathname = Path.Combine(oldLinkedFilesRootDir, sFile);
				var sNewPathname = Path.Combine(sNewLinkedFilesRootDir, sFile);
				var sNewDir = Path.GetDirectoryName(sNewPathname);
				if (!Directory.Exists(sNewDir))
				{
					Directory.CreateDirectory(sNewDir);
				}
				Debug.Assert(FileUtils.TrySimilarFileExists(sOldPathname, out sOldPathname));
				if (FileUtils.TrySimilarFileExists(sNewPathname, out sNewPathname))
				{
					File.Delete(sNewPathname);
				}
				try
				{
					if (action == FileLocationChoice.Move)
					{
						//LT-13343 do copy followed by delete to ensure the file gets put in the new location.
						//If the current FLEX record has a picture displayed the File.Delete will fail.
						File.Copy(sOldPathname, sNewPathname);
						File.Delete(sOldPathname);
					}

					else
					{
						File.Copy(sOldPathname, sNewPathname);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"{ex.Message}: {sOldPathname}");
					rgLockedFiles.Add(sFile);
				}
			}
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor,
				() => StringServices.FixHyperlinkFolder(hyperlinks, oldLinkedFilesRootDir, sNewLinkedFilesRootDir));

			// If any files failed to be moved or copied above, try again now that we've
			// opened a new window and had more time elapse (and more demand to reuse
			// memory) since the failure.
			if (!rgLockedFiles.Any())
			{
				return true;
			}
			GC.Collect();   // make sure the window is disposed!
			Thread.Sleep(1000);
			foreach (var sFile in rgLockedFiles)
			{
				var sOldPathname = Path.Combine(oldLinkedFilesRootDir, sFile);
				var sNewPathname = Path.Combine(sNewLinkedFilesRootDir, sFile);
				try
				{
					if (action == FileLocationChoice.Move)
					{
						FileUtils.Move(sOldPathname, sNewPathname);
					}
					else
					{
						File.Copy(sOldPathname, sNewPathname);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"{ex.Message}: {sOldPathname} (SECOND ATTEMPT)");
				}
			}
			return true;
		}

#endregion IApp interface implementation

#region IFlexApp interface implementation

		/// <summary>
		/// Array of main windows that are currently open for this application. This array can
		/// be used (with foreach) to do all the kinds of things that used to require a custom
		/// method in AfApp to loop through the vector of windows and execute some method for
		/// each one (e.g., AreAllWndsOkToChange, SaveAllWndsEdits, etc.).
		/// In C++, was GetMainWindows()
		/// </summary>
		public List<IFwMainWnd> MainWindows
		{
			get
			{
				CheckDisposed();
				return new List<IFwMainWnd>(_windows.Select(tuple => tuple.Item1));
			}
		}

		/// <summary>
		/// Activate the given window.
		/// </summary>
		/// <param name="iMainWnd">Index (in the internal list of main windows) of the window to activate</param>
		public void ActivateWindow(int iMainWnd)
		{
			var wnd = (Form)MainWindows[iMainWnd];
			wnd.Activate();
			if (wnd.WindowState == FormWindowState.Minimized)
			{
				wnd.WindowState = FormWindowState.Normal;
			}
		}

		/// <summary>
		/// Creates and opens a main FLEx window.
		/// </summary>
		public Form NewMainAppWnd(IProgress progressDlg, bool isNewCache, IFwMainWnd wndCopyFrom = null)
		{
			if (progressDlg != null)
			{
				progressDlg.Message = string.Format(LanguageExplorerResources.ksCreatingWindowForX, Cache.ProjectId.Name);
			}
			// We pass a copy of the link information because it doesn't get used until after the following line
			// removes the information we need.
			var exportLifetimeContext = WindowFactory.CreateExport();
			var factoryMadeIFwMainWnd = exportLifetimeContext.Value;
			_windows.Add(new Tuple<IFwMainWnd, ExportLifetimeContext<IFwMainWnd>>(factoryMadeIFwMainWnd, exportLifetimeContext));

			FwAppArgs.ClearLinkInformation(); // Make sure the next window that is opened doesn't default to the same place

			if (isNewCache)
			{
				InitializePartInventories(progressDlg, true);
			}
			// Let the application do its initialization of the new window
			var factoryMadeIFwMainWndAsForm = (Form)factoryMadeIFwMainWnd;
			using (new DataUpdateMonitor(factoryMadeIFwMainWndAsForm, "Creating new main window"))
			{
				factoryMadeIFwMainWnd.Initialize(wndCopyFrom != null, FwAppArgs.HasLinkInformation ? FwAppArgs.CopyLinkArgs() : null);
				factoryMadeIFwMainWndAsForm.Closing += OnClosingWindow;
				factoryMadeIFwMainWndAsForm.Closed += OnWindowClosed;
				factoryMadeIFwMainWndAsForm.Activated += fwMainWindow_Activated;
				if (factoryMadeIFwMainWnd == Form.ActiveForm)
				{
					ActiveMainWindow = factoryMadeIFwMainWndAsForm;
				}
				factoryMadeIFwMainWndAsForm.HandleDestroyed += fwMainWindow_HandleDestroyed;
				factoryMadeIFwMainWndAsForm.FormClosing += FwMainWindowOnFormClosing;
				factoryMadeIFwMainWndAsForm.Show(); // Show method loads persisted settings for window & controls
				factoryMadeIFwMainWndAsForm.Activate(); // This makes main window come to front after splash screen closes

				// adjust position if this is an additional window
				if (wndCopyFrom != null)
				{
					AdjustNewWindowPosition(factoryMadeIFwMainWndAsForm, (Form)wndCopyFrom);
				}
				else if (factoryMadeIFwMainWndAsForm.WindowState != FormWindowState.Maximized)
				{
					// Fix the stored position in case it is off the screen.  This can happen if the
					// user has removed a second monitor, or changed the screen resolution downward,
					// since the last time he ran the program.  (See LT-1083.)
					var rcNewWnd = factoryMadeIFwMainWndAsForm.DesktopBounds;
					ScreenHelper.EnsureVisibleRect(ref rcNewWnd);
					factoryMadeIFwMainWndAsForm.DesktopBounds = rcNewWnd;
					factoryMadeIFwMainWndAsForm.StartPosition = FormStartPosition.Manual;
				}

				m_fInitialized = true;
			}

			return factoryMadeIFwMainWndAsForm;
		}

		/// <summary>
		/// Initialize the required inventories.
		/// </summary>
		public void InitializePartInventories(IProgress progressDlg, bool fLoadUserOverrides)
		{
			if (progressDlg != null)
			{
				progressDlg.Message = LanguageExplorerResources.ksInitializingLayouts_;
			}
			LayoutCache.InitializePartInventories(Cache.ProjectId.Name, ApplicationName, fLoadUserOverrides, Cache.ProjectId.ProjectFolder);

			var currentReversalIndices = Cache.LanguageProject.LexDbOA.CurrentReversalIndices;
			if (!currentReversalIndices.Any())
			{
				currentReversalIndices = new List<IReversalIndex>(Cache.LanguageProject.LexDbOA.ReversalIndexesOC.ToArray());
			}

			foreach (var reversalIndex in currentReversalIndices)
			{
				LayoutCache.InitializeLayoutsForWsTag(reversalIndex.WritingSystem, Cache.ProjectId.Name);
			}
		}

		private void FwMainWindowOnFormClosing(object sender, FormClosingEventArgs formClosingEventArgs)
		{
			if (ActiveMainWindow == sender)
			{
				(sender as IFwMainWnd)?.SaveSettings();
			}
		}

		/// <summary>
		/// Closes and re-opens the argument window, in the same place, as a drastic way of applying new settings.
		/// </summary>
		public void ReplaceMainWindow(IFwMainWnd wndActive)
		{
			wndActive.SaveSettings();
			FwManager.OpenNewWindowForApp(wndActive);
			m_windowToCloseOnIdle = wndActive;
			Application.Idle += CloseOldWindow;
		}

		/// <summary>
		/// Use this for slow operations that should happen during the splash screen instead of
		/// during app construction
		/// </summary>
		public void DoApplicationInitialization(IProgress progressDlg)
		{
			InitializeMessageDialogs(progressDlg);
			if (progressDlg != null)
			{
				progressDlg.Message = LanguageExplorerResources.ksLoading_;
			}
		}

		/// <summary>
		/// Called just after DoApplicationInitialization(). Allows a separate overide of
		/// loading settings. If you override this you probably also want to
		/// override SaveSettings.
		/// </summary>
		public void LoadSettings()
		{
			// Keyman has an option to change the system keyboard with a keyman keyboard change.
			// Unfortunately, this messes up changing writing systems in FieldWorks when Keyman
			// is running. The only fix seems to be to turn that option off... (TE-8686)
			try
			{
				using (var engineKey = Registry.CurrentUser.OpenSubKey(@"Software\Tavultesoft\Keyman Engine", false))
				{
					if (engineKey == null)
					{
						return;
					}
					foreach (var version in engineKey.GetSubKeyNames())
					{
						using (var keyVersion = engineKey.OpenSubKey(version, true))
						{
							if (keyVersion == null)
							{
								continue;
							}
							var value = keyVersion.GetValue("switch language with keyboard");
							if (value == null || (int)value != 0)
							{
								keyVersion.SetValue("switch language with keyboard", 0);
							}
						}
					}
				}
			}
			catch (SecurityException)
			{
				// User doesn't have access to the registry key, so just hope the user is fine
				// with what he gets.
			}
		}

		/// <summary>
		///	App-specific initialization of the cache.
		/// </summary>
		/// <returns>True if the initialize was successful, false otherwise</returns>
		public bool InitCacheForApp(IThreadedProgress progressDlg)
		{
			Cache.ServiceLocator.DataSetup.LoadDomainAsync(BackendBulkLoadDomain.All);

			// The try-catch block is modeled after that used by TeScrInitializer.Initialize(),
			// as the suggestion for fixing LT-8797.
			try
			{
				// Make sure this DB uses the current stylesheet version.
				FlexStylesXmlAccessor.EnsureCurrentStylesheet(Cache.LangProject, progressDlg);
			}
			catch (WorkerThreadException e)
			{
				MessageBox.Show(Form.ActiveForm, e.InnerException.Message, ApplicationName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Gets the cache, or null if not available.
		/// </summary>
		public LcmCache Cache => m_fwManager?.Cache;

		/// <summary>
		/// Gets the FieldWorks manager for this application.
		/// </summary>
		public IFieldWorksManager FwManager => m_fwManager;

		/// <summary>
		/// Removes the specified IFwMainWnd from the list of windows. If it is ok to close down
		/// the application and the count of main windows is zero, then this method will also
		/// shut down the application.
		/// </summary>
		public void RemoveWindow(IFwMainWnd fwMainWindow)
		{
			if (IsDisposed || BeingDisposed)
			{
				return;
			}

			if (!MainWindows.Contains(fwMainWindow))
			{
				return; // It isn't our window.
			}

			// NOTE: The main window that was passed in is most likely already disposed, so
			// make sure we don't call anything that would throw an ObjectDisposedException!
			var gonerTuple = (_windows
				.Select(tuple => new {tuple, currentWindow = tuple.Item1})
				.Where(@t => @t.currentWindow == fwMainWindow)
				.Select(@t => @t.tuple)).FirstOrDefault();
			if (gonerTuple != null)
			{
				_windows.Remove(gonerTuple);
				var form = (Form)gonerTuple.Item1;
				form.Activated -= fwMainWindow_Activated;
				form.HandleDestroyed -= fwMainWindow_HandleDestroyed;
				form.FormClosing -= FwMainWindowOnFormClosing;
				gonerTuple.Item2.Dispose(); // Disposing the factory also disposes the IFwMainWnd it created.
			}

			if (ActiveMainWindow == fwMainWindow)
			{
				ActiveMainWindow = null; // Just in case
			}

			if (!_windows.Any())
			{
				m_fwManager.ExecuteAsync(m_fwManager.ShutdownApp, this);
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance has a modal dialog or message box open.
		/// </summary>
		public bool IsModalDialogOpen { get; private set; }

		/// <summary>
		/// Gets the full path of the product executable filename
		/// </summary>
		public string ProductExecutableFile => FwDirectoryFinder.FieldWorksExe;

		/// <summary>
		/// Gets a value indicating whether this instance has been fully initialized.
		/// </summary>
		public bool HasBeenFullyInitialized => m_fInitialized && !IsDisposed && !BeingDisposed;

		/// <summary>
		/// Gets the registry settings for this FwApp
		/// </summary>
		public FwRegistrySettings RegistrySettings { get; private set; }

		/// <summary>
		/// Return a string from a resource ID.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>The resource string</returns>
		public string GetResourceString(string stid)
		{
			var str = ResourceString(stid);
			if (string.IsNullOrEmpty(str))
			{
				str = ResourceHelper.GetResourceString(stid);
			}

			return str;
		}

		/// <summary>
		/// The name of the sample DB for the app.
		/// </summary>
		public string SampleDatabase => Path.Combine(FwDirectoryFinder.LcmDirectories.ProjectsDirectory, "Sena 3", "Sena 3" + LcmFileHelper.ksFwDataXmlFileExtension);

		public FwAppArgs FwAppArgs { set; private get; }

#endregion IFlexApp interface implementation

		private static void RestartSpellChecking(Control root)
		{
			var rootSite = root as RootSite;
			rootSite?.RestartSpellChecking();
			foreach (Control c in root.Controls)
			{
				RestartSpellChecking(c);
			}
		}

		/// <summary>
		/// Initialize the required inventories.
		/// </summary>
		/// <param name="progressDlg">The progress dialog</param>
		private static void InitializeMessageDialogs(IProgress progressDlg)
		{
			if (progressDlg != null)
			{
				progressDlg.Message = LanguageExplorerResources.ksInitializingMessageDialogs_;
			}
			MessageBoxExManager.DefineMessageBox("TextChartNewFeature", LanguageExplorerResources.ksInformation, LanguageExplorerResources.ksChartTemplateWarning, true, "info");
			MessageBoxExManager.DefineMessageBox("CategorizedEntry-Intro", LanguageExplorerResources.ksInformation, LanguageExplorerResources.ksUsedForSemanticBasedEntry, true, "info");
			MessageBoxExManager.DefineMessageBox("CreateNewFromGrammaticalCategoryCatalog", LanguageExplorerResources.ksInformation, LanguageExplorerResources.ksCreatingCustomGramCategory, true, "info");
			MessageBoxExManager.DefineMessageBox("CreateNewLexicalReferenceType", LanguageExplorerResources.ksInformation, LanguageExplorerResources.ksCreatingCustomLexRefType, true, "info");
			MessageBoxExManager.DefineMessageBox("ClassifiedDictionary-Intro", LanguageExplorerResources.ksInformation, LanguageExplorerResources.ksShowingSemanticClassification, true, "info");

			MessageBoxExManager.ReadSettingsFile();
			if (progressDlg != null)
			{
				progressDlg.Message = string.Empty;
			}
		}

		private void CloseOldWindow(object sender, EventArgs e)
		{
			Application.Idle -= CloseOldWindow;
			m_windowToCloseOnIdle?.Close();
			m_windowToCloseOnIdle = null;
		}

		/// <summary>
		/// Returns str surrounded by double-quotes.
		/// This is useful for paths containing spaces in Linux.
		/// </summary>
		private static string Enquote(string str)
		{
			return "\"" + str + "\"";
		}

		/// <summary>
		/// Like OpenDocument(), but allowing specification of specific exception type T to catch.
		/// </summary>
		private static void OpenDocument<T>(string path, Action<T> exceptionHandler) where T : Exception
		{
			try
			{
				if (MiscUtils.IsUnix && (path.EndsWith(".html") || path.EndsWith(".htm")))
				{
					Process.Start(webBrowserProgramLinux, Enquote(path));
				}
				else
				{
					Process.Start(path);
				}
			}
			catch (T e)
			{
				exceptionHandler?.Invoke(e);
			}
		}

		/// <summary>
		/// Adjust the new window position - offset right and down from the original.
		/// Also copy the window size, state, and set the StartPosition mode to manual.
		/// </summary>
		private static void AdjustNewWindowPosition(Form wndNew, Form wndCopyFrom)
		{
			// Get position and size
			var rcNewWnd = wndCopyFrom.DesktopBounds;

			// However, desktopBounds are not useful when window is maximized; in that case
			// get the info from Persistence instead... NormalStateDesktopBounds
			if (wndCopyFrom.WindowState == FormWindowState.Maximized)
			{
				// Here we subtract twice the caption height, which with the offset below insets it all around.
				rcNewWnd.Width -= SystemInformation.CaptionHeight * 2;
				rcNewWnd.Height -= SystemInformation.CaptionHeight * 2;
			}

			//Offset right and down
			rcNewWnd.X += SystemInformation.CaptionHeight;
			rcNewWnd.Y += SystemInformation.CaptionHeight;

			// We we will check if we went too far right or down, as Word 2002 checks.
			// If rcNewWnd is beyond bottom or right of screen...
			// Get the working area of the screen on which the new window will be placed.
			//Rectangle rcScrn = Screen.FromRectangle(rcNewWnd).WorkingArea;

			// If our adjusted rcNewWnd is partly off the screen, move it so it is fully
			// on the screen its mostly on. Note: this will only be necessary when the window
			// being copied from is partly off the screen in a single monitor system or
			// spanning multiple monitors in a multiple monitor system.
			ScreenHelper.EnsureVisibleRect(ref rcNewWnd);

			// Set the properties of the new window
			wndNew.DesktopBounds = rcNewWnd;
			wndNew.StartPosition = FormStartPosition.Manual;
			wndNew.WindowState = wndCopyFrom.WindowState;
		}

		/// <summary>
		/// Handles the EnterThreadModal event of the Application control.
		/// </summary>
		private void Application_EnterThreadModal(object sender, EventArgs e)
		{
			IsModalDialogOpen = true;
		}

		/// <summary>
		/// Handles the LeaveThreadModal event of the Application control.
		/// </summary>
		private void Application_LeaveThreadModal(object sender, EventArgs e)
		{
			IsModalDialogOpen = false;
		}

		/// <summary>
		/// When a window is closed, we need to make sure we close any root boxes that may
		/// be on the window.
		/// </summary>
		private static void OnWindowClosed(object sender, EventArgs e)
		{
			CloseRootBoxes(sender as Control);
		}

		/// <summary>
		/// Recursively look at all the controls belonging to the specified control and save
		/// the settings for each root box for controls of type ISettings. Then close
		/// the root box for controls of type IRootSite. Ideally IRootSite controls should
		/// close their root boxes in the OnHandleDestroyed event, but since sometimes IRootSite
		/// controls are created but never shown (which means their handle is never created),
		/// we have to close the rootboxes here instead.
		/// </summary>
		/// <param name="ctrl">A main window or any of its descendents</param>
		private static void CloseRootBoxes(Control ctrl)
		{
			if (ctrl == null)
			{
				return;
			}

			(ctrl as ISettings)?.SaveSettingsNow();
			(ctrl as IRootSite)?.CloseRootBox();

			foreach (Control childControl in ctrl.Controls)
			{
				CloseRootBoxes(childControl);
			}
		}

		/// <summary>
		/// The active main window is closing, so try to find some other main window that can
		/// "own" the find/replace dialog, so it can stay alive.
		/// If we can't find one, then all main windows are going away and we're going
		/// to have to close, too.
		/// </summary>
		private void OnClosingWindow(object sender, CancelEventArgs e)
		{
			if (!(sender is IFwMainWnd))
			{
				return;
			}
			if (FindReplaceDialog == null)
			{
				return;
			}
			foreach (var fwWnd in MainWindows)
			{
				if (fwWnd == sender || fwWnd.ActiveView == null)
				{
					continue;
				}
				m_findReplaceDlg.SetOwner(fwWnd.ActiveView.CastAsIVwRootSite(), (Form)fwWnd, FindPattern);
				return;
			}
			// This should never happen, but, just in case a new owner for
			// the find/replace dialog cannot be found, close it so it doesn't
			// get left hanging around without an owner.
			RemoveFindReplaceDialog();
		}

		/// <summary>
		/// Note the most recent of our main windows to become active.
		/// </summary>
		private void fwMainWindow_Activated(object sender, EventArgs e)
		{
			ActiveMainWindow = (Form)sender;
		}

		/// <summary>
		/// Make sure a window that's no longer valid isn't considered active.
		/// </summary>
		private void fwMainWindow_HandleDestroyed(object sender, EventArgs e)
		{
			if (ActiveMainWindow == sender)
			{
				ActiveMainWindow = null;
			}
		}

		/// <summary>
		/// Calculate a size (width or height) which is 2/3 of the screen area or the minimum
		/// allowable size.
		/// </summary>
		/// <param name="screenSize">Total available width or height (for the screen)</param>
		/// <param name="minSize">Minimum width or height for the window</param>
		/// <returns>The ideal width or height for a cascaded window</returns>
		private static int CascadeSize(int screenSize, int minSize)
		{
			var retSize = (screenSize * 2) / 3;
			if (retSize < minSize)
			{
				retSize = minSize;
			}
			return retSize;
		}

		/// <summary>
		/// Cascade the windows from top left and resize them to fill 2/3 of the screen area (or
		/// the minimum allowable size).
		/// </summary>
		private void CascadeWindows(Form currentWindow)
		{
			// Get the screen in which to cascade.
			var scrn = Screen.FromControl(currentWindow);
			var rcScrnAdjusted = ScreenHelper.AdjustedWorkingArea(scrn);
			var rcUpperLeft = rcScrnAdjusted;
			rcUpperLeft.Width = CascadeSize(rcUpperLeft.Width, currentWindow.MinimumSize.Width);
			rcUpperLeft.Height = CascadeSize(rcUpperLeft.Height, currentWindow.MinimumSize.Height);
			var rc = rcUpperLeft;

			foreach (Form wnd in MainWindows)
			{
				// Ignore windows that are on other screens or which are minimized.
				if (scrn.WorkingArea == Screen.FromControl(wnd).WorkingArea && wnd != currentWindow && wnd.WindowState != FormWindowState.Minimized)
				{
					if (wnd.WindowState == FormWindowState.Maximized)
					{
						wnd.WindowState = FormWindowState.Normal;
					}

					wnd.DesktopBounds = rc;
					wnd.Activate();
					rc.Offset(SystemInformation.CaptionHeight, SystemInformation.CaptionHeight);
					if (!rcScrnAdjusted.Contains(rc))
					{
						rc = rcUpperLeft;
					}
				}
			}

			// Make the active window the last one and activate it.
			if (currentWindow.WindowState == FormWindowState.Maximized)
			{
				currentWindow.WindowState = FormWindowState.Normal;
			}
			currentWindow.DesktopBounds = rc;
			currentWindow.Activate();
		}

		/// <summary>
		/// This method can be used to do the size and spacing calculations when tiling either
		/// side-by-side or stacked. It calculates two things: 1) The desired width or height
		/// of tiled windows. 2) how many pixels between the left or top edges of tiled windows.
		/// If the calculated width or height of a window is less than the allowable minimum,
		/// then tiled windows will be overlapped.
		/// </summary>
		/// <param name="scrn">The screen where the tiling will take place (only windows on this
		/// screen will actually get tiled).</param>
		/// <param name="screenDimension">The width or height, in pixels, of the display on
		/// which tiling will be performed.</param>
		/// <param name="minWindowDimension">The minimum allowable width or height, in pixels,
		/// of tiled windows.</param>
		/// <param name="desiredWindowDimension">The desired width or height, in pixels, of
		/// tiled windows.</param>
		/// <param name="windowSpacing">The distance, in pixels, between the left or top edge
		/// of each tiled window. If there is only one window, this is undefined.</param>
		private void CalcTileSizeAndSpacing(Screen scrn, int screenDimension, int minWindowDimension, out int desiredWindowDimension, out int windowSpacing)
		{
			CalcTileSizeAndSpacing(scrn, MainWindows, screenDimension, minWindowDimension, out desiredWindowDimension, out windowSpacing);
		}

		/// <summary>
		/// This method can be used to do the size and spacing calculations when tiling either
		/// side-by-side or stacked. It calculates two things: 1) The desired width or height
		/// of tiled windows. 2) how many pixels between the left or top edges of tiled windows.
		/// If the calculated width or height of a window is less than the allowable minimum,
		/// then tiled windows will be overlapped.
		/// </summary>
		/// <param name="scrn">The screen where the tiling will take place (only windows on this
		/// screen will actually get tiled).</param>
		/// <param name="windowsToTile">A list of all the windows to tile (including the
		/// current window)</param>
		/// <param name="screenDimension">The width or height, in pixels, of the display on
		/// which tiling will be performed.</param>
		/// <param name="minWindowDimension">The minimum allowable width or height, in pixels,
		/// of tiled windows.</param>
		/// <param name="desiredWindowDimension">The desired width or height, in pixels, of
		/// tiled windows.</param>
		/// <param name="windowSpacing">The distance, in pixels, between the left or top edge
		/// of each tiled window. If there is only one window, this is undefined.</param>
		private static void CalcTileSizeAndSpacing(Screen scrn, ICollection<IFwMainWnd> windowsToTile,
			int screenDimension, int minWindowDimension,
			out int desiredWindowDimension, out int windowSpacing)
		{
			var windowCount = windowsToTile.Count;

			// Don't count windows if they're minimized.
			foreach (Form wnd in windowsToTile)
			{
				if (wnd.WindowState == FormWindowState.Minimized || Screen.FromControl(wnd).WorkingArea != scrn.WorkingArea)
				{
					windowCount--;
				}
			}

			desiredWindowDimension = windowSpacing = screenDimension / windowCount;

			// Check if our desired window width is smaller than the minimum. If so, then
			// calculate what the overlap should be.
			if (desiredWindowDimension >= minWindowDimension)
			{
				return;
			}
			double overlap = (minWindowDimension * windowCount - screenDimension) / (windowCount - 1);

			windowSpacing = minWindowDimension - (int)Math.Round(overlap + 0.5);
			desiredWindowDimension = minWindowDimension;
		}

		/// <summary>
		/// Arrange the windows top to bottom or left to right.
		/// </summary>
		/// <param name="wndCurr">Current Window (i.e. window whose menu was used to issue a
		/// tile vertical or horizontal command.</param>
		/// <param name="orientation">The value indicating whether to tile side by side or
		/// stacked.</param>
		private void TileWindows(Form wndCurr, WindowTiling orientation)
		{
			TileWindows(wndCurr, MainWindows, orientation);
		}

		/// <summary>
		/// Arrange the windows top to bottom or left to right.
		/// </summary>
		/// <param name="wndCurr">Current Window (i.e. window whose menu was used to issue a
		/// tile vertical or horizontal command.</param>
		/// <param name="windowsToTile">A list of all the windows to tile (including the
		/// current window)</param>
		/// <param name="orientation">The value indicating whether to tile side by side or
		/// stacked.</param>
		private static void TileWindows(Form wndCurr, List<IFwMainWnd> windowsToTile, WindowTiling orientation)
		{
			// Get the screen in which to tile.
			var scrn = Screen.FromControl(wndCurr);
			int desiredDimension, windowSpacing;
			// At this point, assume the entire screen's working area is the desired size
			// and location for tiled windows, even though it's highly likely this will
			// change below.
			var rcDesired = scrn.WorkingArea;

			// Get the proper window width or height and the space between the windows
			// as they are tiled.
			if (orientation == WindowTiling.Stacked)
			{
				CalcTileSizeAndSpacing(scrn, windowsToTile, scrn.WorkingArea.Height, wndCurr.MinimumSize.Height, out desiredDimension, out windowSpacing);
				rcDesired.Height = desiredDimension;
			}
			else
			{
				CalcTileSizeAndSpacing(scrn, windowsToTile, scrn.WorkingArea.Width, wndCurr.MinimumSize.Width, out desiredDimension, out windowSpacing);
				rcDesired.Width = desiredDimension;
			}

			// There is a strange situation when a user's task bar is at the right or top
			// of the primary display. The working area returns the correct rectangle that
			// does not include the task bar. However, we cannot set a widnow's X or Y
			// coordinate to the working area's X or Y. If the window is to be located
			// in the upper left corner next to the task bar, X and Y must be 0.
			rcDesired.X -= ScreenHelper.TaskbarWidth;
			rcDesired.Y -= ScreenHelper.TaskbarHeight;

			// Move the active window to its proper place and size.
			wndCurr.DesktopBounds = rcDesired;

			// Now move the rest of the non minimized windows to their proper place.
			foreach (Form wnd in windowsToTile)
			{
				if (wnd.WindowState == FormWindowState.Maximized)
				{
					wnd.WindowState = FormWindowState.Normal;
				}

				if (wnd != wndCurr && wnd.WindowState != FormWindowState.Minimized && Screen.FromControl(wnd).WorkingArea == scrn.WorkingArea)
				{
					if (orientation == WindowTiling.Stacked)
					{
						rcDesired.Y += windowSpacing;
					}
					else
					{
						rcDesired.X += windowSpacing;
					}

					wnd.DesktopBounds = rcDesired;
				}
			}

			// If there was any overlapping of tiled windows, go from bottom to the top
			// or right to left and activate each window so the tiling looks correct. i.e.
			// Each window is overlapped on its top edge by the window directly on top or left.
			if (windowSpacing != desiredDimension)
			{
				for (var i = windowsToTile.Count - 1; i >= 0; i--)
				{
					var currentToTile = windowsToTile[i];
					var currentWindowToTile = (Form)currentToTile;
					if (currentToTile != wndCurr && currentWindowToTile.WindowState != FormWindowState.Minimized &&
						Screen.FromControl(currentWindowToTile).WorkingArea == scrn.WorkingArea)
					{
						currentWindowToTile.Activate();
					}
				}
			}

			// Finally, make the current window active.
			wndCurr.Activate();
		}

		/// <summary>
		/// Suppress execution of all synchronize messages and store them in a queue instead.
		/// </summary>
		private void SuppressSynchronize()
		{
			if (m_suppressedCacheInfo != null)
			{
				m_suppressedCacheInfo.Count++; // Nested call
			}
			else
			{
				m_suppressedCacheInfo = new SuppressedCacheInfo();
			}
		}

		/// <summary>
		/// Resume execution of synchronize messages. If there are any messages in the queue
		/// execute them now.
		/// </summary>
		private void ResumeSynchronize()
		{
			if (m_suppressedCacheInfo == null)
			{
				return; // Nothing to do
			}

			m_suppressedCacheInfo.Count--;
			if (m_suppressedCacheInfo.Count > 0)
			{
				return; // Still nested
			}

			BeginUpdate();
			var messages = m_suppressedCacheInfo.Queue;
			m_suppressedCacheInfo = null;

			var fProcessUndoRedoAfter = false;
			var savedUndoRedo = SyncMsg.ksyncFullRefresh; // Arbitrary
			foreach (var synchMsg in messages)
			{
				if (synchMsg == SyncMsg.ksyncUndoRedo)
				{
					// we must process this synch message after all the others
					fProcessUndoRedoAfter = true;
					savedUndoRedo = synchMsg;
					continue;
				}
				// Do the synch
				if (!Synchronize(synchMsg))
				{
					fProcessUndoRedoAfter = false; // Refresh already done, final UndoRedo unnecessary
					break; // One resulted in Refresh everything, ignore other synch msgs.
				}
			}

			if (fProcessUndoRedoAfter)
			{
				Synchronize(savedUndoRedo);
			}

			// NOTE: This code may present a race condition, because there is a slight
			// possibility that a sync message can come to the App at
			// this point and then get cleared from the syncMessages list and never get run.
			EndUpdate();
		}

		/// <summary>
		/// Suppress all calls to <see cref="T:RefreshAllViews()"/> until <see cref="T:EndUpdate"/> is called.
		/// </summary>
		/// <remarks>Used by <see cref="T:ResumeSynchronize"/> to do only one refresh of the view.</remarks>
		private void BeginUpdate()
		{
			CheckDisposed();

			Debug.Assert(DeclareRefreshInterest == RefreshInterest.NotSupressingRefresh, "Nested BeginUpdate");
			DeclareRefreshInterest = RefreshInterest.SupressingRefreshButDoNotWantRefresh;
		}

		/// <summary>
		/// Do a <see cref="T:RefreshAllViews()"/> if it was called at least once after <see cref="T:BeginUpdate"/>
		/// </summary>
		private void EndUpdate()
		{
			CheckDisposed();

			Debug.Assert(DeclareRefreshInterest != RefreshInterest.NotSupressingRefresh, "EndUpdate called without BeginUpdate");

			var needRefresh = DeclareRefreshInterest == RefreshInterest.SupressingRefreshAndWantRefresh;
			DeclareRefreshInterest = RefreshInterest.NotSupressingRefresh; // Make sure we don't try suppress the following RefreshAllViews()
			if (needRefresh)
			{
				RefreshAllViews();
			}
		}

		/// <summary>
		/// Build a list of files that can be moved (or copied) to the new LinkedFiles root
		/// directory.
		/// </summary>
		private static void CollectMovableFilesFromFolder(ICmFolder folder, ICollection<string> rgFilesToMove, string sOldRootDir, string sNewRootDir)
		{
			foreach (var file in folder.FilesOC)
			{
				var sFilepath = file.InternalPath;
				// only select files which have relative paths so they are in the LinkedFilesRootDir
				// or, don't put the same file in more than once!
				if (Path.IsPathRooted(sFilepath) || rgFilesToMove.Contains(sFilepath))
				{
					continue;
				}
				var sOldFilePath = Path.Combine(sOldRootDir, sFilepath);
				if (!FileUtils.TrySimilarFileExists(sOldFilePath, out sOldFilePath))
				{
					continue;
				}
				var sNewFilePath = Path.Combine(sNewRootDir, sFilepath);
				if (FileUtils.TrySimilarFileExists(sNewFilePath, out sNewFilePath))
				{
					//if the file exists in the destination LinkedFiles location, then only copy/move it if
					//file in the source location is newer.
					var dateTimeOfFileSourceFile = File.GetLastWriteTime(sOldFilePath);
					var dateTimeOfFileDestinationFile = File.GetLastWriteTime(sNewFilePath);
					if (dateTimeOfFileSourceFile > dateTimeOfFileDestinationFile)
					{
						rgFilesToMove.Add(sFilepath);
					}
				}
				else
				{
					//if the file does not exist in the destination LinkeFiles location then copy/move it.
					rgFilesToMove.Add(sFilepath);
				}
			}
			foreach (var sub in folder.SubFoldersOC)
			{
				CollectMovableFilesFromFolder(sub, rgFilesToMove, sOldRootDir, sNewRootDir);
			}
		}

		/// <summary>
		/// Expand the internal paths from relative to absolute as needed, since the user
		/// doesn't want to move (or copy) them.
		/// </summary>
		private static void ExpandToFullPath(ICmFolder folder, string sOldRootDir, string sNewRootDir)
		{
			foreach (var file in folder.FilesOC)
			{
				var sFilepath = file.InternalPath;
				if (Path.IsPathRooted(sFilepath))
				{
					continue;
				}
				if (FileUtils.SimilarFileExists(Path.Combine(sOldRootDir, sFilepath)) &&
					!FileUtils.SimilarFileExists(Path.Combine(sNewRootDir, sFilepath)))
				{
					file.InternalPath = Path.Combine(sOldRootDir, sFilepath);
				}
			}
			foreach (var sub in folder.SubFoldersOC)
			{
				ExpandToFullPath(sub, sOldRootDir, sNewRootDir);
			}
		}

#if RANDYTODO //Old Mediator stuff
		public bool OnRestoreDefaultLayouts(object commandObject)
					{
			CheckDisposed();

			Form formActive = ActiveForm;
			IFwMainWnd wndActive = formActive as IFwMainWnd;
			if (wndActive != null)
						{
				bool fRestore;
				using (RestoreDefaultsDlg dlg = new RestoreDefaultsDlg(this))
					fRestore = (dlg.ShowDialog(formActive) == DialogResult.Yes);
				if (fRestore)
							{
					InitializePartInventories(null, false);
					ReplaceMainWindow(wndActive);
						}
					}
					return true;
				}

		public bool OnHelpNotesLinguaLinksDatabaseImport(object sender)
					{
			CheckDisposed();

			string path = String.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}Language Explorer{0}Training{0}Technical Notes on LinguaLinks Database Import.pdf",
				Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(LanguageExplorerResources.ksCannotLaunchX, path),
					LanguageExplorerResources.ksError);
			});
			return true;
		}

		public bool OnHelpNotesInterlinearImport(object sender)
						{
			CheckDisposed();

			string path = String.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}Language Explorer{0}Training{0}Technical Notes on Interlinear Import.pdf",
				Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(LanguageExplorerResources.ksCannotLaunchX, path),
					LanguageExplorerResources.ksError);
			});
			return true;
						}

		public bool OnHelpNotesSFMDatabaseImport(object sender)
						{
			CheckDisposed();

			string path = String.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}Language Explorer{0}Training{0}Technical Notes on SFM Database Import.pdf",
				Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(LanguageExplorerResources.ksCannotLaunchX, path),
					LanguageExplorerResources.ksError);
			});
			return true;
		}

		/// <summary>
		/// Display a file given a path relative to the FieldWorks/Helps directory.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnHelpLexicographyIntro(object commandObject)
		{
			CheckDisposed();

			XCore.Command command = (XCore.Command)commandObject;
			string fileName = SIL.Utils.XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "file");
			fileName = fileName.Replace('\\', Path.DirectorySeparatorChar);
			string path = String.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}Language Explorer{0}Training{0}" + fileName, Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(FrameworkStrings.ksCannotShowX, path),
					LexTextStrings.ksError);
			});
			return true;
				}

		/// <summary>
		/// Display a file given a path relative to the FieldWorks/Helps directory.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnHelpHelpsFile(object commandObject)
		{
			CheckDisposed();

			XCore.Command command = (XCore.Command)commandObject;
			string fileName = SIL.Utils.XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "file");
			fileName = fileName.Replace('\\', Path.DirectorySeparatorChar);
			string path = String.Format(FwDirectoryFinder.CodeDirectory + "{0}Helps{0}" + fileName,
				Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(FrameworkStrings.ksCannotShowX, path),
					LexTextStrings.ksError);
			});
			return true;
		}

		public bool OnHelpMorphologyIntro(object sender)
		{
			CheckDisposed();

			string path = String.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}WW-ConceptualIntro{0}ConceptualIntroduction.htm",
				Path.DirectorySeparatorChar);

			OpenDocument(path, (e) => {
				MessageBox.Show(null, String.Format(LanguageExplorerResources.ksCannotLaunchX, path),
					LanguageExplorerResources.ksError);
			});
			return true;
		}
		#endif

		#region SuppressedCacheInfo class

		/// <summary>
		/// Helper class that contains queued SyncMsgs and a reference count for
		/// Suppress/ResumeSynchronize.
		/// </summary>
		private sealed class SuppressedCacheInfo
		{
			/// <summary>Reference count</summary>
			public int Count = 1;
			/// <summary>SyncMsg queue</summary>
			public readonly Queue<SyncMsg> Queue = new Queue<SyncMsg>();
		}

		#endregion

		/// <summary>
		/// The different window tiling options
		/// </summary>
		private enum WindowTiling
		{
			/// <summary>Top to bottom (horizontal)</summary>
			Stacked,
			/// <summary>Side by side (vertical)</summary>
			SideBySide,
		}
	}
}
