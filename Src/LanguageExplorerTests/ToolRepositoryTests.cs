// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Windows.Forms.VisualStyles;
using LanguageExplorer;
using LanguageExplorer.Impls;
using NUnit.Framework;

namespace LanguageExplorerTests
{
	/// <summary>
	/// Test the ToolRepository.
	/// </summary>
	[TestFixture]
	public class ToolRepositoryTests
	{
		private IToolRepository _toolRepository;
		private IAreaRepository _areaRepository;

		/// <summary>
		/// Set up test fixture.
		/// </summary>
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			_toolRepository = new ToolRepository();
			_areaRepository = new AreaRepository(_toolRepository);
		}

		/// <summary>
		/// Tear down the test fixture.
		/// </summary>
		[TestFixtureTearDown]
		public void FixtureTeardown()
		{
			_toolRepository = null;
			_areaRepository = null;
		}

		/// <summary>
		/// Doesn't have some unknown tool.
		/// </summary>
		[Test]
		public void UnknownToolNotPresent()
		{
			Assert.IsNull(_toolRepository.GetTool("bogusTool"));
		}

		/// <summary>
		/// Has all tools for each area.
		/// </summary>
		[Test]
		public void HasAllExpectedToolsForEachArea()
		{
			var myToolNamesInOrder = new List<string>
				{
					"lexiconEdit",
					"lexiconBrowse",
					"lexiconDictionary",
					"rapidDataEntry",
					"lexiconClassifiedDictionary",
					"bulkEditEntriesOrSenses",
					"reversalEditComplete",
					"reversalBulkEditReversalEntries"
				};
			var currentAreaName = "lexicon";
			TestArea(currentAreaName, myToolNamesInOrder);

			currentAreaName = "textAndWords";
			myToolNamesInOrder = new List<string>
				{
					"interlinearEdit",
					"concordance",
					"complexConcordance",
					"wordListConcordance",
					"Analyses",
					"bulkEditWordforms",
					"corpusStatistics"
				};
			TestArea(currentAreaName, myToolNamesInOrder);

			currentAreaName = "grammar";
			myToolNamesInOrder = new List<string>
				{
					"posEdit",
					"categoryBrowse",
					"compoundRuleAdvancedEdit",
					"phonemeEdit",
					"phonologicalFeaturesAdvancedEdit",
					"bulkEditPhonemes",
					"naturalClassEdit",
					"EnvironmentEdit",
					"PhonologicalRuleEdit",
					"AdhocCoprohibitionRuleEdit",
					"featuresAdvancedEdit",
					"ProdRestrictEdit",
					"grammarSketch",
					"lexiconProblems"
				};
			TestArea(currentAreaName, myToolNamesInOrder);

			currentAreaName = "notebook";
			myToolNamesInOrder = new List<string>
				{
					"notebookEdit",
					"notebookBrowse",
					"notebookDocument"
				};
			TestArea(currentAreaName, myToolNamesInOrder);

			currentAreaName = "lists";
			myToolNamesInOrder = new List<string>
				{
					"domainTypeEdit",
					"anthroEdit",
					"complexEntryTypeEdit",
					"confidenceEdit",
					"chartmarkEdit",
					"charttempEdit",
					"educationEdit",
					"roleEdit",
					"featureTypesAdvancedEdit",
					"genresEdit",
					"lexRefEdit",
					"locationsEdit",
					"publicationsEdit",
					"morphTypeEdit",
					"peopleEdit",
					"positionsEdit",
					"restrictionsEdit",
					"semanticDomainEdit",
					"senseTypeEdit",
					"statusEdit",
					"textMarkupTagsEdit",
					"translationTypeEdit",
					"usageTypeEdit",
					"variantEntryTypeEdit",
					"recTypeEdit",
					"timeOfDayEdit",
					"reversalToolReversalIndexPOS"
				};
			TestArea(currentAreaName, myToolNamesInOrder);
		}

		private void TestArea(string currentAreaName, IList<string> myToolNamesInOrder)
		{
			var currentArea = _areaRepository.GetArea(currentAreaName);
			var myToolsInOrderFromToolRepository = _toolRepository.AllToolsForAreaInOrder(myToolNamesInOrder,
				currentArea.MachineName);
			var myToolsInOrderFromArea = currentArea.AllToolsInOrder;
			CollectionAssert.AreEqual(myToolsInOrderFromToolRepository, myToolsInOrderFromArea);
		}
	}
}