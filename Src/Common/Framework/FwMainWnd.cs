using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Main window class for FW/FLEx.
	/// </summary>
	public partial class FwMainWnd : Form, IFwMainWnd
	{
		/// <summary>
		///  Web browser to use in Linux
		/// </summary>
		private string m_webBrowserProgramLinux = "firefox";

		/// <summary>
		/// Create new instance of window.
		/// </summary>
		public FwMainWnd()
		{
			InitializeComponent();

			PropTable = new PropertyTable(new Mediator());
		}

		/// <summary>
		/// Create new instance of window.
		/// </summary>
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
		public FdoCache Cache { get; private set; }

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
		/// Refreshes all the views that belong to this main window
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshAllViews()
		{
			throw new NotImplementedException();
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
	}
}
