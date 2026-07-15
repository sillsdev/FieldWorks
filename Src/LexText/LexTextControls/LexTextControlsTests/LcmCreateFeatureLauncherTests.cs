// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.LexText.Controls;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LexTextControlsTests
{
	/// <summary>
	/// The LCModel-aware side of the §19b Stage-3 create-feature / add-value flow
	/// (<see cref="LcmCreateFeatureLauncher"/>): creating a closed feature in the inflection / phonological feature
	/// system and adding a symbolic value to a closed feature, over a real LcmCache (via InternalsVisibleTo) — the
	/// unit-testable core that mirrors MasterInflectionFeatureListDlg / MasterPhonologicalFeatureListDlg's blank-create
	/// + the feature-system add-value flow. The modal loop itself is desktop-only (exercised by the headless
	/// CreateFeatureDialogTests); here we cover the create cores + the round-trip through the feature system. The base
	/// opens an undoable UOW in TestSetup, and the create cores open their OWN UOW, so each test ends the base task
	/// first (a nested task would throw), mirroring LcmCreatePartOfSpeechLauncherTests.
	/// </summary>
	[TestFixture]
	public class LcmCreateFeatureLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		// ----- create an inflection (Ms) feature: a closed feature added to MsFeatureSystem + the Infl type -----

		[Test]
		public void CreateClosedFeature_Inflection_AddsToMsFeatureSystemAndInflType()
		{
			m_actionHandler.EndUndoTask(); // CreateClosedFeature opens its own undoable step

			var before = Cache.LanguageProject.MsFeatureSystemOA.FeaturesOC.Count;
			var (node, children) = LcmCreateFeatureLauncher.CreateClosedFeature(
				Cache, FeatureSystemKind.Inflection, "Tense", "tns");

			Assert.That(node, Is.Not.Null, "the new feature yields a node");
			Assert.That(node.Kind, Is.EqualTo(FwFeatureNodeKind.Closed));
			Assert.That(node.Name, Is.EqualTo("Tense"));
			Assert.That(children, Is.Null.Or.Empty, "a blank inflection feature has no default values");
			Assert.That(Cache.LanguageProject.MsFeatureSystemOA.FeaturesOC.Count, Is.EqualTo(before + 1),
				"the feature is added to the morphosyntactic feature system");

			// The "Infl" feature-structure type exists and contains the new feature (MasterInflectionFeatureListDlg parity).
			var inflType = Cache.LanguageProject.MsFeatureSystemOA.GetFeatureType("Infl");
			Assert.That(inflType, Is.Not.Null, "the 'Infl' feature-structure type is ensured");
			Assert.That(inflType.FeaturesRS.Any(f => f.Guid.ToString() == node.Id), Is.True,
				"the new feature is added to the Infl type's FeaturesRS");
		}

		// ----- create a phonological feature: a closed feature added to PhFeatureSystem + the +/- default values -----

		[Test]
		public void CreateClosedFeature_Phonological_AddsToPhFeatureSystemWithPlusMinusValues()
		{
			m_actionHandler.EndUndoTask();

			var before = Cache.LanguageProject.PhFeatureSystemOA.FeaturesOC.Count;
			var (node, children) = LcmCreateFeatureLauncher.CreateClosedFeature(
				Cache, FeatureSystemKind.Phonological, "voiced", "vd");

			Assert.That(node, Is.Not.Null);
			Assert.That(Cache.LanguageProject.PhFeatureSystemOA.FeaturesOC.Count, Is.EqualTo(before + 1),
				"the feature is added to the phonological feature system");
			Assert.That(children, Is.Not.Null);
			Assert.That(children.Count, Is.EqualTo(2),
				"a blank phonological feature gets the two default +/- values (MasterPhonologicalFeatureListDlg parity)");
			Assert.That(children.All(c => c.Kind == FwFeatureNodeKind.Value), Is.True);

			var feature = (IFsClosedFeature)Cache.ServiceLocator.GetInstance<IFsClosedFeatureRepository>()
				.GetObject(System.Guid.Parse(node.Id));
			Assert.That(feature.ValuesOC.Count, Is.EqualTo(2));
		}

		// ----- add a value to an existing closed feature -----

		[Test]
		public void CreateValue_AddsSymbolicValueToTheClosedFeature()
		{
			m_actionHandler.EndUndoTask();
			// A closed feature to add a value to.
			var (featureNode, _) = LcmCreateFeatureLauncher.CreateClosedFeature(
				Cache, FeatureSystemKind.Inflection, "Tense", "tns");
			var feature = (IFsClosedFeature)Cache.ServiceLocator.GetInstance<IFsClosedFeatureRepository>()
				.GetObject(System.Guid.Parse(featureNode.Id));
			var before = feature.ValuesOC.Count;

			var valueNode = LcmCreateFeatureLauncher.CreateValue(
				Cache, FeatureSystemKind.Inflection, featureNode.Id, "past", "pst");

			Assert.That(valueNode, Is.Not.Null);
			Assert.That(valueNode.Kind, Is.EqualTo(FwFeatureNodeKind.Value));
			Assert.That(valueNode.Name, Is.EqualTo("past"));
			Assert.That(feature.ValuesOC.Count, Is.EqualTo(before + 1), "the value is added to the closed feature");
			Assert.That(feature.ValuesOC.Any(v => v.Guid.ToString() == valueNode.Id), Is.True);
		}

		[Test]
		public void CreateValue_UnknownFeatureId_ReturnsNull()
		{
			m_actionHandler.EndUndoTask();
			Assert.That(LcmCreateFeatureLauncher.CreateValue(
				Cache, FeatureSystemKind.Inflection, System.Guid.NewGuid().ToString(), "x", "x"),
				Is.Null, "an unresolvable feature id is a no-op");
		}

		[Test]
		public void CreateClosedFeature_EmptyName_ReturnsNull()
		{
			m_actionHandler.EndUndoTask();
			var (node, children) = LcmCreateFeatureLauncher.CreateClosedFeature(
				Cache, FeatureSystemKind.Inflection, "", null);
			Assert.That(node, Is.Null, "an empty name yields no feature");
		}

		// ----- T2 integration: create a feature, then it appears in the (rebuilt) phonological feature system -----

		[Test]
		public void Integration_CreatePhonologicalFeature_AppearsInRebuiltNodeSystem()
		{
			m_actionHandler.EndUndoTask();
			var (node, _) = LcmCreateFeatureLauncher.CreateClosedFeature(
				Cache, FeatureSystemKind.Phonological, "nasal", "nas");

			// The phonological node-system builder now surfaces the new feature + its two values.
			var nodes = FwFeatureStructureAdapter.BuildPhonologicalNodes(Cache);
			Assert.That(nodes.Any(n => n.Id == node.Id && n.Kind == FwFeatureNodeKind.Closed), Is.True,
				"the just-created phonological feature appears in the rebuilt node system");
			Assert.That(nodes.Count(n => n.Kind == FwFeatureNodeKind.Value), Is.EqualTo(2),
				"its +/- values appear under it");
		}
	}
}
