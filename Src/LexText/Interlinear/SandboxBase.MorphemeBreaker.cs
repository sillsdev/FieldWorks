// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.IText
{
	partial class SandboxBase
	{
		/// <summary>
		/// Analyze the string. We're looking for something like un- except -ion -al -ly.
		/// The one with spaces on both sides is a root, the others are prefixes or suffixes.
		/// Todo WW(JohnT): enhance to handle other morpheme break characters.
		/// Todo WW (JohnT): enhance to look for trailing ;POS and handle appropriately.
		/// </summary>
		public class MorphemeBreaker
		{
			string m_input; // string being processed into morphemes.
			ISilDataAccess m_sda; // cache to update with new objects etc (m_caches.DataAccess).
			IVwCacheDa m_cda; // another interface on same cache.
			CachePair m_caches; // Both the caches we are working with.
			int m_hvoSbWord; // HVO of the Sandbox word that will own the new morphs
			int m_cOldMorphs;
			int m_cNewMorphs;
			ITsStrFactory m_tsf = TsStrFactoryClass.Create();
			int m_wsVern = 0;
			IMoMorphTypeRepository m_types;
			int m_imorph = 0;
			SandboxBase m_sandbox;

			// These variables are used to re-establish a selection in the morpheme break line
			// after rebuilding the morphemes.
			int m_ichSelInput; // The character position in m_input where we want the selection.
			int m_tagSel = -1; // The property we want the selection in (or -1 for none).
			int m_ihvoSelMorph; // The index of the morpheme we want the selection in.
			int m_ichSelOutput; // The character offset where we want the selection to be made.
			int m_cchPrevMorphemes; // Total length of morphemes before m_imorph.

			public MorphemeBreaker(CachePair caches, string input, int hvoSbWord, int wsVern,
				SandboxBase sandbox)
			{
				m_caches = caches;
				m_sda = caches.DataAccess;
				m_cda = (IVwCacheDa)m_sda;
				m_input = input;
				m_hvoSbWord = hvoSbWord;
				m_cOldMorphs = m_sda.get_VecSize(m_hvoSbWord, ktagSbWordMorphs);
				ITsStrFactory m_tsf = TsStrFactoryClass.Create();
				m_wsVern = wsVern;
				m_types = m_caches.MainCache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
				m_sandbox = sandbox;
			}

			/// <summary>
			/// Should be called with a non-empty sequence of characters from m_ichStartMorpheme
			/// to m_ich as a morpheme.
			/// </summary>
			/// <param name="stMorph">the morph form to store in the sandbox</param>
			/// <param name="ccTrailing">number of trailing spaces for the morph</param>
			/// <param name="fMonoMorphemic">flag whether we're processing a monomorphemic word</param>
			void HandleMorpheme(string stMorph, int ccTrailing, bool fMonoMorphemic)
			{
				int clsidForm; // The subclass of MoMorph to create if we need a new object.
				string realForm = stMorph; // Gets stripped of morpheme-type characters.
				IMoMorphType mmt;
				try
				{
					mmt = MorphServices.FindMorphType(m_caches.MainCache, ref realForm, out clsidForm);
				}
				catch (Exception e)
				{
					MessageBox.Show(null, e.Message, ITextStrings.ksWarning, MessageBoxButtons.OK);
					mmt = m_types.GetObject(MoMorphTypeTags.kguidMorphStem);
					//clsidForm = MoStemAllomorphTags.kClassId; // clsidForm isn't used anywhere
				}
				int hvoSbForm = 0; // hvo of the SbNamedObj that is the form of the morph.
				int hvoSbMorph = 0;
				bool fCanReuseOldMorphData = false;
				int maxSkip = m_cOldMorphs - m_cNewMorphs;
				if (m_imorph < m_cOldMorphs)
				{
					// If there's existing analysis and any morphs match, keep the analysis of
					// the existing morph. It's probably the best guess.
					string sSbForm = GetExistingMorphForm(out hvoSbMorph, out hvoSbForm, m_imorph);
					if (sSbForm != realForm && maxSkip > 0)
					{
						// If we're deleting morph breaks, we may need to skip over a morph to
						// find the matching existing morph.
						int hvoSbFormT = 0;
						int hvoSbMorphT = 0;
						string sSbFormT = null;
						List<int> skippedMorphs = new List<int>();
						skippedMorphs.Add(hvoSbMorph);
						for (int skip = 1; skip <= maxSkip; ++skip)
						{
							sSbFormT = GetExistingMorphForm(out hvoSbMorphT, out hvoSbFormT, m_imorph + skip);
							if (sSbFormT == realForm)
							{
								hvoSbForm = hvoSbFormT;
								hvoSbMorph = hvoSbMorphT;
								sSbForm = sSbFormT;
								foreach (int hvo in skippedMorphs)
									m_sda.DeleteObjOwner(m_hvoSbWord, hvo, ktagSbWordMorphs, m_imorph);
								m_cOldMorphs -= skippedMorphs.Count;
								break;
							}
							skippedMorphs.Add(hvoSbMorphT);
						}
					}
					if (sSbForm != realForm)
					{
						// Clear out the old analysis. Can't be relevant to a different form.
						m_cda.CacheObjProp(hvoSbMorph, ktagSbMorphEntry, 0);
						m_cda.CacheObjProp(hvoSbMorph, ktagSbMorphGloss, 0);
						m_cda.CacheObjProp(hvoSbMorph, ktagSbMorphPos, 0);
					}
					else
					{
						fCanReuseOldMorphData = m_sda.get_StringProp(hvoSbMorph, ktagSbMorphPrefix).Text == mmt.Prefix
						&& m_sda.get_StringProp(hvoSbMorph, ktagSbMorphPostfix).Text == mmt.Postfix;
						//&& m_sda.get_IntProp(hvoSbMorph, ktagSbMorphClsid) == clsidForm
						//&& m_sda.get_IntProp(hvoSbMorph, ktagSbMorphRealType) == mmt.Hvo;
					}
				}
				else
				{
					// Make a new morph, and an SbNamedObj to go with it.
					hvoSbMorph = m_sda.MakeNewObject(kclsidSbMorph, m_hvoSbWord,
						ktagSbWordMorphs, m_imorph);
					hvoSbForm = m_sda.MakeNewObject(kclsidSbNamedObj, hvoSbMorph,
						ktagSbMorphForm, -2); // -2 for atomic
				}
				if (!fCanReuseOldMorphData)
				{
					// This might be redundant, but it isn't expensive.
					m_cda.CacheStringAlt(hvoSbForm, ktagSbNamedObjName, m_wsVern,
						m_tsf.MakeString(realForm, m_wsVern));
					m_cda.CacheStringProp(hvoSbMorph, ktagSbMorphPrefix,
						m_tsf.MakeString(mmt.Prefix, m_wsVern));
					m_cda.CacheStringProp(hvoSbMorph, ktagSbMorphPostfix,
						m_tsf.MakeString(mmt.Postfix, m_wsVern));
					//m_cda.CacheIntProp(hvoSbMorph, ktagSbMorphClsid, clsidForm);
					//m_cda.CacheIntProp(hvoSbMorph, ktagSbMorphRealType, mmt.Hvo);
					// Fill in defaults.
					m_sandbox.EstablishDefaultEntry(hvoSbMorph, realForm, mmt, fMonoMorphemic);
				}
				// the morpheme is not a guess.
				m_cda.CacheIntProp(hvoSbForm, ktagSbNamedObjGuess, 0);
				// Figure whether selection is in this morpheme.
				int ichSelMorph = m_ichSelInput - m_cchPrevMorphemes;

				int cchPrefix = (mmt.Prefix == null) ? 0 : mmt.Prefix.Length;
				int cchPostfix = (mmt.Postfix == null) ? 0 : mmt.Postfix.Length;
				int cchMorph = realForm.Length;
				// If this is < 0, we must be in a later morpheme and should have already
				// established m_ichSelOutput.
				if (ichSelMorph >= 0 && ichSelMorph <= cchPrefix + cchPostfix + cchMorph)
				{
					m_ihvoSelMorph = m_imorph;
					m_ichSelOutput = ichSelMorph - cchPrefix;
					m_tagSel = ktagSbNamedObjName;
					if (m_ichSelOutput < 0)
					{
						// in the prefix
						m_tagSel = ktagSbMorphPrefix;
						m_ichSelOutput = ichSelMorph;
					}
					else if (m_ichSelOutput > cchMorph)
					{
						if (cchPostfix > 0)
						{
							// in the postfix
							m_ichSelOutput = cchPostfix;
							m_tagSel = ktagSbMorphPostfix;
						}
						else
						{
							m_ichSelOutput = cchMorph;
						}
					}
				}
				m_cchPrevMorphemes += cchPrefix + cchPostfix + cchMorph + ccTrailing;
				m_imorph++;
			}

			private string GetExistingMorphForm(out int hvoSbMorph, out int hvoSbForm, int imorph)
			{
				hvoSbMorph = m_sda.get_VecItem(m_hvoSbWord, ktagSbWordMorphs, imorph);
				hvoSbForm = m_sda.get_ObjectProp(hvoSbMorph, ktagSbMorphForm);
				Debug.Assert(hvoSbForm != 0); // We always have one of these for each form.
				return m_sda.get_MultiStringAlt(hvoSbForm, ktagSbNamedObjName, m_wsVern).Text;
			}

			/// <summary>
			/// Handle basic work on finding the morpheme breaks.
			/// </summary>
			/// <param name="input"></param>
			/// <param name="breakMarkers"></param>
			/// <param name="prefixMarkers"></param>
			/// <param name="postfixMarkers"></param>
			/// <returns>A string suitable for followup processing by client.</returns>
			public static string DoBasicFinding(string input, string[] breakMarkers, string[] prefixMarkers, string[] postfixMarkers)
			{
				string fullForm = input;
				int iMatched = -1;
				// the morphBreakSpace should be the last item.
				string morphBreakSpace = breakMarkers[breakMarkers.Length - 1];
				Debug.Assert(morphBreakSpace == " " || morphBreakSpace == "  ",
					"expected a morphbreak space at last index");

				// First, find the segment boundaries.
				List<int> vichMin = new List<int>();
				List<int> vichLim = new List<int>();
				int ccchSeg = 0;
				vichMin.Add(0);
				for (int ichStart = 0; ichStart < fullForm.Length; )
				{
					int ichBrk = MiscUtils.IndexOfAnyString(fullForm, breakMarkers,
						ichStart, out iMatched);
					if (ichBrk < 0)
						break;
					vichLim.Add(ichBrk);
					// Skip over consecutive markers
					for (ichBrk += breakMarkers[iMatched].Length;
						ichBrk < fullForm.Length;
						ichBrk += breakMarkers[iMatched].Length)
					{
						if (MiscUtils.IndexOfAnyString(fullForm, breakMarkers, ichBrk, out iMatched) != ichBrk)
							break;
					}
					vichMin.Add(ichBrk);
					ichStart = ichBrk;
				}
				vichLim.Add(fullForm.Length);
				Debug.Assert(vichMin.Count == vichLim.Count);
				int ieRoot = 0;
				int cchRoot = 0;
				int cLongest = 0;
				for (int i = 0; i < vichMin.Count; ++i)
				{
					int ichMin = vichMin[i];
					int ichLim = vichLim[i];
					int cchSeg = ichLim - ichMin;
					ccchSeg++;
					if (cchRoot < cchSeg)
					{
						cchRoot = cchSeg;
						ieRoot = i;
						cLongest = 1;
					}
					else if (cchRoot == cchSeg)
					{
						++cLongest;
					}
				}
				if (cLongest == ccchSeg && cLongest > 2)
				{
					// All equal length, 3 or more segments.
					ieRoot = 1;		// Pure speculation at this point, based on lengths.
				}
				// Look for a root that's delimited by spaces fore and aft.
				for (int i = 0; i < vichMin.Count; ++i)
				{
					int ichMin = vichMin[i];
					int ichLim = vichLim[i];
					if ((ichMin == 0 || fullForm[ichMin - 1] == ' ') &&
						(ichLim == fullForm.Length || fullForm[ichLim] == ' '))
					{
						ieRoot = i;
						cchRoot = ichLim - ichMin;
						break;
					}
				}
				// Here it is: what we've been computing towards up to this point!
				int ichRootMin = vichMin[ieRoot];

				int iMatchedPrefix, iMatchedPostfix;
				// The code to insert spaces is problematic. After all, some words composed of compounded roots are hyphenated,
				// but we like to use hyphens to mark affixes. Automatically inserting a space in order to handle affixes prevents
				// hyphenated roots from being properly handled.
				for (bool fFixedProblem = true; fFixedProblem; )
				{
					fFixedProblem = false;
					for (int ichStart = 0; ; )
					{
						int indexPrefix = MiscUtils.IndexOfAnyString(fullForm, prefixMarkers, ichStart, out iMatchedPrefix);
						int indexPostfix = MiscUtils.IndexOfAnyString(fullForm, postfixMarkers, ichStart, out iMatchedPostfix);
						if (indexPrefix < 0 && indexPostfix < 0)
							break; // no (remaining) problems!!
						int index;
						int ichNext;
						int cchMarker;
						if (indexPostfix < 0)
						{
							index = indexPrefix;
							cchMarker = prefixMarkers[iMatchedPrefix].Length;
							ichNext = indexPrefix + cchMarker;
						}
						else if (indexPrefix < 0)
						{
							index = indexPostfix;
							cchMarker = postfixMarkers[iMatchedPostfix].Length;
							ichNext = indexPostfix + cchMarker;
						}
						else if (indexPostfix < indexPrefix)
						{
							index = indexPostfix;
							cchMarker = postfixMarkers[iMatchedPostfix].Length;
							ichNext = indexPostfix + cchMarker;
						}
						else
						{
							index = indexPrefix;
							cchMarker = prefixMarkers[iMatchedPrefix].Length;
							ichNext = indexPrefix + cchMarker;
						}
						int cchWordPreceding = 0;
						bool fFoundProblemPreceding = false;
						for (int ich = index - 1; ich >= ichStart; --ich, ++cchWordPreceding)
						{
							if (ich > index - morphBreakSpace.Length)
							{
								// we'll assume we found a problem if we found a space here
								// unless we match the morphBreakSpace next iteration.
								if (fullForm[ich] == ' ')
									fFoundProblemPreceding = true;
								continue;	// we can't match the substring (yet)
							}
							if (fullForm.Substring(ich, morphBreakSpace.Length) == morphBreakSpace)
							{
								// after having enough room to check the morphBreakSpace,
								// we found one, so we don't really have ccWordPreceding.
								if (ich + morphBreakSpace.Length == index)
								{
									cchWordPreceding = 0;
									fFoundProblemPreceding = false;
								}
								break;
							}
						}

						indexPrefix = MiscUtils.IndexOfAnyString(fullForm, prefixMarkers, ichNext, out iMatchedPrefix);
						indexPostfix = MiscUtils.IndexOfAnyString(fullForm, postfixMarkers, ichNext, out iMatchedPostfix);
						int index2;
						if (indexPrefix < 0 && indexPostfix < 0)
							index2 = fullForm.Length;
						else if (indexPrefix < 0)
							index2 = indexPostfix;
						else if (indexPostfix < 0)
							index2 = indexPrefix;
						else
							index2 = Math.Min(indexPrefix, indexPostfix);

						int cchWordFollowing = 0;
						for (int ich = ichNext; ich < index2; ++ich, ++cchWordFollowing)
						{
							if (ich + morphBreakSpace.Length > fullForm.Length)
								continue;	// we can't match the substring
							if (fullForm.Substring(ich, morphBreakSpace.Length) == morphBreakSpace)
								break;
						}
						ichStart = ichNext; // for next iteration of inner loop, if any
						if (cchWordFollowing > 0 && cchWordPreceding > 0 || fFoundProblemPreceding)
						{
							// We will fix a problem! Insert a space at index or index + cchMarker.
							fFixedProblem = true;
							string morphBreakSpaceAdjusted = morphBreakSpace;
							if (fFoundProblemPreceding)
							{
								// we found a preceding space, we need to just
								// add one more space before the affix.
								morphBreakSpaceAdjusted = " ";
							}
							else if (index < ichRootMin)
							{
								// if before the root, guess a prefix.  Otherwise, guess a suffix.
								index += cchMarker;	// adjust for the marker (can be > 1)
								ichRootMin += morphBreakSpace.Length;	// adjust for inserted space.
							}
							Debug.Assert(index < input.Length && fullForm.Length <= input.Length);
							fullForm = fullForm.Substring(0, index) + morphBreakSpaceAdjusted +
								fullForm.Substring(index);
							break; // from inner loop, continue outer.
						}
					}
				}

				return fullForm;
			}

			public static List<string> BreakIntoMorphs(string morphFormWithMarkers, string baseWord)
			{
				List<int> ichMinsOfNextMorph;
				return BreakIntoMorphs(morphFormWithMarkers, baseWord, out ichMinsOfNextMorph);
			}

			/// <summary>
			/// Split the string into morphs respecting existing spaces in base word
			/// </summary>
			/// <param name="morphFormWithMarkers"></param>
			/// <param name="baseWord"></param>
			/// <param name="ccTrailingMorphs">character count of number of trailing whitespaces for each morph</param>
			/// <returns>list of morphs for the given word</returns>
			private static List<string> BreakIntoMorphs(string morphFormWithMarkers, string baseWord, out List<int> ccTrailingMorphs)
			{
				ccTrailingMorphs = new List<int>();
				// if the morphForm break down matches the base word, just return this string.
				// the user hasn't done anything to change the morphbreaks.
				if (morphFormWithMarkers == baseWord)
				{
					ccTrailingMorphs.Add(0);
					return new List<string>(new string[] { morphFormWithMarkers });
				}
				// find any existing white spaces in the baseWord.
				List<string> morphs = new List<string>();
				// we're dealing with a phrase if there are spaces in the word.
				bool fBaseWordIsPhrase = SandboxBase.IsPhrase(baseWord);
				List<int> morphEndOffsets = IchLimOfMorphs(morphFormWithMarkers, fBaseWordIsPhrase);
				int prevEndOffset = 0;
				foreach (int morphEndOffset in morphEndOffsets)
				{
					string morph = morphFormWithMarkers.Substring(prevEndOffset, morphEndOffset - prevEndOffset);
					morph = morph.Trim();
					// figure the trailing characters following the previous morph by the difference betweeen
					// the current morphEndOffset the length of the trimmed morph and prevEndOffset
					if (prevEndOffset > 0)
						ccTrailingMorphs.Add(morphEndOffset - prevEndOffset - morph.Length);
					if (!String.IsNullOrEmpty(morph))
						morphs.Add(morph);
					prevEndOffset = morphEndOffset;
				}
				// add the count of the final trailing space characters
				ccTrailingMorphs.Add(morphFormWithMarkers.Length - morphEndOffsets[morphEndOffsets.Count - 1]);
				return morphs;
			}

			/// <summary>
			/// get the end offsets for potential morphs based upon whitespace delimiters
			/// </summary>
			/// <param name="sourceString"></param>
			/// <returns></returns>
			private static List<int> IchLimOfMorphs(string sourceString, bool fBaseWordIsPhrase)
			{
				List<int> whiteSpaceOffsets = WhiteSpaceOffsets(sourceString);
				List<int> morphEndOffsets = new List<int>(whiteSpaceOffsets);
				int prevOffset = -1;
				int cOffsets = whiteSpaceOffsets.Count;
				foreach (int offset in whiteSpaceOffsets)
				{
					// we always want to remove spaces following a previous space
					// or if we're in a a phrase, always remove the last offset, since
					// it cannot be followed by a second one.
					if (prevOffset != -1 && offset == prevOffset + 1 ||
						fBaseWordIsPhrase && offset == whiteSpaceOffsets[whiteSpaceOffsets.Count - 1])
					{
						morphEndOffsets.Remove(offset);
					}

					if (fBaseWordIsPhrase)
					{
						// for a phrase, we always want to remove previous offsets
						// that are not followed by a space offset
						if (prevOffset != -1 && prevOffset != offset - 1)
						{
							morphEndOffsets.Remove(prevOffset);
						}
					}
					prevOffset = offset;
				}
				// finally add the end of the sourcestring to the offsets.
				morphEndOffsets.Add(sourceString.Length);
				return morphEndOffsets;
			}

			private static List<int> WhiteSpaceOffsets(string sourceString)
			{
				List<int> whiteSpaceOffsets = new List<int>();
				int ichMatch = 0;
				do
				{
					ichMatch = sourceString.IndexOfAny(Unicode.SpaceChars, ichMatch);
					if (ichMatch != -1)
					{
						whiteSpaceOffsets.Add(ichMatch);
						ichMatch++;
					}
				} while (ichMatch != -1);
				return whiteSpaceOffsets;
			}


			/// <summary>
			/// Run the morpheme breaking algorithm.
			/// </summary>
			public void Run()
			{
				// If morpheme break characters occur in invalid positions, try to fix things.
				// This is most often due to the user inserting hyphens but neglecting to also
				// insert spaces, thus leading to ambiguity.  Sometimes inserting spaces and
				// sometimes not makes our life even more complicated, if not impossible.
				// The basic heuristic is this:
				// 1) find the root, which is defined as the longest contiguous stretch of
				//	characters not containing a break character or a space.  If all segments
				//	are the same length, the first of two segments is the root, or the second
				//	of more than two segments is the root.  However, if one or more roots are
				//	marked by surrounding spaces, they can be shorter than the prefixes or
				//	suffixes.
				// 2) everything before the root is a prefix.
				// 3) everything after the root is a suffix.
				// Of course, if the user sometimes inserts spaces, and sometimes doesn't, all
				// bets are off!  The same is true if he doubles (or worse, triples...) the
				// break characters.

				string[] prefixMarkers = MorphServices.PrefixMarkers(m_caches.MainCache);
				string[] postfixMarkers = MorphServices.PostfixMarkers(m_caches.MainCache);

				StringCollection allMarkers = new StringCollection();
				foreach (string s in prefixMarkers)
				{
					allMarkers.Add(s);
				}

				foreach (string s in postfixMarkers)
				{
					if (!allMarkers.Contains(s))
						allMarkers.Add(s);
				}
				ITsString tssBaseWordform = m_sda.get_MultiStringAlt(kSbWord, ktagSbWordForm, m_sandbox.RawWordformWs);
				// for phrases, the breaking character is a double-space. for normal words, it's simply a space.
				bool fBaseWordIsPhrase = SandboxBase.IsPhrase(tssBaseWordform.Text);
				allMarkers.Add(fBaseWordIsPhrase ? "  " : " ");

				var breakMarkers = new string[allMarkers.Count];
				allMarkers.CopyTo(breakMarkers, 0);
				// If we trim our input string or add spaces, be sure to readjust our selection pointer, m_ichSelInput.
				// Can't do it in DoBasicFinding() which knows where it changed since it is static.
				// For trimming, it is adjusted in TrimInputString().
				string fullForm = DoBasicFinding(TrimInputString(), breakMarkers, prefixMarkers, postfixMarkers);
				AdjustIpForInsertions(fullForm);

				List<int> ccTrailingMorphs;
				List<string> morphs = BreakIntoMorphs(fullForm, tssBaseWordform.Text, out ccTrailingMorphs);
				int imorph = 0;
				m_cNewMorphs = morphs.Count;
				foreach (string morph in morphs)
				{
					HandleMorpheme(morph, ccTrailingMorphs[imorph], morphs.Count == 1);
					++imorph;
				}
				// Delete any leftover old morphemes.
				var oldMorphHvos = new List<int>();
				imorph = m_imorph;
				for (; m_imorph < m_cOldMorphs; m_imorph++)
				{
					oldMorphHvos.Add(m_sda.get_VecItem(m_hvoSbWord, ktagSbWordMorphs, m_imorph));
				}
				foreach (int hvo in oldMorphHvos)
					m_sda.DeleteObjOwner(m_hvoSbWord, hvo, ktagSbWordMorphs, imorph);
			}

			/// <summary>
			/// Check the fullForm for added spaces. m_input must be unchanged since fullForm was changed.
			/// </summary>
			/// <param name="fullForm">The Form that may have had spaces added w/o adjusting the ip</param>
			private void AdjustIpForInsertions(string fullForm)
			{
				int ch;
				for (ch = 0; ch < m_input.Length; ch++)
				{   // find where the two strings first differ
					if (m_input[ch] != fullForm[ch]) break;
				}
				if (ch < m_input.Length || ch < fullForm.Length)
				{   // ch is inside one of the strings
					if (m_ichSelInput >= ch)
					{   // Move ip forward at least one, two if this is a phrase
						m_ichSelInput++;
						if (ch + 1 < fullForm.Length && ' ' == fullForm[ch + 1])
							m_ichSelInput++;
					}
				}
			}

			private string TrimInputString()
			{
				string origInput = m_input;
				m_input = m_input.Trim();
				// first see if the selection was at the end of the input string on a whitespace
				if (origInput.LastIndexOfAny(Unicode.SpaceChars) == (origInput.Length - 1) &&
					m_ichSelInput == origInput.Length)
				{
					m_ichSelInput = m_input.Length;	// adjust to the new length
				}
				else if (origInput.IndexOfAny(Unicode.SpaceChars) == 0 &&
					m_ichSelInput >= 0)
				{
					// if we trimmed something from the start of our input string
					// then adjust the selection offset by the the amount trimmed.
					m_ichSelInput -= origInput.IndexOf(m_input);
				}
				return m_input;
			}

			/// <summary>
			/// The selection (in m_sinput) where we'd like to restore the selection
			/// (by means of MakeSel, after calling Run).
			/// </summary>
			public int IchSel
			{
				get { return m_ichSelInput; }
				set { m_ichSelInput = value; }
			}

			/// <summary>
			/// Reestablish a selection, if possible.
			/// </summary>
			public void MakeSel()
			{
				if (m_tagSel == -1)
					return;
				int clev = 2; // typically two level
				if (m_tagSel != ktagSbNamedObjName)
					clev--; // prefix and postfix are one level less embedded
				SelLevInfo[] rgsli = new SelLevInfo[clev];
				// The selection is in the morphemes of the root object
				rgsli[clev - 1].ihvo = m_ihvoSelMorph;
				rgsli[clev - 1].tag = ktagSbWordMorphs;
				if (clev > 1)
					rgsli[0].tag = ktagSbMorphForm; // leave other slots zero

				// Set writing system of the selection (LT-16593).
				var propsBuilder = TsPropsBldrClass.Create();
				propsBuilder.SetIntPropValues((int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, m_wsVern);
				try
				{
					m_sandbox.RootBox.MakeTextSelection(
						m_sandbox.IndexOfCurrentItem, // which root,
						clev, rgsli,
						m_tagSel,
						0, // no previous occurrence
						m_ichSelOutput, m_ichSelOutput, m_wsVern,
						false, // needs to be false here to associate with trailing character
						// esp. for when the cursor is at the beginning of the morpheme (LT-7773)
						-1, // end not in different object
						propsBuilder.GetTextProps(),
						true); // install it.
				}
				catch (Exception)
				{
					// Ignore anything that goes wrong making a selection. At worst we just don't have one.
				}
			}
		}
	}

	#region SandboxEditMonitor class
	/// <summary>
	/// This class is mostly responsible for performing various operations on the sequence of morphemes, to do
	/// with altering the morpheme breakdown. It also issues m_sandbox.OnUpdateEdited() when PropChanges
	/// occur in the cache.
	///
	/// 1. Provides an IVwNotifyChange implementation that updates all the rest of the fields when
	/// the morpheme text (or ktagSbMorphPostfix or ktagSbMorphPrefix) is edited.
	/// For example: institutionally -- originally one morpheme, probably no matches, *** below.
	/// institution ally -- breaks into two morphemes, look up both, nothing found
	/// institution -ally -- hyphen breaks out to ktagSbMorphPrefix.
	/// institution -al ly -- make third morpheme
	/// institution -al -ly -- move hyphen to ktagSbMorphPrefix
	/// in-stitution -al -ly -- I think we treat this as an odd morpheme.
	/// in- stitution -al -ly -- break up, make ktabSbMorphSuffix

	/// All these cases are handled by calling the routine that collapses the
	/// morphemes into a single string, then the one that regenerates them (any time a relevant
	/// property changes), while keeping track of how to restore the selection.

	/// When backspace or del forward tries to delete a space, we need to collapse morphemes.
	/// The root site will receive OnProblemDeletion(sel, kdptBsAtStartPara\kdptDelAtEndPara).
	/// Basically we need to be able to collapse the morphemes to a string, keeping track
	/// of the position, make the change, recompute morphemes etc, and restore the selection.
	/// Again, this is basically done by figuring the combined morphemes, deleting the space,
	/// then figuring the resulting morphemes (and restoring the selection).
	/// </summary>
	internal class SandboxEditMonitor : FwDisposableBase, IVwNotifyChange
	{
		SandboxBase m_sandbox; // The sandbox we're working from.
		string m_morphString; // The representation of the current morphemes as a simple string.
		int m_ichSel = -1; // The index of the selection within that string, or -1 if we don't know it.
		ISilDataAccess m_sda;
		int m_hvoSbWord;
		int m_hvoMorph;
		bool m_fNeedMorphemeUpdate = false; // Set true if a property we care about changes.
		/// <summary>
		/// don't start monitoring until directed to do so.
		/// </summary>
		private bool m_monitorPropChanges = false;
		private bool m_propChangesOccurredWhileNotMonitoring = false;

		internal SandboxEditMonitor(SandboxBase sandbox)
		{
			m_sandbox = sandbox;
			m_sda = sandbox.Caches.DataAccess;
			m_hvoSbWord = m_sandbox.RootWordHvo;
			m_sda.AddNotification(this);
		}

		internal bool NeedMorphemeUpdate
		{
			get
			{
				CheckDisposed();
				return m_fNeedMorphemeUpdate;
			}
			set
			{
				CheckDisposed();
				m_fNeedMorphemeUpdate = value;
			}
		}

		internal string BuildCurrentMorphsString()
		{
			CheckDisposed();

			return BuildCurrentMorphsString(null);
		}

		/// <summary>
		/// If we can't get the ws from a selection, we should get it from the choice line.
		/// </summary>
		int VernWsForPrimaryMorphemeLine
		{
			get
			{
				// For now, use the real default vernacular ws for the sandbox.
				// This only becomes available after the sandbox has been initialized.
				return m_sandbox.RawWordformWs;
			}
		}

		internal string BuildCurrentMorphsString(IVwSelection sel)
		{
			CheckDisposed();

			int ichSel = -1;
			int hvoObj = 0;
			int tag = 0;
			int ws = 0;
			if (sel != null)
			{
				TextSelInfo selInfo = new TextSelInfo(sel);
				ws = selInfo.WsAltAnchor;
				ichSel = selInfo.IchAnchor;
				hvoObj = selInfo.HvoAnchor;
				tag = selInfo.TagAnchor;
			}
			// for now, we'll just configure getting the string for the primary morpheme line.
			ws = this.VernWsForPrimaryMorphemeLine;
			m_ichSel = -1;

			ITsStrBldr builder = TsStrBldrClass.Create();
			ITsString space = TsStringUtils.MakeTss(" ", ws);
			ISilDataAccess sda = m_sandbox.Caches.DataAccess;

			ITsString tssWordform = m_sandbox.SbWordForm(ws);
			// we're dealing with a phrase if there are spaces in the word.
			bool fBaseWordIsPhrase = SandboxBase.IsPhrase(tssWordform.Text);
			int cmorphs = m_sda.get_VecSize(m_hvoSbWord, SandboxBase.ktagSbWordMorphs);
			for (int imorph = 0; imorph < cmorphs; ++imorph)
			{
				int hvoMorph = m_sda.get_VecItem(m_hvoSbWord, SandboxBase.ktagSbWordMorphs, imorph);
				if (imorph != 0)
				{
					builder.ReplaceTsString(builder.Length, builder.Length, space);
					// add a second space to separate morphs in a phrase.
					if (fBaseWordIsPhrase)
						builder.ReplaceTsString(builder.Length, builder.Length, space);
				}
				int hvoMorphForm = sda.get_ObjectProp(hvoMorph, SandboxBase.ktagSbMorphForm);
				if (hvoMorph == hvoObj && tag == SandboxBase.ktagSbMorphPrefix)
					m_ichSel = builder.Length + ichSel;
				builder.ReplaceTsString(builder.Length, builder.Length,
					sda.get_StringProp(hvoMorph, SandboxBase.ktagSbMorphPrefix));
				if (hvoMorphForm == hvoObj && tag == SandboxBase.ktagSbNamedObjName)
					m_ichSel = builder.Length + ichSel;
				builder.ReplaceTsString(builder.Length, builder.Length,
					sda.get_MultiStringAlt(hvoMorphForm, SandboxBase.ktagSbNamedObjName, ws));
				if (hvoMorph == hvoObj && tag == SandboxBase.ktagSbMorphPostfix)
					m_ichSel = builder.Length + ichSel;
				builder.ReplaceTsString(builder.Length, builder.Length,
					sda.get_StringProp(hvoMorph, SandboxBase.ktagSbMorphPostfix));
			}
			if (cmorphs == 0)
			{
				if (m_hvoSbWord == hvoObj && tag == SandboxBase.ktagMissingMorphs)
					m_ichSel = ichSel;
				m_morphString = SandboxBase.InterlinComboHandler.StrFromTss(tssWordform);
			}
			else
			{
				m_morphString = SandboxBase.InterlinComboHandler.StrFromTss(builder.GetString());
			}
			return m_morphString;
		}

		private static bool IsBaseWordPhrase(string baseWord)
		{

			bool fBaseWordIsPhrase = baseWord.IndexOfAny(Unicode.SpaceChars) != -1;
			return fBaseWordIsPhrase;
		}

		/// <summary>
		/// Handle an otherwise-difficult backspace (joining morphemes by deleting a 'space')
		/// Return true if successful.
		/// </summary>
		/// <returns></returns>
		public bool HandleBackspace()
		{
			CheckDisposed();

			string currentMorphemes = BuildCurrentMorphsString(m_sandbox.RootBox.Selection);
			if (m_ichSel <= 0)
				return false;
			// This would be risky if we might be deleting a diacritic or surrogate, but we're certainly
			// deleting a space.
			currentMorphemes = currentMorphemes.Substring(0, m_ichSel - 1)
				+ currentMorphemes.Substring(m_ichSel);
			m_ichSel--;
			SetMorphemes(currentMorphemes);
			return true;
		}

		/// <summary>
		/// Handle an otherwise-difficult delete (joining morphemes by deleting a 'space').
		/// </summary>
		/// <returns></returns>
		public bool HandleDelete()
		{
			CheckDisposed();

			string currentMorphemes = BuildCurrentMorphsString(m_sandbox.RootBox.Selection);
			if (m_ichSel < 0 || m_ichSel >= currentMorphemes.Length)
				return false;
			// This would be risky if we might be deleting a diacritic or surrogate, but we're certainly
			// deleting a space.
			currentMorphemes = currentMorphemes.Substring(0, m_ichSel)
				+ currentMorphemes.Substring(m_ichSel + 1);
			SetMorphemes(currentMorphemes);
			return true;
		}

		#region IVwNotifyChange Members

		/// <summary>
		/// A property changed. Is it one of the ones that requires us to update the morpheme list?
		/// Even if so, we shouldn't do it now, because it's dangerous to issue new PropChanged
		/// messages for the same property during a PropChanged. Instead we wait for a DoUpdates call.
		/// Also don't do it if we're in the middle of processing such an update already.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();
			if (!m_monitorPropChanges)
			{
				m_propChangesOccurredWhileNotMonitoring = true;
				return;
			}
			if (IsPropMorphBreak(hvo, tag, ivMin))
			{
				m_fNeedMorphemeUpdate = true;
			}
			// notify the parent sandbox that something has changed its cache.
			m_sandbox.OnUpdateEdited();
		}

		public void DoPendingMorphemeUpdates()
		{
			CheckDisposed();

			if (!m_fNeedMorphemeUpdate)
				return; // Nothing we care about has changed.
			// This needs to be set BEFORE we call UpdateMorphemes...otherwise, UpdateMorphemes eventually
			// changes the selection, which triggers another call, making an infinite loop until the
			// stack overflows.
			m_fNeedMorphemeUpdate = false;
			try
			{
				if (m_hvoMorph != 0)
				{
					// The actual form of the morpheme changed. Any current analysis can't be
					// relevant any more. (We might expect the morpheme breaker to fix this, but
					// in fact it thinks the morpheme hasn't changed, because the cache value
					// has already been updated.)
					IVwCacheDa cda = m_sda as IVwCacheDa;
					cda.CacheObjProp(m_hvoMorph, SandboxBase.ktagSbMorphEntry, 0);
					cda.CacheObjProp(m_hvoMorph, SandboxBase.ktagSbMorphGloss, 0);
					cda.CacheObjProp(m_hvoMorph, SandboxBase.ktagSbMorphPos, 0);
				}
				UpdateMorphemes();
			}
			finally
			{
				// We also do this as a way of making quite sure that it doesn't get set again
				// as a side effect of UpdateMorphemes...another way we could get an infinite
				// loop.
				m_fNeedMorphemeUpdate = false;
			}

		}

		/// <summary>
		/// Is the property one of the ones that represents a morpheme breakdown?
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public bool IsPropMorphBreak(int hvo, int tag, int ws)
		{
			CheckDisposed();

			switch (tag)
			{
				case SandboxBase.ktagSbMorphPostfix:
				case SandboxBase.ktagSbMorphPrefix:
					m_hvoMorph = 0;
					return true;
				case SandboxBase.ktagSbNamedObjName:
					if (ws != VernWsForPrimaryMorphemeLine)
						return false;
					// Name of some object: is it a morph?
					int cmorphs = m_sda.get_VecSize(m_hvoSbWord, SandboxBase.ktagSbWordMorphs);
					for (int imorph = 0; imorph < cmorphs; ++imorph)
					{
						m_hvoMorph = m_sda.get_VecItem(m_hvoSbWord, SandboxBase.ktagSbWordMorphs,
							imorph);
						if (hvo == m_sda.get_ObjectProp(m_hvoMorph, SandboxBase.ktagSbMorphForm))
						{
							return true;
						}
					}
					m_hvoMorph = 0;
					break;
				case SandboxBase.ktagSbWordMorphs:
					return true;
				default:
					// Some property we don't care about.
					return false;
			}
			return false;
		}

		void UpdateMorphemes()
		{
			SetMorphemes(BuildCurrentMorphsString(m_sandbox.RootBox.Selection));
		}

		void SetMorphemes(string currentMorphemes)
		{
			if (currentMorphemes.Length == 0)
			{
				// Reconstructing the sandbox rootbox after deleting all morpheme characters
				// will cause the user to lose the ability to type in the morpheme line (cf. LT-1621).
				// So just return here, since there are no morphemes to process.
				return;
			}
			using (new SandboxEditMonitorHelper(this, true))
			{
				// This code largely duplicates that found in UpdateMorphBreaks() following the call
				// to the EditMorphBreaksDlg, with addition of the m_monitorPropChanges flag and setting
				// the selection to stay in synch with the typing.  Modifying the code to more
				// closely follow that code fixed LT-1023.
				IVwCacheDa cda = (IVwCacheDa)m_sda;
				SandboxBase.MorphemeBreaker mb = new SandboxBase.MorphemeBreaker(m_sandbox.Caches,
					currentMorphemes, m_hvoSbWord, VernWsForPrimaryMorphemeLine, m_sandbox);
				mb.IchSel = m_ichSel;
				mb.Run();
				m_fNeedMorphemeUpdate = false;
				m_sandbox.RootBox.Reconstruct(); // Everything changed, more or less.
				mb.MakeSel();
			}
		}

		#endregion

		internal void StartMonitoring()
		{
			if (m_propChangesOccurredWhileNotMonitoring)
			{
				m_sandbox.OnUpdateEdited();
				m_propChangesOccurredWhileNotMonitoring = false;
			}
			m_monitorPropChanges = true;
		}

		internal void StopMonitoring()
		{
			m_monitorPropChanges = false;
		}

		internal bool IsMonitoring
		{
			get { return m_monitorPropChanges; }
		}

		#region FwDisposableBase

		protected override void DisposeManagedResources()
		{
			// Dispose managed resources here.
			if (m_sda != null )
			{
				m_sda.RemoveNotification(this);
			}
		}

		protected override void DisposeUnmanagedResources()
		{
			m_sda = null;
			m_sandbox = null;
		}

		#endregion
	}

	#endregion SandboxEditMonitor class

	internal class SandboxEditMonitorHelper : FwDisposableBase
	{
		internal SandboxEditMonitorHelper(SandboxEditMonitor editMonitor, bool fSuspendMonitor)
		{
			EditMonitor = editMonitor;
			if (fSuspendMonitor)
			{
				EditMonitor.StopMonitoring();
				SuspendedMonitor = true;
			}
		}

		SandboxEditMonitor EditMonitor { get; set; }
		bool SuspendedMonitor { get; set; }

		protected override void DisposeManagedResources()
		{
			// re-enable monitor if we had suspended it.
			if (SuspendedMonitor)
			{
				EditMonitor.StartMonitoring();
				SuspendedMonitor = false;
			}
		}

		protected override void DisposeUnmanagedResources()
		{
			EditMonitor = null;
		}
	}
}
