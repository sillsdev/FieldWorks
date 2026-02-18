using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia;

[assembly: AvaloniaTestApplication(typeof(SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Tests.AvaloniaHeadlessTestAppBuilder))]

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Tests;

public sealed class AvaloniaHeadlessTestAppBuilder
{
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UseHeadless(new AvaloniaHeadlessPlatformOptions())
			.LogToTrace();
}
