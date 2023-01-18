// Copyright (c) 2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using LanguageExplorer;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;

// ReSharper disable PossibleNullReferenceException (tests are expected to pass; ReSharper warnings are ugly)
namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class RecordListTests : MemoryOnlyBackendProviderTestBase
	{
		internal IRecordList _recordList;
		private StatusBar _statusBar;
		protected int m_wsEn;
		private IReversalIndexEntryFactory m_revIndexEntryFactory;
		private IReversalIndexFactory m_revIndexFactory;
		private IReversalIndexRepository m_revIndexRepo;
		protected IReversalIndex m_revIndex;

		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			var flexComponentParameters = TestSetupServices.SetupEverything(Cache, false);
			_statusBar = StatusBarPanelServices.CreateStatusBarFor_TESTS();
			Cache.ProjectId.Path = Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests/TestData/");
			m_wsEn = Cache.DefaultAnalWs;
			SetupReversalFactoriesAndRepositories();
			flexComponentParameters.PropertyTable.SetProperty(LanguageExplorerConstants.ReversalIndexGuid, m_revIndexRepo.FindOrCreateIndexForWs(m_wsEn).Guid.ToString());
			flexComponentParameters.PropertyTable.SetProperty(FwUtilsConstants.cache, Cache);
			_recordList = flexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(LanguageExplorerConstants.AllReversalEntries, _statusBar, RecordListActivator.AllReversalEntriesFactoryMethod);
		}

		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			_recordList.ActivateUI(false);
			AddSomeReversalEntries(1);
		}

		[TearDown]
		public void TearDown()
		{
			base.TestTearDown();
			ClearReversalEntries();
		}

		[OneTimeTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			_statusBar.Dispose();
		}
		private void SetupReversalFactoriesAndRepositories()
		{
			Assert.That(Cache, Is.Not.Null, "No cache yet!?");
			m_revIndexEntryFactory = Cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>();
			m_revIndexFactory = Cache.ServiceLocator.GetInstance<IReversalIndexFactory>();
			m_revIndexRepo = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
		}

		protected IReversalIndex CreateReversalIndex()
		{
			Assert.That(m_revIndexFactory, Is.Not.Null, "Fixture Initialization is not complete.");

			//create a reversal index for this project.
			var wsObj = Cache.LanguageProject.DefaultAnalysisWritingSystem;
			var revIndex = m_revIndexRepo.FindOrCreateIndexForWs(wsObj.Handle);
			return revIndex;
		}

		private IReversalIndexEntry CreateAndAddReversalIndexEntry(IReversalIndex revIndex = null)
		{
			revIndex ??= m_revIndex ?? (m_revIndex = CreateReversalIndex());
			Assert.That(m_revIndexEntryFactory, Is.Not.Null, "Fixture Initialization is not complete.");

			var revIndexEntry = m_revIndexEntryFactory.Create();
			revIndex.EntriesOC.Add(revIndexEntry);
			return revIndexEntry;
		}

		private List<IReversalIndexEntry> AddSomeReversalEntries(int cRevEntries = 5)
		{
			var entries = new List<IReversalIndexEntry>(cRevEntries);
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				for (var i = 0; i < cRevEntries; i++)
				{
					entries.Add(CreateAndAddReversalIndexEntry());
				}
			});
			return entries;
		}
		private void ClearReversalEntries()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_revIndex.AllEntries.ForEach(e => e.Delete());
			});
		}
		/// <summary>
		/// Finish loading the list for tests that need it, then reset the reload count
		/// </summary>
		protected void FinishLoadingAndAssertPreconditions()
		{
			// SetSuppressingLoadList and ReloadList separately, since other tests may leave residue.
			_recordList.ListLoadingSuppressed = false;
			_recordList.UpdateList(false);
			_recordList.CurrentIndex = 0;

			Assert.That(_recordList.OwningObject, Is.Not.Null);
			Assert.That(_recordList.CurrentIndex, Is.EqualTo(0));
			Assert.That(_recordList.CurrentObjectHvo, Is.GreaterThan(0));
		}

		[Test]
		public void Reload_NoInsertionsOrDeletions_NoReloads()
		{
			AddSomeReversalEntries(1);
			FinishLoadingAndAssertPreconditions();

			var reloadCounter = new ReloadCounter(_recordList);

			// SUT
			_recordList.PropChanged(_recordList.OwningObject.Hvo, ReversalIndexTags.kflidEntries, 0, 0, 0);

			Assert.That(reloadCounter.ReloadCallCount, Is.EqualTo(0));
			Assert.That(reloadCounter.ReloadEventCount, Is.EqualTo(0));
		}

		[Test]
		public void Reload_OneExistingItem_Reloads()
		{
			FinishLoadingAndAssertPreconditions();
			Assert.That(_recordList.ListSize, Is.EqualTo(1), "One item should have been added by per-test setup");

			var reloadCounter = new ReloadCounter(_recordList);
			// SUT
			_recordList.PropChanged(_recordList.OwningObject.Hvo, ReversalIndexTags.kflidEntries, 0, 1, 1);

			Assert.That(reloadCounter.ReloadCallCount, Is.EqualTo(1));
		}

		[Test]
		public void Reload_NoCurrentObject_Reloads()
		{
			AddSomeReversalEntries();
			FinishLoadingAndAssertPreconditions();
			_recordList.CurrentIndex = -1; // This clears the current object from the _recordList
			var reloadCounter = new ReloadCounter(_recordList);

			// SUT
			_recordList.PropChanged(_recordList.OwningObject.Hvo, ReversalIndexTags.kflidEntries, 0, 0, 0);

			Assert.That(reloadCounter.ReloadCallCount, Is.EqualTo(1));
		}

		[Test]
		public void Reload_NoOwningObject_Reloads()
		{
			AddSomeReversalEntries();
			FinishLoadingAndAssertPreconditions();
			var owningObject = _recordList.OwningObject;
			_recordList.OwningObject = null;
			try
			{
				var reloadCounter = new ReloadCounter(_recordList);
				// SUT
				_recordList.UpdateList(false, true);

				Assert.That(reloadCounter.ReloadCallCount, Is.EqualTo(1));
			}
			finally
			{
				_recordList.OwningObject = owningObject;
			}
		}

		[Test]
		public void Reload_OneItemToUpdateOfMany_UsesQuickUpdate()
		{
			AddSomeReversalEntries();
			FinishLoadingAndAssertPreconditions();

			var reloadCounter = new ReloadCounter(_recordList);
			// SUT
			_recordList.PropChanged(_recordList.OwningObject.Hvo, ReversalIndexTags.kflidEntries, 0, 1, 1);

			Assert.That(reloadCounter.ReloadCallCount, Is.EqualTo(0));
			Assert.That(reloadCounter.ReloadEventCount, Is.EqualTo(1));
		}

		[Test]
		public void Reload_ManyChanges_Reloads()
		{
			AddSomeReversalEntries();
			FinishLoadingAndAssertPreconditions();

			var reloadCounter = new ReloadCounter(_recordList);
			// SUT
			_recordList.PropChanged(_recordList.OwningObject.Hvo, ReversalIndexTags.kflidEntries, 1, 2, 3);

			Assert.That(reloadCounter.ReloadCallCount, Is.EqualTo(1));
		}

		[Test]
		public void Reload_AddedItems_Reloads([Values(1, 3)] int cAdded)
		{
			FinishLoadingAndAssertPreconditions();

			var reloadCounter = new ReloadCounter(_recordList);

			// SUT (committing the undo task should trigger a reload)
			AddSomeReversalEntries(cAdded);

			Assert.That(reloadCounter.ReloadCallCount, Is.EqualTo(1), "Inserting item(s) requires reloading the list");
			Assert.That(_recordList.ListSize, Is.EqualTo(1 + cAdded), "List should contain the new items");
		}

		[Test]
		public void Reload_DeletedSomeItems_UsesQuickUpdate([Values(1, 3)] int cAdded)
		{
			var deletable = AddSomeReversalEntries(cAdded);
			AddSomeReversalEntries(6); // these are not deletable
			FinishLoadingAndAssertPreconditions();

			var reloadCounter = new ReloadCounter(_recordList);
			// SUT (committing the undo task should trigger a reload)
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				deletable.ForEach(e => e.Delete());
			});

			Assert.That(reloadCounter.ReloadCallCount, Is.EqualTo(0), "Simply deleting a few items shouldn't require the entire list to be reloaded");
			Assert.That(reloadCounter.ReloadEventCount, Is.EqualTo(1), "Removing items should trigger a reloaded event");
			Assert.That(_recordList.ListSize, Is.EqualTo(7), "Deletable items should have been deleted");
		}

		[Test]
		public void Reload_DeletedHalfOfItems_Reloads()
		{
			var deletable = AddSomeReversalEntries(3);
			AddSomeReversalEntries(2);
			FinishLoadingAndAssertPreconditions();

			var reloadCounter = new ReloadCounter(_recordList);
			// SUT (committing the undo task should trigger a reload)
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				deletable.ForEach(e => e.Delete());
			});

			Assert.That(reloadCounter.ReloadCallCount, Is.EqualTo(1), "Deleting half the items should reload the whole list");
			Assert.That(_recordList.ListSize, Is.EqualTo(3), "Deletable items should have been deleted");
		}

		[Test]
		public void ListLoadingSuppressed()
		{
			var reloadCounter = new ReloadCounter(_recordList);
			Assert.That(_recordList.RequestedLoadWhileSuppressed, Is.False, "After ActivateUI is called a Reload happens and this is false");
			Assert.That(_recordList.ListLoadingSuppressed, Is.False, "Lists start with loading suppressed");
			// Suppress list loading and then request a reload
			_recordList.ListLoadingSuppressed = true;
			Assert.That(_recordList.RequestedLoadWhileSuppressed, Is.False, "Reload should still be pending");
			_recordList.ReloadIfNeeded();
			Assert.That(_recordList.RequestedLoadWhileSuppressed, Is.True, "Should be true as we requested a reload, while suppressed.");
			Assert.That(reloadCounter.ReloadEventCount, Is.EqualTo(0), "Shouldn't have actually reloaded");
			_recordList.ListLoadingSuppressed = false;
			Assert.That(reloadCounter.ReloadCallCount, Is.EqualTo(2), "Setting suppression to false should have triggered the suppressed reload");
			Assert.That(reloadCounter.ReloadEventCount, Is.EqualTo(1), "Setting suppression to false should have triggered the suppressed reload");
			Assert.That(_recordList.RequestedLoadWhileSuppressed, Is.False, "The requested reload has completed; the request can be forgotten");
			_recordList.RequestedLoadWhileSuppressed = true;
			_recordList.ReloadIfNeeded();
			Assert.That(reloadCounter.ReloadEventCount, Is.EqualTo(2), "should have reloaded as requested");
			Assert.That(_recordList.RequestedLoadWhileSuppressed, Is.False, "Reload should have cleared the reload requested");
		}

		[Test]
		public void ListLoadingSuppressedByInactiveClerk()
		{
			var reloadCounter = new ReloadCounter(_recordList);
			_recordList.BecomeInactive();
			Assert.That(_recordList.IsActiveInGui, Is.False, "Clerk starts inactive");
			_recordList.ListLoadingSuppressed = false;
			_recordList.RequestedLoadWhileSuppressed = false;
			_recordList.ReloadIfNeeded();
			Assert.That(_recordList.RequestedLoadWhileSuppressed, Is.True, "Requested Load While Inactive");
			Assert.That(reloadCounter.ReloadEventCount, Is.EqualTo(0), "Shouldn't have actually reloaded");
		}

		private class ReloadCounter
		{
			private IRecordList recordList;

			public ReloadCounter(IRecordList recordList)
			{
				this.recordList = recordList;
				recordList.Subscriber.Subscribe("RestoreScrollPosition", IncrementReloadEvent); // RestoreScrollPosition is fired only when an actual reload happens
				recordList.Subscriber.Subscribe("ReloadListCalled", IncrementReloadCall);
			}

			public int ReloadCallCount { get; private set; }
			public int ReloadEventCount { get; private set; }

			private void IncrementReloadCall(object _)
			{
				++ReloadCallCount;
			}

			private void IncrementReloadEvent(object _)
			{
				++ReloadEventCount;
			}
		}
	}
}