// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 3.1, derive-on-commit) — the reusable port of the legacy
	/// <c>BasicIPASymbolSlice</c> derive logic: when a phoneme's <c>BasicIPASymbol</c> is set, fill its
	/// Description (per analysis writing system) and phonological Features from <c>BasicIPAInfo.xml</c>;
	/// clearing the symbol clears them. Fills only EMPTY Description/Features (never overwrites a user's
	/// own edits), exactly like the legacy guard. LCModel writes happen inside the caller's fenced UOW.
	/// </summary>
	public static class BasicIpaSymbolDeriver
	{
		private static readonly XDocument s_ipaInfo = LoadIpaInfo();

		private static XDocument LoadIpaInfo()
		{
			try
			{
				return XDocument.Load(Path.Combine(FwDirectoryFinder.TemplateDirectory, PhPhonemeTags.ksBasicIPAInfoFile));
			}
			catch
			{
				return null; // missing template: derive becomes a no-op, the symbol still saves
			}
		}

		/// <summary>Derive Description + Features for <paramref name="phoneme"/> from its current BasicIPASymbol.</summary>
		public static void Derive(IPhPhoneme phoneme, LcmCache cache)
		{
			if (phoneme == null || cache == null)
				return;
			var symbol = phoneme.BasicIPASymbol?.Text ?? string.Empty;
			DeriveDescription(phoneme, cache, symbol);
			DeriveFeatures(phoneme, cache, symbol);
		}

		private static void DeriveDescription(IPhPhoneme phoneme, LcmCache cache, string symbol)
		{
			foreach (CoreWritingSystemDefinition ws in cache.ServiceLocator.WritingSystems.AnalysisWritingSystems)
			{
				var handle = ws.Handle;
				var existing = phoneme.Description.get_String(handle).Text;
				if (symbol.Length == 0)
				{
					// Clearing the symbol clears a previously-derived description.
					if (!string.IsNullOrEmpty(existing))
						phoneme.Description.set_String(handle, string.Empty);
					continue;
				}
				if (!string.IsNullOrEmpty(existing))
					continue; // never overwrite the user's own description (legacy guard)
				var description = s_ipaInfo?.XPathSelectElement(
					"/SegmentDefinitions/SegmentDefinition[Representations/Representation[.='" +
					XmlUtils.MakeSafeXmlAttribute(symbol) + "']]/Descriptions/Description[@lang='" + ws.Id + "']");
				if (description != null)
					phoneme.Description.set_String(handle, (string)description);
			}
		}

		private static void DeriveFeatures(IPhPhoneme phoneme, LcmCache cache, string symbol)
		{
			if (symbol.Length == 0)
			{
				phoneme.FeaturesOA?.FeatureSpecsOC.Clear();
				return;
			}
			// Only populate when there are no features yet (never clobber the user's own feature edits).
			if (phoneme.FeaturesOA != null && phoneme.FeaturesOA.FeatureSpecsOC.Count > 0)
				return;

			var features = s_ipaInfo?.XPathSelectElement(
				"/SegmentDefinitions/SegmentDefinition[Representations/Representation[.='" +
				XmlUtils.MakeSafeXmlAttribute(symbol) + "']]/Features");
			if (features == null)
				return;

			var featSystem = cache.LangProject.PhFeatureSystemOA;
			foreach (var pair in features.Elements("FeatureValuePair"))
			{
				var featDefn = featSystem.GetFeature((string)pair.Attribute("feature"));
				var symVal = featSystem.GetSymbolicValue((string)pair.Attribute("value"));
				if (featDefn == null || symVal == null)
					continue;
				if (phoneme.FeaturesOA == null)
					phoneme.FeaturesOA = cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
				var value = cache.ServiceLocator.GetInstance<IFsClosedValueFactory>().Create();
				phoneme.FeaturesOA.FeatureSpecsOC.Add(value);
				value.FeatureRA = featDefn;
				value.ValueRA = symVal;
			}
		}
	}
}
