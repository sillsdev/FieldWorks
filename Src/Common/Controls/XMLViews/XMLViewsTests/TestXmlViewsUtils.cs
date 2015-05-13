using System;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Filters;
using NUnit.Framework;
using System.Xml;

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
		static public bool NodesMatch(XmlNode node1, XmlNode node2)
		{
			if (node1.Name != node2.Name)
				return false;
			if (node1.ChildNodes.Count != node2.ChildNodes.Count)
				return false;
			if (node1.Attributes.Count != node2.Attributes.Count)
				return false;
			if (node1.InnerText != node2.InnerText)
				return false;
			for (int i = 0; i < node1.Attributes.Count; i++)
			{
				XmlAttribute xa1 = node1.Attributes[i];
				XmlAttribute xa2 = node2.Attributes[xa1.Name];
				if (xa2 == null || xa1.Value != xa2.Value)
					return false;
			}
			for (int i = 0; i < node1.ChildNodes.Count; i++)
				if (!NodesMatch(node1.ChildNodes[i], node2.ChildNodes[i]))
					return false;
			return true;
		}

		public static XmlNode GetRootNode(XmlDocument doc, string name)
		{
			return doc.DocumentElement.SelectSingleNode("//" + name);
		}

		[Test]
		public void CopyWithParamDefaults()
		{
			XmlDocument docSrc = new XmlDocument();
			docSrc.LoadXml(
				"<column label=\"Gloss\"> "
					+"<seq field=\"Senses\" sep=\"$delimiter=commaSpace\"> "
					+"<string field=\"Gloss\" ws=\"$ws=analysis\"/> "
					+"</seq> "
				+"</column>");

			XmlNode source = GetRootNode(docSrc, "column");
			Assert.IsNotNull(source);

			XmlNode output = XmlViewsUtils.CopyWithParamDefaults(source);
			Assert.IsNotNull(output);
			Assert.IsFalse(source == output);

			XmlDocument docExpected = new XmlDocument();
			docExpected.LoadXml(
				"<column label=\"Gloss\"> "
					+"<seq field=\"Senses\" sep=\"commaSpace\"> "
						+"<string field=\"Gloss\" ws=\"analysis\"/> "
					+"</seq> "
				+"</column>");
			XmlNode expected = GetRootNode(docExpected, "column");
			Assert.IsTrue(NodesMatch(output, expected));
		}

		[Test]
		public void TrivialCopyWithParamDefaults()
		{
			XmlDocument docSrc = new XmlDocument();
			docSrc.LoadXml(
				"<column label=\"Gloss\"> "
				+"<seq field=\"Senses\" sep=\"commaSpace\"> "
				+"<string field=\"Gloss\" ws=\"analysis\"/> "
				+"</seq> "
				+"</column>");

			XmlNode source = GetRootNode(docSrc, "column");
			Assert.IsNotNull(source);
			XmlNode output = XmlViewsUtils.CopyWithParamDefaults(source);
			Assert.IsTrue(source == output);
		}

		[Test]
		public void FindDefaults()
		{
			XmlDocument docSrc = new XmlDocument();
			docSrc.LoadXml(
				"<column label=\"Gloss\"> "
				+"<seq field=\"Senses\" sep=\"$delimiter=commaSpace\"> "
				+"<string field=\"Gloss\" ws=\"$ws=analysis\"/> "
				+"</seq> "
				+"</column>");

			XmlNode source = GetRootNode(docSrc, "column");
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
		}

		[Test]
		public void StringsFor()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			entry.CitationForm.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString("kick", Cache.DefaultVernWs);
			var doc = new XmlDocument();
			doc.LoadXml(@"<string class='LexEntry' field='CitationForm'/>");
			var node = doc.DocumentElement;
			var strings = XmlViewsUtils.StringsFor(Cache, Cache.DomainDataByFlid, node, entry.Hvo, null, null,
				WritingSystemServices.kwsVern);
			Assert.That(strings, Has.Length.EqualTo(1));
			Assert.That(strings, Has.Member("kick"));
		}
	}
}
