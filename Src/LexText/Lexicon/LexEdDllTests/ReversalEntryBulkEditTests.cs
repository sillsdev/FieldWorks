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
			using(var mediator = GenerateMediator())
			using(var recordList = new TestReversalRecordList(Cache, mediator))
			{
				mediator.PropertyTable.SetProperty("ReversalIndexPublicationLayout", "publishReversal" + wsId);

				var propTableId = recordList.GetPropertyTableId(FieldName);
				StringAssert.Contains(FieldName, propTableId);
				StringAssert.Contains(wsId, propTableId);
			}
		}

		[Test]
		public void PropertyTableIdReturnsNullIfNoActiveReversalIndex()
		{
			using (var mediator = GenerateMediator())
			using(var recordList = new TestReversalRecordList(Cache, mediator))
			{
				Assert.Null(recordList.GetPropertyTableId(FieldName));
			}
		}

		private Mediator GenerateMediator()
		{
			var mediator = new Mediator();
			mediator.PropertyTable.SetProperty("cache", Cache);
			return mediator;
		}

		class TestReversalRecordList : AllReversalEntriesRecordList
		{
			public TestReversalRecordList(FdoCache cache, Mediator mediator)
			{
				Init(cache, mediator, null);
			}

			public string GetPropertyTableId(string fieldName)
			{
				return PropertyTableId(fieldName);
			}
		}
	}
}
