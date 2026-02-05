// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace SIL.FieldWorks.Common.RootSites.RenderBenchmark
{
	/// <summary>
	/// Provides helpers for building and loading render scenario configurations.
	/// </summary>
	public class RenderScenarioDataBuilder
	{
		private readonly List<RenderScenario> m_scenarios = new List<RenderScenario>();

		/// <summary>
		/// Gets the test data directory path.
		/// </summary>
		public static string TestDataDirectory
		{
			get
			{
				// Handle different output path depths (e.g. Output/Debug vs. bin/Debug/net472)
				var baseDir = AppDomain.CurrentDomain.BaseDirectory;

				// Try 2 levels up (Output/Debug -> Root)
				var path2 = Path.Combine(baseDir, "..", "..", "Src", "Common", "RootSite", "RootSiteTests", "TestData");
				if (Directory.Exists(path2)) return Path.GetFullPath(path2);

				// Try 3 levels up (bin/Debug/net -> Root)
				var path3 = Path.Combine(baseDir, "..", "..", "..", "Src", "Common", "RootSite", "RootSiteTests", "TestData");
				if (Directory.Exists(path3)) return Path.GetFullPath(path3);

				// Fallback to 2 levels and let it fail with full path for debugging
				return Path.GetFullPath(path2);
			}
		}

		/// <summary>
		/// Gets the default scenarios file path.
		/// </summary>
		public static string DefaultScenariosPath => Path.Combine(TestDataDirectory, "RenderBenchmarkScenarios.json");

		/// <summary>
		/// Gets the default flags file path.
		/// </summary>
		public static string DefaultFlagsPath => Path.Combine(TestDataDirectory, "RenderBenchmarkFlags.json");

		/// <summary>
		/// Gets the snapshots directory path.
		/// </summary>
		public static string SnapshotsDirectory => Path.Combine(TestDataDirectory, "RenderSnapshots");

		/// <summary>
		/// Creates a new scenario builder.
		/// </summary>
		/// <returns>A new builder instance.</returns>
		public static RenderScenarioDataBuilder Create()
		{
			return new RenderScenarioDataBuilder();
		}

		/// <summary>
		/// Adds a simple scenario (minimal entry with one sense).
		/// </summary>
		/// <param name="rootHvo">The root object HVO.</param>
		/// <param name="rootFlid">The root field ID.</param>
		/// <returns>The builder for chaining.</returns>
		public RenderScenarioDataBuilder AddSimpleScenario(int rootHvo, int rootFlid)
		{
			m_scenarios.Add(new RenderScenario
			{
				Id = "simple",
				Description = "Minimal lexical entry with one sense, one definition",
				RootObjectHvo = rootHvo,
				RootFlid = rootFlid,
				ExpectedSnapshotPath = Path.Combine(SnapshotsDirectory, "simple.png"),
				Tags = new[] { "baseline", "minimal" }
			});
			return this;
		}

		/// <summary>
		/// Adds a medium complexity scenario.
		/// </summary>
		/// <param name="rootHvo">The root object HVO.</param>
		/// <param name="rootFlid">The root field ID.</param>
		/// <returns>The builder for chaining.</returns>
		public RenderScenarioDataBuilder AddMediumScenario(int rootHvo, int rootFlid)
		{
			m_scenarios.Add(new RenderScenario
			{
				Id = "medium",
				Description = "Entry with 3 senses, multiple definitions, example sentences",
				RootObjectHvo = rootHvo,
				RootFlid = rootFlid,
				ExpectedSnapshotPath = Path.Combine(SnapshotsDirectory, "medium.png"),
				Tags = new[] { "typical", "multi-sense" }
			});
			return this;
		}

		/// <summary>
		/// Adds a complex scenario with many senses.
		/// </summary>
		/// <param name="rootHvo">The root object HVO.</param>
		/// <param name="rootFlid">The root field ID.</param>
		/// <returns>The builder for chaining.</returns>
		public RenderScenarioDataBuilder AddComplexScenario(int rootHvo, int rootFlid)
		{
			m_scenarios.Add(new RenderScenario
			{
				Id = "complex",
				Description = "Entry with 10+ senses, subsenses, extensive cross-references",
				RootObjectHvo = rootHvo,
				RootFlid = rootFlid,
				ExpectedSnapshotPath = Path.Combine(SnapshotsDirectory, "complex.png"),
				Tags = new[] { "stress", "multi-sense", "cross-refs" }
			});
			return this;
		}

		/// <summary>
		/// Adds a deep-nested scenario.
		/// </summary>
		/// <param name="rootHvo">The root object HVO.</param>
		/// <param name="rootFlid">The root field ID.</param>
		/// <returns>The builder for chaining.</returns>
		public RenderScenarioDataBuilder AddDeepNestedScenario(int rootHvo, int rootFlid)
		{
			m_scenarios.Add(new RenderScenario
			{
				Id = "deep-nested",
				Description = "Entry with deeply nested subsenses (5+ levels)",
				RootObjectHvo = rootHvo,
				RootFlid = rootFlid,
				ExpectedSnapshotPath = Path.Combine(SnapshotsDirectory, "deep-nested.png"),
				Tags = new[] { "nested", "hierarchy", "stress" }
			});
			return this;
		}

		/// <summary>
		/// Adds a custom-field-heavy scenario.
		/// </summary>
		/// <param name="rootHvo">The root object HVO.</param>
		/// <param name="rootFlid">The root field ID.</param>
		/// <returns>The builder for chaining.</returns>
		public RenderScenarioDataBuilder AddCustomFieldHeavyScenario(int rootHvo, int rootFlid)
		{
			m_scenarios.Add(new RenderScenario
			{
				Id = "custom-field-heavy",
				Description = "Entry with many custom fields of various types",
				RootObjectHvo = rootHvo,
				RootFlid = rootFlid,
				ExpectedSnapshotPath = Path.Combine(SnapshotsDirectory, "custom-field-heavy.png"),
				Tags = new[] { "custom-fields", "extensibility" }
			});
			return this;
		}

		/// <summary>
		/// Adds a custom scenario.
		/// </summary>
		/// <param name="scenario">The scenario to add.</param>
		/// <returns>The builder for chaining.</returns>
		public RenderScenarioDataBuilder AddScenario(RenderScenario scenario)
		{
			if (scenario == null)
				throw new ArgumentNullException(nameof(scenario));

			m_scenarios.Add(scenario);
			return this;
		}

		/// <summary>
		/// Builds and returns the list of scenarios.
		/// </summary>
		/// <returns>The built scenarios.</returns>
		public List<RenderScenario> Build()
		{
			return m_scenarios.ToList();
		}

		/// <summary>
		/// Saves the scenarios to a JSON file.
		/// </summary>
		/// <param name="outputPath">The output file path.</param>
		public void SaveToFile(string outputPath = null)
		{
			outputPath = outputPath ?? DefaultScenariosPath;

			var directory = Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			var wrapper = new ScenariosWrapper { Scenarios = m_scenarios };
			var json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
			File.WriteAllText(outputPath, json, Encoding.UTF8);
		}

		/// <summary>
		/// Loads scenarios from a JSON file.
		/// </summary>
		/// <param name="inputPath">The input file path.</param>
		/// <returns>The loaded scenarios, or empty list if file not found.</returns>
		public static List<RenderScenario> LoadFromFile(string inputPath = null)
		{
			inputPath = inputPath ?? DefaultScenariosPath;

			if (!File.Exists(inputPath))
				return new List<RenderScenario>();

			var json = File.ReadAllText(inputPath, Encoding.UTF8);
			var wrapper = JsonConvert.DeserializeObject<ScenariosWrapper>(json);
			return wrapper?.Scenarios ?? new List<RenderScenario>();
		}

		/// <summary>
		/// Creates the standard five-scenario suite.
		/// </summary>
		/// <param name="rootHvo">The root object HVO for all scenarios.</param>
		/// <param name="rootFlid">The root field ID for all scenarios.</param>
		/// <returns>A builder with all five standard scenarios.</returns>
		public static RenderScenarioDataBuilder CreateStandardSuite(int rootHvo, int rootFlid)
		{
			return Create()
				.AddSimpleScenario(rootHvo, rootFlid)
				.AddMediumScenario(rootHvo, rootFlid)
				.AddComplexScenario(rootHvo, rootFlid)
				.AddDeepNestedScenario(rootHvo, rootFlid)
				.AddCustomFieldHeavyScenario(rootHvo, rootFlid);
		}

		/// <summary>
		/// Gets scenarios filtered by tag.
		/// </summary>
		/// <param name="scenarios">The scenarios to filter.</param>
		/// <param name="tag">The tag to filter by.</param>
		/// <returns>Scenarios matching the tag.</returns>
		public static IEnumerable<RenderScenario> FilterByTag(IEnumerable<RenderScenario> scenarios, string tag)
		{
			return scenarios.Where(s => s.Tags != null && s.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
		}

		private class ScenariosWrapper
		{
			[JsonProperty("scenarios")]
			public List<RenderScenario> Scenarios { get; set; } = new List<RenderScenario>();
		}
	}
}
