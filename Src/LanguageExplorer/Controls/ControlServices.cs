// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SilEncConverters40;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Random static methods for the LanguageExplorer.Controls namespace.
	/// If at some later time a better organizing principle can be found,
	/// then this class can be removed and the methods moved elsewhere.
	/// </summary>
	internal static class ControlServices
	{
		/// <summary>
		/// Traverse a tree of PartOfSpeech objects.
		///	Put the appropriate descendant identifiers into collector.
		/// </summary>
		/// <param name="cache">data access to retrieve info</param>
		/// <param name="itemFlid">want children where this is non-empty in the collector</param>
		/// <param name="flidName">multi unicode prop to get name of item from</param>
		/// <param name="wsName">multi unicode writing system to get name of item from</param>
		/// <param name="collector">Add for each item an HvoTreeNode with the name and id of the item.</param>
		internal static void GatherPartsOfSpeech(LcmCache cache, int itemFlid, int flidName, int wsName, List<HvoTreeNode> collector)
		{
			var mainPartsOfSpeechList = cache.LanguageProject.PartsOfSpeechOA;
			var allPartsOfSpeech = mainPartsOfSpeechList.AllPossibilities();
			foreach (var possibility in allPartsOfSpeech)
			{
				var pos = possibility as IPartOfSpeech;
				if (pos == null)
				{
					// Can't ever happen.
					continue;
				}
				var canMakeNode = false;
				switch (itemFlid)
				{
					case PartOfSpeechTags.kflidInflectionClasses:
						canMakeNode = pos.InflectionClassesOC.Any();
						break;
					case PartOfSpeechTags.kflidInflectableFeats:
						canMakeNode = pos.InflectableFeatsRC.Any();
						break;
				}
				if (canMakeNode)
				{
					IMultiUnicode multiUnicode = null;
					switch (flidName)
					{
						case CmPossibilityTags.kflidName:
							multiUnicode = pos.Name;
							break;
						case CmPossibilityTags.kflidAbbreviation:
							multiUnicode = pos.Abbreviation;
							break;
					}
					if (multiUnicode != null)
					{
						collector.Add(new HvoTreeNode(multiUnicode.GetAlternativeOrBestTss(wsName, out _), pos.Hvo));
					}
				}
			}
		}

		internal static void EnsureWindows1252ConverterExists()
		{
			var encConv = new EncConverters();
			var de = encConv.GetEnumerator();
			// REVIEW: SHOULD THIS NAME BE LOCALIZED?
			const string sEncConvName = "Windows1252<>Unicode";
			var fMustCreateEncCnv = true;
			while (de.MoveNext())
			{
				if ((string)de.Key != null && (string)de.Key == sEncConvName)
				{
					fMustCreateEncCnv = false;
					break;
				}
			}
			if (fMustCreateEncCnv)
			{
				try
				{
					encConv.AddConversionMap(sEncConvName, "1252", ECInterfaces.ConvType.Legacy_to_from_Unicode, "cp", "", "", ECInterfaces.ProcessTypeFlags.CodePageConversion);
				}
				catch (ECException exception)
				{
					MessageBox.Show(exception.Message, LanguageExplorerControls.ksConvMapError, MessageBoxButtons.OK);
				}
			}
		}

		/// <summary>
		/// Gets a list of TextTags that reference the analysis occurrences in the input parameter.
		/// Will need enhancement to work with multi-segment tags.
		/// </summary>
		internal static ISet<ITextTag> GetTaggingReferencingTheseWords(List<AnalysisOccurrence> occurrences)
		{
			var results = new HashSet<ITextTag>();
			if (occurrences.Count == 0 || !occurrences[0].IsValid)
			{
				return results;
			}
			if (!(occurrences[0].Segment.Paragraph.Owner is IStText text))
			{
				throw new NullReferenceException("Unexpected error!");
			}
			var tags = text.TagsOC;
			if (!tags.Any())
			{
				return results;
			}
			var occurenceSet = new HashSet<AnalysisOccurrence>(occurrences); ;
			// Collect all segments referenced by these words
			var segsUsed = new HashSet<ISegment>(occurenceSet.Select(o => o.Segment));
			// Collect all tags referencing those segments
			// Enhance: This won't work for multi-segment tags where a tag can reference 3+ segments.
			// but see note on foreach below.
			var tagsRefSegs = new HashSet<ITextTag>(tags.Where(ttag => segsUsed.Contains(ttag.BeginSegmentRA) || segsUsed.Contains(ttag.EndSegmentRA)));
			foreach (var ttag in tagsRefSegs) // A slower, but more complete form can replace tagsRefSegs with tags here.
			{
				if (occurenceSet.Intersect(ttag.GetOccurrences()).Any())
				{
					results.Add(ttag);
				}
			}
			return results;
		}
	}
}
