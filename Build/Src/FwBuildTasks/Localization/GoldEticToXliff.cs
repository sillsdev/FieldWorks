// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// ReSharper disable PossibleNullReferenceException - Wolf!

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	public class GoldEticToXliff : Task
	{
		private const string WsEn = "en";
		private const string WsAtt = "ws";

		internal static readonly List<ConversionMap> NormalEltMaps = new List<ConversionMap>
		{
			new ConversionMap("abbrev", "_abbr"),
			new ConversionMap("term", "_term"),
			new ConversionMap("def", "_def")
		};

		internal static readonly ConversionMap CitationMap = new ConversionMap("citation", "_cit");

		[Required]
		public string SourceXml { get; set; }

		[Required]
		public string XliffOutputDir { get; set; }

		public override bool Execute()
		{
			// log only if we're not running tests
			if (BuildEngine != null)
			{
				Log.LogMessage($"Converting '{SourceXml}' to XLIFF at '{XliffOutputDir}'.");
			}

			foreach (var kvp in ConvertGoldEticToXliff(SourceXml))
			{
				kvp.Value.Save(Path.Combine(XliffOutputDir, $"{Path.GetFileNameWithoutExtension(SourceXml)}.{kvp.Key}.xlf"));
			}

			return true;
		}

		private static Dictionary<string, XDocument> ConvertGoldEticToXliff(string inFileName)
		{
			using (var reader = new XmlTextReader(inFileName))
			{
				return ConvertGoldEticToXliff(inFileName, XDocument.Load(reader));
			}
		}

		internal static Dictionary<string, XDocument> ConvertGoldEticToXliff(string inFileName, XDocument goldEtic)
		{
			// Determine available WS's based on the first item. All others should have the same WS's.
			var firstItemTerms = goldEtic.XPathSelectElement("/eticPOSList/item")?.XPathSelectElements("term").ToList();
			if (firstItemTerms == null)
			{
				throw new ArgumentException($"The given file had no items: {inFileName}");
			}

			// the source (cites the file's source) is not localized, but should be included.
			// The source element has a link element within its text; getting its Nodes preserves this link
			var citeSourceParts = goldEtic.XPathSelectElement("/eticPOSList/source").Nodes().Select(n => n.ToString());
			var citeSourceElt = XElement.Load(new XmlTextReader(
				$"<sil:source>{string.Join("", citeSourceParts)}</sil:source>",
				XmlNodeType.Element,
				new XmlParserContext(null, XliffUtils.NameSpaceManager, null, XmlSpace.None)));

			var baseXliff = string.Format(XliffUtils.BodyTemplate, Path.GetFileName(inFileName));
			var xliffDocs = new Dictionary<string, XDocument>();
			foreach (var ws in firstItemTerms.Select(t => t.Attribute(WsAtt).Value))
			{
				xliffDocs[ws] = XDocument.Parse(baseXliff);
				if (ws != WsEn)
				{
					xliffDocs[ws].XPathSelectElement("/xliff/file").SetAttributeValue("target-language", ws);
				}
				var bodyElem = xliffDocs[ws].XPathSelectElement("/xliff/file/body", XliffUtils.NameSpaceManager);
				bodyElem.Add(citeSourceElt);
				ConvertItems(goldEtic.XPathSelectElement("/eticPOSList"), bodyElem, ws);
			}

			return xliffDocs;
		}

		private static void ConvertItems(XElement sourceElt, XElement destElt, string targetLang)
		{
			foreach (var item in sourceElt.Elements("item"))
			{
				var id = item.Attribute("id").Value;
				var guid = item.Attribute("guid").Value;
				var itemGroup = XElement.Parse(string.Format(XliffUtils.EmptyGroup, $"{guid}_{id}"));

				foreach (var eltMap in NormalEltMaps)
				{
					var sourceTxt = item.XPathSelectElement($"{eltMap.ElementName}[@ws='{WsEn}']").Value;
					var transUnit = XElement.Parse(string.Format(XliffUtils.TransUnitTemplate, $"{guid}_{id}{eltMap.IdSuffix}", sourceTxt));
					if (targetLang != WsEn)
					{
						var val = item.XPathSelectElement($"{eltMap.ElementName}[@ws='{targetLang}']").Value;
						if (!string.IsNullOrWhiteSpace(val))
						{
							transUnit.Add(XElement.Parse(string.Format(XliffUtils.FinalTargetTemplate, val)));
						}
					}
					itemGroup.Add(transUnit);
				}

				// Each language within an item can independently have zero, one, or many citations
				var citationsTransUnit = XElement.Parse(string.Format(XliffUtils.TransUnitTemplate,
					$"{guid}_{id}{CitationMap.IdSuffix}", string.Join(";",
						item.XPathSelectElements($"{CitationMap.ElementName}[@ws='{WsEn}']").Select(c => c.Value))));
				citationsTransUnit.Add(XElement.Parse(
					"<note>If you consult additional sources that describe this grammatical category, include a " +
					"semicolon-separated list of source citations (e.g. \"Alvarez 1905:103;Comrie 1989:42;Payne 1997:86\").\n" +
					"If you translate the existing information without consulting additional sources, " +
					"please leave this blank.</note>"));
				if (targetLang != WsEn)
				{
					var val = string.Join(";", item.XPathSelectElements($"{CitationMap.ElementName}[@ws='{targetLang}']").Select(c => c.Value));
					if (!string.IsNullOrEmpty(val))
					{
						citationsTransUnit.Add(XElement.Parse(string.Format(XliffUtils.FinalTargetTemplate, val)));
					}
				}
				itemGroup.Add(citationsTransUnit);

				ConvertItems(item, itemGroup, targetLang);

				destElt.Add(itemGroup);
			}
		}
	}
}
