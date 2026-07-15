// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the per-stage PNG harness (linked in via the csproj)

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// Shared realize/pump/capture idiom for headless dialog tests (formerly reimplemented verbatim as a
	/// private Show()/Capture() pair in each *DialogTests.cs fixture; PR #964 review §6 cleanup #3).
	/// Per-dialog VM/View construction stays in each fixture's own Show() — only the realize/pump/snapshot/
	/// assert sequence and the automation-id lookup are shared here.
	/// </summary>
	internal static class AvaloniaDialogTestHarness
	{
		/// <summary>
		/// Hosts <paramref name="view"/> in a new headless <see cref="Window"/>, pumps it to a realized layout,
		/// captures the named snapshot stage, and asserts no text-crowding defect. Set <paramref name="forceRenderTick"/>
		/// for dialogs whose initial paint needs a headless render-timer tick before the layout settles.
		/// </summary>
		public static Window Realize(Control view, double width, double height, string stageName, bool forceRenderTick = false)
		{
			var window = new Window { Content = view, Width = width, Height = height };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			if (forceRenderTick)
			{
				AvaloniaHeadlessPlatform.ForceRenderTimerTick();
				Dispatcher.UIThread.RunJobs();
			}
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			// Capture the realized stage BEFORE asserting, so the PNG exists for visual review even if the assert fails.
			DialogSnapshot.Capture(window, stageName);
			DialogLayoutAssert.AssertNoCrowding(view);
			return window;
		}

		/// <summary>
		/// Re-pumps an already-realized <paramref name="view"/> and snapshots a later interaction stage
		/// (post-selection, filtered, edited, etc.). Snapshots the view's hosting window, since the view
		/// already has a visual parent from a prior <see cref="Realize"/> call.
		/// </summary>
		public static void Recapture(Control view, string stageName)
		{
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture((Window)view.GetVisualRoot(), stageName);
		}

		public static T FindByAutomationId<T>(Control root, string id) where T : Control
			=> root.GetVisualDescendants().OfType<T>()
				.First(c => AutomationProperties.GetAutomationId(c) == id);

		/// <summary>
		/// Pumps a realized surface to a settled layout without snapshotting or asserting — for fixtures
		/// (e.g. plain-control tests) that capture/assert explicitly at each call site instead of inside Show().
		/// </summary>
		public static void Pump(Control surface)
		{
			surface.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
		}
	}
}
