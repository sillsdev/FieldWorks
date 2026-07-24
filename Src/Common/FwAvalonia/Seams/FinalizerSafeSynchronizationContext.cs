// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
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
		private static readonly TraceSwitch s_interopTrace =
			new TraceSwitch("FwAvaloniaHostInterop", "WinForms/Avalonia hosting interop diagnostics");

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
			catch (InvalidOperationException e)
			{
				// Marshaling window gone (ObjectDisposedException is a subtype) — for a MicroCom
				// finalizer's native Release the posted work is moot; anything else was collateral.
				ReportSwallowedPost(d, e);
			}
			catch (InvalidAsynchronousStateException e)
			{
				// Destination thread exited.
				ReportSwallowedPost(d, e);
			}
		}

		/// <summary>
		/// True when the callback is a MicroCom finalizer post (the crash class this wrapper exists
		/// for) — identified by the callback's declaring type living in the MicroCom runtime.
		/// Pinned against the referenced Avalonia assemblies by FinalizerSafeSyncContextTests so an
		/// Avalonia bump that relocates the namespace fails the build instead of reopening the crash.
		/// </summary>
		public static bool IsMicroComCallback(SendOrPostCallback d) =>
			d?.Method?.DeclaringType?.FullName?.StartsWith("MicroCom.", StringComparison.Ordinal) == true;

		/// <summary>
		/// Invoked when a NON-MicroCom post is dropped (e.g. an async continuation that will now
		/// never resume). Defaults to <see cref="Debug.Fail(string)"/> so Debug builds surface
		/// teardown bugs loudly; Release builds only log. Tests substitute a recorder.
		/// </summary>
		public static Action<string> NonMicroComDropHandler = message => Debug.Fail(message);

		// A swallowed MicroCom Release is expected and harmless (verbose trace). Anything else is a
		// DROPPED callback, so it logs as a warning and routes through the loud-in-Debug handler.
		private static void ReportSwallowedPost(SendOrPostCallback d, Exception e)
		{
			var target = d?.Method == null
				? "<unknown>"
				: (d.Method.DeclaringType?.FullName ?? "<global>") + "." + d.Method.Name;
			if (IsMicroComCallback(d))
			{
				Trace.WriteLineIf(s_interopTrace.TraceVerbose,
					$"[FinalizerSafeSyncContext] Swallowed moot MicroCom post ({e.GetType().Name}): {target}");
				return;
			}
			Trace.WriteLineIf(s_interopTrace.TraceWarning,
				$"[FinalizerSafeSyncContext] DROPPED post ({e.GetType().Name}): {target} — the callback will never run");
			NonMicroComDropHandler($"FinalizerSafeSynchronizationContext dropped a non-MicroCom post: {target} ({e.GetType().Name})");
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
