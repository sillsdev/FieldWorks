// Copyright (c) 2009-2018 SIL International
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
	public partial class InspectorWnd : DockContent
	{
		/// <summary />
		public delegate bool WillObjDisappearOnRefreshHandler(object sender, IInspectorObject io);
		/// <summary />
		public event WillObjDisappearOnRefreshHandler WillObjDisappearOnRefresh;

		/// <summary />
		public InspectorWnd()
		{
			InitializeComponent();
			gridInspector.CreateDefaultColumns();
			gridInspector.ShadingColor = Properties.Settings.Default.UseShading ? Properties.Settings.Default.ShadeColor : Color.Empty;
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
		public IInspectorObject CurrentInspectorObject => InspectorList != null && InspectorList.Count > 0
				&& gridInspector.CurrentCellAddress.Y >= 0 ? InspectorList[gridInspector.CurrentCellAddress.Y] : null;

		/// <summary>
		/// Sets the top level object.
		/// </summary>
		public void SetTopLevelObject(object obj, IInspectorList list)
		{
			if (InspectorList != null)
			{
				InspectorList.BeginItemExpanding -= m_list_BeginItemExpanding;
				InspectorList.EndItemExpanding -= m_list_EndItemExpanding;
			}

			InspectorList = list;
			InspectorList.Initialize(obj);
			gridInspector.List = InspectorList;

			InspectorList.BeginItemExpanding += m_list_BeginItemExpanding;
			InspectorList.EndItemExpanding += m_list_EndItemExpanding;
		}

		/// <summary>
		/// Handles the BeginItemExpanding event of the m_list control.
		/// </summary>
		void m_list_BeginItemExpanding(object sender, EventArgs e)
		{
			Cursor = Cursors.WaitCursor;
		}

		/// <summary>
		/// Handles the EndItemExpanding event of the m_list control.
		/// </summary>
		void m_list_EndItemExpanding(object sender, EventArgs e)
		{
			Cursor = Cursors.Default;
		}

		/// <summary>
		/// Gets the inspector list.
		/// </summary>
		public IInspectorList InspectorList { get; private set; }

		/// <summary>
		/// Gets the inspector grid.
		/// </summary>
		public InspectorGrid InspectorGrid => gridInspector;

		/// <summary>
		/// Handles the CellMouseDown event of the gridInspector control.
		/// </summary>
		private void gridInspector_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.RowIndex < InspectorList.Count)
			{
				gridInspector.CurrentCell = gridInspector[e.ColumnIndex, e.RowIndex];
			}
		}

		/// <summary>
		/// Refreshes the view.
		/// </summary>
		public void RefreshView()
		{
			// Get the object that's displayed in the first visible row of the grid.
			var firstDisplayedObj = InspectorList[gridInspector.FirstDisplayedScrollingRowIndex];

			// Get the current, selected row in the grid.
			var currSelectedObj = gridInspector.CurrentObject;
			if (currSelectedObj != null && WillObjDisappearOnRefresh != null)
			{
				// Check if the selected object will disappear after refreshing the grid.
				// If so, then save a reference to the selected object's parent object.
				if (WillObjDisappearOnRefresh(this, currSelectedObj))
				{
					currSelectedObj = InspectorList.GetParent(gridInspector.CurrentCellAddress.Y);
				}
			}

			var keyFirstDisplayedObj = (firstDisplayedObj != null ? firstDisplayedObj.Key : -1);
			var keyCurrSelectedObj = (currSelectedObj != null ? currSelectedObj.Key : -1);

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

			gridInspector.SuspendLayout();
			gridInspector.List = InspectorList;
			gridInspector.FirstDisplayedScrollingRowIndex = firstRow;
			gridInspector.CurrentCell = gridInspector[0, currRow];
			gridInspector.ResumeLayout();
		}

		/// <summary>
		/// Refreshes the view.
		/// This overload specifies the type of action that triggered this method.
		/// This is needed because the key changes during adds, updates, and moves.
		/// </summary>
		public void RefreshView(string type)
		{
			int mFirstDisplayIndex = 0, mCurrentDisplayIndex = 0;
			// Get the object that's displayed in the first visible row of the grid.
			mFirstDisplayIndex = gridInspector.FirstDisplayedScrollingRowIndex;

			// Get the current, selected row in the grid.
			var currSelectedObj = gridInspector.CurrentObject;

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

			gridInspector.SuspendLayout();
			gridInspector.List = InspectorList;
			gridInspector.FirstDisplayedScrollingRowIndex = firstRow;
			gridInspector.CurrentCell = gridInspector[0, currRow];
			gridInspector.ResumeLayout();
		}
	}
}