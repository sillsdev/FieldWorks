using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;

namespace SIL.FieldWorks.Common.Avalonia.PreviewHost;

internal static class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += (_, e) =>
		{
			try
			{
				Trace.TraceError($"[FwAvaloniaPreviewHost] Unhandled exception (terminating={e.IsTerminating}).{Environment.NewLine}{e.ExceptionObject}");
				Trace.Flush();
			}
			catch
			{
				// ignored
			}
		};

		TaskScheduler.UnobservedTaskException += (_, e) =>
		{
			try
			{
				Trace.TraceError($"[FwAvaloniaPreviewHost] Unobserved task exception.{Environment.NewLine}{e.Exception}");
				Trace.Flush();
				// Avoid process termination in some hosting contexts.
				e.SetObserved();
			}
			catch
			{
				// ignored
			}
		};

		AppDomain.CurrentDomain.ProcessExit += (_, _) =>
		{
			try
			{
				Trace.WriteLine("[FwAvaloniaPreviewHost] ProcessExit.");
				Trace.Flush();
			}
			catch
			{
				// ignored
			}
		};

		PreviewHostLogging.Initialize();

		PreviewOptions.Current = PreviewOptions.Parse(args);
		System.Diagnostics.Trace.WriteLine($"[FwAvaloniaPreviewHost] Starting. module='{PreviewOptions.Current.ModuleId}' data='{PreviewOptions.Current.DataMode}'");

		BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(Array.Empty<string>());
	}

	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.LogToTrace();
}
