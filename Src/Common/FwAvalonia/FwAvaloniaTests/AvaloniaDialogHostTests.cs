// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaTests
{
	/// <summary>
	/// The dialog-host ownership/threading contract (Task 7): the host disposes the dialog body and the
	/// view-model after close, and the compact-density styles apply once (idempotent). The full modal
	/// <see cref="AvaloniaDialogHost.ShowModal"/> spins a real WinForms modal loop, which does not run under
	/// the headless Avalonia host; these cover the extracted, deterministic pieces of that contract.
	/// </summary>
	[TestFixture]
	public class AvaloniaDialogHostTests
	{
		private sealed class DisposableVm : IDialogViewModel, IDisposable
		{
			public int DisposeCount { get; private set; }
#pragma warning disable 67 // event is part of the contract; not raised in this test
			public event EventHandler<bool> CloseRequested;
#pragma warning restore 67
			public void Dispose() => DisposeCount++;
		}

		private sealed class PlainVm : IDialogViewModel
		{
#pragma warning disable 67
			public event EventHandler<bool> CloseRequested;
#pragma warning restore 67
		}

		private sealed class DisposableControl : Control, IDisposable
		{
			public int DisposeCount { get; private set; }
			public void Dispose() => DisposeCount++;
		}

		[Test]
		public void DisposeDialogResources_DisposesDisposableBodyAndViewModel()
		{
			var body = new DisposableControl();
			var vm = new DisposableVm();

			AvaloniaDialogHost.DisposeDialogResources(body, vm);

			Assert.That(body.DisposeCount, Is.EqualTo(1), "the host owns and disposes the dialog body");
			Assert.That(vm.DisposeCount, Is.EqualTo(1), "the VM is disposed if IDisposable (it owns its resources)");
		}

		[Test]
		public void DisposeDialogResources_NonDisposableViewModel_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => AvaloniaDialogHost.DisposeDialogResources(new Control(), new PlainVm()));
		}

		[Test]
		public void DisposeDialogResources_NullArguments_DoNotThrow()
		{
			Assert.DoesNotThrow(() => AvaloniaDialogHost.DisposeDialogResources(null, null));
		}

		[AvaloniaTest]
		public void CompactDialogStyles_Apply_IsIdempotent()
		{
			var body = new Border();
			Assert.That(body.Styles.Count, Is.EqualTo(0));

			CompactDialogStyles.Apply(body);
			var afterFirst = body.Styles.Count;
			Assert.That(afterFirst, Is.GreaterThan(0), "first Apply installs the compact styles");

			CompactDialogStyles.Apply(body);
			Assert.That(body.Styles.Count, Is.EqualTo(afterFirst),
				"a second Apply must not stack duplicate styles (genuinely idempotent)");
		}

		[AvaloniaTest]
		public void CompactDialogStyles_Apply_NullControl_DoesNotThrow()
		{
			Assert.DoesNotThrow(() => CompactDialogStyles.Apply(null));
		}
	}
}
