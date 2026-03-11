using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites.RenderBenchmark;
using SIL.FieldWorks.Common.RenderVerification;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Main benchmark suite that executes all scenarios and generates a timing report.
	/// Each scenario is timed and also checked against the committed pixel baselines
	/// so performance and correctness are validated together.
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
			if (m_results == null) m_results = new List<BenchmarkResult>();
			m_reportWriter = new RenderBenchmarkReportWriter(OutputDir);
			m_environmentValidator = new RenderEnvironmentValidator();

			if (!Directory.Exists(OutputDir))
				Directory.CreateDirectory(OutputDir);
		}

		[OneTimeTearDown]
		public void SuiteTeardown()
		{
			var run = new BenchmarkRun
			{
				Results = m_results,
				EnvironmentHash = m_environmentValidator?.GetEnvironmentHash() ?? "Unknown",
				Configuration = "Debug",
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
                FragmentId = m_frag,
                ViewType = scenarioConfig.ViewType,
                SimulateIfDataDoubleRender = scenarioConfig.SimulateIfDataDoubleRender
            };

			using (var harness = new RenderBenchmarkHarness(Cache, scenario, m_environmentValidator))
			{
				// 1. Cold Render
				var coldTiming = harness.ExecuteColdRender();

				// Validate the canonical cold-render output against the committed baseline.
				bool pixelPerfectPass;
				string mismatchDetails;
				string snapshotPath;
				using (var bitmap = harness.CaptureViewBitmap())
				{
					string directory = RenderBaselineVerifier.GetSourceFileDirectory();
					string name = $"RenderVerifyTests.VerifyScenario_{scenarioId}";
					var verification = RenderBaselineVerifier.Verify(bitmap, directory, name, scenarioId);

					pixelPerfectPass = verification.Passed;
					mismatchDetails = verification.FailureMessage;
					snapshotPath = verification.VerifiedPath;

					if (!pixelPerfectPass)
						TestContext.WriteLine($"[VERIFY] {mismatchDetails}");
				}

				// 2. Warm Render
				var warmTiming = harness.ExecuteWarmRender();

				// 3. Record Result
				var benchmarkResult = new BenchmarkResult
				{
					ScenarioId = scenarioId,
					ScenarioDescription = scenarioConfig.Description,
					ColdRenderMs = coldTiming.DurationMs,
					WarmRenderMs = warmTiming.DurationMs,
					PixelPerfectPass = pixelPerfectPass,
					MismatchDetails = mismatchDetails,
					SnapshotPath = snapshotPath,
					TraceEvents = new List<TraceEvent>(harness.TraceEvents)
				};

				m_results.Add(benchmarkResult);

				if (!pixelPerfectPass)
					Assert.Fail(mismatchDetails);
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
                TestContext.Error.WriteLine($"Error discovering scenarios: {ex.Message}");
                return new[] { "simple", "medium", "complex", "deep-nested", "custom-heavy" };
            }
		}
	}
}
