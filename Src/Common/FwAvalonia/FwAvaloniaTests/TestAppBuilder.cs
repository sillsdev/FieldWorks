// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia;
using Avalonia.Headless;
using FwAvaloniaTests;
using SIL.FieldWorks.Common.FwAvalonia.Poc;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace FwAvaloniaTests
{
	/// <summary>
	/// Headless Avalonia application builder for the POC tests. Uses <see cref="PocApp"/> so the
	/// Fluent theme is applied and the pure-C# controls receive templates under the headless platform.
	/// Skia drawing (instead of the null headless drawing backend) enables rendered-frame capture for
	/// visual parity evidence (task 6.9); all other headless behavior (input, focus, layout) is unchanged.
	/// </summary>
	public static class TestAppBuilder
	{
		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<PocApp>()
				.UseSkia()
				.UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
	}
}
