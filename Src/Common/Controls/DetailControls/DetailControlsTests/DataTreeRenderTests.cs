// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.RenderVerification;
using VerifyTests;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Snapshot tests using Verify for pixel-perfect validation of the full DataTree edit view,
	/// including WinForms chrome (grey labels, icons, section headers, separators) and
	/// Views engine text content (rendered via VwDrawRootBuffered overlay).
	/// </summary>
	/// <remarks>
	/// These tests exercise the production DataTree/Slice rendering pipeline that FLEx uses
	/// to display the lexical entry edit view. Unlike the RootSiteTests lex entry scenarios
	/// (which only test Views engine text rendering), these capture the full UI composition.
	/// We use InnerVerifier directly because Verify.NUnit requires NUnit 4.x and FieldWorks
	/// pins NUnit 3.13.3.
	/// </remarks>
	[TestFixture]
	public class DataTreeRenderTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ILexEntry m_entry;

		#region Scenario Data Creation

		/// <summary>
		/// Creates a simple lex entry with 3 senses for the "simple" scenario.
		/// </summary>
		private void CreateSimpleEntry()
		{
			// No UoW wrapper needed — base class opens one in TestSetup.
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			// Set lexeme form
			var morphFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFactory.Create();
			m_entry.LexemeFormOA = morph;
			morph.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString(
				"faire", Cache.DefaultVernWs);

			// Citation form
			m_entry.CitationForm.VernacularDefaultWritingSystem = TsStringUtils.MakeString(
				"faire", Cache.DefaultVernWs);

			// Add 3 senses
			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			for (int i = 1; i <= 3; i++)
			{
				var sense = senseFactory.Create();
				m_entry.SensesOS.Add(sense);
				sense.Gloss.AnalysisDefaultWritingSystem = TsStringUtils.MakeString(
					$"sense {i} gloss", Cache.DefaultAnalWs);
				sense.Definition.AnalysisDefaultWritingSystem = TsStringUtils.MakeString(
					$"Definition for sense {i} of the entry", Cache.DefaultAnalWs);
			}

			// Enrich with additional ifdata fields
			EnrichEntry(m_entry, "faire");
		}

		/// <summary>
		/// Creates a lex entry with nested senses (4 levels deep, 2 wide).
		/// </summary>
		private void CreateDeepEntry()
		{
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			var morphFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFactory.Create();
			m_entry.LexemeFormOA = morph;
			morph.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString(
				"benchmark-deep", Cache.DefaultVernWs);

			m_entry.CitationForm.VernacularDefaultWritingSystem = TsStringUtils.MakeString(
				"benchmark-deep", Cache.DefaultVernWs);

			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			CreateNestedSenses(m_entry, senseFactory, 4, 2, "", 1);

			EnrichEntry(m_entry, "benchmark-deep");
		}

		/// <summary>
		/// Creates a lex entry with extreme nesting (6 levels deep, 2 wide = 126 senses).
		/// </summary>
		private void CreateExtremeEntry()
		{
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			var morphFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFactory.Create();
			m_entry.LexemeFormOA = morph;
			morph.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString(
				"benchmark-extreme", Cache.DefaultVernWs);

			m_entry.CitationForm.VernacularDefaultWritingSystem = TsStringUtils.MakeString(
				"benchmark-extreme", Cache.DefaultVernWs);

			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			CreateNestedSenses(m_entry, senseFactory, 6, 2, "", 1);

			EnrichEntry(m_entry, "benchmark-extreme");
		}

		private void CreateNestedSenses(ICmObject owner, ILexSenseFactory senseFactory,
			int remainingDepth, int breadth, string prefix, int startNumber)
		{
			if (remainingDepth <= 0) return;

			for (int i = 0; i < breadth; i++)
			{
				int num = startNumber + i;
				string senseNum = string.IsNullOrEmpty(prefix) ? num.ToString() : $"{prefix}.{num}";

				var sense = senseFactory.Create();

				if (owner is ILexEntry entry)
					entry.SensesOS.Add(sense);
				else if (owner is ILexSense parentSense)
					parentSense.SensesOS.Add(sense);

				sense.Gloss.AnalysisDefaultWritingSystem = TsStringUtils.MakeString(
					$"gloss {senseNum}", Cache.DefaultAnalWs);
				sense.Definition.AnalysisDefaultWritingSystem = TsStringUtils.MakeString(
					$"Definition for sense {senseNum}", Cache.DefaultAnalWs);

				// Recurse into subsenses
				CreateNestedSenses(sense, senseFactory, remainingDepth - 1, breadth, senseNum, 1);
			}
		}

		/// <summary>
		/// Enriches a lex entry with additional fields that trigger ifdata layout parts.
		/// These populate Pronunciation, LiteralMeaning, Bibliography, Restrictions,
		/// and SummaryDefinition slices in the production "Normal" layout.
		/// Etymology is intentionally excluded because its SummarySlice creates native
		/// COM views that crash in test context.
		/// </summary>
		private void EnrichEntry(ILexEntry entry, string word)
		{
			// Pronunciation (ifdata, owned sequence object)
			var pronFactory = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>();
			var pronunciation = pronFactory.Create();
			entry.PronunciationsOS.Add(pronunciation);
			pronunciation.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString(
				$"/{word}/", Cache.DefaultVernWs);

			// Simple string ifdata fields
			entry.LiteralMeaning.AnalysisDefaultWritingSystem = TsStringUtils.MakeString(
				$"literal meaning of {word}", Cache.DefaultAnalWs);
			entry.Bibliography.AnalysisDefaultWritingSystem = TsStringUtils.MakeString(
				$"See: Larousse, p. 423; Oxford Dictionary, p. 198", Cache.DefaultAnalWs);
			entry.Restrictions.AnalysisDefaultWritingSystem = TsStringUtils.MakeString(
				"Formal register only", Cache.DefaultAnalWs);
			entry.SummaryDefinition.AnalysisDefaultWritingSystem = TsStringUtils.MakeString(
				$"A common verb meaning '{word}'", Cache.DefaultAnalWs);
		}

		#endregion

		#region Verify Infrastructure

		/// <summary>
		/// Returns the directory containing this source file (resolved at compile time).
		/// Verify stores .verified.png baselines alongside the test source file.
		/// </summary>
		private static string GetSourceFileDirectory([CallerFilePath] string sourceFile = "")
			=> Path.GetDirectoryName(sourceFile);

		/// <summary>
		/// Runs a Verify snapshot comparison for a DataTree-rendered bitmap.
		/// Uses InnerVerifier directly because Verify.NUnit requires NUnit 4.x
		/// and FieldWorks pins NUnit 3.13.3.
		/// </summary>
		private async Task VerifyDataTreeBitmap(Bitmap bitmap, string scenarioId)
		{
			using (var stream = new MemoryStream())
			{
				bitmap.Save(stream, ImageFormat.Png);
				stream.Position = 0;

				string directory = GetSourceFileDirectory();
				string name = $"DataTreeRenderTests.DataTreeRender_{scenarioId}";

				using (var verifier = new InnerVerifier(directory, name))
				{
					await verifier.VerifyStream(stream, "png", null);
				}
			}
		}

		#endregion

		#region Snapshot Tests

		/// <summary>
		/// Verifies the full DataTree rendering for a simple lex entry with 3 senses.
		/// Captures grey labels, WS indicators, sense summaries, all WinForms chrome.
		/// Uses production layouts from DistFiles to get the full view.
		/// </summary>
		[Test]
		public async Task DataTreeRender_Simple()
		{
			CreateSimpleEntry();

			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 768, true);
				DumpSliceDiagnostics(harness, "Simple");

				Assert.That(harness.SliceCount, Is.GreaterThan(0),
					"DataTree should have populated some slices");

				var bitmap = harness.CaptureCompositeBitmap();
				Assert.That(bitmap, Is.Not.Null, "Composite bitmap capture should succeed");

				// Content density check
				double density = CalculateNonWhiteDensity(bitmap);
				Console.WriteLine($"[DATATREE] Non-white density: {density:F2}%");
				Console.WriteLine($"[DATATREE] Bitmap size: {bitmap.Width}x{bitmap.Height}");

				await VerifyDataTreeBitmap(bitmap, "simple");
			}
		}

		/// <summary>
		/// Verifies the full DataTree rendering for a deeply nested lex entry.
		/// Tests recursive slice indentation and tree line rendering.
		/// </summary>
		[Test]
		public async Task DataTreeRender_Deep()
		{
			CreateDeepEntry();

			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 1200, true);
				DumpSliceDiagnostics(harness, "Deep");

				Assert.That(harness.SliceCount, Is.GreaterThan(0),
					"DataTree should have populated some slices");

				var bitmap = harness.CaptureCompositeBitmap();
				Assert.That(bitmap, Is.Not.Null, "Composite bitmap capture should succeed");

				double density = CalculateNonWhiteDensity(bitmap);
				Console.WriteLine($"[DATATREE] Non-white density: {density:F2}%");
				Console.WriteLine($"[DATATREE] Bitmap size: {bitmap.Width}x{bitmap.Height}");

				await VerifyDataTreeBitmap(bitmap, "deep");
			}
		}

		/// <summary>
		/// Verifies the full DataTree rendering for an extreme nesting scenario.
		/// 6-level nesting with 126 senses exercises the full slice rendering pipeline.
		/// </summary>
		[Test]
		public async Task DataTreeRender_Extreme()
		{
			CreateExtremeEntry();

			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 2400, true);
				DumpSliceDiagnostics(harness, "Extreme");

				Assert.That(harness.SliceCount, Is.GreaterThan(0),
					"DataTree should have populated some slices");

				var bitmap = harness.CaptureCompositeBitmap();
				Assert.That(bitmap, Is.Not.Null, "Composite bitmap capture should succeed");

				double density = CalculateNonWhiteDensity(bitmap);
				Console.WriteLine($"[DATATREE] Non-white density: {density:F2}%");
				Console.WriteLine($"[DATATREE] Bitmap size: {bitmap.Width}x{bitmap.Height}");

				await VerifyDataTreeBitmap(bitmap, "extreme");
			}
		}

		#endregion

		#region Timing Tests

		/// <summary>
		/// Benchmarks DataTree population time at varying nesting depths.
		/// Reports the exponential growth in slice creation and rendering time.
		/// </summary>
		[Test]
		[TestCase(2, 3, "shallow", Description = "Depth 2, breadth 3 = 12 senses")]
		[TestCase(4, 2, "deep", Description = "Depth 4, breadth 2 = 30 senses")]
		[TestCase(6, 2, "extreme", Description = "Depth 6, breadth 2 = 126 senses")]
		public void DataTreeTiming(int depth, int breadth, string label)
		{
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var morphFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFactory.Create();
			m_entry.LexemeFormOA = morph;
			morph.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString(
				$"timing-{label}", Cache.DefaultVernWs);

			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			CreateNestedSenses(m_entry, senseFactory, depth, breadth, "", 1);

			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 2400, true);

				Console.WriteLine($"[DATATREE-TIMING] {label}: " +
					$"Init={harness.LastTiming.InitializationMs:F1}ms, " +
					$"Populate={harness.LastTiming.PopulateSlicesMs:F1}ms, " +
					$"Total={harness.LastTiming.TotalMs:F1}ms, " +
					$"Slices={harness.LastTiming.SliceCount}");

				Assert.That(harness.SliceCount, Is.GreaterThan(0),
					$"DataTree should create slices for {label} scenario");

				// Capture bitmap to exercise the full pipeline
				var bitmap = harness.CaptureCompositeBitmap();
				Assert.That(bitmap, Is.Not.Null, "Composite capture should succeed");

				double density = CalculateNonWhiteDensity(bitmap);
				Console.WriteLine($"[DATATREE-TIMING] {label}: Non-white density={density:F2}%");
			}
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Dumps detailed slice diagnostic information to console output for debugging.
		/// </summary>
		private static void DumpSliceDiagnostics(DataTreeRenderHarness harness, string label)
		{
			Console.WriteLine($"[DATATREE] === {label} Diagnostics ===");
			Console.WriteLine($"[DATATREE] Slice count: {harness.SliceCount}");
			Console.WriteLine($"[DATATREE] Populate time: {harness.LastTiming.PopulateSlicesMs:F1}ms");
			Console.WriteLine($"[DATATREE] Init time: {harness.LastTiming.InitializationMs:F1}ms");

			// Summary by type
			var typeCounts = harness.LastTiming.SliceDiagnostics
				.GroupBy(d => d.TypeName)
				.OrderByDescending(g => g.Count());
			foreach (var group in typeCounts)
			{
				Console.WriteLine($"[DATATREE]   {group.Key}: {group.Count()}");
			}

			// Individual slice details (first 30)
			int limit = Math.Min(harness.LastTiming.SliceDiagnostics.Count, 30);
			for (int i = 0; i < limit; i++)
			{
				var d = harness.LastTiming.SliceDiagnostics[i];
				Console.WriteLine($"[DATATREE]   [{d.Index}] {d.TypeName,-25} " +
					$"Label=\"{d.Label}\" Visible={d.Visible} " +
					$"Bounds={d.Bounds} HasRootBox={d.HasRootBox}");
			}
			if (harness.LastTiming.SliceDiagnostics.Count > 30)
			{
				Console.WriteLine($"[DATATREE]   ... ({harness.LastTiming.SliceDiagnostics.Count - 30} more)");
			}
		}

		private static double CalculateNonWhiteDensity(Bitmap bitmap)
		{
			int nonWhiteCount = 0;
			int totalPixels = bitmap.Width * bitmap.Height;

			for (int y = 0; y < bitmap.Height; y++)
			{
				for (int x = 0; x < bitmap.Width; x++)
				{
					var pixel = bitmap.GetPixel(x, y);
					if (pixel.R < 250 || pixel.G < 250 || pixel.B < 250)
						nonWhiteCount++;
				}
			}

			return totalPixels > 0 ? (nonWhiteCount * 100.0 / totalPixels) : 0.0;
		}

		#endregion
	}
}
