// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: OptionListBox.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Windows.Forms;
using System.Drawing;

using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for OptionListBox.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class OptionListBox : ListBox, IFWDisposable
	{
		private Bitmap m_imgSelected;
		private Bitmap m_imgUnselected;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="OptionListBox"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public OptionListBox()
		{
			CheckDisposed();

			DrawMode = DrawMode.OwnerDrawVariable;
			Sorted = false;

			m_imgSelected = new Bitmap(GetType(), "resources.SelectedOption.bmp");
			m_imgUnselected = new Bitmap(GetType(), "resources.UnSelectedOption.bmp");
			m_imgSelected.MakeTransparent(Color.Magenta);
			m_imgUnselected.MakeTransparent(Color.Magenta);
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

		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				if (m_imgSelected != null)
					m_imgSelected.Dispose();
				if (m_imgUnselected != null)
					m_imgUnselected.Dispose();
			}
			m_imgSelected = null;
			m_imgUnselected = null;

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnMeasureItem(MeasureItemEventArgs e)
		{
			base.OnMeasureItem(e);
			e.ItemHeight += 4;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnDrawItem(DrawItemEventArgs e)
		{
			bool selected = ((e.State & DrawItemState.Selected) != 0);

			e.Graphics.FillRectangle(SystemBrushes.Window, e.Bounds);

			if (Items.Count > 0 && e.Index >= 0)
			{
				// Fill the background
				Rectangle rc = e.Bounds;
				rc.X = 17;
				rc.Width = (int)e.Graphics.MeasureString(Items[e.Index].ToString(),
					SystemInformation.MenuFont).Width + 2;
				e.Graphics.FillRectangle((selected ?
					SystemBrushes.Highlight : SystemBrushes.Window), rc);

				if (selected)
				{
					ControlPaint.DrawFocusRectangle(e.Graphics, rc,
						SystemColors.HighlightText, SystemColors.Highlight);
				}

				// Draw the radio button.
				rc = new Rectangle(2, e.Bounds.Top, 12, 12);
				rc.Y += ((e.Bounds.Height / 2) - 6);
				e.Graphics.DrawImage((selected ? m_imgSelected : m_imgUnselected), rc);

				// Draw the text.
				StringFormat sf = (StringFormat)StringFormat.GenericTypographic.Clone();
				sf.LineAlignment = StringAlignment.Center;
				sf.Alignment = StringAlignment.Near;

				if (RightToLeft == RightToLeft.Yes)
					sf.FormatFlags = StringFormatFlags.DirectionRightToLeft;

				rc = e.Bounds;
				rc.X += 20;
				rc.Width -= 20;
				e.Graphics.DrawString(Items[e.Index].ToString(), SystemInformation.MenuFont,
					(selected ? SystemBrushes.HighlightText : SystemBrushes.WindowText), rc, sf);
			}

			base.OnDrawItem(e);
		}
	}
}
