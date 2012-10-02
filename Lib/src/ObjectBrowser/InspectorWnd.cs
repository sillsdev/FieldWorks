using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using WeifenLuo.WinFormsUI.Docking;

namespace SIL.ObjectBrowser
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class InspectorWnd : DockContent
	{
		/// <summary></summary>
		public delegate bool WillObjDisappearOnRefreshHandler(object sender, IInspectorObject io);
		/// <summary></summary>
		public event WillObjDisappearOnRefreshHandler WillObjDisappearOnRefresh;

		private IInspectorList m_list;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="InspectorWnd"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InspectorWnd()
		{
			InitializeComponent();
			gridInspector.CreateDefaultColumns();
			gridInspector.ShadingColor = (Properties.Settings.Default.UseShading ?
				Properties.Settings.Default.ShadeColor : Color.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				if (m_list != null)
				{
					m_list.BeginItemExpanding -= m_list_BeginItemExpanding;
					m_list.EndItemExpanding -= m_list_EndItemExpanding;
				}

				components.Dispose();
			}

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current inspector object.
		/// </summary>
		/// <value>The current inspector object.</value>
		/// ------------------------------------------------------------------------------------
		public IInspectorObject CurrentInspectorObject
		{
			get
			{
				if (m_list != null && m_list.Count > 0 && gridInspector.CurrentCellAddress.Y >= 0)
					return m_list[gridInspector.CurrentCellAddress.Y];

				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the top level object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public object TopLevelObject
		{
			get { return m_list.TopLevelObject; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the top level object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetTopLevelObject(object obj, IInspectorList list)
		{
			if (m_list != null)
			{
				m_list.BeginItemExpanding -= m_list_BeginItemExpanding;
				m_list.EndItemExpanding -= m_list_EndItemExpanding;
			}

			m_list = list;
			m_list.Initialize(obj);
			gridInspector.List = m_list;

			m_list.BeginItemExpanding += m_list_BeginItemExpanding;
			m_list.EndItemExpanding += m_list_EndItemExpanding;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the BeginItemExpanding event of the m_list control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		void m_list_BeginItemExpanding(object sender, EventArgs e)
		{
			Cursor = Cursors.WaitCursor;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the EndItemExpanding event of the m_list control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		void m_list_EndItemExpanding(object sender, EventArgs e)
		{
			Cursor = Cursors.Default;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the inspector list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IInspectorList InspectorList
		{
			get { return m_list; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the inspector grid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InspectorGrid InspectorGrid
		{
			get { return gridInspector; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CellMouseDown event of the gridInspector control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.DataGridViewCellMouseEventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void gridInspector_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.RowIndex < m_list.Count)
				gridInspector.CurrentCell = gridInspector[e.ColumnIndex, e.RowIndex];
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Handles the KeyPress event of the tstxtSearch control.
		///// </summary>
		///// <param name="sender">The source of the event.</param>
		///// <param name="e">The <see cref="System.Windows.Forms.KeyPressEventArgs"/> instance containing the event data.</param>
		///// ------------------------------------------------------------------------------------
		//private void tstxtSearch_KeyPress(object sender, KeyPressEventArgs e)
		//{
		//    if (e.KeyChar != (char)Keys.Enter)
		//        return;

		//    Guid guid = new Guid(tstxtSearch.Text.Trim());
		//    int i = m_list.GotoGuid(guid);
		//    if (i >= 0)
		//    {
		//        gridInspector.RowCount = m_list.Count;
		//        gridInspector.Invalidate();
		//        gridInspector.CurrentCell = gridInspector[0, i];
		//    }
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshView()
		{
			// Get the object that's displayed in the first visible row of the grid.
			IInspectorObject firstDisplayedObj =
				m_list[gridInspector.FirstDisplayedScrollingRowIndex];

			// Get the current, selected row in the grid.
			IInspectorObject currSelectedObj = gridInspector.CurrentObject;
			if (currSelectedObj != null && WillObjDisappearOnRefresh != null)
			{
				// Check if the selected object will disappear after refreshing the grid.
				// If so, then save a reference to the selected object's parent object.
				if (WillObjDisappearOnRefresh(this, currSelectedObj))
					currSelectedObj = m_list.GetParent(gridInspector.CurrentCellAddress.Y);
			}

			int keyFirstDisplayedObj = (firstDisplayedObj != null ? firstDisplayedObj.Key : -1);
			int keyCurrSelectedObj = (currSelectedObj != null ? currSelectedObj.Key : -1);

			// Save all the expanded objects.
			List<int> expandedObjects = new List<int>();

			for (int i = 0; i < m_list.Count; i++)
			{
				if (m_list.IsExpanded(i))
					expandedObjects.Add(m_list[i].Key);
			}

			m_list.Initialize(m_list.TopLevelObject);

			// Now that the list is rebuilt, go through the list of objects that
			// were previously expanded and expand them again.
			int firstRow = 0;
			int currRow = 0;
			int irow = 0;

			while (++irow < m_list.Count)
			{
				IInspectorObject io = m_list[irow];
				int key = io.Key;
				int index = expandedObjects.IndexOf(key);
				if (index >= 0)
				{
					m_list.ExpandObject(irow);
					expandedObjects.RemoveAt(index);
				}

				if (key == keyFirstDisplayedObj)
					firstRow = irow;

				if (key == keyCurrSelectedObj)
					currRow = irow;
			}

			gridInspector.SuspendLayout();
			gridInspector.List = m_list;
			gridInspector.FirstDisplayedScrollingRowIndex = firstRow;
			gridInspector.CurrentCell = gridInspector[0, currRow];
			gridInspector.ResumeLayout();
		}
			/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refreshes the view.
		/// This overload specifies the type of action that triggered this method.
		/// This is needed because the key changes during adds, updates, and moves.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshView(string type)
		{
			int mFirstDisplayIndex = 0, mCurrentDisplayIndex = 0;
			// Get the object that's displayed in the first visible row of the grid.
			mFirstDisplayIndex = gridInspector.FirstDisplayedScrollingRowIndex;

			// Get the current, selected row in the grid.
			IInspectorObject currSelectedObj = gridInspector.CurrentObject;

			// If type is "Up", "Down", or "Delete", use parent as the selected/current row.
			// If so, then save a reference to the selected object's parent object.
			if (type == "Up" || type == "Down" || type == "Delete" && (currSelectedObj != null))
				currSelectedObj = currSelectedObj.ParentInspectorObject;

			if (currSelectedObj != null)
			{
				for (int i = 0; i < m_list.Count; i++)
				{
					if (m_list[i].Key == currSelectedObj.Key)
					{
						mCurrentDisplayIndex = i;
						break;
					}
				}
			}


			// Save all the expanded objects.
			List<int> expandedObjects = new List<int>();

			for (int i = 0; i < m_list.Count; i++)
			{
				if (m_list.IsExpanded(i))
					expandedObjects.Add(i);
			}
			if (type == "Add")
				expandedObjects.Add(mCurrentDisplayIndex);

			m_list.Initialize(m_list.TopLevelObject);

			// Now that the list is rebuilt, go through the list of objects that
			// were previously expanded and expand them again.
			int firstRow = 0;
			int currRow = 0;
			int irow = 0;

			for (irow = 0; irow < m_list.Count; irow++)
			{
				IInspectorObject io = m_list[irow];
				int index = expandedObjects.IndexOf(irow);
				if (index >= 0)
				{
					m_list.ExpandObject(irow);
					expandedObjects.RemoveAt(index);
				}

				if (irow == mFirstDisplayIndex)
					firstRow = irow;

				if (irow == mCurrentDisplayIndex)
					currRow = irow;
			}

			gridInspector.SuspendLayout();
			gridInspector.List = m_list;
			gridInspector.FirstDisplayedScrollingRowIndex = firstRow;
			gridInspector.CurrentCell = gridInspector[0, currRow];
			gridInspector.ResumeLayout();
		}
}
}
