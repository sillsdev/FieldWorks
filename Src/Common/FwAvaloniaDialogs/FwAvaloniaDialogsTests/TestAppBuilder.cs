// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia;
using Avalonia.Headless;
using FwAvaloniaDialogsTests;
using SIL.FieldWorks.Common.FwAvalonia;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// Headless Avalonia application builder for the dialog-MVVM tests. Reuses <see cref="FwAvaloniaApp"/>
	/// from the foundation so the Fluent theme applies and templated controls (TabControl, CheckBox,
	/// ComboBox, Button) realize under the headless platform.
	/// </summary>
	public static class TestAppBuilder
	{
		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<FwAvaloniaApp>()
				.UseSkia()
				.UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
	}
}
