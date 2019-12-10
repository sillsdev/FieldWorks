// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal static class InterlinearTextServices
	{
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
			var text = occurrences[0].Segment.Paragraph.Owner as IStText;
			if (text == null)
			{
				throw new NullReferenceException("Unexpected error!");
			}
			var tags = text.TagsOC;
			if (tags.Count == 0)
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