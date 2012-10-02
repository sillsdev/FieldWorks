// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2005' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ChecksViewWrapper.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ControlExtenders;
using Microsoft.Win32;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.SplitGridView;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IChecksViewWrapperView
	{
		///// <summary>Gets or sets the settings key.</summary>
		//RegistryKey SettingsKey { get; set; }
		/// <summary>Gets or sets the persistence object.</summary>
		Persistence Persistence { get; set; }
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds a collection of view windows that make up a checking View
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class ChecksViewWrapper : UserControl, ISelectableView, IRootSite, ISettings,
		IxCoreColleague
	{
		#region Enumeration
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public enum CheckView
		{
			/// <summary></summary>
			Grid,
			/// <summary></summary>
			Draft,
			/// <summary></summary>
			Tree,
			/// <summary></summary>
			None
		}
		#endregion

		#region Data members
		/// <summary>registry settings</summary>
		protected Persistence m_persistence;
		/// <summary></summary>
		protected RegistryKey m_settingsKey;
		/// <summary>The editing helper of the (lower right) draft view pane.</summary>
		protected TeEditingHelper m_draftViewEditingHelper;
		/// <summary></summary>
		protected FwMainWnd m_mainWnd;
		/// <summary></summary>
		protected UserControl m_gridControl;
		/// <summary></summary>
		protected IRootSite m_draftView;
		/// <summary>control containing the toolstrip and the key terms tree</summary>
		protected CheckControl m_treeContainer;
		/// <summary></summary>
		protected bool m_fShownBefore;
		/// <summary></summary>
		protected bool m_fInitialActivation = true;
		/// <summary></summary>
		protected FdoCache m_cache;
		/// <summary>The Scripture object</summary>
		protected IScripture m_scr;
		/// <summary>
		/// base caption string that this client window should display in the info bar
		/// </summary>
		protected string m_baseInfoBarCaption;
		/// <summary></summary>
		protected bool m_fDisplayUI = true;
		/// <summary></summary>
		protected DockExtender m_dockExtender;
		/// <summary></summary>
		protected IFloaty m_floaty;
		/// <summary></summary>
		protected FwSplitContainer m_rightView;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a ChecksViewWrapper
		/// </summary>
		/// <param name="parent">The parent of the split wrapper (can be null). Will be replaced
		/// with real parent later.</param>
		/// <param name="cache">The Cache to give to the views</param>
		/// <param name="draftViewProxy">View proxy for creating the draft view to display in
		/// the lower right corner</param>
		/// <param name="settingsRegKey">The parent control's ISettings registry key</param>
		/// ------------------------------------------------------------------------------------
		public ChecksViewWrapper(Control parent, FdoCache cache, ViewProxy draftViewProxy,
			RegistryKey settingsRegKey)
		{
			SuspendLayout();
			m_cache = cache;
			m_scr = m_cache.LangProject.TranslatedScriptureOA;
			m_mainWnd = parent as FwMainWnd;

			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
			Dock = DockStyle.Fill;
			Name = "ChecksViewWrapper";
			Visible = false;

			// If we have a main window, then ignore the one sent to us and use the main
			// window's extended settings key. Otherwise use the one sent to us.
			SettingsKey = settingsRegKey;

			if (parent != null)
				Site = parent.Site;

			// Set up the right view
			m_rightView = new FwSplitContainer(SettingsKey);
			m_rightView.Name = "ChecksRightView";
			m_rightView.Orientation = Orientation.Horizontal;
			m_rightView.DesiredFirstPanePercentage = 0.7f;
			m_rightView.PanelToActivate = m_rightView.Panel1;
			m_rightView.Panel1Collapsed = false;
			m_rightView.Panel2Collapsed = false;
			m_rightView.Tag = draftViewProxy;
			m_rightView.Dock = DockStyle.Fill;
			Controls.Add(m_rightView);

			// Note: setting parent needs to come last
			Parent = parent;

			ResumeLayout();

			// This should only be null when running tests.
			if (m_mainWnd != null)
				m_mainWnd.Mediator.AddColleague(this);
		}

		#endregion

		#region IDisposable override
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this before doing anything else.
		/// </summary>
		/// <remarks>This method is thread safe.</remarks>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(
					string.Format("'{0}' in use after being disposed.", GetType().Name));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (disposing)
			{
				// NOTE: don't dispose m_gridControl, m_draftView and m_treeContainer here.
				// They all got added to a Controls collection and will be disposed when the
				// base class disposes below.

				// This should only be null when running tests.
				if (m_mainWnd != null && m_mainWnd.Mediator != null)
					m_mainWnd.Mediator.RemoveColleague(this);

				if (m_dockExtender != null)
					m_dockExtender.Dispose();
			}

			m_dockExtender = null;
			m_draftView = null;
			m_treeContainer = null;
			m_gridControl = null;

			base.Dispose(disposing);
		}

		#endregion // IDisposable override

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to display UI.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool DisplayUI
		{
			get { return m_fDisplayUI; }
			set { m_fDisplayUI = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the draft view that's part of the checking view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IRootSite DraftView
		{
			get
			{
				CheckDisposed();
				return m_draftView;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the piece of the checking view that's active.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckView ActiveCheckingView
		{
			get
			{
				CheckDisposed();

				if (m_treeContainer.ContainsFocus)
					return CheckView.Tree;

				if (m_gridControl.ContainsFocus)
					return CheckView.Grid;

				if (DraftView != null && ((Control)DraftView).ContainsFocus)
					return CheckView.Draft;

				return CheckView.None;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the editing helper.
		/// </summary>
		/// <value>The editing helper.</value>
		/// ------------------------------------------------------------------------------------
		protected TeEditingHelper EditingHelper
		{
			get { return (m_draftView == null ? null : m_draftView.EditingHelper as TeEditingHelper); }
		}
		#endregion

		#region Event stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when to enable or disable the enable send sync message button.
		/// </summary>
		/// <param name="args">The args.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateEnableSendSyncMessage(object args)
		{
			CheckDisposed();

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null && m_draftView.EditingHelper != null)
			{
				itemProps.Enabled = true;
				itemProps.Checked = TeProjectSettings.SendSyncMessages;
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when to enable or disable the enable receive sync message button.
		/// </summary>
		/// <param name="args">The args.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool OnUpdateEnableReceiveSyncMessage(object args)
		{
			CheckDisposed();

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps != null && m_draftView.EditingHelper != null)
			{
				itemProps.Enabled = true;
				itemProps.Checked = TeProjectSettings.ReceiveSyncMessages;
				itemProps.Update = true;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			Form frm = FindForm();
			if (frm != null)
				frm.VisibleChanged += frm_VisibleChanged;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the main window gets hidden or shown and the docking window is undocked, make
		/// sure its visible state tracks with the main window's state.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void frm_VisibleChanged(object sender, EventArgs e)
		{
			if (m_floaty != null && m_floaty.DockMode == DockStyle.None)
			{
				if (((Form)sender).Visible)
					m_floaty.Show();
				else
					m_floaty.Hide();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the visible changed event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);

			if (!m_fShownBefore && Visible)
				Initialize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the check control.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected abstract CheckControl CreateCheckControl();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the grid control.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual UserControl CreateGridControl(FwMainWnd mainWnd)
		{
			throw new NotImplementedException("Must implement in subclass");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void Initialize()
		{
			if (m_fShownBefore)
				return;

			// Create a FwSplitContainer with two draft views.
			Control draftView = ((ViewProxy)m_rightView.Tag).CreateView(this);
			draftView.Dock = DockStyle.Fill;
			m_draftView = draftView as IRootSite;
			if (draftView is ISelectableView)
				((ISelectableView)draftView).BaseInfoBarCaption = m_baseInfoBarCaption;

			// Create a draft view of Scripture in the project.
			m_rightView.Panel2.Controls.Add(draftView);

			// Create a view for the list of renderings for the selected key term
			m_gridControl = CreateGridControl(m_mainWnd);
			m_gridControl.Dock = DockStyle.Fill;

			if (m_gridControl is ISelectableView)
				((ISelectableView)m_gridControl).BaseInfoBarCaption = m_baseInfoBarCaption;

			if (m_gridControl is IChecksViewWrapperView)
				((IChecksViewWrapperView)m_gridControl).Persistence = m_persistence;

			m_rightView.Panel1.Controls.Add(m_gridControl);

			// Create a key terms control (containing the tool strip and tree).
			// Subscribe to events so that the enabled status of the tool strip buttons can be updated.
			m_treeContainer = CreateCheckControl();
			m_treeContainer.Dock = DockStyle.Left;

			m_treeContainer.Persistence = m_persistence;

			Controls.Add(m_treeContainer);

			m_dockExtender = new DockExtender(this);
			m_floaty = m_dockExtender.Attach(m_treeContainer, m_treeContainer.ToolStrip, true, m_persistence);
			m_floaty.DockOnInside = false; // outside
			m_floaty.HideHandle = false;
			m_floaty.AllowedDocking = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
			m_floaty.ShowCloseButton = false;
			m_treeContainer.Floaty = m_floaty;
			m_fShownBefore = true;
		}

		#endregion

		#region Load/Save Settings
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the view settings: zoom settings for the bottom draft view and the keyterms
		/// rendering view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void OnLoadSettings(RegistryKey key)
		{
			CheckDisposed();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the settings for the keyterms view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnSaveSettings(RegistryKey key)
		{
			CheckDisposed();
		}

		#endregion

		#region Private properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the wrapper is in the midst of being
		/// activated for the first time.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool InitialActivation
		{
			get { return m_fInitialActivation; }
		}

		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activate the view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void ActivateView()
		{
			CheckDisposed();

			bool fShownBefore = m_fShownBefore;

			if (!m_fShownBefore)
				Initialize();

			PerformLayout();
			Show();
			m_dockExtender.Floaties.Find(m_treeContainer).FloatingContainerBounds = m_treeContainer.Bounds;

			if (!fShownBefore)
				OnLoadSettings(SettingsKey);

			if (m_draftView is ISelectableView)
				((ISelectableView)m_draftView).ActivateView();

			m_fInitialActivation = false;
		}

		#endregion

		#region ISelectableView Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activate the view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ISelectableView.ActivateView()
		{
			ActivateView();

			ISelectableView grid = m_gridControl as ISelectableView;
			if (grid != null)
				grid.ActivateView();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make it inactive by hiding it.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		void ISelectableView.DeactivateView()
		{
			CheckDisposed();

			Enabled = false;
			Visible = false;
			Enabled = true;

			ISelectableView grid = m_gridControl as ISelectableView;
			if (grid != null)
				grid.DeactivateView();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the base caption string that should display in
		/// the info bar. This is used only if this is the client window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ISelectableView.BaseInfoBarCaption
		{
			get
			{
				CheckDisposed();
				if (ActiveControl is ISelectableView)
					return ((ISelectableView)ActiveControl).BaseInfoBarCaption;
				return m_baseInfoBarCaption;
			}
			set
			{
				CheckDisposed();
				if (ActiveControl is ISelectableView)
					((ISelectableView)ActiveControl).BaseInfoBarCaption = value;
				m_baseInfoBarCaption = (value == null) ? null :
					value.Replace(Environment.NewLine, " ");
			}
		}

		#endregion

		#region IRootSite Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the complete list of rootboxes used within this IRootSite control.
		/// The resulting list may contain zero or more items.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		List<IVwRootBox> IRootSite.AllRootBoxes()
		{
			return new List<IVwRootBox>();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets sets whether or not to allow painting on the view
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		bool IRootSite.AllowPainting
		{
			get
			{
				if (m_draftView != null)
					return m_draftView.AllowPainting;
				return false;
			}
			set { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows the IRootSite to be cast as an IVwRootSite
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		IVwRootSite IRootSite.CastAsIVwRootSite()
		{
			if (m_draftView != null)
				return m_draftView.CastAsIVwRootSite();
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows forcing the closing of root boxes. This is necessary when an instance of a
		/// SimpleRootSite is created but never shown so it's handle doesn't get created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void IRootSite.CloseRootBox()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the editing helper for this IRootsite.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		EditingHelper IRootSite.EditingHelper
		{
			get { return EditingHelper; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the width available for laying things out in the view.
		/// Return the layout width for the window, depending on whether or not there is a
		/// scroll bar. If there is no scroll bar, we pretend that there is, so we don't have
		/// to keep adjusting the width back and forth based on the toggling on and off of
		/// vertical and horizontal scroll bars and their interaction.
		/// The return result is in pixels.
		/// The only common reason to override this is to answer instead a very large integer,
		/// which has the effect of turning off line wrap, as everything apparently fits on
		/// a line.
		/// </summary>
		/// <param name="prootb">The root box</param>
		/// <returns>Width available for layout</returns>
		/// ------------------------------------------------------------------------------------
		int IRootSite.GetAvailWidth(IVwRootBox prootb)
		{
			if (m_draftView != null)
				return m_draftView.GetAvailWidth(prootb);
			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the display :)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void IRootSite.RefreshDisplay()
		{
			if (m_draftView != null)
				m_draftView.RefreshDisplay();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll the selection in view and set the IP at the given client position.
		/// </summary>
		/// <param name="sel">The selection</param>
		/// <param name="dyPos">Position from top of client window where IP should be set</param>
		/// ------------------------------------------------------------------------------------
		bool IRootSite.ScrollSelectionToLocation(IVwSelection sel, int dyPos)
		{
			if (m_draftView != null)
				return m_draftView.ScrollSelectionToLocation(sel, dyPos);
			return false;
		}
		#endregion

		#region ISettings Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implement this method to save the persistent settings for your class. Normally, this
		/// should also result in recursively calling SaveSettingsNow for all children which
		/// also implement ISettings.
		/// </summary>
		/// <remarks>Note: A form's implementation of
		/// <see cref="M:SIL.FieldWorks.Common.Controls.ISettings.SaveSettingsNow"/> normally
		/// calls <see cref="M:SIL.FieldWorks.Common.Controls.Persistence.SaveSettingsNow(System.Windows.Forms.Control)"/>
		/// (after optionally saving any class-specific properties).</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void SaveSettingsNow()
		{
			CheckDisposed();
			if (m_persistence != null)
				m_persistence.SaveSettingsNow(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a key in the registry where <see cref="T:SIL.FieldWorks.Common.Controls.Persistence"/>
		/// should store settings.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public virtual RegistryKey SettingsKey
		{
			get { return m_settingsKey; }
			protected set
			{
				if (m_persistence != null)
				{
					m_persistence.LoadSettings -= OnLoadSettings;
					m_persistence.SaveSettings -= OnSaveSettings;
					m_persistence.Dispose();
					m_persistence = null;
				}

				m_settingsKey = value;
				if (m_settingsKey != null)
				{
					m_persistence = new Persistence(this, OnLoadSettings, OnSaveSettings);
					m_persistence.EnableSaveWindowSettings = false;
				}
			}
		}

		#endregion

		#region IxCoreColleague Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the message targets.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] { this };
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No-op.
		/// </summary>
		/// <param name="mediator">The mediator.</param>
		/// <param name="configurationParameters">The configuration parameters.</param>
		/// ------------------------------------------------------------------------------------
		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			// Not used.
		}

		#endregion
	}
}
