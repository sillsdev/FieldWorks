// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Threading;

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>
	/// Crash guard for hosting Avalonia inside WinForms (16.1). Avalonia's MicroCom COM proxies
	/// capture the ambient <see cref="SynchronizationContext"/> at creation
	/// (MicroComProxyBase._synchronizationContext) and their FINALIZERS post the native Release
	/// back through it. When that post lands after the WinForms marshaling window is gone —
	/// project switch, window teardown, shutdown, or simply an idle-time GC afterwards —
	/// <c>WindowsFormsSynchronizationContext.Post</c> throws <see cref="InvalidOperationException"/>
	/// on the FINALIZER thread, which terminates the whole process:
	///   InvalidOperationException → Control.MarshaledInvoke → BeginInvoke
	///   → WindowsFormsSynchronizationContext.Post → MicroCom.Runtime.MicroComProxyBase.Finalize().
	/// Installed as the UI thread's ambient context BEFORE Avalonia initializes, this wrapper is
	/// what every proxy captures; it delegates to the real context but swallows POST marshal
	/// failures whose only victim would be a moot native Release (synchronous Send failures still
	/// surface — the caller is waiting on the result). WinForms will not displace it —
	/// InstallIfNeeded only replaces null/base-type contexts, never custom ones.
	/// </summary>
	public sealed class FinalizerSafeSynchronizationContext : SynchronizationContext
	{
		private readonly SynchronizationContext _inner;

		/// <summary>Public for tests; product code installs via <see cref="InstallOnCurrentThread"/>.</summary>
		public FinalizerSafeSynchronizationContext(SynchronizationContext inner)
		{
			_inner = inner ?? throw new ArgumentNullException(nameof(inner));
		}

		/// <summary>Wraps the current thread's ambient context (idempotent). Call on the UI thread.</summary>
		public static void InstallOnCurrentThread()
		{
			var current = Current;
			if (current is FinalizerSafeSynchronizationContext)
				return;
			SetSynchronizationContext(new FinalizerSafeSynchronizationContext(
				current ?? new System.Windows.Forms.WindowsFormsSynchronizationContext()));
		}

		public override void Post(SendOrPostCallback d, object state)
		{
			try
			{
				_inner.Post(d, state);
			}
			catch (InvalidOperationException)
			{
				// Marshaling window gone (ObjectDisposedException is a subtype) — the posted work,
				// a finalizer's native Release, is moot.
			}
			catch (InvalidAsynchronousStateException)
			{
				// Destination thread exited.
			}
		}

		// Send is NOT swallowed: the finalizer rationale above only covers Post (MicroCom proxy
		// finalizers post their native Release). Send is a synchronous call whose caller is
		// waiting on the result — silently skipping the callback would corrupt that caller's
		// state, so marshal failures surface to it.
		public override void Send(SendOrPostCallback d, object state) => _inner.Send(d, state);

		public override SynchronizationContext CreateCopy()
			=> new FinalizerSafeSynchronizationContext(_inner.CreateCopy());

		public override void OperationStarted() => _inner.OperationStarted();

		public override void OperationCompleted() => _inner.OperationCompleted();
	}
}
