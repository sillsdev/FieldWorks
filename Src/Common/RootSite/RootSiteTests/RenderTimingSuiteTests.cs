using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites.RenderBenchmark;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Main benchmark suite that executes all scenarios and generates a timing report.
	/// </summary>
	[TestFixture]
	[Category("RenderBenchmark")]
	[Category("Performance")]
	public class RenderTimingSuiteTests : RenderBenchmarkTestsBase
	{
		private static List<BenchmarkResult> m_results;
		private RenderBenchmarkReportWriter m_reportWriter;
		private RenderEnvironmentValidator m_environmentValidator;

		private static readonly string OutputDir = Path.Combine(
			TestContext.CurrentContext.TestDirectory,
			"..", "..", "Output", "RenderBenchmarks");

		[OneTimeSetUp]
		public void SuiteSetup()
		{
			// base.FixtureSetup(); // Removed as RealDataTestsBase does not support OneTimeSetup
			if (m_results == null) m_results = new List<BenchmarkResult>();
			m_reportWriter = new RenderBenchmarkReportWriter(OutputDir);
			m_environmentValidator = new RenderEnvironmentValidator();

			// Ensure output exists
			if (!Directory.Exists(OutputDir))
				Directory.CreateDirectory(OutputDir);
		}

		[OneTimeTearDown]
		public void SuiteTeardown()
		{
			// Generate final report
			var run = new BenchmarkRun
			{
				Results = m_results,
				EnvironmentHash = m_environmentValidator?.GetEnvironmentHash() ?? "Unknown",
				Configuration = "Debug", // Assumption
                MachineName = Environment.MachineName
			};

			m_reportWriter?.WriteReport(run);
			TestContext.WriteLine($"Benchmark report written to: {OutputDir}");
            TestContext.WriteLine($"Summary: {Path.Combine(OutputDir, "summary.md")}");
		}

		[Test, TestCaseSource(nameof(GetScenarios))]
		public void RunBenchmark(string scenarioId)
		{
			// Load config
            var allScenarios = RenderScenarioDataBuilder.LoadFromFile();
            var scenarioConfig = allScenarios.FirstOrDefault(s => s.Id == scenarioId);
            Assert.IsNotNull(scenarioConfig, $"Scenario {scenarioId} not found in config");

            TestContext.WriteLine($"Running Scenario: {scenarioId} - {scenarioConfig.Description}");

			// Setup Data (creates the Scripture book within a UndoableUnitOfWork)
			using (var uow = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor,
				"Setup Scenario", "Undo Setup Scenario"))
			{
				SetupScenarioData(scenarioId);
				uow.RollBack = false;
			}

			// Configure Scenario
            var scenario = new RenderScenario
            {
                Id = scenarioConfig.Id,
                Description = scenarioConfig.Description,
                Tags = scenarioConfig.Tags,
                RootObjectHvo = m_hvoRoot,
                RootFlid = m_flidContainingTexts,
                FragmentId = m_frag
            };

            // Resolve snapshot path relative to source directory
            // The JSON path is relative to TestData folder usually.
            // scenarioConfig.ExpectedSnapshotPath like "TestData/RenderSnapshots/simple.png"
            // We want absolute path.

            // RenderScenarioDataBuilder.TestDataDirectory is where the json is.
            string sourceSnapshotPath = Path.Combine(RenderScenarioDataBuilder.TestDataDirectory, "..", scenarioConfig.ExpectedSnapshotPath);
            sourceSnapshotPath = Path.GetFullPath(sourceSnapshotPath);

            scenario.ExpectedSnapshotPath = sourceSnapshotPath;

			using (var harness = new RenderBenchmarkHarness(Cache, scenario, m_environmentValidator))
			{
				// 1. Cold Render
				var coldTiming = harness.ExecuteColdRender();

				// 2. Warm Render
				var warmTiming = harness.ExecuteWarmRender();

				// 3. Pixel Check
				var bitmap = harness.CaptureViewBitmap();

				// Bootstrap: Create snapshot if missing
				if (!File.Exists(scenario.ExpectedSnapshotPath))
				{
					TestContext.WriteLine($"[BOOTSTRAP] Creating snapshot for {scenarioId} at {scenario.ExpectedSnapshotPath}");
                    Directory.CreateDirectory(Path.GetDirectoryName(scenario.ExpectedSnapshotPath));
					bitmap.Save(scenario.ExpectedSnapshotPath);
				}

				var comparer = new RenderBitmapComparer();
                var result = comparer.CompareToBaseline(scenario.ExpectedSnapshotPath, bitmap);

                if (!result.IsMatch)
                {
                     string diffPath = Path.Combine(OutputDir, $"{scenarioId}-diff.png");
                     try {
                        comparer.SaveDiffImage(result, diffPath);
                        TestContext.WriteLine($"Mismatch! Diff saved to {diffPath}");
                     } catch (Exception ex) {
                        TestContext.WriteLine($"Failed to save diff image: {ex.Message}");
                     }
                }
                else
                {
                     TestContext.WriteLine("Pixel verification passed.");
                }

				// 4. Record Result
				var benchmarkResult = new BenchmarkResult
				{
					ScenarioId = scenarioId,
					ScenarioDescription = scenarioConfig.Description,
					ColdRenderMs = coldTiming.DurationMs,
					WarmRenderMs = warmTiming.DurationMs,
					PixelPerfectPass = result.IsMatch,
					MismatchDetails = result.MismatchReason,
					SnapshotPath = scenario.ExpectedSnapshotPath
				};

				m_results.Add(benchmarkResult);
			}
		}

		public static IEnumerable<string> GetScenarios()
		{
            try
            {
			    var scenarios = RenderScenarioDataBuilder.LoadFromFile();
                if (scenarios.Count == 0) return new[] { "simple" };
			    return scenarios.Select(s => s.Id);
            }
            catch (Exception ex)
            {
                // Fallback if file not found during discovery
                TestContext.Error.WriteLine($"Error discovering scenarios: {ex.Message}");
                return new[] { "simple", "medium", "complex", "deep-nested", "custom-heavy" };
            }
		}
	}
}
