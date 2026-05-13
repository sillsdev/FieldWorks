using System;
using Avalonia;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia;

internal static class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		AdvancedEntryTrace.Info("Starting AdvancedEntry.Avalonia");

		BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);
	}

	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.LogToTrace();
}