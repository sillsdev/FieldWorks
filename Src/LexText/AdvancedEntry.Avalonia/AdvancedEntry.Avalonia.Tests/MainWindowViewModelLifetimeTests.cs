using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using NUnit.Framework;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.ViewModels;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Views;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Tests;

[TestFixture]
public sealed class MainWindowViewModelLifetimeTests
{
	[AvaloniaTest]
	public async Task Cancel_DisposesLoadedLifetimeAndClearsEntry()
	{
		var lifetime = new TrackingLifetime();
		var entry = new object();
		var viewModel = new MainWindowViewModel();
		viewModel.StartLoading(_ => Task.FromResult<(object? Entry, IDisposable? Lifetime)>((entry, lifetime)));
		await WaitForAsync(() => ReferenceEquals(viewModel.Entry, entry));

		viewModel.Cancel();

		Assert.That(lifetime.DisposeCount, Is.EqualTo(1));
		Assert.That(viewModel.Entry, Is.Null);
	}

	[AvaloniaTest]
	public async Task SaveCommand_DisposesLoadedSavableLifetimeAndRequestsClose()
	{
		var lifetime = new TrackingSavableLifetime();
		var entry = new object();
		var closeRequested = false;
		var viewModel = new MainWindowViewModel();
		viewModel.RequestClose += (_, _) => closeRequested = true;
		viewModel.StartLoading(_ => Task.FromResult<(object? Entry, IDisposable? Lifetime)>((entry, lifetime)));
		await WaitForAsync(() => viewModel.SaveCommand.CanExecute(null));

		viewModel.SaveCommand.Execute(null);

		Assert.That(lifetime.SaveCount, Is.EqualTo(1));
		Assert.That(lifetime.DisposeCount, Is.EqualTo(1));
		Assert.That(viewModel.Entry, Is.Null);
		Assert.That(viewModel.CancelOnClose, Is.False);
		Assert.That(closeRequested, Is.True);
	}

	[AvaloniaTest]
	public async Task CancelCommand_DisposesLoadedLifetimeDisablesCloseCancelAndRequestsClose()
	{
		var lifetime = new TrackingLifetime();
		var entry = new object();
		var closeRequestCount = 0;
		var viewModel = new MainWindowViewModel();
		viewModel.RequestClose += (_, _) => closeRequestCount++;
		viewModel.StartLoading(_ => Task.FromResult<(object? Entry, IDisposable? Lifetime)>((entry, lifetime)));
		await WaitForAsync(() => ReferenceEquals(viewModel.Entry, entry));

		viewModel.CancelCommand.Execute(null);

		Assert.That(lifetime.DisposeCount, Is.EqualTo(1));
		Assert.That(viewModel.Entry, Is.Null);
		Assert.That(viewModel.CancelOnClose, Is.False);
		Assert.That(closeRequestCount, Is.EqualTo(1));
		Assert.That(viewModel.SaveCommand.CanExecute(null), Is.False);
	}

	[AvaloniaTest]
	public async Task SaveCommand_CanExecuteChangesWhenLifetimeLoadsAndDisposes()
	{
		var lifetime = new TrackingSavableLifetime();
		var entry = new object();
		var canExecuteChangedCount = 0;
		var viewModel = new MainWindowViewModel();
		viewModel.SaveCommand.CanExecuteChanged += (_, _) => canExecuteChangedCount++;

		viewModel.StartLoading(_ => Task.FromResult<(object? Entry, IDisposable? Lifetime)>((entry, lifetime)));
		await WaitForAsync(() => viewModel.SaveCommand.CanExecute(null));

		Assert.That(canExecuteChangedCount, Is.GreaterThanOrEqualTo(1));

		viewModel.Cancel();

		Assert.That(viewModel.SaveCommand.CanExecute(null), Is.False);
		Assert.That(canExecuteChangedCount, Is.GreaterThanOrEqualTo(2));
	}

	[AvaloniaTest]
	public async Task CancelBeforeLoaderCompletes_DisposesLateLifetimeAndDoesNotSetEntry()
	{
		var lifetime = new TrackingLifetime();
		var entry = new object();
		var loader = new TaskCompletionSource<(object? Entry, IDisposable? Lifetime)>();
		var viewModel = new MainWindowViewModel();

		viewModel.StartLoading(_ => loader.Task);
		viewModel.Cancel();
		loader.SetResult((entry, lifetime));

		await WaitForAsync(() => lifetime.DisposeCount == 1);

		Assert.That(viewModel.Entry, Is.Null);
		Assert.That(viewModel.SaveCommand.CanExecute(null), Is.False);
	}

	[AvaloniaTest]
	public async Task MainWindow_DataContextChangeUnsubscribesPreviousViewModelCloseHandler()
	{
		var oldViewModel = new MainWindowViewModel();
		var newViewModel = new MainWindowViewModel();
		var closeCount = 0;
		var window = new MainWindow
		{
			Width = 640,
			Height = 480,
			DataContext = oldViewModel,
		};
		window.Closed += (_, _) => closeCount++;

		try
		{
			window.Show();
			window.DataContext = newViewModel;

			oldViewModel.CancelCommand.Execute(null);
			await FlushUi();

			Assert.That(closeCount, Is.EqualTo(0));
			Assert.That(window.IsVisible, Is.True);

			newViewModel.CancelCommand.Execute(null);
			await WaitForAsync(() => closeCount == 1);
		}
		finally
		{
			if (window.IsVisible)
				window.Close();
		}
	}

	private static async Task WaitForAsync(Func<bool> condition)
	{
		for (var attempt = 0; attempt < 20; attempt++)
		{
			await FlushUi();
			if (condition())
				return;
		}

		Assert.Fail("Timed out waiting for view-model state.");
	}

	private static Task FlushUi() => Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background).GetTask();

	private class TrackingLifetime : IDisposable
	{
		public int DisposeCount { get; private set; }

		public void Dispose()
		{
			DisposeCount++;
		}
	}

	private sealed class TrackingSavableLifetime : TrackingLifetime, ISavableWork
	{
		public int SaveCount { get; private set; }

		public void Save()
		{
			SaveCount++;
		}
	}
}
