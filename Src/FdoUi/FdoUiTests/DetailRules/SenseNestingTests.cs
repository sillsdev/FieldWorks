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
	/// The Demote and Promote rule of a sense: when each applies, where the sense lands, and
	/// that each move is one undoable step.
	/// </summary>
	[TestFixture]
	public class SenseNestingTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry _entry;
		private ILexSense _first;
		private ILexSense _second;
		private ILexSense _third;
		private ILexSense _subsense;

		// An entry with three senses; the first has one subsense.
		[SetUp]
		public void MakeEntry()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var senses = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
				_entry.SensesOS.Add(_first = senses.Create());
				_entry.SensesOS.Add(_second = senses.Create());
				_entry.SensesOS.Add(_third = senses.Create());
				_first.SensesOS.Add(_subsense = senses.Create());
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
		public void CanDemote_OnlyASenseWithASibling()
		{
			Assert.That(SenseNesting.CanDemote(_second), Is.True);
			Assert.That(SenseNesting.CanDemote(_subsense), Is.False, "the only subsense has no sibling");
			Assert.That(SenseNesting.CanDemote(_entry), Is.False, "no sense");
			Assert.That(SenseNesting.CanDemote(null), Is.False);

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				_first.SensesOS.Add(Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create()));

			Assert.That(SenseNesting.CanDemote(_subsense), Is.True, "a subsense with a sibling");
		}

		[Test]
		public void Demote_MakesTheSenseTheLastSubsenseOfTheOneBefore_InOneUndoableStep()
		{
			Assert.That(SenseNesting.Demote(_second), Is.True);

			Assert.That(_entry.SensesOS, Is.EqualTo(new[] { _first, _third }));
			Assert.That(_first.SensesOS, Is.EqualTo(new[] { _subsense, _second }), "after the existing subsense");
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo("Undo Demote"));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_entry.SensesOS, Is.EqualTo(new[] { _first, _second, _third }), "one Undo puts it back");
		}

		[Test]
		public void Demote_TheFirstSense_GoesUnderTheOneAfterIt()
		{
			Assert.That(SenseNesting.Demote(_first), Is.True);

			Assert.That(_entry.SensesOS, Is.EqualTo(new[] { _second, _third }));
			Assert.That(_second.SensesOS, Is.EqualTo(new[] { _first }));
			Assert.That(_first.SensesOS, Is.EqualTo(new[] { _subsense }), "its own subsenses go with it");
		}

		[Test]
		public void Demote_DoesNothing_WhenTheRuleRefuses()
		{
			Assert.That(SenseNesting.Demote(_subsense), Is.False);
			Assert.That(_first.SensesOS, Is.EqualTo(new[] { _subsense }));
		}

		[Test]
		public void CanPromote_OnlyASubsense()
		{
			Assert.That(SenseNesting.CanPromote(_subsense), Is.True);
			Assert.That(SenseNesting.CanPromote(_second), Is.False, "already a top-level sense");
			Assert.That(SenseNesting.CanPromote(null), Is.False);
		}

		[Test]
		public void Promote_PutsTheSubsenseRightAfterItsSense_InOneUndoableStep()
		{
			Assert.That(SenseNesting.Promote(_subsense), Is.True);

			Assert.That(_entry.SensesOS, Is.EqualTo(new[] { _first, _subsense, _second, _third }));
			Assert.That(_first.SensesOS, Is.Empty);
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo("Undo Promote"));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_first.SensesOS, Is.EqualTo(new[] { _subsense }), "one Undo puts it back");
		}

		[Test]
		public void Promote_ASubsubsense_StaysUnderTheTopLevelSense()
		{
			ILexSense subsubsense = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				_subsense.SensesOS.Add(subsubsense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create()));

			Assert.That(SenseNesting.Promote(subsubsense), Is.True);

			Assert.That(_first.SensesOS, Is.EqualTo(new[] { _subsense, subsubsense }));
			Assert.That(SenseNesting.Promote(_second), Is.False, "a top-level sense stays");
		}
	}
}
