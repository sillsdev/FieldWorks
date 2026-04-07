// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.XWorks;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using XCore;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.IText
{
	[TestFixture]
	public class InterlinearTextRecordClerkTests : MemoryOnlyBackendProviderTestBase, IDisposable
	{
		private const int InterestingTextsFlid = 899800;

		private IStText m_stText;
		private MockFwXApp m_application;
		private MockFwXWindow m_window;
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;

		[SetUp]
		public void SetUpTest()
		{
			ResetClerkProperties();
		}

		[TearDown]
		public void TearDownTest()
		{
			ResetClerkProperties();
		}

		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, DoFixtureSetup);
		}

		private void DoFixtureSetup()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;
			m_propertyTable = m_window.PropTable;
		}

		#region disposal
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				m_application?.Dispose();
				m_window?.Dispose();
				m_propertyTable?.Dispose();
			}
			m_application = null;
			m_window = null;
			m_propertyTable = null;
		}

		~InterlinearTextRecordClerkTests()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}
		#endregion disposal

		private void ResetClerkProperties()
		{
			if (m_propertyTable == null || m_propertyTable.IsDisposed)
				return;

			m_propertyTable.RemoveProperty("ActiveClerk");
			m_propertyTable.RemoveProperty("OldActiveClerk");
		}

		[Test]
		public void CreateStTextShouldAlsoCreateDsConstChart()
		{
			using (var interlinTextRecordClerk = new InterlinearTextRecordClerkDerived(m_mediator, m_propertyTable))
			{
				interlinTextRecordClerk.InitializeList(Cache);
				var discourseData = Cache.LangProject.DiscourseDataOA;
				Assert.That(discourseData, Is.Null);
				interlinTextRecordClerk.CreateStText(Cache);
				Assert.That(Cache.LangProject.DiscourseDataOA.ChartsOC.Any(), Is.True);
			}
		}

		[Test]
		public void SetInterestingTexts_WhenListShrinks_ReloadUpdatesSortedObjects()
		{
			var texts = CreateInterlinearTexts(5);
			var interestingTexts = InterestingTextsDecorator.GetInterestingTextList(m_mediator, m_propertyTable, Cache.ServiceLocator);
			interestingTexts.SetInterestingTexts(texts);

			using (var interlinTextRecordClerk = new InterlinearTextRecordClerkDerived(m_mediator, m_propertyTable))
			{
				interlinTextRecordClerk.InitializeList(Cache);
				interlinTextRecordClerk.List.ReloadList();

				Assert.That(interlinTextRecordClerk.SortedObjectHvos, Is.EqualTo(texts.Select(text => text.Hvo).ToArray()),
					"Precondition: the interlinear list should initially contain all core texts");

				var keptTexts = texts.Take(2).ToArray();

				interestingTexts.SetInterestingTexts(keptTexts);
				interlinTextRecordClerk.ReloadIfNeeded();

				Assert.That(interestingTexts.InterestingTexts.Select(text => text.Hvo).ToArray(), Is.EqualTo(keptTexts.Select(text => text.Hvo).ToArray()),
					"Sanity check: the InterestingTextList model should reflect the shrink");
				Assert.That(interlinTextRecordClerk.SortedObjectHvos, Is.EqualTo(keptTexts.Select(text => text.Hvo).ToArray()),
					"Reloading the interlinear clerk should rebuild SortedObjects from the updated InterestingTexts list");
			}
		}

		[Test]
		public void InterestingTextsDecorator_RequeriesAfterNotificationDrop_DoesNotReturnStaleTexts()
		{
			var texts = CreateInterlinearTexts(5);
			var interestingTexts = InterestingTextsDecorator.GetInterestingTextList(m_mediator, m_propertyTable, Cache.ServiceLocator);
			interestingTexts.SetInterestingTexts(texts);

			var decorator = new InterestingTextsDecorator(Cache.MainCacheAccessor as ISilDataAccessManaged, null, Cache.ServiceLocator);
			decorator.SetMediator(m_mediator, m_propertyTable);
			decorator.SetRootHvo(Cache.LangProject.Hvo);

			var dummyNotifiee = new DummyNotifyChange();
			decorator.AddNotification(dummyNotifiee);

			Assert.That(decorator.VecProp(Cache.LangProject.Hvo, InterestingTextsFlid),
				Is.EqualTo(texts.Select(text => text.Hvo).ToArray()),
				"Precondition: the decorator should cache the current interesting texts");

			decorator.RemoveNotification(dummyNotifiee);

			var keptTexts = texts.Take(2).ToArray();
			interestingTexts.SetInterestingTexts(keptTexts);

			Assert.That(decorator.VecProp(Cache.LangProject.Hvo, InterestingTextsFlid),
				Is.EqualTo(keptTexts.Select(text => text.Hvo).ToArray()),
				"After dropping and later reusing notifications, the decorator should not keep serving stale cached texts");
		}

		[Test]
		public void InterestingTextsDecorator_QueryWithoutNotifiees_DoesNotResubscribe()
		{
			var texts = CreateInterlinearTexts(5);
			var interestingTexts = InterestingTextsDecorator.GetInterestingTextList(m_mediator, m_propertyTable, Cache.ServiceLocator);
			interestingTexts.SetInterestingTexts(texts);

			var decorator = new InterestingTextsDecorator(Cache.MainCacheAccessor as ISilDataAccessManaged, null, Cache.ServiceLocator);
			decorator.SetMediator(m_mediator, m_propertyTable);
			decorator.SetRootHvo(Cache.LangProject.Hvo);

			var dummyNotifiee = new DummyNotifyChange();
			decorator.AddNotification(dummyNotifiee);
			Assert.That(decorator.VecProp(Cache.LangProject.Hvo, InterestingTextsFlid),
				Is.EqualTo(texts.Select(text => text.Hvo).ToArray()),
				"Precondition: the decorator should serve the initial interesting texts");

			decorator.RemoveNotification(dummyNotifiee);
			Assert.That(GetInterestingTextsChangedSubscriptionFlag(decorator), Is.False,
				"After the last notifiee is removed, the decorator should drop its InterestingTextsChanged subscription");

			var keptTexts = texts.Take(2).ToArray();
			interestingTexts.SetInterestingTexts(keptTexts);

			Assert.That(decorator.VecProp(Cache.LangProject.Hvo, InterestingTextsFlid),
				Is.EqualTo(keptTexts.Select(text => text.Hvo).ToArray()),
				"A direct query should still refresh the cached texts even when there are no active notifiees");
			Assert.That(GetInterestingTextsChangedSubscriptionFlag(decorator), Is.False,
				"Refreshing the cache without notifiees should not reattach the InterestingTextsChanged event handler");
		}

		private static bool GetInterestingTextsChangedSubscriptionFlag(InterestingTextsDecorator decorator)
		{
			return (bool)typeof(InterestingTextsDecorator)
				.GetField("m_isSubscribedToInterestingTextsChanged", BindingFlags.Instance | BindingFlags.NonPublic)
				.GetValue(decorator);
		}

		private List<IStText> CreateInterlinearTexts(int count)
		{
			var createdTexts = new List<IStText>(count);
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var textFactory = Cache.ServiceLocator.GetInstance<ITextFactory>();
				var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
				for (var i = 0; i < count; i++)
				{
					var text = textFactory.Create();
					var stText = stTextFactory.Create();
					text.ContentsOA = stText;
					createdTexts.Add(stText);
				}
			});

			return createdTexts;
		}

		#region Test Classes
		private class InterlinearTextRecordClerkDerived : InterlinearTextsRecordClerk
		{
			private const string InterlinearRecordListXml = "<recordList owner=\"LangProject\" property=\"InterestingTexts\"><decoratorClass assemblyPath=\"xWorks.dll\" class=\"SIL.FieldWorks.XWorks.InterestingTextsDecorator\" /></recordList>";

			public InterlinearTextRecordClerkDerived(Mediator mediator, PropertyTable propertyTable)
			{
				m_mediator = mediator;
				m_propertyTable = propertyTable;
				m_id = "interlinearTexts";
				m_fIsActiveInGui = true;
			}

			public RecordList List => m_list;

			public int[] SortedObjectHvos => m_list.SortedObjects.Cast<IManyOnePathSortItem>().Select(item => item.RootObjectHvo).ToArray();

			public void InitializeList(LcmCache cache)
			{
				if (m_list != null)
					return;

				var xmlDoc = new XmlDocument();
				xmlDoc.LoadXml(InterlinearRecordListXml);
				m_list = RecordList.Create(cache, m_mediator, m_propertyTable, xmlDoc.DocumentElement);
				typeof(RecordList).GetProperty("Clerk", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
					.SetValue(m_list, this, null);
				m_propertyTable.SetProperty("ActiveClerk", this, false);
			}

			public void CreateStText(LcmCache cache)
			{
				InitializeList(cache);
				CreateAndInsertStText createAndInsertStText = new NonUndoableCreateAndInsertStText(cache, this);
				createAndInsertStText.Create();
			}
		}
		#endregion Test Classes

		private sealed class DummyNotifyChange : IVwNotifyChange
		{
			public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
			{
			}
		}
	}
}