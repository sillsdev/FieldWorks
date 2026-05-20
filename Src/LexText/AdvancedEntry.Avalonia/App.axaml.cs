using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = new Views.MainWindow
			{
				DataContext = new ViewModels.MainWindowViewModel()
			};
		}

		base.OnFrameworkInitializationCompleted();
	}
}