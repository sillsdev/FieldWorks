// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using NUnit.Framework;
using SIL.FieldWorks.Common.RenderVerification;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	internal sealed class DataTreeTimingBaseline
	{
		public int Depth { get; set; }
		public int Breadth { get; set; }
		public int Slices { get; set; }
		public double MaxInitMs { get; set; }
		public double MaxPopulateMs { get; set; }
		public double MaxTotalMs { get; set; }
		public double MinDensity { get; set; }
		public double MaxDensity { get; set; }
	}

	internal static class DataTreeTimingBaselineCatalog
	{
		private const string ReportTimingBaselinesEnvVar = "FW_REPORT_TIMING_BASELINES";
		private static readonly Lazy<IReadOnlyDictionary<string, DataTreeTimingBaseline>> s_baselines =
			new Lazy<IReadOnlyDictionary<string, DataTreeTimingBaseline>>(LoadBaselines);

		internal static IReadOnlyDictionary<string, DataTreeTimingBaseline> Baselines => s_baselines.Value;

		internal static string BaselineFilePath => Path.Combine(GetSourceFileDirectory(), "DataTreeTimingBaselines.json");

		internal static void AssertMatches(string scenario, int depth, int breadth, DataTreeTimingInfo timing, double density)
		{
			if (!Baselines.TryGetValue(scenario, out var baseline))
			{
				WriteTimingReport(
					$"Missing local timing baseline for scenario '{scenario}'. " +
					$"Skipping timing threshold checks. Expected file: {BaselineFilePath}");
				return;
			}

			WarnIfValueDiffers(scenario, "Depth", depth, baseline.Depth);
			WarnIfValueDiffers(scenario, "Breadth", breadth, baseline.Breadth);
			WarnIfValueDiffers(scenario, "Slice count", timing.SliceCount, baseline.Slices);
			WarnIfTimingExceedsBaseline(scenario, "Init", timing.InitializationMs, baseline.MaxInitMs);
			WarnIfTimingExceedsBaseline(scenario, "Populate", timing.PopulateSlicesMs, baseline.MaxPopulateMs);
			WarnIfTimingExceedsBaseline(scenario, "Total", timing.TotalMs, baseline.MaxTotalMs);
			WarnIfDensityOutOfRange(scenario, density, baseline.MinDensity, baseline.MaxDensity);
		}

		internal static void AssertSnapshotCoverage()
		{
			if (Baselines.Count == 0)
			{
				WriteTimingReport(
					$"No local timing baselines loaded from {BaselineFilePath}. " +
					"Skipping timing baseline coverage assertion.");
				return;
			}

			var snapshotScenarioIds = Directory
				.GetFiles(GetSourceFileDirectory(), "DataTreeRenderTests.DataTreeRender_*.verified.png")
				.Select(path => Path.GetFileName(path))
				.Select(name => name.Substring(
					"DataTreeRenderTests.DataTreeRender_".Length,
					name.Length - "DataTreeRenderTests.DataTreeRender_".Length - ".verified.png".Length))
				.OrderBy(name => name, StringComparer.Ordinal)
				.ToList();

			var missingScenarioIds = snapshotScenarioIds
				.Where(id => !Baselines.ContainsKey(id))
				.ToList();

			if (missingScenarioIds.Count > 0)
			{
				WriteTimingReport(
					$"Committed snapshot scenarios are missing timing baselines in {BaselineFilePath}: " +
					string.Join(", ", missingScenarioIds));
			}
		}

		private static IReadOnlyDictionary<string, DataTreeTimingBaseline> LoadBaselines()
		{
			if (!File.Exists(BaselineFilePath))
			{
				WriteTimingReport(
					$"Timing baseline file not found at {BaselineFilePath}. " +
					"Using empty baseline catalog.");
				return new Dictionary<string, DataTreeTimingBaseline>(StringComparer.Ordinal);
			}

			var json = File.ReadAllText(BaselineFilePath);
			var baselines = JsonConvert.DeserializeObject<Dictionary<string, DataTreeTimingBaseline>>(json);
			return baselines ?? new Dictionary<string, DataTreeTimingBaseline>(StringComparer.Ordinal);
		}

		private static string GetSourceFileDirectory([CallerFilePath] string sourceFile = "")
		{
			return Path.GetDirectoryName(sourceFile);
		}

		private static bool IsTimingReportingEnabled()
		{
			return string.Equals(
				Environment.GetEnvironmentVariable(ReportTimingBaselinesEnvVar),
				"1",
				StringComparison.Ordinal);
		}

		private static bool IsRunningInCi()
		{
			return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"));
		}

		private static void WarnIfTimingExceedsBaseline(string scenario, string metricName, double actualMs, double baselineMs)
		{
			if (actualMs <= baselineMs)
				return;

			WriteTimingReport(
				$"{scenario} {metricName} exceeded local baseline: " +
				$"actual={actualMs:F2}ms baseline={baselineMs:F2}ms");
		}

		private static void WarnIfValueDiffers(string scenario, string metricName, int actualValue, int baselineValue)
		{
			if (actualValue == baselineValue)
				return;

			WriteTimingReport(
				$"{scenario} {metricName} differs from local baseline: " +
				$"actual={actualValue} baseline={baselineValue}");
		}

		private static void WarnIfDensityOutOfRange(string scenario, double actualDensity, double minDensity, double maxDensity)
		{
			if (actualDensity < minDensity)
			{
				WriteTimingReport(
					$"{scenario} rendered less content than its local timing baseline: " +
					$"actual={actualDensity:F2}% min={minDensity:F2}%");
				return;
			}

			if (actualDensity > maxDensity)
			{
				WriteTimingReport(
					$"{scenario} rendered more content than its local timing baseline: " +
					$"actual={actualDensity:F2}% max={maxDensity:F2}%");
			}
		}

		private static void WriteTimingReport(string message)
		{
			if (IsRunningInCi() || !IsTimingReportingEnabled())
				return;

			TestContext.Progress.WriteLine($"[DATATREE-TIMING] {message}");
		}
	}
}