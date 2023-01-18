// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer;
using LanguageExplorer.Areas;
using LanguageExplorer.Filters;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;

namespace LanguageExplorerTests.Filters
{
	[TestFixture]
	public class WordformFiltersTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void OnAdjustFilterSelection()
		{
			IRecordFilterListProvider flp = new WfiRecordFilterListProvider();
			var wfiset = Cache.ServiceLocator.GetInstance<IWfiWordSetFactory>().Create();
			Cache.LangProject.MorphologicalDataOA.TestSetsOC.Add(wfiset);
			var wf1 = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			wf1.Form.VernacularDefaultWritingSystem = TsStringUtils.MakeString("kick", Cache.DefaultVernWs);
			wfiset.CasesRC.Add(wf1);
			var andFilter = new AndFilter();
			var wsf = new WordSetFilter(wfiset);
			var flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			try
			{
				flexComponentParameters.PropertyTable.SetProperty("cache", Cache);
				flp.InitializeFlexComponent(flexComponentParameters);
				wsf.Cache = Cache;
				andFilter.Add(wsf);
				flp.Filters.Add(wsf);
				flp.AdjustFilterSelection(andFilter);
			}
			finally
			{
				TestSetupServices.DisposeTrash(flexComponentParameters);
			}
		}
	}
}