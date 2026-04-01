// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.RenderVerification;
using SIL.Utils;

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
	/// Baselines are committed PNG files stored next to this test source.
	/// </remarks>
	[TestFixture]
	public class DataTreeRenderTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private const string DeterministicRenderFontFamily = "Segoe UI";
		private const int MaxAllowedPixelDifferences = 4;
		private ILexEntry m_entry;

		private static ITsString MakeRenderString(string value, int writingSystemHandle)
		{
			var propsBuilder = TsStringUtils.MakePropsBldr();
			propsBuilder.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, writingSystemHandle);
			propsBuilder.SetStrPropValue((int)FwTextPropType.ktptFontFamily,
				DeterministicRenderFontFamily);

			var stringBuilder = TsStringUtils.MakeStrBldr();
			stringBuilder.Replace(0, 0, value, propsBuilder.GetTextProps());
			return stringBuilder.GetString();
		}

		#region Scenario Data Creation

		/// <summary>
		/// Creates a simple lex entry with 3 senses for the "simple" scenario.
		/// All fields filled with predictable text: "FieldName - simple".
		/// </summary>
		private void CreateSimpleEntry()
		{
			const string testName = "simple";
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			var morphFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFactory.Create();
			m_entry.LexemeFormOA = morph;
			morph.Form.VernacularDefaultWritingSystem = MakeRenderString(
				$"LexemeForm - {testName}", Cache.DefaultVernWs);

			m_entry.CitationForm.VernacularDefaultWritingSystem = MakeRenderString(
				$"CitationForm - {testName}", Cache.DefaultVernWs);

			// Add 3 senses with predictable text
			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			for (int i = 1; i <= 3; i++)
			{
				var sense = senseFactory.Create();
				m_entry.SensesOS.Add(sense);
				FillSenseFields(sense, $"{i}", testName);
			}

			EnrichEntry(m_entry, testName);
		}

		/// <summary>
		/// Creates a lex entry with triple-nested senses (depth 3, breadth 2).
		/// 2 senses × 2 subsenses × 2 sub-sub-senses = 14 total senses (2+4+8).
		/// This is the "slow" scenario — realistic deeply nested entry.
		/// </summary>
		private void CreateDeepEntry()
		{
			const string testName = "deep";
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			var morphFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFactory.Create();
			m_entry.LexemeFormOA = morph;
			morph.Form.VernacularDefaultWritingSystem = MakeRenderString(
				$"LexemeForm - {testName}", Cache.DefaultVernWs);

			m_entry.CitationForm.VernacularDefaultWritingSystem = MakeRenderString(
				$"CitationForm - {testName}", Cache.DefaultVernWs);

			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			CreateNestedSenses(m_entry, senseFactory, 3, 2, "", 1, testName);

			EnrichEntry(m_entry, testName);
		}

		/// <summary>
		/// Creates a lex entry with sub-sub-sub senses (depth 4, breadth 2).
		/// 2 + 4 + 8 + 16 = 30 senses total.
		/// Used to validate deeper recursive rendering with hidden fields enabled.
		/// </summary>
		private void CreateSubSubSubEntry()
		{
			const string testName = "subsubsub-hidden";
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			var morphFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFactory.Create();
			m_entry.LexemeFormOA = morph;
			morph.Form.VernacularDefaultWritingSystem = MakeRenderString(
				$"LexemeForm - {testName}", Cache.DefaultVernWs);

			m_entry.CitationForm.VernacularDefaultWritingSystem = MakeRenderString(
				$"CitationForm - {testName}", Cache.DefaultVernWs);

			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			CreateNestedSenses(m_entry, senseFactory, 4, 2, "", 1, testName);

			EnrichEntry(m_entry, testName);
		}

		/// <summary>
		/// Creates a lex entry with extreme nesting (6 levels deep, 2 wide = 126 senses).
		/// Stress test for the DataTree slice rendering pipeline.
		/// </summary>
		private void CreateExtremeEntry()
		{
			const string testName = "extreme";
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			var morphFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFactory.Create();
			m_entry.LexemeFormOA = morph;
			morph.Form.VernacularDefaultWritingSystem = MakeRenderString(
				$"LexemeForm - {testName}", Cache.DefaultVernWs);

			m_entry.CitationForm.VernacularDefaultWritingSystem = MakeRenderString(
				$"CitationForm - {testName}", Cache.DefaultVernWs);

			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			CreateNestedSenses(m_entry, senseFactory, 6, 2, "", 1, testName);

			EnrichEntry(m_entry, testName);
		}

		private void CreateNestedSenses(ICmObject owner, ILexSenseFactory senseFactory,
			int remainingDepth, int breadth, string prefix, int startNumber, string testName)
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

				FillSenseFields(sense, senseNum, testName);

				// Recurse into subsenses
				CreateNestedSenses(sense, senseFactory, remainingDepth - 1, breadth, senseNum, 1, testName);
			}
		}

		/// <summary>
		/// Fills sense-level fields with predictable text: "FieldName - testName sense N".
		/// </summary>
		private void FillSenseFields(ILexSense sense, string senseNum, string testName)
		{
			sense.Gloss.AnalysisDefaultWritingSystem = MakeRenderString(
				$"Gloss - {testName} sense {senseNum}", Cache.DefaultAnalWs);
			sense.Definition.AnalysisDefaultWritingSystem = MakeRenderString(
				$"Definition - {testName} sense {senseNum}", Cache.DefaultAnalWs);
			sense.ScientificName = MakeRenderString(
				$"ScientificName - {testName} sense {senseNum}", Cache.DefaultAnalWs);
		}

		/// <summary>
		/// Enriches a lex entry with additional fields that trigger ifdata layout parts.
		/// All fields use predictable text: "FieldName - testName".
		/// Populates: Pronunciation, LiteralMeaning, Bibliography, Restrictions,
		/// SummaryDefinition, and Comment.
		/// Etymology is intentionally excluded because its SummarySlice creates native
		/// COM views that crash in test context.
		/// </summary>
		private void EnrichEntry(ILexEntry entry, string testName)
		{
			// Pronunciation (ifdata, owned sequence object)
			var pronFactory = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>();
			var pronunciation = pronFactory.Create();
			entry.PronunciationsOS.Add(pronunciation);
			pronunciation.Form.VernacularDefaultWritingSystem = MakeRenderString(
				$"Pronunciation - {testName}", Cache.DefaultVernWs);

			// MultiString ifdata fields
			entry.LiteralMeaning.AnalysisDefaultWritingSystem = MakeRenderString(
				$"LiteralMeaning - {testName}", Cache.DefaultAnalWs);
			entry.Bibliography.AnalysisDefaultWritingSystem = MakeRenderString(
				$"Bibliography - {testName}", Cache.DefaultAnalWs);
			entry.Restrictions.AnalysisDefaultWritingSystem = MakeRenderString(
				$"Restrictions - {testName}", Cache.DefaultAnalWs);
			entry.SummaryDefinition.AnalysisDefaultWritingSystem = MakeRenderString(
				$"SummaryDefinition - {testName}", Cache.DefaultAnalWs);
			entry.Comment.AnalysisDefaultWritingSystem = MakeRenderString(
				$"Comment - {testName}", Cache.DefaultAnalWs);
		}

		/// <summary>
		/// Creates a minimal lex entry with a single sense and no optional fields.
		/// Exercises the "collapsed" view — bare minimum rendering path.
		/// </summary>
		private void CreateCollapsedEntry()
		{
			const string testName = "collapsed";
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			var morphFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFactory.Create();
			m_entry.LexemeFormOA = morph;
			morph.Form.VernacularDefaultWritingSystem = MakeRenderString(
				$"LexemeForm - {testName}", Cache.DefaultVernWs);

			m_entry.CitationForm.VernacularDefaultWritingSystem = MakeRenderString(
				$"CitationForm - {testName}", Cache.DefaultVernWs);

			// Single sense — minimal entry, no enrichment
			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			var sense = senseFactory.Create();
			m_entry.SensesOS.Add(sense);
			FillSenseFields(sense, "1", testName);
		}

		/// <summary>
		/// Creates a fully enriched lex entry with all available optional fields populated.
		/// 4 senses with all sense-level fields, plus full entry enrichment.
		/// Exercises the "expanded" view — maximum slice count for fields we can safely render.
		/// </summary>
		private void CreateExpandedEntry()
		{
			const string testName = "expanded";
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			var morphFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFactory.Create();
			m_entry.LexemeFormOA = morph;
			morph.Form.VernacularDefaultWritingSystem = MakeRenderString(
				$"LexemeForm - {testName}", Cache.DefaultVernWs);

			m_entry.CitationForm.VernacularDefaultWritingSystem = MakeRenderString(
				$"CitationForm - {testName}", Cache.DefaultVernWs);

			// Multiple senses with all fields
			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			for (int i = 1; i <= 4; i++)
			{
				var sense = senseFactory.Create();
				m_entry.SensesOS.Add(sense);
				FillSenseFields(sense, $"{i}", testName);
			}

			// Full enrichment
			EnrichEntry(m_entry, testName);

			// Add a second pronunciation
			var pronFactory = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>();
			var pron2 = pronFactory.Create();
			m_entry.PronunciationsOS.Add(pron2);
			pron2.Form.VernacularDefaultWritingSystem = MakeRenderString(
				$"Pronunciation2 - {testName}", Cache.DefaultVernWs);
		}

		/// <summary>
		/// Creates a lex entry with values in multiple writing systems.
		/// Exercises the MultiStringSlice rendering with WS indicators.
		/// Fields filled with predictable text in both English and French.
		/// </summary>
		private void CreateMultiWsEntry()
		{
			const string testName = "multiws";
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();

			var morphFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFactory.Create();
			m_entry.LexemeFormOA = morph;

			morph.Form.VernacularDefaultWritingSystem = MakeRenderString(
				$"LexemeForm - {testName}", Cache.DefaultVernWs);

			m_entry.CitationForm.VernacularDefaultWritingSystem = MakeRenderString(
				$"CitationForm - {testName}", Cache.DefaultVernWs);

			int analWs = Cache.DefaultAnalWs;

			// Add a French writing system
			int frWs = analWs;
			try
			{
				var wsManager = Cache.ServiceLocator.WritingSystemManager;
				CoreWritingSystemDefinition frWsDef;
				wsManager.GetOrSet("fr", out frWsDef);
				frWs = frWsDef.Handle;
				Cache.LanguageProject.AddToCurrentAnalysisWritingSystems(frWsDef);
			}
			catch
			{
				// If we can't create French WS, proceed with default analysis WS
			}

			// Senses with multi-WS text
			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			for (int i = 1; i <= 2; i++)
			{
				var sense = senseFactory.Create();
				m_entry.SensesOS.Add(sense);
				sense.Gloss.set_String(analWs, MakeRenderString(
					$"Gloss - {testName} sense {i} (en)", analWs));
				if (frWs != analWs)
				{
					sense.Gloss.set_String(frWs, MakeRenderString(
						$"Gloss - {testName} sens {i} (fr)", frWs));
				}
				sense.Definition.set_String(analWs, MakeRenderString(
					$"Definition - {testName} sense {i} (en)", analWs));
				if (frWs != analWs)
				{
					sense.Definition.set_String(frWs, MakeRenderString(
						$"Definition - {testName} sens {i} (fr)", frWs));
				}
			}

			// Multi-WS entry-level fields
			m_entry.LiteralMeaning.set_String(analWs, MakeRenderString(
				$"LiteralMeaning - {testName} (en)", analWs));
			if (frWs != analWs)
			{
				m_entry.LiteralMeaning.set_String(frWs, MakeRenderString(
					$"LiteralMeaning - {testName} (fr)", frWs));
			}

			m_entry.SummaryDefinition.set_String(analWs, MakeRenderString(
				$"SummaryDefinition - {testName} (en)", analWs));
			if (frWs != analWs)
			{
				m_entry.SummaryDefinition.set_String(frWs, MakeRenderString(
					$"SummaryDefinition - {testName} (fr)", frWs));
			}

			// Pronunciation
			var pronFactory = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>();
			var pronunciation = pronFactory.Create();
			m_entry.PronunciationsOS.Add(pronunciation);
			pronunciation.Form.VernacularDefaultWritingSystem = MakeRenderString(
				$"Pronunciation - {testName}", Cache.DefaultVernWs);
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
		/// Uses committed PNG baselines stored alongside the test source file.
		/// </summary>
		private async Task VerifyDataTreeBitmap(Bitmap bitmap, string scenarioId)
		{
			string directory = GetSourceFileDirectory();
			string name = $"DataTreeRenderTests.DataTreeRender_{scenarioId}";
			string verifiedPath = Path.Combine(directory, $"{name}.verified.png");
			string receivedPath = Path.Combine(directory, $"{name}.received.png");
			string diffPath = Path.Combine(directory, $"{name}.diff.png");

			if (File.Exists(diffPath))
				File.Delete(diffPath);

			if (!File.Exists(verifiedPath))
			{
				bitmap.Save(receivedPath, ImageFormat.Png);
				Assert.Fail(
					$"Missing verified render baseline for '{scenarioId}'. Review and accept {receivedPath} as the new baseline.");
			}

			using (var expectedBitmap = new Bitmap(verifiedPath))
			{
				int differentPixelCount = CountDifferentPixels(expectedBitmap, bitmap);
				if (differentPixelCount > MaxAllowedPixelDifferences)
				{
					using (var diffBitmap = CreateDiffBitmap(expectedBitmap, bitmap))
					{
						diffBitmap.Save(diffPath, ImageFormat.Png);
					}

					bitmap.Save(receivedPath, ImageFormat.Png);
					Assert.Fail(
						$"Render output for '{scenarioId}' differed from baseline by {differentPixelCount} pixels; " +
						$"{MaxAllowedPixelDifferences} or fewer differences are allowed. See {diffPath}.");
				}

				DeleteIfPresent(receivedPath);
				DeleteIfPresent(diffPath);
			}

			await Task.CompletedTask;
		}

		private static void DeleteIfPresent(string path)
		{
			if (File.Exists(path))
				File.Delete(path);
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

		private static int ScaleDiffChannel(int expected, int actual)
		{
			return Math.Min(255, Math.Abs(expected - actual) * 4);
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

				double density = CalculateNonWhiteDensity(bitmap);
				Console.WriteLine($"[DATATREE] Non-white density: {density:F2}%");
				Console.WriteLine($"[DATATREE] Bitmap size: {bitmap.Width}x{bitmap.Height}");

				RecordTiming("simple", 1, 3, harness.LastTiming, density);
				await VerifyDataTreeBitmap(bitmap, "simple");
			}
		}

		/// <summary>
		/// Verifies the full DataTree rendering for a triple-nested lex entry.
		/// 2 senses × 2 subsenses × 2 sub-sub-senses = 14 total senses.
		/// This is the "slow" scenario for realistic deep nesting.
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

				RecordTiming("deep", 3, 2, harness.LastTiming, density);
				await VerifyDataTreeBitmap(bitmap, "deep");
			}
		}

		/// <summary>
		/// Verifies a depth-4 lexeme edit tree can render recursive senses while hidden
		/// entry fields are forced visible. The hidden-field layout is test-only, but the
		/// render path is the real DataTree pipeline.
		/// </summary>
		[Test]
		public async Task DataTreeRender_SubSubSubSenses_ShowHiddenFields()
		{
			CreateSubSubSubEntry();
			const string layoutName = "NormalWithHiddenFieldsIndented";
			var hiddenLabels = new[]
			{
				"Bibliography",
				"Comment",
				"Literal Meaning",
				"Restrictions",
				"Summary Definition"
			};

			using (var withoutHiddenFields = new DataTreeRenderHarness(Cache, m_entry, layoutName))
			using (var withHiddenFields = new DataTreeRenderHarness(Cache, m_entry, layoutName, showHiddenFields: true))
			{
				withoutHiddenFields.PopulateSlices(1024, 2400, false);
				withHiddenFields.PopulateSlices(1024, 2400, false);

				var labelsWithoutHiddenFields = withoutHiddenFields.LastTiming.SliceDiagnostics
					.Select(diag => diag.Label)
					.Where(label => !string.IsNullOrEmpty(label))
					.ToList();
				var labelsWithHiddenFields = withHiddenFields.LastTiming.SliceDiagnostics
					.Select(diag => diag.Label)
					.Where(label => !string.IsNullOrEmpty(label))
					.ToList();

				foreach (var hiddenLabel in hiddenLabels)
				{
					Assert.That(labelsWithoutHiddenFields, Does.Not.Contain(hiddenLabel),
						$"{hiddenLabel} should stay hidden when ShowHiddenFields is off.");
					Assert.That(labelsWithHiddenFields, Does.Contain(hiddenLabel),
						$"{hiddenLabel} should be rendered when ShowHiddenFields is enabled.");
				}

				Assert.That(withHiddenFields.SliceCount, Is.GreaterThan(withoutHiddenFields.SliceCount),
					"Enabling hidden fields should increase the number of rendered slices.");

				int glossSliceCount = labelsWithHiddenFields.Count(label => label == "Gloss");
				int scientificNameSliceCount = labelsWithHiddenFields.Count(label => label == "ScientificName");
				Assert.That(glossSliceCount, Is.GreaterThanOrEqualTo(30),
					$"Expected at least 30 gloss slices for the depth-4 sense tree, but saw {glossSliceCount}.");
				Assert.That(scientificNameSliceCount, Is.GreaterThanOrEqualTo(30),
					$"Expected at least 30 scientific-name slices for the depth-4 sense tree, but saw {scientificNameSliceCount}.");

				int maxIndent = withHiddenFields.DataTree.Slices.Cast<Slice>().Max(slice => slice.Indent);
				Assert.That(maxIndent, Is.GreaterThanOrEqualTo(3),
					$"Expected nested subsenses to create at least 3 levels of indentation, but saw {maxIndent}.");

				var bitmap = withHiddenFields.CaptureCompositeBitmap();
				Assert.That(bitmap, Is.Not.Null, "Composite bitmap capture should succeed with hidden fields enabled.");
				double density = CalculateNonWhiteDensity(bitmap);
				RecordTiming("subsubsub-hidden", 4, 2, withHiddenFields.LastTiming, density);
				await VerifyDataTreeBitmap(bitmap, "subsubsub-hidden");
				bitmap.Dispose();
			}
		}

		/// <summary>
		/// Verifies a production-like lexeme edit snapshot for the depth-4 hidden-fields scenario.
		/// This keeps the focused hidden-field regression separate while adding top-matter coverage
		/// closer to the real lexeme edit view.
		/// </summary>
		[Test]
		public async Task DataTreeRender_SubSubSubSenses_ShowHiddenFields_ProductionLike()
		{
			CreateSubSubSubEntry();
			const string layoutName = "ProductionLikeWithHiddenFieldsIndented";

			using (var harness = new DataTreeRenderHarness(Cache, m_entry, layoutName, showHiddenFields: true))
			{
				harness.PopulateSlices(1024, 2600, false);

				var labels = harness.LastTiming.SliceDiagnostics
					.Select(diag => diag.Label)
					.Where(label => !string.IsNullOrEmpty(label))
					.ToList();

				Assert.That(labels, Does.Contain("Lexeme Form"),
					"Production-like scenario should include the lexeme form top matter.");
				Assert.That(labels, Does.Contain("Citation Form"),
					"Production-like scenario should include the citation form top matter.");
				Assert.That(labels, Does.Contain("Pronunciation"),
					"Production-like scenario should include pronunciation top matter.");
				Assert.That(labels, Does.Contain("Bibliography"));
				Assert.That(labels, Does.Contain("Comment"));
				Assert.That(labels, Does.Contain("Literal Meaning"));
				Assert.That(labels, Does.Contain("Restrictions"));
				Assert.That(labels, Does.Contain("Summary Definition"));

				int maxIndent = harness.DataTree.Slices.Cast<Slice>().Max(slice => slice.Indent);
				Assert.That(maxIndent, Is.GreaterThanOrEqualTo(3),
					$"Expected nested subsenses to create at least 3 levels of indentation, but saw {maxIndent}.");

				var bitmap = harness.CaptureCompositeBitmap();
				Assert.That(bitmap, Is.Not.Null, "Composite bitmap capture should succeed for the production-like layout.");

				double density = CalculateNonWhiteDensity(bitmap);
				RecordTiming("subsubsub-hidden-productionlike", 4, 2, harness.LastTiming, density);
				await VerifyDataTreeBitmap(bitmap, "subsubsub-hidden-productionlike");
				bitmap.Dispose();
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

				RecordTiming("extreme", 6, 2, harness.LastTiming, density);
				await VerifyDataTreeBitmap(bitmap, "extreme");
			}
		}

		/// <summary>
		/// Verifies the DataTree rendering for a minimal entry with a single sense.
		/// Exercises the bare minimum rendering path — collapsed view.
		/// </summary>
		[Test]
		public async Task DataTreeRender_Collapsed()
		{
			CreateCollapsedEntry();

			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 400, true);
				DumpSliceDiagnostics(harness, "Collapsed");

				Assert.That(harness.SliceCount, Is.GreaterThan(0),
					"DataTree should have populated some slices");

				var bitmap = harness.CaptureCompositeBitmap();
				Assert.That(bitmap, Is.Not.Null, "Composite bitmap capture should succeed");

				double density = CalculateNonWhiteDensity(bitmap);
				Console.WriteLine($"[DATATREE] Non-white density: {density:F2}%");
				Console.WriteLine($"[DATATREE] Bitmap size: {bitmap.Width}x{bitmap.Height}");

				RecordTiming("collapsed", 1, 1, harness.LastTiming, density);
				await VerifyDataTreeBitmap(bitmap, "collapsed");
			}
		}

		/// <summary>
		/// Verifies the DataTree rendering for a fully enriched entry with all optional
		/// fields populated. Maximum slice count with multiple pronunciations,
		/// scientific names, and extended definitions.
		/// </summary>
		[Test]
		public async Task DataTreeRender_Expanded()
		{
			CreateExpandedEntry();

			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 1200, true);
				DumpSliceDiagnostics(harness, "Expanded");

				Assert.That(harness.SliceCount, Is.GreaterThan(0),
					"DataTree should have populated some slices");

				var bitmap = harness.CaptureCompositeBitmap();
				Assert.That(bitmap, Is.Not.Null, "Composite bitmap capture should succeed");

				double density = CalculateNonWhiteDensity(bitmap);
				Console.WriteLine($"[DATATREE] Non-white density: {density:F2}%");
				Console.WriteLine($"[DATATREE] Bitmap size: {bitmap.Width}x{bitmap.Height}");

				RecordTiming("expanded", 1, 4, harness.LastTiming, density);
				await VerifyDataTreeBitmap(bitmap, "expanded");
			}
		}

		/// <summary>
		/// Verifies the DataTree rendering for an entry with multiple writing systems.
		/// Exercises MultiStringSlice WS indicators and font fallback across French and English.
		/// </summary>
		[Test]
		public async Task DataTreeRender_MultiWs()
		{
			CreateMultiWsEntry();

			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 768, true);
				DumpSliceDiagnostics(harness, "MultiWs");

				Assert.That(harness.SliceCount, Is.GreaterThan(0),
					"DataTree should have populated some slices");

				var bitmap = harness.CaptureCompositeBitmap();
				Assert.That(bitmap, Is.Not.Null, "Composite bitmap capture should succeed");

				double density = CalculateNonWhiteDensity(bitmap);
				Console.WriteLine($"[DATATREE] Non-white density: {density:F2}%");
				Console.WriteLine($"[DATATREE] Bitmap size: {bitmap.Width}x{bitmap.Height}");

				RecordTiming("multiws", 1, 2, harness.LastTiming, density);
				await VerifyDataTreeBitmap(bitmap, "multiws");
			}
		}

		#endregion

		#region Timing Tests

		/// <summary>
		/// Benchmarks DataTree population time at varying nesting depths.
		/// Reports the exponential growth in slice creation and rendering time.
		/// Writes timing results to Output/RenderBenchmarks/datatree-timings.json.
		/// </summary>
		[Test]
		[TestCase(2, 3, "shallow", Description = "Depth 2, breadth 3 = 12 senses")]
		[TestCase(3, 2, "deep", Description = "Depth 3, breadth 2 = 14 senses (triple-nested)")]
		[TestCase(6, 2, "extreme", Description = "Depth 6, breadth 2 = 126 senses")]
		public void DataTreeTiming(int depth, int breadth, string label)
		{
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var morphFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFactory.Create();
			m_entry.LexemeFormOA = morph;
			morph.Form.VernacularDefaultWritingSystem = MakeRenderString(
				$"LexemeForm - timing-{label}", Cache.DefaultVernWs);

			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			CreateNestedSenses(m_entry, senseFactory, depth, breadth, "", 1, $"timing-{label}");
			EnrichEntry(m_entry, $"timing-{label}");

			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				// Use test inventories for timing to keep recursive sense growth active
				// while avoiding known production-layout crash-only parts in test context.
				harness.PopulateSlices(1024, 2400, false);

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

				// Record timing to file
				RecordTiming($"timing-{label}", depth, breadth, harness.LastTiming, density);
			}
		}

		/// <summary>
		/// Measures paint/capture time for the extreme scenario.
		/// Exercises the full OnPaint → HandlePaintLinesBetweenSlices pipeline
		/// via DrawToBitmap. This provides a baseline for paint optimizations
		/// (clip-rect culling, double-buffering).
		/// </summary>
		[Test]
		public void DataTreeTiming_PaintPerformance()
		{
			CreateExtremeEntry();
			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 2400, false);
				Assert.That(harness.SliceCount, Is.GreaterThan(0),
					"DataTree should create slices for extreme scenario");

				// Warm-up capture (first paint triggers handle creation, layout convergence, etc.)
				var warmup = harness.CaptureCompositeBitmap();
				Assert.That(warmup, Is.Not.Null, "Warm-up capture should succeed");
				warmup.Dispose();

				// Timed capture: DrawToBitmap → OnPaint → HandlePaintLinesBetweenSlices
				var sw = System.Diagnostics.Stopwatch.StartNew();
				var bitmap = harness.CaptureCompositeBitmap();
				sw.Stop();

				Assert.That(bitmap, Is.Not.Null, "Timed capture should succeed");
				double captureMs = sw.Elapsed.TotalMilliseconds;
				Console.WriteLine($"[PAINT-TIMING] Extreme scenario capture: {captureMs:F1}ms");
				Console.WriteLine($"[PAINT-TIMING] Slices: {harness.SliceCount}, Bitmap: {bitmap.Width}x{bitmap.Height}");

				RecordTiming("paint-extreme", 6, 2, harness.LastTiming,
					CalculateNonWhiteDensity(bitmap));
				bitmap.Dispose();
			}
		}

		/// <summary>
		/// Verifies benchmark workload grows with scenario complexity.
		/// This guards against timing tests that accidentally stop exercising deeper data.
		/// </summary>
		[Test]
		public void DataTreeTiming_WorkloadGrowsWithComplexity()
		{
			int shallowSlices = RunTimingScenarioAndGetSliceCount(2, 3, "growth-shallow");
			int deepSlices = RunTimingScenarioAndGetSliceCount(3, 2, "growth-deep");
			int extremeSlices = RunTimingScenarioAndGetSliceCount(6, 2, "growth-extreme");

			Assert.That(deepSlices, Is.GreaterThan(shallowSlices),
				$"Expected deep workload to exceed shallow workload, but got shallow={shallowSlices}, deep={deepSlices}");
			Assert.That(extremeSlices, Is.GreaterThan(deepSlices),
				$"Expected extreme workload to exceed deep workload, but got deep={deepSlices}, extreme={extremeSlices}");
		}

		[Test]
		public void DataTreeTimingBaselines_CoverAllSnapshotScenarios()
		{
			DataTreeTimingBaselineCatalog.AssertSnapshotCoverage();
		}

		#endregion

		#region Optimization Regression Tests

		/// <summary>
		/// Verifies that all visible slices in the viewport have consistent width after
		/// layout converges. Exercises Enhancement 5 (SetWidthForDataTreeLayout early-exit):
		/// after the first layout pass sets widths, subsequent passes should be no-ops.
		/// </summary>
		[Test]
		public void DataTreeOpt_WidthStabilityAfterLayout()
		{
			CreateDeepEntry();
			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 2400, false);
				Assert.That(harness.SliceCount, Is.GreaterThan(0),
					"Should have slices");

				// Record widths after initial layout convergence
				var dt = harness.DataTree;
				int desiredWidth = dt.ClientRectangle.Width;
				var initialWidths = new int[dt.Slices.Count];
				for (int i = 0; i < dt.Slices.Count; i++)
					initialWidths[i] = ((Slice)dt.Slices[i]).Width;

				// Force a second paint/layout pass — widths should remain identical
				var bitmap = harness.CaptureCompositeBitmap();
				Assert.That(bitmap, Is.Not.Null, "Second paint should succeed");
				bitmap.Dispose();

				for (int i = 0; i < dt.Slices.Count; i++)
				{
					int currentWidth = ((Slice)dt.Slices[i]).Width;
					Assert.That(currentWidth, Is.EqualTo(initialWidths[i]),
						$"Slice [{i}] width changed after second layout pass " +
						$"(was {initialWidths[i]}, now {currentWidth}). " +
						$"Enhancement 5 early-exit should prevent width changes when stable.");
				}

				Console.WriteLine($"[OPT-TEST] Width stability: {dt.Slices.Count} slices " +
					$"all stable at width={desiredWidth}");
			}
		}

		/// <summary>
		/// Verifies that all slices in the viewport are marked Visible=true after layout.
		/// Exercises Enhancement 9 (MakeSliceVisible high-water mark): the optimization
		/// must not skip making any slice visible that should be visible.
		/// </summary>
		[Test]
		public void DataTreeOpt_AllViewportSlicesVisible()
		{
			CreateExtremeEntry();
			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				// Use a viewport smaller than total content to exercise partial visibility
				harness.PopulateSlices(1024, 800, false);
				Assert.That(harness.SliceCount, Is.GreaterThan(0),
					"Should have slices");

				// Force layout + paint to trigger MakeSliceVisible calls
				var bitmap = harness.CaptureCompositeBitmap();
				Assert.That(bitmap, Is.Not.Null, "Paint should succeed");
				bitmap.Dispose();

				var dt = harness.DataTree;
				var diagnostics = harness.LastTiming.SliceDiagnostics;

				// Every slice that is within the viewport bounds should be Visible
				int viewportBottom = dt.ClientRectangle.Height;
				int visibleCount = 0;
				int totalCount = 0;
				for (int i = 0; i < dt.Slices.Count; i++)
				{
					var slice = (Slice)dt.Slices[i];
					totalCount++;
					int sliceTop = slice.Top;
					int sliceBottom = sliceTop + slice.Height;

					// Slice intersects viewport
					if (sliceBottom > 0 && sliceTop < viewportBottom)
					{
						Assert.That(slice.Visible, Is.True,
							$"Slice [{i}] ({slice.GetType().Name}, Label=\"{slice.Label}\") " +
							$"at Y={sliceTop}-{sliceBottom} is in viewport (0-{viewportBottom}) " +
							$"but Visible=false. Enhancement 9 high-water mark may have skipped it.");
						visibleCount++;
					}
				}

				Assert.That(visibleCount, Is.GreaterThan(0),
					"At least one slice should be visible in the viewport");
				Console.WriteLine($"[OPT-TEST] Visibility: {visibleCount}/{totalCount} slices " +
					$"visible in viewport 0-{viewportBottom}");
			}
		}

		/// <summary>
		/// Verifies that the XML attribute cache on Slice returns the same results as
		/// direct XML parsing. Exercises Enhancement 8 (cached IsHeader/SkipSpacerLine/SameObject):
		/// the cache must be correct and must be invalidated when ConfigurationNode changes.
		/// </summary>
		[Test]
		public void DataTreeOpt_XmlCacheConsistency()
		{
			CreateSimpleEntry();
			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 1200, false);
				Assert.That(harness.SliceCount, Is.GreaterThan(0),
					"Should have slices");

				var dt = harness.DataTree;
				int checkedCount = 0;

				for (int i = 0; i < dt.Slices.Count; i++)
				{
					var slice = (Slice)dt.Slices[i];
					if (slice.ConfigurationNode == null)
						continue;

					// Get the cached property values
					bool cachedIsHeader = slice.IsHeader;
					bool cachedSkipSpacer = slice.SkipSpacerLine;
					bool cachedSameObj = slice.SameObject;

					// Get the ground truth from direct XML parsing
					bool directIsHeader = SIL.Utils.XmlUtils.GetOptionalBooleanAttributeValue(
						slice.ConfigurationNode, "header", false);
					bool directSkipSpacer = SIL.Utils.XmlUtils.GetOptionalBooleanAttributeValue(
						slice.ConfigurationNode, "skipSpacerLine", false);
					bool directSameObj = SIL.Utils.XmlUtils.GetOptionalBooleanAttributeValue(
						slice.ConfigurationNode, "sameObject", false);

					Assert.That(cachedIsHeader, Is.EqualTo(directIsHeader),
						$"Slice [{i}] IsHeader cache mismatch: cached={cachedIsHeader}, " +
						$"XML={directIsHeader}");
					Assert.That(cachedSkipSpacer, Is.EqualTo(directSkipSpacer),
						$"Slice [{i}] SkipSpacerLine cache mismatch: cached={cachedSkipSpacer}, " +
						$"XML={directSkipSpacer}");
					Assert.That(cachedSameObj, Is.EqualTo(directSameObj),
						$"Slice [{i}] SameObject cache mismatch: cached={cachedSameObj}, " +
						$"XML={directSameObj}");

					checkedCount++;
				}

				Assert.That(checkedCount, Is.GreaterThan(0),
					"Should have checked at least one slice's XML cache");
				Console.WriteLine($"[OPT-TEST] XML cache consistency: {checkedCount} slices verified");
			}
		}

		/// <summary>
		/// Verifies that ConfigurationNode setter invalidates the XML attribute cache.
		/// Re-setting the same ConfigurationNode should still produce correct results
		/// (cache re-populated from XML). Exercises Enhancement 8 cache invalidation.
		/// </summary>
		[Test]
		public void DataTreeOpt_XmlCacheInvalidationOnConfigChange()
		{
			CreateSimpleEntry();
			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 1200, false);
				Assert.That(harness.SliceCount, Is.GreaterThan(0),
					"Should have slices");

				var dt = harness.DataTree;

				// Find a slice with a ConfigurationNode to test cache invalidation
				Slice testSlice = null;
				for (int i = 0; i < dt.Slices.Count; i++)
				{
					var slice = (Slice)dt.Slices[i];
					if (slice.ConfigurationNode != null)
					{
						testSlice = slice;
						break;
					}
				}
				Assert.That(testSlice, Is.Not.Null,
					"Should find at least one slice with a ConfigurationNode");

				// Prime the cache by accessing each property
				bool originalIsHeader = testSlice.IsHeader;
				bool originalSkipSpacer = testSlice.SkipSpacerLine;
				bool originalSameObj = testSlice.SameObject;

				// Re-set the ConfigurationNode (triggers cache invalidation)
				var savedNode = testSlice.ConfigurationNode;
				testSlice.ConfigurationNode = savedNode;

				// Cache should be re-populated with same values
				Assert.That(testSlice.IsHeader, Is.EqualTo(originalIsHeader),
					"IsHeader should match after ConfigurationNode reset");
				Assert.That(testSlice.SkipSpacerLine, Is.EqualTo(originalSkipSpacer),
					"SkipSpacerLine should match after ConfigurationNode reset");
				Assert.That(testSlice.SameObject, Is.EqualTo(originalSameObj),
					"SameObject should match after ConfigurationNode reset");

				Console.WriteLine($"[OPT-TEST] Cache invalidation: verified on slice " +
					$"'{testSlice.Label}' ({testSlice.GetType().Name})");
			}
		}

		/// <summary>
		/// Verifies that multiple sequential paint captures produce identical output.
		/// Exercises all paint-path optimizations (Enhancement 3 clip-rect culling,
		/// Enhancement 4 double-buffering, Enhancement 7 paint-path width skip,
		/// Enhancement 8 XML caching): repeated paints must be deterministic.
		/// </summary>
		[Test]
		public void DataTreeOpt_SequentialPaintsProduceIdenticalOutput()
		{
			CreateDeepEntry();
			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 1200, false);
				Assert.That(harness.SliceCount, Is.GreaterThan(0),
					"Should have slices");

				// Warm-up to get past initial layout convergence
				var warmup = harness.CaptureCompositeBitmap();
				Assert.That(warmup, Is.Not.Null);
				warmup.Dispose();

				// Capture twice
				var capture1 = harness.CaptureCompositeBitmap();
				var capture2 = harness.CaptureCompositeBitmap();

				Assert.That(capture1, Is.Not.Null, "First capture should succeed");
				Assert.That(capture2, Is.Not.Null, "Second capture should succeed");

				Assert.That(capture2.Width, Is.EqualTo(capture1.Width),
					"Bitmap widths should match");
				Assert.That(capture2.Height, Is.EqualTo(capture1.Height),
					"Bitmap heights should match");

				// Compare pixel-by-pixel — paint must be deterministic
				int mismatchCount = 0;
				for (int y = 0; y < capture1.Height; y++)
				{
					for (int x = 0; x < capture1.Width; x++)
					{
						if (capture1.GetPixel(x, y) != capture2.GetPixel(x, y))
							mismatchCount++;
					}
				}

				double mismatchRate = (double)mismatchCount / (capture1.Width * capture1.Height) * 100;
				Assert.That(mismatchRate, Is.LessThan(0.1),
					$"Sequential paints differ in {mismatchCount} pixels ({mismatchRate:F3}%). " +
					$"Paint optimizations must produce deterministic output.");

				Console.WriteLine($"[OPT-TEST] Paint determinism: {capture1.Width}x{capture1.Height}, " +
					$"mismatch={mismatchCount} ({mismatchRate:F3}%)");

				capture1.Dispose();
				capture2.Dispose();
			}
		}

		/// <summary>
		/// Verifies that slice positions are monotonically increasing (each slice's Top
		/// is >= previous slice's Top + Height). This is a prerequisite for Enhancement 3's
		/// clip-rect culling early-break optimization in HandlePaintLinesBetweenSlices.
		/// </summary>
		[Test]
		public void DataTreeOpt_SlicePositionsMonotonicallyIncreasing()
		{
			CreateExtremeEntry();
			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 2400, false);
				Assert.That(harness.SliceCount, Is.GreaterThan(1),
					"Should have multiple slices to test ordering");

				var dt = harness.DataTree;
				int previousBottom = int.MinValue;
				int checkedCount = 0;

				for (int i = 0; i < dt.Slices.Count; i++)
				{
					var slice = (Slice)dt.Slices[i];
					int sliceTop = slice.Top;

					if (i > 0)
					{
						Assert.That(sliceTop, Is.GreaterThanOrEqualTo(previousBottom),
							$"Slice [{i}] ({slice.GetType().Name}, Label=\"{slice.Label}\") " +
							$"Top={sliceTop} overlaps previous slice bottom={previousBottom}. " +
							$"Monotonic ordering required for clip-rect culling (Enhancement 3).");
					}

					previousBottom = sliceTop + slice.Height;
					checkedCount++;
				}

				Console.WriteLine($"[OPT-TEST] Monotonic ordering: {checkedCount} slices verified");
			}
		}

		/// <summary>
		/// Verifies IsHeaderNode delegates correctly to the cached IsHeader property.
		/// The public IsHeaderNode property should always agree with the internal
		/// cached IsHeader value. Exercises Enhancement 8 delegation.
		/// </summary>
		[Test]
		public void DataTreeOpt_IsHeaderNodeDelegatesToCachedProperty()
		{
			CreateSimpleEntry();
			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 1200, false);
				Assert.That(harness.SliceCount, Is.GreaterThan(0),
					"Should have slices");

				var dt = harness.DataTree;
				int checkedCount = 0;

				for (int i = 0; i < dt.Slices.Count; i++)
				{
					var slice = (Slice)dt.Slices[i];
					if (slice.ConfigurationNode == null)
						continue;

					// IsHeaderNode (public) should delegate to IsHeader (internal cached)
					Assert.That(slice.IsHeaderNode, Is.EqualTo(slice.IsHeader),
						$"Slice [{i}] IsHeaderNode/IsHeader mismatch");
					checkedCount++;
				}

				Assert.That(checkedCount, Is.GreaterThan(0),
					"Should have checked at least one slice");
				Console.WriteLine($"[OPT-TEST] IsHeaderNode delegation: {checkedCount} slices verified");
			}
		}

		/// <summary>
		/// Verifies that slice positions set by the full layout pass (fFull=true,
		/// called from OnLayout) agree with the accumulated yTop values computed by
		/// the paint path (fFull=false, called from OnPaint). This is the core safety
		/// invariant for a future binary-search optimization: if the paint path skips
		/// iterating above-viewport slices, the positions it uses must match what
		/// the full layout established.
		/// Failure mode: yTop accumulator drift when the paint path doesn't walk all slices.
		/// </summary>
		[Test]
		public void DataTreeOpt_FullLayoutAndPaintPathPositionsAgree()
		{
			CreateExtremeEntry();
			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 2400, false);
				Assert.That(harness.SliceCount, Is.GreaterThan(0), "Should have slices");

				var dt = harness.DataTree;

				// Record positions after full layout (set by OnLayout → HandleLayout1(fFull=true))
				var fullLayoutPositions = new int[dt.Slices.Count];
				var fullLayoutHeights = new int[dt.Slices.Count];
				for (int i = 0; i < dt.Slices.Count; i++)
				{
					var slice = (Slice)dt.Slices[i];
					fullLayoutPositions[i] = slice.Top;
					fullLayoutHeights[i] = slice.Height;
				}

				// Force another paint-path layout by invalidating and pumping
				dt.Invalidate();
				System.Windows.Forms.Application.DoEvents();

				// Verify positions haven't drifted
				int checkedCount = 0;
				for (int i = 0; i < dt.Slices.Count; i++)
				{
					var slice = (Slice)dt.Slices[i];
					Assert.That(slice.Top, Is.EqualTo(fullLayoutPositions[i]),
						$"Slice [{i}] ({slice.GetType().Name}, Label=\"{slice.Label}\") " +
						$"Top drifted from {fullLayoutPositions[i]} to {slice.Top} " +
						$"after paint-path layout. Full→paint position agreement broken.");
					checkedCount++;
				}

				Console.WriteLine($"[OPT-TEST] Position agreement: {checkedCount} slices verified");
			}
		}

		/// <summary>
		/// Verifies that AutoScrollPosition does not drift across multiple paint passes.
		/// The paint path adjusts scroll position when slices above the viewport change
		/// height (e.g., DummyObjectSlice → real slice). After initial convergence,
		/// scroll position must be stable.
		/// Failure mode: binary search skips the desiredScrollPosition adjustment for
		/// above-viewport slices, causing scroll jumps.
		/// </summary>
		[Test]
		public void DataTreeOpt_ScrollPositionStableAcrossPaints()
		{
			CreateExtremeEntry();
			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				// Use a small viewport so most slices are below the fold
				harness.PopulateSlices(1024, 400, false);
				Assert.That(harness.SliceCount, Is.GreaterThan(0), "Should have slices");

				var dt = harness.DataTree;

				// Warm up — first paint triggers layout convergence
				var warmup = harness.CaptureCompositeBitmap();
				Assert.That(warmup, Is.Not.Null);
				warmup.Dispose();

				// Record scroll position after convergence
				var scrollAfterConvergence = dt.AutoScrollPosition;

				// Force multiple paint passes
				for (int pass = 0; pass < 3; pass++)
				{
					dt.Invalidate();
					System.Windows.Forms.Application.DoEvents();
				}

				Assert.That(dt.AutoScrollPosition, Is.EqualTo(scrollAfterConvergence),
					$"AutoScrollPosition drifted from {scrollAfterConvergence} to " +
					$"{dt.AutoScrollPosition} after 3 additional paint passes. " +
					$"Scroll stability is essential for binary-search correctness.");

				Console.WriteLine($"[OPT-TEST] Scroll stability: position={dt.AutoScrollPosition} " +
					$"stable across 3 additional paints");
			}
		}

		/// <summary>
		/// Verifies that all slices at or before the last visible slice are also visible.
		/// The .NET Framework has a bug (LT-7307) where making a slice visible when
		/// prior slices are invisible causes index corruption. The MakeSliceVisible
		/// method guarantees all preceding slices are visible before making the target
		/// visible. A binary search that starts mid-list must preserve this invariant.
		/// Failure mode: binary search skips MakeSliceVisible for slices 0..N-1, leaving
		/// gaps in the visibility sequence.
		/// </summary>
		[Test]
		public void DataTreeOpt_VisibilitySequenceHasNoGaps()
		{
			CreateExtremeEntry();
			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 800, false);
				Assert.That(harness.SliceCount, Is.GreaterThan(0), "Should have slices");

				// Force paint to trigger MakeSliceVisible
				var bitmap = harness.CaptureCompositeBitmap();
				Assert.That(bitmap, Is.Not.Null);
				bitmap.Dispose();

				var dt = harness.DataTree;

				// Find the highest-index visible slice
				int highestVisibleIndex = -1;
				for (int i = 0; i < dt.Slices.Count; i++)
				{
					if (((Slice)dt.Slices[i]).Visible)
						highestVisibleIndex = i;
				}

				Assert.That(highestVisibleIndex, Is.GreaterThan(0),
					"Should have multiple visible slices");

				// All slices 0..highestVisibleIndex must be visible (no gaps)
				int gapCount = 0;
				for (int i = 0; i <= highestVisibleIndex; i++)
				{
					var slice = (Slice)dt.Slices[i];
					if (!slice.Visible)
					{
						gapCount++;
						Console.WriteLine($"[OPT-TEST] Visibility gap at [{i}] " +
							$"({slice.GetType().Name}, Label=\"{slice.Label}\")");
					}
				}

				Assert.That(gapCount, Is.EqualTo(0),
					$"Found {gapCount} invisible slices before the last visible slice " +
					$"(index {highestVisibleIndex}). LT-7307: all preceding slices must " +
					$"be visible to prevent index corruption.");

				Console.WriteLine($"[OPT-TEST] Visibility sequence: no gaps in 0..{highestVisibleIndex}");
			}
		}

		/// <summary>
		/// Removing the last visible slice shifts the first invisible off-screen slice into the
		/// visible prefix. MakeSliceVisible must rebuild that prefix from the start on the next call;
		/// otherwise LT-7307 can be violated by skipping the shifted slice.
		/// </summary>
		[Test]
		public void DataTreeOpt_RemoveVisibleSliceInvalidatesHighWaterMark()
		{
			CreateExtremeEntry();
			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 800, false);
				Assert.That(harness.SliceCount, Is.GreaterThan(0), "Should have slices");

				var dt = harness.DataTree;
				const int cachedPrefixEnd = 4;
				Assert.That(dt.Slices.Count, Is.GreaterThan(cachedPrefixEnd + 2),
					"Need enough slices to remove one cached-visible slice and then show a later one");

				for (int i = 0; i < dt.Slices.Count; i++)
				{
					((Slice)dt.Slices[i]).Visible = i <= cachedPrefixEnd;
				}

				var highWaterMarkField = typeof(DataTree).GetField("m_lastVisibleHighWaterMark",
					BindingFlags.Instance | BindingFlags.NonPublic);
				Assert.That(highWaterMarkField, Is.Not.Null,
					"Test must be able to seed the private visibility cache deterministically");
				highWaterMarkField.SetValue(dt, cachedPrefixEnd);

				dt.RemoveSliceAt(cachedPrefixEnd);

				var shiftedSlice = (Slice)dt.Slices[cachedPrefixEnd];
				Assert.That(shiftedSlice.Visible, Is.False,
					"Removing the cached frontier should shift an off-screen slice into that slot");

				int targetIndex = cachedPrefixEnd + 1;
				Assert.That(((Slice)dt.Slices[targetIndex]).Visible, Is.False,
					"The later target slice should begin off-screen for this regression test");

				dt.MakeSliceVisible((Slice)dt.Slices[targetIndex], targetIndex);

				Assert.That(((Slice)dt.Slices[cachedPrefixEnd]).Visible, Is.True,
					"Removing a slice must invalidate the high-water mark so MakeSliceVisible repairs the shifted prefix");
				Assert.That(((Slice)dt.Slices[targetIndex]).Visible, Is.True,
					"The requested target slice should still be made visible");
			}
		}

		/// <summary>
		/// Verifies that no DummyObjectSlice remains in the viewport after paint.
		/// The paint path must make all viewport slices real via FieldAt(). A binary
		/// search that miscalculates which slices are in the viewport could leave
		/// DummyObjectSlices un-expanded.
		/// Failure mode: binary search starts too late, leaving slices at the top edge
		/// of the viewport as dummies.
		/// </summary>
		[Test]
		public void DataTreeOpt_NoDummySlicesInViewportAfterPaint()
		{
			CreateExtremeEntry();
			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 800, false);
				Assert.That(harness.SliceCount, Is.GreaterThan(0), "Should have slices");

				// Force full paint cycle
				var bitmap = harness.CaptureCompositeBitmap();
				Assert.That(bitmap, Is.Not.Null);
				bitmap.Dispose();

				var dt = harness.DataTree;
				int viewportHeight = dt.ClientRectangle.Height;
				int dummyCount = 0;

				for (int i = 0; i < dt.Slices.Count; i++)
				{
					var slice = (Slice)dt.Slices[i];
					int sliceTop = slice.Top;
					int sliceBottom = sliceTop + slice.Height;

					// Slice intersects the viewport
					if (sliceBottom > 0 && sliceTop < viewportHeight)
					{
						if (!slice.IsRealSlice)
						{
							dummyCount++;
							Console.WriteLine($"[OPT-TEST] Dummy in viewport at [{i}]: " +
								$"{slice.GetType().Name} Y={sliceTop}-{sliceBottom}");
						}
					}
				}

				Assert.That(dummyCount, Is.EqualTo(0),
					$"Found {dummyCount} DummyObjectSlice(s) in viewport (0-{viewportHeight}). " +
					$"Paint path must make all viewport slices real via FieldAt().");

				Console.WriteLine($"[OPT-TEST] No dummies in viewport 0-{viewportHeight}");
			}
		}

		/// <summary>
		/// Verifies that slice heights are stable after layout convergence.
		/// A binary search for the first visible slice depends on accumulated
		/// heights being deterministic: if heights change between paint calls
		/// (e.g., because DummyObjectSlice→real changes weren't finalized),
		/// the binary search would compute wrong yTop offsets and skip or
		/// double-show slices.
		/// After the initial full-layout pass, heights should never change
		/// on subsequent paint passes (since all viewport slices are already real).
		/// Failure mode: binary search pre-computes yTop from stale heights,
		/// causing slices to render at wrong positions.
		/// </summary>
		[Test]
		public void DataTreeOpt_SliceHeightsStableAfterConvergence()
		{
			CreateExtremeEntry();
			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 800, false);
				Assert.That(harness.SliceCount, Is.GreaterThan(0), "Should have slices");

				// Force full convergence — first paint makes everything real
				var warmup = harness.CaptureCompositeBitmap();
				Assert.That(warmup, Is.Not.Null);
				warmup.Dispose();

				var dt = harness.DataTree;

				// Record converged heights
				var convergedHeights = new int[dt.Slices.Count];
				for (int i = 0; i < dt.Slices.Count; i++)
					convergedHeights[i] = ((Slice)dt.Slices[i]).Height;

				// Force 3 more paint cycles
				for (int pass = 0; pass < 3; pass++)
				{
					dt.Invalidate();
					System.Windows.Forms.Application.DoEvents();
				}

				// Verify heights haven't changed
				int driftCount = 0;
				for (int i = 0; i < dt.Slices.Count; i++)
				{
					var slice = (Slice)dt.Slices[i];
					if (slice.Height != convergedHeights[i])
					{
						driftCount++;
						Console.WriteLine($"[OPT-TEST] Height drift at [{i}] " +
							$"({slice.GetType().Name}): {convergedHeights[i]} → {slice.Height}");
					}
				}

				Assert.That(driftCount, Is.EqualTo(0),
					$"{driftCount} slice(s) changed height after convergence. " +
					$"Binary search requires stable heights to compute accurate yTop offsets.");

				Console.WriteLine($"[OPT-TEST] Height stability: {dt.Slices.Count} slices " +
					$"stable across 3 post-convergence paint passes");
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

		private int RunTimingScenarioAndGetSliceCount(int depth, int breadth, string label)
		{
			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var morphFactory = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>();
			var morph = morphFactory.Create();
			m_entry.LexemeFormOA = morph;
			morph.Form.VernacularDefaultWritingSystem = MakeRenderString(
				$"LexemeForm - timing-{label}", Cache.DefaultVernWs);

			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			CreateNestedSenses(m_entry, senseFactory, depth, breadth, "", 1, $"timing-{label}");
			EnrichEntry(m_entry, $"timing-{label}");

			using (var harness = new DataTreeRenderHarness(Cache, m_entry, "Normal"))
			{
				harness.PopulateSlices(1024, 2400, false);
				Assert.That(harness.SliceCount, Is.GreaterThan(0),
					$"DataTree should create slices for growth scenario {label}");
				Console.WriteLine($"[DATATREE-TIMING] growth check {label}: Slices={harness.SliceCount}");
				return harness.SliceCount;
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

		/// <summary>
		/// Records timing data for a scenario to Output/RenderBenchmarks/datatree-timings.json.
		/// The file accumulates entries keyed by scenario name, updating on each run.
		/// </summary>
		private static void RecordTiming(string scenario, int depth, int breadth,
			DataTreeTimingInfo timing, double density)
		{
			DataTreeTimingBaselineCatalog.AssertMatches(scenario, depth, breadth, timing, density);

			string outputDir = Path.Combine(
				AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Output", "RenderBenchmarks");
			if (!Directory.Exists(outputDir))
				Directory.CreateDirectory(outputDir);

			string filePath = Path.Combine(outputDir, "datatree-timings.json");

			// Load existing data or start fresh
			Dictionary<string, object> allTimings;
			if (File.Exists(filePath))
			{
				string existing = File.ReadAllText(filePath);
				allTimings = JsonConvert.DeserializeObject<Dictionary<string, object>>(existing)
					?? new Dictionary<string, object>();
			}
			else
			{
				allTimings = new Dictionary<string, object>();
			}

			// Update entry for this scenario
			allTimings[scenario] = new
			{
				depth,
				breadth,
				slices = timing.SliceCount,
				initMs = Math.Round(timing.InitializationMs, 1),
				populateMs = Math.Round(timing.PopulateSlicesMs, 1),
				totalMs = Math.Round(timing.TotalMs, 1),
				density = Math.Round(density, 2),
				timestamp = DateTime.UtcNow.ToString("o")
			};

			string json = JsonConvert.SerializeObject(allTimings, Formatting.Indented);
			File.WriteAllText(filePath, json);
			Console.WriteLine($"[DATATREE-TIMING] Wrote timing for '{scenario}' to {filePath}");
		}

		#endregion
	}
}
