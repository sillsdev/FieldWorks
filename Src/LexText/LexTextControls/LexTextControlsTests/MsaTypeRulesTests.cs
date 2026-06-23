// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.LexText.Controls;
using SIL.LCModel;

namespace LexTextControlsTests
{
	/// <summary>
	/// Pure tests (no cache, no UI, no STA) for the MSA editing rules extracted from MSAGroupBox.
	/// </summary>
	[TestFixture]
	public class MsaTypeRulesTests
	{
		[TestCase(MsaType.kUnclassified, true)]
		[TestCase(MsaType.kInfl, true)]
		[TestCase(MsaType.kDeriv, true)]
		[TestCase(MsaType.kStem, false)]
		[TestCase(MsaType.kRoot, false)]
		[TestCase(MsaType.kNotSet, false)]
		[TestCase(MsaType.kMixed, false)]
		public void IsAffixMsaType_OnlyAffixTypesAreAffixes(MsaType type, bool expected)
		{
			Assert.That(MsaTypeRules.IsAffixMsaType(type), Is.EqualTo(expected));
		}

		// Encodes the regression fix (LT-22194): setting the main category must not force kStem
		// when an affix type has already been established.
		[TestCase(MsaType.kUnclassified, false)]
		[TestCase(MsaType.kInfl, false)]
		[TestCase(MsaType.kDeriv, false)]
		[TestCase(MsaType.kStem, false)]
		[TestCase(MsaType.kRoot, true)]
		[TestCase(MsaType.kNotSet, true)]
		[TestCase(MsaType.kMixed, true)]
		public void ShouldForceStemForMainPos_PreservesAffixTypes(MsaType current, bool expected)
		{
			Assert.That(MsaTypeRules.ShouldForceStemForMainPos(current), Is.EqualTo(expected));
		}

		[TestCase(MoMorphTypeTags.kMorphStem)]
		[TestCase(MoMorphTypeTags.kMorphBoundStem)]
		[TestCase(MoMorphTypeTags.kMorphPhrase)]
		[TestCase(MoMorphTypeTags.kMorphDiscontiguousPhrase)]
		public void MsaTypeForMorphType_StemLikeTypes_ReturnStem(string morphTypeGuid)
		{
			// Result is independent of the current type for stem-like morpheme types.
			Assert.That(MsaTypeRules.MsaTypeForMorphType(morphTypeGuid, MsaType.kNotSet), Is.EqualTo(MsaType.kStem));
			Assert.That(MsaTypeRules.MsaTypeForMorphType(morphTypeGuid, MsaType.kInfl), Is.EqualTo(MsaType.kStem));
		}

		[TestCase(MoMorphTypeTags.kMorphProclitic)]
		[TestCase(MoMorphTypeTags.kMorphClitic)]
		[TestCase(MoMorphTypeTags.kMorphEnclitic)]
		[TestCase(MoMorphTypeTags.kMorphParticle)]
		[TestCase(MoMorphTypeTags.kMorphRoot)]
		[TestCase(MoMorphTypeTags.kMorphBoundRoot)]
		public void MsaTypeForMorphType_RootLikeTypes_ReturnRoot(string morphTypeGuid)
		{
			Assert.That(MsaTypeRules.MsaTypeForMorphType(morphTypeGuid, MsaType.kNotSet), Is.EqualTo(MsaType.kRoot));
		}

		[TestCase(MoMorphTypeTags.kMorphPrefix)]
		[TestCase(MoMorphTypeTags.kMorphSuffix)]
		[TestCase(MoMorphTypeTags.kMorphInfix)]
		[TestCase(MoMorphTypeTags.kMorphCircumfix)]
		public void MsaTypeForMorphType_AffixTypes_FromStemLike_ReturnUnclassified(string morphTypeGuid)
		{
			Assert.That(MsaTypeRules.MsaTypeForMorphType(morphTypeGuid, MsaType.kStem), Is.EqualTo(MsaType.kUnclassified));
			Assert.That(MsaTypeRules.MsaTypeForMorphType(morphTypeGuid, MsaType.kRoot), Is.EqualTo(MsaType.kUnclassified));
		}

		[TestCase(MsaType.kUnclassified)]
		[TestCase(MsaType.kInfl)]
		[TestCase(MsaType.kDeriv)]
		public void MsaTypeForMorphType_AffixTypes_KeepExistingAffixType(MsaType current)
		{
			// An affix morpheme type must not downgrade a more specific affix type already chosen.
			Assert.That(MsaTypeRules.MsaTypeForMorphType(MoMorphTypeTags.kMorphPrefix, current), Is.EqualTo(current));
		}
	}

	/// <summary>
	/// Cache-backed tests (no UI, no STA) for <see cref="MsaTypeRules.BuildSandboxMsa"/>, which needs
	/// real IPartOfSpeech / IMoInflAffixSlot model objects. The objects are created in CreateTestData
	/// so they run inside the base fixture's unit of work (avoiding nested-task errors).
	/// </summary>
	[TestFixture]
	public class MsaTypeRulesSandboxMsaTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IPartOfSpeech m_mainPos;
		private IPartOfSpeech m_secondaryPos;
		private IMoInflAffixSlot m_slot;

		protected override void CreateTestData()
		{
			base.CreateTestData();
			var posFactory = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
			m_mainPos = posFactory.Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(m_mainPos);
			m_secondaryPos = posFactory.Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(m_secondaryPos);
			m_slot = Cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>().Create();
			m_mainPos.AffixSlotsOC.Add(m_slot);
		}

		// SandboxGenericMSA stores a root as a stem MSA, so kRoot reads back as kStem.
		[TestCase(MsaType.kStem, MsaType.kStem)]
		[TestCase(MsaType.kRoot, MsaType.kStem)]
		[TestCase(MsaType.kUnclassified, MsaType.kUnclassified)]
		public void BuildSandboxMsa_SimpleTypes_SetOnlyMainPos(MsaType type, MsaType expectedMsaType)
		{
			var msa = MsaTypeRules.BuildSandboxMsa(type, m_mainPos, null, null, false);

			Assert.That(msa.MsaType, Is.EqualTo(expectedMsaType));
			Assert.That(msa.MainPOS, Is.EqualTo(m_mainPos));
			Assert.That(msa.SecondaryPOS, Is.Null);
			Assert.That(msa.Slot, Is.Null);
		}

		[Test]
		public void BuildSandboxMsa_Deriv_SetsSecondaryPos()
		{
			var msa = MsaTypeRules.BuildSandboxMsa(MsaType.kDeriv, m_mainPos, m_secondaryPos, null, false);

			Assert.That(msa.MsaType, Is.EqualTo(MsaType.kDeriv));
			Assert.That(msa.MainPOS, Is.EqualTo(m_mainPos));
			Assert.That(msa.SecondaryPOS, Is.EqualTo(m_secondaryPos));
			Assert.That(msa.Slot, Is.Null);
		}

		[Test]
		public void BuildSandboxMsa_Infl_IncludesSlotOnlyWhenValid()
		{
			Assert.That(MsaTypeRules.BuildSandboxMsa(MsaType.kInfl, m_mainPos, null, m_slot, true).Slot,
				Is.EqualTo(m_slot), "a valid slot should be included");
			Assert.That(MsaTypeRules.BuildSandboxMsa(MsaType.kInfl, m_mainPos, null, m_slot, false).Slot,
				Is.Null, "slot must be dropped when not valid for the category");
			Assert.That(MsaTypeRules.BuildSandboxMsa(MsaType.kInfl, m_mainPos, null, null, true).Slot,
				Is.Null, "a null slot stays null");
		}
	}
}
