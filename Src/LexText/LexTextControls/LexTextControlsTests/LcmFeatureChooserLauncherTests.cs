// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Reflection;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.LexText.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;

namespace LexTextControlsTests
{
	/// <summary>
	/// The LCModel-aware side of the §19b Stage-3 standalone feature-structure chooser launchers
	/// (<see cref="LcmInflectionFeatureChooserLauncher"/> / <see cref="LcmPhonologicalFeatureChooserLauncher"/>) and
	/// the shared write path (<c>FwFeatureStructureAdapter.ApplyFeaturesToOwner</c>) over a real LcmCache: building the
	/// feature system + current assignments, rebuilding the IFsFeatStruc from a chosen assignment set, the inflection
	/// LT-13596 empty-FS delete vs the phonological keep-empty, and the create-feature → assign → commit → reopen
	/// round-trip (T2/T4). The modal loop is desktop-only (exercised by the headless FeatureChooserDialogTests). The
	/// base opens an undoable UOW in TestSetup; the create cores open their own, so tests that create end the base
	/// task first.
	/// </summary>
	[TestFixture]
	public class LcmFeatureChooserLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IPartOfSpeech _verb;
		private IFsClosedFeature _tense;
		private IFsSymFeatVal _past;
		private IFsSymFeatVal _present;

		protected override void CreateTestData()
		{
			base.CreateTestData();
			_verb = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LanguageProject.PartsOfSpeechOA.PossibilitiesOS.Add(_verb);
			_verb.Name.set_String(Cache.DefaultAnalWs, "Verb");

			_tense = Cache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create();
			Cache.LangProject.MsFeatureSystemOA.FeaturesOC.Add(_tense);
			_tense.Name.set_String(Cache.DefaultAnalWs, "Tense");
			_past = Cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>().Create();
			_tense.ValuesOC.Add(_past);
			_past.Name.set_String(Cache.DefaultAnalWs, "past");
			_present = Cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>().Create();
			_tense.ValuesOC.Add(_present);
			_present.Name.set_String(Cache.DefaultAnalWs, "present");
			_verb.InflectableFeatsRC.Add(_tense);
		}

		// An MSA that owns an inflection IFsFeatStruc (the surface the chooser edits).
		private IMoInflAffMsa MakeInflMsa()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			msa.PartOfSpeechRA = _verb;
			return msa;
		}

		// ----- BuildInput: the inflection chooser feeds the POS's feature system + seeds the existing assignments ---

		[Test]
		public void InflectionBuildInput_FeedsPosSystemAndSeedsExistingAssignments()
		{
			var msa = MakeInflMsa();
			var msaInflFlid = MoInflAffMsaTags.kflidInflFeats;
			// Seed an existing FS with Tense=past via the adapter.
			var nodes = FwFeatureStructureAdapter.BuildNodes(_verb);
			FwFeatureStructureAdapter.ApplyInflectionFeatures(Cache, msa, nodes,
				new[] { new FwFeatureValueAssignment(_tense.Guid.ToString(), _past.Guid.ToString()) });
			var fs = msa.InflFeatsOA;

			var launcher = TestableInflectionLauncher(_verb, fs, msa, msaInflFlid);
			var input = launcher.BuildInput(Cache, _verb, fs);

			Assert.That(input.Nodes.Any(n => n.Id == _tense.Guid.ToString()), Is.True,
				"the POS's inflectable features feed the chooser");
			Assert.That(input.InitialAssignments.Single().ValueId, Is.EqualTo(_past.Guid.ToString()),
				"the existing IFsFeatStruc seeds the assignment set");
		}

		// ----- write round-trip: a chosen assignment set rebuilds the FS; empty deletes it (inflection LT-13596) -----

		[Test]
		public void ApplyFeaturesToOwner_Inflection_RebuildsThenEmptyDeletesTheFs()
		{
			var msa = MakeInflMsa(); // created inside the base's open UOW
			m_actionHandler.EndUndoTask();
			var nodes = FwFeatureStructureAdapter.BuildNodes(_verb);

			// Assign Tense=present: an FS is created on the MSA and carries the value.
			IFsFeatStruc fs = null;
			UndoableUnitOfWorkHelper.Do("u", "r", Cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				fs = FwFeatureStructureAdapter.ApplyFeaturesToOwner(Cache, msa.InflFeatsOA, msa,
					MoInflAffMsaTags.kflidInflFeats, nodes,
					new[] { new FwFeatureValueAssignment(_tense.Guid.ToString(), _present.Guid.ToString()) },
					deleteWhenEmpty: true);
			});
			Assert.That(fs, Is.Not.Null, "assigning a value creates the FS");
			Assert.That(FwFeatureStructureAdapter.ReadAssignments(fs).Single().ValueId,
				Is.EqualTo(_present.Guid.ToString()), "the FS round-trips the chosen value");

			// Now choose nothing: the inflection path deletes the emptied FS (LT-13596).
			IFsFeatStruc after = fs;
			UndoableUnitOfWorkHelper.Do("u", "r", Cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				after = FwFeatureStructureAdapter.ApplyFeaturesToOwner(Cache, fs, msa,
					MoInflAffMsaTags.kflidInflFeats, nodes,
					new FwFeatureValueAssignment[0], deleteWhenEmpty: true);
			});
			Assert.That(after, Is.Null, "an emptied inflection FS is deleted (LT-13596)");
		}

		// ----- phonological: BuildInput feeds the flat PhFeatureSystem; empty does NOT delete -----

		[Test]
		public void PhonologicalBuildInput_FeedsFlatPhFeatureSystem()
		{
			m_actionHandler.EndUndoTask();
			// A phonological closed feature so the system is non-empty.
			LcmCreateFeatureLauncher.CreateClosedFeature(Cache, FeatureSystemKind.Phonological, "voiced", "vd");

			var launcher = TestablePhonologicalLauncher(null, null, 0);
			var input = launcher.BuildInput(Cache, null);

			Assert.That(input.Nodes.Any(n => n.Name == "voiced" && n.Kind == FwFeatureNodeKind.Closed), Is.True,
				"the phonological feature system feeds the chooser as a flat closed-feature list");
			Assert.That(input.Nodes.Count(n => n.Kind == FwFeatureNodeKind.Value), Is.EqualTo(2),
				"the feature's +/- values feed under it");
		}

		// ----- T4 workflow: edit MSA -> need a feature that doesn't exist -> create it -> assign -> commit -> reopen ---

		[Test]
		public void Workflow_CreateFeature_AssignValue_Commit_Reopen_RoundTrips()
		{
			var msa = MakeInflMsa(); // created inside the base's open UOW
			m_actionHandler.EndUndoTask();

			// 1) The needed feature doesn't exist yet: create it (the inline create-feature flow).
			var (newFeatureNode, _) = LcmCreateFeatureLauncher.CreateClosedFeature(
				Cache, FeatureSystemKind.Inflection, "Aspect", "asp");
			// 2) Add a value to it (the add-value flow).
			var newValueNode = LcmCreateFeatureLauncher.CreateValue(
				Cache, FeatureSystemKind.Inflection, newFeatureNode.Id, "perfective", "pfv");
			// The new feature must be attached to the POS so the editor's POS-driven system shows it.
			var feature = (IFsClosedFeature)Cache.ServiceLocator.GetInstance<IFsClosedFeatureRepository>()
				.GetObject(System.Guid.Parse(newFeatureNode.Id));
			UndoableUnitOfWorkHelper.Do("u", "r", Cache.ServiceLocator.GetInstance<IActionHandler>(),
				() => _verb.InflectableFeatsRC.Add(feature));

			// 3) Assign the new value and commit (rebuild the MSA's IFsFeatStruc).
			var nodes = FwFeatureStructureAdapter.BuildNodes(_verb);
			IFsFeatStruc fs = null;
			UndoableUnitOfWorkHelper.Do("u", "r", Cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				fs = FwFeatureStructureAdapter.ApplyFeaturesToOwner(Cache, msa.InflFeatsOA, msa,
					MoInflAffMsaTags.kflidInflFeats, nodes,
					new[] { new FwFeatureValueAssignment(newFeatureNode.Id, newValueNode.Id) },
					deleteWhenEmpty: true);
			});

			// 4) Reopen: read the FS back and verify the just-created feature+value round-tripped.
			var reopened = FwFeatureStructureAdapter.ReadAssignments(msa.InflFeatsOA);
			Assert.That(reopened.Single().ClosedFeatureId, Is.EqualTo(newFeatureNode.Id),
				"the created feature is persisted on the MSA's IFsFeatStruc");
			Assert.That(reopened.Single().ValueId, Is.EqualTo(newValueNode.Id),
				"the created value round-trips");
		}

		// ----- POS resolution for the standalone slice path (GetPosFromCmObjectAndFlid lift) -----

		[Test]
		public void GetInflectionFeaturePos_ResolvesFromMsaOwner()
		{
			var msa = MakeInflMsa();
			var pos = FwFeatureStructureAdapter.GetInflectionFeaturePos((ICmObject)msa,
				MoInflAffMsaTags.kflidInflFeats);
			Assert.That(pos, Is.SameAs(_verb), "the inflection-affix MSA's POS resolves (GetPosFromCmObjectAndFlid)");
		}

		// ----- Show guard clauses: a null cache must fail fast at the public entry point, before any modal is built ---

		[Test]
		public void InflectionShow_NullCache_ThrowsArgumentNullException()
		{
			Assert.That(() => LcmInflectionFeatureChooserLauncher.Show(null, null, null, _verb, null, null,
					MoInflAffMsaTags.kflidInflFeats, null, null, out _),
				Throws.ArgumentNullException,
				"a null cache must fail fast instead of NRE-ing later when the launcher builds its state");
		}

		[Test]
		public void PhonologicalShow_NullCache_ThrowsArgumentNullException()
		{
			Assert.That(() => LcmPhonologicalFeatureChooserLauncher.Show(null, null, null, null, null,
					MoInflAffMsaTags.kflidInflFeats, null, null, out _),
				Throws.ArgumentNullException,
				"a null cache must fail fast instead of NRE-ing later when the launcher builds its state");
		}

		// ----- Apply: the launcher must wire its own deleteWhenEmpty flag correctly (LT-13596 vs phonological keep) ---

		[Test]
		public void InflectionApply_EmptyAssignments_DeletesTheFs()
		{
			var msa = MakeInflMsa(); // created inside the base's open UOW
			m_actionHandler.EndUndoTask();
			var nodes = FwFeatureStructureAdapter.BuildNodes(_verb);

			// Seed an existing FS with a real value so there is something to delete.
			IFsFeatStruc fs = null;
			UndoableUnitOfWorkHelper.Do("u", "r", Cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				fs = FwFeatureStructureAdapter.ApplyFeaturesToOwner(Cache, msa.InflFeatsOA, msa,
					MoInflAffMsaTags.kflidInflFeats, nodes,
					new[] { new FwFeatureValueAssignment(_tense.Guid.ToString(), _past.Guid.ToString()) },
					deleteWhenEmpty: true);
			});
			Assert.That(fs, Is.Not.Null, "seeding the FS with a value must succeed before we can test emptying it");

			var launcher = TestableInflectionLauncher(_verb, fs, msa, MoInflAffMsaTags.kflidInflFeats);
			launcher.BuildInput(Cache, _verb, fs); // populates the launcher's captured node system, as BuildState would

			var resultFs = InvokeApply(launcher, null);

			Assert.That(resultFs, Is.Null,
				"the inflection launcher's Apply must wire deleteWhenEmpty:true so an emptied FS is deleted (LT-13596); " +
				"wiring it wrong leaves a stale empty FS behind");
		}

		[Test]
		public void PhonologicalApply_EmptyAssignments_KeepsTheFs()
		{
			var msa = MakeInflMsa(); // reuse the MSA's inflection-feats slot purely as a generic FS-owning flid
			m_actionHandler.EndUndoTask();
			var nodes = FwFeatureStructureAdapter.BuildNodes(_verb);

			// Seed an existing FS with a real value so there is something that could (wrongly) be deleted.
			IFsFeatStruc fs = null;
			UndoableUnitOfWorkHelper.Do("u", "r", Cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				fs = FwFeatureStructureAdapter.ApplyFeaturesToOwner(Cache, msa.InflFeatsOA, msa,
					MoInflAffMsaTags.kflidInflFeats, nodes,
					new[] { new FwFeatureValueAssignment(_tense.Guid.ToString(), _past.Guid.ToString()) },
					deleteWhenEmpty: true);
			});
			Assert.That(fs, Is.Not.Null, "seeding the FS with a value must succeed before we can test emptying it");

			var launcher = TestablePhonologicalLauncher(fs, msa, MoInflAffMsaTags.kflidInflFeats);
			launcher.BuildInput(Cache, fs); // populates the launcher's captured node system, as BuildState would

			var resultFs = InvokeApply(launcher, null);

			Assert.That(resultFs, Is.Not.Null,
				"the phonological launcher's Apply must wire deleteWhenEmpty:false so an emptied FS is kept, matching " +
				"the legacy PhonologicalFeatureChooserDlg_Closing behavior; wiring it like the inflection launcher would " +
				"wrongly delete the FS");
		}

		// ----- test helpers: build the (private-ctor) launchers via reflection-free internal entry isn't available,
		//        so use a tiny shim that exposes BuildInput by constructing through the public Show path is overkill;
		//        instead we test BuildInput through a minimal internal-accessible wrapper. -----

		private LcmInflectionFeatureChooserLauncher TestableInflectionLauncher(IPartOfSpeech pos, IFsFeatStruc fs,
			ICmObject owner, int flid)
			=> (LcmInflectionFeatureChooserLauncher)System.Activator.CreateInstance(
				typeof(LcmInflectionFeatureChooserLauncher),
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null,
				new object[] { Cache, null, null, null, pos, fs, owner, flid }, null);

		private LcmPhonologicalFeatureChooserLauncher TestablePhonologicalLauncher(IFsFeatStruc fs, ICmObject owner,
			int flid)
			=> (LcmPhonologicalFeatureChooserLauncher)System.Activator.CreateInstance(
				typeof(LcmPhonologicalFeatureChooserLauncher),
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null,
				new object[] { Cache, null, null, null, fs, owner, flid }, null);

		// Invokes the protected-override "Apply(state)" step directly (the write-back the base's Run() calls after an
		// OK) without going through the modal Run()/ShowModal loop. _viewModel is left null by construction, so Apply
		// falls back to FeatureChooserPayload.Empty (an empty-but-non-null assignment set) -- exactly the "user chose
		// nothing" case this covers. Returns the launcher's ResultFs after Apply runs.
		private static IFsFeatStruc InvokeApply(object launcher, object state)
		{
			var applyMethod = launcher.GetType().GetMethod("Apply", BindingFlags.NonPublic | BindingFlags.Instance);
			applyMethod.Invoke(launcher, new[] { state });
			var resultFsProperty = launcher.GetType().GetProperty("ResultFs");
			return (IFsFeatStruc)resultFsProperty.GetValue(launcher);
		}
	}
}
