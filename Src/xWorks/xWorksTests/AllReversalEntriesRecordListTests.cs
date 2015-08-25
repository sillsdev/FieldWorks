using System;
using System.Collections.Generic;
using System.Xml;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.XWorks.LexEd;

namespace SIL.FieldWorks.XWorks
{
	public class AllReversalEntriesRecordListTestBase : XWorksAppTestBase, IDisposable
	{
		protected IPropertyTable m_propertyTable;
		protected IPublisher m_publisher;
		protected ISubscriber m_subscriber;
		protected List<ICmObject> m_createdObjectList;

		private IReversalIndexEntryFactory m_revIndexEntryFactory;

		private IReversalIndexFactory m_revIndexFactory;
		private IReversalIndexRepository m_revIndexRepo;

		protected AllReversalEntriesRecordList m_allReversalEntriesRecordList;
		protected IReversalIndexEntry m_revEntry;
		protected IReversalIndex m_revIndex;


		#region IDisposable Section (pass Gendarme rules)
		~AllReversalEntriesRecordListTestBase()
		{
			Dispose(false);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_window != null)
				{
					m_window.Dispose();
				}
				if (m_propertyTable != null)
				{
					m_propertyTable.Dispose();
				}

				if (m_allReversalEntriesRecordList != null)
				{
					m_allReversalEntriesRecordList.Dispose();
				}
			}
			IsDisposed = true;

			m_propertyTable = null;
			m_publisher = null;
			m_subscriber = null;
			m_allReversalEntriesRecordList = null;
			m_window = null;
		}
		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		protected bool IsDisposed
		{
			get;
			private set;
		}
		#endregion IDisposable Section (pass Gendarme rules)

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
		/// This is done after the entire set of tests is run.
		/// </summary>
		[TestFixtureTearDown]
		public void ReveralEntriesFixtureTearDown()
		{
			Dispose();
		}

		/// <summary>
		/// This is done before the entire set of tests is run.
		/// </summary>
		[TestFixtureSetUp]
		public void ReveralEntriesFixtureInit()
		{
			SetupReversalFactoriesAndRepositories();
		}

		private void SetupReversalFactoriesAndRepositories()
		{
			Assert.True(Cache != null, "No cache yet!?");
			m_revIndexEntryFactory = Cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>();
			m_revIndexFactory = Cache.ServiceLocator.GetInstance<IReversalIndexFactory>();
			m_revIndexRepo = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Setup is done before each test is run.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Initialize()
		{
			PubSubSystemFactory.CreatePubSubSystem(out m_publisher, out m_subscriber);
			m_propertyTable = PropertyTableFactory.CreatePropertyTable(m_publisher);
			//Rick: So far the tests in this file are very basic.
			//I would suggest looking in BulkEditBarTests.cs to expand the capabilities of these tests.
			CreateAndInitializeNewWindow();
		}

		/// <summary>
		/// Any db object created here or in tests must be added to m_createdObjectList.
		/// </summary>
		protected void CreateTestData()
		{
			var rootMT = GetMorphTypeOrCreateOne("root");
			var adjPOS = GetGrammaticalCategoryOrCreateOne("adjective", Cache.LangProject.PartsOfSpeechOA);

			// le='pus' mtype='root' Sense1(gle='green' pos='adj.')
			var lexEntry = AddLexeme(m_createdObjectList, "pus", rootMT, "green", adjPOS);

			var revEntry = GetOrCreateReversalIndexEntry(m_createdObjectList);

			m_revIndex = AddReversalIndex(m_createdObjectList, revEntry);
		}

		protected IReversalIndex AddReversalIndex(List<ICmObject> addList, IReversalIndexEntry revIndexEntry)
		{
			Assert.IsNotNull(m_revIndexFactory, "Fixture Initialization is not complete.");
			Assert.IsNotNull(m_window, "No window.");

			//create a reversal index for this project.
			var wsObj = Cache.LanguageProject.DefaultAnalysisWritingSystem;
			IReversalIndex revIndex = m_revIndexRepo.FindOrCreateIndexForWs(wsObj.Handle);
			//Add an entry to the Reveral index
			revIndex.EntriesOC.Add(revIndexEntry);

			addList.Add(revIndex);
			return revIndex;
		}

		protected IReversalIndexEntry GetOrCreateReversalIndexEntry(List<ICmObject> addList)
		{
			Assert.IsNotNull(m_revIndexEntryFactory, "Fixture Initialization is not complete.");
			Assert.IsNotNull(m_window, "No window.");

			var revIndexEntry = m_revIndexEntryFactory.Create();

			addList.Add(revIndexEntry);
			return revIndexEntry;
		}

		/// <summary>
		/// Setup is done after each test is run.
		/// </summary>
		[TearDown]
		public void CleanUp()
		{
			UndoAllActions();
			m_propertyTable.Dispose();
			m_propertyTable = null;
			m_publisher = null;
			m_subscriber = null;
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
#if RANDYTODO
			m_window = new MockFwXWindow(m_application, m_configFilePath); // (MockFwXApp)
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;
			m_propertyTable = m_window.PropTable;
			((MockFwXWindow)m_window).ClearReplacements();
			ProcessPendingItems();
			m_window.LoadUI(m_configFilePath); // actually loads UI here.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateTestData);
#endif
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
				if (obj is IMoMorphType || obj is IPartOfSpeech)
					continue; // these don't need to be deleted between tests
				if (obj is ILexEntry)
					obj.Delete();
				// Some types won't get deleted directly (e.g. ILexSense),
				// but should get deleted by their owner.
			}
		}

		protected void ProcessPendingItems()
		{
#if RANDYTODO
			// used in CreateAndInitializeWindow() and in MasterRefresh()
			//m_mediator = m_window.Mediator;
			((MockFwXWindow)m_window).ProcessPendingItems();
#endif
		}

		#endregion Setup and Teardown
	}

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
			const string reversalIndexClerk = @"
<recordList owner='ReversalIndex' property='AllEntries'>
	<dynamicloaderinfo assemblyPath='LexEdDll.dll' class='SIL.FieldWorks.XWorks.LexEd.AllReversalEntriesRecordList' />
</recordList>";
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(reversalIndexClerk);
			XmlNode newNode = doc.DocumentElement;
			using (var list = new AllReversalEntriesRecordList())
			{
				list.InitializeFlexComponent(m_propertyTable, m_publisher, m_subscriber);

				Assert.IsNull(list.OwningObject,
					"When AllReversalEntriesRecordList is called and the Clerk is null then the OwningObject should not be set, i.e. left as Null");
			}
		}

		#endregion AllReversalEntriesRecordListTests tests
	}
}
