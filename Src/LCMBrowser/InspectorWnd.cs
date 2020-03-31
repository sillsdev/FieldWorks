// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace LCMBrowser
{
	/// <summary />
	internal sealed partial class InspectorWnd : DockContent
	{
		/// <summary />
		internal delegate bool WillObjDisappearOnRefreshHandler(object sender, IInspectorObject io);
		/// <summary />
		internal event WillObjDisappearOnRefreshHandler WillObjDisappearOnRefresh;

		/// <summary />
		internal InspectorWnd()
		{
			InitializeComponent();
			InspectorGrid.CreateDefaultColumns();
			InspectorGrid.ShadingColor = Properties.Settings.Default.UseShading ? Properties.Settings.Default.ShadeColor : Color.Empty;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (disposing)
			{
				if (InspectorList != null)
				{
					InspectorList.BeginItemExpanding -= m_list_BeginItemExpanding;
					InspectorList.EndItemExpanding -= m_list_EndItemExpanding;
				}

				components?.Dispose();
			}

			base.Dispose(disposing);
		}

		/// <summary>
		/// Gets the current inspector object.
		/// </summary>
		internal IInspectorObject CurrentInspectorObject => InspectorList != null && InspectorList.Count > 0
																				  && InspectorGrid.CurrentCellAddress.Y >= 0 ? InspectorList[InspectorGrid.CurrentCellAddress.Y] : null;

		/// <summary>
		/// Sets the top level object.
		/// </summary>
		internal void SetTopLevelObject(object obj, IInspectorList list)
		{
			if (InspectorList != null)
			{
				InspectorList.BeginItemExpanding -= m_list_BeginItemExpanding;
				InspectorList.EndItemExpanding -= m_list_EndItemExpanding;
			}

			InspectorList = list;
			InspectorList.Initialize(obj);
			InspectorGrid.List = InspectorList;

			InspectorList.BeginItemExpanding += m_list_BeginItemExpanding;
			InspectorList.EndItemExpanding += m_list_EndItemExpanding;
		}

		/// <summary>
		/// Handles the BeginItemExpanding event of the m_list control.
		/// </summary>
		private void m_list_BeginItemExpanding(object sender, EventArgs e)
		{
			Cursor = Cursors.WaitCursor;
		}

		/// <summary>
		/// Handles the EndItemExpanding event of the m_list control.
		/// </summary>
		private void m_list_EndItemExpanding(object sender, EventArgs e)
		{
			Cursor = Cursors.Default;
		}

		/// <summary>
		/// Gets the inspector list.
		/// </summary>
		internal IInspectorList InspectorList { get; private set; }

		/// <summary>
		/// Gets the inspector grid.
		/// </summary>
		internal InspectorGrid InspectorGrid { get; private set; }

		/// <summary>
		/// Handles the CellMouseDown event of the gridInspector control.
		/// </summary>
		private void gridInspector_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.RowIndex < InspectorList.Count)
			{
				InspectorGrid.CurrentCell = InspectorGrid[e.ColumnIndex, e.RowIndex];
			}
		}

		/// <summary>
		/// Refreshes the view.
		/// </summary>
		internal void RefreshView()
		{
			// Get the object that's displayed in the first visible row of the grid.
			var firstDisplayedObj = InspectorList[InspectorGrid.FirstDisplayedScrollingRowIndex];

			// Get the current, selected row in the grid.
			var currSelectedObj = InspectorGrid.CurrentObject;
			if (currSelectedObj != null && WillObjDisappearOnRefresh != null)
			{
				// Check if the selected object will disappear after refreshing the grid.
				// If so, then save a reference to the selected object's parent object.
				if (WillObjDisappearOnRefresh(this, currSelectedObj))
				{
					currSelectedObj = InspectorList.GetParent(InspectorGrid.CurrentCellAddress.Y);
				}
			}

			var keyFirstDisplayedObj = firstDisplayedObj?.Key ?? -1;
			var keyCurrSelectedObj = currSelectedObj?.Key ?? -1;
			// Save all the expanded objects.
			var expandedObjects = new List<int>();

			for (var i = 0; i < InspectorList.Count; i++)
			{
				if (InspectorList.IsExpanded(i))
				{
					expandedObjects.Add(InspectorList[i].Key);
				}
			}

			InspectorList.Initialize(InspectorList.TopLevelObject);

			// Now that the list is rebuilt, go through the list of objects that
			// were previously expanded and expand them again.
			var firstRow = 0;
			var currRow = 0;
			var irow = 0;

			while (++irow < InspectorList.Count)
			{
				var io = InspectorList[irow];
				var key = io.Key;
				var index = expandedObjects.IndexOf(key);
				if (index >= 0)
				{
					InspectorList.ExpandObject(irow);
					expandedObjects.RemoveAt(index);
				}
				if (key == keyFirstDisplayedObj)
				{
					firstRow = irow;
				}
				if (key == keyCurrSelectedObj)
				{
					currRow = irow;
				}
			}

			InspectorGrid.SuspendLayout();
			InspectorGrid.List = InspectorList;
			InspectorGrid.FirstDisplayedScrollingRowIndex = firstRow;
			InspectorGrid.CurrentCell = InspectorGrid[0, currRow];
			InspectorGrid.ResumeLayout();
		}

		/// <summary>
		/// Refreshes the view.
		/// This overload specifies the type of action that triggered this method.
		/// This is needed because the key changes during adds, updates, and moves.
		/// </summary>
		internal void RefreshView(string type)
		{
			var mCurrentDisplayIndex = 0;
			// Get the object that's displayed in the first visible row of the grid.
			var mFirstDisplayIndex = InspectorGrid.FirstDisplayedScrollingRowIndex;

			// Get the current, selected row in the grid.
			var currSelectedObj = InspectorGrid.CurrentObject;

			// If type is "Up", "Down", or "Delete", use parent as the selected/current row.
			// If so, then save a reference to the selected object's parent object.
			if (type == "Up" || type == "Down" || type == "Delete" && currSelectedObj != null)
			{
				currSelectedObj = currSelectedObj.ParentInspectorObject;
			}
			if (currSelectedObj != null)
			{
				for (var i = 0; i < InspectorList.Count; i++)
				{
					if (InspectorList[i].Key == currSelectedObj.Key)
					{
						mCurrentDisplayIndex = i;
						break;
					}
				}
			}

			// Save all the expanded objects.
			var expandedObjects = new List<int>();

			for (var i = 0; i < InspectorList.Count; i++)
			{
				if (InspectorList.IsExpanded(i))
				{
					expandedObjects.Add(i);
				}
			}
			if (type == "Add")
			{
				expandedObjects.Add(mCurrentDisplayIndex);
			}
			InspectorList.Initialize(InspectorList.TopLevelObject);

			// Now that the list is rebuilt, go through the list of objects that
			// were previously expanded and expand them again.
			var firstRow = 0;
			var currRow = 0;
			int irow;

			for (irow = 0; irow < InspectorList.Count; irow++)
			{
				var index = expandedObjects.IndexOf(irow);
				if (index >= 0)
				{
					InspectorList.ExpandObject(irow);
					expandedObjects.RemoveAt(index);
				}
				if (irow == mFirstDisplayIndex)
				{
					firstRow = irow;
				}
				if (irow == mCurrentDisplayIndex)
				{
					currRow = irow;
				}
			}

			InspectorGrid.SuspendLayout();
			InspectorGrid.List = InspectorList;
			InspectorGrid.FirstDisplayedScrollingRowIndex = firstRow;
			InspectorGrid.CurrentCell = InspectorGrid[0, currRow];
			InspectorGrid.ResumeLayout();
		}
	}
}