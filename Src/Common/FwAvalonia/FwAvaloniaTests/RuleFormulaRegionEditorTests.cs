// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the PNG harness
using FwAvaloniaDialogsTests;        // DialogLayoutAssert — the shared geometry tripwire

namespace FwAvaloniaTests
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 1.3) — T5 visual baseline + render assertions for the read-only
	/// <see cref="RuleFormulaRegionEditor"/>. Binds the LCModel-free <see cref="RuleFormulaModel"/> DTO and
	/// renders the cell grid (LHS → RHS / LeftCtx __ RightCtx); these pin the visible states (a populated
	/// regular rule, and one with empty cell groups) with a PNG per stage and the AssertNoCrowding tripwire.
	/// </summary>
	[TestFixture]
	public class RuleFormulaRegionEditorTests
	{
		private static RuleFormulaModel RegularRule() => new RuleFormulaModel("PhRegularRule", new[]
		{
			new RuleFormulaSection(RuleSectionRole.Lhs, new[] { new RuleCell(RuleCellKind.Phoneme, "p") }),
			new RuleFormulaSection(RuleSectionRole.Rhs, new[] { new RuleCell(RuleCellKind.NaturalClass, "V") }),
			new RuleFormulaSection(RuleSectionRole.LeftContext, new[] { new RuleCell(RuleCellKind.NaturalClass, "C") }),
			new RuleFormulaSection(RuleSectionRole.RightContext, new[] { new RuleCell(RuleCellKind.Boundary, "#") }),
		});

		private static RuleFormulaModel PartlyEmptyRule() => new RuleFormulaModel("PhRegularRule", new[]
		{
			new RuleFormulaSection(RuleSectionRole.Lhs, new[] { new RuleCell(RuleCellKind.Phoneme, "p") }),
			new RuleFormulaSection(RuleSectionRole.Rhs, Enumerable.Empty<RuleCell>()),
			new RuleFormulaSection(RuleSectionRole.LeftContext, Enumerable.Empty<RuleCell>()),
			new RuleFormulaSection(RuleSectionRole.RightContext, Enumerable.Empty<RuleCell>()),
		});

		private static (RuleFormulaRegionEditor Editor, Window Window) Show(RuleFormulaModel model)
		{
			var editor = new RuleFormulaRegionEditor(model);
			var window = new Window { Content = editor, Width = 360, Height = 80 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			return (editor, window);
		}

		private static List<string> Texts(Control root) =>
			root.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();

		[AvaloniaTest]
		public void RegularRule_RendersCellsAndSeparators()
		{
			var (editor, window) = Show(RegularRule());

			DialogSnapshot.Capture(window, "RuleFormulaEditor-01-regular");
			DialogLayoutAssert.AssertNoCrowding(editor);

			Assert.That(AutomationProperties.GetAutomationId(editor), Is.EqualTo(RuleFormulaRegionEditor.RuleFormulaAutomationId));
			Assert.That(AutomationProperties.GetName(editor), Is.EqualTo("p → [V] / [C] __ #"),
				"the whole formula reads as the accessibility name");

			var texts = Texts(editor);
			Assert.That(texts, Does.Contain("p"));
			Assert.That(texts, Does.Contain("[V]"), "a natural-class cell is bracketed chrome");
			Assert.That(texts, Does.Contain("[C]"));
			Assert.That(texts, Does.Contain("#"));
			Assert.That(texts, Does.Contain("→").And.Contain("/").And.Contain("__"),
				"the role chrome glyphs render as separators");
		}

		private sealed class FakeSink : IRuleCellCommandSink
		{
			public bool InsertCell(RuleSectionRole role, int index, RuleCellSpec spec) => true;
			public bool DeleteCell(RuleSectionRole role, int index) => true;
			public bool MoveCell(RuleSectionRole role, int from, int to) => true;
			public bool SetCell(RuleSectionRole role, int index, RuleCellSpec spec) => true;
		}

		[AvaloniaTest]
		public void EditableGrid_RendersChooserCells_AndInsertButtons_WithoutCrowding()
		{
			var editor = new RuleFormulaRegionEditor
			{
				Sink = new FakeSink(),
				Options = new List<RegionChoiceOption>
				{
					new RegionChoiceOption("P:" + System.Guid.NewGuid(), "p"),
					new RegionChoiceOption("N:" + System.Guid.NewGuid(), "V"),
				}
			};
			editor.SetModel(RegularRule());
			var window = new Window { Content = editor, Width = 420, Height = 90 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			DialogSnapshot.Capture(window, "RuleFormulaEditor-03-editable");
			DialogLayoutAssert.AssertNoCrowding(editor);

			Assert.That(editor.IsEditable, Is.True);
			var buttons = editor.GetVisualDescendants().OfType<Button>().ToList();
			Assert.That(buttons.Count(b => (AutomationProperties.GetAutomationId(b) ?? "").StartsWith("RuleCell-")),
				Is.EqualTo(4), "one chooser button per cell (p, V, C, #)");
			Assert.That(buttons.Count(b => (AutomationProperties.GetAutomationId(b) ?? "").StartsWith("RuleInsert-")),
				Is.EqualTo(4), "one trailing insert button per section");
		}

		[AvaloniaTest]
		public void EmptyCellGroups_RenderPlaceholders_WithoutCrowding()
		{
			var (editor, window) = Show(PartlyEmptyRule());

			DialogSnapshot.Capture(window, "RuleFormulaEditor-02-empty-groups");
			DialogLayoutAssert.AssertNoCrowding(editor);

			Assert.That(AutomationProperties.GetName(editor), Is.EqualTo("p →  /  __ "));
			// The three empty groups (RHS, left, right) each draw a faint placeholder box.
			var placeholders = editor.GetVisualDescendants().OfType<Border>().Count(b => b != editor);
			Assert.That(placeholders, Is.EqualTo(3), "each empty cell group shows one placeholder box");
		}
	}
}
