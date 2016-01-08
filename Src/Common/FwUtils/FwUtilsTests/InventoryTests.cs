// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: InventoryTests.cs
// Responsibility: John Thomson
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.Utils;
using System.Resources;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Summary description for XmlUtilsTest.
	/// </summary>
	[TestFixture]
	public class InventoryTests : TestBaseForTestsThatCreateTempFilesBasedOnResources
	{
		Inventory m_inventory;

		/// <summary>
		/// Initialize everything...individual tests check what we got.
		/// </summary>
		[TestFixtureSetUp]
		public void Setup()
		{
			Dictionary<string, string[]> keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["layout"] = new[] {"class", "type", "mode", "name" };
			keyAttrs["group"] = new[] {"label"};
			keyAttrs["part"] = new[] {"ref"};

			string testPathBase = CreateTempTestFiles(typeof(Properties.Resources), "InventoryBaseTestFiles");
			string testPathLater = CreateTempTestFiles(typeof(Properties.Resources), "InventoryLaterTestFiles");

			m_inventory = new Inventory(new[] {testPathBase, testPathLater},
				"*Layouts.xml", "/layoutInventory/*", keyAttrs, "InventoryTests", "projectPath");
		}

		/// <summary />
		protected override ResourceManager ResourceMgr
		{
			get
			{
				return Properties.Resources.ResourceManager;
			}
		}

		XElement CheckNode(string name, string[] keyvals, string target)
		{
			var node = m_inventory.GetElement(name, keyvals);
			Check(node, target);
			return node;
		}

		static void Check(XElement node, string target)
		{
			if (node == null)
				Assert.IsNotNull(node, "expected node not found: " + target);
			var match = node.Attribute("match");
			if (match == null)
				Assert.IsNotNull(match, "expected node lacks match attr: " + target);
			Assert.AreEqual(target, node.Attribute("match").Value);
		}
		void CheckBaseNode(string name, string[] keyvals, string target)
		{
			var node = m_inventory.GetBase(name, keyvals);
			Check(node, target);
		}
		void CheckAlterationNode(string name, string[] keyvals, string target)
		{
			var node = m_inventory.GetAlteration(name, keyvals);
			Check(node, target);
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

		static void VerifyAttr(XElement node, string attr, string val)
		{
			Assert.AreEqual(val, XmlUtils.GetOptionalAttributeValue(node, attr));
		}

		// Verifies that parent's index'th child has the specified value for the specified attribute.
		// Returns the child.
		static XElement VerifyChild(XContainer parent, int index, string attr, string val)
		{
			Assert.IsTrue(parent.Elements().Count() > index);
			var child = parent.Elements().ToList()[index];
			VerifyAttr(child, attr, val);
			return child;
		}

		/// <summary />
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
			Assert.IsNull(m_inventory.GetAlteration("layout", new[] {"LexEntry", "jtview", null, "Test1"}),
				"GetAlteration should be null for non-derived node.");

			// Check correct working of unification:
			// - first main child is present as expected
			var groupMain = unified.Elements().ToList()[0];
			Assert.IsTrue("group" == groupMain.Name, "first child of unified should be a group");
			Assert.AreEqual(3, groupMain.Elements().Count(), "main group should have three chidren");
			Assert.AreEqual("main", XmlUtils.GetOptionalAttributeValue(groupMain, "label"),
				"first child should be group 'main'");
			// - added elements are added. (Also checks default order: original plus extras.)
			// - unmatched original elements are left alone.
			var part0M = VerifyChild(groupMain, 0, "ref", "LexEntry-Jt-Citationform"); // part0M
			VerifyChild(groupMain, 1, "ref", "LexEntry-Jt-Senses"); // part1M
			VerifyChild(groupMain, 2, "ref", "LexEntry-Jt-Forms"); // part2M

			// - child elements are correctly ordered when 'reorder' is true.
			var groupSecond = unified.Elements().ToList()[1];
			Assert.IsTrue("group" == groupSecond.Name, "second child of unified should be a group");
			Assert.AreEqual(3, groupSecond.Elements().Count(), "main group should have three chidren");
			Assert.AreEqual("second", XmlUtils.GetOptionalAttributeValue(groupSecond, "label"),
				"second child should be group 'second'");
			VerifyChild(groupSecond, 0, "ref", "LexEntry-Jt-Forms"); // part0S
			var part1S = VerifyChild(groupSecond, 1, "ref", "LexEntry-Jt-Citationform"); // part1S
			VerifyChild(groupSecond, 2, "ref", "LexEntry-Jt-Senses"); // part2S

			// - check no reordering when no element added, and reorder is false
			var groupThird = unified.Elements().ToList()[2];
			Assert.IsTrue("group" == groupThird.Name, "Third child of unified should be a group");
			Assert.AreEqual(3, groupThird.Elements().Count(), "main group should have three chidren");
			Assert.AreEqual("third", XmlUtils.GetOptionalAttributeValue(groupThird, "label"),
				"third child should be group 'Third'");
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
		/// </summary>
		[Test]
		public void Overrides()
		{
			// Check that we can find a base node for a layout derived from an override.
			CheckBaseNode("layout", new[] {"LexSense", "jtview", null, "Test8D"}, "test7D");
			CheckAlterationNode("layout", new[] {"LexSense", "jtview", null, "Test8D"}, "test8D");
			var unified = CheckNode("layout", new[] {"LexSense", "jtview", null, "Test8D"}, "test8D");
			var groupMain = unified.Elements().ToList()[0];
			Assert.IsTrue("group" == groupMain.Name, "first child of unified should be a group");
			Assert.AreEqual(0, groupMain.Elements().Count(), "main group should have no chidren");
			Assert.AreEqual("main", XmlUtils.GetOptionalAttributeValue(groupMain, "label"),
				"first child should be group 'main'");

			VerifyAttr(groupMain, "ws", "analysis"); // inherited from override.
			VerifyAttr(groupMain, "rubbish", "goodstuff"); // overridden.
			VerifyAttr(groupMain, "dummy", "base"); // inherited from original base.
		}

		/// <summary />
		[Test]
		public void OverrideDerived()
		{
			// Check that we can find a base node for a layout derived from an override.
			CheckBaseNode("layout", new[] {"LexEntry", "jtview", null, "DerivedForOverride"}, "DO2");
			CheckAlterationNode("layout", new[] {"LexEntry", "jtview", null, "DerivedForOverride"}, "DO3");
			var unified = CheckNode("layout", new[] {"LexEntry", "jtview", null, "DerivedForOverride"}, "DO3");
			var groupSecond = unified.Elements().ToList()[1];
			Assert.IsTrue("group" == groupSecond.Name, "first child of unified should be a group");
			Assert.AreEqual(2, groupSecond.Elements().Count(), "main group should have two chidren");
			Assert.AreEqual("second", XmlUtils.GetOptionalAttributeValue(groupSecond, "label"),
				"second child should be group 'second'");

			VerifyAttr(groupSecond, "ws", "vernacular"); // inherited from derived element.
			VerifyAttr(groupSecond, "rubbish", "nonsense"); // from override.
		}

		/// <summary />
		[Test]
		public void GetUnified()
		{
			var baseNode = m_inventory.GetElement("layout",
				new[] {"LexEntry", "detail", null, "TestGetUnify1"});
			var alteration = m_inventory.GetElement("layout",
				new[] {"LexEntry", "detail", null, "TestGetUnify2"});
			var unified = m_inventory.GetUnified(baseNode, alteration);
			Assert.AreEqual(3, unified.Elements().Count());
			Assert.AreEqual("main", unified.Elements().ToList()[0].Attribute("label").Value);
			Assert.AreEqual("second", unified.Elements().ToList()[1].Attribute("label").Value);
			Assert.AreEqual("third", unified.Elements().ToList()[2].Attribute("label").Value);
			var repeat = m_inventory.GetUnified(baseNode, alteration);
			Assert.AreSame(unified, repeat); // ensure not generating repeatedly.
		}
	}

	/// <summary />
	[TestFixture]
	public class CreateOverrideTests : TestBaseForTestsThatCreateTempFilesBasedOnResources
	{
		XElement _root;
		/// <summary>
		/// Initialize everything...load a set of fragments from a file.
		/// </summary>
		[TestFixtureSetUp]
		public void Setup()
		{
			var folder = CreateTempTestFiles(typeof(Properties.Resources), "CreateOverrideTestData");
			var doc = XDocument.Load(Path.Combine(folder, "CreateOverrideTestData.xml"));
			_root = doc.Root;
		}

		/// <summary />
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
			var cfNewPartRef = result.XPathSelectElement("part[@ref=\"CitationForm\"]");
			Assert.AreEqual("ifdata", XmlUtils.GetOptionalAttributeValue(cfNewPartRef, "visibility"));
			Assert.AreEqual("7", XmlUtils.GetOptionalAttributeValue(result, "version"));
		}

		/// <summary />
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
			var glossNewPartRef = result.XPathSelectElement("//part[@ref=\"Gloss\"]");
			Assert.AreEqual("ifdata", XmlUtils.GetOptionalAttributeValue(glossNewPartRef, "visibility"));
			var sensesNewPartRef = glossNewPartRef.Parent;
			Assert.IsTrue("part" == sensesNewPartRef.Name);
			Assert.AreEqual("Senses", XmlUtils.GetOptionalAttributeValue(sensesNewPartRef, "ref"));
			var rootNewLayout = sensesNewPartRef.Parent;
			Assert.IsTrue("layout" == rootNewLayout.Name);
			Assert.AreEqual(result, rootNewLayout);
		}

		/// <summary />
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
			var glossNewPartRef = result.XPathSelectElement("//part[@ref=\"Gloss\"]");
			Assert.AreEqual("ifdata", XmlUtils.GetOptionalAttributeValue(glossNewPartRef, "visibility"));
			var synNewPartRef = glossNewPartRef.Parent;
			Assert.IsTrue("part" == synNewPartRef.Name);
			Assert.AreEqual("Synonyms", XmlUtils.GetOptionalAttributeValue(synNewPartRef, "ref"));
			// Should have kept unmodified attributes of this element.
			Assert.AreEqual("TestingParam", XmlUtils.GetOptionalAttributeValue(synNewPartRef, "param"));
			var sensesNewPartRef = synNewPartRef.Parent;
			Assert.IsTrue("part" == sensesNewPartRef.Name);
			Assert.AreEqual("Senses", XmlUtils.GetOptionalAttributeValue(sensesNewPartRef, "ref"));
			var rootNewLayout = sensesNewPartRef.Parent;
			Assert.IsTrue("layout" == rootNewLayout.Name);
			Assert.AreEqual(result, rootNewLayout);
		}

		/// <summary />
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
			var antonymNewPartRef = result.XPathSelectElement("//part[@ref=\"Antonymns\"]");
			Assert.AreEqual("ifdata", XmlUtils.GetOptionalAttributeValue(antonymNewPartRef, "visibility"));
			var indentNewPartRef = antonymNewPartRef.Parent;
			Assert.IsTrue("indent" == indentNewPartRef.Name);
			var sensesNewPartRef = indentNewPartRef.Parent;
			Assert.IsTrue("part" == sensesNewPartRef.Name);
			Assert.AreEqual("Senses", XmlUtils.GetOptionalAttributeValue(sensesNewPartRef, "ref"));
			var rootNewLayout = sensesNewPartRef.Parent;
			Assert.IsTrue("layout" == rootNewLayout.Name);
			Assert.AreEqual(result, rootNewLayout);
		}
	}
}
