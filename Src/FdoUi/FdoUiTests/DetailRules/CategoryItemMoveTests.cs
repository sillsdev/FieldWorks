// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The Move rule of an affix slot and an affix template: which category holds one, what
	/// follows it to another category, and that each move is one undoable step.
	/// </summary>
	[TestFixture]
	public class CategoryItemMoveTests : MemoryOnlyBackendProviderTestBase
	{
		private const string Undo = "Undo Move";
		private const string Redo = "Redo Move";

		private IPartOfSpeech _verb;
		private IPartOfSpeech _irregular;
		private IPartOfSpeech _noun;
		private IMoInflAffixSlot _slot;
		private IMoInflAffixTemplate _template;
		private ILexEntry _entry;
		private IMoInflAffMsa _affix;

		// Verb with the subcategory Irregular, and Noun. Irregular owns the slot Tense, a
		// template that uses it as a prefix slot, and is the category of one affix in the slot.
		[SetUp]
		public void MakeCategories()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var services = Cache.ServiceLocator;
				var categories = services.GetInstance<IPartOfSpeechFactory>();
				var list = Cache.LangProject.PartsOfSpeechOA;
				list.PossibilitiesOS.Add(_verb = categories.Create());
				_verb.SubPossibilitiesOS.Add(_irregular = categories.Create());
				list.PossibilitiesOS.Add(_noun = categories.Create());
				_irregular.AffixSlotsOC.Add(_slot = services.GetInstance<IMoInflAffixSlotFactory>().Create());
				_slot.Name.SetAnalysisDefaultWritingSystem("Tense");
				_irregular.AffixTemplatesOS.Add(_template = services.GetInstance<IMoInflAffixTemplateFactory>().Create());
				_template.PrefixSlotsRS.Add(_slot);
				_entry = services.GetInstance<ILexEntryFactory>().Create();
				_entry.MorphoSyntaxAnalysesOC.Add(_affix = services.GetInstance<IMoInflAffMsaFactory>().Create());
				_affix.PartOfSpeechRA = _irregular;
				_affix.SlotsRC.Add(_slot);
			});
		}

		[TearDown]
		public void DeleteCategories()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				foreach (var obj in new ICmObject[] { _entry, _verb, _noun })
				{
					if (obj.IsValidObject)
						obj.Delete();
				}
			});
		}

		[Test]
		public void CategoryOf_IsTheOwnerOfASlotOrATemplate_AndNullForAnythingElse()
		{
			Assert.That(CategoryItemMove.CategoryOf(_slot), Is.SameAs(_irregular));
			Assert.That(CategoryItemMove.CategoryOf(_template), Is.SameAs(_irregular));
			Assert.That(CategoryItemMove.CategoryOf(_irregular), Is.Null, "a category is no slot or template");
			Assert.That(CategoryItemMove.CategoryOf(null), Is.Null);
		}

		[Test]
		public void Move_ASlot_PutsItInTheCategory_WithItsAffixes_InOneUndoableStep()
		{
			Assert.That(CategoryItemMove.Move(_slot, _noun, Undo, Redo), Is.True);

			Assert.That(_slot.Owner, Is.SameAs(_noun));
			Assert.That(_affix.PartOfSpeechRA, Is.SameAs(_noun), "the slot's affix takes the new category");
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_slot.Owner, Is.SameAs(_irregular), "one Undo");
			Assert.That(_affix.PartOfSpeechRA, Is.SameAs(_irregular));
		}

		[Test]
		public void Move_ToTheCategoryItIsIn_OrOfAnotherKindOfObject_DoesNothing()
		{
			var steps = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			Assert.That(CategoryItemMove.Move(_slot, _irregular, Undo, Redo), Is.False);
			Assert.That(CategoryItemMove.Move(_template, _irregular, Undo, Redo), Is.False);
			Assert.That(CategoryItemMove.Move(_irregular, _noun, Undo, Redo), Is.False, "a category is no slot or template");
			Assert.That(CategoryItemMove.Move(_slot, null, Undo, Redo), Is.False);

			Assert.That(Cache.ActionHandlerAccessor.UndoableSequenceCount, Is.EqualTo(steps), "no undoable step");
		}

		[Test]
		public void Move_ATemplateToTheCategoryAbove_TakesItsSlotAlong_InOneUndoableStep()
		{
			Assert.That(CategoryItemMove.Move(_template, _verb, Undo, Redo), Is.True);

			Assert.That(_template.Owner, Is.SameAs(_verb));
			Assert.That(_slot.Owner, Is.SameAs(_verb), "the slot it uses follows");
			Assert.That(_affix.PartOfSpeechRA, Is.SameAs(_verb));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_template.Owner, Is.SameAs(_irregular), "one Undo");
			Assert.That(_slot.Owner, Is.SameAs(_irregular));
		}

		[Test]
		public void Move_ATemplateToTheCategoryAbove_LeavesASlotWhoseNameIsUsedThere()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var taken = Cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>().Create();
				_verb.AffixSlotsOC.Add(taken);
				taken.Name.SetAnalysisDefaultWritingSystem("Tense");
			});

			Assert.That(CategoryItemMove.Move(_template, _verb, Undo, Redo), Is.True);

			Assert.That(_template.Owner, Is.SameAs(_verb));
			Assert.That(_slot.Owner, Is.SameAs(_irregular), "Verb already has a slot named Tense");
		}

		[Test]
		public void Move_ATemplateToAnotherCategoryThanTheOneAbove_LeavesItsSlots()
		{
			Assert.That(CategoryItemMove.Move(_template, _noun, Undo, Redo), Is.True);

			Assert.That(_template.Owner, Is.SameAs(_noun));
			Assert.That(_slot.Owner, Is.SameAs(_irregular));
		}

		[Test]
		public void SlotNamed_FindsASlotOfTheCategoryOrOneAboveIt()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => _verb.AffixSlotsOC.Add(_slot));

			Assert.That(CategoryItemMove.SlotNamed(_irregular, "Tense"), Is.SameAs(_slot), "inherited from Verb");
			Assert.That(CategoryItemMove.SlotNamed(_verb, "Tense"), Is.SameAs(_slot));
			Assert.That(CategoryItemMove.SlotNamed(_noun, "Tense"), Is.Null);
		}
	}
}
