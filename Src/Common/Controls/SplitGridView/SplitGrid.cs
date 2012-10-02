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
// File: SplitGrid.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.Common.Controls.SplitGridView
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The SplitGrid control contains multiple controls layed out in a grid that the user can
	/// resize.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SplitGrid : Panel, IRootSite, ISelectableView, IFWDisposable,
		IControl, IxCoreColleague
	{
		#region Member variables
		/// <summary>
		/// base caption string that this client window should display in the info bar
		/// </summary>
		private string m_baseInfoBarCaption;
		/// <summary></summary>
		protected CollapsibleDataGridView m_grid;
		private List<RootSiteGroup> m_groups = new List<RootSiteGroup>();
		/// <summary></summary>
		protected FdoCache m_cache;
		/// <summary></summary>
		protected IVwStylesheet m_StyleSheet;
		/// <summary></summary>
		protected ActiveViewHelper m_activeViewHelper;
		private int m_MaxRows;
		private IControlCreator m_ControlCreator;
		/// <summary>The coordinates of the cell that will be activated</summary>
		protected Point m_CellToActivate;
		private IRootSite m_lastFocusedRootSite;
		/// <summary><c>true</c> if this SplitGrid was previously displayed, <c>false</c> if
		/// it hasn't been shown yet.</summary>
		private bool m_fWasVisibleBefore;
		private RootSiteGroup m_defaultGroup;
		/// <summary>Number of times that SetAllowLayoutForAllViews(false) got called without
		/// a corresponding SetAllowLayoutForAllViews(true).</summary>
		private int m_nDisallowLayout;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SplitGrid"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SplitGrid(): this(null, null, 2, 2)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SplitGrid"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="columns">The number of columns.</param>
		/// <param name="rows">The number of rows.</param>
		/// ------------------------------------------------------------------------------------
		public SplitGrid(FdoCache cache, IVwStylesheet styleSheet, int rows, int columns)
		{
			m_cache = cache;
			m_StyleSheet = styleSheet;
			m_grid = CreateDataGridView();
			m_defaultGroup = new RootSiteGroup(this);
			m_groups.Add(m_defaultGroup);
			m_activeViewHelper = new ActiveViewHelper(this);
			BorderStyle = BorderStyle.None;

			SuspendLayout();
			m_grid.Dock = DockStyle.Fill;
			m_grid.BackgroundColor = SystemColors.Control;
			m_grid.BorderStyle = BorderStyle.None;
			m_grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			m_grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
			m_grid.RowHeadersVisible = false;
			m_grid.ColumnHeadersVisible = false;
			m_grid.AllowUserToAddRows = false;
			m_grid.AllowUserToDeleteRows = false;
			m_grid.AllowUserToOrderColumns = false;
			m_grid.AdvancedCellBorderStyle.All = DataGridViewAdvancedCellBorderStyle.Outset;
			m_grid.AdvancedCellBorderStyle.Top = DataGridViewAdvancedCellBorderStyle.None;
			m_grid.ScrollBars = ScrollBars.None;
			m_grid.RowStateChanged += new DataGridViewRowStateChangedEventHandler(OnRowStateChanged);
			m_grid.ColumnStateChanged += new DataGridViewColumnStateChangedEventHandler(OnColumnStateChanged);
			m_grid.MouseDown += new MouseEventHandler(OnGridMouseDown);
			m_grid.MouseUp += new MouseEventHandler(OnGridMouseUp);
			m_grid.RowTemplate = new DataGridViewControlRow();

			for (int i = 0; i < columns; i++)
			{
				DataGridViewControlColumn column = new DataGridViewControlColumn(i == columns - 1);
				m_grid.Columns.Add(column);
			}
			m_MaxRows = rows;
			Controls.Add(m_grid);
			ResumeLayout(true);

			m_grid.BringToFront();
			Visible = false;
		}

		#endregion

		#region Disposed stuff
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
		///
		/// </summary>
		/// <param name="disposing"></param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (m_groups != null)
				{
					foreach (RootSiteGroup group in m_groups)
						group.Dispose();
					m_groups.Clear();

					if (Parent != null && Parent is Form)
						((Form)Parent).Shown -= new EventHandler(OnShown);

					// m_grid will be disposed from base class
				}
			}

			m_groups = null;
			m_grid = null;
			m_StyleSheet = null;
			m_cache = null;

			base.Dispose(disposing);
		}
		#endregion

		#region ISelectableView Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activate the view" make it the current one...includes at least showing it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ActivateView()
		{
			// Don't allow the views code to layout (or paint) the views until we know where we
			// are going to be when we are shown.
			SetAllowLayoutForAllViews(false);

			try
			{
				// show views (should cause the controls to be layed out when the wrapper is shown
				// below).
				ShowViews();

				// although the grid is already shown when we come in here the first time we
				// still want to call Show() because we'll come here again when the user
				// switches views for example.
				Show();
				PerformLayout();
			}
			finally
			{
				SetAllowPaintingForAllViews(false);
				try
				{
					// Now that .Net has had a chance to position the controls in their proper
					// place, we set the AllowLayout property to give the views code a chance
					// to do its layout (which will probably take a really, really long time).
					SetAllowLayoutForAllViews(true);
				}
				finally
				{
					SetAllowPaintingForAllViews(true);
				}
			}


			if (ControlToActivate is ISelectableView)
				((ISelectableView)ControlToActivate).ActivateView();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the base caption string for the info bar. Individual views may want
		/// to get this property to build the caption for their info bar. For example, they may
		/// want to add a Scripture reference.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public string BaseInfoBarCaption
		{
			get
			{
				CheckDisposed();
				if (ControlToActivate is ISelectableView)
					return ((ISelectableView)ControlToActivate).BaseInfoBarCaption;
				return m_baseInfoBarCaption;
			}
			set
			{
				CheckDisposed();
				if (ControlToActivate is ISelectableView)
					((ISelectableView)ControlToActivate).BaseInfoBarCaption = value;
				m_baseInfoBarCaption = value == null ? null :
					value.Replace(Environment.NewLine, " ");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deactivate the view...app is closing or some other view is becoming active...
		/// at least, hide it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DeactivateView()
		{
			CheckDisposed();

			Visible = false;
		}

		#endregion

		#region IRootSite Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A list of zero or more internal rootboxes.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		List<IVwRootBox> IRootSite.AllRootBoxes()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 	<c>false</c> to prevent OnPaint from happening, <c>true</c> to perform
		/// OnPaint. This is used to prevent redraws from happening while we do a RefreshDisplay.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IRootSite.AllowPainting
		{
			get
			{
				if (m_groups.Count > 0)
					return m_groups[0].AllowPainting;
				return true;
			}
			set { SetAllowPaintingForAllViews(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows the IRootSite to be cast as an IVwRootSite, except it doesn't really because
		/// it always throws an exception, but it was the thought that counted.
		/// </summary>
		/// <returns>death and destruction</returns>
		/// ------------------------------------------------------------------------------------
		IVwRootSite IRootSite.CastAsIVwRootSite()
		{
			// REVIEW (TimS): This method was refactored to actually try to return something to
			// keep things from crashing if the SplitGrid had focus for some reason.
			return (FocusedRootSite == null ? null : FocusedRootSite.CastAsIVwRootSite());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void IRootSite.CloseRootBox()
		{
			foreach (RootSiteGroup group in m_groups)
				group.CloseRootBox();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Commit any outstanding changes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void IRootSite.Commit()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets editing helper from focused root site, if there is one.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		EditingHelper IRootSite.EditingHelper
		{
			get
			{
				CheckDisposed();
				return (FocusedRootSite == null ? null : FocusedRootSite.EditingHelper);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the display :)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void IRootSite.RefreshDisplay()
		{
			CheckDisposed();

			// suspend painting for each of the panes if we are the top-level split-wrapper.
			SetAllowPaintingForAllViews(false);
			try
			{
				// Refresh all of the views.
				foreach (RootSiteGroup group in m_groups)
					group.RefreshDisplay();
			}
			finally
			{
				// re-allow painting
				SetAllowPaintingForAllViews(true);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll the selection in view and set the IP at the given client position.
		/// </summary>
		/// <param name="sel">The selection</param>
		/// <param name="dyPos">Position from top of client window where IP should be set</param>
		/// ------------------------------------------------------------------------------------
		void IRootSite.ScrollSelectionToLocation(IVwSelection sel, int dyPos)
		{
			throw new Exception("The method or operation is not implemented.");
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
		public int GetAvailWidth(IVwRootBox prootb)
		{
			throw new Exception("The method or operation is not implemented.");
		}
		#endregion

		#region Public methods and properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new group.
		/// </summary>
		/// <param name="viewTypeId">The view type id.</param>
		/// <returns>The new group.</returns>
		/// ------------------------------------------------------------------------------------
		public IRootSiteGroup CreateGroup(int viewTypeId)
		{
			CheckDisposed();
			RootSiteGroup group = new SyncedRootSiteGroup(this, m_cache, viewTypeId);
			m_groups.Add(group);
			return group;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the group for the cell at the given position.
		/// </summary>
		/// <param name="iRow">The index of the row.</param>
		/// <param name="iColumn">The index of the column.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IRootSiteGroup GetGroup(int iRow, int iColumn)
		{
			return ((DataGridViewControlCell)m_grid[iColumn, iRow]).ControlCreateInfo.Group;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the control at the specified location.
		/// </summary>
		/// <param name="group">The group.</param>
		/// <param name="column">The column index.</param>
		/// <param name="row">The row index.</param>
		/// <param name="controlInfo">Client provided information necessary to create the
		/// control. This information is passed to IControlCreator when creating the control.</param>
		/// ------------------------------------------------------------------------------------
		public void AddControl(IRootSiteGroup group, int row, int column, object controlInfo)
		{
			AddControl(group, row, column, controlInfo, false, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the control at the specified location.
		/// </summary>
		/// <param name="group">The group.</param>
		/// <param name="column">The column index.</param>
		/// <param name="row">The row index.</param>
		/// <param name="controlInfo">Client provided information necessary to create the
		/// control. This information is passed to IControlCreator when creating the control.</param>
		/// <param name="fDefaultFocusControl"><c>true</c> if this control will get focus
		/// if this split grid receives focus.</param>
		/// <param name="fScrollingContainer"><c>true</c> if this control should control
		/// scrolling (and display a scroll bar)</param>
		/// ------------------------------------------------------------------------------------
		public void AddControl(IRootSiteGroup group, int row, int column, object controlInfo,
			bool fDefaultFocusControl, bool fScrollingContainer)
		{
			CheckBounds(row, column);

			if (fDefaultFocusControl)
				m_CellToActivate = new Point(column, row);

			if (row >= m_grid.RowCount)
			{
				m_grid.RowCount = row + 1;
				m_grid.Rows[m_grid.RowCount - 1].Visible = false;
				m_grid.Rows[m_grid.RowCount - 1].MinimumHeight =
					DataGridViewControlColumn.kMinimumValue;
			}

			DataGridViewControlCell cell = m_grid.Rows[row].Cells[column] as DataGridViewControlCell;
			Debug.Assert(cell != null);
			cell.ControlCreateInfo = new ControlCreateInfo(group != null ? group : m_defaultGroup,
				controlInfo, fScrollingContainer);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the control at the specified position.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <param name="column">The column.</param>
		/// <returns>The control at the specified position.</returns>
		/// ------------------------------------------------------------------------------------
		public Control GetControl(int row, int column)
		{
			CheckBounds(row, column);
			return ((DataGridViewControlCell)m_grid.Rows[row].Cells[column]).Control;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the column at the specified index.
		/// </summary>
		/// <param name="iColumn">The index of the column.</param>
		/// <returns>The column at position <paramref name="iColumn"/>.</returns>
		/// ------------------------------------------------------------------------------------
		public DataGridViewControlColumn GetColumn(int iColumn)
		{
			return m_grid.Columns[iColumn] as DataGridViewControlColumn;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the row at the specified index.
		/// </summary>
		/// <param name="iRow">The index of the row.</param>
		/// <returns>The row at position <paramref name="iRow"/>.</returns>
		/// ------------------------------------------------------------------------------------
		public DataGridViewControlRow GetRow(int iRow)
		{
			return m_grid.Rows[iRow] as DataGridViewControlRow;
		}
		#endregion

		#region Event handler
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.ParentChanged"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);

			if (Parent is Form)
				((Form)Parent).Shown += new EventHandler(OnShown);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the parent form is intially displayed. We have to adjust the widths
		/// of the columns.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the
		/// event data.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnShown(object sender, EventArgs e)
		{
			if (m_grid.RowCount == 0)
				return; // not much we can do

			// Create the controls in all visible columns
			List<DataGridViewControlCell> cellsToInit = new List<DataGridViewControlCell>();
			foreach (DataGridViewControlRow row in m_grid.Rows)
			{
				if (!row.Visible)
					continue;

				foreach (DataGridViewControlCell cell in row.Cells)
				{
					if (cell.Control == null)
					{
						// create the control
						CreateHostedControl(cell);
						cellsToInit.Add(cell);
					}
				}
			}

			// Now initialize all the controls we just created
			foreach (DataGridViewControlCell cell in cellsToInit)
				InitControl(cell);

			SetAllowLayoutForAllViews(false);
			try
			{
				// Since we're showing now, the grid must also be fully initialized
				m_grid.IsFullyInitialized = true;
				// Adjust the height of the rows
				m_grid.ResizeRows(true);
			}
			finally
			{
				SetAllowLayoutForAllViews(true);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the last visible row unsizable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetLastVisibleRowUnsizable()
		{
			foreach (DataGridViewControlRow row in m_grid.Rows)
			{
				if (!row.Visible)
					continue;

				row.BaseResizable =
					(row.Index == m_grid.LastVisibleResizableRow) ? DataGridViewTriState.False :
					row.ResizableSet ? row.InternalResizable : DataGridViewTriState.NotSet;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.GotFocus"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);

			if (ControlToActivate != null)
				ControlToActivate.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the row state changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DataGridViewRowStateChangedEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnRowStateChanged(object sender, DataGridViewRowStateChangedEventArgs e)
		{
			if (e.StateChanged != DataGridViewElementStates.Visible ||
				m_grid.DisplayedRowCount(true) == 0)
			{
				return;
			}

			OnShown(sender, EventArgs.Empty);
			SetLastVisibleRowUnsizable();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the column state changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DataGridViewColumnStateChangedEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnColumnStateChanged(object sender, DataGridViewColumnStateChangedEventArgs e)
		{
			if (e.StateChanged != DataGridViewElementStates.Visible ||
				m_grid.DisplayedColumnCount(true) == 0)
			{
				return;
			}

			OnShown(sender, EventArgs.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user presses the mouse button on the grid.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.MouseEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnGridMouseDown(object sender, MouseEventArgs e)
		{
			m_lastFocusedRootSite = FocusedRootSite;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user releases the mouse button on the grid.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.MouseEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnGridMouseUp(object sender, MouseEventArgs e)
		{
			if (m_lastFocusedRootSite != null && ((Control)m_lastFocusedRootSite).Visible)
				((Control)m_lastFocusedRootSite).Focus();
			else if (ControlToActivate != null)
				ControlToActivate.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the visible changed event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);

			if (!m_fWasVisibleBefore && Visible)
			{
				// Since we start hidden we have to call OnShown so that the heights of the rows
				// get properly initialized.
				OnShown(this, e);
				SetLastVisibleRowUnsizable();
				m_fWasVisibleBefore = true;
			}
		}
		#endregion

		#region Private and protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the data grid view. Put in a method to allow override for unit tests.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual CollapsibleDataGridView CreateDataGridView()
		{
			return new CollapsibleDataGridView();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the control in column.
		/// </summary>
		/// <param name="cell">The cell that will host the control we create.</param>
		/// ------------------------------------------------------------------------------------
		private void CreateHostedControl(DataGridViewControlCell cell)
		{
			if (cell.ControlCreateInfo == null || (ControlCreator == null &&
				!(cell.ControlCreateInfo.ClientControlInfo is FixedControlCreateInfo) ))
				return;

			IRootSiteGroup group = cell.ControlCreateInfo.Group;
			Control c;
			if (cell.ControlCreateInfo.ClientControlInfo is FixedControlCreateInfo)
			{
				// We know how to deal with this!
				c = ((FixedControlCreateInfo)cell.ControlCreateInfo.ClientControlInfo).Control;
			}
			else
				c = ControlCreator.Create(this, cell.ControlCreateInfo.ClientControlInfo);

			if (c is RootSite)
			{
				RootSite rs = c as RootSite;
				rs.Cache = m_cache;
				rs.StyleSheet = m_StyleSheet;
			}

			if (group != null && c is IRootSiteSlave)
			{
				IRootSiteSlave slave = c as IRootSiteSlave;
				group.AddToSyncGroup(slave);

				if (cell.ControlCreateInfo.IsScrollingController)
					group.ScrollingController = slave;
			}

			if (c is ISelectableView)
				((ISelectableView)c).BaseInfoBarCaption = m_baseInfoBarCaption;

			cell.ControlCreateInfo.Control = c;

			OnHostedControlCreated(c);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a hosted control has been newly created. The default implementation
		/// does nothing; this exists merely to allow subclasses to override.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnHostedControlCreated(Control c)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the control. This consists of calling MakeRoot and creating the handle
		/// of the control.
		/// </summary>
		/// <param name="cell">The cell.</param>
		/// ------------------------------------------------------------------------------------
		private void InitControl(DataGridViewControlCell cell)
		{
			if (cell.ControlCreateInfo == null)
				return;

			Control c = cell.ControlCreateInfo.Control;
			if (c is SimpleRootSite)
			{
				SimpleRootSite rootSite = c as SimpleRootSite;
				rootSite.AllowLayout = false;
				rootSite.MakeRoot();
			}

			cell.Control = c;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the bounds.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <param name="column">The column.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckBounds(int row, int column)
		{
			CheckDisposed();
			if (row < 0)
				throw new ArgumentException("Rows can't be negative", "row");
			if (column < 0)
				throw new ArgumentException("Columns can't be negative", "column");
			if (column >= m_grid.ColumnCount)
				throw new ArgumentException("SplitGrid doesn't contain enough columns", "column");
			if (row >= m_MaxRows)
				throw new ArgumentException("SplitGrid doesn't contain enough rows", "row");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows/disallows all the views in this wrapper to start to do layouts
		/// </summary>
		/// <param name="allowLayout">True to allow layouts for views, false otherwise</param>
		/// ------------------------------------------------------------------------------------
		private void SetAllowLayoutForAllViews(bool allowLayout)
		{
			if (allowLayout)
			{
				if (m_nDisallowLayout <= 0)
					return;

				m_nDisallowLayout--;
			}

			if (m_nDisallowLayout == 0)
			{
				foreach (RootSiteGroup group in m_groups)
				{
					group.AllowLayout = allowLayout;
				}
			}

			if (!allowLayout)
				m_nDisallowLayout++;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows/disallows all the views in this wrapper to start to paint
		/// </summary>
		/// <param name="allowPainting">True to allow painting for views, false otherwise</param>
		/// ------------------------------------------------------------------------------------
		private void SetAllowPaintingForAllViews(bool allowPainting)
		{
			foreach (RootSiteGroup group in m_groups)
			{
				group.AllowPainting = allowPainting;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the views.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ShowViews()
		{
			foreach (RootSiteGroup group in m_groups)
			{
				group.ShowViews();
			}
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets which slave rootsite is the active, or focused, one. Commands such as
		/// Find/Replace will pertain to the active rootsite.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		protected internal IRootSite FocusedRootSite
		{
			get
			{
				CheckDisposed();

				// Return null in case the SplitGrid is the active view, even if this should
				// never happen. Otherwise we get a stack overflow.
				if (m_activeViewHelper.ActiveView == this)
					return null;

				if (m_activeViewHelper.ActiveView is IRootSiteGroup)
					return ((IRootSiteGroup)m_activeViewHelper.ActiveView).FocusedRootSite;

				IRootSite rootSite = m_activeViewHelper.ActiveView;

				if (rootSite == null)
				{
					// ActiveView probably not visible
					if (ControlToActivate != null && ControlToActivate.Visible)
						return ControlToActivate as IRootSite;
					if (m_grid.Columns[m_CellToActivate.X].Visible)
					{
						// If the column that contains the control to activate is visible
						// we return the cell in the first visible row.
						foreach (DataGridViewControlRow row in m_grid.Rows)
						{
							if (row.Visible)
								return GetControl(row.Index, m_CellToActivate.X) as IRootSite;
						}
					}
					else
					{
						// Return the first visible cell regardless of column
						foreach (DataGridViewControlRow row in m_grid.Rows)
						{
							if (!row.Visible)
								continue;
							foreach (DataGridViewControlCell cell in row.Cells)
							{
								if (cell.Visible)
									return GetControl(row.Index, cell.OwningColumn.Index) as IRootSite;
							}
						}
					}
				}
				return rootSite;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the control to activate.
		/// </summary>
		/// <value>The control to activate.</value>
		/// ------------------------------------------------------------------------------------
		protected Control ControlToActivate
		{
			get
			{
				DataGridViewControlCell cell =
					(DataGridViewControlCell)m_grid.Rows[m_CellToActivate.Y].Cells[m_CellToActivate.X];
				return cell.Control;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the control creator.
		/// </summary>
		/// <value>The control creator.</value>
		/// ------------------------------------------------------------------------------------
		protected virtual IControlCreator ControlCreator
		{
			get
			{
				if (m_ControlCreator == null)
				{
					if (Site != null)
					{
						m_ControlCreator = Site.GetService(typeof(IControlCreator))
							as IControlCreator;
					}
					else if (Parent is IControlCreator)
						m_ControlCreator = Parent as IControlCreator;

					Debug.WriteLineIf(m_ControlCreator == null,
						"Client didn't provide a ControlCreator - unable to create hosted controls");
				}
				return m_ControlCreator;
			}
		}

		#endregion

		#region IControl Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of focusable controls.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public List<Control> FocusableControls
		{
			get
			{
				CheckDisposed();

				List<Control> controls = new List<Control>();
				foreach (RootSiteGroup group in m_groups)
				{
					foreach (IRootSiteSlave slave in group.Slaves)
					{
						if (slave is Control)
							controls.Add(slave as Control);
					}
				}

				return controls;
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
			CheckDisposed();

			// return list of view windows with focused window being the first one
			List<IxCoreColleague> targets = new List<IxCoreColleague>();
			foreach (RootSiteGroup group in m_groups)
			{
				foreach (IRootSiteSlave slave in group.Slaves)
				{
					if (slave is IxCoreColleague &&
						(((Control)slave).Contains((Control)FocusedRootSite) || slave == FocusedRootSite))
					{
						targets.AddRange(((IxCoreColleague)slave).GetMessageTargets());
						break;
					}
				}
			}

			targets.Add(this);
			return targets.ToArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the specified mediator.
		/// </summary>
		/// <param name="mediator">The mediator.</param>
		/// <param name="configurationParameters">The configuration parameters.</param>
		/// ------------------------------------------------------------------------------------
		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
		}

		#endregion
	}
}
