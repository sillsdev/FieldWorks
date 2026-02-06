// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;
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
		private readonly LcmCache m_cache;
		private readonly RenderScenario m_scenario;
		private readonly RenderEnvironmentValidator m_environmentValidator;
		private DummyBasicView m_view;
		private bool m_disposed;

		/// <summary>
		/// Gets the last render timing result.
		/// </summary>
		public RenderTimingResult LastTiming { get; private set; }

		/// <summary>
		/// Gets the last captured bitmap (may be null if capture failed).
		/// </summary>
		public Bitmap LastCapture { get; private set; }

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
			DisposeView();

			var stopwatch = Stopwatch.StartNew();

			m_view = CreateView(width, height);
			m_view.MakeRoot(m_scenario.RootObjectHvo, m_scenario.RootFlid, m_scenario.FragmentId);
			PerformOffscreenLayout(width, height);

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
			m_view.RootBox?.Reconstruct();
			PerformOffscreenLayout(m_view.Width, m_view.Height);

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

			// Create a temp bitmap to get a strictly compatible HDC
			using (var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
			using (var g = Graphics.FromImage(bmp))
			{
				IntPtr hdc = g.GetHdc();
				try
				{
					// Use VwGraphicsWin32 directly to drive the layout
					IVwGraphics vwGraphics = VwGraphicsWin32Class.Create();
					((IVwGraphicsWin32)vwGraphics).Initialize(hdc);
					try
					{
						m_view.RootBox.Layout(vwGraphics, width);
					}
					finally
					{
						vwGraphics.ReleaseDC();
					}
				}
				finally
				{
					g.ReleaseHdc(hdc);
				}
			}
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

				// Create bitmap and get its Graphics/HDC
				var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
				using (var graphics = Graphics.FromImage(bitmap))
				{
					// Fill with white background first
					graphics.Clear(Color.White);

					IntPtr hdc = graphics.GetHdc();
					try
					{
						// Create the draw buffer and render the root box directly
						var vdrb = new SIL.FieldWorks.Views.VwDrawRootBuffered();
						var clientRect = new Rect(0, 0, width, height);

						// Use white background (BGR format: 0x00FFFFFF)
						const uint whiteColor = 0x00FFFFFF;

						vdrb.DrawTheRoot(m_view.RootBox, hdc, clientRect, whiteColor, true, m_view);
					}
					finally
					{
						graphics.ReleaseHdc(hdc);
					}
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
			if (m_view != null)
			{
				var form = m_view.Parent as Form;
				m_view.Dispose();
				m_view = null;
				form?.Dispose();
			}
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
				DisposeView();
				LastCapture?.Dispose();
				LastCapture = null;
			}

			m_disposed = true;
		}
	}

	/// <summary>
	/// Represents the timing result for a single render operation.
	/// </summary>
	public class RenderTimingResult
	{
		/// <summary>Gets or sets the scenario identifier.</summary>
		public string ScenarioId { get; set; }

		/// <summary>Gets or sets whether this was a cold render.</summary>
		public bool IsColdRender { get; set; }

		/// <summary>Gets or sets the render duration in milliseconds.</summary>
		public double DurationMs { get; set; }

		/// <summary>Gets or sets the timestamp of the render.</summary>
		public DateTime Timestamp { get; set; }
	}

	/// <summary>
	/// Specifies which view constructor pipeline a scenario exercises.
	/// </summary>
	public enum RenderViewType
	{
		/// <summary>Scripture view (StVc / GenericScriptureVc).</summary>
		Scripture,

		/// <summary>Lexical entry view (LexEntryVc with nested senses).</summary>
		LexEntry
	}

	/// <summary>
	/// Represents a render scenario configuration.
	/// </summary>
	public class RenderScenario
	{
		/// <summary>Gets or sets the unique scenario identifier.</summary>
		public string Id { get; set; }

		/// <summary>Gets or sets the human-readable description.</summary>
		public string Description { get; set; }

		/// <summary>Gets or sets the root object HVO for the view.</summary>
		public int RootObjectHvo { get; set; }

		/// <summary>Gets or sets the root field ID.</summary>
		public int RootFlid { get; set; }

		/// <summary>Gets or sets the fragment ID for the view constructor.</summary>
		public int FragmentId { get; set; } = 1;

		/// <summary>Gets or sets the path to the expected snapshot image.</summary>
		public string ExpectedSnapshotPath { get; set; }

		/// <summary>Gets or sets category tags for filtering.</summary>
		public string[] Tags { get; set; } = Array.Empty<string>();

		/// <summary>
		/// Gets or sets the view type (Scripture or LexEntry).
		/// Determines which view constructor pipeline is used for rendering.
		/// </summary>
		public RenderViewType ViewType { get; set; } = RenderViewType.Scripture;

		/// <summary>
		/// Gets or sets whether to simulate the XmlVc ifdata double-render pattern.
		/// Only applies to <see cref="RenderViewType.LexEntry"/> scenarios.
		/// </summary>
		public bool SimulateIfDataDoubleRender { get; set; }
	}
}
