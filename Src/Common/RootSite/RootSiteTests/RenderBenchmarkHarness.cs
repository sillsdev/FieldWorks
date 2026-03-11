// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RenderVerification;
using SIL.LCModel;

namespace SIL.FieldWorks.Common.RootSites.RenderBenchmark
{
	/// <summary>
	/// Harness for rendering lexical entries offscreen and capturing timing/bitmap output.
	/// Uses DummyBasicView infrastructure with Views engine rendering for pixel-perfect validation.
	/// </summary>
	/// <remarks>
	/// Bitmap capture uses IVwDrawRootBuffered to render the RootBox directly to a GDI+ bitmap,
	/// ensuring accurate capture of Views engine content including styled text, selections, and
	/// complex multi-writing-system layouts. This approach bypasses WinForms DrawToBitmap which
	/// doesn't work correctly for Views controls.
	/// </remarks>
	public class RenderBenchmarkHarness : IDisposable
	{
		private const int CapturePaddingPx = 4;

		private readonly LcmCache m_cache;
		private readonly RenderScenario m_scenario;
		private readonly RenderEnvironmentValidator m_environmentValidator;
		private readonly List<TraceEvent> m_traceEvents = new List<TraceEvent>();
		private DummyBasicView m_view;
		private bool m_disposed;
		private double m_traceTimelineMs;

		// Cached GDI resources for offscreen layout (avoid per-call allocation).
		private Bitmap m_layoutBmp;
		private Graphics m_layoutGraphics;
		private IntPtr m_layoutHdc;
		private IVwGraphics m_layoutVwGraphics;

		/// <summary>
		/// Gets the last render timing result.
		/// </summary>
		public RenderTimingResult LastTiming { get; private set; }

		/// <summary>
		/// Gets the last captured bitmap (may be null if capture failed).
		/// </summary>
		public Bitmap LastCapture { get; private set; }

		/// <summary>
		/// Gets a snapshot of per-stage trace events captured for this harness instance.
		/// </summary>
		public IReadOnlyList<TraceEvent> TraceEvents => m_traceEvents.ToArray();

		/// <summary>
		/// Gets the environment hash for the current rendering context.
		/// </summary>
		public string EnvironmentHash => m_environmentValidator?.GetEnvironmentHash() ?? string.Empty;

		/// <summary>
		/// Initializes a new instance of the <see cref="RenderBenchmarkHarness"/> class.
		/// </summary>
		/// <param name="cache">The LCModel cache.</param>
		/// <param name="scenario">The render scenario to execute.</param>
		/// <param name="environmentValidator">Optional environment validator for deterministic checks.</param>
		public RenderBenchmarkHarness(LcmCache cache, RenderScenario scenario, RenderEnvironmentValidator environmentValidator = null)
		{
			m_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			m_scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
			m_environmentValidator = environmentValidator ?? new RenderEnvironmentValidator();
		}

		/// <summary>
		/// Executes a cold render (first render after view creation).
		/// </summary>
		/// <param name="width">View width in pixels.</param>
		/// <param name="height">View height in pixels.</param>
		/// <returns>The timing result for the cold render.</returns>
		public RenderTimingResult ExecuteColdRender(int width = 800, int height = 600)
		{
			ResetTraceEvents();
			DisposeView();

			var stopwatch = Stopwatch.StartNew();

			m_view = MeasureStage(
				"CreateView",
				() => CreateView(width, height),
				new Dictionary<string, string> { { "phase", "cold" } });

			MeasureStage(
				"MakeRoot",
				() => m_view.MakeRoot(m_scenario.RootObjectHvo, m_scenario.RootFlid, m_scenario.FragmentId),
				new Dictionary<string, string> { { "phase", "cold" } });

			MeasureStage(
				"PerformOffscreenLayout",
				() => PerformOffscreenLayout(width, height),
				new Dictionary<string, string> { { "phase", "cold" } });

			if (EnsureViewSizedToContent(width, height))
			{
				MeasureStage(
					"PerformOffscreenLayout",
					() => PerformOffscreenLayout(width, m_view.Height),
					new Dictionary<string, string> { { "phase", "cold-resized" } });
			}

			if (m_view.RootBox != null && (m_view.RootBox.Width <= 0 || m_view.RootBox.Height <= 0))
			{
				throw new InvalidOperationException($"[RenderBenchmarkHarness] RootBox dimensions are zero/negative after layout ({m_view.RootBox.Width}x{m_view.RootBox.Height}). View Size: {m_view.Width}x{m_view.Height}. Capture will be empty.");
			}

			stopwatch.Stop();

			LastTiming = new RenderTimingResult
			{
				ScenarioId = m_scenario.Id,
				IsColdRender = true,
				DurationMs = stopwatch.Elapsed.TotalMilliseconds,
				Timestamp = DateTime.UtcNow
			};

			return LastTiming;
		}

		/// <summary>
		/// Executes a warm render (subsequent render with existing view/cache).
		/// </summary>
		/// <returns>The timing result for the warm render.</returns>
		public RenderTimingResult ExecuteWarmRender()
		{
			if (m_view == null)
			{
				throw new InvalidOperationException("Must call ExecuteColdRender before ExecuteWarmRender.");
			}

			var stopwatch = Stopwatch.StartNew();

			// Force a full relayout to simulate warm render
			MeasureStage(
				"Reconstruct",
				() => m_view.RootBox?.Reconstruct(),
				new Dictionary<string, string> { { "phase", "warm" } });

			MeasureStage(
				"PerformOffscreenLayout",
				() => PerformOffscreenLayout(m_view.Width, m_view.Height),
				new Dictionary<string, string> { { "phase", "warm" } });

			if (EnsureViewSizedToContent(m_view.Width, m_view.Height))
			{
				MeasureStage(
					"PerformOffscreenLayout",
					() => PerformOffscreenLayout(m_view.Width, m_view.Height),
					new Dictionary<string, string> { { "phase", "warm-resized" } });
			}

			stopwatch.Stop();

			LastTiming = new RenderTimingResult
			{
				ScenarioId = m_scenario.Id,
				IsColdRender = false,
				DurationMs = stopwatch.Elapsed.TotalMilliseconds,
				Timestamp = DateTime.UtcNow
			};

			return LastTiming;
		}

		/// <summary>
		/// Performs layout using an offscreen graphics context matching the target bitmap format.
		/// This prevents dependency on the Control's window handle or screen DC.
		/// </summary>
		private void PerformOffscreenLayout(int width, int height)
		{
			if (m_view?.RootBox == null) return;

			// Use the same width the site reports to Reconstruct, so the
			// PATH-L1 layout guard can detect truly redundant calls.
			int layoutWidth = m_view.GetAvailWidth(m_view.RootBox);

			// PATH-L4: Cache the offscreen GDI resources across calls to
			// eliminate ~27ms per-call Bitmap/Graphics/HDC allocation overhead.
			// Layout itself takes <0.1ms when the PATH-L1 guard fires.
			if (m_layoutBmp == null || m_layoutBmp.Width != width || m_layoutBmp.Height != height)
			{
				DisposeLayoutResources();
				m_layoutBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
				m_layoutGraphics = Graphics.FromImage(m_layoutBmp);
				m_layoutHdc = m_layoutGraphics.GetHdc();
				m_layoutVwGraphics = VwGraphicsWin32Class.Create();
				((IVwGraphicsWin32)m_layoutVwGraphics).Initialize(m_layoutHdc);
			}

			m_view.RootBox.Layout(m_layoutVwGraphics, layoutWidth);
		}

		private bool EnsureViewSizedToContent(int width, int minimumHeight)
		{
			if (m_view?.RootBox == null)
				return false;

			int requiredHeight = Math.Max(minimumHeight, m_view.RootBox.Height + CapturePaddingPx);
			if (requiredHeight <= 0 || requiredHeight == m_view.Height)
				return false;

			ResizeHostedView(width, requiredHeight);
			return true;
		}

		private void ResizeHostedView(int width, int height)
		{
			if (m_view == null)
				return;

			var newSize = new Size(width, height);
			m_view.Size = newSize;

			if (m_view.Parent is Form form)
				form.ClientSize = newSize;
		}

		/// <summary>
		/// Captures the current view as a bitmap using the Views engine's rendering.
		/// </summary>
		/// <remarks>
		/// Uses IVwDrawRootBuffered to render the RootBox directly to a bitmap,
		/// bypassing DrawToBitmap which doesn't work correctly for Views controls.
		/// </remarks>
		/// <returns>The captured bitmap, or null if capture failed.</returns>
		public Bitmap CaptureViewBitmap()
		{
			if (m_view == null)
			{
				throw new InvalidOperationException("No view available. Call ExecuteColdRender first.");
			}

			if (m_view.RootBox == null)
			{
				throw new InvalidOperationException("RootBox not initialized. MakeRoot may have failed.");
			}

			try
			{
				var width = m_view.Width;
				var height = m_view.Height;

				Bitmap bitmap = null;
				MeasureStage(
					"PrepareToDraw",
					() => bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb),
					new Dictionary<string, string> { { "phase", "capture" } });

				// Create bitmap and get its Graphics/HDC
				using (var graphics = Graphics.FromImage(bitmap))
				{
					// Fill with white background first
					graphics.Clear(Color.White);

					MeasureStage(
						"DrawTheRoot",
						() =>
						{
							IntPtr hdc = graphics.GetHdc();
							try
							{
								var vdrb = new SIL.FieldWorks.Views.VwDrawRootBuffered();
								var clientRect = new Rect(0, 0, width, height);
								const uint whiteColor = 0x00FFFFFF;
								vdrb.DrawTheRoot(m_view.RootBox, hdc, clientRect, whiteColor, true, m_view);
							}
							finally
							{
								graphics.ReleaseHdc(hdc);
							}
						},
						new Dictionary<string, string> { { "phase", "capture" } });
				}

				LastCapture = bitmap;
				return bitmap;
			}
			catch (Exception ex)
			{
				Trace.TraceWarning($"[RenderBenchmarkHarness] View capture failed: {ex.Message}");
				return null;
			}
		}

		private void ResetTraceEvents()
		{
			m_traceEvents.Clear();
			m_traceTimelineMs = 0;
		}

		private void MeasureStage(string stage, Action action, Dictionary<string, string> context = null)
		{
			var stopwatch = Stopwatch.StartNew();
			action();
			stopwatch.Stop();
			RecordStage(stage, stopwatch.Elapsed.TotalMilliseconds, context);
		}

		private T MeasureStage<T>(string stage, Func<T> func, Dictionary<string, string> context = null)
		{
			var stopwatch = Stopwatch.StartNew();
			T result = func();
			stopwatch.Stop();
			RecordStage(stage, stopwatch.Elapsed.TotalMilliseconds, context);
			return result;
		}

		private void RecordStage(string stage, double durationMs, Dictionary<string, string> context)
		{
			m_traceEvents.Add(new TraceEvent
			{
				Stage = stage,
				StartTimeMs = m_traceTimelineMs,
				DurationMs = durationMs,
				Context = context
			});
			m_traceTimelineMs += durationMs;
		}

		/// <summary>
		/// Saves the last captured bitmap to the specified path.
		/// </summary>
		/// <param name="outputPath">The file path to save the bitmap.</param>
		/// <param name="format">The image format (default: PNG).</param>
		public void SaveCapture(string outputPath, ImageFormat format = null)
		{
			if (LastCapture == null)
			{
				throw new InvalidOperationException("No capture available. Call CaptureViewBitmap first.");
			}

			var directory = Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			LastCapture.Save(outputPath, format ?? ImageFormat.Png);
		}

		/// <summary>
		/// Validates that the current environment matches the expected deterministic settings.
		/// </summary>
		/// <param name="expectedHash">The expected environment hash.</param>
		/// <returns>True if the environment matches; otherwise, false.</returns>
		public bool ValidateEnvironment(string expectedHash)
		{
			return m_environmentValidator.Validate(expectedHash);
		}

		private DummyBasicView CreateView(int width, int height)
		{
			// Host in a Form to ensure valid layout context (handle, client rect, etc.)
			var form = new Form
			{
				FormBorderStyle = FormBorderStyle.None,
				ShowInTaskbar = false,
				ClientSize = new Size(width, height)
			};

			DummyBasicView view;

			switch (m_scenario.ViewType)
			{
				case RenderViewType.LexEntry:
					view = CreateLexEntryView(width, height);
					break;

				default: // Scripture
					view = CreateScriptureView(width, height);
					break;
			}

			// Ensure styles are available (both StVc and LexEntryVc rely on stylesheet)
			var ss = new SIL.LCModel.DomainServices.LcmStyleSheet();
			ss.Init(m_cache, m_cache.LangProject.Hvo, SIL.LCModel.LangProjectTags.kflidStyles);
			view.StyleSheet = ss;

			form.Controls.Add(view);
			form.CreateControl(); // Creates form handle and children handles

			// Force handle creation if not yet created (critical for DoLayout)
			if (!view.IsHandleCreated)
			{
				var h = view.Handle;
			}
			if (!view.IsHandleCreated)
				throw new InvalidOperationException("View handle failed to create.");

			return view;
		}

		private DummyBasicView CreateScriptureView(int width, int height)
		{
			var view = new GenericScriptureView(m_scenario.RootObjectHvo, m_scenario.RootFlid)
			{
				Cache = m_cache,
				Visible = true,
				Dock = DockStyle.None,
				Location = Point.Empty,
				Size = new Size(width, height)
			};
			view.RootFragmentId = m_scenario.FragmentId;
			return view;
		}

		private DummyBasicView CreateLexEntryView(int width, int height)
		{
			var view = new GenericLexEntryView(m_scenario.RootObjectHvo, m_scenario.RootFlid)
			{
				Cache = m_cache,
				Visible = true,
				Dock = DockStyle.None,
				Location = Point.Empty,
				Size = new Size(width, height),
				SimulateIfDataDoubleRender = m_scenario.SimulateIfDataDoubleRender
			};
			view.RootFragmentId = LexEntryVc.kFragEntry;
			return view;
		}

		private void DisposeView()
		{
			DisposeLayoutResources();
			if (m_view != null)
			{
				var form = m_view.Parent as Form;
				m_view.Dispose();
				m_view = null;
				form?.Dispose();
			}
		}

		private void DisposeLayoutResources()
		{
			if (m_layoutVwGraphics != null)
			{
				m_layoutVwGraphics.ReleaseDC();
				m_layoutVwGraphics = null;
			}
			if (m_layoutHdc != IntPtr.Zero && m_layoutGraphics != null)
			{
				m_layoutGraphics.ReleaseHdc(m_layoutHdc);
				m_layoutHdc = IntPtr.Zero;
			}
			m_layoutGraphics?.Dispose();
			m_layoutGraphics = null;
			m_layoutBmp?.Dispose();
			m_layoutBmp = null;
		}

		/// <summary>
		/// Releases all resources used by the harness.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases the unmanaged resources and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">True to release both managed and unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (m_disposed)
				return;

			if (disposing)
			{
				DisposeLayoutResources();
				DisposeView();
				LastCapture?.Dispose();
				LastCapture = null;
			}

			m_disposed = true;
		}
	}
}
