using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.ViewModels;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Views;

public partial class MainWindow : Window
{
	private MainWindowViewModel? _viewModel;

	public MainWindow()
	{
		InitializeComponent();
	}

	protected override void OnDataContextChanged(EventArgs e)
	{
		if (_viewModel is not null)
			_viewModel.RequestClose -= ViewModelOnRequestClose;

		_viewModel = DataContext as MainWindowViewModel;
		if (_viewModel is not null)
			_viewModel.RequestClose += ViewModelOnRequestClose;

		base.OnDataContextChanged(e);
	}

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		if (DataContext is MainWindowViewModel vm)
		{
			if (vm.CancelOnClose)
				vm.Cancel();
		}
		else if (DataContext is ICancelableWork cancelable)
		{
			cancelable.Cancel();
		}

		base.OnClosing(e);
	}

	private void ViewModelOnRequestClose(object? sender, EventArgs e) => Close();

	private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}