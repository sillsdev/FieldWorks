// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CheckGrid.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.UIAdapters;
using System.ComponentModel;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.Controls;
using Microsoft.Win32;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;
using System.Drawing;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ICornerGlyphGrid
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a value indicating whether or not to paint a red corner in the cell
		/// whose address is the specified row and column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool ShouldDrawCornerGlyph(int iCol, int iRow);
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CheckGrid : DataGridView, IVwNotifyChange, ICornerGlyphGrid
	{
		/// <summary></summary>
		protected FdoCache m_cache;
		/// <summary></summary>
		protected ITMAdapter m_tmAdapter;
		/// <summary></summary>
		protected List<ICheckGridRowObject> m_list;

		private int m_rowHeight = 20;
		private RegistryKey m_settingsKey;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckGrid()
		{
			DoubleBuffered = true;
			AutoGenerateColumns = false;
			VirtualMode = true;
			AllowUserToAddRows = false;
			DefaultCellStyle.Font = SystemInformation.MenuFont;
			ShowCellToolTips = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public FdoCache Cache
		{
			get { return m_cache; }
			set
			{
				System.Diagnostics.Debug.Assert(m_cache == null);
				System.Diagnostics.Debug.Assert(value != null);
				m_cache = value;
				m_cache.MainCacheAccessor.AddNotification(this);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the toolbar menu adapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITMAdapter TMAdapter
		{
			set { m_tmAdapter = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int RowHeight
		{
			get { return m_rowHeight; }
			set
			{
				m_rowHeight = value;
				if (RowCount > 0)
					UpdateRowHeightInfo(0, true);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RegistryKey SettingsKey
		{
			get { return m_settingsKey; }
			set { m_settingsKey = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the height of the row when it's requested.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnRowHeightInfoNeeded(DataGridViewRowHeightInfoNeededEventArgs e)
		{
			e.Height = m_rowHeight;
			e.MinimumHeight = m_rowHeight;
			base.OnRowHeightInfoNeeded(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// In order to achieve double buffering without the problem that arises from having
		/// double buffering on while sizing rows and columns or dragging columns around,
		/// monitor when the mouse goes down and turn off double buffering when it goes down
		/// on a column heading or over the dividers between rows or the dividers between
		/// columns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e)
		{
			if (e.RowIndex == -1 || (e.ColumnIndex == -1 && Cursor == Cursors.SizeNS))
				DoubleBuffered = false;

			base.OnCellMouseDown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.DataGridView.CellMouseUp"></see> event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellMouseUp(DataGridViewCellMouseEventArgs e)
		{
			// When double buffering is off, it means it was turned off in
			// the cell mouse down event. Therefore, turn it back on.
			if (!DoubleBuffered)
				DoubleBuffered = true;

			base.OnCellMouseUp(e);

			if (e.Button != MouseButtons.Right || e.RowIndex < 0)
				return;

			// At this point, we know the user clicked the right mouse button.
			OnCellRightMouseUp(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the row in which the user clicked is not the current row, then make it current.
		/// </summary>
		/// <param name="e">The <see cref="System.Windows.Forms.DataGridViewCellMouseEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnCellRightMouseUp(DataGridViewCellMouseEventArgs e)
		{
			if (CurrentCellAddress.Y != e.RowIndex)
				CurrentCell = this[e.ColumnIndex, e.RowIndex];
		}

		#region IVwNotifyChange Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
		}

		#endregion

		#region ICornerGlyphGrid Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual bool ShouldDrawCornerGlyph(int iCol, int iRow)
		{
			return false;
		}

		#endregion
	}
}
