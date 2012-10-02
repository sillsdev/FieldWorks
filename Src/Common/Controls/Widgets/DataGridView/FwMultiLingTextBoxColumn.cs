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
// File: FwTextBoxColumn.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using System.Drawing;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A column in a DataGridView that consists of FwTextBoxCells.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwMultiLingTextBoxColumn : FwTextBoxColumn
	{
		#region Constructor and Dispose
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwTextBoxColumn"/> class.
		/// </summary>
		/// <remarks>Used by Designer</remarks>
		/// ------------------------------------------------------------------------------------
		public FwMultiLingTextBoxColumn()
			: this(null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwTextBoxColumn"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public FwMultiLingTextBoxColumn(FdoCache cache) : base(cache)
		{
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ICU locale.
		/// </summary>
		/// <param name="rowIndex">Index of the row.</param>
		/// <returns>The ICU locale</returns>
		/// ------------------------------------------------------------------------------------
		private string GetIcuLocale(int rowIndex)
		{
			int ws = GetWritingSystem(rowIndex);
			return m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system.
		/// </summary>
		/// <param name="rowIndex">Index of the row.</param>
		/// <returns>The HVO of the writing system</returns>
		/// ------------------------------------------------------------------------------------
		internal int GetWritingSystem(int rowIndex)
		{
			if (rowIndex >= 0)
			{
				FwTextBoxRow row = DataGridView.Rows[rowIndex] as FwTextBoxRow;
				if (row != null)
					return row.WritingSystemHvo;
			}

			return m_ws;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the band is associated with a different
		/// <see cref="T:System.Windows.Forms.DataGridView"></see>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnDataGridViewChanged()
		{
			base.OnDataGridViewChanged();

			if (DataGridView != null)
			{
				// In case we already subscribed to the events we want to remove those handlers
				// so that we don't get called twice.
				DataGridView.ColumnWidthChanged -= new DataGridViewColumnEventHandler(OnColumnWidthChanged);
				DataGridView.SortCompare -= new DataGridViewSortCompareEventHandler(OnSortCompare);
				DataGridView.DefaultCellStyleChanged -= new EventHandler(OnDefaultCellStyleChanged);
				DataGridView.ColumnWidthChanged += new DataGridViewColumnEventHandler(OnColumnWidthChanged);
				DataGridView.SortCompare += new DataGridViewSortCompareEventHandler(OnSortCompare);
				DataGridView.DefaultCellStyleChanged += new EventHandler(OnDefaultCellStyleChanged);
				DataGridView.RowHeightChanged += new DataGridViewRowEventHandler(DataGridView_RowHeightChanged);

				m_cellStyle = InheritedStyle;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the text box control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeTextBoxControl(ITsString tss, int rowIndex)
		{
			if (rowIndex < 0 || TextBoxControl == null)
				return;

			if (m_cache != null)
			{
				m_textBoxControl.WritingSystemFactory =
					m_cache.LanguageWritingSystemFactoryAccessor;
			}

			m_textBoxControl.WritingSystemCode = GetWritingSystem(rowIndex);
			m_textBoxControl.Tss = tss;
		}
	}
}
