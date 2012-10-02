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
// File: DiffViewWrapper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls.SplitGridView;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The DiffViewWrapper class holds four rows (label/draft/footnote/buttons) and two columns
	/// (revision/current).
	/// </summary>
	/// <remarks>Class needs to be public for testing purposes.</remarks>
	/// ----------------------------------------------------------------------------------------
	internal class DiffViewWrapper: SplitGrid
	{
		private const int kLabelRow = 0;
		private const int kDraftRow = 1;
		private const int kFootnoteRow = 2;
		private const int kButtonRow = 3;
		private const int kRevisionColumn = 0;
		private const int kCurrentColumn = 1;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DiffViewWrapper"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="parent">The parent object.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="currentDraft">The current draft.</param>
		/// <param name="revisionDraft">The revision draft.</param>
		/// <param name="currentFootnote">The current footnote.</param>
		/// <param name="revisionFootnote">The revision footnote.</param>
		/// <param name="otherControls">A TableLayoutPanel that contains the controls to show
		/// as labels and buttons above and below the current and revision views.</param>
		/// ------------------------------------------------------------------------------------
		public DiffViewWrapper(string name, Control parent, FdoCache cache,
			IVwStylesheet styleSheet, object currentDraft, object revisionDraft,
			object currentFootnote, object revisionFootnote, TableLayoutPanel otherControls)
			: base(cache, styleSheet, 4, 2)
		{
			Dock = DockStyle.Fill;
			Name = name;
			Parent = parent;
			Padding = otherControls.Padding;

			// Add the labels at the top
			AddControl(null, kLabelRow, kRevisionColumn, new FixedControlCreateInfo(
				otherControls.GetControlFromPosition(kRevisionColumn, kLabelRow)));
			AddControl(null, kLabelRow, kCurrentColumn, new FixedControlCreateInfo(
				otherControls.GetControlFromPosition(kCurrentColumn, kLabelRow)));

			// Add the views
			AddControl(null, kDraftRow, kRevisionColumn, revisionDraft, false, true);
			AddControl(null, kDraftRow, kCurrentColumn, currentDraft, true, true);
			AddControl(null, kFootnoteRow, kRevisionColumn, revisionFootnote, false, true);
			AddControl(null, kFootnoteRow, kCurrentColumn, currentFootnote, false, true);

			// Add the buttons below the views
			AddControl(null, kButtonRow, kRevisionColumn, new FixedControlCreateInfo(
				otherControls.GetControlFromPosition(kRevisionColumn, kButtonRow)));
			AddControl(null, kButtonRow, kCurrentColumn, new FixedControlCreateInfo(
				otherControls.GetControlFromPosition(kCurrentColumn, kButtonRow)));

			// Set properties on the rows

			// We have to set the desired height first because this resets the visible
			// state - strange.
			GetRow(kLabelRow).MinimumHeight =
				otherControls.GetControlFromPosition(kRevisionColumn, kLabelRow).Height;
			GetRow(kButtonRow).MinimumHeight =
				otherControls.GetControlFromPosition(kRevisionColumn, kButtonRow).Height;

			DataGridViewControlRow row = GetRow(kLabelRow);
			row.Visible = true;
			row.Resizable = DataGridViewTriState.False;
			row.IsAutoFill = false;
			DataGridViewAdvancedBorderStyle noBorder = new DataGridViewAdvancedBorderStyle();
			noBorder.All = DataGridViewAdvancedCellBorderStyle.None;
			row.AdvancedBorderStyle = noBorder;

			row = GetRow(kDraftRow);
			row.Visible = true;

			row = GetRow(kFootnoteRow);
			row.FillWeight = 18;	// 30%
			row.Visible = false;

			row = GetRow(kButtonRow);
			row.Visible = true;
			row.Resizable = DataGridViewTriState.False;
			row.IsAutoFill = false;
			row.AdvancedBorderStyle = noBorder;

			// Set properties on the two columns
			DataGridViewControlColumn column = GetColumn(kRevisionColumn);
			column.MinimumWidth = 220;
			column.Visible = true;
			column.FillWeight = 100;
			column.Name = "RevisionColumn";

			column = GetColumn(kCurrentColumn);
			column.MinimumWidth = 315;
			column.Visible = true;
			column.FillWeight = 100;
			column.Name = "CurrentColumn";
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the zoom value.
		/// </summary>
		/// <value>The zoom.</value>
		/// ------------------------------------------------------------------------------------
		public float Zoom
		{
			get
			{
				CheckDisposed();
				// Return the active view's zoom value.
				if (m_activeViewHelper != null && m_activeViewHelper.ActiveView is IRootSiteSlave)
					return ((IRootSiteSlave)m_activeViewHelper.ActiveView).Zoom;

				if (CurrentDiffView != null)
					return CurrentDiffView.Zoom;

				return 1.0f;
			}
			set
			{
				CheckDisposed();

				if (m_activeViewHelper != null && m_activeViewHelper.ActiveView is IRootSiteSlave)
				{
					IRootSiteSlave activeView = (IRootSiteSlave)m_activeViewHelper.ActiveView;
					if (activeView == RevisionDiffFootnoteView ||
						activeView == CurrentDiffFootnoteView)
					{
						if (RevisionDiffFootnoteView != null)
							RevisionDiffFootnoteView.Zoom = value;
						if (CurrentDiffFootnoteView != null)
							CurrentDiffFootnoteView.Zoom = value;
						return;
					}
				}
				if (RevisionDiffView != null)
					RevisionDiffView.Zoom = value;
				if (CurrentDiffView != null)
					CurrentDiffView.Zoom = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current diff view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DiffView CurrentDiffView
		{
			get { return GetControl(kDraftRow, kCurrentColumn) as DiffView; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the revision diff view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DiffView RevisionDiffView
		{
			get { return GetControl(kDraftRow, kRevisionColumn) as DiffView; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current diff footnote view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DiffFootnoteView CurrentDiffFootnoteView
		{
			get { return GetControl(kFootnoteRow, kCurrentColumn) as DiffFootnoteView; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the revision diff footnote view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DiffFootnoteView RevisionDiffFootnoteView
		{
			get { return GetControl(kFootnoteRow, kRevisionColumn) as DiffFootnoteView; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the footnote row is visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsFootnoteRowVisible
		{
			get { return GetRow(kFootnoteRow).Visible; }
			set { GetRow(kFootnoteRow).Visible = value; }
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Re-centers the columns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ReCenter()
		{
			using (new WaitCursor(this))
			{
				int columnWidth = Width / 2;
				GetColumn(kRevisionColumn).Width = columnWidth;
				GetColumn(kCurrentColumn).Width = columnWidth;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do refresh on all views that have been created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void RefreshAllViews()
		{
			CurrentDiffView.RefreshDisplay();
			if (CurrentDiffFootnoteView != null)
				CurrentDiffFootnoteView.RefreshDisplay();
			RevisionDiffView.RefreshDisplay();
			if (RevisionDiffFootnoteView != null)
				RevisionDiffFootnoteView.RefreshDisplay();
		}
	}
}
