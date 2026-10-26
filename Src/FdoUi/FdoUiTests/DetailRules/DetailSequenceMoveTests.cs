// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The Move Up / Move Down rule of a detail row: where it applies, the ends of the
	/// sequence, the complex-form slot of an entry's EntryRefs, and the moves themselves.
	/// </summary>
	[TestFixture]
	public class DetailSequenceMoveTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry _entry;
		private ILexPronunciation _first;
		private ILexPronunciation _middle;
		private ILexPronunciation _last;
		private ILexEntryRef _complexForm;
		private ILexEntryRef _variant1;
		private ILexEntryRef _variant2;

		// An entry with a lexeme form, three pronunciations and three entry refs: a complex
		// form first, then two variants.
		[SetUp]
		public void MakeEntry()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				_entry.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				var pronunciations = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>();
				_entry.PronunciationsOS.Add(_first = pronunciations.Create());
				_entry.PronunciationsOS.Add(_middle = pronunciations.Create());
				_entry.PronunciationsOS.Add(_last = pronunciations.Create());
				var refs = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>();
				_entry.EntryRefsOS.Add(_complexForm = refs.Create());
				_complexForm.RefType = LexEntryRefTags.krtComplexForm;
				_complexForm.ComplexEntryTypesRS.Add(Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>()
					.GetObject(LexEntryTypeTags.kguidLexTypCompound));
				_entry.EntryRefsOS.Add(_variant1 = refs.Create());
				_variant1.RefType = LexEntryRefTags.krtVariant;
				_entry.EntryRefsOS.Add(_variant2 = refs.Create());
				_variant2.RefType = LexEntryRefTags.krtVariant;
			});
		}

		[TearDown]
		public void DeleteEntry()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				if (_entry.IsValidObject)
					_entry.Delete();
			});
		}

		[Test]
		public void IsInSequence_OnlyForAnObjectInAnOwningSequence()
		{
			Assert.That(DetailSequenceMove.IsInSequence(_middle), Is.True);
			Assert.That(DetailSequenceMove.IsInSequence(_entry), Is.False, "unowned");
			Assert.That(DetailSequenceMove.IsInSequence(_entry.LexemeFormOA), Is.False, "an atomic field");
			Assert.That(DetailSequenceMove.IsInSequence(null), Is.False);
		}

		[Test]
		public void CanMoveUp_AndDown_StopAtTheEnds()
		{
			Assert.That(DetailSequenceMove.CanMoveUp(_first), Is.False);
			Assert.That(DetailSequenceMove.CanMoveUp(_middle), Is.True);
			Assert.That(DetailSequenceMove.CanMoveUp(_last), Is.True);
			Assert.That(DetailSequenceMove.CanMoveDown(_first), Is.True);
			Assert.That(DetailSequenceMove.CanMoveDown(_middle), Is.True);
			Assert.That(DetailSequenceMove.CanMoveDown(_last), Is.False);
			Assert.That(DetailSequenceMove.CanMoveUp(_entry), Is.False, "not in a sequence");
			Assert.That(DetailSequenceMove.CanMoveDown(_entry), Is.False);
		}

		// The first EntryRefs slot is the complex form's: the second ref cannot move up while
		// the first is a complex form, and can once it is not.
		[Test]
		public void CanMoveUp_InEntryRefs_KeepsTheComplexFormFirst()
		{
			Assert.That(DetailSequenceMove.CanMoveUp(_variant1), Is.False, "it would displace the complex form");
			Assert.That(DetailSequenceMove.CanMoveUp(_variant2), Is.True, "the third ref only moves to second");
			Assert.That(DetailSequenceMove.CanMoveDown(_complexForm), Is.True, "the rule covers Move Up only, as before");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => _complexForm.ComplexEntryTypesRS.Clear());

			Assert.That(DetailSequenceMove.CanMoveUp(_variant1), Is.True, "a first ref that is no complex form reserves nothing");
		}

		[Test]
		public void MoveUp_AndMoveDown_SwapNeighbours_InOneUndoableStep()
		{
			Assert.That(DetailSequenceMove.MoveUp(_last, "undo", "redo"), Is.True);
			Assert.That(_entry.PronunciationsOS.ToList(), Is.EqualTo(new[] { _first, _last, _middle }));
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo("undo"));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_entry.PronunciationsOS.ToList(), Is.EqualTo(new[] { _first, _middle, _last }), "one Undo restores the order");

			Assert.That(DetailSequenceMove.MoveDown(_first, "undo", "redo"), Is.True);
			Assert.That(_entry.PronunciationsOS.ToList(), Is.EqualTo(new[] { _middle, _first, _last }));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_entry.PronunciationsOS.ToList(), Is.EqualTo(new[] { _first, _middle, _last }));
		}

		[Test]
		public void MoveUp_AndMoveDown_DoNothing_WhereTheRuleRefuses()
		{
			Assert.That(DetailSequenceMove.MoveUp(_first, "undo", "redo"), Is.False);
			Assert.That(DetailSequenceMove.MoveDown(_last, "undo", "redo"), Is.False);
			Assert.That(DetailSequenceMove.MoveUp(_variant1, "undo", "redo"), Is.False, "the complex form keeps its slot");
			Assert.That(DetailSequenceMove.MoveUp(_entry, "undo", "redo"), Is.False);
			Assert.That(_entry.PronunciationsOS.ToList(), Is.EqualTo(new[] { _first, _middle, _last }));
			Assert.That(_entry.EntryRefsOS.ToList(), Is.EqualTo(new[] { _complexForm, _variant1, _variant2 }));
		}
	}
}
