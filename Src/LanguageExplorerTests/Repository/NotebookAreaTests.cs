// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			_areaMachineName = AreaServices.NotebookAreaMachineName;

			base.FixtureSetup();
		}

		/// <summary>
		/// Tear down the test fixture.
		/// </summary>
		[TestFixtureTearDown]
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
		[TestCase(AreaServices.NotebookEditToolUiName, 0, AreaServices.NotebookEditToolMachineName)]
		[TestCase(AreaServices.NotebookBrowseToolUiName, 1, AreaServices.NotebookBrowseToolMachineName)]
		[TestCase(AreaServices.NotebookDocumentToolUiName, 2, AreaServices.NotebookDocumentToolMachineName)]
		public void AreaRepositoryHasAllNotebookToolsInCorrectOrder(string uiName, int idx, string expectedMachineName)
		{
			DoTests(uiName, idx, expectedMachineName);
		}
	}
}