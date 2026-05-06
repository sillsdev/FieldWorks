using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites.RenderBenchmark;
using SIL.FieldWorks.Common.RenderVerification;

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
			m_results = new List<BenchmarkResult>();
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
				MachineName = Environment.MachineName,
				FeatureFlags = GetActivePerfFlags()
			};

			m_reportWriter?.WriteReport(run);
			TestContext.WriteLine($"Benchmark report written to: {OutputDir}");
			TestContext.WriteLine($"Summary: {Path.Combine(OutputDir, "summary.md")}");
		}

		[Test, TestCaseSource(nameof(GetScenarios))]
		public void RunBenchmark(string scenarioId)
		{
			var execution = ExecuteScenarioAndCapture(
				scenarioId,
				includeWarmRender: true,
				environmentValidator: m_environmentValidator);

			TestContext.WriteLine($"Running Scenario: {scenarioId} - {execution.Scenario.Description}");

			if (!execution.Verification.Passed)
				TestContext.WriteLine($"[VERIFY] {execution.Verification.FailureMessage}");

			m_results.Add(new BenchmarkResult
			{
				ScenarioId = execution.Scenario.Id,
				ScenarioDescription = execution.Scenario.Description,
				ColdRenderMs = execution.ColdTiming.DurationMs,
				WarmRenderMs = execution.WarmTiming.DurationMs,
				ColdPerformOffscreenLayoutMs = SumStageDuration(execution.TraceEvents, "PerformOffscreenLayout", "cold"),
				WarmPerformOffscreenLayoutMs = SumStageDuration(execution.TraceEvents, "PerformOffscreenLayout", "warm"),
				PixelPerfectPass = execution.Verification.Passed,
				MismatchDetails = execution.Verification.FailureMessage,
				SnapshotPath = execution.Verification.VerifiedPath,
				TraceEvents = execution.TraceEvents
			});

			if (!execution.Verification.Passed)
				Assert.Fail(execution.Verification.FailureMessage);
		}

		public static IEnumerable<string> GetScenarios()
		{
			return GetConfiguredScenarioIds("simple", "medium", "complex", "deep-nested", "custom-heavy");
		}

		private static Dictionary<string, string> GetActivePerfFlags()
		{
			return new Dictionary<string, string>
			{
				{ "FW_PERF_P125_PATH1", GetPerfFlagState("FW_PERF_P125_PATH1") },
				{ "FW_PERF_P125_PATH2", GetPerfFlagState("FW_PERF_P125_PATH2") },
				{ "FW_PERF_P125_PATH5", GetPerfFlagState("FW_PERF_P125_PATH5") }
			};
		}

		private static string GetPerfFlagState(string variableName)
		{
			var value = Environment.GetEnvironmentVariable(variableName);
			if (!string.IsNullOrEmpty(value))
				return value;

			if (string.Equals(variableName, "FW_PERF_P125_PATH5", StringComparison.OrdinalIgnoreCase))
				return "not-implemented";

			return "default-on";
		}

		private static double SumStageDuration(IEnumerable<TraceEvent> traceEvents, string stage, string phasePrefix)
		{
			if (traceEvents == null)
				return 0;

			double total = 0;
			foreach (var traceEvent in traceEvents)
			{
				if (!string.Equals(traceEvent.Stage, stage, StringComparison.OrdinalIgnoreCase))
					continue;

				if (traceEvent.Context == null || !traceEvent.Context.TryGetValue("phase", out var phase))
					continue;

				if (phase.StartsWith(phasePrefix, StringComparison.OrdinalIgnoreCase))
					total += traceEvent.DurationMs;
			}

			return total;
		}
	}
}
