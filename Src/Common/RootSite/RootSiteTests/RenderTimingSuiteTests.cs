using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites.RenderBenchmark;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Main benchmark suite that executes all scenarios and generates a timing report.
	/// Pixel-perfect validation is handled by RenderVerifyTests; this suite focuses
	/// on performance measurement and content-density sanity checks.
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

				// 2. Warm Render
				var warmTiming = harness.ExecuteWarmRender();

				// 3. Content density sanity check (pixel-perfect validation is in RenderVerifyTests)
				bool contentOk = true;
				string contentNote = null;
				using (var bitmap = harness.CaptureViewBitmap())
				{
					double density = MeasureContentDensity(bitmap);
					TestContext.WriteLine($"[CONTENT] Non-white density: {density:F2}%");
					if (density < 0.4)
					{
						contentOk = false;
						contentNote = $"Content density too low ({density:F2}%). Image may be blank.";
						TestContext.WriteLine($"[WARN] {contentNote}");
					}
				}

				// 4. Record Result
				var benchmarkResult = new BenchmarkResult
				{
					ScenarioId = scenarioId,
					ScenarioDescription = scenarioConfig.Description,
					ColdRenderMs = coldTiming.DurationMs,
					WarmRenderMs = warmTiming.DurationMs,
					PixelPerfectPass = contentOk,
					MismatchDetails = contentNote
				};

				m_results.Add(benchmarkResult);
			}
		}

		/// <summary>
		/// Measures the percentage of non-white pixels in the bitmap (sampled every 4th pixel).
		/// </summary>
		private static double MeasureContentDensity(Bitmap bitmap)
		{
			if (bitmap == null) return 0;
			long nonWhite = 0;
			for (int y = 0; y < bitmap.Height; y += 4)
			{
				for (int x = 0; x < bitmap.Width; x += 4)
				{
					Color pixel = bitmap.GetPixel(x, y);
					if (pixel.R < 250 || pixel.G < 250 || pixel.B < 250)
						nonWhite++;
				}
			}
			long sampled = (long)(bitmap.Width / 4) * (bitmap.Height / 4);
			return sampled > 0 ? (double)nonWhite / sampled * 100.0 : 0;
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
