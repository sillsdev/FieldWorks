// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.CoreTests;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.Controls;

namespace XMLViewsTests
{

	[TestFixture]
	public class TestXmlViewsDataCache : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void SetAndAccessMultiStrings()
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();

			int kflid = XMLViewsDataCache.ktagEditColumnBase;
			int hvoRoot = 10578;
			int wsEng = Cache.WritingSystemFactory.GetWsFromStr("en");
			XMLViewsDataCache xmlCache = new XMLViewsDataCache(Cache.MainCacheAccessor as ISilDataAccessManaged, true, new Dictionary<int, int>());
			Notifiee recorder = new Notifiee();
			xmlCache.AddNotification(recorder);
			Assert.AreEqual(0, xmlCache.get_MultiStringAlt(hvoRoot, kflid, wsEng).Length);
			Assert.AreEqual(0, recorder.Changes.Count);
			ITsString test1 = tsf.MakeString("test1", wsEng);
			xmlCache.CacheMultiString(hvoRoot, kflid, wsEng, test1);
			Assert.AreEqual(0, recorder.Changes.Count);
			Assert.AreEqual(test1, xmlCache.get_MultiStringAlt(hvoRoot, kflid, wsEng));
			ITsString test2 = tsf.MakeString("blah", wsEng);
			xmlCache.SetMultiStringAlt(hvoRoot, kflid, wsEng, test2);
			Assert.AreEqual(test2, xmlCache.get_MultiStringAlt(hvoRoot, kflid, wsEng));


			recorder.CheckChanges(new ChangeInformationTest[] {new ChangeInformationTest(hvoRoot, kflid, wsEng, 0, 0)},
								  "expected PropChanged from setting string");
			xmlCache.RemoveNotification(recorder);

			// Enhance JohnT: a better test would verify that it doesn't intefere with other multistrings,
			// and that it can store stuff independently for different HVOs, tags, and WSs.
		}
	}
}
