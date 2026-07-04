// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 1.2) — projects an LCModel phonological rule into the LCModel-free
	/// <see cref="RuleFormulaModel"/> the Avalonia <c>RuleFormulaRegionEditor</c> renders (design Decision 1/3:
	/// ALL LCModel reads live here in the xWorks/Morphology plugin layer, never in the FwAvalonia view).
	///
	/// <para>Cell display text mirrors the legacy <c>RuleFormulaVcBase</c>/<c>RegRuleFormulaVc</c>: terminal
	/// units (phoneme/boundary) show <c>Name.BestVernacularAlternative</c>; natural classes show
	/// <c>Abbreviation.BestAnalysisAlternative</c> (then <c>Name</c>); a variable shows "X". Brackets around a
	/// natural class are chrome the view derives from the cell kind, so the token here is bare.</para>
	///
	/// <para>// PARITY (task 1.2 read-only scope): feature-based natural classes (PhNCFeatures: multiline
	/// feature stack + α/β feature-constraint variables) render as their abbreviation/name only, and
	/// iteration contexts (PhIterationContext min/max sub/superscript) render their member only. Both are
	/// deferred to the editable phase per design (advisor-confirmed scope call).</para>
	/// </summary>
	public static class RuleFormulaProjector
	{
		/// <summary>Rule-kind tag for a regular phonological rule (PhRegularRule via PhSegmentRule).</summary>
		public const string RegularRuleKind = "PhRegularRule";

		/// <summary>Rule-kind tag for a metathesis phonological rule (PhMetathesisRule).</summary>
		public const string MetathesisRuleKind = "PhMetathesisRule";

		/// <summary>Rule-kind tag for a compound / affix-process rule (MoAffixProcess).</summary>
		public const string CompoundRuleKind = "MoAffixProcess";

		/// <summary>
		/// Project the regular-rule right-hand-side (the slice's root object) into a four-section formula
		/// model: LHS (the owning rule's StrucDesc), RHS (this RHS's StrucChange), left context, right context.
		/// Empty sections are kept — the formula always shows all four (and their chrome separators).
		/// </summary>
		public static RuleFormulaModel ProjectRegularRule(IPhSegRuleRHS rhs)
		{
			if (rhs == null) throw new ArgumentNullException(nameof(rhs));
			var rule = rhs.OwningRule;
			var sections = new List<RuleFormulaSection>
			{
				new RuleFormulaSection(RuleSectionRole.Lhs, ProjectSimpleContexts(rule?.StrucDescOS)),
				new RuleFormulaSection(RuleSectionRole.Rhs, ProjectSimpleContexts(rhs.StrucChangeOS)),
				new RuleFormulaSection(RuleSectionRole.LeftContext, ProjectContext(rhs.LeftContextOA)),
				new RuleFormulaSection(RuleSectionRole.RightContext, ProjectContext(rhs.RightContextOA)),
			};
			return new RuleFormulaModel(RegularRuleKind, sections);
		}

		/// <summary>
		/// Project a metathesis rule into four cell groups (left env / left switch / right switch / right
		/// env). The single <c>StrucDescOS</c> is partitioned by <c>GetStrucChangeIndex</c> per context (which
		/// already folds the optional "middle" contexts into whichever switch owns them), so we bucket each
		/// context in document order into its role — matching the legacy <c>MetaRuleFormulaControl</c> input
		/// row. // PARITY (task 2.3 read-only): the legacy result/swap row is a display echo of the input
		/// switch pair; the editor shows the input row and notes the swap via the "↔" chrome.
		/// </summary>
		public static RuleFormulaModel ProjectMetathesisRule(IPhMetathesisRule rule)
		{
			if (rule == null) throw new ArgumentNullException(nameof(rule));
			var left = new List<RuleCell>();
			var leftSwitch = new List<RuleCell>();
			var rightSwitch = new List<RuleCell>();
			var right = new List<RuleCell>();

			foreach (var ctx in rule.StrucDescOS)
			{
				var bucket = BucketFor(rule.GetStrucChangeIndex(ctx), left, leftSwitch, rightSwitch, right);
				bucket?.AddRange(ProjectContext(ctx));
			}

			return new RuleFormulaModel(MetathesisRuleKind, new List<RuleFormulaSection>
			{
				new RuleFormulaSection(RuleSectionRole.LeftEnv, left),
				new RuleFormulaSection(RuleSectionRole.LeftSwitch, leftSwitch),
				new RuleFormulaSection(RuleSectionRole.RightSwitch, rightSwitch),
				new RuleFormulaSection(RuleSectionRole.RightEnv, right),
			});
		}

		/// <summary>
		/// Project a compound / affix-process rule (MoAffixProcess) into an Input section (the
		/// input columns, each an IPhContextOrVar flattened like a context) and an Output section (the rule
		/// mappings). A copy-from-input maps to the 1-based index of its referenced input column; an
		/// insert-phones maps to its phoneme/boundary tokens; a modify-from-input maps to "n[class]".
		/// // PARITY (task 2.5 read-only): editing the input/output and the modify feature detail are deferred.
		/// </summary>
		public static RuleFormulaModel ProjectCompoundRule(IMoAffixProcess rule)
		{
			if (rule == null) throw new ArgumentNullException(nameof(rule));
			var inputs = new List<RuleCell>();
			foreach (var input in rule.InputOS)
				inputs.AddRange(ProjectContext(input));

			var outputs = new List<RuleCell>();
			foreach (var mapping in rule.OutputOS)
				outputs.AddRange(ProjectMapping(mapping));

			return new RuleFormulaModel(CompoundRuleKind, new List<RuleFormulaSection>
			{
				new RuleFormulaSection(RuleSectionRole.Input, inputs),
				new RuleFormulaSection(RuleSectionRole.Output, outputs),
			});
		}

		private static IEnumerable<RuleCell> ProjectMapping(IMoRuleMapping mapping)
		{
			var cells = new List<RuleCell>();
			if (mapping == null)
				return cells;
			switch (mapping.ClassID)
			{
				case MoCopyFromInputTags.kClassId:
					var copy = (IMoCopyFromInput)mapping;
					cells.Add(new RuleCell(RuleCellKind.Slot, IndexLabel(copy.ContentRA)));
					break;
				case MoInsertPhonesTags.kClassId:
					foreach (var tu in ((IMoInsertPhones)mapping).ContentRS)
						cells.Add(new RuleCell(tu is IPhBdryMarker ? RuleCellKind.Boundary : RuleCellKind.Phoneme,
							TermUnitText(tu), tu?.Guid));
					break;
				case MoModifyFromInputTags.kClassId:
					var modify = (IMoModifyFromInput)mapping;
					cells.Add(new RuleCell(RuleCellKind.Slot,
						IndexLabel(modify.ContentRA) + "[" + NaturalClassText(modify.ModificationRA) + "]"));
					break;
				case MoInsertNCTags.kClassId:
					var insNc = (IMoInsertNC)mapping;
					cells.Add(new RuleCell(RuleCellKind.NaturalClass, NaturalClassText(insNc.ContentRA),
						insNc.ContentRA?.Guid));
					break;
			}
			return cells;
		}

		// The 1-based column number of a referenced input (legacy ContentRA.IndexInOwner + 1), or "0" when unset.
		private static string IndexLabel(IPhContextOrVar input)
			=> input == null ? "0" : (input.IndexInOwner + 1).ToString();

		private static List<RuleCell> BucketFor(int kidx, List<RuleCell> left, List<RuleCell> leftSwitch,
			List<RuleCell> rightSwitch, List<RuleCell> right)
		{
			if (kidx == PhMetathesisRuleTags.kidxLeftEnv) return left;
			if (kidx == PhMetathesisRuleTags.kidxLeftSwitch) return leftSwitch;
			if (kidx == PhMetathesisRuleTags.kidxRightSwitch) return rightSwitch;
			if (kidx == PhMetathesisRuleTags.kidxRightEnv) return right;
			return null;
		}

		private static IEnumerable<RuleCell> ProjectSimpleContexts(IEnumerable<IPhSimpleContext> seq)
		{
			var cells = new List<RuleCell>();
			if (seq == null)
				return cells;
			foreach (var ctx in seq)
				cells.AddRange(ProjectContext(ctx));
			return cells;
		}

		/// <summary>
		/// Flatten a context-or-variable into bare cells. A sequence flattens its members; simple contexts
		/// (seg/bdry/NC) map to one cell each; a variable is "X". Mirrors <c>RuleFormulaVcBase.Display</c>'s
		/// dispatch on <c>ClassID</c>.
		/// </summary>
		private static IEnumerable<RuleCell> ProjectContext(IPhContextOrVar ctxtOrVar)
		{
			var cells = new List<RuleCell>();
			if (ctxtOrVar == null)
				return cells;

			switch (ctxtOrVar.ClassID)
			{
				case PhSequenceContextTags.kClassId:
					foreach (var member in ((IPhSequenceContext)ctxtOrVar).MembersRS)
						cells.AddRange(ProjectContext(member));
					break;

				case PhSimpleContextSegTags.kClassId:
					var seg = (IPhSimpleContextSeg)ctxtOrVar;
					cells.Add(new RuleCell(RuleCellKind.Phoneme, TermUnitText(seg.FeatureStructureRA),
						seg.FeatureStructureRA?.Guid));
					break;

				case PhSimpleContextBdryTags.kClassId:
					var bdry = (IPhSimpleContextBdry)ctxtOrVar;
					cells.Add(new RuleCell(RuleCellKind.Boundary, TermUnitText(bdry.FeatureStructureRA),
						bdry.FeatureStructureRA?.Guid));
					break;

				case PhSimpleContextNCTags.kClassId:
					var nc = (IPhSimpleContextNC)ctxtOrVar;
					cells.Add(new RuleCell(RuleCellKind.NaturalClass, NaturalClassText(nc.FeatureStructureRA),
						nc.FeatureStructureRA?.Guid));
					break;

				case PhVariableTags.kClassId:
					cells.Add(new RuleCell(RuleCellKind.Slot, "X"));
					break;

				case PhIterationContextTags.kClassId:
					// PARITY: min/max sub/superscript deferred (1.2 scope) — project the member only.
					cells.AddRange(ProjectContext(((IPhIterationContext)ctxtOrVar).MemberRA));
					break;
			}
			return cells;
		}

		private static string TermUnitText(IPhTerminalUnit tu)
		{
			if (tu == null)
				return "?";
			var text = tu.Name.BestVernacularAlternative?.Text;
			return string.IsNullOrEmpty(text) ? "?" : text;
		}

		private static string NaturalClassText(IPhNaturalClass nc)
		{
			if (nc == null)
				return "?";
			var abbr = nc.Abbreviation.BestAnalysisAlternative?.Text;
			if (!string.IsNullOrEmpty(abbr))
				return abbr;
			var name = nc.Name.BestAnalysisAlternative?.Text;
			return string.IsNullOrEmpty(name) ? "?" : name;
		}
	}
}
