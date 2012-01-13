using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;
using NUnit.Framework;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO;

namespace XMLViewsTests
{
	/// <summary>
	/// Test the PartGenerator.
	/// </summary>
	[TestFixture]
	public class TestPartGenerate : MemoryOnlyBackendProviderTestBase
	{
		/// <summary>
		///
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				// add a custom field named "MyRestrictions" to LexEntry.
				FieldDescription custom = new FieldDescription(Cache);
				custom.Class = LexEntryTags.kClassId;
				custom.Name = "MyRestrictions";
				custom.Type = CellarPropertyType.MultiBigString;
				custom.WsSelector = (int)WritingSystemServices.kwsAnalVerns;
				custom.Userlabel = "MyRestrictions";
				custom.UpdateCustomField();
			});
		}

		/// <summary>
		/// Verify that one of the nodes in items matches the expected one
		/// </summary>
		/// <param name="items"></param>
		/// <param name="expected"></param>
		bool SomeNodeMatches(XmlNode[] items, XmlNode expected)
		{
			foreach(XmlNode node in items)
				if (TestXmlViewsUtils.NodesMatch(node, expected))
					return true;
			return false;
		}

		bool StringArrayIncludes(string[] vals, string target)
		{
			foreach(string s in vals)
				if (s == target)
					return true;
			return false;
		}

		/// <summary>
		/// Test generating an element for each ML string property in a class.
		/// </summary>
		[Test]
		public void GenerateMlString()
		{
			XmlDocument docSrc = new XmlDocument();
			docSrc.LoadXml(
				"<generate class=\"LexEntry\" fieldType=\"mlstring\" restrictions=\"none\"> "
					+"<column label=\"$label\"> "
						+"<seq field=\"Senses\" sep=\"$delimiter:commaSpace\"> "
						+"<string field=\"$fieldName\" ws=\"$ws:analysis\"/> "
						+"</seq> "
					+"</column> "
				+"</generate>");
			XmlNode source = TestXmlViewsUtils.GetRootNode(docSrc, "generate");
			Assert.IsNotNull(source);

			PartGenerator generator = new PartGenerator(Cache, source);

			string[] fields = generator.FieldNames;
			Assert.AreEqual(7, fields.Length);
			Assert.IsTrue(StringArrayIncludes(fields, "CitationForm"));
			Assert.IsTrue(StringArrayIncludes(fields, "Bibliography"));
			Assert.IsTrue(StringArrayIncludes(fields, "Comment"));
			Assert.IsTrue(StringArrayIncludes(fields, "LiteralMeaning"));
			Assert.IsTrue(StringArrayIncludes(fields, "Restrictions"));
			Assert.IsTrue(StringArrayIncludes(fields, "SummaryDefinition"));
			Assert.IsTrue(StringArrayIncludes(fields, "MyRestrictions"));

			XmlNode[] results = generator.Generate();

			Assert.AreEqual(7, results.Length);

			XmlDocument docExpected = new XmlDocument();

			// LT-6956 : sense the test is calling Generate - add the "originalLabel" attribute.
			docExpected.LoadXml(
				"<column label=\"CitationForm\" originalLabel=\"CitationForm\" > "
					+"<seq field=\"Senses\" sep=\"$delimiter:commaSpace\"> "
					+"<string field=\"CitationForm\" ws=\"$ws:analysis\" class=\"LexEntry\"/> "
					+"</seq> "
				+"</column>");
			XmlNode expected = TestXmlViewsUtils.GetRootNode(docExpected, "column");

			Assert.IsTrue(SomeNodeMatches(results, expected), "CitationForm field is wrong");

			XmlDocument docExpected2 = new XmlDocument();
			docExpected2.LoadXml(
				"<column label=\"Bibliography\" originalLabel=\"Bibliography\"> "
				+"<seq field=\"Senses\" sep=\"$delimiter:commaSpace\"> "
				+"<string field=\"Bibliography\" ws=\"$ws:analysis\" class=\"LexEntry\"/> "
				+"</seq> "
				+"</column>");
			XmlNode expected2 = TestXmlViewsUtils.GetRootNode(docExpected2, "column");
			Assert.IsTrue(SomeNodeMatches(results, expected2), "Bibliography field is wrong");

			XmlDocument docExpected3 = new XmlDocument();
			docExpected3.LoadXml(
				"<column label=\"MyRestrictions\" originalLabel=\"MyRestrictions\"> "
				+"<seq field=\"Senses\" sep=\"$delimiter:commaSpace\"> "
				+"<string field=\"MyRestrictions\" ws=\"$ws:analysis\" class=\"LexEntry\"/> "
				+"</seq> "
				+"</column>");
			XmlNode expected3 = TestXmlViewsUtils.GetRootNode(docExpected3, "column");
			Assert.IsTrue(SomeNodeMatches(results, expected3), "generated MyRestrictions field is wrong");
		}

		/// <summary>
		/// Test generating an element for each ML string property in a class that is a custom field.
		/// </summary>
		[Test]
		public void GenerateMlCustomString()
		{
			XmlDocument docSrc = new XmlDocument();
			docSrc.LoadXml(
				"<generate class=\"LexEntry\" fieldType=\"mlstring\" restrictions=\"customOnly\"> "
				+"<column label=\"$label\"> "
				+"<seq field=\"Senses\" sep=\"$delimiter:commaSpace\"> "
				+"<string field=\"$fieldName\" ws=\"$ws:analysis\" class=\"LexEntry\"/> "
				+"</seq> "
				+"</column> "
				+"</generate>");
			XmlNode source = TestXmlViewsUtils.GetRootNode(docSrc, "generate");
			Assert.IsNotNull(source);

			PartGenerator generator = new PartGenerator(Cache, source);

			string[] fields = generator.FieldNames;
			Assert.AreEqual(1, fields.Length);
			Assert.IsTrue(StringArrayIncludes(fields, "MyRestrictions"));

			XmlNode[] results = generator.Generate();

			// SampleCm.xml has three ML attrs on LexEntry
			Assert.AreEqual(1, results.Length);

			XmlDocument docExpected3 = new XmlDocument();
			docExpected3.LoadXml(
				"<column label=\"MyRestrictions\" originalLabel=\"MyRestrictions\" > "
				+"<seq field=\"Senses\" sep=\"$delimiter:commaSpace\"> "
				+"<string field=\"MyRestrictions\" ws=\"$ws:analysis\" class=\"LexEntry\"/> "
				+"</seq> "
				+"</column>");
			XmlNode expected3 = TestXmlViewsUtils.GetRootNode(docExpected3, "column");
			Assert.IsTrue(SomeNodeMatches(results, expected3));
		}

		// Return true if there is a node in nodes between min and (lim -1)
		// that has the specified name and label attributes.
		bool NameAndLabelOccur(List<XmlNode> nodes, int min, int lim, string name, string label)
		{
			for (int i = min; i < lim; i++)
			{
				XmlNode node = nodes[i];
				if (node.Name == name  && XmlUtils.GetOptionalAttributeValue(node, "label") == label)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Test generating an array of children, automatically expanding generate nodes.
		/// </summary>
		[Test]
		public void GenerateParts()
		{
			XmlDocument docSrc = new XmlDocument();
			docSrc.LoadXml(
				"<root> "
					+"<dummy1/> "
					+"<generate class=\"LexEntry\" fieldType=\"mlstring\" restrictions=\"none\"> "
						+"<column label=\"$fieldName\"> "
							+"<seq field=\"Senses\" sep=\"$delimiter:commaSpace\"> "
								+"<string field=\"$fieldName\" ws=\"$ws:analysis\"/> "
							+"</seq> "
						+"</column> "
					+"</generate> "
					+"<dummy2/> "
					+"<generate class=\"LexEntry\" fieldType=\"mlstring\" restrictions=\"none\"> "
						+"<dummyG label=\"$fieldName\"/> "
					+"</generate> "
					+"<dummy3/> "
					+"<dummy4/> "
				+"</root>");
			XmlNode source = TestXmlViewsUtils.GetRootNode(docSrc, "root");
			Assert.IsNotNull(source);

			List<XmlNode> nodes = PartGenerator.GetGeneratedChildren(source, Cache);

			Assert.AreEqual(1+7+1+7+2, nodes.Count);
			Assert.AreEqual("dummy1", nodes[0].Name);
			Assert.AreEqual("dummy2", nodes[1+7].Name);
			Assert.AreEqual("dummy3", nodes[1+7+1+7].Name);
			Assert.AreEqual("dummy4", nodes[1+7+1+7+1].Name);
			Assert.IsTrue(NameAndLabelOccur(nodes, 1, 1+7, "column", "CitationForm"));
			Assert.IsTrue(NameAndLabelOccur(nodes, 1, 1+7, "column", "Bibliography"));
			Assert.IsTrue(NameAndLabelOccur(nodes, 1+7+1, 1+7+1+7, "dummyG", "CitationForm"));
			Assert.IsTrue(NameAndLabelOccur(nodes, 1+7+1, 1+7+1+7, "dummyG", "MyRestrictions"));
		}
	}
}
