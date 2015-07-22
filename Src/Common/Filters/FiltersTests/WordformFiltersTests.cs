using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using XCore;

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
			using (var mediator = new Mediator())
			using (var propertyTable = new PropertyTable(new MockPublisher()))
			{
				propertyTable.SetProperty("cache", Cache, true);
				flp.Init(mediator, propertyTable, null);
				wsf.Cache = Cache;
				andFilter.Add(wsf);
				flp.Filters.Add(wsf);
				flp.OnAdjustFilterSelection(andFilter);
			}
		}
	}
}
