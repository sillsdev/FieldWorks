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
// File: KeyTermRenderingsControl.cs
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
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.TE.TeEditorialChecks;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Key Terms data grid
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class KeyTermRenderingsControl : CheckRenderingControl // UserControl, ISelectableView
	{
		#region Constants
		/// <summary>Column indeces for the values in the datagrid</summary>
		internal const int kRefCol = 0;
		internal const int kRenderingCol = 1;
		internal const int kStatusCol = 2;
		internal const int kCommentCol = 3;
		#endregion

		#region Data members
		private bool m_firstLoad = true;
		private int m_wsGreek = -1;
		private int m_wsHebrew = -1;
		private FwStyleSheet m_stylesheet;
		#endregion

		#region delegates & events

		/// <summary>Defines the signature of the event handler</summary>
		internal delegate void ScrRefEventHandler(object sender, ScrRefEventArgs refArgs);

		/// <summary>Event raised when the focused reference changes.</summary>
		internal event ScrRefEventHandler ReferenceChanged;

		/// <summary>Event raised when the focused reference changes.</summary>
		internal event EventHandler ReferenceListEmptied;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:KeyTermRenderingsControl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private KeyTermRenderingsControl() : base(null, null)
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:KeyTermRenderingsControl"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="mainWnd">the FwMainWnd that owns this control.</param>
		/// ------------------------------------------------------------------------------------
		public KeyTermRenderingsControl(FdoCache cache,	FwMainWnd mainWnd) : base(cache, mainWnd)
		{
			InitializeComponent();
			AccessibleName = Name;
			DataGridView = m_dataGridView;

			// Setup columns
			m_Rendering.Cache = m_cache;
			m_Rendering.WritingSystemCode = m_cache.DefaultVernWs;
			m_OriginalTerm.Cache = m_cache;

			IWritingSystem ws;
			m_cache.ServiceLocator.WritingSystemManager.GetOrSet("grc", out ws);
			m_wsGreek = ws.Handle;
			if (m_wsGreek <= 0)
				throw new Exception("The Greek writing system is not defined.");
			m_cache.ServiceLocator.WritingSystemManager.GetOrSet("hbo", out ws);
			m_wsHebrew = ws.Handle;
			if (m_wsHebrew <= 0)
				throw new Exception("The Hebrew writing system is not defined.");

			if (mainWnd != null)
			{
				Parent = mainWnd;
				m_stylesheet = mainWnd.StyleSheet;
				m_Rendering.Font = m_stylesheet.GetUiFontForWritingSystem(cache.DefaultVernWs,
					FontInfo.kDefaultFontSize);
			}

			m_list = new List<ICheckGridRowObject>();
			m_gridSorter = new CheckGridListSorter(m_list);
			m_gridSorter.AddComparer(m_Rendering.DataPropertyName, m_tsStrComparer);
			m_gridSorter.AddComparer(m_OriginalTerm.DataPropertyName, m_tsStrComparer);
			m_gridSorter.AddComparer(m_Status.DataPropertyName, new RenderingStatusComparer());
			m_gridSorter.AddComparer(m_Reference.DataPropertyName,
				new ScriptureReferenceComparer(m_cache.LanguageProject.TranslatedScriptureOA.ScrProjMetaDataProvider));

			m_dataGridView.Cache = m_cache;
			m_dataGridView.ColumnHeaderMouseClick += m_dataGridView_ColumnHeaderMouseClick;
		}

		#endregion

		#region Dispose
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (m_Reference != null)
					m_Reference.Dispose();
				if (m_Rendering != null)
					m_Rendering.Dispose();
				if (m_Status != null)
					m_Status.Dispose();
				if (m_OriginalTerm != null)
					m_OriginalTerm.Dispose();

				if (components != null)
					components.Dispose();
			}
			m_Reference = null;
			m_Rendering = null;
			m_Status = null;
			m_OriginalTerm = null;
			components = null;

			base.Dispose(disposing);
		}

		#endregion

		#region Loading key term renderings list
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build a list of the KeyTermRef objects that will be displayed in the key terms
		/// rendering grid.
		/// </summary>
		/// <param name="chkTerm">The term node to get references for. This could be the
		/// category node, in which case we just clear the list.</param>
		/// <param name="filteredBookIds">list of book ids that are in the current filter.
		/// If null, then filtering is not on and all references should be included</param>
		/// ------------------------------------------------------------------------------------
		public void LoadRenderingsForKeyTerm(IChkTerm chkTerm, List<int> filteredBookIds)
		{
			m_dataGridView.RowEnter -= m_dataGridView_RowEnter;
			m_dataGridView.RowCount = 0;
			m_list.Clear();
			PopulateKeyTermRefs(chkTerm, filteredBookIds);
			if (m_stylesheet != null)
			{
				if (!String.IsNullOrEmpty(chkTerm.Name.get_String(m_wsGreek).Text))
				{
					m_OriginalTerm.WritingSystemCode = m_wsGreek;
					m_OriginalTerm.Font = m_stylesheet.GetUiFontForWritingSystem(m_wsGreek,
						FontInfo.kDefaultFontSize);
				}
				else if (!String.IsNullOrEmpty(chkTerm.Name.get_String(m_wsHebrew).Text))
				{
					m_OriginalTerm.WritingSystemCode = m_wsHebrew;
					m_OriginalTerm.Font = m_stylesheet.GetUiFontForWritingSystem(m_wsHebrew,
						FontInfo.kDefaultFontSize);
				}
				else
					throw new ArgumentException("Unexpected biblical term - no Greek or Hebrew Name. Term id = " + chkTerm);
			}
#if __MonoCS__
			try
			{
				m_dataGridView.RowCount = m_list.Count;
			}
			catch(System.ArgumentOutOfRangeException e)
			{
				// TODO-Linux FWNX-189: remove try catch when https://bugzilla.novell.com/show_bug.cgi?id=516960 is fixed
			}
#else
			m_dataGridView.RowCount = m_list.Count;
#endif
			m_dataGridView.KeyTermRefs = m_list;
			m_dataGridView.RowEnter += m_dataGridView_RowEnter;
			Sort(m_sortedColumn, false, kRefCol);
			m_prevResultRow = -1;

			if (m_firstLoad && m_persistence != null)
				OnLoadSettings(m_persistence.SettingsKey);

			m_firstLoad = false;

			if (m_dataGridView.RowCount == 0)
			{
				if (ReferenceListEmptied != null)
					ReferenceListEmptied(this, EventArgs.Empty);
			}
			else
				OnReferenceChanged(0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Recursively populate a list with key term references
		/// </summary>
		/// <param name="keyTerm">ChkTerm which is part of the keyterm hierarchy</param>
		/// <param name="filteredBookIds">list of books in the filter, or null if no filter</param>
		/// ------------------------------------------------------------------------------------
		private void PopulateKeyTermRefs(IChkTerm keyTerm, List<int> filteredBookIds)
		{
			foreach (IChkRef chkRef in keyTerm.OccurrencesOS)
			{
				KeyTermRef keyRef = new KeyTermRef(chkRef);
				if (filteredBookIds == null || filteredBookIds.Contains(keyRef.RefInCurrVersification.Book))
				{
					keyRef.PropertyChanged -= OnKeyTermRefPropertyChanged;
					keyRef.PropertyChanged += OnKeyTermRefPropertyChanged;
					m_list.Add(keyRef);
				}
			}

			foreach (IChkTerm kt in keyTerm.SubPossibilitiesOS)
				PopulateKeyTermRefs(kt, filteredBookIds);
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string StatusBarTextFormat
		{
			get { return TeResourceHelper.GetResourceString("kstidKeyTermsVwStatusTextFmt"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of key term references in the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ReferenceCount
		{
			get { return m_list.Count; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selected key term reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal KeyTermRef SelectedReference
		{
			get { return m_dataGridView.SelectedReference; }
		}

		#endregion

		#region Events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the RowEnter event of the m_dataGridView control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_dataGridView_RowEnter(object sender, DataGridViewCellEventArgs e)
		{
			if (e == null || e.RowIndex == m_prevResultRow)
				return;
			OnReferenceChanged(e.RowIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the ReferenceChanged event.
		/// </summary>
		/// <param name="iRow">The index of the selected row.</param>
		/// ------------------------------------------------------------------------------------
		private void OnReferenceChanged(int iRow)
		{
			if (ReferenceChanged != null)
			{
				ReferenceChanged(this, new ScrRefEventArgs(GetKeyTermRef(iRow)));
				m_prevResultRow = iRow;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the ColumnHeaderMouseClick event of the m_dataGridView control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_dataGridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left)
				return;

			DataGridViewColumn column = m_dataGridView.Columns[e.ColumnIndex];
			if (column.SortMode == DataGridViewColumnSortMode.Programmatic)
				Sort(column, true, kRefCol);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the a property of a KeyTermRef changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.ComponentModel.PropertyChangedEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnKeyTermRefPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			DataGridViewColumn col = GetColumnFromPropertyName(e.PropertyName);
			if (col != null)
			{
				m_dataGridView.UpdateCellValue(
					col.Index, m_list.IndexOf(sender as KeyTermRef));
			}
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the column that displays the given property.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <returns>The column that displays the property.</returns>
		/// ------------------------------------------------------------------------------------
		private DataGridViewColumn GetColumnFromPropertyName(string propertyName)
		{
			switch (propertyName)
			{
				case "Status":
				case "RenderingStatus": return m_Status;
				case "Ref":
				case "Reference": return m_Reference;
				case "KeyWord":
				case "KeyWordString": return m_OriginalTerm;
				case "RenderingRA":
				case "Rendering": return m_Rendering;
			}
			return null;
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Gets the property name from column.
//		/// </summary>
//		/// <param name="column">The column.</param>
//		/// <returns>The property name</returns>
//		/// ------------------------------------------------------------------------------------
//		private string GetPropertyNameFromColumn(DataGridViewColumn column)
//		{
//			switch (column.Name)
//			{
//				case "m_Status": return m_Status.DataPropertyName;
//				case "m_Reference": return m_Reference.DataPropertyName;
//				case "m_OriginalTerm": return m_OriginalTerm.DataPropertyName;
//				case "m_Rendering": return m_Rendering.DataPropertyName;
//			}

//			return string.Empty;
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects the key term reference in the grid to the one whose guid is the same as
		/// that specified. If the specified guid cannot be found, then the first key term
		/// reference in the grid is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SelectKeyTermRef(Guid guid)
		{
#if __MonoCS__
			// setting m_dataGridView.CurrentCell to [0,0] when DisplayedColumnCount returns 0
			// Throws an exception on mono. The following code, adjusts the postioning so that
			// DisplayedColumnCount returns > 0.
			// TODO-Linux FWNX-189: remove when https://bugzilla.novell.com/show_bug.cgi?id=516960 is fixed
			if (m_dataGridView.DisplayedColumnCount(false) <= 0)
			{
				try
				{
					for(int i=0; i < m_dataGridView.Columns.Count; ++i)
						if (m_dataGridView.Columns[i].Width >= m_dataGridView.Width)
							m_dataGridView.Columns[i].Width = m_dataGridView.Width -1;
				}
				catch
				{
					return;
				}
			}
#endif
			if (m_dataGridView.RowCount > 0)
			{
				int i = (guid == Guid.Empty ? 0 : GetKeyTermRef(guid));
				m_dataGridView.CurrentCell =
					m_dataGridView[0, (i < 0 || i >= m_dataGridView.RowCount ? 0 : i)];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the key term reference for the specified guid. If a key term
		/// reference for the guid cannot be found, then -1 is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int GetKeyTermRef(Guid guid)
		{
			for (int i = 0; i < m_list.Count; i++)
			{
				KeyTermRef keyTermRef = m_list[i] as KeyTermRef;
				if (keyTermRef != null && keyTermRef.ChkRef.Guid == guid)
					return i;
			}

			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the key term reference for the specified row.
		/// </summary>
		/// <param name="rowIndex">Index of the row.</param>
		/// <returns>key term at the specified row, or null if one does not exist for the
		/// specified row</returns>
		/// ------------------------------------------------------------------------------------
		private KeyTermRef GetKeyTermRef(int rowIndex)
		{
			return (rowIndex < 0 || rowIndex >= m_list.Count ?
				null : m_list[rowIndex] as KeyTermRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the guid of the current row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override object GetPreSortRow()
		{
			KeyTermRef keyTermRef = SelectedReference;
			return (keyTermRef != null && keyTermRef.IsValid ? keyTermRef.ChkRef.Guid : Guid.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Use the supplied information to set the current key terms ref. row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void RestorePreSortRow(object restoreRow)
		{
			if (restoreRow != null && restoreRow.GetType() == typeof(Guid))
				SelectKeyTermRef((Guid)restoreRow);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnSaveSettings(Microsoft.Win32.RegistryKey key)
		{
			CheckDisposed();
			base.OnSaveSettings(key);

			KeyTermRef keyTermRef = SelectedReference;

			// Sometimes we get here when the cache has already been disposed but the ref.
			// is still an object. In that case referencing the guid will cause a crash,
			// therefore ignore exceptions.
			try
			{
				if (key != null && keyTermRef != null && keyTermRef != KeyTermRef.Empty)
					key.SetValue("SelectedKeyTermRef", keyTermRef.ChkRef.Guid);
			}
			catch { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoadSettings(Microsoft.Win32.RegistryKey key)
		{
			CheckDisposed();
			base.OnLoadSettings(key);

			if (m_list == null || key == null)
				return;

			string value = key.GetValue("SelectedKeyTermRef", null) as string;
			if (value != null)
			{
				Guid guid = new Guid(value);
				SelectKeyTermRef(guid);
			}
		}
	}

	#region KeyTermsGrid class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class KeyTermsGrid : CheckGrid
	{
		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the checking errors.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<ICheckGridRowObject> KeyTermRefs
		{
			set { m_list = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selected reference.
		/// </summary>
		/// <value>The selected reference.</value>
		/// ------------------------------------------------------------------------------------
		internal KeyTermRef SelectedReference
		{
			get
			{
				if (CurrentRow == null)
					return KeyTermRef.Empty;

				int rowIndex = CurrentRow.Index;
				return (m_list != null && m_list.Count > rowIndex &&
					rowIndex >= 0 ? m_list[rowIndex] as KeyTermRef :
					KeyTermRef.Empty);
			}
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when cell value needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
		{
			Debug.Assert(m_list != null, "Why did OnCellValueNeeded get called without first setting KeyTermRefs?");

			KeyTermRef keyTermRef = (m_list == null) ? null : m_list[e.RowIndex] as KeyTermRef;

			if (keyTermRef == null)
			{
				e.Value = null;
				return;
			}

			switch (e.ColumnIndex)
			{
				case KeyTermRenderingsControl.kRefCol:
					e.Value = keyTermRef.Reference;
					break;

				case KeyTermRenderingsControl.kRenderingCol:
					e.Value = keyTermRef.Rendering;
					break;

				case KeyTermRenderingsControl.kStatusCol:
					e.Value = keyTermRef.RenderingStatus;
					break;

				case KeyTermRenderingsControl.kCommentCol:
					e.Value = keyTermRef.KeyWordString;
					break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user presses a mouse button. If it is the right mouse button we
		/// select the current row (for the left mouse button we let .NET deal with it).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			if (e.Button == MouseButtons.Right)
			{
				HitTestInfo hti = HitTest(e.X, e.Y);
				if (hti.Type != DataGridViewHitTestType.None && hti.RowIndex >= 0)
					Rows[hti.RowIndex].Selected = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user releases the mouse button. If it is the right mouse button
		/// we want to show the popup menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);

			if (m_tmAdapter == null || e.Button != MouseButtons.Right)
				return;

//			DataGridView.HitTestInfo hti = HitTest(e.X, e.Y);
			//if (hti.RowIndex >= 0) -- Also allow context menu in header row
			{
				Point pt = PointToScreen(new Point(e.X, e.Y));
				m_tmAdapter.PopupMenu("cmnuKeyTermsRenderingView", pt.X, pt.Y);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FindIndexOfRefWithHvo(int hvo)
		{
			if (m_list != null && m_list.Count > 0)
			{
				for (int i = 0; i < m_list.Count; i++)
				{
					KeyTermRef keyTermRef = m_list[i] as KeyTermRef;
					if (keyTermRef != null && keyTermRef.ChkRef.Hvo == hvo)
						return i;
				}
			}

			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);

			if (tag != ChkRefTags.kflidRef &&
				tag != ChkRefTags.kflidRendering &&
				tag != ChkRefTags.kflidStatus &&
				tag != ChkRefTags.kflidKeyWord)
			{
				return;
			}

			int iKeyTermRef = FindIndexOfRefWithHvo(hvo);
			if (iKeyTermRef < 0)
				return;

			KeyTermRef keyTermRef = m_list[iKeyTermRef] as KeyTermRef;
			string propertyName = m_cache.MetaDataCacheAccessor.GetFieldName(tag);
			keyTermRef.OnPropertyChanged(propertyName);

			// In the data grid view we are displaying some artifical properties, yet
			// we change the underlaying property. If it is one of those we do a prop
			// changed for our artifical ones as well.
			int invalidateColumn = -1; // column to invalidate
			switch (propertyName)
			{
				default: return;
				case "Status":
					propertyName = "RenderingStatus";
					invalidateColumn = KeyTermRenderingsControl.kStatusCol;
					break;
				case "Ref":
					propertyName = "Reference";
					invalidateColumn = KeyTermRenderingsControl.kRefCol;
					break;
				case "KeyWord":
					propertyName = "KeyWordString";
					invalidateColumn = KeyTermRenderingsControl.kCommentCol;
					break;
				case "RenderingRA":
					propertyName = "Rendering";
					invalidateColumn = KeyTermRenderingsControl.kRenderingCol;
					break;
			}
			// TE-6621: In some cases (which developers nor JAARS testers can reproduce), the
			// column index is invalid for the datagrid, so we confirm that the index is
			// within the proper range.
			if (invalidateColumn >= 0 && invalidateColumn < ColumnCount)
				InvalidateColumn(invalidateColumn);

			keyTermRef.OnPropertyChanged(propertyName);
		}
	}

	#endregion
}
