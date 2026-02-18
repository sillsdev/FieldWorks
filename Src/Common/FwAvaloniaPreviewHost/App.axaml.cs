using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SIL.FieldWorks.Common.Avalonia.Preview;

namespace SIL.FieldWorks.Common.Avalonia.PreviewHost;

public sealed partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		System.Diagnostics.Trace.WriteLine($"[FwAvaloniaPreviewHost] Framework init. Lifetime='{ApplicationLifetime?.GetType().FullName ?? "<null>"}'");

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			try
			{
				var catalog = new ModuleCatalog();
				var options = PreviewOptions.Current;
				System.Diagnostics.Trace.WriteLine($"[FwAvaloniaPreviewHost] Resolving module. module='{options.ModuleId}' data='{options.DataMode}'");
				var module = catalog.Find(options.ModuleId);
				if (module is null)
				{
					throw new InvalidOperationException(
						"No preview modules were found. Ensure at least one module assembly is present and declares [assembly: FwPreviewModule(...)]");
				}

				var window = CreateWindow(module, options.DataMode);
				window.Opened += (_, _) => System.Diagnostics.Trace.WriteLine("[FwAvaloniaPreviewHost] MainWindow Opened.");
				window.Closing += (_, _) => System.Diagnostics.Trace.WriteLine("[FwAvaloniaPreviewHost] MainWindow Closing.");
				window.Closed += (_, _) => System.Diagnostics.Trace.WriteLine("[FwAvaloniaPreviewHost] MainWindow Closed.");
				System.Diagnostics.Trace.WriteLine($"[FwAvaloniaPreviewHost] MainWindow created. type='{window.GetType().FullName}' title='{window.Title}'");
				desktop.MainWindow = window;
				if (!desktop.MainWindow.IsVisible)
				{
					desktop.MainWindow.Show();
					System.Diagnostics.Trace.WriteLine("[FwAvaloniaPreviewHost] MainWindow shown.");
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Trace.TraceError($"[FwAvaloniaPreviewHost] Startup failed. module='{PreviewOptions.Current.ModuleId}' data='{PreviewOptions.Current.DataMode}' log='{PreviewHostLogging.LogFilePath}'{Environment.NewLine}{ex}");
				desktop.MainWindow = CreateErrorWindow(ex, PreviewOptions.Current);
				if (!desktop.MainWindow.IsVisible)
					desktop.MainWindow.Show();
			}
		}

		base.OnFrameworkInitializationCompleted();
	}

	private static Window CreateErrorWindow(Exception ex, PreviewOptions options)
	{
		var message =
			$"Preview Host failed to start.\n\n" +
			$"module: {options.ModuleId}\n" +
			$"data: {options.DataMode}\n\n" +
			$"log: {PreviewHostLogging.LogFilePath}\n\n" +
			ex.ToString();

		return new Window
		{
			Title = "FieldWorks Avalonia Preview Host - Error",
			Width = 1000,
			Height = 700,
			Content = new TextBox
			{
				Text = message,
				IsReadOnly = true,
				TextWrapping = global::Avalonia.Media.TextWrapping.NoWrap,
				AcceptsReturn = true,
			},
		};
	}

	private static Window CreateWindow(ModuleInfo module, string dataMode)
	{
		if (Activator.CreateInstance(module.WindowType) is not Window window)
			throw new InvalidOperationException($"Module '{module.Id}' window type is not an Avalonia Window: {module.WindowType.FullName}");

		if (module.DataProviderType is not null)
		{
			if (Activator.CreateInstance(module.DataProviderType) is IFwPreviewDataProvider provider)
			{
				window.DataContext = provider.CreateDataContext(dataMode);
			}
		}

		window.Title = $"{module.DisplayName} (Preview)";
		return window;
	}
}
