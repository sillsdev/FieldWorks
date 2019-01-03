// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml.Linq;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Filters;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorerTests.Controls.XMLViews
{
	/// <summary>
	/// Test XmlViewsUtils
	/// </summary>
	[TestFixture]
	public class TestXmlViewsUtils : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void CopyWithParamDefaults()
		{
			var docSrc = XDocument.Parse(
				"<column label=\"Gloss\"> "
					+ "<seq field=\"Senses\" sep=\"$delimiter=commaSpace\"> "
					+ "<string field=\"Gloss\" ws=\"$ws=analysis\"/> "
					+ "</seq> "
				+ "</column>");

			var source = TestUtilities.GetRootNode(docSrc, "column");
			Assert.IsNotNull(source);

			var output = XmlViewsUtils.CopyWithParamDefaults(source);
			Assert.IsNotNull(output);
			Assert.IsFalse(source == output);

			var docExpected = XDocument.Parse(
				"<column label=\"Gloss\"> "
					+ "<seq field=\"Senses\" sep=\"commaSpace\"> "
						+ "<string field=\"Gloss\" ws=\"analysis\"/> "
					+ "</seq> "
				+ "</column>");
			var expected = TestUtilities.GetRootNode(docExpected, "column");
			Assert.IsTrue(TestUtilities.NodesMatch(output, expected));
		}

		[Test]
		public void TrivialCopyWithParamDefaults()
		{
			var docSrc = XDocument.Parse(
				"<column label=\"Gloss\"> "
				+ "<seq field=\"Senses\" sep=\"commaSpace\"> "
				+ "<string field=\"Gloss\" ws=\"analysis\"/> "
				+ "</seq> "
				+ "</column>");

			var source = TestUtilities.GetRootNode(docSrc, "column");
			Assert.IsNotNull(source);
			var output = XmlViewsUtils.CopyWithParamDefaults(source);
			Assert.IsTrue(source == output);
		}

		[Test]
		public void FindDefaults()
		{
			var docSrc = XDocument.Parse(
				"<column label=\"Gloss\"> "
				+ "<seq field=\"Senses\" sep=\"$delimiter=commaSpace\"> "
				+ "<string field=\"Gloss\" ws=\"$ws=analysis\"/> "
				+ "</seq> "
				+ "</column>");

			var source = TestUtilities.GetRootNode(docSrc, "column");
			Assert.IsNotNull(source);
			Assert.IsTrue(XmlViewsUtils.HasParam(source));

			var paramList = XmlViewsUtils.FindParams(source);
			Assert.AreEqual(2, paramList.Length);
			Assert.AreEqual("$delimiter=commaSpace", paramList[0]);
			Assert.AreEqual("$ws=analysis", paramList[1]);

		}

		[Test]
		public void AlphaCompNumberString()
		{
			var zero = XmlViewsUtils.AlphaCompNumberString(0);
			var one = XmlViewsUtils.AlphaCompNumberString(1);
			var two = XmlViewsUtils.AlphaCompNumberString(2);
			var ten = XmlViewsUtils.AlphaCompNumberString(10);
			var eleven = XmlViewsUtils.AlphaCompNumberString(11);
			var hundred = XmlViewsUtils.AlphaCompNumberString(100);
			var minus1 = XmlViewsUtils.AlphaCompNumberString(-1);
			var minus2 = XmlViewsUtils.AlphaCompNumberString(-2);
			var minus10 = XmlViewsUtils.AlphaCompNumberString(-10);
			var max = XmlViewsUtils.AlphaCompNumberString(Int32.MaxValue);
			var min = XmlViewsUtils.AlphaCompNumberString(Int32.MinValue);
			var comp = new IcuComparer("en");
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
			var strings = XmlViewsUtils.StringsFor(Cache, Cache.DomainDataByFlid, node, entry.Hvo, null, null, WritingSystemServices.kwsVern);
			Assert.That(strings, Has.Length.EqualTo(1));
			Assert.That(strings, Has.Member("kick"));
		}
	}
}