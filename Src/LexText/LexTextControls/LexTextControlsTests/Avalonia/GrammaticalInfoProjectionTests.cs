// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
	/// The cache-bound half of a grammatical-info section (<see
	/// cref="GrammaticalInfoProjection"/>) over a real <c>LcmCache</c>: projecting the parts-of-
	/// speech hierarchy, the inflectional-affix slots, the inflection classes and the
	/// inflectable-feature system into the LCModel-free shapes the shared Avalonia MSA section
	/// consumes, resolving a chosen <see cref="FwSandboxMsa"/> back into a
	/// <c>SandboxGenericMSA</c>, and applying an inflection class or feature structure inside a
	/// unit of work.
	///
	/// These cover the projection on its own, with no dialog and no modal. What a particular
	/// dialog does with the projection stays with that dialog's own fixture.
	/// </summary>
	[TestFixture]
	public class GrammaticalInfoProjectionTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ILexEntry _cantar;
		private IPartOfSpeech _verb;
		private IPartOfSpeech _noun;
		private IMoInflAffixSlot _tenseSlot;
		private IMoInflClass _nounStrong;
		private IMoInflClass _nounIrregular;
		private IMoInflClass _nounWeak;
		private IFsClosedFeature _tenseFeature;

		// The base opens an undoable UOW in TestSetup and calls CreateTestData() inside it, so
		// data is created directly here with NO UOW wrapper (a nested task would throw).
		protected override void CreateTestData()
		{
			base.CreateTestData();
			_cantar = MakeEntry("cantar", "to sing");

			_verb = MakePos("Verb");
			_noun = MakePos("Noun");

			// An inflectional-affix slot on the verb, so the slot projection has something to
			// offer.
			_tenseSlot = Cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>().Create();
			_verb.AffixSlotsOC.Add(_tenseSlot);
			_tenseSlot.Name.set_String(Cache.DefaultAnalWs, "Tense");

			// Inflection classes on the noun, one of them nested, so document order and depth
			// tagging are both observable: Strong (0), Irregular (1, under Strong), Weak (0).
			_nounStrong = MakeInflClass(_noun.InflectionClassesOC, "Strong");
			_nounIrregular = MakeInflClass(_nounStrong.SubclassesOC, "Irregular");
			_nounWeak = MakeInflClass(_noun.InflectionClassesOC, "Weak");

			// An inflectable closed feature on the verb (Tense {past, present}).
			_tenseFeature = Cache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create();
			Cache.LangProject.MsFeatureSystemOA.FeaturesOC.Add(_tenseFeature);
			_tenseFeature.Name.set_String(Cache.DefaultAnalWs, "Tense");
			MakeSymValue(_tenseFeature, "past");
			MakeSymValue(_tenseFeature, "present");
			_verb.InflectableFeatsRC.Add(_tenseFeature);
		}

		// The object must belong to its owner before a multi-string accessor will take a value.
		private IMoInflClass MakeInflClass(ILcmOwningCollection<IMoInflClass> owner, string name)
		{
			var cls = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			owner.Add(cls);
			cls.Name.set_String(Cache.DefaultAnalWs, name);
			return cls;
		}

		private void MakeSymValue(IFsClosedFeature feature, string name)
		{
			var val = Cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>().Create();
			feature.ValuesOC.Add(val);
			val.Name.set_String(Cache.DefaultAnalWs, name);
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

		// ----- parts of speech, morph-type mapping, slots -----

		[Test]
		public void BuildPosNodes_ProjectsThePartsOfSpeechAsGuidKeyedNodes()
		{
			var nodes = GrammaticalInfoProjection.BuildPosNodes(Cache);
			Assert.That(nodes.Any(n => n.Id == _verb.Guid.ToString() && n.Name == "Verb"), Is.True,
				"the POS nodes carry the project parts of speech, keyed by guid string");
			Assert.That(nodes.Any(n => n.Id == _noun.Guid.ToString() && n.Name == "Noun"), Is.True);
		}

		[Test]
		public void BuildMorphTypeToMsaTypeMap_MapsStemAndAffixFamilies()
		{
			var map = GrammaticalInfoProjection.BuildMorphTypeToMsaTypeMap(Cache);
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
			var slots = GrammaticalInfoProjection.BuildSlots(Cache, _verb.Guid.ToString(),
				MoMorphTypeTags.kguidMorphSuffix.ToString());
			Assert.That(slots.Any(s => s.Id == _tenseSlot.Guid.ToString() && s.Name == "Tense"), Is.True,
				"the verb's inflectional-affix slot is offered, keyed by guid string");
		}

		// ----- inflection classes -----

		[Test]
		public void BuildInflectionClasses_ReturnsThePosClassesDepthTagged()
		{
			var classes = GrammaticalInfoProjection.BuildInflectionClasses(Cache, _noun.Guid.ToString());

			Assert.That(classes.Select(c => c.Name), Is.EqualTo(new[] { "Strong", "Irregular", "Weak" }),
				"the POS's classes are returned in document order, nested subclasses inline");
			Assert.That(classes.Select(c => c.Depth), Is.EqualTo(new[] { 0, 1, 0 }),
				"nested subclasses are depth-tagged");
			Assert.That(classes.Single(c => c.Name == "Strong").Id, Is.EqualTo(_nounStrong.Guid.ToString()),
				"the id round-trips the inflection-class guid");
		}

		[Test]
		public void BuildInflectionClasses_ReturnsEmpty_WhenPosHasNoClasses()
		{
			Assert.That(GrammaticalInfoProjection.BuildInflectionClasses(Cache, _verb.Guid.ToString()),
				Is.Empty, "a POS with no inflection classes yields none");
		}

		[Test]
		public void ApplyInflectionClass_SetsTheChosenClass_OnAStemMsa()
		{
			var vernTag = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Id;
			var msa = new FwSandboxMsa(FwMsaType.Stem, mainPosId: _noun.Guid.ToString(),
				inflectionClassId: _nounWeak.Guid.ToString());
			var payload = new InsertEntryDlgPayload(
				new System.Collections.Generic.Dictionary<string, string> { [vernTag] = "perro" },
				new System.Collections.Generic.Dictionary<string, string>(),
				MoMorphTypeTags.kguidMorphStem.ToString(), msa: msa);

			// Build the entry, which find-or-creates the stem MSA, then apply the class over it.
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>()
				.Create(LcmInsertEntryDialogLauncher.BuildEntryComponents(Cache, payload));
			GrammaticalInfoProjection.ApplyInflectionClass(Cache, entry, msa);

			var stemMsa = entry.SensesOS[0].MorphoSyntaxAnalysisRA as IMoStemMsa;
			Assert.That(stemMsa, Is.Not.Null);
			Assert.That(stemMsa.InflectionClassRA, Is.SameAs(_nounWeak),
				"the chosen inflection class is set on the stem MSA (the SetEntryMsa parity)");
		}

		// ----- inflection features -----

		[Test]
		public void BuildInflectionFeatures_ReturnsEmpty_WhenPosIsUnknown()
		{
			Assert.That(GrammaticalInfoProjection.BuildInflectionFeatures(Cache, "not-a-guid"), Is.Empty);
		}

		[Test]
		public void BuildInflectionFeatures_ProjectsTheClosedFeatureAndItsValues()
		{
			var nodes = GrammaticalInfoProjection.BuildInflectionFeatures(Cache, _verb.Guid.ToString());

			Assert.That(nodes.Any(n => n.Name == "Tense"), Is.True,
				"the POS's inflectable closed feature is projected");
			Assert.That(nodes.Any(n => n.Name == "past") && nodes.Any(n => n.Name == "present"), Is.True,
				"a closed feature is expanded to its values");
		}

		// MSAPopupTreeManager calls ApplyInflectionFeatures inside a BeginUndoTask/EndUndoTask
		// pair. A guard regressing to a plain dereference would throw mid-task and unbalance the
		// undo stack.

		[Test]
		public void ApplyInflectionFeatures_DoesNotThrow_WhenSenseIsNull()
		{
			var chosen = new FwSandboxMsa(FwMsaType.Inflectional, mainPosId: _verb.Guid.ToString());
			Assert.DoesNotThrow(
				() => GrammaticalInfoProjection.ApplyInflectionFeatures(Cache, (ILexSense)null, chosen));
		}

		[Test]
		public void ApplyInflectionFeatures_LeavesTheAssignedMsa_WhenChosenIsNull()
		{
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			_cantar.SensesOS.Add(sense);
			sense.SandboxMSA = new SandboxGenericMSA { MsaType = MsaType.kStem, MainPOS = _noun };

			Assert.DoesNotThrow(() => GrammaticalInfoProjection.ApplyInflectionFeatures(Cache, sense, null));
			Assert.That(sense.MorphoSyntaxAnalysisRA, Is.InstanceOf<IMoStemMsa>(),
				"a null chosen payload leaves the sense's already-assigned MSA untouched");
		}

		[Test]
		public void ApplyInflectionFeatures_DoesNotThrow_WhenMsaIsNull()
		{
			var chosen = new FwSandboxMsa(FwMsaType.Inflectional, mainPosId: _verb.Guid.ToString());
			Assert.DoesNotThrow(() =>
				GrammaticalInfoProjection.ApplyInflectionFeatures(Cache, (IMoMorphSynAnalysis)null, chosen));
		}

		[Test]
		public void ApplyInflectionFeatures_DoesNotThrow_WhenCacheIsNull()
		{
			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			_cantar.MorphoSyntaxAnalysesOC.Add(msa);
			var chosen = new FwSandboxMsa(FwMsaType.Inflectional, mainPosId: _verb.Guid.ToString());

			Assert.DoesNotThrow(() => GrammaticalInfoProjection.ApplyInflectionFeatures(null, msa, chosen));
		}

		// ----- resolving a chosen descriptor back into a SandboxGenericMSA -----

		[Test]
		public void BuildSandboxMsa_ResolvesTheChosenMainPos()
		{
			var chosen = new FwSandboxMsa(FwMsaType.Stem, mainPosId: _noun.Guid.ToString());
			var morphType = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
				.GetObject(MoMorphTypeTags.kguidMorphStem);

			var resolved = GrammaticalInfoProjection.BuildSandboxMsa(Cache, chosen, morphType);

			Assert.That(resolved.MsaType, Is.EqualTo(MsaType.kStem));
			Assert.That(resolved.MainPOS, Is.SameAs(_noun), "the chosen main POS id resolves to the live POS");
		}

		[Test]
		public void BuildSandboxMsa_ResolvesTheSlot_ForAnInflectionalAffix()
		{
			var chosen = new FwSandboxMsa(FwMsaType.Inflectional, mainPosId: _verb.Guid.ToString(),
				slotId: _tenseSlot.Guid.ToString());
			var morphType = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
				.GetObject(MoMorphTypeTags.kguidMorphSuffix);

			var resolved = GrammaticalInfoProjection.BuildSandboxMsa(Cache, chosen, morphType);

			Assert.That(resolved.MsaType, Is.EqualTo(MsaType.kInfl));
			Assert.That(resolved.Slot, Is.SameAs(_tenseSlot), "an inflectional affix carries its slot");
		}

		[Test]
		public void BuildSandboxMsa_FallsBackToTheMorphTypeDefault_WhenNoDescriptorIsChosen()
		{
			var suffix = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
				.GetObject(MoMorphTypeTags.kguidMorphSuffix);

			var resolved = GrammaticalInfoProjection.BuildSandboxMsa(Cache, null, suffix);

			Assert.That(resolved.MsaType, Is.EqualTo(MsaType.kUnclassified),
				"an affix with no chosen descriptor opens as an unclassified-affix MSA");
		}

		[Test]
		public void MorphTypeGuidToMsaType_MapsAnUnknownGuidToUnclassified()
		{
			Assert.That(GrammaticalInfoProjection.MorphTypeGuidToMsaType("not-a-guid"),
				Is.EqualTo(FwMsaType.Unclassified),
				"an unrecognized morph type falls into the affix family's Unclassified");
		}
	}
}
