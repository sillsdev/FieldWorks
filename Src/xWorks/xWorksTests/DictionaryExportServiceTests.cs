// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using Palaso.IO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class DictionaryExportServiceTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void GetCountsOfReversalIndexes_Works()
		{
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");

			var revIndexFr = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(wsFr);
			var revIndexEn = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(wsEn);

			revIndexFr.FindOrCreateReversalEntry("blah fr");
			revIndexFr.FindOrCreateReversalEntry("blah fr2");
			revIndexFr.FindOrCreateReversalEntry("blah fr3");
			revIndexEn.FindOrCreateReversalEntry("blah en");
			revIndexEn.FindOrCreateReversalEntry("blah en2");

			var selectedReversalIndexes = new List<string>{ revIndexFr.ShortName, revIndexEn.ShortName };

			// SUT
			var result = DictionaryExportService.GetCountsOfReversalIndexes(Cache, selectedReversalIndexes);

			Assert.That(result.Keys.Count, Is.EqualTo(2), "Wrong number of languages provided");
			Assert.That(result["English"], Is.EqualTo(2), "Wrong number of English reversal index entries");
			Assert.That(result["French"], Is.EqualTo(3), "Wrong number of French reversal index entries");
		}
	}
}