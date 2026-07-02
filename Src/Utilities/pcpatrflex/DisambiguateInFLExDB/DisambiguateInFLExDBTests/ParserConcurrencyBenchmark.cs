// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Headless benchmark for concurrent bulk parsing (Parse All Words).
// NOT part of the normal test suite ([Explicit]); run by FullyQualifiedName filter against a
// real project whose path is given in the FW_BENCH_FWDATA environment variable.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GenerateHCConfig;
using NUnit.Framework;
using SIL.FieldWorks;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;

namespace SIL.DisambiguateInFLExDBTests
{
	[TestFixture]
	[Explicit("Performance benchmark; run manually with FW_BENCH_FWDATA set")]
	internal class ParserConcurrencyBenchmark
	{
		private LcmCache m_cache;

		[OneTimeSetUp]
		public void Setup()
		{
			string fwdata = Environment.GetEnvironmentVariable("FW_BENCH_FWDATA");
			Assert.That(fwdata, Is.Not.Null.And.Not.Empty, "Set FW_BENCH_FWDATA to a .fwdata path");
			Assert.That(File.Exists(fwdata), Is.True, "fwdata not found: " + fwdata);

			FwRegistryHelper.Initialize();
			FwUtils.InitializeIcu();
			var sync = new SingleThreadedSynchronizeInvoke();
			var logger = new ConsoleLogger(sync);
			var dirs = new NullFdoDirectories();
			var settings = new LcmSettings();
			var progress = new NullThreadedProgress(sync);
			var projId = new ProjectId(fwdata);
			m_cache = LcmCache.CreateCacheFromExistingData(projId, "en", logger, dirs, settings, progress);
		}

		[OneTimeTearDown]
		public void Teardown()
		{
			if (m_cache != null)
			{
				ProjectLockingService.UnlockCurrentProject(m_cache);
				m_cache.Dispose();
				m_cache = null;
			}
		}

		[Test]
		[Timeout(2400000)]
		public void Benchmark()
		{
			// Make sure we exercise the HermitCrab (parallelizable) path.
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				m_cache.LanguageProject.MorphologicalDataOA.ActiveParser = "HC");

			var allWordforms = m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances().ToList();
			int cores = Environment.ProcessorCount;

			// Keep the A/B subset small enough that the SERIAL pass finishes quickly.
			int limit = allWordforms.Count;
			string limitEnv = Environment.GetEnvironmentVariable("FW_BENCH_LIMIT");
			if (!string.IsNullOrEmpty(limitEnv) && int.TryParse(limitEnv, out int parsedLimit))
				limit = Math.Min(parsedLimit, allWordforms.Count);
			var wordforms = allWordforms.Take(limit).ToList();

			Log($"Project wordforms: {allWordforms.Count} (benchmarking {wordforms.Count})   logical cores: {cores}");

			using (var idleQueue = new IdleQueue { IsPaused = true })
			using (var worker = new ParserWorker(m_cache, null, t => { }, idleQueue, Path.GetTempPath()))
			{
				// One-time grammar load + JIT warm-up (parse a single word).
				var loadSw = Stopwatch.StartNew();
				worker.ParseAndUpdateWordforms(wordforms.Take(1).ToList(), ParserPriority.Low, false, 1);
				DrainIdle(idleQueue);
				loadSw.Stop();
				Log($"Grammar load + warm-up (1 word): {loadSw.ElapsedMilliseconds} ms");

				// Serial-only mode: just the full-project baseline (the true "pre" number).
				if (Environment.GetEnvironmentVariable("FW_BENCH_SERIAL_ONLY") == "1")
				{
					long s = MeasureParse(worker, wordforms, 1, "maxDop= 1 (serial)");
					MeasureFile(idleQueue, "        ");
					Log($"==> Full project serial ({wordforms.Count} wordforms): {s / 1000.0:0.0}s");
					return;
				}

				// Parallel-only mode: skip the (very slow) full-project serial baseline; just time
				// the default cap and full-core runs to report real post-change wall-clock.
				if (Environment.GetEnvironmentVariable("FW_BENCH_PARALLEL_ONLY") == "1")
				{
					int cap = Math.Max(1, Math.Min(cores - 1, 4));
					long capParse = MeasureParse(worker, wordforms, cap, $"maxDop={cap,2} (default cap)");
					MeasureFile(idleQueue, "        ");
					long fullParse = MeasureParse(worker, wordforms, cores, $"maxDop={cores,2} (all cores)");
					MeasureFile(idleQueue, "        ");
					Log($"==> Full project ({wordforms.Count} wordforms): {capParse / 1000.0:0.0}s at cap {cap}, {fullParse / 1000.0:0.0}s at {cores} cores");
					return;
				}

				long serialParse = MeasureParse(worker, wordforms, 1, "maxDop= 1");
				long serialFile = MeasureFile(idleQueue, "        ");

				// Sweep the degree of parallelism to find where scaling plateaus.
				var dops = new[] { 2, 4, 8, 12, 16, cores }.Where(d => d <= cores).Distinct().OrderBy(d => d).ToArray();
				Log("");
				Log($"{"DOP",4} | {"parse ms",9} | {"speedup",7} | cores-equiv");
				Log($"{1,4} | {serialParse,9} | {1.0,6:0.00}x | {1.0,4:0.0}");
				foreach (int dop in dops)
				{
					long p = MeasureParseQuiet(worker, wordforms, dop);
					MeasureFile(idleQueue, null);
					double sp = serialParse / (double) Math.Max(1, p);
					Log($"{dop,4} | {p,9} | {sp,6:0.00}x | {sp,4:0.0}");
				}

				long bulk = serialParse + serialFile;
				Log("");
				Log($"==> Serial split over {wordforms.Count} wordforms: parse {serialParse} ms ({100.0 * serialParse / Math.Max(1, bulk):0}%) " +
					$"vs file {serialFile} ms ({100.0 * serialFile / Math.Max(1, bulk):0}%)");
				Log($"==> Per-wordform serial parse {serialParse / (double) wordforms.Count:0.0} ms, file {serialFile / (double) wordforms.Count:0.0} ms");
			}
		}

		private long MeasureParse(ParserWorker worker, List<IWfiWordform> wfs, int dop, string label)
		{
			HCParser.DiagMorpherParseTicks = 0;
			HCParser.DiagGetMorphsTicks = 0;
			var sw = Stopwatch.StartNew();
			worker.ParseAndUpdateWordforms(wfs, ParserPriority.Low, false, dop);
			sw.Stop();
			long morpherMs = HCParser.DiagMorpherParseTicks / TimeSpan.TicksPerMillisecond;
			long getMorphsMs = HCParser.DiagGetMorphsTicks / TimeSpan.TicksPerMillisecond;
			Log($"{label}: parse {sw.ElapsedMilliseconds,7} ms  (summed across threads: morpher {morpherMs} ms, GetMorphs/readlock {getMorphsMs} ms)");
			return sw.ElapsedMilliseconds;
		}

		private long MeasureParseQuiet(ParserWorker worker, List<IWfiWordform> wfs, int dop)
		{
			var sw = Stopwatch.StartNew();
			worker.ParseAndUpdateWordforms(wfs, ParserPriority.Low, false, dop);
			sw.Stop();
			return sw.ElapsedMilliseconds;
		}

		private long MeasureFile(IdleQueue q, string label)
		{
			var sw = Stopwatch.StartNew();
			DrainIdle(q);
			sw.Stop();
			if (label != null)
				Log($"{label} : file  {sw.ElapsedMilliseconds,7} ms");
			return sw.ElapsedMilliseconds;
		}

		private void DrainIdle(IdleQueue q)
		{
			// Run filing repeatedly until the queue is empty (UpdateWordforms re-queues itself if
			// it can't complete; in this single-threaded benchmark it completes on the first pass).
			foreach (IdleQueueTask task in q.ToArray())
				task.Delegate(task.Parameter);
			q.Clear();
		}

		private static void Log(string msg)
		{
			TestContext.Progress.WriteLine(msg);
			Console.WriteLine(msg);
		}
	}
}
