// Copyright (c) 2018-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.DisambiguateInFLExDB
{
	public class SegmentDisambiguation
	{
		string tempFileName = "";

		public SegmentDisambiguation(ISegment segment, List<Guid> disambiguatedMorphBundleGuids)
		{
			Segment = segment;
			DisambiguatedMorphBundles = disambiguatedMorphBundleGuids;
			tempFileName = Path.Combine(Path.GetTempPath(), "PcPatrFLExDebug.txt");
			if (File.Exists(tempFileName))
				File.Delete(tempFileName);
		}

		public ISegment Segment { get; set; }
		public List<Guid> DisambiguatedMorphBundles { get; set; }

		public void Disambiguate(LcmCache cache)
		{
			NonUndoableUnitOfWorkHelper.Do(
				cache.ActionHandlerAccessor,
				() =>
				{
					int guidsIndex = 0;
					int analysesIndex = 0;
					foreach (IAnalysis analysis in Segment.AnalysesRS)
					{
						// The Analyses can be:
						//	  a WfiWordForm when it is unanalyzed,
						//    a WfiAnalysis when it is partially analyzed, and
						//    a WfiGloss when it is fully analyzed.
						//    a PunctuationForm.
						// Partial means the user/parser has gotten it down to one analysis, but no word gloss has been chosen.
						// Full means the user/parser has gotten it down to one analysis and has chosen a word gloss.
						if (
							(
								analysis.ClassID == WfiWordformTags.kClassId
								|| analysis.ClassID == WfiGlossTags.kClassId
								|| analysis.ClassID == WfiAnalysisTags.kClassId
							)
							&& guidsIndex < DisambiguatedMorphBundles.Count
						)
						{
							var wfiMorphBundleGuidToUse = DisambiguatedMorphBundles.ElementAt(
								guidsIndex
							);
							var wfiMorphBundle = cache.ServiceLocator.ObjectRepository.GetObject(
								wfiMorphBundleGuidToUse
							);
							var bundle = wfiMorphBundle as IWfiMorphBundle;
							EnsureMorphBundleHasSense(bundle);
							if (wfiMorphBundle.Owner is IWfiAnalysis wfiAnalysisToUse)
							{
								wfiAnalysisToUse.SetAgentOpinion(
									cache.LanguageProject.DefaultUserAgent,
									Opinions.approves
								);
								if (wfiAnalysisToUse.MeaningsOC.Count == 1)
								{
									Segment.AnalysesRS[analysesIndex] =
										wfiAnalysisToUse.MeaningsOC.ElementAt(0);
								}
								else
								{
									Segment.AnalysesRS[analysesIndex] = wfiAnalysisToUse;
								}
								foreach (IWfiMorphBundle b in wfiAnalysisToUse.MorphBundlesOS)
								{
									if (b != bundle)
									{
										// make sure all bundles in the analysis have a sense
										EnsureMorphBundleHasSense(b);
									}
								}
							}
							guidsIndex++;
						}
						analysesIndex++;
					}
				}
			);
		}

		public void EnsureMorphBundleHasSense(IWfiMorphBundle bundle)
		{
			if (bundle != null)
			{
				var sense = bundle.SenseRA;
				if (sense == null)
				{ // bundle does not have a sense (due to coming from parser)
				  // find its sense and set it in bundle
					var msa = bundle.MsaRA;
					if (msa != null)
					{
						var entry = msa.Owner as ILexEntry;
						if (entry != null)
						{
							sense = entry.SenseWithMsa(msa);
							if (sense != null)
							{
								bundle.SenseRA = sense;
							}
						}
					}
				}
			}
		}
	}
}
