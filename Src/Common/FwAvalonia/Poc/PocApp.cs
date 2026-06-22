// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia;
using Avalonia.Themes.Fluent;

namespace SIL.FieldWorks.Common.FwAvalonia.Poc
{
	/// <summary>
	/// Minimal Avalonia <see cref="Application"/> for the POC. Adds the Fluent theme so the
	/// pure-C# controls receive templates both in the Preview Host and in headless tests.
	/// </summary>
	public sealed class PocApp : Application
	{
		public override void Initialize()
		{
			Styles.Add(new FluentTheme());
		}
	}

	/// <summary>
	/// AppBuilder configuration for the POC. Desktop/Win32 platform detection is used for the
	/// in-process embedding path beside WinForms; headless tests configure their own platform.
	/// </summary>
	public static class PocAvaloniaHost
	{
		/// <summary>Builds the AppBuilder for desktop (Win32) hosting.</summary>
		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<PocApp>()
				.UsePlatformDetect()
				.LogToTrace();

		/// <summary>Creates the POC slice control for the given entry (the embeddable Avalonia content).</summary>
		public static PocLexEntrySlice CreateSlice(PocEntryDto entry)
			=> new PocLexEntrySlice(entry);
	}
}
