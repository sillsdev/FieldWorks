// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
				FwAvaloniaHost.BuildAvaloniaApp().SetupWithoutStarting();
				s_initialized = true;
			}
		}
	}
}
