// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// The LCModel-aware <see cref="FwFeatureStructureAdapter"/> (Phase-1 §19b Stage 2): builds the depth-tagged
	/// feature-node system from a POS's <c>InflectableFeatsRC</c>, reads the flat assignment set from an existing
	/// <c>IFsFeatStruc</c>, and rebuilds the nested FS from the flat set (the recursive-ascent of
	/// <c>BuildFeatureStructure</c>). Proven against a REAL <c>LcmCache</c> with a programmatically built feature
	/// system: a top-level closed "aspect" feature and a complex "Agreement" feature whose type nests a closed
	/// "gender" feature, both inflectable on a Verb POS.
	/// </summary>
	[TestFixture]
	public class FwFeatureStructureAdapterTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IPartOfSpeech _verb;

		// Programmatically built feature system (the proven AddClosedFeature/AddFSType pattern from
		// ComplexConcPatternModelTests): a top-level closed "aspect" {imperfective, continuous} and a complex
		// "Agreement" feature whose type nests a closed "gender" {masculine gender, feminine gender}. Building it in
		// code (rather than the XML proxy import) gives stable guids + a guaranteed complex feature on the POS.
		private string _aspectId;

		protected override void CreateTestData()
		{
			base.CreateTestData();
			_verb = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LanguageProject.PartsOfSpeechOA.PossibilitiesOS.Add(_verb);
			_verb.Name.set_String(Cache.DefaultAnalWs, "Verb");
			BuildFeatureSystem();
		}

		private void BuildFeatureSystem()
		{
			var msfs = Cache.LanguageProject.MsFeatureSystemOA;

			// Top-level closed aspect feature {imperfective, continuous}.
			var aspect = AddClosedFeature(msfs, "aspect", "imperfective aspect", "continuous aspect");
			_aspectId = aspect.Guid.ToString();
			_verb.InflectableFeatsRC.Add(aspect);

			// Complex "Agreement" feature: its type nests a closed "gender" {masculine gender, feminine gender}.
			var gender = AddClosedFeature(msfs, "gender", "masculine gender", "feminine gender");
			var agrType = Cache.ServiceLocator.GetInstance<IFsFeatStrucTypeFactory>().Create();
			msfs.TypesOC.Add(agrType);
			agrType.Name.set_String(Cache.DefaultAnalWs, "agreement type");
			agrType.FeaturesRS.Add(gender);
			var agreement = Cache.ServiceLocator.GetInstance<IFsComplexFeatureFactory>().Create();
			msfs.FeaturesOC.Add(agreement);
			agreement.Name.set_String(Cache.DefaultAnalWs, "Agreement");
			agreement.TypeRA = agrType;
			_verb.InflectableFeatsRC.Add(agreement);
		}

		private IFsClosedFeature AddClosedFeature(IFsFeatureSystem msfs, string name, params string[] values)
		{
			var feat = Cache.ServiceLocator.GetInstance<IFsClosedFeatureFactory>().Create();
			msfs.FeaturesOC.Add(feat);
			feat.Name.set_String(Cache.DefaultAnalWs, name);
			foreach (var value in values)
			{
				var sym = Cache.ServiceLocator.GetInstance<IFsSymFeatValFactory>().Create();
				feat.ValuesOC.Add(sym);
				sym.Name.set_String(Cache.DefaultAnalWs, value);
			}
			return feat;
		}

		// ----- BuildNodes (PopulateTreeFromPos + AddNode) -----

		[Test]
		public void BuildNodes_ClosedFeature_EmitsClosedNodePlusValues()
		{
			var nodes = FwFeatureStructureAdapter.BuildNodes(_verb);

			// The aspect feature is a top-level closed node.
			var aspect = nodes.FirstOrDefault(n => n.Id == _aspectId);
			Assert.That(aspect, Is.Not.Null, "the top-level closed aspect feature is a node");
			Assert.That(aspect.Kind, Is.EqualTo(FwFeatureNodeKind.Closed));
			Assert.That(aspect.Depth, Is.EqualTo(0));
			// The values follow at depth+1 (the editor folds them under the closed feature).
			var aspectValues = nodes.Where(n => n.Kind == FwFeatureNodeKind.Value && n.Depth == 1).ToList();
			Assert.That(aspectValues, Is.Not.Empty, "the closed feature's values are emitted as Value nodes");
		}

		[Test]
		public void BuildNodes_ComplexFeature_NestsChildrenByDepth()
		{
			var nodes = FwFeatureStructureAdapter.BuildNodes(_verb);

			// The Agreement feature is a top-level Complex node.
			var complex = nodes.FirstOrDefault(n => n.Kind == FwFeatureNodeKind.Complex && n.Depth == 0);
			Assert.That(complex, Is.Not.Null, "the complex Agreement feature is a top-level Complex node");

			// Its nested closed features sit at depth 1, with their symbolic values at depth 2 (the recursive
			// AddNode fold). The gender feature is one of them.
			var nestedClosed = nodes.Where(n => n.Kind == FwFeatureNodeKind.Closed && n.Depth == 1).ToList();
			Assert.That(nestedClosed, Is.Not.Empty,
				"the complex feature's nested closed features are one level under it");
			var nestedValues = nodes.Where(n => n.Kind == FwFeatureNodeKind.Value && n.Depth == 2).ToList();
			Assert.That(nestedValues, Is.Not.Empty,
				"the nested closed features' values are two levels under the complex feature");
			Assert.That(nestedClosed.Any(n => n.Name == "gender"), Is.True,
				"the nested gender feature is present by name");
		}

		[Test]
		public void BuildNodes_NullPos_IsEmpty()
		{
			Assert.That(FwFeatureStructureAdapter.BuildNodes(null), Is.Empty);
		}

		// ----- WriteFeatures (BuildFeatureStructure recursive-ascent) + ReadAssignments round-trip -----

		[Test]
		public void WriteFeatures_RebuildsTopLevelClosedValue_AndReadsBack()
		{
			var nodes = FwFeatureStructureAdapter.BuildNodes(_verb);
			var aspectValueId = nodes.First(n => n.Kind == FwFeatureNodeKind.Value && n.Depth == 1).Id;

			var fs = MakeFeatureStructure();
			var assignments = new[] { new FwFeatureValueAssignment(_aspectId, aspectValueId) };

			FwFeatureStructureAdapter.WriteFeatures(Cache, fs, nodes, assignments);

			Assert.That(fs.FeatureSpecsOC.Count, Is.EqualTo(1), "one closed value written");
			var closed = fs.FeatureSpecsOC.OfType<IFsClosedValue>().Single();
			Assert.That(closed.FeatureRA.Guid.ToString(), Is.EqualTo(_aspectId));
			Assert.That(closed.ValueRA.Guid.ToString(), Is.EqualTo(aspectValueId));

			// Read it back to the same flat assignment.
			var readBack = FwFeatureStructureAdapter.ReadAssignments(fs);
			Assert.That(readBack.Count, Is.EqualTo(1));
			Assert.That(readBack[0].ClosedFeatureId, Is.EqualTo(_aspectId));
			Assert.That(readBack[0].ValueId, Is.EqualTo(aspectValueId));
		}

		[Test]
		public void WriteFeatures_RebuildsNestedComplex_AndReadsBack()
		{
			var nodes = FwFeatureStructureAdapter.BuildNodes(_verb);
			var fs = MakeFeatureStructure();
			// Pick the nested gender = feminine (under the complex subject-agreement feature). Resolve the ids from the
			// node list by name (the XML proxy import may mint fresh guids).
			var genderId = NodeId(nodes, FwFeatureNodeKind.Closed, "gender");
			var femId = NodeId(nodes, FwFeatureNodeKind.Value, "feminine gender");
			var assignments = new[] { new FwFeatureValueAssignment(genderId, femId) };

			FwFeatureStructureAdapter.WriteFeatures(Cache, fs, nodes, assignments);

			// Top level should be a single complex value whose nested FS holds the closed gender value (the
			// recursive-ascent nesting, mirroring BuildFeatureStructure).
			var complex = fs.FeatureSpecsOC.OfType<IFsComplexValue>().Single();
			Assert.That(complex.FeatureRA, Is.InstanceOf<IFsComplexFeature>(),
				"the nested feature's complex ancestor is reconstructed");
			var nested = (IFsFeatStruc)complex.ValueOA;
			var closed = nested.FeatureSpecsOC.OfType<IFsClosedValue>().Single();
			Assert.That(closed.FeatureRA.Guid.ToString(), Is.EqualTo(genderId));
			Assert.That(closed.ValueRA.Guid.ToString(), Is.EqualTo(femId));

			// Read-back flattens to the (closed feature, value) pair.
			var readBack = FwFeatureStructureAdapter.ReadAssignments(fs);
			Assert.That(readBack.Count, Is.EqualTo(1));
			Assert.That(readBack[0].ClosedFeatureId, Is.EqualTo(genderId));
			Assert.That(readBack[0].ValueId, Is.EqualTo(femId));
		}

		[Test]
		public void WriteFeatures_ClearsExistingSpecs_BeforeRebuild()
		{
			var nodes = FwFeatureStructureAdapter.BuildNodes(_verb);
			var fs = MakeFeatureStructure();
			var genderId = NodeId(nodes, FwFeatureNodeKind.Closed, "gender");
			var femId = NodeId(nodes, FwFeatureNodeKind.Value, "feminine gender");
			var mascId = NodeId(nodes, FwFeatureNodeKind.Value, "masculine gender");

			// First write feminine, then overwrite with masculine — the second write must clear the first.
			FwFeatureStructureAdapter.WriteFeatures(Cache, fs, nodes,
				new[] { new FwFeatureValueAssignment(genderId, femId) });
			FwFeatureStructureAdapter.WriteFeatures(Cache, fs, nodes,
				new[] { new FwFeatureValueAssignment(genderId, mascId) });

			var readBack = FwFeatureStructureAdapter.ReadAssignments(fs);
			Assert.That(readBack.Count, Is.EqualTo(1), "the prior spec was cleared");
			Assert.That(readBack[0].ValueId, Is.EqualTo(mascId), "only the latest value remains");
		}

		// Resolve a node's id by kind + display name from the built node list (the XML proxy import may mint guids).
		private static string NodeId(IReadOnlyList<FwFeatureNode> nodes, FwFeatureNodeKind kind, string name)
		{
			var node = nodes.FirstOrDefault(n => n.Kind == kind && n.Name == name);
			Assert.That(node, Is.Not.Null, $"expected a {kind} node named '{name}'");
			return node.Id;
		}

		[Test]
		public void WriteFeatures_DropsUnresolvableId()
		{
			var nodes = FwFeatureStructureAdapter.BuildNodes(_verb);
			var fs = MakeFeatureStructure();
			var bogus = "00000000-0000-0000-0000-000000000001";

			FwFeatureStructureAdapter.WriteFeatures(Cache, fs, nodes,
				new[] { new FwFeatureValueAssignment(bogus, bogus) });

			Assert.That(fs.FeatureSpecsOC, Is.Empty, "a stale/unresolvable assignment writes nothing");
		}

		// ----- ApplyInflectionFeatures (the create-side _Closing parity, scoped to InflFeatsOA) -----

		[Test]
		public void ApplyInflectionFeatures_InflAffMsa_CreatesAndRebuildsInflFeats()
		{
			var nodes = FwFeatureStructureAdapter.BuildNodes(_verb);
			var aspectValueId = nodes.First(n => n.Kind == FwFeatureNodeKind.Value && n.Depth == 1).Id;
			var msa = MakeInflAffMsa();

			FwFeatureStructureAdapter.ApplyInflectionFeatures(Cache, msa, nodes,
				new[] { new FwFeatureValueAssignment(_aspectId, aspectValueId) });

			Assert.That(msa.InflFeatsOA, Is.Not.Null, "the inflection FS is created on InflFeatsOA");
			var readBack = FwFeatureStructureAdapter.ReadAssignments(msa.InflFeatsOA);
			Assert.That(readBack.Single().ValueId, Is.EqualTo(aspectValueId), "the chosen value round-trips");
		}

		[Test]
		public void ApplyInflectionFeatures_EmptySet_DeletesFs_LT13596()
		{
			var nodes = FwFeatureStructureAdapter.BuildNodes(_verb);
			var aspectValueId = nodes.First(n => n.Kind == FwFeatureNodeKind.Value && n.Depth == 1).Id;
			var msa = MakeInflAffMsa();

			// First set a feature, then re-apply with an empty set (the legacy "None of the above" everywhere).
			FwFeatureStructureAdapter.ApplyInflectionFeatures(Cache, msa, nodes,
				new[] { new FwFeatureValueAssignment(_aspectId, aspectValueId) });
			Assert.That(msa.InflFeatsOA, Is.Not.Null);
			FwFeatureStructureAdapter.ApplyInflectionFeatures(Cache, msa, nodes,
				System.Array.Empty<FwFeatureValueAssignment>());

			Assert.That(msa.InflFeatsOA, Is.Null, "an emptied assignment set deletes the FS (LT-13596)");
		}

		[Test]
		public void ApplyInflectionFeatures_StemMsa_IsNoOp()
		{
			var nodes = FwFeatureStructureAdapter.BuildNodes(_verb);
			var aspectValueId = nodes.First(n => n.Kind == FwFeatureNodeKind.Value && n.Depth == 1).Id;
			var sense = MakeSenseWithStemMsa();

			FwFeatureStructureAdapter.ApplyInflectionFeatures(Cache, sense.MorphoSyntaxAnalysisRA, nodes,
				new[] { new FwFeatureValueAssignment(_aspectId, aspectValueId) });

			Assert.That(sense.MorphoSyntaxAnalysisRA, Is.InstanceOf<IMoStemMsa>(),
				"a stem MSA is unaffected (the feature editor is scoped to infl/deriv)");
		}

		// ----- helpers -----

		// The base (MemoryOnlyBackendProviderRestoredForEachTestTestBase) opens an undoable UOW in TestSetup, so all
		// creation/writes run directly here with NO UOW wrapper (a nested task would throw "Nested tasks are not
		// supported"). The adapter's WriteFeatures runs inside the same open task — exactly as the launcher's create
		// runs inside its own single UOW at runtime.
		private IFsFeatStruc MakeFeatureStructure()
		{
			_verb.DefaultFeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			return _verb.DefaultFeaturesOA;
		}

		private IMoInflAffMsa MakeInflAffMsa()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			entry.MorphoSyntaxAnalysesOC.Add(msa);
			msa.PartOfSpeechRA = _verb;
			return msa;
		}

		private ILexSense MakeSenseWithStemMsa()
		{
			var components = new LexEntryComponents
			{
				MorphType = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>()
					.GetObject(MoMorphTypeTags.kguidMorphStem)
			};
			components.LexemeFormAlternatives.Add(TsStringUtils.MakeString("x", Cache.DefaultVernWs));
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(components);
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.SandboxMSA = new SandboxGenericMSA { MsaType = MsaType.kStem, MainPOS = _verb };
			return sense;
		}
	}
}
