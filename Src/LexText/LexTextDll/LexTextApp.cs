// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LexTextApp.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Resources;
using System.Reflection;
using System.Globalization;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.Win32;
using System.Xml;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.Utils;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FwCoreDlgs;
using XCore;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.FwUtils;
using System.ComponentModel;

namespace SIL.FieldWorks.XWorks.LexText
{
	/// <summary>
	/// Summary description for LexTextApp.
	/// </summary>
	public class LexTextApp : FwXApp, IApp, IxCoreColleague
	{
		private static ResourceManager s_helpResources = null;

		protected XMessageBoxExManager m_messageBoxExManager;
		private CreateModifyTimeManager m_cmTimeManager;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Application entry point. If LexTextApp isn't already running,
		/// an instance of the app is created.
		/// </summary>
		/// <param name="rgArgs">Command-line arguments</param>
		/// <returns>0</returns>
		/// -----------------------------------------------------------------------------------
		[STAThread]
		public static int Main(string[] rgArgs)
		{
			// Enable visual styles. Ignored on Windows 2000. Needs to be called before
			// we create any controls! Unfortunately, this alone is not good enough. We
			// also need to use a manifest, because some ListView and TreeView controls
			// in native code do not have icons if we just use this method. This is caused
			// by a bug in XP.
			Application.EnableVisualStyles();

			// Create a semaphore to keep more than one instance of the application
			// from running at the same time.  If the semaphore is signalled, then
			// this instance can run.
			Win32.SecurityAttributes sa = new Win32.SecurityAttributes();
			IntPtr semaphore = Win32.CreateSemaphore(ref sa, 1, 1,
				Process.GetCurrentProcess().MainModule.ModuleName);
			switch (Win32.WaitForSingleObject(semaphore, 0))
			{
				case Win32.WAIT_OBJECT_0:
					// Using the 'using' gizmo will call Dispose on app,
					// which in turn will call Dispose for all FdoCache objects,
					// which will release all of the COM objects it connects to.
					using (LexTextApp application = new LexTextApp(rgArgs))
					{
						SIL.Utils.ErrorReporter.EmailAddress = "FlexErrors@sil.org";

						string extensionBaseDir = SIL.FieldWorks.Common.Utils.DirectoryFinder.GetFWDataSubDirectory(@"\Language Explorer\Configuration\Words\Extensions");
						string extensionDir = Path.Combine(extensionBaseDir, "Respeller");
						if (Directory.Exists(extensionDir))
						{
							try
							{
								foreach (string file in Directory.GetFiles(extensionDir))
								{
									File.SetAttributes(file, FileAttributes.Normal);
								}
								Directory.Delete(extensionDir, true);
							}
							catch
							{
								MessageBox.Show(String.Format("Please delete the '{0}' folder and all files in it, in order to run Language Explorer.", extensionDir));
								return 1;
							}
						}
						extensionDir = Path.Combine(extensionBaseDir, "DeleteWordforms");
						if (Directory.Exists(extensionDir))
						{
							try
							{
								foreach (string file in Directory.GetFiles(extensionDir))
								{
									File.SetAttributes(file, FileAttributes.Normal);
								}
								Directory.Delete(extensionDir, true);
							}
							catch
							{
								MessageBox.Show(String.Format("Please delete the '{0}' folder and all files in it, in order to run Language Explorer.", extensionDir));
								return 1;
							}
						}

						application.Run();
					}
					int previousCount;
					Win32.ReleaseSemaphore(semaphore, 1, out previousCount);
					break;

				case Win32.WAIT_TIMEOUT:
					// If the semaphore wait times out then another instance is running.
					// Try to get a handle to its window and activate it.  Then terminate
					// this process.
					try
					{
						IntPtr hWndMain = ExistingProcess.MainWindowHandle;
						if (hWndMain != (IntPtr)0)
							Win32.SetForegroundWindow(hWndMain);
					}
					catch
					{
						// The other instance does not have a window handle.  It is either in
						// the process of starting up or shutting down.
					}
					break;
			}

			return 0;
		}


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="arguments">Command line arguments.</param>
		protected LexTextApp(string[] arguments) : base(arguments)
		{
		}

		/// <summary>
		/// needed for automated tests
		/// </summary>
		/// <param name="arguments"></param>
		public LexTextApp() : base()
		{
		}

		/// <summary>
		/// Use this for slow operations that should happen during the splash screen instead of
		/// during app construction
		/// </summary>
		protected override void DoApplicationInitialization()
		{
			base.DoApplicationInitialization();
			InitializeMessageDialogs();
			WriteSplashScreen(LexTextStrings.ksLoading_);

		}

		/// <summary>
		/// Initialize the required inventories.
		/// </summary>
		private void InitializeMessageDialogs()
		{
			WriteSplashScreen(LexTextStrings.ksInitializingMessageDialogs_);
			m_messageBoxExManager = XMessageBoxExManager.CreateXMessageBoxExManager();
			m_messageBoxExManager.DefineMessageBox("TextChartNewFeature",
				LexTextStrings.ksNewFeature,
				LexTextStrings.ksChartTemplateWarning, true, "exclamation");
			m_messageBoxExManager.DefineMessageBox("CategorizedEntry-Intro",
				LexTextStrings.ksInformation,
				LexTextStrings.ksUsedForSemanticBasedEntry, true, "info");
			m_messageBoxExManager.DefineMessageBox("CreateNewFromGrammaticalCategoryCatalog",
				LexTextStrings.ksInformation,
				LexTextStrings.ksCreatingCustomGramCategory, true, "info");
			m_messageBoxExManager.DefineMessageBox("CreateNewLexicalReferenceType",
				LexTextStrings.ksInformation,
				LexTextStrings.ksCreatingCustomLexRefType, true, "info");
			m_messageBoxExManager.DefineMessageBox("ClassifiedDictionary-Intro",
				LexTextStrings.ksInformation,
				LexTextStrings.ksShowingSemanticClassification, true, "info");

			m_messageBoxExManager.ReadSettingsFile();
			WriteSplashScreen("");
		}

		/// <summary>
		/// Initialize the required inventories.
		/// </summary>
		private void InitializePartInventories(FdoCache cache, bool fLoadUserOverrides)
		{
			WriteSplashScreen(LexTextStrings.ksInitializingLayouts_);
			LayoutCache.InitializePartInventories(false, cache.DatabaseName, fLoadUserOverrides);
			int flid = BaseVirtualHandler.GetInstalledHandlerTag(cache, "LexDb", "CurrentReversalIndices");
			if (flid == 0)
				flid = (int)LexDb.LexDbTags.kflidReversalIndexes;
			int[] rghvoIndexes = cache.GetVectorProperty(cache.LangProject.LexDbOA.Hvo, flid, false);
			foreach (int hvoIndex in rghvoIndexes)
			{
				int ws = cache.GetObjProperty(hvoIndex, (int)ReversalIndex.ReversalIndexTags.kflidWritingSystem);
				string sWs = cache.GetUnicodeProperty(ws, (int)LgWritingSystem.LgWritingSystemTags.kflidICULocale);
				LayoutCache.InitializeLayoutsForWsTag(sWs, cache.DatabaseName);
			}
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
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed || BeingDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_messageBoxExManager != null)
					m_messageBoxExManager.Dispose();
				if (m_cmTimeManager != null)
					m_cmTimeManager.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_messageBoxExManager = null;
			m_cmTimeManager = null;

			base.Dispose(disposing);
		}


		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Guid for the application (used for uniquely identifying DB items that "belong" to
		///		this app.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public static Guid AppGuid
		{
			get
			{
				return new Guid("E716C901-3171-421f-83E1-3E012DEC9489");
			}
		}

		/// <summary>
		/// This application processes DB sync records.
		/// </summary>
		public override Guid SyncGuid
		{
			get
			{
				CheckDisposed();
				return AppGuid;
			}
		}

		public override string ProductName
		{
			get
			{
				CheckDisposed();
				return LexTextStrings.ksFieldWorksLanguageExplorer;
			}
		}

		public override string DefaultConfigurationPathname
		{
			get
			{
				CheckDisposed();
				return @"Language Explorer\Configuration\Main.xml";
			}
		}

		private static bool m_fResourceFailed = false;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>string</returns>
		/// -----------------------------------------------------------------------------------
		string SIL.FieldWorks.Common.RootSites.IApp.ResourceString(string stid)
		{
			CheckDisposed();

			try
			{
				// No need to allocate a different ResourceManager than the one the generated code
				// produces, and it should be more reliable (I hope).
				//s_stringResources = new System.Resources.ResourceManager(
				//    "SIL.FieldWorks.XWorks.LexText.LexTextStrings", Assembly.GetExecutingAssembly());
				return (stid == null ? "NullStringID" : LexTextStrings.ResourceManager.GetString(stid));
			}
			catch (Exception e)
			{
				if (!m_fResourceFailed)
				{
					MessageBox.Show(null,
						String.Format(LexTextStrings.ksErrorLoadingResourceStrings, e.Message),
						LexTextStrings.ksError);
					m_fResourceFailed = true;
				}
				if (stid == null)
					return "NullStringID";
				else
					return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The HTML help file (.chm) for Flex.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string HelpFile
		{
			get
			{
				CheckDisposed();

				return DirectoryFinder.FWCodeDirectory + GetHelpString("UserHelpFile", 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the sample DB for the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string SampleDatabase
		{
			get
			{
				CheckDisposed();
				return "Sena 3";
			}
		}

		/// <summary>
		/// override this with the name of your icon
		/// This icon file should be included in the assembly, and its "build action" should be set to "embedded resource"
		/// </summary>
		protected override string ApplicationIconName
		{
			get { return "lt.ico"; }
		}

		/// <summary>
		/// Gets the registry settings key name for the application.
		/// </summary>
		/// <remarks>Subclasses should override this, or all its settings will go in "FwXapp".</remarks>
		protected override string SettingsKeyName
		{
			get { return "Language Explorer"; }
		}

		#region IHelpTopicProvider implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a help file URL or topic
		/// </summary>
		/// <param name="stid"></param>
		/// <param name="iKey"></param>
		/// <returns>The requested string</returns>
		/// ------------------------------------------------------------------------------------
		public override string GetHelpString(string stid, int iKey)
		{
			CheckDisposed();

			if (s_helpResources == null)
			{
				s_helpResources = new System.Resources.ResourceManager(
					"SIL.FieldWorks.XWorks.LexText.HelpTopicPaths", Assembly.GetExecutingAssembly());
			}

			if (stid == null)
			{
				return "NullStringID";
			}
			else
			{
				string helpString = s_helpResources.GetString(stid); // First try to find it in our resource file
				if (helpString == null) // If that doesn't work, try the more general one
					helpString = base.GetHelpString(stid, iKey);
				return helpString;
			}
		}
		#endregion

		/// <summary>
		/// Used to launch various import dialogs, but could do other things
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnLaunchConnectedDialog(object commandObject)
		{
			CheckDisposed();

			XCore.Command command = (XCore.Command)commandObject;
			System.Xml.XmlNode first = command.Parameters[0];
			System.Xml.XmlNode classInfo = first.SelectSingleNode("dynamicloaderinfo");

			Form formActive = ActiveForm;
			FdoCache cache = GetActiveCache(formActive);

			if (cache != null)
			{
				FwXWindow wndActive = formActive as FwXWindow;
				SIL.FieldWorks.LexText.Controls.IFwExtension dlg = null;
				try
				{
					try
					{
						dlg = (SIL.FieldWorks.LexText.Controls.IFwExtension)Utils.DynamicLoader.CreateObject(classInfo);
					}
					catch (Exception error)
					{
						string message = XmlUtils.GetOptionalAttributeValue(classInfo, "notFoundMessage", null);	// Make this localizable!
						if (message != null)
							throw new ApplicationException(message, error);
					}
					dlg.Init(cache, wndActive.Mediator);
					DialogResult dr = ((Form)dlg).ShowDialog(ActiveForm);
					if (dr == DialogResult.OK)
					{
						if (dlg is LexOptionsDlg)
						{
							LexOptionsDlg loDlg = dlg as LexOptionsDlg;
							string sWsUserOld = s_sWsUser;
							s_sWsUser = loDlg.NewUserWs;
							if ((sWsUserOld != s_sWsUser) || loDlg.PluginsUpdated)
							{
								// Make everything we've imported visible,
								// or make the plugin install/uninstall real.
								wndActive.Mediator.SendMessage("MasterRefresh", wndActive);
							}
						}
						else if (dlg is SIL.FieldWorks.IText.LinguaLinksImportDlg ||
							dlg is SIL.FieldWorks.IText.InterlinearImportDlg ||
							dlg is SIL.FieldWorks.LexText.Controls.LexImportWizard ||
							dlg is SIL.FieldWorks.LexText.Controls.LiftImportDlg)
						{
							// Make everything we've imported visible.
							wndActive.Mediator.SendMessage("MasterRefresh", wndActive);
						}
					}
				}
				finally
				{
					if (dlg != null && dlg is IDisposable)
						(dlg as IDisposable).Dispose();
				}
			}
			return true;
		}

		private FdoCache GetActiveCache(Form formActive)
		{
			FdoCache cache = null;
			foreach (FwXWindow wnd in m_rgMainWindows)
			{
				if ((object)wnd == (object)formActive)
				{
					cache = wnd.Cache;
					break;
				}
			}
			return cache;
		}
		/*
				public bool OnImportSFMLexicon(object sender)
				{
					CheckDisposed();

					FdoCache cache = null;
					Form formActive = ActiveForm;
					foreach (FwXWindow wnd in m_rgMainWindows)
					{
						if ((object)wnd == (object)formActive)
						{
							cache = wnd.Cache;
							break;
						}
					}

					if (cache != null)
					{
						FwXWindow wndActive = formActive as FwXWindow;
						//SIL.FieldWorks.LexText.Controls.ImportLexiconDlg dlg =
						//	new SIL.FieldWorks.LexText.Controls.ImportLexiconDlg(cache, "", "");
						SIL.FieldWorks.LexText.Controls.LexImportWizard dlg =
							new SIL.FieldWorks.LexText.Controls.LexImportWizard(cache, wndActive.Mediator );
						if(dlg.ShowDialog(ActiveForm) == DialogResult.OK)

						{
							// Make everything we've imported visible.  Note that this needs to happen regardless of what the return value of ShowDialog is.
							// An exception during processing may cause the result to not be OK, but entries will still have been added.
							wndActive.Mediator.BroadcastMessage("MasterRefresh", null);
						}
					}
					return true;
				}

				/// <summary>
				/// Launch a dialog that will allow the user to import LinguaLinks data.
				/// </summary>
				/// <param name="sender"></param>
				/// <returns>true, to indicate it was handled here.</returns>
				public bool OnImportLinguaLinksData(object sender)
				{
					FdoCache cache = null;
					Form formActive = ActiveForm;
					foreach (FwXWindow wnd in m_rgMainWindows)
					{
						if ((object)wnd == (object)formActive)
						{
							cache = wnd.Cache;
							break;
						}
					}
					if (cache != null)
					{
						FwXWindow wndActive = formActive as FwXWindow;
						using (LinguaLinksImportDlg dlg =
							new LinguaLinksImportDlg(cache, wndActive.Mediator))
						{
							if (dlg.ShowDialog(ActiveForm) == DialogResult.OK)
							{
								// Make everything we've imported visible.
								wndActive.Mediator.SendMessage("MasterRefresh", wndActive);
							}

						}
					}
					return true;
				}
		*/

		public bool OnRestoreDefaultLayouts(object commandObject)
		{
			CheckDisposed();

			Form formActive = ActiveForm;
			FdoCache cache = GetActiveCache(formActive);
			FwXWindow wndActive = formActive as FwXWindow;
			if (cache != null && wndActive != null)
			{
				bool fRestore = false;
				using (RestoreDefaultsDlg dlg = new RestoreDefaultsDlg())
				{
					fRestore = (dlg.ShowDialog(formActive) == DialogResult.Yes);
				}
				if (fRestore)
				{
					InitializePartInventories(cache, false);
					wndActive.Mediator.BroadcastMessage("MasterRefresh", wndActive);
				}
			}
			return true;
		}

		/// <summary>
		/// This implements the "Synchronize with LiftShare..." menu command.
		/// </summary>
		/// <param name="sender"></param>
		/// <returns></returns>
		/// <remarks>Until LiftShare is fully implemented, this is irrelevant.</remarks>
		public bool OnSynchronize(object sender)
		{
			FdoCache cache = null;
			Form formActive = ActiveForm;
			foreach (FwXWindow wnd in m_rgMainWindows)
			{
				if ((object)wnd == (object)formActive)
				{
					cache = wnd.Cache;
					break;
				}
			}
			if (cache != null)
			{
				FwXWindow wndActive = formActive as FwXWindow;
				LiftSynchronizeDlg dlg = new LiftSynchronizeDlg(cache, wndActive.Mediator);
				dlg.ShowDialog(formActive);
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// On Refresh, we want to reload the XML configuration files.  This greatly facilitates developing
		/// those files, even though it's not as useful for normal use.  It might prove useful whenever we
		/// get around to allowing user customization (or it might not).
		/// </summary>
		/// <param name="sender"></param>
		/// <returns></returns>
		public bool OnRefresh(object sender)
		{
			CheckDisposed();
			Set<string> setDatabases = new Set<string>();
			foreach (FwXWindow wnd in m_rgMainWindows)
			{
				string sDatabase = wnd.Cache.DatabaseName;
				if (setDatabases.Contains(sDatabase))
					continue;
				setDatabases.Add(sDatabase);
				Inventory.GetInventory("layouts", sDatabase).ReloadIfChanges();
				Inventory.GetInventory("parts", sDatabase).ReloadIfChanges();
			}
			return false;
		}

		public bool OnHelpUserManual(object sender)
		{
			CheckDisposed();

			string path = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\Training\Flex Student Manual.doc");
			try
			{
				System.Diagnostics.Process.Start(path);
			}
			catch (Exception)
			{
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotLaunchX, path),
					LexTextStrings.ksError);
			}
			return true;
		}

		public bool OnHelpInstructorGuide(object sender)
		{
			CheckDisposed();

			string path = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\Training\FLEx Instructor Guide.doc");
			try
			{
				System.Diagnostics.Process.Start(path);
			}
			catch (Exception)
			{
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotLaunchX, path),
					LexTextStrings.ksError);
			}
			return true;
		}

		public bool OnHelpNotesLinguaLinksDatabaseImport(object sender)
		{
			CheckDisposed();

			string path = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\Training\Technical Notes on LinguaLinks Database Import.doc");
			try
			{
				System.Diagnostics.Process.Start(path);
			}
			catch(Exception)
			{
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotLaunchX, path),
					LexTextStrings.ksError);
			}
			return true;
		}

		public bool OnHelpNotesInterlinearImport(object sender)
		{
			CheckDisposed();

			string path = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\Training\Technical Notes on Interlinear Import.doc");
			try
			{
				System.Diagnostics.Process.Start(path);
			}
			catch(Exception)
			{
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotLaunchX, path),
					LexTextStrings.ksError);
			}
			return true;
		}

		public bool OnHelpNotesSFMDatabaseImport(object sender)
		{
			CheckDisposed();

			string path = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\Training\Technical Notes on SFM Database Import.doc");
			try
			{
				System.Diagnostics.Process.Start(path);
			}
			catch(Exception)
			{
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotLaunchX, path),
					LexTextStrings.ksError);
			}
			return true;
		}

		/// <summary>
		/// Display a file from the Language Explorer\Training directory.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnHelpTrainingFile(object commandObject)
		{
			CheckDisposed();

			XCore.Command command = (XCore.Command)commandObject;
			string fileName = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "file");
			string path = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\Training\" + fileName);
			try
			{
				System.Diagnostics.Process.Start(path);
			}
			catch (Exception)
			{
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotShowX, path),
					LexTextStrings.ksError);
			}
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
			string fileName = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "file");
			string path = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\Training\" + fileName);
			try
			{
				System.Diagnostics.Process.Start(path);
			}
			catch (Exception)
			{
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotShowX, path),
					LexTextStrings.ksError);
			}
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
			string fileName = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "file");
			string path = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Helps\" + fileName);
			try
			{
				System.Diagnostics.Process.Start(path);
			}
			catch (Exception)
			{
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotShowX, path),
					LexTextStrings.ksError);
			}
			return true;
		}

		public bool OnHelpDemoMovies(object commandObject)
		{
			CheckDisposed();

			try
			{
				string pathMovies = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\Movies\Demo Movies.html");
				try
				{
					Process p = System.Diagnostics.Process.Start(pathMovies);
				}
				catch (Win32Exception win32err)
				{
					if (win32err.NativeErrorCode == 1155)
					{
						// The user has the movie files, but does not have a file association for .html files.
						// Try to launch Internet Explorer directly:
						Process.Start("IExplore.exe", pathMovies);
					}
					else
					{
						// User probably does not have movies. Try to launch the "no movies" web page:
						string pathNoMovies = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\Movies\notfound.html");
						try
						{
							System.Diagnostics.Process.Start(pathNoMovies);
						}
						catch (Win32Exception win32err2)
						{
							if (win32err2.NativeErrorCode == 1155)
							{
								// The user does not have a file association for .html files.
								// Try to launch Internet Explorer directly:
								Process.Start("IExplore.exe", pathNoMovies);
							}
							else
								throw win32err2;
						}
					}
				}
			}
			catch (Exception)
			{
				// Some other unforeseen error:
				MessageBox.Show(null, String.Format(LexTextStrings.ksErrorCannotLaunchMovies,
					Path.Combine(DirectoryFinder.FWCodeDirectory, @"Language Explorer\Movies")),
					LexTextStrings.ksError);
			}
			return true;
		}

		public bool OnHelpMorphologyIntro(object sender)
		{
			CheckDisposed();

			string path = Path.Combine(DirectoryFinder.FWCodeDirectory, @"Helps\WW-ConceptualIntro\ConceptualIntroduction.htm");
			try
			{
				System.Diagnostics.Process.Start(path);
			}
			catch(Exception)
			{
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotLaunchX, path),
					LexTextStrings.ksError);
			}
			return true;
		}

		/// <summary>
		/// Launch the main help file for Flex.
		/// </summary>
		/// <param name="sender"></param>
		/// <returns></returns>
		public bool OnHelpLanguageExplorer(object sender)
		{
			CheckDisposed();

			try
			{
				// When the help window is closed it will return focus to the window that opened it (see MSDN
				// documentation for HtmlHelp()). We don't want to use the main window as the parent, because if
				// a modal dialog is visible, it will still return focus to the main window, allowing the main window
				// to perform some behaviors (such as refresh by pressing F5) while the modal dialog is visible,
				// which can be bad. So, we just create a dummy control and pass that in as the parent.
				Help.ShowHelp(new Control(), HelpFile);
			}
			catch(Exception)
			{
				MessageBox.Show(null, String.Format(LexTextStrings.ksCannotLaunchX, HelpFile),
					LexTextStrings.ksError);
			}
			return true;
		}

		protected override Form NewMainAppWnd(FdoCache cache, bool isNewCache, Form wndCopyFrom,
			bool fOpeningNewProject)
		{
			WriteSplashScreen(String.Format(LexTextStrings.ksCreatingWindowForX, cache.DatabaseName));

			// The try-catch block is modeled after that used by TeScrInitializer.Initialize(),
			// as the suggestion for fixing LT-8797.
			try
			{
				// Make sure this DB uses the current stylesheet version.
				if (MiscUtils.IsServerLocal(cache.ServerName) && cache.GetNumberOfRemoteClients() == 0)
					FlexStylesXmlAccessor.EnsureCurrentStylesheet(cache.LangProject);
			}
			catch (WorkerThreadException e)
			{
				UndoResult ures;
				while (cache.Undo(out ures)) ; // Enhance JohnT: make use of ures?
				MessageBox.Show(Form.ActiveForm, e.InnerException.Message,
					LexTextStrings.ksFieldWorksLanguageExplorer,
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return null;
			}
			Form form = base.NewMainAppWnd(cache, isNewCache, wndCopyFrom, fOpeningNewProject);

			// Ensure that all the relevant writing systems are installed.
			if (isNewCache)
			{
				ILangProject lp = cache.LangProject;
				// Loop through the Vernacular WS and initialize them
				foreach (ILgWritingSystem ws in lp.VernWssRC)
					cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws.Hvo);
				// Loop through the Analysis WS and initialize them
				foreach (ILgWritingSystem ws in lp.AnalysisWssRC)
					cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(ws.Hvo);
			}
			cache.Save();

			if (form != null && form is FwXWindow)
			{
				FwXWindow wnd = form as FwXWindow;
				wnd.Mediator.BroadcastPendingItems();
				IFwCheckAnthroList fcal = FwCheckAnthroListClass.Create();
				string sDesc = wnd.Mediator.StringTbl.GetString("AnthroListUse", "DialogStrings");
				fcal.Description = sDesc;
				string sHelpFile = HelpFile;
				fcal.HelpFilename = sHelpFile;
				fcal.CheckAnthroList(wnd.Cache.DatabaseAccessor, (uint)form.Handle,
					wnd.Cache.LangProject.Name.UserDefaultWritingSystem, wnd.Cache.DefaultUserWs);
				m_activeMainWindow = form;
			}
			if (isNewCache && form != null)
				InitializePartInventories(cache, true);
			return form;
		}

		/// <summary>
		/// Provides a hook for initializing a cache in special ways. For example,
		/// LexTextApp sets up a CreateModifyTimeManager.
		/// </summary>
		/// <param name="cache"></param>
		protected override void InitCache(FdoCache cache)
		{
			base.InitCache(cache);
			// Just create one...it hooks itself to the cache.
			m_cmTimeManager = new CreateModifyTimeManager(cache);

			AddDefaultWordformingOverridesIfNeeded(cache);
		}

		/// <summary>
		/// Adds the default word-forming character overrides to the list of valid
		/// characters for each vernacular writing system that is using the old
		/// valid characters representation.
		/// </summary>
		/// <param name="cache">The cache.</param>
		void AddDefaultWordformingOverridesIfNeeded(FdoCache cache)
		{
			ILgWritingSystemFactory lgwsf = cache.LanguageWritingSystemFactoryAccessor;
			foreach (ILgWritingSystem wsObj in cache.LangProject.VernWssRC)
			{
				IWritingSystem ws = lgwsf.get_EngineOrNull(wsObj.Hvo);
				string validCharsSrc = ws.ValidChars;
				if (!ValidCharacters.IsNewValidCharsString(validCharsSrc))
				{
					LanguageDefinition langDef = new LanguageDefinition(ws);
					ValidCharacters valChars = ValidCharacters.Load(langDef);
					valChars.AddDefaultWordformingCharOverrides();

					ws.ValidChars = langDef.ValidChars = valChars.XmlString;
					using (new SuppressSubTasks(cache))
					{
						ws.SaveIfDirty(cache.DatabaseAccessor);
					}
					langDef.Serialize();
				}
			}
		}

		/// <summary>
		/// Provides an application-wide default for allowed style contexts for windows that
		/// don't have an FwEditingHelper (i.e., all but TE windows at present).
		/// For Flex, we currently want to allow general styles only. This is mainly to rule out
		/// ContextValues.InternalConfigureView ones, which are only used when configuring
		/// a view, not for applying styles directly to individual docs.
		/// </summary>
		public override List<ContextValues> DefaultStyleContexts
		{
			get
			{
				CheckDisposed();
				return new List<ContextValues>(new ContextValues[] { ContextValues.General });
			}
		}

		#region IxCoreColleague Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization. Never called because we don't use the xWindow class.
		/// </summary>
		/// <param name="mediator">Message mediator</param>
		/// <param name="configurationParameters">Not used</param>
		/// ------------------------------------------------------------------------------------
		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			CheckDisposed();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the message targets in the right order (i.e. main window that has focus first)
		/// </summary>
		/// <returns>List of main windows (which are possible message targets)</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[] { this };
		}

		#endregion
	}
}
