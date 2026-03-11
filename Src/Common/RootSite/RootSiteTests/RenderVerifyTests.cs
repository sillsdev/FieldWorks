// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites.RenderBenchmark;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Snapshot tests for render baseline validation.
	///
	/// Each run saves a .received.png and compares it against the committed
	/// .verified.png baseline by decoded pixel values, not by the PNG file bytes.
	/// Small encoder-level differences are therefore ignored as long as the rendered
	/// image differs by fewer than five pixels.
	///
	/// Each scenario is set up inside its own UndoableUnitOfWork, matching the pattern
	/// used by RenderTimingSuiteTests.
	/// </summary>
	[TestFixture]
	[Category("RenderBenchmark")]
	public class RenderVerifyTests : RenderBenchmarkTestsBase
	{
		/// <summary>
		/// CreateTestData is a no-op; individual tests call SetupScenarioData within a UoW.
		/// </summary>
		protected override void CreateTestData()
		{
			// Scenario data is created per-test inside a UoW (see VerifyScenario).
		}

		/// <summary>
		/// Verifies that a scenario renders consistently against its .verified.png baseline.
		/// On first run, creates the .received.png for acceptance. On subsequent runs,
		/// compares decoded pixels against the committed .verified.png baseline.
		/// </summary>
		[Test, TestCaseSource(nameof(GetVerifyScenarios))]
		public async Task VerifyScenario(string scenarioId)
		{
			var execution = ExecuteScenarioAndCapture(scenarioId, includeWarmRender: false);
			if (!execution.Verification.Passed)
				Assert.Fail(execution.Verification.FailureMessage);

			await Task.CompletedTask;
		}

		/// <summary>
		/// Provides all scenario IDs from the JSON config for parameterized Verify tests.
		/// </summary>
		public static IEnumerable<string> GetVerifyScenarios()
		{
			return GetConfiguredScenarioIds(
				"simple",
				"medium",
				"complex",
				"deep-nested",
				"custom-heavy",
				"many-paragraphs",
				"footnote-heavy",
				"mixed-styles",
				"long-prose",
				"multi-book");
		}
	}
}
