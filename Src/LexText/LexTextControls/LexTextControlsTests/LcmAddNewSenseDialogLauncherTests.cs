// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.LexText.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LexTextControlsTests
{
	/// <summary>
	/// The LCModel-aware side of the reusable Avalonia Add New Sense dialog launcher
	/// (<see cref="LcmAddNewSenseDialogLauncher"/>): building the read-only citation form + per-WS gloss field + the
	/// MSA section (POS nodes, slot provider, the initial MsaType the entry's morph type implies) from a real cache,
	/// and the ONE undoable create that turns the dialog payload into a new <c>ILexSense</c> (gloss + find-or-created
	/// MSA). The modal loop itself is desktop-only, so it is exercised by the headless AddNewSenseDialogTests in
	/// FwAvaloniaDialogsTests; here we cover the pure LCModel mapping + create over a real LcmCache (via
	/// InternalsVisibleTo).
	/// </summary>
	[TestFixture]
	public class LcmAddNewSenseDialogLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ILexEntry _casa;
		private IPartOfSpeech _verb;
		private IPartOfSpeech _noun;
		private IMoInflAffixSlot _tenseSlot;
		private IMoInflClass _nounStrong;
		private IMoInflClass _nounStrongIrregular;
		private IMoInflClass _nounWeak;
		private IFsClosedFeature _tenseFeature;
		private IFsSymFeatVal _pastValue;

		protected override void CreateTestData()
		{
			base.CreateTestData();
			_casa = MakeEntry("casa", "house");
			_verb = MakePos("Verb");
			_noun = MakePos("Noun");
			_tenseSlot = Cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>().Create();
			_verb.AffixSlotsOC.Add(_tenseSlot);
			_tenseSlot.Name.set_String(Cache.DefaultAnalWs, "Tense");

			// An inflectable closed feature on the verb (Tense {past, present}) for the §19b inflection-feature tests.
			_tenseFeature = Cache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create();
			Cache.LangProject.MsFeatureSystemOA.FeaturesOC.Add(_tenseFeature);
			_tenseFeature.Name.set_String(Cache.DefaultAnalWs, "Tense");
			_pastValue = Cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>().Create();
			_tenseFeature.ValuesOC.Add(_pastValue);
			_pastValue.Name.set_String(Cache.DefaultAnalWs, "past");
			_verb.InflectableFeatsRC.Add(_tenseFeature);

			// Noun's inflection classes: Strong (with a nested Irregular subclass) + Weak. Verb has none.
			_nounStrong = MakeInflClass(_noun, "Strong");
			_nounStrongIrregular = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			_nounStrong.SubclassesOC.Add(_nounStrongIrregular);
			_nounStrongIrregular.Name.set_String(Cache.DefaultAnalWs, "Irregular");
			_nounWeak = MakeInflClass(_noun, "Weak");
		}

		private IMoInflClass MakeInflClass(IPartOfSpeech pos, string name)
		{
			var cls = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			pos.InflectionClassesOC.Add(cls);
			cls.Name.set_String(Cache.DefaultAnalWs, name);
			return cls;
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

		// ----- BuildInput: citation form + gloss field + MSA section -----

		[Test]
		public void BuildInput_BuildsTheCitationFormGlossFieldAndPosNodes()
		{
			var input = LcmAddNewSenseDialogLauncher.BuildInput(Cache, _casa, tssCitationForm: null);

			Assert.That(input.CitationForm, Is.Not.Null.And.Not.Empty,
				"the citation form falls back to the entry's headword");
			var analTags = Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems
				.Select(ws => ws.Id).ToList();
			Assert.That(input.Gloss.Values.Select(v => v.WsTag), Is.EqualTo(analTags),
				"the gloss field has one row per current analysis writing system");
			Assert.That(input.Gloss.Values.All(v => string.IsNullOrEmpty(v.Value)), Is.True, "rows start empty");
			Assert.That(input.PosNodes.Select(n => n.Name),
				Does.Contain("Verb").And.Contain("Noun"), "the project POS hierarchy is fed to the box");
		}

		[Test]
		public void BuildInput_StemEntry_OpensTheBoxAsStem()
		{
			var input = LcmAddNewSenseDialogLauncher.BuildInput(Cache, _casa, tssCitationForm: null);
			Assert.That(input.InitialMsaType, Is.EqualTo(FwMsaType.Stem),
				"a stem entry opens the MSA box in the stem class (MorphTypePreference parity)");
		}

		[Test]
		public void BuildInput_UsesTheSuppliedCitationFormString()
		{
			var tss = TsStringUtils.MakeString("casita", Cache.DefaultVernWs);
			var input = LcmAddNewSenseDialogLauncher.BuildInput(Cache, _casa, tss);
			Assert.That(input.CitationForm, Is.EqualTo("casita"),
				"the supplied citation form string wins over the headword");
		}

		// ----- CreateSense: ONE undoable step adds the sense with the gloss + MSA -----

		[Test]
		public void CreateSense_AddsASenseWithTheGlossAndMsa()
		{
			var analTag = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id;
			var payload = new AddNewSensePayload(
				new Dictionary<string, string> { [analTag] = "home" },
				new FwSandboxMsa(FwMsaType.Stem, mainPosId: _noun.Guid.ToString()));

			var sensesBefore = _casa.SensesOS.Count;
			// The base opens an undoable UOW in TestSetup, so the create runs directly here (the launcher's Apply
			// opens its OWN single UndoableUnitOfWorkHelper.Do at runtime; here we exercise the same create core).
			var sense = LcmAddNewSenseDialogLauncher.CreateSense(Cache, _casa, payload);

			Assert.That(sense, Is.Not.Null);
			Assert.That(_casa.SensesOS.Count, Is.EqualTo(sensesBefore + 1), "a new sense is added to the entry");
			var analWs = Cache.DefaultAnalWs;
			Assert.That(sense.Gloss.get_String(analWs).Text, Is.EqualTo("home"), "the gloss is set");
			Assert.That(sense.MorphoSyntaxAnalysisRA, Is.Not.Null, "the MSA is find-or-created on the sense");
			var stemMsa = sense.MorphoSyntaxAnalysisRA as IMoStemMsa;
			Assert.That(stemMsa, Is.Not.Null, "a stem MSA was created");
			Assert.That(stemMsa.PartOfSpeechRA, Is.SameAs(_noun), "the chosen main POS is resolved onto the MSA");
		}

		[Test]
		public void CreateSense_InflectionalMsa_ResolvesTheSlot()
		{
			var analTag = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id;
			var payload = new AddNewSensePayload(
				new Dictionary<string, string> { [analTag] = "PAST" },
				new FwSandboxMsa(FwMsaType.Inflectional, mainPosId: _verb.Guid.ToString(),
					slotId: _tenseSlot.Guid.ToString()));

			var sense = LcmAddNewSenseDialogLauncher.CreateSense(Cache, _casa, payload);

			var inflMsa = sense.MorphoSyntaxAnalysisRA as IMoInflAffMsa;
			Assert.That(inflMsa, Is.Not.Null, "an inflectional-affix MSA was created");
			Assert.That(inflMsa.PartOfSpeechRA, Is.SameAs(_verb));
			Assert.That(inflMsa.SlotsRC.Contains(_tenseSlot), Is.True, "the chosen slot is resolved onto the MSA");
		}

		// ----- inflection-class feed + commit (Stage 6) -----

		[Test]
		public void BuildInflectionClasses_ReturnsThePosClassesDepthTagged()
		{
			var classes = LcmInsertEntryDialogLauncher.BuildInflectionClasses(Cache, _noun.Guid.ToString());

			// Strong (depth 0), Irregular (depth 1, nested under Strong), Weak (depth 0) — document order.
			Assert.That(classes.Select(c => c.Name), Is.EqualTo(new[] { "Strong", "Irregular", "Weak" }),
				"the POS's classes are returned in document order, nested subclasses inline");
			Assert.That(classes.Select(c => c.Depth), Is.EqualTo(new[] { 0, 1, 0 }),
				"nested subclasses are depth-tagged");
			Assert.That(classes.Single(c => c.Name == "Strong").Id, Is.EqualTo(_nounStrong.Guid.ToString()),
				"the id round-trips the inflection-class guid");
		}

		[Test]
		public void BuildInflectionClasses_PosWithNoClasses_ReturnsEmpty()
		{
			Assert.That(LcmInsertEntryDialogLauncher.BuildInflectionClasses(Cache, _verb.Guid.ToString()),
				Is.Empty, "a POS with no inflection classes yields none");
		}

		[Test]
		public void BuildInput_FeedsTheInflectionClassProvider()
		{
			var input = LcmAddNewSenseDialogLauncher.BuildInput(Cache, _casa, tssCitationForm: null);
			Assert.That(input.InflectionClassesForPos, Is.Not.Null, "the inflection-class provider is wired");
			var nounClasses = input.InflectionClassesForPos(_noun.Guid.ToString());
			Assert.That(nounClasses.Select(c => c.Name), Is.EqualTo(new[] { "Strong", "Irregular", "Weak" }),
				"the provider returns the selected POS's classes");
		}

		[Test]
		public void CreateSense_StemMsa_SetsTheChosenInflectionClassInOneUow()
		{
			var analTag = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id;
			var payload = new AddNewSensePayload(
				new Dictionary<string, string> { [analTag] = "home" },
				new FwSandboxMsa(FwMsaType.Stem, mainPosId: _noun.Guid.ToString(),
					inflectionClassId: _nounWeak.Guid.ToString()));

			var sense = LcmAddNewSenseDialogLauncher.CreateSense(Cache, _casa, payload);

			var stemMsa = sense.MorphoSyntaxAnalysisRA as IMoStemMsa;
			Assert.That(stemMsa, Is.Not.Null, "a stem MSA was created");
			Assert.That(stemMsa.PartOfSpeechRA, Is.SameAs(_noun));
			Assert.That(stemMsa.InflectionClassRA, Is.SameAs(_nounWeak),
				"the chosen inflection class is set on the find-or-created stem MSA");
		}

		[Test]
		public void CreateSense_StemMsa_NoInflectionClass_LeavesItNull()
		{
			var analTag = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id;
			var payload = new AddNewSensePayload(
				new Dictionary<string, string> { [analTag] = "home" },
				new FwSandboxMsa(FwMsaType.Stem, mainPosId: _noun.Guid.ToString()));

			var sense = LcmAddNewSenseDialogLauncher.CreateSense(Cache, _casa, payload);

			var stemMsa = sense.MorphoSyntaxAnalysisRA as IMoStemMsa;
			Assert.That(stemMsa.InflectionClassRA, Is.Null, "<None> leaves the inflection class unset");
		}

		// ----- inflection features (§19b Stage 2): feed + persist on the sense's infl MSA -----

		[Test]
		public void BuildInput_FeedsTheInflectionFeatureProvider()
		{
			var input = LcmAddNewSenseDialogLauncher.BuildInput(Cache, _casa, tssCitationForm: null);
			Assert.That(input.InflectionFeaturesForPos, Is.Not.Null, "the inflection-feature-system provider is wired");
			Assert.That(input.InflectionFeaturesForPos(_verb.Guid.ToString()).Any(n => n.Id == _tenseFeature.Guid.ToString()),
				Is.True, "the provider returns the selected POS's inflectable features");
		}

		// T2 integration (I4): the sense create path composes gloss + infl MSA POS + INFLECTION FEATURE, and the
		// IFsFeatStruc is persisted on the created MSA.
		[Test]
		public void CreateSense_InflectionalAffix_PersistsInflectionFeatures()
		{
			var analTag = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id;
			var payload = new AddNewSensePayload(
				new Dictionary<string, string> { [analTag] = "PAST" },
				new FwSandboxMsa(FwMsaType.Inflectional, mainPosId: _verb.Guid.ToString(),
					inflectionFeatures: new[]
					{
						new FwFeatureValueAssignment(_tenseFeature.Guid.ToString(), _pastValue.Guid.ToString())
					}));

			var sense = LcmAddNewSenseDialogLauncher.CreateSense(Cache, _casa, payload);

			var inflMsa = sense.MorphoSyntaxAnalysisRA as IMoInflAffMsa;
			Assert.That(inflMsa, Is.Not.Null, "an inflectional-affix MSA was created");
			Assert.That(inflMsa.PartOfSpeechRA, Is.SameAs(_verb), "the POS composes");
			Assert.That(inflMsa.InflFeatsOA, Is.Not.Null, "the inflection FS is persisted on the sense's MSA");
			var readBack = FwFeatureStructureAdapter.ReadAssignments(inflMsa.InflFeatsOA);
			Assert.That(readBack.Single().ValueId, Is.EqualTo(_pastValue.Guid.ToString()),
				"the chosen inflection feature value round-trips");
		}

		[Test]
		public void FirstAllomorphMorphTypeGuid_FallsBackToTheLexemeFormMorphType()
		{
			Assert.That(LcmAddNewSenseDialogLauncher.FirstAllomorphMorphTypeGuid(_casa),
				Is.EqualTo(MoMorphTypeTags.kguidMorphStem.ToString()),
				"a stem entry's morph type resolves to the stem guid");
		}

		// ----- FirstAllomorphMorphTypeGuid: the AlternateFormsOS loop itself (not just the fallback) -----

		[Test]
		public void FirstAllomorphMorphTypeGuid_PrefersTheFirstAllomorphsMorphTypeOverTheLexemeForm()
		{
			// The legacy MorphTypePreference loop walks AlternateFormsOS FIRST and only falls back to the lexeme
			// form when there is no allomorph with a morph type. Only the fallback branch had coverage before this
			// test; if the loop body regressed (e.g. skipped straight to the fallback), a prefix/suffix entry would
			// silently open the Add-Sense MSA box seeded as a stem instead of the correct affix class.
			var prefixType = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
				.GetObject(MoMorphTypeTags.kguidMorphPrefix);
			var allomorph = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			_casa.AlternateFormsOS.Add(allomorph);
			allomorph.MorphTypeRA = prefixType;

			Assert.That(LcmAddNewSenseDialogLauncher.FirstAllomorphMorphTypeGuid(_casa),
				Is.EqualTo(prefixType.Guid.ToString()),
				"the first allomorph's own morph type wins over the entry's lexeme-form (stem) morph type");
		}

		// ----- BuildInput: the slot provider (only ever wired, never invoked, before this test) -----

		[Test]
		public void BuildInput_FeedsTheSlotProvider()
		{
			// InflectionClassesForPos and InflectionFeaturesForPos are both exercised by name elsewhere, but
			// SlotsForPos itself was never actually called — only ever asserted non-null via the MSA-section shape.
			// A regression here (e.g. passing the wrong morph-type guid through the closure) would silently leave
			// the affix "Fills Slot" column empty for a real verb, with no test failing.
			var input = LcmAddNewSenseDialogLauncher.BuildInput(Cache, _casa, tssCitationForm: null);

			var slots = input.SlotsForPos(_verb.Guid.ToString());

			Assert.That(slots.Select(s => s.Id), Does.Contain(_tenseSlot.Guid.ToString()),
				"the slot provider resolves the chosen POS's affix slots (the Insert-Entry BuildSlots delegate)");
		}

		// ----- Show: the null-argument guard clauses (throw before the modal ever runs) -----

		[Test]
		public void Show_NullCache_ThrowsWithoutRunningTheModal()
		{
			// The guard clauses at the top of Show run before the launcher is even constructed, so they are reachable
			// without touching the modal loop. If this regressed (e.g. the null check were removed or reordered
			// after construction), a null cache would NRE deep inside BuildState instead of failing fast at the
			// public entry point.
			Assert.That(() => LcmAddNewSenseDialogLauncher.Show(null, null, null, _casa, null, null),
				Throws.TypeOf<ArgumentNullException>());
		}

		[Test]
		public void Show_NullEntry_ThrowsWithoutRunningTheModal()
		{
			Assert.That(() => LcmAddNewSenseDialogLauncher.Show(Cache, null, null, null, null, null),
				Throws.TypeOf<ArgumentNullException>());
		}

		// ----- CreateSense: the gloss loop's writing-system edge cases -----

		[Test]
		public void CreateSense_SkipsEmptyGlossAlternatives()
		{
			// The lift of AddNewSenseDlg_Closing sets each NON-empty gloss alternative; an empty row must be
			// skipped rather than stomping the sense's gloss for that writing system with an empty string.
			var analTag = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id;
			var payload = new AddNewSensePayload(
				new Dictionary<string, string> { [analTag] = "" },
				new FwSandboxMsa(FwMsaType.Stem, mainPosId: _noun.Guid.ToString()));

			var sense = LcmAddNewSenseDialogLauncher.CreateSense(Cache, _casa, payload);

			Assert.That(sense.Gloss.get_String(Cache.DefaultAnalWs).Text, Is.Null.Or.Empty,
				"an empty gloss alternative is skipped, not written as an empty string");
		}

		[Test]
		public void CreateSense_UnresolvableWritingSystemTag_FallsBackToTheDefaultAnalysisWs()
		{
			// If the payload's ws tag doesn't resolve to a live writing system (ws == 0 — e.g. a stale/garbled tag),
			// the create must still land the gloss somewhere sane (the default analysis WS) rather than silently
			// dropping the user's typed gloss text.
			var payload = new AddNewSensePayload(
				new Dictionary<string, string> { ["zzz-bogus-ws-tag"] = "home" },
				new FwSandboxMsa(FwMsaType.Stem, mainPosId: _noun.Guid.ToString()));

			var sense = LcmAddNewSenseDialogLauncher.CreateSense(Cache, _casa, payload);

			Assert.That(sense.Gloss.get_String(Cache.DefaultAnalWs).Text, Is.EqualTo("home"),
				"an unresolvable ws tag falls back to the default analysis writing system rather than losing the gloss");
		}
	}
}
