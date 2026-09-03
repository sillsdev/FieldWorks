// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Writes the phonological features the Basic IPA inventory defines for a phoneme's
	/// Basic IPA symbol.
	/// </summary>
	/// <remarks>
	/// A plain service rather than a method on BasicIPASymbolSlice so that the repair for
	/// phonemes already carrying duplicates (LT-22716) has something to call: neither a
	/// Tools &gt; Utilities utility nor a FixData rule can call a UserControl, which every
	/// Slice is.
	///
	/// Callers own the unit of work. Nothing here starts one.
	/// </remarks>
	public static class PhonemeFeaturePopulator
	{
		/// <summary>
		/// Applies the inventory's feature specifications for <paramref name="phoneme"/>'s
		/// current Basic IPA symbol, and returns how many it wrote.
		/// </summary>
		/// <remarks>
		/// Idempotent by construction: an existing specification for a feature is updated
		/// rather than joined by a second one, so calling this repeatedly cannot leave a
		/// phoneme with two contradictory values for one feature (LT-22714). That holds
		/// however the caller decides when to call it.
		///
		/// A feature or value the project's phonological feature system does not define is
		/// skipped, since a memory-only or partially-configured project need not have every
		/// feature the shipped inventory names.
		/// </remarks>
		public static int ApplyFeaturesFromIpaSymbol(LcmCache cache, IPhPhoneme phoneme,
			XDocument ipaInfo)
		{
			var symbol = phoneme.BasicIPASymbol;
			if (symbol == null || symbol.Length == 0)
				return 0;

			// Mono XPath processing crashes when the expression starts out with // here.
			// See FWNX-730.
			var xpath = "/SegmentDefinitions/SegmentDefinition[Representations/Representation[.='"
				+ XmlUtils.MakeSafeXmlAttribute(symbol.Text) + "']]/Features";
			var features = ipaInfo.XPathSelectElement(xpath);
			if (features == null)
				return 0;

			var featureSystem = cache.LanguageProject.PhFeatureSystemOA;
			var written = 0;
			foreach (var feature in features.Elements("FeatureValuePair"))
			{
				var closedFeat = featureSystem.GetFeature((string)feature.Attribute("feature"))
					as IFsClosedFeature;
				if (closedFeat == null)
					continue;
				var symVal = featureSystem.GetSymbolicValue((string)feature.Attribute("value"));
				if (symVal == null)
					continue;

				if (phoneme.FeaturesOA == null)
					phoneme.FeaturesOA = cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();

				var value = phoneme.FeaturesOA.GetOrCreateValue(closedFeat);
				value.FeatureRA = closedFeat;
				value.ValueRA = symVal;
				written++;
			}
			return written;
		}

		/// <summary>Removes every feature specification the phoneme holds.</summary>
		public static void ClearFeatures(IPhPhoneme phoneme)
		{
			if (phoneme.FeaturesOA != null)
				phoneme.FeaturesOA.FeatureSpecsOC.Clear();
		}

		/// <summary>
		/// The features this phoneme specifies more than once, by catalog id. Empty is the
		/// healthy state; a non-empty result is the LT-22714 damage.
		/// </summary>
		public static string[] DuplicatedFeatureIds(IPhPhoneme phoneme)
		{
			if (phoneme.FeaturesOA == null)
				return new string[0];
			return phoneme.FeaturesOA.FeatureSpecsOC
				.GroupBy(spec => spec.FeatureRA)
				.Where(group => group.Count() > 1)
				.Select(group => group.Key == null ? "(none)" : group.Key.CatalogSourceId)
				.ToArray();
		}
	}
}
