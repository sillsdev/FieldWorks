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
	/// avalonia-rule-formula-editor (task 1.1) — the LCModel-free rule-formula projection DTO the Avalonia
	/// editor binds to. Immutable cell-mutation helpers (insert/remove/move) underpin the editor staging
	/// before the Morphology plugin commits the change to the rule in one UOW.
	/// </summary>
	[TestFixture]
	public class RuleFormulaModelTests
	{
		private static RuleFormulaModel Three() => new RuleFormulaModel("PhRegularRule", new[]
		{
			new RuleCell(RuleCellKind.Phoneme, "p", Guid.NewGuid()),
			new RuleCell(RuleCellKind.NaturalClass, "[V]", Guid.NewGuid()),
			new RuleCell(RuleCellKind.Boundary, "#"),
		});

		private static string Text(RuleFormulaModel m) => string.Concat(m.Cells.Select(c => c.DisplayText));

		[Test]
		public void InsertCell_AddsAtIndex_AndIsImmutable()
		{
			var m = Three();
			var n = m.InsertCell(1, new RuleCell(RuleCellKind.Slot, "X"));
			Assert.That(Text(n), Is.EqualTo("pX[V]#"));
			Assert.That(Text(m), Is.EqualTo("p[V]#"), "the original model is unchanged (immutable)");
		}

		[Test]
		public void InsertCell_OutOfRangeIndex_IsClamped()
		{
			var m = Three();
			Assert.That(Text(m.InsertCell(99, new RuleCell(RuleCellKind.Slot, "X"))), Is.EqualTo("p[V]#X"));
			Assert.That(Text(m.InsertCell(-5, new RuleCell(RuleCellKind.Slot, "X"))), Is.EqualTo("Xp[V]#"));
		}

		[Test]
		public void RemoveCell_DropsCell_OrNoOpsOutOfRange()
		{
			var m = Three();
			Assert.That(Text(m.RemoveCell(1)), Is.EqualTo("p#"));
			Assert.That(m.RemoveCell(99), Is.SameAs(m), "out-of-range remove is a no-op returning the same model");
		}

		[Test]
		public void MoveCell_ReordersCells()
		{
			Assert.That(Text(Three().MoveCell(0, 2)), Is.EqualTo("[V]#p"));
		}

		[Test]
		public void Cell_CarriesKindAndTargetGuid()
		{
			var g = Guid.NewGuid();
			var cell = new RuleCell(RuleCellKind.NaturalClass, "[V]", g);
			Assert.That(cell.Kind, Is.EqualTo(RuleCellKind.NaturalClass));
			Assert.That(cell.TargetGuid, Is.EqualTo(g));
			Assert.That(new RuleCell(RuleCellKind.Slot, "X").TargetGuid, Is.Null, "a pure slot has no target");
		}
	}
}
