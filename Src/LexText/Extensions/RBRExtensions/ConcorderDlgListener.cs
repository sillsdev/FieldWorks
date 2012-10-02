using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using XCore;

namespace RBRExtensions
{
	/// <summary>
	/// XCore listener for the Concorder dlg.
	/// </summary>
	public class ConcorderDlgListener : DlgListenerBase
	{
		/// <summary>
		/// Provide access to the Win32 ::PostMessage() function.
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="Msg"></param>
		/// <param name="wParam"></param>
		/// <param name="lParam"></param>
		/// <returns></returns>
		[System.Runtime.InteropServices.DllImport("User32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
		public static extern bool PostMessage(IntPtr hWnd, int Msg, uint wParam, uint lParam);
		/// <summary>
		/// Provide access to the Win32 ::IsWindow() function.
		/// </summary>
		/// <param name="hWnd"></param>
		/// <returns></returns>
		[System.Runtime.InteropServices.DllImport("User32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
		public static extern bool IsWindow(IntPtr hWnd);

		#region Properties

		/// <summary>
		/// Override to get a suitable label.
		/// </summary>
		protected override string PersistentLabel
		{
			get { return "ConcorderDlg"; }
		}

		#endregion Properties

		#region Construction and Initialization

		/// <summary>
		/// Constructor.
		/// </summary>
		public ConcorderDlgListener()
		{
		}

		#endregion Construction and Initialization

		List<IntPtr> m_rghwnd = new List<IntPtr>();

		#region XCORE Message Handlers

		/// <summary>
		/// Launch the Concorder dlg.
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnViewConcorder(object argument)
		{
			CheckDisposed();

			// It is modeless, so we don't dispose it here.
			Concorder dlg = new Concorder();
			XWindow xwindow = m_mediator.PropertyTable.GetValue("window") as XWindow;
			dlg.SetDlgInfo(xwindow, m_configurationParameters);
			dlg.Show(xwindow);
			PruneDeadHandles();
			m_rghwnd.Add(dlg.Handle);

			return true;
		}

		/// <summary>
		/// Try to keep list from growing indefinitely as user creates and closes
		/// Concorder dlgs.
		/// </summary>
		private void PruneDeadHandles()
		{
			// deleting from the end of the list should be safe.
			for (int i = m_rghwnd.Count - 1; i >= 0; --i)
			{
				if (!IsWindow(m_rghwnd[i]))
					m_rghwnd.RemoveAt(i);
			}
		}

		/// <summary>
		/// Close any open Concorder dlg.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnCloseConcorder(object argument)
		{
			CheckDisposed();

			foreach (IntPtr hwnd in m_rghwnd)
			{
				if (IsWindow(hwnd))
					PostMessage(hwnd, Concorder.WM_BROADCAST_CLOSE_CONCORDER, 0, 0);
			}
			m_rghwnd.Clear();
			return true;
		}

		#endregion XCORE Message Handlers
	}
}
