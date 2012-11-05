//---------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002-2010, SIL International. All Rights Reserved.
// <copyright from='2002' to='2010' company='SIL International'>
//    Copyright (c) 2010, SIL International. All Rights Reserved.
// </copyright>
#endregion
//
// File: MainWindowDelegate.cs
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.Utils;

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

		/// <summary>
		/// Specifically the stylesheet that should currently be used to apply or edit styles.
		/// Usually the same as Stylesheet, but sometimes (e.g., Baseline tab in IText) depends
		/// on the content of the active view.
		/// </summary>
		FwStyleSheet ActiveStyleSheet { get; }

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
		/// <param name="setPropsToFactorySettings">Delegate to set style info properties back
		/// to the default facotry settings</param>
		/// ------------------------------------------------------------------------------------
		bool ShowStylesDialog(string paraStyleName, string charStyleName,
			Action<StyleInfo> setPropsToFactorySettings);

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
		/// Get the stylesheet used in the active view, or the default Stylesheet if none.
		/// </summary>
		FwStyleSheet ActiveStyleSheet { get; }
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
		/// Gets or sets a value indicating whether or not to show the TE style type combo box
		/// in the styles dialog where the user can select the type of styles to show
		/// (all, basic, or custom styles).  False indicates a FLEx style type combo box
		/// (all, basic, dictionary, or custom styles).
		/// </summary>
		bool ShowTEStylesComboInStylesDialog { get; }
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
	public sealed class MainWindowDelegate : IMainWindowDelegatedFunctions
	{
		#region Data members
		private IMainWindowDelegateCallbacks m_callbacks; // FwXWindow or FwMainWnd
		private FwApp m_app;
		private ContextValues m_prevParaStyleContext = ContextValues.General;
		/// <summary>
		/// The window's private undo stack.
		/// </summary>
		private IActionHandler m_undoStack;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwApp App
		{
			set { m_app = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private FdoCache Cache
		{
			get { return m_callbacks.Cache; }
		}

		/// <summary>
		/// Activate the window's Undo stack.
		/// </summary>
		public void OnActivated()
		{
			var manager = Cache.ServiceLocator.GetInstance<IUndoStackManager>();
			if (m_undoStack == null)
			{
				m_undoStack = manager.CreateUndoStack();
			}
			manager.SetCurrentStack(m_undoStack);
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
				return m_callbacks.StyleSheet;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The style sheet of the active view, if any.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwStyleSheet ActiveStyleSheet
		{
			get
			{
				return m_callbacks.ActiveStyleSheet;
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string that will go in the caption of the main window, or <c>null</c> if
		/// cache or m_app are null
		/// </summary>
		/// <param name="cache">The FDO cache</param>
		/// ------------------------------------------------------------------------------------
		public string GetMainWindowCaption(FdoCache cache)
		{
			if (cache != null && m_app != null)
			{
				return string.Format(m_app.GetResourceString("kstidAppCaptionWithProjectName"),
					GetProjectName(cache), m_app.ApplicationName);
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
			return cache.ProjectId.UiName;
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
				return m_callbacks.StyleSheetOwningFlid;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The "Open Existing Project" menu option has been selected. Display the Choose
		/// Language Project dialog.
		/// </summary>
		/// <param name="owner">The owner.</param>
		/// ------------------------------------------------------------------------------------
		public void FileOpen(Form owner)
		{
			m_app.FwManager.ChooseLangProject(m_app, owner);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The "New FieldWorks Project" menu option has been selected. Display the New
		/// FieldWorks Project dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void FileNew(Form owner)
		{
			m_app.FwManager.CreateNewProject(m_app, owner);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close all windows and shut down the application
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void FileExit()
		{
			m_app.ExitAppplication();
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

				// if possible, use the same logic as displaying the toolbar writing system and
				// style choosers.
				if (selHelper.RootSite is RootSite)
				{
					RootSite rs = selHelper.RootSite as RootSite;
					return rs.CanApplyStyle;
				}

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
				ParaStyleListHelper.AddStyles(ActiveStyleSheet);
			}

			if (CharStyleListHelper != null)
			{
				CharStyleListHelper.ShowOnlyStylesOfType = StyleType.kstCharacter;
				CharStyleListHelper.AddStyles(ActiveStyleSheet);
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
			var style = (paraStyleName == string.Empty) ? null :
				ParaStyleListHelper.StyleFromName(paraStyleName);

			RefreshParaStyleComboBoxList(style, rootsite);
			if (ParaStyleListHelper.SelectedStyleName != paraStyleName)
				ParaStyleListHelper.SelectedStyleName = paraStyleName;

			ContextValues currentContext = (style != null) ? style.Context :
				(fwEditingHelper != null) ? fwEditingHelper.InternalContext : ContextValues.General;

			if (CharStyleListHelper != null)
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
					charStyleName = ResourceHelper.DefaultParaCharsStyleName;
				if (charStyleName == null)
					charStyleName = string.Empty;
				if (CharStyleListHelper.SelectedStyleName != charStyleName)
					CharStyleListHelper.SelectedStyleName = charStyleName;
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
			IWritingSystem ws = Cache.ServiceLocator.WritingSystemManager.Get(hvoWs);
			box.SelectedIndex = box.FindString(ws.DisplayLabel);
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
					ParaStyleListHelper.IncludeStylesWithContext.Add(style.Context);
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
		/// <param name="setPropsToFactorySettings">Delegate to set style info properties back
		/// to the default facotry settings</param>
		/// <returns>
		/// true if a refresh is needed to reload the cache
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool ShowStylesDialog(string paraStyleName, string charStyleName,
			Action<StyleInfo> setPropsToFactorySettings)
		{
			StVc vc = null;
			IVwRootSite activeViewSite = null;
			if (EditingHelper == null)
				return true;
			if (ActiveView != null)
			{
				vc = ActiveView.EditingHelper.ViewConstructor as StVc;
				activeViewSite = ActiveView.CastAsIVwRootSite();
			}
			if (paraStyleName == null && charStyleName == null && EditingHelper.CurrentSelection != null
				&& EditingHelper.CurrentSelection.Selection != null)
			{
				// If the caller didn't know the default style, try to figure it out from
				// the selection.
				GetStyleNames(ActiveView as SimpleRootSite, EditingHelper.CurrentSelection.Selection,
					ref paraStyleName, ref charStyleName);
			}
			using (FwStylesDlg stylesDlg = new FwStylesDlg(activeViewSite,
				Cache, ActiveStyleSheet, (vc == null) ? false : vc.RightToLeft,
				Cache.ServiceLocator.WritingSystems.AllWritingSystems.Any(ws => ws.RightToLeftScript),
				ActiveStyleSheet.GetDefaultBasedOnStyleName(),
				MaxStyleLevelToShow, m_app.MeasurementSystem, paraStyleName, charStyleName,
				HvoAppRootObject, m_app, m_app))
			{
				stylesDlg.SetPropsToFactorySettings = setPropsToFactorySettings;
				stylesDlg.StylesRenamedOrDeleted += m_callbacks.OnStylesRenamedOrDeleted;
				stylesDlg.AllowSelectStyleTypes = true;
				stylesDlg.ShowTEStyleTypes = m_callbacks.ShowTEStylesComboInStylesDialog;
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

				IVwSelection sel = EditingHelper.CurrentSelection.Selection;
				if (paraStyleName == null && charStyleName == null)
				{
					// If the caller didn't know the default style, try to figure it out from
					// the selection.
					GetStyleNames(rootsite, sel, ref paraStyleName, ref charStyleName);
				}
				int hvoRoot, frag;
				IVwViewConstructor vc;
				IVwStylesheet ss;
				ActiveView.CastAsIVwRootSite().RootBox.GetRootObject(out hvoRoot, out vc, out frag, out ss);
				using (FwApplyStyleDlg applyStyleDlg = new FwApplyStyleDlg(ActiveView.CastAsIVwRootSite(),
					Cache, ActiveStyleSheet.RootObjectHvo, StyleSheetOwningFlid,
					ActiveStyleSheet.GetDefaultBasedOnStyleName(), maxStyleLevel,
					paraStyleName, charStyleName, hvoRoot, m_app, m_app))
				{
					if (FwEditingHelper != null)
					{
						if (FwEditingHelper.ApplicableStyleContexts != null)
							applyStyleDlg.ApplicableStyleContexts = FwEditingHelper.ApplicableStyleContexts;
					}
					else if (m_app != null)
					{
						// Window doesn't have an editing helper, go with whole-app default
						if (m_app.DefaultStyleContexts != null)
							applyStyleDlg.ApplicableStyleContexts = m_app.DefaultStyleContexts;
					}
					applyStyleDlg.AllowSelectStyleTypes = m_callbacks.ShowTEStylesComboInStylesDialog;
					applyStyleDlg.CanApplyCharacterStyle = sel.CanFormatChar;
					applyStyleDlg.CanApplyParagraphStyle = sel.CanFormatPara;

					if (applyStyleDlg.ShowDialog(m_callbacks) == DialogResult.OK)
					{
						string sUndo, sRedo;
						ResourceHelper.MakeUndoRedoLabels("kstidUndoApplyStyle", out sUndo, out sRedo);
						using (UndoTaskHelper helper = new UndoTaskHelper(Cache.ActionHandlerAccessor,
							ActiveView.CastAsIVwRootSite(), sUndo, sRedo))
						{
							EditingHelper.ApplyStyle(applyStyleDlg.StyleChosen);
							helper.RollBack = false;
						}
					}
				}
			}
			finally
			{
				if (rootsite != null)
					rootsite.ShowRangeSelAfterLostFocus = false;
			}
		}

		private void GetStyleNames(SimpleRootSite rootsite, IVwSelection sel, ref string paraStyleName,
			ref string charStyleName)
		{
			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			int cttp;
			SelectionHelper.GetSelectionProps(sel, out vttp, out vvps, out cttp);
			bool fSingleStyle = true;
			string sStyle = null;
			for (int ittp = 0; ittp < cttp; ++ittp)
			{
				string style = vttp[ittp].Style();
				if (ittp == 0)
					sStyle = style;
				else if (sStyle != style)
					fSingleStyle = false;
			}
			if (fSingleStyle && !String.IsNullOrEmpty(sStyle))
			{
				if (ActiveStyleSheet.GetType(sStyle) == (int)StyleType.kstCharacter)
				{
					if (sel.CanFormatChar)
						charStyleName = sStyle;
				}
				else
				{
					if (sel.CanFormatPara)
						paraStyleName = sStyle;
				}
			}
			if (paraStyleName == null)
			{
				// Look at the paragraph (if there is one) to get the paragraph style.
				var helper = SelectionHelper.GetSelectionInfo(sel, rootsite);
				var info = helper.GetLevelInfo(SelectionHelper.SelLimitType.End);
				if (info.Length > 0)
				{
					var hvo = info[0].hvo;
					if (hvo != 0)
					{
						var cmObjectRepository = m_callbacks.Cache.ServiceLocator.GetInstance<ICmObjectRepository>();
						if (cmObjectRepository.IsValidObjectId(hvo)) // perhaps some sort of dummy; we can't get paragraph style.
						{
							var cmo = cmObjectRepository.GetObject(hvo);
							if (cmo is IStPara)
								paraStyleName = (cmo as IStPara).StyleName;
						}
					}
				}
			}
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show About box
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShowHelpAbout()
		{
			using (FwHelpAbout helpAboutWnd = new FwHelpAbout())
			{
				helpAboutWnd.ProductExecutableAssembly = Assembly.LoadFile(m_app.ProductExecutableFile);
				helpAboutWnd.ShowDialog();
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Create Shortcut on Desktop menu/toolbar item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool OnCreateShortcut(object args)
		{
			return CreateShortcut(Environment.GetFolderPath(
				Environment.SpecialFolder.DesktopDirectory));
		}

		/// <summary>
		/// Create shortcut to current project in directory, if exists.
		/// </summary>
		internal bool CreateShortcut(string directory)
		{
			if (!FileUtils.DirectoryExists(directory))
			{
				MessageBoxUtils.Show(String.Format(
					"Error: Cannot create project shortcut because destination directory '{0}' does not exist.",
					directory));
				return true;
			}

			var arguments = "-" + FwAppArgs.kProject + " \"" + Cache.ProjectId.Handle + "\"" +
				" -" + FwAppArgs.kServer + " \"" + Cache.ProjectId.ServerName + "\"";
			var description = ResourceHelper.FormatResourceString(
				"kstidCreateShortcutLinkDescription", Cache.ProjectId.UiName,
				m_app.ApplicationName);

			if (MiscUtils.IsUnix)
				return CreateProjectLauncher(arguments, description, directory);

			return CreateProjectShortcut(arguments, description, directory);
		}

		/// <summary>
		/// Create Windows shortcut in directory (such as Desktop) to open TE or Flex project.
		/// </summary>
		private bool CreateProjectShortcut(string applicationArguments,
			string shortcutDescription, string directory)
		{
			IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShellClass();

			string filename = Cache.ProjectId.UiName;
			filename = Path.ChangeExtension(filename, "lnk");
			string linkPath = Path.Combine(directory, filename);

			IWshRuntimeLibrary.IWshShortcut link =
				(IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(linkPath);
			if (link.FullName != linkPath)
			{
				var msg = string.Format(FrameworkStrings.ksCannotCreateShortcut,
					m_app.ProductExecutableFile + " " + applicationArguments);
				MessageBox.Show(Form.ActiveForm, msg,
					FrameworkStrings.ksCannotCreateShortcutCaption, MessageBoxButtons.OK,
					MessageBoxIcon.Asterisk);
				return true;
			}
			link.TargetPath = m_app.ProductExecutableFile;
			link.Arguments = applicationArguments;
			link.Description = shortcutDescription;
			link.IconLocation = link.TargetPath + ",0";
			link.Save();

			return true;
		}

		/// <summary>
		/// Create Gnome/KDE launcher in directory (such as Desktop) to open TE or Flex project
		/// in Linux. Returns true if successful, otherwise false.
		/// FWNX-458
		/// </summary>
		private bool CreateProjectLauncher(string applicationArguments,
			string launcherDescription, string directory)
		{
			var projectName = Cache.ProjectId.UiName;
			var pathExtension = ".desktop";
			var launcherPath = MakeLauncherPath(directory, projectName, null, pathExtension);

			// Choose a different name if already in use
			int tailNumber = 2;
			while (FileUtils.SimilarFileExists(launcherPath))
			{
				var tail = "-" + tailNumber.ToString();
				launcherPath = MakeLauncherPath(directory, projectName, tail, pathExtension);
				tailNumber++;
			}

			string applicationExecutablePath = null;
			string iconPath = null;
			if (m_app.ApplicationName == FwUtils.FwUtils.ksTeAppName)
			{
				applicationExecutablePath = "fieldworks-te";
				iconPath = "fieldworks-te";
			}
			else if (m_app.ApplicationName == FwUtils.FwUtils.ksFlexAppName)
			{
				applicationExecutablePath = "fieldworks-flex";
				iconPath = "fieldworks-flex";
			}
			if (string.IsNullOrEmpty(applicationExecutablePath))
				return false;

			string content = String.Format(
				"[Desktop Entry]{0}" +
				"Version=1.0{0}" +
				"Terminal=false{0}" +
				"Exec=" + applicationExecutablePath + " " + applicationArguments + "{0}" +
				"Icon=" + iconPath + "{0}" +
				"Type=Application{0}" +
				"Name=" + projectName + "{0}" +
				"Comment=" + launcherDescription + "{0}", Environment.NewLine);

			// Don't write a BOM
			using (var launcher = FileUtils.OpenFileForWrite(launcherPath,
				new UTF8Encoding(false)))
			{
				launcher.Write(content);

				FileUtils.SetExecutable(launcherPath);

				return true;
			}
		}

		private static string MakeLauncherPath(string directory, string projectName,
			string tail, string pathExtension)
		{
			return Path.Combine(directory, projectName + tail + pathExtension);
		}
	}
}