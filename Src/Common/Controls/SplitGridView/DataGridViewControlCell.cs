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
// File: DataGridViewControlCell.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.Controls.SplitGridView
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a cell in a DataGridView control that can hold any control.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataGridViewControlCell : DataGridViewCell
	{
		#region Member variables
		private Control m_Control;
		/// <summary>Used to store information necessary to dynamically create the control. This
		/// information is passed to the IControlCreator object.</summary>
		private ControlCreateInfo m_ControlCreateInfo;
		private bool m_fAllowPaint = true;
		#endregion

		#region Constructors and Clone
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DataGridViewControlCell"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DataGridViewControlCell()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DataGridViewControlCell"/> class.
		/// </summary>
		/// <param name="controlInfo">Client provided information necessary to create the
		/// control. This information is passed to IControlCreator when creating the control.</param>
		/// ------------------------------------------------------------------------------------
		public DataGridViewControlCell(ControlCreateInfo controlInfo): this()
		{
			m_ControlCreateInfo = controlInfo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an exact copy of this cell.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Object"/> that represents the cloned
		/// <see cref="T:System.Windows.Forms.DataGridViewCell"></see>.
		/// </returns>
		/// <PermissionSet>
		///		<IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		///		<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		///		<IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/>
		///		<IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		/// ------------------------------------------------------------------------------------
		public override object Clone()
		{
			DataGridViewControlCell dataGridViewCell = base.Clone() as DataGridViewControlCell;
			Debug.Assert(dataGridViewCell != null);
			dataGridViewCell.m_Control = m_Control;
			dataGridViewCell.m_ControlCreateInfo = m_ControlCreateInfo;

			return dataGridViewCell;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a simple rootsite's accessibility object if the control is a simple
		/// rootsite. Otherwise the controls accessibility object is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override AccessibleObject CreateAccessibilityInstance()
		{
			// Using this we get accessibility objects for the draft view/div/div/para/string, but nothing above the draft view.
			//return (m_Control is SimpleRootSite ? null : m_Control.AccessibilityObject);
			// Using this we currently see the DraftView (the simple root site itself) but none of its children or ancestors.
			return m_Control.AccessibilityObject;
			// Using this we don't see any detail below the data grid row.
			//return (m_Control is SimpleRootSite ? base.CreateAccessibilityInstance() : m_Control.AccessibilityObject);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the control hosted by this cell.
		/// </summary>
		/// <value>The control.</value>
		/// ------------------------------------------------------------------------------------
		public Control Control
		{
			get { return m_Control; }
			set
			{
				m_Control = value;
				if (m_Control != null)
				{
					m_Control.Parent = DataGridView;
					m_Control.Dock = DockStyle.None;
					OnColumnWidthChanged(this, new DataGridViewColumnEventArgs(OwningColumn));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the control create info.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ControlCreateInfo ControlCreateInfo
		{
			get { return m_ControlCreateInfo; }
			set { m_ControlCreateInfo = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the <see cref="P:System.Windows.Forms.DataGridViewElement.DataGridView"/>
		/// property of the cell changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnDataGridViewChanged()
		{
			base.OnDataGridViewChanged();

			if (DataGridView != null)
			{
				DataGridView.ColumnWidthChanged += new DataGridViewColumnEventHandler(OnColumnWidthChanged);
				DataGridView.RowHeightChanged += new DataGridViewRowEventHandler(OnRowHeightChanged);
				DataGridView.ColumnStateChanged += new DataGridViewColumnStateChangedEventHandler(OnColumnStateChanged);
				DataGridView.RowStateChanged += new DataGridViewRowStateChangedEventHandler(OnRowStateChanged);
			}

			if (m_Control != null)
				m_Control.Parent = DataGridView;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of the formatted value associated with the cell.
		/// </summary>
		/// <value></value>
		/// <returns>A <see cref="T:System.Type"></see> representing the type of the cell's
		/// formatted value.</returns>
		/// ------------------------------------------------------------------------------------
		public override Type FormattedValueType
		{
			get	{return m_Control.GetType();}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override object GetFormattedValue(object value, int rowIndex, ref DataGridViewCellStyle cellStyle, System.ComponentModel.TypeConverter valueTypeConverter, System.ComponentModel.TypeConverter formattedValueTypeConverter, DataGridViewDataErrorContexts context)
		{
			return m_Control;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Modifies the input cell border style according to the specified criteria.
		/// </summary>
		/// <param name="dataGridViewAdvancedBorderStyleInput">
		///		A <see cref="T:System.Windows.Forms.DataGridViewAdvancedBorderStyle"></see> that
		///		represents the cell border style to modify.
		/// </param>
		/// <param name="dataGridViewAdvancedBorderStylePlaceholder">
		///		A <see cref="T:System.Windows.Forms.DataGridViewAdvancedBorderStyle"></see> that
		///		is used to store intermediate changes to the cell border style.
		/// </param>
		/// <param name="singleVerticalBorderAdded">
		///		true to add a vertical border to the cell; otherwise, false.
		/// </param>
		/// <param name="singleHorizontalBorderAdded">
		///		true to add a horizontal border to the cell; otherwise, false.
		/// </param>
		/// <param name="isFirstDisplayedColumn">
		///		true if the hosting cell is in the first visible column; otherwise, false.
		/// </param>
		/// <param name="isFirstDisplayedRow">
		///		true if the hosting cell is in the first visible row; otherwise, false.
		/// </param>
		/// <returns>
		///	The modified <see cref="T:System.Windows.Forms.DataGridViewAdvancedBorderStyle"/>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override DataGridViewAdvancedBorderStyle AdjustCellBorderStyle(
			DataGridViewAdvancedBorderStyle dataGridViewAdvancedBorderStyleInput,
			DataGridViewAdvancedBorderStyle dataGridViewAdvancedBorderStylePlaceholder,
			bool singleVerticalBorderAdded, bool singleHorizontalBorderAdded,
			bool isFirstDisplayedColumn, bool isFirstDisplayedRow)
		{
			if (((DataGridViewControlRow)OwningRow).AdvancedBorderStyle != null)
			{
				dataGridViewAdvancedBorderStylePlaceholder =
					((DataGridViewControlRow)OwningRow).AdvancedBorderStyle;
				return dataGridViewAdvancedBorderStylePlaceholder;
			}

			return base.AdjustCellBorderStyle(dataGridViewAdvancedBorderStyleInput,
				dataGridViewAdvancedBorderStylePlaceholder, singleVerticalBorderAdded,
				singleHorizontalBorderAdded, isFirstDisplayedColumn, isFirstDisplayedRow);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paints the current <see cref="T:System.Windows.Forms.DataGridViewCell"></see>.
		/// </summary>
		/// <param name="graphics">The <see cref="T:System.Drawing.Graphics"></see> used to
		/// paint the <see cref="T:System.Windows.Forms.DataGridViewCell"></see>.</param>
		/// <param name="clipBounds">A <see cref="T:System.Drawing.Rectangle"></see> that
		/// represents the area of the <see cref="T:System.Windows.Forms.DataGridView"></see>
		/// that needs to be repainted.</param>
		/// <param name="cellBounds">A <see cref="T:System.Drawing.Rectangle"></see> that
		/// contains the bounds of the <see cref="T:System.Windows.Forms.DataGridViewCell"></see>
		/// that is being painted.</param>
		/// <param name="rowIndex">The row index of the cell that is being painted.</param>
		/// <param name="cellState">A bitwise combination of
		/// <see cref="T:System.Windows.Forms.DataGridViewElementStates"></see> values that
		/// specifies the state of the cell.</param>
		/// <param name="value">The data of the <see cref="T:System.Windows.Forms.DataGridViewCell">
		/// </see> that is being painted.</param>
		/// <param name="formattedValue">The formatted data of the
		/// <see cref="T:System.Windows.Forms.DataGridViewCell"></see> that is being painted.
		/// </param>
		/// <param name="errorText">An error message that is associated with the cell.</param>
		/// <param name="cellStyle">A <see cref="T:System.Windows.Forms.DataGridViewCellStyle">
		/// </see> that contains formatting and style information about the cell.</param>
		/// <param name="advancedBorderStyle">A
		/// <see cref="T:System.Windows.Forms.DataGridViewAdvancedBorderStyle"></see> that
		/// contains border styles for the cell that is being painted.</param>
		/// <param name="paintParts">A bitwise combination of the
		/// <see cref="T:System.Windows.Forms.DataGridViewPaintParts"></see> values that
		/// specifies which parts of the cell need to be painted.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Paint(Graphics graphics, Rectangle clipBounds,
			Rectangle cellBounds, int rowIndex, DataGridViewElementStates cellState,
			object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle,
			DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
		{
			if (!m_fAllowPaint)
				return;

			// Clear the background
			using (Region background = new Region(cellBounds))
			{
				background.Xor(GetCellContentDisplayRectangle(advancedBorderStyle));
				background.Intersect(clipBounds);
				graphics.FillRegion(SystemBrushes.Window, background);
			}

			// Invalidate the view so that it redraws
			if (m_Control != null)
			{
				Point pt = m_Control.PointToClient(DataGridView.PointToScreen(new Point(clipBounds.X, clipBounds.Y)));
//				Rectangle borderRect = BorderWidths(advancedBorderStyle);
				Rectangle toDraw = new Rectangle(0, 0,
					cellBounds.Width, cellBounds.Height);
				Rectangle clientClip = new Rectangle(pt.X, pt.Y, clipBounds.Width, clipBounds.Height);
				toDraw.Intersect(clientClip);
				m_Control.Invalidate(toDraw, true);
				m_Control.Update();
			}

			// Finally draw the borders
			if ((paintParts & DataGridViewPaintParts.Border) == DataGridViewPaintParts.Border)
			{
#if !__MonoCS__
				PaintBorder(graphics, clipBounds, cellBounds, cellStyle, advancedBorderStyle);
#else
				// TODO-Linux: Something possibly DataGridView (but not it seems the ViewControl itself) - if drawing border over this border
				// so to work around this problem just draw border 2 pixel's in - not a great solution :)
				// Need to fix The DataGridView not to do this.
				PaintBorder(graphics, clipBounds, cellBounds, cellStyle, advancedBorderStyle);
				if (cellBounds.Width > 2)
					cellBounds.Inflate(-2, -2);
				PaintBorder(graphics, clipBounds, cellBounds, cellStyle, advancedBorderStyle);
#endif
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the bounding rectangle that encloses the cell's content area, which is
		/// calculated using the specified <see cref="T:System.Drawing.Graphics"></see> and
		/// cell style.
		/// </summary>
		/// <param name="graphics">The graphics context for the cell.</param>
		/// <param name="cellStyle">The <see cref="T:System.Windows.Forms.DataGridViewCellStyle">
		/// </see> to be applied to the cell.</param>
		/// <param name="rowIndex">The index of the cell's parent row.</param>
		/// <returns>
		/// The <see cref="T:System.Drawing.Rectangle"></see> that bounds the cell's contents.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected override Rectangle GetContentBounds(Graphics graphics,
			DataGridViewCellStyle cellStyle, int rowIndex)
		{
			Rectangle borderRect = BorderWidths(DataGridView.AdvancedCellBorderStyle);
			Rectangle bounds = new Rectangle(borderRect.Location, Size);
			bounds.Width -= borderRect.Right;
			bounds.Height -= borderRect.Bottom;
			return bounds;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the width of the column changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DataGridViewColumnEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		internal void OnColumnWidthChanged(object sender, DataGridViewColumnEventArgs e)
		{
			if (e.Column != OwningColumn || RowIndex < 0 || ColumnIndex < 0 || m_Control == null)
				return;

			UpdateControlBounds();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the row height changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DataGridViewRowEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		internal void OnRowHeightChanged(object sender, DataGridViewRowEventArgs e)
		{
			if (e.Row != OwningRow || RowIndex < 0 || ColumnIndex < 0 || m_Control == null)
				return;

			UpdateControlBounds();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cell display rectangle for displaying the content, relative to the data
		/// grid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal Rectangle CellContentDisplayRectangle
		{
			get
			{
				DataGridViewAdvancedBorderStyle advancedBorderStyle =
					AdjustCellBorderStyle(DataGridView.AdvancedCellBorderStyle,
						new DataGridViewAdvancedBorderStyle(), false, false, false, false);

				return GetCellContentDisplayRectangle(advancedBorderStyle);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cell content display rectangle.
		/// </summary>
		/// <param name="advancedBorderStyle">The advanced border style.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal Rectangle GetCellContentDisplayRectangle(
			DataGridViewAdvancedBorderStyle advancedBorderStyle)
		{
			Rectangle rect = DataGridView.GetCellDisplayRectangle(ColumnIndex, RowIndex,
				true);
			Rectangle borderRect = BorderWidths(advancedBorderStyle);
			rect.Inflate(-borderRect.Width, -borderRect.Height);
			rect = new Rectangle(rect.Left + Style.Padding.Left,
				rect.Top + Style.Padding.Top, rect.Width - Style.Padding.Horizontal,
				rect.Height - Style.Padding.Vertical);
			return rect;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the focus moves to a cell.
		/// </summary>
		/// <param name="rowIndex">The index of the cell's parent row.</param>
		/// <param name="throughMouseClick">true if a user action moved focus to the cell;
		/// false if a programmatic operation moved focus to the cell.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnEnter(int rowIndex, bool throughMouseClick)
		{
			base.OnEnter(rowIndex, throughMouseClick);
			if (Control != null)
				Control.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the visible property of the column changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnColumnStateChanged(object sender, DataGridViewColumnStateChangedEventArgs e)
		{
			if (e.Column != OwningColumn || e.StateChanged != DataGridViewElementStates.Visible)
				return;

			ControlVisible = OwningRow.Index >= 0 ? OwningColumn.Visible && OwningRow.Visible : false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the visible property of the row changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected void OnRowStateChanged(object sender, DataGridViewRowStateChangedEventArgs e)
		{
			if (e.Row != OwningRow || e.StateChanged != DataGridViewElementStates.Visible)
				return;

			ControlVisible = OwningColumn.Index >= 0 ? OwningColumn.Visible && OwningRow.Visible : false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether the control is visible.
		/// </summary>
		/// <value><c>true</c> if the control is visible; otherwise, <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		protected bool ControlVisible
		{
			get { return Control.Visible; }
			set
			{
				if (Control != null)
				{
					Control.Visible = value;
					if (value)
					{
						m_fAllowPaint = false;
						try
						{
							if (Control.Height == 0 || Control.Width == 0)
								OnRowHeightChanged(this, new DataGridViewRowEventArgs(OwningRow));
							Control.Invalidate(true);
							Control.Update();
						}
						finally
						{
							m_fAllowPaint = true;
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the bounds of the hosted control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateControlBounds()
		{
			if (m_Control != null)
			{
				Rectangle rect = CellContentDisplayRectangle;
				m_Control.Bounds = new Rectangle(rect.Location, new Size(rect.Width, rect.Height));
			}
		}
	}
}
