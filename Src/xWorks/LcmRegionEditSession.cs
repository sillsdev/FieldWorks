// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The product fenced edit session (tasks 6.8/6.10, per the `avalonia-edit-sessions` and
	/// `avalonia-undo-redo` seam specs): one LCModel undo task spanning the user's edit, applied
	/// directly to the domain. <see cref="Commit"/> ends the task — every staged field edit becomes
	/// ONE step on the single global LCModel action-handler stack legacy surfaces share, so Ctrl+Z
	/// works across frameworks in both directions by construction. <see cref="Cancel"/> rolls the
	/// whole task back to the depth captured at open (the same pattern legacy composition editing
	/// uses, IbusRootSiteEventHandler). Idempotent: a second Commit/Cancel is a no-op.
	/// </summary>
	public sealed class LcmRegionEditSession : IEditSession
	{
		private readonly LcmCache _cache;
		private readonly int _depth;

		public LcmRegionEditSession(LcmCache cache, string undoLabel, string redoLabel)
		{
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_depth = cache.ActionHandlerAccessor.CurrentDepth;
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
			if (TaskStillOpen)
				_cache.DomainDataByFlid.EndUndoTask();
		}

		/// <inheritdoc />
		public void Cancel()
		{
			if (!IsOpen)
				return;
			IsOpen = false;
			if (TaskStillOpen)
				_cache.ActionHandlerAccessor.Rollback(_depth);
		}

		// RecordClerk.SaveOnChangeRecord (LT-16673) force-ends any open undo task on record change,
		// closing this session's task underneath it. Ending or rolling back again would throw
		// ("Rollback not supported in the current state"), so both closers check first.
		private bool TaskStillOpen => _cache.ActionHandlerAccessor.CurrentDepth > _depth;
	}
}
