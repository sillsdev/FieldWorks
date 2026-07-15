// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using SIL.FieldWorks.Common.FwAvalonia.Preview;

namespace SIL.FieldWorks.Common.FwAvalonia.PreviewHost
{
	internal sealed class PreviewHostApp : Application
	{
		public override void Initialize()
		{
			Styles.Add(new FluentTheme());
		}

		public override void OnFrameworkInitializationCompleted()
		{
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				try
				{
					var catalog = new ModuleCatalog();
					var options = PreviewOptions.Current;
					var module = catalog.Find(options.ModuleId);
					if (module == null)
					{
						throw new InvalidOperationException(
							"No preview modules were found. Ensure at least one module assembly is present and declares [assembly: FwPreviewModule(...)]");
					}

					var window = CreateWindow(module, options.DataMode);
					desktop.MainWindow = window;
					if (!window.IsVisible)
						window.Show();
				}
				catch (Exception ex)
				{
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
				"Preview Host failed to start.\n\n" +
				"module: " + options.ModuleId + "\n" +
				"data: " + options.DataMode + "\n\n" +
				"log: " + PreviewHostLogging.LogFilePath + "\n\n" +
				ex.ToString();

			var text = new TextBox
			{
				Text = message,
				IsReadOnly = true,
				TextWrapping = TextWrapping.NoWrap,
				AcceptsReturn = true
			};
			AutomationProperties.SetAutomationId(text, "PreviewHost.ErrorText");
			AutomationProperties.SetName(text, "Preview host error text");

			var window = new Window
			{
				Title = "FieldWorks Avalonia Preview Host - Error",
				Width = 1000,
				Height = 700,
				Content = text
			};
			AutomationProperties.SetAutomationId(window, "PreviewHost.ErrorWindow");
			AutomationProperties.SetName(window, "FieldWorks Avalonia Preview Host Error");
			return window;
		}

		private static Window CreateWindow(ModuleInfo module, string dataMode)
		{
			var window = Activator.CreateInstance(module.WindowType) as Window;
			if (window == null)
				throw new InvalidOperationException("Module '" + module.Id + "' window type is not an Avalonia Window: " + module.WindowType.FullName);

			if (module.DataProviderType != null)
			{
				var provider = Activator.CreateInstance(module.DataProviderType) as IFwPreviewDataProvider;
				if (provider != null)
					window.DataContext = provider.CreateDataContext(dataMode);
			}

			window.Title = module.DisplayName + " (Preview)";
			AutomationProperties.SetAutomationId(window, "FwAvaloniaPreviewHost.MainWindow");
			AutomationProperties.SetName(window, window.Title);
			return window;
		}
	}
}
