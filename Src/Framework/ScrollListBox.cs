// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Listbox that provides access to scroll messages
	/// </summary>
	public class ScrollListBox : ListBox
	{
		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			base.Dispose(disposing);
		}

		/// <inheritdoc />
		protected override CreateParams CreateParams
		{
			get
			{
				var createParams = base.CreateParams;
				createParams.Style |= 0x0400; // LBS_WANTKEYBOARDINPUT

				return createParams;
			}
		}

		/// <summary>
		/// Occurs when the vertical scroll box has been moved by either a mouse or keyboard
		/// action.
		/// </summary>
		public event ScrollEventHandler VScroll;

		/// <inheritdoc />
		protected override void WndProc(ref Message m)
		{
			if (m.Msg == (int)Win32.WinMsgs.WM_VSCROLL)
			{
				OnVScroll(m.WParam);
				m.Result = IntPtr.Zero;
				return;
			}
			if (m.Msg == (int)Win32.WinMsgs.WM_KEYDOWN)
			{
				OnKeyDown(new KeyEventArgs((Keys)m.WParam.ToInt32()));
				m.Result = IntPtr.Zero;
				return;
			}
			if (m.Msg == (int)Win32.WinMsgs.WM_MOUSEMOVE)
			{
				OnMouseMove(new MouseEventArgs(FwUtils.FwUtils.TranslateMouseButtons((Win32.MouseButtons)m.WParam.ToInt32()), 0, MiscUtils.LoWord(m.LParam), MiscUtils.HiWord(m.LParam), 0));
				m.Result = IntPtr.Zero;
				return;
			}

			base.WndProc(ref m);
		}

		/// <summary>
		/// Raises the VScroll event and does the scrolling
		/// </summary>
		private void OnVScroll(IntPtr wParam)
		{
			var type = (ScrollEventType)MiscUtils.LoWord(wParam);
			var iLimit = Items.Count - ItemsPerPage;
			var topIndex = -1;
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
			{
				topIndex = 0;
			}
			else if (topIndex > iLimit)
			{
				topIndex = iLimit;
			}

			VScroll?.Invoke(this, new ScrollEventArgs(type, topIndex));

			if (HandleScrolling)
			{
				TopIndex = topIndex;
			}
		}

		/// <summary>
		/// Scroll vertical
		/// </summary>
		public void VerticalScroll(ScrollEventType type)
		{
			Win32.PostMessage(Handle, Win32.WinMsgs.WM_VSCROLL, (int)type, 0);
		}

		/// <summary>
		/// Gets the number of items that are shown on one page
		/// </summary>
		[Browsable(false)]
		public int ItemsPerPage => Height / ItemHeight;

		/// <summary>
		/// Gets or sets a flag if list box handles scrolling (<c>true</c>) or if parent
		/// sets scroll position (<c>false</c>)
		/// </summary>
		[Description("true if list box handles scrolling, false if parent sets scroll position")]
		[DefaultValue(true)]
		public bool HandleScrolling { get; set; } = true;
	}
}