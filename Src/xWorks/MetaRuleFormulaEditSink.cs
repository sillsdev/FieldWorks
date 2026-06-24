// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 2.3 editing) — the LCModel-side handler for METATHESIS cell
	/// gestures. A metathesis rule keeps one <c>StrucDescOS</c> partitioned into four cells by index ranges
	/// (LeftEnv / LeftSwitch / RightSwitch / RightEnv); a gesture maps a cell-local index to the absolute
	/// <c>StrucDescOS</c> position, mutates it, and maintains the partition metadata exactly as the legacy
	/// <c>MetaRuleFormulaControl</c>: insert calls <c>UpdateStrucChange(kidx, index, true)</c> after
	/// inserting; delete relies on <c>PhSimpleContext.DeleteObjectSideEffects</c> to re-derive the partition.
	/// Staged through the region's shared fenced session, one undo step per gesture, then re-projects.
	///
	/// <para>// PARITY: scoped to rules WITHOUT a "middle" context (the common case — middle is an advanced
	/// option that folds into a switch cell). When <c>MiddleIndex != -1</c>, switch-cell edits reject; the
	/// environment cells stay editable (they are outside the switch pair). Move/reorder is deferred.</para>
	/// </summary>
	public sealed class MetaRuleFormulaEditSink : IRuleCellCommandSink
	{
		private readonly IPhMetathesisRule _rule;
		private readonly LcmCache _cache;
		private readonly IRegionEditContext _host;
		private readonly Action<RuleFormulaModel> _onModelChanged;

		public MetaRuleFormulaEditSink(IPhMetathesisRule rule, LcmCache cache, IRegionEditContext host,
			Action<RuleFormulaModel> onModelChanged)
		{
			_rule = rule ?? throw new ArgumentNullException(nameof(rule));
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_host = host;
			_onModelChanged = onModelChanged;
		}

		public bool InsertCell(RuleSectionRole role, int index, RuleCellSpec spec)
		{
			if (spec == null || !TryKidx(role, out var kidx) || BlockedByMiddle(role) || !TryResolveTarget(spec, out var target))
				return false;
			int count = CellCount(role);
			if (index < 0 || index > count)
				return false;
			int absolute = InsertionIndex(role, index);
			return Apply(() =>
			{
				var ctx = CreateBareContext(spec.Kind);
				if (ctx == null)
					return false;
				_rule.StrucDescOS.Insert(absolute, ctx);     // own it first
				_rule.UpdateStrucChange(kidx, absolute, true); // maintain the partition metadata
				SetTarget(ctx, target);
				return true;
			});
		}

		public bool DeleteCell(RuleSectionRole role, int index)
		{
			if (!TryKidx(role, out _) || BlockedByMiddle(role))
				return false;
			int count = CellCount(role);
			if (index < 0 || index >= count)
				return false;
			int absolute = CellStart(role) + index;
			return Apply(() =>
			{
				var ctx = _rule.StrucDescOS[absolute];
				ctx.PreRemovalSideEffects();
				_rule.StrucDescOS.Remove(ctx);   // DeleteObjectSideEffects re-derives the partition indices
				return true;
			});
		}

		// Reordering within a metathesis cell (MoveOwnSeq + partition re-derivation) is deferred. // PARITY
		public bool MoveCell(RuleSectionRole role, int from, int to) => false;

		public bool SetCell(RuleSectionRole role, int index, RuleCellSpec spec)
		{
			if (spec == null || !TryKidx(role, out var kidx) || BlockedByMiddle(role) || !TryResolveTarget(spec, out var target))
				return false;
			int count = CellCount(role);
			if (index < 0 || index >= count)
				return false;
			int absolute = CellStart(role) + index;
			return Apply(() =>
			{
				var old = _rule.StrucDescOS[absolute];
				old.PreRemovalSideEffects();
				_rule.StrucDescOS.Remove(old);
				var fresh = CreateBareContext(spec.Kind);
				if (fresh == null)
					return false;
				_rule.StrucDescOS.Insert(absolute, fresh);
				_rule.UpdateStrucChange(kidx, absolute, true);
				SetTarget(fresh, target);
				return true;
			});
		}

		// ----- metathesis partition math (legacy MetaRuleFormulaControl parity, non-middle) -----

		private bool BlockedByMiddle(RuleSectionRole role)
			=> _rule.MiddleIndex != -1
				&& (role == RuleSectionRole.LeftSwitch || role == RuleSectionRole.RightSwitch);

		private int CellCount(RuleSectionRole role)
		{
			switch (role)
			{
				case RuleSectionRole.LeftEnv: return _rule.LeftEnvIndex == -1 ? 0 : _rule.LeftEnvLimit;
				case RuleSectionRole.LeftSwitch: return _rule.LeftSwitchIndex == -1 ? 0 : _rule.LeftSwitchLimit - _rule.LeftSwitchIndex;
				case RuleSectionRole.RightSwitch: return _rule.RightSwitchIndex == -1 ? 0 : _rule.RightSwitchLimit - _rule.RightSwitchIndex;
				case RuleSectionRole.RightEnv: return _rule.RightEnvIndex == -1 ? 0 : _rule.RightEnvLimit - _rule.RightEnvIndex;
				default: return 0;
			}
		}

		// The absolute StrucDescOS index where this cell's contexts start (only meaningful for a non-empty cell).
		private int CellStart(RuleSectionRole role)
		{
			switch (role)
			{
				case RuleSectionRole.LeftEnv: return 0;
				case RuleSectionRole.LeftSwitch: return _rule.LeftSwitchIndex;
				case RuleSectionRole.RightSwitch: return _rule.RightSwitchIndex;
				case RuleSectionRole.RightEnv: return _rule.RightEnvIndex;
				default: return _rule.StrucDescOS.Count;
			}
		}

		// Where to insert a cell at cell-local position `index`. For a non-empty cell this is CellStart+index;
		// for an empty cell it is the legacy GetInsertionIndex fallback (the start of the next non-empty cell,
		// else the end). Middle is excluded by BlockedByMiddle for the switch cells.
		private int InsertionIndex(RuleSectionRole role, int index)
		{
			if (CellCount(role) > 0)
				return CellStart(role) + index;

			switch (role)
			{
				case RuleSectionRole.LeftEnv:
					return 0;
				case RuleSectionRole.LeftSwitch:
					if (_rule.RightSwitchIndex != -1) return _rule.RightSwitchIndex;
					if (_rule.RightEnvIndex != -1) return _rule.RightEnvIndex;
					break;
				case RuleSectionRole.RightSwitch:
					if (_rule.RightEnvIndex != -1) return _rule.RightEnvIndex;
					break;
			}
			return _rule.StrucDescOS.Count;
		}

		private static bool TryKidx(RuleSectionRole role, out int kidx)
		{
			switch (role)
			{
				case RuleSectionRole.LeftEnv: kidx = PhMetathesisRuleTags.kidxLeftEnv; return true;
				case RuleSectionRole.LeftSwitch: kidx = PhMetathesisRuleTags.kidxLeftSwitch; return true;
				case RuleSectionRole.RightSwitch: kidx = PhMetathesisRuleTags.kidxRightSwitch; return true;
				case RuleSectionRole.RightEnv: kidx = PhMetathesisRuleTags.kidxRightEnv; return true;
				default: kidx = -1; return false;
			}
		}

		// ----- shared staging + context build (kept local for metathesis isolation) -----

		private IPhSimpleContext CreateBareContext(RuleCellKind kind)
		{
			switch (kind)
			{
				case RuleCellKind.Phoneme: return _cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
				case RuleCellKind.Boundary: return _cache.ServiceLocator.GetInstance<IPhSimpleContextBdryFactory>().Create();
				case RuleCellKind.NaturalClass: return _cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
				default: return null;
			}
		}

		private static void SetTarget(IPhSimpleContext ctx, ICmObject target)
		{
			switch (ctx)
			{
				case IPhSimpleContextSeg seg: seg.FeatureStructureRA = (IPhPhoneme)target; break;
				case IPhSimpleContextBdry bdry: bdry.FeatureStructureRA = (IPhBdryMarker)target; break;
				case IPhSimpleContextNC nc: nc.FeatureStructureRA = (IPhNaturalClass)target; break;
			}
		}

		private bool TryResolveTarget(RuleCellSpec spec, out ICmObject target)
		{
			target = null;
			if (spec.TargetGuid == null
				|| !_cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(spec.TargetGuid.Value, out var obj)
				|| obj == null)
				return false;
			switch (spec.Kind)
			{
				case RuleCellKind.Phoneme: if (!(obj is IPhPhoneme)) return false; break;
				case RuleCellKind.Boundary: if (!(obj is IPhBdryMarker)) return false; break;
				case RuleCellKind.NaturalClass: if (!(obj is IPhNaturalClass)) return false; break;
				default: return false;
			}
			target = obj;
			return true;
		}

		private bool Apply(Func<bool> mutate)
		{
			bool ok;
			if (_host is RegionEditContextBase fenced)
			{
				ok = fenced.Stage(mutate, "Rule Formula");
				if (ok)
					fenced.Commit();
			}
			else
			{
				ok = mutate();
			}
			if (ok)
				_onModelChanged?.Invoke(RuleFormulaProjector.ProjectMetathesisRule(_rule));
			return ok;
		}
	}
}
