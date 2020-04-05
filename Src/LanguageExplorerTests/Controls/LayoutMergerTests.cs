// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using LanguageExplorer;
using LanguageExplorer.Controls;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.TestUtilities;
using SIL.Xml;

namespace LanguageExplorerTests.Controls
{
	[TestFixture]
	public class LayoutMergerTests
	{
		private Inventory m_inventory;
		private string m_testPathMerge;

		[TestFixtureSetUp]
		public void Setup()
		{
			var keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["layout"] = new[] { "class", "type", "name", "choiceGuid" };
			keyAttrs["group"] = new[] { "label" };
			keyAttrs["part"] = new[] { "ref" };

			m_testPathMerge = Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests", "Controls", "XMLViews", "LayoutMergerTestData");
			m_inventory = new Inventory(new[] { m_testPathMerge }, "*.fwlayout", "/LayoutInventory/*", keyAttrs, "InventoryMergeTests", "projectPath")
			{
				Merger = new LayoutMerger()
			};
		}

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
				Path.Combine(FwDirectoryFinder.SourceDirectory, "LanguageExplorerTests", "Controls", "XMLViews", "LayoutMergerTestData", "My_Stem-based_LexEntry_Layouts.xml")
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

		[Test]
		public void TestDupKeyOnMigration()
		{
			var newMaster = Path.Combine(m_testPathMerge, "NewMaster.xml");
			var newMasterDoc = XDocument.Load(newMaster);

			var user = Path.Combine(m_testPathMerge, "User.xml");
			var userDoc = XDocument.Load(user);
			var sourceFilePath = Path.Combine(m_testPathMerge, "LexSensePartsOutput.xml");
			var outDirFilePath = Path.Combine(Path.GetTempPath(), "LexSensePartsOutput.xml");
			if (File.Exists(outDirFilePath))
			{
				File.Delete(outDirFilePath);
			}
			File.Copy(sourceFilePath, outDirFilePath);
			var outputDoc = XDocument.Load(outDirFilePath);
			IOldVersionMerger merger = new LayoutMerger();

			var output = merger.Merge(newMasterDoc.Root, userDoc.Root, outputDoc, "");
			const string checkValue1 = "layout[@class='LexSense' and @type='jtview' and @name='publishRootSub']/part[@ref='SemanticDomainsConfig' and @label='Semantic Domains' and @dup='1-2.0.0']";
			AssertThatXmlIn.String(output.GetOuterXml()).HasSpecifiedNumberOfMatchesForXpath(checkValue1, 1);
			const string checkValue2 = "layout[@class='LexSense' and @type='jtview' and @name='publishRootSub']/part[@ref='SemanticDomainsConfig' and @label='Semantic Domains (1)' and @dup='1-2.0.0-1']";
			AssertThatXmlIn.String(output.GetOuterXml()).HasSpecifiedNumberOfMatchesForXpath(checkValue2, 1);
			const string checkValue3 = "layout[@class='LexSense' and @type='jtview' and @name='publishRootSub']/part[@ref='SemanticDomainsConfig' and @label='Semantic Domains (2)' and @dup='1-2.0.0-2']";
			AssertThatXmlIn.String(output.GetOuterXml()).HasSpecifiedNumberOfMatchesForXpath(checkValue3, 1);
		}

		private static void TestMerge(string newMaster, string user, string expectedOutput, string suffix)
		{
			var newMasterDoc = XDocument.Parse(newMaster);
			var userDoc = XDocument.Parse(user);
			var outputDoc = new XDocument();
			IOldVersionMerger merger = new LayoutMerger();
			var output = merger.Merge(newMasterDoc.Root, userDoc.Root, outputDoc, suffix);
			var expectedDoc = XDocument.Parse(expectedOutput);
			Assert.IsTrue(XmlUtils.NodesMatch(output, expectedDoc.Root));
		}

		[Test]
		public void LayoutAttrsCopied()
		{
			TestMerge(
				@"<layout type='jtview' name='publishStemMinorPara'></layout>",
				@"<layout></layout>",
				@"<layout type='jtview' name='publishStemMinorPara'></layout>",
				string.Empty);
		}

		[Test]
		public void NonPartChildrenCopied()
		{
			TestMerge(
			@"<layout>
				<generate someAttr='nonsence'></generate>
			</layout>",
			@"<layout>
			</layout>",
			@"<layout>
				<generate someAttr='nonsence'></generate>
			</layout>",
			string.Empty);
		}

		// Cur1 is output first because before any node that occurs in wanted.
		// Cur3 is output next because in wanted.
		// Cur4 is not in output, but follows cur3 in input.
		// Cur2 is then copied in expected output order.
		// Cur3 is then copied again, but without another copy of node 4.
		// Custom3 is then copied from output, since $child is special.
		// Custom1 is then copied from input, with following custom1.
		// Cur5 and 6 must follow "generate", but are put in output order.
		// Custom4 is output in the second group, as that's where it occurs in the input.
		[Test]
		public void PartsCopiedAndReordered()
		{
			TestMerge(
			@"<layout>
				<part ref='cur1'/>
				<part ref='cur2'/>
				<part ref='cur3'/>
				<part ref='cur4'/>
				<part ref='$child' label='custom1'/>
				<part ref='$child' label='custom2'/>
				<generate/>
				<part ref='cur5'/>
				<part ref='cur6'/>
				<part ref='$child' label='custom4'/>
		   </layout>",
			@"<layout>
				<part ref='cur6'/>
				<part ref='cur5'/>
				<part ref='cur3'/>
				<part ref='cur2'/>
				<part ref='cur3'/>
				<part ref='$child' label='custom3'/>
				<part ref='$child' label='custom1'/>
				<part ref='$child' label='custom4'/>
			</layout>",
			@"<layout>
				<part ref='cur1'/>
				<part ref='cur3'/>
				<part ref='cur4'/>
				<part ref='cur2'/>
				<part ref='cur3'/>
				<part ref='$child' label='custom3'/>
				<part ref='$child' label='custom1'/>
				<part ref='$child' label='custom2'/>
				<generate/>
				<part ref='cur6'/>
				<part ref='cur5'/>
				<part ref='$child' label='custom4'/>
			</layout>",
			string.Empty);
		}

		[Test]
		public void SpecialAttrsOverridden()
		{
			TestMerge(
			@"<layout>
				<part ref='cur1' before='' after='' ws='analysis' style='oldStyle' number='true' numstyle='0' numsingle='false' visibility='ifData' extra='somevalue'/>
			</layout>",
			@"<layout>
				<part ref='cur1' before=' (' after=') ' sep=', ' ws='xkal' style='newStyle' showLabels='true' number='false' numstyle='9' numsingle='true' visibility='never'/>
			</layout>",
			@"<layout>
				<part ref='cur1' before=' (' after=') ' sep=', ' ws='xkal' style='newStyle' showLabels='true' number='false' numstyle='9' numsingle='true' visibility='never' extra='somevalue'/>
			</layout>",
			string.Empty);
		}

		[Test]
		public void SenseInParaMigratedCorrectly()
		{
			TestMerge(
			@"<layout>
				<part ref='SensesConfig' label='Senses' param='publishRoot'/>
			</layout>",
			@"<layout>
				<part ref='SensesConfig' label='Senses' param='publishRoot_AsPara' flowType='divInPara'/>
			</layout>",
			@"<layout>
				<part ref='SensesConfig' label='Senses' param='publishRoot_AsPara' flowType='divInPara'/>
			</layout>",
			string.Empty);
		}

		[Test]
		public void UserCreatedConfigsPreserveParamSuffix()
		{
			TestMerge(
			@"<layout class='LexEntry' type='jtview' name='publishSummaryDefForStemVariantRef' extra='new'>
				<part ref='newRef' param='publishNewRefPart' />
			</layout>",
			@"<layout class='LexEntry' type='jtview' name='publishSummaryDefForStemVariantRef#Stem-980'>
				<part ref='oldRef' param='publishOldRefPart#Stem-980' />
			</layout>",
			@"<layout class='LexEntry' type='jtview' name='publishSummaryDefForStemVariantRef#Stem-980' extra='new'>
				<part ref='newRef' param='publishNewRefPart#Stem-980' />
			</layout>",
			"#Stem-980");
		}

		[Test]
		public void UserDuplicatedNodePreservesParamSuffix()
		{
			TestMerge(
			@"<layout class='LexExampleSentence' type='jtview' name='publishStem'>
				<part ref='TranslationsConfig' label='Translations' param='publishStem' css='translations' />
			</layout>",
			@"<layout class='LexExampleSentence' type='jtview' name='publishStem%01'>
				<part ref='TranslationsConfig' label='Translations' param='publishStem%01' css='translations' dup='1.0' />
			</layout>",
			@"<layout class='LexExampleSentence' type='jtview' name='publishStem%01'>
				<part ref='TranslationsConfig' label='Translations' param='publishStem%01' css='translations' dup='1.0' />
			</layout>",
			"%01");
		}

		[Test]
		public void UserDuplicatedNodePreservesPartRefLabelSuffix_Basic()
		{
			TestMerge(
			@"<layout name='publishStem'>
				<part ref='ExamplesConfig' label='Examples' param='publishStem' css='examples'/>
			</layout>",
			@"<layout name='publishStem'>
				<part ref='ExamplesConfig' label='Examples' param='publishStem' css='examples'/>
				<part ref='ExamplesConfig' label='Examples (1)' param='publishStem%01' css='examples' dup='1' />
			</layout>",
			@"<layout name='publishStem'>
				<part ref='ExamplesConfig' label='Examples' param='publishStem' css='examples'/>
				<part ref='ExamplesConfig' label='Examples (1)' param='publishStem%01' css='examples' dup='1' />
			</layout>",
			string.Empty);
		}

		[Test]
		public void UserDuplicatedNodePreservesPartRefLabelSuffix_Two()
		{
			TestMerge(
			@"<layout name='publishStem'>
				<part ref='ExamplesConfig' label='Examples' param='publishStem' css='examples'/>
			</layout>",
			@"<layout name='publishStem%01'>
				<part ref='ExamplesConfig' label='Examples (2)' param='publishStem%01' css='examples' dup='1' />
			</layout>",
			@"<layout name='publishStem%01'>
				<part ref='ExamplesConfig' label='Examples (2)' param='publishStem%01' css='examples' dup='1' />
			</layout>",
			"%01");
		}

		[Test]
		public void UserDuplicatedNodePreservesPartRefLabelSuffix_MoreComplicated()
		{
			TestMerge(
			@"<layout class='LexExampleSentence' type='jtview' name='publishStem'>
				<part ref='ExamplesConfig' label='Examples' param='publishStem' css='examples'/>
			</layout>",
			@"<layout class='LexExampleSentence' type='jtview' name='publishStem%01'>
				<part ref='ExamplesConfig' label='Examples (1) (1)' param='publishStem%01' css='examples' dup='1.1' />
			</layout>",
			@"<layout class='LexExampleSentence' type='jtview' name='publishStem%01'>
				<part ref='ExamplesConfig' label='Examples (1) (1)' param='publishStem%01' css='examples' dup='1.1' />
			</layout>",
			"%01");
		}

		/// <summary>
		/// LT-14650 XHTML output of configured dictionary showed up a bad layout merger
		/// </summary>
		[Test]
		public void SpecialAttrsOverridden_RelTypeSeqAndDup()
		{
			TestMerge(
			@"<layout>
				<part ref='LexReferencesConfig' label='Lexical Relations' before='' after='' sep=' ' visibility='ifData' param='publishStemSenseRef' css='relations' lexreltype='sense' extra='somevalue'/>
			</layout>",
			@"<layout>
				<part ref='LexReferencesConfig' label='Lexical Relations' before='' after=' ' sep='; ' visibility='ifdata' param='publishStemSenseRef' css='relations' lexreltype='sense' reltypeseq='+b7862f14-ea5e-11de-8d47-0013722f8dec,-b7921ac2-ea5e-11de-880d-0013722f8dec' unwanted='2' />
				<part ref='LexReferencesConfig' label='Lexical Relations (1)' before='' after=' ' sep='; ' visibility='ifdata' param='publishStemSenseRef%01' css='relations' lexreltype='sense' reltypeseq='-b7862f14-ea5e-11de-8d47-0013722f8dec,-b7921ac2-ea5e-11de-880d-0013722f8dec' unwanted='3' dup='1' />
			</layout>",
			@"<layout>
				<part ref='LexReferencesConfig' label='Lexical Relations' before='' after=' ' sep='; ' visibility='ifdata' param='publishStemSenseRef' css='relations' lexreltype='sense' extra='somevalue' reltypeseq='+b7862f14-ea5e-11de-8d47-0013722f8dec,-b7921ac2-ea5e-11de-880d-0013722f8dec' />
				<part ref='LexReferencesConfig' label='Lexical Relations (1)' before='' after=' ' sep='; ' visibility='ifdata' param='publishStemSenseRef%01' css='relations' lexreltype='sense' extra='somevalue' reltypeseq='-b7862f14-ea5e-11de-8d47-0013722f8dec,-b7921ac2-ea5e-11de-880d-0013722f8dec' dup='1' />
			</layout>",
			string.Empty);
		}

		/// <summary>
		/// LT-17178 Duplicates of child nodes do not appear when migrating from 8.2.4 to 8.3 build
		/// </summary>
		[Test]
		public void SpecialAttrsOverridden_DuplicateLocationNodes()
		{
			TestMerge(
			@"<layout>
				<part ref='LocationConfig' label='Location' before='' after=' ' visibility='ifdata' style='Dictionary-Contrasting' param='publishStemLocation' />
				<part ref='LocationConfig' label='Location (1)' before='' after=' ' visibility='ifdata' style='Dictionary-Contrasting' param='publishStemLocation%01' dup='1' />
			</layout>",
			@"<layout>
				<part ref='LocationConfig' label='Location' before='' after=' ' visibility='ifdata' style='Dictionary-Contrasting' param='publishStemLocation%01' dup='1.0' />
				<part ref='LocationConfig' label='Location (1)' before='' after=' ' visibility='ifdata' style='Dictionary-Contrasting' param='publishStemLocation%01' dup='1.0' />
			</layout>",
			@"<layout>
				<part ref='LocationConfig' label='Location' before='' after=' ' visibility='ifdata' style='Dictionary-Contrasting' param='publishStemLocation%01' dup='1.0' />
				<part ref='LocationConfig' label='Location (1)' before='' after=' ' visibility='ifdata' style='Dictionary-Contrasting' param='publishStemLocation%01' dup='1.0-1' />
			</layout>",
			string.Empty);
		}
	}
}