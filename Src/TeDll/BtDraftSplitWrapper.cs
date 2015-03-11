// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BtDraftSplitWrapper.cs
// Responsibility: TE Team

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.SplitGridView;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class wraps together a style bar, a vernacular draft view, and a back-translation
	/// draft view, not to mention a footnote view or two (plus a kangaroo).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BtDraftSplitWrapper: ViewWrapper
	{
		#region Member variables
		private CoreWritingSystemDefinition m_VernWs;
		private IContainer m_Container;
		private DraftView m_mainDraftView;
		private DraftView m_mainBtView;
		private FootnoteView m_ftDraftView;
		private FootnoteView m_ftBtView;
		private RegistryKey m_settingsRegKey;
		private RegistryFloatSetting m_leftPaneWeight;
		private RegistryFloatSetting m_rightPaneWeight;
		#endregion

		#region Construction & Initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DraftViewWrapper"/> class.
		/// </summary>
		/// <param name="name">The name of the split grid</param>
		/// <param name="parent">The parent of the split wrapper (can be null). Will be replaced
		/// with real parent later.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="settingsRegKey">The settings reg key.</param>
		/// <param name="draftView">The vernacular Scripture draft view proxy.</param>
		/// <param name="stylebar">The Scripture stylebar proxy.</param>
		/// <param name="btDraftView">The BT Scripture draft view proxy.</param>
		/// <param name="footnoteDraftView">The vernacular footnote draft view proxy.</param>
		/// <param name="footnoteStylebar">The footnote stylebar proxy.</param>
		/// <param name="footnoteBtDraftView">The BT footnote draft view proxy.</param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "persistence gets disposed when m_Container gets disposed")]
		internal BtDraftSplitWrapper(string name, Control parent, FdoCache cache,
			IVwStylesheet styleSheet, RegistryKey settingsRegKey, TeScrDraftViewProxy draftView,
			DraftStylebarProxy stylebar, TeScrDraftViewProxy btDraftView,
			TeFootnoteDraftViewProxy footnoteDraftView, DraftStylebarProxy footnoteStylebar,
			TeFootnoteDraftViewProxy footnoteBtDraftView)
			: base(name, parent, cache, styleSheet, settingsRegKey, draftView, stylebar,
			footnoteDraftView, footnoteStylebar, 2, 3)
		{
			m_settingsRegKey = settingsRegKey;

			IRootSiteGroup group = GetGroup(kDraftRow, kDraftViewColumn);
			AddControl(group, kDraftRow, kBackTransColumn, btDraftView, true,
				!ChangeSides);

			group = GetGroup(kFootnoteRow, kDraftViewColumn);
			AddControl(group, kFootnoteRow, kBackTransColumn, footnoteBtDraftView, false,
				!ChangeSides);

			DataGridViewControlColumn frontTransColumn = GetColumn(kDraftViewColumn);
			frontTransColumn.FillWeight = 100;
			frontTransColumn.MaxPercentage = 0.9f;
			frontTransColumn.MinimumWidth = 60;
			frontTransColumn.IsCollapsible = false;
			frontTransColumn.Visible = true;

			DataGridViewControlColumn backTransColumn = GetColumn(kBackTransColumn);
			backTransColumn.FillWeight = 100;
			backTransColumn.MaxPercentage = 0.9f;
			backTransColumn.MinimumWidth = 60;
			backTransColumn.IsCollapsible = false;
			backTransColumn.Visible = true;

			// Now set up a persistence object for loading. This will be disposed automatically
			// when we dispose m_Container.
			m_Container = new FwContainer();
			Persistence persistence = new Persistence(m_Container, this);
			persistence.LoadSettings += new Persistence.Settings(OnLoadSettings);

			// Retrieve the parent's Persistence service. We use that for saving rather then
			// our own because otherwise some things like the FDO cache might already be disposed
			// when we try to save the settings.
			if (parent.Site != null)
			{
				persistence = (Persistence)parent.Site.GetService(typeof(Persistence));
				if (persistence != null)
					persistence.SaveSettings += new Persistence.Settings(OnSaveSettings);
			}

			m_leftPaneWeight = new RegistryFloatSetting(settingsRegKey, Name + "LeftPaneWeight", -1.0f);
			m_rightPaneWeight = new RegistryFloatSetting(settingsRegKey, Name + "RightPaneWeight", -1.0f);
		}
		#endregion

		#region Dispose stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="disposing"></param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				m_Container.Dispose();
			}
			m_Container = null;

			base.Dispose(disposing);
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to swap the front and back translation views.
		/// </summary>
		/// <value><c>true</c> if we're in a RTL project where the back translation is to the
		/// left of the draft view; <c>false</c> if we're in a LTR project where the back
		/// translation is to the right of the draft view.</value>
		/// ------------------------------------------------------------------------------------
		private bool ChangeSides
		{
			get
			{
				CheckDisposed();
				if (m_VernWs == null)
					m_VernWs = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
				return m_VernWs.RightToLeftScript;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the draft view column.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		protected override int kDraftViewColumn
		{
			get { return (ChangeSides) ? 2 : base.kDraftViewColumn; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the BT column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected int kBackTransColumn
		{
			get { return (ChangeSides) ? base.kDraftViewColumn : 2; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the vernacular draft view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DraftView VernDraftView
		{
			get
			{
				CheckDisposed();
				return DraftView;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the back-translation draft view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DraftView BTDraftView
		{
			get
			{
				CheckDisposed();
				return GetControl(kDraftRow, kBackTransColumn) as DraftView;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the front or back translation footnote window (depending on which one is
		/// active).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FootnoteView FootnoteView
		{
			get
			{
				CheckDisposed();
				return GetControl(kFootnoteRow,
					IsBtActivated ? kBackTransColumn : kDraftViewColumn) as FootnoteView;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the front of back translation side is activated.
		/// </summary>
		/// <value><c>true</c> if the back translation side is active, <c>false</c> if the front
		/// translation side is active.</value>
		/// ------------------------------------------------------------------------------------
		private bool IsBtActivated
		{
			get
			{
				return FocusedRootSite == GetControl(kDraftRow, kBackTransColumn) ||
					FocusedRootSite == GetControl(kFootnoteRow, kBackTransColumn);
			}
		}

		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the footnote view.
		/// </summary>
		/// <param name="footnote">Footnote to scroll to or null to just display the footnote view</param>
		/// <param name="fPutInsertionPtAtEnd">if set to <c>true</c> and showing the view,
		/// the insertion point will be put at the end of the footnote text instead of at the
		/// beginning, as would be appropriate in the case of a newly inserted footnote that has
		/// Reference Text. This parameter is ignored if footnote is null.</param>
		/// ------------------------------------------------------------------------------------
		internal override void ShowOrHideFootnoteView(IStFootnote footnote, bool fPutInsertionPtAtEnd)
		{
			CheckDisposed();

			bool fBtActivated = IsBtActivated;

			if (footnote != null && FootnoteViewShowing)
			{
				FootnoteView footnoteView = (FootnoteView)GetControl(kFootnoteRow,
					fBtActivated ? kBackTransColumn : kDraftViewColumn);
				footnoteView.ScrollToFootnote(footnote, fPutInsertionPtAtEnd);
			}
			else
				base.ShowOrHideFootnoteView(footnote, fPutInsertionPtAtEnd);

			SetFocus(fBtActivated);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the row height changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DataGridViewRowEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnRowHeightChanged(object sender, DataGridViewRowEventArgs e)
		{
			bool fHideFootnoteView = (!e.Row.Visible && m_grid.DisplayedRowCount(false) == 1);
			bool fBtActivated = (FocusedRootSite ==
				GetControl(FootnoteViewShowing ? kFootnoteRow : kDraftRow, kBackTransColumn));

			base.OnRowHeightChanged(sender, e);

			if (fHideFootnoteView)
			{
				// We have hidden the footnote view because it was the only row left. Now
				// restore the focus to the FT or BT side.
				SetFocus(fBtActivated);
			}
		}
		#endregion

		#region Load/Save Settings
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load size of columns and rows
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnLoadSettings(RegistryKey key)
		{
			CheckDisposed();

			float fillWeight = m_leftPaneWeight.Value;
			if (fillWeight > -1.0f)
				GetColumn(kDraftViewColumn).FillWeight = fillWeight;
			fillWeight = m_rightPaneWeight.Value;
			if (fillWeight > -1.0f)
				GetColumn(kBackTransColumn).FillWeight = fillWeight;

			string btViewWasActive = (string)key.GetValue(Name + "LastActivatedViewWasBTView", "True");
			m_CellToActivate = new Point(bool.Parse(btViewWasActive) ? kBackTransColumn : kDraftViewColumn, kDraftRow);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save size of columns and rows
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="GetColumn() returns a reference")]
		private void OnSaveSettings(RegistryKey key)
		{
			CheckDisposed();
			m_leftPaneWeight.Value = GetColumn(kDraftViewColumn).FillWeight;
			m_rightPaneWeight.Value = GetColumn(kBackTransColumn).FillWeight;
			key.SetValue(Name + "LastActivatedViewWasBTView",
				FocusedRootSite == GetControl(kDraftRow, kBackTransColumn));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a key in the registry where <see cref="T:SIL.FieldWorks.Common.Controls.Persistence"/> should store settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override RegistryKey SettingsKey
		{
			get { return m_settingsRegKey; }
		}

		#endregion

		#region Private/protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the focus to the front or back translation side.
		/// </summary>
		/// <param name="fFocusOnBtSide"><c>true</c> to set focus on BT side, <c>false</c> to
		/// set focus on FT side.</param>
		/// <remarks>Note: this method gets called only after showing or hiding the footnote
		/// pane. This means it is save to set the focus to the footnote row if the footnote
		/// pane is showing!</remarks>
		/// ------------------------------------------------------------------------------------
		private void SetFocus(bool fFocusOnBtSide)
		{
			// If the user was on the BT side we have to set the focus to the corresponding
			// view in the other row (i.e. if he was in the BT draft view we want to activate
			// the BT footnote view)
			Control control = GetControl(FootnoteViewShowing ? kFootnoteRow : kDraftRow,
				fFocusOnBtSide ? kBackTransColumn : kDraftViewColumn);
			if (((IRootSite)control).EditingHelper.CurrentSelection == null)
				((IRootSite)control).CastAsIVwRootSite().RootBox.MakeSimpleSel(true, true, false, true);

			if (control is SimpleRootSite)
				((SimpleRootSite)control).ScrollSelectionIntoView(null, VwScrollSelOpts.kssoDefault);

			control.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Connect each BT view to its corresponding main view.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.ControlEventArgs"/> that
		/// contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnControlAdded(ControlEventArgs e)
		{
			base.OnControlAdded(e);
			if (e.Control is DataGridView)
				e.Control.ControlAdded += DataGridView_ControlAdded;

			if (e.Control is DraftView)
			{
				DraftView view = (DraftView)e.Control;
				if (view.IsBackTranslation)
					m_mainBtView = view;
				else
					m_mainDraftView = view;
				if (m_mainBtView != null && m_mainDraftView != null)
					m_mainBtView.VernacularDraftView = m_mainDraftView;
			}
			else if (e.Control is FootnoteView)
			{
				FootnoteView ftView = (FootnoteView)e.Control;
				if (ftView.IsBackTranslation)
					m_ftBtView = ftView;
				else
					m_ftDraftView = ftView;
				if (m_ftBtView != null && m_ftDraftView != null)
					m_ftBtView.VernacularDraftView = m_ftDraftView;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ControlAdded event of the Control control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.ControlEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void DataGridView_ControlAdded(object sender, ControlEventArgs e)
		{
			OnControlAdded(e);
		}
		#endregion
	}
}
