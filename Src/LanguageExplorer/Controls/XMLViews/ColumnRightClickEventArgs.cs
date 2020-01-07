// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <remarks>
	/// Specialized event argument for right mouse button clicks in list view column headers.
	/// </remarks>
	public class ColumnRightClickEventArgs : EventArgs
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public ColumnRightClickEventArgs(int icol, Point ptLoc)
		{
			Column = icol;
			Location = ptLoc;
		}

		/// <summary>
		/// Gets the column.
		/// </summary>
		/// <value>The column.</value>
		public int Column { get; }

		/// <summary>
		/// Gets the location.
		/// </summary>
		/// <value>The location.</value>
		public Point Location { get; }
	}

	/// <summary>
	/// Specialized event handler for right mouse button clicks in list view column headers.
	/// </summary>
	public delegate void ColumnRightClickEventHandler(object sender, ColumnRightClickEventArgs e);
}