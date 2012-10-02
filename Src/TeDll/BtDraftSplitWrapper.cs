// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BtDraftSplitWrapper.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Win32;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.SplitGridView;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;

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
		private LgWritingSystem m_VernWs;
		private IContainer m_Container;
		private DraftView m_mainDraftView;
		private DraftView m_mainBtView;
		private DraftView m_ftDraftView;
		private DraftView m_ftBtView;
		#endregion

		#region Construction & Initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DraftViewWrapper"/> class.
		/// </summary>
		/// <param name="name">The name of the split grid</param>
		/// <param name="parent">The parent of the split wrapper (can be null). Will be replaced
		/// with real parent later.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="settingsRegKey">The settings reg key.</param>
		/// <param name="draftView">The draft view.</param>
		/// <param name="stylebar">The stylebar.</param>
		/// <param name="btDraftView">The bt draft view.</param>
		/// <param name="footnoteDraftView">The footnote draft view.</param>
		/// <param name="footnoteStylebar">The footnote stylebar</param>
		/// <param name="footnoteBtDraftView">The footnote bt draft view.</param>
		/// ------------------------------------------------------------------------------------
		public BtDraftSplitWrapper(string name, Control parent, FdoCache cache,
			IVwStylesheet styleSheet, RegistryKey settingsRegKey, object draftView,
			object stylebar, object btDraftView, object footnoteDraftView,
			object footnoteStylebar, object footnoteBtDraftView)
			: base(name, parent, cache, styleSheet, settingsRegKey, draftView, stylebar,
			footnoteDraftView, footnoteStylebar, 2, 3)
		{
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
			// we don't want to implement ISettings, so we set the default key path instead,
			// but we have to strip the HKCU\ of the name.
			persistence.DefaultKeyPath =
				settingsRegKey.Name.Substring(Registry.CurrentUser.Name.Length + 1);

			// Retrieve the parent's Persistence service. We use that for saving rather then
			// our own because otherwise some things like the FDO cache might already be disposed
			// when we try to save the settings.
			if (parent.Site != null)
			{
				persistence = (Persistence)parent.Site.GetService(typeof(Persistence));
				if (persistence != null)
					persistence.SaveSettings += new Persistence.Settings(OnSaveSettings);
			}
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
					m_VernWs = new LgWritingSystem(m_cache, m_cache.DefaultVernWs);
				return m_VernWs.RightToLeft;
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
			get
			{
				if (ChangeSides)
					return 2;
				return base.kDraftViewColumn;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the BT column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected int kBackTransColumn
		{
			get
			{
				if (ChangeSides)
					return base.kDraftViewColumn;
				return 2;
			}
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
		internal override void ShowOrHideFootnoteView(StFootnote footnote, bool fPutInsertionPtAtEnd)
		{
			CheckDisposed();

			bool fBtActivated = IsBtActivated;

			if (!FootnoteViewShowing)
			{
				base.ShowOrHideFootnoteView(footnote, fPutInsertionPtAtEnd);
			}
			else if (footnote != null)
			{
				FootnoteView footnoteView = (FootnoteView)GetControl(kFootnoteRow,
					fBtActivated ? kBackTransColumn : kDraftViewColumn);
				footnoteView.ScrollToFootnote(footnote, fPutInsertionPtAtEnd);
			}

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

			float fillWeight = RegistryHelper.ReadFloatSetting(key, Name + "LeftPaneWeight",
				-1.0f);
			if (fillWeight > -1.0f)
				GetColumn(kDraftViewColumn).FillWeight = fillWeight;
			fillWeight = RegistryHelper.ReadFloatSetting(key, Name + "RightPaneWeight", -1.0f);
			if (fillWeight > -1.0f)
				GetColumn(kBackTransColumn).FillWeight = fillWeight;

			string btViewWasActive = (string)key.GetValue(Name + "LastActivatedViewWasBTView",
				"True");
			m_CellToActivate =
				new Point(bool.Parse(btViewWasActive) ? kBackTransColumn : kDraftViewColumn,
				kDraftRow);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save size of columns and rows
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnSaveSettings(RegistryKey key)
		{
			CheckDisposed();

			RegistryHelper.WriteFloatSetting(key, Name + "LeftPaneWeight",
				GetColumn(kDraftViewColumn).FillWeight);
			RegistryHelper.WriteFloatSetting(key, Name + "RightPaneWeight",
				GetColumn(kBackTransColumn).FillWeight);
			key.SetValue(Name + "LastActivatedViewWasBTView",
				FocusedRootSite == GetControl(kDraftRow, kBackTransColumn));
		}

		#endregion

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

		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// BtDraftSplitWrapper
			//
			this.AccessibleName = "BtDraftSplitWrapper";
			this.ResumeLayout(false);

		}

		/// <summary>
		/// Connect each BT view to its corresponding main view.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnControlAdded(ControlEventArgs e)
		{
			base.OnControlAdded(e);
			if (e.Control is DataGridView)
				e.Control.ControlAdded += new ControlEventHandler(Control_ControlAdded);
			if (!(e.Control is DraftView))
				return;
			TE.DraftView view = e.Control as DraftView;
			TeViewType t = view.ViewType;
			if ((t & TeViewType.BackTranslation) == 0 && (t & TeViewType.FootnoteView) == 0)
			{
				m_mainDraftView = view;
				if (m_mainBtView != null)
					m_mainBtView.MainTrans = m_mainDraftView;
			}
			else if ((t & TeViewType.BackTranslation) != 0 && (t & TeViewType.FootnoteView) == 0)
			{
				m_mainBtView = view;
				if (m_mainDraftView != null)
					m_mainBtView.MainTrans = m_mainDraftView;
			}
			else if ((t & TeViewType.BackTranslation) == 0 && (t & TeViewType.FootnoteView) != 0)
			{
				m_ftDraftView = view;
				if (m_ftBtView != null)
					m_ftBtView.MainTrans = m_ftDraftView;
			}
			else if ((t & TeViewType.BackTranslation) == 0 && (t & TeViewType.FootnoteView) == 0)
			{
				m_ftBtView = view;
				if (m_ftDraftView != null)
					m_ftBtView.MainTrans = m_ftDraftView;
			}

		}

		void Control_ControlAdded(object sender, ControlEventArgs e)
		{
			OnControlAdded(e);
		}
	}
}
