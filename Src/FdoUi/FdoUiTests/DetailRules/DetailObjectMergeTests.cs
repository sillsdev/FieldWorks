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
	/// The Merge rule of a detail row: the allomorph, subsense, required-vector and
	/// same-class cases of <see cref="DetailObjectMerge.CanMergeNow"/>, and the item text.
	/// </summary>
	[TestFixture]
	public class DetailObjectMergeTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry _entry;
		private IMoStemAllomorph _stemAllomorph;
		private IMoAffixAllomorph _affixAllomorph;
		private ILexSense _sense;
		private ILexSense _subsense;
		private ILexPronunciation _pronunciation;

		// An entry with a stem lexeme form, a stem allomorph and an affix allomorph, one sense
		// with one subsense, and one pronunciation. The entry has no entry refs, so it
		// requires its senses.
		[SetUp]
		public void MakeEntry()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				_entry.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				_stemAllomorph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				_entry.AlternateFormsOS.Add(_stemAllomorph);
				_affixAllomorph = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
				_entry.AlternateFormsOS.Add(_affixAllomorph);
				_sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				_entry.SensesOS.Add(_sense);
				_subsense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				_sense.SensesOS.Add(_subsense);
				_pronunciation = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create();
				_entry.PronunciationsOS.Add(_pronunciation);
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
		public void CanMergeNow_AnAllomorphOfTheLexemeFormsClass_EvenAsTheOnlyOneOfItsClass()
		{
			Assert.That(DetailObjectMerge.CanMergeNow(_stemAllomorph), Is.True, "the lexeme form is a candidate");
		}

		[Test]
		public void CanMergeNow_RefusesAnAllomorphOfAnotherClass_UnlessAnotherOfItsClassExists()
		{
			Assert.That(DetailObjectMerge.CanMergeNow(_affixAllomorph), Is.False, "neither the lexeme form nor another affix allomorph");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				_entry.AlternateFormsOS.Add(Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create()));

			Assert.That(DetailObjectMerge.CanMergeNow(_affixAllomorph), Is.True);
		}

		[Test]
		public void CanMergeNow_AnAllomorph_WhenTheEntryHasNoLexemeForm()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => _entry.LexemeFormOA = null);

			Assert.That(DetailObjectMerge.CanMergeNow(_stemAllomorph), Is.False, "no lexeme form and no other stem allomorph");
		}

		[Test]
		public void CanMergeNow_ASubsense_Always_AndASenseOnlyWithAnotherSense()
		{
			Assert.That(DetailObjectMerge.CanMergeNow(_subsense), Is.True, "a subsense merges into its sense");
			Assert.That(DetailObjectMerge.CanMergeNow(_sense), Is.False, "the entry's only sense");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				_entry.SensesOS.Add(Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create()));

			Assert.That(DetailObjectMerge.CanMergeNow(_sense), Is.True);
		}

		[Test]
		public void CanMergeNow_RefusesAnObjectWithNoPeer_AndAnythingOutsideAVector()
		{
			Assert.That(DetailObjectMerge.CanMergeNow(_pronunciation), Is.False, "the only pronunciation");
			Assert.That(DetailObjectMerge.CanMergeNow(_entry.LexemeFormOA), Is.False, "an atomic field");
			Assert.That(DetailObjectMerge.CanMergeNow(_entry), Is.False, "unowned");
			Assert.That(DetailObjectMerge.CanMergeNow(null), Is.False);
		}

		[Test]
		public void MergeLabel_AppendsTheCannotMergeNote_OnlyWhenRefused()
		{
			var note = StringTable.Table.GetString(DetailObjectMerge.CannotMergeKey);
			Assert.That(DetailObjectMerge.MergeLabel("Merge Allomorph into...", true), Is.EqualTo("Merge Allomorph into..."));
			Assert.That(DetailObjectMerge.MergeLabel("Merge Allomorph into...", false), Is.EqualTo("Merge Allomorph into..." + note));
			Assert.That(DetailObjectMerge.MergeLabel(null, false), Is.EqualTo(note));
		}
	}
}
