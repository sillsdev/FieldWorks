// Copyright (c) 2022-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SIL.AlloGenModel;
using SIL.AlloGenService;

namespace SIL.AlloGenServiceTests
{
	[TestFixture]
	class PatternMatcherTest : FwTestBase
	{
		[Test]
		public void LoadFWDatabaseTest()
		{
			Assert.NotNull(myCache);
			var lexEntries = myCache.LangProject.LexDbOA.Entries;
			Assert.AreEqual(4530, lexEntries.Count());
			var lexEntriesWithOneAllomorph = lexEntries.Where(e => e.AllAllomorphs.Count() == 1);
			Assert.AreEqual(4480, lexEntriesWithOneAllomorph.Count());
		}

		[Test]
		public void LexEntriesPerMorphTypesTest()
		{
			var lexEntriesPerMorphTypes = patternMatcher.MatchMorphTypes(
				patternMatcher.EntriesWithNoAllomorphs,
				pattern
			);
			Assert.AreEqual(4360, lexEntriesPerMorphTypes.Count());
		}

		[Test]
		public void LexEntriesPerCategoriesTest()
		{
			// Verb
			pattern.Category.Guid = "86ff66f6-0774-407a-a0dc-3eeaf873daf7";
			var lexEntriesPerCategory = patternMatcher.MatchCategory(
				patternMatcher.EntriesWithNoAllomorphs,
				pattern
			);
			Assert.AreEqual(1491, lexEntriesPerCategory.Count());
			// transitive verb
			pattern.Category.Guid = "54712931-442f-42d5-8634-f12bd2e310ce";
			lexEntriesPerCategory = patternMatcher.MatchCategory(
				patternMatcher.EntriesWithNoAllomorphs,
				pattern
			);
			Assert.AreEqual(664, lexEntriesPerCategory.Count());
			// Intranitive verb
			pattern.Category.Guid = "4459ff09-9ee0-4b50-8787-ae40fd76d3b7";
			lexEntriesPerCategory = patternMatcher.MatchCategory(
				patternMatcher.EntriesWithNoAllomorphs,
				pattern
			);
			Assert.AreEqual(827, lexEntriesPerCategory.Count());
		}

		// Cannot get IVwPattern to work with tester
		//[Test]
		//public void LexEntriesPerMatchStringTest()
		//{
		//    var lexEntriesPerMatchString = pm.MatchMatchString(pm.SingleAllomorphs);
		//    Assert.AreEqual(1234, lexEntriesPerMatchString.Count());
		//}
		// Cannot get IVwPattern to work with tester
		//[Test]
		//public void LexEntriesWithAllosPerPatternTest()
		//{
		//    var lexEntriesWithAllosThatDoNotMatch = patternMatcher.MatchEntriesWithAllosPerPattern(operation, pattern);
		//    Assert.AreEqual(2, lexEntriesWithAllosThatDoNotMatch.Count());
		//}
	}
}
