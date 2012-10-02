// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DraftViewBase.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using Microsoft.Win32;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class DraftViewBase : FwRootSite, ITeDraftView, ITeView, IBtAwareView, ISettings
	{
		#region Data members
		private IContainer components;
		/// <summary>Object to enable persistence of settings</summary>
		protected Persistence m_persistence;
		/// <summary>The location tracker</summary>
		protected readonly LocationTrackerImpl m_locationTracker;
		/// <summary>The app</summary>
		protected readonly IApp m_app;
		/// <summary>Tag that identifies the book filter instance</summary>
		protected readonly int m_filterInstance;
		/// <summary>The book filter</summary>
		protected readonly FilteredScrBooks m_bookFilter;
		/// <summary>Initial writing system to use if this is a back translation view (only
		/// used in MakeRoot -- after that, use BackTranslationWs)</summary>
		private readonly int m_initialBtWs;
		/// <summary>Initial editable state (only used in CreateEditingHelper -- after that,
		/// use Editable)</summary>
		protected readonly bool m_initialEditableState;
		private RegistryFloatSetting m_zoomFactor;
		/// <summary>The view contructor for this view</summary>
		protected TeStVc m_vc;
		/// <summary>If this is a BT view, this is the corresponding vernacular view</summary>
		protected ITeDraftView m_vernDraftView;
		/// <summary>Bit-flags indicating type of view</summary>
		protected readonly TeViewType m_viewType;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DraftViewBase"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="filterInstance">The tag that identifies the book filter instance.</param>
		/// <param name="app">The application.</param>
		/// <param name="viewName">The name of the view.</param>
		/// <param name="fEditable"><c>true</c> if view is to be editable.</param>
		/// <param name="viewType">Bit-flags indicating type of view.</param>
		/// <param name="btWs">The back translation writing system (if needed).</param>
		/// ------------------------------------------------------------------------------------
		public DraftViewBase(FdoCache cache, int filterInstance, IApp app, string viewName,
			bool fEditable, TeViewType viewType, int btWs)
			: base(cache)
		{
			InitializePersistence();

			m_filterInstance = filterInstance;
			m_app = app;
			AccessibleName = Name = viewName;

			m_initialEditableState = fEditable;
			m_viewType = viewType;
			m_initialBtWs = btWs;
			m_locationTracker = new LocationTrackerImpl(cache, m_filterInstance, ContentType);
			m_bookFilter = cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(m_filterInstance);

			BackColor = EditableColor;
			DoSpellCheck = TeProjectSettings.ShowSpellingErrors;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the persistence thingy
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializePersistence()
		{
			components = new Container();
			m_persistence = new Persistence(components);
			((ISupportInitialize)m_persistence).BeginInit();
			m_persistence.EnableSaveWindowSettings = false;
			m_persistence.Parent = this;
			m_persistence.SaveSettings += OnSaveSettings;
			m_persistence.LoadSettings += OnLoadSettings;
			((ISupportInitialize)m_persistence).EndInit();
		}
		#endregion

		#region Dispose crud
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
					components.Dispose();
				var disposable = m_vc as IDisposable;
				if (disposable != null)
					disposable.Dispose();
				if (m_zoomFactor != null)
					m_zoomFactor.Dispose();
			}
			m_vc = null;
			m_zoomFactor = null;
			base.Dispose(disposing);
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The view constructor "fragment" associated with the root object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract int RootFrag { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating the initial editable state of the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool InitialEditableState
		{
			get { return m_initialEditableState; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If this is a back translation view, this is the corresponding vernacular view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITeDraftView VernacularDraftView
		{
			get { return m_vernDraftView; }
			set
			{
				m_vernDraftView = value;
				Debug.Assert(m_vernDraftView != this && m_vernDraftView.RootBox != RootBox);
				Debug.Assert(GetType() == value.GetType());
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the FDO cache
		/// </summary>
		/// <value>A <see cref="FdoCache"/></value>
		/// -----------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override FdoCache Cache
		{
			set
			{
				if (m_fdoCache != null && m_fdoCache != value)
					throw new InvalidOperationException("Changing the cache after its already been set is bad!");
				base.Cache = value;
			}
#if __MonoCS__
			// TODO-Linux: work around mono bug: https://bugzilla.novell.com/show_bug.cgi?id=494822
			get { return base.Cache;}
#endif
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The color the window should be if it is editable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal Color EditableColor
		{
			get
			{
				return ContentType == StVc.ContentTypes.kctSegmentBT ? TeResourceHelper.ReadOnlyTextBackgroundColor : SystemColors.Window;
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
				return m_bookFilter;
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
		/// Gets the type of the content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual StVc.ContentTypes ContentType
		{
			get
			{
				if (IsBackTranslation)
					return Options.UseInterlinearBackTranslation ? StVc.ContentTypes.kctSegmentBT : StVc.ContentTypes.kctSimpleBT;
				return StVc.ContentTypes.kctNormal;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the default WS of the view constructor. If the view has not yet been
		/// constructed, this returns -1.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ViewConstructorWS
		{
			get
			{
				CheckDisposed();
				return m_vc == null ? -1 : m_vc.DefaultWs;
			}
			set
			{
				CheckDisposed();

				m_vc.DefaultWs = value;
				RefreshDisplay();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets whether this view displays a back translation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsBackTranslation
		{
			get
			{
				CheckDisposed();
				return (m_viewType & TeViewType.BackTranslation) != 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the back translation writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BackTranslationWS
		{
			get
			{
				CheckDisposed();
				return !IsBackTranslation ? 0 : ViewConstructorWS;
			}
			set
			{
				CheckDisposed();
				if (IsBackTranslation)
					ViewConstructorWS = value;
			}
		}
		#endregion

		#region Virtual methods
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
			Editable = m_initialEditableState;

			// Check for non-existing rootbox before creating a new one. By doing that we
			// can mock the rootbox in our tests. However, this might cause problems in our
			// real code - altough I think MakeRoot() should be called only once.
			if (m_rootb == null)
				m_rootb = MakeRootBox();

			m_rootb.SetSite(this);

			HorizMargin = 10;

			// Set up a new view constructor.
			m_vc = CreateViewConstructor();
			m_vc.Name = Name + " VC";
			m_vc.HeightEstimator = (Group == null) ? this : Group as IHeightEstimator;
			m_vc.Cache = m_fdoCache;
			m_vc.ContentType = ContentType;
			m_vc.BackColor = BackColor;
			m_vc.Editable = EditingHelper.Editable;

			if (IsBackTranslation)
				m_vc.DefaultWs = m_initialBtWs;

			m_rootb.DataAccess = new ScrBookFilterDecorator(m_fdoCache, m_filterInstance);

			MakeRootObject();
			base.MakeRoot();

			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.

			Synchronize(m_rootb);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the root box.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual IVwRootBox MakeRootBox()
		{
			return VwRootBoxClass.Create();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This part of MakeRoot is isolated so that it can be overridden in vertical draft view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void MakeRootObject()
		{
			m_rootb.SetRootObject(m_fdoCache.LangProject.TranslatedScriptureOA.Hvo,
				m_vc, RootFrag, m_styleSheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a view constructor suitable for this kind of view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract TeStVc CreateViewConstructor();

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

			// Ensure Information bar is set correctly. FWNX-244
			var teEditingHelper = (EditingHelper as TeEditingHelper);
			if (teEditingHelper != null)
				teEditingHelper.SetInformationBarForSelection();
		}
		#endregion

		#region ISettings implementation
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
				return TheMainWnd != null ? TheMainWnd.MainWndSettingsKey : null;
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
		protected virtual void OnLoadSettings(RegistryKey key)
		{
			CheckDisposed();
			m_zoomFactor = new RegistryFloatSetting(key, "ZoomFactor" + Name, 1.5f);
			Zoom = m_zoomFactor.Value;
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Save passage to the registry
		/// </summary>
		/// <param name="key">Location in the registry</param>
		///-------------------------------------------------------------------------------------
		protected virtual void OnSaveSettings(RegistryKey key)
		{
			CheckDisposed();
			m_zoomFactor.Value = Zoom;
		}
		#endregion

		#region Overrides of SimpleRootSite
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to update the spell check status, and .
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool RefreshDisplay()
		{
			DoSpellCheck = TeProjectSettings.ShowSpellingErrors;
			if (m_vc != null)
				m_vc.HvoOfSegmentWhoseBtPromptIsToBeSupressed = 0;
			base.RefreshDisplay();
			//Enhance: if all Refreshable descendant controls have been refreshed return true (using above line in result)
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.LostFocus"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);

			// If this is the segmented BT view, then make sure we remove the highlighting
			// for the corresponding vernacular view. (FWR-1337)
			if (VernacularDraftView != null && VernacularDraftView.Vc != null)
				VernacularDraftView.Vc.SetupOverrides(null, -1, -1, null, VernacularDraftView.RootBox);
		}
		#endregion

		#region ITeDraftView Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide access to our View Constructor so the editing helper can set which paragraph
		/// segment should be highlighted when a BT segment is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TeStVc Vc
		{
			get { return m_vc; }
		}
		#endregion
	}
}
