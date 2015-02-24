// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeMainWnd.cs
// Responsibility: TE Team
//
// <remarks>
// Implementation of TeMainWnd. This is subclassed from FwMainWnd.
// </remarks>

using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Media;
using System.Windows.Forms;
using Microsoft.Win32;
using Paratext;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.TE.TeEditorialChecks;
using SIL.WritingSystems;
using SILUBS.SharedScrControls;
using SILUBS.SharedScrUtils;
using XCore;
using SIL.CoreImpl;
using SIL.OxesIO;
using SILUBS.PhraseTranslationHelper;
using StyleInfo = SIL.FieldWorks.FwCoreDlgControls.StyleInfo;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Main window for Translation Editor
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeMainWnd : FwMainWnd, IMessageFilter, IPageSetupCallbacks, ITeImportCallbacks,
		IScrRefTracker
	{
		#region DivInfo struct
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Needed for implementation of OnHeaderFooterSetup to temporarily store
		/// and compare vital info about a PubDivision.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public struct DivInfo
		{
			/// <summary>Hvo of a PubDivision's H/F set</summary>
			public int hfSet;
			/// <summary>A PubDivision's DifferentFirstHF value</summary>
			public bool fDifferentFirstHF;
			/// <summary>A PubDivision's DifferentEvenHF value</summary>
			public bool fDifferentEvenHF;

			/// <summary>
			/// Contstructor
			/// </summary>
			/// <param name="div"></param>
			public DivInfo(IPubDivision div)
			{
				this.hfSet = div.HFSetOA.Hvo;
				this.fDifferentFirstHF = div.DifferentFirstHF;
				this.fDifferentEvenHF = div.DifferentEvenHF;
			}

			/// <summary>
			/// Operator ==
			/// </summary>
			/// <param name="l"></param>
			/// <param name="r"></param>
			/// <returns></returns>
			public static bool operator ==(DivInfo l, DivInfo r)
			{
				if ((Object)l == null && (Object)r == null)
					return true;
				if ((Object)l == null || (Object)r == null)
					return false;
				return l.Equals(r);
			}

			/// <summary>
			/// Operator !=
			/// </summary>
			/// <param name="l"></param>
			/// <param name="r"></param>
			/// <returns></returns>
			public static bool operator !=(DivInfo l, DivInfo r)
			{
				if ((Object)l == null && (Object)r == null)
					return false;
				if ((Object)l == null || (Object)r == null)
					return true;
				return !l.Equals(r);
			}

			/// <summary>
			/// Equals method
			/// </summary>
			/// <param name="obj"></param>
			/// <returns></returns>
			public override bool Equals(object obj)
			{
				// This is the behavior defined by Object.Equals().
				if (obj == null)
					return false;

				// Make sure that we can  cast this object to a CmObject.
				if (!(obj is DivInfo))
					return false;

				DivInfo o = (DivInfo)obj;
				return (hfSet == o.hfSet &&
					fDifferentFirstHF == o.fDifferentFirstHF &&
					fDifferentEvenHF == o.fDifferentEvenHF);
			}

			/// <summary>
			/// Required. Just return base implementation.
			/// </summary>
			/// <returns></returns>
			public override int GetHashCode()
			{
				return base.GetHashCode();
			}
		}
		#endregion

		#region RestoreSelInfo class

		/// <summary>Contains information to restore a selection after applying a book filter</summary>
		private class RestoreSelInfo
		{
			/// <summary>the index of the book (in the bookfilter) at the anchor</summary>
			public int iBookAnchor;
			/// <summary>the index of the book (in the bookfilter) at the end</summary>
			public int iBookEnd;
			/// <summary>the index of the section at the anchor (relative to the book)</summary>
			public int iSectionAnchor;
			/// <summary>the index of the section at the end (relative to the book)</summary>
			public int iSectionEnd;

			/// <summary></summary>
			public SelectionHelper SelHelper;

			/// <summary>The location tracker of the rootsite for this selection</summary>
			public ILocationTracker LocationTracker;

			/// <summary><c>true</c> if the selection info is for the currently active view</summary>
			public bool IsActiveView;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="RestoreSelInfo"/> struct.
			/// </summary>
			/// <param name="locTracker">The location tracker.</param>
			/// <param name="selHelper">The selection helper.</param>
			/// <param name="iBookAnchor">The index of the book (in the bookfilter) at the
			/// anchor.</param>
			/// <param name="iSectionAnchor">The index of the section at the anchor (relative to
			/// the book).</param>
			/// <param name="iBookEnd">The index of the book (in the bookfilter) at the
			/// end.</param>
			/// <param name="iSectionEnd">The index of the section at the end (relative to the
			/// book).</param>
			/// <param name="fIsActiveView"><c>true</c> if the selection info is for the
			/// currently active view.</param>
			/// --------------------------------------------------------------------------------
			public RestoreSelInfo(ILocationTracker locTracker, SelectionHelper selHelper,
				int iBookAnchor, int iSectionAnchor, int iBookEnd, int iSectionEnd,
				bool fIsActiveView)
			{
				LocationTracker = locTracker;
				SelHelper = selHelper;
				this.iBookAnchor = iBookAnchor;
				this.iSectionAnchor = iSectionAnchor;
				this.iBookEnd = iBookEnd;
				this.iSectionEnd = iSectionEnd;
				IsActiveView = fIsActiveView;
			}
		}
		#endregion

		#region TeSelectableViewFactory class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Factory that can create ISelectableViews in TE.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class TeSelectableViewFactory : ISelectableViewFactory
		{
			/// <summary></summary>
			protected readonly string m_viewName;
			/// <summary></summary>
			protected readonly TeViewType m_viewType;

			/// <summary></summary>
			/// <returns></returns>
			public delegate ISelectableView CreatorDelegate(string viewName,
				TeViewType viewType, SBTabItemProperties tabItem);

			/// <summary></summary>
			private readonly CreatorDelegate m_creatorMethod;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="TeSelectableViewFactory"/> class.
			/// </summary>
			/// <param name="viewName">Name of the view.</param>
			/// <param name="viewType">Type of the view.</param>
			/// <param name="creatorMethod">The creator method.</param>
			/// --------------------------------------------------------------------------------
			public TeSelectableViewFactory(string viewName, TeViewType viewType,
				CreatorDelegate creatorMethod)
			{
				m_viewName = viewName;
				m_creatorMethod = creatorMethod;
				m_viewType = viewType;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the "short" name of the user view.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public string BaseInfoBarCaption
			{
				get { return m_viewName.Replace(Environment.NewLine, " "); }
			}

			#region ISelectableViewFactory Members
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Creates an ISelectableView
			/// /// </summary>
			/// <param name="tabItem">The tab item.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			public virtual ISelectableView Create(SBTabItemProperties tabItem)
			{
				return m_creatorMethod(m_viewName, m_viewType, tabItem);
			}

			#endregion
		}
		#endregion

		#region TePrintLayoutViewFactory class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Factory for print layout views
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class TePrintLayoutViewFactory : TeSelectableViewFactory
		{
			private readonly string m_pubName;

			/// <summary></summary>
			/// <returns></returns>
			public delegate ISelectableView PrintLayoutCreatorDelegate(string viewName,
				TeViewType viewType, string pubName, SBTabItemProperties tabItem);

			/// <summary></summary>
			private readonly PrintLayoutCreatorDelegate m_creatorMethod;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="TePrintLayoutViewFactory"/> class.
			/// </summary>
			/// <param name="viewName">Name of the view.</param>
			/// <param name="viewType">Type of the view.</param>
			/// <param name="pubName">Name of the publication.</param>
			/// <param name="creatorMethod">The creator method.</param>
			/// --------------------------------------------------------------------------------
			public TePrintLayoutViewFactory(string viewName, TeViewType viewType,
				string pubName, PrintLayoutCreatorDelegate creatorMethod)
				: base(viewName, viewType, null)
			{
				m_pubName = pubName;
				m_creatorMethod = creatorMethod;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// The name of the IPublication used for this view.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public string PublicationName
			{
				get { return m_pubName; }
			}

			#region ISelectableViewFactory Members
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Creates an ISelectableView
			/// </summary>
			/// <param name="tabItem">The tab item.</param>
			/// <returns></returns>
			/// --------------------------------------------------------------------------------
			public override ISelectableView Create(SBTabItemProperties tabItem)
			{
				return m_creatorMethod(m_viewName, m_viewType, m_pubName, tabItem);
			}

			#endregion
		}
		#endregion

		#region Constants
		/// <summary>Internal name of default tab on the sidebar</summary>
		private const string kScrSBTabName = "TabScripture";
		/// <summary>Internal name of back translation tab on the sidebar</summary>
		private const string kBTSBTabName = "TabBackTrans";
		/// <summary>Internal name of checking tab on the sidebar</summary>
		private const string kChkSBTabName = "TabChecking";
		/// <summary>Internal name of publications tab on the sidebar</summary>
		private const string kPubSBTabName = "TabPubs";
		/// <summary>Internal name of default button in the Scripture taskbar tab</summary>
		private const string kScrDraftViewSBItemName = "ScrDraftViewItem";
		/// <summary>Internal name of print layout button on the Scripture taskbar tab</summary>
		private const string kScrPrintLayoutSBItemName = "ScrPrintLayoutItem";
		/// <summary>Internal name of trial publication button on the Scripture taskbar tab</summary>
		private const string kScrTrialPublicationSBItemName = "ScrTrialPublicationSBItem";
		/// <summary>Internal name of correction printout button on the Scripture taskbar tab</summary>
		private const string kScrCorrectionLayoutSBItemName = "ScrCorrectionLayoutItem";
		/// <summary>Internal name of default draft view in the back translation taskbar tab</summary>
		private const string kBTDraftViewSBItemName = "BTDraftViewItem";
		/// <summary>Internal name of the review view in the back translation taskbar tab</summary>
		private const string kBTReviewViewSBItemName = "BTReviewViewItem";
		/// <summary>Internal name of print layout button in the back translation taskbar tab</summary>
		private const string kBTPrintLayoutSBItemName = "BTPrintLayoutItem";
		/// <summary>Internal name of print layout side-by-side button in the back translation
		/// taskbar tab</summary>
		private const string kBTPrintLayoutSbsSBItemName = "BTPrintLayoutSideBySideItem";
		/// <summary>Internal name of editorial checks button in the checking taskbar tab</summary>
		private const string kChkEditChecksSBItemName = "ChkEditChecksItem";
		/// <summary>Internal name of keyterms button in the checking taskbar tab</summary>
		private const string kChkKeyTermsSBItemName = "ChkKeyTermsItem";
		SBTabItemProperties m_keyTermsViewItemProps;
		/// <summary>Internal name of vertical draft view button in the Scripture taskbar tab</summary>
		private const string kScrVerticalViewSBItemName = "ScrVertDraftViewItem";

		// Names of back translation views.
		/// <summary>Name of the BT review view</summary>
		public const string kstidBTDraftView = "BTDraftView";
		/// <summary>Name of the BT review view</summary>
		public const string kstidBTReviewView = "BTReviewDraftView";
		/// <summary>Name of the BT print layout view</summary>
		public const string kstidBTPrintLayoutView = "BTSimplePrintLayoutView";
		/// <summary>Name of the BT parallel print layout view</summary>
		public const string kstidBTParallelPrintLayoutView = "BTParallelPrintLayoutView";

		/// <summary>The current number of user views for TE (includes views shown in NotesMainWnd)
		/// but not experimental views (e.g., vertical draft view)</summary>
		public const int kNumOfTEViews = 12;

		private const string kDraftViewName = "FrontTransDraftView";
		private const string kDraftFootnoteViewName = "FrontTransFootnoteView";
		internal const string kDraftViewWrapperName = "draftViewWrapper";
		private const string kBTReviewWrapperName = "BT review wrapper";
		private const string kBtDraftSplitView = "btDraftSplitView";
		private const string kBackTransView = "BackTransDraftView";
		private const string kBackTransFootnoteView = "BackTransFootnoteView";
		private const string kBtWsRegEntryName = "BtWs";

		#endregion

		#region Enumeration
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The task tabs in the sidebar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public enum TeSidebarTabType
		{
			/// <summary>Scripture tasks</summary>
			Scripture,
			/// <summary>Back translation tasks</summary>
			BackTrans,
			/// <summary>Checking tasks</summary>
			Checking,
			/// <summary>Publication tasks</summary>
			Publications
		}
		#endregion

		#region Member variables
		/// <summary></summary>
		protected IVwSelection m_commonSelection;
		/// <summary></summary>
		protected ILangProject m_lp;
		/// <summary></summary>
		protected IScripture m_scr;
		/// <summary></summary>
		protected IRootSite m_viewThatLostFocus;

		private string m_prevTabItemName;

		/// <summary>Virtual sequence of books</summary>
		protected FilteredScrBooks m_bookFilter;

		/// <summary></summary>
		protected DbScrPassageControl m_gotoRefCtrl;

		/// <summary>The default writing system used for newly created BT views</summary>
		private int m_defaultBackTranslationWs;

		/// <summary>All uncreated views</summary>
		private Dictionary<TeViewType, ISelectableViewFactory> m_uncreatedViews =
			new Dictionary<TeViewType, ISelectableViewFactory>();

		/// <summary>
		/// This object provides methods for initializing and creating context menus that can
		/// be extended by spelling correction items.  See TE-6901.
		/// </summary>
		private ContextMenuHelper m_cmnuHelper = null;

		private FocusMessageHandling m_syncHandler;

		private DockableUsfmBrowser m_usfmBrowser;
		private bool m_fUsfmBrowserEnabled = true;
		static private UNSQuestionsDialog m_transceleratorWindow;

		private RegistryFloatSetting m_draftViewZoomSettingAlternate;
		private RegistryFloatSetting m_footnoteViewZoomSettingAlternate;
		private const string kComprehensionCheckingToolSubKey = "ComprehensionCheckingTool";
		private const string kCCSettings = "Settings";
		FwLinkArgs m_startupLink;
		#endregion

		#region TeMainWnd Constructors, Initializers, Cleanup
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor for TeMainWnd.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public TeMainWnd()
		{
			Init();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeMainWnd"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		protected TeMainWnd(FdoCache cache) : base(cache)
		{
			Init();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the TeMainWnd class
		/// </summary>
		/// <param name="app"></param>
		/// <param name="wndCopyFrom"></param>
		/// <param name="startupLink">Optional link to jump to in OnFinishedInit</param>
		/// -----------------------------------------------------------------------------------
		public TeMainWnd(FwApp app, Form wndCopyFrom, FwLinkArgs startupLink)
			: base(app, wndCopyFrom)
		{
			m_startupLink = startupLink;
			Init();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the TeMainWnd class
		/// </summary>
		/// <param name="app"></param>
		/// <param name="wndCopyFrom"></param>
		/// -----------------------------------------------------------------------------------
		public TeMainWnd(FwApp app, Form wndCopyFrom) : this(app, wndCopyFrom, null)
		{
		}

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
			if (IsDisposed || Disposing)
				return;

			if (disposing)
			{
				if (m_scr != null)
					m_scr.BooksChanged -= BooksChanged;

				// Dispose managed resources here.
				Application.RemoveMessageFilter(this);
				// KeyTermsViewWrapper gets disposed when base class disposes m_rgClientViews
				if (m_gotoRefCtrl != null && m_gotoRefCtrl.Parent == null)
					m_gotoRefCtrl.Dispose();

				if (m_syncHandler != null)
				{
					m_syncHandler.ReferenceChanged -= ScrollToReference;
					m_syncHandler.AnnotationChanged -= ScrollToCitedText;
					m_syncHandler.Dispose();
				}

				if (m_bookFilter != null)
					m_bookFilter.FilterChanged -= BookFilterChanged;

				if (m_draftViewZoomSettingAlternate != null)
					m_draftViewZoomSettingAlternate.Dispose();

				if (m_footnoteViewZoomSettingAlternate != null)
					m_footnoteViewZoomSettingAlternate.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_gotoRefCtrl = null;
			m_syncHandler = null;
			m_viewThatLostFocus = null;
			m_bookFilter = null;
			m_lp = null;
			m_scr = null;
			m_draftViewZoomSettingAlternate = null;
			m_footnoteViewZoomSettingAlternate = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do required initializations
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void Init()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			if (m_cache != null)
			{
				m_lp = m_cache.LangProject;
				m_scr = m_lp.TranslatedScriptureOA;
				m_scr.BooksChanged += BooksChanged;

				m_bookFilter = Cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(Handle.ToInt32());
				m_bookFilter.FilterChanged += BookFilterChanged;
				m_bookFilter.SetSavedFilterFromString(TeProjectSettings.BookFilterBooks);

				ILgWritingSystemFactory lgwsf = Cache.LanguageWritingSystemFactoryAccessor;
				m_defaultBackTranslationWs = -1;
				using (RegistryStringSetting regDefBtWs = GetBtWsRegistrySetting(String.Empty))
				{
					if (!String.IsNullOrEmpty(regDefBtWs.Value))
						m_defaultBackTranslationWs = lgwsf.GetWsFromStr(regDefBtWs.Value);
					if (m_defaultBackTranslationWs <= 0)
						m_defaultBackTranslationWs = Cache.DefaultAnalWs;
				}
			}
			Application.AddMessageFilter(this);

			if (TMAdapter != null)
				InitializeInsertBookMenus(); // must do after menus are created in InitializeComponent()

			if (DesignMode)
				return;
			SetupSideBarInfoBar();

			Debug.Assert(m_scr != null);
			// Initialize the scripture passage control object.
			GotoReferenceControl.Initialize(ScrReference.StartOfBible(m_scr.Versification));

			UpdateCaptionBar();

			MaxStyleLevel = ToolsOptionsDialog.MaxStyleLevel;

			m_syncHandler = new FocusMessageHandling(this);
			m_draftViewZoomSettingAlternate = new RegistryFloatSetting(MainWndSettingsKey,
				"ZoomFactor" + kDraftViewName, 1.5f);
			m_footnoteViewZoomSettingAlternate = new RegistryFloatSetting(MainWndSettingsKey,
				"ZoomFactor" + kDraftFootnoteViewName, 1.5f);
		}

		/// <summary>
		/// Message handling priority
		/// </summary>
		public override int Priority
		{
			get { return (int)ColleaguePriority.High; }
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

			string cfgMsg = "SideBarConfigure";
			string cfgText = TeResourceHelper.GetResourceString("kstidSideBarConfigureItem");
			string fmttooltip = TeResourceHelper.GetResourceString("kstidInfoBarButtonTooltipFormat");

			// Add the scripture tab.
			SBTabProperties tabProps = new SBTabProperties();
			tabProps.Name = kScrSBTabName;
			tabProps.Text = TeResourceHelper.GetResourceString("kstidScriptureTask");
			tabProps.Message = "SideBarTabClicked";
			tabProps.ConfigureMessage = cfgMsg;
			tabProps.ConfigureMenuText = cfgText;
			tabProps.InfoBarButtonToolTipFormat = fmttooltip;
			tabProps.ImageIndex = 0;
			SIBAdapter.AddTab(tabProps);

			// Add the back translation tab.
			tabProps = new SBTabProperties();
			tabProps.Name = kBTSBTabName;
			tabProps.Text = TeResourceHelper.GetResourceString("kstidBackTransTask");
			tabProps.Message = "SideBarTabClicked";
			tabProps.ConfigureMessage = cfgMsg;
			tabProps.ConfigureMenuText = cfgText;
			tabProps.InfoBarButtonToolTipFormat = fmttooltip;
			tabProps.ImageIndex = 1;
			SIBAdapter.AddTab(tabProps);

			// Add the checking tab.
			tabProps = new SBTabProperties();
			tabProps.Name = kChkSBTabName;
			tabProps.Text = TeResourceHelper.GetResourceString("kstidCheckingTask");
			tabProps.Message = "SideBarTabClicked";
			tabProps.ConfigureMessage = cfgMsg;
			tabProps.ConfigureMenuText = cfgText;
			tabProps.InfoBarButtonToolTipFormat = fmttooltip;
			tabProps.ImageIndex = 2;
			SIBAdapter.AddTab(tabProps);

			// Add the publications tab.
			tabProps = new SBTabProperties();
			tabProps.Name = kPubSBTabName;
			tabProps.Text = TeResourceHelper.GetResourceString("kstidPublicationsTask");
			tabProps.Message = "SideBarTabClicked";
			tabProps.ConfigureMessage = cfgMsg;
			tabProps.ConfigureMenuText = cfgText;
			tabProps.InfoBarButtonToolTipFormat = fmttooltip;
			tabProps.Enabled = false;
			tabProps.ImageIndex = 3;
			SIBAdapter.AddTab(tabProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add TE specific toolbars to the ones added by the framework's main window.
		/// </summary>
		/// <returns>An array of toolbar definitions.</returns>
		/// ------------------------------------------------------------------------------------
		protected override string GetAppSpecificMenuToolBarDefinition()
		{
			return FwDirectoryFinder.CodeDirectory +
				"/Translation Editor/Configuration/TeTMDefinition.xml";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store the information needed to create context menus.
		/// </summary>
		/// <param name="generalFile"></param>
		/// <param name="specificFile"></param>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeContextMenus(string generalFile, string specificFile)
		{
			if (m_cmnuHelper == null)
				m_cmnuHelper = new ContextMenuHelper(this);
			m_cmnuHelper.InitializeContextMenus(generalFile, specificFile);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the desired context menu.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override ContextMenuStrip CreateContextMenu(string name)
		{
			if (m_cmnuHelper != null)
				return m_cmnuHelper.CreateContextMenu(name);
			else
				return base.CreateContextMenu(name);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the Page Setup dialog with TE-specific settings.
		/// </summary>
		/// <param name="dlg">The Page Setup dialog.</param>
		/// ------------------------------------------------------------------------------------
		private void InitializePageSetupDlg(TePageSetupDlg dlg)
		{
			// Customize Page Setup dialog for different TE views.
			Debug.Assert(ActiveEditingHelper != null);
			if (ActiveEditingHelper.IsTrialPublicationView)
				return;
			dlg.HideAllowNonStandardChoicesOption();
			if (ActiveEditingHelper.IsBackTranslation)
				dlg.MaxNumberOfColumns = 1;
			else if (ActiveEditingHelper.IsCorrectionView)
			{
				dlg.IsLineSpacingVisible = false;
				dlg.IsPubPageSizeComboBoxEnabled = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applications containing derivations of this class implement this if they have their
		/// own custom control they want on a toolbar.
		/// </summary>
		/// <param name="name">Name of the toolbar container item for which a control is
		/// requested.</param>
		/// <param name="tooltip">The tooltip to assign to the custom control.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override Control LoadAppSpecificCustomToolBarControls(string name, string tooltip)
		{
			if (name != "tbbGotoScrCtrl")
				return null;

			// Since the toolbar adapter doesn't really have the ability to set
			// the tooltip for all controls that may be embedded in a custom
			// control, we need to set the tooltip for the GotoReferenceControl
			// ourselves.
			GotoReferenceControl.ToolTip = tooltip;
			GotoReferenceControl.Width = 100;
			return GotoReferenceControl;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the individual views (panes) and wrappers for each user view.
		/// Add the views to the side bar tabs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddUserViews()
		{
			// Add views to the Scripture tab
			AddDraftView(TeResourceHelper.GetResourceString("kstidDraftView"));
			AddPrintLayoutView(TeResourceHelper.GetResourceString("kstidPrintLayoutView"),
				TeViewType.PrintLayout, "Scripture Draft",
							TeResourceHelper.SideBarIndices.PrintLayout,
							kScrPrintLayoutSBItemName);
			AddPrintLayoutView(TeResourceHelper.GetResourceString("kstidCorrectionLayoutView"),
				TeViewType.Correction, "Correction Layout",
				TeResourceHelper.SideBarIndices.CorrectionPrintLayout,
				kScrCorrectionLayoutSBItemName);
			AddPrintLayoutView(TeResourceHelper.GetResourceString("kstidTrialPublicationView"),
				TeViewType.TrialPublication, "Trial Publication",
							TeResourceHelper.SideBarIndices.TrialPublication,
							kScrTrialPublicationSBItemName);
			if (Options.UseVerticalDraftView)
			{
				// create the vertical draft view
				AddVerticalView("VEdit");
			}

			// Add views to the Back Translation tab
			AddBackTransDraftView(TeResourceHelper.GetResourceString("kstidBackTransDraftView"));
			AddBackTransPrintLayoutView(TeResourceHelper.GetResourceString("kstidBackTransPrintLayoutSbsView"),
							TeViewType.BackTranslationParallelPrint);
			AddBackTransConsultantCheckView(TeResourceHelper.GetResourceString("kstidBackTransReviewView"));
			AddBackTransPrintLayoutView(TeResourceHelper.GetResourceString("kstidBackTransPrintLayoutView"),
							TeViewType.BackTranslationSimplePrintLayout);
			// create Default Concordance View
			//MakeNewView(UserViewType.kvwtConc, string.Empty, "Concordance");
			//sideBarButton = new SideBarButton();
			//sideBarButton.ImageIndex = 0; // TODO: fix this.
			//sideBarButton.Name = "concViewButton";
			//sideBarButton.Text = userView.ViewNameShort;
			//sideBarFw.Tabs[0].Buttons.Add(sideBarButton);
			//m_rgClientViews.Add(null);

			// Add views to the Checking tab
			AddEditorialChecksView(TeResourceHelper.GetResourceString("kstidEditorialChecks"));
			AddKeyTermsView(TeResourceHelper.GetResourceString("kstidKeyTermsView"));
			}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Scripture/Draft View
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void AddDraftView(string viewName)
		{
			// Add this user view to the Scripture sidebar tab.
			SBTabItemProperties itemProps = new SBTabItemProperties(this);
			itemProps.Name = kScrDraftViewSBItemName;
			itemProps.Text = viewName;
			itemProps.ImageIndex = (int)TeResourceHelper.SideBarIndices.Draft;
			itemProps.Tag = TeViewType.DraftView;
			itemProps.Message = "SwitchActiveView";
			AddSideBarTabItem(kScrSBTabName, itemProps);
			m_uncreatedViews.Add(TeViewType.DraftView,
				new TeSelectableViewFactory(viewName, TeViewType.DraftView, CreateDraftView));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Scripture/Vertical View.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void AddVerticalView(string viewName)
		{
			// Add this user view to the Scripture sidebar tab.
			SBTabItemProperties itemProps = new SBTabItemProperties(this);
			itemProps.Name = kScrVerticalViewSBItemName;
			itemProps.Text = "VEdit";
			itemProps.ImageIndex = (int)TeResourceHelper.SideBarIndices.Draft; // should maybe be some new constant?
			itemProps.Tag = TeViewType.VerticalView;
			itemProps.Message = "SwitchActiveView";
			AddSideBarTabItem(kScrSBTabName, itemProps);
			m_uncreatedViews.Add(TeViewType.VerticalView,
				new TeSelectableViewFactory(viewName, TeViewType.VerticalView, CreateVerticalView));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Scripture/Print layout View
		/// </summary>
		/// <param name="viewName">Name of the resource string for the view.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="pubName">Name of the publication.</param>
		/// <param name="sideBarIndex">Index of the icon for the view in the side bar.</param>
		/// <param name="sideBarItemName">Name of the side bar item.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void AddPrintLayoutView(string viewName, TeViewType viewType,
			string pubName, TeResourceHelper.SideBarIndices sideBarIndex, string sideBarItemName)
		{
			// Add this user view to the Scripture sidebar tab.
			SBTabItemProperties itemProps = new SBTabItemProperties(this);
			itemProps.Name = sideBarItemName;
			itemProps.Text = viewName;
			itemProps.ImageIndex = (int)sideBarIndex;
			itemProps.Tag = viewType;
			itemProps.Message = "SwitchActiveView";
			AddSideBarTabItem(kScrSBTabName, itemProps);
			m_uncreatedViews.Add(viewType,
				new TePrintLayoutViewFactory(viewName, viewType, pubName, CreatePrintLayoutView));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Back Translation Review view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void AddBackTransConsultantCheckView(string viewName)
		{
			// Add this user view to the BT sidebar tab.
			SBTabItemProperties itemProps = new SBTabItemProperties(this);
			itemProps.Name = kBTReviewViewSBItemName;
			itemProps.Text = viewName;
			itemProps.ImageIndex = (int)TeResourceHelper.SideBarIndices.BTConsultantCheck;
			itemProps.Tag = TeViewType.BackTranslationConsultantCheck;
			itemProps.Message = "SwitchActiveView";
			AddSideBarTabItem(kBTSBTabName, itemProps);
			m_uncreatedViews.Add(TeViewType.BackTranslationConsultantCheck,
				new TeSelectableViewFactory(viewName, TeViewType.BackTranslationConsultantCheck,
					CreateBackTransConsultantCheckView));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Back Translation/Draft View
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void AddBackTransDraftView(string viewName)
		{
			// Add this user view to the BackTrans sidebar tab.
			SBTabItemProperties itemProps = new SBTabItemProperties(this);
			itemProps.Name = kBTDraftViewSBItemName;
			itemProps.Text = viewName;
			itemProps.ImageIndex = (int)TeResourceHelper.SideBarIndices.BTDraft;
			itemProps.Tag = TeViewType.BackTranslationDraft;
			itemProps.Message = "SwitchActiveView";
			AddSideBarTabItem(kBTSBTabName, itemProps);
			m_uncreatedViews.Add(TeViewType.BackTranslationDraft,
				new TeSelectableViewFactory(viewName, TeViewType.BackTranslationDraft,
					CreateBackTransDraftView));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Back Translation/Print Layout View
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void AddBackTransPrintLayoutView(string viewName, TeViewType viewType)
		{
			bool fIsSideBySide = viewType == TeViewType.BackTranslationParallelPrint;

			// Add this user view to the BackTrans sidebar tab.
			SBTabItemProperties itemProps = new SBTabItemProperties(this);
			itemProps.Name = fIsSideBySide ?
				kBTPrintLayoutSbsSBItemName : kBTPrintLayoutSBItemName;
			itemProps.Text = viewName;
			itemProps.ImageIndex = fIsSideBySide ?
				(int)TeResourceHelper.SideBarIndices.BTParallelPrintLayout :
				(int)TeResourceHelper.SideBarIndices.BTSimplePrintLayout;
			itemProps.Tag = viewType;
			itemProps.Message = "SwitchActiveView";
			AddSideBarTabItem("TabBackTrans", itemProps);

			string pubName = (viewType == TeViewType.BackTranslationParallelPrint) ?
				"Back Translation Side-by-Side" : "Back Translation";

			m_uncreatedViews.Add(viewType, new TePrintLayoutViewFactory(viewName, viewType,
				pubName, CreateBackTransPrintLayoutView));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Editorial Checks View
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void AddEditorialChecksView(string viewName)
		{
			// Add this user view to the Checking sidebar tab.
			SBTabItemProperties itemProps = new SBTabItemProperties(this);
			itemProps.Name = kChkEditChecksSBItemName;
			itemProps.Text = viewName;
			itemProps.ImageIndex = (int)TeResourceHelper.SideBarIndices.EditorialChecks;
			itemProps.Tag = TeViewType.EditorialChecks;
			itemProps.Message = "SwitchActiveView";
			AddSideBarTabItem(kChkSBTabName, itemProps);
			m_uncreatedViews.Add(TeViewType.EditorialChecks,
				new TeSelectableViewFactory(viewName, TeViewType.EditorialChecks, CreateEditorialChecksView));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Checking/Key Terms View
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void AddKeyTermsView(string viewName)
		{
			// Add this user view to the Checking sidebar tab.
			m_keyTermsViewItemProps = new SBTabItemProperties(this);
			m_keyTermsViewItemProps.Name = kChkKeyTermsSBItemName;
			m_keyTermsViewItemProps.Text = viewName;
			m_keyTermsViewItemProps.ImageIndex = (int)TeResourceHelper.SideBarIndices.KeyTerms;
			m_keyTermsViewItemProps.Tag = TeViewType.KeyTerms;
			m_keyTermsViewItemProps.Message = "SwitchActiveView";
			AddSideBarTabItem(kChkSBTabName, m_keyTermsViewItemProps);
			m_uncreatedViews.Add(TeViewType.KeyTerms, new TeSelectableViewFactory(viewName,
				TeViewType.KeyTerms, CreateKeyTermsView));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the Scripture/Draft View
		/// </summary>
		/// <param name="viewName">Name of the view.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="tabItem">The tab item.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected ISelectableView CreateDraftView(string viewName, TeViewType viewType,
			SBTabItemProperties tabItem)
		{
			TeScrDraftViewProxy topDraftView = new TeScrDraftViewProxy(this, "TopDraftView",
				true, false, false, TeViewType.DraftView);
			DraftStylebarProxy topStylebar = new DraftStylebarProxy(this, "Top", false);
			TeScrDraftViewProxy bottomDraftView = new TeScrDraftViewProxy(this, "BottomDraftView",
				true, false, false, TeViewType.DraftView);
			DraftStylebarProxy bottomStylebar = new DraftStylebarProxy(this, "Bottom", false);

			TeFootnoteDraftViewProxy footnoteDraftView = new TeFootnoteDraftViewProxy(this,
				"DraftFootnoteView", true, false);
			DraftStylebarProxy footnoteStylebar = new DraftStylebarProxy(this, "Footnote", true);

			// Construct the one draft view wrapper (client window)
			DraftViewWrapper draftViewWrap = new DraftViewWrapper(kDraftViewWrapperName, this,
				m_cache, StyleSheet, SettingsKey, topDraftView, topStylebar, bottomDraftView,
				bottomStylebar, footnoteDraftView, footnoteStylebar);
			((ISelectableView)draftViewWrap).BaseInfoBarCaption = viewName;
			draftViewWrap.ResumeLayout();

			if (tabItem != null)
			{
				tabItem.Tag = draftViewWrap;
				tabItem.Update = true;
			}

			ClientControls.Add(draftViewWrap);
			// Bring the draftView to the top of the z-order, so that
			// (if it is the active view) it fills only the remaining space
			draftViewWrap.BringToFront();
			m_rgClientViews.Add(TeEditingHelper.ViewTypeString(viewType), draftViewWrap);
			m_uncreatedViews.Remove(viewType);
			return draftViewWrap;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the Scripture/Vertical View.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ISelectableView CreateVerticalView(string viewName, TeViewType viewType,
			SBTabItemProperties tabItem)
		{
			// Current view cannot handle an empty project, so the quick fix is to not put up the
			// view at all for an empty project. User will need to add a book and then re-open TE.
			// Since this is only for testing now, this seems like the safest/easiest fix.
			//
			// This will not prevent crashes if the last book is deleted. There are also other ways
			// you can probably crash the view - delete the section that the view is showing.
			if (Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS.Count == 0)
				return null;

			// Create a draft view (adapted from CreateDraftContainer).
			DraftView vDraft = new VerticalDraftView(m_cache, m_app, m_app, false, Handle.ToInt32());
			// Although the constructor sets this, setting it with the setter (bizarrely) does extra stuff which for the
			// moment we need. When we refine VerticalDraftView so it doesn't use laziness, we can remove this, since
			// it won't need the paragraph counter which the Cache setter creates.
			vDraft.Cache = m_cache;
			vDraft.StyleSheet = m_StyleSheet;
			vDraft.MakeRoot();
			vDraft.Editable = true;

			vDraft.Anchor = AnchorStyles.Top | AnchorStyles.Left |
				AnchorStyles.Right | AnchorStyles.Bottom;
			vDraft.Dock = DockStyle.Fill;
			((ISelectableView)vDraft).BaseInfoBarCaption = "VEdit";

			// This is done in the constructors of many of our other views, and it is essential that a
			// main client window be not visible, otherwise, it appears initially even if not meant to
			// be selected (at least, the last one created and brought to the front appears). Only one
			// client control is supposed to be visible.
			// I thought it better to put it here because the VerticalDraftView is parallel in function
			// to a regular DraftView, and if there are other clients, they may not want it be initially
			// invisible; unlike other classes used as client windows, this one is not necessarily
			// intended to be used ONLY as a direct client window of the main window. So I put setting
			// the visibility here.
			vDraft.Visible = false;

			if (tabItem != null)
			{
				tabItem.Tag = vDraft;
				tabItem.Update = true;
			}

			ClientControls.Add(vDraft);
			// Bring the draftView to the top of the z-order, so that
			// (if it is the active view) it fills only the remaining space (Review JohnT: is this needed here? Copied from DraftView)
			vDraft.BringToFront();
			m_rgClientViews.Add(TeEditingHelper.ViewTypeString(TeViewType.VerticalView), vDraft);
			m_uncreatedViews.Remove(TeViewType.VerticalView);
			return vDraft;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Scripture/Print layout View
		/// </summary>
		/// <param name="viewName">Name of the view.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="pubName">Name of the publication.</param>
		/// <param name="tabItem">The tab item.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected ISelectableView CreatePrintLayoutView(string viewName, TeViewType viewType,
			string pubName, SBTabItemProperties tabItem)
		{
			// Construct the publication control (client window)
			IPublication pub =
				Cache.ServiceLocator.GetInstance<IPublicationRepository>().FindByName(pubName);
			Debug.Assert(pub != null, "Unable to find the publication " + pubName);

			ScripturePublication pubControl = CreatePublicationView(pub, viewType, m_cache.DefaultVernWs);
			WritingSystem wsObj = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			using (var uowHelper = new NonUndoableUnitOfWorkHelper(m_cache.ServiceLocator.ActionHandler))
			{
				pub.IsLeftBound = !wsObj.RightToLeftScript;
				uowHelper.RollBack = false;
			}

			((ISelectableView)pubControl).BaseInfoBarCaption = viewName;

			if (tabItem != null)
			{
				tabItem.Tag = pubControl;
				tabItem.Update = true;
			}

			ClientControls.Add(pubControl);
			// Bring the publication to the top of the z-order, so that
			// (if it is the active view) it fills only the remaining space
			ClientControls.SetChildIndex(pubControl, 0);
			m_rgClientViews.Add(TeEditingHelper.ViewTypeString(viewType), pubControl);
			m_uncreatedViews.Remove(viewType);
			return pubControl;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the Back Translation Review view
		/// </summary>
		/// <param name="viewName">Name of the view.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="tabItem">The tab item.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected ISelectableView CreateBackTransConsultantCheckView(string viewName,
			TeViewType viewType, SBTabItemProperties tabItem)
		{
			TeScrDraftViewProxy topDraftView = new TeScrDraftViewProxy(this, "BTReviewDraftView",
				false, false, false, TeViewType.BackTranslationConsultantCheck);
			DraftStylebarProxy topStylebar = new DraftStylebarProxy(this, "BTReview", false);

			TeFootnoteDraftViewProxy footnoteDraftView = new TeFootnoteDraftViewProxy(this,
				"BTReviewFootnoteView", false, true);
			DraftStylebarProxy footnoteStylebar = new DraftStylebarProxy(this,
				"BTReviewFootnote", true);

			// Construct the one view wrapper (client window)
			ViewWrapper reviewWrap = new ViewWrapper(kBTReviewWrapperName, this, m_cache,
				StyleSheet, SettingsKey, topDraftView, topStylebar, footnoteDraftView,
				footnoteStylebar);
			((ISelectableView)reviewWrap).BaseInfoBarCaption = viewName;

			if (tabItem != null)
			{
				tabItem.Tag = reviewWrap;
				tabItem.Update = true;
			}

			ClientControls.Add(reviewWrap);
			// Bring the draftView to the top of the z-order, so that
			// (if it is the active view) it fills only the remaining space
			reviewWrap.BringToFront();
			m_rgClientViews.Add(TeEditingHelper.ViewTypeString(TeViewType.BackTranslationConsultantCheck),
				reviewWrap);
			m_uncreatedViews.Remove(TeViewType.BackTranslationConsultantCheck);
			return reviewWrap;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the Back Translation/Draft View
		/// </summary>
		/// <param name="viewName">Name of the view.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="tabItem">The tab item.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected ISelectableView CreateBackTransDraftView(string viewName, TeViewType viewType,
			SBTabItemProperties tabItem)
		{
			TeScrDraftViewProxy draftView = new TeScrDraftViewProxy(this, kDraftViewName, true,
				true, false, TeViewType.DraftView);
			TeScrDraftViewProxy btDraftView = new TeScrDraftViewProxy(this, kBackTransView, true,
				true, false, TeViewType.BackTranslationDraft);
			DraftStylebarProxy stylebar = new DraftStylebarProxy(this, "BTSplit", false);

			TeFootnoteDraftViewProxy footnoteDraftView = new TeFootnoteDraftViewProxy(this,
				kDraftFootnoteViewName, true, false);
			TeFootnoteDraftViewProxy footnoteBtDraftView = new TeFootnoteDraftViewProxy(this,
				kBackTransFootnoteView, true, true);
			DraftStylebarProxy footnoteStylebar = new DraftStylebarProxy(this,
				"BackTransFootnote", true);

			// Construct the one view wrapper
			BtDraftSplitWrapper btWrap = new BtDraftSplitWrapper(kBtDraftSplitView, this,
				m_cache, StyleSheet, SettingsKey, draftView, stylebar, btDraftView,
				footnoteDraftView, footnoteStylebar, footnoteBtDraftView);
			((ISelectableView)btWrap).BaseInfoBarCaption = viewName;

			if (tabItem != null)
			{
				tabItem.Tag = btWrap;
				tabItem.Update = true;
			}

			ClientControls.Add(btWrap);
			// Bring the wrapper to the top of the z-order, so that
			// (if it is the active view) it fills only the remaining space
			btWrap.BringToFront();
			//Debug.Assert(m_rgClientViews.Count >= 1);
			m_rgClientViews.Add(TeEditingHelper.ViewTypeString(TeViewType.BackTranslationDraft), btWrap);
			m_uncreatedViews.Remove(TeViewType.BackTranslationDraft);
			return btWrap;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the Back Translation/Print Layout View
		/// </summary>
		/// <param name="viewName">Name of the view.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="pubName">Name of the publication.</param>
		/// <param name="tabItem">The tab item.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected ISelectableView CreateBackTransPrintLayoutView(string viewName,
			TeViewType viewType, string pubName, SBTabItemProperties tabItem)
		{
			// Construct the publication control (client window)
			IPublication pub =
				Cache.ServiceLocator.GetInstance<IPublicationRepository>().FindByName(pubName);
			int ws = GetBackTranslationWsForView(TeEditingHelper.ViewTypeString(viewType));
			ScripturePublication pubControl = CreatePublicationView(pub, viewType, ws);
			pubControl.BaseInfoBarCaption = viewName;
			WritingSystem wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);

			using (var uowHelper = new NonUndoableUnitOfWorkHelper(m_cache.ServiceLocator.ActionHandler))
			{
				pub.IsLeftBound = !wsObj.RightToLeftScript;
				uowHelper.RollBack = false;
			}

			if (tabItem != null)
			{
				tabItem.Tag = pubControl;
				tabItem.Update = true;
			}

			ClientControls.Add(pubControl);

			// Bring the publication to the top of the z-order, so that
			// (if it is the active view) it fills only the remaining space
			ClientControls.SetChildIndex(pubControl, 0);
			m_rgClientViews.Add(TeEditingHelper.ViewTypeString(viewType), pubControl);
			m_uncreatedViews.Remove(viewType);
			return pubControl;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the editorial checks view when the user switches to it.
		/// </summary>
		/// <param name="viewName">Name of the view.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="tabItem">The tab item.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual ISelectableView CreateEditorialChecksView(string viewName,
			TeViewType viewType, SBTabItemProperties tabItem)
		{
			// Construct a editorial checks view (client window)
			var checkingViewProxy = new CheckingViewProxy(this, "EditorialChecksDraftView");

			EditorialChecksViewWrapper viewWrapper = new EditorialChecksViewWrapper(this,
				m_cache, m_bookFilter, checkingViewProxy, m_app.ProjectSpecificSettingsKey,
				m_delegate.GetProjectName(m_cache), m_app, m_app);

			((ISelectableView)viewWrapper).BaseInfoBarCaption = viewName;

			if (tabItem != null)
			{
				tabItem.Tag = viewWrapper;
				tabItem.Update = true;
			}

			ClientControls.Add(viewWrapper);
			// Bring the key terms view to the top of the z-order, so that
			// (if it is the active view) it fills only the remaining space
			ClientControls.SetChildIndex(viewWrapper, 0);
			m_rgClientViews.Add(TeEditingHelper.ViewTypeString(TeViewType.EditorialChecks),
				viewWrapper);
			m_uncreatedViews.Remove(TeViewType.EditorialChecks);

			return viewWrapper;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the key terms view when the user switches to it.
		/// </summary>
		/// <param name="viewName">Name of the view.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="tabItem">The tab item.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual ISelectableView CreateKeyTermsView(string viewName,
			TeViewType viewType, SBTabItemProperties tabItem)
		{
			Debug.Assert(TheKeyTermsWrapper == null);

			// Construct a key terms view (client window)
			CheckingViewProxy checkingViewProxy = new CheckingViewProxy(this, "BiblicalTermsDraftView");

			KeyTermsViewWrapper keyTermsViewWrapper = new KeyTermsViewWrapper(this, m_cache,
				checkingViewProxy, m_app.ProjectSpecificSettingsKey, Handle.ToInt32(),
				m_delegate.GetProjectName(m_cache), m_StyleSheet, m_app);
			((ISelectableView)keyTermsViewWrapper).BaseInfoBarCaption = viewName;

			if (tabItem != null)
			{
				tabItem.Tag = keyTermsViewWrapper;
				tabItem.Update = true;
			}

			ClientControls.Add(keyTermsViewWrapper);
			// Bring the key terms view to the top of the z-order, so that
			// (if it is the active view) it fills only the remaining space
			ClientControls.SetChildIndex(keyTermsViewWrapper, 0);
			m_rgClientViews.Add(TeEditingHelper.ViewTypeString(TeViewType.KeyTerms),
				keyTermsViewWrapper as IRootSite);
			m_uncreatedViews.Remove(TeViewType.KeyTerms);

			return keyTermsViewWrapper;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a publication view of the desired type
		/// </summary>
		/// <param name="pub">The DB representation of the publication</param>
		/// <param name="viewType">The type of view to create.</param>
		/// <param name="btWs">BacktTranslation WS</param>
		/// <returns>A ScripturePublication</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual ScripturePublication CreatePublicationView(IPublication pub,
			TeViewType viewType, int btWs)
		{
			ScripturePublication pubControl = (viewType == TeViewType.BackTranslationParallelPrint ?
				new TeBtPublication(StyleSheet, Handle.ToInt32(), pub, viewType, DateTime.Now, m_app, m_app, btWs) :
				new ScripturePublication(StyleSheet, Handle.ToInt32(), pub, viewType, DateTime.Now, m_app, m_app, btWs));

			pubControl.Anchor = AnchorStyles.Top | AnchorStyles.Left |
				AnchorStyles.Right | AnchorStyles.Bottom;

			pubControl.Dock = DockStyle.Fill;
			pubControl.Name = TeEditingHelper.ViewTypeString(viewType);
			pubControl.Visible = false;
			RegisterFocusableView(pubControl);
			return pubControl;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add filters to the side bar
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void AddFilters()
		{
			// Add defined filters to the filters submenu
			if (TMAdapter == null)
				return;

			foreach (ICmFilter filter in m_cache.LangProject.FiltersOC)
			{
				// only use filters that are defined for this application
				if (filter.App != TeApp.AppGuid)
					continue;

				switch (filter.ClassId)
				{
					case ScrBookTags.kClassId:
						{
							string filterName = string.Format(
								TeResourceHelper.GetResourceString("kstidFilterNameTemplate"), filter.Name);

							TMItemProperties itemProps = new TMItemProperties();
							itemProps.Text = filterName;

							// setup the event handler for book filters
							itemProps.CommandId = "CmdBookFilterDlg";
							TMAdapter.AddMenuItem(itemProps, "mnuFilters", null);
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
		/// Create the "book" filter if it doesn't already exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateBookFilterIfMissing()
		{
			foreach (ICmFilter filter in m_cache.LangProject.FiltersOC)
			{
				if (filter.App == TeApp.AppGuid && filter.ClassId == ScrBookTags.kClassId)
					return;
			}

			ICmFilter bookFilter = m_cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			m_cache.LangProject.FiltersOC.Add(bookFilter);
			bookFilter.App = TeApp.AppGuid;
			bookFilter.ClassId = ScrBookTags.kClassId;
			bookFilter.Name = TeResourceHelper.GetResourceString("kstidFilterBook");
			bookFilter.ColumnInfo = "3002,3002003";

			// Theoretically, we should probably be creating a CmRow and a CmCell for this filter,
			// but since we aren't making use of the FW approach for filtering by book, we don't
			// need those things.
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create the client windows and add corresponding stuff to the sidebar, View menu,
		/// etc.
		/// </summary>
		///
		/// <exception cref="Exception">Invalid user view type in database</exception>
		/// -----------------------------------------------------------------------------------
		public override void InitAndShowClient()
		{
			CheckDisposed();

			using (NonUndoableUnitOfWorkHelper undoHelper = new NonUndoableUnitOfWorkHelper(
				m_cache.ServiceLocator.GetInstance<IActionHandler>()))
			{
				CreateBookFilterIfMissing();

				// Add the user views to the sidebar, menu, info bar
				AddUserViews();

				// Add the filters too
				AddFilters();

				undoHelper.RollBack = false;
			}

			if (SIBAdapter != null)
			{
				SIBAdapter.SetupViewMenuForSideBarTabs(TMAdapter, "mnuFilters");
				SIBAdapter.LoadSettings(MainWndSettingsKey);
			}

			GotoReferenceControl.PassageChanged +=
				new ScrPassageControl.PassageChangedHandler(OnPassageChanged);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When finished initializing, show a helpful window if there are no books yet in the
		/// project. Handle some of the commands on that dialog box.
		/// </summary>
		/// <returns>True if successful; false if user chooses to exit app</returns>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="REVIEW: ParatextHelper.GetAssociatedProject returns a reference (?)")]
		public override bool OnFinishedInit()
		{
			CheckDisposed();

			if (!base.OnFinishedInit())
				return false;

			EnsureEnglishLdsExists();
			ScrText assocProj = ParatextHelper.GetAssociatedProject(m_cache.ProjectId);
			if (assocProj != null && m_app.MainWindows.Count == 1)
			{
				MessageBox.Show(this, string.Format(TeResourceHelper.GetResourceString("kstidNoExportStartupMsg"),
					assocProj.JoinedNameAndFullName), FwUtils.ksTeAppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}

			// If there are books in the project...
			if (m_cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS.Count > 0)
			{
				if (m_startupLink != null)
				{
					// This should automatically disabled any problem filters.
					m_app.HandleIncomingLink(m_startupLink);
				}
				else
				{
					// When the TE window first opens, check if a book filter was enabled when
					// the user last closed TE. If so, then show the user the book filter dialog.
					if (TeProjectSettings.BookFilterEnabled)
						OnBookFilter(null);
				}

				if (!ActiveViewHelper.IsViewVisible(ActiveView) && SIBAdapter != null)
				{
					// Attempt to get the last active tab and tab item from the registry.
					// If no tab and tab item are set, default to the Scripture tab
					// and the Draft view tab item.
					string activeTab = (string)MainWndSettingsKey.GetValue("ActiveTab", kScrSBTabName);
					string activeTabItem = (string)MainWndSettingsKey.GetValue("ActiveTabItem",
						kScrDraftViewSBItemName);
					SIBAdapter.SetCurrentTabItem(activeTab, activeTabItem, true);
				}

				return true;
			}

			// There are no books in the project. Handle things nicely for the user.
			// switch to Scripture Task - Draft view
			if (SIBAdapter != null)
				SIBAdapter.SetCurrentTabItem(kScrSBTabName, kScrDraftViewSBItemName, true);

			// Open a helpful window
			//Mediator msgMediator = (m_app != null ? m_app.MessageMediator : null);
			using (EmptyScripture dlg = new EmptyScripture(m_tmAdapter, m_cache, m_app))
			{
				dlg.ShowDialog();

				switch (dlg.OptionChosen)
				{
					case EmptyScripture.Option.Book:
						// insert the book
						Debug.Assert(ActiveEditingHelper != null);
						string undo, redo;
						TeResourceHelper.MakeUndoRedoLabels("kstidInsertBook", out undo, out redo);

						using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
							m_cache.ServiceLocator.GetInstance<IActionHandler>(), undo, redo))
						{
							ActiveEditingHelper.InsertBook(dlg.BookChosen);
							undoHelper.RollBack = false;
						}
						break;
					case EmptyScripture.Option.Import:
						OnImportStandardFormat(null);
						break;
					case EmptyScripture.Option.Exit:
						OnFileClose(null);
						return true;
				}
			}

			return true;
		}

		private void EnsureEnglishLdsExists()
		{
			string paratextProjectDir = ParatextHelper.ProjectsDirectory;

			if (!String.IsNullOrEmpty(paratextProjectDir))
			{
				string englishLdsPathname = Path.Combine(paratextProjectDir, "English.lds");
				if (!File.Exists(englishLdsPathname))
				{
					IStStyle normalStyle = m_StyleSheet.FindStyle(ScrStyleNames.Normal);
					ParatextLdsFileAccessor ldsAccessor = new ParatextLdsFileAccessor(Cache);
					UsfmStyEntry normalUsfmStyle = new UsfmStyEntry();
					StyleInfoTable styleTable = new StyleInfoTable(normalStyle.Name,
						Cache.ServiceLocator.WritingSystemManager);
					normalUsfmStyle.SetPropertiesBasedOnStyle(normalStyle);
					styleTable.Add(normalStyle.Name, normalUsfmStyle);
					styleTable.ConnectStyles();
					ldsAccessor.WriteParatextLdsFile(englishLdsPathname,
						Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en"), normalUsfmStyle);
					// We pass the directory (rather than passing no arguments, and letting the paratext dll figure
					// it out) because the figuring out goes wrong on Linux, where both programs are simulating
					// the registry.
					ScrTextCollection.Initialize(ParatextHelper.ProjectsDirectory, false);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populate the InsertBook menus with the book names from the database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeInsertBookMenus()
		{
			CheckDisposed();

			TMItemProperties itemProps;

			for (int bookNum = 1; bookNum <= 66; bookNum++)
			{
				string bookName = ScriptureServices.GetUiBookName(bookNum);

				itemProps = new TMItemProperties();
				itemProps.Text = bookName;
				itemProps.Name = "mnu" + bookName;
				itemProps.CommandId = "CmdInsertBook";
				itemProps.Tag = bookNum;

				// Add books to the menu off the Insert main menu. (Matthew is 40);
				TMAdapter.AddMenuItem(itemProps,
					(bookNum < 40 ? "mnuOldTestament" : "mnuNewTestament"), null);

				// Add books to the context menu that pops-up by clicking the
				// Books button on the empty scripture dialog. (Matthew is 40);
				itemProps.Name = "c" + itemProps.Name;
				TMAdapter.AddContextMenuItem(itemProps, "cmnuInsertBooks",
					(bookNum < 40 ? "cmnuOldTestament" : "cmnuNewTestament"), null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the scripture passage changed.
		/// </summary>
		/// <param name="newReference">The new reference.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnPassageChanged(ScrReference newReference)
		{
			Logger.WriteEvent(string.Format("TeMainWnd: New reference is {0}", newReference.AsString));

			if (!newReference.IsEmpty)
				GotoVerse(newReference);

			UserControl ctrl = ActiveView as UserControl;
			if (ctrl != null)
				ctrl.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is called through the message mediator from TeEditingHelper when the selection
		/// changes.  Used to update the GoToReferenceControl on the information toolbar.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnSelectedPassageChanged(object obj)
		{
			CheckDisposed();

			if (obj == null || obj.GetType() != typeof(ScrReference))
				return false;

			GotoReferenceControl.ScReference = (ScrReference)obj;
			return true;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the active view is showing a back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool IsBtViewActive
		{
			get
			{
				return m_selectedView is BtDraftSplitWrapper ||
					(ActiveEditingHelper.ViewType & TeViewType.BackTranslation) != 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the back translation writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DefaultBackTranslationWs
		{
			get { return m_defaultBackTranslationWs; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an object that encapsulates the registry key for the writing system ICU locale
		/// for the given BT view.
		/// </summary>
		/// <param name="btViewName">Name of the back translation view.</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "See TODO")]
		private RegistryStringSetting GetBtWsRegistrySetting(string btViewName)
		{
			// TODO: we're leaking btWsRegEntries
			RegistryKey btWsRegEntries = MainWndSettingsKey.CreateSubKey(kBtWsRegEntryName);
			return new RegistryStringSetting(btWsRegEntries, btViewName, string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the currently selected (visible) view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ISelectableView SelectedView
		{
			get
			{
				foreach (ISelectableView view in m_rgClientViews.Values)
					if (view != null && ((Control)view).Visible)
						return view;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the key terms view is visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool KeyTermsViewIsVisible
		{
			get
			{
				// NOTE: don't use TheKeyTermsWrapper here - this will cause TeKeyTerms.dll
				// to get loaded!
				IRootSite keyTermsWrapper;
				return (m_rgClientViews.TryGetValue(TeEditingHelper.ViewTypeString(TeViewType.KeyTerms),
					out keyTermsWrapper) && keyTermsWrapper is Control && ((Control)keyTermsWrapper).Visible);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the key terms view is created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool KeyTermsViewIsCreated
		{
			get
			{
				// NOTE: don't use TheKeyTermsWrapper here - this will cause TeKeyTerms.dll
				// to get loaded!
				IRootSite keyTermsWrapper;
				return m_rgClientViews.TryGetValue(TeEditingHelper.ViewTypeString(TeViewType.KeyTerms),
					out keyTermsWrapper) && keyTermsWrapper is Control;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the common selection (i.e. the selection that a view should make when it
		/// gains focus)
		/// </summary>
		/// <remarks>
		/// NOTE: this should be set to null if the view shouldn't try to make a selection.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected IVwSelection CommonSelection
		{
			get { return m_commonSelection; }
			set { m_commonSelection = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the goto reference toolbar control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DbScrPassageControl GotoReferenceControl
		{
			get
			{
				CheckDisposed();

				if (m_gotoRefCtrl == null)
				{
					m_gotoRefCtrl = new DbScrPassageControl(ScrReference.Empty, Cache.LangProject.TranslatedScriptureOA);
					m_gotoRefCtrl.ErrorCaption = FwUtils.ksTeAppName;
					m_gotoRefCtrl.AccessibleName = "GotoScrCtrl";
					m_gotoRefCtrl.Width = 150; // REVIEW: does this need to be localizable?
				}

				return m_gotoRefCtrl;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether a book filter is in effect. Returns <c>false</c> if all of the books
		/// are visible in the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool BookFilterInEffect
		{
			get
			{
				CheckDisposed();
				return (m_bookFilter == null ? false : !m_bookFilter.AllBooks);
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the key terms wrapper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public KeyTermsViewWrapper TheKeyTermsWrapper
		{
			get
			{
				CheckDisposed();

				IRootSite wrapper;
				if (m_rgClientViews.TryGetValue(TeEditingHelper.ViewTypeString(TeViewType.KeyTerms),
					out wrapper))
					return wrapper as KeyTermsViewWrapper;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the key terms draft view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SimpleDraftViewWrapper KeyTermsDraftView
		{
			get
			{
				CheckDisposed();
				if (TheKeyTermsWrapper != null)
					return (SimpleDraftViewWrapper)TheKeyTermsWrapper.DraftView;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the editorial checks view wrapper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EditorialChecksViewWrapper TheEditChecksViewWrapper
		{
			get
			{
				CheckDisposed();

				IRootSite wrapper;
				if (m_rgClientViews.TryGetValue(TeEditingHelper.ViewTypeString(TeViewType.EditorialChecks),
					out wrapper))
				{
					return wrapper as EditorialChecksViewWrapper;
				}
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the editorial checks draft view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SimpleDraftViewWrapper EditorialChecksDraftView
		{
			get
			{
				CheckDisposed();
				if (TheEditChecksViewWrapper != null)
					return (SimpleDraftViewWrapper)TheEditChecksViewWrapper.DraftView;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the wrapper for the BT/Draft split view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BtDraftSplitWrapper TheBTSplitWrapper
		{
			get
			{
				CheckDisposed();

				IRootSite wrapper;
				if (m_rgClientViews.TryGetValue(
					TeEditingHelper.ViewTypeString(TeViewType.BackTranslationDraft), out wrapper))
					return wrapper as BtDraftSplitWrapper;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the wrapper for the BT review view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ViewWrapper TheBTReviewWrapper
		{
			get
			{
				CheckDisposed();

				IRootSite wrapper;
				if (m_rgClientViews.TryGetValue(
					TeEditingHelper.ViewTypeString(TeViewType.BackTranslationConsultantCheck), out wrapper))
					return wrapper as ViewWrapper;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the draft view wrapper window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
	public DraftViewWrapper TheDraftViewWrapper
		{
			get
			{
				CheckDisposed();

				IRootSite wrapper;
				if (m_rgClientViews.TryGetValue(TeEditingHelper.ViewTypeString(TeViewType.DraftView),
					out wrapper))
					return wrapper as DraftViewWrapper;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the draft view (The actual DraftView, not the wrapper.)
		/// (When there are multiple draftviews (i.e. when the draftview is split into two
		/// panes), this will return the one with focus).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual DraftView TheDraftView
		{
			get
			{
				CheckDisposed();

				DraftViewWrapper wrapper = TheDraftViewWrapper;
				if (wrapper != null)
					return wrapper.DraftView;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the draft view zoom percent.
		/// </summary>
		/// <remarks>This is safe to call even if the draft view has not been created and
		/// initialized</remarks>
		/// ------------------------------------------------------------------------------------
		public float DraftViewZoomPercent
		{
			get
			{
				return TheDraftView != null ? TheDraftView.Zoom :
					m_draftViewZoomSettingAlternate.Value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote view zoom percent.
		/// </summary>
		/// <remarks>This is safe to call even if the footnote view has not been created and
		/// initialized</remarks>
		/// ------------------------------------------------------------------------------------
		public float FootnoteZoomPercent
		{
			get
			{
				DraftViewWrapper wrapper = TheDraftViewWrapper;
				return (wrapper != null && wrapper.FootnoteView != null) ? wrapper.FootnoteView.Zoom :
					m_footnoteViewZoomSettingAlternate.Value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the editing helper associated with the active view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual TeEditingHelper ActiveEditingHelper
		{
			get
			{
				CheckDisposed();
				return ActiveView == null || ActiveView.EditingHelper == null ? null :
					ActiveView.EditingHelper.CastAs<TeEditingHelper>();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the root object (Scripture)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int HvoAppRootObject
		{
			get
			{
				CheckDisposed();
				return m_scr.Hvo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets the Scripture style sheet
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FwStyleSheet StyleSheet
		{
			get
			{
				CheckDisposed();

				if (m_StyleSheet == null)
					m_StyleSheet = new TeStyleSheet();
				return m_StyleSheet;
			}
		}

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
		/// Gets the book filter for this main window
		/// </summary>
		/// <remarks>Virtual to support testing</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual FilteredScrBooks BookFilter
		{
			get
			{
				CheckDisposed();
				return m_bookFilter;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the publication control associated with the current view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override IPublicationView CurrentPublicationView
		{
			get { return CurrentPublicationControl; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the publication control associated with the current view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected PublicationControl CurrentPublicationControl
		{
			get
			{
				PublicationControl pubControl = ActiveView as PublicationControl;
				if (pubControl != null)
					return pubControl;

				// if the active view is not a publication then find the one that is assigned
				// to the current view.
				if (((TheDraftViewWrapper != null && TheDraftViewWrapper.Visible) ||
					(TheBTReviewWrapper != null && TheBTReviewWrapper.Visible) ||
					(TheBTSplitWrapper != null && TheBTSplitWrapper.Visible)) &&
					ActiveEditingHelper != null)
				{
					return GetNamedView(TeEditingHelper.ViewTypeString(
						ActiveEditingHelper.CorrespondingPrintableViewType))
						as PublicationControl;
				}

				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the publication associated with the current view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string CurrentPublicationName
		{
			get
			{
				if (CurrentPublicationControl != null)
					return CurrentPublicationControl.BaseInfoBarCaption;
				TePrintLayoutViewFactory factory = CorrespondingPrintViewFactory;
				return (factory == null) ? null : factory.BaseInfoBarCaption;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets publication used by current view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override IPublication CurrentPublication
		{
			get
			{
				PublicationControl pubControl = CurrentPublicationControl;
				if (pubControl == null)
				{
					TePrintLayoutViewFactory viewFactory = CorrespondingPrintViewFactory;
					if (viewFactory == null)
						return null;
					return m_cache.ServiceLocator.GetInstance<IPublicationRepository>().FindByName(viewFactory.PublicationName);
				}
				return pubControl.Publication;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the view factory for building a print layout view that corresponds to the
		/// current "draft" view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private TePrintLayoutViewFactory CorrespondingPrintViewFactory
		{
			get
			{
				if (ActiveEditingHelper == null)
					return null;
				ISelectableViewFactory factory;
				if (m_uncreatedViews.TryGetValue(ActiveEditingHelper.CorrespondingPrintableViewType,
					out factory))
				{
					return factory as TePrintLayoutViewFactory;
				}
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The current view can be printed or has an associated view that can be printed. (Used
		/// for enabling File Page Setup command)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool CanPrintFromView
		{
			get
			{
				IPublicationView pubView = CurrentPublicationView;
				return (pubView != null || (ActiveEditingHelper != null &&
					m_uncreatedViews.ContainsKey(ActiveEditingHelper.CorrespondingPrintableViewType)));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Current view (or its associated printable view) has at least one page of printable
		/// material. (Used for enabling the File Print command)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool HaveSomethingToPrint
		{
			get
			{
				return (CanPrintFromView &&
					(base.HaveSomethingToPrint || BookFilter.BookCount > 0));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether there is a selection in an editable view of
		/// Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool SelectionInEditableScripture
		{
			get
			{
				return ActiveEditingHelper != null &&
					BookFilter != null &&
					BookFilter.BookCount > 0 &&
					ActiveEditingHelper.CurrentSelection != null &&
					ActiveEditingHelper.Editable;
			}
		}

		#endregion

		#region Windows Form Designer generated code
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TeMainWnd));
			((System.ComponentModel.ISupportInitialize)(this.m_persistence)).BeginInit();
			this.SuspendLayout();
			//
			// TeMainWnd
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Name = "TeMainWnd";
			((System.ComponentModel.ISupportInitialize)(this.m_persistence)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region Commented Code
		// Sample of code from FwMainWnd for Browse,Data Entry, and Document Views
		//			private void m_mnuViewsBrowse_Click(object sender, System.EventArgs e)
		//				{
		//					this.m_mnuViewsBrowse.Checked = true;
		//					this.m_mnuViewsDataEntry.Checked = false;
		//					this.m_mnuViewsDocument.Checked = false;
		//					this.informationBar1.InfoBarLabel.Text = "Browse";
		//					this.informationBarButton.ImageIndex = 0;
		//				}
		//
		//			private void m_mnuViewsDataEntry_Click(object sender, System.EventArgs e)
		//			{
		//				this.m_mnuViewsBrowse.Checked = false;
		//				this.m_mnuViewsDataEntry.Checked = true;
		//				this.m_mnuViewsDocument.Checked = false;
		//				this.informationBar1.InfoBarLabel.Text = "Data Entry";
		//				this.informationBarButton.ImageIndex = 1;
		//			}
		//
		//			private void m_mnuViewsDocument_Click(object sender, System.EventArgs e)
		//			{
		//				this.m_mnuViewsBrowse.Checked = false;
		//				this.m_mnuViewsDataEntry.Checked = false;
		//				this.m_mnuViewsDocument.Checked = true;
		//				this.informationBar1.InfoBarLabel.Text = "Document";
		//				this.informationBarButton.ImageIndex = 2;
		//			}
		#endregion

		#region Update message handlers

		/// <summary>
		/// This menu item is available if Flex is.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		protected bool OnUpdateCorrectMisspelledWords(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;

				if (itemProps != null)
				{
					itemProps.Enabled = FwUtils.IsFlexInstalled;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}
		/// <summary>
		/// This menu item is available if Flex is.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		protected bool OnUpdateReviewMisspelledWords(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;

				if (itemProps != null)
				{
					itemProps.Enabled = FwUtils.IsFlexInstalled;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle updating the "Find in dictionary" menu item. If there is no selection we want
		/// to disable this menu item (TE-4805).
		/// </summary>
		/// <param name="args"><c>true</c> if we handled this message.</param>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateFindInDictionary(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;

				if (itemProps != null)
				{
					// Need to check that Flex config files exist (TE-4377)
					if (ActiveView != null && File.Exists(LexEntryUi.FlexConfigFile))
					{
						IVwRootSite rootsite = ActiveView.CastAsIVwRootSite();
						if (rootsite == null || rootsite.RootBox == null)
							itemProps.Enabled = false;
						else
						{
							IVwSelection sel = rootsite.RootBox.Selection;
							if (sel != null &&
								sel.SelType == VwSelType.kstText)
							{
								SelectionHelper helper = SelectionHelper.Create(sel, rootsite);
								// Only want to do find in dictionary for vernacular fields - will never find anyhing
								// when searching for analysis text
								itemProps.Enabled = helper.TextPropId == StTxtParaTags.kflidContents;
								// Should be able to do find for words in a caption - by doing a check like that shown below,
								// but the dialog is not expecting a multi-string field.
								// (helper.TextPropId == CmPictureTags.kflidCaption && helper.Ws == Cache.DefaultVernWs);
							}
							else
								itemProps.Enabled = false;
						}
					}
					else
						itemProps.Enabled = false;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle updating the "Find in related words" menu item. If there is no selection we
		/// want to disable this menu item (TE-4805).
		/// </summary>
		/// <param name="args"><c>true</c> if we handled this message.</param>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateFindRelatedWords(object args)
		{
			return OnUpdateFindInDictionary(args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle updating the "no filter" menu
		/// </summary>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateNoFilters(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;

				if (itemProps != null)
				{
					itemProps.Checked = !BookFilterInEffect;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle updating the book filter menu
		/// </summary>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateBookFilter(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;

				if (itemProps != null)
				{
					itemProps.Checked = BookFilterInEffect;
					itemProps.Enabled = (m_scr.ScriptureBooksOS.Count > 1);
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle updating the book filter toolbar button
		/// </summary>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateBookFilterToggle(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;

				if (itemProps != null)
				{
					itemProps.Checked = BookFilterInEffect;
					itemProps.Enabled = (m_scr != null && m_scr.ScriptureBooksOS.Count > 1);
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
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
					if (itemProps != null)
						tabProps = itemProps.Tag as SBTabProperties;
				}

				if (tabProps == null)
					return false;

				// For now, the menu is only visible when the current
				// view is a back translation view.
				SBTabProperties currTabProps = SIBAdapter.CurrentTabProperties;
				tabProps.ConfigureMenuVisible = tabProps.Name == kBTSBTabName;
				tabProps.ConfigureMenuEnabled = currTabProps != null && currTabProps.Name == kBTSBTabName;

				if (itemProps != null)
				{
					itemProps.Visible = tabProps.ConfigureMenuVisible;
					itemProps.Enabled = tabProps.Enabled;
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
		/// Update the Apply Untranslated Word menu/toolbar item
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateApplyUnTransWord(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = SelectionInEditableScripture &&
						ActiveEditingHelper.IsBackTranslation &&
						!BTStatusIconSelected();
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if a status icon is selected in the BT view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool BTStatusIconSelected()
		{
			SelectionHelper helper = ActiveEditingHelper.CurrentSelection;
			if (helper == null || helper.NumberOfLevels <= 0)
				return false;

			return (helper.LevelInfo[0].tag == StTxtParaTags.kflidTranslations);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enables/disables the styles combobox
		/// </summary>
		/// <param name="args"></param>
		/// <returns>true if the combo box is defined and the parent form is TeMainWnd;
		/// false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateStyleComboBox(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;

				if (itemProps == null)
					return false;

				ComboBox cbo = itemProps.Control as ComboBox;

				if (cbo != null)
				{
					itemProps.Enabled = SelectionInEditableScripture &&
						!ActiveEditingHelper.IsPictureSelected &&
						!(ActiveEditingHelper.IsBackTranslation &&
						itemProps.Name == "tbbParaStylesCombo");

					if (KeyTermsViewIsVisible && !(ActiveView is DraftView))
					{
						itemProps.Enabled = false;
					}

					cbo.Enabled = itemProps.Enabled;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the scripture reference control status.  If there are no books then
		/// disable it.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGotoReferenceCtrl(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					if (itemProps.Control != null)
						itemProps.Control.Enabled =
							ActiveEditingHelper != null && m_scr.ScriptureBooksOS.Count > 0;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the GotoReferenceDialog dialog box and lets the user input a reference to
		/// go to.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateGoToReference(object args)
		{
			CheckDisposed();
			try
			{

				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = ActiveEditingHelper != null && m_scr.ScriptureBooksOS.Count > 0;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the Send Scripture References menu/toolbar item.
		/// </summary>
		/// <param name="args">The args.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateSendReferences(object args)
		{
			CheckDisposed();

			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null && ActiveEditingHelper != null)
				{
					itemProps.Enabled = true;
					itemProps.Checked = TeProjectSettings.SendSyncMessages;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the Footnote properties menu/toolbar item.
		/// </summary>
		/// <param name="args">The args.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateFootnoteProperties(object args)
		{
			CheckDisposed();

			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = (ActiveEditingHelper != null);
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the Scripture properties menu/toolbar item.
		/// </summary>
		/// <param name="args">The args.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateScriptureProperties(object args)
		{
			CheckDisposed();

			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = (ActiveEditingHelper != null);
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the Receive Scripture References menu/toolbar item.
		/// </summary>
		/// <param name="args">The args.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateReceiveReferences(object args)
		{
			CheckDisposed();

			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null && ActiveEditingHelper != null)
				{
					bool fIsCheckingView = (m_selectedView is EditorialChecksViewWrapper ||
						m_selectedView is KeyTermsViewWrapper);

					itemProps.Enabled = !fIsCheckingView;
					itemProps.Checked = (!fIsCheckingView && TeProjectSettings.ReceiveSyncMessages);
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
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
			CheckDisposed();
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					if (ActiveEditingHelper == null || ActiveEditingHelper.CurrentSelection == null)
						return false;
					itemProps.Enabled = (SelectionInEditableScripture &&
						(ActiveEditingHelper.CurrentSelection.GetTextPropId(SelectionHelper.SelLimitType.Anchor) != SimpleRootSite.kTagUserPrompt ||
						ActiveEditingHelper.CurrentSelection.GetTextPropId(SelectionHelper.SelLimitType.End) != SimpleRootSite.kTagUserPrompt));

					m_writingSystemSelector.Enabled = itemProps.Enabled;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Edit/Select All is not a valid command for TE.  This will keep it disabled.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditSelectAll(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = false;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles enabling or disabling the Goto Menu.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoTo(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = (ActiveEditingHelper != null);
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles enabling or disabling the Goto First, Next, Prev, and Last menus.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoToRelativeItem(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;

				if (itemProps != null)
				{
					itemProps.Enabled = m_scr.ScriptureBooksOS.Count > 0;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when updating the enabled status of Go To First: Book submenu item.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoToFirstBook(object args)
		{
			return UpdateGoToSubItems(args as TMItemProperties, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when updating the enabled status of Go To Last: Book submenu item..
		/// </summary>
		/// <param name="args">The args.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoToLastBook(object args)
		{
			return UpdateGoToSubItems(args as TMItemProperties, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when updating the enabled status of Go To Next: Book submenu item..
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoToNextBook(object args)
		{
			return UpdateGoToSubItems(args as TMItemProperties, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when updating the enabled status of Go To Next: Section submenu item..
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoToNextSection(object args)
		{
			return UpdateGoToSubItems(args as TMItemProperties, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when updating the enabled status of Go To Next: Chapter submenu item..
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoToNextChapter(object args)
		{
			return UpdateGoToSubItems(args as TMItemProperties, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when updating the enabled status of Go To Next: Footnote submenu item..
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoToNextFootnote(object args)
		{
			return UpdateGoToSubItems(args as TMItemProperties, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the BackTranslationNextMissingBtFootnoteMkr command by attempting to
		/// navigate to a position in the back translation that closely corresponds to a place
		/// in the verncaular that has a footnote whose marker has not been inserted into the
		/// back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "btDraftEditingHelper is a reference")]
		protected bool OnUpdateBackTranslationNextMissingBtFootnoteMkr(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;

			if (itemProps == null)
				return false;

			itemProps.Update = true;
			itemProps.Enabled = false;
			if (m_bookFilter.BookCount == 0 ||
				ActiveEditingHelper == null ||
				ActiveEditingHelper.CurrentSelection == null)
			{
				return true;
			}

			TeEditingHelper btDraftEditingHelper = ((TheBTSplitWrapper != null &&
				(ActiveView == TheBTSplitWrapper.DraftView || ActiveView == TheBTSplitWrapper.BTDraftView)) ?
				TheBTSplitWrapper.BTDraftView.TeEditingHelper : null);
			itemProps.Enabled = (btDraftEditingHelper != null && btDraftEditingHelper.CurrentSelection != null);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when updating the enabled status of Go To Prev: Book submenu item..
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoToPrevBook(object args)
		{
			return UpdateGoToSubItems(args as TMItemProperties, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when updating the enabled status of Go To Prev: Section submenu item..
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoToPrevSection(object args)
		{
			return UpdateGoToSubItems(args as TMItemProperties, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when updating the enabled status of Go To Prev: Chapter submenu item..
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoToPrevChapter(object args)
		{
			return UpdateGoToSubItems(args as TMItemProperties, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when updating the enabled status of Go To Prev: Footnote submenu item..
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoToPrevFootnote(object args)
		{
			return UpdateGoToSubItems(args as TMItemProperties, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the enabled status of sub items in the GoTo menus.
		/// </summary>
		/// <param name="itemProps">The item props.</param>
		/// <param name="fSelectionRequired">if set to <c>true</c> selection is required in
		/// order for this command to be enabled.</param>
		/// <returns>true if handled</returns>
		/// ------------------------------------------------------------------------------------
		public bool UpdateGoToSubItems(TMItemProperties itemProps, bool fSelectionRequired)
		{
			try
			{
				if (itemProps == null)
					return false;

				itemProps.Update = true;
				if (m_bookFilter.BookCount > 0 && ActiveEditingHelper != null)
				{
					if (fSelectionRequired)
					{
						itemProps.Enabled = ActiveEditingHelper.CurrentSelection != null &&
							ActiveEditingHelper.CurrentSelection.LevelInfo.Length > 0;
					}
					else
						itemProps.Enabled = true;
				}
				else
					itemProps.Enabled = false;

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
		/// Display current book name in "First" menu
		/// </summary>
		/// <param name="args">The menu item about to display</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoToFirstSection(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			return UpdateGoToFirstLast(itemProps, SelectionHelper.SelLimitType.Top);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display current book name in "Last" menu
		/// </summary>
		/// <param name="args">The menu item about to display</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoToLastSection(object args)
		{
			return UpdateGoToFirstLast(args as TMItemProperties, SelectionHelper.SelLimitType.Bottom);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display current book name in "First" menu
		/// </summary>
		/// <param name="args">The menu item about to display</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoToFirstChapter(object args)
		{
			return UpdateGoToFirstLast(args as TMItemProperties, SelectionHelper.SelLimitType.Top);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display current book name in "Last" menu
		/// </summary>
		/// <param name="args">The menu item about to display</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGoToLastChapter(object args)
		{
			return UpdateGoToFirstLast(args as TMItemProperties, SelectionHelper.SelLimitType.Bottom);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display current book name in "First" and "Last" menus
		/// </summary>
		/// <param name="itemProps">The "first" or "Last" menu item</param>
		/// <param name="selLimitType">Determines whether the current book will be based
		/// on the top or the bottom of the current selection</param>
		/// ------------------------------------------------------------------------------------
		private bool UpdateGoToFirstLast(TMItemProperties itemProps,
			SelectionHelper.SelLimitType selLimitType)
		{
			try
			{
				if (itemProps == null || ActiveEditingHelper == null || itemProps.ParentForm != this)
					return false;

				itemProps.Enabled = (m_bookFilter.BookCount > 0 &&
					ActiveEditingHelper.CurrentSelection != null &&
					ActiveEditingHelper.CurrentSelection.LevelInfo.Length > 0);

				string bookName = ActiveEditingHelper.CurrentBook(selLimitType);

				itemProps.Text = string.Format(itemProps.OriginalText,
					(bookName != null) ? bookName : string.Empty);

				itemProps.Update = true;
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
		/// Updates the menu's checkmark status given the state of the note view.
		/// </summary>
		/// <param name="arg">The menu or toolbar item</param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "notesWnd is a reference")]
		protected bool OnUpdateViewNotes(object arg)
		{
			try
			{
				TMItemProperties itemProps = arg as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = true;
					NotesMainWnd notesWnd = ((TeApp)m_app).NotesWindow;
					itemProps.Checked = (notesWnd != null);
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the menu's checkmark status given the state of the footnote view.
		/// </summary>
		/// <param name="arg">The menu or toolbar item</param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateViewFootnotes(object arg)
		{
			try
			{
				TMItemProperties itemProps = arg as TMItemProperties;
				if (itemProps != null)
				{
					ViewWrapper wrapper = null;
					if (TheDraftViewWrapper != null && TheDraftViewWrapper.Visible)
						wrapper = TheDraftViewWrapper;
					else if (TheBTSplitWrapper != null && TheBTSplitWrapper.Visible)
						wrapper = TheBTSplitWrapper;
					else if (TheBTReviewWrapper != null && TheBTReviewWrapper.Visible)
						wrapper = TheBTReviewWrapper;
					else if (TheKeyTermsWrapper != null && TheKeyTermsWrapper.Visible)
						wrapper = KeyTermsDraftView;
					else if (TheEditChecksViewWrapper != null && TheEditChecksViewWrapper.Visible)
						wrapper = EditorialChecksDraftView;

					if (wrapper != null)
					{
						itemProps.Enabled = true;
						itemProps.Checked = wrapper.FootnoteViewShowing;
					}
					else
					{
						itemProps.Enabled = false;
						itemProps.Checked = false;
					}
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the menu's checkmark status given the state of the style pane view.
		/// </summary>
		/// <param name="arg">The menu or toolbar item</param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateViewStylePane(object arg)
		{
			try
			{
				TMItemProperties itemProps = arg as TMItemProperties;
				if (itemProps == null || itemProps.ParentForm != this)
					return false;

				if (ActiveEditingHelper == null)
				{
					itemProps.Enabled = false;
					itemProps.Checked = false;
					itemProps.Update = true;
					return true;
				}

				ViewWrapper selectedView = SelectedView as ViewWrapper;
				if (selectedView != null)
				{
					itemProps.Enabled = true;
					itemProps.Checked = selectedView.StylePaneShowing;
				}
				else
				{
					itemProps.Enabled = false;
					itemProps.Checked = false;
				}

				itemProps.Update = true;
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
		/// Updates the Edit/Header Footer Setup menu item.
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateHeaderFooterSetup(object arg)
		{
			try
			{
				TMItemProperties itemProps = arg as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = (CurrentPublication != null);
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the insert section toolbar button and menu option
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateInsertSection(object args)
		{
			CheckDisposed();

			try
			{
				bool fSectionInsertionAllowed = SelectionInEditableScripture &&
					!ActiveEditingHelper.InBookTitle &&
					!ActiveEditingHelper.IsBackTranslation &&
					!ActiveEditingHelper.IsPictureSelected;

				// If the selection is in intro material, then disable unless the selection is at
				// the end of the last intro section
				if (fSectionInsertionAllowed && ActiveEditingHelper.CurrentStartRef.Verse == 0)
				{
					fSectionInsertionAllowed = ActiveEditingHelper.AtEndOfSection &&
						(ActiveEditingHelper.ScriptureCanImmediatelyFollowCurrentSection);
				}

				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = fSectionInsertionAllowed;
					itemProps.Update = true;
					return true;
				}

			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the insert section toolbar button and menu option
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateInsertIntroSection(object args)
		{
			CheckDisposed();

			try
			{
				bool fSectionInsertionAllowed = SelectionInEditableScripture &&
					!ActiveEditingHelper.IsBackTranslation &&
					!ActiveEditingHelper.IsPictureSelected &&
					(ActiveEditingHelper.CurrentStartRef.Verse == 0 ||
					ActiveEditingHelper.AtBeginningOfFirstScriptureSection);

				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = fSectionInsertionAllowed;
					itemProps.Update = true;
					return true;
				}

			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populate the Insert Note menu with subitems for each type of note the user is
		/// allowed to create.
		/// </summary>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateInsertNoteParent(object args)
		{
			CheckDisposed();

			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps == null || itemProps.ParentForm != this)
					return false;

				itemProps.Update = true;
				itemProps.Enabled = false;

				if (ActiveEditingHelper == null || ActiveEditingHelper.CurrentSelection == null ||
					ActiveEditingHelper.CurrentSelection.LevelInfo.Length == 0)
				{
					return true;
				}

				ScrReference scrRef = ActiveEditingHelper.CurrentStartRef;

				// Don't allow inserting a note when the selection is not
				// in a valid book (e.g. when a picture is selected).
				if (scrRef == null || scrRef.Book <= 0)
					return true;

				bool fSubMenuAdded = false;
				TMAdapter.RemoveMenuSubItems("mnuInsertNote");

				foreach (ICmAnnotationDefn subdfn in m_lp.ScriptureAnnotationDfns)
				{
					if (subdfn.UserCanCreate)
					{
						TMItemProperties noteItemProps = new TMItemProperties();
						noteItemProps.Text = subdfn.Name.UserDefaultWritingSystem.Text;
						noteItemProps.Name = "mnu" + noteItemProps.Text.Replace(" ", string.Empty);
						noteItemProps.Enabled = true;
						noteItemProps.Visible = true;
						noteItemProps.CommandId = "CmdInsertNote";
						noteItemProps.Tag = subdfn;
						TMAdapter.AddMenuItem(noteItemProps, "mnuInsertNote", null);
						fSubMenuAdded = true;
						// ENHANCE: Might need to create cascading submenus ad nauseum if the
						// subdefinitions have further subdefinitions.
					}
				}

				itemProps.Enabled = fSubMenuAdded;
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
		/// Handle the popup of the Insert Book popup menu.  When the submenu pops up, each of
		/// the books in the menu that already exist in the database needs to be disabled.
		/// You can not insert a book that already exists.
		/// </summary>
		/// <param name="args">The Insert Book menu being popped up</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateInsertBook(object args)
		{
			CheckDisposed();

			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps == null || itemProps.ParentForm != this)
					return false;

				itemProps.Update = true;
				itemProps.Enabled = ActiveEditingHelper != null && !ActiveEditingHelper.IsBackTranslation;
				if (!itemProps.Enabled || itemProps.Tag.GetType() != typeof(int))
					return true;

				int bookNum = (int)itemProps.Tag;

				// Disable the menu item for each book that already exists in our Scripture object
				foreach (IScrBook book in m_scr.ScriptureBooksOS)
				{
					if (bookNum == book.CanonicalNum)
					{
						itemProps.Enabled = false;
						return true;
					}
				}

				itemProps.Enabled = true;
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
		/// Called to update the Remove Character Style menu or toolbar item.
		/// </summary>
		/// <param name="args">The menu item</param>
		/// <returns>
		/// 	<c>true</c> if handled, otherwise <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateRemoveCharacterStyle(object args)
		{
			bool fEnabled = (SelectionInEditableScripture &&
				!ActiveEditingHelper.IsPictureSelected &&
				ActiveEditingHelper.GetCharStyleNameFromSelection() != string.Empty);

			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = fEnabled;
					itemProps.Update = true;
					return true;
				}

			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the insert chapter number toolbar button and menu option
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateInsertChapterNumber(object args)
		{
			CheckDisposed();

			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = SelectionInEditableScripture && ActiveEditingHelper.CanInsertChapterNumber;
					itemProps.Update = true;
					return true;
				}

			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles updating the Insert Footnote menu.
		/// Disables the footnote menu command if (1) we are in a view that has no editing
		/// capabilities, (2) there are no books in the book filter, (3) if we are in key terms
		/// view, or (4) if the user has the insertion point in a  footnote.
		/// </summary>
		/// <returns>true if we handled the update message; false if we didn't handle it</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateInsertGeneralFootnote(object args)
		{
			CheckDisposed();

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = InsertGeneralFootnoteAllowed();
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles updating the insert cross reference menu.
		/// </summary>
		/// <returns>true if we handled the update message; false if we didn't handle it</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateInsertCrossRefFootnote(object args)
		{
			CheckDisposed();

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Enabled = InsertCrossRefFootnoteAllowed();
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if a general footnote can be inserted
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool InsertGeneralFootnoteAllowed()
		{
			int tag;
			return (InsertFootnoteAllowed(out tag));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if a cross-reference footnote can be inserted
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool InsertCrossRefFootnoteAllowed()
		{
			int tag;
			return (InsertFootnoteAllowed(out tag) && tag != ScrBookTags.kflidTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if a footnote can be inserted
		/// </summary>
		/// <param name="tagSelectedScrElement">Flid indicating the type of Scripture element
		/// (title, section head, contents, etc. containing the selection)</param>
		/// <returns>true if a footnote can be inserted; false if it cannot</returns>
		/// ------------------------------------------------------------------------------------
		private bool InsertFootnoteAllowed(out int tagSelectedScrElement)
		{
			TeEditingHelper editingHelper = ActiveEditingHelper;
			if (editingHelper == null)
			{
				tagSelectedScrElement = 0;
				return false;
			}

			int hvoSelection;
			bool gotScrElement = editingHelper.GetSelectedScrElement(out tagSelectedScrElement, out hvoSelection);

			SelectionHelper currentSel = editingHelper.CurrentSelection;
			Debug.Assert(currentSel == null || currentSel.Selection != null,
				"We assume that editingHelper.CurrentSelection.Selection is not null.");

			return (gotScrElement &&
				BookFilter != null &&
				currentSel != null &&
				currentSel.Selection.SelType != VwSelType.kstPicture &&
				BookFilter.BookCount > 0 &&
				(!currentSel.IsRange || // Range selection can't cross books
				editingHelper.GetBookIndex(SelectionHelper.SelLimitType.Anchor) == editingHelper.GetBookIndex(SelectionHelper.SelLimitType.End)) &&
				editingHelper.Editable &&
				(editingHelper.TheClientWnd.GetType().Name != "KeyTermsViewWrapper") &&
				!editingHelper.IsPictureSelected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles updating the insert picture menu.
		/// Disables the picture properties menu command if a picture isn't selected.
		/// </summary>
		/// <param name="args"></param>
		/// <returns>true if we handled the update message; false if we didn't handle it</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdatePictureProperties(object args)
		{
			CheckDisposed();

			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					TeEditingHelper helper = ActiveEditingHelper;
					itemProps.Enabled = (helper != null && helper.IsPictureSelected);
					itemProps.Update = true;
					return true;
				}

			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles updating the remove book menu.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateEditRemoveBook(object args)
		{
			CheckDisposed();

			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Update = true;
					itemProps.Enabled = (!DataUpdateMonitor.IsUpdateInProgress() &&
						SelectionInEditableScripture &&
						!ActiveEditingHelper.IsBackTranslation);

					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles updating the Import Standard Format menu.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateImportStandardFormat(object args)
		{
			CheckDisposed();

			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = !DataUpdateMonitor.IsUpdateInProgress();
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles updating the insert picture menu.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateInsertPictureDialog(object args)
		{
			CheckDisposed();

			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Update = true;
					int tagSelection = 0;
					if (ActiveEditingHelper != null)
					{
						int hvoSelection;
						ActiveEditingHelper.GetSelectedScrElement(out tagSelection, out hvoSelection);
						itemProps.Enabled = (SelectionInEditableScripture &&
							tagSelection != 0 &&
							tagSelection != ScrBookTags.kflidFootnotes &&
							!ActiveEditingHelper.IsPictureSelected);
					}
					else
						itemProps.Enabled = false; // no selection

					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="args">The menu item</param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateResetParagraphStyle(object args)
		{
			CheckDisposed();

			try
			{
				// If there are books then allow the option to be enabled.
				bool enableResetParaStyle = SelectionInEditableScripture &&
					!ActiveEditingHelper.IsPictureSelected &&
					!ActiveEditingHelper.IsBackTranslation;

				// If the selection crosses more than one paragraph then disable the option.
				if (enableResetParaStyle)
				{
					SelLevInfo[] anchor =
						ActiveEditingHelper.CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
					SelLevInfo[] end =
						ActiveEditingHelper.CurrentSelection.GetLevelInfo(SelectionHelper.SelLimitType.End);
					enableResetParaStyle = (anchor.Length > 0 && anchor.Length == end.Length && anchor[0].hvo == end[0].hvo);
				}

				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = enableResetParaStyle;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the insert verse number toolbar button and menu option
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateInsertVerseNumber(object args)
		{
			CheckDisposed();

			try
			{
				bool enabled = SelectionInEditableScripture &&
					ActiveEditingHelper.CanInsertNumberInElement;

				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = enabled;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the insert verse numbers mode toolbar button and menu option
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateInsertVerseNumbers(object args)
		{
			CheckDisposed();

			try
			{
				bool haveBooks = BookFilter.BookCount > 0;

				// Determine if the item should be enabled ...
				bool enabled = ActiveEditingHelper != null && haveBooks && ActiveEditingHelper.Editable
					&& ActiveEditingHelper.ContentType != StVc.ContentTypes.kctSegmentBT;

				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					bool insertInProgress =
						ActiveEditingHelper != null && ActiveEditingHelper.InsertVerseActive;

					itemProps.Enabled = enabled;
					itemProps.Checked = (haveBooks && insertInProgress);
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Common code for updating the file/export menu items
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool OnUpdateFileExports(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Visible = true;
					itemProps.Enabled = BookFilter.BookCount > 0;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the File/Export/Usfm Paratext menu command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateFileExportUsfmParatext(object args)
		{
			return OnUpdateFileExports(args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the File/Export/Usfm Toolbox menu command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateFileExportUsfmToolbox(object args)
		{
			return OnUpdateFileExports(args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the File/Export/Xml menu command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateFileExportXml(object args)
		{
			return OnUpdateFileExports(args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the File/Export/Rtf menu command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateFileExportRtf(object args)
		{
			return OnUpdateFileExports(args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the File/Export/Xhtml menu command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateFileExportXhtml(object args)
		{
			if (Options.UseXhtmlExport)
				return OnUpdateFileExports(args);

			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Visible = false;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the File/Export/Pathway menu command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateFileExportPs(object args)
		{
			if (Options.UseXhtmlExport && PathwayUtils.IsPathwayForScrInstalled)
				return OnUpdateFileExports(args);

			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Visible = false;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the book properties menu item should be enabled
		/// </summary>
		/// <param name="args"></param>
		/// <returns>true if the message was handled</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateBookProperties(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = (ActiveEditingHelper != null && BookFilter.BookCount > 0);
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw;// just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the back translation Use Interlinear Text Tool menu item should be
		/// enabled.
		/// </summary>
		/// <param name="args"></param>
		/// <returns>true if the message was handled</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInterlinearize(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					// TODO (TE-9038): This probably needs to change to IsPictureReallySelected when we support interlinearization of picture captions.
					itemProps.Enabled = IsBtViewActive && !ActiveEditingHelper.IsPictureSelected;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the back translation chapter and verse template menu item should be
		/// enabled.
		/// </summary>
		/// <param name="args"></param>
		/// <returns>true if the message was handled</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateInsertBackTransCV(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = SelectionInEditableScripture &&
						ActiveEditingHelper.IsBackTranslation &&
						ActiveEditingHelper.CurrentStartRef.Verse != 0 &&
						!ActiveEditingHelper.IsPictureSelected;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle updating the "Update Page Break" menu/toolbar item.
		/// </summary>
		/// <param name="args">menu/toolbar item</param>
		/// <returns><c>true</c> if we handled it, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateViewUpdatePageBreak(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps == null)
					return false;

				itemProps.Update = true;
				itemProps.Enabled = ActiveView is PublicationControl && m_scr.ScriptureBooksOS.Count > 0;
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
		#endregion

		#region File Menu event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the book properties dialog
		/// </summary>
		/// <param name="args"></param>
		/// <returns>true if the message was handled</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnBookProperties(object args)
		{
			if (ActiveEditingHelper == null)
				return false;

			using (new WaitCursor(this))
			{
				// Find out which book is active.  Also make sure that there is a book to use.
				int index = ActiveEditingHelper.BookIndex;
				if (index == -1)
					index = 0;
				if (index >= BookFilter.BookCount)
					return false;
				IScrBook book = BookFilter.GetBook(index);

				// Show the book properties dialog with an undo/redo action
				string undo;
				string redo;
				TeResourceHelper.MakeUndoRedoLabels("kstidUndoRedoBookProperties", out undo, out redo);
				using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
					Cache.ServiceLocator.GetInstance<IActionHandler>(), undo, redo))
				{
					using (BookPropertiesDialog dlg = new BookPropertiesDialog(book, m_StyleSheet, m_app))
					{
						if (dlg.ShowDialog() == DialogResult.OK)
						{
							RefreshAllViews();
							undoHelper.RollBack = false;
						}
					}
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File/Saved Versions command
		/// </summary>
		/// <param name="args"></param>
		/// <returns>true (means we handled it)</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnSavedVersions(object args)
		{
			using (SavedVersionsDialog dlg = new SavedVersionsDialog(m_cache, StyleSheet,
				DraftViewZoomPercent, FootnoteZoomPercent, m_app, m_app))
			{
				dlg.ShowDialog();
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is called when the user selects File, Import, Standard Format.
		/// Despite of the name it handles both SF and PT imports.
		/// </summary>
		/// <param name="args"></param>
		/// <returns>true (means we handled it)</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnImportStandardFormat(object args)
		{
			TeImportManager.ImportSf(this, this, m_app);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is called when the user selects File, Import, Open XML for Editing Scripture.
		/// </summary>
		/// <param name="args">The args.</param>
		/// <returns>true (means we handled it)</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnImportOXES(object args)
		{
			TeImportManager.ImportXml(this, this, m_app);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File/Export/Usfm Paratext menu command
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="REVIEW: ParatextHelper.GetAssociatedProject returns a reference (?)")]
		protected bool OnFileExportUsfmParatext(object args)
		{
			AdjustScriptureAnnotations();

			ScrText assocProj = ParatextHelper.GetAssociatedProject(Cache.ProjectId);
			if (assocProj != null)
			{
				MessageBox.Show(this, String.Format(TeResourceHelper.GetResourceString("kstidParatextExportNotAvailable"),
					assocProj.JoinedNameAndFullName), FwUtils.ksTeAppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}

			using (ExportPtxDialog dlg = new ExportPtxDialog(m_cache, m_bookFilter,
				m_app.ProjectSpecificSettingsKey, m_app, m_app))
			{
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					using (ExportUsfm exporter = new ExportUsfm(m_cache, m_bookFilter, dlg.OutputSpec,
						m_app, dlg.FileNameFormat))
					{
						exporter.OverwriteWithoutAsking = dlg.OverwriteConfirmed;
						exporter.MarkupSystem = MarkupType.Paratext;
						exporter.ExportScriptureDomain = dlg.ExportScriptureDomain;
						exporter.ExportBackTranslationDomain = dlg.ExportBackTranslationDomain;
						if (exporter.ExportBackTranslationDomain)
							exporter.RequestedAnalysisWss = dlg.RequestedAnalWs;
						exporter.ExportNotesDomain = dlg.ExportNotesDomain;
						exporter.ParatextProjectShortName = dlg.ShortName;
						exporter.ParatextProjectFolder = dlg.ParatextProjectFolder;
						exporter.Run(this);
					}
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File/Export/Usfm Toolbox menu command
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFileExportUsfmToolbox(object args)
		{
			AdjustScriptureAnnotations();
			using (ExportTbxDialog dlg = new ExportTbxDialog(m_cache, m_bookFilter,
				m_app.ProjectSpecificSettingsKey, m_app, m_app))
			{
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					using (ExportUsfm exporter = dlg.ExportSplitByBook ?
						new ExportUsfm(m_cache, m_bookFilter, dlg.OutputSpec, m_app, dlg.FileNameFormat) :
						new ExportUsfm(m_cache, m_bookFilter, dlg.OutputSpec, m_app))
					{
						exporter.MarkupSystem = MarkupType.Toolbox;
						exporter.ExportScriptureDomain = dlg.ExportScriptureDomain;
						exporter.ExportBackTranslationDomain = dlg.ExportBackTranslationDomain;
						exporter.ExportNotesDomain = dlg.ExportNotesDomain;
						exporter.RequestedAnalysisWss = dlg.RequestedAnalWs;
						Application.DoEvents(); // REVIEW (TimS): why is this here?
						exporter.Run(this);
					}
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File/Export/Xml menu command
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFileExportXml(object args)
		{
			AdjustScriptureAnnotations();
			using (ExportXmlDialog dlg = new ExportXmlDialog(m_cache, m_bookFilter, GetReferenceToCurrentBook().Book,
				m_StyleSheet, FileType.OXES, m_app))
			{
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					string filename = Path.GetFileName(dlg.FileName);
					string directory = Path.GetDirectoryName(dlg.FileName);
					// REVIEW (TimS): What does this code accomplish? It seems to just re-create
					// dlg.FileName in parts.
					if (directory.EndsWith(Path.VolumeSeparatorChar.ToString()))
						directory += Path.DirectorySeparatorChar;

					ExportXml export = new ExportXml(Path.Combine(directory, filename), m_cache,
						m_bookFilter, m_app, dlg.ExportWhat, dlg.BookNumber, dlg.FirstSection,
						dlg.LastSection, dlg.Description);
					if (export.Run(this))
					{
						// Validate the XML file. If it doesn't pass validation, display a
						// yellow error message box.
						string strError = Validator.GetAnyValidationErrors(export.FileName);
						if (!String.IsNullOrEmpty(strError))
							throw new ContinuableErrorException(strError);
					}
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check scripture annotations for titles and introductions to make sure they point to
		/// paragraphs in the current version of books. This will ensure that they are included
		/// at the proper location in the export.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AdjustScriptureAnnotations()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => m_scr.AdjustAnnotationReferences());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File/Export/Xhtml menu command
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFileExportXhtml(object args)
		{
			using (ExportXmlDialog dlg = new ExportXmlDialog(m_cache, m_bookFilter, GetReferenceToCurrentBook().Book,
				m_StyleSheet, FileType.XHTML, m_app))
			{
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					// TODO/REVIEW: get the stylesheet overridden for printing.
					ExportXhtml export = new ExportXhtml(dlg.FileName, m_cache, m_bookFilter,
						dlg.ExportWhat, dlg.BookNumber, dlg.FirstSection, dlg.LastSection,
						dlg.Description, m_StyleSheet, CurrentPublication, m_app);
					export.Run(this);
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File/Export/Open Office menu command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnFileExportPs(object args)
		{
			try
			{
				if (!PathwayUtils.IsPathwayForScrInstalled)
				{
					MessageBox.Show(this, ResourceHelper.GetResourceString("kstidInvalidPathwayInstallation"),
						m_app.ApplicationName, MessageBoxButtons.OK);
					return false;
				}

				// Show the Pathway dialog.
				string pathwayDir = PathwayUtils.PathwayInstallDirectory;
				string cssDllPath = Path.Combine(pathwayDir, "CssDialog.dll");
				var dlg = ReflectionHelper.CreateObject(cssDllPath,
					"SIL.PublishingSolution.ScriptureContents", null);
				Debug.Assert(dlg != null, "missing CssDialog.dll from Pathway install");

				ReflectionHelper.SetProperty(dlg, "DatabaseName", m_cache.ProjectId.Name);
				ReflectionHelper.SetProperty(dlg, "PublicationName", CurrentPublication.Name);
				DialogResult result = (DialogResult) ReflectionHelper.GetResult(dlg, "ShowDialog");
				if (result != DialogResult.Cancel)
				{
					// Get the output location for the XHTML file.
					string outputLocationPath = (string) ReflectionHelper.GetProperty(dlg, "OutputLocationPath");
					string fileName = Path.Combine(outputLocationPath, CurrentPublication.Name + ".xhtml");
					if (result == DialogResult.Yes)
					{
						ExportXhtml export = new ExportXhtml(fileName, m_cache, m_bookFilter,
							ExportWhat.FilteredBooks, 1, 1, 1, "Pathway TranslationEditor Export",
							m_StyleSheet, CurrentPublication, m_app);
						export.Run(this);
					}
					else
					{
						string existingLocationPath = (string) ReflectionHelper.GetField(dlg, "ExistingLocationPath");
						File.Copy(existingLocationPath, fileName);
					}

					// Export the XHTML file to the specified format.
					var exporter = ReflectionHelper.CreateObject(Path.Combine(pathwayDir, "PsExport.dll"),
						"SIL.PublishingSolution.PsExport", null);
					Debug.Assert(exporter != null);
					ReflectionHelper.SetProperty(exporter, "DataType", "Scripture");
					ReflectionHelper.CallMethod(exporter, "Export", fileName);
				}
			}
			catch
			{
				MessageBox.Show(this, ResourceHelper.GetResourceString("kstidInvalidPathwayInstallation"),
						m_app.ApplicationName, MessageBoxButtons.OK);
				return false;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the File/Export/Rtf menu command
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFileExportRtf(object args)
		{
			using (ExportRtfDialog dlg = new ExportRtfDialog(m_cache, m_app))
			{
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					using (new WaitCursor(this))
					{
						int ws = 0;

						// Determine the type of export to do
						ExportContent exportContent = ExportContent.Scripture;

						if (KeyTermsViewIsVisible)
						{
							exportContent = ExportContent.KeyTermRenderings;
						}
						else if (TheBTSplitWrapper != null && TheBTSplitWrapper.Visible)
						{
							exportContent = ExportContent.BackTranslation;
							ws = ((DraftView)GetNamedView(kBackTransView)).ViewConstructorWS;
						}
						else if (TheBTReviewWrapper != null && TheBTReviewWrapper.Visible)
						{
							exportContent = ExportContent.BackTranslation;
							ws = ((DraftView)GetNamedView(kstidBTReviewView)).ViewConstructorWS;
						}
						else if (ActiveEditingHelper != null &&
							(ActiveEditingHelper.ViewType & (TeViewType.BackTranslation | TeViewType.Print))
							== (TeViewType.BackTranslation | TeViewType.Print))
						{
							exportContent = ExportContent.BackTranslation;
							ws = ((ScripturePublication)GetNamedView(TeEditingHelper.ViewTypeString(
								ActiveEditingHelper.ViewType))).BackTranslationWS;
						}

						// Do the export
						ExportRtf export = new ExportRtf(dlg.FileName, m_cache, m_bookFilter,
							exportContent, ws, StyleSheet, m_app);
						if (export.Run(this))
						{
							// No exceptions thrown during export.
							// If the user specified they wanted to automatically open the file...
							if (dlg.AutoOpenFile)
							{
								// attempt to open it.
								using (Process openDocument = new Process())
								{
									openDocument.StartInfo.FileName = dlg.FileName;
									try
									{
										openDocument.Start();
									}
									catch (Exception)
									{
										// Unable to open document with associated application.
										// Bring up an "Open with..." dialog box to specify alternate application.
										// TODO-Linux: this doesn't look like it would work on Linux
										openDocument.StartInfo.FileName = "rundll32.exe";
										openDocument.StartInfo.Arguments =
											string.Format("shell32.dll, OpenAs_RunDLL \"{0}\"", dlg.FileName);
										openDocument.StartInfo.UseShellExecute = true;
										openDocument.StartInfo.ErrorDialog = true;
										openDocument.Start();
									}
								}
							}
						}
					}
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prints the current view
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override bool OnFilePrint(object args)
		{
			IPublicationView pubView = CurrentPublicationView;
			if (pubView == null)
			{
				// We're in a draft view and the corresponding print layout view hasn't been
				// created yet. So do it now.
				using (new WaitCursor(this))
				{
					// if the active view is not a publication then find the one that is assigned
					// to the current view.
					ISelectableViewFactory viewFactory;
					if (m_uncreatedViews.TryGetValue(ActiveEditingHelper.CorrespondingPrintableViewType,
						out viewFactory))
					{
						viewFactory.Create(null);
					}
				}
			}

			return base.OnFilePrint(args);
		}
		#endregion

		#region View Menu event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the configure event for any menu associated with the sidebar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnSideBarConfigure(object arg)
		{
			IBtAwareView btView = SIBAdapter.CurrentTabItemProperties.Tag as IBtAwareView;

			if (btView == null || btView.BackTranslationWS <= 0)
				throw new InvalidOperationException("Cannot call OnSideBarConfigure unless the active view is a back translation view.");

			using (BackTransLanguageDialog dlg = new BackTransLanguageDialog(m_cache,
				btView.BackTranslationWS, m_app))
			{
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					if (dlg.ChangeAllBtWs)
						ChangeDefaultBTWritingSystem(dlg.SelectedWS);
					else
						btView.BackTranslationWS = dlg.SelectedWS;
				}
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the "no filter" menu
		/// </summary>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		protected bool OnNoFilters(object args)
		{
			if (DataUpdateMonitor.IsUpdateInProgress())
				return true; //discard this event

			using (new WaitCursor(this))
			using (new DataUpdateMonitor(this, "OnNoFilter"))
			{
				// turn off an active filter
				UpdateBookFilter(m_scr.ScriptureBooksOS.ToArray());

				//set the focus back to the active view
				if (ActiveView != null && ActiveView is Control)
					((Control)ActiveView).Focus();
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the "book filter" button
		/// </summary>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		protected bool OnBookFilterToggle(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;

			if (itemProps != null)
			{
				using (new WaitCursor(this))
				{
					// If there is already a filter in effect, turn it off.
					if (BookFilterInEffect)
					{
						TurnOffAllFilters();
						itemProps.Checked = false;
					}
					else
					{
						// If there was no previous filter, show the dialog asking for a new filter.
						if (m_bookFilter.SavedFilter.Length == 0)
							OnBookFilter(null);
						else
						{
							// If there was a previous filter, then use it.
							UpdateBookFilter(m_bookFilter.SavedFilter);
							itemProps.Checked = true;
						}
					}
					itemProps.Update = true;
					return true;
				}
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle a click on the book filter menu
		/// </summary>
		/// <param name="args"></param>
		/// ------------------------------------------------------------------------------------
		protected bool OnBookFilter(object args)
		{
			if (DataUpdateMonitor.IsUpdateInProgress())
				return true; //discard this event

			// we don't want to show the dialog if there is only one book to select
			if (m_scr.ScriptureBooksOS.Count < 2)
			{
				TurnOffAllFilters();
				return true; // discard this event
			}

			using (new WaitCursor(this))
			using (FilterBookDialog dlg = new FilterBookDialog(m_cache, m_bookFilter.SavedFilter, m_app))
			{
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					IScrBook[] filteredBooks = dlg.GetListOfIncludedTexts();
					UpdateBookFilter(filteredBooks);
				}
			}

			// If all the books are now in the filter, then turn off any filters.
			if (m_bookFilter.AllBooks)
				TurnOffAllFilters();

			//set the focus back to the active view
			if (ActiveView != null && ActiveView is Control)
				((Control)ActiveView).Focus();

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toggles the visibility of the footnote view.
		/// </summary>
		/// <param name="arg">The menu or toolbar item</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnViewFootnotes(object arg)
		{
			using (new WaitCursor(this))
			{
				if (TheDraftViewWrapper != null && TheDraftViewWrapper.Visible)
				{
					// show the footnotes (the secondary view) in the Draft view
					if (TheDraftViewWrapper.FootnoteViewShowing)
						TheDraftViewWrapper.HideFootnoteView();
					else
					{
						TheDraftViewWrapper.ShowFootnoteView(null);
						bool fFoundFootnote = false;
						foreach (IScrBook book in BookFilter)
						{
							if (book.FootnotesOS.Count > 0)
							{
								fFoundFootnote = true;
								break;
							}
						}
						TheDraftView.ScrollSelectionIntoView(null, VwScrollSelOpts.kssoDefault);
						if (fFoundFootnote)
							TheDraftView.SynchFootnoteView();
						else
							TheDraftViewWrapper.Focus();
					}
					return true;
				}

				if (TheBTSplitWrapper != null && TheBTSplitWrapper.Visible)
				{
					// show the footnotes (the bottom view) in the split BT view
					TheBTSplitWrapper.ShowFootnoteView(null);
					return true;
				}

				if (TheBTReviewWrapper != null && TheBTReviewWrapper.Visible)
				{
					// show the footnotes (the bottom view) in the BT review view
					TheBTReviewWrapper.ShowFootnoteView(null); // this toggles the footnote view
					return true;
				}

				if (TheKeyTermsWrapper != null && TheKeyTermsWrapper.Visible)
				{
					// show the footnotes (the bottom view) in the key terms view
					// this toggles the footnote view
					KeyTermsDraftView.ShowFootnoteView(null);
					return true;
				}

				if (TheEditChecksViewWrapper != null && TheEditChecksViewWrapper.Visible)
				{
					// show the footnotes (the bottom view) in the editorial checks view
					// this toggles the footnote view
					EditorialChecksDraftView.ShowFootnoteView(null);
					return true;
				}

				Debug.Assert(false, "Are we missing a Wrapper type in OnViewFootnotes?");
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toggles the visibility of the notes view.
		/// </summary>
		/// <param name="arg">The menu or toolbar item</param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnViewNotes(object arg)
		{
			NotesMainWnd notesWnd = ((TeApp)m_app).NotesWindow;

			if (notesWnd == null)
			{
				try
				{
					// Make sure the user can't close the main window when bringing up the
					// notes window (TE-8561)
					m_app.EnableMainWindows(false);

					m_syncHandler.IgnoreAnySyncMessages = true;
					notesWnd = new NotesMainWnd(m_app, m_StyleSheet, DraftViewZoomPercent);
					m_app.InitAndShowMainWindow(notesWnd, null);
					if (!notesWnd.OnFinishedInit())
						Debug.Fail("Notes window did not initialize properly");
				}
				finally
				{
					m_app.EnableMainWindows(true);
					m_syncHandler.IgnoreAnySyncMessages = false;
				}

				((TeApp)m_app).NotesWindow = notesWnd;
				RespondToSyncScrollingMsgs(true);
				notesWnd.SyncHandler = m_syncHandler;
				if (ActiveEditingHelper != null)
					notesWnd.ScrollToScrEditingLocation(this, ActiveEditingHelper);
			}
			else if (arg != null)
			{
				// The user chose the View/Notes menu option, which is a toggle,
				// so we close the existing window.
				notesWnd.Close();
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Brings up the header footer setup dialog from the View menu
		/// </summary>
		/// <param name="arg"></param>
		/// <returns><c>true</c> if handled, otherwise <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnHeaderFooterSetup(object arg)
		{
			if (CurrentPublication == null)
				return false;

			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoRedoHeaderFooterSetup", out undo,
				out redo);
			using (UndoTaskHelper undoTaskHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor,
					  ActiveView.CastAsIVwRootSite(), undo, redo))
			{
				Dictionary<IPubDivision, DivInfo> divHfSets = new Dictionary<IPubDivision, DivInfo>();
				foreach (IPubDivision div in CurrentPublication.DivisionsOS)
					divHfSets[div] = new DivInfo(div);

				// Save the names and hvos of the HF sets before going into the dialog.
				// In case the user changes the name of one or more sets, we need to
				// be able to get back to the original names.
				Dictionary<string, int> origHfSets = new Dictionary<string, int>();
				foreach (IPubHFSet hfs in m_scr.HeaderFooterSetsOC)
					origHfSets[hfs.Name] = hfs.Hvo;

				// get default writing system. Use the default vernacular writing system
				// if the ScripturePublication is null.
				using (HeaderFooterSetupDlg dlg = new HeaderFooterSetupDlg(m_cache, CurrentPublication,
					m_app, TePublicationsInit.FactoryHeaderFooterSets, m_scr))
				{
					dlg.ShowDialog();

					// If the H/F set for any of the divisions of the current publication has changed
					// refresh its view
					foreach (IPubDivision div in CurrentPublication.DivisionsOS)
					{
						if (divHfSets[div] != new DivInfo(div))
						{
							ActiveView.RefreshDisplay();
							break;
						}
					}

					// Need to refresh the views for any other publications that are
					// using an H/F set that was modified
					foreach (ISelectableView view in m_rgClientViews.Values)
					{
						PublicationControl pubCtrl = view as PublicationControl;
						if (pubCtrl != null && pubCtrl != ActiveView)
						{
							foreach (IPubDivision div in pubCtrl.Publication.DivisionsOS)
							{
								// Lookup the hvo for the changed HF set based on the
								// original name, which is still stored in the division.
								int hfHvo;
								if (origHfSets.TryGetValue(div.HFSetOA.Name, out hfHvo) &&
									dlg.HFSetWasModified(hfHvo))
								{
									IPubHFSet scrHFset =
										m_cache.ServiceLocator.GetInstance<IPubHFSetRepository>().GetObject(hfHvo);
									div.HFSetOA.CloneDetails(scrHFset);
								}
							}
						}
					}
				}
				undoTaskHelper.RollBack = false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the scripture properties dialog from the File menu
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnScriptureProperties(object args)
		{
			if (DisplayScripturePropertiesDlg(false, true) == DialogResult.OK)
				m_app.RefreshAllViews();
			return true;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the scripture properties dialog from the chapter verse check dialog.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnScripturePropertiesForCVCheck(object args)
		{
			if (DisplayScripturePropertiesDlg(false, true) == DialogResult.OK)
				m_app.RefreshAllViews();
			return true;
		}

		#endregion

		#region Insert menu handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For the Insert Section menu, if IP at the end of a section, inserts a new section.
		/// Sets the IP in the new para of the new section.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if we handle this.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnInsertSection(object args)
		{
			if (ActiveEditingHelper == null)
				return false;

			string undo, redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidInsertSection", out undo, out redo);
			using (UndoTaskHelper undoHelper = new UndoTaskHelper(ActiveView.CastAsIVwRootSite(), undo, redo))
			using (new DataUpdateMonitor(this, "Insert Section"))
			{
				ActiveEditingHelper.CreateSection(false);
				undoHelper.RollBack = false;
				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For the Insert Section menu, if IP at the end of a section, inserts a new section.
		/// Sets the IP in the new para of the new section.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if we handle this.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnInsertIntroSection(object args)
		{
			if (ActiveEditingHelper == null)
				return false;
			string undo, redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidInsertSection", out undo, out redo);
			using (UndoTaskHelper undoHelper = new UndoTaskHelper(ActiveView.CastAsIVwRootSite(), undo, redo))
			using (new DataUpdateMonitor(this, "Insert Section"))
			{
				ActiveEditingHelper.CreateSection(true);
				undoHelper.RollBack = false;
				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For the Insert-Book menu, handle the click on the book in the book list. Create the
		/// requested book and title.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if we handle this</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnInsertBook(object args)
		{
			if (ActiveEditingHelper == null)
				return false;

			if (DataUpdateMonitor.IsUpdateInProgress())
				return true; //discard this event

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null && itemProps.Tag.GetType() == typeof(int))
			{
				if (ActiveView != null && ActiveView is Control)
					((Control)ActiveView).Focus();

				string undo, redo;
				TeResourceHelper.MakeUndoRedoLabels("kstidInsertBook", out undo, out redo);

				using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
					m_cache.ServiceLocator.GetInstance<IActionHandler>(), undo, redo))
				{
					ActiveEditingHelper.InsertBook((int)itemProps.Tag);
					undoHelper.RollBack = false;
				}
				return true;
			}

			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Insert a new chapter number
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if we handle this</returns>
		/// -----------------------------------------------------------------------------------
		protected bool OnInsertChapterNumber(object args)
		{
			if (ActiveEditingHelper == null)
				return false;

			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidInsertChapterNumber", out undo, out redo);

			// REVIEW: Do we need to also use a DataUpdateMonitor in the new FDO?
			using (UndoTaskHelper undoHelper = new UndoTaskHelper(ActiveView.CastAsIVwRootSite(), undo, redo))
			{
				if (!DataUpdateMonitor.IsUpdateInProgress())
					ActiveEditingHelper.InsertChapterNumber();

				undoHelper.RollBack = false;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new annotation and displays (or gives focus to) the Annotation window
		/// </summary>
		/// <param name="args">the menu item that was clicked</param>
		/// <returns><c>true</c> if we handle this</returns>
		/// <remarks>Called from TeMainWnd when the user clicks one of the submenu items of the
		/// Insert Note menu
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "notesWnd is a reference")]
		protected bool OnInsertNote(object args)
		{
			if (ActiveEditingHelper == null || ActiveEditingHelper.CurrentSelection == null)
				return false;

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null)
				return false;

			ICmAnnotationDefn type = itemProps.Tag as ICmAnnotationDefn;
			if (type == null)
				return false;
			// make sure the notes window is showing
			OnViewNotes(null);

			using (new IgnoreSynchMessages(this))
			{
				// Get information from the selection about the location of the annotation.
				ICmObject topObj, bottomObj;
				int wsSelector;
				int startOffset, endOffset;
				ITsString tssQuote;
				BCVRef startRef, endRef;

				ActiveEditingHelper.GetAnnotationLocationInfo(out topObj, out bottomObj, out wsSelector,
					out startOffset, out endOffset, out tssQuote, out startRef, out endRef);

				// If the selection is in a user prompt, then tssQuote will contain the text
				// found in the prompt. In that case, make sure the cited text contains nothing.
				if (ActiveEditingHelper.IsSelectionInUserPrompt && tssQuote != null && tssQuote.Length > 0)
				{
					// Remove the user prompt property from the TsString and set the writing system to
					// the vernacular.
					ITsPropsBldr ttpBldr = tssQuote.get_Properties(0).GetBldr();
					ttpBldr.SetIntPropValues(SimpleRootSite.ktptUserPrompt, -1, -1);
					ttpBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, -1, -1);
					ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
						(int)FwTextPropVar.ktpvDefault, ActiveEditingHelper.ViewConstructorWS);

					ITsStrBldr bldr = tssQuote.GetBldr();
					bldr.Replace(0, tssQuote.Length, string.Empty, ttpBldr.GetTextProps());

					tssQuote = bldr.GetString();
				}

				string sUndo, sRedo;
				TeResourceHelper.MakeUndoRedoLabels("kstidInsertAnnotation", out sUndo, out sRedo);
				string sType = type.Name.UserDefaultWritingSystem.Text;
				sUndo = string.Format(sUndo, sType);
				sRedo = string.Format(sRedo, sType);
				// Activate the notes window - this is required so that insert of the note will be done on the
				// notes window undo stack
				NotesMainWnd notesWnd = ((TeApp)m_app).NotesWindow;
				notesWnd.Activate();
				NotesEditingHelper notesEditingHelper = (NotesEditingHelper)notesWnd.EditingHelper;
				using (UndoTaskHelper undoHelper = new UndoTaskHelper(Cache.ServiceLocator.GetInstance<IActionHandler>(),
					notesEditingHelper.EditedRootBox.Site, sUndo, sRedo))
				{
					// let the notes editing helper create the note
					notesEditingHelper.InsertNote(type, startRef, endRef, topObj, bottomObj,
						startOffset, endOffset, tssQuote);

					undoHelper.RollBack = false;
				}

				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a general footnote.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if we handle this</returns>
		/// <remarks>Called when the user clicks InsertFootnote on the main menu, as well as
		/// from DraftView when the user clicks InsertFootnote on the context menu.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected bool OnInsertGeneralFootnote(object args)
		{
			if (InsertGeneralFootnoteAllowed())
				InsertFootnoteInternal(ScrStyleNames.NormalFootnoteParagraph);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a cross-reference footnote.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if we handle this</returns>
		/// <remarks>Called when the user clicks Insert Cross Reference on the main menu.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected bool OnInsertCrossRefFootnote(object args)
		{
			if (InsertCrossRefFootnoteAllowed())
				InsertFootnoteInternal(ScrStyleNames.CrossRefFootnoteParagraph);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ball rolling to insert a footnote.
		/// </summary>
		/// <param name="styleName">style name for the footnote paragraph</param>
		/// <returns>True if a footnote was inserted, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		protected bool InsertFootnoteInternal(string styleName)
		{
			using (new WaitCursor(this))
			{
				Debug.Assert(ActiveEditingHelper != null,
					"We assume that ActiveEditingHelper must be assigned if we are inserting a footnote.");

				int ihvoFootnote;
				IStFootnote footnote;
				string undo;
				string redo;
				TeResourceHelper.MakeUndoRedoLabels((styleName == ScrStyleNames.CrossRefFootnoteParagraph) ?
					"kstidInsertCrossReference" : "kstidInsertFootnote", out undo, out redo);
				using (UndoTaskHelper undoHelper = new UndoTaskHelper(ActiveView.CastAsIVwRootSite(),
					undo, redo))
				{
					footnote = ActiveEditingHelper.InsertFootnote(styleName, out ihvoFootnote);
					undoHelper.RollBack = false;
				}

				if (footnote == null || ihvoFootnote < 0)
					return false;

				// Bring the new footnote into view and set the focus there so the user
				// can start typing in the text. (TE-836)
				if (TheDraftViewWrapper != null && TheDraftViewWrapper.Visible)
					TheDraftViewWrapper.ShowOrHideFootnoteView(footnote, true);
				else if (TheBTSplitWrapper != null && TheBTSplitWrapper.Visible)
					TheBTSplitWrapper.ShowOrHideFootnoteView(footnote, true);
				else if (TheKeyTermsWrapper != null && TheKeyTermsWrapper.Visible)
					KeyTermsDraftView.ShowFootnoteView(footnote);
				else if (TheEditChecksViewWrapper != null && TheEditChecksViewWrapper.Visible)
					EditorialChecksDraftView.ShowFootnoteView(footnote);
				else if ((ActiveEditingHelper.ViewType & (TeViewType.Scripture | TeViewType.Print)) ==
					(TeViewType.Scripture | TeViewType.Print))
				{
					// Workaround for TE-5557
					//// TODO: Handle BT print layout view
					//SelectionHelper footnoteHelper = new SelectionHelper();
					//footnoteHelper.TextPropId = StTxtParaTags.kflidContents;
					//footnoteHelper.NumberOfLevels = 3;

					//footnoteHelper.LevelInfo[2] = bookInfo;

					//footnoteHelper.LevelInfo[1].tag = (int)ScrBook.ScrBookTags.kflidFootnotes;
					//footnoteHelper.LevelInfo[1].hvo = footnote.Hvo;
					//footnoteHelper.LevelInfo[1].ihvo = ihvoFootnote;

					//footnoteHelper.LevelInfo[0].tag = (int)StTxtPara.StTxtParaTags.kflidContents;
					//footnoteHelper.LevelInfo[0].ihvo = 0;
					//footnoteHelper.LevelInfo[0].hvo = footnote.ParagraphsOS[0].Hvo;

					//// set the selection into the footnote of the printlayout view

					//// TODO (TimS/DaveE): need to get the rootsite of the footnote section of the
					//// printlayout view to make this work.
					//footnoteHelper.SetSelection(ActiveView.CastAsIVwRootSite());
				}
			}
			return true;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method implements the ReviewMisspelledWords menu item. It is called using
		/// reflection by xCore, not directly. See TeTMDefinition.xml, look for
		/// CmdReviewMisspelledWords. It launches Flex (if necessary) and configues it to show
		/// the Bulk Edit Wordforms view filtered for words where spelling status is Undecided
		/// and occurrences in corpus is not zero.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnReviewMisspelledWords(object args)
		{
			FwAppArgs link = new FwAppArgs(FwUtils.ksFlexAppName, m_cache.ProjectId.Handle,
				m_cache.ProjectId.ServerName, "toolBulkEditWordforms", Guid.Empty);
			List<Property> additionalProps = link.PropertyTableEntries;
			additionalProps.Add(new Property("SuspendLoadListUntilOnChangeFilter", link.ToolName));
			additionalProps.Add(new Property("LinkSetupInfo", "TeReviewUndecidedSpelling"));
			// Enhance JohnT: if a book filter is active, replace "all" with something appropriate.
			// (Will also need to enhance BrowseViewer.SetupLinkScripture.)
			additionalProps.Add(new Property("LinkScriptureBooksWanted", "all"));
			m_app.FwManager.HandleLinkRequest(link);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method implements the CorrectMisspelledWords menu item. It is called using
		/// reflection by xCore, not directly. See TeTMDefinition.xml, look for
		/// CmdCorrectMisspelledWords. It launches Flex (if necessary) and configues it to show
		/// the Analyses view filtered for words where spelling status is Incorrect
		/// and occurrences in corpus is not zero.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnCorrectMisspelledWords(object args)
		{
			FwAppArgs link = new FwAppArgs(FwUtils.ksFlexAppName, m_cache.ProjectId.Handle,
				m_cache.ProjectId.ServerName, "Analyses", Guid.Empty);
			List<Property> additionalProps = link.PropertyTableEntries;
			additionalProps.Add(new Property("SuspendLoadListUntilOnChangeFilter", link.ToolName));
			additionalProps.Add(new Property("LinkSetupInfo", "TeCorrectSpelling"));
			// Enhance JohnT: if a book filter is active, replace "all" with something appropriate.
			// (Will also need to enhance BrowseViewer.SetupLinkScripture.)
			additionalProps.Add(new Property("LinkScriptureBooksWanted", "all"));
			m_app.FwManager.HandleLinkRequest(link);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method implements the Interlinearize menu item (in the Back Translation section). It is called using
		/// reflection by xCore, not directly. See TeTMDefinition.xml, look for
		/// CmdInterlinearize. It launches Flex (if necessary) and configues it to show
		/// the the Interlinear Text view with the current StText selected.
		/// </summary>
		/// <param name="args">The args.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnInterlinearize(object args)
		{
			if (ActiveEditingHelper == null || ActiveEditingHelper.CurrentSelection == null)
				return true;
			IStText text = null;
			foreach (SelLevInfo info in ActiveEditingHelper.CurrentSelection.LevelInfo)
			{
				// Review TE Team (JohnT): should we allow StFoonote also?
				// If so may take extra work to ensure FLEx can interlinearize them.
				if (m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetClsid(info.hvo) ==
					StTextTags.kClassId)
				{
					text = m_cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(info.hvo);
					break;
				}
			}
			if (text == null)
				return true;
			FwAppArgs link = new FwAppArgs(FwUtils.ksFlexAppName, m_cache.ProjectId.Handle,
				m_cache.ProjectId.ServerName, "interlinearEdit", text.Guid);
			m_app.FwManager.HandleLinkRequest(link);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handler to update the menu item for translating UNS questions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateUnsQuestions(object args)
		{
			CheckDisposed();
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null)
			{
				itemProps.Visible = Options.ShowTranslateUnsQuestions;
				itemProps.Update = true;
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method implements the Translate UNS Questions menu item. It is called using
		/// reflection by xCore, not directly. See TeTMDefinition.xml, look for
		/// CmdUnsQuestions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUnsQuestions(object args)
		{
			if (m_transceleratorWindow != null)
			{
				m_transceleratorWindow.Activate();
				return true;
			}

			using (new WaitCursor(this))
			{
				List<IKeyTerm> keyTerms = new List<IKeyTerm>();
				foreach (ICmPossibility keyTerm in Cache.LanguageProject.KeyTermsList.PossibilitiesOS)
					AddKtLeafNodes(keyTerms, keyTerm);

				WritingSystem vernWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
				WritingSystem defaultWs = Cache.ServiceLocator.WritingSystemManager.UserWritingSystem;

				ComprehensionCheckingSettings ccSettings;
				try
				{
					using (RegistryKey key = m_app.ProjectSpecificSettingsKey.OpenSubKey(kComprehensionCheckingToolSubKey))
					{
						ccSettings = ComprehensionCheckingSettings.LoadFromString(
							(string)key.GetValue(kCCSettings, string.Empty));
					}
					if (string.IsNullOrEmpty(ccSettings.QuestionsFile))
						ccSettings.QuestionsFile = Path.Combine(FwDirectoryFinder.TeFolder, "QTTallBooks.sfm");
				}
				catch
				{
					ccSettings = new ComprehensionCheckingSettings(Path.Combine(FwDirectoryFinder.TeFolder, "QTTallBooks.sfm"));
				}
				ScrReference start, end;
				m_bookFilter.GetRefRangeForContiguousBooks(out start, out end);

				m_transceleratorWindow = new UNSQuestionsDialog(Cache.ProjectId.Name, keyTerms,
					m_StyleSheet.GetUiFontForWritingSystem(Cache.DefaultVernWs, 0), vernWs.IcuLocale,
					vernWs.RightToLeftScript, Path.Combine(ScrTextCollection.SettingsDirectory ?? @"c:\My Paratext Projects", "cms"), ccSettings,
					App.ApplicationName, start, end,
					vern => ((vern ? vernWs : defaultWs)).LocalKeyboard.Activate(),
					() => ShowHelp.ShowHelpTopic(m_app, "khtpNoHelpTopic"),
					LookupTerm); // TODO: Come up with a Help topic

				m_transceleratorWindow.GetAvailableBooks = () => m_bookFilter.BookIds;
				m_transceleratorWindow.Closed += SaveComprehensionCheckingSettings;
			}
			m_transceleratorWindow.Show();

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the Transcelerator settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void SaveComprehensionCheckingSettings(object sender, EventArgs e)
		{
			Debug.Assert(sender == m_transceleratorWindow);
			try
			{
				using (RegistryKey key = m_app.ProjectSpecificSettingsKey.CreateSubKey(kComprehensionCheckingToolSubKey))
				{
					key.SetValue(kCCSettings, m_transceleratorWindow.Settings.ToString());
				}
			}
			catch (Exception error)
			{
				Logger.WriteError(error);
			}
			m_transceleratorWindow = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the kt leaf nodes.
		/// </summary>
		/// <param name="keyTerms">The key terms.</param>
		/// <param name="keyTerm">The key term.</param>
		/// ------------------------------------------------------------------------------------
		private void AddKtLeafNodes(List<IKeyTerm> keyTerms, ICmPossibility keyTerm)
		{
			IFdoOwningSequence<ICmPossibility> subKeyTerms = keyTerm.SubPossibilitiesOS;
			if (subKeyTerms.Count > 0)
			{
				foreach (ICmPossibility subKeyTerm in subKeyTerms)
					AddKtLeafNodes(keyTerms, subKeyTerm);
			}
			else
			{
				IKeyTerm kt = (IKeyTerm)keyTerm;
				string englishTerm = kt.Term;
				if (englishTerm != null)
					keyTerms.Add(kt);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to look up and select one of the given terms, if possible, selecting one
		/// that has an occurrence in the given reference range.
		/// </summary>
		/// <param name="terms">One or more (more-or-less related) terms.</param>
		/// <param name="startRef">The start reference (a BBCCCVVV integer).</param>
		/// <param name="endRef">The end reference (a BBCCCVVV integer).</param>
		/// ------------------------------------------------------------------------------------
		private void LookupTerm(IEnumerable<IKeyTerm> terms, int startRef, int endRef)
		{
			if (!KeyTermsViewIsCreated)
				m_sibAdapter.SetCurrentTabItem(kChkSBTabName, kChkKeyTermsSBItemName, true);

			if (TheKeyTermsWrapper.SelectTerm(terms.Cast<IChkTerm>(), startRef, endRef))
			{
				if (!KeyTermsViewIsVisible)
					m_sibAdapter.SetCurrentTabItem(kChkSBTabName, kChkKeyTermsSBItemName, true);
				Activate();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method handles the case when the user has chosen one of the suggested
		/// spellings for a misspelled word on which they right-clicked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnSpellingSuggestionChosen(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null)
				return false;

			SpellCorrectMenuItem dictItem = itemProps.Tag as SpellCorrectMenuItem;
			if (dictItem == null)
				return false;

			dictItem.DoIt();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Turn on or off vernacular spelling.
		/// </summary>
		/// <param name="args"></param>
		/// <returns>true if handled.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnShowSpellingErrors(object args)
		{
			if (TeProjectSettings.ShowSpellingErrors)
				TeProjectSettings.ShowSpellingErrors = false;
			else
			{
				TeProjectSettings.ShowSpellingErrors = true;
				// Make sure that the spelling dictionary is up-to-date.
				// Note that this will currently turn vernacular spelling on for FLEx, too.
				WritingSystem wsObj = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
				if (string.IsNullOrEmpty(wsObj.SpellCheckingID) || wsObj.SpellCheckingID == "<None>")
					wsObj.SpellCheckingID = wsObj.ID.Replace('-', '_');
				using (new WaitCursor(this))
					WfiWordformServices.ConformSpellingDictToWordforms(m_cache);
			}

			// update all the views and their helpers.
			UpdateSpellingStatus(TeProjectSettings.ShowSpellingErrors);

			RefreshAllViews();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the spelling status of all embedded simple views.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void UpdateSpellingStatus(bool status)
		{
			foreach (IRootSite site in m_viewHelper.Views)
			{
				if (site != null && site is RootSite)
					((RootSite)site).DoSpellCheck = status;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the item checked if we have a dictionary.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateShowSpellingErrors(object args)
		{
			try
			{
				TMItemProperties itemProps = args as TMItemProperties;

				if (itemProps != null)
				{
					itemProps.Checked = false;
					itemProps.Checked = TeProjectSettings.ShowSpellingErrors;

					//itemProps.Checked = SpellingHelper.DictionaryExists(m_cache.DefaultVernWs, m_cache.LanguageWritingSystemFactoryAccessor);
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to add a word to spelling dictionary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnAddToDictionary(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null || !(itemProps.Tag is AddToDictMenuItem))
				return false;

			((AddToDictMenuItem)itemProps.Tag).AddWordToDictionary();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the Add to Spelling Dictionary command in the context
		/// menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateAddToDictionary(object args)
		{
			return UpdateSpellingMenus(args as TMItemProperties);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the Change Multiple Occurrences command in the context
		/// menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateChangeMultipleOccurrences(object args)
		{
			return UpdateSpellingMenus(args as TMItemProperties);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper method to determine if the spelling menu items should be visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool UpdateSpellingMenus(TMItemProperties itemProps)
		{
			if (itemProps == null || EditingHelper == null)
				return false;

			itemProps.Update = true;
			// Strictly itemProps must be an AddToDictMenuItem for AddToDictionary.
			// For ChangeMultipleOccurrences, we just set it to something to indicate that the
			// command should be enabled.
			itemProps.Enabled = (itemProps.Tag != null);
			itemProps.Visible =
				ActiveEditingHelper.SpellCheckingStatus == RootSiteEditingHelper.SpellCheckStatus.Enabled;

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change Spelling of Multiple Occurences.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnChangeMultipleOccurrences(object args)
		{
			IVwRootSite rootsite = ActiveView.CastAsIVwRootSite();
			if (rootsite == null || rootsite.RootBox == null)
				return false;

			// The following code roughly exactly matches LexText/Interlinear/RawTextPane.OnLexiconLookup.
			// They should probably stay in sync.
			IVwSelection sel = rootsite.RootBox.Selection;
			if (sel == null)
				return false;
			sel = sel.EndPoint(false);
			if (sel == null)
				return false;
			sel = sel.GrowToWord();
			if (sel == null)
				return false;
			ITsString tss;
			int ichMin, ichLim, hvo, tag, ws;
			bool fAssocPrev;
			sel.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvo, out tag, out ws);
			sel.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);

			ITsString tssContext = Cache.MainCacheAccessor.get_StringProp(hvo, tag);
			if (tssContext == null)
				return false;
			// If the string is empty, it might be because it's multilingual.  Try that alternative.
			// (See TE-6374.)
			if (tssContext.Length == 0 && ws != 0)
				tssContext = Cache.MainCacheAccessor.get_MultiStringAlt(hvo, tag, ws);

			ITsString tssWf = null;
			if (tssContext != null && tssContext.Length != 0)
				tssWf = tssContext.GetSubstring(ichMin, ichLim);
			if (tssWf == null || tssWf.Length == 0)
				return false;

			if (ws == 0)
				ws = Cache.DefaultVernWs;
			ChangeSpellingInfo csi = new ChangeSpellingInfo(tssWf.Text, ws, Cache, App);
			csi.DoIt(EditingHelper.EditedRootBox.Site);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method implements the FindInDictionary menu item (both menu bar and context
		/// menu). It is called using reflection by xCore, not directly.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFindInDictionary(object args)
		{
			IVwRootSite rootsite = ActiveView.CastAsIVwRootSite();
			if (rootsite == null || rootsite.RootBox == null)
				return false;

			// The following code roughly exactly matches LexText/Interlinear/RawTextPane.OnLexiconLookup.
			// They should probably stay in sync.
			IVwSelection sel = rootsite.RootBox.Selection;
			if (sel == null)
				return false;
			sel = sel.EndPoint(false);
			if (sel == null)
				return false;
			sel = sel.GrowToWord();
			if (sel == null || !sel.IsRange)
				return false;
			ITsString tss;
			int ichMin, ichLim, hvo, tag, ws;
			bool fAssocPrev;
			sel.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvo, out tag, out ws);
			sel.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);
			LexEntryUi.DisplayOrCreateEntry(Cache, hvo, tag, ws, ichMin, ichLim, this,
				Mediator, m_app, "UserHelpFile");

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method implements the FindRelatedWords menu item (both menu bar and context menu).
		/// It is called using reflection by xCore, not directly.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFindRelatedWords(object args)
		{
			IVwRootSite rootsite = ActiveView.CastAsIVwRootSite();
			if (rootsite == null || rootsite.RootBox == null)
				return false;

			IVwSelection sel = rootsite.RootBox.Selection;
			if (sel == null)
				return false;
			LexEntryUi.DisplayRelatedEntries(this.Cache, sel, this, Mediator, m_app, "UserHelpFile");
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the insert picture dialog.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if we handle this</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnInsertPictureDialog(object args)
		{
			Debug.Assert(m_cache != null);
			FwEditingHelper editHelper = ActiveEditingHelper;
			if (editHelper == null)
				return false;

			if (editHelper.IsBackTranslation)
			{
				SelectionHelper helper = editHelper.CurrentSelection;
				ICmPictureRepository repo = Cache.ServiceLocator.GetInstance<ICmPictureRepository>();
				SelLevInfo info;
				ISegment segment;
				if (helper.GetLevelInfoForTag(StTxtParaTags.kflidSegments, out info))
					segment = m_cache.ServiceLocator.GetInstance<ISegmentRepository>().GetObject(info.hvo);
				else
				{
					SelLevInfo paraLevInfo;
					helper.GetLevelInfoForTag(StTextTags.kflidParagraphs, out paraLevInfo);
					IStTxtPara para = Cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(paraLevInfo.hvo);
					segment = para.GetSegmentForOffsetInFreeTranslation(helper.GetIch(SelectionHelper.SelLimitType.Top), helper.Ws);
				}
				ITsString tssVernSegment = segment.BaselineText;
				Guid guidNextPicNotInBt = tssVernSegment.GetAllEmbeddedObjectGuids(FwObjDataTypes.kodtGuidMoveableObjDisp).FirstOrDefault(g =>
					!segment.FreeTranslation.get_String(helper.Ws).GetAllEmbeddedObjectGuids(FwObjDataTypes.kodtGuidMoveableObjDisp).Contains(g));
				if (guidNextPicNotInBt == Guid.Empty)
					return false;
				ICmPicture pict = repo.GetObject(guidNextPicNotInBt);
				string undo;
				string redo;
				TeResourceHelper.MakeUndoRedoLabels("kstidInsertPicture", out undo, out redo);
				using (UndoTaskHelper undoTaskHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor,
						  editHelper.EditedRootBox.Site, undo, redo))
				{
					editHelper.InsertPicture(pict);
					undoTaskHelper.RollBack = false;
				}
			}
			else
			{
				using (PicturePropertiesDialog dlg = new PicturePropertiesDialog(m_cache, null,
					m_app, m_app))
				{
					// Don't allow inserting an empty picture, so don't check for result of Initialize()
					if (dlg.Initialize())
					{
						if (dlg.ShowDialog() == DialogResult.OK)
							InsertThePicture(null, dlg);
					}
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the clicking on the "Picture Properties" context menu.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnPictureProperties(object args)
		{
			if (ActiveEditingHelper == null || !ActiveEditingHelper.IsPictureSelected)
				return false;

			ICmPicture pic = ActiveEditingHelper.Picture;
			using (PicturePropertiesDialog dlg = new PicturePropertiesDialog(m_cache, pic, m_app, m_app))
			{
				if (dlg.Initialize())
				{
					if (dlg.ShowDialog() == DialogResult.OK)
						InsertThePicture(pic, dlg);
				}
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts the picture at the current selection (or updates the picture we are editing)
		/// </summary>
		/// <param name="initialPicture">picture to modify, or null to insert</param>
		/// <param name="dlg">the dialog we ran to get or modify the picture.</param>
		/// ------------------------------------------------------------------------------------
		private void InsertThePicture(ICmPicture initialPicture, PicturePropertiesDialog dlg)
		{
			FwEditingHelper editHelper = ActiveEditingHelper;
			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels(initialPicture == null ?
				"kstidInsertPicture" : "kstidUpdatePicture", out undo, out redo);
			using (UndoTaskHelper undoTaskHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor,
					  editHelper.EditedRootBox.Site, undo, redo))
			{
				string strLocalPictures = CmFolderTags.DefaultPictureFolder;

				if (initialPicture != null)
					initialPicture.UpdatePicture(dlg.CurrentFile, dlg.Caption, strLocalPictures, m_cache.DefaultVernWs);
				else
					editHelper.InsertPicture(dlg.CurrentFile, dlg.Caption, strLocalPictures);
				undoTaskHelper.RollBack = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a verse number at the current location in the text.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnInsertVerseNumber(object args)
		{
			if (ActiveEditingHelper == null)
				return false;

			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidInsertVerseNumber", out undo, out redo);
			using (UndoTaskHelper undoHelper = new UndoTaskHelper(ActiveView.CastAsIVwRootSite(), undo, redo))
			{
				ActiveEditingHelper.InsertVerseNumber();
				undoHelper.RollBack = false;
			}
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Toggle the InsertVerseNumber mode
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if we handle this</returns>
		/// -----------------------------------------------------------------------------------
		protected bool OnInsertVerseNumbers(object args)
		{
			if (ActiveEditingHelper == null)
				return false;

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null)
				return false;

			ActiveEditingHelper.ProcessInsertVerseNumbers(itemProps.Checked);
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Generate chapter and verse numbers for the translations of the paragraphs in the
		/// current section content.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c> if we handle this</returns>
		/// -----------------------------------------------------------------------------------
		protected bool OnInsertBackTransCV(object args)
		{
			if (ActiveEditingHelper == null)
				return false;

			string undo;
			string redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidGenerateSectionTemplate", out undo, out redo);
			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
				m_cache.ServiceLocator.GetInstance<IActionHandler>(), undo, redo))
			{
				ActiveEditingHelper.GenerateTranslationCVNumsForSection();
				undoHelper.RollBack = false;
			}
			return true;
		}
		#endregion

		#region Edit menu handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Undo handler. Main undo is done in FwMainWnd, but we need this method to be able
		/// to deal with staying in verse insert mode.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected override bool OnEditUndo(object args)
		{
			if (ActiveEditingHelper != null && ActiveEditingHelper.InsertVerseActive)
				ActiveEditingHelper.PreventEndInsertVerseNumbers();

			return base.OnEditUndo(args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handler for "Remove Book" option of Edit menu.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnEditRemoveBook(object args)
		{
			if (ActiveEditingHelper == null ||
				ActiveEditingHelper.BookIndex < 0)
				return false;

			RemoveBook(ActiveEditingHelper.BookIndex);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes requested book.  If not running in test, message box will be displayed to
		/// confirm deletion.
		/// </summary>
		/// <param name="iBook">index of book to be deleted</param>
		/// ------------------------------------------------------------------------------------
		protected void RemoveBook(int iBook)
		{
			Debug.Assert(ActiveEditingHelper != null);

			// display message box if not running in a test
			if (!MiscUtils.RunningTests)
			{
				IScrBook book = BookFilter.GetBook(iBook);
				string msg = string.Format(TeResourceHelper.GetResourceString("kstidConfirmRemoveBook"),
					book.BestUIName);
				DialogResult userResponse = MessageBox.Show(this, msg, m_app.ApplicationName,
					MessageBoxButtons.YesNo);
				if (userResponse != DialogResult.Yes)
					return;
			}

			if (!DataUpdateMonitor.IsUpdateInProgress())
			{
				string undo, redo;
				TeResourceHelper.MakeUndoRedoLabels("kstidUndoRemoveBook", out undo, out redo);

				using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
					m_cache.ServiceLocator.GetInstance<IActionHandler>(), undo, redo))
				{
					ActiveEditingHelper.RemoveBook(iBook);
					BookFilter.BooksDeleted();
					ShowFilterStatusBarMessage(BookFilterInEffect);
					undoHelper.RollBack = false;
				}
			}
		}

		#endregion

		#region Tools menu handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Respond to the Tools/Options menu option to bring up the options dialog.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnToolsOptions(object args)
		{
			int origUiWs = m_cache.DefaultUserWs;
			using (ToolsOptionsDialog dlg = new ToolsOptionsDialog(m_app, m_app, m_cache.ServiceLocator.WritingSystemManager))
			{
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					MaxStyleLevel = ToolsOptionsDialog.MaxStyleLevel;
					InitStyleComboBox();
					if (m_cache.DefaultUserWs != origUiWs)
					{
						using (new DataUpdateMonitor(null, "Updating Key Term Localizations"))
						{
							TeKeyTermsInit.EnsureCurrentLocalization(
								m_cache.WritingSystemFactory.GetStrFromWs(m_cache.DefaultUserWs), m_scr, m_app, this);
						}
					}
					m_app.RefreshAllViews();
				}
			}

			return true;
		}
		#endregion

		#region Format handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// handle the footnote properties context menu
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFootnoteProperties(object args)
		{
			if (ActiveView == null)
				return false;

			string undo, redo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoFootnoteProperties", out undo, out redo);
			using(UndoTaskHelper undoTaskHelper = new UndoTaskHelper(m_cache.ActionHandlerAccessor,
				ActiveView.CastAsIVwRootSite(), undo, redo))
			{
				if (DisplayFootnoteDialog() == DialogResult.OK)
					m_app.RefreshAllViews();
				undoTaskHelper.RollBack = false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays footnote properties dialog.
		/// </summary>
		/// <returns>Dialog result</returns>
		/// <remarks>Created so that dialog could be skipped in testing</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual DialogResult DisplayFootnoteDialog()
		{
			return DisplayScripturePropertiesDlg(true, true);
		}
		#endregion

		#region Help menu handlers
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
				ShowHelp.ShowHelpTopic(m_app, "khtpGettingStarted");
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Student Training Manual menu item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnTrainingStudentManual(object args)
		{
			OpenTrainingDoc("Training", "TE Student Manual.doc");
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Training Exercises menu item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnTrainingExercises(object args)
		{
			OpenTrainingDoc("Training", "TE Exercises.doc");
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Instructor Guide menu item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnTrainingInstructorGuide(object args)
		{
			OpenTrainingDoc("Training", "TE Instructor Guide.doc");
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Copy-and-paste Data menu item.
		/// </summary>
		/// <param name="args">The args.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnTrainingCopyPasteData(object args)
		{
			OpenTrainingDoc("Training", "TE Copy-and-paste Data.rtf");
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the Help Demos menu item.
		/// </summary>
		/// <param name="args">The args.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDemos(object args)
		{
			OpenTrainingDoc("Demos", "index.html");
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open a training document in the associated application.
		/// </summary>
		/// <param name="folder">Sub-folder containing document</param>
		/// <param name="document">Training document to be opened</param>
		/// ------------------------------------------------------------------------------------
		private void OpenTrainingDoc(string folder, string document)
		{
			string helpTeFolder = String.Format(FwDirectoryFinder.CodeDirectory +
				"{0}Helps{0}Translation Editor", Path.DirectorySeparatorChar);
			string path = Path.Combine(Path.Combine(helpTeFolder, folder), document);
			ProcessStartInfo processInfo = new ProcessStartInfo(path);
			processInfo.UseShellExecute = true;
			try
			{
				using (Process.Start(processInfo))
				{
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
		}
		#endregion

		#region Other Message handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnApplyUnTransWord(object arg)
		{
			if (ActiveEditingHelper == null)
				return false;
			// Create a new undo-task corresponding to the applying of the style.
			string sUndo, sRedo;
			ResourceHelper.MakeUndoRedoLabels("kstidUndoApplyStyle", out sUndo, out sRedo);
			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
				Cache.ServiceLocator.GetInstance<IActionHandler>(), sUndo, sRedo))
			{
				ActiveEditingHelper.ApplyStyle(ScrStyleNames.UntranslatedWord);
				undoHelper.RollBack = false;
			}
			//ActiveEditingHelper.ApplyWritingSystem();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clicking on a footnote in a draft view does the same as the menu item View Footnotes,
		/// except that the provided StFootnote forces the footnote window to show
		/// (instead of toggling).
		/// </summary>
		/// <param name="arg">The footnote that was clicked on.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnFootnoteClick(object arg)
		{
			if (!(arg is IStFootnote))
				return false;

			using (new WaitCursor(this))
			{
				IStFootnote footnote = (IStFootnote)arg;

				if (TheDraftViewWrapper != null && TheDraftViewWrapper.Visible)
				{
					TheDraftViewWrapper.ShowFootnoteView(footnote);
					return true;
				}
				else if (TheBTSplitWrapper != null && TheBTSplitWrapper.Visible)
				{
					TheBTSplitWrapper.ShowFootnoteView(footnote);
					return true;
				}
				else if (TheBTReviewWrapper != null && TheBTReviewWrapper.Visible)
				{
					// show the footnotes (the bottom view) in the BT review view
					TheBTReviewWrapper.ShowFootnoteView(footnote);
					return true;
				}
				else if (TheKeyTermsWrapper != null && TheKeyTermsWrapper.Visible)
				{
					// show the footnotes (the bottom view) in the key terms view
					KeyTermsDraftView.ShowFootnoteView(footnote);
					return true;
				}
				else if (TheEditChecksViewWrapper != null && TheEditChecksViewWrapper.Visible)
				{
					// show the footnotes (the bottom view) in the editorial checks view
					EditorialChecksDraftView.ShowFootnoteView(footnote);
					return true;
				}
			}

			Debug.Assert(false, "Are we missing a Wrapper type in OnFootnoteClick?");
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Remove Character Style menu/toolbar command.
		/// </summary>
		/// <param name="args">The arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// -----------------------------------------------------------------------------------
		protected bool OnRemoveCharacterStyle(object args)
		{
			if (ActiveEditingHelper == null)
				return false;
			ActiveEditingHelper.RemoveCharFormatting();
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Reset Paragraph Style menu/toolbar command.
		/// </summary>
		/// <param name="args">The arguments</param>
		/// <returns><c>true</c> if message handled, otherwise <c>false</c>.</returns>
		/// -----------------------------------------------------------------------------------
		protected bool OnResetParagraphStyle(object args)
		{
			if (ActiveEditingHelper == null)
				return false;

			string sUndo;
			string sRedo;
			TeResourceHelper.MakeUndoRedoLabels("kstidUndoApplyStyle", out sUndo, out sRedo);
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(sUndo, sRedo, m_cache.ActionHandlerAccessor, () =>
			{
				ActiveEditingHelper.ResetParagraphStyle();
			});
			return true;
		}
		#endregion

		#region Other event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (!DesignMode)
			{
				InitStyleSheet(m_scr.Hvo, ScriptureTags.kflidStyles);
				UpdateWritingSystemsInCombobox();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do stuff when the window is closing.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			TeProjectSettings.BookFilterEnabled = BookFilterInEffect;
			TeProjectSettings.BookFilterBooks = m_bookFilter.GetSavedFilterAsString();

			base.OnClosing(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Registers a view.
		/// </summary>
		/// <param name="view">The view.</param>
		/// ------------------------------------------------------------------------------------
		internal void RegisterFocusableView(UserControl view)
		{
			view.LostFocus += ViewLostFocus;
			view.GotFocus += ViewGotFocus;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the selection of the view that loses focus
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void ViewLostFocus(object sender, EventArgs e)
		{
			IRootSite site = sender as IRootSite;
			if (site != null)
			{
				if (sender is Control && !((Control)sender).Visible)
					return;

				IVwRootBox rootBox = site.CastAsIVwRootSite().RootBox;
				if (rootBox == null)
					return;

				CommonSelection = rootBox.Selection;
				m_viewThatLostFocus = site;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tries to set the previous selection into the view that has gained focus
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "findReplaceDlg is a reference")]
		private void ViewGotFocus(object sender, EventArgs e)
		{
			if (!(sender is IRootSite) || SIBAdapter == null)
				return;

			IRootSite site = (IRootSite)sender;
			if (site == m_viewThatLostFocus)
				return;

			// set the owner of the find and replace dialog to be the newly focused view
			IVwRootSite rootSite = site.CastAsIVwRootSite();
			FwFindReplaceDlg findReplaceDlg = m_app.FindReplaceDialog;
			if (findReplaceDlg != null)
				findReplaceDlg.SetOwner(rootSite, this, m_app.FindPattern);

			// if the view that lost focus is of the same type as the one gaining focus
			// then don't bother setting the selection. This keeps the split draft view
			// from moving to unexpected places when the user clicks on the other view.
			if (SIBAdapter.CurrentTabItemProperties != null)
			{
				if (SIBAdapter.CurrentTabItemProperties.Name == m_prevTabItemName)
					return;

				m_prevTabItemName = SIBAdapter.CurrentTabItemProperties.Name;
			}

			if (CommonSelection == null)
				return;

#if !DEBUG
			try
			{
#endif
				ILocationTracker newLocation = ((ITeView)rootSite).LocationTracker;
				SelectionHelper newSelHelper = SelectionHelper.Create(CommonSelection, rootSite);
				ILocationTracker oldLocation = ((ITeView)m_viewThatLostFocus).LocationTracker;
				SelectionHelper oldSelHelper = SelectionHelper.Create(CommonSelection,
					m_viewThatLostFocus.CastAsIVwRootSite());
				if (oldSelHelper == null)
					return; // Hopefully rare! Has been observed e.g. when converting old BT to segmented overwrites selection.
				int iBook = oldLocation.GetBookIndex(oldSelHelper, SelectionHelper.SelLimitType.Anchor);
				int iSection = oldLocation.GetSectionIndexInBook(oldSelHelper, SelectionHelper.SelLimitType.Anchor);
				if (rootSite is ScripturePublication)
				{
					if (!(m_viewThatLostFocus is ScripturePublication))
					{
						// Switching from a drafting view to a print layout view, so strip
						//off the book level
						newSelHelper.RemoveLevel(BookFilter.Tag);
					}
					// Whatever the old view, the new one needs to focus the right division.
					newLocation.SetBookAndSection(newSelHelper, SelectionHelper.SelLimitType.Anchor, iBook, iSection);
					newLocation.SetBookAndSection(newSelHelper, SelectionHelper.SelLimitType.End, iBook, iSection);
				}
				else if (m_viewThatLostFocus is ScripturePublication)
				{
					// Switching from a print layout view to a drafting view, so add on
					// the book level
					newSelHelper.AppendLevel(BookFilter.Tag, iBook, 0);
					// Setting the book is redundant here, of course.
					newLocation.SetBookAndSection(newSelHelper, SelectionHelper.SelLimitType.Anchor, iBook, iSection);
					newLocation.SetBookAndSection(newSelHelper, SelectionHelper.SelLimitType.End, iBook, iSection);
				}

				TeEditingHelper newEditingHelper = site.EditingHelper as TeEditingHelper;
				StVc.ContentTypes newContentType = StVc.ContentTypes.kctNormal;
				if (newEditingHelper != null)
					newContentType = newEditingHelper.ContentType;
				ScripturePublication newScrPub = site as ScripturePublication;
				if (newScrPub != null)
					newContentType = newScrPub.ContentType;

				if ((newEditingHelper != null && newEditingHelper.IsBackTranslation) ||
					(newScrPub != null && (newScrPub.ViewType & TeViewType.BackTranslation) != 0))
				{
					// Print layout views that show both the BT and vernacular can make
					// the new selection in either side, so don't adjust the selection
					// levels at all.
					if (newScrPub == null ||
						newScrPub.ViewType != TeViewType.BackTranslationParallelPrint)
					{
						// We're switching to a view that can only restore the selection
						// in the back translation. Add the BT level if we're coming from
						// a view whose selection was in the vernacular.
						// -1 is the "tag" for CmTranslations
						if (newSelHelper.GetLevelForTag(StTxtParaTags.kflidSegments) < 0)
						{
							// We won't try to match exact position, just somewhere close.
							newSelHelper.IchAnchor = 0;
							newSelHelper.IchEnd = 0;
							int targetFlid;
							if (!Options.UseInterlinearBackTranslation)
							{
								newSelHelper.InsertLevel(0, -1, 0, 0 /*dv.BackTranslationWS */);
								targetFlid = CmTranslationTags.kflidTranslation;
							}
							else
							{
								// We need to know the paragraph. But this is quite tricky. The newSelHelper was not
								// created from a selection, so we can't use the hvo property in the SelLevInfo.
								// The typical case is that the last SelLevInfo indicates a book, the next-to-last
								// a section, the one before that contents or heading, and the one before that the
								// relevant paragraph in that StText.
								// But, it may be a print preview which lacks the book level.
								// And, it may be the title of the book, which lacks the section level.
								IScrBook book = oldLocation.GetBook(oldSelHelper, SelectionHelper.SelLimitType.Anchor);
								if (book == null)
									return;
								int ilevParas = newSelHelper.GetLevelForTag(StTextTags.kflidParagraphs);
								IStText stText = null;
								switch (newSelHelper.LevelInfo[ilevParas + 1].tag)
								{
									case ScrBookTags.kflidTitle:
										stText = book.TitleOA;
										break;
									case ScrSectionTags.kflidContent:
										stText = book.SectionsOS[iSection].ContentOA;
										break;
									case ScrSectionTags.kflidHeading:
										stText = book.SectionsOS[iSection].HeadingOA;
										break;
									case ScrBookTags.kflidFootnotes:
										int iFootnote = newSelHelper.GetLevelInfoForTag(ScrBookTags.kflidFootnotes).ihvo;
										stText = book.FootnotesOS[iFootnote];
										break;
								}
								IScrTxtPara para = (IScrTxtPara)stText[newSelHelper.LevelInfo[ilevParas].ihvo];
								// Now we know the paragraph we can figure out the first editable segment in it.
								int btWs = (newEditingHelper == null ?
									newScrPub.ViewConstructorWS : newEditingHelper.ViewConstructorWS);
								int iseg = TeEditingHelper.GetBtSegIndexForVernChar(para, oldSelHelper.IchAnchor, btWs);
								if (iseg < 0)
									iseg = 0;
								newSelHelper.InsertLevel(0, StTxtParaTags.kflidSegments, iseg, 0);
								targetFlid = SegmentTags.kflidFreeTranslation;
							}
							// Change the target text property to the expected one for the type of BT view.
							newSelHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, targetFlid);
							newSelHelper.SetTextPropId(SelectionHelper.SelLimitType.End, targetFlid);
						}
					}
				}
				else
				{
					// We're switching to a view that is not displaying the BT. Remove
					// the BT level(s) if present.
					if (newSelHelper != null && newSelHelper.GetLevelForTag(StTxtParaTags.kflidSegments) >= 0)
					{
						int clev = newSelHelper.GetLevelForTag(StTxtParaTags.kflidSegments);

						// Removes the level for the CmTranslation or the two levels for the
						// Segments and the FT of the segment. Everything up to and including
						// the first BT-related level.
						for (int i = 0; i <= clev; i++)
							newSelHelper.RemoveLevelAt(0);

						// Don't try to match more exactly than paragraph.
						newSelHelper.IchAnchor = 0;
						newSelHelper.IchEnd = 0;

						// Non-BT views always display Contents.
						newSelHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, StTxtParaTags.kflidContents);
						newSelHelper.SetTextPropId(SelectionHelper.SelLimitType.End, StTxtParaTags.kflidContents);
					}
				}

				if (newSelHelper != null && newSelHelper.MakeBest(true) == null)
				{
					// try again with (or without) user prompt. It is possible that e.g. the BT
					// view displays a prompt where the draft view has text.
					if (newSelHelper.GetTextPropId(SelectionHelper.SelLimitType.Anchor) == SimpleRootSite.kTagUserPrompt)
					{
						int tag;
						switch (newContentType)
						{
							case StVc.ContentTypes.kctSimpleBT:
								tag = CmTranslationTags.kflidTranslation;
								break;
							case StVc.ContentTypes.kctSegmentBT:
								tag = SegmentTags.kflidFreeTranslation;
								break;
							default: // to make compiler happy.
							case StVc.ContentTypes.kctNormal:
								tag = StTxtParaTags.kflidContents;
								break;
						}
						newSelHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, tag);
						newSelHelper.SetTextPropId(SelectionHelper.SelLimitType.End, tag);
					}
					else
					{
						newSelHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, SimpleRootSite.kTagUserPrompt);
						newSelHelper.SetTextPropId(SelectionHelper.SelLimitType.End, SimpleRootSite.kTagUserPrompt);
					}

					newSelHelper.MakeBest(true);
				}
#if !DEBUG
			}
			catch
			{
				// just ignore in release builds
			}
#endif
			CommonSelection = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the "Filtered" indicator on the status bar in response to a book being added
		/// or removed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void BooksChanged(ICmObject sender)
		{
			ShowFilterStatusBarMessage(BookFilterInEffect);
		}
		#endregion

		#region Go to Chapter and Go to Referenec message handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the first chapter in the current book
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnGoToFirstChapter(object args)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;

			using (new WaitCursor(this))
			{
				try
				{
					IScrBook book = m_bookFilter.GetBook(ActiveEditingHelper.BookIndex);
					GotoVerse(new ScrReference(book.CanonicalNum, 1, 1, m_scr.Versification));
				}
				catch
				{
#if DEBUG
					throw; // just ignore in release builds
#endif
				}
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the previous chapter
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnGoToPrevChapter(object args)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;
			using (new WaitCursor(this))
			{
				GotoVerse(ActiveEditingHelper.GetPrevChapter());
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the next chapter
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnGoToNextChapter(object args)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;
			using (new WaitCursor(this))
			{
				GotoVerse(ActiveEditingHelper.GetNextChapter());
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the last  chapter in the current book
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnGoToLastChapter(object args)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;
			using (new WaitCursor(this))
			{
				GotoVerse(ActiveEditingHelper.GetLastChapter());
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the GotoReferenceDialog dialog box and lets the user input a reference to
		/// go to.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnGoToReference(object args)
		{
			CheckDisposed();

			ScrReference reference = GetReferenceToCurrentBook();
			if (reference == null)
				return false;
			using (GotoReferenceDialog dialog = new GotoReferenceDialog(reference, m_scr, m_app))
			{
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					using (new WaitCursor(this))
					{
						GotoVerse(dialog.ScReference);
					}
				}
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a reference to the beginning of the current book.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ScrReference GetReferenceToCurrentBook()
		{
			if (ActiveEditingHelper == null)
				return null;
			int bookIndex = ActiveEditingHelper.BookIndex;
			IScrBook book = null;
			int bookCount = m_bookFilter.BookCount;
			if (bookCount > 0)
			{
				if (bookIndex < 0 || bookIndex >= bookCount) // bookIndex out of range
					bookIndex = 0; // This probably can't happen, but if it does, just use first filtered book
				book = m_bookFilter.GetBook(bookIndex);
			}
			if (book == null)
			{
				// Couldn't find a book in the filter (all filtered out?), so use first book in unfiltered Scripture
				book = m_scr.ScriptureBooksOS[0];
			}
			if (book == null)
			{
				Debug.Assert(false, "Hopefully this can never happen.");
				return null;
			}
			return new ScrReference((short)book.CanonicalNum, 1, 1, m_scr.Versification);
		}
		#endregion

		#region IScrRefTracker implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is called from FocusMessageHandling.SetUsfmFocus to set the current
		/// reference in the usfm browser if one is active.
		/// </summary>
		/// <param name="sr">The target reference in the current project's versification</param>
		/// ------------------------------------------------------------------------------------
		public void SetCurrentReference(ScrReference sr)
		{
			if (m_usfmBrowser != null)
				m_usfmBrowser.SetTeScriptureReference(sr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Syncs to the current Scripture reference location of the given editing helper.
		/// </summary>
		/// <param name="editingHelper">The editing helper.</param>
		/// <param name="fSendInternalOnly">if set to <c>true</c> does not send reference to
		/// third-party listeners.</param>
		/// ------------------------------------------------------------------------------------
		public void SyncToScrLocation(TeEditingHelper editingHelper, bool fSendInternalOnly)
		{
			if (m_syncHandler != null)
				m_syncHandler.SyncToScrLocation(this, editingHelper, fSendInternalOnly);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides simple access to the reference at the current insertion point.
		/// </summary>
		/// <remarks>
		/// This property is not guaranteed to return a ScrReference containing the book, chapter,
		/// AND verse.  It will return as much as it can, but not neccessarily all of it. It
		/// will not search back into a previous section if it can't find the verse number in
		/// the current section. This means that if a verse crosses a section break, the verse
		/// number will be inferred from the section start ref.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public ScrReference CurrentRef
		{
			get
			{
				return ActiveEditingHelper != null ? ActiveEditingHelper.CurrentStartRef : null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to ignore any sync messages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IgnoreAnySyncMessages
		{
			get { return m_syncHandler.IgnoreAnySyncMessages; }
			set { m_syncHandler.IgnoreAnySyncMessages = value; }
		}
		#endregion

		#region SyncMessage handling methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enables the sending of sync messages.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnSendReferences(object args)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null)
				return false;

			itemProps.Checked = !itemProps.Checked;
			itemProps.Update = true;

			TeProjectSettings.SendSyncMessages = itemProps.Checked;

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enables or disables this editing helper's ability to respond to synchronizing to
		/// the notes window or external applications (e.g. Libronix, Paratext, etc.). Each
		/// view has its own TeEditingHelper and this is called whenever the active view
		/// changes. When a view becomes active, this method of its editing helper is called
		/// to enable responding and vice versa when the view becomes inactive... in case it
		/// isn't obvious.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RespondToSyncScrollingMsgs(bool enable)
		{
			if (enable)
			{
				m_syncHandler.ReferenceChanged += ScrollToReference;
				m_syncHandler.AnnotationChanged += ScrollToCitedText;
				if (TeProjectSettings.ReceiveSyncMessages)
					m_syncHandler.EnableLibronixLinking = true; // turn on the connection
			}
			else
			{
				m_syncHandler.ReferenceChanged -= ScrollToReference;
				m_syncHandler.AnnotationChanged -= ScrollToCitedText;
				m_syncHandler.EnableLibronixLinking = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles a callback from a 3rd party application to scroll to particular reference
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="scrRef">The Scripture reference.</param>
		/// <param name="tssSelectedText">The selected text.</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "notesWnd is a reference")]
		public void ScrollToReference(object sender, ScrReference scrRef, ITsString tssSelectedText)
		{
			CheckDisposed();

			NotesMainWnd notesWnd = ((TeApp)m_app).NotesWindow;
			// We don't want editorial checks and key terms draft views to respond
			// to sync. messages because they get their messages from the lists
			// to which they correspond.
			if (m_selectedView is KeyTermsViewWrapper || m_selectedView is EditorialChecksViewWrapper)
			{
				if (notesWnd != null)
				{
					// Don't send sync. messages from the notes view when the ed. checks or
					// key terms view is showing.
					notesWnd.SendSyncScrollingMsgs = false;
				}
				return;
			}

			BtDraftSplitWrapper btWrapper = m_selectedView as BtDraftSplitWrapper;
			if (btWrapper == null)
			{
				if (ActiveEditingHelper != null && notesWnd != null)
				{
					// Don't send sync. messages from the notes view when the ed.
					// checks or key terms view is showing.
					notesWnd.SendSyncScrollingMsgs = true;
				}
			}
			else if (notesWnd != null)
			{
				notesWnd.SendSyncScrollingMsgs = false;
			}

			if (sender != ActiveEditingHelper && ActiveEditingHelper != null)
			{
				if (btWrapper == null)
					ActiveEditingHelper.SelectVerseText(scrRef, tssSelectedText, false);
				else
				{
					((TeEditingHelper)btWrapper.VernDraftView.EditingHelper).SelectVerseText(scrRef, tssSelectedText, false);
					((TeEditingHelper)btWrapper.BTDraftView.EditingHelper).SelectVerseText(scrRef, tssSelectedText, false);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scrolls to cited text.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="annotation">The annotation whose reference and cited text we should
		/// sync. to.</param>
		/// ------------------------------------------------------------------------------------
		void ScrollToCitedText(object sender, IScrScriptureNote annotation)
		{
			CheckDisposed();
			if (sender != ActiveEditingHelper && annotation != null)
				ActiveEditingHelper.GoToScrScriptureNoteRef(annotation, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Turn on or off the control that shows USFM resources.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnShowUsfmResources(object args)
		{
			if (MiscUtils.IsUnix) // FWNX-386
			{
				return false;
			}

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null || !(ActiveView is ITeView))
				return false;

			using (RegistryBoolSetting showUSFMResources = new RegistryBoolSetting(m_app.ProjectSpecificSettingsKey,
				"USFMResourcesVisible" + m_selectedView.Name, false))
			{
				showUSFMResources.Value = itemProps.Checked = !showUSFMResources.Value;
				itemProps.Update = true;

				DisplayUsfmBrowser(itemProps.Checked);

				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays or hides the USFM resource browser.
		/// </summary>
		/// <param name="fDisplay">if set to <c>true</c> display the browser; if <c>false</c>
		/// hide the browser.</param>
		/// ------------------------------------------------------------------------------------
		private void DisplayUsfmBrowser(bool fDisplay)
		{
			if (fDisplay)
				ShowUsfmResources();
			else if (m_usfmBrowser != null)
				m_usfmBrowser.HideFloaty();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to enable or disable the Send Scripture References menu/toolbar item.
		/// </summary>
		/// <param name="args">The args.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateShowUsfmResources(object args)
		{
			CheckDisposed();
			if (MiscUtils.IsUnix) // FWNX-386
			{
				TMItemProperties itemProps = args as TMItemProperties;
				itemProps.Enabled = false;
				itemProps.Update = true;
				return true;
			}

			try
			{
				TMItemProperties itemProps = args as TMItemProperties;
				if (itemProps != null)
				{
					itemProps.Enabled = m_fUsfmBrowserEnabled;
					itemProps.Checked = m_usfmBrowser != null && m_usfmBrowser.Showing;
					itemProps.Update = true;
					return true;
				}
			}
			catch
			{
#if DEBUG
				throw; // just ignore in release builds
#endif
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the usfm resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ShowUsfmResources()
		{
			if (m_usfmBrowser == null)
			{
				SuspendLayout();
				try
				{
					m_usfmBrowser = new DockableUsfmBrowser();
					if (!m_usfmBrowser.Install(this.splitContainer.Panel2, m_app))
					{
						m_usfmBrowser = null;
						return;
					}
					// JohnT: I don't really understand this next line, but without it, it sometimes docks between
					// the title bar and the body of the main control, and sometimes docks above the title bar
					// but puts its splitter below it. If you mess with it, be sure to test both moving it with the
					// menu and dragging it into place...it's quite possible to get them behaving differently. Grrr!
					m_usfmBrowser.Floaty.DockOnInside = false;
					m_usfmBrowser.Floaty.AllowedDocking = AnchorStyles.Top | AnchorStyles.Bottom;
					m_usfmBrowser.Floaty.RegistryValuePrefix = "USFM";
					m_usfmBrowser.Floaty.DefaultLocation = DockStyle.Bottom;
					m_usfmBrowser.Floaty.SettingsKey = m_app.ProjectSpecificSettingsKey;
					m_usfmBrowser.LoadSettings(m_app.ProjectSpecificSettingsKey);
				}
				catch
				{
					m_usfmBrowser = null;
					m_fUsfmBrowserEnabled = false;
					throw;
				}
				finally
				{
					ResumeLayout();
				}
			}

			m_usfmBrowser.ShowFloaty();
			m_usfmBrowser.SendToBack();
			if (ActiveEditingHelper != null && ActiveEditingHelper.OkayToResendScriptureReference)
				SyncToScrLocation(ActiveEditingHelper, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enables the receiving of sync messages.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnReceiveReferences(object args)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null)
				return false;

			itemProps.Checked = !itemProps.Checked;
			itemProps.Update = true;

			TeProjectSettings.ReceiveSyncMessages = itemProps.Checked;

			return true;
		}
		#endregion

		#region Go to Footnote handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the next footnote.
		/// </summary>
		/// <param name="menuItem"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnGoToNextFootnote(object menuItem)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;

			using (new WaitCursor(this))
			{
				ActiveEditingHelper.GoToNextFootnote();
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the BackTranslationNextMissingBtFootnoteMkr command by attempting to
		/// navigate to a position in the back translation that closely corresponds to a place
		/// in the verncaular that has a footnote whose marker has not been inserted into the
		/// back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnBackTranslationNextMissingBtFootnoteMkr(object args)
		{
			// Move to the next paragraph corresponding to a vernacular paragraph which has a
			// footnote that has not been inserted into this BT
			using (new WaitCursor(this))
			{
				if (!TheBTSplitWrapper.BTDraftView.TeEditingHelper.GoToNextMissingBtFootnoteMkr(ActiveEditingHelper))
					SystemSounds.Beep.Play();
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the previous Scripture book
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnGoToPrevFootnote(object args)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;

			using (new WaitCursor(this))
			{
				ActiveEditingHelper.GoToPreviousFootnote();
			}
			return true;
		}

		#endregion

		#region Go to Book message handler
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the first Scripture book
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnGoToFirstBook(object args)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;
			using (new WaitCursor(this))
			{
				ActiveEditingHelper.GoToFirstBook();
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the next footnote.
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnGoToPrevBook(object args)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;
			using (new WaitCursor(this))
			{
				ActiveEditingHelper.GoToPrevBook();
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the next Scripture book
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnGoToNextBook(object args)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;
			using (new WaitCursor(this))
			{
				ActiveEditingHelper.GoToNextBook();
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the last Scripture book
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnGoToLastBook(object args)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;
			using (new WaitCursor(this))
			{
				ActiveEditingHelper.GoToLastBook();
			}
			return true;
		}
		#endregion

		#region Go to Section message handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the first Scripture section in the current book
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnGoToFirstSection(object args)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;
			using (new WaitCursor(this))
			{
				ActiveEditingHelper.GoToFirstSection();
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the previous Scripture section
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnGoToPrevSection(object args)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;
			using (new WaitCursor(this))
			{
				ActiveEditingHelper.GoToPrevSection();
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the next Scripture section
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnGoToNextSection(object args)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;
			using (new WaitCursor(this))
			{
				ActiveEditingHelper.GoToNextSection();
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the last Scripture section in the current book
		/// </summary>
		/// <param name="args"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnGoToLastSection(object args)
		{
			CheckDisposed();

			if (ActiveEditingHelper == null)
				return false;
			using (new WaitCursor(this))
			{
				ActiveEditingHelper.GoToLastSection();
			}
			return true;
		}
		#endregion

		#region Miscellaneous Private/protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the scripture properties dialog.
		/// </summary>
		/// <param name="fOnFootnoteTab">set to <c>true</c> to display Footnotes tab,
		/// set to <c>false</c> to display Scripture tab.</param>
		/// <param name="showFootnoteTab">Value determining whether or not to show the
		/// footnote tab.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "rootSite is a reference")]
		private DialogResult DisplayScripturePropertiesDlg(bool fOnFootnoteTab, bool showFootnoteTab)
		{
			// even when the IP is in the footnote pane we still want to pass the draft view
			// because that controls the footnote pane. Otherwise the scrolling doesn't work
			// too well (DraftView scrolls to random place if IP is in footnote pane).
			IRootSite rootSite = ActiveView;
			var footnoteView = rootSite as FootnoteView;
			if (footnoteView != null)
				rootSite = footnoteView.DraftView;

			using (ScriptureProperties dlg = new ScriptureProperties(m_cache,
				m_StyleSheet, rootSite, showFootnoteTab, m_app))
			{
				if (fOnFootnoteTab && showFootnoteTab)
					dlg.DialogTab = ScriptureProperties.kFootnotesTab;

				return dlg.ShowDialog(this);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the filter panel in the status bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void BookFilterChanged(object sender, EventArgs e)
		{
			ShowFilterStatusBarMessage(BookFilterInEffect);
			RefreshAllViews();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refresh the notes window to show any updates
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "notesWnd is a reference")]
		protected void RefreshNotesWindow()
		{
			NotesMainWnd notesWnd = ((TeApp)m_app).NotesWindow;
			if (notesWnd != null)
				notesWnd.RefreshAllViews();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the default BT writing system for this main window and update any back
		/// translation views that have already been instantiated.
		/// </summary>
		/// <param name="newWs">The new writing system for the back translation view.</param>
		/// ------------------------------------------------------------------------------------
		private void ChangeDefaultBTWritingSystem(int newWs)
		{
			using (new WaitCursor(this))
			{
				m_defaultBackTranslationWs = newWs;

				foreach (IRootSite view in m_rgClientViews.Values)
				{
					IBtAwareView btView = view as IBtAwareView;
					if (btView != null)
						btView.BackTranslationWS = newWs;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show help topic for currently selected style. (This method is also in NotesMainWnd.
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
		/// Update the book filter with new set of filtered books. Preserve any existing
		/// selections for all views.
		/// </summary>
		/// <param name="rghvoBooks">Array of ids for the books that are to comprise the new
		/// filtered set</param>
		/// ------------------------------------------------------------------------------------
		private void UpdateBookFilter(IScrBook[] rghvoBooks)
		{
			using (new WaitCursor(this))
			{
				List<RestoreSelInfo> restoreInfos = new List<RestoreSelInfo>(m_rgClientViews.Count);
				List<IScrBook> newFilteredBooks = new List<IScrBook>(rghvoBooks);

				foreach (IRootSite rootsite in m_viewHelper.Views)
				{
					if (rootsite == null || rootsite.EditingHelper == null ||
						rootsite.EditingHelper.CurrentSelection == null)
						continue;

					SelectionHelper selHelper = rootsite.EditingHelper.CurrentSelection;

					ITeView itev = rootsite.EditingHelper.Control as ITeView;
					if (itev == null || itev.LocationTracker == null)
						continue;

					ILocationTracker tracker = itev.LocationTracker;

					IScrBook bookAnchor = tracker.GetBook(selHelper, SelectionHelper.SelLimitType.Anchor);
					IScrBook bookEnd = tracker.GetBook(selHelper, SelectionHelper.SelLimitType.End);
					int iSectionAnchor = tracker.GetSectionIndexInBook(selHelper, SelectionHelper.SelLimitType.Anchor);
					int iSectionEnd = tracker.GetSectionIndexInBook(selHelper, SelectionHelper.SelLimitType.End);
					int ihvoBookNewAnchor = newFilteredBooks.IndexOf(bookAnchor);
					int ihvoBookNewEnd = newFilteredBooks.IndexOf(bookEnd);
					if (ihvoBookNewAnchor >= 0 || ihvoBookNewEnd >= 0)
					{
						// Book is still in the filter. Keep the selection in the same place.
						if (ihvoBookNewAnchor < 0)
						{
							ihvoBookNewAnchor = ihvoBookNewEnd;
							iSectionAnchor = iSectionEnd;
						}
						else if (ihvoBookNewEnd < 0)
						{
							ihvoBookNewEnd = ihvoBookNewAnchor;
							iSectionEnd = iSectionAnchor;
						}

						restoreInfos.Add(new RestoreSelInfo(tracker, selHelper,
							ihvoBookNewAnchor, iSectionAnchor, ihvoBookNewEnd, iSectionEnd,
							rootsite == m_viewHelper.ActiveView));
					}
					else
					{
						// Book isn't in filter anymore. Set IP to beginning of first displayed
						// book
						selHelper.AssocPrev = false;
						selHelper.IchAnchor = 0;
						if (selHelper.GetLevelForTag(StTextTags.kflidParagraphs) >= 0)
						{
							selHelper.LevelInfo[selHelper.GetLevelForTag(
								StTextTags.kflidParagraphs)].ihvo = 0;
						}
						restoreInfos.Add(new RestoreSelInfo(tracker, selHelper,
							0, 0, 0, 0, rootsite == m_viewHelper.ActiveView));
					}
				}

				m_bookFilter.FilteredBooks = rghvoBooks;

				RestoreSelInfo activeViewSelInfo = null;

				// Restore the selection in the active view. If the selection cannot be restored because
				// the book is now filtered out of view, just go to the top of the window instead.
				// We used to restore the selection in all views, but this caused problems described in
				// TE-8379 and was totally pointless since we try to make a useful selection based on the
				// previous view whenever switching to a new active view.
				foreach (RestoreSelInfo restoreSelInfo in restoreInfos)
				{
					if (restoreSelInfo.IsActiveView)
						activeViewSelInfo = restoreSelInfo;
				}

				// Do the active view last so that the style combo box shows the right style
				if (activeViewSelInfo != null)
					RestoreSelection(activeViewSelInfo);

				if (KeyTermsViewIsCreated)
					UpdateKeyTermsBookFilter();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the key terms view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateKeyTermsView()
		{
			if (KeyTermsViewIsVisible)
				TheKeyTermsWrapper.UpdateKeyTermsView();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the key terms book filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateKeyTermsBookFilter()
		{
			if (TheKeyTermsWrapper != null)
				TheKeyTermsWrapper.UpdateBookFilter();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restores the selection after applying a book filter.
		/// </summary>
		/// <param name="restoreSelInfo">The restore selection info.</param>
		/// ------------------------------------------------------------------------------------
		private void RestoreSelection(RestoreSelInfo restoreSelInfo)
		{
			SelectionHelper selHelper = restoreSelInfo.SelHelper;
			restoreSelInfo.LocationTracker.SetBookAndSection(selHelper,
				SelectionHelper.SelLimitType.Anchor, restoreSelInfo.iBookAnchor,
				restoreSelInfo.iSectionAnchor);
			restoreSelInfo.LocationTracker.SetBookAndSection(selHelper,
				SelectionHelper.SelLimitType.End, restoreSelInfo.iBookEnd,
				restoreSelInfo.iSectionEnd);

			if (!selHelper.RestoreSelectionAndScrollPos())
			{
				// Go to the beginning of the first section of the first book in the filter.
				for (int i = 0; i < selHelper.NumberOfLevels; i++)
					selHelper.LevelInfo[i].ihvo = 0;
				IVwSelection sel = selHelper.SetSelection(true);
				if (sel == null)
				{
					// If selection was in a footnote view and is no longer valid (because the
					// filtered books do not have footnotes), make a selection in the corresponding
					// draft view.
					if (selHelper.RootSite is FootnoteView)
						((FootnoteView)selHelper.RootSite).DraftView.Focus();
					else if (selHelper.RootSite != null && selHelper.RootSite.RootBox != null)
					{
						// It is possible that we're in the footnotes of a print layout view. In
						// this case restoring the selection didn't work. Make a selection at
						// the beginning of the current book (SetBookAndSection took us to the
						// right book, now we just have to make a selection and scroll that into
						// view).
						sel = selHelper.RootSite.RootBox.MakeSimpleSel(true, true, false, true);
						selHelper.RootSite.ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
					}
				}
			}
		}
		#endregion

		#region Other overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If showing the USFM browser docked, window can't get as small as usual.
		/// </summary>
		/// <value></value>
		/// <returns>A <see cref="T:System.Drawing.Size"/> that represents the minimum size for the form.</returns>
		/// <exception cref="T:System.ArgumentOutOfRangeException">The values of the height or width within the <see cref="T:System.Drawing.Size"/> object are less than zero. </exception>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// 	<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/>
		/// 	<IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		/// ------------------------------------------------------------------------------------
		public override System.Drawing.Size MinimumSize
		{
			get
			{
				if (DesignMode || m_usfmBrowser == null ||
					(m_usfmBrowser.Floaty != null && m_usfmBrowser.Floaty.DockMode == DockStyle.None))
				{
					return base.MinimumSize;
				}

				System.Drawing.Size result = base.MinimumSize;
				result.Height += m_usfmBrowser.Height;
				return result;
			}
			set
			{
				base.MinimumSize = value;
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the page setup dialog.
		/// </summary>
		/// <param name="pgl">The PubPageLayout object</param>
		/// <param name="pub">The Publication object</param>
		/// <param name="div">The PubDivision object</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override IPageSetupDialog CreatePageSetupDialog(IPubPageLayout pgl,
			IPublication pub, IPubDivision div)
		{
			WritingSystem wsObj = m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem;

			var dlg = new TePageSetupDlg(pgl, m_scr, pub, div, this,
				m_app, m_app, ActiveEditingHelper.IsTrialPublicationView,
				TePublicationsInit.GetPubPageSizes(pub.Name, wsObj.ID));
			InitializePageSetupDlg(dlg);
			return dlg;
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
		/// Hide the previously active view and activate the new one (corresponding to the
		/// button clicked in the sidebar (or the menu item chosen).
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override bool OnSwitchActiveView(object args)
		{
			SBTabItemProperties itemProps = args as SBTabItemProperties;
			if (itemProps == null || itemProps.ParentForm != this)
				return false;

			if (itemProps.Tag is TeViewType)
			{
				TeViewType viewType = (TeViewType)itemProps.Tag;
				IRootSite rootSite;
				ISelectableViewFactory viewFactory;
				if (m_rgClientViews.TryGetValue(TeEditingHelper.ViewTypeString(viewType), out rootSite))
				{
					// We already created the view, but we couldn't update the tab item (e.g.
					// view got created while printing from draft view)
					itemProps.Tag = rootSite;
					itemProps.Update = true;
				}
				else if (m_uncreatedViews.TryGetValue(viewType, out viewFactory))
					viewFactory.Create(itemProps);
				else
					return false;
			}

			RespondToSyncScrollingMsgs(false);
			bool ret = base.OnSwitchActiveView(args);
			using (RegistryBoolSetting showUSFMResources = new RegistryBoolSetting(m_app.ProjectSpecificSettingsKey,
				"USFMResourcesVisible" + m_selectedView.Name, false))
			{
				DisplayUsfmBrowser(showUSFMResources.Value);
				RespondToSyncScrollingMsgs(true);
				return ret;
			}
		}

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
		/// Make sure zooming isn't allowed for style bars.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool ZoomEnabledForView(SimpleRootSite view)
		{
			return !(view is DraftStyleBar);
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
				case kScrSBTabName: return kScrDraftViewSBItemName;
				case kBTSBTabName: return kBTDraftViewSBItemName;
				case kChkSBTabName: return kChkEditChecksSBItemName;
				case kPubSBTabName: break;
			}
			return base.GetDefaultItemForTab(tabName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the properties of a StyleInfo to the factory default settings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void SetPropsToFactorySettings(StyleInfo styleInfo)
		{
			TeStylesXmlAccessor.SetPropsToFactorySettings(styleInfo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes Windows messages.
		/// </summary>
		/// <param name="msg">The Windows Message to process.</param>
		/// ------------------------------------------------------------------------------------
		protected override void WndProc(ref Message msg)
		{
			if (msg.Msg == SantaFeFocusMessageHandler.FocusMsg)
				m_syncHandler.ReceiveFocusMessage(msg);

			base.WndProc(ref msg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save UI settings specific to this window.
		/// </summary>
		/// <param name="key">The Registry Key.</param>
		/// ------------------------------------------------------------------------------------
		protected override void SaveSettings(RegistryKey key)
		{
			base.SaveSettings(key);

			TeEditingHelper editingHelper = ActiveEditingHelper;
			if (editingHelper == null)
				return;

			try
			{
				TeProjectSettings.ShowUsfmResources = (m_usfmBrowser != null && m_usfmBrowser.Visible);
				if (m_usfmBrowser != null)
					m_usfmBrowser.SaveSettings(m_app.ProjectSpecificSettingsKey);

				try
				{
					MainWndSettingsKey.DeleteSubKeyTree(kBtWsRegEntryName);
				}
				catch
				{
					// ignore - easier than checking for existence of key
				}

				ILgWritingSystemFactory lgwsf = Cache.LanguageWritingSystemFactoryAccessor;

				if (DefaultBackTranslationWs != Cache.DefaultAnalWs)
				{
					using (RegistryStringSetting regDefBtWs = GetBtWsRegistrySetting(String.Empty))
						regDefBtWs.Value = lgwsf.GetStrFromWs(DefaultBackTranslationWs);
				}

				foreach (ISelectableView view in m_rgClientViews.Values)
				{
					IBtAwareView btView = view as IBtAwareView;
					if (btView != null && btView.BackTranslationWS > 0 &&
						btView.BackTranslationWS != DefaultBackTranslationWs)
					{
						using (RegistryStringSetting regViewBtWs = GetBtWsRegistrySetting(view.Name))
							regViewBtWs.Value = lgwsf.GetStrFromWs(btView.BackTranslationWS);
					}
				}
			}
			catch (Exception e)
			{
				// just ignore any exceptions. It really doesn't matter if SaveSettings() fail
				Console.WriteLine("TeMainWnd.SaveSettings: Exception caught: {0}", e.Message);
			}
		}
		#endregion

		#region Synchronization Methods
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

			if (sync == SyncMsg.ksyncWs)
			{
				// Moved the call to UpdateWritingSystemsInCombobox() from OnFileProjectProperties()
				// to here. This fixes the crash from TE-5337.
				// We have to reload the writing systems that are displayed in the combo box
				UpdateWritingSystemsInCombobox();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window synchronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <param name="sync">synchronization information record</param>
		/// <returns>false if caller should RefreshAllViews</returns>
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
					//REVIEW: can we skip the rest of this, if we know that a ksynchUndoRedo will do it for us later?
					RefreshAllViews();

					ParaStyleListHelper.IgnoreListRefresh = false;
					CharStyleListHelper.IgnoreListRefresh = false;
					InitStyleComboBox();
					return true;
				case SyncMsg.ksyncUndoRedo:
					//	RefreshAllViews();
					InitStyleComboBox();
					return true;
				case SyncMsg.ksyncWs:
					// We should be handling these messages with propchanges on the cache
					return true;
			}

			// Updating views in all windows. m_app should never be null unless
			// running from a test.
			if (m_app != null)
				return false; // causes a RefreshAllViews, and allows caller to notify its callers.

			RefreshAllViews(); // special case for testing.
			return true;
		}

		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the back translation writing system to use for the given named view.
		/// </summary>
		/// <param name="viewName">Name of the top-level (selectable) view.</param>
		/// <returns>the HVO of the back translation writing system</returns>
		/// ------------------------------------------------------------------------------------
		internal int GetBackTranslationWsForView(string viewName)
		{
			string icuLocale;
			using (var setting = GetBtWsRegistrySetting(viewName))
				icuLocale = setting.Value;
			int ws = string.IsNullOrEmpty(icuLocale) ? -1 :
				Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(icuLocale);
			return ws > 0 ? ws : m_defaultBackTranslationWs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Turns off all the active filters
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void TurnOffAllFilters()
		{
			CheckDisposed();
			OnNoFilters(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to set the selection immediately following the last character of the closest
		/// verse number to the requested verse. If no section exists within one chapter of the
		/// requested verse, the selection will not be changed.
		/// </summary>
		/// <param name="targetRef">Reference to seek</param>
		/// <returns>true if the selection is changed (to the requested verse or one nearby);
		/// false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool GotoVerse(ScrReference targetRef)
		{
			CheckDisposed();

			using (new WaitCursor(this))
			{
				// TE-3073: If the desired book is in the project but not in the filter then add it.
				IScrBook projBook = m_scr.FindBook(targetRef.Book);
				if (projBook != null && m_bookFilter.GetBookByOrd(targetRef.Book) == null)
					m_bookFilter.Add(projBook);

				// can only goto verse if we have an editing helper
				TeEditingHelper helper = ActiveEditingHelper;
				if (helper == null)
					return false;

				return helper.GotoVerse(targetRef);
			}
		}
		#endregion

		#region View-creation methods
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Creates the control based on the create info.
		///// </summary>
		///// <param name="sender">The caller.</param>
		///// <param name="createInfo">The create info previously specified by the client.</param>
		///// <returns>The newly created control.</returns>
		///// ------------------------------------------------------------------------------------
		//protected virtual Control CreateControl(object sender, object createInfo)
		//{
		//    CheckDisposed();

		//    ViewWrapper wrapper = sender as ViewWrapper;
		//    // We compare the type as string so that the type isn't referenced. This allows
		//    // the DLL where the type is defined to be loaded later when we actually execute
		//    // a method that references a type from that DLL.
		//    string createInfoType = createInfo.GetType().Name;
		//    if (createInfoType == "TeScrDraftViewProxy")
		//        return CreateDraftView(createInfo, wrapper);
		//    if (createInfoType == "DraftStylebarProxy")
		//        return CreateDraftStyleBar(createInfo);
		//    if (createInfoType == "ViewCreateInfo")
		//        return CreateFootnoteView(createInfo, wrapper);
		//    if (createInfoType == "ChecksDraftViewCreateInfo")
		//        return CreateCheckingDraftView(createInfo);
		//    if (createInfoType == "KeyTermRenderingsCreateInfo")
		//        return CreateKeyTermRenderingsControl();

		//    return null;
		//}
		#endregion

		#region IPageSetupCallbacks Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of header/footer set names that cannot be deleted.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public List<string> FactoryHeaderFooterSetNames
		{
			get { CheckDisposed();  return TePublicationsInit.FactoryHeaderFooterSets; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the size of the Normal style's font in millipoints. This property is used when
		/// the font size and line spacing are not specified explicitly in the publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int NormalFontSize
		{
			get { CheckDisposed(); return StyleSheet.NormalFontSize; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height of the normal line in millipoints. This property is used when
		/// the font size and line spacing are not specified explicitly in the publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int NormalLineHeight
		{
			get { CheckDisposed(); return ScripturePublication.GetNormalLineHeight(StyleSheet); }
		}
		#endregion

		#region IMessageFilter implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prefilters the message for special handling of shortcuts.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool PreFilterMessage(ref Message message)
		{
			if (message.Msg != (int)Win32.WinMsgs.WM_KEYDOWN)
				return false;

			// Check if the keypress is a toolbar or menu item shortcut. If so and the
			// toolbar or menu item is disabled, then eat the keypress so no child
			// windows have any chance of processing it. (TE-8078)
			Keys key = ((Keys)(int)message.WParam & Keys.KeyCode);
			key |= ModifierKeys;
			bool isItemEnabled = false;
			if (m_tmAdapter.IsShortcutKey(key, ref isItemEnabled) && !isItemEnabled && ContainsFocus)
				return true;

			if (ModifierKeys == Keys.Control && message.WParam == (IntPtr)Keys.B)
			{
				// Control-B was pressed
				if (m_tmAdapter != null)
				{
					TMBarProperties barProps = m_tmAdapter.GetBarProperties("tbGoto");
					// If the Scripture passage control is visible...
					if (barProps != null && barProps.Visible)
					{
						// set focus to this control.
						GotoReferenceControl.Focus();
						return true;
					}
				}
			}

			return false;
		}

		#endregion
	}
}
