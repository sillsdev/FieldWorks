// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	public class ColumnDragDropReorderedEventArgs : EventArgs
	{
		private List<int> m_displayedColumnOrder;

		/// <summary>
		/// Constructor.
		/// </summary>
		public ColumnDragDropReorderedEventArgs(List<int> newColumnOrder)
		{
			m_displayedColumnOrder = newColumnOrder;
		}

		/// <summary>
		/// Contains indices for the Columns collection in the order they are displayed.
		/// </summary>
		public List<int> DragDropColumnOrder => m_displayedColumnOrder ?? (m_displayedColumnOrder = new List<int>());
	}

	/// <summary>
	/// Handles drag-n-drop for reordering columns
	/// </summary>
	public delegate void ColumnDragDropReorderedHandler(object sender, ColumnDragDropReorderedEventArgs e);
}