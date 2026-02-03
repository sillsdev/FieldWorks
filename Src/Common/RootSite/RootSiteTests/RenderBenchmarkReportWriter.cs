// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.Common.RootSites.RenderBenchmark
{
	/// <summary>
	/// Generates summary reports for benchmark runs.
	/// Outputs results in JSON and Markdown formats.
	/// </summary>
	public class RenderBenchmarkReportWriter
	{
		private readonly RenderBenchmarkComparer m_comparer;
		private readonly RenderTraceParser m_traceParser;

		/// <summary>
		/// Gets or sets the output directory for reports.
		/// </summary>
		public string OutputDirectory { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RenderBenchmarkReportWriter"/> class.
		/// </summary>
		/// <param name="outputDirectory">The output directory for reports.</param>
		public RenderBenchmarkReportWriter(string outputDirectory = null)
		{
			OutputDirectory = outputDirectory ?? RenderDiagnosticsToggle.DefaultOutputDirectory;
			m_comparer = new RenderBenchmarkComparer();
			m_traceParser = new RenderTraceParser();

			if (!Directory.Exists(OutputDirectory))
			{
				Directory.CreateDirectory(OutputDirectory);
			}
		}

		/// <summary>
		/// Writes a complete benchmark run to JSON and Markdown files.
		/// </summary>
		/// <param name="run">The benchmark run to write.</param>
		/// <param name="baselineRun">Optional baseline run for comparison.</param>
		public void WriteReport(BenchmarkRun run, BenchmarkRun baselineRun = null)
		{
			if (run == null)
				throw new ArgumentNullException(nameof(run));

			// Generate summary if not present
			if (run.Summary == null)
			{
				run.Summary = GenerateSummary(run);
			}

			// Compare to baseline if provided
			BenchmarkComparisonResult comparison = null;
			if (baselineRun != null)
			{
				comparison = m_comparer.Compare(baselineRun, run);
				run.Summary.HasRegressions = comparison.HasRegressions;
				run.Summary.Regressions = comparison.Regressions;
			}

			// Write JSON results
			var jsonPath = Path.Combine(OutputDirectory, "results.json");
			run.SaveToFile(jsonPath);

			// Write Markdown summary
			var markdownPath = Path.Combine(OutputDirectory, "summary.md");
			WriteSummaryMarkdown(run, comparison, markdownPath);
		}

		/// <summary>
		/// Generates an analysis summary for a benchmark run.
		/// </summary>
		/// <param name="run">The benchmark run.</param>
		/// <returns>The generated summary.</returns>
		public AnalysisSummary GenerateSummary(BenchmarkRun run)
		{
			var summary = new AnalysisSummary
			{
				RunId = run.Id,
				TotalScenarios = run.Results.Count,
				PassingScenarios = run.Results.Count(r => r.PixelPerfectPass),
				FailingScenarios = run.Results.Count(r => !r.PixelPerfectPass)
			};

			// Calculate averages
			if (run.Results.Any())
			{
				summary.AverageColdRenderMs = run.Results.Average(r => r.ColdRenderMs);
				summary.AverageWarmRenderMs = run.Results.Average(r => r.WarmRenderMs);
			}

			// Aggregate trace events for top contributors
			var allTraceEvents = run.Results
				.Where(r => r.TraceEvents != null)
				.SelectMany(r => r.TraceEvents)
				.ToList();

			if (allTraceEvents.Any())
			{
				summary.TopContributors = m_traceParser.GetTopContributors(allTraceEvents, count: 5);
			}

			// Generate recommendations
			summary.Recommendations = GenerateRecommendations(run, summary);

			return summary;
		}

		/// <summary>
		/// Writes the summary in Markdown format.
		/// </summary>
		/// <param name="run">The benchmark run.</param>
		/// <param name="comparison">Optional comparison result.</param>
		/// <param name="outputPath">The output file path.</param>
		public void WriteSummaryMarkdown(BenchmarkRun run, BenchmarkComparisonResult comparison, string outputPath)
		{
			var sb = new StringBuilder();

			sb.AppendLine("# Render Benchmark Summary");
			sb.AppendLine();
			sb.AppendLine($"**Run ID**: {run.Id}");
			sb.AppendLine($"**Timestamp**: {run.RunAt:u}");
			sb.AppendLine($"**Machine**: {run.MachineName}");
			sb.AppendLine($"**Configuration**: {run.Configuration ?? "Debug"}");
			sb.AppendLine($"**Environment Hash**: `{run.EnvironmentHash}`");
			sb.AppendLine();

			// Overall Status
			var summary = run.Summary;
			if (summary != null)
			{
				var statusIcon = summary.FailingScenarios == 0 ? "✅" : "⚠️";
				sb.AppendLine($"## Status {statusIcon}");
				sb.AppendLine();
				sb.AppendLine($"- **Total Scenarios**: {summary.TotalScenarios}");
				sb.AppendLine($"- **Passing**: {summary.PassingScenarios}");
				sb.AppendLine($"- **Failing**: {summary.FailingScenarios}");
				sb.AppendLine($"- **Avg Cold Render**: {summary.AverageColdRenderMs:F2}ms");
				sb.AppendLine($"- **Avg Warm Render**: {summary.AverageWarmRenderMs:F2}ms");
				sb.AppendLine();
			}

			// Regression Status
			if (comparison != null)
			{
				if (comparison.HasRegressions)
				{
					sb.AppendLine("## ⚠️ Regressions Detected");
					sb.AppendLine();
					foreach (var reg in comparison.Regressions)
					{
						sb.AppendLine($"- **{reg.ScenarioId}** ({reg.Metric}): {reg.BaselineValue:F2}ms → {reg.CurrentValue:F2}ms (+{reg.RegressionPercent:F1}%)");
					}
					sb.AppendLine();
				}

				if (comparison.HasImprovements)
				{
					sb.AppendLine("## ✅ Improvements");
					sb.AppendLine();
					foreach (var imp in comparison.Improvements)
					{
						sb.AppendLine($"- **{imp.ScenarioId}** ({imp.Metric}): {imp.BaselineValue:F2}ms → {imp.CurrentValue:F2}ms ({imp.RegressionPercent:F1}%)");
					}
					sb.AppendLine();
				}
			}

			// Scenario Details Table
			sb.AppendLine("## Scenario Results");
			sb.AppendLine();
			sb.AppendLine("| Scenario | Cold (ms) | Warm (ms) | Pixel Pass | Variance |");
			sb.AppendLine("|----------|-----------|-----------|------------|----------|");

			foreach (var result in run.Results.OrderBy(r => r.ScenarioId))
			{
				var passIcon = result.PixelPerfectPass ? "✅" : "❌";
				sb.AppendLine($"| {result.ScenarioId} | {result.ColdRenderMs:F2} | {result.WarmRenderMs:F2} | {passIcon} | {result.VariancePercent:F1}% |");
			}
			sb.AppendLine();

			// Top Contributors
			if (summary?.TopContributors?.Any() == true)
			{
				sb.AppendLine("## Top Time Contributors");
				sb.AppendLine();
				sb.AppendLine("| Stage | Avg Duration (ms) | Share % |");
				sb.AppendLine("|-------|-------------------|---------|");

				foreach (var contributor in summary.TopContributors)
				{
					sb.AppendLine($"| {contributor.Stage} | {contributor.AverageDurationMs:F2} | {contributor.SharePercent:F1}% |");
				}
				sb.AppendLine();
			}

			// Recommendations
			if (summary?.Recommendations?.Any() == true)
			{
				sb.AppendLine("## Recommendations");
				sb.AppendLine();
				foreach (var rec in summary.Recommendations)
				{
					sb.AppendLine($"- {rec}");
				}
				sb.AppendLine();
			}

			// Write to file
			File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
		}

		/// <summary>
		/// Writes a comparison report between baseline and current run.
		/// </summary>
		/// <param name="baselineFilePath">Path to the baseline results JSON.</param>
		/// <param name="current">The current benchmark run.</param>
		/// <param name="outputPath">The output path for the comparison report.</param>
		public void WriteComparisonReport(string baselineFilePath, BenchmarkRun current, string outputPath = null)
		{
			var baseline = BenchmarkRun.LoadFromFile(baselineFilePath);
			var comparison = m_comparer.Compare(baseline, current);

			outputPath = outputPath ?? Path.Combine(OutputDirectory, "comparison.md");
			var content = m_comparer.GenerateSummary(comparison);
			File.WriteAllText(outputPath, content, Encoding.UTF8);
		}

		private List<string> GenerateRecommendations(BenchmarkRun run, AnalysisSummary summary)
		{
			var recommendations = new List<string>();

			// Check for high cold/warm ratio
			if (summary.AverageColdRenderMs > 0 && summary.AverageWarmRenderMs > 0)
			{
				var ratio = summary.AverageColdRenderMs / summary.AverageWarmRenderMs;
				if (ratio > 5)
				{
					recommendations.Add("High cold/warm ratio suggests initialization overhead. Consider lazy loading or caching.");
				}
			}

			// Check for failing scenarios
			if (summary.FailingScenarios > 0)
			{
				recommendations.Add($"Fix {summary.FailingScenarios} failing pixel-perfect validation(s) before optimizing.");
			}

			// Check for high variance
			var highVarianceScenarios = run.Results.Where(r => r.VariancePercent > 10).ToList();
			if (highVarianceScenarios.Any())
			{
				recommendations.Add($"{highVarianceScenarios.Count} scenario(s) have >10% variance. Consider more test iterations or environment stabilization.");
			}

			// Check top contributors for optimization opportunities
			if (summary.TopContributors?.Any() == true)
			{
				var topStage = summary.TopContributors.First();
				if (topStage.SharePercent > 40)
				{
					recommendations.Add($"'{topStage.Stage}' contributes {topStage.SharePercent:F1}% of render time. Prioritize optimization here.");
				}
			}

			// Default recommendations if none generated
			if (!recommendations.Any())
			{
				recommendations.Add("No immediate optimization targets identified. Consider profiling deeper stages.");
				recommendations.Add("Enable trace diagnostics for detailed stage-level timing.");
				recommendations.Add("Review lazy expansion patterns for complex/deep-nested scenarios.");
			}

			return recommendations;
		}
	}
}
