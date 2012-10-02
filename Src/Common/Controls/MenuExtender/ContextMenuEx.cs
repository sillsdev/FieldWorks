using System;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class extends the ContextMenu. For some reason, the .Net framework doesn't
	/// generate the popup event for any menu items except the context menu itself. If any of
	/// the menu items in the context menu have sub menus of their own, you can subscribe to
	/// those menu item's popup events until you're blue in the face and you'll never see the
	/// event. A ContextMenuEx however, will raise the popup event for every menu item in the
	/// context menu that has sub menus.
	///
	/// This class will also monitor menu selections and set some flags used in the ToolBarEx
	/// class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ContextMenuEx : ContextMenu, IFWDisposable
	{
		#region Member variables and declarations
		[StructLayout(LayoutKind.Sequential)]
			private struct MSG
		{
			public IntPtr hwnd;
			public int message;
			public IntPtr wParam;
			public IntPtr lParam;
			public int time;
			public int pt_x;
			public int pt_y;
		}

		[DllImport("user32.dll", CharSet=CharSet.Auto)]
		private static extern IntPtr SetWindowsHookEx(int hookid, HookProc pfnhook, IntPtr hinst, int threadid);
		[DllImport("user32.dll", CharSet=CharSet.Auto, ExactSpelling=true)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhook);
		[DllImport("user32.dll", CharSet=CharSet.Auto, ExactSpelling=true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhook, int code, IntPtr wparam, IntPtr lparam);
		[DllImport("kernel32.dll", ExactSpelling=true, CharSet=CharSet.Auto)]
		private static extern int GetCurrentThreadId();

		private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

		private const int WM_INITMENUPOPUP = 0x117;
		private const int WM_MENUSELECT = 0x11F;
		private const int WH_CALLWNDPROC = 0x04;
		private const int MF_POPUP = 0x10;

		private IntPtr m_oldHookHandle;
		private bool m_currentHasSubMenu = false;
		private bool m_currentMenuAtTopLevel = false;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ContextMenuEx() : base()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="menuItems"></param>
		/// ------------------------------------------------------------------------------------
		public ContextMenuEx(MenuItem[] menuItems) : base(menuItems)
		{
		}
		#endregion

		#region Disposal

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
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		private bool m_isDisposed = false;

		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (m_isDisposed)
				return;

			if (disposing)
			{
			}

			base.Dispose(disposing);

			m_isDisposed = true;
		}

		#endregion Disposal

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the current menu item has a submenu. This
		/// property is correct regardless of how deep the selected current menu item is in
		/// the context menu. (Obviously this property is only valid while a context menu is
		/// being shown.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CurrentItemHasSubMenu
		{
			get
			{
				CheckDisposed();
				return m_currentHasSubMenu;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the current menu item is one of the top-level
		/// items in the context menu. This property is correct regardless of how deep the
		/// selected current menu item is in the context menu. (Obviously this property is only
		/// valid while a context menu is being shown.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsCurrentItemAtTopLevel
		{
			get
			{
				CheckDisposed();
				return m_currentMenuAtTopLevel;
			}
		}
		#endregion

		#region Implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ctrl"></param>
		/// <param name="pt"></param>
		/// ------------------------------------------------------------------------------------
		public new void Show(Control ctrl, Point pt)
		{
			CheckDisposed();

			// Setup a hook so we can watch all the windows messages.
			HookProc hookProc = new HookProc(WatchForPopupHookProc);
			GCHandle hookProcHandle = GCHandle.Alloc(hookProc);
			m_oldHookHandle = SetWindowsHookEx(WH_CALLWNDPROC, hookProc, IntPtr.Zero,
				GetCurrentThreadId());

			if (m_oldHookHandle == IntPtr.Zero)
				throw new System.Security.SecurityException();

			base.Show(ctrl, pt);

			// Remove the hook that was setup before the menu was shown.
			UnhookWindowsHookEx(m_oldHookHandle);
			hookProcHandle.Free();
			m_oldHookHandle = IntPtr.Zero;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This hook procedure will monitor all the windows messages while the context menu
		/// is visible.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="wparam"></param>
		/// <param name="lparam"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IntPtr WatchForPopupHookProc(int code, IntPtr wparam, IntPtr lparam)
		{
			if (code >= 0)
			{
				// I'm not sure why the values in the msg structure don't seem to correspond
				// to what they really are. It's like they didn't get marshalled correctly.
				// But, this works so, whatever. The wParam is the windows message and the
				// hWnd is in the message.
				MSG msg = (MSG)Marshal.PtrToStructure(lparam, typeof(MSG));

				// We only care about menu select and init. popup messsages.
				if ((int)msg.wParam == WM_MENUSELECT)
					ProcessMenuSelect(msg);
				else if ((int)msg.wParam == WM_INITMENUPOPUP)
					ProcessInitPopup(msg);
			}

			return CallNextHookEx(m_oldHookHandle, code, wparam, lparam);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the item being selected has a sub menu and whether or not
		/// it's a top-level menu.
		/// </summary>
		/// <param name="msg">The MSG structure containing information about the newly
		/// selected menu item.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessMenuSelect(MSG msg)
		{
			// Determine whether or not the menu being selected has submenus. (Why the flags
			// are in the message element of the MSG structure is a mystery to me.)
			int flags = msg.message >> 16;
			m_currentHasSubMenu = ((flags & MF_POPUP) > 0);

			// Determine whether or not the item being selected is in the context menu or is
			// in a submenu.
			m_currentMenuAtTopLevel = (msg.hwnd == Handle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the menu whose sub menu is being popped-up and invokes it's OnPopup event.
		/// </summary>
		/// <param name="msg">The MSG structure containing information about the newly
		/// selected menu item.</param>
		/// ------------------------------------------------------------------------------------
		private void ProcessInitPopup(MSG msg)
		{
			// Get the menu handle of the menu that's about to popup. Use that to find the
			// MenuItem to which it corresponds. (Why the handle is in the message element of
			// the MSG structure is a mystery to me.)
			IntPtr hWnd = (IntPtr)msg.message;
			MenuItem menuItem = FindMenuItem(MenuItem.FindHandle, hWnd);

			// Did we find a MenuItem and does it have a sub menu?
			if (menuItem != null && menuItem.IsParent)
			{
				// Use reflection to raise the menu item's OnPopup event.
				MethodInfo onpopup = menuItem.GetType().GetMethod("OnPopup",
					BindingFlags.Instance |	BindingFlags.Public |
					BindingFlags.NonPublic);

				onpopup.Invoke(menuItem, new object [] {new EventArgs()});
			}
		}

		#endregion
	}
}
