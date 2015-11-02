// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using NUnit.Framework;

namespace SIL.FieldWorks.FDO.FDOTests
{
	class GoldEticTests : BaseTest
	{
		[Test]
		public void AllPOSTemplatePossibilityItemsHaveGUIDs()
		{
			var posFilePath = Path.Combine(FwDirectoryFinder.TemplateDirectory, "POS.xml");
			AssertThatXmlIn.File(posFilePath).HasNoMatchForXpath("//PartOfSpeech[not(@guid)]");
		}

		[Test]
		public void AllPOSTemplatePossibilityItemsMatchGoldEticStandard()
		{
			var posFilePath = Path.Combine(FwDirectoryFinder.TemplateDirectory, "POS.xml");
			var goldEticFilePath = Path.Combine(FwDirectoryFinder.TemplateDirectory, "GOLDEtic.xml");
			var dom = new XmlDocument();
			// Load the intitial Part of Speech list.
			dom.Load(posFilePath);
			var posNodes = dom.SelectNodes("//PartOfSpeech[@guid]");
			foreach(XmlElement posNode in posNodes)
			{
				var guid = posNode.Attributes["guid"];
				var catalogIdNode = (XmlElement)posNode.SelectSingleNode("CatalogSourceId/Uni");
				Assert.NotNull(catalogIdNode, "Part of speech list item missing CatalogSourceId: " + posNode.OuterXml);
				var catalogId = catalogIdNode.InnerText;
				var posMatchingXpath = string.Format("//item[@id='{0}' and @guid='{1}']", catalogId, guid.Value);
				AssertThatXmlIn.File(goldEticFilePath).HasSpecifiedNumberOfMatchesForXpath(posMatchingXpath, 1);
			}
		}
	}
}
