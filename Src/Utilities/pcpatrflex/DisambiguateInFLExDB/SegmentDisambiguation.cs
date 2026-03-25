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
		String tempFileName = "";

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
					using (StreamWriter file = new StreamWriter(tempFileName, true))
					{
						file.WriteLine(
							"DisambiguatedMorphBundles count=" + DisambiguatedMorphBundles.Count
						);
						int ig = 0;
						foreach (Guid g in DisambiguatedMorphBundles)
						{
							file.WriteLine("\t i=" + ig + " guid=\"" + g + "\"");
						}
						file.WriteLine("Segment.Analyses count=" + Segment.AnalysesRS.Count);
						file.WriteLine("WfiWordForm     ID=" + WfiWordformTags.kClassId);
						file.WriteLine("WfiGloss        ID=" + WfiGlossTags.kClassId);
						file.WriteLine("WfiAnalysis     ID=" + WfiAnalysisTags.kClassId);
						file.WriteLine("PunctuationForm ID=" + PunctuationFormTags.kClassId);
					}
					int guidsIndex = 0;
					int analysesIndex = 0;
					foreach (IAnalysis analysis in Segment.AnalysesRS)
					{
						using (StreamWriter file = new StreamWriter(tempFileName, true))
						{
							file.WriteLine("\tAnalysis guid=\"" + analysis.Guid + "\"");
							file.WriteLine("\tClass id=\"" + analysis.ClassID + "\"");
						}
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
							using (StreamWriter file = new StreamWriter(tempFileName, true))
							{
								file.WriteLine(
									"\t\twfiMorphBundleGuidToUse=\""
										+ wfiMorphBundleGuidToUse
										+ "\""
								);
								String s =
									(wfiMorphBundle == null)
										? "null"
										: wfiMorphBundle.Guid.ToString();
								file.WriteLine("\t\twfiMorphBundle=\"" + s + "\"");
								s = (bundle == null) ? "null" : bundle.Guid.ToString();
								file.WriteLine("\t\tbundle=" + s + "\"");
							}
							EnsureMorphBundleHasSense(bundle);
							if (wfiMorphBundle.Owner is IWfiAnalysis wfiAnalysisToUse)
							{
								using (StreamWriter file = new StreamWriter(tempFileName, true))
								{
									file.WriteLine(
										"wfiAnalysis guid=\"" + wfiAnalysisToUse.Guid + "\""
									);
								}
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
				using (StreamWriter file = new StreamWriter(tempFileName, true))
				{
					file.WriteLine("\tEMBHS: bundle guid=\"" + bundle.Guid + "\"");
					var sense = bundle.SenseRA;
					if (sense == null)
					{ // bundle does not have a sense (due to coming from parser)
					  // find its sense and set it in bundle
						file.WriteLine("\t\tbundle missing sense");
						var msa = bundle.MsaRA;
						if (msa != null)
						{
							file.WriteLine("\t\tmsa guid=\"" + msa.Guid + "\"");
							var entry = msa.Owner as ILexEntry;
							if (entry != null)
							{
								file.WriteLine("\t\tentry guid=\"" + entry.Guid + "\"");
								sense = entry.SenseWithMsa(msa);
								if (sense != null)
								{
									file.WriteLine("\t\tsense guid=\"" + sense.Guid + "\"");
									bundle.SenseRA = sense;
								}
							}
						}
					}
					else
					{
						file.WriteLine("\t\tsense guid=\"" + sense.Guid + "\"");
					}
				}
			}
			else
			{
				using (StreamWriter file = new StreamWriter(tempFileName, true))
				{
					file.WriteLine("\tEMBHS: bundle is null");
				}
			}
		}
	}
}
