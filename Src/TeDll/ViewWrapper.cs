// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ViewWrapper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.SplitGridView;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.FwUtils;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;
using SIL.FieldWorks.TE.TeEditorialChecks;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This ViewWrapper class holds two rows with two columns: a column for the style bar and a
	/// column for the scripture. We display the draft view in the upper row and possibly a
	/// footnote view in the bottom row.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ViewWrapper : SplitGrid, IViewFootnotes, ISettings, IBtAwareView
	{
		#region Member variables
		/// <summary>Stores the width of the window area allotted to the style pane.
		/// If this is 0, the style pane is hidden.</summary>
		private RegistryIntSetting m_stylePaneWidth;
		#endregion

		#region Construction and initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor - used for Designer
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ViewWrapper()
		{
		}

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
		/// <param name="footnoteDraftView">The footnote draft view.</param>
		/// <param name="footnoteStylebar">The footnote stylebar</param>
		/// <param name="rows">The number of rows.</param>
		/// <param name="columns">The number of columns.</param>
		/// <remarks>If the view wrapper does not have a stylebar, then override the
		/// kStyleColumn property to a negative value so that it will be ignored.</remarks>
		/// ------------------------------------------------------------------------------------
		protected ViewWrapper(string name, Control parent, FdoCache cache, IVwStylesheet styleSheet,
			RegistryKey settingsRegKey, object draftView, object stylebar,
			object footnoteDraftView, object footnoteStylebar, int rows, int columns)
			: base(cache, styleSheet, rows, columns)
		{
			Dock = DockStyle.Fill;
			Name = name;
			m_stylePaneWidth = new RegistryIntSetting(settingsRegKey, name + "StyleWidth", 0);
			if (parent != null)
				Site = parent.Site;

			IRootSiteGroup group = CreateGroup((int)TeViewGroup.Scripture);
			if (kStyleColumn >= 0)
				AddControl(group, kDraftRow, kStyleColumn, stylebar);
			AddControl(group, kDraftRow, kDraftViewColumn, draftView, true, true);
			group = CreateGroup((int)TeViewGroup.Footnote);
			if (kStyleColumn >= 0)
				AddControl(group, kFootnoteRow, kStyleColumn, footnoteStylebar);
			AddControl(group, kFootnoteRow, kDraftViewColumn, footnoteDraftView, false, true);

			GetRow(kFootnoteRow).FillWeight = 18;
			m_grid.Rows[kDraftRow].Visible = true;
			m_grid.Columns[kDraftViewColumn].Visible = true;
			m_grid.ColumnWidthChanged += new DataGridViewColumnEventHandler(OnColumnWidthChanged);
			m_grid.RowHeightChanged += new DataGridViewRowEventHandler(OnRowHeightChanged);

			if (kStyleColumn >= 0)
			{
				DataGridViewControlColumn styleColumn = GetColumn(kStyleColumn);
				styleColumn.ThresholdWidth = 20;
				styleColumn.StandardWidth = 100;
				styleColumn.MaxPercentage = 0.5f;
				styleColumn.IsCollapsible = true;
				styleColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
				if (m_stylePaneWidth.Value > 0)
				{
					styleColumn.Width = Math.Max(m_stylePaneWidth.Value, styleColumn.ThresholdWidth);
					styleColumn.Visible = true;
				}
			}
		}

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
		/// <param name="footnoteDraftView">The footnote draft view.</param>
		/// <param name="footnoteStylebar">The footnote stylebar</param>
		/// ------------------------------------------------------------------------------------
		public ViewWrapper(string name, Control parent, FdoCache cache, IVwStylesheet styleSheet,
			RegistryKey settingsRegKey, object draftView, object stylebar,
			object footnoteDraftView, object footnoteStylebar)
			: this(name, parent, cache, styleSheet, settingsRegKey, draftView, stylebar,
				footnoteDraftView, footnoteStylebar, 2, 2)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the component.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// ViewWrapper
			//
			this.AccessibleName = "ViewWrapper";
			this.ResumeLayout(false);

		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the style column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual int kStyleColumn
		{
			get { return 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the draft view column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual int kDraftViewColumn
		{
			get { return 1; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the draft row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual int kDraftRow
		{
			get { return 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the footnote row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual int kFootnoteRow
		{
			get { return 1; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets which view is the active, or focused, one. Commands such as
		/// Find/Replace will pertain to the active view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwRootSite FocusedView
		{
			get
			{
				CheckDisposed();
				return (FwRootSite)FocusedRootSite;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the main child view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual DraftView DraftView
		{
			get
			{
				CheckDisposed();
				return GetControl(kDraftRow, kDraftViewColumn) as DraftView;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote window.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual FootnoteView FootnoteView
		{
			get
			{
				CheckDisposed();
				return GetControl(kFootnoteRow, kDraftViewColumn) as FootnoteView;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the footnote view is currently visible.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public bool FootnoteViewShowing
		{
			get
			{
				CheckDisposed();
				Control fnView = GetControl(kFootnoteRow, kDraftViewColumn);
				if (fnView != null)
					return fnView.Visible;
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether the footnote pane or the other (Scripture) pane has
		/// focus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool FootnoteViewFocused
		{
			set
			{
				GetControl(value ? kFootnoteRow : kDraftRow, kDraftViewColumn).Focus();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the style pane is showing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool StylePaneShowing
		{
			get
			{
				CheckDisposed();
				return m_stylePaneWidth.Value != 0;
			}
		}
		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toggles the visibility of the Style Pane view.
		/// </summary>
		/// <param name="arg">The menu or toolbar item</param>
		/// <returns><c>true</c> because we handled the message.</returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnViewStylePane(object arg)
		{
			Debug.Assert(kStyleColumn >= 0);
			//toggle
			bool fShow = !m_grid.Columns[kStyleColumn].Visible;

			// show or hide the style pane in each of the views
			m_grid.Columns[kStyleColumn].Visible = fShow;

			// save a value to the registry
			m_stylePaneWidth.Value = fShow ? m_grid.Columns[kStyleColumn].Width : 0;

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the width of the style pane changed we save the style pane width into the
		/// registry.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DataGridViewColumnEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
		{
			CheckDisposed();

			if (e.Column.Index != kStyleColumn)
				return;

			if (e.Column.Width == DataGridViewControlColumn.kMinimumValue)
				m_stylePaneWidth.Value = 0;
			else
				m_stylePaneWidth.Value = e.Column.Width;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the row height changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DataGridViewRowEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnRowHeightChanged(object sender, DataGridViewRowEventArgs e)
		{
			if (!e.Row.Visible && m_grid.DisplayedRowCount(false) == 1)
			{
				// If the user drags the footnote pane all the way up to the top, we want
				// to display the draftview instead of the footnote pane
				if (FootnoteViewShowing)
				{
					m_grid.Rows[kFootnoteRow].Visible = false;
					m_grid.Rows[kDraftRow].Visible = true;
				}

				IRootSite rootSite = FocusedRootSite;
				if (rootSite != null)
				{
					((Control)rootSite).Focus();
					if (rootSite.EditingHelper.CurrentSelection == null)
						rootSite.CastAsIVwRootSite().RootBox.MakeSimpleSel(true, true, false, true);
				}
			}
		}

		#endregion

		#region Public and Internal Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show (or hide) the footnote view.
		/// </summary>
		/// <param name="footnote">Footnote to scroll to or <c>null</c> to just display or hide
		/// the footnote view. If a footnote is given the footnote view will always be shown.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public void ShowFootnoteView(StFootnote footnote)
		{
			CheckDisposed();

			ShowOrHideFootnoteView(footnote, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show or hide the footnote view.
		/// </summary>
		/// <param name="footnote">Footnote to scroll to or <c>null</c> to just display or hide
		/// the footnote view. If a footnote is given the footnote view will always be shown.</param>
		/// <param name="fPutInsertionPtAtEnd">if set to <c>true</c> and showing the view,
		/// the insertion point will be put at the end of the footnote text instead of at the
		/// beginning, as would be appropriate in the case of a newly inserted footnote that has
		/// Reference Text. This parameter is ignored if footnote is null.</param>
		/// ------------------------------------------------------------------------------------
		internal virtual void ShowOrHideFootnoteView(StFootnote footnote, bool fPutInsertionPtAtEnd)
		{
			CheckDisposed();

			//toggle
			bool fShow = !m_grid.Rows[kFootnoteRow].Visible || footnote != null;

			// Prevent painting while we possibly expand lazy boxes
			((IRootSite)this).AllowPainting = false;
			try
			{
				// show or hide the footnote view
				m_grid.Rows[kFootnoteRow].Visible = fShow;
			}
			finally
			{
				((IRootSite)this).AllowPainting = true;
			}

			if (fShow && FootnoteView != null)
			{
				if (footnote != null)
					FootnoteView.ScrollToFootnote(footnote, fPutInsertionPtAtEnd);

				FootnoteView.Focus();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hides the footnote view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void HideFootnoteView()
		{
			CheckDisposed();

			if (!FootnoteViewShowing)
				return;

			bool footnoteViewHadFocus = (FocusedRootSite == FootnoteView);

			// Will hide it if it is currently showing
			ShowFootnoteView(null);

			if (footnoteViewHadFocus)
			{
				if (FocusedRootSite != null)
					((Control)FocusedRootSite).Focus();
				else
					GetControl(kDraftRow, kDraftViewColumn).Focus();
			}
		}
		#endregion

		#region ISettings Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the window should keep its current Size and Location
		/// properities when it is displayed.
		/// Returns false if the window should override these with values persisted in the
		/// registry. By default, most implementations should return false.
		/// This is irrelevant for non-Forms; their implementation should return false also.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		bool ISettings.KeepWindowSizePos
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implement this method to save the persistent settings for your class. Normally, this
		/// should also result in recursively calling SaveSettingsNow for all children which
		/// also implement ISettings.
		/// </summary>
		/// <remarks>Note: A form's implementation of <see cref="M:SIL.FieldWorks.Common.Controls.ISettings.SaveSettingsNow"/> normally
		/// calls <see cref="M:SIL.FieldWorks.Common.Controls.Persistence.SaveSettingsNow(System.Windows.Forms.Control)"/> (after optionally saving any
		/// class-specific properties).</remarks>
		/// ------------------------------------------------------------------------------------
		void ISettings.SaveSettingsNow()
		{
			for (int iRow = 0; iRow < m_grid.RowCount; iRow++)
			{
				for (int iColumn = 0; iColumn < m_grid.ColumnCount; iColumn++)
				{
					Control ctrl = GetControl(iRow, iColumn);
					if (ctrl is ISettings)
						((ISettings)ctrl).SaveSettingsNow();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a key in the registry where <see cref="T:SIL.FieldWorks.Common.Controls.Persistence"/> should store settings.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		RegistryKey ISettings.SettingsKey
		{
			get { return null; }
		}

		#endregion

		#region IBtAwareView Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets the back translation writing system.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public int BackTranslationWS
		{
			get
			{
				foreach (Control ctrl in FocusableControls)
				{
					IBtAwareView view = ctrl as IBtAwareView;
					if (view != null && view.BackTranslationWS > 0)
						return view.BackTranslationWS;
				}
				return -1;
			}
			set
			{
				foreach (Control ctrl in FocusableControls)
				{
					IBtAwareView view = ctrl as IBtAwareView;
					if (view != null)
						view.BackTranslationWS = value;
				}
			}
		}
		#endregion
	}
}
