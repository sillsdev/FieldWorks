// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwListView.cs
// Responsibility: DavidO
// Last reviewed:
//
// <remarks>
// This subclass of the ListView control allows items and subitems to be custom drawn.
// It also provides event trapping for column resizing.
// REVIEW DavidO: Currently, this doesn't support the LargeIcon view. Will there ever be a
// need to custom draw items in LargeIcon view. If not, then the standard ListView control
// will suffice.
// </remarks>

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
using System.Resources;
using System.Drawing.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	///--------------------------------------------------------------------------------
	/// <summary>
	/// FwListView is a customized list view control that adds some additional events
	/// and a property. The events allow the user of the control to have control of
	/// the drawing of items and subitems of the listview.
	/// </summary>
	///--------------------------------------------------------------------------------
	[Serializable]
	public class FwListView : ListView, IFWDisposable
	{
		/// <summary>
		/// This set is used to remember which rows have had forced repaints the first time the
		/// mouse moves over them, compensating for an extra DrawItem event sent by the wrapped
		/// Win32 control.
		/// </summary>
		private Set<ListViewItem> m_invalidatedItemList = new Set<ListViewItem>();

		///--------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for customized ListView control.
		/// </summary>
		///--------------------------------------------------------------------------------
		public FwListView()
		{
			DoubleBuffered = true;
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		//*******************************************************************************************
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		//*******************************************************************************************
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_invalidatedItemList != null)
					m_invalidatedItemList.Clear();
			}
			m_invalidatedItemList = null;

			base.Dispose( disposing );
		}

		/////--------------------------------------------------------------------------------
		///// <summary>
		///// Erases (i.e. fills with the list view control's background color) the
		///// current item, including the checkbox and image areas.
		///// </summary>
		/////--------------------------------------------------------------------------------
		//public void DefaultItemErase()
		//{
		//    // Can't erase a subitem or do anything without a graphics object.
		//    if (m_drawingCustomSubItem || m_graphics == null)
		//        return;

		//    m_graphics.FillRectangle(new SolidBrush(BackColor),
		//        GetItemRect(m_row, ItemBoundsPortion.ItemOnly));
		//}

		/////--------------------------------------------------------------------------------
		///// <summary>
		///// Erases (i.e. fills with the list view control's background color) the
		///// current sub item, including the checkbox and image areas.
		///// </summary>
		/////--------------------------------------------------------------------------------
		//public void DefaultSubItemErase()
		//{
		//    // Can't erase a non subitem or do anything without a graphics object.
		//    if (!m_drawingCustomSubItem || m_graphics == null)
		//        return;

		//    m_graphics.FillRectangle(new SolidBrush(BackColor), m_rcText);
		//}

		/////--------------------------------------------------------------------------------
		///// <summary>
		///// Fills the current list view item's (or subitem's) cell with the default color.
		///// The default color is determined by the following: 1) is the current item
		///// selected, 2) does the list view have focus, 3) are the FullRowSelect and
		///// HideSelection properties set to true, and 4) is the current item a subitem.
		///// This method should only be called from delegates of the FwDrawItem or
		///// FwDrawSubItem method. If this is called from somewhere else, then this method
		///// is ignored.
		///// </summary>
		///// <param name="drawFocusRect">A flag indicating whether or not to draw a focus
		///// rectangle around the item after drawing it's background. This is ignored when
		///// HideSelection is true and the list view control doesn't have focus.</param>
		///// <returns>The default color the caller should use for text drawn over the
		///// filled rectagle.</returns>
		/////--------------------------------------------------------------------------------
		//public Color DrawDefaultItemBackground(bool drawFocusRect)
		//{
		//    return DrawDefaultItemBackground((m_drawingCustomSubItem ?
		//        Items[m_row].SubItems[m_col].Text : Items[m_row].Text),
		//        Font, null, drawFocusRect);
		//}

		/////--------------------------------------------------------------------------------
		///// <summary>
		///// Fills the current list view item's (or subitem's) cell with the default color.
		///// The default color is determined by the following: 1) is the current item
		///// selected, 2) does the list view have focus, 3) are the FullRowSelect and
		///// HideSelection properties set to true, and 4) is the current item a subitem.
		///// This method should only be called from delegates of the FwDrawItem or
		///// FwDrawSubItem method. If this is called from somewhere else, then this method
		///// is ignored.
		///// </summary>
		///// <param name="textToMeasure">The string that is measured when determining how
		///// wide of a rectangle to paint when painting selected items. This is ignored in
		///// the following conditions: 1) HideSelection is true and the list view control
		///// doesn't have focus, or 2) FullRowSelect is true, or 3) FullRowSelect is true
		///// and the item being drawn is not a subitem, or 4) the current item being drawn
		///// isn't selected.</param>
		///// <param name="drawFocusRect">A flag indicating whether or not to draw a focus
		///// rectangle around the item after drawing it's background. This is ignored when
		///// HideSelection is true and the list view control doesn't have focus.</param>
		///// <returns>The default color the caller should use for text drawn over the
		///// filled rectagle.</returns>
		/////--------------------------------------------------------------------------------
		//public Color DrawDefaultBackground(string textToMeasure, bool drawFocusRect)
		//{
		//    return DrawDefaultItemBackground(textToMeasure, Font,
		//        null, drawFocusRect);
		//}

		/////--------------------------------------------------------------------------------
		///// <summary>
		///// Fills the current list view item's (or subitem's) cell with the default color.
		///// The default color is determined by the following: 1) is the current item
		///// selected, 2) does the list view have focus, 3) are the FullRowSelect and
		///// HideSelection properties set to true, and 4) is the current item a subitem.
		///// This method should only be called from delegates of the FwDrawItem or
		///// FwDrawSubItem method. If this is called from somewhere else, then this method
		///// is ignored.
		///// </summary>
		///// <param name="sf">The string format used to measure how wide a string is when
		///// drawing the background for selected items. This is ignored in the following
		///// conditions: 1) HideSelection is true and the list view control doesn't have
		///// focus, or 2) FullRowSelect is true, or 3) FullRowSelect is true and the item
		///// being drawn is not a subitem, or 4) the current item being drawn isn't selected.
		///// </param>
		///// <param name="drawFocusRect">A flag indicating whether or not to draw a focus
		///// rectangle around the item after drawing it's background. This is ignored when
		///// HideSelection is true and the list view control doesn't have focus.</param>
		///// <returns>The default color the caller should use for text drawn over the
		///// filled rectagle.</returns>
		/////--------------------------------------------------------------------------------
		//public Color DrawDefaultItemBackground(StringFormat sf, bool drawFocusRect)
		//{
		//    return DrawDefaultItemBackground(Items[m_row].SubItems[m_col].Text,
		//        Font, sf, drawFocusRect);
		//}

		/////--------------------------------------------------------------------------------
		///// <summary>
		///// Fills the current list view item's (or subitem's) cell with the default color.
		///// The default color is determined by the following: 1) is the current item
		///// selected, 2) does the list view have focus, 3) are the FullRowSelect and
		///// HideSelection properties set to true, and 4) is the current item a subitem.
		///// This method should only be called from delegates of the FwDrawItem or
		///// FwDrawSubItem method. If this is called from somewhere else, then this method
		///// is ignored.
		///// </summary>
		///// <param name="textToMeasure">The string that is measured when determining how
		///// wide of a rectangle to paint when painting selected items. See the
		///// StringFormat argument description to see under what conditions this argument
		///// is ignored.</param>
		///// <param name="fnt">A font used to measure how wide a string is when drawing the
		///// background for selected items. See the StringFormat argument description to see
		///// under what conditions this argument is ignored.</param>
		///// <param name="sf">The string format used to measure how wide a string is when
		///// drawing the background for selected items. This is ignored in the following
		///// conditions: 1) HideSelection is true and the list view control doesn't have
		///// focus, or 2) FullRowSelect is true, or 3) FullRowSelect is true and the item
		///// being drawn is not a subitem, or 4) the current item being drawn isn't selected.
		///// </param>
		///// <param name="drawFocusRect">A flag indicating whether or not to draw a focus
		///// rectangle around the item after drawing it's background. This is ignored when
		///// HideSelection is true and the list view control doesn't have focus.</param>
		///// <returns>The default color the caller should use for text drawn over the
		///// filled rectagle.</returns>
		/////--------------------------------------------------------------------------------
		//public Color DrawDefaultItemBackground(string textToMeasure, Font fnt,
		//    StringFormat sf, bool drawFocusRect)
		//{
		//    // If there is no usable graphics object, then get out of here.
		//    if (m_graphics == null)
		//        return Color.Empty;

		//    // First, erase the cell.
		//    if (m_drawingCustomSubItem)
		//        DefaultSubItemErase();
		//    else
		//        DefaultItemErase();

		//    // We're done if: 1) the current row is not selected or 2) the current row is
		//    // selected but the list view doesn't have focus and HideSelection is true,
		//    // or 3) the current item being drawn is a subitem and the FullRowSelect
		//    // property is false.
		//    if ((!Items[m_row].Selected) ||
		//        (!Focused && Items[m_row].Selected && HideSelection) ||
		//        (m_drawingCustomSubItem && !FullRowSelect))
		//    {
		//        return ForeColor;
		//    }

		//    // At this point we know we're going to fill the item with the highlight color.
		//    Rectangle rcFillRect;

		//    // If the FullRowSelect property is true or we're filling the
		//    // rectangle occupied by a subitem, then just fill the entire text rectangle.
		//    // Otherwise, calculate how wide the text is and only fill a rectangle that's
		//    // a little wider than the text.
		//    if (FullRowSelect || m_drawingCustomSubItem)
		//        rcFillRect = m_rcText;
		//    else
		//    {
		//        int maxWidth = m_rcText.Width;

		//        SizeF sz = (sf != null ?
		//            m_graphics.MeasureString(textToMeasure, fnt, maxWidth, sf) :
		//            m_graphics.MeasureString(textToMeasure, fnt, maxWidth));

		//        rcFillRect = new Rectangle(m_rcText.Left, m_rcText.Top,
		//            (int)sz.Width + 1, m_rcText.Height);

		//        m_rcFocusRect = rcFillRect;
		//    }

		//    // At this point, we know we have to fill the item with a selected color
		//    // indicative of whether or not the list view control has focus.
		//    m_graphics.FillRectangle(new SolidBrush(
		//        (Focused ? SystemColors.Highlight : SystemColors.Control)),	rcFillRect);

		//    // If the list view has focus then draw a focus rectangle if the caller has
		//    // specified to do so.
		//    if (Focused && drawFocusRect)
		//        DrawFocusRect(rcFillRect);

		//    // Return a foreground color indicative of whether or not the list view
		//    // control has focus.
		//    return (Focused ? SystemColors.HighlightText : SystemColors.WindowText);
		//}



		///--------------------------------------------------------------------------------
		/// <summary>
		/// If there is any custom drawing, then make sure the custom drawing events
		/// get fired so the delegates have a chance to paint things before the control
		/// looses focus. Only selected items will be redrawn.
		/// </summary>
		///--------------------------------------------------------------------------------
		protected override void OnLeave(System.EventArgs e)
		{
			if (OwnerDraw)
			{
				// Only invalidate the selected items.
				foreach (ListViewItem lvi in SelectedItems)
				{
					Invalidate(GetItemRect(lvi.Index, (FullRowSelect ?
						ItemBoundsPortion.Entire : ItemBoundsPortion.Label)));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepares to modify item.
		/// This removes the selected mapping from the invalidated item list because it
		/// has already been moused over once and was addressed in lvMappings_MouseMove().
		/// </summary>
		/// <param name="item">The item about to be modified</param>
		/// ------------------------------------------------------------------------------------
		public void PrepareToModifyItem(ListViewItem item)
		{
			CheckDisposed();

			if (item != null)
				m_invalidatedItemList.Remove(item);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the color to use for drawing the text of a subitem.
		/// </summary>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DrawListViewSubItemEventArgs"/>
		/// instance containing the event data.</param>
		/// <returns>The color to use</returns>
		/// ------------------------------------------------------------------------------------
		public Color GetTextColor(DrawListViewSubItemEventArgs e)
		{
			CheckDisposed();

			// If there is no usable graphics object, then get out of here.
			if (e.Graphics == null)
				return Color.Empty;

			// Per LT-4930, we do NOT want to switch back to the normal foreground color when
			// the control does not have focus.
			return e.Item.Selected ? SystemColors.HighlightText : ForeColor;
			//return e.Item.Selected ? (Focused ? SystemColors.HighlightText : ForeColor) : ForeColor;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the DrawColumnHeader event by doing the default drawing
		/// </summary>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DrawListViewColumnHeaderEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
		{
			e.DrawDefault = true;
			base.OnDrawColumnHeader(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the MouseMove event. Forces each row to repaint itself the first
		/// time the mouse moves over it, compensating for an extra DrawItem event
		/// sent by the wrapped Win32 control.
		/// </summary>
		/// <param name="e">The <see cref="T:System.Windows.Forms.MouseEventArgs"/> instance
		/// containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			ListViewItem item = GetItemAt(e.X, e.Y);
			if (item != null && !m_invalidatedItemList.Contains(item))
			{
				Invalidate(item.Bounds);
				m_invalidatedItemList.Add(item);
			}
			base.OnMouseMove(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws an item.
		/// </summary>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DrawListViewItemEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnDrawItem(DrawListViewItemEventArgs e)
		{
			// Draw the item's background fill.
			Color backColor = e.Item.Selected ? (Focused ? SystemColors.Highlight : SystemColors.GrayText) : BackColor;
			using (SolidBrush brush = new SolidBrush(backColor))
				e.Graphics.FillRectangle(brush, e.Bounds);

			if (e.Item.Focused)
				ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds);

			base.OnDrawItem(e);
		}
	}
}
