// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Avalonia;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Single, process-wide guard that initializes the in-process Avalonia runtime exactly once. Avalonia
	/// throws if <c>SetupWithoutStarting</c> runs more than once, so every WinForms entry point that hosts
	/// Avalonia content (the region host <see cref="AvaloniaRegionHostControl"/> and the dialog host
	/// <see cref="AvaloniaDialogHost"/>) must funnel through here rather than keeping its own guard.
	/// </summary>
	public static class FwAvaloniaRuntime
	{
		private static readonly object s_gate = new object();
		private static bool s_initialized;

		/// <summary>
		/// Test-only hook that lets a test assembly substitute the <see cref="AppBuilder"/> used by
		/// <see cref="EnsureInitialized"/> — without this production DLL referencing Avalonia.Headless.
		/// xWorks integration tests that drive the product surface (RecordEditView/RecordBrowseView/…)
		/// otherwise initialize the REAL Win32 Avalonia platform process-wide, so any flyout/dialog/popup
		/// becomes a real on-screen OS window that flashes and can steal keypresses. A test
		/// <c>[SetUpFixture]</c> sets this to a headless builder before any test runs; production leaves it
		/// null and behavior is identical to calling <see cref="FwAvaloniaHost.BuildAvaloniaApp"/> directly.
		/// Only honored on the first (winning) <see cref="EnsureInitialized"/> call — once the runtime is
		/// set up it cannot be re-platformed, so this must be set before the first host is constructed.
		/// </summary>
		public static Func<AppBuilder> AppBuilderOverride { get; set; }

		/// <summary>True once <see cref="EnsureInitialized"/> has set up the Avalonia runtime.</summary>
		public static bool IsInitialized => s_initialized;

		/// <summary>Idempotently sets up the Avalonia app for in-process net48 hosting.</summary>
		public static void EnsureInitialized()
		{
			if (s_initialized)
				return;

			lock (s_gate)
			{
				if (s_initialized)
					return;

				FinalizerSafeSynchronizationContext.InstallOnCurrentThread();
				var builder = AppBuilderOverride?.Invoke() ?? FwAvaloniaHost.BuildAvaloniaApp();
				builder.SetupWithoutStarting();
				s_initialized = true;
			}
		}
	}
}
