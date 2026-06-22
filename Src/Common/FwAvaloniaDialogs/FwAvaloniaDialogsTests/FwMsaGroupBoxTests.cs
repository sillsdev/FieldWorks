// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using FwAvaloniaDialogs;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the per-stage PNG harness (linked in via the csproj)
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The LCModel-free <see cref="FwMsaGroupBox"/> composite (the grammatical-info editor), the parity of the
	/// WinForms <c>MSAGroupBox</c>. Proven on a realized headless surface: for EACH <see cref="FwMsaType"/> the
	/// right widgets are visible and the others hidden; switching the type reconfigures live; the emitted
	/// <see cref="FwSandboxMsa"/> payload reflects the picks per type; and the create-POS request forwards.
	/// Per-config PNGs are captured for subjective visual review.
	/// </summary>
	[TestFixture]
	public class FwMsaGroupBoxTests
	{
		// A small POS hierarchy in document order with Depth: Noun > {Proper noun}, Verb (flat).
		private static IReadOnlyList<FwPosNode> PosNodes() => new List<FwPosNode>
		{
			new FwPosNode("g-noun", "Noun", 0),
			new FwPosNode("g-noun-proper", "Proper noun", 1),
			new FwPosNode("g-verb", "Verb", 0)
		};

		private static IReadOnlyList<FwInflectionSlot> Slots() => new List<FwInflectionSlot>
		{
			new FwInflectionSlot("s-tense", "Tense"),
			new FwInflectionSlot("s-number", "Number")
		};

		// The Noun POS's inflection classes (a top-level class with a nested subclass + a sibling), depth-tagged.
		private static IReadOnlyList<FwInflectionClass> NounInflClasses() => new List<FwInflectionClass>
		{
			new FwInflectionClass("c-strong", "Strong", 0),
			new FwInflectionClass("c-strong-irregular", "Irregular", 1),
			new FwInflectionClass("c-weak", "Weak", 0)
		};

		// The Verb POS's inflection classes (a different, smaller set, to prove the refresh on POS change).
		private static IReadOnlyList<FwInflectionClass> VerbInflClasses() => new List<FwInflectionClass>
		{
			new FwInflectionClass("c-transitive", "Transitive", 0)
		};

		// Build, host, and pump a group box for the given type. Returns the box (and its hosting window for capture).
		private static (FwMsaGroupBox box, Window window) Show(FwMsaType type)
		{
			var box = new FwMsaGroupBox();
			box.SetPosNodes(PosNodes());
			box.SetSlots(Slots());
			box.SetInflectionClasses(NounInflClasses());
			box.MsaType = type;

			var window = new Window { Content = box, Width = 560, Height = 140 };
			window.Show();
			Pump(window);
			return (box, window);
		}

		private static void Pump(Control surface)
		{
			surface.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
		}

		// ----- MsaType -> visible widgets (mirrors MSAGroupBox exactly) -----

		[AvaloniaTest]
		public void Stem_ShowsMainPosOnly()
		{
			var (box, window) = Show(FwMsaType.Stem);
			DialogSnapshot.Capture(window, "FwMsaGroupBox-01-stem");

			Assert.That(box.MainCatPanel.IsVisible, Is.True, "stem shows the Main POS");
			Assert.That(box.AffixTypePanel.IsVisible, Is.False, "stem hides the affix-type picker");
			Assert.That(box.SlotsPanel.IsVisible, Is.False, "stem hides the slots panel");

			DialogLayoutAssert.AssertNoCrowding(box);
		}

		[AvaloniaTest]
		public void Root_ShowsMainPosOnly_LikeStem()
		{
			var (box, _) = Show(FwMsaType.Root);

			Assert.That(box.MainCatPanel.IsVisible, Is.True);
			Assert.That(box.AffixTypePanel.IsVisible, Is.False, "root is laid out exactly like stem (Main POS only)");
			Assert.That(box.SlotsPanel.IsVisible, Is.False);
		}

		[AvaloniaTest]
		public void Unclassified_ShowsAffixTypeAndMainPos_AffixTypeIsNotSure()
		{
			var (box, window) = Show(FwMsaType.Unclassified);
			DialogSnapshot.Capture(window, "FwMsaGroupBox-04-unclassified");

			Assert.That(box.AffixTypePanel.IsVisible, Is.True, "unclassified shows the affix-type picker");
			Assert.That(box.MainCatPanel.IsVisible, Is.True, "and the Main POS");
			Assert.That(box.SlotsPanel.IsVisible, Is.False, "but not the slots panel");
			Assert.That(box.AffixTypeCombo.SelectedIndex, Is.EqualTo(0), "unclassified pins the affix type to <Not sure>");

			DialogLayoutAssert.AssertNoCrowding(box);
		}

		[AvaloniaTest]
		public void Inflectional_ShowsAffixTypeMainPosAndSlot_NotSecondaryPos()
		{
			var (box, window) = Show(FwMsaType.Inflectional);
			DialogSnapshot.Capture(window, "FwMsaGroupBox-02-inflectional");

			Assert.That(box.AffixTypePanel.IsVisible, Is.True);
			Assert.That(box.MainCatPanel.IsVisible, Is.True);
			Assert.That(box.SlotsPanel.IsVisible, Is.True, "inflectional shows the slots panel");
			Assert.That(box.SlotCombo.IsVisible, Is.True, "with the Slot combo visible");
			Assert.That(box.SecondaryPosChooser.IsVisible, Is.False, "and the Secondary POS hidden");
			Assert.That(box.AffixTypeCombo.SelectedIndex, Is.EqualTo(1), "inflectional pins the affix type to Inflectional");

			DialogLayoutAssert.AssertNoCrowding(box);
		}

		[AvaloniaTest]
		public void Derivational_ShowsAffixTypeMainPosAndSecondaryPos_NotSlot()
		{
			var (box, window) = Show(FwMsaType.Derivational);
			DialogSnapshot.Capture(window, "FwMsaGroupBox-03-derivational");

			Assert.That(box.AffixTypePanel.IsVisible, Is.True);
			Assert.That(box.MainCatPanel.IsVisible, Is.True);
			Assert.That(box.SlotsPanel.IsVisible, Is.True, "derivational shows the slots panel");
			Assert.That(box.SecondaryPosChooser.IsVisible, Is.True, "with the Secondary POS visible");
			Assert.That(box.SlotCombo.IsVisible, Is.False, "and the Slot combo hidden");
			Assert.That(box.AffixTypeCombo.SelectedIndex, Is.EqualTo(2), "derivational pins the affix type to Derivational");

			DialogLayoutAssert.AssertNoCrowding(box);
		}

		// ----- switching the type reconfigures live -----

		[AvaloniaTest]
		public void SwitchingMsaType_ReconfiguresWidgetsLive()
		{
			var (box, window) = Show(FwMsaType.Stem);
			Assert.That(box.AffixTypePanel.IsVisible, Is.False);
			Assert.That(box.SlotsPanel.IsVisible, Is.False);

			// Stem -> Inflectional: affix-type + slots appear (slot combo, not secondary POS).
			box.MsaType = FwMsaType.Inflectional;
			Pump(window);
			Assert.That(box.AffixTypePanel.IsVisible, Is.True);
			Assert.That(box.SlotsPanel.IsVisible, Is.True);
			Assert.That(box.SlotCombo.IsVisible, Is.True);
			Assert.That(box.SecondaryPosChooser.IsVisible, Is.False);

			// Inflectional -> Derivational: the slots panel swaps the slot combo for the secondary POS.
			box.MsaType = FwMsaType.Derivational;
			Pump(window);
			Assert.That(box.SlotsPanel.IsVisible, Is.True);
			Assert.That(box.SecondaryPosChooser.IsVisible, Is.True);
			Assert.That(box.SlotCombo.IsVisible, Is.False);

			// Derivational -> Stem: everything but the Main POS collapses again.
			box.MsaType = FwMsaType.Stem;
			Pump(window);
			Assert.That(box.AffixTypePanel.IsVisible, Is.False);
			Assert.That(box.SlotsPanel.IsVisible, Is.False);
			Assert.That(box.MainCatPanel.IsVisible, Is.True);
		}

		[AvaloniaTest]
		public void ChangingAffixTypeCombo_ReDerivesMsaType()
		{
			var (box, window) = Show(FwMsaType.Unclassified);
			Assert.That(box.MsaType, Is.EqualTo(FwMsaType.Unclassified));

			// Picking "Inflectional" in the affix-type combo re-derives the type (like HandleComboMSATypesChange).
			box.AffixTypeCombo.SelectedIndex = 1;
			Pump(window);
			Assert.That(box.MsaType, Is.EqualTo(FwMsaType.Inflectional));
			Assert.That(box.SlotCombo.IsVisible, Is.True);

			box.AffixTypeCombo.SelectedIndex = 2;
			Pump(window);
			Assert.That(box.MsaType, Is.EqualTo(FwMsaType.Derivational));
			Assert.That(box.SecondaryPosChooser.IsVisible, Is.True);
		}

		// ----- emitted FwSandboxMsa payload reflects the picks per type -----

		[AvaloniaTest]
		public void Payload_Stem_CarriesMainPosOnly()
		{
			var (box, _) = Show(FwMsaType.Stem);
			box.MainPosId = "g-noun";

			var msa = box.SandboxMsa;
			Assert.That(msa.MsaType, Is.EqualTo(FwMsaType.Stem));
			Assert.That(msa.MainPosId, Is.EqualTo("g-noun"));
			Assert.That(msa.SecondaryPosId, Is.Null);
			Assert.That(msa.SlotId, Is.Null);
		}

		[AvaloniaTest]
		public void Payload_Inflectional_CarriesMainPosAndSlot()
		{
			var (box, _) = Show(FwMsaType.Inflectional);
			box.MainPosId = "g-verb";
			box.SlotId = "s-tense";

			var msa = box.SandboxMsa;
			Assert.That(msa.MsaType, Is.EqualTo(FwMsaType.Inflectional));
			Assert.That(msa.MainPosId, Is.EqualTo("g-verb"));
			Assert.That(msa.SlotId, Is.EqualTo("s-tense"), "inflectional carries the slot");
			Assert.That(msa.SecondaryPosId, Is.Null, "inflectional does not carry a secondary POS");
		}

		[AvaloniaTest]
		public void Payload_Derivational_CarriesMainAndSecondaryPos()
		{
			var (box, _) = Show(FwMsaType.Derivational);
			box.MainPosId = "g-verb";
			box.SecondaryPosId = "g-noun";

			var msa = box.SandboxMsa;
			Assert.That(msa.MsaType, Is.EqualTo(FwMsaType.Derivational));
			Assert.That(msa.MainPosId, Is.EqualTo("g-verb"));
			Assert.That(msa.SecondaryPosId, Is.EqualTo("g-noun"), "derivational carries the secondary POS");
			Assert.That(msa.SlotId, Is.Null, "derivational does not carry a slot");
		}

		[AvaloniaTest]
		public void Payload_Unclassified_CarriesMainPosOnly()
		{
			var (box, _) = Show(FwMsaType.Unclassified);
			box.MainPosId = "g-noun";

			var msa = box.SandboxMsa;
			Assert.That(msa.MsaType, Is.EqualTo(FwMsaType.Unclassified));
			Assert.That(msa.MainPosId, Is.EqualTo("g-noun"));
			Assert.That(msa.SecondaryPosId, Is.Null);
			Assert.That(msa.SlotId, Is.Null);
		}

		// ----- inflection-class picker (Stage 6): shown for stem/root, hidden for affixes -----

		[AvaloniaTest]
		public void InflectionClass_ShownForStem_WithMainPos()
		{
			var (box, window) = Show(FwMsaType.Stem);
			box.MainPosId = "g-noun";
			Pump(window);

			// Capture BEFORE the crowding assert so the dense, aligned new field is reviewable.
			DialogSnapshot.Capture(window, "FwMsaGroupBox-05-stem-with-inflclass");

			Assert.That(box.InflectionClassPanel.IsVisible, Is.True, "stem shows the inflection-class picker");
			// "<None>" + the three Noun classes (the sentinel row keeps the empty pick selectable).
			Assert.That(box.InflectionClassCombo.ItemCount, Is.EqualTo(4),
				"the picker is populated from the selected POS's classes plus the <None> row");

			DialogLayoutAssert.AssertNoCrowding(box);
		}

		[AvaloniaTest]
		public void InflectionClass_ShownForRoot()
		{
			var (box, _) = Show(FwMsaType.Root);
			Assert.That(box.InflectionClassPanel.IsVisible, Is.True, "root shows the inflection-class picker (like stem)");
		}

		[AvaloniaTest]
		public void InflectionClass_HiddenForAffixTypes()
		{
			foreach (var type in new[] { FwMsaType.Unclassified, FwMsaType.Inflectional, FwMsaType.Derivational })
			{
				var (box, _) = Show(type);
				Assert.That(box.InflectionClassPanel.IsVisible, Is.False,
					$"the inflection-class picker is hidden for {type}");
			}
		}

		[AvaloniaTest]
		public void InflectionClass_HidesWhenSwitchingStemToAffix()
		{
			var (box, window) = Show(FwMsaType.Stem);
			Assert.That(box.InflectionClassPanel.IsVisible, Is.True);

			box.MsaType = FwMsaType.Inflectional;
			Pump(window);
			Assert.That(box.InflectionClassPanel.IsVisible, Is.False, "switching to an affix hides the inflection class");

			box.MsaType = FwMsaType.Stem;
			Pump(window);
			Assert.That(box.InflectionClassPanel.IsVisible, Is.True, "switching back to stem shows it again");
		}

		[AvaloniaTest]
		public void InflectionClass_RefreshesWhenMainPosChanges()
		{
			var (box, window) = Show(FwMsaType.Stem);
			box.MainPosId = "g-noun";
			box.SetInflectionClasses(NounInflClasses());
			Pump(window);
			Assert.That(box.InflectionClassCombo.ItemCount, Is.EqualTo(4), "Noun: <None> + 3 classes");

			// Subscribe to MainPosChanged and re-feed (the host's POS-change wiring) — Verb has a smaller class set.
			box.MainPosChanged += posId =>
			{
				box.SetInflectionClasses(posId == "g-verb" ? VerbInflClasses() : NounInflClasses());
			};

			// A real user pick of Verb fires MainPosChanged, which re-feeds the Verb classes.
			var verbNode = NodeFor(box.MainPosChooser, "g-verb");
			box.MainPosChooser.Tree.SelectedItem = verbNode;
			Dispatcher.UIThread.RunJobs();
			Pump(window);

			Assert.That(box.InflectionClassCombo.ItemCount, Is.EqualTo(2),
				"changing the main POS re-populates the inflection classes (Verb: <None> + 1)");
		}

		[AvaloniaTest]
		public void Payload_Stem_CarriesInflectionClass()
		{
			var (box, window) = Show(FwMsaType.Stem);
			box.MainPosId = "g-noun";
			box.InflectionClassId = "c-weak";
			Pump(window);

			var msa = box.SandboxMsa;
			Assert.That(msa.MsaType, Is.EqualTo(FwMsaType.Stem));
			Assert.That(msa.InflectionClassId, Is.EqualTo("c-weak"), "the chosen inflection class flows into the payload");
		}

		[AvaloniaTest]
		public void Payload_Stem_NoneInflectionClass_IsNull()
		{
			var (box, _) = Show(FwMsaType.Stem);
			box.MainPosId = "g-noun";
			// Leave the "<None>" sentinel selected (the default after SetInflectionClasses).

			Assert.That(box.InflectionClassId, Is.Null, "<None> reads back as null");
			Assert.That(box.SandboxMsa.InflectionClassId, Is.Null, "and the payload carries no inflection class");
		}

		[AvaloniaTest]
		public void Payload_Inflectional_DoesNotCarryInflectionClass()
		{
			var (box, _) = Show(FwMsaType.Inflectional);
			box.MainPosId = "g-verb";

			Assert.That(box.SandboxMsa.InflectionClassId, Is.Null,
				"an inflectional affix MSA carries no inflection class (Stage 6 scopes it to stem/root)");
		}

		[AvaloniaTest]
		public void MsaChanged_FiresOnInflectionClassPick()
		{
			var (box, window) = Show(FwMsaType.Stem);
			box.MainPosId = "g-noun";
			Pump(window);

			FwSandboxMsa last = null;
			box.MsaChanged += m => last = m;

			// Select "Strong" (index 1, after the <None> row at 0).
			box.InflectionClassCombo.SelectedIndex = 1;
			Pump(window);

			Assert.That(last, Is.Not.Null, "an inflection-class pick raises MsaChanged");
			Assert.That(last.InflectionClassId, Is.EqualTo("c-strong"));
		}

		// ----- change event fires on a user pick -----

		[AvaloniaTest]
		public void MsaChanged_FiresOnMainPosPick()
		{
			var (box, _) = Show(FwMsaType.Stem);
			FwSandboxMsa last = null;

			// Seeding the POS via the setter does NOT raise (it is the host's seed path), then we subscribe.
			box.MainPosId = "g-noun";
			box.MsaChanged += m => last = m;
			Assert.That(last, Is.Null, "seeding the POS does not raise MsaChanged");

			// A real user pick goes through the chooser's commit path (clicking a tree row -> SelectionChanged).
			// Resolve the target node via the chooser's own selection round-trip, then drive the tree pick.
			var verbNode = NodeFor(box.MainPosChooser, "g-verb");
			box.MainPosChooser.Tree.SelectedItem = verbNode;
			Dispatcher.UIThread.RunJobs();

			Assert.That(last, Is.Not.Null, "a user POS pick raises MsaChanged");
			Assert.That(last.MainPosId, Is.EqualTo("g-verb"));
		}

		[AvaloniaTest]
		public void MsaChanged_FiresOnSlotPick()
		{
			var (box, window) = Show(FwMsaType.Inflectional);
			FwSandboxMsa last = null;
			box.MsaChanged += m => last = m;

			box.SlotCombo.SelectedIndex = 0; // Tense
			Pump(window);

			Assert.That(last, Is.Not.Null, "a slot pick raises MsaChanged");
			Assert.That(last.SlotId, Is.EqualTo("s-tense"));
		}

		// ----- CreateNewPos forwards from either chooser -----

		[AvaloniaTest]
		public void CreateNewPos_ForwardsFromMainChooser()
		{
			var (box, _) = Show(FwMsaType.Stem);
			var fired = 0;
			box.CreateNewPosRequested += () => fired++;

			box.MainPosChooser.RaiseCreateNew();
			Assert.That(fired, Is.EqualTo(1), "the main chooser's create request forwards through the group box");
		}

		[AvaloniaTest]
		public void CreateNewPos_ForwardsFromSecondaryChooser()
		{
			var (box, _) = Show(FwMsaType.Derivational);
			var fired = 0;
			box.CreateNewPosRequested += () => fired++;

			box.SecondaryPosChooser.RaiseCreateNew();
			Assert.That(fired, Is.EqualTo(1), "the secondary chooser's create request forwards through the group box");
		}

		[AvaloniaTest]
		public void AcceptCreatedMainPos_AddsAndSelectsTheNewNode()
		{
			var (box, _) = Show(FwMsaType.Stem);
			box.AcceptCreatedMainPos(new FwPosNode("g-adj", "Adjective", 0));

			Assert.That(box.MainPosId, Is.EqualTo("g-adj"), "the created node becomes the main POS selection");
		}

		// Resolve the chooser's internal tree-node model for an id WITHOUT reflecting its private type: the
		// chooser's SelectedPosId setter applies the selection to its TreeView (Tree.SelectedItem), so seeding
		// the id and reading back Tree.SelectedItem yields the node. Reset the seed (-> null) so a later pick of
		// that node is observable as a change rather than a no-op re-selection.
		private static object NodeFor(FwPosChooser chooser, string id)
		{
			chooser.SelectedPosId = id;
			var node = chooser.Tree.SelectedItem;
			chooser.SelectedPosId = null;
			Dispatcher.UIThread.RunJobs();
			return node;
		}

		// ===== inflection-feature editor wiring (Phase-1 §19b Stage 2) =====

		// A small feature system in document order: a closed feature "Number" {sg, pl} and a complex feature
		// "Agreement" nesting a closed "Person" {1, 2}. Depth-tagged like the editor's seam.
		private static IReadOnlyList<FwFeatureNode> Features() => new List<FwFeatureNode>
		{
			new FwFeatureNode("f-number", "Number", FwFeatureNodeKind.Closed, 0),
			new FwFeatureNode("v-sg", "Singular", FwFeatureNodeKind.Value, 1),
			new FwFeatureNode("v-pl", "Plural", FwFeatureNodeKind.Value, 1),
			new FwFeatureNode("f-agr", "Agreement", FwFeatureNodeKind.Complex, 0),
			new FwFeatureNode("f-person", "Person", FwFeatureNodeKind.Closed, 1),
			new FwFeatureNode("v-1", "First", FwFeatureNodeKind.Value, 2),
			new FwFeatureNode("v-2", "Second", FwFeatureNodeKind.Value, 2)
		};

		// Build, host, and pump a group box of the given type with the POS + slot + feature feeds.
		private static (FwMsaGroupBox box, Window window) ShowWithFeatures(FwMsaType type)
		{
			var box = new FwMsaGroupBox();
			box.SetPosNodes(PosNodes());
			box.SetSlots(Slots());
			box.SetInflectionClasses(NounInflClasses());
			box.SetInflectionFeatureNodes(Features());
			box.MsaType = type;

			var window = new Window { Content = box, Width = 640, Height = 220 };
			window.Show();
			Pump(window);
			return (box, window);
		}

		[AvaloniaTest]
		public void InflectionFeatures_ShownForInflectional()
		{
			var (box, window) = ShowWithFeatures(FwMsaType.Inflectional);
			DialogSnapshot.Capture(window, "FwMsaGroupBox-06-infl-features-empty");

			Assert.That(box.InflectionFeaturesPanel.IsVisible, Is.True,
				"an inflectional affix shows the inflection-feature editor (the legacy MsaInflectionFeatureListDlg affordance)");
			DialogLayoutAssert.AssertNoCrowding(box);
		}

		[AvaloniaTest]
		public void InflectionFeatures_ShownForDerivational()
		{
			var (box, _) = ShowWithFeatures(FwMsaType.Derivational);
			Assert.That(box.InflectionFeaturesPanel.IsVisible, Is.True,
				"a derivational affix shows the inflection-feature editor (the FROM features)");
		}

		[AvaloniaTest]
		public void InflectionFeatures_HiddenForStemRootUnclassified()
		{
			foreach (var type in new[] { FwMsaType.Stem, FwMsaType.Root, FwMsaType.Unclassified })
			{
				var (box, _) = ShowWithFeatures(type);
				Assert.That(box.InflectionFeaturesPanel.IsVisible, Is.False,
					$"the inflection-feature editor is hidden for {type}");
			}
		}

		[AvaloniaTest]
		public void InflectionFeatures_HidesWhenSwitchingToStem()
		{
			var (box, window) = ShowWithFeatures(FwMsaType.Inflectional);
			Assert.That(box.InflectionFeaturesPanel.IsVisible, Is.True);

			box.MsaType = FwMsaType.Stem;
			Pump(window);
			Assert.That(box.InflectionFeaturesPanel.IsVisible, Is.False, "switching to stem hides the feature editor");

			box.MsaType = FwMsaType.Inflectional;
			Pump(window);
			Assert.That(box.InflectionFeaturesPanel.IsVisible, Is.True, "switching back to inflectional shows it again");
		}

		[AvaloniaTest]
		public void Payload_Inflectional_CarriesInflectionFeatures()
		{
			var (box, window) = ShowWithFeatures(FwMsaType.Inflectional);
			box.MainPosId = "g-verb";

			// Pick "Plural" under the "Number" closed feature (the editor's deterministic pick path).
			box.InflectionFeaturesEditor.SelectValue("v-pl");
			Pump(window);

			DialogSnapshot.Capture(window, "FwMsaGroupBox-07-infl-feature-assigned");

			var msa = box.SandboxMsa;
			Assert.That(msa.MsaType, Is.EqualTo(FwMsaType.Inflectional));
			Assert.That(msa.InflectionFeatures.Count, Is.EqualTo(1), "the chosen feature value flows into the payload");
			Assert.That(msa.InflectionFeatures[0].ClosedFeatureId, Is.EqualTo("f-number"));
			Assert.That(msa.InflectionFeatures[0].ValueId, Is.EqualTo("v-pl"));

			DialogLayoutAssert.AssertNoCrowding(box);
		}

		[AvaloniaTest]
		public void Payload_Inflectional_NoFeatures_Empty()
		{
			var (box, _) = ShowWithFeatures(FwMsaType.Inflectional);
			Assert.That(box.SandboxMsa.InflectionFeatures, Is.Empty,
				"no feature chosen ⇒ an empty assignment set (the legacy unspecified/delete-FS case)");
		}

		[AvaloniaTest]
		public void Payload_Stem_NoInflectionFeatures()
		{
			var (box, window) = ShowWithFeatures(FwMsaType.Inflectional);
			box.InflectionFeaturesEditor.SelectValue("v-pl");
			Pump(window);
			Assert.That(box.SandboxMsa.InflectionFeatures, Is.Not.Empty);

			// Switching to stem drops the features from the payload (Stage 6/§19b scope it to infl/deriv).
			box.MsaType = FwMsaType.Stem;
			Pump(window);
			Assert.That(box.SandboxMsa.InflectionFeatures, Is.Empty,
				"a stem MSA carries no inflection features");
		}

		[AvaloniaTest]
		public void MsaChanged_FiresOnInflectionFeaturePick()
		{
			var (box, window) = ShowWithFeatures(FwMsaType.Inflectional);
			FwSandboxMsa last = null;
			box.MsaChanged += m => last = m;

			box.InflectionFeaturesEditor.SelectValue("v-sg");
			Pump(window);

			Assert.That(last, Is.Not.Null, "an inflection-feature pick raises MsaChanged");
			Assert.That(last.InflectionFeatures.Count, Is.EqualTo(1));
			Assert.That(last.InflectionFeatures[0].ValueId, Is.EqualTo("v-sg"));
		}

		[AvaloniaTest]
		public void InflectionFeatures_SeedAssignments_Silently()
		{
			var (box, window) = ShowWithFeatures(FwMsaType.Inflectional);
			var fired = 0;
			box.MsaChanged += _ => fired++;

			// Seeding (the host's edit-path load) must NOT raise MsaChanged.
			box.SetInflectionFeatureAssignments(new[] { new FwFeatureValueAssignment("f-number", "v-pl") });
			Pump(window);

			Assert.That(fired, Is.EqualTo(0), "seeding assignments is silent");
			Assert.That(box.InflectionFeatures.Count, Is.EqualTo(1), "but the seeded value reads back");
			Assert.That(box.InflectionFeatures[0].ValueId, Is.EqualTo("v-pl"));
		}

		[AvaloniaTest]
		public void CreateFeature_ForwardsThroughBox()
		{
			var (box, _) = ShowWithFeatures(FwMsaType.Inflectional);
			var fired = 0;
			box.CreateNewFeatureRequested += () => fired++;

			box.InflectionFeaturesEditor.RaiseCreateNewFeature();
			Assert.That(fired, Is.EqualTo(1), "the editor's create-feature request forwards through the box");
		}

		[AvaloniaTest]
		public void CreateValue_ForwardsThroughBox_WithFeatureId()
		{
			var (box, _) = ShowWithFeatures(FwMsaType.Inflectional);
			string requested = null;
			box.CreateNewValueRequested += id => requested = id;

			box.InflectionFeaturesEditor.RaiseCreateNewValue("f-number");
			Assert.That(requested, Is.EqualTo("f-number"), "the editor's add-value request forwards the feature id");
		}

		[AvaloniaTest]
		public void InflectionFeatures_ComplexScriptNames_NoCrowding()
		{
			var box = new FwMsaGroupBox();
			box.SetPosNodes(PosNodes());
			box.SetSlots(Slots());
			// RTL / complex-script feature & value names (display safety).
			box.SetInflectionFeatureNodes(new List<FwFeatureNode>
			{
				new FwFeatureNode("f-rtl", "תכונה", FwFeatureNodeKind.Closed, 0),
				new FwFeatureNode("v-rtl1", "ערך־ראשון", FwFeatureNodeKind.Value, 1),
				new FwFeatureNode("v-rtl2", "ערך־שני", FwFeatureNodeKind.Value, 1)
			});
			box.MsaType = FwMsaType.Inflectional;
			var window = new Window { Content = box, Width = 640, Height = 220 };
			window.Show();
			Pump(window);

			Assert.That(box.InflectionFeaturesPanel.IsVisible, Is.True);
			DialogLayoutAssert.AssertNoCrowding(box);
		}
	}
}
