// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using IWshRuntimeLibrary;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.Archiving;
using SIL.FieldWorks.Common.Framework.Impls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.Utils.FileDialog;
using XCore;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Main window class for FW/FLEx.
	/// </summary>
	/// <remarks>
	/// Main CollapsingSplitContainer control for XWindow.
	/// It holds the Sidebar (m_sidebar) in its Panel1 (left side).
	/// It holds m_mainContentControl in Panel2, when m_recordBar is not showing.
	/// It holds another CollapsingSplitContainer (m_secondarySplitContainer) in Panel2,
	/// when the record list and the main control are both showing.
	///
	/// Controlling properties are:
	/// This is always true.
	/// property name="ShowSidebar" bool="true" persist="true"
	/// This is the splitter distance for the sidebar/secondary splitter pair of controls.
	/// property name="SidebarWidthGlobal" intValue="140" persist="true"
	/// This property is driven by the needs of the current main control, not the user.
	/// property name="ShowRecordList" bool="false" persist="true"
	/// This is the splitter distance for the record list/main content pair of controls.
	/// property name="RecordListWidthGlobal" intValue="200" persist="true"
	///
	/// Event handlers expected to be managed by areas/tools that are ostsensibly global:
	///		1. printToolStripMenuItem : the active tool can enable this and add an event handler, if needed.
	///		2. exportToolStripMenuItem : the active tool can enable this and add an event handler, if needed.
	/// </remarks>
	public partial class FwMainWnd : Form, IFwMainWnd
	{
		/// <summary>
		///  Web browser to use in Linux
		/// </summary>
		private string m_webBrowserProgramLinux = "firefox";
		private IAreaRepository m_areaRepository;

		private IArea m_currentArea;

		/// <summary>
		/// Create new instance of window.
		/// </summary>
		public FwMainWnd()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Create new instance of window.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "PropTable is disposed when closed.")]
		public FwMainWnd(FwMainWnd wndCopyFrom, FwLinkArgs linkArgs)
			: this()
		{
			if (wndCopyFrom != null)
			{
				throw new NotImplementedException("Support for the 'wndCopyFrom' is not yet implemented.");
			}
			if (linkArgs != null)
			{
				throw new NotImplementedException("Support for the 'linkArgs' is not yet implemented.");
			}
			_sendReceiveToolStripMenuItem.Enabled = FLExBridgeHelper.IsFlexBridgeInstalled();
			projectLocationsToolStripMenuItem.Enabled = FwRegistryHelper.FieldWorksRegistryKeyLocalMachine.CanWriteKey();
			archiveWithRAMPSILToolStripMenuItem.Enabled = ReapRamp.Installed;

			m_areaRepository = new AreaRepository();

			PropTable = new PropertyTable(new Mediator())
			{
				UserSettingDirectory = FdoFileHelper.GetConfigSettingsDir(FwApp.App.Cache.ProjectId.ProjectFolder),
				LocalSettingsId = "local"
			};
			if (!Directory.Exists(PropTable.UserSettingDirectory))
			{
				Directory.CreateDirectory(PropTable.UserSettingDirectory);
			}
			PropTable.RestoreFromFile(PropTable.GlobalSettingsId);
			PropTable.RestoreFromFile(PropTable.LocalSettingsId);

			// NOTE: The "lexicon" area must be present.
			// The persisted area could be obsolete, and not present,
			// so we'll use "lexicon", if the stored one cannot be found.
			// The "lexicon" area must be available, even if there are no other areas.
			m_currentArea = m_areaRepository.GetArea(PropTable.GetStringProperty("InitialArea", "lexicon")) ??
							m_areaRepository.GetArea("lexicon");
			// TODO: If no tool has been persisted, or persisted tool is not in persisted area, pick the default for persisted area.
			m_currentArea.Activate();

			SetWindowTitle();
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call PropertyTable.DoStuff.
		/// </summary>
		public PropertyTable PropTable { get; private set; }

		#endregion

		#region Implementation of IFwMainWnd

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the active view of the window
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IRootSite ActiveView { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the data object cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get { return FwApp.App.Cache; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the client windows and add correspnding stuff to the sidebar, View menu,  etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitAndShowClient()
		{
			CheckDisposed();

			Show();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a Rectangle representing the position and size of the window in its
		/// normal (non-minimized, non-maximized) state.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Rectangle NormalStateDesktopBounds { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called just before a window syncronizes it's views with DB changes (e.g. when an
		/// undo or redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// ------------------------------------------------------------------------------------
		public void PreSynchronize(SyncMsg sync)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window syncronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// <returns>true if successful; false results in RefreshAllWindows.</returns>
		/// ------------------------------------------------------------------------------------
		public bool Synchronize(SyncMsg sync)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window is finished being created and completely initialized.
		/// </summary>
		/// <returns>True if successful; false otherwise.  False should keep the main window
		/// from being shown/initialized (maybe even close the window if false is returned)
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool OnFinishedInit()
		{
			CheckDisposed();

#if USEXMLCONFIG
			if (m_startupLink != null)
				m_mediator.SendMessage("FollowLink", m_startupLink);
			UpdateControls();
#endif
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Find menu command.
		/// </summary>
		/// <param name="args">Arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnEditFind(object args)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepare to refresh the main window and its IAreas and ITools.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PrepareToRefresh()
		{
			// the original code in otherMainWindow.PrepareToRefresh() did this: m_mediator.SendMessageToAllNow("PrepareToRefresh", null);
			// There are/were three impls of "OnPrepareToRefresh": XWindow, XWorksViewBase, and InterlinMaster
			// The IArea & ITool interfaces have "PrepareToRefresh()" methods now.
			// TODO: When the relevant IArea and/or ITool impls are developed that use either
			// TODO: XWorksViewBase or InterlinMaster code, then those area/tool impls will need to call the
			// TODO: "OnPrepareToRefresh" (renamed to simply ""PrepareToRefresh"") methods on those classes.
			// TODO: The 'XWindow class will need nothing done to it, since it is just going to be deleted.
			// TODO (RandyR): Remove all of these comments, when XWorksViewBase or InterlinMaster code is put back in service.
			m_currentArea.PrepareToRefresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finish refreshing the main window and its IAreas and ITools.
		/// </summary>
		/// <remarks>
		/// This should call Refresh on real window implementations,
		/// after everything else is done.</remarks>
		/// ------------------------------------------------------------------------------------
		public void FinishRefresh()
		{
			m_currentArea.FinishRefresh();
			Refresh();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes all the views in this wiondow and in all others in the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshAllViews()
		{
			// Susanna asked that refresh affect only the currently active project, which is
			// what the string and List variables below attempt to handle.  See LT-6444.
			var activeWnd = ActiveForm as IFwMainWnd;

			var allMainWindowsExceptActiveWindow = new List<IFwMainWnd>();
			foreach (var otherMainWindow in FwApp.App.MainWindows.Where(mw => mw != activeWnd))
			{
				otherMainWindow.PrepareToRefresh();
				allMainWindowsExceptActiveWindow.Add(otherMainWindow);
			}

			// Now that all IFwMainWnds except currently active one have done basic refresh preparation,
			// have them all finish refreshing.
			foreach (var otherMainWindow in allMainWindowsExceptActiveWindow)
			{
				otherMainWindow.FinishRefresh();
			}

			// LT-3963: active IFwMainWnd changes as a result of a refresh.
			// Make sure focus doesn't switch to another FLEx application / window also
			// make sure the application focus isn't lost all together.
			// ALSO, after doing a refresh with just a single application / window,
			// the application would loose focus and you'd have to click into it to
			// get that back, this will reset that too.
			if (activeWnd != null)
			{
				// Refresh it last, so its saved settings get restored.
				activeWnd.FinishRefresh();
				var activeForm = activeWnd as Form;
				if (activeForm != null)
				{
					activeForm.Activate();
				}
			}
		}

		#endregion

		#region Implementation of IFWDisposable

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}

				// TODO: Is this comment still relvant?
				// TODO: Seems like FLEx worked well with it in this place (in the original window) for a long time.
				// The removing of the window needs to happen later; after this main window is
				// already disposed of. This is needed for side-effects that require a running
				// message loop.
				FwApp.App.FwManager.ExecuteAsync(FwApp.App.RemoveWindow, this);

				if (PropTable != null)
				{
					PropTable.Dispose();
					PropTable = null;
				}
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~FwMainWnd()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// This method throws an ObjectDisposedException if IsDisposed returns
		/// true.  This is the case where a method or property in an object is being
		/// used but the object itself is no longer valid.
		///
		/// This method should be added to all public properties and methods of this
		/// object and all other objects derived from it (extensive).
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion


		private void File_CloseWindow(object sender, EventArgs e)
		{
			Close();
		}

		private void Help_LanguageExplorer(object sender, EventArgs e)
		{
			var helpFile = FwApp.App.HelpFile;
			try
			{
				// When the help window is closed it will return focus to the window that opened it (see MSDN
				// documentation for HtmlHelp()). We don't want to use the main window as the parent, because if
				// a modal dialog is visible, it will still return focus to the main window, allowing the main window
				// to perform some behaviors (such as refresh by pressing F5) while the modal dialog is visible,
				// which can be bad. So, we just create a dummy control and pass that in as the parent.
				Help.ShowHelp(new Control(), helpFile);
			}
			catch (Exception)
			{
				MessageBox.Show(this, string.Format(FrameworkStrings.ksCannotLaunchX, helpFile),
					FrameworkStrings.ksError);
			}
		}

		private void Help_Training(object sender, EventArgs e)
		{
			using (var process = new Process())
			{
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.FileName = "http://wiki.lingtransoft.info/doku.php?id=tutorials:student_manual";
				process.Start();
				process.Close();
			}
		}

		private void Help_DemoMovies(object sender, EventArgs e)
		{
			try
			{
				var pathMovies = String.Format(FwDirectoryFinder.CodeDirectory +
					"{0}Language Explorer{0}Movies{0}Demo Movies.html",
					Path.DirectorySeparatorChar);

				OpenDocument<Win32Exception>(pathMovies, (win32err) =>
				{
					if (win32err.NativeErrorCode == 1155)
					{
						// The user has the movie files, but does not have a file association for .html files.
						// Try to launch Internet Explorer directly:
						using (Process.Start("IExplore.exe", pathMovies))
						{
						}
					}
					else
					{
						// User probably does not have movies. Try to launch the "no movies" web page:
						string pathNoMovies = String.Format(FwDirectoryFinder.CodeDirectory +
							"{0}Language Explorer{0}Movies{0}notfound.html",
							Path.DirectorySeparatorChar);

						OpenDocument<Win32Exception>(pathNoMovies, (win32err2) =>
						{
							if (win32err2.NativeErrorCode == 1155)
							{
								// The user does not have a file association for .html files.
								// Try to launch Internet Explorer directly:
								using (Process.Start("IExplore.exe", pathNoMovies))
								{
								}
							}
							else
								throw win32err2;
						});
					}
				});
			}
			catch (Exception)
			{
				// Some other unforeseen error:
				MessageBox.Show(null, String.Format(FrameworkStrings.ksErrorCannotLaunchMovies,
					string.Format(FwDirectoryFinder.CodeDirectory + "{0}Language Explorer{0}Movies",
					Path.DirectorySeparatorChar)), FrameworkStrings.ksError);
			}
		}

		/// <summary>
		/// Uses Process.Start to run path. If running in Linux and path ends in .html or .htm,
		/// surrounds the path in double quotes and opens it with a web browser.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="exceptionHandler"/>
		/// Delegate to run if an exception is thrown. Takes the exception as an argument.

		private void OpenDocument(string path, Action<Exception> exceptionHandler)
		{
			OpenDocument<Exception>(path, exceptionHandler);
		}

		/// <summary>
		/// Like OpenDocument(), but allowing specification of specific exception type T to catch.
		/// </summary>
		private void OpenDocument<T>(string path, Action<T> exceptionHandler) where T : Exception
		{
			try
			{
				if (MiscUtils.IsUnix && (path.EndsWith(".html") || path.EndsWith(".htm")))
				{
					using (Process.Start(m_webBrowserProgramLinux, Enquote(path)))
					{
					}
				}
				else
				{
					using (Process.Start(path))
					{
					}
				}
			}
			catch (T e)
			{
				if (exceptionHandler != null)
					exceptionHandler(e);
			}
		}

		/// <summary>
		/// Returns str surrounded by double-quotes.
		/// This is useful for paths containing spaces in Linux.
		/// </summary>
		private static string Enquote(string str)
		{
			return "\"" + str + "\"";
		}

		private void Help_Technical_Notes_on_FieldWorks_Send_Receive(object sender, EventArgs e)
		{
			string path = String.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}Language Explorer{0}Training{0}Technical Notes on FieldWorks Send-Receive.pdf",
				Path.DirectorySeparatorChar);

			OpenDocument(path, (err) =>
			{
				MessageBox.Show(null, String.Format(FrameworkStrings.ksCannotLaunchX, path),
					FrameworkStrings.ksError);
			});
		}

		private void Help_ReportProblem(object sender, EventArgs e)
		{
			ErrorReporter.ReportProblem(FwRegistryHelper.FieldWorksRegistryKey, FwApp.App.SupportEmailAddress, this);
		}

		private void Help_Make_a_Suggestion(object sender, EventArgs e)
		{
			ErrorReporter.MakeSuggestion(FwRegistryHelper.FieldWorksRegistryKey, "FLExDevteam@sil.org", this);
		}

		private void Help_About_Language_Explorer(object sender, EventArgs e)
		{
			using (var helpAboutWnd = new FwHelpAbout())
			{
				helpAboutWnd.ProductExecutableAssembly = Assembly.LoadFile(FwApp.App.ProductExecutableFile);
				helpAboutWnd.ShowDialog();
			}
		}

		private void File_New_FieldWorks_Project(object sender, EventArgs e)
		{
			if (FwApp.App.ActiveMainWindow != this)
				throw new InvalidOperationException("Unexpected active window for app.");
			FwApp.App.FwManager.CreateNewProject();
		}

		private void File_Open(object sender, EventArgs e)
		{
			FwApp.App.FwManager.ChooseLangProject();
		}

		private void File_FieldWorks_Project_Properties(object sender, EventArgs e)
		{
			// 'true' for either of these two menus,
			// but 'false' for fieldWorksProjectPropertiesToolStripMenuItem on the File menu.
			LaunchProjPropertiesDlg(sender == setUpWritingSystemsToolStripMenuItem || sender == setUpWritingSystemsToolStripMenuItem1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Launches the proj properties DLG.
		/// </summary>
		/// <param name="startOnWSPage">if set to <c>true</c> [start on WS page].</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "cache is disposed elsewhere.")]
		private void LaunchProjPropertiesDlg(bool startOnWSPage)
		{
			FdoCache cache = FwApp.App.Cache;
			if (!SharedBackendServicesHelper.WarnOnOpeningSingleUserDialog(cache))
				return;

			bool fDbRenamed = false;
			string sProject = cache.ProjectId.Name;
			string sLinkedFilesRootDir = cache.LangProject.LinkedFilesRootDir;
			using (var dlg = new FwProjPropertiesDlg(cache, FwApp.App, FwApp.App, FontHeightAdjuster.StyleSheetFromPropertyTable(PropTable)))
			{
				dlg.ProjectPropertiesChanged += OnProjectPropertiesChanged;
				if (startOnWSPage)
				{
					dlg.StartWithWSPage();
				}
				if (dlg.ShowDialog(this) != DialogResult.Abort)
				{
					// NOTE: This code is called, even if the user cancelled the dlg.
					fDbRenamed = dlg.ProjectNameChanged();
					if (fDbRenamed)
					{
						sProject = dlg.ProjectName;
					}
					bool fFilesMoved = false;
					if (dlg.LinkedFilesChanged())
					{
						fFilesMoved = FwApp.App.UpdateExternalLinks(sLinkedFilesRootDir);
					}
					// no need for any of these refreshes if entire window has been/will be
					// destroyed and recreated.
					if (!fDbRenamed && !fFilesMoved)
					{
						SetWindowTitle();
					}
				}
			}
			if (fDbRenamed)
			{
				FwApp.App.FwManager.RenameProject(sProject);
			}
		}

		private void SetWindowTitle()
		{
			Text = string.Format("{0} - {1} {2}",
				FwApp.App.Cache.ProjectId.UiName,
				FwUtils.FwUtils.ksSuiteName,
				FwUtils.FwUtils.ksSuiteName);
		}

		private void OnProjectPropertiesChanged(object sender, EventArgs eventArgs)
		{
			// this event is fired before the Project Properties dialog is closed, so that we have a chance
			// to refresh everything before Paint events start getting fired, which can cause problems if
			// any writing systems are removed that a rootsite is currently displaying
			var dlg = (FwProjPropertiesDlg)sender;
			if (dlg.WritingSystemsChanged())
			{
				View_Refresh(sender, eventArgs);
			}
		}

		/// <summary>
		/// This is the one (and should be only) handler for the user Refresh command.
		/// Refresh wants to first clean up the cache, then give things like Clerks a
		/// chance to reload stuff (calling the old OnRefresh methods), then give
		/// windows a chance to redisplay themselves.
		/// </summary>
		private void View_Refresh(object sender, EventArgs e)
		{
			RefreshAllViews();
		}

		private void File_Back_up_this_Project(object sender, EventArgs e)
		{
			// Have current IArea put any needed properties into the table.
			// Note: This covers what was done using: GlobalSettingServices.SaveSettings(Cache.ServiceLocator, m_propertyTable);
			m_currentArea.EnsurePropertiesAreCurrent(PropTable);
			// first save global settings, ignoring database specific ones.
			PropTable.SaveGlobalSettings();
			// now save database specific settings.
			PropTable.SaveLocalSettings();

			FwApp.App.FwManager.BackupProject(this);
		}

		private void File_Restore_a_Project(object sender, EventArgs e)
		{
			FwApp.App.FwManager.RestoreProject(FwApp.App, this);
		}

		private void File_Project_Location(object sender, EventArgs e)
		{
			FwApp.App.FwManager.FileProjectLocation(FwApp.App, this);
		}

		private void File_Delete_Project(object sender, EventArgs e)
		{
			FwApp.App.FwManager.DeleteProject(FwApp.App, this);
		}

		private void File_Create_Shortcut_on_Desktop(object sender, EventArgs e)
		{
			var directory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			if (!FileUtils.DirectoryExists(directory))
			{
				MessageBoxUtils.Show(string.Format(
					"Error: Cannot create project shortcut because destination directory '{0}' does not exist.",
					directory));
				return;
			}

			var applicationArguments = "-" + FwAppArgs.kProject + " \"" + FwApp.App.Cache.ProjectId.Handle + "\"";
			var description = ResourceHelper.FormatResourceString(
				"kstidCreateShortcutLinkDescription", FwApp.App.Cache.ProjectId.UiName,
				FwApp.App.ApplicationName);

			if (MiscUtils.IsUnix)
			{
				var projectName = FwApp.App.Cache.ProjectId.UiName;
				const string pathExtension = ".desktop";
				var launcherPath = Path.Combine(directory, projectName + pathExtension);

				// Choose a different name if already in use
				int tailNumber = 2;
				while (FileUtils.SimilarFileExists(launcherPath))
				{
					var tail = "-" + tailNumber;
					launcherPath = Path.Combine(directory, projectName + tail + pathExtension);
					tailNumber++;
				}

				const string applicationExecutablePath = "fieldworks-flex";
				const string iconPath = "fieldworks-flex";
				if (string.IsNullOrEmpty(applicationExecutablePath))
					return;
				var content = String.Format(
					"[Desktop Entry]{0}" +
					"Version=1.0{0}" +
					"Terminal=false{0}" +
					"Exec=" + applicationExecutablePath + " " + applicationArguments + "{0}" +
					"Icon=" + iconPath + "{0}" +
					"Type=Application{0}" +
					"Name=" + projectName + "{0}" +
					"Comment=" + description + "{0}", Environment.NewLine);

				// Don't write a BOM
				using (var launcher = FileUtils.OpenFileForWrite(launcherPath, new UTF8Encoding(false)))
				{
					launcher.Write(content);
					FileUtils.SetExecutable(launcherPath);
				}
			}
			else
			{
				WshShell shell = new WshShellClass();

				var filename = FwApp.App.Cache.ProjectId.UiName;
				filename = Path.ChangeExtension(filename, "lnk");
				var linkPath = Path.Combine(directory, filename);

				var link = (IWshShortcut)shell.CreateShortcut(linkPath);
				if (link.FullName != linkPath)
				{
					var msg = string.Format(FrameworkStrings.ksCannotCreateShortcut,
						FwApp.App.ProductExecutableFile + " " + applicationArguments);
					MessageBox.Show(ActiveForm, msg,
						FrameworkStrings.ksCannotCreateShortcutCaption, MessageBoxButtons.OK,
						MessageBoxIcon.Asterisk);
					return;
				}
				link.TargetPath = FwApp.App.ProductExecutableFile;
				link.Arguments = applicationArguments;
				link.Description = description;
				link.IconLocation = link.TargetPath + ",0";
				link.Save();
			}
		}

		private void File_Archive_With_RAMP(object sender, EventArgs e)
		{
			// prompt the user to select or create a FieldWorks backup
			var filesToArchive = FwApp.App.FwManager.ArchiveProjectWithRamp(FwApp.App, this);

			// if there are no files to archive, return now.
			if((filesToArchive == null) || (filesToArchive.Count == 0))
				return;

			// show the RAMP dialog
			var ramp = new ReapRamp();
			ramp.ArchiveNow(this, MainMenuStrip.Font, Icon, filesToArchive, PropTable, FwApp.App, FwApp.App.Cache);
		}

		private void File_Page_Setup(object sender, EventArgs e)
		{
			throw new NotSupportedException("There was no code to support this menu in the original system.");
		}

		private void File_Translated_List_Content(object sender, EventArgs e)
		{
			string filename;
			// ActiveForm can go null (see FWNX-731), so cache its value, and check whether
			// we need to use 'this' instead (which might be a better idea anyway).
			var form = ActiveForm ?? this;
			using (var dlg = new OpenFileDialogAdapter())
			{
				dlg.CheckFileExists = true;
				dlg.RestoreDirectory = true;
				dlg.Title = ResourceHelper.GetResourceString("kstidOpenTranslatedLists");
				dlg.ValidateNames = true;
				dlg.Multiselect = false;
				dlg.Filter = ResourceHelper.FileFilter(FileFilterType.FieldWorksTranslatedLists);
				if (dlg.ShowDialog(form) != DialogResult.OK)
				{
					return;
				}
				filename = dlg.FileName;
			}
			using (new WaitCursor(form, true))
			{
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(FwApp.App.Cache.ActionHandlerAccessor,
					() =>
					{
						using (var dlg = new ProgressDialogWithTask(this))
						{
							dlg.AllowCancel = true;
							dlg.Maximum = 200;
							dlg.Message = filename;
							dlg.RunTask(true, FdoCache.ImportTranslatedLists, filename, FwApp.App.Cache);
						}
					});
			}
		}
	}
}
