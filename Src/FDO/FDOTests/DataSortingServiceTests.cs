// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.CoreTests.MetaDataCacheTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Infrastructure.Impl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	///<summary>
	/// Test the DataSortingService class.
	///</summary>
	[TestFixture]
	public class DataSortingServiceTests : FieldTestBase
	{
		/// <summary>
		/// Check for one item in collection property.
		/// </summary>
		[Test]
		public void OneItemInCollectionRemains()
		{
			var collData = new XElement("CollectionProperty",
										new XElement("objsur", new XAttribute("guid", "c1ecf889-e382-11de-8a39-0800200c9a66")));
			DataSortingService.SortCollectionProperties(collData);
			Assert.AreEqual("CollectionProperty", collData.Name.LocalName);
			Assert.AreEqual(1, collData.Elements().Count());
			Assert.AreEqual("c1ecf889-e382-11de-8a39-0800200c9a66", collData.Element("objsur").Attribute("guid").Value);
		}

		/// <summary>
		/// Check that collection property is sorted.
		/// </summary>
		[Test]
		public void CollectionPropertyIsSorted()
		{
			var collData = new XElement("CollectionProperty",
				new XElement("objsur", new XAttribute("guid", "c1ecf88b-e382-11de-8a39-0800200c9a66")),
				new XElement("objsur", new XAttribute("guid", "c1ecf88a-e382-11de-8a39-0800200c9a66")));
			DataSortingService.SortCollectionProperties(collData);
			Assert.AreEqual("CollectionProperty", collData.Name.LocalName);
			Assert.AreEqual(2, collData.Elements().Count());
			Assert.AreEqual("c1ecf88a-e382-11de-8a39-0800200c9a66", collData.Elements().ElementAt(0).Attribute("guid").Value);
			Assert.AreEqual("c1ecf88b-e382-11de-8a39-0800200c9a66", collData.Elements().ElementAt(1).Attribute("guid").Value);
		}

		/// <summary>
		/// Check that collection property has no duplicates.
		/// </summary>
		[Test]
		public void DuplicatesInCollectionPropertyAreRemoved()
		{
			var collData = new XElement("CollectionProperty",
				new XElement("objsur", new XAttribute("guid", "c1ecf88b-e382-11de-8a39-0800200c9a66")),
				new XElement("objsur", new XAttribute("guid", "c1ecf88b-e382-11de-8a39-0800200c9a66")),
				new XElement("objsur", new XAttribute("guid", "c1ecf88a-e382-11de-8a39-0800200c9a66")));
			DataSortingService.SortCollectionProperties(collData);
			Assert.AreEqual("CollectionProperty", collData.Name.LocalName);
			Assert.AreEqual(2, collData.Elements().Count());
			Assert.AreEqual("c1ecf88a-e382-11de-8a39-0800200c9a66", collData.Elements().ElementAt(0).Attribute("guid").Value);
			Assert.AreEqual("c1ecf88b-e382-11de-8a39-0800200c9a66", collData.Elements().ElementAt(1).Attribute("guid").Value);
		}

		/// <summary>
		/// Check for one item in multi-something property.
		/// </summary>
		[Test]
		public void OneItemInMultiRemains()
		{
			var multiSomethingData = new XElement("MultiSomething",
										new XElement("AStr", new XAttribute("ws", "en")));
			DataSortingService.SortMultiSomethingProperty(multiSomethingData);
			Assert.AreEqual("MultiSomething", multiSomethingData.Name.LocalName);
			Assert.AreEqual(1, multiSomethingData.Elements().Count());
			Assert.AreEqual("en", multiSomethingData.Element("AStr").Attribute("ws").Value);
		}

		/// <summary>
		/// Check that multi-something property is sorted.
		/// </summary>
		[Test]
		public void MultiSomethingPropertyIsSorted()
		{
			var multiSomethingData = new XElement("MultiSomething",
										new XElement("AStr", new XAttribute("ws", "es")),
										new XElement("AStr", new XAttribute("ws", "en")));
			DataSortingService.SortMultiSomethingProperty(multiSomethingData);
			Assert.AreEqual("MultiSomething", multiSomethingData.Name.LocalName);
			Assert.AreEqual(2, multiSomethingData.Elements().Count());
			Assert.AreEqual("en", multiSomethingData.Elements().ElementAt(0).Attribute("ws").Value);
			Assert.AreEqual("es", multiSomethingData.Elements().ElementAt(1).Attribute("ws").Value);
		}

		/// <summary>
		/// Check that all attributes are sorted.
		/// </summary>
		[Test]
		public void AttributesAreSorted()
		{
			var root = new XElement("outer",
				new XAttribute("outerB", "outerBData"),
				new XAttribute("outera", "outerAData"),
				new XElement("inner", new XAttribute("innerB", "innerBData"), new XAttribute("innerA", "innerAData")));
			DataSortingService.SortAttributes(root);
			Assert.AreEqual("outer", root.Name.LocalName);
			Assert.AreEqual(1, root.Elements().Count());

			Assert.AreEqual("outerAData", root.Attributes().ElementAt(0).Value);
			Assert.AreEqual("outerBData", root.Attributes().ElementAt(1).Value);

			Assert.AreEqual("innerAData", root.Element("inner").Attributes().ElementAt(0).Value);
			Assert.AreEqual("innerBData", root.Element("inner").Attributes().ElementAt(1).Value);
		}

		/// <summary>
		/// Check that custom property declarations are sorted.
		/// </summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification="Newline is in source file and so will be correct according to current platform")]
		public void CustomPropertiesAreSorted()
		{
			const string sortedCustomData =
@"<AdditionalFields>
<CustomField type='Boolean' name='Certified' class='WfiWordform' />
<CustomField destclass='7' listRoot='53241fd4-72ae-4082-af55-6b659657083c' name='Tone' type='RC' class='LexEntry' />
<CustomField wsSelector='-2' type='String' name='Paradigm' class='LexEntry' />
</AdditionalFields>";
			var sortedCustomDataElement = DataSortingService.SortCustomPropertiesRecord(sortedCustomData);
			Assert.AreEqual("AdditionalFields", sortedCustomDataElement.Name.LocalName);
			Assert.AreEqual(3, sortedCustomDataElement.Elements().Count());

			var customData = sortedCustomDataElement.Elements().ElementAt(0);
			CheckAttributes(customData,
				new List<string> { "class", "name", "type", "wsSelector" },
				new List<string> {"LexEntry", "Paradigm", "String", "-2" });

			customData = sortedCustomDataElement.Elements().ElementAt(1);
			CheckAttributes(customData,
				new List<string> { "class", "destclass", "listRoot", "name", "type" },
				new List<string> { "LexEntry", "7", "53241fd4-72ae-4082-af55-6b659657083c", "Tone", "RC" });

			customData = sortedCustomDataElement.Elements().ElementAt(2);
			CheckAttributes(customData,
				new List<string> { "class", "name", "type" },
				new List<string> {	"WfiWordform", "Certified", "Boolean" });
		}

		private static void CheckAttributes(XElement customData, IList<string> expectedAttributeNames, IList<string> expectedAttributeValues)
		{
			for (var i = 0; i < expectedAttributeNames.Count; ++i)
			{
				var attr = customData.Attributes().ElementAt(i);
				Assert.AreEqual(expectedAttributeNames[i], attr.Name.LocalName);
				Assert.AreEqual(expectedAttributeValues[i], attr.Value);
			}
		}

		/// <summary>
		/// Check that a main element is sorted.
		/// </summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification="Newline is in source file and so will be correct according to current platform")]
		public void SortMainElement()
		{
			const string rt =
@"<rt ownerguid='fe832a87-4846-4895-9c7e-98c5da0c84ba' class='CmPossibilityList' guid='fb5e83e5-6576-455d-aba0-0b7a722b9b5d'>
<Possibilities>
<objsur guid='d3c9c406-3ed6-4529-8807-db0864d2df07' t='o' />
<objsur guid='595daad3-9b65-43dc-b60c-705544921559' t='o' />
</Possibilities>
<Custom name='CustomProp' />
<DateCreated val='1995-1-25 13:22:29.0' />
<Abbreviation>
<AUni ws='es'>Categorías Gramáticas</AUni>
<AUni ws='en'>Parts Of Speech</AUni>
</Abbreviation>
</rt>";
			var sortableProperties = new Dictionary<string, Dictionary<string, HashSet<string>>>();
			var pl = new Dictionary<string, HashSet<string>>();
			sortableProperties.Add("CmPossibilityList", pl);
			var hs = new HashSet<string> {"Possibilities"};
			pl.Add("Collections", hs);
			hs = new HashSet<string> { "Abbreviation" };
			pl.Add("MultiAlt", hs);

			//var tempInputPathname = Path.GetTempFileName();
			//var tempOutputPathname = Path.GetTempFileName();
			//File.WriteAllText(tempInputPathname, rt);
			//try
			//{
			//	using (var writer = XmlWriter.Create(tempOutputPathname))
			//	{
			var rtElement = DataSortingService.SortMainElement(sortableProperties, rt);
			//		writer.Flush();
			//		writer.Close();
			//	}
			//	var doc = XDocument.Load(tempOutputPathname);
			Assert.AreEqual("class", rtElement.Attributes().ElementAt(0).Name.LocalName);
			Assert.AreEqual("guid", rtElement.Attributes().ElementAt(1).Name.LocalName);
			Assert.AreEqual("ownerguid", rtElement.Attributes().ElementAt(2).Name.LocalName);

			Assert.AreEqual(4, rtElement.Elements().Count());
			var sortedProp = rtElement.Elements().ElementAt(0);
			Assert.AreEqual("en", sortedProp.Element("AUni").Attribute("ws").Value); // Make sure SortMainElement called mutli sorter.
			Assert.AreEqual("Abbreviation", sortedProp.Name.LocalName);
			Assert.AreEqual("Custom", rtElement.Elements().ElementAt(1).Name.LocalName);
			Assert.AreEqual("DateCreated", rtElement.Elements().ElementAt(2).Name.LocalName);
			sortedProp = rtElement.Elements().ElementAt(3);
			Assert.AreEqual("595daad3-9b65-43dc-b60c-705544921559", sortedProp.Element("objsur").Attribute("guid").Value); // Make sure SortMainElement called coll sorter.
			Assert.AreEqual("Possibilities", sortedProp.Name.LocalName);
			//}
			//finally
			//{
			//	//File.Delete(tempInputPathname);
			//	File.Delete(tempOutputPathname);
			//}
		}

		/// <summary>
		/// Check that the whole file is sorted.
		/// </summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification="Newline is in source file and so will be correct according to current platform")]
		public void SortEntireFile()
		{
			const string rt =
@"<?xml version='1.0' encoding='utf-8'?>
<languageproject version='7000037'>
<AdditionalFields>
<CustomField class='WfiWordform' name='Certified' type='Boolean' />
<CustomField class='LexEntry' destclass='7' listRoot='53241fd4-72ae-4082-af55-6b659657083c' name='Tone' type='RC' />
</AdditionalFields>
<rt guid='c1ecf88d-e382-11de-8a39-0800200c9a66' class='WfiWordform' />
<rt guid='c1ecf88c-e382-11de-8a39-0800200c9a66' class='LexEntry' />
</languageproject>";

			var tempInputPathname = Path.GetTempFileName();
			var tempOutputPathname = Path.GetTempFileName();
			File.WriteAllText(tempInputPathname, rt);
			try
			{
				using (var writer = XmlWriter.Create(tempOutputPathname))
				{
					writer.WriteStartElement("languageproject");
					DataSortingService.SortEntireFile(((IFwMetaDataCacheManagedInternal)m_mdc).GetSortableProperties(), writer, tempInputPathname);
					writer.WriteEndElement();
					writer.Flush();
					writer.Close();
				}
				var doc = XDocument.Load(tempOutputPathname);
				var rtElement = doc.Root;
				Assert.AreEqual(3, rtElement.Elements().Count());
				var sortedProp = rtElement.Elements().ElementAt(0);
				Assert.AreEqual("LexEntry", sortedProp.Element("CustomField").Attribute("class").Value); // Make sure SortCustomPropertiesRecord was called.
				sortedProp = rtElement.Elements().ElementAt(1);
				Assert.AreEqual("c1ecf88c-e382-11de-8a39-0800200c9a66", sortedProp.Attribute("guid").Value); // Make sure SortMainElement was called.
			}
			finally
			{
				File.Delete(tempInputPathname);
				File.Delete(tempOutputPathname);
			}
		}
	}
}
