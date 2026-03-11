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
		private static readonly Lazy<IReadOnlyDictionary<string, DataTreeTimingBaseline>> s_baselines =
			new Lazy<IReadOnlyDictionary<string, DataTreeTimingBaseline>>(LoadBaselines);

		internal static IReadOnlyDictionary<string, DataTreeTimingBaseline> Baselines => s_baselines.Value;

		internal static string BaselineFilePath => Path.Combine(GetSourceFileDirectory(), "DataTreeTimingBaselines.json");

		internal static void AssertMatches(string scenario, int depth, int breadth, DataTreeTimingInfo timing, double density)
		{
			if (!Baselines.TryGetValue(scenario, out var baseline))
			{
				Assert.Fail($"Missing DataTree timing baseline for scenario '{scenario}'. Add it to {BaselineFilePath}.");
			}

			Assert.That(depth, Is.EqualTo(baseline.Depth),
				$"Scenario '{scenario}' depth no longer matches its committed timing baseline.");
			Assert.That(breadth, Is.EqualTo(baseline.Breadth),
				$"Scenario '{scenario}' breadth no longer matches its committed timing baseline.");
			Assert.That(timing.SliceCount, Is.EqualTo(baseline.Slices),
				$"Scenario '{scenario}' slice count no longer matches its committed timing baseline.");
			Assert.That(timing.InitializationMs, Is.LessThanOrEqualTo(baseline.MaxInitMs),
				$"Scenario '{scenario}' initialization time regressed beyond its timing baseline.");
			Assert.That(timing.PopulateSlicesMs, Is.LessThanOrEqualTo(baseline.MaxPopulateMs),
				$"Scenario '{scenario}' populate time regressed beyond its timing baseline.");
			Assert.That(timing.TotalMs, Is.LessThanOrEqualTo(baseline.MaxTotalMs),
				$"Scenario '{scenario}' total time regressed beyond its timing baseline.");
			Assert.That(density, Is.GreaterThanOrEqualTo(baseline.MinDensity),
				$"Scenario '{scenario}' rendered less content than expected for its timing baseline.");
			Assert.That(density, Is.LessThanOrEqualTo(baseline.MaxDensity),
				$"Scenario '{scenario}' rendered more content than expected for its timing baseline.");
		}

		internal static void AssertSnapshotCoverage()
		{
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

			Assert.That(missingScenarioIds, Is.Empty,
				$"Committed snapshot scenarios must all have timing baselines in {BaselineFilePath}.");
		}

		private static IReadOnlyDictionary<string, DataTreeTimingBaseline> LoadBaselines()
		{
			if (!File.Exists(BaselineFilePath))
				throw new FileNotFoundException("DataTree timing baseline file not found.", BaselineFilePath);

			var json = File.ReadAllText(BaselineFilePath);
			var baselines = JsonConvert.DeserializeObject<Dictionary<string, DataTreeTimingBaseline>>(json);
			return baselines ?? new Dictionary<string, DataTreeTimingBaseline>(StringComparer.Ordinal);
		}

		private static string GetSourceFileDirectory([CallerFilePath] string sourceFile = "")
		{
			return Path.GetDirectoryName(sourceFile);
		}
	}
}