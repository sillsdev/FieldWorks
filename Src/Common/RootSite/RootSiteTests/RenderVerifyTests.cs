// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
		private const string UpdateBaselinesEnvVar = "FW_UPDATE_RENDER_BASELINES";
		private const int MaxAllowedPixelDifferences = 4;

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
					string directory = GetSourceFileDirectory();
					string name = $"RenderVerifyTests.VerifyScenario_{scenarioId}";
					RefreshVerifiedBaselineIfRequested(bitmap, directory, name);
					VerifyRenderedBitmap(bitmap, directory, name, scenarioId);
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

		/// <summary>
		/// Returns the directory containing this source file (for Verify file placement).
		/// Uses [CallerFilePath] to resolve at compile time.
		/// </summary>
		private static string GetSourceFileDirectory([CallerFilePath] string sourceFile = "")
		{
			return Path.GetDirectoryName(sourceFile);
		}

		private static void RefreshVerifiedBaselineIfRequested(Bitmap bitmap, string directory, string name)
		{
			if (!string.Equals(Environment.GetEnvironmentVariable(UpdateBaselinesEnvVar), "1", StringComparison.Ordinal))
				return;

			string verifiedPath = Path.Combine(directory, $"{name}.verified.png");
			bitmap.Save(verifiedPath, ImageFormat.Png);
		}

		private static void VerifyRenderedBitmap(Bitmap actualBitmap, string directory, string name, string scenarioId)
		{
			string verifiedPath = Path.Combine(directory, $"{name}.verified.png");
			string diffPath = Path.Combine(directory, $"{name}.diff.png");
			if (File.Exists(diffPath))
				File.Delete(diffPath);

			if (!File.Exists(verifiedPath))
			{
				string receivedPath = Path.Combine(directory, $"{name}.received.png");
				actualBitmap.Save(receivedPath, ImageFormat.Png);
				Assert.Fail($"Missing verified render baseline for '{scenarioId}'. Review and accept {receivedPath} as the new baseline.");
			}

			using (var expectedBitmap = new Bitmap(verifiedPath))
			{
				int differentPixelCount = CountDifferentPixels(expectedBitmap, actualBitmap);
				if (differentPixelCount <= MaxAllowedPixelDifferences)
				{
					return;
				}

				using (var diffBitmap = CreateDiffBitmap(expectedBitmap, actualBitmap))
				{
					diffBitmap.Save(diffPath, ImageFormat.Png);
				}

				Assert.Fail(
					$"Render output for '{scenarioId}' differed from baseline by {differentPixelCount} pixels; " +
					$"{MaxAllowedPixelDifferences} or fewer differences are allowed. See {diffPath}.");
			}
		}

		private static int CountDifferentPixels(Bitmap expectedBitmap, Bitmap actualBitmap)
		{
			int maxWidth = Math.Max(expectedBitmap.Width, actualBitmap.Width);
			int maxHeight = Math.Max(expectedBitmap.Height, actualBitmap.Height);
			int differentPixelCount = 0;

			for (int y = 0; y < maxHeight; y++)
			{
				for (int x = 0; x < maxWidth; x++)
				{
					bool expectedInBounds = x < expectedBitmap.Width && y < expectedBitmap.Height;
					bool actualInBounds = x < actualBitmap.Width && y < actualBitmap.Height;

					if (!expectedInBounds || !actualInBounds)
					{
						differentPixelCount++;
						continue;
					}

					if (expectedBitmap.GetPixel(x, y) != actualBitmap.GetPixel(x, y))
						differentPixelCount++;
				}
			}

			return differentPixelCount;
		}

		private static Bitmap CreateDiffBitmap(Bitmap expectedBitmap, Bitmap actualBitmap)
		{
			int maxWidth = Math.Max(expectedBitmap.Width, actualBitmap.Width);
			int maxHeight = Math.Max(expectedBitmap.Height, actualBitmap.Height);
			var diffBitmap = new Bitmap(maxWidth, maxHeight);

			for (int y = 0; y < maxHeight; y++)
			{
				for (int x = 0; x < maxWidth; x++)
				{
					Color expected = x < expectedBitmap.Width && y < expectedBitmap.Height
						? expectedBitmap.GetPixel(x, y)
						: Color.White;
					Color actual = x < actualBitmap.Width && y < actualBitmap.Height
						? actualBitmap.GetPixel(x, y)
						: Color.White;

					diffBitmap.SetPixel(x, y, CreateDiffPixel(expected, actual));
				}
			}

			return diffBitmap;
		}

		private static Color CreateDiffPixel(Color expected, Color actual)
		{
			return Color.FromArgb(
				255,
				ScaleDiffChannel(expected.R, actual.R),
				ScaleDiffChannel(expected.G, actual.G),
				ScaleDiffChannel(expected.B, actual.B));
		}

		private static int ScaleDiffChannel(int expectedChannel, int actualChannel)
		{
			int delta = actualChannel - expectedChannel;
			return Math.Max(0, Math.Min(255, 128 + (delta / 2)));
		}
	}
}
