// Copyright (c) 2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.XWorks.LexEd;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

// ReSharper disable PossibleNullReferenceException (tests are expected to pass; ReSharper warnings are ugly)
namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class RecordListTests : AllReversalEntriesRecordListTestBase
	{
		protected RecordClerk m_clerk;
		protected AllReversalEntriesRecordListForTests m_list;
		protected int m_wsEn;

		[OneTimeSetUp]
		public override void FixtureInit()
		{
			base.FixtureInit();
			CreateClerkAndList();
			m_propertyTable.SetProperty("ActiveClerk", m_clerk, false);
			Cache.ProjectId.Path = Path.Combine(FwDirectoryFinder.SourceDirectory, "xWorks/xWorksTests/TestData/");
			m_wsEn = Cache.DefaultAnalWs;
		}

		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_clerk.ActivateUI(false);
			m_list.ResetReloadCount();
		}

		private void CreateClerkAndList()
		{
			// ReSharper disable StringLiteralTypo - copied from configuration xml
			const string entryClerk = @"<?xml version='1.0' encoding='UTF-8'?>
			<root>
				<clerks>
					<clerk id='AllReversalEntries'>
						<dynamicloaderinfo assemblyPath='xWorksTests.dll' class='SIL.FieldWorks.XWorks.ReversalEntryClerkForListTests'/>
						<recordList owner='ReversalIndex' property='AllEntries'>
							<dynamicloaderinfo assemblyPath='xWorksTests.dll' class='SIL.FieldWorks.XWorks.AllReversalEntriesRecordListForTests'/>
						</recordList>
					</clerk>
				</clerks>
				<tools>
					<tool label='Reversal Indexes' value='reversalToolEditComplete' icon='SideBySideView'>
						<control>
							<dynamicloaderinfo assemblyPath='xWorks.dll' class='SIL.FieldWorks.XWorks.XhtmlDocView'/>
							<parameters area='lexicon' clerk='AllReversalEntries' altTitleId='ReversalIndexEntry-Plural' persistContext='Reversal'
								backColor='White' layout='' layoutProperty='ReversalIndexPublicationLayout' layoutSuffix='Preview' editable='false'
								configureObjectName='ReversalIndex'/>
						</control>
					</tool>
				</tools>
			</root>";
			var doc = new XmlDocument();
			doc.LoadXml(entryClerk);
			var clerkNode = doc.SelectSingleNode("//tools/tool[@label='Reversal Indexes']//parameters[@area='lexicon']");
			m_clerk = RecordClerkFactory.CreateClerk(m_mediator, m_propertyTable, clerkNode, false);
			m_clerk.Init(m_mediator, m_propertyTable, clerkNode);
			m_list = (AllReversalEntriesRecordListForTests)m_clerk.GetType().GetField("m_list", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(m_clerk);
			m_list.ResetReloadCount();
		}

		/// <remarks>called by OneTimeTearDown</remarks>
		protected override void TearDown()
		{
			base.TearDown();
			m_clerk?.Dispose();
			m_clerk = null;
		}

		/// <summary>
		/// Finish loading the list for tests that need it, then reset the reload count
		/// </summary>
		protected void FinishLoadingAndAssertPreconditions()
		{
			// SetSuppressingLoadList and ReloadList separately, since other tests may leave residue.
			m_list.SetSuppressingLoadList(false);
			m_list.ReloadList();
			m_list.CurrentIndex = 0;
			m_list.ResetReloadCount();

			Assert.That(m_list.OwningObject, Is.Not.Null);
			Assert.That(m_list.CurrentIndex, Is.EqualTo(0));
			Assert.That(m_list.HvoCurrent, Is.GreaterThan(0));
		}

		[Test]
		public void Reload_NoInsertionsOrDeletions_NoReloads()
		{
			FinishLoadingAndAssertPreconditions();

			// SUT
			m_list.ReloadList(0, 0, 0);

			Assert.That(m_list.ReloadCallCount, Is.EqualTo(0));
			Assert.That(m_list.ReloadEventCount, Is.EqualTo(0));
		}

		[Test]
		public void Reload_OneExistingItem_Reloads()
		{
			FinishLoadingAndAssertPreconditions();
			Assert.That(m_list.ItemCount, Is.EqualTo(1), "One item should have been added by per-test setup");

			// SUT
			m_list.ReloadList(0, 1, 1);

			Assert.That(m_list.ReloadCallCount, Is.EqualTo(1));
		}

		[Test]
		public void Reload_NoCurrentObject_Reloads()
		{
			AddSomeReversalEntries();
			FinishLoadingAndAssertPreconditions();
			m_list.HvoCurrent = 0;

			// SUT
			m_list.ReloadList(0, 0, 0);

			Assert.That(m_list.ReloadCallCount, Is.EqualTo(1));
		}

		[Test]
		public void Reload_NoOwningObject_Reloads()
		{
			AddSomeReversalEntries();
			FinishLoadingAndAssertPreconditions();
			var owningObject = m_list.ClearOwningObjectNoSideEffects();
			try
			{
				// SUT
				m_list.ReloadList(0, 0, 0);

				Assert.That(m_list.ReloadCallCount, Is.EqualTo(1));
			}
			finally
			{
				m_list.OwningObject = owningObject;
			}
		}

		[Test]
		public void Reload_OneItemToUpdateOfMany_UsesQuickUpdate()
		{
			AddSomeReversalEntries();
			FinishLoadingAndAssertPreconditions();

			// SUT
			m_list.ReloadList(0, 1, 1);

			Assert.That(m_list.ReloadCallCount, Is.EqualTo(0));
			Assert.That(m_list.ReloadEventCount, Is.EqualTo(1));
		}

		[Test]
		public void Reload_ManyChanges_Reloads()
		{
			AddSomeReversalEntries();
			FinishLoadingAndAssertPreconditions();

			// SUT
			m_list.ReloadList(1, 2, 3);

			Assert.That(m_list.ReloadCallCount, Is.EqualTo(1));
		}

		[Test]
		public void Reload_AddedItems_Reloads([Values(1, 3)] int cAdded)
		{
			FinishLoadingAndAssertPreconditions();

			// SUT (committing the undo task should trigger a reload)
			AddSomeReversalEntries(cAdded);

			Assert.That(m_list.ReloadCallCount, Is.EqualTo(1), "Inserting item(s) requires reloading the list");
			Assert.That(m_list.ItemCount, Is.EqualTo(1 + cAdded), "List should contain the new items");
		}

		[Test]
		public void Reload_DeletedSomeItems_UsesQuickUpdate([Values(1, 3)] int cAdded)
		{
			var deletable = AddSomeReversalEntries(cAdded);
			AddSomeReversalEntries(6); // these are not deletable
			FinishLoadingAndAssertPreconditions();

			// SUT (committing the undo task should trigger a reload)
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				deletable.ForEach(e => e.Delete());
			});

			Assert.That(m_list.ReloadCallCount, Is.EqualTo(0), "Simply deleting a few items should't require the entire list to be reloaded");
			Assert.That(m_list.ReloadEventCount, Is.EqualTo(1), "Removing items should trigger a reloaded event");
			Assert.That(m_list.ItemCount, Is.EqualTo(7), "Deletable items should have been deleted");
		}

		[Test]
		public void Reload_DeletedHalfOfItems_Reloads()
		{
			var deletable = AddSomeReversalEntries(3);
			AddSomeReversalEntries(2);
			FinishLoadingAndAssertPreconditions();

			// SUT (committing the undo task should trigger a reload)
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				deletable.ForEach(e => e.Delete());
			});

			Assert.That(m_list.ReloadCallCount, Is.EqualTo(1), "Deleting half the items should reload the whole list");
			Assert.That(m_list.ItemCount, Is.EqualTo(3), "Deletable items should have been deleted");
		}

		[Test]
		public void ListLoadingSuppressed()
		{
			Assert.That(m_list.RequestedLoadWhileSuppressed, Is.True, "Lists start with a pending reload");
			Assert.That(m_list.ListLoadingSuppressed, Is.True, "Lists start with loading suppressed");
			// Set to false w/o the side effect of reloading
			m_list.SetSuppressingLoadList(false);
			Assert.That(m_list.RequestedLoadWhileSuppressed, Is.True, "Reload should still be pending");
			Assert.That(m_list.ReloadCallCount, Is.EqualTo(0), "Shouldn't have reloaded");
			Assert.That(m_list.ReloadEventCount, Is.EqualTo(0), "How did that happen?");

			m_list.ListLoadingSuppressed = true;
			Assert.That(m_list.RequestedLoadWhileSuppressed, Is.False, "Haven't tried to reload since suppressing");
			m_list.ReloadList();
			Assert.That(m_list.RequestedLoadWhileSuppressed, Is.True, "Requested Load While Suppressed");
			Assert.That(m_list.ReloadEventCount, Is.EqualTo(0), "Shouldn't have actually reloaded");

			m_list.ListLoadingSuppressed = false;
			Assert.That(m_list.ReloadCallCount, Is.EqualTo(2), "Setting suppression to false should have triggered the suppressed reload");
			Assert.That(m_list.ReloadEventCount, Is.EqualTo(1), "Setting suppression to false should have triggered the suppressed reload");
			Assert.That(m_list.RequestedLoadWhileSuppressed, Is.False, "The requested reload has completed; the request can be forgotten");

			m_list.RequestedLoadWhileSuppressed = true;
			m_list.ReloadList();
			Assert.That(m_list.ReloadEventCount, Is.EqualTo(2), "should have reloaded as requested");
			Assert.That(m_list.RequestedLoadWhileSuppressed, Is.False, "Reload should have cleared the reload requested");
		}

		[Test]
		public void ListLoadingSuppressedByInactiveClerk()
		{
			m_clerk.BecomeInactive();
			Assert.That(m_clerk.IsActiveInGui, Is.False, "Clerk starts inactive");
			m_list.SetSuppressingLoadList(false);
			m_list.RequestedLoadWhileSuppressed = false;
			m_list.ReloadList();
			Assert.That(m_list.RequestedLoadWhileSuppressed, Is.True, "Requested Load While Inactive");
			Assert.That(m_list.ReloadEventCount, Is.EqualTo(0), "Shouldn't have actually reloaded");
		}
	}

	// ReSharper disable once UnusedMember.Global (created by reflection)
	public class ReversalEntryClerkForListTests : ReversalEntryClerk
	{
		public override void ActivateUI(bool useRecordTreeBar, bool updateStatusBar = true) => m_fIsActiveInGui = true;

		/// <returns>false: we didn't even try (w/o extra setup, trying crashes)</returns>
		protected override bool TryRestoreSorter(XmlNode clerkConfiguration, LcmCache cache)
		{
			return false;
		}
	}

	public class AllReversalEntriesRecordListForTests : AllReversalEntriesRecordList
	{
		public ICmObject ClearOwningObjectNoSideEffects()
		{
			var oldOwner = m_owningObject;
			m_owningObject = null;
			return oldOwner;
		}

		public int HvoCurrent
		{
			get => m_hvoCurrent;
			set => m_hvoCurrent = value;
		}

		/// <summary>Number of items in the list</summary>
		public int ItemCount => VirtualListPublisher.get_VecSize(m_owningObject.Hvo, m_flid);
		/// <summary>Number of times the ReloadList method was called, even if reloading was suppressed</summary>
		public int ReloadCallCount { get; private set; }
		/// <summary>Number of DoneReload events that have fired (includes full reloads and single-item "updates" and "replacements"</summary>
		public int ReloadEventCount { get; private set; }

		public void ResetReloadCount() => ReloadCallCount = ReloadEventCount = 0; // TODO (Hasso) 2022.08: call this from Init

		public AllReversalEntriesRecordListForTests()
		{
			DoneReload += (sender, args) => { ReloadEventCount++; };
		}

		public override void ReloadList()
		{
			ReloadCallCount++;
			base.ReloadList();
		}
	}
}
