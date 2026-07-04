// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-interlinear-editor (task 3.2.1) — T1 for the write-back's MSA prune (THE risk, design
	/// Decision 4): mirrors the legacy <c>AnalysisInterlinearRs.SaveChanges</c> — when a bundle moves off an
	/// MSA that no surviving sense uses, that MSA is identified, <c>CanDelete</c> is true, and the delete
	/// succeeds; and the inverse — an MSA a surviving sense still references is NOT pruned. Over a real
	/// in-memory cache (<see cref="MemoryOnlyBackendProviderTestBase"/>); the prune logic is exercised
	/// directly through <see cref="InterlinearAnalysisWriteBack.ApplySenseChoice"/> inside a UOW.
	/// </summary>
	[TestFixture]
	public class InterlinearMSAPruneTests : MemoryOnlyBackendProviderTestBase
	{
		private IWfiAnalysis m_analysis;
		private IWfiMorphBundle m_bundle;
		private ILexEntry m_entry;
		private ILexSense m_senseA;
		private ILexSense m_senseB;
		private IMoMorphSynAnalysis m_msaA;
		private IMoMorphSynAnalysis m_msaB;
		private IMoMorphSynAnalysis m_danglingMsa;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var wordform = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
				wordform.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("pula", Cache.DefaultVernWs));
				m_analysis = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
				wordform.AnalysesOC.Add(m_analysis);

				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var moForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = moForm;
				moForm.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("pula", Cache.DefaultVernWs));

				m_msaA = MakeStemMsa("n");
				m_msaB = MakeStemMsa("v");
				m_danglingMsa = MakeStemMsa("adj"); // owned by the entry but referenced by NO sense

				m_senseA = MakeSense("rain", m_msaA);
				m_senseB = MakeSense("storm", m_msaB);

				m_bundle = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
				m_analysis.MorphBundlesOS.Add(m_bundle);
				m_bundle.MorphRA = moForm;
			});
		}

		private IMoMorphSynAnalysis MakeStemMsa(string posAbbr)
		{
			var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			m_entry.MorphoSyntaxAnalysesOC.Add(msa);
			var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
			pos.Abbreviation.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString(posAbbr, Cache.DefaultAnalWs));
			((IMoStemMsa)msa).PartOfSpeechRA = pos;
			return msa;
		}

		private ILexSense MakeSense(string gloss, IMoMorphSynAnalysis msa)
		{
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			m_entry.SensesOS.Add(sense);
			sense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString(gloss, Cache.DefaultAnalWs));
			sense.MorphoSyntaxAnalysisRA = msa;
			return sense;
		}

		private InterlinearAnalysisWriteBack WriteBack()
			=> new InterlinearAnalysisWriteBack(Cache, null /* no fenced host: apply directly in the test UOW */, m_analysis);

		[Test]
		public void OrphanedMsa_IsPruned_WhenTheBundleMovesOffIt()
		{
			// The bundle uses the DANGLING msa (no sense references it). Move the bundle to senseA, whose MSA
			// is msaA: the dangling MSA is now referenced by nothing → CanDelete → pruned (legacy parity).
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_bundle.MsaRA = m_danglingMsa;
				m_bundle.SenseRA = null;
			});
			// NB: m_danglingMsa.CanDelete is FALSE here — the bundle still references it. The legacy
			// SaveChanges re-points the bundle BEFORE checking CanDelete for exactly this reason; the real
			// check is that it becomes invalid AFTER the edit moves the bundle off it.
			var danglingHvo = m_danglingMsa.Hvo;

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => Assert.That(WriteBack().ApplySenseChoice(0, m_senseA.Guid.ToString()), Is.True));

			Assert.That(m_bundle.SenseRA, Is.EqualTo(m_senseA), "the bundle moved to senseA");
			Assert.That(m_bundle.MsaRA, Is.EqualTo(m_msaA), "the bundle's MSA derived from the chosen sense");
			Assert.That(Cache.ServiceLocator.IsValidObjectId(danglingHvo), Is.False,
				"the orphaned MSA no surviving sense uses was pruned");
		}

		[Test]
		public void MsaStillUsedByASurvivingSense_IsNotPruned()
		{
			// The bundle uses msaA, which senseA also references. Move the bundle to senseB: msaA was in the
			// analysis's referenced set, but senseA STILL uses it → NOT deletable → NOT pruned (the inverse).
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_bundle.MsaRA = m_msaA;
				m_bundle.SenseRA = m_senseA;
			});
			var msaAHvo = m_msaA.Hvo;

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => Assert.That(WriteBack().ApplySenseChoice(0, m_senseB.Guid.ToString()), Is.True));

			Assert.That(m_bundle.MsaRA, Is.EqualTo(m_msaB), "the bundle moved to senseB's MSA");
			Assert.That(Cache.ServiceLocator.IsValidObjectId(msaAHvo), Is.True,
				"an MSA a surviving sense (senseA) still references must NOT be pruned");
			Assert.That(m_senseA.MorphoSyntaxAnalysisRA, Is.EqualTo(m_msaA), "senseA still references msaA");
		}

		[Test]
		public void ChooseMsa_RepointsOnlyTheMsa_LeavingTheSense_AndPrunesOrphan()
		{
			// The grammatical-info line is independent of the sense (legacy renders it as its own combo).
			// The bundle uses the dangling MSA with senseA chosen; pick msaB for the grammatical-info line:
			// the bundle's MSA moves to msaB, the SENSE stays senseA, and the orphaned dangling MSA is pruned.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_bundle.SenseRA = m_senseA;
				m_bundle.MsaRA = m_danglingMsa;
			});
			var danglingHvo = m_danglingMsa.Hvo;

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => Assert.That(WriteBack().ApplyMsaChoice(0, m_msaB.Guid.ToString()), Is.True));

			Assert.That(m_bundle.MsaRA, Is.EqualTo(m_msaB), "the grammatical-info choice re-pointed the MSA");
			Assert.That(m_bundle.SenseRA, Is.EqualTo(m_senseA), "the MSA choice left the sense untouched (independent line)");
			Assert.That(Cache.ServiceLocator.IsValidObjectId(danglingHvo), Is.False,
				"the orphaned MSA the bundle moved off was pruned");
		}

		[Test]
		public void ChooseMorph_RepointsToAnotherEntry_ResetsSenseAndMsa_AndPrunesOrphan()
		{
			// A second homograph entry sharing the surface form "pula", with its own sense + MSA.
			ILexEntry other = null;
			IMoForm otherForm = null;
			ILexSense otherSense = null;
			IMoMorphSynAnalysis otherMsa = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				other = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				otherForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				other.LexemeFormOA = otherForm;
				otherForm.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("pula", Cache.DefaultVernWs));
				otherMsa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				other.MorphoSyntaxAnalysesOC.Add(otherMsa);
				var pos = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(pos);
				pos.Abbreviation.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("n2", Cache.DefaultAnalWs));
				((IMoStemMsa)otherMsa).PartOfSpeechRA = pos;
				otherSense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				other.SensesOS.Add(otherSense);
				otherSense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("downpour", Cache.DefaultAnalWs));
				otherSense.MorphoSyntaxAnalysisRA = otherMsa;

				// The bundle starts on m_entry's form with the dangling (orphan-able) MSA.
				m_bundle.MorphRA = m_entry.LexemeFormOA;
				m_bundle.SenseRA = null;
				m_bundle.MsaRA = m_danglingMsa;
			});
			var danglingHvo = m_danglingMsa.Hvo;

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => Assert.That(WriteBack().ApplyMorphChoice(0, otherForm.Guid.ToString()), Is.True));

			Assert.That(m_bundle.MorphRA, Is.EqualTo(otherForm), "the bundle re-points to the chosen entry's form");
			Assert.That(m_bundle.SenseRA, Is.EqualTo(otherSense), "sense resets to the new entry's default sense");
			Assert.That(m_bundle.MsaRA, Is.EqualTo(otherMsa), "MSA derives from that default sense");
			Assert.That(Cache.ServiceLocator.IsValidObjectId(danglingHvo), Is.False,
				"the orphaned MSA the bundle moved off was pruned");
		}

		[Test]
		public void ClearingTheSense_ClearsBundleSenseAndMsa()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_bundle.MsaRA = m_msaA;
				m_bundle.SenseRA = m_senseA;
			});

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => WriteBack().ApplySenseChoice(0, null));

			Assert.That(m_bundle.SenseRA, Is.Null, "a null key clears the bundle's sense");
			Assert.That(m_bundle.MsaRA, Is.Null, "clearing the sense clears the derived MSA");
		}
	}
}
