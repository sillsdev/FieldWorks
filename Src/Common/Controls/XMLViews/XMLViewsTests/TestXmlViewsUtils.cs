// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Filters;
using SIL.CoreImpl.Text;
using SIL.Xml;

namespace XMLViewsTests
{
	/// <summary>
	/// Test XmlViewsUtils
	/// </summary>
	[TestFixture]
	public class TestXmlViewsUtils : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Return true if the two nodes match. Corresponding children shoudl match, and
		/// corresponding attributes (though not necessarily in the same order).
		/// The nodes are expected to be actually XmlElements
		/// </summary>
		/// <param name="node1"></param>
		/// <param name="node2"></param>
		/// <returns></returns>
		static public bool NodesMatch(XElement node1, XElement node2)
		{
			if (node1.Name != node2.Name)
				return false;
			if (node1.Name.LocalName != node2.Name.LocalName)
				return false;
			if (node1.Elements().Count() != node2.Elements().Count())
				return false;
			if (node1.Attributes().Count() != node2.Attributes().Count())
				return false;
			if (node1.GetInnerText() != node2.GetInnerText())
				return false;
			foreach (var attr in node1.Attributes())
			{
				var xa2 = node2.Attribute(attr.Name);
				if (xa2 == null || attr.Value != xa2.Value)
					return false;
			}
			var node1ElementsAsList = node1.Elements().ToList();
			var node2ElementsAsList = node2.Elements().ToList();
			return !node1ElementsAsList.Where((t, i) => !NodesMatch(t, node2ElementsAsList[i])).Any();
		}

		public static XElement GetRootNode(XDocument doc, string name)
		{
			return doc.Root.XPathSelectElement("//" + name);
		}

		[Test]
		public void CopyWithParamDefaults()
		{
			var docSrc = XDocument.Parse(
				"<column label=\"Gloss\"> "
					+"<seq field=\"Senses\" sep=\"$delimiter=commaSpace\"> "
					+"<string field=\"Gloss\" ws=\"$ws=analysis\"/> "
					+"</seq> "
				+"</column>");

			var source = GetRootNode(docSrc, "column");
			Assert.IsNotNull(source);

			var output = XmlViewsUtils.CopyWithParamDefaults(source);
			Assert.IsNotNull(output);
			Assert.IsFalse(source == output);

			var docExpected = XDocument.Parse(
				"<column label=\"Gloss\"> "
					+"<seq field=\"Senses\" sep=\"commaSpace\"> "
						+"<string field=\"Gloss\" ws=\"analysis\"/> "
					+"</seq> "
				+"</column>");
			var expected = GetRootNode(docExpected, "column");
			Assert.IsTrue(NodesMatch(output, expected));
		}

		[Test]
		public void TrivialCopyWithParamDefaults()
		{
			var docSrc = XDocument.Parse(
				"<column label=\"Gloss\"> "
				+"<seq field=\"Senses\" sep=\"commaSpace\"> "
				+"<string field=\"Gloss\" ws=\"analysis\"/> "
				+"</seq> "
				+"</column>");

			var source = GetRootNode(docSrc, "column");
			Assert.IsNotNull(source);
			var output = XmlViewsUtils.CopyWithParamDefaults(source);
			Assert.IsTrue(source == output);
		}

		[Test]
		public void FindDefaults()
		{
			var docSrc = XDocument.Parse(
				"<column label=\"Gloss\"> "
				+"<seq field=\"Senses\" sep=\"$delimiter=commaSpace\"> "
				+"<string field=\"Gloss\" ws=\"$ws=analysis\"/> "
				+"</seq> "
				+"</column>");

			var source = GetRootNode(docSrc, "column");
			Assert.IsNotNull(source);
			Assert.IsTrue(XmlViewsUtils.HasParam(source));

			string[] paramList = XmlViewsUtils.FindParams(source);
			Assert.AreEqual(2, paramList.Length);
			Assert.AreEqual("$delimiter=commaSpace", paramList[0]);
			Assert.AreEqual("$ws=analysis", paramList[1]);

		}

		[Test]
		public void AlphaCompNumberString()
		{
			string zero = XmlViewsUtils.AlphaCompNumberString(0);
			string one = XmlViewsUtils.AlphaCompNumberString(1);
			string two = XmlViewsUtils.AlphaCompNumberString(2);
			string ten = XmlViewsUtils.AlphaCompNumberString(10);
			string eleven = XmlViewsUtils.AlphaCompNumberString(11);
			string hundred = XmlViewsUtils.AlphaCompNumberString(100);
			string minus1 = XmlViewsUtils.AlphaCompNumberString(-1);
			string minus2 = XmlViewsUtils.AlphaCompNumberString(-2);
			string minus10 = XmlViewsUtils.AlphaCompNumberString(-10);
			string max = XmlViewsUtils.AlphaCompNumberString(Int32.MaxValue);
			string min = XmlViewsUtils.AlphaCompNumberString(Int32.MinValue);
			IcuComparer comp = new IcuComparer("en");
			comp.OpenCollatingEngine();
			Assert.IsTrue(comp.Compare(zero, one) < 0);
			Assert.IsTrue(comp.Compare(one, two) < 0);
			Assert.IsTrue(comp.Compare(two, ten) < 0);
			Assert.IsTrue(comp.Compare(ten, eleven) < 0);
			Assert.IsTrue(comp.Compare(eleven, hundred) < 0);
			Assert.IsTrue(comp.Compare(minus1, zero) < 0);
			Assert.IsTrue(comp.Compare(minus2, minus1) < 0);
			Assert.IsTrue(comp.Compare(minus10, minus2) < 0);
			Assert.IsTrue(comp.Compare(hundred, max) < 0);
			Assert.IsTrue(comp.Compare(min, minus10) < 0);

			Assert.IsTrue(comp.Compare(ten, zero) > 0);
			Assert.IsTrue(comp.Compare(ten, minus1) > 0);
			Assert.IsTrue(comp.Compare(hundred, minus10) > 0);
			Assert.IsTrue(comp.Compare(one, one) == 0);
			Assert.IsTrue(comp.Compare(ten, ten) == 0);
			Assert.IsTrue(comp.Compare(minus1, minus1) == 0);
			comp.CloseCollatingEngine();
		}

		[Test]
		public void StringsFor()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.VernacularDefaultWritingSystem = TsStringUtils.MakeString("kick", Cache.DefaultVernWs);
			var doc = XDocument.Parse(@"<string class='LexEntry' field='CitationForm'/>");
			var node = doc.Root;
			var strings = XmlViewsUtils.StringsFor(Cache, Cache.DomainDataByFlid, node, entry.Hvo, null, null,
				WritingSystemServices.kwsVern);
			Assert.That(strings, Has.Length.EqualTo(1));
			Assert.That(strings, Has.Member("kick"));
		}
	}
}
