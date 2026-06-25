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
	/// The LCModel-aware side of the reusable Avalonia "Create New Grammatical Info." dialog launcher
	/// (<see cref="LcmMsaCreatorDialogLauncher"/>): building the read-only lexical entry + senses summary + the MSA
	/// section seeded from an existing <c>SandboxGenericMSA</c> from a real cache, and resolving the chosen
	/// <see cref="FwSandboxMsa"/> back into a real <c>SandboxGenericMSA</c> the caller applies (assign to a sense, or
	/// UpdateOrReplace an existing MSA). The modal loop is desktop-only (exercised by the headless
	/// MsaCreatorDialogTests); here we cover the pure LCModel mapping over a real LcmCache (via InternalsVisibleTo).
	/// </summary>
	[TestFixture]
	public class LcmMsaCreatorDialogLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ILexEntry _cantar;
		private IPartOfSpeech _verb;
		private IPartOfSpeech _noun;

		private IFsClosedFeature _tenseFeature;
		private IFsSymFeatVal _pastValue;

		protected override void CreateTestData()
		{
			base.CreateTestData();
			_cantar = MakeEntry("cantar", "to sing");
			_verb = MakePos("Verb");
			_noun = MakePos("Noun");

			// An inflectable closed feature on the verb (Tense {past}) for the §19b inflection-feature edit-path seed.
			_tenseFeature = Cache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create();
			Cache.LangProject.MsFeatureSystemOA.FeaturesOC.Add(_tenseFeature);
			_tenseFeature.Name.set_String(Cache.DefaultAnalWs, "Tense");
			_pastValue = Cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>().Create();
			_tenseFeature.ValuesOC.Add(_pastValue);
			_pastValue.Name.set_String(Cache.DefaultAnalWs, "past");
			_verb.InflectableFeatsRC.Add(_tenseFeature);
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

		// ----- BuildInput: read-only context + the box seeded from an existing MSA -----

		[Test]
		public void BuildInput_SeedsTheBoxFromAStemSandboxMsa()
		{
			var seed = new SandboxGenericMSA { MsaType = MsaType.kStem, MainPOS = _noun };
			var input = LcmMsaCreatorDialogLauncher.BuildInput(Cache, _cantar, seed,
				hvoOriginalMsa: 0, useForEdit: false, titleForEdit: null);

			Assert.That(input.LexicalEntry, Is.Not.Null.And.Not.Empty, "the read-only lexical entry headword is shown");
			Assert.That(input.InitialMsaType, Is.EqualTo(FwMsaType.Stem), "the box seeds the existing MSA class");
			Assert.That(input.InitialMainPosId, Is.EqualTo(_noun.Guid.ToString()), "and the existing main POS");
			Assert.That(input.PosNodes.Select(n => n.Name), Does.Contain("Verb").And.Contain("Noun"));
		}

		[Test]
		public void BuildInput_SeedsADerivationalSandboxMsa()
		{
			var seed = new SandboxGenericMSA { MsaType = MsaType.kDeriv, MainPOS = _verb, SecondaryPOS = _noun };
			var input = LcmMsaCreatorDialogLauncher.BuildInput(Cache, _cantar, seed, 0, false, null);

			Assert.That(input.InitialMsaType, Is.EqualTo(FwMsaType.Derivational));
			Assert.That(input.InitialMainPosId, Is.EqualTo(_verb.Guid.ToString()));
			Assert.That(input.InitialSecondaryPosId, Is.EqualTo(_noun.Guid.ToString()),
				"the derivational secondary POS is seeded");
		}

		[Test]
		public void BuildInput_EditTitle_OverridesTheDefaultTitle()
		{
			var seed = new SandboxGenericMSA { MsaType = MsaType.kStem };
			var input = LcmMsaCreatorDialogLauncher.BuildInput(Cache, _cantar, seed,
				hvoOriginalMsa: 0, useForEdit: true, titleForEdit: "Modify Grammatical Info");
			Assert.That(input.Title, Is.EqualTo("Modify Grammatical Info"),
				"the edit context overrides the create title (legacy useForEdit branch)");
		}

		// ----- the chosen FwSandboxMsa resolves back to a real SandboxGenericMSA (the shared resolver) -----

		[Test]
		public void BuildSandboxMsa_ResolvesAChosenStemMsa()
		{
			// The launcher's Apply uses the shared LcmInsertEntryDialogLauncher.BuildSandboxMsa; verify the
			// round-trip resolves the kit payload's POS ids back to the live objects.
			var chosen = new FwSandboxMsa(FwMsaType.Stem, mainPosId: _noun.Guid.ToString());
			var morphType = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
				.GetObject(MoMorphTypeTags.kguidMorphStem);

			var resolved = LcmInsertEntryDialogLauncher.BuildSandboxMsa(Cache, chosen, morphType);

			Assert.That(resolved.MsaType, Is.EqualTo(MsaType.kStem));
			Assert.That(resolved.MainPOS, Is.SameAs(_noun), "the chosen main POS id resolves to the live POS");
		}

		// ----- inflection-class feed + seed (Stage 6) -----

		[Test]
		public void BuildInput_FeedsTheInflectionClassProviderAndSeedsFromAnExistingStemMsa()
		{
			// An existing stem MSA on a sense, carrying an inflection class on the noun.
			var inflClass = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			_noun.InflectionClassesOC.Add(inflClass);
			inflClass.Name.set_String(Cache.DefaultAnalWs, "Weak");

			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			_cantar.SensesOS.Add(sense);
			sense.SandboxMSA = new SandboxGenericMSA { MsaType = MsaType.kStem, MainPOS = _noun };
			var stemMsa = (IMoStemMsa)sense.MorphoSyntaxAnalysisRA;
			stemMsa.InflectionClassRA = inflClass;

			var seed = new SandboxGenericMSA { MsaType = MsaType.kStem, MainPOS = _noun };
			var input = LcmMsaCreatorDialogLauncher.BuildInput(Cache, _cantar, seed,
				hvoOriginalMsa: stemMsa.Hvo, useForEdit: true, titleForEdit: null);

			Assert.That(input.InflectionClassesForPos, Is.Not.Null, "the inflection-class provider is wired");
			Assert.That(input.InflectionClassesForPos(_noun.Guid.ToString()).Select(c => c.Name),
				Does.Contain("Weak"), "the provider returns the selected POS's classes");
			Assert.That(input.InitialInflectionClassId, Is.EqualTo(inflClass.Guid.ToString()),
				"the existing stem MSA's inflection class seeds the picker");
		}

		[Test]
		public void InflectionClassIdFromExistingMsa_NoOriginalMsa_IsNull()
		{
			Assert.That(LcmMsaCreatorDialogLauncher.InflectionClassIdFromExistingMsa(Cache, hvoOriginalMsa: 0),
				Is.Null, "the create path (no original MSA) seeds no inflection class");
		}

		// ----- inflection-feature feed + seed from an existing MSA (§19b Stage 2 edit path) -----

		[Test]
		public void BuildInput_FeedsTheInflectionFeatureProviderAndSeedsFromAnExistingInflMsa()
		{
			// An existing inflectional-affix MSA on the verb, carrying a tense=past inflection feature.
			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			_cantar.MorphoSyntaxAnalysesOC.Add(msa);
			msa.PartOfSpeechRA = _verb;
			var nodes = FwFeatureStructureAdapter.BuildNodes(_verb);
			FwFeatureStructureAdapter.ApplyInflectionFeatures(Cache, msa, nodes,
				new[] { new FwFeatureValueAssignment(_tenseFeature.Guid.ToString(), _pastValue.Guid.ToString()) });

			var seed = new SandboxGenericMSA { MsaType = MsaType.kInfl, MainPOS = _verb };
			var input = LcmMsaCreatorDialogLauncher.BuildInput(Cache, _cantar, seed,
				hvoOriginalMsa: msa.Hvo, useForEdit: true, titleForEdit: null);

			Assert.That(input.InflectionFeaturesForPos, Is.Not.Null, "the inflection-feature provider is wired");
			Assert.That(input.InflectionFeaturesForPos(_verb.Guid.ToString())
					.Any(n => n.Id == _tenseFeature.Guid.ToString()),
				Is.True, "the provider returns the POS's inflectable features");
			Assert.That(input.InitialInflectionFeatures, Is.Not.Null.And.Not.Empty,
				"the existing MSA's IFsFeatStruc seeds the editor's assignment set");
			Assert.That(input.InitialInflectionFeatures[0].ValueId, Is.EqualTo(_pastValue.Guid.ToString()),
				"the seeded assignment matches the persisted feature value");
		}

		[Test]
		public void InflectionFeaturesFromExistingMsa_NoOriginalMsa_IsNull()
		{
			Assert.That(LcmMsaCreatorDialogLauncher.InflectionFeaturesFromExistingMsa(Cache, hvoOriginalMsa: 0),
				Is.Null, "the create path (no original MSA) seeds no inflection features");
		}

		[Test]
		public void BuildSensesSummary_NoOriginalMsa_IsEmpty()
		{
			Assert.That(LcmMsaCreatorDialogLauncher.BuildSensesSummary(Cache, _cantar, hvoOriginalMsa: 0),
				Is.Null, "with no original MSA (the create path) there is no senses summary");
		}

		[Test]
		public void BuildSensesSummary_ListsSensesSharingTheOriginalMsa()
		{
			// Give cantar a sense with an MSA, then verify the summary lists it on the edit path.
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			_cantar.SensesOS.Add(sense);
			sense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("to sing", Cache.DefaultAnalWs));
			sense.SandboxMSA = new SandboxGenericMSA { MsaType = MsaType.kStem, MainPOS = _verb };
			var msaHvo = sense.MorphoSyntaxAnalysisRA.Hvo;

			var summary = LcmMsaCreatorDialogLauncher.BuildSensesSummary(Cache, _cantar, msaHvo);
			Assert.That(summary, Is.Not.Null.And.Not.Empty,
				"the senses sharing the original MSA are summarized (the legacy m_fwtbSenses loop)");
		}
	}
}
