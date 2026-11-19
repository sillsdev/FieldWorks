// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The Promote and Demote rules of a notebook record: when each applies, which records may
	/// take a demoted one, where the record lands, and that each move is one undoable step.
	/// </summary>
	[TestFixture]
	public class RecordNestingTests : MemoryOnlyBackendProviderTestBase
	{
		private IRnResearchNbk _notebook;
		private IRnGenericRec _top;
		private IRnGenericRec _otherTop;
		private IRnGenericRec _sub1;
		private IRnGenericRec _sub2;
		private IRnGenericRec _sub3;
		private IRnGenericRec _subsub;

		// Two top-level records; the first has three subrecords, the first of them one more.
		[SetUp]
		public void MakeRecords()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				_notebook = Cache.LanguageProject.ResearchNotebookOA;
				if (_notebook == null)
				{
					Cache.LanguageProject.ResearchNotebookOA = _notebook
						= Cache.ServiceLocator.GetInstance<IRnResearchNbkFactory>().Create();
				}
				var records = Cache.ServiceLocator.GetInstance<IRnGenericRecFactory>();
				_notebook.RecordsOC.Add(_top = records.Create());
				_notebook.RecordsOC.Add(_otherTop = records.Create());
				_top.SubRecordsOS.Add(_sub1 = records.Create());
				_top.SubRecordsOS.Add(_sub2 = records.Create());
				_top.SubRecordsOS.Add(_sub3 = records.Create());
				_sub1.SubRecordsOS.Add(_subsub = records.Create());
			});
		}

		// Every top-level record, the promoted ones included.
		[TearDown]
		public void DeleteRecords()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				foreach (var record in _notebook.RecordsOC.ToList())
					record.Delete();
			});
		}

		[Test]
		public void CanPromote_OnlyASubrecord()
		{
			Assert.That(RecordNesting.CanPromote(_sub2), Is.True);
			Assert.That(RecordNesting.CanPromote(_subsub), Is.True);
			Assert.That(RecordNesting.CanPromote(_top), Is.False, "a top-level record");
			Assert.That(RecordNesting.CanPromote(_notebook), Is.False, "no record");
			Assert.That(RecordNesting.CanPromote(null), Is.False);
		}

		[Test]
		public void Promote_ASubrecordOfASubrecord_PutsItRightAfterItsOwner_InOneUndoableStep()
		{
			Assert.That(RecordNesting.Promote(_subsub), Is.True);

			Assert.That(_top.SubRecordsOS.ToList(), Is.EqualTo(new[] { _sub1, _subsub, _sub2, _sub3 }));
			Assert.That(_sub1.SubRecordsOS, Is.Empty);
			Assert.That(RecordNesting.IsTopLevel(_subsub), Is.False);
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_subsub.Owner, Is.EqualTo(_sub1), "one Undo puts it back");
		}

		[Test]
		public void Promote_ASubrecordOfATopLevelRecord_MakesItTopLevel()
		{
			Assert.That(RecordNesting.Promote(_sub2), Is.True);

			Assert.That(_sub2.Owner, Is.EqualTo(_notebook));
			Assert.That(RecordNesting.IsTopLevel(_sub2), Is.True);
			Assert.That(_top.SubRecordsOS.ToList(), Is.EqualTo(new[] { _sub1, _sub3 }));
			Assert.That(RecordNesting.Promote(_top), Is.False, "a top-level record goes no higher");
		}

		[Test]
		public void CanDemoteSubrecord_OnlyASubrecordWithASibling()
		{
			Assert.That(RecordNesting.CanDemoteSubrecord(_sub2), Is.True);
			Assert.That(RecordNesting.CanDemoteSubrecord(_subsub), Is.False, "the only subrecord has no sibling");
			Assert.That(RecordNesting.CanDemoteSubrecord(_top), Is.False, "a top-level record");
			Assert.That(RecordNesting.SubrecordDemoteOwners(_sub2), Is.EqualTo(new[] { _sub1, _sub3 }),
				"its siblings, in order");
			Assert.That(RecordNesting.SubrecordDemoteOwners(_subsub), Is.Empty);
		}

		[Test]
		public void CanDemoteRecord_OnlyATopLevelRecordWithAnother()
		{
			Assert.That(RecordNesting.CanDemoteRecord(_top), Is.True);
			Assert.That(RecordNesting.CanDemoteRecord(_sub1), Is.False, "a subrecord");
			Assert.That(RecordNesting.RecordDemoteOwners(_top), Is.EqualTo(new[] { _otherTop }));

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => _otherTop.Delete());
			Assert.That(RecordNesting.CanDemoteRecord(_top), Is.False, "the only top-level record");
		}

		[Test]
		public void ChooseOwner_TakesTheOnlyCandidate_AndAsksAmongMore()
		{
			var asked = 0;
			IRnGenericRec Ask(IReadOnlyList<IRnGenericRec> candidates)
			{
				asked++;
				return candidates.Last();
			}

			Assert.That(RecordNesting.ChooseOwner(new[] { _sub1 }, Ask), Is.EqualTo(_sub1));
			Assert.That(asked, Is.EqualTo(0), "one candidate needs no question");
			Assert.That(RecordNesting.ChooseOwner(new[] { _sub1, _sub3 }, Ask), Is.EqualTo(_sub3));
			Assert.That(asked, Is.EqualTo(1));
			Assert.That(RecordNesting.ChooseOwner(new IRnGenericRec[0], Ask), Is.Null);
		}

		[Test]
		public void Demote_MakesTheRecordTheFirstSubrecordOfItsNewOwner_InOneUndoableStep()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				_sub3.SubRecordsOS.Add(Cache.ServiceLocator.GetInstance<IRnGenericRecFactory>().Create()));
			var existing = _sub3.SubRecordsOS[0];

			Assert.That(RecordNesting.Demote(_sub2, _sub3), Is.True);

			Assert.That(_sub3.SubRecordsOS.ToList(), Is.EqualTo(new[] { _sub2, existing }));
			Assert.That(_top.SubRecordsOS.ToList(), Is.EqualTo(new[] { _sub1, _sub3 }));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_sub2.Owner, Is.EqualTo(_top), "one Undo puts it back");
		}

		[Test]
		public void Demote_ATopLevelRecord_UnderAnother()
		{
			Assert.That(RecordNesting.Demote(_top, _otherTop), Is.True);

			Assert.That(_otherTop.SubRecordsOS.ToList(), Is.EqualTo(new[] { _top }));
			Assert.That(_notebook.RecordsOC.Contains(_top), Is.False);
		}
	}
}
