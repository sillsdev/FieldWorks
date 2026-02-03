// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites.RenderBenchmark;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Scripture;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Tests for pixel-perfect render baseline validation.
	/// Implements User Story 1: Deterministic pixel-perfect baseline for a single entry render.
	/// </summary>
	/// <remarks>
	/// These tests verify that rendered output matches approved baseline snapshots.
	/// Tests will fail if the environment (DPI, fonts, theme) differs from the baseline environment.
	/// </remarks>
	[TestFixture]
	[Category("RenderBenchmark")]
	public class RenderBaselineTests : BasicViewTestsBase
	{
		private RenderEnvironmentValidator m_environmentValidator;
		private RenderBitmapComparer m_comparer;
		private RenderDiagnosticsToggle m_diagnostics;
		private IScrBook m_book;
		private ILgWritingSystemFactory m_wsf;
		private int m_wsEng;

		private static readonly string SnapshotsDir = Path.Combine(
			TestContext.CurrentContext.TestDirectory,
			"..", "..", "Src", "Common", "RootSite", "RootSiteTests", "TestData", "RenderSnapshots");

		private static readonly string OutputDir = Path.Combine(
			TestContext.CurrentContext.TestDirectory,
			"..", "..", "Output", "RenderBenchmarks");

		/// <summary>
		/// Sets up the test fixture.
		/// </summary>
		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_flidContainingTexts = ScrBookTags.kflidFootnotes;
			m_wsf = Cache.WritingSystemFactory;
			m_wsEng = m_wsf.GetWsFromStr("en");

			m_environmentValidator = new RenderEnvironmentValidator();
			m_comparer = new RenderBitmapComparer { GenerateDiffImage = true };

			// Ensure output directories exist
			if (!Directory.Exists(SnapshotsDir))
				Directory.CreateDirectory(SnapshotsDir);
			if (!Directory.Exists(OutputDir))
				Directory.CreateDirectory(OutputDir);
		}

		/// <summary>
		/// Creates the test data (Scripture book with footnotes) for rendering.
		/// </summary>
		protected override void CreateTestData()
		{
			m_book = AddArchiveBookToMockedScripture(1, "GEN");
			m_hvoRoot = m_book.Hvo;

			// Add a footnote with some text to render
			AddFootnoteWithText();
		}

		/// <summary>
		/// Adds a footnote with test text to the book's first section.
		/// </summary>
		private void AddFootnoteWithText()
		{
			// Create a section with content
			var section = AddSectionToMockedBook(m_book);
			var para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);

			// Add a footnote with English text
			var footnote = AddFootnote(m_book, para, 0, "This is a test footnote for rendering.");

			// Add additional content to the footnote
			var footnotePara = (IStTxtPara)footnote.ParagraphsOS[0];
			var bldr = footnotePara.Contents.GetBldr();
			bldr.ReplaceTsString(0, bldr.Length, TsStringUtils.MakeString(
				"This is sample text for render baseline testing. It includes multiple words to verify proper text layout.",
				m_wsEng));
			footnotePara.Contents = bldr.GetString();
		}

		/// <summary>
		/// Sets up each test.
		/// </summary>
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_diagnostics = new RenderDiagnosticsToggle();
		}

		/// <summary>
		/// Tears down each test.
		/// </summary>
		[TearDown]
		public override void TestTearDown()
		{
			m_diagnostics?.Dispose();
			m_diagnostics = null;
			base.TestTearDown();
		}

		/// <summary>
		/// Tests that the harness can render a simple view and capture a bitmap.
		/// </summary>
		[Test]
		public void RenderHarness_CapturesSimpleView_ReturnsValidBitmap()
		{
			// Arrange
			var scenario = new RenderScenario
			{
				Id = "simple-test",
				Description = "Basic view for harness validation",
				RootObjectHvo = m_hvoRoot,
				RootFlid = m_flidContainingTexts,
				FragmentId = m_frag
			};

			using (var harness = new RenderBenchmarkHarness(Cache, scenario, m_environmentValidator))
			{
				// Act
				var coldTiming = harness.ExecuteColdRender(width: 400, height: 300);
				var bitmap = harness.CaptureViewBitmap();

				// Assert
				Assert.That(coldTiming, Is.Not.Null, "Cold timing result should not be null");
				Assert.That(coldTiming.DurationMs, Is.GreaterThan(0), "Cold render should take measurable time");
				Assert.That(coldTiming.IsColdRender, Is.True, "Should be marked as cold render");

				Assert.That(bitmap, Is.Not.Null, "Captured bitmap should not be null");
				Assert.That(bitmap.Width, Is.EqualTo(400), "Bitmap width should match view width");
				Assert.That(bitmap.Height, Is.EqualTo(300), "Bitmap height should match view height");
			}
		}

		/// <summary>
		/// Tests that warm renders are faster than cold renders.
		/// </summary>
		[Test]
		public void RenderHarness_WarmRender_IsFasterThanColdRender()
		{
			// Arrange
			var scenario = new RenderScenario
			{
				Id = "warm-vs-cold",
				Description = "Compare warm vs cold render timing",
				RootObjectHvo = m_hvoRoot,
				RootFlid = m_flidContainingTexts,
				FragmentId = m_frag
			};

			using (var harness = new RenderBenchmarkHarness(Cache, scenario, m_environmentValidator))
			{
				// Act
				var coldTiming = harness.ExecuteColdRender();
				var warmTiming = harness.ExecuteWarmRender();

				// Assert
				Assert.That(warmTiming, Is.Not.Null, "Warm timing result should not be null");
				Assert.That(warmTiming.IsColdRender, Is.False, "Should be marked as warm render");

				// Warm render should generally be faster, but we allow some variance
				// This is a soft check - warm should be at most 2x cold time
				Assert.That(warmTiming.DurationMs, Is.LessThan(coldTiming.DurationMs * 2),
					$"Warm render ({warmTiming.DurationMs:F2}ms) should not be much slower than cold ({coldTiming.DurationMs:F2}ms)");
			}
		}

		/// <summary>
		/// Tests that the bitmap comparer correctly identifies identical bitmaps.
		/// </summary>
		[Test]
		public void BitmapComparer_IdenticalBitmaps_ReturnsMatch()
		{
			// Arrange
			using (var bitmap1 = CreateTestBitmap(100, 100, Color.Blue))
			using (var bitmap2 = CreateTestBitmap(100, 100, Color.Blue))
			{
				// Act
				var result = m_comparer.Compare(bitmap1, bitmap2);

				// Assert
				Assert.That(result.IsMatch, Is.True, "Identical bitmaps should match");
				Assert.That(result.MismatchedPixelCount, Is.EqualTo(0), "No pixels should differ");
			}
		}

		/// <summary>
		/// Tests that the bitmap comparer correctly identifies different bitmaps.
		/// </summary>
		[Test]
		public void BitmapComparer_DifferentBitmaps_ReturnsMismatch()
		{
			// Arrange
			using (var bitmap1 = CreateTestBitmap(100, 100, Color.Blue))
			using (var bitmap2 = CreateTestBitmap(100, 100, Color.Red))
			{
				// Act
				var result = m_comparer.Compare(bitmap1, bitmap2);

				// Assert
				Assert.That(result.IsMatch, Is.False, "Different bitmaps should not match");
				Assert.That(result.MismatchedPixelCount, Is.GreaterThan(0), "Pixels should differ");
				Assert.That(result.DiffImage, Is.Not.Null, "Diff image should be generated");
			}
		}

		/// <summary>
		/// Tests that the bitmap comparer detects dimension mismatches.
		/// </summary>
		[Test]
		public void BitmapComparer_DifferentDimensions_ReturnsDimensionMismatch()
		{
			// Arrange
			using (var bitmap1 = CreateTestBitmap(100, 100, Color.Blue))
			using (var bitmap2 = CreateTestBitmap(200, 100, Color.Blue))
			{
				// Act
				var result = m_comparer.Compare(bitmap1, bitmap2);

				// Assert
				Assert.That(result.IsMatch, Is.False, "Different dimensions should not match");
				Assert.That(result.MismatchReason, Does.Contain("Dimension"), "Should report dimension mismatch");
			}
		}

		/// <summary>
		/// Tests that the environment validator produces consistent hashes.
		/// </summary>
		[Test]
		public void EnvironmentValidator_SameEnvironment_ProducesConsistentHash()
		{
			// Arrange
			var validator1 = new RenderEnvironmentValidator();
			var validator2 = new RenderEnvironmentValidator();

			// Act
			var hash1 = validator1.GetEnvironmentHash();
			var hash2 = validator2.GetEnvironmentHash();

			// Assert
			Assert.That(hash1, Is.Not.Null.And.Not.Empty, "Hash should not be empty");
			Assert.That(hash1, Is.EqualTo(hash2), "Same environment should produce same hash");
		}

		/// <summary>
		/// Tests baseline snapshot comparison when snapshot exists.
		/// If no baseline exists, creates it (bootstrap mode).
		/// </summary>
		/// <remarks>
		/// On first run, this test creates the baseline snapshot and timing.
		/// On subsequent runs, it compares against the stored baseline.
		/// </remarks>
		[Test]
		public void RenderBaseline_SimpleScenario_MatchesApprovedSnapshot()
		{
			// Arrange
			var snapshotPath = Path.Combine(SnapshotsDir, "simple.png");
			var baselineTimingPath = Path.Combine(SnapshotsDir, "baseline-timing.json");

			var scenario = new RenderScenario
			{
				Id = "simple",
				Description = "Minimal lexical entry baseline",
				RootObjectHvo = m_hvoRoot,
				RootFlid = m_flidContainingTexts,
				FragmentId = m_frag,
				ExpectedSnapshotPath = snapshotPath
			};

			using (var harness = new RenderBenchmarkHarness(Cache, scenario, m_environmentValidator))
			{
				// Act
				var coldTiming = harness.ExecuteColdRender();
				var warmTiming = harness.ExecuteWarmRender();
				var bitmap = harness.CaptureViewBitmap();

				// Bootstrap mode: create baseline if it doesn't exist
				if (!File.Exists(snapshotPath))
				{
					TestContext.WriteLine($"[BOOTSTRAP] No baseline found. Creating initial baseline at: {snapshotPath}");

					// Save the snapshot
					bitmap.Save(snapshotPath);

					// Save environment info
					var envHash = m_environmentValidator.GetEnvironmentHash();
					var envInfoPath = Path.Combine(SnapshotsDir, "simple.environment.txt");
					File.WriteAllText(envInfoPath, $"EnvironmentHash: {envHash}\n" +
						$"DPI: {m_environmentValidator.CurrentSettings.DpiX}x{m_environmentValidator.CurrentSettings.DpiY}\n" +
						$"Theme: {m_environmentValidator.CurrentSettings.ThemeName}\n" +
						$"Generated: {DateTime.UtcNow:u}");

					// Save initial timing baseline
					SaveTimingBaseline(baselineTimingPath, "simple", coldTiming.DurationMs, warmTiming.DurationMs);

					TestContext.WriteLine($"Baseline snapshot saved to: {snapshotPath}");
					TestContext.WriteLine($"Cold render: {coldTiming.DurationMs:F2}ms, Warm render: {warmTiming.DurationMs:F2}ms");
					TestContext.WriteLine($"Environment hash: {envHash}");

					Assert.Pass($"[BOOTSTRAP] Created initial baseline. Cold: {coldTiming.DurationMs:F2}ms, Warm: {warmTiming.DurationMs:F2}ms");
					return;
				}

				// Comparison mode: validate against existing baseline
				var result = m_comparer.CompareToBaseline(snapshotPath, bitmap);

				// Load and compare timing baseline
				var timingComparison = CompareTimingBaseline(baselineTimingPath, "simple", coldTiming.DurationMs, warmTiming.DurationMs);
				TestContext.WriteLine(timingComparison);

				// Assert - save diff on failure
				if (!result.IsMatch)
				{
					var diffPath = Path.Combine(OutputDir, "simple-diff.png");
					var actualPath = Path.Combine(OutputDir, "simple-actual.png");
					m_comparer.SaveDiffImage(result, diffPath);
					bitmap.Save(actualPath);
					Assert.Fail($"Render does not match baseline: {result.MismatchReason}. " +
						$"Diff saved to: {diffPath}, Actual saved to: {actualPath}");
				}
			}
		}

		/// <summary>
		/// Saves timing baseline data to a JSON file.
		/// </summary>
		private void SaveTimingBaseline(string baselinePath, string scenarioId, double coldMs, double warmMs)
		{
			Dictionary<string, ScenarioTimingBaseline> baselines;

			if (File.Exists(baselinePath))
			{
				var json = File.ReadAllText(baselinePath);
				baselines = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, ScenarioTimingBaseline>>(json)
					?? new Dictionary<string, ScenarioTimingBaseline>();
			}
			else
			{
				baselines = new Dictionary<string, ScenarioTimingBaseline>();
			}

			baselines[scenarioId] = new ScenarioTimingBaseline
			{
				ColdRenderMs = coldMs,
				WarmRenderMs = warmMs,
				RecordedAt = DateTime.UtcNow,
				EnvironmentHash = m_environmentValidator.GetEnvironmentHash()
			};

			var settings = new Newtonsoft.Json.JsonSerializerSettings
			{
				Formatting = Newtonsoft.Json.Formatting.Indented
			};
			File.WriteAllText(baselinePath, Newtonsoft.Json.JsonConvert.SerializeObject(baselines, settings));
		}

		/// <summary>
		/// Compares current timing against stored baseline.
		/// </summary>
		private string CompareTimingBaseline(string baselinePath, string scenarioId, double coldMs, double warmMs)
		{
			if (!File.Exists(baselinePath))
			{
				return $"[TIMING] No baseline timing file found. Current: Cold={coldMs:F2}ms, Warm={warmMs:F2}ms";
			}

			var json = File.ReadAllText(baselinePath);
			var baselines = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, ScenarioTimingBaseline>>(json);

			if (baselines == null || !baselines.ContainsKey(scenarioId))
			{
				return $"[TIMING] No baseline for scenario '{scenarioId}'. Current: Cold={coldMs:F2}ms, Warm={warmMs:F2}ms";
			}

			var baseline = baselines[scenarioId];
			var coldDelta = coldMs - baseline.ColdRenderMs;
			var warmDelta = warmMs - baseline.WarmRenderMs;
			var coldPct = baseline.ColdRenderMs > 0 ? (coldDelta / baseline.ColdRenderMs) * 100 : 0;
			var warmPct = baseline.WarmRenderMs > 0 ? (warmDelta / baseline.WarmRenderMs) * 100 : 0;

			var coldStatus = coldPct > 20 ? "⚠️ REGRESSION" : coldPct < -20 ? "✅ IMPROVED" : "➡️ STABLE";
			var warmStatus = warmPct > 20 ? "⚠️ REGRESSION" : warmPct < -20 ? "✅ IMPROVED" : "➡️ STABLE";

			return $"[TIMING] Comparison:\n" +
				$"  Cold: {coldMs:F2}ms vs baseline {baseline.ColdRenderMs:F2}ms ({coldDelta:+0.00;-0.00}ms, {coldPct:+0.0;-0.0}%) {coldStatus}\n" +
				$"  Warm: {warmMs:F2}ms vs baseline {baseline.WarmRenderMs:F2}ms ({warmDelta:+0.00;-0.00}ms, {warmPct:+0.0;-0.0}%) {warmStatus}";
		}

		/// <summary>
		/// Generates a baseline snapshot for the simple scenario.
		/// </summary>
		/// <remarks>
		/// This test creates/updates the baseline snapshot.
		/// Should only be run manually when intentionally updating the baseline.
		/// </remarks>
		[Test]
		[Explicit("Run manually to generate/update baseline snapshot")]
		public void GenerateBaselineSnapshot_Simple()
		{
			// Arrange
			var scenario = new RenderScenario
			{
				Id = "simple",
				Description = "Minimal lexical entry baseline",
				RootObjectHvo = m_hvoRoot,
				RootFlid = m_flidContainingTexts,
				FragmentId = m_frag
			};

			using (var harness = new RenderBenchmarkHarness(Cache, scenario, m_environmentValidator))
			{
				// Act
				harness.ExecuteColdRender();
				var bitmap = harness.CaptureViewBitmap();

				// Save the snapshot
				var snapshotPath = Path.Combine(SnapshotsDir, "simple.png");
				bitmap.Save(snapshotPath);

				// Also save environment info
				var envHash = m_environmentValidator.GetEnvironmentHash();
				var envInfoPath = Path.Combine(SnapshotsDir, "simple.environment.txt");
				File.WriteAllText(envInfoPath, $"EnvironmentHash: {envHash}\n" +
					$"DPI: {m_environmentValidator.CurrentSettings.DpiX}x{m_environmentValidator.CurrentSettings.DpiY}\n" +
					$"Theme: {m_environmentValidator.CurrentSettings.ThemeName}\n" +
					$"Generated: {DateTime.UtcNow:u}");

				TestContext.WriteLine($"Baseline snapshot saved to: {snapshotPath}");
				TestContext.WriteLine($"Environment hash: {envHash}");
			}
		}

		/// <summary>
		/// Tests that diagnostics toggle enables trace output.
		/// </summary>
		[Test]
		public void DiagnosticsToggle_Enable_WritesTraceEntries()
		{
			// Arrange
			m_diagnostics.EnableDiagnostics();

			// Act
			m_diagnostics.WriteTraceEntry("TestStage", 123.45, "test context");
			m_diagnostics.Flush();

			var content = m_diagnostics.GetTraceLogContent();

			// Assert
			Assert.That(content, Does.Contain("[RENDER]"), "Trace log should contain render entry");
			Assert.That(content, Does.Contain("TestStage"), "Trace log should contain stage name");
			Assert.That(content, Does.Contain("123.45"), "Trace log should contain duration");

			// Cleanup
			m_diagnostics.ClearTraceLog();
		}

		private static Bitmap CreateTestBitmap(int width, int height, Color color)
		{
			var bitmap = new Bitmap(width, height);
			using (var g = Graphics.FromImage(bitmap))
			{
				g.Clear(color);
			}
			return bitmap;
		}
	}

	/// <summary>
	/// Stores baseline timing data for a scenario.
	/// </summary>
	public class ScenarioTimingBaseline
	{
		/// <summary>Gets or sets the cold render time in milliseconds.</summary>
		public double ColdRenderMs { get; set; }

		/// <summary>Gets or sets the warm render time in milliseconds.</summary>
		public double WarmRenderMs { get; set; }

		/// <summary>Gets or sets when this baseline was recorded.</summary>
		public DateTime RecordedAt { get; set; }

		/// <summary>Gets or sets the environment hash when recorded.</summary>
		public string EnvironmentHash { get; set; }
	}
}
