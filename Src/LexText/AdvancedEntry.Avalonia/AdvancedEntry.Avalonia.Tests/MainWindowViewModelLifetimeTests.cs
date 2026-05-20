using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using NUnit.Framework;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.ViewModels;

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

	private static async Task WaitForAsync(Func<bool> condition)
	{
		for (var attempt = 0; attempt < 20; attempt++)
		{
			await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background).GetTask();
			if (condition())
				return;
		}

		Assert.Fail("Timed out waiting for view-model state.");
	}

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
