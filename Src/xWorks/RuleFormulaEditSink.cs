// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 2.1) — the LCModel-side handler for the rule grid's cell gestures
	/// (design Decision 1: ALL rule reads/writes live here, never in the FwAvalonia view). It applies each
	/// gesture to the regular rule inside the region's SHARED fenced edit session (<see cref="Apply"/> stages
	/// through <c>RegionEditContextBase.Stage</c> and commits as ONE undoable step — the structural-gesture
	/// "commit immediately + re-show" cadence the StText paragraph CRUD uses), then re-projects so the grid
	/// re-renders from domain truth.
	///
	/// <para>Covers all four regular-rule sections: the plain owning-sequence LHS (<c>StrucDescOS</c>) /
	/// RHS (<c>StrucChangeOS</c>), and the left/right CONTEXT atomic slots with their
	/// atomic↔<c>PhSequenceContext</c> transition (0→1→2 promotes a single context into a sequence; deleting
	/// back removes members — legacy <c>RegRuleFormulaControl.CreateSeqCtxt</c>/<c>InsertContextInto</c>/
	/// <c>RemoveContextsFrom</c> parity). One simple context maps to one projected cell, so (role, index)
	/// aligns with the projector's flatten order. // PARITY: context members are assumed simple contexts
	/// (the projector defers iteration / nested sequences).</para>
	/// </summary>
	public sealed class RuleFormulaEditSink : IRuleCellCommandSink
	{
		private readonly IPhSegRuleRHS _rhs;
		private readonly LcmCache _cache;
		private readonly IRegionEditContext _host;
		private readonly Action<RuleFormulaModel> _onModelChanged;

		public RuleFormulaEditSink(IPhSegRuleRHS rhs, LcmCache cache, IRegionEditContext host,
			Action<RuleFormulaModel> onModelChanged)
		{
			_rhs = rhs ?? throw new ArgumentNullException(nameof(rhs));
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_host = host;
			_onModelChanged = onModelChanged;
		}

		public bool InsertCell(RuleSectionRole role, int index, RuleCellSpec spec)
		{
			if (spec == null || !TryResolveTarget(spec, out var target))   // resolve BEFORE mutating
				return false;

			var seq = OwningSequenceFor(role);
			if (seq != null)   // LHS / RHS — plain owning sequence
			{
				if (index < 0 || index > seq.Count)
					return false;
				return Apply(() =>
				{
					var ctx = CreateBareContext(spec.Kind);
					if (ctx == null)
						return false;
					seq.Insert(index, ctx);     // own it FIRST, then set the reference (legacy order)
					SetTarget(ctx, target);
					return true;
				});
			}

			if (!IsContextSection(role))
				return false;
			return Apply(() => InsertIntoContext(role, index, spec.Kind, target));
		}

		public bool DeleteCell(RuleSectionRole role, int index)
		{
			var seq = OwningSequenceFor(role);
			if (seq != null)   // LHS / RHS
			{
				if (index < 0 || index >= seq.Count)
					return false;
				return Apply(() =>
				{
					var ctx = seq[index];
					ctx.PreRemovalSideEffects();
					seq.Remove(ctx);   // owning sequence: Remove deletes the owned context (legacy parity)
					return true;
				});
			}

			if (!IsContextSection(role) || GetContextOA(role) == null)
				return false;
			return Apply(() => DeleteFromContext(role, index));
		}

		public bool MoveCell(RuleSectionRole role, int from, int to)
		{
			var seq = OwningSequenceFor(role);
			if (seq != null)   // LHS / RHS
			{
				int n = seq.Count;
				if (from < 0 || from >= n || to < 0 || to >= n || from == to)
					return false;
				return Apply(() =>
				{
					// MoveOwnSeq inserts the moved item BEFORE the object at the destination index in the
					// ORIGINAL ordering: moving later (to>from) targets to+1; moving earlier targets to.
					int dest = to > from ? to + 1 : to;
					_cache.DomainDataByFlid.MoveOwnSeq(OwnerHvoFor(role), FlidFor(role), from, from,
						OwnerHvoFor(role), FlidFor(role), dest);
					return true;
				});
			}

			// A context section only reorders when it is a multi-member sequence.
			if (!IsContextSection(role) || !(GetContextOA(role) is IPhSequenceContext members))
				return false;
			int count = members.MembersRS.Count;
			if (from < 0 || from >= count || to < 0 || to >= count || from == to)
				return false;
			return Apply(() =>
			{
				var m = members.MembersRS[from];
				members.MembersRS.Remove(m);     // reference sequence: Remove does NOT delete the owned ctx
				members.MembersRS.Insert(to, m); // post-removal insert at the final index
				return true;
			});
		}

		public bool SetCell(RuleSectionRole role, int index, RuleCellSpec spec)
		{
			if (spec == null || !TryResolveTarget(spec, out var target))   // resolve BEFORE mutating
				return false;

			var seq = OwningSequenceFor(role);
			if (seq != null)   // LHS / RHS
			{
				if (index < 0 || index >= seq.Count)
					return false;
				return Apply(() =>
				{
					var fresh = CreateBareContext(spec.Kind);
					if (fresh == null)
						return false;
					var old = seq[index];
					old.PreRemovalSideEffects();
					seq.Remove(old);
					seq.Insert(index, fresh);   // own it FIRST, then set the reference (legacy order)
					SetTarget(fresh, target);
					return true;
				});
			}

			if (!IsContextSection(role) || GetContextOA(role) == null)
				return false;
			return Apply(() => SetInContext(role, index, spec.Kind, target));
		}

		// ----- context-section (Left/Right) mutations: atomic ↔ PhSequenceContext (legacy
		// RegRuleFormulaControl.CreateSeqCtxt / InsertContextInto / RemoveContextsFrom parity). A null OA = 0
		// cells, a simple context = 1 cell (index 0), a PhSequenceContext = its MembersRS (index per member).
		// The projector flattens these the same way, so (role,index) aligns. // PARITY: members are assumed
		// simple contexts (the projector defers iteration/nested-sequence), which is the rule-editor norm. -----

		private bool InsertIntoContext(RuleSectionRole role, int index, RuleCellKind kind, ICmObject target)
		{
			var ctx = GetContextOA(role);
			if (ctx == null)
			{
				if (index != 0)
					return false;
				var only = CreateBareContext(kind);
				if (only == null)
					return false;
				SetContextOA(role, only);   // atomic single — own it first
				SetTarget(only, target);
				return true;
			}

			var seq = ctx as IPhSequenceContext;
			if (seq == null)
			{
				// Promote the existing single context into a 2+ sequence (CreateSeqCtxt parity).
				_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(ctx);   // move out of the atomic slot
				seq = _cache.ServiceLocator.GetInstance<IPhSequenceContextFactory>().Create();
				SetContextOA(role, seq);
				seq.MembersRS.Add(ctx);
			}
			if (index < 0 || index > seq.MembersRS.Count)
				return false;
			var fresh = CreateBareContext(kind);
			if (fresh == null)
				return false;
			_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(fresh);   // own it, then reference + target
			seq.MembersRS.Insert(index, fresh);
			SetTarget(fresh, target);
			return true;
		}

		private bool DeleteFromContext(RuleSectionRole role, int index)
		{
			var ctx = GetContextOA(role);
			if (ctx is IPhSequenceContext seq)
			{
				if (index < 0 || index >= seq.MembersRS.Count)
					return false;
				var member = seq.MembersRS[index];
				member.PreRemovalSideEffects();
				_cache.LangProject.PhonologicalDataOA.ContextsOS.Remove(member);   // also drops the reference
				return true;
			}
			if (index != 0)
				return false;
			ctx.PreRemovalSideEffects();
			SetContextOA(role, null);   // owning-atomic clear deletes the single context
			return true;
		}

		private bool SetInContext(RuleSectionRole role, int index, RuleCellKind kind, ICmObject target)
		{
			var ctx = GetContextOA(role);
			if (ctx is IPhSequenceContext seq)
			{
				if (index < 0 || index >= seq.MembersRS.Count)
					return false;
				var old = seq.MembersRS[index];
				old.PreRemovalSideEffects();
				_cache.LangProject.PhonologicalDataOA.ContextsOS.Remove(old);
				var fresh = CreateBareContext(kind);
				if (fresh == null)
					return false;
				_cache.LangProject.PhonologicalDataOA.ContextsOS.Add(fresh);
				seq.MembersRS.Insert(index, fresh);
				SetTarget(fresh, target);
				return true;
			}
			if (index != 0)
				return false;
			var replacement = CreateBareContext(kind);
			if (replacement == null)
				return false;
			ctx.PreRemovalSideEffects();
			SetContextOA(role, replacement);   // owning-atomic replace deletes the old single context
			SetTarget(replacement, target);
			return true;
		}

		// ----- helpers -----

		private static bool IsContextSection(RuleSectionRole role)
			=> role == RuleSectionRole.LeftContext || role == RuleSectionRole.RightContext;

		private IPhPhonContext GetContextOA(RuleSectionRole role)
			=> role == RuleSectionRole.LeftContext ? _rhs.LeftContextOA : _rhs.RightContextOA;

		private void SetContextOA(RuleSectionRole role, IPhPhonContext value)
		{
			if (role == RuleSectionRole.LeftContext)
				_rhs.LeftContextOA = value;
			else
				_rhs.RightContextOA = value;
		}

		private ILcmOwningSequence<IPhSimpleContext> OwningSequenceFor(RuleSectionRole role)
		{
			switch (role)
			{
				case RuleSectionRole.Lhs: return _rhs.OwningRule?.StrucDescOS;
				case RuleSectionRole.Rhs: return _rhs.StrucChangeOS;
				default: return null;   // PARITY: context sections deferred to the next increment
			}
		}

		private int OwnerHvoFor(RuleSectionRole role)
			=> role == RuleSectionRole.Lhs ? _rhs.OwningRule.Hvo : _rhs.Hvo;

		private static int FlidFor(RuleSectionRole role)
			=> role == RuleSectionRole.Lhs ? PhSegmentRuleTags.kflidStrucDesc : PhSegRuleRHSTags.kflidStrucChange;

		// Create the bare simple context (no target yet). The reference is set by SetTarget AFTER the
		// context is owned by its sequence — setting a reference on an unowned object NREs (legacy order).
		private IPhSimpleContext CreateBareContext(RuleCellKind kind)
		{
			switch (kind)
			{
				case RuleCellKind.Phoneme:
					return _cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
				case RuleCellKind.Boundary:
					return _cache.ServiceLocator.GetInstance<IPhSimpleContextBdryFactory>().Create();
				case RuleCellKind.NaturalClass:
					return _cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
				default:
					return null;   // Slot/variable is not a simple context (not valid in StrucDesc/StrucChange)
			}
		}

		// Point an owned context at its (already-resolved + type-checked) target object.
		private static void SetTarget(IPhSimpleContext ctx, ICmObject target)
		{
			switch (ctx)
			{
				case IPhSimpleContextSeg seg: seg.FeatureStructureRA = (IPhPhoneme)target; break;
				case IPhSimpleContextBdry bdry: bdry.FeatureStructureRA = (IPhBdryMarker)target; break;
				case IPhSimpleContextNC nc: nc.FeatureStructureRA = (IPhNaturalClass)target; break;
			}
		}

		// Resolve + type-check the spec's target object before any mutation, so a bad spec rejects cleanly.
		private bool TryResolveTarget(RuleCellSpec spec, out ICmObject target)
		{
			target = null;
			if (spec.TargetGuid == null)
				return false;
			if (!_cache.ServiceLocator.GetInstance<ICmObjectRepository>()
					.TryGetObject(spec.TargetGuid.Value, out var obj) || obj == null)
				return false;
			switch (spec.Kind)
			{
				case RuleCellKind.Phoneme:
					if (!(obj is IPhPhoneme)) return false;
					break;
				case RuleCellKind.Boundary:
					if (!(obj is IPhBdryMarker)) return false;
					break;
				case RuleCellKind.NaturalClass:
					if (!(obj is IPhNaturalClass)) return false;
					break;
				default:
					return false;
			}
			target = obj;
			return true;
		}

		// Stage the mutation on the host's shared fenced session and commit it as ONE undo step, then
		// re-project so the grid refreshes from domain truth. A non-fenced host (a test fake) runs the
		// mutation directly (the caller supplies the UOW).
		private bool Apply(Func<bool> mutate)
		{
			bool ok;
			if (_host is RegionEditContextBase fenced)
			{
				ok = fenced.Stage(mutate, "Rule Formula");
				if (ok)
					fenced.Commit();   // commit-immediately: one undoable step per gesture (task 2.6)
			}
			else
			{
				ok = mutate();
			}
			if (ok)
				_onModelChanged?.Invoke(RuleFormulaProjector.ProjectRegularRule(_rhs));
			return ok;
		}
	}
}
