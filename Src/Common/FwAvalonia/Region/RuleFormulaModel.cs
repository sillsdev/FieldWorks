// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 1.1/1.2) — the kind of a single cell in a phonological/morphological
	/// rule formula. Mirrors the cell kinds the legacy RuleFormulaControl grid renders.
	/// </summary>
	public enum RuleCellKind
	{
		/// <summary>A single phoneme (PhPhoneme), via PhSimpleContextSeg.</summary>
		Phoneme,

		/// <summary>A natural class (PhNaturalClass), via PhSimpleContextNC. Rendered bracketed [..].</summary>
		NaturalClass,

		/// <summary>A boundary marker (PhBdryMarker), e.g. a morpheme/word boundary, via PhSimpleContextBdry.</summary>
		Boundary,

		/// <summary>A structural slot / variable (PhVariable "X"), or a placeholder for an unset context.</summary>
		Slot,
	}

	/// <summary>
	/// avalonia-rule-formula-editor — the role a <see cref="RuleFormulaSection"/> plays in a rule formula.
	/// Each role owns a distinct LCModel collection (so write-back can target the right home) and a fixed
	/// chrome separator the view/formatter emits before it. Regular-rule roles ship in task 1.2; metathesis
	/// (2.3) and compound (2.5) add their own roles without reshaping the model.
	/// </summary>
	public enum RuleSectionRole
	{
		/// <summary>Left-hand side — the input/structural-description cells (PhSegmentRule.StrucDescOS).</summary>
		Lhs,

		/// <summary>Right-hand side — the structural-change cells (PhSegRuleRHS.StrucChangeOS).</summary>
		Rhs,

		/// <summary>Left environment/context (PhSegRuleRHS.LeftContextOA).</summary>
		LeftContext,

		/// <summary>Right environment/context (PhSegRuleRHS.RightContextOA).</summary>
		RightContext,

		// Metathesis roles (PhMetathesisRule.StrucDescOS partitioned by GetStrucChangeIndex). The two
		// switch groups are the swapped pair; the env groups frame them.
		/// <summary>Metathesis left environment (kidxLeftEnv).</summary>
		LeftEnv,

		/// <summary>Metathesis left switch group, incl. any middle contexts when IsMiddleWithLeftSwitch (kidxLeftSwitch).</summary>
		LeftSwitch,

		/// <summary>Metathesis right switch group, incl. any middle contexts otherwise (kidxRightSwitch).</summary>
		RightSwitch,

		/// <summary>Metathesis right environment (kidxRightEnv).</summary>
		RightEnv,

		// Compound / affix-process roles (MoAffixProcess.InputOS / OutputOS).
		/// <summary>Compound-rule input columns (MoAffixProcess.InputOS).</summary>
		Input,

		/// <summary>Compound-rule output mappings (MoAffixProcess.OutputOS).</summary>
		Output,
	}

	/// <summary>
	/// avalonia-rule-formula-editor (task 1.1) — one cell of a rule formula, as an LCModel-free DTO the
	/// Avalonia editor binds to (the xWorks/Morphology adapter projects the rule into these and maps edits
	/// back; the view never touches LCModel — design Decision 1/3). Immutable: edits produce a new model.
	/// <para><see cref="DisplayText"/> is the BARE token (e.g. "p", "V", "#") — brackets/parentheses are
	/// chrome the view/formatter derives from <see cref="Kind"/>, so the token maps cleanly to its target
	/// object on write-back.</para>
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

		/// <summary>The bare token shown in the cell (phoneme/boundary glyph, class abbreviation, slot label);
		/// brackets are NOT included — the view adds them for <see cref="RuleCellKind.NaturalClass"/>.</summary>
		public string DisplayText { get; }

		/// <summary>The referenced LCModel object's GUID (phoneme/natural-class/boundary), or null for a
		/// pure structural slot/variable. Carried so the adapter can map a cell back to its target on commit.</summary>
		public Guid? TargetGuid { get; }

		public RuleCell WithDisplayText(string displayText) => new RuleCell(Kind, displayText, TargetGuid);
	}

	/// <summary>
	/// avalonia-rule-formula-editor (task 1.2) — one editable cell group of a rule formula (the LHS, RHS,
	/// left or right context). Each section maps to a distinct LCModel collection, so cell mutations stage
	/// against the right home on commit. Immutable; Insert/Remove/Move return a new section.
	/// </summary>
	public sealed class RuleFormulaSection
	{
		public RuleFormulaSection(RuleSectionRole role, IEnumerable<RuleCell> cells)
		{
			Role = role;
			Cells = (cells ?? Enumerable.Empty<RuleCell>()).ToList();
		}

		/// <summary>The role this group plays in the formula (drives ordering + chrome separators).</summary>
		public RuleSectionRole Role { get; }

		/// <summary>The cells in document order (left-to-right within the group). May be empty — an empty
		/// group still renders (the legacy Vc draws an empty bracket pile).</summary>
		public IReadOnlyList<RuleCell> Cells { get; }

		/// <summary>Insert <paramref name="cell"/> at <paramref name="index"/> (clamped), returning a new section.</summary>
		public RuleFormulaSection InsertCell(int index, RuleCell cell)
		{
			if (cell == null) throw new ArgumentNullException(nameof(cell));
			var list = Cells.ToList();
			list.Insert(Clamp(index, 0, list.Count), cell);
			return new RuleFormulaSection(Role, list);
		}

		/// <summary>Remove the cell at <paramref name="index"/>, returning a new section (no-op if out of range).</summary>
		public RuleFormulaSection RemoveCell(int index)
		{
			if (index < 0 || index >= Cells.Count) return this;
			var list = Cells.ToList();
			list.RemoveAt(index);
			return new RuleFormulaSection(Role, list);
		}

		/// <summary>Move the cell at <paramref name="from"/> to <paramref name="to"/> (both clamped), returning a new section.</summary>
		public RuleFormulaSection MoveCell(int from, int to)
		{
			if (from < 0 || from >= Cells.Count) return this;
			var list = Cells.ToList();
			var cell = list[from];
			list.RemoveAt(from);
			list.Insert(Clamp(to, 0, list.Count), cell);
			return new RuleFormulaSection(Role, list);
		}

		internal static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);
	}

	/// <summary>
	/// avalonia-rule-formula-editor (task 1.2) — the LCModel-free projection of a phonological/morphological
	/// rule the Avalonia <c>RuleFormulaRegionEditor</c> renders: an ordered list of cell-group
	/// <see cref="Sections"/> (LHS/RHS/env) plus the rule kind. The Morphology projector builds this from a
	/// <c>PhSegRuleRHS</c>/<c>MoCompoundRule</c> (read) and applies cell edits back to it inside one fenced
	/// UOW (write). Immutable; mutation helpers return a new instance so the editor can stage without
	/// touching LCModel.
	/// </summary>
	public sealed class RuleFormulaModel
	{
		public RuleFormulaModel(string ruleKind, IEnumerable<RuleFormulaSection> sections)
		{
			RuleKind = ruleKind ?? string.Empty;
			Sections = (sections ?? Enumerable.Empty<RuleFormulaSection>()).ToList();
		}

		/// <summary>The rule's class/kind tag (e.g. "PhRegularRule", "PhMetathesisRule", "MoCompoundRule").</summary>
		public string RuleKind { get; }

		/// <summary>The cell groups in document order.</summary>
		public IReadOnlyList<RuleFormulaSection> Sections { get; }

		/// <summary>The section playing <paramref name="role"/>, or null if the model has none.</summary>
		public RuleFormulaSection SectionFor(RuleSectionRole role) => Sections.FirstOrDefault(s => s.Role == role);

		/// <summary>Replace the section at <paramref name="index"/> with <paramref name="section"/>, returning a new model.</summary>
		public RuleFormulaModel WithSection(int index, RuleFormulaSection section)
		{
			if (section == null) throw new ArgumentNullException(nameof(section));
			if (index < 0 || index >= Sections.Count) return this;
			var list = Sections.ToList();
			list[index] = section;
			return new RuleFormulaModel(RuleKind, list);
		}

		/// <summary>
		/// Render the formula as a single deterministic string (bare tokens, NC bracketed, with the chrome
		/// separator each role carries). This is the canonical text the parity test asserts against a
		/// hand-written oracle, and the accessibility text the view exposes. Empty sections contribute "".
		/// </summary>
		public string ToFormulaString()
		{
			var sb = new StringBuilder();
			foreach (var section in Sections)
			{
				sb.Append(SeparatorBefore(section.Role));
				foreach (var cell in section.Cells)
					sb.Append(Token(cell));
			}
			return sb.ToString();
		}

		/// <summary>The bare token for a cell: natural classes are bracketed [..]; everything else is the
		/// raw <see cref="RuleCell.DisplayText"/>. The view and the formula string share this so they never drift.</summary>
		public static string Token(RuleCell cell)
			=> cell != null && cell.Kind == RuleCellKind.NaturalClass ? "[" + cell.DisplayText + "]" : cell?.DisplayText ?? string.Empty;

		/// <summary>The bare chrome glyph that precedes a section of the given role (no surrounding spaces):
		/// "" for LHS, "→" before RHS, "/" before the left context, "__" before the right context. The view
		/// renders these as muted separator labels; <see cref="ToFormulaString"/> wraps them in spaces.</summary>
		public static string RoleSeparatorGlyph(RuleSectionRole role)
		{
			switch (role)
			{
				case RuleSectionRole.Rhs: return "→";
				case RuleSectionRole.LeftContext: return "/";
				case RuleSectionRole.RightContext: return "__";
				// Metathesis: delimit the four cells; "↔" marks the swapped switch pair.
				case RuleSectionRole.LeftSwitch: return "|";
				case RuleSectionRole.RightSwitch: return "↔";
				case RuleSectionRole.RightEnv: return "|";
				// Compound: input columns ⇒ output mappings.
				case RuleSectionRole.Output: return "⇒";
				default: return string.Empty;   // Lhs, LeftEnv, Input (and any not-yet-mapped role)
			}
		}

		/// <summary>The fixed chrome separator the formula string emits before a section of the given role.</summary>
		private static string SeparatorBefore(RuleSectionRole role)
		{
			var glyph = RoleSeparatorGlyph(role);
			return glyph.Length == 0 ? string.Empty : " " + glyph + " ";
		}
	}
}
