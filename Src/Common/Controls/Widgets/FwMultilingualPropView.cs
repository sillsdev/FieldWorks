// ---------------------------------------------------------------------------------------------
#region
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwMultilingualPropView.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Resources;
using SIL.Utils;


namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// Interface that provides the data for FwMultilingualPropView.
	/// </summary>
	public interface IFwMultilingualPropViewDataSource
	{
		/// <summary>Associated FwMultilingualPropView grid</summary>
		FwMultilingualPropView Grid { set; }

		/// <summary>Saves the multi lingual string for the active writing systems and the specified field IDs.</summary>
		void SaveMultiLingualStrings();

		/// <summary>
		/// Checks this class has been setup correctly. Throws InvalidOperationException if it isn't.
		/// </summary>
		void CheckSettings();

		/// <summary>
		/// a description of fields to display for each writing system
		/// </summary>
		List<FwMultilingualPropView.ColumnInfo> FieldsToDisplay { get; }

		/// <summary>
		/// a list of writing systems for which to display information
		/// </summary>
		List<int> WritingSystemsToDisplay { get; }

		/// <summary> </summary>
		IWritingSystemManager WritingSystemManager { get; }

		/// <summary></summary>
		ITsString GetMultiStringAlt(int tag, int ws);

		/// <summary>
		/// Adds a column with the given name.
		/// </summary>
		void AddColumn(string name, int widthPct);

		/// <summary> </summary>
		FdoCache Cache { get; set; }

		/// <summary> </summary>
		int RootObject { get; set; }
	}

	/// <summary>
	/// Standard Implementation of IFwMultilingualPropViewDataSource
	/// </summary>
	internal class FwMultilingualPropViewDataSource : IFwMultilingualPropViewDataSource
	{
		#region Data members
		/// <summary>the HVO of the root object which will have multilingual info displayed</summary>
		private int m_hvoRootObject = 0;
		/// <summary>a description of fields to display for each writing system</summary>
		private List<FwMultilingualPropView.ColumnInfo> m_fieldsToDisplay = new List<FwMultilingualPropView.ColumnInfo>();
		/// <summary>a list of writing systems for which to display information</summary>
		private List<int> m_writingSystemsToDisplay = new List<int>();
		/// <summary>database cache</summary>
		private FdoCache m_cache;
		/// <summary></summary>
		private IVwStylesheet m_stylesheet;
		#endregion

		#region Public properties

		public FwMultilingualPropView Grid { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public FdoCache Cache {
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the stylesheet.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IVwStylesheet StyleSheet {
			get { return m_stylesheet; }
			set { m_stylesheet = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the HVO of an object with one or more MultiString or MultiUnicode
		/// properties to display in the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int RootObject {
			get { return m_hvoRootObject; }
			set { m_hvoRootObject = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the list of fields to display, the strings to use as the column headers, and
		/// the column width.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public List<FwMultilingualPropView.ColumnInfo> FieldsToDisplay {
			get { return m_fieldsToDisplay; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the list of HVOs of the writing systems in which to display the fields.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[BrowsableAttribute(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public List<int> WritingSystemsToDisplay {
			get { return m_writingSystemsToDisplay; }
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the multi lingual string for the active writing systems and the specified
		/// field IDs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SaveMultiLingualStrings()
		{
			int iRow = 0;
			foreach (int ws in m_writingSystemsToDisplay) {
				int iColumn = 1;
				foreach (FwMultilingualPropView.ColumnInfo columnInfo in m_fieldsToDisplay) {
					m_cache.DomainDataByFlid.SetMultiStringAlt(m_hvoRootObject, columnInfo.flid, ws, (ITsString)Grid.Rows[iRow].Cells[iColumn].Value);
					iColumn++;
				}
				iRow++;
			}
		}

		/// <summary>
		/// Checks this class has been setup correctly. Throws InvalidOperationException if it isn't.
		/// </summary>
		public void CheckSettings()
		{
			if (m_cache == null)
				throw new InvalidOperationException("Cache must be set before showing the FwMultilingualPropView named " + Grid.Name);
			if (m_hvoRootObject == 0)
				throw new InvalidOperationException("Root object must be set before showing the FwMultilingualPropView named " + Grid.Name);
			if (m_fieldsToDisplay.Count == 0)
				throw new InvalidOperationException("At least one field must be added to the FieldsToDisplay property before showing the FwMultilingualPropView named " + Grid.Name);
			if (m_writingSystemsToDisplay.Count == 0)
				throw new InvalidOperationException("At least one writing system must be added to the WritingSystemsToDisplay property before showing the FwMultilingualPropView named " + Grid.Name);
		}

		public IWritingSystemManager WritingSystemManager {
			get { return m_cache.ServiceLocator.WritingSystemManager; }
		}

		public ITsString GetMultiStringAlt(int flid, int ws)
		{
			return m_cache.DomainDataByFlid.get_MultiStringAlt(m_hvoRootObject, flid, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a column with the given name.
		/// </summary>
		/// <param name="name">Name of the column.</param>
		/// <param name="widthPct">The width PCT.</param>
		/// ------------------------------------------------------------------------------------
		public void AddColumn(string name, int widthPct)
		{
			FwTextBoxColumn col = new FwTextBoxColumn(m_cache, true);
			col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
			col.HeaderText = name;
			col.FillWeight = (float)widthPct;
			col.Resizable = DataGridViewTriState.True;
			col.StyleSheet = m_stylesheet;
			col.UseTextPropsFontForCell = true;
			Grid.Columns.Add(col);
		}

		#endregion
	}


	#region FwMultilingualPropView
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FwMultilingualPropView is sort of a simulation of a Windows.Forms.ListView. It is used
	/// for displaying one or more MultiString or MultiUnicode properties of a single object
	/// in multiple writing systems.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwMultilingualPropView : DataGridView, IFWDisposable
	{
		#region Struct ColumnInfo
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public struct ColumnInfo
		{
			/// <summary>the field id of the item in the database</summary>
			public int flid;
			/// <summary>the name of the item to appear as the column header</summary>
			public string name;
			/// <summary>the percent of the available width that will be used for the columns</summary>
			public int widthPct;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:ColumnInfo"/> class.
			/// </summary>
			/// <param name="flid">The field id</param>
			/// <param name="name">The name of the field to appear in the column.</param>
			/// <param name="widthPct">The percentage of available width the column will take.</param>
			/// --------------------------------------------------------------------------------
			public ColumnInfo(int flid, string name, int widthPct)
			{
				this.flid = flid;
				this.name = name;
				this.widthPct = widthPct;
			}
		}
		#endregion


		#region Data members
		IFwMultilingualPropViewDataSource PropertyDataSource { get; set; }
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwMultilingualPropView"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwMultilingualPropView() : this(null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwMultilingualPropView"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwMultilingualPropView(IFwMultilingualPropViewDataSource datasource) : base()
		{
			AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
			RowHeadersVisible = false;
			AllowUserToAddRows = false;
			EditMode = DataGridViewEditMode.EditOnEnter;
			CausesValidation = false;

			if (datasource == null)
				datasource = new FwMultilingualPropViewDataSource();

			PropertyDataSource = datasource;
			PropertyDataSource.Grid = this;

			// Can't check for DesignMode here because Site property isn't set yet, so
			// DesignMode always returns false.
		}
		#endregion

		#region IFWDisposable implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Don't call Controls.Clear(). This will be done in the base class.
			}

			base.Dispose(disposing);
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the multi lingual string for the active writing systems and the specified
		/// field IDs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SaveMultiLingualStrings()
		{
			PropertyDataSource.SaveMultiLingualStrings();
		}
		#endregion

		#region Public properties
		/// <summary>
		/// a description of fields to display for each writing system
		/// </summary>
		public List<ColumnInfo> FieldsToDisplay {
			get { return PropertyDataSource.FieldsToDisplay; }
		}

		/// <summary>
		/// a list of writing systems for which to display information
		/// </summary>
		public List<int> WritingSystemsToDisplay {
			get { return PropertyDataSource.WritingSystemsToDisplay; }
		}

		/// <summary> </summary>
		public FdoCache Cache {
			get { return PropertyDataSource.Cache; }
			set {PropertyDataSource.Cache = value; }
		}

		/// <summary>
		/// the HVO of the root object which will have multilingual info displayed
		/// </summary>
		public int RootObject {
			get { return PropertyDataSource.RootObject; }
			set {PropertyDataSource.RootObject = value; }
		}
		#endregion

		#region Overrides of DataGridView control
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.HandleCreated"></see> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"></see> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			if (!DesignMode)
			{
				// Add Writing System column
				DataGridViewColumn wsCol = new DataGridViewColumn();
				// Setting AutoSizeMode to AllCells makes the column not resizable.
				wsCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
				// we have to append a space, otherwise a bug in .NET hides the second word of the
				// string "Writing System".
				wsCol.HeaderText = ResourceHelper.GetResourceString("kstidWritingSystemItemName") + " ";
				wsCol.DefaultCellStyle.ForeColor = SystemColors.ControlText;
				wsCol.DefaultCellStyle.BackColor = SystemColors.Control;
				wsCol.ReadOnly = true;
				wsCol.CellTemplate = new DataGridViewTextBoxCell();
				Columns.Insert(0, wsCol);
			}
			base.OnHandleCreated(e);
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

			if (!Visible || Disposing)
				return;

			if (DesignMode)
				return;

			PropertyDataSource.CheckSettings();

			// Add column headers
			foreach (ColumnInfo colInfo in PropertyDataSource.FieldsToDisplay)
				PropertyDataSource.AddColumn(colInfo.name, colInfo.widthPct);

			int iRow = 0;
			foreach (int ws in PropertyDataSource.WritingSystemsToDisplay) {
				Rows.Add(new FwTextBoxRow(ws));
				var lgws = PropertyDataSource.WritingSystemManager.Get(ws);
				Rows[iRow].Cells[0].Value = lgws.Abbreviation;

				int iCol = 1;
				foreach (ColumnInfo colInfo in PropertyDataSource.FieldsToDisplay)
					AddStringToCell(ws, colInfo.flid, iRow, iCol++);
				iRow++;
			}

			AutoResizeRows(DataGridViewAutoSizeRowsMode.AllCells);
			AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

			// Now set the current cell to the second column so that we skip the (read-only)
			// language name.
			CurrentCell = CurrentRow.Cells[1];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.DataGridView.DataError"></see> event.
		/// </summary>
		/// <param name="displayErrorDialogIfNoHandler">true to display an error dialog box if
		/// there is no handler for the <see cref="E:System.Windows.Forms.DataGridView.DataError"/>
		/// event.</param>
		/// <param name="e">A <see cref="T:System.Windows.Forms.DataGridViewDataErrorEventArgs"/>
		/// that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnDataError(bool displayErrorDialogIfNoHandler, DataGridViewDataErrorEventArgs e)
		{
			base.OnDataError(false, e);
			e.ThrowException = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes keys used for navigating in the
		/// <see cref="T:System.Windows.Forms.DataGridView"></see>.
		/// </summary>
		/// <param name="e">Contains information about the key that was pressed.</param>
		/// <returns>
		/// true if the key was processed; otherwise, false.
		/// </returns>
		/// <exception cref="T:System.InvalidCastException">The key pressed would cause the
		/// control to enter edit mode, but the
		/// <see cref="P:System.Windows.Forms.DataGridViewCell.EditType"></see> property of the
		/// current cell does not indicate a class that derives from
		/// <see cref="T:System.Windows.Forms.Control"></see> and implements
		/// <see cref="T:System.Windows.Forms.IDataGridViewEditingControl"></see>.</exception>
		/// <exception cref="T:System.Exception">This action would commit a cell value or enter
		/// edit mode, but an error in the data source prevents the action and either there is
		/// no handler for the <see cref="E:System.Windows.Forms.DataGridView.DataError"></see>
		/// event or the handler has set the
		/// <see cref="P:System.Windows.Forms.DataGridViewDataErrorEventArgs.ThrowException"/>
		/// property to true.-or-The DELETE key would delete one or more rows, but an error in
		/// the data source prevents the deletion and either there is no handler for the
		/// <see cref="E:System.Windows.Forms.DataGridView.DataError"></see> event or the
		/// handler has set the
		/// <see cref="P:System.Windows.Forms.DataGridViewDataErrorEventArgs.ThrowException"/>
		/// property to true. </exception>
		/// ------------------------------------------------------------------------------------
		protected override bool ProcessDataGridViewKey(KeyEventArgs e)
		{
			// We don't want to process the Enter key. It should go to the parent dialog (TE-5572).
			if (e.KeyCode == Keys.Enter)
				return false;

			bool fRet = base.ProcessDataGridViewKey(e);

			if (CurrentCellAddress.X == 0) {
				if (e.KeyCode == Keys.Tab && e.Shift) {
					if (CurrentCellAddress.Y > 0) {
						CurrentCell = Rows[CurrentCellAddress.Y - 1].Cells[Columns.Count - 1];
						return fRet;
					}

					// We are on the first cell in the first row - since we don't want a
					// selection there we set the selection to the second column again
					// and let the parent handle the Shift-TAB key.
					fRet = false;
				}

				CurrentCell = CurrentRow.Cells[1];
			}

			return fRet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the got focus event.
		/// </summary>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);

			if (EditingControl is FwTextBoxControl)
				((FwTextBoxControl)EditingControl).Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.DataGridView.CellLeave"></see> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.DataGridViewCellEventArgs"></see> that contains the event data.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException">The value of the <see cref="P:System.Windows.Forms.DataGridViewCellEventArgs.ColumnIndex"></see> property of e is greater than the number of columns in the control minus one.-or-The value of the <see cref="P:System.Windows.Forms.DataGridViewCellEventArgs.RowIndex"></see> property of e is greater than the number of rows in the control minus one.</exception>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellLeave(DataGridViewCellEventArgs e)
		{
			base.OnCellLeave(e);

			// In case the cell is still in edit mode, we want to end that now.
			if (IsCurrentCellInEditMode)
				EndEdit();
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the TsString to the given cell.
		/// </summary>
		/// <param name="ws">The HVO of the writing system.</param>
		/// <param name="flid">The flid of the property to display.</param>
		/// <param name="iRow">The index of the row in which to place the control.</param>
		/// <param name="iCol">The index of the column in which to place the control.</param>
		/// ------------------------------------------------------------------------------------
		private void AddStringToCell(int ws, int flid, int iRow, int iCol)
		{
			FwTextBoxCell cell = Rows[iRow].Cells[iCol] as FwTextBoxCell;
			if (cell != null)
				cell.Value = PropertyDataSource.GetMultiStringAlt(flid, ws);
		}
		#endregion
	}
	#endregion
}
