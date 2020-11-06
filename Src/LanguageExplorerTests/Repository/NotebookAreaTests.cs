// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer;
using LanguageExplorer.Areas;
using NUnit.Framework;

namespace LanguageExplorerTests.Repository
{
	[TestFixture]
	internal class NotebookAreaTests : AreaTestBase
	{
		/// <summary>
		/// Set up test fixture.
		/// </summary>
		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			_areaMachineName = LanguageExplorerConstants.NotebookAreaMachineName;

			base.FixtureSetup();
		}

		/// <summary>
		/// Tear down the test fixture.
		/// </summary>
		[OneTimeTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
		}

		/// <summary>
		/// Make sure the Notebook area has the expected number of tools.
		/// </summary>
		[Test]
		public void NotebookAreaHasAllExpectedTools()
		{
			Assert.AreEqual(3, _myOrderedTools.Count);
		}

		/// <summary>
		/// Make sure the Notebook area has tools in the right order.
		/// </summary>
		[TestCase(LanguageExplorerConstants.NotebookEditToolUiName, 0, LanguageExplorerConstants.NotebookEditToolMachineName)]
		[TestCase(LanguageExplorerConstants.NotebookBrowseToolUiName, 1, LanguageExplorerConstants.NotebookBrowseToolMachineName)]
		[TestCase(LanguageExplorerConstants.NotebookDocumentToolUiName, 2, LanguageExplorerConstants.NotebookDocumentToolMachineName)]
		public void AreaRepositoryHasAllNotebookToolsInCorrectOrder(string uiName, int idx, string expectedMachineName)
		{
			DoTests(uiName, idx, expectedMachineName);
		}
	}
}