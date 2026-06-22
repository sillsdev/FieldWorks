// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Review round-1 hardening of the fenced edit session against the rest of the app:
	/// (1) global Undo/Redo while a session is open used to throw LockRecursionException
	/// (UndoStack.Undo re-enters the non-recursive UOW write lock the open task already holds) —
	/// the holder's undo guard settles the session and converts the gesture into "close the
	/// pending edit"; (2) Settle is the one auto-save policy (commit when valid, cancel when not)
	/// shared by every host path (navigation, go-away, undo, dispose); (3) the refresh controller
	/// coalesces PropChanged bursts through a host scheduler instead of recomposing the region
	/// synchronously once per notification.
	/// </summary>
	[TestFixture]
	public class RegionEditGuardAndSchedulingTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				m_entry.SensesOS.Add(sense);
			});
		}

		private string LexemeText => m_entry.LexemeFormOA.Form.get_String(Cache.DefaultVernWs).Text;

		private LexicalEditRegionEditContext OpenSessionWith(string text)
		{
			var context = new LexicalEditRegionEditContext(m_entry, Cache);
			Assert.That(context.TrySetText(LexicalEditRegionEditingTests.F("Form"), "vern", text), Is.True);
			Assert.That(context.IsOpen, Is.True);
			return context;
		}

		[Test]
		public void Settle_CommitsAValidOpenSession_AsOneUndoStep()
		{
			var holder = new RegionEditContextHolder();
			holder.Replace(OpenSessionWith("perro"));

			holder.Settle();

			Assert.That(holder.Current.IsOpen, Is.False);
			Assert.That(LexemeText, Is.EqualTo("perro"), "a valid pending edit is saved, not discarded");
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(LexemeText, Is.EqualTo("casa"), "the settle is one normal undo step");
		}

		[Test]
		public void Settle_CancelsAnInvalidOpenSession()
		{
			var holder = new RegionEditContextHolder();
			holder.Replace(OpenSessionWith("")); // empties the lexeme form -> validation error

			holder.Settle();

			Assert.That(holder.Current.IsOpen, Is.False);
			Assert.That(LexemeText, Is.EqualTo("casa"), "invalid staged state rolls back instead of committing");
			Assert.That(Cache.ActionHandlerAccessor.CurrentDepth, Is.EqualTo(0));
		}

		// ITEM 2 (invalid-edit-on-navigate UX): a Settle that rolls back because validation FAILED must
		// SURFACE the reason (returned + the InvalidEditRolledBack hook fires), not silently discard the
		// edit. The data is still rolled back safely; only the silence is fixed.
		[Test]
		public void Settle_InvalidOpenSession_SurfacesTheValidationReason_NotASilentRollback()
		{
			var holder = new RegionEditContextHolder();
			IReadOnlyList<string> surfaced = null;
			holder.InvalidEditRolledBack = reasons => surfaced = reasons;
			holder.Replace(OpenSessionWith("")); // clears the required lexeme form -> validation fails

			var returned = holder.Settle();

			Assert.That(returned, Is.Not.Empty, "Settle returns the reasons it rolled back on");
			Assert.That(returned, Contains.Item(
				SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaStrings.LexemeFormRequired));
			Assert.That(surfaced, Is.EqualTo(returned),
				"the host hook fires with the same reasons so the user is told, not left guessing");
			Assert.That(holder.Current.IsOpen, Is.False, "the edit is still rolled back (the safe close)");
			Assert.That(LexemeText, Is.EqualTo("casa"), "the invalid edit is discarded, but not silently");
		}

		// ITEM 2: a clean commit and a nothing-to-settle no-op must NOT fire the warning hook.
		[Test]
		public void Settle_ValidCommitOrNoOp_DoesNotSurfaceAnyValidationReason()
		{
			var fired = 0;
			var holder = new RegionEditContextHolder();
			holder.InvalidEditRolledBack = _ => fired++;

			Assert.That(holder.Settle(), Is.Empty, "nothing open settles to no reasons");

			holder.Replace(OpenSessionWith("perro")); // a VALID edit
			Assert.That(holder.Settle(), Is.Empty, "a clean commit reports no validation reasons");
			Assert.That(fired, Is.EqualTo(0), "the warning hook never fires for a clean settle");
			Assert.That(LexemeText, Is.EqualTo("perro"));
		}

		// Each undo test needs a PRIOR committed bundle (CanUndo requires one) and must never leak
		// an open session into the shared fixture cache, hence the try/finally shape.
		private void WithPriorUndoStepAndOpenSession(Action<LexicalEditRegionEditContext> test)
		{
			UndoableUnitOfWorkHelper.Do("Undo seed", "Redo seed", Cache.ActionHandlerAccessor, () =>
				m_entry.CitationForm.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("seed", Cache.DefaultVernWs)));
			var context = OpenSessionWith("perro");
			try
			{
				test(context);
			}
			finally
			{
				if (context.IsOpen)
					context.Cancel();
			}
		}

		// Characterization of the bug the guard exists for: LCModel's UndoStack.Undo() enters the
		// non-recursive UOW write lock, which the open fenced task's thread already holds.
		[Test]
		public void Undo_WhileASessionIsOpen_WithoutTheGuard_Throws()
		{
			WithPriorUndoStepAndOpenSession(context =>
			{
				Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True,
					"precondition: LCModel reports undo available even while a task is open");
				Assert.That(() => Cache.ActionHandlerAccessor.Undo(),
					Throws.InstanceOf<LockRecursionException>(),
					"characterization: Edit > Undo mid-edit crashes without the guard");
			});
		}

		[Test]
		public void UndoGuard_UndoWhileSessionOpen_SettlesThePendingEditInsteadOfThrowing()
		{
			WithPriorUndoStepAndOpenSession(context =>
			{
				var holder = new RegionEditContextHolder();
				holder.AttachUndoGuard(Cache.ActionHandlerAccessor);
				try
				{
					holder.Replace(context);

					Assert.That(() => Cache.ActionHandlerAccessor.Undo(), Throws.Nothing,
						"the guard must intercept the undo before it re-enters the write lock");
					Assert.That(Cache.ActionHandlerAccessor.CurrentDepth, Is.EqualTo(0), "the session settled");
					Assert.That(LexemeText, Is.EqualTo("perro"),
						"the first undo gesture closes the pending edit (it does not also undo it)");

					Cache.ActionHandlerAccessor.Undo();
					Assert.That(LexemeText, Is.EqualTo("casa"), "the next undo reverts the settled edit");
				}
				finally
				{
					holder.DetachUndoGuard();
				}
			});
		}

		[Test]
		public void UndoGuard_Detached_StopsIntercepting()
		{
			WithPriorUndoStepAndOpenSession(context =>
			{
				var holder = new RegionEditContextHolder();
				holder.AttachUndoGuard(Cache.ActionHandlerAccessor);
				holder.DetachUndoGuard();
				holder.Replace(context);

				Assert.That(() => Cache.ActionHandlerAccessor.Undo(),
					Throws.InstanceOf<LockRecursionException>(),
					"after detach the guard must not linger on the action handler");
			});
		}

		// The lexical host's relevance predicate, exactly as RecordEditView injects it.
		private Func<ICmObject, bool> LexicalRelevance => changed =>
			RecordEditView.IsChangeWithinEntry(changed, m_entry);

		[Test]
		public void RefreshController_WithAScheduler_CoalescesABurstIntoOneRefresh()
		{
			var scheduled = new List<Action>();
			var refreshes = 0;
			using (new AvaloniaRegionRefreshController(
				Cache, () => m_entry, () => false, () => refreshes++, new RefreshCoordinator(),
				schedule: scheduled.Add, isRelevant: LexicalRelevance))
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				{
					m_entry.CitationForm.set_String(Cache.DefaultVernWs,
						TsStringUtils.MakeString("one", Cache.DefaultVernWs));
					m_entry.Bibliography.set_String(Cache.DefaultAnalWs,
						TsStringUtils.MakeString("two", Cache.DefaultAnalWs));
					m_entry.SensesOS[0].Gloss.set_String(Cache.DefaultAnalWs,
						TsStringUtils.MakeString("three", Cache.DefaultAnalWs));
				});

				Assert.That(scheduled.Count, Is.EqualTo(1),
					"a burst of PropChanged notifications coalesces into one scheduled refresh");
				Assert.That(refreshes, Is.EqualTo(0), "nothing runs until the host's scheduler fires");

				scheduled[0]();
				Assert.That(refreshes, Is.EqualTo(1));

				// The next change after the flush schedules a fresh refresh.
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					m_entry.CitationForm.set_String(Cache.DefaultVernWs,
						TsStringUtils.MakeString("four", Cache.DefaultVernWs)));
				Assert.That(scheduled.Count, Is.EqualTo(2));
			}
		}

		// Review round 2: a rebuild can itself raise PropChanged (a settle-commit inside it). Those
		// changes are already covered — the recompose reads current domain state — so they must
		// coalesce into the running delivery, not queue a second identical recompose.
		[Test]
		public void RefreshController_ChangeRaisedDuringTheRebuild_CoalescesIntoIt()
		{
			var scheduled = new List<Action>();
			var refreshes = 0;
			AvaloniaRegionRefreshController controller = null;
			void Refresh()
			{
				refreshes++;
				if (refreshes == 1)
				{
					// Simulate a settle-commit inside the rebuild: a domain write raising PropChanged
					// while _refresh() is still on the stack.
					NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
						m_entry.CitationForm.set_String(Cache.DefaultVernWs,
							TsStringUtils.MakeString("inside", Cache.DefaultVernWs)));
				}
			}
			using (controller = new AvaloniaRegionRefreshController(
				Cache, () => m_entry, () => false, Refresh, new RefreshCoordinator(),
				schedule: scheduled.Add))
			{
				controller.RequestRefresh();
				Assert.That(scheduled.Count, Is.EqualTo(1));

				scheduled[0]();
				Assert.That(refreshes, Is.EqualTo(1));
				Assert.That(scheduled.Count, Is.EqualTo(1),
					"a change raised DURING the rebuild must not re-queue a second identical recompose");

				// And the queue is open again afterwards.
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					m_entry.CitationForm.set_String(Cache.DefaultVernWs,
						TsStringUtils.MakeString("after", Cache.DefaultVernWs)));
				Assert.That(scheduled.Count, Is.EqualTo(2));
			}
		}

		[Test]
		public void RefreshController_RefreshThrows_QueueDoesNotWedge()
		{
			var scheduled = new List<Action>();
			var refreshes = 0;
			void Refresh()
			{
				refreshes++;
				if (refreshes == 1)
					throw new InvalidOperationException("rebuild failed");
			}
			using (new AvaloniaRegionRefreshController(
				Cache, () => m_entry, () => false, Refresh, new RefreshCoordinator(),
				schedule: scheduled.Add))
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					m_entry.CitationForm.set_String(Cache.DefaultVernWs,
						TsStringUtils.MakeString("boom", Cache.DefaultVernWs)));
				Assert.That(scheduled.Count, Is.EqualTo(1));
				Assert.That(() => scheduled[0](), Throws.InstanceOf<InvalidOperationException>());

				// The flag reset in finally: the next change schedules a fresh refresh.
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					m_entry.CitationForm.set_String(Cache.DefaultVernWs,
						TsStringUtils.MakeString("again", Cache.DefaultVernWs)));
				Assert.That(scheduled.Count, Is.EqualTo(2));
				scheduled[1]();
				Assert.That(refreshes, Is.EqualTo(2));
			}
		}

		[Test]
		public void RefreshController_SchedulerThrows_QueueDoesNotWedge()
		{
			var schedulerThrows = true;
			var scheduled = new List<Action>();
			var refreshes = 0;
			void Schedule(Action runner)
			{
				if (schedulerThrows)
					throw new ObjectDisposedException("host scheduler gone");
				scheduled.Add(runner);
			}
			using (var controller = new AvaloniaRegionRefreshController(
				Cache, () => m_entry, () => false, () => refreshes++, new RefreshCoordinator(),
				schedule: Schedule))
			{
				Assert.That(() => controller.RequestRefresh(), Throws.InstanceOf<ObjectDisposedException>());

				schedulerThrows = false;
				controller.RequestRefresh();
				Assert.That(scheduled.Count, Is.EqualTo(1), "a failed schedule must not wedge the queue");
				scheduled[0]();
				Assert.That(refreshes, Is.EqualTo(1));
			}
		}

		[Test]
		public void RefreshController_DiscardHeldRefresh_DropsTheHeldDeliveryWithoutRefreshing()
		{
			var editing = true;
			var refreshes = 0;
			using (var controller = new AvaloniaRegionRefreshController(
				Cache, () => m_entry, () => editing, () => refreshes++, new RefreshCoordinator()))
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					m_entry.CitationForm.set_String(Cache.DefaultVernWs,
						TsStringUtils.MakeString("raced", Cache.DefaultVernWs)));

				editing = false;
				controller.DiscardHeldRefresh();
				Assert.That(refreshes, Is.EqualTo(0),
					"the host discards the held delivery when it is about to re-show anyway");

				// The completion pair the host actually runs (OnAvaloniaRegionEditCompleted):
				// discard + ONE explicit request — one recompose, not the held one plus its own.
				controller.RequestRefresh();
				Assert.That(refreshes, Is.EqualTo(1),
					"after the discard exactly the one requested re-show runs (nothing was left pending)");
			}
		}

		// The held-while-editing scenario through the surviving API: when editing ends, the next
		// relevant notification delivers the refresh that was held during the edit (the host's
		// explicit completion path is the DiscardHeldRefresh + RequestRefresh pair, above).
		// A UOW raises one PropChanged per changed property, so the single-delivery guarantee
		// stands on the coalescing scheduler the production host supplies — model it here with a
		// queue that runs after the notification burst, exactly like the host's BeginInvoke.
		[Test]
		public void RefreshController_HeldRefresh_DeliversOnTheNextNotificationAfterEditingEnds()
		{
			var editing = true;
			var refreshes = 0;
			var queued = new List<Action>();
			using (new AvaloniaRegionRefreshController(
				Cache, () => m_entry, () => editing, () => refreshes++, new RefreshCoordinator(),
				schedule: a => queued.Add(a)))
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					m_entry.CitationForm.set_String(Cache.DefaultVernWs,
						TsStringUtils.MakeString("raced", Cache.DefaultVernWs)));
				Assert.That(refreshes, Is.EqualTo(0), "held while the surface's own session is open");
				Assert.That(queued, Is.Empty, "a held refresh is pending, not scheduled");

				editing = false;
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					m_entry.CitationForm.set_String(Cache.DefaultVernWs,
						TsStringUtils.MakeString("after", Cache.DefaultVernWs)));
				Assert.That(queued, Has.Count.EqualTo(1),
					"the whole notification burst coalesces into one scheduled delivery");

				queued[0]();
				Assert.That(refreshes, Is.EqualTo(1),
					"one delivery covers both the held refresh and the new change");
			}
		}

		// Relevance is injected by the host, not hard-coded to ILexEntry inside the controller.
		[Test]
		public void RefreshController_HostPredicate_CoversObjectsOwnedByTheDisplayedEntry()
		{
			var refreshes = 0;
			using (new AvaloniaRegionRefreshController(
				Cache, () => m_entry, () => false, () => refreshes++, new RefreshCoordinator(),
				isRelevant: LexicalRelevance))
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					m_entry.SensesOS[0].Gloss.set_String(Cache.DefaultAnalWs,
						TsStringUtils.MakeString("sense-level", Cache.DefaultAnalWs)));

				Assert.That(refreshes, Is.GreaterThanOrEqualTo(1),
					"a change to an object OWNED by the displayed entry reaches the surface through the host's predicate");
			}
		}

		[Test]
		public void RefreshController_HostPredicate_DecidesRelevanceBeyondTheDisplayedRecord()
		{
			ILexEntry other = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				other = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create());

			var refreshes = 0;
			using (new AvaloniaRegionRefreshController(
				Cache, () => m_entry, () => false, () => refreshes++, new RefreshCoordinator(),
				isRelevant: changed => true))
			{
				NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
					other.CitationForm.set_String(Cache.DefaultVernWs,
						TsStringUtils.MakeString("unrelated", Cache.DefaultVernWs)));

				Assert.That(refreshes, Is.GreaterThanOrEqualTo(1),
					"the injected predicate — not a built-in entry walk — decides relevance for other objects");
			}
		}

		// The predicate RecordEditView injects: containment in the displayed entry via the owner
		// chain, the behavior the controller used to hard-code.
		[Test]
		public void IsChangeWithinEntry_WalksTheOwnerChainToTheDisplayedEntry()
		{
			Assert.That(RecordEditView.IsChangeWithinEntry(m_entry, m_entry), Is.True,
				"the entry itself");
			Assert.That(RecordEditView.IsChangeWithinEntry(m_entry.SensesOS[0], m_entry), Is.True,
				"a sense owned by the entry");
			Assert.That(RecordEditView.IsChangeWithinEntry(m_entry.LexemeFormOA, m_entry), Is.True,
				"the lexeme form owned by the entry");
			Assert.That(RecordEditView.IsChangeWithinEntry(Cache.LangProject, m_entry), Is.False,
				"an object outside the entry");
			Assert.That(RecordEditView.IsChangeWithinEntry(null, m_entry), Is.False);
			Assert.That(RecordEditView.IsChangeWithinEntry(m_entry, null), Is.False);
		}
	}
}
