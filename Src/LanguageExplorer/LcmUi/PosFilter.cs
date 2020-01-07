// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// A special filter, where items are LexSenses, and matches are ones where an MSA is an MoStemMsa that
	/// has the correct POS.
	/// </summary>
	internal class PosFilter : ColumnSpecFilter
	{
		/// <summary>
		/// Default constructor for persistence.
		/// </summary>
		public PosFilter() { }

		internal PosFilter(LcmCache cache, ListMatchOptions mode, int[] targets, XElement colSpec)
			: base(cache, mode, targets, colSpec)
		{
		}

		protected override string BeSpec => "external";

		public override bool CompatibleFilter(XElement colSpec)
		{
			if (!base.CompatibleFilter(colSpec))
			{
				return false;
			}
			var typeForLoaderNode = DynamicLoader.TypeForLoaderNode(colSpec);
			// Naturally we are compatible with ourself, and BulkPosEditor has a FilterType which causes
			// a filter of this type to be created, too.
			return typeForLoaderNode == typeof(BulkPosEditor) || typeForLoaderNode == typeof(PosFilter);
		}

		/// <summary>
		/// Return the HVO of the list from which choices can be made.
		/// </summary>
		public static int List(LcmCache cache)
		{
			return cache.LanguageProject.PartsOfSpeechOA.Hvo;
		}

		/// <summary>
		/// This is a filter for an atomic property, and the "all" and "only" options should not be presented.
		/// </summary>
		public static bool Atomic => true;
	}
}