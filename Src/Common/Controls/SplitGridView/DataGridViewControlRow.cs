// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataGridViewControlRow.cs
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
	/// Represents a row of DataGridViewControlCells in a CollapsibleDataGridView.
	/// We enhance DataGridViewRow and implement some of the functionality DataGridViewColumn
	/// has for AutoSizeColumnsMode.Fill.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DataGridViewControlRow: DataGridViewRow
	{
		private float m_FillWeight;
		private bool m_fAutoFill;
		private DataGridViewAdvancedBorderStyle m_borderStyle;
		private DataGridViewTriState m_Resizable;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DataGridViewControlRow"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DataGridViewControlRow()
			: base()
		{
			MinimumHeight = DataGridViewControlColumn.kMinimumValue;
			m_FillWeight = 100;
			m_fAutoFill = true;
			m_Resizable = DataGridViewTriState.NotSet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an exact copy of this row.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Object"></see> that represents the cloned
		/// <see cref="T:System.Windows.Forms.DataGridViewRow"></see>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override object Clone()
		{
			DataGridViewControlRow row = base.Clone() as DataGridViewControlRow;
			row.m_FillWeight = m_FillWeight;
			row.m_fAutoFill = m_fAutoFill;
			return row;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the fill weight.
		/// </summary>
		/// <remarks>The default value is 100.</remarks>
		/// <value>The fill weight.</value>
		/// ------------------------------------------------------------------------------------
		public float FillWeight
		{
			get { return m_FillWeight; }
			set { m_FillWeight = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the fill weight of this row should be
		/// included when calculating auto fill.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is auto fill; otherwise, <c>false</c>.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public bool IsAutoFill
		{
			get { return m_fAutoFill; }
			set { m_fAutoFill = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the advanced border style.
		/// </summary>
		/// <value>The advanced border style.</value>
		/// ------------------------------------------------------------------------------------
		public DataGridViewAdvancedBorderStyle AdvancedBorderStyle
		{
			get { return m_borderStyle; }
			set { m_borderStyle = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether users can resize the row or indicating that
		/// the behavior is inherited from the
		/// <see cref="P:System.Windows.Forms.DataGridView.AllowUserToResizeRows"></see> property.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Windows.Forms.DataGridViewTriState"></see> value that
		/// indicates whether the row can be resized or whether it can be resized only when the
		/// <see cref="P:System.Windows.Forms.DataGridView.AllowUserToResizeRows"></see> property
		/// is set to true.
		/// </returns>
		/// <exception cref="T:System.InvalidOperationException">The row is in a
		/// <see cref="T:System.Windows.Forms.DataGridView"></see> control and is a shared
		/// row.</exception>
		/// ------------------------------------------------------------------------------------
		public override DataGridViewTriState Resizable
		{
			get
			{
				return base.Resizable;
			}
			set
			{
				m_Resizable = value;
				base.Resizable = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the Resizable property without modifying the m_Resizable variable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DataGridViewTriState BaseResizable
		{
			get { return base.Resizable; }
			set { base.Resizable = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the explicitly set Resizable property value from m_Resizable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal DataGridViewTriState InternalResizable
		{
			get { return m_Resizable; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether Resizable was set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool ResizableSet
		{
			get { return m_Resizable != DataGridViewTriState.NotSet; }
		}

	}
}
