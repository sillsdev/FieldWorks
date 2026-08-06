// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Tells the WinForms hosts whether the in-process Avalonia runtime is on a HEADLESS windowing
	/// platform (i.e. a test set <see cref="FwAvaloniaRuntime.AppBuilderOverride"/> to a headless builder)
	/// rather than the real Win32 platform. The hosts use this to make the WinForms HWND reparent a
	/// deliberate no-op under headless: there is no Win32 top-level handle to reparent, so the Avalonia
	/// content still constructs and lays out off-screen and tests assert logic, not on-screen pixels.
	///
	/// Detection is by reflection on the active <c>IWindowingPlatform</c>'s assembly (it is
	/// <c>Avalonia.Headless</c> under the headless platform). This keeps the production FwAvalonia DLL
	/// free of any compile-time dependency on Avalonia.Headless — production never loads that assembly,
	/// so the probe simply returns false and the real Win32 embed path is unchanged.
	/// </summary>
	internal static class FwAvaloniaPlatform
	{
		private const string HeadlessAssemblyName = "Avalonia.Headless";

		/// <summary>
		/// True when the active Avalonia windowing platform is the headless one. False on the real Win32
		/// platform (production) and false if the runtime is not yet initialized or the platform cannot be
		/// resolved — i.e. it never claims headless unless it can prove it, so production behavior is safe.
		/// </summary>
		internal static bool IsHeadless
		{
			get
			{
				try
				{
					var windowing = ResolveWindowingPlatform();
					return windowing != null
						&& string.Equals(windowing.GetType().Assembly.GetName().Name, HeadlessAssemblyName,
							StringComparison.OrdinalIgnoreCase);
				}
				catch
				{
					// Never let a detection failure change production hosting; default to the Win32 path.
					return false;
				}
			}
		}

		/// <summary>
		/// Makes the WinForms/Avalonia embed (the Win32 HWND reparent in
		/// <c>WinFormsAvaloniaControlHost.OnHandleCreated</c>) a deliberate no-op when the active platform
		/// is HEADLESS, by marking <paramref name="host"/> as design-mode — the control's own escape hatch:
		/// its handle-created path skips creating the embeddable root, getting the (nonexistent) Win32 top
		/// level handle, and calling <c>SetParent</c>/<c>AddMessageFilter</c> when <c>DesignMode</c> is true.
		/// The Avalonia content still constructs and lays out when shown (tests assert logic, not pixels).
		/// No-op on the real Win32 platform, so production hosting is unchanged.
		/// </summary>
		internal static void GuardHeadlessEmbed(Control host)
		{
			if (host != null && IsHeadless && host.Site == null)
				host.Site = new HeadlessDesignModeSite();
		}

		/// <summary>
		/// Minimal <see cref="ISite"/> whose only job is to report <see cref="ISite.DesignMode"/> = true so
		/// a hosted WinForms control takes its design-mode (no native embed) code path under the headless
		/// Avalonia platform. It is not a real designer site; everything else is inert.
		/// </summary>
		private sealed class HeadlessDesignModeSite : ISite
		{
			public IComponent Component => null;
			public IContainer Container => null;
			public bool DesignMode => true;
			public string Name { get; set; }
			public object GetService(Type serviceType) => null;
		}

		// AvaloniaLocator.Current.GetService(typeof(IWindowingPlatform)) via reflection: AvaloniaLocator is
		// internal in Avalonia 11.3 (lives in Avalonia.Base) and IWindowingPlatform lives in Avalonia.Controls.
		private static object ResolveWindowingPlatform()
		{
			var baseAsm = AppDomain.CurrentDomain.GetAssemblies()
				.FirstOrDefault(a => a.GetName().Name == "Avalonia.Base");
			var controlsAsm = AppDomain.CurrentDomain.GetAssemblies()
				.FirstOrDefault(a => a.GetName().Name == "Avalonia.Controls");
			if (baseAsm == null || controlsAsm == null)
				return null;

			var locatorType = baseAsm.GetType("Avalonia.AvaloniaLocator");
			var windowingPlatformType = controlsAsm.GetType("Avalonia.Platform.IWindowingPlatform");
			if (locatorType == null || windowingPlatformType == null)
				return null;

			var current = locatorType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)
				?.GetValue(null);
			if (current == null)
				return null;

			var getService = current.GetType()
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.FirstOrDefault(m => m.Name == "GetService" && m.GetParameters().Length == 1
					&& m.GetParameters()[0].ParameterType == typeof(Type));
			return getService?.Invoke(current, new object[] { windowingPlatformType });
		}
	}
}
