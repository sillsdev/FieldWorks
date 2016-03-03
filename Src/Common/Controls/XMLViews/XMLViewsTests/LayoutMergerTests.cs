using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;

namespace XMLViewsTests
{
	[TestFixture]
	public class LayoutMergerTests
	{
		Inventory m_inventory;

		[TestFixtureSetUp]
		public void Setup()
		{
			var keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["layout"] = new[] { "class", "type", "name", "choiceGuid" };
			keyAttrs["group"] = new[] { "label" };
			keyAttrs["part"] = new[] { "ref" };

			string testPathMerge = Path.Combine(FwDirectoryFinder.SourceDirectory, "Common/Controls/XMLViews/XMLViewsTests/LayoutMergerTestData");
			m_inventory = new Inventory(new[] { testPathMerge }, "*.fwlayout", "/LayoutInventory/*", keyAttrs, "InventoryMergeTests", "projectPath");
			m_inventory.Merger = new LayoutMerger();
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule", Justification = "MSDN says XmlNodeList is not disposable even though Gendarme claims it is.")]
		[Test]
		public void TestMergeCustomCopy()
		{
			Assert.AreEqual(1, m_inventory.GetElements("layout[@class='LexEntry' and @type='jtview' and @name='publishStemEntry']/sublayout[@name='publishStemPara']").Count(), "There should be one subentry from the original setup.");
			Assert.AreEqual(1, m_inventory.GetElements("layout[@class='LexEntry' and @type='jtview' and @name='publishStemPara']/part[@ref='MLHeadWordPub' and @before='']").Count(), "The original layout entry attributes have no value for before.");
			Assert.AreEqual(1, m_inventory.GetElements("layout[@class='LexSense' and @type='jtview' and @name='publishStem']/part[@ref='SmartDefinitionPub' and @before='']").Count(), "The original layout sense attributes have no value for before.");

			var cTypesOrig = m_inventory.GetLayoutTypes().Count;
			var cLayoutsOrig = m_inventory.GetElements("layout").Count();
			var files = new List<string>
			{
				Path.Combine(FwDirectoryFinder.SourceDirectory, "Common/Controls/XMLViews/XMLViewsTests/LayoutMergerTestData/My_Stem-based_LexEntry_Layouts.xml")
			};
			m_inventory.AddElementsFromFiles(files, 0, true);
			Assert.AreEqual(cTypesOrig + 1, m_inventory.GetLayoutTypes().Count, "The merge should have added one new layout type.");
			Assert.AreEqual(cLayoutsOrig + 8, m_inventory.GetElements("layout").Count(), "The merge should have added eight new layout elements.");

			Assert.AreEqual(1, m_inventory.GetElements("layout[@class='LexEntry' and @type='jtview' and @name='publishStemEntry']/sublayout[@name='publishStemPara']").Count(), "There should still be one subentry from the original setup.");
			Assert.AreEqual(1, m_inventory.GetElements("layout[@class='LexEntry' and @type='jtview' and @name='publishStemPara']/part[@ref='MLHeadWordPub' and @before='']").Count(), "The original layout entry attributes should not change.");
			Assert.AreEqual(1, m_inventory.GetElements("layout[@class='LexSense' and @type='jtview' and @name='publishStem']/part[@ref='SmartDefinitionPub' and @before='']").Count(), "The original layout sense attributes should not change.");

			Assert.AreEqual(1, m_inventory.GetElements("layout[@class='LexEntry' and @type='jtview' and @name='publishStemEntry#stem-785']/sublayout[@name='publishStemPara#Stem-785']").Count(), "There should be one subentry from the copied setup, with revised name.");
			Assert.AreEqual(1, m_inventory.GetElements("layout[@class='LexEntry' and @type='jtview' and @name='publishStemPara#stem-785']/part[@ref='MLHeadWordPub' and @before='Headword: ']").Count(), "The revised attributes for entry parts in the copy should pass through the merge.");
			Assert.AreEqual(1, m_inventory.GetElements("layout[@class='LexSense' and @type='jtview' and @name='publishStem#stem-785']/part[@ref='SmartDefinitionPub' and @before='Definition: ']").Count(), "The revised attributes for sense parts in the copy should pass through the merge.");
			// If we add some modifications to the standard layout types in additional data files, then more testing could be done on those values passing through...  But this demonstrates the fixes for https://jira.sil.org/browse/LT-15378.

			Assert.AreEqual(1, m_inventory.GetElements("layout[@class='LexEntry' and @type='jtview' and @name='publishStemMinorEntry#stem-785']/part[@ref='MinorEntryConfig' and @entrytypeseq='-b0000000-c40e-433e-80b5-31da08771344,+024b62c9-93b3-41a0-ab19-587a0030219a']").Count(), "The entrytypeseq attribute for entry parts in the copy should pass through the merge.");
			//Added above test case to handle entrytypeseq to fix https://jira.sil.org/browse/LT-16442
		}
	}
}
