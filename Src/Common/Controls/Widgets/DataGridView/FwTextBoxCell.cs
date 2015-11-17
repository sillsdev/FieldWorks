// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwTextBoxCell.cs
// Responsibility: TE Team

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Displays editable text information from a FwTextBox in a DataGridView control.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwTextBoxCell : DataGridViewTextBoxCell
	{
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
#if !__MonoCS__
			// FWNX-472 on mono calling base.InitalizeEditingControl reverts DataGridView.EditingControl
			// to a DataGridViewTextBoxEditingControl.
			base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
#endif
			FwTextBoxControl ctrl = DataGridView.EditingControl as FwTextBoxControl;
			InitializeTextBoxControl(ctrl, rowIndex);
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
			ITsString tssValue = value as ITsString;
			return base.GetFormattedValue(tssValue != null ? tssValue.Text : string.Empty,
				rowIndex, ref cellStyle, valueTypeConverter, formattedValueTypeConverter, context);
		}

#if __MonoCS__
		/// <summary>
		/// Paint this DataGridView cell.  Overridden to allow RightToLeft display if needed.
		/// </summary>
		protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds,
			int rowIndex, DataGridViewElementStates cellState, object val, object formattedValue,
			string errorText, DataGridViewCellStyle cellStyle,
			DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
		{
			FwTextBoxColumn col = OwningColumn as FwTextBoxColumn;
			if (formattedValue == null || col == null)
			{
				base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, val, formattedValue, errorText, cellStyle,
					advancedBorderStyle, paintParts);
				return;
			}
			// Prepaint
			DataGridViewPaintParts pre = DataGridViewPaintParts.Background | DataGridViewPaintParts.SelectionBackground;
			pre = pre & paintParts;
			base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, val, formattedValue, errorText, cellStyle, advancedBorderStyle, pre);

			// Paint content
			if (!IsInEditMode && (paintParts & DataGridViewPaintParts.ContentForeground) == DataGridViewPaintParts.ContentForeground)
			{
				DrawFwText(graphics, cellBounds, formattedValue.ToString(), cellStyle, col);
			}

			// Postpaint
			DataGridViewPaintParts post = DataGridViewPaintParts.Border | DataGridViewPaintParts.Focus | DataGridViewPaintParts.ErrorIcon;
			post = post & paintParts;
			base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, val, formattedValue, errorText, cellStyle, advancedBorderStyle, post);
		}

		/// <summary>
		/// Draw the text using the FieldWorks IVwGraphicsWin32 interface.
		/// </summary>
		private void DrawFwText(Graphics graphics, Rectangle cellBounds,
			string text, DataGridViewCellStyle cellStyle, FwTextBoxColumn col)
		{
			if (String.IsNullOrEmpty(text))
				return;
			IntPtr hdc = graphics.GetHdc();
			IVwGraphicsWin32 vg = VwGraphicsWin32Class.Create();	// actually VwGraphicsCairo
			try
			{
				vg.Initialize(hdc);
				var renderProps = GetRenderProps(cellStyle, col);
				vg.SetupGraphics(ref renderProps);
				int x;
				int y;
				GetLocationForText(cellBounds, (col.TextBoxControl.RightToLeft == RightToLeft.Yes),
					vg, text, out x, out y);
				vg.PushClipRect(cellBounds);
				vg.DrawText(x, y, text.Length, text, 0);
				vg.PopClipRect();
			}
			finally
			{
				vg.ReleaseDC();
				graphics.ReleaseHdc(hdc);
			}
		}

		/// <summary>
		/// Get the starting location for drawing the text.
		/// </summary>
		private void GetLocationForText(Rectangle cellBounds, bool fRightToLeft,
			IVwGraphicsWin32 vg, string text, out int x, out int y)
		{
			var contentBounds = cellBounds;
			contentBounds.Height -= 2;
			contentBounds.Width -= 2;
			int dx0;
			int dy0;
			vg.GetTextExtent(text.Length, text, out dx0, out dy0);
			if (fRightToLeft)
			{
				x = contentBounds.Right - (dx0 + 4);
			}
			else
			{
				x = contentBounds.Left + 4;
			}
			int dy = (contentBounds.Height - dy0) / 2;
			if (dy > 0)
				y = contentBounds.Top + dy;
			else
				y = contentBounds.Top;
		}

		/// <summary>
		/// Derive the LgCharRenderProps from the DataGridViewCellStyle and FwTextBoxColumn.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "font is a reference")]
		private LgCharRenderProps GetRenderProps(DataGridViewCellStyle cellStyle, FwTextBoxColumn col)
		{
			var renderProps = new LgCharRenderProps();
			renderProps.szFaceName = new ushort[32];	// arrays should be created in constructor, but struct doesn't have one.
			renderProps.szFontVar = new ushort[64];
			var foreColor = Selected ? cellStyle.SelectionForeColor : cellStyle.ForeColor;
			renderProps.clrFore = (uint)foreColor.ToArgb();
			// The background behind the characters must be transparent for the correct
			// background color to show (at least for Selected).
			const uint transparent = 0xC0000000; // FwTextColor.kclrTransparent won't convert to uint
			renderProps.clrBack = transparent;
			renderProps.ws = col.TextBoxControl.WritingSystemCode;
			renderProps.fWsRtl = (byte)(col.TextBoxControl.RightToLeft == RightToLeft.Yes ? 1 : 0);
			var font = cellStyle.Font;
			renderProps.dympHeight = (int)(font.SizeInPoints * 1000.0);	// size in millipoints
			int lim = Math.Min(renderProps.szFaceName.Length, font.Name.Length);
			for (var i = 0; i < lim; ++i)
				renderProps.szFaceName[i] = (ushort)font.Name[i];
			// The rest of these values are set to default values.
			renderProps.clrUnder = 0xFFFFFF;
			renderProps.nDirDepth = 0;
			renderProps.ssv = 0;
			renderProps.unt = 0;
			renderProps.ttvBold = 0;
			renderProps.ttvItalic = 0;
			return renderProps;
		}
#endif
	}
}
