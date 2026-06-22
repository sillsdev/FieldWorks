// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia;
using Avalonia.Themes.Fluent;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Minimal Avalonia <see cref="Application"/> for shared FieldWorks Avalonia surfaces. Adds the
	/// Fluent theme so the pure-C# controls receive templates both in the Preview Host and in
	/// headless tests.
	/// </summary>
	public sealed class FwAvaloniaApp : Application
	{
		public override void Initialize()
		{
			Styles.Add(new FluentTheme());
		}
	}

	/// <summary>
	/// AppBuilder configuration for shared FieldWorks Avalonia hosting. Desktop/Win32 platform
	/// detection is used for the in-process embedding path beside WinForms; headless tests configure
	/// their own platform.
	/// </summary>
	public static class FwAvaloniaHost
	{
		/// <summary>Builds the AppBuilder for desktop (Win32) hosting.</summary>
		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<FwAvaloniaApp>()
				.UsePlatformDetect()
				.LogToTrace();
	}
}