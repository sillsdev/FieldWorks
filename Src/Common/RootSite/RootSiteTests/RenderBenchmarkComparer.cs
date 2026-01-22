// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.Common.RootSites.RenderBenchmark
{
	/// <summary>
	/// Compares benchmark runs to detect performance regressions.
	/// </summary>
	public class RenderBenchmarkComparer
	{
		/// <summary>
		/// Gets or sets the threshold percentage for detecting cold render regressions.
		/// Default is 10% (0.10).
		/// </summary>
		public double ColdRenderRegressionThreshold { get; set; } = 0.10;

		/// <summary>
		/// Gets or sets the threshold percentage for detecting warm render regressions.
		/// Default is 15% (0.15).
		/// </summary>
		public double WarmRenderRegressionThreshold { get; set; } = 0.15;

		/// <summary>
		/// Gets or sets the minimum absolute difference (in ms) to consider a regression.
		/// Avoids false positives for very fast operations.
		/// </summary>
		public double MinAbsoluteDifferenceMs { get; set; } = 5.0;

		/// <summary>
		/// Compares two benchmark runs and identifies regressions.
		/// </summary>
		/// <param name="baseline">The baseline run to compare against.</param>
		/// <param name="current">The current run being evaluated.</param>
		/// <returns>A comparison result with regression details.</returns>
		public BenchmarkComparisonResult Compare(BenchmarkRun baseline, BenchmarkRun current)
		{
			if (baseline == null)
				throw new ArgumentNullException(nameof(baseline));
			if (current == null)
				throw new ArgumentNullException(nameof(current));

			var result = new BenchmarkComparisonResult
			{
				BaselineRunId = baseline.Id,
				CurrentRunId = current.Id,
				BaselineTimestamp = baseline.RunAt,
				CurrentTimestamp = current.RunAt
			};

			// Build lookup for baseline results
			var baselineByScenario = baseline.Results.ToDictionary(r => r.ScenarioId, StringComparer.OrdinalIgnoreCase);

			foreach (var currentResult in current.Results)
			{
				if (!baselineByScenario.TryGetValue(currentResult.ScenarioId, out var baselineResult))
				{
					// New scenario, no comparison possible
					result.NewScenarios.Add(currentResult.ScenarioId);
					continue;
				}

				// Check cold render regression
				var coldDiff = currentResult.ColdRenderMs - baselineResult.ColdRenderMs;
				var coldPercent = baselineResult.ColdRenderMs > 0
					? coldDiff / baselineResult.ColdRenderMs
					: 0;

				if (coldDiff > MinAbsoluteDifferenceMs && coldPercent > ColdRenderRegressionThreshold)
				{
					result.Regressions.Add(new RegressionInfo
					{
						ScenarioId = currentResult.ScenarioId,
						Metric = "ColdRender",
						BaselineValue = baselineResult.ColdRenderMs,
						CurrentValue = currentResult.ColdRenderMs,
						RegressionPercent = coldPercent * 100
					});
				}

				// Check warm render regression
				var warmDiff = currentResult.WarmRenderMs - baselineResult.WarmRenderMs;
				var warmPercent = baselineResult.WarmRenderMs > 0
					? warmDiff / baselineResult.WarmRenderMs
					: 0;

				if (warmDiff > MinAbsoluteDifferenceMs && warmPercent > WarmRenderRegressionThreshold)
				{
					result.Regressions.Add(new RegressionInfo
					{
						ScenarioId = currentResult.ScenarioId,
						Metric = "WarmRender",
						BaselineValue = baselineResult.WarmRenderMs,
						CurrentValue = currentResult.WarmRenderMs,
						RegressionPercent = warmPercent * 100
					});
				}

				// Check pixel validation regression (pass -> fail)
				if (baselineResult.PixelPerfectPass && !currentResult.PixelPerfectPass)
				{
					result.PixelValidationRegressions.Add(currentResult.ScenarioId);
				}

				// Track improvements (inverse of regression)
				if (coldDiff < -MinAbsoluteDifferenceMs && coldPercent < -ColdRenderRegressionThreshold)
				{
					result.Improvements.Add(new RegressionInfo
					{
						ScenarioId = currentResult.ScenarioId,
						Metric = "ColdRender",
						BaselineValue = baselineResult.ColdRenderMs,
						CurrentValue = currentResult.ColdRenderMs,
						RegressionPercent = coldPercent * 100 // Negative = improvement
					});
				}

				if (warmDiff < -MinAbsoluteDifferenceMs && warmPercent < -WarmRenderRegressionThreshold)
				{
					result.Improvements.Add(new RegressionInfo
					{
						ScenarioId = currentResult.ScenarioId,
						Metric = "WarmRender",
						BaselineValue = baselineResult.WarmRenderMs,
						CurrentValue = currentResult.WarmRenderMs,
						RegressionPercent = warmPercent * 100
					});
				}
			}

			// Find missing scenarios (in baseline but not in current)
			var currentScenarios = new HashSet<string>(current.Results.Select(r => r.ScenarioId), StringComparer.OrdinalIgnoreCase);
			result.MissingScenarios = baseline.Results
				.Where(r => !currentScenarios.Contains(r.ScenarioId))
				.Select(r => r.ScenarioId)
				.ToList();

			result.HasRegressions = result.Regressions.Any() || result.PixelValidationRegressions.Any();
			result.HasImprovements = result.Improvements.Any();

			return result;
		}

		/// <summary>
		/// Compares a current run against a baseline file.
		/// </summary>
		/// <param name="baselineFilePath">Path to the baseline results JSON file.</param>
		/// <param name="current">The current run.</param>
		/// <returns>The comparison result.</returns>
		public BenchmarkComparisonResult CompareToBaseline(string baselineFilePath, BenchmarkRun current)
		{
			var baseline = BenchmarkRun.LoadFromFile(baselineFilePath);
			return Compare(baseline, current);
		}

		/// <summary>
		/// Generates a summary report of the comparison.
		/// </summary>
		/// <param name="result">The comparison result.</param>
		/// <returns>A formatted summary string.</returns>
		public string GenerateSummary(BenchmarkComparisonResult result)
		{
			var lines = new List<string>
			{
				"# Benchmark Comparison Summary",
				"",
				$"**Baseline**: {result.BaselineRunId} ({result.BaselineTimestamp:u})",
				$"**Current**: {result.CurrentRunId} ({result.CurrentTimestamp:u})",
				""
			};

			if (result.HasRegressions)
			{
				lines.Add("## ⚠️ Regressions Detected");
				lines.Add("");

				foreach (var reg in result.Regressions)
				{
					lines.Add($"- **{reg.ScenarioId}** ({reg.Metric}): {reg.BaselineValue:F2}ms → {reg.CurrentValue:F2}ms (+{reg.RegressionPercent:F1}%)");
				}

				if (result.PixelValidationRegressions.Any())
				{
					lines.Add("");
					lines.Add("### Pixel Validation Failures");
					foreach (var scenario in result.PixelValidationRegressions)
					{
						lines.Add($"- {scenario}");
					}
				}

				lines.Add("");
			}

			if (result.HasImprovements)
			{
				lines.Add("## ✅ Improvements");
				lines.Add("");

				foreach (var imp in result.Improvements)
				{
					lines.Add($"- **{imp.ScenarioId}** ({imp.Metric}): {imp.BaselineValue:F2}ms → {imp.CurrentValue:F2}ms ({imp.RegressionPercent:F1}%)");
				}

				lines.Add("");
			}

			if (!result.HasRegressions && !result.HasImprovements)
			{
				lines.Add("## ✅ No Significant Changes");
				lines.Add("");
				lines.Add("All scenarios are within acceptable tolerance.");
				lines.Add("");
			}

			if (result.NewScenarios.Any())
			{
				lines.Add("## New Scenarios (no baseline)");
				lines.Add("");
				foreach (var scenario in result.NewScenarios)
				{
					lines.Add($"- {scenario}");
				}
				lines.Add("");
			}

			if (result.MissingScenarios.Any())
			{
				lines.Add("## Missing Scenarios (in baseline, not in current)");
				lines.Add("");
				foreach (var scenario in result.MissingScenarios)
				{
					lines.Add($"- {scenario}");
				}
				lines.Add("");
			}

			return string.Join(Environment.NewLine, lines);
		}
	}

	/// <summary>
	/// Contains the result of comparing two benchmark runs.
	/// </summary>
	public class BenchmarkComparisonResult
	{
		/// <summary>Gets or sets the baseline run identifier.</summary>
		public string BaselineRunId { get; set; }

		/// <summary>Gets or sets the current run identifier.</summary>
		public string CurrentRunId { get; set; }

		/// <summary>Gets or sets the baseline timestamp.</summary>
		public DateTime BaselineTimestamp { get; set; }

		/// <summary>Gets or sets the current timestamp.</summary>
		public DateTime CurrentTimestamp { get; set; }

		/// <summary>Gets or sets whether any regressions were detected.</summary>
		public bool HasRegressions { get; set; }

		/// <summary>Gets or sets whether any improvements were detected.</summary>
		public bool HasImprovements { get; set; }

		/// <summary>Gets or sets the list of timing regressions.</summary>
		public List<RegressionInfo> Regressions { get; set; } = new List<RegressionInfo>();

		/// <summary>Gets or sets the list of timing improvements.</summary>
		public List<RegressionInfo> Improvements { get; set; } = new List<RegressionInfo>();

		/// <summary>Gets or sets scenarios that failed pixel validation but previously passed.</summary>
		public List<string> PixelValidationRegressions { get; set; } = new List<string>();

		/// <summary>Gets or sets new scenarios not in baseline.</summary>
		public List<string> NewScenarios { get; set; } = new List<string>();

		/// <summary>Gets or sets scenarios in baseline but missing from current.</summary>
		public List<string> MissingScenarios { get; set; } = new List<string>();
	}
}
