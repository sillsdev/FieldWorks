// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using LanguageExplorer;
using LanguageExplorer.Areas;
using NUnit.Framework;
using SIL.Extensions;

namespace LanguageExplorerTests.Repository
{
	/// <summary>
	/// Test the AreaRepository.
	/// </summary>
	[TestFixture]
	internal class AreaRepositoryTests : MefTestBase
	{
		private IReadOnlyDictionary<string, IArea> _allAreas;

		/// <summary>
		/// Set up test fixture.
		/// </summary>
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			_allAreas = _areaRepository.AllAreasInOrder;
		}

		/// <summary>
		/// Tear down the test fixture.
		/// </summary>
		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			_allAreas = null;

			base.FixtureTeardown();
		}

		/// <summary>
		/// Doesn't have some unknown area.
		/// </summary>
		[Test]
		public void UnknownAreaNotPresent()
		{
			Assert.IsNull(_areaRepository.GetArea("bogusArea"));
		}

		/// <summary>
		/// Make sure the AreaRepository has the expected number of areas.
		/// </summary>
		[Test]
		public void AreaRepositoryHasAllExpectedAreas()
		{
			Assert.AreEqual(5, _allAreas.Count);
		}

		/// <summary>
		/// Make sure the AreaRepository has the areas in the right order.
		/// </summary>
		[TestCase(AreaServices.LexiconAreaUiName, 0, AreaServices.LexiconAreaMachineName)]
		[TestCase(AreaServices.TextAndWordsAreaUiName, 1, AreaServices.TextAndWordsAreaMachineName)]
		[TestCase(AreaServices.GrammarAreaUiName, 2, AreaServices.GrammarAreaMachineName)]
		[TestCase(AreaServices.NotebookAreaUiName, 3, AreaServices.NotebookAreaMachineName)]
		[TestCase(AreaServices.ListsAreaUiName, 4, AreaServices.ListsAreaMachineName)]
		public void AreaRepositoryHasAllToolsInCorrectOrder(string uiName, int idx, string expectedMachineName)
		{
			var area = _allAreas[uiName];
			Assert.AreEqual(idx, _allAreas.IndexOf(new KeyValuePair<string, IArea>(uiName, area)));
			Assert.AreEqual(expectedMachineName, area.MachineName);
		}
	}
}