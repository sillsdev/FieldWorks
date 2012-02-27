// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2002' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeApp.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using SILUBS.SharedScrUtils;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// The FieldWorks Translation Editor.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class TeApp : FwApp, IApp
	{
		#region Member variables and constants
		const string WorkspaceFile = "TeLibronixWorkspace.xml";

		/// <summary>Provides an array of the Features available in TE</summary>
		private static Feature[] s_AppFeatures;
		private NotesMainWnd m_notesWindow;
		FwAppArgs m_appArgs;

		// TODO: test for unique GUID for each application
		/// <summary>Unique identification for each instance of a TE application</summary>
		private readonly Guid m_syncGuid = Guid.NewGuid();
		#endregion

		#region Construction and Initializing
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TeApp Constructor
		/// </summary>
		/// <param name="fwManager">The FieldWorks manager for dealing with FieldWorks-level
		/// stuff.</param>
		/// <param name="helpTopicProvider">An application-specific help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public TeApp(IFieldWorksManager fwManager, IHelpTopicProvider helpTopicProvider)
			:this(fwManager, helpTopicProvider, null)
		{
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TeApp Constructor takes command line arguments
		/// </summary>
		/// <param name="fwManager">The FieldWorks manager for dealing with FieldWorks-level
		/// stuff.</param>
		/// <param name="helpTopicProvider">An application-specific help topic provider.</param>
		/// <param name="appArgs"></param>
		/// ------------------------------------------------------------------------------------
		public TeApp(IFieldWorksManager fwManager, IHelpTopicProvider helpTopicProvider, FwAppArgs appArgs)
			: base(fwManager, helpTopicProvider)
		{
			Options.AddErrorReportingInfo();
			m_appArgs = appArgs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use this for slow operations that should happen during the splash screen instead of
		/// during app construction
		/// </summary>
		/// <param name="progressDlg">The progress dialog to use.</param>
		/// ------------------------------------------------------------------------------------
		public override void DoApplicationInitialization(IProgress progressDlg)
		{
			base.DoApplicationInitialization(progressDlg);
			CleanupRegistry();
			CleanupOldFiles();
			ScrReference.InitializeVersification(DirectoryFinder.TeFolder, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// At one time, we created registry keys HKEY_CURRENT_USER\HKEY_CURRENT_USER...
		/// inadvertently. Now, we want to clean them up if they still exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void CleanupRegistry()
		{
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey("HKEY_CURRENT_USER", true))
			{
				if (key != null)
				{
					DeleteRegistryKey(key);
					Registry.CurrentUser.DeleteSubKey("HKEY_CURRENT_USER");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recursively delete a registry key
		/// </summary>
		/// <param name="key"></param>
		/// ------------------------------------------------------------------------------------
		private static void DeleteRegistryKey(RegistryKey key)
		{
			// find all the subkeys and delete them recursively.
			foreach (string subKeyName in key.GetSubKeyNames())
			{
				DeleteRegistryKey(key.OpenSubKey(subKeyName, true));
				key.DeleteSubKey(subKeyName);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get rid of old obsolete files from previous versions
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CleanupOldFiles()
		{
			try
			{
				string path = DirectoryFinder.UserAppDataFolder(ApplicationName);

				if (Directory.Exists(path))
				{
					List<string> oldFilesList = new List<string>(Directory.GetFiles(path, "TE.TBDef.tb*.xml"));
					string oldDiffFile = Path.Combine(path, "TBDef.DiffView.tbDiffView.xml");
					if (File.Exists(oldDiffFile))
						oldFilesList.Add(oldDiffFile);

					foreach (string oldFile in oldFilesList)
					{
						File.SetAttributes(oldFile, FileAttributes.Normal);
						File.Delete(oldFile);
					}
				}
			}
			catch { /* Ignore any failures. */ }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///	App-specific initialization of the cache.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <returns>True if the initialize was successful, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public override bool InitCacheForApp(IProgress progressDlg)
		{
			if (!TeScrInitializer.Initialize(Cache, this, progressDlg))
				return false;

			// Make sure this DB uses the current stylesheet version, note categories & and key terms list
			IActionHandler actionHandler = Cache.ServiceLocator.GetInstance<IActionHandler>();
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(actionHandler,
				() => TeScrInitializer.EnsureProjectComponentsValid(Cache, this, progressDlg));

			Cache.ServiceLocator.GetInstance<IParagraphCounterRepository>().RegisterViewTypeId<TeParaCounter>((int)TeViewGroup.Scripture);
			Cache.ServiceLocator.GetInstance<IParagraphCounterRepository>().RegisterViewTypeId<TeParaCounter>((int)TeViewGroup.Footnote);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays a message box asking the user whether or not he wants to open a sample DB.
		/// </summary>
		/// <returns><c>true</c> if user consented to opening the sample database; <c>false</c>
		/// otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		public override bool ShowFirstTimeMessageDlg()
		{
			using (TrainingAvailable dlg = new TrainingAvailable())
			{
				return (dlg.ShowDialog() == DialogResult.Yes);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to save the libronix setting (and the workspace).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void SaveSettings()
		{
			base.SaveSettings();
#if !__MonoCS__
			if (Options.AutoStartLibronix)
				LibronixWorkspaceManager.SaveWorkspace(WorkspaceLocation());
#else
			if (Options.AutoStartLibronix)
				throw new NotImplementedException();
			// TODO-Linux: Librionix not ported..
#endif
		}

#if !__MonoCS__
		private static string WorkspaceLocation()
		{
			return Path.Combine(DirectoryFinder.ProjectsDirectory, WorkspaceFile);
		}
#endif


		/// <summary>
		/// Overridden to load the libronix setting and implement it.
		/// </summary>
		public override void LoadSettings()
		{
			base.LoadSettings();
#if !__MonoCS__
			if (Options.AutoStartLibronix)
				LibronixWorkspaceManager.RestoreIfNotRunning(WorkspaceLocation());
#else

			if (Options.AutoStartLibronix)
				throw new NotImplementedException();
			// TODO-Linux: Librionix not ported..
#endif
			TeProjectSettings.InitSettings(this);
		}
		#endregion

		#region IDisposable override
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
				if (Cache != null)
				{
					Cache.ServiceLocator.GetInstance<IParagraphCounterRepository>().UnregisterViewTypeId((int)TeViewGroup.Scripture);
					Cache.ServiceLocator.GetInstance<IParagraphCounterRepository>().UnregisterViewTypeId((int)TeViewGroup.Footnote);
				}
				NotesWindow = null;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			s_AppFeatures = null;

			base.Dispose(disposing);

			// NB: Do this after the call to the base method, as any TeMainWnds will want to access them.
			//s_notesWindoes = null; // Don't null the hashtable, since it doesn't get added by an instance
		}
		#endregion IDisposable override

		#region TeApp Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path of the product executable filename
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ProductExecutableFile
		{
			get { return DirectoryFinder.TeExe; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Guid for the application (used for uniquely identifying DB items that "belong" to
		/// this app.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public static Guid AppGuid
		{
			get	{return TeResourceHelper.TeAppGuid;}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The RegistryKey for this application.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		override public RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();
				return base.SettingsKey.CreateSubKey(FwSubKey.TE);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ApplicationName
		{
			get { return FwUtils.ksTeAppName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// To participate in automatic synchronization from the database (calling SyncFromDb
		/// in a useful manner) and application must override this, providing a unique Guid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Guid SyncGuid
		{
			get
			{
				CheckDisposed();
				return m_syncGuid;
			}
		}
		#endregion

		#region Application Window Handling
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the notes window
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public NotesMainWnd NotesWindow
		{
			get { return m_notesWindow; }
			set
			{
				NotesMainWnd oldNotesWindow = m_notesWindow;
				m_notesWindow = value;
				if (oldNotesWindow != null)
				{
					oldNotesWindow.Closing -= HandleNotesWindowClosing;
					oldNotesWindow.Close();
				}
				if (m_notesWindow != null)
					m_notesWindow.Closing += HandleNotesWindowClosing;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes sure a closing notes window gets removed from the table of notes windows.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void HandleNotesWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Debug.Assert(sender != null || sender == m_notesWindow);
			m_notesWindow.Closing -= HandleNotesWindowClosing;
			m_notesWindow = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the main Translation Editor window
		/// </summary>
		/// <param name="progressDlg">The progress dialog to use, if needed (can be null).</param>
		/// <param name="fNewCache">Flag indicating whether one-time, application-specific
		/// initialization should be done for this cache.</param>
		/// <param name="wndCopyFrom">Must be null for creating the original app window.
		/// Otherwise, a reference to the main window whose settings we are copying.</param>
		/// <param name="fOpeningNewProject"><c>true</c> if opening a brand spankin' new
		/// project</param>
		/// <returns>
		/// New instance of TeMainWnd if Scripture data has been successfully loaded;
		/// null, otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override Form NewMainAppWnd(IProgress progressDlg, bool fNewCache, Form wndCopyFrom,
			bool fOpeningNewProject)
		{
			// TE-1913: Prevent user from accessing windows that are open to the same project.
			// Originally this was used for importing.
			foreach (Form wnd in MainWindows)
			{
				if (!wnd.Enabled && wnd is FwMainWnd)
					throw new FwStartupException(Properties.Resources.kstidProjectLocked);
			}
			return NewTeMainWnd(wndCopyFrom);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the main Translation Editor window
		/// </summary>
		/// <param name="wndCopyFrom"> Must be null for creating the original app window.
		/// Otherwise, a reference to the main window whose settings we are copying.</param>
		/// <returns>New instance of TeMainWnd</returns>
		///
		/// <remarks>This is virtual to support subclasses (esp. for testing)</remarks>
		/// -----------------------------------------------------------------------------------
		protected virtual TeMainWnd NewTeMainWnd(Form wndCopyFrom)
		{
			Logger.WriteEvent(string.Format("Creating new TeMainWnd for {0}", Cache.ProjectId.Name));
			return new TeMainWnd(this, wndCopyFrom, m_appArgs.HasLinkInformation ? m_appArgs.CopyLinkArgs() : null);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the specified IFwMainWnd from the list of windows.
		/// </summary>
		/// <param name="fwMainWindow">The IFwMainWnd to remove</param>
		/// ------------------------------------------------------------------------------------
		public override void RemoveWindow(IFwMainWnd fwMainWindow)
		{
			base.RemoveWindow(fwMainWindow);

			Debug.Assert(!IsDisposed,
				"Shuting down the app should have happened asynchronously after we get called");
			if (MainWindows.Count == 1 && MainWindows[0] is NotesMainWnd)
				NotesWindow = null; // Notes window is the only window left
		}
		#endregion

		#region Miscellaneous Methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to show the most appropriate passage for the object indicated in the link.
		/// </summary>
		/// <param name="link"></param>
		/// ------------------------------------------------------------------------------------
		public override void HandleIncomingLink(FwLinkArgs link)
		{
			ICmObject target;
			if (!Cache.ServiceLocator.ObjectRepository.TryGetObject(link.TargetGuid, out target))
				return; // can't even get the target object!

			var targetRef = GetTargetRef(target);
			if (targetRef == null)
				return; // don't know how to go there yet.

			var mainWnd = this.ActiveMainWindow as TeMainWnd;
			if (mainWnd == null)
			{
				throw new Exception("life stinks");
			}
			mainWnd.GotoVerse(targetRef);
		}

		/// <summary>
		/// Get a reference we should jump to in order to show the specified object.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		internal ScrReference GetTargetRef(ICmObject target)
		{
			BCVRef targetRef;
			var para = target as IScrTxtPara;
			if (para == null)
				para = target.OwnerOfClass<IStTxtPara>() as IScrTxtPara;
			if (para != null)
			{
				BCVRef end;
				para.GetRefsAtPosition(0, out targetRef, out end);
				return new ScrReference(targetRef, Cache.LangProject.TranslatedScriptureOA.Versification);
			}
			// Not a paragraph or owned by one. Try for a section.
			var section = target as IScrSection;
			if (section == null)
				section = target.OwnerOfClass<IScrSection>();
			if (section == null)
			{
				// Failing that the first section of some book...
				var book = target as IScrBook;
				if (book == null)
					book = target.OwnerOfClass<IScrBook>();
				if (book != null && book.SectionsOS.Count > 0)
					section = book.SectionsOS[0];
			}
			if (section != null)
				return new ScrReference(section.VerseRefMin, Cache.LangProject.TranslatedScriptureOA.Versification);
			// Enhance JohnT: possibly we can do better for footnotes, notes, other Scripture-related objects?
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This provides a hook for any kind of app that wants to configure the dialog
		/// in some special way. TE wants to disable regular expressions for replace.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ConfigureFindReplacedialog()
		{
			// TODO (TE-5023) Enable regular expressions for the Replace tab.
			m_findReplaceDlg.DisableReplacePatternMatching = true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a string from a resource ID
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>string</returns>
		/// -----------------------------------------------------------------------------------
		string IApp.ResourceString(string stid)
		{
			CheckDisposed();

			return TeResourceHelper.GetResourceString(stid);
		}

		#endregion

		#region Proposed Methods for User Profiles  //SarahD

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the features available for TE
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override Feature[] GetAppFeatures()
		{
			CheckDisposed();

			//SarahD
			//TODO:  load the strings from TeStrings.resx
			s_AppFeatures = new SIL.FieldWorks.FDO.Feature[] {
				 new SIL.FieldWorks.FDO.Feature(1, "Feature1", 1),
				 new SIL.FieldWorks.FDO.Feature(2, "Feature2", 1),
				 new SIL.FieldWorks.FDO.Feature(3, "Feature3", 1),
				 new SIL.FieldWorks.FDO.Feature(4, "Feature4", 3),
				 new SIL.FieldWorks.FDO.Feature(5, "Feature5", 3),
				 new SIL.FieldWorks.FDO.Feature(6, "Feature6", 3),
				 new SIL.FieldWorks.FDO.Feature(7, "Feature7", 5),
				 new SIL.FieldWorks.FDO.Feature(8, "Feature8", 5),
				 new SIL.FieldWorks.FDO.Feature(9, "Feature9", 5)};
			return s_AppFeatures;
		}
		#endregion
	}
}
