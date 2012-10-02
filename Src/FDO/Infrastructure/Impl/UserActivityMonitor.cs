using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// This class is a message filter which can be installed in order to track when the user last
	/// pressed a key or did any mouse action, including moving the mouse.
	/// </summary>
	class UserActivityMonitor : IMessageFilter
	{
		internal DateTime LastActivityTime;

		private IntPtr m_lastMousePosition;

		public bool PreFilterMessage(ref Message m)
		{
			if(m.Msg == (int)Win32.WinMsgs.WM_MOUSEMOVE)
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
			if ((m.Msg >= (int)Win32.WinMsgs.WM_MOUSE_Min && m.Msg <= (int)Win32.WinMsgs.WM_MOUSE_Max)
				|| (m.Msg >= (int)Win32.WinMsgs.WM_KEY_Min && m.Msg <= (int)Win32.WinMsgs.WM_KEY_Max))
			{
				LastActivityTime = DateTime.Now;
			}
			return false; // don't want to block any messages.
		}
	}
}
