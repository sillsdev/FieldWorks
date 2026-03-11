// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites.RenderBenchmark;
using SIL.FieldWorks.Common.RenderVerification;
using SIL.LCModel.Infrastructure;

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
			// Create scenario data inside a UoW
			using (var uow = new UndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor,
				"Setup Scenario", "Undo Setup Scenario"))
			{
				SetupScenarioData(scenarioId);
				uow.RollBack = false;
			}

			// Load scenario config to get ViewType
			var allScenarios = RenderScenarioDataBuilder.LoadFromFile();
			var scenarioConfig = allScenarios.FirstOrDefault(s => s.Id == scenarioId);

			var scenario = new RenderScenario
			{
				Id = scenarioId,
				Description = $"Verify snapshot for {scenarioId}",
				RootObjectHvo = m_hvoRoot,
				RootFlid = m_flidContainingTexts,
				FragmentId = m_frag,
				ViewType = scenarioConfig?.ViewType ?? RenderViewType.Scripture,
				SimulateIfDataDoubleRender = scenarioConfig?.SimulateIfDataDoubleRender ?? false
			};

			using (var harness = new RenderBenchmarkHarness(Cache, scenario))
			{
				harness.ExecuteColdRender();
				using (var bitmap = harness.CaptureViewBitmap())
				{
					string directory = RenderBaselineVerifier.GetSourceFileDirectory();
					string name = $"RenderVerifyTests.VerifyScenario_{scenarioId}";
					var verification = RenderBaselineVerifier.Verify(bitmap, directory, name, scenarioId);
					if (!verification.Passed)
						Assert.Fail(verification.FailureMessage);
				}
			}

			await Task.CompletedTask;
		}

		/// <summary>
		/// Provides all scenario IDs from the JSON config for parameterized Verify tests.
		/// </summary>
		public static IEnumerable<string> GetVerifyScenarios()
		{
			try
			{
				var scenarios = RenderScenarioDataBuilder.LoadFromFile();
				if (scenarios.Count > 0)
					return scenarios.Select(s => s.Id);
			}
			catch
			{
				// Fall through to default list
			}
			return new[]
			{
				"simple", "medium", "complex", "deep-nested", "custom-heavy",
				"many-paragraphs", "footnote-heavy", "mixed-styles", "long-prose", "multi-book"
			};
		}
	}
}
