#if __MonoCS__
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.COMInterfaces;
using IBusDotNet;
using NDesk.DBus;

namespace SIL.FieldWorks.Views
{
	/// <summary>
	/// a global cache used only to reduce traffic with ibus via dbus.
	/// </summary>
	public static class GlobalCachedInputContext
	{
		/// <summary>
		/// Caches the current InputContext.
		/// </summary>
		public static InputContext InputContext { get; set; }
		/// <summary>
		/// Cache the keyboard name of the InputContext.
		/// </summary>
		public static string KeyboardName { get; set; }

		/// <summary>
		/// Clear the cached InputContext details.
		/// </summary>
		public static void Clear()
		{
			KeyboardName = String.Empty;
			InputContext = null;
		}
	}
}
#endif