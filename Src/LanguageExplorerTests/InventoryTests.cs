// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Xml.Linq;
using System.Xml.XPath;
using FieldWorks.TestUtilities;
using LanguageExplorer;
using NUnit.Framework;
using SIL.Xml;

namespace LanguageExplorerTests
{
	/// <summary>
	/// Summary description for XmlUtilsTest.
	/// </summary>
	[TestFixture]
	public class InventoryTests : TestBaseForTestsThatCreateTempFilesBasedOnResources
	{
		Inventory _inventory;

		#region Overrides of TestBaseForTestsThatCreateTempFilesBasedOnResources

		/// <inheritdoc />
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			var keyAttrs = new Dictionary<string, string[]>
			{
				["layout"] = new[] { "class", "type", "mode", "name" },
				["group"] = new[] { "label" },
				["part"] = new[] { "ref" }
			};

			var testPathBase = CreateTempTestFiles(typeof(LanguageExplorerTestsResources), "InventoryBaseTestFiles");
			var testPathLater = CreateTempTestFiles(typeof(LanguageExplorerTestsResources), "InventoryLaterTestFiles");
			_inventory = new Inventory(new[] {testPathBase, testPathLater}, "*Layouts.xml", "/layoutInventory/*", keyAttrs, "InventoryTests", "projectPath");

			base.FixtureSetup();
		}

		/// <inheritdoc />
		protected override ResourceManager ResourceMgr => LanguageExplorerTestsResources.ResourceManager;

		#endregion

		private XElement CheckNode(string name, string[] keyvals, string target)
		{
			var node = _inventory.GetElement(name, keyvals);
			Check(node, target);
			return node;
		}

		private static void Check(XElement element, string target)
		{
			if (element == null)
			{
				Assert.IsNotNull(element, "expected node not found: " + target);
			}
			var match = element.Attribute("match");
			if (match == null)
			{
				Assert.IsNotNull(match, "expected node lacks match attr: " + target);
			}
			Assert.AreEqual(target, element.Attribute("match").Value);
		}

		private void CheckBaseNode(string name, string[] keyvals, string target)
		{
			Check(_inventory.GetBase(name, keyvals), target);
		}

		private void CheckAlterationNode(string name, string[] keyvals, string target)
		{
			Check(_inventory.GetAlteration(name, keyvals), target);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void MainTest()
		{
			// Test loading a file and confirm expected elements present.
			// Also checks that elements not replaced survive.
			CheckNode("layout", new[] {"LexSense", "jtview", null, "Test4"}, "test7");
			CheckNode("part", new[] {"nonsense"}, "test5");

			// Check that nodes from both test files are loaded.
			CheckNode("layout", new[] {"LexSense", "jtview", null, "Test3"}, "test6");

			// Test loading a second file with some new and some duplicate elements.
			// (b) new elements are present;
			CheckNode("layout", new[] {"LexEntry", "jtview", null, "Test3D"}, "test3D");
			// (c) appropriate elements are replaced (not unchanged or duplicated)
			CheckNode("layout", new[] {"LexSense", "jtview", null, "Test2"}, "test2D");
		}

		private static void VerifyAttr(XElement element, string attr, string val)
		{
			Assert.AreEqual(val, XmlUtils.GetOptionalAttributeValue(element, attr));
		}

		// Verifies that parent's index'th child has the specified value for the specified attribute.
		// Returns the child.
		private static XElement VerifyChild(XElement parent, int index, string attr, string val)
		{
			Assert.IsTrue(parent.Elements().Count() > index);
			var child = parent.Elements().ToList()[index];
			VerifyAttr(child, attr, val);
			return child;
		}

		[Test]
		public void DerivedElements()
		{
			// Check that we can get a base node for this view BEFORE we have retrieved anything about the layout
			// (a derived one) on which it is based.
			CheckBaseNode("layout", new[] {"LexEntry", "jtview", null, "Test5D"}, "test1D");
			// Load a file involving derived elements.
			// Check that we can retrieve the base, derived, and unified versions of the elements.
			var unified = CheckNode("layout", new[] {"LexEntry", "jtview", null, "Test1D"}, "test1D"); // unified
			CheckBaseNode("layout", new[] {"LexEntry", "jtview", null, "Test1D"}, "test3"); // baseNode
			CheckAlterationNode("layout", new[] {"LexEntry", "jtview", null, "Test1D"}, "test1D"); // derived
			Assert.IsNull(_inventory.GetAlteration("layout", new[] {"LexEntry", "jtview", null, "Test1"}), "GetAlteration should be null for non-derived node.");

			// Check correct working of unification:
			// - first main child is present as expected
			var groupMain = unified.Elements().ToList()[0];
			Assert.AreEqual("group", groupMain.Name.LocalName, "first child of unified should be a group");
			Assert.AreEqual(3, groupMain.Elements().Count(), "main group should have three chidren");
			Assert.AreEqual("main", XmlUtils.GetOptionalAttributeValue(groupMain, "label"), "first child should be group 'main'");
			// - added elements are added. (Also checks default order: original plus extras.)
			// - unmatched original elements are left alone.
			var part0M = VerifyChild(groupMain, 0, "ref", "LexEntry-Jt-Citationform"); // part0M
			VerifyChild(groupMain, 1, "ref", "LexEntry-Jt-Senses"); // part1M
			VerifyChild(groupMain, 2, "ref", "LexEntry-Jt-Forms"); // part2M

			// - child elements are correctly ordered when 'reorder' is true.
			var groupSecond = unified.Elements().ToList()[1];
			Assert.AreEqual("group", groupSecond.Name.LocalName, "second child of unified should be a group");
			Assert.AreEqual(3, groupSecond.Elements().Count(), "main group should have three chidren");
			Assert.AreEqual("second", XmlUtils.GetOptionalAttributeValue(groupSecond, "label"), "second child should be group 'second'");
			VerifyChild(groupSecond, 0, "ref", "LexEntry-Jt-Forms"); // part0S
			var part1S = VerifyChild(groupSecond, 1, "ref", "LexEntry-Jt-Citationform"); // part1S
			VerifyChild(groupSecond, 2, "ref", "LexEntry-Jt-Senses"); // part2S

			// - check no reordering when no element added, and reorder is false
			var groupThird = unified.Elements().ToList()[2];
			Assert.AreEqual("group", groupThird.Name.LocalName, "Third child of unified should be a group");
			Assert.AreEqual(3, groupThird.Elements().Count(), "main group should have three chidren");
			Assert.AreEqual("third", XmlUtils.GetOptionalAttributeValue(groupThird, "label"), "third child should be group 'Third'");
			VerifyChild(groupThird, 0, "ref", "LexEntry-Jt-Citationform");
			VerifyChild(groupThird, 1, "ref", "LexEntry-Jt-Senses");
			VerifyChild(groupThird, 2, "ref", "LexEntry-Jt-Forms");

			// - added attributes are added.
			VerifyAttr(part0M, "part", "default");
			VerifyAttr(part1S, "part", "default");
			VerifyAttr(unified, "dummy", "true");
			// - overridden attributes are replaced.
			VerifyAttr(part0M, "ws", "technical");
			VerifyAttr(part1S, "ws", "technical");
			VerifyAttr(unified, "visible", "never");
			// - attributes not mentioned in override are unchanged.
			VerifyAttr(part0M, "visible", "always");
			VerifyAttr(part1S, "visible", "always");
			VerifyAttr(unified, "ws", "vernacular");
		}

		/// <summary>
		/// Original base:
		/// 	<layout class="LexSense" type="jtview" name="Test1" match="test1">
		///			<group label="main" ws="vernacular" rubbish="nonsense" dummy="base">
		///			</group>
		///		</layout>
		/// Override:
		/// 	<layout class="LexSense" type="jtview" name="Test1" match="test7D" base="Test1" visible="never" dummy="true">
		///			<group label="main" ws="analysis">
		///			</group>
		///		</layout>
		/// Derived from override:
		/// 	<layout class="LexSense" type="jtview" name="Test8D" match="test8D" base="Test1">
		/// 		<group label="main" rubbish="goodstuff">
		/// 		</group>
		/// 	</layout>
		/// </summary>
		[Test]
		public void Overrides()
		{
			// Check that we can find a base node for a layout derived from an override.
			CheckBaseNode("layout", new[] {"LexSense", "jtview", null, "Test8D"}, "test7D");
			CheckAlterationNode("layout", new[] {"LexSense", "jtview", null, "Test8D"}, "test8D");
			var unified = CheckNode("layout", new[] {"LexSense", "jtview", null, "Test8D"}, "test8D");
			var groupMain = unified.Elements().ToList()[0];
			Assert.AreEqual("group", groupMain.Name.LocalName, "first child of unified should be a group");
			Assert.AreEqual(0, groupMain.Elements().Count(), "main group should have no chidren");
			Assert.AreEqual("main", XmlUtils.GetOptionalAttributeValue(groupMain, "label"), "first child should be group 'main'");

			VerifyAttr(groupMain, "ws", "analysis"); // inherited from override.
			VerifyAttr(groupMain, "rubbish", "goodstuff"); // overridden.
			VerifyAttr(groupMain, "dummy", "base"); // inherited from original base.
		}

		[Test]
		public void OverrideDerived()
		{
			// Check that we can find a base node for a layout derived from an override.
			CheckBaseNode("layout", new[] {"LexEntry", "jtview", null, "DerivedForOverride"}, "DO2");
			CheckAlterationNode("layout", new[] {"LexEntry", "jtview", null, "DerivedForOverride"}, "DO3");
			var unified = CheckNode("layout", new[] {"LexEntry", "jtview", null, "DerivedForOverride"}, "DO3");
			var groupSecond = unified.Elements().ToList()[1];
			Assert.AreEqual("group", groupSecond.Name.LocalName, "first child of unified should be a group");
			Assert.AreEqual(2, groupSecond.Elements().Count(), "main group should have two chidren");
			Assert.AreEqual("second", XmlUtils.GetOptionalAttributeValue(groupSecond, "label"), "second child should be group 'second'");

			VerifyAttr(groupSecond, "ws", "vernacular"); // inherited from derived element.
			VerifyAttr(groupSecond, "rubbish", "nonsense"); // from override.
		}

		[Test]
		public void GetUnified()
		{
			var baseNode = _inventory.GetElement("layout", new[] {"LexEntry", "detail", null, "TestGetUnify1"});
			var alteration = _inventory.GetElement("layout", new[] {"LexEntry", "detail", null, "TestGetUnify2"});
			var unified = _inventory.GetUnified(baseNode, alteration);
			Assert.AreEqual(3, unified.Elements().Count());
			Assert.AreEqual("main", unified.Elements().ToList()[0].Attribute("label").Value);
			Assert.AreEqual("second", unified.Elements().ToList()[1].Attribute("label").Value);
			Assert.AreEqual("third", unified.Elements().ToList()[2].Attribute("label").Value);
			var repeat = _inventory.GetUnified(baseNode, alteration);
			Assert.AreSame(unified, repeat); // ensure not generating repeatedly.
		}

		[Test]
		public void GetUnified_SkipsShouldNotMerge()
		{
			var baseNode = _inventory.GetElement("layout", new[] { "MoAffixProcess", "detail", null, "TestGetUnify3" }); // TestGetUnify3 data has shouldNotMerge="true" on its child.
			var alteration = _inventory.GetElement("layout", new[] { "MoAffixProcess", "detail", null, "TestGetUnify4" });
			var unified = _inventory.GetUnified(baseNode, alteration);
			Assert.AreEqual(1, unified.Elements().Count());
			var repeat = _inventory.GetUnified(baseNode, alteration);
			Assert.AreSame(unified, repeat); // ensure not generating repeatedly.
		}
	}

	[TestFixture]
	public class CreateOverrideTests : TestBaseForTestsThatCreateTempFilesBasedOnResources
	{
		XElement _root;

		/// <inheritdoc />
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			var folder = CreateTempTestFiles(typeof(LanguageExplorerTestsResources), "CreateOverrideTestData");
			var doc = XDocument.Load(Path.Combine(folder, "CreateOverrideTestData.xml"));
			_root = doc.Root;

			base.FixtureSetup();
		}

		/// <inheritdoc />
		protected override ResourceManager ResourceMgr => LanguageExplorerTestsResources.ResourceManager;

		[Test]
		public void SimpleOverride()
		{
			// simulate a path to the citation form
			var rootLayout = _root.XPathSelectElement("layout[@name=\"Test1\"]");
			var cfPartRef = rootLayout.XPathSelectElement("part[@ref=\"CitationForm\"]");
			object[] path = {rootLayout, cfPartRef};
			XElement finalPartref;
			var result = Inventory.MakeOverride(path, "visibility", "ifdata", 7, out finalPartref);
			Assert.AreEqual(rootLayout.Elements().Count(), result.Elements().Count());
			var cfNewPartRef =  result.XPathSelectElement("part[@ref=\"CitationForm\"]");
			Assert.AreEqual("ifdata", XmlUtils.GetOptionalAttributeValue(cfNewPartRef, "visibility"));
			Assert.AreEqual("7", XmlUtils.GetOptionalAttributeValue(result, "version"));
		}

		[Test]
		public void LevelTwoOverride()
		{
			// simulate a path to the gloss
			var rootLayout = _root.XPathSelectElement("layout[@name=\"Test1\"]");
			var sensesPartRef = rootLayout.XPathSelectElement("part[@ref=\"Senses\"]");
			var glossPartRef = _root.XPathSelectElement("part[@ref=\"Gloss\"]");
			object[] path = {rootLayout, 1, sensesPartRef, 2, glossPartRef};
			XElement finalPartref;
			var result = Inventory.MakeOverride(path, "visibility", "ifdata", 1, out finalPartref);
			Assert.AreEqual(rootLayout.Elements().Count(), result.Elements().Count());
			var glossNewPartRef =  result.XPathSelectElement("//part[@ref=\"Gloss\"]");
			Assert.AreEqual("ifdata", XmlUtils.GetOptionalAttributeValue(glossNewPartRef, "visibility"));
			var sensesNewPartRef = glossNewPartRef.Parent;
			Assert.AreEqual("part", sensesNewPartRef.Name.LocalName);
			Assert.AreEqual("Senses", XmlUtils.GetOptionalAttributeValue(sensesNewPartRef, "ref"));
			var rootNewLayout = sensesNewPartRef.Parent;
			Assert.AreEqual("layout", rootNewLayout.Name.LocalName);
			Assert.AreEqual(result, rootNewLayout);
		}

		[Test]
		public void LevelThreeOverride()
		{
			// simulate a path to the gloss of a synonym. Include some non-part-ref XML nodes.
			var rootLayout = _root.XPathSelectElement("layout[@name=\"Test1\"]");
			var sensesPartRef = rootLayout.XPathSelectElement("part[@ref=\"Senses\"]");
			var glossPartRef = _root.XPathSelectElement("part[@ref=\"Gloss\"]");
			var synPartRef = _root.XPathSelectElement("part[@ref=\"Synonyms\"]");
			var blahPart = _root.XPathSelectElement("part[@id=\"blah\"]");
			var nonsenceLayout = _root.XPathSelectElement("layout[@id=\"nonsence\"]");
			object[] path = {rootLayout, 1, sensesPartRef, blahPart, nonsenceLayout, synPartRef, 2, glossPartRef};
			XElement finalPartref;
			var result = Inventory.MakeOverride(path, "visibility", "ifdata", 1, out finalPartref);
			Assert.AreEqual(rootLayout.Elements().Count(), result.Elements().Count());
			var glossNewPartRef =  result.XPathSelectElement("//part[@ref=\"Gloss\"]");
			Assert.AreEqual("ifdata", XmlUtils.GetOptionalAttributeValue(glossNewPartRef, "visibility"));
			var synNewPartRef = glossNewPartRef.Parent;
			Assert.AreEqual("part", synNewPartRef.Name.LocalName);
			Assert.AreEqual("Synonyms", XmlUtils.GetOptionalAttributeValue(synNewPartRef, "ref"));
			// Should have kept unmodified attributes of this element.
			Assert.AreEqual("TestingParam", XmlUtils.GetOptionalAttributeValue(synNewPartRef, "param"));
			var sensesNewPartRef = synNewPartRef.Parent;
			Assert.AreEqual("part", sensesNewPartRef.Name.LocalName);
			Assert.AreEqual("Senses", XmlUtils.GetOptionalAttributeValue(sensesNewPartRef, "ref"));
			var rootNewLayout = sensesNewPartRef.Parent;
			Assert.AreEqual("layout", rootNewLayout.Name.LocalName);
			Assert.AreEqual(result, rootNewLayout);
		}

		[Test]
		public void IndentedOverride()
		{
			// simulate a path to the Antonymns
			var rootLayout = _root.XPathSelectElement("layout[@name=\"Test1\"]");
			var sensesPartRef = rootLayout.XPathSelectElement("part[@ref=\"Senses\"]");
			var antonymnPartRef = sensesPartRef.XPathSelectElement("indent/part[@ref=\"Antonymns\"]");
			object[] path = {rootLayout, 1, sensesPartRef, 2, antonymnPartRef};
			XElement finalPartref;
			var result = Inventory.MakeOverride(path, "visibility", "ifdata", 1, out finalPartref);
			Assert.AreEqual(rootLayout.Elements().Count(), result.Elements().Count());
			var antonymNewPartRef =  result.XPathSelectElement("//part[@ref=\"Antonymns\"]");
			Assert.AreEqual("ifdata", XmlUtils.GetOptionalAttributeValue(antonymNewPartRef, "visibility"));
			var indentNewPartRef = antonymNewPartRef.Parent;
			Assert.AreEqual("indent", indentNewPartRef.Name.LocalName);
			var sensesNewPartRef = indentNewPartRef.Parent;
			Assert.AreEqual("part", sensesNewPartRef.Name.LocalName);
			Assert.AreEqual("Senses", XmlUtils.GetOptionalAttributeValue(sensesNewPartRef, "ref"));
			var rootNewLayout = sensesNewPartRef.Parent;
			Assert.AreEqual("layout", rootNewLayout.Name.LocalName);
			Assert.AreEqual(result, rootNewLayout);
		}
	}
}