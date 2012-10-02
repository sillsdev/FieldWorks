//---------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002-2008, SIL International. All Rights Reserved.
// <copyright from='2002' to='2008' company='SIL International'>
//    Copyright (c) 2008, SIL International. All Rights Reserved.
// </copyright>
#endregion
//
// File: MainWindowDelegate.cs
// Responsibility:
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.Common.Framework
{
	#region interface IMainWindowDelegatedFunctions
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This interface defines shared functionality for all FieldWorks main windows,
	/// in particular, both those that inherit from FwMainWnd and those that inherit
	/// from FwXWindow. Ideally these windows would share a base class, but there are
	/// difficulties. Principally, the base class of FwXWindow is XWindow, an
	/// inappropriate base class for FwMainWnd which is not fully using XCore.
	/// So the common base class can't be or derive from XWindow. On the other hand,
	/// XWindow is defined in xCore, and introducing a base class below that doesn't
	/// help because we are keeping xCore isolated from FieldWorks-specific functionality.
	/// It is expected that all functions in this interface will be implemented by
	/// trivial delegation to the identical function in MainWindowDelegate.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IMainWindowDelegatedFunctions
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the string that will go in the caption of the main window.
		/// </summary>
		/// <param name="cache">The FDO cache</param>
		/// ------------------------------------------------------------------------------------
		string GetMainWindowCaption(FdoCache cache);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the project name from the specified cache. If the connection is to a remote
		/// server, the string returned will include the server name, formatted in a form
		/// suitable for including in a window caption.
		/// </summary>
		/// <param name="cache">The FDO cache</param>
		/// ------------------------------------------------------------------------------------
		string GetProjectName(FdoCache cache);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the stylesheet appropriate to this main window.
		/// (Sigh...the only shared function so far, and it ISN'T really delegated...)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		FwStyleSheet StyleSheet { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether this window is in a state where it can Apply a style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool CanApplyStyle { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Launch the Styles dialog.
		/// </summary>
		/// <param name="paraStyleName">Name of the initially selected paragraph style.</param>
		/// <param name="charStyleName">Name of the initially selected character style.</param>
		/// <returns>true if a refresh is needed to reload the cache</returns>
		/// ------------------------------------------------------------------------------------
		bool ShowStylesDialog(string paraStyleName, string charStyleName);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the Format Apply Style dialog. Apply the results to the selection of the active
		/// view if the user clicks OK.
		/// </summary>
		/// <param name="paraStyleName">Name of the initially selected paragraph style.</param>
		/// <param name="charStyleName">Name of the initially selected character style.</param>
		/// <param name="maxStyleLevel">The maximum style level that will be shown in this
		/// dialog. (apps that do not use style levels in their stylesheets can pass 0)</param>
		/// ------------------------------------------------------------------------------------
		void ShowApplyStyleDialog(string paraStyleName, string charStyleName, int maxStyleLevel);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a desktop shortcut for the current application and database.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		bool OnCreateShortcut(object args);
	}
	#endregion

	#region interface IMainWindowDelegateCallbacks
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This interface is implemented by both FwXWindow and FwMainWnd. It documents the
	/// functionality that both must implement to enable the implementation of the
	/// functions of MainWindowDelegate. There is some overlap with other interfaces,
	/// especially IFwMainWnd, but defining both helps to distinguish what FwApp needs
	/// from what MainWindowDelegate needs.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IMainWindowDelegateCallbacks : IWin32Window
	{
		/// <summary>
		/// Get the application name
		/// </summary>
		string ApplicationName { get; }
		/// <summary>
		/// Get the FdoCache used in the window
		/// </summary>
		FdoCache Cache { get; }
		/// <summary>
		/// Get the helper for paragraph styles. May be null.
		/// </summary>
		StyleComboListHelper ParaStyleListHelper { get; }
		/// <summary>
		/// Get the helper for character styles. May be null.
		/// </summary>
		StyleComboListHelper CharStyleListHelper { get; }
		/// <summary>
		/// Get the active view, the one that will be affected by choosing a style.
		/// </summary>
		IRootSite ActiveView { get; }
		/// <summary>
		/// Get the combo box (if any) that controls writing system.
		/// </summary>
		ComboBox WritingSystemSelector { get; }
		/// <summary>
		/// Get the editing helper for the active view.
		/// </summary>
		FwEditingHelper FwEditingHelper { get; }
		/// <summary>
		/// Get any editing helper available.
		/// </summary>
		EditingHelper EditingHelper { get; }
		/// <summary>
		/// Get the stylesheet used in the window.
		/// </summary>
		FwStyleSheet StyleSheet { get; }
		/// <summary>
		/// Get the flid of the owning property of the stylesheet (e.g., Scripture.ScriptureTags.kflidStyles)
		/// </summary>
		int StyleSheetOwningFlid { get; }
		/// <summary>
		/// Gets the hvo of the main "root object" associated with the application to which this
		/// main window belongs. For example, for TE, this would be the HVO of Scripture.
		/// </summary>
		int HvoAppRootObject { get; }
		/// <summary>
		/// Gets the maximum style level to show. Default value is max int to show all styles.
		/// </summary>
		int MaxStyleLevelToShow { get; }
		/// <summary>
		/// Called when styles are renamed or deleted.
		/// </summary>
		void OnStylesRenamedOrDeleted();
		/// <summary>
		/// Allows individual implementations to override the default behavior when populating
		/// the paragraph style list.
		/// </summary>
		/// <returns><c>true</c> if implementation overrides the default; <c>false</c> if the
		/// default behavior is desired.</returns>
		bool PopulateParaStyleListOverride();
		/// <summary>
		/// Gets or sets a value indicating whether or not to show the style type combo box
		/// in the styles dialog where the user can select the type of styles to show
		/// (all, basic, or custom styles).
		/// </summary>
		bool ShowSelectStylesComboInStylesDialog { get; }
		/// <summary>
		/// Gets a value indicating whether the user can select a background color on the
		/// paragraph tab in the styles dialog. This is possible in all apps except TE.
		/// </summary>
		/// <value>The default implementation always return <c>true</c>.</value>
		bool CanSelectParagraphBackgroundColor { get; }
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class provides the real implementations for the functions in IFwMainWindow,
	/// shared by FwXWindow and FwMainWnd. See the comment on IFwMainWindow for details.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class MainWindowDelegate : IMainWindowDelegatedFunctions, IFWDisposable
	{
		#region Data members
		private IMainWindowDelegateCallbacks m_callbacks; // FwXWindow or FwMainWnd
		private ContextValues m_prevParaStyleContext = ContextValues.General;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="callbacks">Main window</param>
		/// ------------------------------------------------------------------------------------
		public MainWindowDelegate(IMainWindowDelegateCallbacks callbacks)
		{
			m_callbacks = callbacks;
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~MainWindowDelegate()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
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
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_callbacks = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private FdoCache Cache
		{
			get { return m_callbacks.Cache; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style list helper for the paragraph styles combo box on the formatting toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private StyleComboListHelper ParaStyleListHelper
		{
			get	{ return m_callbacks.ParaStyleListHelper; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style list helper for the character styles combo box on the formatting toolbar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private StyleComboListHelper CharStyleListHelper
		{
			get {return m_callbacks.CharStyleListHelper;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the combo box used for selecting a writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ComboBox WritingSystemSelector
		{
			get { return m_callbacks.WritingSystemSelector; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the setting for the maximum style level to show.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int MaxStyleLevelToShow
		{
			get { return m_callbacks.MaxStyleLevelToShow; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hvo of the main "root object" associated with the application to which this
		/// main window belongs. For example, for TE, this would be the HVO of Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int HvoAppRootObject
		{
			get { return m_callbacks.HvoAppRootObject; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the active client window
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IRootSite ActiveView
		{
			get { return m_callbacks.ActiveView; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the FW editing helper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private FwEditingHelper FwEditingHelper
		{
			get {return m_callbacks.FwEditingHelper; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the editing helper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private EditingHelper EditingHelper
		{
			get {return m_callbacks.EditingHelper; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The style sheet used to display stuff.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwStyleSheet StyleSheet
		{
			get
			{
				CheckDisposed();
				return m_callbacks.StyleSheet;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string that will go in the caption of the main window, or <c>null</c> if
		/// cache or FwApp.App are null
		/// </summary>
		/// <param name="cache">The FDO cache</param>
		/// ------------------------------------------------------------------------------------
		public string GetMainWindowCaption(FdoCache cache)
		{
			CheckDisposed();

			if (cache != null && FwApp.App != null)
			{
				return string.Format(FwApp.GetResourceString("kstidAppCaptionWithProjectName"),
						GetProjectName(cache), m_callbacks.ApplicationName);
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the project name from the specified cache. If the connection is to a remote
		/// server, the string returned will include the server name, formatted in a form
		/// suitable for including in a window caption.
		/// </summary>
		/// <param name="cache">The FDO cache</param>
		/// ------------------------------------------------------------------------------------
		public string GetProjectName(FdoCache cache)
		{
			CheckDisposed();
			Debug.Assert(cache != null);

			return cache.ProjectName();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the flid of the owning property of the stylesheet
		/// (e.g., Scripture.ScriptureTags.kflidStyles)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int StyleSheetOwningFlid
		{
			get
			{
				CheckDisposed();
				return m_callbacks.StyleSheetOwningFlid;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The "New FieldWorks Project" menu option has been selected. Display the New
		/// FieldWorks Project dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void FileNew(Form owner)
		{
			CheckDisposed();
			FwApp.App.NewProjectDialog(owner);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close all windows and shut down the application
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void FileExit()
		{
			CheckDisposed();
			FwApp.App.ExitAppplication();
		}

		#region Styles handling stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether this window is in a state where it can Apply a style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CanApplyStyle
		{
			get
			{
				if (EditingHelper == null)
					return false;
				SelectionHelper selHelper = EditingHelper.CurrentSelection;

				if (selHelper == null)
					return false;

				IVwSelection sel = selHelper.Selection;

				return (sel != null && sel.IsEditable && (sel.CanFormatChar || sel.CanFormatPara));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reload the styles combo box and update it's current value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitStyleComboBox()
		{
			if (Cache == null)
				return;

			if (ParaStyleListHelper != null)
			{
				ParaStyleListHelper.ShowOnlyStylesOfType = StyleType.kstParagraph;
				ParaStyleListHelper.AddStyles(StyleSheet);
			}

			if (CharStyleListHelper != null)
			{
				CharStyleListHelper.ShowOnlyStylesOfType = StyleType.kstCharacter;
				CharStyleListHelper.AddStyles(StyleSheet);
			}

			UpdateStyleComboBoxValue(ActiveView);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the styles combo boxes on the formatting toolbar, with the correct style name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateStyleComboBoxValue(IRootSite rootsite)
		{
			// If we don't have a paraStyleListHelper, we can't update the paragraph or
			// character style combo.
			if (ParaStyleListHelper == null || rootsite == null || rootsite.EditingHelper == null)
				return;
			FwEditingHelper fwEditingHelper = rootsite.EditingHelper as FwEditingHelper;
			if (fwEditingHelper != null && fwEditingHelper.IsPictureReallySelected)
				return;

			string paraStyleName = rootsite.EditingHelper.GetParaStyleNameFromSelection();
			IStStyle style = (paraStyleName == string.Empty) ? null :
				ParaStyleListHelper.StyleFromName(paraStyleName);

			try
			{
				RefreshParaStyleComboBoxList(style, rootsite);

				if (ParaStyleListHelper.SelectedStyleName != paraStyleName)
					ParaStyleListHelper.SelectedStyleName = paraStyleName;
			}
			catch { }


			ContextValues currentContext = (style != null) ? style.Context :
				(fwEditingHelper != null) ? fwEditingHelper.InternalContext : ContextValues.General;

			if (CharStyleListHelper != null)
			{
				try
				{
					string charStyleName = rootsite.EditingHelper.GetCharStyleNameFromSelection();
					if (CharStyleListHelper.ActiveView != rootsite as Control ||
						m_prevParaStyleContext != currentContext ||
						(charStyleName != null && !CharStyleListHelper.Contains(charStyleName)) ||
						(charStyleName == null && m_prevParaStyleContext == ContextValues.Note) ||
						(fwEditingHelper != null && fwEditingHelper.ForceCharStyleComboRefresh))
					{
						RefreshCharStyleComboBoxList(currentContext, rootsite);
					}
					if (charStyleName == string.Empty)
						charStyleName = FdoResources.DefaultParaCharsStyleName;
					if (charStyleName == null)
						charStyleName = string.Empty;
					if (CharStyleListHelper.SelectedStyleName != charStyleName)
						CharStyleListHelper.SelectedStyleName = charStyleName;
				}
				catch {}
			}
			m_prevParaStyleContext = currentContext;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the Writiing System selector combo box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateWritingSystemSelectorForSelection(IVwRootBox rootbox)
		{
			ComboBox box = WritingSystemSelector;
			if (box == null || rootbox == null)
				return;
			int hvoWs = SelectionHelper.GetFirstWsOfSelection(rootbox.Selection);

			if (hvoWs == 0)
			{
				box.SelectedIndex = -1;
				return;
			}
			LgWritingSystem lgws = new LgWritingSystem(Cache, hvoWs);
			if (lgws == null)
			{
				box.SelectedIndex = -1;
				return;
			}
			box.SelectedIndex = box.FindString(lgws.ShortName);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the paragraph style list of the combo box
		/// </summary>
		/// <param name="style">the current paragraph style where the IP is</param>
		/// <param name="view">The currently active view</param>
		/// ------------------------------------------------------------------------------------
		public void RefreshParaStyleComboBoxList(IStStyle style, IRootSite view)
		{
			if (m_callbacks.PopulateParaStyleListOverride())
				return;

			if (ParaStyleListHelper.ActiveView == view && style != null &&
					m_prevParaStyleContext == style.Context)
			{
				return;
			}

			ParaStyleListHelper.IncludeStylesWithContext.Clear();

			FwEditingHelper editingHelper = view.EditingHelper as FwEditingHelper;
			if (editingHelper != null && editingHelper.ApplicableStyleContexts != null)
			{
				ParaStyleListHelper.IncludeStylesWithContext.AddRange(editingHelper.ApplicableStyleContexts);
			}
			else
			{
				if (style != null)
					ParaStyleListHelper.IncludeStylesWithContext.Add((ContextValues)style.Context);
				ParaStyleListHelper.IncludeStylesWithContext.Add(ContextValues.General);
			}
			ParaStyleListHelper.Refresh();
			ParaStyleListHelper.ActiveView = view as Control;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the character style list of the combo box
		/// </summary>
		/// <param name="styleContext">the current Paragraph style context, usually based on
		/// the selection</param>
		/// <param name="view">The currently active view</param>
		/// ------------------------------------------------------------------------------------
		private void RefreshCharStyleComboBoxList(ContextValues styleContext, IRootSite view)
		{
			CharStyleListHelper.IncludeStylesWithContext.Clear();

			FwEditingHelper editingHelper = view.EditingHelper as FwEditingHelper;
			if (editingHelper != null && editingHelper.ApplicableStyleContexts != null)
			{
				CharStyleListHelper.IncludeStylesWithContext.AddRange(editingHelper.ApplicableStyleContexts);
			}
			else
			{
				CharStyleListHelper.IncludeStylesWithContext.Add(styleContext);
				if (!CharStyleListHelper.IncludeStylesWithContext.Contains(ContextValues.General))
					CharStyleListHelper.IncludeStylesWithContext.Add(ContextValues.General);
				if (editingHelper != null &&
					!CharStyleListHelper.IncludeStylesWithContext.Contains(editingHelper.InternalContext))
				{
					CharStyleListHelper.IncludeStylesWithContext.Add(editingHelper.InternalContext);
				}
			}
			CharStyleListHelper.Refresh();
			CharStyleListHelper.ActiveView = view as Control;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the Format Styles dialog.
		/// </summary>
		/// <param name="paraStyleName">Name of the initially selected paragraph style.</param>
		/// <param name="charStyleName">Name of the initially selected character style.</param>
		/// <returns>
		/// true if a refresh is needed to reload the cache
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool ShowStylesDialog(string paraStyleName, string charStyleName)
		{
			StVc vc = null;
			IVwRootSite activeViewSite = null;
			if (ActiveView != null)
			{
				vc = ActiveView.EditingHelper.ViewConstructor as StVc;
				activeViewSite = ActiveView.CastAsIVwRootSite();
			}
			using (FwStylesDlg stylesDlg = new FwStylesDlg(activeViewSite,
				Cache, StyleSheet, (vc == null) ? false : vc.RightToLeft,
				Cache.ProjectIncludesRightToLeftWs, StyleSheet.GetDefaultBasedOnStyleName(),
				MaxStyleLevelToShow, FwApp.MeasurementSystem, paraStyleName, charStyleName,
				HvoAppRootObject, FwApp.App, FwApp.App))
			{
				stylesDlg.StylesRenamedOrDeleted +=
					new FwStylesDlg.StylesRenOrDelDelegate(m_callbacks.OnStylesRenamedOrDeleted);
				stylesDlg.AllowSelectStyleTypes = m_callbacks.ShowSelectStylesComboInStylesDialog;
				stylesDlg.CanSelectParagraphBackgroundColor = m_callbacks.CanSelectParagraphBackgroundColor;
				return (stylesDlg.ShowDialog(m_callbacks) == DialogResult.OK &&
					((stylesDlg.ChangeType & StyleChangeType.DefChanged) > 0 ||
					(stylesDlg.ChangeType & StyleChangeType.Added) > 0));
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Casts the given contexts values to int.
		/// </summary>
		/// <remarks>Delete this method when we get rid of the old Styles dialog</remarks>
		/// <param name="context">The context.</param>
		/// ------------------------------------------------------------------------------------
		public static int ContextValuesToInt(ContextValues context)
		{
			return (int)context;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the Format Apply Style dialog. Apply the results to the selection of the active
		/// view if the user clicks OK.
		/// </summary>
		/// <param name="paraStyleName">Name of the para style.</param>
		/// <param name="charStyleName">Name of the char style.</param>
		/// <param name="maxStyleLevel">The maximum style level that will be shown in this
		/// dialog. (apps that do not use style levels in their stylesheets can pass 0)</param>
		/// ------------------------------------------------------------------------------------
		public void ShowApplyStyleDialog(string paraStyleName, string charStyleName, int maxStyleLevel)
		{
			SimpleRootSite rootsite = (ActiveView as SimpleRootSite);
			try
			{
				if (rootsite != null)
					rootsite.ShowRangeSelAfterLostFocus = true;

				int hvoRoot, frag;
				IVwViewConstructor vc;
				IVwStylesheet ss;
				ActiveView.CastAsIVwRootSite().RootBox.GetRootObject(out hvoRoot, out vc, out frag, out ss);
				using (FwApplyStyleDlg applyStyleDlg = new FwApplyStyleDlg(ActiveView.CastAsIVwRootSite(),
					Cache, StyleSheet.RootObjectHvo, StyleSheetOwningFlid,
					StyleSheet.GetDefaultBasedOnStyleName(), maxStyleLevel,
					paraStyleName, charStyleName, hvoRoot, FwApp.App, FwApp.App))
				{
					if (FwEditingHelper != null)
					{
						if (FwEditingHelper.ApplicableStyleContexts != null)
							applyStyleDlg.ApplicableStyleContexts = FwEditingHelper.ApplicableStyleContexts;
					}
					else if (FwApp.App != null)
					{
						// Window doesn't have an editing helper, go with whole-app default
						if (FwApp.App.DefaultStyleContexts != null)
							applyStyleDlg.ApplicableStyleContexts = FwApp.App.DefaultStyleContexts;
					}
					applyStyleDlg.AllowSelectStyleTypes = m_callbacks.ShowSelectStylesComboInStylesDialog;
					IVwSelection sel = EditingHelper.CurrentSelection.Selection;
					applyStyleDlg.CanApplyCharacterStyle = sel.CanFormatChar;
					applyStyleDlg.CanApplyParagraphStyle = sel.CanFormatPara;

					if (applyStyleDlg.ShowDialog(m_callbacks) == DialogResult.OK)
						EditingHelper.ApplyStyle(applyStyleDlg.StyleChosen);
				}
			}
			finally
			{
				if (rootsite != null)
					rootsite.ShowRangeSelAfterLostFocus = false;
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Create Shortcut on Desktop menu/toolbar item.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnCreateShortcut(object args)
		{
			CheckDisposed();

			IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShellClass();

			string desktopFolder =
				Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			string database = Cache.DatabaseName;
			string project = Cache.LangProject.Name.UserDefaultWritingSystem;
			bool remote = !MiscUtils.IsServerLocal(Cache.ServerName);
			string filename = "";
			if (remote)
			{
				string[] server = Cache.ServerName.Split('\\');
				if (server.Length > 0)
				{
					filename =
						string.Format(FwApp.GetResourceString("kstidCreateShortcutFilenameRemoteDb"), database, server[0]) +
						".lnk";
				}
			}
			else
			{
				filename = database + ".lnk";
			}
			string linkPath = Path.Combine(desktopFolder, filename);

			IWshRuntimeLibrary.IWshShortcut link =
				(IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(linkPath);
			link.TargetPath = Application.ExecutablePath;
			link.Arguments = "-db \"" + database + "\"";
			if (remote)
			{
				link.Arguments += " -c \"" + Cache.ServerName + "\"";
			}
			if (project == database)
			{
				link.Description = string.Format(
					FwApp.GetResourceString("kstidCreateShortcutLinkDescription"),
					project, database, Application.ProductName);
			}
			else
			{
				link.Description = string.Format(
					FwApp.GetResourceString("kstidCreateShortcutLinkDescriptionAlt"),
					project, database, Application.ProductName);
			}
			link.IconLocation = Application.ExecutablePath + ",0";
			link.Save();

			return true;
		}
	}
}
