// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;

namespace FwAvaloniaTests.VisualChecks
{
	/// <summary>
	/// Headless PNG snapshot harness: renders ANY Avalonia surface (dialog, region/detail view, or browse
	/// table) with the Skia-backed headless backend (<c>UseHeadlessDrawing=false</c> in <c>TestAppBuilder</c>)
	/// and saves a real frame to a gitignored ephemeral folder. Captures land in ONE FLAT folder with a
	/// surface-prefixed file name — <c>Output/Snapshots/&lt;Surface&gt;-&lt;NN&gt;-&lt;stage&gt;.png</c> (e.g.
	/// <c>Output/Snapshots/InsertEntry-01-initial.png</c>, <c>Output/Snapshots/Browse-05-selected.png</c>) — so
	/// every surface's stages sort together by name in one directory. The surface prefix is the snapshot name's
	/// leading segment (the text before the first '-') unless an explicit <c>surfaceOverride</c> is passed.
	///
	/// The PNG is ALWAYS produced so the agent (via the Read tool) and the user can subjectively judge whether
	/// the surface looks right — overlap, clipping, stray strikethrough, lost highlight, alignment, density —
	/// beyond what the DialogLayoutAssert crowding guardrail can assert. Pair the two: capture the PNG, then
	/// assert layout sanity.
	/// </summary>
	public static class DialogSnapshot
	{
		/// <summary>The ephemeral, gitignored folder all snapshots are written into (flat, one folder).</summary>
		public static string Folder => EnsureRoot();

		/// <summary>
		/// Renders <paramref name="surface"/> to <c>Output/Snapshots/&lt;name&gt;.png</c> and returns the full
		/// path. Captures are kept in ONE FLAT folder with a surface-prefixed file name (e.g.
		/// <c>"InsertEntry-01-initial"</c> → <c>Output/Snapshots/InsertEntry-01-initial.png</c>) so a person can
		/// browse every surface's stages together, sorted by name. <paramref name="surfaceOverride"/>, when given,
		/// is prepended as the surface prefix for a name that doesn't already carry one. If the surface is already
		/// a <see cref="Window"/> it is captured at its own size; otherwise it is hosted in a window sized
		/// <paramref name="width"/> x <paramref name="height"/>.
		/// </summary>
		public static string Capture(Control surface, string name, double width = 420, double height = 320,
			string surfaceOverride = null)
		{
			if (surface == null) throw new ArgumentNullException(nameof(surface));
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A snapshot name is required.", nameof(name));

			var window = surface as Window;
			if (window == null)
				window = new Window { Width = width, Height = height, Content = surface };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var fileName = string.IsNullOrWhiteSpace(surfaceOverride) || name.StartsWith(surfaceOverride + "-")
				? name
				: surfaceOverride + "-" + name;
			var path = Path.Combine(EnsureRoot(), Sanitize(fileName) + ".png");
			using (var frame = window.CaptureRenderedFrame())
			{
				if (frame == null)
					throw new InvalidOperationException(
						"CaptureRenderedFrame returned null — the headless app must use Skia drawing (UseHeadlessDrawing=false).");
				frame.Save(path);
			}
			return path;
		}

		private static string EnsureRoot()
		{
			var dir = Path.Combine(RepoRoot(), "Output", "Snapshots");
			Directory.CreateDirectory(dir);
			return dir;
		}

		// Walk up from the running test assembly to the repo/worktree root (the directory with build.ps1),
		// so snapshots land in the shared, gitignored Output folder regardless of the bin path.
		private static string RepoRoot()
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "build.ps1")))
				dir = dir.Parent;
			return dir?.FullName ?? AppContext.BaseDirectory;
		}

		private static string Sanitize(string name)
		{
			foreach (var c in Path.GetInvalidFileNameChars())
				name = name.Replace(c, '-');
			return name;
		}
	}
}
