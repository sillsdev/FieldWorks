// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 1.1) — the kind of a single cell in a phonological/morphological
	/// rule formula. Mirrors the cell kinds the legacy RuleFormulaControl grid renders.
	/// </summary>
	public enum RuleCellKind
	{
		/// <summary>A single phoneme (PhPhoneme).</summary>
		Phoneme,

		/// <summary>A natural class (PhNaturalClass).</summary>
		NaturalClass,

		/// <summary>A boundary marker (PhBdryMarker), e.g. a morpheme/word boundary.</summary>
		Boundary,

		/// <summary>A structural slot (e.g. a variable / context column the rule defines).</summary>
		Slot,
	}

	/// <summary>
	/// avalonia-rule-formula-editor (task 1.1) — one cell of a rule formula, as an LCModel-free DTO the
	/// Avalonia editor binds to (the xWorks/Morphology adapter projects the rule into these and maps edits
	/// back; the view never touches LCModel — design Decision 1/3). Immutable: edits produce a new model.
	/// </summary>
	public sealed class RuleCell
	{
		public RuleCell(RuleCellKind kind, string displayText, Guid? targetGuid = null)
		{
			Kind = kind;
			DisplayText = displayText ?? string.Empty;
			TargetGuid = targetGuid;
		}

		/// <summary>What this cell represents.</summary>
		public RuleCellKind Kind { get; }

		/// <summary>The text shown in the cell (phoneme/class abbreviation, boundary glyph, slot label).</summary>
		public string DisplayText { get; }

		/// <summary>The referenced LCModel object's GUID (phoneme/natural-class/boundary), or null for a
		/// pure structural slot. Carried so the adapter can map a cell back to its target on commit.</summary>
		public Guid? TargetGuid { get; }

		public RuleCell WithDisplayText(string displayText) => new RuleCell(Kind, displayText, TargetGuid);
	}

	/// <summary>
	/// avalonia-rule-formula-editor (task 1.1) — the LCModel-free projection of a phonological/morphological
	/// rule the Avalonia <c>RuleFormulaRegionEditor</c> renders: an ordered list of cells plus the rule
	/// kind. The Morphology plugin builds this from a <c>PhSegmentRule</c>/<c>MoCompoundRule</c> (read) and
	/// applies cell edits back to it inside one fenced UOW (write). Immutable; cell-mutation helpers return a
	/// new instance so the editor can stage without touching LCModel.
	/// </summary>
	public sealed class RuleFormulaModel
	{
		public RuleFormulaModel(string ruleKind, IEnumerable<RuleCell> cells)
		{
			RuleKind = ruleKind ?? string.Empty;
			Cells = (cells ?? Enumerable.Empty<RuleCell>()).ToList();
		}

		/// <summary>The rule's class/kind tag (e.g. "PhRegularRule", "PhMetathesisRule", "MoCompoundRule").</summary>
		public string RuleKind { get; }

		/// <summary>The cells in document order (left-to-right in the formula grid).</summary>
		public IReadOnlyList<RuleCell> Cells { get; }

		/// <summary>Insert <paramref name="cell"/> at <paramref name="index"/> (clamped), returning a new model.</summary>
		public RuleFormulaModel InsertCell(int index, RuleCell cell)
		{
			if (cell == null) throw new ArgumentNullException(nameof(cell));
			var list = Cells.ToList();
			list.Insert(Clamp(index, 0, list.Count), cell);
			return new RuleFormulaModel(RuleKind, list);
		}

		/// <summary>Remove the cell at <paramref name="index"/>, returning a new model (no-op if out of range).</summary>
		public RuleFormulaModel RemoveCell(int index)
		{
			if (index < 0 || index >= Cells.Count) return this;
			var list = Cells.ToList();
			list.RemoveAt(index);
			return new RuleFormulaModel(RuleKind, list);
		}

		/// <summary>Move the cell at <paramref name="from"/> to <paramref name="to"/> (both clamped), returning a new model.</summary>
		public RuleFormulaModel MoveCell(int from, int to)
		{
			if (from < 0 || from >= Cells.Count) return this;
			var list = Cells.ToList();
			var cell = list[from];
			list.RemoveAt(from);
			list.Insert(Clamp(to, 0, list.Count), cell);
			return new RuleFormulaModel(RuleKind, list);
		}

		private static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);
	}
}
