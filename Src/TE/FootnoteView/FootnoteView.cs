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
// File: FootnoteView.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// Implements the footnote view (formerly SeDraftWnd in file DraftWnd.cpp/h).
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.TE
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Implementation of the footnote view
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class FootnoteView : FwRootSite, ISettings, ITeView, IBtAwareView
	{
		#region Data members
		private System.ComponentModel.IContainer components;
		private FootnoteVc m_FootnoteVc;
		private IScripture m_Scripture;
		private bool m_isBackTranslation;
		private int m_btWs;
		private Persistence m_persistence;
		private FwRootSite m_draftView;
		private FilteredScrBooks m_bookFilter;
		private int m_filterInstance;
		private LocationTrackerImpl m_locationTracker;

		/// <summary>Context menu</summary>
		protected System.Windows.Forms.ContextMenu m_contextMenu;
		#endregion

		#region Constructor, Dispose, InitializeComponent
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the FootnoteView class
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="filterInstance">The special tag for the book filter</param>
		/// <param name="draftView">The corresponding draftview pane</param>
		/// <param name="btWs">The HVO of the writing system to display in this view if it is
		/// a back translation view; otherwise less than or equal to 0</param>
		/// ------------------------------------------------------------------------------------
		public FootnoteView(FdoCache cache, int filterInstance, FwRootSite draftView,
			int btWs) : base(cache)
		{
			InitializeComponent();
			m_filterInstance = filterInstance;
			m_draftView = draftView;
			m_btWs = btWs;
			m_isBackTranslation = (btWs > 0);
			BackColor = EditableColor;
			//AutoScroll = false;
			// Enhance JohnT: probably footnote view needs to support the three ContentTypes, too.
			m_locationTracker = new LocationTrackerImpl(cache, m_filterInstance, ContentType);
			DoSpellCheck = (EditingHelper as TeEditingHelper).ShowSpellingErrors;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		StVc.ContentTypes ContentType
		{
			get
			{
				if (m_isBackTranslation)
					return Options.UseInterlinearBackTranslation ? StVc.ContentTypes.kctSegmentBT : StVc.ContentTypes.kctSimpleBT;
				return StVc.ContentTypes.kctNormal;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
				if (m_FootnoteVc != null)
					m_FootnoteVc.Dispose();
			}
			m_FootnoteVc = null;
			m_Scripture = null;
			m_persistence = null;
			m_draftView = null;
			m_bookFilter = null;
		}

		#endregion

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_persistence = new SIL.FieldWorks.Common.Controls.Persistence(this.components);
			((System.ComponentModel.ISupportInitialize)(this.m_persistence)).BeginInit();
			//
			// m_persistence
			//
			this.m_persistence.EnableSaveWindowSettings = false;
			this.m_persistence.Parent = this;
			this.m_persistence.SaveSettings += new SIL.FieldWorks.Common.Controls.Persistence.Settings(this.OnSaveSettings);
			this.m_persistence.LoadSettings += new SIL.FieldWorks.Common.Controls.Persistence.Settings(this.OnLoadSettings);
			//
			// FootnoteView
			//
			this.Name = "FootnoteView";
			((System.ComponentModel.ISupportInitialize)(this.m_persistence)).EndInit();
		}
		#endregion

		#region ISettings Interface and persistence methods
		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the parent's SettingsKey if parent implements ISettings, otherwise null.
		/// </summary>
		///-------------------------------------------------------------------------------------
		public RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();

				if (Parent is ISettings)
					return ((ISettings)Parent).SettingsKey;
				return null;
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a window creation option.
		/// </summary>
		/// <value>By default, returns false</value>
		///-------------------------------------------------------------------------------------
		[Browsable(false)]
		public bool KeepWindowSizePos
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Save the persisted settings now.
		/// </summary>
		///-------------------------------------------------------------------------------------
		public void SaveSettingsNow()
		{
			CheckDisposed();

			m_persistence.SaveSettingsNow(this);
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="key">Location in the registry</param>
		///-------------------------------------------------------------------------------------
		private void OnLoadSettings(RegistryKey key)
		{
			CheckDisposed();
			Zoom = RegistryHelper.ReadFloatSetting(key, "ZoomFactor" + Name, 1.5f);
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Save passage to the registry
		/// </summary>
		/// <param name="key">Location in the registry</param>
		///-------------------------------------------------------------------------------------
		private void OnSaveSettings(RegistryKey key)
		{
			CheckDisposed();
			RegistryHelper.WriteFloatSetting(key, "ZoomFactor" + Name, Zoom);
		}
		#endregion

		#region Event handling methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Right)
			{
				base.OnMouseUp(e);
				return;
			}

			FwMainWnd mainWnd = TheMainWnd as FwMainWnd;
			if (mainWnd != null && mainWnd.TMAdapter != null)
			{
				EditingHelper.ShowContextMenu(e.Location, mainWnd.TMAdapter, this,
					"cmnuFootnoteView", "cmnuAddToDictFV", "cmnuChangeMultiOccurencesFV", "cmnuAddToDictFV",
					((FootnoteEditingHelper)EditingHelper).ShowSpellingErrors);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Focus got set to the footnote view
		/// </summary>
		/// <param name="e">The event data</param>
		/// -----------------------------------------------------------------------------------
		protected override void OnGotFocus(EventArgs e)
		{
			// If there are no footnotes, then don't allow the footnote view to be focused
			if (RootBox.Height <= 0)
			{
				if (DraftView != null)
					DraftView.Focus();
				return;
			}
			base.OnGotFocus(e);

			if (DesignMode)
				return;

			if (TheMainWnd == null)
				return;

			// Make sure the paragraph styles combo box has the appropriate styles for the footnote pane.
			if (TheMainWnd.ParaStyleListHelper.ActiveView != this)
			{
				TheMainWnd.ParaStyleListHelper.IncludeStylesWithContext.Clear();
				TheMainWnd.ParaStyleListHelper.IncludeStylesWithContext.Add(ContextValues.General);
				TheMainWnd.ParaStyleListHelper.IncludeStylesWithContext.Add(ContextValues.Note);
				TheMainWnd.ParaStyleListHelper.Refresh();
				TheMainWnd.ParaStyleListHelper.ActiveView = this;
			}

			// Make sure the character styles combo box has the appropriate styles for the footnote pane.
			if (TheMainWnd.CharStyleListHelper.ActiveView != this)
			{
				if (m_isBackTranslation)
					TheMainWnd.CharStyleListHelper.IncludeStylesWithContext.Add(ContextValues.BackTranslation);
				TheMainWnd.CharStyleListHelper.Refresh();
				TheMainWnd.CharStyleListHelper.ActiveView = this;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Activates the view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void ActivateView()
		{
			CheckDisposed();

			PerformLayout();
			Show();
			Focus();
		}
		#endregion

		#region Other methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the selected footnote.
		/// </summary>
		/// <param name="tag">The flid of the selected footnote</param>
		/// <param name="hvoSel">The hvo of the selected footnote</param>
		/// <returns>True, if a footnote is found at the current selection</returns>
		/// -----------------------------------------------------------------------------------
		protected bool GetSelectedFootnote(out int tag, out int hvoSel)
		{
			hvoSel = 0;
			tag = 0;
			if (m_rootb == null)
				return false;
			IVwSelection vwsel = m_rootb.Selection;
			if (vwsel == null)
				return false;
			return GetSelectedFootnote(vwsel, out tag, out hvoSel);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the flid and hvo corresponding to the current Scripture element (e.g., section
		/// heading, section contents, or title) selected.
		/// </summary>
		/// <param name="vwsel">The current selection</param>
		/// <param name="tag">The flid of the selected footnote</param>
		/// <param name="hvoSel">The hvo of the selected footnote</param>
		/// <returns>True, if a footnote is found at the current selection</returns>
		/// -----------------------------------------------------------------------------------
		protected bool GetSelectedFootnote(IVwSelection vwsel, out int tag, out int hvoSel)
		{
			hvoSel = 0;
			tag = 0;
			int hvoPrevLevel = 0;
			int tagPrev = 0;
			try
			{
				if (vwsel != null)
				{
					// If we look more than 10 levels then something is wrong.
					for (int ilev = 0; ilev < 10; ilev++)
					{
						int ihvo, cpropPrev;
						IVwPropertyStore qvps;
						hvoPrevLevel = hvoSel;
						tagPrev = tag;
						vwsel.PropInfo(false, ilev, out hvoSel, out tag, out ihvo,
							out cpropPrev, out qvps);
						switch (tag)
						{
							case (int)ScrBook.ScrBookTags.kflidFootnotes:
							{
								hvoSel = hvoPrevLevel;
								tag = tagPrev;
								return true;
							}
							default:
								break;
						}
					}
				}
			}
			catch
			{
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scrolls the requested footnote to the top of the view
		/// </summary>
		/// <param name="footnote">The target footnote</param>
		/// <param name="fPutInsertionPtAtEnd">if set to <c>true</c> and showing the view,
		/// the insertion point will be put at the end of the footnote text instead of at the
		/// beginning, as would be appropriate in the case of a newly inserted footnote that has
		/// Reference Text. This parameter is ignored if footnote is null.</param>
		/// ------------------------------------------------------------------------------------
		public void ScrollToFootnote(StFootnote footnote, bool fPutInsertionPtAtEnd)
		{
			CheckDisposed();

			// find book owning this footnote
			int iBook = m_bookFilter.GetBookIndex(footnote.OwnerHVO);
			ScrBook book = new ScrBook(Cache, footnote.OwnerHVO);

			// find index of this footnote
			int iFootnote = footnote.IndexInOwner;

			// create selection pointing to this footnote
			FootnoteEditingHelper.ScrollToFootnote(iBook, iFootnote, (fPutInsertionPtAtEnd ?
				((StTxtPara)footnote.ParagraphsOS[0]).Contents.Length: 0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches through the selection information (at the anchor) to find the level that
		/// is a footnote and then returns the footnote that is represented in the selection.
		/// If no footnote is found then it returns null.
		/// </summary>
		/// <param name="helper">The SelectionHelper to search through</param>
		/// <returns>The found footnote or null if it could not find one</returns>
		/// ------------------------------------------------------------------------------------
		private ScrFootnote GetFootnoteFromHelperInfo(SelectionHelper helper)
		{
			SelLevInfo[] info = helper.LevelInfo;
			for(int i = 0; i < info.Length; i++)
			{
				if (info[i].tag == (int)ScrBook.ScrBookTags.kflidFootnotes)
					return new ScrFootnote(Cache, info[i].hvo);
			}

			return null;
		}
		#endregion

		#region Overrides of RootSite
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether automatic vertical scrolling to show the selection should
		/// occur. Usually this is only appropriate if the window autoscrolls and has a
		/// vertical scroll bar, but TE's footnote view needs to allow it anyway, because in
		/// synchronized scrolling only one of the sync'd windows has a scroll bar.
		/// </summary>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		protected override bool DoAutoVScroll
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the average paragraph height for the footnote view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int AverageParaHeight
		{
			get
			{
				CheckDisposed();
				return (int)(12 * Zoom);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculate height for footnotes.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="dxAvailWidth"></param>
		/// <returns>The estimated height for the specified hvo in paragraphs</returns>
		/// ------------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			CheckDisposed();

			// This is done in the TeParaCounter class.
			Debug.Fail("Shouldn't come here - should use TeParaCounter class instead");
			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide a TE specific implementation of the EditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override EditingHelper EditingHelper
		{
			get
			{
				CheckDisposed();

				if (m_editingHelper == null)
				{
					Debug.Assert(Cache != null);
					m_editingHelper = new FootnoteEditingHelper(this, Cache, FilterInstance,
						DraftView, m_isBackTranslation);
					((FootnoteEditingHelper)m_editingHelper).InternalContext = ContextValues.Note;
				}
				return m_editingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide a Footnote specific implementation of the EditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FootnoteEditingHelper FootnoteEditingHelper
		{
			get
			{
				CheckDisposed();
				return EditingHelper as FootnoteEditingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a problem deletion. No problem deletions currently handled, beep and
		/// return Abort so that default behavior is not tried.
		/// </summary>
		/// <param name="sel"></param>
		/// <param name="dpt"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override VwDelProbResponse OnProblemDeletion(IVwSelection sel,
			VwDelProbType dpt)
		{
			CheckDisposed();

			MiscUtils.ErrorBeep();
			return VwDelProbResponse.kdprAbort;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="rootb"></param>
		/// <param name="vwselNew"></param>
		/// ------------------------------------------------------------------------------------
		public override void SelectionChanged(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();

			base.SelectionChanged(rootb, vwselNew);

			if (EditingHelper is TeEditingHelper)
				((TeEditingHelper)EditingHelper).SetInformationBarForSelection();

			#region Debug code
#if DEBUG_not_exist
			if (TheMainWnd == null)
				return;
			// This section of code will display selection information in the status bar when the
			// program is compiled in Debug mode. The information shown in the status bar is useful
			// when you want to make selections in tests.
			try
			{
				SelectionHelper helper = EditingHelper.CurrentSelection;

				string text = "Book: " + helper.LevelInfo[2].ihvo +
					"  Footnote: " + helper.LevelInfo[1].ihvo +
					"  Paragraph: " + helper.LevelInfo[0].ihvo +
					"  Anchor: " + helper.IchAnchor + "  End: " + helper.IchEnd +
					"  AssocPrev: " + helper.AssocPrev;

				((FwMainWnd)TheMainWnd).StatusBar.Panels[0].Text = text;
			}
			catch
			{
			}
#endif
			#endregion
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the root box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_fdoCache == null || DesignMode)
				return;

			if (FwEditingHelper.ApplicableStyleContexts != null)
			{
				FwEditingHelper.ApplicableStyleContexts = new List<ContextValues>(2);
				FwEditingHelper.ApplicableStyleContexts.Add(ContextValues.Note);
				FwEditingHelper.ApplicableStyleContexts.Add(ContextValues.General);
			}

			// This is muy importante. If a spurious rootbox got made already and we just replace
			// it with a new one, the old one will fire an assertion in its destructor.
			// REVIEW: Is this okay? Is there any way to prevent early creation of rootboxes?
			if (m_rootb == null)
				m_rootb = VwRootBoxClass.Create();

			m_rootb.SetSite(this);

			// Set up a new view constructor.
			m_FootnoteVc = new FootnoteVc(m_filterInstance, TeStVc.LayoutViewTarget.targetDraft,
				m_isBackTranslation ? m_btWs : m_fdoCache.DefaultVernWs);
			m_FootnoteVc.ContentType = this.ContentType;

			m_FootnoteVc.Cache = m_fdoCache;
			m_FootnoteVc.HeightEstimator = Group as IHeightEstimator;
			m_FootnoteVc.Editable = EditingHelper.Editable;
			m_bookFilter = FilteredScrBooks.GetFilterInstance(m_fdoCache, m_filterInstance);

			m_FootnoteVc.Cache = m_fdoCache;

			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;
			//EditingHelper.RootObjects = new int[]{ScriptureObj.Hvo};
			m_rootb.SetRootObject(ScriptureObj.Hvo, m_FootnoteVc,
				(int)FootnoteFrags.kfrScripture, m_styleSheet);

			base.MakeRoot();
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.

			//TODO:
			//ptmw->RegisterRootBox(qrootb);

			Synchronize(m_rootb);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clear the footnote cache before refreshing the display.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void RefreshDisplay()
		{
			CheckDisposed();

			DoSpellCheck = (EditingHelper as TeEditingHelper).ShowSpellingErrors;
			ScrBook.ClearAllFootnoteCaches(m_fdoCache);
			base.RefreshDisplay();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't allow user to paste wacky stuff in the footnote pane.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override VwInsertDiffParaResponse OnInsertDiffParas(IVwRootBox prootb,
			ITsTextProps ttpDest, int cPara, ITsTextProps[] ttpSrc, ITsString[] tssParas,
			ITsString tssTrailing)
		{
			CheckDisposed();

			return VwInsertDiffParaResponse.kidprFail;
		}
		#endregion

		#region Properties
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Sets the FDO cache
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override FdoCache Cache
		{
			set
			{
				CheckDisposed();

				Debug.Assert(m_fdoCache == null || m_fdoCache == value,
					"Changing the cache after its already been set is bad!");
				base.Cache = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/set the editable state of the view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Editable
		{
			get
			{
				CheckDisposed();
				return EditingHelper.Editable;
			}
			set
			{
				CheckDisposed();

				EditingHelper.Editable = value;
				BackColor = value ? EditableColor : TeResourceHelper.NonEditableColor;
			}
		}

		// The color the window should be if it is editable.
		Color EditableColor
		{
			get
			{
				return ContentType == StVc.ContentTypes.kctSegmentBT ? TeResourceHelper.ReadOnlyTextBackgroundColor : SystemColors.Window;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the default WS of the view constructor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BackTranslationWS
		{
			get
			{
				CheckDisposed();
				return m_isBackTranslation ? m_FootnoteVc.DefaultWs : -1;
			}
			set
			{
				CheckDisposed();

				m_FootnoteVc.DefaultWs = value;
				RefreshDisplay();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO for the scripture object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScripture ScriptureObj
		{
			get
			{
				CheckDisposed();

				if (m_Scripture == null && m_fdoCache != null)
					m_Scripture = m_fdoCache.LangProject.TranslatedScriptureOA;
				return m_Scripture;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets value of filter instance - used to make filters unique per main window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FilterInstance
		{
			get
			{
				CheckDisposed();
				return m_filterInstance;
			}
			set
			{
				CheckDisposed();

				m_filterInstance = value;
				m_bookFilter = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the current book filter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FilteredScrBooks BookFilter
		{
			get
			{
				CheckDisposed();

				if (m_bookFilter == null)
				{
					m_bookFilter = FilteredScrBooks.GetFilterInstance(Cache, FilterInstance);
					// if the filter is still null then make one for testing
					if (TheMainWnd == null && m_bookFilter == null)
						m_bookFilter = new FilteredScrBooks(Cache, FilterInstance);
				}
				return m_bookFilter;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the draft view associated with the footnote view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwRootSite DraftView
		{
			get
			{
				CheckDisposed();
				return m_draftView;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Gets the status of the selection in the footnote view.</summary>
		/// ------------------------------------------------------------------------------------
		private bool ValidFootnoteSelection
		{
			get
			{
				CheckDisposed();
				SelectionHelper helper = SelectionHelper.Create(this);
				return (helper == null ? false :
					helper.GetLevelForTag((int)ScrBook.ScrBookTags.kflidFootnotes) >= 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the location tracker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILocationTracker LocationTracker
		{
			get { return m_locationTracker; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the USFM resource viewer is visible for this view
		/// (always false).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RegistryBoolSetting UsfmResourceViewerVisible
		{
			get { return new RegistryBoolSetting(string.Empty, false); }
		}
		#endregion

		#region Update handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disables the toolbar item
		/// </summary>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		private void DisableTMItem(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = false;
				itemProps.Update = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting a footnote inside of a footnote isn't possible
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInsertFootnote(object args)
		{
			if (!Focused)
				return false;
			DisableTMItem(args);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting a footnote from the footnote view isn't possible
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInsertFootnoteDialog(object args)
		{
			if (!Focused)
				return false;
			DisableTMItem(args);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting a cross reference from the footnote view isn't possible
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInsertCrossRefDialog(object args)
		{
			if (!Focused)
				return false;
			DisableTMItem(args);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting a section inside of a footnote isn't possible
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInsertSection(object args)
		{
			if (!Focused)
				return false;
			DisableTMItem(args);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting an intro section inside of a footnote isn't possible
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateInsertIntroSection(object args)
		{
			CheckDisposed();

			if (!Focused)
				return false;
			DisableTMItem(args);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting verse numbers inside of a footnote isn't possible
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInsertVerseNumbers(object args)
		{
			if (!Focused)
				return false;
			DisableTMItem(args);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting chapters inside of a footnote isn't possible
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInsertChapterNumber(object args)
		{
			if (!Focused)
				return false;
			DisableTMItem(args);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserting a book inside of a footnote isn't possible
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInsertBook(object args)
		{
			if (!Focused)
				return false;
			DisableTMItem(args);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Only allow deleting of a footnote if we aren't in a BT footnote view and we have
		/// a valid selection.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateDeleteFootnote(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;

			if (itemProps == null || !Focused)
				return false;

			itemProps.Enabled = !m_isBackTranslation && ValidFootnoteSelection;
			itemProps.Update = true;
			return true;
		}
		#endregion

		#region Menu handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes a footnote
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if we handle this</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDeleteFootnote(object args)
		{
			if (DataUpdateMonitor.IsUpdateInProgress(DataAccess))
				return true; //discard this event

			if (!ValidFootnoteSelection)
				return true;

			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoDelFootnote", out undo, out redo);
			using (new UndoTaskHelper(this, undo, redo, false))
			using (new DataUpdateMonitor(this, RootBox.DataAccess, this, "DeleteFootnote"))
			{
				SelectionHelper helper = SelectionHelper.Create(this);
				int fnLevel = helper.GetLevelForTag((int)ScrBook.ScrBookTags.kflidFootnotes);

				if (helper.Selection.IsRange)
					DeleteFootnoteRange(helper);
				else
				{
					// There's no range selection, so delete only one footnote
					ScrFootnote footnote = new ScrFootnote(m_fdoCache, helper.LevelInfo[fnLevel].hvo);
					ScrFootnote.DeleteFootnoteAndMarker(footnote);
				}

				if (RootBox.Height <= 0)
					DraftView.Focus();
				else
				{
					int iBook = helper.LevelInfo[fnLevel + 1].ihvo;
					ScrBook book = m_bookFilter.GetBook(iBook);
					int iFootnote = helper.LevelInfo[fnLevel].ihvo;

					// If the last footnote in the book was deleted find a footnote to move to
					if (iFootnote >= book.FootnotesOS.Count)
						FindNearestFootnote(ref iBook, ref iFootnote);

					FootnoteEditingHelper.ScrollToFootnote(iBook, iFootnote, 0);
				}
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a footnote is deleted and there are no more in the book, find the book index
		/// and footnote index of the nearest footnote.
		/// </summary>
		/// <param name="iBook">The book index in the filter.</param>
		/// <param name="iFootnote">The footnote index.</param>
		/// ------------------------------------------------------------------------------------
		protected void FindNearestFootnote(ref int iBook, ref int iFootnote)
		{
			// first, try to move to the first footnote in a following book
			for (int iFindBook = iBook + 1; iFindBook < m_bookFilter.BookCount; iFindBook++)
			{
				ScrBook book = m_bookFilter.GetBook(iFindBook);
				if (book.FootnotesOS.Count > 0)
				{
					iBook = iFindBook;
					iFootnote = 0;
					return;
				}
			}

			// we did not find a footnote in any following books, so look at the previous books
			for (int iFindBook = iBook; iFindBook >= 0; iFindBook--)
			{
				ScrBook book = m_bookFilter.GetBook(iFindBook);
				if (book.FootnotesOS.Count > 0)
				{
					iBook = iFindBook;
					iFootnote = book.FootnotesOS.Count - 1;
					return;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes footnotes when there is a range selection.
		/// </summary>
		/// <param name="helper"></param>
		/// ------------------------------------------------------------------------------------
		private void DeleteFootnoteRange(SelectionHelper helper)
		{
			int nTopLevels = helper.GetNumberOfLevels(SelectionHelper.SelLimitType.Top);
			int nBottomLevels = helper.GetNumberOfLevels(SelectionHelper.SelLimitType.Bottom);

			// Get the index of the book containing the first footnote in the selection.
			// Then get the index of the footnote within that book.
			int iFirstBook =
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[nTopLevels-1].ihvo;
			int iFirstFootnote =
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Top)[nTopLevels-2].ihvo;

			// Get the index of the book containing the last footnote in the selection.
			// Then get the index of the footnote within that book.
			int iLastBook =
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Bottom)[nBottomLevels-1].ihvo;
			int iLastFootnote =
				helper.GetLevelInfo(SelectionHelper.SelLimitType.Bottom)[nBottomLevels-2].ihvo;

			// Loop through the books containing footnotes in the selection.
			for (int iBook = iFirstBook; iBook <= iLastBook; iBook++)
			{
				ScrBook book = BookFilter.GetBook(iBook);

				int iBeg = iFirstFootnote;
				if (iFirstBook != iLastBook && iBook > iFirstBook)
					iBeg = 0;

				int iEnd = iLastFootnote;
				if (iFirstBook != iLastBook && iBook < iLastBook)
					iEnd = book.FootnotesOS.Count - 1;

				// Loop through the footnotes from the selection that are in the
				// current book. Go in reverse order through the collection.
				for (int i = iEnd; i >= iBeg; i--)
				{
					// TODO: check filter for each HVO
					ScrFootnote footnote = new ScrFootnote(m_fdoCache, book.FootnotesOS[i].Hvo);
					ScrFootnote.DeleteFootnoteAndMarker(footnote);
				}
			}
		}
		#endregion
	}
}
