// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using System.Runtime.InteropServices;

namespace SIL.FieldWorks.Views
{
	/// <summary>
	/// This class wrapps a hwnd to allow cross platform access to
	/// window properties.
	/// </summary>
	[Guid("3fb0fcd2-ac55-42a8-b580-73b89a2b6215")]
	public class ManagedVwWindow : IVwWindow
	{
		// the wrapped Window
		protected Control m_control = null;

		#region IVwWindow Members

		public void GetClientRectangle(out Rect clientRectangle)
		{
			if ( m_control == null)
				throw new ApplicationException("Window not set");

			clientRectangle.top = m_control.ClientRectangle.Top;
			clientRectangle.left = m_control.ClientRectangle.Left;
			clientRectangle.right = m_control.ClientRectangle.Right;
			clientRectangle.bottom = m_control.ClientRectangle.Bottom;
		}

		public uint Window
		{
			set
			{
				var ptr = (IntPtr) value;
				m_control = Control.FromHandle(ptr);
			}
		}

		#endregion
	}

} // end namespace SIL.FieldWorks.Views
