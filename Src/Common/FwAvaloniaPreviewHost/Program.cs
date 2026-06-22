// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;

namespace SIL.FieldWorks.Common.FwAvalonia.PreviewHost
{
	internal static class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
			{
				try
				{
					Trace.TraceError("[FwAvaloniaPreviewHost] Unhandled exception (terminating=" + e.IsTerminating + ")." + Environment.NewLine + e.ExceptionObject);
					Trace.Flush();
				}
				catch
				{
				}
			};

			TaskScheduler.UnobservedTaskException += (sender, e) =>
			{
				try
				{
					Trace.TraceError("[FwAvaloniaPreviewHost] Unobserved task exception." + Environment.NewLine + e.Exception);
					Trace.Flush();
					e.SetObserved();
				}
				catch
				{
				}
			};

			AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
			{
				try
				{
					Trace.WriteLine("[FwAvaloniaPreviewHost] ProcessExit.");
					Trace.Flush();
				}
				catch
				{
				}
			};

			PreviewHostLogging.Initialize();
			PreviewOptions.Current = PreviewOptions.Parse(args);
			FwAvaloniaLocalizationBootstrap.EnsureInitialized();
			Trace.WriteLine("[FwAvaloniaPreviewHost] Starting. module='" + PreviewOptions.Current.ModuleId + "' data='" + PreviewOptions.Current.DataMode + "'");

			BuildAvaloniaApp().StartWithClassicDesktopLifetime(Array.Empty<string>());
		}

		public static AppBuilder BuildAvaloniaApp()
			=> AppBuilder.Configure<PreviewHostApp>()
				.UsePlatformDetect()
				.LogToTrace();
	}
}
