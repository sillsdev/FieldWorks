// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: SideBarTabPanel.cs
// Responsibility: EberhardB
// Last reviewed:
//
// <remarks>
// Implementation of SideBarTabPanel, a specialized panel which handles scrolling without
// scrollbars
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Reflection;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Specialized panel that handles scrolling without showing scrollbars.
	/// Scrolling is managed from outside (i.e. buttons for scrolling are in the parent)
	/// </summary>
	internal class SideBarTabPanel : ScrollableControl, IFWDisposable
	{
		#region Variables
		/// The buttons used as scroll buttonsf
		protected Button m_btnUp = null;
		protected Button m_btnDown = null;

		private int m_maxHeight;
		#endregion

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

		#region Overriden methods
		/// <summary>
		/// Overriden, so that we never ever show scroll bars
		/// </summary>
		protected override void AdjustFormScrollbars(bool displayScrollbars)
		{
			base.AdjustFormScrollbars(false);
		}

		/// <summary>
		/// Recalculate the display rect
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLayout(LayoutEventArgs e)
		{
			base.OnLayout(e);

			// we have to manually set the DisplayRectangle, because this is set to the client
			// window size if we don't show scrollbars (i.e. call AdjustFormScrollbars(false)).
			if (Controls.Count > 0)
			{
				Rectangle rc = DisplayRectangleInternal;
				DisplayRectangleInternal = new Rectangle(0, 0, this.Width,
					Controls.Count * (Controls[0].Height + Padding.Vertical) + Padding.Top);

				SetDisplayRectLocation(rc.Left, rc.Top);
			}
		}

		/// <summary>
		/// Show or hide the scroll buttons
		/// </summary>
		/// <param name="e"></param>
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			// Show or hide scroll buttons
			if (Visible)
				ShowButtons();
		}
		#endregion

		#region Methods
		/// <summary>
		/// Calculate the internal DisplayRectangle
		/// </summary>
		internal void CalculateDispRect()
		{
			CheckDisposed();

			// set the height nearly to 0...
			DisplayRectangleInternal = new Rectangle(0, 0, this.Width, Padding.Vertical);
		}

		/// <summary>
		/// Set the buttons that will be used as scroll buttons for scrolling up and down
		/// </summary>
		/// <param name="btnUp">Scroll up button</param>
		/// <param name="btnDown">Scroll down button</param>
		internal void SetScrollButtons(Button btnUp, Button btnDown)
		{
			CheckDisposed();

			m_btnUp = btnUp;
			m_btnDown = btnDown;
		}

		/// <summary>
		/// Add the new button to our Controls collection and adjust our window height
		/// </summary>
		/// <param name="btn">Button to add</param>
		internal void AddButton(SideBarButton btn)
		{
			CheckDisposed();

			if (btn == null)
				return;

			Controls.Add(btn);

			ShowButtons();
		}

		/// <summary>
		/// Remove the button from the Controls collection and adjust window
		/// </summary>
		/// <param name="btn">Button to remove</param>
		internal void RemoveButton(SideBarButton btn)
		{
			CheckDisposed();

			if (btn == null)
				return;

			Controls.Remove(btn);

			ShowButtons();
		}

		/// <summary>
		/// Remove all sidebar buttons
		/// </summary>
		internal void ClearButtons()
		{
			CheckDisposed();

			// need 2 steps, because removing from a collection changes the
			// collection and therefore doesn't work well with foreach
			List<Control> toRemove = new List<Control>();
			foreach(Control control in Controls)
			{
				if (control is SideBarButton)
					toRemove.Add(control);
			}

			foreach (Control control in toRemove)
				Controls.Remove(control);

			ShowButtons();
		}

		/// <summary>
		/// Show or hide the scroll buttons
		/// </summary>
		internal void ShowButtons()
		{
			CheckDisposed();

			if (m_btnDown == null || m_btnUp == null)
				return;

			SuspendLayout();

			// remember: AutoScrollPosition has negative values!

			// calculate the height including the scroll buttons
			int nNewHeight = MaxHeight;
			int nHeight = Height;
			if (m_btnUp.Visible)
				nHeight += m_btnUp.Height;
			if (m_btnDown.Visible)
				nHeight += m_btnDown.Height;

			// first test if all buttons can be shown at once - if so scroll to 0 position!
			if (nHeight >= DisplayRectangleInternal.Height)
				AutoScrollPosition = new Point(AutoScrollPosition.X, 0);

			bool fShowScrollUp = (AutoScrollPosition.Y < 0);

			if (m_btnUp.Visible != fShowScrollUp)
			{
				m_btnUp.Visible = fShowScrollUp;
			}

			bool fShowScrollDown = (-AutoScrollPosition.Y + nHeight < DisplayRectangleInternal.Height)
				&& (-AutoScrollPosition.Y + nHeight > 0);

			if (m_btnDown.Visible != fShowScrollDown)
			{
				m_btnDown.Visible = fShowScrollDown;
			}

//			System.Console.WriteLine("ShowButtons ({3}): nHeight={1}, DisplayHeight={2}," +
//				"Height={4}, btnUp.Visible={5}, btnDown.Visible={6}, nNewHeight={0}", nNewHeight, nHeight, DisplayRectangleInternal.Height,
//				((SideBarTab)Parent).Title, Height, fShowScrollUp, fShowScrollDown);

			// Calculate our new height and the positioin of the scroll buttons. Although Dock
			// property is set on us and on scroll buttons, it doesn't seem to work correct
			// in all cases, so we have to do it ourself.
			if (fShowScrollUp)
				nNewHeight -= m_btnUp.Height;

			if (fShowScrollDown)
				nNewHeight -= m_btnDown.Height;

			if (nNewHeight != Height)
			{
				if (fShowScrollUp)
					Top = m_btnUp.Bottom;
				else
					Top = m_btnUp.Top;

				Height = nNewHeight;
				m_btnDown.Top = Bottom;
			}
			ResumeLayout(true);
		}
		#endregion

		#region Properties

		/// <summary>
		/// Get or set the internal used DisplayRectangle
		/// </summary>
		[Browsable(false)]
		protected Rectangle DisplayRectangleInternal
		{
			get
			{
				// REVIEW EberhardB: Calling GetDisplayRectInternal via Reflection needs special
				// permissions. Does this prevent our program from running in certain situations?
				Type t = typeof(ScrollableControl);

				return (Rectangle)t.InvokeMember("GetDisplayRectInternal",
					BindingFlags.DeclaredOnly | BindingFlags.NonPublic |
					BindingFlags.InvokeMethod | BindingFlags.Instance, null, this, null);

			}
			set
			{
				// Unfortunately, the function SetDisplayRectangleSize, that sets displayRect (which
				// is used internally in ScrollableControl instead of property DisplayRectangle),
				// is declared private, so we have to call it via Reflection.

				// REVIEW EberhardB: Calling SetDisplayRectangleSize via Reflection needs special
				// permissions. Does this prevent our program from running in certain situations?
				Type t = typeof(ScrollableControl);

				bool b = (bool)t.InvokeMember("SetDisplayRectangleSize",
					BindingFlags.DeclaredOnly | BindingFlags.NonPublic |
					BindingFlags.InvokeMethod | BindingFlags.Instance, null, this,
					new object [] { value.Width, value.Height });
			}
		}

		/// <summary>
		/// Gets or sets the maximum available height of the panel (including scroll buttons).
		/// </summary>
		/// <remarks>This property gets set in the OnResize method of the parent.</remarks>
		[Browsable(false)]
		public int MaxHeight
		{
			get
			{
				CheckDisposed();
				return m_maxHeight;
			}
			set
			{
				CheckDisposed();
				m_maxHeight = value;
			}
		}

		#endregion

		#region Event handler
		/// <summary>
		/// Scroll one button up
		/// </summary>
		internal void OnScrollUp(object sender, System.EventArgs e)
		{
			CheckDisposed();

			if (Controls.Count <= 0)
				return;

			Point pt = AutoScrollPosition;
			pt.Y = -pt.Y - Controls[0].Height - Padding.Vertical;
			AutoScrollPosition = pt;

			ShowButtons();
		}

		/// <summary>
		/// Scroll one button down
		/// </summary>
		internal void OnScrollDown(object sender, System.EventArgs e)
		{
			CheckDisposed();

			if (Controls.Count <= 0)
				return;

			Point pt = AutoScrollPosition;
			pt.Y = -pt.Y + Controls[0].Height + Padding.Vertical;
			AutoScrollPosition = pt;

			ShowButtons();
		}

		#endregion
	}
}
