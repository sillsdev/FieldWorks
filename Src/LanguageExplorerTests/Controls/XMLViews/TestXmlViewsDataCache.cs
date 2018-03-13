// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using LanguageExplorer.Controls.XMLViews;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure.Impl;
using SIL.WritingSystems;

namespace LanguageExplorerTests.Controls.XMLViews
{

	[TestFixture]
	public class TestXmlViewsDataCache : MemoryOnlyBackendProviderTestBase
	{
		#region Overrides of LcmTestBase
		public override void FixtureSetup()
		{
			if (!Sldr.IsInitialized)
			{
				// initialize the SLDR
				Sldr.Initialize();
			}

			base.FixtureSetup();
		}

		public override void FixtureTeardown()
		{
			base.FixtureTeardown();

			if (Sldr.IsInitialized)
			{
				Sldr.Cleanup();
			}
		}
		#endregion

		[Test]
		public void SetAndAccessMultiStrings()
		{
			int kflid = XMLViewsDataCache.ktagEditColumnBase;
			int hvoRoot = 10578;
			int wsEng = Cache.WritingSystemFactory.GetWsFromStr("en");
			XMLViewsDataCache xmlCache = new XMLViewsDataCache(Cache.MainCacheAccessor as ISilDataAccessManaged, true, new Dictionary<int, int>());
			Notifiee recorder = new Notifiee();
			xmlCache.AddNotification(recorder);
			Assert.AreEqual(0, xmlCache.get_MultiStringAlt(hvoRoot, kflid, wsEng).Length);
			Assert.AreEqual(0, recorder.Changes.Count);
			ITsString test1 = TsStringUtils.MakeString("test1", wsEng);
			xmlCache.CacheMultiString(hvoRoot, kflid, wsEng, test1);
			Assert.AreEqual(0, recorder.Changes.Count);
			Assert.AreEqual(test1, xmlCache.get_MultiStringAlt(hvoRoot, kflid, wsEng));
			ITsString test2 = TsStringUtils.MakeString("blah", wsEng);
			xmlCache.SetMultiStringAlt(hvoRoot, kflid, wsEng, test2);
			Assert.AreEqual(test2, xmlCache.get_MultiStringAlt(hvoRoot, kflid, wsEng));


			recorder.CheckChanges(new[] {new ChangeInformationTest(hvoRoot, kflid, wsEng, 0, 0)},
								  "expected PropChanged from setting string");
			xmlCache.RemoveNotification(recorder);

			// Enhance JohnT: a better test would verify that it doesn't intefere with other multistrings,
			// and that it can store stuff independently for different HVOs, tags, and WSs.
		}
	}
}
