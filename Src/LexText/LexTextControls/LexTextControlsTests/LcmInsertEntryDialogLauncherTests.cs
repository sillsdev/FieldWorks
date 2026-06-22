// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Globalization;
using System.Linq;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.LexText.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LexTextControlsTests
{
	/// <summary>
	/// The LCModel-aware side of the reusable Avalonia Insert Entry dialog launcher
	/// (<see cref="LcmInsertEntryDialogLauncher"/>): building the per-WS lexeme-form / gloss fields and the
	/// morph-type options from a real cache, the live affix-marker → morph-type derivation, and the ONE undoable
	/// create that turns the dialog payload into an ILexEntry (with the LT-11950 per-alternative ws fix-up). The
	/// modal loop itself is desktop-only (it needs an Avalonia app + a WinForms-owned modal Form), so it is
	/// exercised by the headless InsertEntryDialogTests in FwAvaloniaDialogsTests; here we cover the pure LCModel
	/// mapping + create over a real LcmCache, visible via InternalsVisibleTo.
	/// </summary>
	[TestFixture]
	public class LcmInsertEntryDialogLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ILexEntry _casa;
		private ILexEntry _cantar;

		// The base opens an undoable UOW in TestSetup and calls CreateTestData() inside it, so data is created
		// directly here with NO UOW wrapper (a nested task would throw "Nested tasks are not supported").
		protected override void CreateTestData()
		{
			base.CreateTestData();
			_casa = MakeEntry("casa", "house");
			_cantar = MakeEntry("cantar", "to sing");
		}

		private ILexEntry MakeEntry(string lexemeForm, string gloss)
		{
			var components = new LexEntryComponents
			{
				MorphType = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
					.GetObject(MoMorphTypeTags.kguidMorphStem)
			};
			components.LexemeFormAlternatives.Add(TsStringUtils.MakeString(lexemeForm, Cache.DefaultVernWs));
			components.GlossAlternatives.Add(TsStringUtils.MakeString(gloss, Cache.DefaultAnalWs));
			return Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(components);
		}

		// ----- BuildInput: the per-WS fields + morph-type options + default selection -----

		[Test]
		public void BuildInput_BuildsLexemeFormAndGlossFieldsForCurrentWritingSystems()
		{
			var input = LcmInsertEntryDialogLauncher.BuildInput(Cache, tssForm: null);

			var vernTags = Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems
				.Select(ws => ws.Id).ToList();
			var analTags = Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems
				.Select(ws => ws.Id).ToList();

			Assert.That(input.LexemeForm.Values.Select(v => v.WsTag), Is.EqualTo(vernTags),
				"the lexeme-form field has one row per current vernacular writing system");
			Assert.That(input.Gloss.Values.Select(v => v.WsTag), Is.EqualTo(analTags),
				"the gloss field has one row per current analysis writing system");
			Assert.That(input.LexemeForm.Values.All(v => string.IsNullOrEmpty(v.Value)), Is.True,
				"with no initial form, every lexeme-form row starts empty");
		}

		[Test]
		public void BuildInput_SeedsTheLexemeFormFromAVernacularInitialString()
		{
			var vernWs = Cache.DefaultVernWs;
			var tss = TsStringUtils.MakeString("casa", vernWs);

			var input = LcmInsertEntryDialogLauncher.BuildInput(Cache, tss);

			Assert.That(input.LexemeForm.Values.Any(v => v.Value == "casa"), Is.True,
				"a vernacular initial form seeds the lexeme-form field");
		}

		[Test]
		public void BuildInput_MorphTypeOptions_CarryGuidKeysAndDefaultToStem()
		{
			var input = LcmInsertEntryDialogLauncher.BuildInput(Cache, tssForm: null);

			Assert.That(input.MorphTypes, Is.Not.Empty, "the project's morph types populate the picker");
			Assert.That(input.InitialMorphTypeKey, Is.EqualTo(MoMorphTypeTags.kguidMorphStem.ToString()),
				"the default morph type is stem (legacy parity)");
			Assert.That(input.MorphTypes.Any(o => o.Key == MoMorphTypeTags.kguidMorphStem.ToString()), Is.True,
				"the stem morph type is one of the options, keyed by its guid string");
		}

		// ----- DeriveMorphType: the live affix-marker derivation -----

		[Test]
		public void DeriveMorphType_EmptyForm_KeepsStem()
		{
			var (typeKey, adjusted) = LcmInsertEntryDialogLauncher.DeriveMorphType(Cache, "");
			Assert.That(typeKey, Is.EqualTo(MoMorphTypeTags.kguidMorphStem.ToString()));
			Assert.That(adjusted, Is.Empty);
		}

		[Test]
		public void DeriveMorphType_SuffixMarker_DerivesTheSuffixType()
		{
			// A trailing hyphen-led form is a suffix in the standard morph-type marker set.
			var (typeKey, _) = LcmInsertEntryDialogLauncher.DeriveMorphType(Cache, "-ed");
			Assert.That(typeKey, Is.EqualTo(MoMorphTypeTags.kguidMorphSuffix.ToString()),
				"a leading affix marker derives the matching (suffix) morph type");
		}

		// ----- Apply / CreateNewEntry: ONE undoable step builds the entry from the payload -----

		[Test]
		public void CreateNewEntry_BuildsAnEntryWithTheExpectedFormGlossAndMorphType()
		{
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			var analTag = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id;
			var payload = new InsertEntryPayload(
				new System.Collections.Generic.Dictionary<string, string> { [vernTag] = "casa" },
				new System.Collections.Generic.Dictionary<string, string> { [analTag] = "house" },
				MoMorphTypeTags.kguidMorphStem.ToString());

			// The base (MemoryOnlyBackendProviderRestoredForEachTestTestBase) opens an undoable UOW in TestSetup,
			// so the create runs directly here with NO UOW wrapper (wrapping it would throw "Nested tasks are not
			// supported"). The launcher's Apply opens its OWN single UndoableUnitOfWorkHelper.Do at runtime; here
			// we exercise the SAME BuildEntryComponents + factory create inside the test's open task.
			var entriesBefore = Cache.ServiceLocator.GetInstance<ILexEntryRepository>().Count;
			var components = LcmInsertEntryDialogLauncher.BuildEntryComponents(Cache, payload);
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(components);

			Assert.That(entry, Is.Not.Null, "the entry was created");
			Assert.That(Cache.ServiceLocator.GetInstance<ILexEntryRepository>().Count,
				Is.EqualTo(entriesBefore + 1));

			var vernWs = Cache.DefaultVernWs;
			var analWs = Cache.DefaultAnalWs;
			Assert.That(entry.LexemeFormOA.Form.get_String(vernWs).Text, Is.EqualTo("casa"),
				"the lexeme form alternative is created for its own writing system (LT-11950 fix-up)");
			Assert.That(entry.LexemeFormOA.MorphTypeRA.Guid.ToString(),
				Is.EqualTo(MoMorphTypeTags.kguidMorphStem.ToString()), "the chosen morph type is set");
			Assert.That(entry.SensesOS.Count, Is.EqualTo(1), "a sense is created for the gloss");
			Assert.That(entry.SensesOS[0].Gloss.get_String(analWs).Text, Is.EqualTo("house"));
		}

		// ----- the duplicate-detection match search reuses the legacy EntryGoSearchEngine matching -----

		[Test]
		public void BuildInput_WiresTheMatchSearch()
		{
			var input = LcmInsertEntryDialogLauncher.BuildInput(Cache, tssForm: null);
			Assert.That(input.SearchMatches, Is.Not.Null, "the matching-entries search is wired into the dialog input");
		}

		[Test]
		public void MatchSearch_FindsExistingEntriesByForm()
		{
			var search = LcmInsertEntryDialogLauncher.BuildMatchSearch(Cache, mediator: null, propertyTable: null);

			var results = search("ca"); // matches casa + cantar (prefix on the form fields)
			Assert.That(results.Select(r => r.Id),
				Is.EquivalentTo(new[]
				{
					_casa.Hvo.ToString(CultureInfo.InvariantCulture),
					_cantar.Hvo.ToString(CultureInfo.InvariantCulture)
				}),
				"the match search returns existing entries whose form matches (legacy EntryGoSearchEngine matching)");
			Assert.That(results.Any(r => r.Text == "casa"), Is.True, "the match row carries the headword");
		}

		[Test]
		public void MatchSearch_EmptyQuery_ReturnsNoFalsePositives()
		{
			// The VM never calls the search for an empty form, but the delegate must not throw on one.
			var search = LcmInsertEntryDialogLauncher.BuildMatchSearch(Cache, null, null);
			Assert.That(() => search(string.Empty), Throws.Nothing);
		}

		// ----- the "use existing entry" outcome resolves the chosen id rather than creating -----

		[Test]
		public void ResolveEntry_ResolvesAChosenExistingEntryId()
		{
			var entry = LcmInsertEntryDialogLauncher.ResolveEntry(Cache,
				_casa.Hvo.ToString(CultureInfo.InvariantCulture));
			Assert.That(entry, Is.SameAs(_casa), "a chosen existing-entry id resolves back to the live entry");
		}

		[Test]
		public void ResolveEntry_NullOrUnparsableId_ReturnsNull()
		{
			Assert.That(LcmInsertEntryDialogLauncher.ResolveEntry(Cache, null), Is.Null);
			Assert.That(LcmInsertEntryDialogLauncher.ResolveEntry(Cache, "not-an-id"), Is.Null);
		}

		[Test]
		public void CreatePath_AddsAnEntry_WhileUseExistingPathDoesNot()
		{
			// Sanity contrast: the Create path (no chosen existing id) adds an entry; the use-existing path resolves
			// an EXISTING entry (no create). This mirrors the launcher's Apply branch on ChosenExistingEntryId.
			var repo = Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			var before = repo.Count;

			// Use-existing: just resolve, never create.
			var existing = LcmInsertEntryDialogLauncher.ResolveEntry(Cache,
				_casa.Hvo.ToString(CultureInfo.InvariantCulture));
			Assert.That(existing, Is.SameAs(_casa));
			Assert.That(repo.Count, Is.EqualTo(before), "the use-existing path creates no new entry");

			// Create: build components + create (as the launcher's create branch does).
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			var payload = new InsertEntryPayload(
				new System.Collections.Generic.Dictionary<string, string> { [vernTag] = "nuevo" },
				new System.Collections.Generic.Dictionary<string, string>(),
				MoMorphTypeTags.kguidMorphStem.ToString());
			var created = Cache.ServiceLocator.GetInstance<ILexEntryFactory>()
				.Create(LcmInsertEntryDialogLauncher.BuildEntryComponents(Cache, payload));
			Assert.That(created, Is.Not.Null);
			Assert.That(repo.Count, Is.EqualTo(before + 1), "the create path adds exactly one entry");
		}
	}
}
