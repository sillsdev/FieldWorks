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
	/// The Duplicate rule of a detail row: which objects it applies to, where the copy goes,
	/// and that the whole duplicate is one undoable step.
	/// </summary>
	[TestFixture]
	public class DetailObjectDuplicationTests : MemoryOnlyBackendProviderTestBase
	{
		private const string Undo = "Undo Duplicate";
		private const string Redo = "Redo Duplicate";

		private IPartOfSpeech _category;
		private IMoInflAffixSlot _slot;
		private IMoInflAffixTemplate _first;
		private IMoInflAffixTemplate _second;
		private ILexEntry _entry;

		// A category with the slot Tense and two templates; the first uses the slot.
		[SetUp]
		public void MakeCategory()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var services = Cache.ServiceLocator;
				Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(
					_category = services.GetInstance<IPartOfSpeechFactory>().Create());
				_category.AffixSlotsOC.Add(_slot = services.GetInstance<IMoInflAffixSlotFactory>().Create());
				_slot.Name.SetAnalysisDefaultWritingSystem("Tense");
				var templates = services.GetInstance<IMoInflAffixTemplateFactory>();
				_category.AffixTemplatesOS.Add(_first = templates.Create());
				_first.Name.SetAnalysisDefaultWritingSystem("Finite");
				_first.PrefixSlotsRS.Add(_slot);
				_category.AffixTemplatesOS.Add(_second = templates.Create());
				_entry = services.GetInstance<ILexEntryFactory>().Create();
			});
		}

		[TearDown]
		public void DeleteCategory()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				foreach (var obj in new ICmObject[] { _entry, _category })
				{
					if (obj.IsValidObject)
						obj.Delete();
				}
			});
		}

		[Test]
		public void CanDuplicate_AnObjectThatCopiesItself_InAnOwningVector()
		{
			Assert.That(DetailObjectDuplication.CanDuplicate(_slot), Is.True, "in an owning collection");
			Assert.That(DetailObjectDuplication.CanDuplicate(_first), Is.True, "in an owning sequence");
			Assert.That(DetailObjectDuplication.CanDuplicate(_entry), Is.False, "an entry does not copy itself");
			Assert.That(DetailObjectDuplication.CanDuplicate(null), Is.False);
			Assert.That(DetailObjectDuplication.Duplicate(_entry, Undo, Redo), Is.Null);
		}

		[Test]
		public void Duplicate_ATemplate_PutsTheCopyRightAfterIt_InOneUndoableStep()
		{
			var copy = (IMoInflAffixTemplate)DetailObjectDuplication.Duplicate(_first, Undo, Redo);

			Assert.That(_category.AffixTemplatesOS.ToList(), Is.EqualTo(new[] { _first, copy, _second }));
			Assert.That(copy.PrefixSlotsRS.ToList(), Is.EqualTo(new[] { _slot }), "the copy uses the same slot");
			Assert.That(copy.Name.AnalysisDefaultWritingSystem.Text, Is.EqualTo("Finite2"), "the copy's name is numbered");
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_category.AffixTemplatesOS.ToList(), Is.EqualTo(new[] { _first, _second }), "one Undo");
		}

		[Test]
		public void Duplicate_ASlot_AddsTheCopyToItsCategory_InOneUndoableStep()
		{
			var copy = (IMoInflAffixSlot)DetailObjectDuplication.Duplicate(_slot, Undo, Redo);

			Assert.That(_category.AffixSlotsOC, Is.EquivalentTo(new[] { _slot, copy }));
			Assert.That(_slot.Name.AnalysisDefaultWritingSystem.Text, Is.EqualTo("Tense"), "the original keeps its name");
			Assert.That(copy.Name.AnalysisDefaultWritingSystem.Text, Is.EqualTo("Tense2"), "the copy's name is numbered");
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_category.AffixSlotsOC, Is.EquivalentTo(new[] { _slot }), "one Undo");
		}

		[Test]
		public void CopyProperties_CopiesOntoAnExistingObject_InItsOwnUndoableStep()
		{
			IMoInflAffixSlot other = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => _category.AffixSlotsOC.Add(other = Cache.ServiceLocator.GetInstance<IMoInflAffixSlotFactory>().Create()));
			Assert.That(DetailObjectDuplication.CanCopy(_slot), Is.True);
			Assert.That(DetailObjectDuplication.CanCopy(_entry), Is.False);

			DetailObjectDuplication.CopyProperties(_slot, other, Undo, Redo);

			Assert.That(other.Name.AnalysisDefaultWritingSystem.Text, Is.EqualTo("Tense2"));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(other.Name.AnalysisDefaultWritingSystem.Text, Is.Null.Or.Empty, "one Undo");
		}
	}
}
