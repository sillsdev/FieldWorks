// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Xml.Linq;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// A special filter, where items are LexSenses, and matches are ones where an MSA is an MoStemMsa that
	/// has the correct POS.
	/// </summary>
	internal sealed class InflectionClassFilter : ColumnSpecFilter
	{
		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		internal InflectionClassFilter(XElement element)
			: base(element)
		{
		}

		internal InflectionClassFilter(LcmCache cache, ListMatchOptions mode, int[] targets, XElement colSpec)
			: base(cache, mode, targets, colSpec)
		{
		}

		protected override string BeSpec => "InflectionClassFilter";

		internal override bool CompatibleFilter(XElement colSpec)
		{
			if (!base.CompatibleFilter(colSpec))
			{
				return false;
			}
			return XmlUtils.GetMandatoryAttributeValue(colSpec, "class").Split('.').Last().Trim() == "InflectionClassEditor";
		}

		/// <summary>
		/// This is a filter for an atomic property, and the "all" and "only" options should not be presented.
		/// Review JOhnT: is this true?
		/// </summary>
		/// <remarks>NB: Used by reflection.</remarks>
		internal static bool Atomic => true;

		/// <summary>
		/// The items for this filter are the leaves of the tree formed by the possibilities in the list,
		/// by following the InflectionClasses property of each PartOfSpeech.
		/// </summary>
		/// <remarks>NB: Used by reflection.</remarks>
		internal static int LeafFlid => PartOfSpeechTags.kflidInflectionClasses;
	}
}
