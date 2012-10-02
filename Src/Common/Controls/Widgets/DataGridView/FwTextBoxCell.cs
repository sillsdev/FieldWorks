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
// File: FwTextBoxCell.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Diagnostics;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Displays editable text information from a FwTextBox in a DataGridView control.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwTextBoxCell : DataGridViewTextBoxCell
	{
		private ITsString m_tssValue;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwTextBoxCell"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwTextBoxCell() : base()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attaches and initializes the hosted editing control.
		/// </summary>
		/// <param name="rowIndex">The index of the row being edited.</param>
		/// <param name="initialFormattedValue">The initial value to be displayed in the
		/// control.</param>
		/// <param name="dataGridViewCellStyle">A cell style that is used to determine the
		/// appearance of the hosted control.</param>
		/// <PermissionSet>
		///		<IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		///		<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		///		<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/>
		///		<IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		/// ------------------------------------------------------------------------------------
		public override void InitializeEditingControl(int rowIndex, object initialFormattedValue,
			DataGridViewCellStyle dataGridViewCellStyle)
		{
			base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
			FwTextBoxControl ctrl = DataGridView.EditingControl as FwTextBoxControl;
			InitializeTextBoxControl(ctrl, rowIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the text box control.
		/// </summary>
		/// <param name="rowIndex">The row index.</param>
		/// <remarks><paramref name="rowIndex"/> is usually the same as RowIndex, except in the
		/// case of a shared row (in which case RowIndex is always -1).</remarks>
		/// ------------------------------------------------------------------------------------
		private void InitializeTextBoxControl(int rowIndex)
		{
			InitializeTextBoxControl(null, rowIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the text box control.
		/// </summary>
		/// <param name="ctrl">The FwTextBox control to initialize.</param>
		/// <param name="rowIndex">The row index.</param>
		/// <remarks><paramref name="rowIndex"/> is usually the same as RowIndex, except in the
		/// case of a shared row (in which case RowIndex is always -1).</remarks>
		/// ------------------------------------------------------------------------------------
		private void InitializeTextBoxControl(FwTextBoxControl ctrl, int rowIndex)
		{
			if (rowIndex < 0)
				return;

			// The owning column owns the FwTextBoxControl we need
			// to edit the cell. So let it do the intialization.
			FwTextBoxColumn col = OwningColumn as FwTextBoxColumn;
			if (col != null)
				col.InitializeTextBoxControl(ctrl, GetValue(rowIndex) as ITsString, rowIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the cell's hosted FwTextBoxCell editing control (which is an
		/// FwTextBoxControl).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Type EditType
		{
			get	{ return typeof(FwTextBoxControl); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the type of value contained in the FwTextBoxCell.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Type ValueType
		{
			get { return typeof(ITsString); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the ITsString
		/// </summary>
		/// <param name="formattedValue">The display value of the cell.</param>
		/// <param name="cellStyle">The <see cref="T:System.Windows.Forms.DataGridViewCellStyle"/>
		/// in effect for the cell.</param>
		/// <param name="formattedValueTypeConverter">A <see cref="T:System.ComponentModel.TypeConverter"/>
		/// for the display value type, or null to use the default converter.</param>
		/// <param name="valueTypeConverter">A <see cref="T:System.ComponentModel.TypeConverter"/>
		/// for the cell value type, or null to use the default converter.</param>
		/// <returns>The cell value.</returns>
		/// <remarks>
		/// Get the ITsString from the text box control rather than trying to parse/convert it
		/// from the string - there is no type converter that could do it and we would need to
		/// know the writing system etc.
		/// NOTE: <paramref name="formattedValue"/> contains either the new value or the previous
		/// value (if the user cancels). This solution always returns the new value. However,
		/// this is currently not a problem because Cancel is handled by the dialog.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public override object ParseFormattedValue(object formattedValue,
			DataGridViewCellStyle cellStyle, TypeConverter formattedValueTypeConverter,
			TypeConverter valueTypeConverter)
		{
			return ((FwTextBoxControl)DataGridView.EditingControl).Tss;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the formatted value associated with the cell.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Type FormattedValueType
		{
			get { return typeof(string); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default value for a cell in the row for new records.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override object DefaultNewRowValue
		{
			get
			{
				FwTextBoxColumn col = OwningColumn as FwTextBoxColumn;
				return (col == null ? null : col.GetDefaultNewRowValue(RowIndex));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text value of the cell's TsString and uses it as the value to display.
		/// </summary>
		/// <param name="value">The value to be formatted.</param>
		/// <param name="rowIndex">The index of the cell's parent row.</param>
		/// <param name="cellStyle">The <see cref="T:System.Windows.Forms.DataGridViewCellStyle"/>
		/// in effect for the cell.</param>
		/// <param name="valueTypeConverter">A <see cref="T:System.ComponentModel.TypeConverter"/>
		/// associated with the value type that provides custom conversion to the formatted value
		/// type, or null if no such custom conversion is needed.</param>
		/// <param name="formattedValueTypeConverter">A <see cref="T:System.ComponentModel.TypeConverter"/>
		/// associated with the formatted value type that provides custom conversion from the value
		/// type, or null if no such custom conversion is needed.</param>
		/// <param name="context">A bitwise combination of
		/// <see cref="T:System.Windows.Forms.DataGridViewDataErrorContexts"/> values describing
		/// the context in which the formatted value is needed.</param>
		/// <returns>
		/// The formatted value of the cell or null if the cell does not belong to a
		/// <see cref="T:System.Windows.Forms.DataGridView"></see> control.
		/// </returns>
		/// <exception cref="T:System.Exception">Formatting failed and either there is no
		/// handler for the <see cref="E:System.Windows.Forms.DataGridView.DataError"/> event
		/// of the <see cref="T:System.Windows.Forms.DataGridView"></see> control or the
		/// handler set the <see cref="P:System.Windows.Forms.DataGridViewDataErrorEventArgs.ThrowException"/>
		/// property to true. The exception object can typically be cast to type
		/// <see cref="T:System.FormatException"></see>.</exception>
		/// ------------------------------------------------------------------------------------
		protected override object GetFormattedValue(object value, int rowIndex,
			ref DataGridViewCellStyle cellStyle, TypeConverter valueTypeConverter,
			TypeConverter formattedValueTypeConverter, DataGridViewDataErrorContexts context)
		{
			m_tssValue = value as ITsString;
			return base.GetFormattedValue(m_tssValue != null ? m_tssValue.Text : string.Empty,
				rowIndex, ref cellStyle, valueTypeConverter, formattedValueTypeConverter, context);
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Renders the text as bitmap.
		///// </summary>
		///// <param name="value">The value.</param>
		///// <param name="rowIndex">Index of the row.</param>
		///// <param name="fSelected"><c>true</c> if the cell is selected</param>
		///// <returns>The text rendered as bitmap</returns>
		///// ------------------------------------------------------------------------------------
		//private Bitmap RenderTextAsBitmap(object value, int rowIndex, bool fSelected)
		//{
		//    if (DataGridView == null || !(value is ITsString))
		//        return null;

		//    if (!DataGridView.Rows[rowIndex].Displayed)
		//        return null;

		//    m_tssValue = value as ITsString;
		//    FwTextBoxColumn column = (FwTextBoxColumn)OwningColumn;
		//    FwTextBoxControl ctrl = column.TextBoxControl;

		//    InitializeTextBoxControl(rowIndex);

		//    // We return a bitmap as formatted value that can be displayed without a FwTextBox
		//    // being shown. By doing this we don't need a text box for every cell.

		//    // First step is to scroll the selection into view. This is especially important
		//    // when we switch between RTL and LTR text (otherwise it might show up as white
		//    // since it is scrolled out of view).
		//    ctrl.ScrollSelectionIntoView();

		//    ctrl.ForeColor = (fSelected ?
		//            SystemColors.HighlightText : SystemColors.WindowText);

		//    ctrl.BackColor = (fSelected ?
		//        SystemColors.Highlight : SystemColors.Window);

		//    // Then draw to a bitmap.
		//    Rectangle bounds = GetContentBounds(rowIndex);

		//    ctrl.Height = bounds.Height;
		//    int leftOver = Math.Max(bounds.Height - ctrl.TextHeight, 0);
		//    //Debug.WriteLine(string.Format(
		//    //    "RenderTextAsBitmap {0}/{1}: top: {2}, bottom: {3}, size: {4},{5}",
		//    //    rowIndex, OwningColumn.Index, leftOver / 2, leftOver - (leftOver / 2),
		//    //    bounds.Width, ctrl.TextHeight));

		//    Bitmap bmp = ctrl.CreateBitmapOfText(
		//        new Padding(0, leftOver / 2, 0, leftOver - (leftOver / 2)),
		//        new Rectangle(0, 0, bounds.Width, ctrl.TextHeight));

		//    return bmp;
		//}
	}
}
