// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FwAvaloniaDialogs;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the per-stage PNG harness (linked in via the csproj)
using NUnit.Framework;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The standalone feature-structure chooser dialog (Phase-1 §19b Stage 3): the Avalonia replacement for the
	/// WinForms MsaInflectionFeatureListDlg / PhonologicalFeatureChooserDlg. The dialog hosts the shared LCModel-free
	/// FwFeatureStructureEditor over OK/Cancel/Help; it seeds the feature system + current assignments, has NO OK gate
	/// (an empty pick is the valid "unspecified / delete the FS" outcome), snapshots the chosen assignment set on OK,
	/// and forwards the editor's inline create-feature / add-value affordances. Runtime proof on a realized headless
	/// surface, with per-stage PNGs for subjective visual review. Traceability: see
	/// openspec/changes/lexical-edit-avalonia-migration/feature-structure-test-research.md (Stage 3).
	/// </summary>
	[TestFixture]
	public class FeatureChooserDialogTests
	{
		// A small feature system: Tense {Past, Present} (closed, top-level) + a complex Agreement > Gender {Masc,Fem}.
		private static IReadOnlyList<FwFeatureNode> FeatureSystem() => new List<FwFeatureNode>
		{
			new FwFeatureNode("f-agr", "Agreement", FwFeatureNodeKind.Complex, 0),
			new FwFeatureNode("f-gender", "Gender", FwFeatureNodeKind.Closed, 1),
			new FwFeatureNode("v-masc", "Masculine", FwFeatureNodeKind.Value, 2),
			new FwFeatureNode("v-fem", "Feminine", FwFeatureNodeKind.Value, 2),
			new FwFeatureNode("f-tense", "Tense", FwFeatureNodeKind.Closed, 0),
			new FwFeatureNode("v-past", "Past", FwFeatureNodeKind.Value, 1),
			new FwFeatureNode("v-pres", "Present", FwFeatureNodeKind.Value, 1)
		};

		private static FeatureChooserDialogInput Input(
			IReadOnlyList<FwFeatureNode> nodes = null,
			IReadOnlyList<FwFeatureValueAssignment> initial = null,
			string helpTopic = null) =>
			new FeatureChooserDialogInput
			{
				Title = "Inflection Feature Information",
				Prompt = "Choose the inflection feature values for this item.",
				AutomationId = "InflFeatures",
				Nodes = nodes ?? FeatureSystem(),
				InitialAssignments = initial,
				HelpTopic = helpTopic
			};

		private static (FeatureChooserDialogView view, FeatureChooserDialogViewModel vm) Show(
			FeatureChooserDialogInput input, string stageName = "FeatureChooser-01-initial")
		{
			var vm = new FeatureChooserDialogViewModel(input);
			var view = new FeatureChooserDialogView { DataContext = vm };
			var window = new Window { Content = view, Width = 360, Height = 460 };
			window.Show();
			Pump(view);
			DialogSnapshot.Capture(window, stageName);
			DialogLayoutAssert.AssertNoCrowding(view);
			return (view, vm);
		}

		private static void Pump(Control surface)
		{
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			surface.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
		}

		// ----- the editor renders, mounted, no OK gate -----

		[AvaloniaTest]
		public void Renders_TheEditorMounted_AndOkAlwaysEnabled()
		{
			var (view, vm) = Show(Input());
			Assert.That(view.GetVisualDescendants().Contains(vm.Editor), Is.True,
				"the feature editor is mounted inside the dialog");
			Assert.That(vm.OkCommand.CanExecute(null), Is.True,
				"the dialog has no OK gate (empty == unspecified, like the legacy dialogs)");
		}

		// ----- seeding from an existing FS (the legacy edit path) -----

		[AvaloniaTest]
		public void SeedsExistingAssignments_AndSnapshotsThemOnOk()
		{
			var (view, vm) = Show(Input(initial:
				new[] { new FwFeatureValueAssignment("f-tense", "v-pres") }), "FeatureChooser-02-seeded");

			Assert.That(vm.Editor.Assignments.Single().ValueId, Is.EqualTo("v-pres"),
				"the existing assignment seeds the editor silently");

			vm.OkCommand.Execute(null);
			Assert.That(vm.Result, Is.Not.Null);
			Assert.That(vm.Result.Assignments.Single().ClosedFeatureId, Is.EqualTo("f-tense"));
			Assert.That(vm.Result.Assignments.Single().ValueId, Is.EqualTo("v-pres"));
		}

		// ----- picking a value flows into the OK snapshot -----

		[AvaloniaTest]
		public void PickValue_FlowsIntoResultOnOk()
		{
			var (view, vm) = Show(Input());
			vm.Editor.SelectValue("v-past");
			Pump(view);
			DialogSnapshot.Capture((Window)view.GetVisualRoot(), "FeatureChooser-03-value-picked");

			vm.OkCommand.Execute(null);
			Assert.That(vm.Result.Assignments.Single().ValueId, Is.EqualTo("v-past"),
				"the picked value rides the OK snapshot");
		}

		// ----- empty pick is valid (the unspecified / delete-the-FS case) -----

		[AvaloniaTest]
		public void NoPick_OkSnapshotsEmptySet()
		{
			var (_, vm) = Show(Input());
			vm.OkCommand.Execute(null);
			Assert.That(vm.Result.Assignments, Is.Empty,
				"OK with no pick yields the empty set (the legacy LT-13596 delete-the-FS case)");
		}

		// ----- create-feature / add-value forwarding -----

		[AvaloniaTest]
		public void CreateFeatureRequest_ForwardsThroughTheDialog()
		{
			var (_, vm) = Show(Input());
			var fired = 0;
			vm.CreateNewFeatureRequested += () => fired++;

			vm.Editor.RaiseCreateNewFeature();
			Assert.That(fired, Is.EqualTo(1), "the editor's create-feature affordance forwards through the VM");
		}

		[AvaloniaTest]
		public void CreateValueRequest_ForwardsWithFeatureId()
		{
			var (_, vm) = Show(Input());
			string requestedFor = null;
			vm.CreateNewValueRequested += id => requestedFor = id;

			vm.Editor.RaiseCreateNewValue("f-tense");
			Assert.That(requestedFor, Is.EqualTo("f-tense"), "the add-value request carries the closed-feature id");
		}

		[AvaloniaTest]
		public void AcceptCreatedFeature_AddsItToTheEditor()
		{
			var (_, vm) = Show(Input());
			vm.AcceptCreatedFeature(new FwFeatureNode("f-case", "Case", FwFeatureNodeKind.Closed, 0),
				new[] { new FwFeatureNode("v-nom", "Nominative", FwFeatureNodeKind.Value, 1) });
			Dispatcher.UIThread.RunJobs();

			vm.Editor.SelectValue("v-nom");
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.Editor.Assignments.Single(a => a.ClosedFeatureId == "f-case").ValueId, Is.EqualTo("v-nom"),
				"the created feature is added and its value is selectable");
		}

		[AvaloniaTest]
		public void AcceptCreatedValue_AddsAndSelects()
		{
			var (_, vm) = Show(Input());
			vm.AcceptCreatedValue("f-tense", new FwFeatureNode("v-future", "Future", FwFeatureNodeKind.Value, 1));
			Dispatcher.UIThread.RunJobs();

			Assert.That(vm.Editor.Assignments.Single(a => a.ClosedFeatureId == "f-tense").ValueId,
				Is.EqualTo("v-future"), "the created value becomes the feature's pick");
		}

		// ----- T3 edge: empty feature system + complex-script names -----

		[AvaloniaTest]
		public void EmptyFeatureSystem_RendersWithoutCrowding()
		{
			var (view, vm) = Show(Input(nodes: new List<FwFeatureNode>()), "FeatureChooser-04-empty");
			Assert.That(vm.Editor.Assignments, Is.Empty);
			Assert.That(vm.OkCommand.CanExecute(null), Is.True);
		}

		[AvaloniaTest]
		public void ComplexScriptNames_RenderWithoutCrowding()
		{
			var rtl = new List<FwFeatureNode>
			{
				new FwFeatureNode("f-rtl", "جنس", FwFeatureNodeKind.Closed, 0),
				new FwFeatureNode("v-rtl1", "مذكر", FwFeatureNodeKind.Value, 1),
				new FwFeatureNode("v-rtl2", "مؤنث", FwFeatureNodeKind.Value, 1)
			};
			var (view, vm) = Show(Input(nodes: rtl), "FeatureChooser-05-rtl");
			vm.Editor.SelectValue("v-rtl1");
			Pump(view);
			Assert.That(vm.Editor.Assignments.Single().ValueId, Is.EqualTo("v-rtl1"));
		}

		// ----- Help + close contract -----

		[AvaloniaTest]
		public void Help_FiresWhenTopicPresent()
		{
			var (_, vm) = Show(Input(helpTopic: "khtpInflectionFeatureChooser"));
			Assert.That(vm.HasHelp, Is.True);
			string topic = null;
			vm.HelpRequested += t => topic = t;
			vm.HelpCommand.Execute(null);
			Assert.That(topic, Is.EqualTo("khtpInflectionFeatureChooser"));
		}

		[AvaloniaTest] // the VM ctor builds owned Avalonia controls — must run on the UI thread
		public void CancelCommand_ClosesWithoutAccepting()
		{
			var vm = new FeatureChooserDialogViewModel(Input());
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.CancelCommand.Execute(null);

			Assert.That(vm.Accepted, Is.False);
			Assert.That(closed, Is.False);
			Assert.That(vm.Result, Is.Null, "Cancel never snapshots a result");
		}

		[Test]
		public void Strings_ResolveFromSharedAccessor()
		{
			Assert.That(FwAvaloniaDialogsStrings.InflectionFeatureChooserTitle, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.PhonologicalFeatureChooserTitle, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.CreateFeatureTitle, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.CreateValueTitle, Is.Not.Null.And.Not.Empty);
		}
	}
}
