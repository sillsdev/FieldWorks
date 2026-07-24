// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using SIL.FieldWorks.Common.RenderVerification;
using SIL.LCModel;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Task 2.13 (refresh-after-edit path): measures the legacy DataTree's re-show cost — a second
	/// <c>ShowObject</c> on the live tree after a model edit, which exercises the slice-reuse
	/// (ObjSeqHashMap) refresh path RecordEditView drives on record navigation and refresh. Numbers
	/// accumulate into the same <c>Output/RenderBenchmarks/datatree-timings.json</c> artifact the
	/// entry-open baselines use and feed region-manifest §5.
	/// </summary>
	[TestFixture]
	public class DataTreeReshowTimingTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void ReshowAfterEdit_OnLiveDataTree_IsMeasuredAndRecorded()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = morph;
			morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("reshow-entry", Cache.DefaultVernWs));
			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			for (var i = 0; i < 3; i++)
			{
				var sense = senseFactory.Create();
				entry.SensesOS.Add(sense);
				sense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString($"gloss {i}", Cache.DefaultAnalWs));
			}

			using (var harness = new DataTreeRenderHarness(Cache, entry))
			{
				harness.PopulateSlices();
				var openMs = harness.LastTiming.PopulateSlicesMs;
				var sliceCount = harness.SliceCount;
				Assert.That(sliceCount, Is.GreaterThan(0));

				// The edit a refresh would follow.
				entry.CitationForm.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("edited", Cache.DefaultVernWs));

				// Refresh on the live tree: RefreshList(false) is the rebuild path legacy refresh
				// drives for the same object (a same-root ShowObject early-outs at DataTree.cs:1073).
				var stopwatch = Stopwatch.StartNew();
				harness.DataTree.RefreshList(false);
				stopwatch.Stop();
				var reshowMs = stopwatch.Elapsed.TotalMilliseconds;

				TestContext.WriteLine(
					$"[DATATREE-TIMING] open={openMs:F1}ms reshow-after-edit={reshowMs:F1}ms slices={sliceCount}");
				Assert.That(reshowMs, Is.GreaterThanOrEqualTo(0));

				RecordTiming("timing-reshow-after-edit", sliceCount, openMs, reshowMs);
			}
		}

		private static void RecordTiming(string scenario, int slices, double openMs, double reshowMs)
		{
			var outputDir = Path.Combine(
				AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Output", "RenderBenchmarks");
			Directory.CreateDirectory(outputDir);
			var filePath = Path.Combine(outputDir, "datatree-timings.json");

			Dictionary<string, object> allTimings = null;
			if (File.Exists(filePath))
				allTimings = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(filePath));
			allTimings = allTimings ?? new Dictionary<string, object>();

			allTimings[scenario] = new
			{
				slices,
				openMs = Math.Round(openMs, 1),
				reshowAfterEditMs = Math.Round(reshowMs, 1),
				timestamp = DateTime.UtcNow.ToString("o")
			};
			File.WriteAllText(filePath, JsonConvert.SerializeObject(allTimings, Formatting.Indented));
		}
	}
}
