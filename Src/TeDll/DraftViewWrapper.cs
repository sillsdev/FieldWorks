// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DraftViewWrapper.cs
// Responsibility: TeTeam
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Win32;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.SplitGridView;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.TE;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// -----------------------------------------------------------------------------------
	/// <summary>
	/// This DraftViewWrapper class holds three rows.
	/// At first, one top/bottom Draft view is displayed: the usual Scripture
	/// Draft view.
	/// A CollapsibleSplitter thingy shown on the vertical scroll bar enables the user to
	/// split the user view and show both top and bottom Draft views.
	/// In addition, a secondary bottom (footnote) view can be displayed instead of one of
	/// the draft views.
	/// </summary>
	/// -----------------------------------------------------------------------------------
	public class DraftViewWrapper : ViewWrapper
	{
		#region Member variables
		private CollapsibleSplitter m_colSplit;
		/// <summary>set to <c>true</c> when we were showing both draft views at the time
		/// when we started to display the footnote pane.</summary>
		private bool m_fBothDraftViewsShowing;
		#endregion

		#region Constructor
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
		/// <param name="topDraftView">The top Scripture draft view proxy.</param>
		/// <param name="topStylebar">The top Scripture stylebar proxy.</param>
		/// <param name="bottomDraftView">The bottom Scripture draft view proxy.</param>
		/// <param name="bottomStylebar">The bottom Scripture stylebar proxy.</param>
		/// <param name="footnoteDraftView">The footnote draft view proxy.</param>
		/// <param name="footnoteStylebar">The footnote stylebar proxy.</param>
		/// ------------------------------------------------------------------------------------
		internal DraftViewWrapper(string name, Control parent, FdoCache cache,
			IVwStylesheet styleSheet, RegistryKey settingsRegKey, TeScrDraftViewProxy topDraftView,
			DraftStylebarProxy topStylebar, TeScrDraftViewProxy bottomDraftView,
			DraftStylebarProxy bottomStylebar, TeFootnoteDraftViewProxy footnoteDraftView,
			DraftStylebarProxy footnoteStylebar)
			: base(name, parent, cache, styleSheet, settingsRegKey, bottomDraftView, bottomStylebar,
			footnoteDraftView, footnoteStylebar, 3, 2)
		{
			IRootSiteGroup group = CreateGroup((int)TeViewGroup.Scripture);
			AddControl(group, kUpperDraftRow, kStyleColumn, topStylebar);
			AddControl(group, kUpperDraftRow, kDraftViewColumn, topDraftView, false, true);
			GetRow(kUpperDraftRow).Visible = false;

			m_colSplit = new CollapsibleSplitter(this);
			m_colSplit.SplitterMoved += OnCollapsibleSplitterMoved;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// called after the collapsible splitter is moved
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="splitterTop">The new splitter top.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnCollapsibleSplitterMoved(object sender, int splitterTop)
		{
			if (splitterTop > 0)
			{
				// splitter was uncollapsed
				// depending on which draft view is showing we might have to swap the two
				// draft views. The draft view that was visible so far should become the bottom
				// draft view.
				if (m_grid.Rows[kUpperDraftRow].Visible)
				{
					// need to swap
					DataGridViewRow row = m_grid.Rows[kLowerDraftRow];
					m_grid.Rows.RemoveAt(kLowerDraftRow);
					m_grid.Rows.Insert(kUpperDraftRow, row);
				}
				m_grid.Rows[kFootnoteRow].Visible = false;
				m_grid.Rows[kLowerDraftRow].Visible = true;
				m_grid.Rows[kUpperDraftRow].Visible = true;
				m_grid.Rows[kUpperDraftRow].Height = splitterTop;
				m_colSplit.Visible = false;
				OnSizeChanged(EventArgs.Empty);
			}
		}
		#endregion

		#region Properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the upper draft row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected int kUpperDraftRow
		{
			get { return 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the lower draft row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected int kLowerDraftRow
		{
			get { return kDraftRow; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the draft row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override int kDraftRow
		{
			get { return 1; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the footnote row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override int kFootnoteRow
		{
			get { return 2; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the main child view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override DraftView DraftView
		{
			get
			{
				CheckDisposed();

				DraftView view = FocusedRootSite as DraftView;
				if (view != null)
					return view;
				Control upperControl = GetControl(kUpperDraftRow, kDraftViewColumn);
				if (upperControl != null && upperControl.Visible)
					return GetControl(kUpperDraftRow, kDraftViewColumn) as DraftView;
				return base.DraftView;
			}
		}

		#endregion

		#region Event Handlers

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
			if (!e.Row.Visible && (m_grid.DisplayedRowCount(false) == 1 || FootnoteViewShowing))
			{
				m_colSplit.Visible = true;
				OnSizeChanged(EventArgs.Empty);
			}
			base.OnRowHeightChanged(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the row state changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DataGridViewRowStateChangedEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnRowStateChanged(object sender,
			DataGridViewRowStateChangedEventArgs e)
		{
			base.OnRowStateChanged(sender, e);

			// We might come here while we add the rows, so we better check that we are fully
			// initialized before expecting to have 3 rows.
			if (e.StateChanged == DataGridViewElementStates.Visible && !e.Row.Visible &&
				m_grid.RowCount == 3 &&
				(m_grid.DisplayedRowCount(false) == 1 || FootnoteViewShowing))
			{
				m_colSplit.Visible = true;
				OnSizeChanged(EventArgs.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the padding changed event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaddingChanged(EventArgs e)
		{
			base.OnPaddingChanged(e);
			OnRowHeightChanged(this, new DataGridViewRowEventArgs(m_grid.Rows[0]));
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
		/// <param name="fPutInsertionPtAtEnd">if set to <c>true</c> and showing the view,
		/// the insertion point will be put at the end of the footnote text instead of at the
		/// beginning, as would be appropriate in the case of a newly inserted footnote that has
		/// Reference Text. This parameter is ignored if footnote is null.</param>
		/// ------------------------------------------------------------------------------------
		internal override void ShowOrHideFootnoteView(IStFootnote footnote, bool fPutInsertionPtAtEnd)
		{
			CheckDisposed();

			//toggle
			bool fShow = !m_grid.Rows[kFootnoteRow].Visible || footnote != null;

			base.ShowOrHideFootnoteView(footnote, fPutInsertionPtAtEnd);

			if (fShow)
			{
				// We don't want to show both draft view rows together with the footnotes
				m_fBothDraftViewsShowing = (m_grid.Rows[kUpperDraftRow].Visible &&
					m_grid.Rows[kLowerDraftRow].Visible);

				// Determine which of the draft view to collapse (if both are open)
				if (m_fBothDraftViewsShowing)
				{
					if (FocusedRootSite == GetControl(kUpperDraftRow, kDraftViewColumn))
						m_grid.Rows[kLowerDraftRow].Visible = false;
					else
						m_grid.Rows[kUpperDraftRow].Visible = false;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Hides the footnote view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="GetControl() returns a reference")]
		public override void HideFootnoteView()
		{
			CheckDisposed();

			if (!FootnoteViewShowing)
				return;

			bool footnoteViewHadFocus = (FocusedRootSite == FootnoteView);
			bool fLowerDraftViewWasShowing = m_grid.Rows[kLowerDraftRow].Visible;

			// Will hide it if it is currently showing
			ShowFootnoteView(null);

			// If both draft views were showing previously, uncollapse both of them.
			if (m_fBothDraftViewsShowing)
				m_grid.Rows[fLowerDraftViewWasShowing ? kUpperDraftRow : kLowerDraftRow].Visible = true;

			if (footnoteViewHadFocus)
			{
				if (FocusedRootSite != null)
					((Control)FocusedRootSite).Focus();
				else if (fLowerDraftViewWasShowing)
				{
					((ISelectableView)GetControl(kUpperDraftRow, kDraftViewColumn)).ActivateView();
					GetControl(kLowerDraftRow, kDraftViewColumn).Focus();
				}
				else
					GetControl(kUpperDraftRow, kDraftViewColumn).Focus();
			}
		}
		#endregion

//		private void InitializeComponent()
//		{
//			this.SuspendLayout();
//			//
//			// DraftViewWrapper
//			//
//			this.AccessibleName = "DraftViewWrapper";
//			this.ResumeLayout(false);

//		}
	}
}
