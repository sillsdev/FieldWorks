// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using LanguageExplorer.Controls.XMLViews;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure.Impl;

namespace LanguageExplorerTests.Controls.XMLViews
{

	[TestFixture]
	public class TestXmlViewsDataCache : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void SetAndAccessMultiStrings()
		{
			const int kflid = XMLViewsDataCache.ktagEditColumnBase;
			const int hvoRoot = 10578;
			var wsEng = Cache.WritingSystemFactory.GetWsFromStr("en");
			var xmlCache = new XMLViewsDataCache(Cache.MainCacheAccessor as ISilDataAccessManaged, true, new Dictionary<int, int>());
			var recorder = new Notifiee();
			xmlCache.AddNotification(recorder);
			Assert.AreEqual(0, xmlCache.get_MultiStringAlt(hvoRoot, kflid, wsEng).Length);
			Assert.AreEqual(0, recorder.Changes.Count);
			var test1 = TsStringUtils.MakeString("test1", wsEng);
			xmlCache.CacheMultiString(hvoRoot, kflid, wsEng, test1);
			Assert.AreEqual(0, recorder.Changes.Count);
			Assert.AreEqual(test1, xmlCache.get_MultiStringAlt(hvoRoot, kflid, wsEng));
			var test2 = TsStringUtils.MakeString("blah", wsEng);
			xmlCache.SetMultiStringAlt(hvoRoot, kflid, wsEng, test2);
			Assert.AreEqual(test2, xmlCache.get_MultiStringAlt(hvoRoot, kflid, wsEng));

			recorder.CheckChanges(new[] { new ChangeInformationTest(hvoRoot, kflid, wsEng, 0, 0) }, "expected PropChanged from setting string");
			xmlCache.RemoveNotification(recorder);
		}
	}
}