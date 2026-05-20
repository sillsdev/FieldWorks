using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.ViewModels;

public interface ICancelableWork
{
	void Cancel();
}

public interface ISavableWork
{
	void Save();
}

public sealed class MainWindowViewModel : INotifyPropertyChanged, ICancelableWork, IDisposable
{
	private readonly CancellationTokenSource _cts = new();
	private readonly DelegateCommand _saveCommand;
	private readonly DelegateCommand _cancelCommand;
	private object? _entry;
	private IDisposable? _entryLifetime;
	private bool _cancelOnClose = true;
	private int _started;

	public event PropertyChangedEventHandler? PropertyChanged;
	public event EventHandler? RequestClose;

	public MainWindowViewModel()
	{
		_saveCommand = new DelegateCommand(SaveCore, CanSave);
		_cancelCommand = new DelegateCommand(CancelCore);
	}

	public ICommand SaveCommand => _saveCommand;
	public ICommand CancelCommand => _cancelCommand;
	public bool CancelOnClose => _cancelOnClose;

	public object? Entry
	{
		get => _entry;
		private set
		{
			if (ReferenceEquals(_entry, value))
				return;
			_entry = value;
			OnPropertyChanged();
		}
	}

	public void StartLoading(Func<CancellationToken, Task<object?>> loader)
	{
		if (Interlocked.Exchange(ref _started, 1) != 0)
			throw new InvalidOperationException("Loading can only be started once.");

		_ = LoadAsync(loader);
	}

	public void StartLoading(Func<CancellationToken, Task<(object? Entry, IDisposable? Lifetime)>> loader)
	{
		if (Interlocked.Exchange(ref _started, 1) != 0)
			throw new InvalidOperationException("Loading can only be started once.");

		_ = LoadAsync(loader);
	}

	public void Cancel()
	{
		_cts.Cancel();
		DisposeLoadedEntry();
	}

	public void Dispose()
	{
		DisposeLoadedEntry();
		_cts.Dispose();
	}

	private async Task LoadAsync(Func<CancellationToken, Task<object?>> loader)
	{
		try
		{
			var loadedEntry = await loader(_cts.Token).ConfigureAwait(false);
			if (_cts.IsCancellationRequested)
				return;

			await Dispatcher.UIThread.InvokeAsync(() => Entry = loadedEntry);
		}
		catch (OperationCanceledException)
		{
			// Expected on window close.
		}
		catch (Exception ex)
		{
			AdvancedEntryTrace.Error("AdvancedEntry load failed", ex);
		}
	}

	private async Task LoadAsync(Func<CancellationToken, Task<(object? Entry, IDisposable? Lifetime)>> loader)
	{
		try
		{
			var (loadedEntry, lifetime) = await loader(_cts.Token).ConfigureAwait(false);
			if (_cts.IsCancellationRequested)
			{
				lifetime?.Dispose();
				return;
			}

			await Dispatcher.UIThread.InvokeAsync(() =>
			{
				DisposeLoadedEntry();
				_entryLifetime = lifetime;
				Entry = loadedEntry;
				_saveCommand.RaiseCanExecuteChanged();
			});
		}
		catch (OperationCanceledException)
		{
			// Expected on window close.
		}
		catch (Exception ex)
		{
			AdvancedEntryTrace.Error("AdvancedEntry load failed", ex);
		}
	}

	private void DisposeLoadedEntry()
	{
		try
		{
			_entryLifetime?.Dispose();
		}
		catch (Exception ex)
		{
			AdvancedEntryTrace.Error("Disposing AdvancedEntry resources failed", ex);
		}
		finally
		{
			_entryLifetime = null;
			Entry = null;
			_saveCommand.RaiseCanExecuteChanged();
		}
	}

	private bool CanSave() => _entryLifetime is ISavableWork;

	private void SaveCore()
	{
		try
		{
			if (_entryLifetime is not ISavableWork savable)
				return;
			savable.Save();
			_cancelOnClose = false;
			DisposeLoadedEntry();
			RequestClose?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			AdvancedEntryTrace.Error("AdvancedEntry save failed", ex);
		}
	}

	private void CancelCore()
	{
		_cancelOnClose = false;
		Cancel();
		RequestClose?.Invoke(this, EventArgs.Empty);
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	private sealed class DelegateCommand : ICommand
	{
		private readonly Action _execute;
		private readonly Func<bool>? _canExecute;

		public DelegateCommand(Action execute, Func<bool>? canExecute = null)
		{
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}

		public event EventHandler? CanExecuteChanged;

		public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
		public void Execute(object? parameter) => _execute();

		public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
	}
}