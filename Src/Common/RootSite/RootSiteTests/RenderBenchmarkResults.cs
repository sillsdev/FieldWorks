// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SIL.FieldWorks.Common.RootSites.RenderBenchmark
{
	/// <summary>
	/// Contains the full results of a benchmark run including all scenario timings.
	/// </summary>
	public class BenchmarkRun
	{
		/// <summary>Gets or sets the unique run identifier.</summary>
		public string Id { get; set; } = Guid.NewGuid().ToString("N");

		/// <summary>Gets or sets the run timestamp.</summary>
		public DateTime RunAt { get; set; } = DateTime.UtcNow;

		/// <summary>Gets or sets the build configuration (Debug/Release).</summary>
		public string Configuration { get; set; }

		/// <summary>Gets or sets the environment hash for deterministic validation.</summary>
		public string EnvironmentHash { get; set; }

		/// <summary>Gets or sets the machine name.</summary>
		public string MachineName { get; set; } = Environment.MachineName;

		/// <summary>Gets or sets the list of scenario results.</summary>
		public List<BenchmarkResult> Results { get; set; } = new List<BenchmarkResult>();

		/// <summary>Gets or sets the analysis summary.</summary>
		public AnalysisSummary Summary { get; set; }

		/// <summary>
		/// Saves the benchmark run to a JSON file.
		/// </summary>
		/// <param name="outputPath">The output file path.</param>
		public void SaveToFile(string outputPath)
		{
			var directory = Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			var settings = new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore,
				Converters = { new StringEnumConverter() }
			};

			var json = JsonConvert.SerializeObject(this, settings);
			File.WriteAllText(outputPath, json, Encoding.UTF8);
		}

		/// <summary>
		/// Loads a benchmark run from a JSON file.
		/// </summary>
		/// <param name="inputPath">The input file path.</param>
		/// <returns>The loaded benchmark run.</returns>
		public static BenchmarkRun LoadFromFile(string inputPath)
		{
			if (!File.Exists(inputPath))
				throw new FileNotFoundException("Benchmark results file not found.", inputPath);

			var json = File.ReadAllText(inputPath, Encoding.UTF8);
			return JsonConvert.DeserializeObject<BenchmarkRun>(json);
		}
	}

	/// <summary>
	/// Contains the timing and validation results for a single scenario.
	/// </summary>
	public class BenchmarkResult
	{
		/// <summary>Gets or sets the scenario identifier.</summary>
		public string ScenarioId { get; set; }

		/// <summary>Gets or sets the scenario description.</summary>
		public string ScenarioDescription { get; set; }

		/// <summary>Gets or sets the cold render duration in milliseconds.</summary>
		public double ColdRenderMs { get; set; }

		/// <summary>Gets or sets the warm render duration in milliseconds.</summary>
		public double WarmRenderMs { get; set; }

		/// <summary>Gets or sets the variance percentage across multiple runs.</summary>
		public double VariancePercent { get; set; }

		/// <summary>Gets or sets whether the pixel-perfect validation passed.</summary>
		public bool PixelPerfectPass { get; set; }

		/// <summary>Gets or sets the mismatch details if validation failed.</summary>
		public string MismatchDetails { get; set; }

		/// <summary>Gets or sets the snapshot path used for comparison.</summary>
		public string SnapshotPath { get; set; }

		/// <summary>Gets or sets the trace events for this scenario (if diagnostics enabled).</summary>
		public List<TraceEvent> TraceEvents { get; set; }
	}

	/// <summary>
	/// Captures a single rendering stage trace event.
	/// </summary>
	public class TraceEvent
	{
		/// <summary>Gets or sets the rendering stage name.</summary>
		public string Stage { get; set; }

		/// <summary>Gets or sets the stage start time (relative to render start).</summary>
		public double StartTimeMs { get; set; }

		/// <summary>Gets or sets the stage duration in milliseconds.</summary>
		public double DurationMs { get; set; }

		/// <summary>Gets or sets additional context metadata.</summary>
		public Dictionary<string, string> Context { get; set; }
	}

	/// <summary>
	/// Summarizes the benchmark run with top contributors and recommendations.
	/// </summary>
	public class AnalysisSummary
	{
		/// <summary>Gets or sets the benchmark run identifier.</summary>
		public string RunId { get; set; }

		/// <summary>Gets or sets the total scenarios executed.</summary>
		public int TotalScenarios { get; set; }

		/// <summary>Gets or sets the number of passing scenarios.</summary>
		public int PassingScenarios { get; set; }

		/// <summary>Gets or sets the number of failing scenarios.</summary>
		public int FailingScenarios { get; set; }

		/// <summary>Gets or sets the average cold render time across scenarios.</summary>
		public double AverageColdRenderMs { get; set; }

		/// <summary>Gets or sets the average warm render time across scenarios.</summary>
		public double AverageWarmRenderMs { get; set; }

		/// <summary>Gets or sets the top time contributors by stage.</summary>
		public List<Contributor> TopContributors { get; set; } = new List<Contributor>();

		/// <summary>Gets or sets optimization recommendations.</summary>
		public List<string> Recommendations { get; set; } = new List<string>();

		/// <summary>Gets or sets whether any regressions were detected compared to baseline.</summary>
		public bool HasRegressions { get; set; }

		/// <summary>Gets or sets regression details if any were detected.</summary>
		public List<RegressionInfo> Regressions { get; set; } = new List<RegressionInfo>();
	}

	/// <summary>
	/// Represents a ranked timing contributor.
	/// </summary>
	public class Contributor
	{
		/// <summary>Gets or sets the rendering stage name.</summary>
		public string Stage { get; set; }

		/// <summary>Gets or sets the average duration in milliseconds.</summary>
		public double AverageDurationMs { get; set; }

		/// <summary>Gets or sets the percentage share of total render time.</summary>
		public double SharePercent { get; set; }
	}

	/// <summary>
	/// Contains information about a detected regression.
	/// </summary>
	public class RegressionInfo
	{
		/// <summary>Gets or sets the scenario identifier.</summary>
		public string ScenarioId { get; set; }

		/// <summary>Gets or sets the metric that regressed.</summary>
		public string Metric { get; set; }

		/// <summary>Gets or sets the baseline value.</summary>
		public double BaselineValue { get; set; }

		/// <summary>Gets or sets the current value.</summary>
		public double CurrentValue { get; set; }

		/// <summary>Gets or sets the regression percentage.</summary>
		public double RegressionPercent { get; set; }
	}

	/// <summary>
	/// Configuration flags for benchmark execution.
	/// </summary>
	public class BenchmarkFlags
	{
		/// <summary>Gets or sets whether diagnostics logging is enabled.</summary>
		public bool DiagnosticsEnabled { get; set; }

		/// <summary>Gets or sets whether trace output is enabled.</summary>
		public bool TraceEnabled { get; set; }

		/// <summary>Gets or sets the capture mode (DrawToBitmap, etc.).</summary>
		public string CaptureMode { get; set; } = "DrawToBitmap";

		/// <summary>
		/// Loads flags from a JSON file.
		/// </summary>
		/// <param name="path">The flags file path.</param>
		/// <returns>The loaded flags, or defaults if file not found.</returns>
		public static BenchmarkFlags LoadFromFile(string path)
		{
			if (!File.Exists(path))
				return new BenchmarkFlags();

			var json = File.ReadAllText(path, Encoding.UTF8);
			return JsonConvert.DeserializeObject<BenchmarkFlags>(json) ?? new BenchmarkFlags();
		}
	}
}
