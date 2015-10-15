// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.XWorks.LexEd;
using XCore;

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
			using(var mediator = new Mediator())
			using(var propertyTable = new PropertyTable(mediator))
			using(var recordList = new TestReversalRecordList(Cache, mediator, propertyTable))
			{
				propertyTable.SetProperty("ReversalIndexPublicationLayout", "publishReversal" + wsId, false);

				var propTableId = recordList.GetPropertyTableId(FieldName);
				StringAssert.Contains(FieldName, propTableId);
				StringAssert.Contains(wsId, propTableId);
			}
		}

		[Test]
		public void PropertyTableIdReturnsNullIfNoActiveReversalIndex()
		{
			using(var mediator = new Mediator())
			using(var propertyTable = new PropertyTable(mediator))
			{
				using(var recordList = new TestReversalRecordList(Cache, mediator, propertyTable))
				{
					Assert.Null(recordList.GetPropertyTableId(FieldName));
				}
			}
		}

		class TestReversalRecordList : AllReversalEntriesRecordList
		{
			public TestReversalRecordList(FdoCache cache, Mediator mediator, PropertyTable propertyTable)
			{
				Init(cache, mediator, propertyTable, null);
			}

			public string GetPropertyTableId(string fieldName)
			{
				return PropertyTableId(fieldName);
			}
		}
	}
}
