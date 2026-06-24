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
	/// avalonia-interlinear-editor (task 3.3) — T4 WORKFLOW (end-to-end, headless, real cache): the user
	/// edits a morph-bundle's sense through the REAL fenced region edit context, commits, and the analysis is
	/// RE-PROJECTED from domain truth (the host re-show) so the assertions prove the data genuinely
	/// round-tripped — not merely that a setter returned true. The gesture also prunes an orphaned MSA
	/// (Sandbox parity, design Decision 4) and lands as ONE undoable step: a single Undo reverts the sense
	/// change AND restores the pruned MSA, proving the gesture is atomic.
	/// <para>The host edit context is the region's own composed <see cref="RegionEditContextBase"/>
	/// (<c>FullEntryRegionComposer.Compose(analysis).EditContext</c>) — the same fenced session every other
	/// row stages on, so the interlinear edit shares the region's undo fence.</para>
	/// </summary>
	[TestFixture]
	public class InterlinearWriteBackWorkflowTests : MemoryOnlyBackendProviderTestBase
	{
		private IWfiAnalysis m_analysis;
		private IWfiMorphBundle m_bundle;
		private ILexEntry m_entry;
		private ILexSense m_senseA;
		private IMoMorphSynAnalysis m_msaA;
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
				m_analysis.SetAgentOpinion(Cache.LangProject.DefaultUserAgent, Opinions.approves);

				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var moForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = moForm;
				moForm.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("pula", Cache.DefaultVernWs));

				m_msaA = MakeStemMsa("n");
				m_danglingMsa = MakeStemMsa("adj"); // referenced by no sense
				m_senseA = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_entry.SensesOS.Add(m_senseA);
				m_senseA.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("rain", Cache.DefaultAnalWs));
				m_senseA.MorphoSyntaxAnalysisRA = m_msaA;

				m_bundle = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
				m_analysis.MorphBundlesOS.Add(m_bundle);
				m_bundle.MorphRA = moForm;
				m_bundle.MsaRA = m_danglingMsa; // bundle starts on the dangling MSA, no sense
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

		[Test]
		public void EditBundleSense_Commits_RoundTrips_PrunesOrphan_AndIsOneUndoStep()
		{
			var danglingHvo = m_danglingMsa.Hvo;

			// The region's own fenced edit context is the host (composed exactly as the product wires it).
			var host = FullEntryRegionComposer.Compose(m_analysis, Cache).EditContext;
			var writeBack = new InterlinearAnalysisWriteBack(Cache, host, m_analysis);

			// GESTURE: pick senseA for bundle 0 (stages on the fenced session) → then the host commits.
			Assert.That(writeBack.ChooseSense(0, m_senseA.Guid.ToString()), Is.True);
			host.Commit();

			// RE-PROJECT from domain truth (the host re-show): the edit round-tripped.
			var reprojected = InterlinearAnalysisProjector.ProjectAnalysis(m_analysis, Cache);
			Assert.That(reprojected.Lines[0].Bundles[0].Gloss, Is.EqualTo("rain"),
				"the chosen sense's gloss round-trips through a real commit + re-projection");
			Assert.That(reprojected.Lines[0].Bundles[0].SenseGuid, Is.EqualTo(m_senseA.Guid));
			Assert.That(reprojected.Lines[0].Bundles[0].MsaGuid, Is.EqualTo(m_msaA.Guid),
				"the bundle's MSA derived from the chosen sense");

			// PRUNE: the orphaned dangling MSA was deleted in the same UOW.
			Assert.That(Cache.ServiceLocator.IsValidObjectId(danglingHvo), Is.False,
				"the orphaned MSA no surviving sense uses was pruned (Sandbox parity)");

			// ONE UNDO STEP: a single Undo reverts the sense change AND restores the pruned MSA (atomic gesture).
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_bundle.SenseRA, Is.Null, "undo reverted the bundle's sense to its original (none)");
			Assert.That(m_bundle.MsaRA, Is.EqualTo(m_danglingMsa), "undo restored the bundle's original MSA");
			Assert.That(Cache.ServiceLocator.IsValidObjectId(danglingHvo), Is.True,
				"undo restored the pruned MSA — the prune was part of the SAME undoable step");
		}

		[Test]
		public void EditBundleMsa_ThroughFencedHost_Commits_RoundTrips_PrunesOrphan_AndIsOneUndoStep()
		{
			// The grammatical-info (MSA) gesture rides the SAME fenced Stage path as the sense gesture — this
			// proves it end-to-end through the composed host (commit + re-project + atomic undo), not just via
			// the direct ApplyMsaChoice unit test.
			var danglingHvo = m_danglingMsa.Hvo;

			var host = FullEntryRegionComposer.Compose(m_analysis, Cache).EditContext;
			var writeBack = new InterlinearAnalysisWriteBack(Cache, host, m_analysis);

			Assert.That(writeBack.ChooseMsa(0, m_msaA.Guid.ToString()), Is.True);
			host.Commit();

			var reprojected = InterlinearAnalysisProjector.ProjectAnalysis(m_analysis, Cache);
			Assert.That(reprojected.Lines[0].Bundles[0].MsaGuid, Is.EqualTo(m_msaA.Guid),
				"the grammatical-info choice round-trips through a real commit + re-projection");
			Assert.That(m_bundle.SenseRA, Is.Null, "the MSA gesture left the sense untouched (independent line)");
			Assert.That(Cache.ServiceLocator.IsValidObjectId(danglingHvo), Is.False,
				"the orphaned MSA the bundle moved off was pruned");

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_bundle.MsaRA, Is.EqualTo(m_danglingMsa), "undo restored the bundle's original MSA");
			Assert.That(Cache.ServiceLocator.IsValidObjectId(danglingHvo), Is.True,
				"undo restored the pruned MSA — the MSA gesture is one atomic undoable step");
		}
	}
}
