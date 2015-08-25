using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.Filters
{
	[TestFixture]
	public class WordformFiltersTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{

		[Test]
		public void OnAdjustFilterSelection()
		{
			var flp = new WfiRecordFilterListProvider();
			var wfiset = Cache.ServiceLocator.GetInstance<IWfiWordSetFactory>().Create();
			Cache.LangProject.MorphologicalDataOA.TestSetsOC.Add(wfiset);
			var wf1 = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			wf1.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("kick", Cache.DefaultVernWs);
			wfiset.CasesRC.Add(wf1);
			var andFilter = new AndFilter();
			var wsf = new WordSetFilter(wfiset);
			IPublisher publisher;
			ISubscriber subscriber;
			PubSubSystemFactory.CreatePubSubSystem(out publisher, out subscriber);
			using (var propertyTable = PropertyTableFactory.CreatePropertyTable(publisher))
			{
				propertyTable.SetProperty("cache", Cache, true, true);
				flp.InitializeFlexComponent(propertyTable, publisher, subscriber);
				wsf.Cache = Cache;
				andFilter.Add(wsf);
				flp.Filters.Add(wsf);
				flp.OnAdjustFilterSelection(andFilter);
			}
		}
	}
}
