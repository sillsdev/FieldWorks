// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrollListBox.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Listbox that provides access to scroll messages
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrollListBox : ListBox, IFWDisposable
	{
		private bool m_fHandleScrolling = true;

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
			if (IsDisposed)
				return;

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the required creation parameters when the control handle is created.
		/// We override the default parameters and set the LBS_WANTKEYBOARDINPUT flag, so
		/// that scrolling up/down in the list box works also with the cursor keys
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;
				createParams.Style |= 0x0400; // LBS_WANTKEYBOARDINPUT

				return createParams;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Occurs when the vertical scroll box has been moved by either a mouse or keyboard
		/// action.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public event ScrollEventHandler VScroll;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Processes Windows messages.
		/// </summary>
		/// <param name="m">The Windows Message to process.</param>
		/// ------------------------------------------------------------------------------------
		protected override void WndProc(ref Message m)
		{
			if (m.Msg == (int)Win32.WinMsgs.WM_VSCROLL)
			{
				OnVScroll(m.WParam);
				m.Result = IntPtr.Zero;
				return;
			}
			else if (m.Msg == (int)Win32.WinMsgs.WM_KEYDOWN)
			{
				OnKeyDown(new KeyEventArgs((Keys)m.WParam.ToInt32()));
				m.Result = IntPtr.Zero;
				return;
			}
			else if (m.Msg == (int)Win32.WinMsgs.WM_MOUSEMOVE)
			{
				OnMouseMove(new MouseEventArgs(
					MiscUtils.TranslateMouseButtons((Win32.MouseButtons)m.WParam.ToInt32()), 0,
					MiscUtils.LoWord(m.LParam), MiscUtils.HiWord(m.LParam), 0));
				m.Result = IntPtr.Zero;
				return;
			}

			base.WndProc (ref m);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the VScroll event and does the scrolling
		/// </summary>
		/// <param name="wParam"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void OnVScroll(IntPtr wParam)
		{
			ScrollEventType type = (ScrollEventType)MiscUtils.LoWord(wParam);
			int iLimit = Items.Count - ItemsPerPage;
			int topIndex = -1;
			switch (type)
			{
				case ScrollEventType.First:
					topIndex = 0;
					break;
				case ScrollEventType.Last:
					topIndex = iLimit;
					break;
				case ScrollEventType.SmallDecrement:
					topIndex = TopIndex - 1;
					break;
				case ScrollEventType.SmallIncrement:
					topIndex = TopIndex + 1;
					break;
				case ScrollEventType.LargeDecrement:
					topIndex = TopIndex - ItemsPerPage;
					break;
				case ScrollEventType.LargeIncrement:
					topIndex = TopIndex + ItemsPerPage;
					break;
				case ScrollEventType.ThumbTrack:
				case ScrollEventType.ThumbPosition:
					topIndex = MiscUtils.HiWord(wParam);
					break;
				case ScrollEventType.EndScroll:
					topIndex = TopIndex;
					break;
			}
			if (topIndex < 0)
				topIndex = 0;
			else if (topIndex > iLimit)
				topIndex = iLimit;

			if (VScroll != null)
				VScroll(this, new ScrollEventArgs(type, topIndex));

			if (m_fHandleScrolling)
				TopIndex = topIndex;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll vertical
		/// </summary>
		/// <param name="type"></param>
		/// ------------------------------------------------------------------------------------
		public void VerticalScroll(ScrollEventType type)
		{
			CheckDisposed();

			Win32.PostMessage(Handle, Win32.WinMsgs.WM_VSCROLL, (int)type, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of items that are shown on one page
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public int ItemsPerPage
		{
			get
			{
				CheckDisposed();

				return Height / ItemHeight;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a flag if list box handles scrolling (<c>true</c>) or if parent
		/// sets scroll position (<c>false</c>)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Description("true if list box handles scrolling, false if parent sets scroll position")]
		[DefaultValue(true)]
		public bool HandleScrolling
		{
			get
			{
				CheckDisposed();

				return m_fHandleScrolling;
			}
			set
			{
				CheckDisposed();

				m_fHandleScrolling = value;
			}
		}
	}
}
