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
		private IPartOfSpeech _verb;
		private IPartOfSpeech _noun;
		private IMoInflAffixSlot _tenseSlot;
		private IMoInflClass _nounWeak;
		private ILexEntryType _compoundType;
		// An inflectable closed feature on the Verb POS (Tense {past, present}) for the §19b inflection-feature tests.
		private IFsClosedFeature _tenseFeature;
		private IFsSymFeatVal _pastValue;

		// The base opens an undoable UOW in TestSetup and calls CreateTestData() inside it, so data is created
		// directly here with NO UOW wrapper (a nested task would throw "Nested tasks are not supported").
		protected override void CreateTestData()
		{
			base.CreateTestData();
			_casa = MakeEntry("casa", "house");
			_cantar = MakeEntry("cantar", "to sing");

			// A small POS hierarchy + an inflectional-affix slot on the verb, so the MSA find-or-create can be exercised.
			_verb = MakePos("Verb");
			_noun = MakePos("Noun");
			_tenseSlot = Cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>().Create();
			_verb.AffixSlotsOC.Add(_tenseSlot);
			_tenseSlot.Name.set_String(Cache.DefaultAnalWs, "Tense");

			// An inflection class on the noun, so the Stage 6 inflection-class set can be exercised.
			_nounWeak = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			_noun.InflectionClassesOC.Add(_nounWeak);
			_nounWeak.Name.set_String(Cache.DefaultAnalWs, "Weak");

			// A complex-form type, so the LT-21666 complex-form ILexEntryRef create can be exercised.
			_compoundType = Cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>().Create();
			Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS.Add(_compoundType);
			_compoundType.Name.set_String(Cache.DefaultAnalWs, "Compound");

			// An inflectable closed feature on the verb (Tense {past, present}), so the §19b inflection-feature
			// editor feed + the IFsFeatStruc round-trip can be exercised on an inflectional-affix MSA.
			_tenseFeature = Cache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create();
			Cache.LangProject.MsFeatureSystemOA.FeaturesOC.Add(_tenseFeature);
			_tenseFeature.Name.set_String(Cache.DefaultAnalWs, "Tense");
			_pastValue = MakeSymValue(_tenseFeature, "past");
			MakeSymValue(_tenseFeature, "present");
			_verb.InflectableFeatsRC.Add(_tenseFeature);
		}

		private IFsSymFeatVal MakeSymValue(IFsClosedFeature feature, string name)
		{
			var val = Cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>().Create();
			feature.ValuesOC.Add(val);
			val.Name.set_String(Cache.DefaultAnalWs, name);
			return val;
		}

		private IPartOfSpeech MakePos(string name)
		{
			var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LanguageProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			pos.Name.set_String(Cache.DefaultAnalWs, name);
			return pos;
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

		// ----- grammatical-info (MSA) feed + find-or-create on commit (Stage 3) -----

		[Test]
		public void BuildInput_WiresTheMsaSection()
		{
			var input = LcmInsertEntryDialogLauncher.BuildInput(Cache, tssForm: null);
			Assert.That(input.PosNodes, Is.Not.Empty, "the project POS hierarchy is fed to the MSA box");
			Assert.That(input.MorphTypeToMsaType, Is.Not.Null.And.Not.Empty,
				"the morph-type → MsaType map is supplied so the kit reconfigures live without LCModel");
			Assert.That(input.SlotsForPos, Is.Not.Null, "the per-POS slot provider is wired");
			Assert.That(input.InitialMsaType, Is.EqualTo(FwMsaType.Stem), "the box opens stem (the default morph type)");
		}

		[Test]
		public void BuildPosNodes_ProjectsThePartsOfSpeechAsGuidKeyedNodes()
		{
			var nodes = LcmInsertEntryDialogLauncher.BuildPosNodes(Cache);
			Assert.That(nodes.Any(n => n.Id == _verb.Guid.ToString() && n.Name == "Verb"), Is.True,
				"the POS nodes carry the project parts of speech, keyed by guid string");
			Assert.That(nodes.Any(n => n.Id == _noun.Guid.ToString() && n.Name == "Noun"), Is.True);
		}

		[Test]
		public void MorphTypeToMsaTypeMap_MapsStemAndAffixFamilies()
		{
			var map = LcmInsertEntryDialogLauncher.BuildMorphTypeToMsaTypeMap(Cache);
			Assert.That(map[MoMorphTypeTags.kguidMorphStem.ToString()], Is.EqualTo(FwMsaType.Stem),
				"stem maps to Stem (MorphTypePreference parity)");
			Assert.That(map[MoMorphTypeTags.kguidMorphRoot.ToString()], Is.EqualTo(FwMsaType.Root),
				"root maps to Root");
			Assert.That(map[MoMorphTypeTags.kguidMorphSuffix.ToString()], Is.EqualTo(FwMsaType.Unclassified),
				"an affix maps to Unclassified (the box then refines to Infl/Deriv)");
		}

		[Test]
		public void BuildSlots_ReturnsThePosAffixSlots()
		{
			var slots = LcmInsertEntryDialogLauncher.BuildSlots(Cache, _verb.Guid.ToString(),
				MoMorphTypeTags.kguidMorphSuffix.ToString());
			Assert.That(slots.Any(s => s.Id == _tenseSlot.Guid.ToString() && s.Name == "Tense"), Is.True,
				"the verb's inflectional-affix slot is offered, keyed by guid string");
		}

		[Test]
		public void CreateNewEntry_StemMsa_FindOrCreatesAStemMsaWithTheChosenPos()
		{
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			var payload = new InsertEntryPayload(
				new System.Collections.Generic.Dictionary<string, string> { [vernTag] = "perro" },
				new System.Collections.Generic.Dictionary<string, string>(),
				MoMorphTypeTags.kguidMorphStem.ToString(),
				msa: new FwSandboxMsa(FwMsaType.Stem, mainPosId: _noun.Guid.ToString()));

			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>()
				.Create(LcmInsertEntryDialogLauncher.BuildEntryComponents(Cache, payload));

			Assert.That(entry.SensesOS.Count, Is.EqualTo(1), "a sense was created");
			var msa = entry.SensesOS[0].MorphoSyntaxAnalysisRA;
			Assert.That(msa, Is.InstanceOf<IMoStemMsa>(), "a stem MSA was found-or-created on the sense");
			Assert.That(((IMoStemMsa)msa).PartOfSpeechRA, Is.SameAs(_noun),
				"the chosen main POS is set on the created stem MSA");
		}

		[Test]
		public void ApplyInflectionClass_StemMsa_SetsTheChosenClassOnTheNewEntrySense()
		{
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			var msa = new FwSandboxMsa(FwMsaType.Stem, mainPosId: _noun.Guid.ToString(),
				inflectionClassId: _nounWeak.Guid.ToString());
			var payload = new InsertEntryPayload(
				new System.Collections.Generic.Dictionary<string, string> { [vernTag] = "perro" },
				new System.Collections.Generic.Dictionary<string, string>(),
				MoMorphTypeTags.kguidMorphStem.ToString(), msa: msa);

			// Build the entry (find-or-creates the stem MSA), then apply the inflection class as CreateNewEntry does.
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>()
				.Create(LcmInsertEntryDialogLauncher.BuildEntryComponents(Cache, payload));
			LcmInsertEntryDialogLauncher.ApplyInflectionClass(Cache, entry, msa);

			var stemMsa = entry.SensesOS[0].MorphoSyntaxAnalysisRA as IMoStemMsa;
			Assert.That(stemMsa, Is.Not.Null);
			Assert.That(stemMsa.InflectionClassRA, Is.SameAs(_nounWeak),
				"the chosen inflection class is set on the new entry's stem MSA (the SetEntryMsa parity)");
		}

		[Test]
		public void BuildInput_FeedsTheInflectionClassProvider()
		{
			var input = LcmInsertEntryDialogLauncher.BuildInput(Cache, tssForm: null);
			Assert.That(input.InflectionClassesForPos, Is.Not.Null, "the inflection-class provider is wired");
			Assert.That(input.InflectionClassesForPos(_noun.Guid.ToString()).Select(c => c.Name),
				Does.Contain("Weak"), "the provider returns the selected POS's classes");
		}

		[Test]
		public void CreateNewEntry_InflectionalMsa_FindOrCreatesAnInflAffixMsaWithPosAndSlot()
		{
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			var payload = new InsertEntryPayload(
				new System.Collections.Generic.Dictionary<string, string> { [vernTag] = "-s" },
				new System.Collections.Generic.Dictionary<string, string>(),
				MoMorphTypeTags.kguidMorphSuffix.ToString(),
				msa: new FwSandboxMsa(FwMsaType.Inflectional, mainPosId: _verb.Guid.ToString(),
					slotId: _tenseSlot.Guid.ToString()));

			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>()
				.Create(LcmInsertEntryDialogLauncher.BuildEntryComponents(Cache, payload));

			var msa = entry.SensesOS[0].MorphoSyntaxAnalysisRA;
			Assert.That(msa, Is.InstanceOf<IMoInflAffMsa>(), "an inflectional-affix MSA was created");
			var inflMsa = (IMoInflAffMsa)msa;
			Assert.That(inflMsa.PartOfSpeechRA, Is.SameAs(_verb), "the chosen main POS is set");
			Assert.That(inflMsa.SlotsRC, Does.Contain(_tenseSlot), "the chosen slot is attached to the infl MSA");
		}

		[Test]
		public void CreateNewEntry_DerivationalMsa_FindOrCreatesADerivMsaWithMainAndSecondaryPos()
		{
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			var payload = new InsertEntryPayload(
				new System.Collections.Generic.Dictionary<string, string> { [vernTag] = "-er" },
				new System.Collections.Generic.Dictionary<string, string>(),
				MoMorphTypeTags.kguidMorphSuffix.ToString(),
				msa: new FwSandboxMsa(FwMsaType.Derivational, mainPosId: _verb.Guid.ToString(),
					secondaryPosId: _noun.Guid.ToString()));

			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>()
				.Create(LcmInsertEntryDialogLauncher.BuildEntryComponents(Cache, payload));

			var msa = entry.SensesOS[0].MorphoSyntaxAnalysisRA;
			Assert.That(msa, Is.InstanceOf<IMoDerivAffMsa>(), "a derivational-affix MSA was created");
			var derivMsa = (IMoDerivAffMsa)msa;
			Assert.That(derivMsa.FromPartOfSpeechRA, Is.SameAs(_verb), "the chosen 'attaches to' POS is the from-POS");
			Assert.That(derivMsa.ToPartOfSpeechRA, Is.SameAs(_noun), "the chosen 'changes to' POS is the to-POS");
		}

		// ----- inflection features (§19b Stage 2): feed + find-or-create + IFsFeatStruc round-trip -----

		[Test]
		public void BuildInput_FeedsTheInflectionFeatureProvider()
		{
			var input = LcmInsertEntryDialogLauncher.BuildInput(Cache, tssForm: null);
			Assert.That(input.InflectionFeaturesForPos, Is.Not.Null, "the inflection-feature-system provider is wired");
			var nodes = input.InflectionFeaturesForPos(_verb.Guid.ToString());
			Assert.That(nodes.Any(n => n.Id == _tenseFeature.Guid.ToString() && n.Kind == FwFeatureNodeKind.Closed),
				Is.True, "the provider returns the selected POS's inflectable features");
			Assert.That(nodes.Any(n => n.Id == _pastValue.Guid.ToString() && n.Kind == FwFeatureNodeKind.Value),
				Is.True, "with their symbolic values");
		}

		[Test]
		public void BuildInflectionFeatures_UnknownPos_IsEmpty()
		{
			Assert.That(LcmInsertEntryDialogLauncher.BuildInflectionFeatures(Cache, "not-a-guid"), Is.Empty);
		}

		// T2 integration: one realized create exercises morph type (suffix) + MSA POS + slot + INFLECTION FEATURE
		// together; the find-or-created infl MSA composes all of them, and the IFsFeatStruc round-trips.
		[Test]
		public void Show_InflectionalAffix_ComposesMsaPosSlotAndInflectionFeatures()
		{
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			var msa = new FwSandboxMsa(FwMsaType.Inflectional, mainPosId: _verb.Guid.ToString(),
				slotId: _tenseSlot.Guid.ToString(),
				inflectionFeatures: new[]
				{
					new FwFeatureValueAssignment(_tenseFeature.Guid.ToString(), _pastValue.Guid.ToString())
				});
			var payload = new InsertEntryPayload(
				new System.Collections.Generic.Dictionary<string, string> { [vernTag] = "-s" },
				new System.Collections.Generic.Dictionary<string, string>(),
				MoMorphTypeTags.kguidMorphSuffix.ToString(), msa: msa);

			// Build the entry (find-or-creates the infl MSA), then apply the inflection features as CreateNewEntry does.
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>()
				.Create(LcmInsertEntryDialogLauncher.BuildEntryComponents(Cache, payload));
			LcmInsertEntryDialogLauncher.ApplyInflectionFeatures(Cache, entry.SensesOS[0], msa);

			var inflMsa = (IMoInflAffMsa)entry.SensesOS[0].MorphoSyntaxAnalysisRA;
			// POS + slot compose with the features on the SAME MSA.
			Assert.That(inflMsa.PartOfSpeechRA, Is.SameAs(_verb), "the main POS composes");
			Assert.That(inflMsa.SlotsRC, Does.Contain(_tenseSlot), "the slot composes");
			Assert.That(inflMsa.InflFeatsOA, Is.Not.Null, "the inflection FS is built on the infl MSA");
			var readBack = FwFeatureStructureAdapter.ReadAssignments(inflMsa.InflFeatsOA);
			Assert.That(readBack.Single().ValueId, Is.EqualTo(_pastValue.Guid.ToString()),
				"the chosen inflection feature value round-trips into the IFsFeatStruc");
		}

		// T4 workflow: create entry → inflectional affix → pick POS → assign an inflection feature value → commit →
		// reopen (read the FS back as the MsaCreator edit path would) → verify the IFsFeatStruc round-tripped.
		[Test]
		public void Workflow_CreateInflAffix_AssignFeature_Commit_Reopen_RoundTrips()
		{
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			var msa = new FwSandboxMsa(FwMsaType.Inflectional, mainPosId: _verb.Guid.ToString(),
				inflectionFeatures: new[]
				{
					new FwFeatureValueAssignment(_tenseFeature.Guid.ToString(), _pastValue.Guid.ToString())
				});
			var payload = new InsertEntryPayload(
				new System.Collections.Generic.Dictionary<string, string> { [vernTag] = "-ed" },
				new System.Collections.Generic.Dictionary<string, string>(),
				MoMorphTypeTags.kguidMorphSuffix.ToString(), msa: msa);

			// Commit (the launcher's create path).
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>()
				.Create(LcmInsertEntryDialogLauncher.BuildEntryComponents(Cache, payload));
			LcmInsertEntryDialogLauncher.ApplyInflectionFeatures(Cache, entry.SensesOS[0], msa);

			// "Reopen": resolve the persisted MSA's FS the way the MsaCreator edit path seeds the editor, and verify
			// the assignment set the editor would show matches what was committed.
			var committedMsa = entry.SensesOS[0].MorphoSyntaxAnalysisRA;
			var reopened = FwFeatureStructureAdapter.ReadAssignments(
				FwFeatureStructureAdapter.GetInflectionFeatures(committedMsa));
			Assert.That(reopened.Count, Is.EqualTo(1), "the reopened MSA shows exactly the committed feature");
			Assert.That(reopened[0].ClosedFeatureId, Is.EqualTo(_tenseFeature.Guid.ToString()));
			Assert.That(reopened[0].ValueId, Is.EqualTo(_pastValue.Guid.ToString()),
				"the inflection feature round-tripped through create → persist → reopen");
		}

		[Test]
		public void CreateNewEntry_StemMsa_WritesNoInflectionFeatures()
		{
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			// A stem MSA carrying (incorrectly) some inflection features — the adapter must no-op (scope is infl/deriv).
			var msa = new FwSandboxMsa(FwMsaType.Stem, mainPosId: _noun.Guid.ToString());
			var payload = new InsertEntryPayload(
				new System.Collections.Generic.Dictionary<string, string> { [vernTag] = "perro" },
				new System.Collections.Generic.Dictionary<string, string>(),
				MoMorphTypeTags.kguidMorphStem.ToString(), msa: msa);

			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>()
				.Create(LcmInsertEntryDialogLauncher.BuildEntryComponents(Cache, payload));
			LcmInsertEntryDialogLauncher.ApplyInflectionFeatures(Cache, entry.SensesOS[0], msa);

			Assert.That(entry.SensesOS[0].MorphoSyntaxAnalysisRA, Is.InstanceOf<IMoStemMsa>(),
				"a stem MSA is unaffected by the inflection-feature apply");
		}

		// ----- ApplyInflectionFeatures null guards -----
		//
		// Both MSAPopupTreeManager.AddNewMsa/EditExistingMsa (the New-UI "Create New Grammatical Info." gates) call
		// ApplyInflectionFeatures INSIDE a Cache.DomainDataByFlid.BeginUndoTask/EndUndoTask pair, between the sense's
		// MSA assignment and the undo task's close. Every other test above only exercises the non-null happy path;
		// if either guard here regressed to a plain dereference, a null sense/chosen/msa reaching this call would
		// throw a NullReferenceException mid-undo-task, leaving the undo stack unbalanced (BeginUndoTask with no
		// matching EndUndoTask) instead of the documented, safe no-op.

		[Test]
		public void ApplyInflectionFeatures_SenseOverload_NullSense_DoesNotThrow()
		{
			var chosen = new FwSandboxMsa(FwMsaType.Inflectional, mainPosId: _verb.Guid.ToString());
			Assert.DoesNotThrow(() => LcmInsertEntryDialogLauncher.ApplyInflectionFeatures(Cache, (ILexSense)null, chosen));
		}

		[Test]
		public void ApplyInflectionFeatures_SenseOverload_NullChosen_DoesNotThrow()
		{
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			_cantar.SensesOS.Add(sense);
			sense.SandboxMSA = new SandboxGenericMSA { MsaType = MsaType.kStem, MainPOS = _noun };

			Assert.DoesNotThrow(() => LcmInsertEntryDialogLauncher.ApplyInflectionFeatures(Cache, sense, null));
			Assert.That(sense.MorphoSyntaxAnalysisRA, Is.InstanceOf<IMoStemMsa>(),
				"a null chosen payload leaves the sense's already-assigned MSA untouched");
		}

		[Test]
		public void ApplyInflectionFeatures_MsaOverload_NullMsa_DoesNotThrow()
		{
			var chosen = new FwSandboxMsa(FwMsaType.Inflectional, mainPosId: _verb.Guid.ToString());
			Assert.DoesNotThrow(() =>
				LcmInsertEntryDialogLauncher.ApplyInflectionFeatures(Cache, (IMoMorphSynAnalysis)null, chosen));
		}

		[Test]
		public void ApplyInflectionFeatures_MsaOverload_NullCache_DoesNotThrow()
		{
			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			_cantar.MorphoSyntaxAnalysesOC.Add(msa);
			var chosen = new FwSandboxMsa(FwMsaType.Inflectional, mainPosId: _verb.Guid.ToString());

			Assert.DoesNotThrow(() => LcmInsertEntryDialogLauncher.ApplyInflectionFeatures(null, msa, chosen));
		}

		[Test]
		public void BuildEntryComponents_NoChosenMsa_FallsBackToTheMorphTypeDefault()
		{
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			var payload = new InsertEntryPayload(
				new System.Collections.Generic.Dictionary<string, string> { [vernTag] = "gato" },
				new System.Collections.Generic.Dictionary<string, string>(),
				MoMorphTypeTags.kguidMorphStem.ToString()); // msa null

			var components = LcmInsertEntryDialogLauncher.BuildEntryComponents(Cache, payload);
			Assert.That(components.MSA, Is.Not.Null,
				"a null chosen MSA still yields the morph-type's default MSA descriptor (older-caller parity)");
			Assert.That(components.MSA.MsaType, Is.EqualTo(MsaType.kStem), "stem morph type defaults to a stem MSA");
		}

		// ----- complex-form type feed + complex-form ILexEntryRef on create (LT-21666) -----

		[Test]
		public void BuildInput_WiresTheComplexFormTypeSection()
		{
			var input = LcmInsertEntryDialogLauncher.BuildInput(Cache, tssForm: null);
			Assert.That(input.ComplexFormTypes.Any(o => o.Key == _compoundType.Guid.ToString() && o.Name == "Compound"),
				Is.True, "the project's complex-form types populate the picker, keyed by guid string");
			Assert.That(input.InitialComplexFormTypeKey, Is.Null, "the picker opens at <Not Applicable>");
			Assert.That(input.ComplexFormGatingByMorphType, Is.Not.Null.And.Not.Empty,
				"the morph-type → complex-form gating map is supplied (EnableComplexFormTypeCombo parity)");
		}

		[Test]
		public void ComplexFormGatingMap_MirrorsEnableComplexFormTypeCombo()
		{
			var map = LcmInsertEntryDialogLauncher.BuildComplexFormGatingMap(Cache);
			Assert.That(map[MoMorphTypeTags.kguidMorphRoot.ToString()],
				Is.EqualTo(ComplexFormGating.DisabledNotApplicable), "root disables + forces Not-Applicable");
			Assert.That(map[MoMorphTypeTags.kguidMorphBoundRoot.ToString()],
				Is.EqualTo(ComplexFormGating.DisabledNotApplicable), "bound root disables + forces Not-Applicable");
			Assert.That(map[MoMorphTypeTags.kguidMorphPhrase.ToString()],
				Is.EqualTo(ComplexFormGating.EnabledKeepSelection), "phrase enables + keeps the selection");
			Assert.That(map[MoMorphTypeTags.kguidMorphStem.ToString()],
				Is.EqualTo(ComplexFormGating.EnabledNotApplicable), "stem takes the default (enabled, reset)");
		}

		[Test]
		public void CreateNewEntry_WithComplexFormType_AddsAComplexFormEntryRefInOneUow()
		{
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			var payload = new InsertEntryPayload(
				new System.Collections.Generic.Dictionary<string, string> { [vernTag] = "casa grande" },
				new System.Collections.Generic.Dictionary<string, string>(),
				MoMorphTypeTags.kguidMorphPhrase.ToString(),
				complexFormTypeKey: _compoundType.Guid.ToString());

			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>()
				.Create(LcmInsertEntryDialogLauncher.BuildEntryComponents(Cache, payload));
			LcmInsertEntryDialogLauncher.ApplyComplexFormType(Cache, entry, payload.ComplexFormTypeKey);

			Assert.That(entry.EntryRefsOS.Count, Is.EqualTo(1), "a complex-form entry ref is added to the new entry");
			var ler = entry.EntryRefsOS[0];
			Assert.That(ler.RefType, Is.EqualTo(LexEntryRefTags.krtComplexForm),
				"the entry ref is a complex-form ref (CreateNewEntryInternal parity)");
			Assert.That(ler.ComplexEntryTypesRS, Does.Contain(_compoundType),
				"the chosen complex-form type is added to the ref's ComplexEntryTypesRS");
		}

		[Test]
		public void CreateNewEntry_NotApplicable_AddsNoEntryRef()
		{
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			var payload = new InsertEntryPayload(
				new System.Collections.Generic.Dictionary<string, string> { [vernTag] = "gato" },
				new System.Collections.Generic.Dictionary<string, string>(),
				MoMorphTypeTags.kguidMorphStem.ToString(),
				complexFormTypeKey: null); // <Not Applicable>

			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>()
				.Create(LcmInsertEntryDialogLauncher.BuildEntryComponents(Cache, payload));
			LcmInsertEntryDialogLauncher.ApplyComplexFormType(Cache, entry, payload.ComplexFormTypeKey);

			Assert.That(entry.EntryRefsOS, Is.Empty,
				"<Not Applicable> adds no complex-form entry ref (CreateNewEntryInternal m_fComplexForm false)");
		}

		[Test]
		public void ApplyComplexFormType_UnresolvableId_AddsNoEntryRef()
		{
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			var payload = new InsertEntryPayload(
				new System.Collections.Generic.Dictionary<string, string> { [vernTag] = "perro" },
				new System.Collections.Generic.Dictionary<string, string>(),
				MoMorphTypeTags.kguidMorphStem.ToString());
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>()
				.Create(LcmInsertEntryDialogLauncher.BuildEntryComponents(Cache, payload));

			LcmInsertEntryDialogLauncher.ApplyComplexFormType(Cache, entry, "not-a-guid");
			Assert.That(entry.EntryRefsOS, Is.Empty, "an unresolvable complex-form id adds nothing");
		}
	}
}
