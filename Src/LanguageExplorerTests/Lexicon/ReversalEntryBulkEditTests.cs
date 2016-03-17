// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using LanguageExplorer.Areas.Lexicon;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.FDOTests;

namespace LanguageExplorerTests.Lexicon
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
			using (var cache = CreateCache())
			{
				var reversalIndexRepository = cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
				var reversalIndex = reversalIndexRepository.AllInstances().FirstOrDefault();
				if (reversalIndex == null)
				{
					// Create one.
					reversalIndex = reversalIndexRepository.FindOrCreateIndexForWs(cache.DefaultAnalWs);
				}
				using (var recordList = new TestReversalRecordList(cache.ServiceLocator, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), reversalIndex))
				{
					propertyTable.SetProperty("cache", cache, SettingsGroup.LocalSettings, false, false);
					recordList.InitializeFlexComponent(new FlexComponentParameterObject(propertyTable, publisher, subscriber));
					propertyTable.SetProperty("ReversalIndexPublicationLayout", "publishReversal" + wsId, true, false);

					var propTableId = recordList.GetPropertyTableId(FieldName);
					StringAssert.Contains(FieldName, propTableId);
					StringAssert.Contains(wsId, propTableId);
				}
			}
		}

		class TestReversalRecordList : AllReversalEntriesRecordList
		{
			internal TestReversalRecordList(IFdoServiceLocator serviceLocator, ISilDataAccessManaged decorator, IReversalIndex reversalIndex)
				: base(serviceLocator, decorator, reversalIndex)
			{
			}

			public string GetPropertyTableId(string fieldName)
			{
				return PropertyTableId(fieldName);
			}
		}
	}
}
