// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Task 7.8 — produces the canonical Path 3 parity bundle for the first-slice scenario per the
	/// 2.9 contract: shared scenarioId/bundleId/failureSummaryId plus an evidence manifest that records
	/// each kind of evidence as proven or pending (never silently omitted). Evidence assembled here: semantic
	/// snapshot (typed IR), Avalonia visual (Skia rendered frame), WinForms visual (the committed
	/// verified baseline), workflow/accessibility (UIA suites), performance (timing baselines).
	/// </summary>
	[TestFixture]
	public class Path3BundleTests
	{
		[AvaloniaTest]
		public void FirstSlice_Path3Bundle_AssemblesAllLanes_WithExplicitStatus()
		{
			var repoRoot = FindRepoRoot();
			var partsDir = Path.Combine(repoRoot, "DistFiles", "Language Explorer", "Configuration", "Parts");
			var definition = LexicalEditFirstSlice.CompileFromLayoutDirectory(partsDir);
			Assert.That(definition, Is.Not.Null);

			var bundleDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "path3-first-slice");
			Directory.CreateDirectory(bundleDir);
			const string scenarioId = "first-slice";
			var bundleId = scenarioId + "-bundle";

			// Semantic evidence: the deterministic typed-IR snapshot.
			var semanticPath = Path.Combine(bundleDir, "semantic-snapshot.txt");
			File.WriteAllText(semanticPath, definition.ToSnapshot());

			// Avalonia visual evidence: a real Skia rendered frame of the region view.
			var model = LexicalEditRegionMapper.FromViewDefinition(definition, new FakeRegionValueProvider());
			var window = new Window { Content = new LexicalEditRegionView(model), Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			var avaloniaPng = Path.Combine(bundleDir, "avalonia-visual.png");
			using (var frame = window.CaptureRenderedFrame())
				frame.Save(avaloniaPng);

			// WinForms visual evidence: the committed legacy baseline. There is no WinForms baseline
			// captured for the exact "first-slice" scenario, so this reuses the closest available
			// legacy render (the depth-4 "production-like" lexeme edit, which is a superset of the
			// first-slice fields) as a proxy. Because the artifact's own scenario name differs from
			// the bundle's scenarioId by design, a plain File.Exists check can't catch someone
			// swapping in an unrelated baseline later, so this also pins and verifies the sidecar's
			// recorded ScenarioId, which is the one piece of evidence in the artifact that actually
			// identifies what it was captured from.
			const string winFormsProxyScenarioId = "subsubsub-hidden-productionlike";
			var winFormsBaseline = Path.Combine(repoRoot, "Src", "Common", "Controls", "DetailControls",
				"DetailControlsTests",
				"DataTreeRenderTests.DataTreeRender_" + winFormsProxyScenarioId + ".verified.png");
			var winFormsBaselineMetadata = winFormsBaseline.Substring(0, winFormsBaseline.Length - ".png".Length) + ".json";
			var winFormsScenarioMatches = TryReadWinFormsScenarioId(winFormsBaselineMetadata, out var winFormsRecordedScenarioId)
				&& winFormsRecordedScenarioId == winFormsProxyScenarioId;

			// Workflow/accessibility evidence: PreviewHostUiaTests exercises the real Windows UIA tree
			// against the preview host, but it requires an interactive desktop session
			// (Environment.UserInteractive) and lives in a separate net48 desktop-environment test project. It
			// self-skips (Assert.Ignore) under a non-interactive host and cannot be invoked from this
			// headless Avalonia unit test. The most this test can honestly do is confirm the suite still
			// exists and still contains the coverage it claims; it cannot prove the run passed. See the
			// fieldworks-uia2-parity-testing skill for the manual/desktop-environment run that actually proves
			// this coverage.
			var previewHostUiaTestsPath = Path.Combine(repoRoot, "Src", "Common", "FwAvaloniaPreviewHost",
				"FwAvaloniaPreviewHostTests", "PreviewHostUiaTests.cs");
			var workflowAccessibilitySuitePresent = File.Exists(previewHostUiaTestsPath)
				&& File.ReadAllText(previewHostUiaTestsPath).Contains("PreviewHost_UiaTree_ExposesLegacyFieldLabels_InLegacyOrder");

			// Performance evidence: DataTreeTimingBaselines.json is gitignored/generated (not committed), so
			// its presence can only be checked at test-run time against wherever it lands on disk, not
			// assumed from the repo. When present it is used as an advisory local baseline (see
			// DataTreeTimingBaselineCatalog); when absent, timing checks are skipped rather than failing,
			// so this evidence cannot honestly be called proven.
			var timingBaselinesPath = Path.Combine(repoRoot, "Src", "Common", "Controls", "DetailControls",
				"DetailControlsTests", "DataTreeTimingBaselines.json");
			var timingBaselinesPresent = File.Exists(timingBaselinesPath) && new FileInfo(timingBaselinesPath).Length > 0;

			var manifest = new
			{
				scenarioId,
				bundleId,
				failureSummaryId = bundleId + "-failures",
				lanes = new Dictionary<string, object>
				{
					["semantic"] = new { status = "proven", artifact = "semantic-snapshot.txt" },
					["visual-avalonia"] = new { status = "proven", artifact = "avalonia-visual.png" },
					["visual-winforms"] = new
					{
						status = (File.Exists(winFormsBaseline) && winFormsScenarioMatches) ? "proven" : "pending",
						artifact = winFormsBaseline,
						note = "proxy baseline from scenario '" + winFormsProxyScenarioId
							+ "' (no first-slice-specific WinForms capture exists); sidecar ScenarioId "
							+ (winFormsScenarioMatches ? "matches the pinned proxy scenario" : "did NOT match — baseline may have been swapped")
					},
					["workflow-accessibility"] = new
					{
						status = "pending",
						artifact = "PreviewHostUiaTests.cs (PreviewHost_UiaTree_ExposesLegacyFieldLabels_InLegacyOrder)",
						note = workflowAccessibilitySuitePresent
							? "suite present and contains the names/order coverage, but it requires a manual desktop-environment UIA run (interactive session) to actually prove this evidence type; it is not invoked from this headless test"
							: "suite file not found, or missing the expected coverage — investigate before trusting this evidence type at all"
					},
					["performance"] = new
					{
						status = timingBaselinesPresent ? "proven" : "pending",
						artifact = "DataTreeTimingBaselines.json + region-manifest.md section 5",
						note = timingBaselinesPresent
							? "local generated baseline file found at " + timingBaselinesPath
							: "DataTreeTimingBaselines.json is gitignored/generated and was not found on disk in this run"
					}
				}
			};
			var manifestPath = Path.Combine(bundleDir, "bundle.json");
			File.WriteAllText(manifestPath, JsonConvert.SerializeObject(manifest, Formatting.Indented));

			Assert.That(new FileInfo(semanticPath).Length, Is.GreaterThan(0));
			Assert.That(new FileInfo(avaloniaPng).Length, Is.GreaterThan(0));
			Assert.That(File.Exists(winFormsBaseline), Is.True, "the committed legacy visual baseline is part of the bundle");
			Assert.That(winFormsScenarioMatches, Is.True,
				"the WinForms baseline's sidecar ScenarioId must match the scenario this bundle pins it to, "
				+ "so a mismatched/swapped baseline is caught rather than silently passing");
			Assert.That(workflowAccessibilitySuitePresent, Is.True,
				"the PreviewHostUiaTests suite backing the workflow-accessibility evidence must exist and retain its names/order coverage");
			TestContext.WriteLine("path3 bundle: " + manifestPath);
		}

		private static bool TryReadWinFormsScenarioId(string metadataPath, out string scenarioId)
		{
			scenarioId = null;
			if (!File.Exists(metadataPath))
				return false;

			var json = JObject.Parse(File.ReadAllText(metadataPath));
			scenarioId = json["ScenarioId"]?.Value<string>();
			return !string.IsNullOrEmpty(scenarioId);
		}

		private static string FindRepoRoot()
		{
			var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FieldWorks.sln")))
				dir = dir.Parent;
			Assert.That(dir, Is.Not.Null);
			return dir.FullName;
		}
	}
}
