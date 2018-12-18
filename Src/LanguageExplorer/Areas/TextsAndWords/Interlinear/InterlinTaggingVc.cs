// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Modifications of InterlinVc for showing TextTag possibilities.
	/// </summary>
	internal class InterlinTaggingVc : InterlinVc
	{
		private int m_lenEndTag;
		private int m_lenStartTag;
		private bool m_fAnalRtl;
		private ITsString m_emptyAnalysisStr;
		private ITextTagRepository m_tagRepo;
		private Dictionary<Tuple<ISegment, int>, ITsString[]> m_tagStrings; // Cache tag strings by ISegment and index

		/// <summary />
		public InterlinTaggingVc(LcmCache cache)
			: base(cache)
		{
			m_cache = cache;
			m_lenEndTag = ITextStrings.ksEndTagSymbol.Length;
			m_lenStartTag = ITextStrings.ksStartTagSymbol.Length;
			SetAnalysisRightToLeft();
			m_emptyAnalysisStr = TsStringUtils.EmptyString(m_cache.DefaultAnalWs);
			m_tagRepo = m_cache.ServiceLocator.GetInstance<ITextTagRepository>();
			m_tagStrings = new Dictionary<Tuple<ISegment, int>, ITsString[]>();
		}

		private static Tuple<ISegment, int> GetDictKey(AnalysisOccurrence point)
		{
			return new Tuple<ISegment, int>(point.Segment, point.Index);
		}

		private void SetAnalysisRightToLeft()
		{
			var wsAnal = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			if (wsAnal != null)
			{
				m_fAnalRtl = wsAnal.RightToLeftScript;
			}
		}

		/// <summary>
		/// Given a segment (for which we should have just loaded the wordforms), load any associated text tagging data
		/// </summary>
		private void LoadDataForTextTags(int hvoSeg)
		{
			// Get a 'real' Segment
			ISegment curSeg;
			try
			{
				curSeg = m_segRepository.GetObject(hvoSeg);
				if (curSeg.AnalysesRS == null || curSeg.AnalysesRS.Count == 0)
				{
					return; // small sanity check
				}
			}
			catch (KeyNotFoundException)
			{
				return; // Hmm... this could be a problem, but we'll just skip it for now.
			}
			// Get all AnalysisOccurrences in this Segment
			var segWords = curSeg.AnalysesRS.Select((t, i) => new AnalysisOccurrence(curSeg, i)).ToList();
			// Find all the tags for this Segment's AnalysisOccurrences and cache them
			var textTagList = InterlinTaggingChild.GetTaggingReferencingTheseWords(segWords);
			var occurrencesTagged = new HashSet<AnalysisOccurrence>();
			foreach (var tag in textTagList)
			{
				occurrencesTagged.UnionWith(tag.GetOccurrences());
				CacheTagString(tag);
			}
			// now go through the list of occurrences that didn't have tags cached, and make sure they have empty strings cached
			var occurrencesWithoutTags = new HashSet<AnalysisOccurrence>(occurrencesTagged);
			occurrencesWithoutTags.SymmetricExceptWith(segWords);
			CacheNullTagString(occurrencesWithoutTags);
		}

		/// <summary>
		/// Caches a possibility label for each analysis occurrence that a tag applies to.
		/// </summary>
		internal void CacheTagString(ITextTag ttag)
		{
			var occurrences = ttag.GetOccurrences();
			var cwordArray = occurrences.Count;
			if (cwordArray == 0)
			{
				return; // No words tagged! Again... shouldn't happen. :)
			}
			var tagPossibility = ttag.TagRA;
			var label = tagPossibility == null ? m_emptyAnalysisStr : tagPossibility.Abbreviation.BestAnalysisAlternative;
			// use 'for' loop because we need to know when we're at the beginning
			// and end of the loop
			for (var i = 0; i < cwordArray; i++)
			{
				// TODO: Someday when we handle more than one layer of tagging, this may change!
				var current = occurrences[i];
				if (current == null || !current.IsValid)
				{
					continue; // Shouldn't happen...
				}
				var strBldr = label.GetBldr();
				if (i == 0) // First occurrence for this tag.
				{
					StartTagSetup(strBldr);
				}
				else
				{
					// Until someone has a better idea, only show the label on the first occurrence.
					// but I have a feeling the label will be shown on the left-most occurrence
					// whether it's a RTL or LTR language!
					label = m_emptyAnalysisStr;
					strBldr = label.GetBldr();
				}
				if (i == cwordArray - 1) // Last occurrence for this tag.
				{
					EndTagSetup(strBldr);
				}
				var key = GetDictKey(current);
				var markupTags = (ICmPossibilityList)tagPossibility.Owner.Owner;
				var possibilityCount = markupTags.PossibilitiesOS.Count;
				var row = tagPossibility.Owner.IndexInOwner;
				ITsString[] myList;
				if (m_tagStrings.ContainsKey(key))
				{
					var currentLength = m_tagStrings[key].Length;
					if (currentLength < possibilityCount)
					{
						myList = new ITsString[row >= currentLength ? row + 1 : currentLength];
						for (int j = 0; j < currentLength; j++)
						{
							myList[j] = m_tagStrings[key][j];
						}
					}
					else
					{
						myList = m_tagStrings[key];
					}
					m_tagStrings.Remove(key);
				}
				else
				{
					myList = new ITsString[row + 1];
				}
				myList[row] = strBldr.GetString();
				m_tagStrings.Add(key, myList);
			}
		}

		/// <summary>
		/// Sets up the end of the tag with its (localizable) symbol.
		/// </summary>
		private void EndTagSetup(ITsStrBldr builder)
		{
			// How this works depends on the directionality of both the vernacular and
			// analysis writing systems.  This does assume that nobody localizes [ and ]
			// to something like ] and [!  I'm not sure those strings should be localizable.
			// See LT-9551.
			if (RightToLeft)
			{
				if (m_fAnalRtl)
				{
					builder.Replace(builder.Length, builder.Length, ITextStrings.ksStartTagSymbol, null);
				}
				else
				{
					builder.Replace(0, 0, ITextStrings.ksStartTagSymbol, null);
				}
			}
			else
			{
				if (m_fAnalRtl)
				{
					builder.Replace(0, 0, ITextStrings.ksEndTagSymbol, null);
				}
				else
				{
					builder.Replace(builder.Length, builder.Length, ITextStrings.ksEndTagSymbol, null);
				}
			}
		}

		/// <summary>
		/// Sets up the beginning of the tag with its (localizable) symbol.
		/// </summary>
		private void StartTagSetup(ITsStrBldr builder)
		{
			// How this works depends on the directionality of both the vernacular and
			// analysis writing systems.  This does assume that nobody localizes [ and ]
			// to something like ] and [!  I'm not sure those strings should be localizable.
			// See LT-9551.
			if (RightToLeft)
			{
				if (m_fAnalRtl)
				{
					builder.Replace(0, 0, ITextStrings.ksEndTagSymbol, null);
				}
				else
				{
					builder.Replace(builder.Length, builder.Length, ITextStrings.ksEndTagSymbol, null);
				}
			}
			else
			{
				if (m_fAnalRtl)
				{
					builder.Replace(builder.Length, builder.Length, ITextStrings.ksStartTagSymbol, null);
				}
				else
				{
					builder.Replace(0, 0, ITextStrings.ksStartTagSymbol, null);
				}
			}
		}

		/// <summary>
		/// Caches a possibility label for each wordform that a tag appliesTo.
		/// This version starts from the hvo.
		/// </summary>
		internal void CacheTagString(int hvoTag)
		{
			CacheTagString(m_tagRepo.GetObject(hvoTag));
		}

		/// <summary>
		/// Caches an empty label for each occurrence that a tag appliesTo in preparation for deletion.
		/// </summary>
		internal void CacheNullTagString(IEnumerable<AnalysisOccurrence> occurrencesToApply)
		{
			if (occurrencesToApply == null)
			{
				return;
			}
			foreach (var occurrence in occurrencesToApply)
			{
				var key = GetDictKey(occurrence);
				if (m_tagStrings.ContainsKey(key))
				{
					m_tagStrings.Remove(key);
				}
				var tagList = new ITsString[0];
				m_tagStrings.Add(key, tagList);
			}
		}

		/// <summary>
		/// Clears a specific row's Tag String Data for the given occurrences
		/// </summary>
		internal void ClearTagStringForRow(IEnumerable<AnalysisOccurrence> occurrencesToApply, int row)
		{
			if (occurrencesToApply == null)
			{
				return;
			}
			foreach (var occurrence in occurrencesToApply)
			{
				var key = GetDictKey(occurrence);
				m_tagStrings[key][row] = m_emptyAnalysisStr;
				if (m_tagStrings[key].Length == row + 1)
				{
					var tagList = new ITsString[row];
					for (var i = 0; i < tagList.Length; i++)
					{
						tagList[i] = m_tagStrings[key][i];
					}
					m_tagStrings[key] = tagList;
				}
			}
		}

		internal override void LoadDataForSegments(int[] rghvo, int hvoPara)
		{
			base.LoadDataForSegments(rghvo, hvoPara);
			foreach (int hvo in rghvo)
			{
				LoadDataForTextTags(hvo);
			}
		}

		/// <summary>
		/// This is for an IAnalysis object at a particular index of a Segment.
		/// </summary>
		internal override void AddExtraBundleRows(IVwEnv vwenv, AnalysisOccurrence analysis)
		{
			ITsString[] tss;
			var key = GetDictKey(analysis);
			if (!m_tagStrings.TryGetValue(key, out tss))
			{
				return;
			}
			var stText = analysis.Segment.Owner.Owner;
			// If either the Segment's analyses sequence or the tags on the text change, we want to redraw this
			vwenv.NoteDependency(new[] { analysis.Segment.Hvo, stText.Hvo }, new[] { SegmentTags.kflidAnalyses, StTextTags.kflidTags }, 2);
			for (var i = 0; i < tss.Length; i++)
			{
				if (tss[i] == null)
				{
					tss[i] = m_emptyAnalysisStr;
				}
				SetTrailingAlignmentIfNeeded(vwenv, tss[i]);
				vwenv.AddString(tss[i]);
			}
		}

		private void SetTrailingAlignmentIfNeeded(IVwEnv vwenv, ITsString tss)
		{
			Debug.Assert(tss != null, "Should get something for a TsString here!");
			var tssText = tss.Text;
			if (string.IsNullOrEmpty(tssText))
			{
				return;
			}
			if (TagEndsWithThisWordform(tssText) && !TagStartsWithThisWordform(tssText))
			{
				// set trailing alignment
				vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalTrailing);
			}
		}

		private bool TagStartsWithThisWordform(string ttagLabel)
		{
			if (ttagLabel.Length >= m_lenStartTag)
			{
				return ttagLabel.Substring(0, m_lenStartTag) == ITextStrings.ksStartTagSymbol;
			}
			return false;
		}

		private bool TagEndsWithThisWordform(string ttagLabel)
		{
			var clen = ttagLabel.Length;
			if (clen >= m_lenEndTag)
			{
				return ttagLabel.Substring(clen - m_lenEndTag, m_lenEndTag) == ITextStrings.ksEndTagSymbol;
			}
			return false;
		}
	}
}