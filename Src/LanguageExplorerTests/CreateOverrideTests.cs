// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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