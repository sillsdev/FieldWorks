// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The Delete rule of a detail row: the required-field cases of
	/// <see cref="DetailObjectDeletion.CanDeleteNow"/>, and the item text.
	/// </summary>
	[TestFixture]
	public class DetailObjectDeletionTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry _entry;
		private ILexSense _sense;
		private ICmTranslation _translation;

		// An entry with one sense, whose example has one translation. The entry has no entry
		// refs, so LexEntry requires its Senses.
		[SetUp]
		public void MakeEntry()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				_sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				_entry.SensesOS.Add(_sense);
				var example = Cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>().Create();
				_sense.ExamplesOS.Add(example);
				var type = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>()
					.GetObject(CmPossibilityTags.kguidTranFreeTranslation);
				_translation = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(example, type);
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
		public void CanDeleteNow_AnUnownedObject()
		{
			Assert.That(_entry.Owner, Is.Null, "precondition");
			Assert.That(DetailObjectDeletion.CanDeleteNow(_entry), Is.True);
		}

		[Test]
		public void CanDeleteNow_AnObjectInAFieldItsOwnerDoesNotRequire()
		{
			Assert.That(_translation.Owner.IsFieldRequired(_translation.OwningFlid), Is.False, "precondition");
			Assert.That(DetailObjectDeletion.CanDeleteNow(_translation), Is.True);
		}

		[Test]
		public void CanDeleteNow_RefusesTheLastItemOfARequiredVector_AndAllowsOneOfTwo()
		{
			Assert.That(_entry.IsFieldRequired(LexEntryTags.kflidSenses), Is.True,
				"precondition: an entry without entry refs requires its senses");
			Assert.That(DetailObjectDeletion.CanDeleteNow(_sense), Is.False, "the only sense stays");

			ILexSense second = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				second = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				_entry.SensesOS.Add(second);
			});

			Assert.That(DetailObjectDeletion.CanDeleteNow(_sense), Is.True);
			Assert.That(DetailObjectDeletion.CanDeleteNow(second), Is.True);
		}

		[Test]
		public void CanDeleteNow_IsFalseForNoObject()
		{
			Assert.That(DetailObjectDeletion.CanDeleteNow(null), Is.False);
		}

		[Test]
		public void DeleteLabel_KeepsAnEnabledLabel_AndNotesADisabledOne()
		{
			var note = StringTable.Table.GetString(DetailObjectDeletion.CannotDeleteKey);
			Assert.That(note, Is.Not.Empty, "precondition");

			Assert.That(DetailObjectDeletion.DeleteLabel("Delete Translation", _translation, true),
				Is.EqualTo("Delete Translation"));
			Assert.That(DetailObjectDeletion.DeleteLabel("Delete Translation", _translation, false),
				Is.EqualTo("Delete Translation" + note));
			Assert.That(DetailObjectDeletion.DeleteLabel(null, null, false), Is.EqualTo(note));
		}

		[Test]
		public void DeleteLabel_NamesTheTargetsClass_WhereTheLabelAsksForIt()
		{
			var className = StringTable.Table.GetString("LexSense", "ClassNames");

			Assert.That(DetailObjectDeletion.DeleteLabel("Delete {0}", _sense, true), Is.EqualTo("Delete " + className));
			Assert.That(DetailObjectDeletion.DeleteLabel("Delete {0}", null, true), Is.EqualTo("Delete {0}"),
				"no target, nothing to name");
		}
	}
}
