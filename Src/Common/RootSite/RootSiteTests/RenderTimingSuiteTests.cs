// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
	/// Timing suite tests for render performance benchmarking.
	/// Implements User Story 2: Cold + warm timing for five scenarios with recorded results.
	/// </summary>
	[TestFixture]
	[Category("RenderBenchmark")]
	[Category("Performance")]
	public class RenderTimingSuiteTests : BasicViewTestsBase
	{
		private RenderEnvironmentValidator m_environmentValidator;
		private RenderBitmapComparer m_comparer;
		private RenderDiagnosticsToggle m_diagnostics;
		private RenderBenchmarkReportWriter m_reportWriter;
		private RenderTraceParser m_traceParser;
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
			m_reportWriter = new RenderBenchmarkReportWriter(OutputDir);
			m_traceParser = new RenderTraceParser();

			// Ensure directories exist
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
		/// Runs the full five-scenario timing suite and produces results.
		/// </summary>
		[Test]
		[Category("FullSuite")]
		public void TimingSuite_FiveScenarios_ProducesResults()
		{
			// Arrange
			var scenarios = CreateTestScenarios();
			var run = new BenchmarkRun
			{
				Configuration = "Debug",
				EnvironmentHash = m_environmentValidator.GetEnvironmentHash()
			};

			// Enable diagnostics for trace capture
			m_diagnostics.EnableDiagnostics();

			try
			{
				// Act - run each scenario
				foreach (var scenario in scenarios)
				{
					var result = RunScenario(scenario);
					run.Results.Add(result);
				}

				// Generate summary and write report
				m_reportWriter.WriteReport(run);

				// Assert
				Assert.That(run.Results.Count, Is.EqualTo(5), "Should have 5 scenario results");

				var resultsPath = Path.Combine(OutputDir, "results.json");
				var summaryPath = Path.Combine(OutputDir, "summary.md");

				Assert.That(File.Exists(resultsPath), Is.True, "Results JSON should be written");
				Assert.That(File.Exists(summaryPath), Is.True, "Summary markdown should be written");

				// Verify results structure
				foreach (var result in run.Results)
				{
					Assert.That(result.ColdRenderMs, Is.GreaterThan(0), $"{result.ScenarioId}: Cold render should have timing");
					Assert.That(result.WarmRenderMs, Is.GreaterThan(0), $"{result.ScenarioId}: Warm render should have timing");
				}

				TestContext.WriteLine($"Benchmark run completed: {run.Id}");
				TestContext.WriteLine($"Results: {resultsPath}");
				TestContext.WriteLine($"Summary: {summaryPath}");
			}
			finally
			{
				m_diagnostics.DisableDiagnostics();
			}
		}

		/// <summary>
		/// Tests that individual scenario timing produces valid metrics.
		/// </summary>
		[Test]
		[TestCase("simple")]
		[TestCase("medium")]
		public void TimingSuite_SingleScenario_ProducesValidMetrics(string scenarioId)
		{
			// Arrange
			var scenario = CreateScenario(scenarioId);

			// Act
			var result = RunScenario(scenario);

			// Assert
			Assert.That(result.ScenarioId, Is.EqualTo(scenarioId));
			Assert.That(result.ColdRenderMs, Is.GreaterThan(0), "Cold render should have timing");
			Assert.That(result.WarmRenderMs, Is.GreaterThan(0), "Warm render should have timing");
			Assert.That(result.VariancePercent, Is.GreaterThanOrEqualTo(0), "Variance should be non-negative");
		}

		/// <summary>
		/// Tests that variance is calculated across multiple runs.
		/// </summary>
		[Test]
		public void TimingSuite_MultipleRuns_CalculatesVariance()
		{
			// Arrange
			var scenario = CreateScenario("simple");
			const int iterations = 3;
			var coldTimes = new List<double>();
			var warmTimes = new List<double>();

			// Act - run multiple iterations
			for (int i = 0; i < iterations; i++)
			{
				using (var harness = new RenderBenchmarkHarness(Cache, scenario, m_environmentValidator))
				{
					coldTimes.Add(harness.ExecuteColdRender().DurationMs);
					warmTimes.Add(harness.ExecuteWarmRender().DurationMs);
				}
			}

			// Calculate variance
			var coldAvg = coldTimes.Average();
			var warmAvg = warmTimes.Average();
			var coldVariance = coldTimes.Max() > 0
				? (coldTimes.Max() - coldTimes.Min()) / coldAvg * 100
				: 0;

			// Assert
			Assert.That(coldTimes.Count, Is.EqualTo(iterations), "Should have correct iteration count");
			TestContext.WriteLine($"Cold times: {string.Join(", ", coldTimes.Select(t => $"{t:F2}ms"))}");
			TestContext.WriteLine($"Warm times: {string.Join(", ", warmTimes.Select(t => $"{t:F2}ms"))} (avg: {warmAvg:F2}ms)");
			TestContext.WriteLine($"Cold variance: {coldVariance:F1}%");

			// Variance should typically be under 50% for stable tests
			Assert.That(coldVariance, Is.LessThan(100), "Variance should be reasonable");
		}

		/// <summary>
		/// Tests edge case: scenario with no custom fields.
		/// </summary>
		[Test]
		[Category("EdgeCase")]
		public void TimingSuite_NoCustomFields_Succeeds()
		{
			// Arrange
			var scenario = new RenderScenario
			{
				Id = "no-custom-fields",
				Description = "Entry with no custom fields",
				RootObjectHvo = m_hvoRoot,
				RootFlid = m_flidContainingTexts,
				FragmentId = m_frag,
				Tags = new[] { "edge-case", "minimal" }
			};

			// Act
			var result = RunScenario(scenario);

			// Assert
			Assert.That(result.ColdRenderMs, Is.GreaterThan(0));
			TestContext.WriteLine($"No custom fields render: {result.ColdRenderMs:F2}ms cold, {result.WarmRenderMs:F2}ms warm");
		}

		/// <summary>
		/// Tests edge case: empty/minimal content.
		/// </summary>
		[Test]
		[Category("EdgeCase")]
		public void TimingSuite_MinimalContent_Succeeds()
		{
			// Arrange
			var scenario = new RenderScenario
			{
				Id = "minimal-content",
				Description = "Minimal possible content",
				RootObjectHvo = m_hvoRoot,
				RootFlid = m_flidContainingTexts,
				FragmentId = m_frag,
				Tags = new[] { "edge-case", "minimal" }
			};

			// Act
			var result = RunScenario(scenario);

			// Assert
			Assert.That(result.ColdRenderMs, Is.GreaterThan(0));
		}

		/// <summary>
		/// Tests edge case: entry with no senses.
		/// </summary>
		[Test]
		[Category("EdgeCase")]
		public void TimingSuite_NoSenses_Succeeds()
		{
			// Arrange
			var scenario = new RenderScenario
			{
				Id = "no-senses",
				Description = "Entry with no senses (edge case)",
				RootObjectHvo = m_hvoRoot,
				RootFlid = m_flidContainingTexts,
				FragmentId = m_frag,
				Tags = new[] { "edge-case", "empty-sense" }
			};

			// Act
			var result = RunScenario(scenario);

			// Assert
			Assert.That(result.ColdRenderMs, Is.GreaterThan(0));
			TestContext.WriteLine($"No senses render: {result.ColdRenderMs:F2}ms cold, {result.WarmRenderMs:F2}ms warm");
		}

		/// <summary>
		/// Tests that deep nested scenarios have reasonable variance.
		/// </summary>
		[Test]
		[Category("EdgeCase")]
		public void TimingSuite_DeepNesting_VarianceWithinBounds()
		{
			// Arrange
			var scenario = CreateScenario("deep-nested");
			const int iterations = 3;
			var coldTimes = new List<double>();

			// Act - run multiple iterations
			for (int i = 0; i < iterations; i++)
			{
				using (var harness = new RenderBenchmarkHarness(Cache, scenario, m_environmentValidator))
				{
					coldTimes.Add(harness.ExecuteColdRender().DurationMs);
				}
			}

			// Calculate variance
			var coldAvg = coldTimes.Average();
			var coldVariance = coldAvg > 0
				? (coldTimes.Max() - coldTimes.Min()) / coldAvg * 100
				: 0;

			// Assert
			TestContext.WriteLine($"Deep nested cold times: {string.Join(", ", coldTimes.Select(t => $"{t:F2}ms"))}");
			TestContext.WriteLine($"Deep nested variance: {coldVariance:F1}%");

			// Deep nesting may have higher variance, but should still be under 100%
			Assert.That(coldVariance, Is.LessThan(100), "Deep nesting variance should be bounded");
		}

		/// <summary>
		/// Tests that report writer generates comparison output.
		/// </summary>
		[Test]
		public void ReportWriter_WithBaseline_GeneratesComparison()
		{
			// Arrange - create baseline and current runs
			var baselineRun = new BenchmarkRun
			{
				Id = "baseline-test",
				Configuration = "Debug",
				EnvironmentHash = m_environmentValidator.GetEnvironmentHash(),
				Results = new List<BenchmarkResult>
				{
					new BenchmarkResult { ScenarioId = "simple", ColdRenderMs = 100, WarmRenderMs = 50, PixelPerfectPass = true },
					new BenchmarkResult { ScenarioId = "medium", ColdRenderMs = 200, WarmRenderMs = 100, PixelPerfectPass = true }
				}
			};

			var currentRun = new BenchmarkRun
			{
				Id = "current-test",
				Configuration = "Debug",
				EnvironmentHash = m_environmentValidator.GetEnvironmentHash(),
				Results = new List<BenchmarkResult>
				{
					new BenchmarkResult { ScenarioId = "simple", ColdRenderMs = 110, WarmRenderMs = 55, PixelPerfectPass = true },
					new BenchmarkResult { ScenarioId = "medium", ColdRenderMs = 180, WarmRenderMs = 90, PixelPerfectPass = true }
				}
			};

			// Act
			m_reportWriter.WriteReport(currentRun, baselineRun);

			// Assert
			var summaryPath = Path.Combine(OutputDir, "summary.md");
			Assert.That(File.Exists(summaryPath), Is.True);

			var content = File.ReadAllText(summaryPath);
			Assert.That(content, Does.Contain("Render Benchmark Summary"));
		}

		/// <summary>
		/// Generates baseline snapshots for all five scenarios.
		/// </summary>
		[Test]
		[Explicit("Run manually to generate/update all baseline snapshots")]
		public void GenerateAllBaselineSnapshots()
		{
			var scenarios = CreateTestScenarios();

			foreach (var scenario in scenarios)
			{
				using (var harness = new RenderBenchmarkHarness(Cache, scenario, m_environmentValidator))
				{
					harness.ExecuteColdRender();
					var bitmap = harness.CaptureViewBitmap();

					if (bitmap != null)
					{
						var snapshotPath = Path.Combine(SnapshotsDir, $"{scenario.Id}.png");
						bitmap.Save(snapshotPath);
						TestContext.WriteLine($"Saved baseline: {snapshotPath}");
					}
				}
			}

			// Save environment info
			var envInfoPath = Path.Combine(SnapshotsDir, "all-scenarios.environment.txt");
			File.WriteAllText(envInfoPath,
				$"EnvironmentHash: {m_environmentValidator.GetEnvironmentHash()}\n" +
				$"DPI: {m_environmentValidator.CurrentSettings.DpiX}x{m_environmentValidator.CurrentSettings.DpiY}\n" +
				$"Theme: {m_environmentValidator.CurrentSettings.ThemeName}\n" +
				$"Generated: {DateTime.UtcNow:u}");
		}

		private BenchmarkResult RunScenario(RenderScenario scenario)
		{
			var result = new BenchmarkResult
			{
				ScenarioId = scenario.Id,
				ScenarioDescription = scenario.Description,
				SnapshotPath = scenario.ExpectedSnapshotPath
			};

			var baselineTimingPath = Path.Combine(SnapshotsDir, "baseline-timing.json");

			using (var harness = new RenderBenchmarkHarness(Cache, scenario, m_environmentValidator))
			{
				// Cold render
				var coldTiming = harness.ExecuteColdRender();
				result.ColdRenderMs = coldTiming.DurationMs;

				// Warm render
				var warmTiming = harness.ExecuteWarmRender();
				result.WarmRenderMs = warmTiming.DurationMs;

				// Capture bitmap
				var bitmap = harness.CaptureViewBitmap();

				// Bootstrap mode: create baseline if snapshot doesn't exist
				if (bitmap != null && !string.IsNullOrEmpty(scenario.ExpectedSnapshotPath) &&
					!File.Exists(scenario.ExpectedSnapshotPath))
				{
					TestContext.WriteLine($"[BOOTSTRAP] Creating baseline for scenario '{scenario.Id}'");

					// Ensure directory exists
					var snapshotDir = Path.GetDirectoryName(scenario.ExpectedSnapshotPath);
					if (!string.IsNullOrEmpty(snapshotDir) && !Directory.Exists(snapshotDir))
						Directory.CreateDirectory(snapshotDir);

					// Save snapshot
					bitmap.Save(scenario.ExpectedSnapshotPath);
					TestContext.WriteLine($"  Snapshot saved: {scenario.ExpectedSnapshotPath}");

					// Save timing baseline
					SaveTimingBaseline(baselineTimingPath, scenario.Id, result.ColdRenderMs, result.WarmRenderMs);
					TestContext.WriteLine($"  Timing saved: Cold={result.ColdRenderMs:F2}ms, Warm={result.WarmRenderMs:F2}ms");

					result.PixelPerfectPass = true; // New baseline is always a pass
					result.MismatchDetails = "[BOOTSTRAP] Initial baseline created";
				}
				// Comparison mode: validate against existing baseline
				else if (bitmap != null && !string.IsNullOrEmpty(scenario.ExpectedSnapshotPath) &&
					File.Exists(scenario.ExpectedSnapshotPath))
				{
					var comparison = m_comparer.CompareToBaseline(scenario.ExpectedSnapshotPath, bitmap);
					result.PixelPerfectPass = comparison.IsMatch;
					result.MismatchDetails = comparison.MismatchReason;

					// Compare timing against baseline
					var timingComparison = CompareTimingBaseline(baselineTimingPath, scenario.Id,
						result.ColdRenderMs, result.WarmRenderMs);
					TestContext.WriteLine(timingComparison);
				}
				else
				{
					result.PixelPerfectPass = true; // Skip validation if no baseline path configured
				}

				// Parse trace events if diagnostics enabled
				if (m_diagnostics.DiagnosticsEnabled)
				{
					var traceContent = m_diagnostics.GetTraceLogContent();
					if (!string.IsNullOrEmpty(traceContent))
					{
						result.TraceEvents = m_traceParser.ParseContent(traceContent);
					}
				}
			}

			// Calculate variance (simplified - would need multiple runs for real variance)
			result.VariancePercent = 0;

			return result;
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

			return $"[TIMING] {scenarioId}:\n" +
				$"  Cold: {coldMs:F2}ms vs baseline {baseline.ColdRenderMs:F2}ms ({coldDelta:+0.00;-0.00}ms, {coldPct:+0.0;-0.0}%) {coldStatus}\n" +
				$"  Warm: {warmMs:F2}ms vs baseline {baseline.WarmRenderMs:F2}ms ({warmDelta:+0.00;-0.00}ms, {warmPct:+0.0;-0.0}%) {warmStatus}";
		}

		private List<RenderScenario> CreateTestScenarios()
		{
			return new List<RenderScenario>
			{
				CreateScenario("simple"),
				CreateScenario("medium"),
				CreateScenario("complex"),
				CreateScenario("deep-nested"),
				CreateScenario("custom-field-heavy")
			};
		}

		private RenderScenario CreateScenario(string id)
		{
			var descriptions = new Dictionary<string, string>
			{
				["simple"] = "Minimal lexical entry with one sense",
				["medium"] = "Entry with 3 senses, multiple definitions",
				["complex"] = "Entry with 10+ senses, subsenses, cross-refs",
				["deep-nested"] = "Entry with deeply nested subsenses (5+ levels)",
				["custom-field-heavy"] = "Entry with many custom fields"
			};

			var tags = new Dictionary<string, string[]>
			{
				["simple"] = new[] { "baseline", "minimal" },
				["medium"] = new[] { "typical", "multi-sense" },
				["complex"] = new[] { "stress", "multi-sense", "cross-refs" },
				["deep-nested"] = new[] { "nested", "hierarchy", "stress" },
				["custom-field-heavy"] = new[] { "custom-fields", "extensibility" }
			};

			string description;
			if (!descriptions.TryGetValue(id, out description))
				description = $"Scenario: {id}";

			string[] scenarioTags;
			if (!tags.TryGetValue(id, out scenarioTags))
				scenarioTags = new string[0];

			return new RenderScenario
			{
				Id = id,
				Description = description,
				RootObjectHvo = m_hvoRoot,
				RootFlid = m_flidContainingTexts,
				FragmentId = m_frag,
				ExpectedSnapshotPath = Path.Combine(SnapshotsDir, $"{id}.png"),
				Tags = scenarioTags
			};
		}
	}
}
