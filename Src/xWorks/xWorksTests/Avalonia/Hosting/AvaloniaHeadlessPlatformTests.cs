// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Reflection;
using Avalonia;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The programmatic proxy for "no real Avalonia window flashes during the test run". The
	/// assembly-level <see cref="AvaloniaHeadlessSetUpFixture"/> sets
	/// <see cref="FwAvaloniaRuntime.AppBuilderOverride"/> to a HEADLESS builder before any test runs, and
	/// the product surface hosts (RecordEditView/RecordBrowseView) funnel through
	/// <see cref="FwAvaloniaRuntime.EnsureInitialized"/>. So after initialization the active Avalonia
	/// windowing platform must be the headless implementation, not the real Win32 one — any window,
	/// flyout, or popup the product code raises is therefore off-screen. We can't visually confirm "no
	/// flash"; this type assertion is the evidence.
	///
	/// <c>IWindowingPlatform</c> and <c>AvaloniaLocator.Current</c> are internal in Avalonia 11.3, so the
	/// active platform is resolved through reflection rather than the (compile-time-inaccessible) locator
	/// API — the assertion is on the resolved implementation's assembly, which is the load-bearing fact.
	/// </summary>
	[TestFixture]
	public class AvaloniaHeadlessPlatformTests
	{
		[Test]
		public void Override_IsTheHeadlessBuilder()
		{
			Assert.That(FwAvaloniaRuntime.AppBuilderOverride, Is.Not.Null,
				"the assembly SetUpFixture must install the headless AppBuilder override before any test runs");
		}

		[Test]
		public void ActiveWindowingPlatform_IsHeadless_NotWin32()
		{
			// Force the same one-time init the product hosts trigger. Idempotent: if a product-surface
			// test already initialized it, this is a no-op; either way the platform is now resolvable.
			FwAvaloniaRuntime.EnsureInitialized();
			Assert.That(FwAvaloniaRuntime.IsInitialized, Is.True);

			var windowing = ResolveWindowingPlatform();
			Assert.That(windowing, Is.Not.Null, "an Avalonia windowing platform must be registered after init");

			var platformAssembly = windowing.GetType().Assembly.GetName().Name;
			Assert.That(platformAssembly, Is.EqualTo("Avalonia.Headless"),
				"the active windowing platform must be the HEADLESS one (so product popups/dialogs never " +
				"become real on-screen Win32 windows); was: " + windowing.GetType().FullName);

			// And explicitly NOT the real Win32 desktop platform.
			Assert.That(windowing.GetType().FullName, Does.Not.Contain("Win32"),
				"the active windowing platform must not be the real Win32 platform");
		}

		// AvaloniaLocator.Current.GetService<IWindowingPlatform>() via reflection (both are internal in 11.3).
		private static object ResolveWindowingPlatform()
		{
			// AvaloniaLocator and IWindowingPlatform both live in Avalonia.Base (AppBuilder is type-forwarded
			// into Avalonia.Controls, so resolve the base assembly explicitly rather than via a public type).
			// EnsureInitialized() has run, so Avalonia.Base is loaded; fall back to Load by name defensively.
			var baseAsm = AppDomain.CurrentDomain.GetAssemblies()
				.FirstOrDefault(a => a.GetName().Name == "Avalonia.Base")
				?? Assembly.Load("Avalonia.Base");
			var locatorType = baseAsm.GetType("Avalonia.AvaloniaLocator", throwOnError: true);
			var current = locatorType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)
				?.GetValue(null);
			Assert.That(current, Is.Not.Null, "AvaloniaLocator.Current was not resolvable by reflection");

			// IWindowingPlatform lives in Avalonia.Controls (not Avalonia.Base) in 11.3.
			var controlsAsm = AppDomain.CurrentDomain.GetAssemblies()
				.FirstOrDefault(a => a.GetName().Name == "Avalonia.Controls")
				?? Assembly.Load("Avalonia.Controls");
			var windowingPlatformType = controlsAsm.GetType("Avalonia.Platform.IWindowingPlatform", throwOnError: true);
			var getService = current.GetType()
				.GetMethods(BindingFlags.Public | BindingFlags.Instance)
				.First(m => m.Name == "GetService" && m.GetParameters().Length == 1
					&& m.GetParameters()[0].ParameterType == typeof(Type));
			return getService.Invoke(current, new object[] { windowingPlatformType });
		}
	}
}
