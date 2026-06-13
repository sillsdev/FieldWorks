// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Lifecycle hardening for the fenced edit session (the "Commit at wrong place" shutdown crash):
	/// an LCModel undo task left open anywhere makes every later <c>IUndoStackManager.Save()</c> —
	/// including the one FieldWorks runs at shutdown — throw. These tests pin down the failure
	/// mechanism and prove the two seams that prevent it: <see cref="RegionEditContextHolder"/>
	/// (the host never orphans an open context when re-showing a region) and the defensive
	/// <see cref="LcmRegionEditSession"/> Commit/Cancel (safe even after the clerk force-ended the
	/// task through <c>RecordClerk.SaveOnChangeRecord</c>, the LT-16673 path).
	/// </summary>
	[TestFixture]
	public class RegionEditSessionLifecycleTests : MemoryOnlyBackendProviderTestBase
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

		private LexicalEditRegionField FormField => LexicalEditRegionEditingTests.F("Form");

		private void ShutdownStyleSave()
			=> Cache.ServiceLocator.GetInstance<IUndoStackManager>().Save();

		// Characterizes the crash mechanism (the error report's stack): an undo task abandoned
		// mid-edit makes the shutdown Save throw InvalidOperationException "Commit at wrong place."
		[Test]
		public void Save_WithAnAbandonedOpenSession_ThrowsCommitAtWrongPlace()
		{
			var abandoned = new LexicalEditRegionEditContext(m_entry, Cache);
			abandoned.TrySetText(FormField, "vern", "half-typed");
			Assert.That(abandoned.IsOpen, Is.True);

			// Nobody commits or cancels; this is exactly the state FieldWorks was in at shutdown.
			Assert.That(() => ShutdownStyleSave(),
				Throws.InvalidOperationException.With.Message.Contains("Commit at wrong place"),
				"characterization: an orphaned fenced session breaks every later Save");
			// (CheckReadyForCommit rolls the abandoned task back before throwing, so the fixture
			// is clean for the next test.)
		}

		[Test]
		public void Holder_ReplacingAContextMidEdit_CancelsTheOpenSession_SoShutdownSaveSucceeds()
		{
			var holder = new RegionEditContextHolder();
			var first = new LexicalEditRegionEditContext(m_entry, Cache);
			holder.Replace(first);
			first.TrySetText(FormField, "vern", "half-typed");
			Assert.That(first.IsOpen, Is.True);

			// Re-showing the region (navigation, refresh, ShowHiddenFields, …) swaps the context.
			var second = new LexicalEditRegionEditContext(m_entry, Cache);
			holder.Replace(second);

			Assert.That(first.IsOpen, Is.False, "the displaced context's open session must be cancelled, never orphaned");
			Assert.That(Cache.ActionHandlerAccessor.CurrentDepth, Is.EqualTo(0), "no undo task may stay open");
			Assert.That(LexemeText, Is.EqualTo("casa"), "the half-typed edit rolls back");
			Assert.That(() => ShutdownStyleSave(), Throws.Nothing, "shutdown Save must succeed after a mid-edit re-show");
		}

		[Test]
		public void Holder_ClearMidEdit_CancelsTheOpenSession()
		{
			var holder = new RegionEditContextHolder();
			var context = new LexicalEditRegionEditContext(m_entry, Cache);
			holder.Replace(context);
			context.TrySetText(FormField, "vern", "half-typed");

			holder.Clear(); // dispose / non-entry record path

			Assert.That(context.IsOpen, Is.False);
			Assert.That(holder.Current, Is.Null);
			Assert.That(() => ShutdownStyleSave(), Throws.Nothing);
		}

		[Test]
		public void Holder_ReplacingWithTheSameContext_DoesNotCancelIt()
		{
			var holder = new RegionEditContextHolder();
			var context = new LexicalEditRegionEditContext(m_entry, Cache);
			holder.Replace(context);
			context.TrySetText(FormField, "vern", "still typing");

			holder.Replace(context);

			Assert.That(context.IsOpen, Is.True, "re-assigning the same context must not kill the user's open edit");
			context.Cancel();
		}

		// RecordClerk.SaveOnChangeRecord (LT-16673) force-ends any open undo task when the record
		// changes. The session must notice its task is gone instead of throwing
		// ("Rollback not supported in the current state" / unbalanced EndUndoTask).
		[Test]
		public void Cancel_AfterTheClerkForceEndedTheTask_IsASafeNoOp()
		{
			var context = new LexicalEditRegionEditContext(m_entry, Cache);
			context.TrySetText(FormField, "vern", "perro");
			Cache.ActionHandlerAccessor.EndUndoTask(); // what RecordClerk.SaveOnChangeRecord does

			Assert.That(() => context.Cancel(), Throws.Nothing,
				"cancelling a session whose task was force-ended elsewhere must not throw");
			Assert.That(Cache.ActionHandlerAccessor.CurrentDepth, Is.EqualTo(0));
			Assert.That(() => ShutdownStyleSave(), Throws.Nothing);

			// The force-ended task became a normal undo step; undo it to restore the fixture.
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(LexemeText, Is.EqualTo("casa"));
		}

		[Test]
		public void Commit_AfterTheClerkForceEndedTheTask_IsASafeNoOp()
		{
			var context = new LexicalEditRegionEditContext(m_entry, Cache);
			context.TrySetText(FormField, "vern", "perro");
			Cache.ActionHandlerAccessor.EndUndoTask(); // what RecordClerk.SaveOnChangeRecord does

			Assert.That(() => context.Commit(), Throws.Nothing,
				"committing a session whose task was force-ended elsewhere must not throw");
			Assert.That(Cache.ActionHandlerAccessor.CurrentDepth, Is.EqualTo(0));
			Assert.That(() => ShutdownStyleSave(), Throws.Nothing);

			Cache.ActionHandlerAccessor.Undo();
		}

		private string LexemeText => m_entry.LexemeFormOA.Form.get_String(Cache.DefaultVernWs).Text;
	}
}
