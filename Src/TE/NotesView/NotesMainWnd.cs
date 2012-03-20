// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2005' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: NotesMainWnd.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// NotesMainWnd is a main window for displaying annotations.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class NotesMainWnd : FwMainWnd
	{
		#region Constants
		/// <summary>Internal name of default tab on the sidebar</summary>
		private const string kViewsSBTabInternalName = "TabViews";
		/// <summary>Internal name of back translation tab on the sidebar</summary>
		private const string kstidFilterSBTabInternalName = "TabFilters";
		/// <summary>Internal name of sort methods tab on the sidebar</summary>
		private const string kstidSortSBTabInternalName = "TabSort";
		/// <summary>Internal name of default button in the sort methods tab</summary>
		private const string kstidReferenceSort = "kstidReferenceSort";
		/// <summary>Internal name of default button in the Filters taskbar tab</summary>
		private const string kstidNoFilter = "kstidNoFilter";
		#endregion

		#region Data members
		private readonly IScripture m_scr;
		private readonly float m_zoomPercent = 1.0f;
		private NotesDataEntryView m_dataEntryView;
		// This gets set in the constructor. But set it to something ridiculous here.
//		private int m_maxSideBarWidth = 5;
//		private IContainer components;
		private FocusMessageHandling m_syncHandler;
		#endregion

		#region Construction, Initialization, etc.
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="NotesMainWnd"/> class.
		/// </summary>
		/// <param name="app">The app</param>
		/// <param name="stylesheet">The stylesheet</param>
		/// <param name="zoomPercent">The zoom percentage</param>
		/// -----------------------------------------------------------------------------------
		public NotesMainWnd(FwApp app, FwStyleSheet stylesheet, float zoomPercent) : base(app, null)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			Name = "NotesMainWnd";
			m_zoomPercent = zoomPercent;
			m_scr = m_app.Cache.LangProject.TranslatedScriptureOA;
			m_StyleSheet = stylesheet;

			SetupSideBarInfoBar();
			AddFilters();

			// Save the max. width for the sidebar and set it to the default width.
//			m_maxSideBarWidth = m_sideBarContainer.Width;
			m_sideBarContainer.Width = kDefaultSideBarWidth;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"></param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (m_beingDisposed || IsDisposed)
				return;

			if (disposing)
			{
				// Need to make sure we set this flag back to false! (TE-4856)
				Debug.Assert(m_cache != null && m_cache.ActionHandlerAccessor != null);
				m_cache.ActionHandlerAccessor.CreateMarkIfNeeded(false);
				if (m_syncHandler != null)
				{
					m_syncHandler.ReferenceChanged -= ScrollToReference;
					m_syncHandler.ScrEditingLocationChanged -= ScrollToScrEditingLocation;
				}

				m_syncHandler = null;
			}

			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize stuff
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
//			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(NotesMainWnd));
			this.SuspendLayout();
			this.AccessibleDescription = resources.GetString("$this.AccessibleDescription");
			this.AccessibleName = resources.GetString("$this.AccessibleName");
			this.AutoScaleMode = AutoScaleMode.Font;
			this.AutoScroll = ((bool)(resources.GetObject("$this.AutoScroll")));
			this.AutoScrollMargin = ((System.Drawing.Size)(resources.GetObject("$this.AutoScrollMargin")));
			this.AutoScrollMinSize = ((System.Drawing.Size)(resources.GetObject("$this.AutoScrollMinSize")));
			this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
			this.ClientSize = ((System.Drawing.Size)(resources.GetObject("$this.ClientSize")));
			this.Enabled = ((bool)(resources.GetObject("$this.Enabled")));
			this.Font = ((System.Drawing.Font)(resources.GetObject("$this.Font")));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.ImeMode = ((System.Windows.Forms.ImeMode)(resources.GetObject("$this.ImeMode")));
			this.Location = ((System.Drawing.Point)(resources.GetObject("$this.Location")));
			this.MaximumSize = ((System.Drawing.Size)(resources.GetObject("$this.MaximumSize")));
			this.MinimumSize = ((System.Drawing.Size)(resources.GetObject("$this.MinimumSize")));
			this.Name = "NotesMainWnd";
			this.RightToLeft = ((System.Windows.Forms.RightToLeft)(resources.GetObject("$this.RightToLeft")));
			this.StartPosition = ((System.Windows.Forms.FormStartPosition)(resources.GetObject("$this.StartPosition")));
			this.Text = resources.GetString("$this.Text");
			this.ResumeLayout(false);

		}
		#endregion

		#region Navigation methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll any note(s) with the given reference into view
		/// </summary>
		/// <param name="sender">The sender (can be null).</param>
		/// <param name="scrRef">The Scripture reference.</param>
		/// <param name="quotedText">The selected text (can be null).</param>
		/// ------------------------------------------------------------------------------------
		public void ScrollToReference(object sender, ScrReference scrRef, ITsString quotedText)
		{
			NotesDataEntryView view = ActiveView as NotesDataEntryView;
			Debug.Assert(view != null);
			if (view != null && sender != view)
				view.ScrollRefIntoView(scrRef, quotedText != null ? quotedText.Text : null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scrolls to SCR editing location.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="editingHelper">The editing helper.</param>
		/// ------------------------------------------------------------------------------------
		public void ScrollToScrEditingLocation(object sender, TeEditingHelper editingHelper)
		{
			NotesDataEntryView view = ActiveView as NotesDataEntryView;
			Debug.Assert(view != null);
			if (view != null && sender != view)
				view.ScrollRelevantAnnotationIntoView(editingHelper);
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add Notes specific toolbars to the ones added by the framework's main window.
		/// </summary>
		/// <returns>An array of toolbar definitions.</returns>
		/// ------------------------------------------------------------------------------------
		protected override string GetAppSpecificMenuToolBarDefinition()
		{
			return DirectoryFinder.FWCodeDirectory +
				@"/Translation Editor/Configuration/NotesTMDefinition.xml";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the sidebar/info. bar adapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupSideBarInfoBar()
		{
			// Null when running tests.
			if (SIBAdapter == null)
				return;

			SIBAdapter.ItemImageListLarge = TeResourceHelper.TeSideBarLargeImages;
			SIBAdapter.ItemImageListSmall = TeResourceHelper.TeSideBarSmallImages;
			SIBAdapter.TabImageList = TeResourceHelper.TeSideBarTabImages;
			SIBAdapter.LargeIconModeImageIndex = 4;
			SIBAdapter.SmallIconModeImageIndex = 5;

			string fmttooltip = TeResourceHelper.GetResourceString("kstidInfoBarButtonTooltipFormat");

			// Add the views tab.
			SBTabProperties tabProps = new SBTabProperties();
			tabProps.Name = kViewsSBTabInternalName;
			tabProps.Text = TeResourceHelper.GetResourceString("kstidViews");
			tabProps.Message = "SideBarTabClicked";
			tabProps.InfoBarButtonToolTipFormat = fmttooltip;
			tabProps.ImageIndex = 0;
			SIBAdapter.AddTab(tabProps);

			// Add the filters tab.
			tabProps = new SBTabProperties();
			tabProps.Name = kstidFilterSBTabInternalName;
			tabProps.Text = TeResourceHelper.GetResourceString("kstidFilters");
			tabProps.InfoBarButtonToolTipFormat = fmttooltip;
			tabProps.ImageIndex = 6;
			SIBAdapter.AddTab(tabProps);

			// Add the "No filter" button to the filters tab
			SBTabItemProperties itemProps = new SBTabItemProperties(this);
			itemProps.Name = kstidNoFilter;
			itemProps.Text = TeResourceHelper.GetResourceString(kstidNoFilter);
			itemProps.ImageIndex = (int)TeResourceHelper.SideBarIndices.NoFilter;
			itemProps.Tag = null;
			itemProps.Message = "ChangeFilter";
			itemProps.ClickAlways = true;
			AddSideBarTabItem(kstidFilterSBTabInternalName, itemProps);

			// Add the sort tab.
			tabProps = new SBTabProperties();
			tabProps.Name = kstidSortSBTabInternalName;
			tabProps.Text = TeResourceHelper.GetResourceString("kstidSort");
			tabProps.InfoBarButtonToolTipFormat = fmttooltip;
			tabProps.ImageIndex = 7;
			SIBAdapter.AddTab(tabProps);

			// Add the "Reference" button to the sort methods tab
			itemProps = new SBTabItemProperties(this);
			// REVIEW: When we implement sort orders defined in the DB, we'll need to consider
			// whether we want to have one hard-coded sort-order like this or not.
			itemProps.Name = kstidReferenceSort;
			itemProps.Text = TeResourceHelper.GetResourceString("kstidReferenceSort");
			itemProps.ImageIndex = (int)TeResourceHelper.SideBarIndices.SortMethod;
			itemProps.Tag = null;
			itemProps.Message = "ChangeSortMethod";
			AddSideBarTabItem(kstidSortSBTabInternalName, itemProps);

			// Set current tab and item - No filter by default
			SIBAdapter.SetCurrentTabItem(kstidFilterSBTabInternalName, kstidNoFilter, true);
			SIBAdapter.SetCurrentTabItem(kstidSortSBTabInternalName, kstidReferenceSort, true);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create the client windows and add corresponding stuff to the sidebar, View menu,
		/// etc.
		/// </summary>
		/// <exception cref="Exception">Invalid user view type in database</exception>
		/// -----------------------------------------------------------------------------------
		public override void InitAndShowClient()
		{
			CheckDisposed();

			// Add the user views to the sidebar, menu, info bar
			int wsUser = m_cache.DefaultUserWs;
			string viewName = TeResourceHelper.GetResourceString("kstidNotes");
			AddNotesView(viewName, wsUser);
			SIBAdapter.SetCurrentTabItem(kViewsSBTabInternalName, viewName, true);

			if (SIBAdapter != null)
				SIBAdapter.LoadSettings(MainWndSettingsKey);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add filters to the side bar
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void AddFilters()
		{
			// Add defined filters to the filters submenu and to the filters sidebar tab
			if (TMAdapter == null)
				return;

			SBTabItemProperties sbItemProps;
			TMItemProperties tmItemProps;
			foreach (ICmFilter filter in m_cache.LangProject.FiltersOC)
			{
				// only use filters that are defined for this application
				if (filter.App != TeResourceHelper.TeAppGuid)
					continue;

				switch (filter.ClassId)
				{
					case ScrScriptureNoteTags.kClassId:
					{
						string strFilterName =
							Properties.Resources.ResourceManager.GetString(filter.FilterName);

						// Add this filter to the Filters sidebar tab
						sbItemProps = new SBTabItemProperties(this);
						sbItemProps.Name = filter.Name;
						sbItemProps.Text = strFilterName;
						sbItemProps.ImageIndex = (int)TeResourceHelper.SideBarIndices.BasicFilter;
						sbItemProps.Tag = filter;
						sbItemProps.Message = "ChangeFilter";
						sbItemProps.ClickAlways = true;
						AddSideBarTabItem(kstidFilterSBTabInternalName, sbItemProps);
						break;
					}
					default:
						// ENHANCE: if other types of filters are needed, add handlers here
						break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This updates the given configure menu item. This configure menu item is on the
		/// sidebar tab's context menu or on one of the view menu item's sub-menus or one of
		/// the context menus that pops-up when the user clicks on an info. bar button.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateSideBarConfigure(object args)
		{
			try
			{
				TMItemProperties itemProps = null;
				SBTabProperties tabProps = args as SBTabProperties;
				if (tabProps == null)
				{
					itemProps = args as TMItemProperties;
				}

				if (itemProps != null)
				{
					itemProps.Visible = false;
					itemProps.Update = true;
				}

				return true;
			}
			catch
			{
#if DEBUG
				throw;
#else
				return false; // just ignore in release builds
#endif
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Select the button on the filter sidebar corresponding to the given filter (will
		/// not generate the events)
		/// </summary>
		/// <param name="filter">The filter whose corresponding button should be selected</param>
		/// ------------------------------------------------------------------------------------
		public void SelectFilterButton(IFilter filter)
		{
			CheckDisposed();

			if (filter == null)
			{
				SIBAdapter.SetCurrentTabItem(kstidFilterSBTabInternalName, kstidNoFilter, false);
				return;
			}

#if !__MonoCS__ // TODO-Linux FWNX-236: // remove !MONO block when issue FWNX-236 resolved.
			foreach (SBTabItemProperties filterBtn in SIBAdapter.Tabs[1].Items)
			{
				if (filter.FilterName == filterBtn.Name)
					SIBAdapter.SetCurrentTabItem(kstidFilterSBTabInternalName, filterBtn.Text, false);
			}
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Notes Data Entry View
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void AddNotesView(string viewName, int wsUser)
		{
			m_dataEntryView = new NotesDataEntryView(Cache, m_app, viewName, this);
			m_dataEntryView.Zoom = m_zoomPercent;
			m_dataEntryView.StyleSheet = m_StyleSheet;
			m_dataEntryView.Dock = DockStyle.Fill;
			m_dataEntryView.FilterChanged += NoteFilterChanged;

			// Add this user view to the sidebar tab.
			SBTabItemProperties itemProps = new SBTabItemProperties(this);
			itemProps.Name = viewName;
			itemProps.Text = viewName;
			itemProps.ImageIndex = (int)TeResourceHelper.SideBarIndices.DataEntry;
			itemProps.Tag = m_dataEntryView;
			itemProps.Message = "SwitchActiveView";
			AddSideBarTabItem(kViewsSBTabInternalName, itemProps);

			ClientControls.Add(m_dataEntryView);
			// Bring the draftView to the top of the z-order, so that
			// (if it is the active view) it fills only the remaining space
			m_dataEntryView.BringToFront();
			m_rgClientViews.Add(m_dataEntryView.GetType().Name, m_dataEntryView);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When finished initializing, show the default view.
		/// </summary>
		/// <returns>True if successful; false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public override bool OnFinishedInit()
		{
			CheckDisposed();

			if (!base.OnFinishedInit())
				return false;

			// Display Notes view
			if (SIBAdapter != null)
			{
				if (m_dataEntryView != null)
					m_dataEntryView.Focus();

#if !__MonoCS__ // TODO-Linux FWNX-236: // remove !MONO block when issue FWNX-236 resolved.
			/*if (SIBAdapter.CurrentTabItemProperties == null)
				{
					SIBAdapter.SetCurrentTabItem(kViewsSBTabInternalName,
						SIBAdapter.Tabs[0].Items[0].Name, true);
				}*/
#endif
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the name of the default sidebar item for the given tab
		/// </summary>
		/// <param name="tabName">Name of the sidebar tab</param>
		/// <returns>The name of the default sidebar item, or null</returns>
		/// ------------------------------------------------------------------------------------
		protected override string GetDefaultItemForTab(string tabName)
		{
			switch (tabName)
			{
				case kViewsSBTabInternalName: return SIBAdapter.Tabs[0].Items[0].Name;
				case kstidFilterSBTabInternalName: return kstidNoFilter;
			}

			return base.GetDefaultItemForTab(tabName);
		}

		#endregion

		#region Other overridden methods and properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the Format Apply Style dialog
		/// </summary>
		/// <param name="paraStyleName">The currently-selected Paragraph style name</param>
		/// <param name="charStyleName">The currently-selected Character style name</param>
		/// ------------------------------------------------------------------------------------
		protected override void ShowApplyStyleDialog(string paraStyleName, string charStyleName)
		{
			m_delegate.ShowApplyStyleDialog(paraStyleName, charStyleName,
				(int)Options.ShowStyleLevelSetting);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not to show the style type combo box
		/// in the styles dialog where the user can select the type of styles to show
		/// (all, basic, or custom styles).  False indicates a FLEx style type combo box
		/// (all, basic, dictionary, or custom styles).
		/// </summary>
		/// <value>The implementation in TE always returns <c>true</c></value>
		/// ------------------------------------------------------------------------------------
		public override bool ShowTEStylesComboInStylesDialog
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the user can select a background color on the
		/// paragraph tab in the styles dialog. This is possible in all apps except TE.
		/// </summary>
		/// <value>The implementation in TE always return <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public override bool CanSelectParagraphBackgroundColor
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For the Notes view, we override the default behavior to make the para style combo
		/// only display the current paragraph style in the list.
		/// </summary>
		/// <returns><c>false</c> by default, but overridden versions may return <c>true</c> if
		/// to prevent the default behavior.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool PopulateParaStyleListOverride()
		{
			string paraStyleName = ActiveEditingHelper.GetParaStyleNameFromSelection();
			ParaStyleListHelper.ExplicitStylesToDisplay = new List<string>(1);
			ParaStyleListHelper.ExplicitStylesToDisplay.Add(paraStyleName);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current style names from the selected text
		/// </summary>
		/// <param name="paraStyleName">Name of the para style.</param>
		/// <param name="charStyleName">Name of the char style.</param>
		/// ------------------------------------------------------------------------------------
		protected override void GetCurrentStyleNames(out string paraStyleName, out string charStyleName)
		{
			base.GetCurrentStyleNames(out paraStyleName, out charStyleName);
			if (paraStyleName == null || paraStyleName == string.Empty)
			{
				paraStyleName = ActiveEditingHelper.GetParaStyleNameFromSelection();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (DesignMode)
				return;

			InitStyleSheet(m_scr.Hvo, ScriptureTags.kflidStyles);
			UpdateWritingSystemsInCombobox();

			if (SIBAdapter != null)
				SIBAdapter.SetupViewMenuForSideBarTabs(TMAdapter, "mnuToolBars");

			// We don't need the training menu for the notes window.
			if (TMAdapter != null)
				TMAdapter.RemoveMenuItem("mnuHelp", "mnuTraining");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called just before a window synchronizes it's views with DB changes (e.g. when an
		/// undo or redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization message</param>
		/// ------------------------------------------------------------------------------------
		public override void PreSynchronize(SyncMsg sync)
		{
			CheckDisposed();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle import sync messages.
		/// </summary>
		/// <param name="sync"></param>
		/// ------------------------------------------------------------------------------------
		public override bool Synchronize(SyncMsg sync)
		{
			CheckDisposed();

			switch (sync)
			{
				case SyncMsg.ksyncStyle:
					ParaStyleListHelper.IgnoreListRefresh = true;
					CharStyleListHelper.IgnoreListRefresh = true;

					ReSynchStyleSheet();
					// REVIEW: can we skip the rest of this, if we know that a ksynchUndoRedo will
					// do it for us later?
					RefreshAllViews();

					ParaStyleListHelper.IgnoreListRefresh = false;
					CharStyleListHelper.IgnoreListRefresh = false;
					InitStyleComboBox();
					return true;
				case SyncMsg.ksyncUndoRedo:
					ParaStyleListHelper.IgnoreListRefresh = true;
					CharStyleListHelper.IgnoreListRefresh = true;

					RefreshAllViews();

					ParaStyleListHelper.IgnoreListRefresh = false;
					CharStyleListHelper.IgnoreListRefresh = false;
					InitStyleComboBox();
					return true;
				case SyncMsg.ksyncWs:
					return true; // Don't care -- do nothing
			}

			// Updating views in all windows. IHelpTopicProvider.App should never be null unless
			// running from a test.
			if (m_app != null)
				return false; // causes a RefreshAllViews, and allows caller to notify its callers.

			RefreshAllViews(); // special case for testing.
			return true;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the flid of the owning property of the Scripture stylesheet
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int StyleSheetOwningFlid
		{
			get
			{
				CheckDisposed();
				return ScriptureTags.kflidStyles;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the setting for style levels to show.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int MaxStyleLevelToShow
		{
			get
			{
				CheckDisposed();
				return (int)Options.ShowStyleLevelSetting;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int HvoAppRootObject
		{
			get { CheckDisposed(); return m_scr.Hvo; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the top-level annotation for the current selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IScrScriptureNote CurrentAnnotation
		{
			get
			{
				if (FwEditingHelper == null || FwEditingHelper.CurrentSelection == null)
					return null;
				SelectionHelper helper = FwEditingHelper.CurrentSelection;
				SelLevInfo annInfo;
				bool fFoundAnnLev = helper.GetLevelInfoForTag(ScrBookAnnotationsTags.kflidNotes,
					out annInfo);

				if (!fFoundAnnLev)
					return null;
				IScrScriptureNote note = m_cache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().GetObject(annInfo.hvo);
				Debug.Assert(note.IsValidObject);
				return note;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the editing helper associatied with the active view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual NotesEditingHelper ActiveEditingHelper
		{
			get
			{
				CheckDisposed();
				return (ActiveView == null ? null :
					ActiveView.EditingHelper.CastAs<NotesEditingHelper>());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not a synchronizing scrolling message
		/// is sent when the selection changes to a different annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool SendSyncScrollingMsgs
		{
			get { return m_dataEntryView == null ? false : m_dataEntryView.SendSyncScrollingMsgs; }
			set
			{
				if (m_dataEntryView != null)
					m_dataEntryView.SendSyncScrollingMsgs = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an object that can broker synch scrolling based on the currently "focused"
		/// Scripture reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FocusMessageHandling SyncHandler
		{
			get { return m_syncHandler; }
			set
			{
				if (m_syncHandler != null)
				{
					m_syncHandler.ReferenceChanged -= ScrollToReference;
					m_syncHandler.ScrEditingLocationChanged -= ScrollToScrEditingLocation;
				}

				m_syncHandler = value;

				if (m_syncHandler != null)
				{
					m_syncHandler.ReferenceChanged += ScrollToReference;
					m_syncHandler.ScrEditingLocationChanged += ScrollToScrEditingLocation;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether we have anything to export (i.e., at least one
		/// annotation that matches the filter).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool HaveSomethingToExport
		{
			get
			{
				TeNotesVc vc = m_dataEntryView.NotesEditingHelper.CurrentNotesVc;
				foreach (IScrBookAnnotations ann in m_scr.BookAnnotationsOS)
				{
					if (ann.NotesOS.Count > 0)
						return true;
				}
				return false;
			}
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the help file for the application
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnHelpApplication(object args)
		{
			if (CharStylesComboBox.Focused)
				 ShowStylesHelp(CharStylesComboBox.SelectedItem as StyleListItem);
			else if (ParaStylesComboBox.Focused)
				ShowStylesHelp(ParaStylesComboBox.SelectedItem as StyleListItem);
			else
				ShowHelp.ShowHelpTopic(m_app, "khtpTeNotesWndHelp");

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show help topic for currently selected style. (This method is also in TeMainWnd.
		/// I tried to move it out but the reference chain would have made it ugly).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ShowStylesHelp(StyleListItem item)
		{
			string helpTopic = null;

			if (item != null)
				helpTopic = TeStylesXmlAccessor.GetHelpTopicForStyle(item.Name);

			// I don't really like doing this, but oh well.
			if (string.IsNullOrEmpty(helpTopic) ||
				helpTopic.ToLower().StartsWith("help_topic_does_not_exist"))
			{
				helpTopic = TeResourceHelper.GetResourceString("kstidHelpTopicAllStyles");
			}

			Help.ShowHelp(new Label(), m_app.HelpFile, helpTopic);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Import an Open XML for Exchanging Scripture Annotations (OXESA) file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool OnFileImportOXESA(object args)
		{
			CheckDisposed();
			using (TeImportExportFileDialog dlg = new TeImportExportFileDialog(m_cache.ProjectId.Name, FileType.OXESA))
			{
				if (dlg.ShowOpenDialog(null, this) == DialogResult.OK)
				{
					string sUndo, sRedo;
					TeResourceHelper.MakeUndoRedoLabels("kstidImportAnnotations", out sUndo, out sRedo);

					using (UndoTaskHelper undoHelper = new UndoTaskHelper(Cache.ActionHandlerAccessor,
					   null, sUndo, sRedo))
					{
						using (WaitCursor waitCursor = new WaitCursor(this))
						{

							Exception e;
							XmlScrAnnotationsList.LoadFromFile(dlg.FileName, m_cache, m_StyleSheet, out e);
							if (e != null)
							{
								// Something went wrong while importing so let the user know.
								MessageBox.Show(string.Format(Properties.Resources.kstidOxesaImportFailedMsg, e.Message),
												Properties.Resources.kstidOxesaImportFailedTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
							}
							undoHelper.RollBack = false;
						}
					}
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether it is valid to export an OXESA file.
		/// </summary>
		/// <param name="args">The toolbar/menu-item properties (we hope)</param>
		/// <returns><c>true</c> if handled; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateFileExportOXESA(object args)
		{
			CheckDisposed();

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = HaveSomethingToExport;
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Export an Open XML for Exchanging Scripture Annotations (OXESA) file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool OnFileExportOXESA(object args)
		{
			CheckDisposed();
			XmlScrAnnotationsList list = new XmlScrAnnotationsList(Cache);
			// need to use the dataAccess from the rootbox to get the filtered list - TE-9277
			var dataAccess = m_dataEntryView.RootBox.DataAccess;
			foreach (IScrBookAnnotations annotations in m_scr.BookAnnotationsOS)
			{
				List<IScrScriptureNote> notesToExport = new List<IScrScriptureNote>();
				// TE-9371: Changed way to get hvo's of filtered notes to improve performance
				int arraySize = dataAccess.get_VecSize(annotations.Hvo, ScrBookAnnotationsTags.kflidNotes);
				int[] itemHvos;
				using (var arrayPtr = MarshalEx.ArrayToNative<int>(arraySize))
				{
					int chvo;
					dataAccess.VecProp(annotations.Hvo, ScrBookAnnotationsTags.kflidNotes, arraySize, out chvo, arrayPtr);
					itemHvos = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
				}
				foreach (int hvo in itemHvos)
				{
					IScrScriptureNote note =
						Cache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().GetObject(hvo);

					if (note.AnnotationTypeRA.Guid == CmAnnotationDefnTags.kguidAnnTranslatorNote ||
						note.AnnotationTypeRA.Guid == CmAnnotationDefnTags.kguidAnnConsultantNote)
					{
						// When we are exporting only notes, we only want to export notes that
						// are translator notes or consultant notes (i.e. no checking error notes).
						notesToExport.Add(note);
					}
				}
				list.Add(notesToExport);
			}

			using (TeImportExportFileDialog dlg = new TeImportExportFileDialog(Cache.ProjectId.Name, FileType.OXESA))
			{
				// TODO: Need to supply a decent default filename. Should it include
				// a date/time to avoid accidental overwriting?
				if (dlg.ShowSaveDialog(null, this) == DialogResult.OK)
					list.SerializeToFile(dlg.FileName);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paste what is in the clipboard as a URL
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnPasteHyperlink(object args)
		{
			CheckDisposed();
			return (ActiveEditingHelper != null && ActiveEditingHelper.PasteUrl(m_StyleSheet));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the Paste Hyperlink command is enabled
		/// </summary>
		/// <param name="args">The toolbar/menu-item properties (we hope)</param>
		/// <returns><c>true</c> if handled; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool OnUpdatePasteHyperlink(object args)
		{
			CheckDisposed();

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = (ActiveEditingHelper != null && ActiveEditingHelper.CanPasteUrl());
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateInsertResponse(object args)
		{
			CheckDisposed();

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = (CurrentAnnotation != null);
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enables/disables the styles combobox
		/// </summary>
		/// <param name="args"></param>
		/// <returns>true if the combo box is defined and the parent form is NotesMainWnd;
		/// false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateStyleComboBox(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;

			if (itemProps == null)
				return false;

			ComboBox cbo = itemProps.Control as ComboBox;

			if (cbo != null)
			{
				bool fEnable = (FwEditingHelper != null &&
					FwEditingHelper.CurrentSelection != null &&
					FwEditingHelper.CurrentSelection.Selection.IsEditable
					//&& cbo == m_charStylesComboBox
					);

				itemProps.Enabled = fEnable;
				cbo.Enabled = itemProps.Enabled;
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete the current annotation.
		/// </summary>
		/// <returns>true if we delete an annotation, otherwise false</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnEditDeleteNote(object args)
		{
			CheckDisposed();

			if (FwEditingHelper == null || FwEditingHelper.CurrentSelection == null)
				return false;

			SelectionHelper helper = FwEditingHelper.CurrentSelection;
			IScrScriptureNote ann = null;
			IScrBookAnnotations sba = null;
			foreach (SelLevInfo info in helper.LevelInfo)
			{
				if (info.tag == ScriptureTags.kflidBookAnnotations)
					sba = Cache.ServiceLocator.GetInstance<IScrBookAnnotationsRepository>().GetObject(info.hvo);
				else if (info.tag == ScrBookAnnotationsTags.kflidNotes)
					ann = Cache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().GetObject(info.hvo);
			}
			if (sba == null || ann == null) // Didn't find a deletable note... weird!
				return false;

			string sUndo, sRedo;
			TeResourceHelper.MakeUndoRedoLabels("kstidDeleteAnnotation", out sUndo, out sRedo);

			int index = m_dataEntryView.DataAccess.GetObjIndex(sba.Hvo, ScrBookAnnotationsTags.kflidNotes, ann.Hvo);

			using (UndoTaskHelper undoTaskHelper = new UndoTaskHelper(Cache.ActionHandlerAccessor,
					   null, sUndo, sRedo))
			{
				m_dataEntryView.ResetPrevNoteHvo();
				sba.NotesOS.Remove(ann);
				undoTaskHelper.RollBack = false;
			}

			int numDisplayedNotes = m_dataEntryView.DataAccess.get_VecSize(sba.Hvo, ScrBookAnnotationsTags.kflidNotes);
			if (numDisplayedNotes > 0)
			{
				if (index >= numDisplayedNotes)
					index = numDisplayedNotes - 1;
				((NotesEditingHelper)EditingHelper).MakeSelectionInNote(m_dataEntryView.m_vc, sba.IndexInOwner, index,
					m_dataEntryView, false);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collapses all annotations.
		/// </summary>
		/// <returns><c>true</c> if we collapse all annotations, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnViewCollapseAllNotes(object args)
		{
			CheckDisposed();
			m_dataEntryView.CollapseAllAnnotations();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a reponse to the current annotation. This really appends a response, it doesn't
		/// insert.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnInsertResponse(object args)
		{
			CheckDisposed();

			IScrScriptureNote ann = CurrentAnnotation;
			if (ann == null)
				return false;

			m_dataEntryView.ExpandAnnotationIfNeeded(ann.Hvo);

			string sUndo, sRedo;
			TeResourceHelper.MakeUndoRedoLabels("kstidCreateResponse", out sUndo, out sRedo);
			using (UndoTaskHelper undoTaskHelper =
				new UndoTaskHelper(m_cache.ActionHandlerAccessor, null, sUndo, sRedo))
			{
				int iPos = ann.ResponsesOS.Count; // position where response will be inserted
				ann.CreateResponse();
				m_dataEntryView.OpenExpansionBox(ann.ResponsesOS[0].Hvo);
				m_dataEntryView.Focus();

				// Set the selection in the inserted response.
				int book = BCVRef.GetBookFromBcv(ann.BeginRef) - 1;
				int index = ann.IndexInOwner; //TODO: We actually want an index in the filtered list
				m_dataEntryView.NotesEditingHelper.MakeSelectionInNote(book, index,
					ann.ResponsesOS.Count - 1, ScrScriptureNoteTags.kflidResponses);
				undoTaskHelper.RollBack = false;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the Writing System Selector combobox enabled or disabled
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateWritingSystem(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = (FwEditingHelper != null &&
					FwEditingHelper.CurrentSelection != null &&
					FwEditingHelper.CurrentSelection.Selection.IsEditable);
				m_writingSystemSelector.Enabled = itemProps.Enabled;
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the filter panel in the status bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void NoteFilterChanged(object sender, ICmFilter filter)
		{
			bool fFiltered = (filter != null);
			ShowFilterStatusBarMessage(fFiltered);
			// Filter can be turned off programmitically when note not matching current filter is
			// inserted.
			if (!fFiltered && (SIBAdapter.CurrentTabItemProperties != null) && SIBAdapter.CurrentTabItemProperties.Name != kstidNoFilter)
				SIBAdapter.SetCurrentTabItem(kstidFilterSBTabInternalName, kstidNoFilter, true);
		}
		#endregion
	}
}
