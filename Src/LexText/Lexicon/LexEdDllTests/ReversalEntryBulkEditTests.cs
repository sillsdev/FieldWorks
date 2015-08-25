// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.XWorks.LexEd;

namespace LexEdDllTests
{
	[TestFixture]
	public class ReversalEntryBulkEditTests : MemoryOnlyBackendProviderTestBase
	{
		const string FieldName = "field";

		[Test]
		public void PropertyTableIdContainsWsId()
		{
			const string wsId = "en";
			IPublisher publisher;
			ISubscriber subscriber;
			PubSubSystemFactory.CreatePubSubSystem(out publisher, out subscriber);
			using (var propertyTable = PropertyTableFactory.CreatePropertyTable(publisher))
			using(var cache = CreateCache())
			using(var recordList = new TestReversalRecordList())
			{
				propertyTable.SetProperty("cache", cache, SettingsGroup.LocalSettings, false, false);
				recordList.InitializeFlexComponent(propertyTable, publisher, subscriber);
				recordList.Init(null);
				propertyTable.SetProperty("ReversalIndexPublicationLayout", "publishReversal" + wsId, true, false);

				var propTableId = recordList.GetPropertyTableId(FieldName);
				StringAssert.Contains(FieldName, propTableId);
				StringAssert.Contains(wsId, propTableId);
			}
		}

		[Test]
		public void PropertyTableIdReturnsNullIfNoActiveReversalIndex()
		{
			IPublisher publisher;
			ISubscriber subscriber;
			PubSubSystemFactory.CreatePubSubSystem(out publisher, out subscriber);
			using (var propertyTable = PropertyTableFactory.CreatePropertyTable(publisher))
			using (var recordList = new TestReversalRecordList())
			{
				recordList.InitializeFlexComponent(propertyTable, publisher, subscriber);
				Assert.Null(recordList.GetPropertyTableId(FieldName));
			}
		}

		class TestReversalRecordList : AllReversalEntriesRecordList
		{
			public TestReversalRecordList()
			{
			}

			public string GetPropertyTableId(string fieldName)
			{
				return PropertyTableId(fieldName);
			}
		}
	}
}
