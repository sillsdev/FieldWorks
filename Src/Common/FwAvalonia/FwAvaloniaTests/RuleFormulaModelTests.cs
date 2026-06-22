// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaTests
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 1.1/1.2) — the LCModel-free rule-formula projection DTO the
	/// Avalonia editor binds to. Sectioned (LHS/RHS/env) with immutable per-section cell-mutation helpers
	/// (insert/remove/move) that underpin the editor staging before the Morphology plugin commits the
	/// change to the rule in one UOW. <see cref="RuleFormulaModel.ToFormulaString"/> is the canonical
	/// rendering the parity test asserts against.
	/// </summary>
	[TestFixture]
	public class RuleFormulaModelTests
	{
		private static RuleFormulaSection Three(RuleSectionRole role = RuleSectionRole.Lhs) =>
			new RuleFormulaSection(role, new[]
			{
				new RuleCell(RuleCellKind.Phoneme, "p", Guid.NewGuid()),
				new RuleCell(RuleCellKind.NaturalClass, "V", Guid.NewGuid()),
				new RuleCell(RuleCellKind.Boundary, "#"),
			});

		private static string Text(RuleFormulaSection s) => string.Concat(s.Cells.Select(c => c.DisplayText));

		[Test]
		public void InsertCell_AddsAtIndex_AndIsImmutable()
		{
			var s = Three();
			var n = s.InsertCell(1, new RuleCell(RuleCellKind.Slot, "X"));
			Assert.That(Text(n), Is.EqualTo("pXV#"));
			Assert.That(Text(s), Is.EqualTo("pV#"), "the original section is unchanged (immutable)");
		}

		[Test]
		public void InsertCell_OutOfRangeIndex_IsClamped()
		{
			var s = Three();
			Assert.That(Text(s.InsertCell(99, new RuleCell(RuleCellKind.Slot, "X"))), Is.EqualTo("pV#X"));
			Assert.That(Text(s.InsertCell(-5, new RuleCell(RuleCellKind.Slot, "X"))), Is.EqualTo("XpV#"));
		}

		[Test]
		public void RemoveCell_DropsCell_OrNoOpsOutOfRange()
		{
			var s = Three();
			Assert.That(Text(s.RemoveCell(1)), Is.EqualTo("p#"));
			Assert.That(s.RemoveCell(99), Is.SameAs(s), "out-of-range remove is a no-op returning the same section");
		}

		[Test]
		public void MoveCell_ReordersCells()
		{
			Assert.That(Text(Three().MoveCell(0, 2)), Is.EqualTo("V#p"));
		}

		[Test]
		public void Cell_CarriesKindAndTargetGuid()
		{
			var g = Guid.NewGuid();
			var cell = new RuleCell(RuleCellKind.NaturalClass, "V", g);
			Assert.That(cell.Kind, Is.EqualTo(RuleCellKind.NaturalClass));
			Assert.That(cell.TargetGuid, Is.EqualTo(g));
			Assert.That(new RuleCell(RuleCellKind.Slot, "X").TargetGuid, Is.Null, "a pure slot has no target");
		}

		[Test]
		public void SectionFor_FindsByRole_AndWithSectionReplaces()
		{
			var model = new RuleFormulaModel("PhRegularRule", new[]
			{
				new RuleFormulaSection(RuleSectionRole.Lhs, new[] { new RuleCell(RuleCellKind.Phoneme, "p") }),
				new RuleFormulaSection(RuleSectionRole.Rhs, Enumerable.Empty<RuleCell>()),
			});
			Assert.That(model.SectionFor(RuleSectionRole.Lhs).Cells, Has.Count.EqualTo(1));
			Assert.That(model.SectionFor(RuleSectionRole.RightContext), Is.Null);

			var rhsWithCell = model.SectionFor(RuleSectionRole.Rhs).InsertCell(0, new RuleCell(RuleCellKind.Boundary, "#"));
			var updated = model.WithSection(1, rhsWithCell);
			Assert.That(updated.SectionFor(RuleSectionRole.Rhs).Cells, Has.Count.EqualTo(1));
			Assert.That(model.SectionFor(RuleSectionRole.Rhs).Cells, Is.Empty, "original model unchanged (immutable)");
		}

		[Test]
		public void ToFormulaString_RendersBareTokens_NcBracketed_WithRoleSeparators()
		{
			// p → [V] / [C] __ #  — the canonical regular-rule formula oracle shape.
			var model = new RuleFormulaModel("PhRegularRule", new[]
			{
				new RuleFormulaSection(RuleSectionRole.Lhs, new[] { new RuleCell(RuleCellKind.Phoneme, "p") }),
				new RuleFormulaSection(RuleSectionRole.Rhs, new[] { new RuleCell(RuleCellKind.NaturalClass, "V") }),
				new RuleFormulaSection(RuleSectionRole.LeftContext, new[] { new RuleCell(RuleCellKind.NaturalClass, "C") }),
				new RuleFormulaSection(RuleSectionRole.RightContext, new[] { new RuleCell(RuleCellKind.Boundary, "#") }),
			});
			Assert.That(model.ToFormulaString(), Is.EqualTo("p → [V] / [C] __ #"));
		}

		[Test]
		public void ToFormulaString_EmptySectionsContributeNothingButTheirSeparator()
		{
			var model = new RuleFormulaModel("PhRegularRule", new[]
			{
				new RuleFormulaSection(RuleSectionRole.Lhs, new[] { new RuleCell(RuleCellKind.Phoneme, "p") }),
				new RuleFormulaSection(RuleSectionRole.Rhs, Enumerable.Empty<RuleCell>()),
				new RuleFormulaSection(RuleSectionRole.LeftContext, Enumerable.Empty<RuleCell>()),
				new RuleFormulaSection(RuleSectionRole.RightContext, Enumerable.Empty<RuleCell>()),
			});
			Assert.That(model.ToFormulaString(), Is.EqualTo("p →  /  __ "));
		}
	}
}
