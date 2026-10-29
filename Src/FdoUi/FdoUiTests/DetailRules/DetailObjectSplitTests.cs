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
	/// The Split rule of a detail row (Move Sense to a New Entry): a subsense always splits
	/// off, any other item only when its owner keeps another.
	/// </summary>
	[TestFixture]
	public class DetailObjectSplitTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry _entry;
		private ILexSense _sense;
		private ILexSense _subsense;

		// An entry with a lexeme form and one sense, which has one subsense.
		[SetUp]
		public void MakeEntry()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				_entry.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				_entry.SensesOS.Add(_sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create());
				_sense.SensesOS.Add(_subsense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create());
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
		public void CanSplitNow_ASubsense_EvenAsTheOnlyOne()
		{
			Assert.That(DetailObjectSplit.CanSplitNow(_subsense), Is.True);
		}

		[Test]
		public void CanSplitNow_ASense_OnlyWhenTheEntryKeepsAnother()
		{
			Assert.That(DetailObjectSplit.CanSplitNow(_sense), Is.False, "the entry's only sense");

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				_entry.SensesOS.Add(Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create()));

			Assert.That(DetailObjectSplit.CanSplitNow(_sense), Is.True);
		}

		[Test]
		public void CanSplitNow_RefusesAnObjectOutsideAnOwningVector()
		{
			Assert.That(DetailObjectSplit.CanSplitNow(_entry.LexemeFormOA), Is.False, "an atomic field");
			Assert.That(DetailObjectSplit.CanSplitNow(_entry), Is.False, "an entry has no owner");
			Assert.That(DetailObjectSplit.CanSplitNow(null), Is.False);
		}
	}
}
