using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Palaso.Xml;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// Class used to sort all of the data in the XML BEP, so Mercurial doesn't suffer so much.
	/// </summary>
	internal static class DataSortingService
	{
		internal static readonly Encoding Utf8 = Encoding.UTF8;
		private const string OptionalFirstElementTag = "AdditionalFields";
		private const string StartTag = "rt";
		private const string Collections = "Collections";
		private const string MultiAlt = "MultiAlt";

		internal static void SortEntireFile(Dictionary<string, Dictionary<string, HashSet<string>>> sortableProperties, XmlWriter writer, string pathname)
		{
			var readerSettings = new XmlReaderSettings { IgnoreWhitespace = true };

			// Step 2: Sort and rewrite file.
			using (var fastSplitter = new FastXmlElementSplitter(pathname))
			{
				var sortedObjects = new SortedDictionary<string, string>();
				bool foundOptionalFirstElement;
				foreach (var record in fastSplitter.GetSecondLevelElementStrings(OptionalFirstElementTag, StartTag, out foundOptionalFirstElement))
				{
					if (foundOptionalFirstElement)
					{
						// Step 2A: Write out custom property declaration(s).
						WriteElement(writer, readerSettings, SortCustomPropertiesRecord(record));
						foundOptionalFirstElement = false;
					}
					else
					{
						// Step 2B: Sort main CmObject record.
						var sortedMainObject = SortMainElement(sortableProperties, record);
						sortedObjects.Add(sortedMainObject.Attribute("guid").Value, sortedMainObject.ToString());
					}
				}
				foreach (var sortedObjectKvp in sortedObjects)
				{
					WriteElement(writer, readerSettings, sortedObjectKvp.Value);
				}
			}
		}

		internal static XElement SortCustomPropertiesRecord(string optionalFirstElement)
		{
			var customPropertiesElement = XElement.Parse(optionalFirstElement);

			SortCustomPropertiesRecord(customPropertiesElement);

			return customPropertiesElement;
		}

		internal static void SortCustomPropertiesRecord(XElement customPropertiesElement)
		{
			// <CustomField name="Certified" class="WfiWordform" type="Boolean" ... />

			// 1. Sort child elements by using a compound key of 'class'+'name'.
			var sortedCustomProperties = new SortedDictionary<string, XElement>();
			foreach (var customProperty in customPropertiesElement.Elements())
			{
// ReSharper disable PossibleNullReferenceException
				// Needs to add 'key' attr, which is class+name, so fast splitter has one id attr to use in its work.
				sortedCustomProperties.Add(customProperty.Attribute("class").Value + customProperty.Attribute("name").Value, customProperty);
// ReSharper restore PossibleNullReferenceException
			}
			customPropertiesElement.Elements().Remove();
			foreach (var propertyKvp in sortedCustomProperties)
				customPropertiesElement.Add(propertyKvp.Value);

			// Sort all attributes.
			SortAttributes(customPropertiesElement);
		}

		internal static XElement SortMainElement(Dictionary<string, Dictionary<string, HashSet<string>>> sortableProperties, string rootData)
		{
			var sortedResult = XElement.Parse(rootData);
			var className = sortedResult.Attribute("class").Value;

			// Get collection properties for the class.
			Dictionary<string, HashSet<string>> sortablePropertiesForClass;
			if (!sortableProperties.TryGetValue(className, out sortablePropertiesForClass))
			{
				// Appears to be a newly obsolete instance of 'className'.
				sortablePropertiesForClass = new Dictionary<string, HashSet<string>>(3, StringComparer.OrdinalIgnoreCase)
												{
													{Collections, new HashSet<string>()},
													{MultiAlt, new HashSet<string>()}
												};
				sortableProperties.Add(className, sortablePropertiesForClass);
			}

			var collData = sortablePropertiesForClass[Collections];
			var multiAltData = sortablePropertiesForClass[MultiAlt];

			var sortedPropertyElements = new SortedDictionary<string, XElement>();
			foreach (var propertyElement in sortedResult.Elements())
			{
				var propName = propertyElement.Name.LocalName;
				// <Custom name="Certified" val="True" />
// ReSharper disable PossibleNullReferenceException
				if (propName == "Custom")
					propName = propertyElement.Attribute("name").Value; // Sort custom props by their name attrs.
// ReSharper restore PossibleNullReferenceException
				if (collData.Contains(propName))
					SortCollectionProperties(propertyElement);
				if (multiAltData.Contains(propName))
					SortMultiSomethingProperty(propertyElement);
				sortedPropertyElements.Add(propName, propertyElement);
			}
			sortedResult.Elements().Remove();
			foreach (var kvp in sortedPropertyElements)
				sortedResult.Add(kvp.Value);

			// 3. Sort attributes at all levels.
			SortAttributes(sortedResult);

			return sortedResult;
		}

		internal static void SortAttributes(XElement element)
		{
			if (element.HasElements)
			{
				foreach (var childElement in element.Elements())
					SortAttributes(childElement);
			}

			if (element.Attributes().Count() < 2)
				return;

			var sortedAttributes = new SortedDictionary<string, XAttribute>();
			foreach (var attr in element.Attributes())
				sortedAttributes.Add(attr.Name.LocalName, attr);

			element.Attributes().Remove();
			foreach (var sortedAttrKvp in sortedAttributes)
				element.Add(sortedAttrKvp.Value);
		}

		internal static void SortMultiSomethingProperty(XContainer multiSomethingProperty)
		{
			if (multiSomethingProperty.Elements().Count() < 2)
				return;

			var sortedAlternativeElements = new SortedDictionary<string, XElement>();
			foreach (var alternativeElement in multiSomethingProperty.Elements())
			{
				var ws = alternativeElement.Attribute("ws").Value;
				if (!sortedAlternativeElements.ContainsKey(ws))
					sortedAlternativeElements.Add(ws, alternativeElement);
			}

			multiSomethingProperty.Elements().Remove();
			foreach (var kvp in sortedAlternativeElements)
				multiSomethingProperty.Add(kvp.Value);
		}

		internal static void SortCollectionProperties(XContainer propertyElement)
		{
			if (propertyElement.Elements().Count() < 2)
				return;

			// Write collection properties in guid sorted order,
			// since order is not significant in collections.
			var sortCollectionData = new SortedDictionary<string, XElement>();
			foreach (var objsurElement in propertyElement.Elements("objsur"))
			{
// ReSharper disable PossibleNullReferenceException
				var key = objsurElement.Attribute("guid").Value;
// ReSharper restore PossibleNullReferenceException
				if (!sortCollectionData.ContainsKey(key))
					sortCollectionData.Add(key, objsurElement);
			}

			propertyElement.Elements().Remove();
			foreach (var kvp in sortCollectionData)
				propertyElement.Add(kvp.Value);
		}

		internal static void WriteElement(XmlWriter writer, XmlReaderSettings readerSettings, XElement element)
		{
			WriteElement(writer, readerSettings, element.ToString());
		}

		internal static void WriteElement(XmlWriter writer, XmlReaderSettings readerSettings, string element)
		{
			using (var nodeReader = XmlReader.Create(new MemoryStream(Utf8.GetBytes(element), false), readerSettings))
				writer.WriteNode(nodeReader, true);
		}

		public static List<byte[]> GetLessorNewbies(Guid currentGuid, SortedDictionary<Guid, byte[]> newbies)
		{
			var results = new List<byte[]>();
			var keys = new List<Guid>();

			foreach (var newbieKvp in newbies.TakeWhile(newbieKvp => newbieKvp.Key.CompareTo(currentGuid) < 0))
			{
				keys.Add(newbieKvp.Key);
				results.Add(newbieKvp.Value);
			}
			// Remove items in results from 'newbies'.
			foreach (var key in keys)
			{
				newbies.Remove(key);
			}

			return results;
		}
	}
}