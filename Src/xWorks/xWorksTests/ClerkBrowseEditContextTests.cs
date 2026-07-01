// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// OpenSpec 3c/6.x close-out coverage — the browse edit context resolves the target object per cell
	/// from the field's ObjectHvo, DELEGATES the write to the proven per-entry edit context, and (the
	/// data-safety claim) commits the previous row's open edit when the user moves to a different row, so
	/// each row's edit is one independent step on the single global undo stack. Non-LexEntry / invalid /
	/// hvo-less fields are no-ops (read-only), preserving the conservative default.
	/// </summary>
	[TestFixture]
	public class ClerkBrowseEditContextTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entryA;
		private ILexEntry m_entryB;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entryA = MakeEntry("aaa");
				m_entryB = MakeEntry("bbb");
			});
		}

		private ILexEntry MakeEntry(string lexeme)
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = morph;
			morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString(lexeme, Cache.DefaultVernWs));
			return entry;
		}

		private string LexemeOf(ILexEntry entry) => entry.LexemeFormOA.Form.get_String(Cache.DefaultVernWs).Text;

		// A browse-cell field addresses its target row by ObjectHvo (the lexeme-form "Form" write path).
		private static LexicalEditRegionField FormField(int hvo) => new LexicalEditRegionField(
			$"browse/{hvo}/0", "Lexeme Form", "Form", null, RegionFieldKind.Text,
			EditorClassification.Known, "Cell", null, SurfaceRouting.Product,
			values: null, options: null, selectedOptionKey: null, isEditable: true, objectHvo: hvo);

		[Test]
		public void SwitchingRows_CommitsPriorRowEdit_AsSeparateUndoSteps()
		{
			var context = new ClerkBrowseEditContext(Cache);

			// Edit row A, then move to row B — moving rows must commit A's edit first.
			Assert.That(context.TrySetText(FormField(m_entryA.Hvo), "vern", "a2"), Is.True);
			Assert.That(context.TrySetText(FormField(m_entryB.Hvo), "vern", "b2"), Is.True,
				"editing a different row's cell retargets the delegate");
			context.Commit(); // commit row B's edit

			Assert.That(LexemeOf(m_entryA), Is.EqualTo("a2"));
			Assert.That(LexemeOf(m_entryB), Is.EqualTo("b2"));

			// Two independent undo steps — one per row — on the global action handler.
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(LexemeOf(m_entryB), Is.EqualTo("bbb"), "first undo reverts only the last row's edit");
			Assert.That(LexemeOf(m_entryA), Is.EqualTo("a2"), "the prior row's committed edit is a separate step");
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(LexemeOf(m_entryA), Is.EqualTo("aaa"), "the second undo reverts the first row's edit");
		}

		[Test]
		public void NonLexEntryOrInvalidOrHvolessField_IsNoOp()
		{
			var context = new ClerkBrowseEditContext(Cache);

			Assert.That(context.TrySetText(FormField(0), "vern", "x"), Is.False, "no object hvo = no write");
			Assert.That(context.TrySetText(FormField(999999), "vern", "x"), Is.False, "invalid hvo = no write");

			// A non-LexEntry object (the morph) is not writable through this context.
			Assert.That(context.TrySetText(FormField(m_entryA.LexemeFormOA.Hvo), "vern", "x"), Is.False,
				"a non-LexEntry object is read-only");
			Assert.That(LexemeOf(m_entryA), Is.EqualTo("aaa"), "no edit leaked to the model");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.False, "no-op edits create no undo step");
		}

		[Test]
		public void EditingTheSameRowAgainAfterCommit_IsAFreshIndependentStep()
		{
			var context = new ClerkBrowseEditContext(Cache);

			// First edit + commit of row A.
			Assert.That(context.TrySetText(FormField(m_entryA.Hvo), "vern", "a2"), Is.True);
			context.Commit();
			Assert.That(LexemeOf(m_entryA), Is.EqualTo("a2"));
			Assert.That(context.IsOpen, Is.False, "committing closes the session and resets the cached delegate");

			// Editing the SAME row again must start a clean session — not reuse the committed (closed)
			// delegate — and commit as an independent second undo step. (Regression: Commit/Cancel reset
			// _current so a re-edit of the same row never reopens an already-closed/force-ended session.)
			Assert.That(context.TrySetText(FormField(m_entryA.Hvo), "vern", "a3"), Is.True);
			context.Commit();
			Assert.That(LexemeOf(m_entryA), Is.EqualTo("a3"));

			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(LexemeOf(m_entryA), Is.EqualTo("a2"), "the re-edit is its own undo step");
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(LexemeOf(m_entryA), Is.EqualTo("aaa"), "the first edit is a separate undo step");
		}

		[Test]
		public void Cancel_DiscardsTheCurrentRowEdit()
		{
			var context = new ClerkBrowseEditContext(Cache);
			Assert.That(context.TrySetText(FormField(m_entryA.Hvo), "vern", "a2"), Is.True);
			context.Cancel();
			Assert.That(LexemeOf(m_entryA), Is.EqualTo("aaa"), "cancel rolls back the staged edit");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.False);
		}

		// ----- Phase 1 List Choice batch fence (one UOW across N rows) -----

		// A browse-cell MorphType field for a row (the List-Choice write path; set by option key = guid).
		private static LexicalEditRegionField MorphTypeField(int hvo) => new LexicalEditRegionField(
			$"browse/{hvo}/2", "Morph Type", "MorphType", null, RegionFieldKind.Chooser,
			EditorClassification.Known, "Cell", null, SurfaceRouting.Product,
			values: null, options: null, selectedOptionKey: null, isEditable: true, objectHvo: hvo);

		// A morph type from the standard list that differs from BOTH entries' current morph type, so the
		// write is observable. Picked by repository (the list is seeded by LCM) rather than by localized name.
		private IMoMorphType ADistinctMorphType()
		{
			var current = new HashSet<IMoMorphType>(new[]
			{
				m_entryA.LexemeFormOA.MorphTypeRA, m_entryB.LexemeFormOA.MorphTypeRA
			});
			var target = Cache.LangProject.LexDbOA.MorphTypesOA.ReallyReallyAllPossibilities
				.OfType<IMoMorphType>().FirstOrDefault(mt => !current.Contains(mt));
			Assert.That(target, Is.Not.Null, "need a morph type distinct from the rows' current types");
			return target;
		}

		[Test]
		public void Batch_AppliesMorphTypeAcrossRows_AsOneUndoStep()
		{
			var context = new ClerkBrowseEditContext(Cache);
			var target = ADistinctMorphType();
			var origA = m_entryA.LexemeFormOA.MorphTypeRA;
			var origB = m_entryB.LexemeFormOA.MorphTypeRA;

			// The batch fence: open ONE outer task, write both rows' MorphType, end it once.
			context.BeginBatch();
			Assert.That(context.TrySetOption(MorphTypeField(m_entryA.Hvo), target.Guid.ToString()), Is.True);
			Assert.That(context.TrySetOption(MorphTypeField(m_entryB.Hvo), target.Guid.ToString()), Is.True,
				"a second row's write retargets the per-object delegate WITHIN the open batch");
			context.EndBatch();

			Assert.That(m_entryA.LexemeFormOA.MorphTypeRA, Is.EqualTo(target), "row A got the chosen morph type");
			Assert.That(m_entryB.LexemeFormOA.MorphTypeRA, Is.EqualTo(target), "row B got the chosen morph type");

			// ONE undo step reverts BOTH rows — the parity gate (a bulk edit is a single undoable change).
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.True);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_entryA.LexemeFormOA.MorphTypeRA, Is.EqualTo(origA), "the single undo reverts row A");
			Assert.That(m_entryB.LexemeFormOA.MorphTypeRA, Is.EqualTo(origB), "the single undo reverts row B too");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.False,
				"the whole bulk edit was ONE step — there is no second step to undo");
		}

		[Test]
		public void Batch_RollsBackEverything_WhenEndedWithCommitFalse()
		{
			var context = new ClerkBrowseEditContext(Cache);
			var target = ADistinctMorphType();
			var origA = m_entryA.LexemeFormOA.MorphTypeRA;

			context.BeginBatch();
			Assert.That(context.TrySetOption(MorphTypeField(m_entryA.Hvo), target.Guid.ToString()), Is.True);
			context.EndBatch(commit: false);

			Assert.That(m_entryA.LexemeFormOA.MorphTypeRA, Is.EqualTo(origA), "an aborted batch leaves the model untouched");
			Assert.That(Cache.ActionHandlerAccessor.CanUndo(), Is.False, "an aborted batch creates no undo step");
		}
	}
}
