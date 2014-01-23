// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataGridViewControlColumn.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls.SplitGridView
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a column of DataGridViewControlCells in a CollapsibleDataGridView.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DataGridViewControlColumn: DataGridViewColumn
	{
		/// <summary>The minimum value that MinimumWidth/MinimumHeight can take.</summary>
		public const int kMinimumValue = 2;

		private int m_ThresholdWidth;
		private int m_StandardWidth;
		private float m_MaxPercentage;
		private bool m_fIsCollapsible;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DataGridViewControlColumn"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DataGridViewControlColumn()
			: base(new DataGridViewControlCell())
		{
			// by default this column isn't visible.
			Visible = false;
			MinimumWidth = kMinimumValue;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DataGridViewControlColumn"/> class.
		/// </summary>
		/// <param name="fLastColumn"><c>true</c>if this is the last column. This causes the
		/// column not to be resizable.</param>
		/// ------------------------------------------------------------------------------------
		public DataGridViewControlColumn(bool fLastColumn)
			: this()
		{
			Resizable = fLastColumn ? DataGridViewTriState.False : DataGridViewTriState.True;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an exact copy of this column.
		/// </summary>
		/// <returns>An object that represents the cloned DataGridViewControlColumn.</returns>
		/// ------------------------------------------------------------------------------------
		public override object Clone()
		{
			DataGridViewControlColumn column = base.Clone() as DataGridViewControlColumn;
			column.m_fIsCollapsible = m_fIsCollapsible;
			column.m_MaxPercentage = m_MaxPercentage;
			column.m_StandardWidth = m_StandardWidth;
			column.m_ThresholdWidth = m_ThresholdWidth;
			return column;
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
				DataGridView.ColumnStateChanged +=
					new DataGridViewColumnStateChangedEventHandler(OnColumnStateChanged);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the minimum width that this column is displayed with. The user can
		/// drag the splitter below this threshhold width but it will be reset to this width.
		/// If they collapse this column by dragging the splitter, the column will be collapsed.
		/// </summary>
		/// <value>The width of the threshhold.</value>
		/// ------------------------------------------------------------------------------------
		public int ThresholdWidth
		{
			get { return m_ThresholdWidth; }
			set
			{
				m_ThresholdWidth = value;
				if (m_StandardWidth < m_ThresholdWidth)
					m_StandardWidth = m_ThresholdWidth;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the width that this column will have when it is turned on. This gets
		/// set by the CollapsibleDataGridView when the user drags the splitter.
		/// </summary>
		/// <value>The width of the standard.</value>
		/// ------------------------------------------------------------------------------------
		public int StandardWidth
		{
			get { return m_StandardWidth; }
			set
			{
				m_StandardWidth = value;
				if (m_StandardWidth < m_ThresholdWidth)
					m_StandardWidth = m_ThresholdWidth;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the maximum percentage this column can occupy.
		/// </summary>
		/// <value>The max percentage.</value>
		/// ------------------------------------------------------------------------------------
		public float MaxPercentage
		{
			get { return m_MaxPercentage; }
			set { m_MaxPercentage = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:DataGridViewControlColumn"/>
		/// is collapsible.
		/// </summary>
		/// <value><c>true</c> if collapsible; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public bool IsCollapsible
		{
			get { return m_fIsCollapsible; }
			set { m_fIsCollapsible = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the column state changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DataGridViewColumnStateChangedEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnColumnStateChanged(object sender, DataGridViewColumnStateChangedEventArgs e)
		{
			if (e.Column != this || e.StateChanged != DataGridViewElementStates.Visible)
				return;

			if (Visible && Width <= kMinimumValue)
				Width = m_StandardWidth;
		}

	}
}
