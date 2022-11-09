// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// ReSharper disable PossibleNullReferenceException -- Wolf!

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	public class XliffToGoldEtic : Task
	{
		private const string WsEn = "en";
		private const string Source = "source";
		private const string Target = "target";

		[Required]
		public string[] XliffSourceFiles { get; set; }

		[Required]
		public string OutputXml { get; set; }

		public override bool Execute()
		{
			// log only if we're not running tests
			if (BuildEngine != null)
			{
				Log.LogMessage($"Combining {XliffSourceFiles.Length} translations of GOLD Etic grammatical categories into '{OutputXml}'.");
			}
			CombineXliffs(XliffSourceFiles.Select(OpenXliff).ToList()).Save(OutputXml);
			return true;
		}

		private static XDocument OpenXliff(string xliffFile)
		{
			using (var reader = new XmlTextReader(xliffFile))
			{
				return XDocument.Load(reader);
			}
		}

		public XDocument CombineXliffs(List<XDocument> xliffDocs)
		{
			var result = XDocument.Parse("<?xml version='1.0' encoding='utf-8'?><eticPOSList/>");

			// The source element has a link element within its text; getting its Nodes preserves this link
			var citeSourceParts = xliffDocs.First().XPathSelectElement("/xliff/file/body")
				.Element(XliffUtils.SilNamespace + Source).Nodes().Select(n => n.ToString());
			var citeSourceElt = XElement.Parse($"<source>{string.Join("", citeSourceParts)}</source>");
			result.Root.Add(citeSourceElt);

			AddItems(result.Root, xliffDocs.ToDictionary(
				xDoc => NormalizeLocales.Normalize(xDoc.XPathSelectElement("/xliff/file").Attribute("target-language")?.Value ?? WsEn),
				xDoc => xDoc.XPathSelectElement("/xliff/file/body")));

			return result;
		}

		private void AddItems(XElement target, Dictionary<string, XElement> sources)
		{
			var firstSource = sources.Values.First();
			var firstSourceGroups = firstSource.Elements("group").ToList();
			foreach (var kvp in sources.Where(kvp => firstSourceGroups.Count != kvp.Value.Elements("group").Count()))
			{
				var groupId = firstSource.Attribute("id")?.Value;
				var idMessage = groupId == null ? string.Empty : $" for {groupId}";
				Log.LogError($"GOLD Etic for {kvp.Key} has a different number of items than {sources.Keys.First()}{idMessage}");
			}

			foreach (var itemGroup in firstSourceGroups)
			{
				var groupId = itemGroup.Attribute("id").Value;

				var allSourcesGroups = sources.ToDictionary(kvp => kvp.Key,
					kvp => kvp.Value.Elements("group").FirstOrDefault(elt => elt.Attribute("id").Value == groupId));
				var badSourceLangs = new List<string>();
				foreach (var kvp in allSourcesGroups.Where(kvp => kvp.Value == null))
				{
					Log.LogError($"GOLD Etic for {kvp.Key} does not contain {groupId}");
					badSourceLangs.Add(kvp.Key);
				}

				// For now, ignore any bad files and see how far we get with others
				foreach (var badSourceLang in badSourceLangs)
				{
					allSourcesGroups.Remove(badSourceLang);
				}

				// These loops would look cleaner if languages were in the outer loop, but this is how the original GOLDEtic.xml
				// is organized. Hasso would not be opposed to inverting them now that everything is done and tested.
				var groupIdLength = groupId.Length;
				var groupIdParts = groupId.Split('_');
				var itemElt = XElement.Parse($"<item type='category' id='{groupIdParts[1]}' guid='{groupIdParts[0]}'/>");
				foreach (var eltMap in GoldEticToXliff.NormalEltMaps)
				{
					var englishValue = TransUnitFor(itemGroup, groupIdLength, eltMap).Element(Source).Value;
					itemElt.Add(XElement.Parse($"<{eltMap.ElementName} ws='{WsEn}'>{englishValue}</{eltMap.ElementName}>"));
					foreach (var kvp in allSourcesGroups)
					{
						var translationElt = TransUnitFor(kvp.Value, groupIdLength, eltMap).Element(Target);
						if (XliffUtils.IsTranslated(translationElt))
						{
							itemElt.Add(XElement.Parse($"<{eltMap.ElementName} ws='{kvp.Key}'>{translationElt.Value}</{eltMap.ElementName}>"));
						}
					}
				}

				AddCitations(itemElt, WsEn, TransUnitFor(itemGroup, groupIdLength, GoldEticToXliff.CitationMap).Element(Source));
				foreach (var kvp in allSourcesGroups)
				{
					AddCitations(itemElt, kvp.Key, TransUnitFor(kvp.Value, groupIdLength, GoldEticToXliff.CitationMap).Element(Target));
				}

				// convert subitems
				AddItems(itemElt, allSourcesGroups);

				target.Add(itemElt);
			}
		}

		private static XElement TransUnitFor(XElement itemGroup, int idBaseLength, ConversionMap eltMap)
		{
			return itemGroup.Elements("trans-unit").First(e => e.Attribute("id").Value.Substring(idBaseLength) == eltMap.IdSuffix);
		}

		/// <param name="item"></param>
		/// <param name="ws"></param>
		/// <param name="citElt">The source or target element containing the citations</param>
		private static void AddCitations(XElement item, string ws, XElement citElt)
		{
			// Convert only source elements and properly-translated target elements
			if (citElt?.Name != Source && !XliffUtils.IsTranslated(citElt))
			{
				return;
			}

			foreach (var citation in citElt.Value.Split(';').Where(citation => !string.IsNullOrEmpty(citation)))
			{
				item.Add(XElement.Parse(
					$"<{GoldEticToXliff.CitationMap.ElementName} ws='{ws}'>{citation}</{GoldEticToXliff.CitationMap.ElementName}>"));
			}
		}
	}
}
