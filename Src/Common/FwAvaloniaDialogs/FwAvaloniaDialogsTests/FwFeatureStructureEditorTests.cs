// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FwAvaloniaDialogs;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the per-stage PNG harness (linked in via the csproj)
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The LCModel-free <see cref="FwFeatureStructureEditor"/> (the FsFeatStruc tree editor), parity of the
	/// WinForms <c>FeatureStructureTreeView</c> / <c>MsaInflectionFeatureListDlg</c> /
	/// <c>PhonologicalFeatureChooserDlg</c>. Proven on a realized headless surface per the §19.0 rubric (T1
	/// unit, T2 integration, T3 edges, T4 workflow, T5 visual). Traceability: see
	/// openspec/changes/lexical-edit-avalonia-migration/feature-structure-test-research.md.
	/// </summary>
	[TestFixture]
	public class FwFeatureStructureEditorTests
	{
		// A feature system in document order with Depth + Kind:
		//   Agreement (complex)
		//     Gender (closed)
		//       Masculine, Feminine (values)
		//     Number (closed)
		//       Singular, Plural (values)
		//   Tense (closed, top-level)
		//     Past, Present (values)
		private static IReadOnlyList<FwFeatureNode> FeatureSystem() => new List<FwFeatureNode>
		{
			new FwFeatureNode("f-agr", "Agreement", FwFeatureNodeKind.Complex, 0),
			new FwFeatureNode("f-gender", "Gender", FwFeatureNodeKind.Closed, 1),
			new FwFeatureNode("v-masc", "Masculine", FwFeatureNodeKind.Value, 2),
			new FwFeatureNode("v-fem", "Feminine", FwFeatureNodeKind.Value, 2),
			new FwFeatureNode("f-number", "Number", FwFeatureNodeKind.Closed, 1),
			new FwFeatureNode("v-sg", "Singular", FwFeatureNodeKind.Value, 2),
			new FwFeatureNode("v-pl", "Plural", FwFeatureNodeKind.Value, 2),
			new FwFeatureNode("f-tense", "Tense", FwFeatureNodeKind.Closed, 0),
			new FwFeatureNode("v-past", "Past", FwFeatureNodeKind.Value, 1),
			new FwFeatureNode("v-pres", "Present", FwFeatureNodeKind.Value, 1)
		};

		private static (FwFeatureStructureEditor editor, Window window) Show(
			IReadOnlyList<FwFeatureNode> nodes = null)
		{
			var editor = new FwFeatureStructureEditor("InflFeatures");
			editor.SetNodes(nodes ?? FeatureSystem());

			var window = new Window { Content = editor, Width = 360, Height = 420 };
			window.Show();
			Pump(window);
			return (editor, window);
		}

		private static void Pump(Control surface) => AvaloniaDialogTestHarness.Pump(surface);

		// Resolve the tree-node model object for a feature/value id without reflecting the private node type:
		// the editor exposes its TreeView, so we walk its realized containers' DataContexts by matching the
		// rendered name. (Used to drive picks/expansion through the realized tree.)
		private static object NodeForName(FwFeatureStructureEditor editor, string name)
		{
			return editor.Tree.GetLogicalDescendants()
				.OfType<TreeViewItem>()
				.Select(i => i.DataContext)
				.FirstOrDefault(dc => dc != null && DescribesName(dc, name));
		}

		// The node model is private; match on its public-facing Source.Name via the radio/label text instead.
		private static bool DescribesName(object dataContext, string name)
		{
			var prop = dataContext.GetType().GetProperty("Source");
			var src = prop?.GetValue(dataContext) as FwFeatureNode;
			return src != null && src.Name == name;
		}

		// Find the realized RadioButton for a value by its accessible name (the value display text).
		private static RadioButton ValueRadio(FwFeatureStructureEditor editor, string valueName)
		{
			return editor.Tree.GetVisualDescendants().OfType<RadioButton>()
				.FirstOrDefault(r => (r.Content as string) == valueName);
		}

		// Expand a feature's realized TreeViewItem (its IsExpanded is two-way bound to the node, so the model
		// follows and the children realize after a layout pass). Pump AFTER calling so the children realize.
		private static void Expand(FwFeatureStructureEditor editor, string featureName)
		{
			var item = editor.Tree.GetLogicalDescendants().OfType<TreeViewItem>()
				.FirstOrDefault(i => DescribesName(i.DataContext, featureName));
			if (item != null)
				item.IsExpanded = true;
		}

		// ----- T1: tree renders from the feature system -----

		[AvaloniaTest]
		public void Renders_FeatureSystem_AsTree()
		{
			var (editor, window) = Show();
			DialogSnapshot.Capture(window, "FwFeatureStructureEditor-01-initial");

			// Top level shows the complex feature and the top-level closed feature.
			Assert.That(NodeForName(editor, "Agreement"), Is.Not.Null, "the complex feature renders");
			Assert.That(NodeForName(editor, "Tense"), Is.Not.Null, "the top-level closed feature renders");

			DialogLayoutAssert.AssertNoCrowding(editor);
		}

		[AvaloniaTest]
		public void ClosedFeature_ShowsValues_PlusNone()
		{
			var (editor, window) = Show();
			Expand(editor, "Tense");
			Pump(window);

			// The closed feature's two values render as radios, plus the auto-appended "<None>".
			Assert.That(ValueRadio(editor, "Past"), Is.Not.Null);
			Assert.That(ValueRadio(editor, "Present"), Is.Not.Null);
			Assert.That(ValueRadio(editor, FwAvaloniaDialogsStrings.FeatureEditorNone), Is.Not.Null,
				"each closed feature carries a trailing <None> radio (the legacy 'None of the above')");
		}

		[AvaloniaTest]
		public void PickingValue_CommitsAndEmits()
		{
			var (editor, window) = Show();

			IReadOnlyList<FwFeatureValueAssignment> last = null;
			editor.AssignmentsChanged += a => last = a;

			editor.SelectValue("v-past");
			Pump(window);

			Assert.That(last, Is.Not.Null, "a value pick raises AssignmentsChanged");
			Assert.That(editor.Assignments.Count, Is.EqualTo(1));
			Assert.That(editor.Assignments[0].ClosedFeatureId, Is.EqualTo("f-tense"));
			Assert.That(editor.Assignments[0].ValueId, Is.EqualTo("v-past"));

			// Capture the realized surface with the value visible for the per-stage PNG.
			Expand(editor, "Tense");
			Pump(window);
			DialogSnapshot.Capture(window, "FwFeatureStructureEditor-03-value-picked");
		}

		[AvaloniaTest]
		public void PickingValue_DeselectsSiblings()
		{
			var (editor, window) = Show();

			editor.SelectValue("v-past");
			Pump(window);
			editor.SelectValue("v-pres");
			Pump(window);

			// Exactly one value per closed feature (radio mutual exclusion, like HandleCheckBoxNodes).
			Assert.That(editor.Assignments.Count, Is.EqualTo(1));
			Assert.That(editor.Assignments[0].ValueId, Is.EqualTo("v-pres"), "the later pick replaces the earlier");
		}

		[AvaloniaTest]
		public void NoneSelected_EmitsNoAssignment()
		{
			var (editor, window) = Show();

			editor.SelectValue("v-past");
			Pump(window);
			Assert.That(editor.Assignments.Count, Is.EqualTo(1));

			// Re-selecting <None> clears the feature's assignment (B5/B9).
			editor.ClearFeature("f-tense");
			Pump(window);
			Assert.That(editor.Assignments, Is.Empty, "<None> means unspecified — no assignment emitted");
		}

		[AvaloniaTest]
		public void ReselectingNone_RemovesAssignment_AndEmits()
		{
			var (editor, window) = Show();
			editor.SelectValue("v-past");
			Pump(window);

			var raised = 0;
			editor.AssignmentsChanged += a => raised++;
			editor.ClearFeature("f-tense");
			Pump(window);

			Assert.That(raised, Is.EqualTo(1), "removing a value via <None> also raises the change event");
			Assert.That(editor.Assignments, Is.Empty);
		}

		[AvaloniaTest]
		public void ComplexFeature_ExpandCollapse()
		{
			var (editor, window) = Show();
			// Before expanding Agreement, its nested Gender values are not realized.
			Assert.That(ValueRadio(editor, "Masculine"), Is.Null);

			Expand(editor, "Agreement");
			Pump(window);
			Expand(editor, "Gender");
			Pump(window);

			Assert.That(ValueRadio(editor, "Masculine"), Is.Not.Null, "the nested closed feature's values reveal");
			DialogSnapshot.Capture(window, "FwFeatureStructureEditor-02-expanded");
			DialogLayoutAssert.AssertNoCrowding(editor);
		}

		[AvaloniaTest]
		public void SetAssignments_SeedsSelection_Silently()
		{
			var (editor, window) = Show();
			var raised = 0;
			editor.AssignmentsChanged += a => raised++;

			editor.SetAssignments(new[] { new FwFeatureValueAssignment("f-tense", "v-pres") });
			Pump(window);

			Assert.That(raised, Is.EqualTo(0), "seeding does not raise AssignmentsChanged");
			Assert.That(editor.Assignments.Count, Is.EqualTo(1));
			Assert.That(editor.Assignments[0].ValueId, Is.EqualTo("v-pres"));
		}

		[AvaloniaTest]
		public void AddingSecondAssignment_KeepsBoth()
		{
			var (editor, window) = Show();

			editor.SelectValue("v-past");
			Pump(window);
			editor.SelectValue("v-fem");
			Pump(window);

			var ids = editor.Assignments.ToDictionary(a => a.ClosedFeatureId, a => a.ValueId);
			Assert.That(ids.Count, Is.EqualTo(2), "two distinct features each carry their own value");
			Assert.That(ids["f-tense"], Is.EqualTo("v-past"));
			Assert.That(ids["f-gender"], Is.EqualTo("v-fem"));
		}

		[AvaloniaTest]
		public void Assignments_AreFlatPerClosedFeature()
		{
			var (editor, window) = Show();
			editor.SetAssignments(new[]
			{
				new FwFeatureValueAssignment("f-gender", "v-masc"),
				new FwFeatureValueAssignment("f-tense", "v-pres")
			});
			Pump(window);

			// Output is flat (one entry per closed feature); the nesting under Agreement is implicit.
			Assert.That(editor.Assignments.Select(a => a.ClosedFeatureId),
				Is.EquivalentTo(new[] { "f-gender", "f-tense" }));
		}

		// ----- T1: create-requested hooks (deferred wiring) -----

		[AvaloniaTest]
		public void CreateFeature_RaisesRequest()
		{
			var (editor, _) = Show();
			var fired = 0;
			editor.CreateNewFeatureRequested += () => fired++;

			editor.RaiseCreateNewFeature();
			Assert.That(fired, Is.EqualTo(1), "the inline create-feature affordance raises the request");
		}

		[AvaloniaTest]
		public void CreateValue_RaisesRequest_WithFeatureId()
		{
			var (editor, _) = Show();
			string requestedFor = null;
			editor.CreateNewValueRequested += id => requestedFor = id;

			editor.RaiseCreateNewValue("f-tense");
			Assert.That(requestedFor, Is.EqualTo("f-tense"), "the add-value request carries the closed-feature id");
		}

		[AvaloniaTest]
		public void AcceptCreatedFeature_AddsToTree()
		{
			var (editor, window) = Show();
			editor.AcceptCreatedFeature(
				new FwFeatureNode("f-case", "Case", FwFeatureNodeKind.Closed, 0),
				new[] { new FwFeatureNode("v-nom", "Nominative", FwFeatureNodeKind.Value, 1) });
			Pump(window);

			Assert.That(NodeForName(editor, "Case"), Is.Not.Null, "the created feature is added to the tree");
		}

		[AvaloniaTest]
		public void AcceptCreatedValue_AddsAndSelects()
		{
			var (editor, window) = Show();
			IReadOnlyList<FwFeatureValueAssignment> last = null;
			editor.AssignmentsChanged += a => last = a;

			editor.AcceptCreatedValue("f-tense", new FwFeatureNode("v-future", "Future", FwFeatureNodeKind.Value, 1));
			Pump(window);

			Assert.That(last, Is.Not.Null, "accepting a created value selects it (raising the change event)");
			Assert.That(editor.Assignments.Single(a => a.ClosedFeatureId == "f-tense").ValueId,
				Is.EqualTo("v-future"), "the new value becomes the feature's pick");
		}

		// ----- T1: keyboard + filter -----

		[AvaloniaTest]
		public void Keyboard_SpaceTogglesValue()
		{
			var (editor, window) = Show();
			Expand(editor, "Tense");
			Pump(window);

			editor.Tree.SelectedItem = NodeForName(editor, "Present");
			Pump(window);
			editor.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Space
			});
			Pump(window);

			Assert.That(editor.Assignments.Count, Is.EqualTo(1), "Space toggles the highlighted value");
			Assert.That(editor.Assignments[0].ValueId, Is.EqualTo("v-pres"));
		}

		[AvaloniaTest]
		public void Filter_NarrowsToMatchingNodes()
		{
			var (editor, window) = Show();
			editor.FilterBox.Text = "gend";
			Pump(window);

			Assert.That(editor.Tree.IsVisible, Is.False, "the tree hides while filtering");
			Assert.That(editor.FilterList.IsVisible, Is.True, "the flat result list shows");
			var list = (ListBox)editor.FilterList;
			Assert.That(list.ItemCount, Is.EqualTo(1), "only the matching node remains");

			editor.FilterBox.Text = string.Empty;
			Pump(window);
			Assert.That(editor.Tree.IsVisible, Is.True, "clearing the filter restores the tree");
		}

		// ----- T2: integration (composition, no cross-talk) -----

		[AvaloniaTest]
		public void Integration_FeatureEditorWithPosChooser_Compose()
		{
			var pos = new FwPosChooser("MainPos");
			pos.SetNodes(new List<FwPosNode> { new FwPosNode("p-noun", "Noun", 0), new FwPosNode("p-verb", "Verb", 0) });

			var editor = new FwFeatureStructureEditor("InflFeatures");
			editor.SetNodes(FeatureSystem());

			var panel = new StackPanel { Orientation = Orientation.Vertical };
			panel.Children.Add(pos);
			panel.Children.Add(editor);
			var window = new Window { Content = panel, Width = 380, Height = 520 };
			window.Show();
			Pump(window);

			var posPicks = new List<string>();
			pos.SelectionChanged += id => posPicks.Add(id);
			var featurePicks = 0;
			editor.AssignmentsChanged += a => featurePicks++;

			// Drive the POS chooser via its own tree-pick round-trip (the chooser's SelectedPosId setter applies
			// the selection to its TreeView, so seeding the id and reading back Tree.SelectedItem yields the node;
			// reset to null first so the later pick reads as a change rather than a no-op re-selection).
			pos.SelectedPosId = "p-verb";
			var verbNode = pos.Tree.SelectedItem;
			pos.SelectedPosId = null;
			Dispatcher.UIThread.RunJobs();
			pos.Tree.SelectedItem = verbNode;
			Dispatcher.UIThread.RunJobs();
			Assert.That(featurePicks, Is.EqualTo(0), "a POS pick does not fire the feature editor");

			// Drive the feature editor: its event fires, the POS chooser's does not.
			var posCountBefore = posPicks.Count;
			editor.SelectValue("v-past");
			Pump(window);
			Assert.That(featurePicks, Is.EqualTo(1), "a feature pick fires the feature editor");
			Assert.That(posPicks.Count, Is.EqualTo(posCountBefore), "and does not fire the POS chooser");

			// The combined read-back reflects both.
			Assert.That(pos.SelectedPosId, Is.EqualTo("p-verb"));
			Assert.That(editor.Assignments.Single().ValueId, Is.EqualTo("v-past"));
			Expand(editor, "Tense");
			Pump(window);
			DialogSnapshot.Capture(window, "FwFeatureStructureEditor-04-multi-feature");
			DialogLayoutAssert.AssertNoCrowding(panel);
		}

		[AvaloniaTest]
		public void Integration_TwoFeatureEditors_NoCrosstalk()
		{
			var a = new FwFeatureStructureEditor("FeaturesA");
			var b = new FwFeatureStructureEditor("FeaturesB");
			a.SetNodes(FeatureSystem());
			b.SetNodes(FeatureSystem());

			var panel = new StackPanel { Orientation = Orientation.Vertical };
			panel.Children.Add(a);
			panel.Children.Add(b);
			var window = new Window { Content = panel, Width = 380, Height = 640 };
			window.Show();
			Pump(window);

			var aChanges = 0;
			var bChanges = 0;
			a.AssignmentsChanged += _ => aChanges++;
			b.AssignmentsChanged += _ => bChanges++;

			a.SelectValue("v-past");
			Pump(window);

			Assert.That(aChanges, Is.EqualTo(1));
			Assert.That(bChanges, Is.EqualTo(0), "editor B does not react to editor A");
			Assert.That(b.Assignments, Is.Empty, "and B's state is independent");
		}

		// ----- T3: edge cases -----

		[AvaloniaTest]
		public void EmptyFeatureSystem_RendersEmpty_NoCrash()
		{
			var (editor, window) = Show(new List<FwFeatureNode>());
			Pump(window);

			Assert.That(editor.Assignments, Is.Empty);
			DialogLayoutAssert.AssertNoCrowding(editor);
		}

		[AvaloniaTest]
		public void DeeplyNestedComplex_Renders()
		{
			// Complex -> Complex -> Closed -> Value (three levels of nesting).
			var deep = new List<FwFeatureNode>
			{
				new FwFeatureNode("c1", "Outer", FwFeatureNodeKind.Complex, 0),
				new FwFeatureNode("c2", "Inner", FwFeatureNodeKind.Complex, 1),
				new FwFeatureNode("cl", "Deep", FwFeatureNodeKind.Closed, 2),
				new FwFeatureNode("dv", "DeepVal", FwFeatureNodeKind.Value, 3)
			};
			var (editor, window) = Show(deep);
			Expand(editor, "Outer");
			Pump(window);
			Expand(editor, "Inner");
			Pump(window);
			Expand(editor, "Deep");
			Pump(window);

			Assert.That(ValueRadio(editor, "DeepVal"), Is.Not.Null, "the deeply nested value renders");
			editor.SelectValue("dv");
			Pump(window);
			Assert.That(editor.Assignments.Single().ClosedFeatureId, Is.EqualTo("cl"),
				"the assignment is flat — the host rebuilds the nesting");
		}

		[AvaloniaTest]
		public void ClosedFeature_NoValueChosen_IsValid()
		{
			var (editor, window) = Show();
			// Touch a feature but leave <None> selected.
			Expand(editor, "Tense");
			Pump(window);
			Assert.That(editor.Assignments, Is.Empty, "an untouched closed feature contributes no assignment");
			DialogLayoutAssert.AssertNoCrowding(editor);
		}

		[AvaloniaTest]
		public void RapidExpandCollapseThenPick_StaysCoherent()
		{
			var (editor, window) = Show();
			for (var i = 0; i < 5; i++)
			{
				Expand(editor, "Tense");
				Pump(window);
			}
			editor.SelectValue("v-pres");
			Pump(window);

			Assert.That(editor.Assignments.Count, Is.EqualTo(1), "state stays coherent after rapid expand/collapse");
			Assert.That(editor.Assignments[0].ValueId, Is.EqualTo("v-pres"));
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
			var (editor, window) = Show(rtl);
			Expand(editor, "جنس");
			Pump(window);

			Assert.That(ValueRadio(editor, "مذكر"), Is.Not.Null, "complex-script value names render");
			editor.SelectValue("v-rtl1");
			Pump(window);
			Assert.That(editor.Assignments.Single().ValueId, Is.EqualTo("v-rtl1"));
			DialogLayoutAssert.AssertNoCrowding(editor);
		}

		[AvaloniaTest]
		public void LargeFeatureSystem_RendersWithinBudget()
		{
			var big = new List<FwFeatureNode>();
			for (var f = 0; f < 60; f++)
			{
				big.Add(new FwFeatureNode($"f{f}", $"Feature{f}", FwFeatureNodeKind.Closed, 0));
				for (var v = 0; v < 8; v++)
					big.Add(new FwFeatureNode($"f{f}v{v}", $"Val{f}_{v}", FwFeatureNodeKind.Value, 1));
			}

			var sw = Stopwatch.StartNew();
			var (editor, window) = Show(big);
			sw.Stop();

			Assert.That(editor.Assignments, Is.Empty);
			Assert.That(sw.ElapsedMilliseconds, Is.LessThan(8000),
				"a 60-feature/480-value system renders within a generous headless budget");
			DialogLayoutAssert.AssertNoCrowding(editor);
		}

		// ----- T4: end-to-end workflow -----

		[AvaloniaTest]
		public void Workflow_ExpandPickAddSecond_ReadBack()
		{
			// open editor -> expand a complex feature -> pick a closed value -> add a second feature -> read back.
			var (editor, window) = Show();

			// Expand a complex feature to reveal its nested closed feature, then pick a value there.
			Expand(editor, "Agreement");
			Pump(window);
			Expand(editor, "Gender");
			Pump(window);
			editor.SelectValue("v-masc");
			Pump(window);

			// Add a second, top-level feature's value.
			Expand(editor, "Tense");
			Pump(window);
			editor.SelectValue("v-pres");
			Pump(window);

			var ids = editor.Assignments.ToDictionary(a => a.ClosedFeatureId, a => a.ValueId);
			Assert.That(ids.Count, Is.EqualTo(2));
			Assert.That(ids["f-gender"], Is.EqualTo("v-masc"), "the nested complex feature's closed value");
			Assert.That(ids["f-tense"], Is.EqualTo("v-pres"), "the second, top-level feature's value");
		}
	}
}
