// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The cross-surface refresh propagation gate, Avalonia side only (task 3.15). Both surfaces
	/// share one LCModel cache, so consistency stands on the <c>PropChanged</c> notification loop:
	/// this controller subscribes to the real <see cref="ISilDataAccess"/> notification bus and asks
	/// the host to re-resolve/re-show the Avalonia region whenever a change lands inside the entry
	/// the surface is displaying — whether it came from a legacy surface, F5/RefreshAllViews-driven
	/// reloads, or any other writer. While the surface's own edit session is open, refreshes are
	/// gated through an <see cref="ILexicalRefreshCoordinator"/> (suspend/pending, the LT-22414
	/// model) and delivered once on edit completion, so a half-typed edit is never stomped.
	///
	/// Delivery is coalesced through the host's <c>schedule</c> delegate (review round 1): one
	/// committed undo task or external bulk edit raises one PropChanged per changed property, and
	/// recomposing the region synchronously per notification both froze the UI on bursts and
	/// reentrantly tore down the view while Commit/Cancel were still on the stack. With a scheduler
	/// a burst becomes ONE queued refresh that runs after the current call stack unwinds; without
	/// one (tests, simple hosts) delivery stays synchronous.
	/// </summary>
	public sealed class AvaloniaRegionRefreshController : IVwNotifyChange, IDisposable
	{
		private readonly LcmCache _cache;
		private readonly Func<ICmObject> _currentRecord;
		private readonly Func<bool> _isEditing;
		private readonly Action _refresh;
		private readonly ILexicalRefreshCoordinator _coordinator;
		private readonly Action<Action> _schedule;
		private bool _refreshQueued;
		private bool _disposed;

		public AvaloniaRegionRefreshController(
			LcmCache cache,
			Func<ICmObject> currentRecord,
			Func<bool> isEditing,
			Action refresh,
			ILexicalRefreshCoordinator coordinator,
			Action<Action> schedule = null)
		{
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_currentRecord = currentRecord ?? throw new ArgumentNullException(nameof(currentRecord));
			_isEditing = isEditing ?? throw new ArgumentNullException(nameof(isEditing));
			_refresh = refresh ?? throw new ArgumentNullException(nameof(refresh));
			_coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
			_schedule = schedule;
			cache.DomainDataByFlid.AddNotification(this);
		}

		/// <inheritdoc />
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			// A queued refresh recomposes from CURRENT domain state, so further notifications are
			// already covered — skip even the relevance walk (PropChanged fires app-wide).
			if (_disposed || _refreshQueued || !IsRelevant(hvo))
				return;

			if (_isEditing())
			{
				// The surface's own session is writing (or an external edit raced it): hold the
				// refresh until the edit completes rather than stomping in-progress input.
				if (!_coordinator.IsSuspended)
					_coordinator.BeginSuspend();
				_coordinator.RequestRefresh();
				return;
			}

			if (_coordinator.IsSuspended)
			{
				// Editing ended between notifications; deliver the pending refresh (covers it) now.
				if (_coordinator.EndSuspend())
					ScheduleRefresh();
				return;
			}

			if (_coordinator.RequestRefresh())
				ScheduleRefresh();
		}

		/// <summary>
		/// Called by the host when its edit session committed or cancelled: delivers any refresh that
		/// was held while editing.
		/// </summary>
		public void NotifyEditCompleted()
		{
			if (_disposed)
				return;
			if (_coordinator.IsSuspended && _coordinator.EndSuspend())
				ScheduleRefresh();
		}

		/// <summary>
		/// Host-initiated refresh (e.g. after a commit/cancel completed) routed through the SAME
		/// coalesced, editing-aware queue as PropChanged deliveries, so a completion plus a
		/// notification burst still recomposes exactly once.
		/// </summary>
		public void RequestRefresh()
		{
			if (_disposed)
				return;
			ScheduleRefresh();
		}

		/// <summary>
		/// Called by the host when it is about to re-show the region itself anyway: drops any held
		/// delivery so completion does not double the recompose.
		/// </summary>
		public void DiscardHeldRefresh()
		{
			if (_disposed)
				return;
			if (_coordinator.IsSuspended)
				_coordinator.EndSuspend();
		}

		// Coalesce: one queued delivery covers the whole burst. The runner re-checks state because
		// the world can change between queueing and running (host disposed; user started typing —
		// then the refresh converts back into a held delivery instead of stomping the edit).
		private void ScheduleRefresh()
		{
			if (_refreshQueued)
				return;
			_refreshQueued = true;
			void Runner()
			{
				// _refreshQueued stays true UNTIL the refresh completes: a rebuild can itself raise
				// PropChanged (e.g. a settle-commit inside it), and those notifications are already
				// covered — the recompose reads current domain state — so they must coalesce into
				// this delivery instead of queueing a second identical one (review round 2).
				try
				{
					if (_disposed)
						return;
					if (_isEditing())
					{
						if (!_coordinator.IsSuspended)
							_coordinator.BeginSuspend();
						_coordinator.RequestRefresh();
						return;
					}
					_refresh();
				}
				finally
				{
					_refreshQueued = false;
				}
			}

			if (_schedule != null)
			{
				try
				{
					_schedule(Runner);
				}
				catch
				{
					// If the host scheduler rejects the work, the runner will never fire; leaving
					// the flag set would wedge the queue (no refresh could ever be scheduled again).
					_refreshQueued = false;
					throw;
				}
			}
			else
			{
				Runner();
			}
		}

		// A change is relevant when the changed object is, or is owned by, the entry on display.
		private bool IsRelevant(int hvo)
		{
			var current = _currentRecord();
			if (current == null)
				return false;
			if (hvo == current.Hvo)
				return true;

			if (!_cache.ServiceLocator.ObjectRepository.TryGetObject(hvo, out var changed))
				return false;
			var owningEntry = changed as ILexEntry ?? changed.OwnerOfClass<ILexEntry>();
			return owningEntry != null && owningEntry.Hvo == current.Hvo;
		}

		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;
			_cache.DomainDataByFlid.RemoveNotification(this);
		}
	}
}
