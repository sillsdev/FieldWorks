// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace SIL.Utils
{
	/// <summary>
	/// This class is a message filter which can be installed in order to track when the user last
	/// pressed a key or did any mouse action, including moving the mouse.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithNativeFieldsShouldBeDisposableRule", Justification="No unmanaged resources to release")]
	public class UserActivityMonitor : IMessageFilter
	{
		private IntPtr m_lastMousePosition;

		/// <summary>
		/// Starts monitoring user activity.
		/// </summary>
		public void StartMonitoring()
		{
			Application.AddMessageFilter(this);
		}

		/// <summary>
		/// Stops monitoring user activity.
		/// </summary>
		public void StopMonitoring()
		{
			Application.RemoveMessageFilter(this);
		}

		/// <summary>
		/// Gets the last user activity time.
		/// </summary>
		/// <value>
		/// The last activity user time.
		/// </value>
		public DateTime LastActivityTime { get; private set; }

		bool IMessageFilter.PreFilterMessage(ref Message m)
		{
			if(m.Msg == (int) Win32.WinMsgs.WM_MOUSEMOVE)
			{
				// For mouse move, we get spurious ones when it didn't really move. So check the actual position.
				if (m.LParam != m_lastMousePosition)
				{
					LastActivityTime = DateTime.Now;
					m_lastMousePosition = m.LParam;
					// Enhance JohnT: suppress ones where it doesn't move??
				}
				return false;
			}
			if ((m.Msg >= (int) Win32.WinMsgs.WM_MOUSE_Min && m.Msg <= (int) Win32.WinMsgs.WM_MOUSE_Max)
				|| (m.Msg >= (int) Win32.WinMsgs.WM_KEY_Min && m.Msg <= (int) Win32.WinMsgs.WM_KEY_Max))
			{
				LastActivityTime = DateTime.Now;
			}
			return false; // don't want to block any messages.
		}
	}
}
