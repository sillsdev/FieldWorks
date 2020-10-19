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
	internal sealed class PosFilter : ColumnSpecFilter
	{
		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		internal PosFilter(XElement element)
			: base(element)
		{}

		internal PosFilter(LcmCache cache, ListMatchOptions mode, int[] targets, XElement colSpec)
			: base(cache, mode, targets, colSpec)
		{
		}

		protected override string BeSpec => "PosFilter";

		internal override bool CompatibleFilter(XElement colSpec)
		{
			if (!base.CompatibleFilter(colSpec))
			{
				return false;
			}
#if RANDYTODO
			// TODO: Is this still needed now?
#endif
			var typeForLoaderNode = XmlUtils.GetMandatoryAttributeValue(colSpec, "class").Split('.').Last().Trim();
			// Naturally we are compatible with ourself, and BulkPosEditor has a FilterType which causes
			// a filter of this type to be created, too.
			return typeForLoaderNode == "BulkPosEditor" || typeForLoaderNode == "PosFilter";
		}

		/// <summary>
		/// Return the HVO of the list from which choices can be made.
		/// </summary>
		internal static int List(LcmCache cache)
		{
			return cache.LanguageProject.PartsOfSpeechOA.Hvo;
		}

		/// <summary>
		/// This is a filter for an atomic property, and the "all" and "only" options should not be presented.
		/// </summary>
		/// <remarks>NB: Used by reflection.</remarks>
		internal static bool Atomic => true;
	}
}