// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Original author: MarkS 2010-11-29 LanguageSetupTests.cs

using System;
using System.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using SIL.WritingSystems;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary/>
	[TestFixture]
	public class LanguageSetupTests : BaseTest
	{
		/// <summary>
		/// Enable isolated performance testing of LoadList, ensuring it does not take
		/// a long time.
		/// FWNX-495: Select Language for New Writing System dialog can block user
		/// taking forever to search
		/// </summary>
		[Test]
		[Category("ByHand")]
		public void LoadList_query_notTooLong()
		{
			var query = "en";
			var tooLong = TimeSpan.FromMinutes(1);
			using (var languageSetup = new LanguageSetup())
			{
				var beginTime = DateTime.Now;
				ReflectionHelper.CallMethod(languageSetup, "LoadList", new string[] { query });
				var endTime = DateTime.Now;
				var timeElapsed = endTime - beginTime;
				Assert.That(timeElapsed, Is.Not.GreaterThan(tooLong));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the LanguageSubtag property for a new writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LanguageSubtag_newWS()
		{
			var wsManager = new WritingSystemManager();
			using (var langSetup = new LanguageSetup())
			{
				langSetup.WritingSystemManager = wsManager;
				langSetup.StartedInModifyState = false;
				langSetup.LanguageName = "Monkey";
				LanguageSubtag subtag = langSetup.LanguageSubtag;
				Assert.AreEqual("mon", subtag.Code);

				var newSubtag = new LanguageSubtag("mon", "Moniker");
				CoreWritingSystemDefinition newWs = wsManager.Create(newSubtag, null, null, Enumerable.Empty<VariantSubtag>());
				wsManager.Set(newWs);
				subtag = langSetup.LanguageSubtag;
				Assert.AreEqual("aaa", subtag.Code, "Language code 'mon' should already be in use");
			}
		}
	}
}
