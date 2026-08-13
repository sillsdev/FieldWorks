// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The product fenced edit session: one LCModel undo task spanning the user's edit, applied
	/// directly to the domain. <see cref="Commit"/> ends the task -- every staged field edit becomes
	/// ONE step on the single global LCModel action-handler stack legacy views share, so Ctrl+Z
	/// works across frameworks in both directions by construction. <see cref="Cancel"/> rolls the
	/// whole task back to the depth captured at open (the same pattern legacy composition editing
	/// uses, IbusRootSiteEventHandler). Idempotent: a second Commit/Cancel is a no-op.
	/// </summary>
	public sealed class LcmDetailEditSession : IEditSession
	{
		private readonly LcmCache _cache;
		private readonly int _depth;
		// Identity of the undo stack at open: if the clerk force-ends this session's task, the
		// sequence count advances, so a depth match alone no longer proves the OPEN task is ours.
		private readonly int _undoableSequenceCountAtOpen;
		// Since the action handler does not support nested BeginUndoTask, when a
		// task is already open, this session joins it; Commit/Cancel become
		// no-ops, mirroring RootSiteEditingHelper's CurrentDepth==0 rule.
		private readonly bool _ownsTask;

		public LcmDetailEditSession(LcmCache cache, string undoLabel, string redoLabel)
		{
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_depth = cache.ActionHandlerAccessor.CurrentDepth;
			_undoableSequenceCountAtOpen = cache.ActionHandlerAccessor.UndoableSequenceCount;
			_ownsTask = _depth == 0;
			if (_ownsTask)
				cache.DomainDataByFlid.BeginUndoTask(undoLabel, redoLabel);
			IsOpen = true;
		}

		/// <inheritdoc />
		public bool IsOpen { get; private set; }

		/// <inheritdoc />
		public void Commit()
		{
			if (!IsOpen)
				return;
			IsOpen = false;
			// A joined (non-owning) session leaves the outer task open for its owner to end as one step.
			if (_ownsTask && TaskStillOpen)
				_cache.DomainDataByFlid.EndUndoTask();
		}

		/// <inheritdoc />
		public void Cancel()
		{
			if (!IsOpen)
				return;
			IsOpen = false;
			// A joined session never rolls back the outer task -- only its owner does, on a batch abort.
			if (_ownsTask && TaskStillOpen)
				_cache.ActionHandlerAccessor.Rollback(_depth);
		}

		// RecordClerk.SaveOnChangeRecord (LT-16673) force-ends any open undo task on record change,
		// closing this session's task underneath it. Ending or rolling back again would throw
		// ("Rollback not supported in the current state") -- and if ANOTHER task has opened since,
		// a depth check alone would end/roll back that unrelated task. The sequence count advances
		// when our task is force-ended, so both conditions together identify the open task as ours.
		// (A force-Rollback by a third party leaves the count unchanged and stays undetected; that
		// is not the LT-16673 path.)
		private bool TaskStillOpen =>
			_cache.ActionHandlerAccessor.CurrentDepth > _depth
			&& _cache.ActionHandlerAccessor.UndoableSequenceCount == _undoableSequenceCountAtOpen;
	}
}
