// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using WinFormsControl = System.Windows.Forms.Control;
using Form = System.Windows.Forms.Form;
using FormBorderStyle = System.Windows.Forms.FormBorderStyle;

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

		/// <summary>
		/// The UI-thread guard (the one cheaply-testable slice of the otherwise desktop-only modal path):
		/// <see cref="AvaloniaDialogHost.ShowModal"/> must fail fast with <see cref="InvalidOperationException"/>
		/// when the owner is a WinForms <see cref="WinFormsControl"/> whose <c>InvokeRequired</c> is true — i.e.
		/// the call is on the wrong thread for the owner's message loop. Modal hosting + Avalonia share the
		/// single WinForms UI thread during coexistence; touching them off that thread is a re-entrancy /
		/// cross-thread bug, so the guard runs before any windowing.
		///
		/// To make <c>InvokeRequired</c> true deterministically, the owner control's window handle is created
		/// on a dedicated worker thread (kept alive for the duration), then ShowModal is invoked from this test
		/// thread — a different thread than the one that owns the handle.
		/// </summary>
		[Test]
		public void ShowModal_OwnerOnAnotherThread_ThrowsInvalidOperation()
		{
			WinFormsControl owner = null;
			var handleCreated = new ManualResetEventSlim(false);
			var release = new ManualResetEventSlim(false);

			var ownerThread = new Thread(() =>
			{
				owner = new WinFormsControl();
				owner.CreateControl();      // forces handle creation, binding the handle to THIS thread
				var _ = owner.Handle;       // ensure the handle exists before we signal
				handleCreated.Set();
				release.Wait();             // keep the owning thread alive so the handle stays valid
				owner.Dispose();
			});
			ownerThread.IsBackground = true;
			ownerThread.SetApartmentState(ApartmentState.STA);
			ownerThread.Start();

			try
			{
				Assert.That(handleCreated.Wait(TimeSpan.FromSeconds(5)), Is.True, "owner handle was not created in time");
				Assert.That(owner.InvokeRequired, Is.True,
					"precondition: the owner's handle is on the worker thread, so the test thread requires Invoke");

				var ex = Assert.Throws<InvalidOperationException>(() =>
					AvaloniaDialogHost.ShowModal(owner, new Control(), new PlainVm(), "title"));
				Assert.That(ex.Message, Does.Contain("UI thread"),
					"the guard explains it must be called on the WinForms message-loop thread");
			}
			finally
			{
				release.Set();
				ownerThread.Join(TimeSpan.FromSeconds(5));
			}
		}

		// --- Sizing / min-size / size-persistence (Task: resizable ShowModal). ShowModal itself spins a real
		// modal loop (not headless-runnable), so these cover the extracted ApplySizing helper that ShowModal
		// delegates to: border style, min-size, and the get-hook that seeds the initial (remembered) size.
		// SANCTIONED EXCEPTION to the no-WinForms-Forms-in-tests rule (review, July 2026): these are bare
		// `new Form()` property bags — no designer tree, never shown, no message loop — and the subject
		// under test IS Form property manipulation (frame delta, FixedDialog min-size semantics), which a
		// fake would untest. App dialogs/designer Forms remain banned; test presenters for those. ---

		[Test]
		public void ApplySizing_Default_IsFixedDialog_WithFixedClientSize()
		{
			using (var form = new Form())
			{
				AvaloniaDialogHost.ApplySizing(form, width: 420, height: 320, resizable: false);

				Assert.That(form.FormBorderStyle, Is.EqualTo(FormBorderStyle.FixedDialog),
					"the legacy default stays a fixed dialog");
				Assert.That(form.ClientSize, Is.EqualTo(new Size(420, 320)));
				Assert.That(form.MinimumSize, Is.EqualTo(Size.Empty), "fixed dialogs get no min-size");
			}
		}

		[Test]
		public void ApplySizing_Resizable_SetsSizableBorder_AndMinSizeDefaultsToInitial()
		{
			using (var form = new Form())
			{
				AvaloniaDialogHost.ApplySizing(form, width: 500, height: 400, resizable: true);

				Assert.That(form.FormBorderStyle, Is.EqualTo(FormBorderStyle.Sizable));
				Assert.That(form.ClientSize, Is.EqualTo(new Size(500, 400)));
				// MinimumSize is an outer size >= the client minimum (window frame added when a handle is realized).
				Assert.That(form.MinimumSize.Width, Is.GreaterThanOrEqualTo(500));
				Assert.That(form.MinimumSize.Height, Is.GreaterThanOrEqualTo(400));
			}
		}

		[Test]
		public void ApplySizing_Resizable_HonorsExplicitMinSize()
		{
			using (var form = new Form())
			{
				AvaloniaDialogHost.ApplySizing(form, width: 600, height: 500, resizable: true,
					minWidth: 300, minHeight: 200);

				Assert.That(form.ClientSize, Is.EqualTo(new Size(600, 500)));
				Assert.That(form.MinimumSize.Width, Is.GreaterThanOrEqualTo(300));
				Assert.That(form.MinimumSize.Height, Is.GreaterThanOrEqualTo(200));
				Assert.That(form.MinimumSize.Width, Is.LessThan(600),
					"an explicit smaller minimum is honored, not bumped up to the initial size");
			}
		}

		[Test]
		public void ApplySizing_Resizable_InvokesGetHook_AndSeedsRememberedSize()
		{
			using (var form = new Form())
			{
				var hookCalls = 0;
				Func<Size?> getRemembered = () => { hookCalls++; return new Size(640, 480); };

				AvaloniaDialogHost.ApplySizing(form, width: 420, height: 320, resizable: true,
					getRememberedSize: getRemembered);

				Assert.That(hookCalls, Is.EqualTo(1), "the size get-hook is consulted exactly once");
				Assert.That(form.ClientSize, Is.EqualTo(new Size(640, 480)),
					"a remembered size seeds the initial client size in place of width/height");
			}
		}

		[Test]
		public void ApplySizing_Resizable_ClampsStaleRememberedSizeUpToMinimum()
		{
			using (var form = new Form())
			{
				// Remembered size is smaller than the minimum (e.g. min raised since last save).
				Func<Size?> getRemembered = () => new Size(100, 80);

				AvaloniaDialogHost.ApplySizing(form, width: 420, height: 320, resizable: true,
					minWidth: 300, minHeight: 250, getRememberedSize: getRemembered);

				Assert.That(form.ClientSize, Is.EqualTo(new Size(300, 250)),
					"a stale remembered size is clamped up to the client minimum");
			}
		}

		[Test]
		public void ApplySizing_NotResizable_IgnoresGetHookAndMinSize()
		{
			using (var form = new Form())
			{
				var hookCalls = 0;
				Func<Size?> getRemembered = () => { hookCalls++; return new Size(900, 700); };

				AvaloniaDialogHost.ApplySizing(form, width: 420, height: 320, resizable: false,
					minWidth: 200, minHeight: 150, getRememberedSize: getRemembered);

				Assert.That(hookCalls, Is.EqualTo(0), "the persistence hook is ignored for fixed dialogs");
				Assert.That(form.ClientSize, Is.EqualTo(new Size(420, 320)));
				Assert.That(form.MinimumSize, Is.EqualTo(Size.Empty));
			}
		}

		[Test]
		public void ApplySizing_NullForm_Throws()
		{
			Assert.Throws<ArgumentNullException>(
				() => AvaloniaDialogHost.ApplySizing(null, 100, 100, resizable: true));
		}

		// --- Nested-modal owner resolution (pointer-input bug fix). ShowModal itself spins a real modal
		// loop, so this covers the extracted decision ShowModal delegates to: prefer whatever form is truly
		// topmost/active right now over a possibly-stale caller-supplied owner. ---

		[Test]
		public void ResolveEffectiveOwner_ActiveFormPresent_PrefersActiveFormOverOwner()
		{
			using (var active = new Form())
			using (var staleOwner = new Form())
			{
				var effective = AvaloniaDialogHost.ResolveEffectiveOwner(active, staleOwner);

				Assert.That(effective, Is.SameAs(active),
					"a nested dialog (Feature Manager from Options) must show over whichever form is truly " +
					"topmost, not a stale owner captured earlier — otherwise the stale owner keeps input focus");
			}
		}

		[Test]
		public void ResolveEffectiveOwner_NoActiveForm_FallsBackToOwner()
		{
			using (var owner = new Form())
			{
				var effective = AvaloniaDialogHost.ResolveEffectiveOwner(null, owner);

				Assert.That(effective, Is.SameAs(owner),
					"with no active form (e.g. headless), the caller-supplied owner is used unchanged");
			}
		}

		[Test]
		public void ResolveEffectiveOwner_NeitherPresent_ReturnsNull()
		{
			Assert.That(AvaloniaDialogHost.ResolveEffectiveOwner(null, null), Is.Null);
		}
	}
}
