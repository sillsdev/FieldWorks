// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Styling;
using NUnit.Framework;

namespace FwAvaloniaTests
{
	/// <summary>
	/// The app must pin the Light theme variant. FwColorTokens.axaml ships a complete Dark
	/// dictionary that is a first-pass placeholder rather than design-approved, and an unset
	/// RequestedThemeVariant makes ActualThemeVariant follow the OS app theme -- so a machine in
	/// dark mode renders the unreviewed palette, and resolves it silently, because every Light
	/// key has a Dark counterpart and nothing fails to resolve.
	/// </summary>
	[TestFixture]
	public class ThemeVariantPinningTests
	{
		/// <summary>
		/// Asserts the requested variant, not the actual one. Headless reports
		/// ActualThemeVariant as Light whatever the request is, so the obvious version of this
		/// test passes against an unpinned app and proves nothing.
		/// </summary>
		[AvaloniaTest]
		public void Application_PinsTheLightThemeVariant()
		{
			var app = Application.Current;

			Assert.That(app, Is.Not.Null, "the headless harness must have started an Application");
			Assert.That(app.RequestedThemeVariant, Is.EqualTo(ThemeVariant.Light),
				"the app must request Light explicitly; leaving it unset follows the OS app "
				+ "theme and can resolve the unreviewed Dark palette");
		}
	}
}
