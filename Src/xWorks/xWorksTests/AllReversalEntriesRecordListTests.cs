// Copyright (c) 2015-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.XWorks.LexEd;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	public class AllReversalEntriesRecordListTestBase : XWorksAppTestBase
	{
		protected Mediator m_mediator;
		protected PropertyTable m_propertyTable;
		protected List<ICmObject> m_createdObjectList;

		private IReversalIndexEntryFactory m_revIndexEntryFactory;

		private IReversalIndexFactory m_revIndexFactory;
		private IReversalIndexRepository m_revIndexRepo;

		protected IReversalIndexEntry m_revEntry;
		protected IReversalIndex m_revIndex;

		#region Setup and Teardown
		/// <summary>
		/// Run by FixtureInit() in XWorksAppTestBase
		/// </summary>
		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = this.Cache }, null, null);
			m_createdObjectList = new List<ICmObject>();
		}

		/// <summary>
		/// This is done before the entire set of tests is run.
		/// </summary>
		[OneTimeSetUp]
		public override void FixtureInit()
		{
			base.FixtureInit();
			SetupReversalFactoriesAndRepositories();
			CreateAndInitializeNewWindow();
		}

		private void SetupReversalFactoriesAndRepositories()
		{
			Assert.That(Cache, Is.Not.Null, "No cache yet!?");
			m_revIndexEntryFactory = Cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>();
			m_revIndexFactory = Cache.ServiceLocator.GetInstance<IReversalIndexFactory>();
			m_revIndexRepo = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
		}

		/// <summary>
		/// Any db object created here or in tests must be added to m_createdObjectList.
		/// </summary>
		protected void CreateTestData()
		{
			var rootMorphType = GetMorphTypeOrCreateOne("root");
			var adjPOS = GetGrammaticalCategoryOrCreateOne("adjective", Cache.LangProject.PartsOfSpeechOA);

			// REVIEW (Hasso) 2022.08: This ILexEntry is never used; is it necessary for the test?
			// le='pus' mtype='root' Sense1(gle='green' pos='adj.')
			AddLexeme(m_createdObjectList, "pus", rootMorphType, "green", adjPOS);

			m_revIndex = CreateReversalIndex();
		}

		protected IReversalIndex CreateReversalIndex()
		{
			Assert.That(m_revIndexFactory, Is.Not.Null, "Fixture Initialization is not complete.");
			Assert.That(m_window, Is.Not.Null, "No window.");

			//create a reversal index for this project.
			var wsObj = Cache.LanguageProject.DefaultAnalysisWritingSystem;
			var revIndex = m_revIndexRepo.FindOrCreateIndexForWs(wsObj.Handle);
			m_createdObjectList.Add(revIndex);
			return revIndex;
		}

		protected IReversalIndexEntry CreateAndAddReversalIndexEntry(IReversalIndex revIndex = null)
		{
			revIndex = revIndex ?? m_revIndex ?? (m_revIndex = CreateReversalIndex());
			Assert.That(m_revIndexEntryFactory, Is.Not.Null, "Fixture Initialization is not complete.");

			var revIndexEntry = m_revIndexEntryFactory.Create();
			m_createdObjectList.Add(revIndexEntry);
			revIndex.EntriesOC.Add(revIndexEntry);
			return revIndexEntry;
		}

		protected List<IReversalIndexEntry> AddSomeReversalEntries(int cRevEntries = 5)
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

		protected void ClearReversalEntries()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_revIndex.AllEntries.ForEach(e => e.Delete());
			});
		}

		[SetUp]
		public override void TestSetup()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => CreateAndAddReversalIndexEntry());
		}

		[TearDown]
		public override void TestTearDown()
		{
			UndoAllActions();
			// delete property table settings.
			m_propertyTable.RemoveLocalAndGlobalSettings();
			base.TestTearDown();
		}

		private void UndoAllActions()
		{
			// Often looping through Undo() is not enough because changing
			// 'CurrentContentControl' zaps undo stack!
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, DestroyTestData);
			m_createdObjectList.Clear();
		}

		protected void CreateAndInitializeNewWindow()
		{
			m_window = new MockFwXWindow(m_application, m_configFilePath); // (MockFwXApp)
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;
			m_propertyTable = m_window.PropTable;
			((MockFwXWindow)m_window).ClearReplacements();
			// delete property table settings.
			m_propertyTable.RemoveLocalAndGlobalSettings();
			ProcessPendingItems();
			m_window.LoadUI(m_configFilePath); // actually loads UI here.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateTestData);
		}

		/// <summary>
		/// Use m_createdObjectList to destroy all test data created in CreateTestData()
		/// and in tests.
		/// </summary>
		protected void DestroyTestData()
		{
			foreach (ICmObject obj in m_createdObjectList)
			{
				if (!obj.IsValidObject)
					continue; // owned object could have been deleted already by owner
				if (obj is ILexEntry || obj is IReversalIndexEntry)
					obj.Delete();
				// Some types won't get deleted directly (e.g. ILexSense, IMoMorphType, IPartOfSpeech),
				// but should get deleted by their owner.
			}
		}

		protected void ProcessPendingItems()
		{
			// used in CreateAndInitializeWindow() and in MasterRefresh()
			//m_mediator = m_window.Mediator;
			((MockFwXWindow)m_window).ProcessPendingItems();
		}

		#endregion Setup and Teardown
	}

	/// <remarks>
	/// Rick (2013.07): So far the tests in this file are very basic. I would suggest looking in BulkEditBarTests.cs to expand the capabilities of these tests.
	/// </remarks>
	[TestFixture]
	public class AllReversalEntriesRecordListTests : AllReversalEntriesRecordListTestBase
	{
		#region AllReversalEntriesRecordListTests tests

		/// <summary>
		/// This test was written for LT-14722 Stop Crash when clicking Reversal Indexes
		///
		/// A stack overflow was happening when the Init method for AllReversalEntriesRecordList
		/// was being called while the Clerk was null.
		///
		/// This is a very minimal test to verify that the fix made for LT-14722 is still in effect.
		/// </summary>
		[Test]
		public void AllReversalIndexes_Init_Test()
		{
			const string recordListXmlFrag = @"
<recordList owner='ReversalIndex' property='AllEntries'>
	<dynamicloaderinfo assemblyPath='LexEdDll.dll' class='SIL.FieldWorks.XWorks.LexEd.AllReversalEntriesRecordList' />
</recordList>";
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(recordListXmlFrag);
			XmlNode recordListNode = doc.DocumentElement;
			using (var list = new AllReversalEntriesRecordList())
			{
				list.Init(Cache, m_mediator, m_propertyTable, recordListNode);

				Assert.IsNull(list.OwningObject,
					"When AllReversalEntriesRecordList is called and the Clerk is null then the OwningObject should not be set, i.e. left as Null");
			}
		}

		#endregion AllReversalEntriesRecordListTests tests
	}
}
