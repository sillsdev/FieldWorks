// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: KeyTermsRenderingStatusCell.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A specialized ImageCell that deals with zooming the image
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CheckGridStatusCell: DataGridViewImageCell
	{
		private static Dictionary<Color, SolidBrush> m_Brushes = new Dictionary<Color, SolidBrush>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CheckGridStatusCell"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckGridStatusCell()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CheckGridStatusCell"/> class.
		/// </summary>
		/// <param name="valueIsIcon">The cell will display an
		/// <see cref="T:System.Drawing.Icon"/> value.</param>
		/// ------------------------------------------------------------------------------------
		public CheckGridStatusCell(bool valueIsIcon)
			: base(valueIsIcon)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="graphics">The <see cref="T:System.Drawing.Graphics"/> used to paint
		/// the <see cref="T:System.Windows.Forms.DataGridViewCell"/>.</param>
		/// <param name="clipBounds">A <see cref="T:System.Drawing.Rectangle"/> that represents
		/// the area of the <see cref="T:System.Windows.Forms.DataGridView"/> that needs to be
		/// repainted.</param>
		/// <param name="cellBounds">A <see cref="T:System.Drawing.Rectangle"/> that contains
		/// the bounds of the <see cref="T:System.Windows.Forms.DataGridViewCell"/> that is
		/// being painted.</param>
		/// <param name="rowIndex">The row index of the cell that is being painted.</param>
		/// <param name="elementState"></param>
		/// <param name="value">The data of the <see cref="T:System.Windows.Forms.DataGridViewCell"/>
		/// that is being painted.</param>
		/// <param name="formattedValue">The formatted data of the
		/// <see cref="T:System.Windows.Forms.DataGridViewCell"/> that is being painted.</param>
		/// <param name="errorText">An error message that is associated with the cell.</param>
		/// <param name="cellStyle">A <see cref="T:System.Windows.Forms.DataGridViewCellStyle"/>
		/// that contains formatting and style information about the cell.</param>
		/// <param name="advancedBorderStyle">A
		/// <see cref="T:System.Windows.Forms.DataGridViewAdvancedBorderStyle"/> that contains
		/// border styles for the cell that is being painted.</param>
		/// <param name="paintParts">A bitwise combination of the
		/// <see cref="T:System.Windows.Forms.DataGridViewPaintParts"/> values that specifies
		/// which parts of the cell need to be painted.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Paint(Graphics graphics, Rectangle clipBounds,
			Rectangle cellBounds, int rowIndex, DataGridViewElementStates elementState,
			object value, object formattedValue, string errorText,
			DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle,
			DataGridViewPaintParts paintParts)
		{
			Bitmap bitmap = formattedValue as Bitmap;
			if (bitmap == null || !(OwningColumn is CheckGridStatusColumn))
			{
				// We don't know enough to paint this cell, so we let the base class handle it.
				base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState, value,
					formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
				return;
			}

			// Let base class paint everything except content
			base.Paint(graphics, clipBounds, cellBounds, rowIndex, elementState, value,
				formattedValue, errorText, cellStyle, advancedBorderStyle,
				paintParts & ~(DataGridViewPaintParts.ContentForeground | DataGridViewPaintParts.Background));

			Rectangle cellValueBounds = cellBounds;
			Rectangle borderRect = BorderWidths(advancedBorderStyle);
			cellValueBounds.Offset(borderRect.X, borderRect.Y);
			cellValueBounds.Width -= borderRect.Right;
			cellValueBounds.Height -= borderRect.Bottom;
			if (cellValueBounds.Width <= 0 || cellValueBounds.Height <= 0)
				return;

			if ((paintParts & DataGridViewPaintParts.Background) != 0)
			{
				bool fSelected = (elementState & DataGridViewElementStates.Selected) != 0;
				Color color = (fSelected & (paintParts & DataGridViewPaintParts.SelectionBackground) != 0) ?
					cellStyle.SelectionBackColor : cellStyle.BackColor;
				graphics.FillRectangle(GetCachedBrush(color), cellValueBounds);
			}

			if ((paintParts & DataGridViewPaintParts.ContentForeground) != 0)
			{
				// scale the image according to zoom factor
				float zoomFactor = ((CheckGridStatusColumn)OwningColumn).ZoomFactor;

				int zoomedImageWidth = (int)(bitmap.Width * zoomFactor);
				int zoomedImageHeight = (int)(bitmap.Height * zoomFactor);
				Rectangle drawRect = new Rectangle(
					cellValueBounds.X + (cellValueBounds.Width - zoomedImageWidth) / 2,
					cellValueBounds.Y + (cellValueBounds.Height - zoomedImageHeight) / 2,
					zoomedImageWidth, zoomedImageHeight);

				Region clip = graphics.Clip;
				graphics.SetClip(Rectangle.Intersect(Rectangle.Intersect(drawRect, cellValueBounds),
					Rectangle.Truncate(graphics.VisibleClipBounds)));
				graphics.DrawImage(bitmap, drawRect);
				graphics.Clip = clip;

				ICornerGlyphGrid owningGrid = DataGridView as ICornerGlyphGrid;
				if (owningGrid != null && owningGrid.ShouldDrawCornerGlyph(ColumnIndex, rowIndex))
					DrawCornerGlyph(graphics, rowIndex, cellBounds);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When painting a status column cell for rows that are ignored and have an associated
		/// annotation, then paint a red triangle in the cell's corner.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawCornerGlyph(Graphics g, int iRow, Rectangle rc)
		{
			// First, using the grid's default background color (i.e. the non selected
			// row background color), draw a triangle whose hypothenuse is slightly
			// longer than that of the red triangle's. This will cause the triangle to
			// be more visible if this cell is in a selected row.
			Point pt1 = new Point(rc.Right - 8, rc.Y);
			Point pt2 = new Point(rc.Right - 1, rc.Y + 7);
			Point ptCorner = new Point(rc.Right - 1, rc.Top);
			using (SolidBrush br = new SolidBrush(DataGridView.DefaultCellStyle.BackColor))
				g.FillPolygon(br, new Point[] { pt1, pt2, ptCorner });

			// Now shrink the triangle region slightly and paint a gradient red.
			pt1 = new Point(rc.Right - 7, rc.Y);
			pt2 = new Point(rc.Right - 1, rc.Y + 6);
			using (LinearGradientBrush br =
				new LinearGradientBrush(pt1, pt2, Color.Red, Color.DarkRed))
			{
				g.FillPolygon(br, new Point[] { pt1, pt2, ptCorner });
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a cached brush.
		/// </summary>
		/// <param name="color">The color.</param>
		/// <returns>The brush</returns>
		/// ------------------------------------------------------------------------------------
		internal SolidBrush GetCachedBrush(Color color)
		{
			SolidBrush brush;
			if (!m_Brushes.TryGetValue(color, out brush))
			{
				brush = new SolidBrush(color);
				m_Brushes.Add(color, brush);
			}
			return brush;
		}
	}
}
