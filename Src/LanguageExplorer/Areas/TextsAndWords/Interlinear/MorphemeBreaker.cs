// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Analyze the string. We're looking for something like un- except -ion -al -ly.
	/// The one with spaces on both sides is a root, the others are prefixes or suffixes.
	/// Todo WW(JohnT): enhance to handle other morpheme break characters.
	/// Todo WW (JohnT): enhance to look for trailing ;POS and handle appropriately.
	/// </summary>
	internal class MorphemeBreaker
	{
		string m_input; // string being processed into morphemes.
		ISilDataAccess m_sda; // cache to update with new objects etc (m_caches.DataAccess).
		IVwCacheDa m_cda; // another interface on same cache.
		CachePair m_caches; // Both the caches we are working with.
		int m_hvoSbWord; // HVO of the Sandbox word that will own the new morphs
		int m_cOldMorphs;
		int m_cNewMorphs;
		int m_wsVern = 0;
		IMoMorphTypeRepository m_types;
		int m_imorph = 0;
		SandboxBase m_sandbox;

		// These variables are used to re-establish a selection in the morpheme break line
		// after rebuilding the morphemes.
		int m_tagSel = -1; // The property we want the selection in (or -1 for none).
		int m_ihvoSelMorph; // The index of the morpheme we want the selection in.
		int m_ichSelOutput; // The character offset where we want the selection to be made.
		int m_cchPrevMorphemes; // Total length of morphemes before m_imorph.

		public MorphemeBreaker(CachePair caches, string input, int hvoSbWord, int wsVern, SandboxBase sandbox)
		{
			m_caches = caches;
			m_sda = caches.DataAccess;
			m_cda = (IVwCacheDa)m_sda;
			m_input = input;
			m_hvoSbWord = hvoSbWord;
			m_cOldMorphs = m_sda.get_VecSize(m_hvoSbWord, SandboxBase.ktagSbWordMorphs);
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
		private void HandleMorpheme(string stMorph, int ccTrailing, bool fMonoMorphemic)
		{
			var realForm = stMorph; // Gets stripped of morpheme-type characters.
			IMoMorphType mmt;
			try
			{
				// The subclass of MoMorph to create if we need a new object.
				int clsidForm;
				mmt = MorphServices.FindMorphType(m_caches.MainCache, ref realForm, out clsidForm);
			}
			catch (Exception e)
			{
				MessageBox.Show(null, e.Message, ITextStrings.ksWarning, MessageBoxButtons.OK);
				mmt = m_types.GetObject(MoMorphTypeTags.kguidMorphStem);
			}
			int hvoSbForm; // hvo of the SbNamedObj that is the form of the morph.
			int hvoSbMorph;
			var fCanReuseOldMorphData = false;
			var maxSkip = m_cOldMorphs - m_cNewMorphs;
			if (m_imorph < m_cOldMorphs)
			{
				// If there's existing analysis and any morphs match, keep the analysis of
				// the existing morph. It's probably the best guess.
				var sSbForm = GetExistingMorphForm(out hvoSbMorph, out hvoSbForm, m_imorph);
				if (sSbForm != realForm && maxSkip > 0)
				{
					// If we're deleting morph breaks, we may need to skip over a morph to
					// find the matching existing morph.
					var skippedMorphs = new List<int>
					{
						hvoSbMorph
					};
					for (var skip = 1; skip <= maxSkip; ++skip)
					{
						int hvoSbFormT;
						int hvoSbMorphT;
						var sSbFormT = GetExistingMorphForm(out hvoSbMorphT, out hvoSbFormT, m_imorph + skip);
						if (sSbFormT == realForm)
						{
							hvoSbForm = hvoSbFormT;
							hvoSbMorph = hvoSbMorphT;
							sSbForm = sSbFormT;
							foreach (var hvo in skippedMorphs)
							{
								m_sda.DeleteObjOwner(m_hvoSbWord, hvo, SandboxBase.ktagSbWordMorphs, m_imorph);
							}
							m_cOldMorphs -= skippedMorphs.Count;
							break;
						}
						skippedMorphs.Add(hvoSbMorphT);
					}
				}
				if (sSbForm != realForm)
				{
					// Clear out the old analysis. Can't be relevant to a different form.
					m_cda.CacheObjProp(hvoSbMorph, SandboxBase.ktagSbMorphEntry, 0);
					m_cda.CacheObjProp(hvoSbMorph, SandboxBase.ktagSbMorphGloss, 0);
					m_cda.CacheObjProp(hvoSbMorph, SandboxBase.ktagSbMorphPos, 0);
				}
				else
				{
					fCanReuseOldMorphData = m_sda.get_StringProp(hvoSbMorph, SandboxBase.ktagSbMorphPrefix).Text == mmt.Prefix
						&& m_sda.get_StringProp(hvoSbMorph, SandboxBase.ktagSbMorphPostfix).Text == mmt.Postfix;
				}
			}
			else
			{
				// Make a new morph, and an SbNamedObj to go with it.
				hvoSbMorph = m_sda.MakeNewObject(SandboxBase.kclsidSbMorph, m_hvoSbWord, SandboxBase.ktagSbWordMorphs, m_imorph);
				hvoSbForm = m_sda.MakeNewObject(SandboxBase.kclsidSbNamedObj, hvoSbMorph, SandboxBase.ktagSbMorphForm, -2); // -2 for atomic
			}
			if (!fCanReuseOldMorphData)
			{
				// This might be redundant, but it isn't expensive.
				m_cda.CacheStringAlt(hvoSbForm, SandboxBase.ktagSbNamedObjName, m_wsVern, TsStringUtils.MakeString(realForm, m_wsVern));
				m_cda.CacheStringProp(hvoSbMorph, SandboxBase.ktagSbMorphPrefix, TsStringUtils.MakeString(mmt.Prefix, m_wsVern));
				m_cda.CacheStringProp(hvoSbMorph, SandboxBase.ktagSbMorphPostfix, TsStringUtils.MakeString(mmt.Postfix, m_wsVern));
				// Fill in defaults.
				m_sandbox.EstablishDefaultEntry(hvoSbMorph, realForm, mmt, fMonoMorphemic);
			}
			// the morpheme is not a guess.
			m_cda.CacheIntProp(hvoSbForm, SandboxBase.ktagSbNamedObjGuess, 0);
			// Figure whether selection is in this morpheme.
			var ichSelMorph = IchSel - m_cchPrevMorphemes;
			var cchPrefix = mmt.Prefix?.Length ?? 0;
			var cchPostfix = mmt.Postfix?.Length ?? 0;
			var cchMorph = realForm.Length;
			// If this is < 0, we must be in a later morpheme and should have already
			// established m_ichSelOutput.
			if (ichSelMorph >= 0 && ichSelMorph <= cchPrefix + cchPostfix + cchMorph)
			{
				m_ihvoSelMorph = m_imorph;
				m_ichSelOutput = ichSelMorph - cchPrefix;
				m_tagSel = SandboxBase.ktagSbNamedObjName;
				if (m_ichSelOutput < 0)
				{
					// in the prefix
					m_tagSel = SandboxBase.ktagSbMorphPrefix;
					m_ichSelOutput = ichSelMorph;
				}
				else if (m_ichSelOutput > cchMorph)
				{
					if (cchPostfix > 0)
					{
						// in the postfix
						m_ichSelOutput = cchPostfix;
						m_tagSel = SandboxBase.ktagSbMorphPostfix;
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
			hvoSbMorph = m_sda.get_VecItem(m_hvoSbWord, SandboxBase.ktagSbWordMorphs, imorph);
			hvoSbForm = m_sda.get_ObjectProp(hvoSbMorph, SandboxBase.ktagSbMorphForm);
			Debug.Assert(hvoSbForm != 0); // We always have one of these for each form.
			return m_sda.get_MultiStringAlt(hvoSbForm, SandboxBase.ktagSbNamedObjName, m_wsVern).Text;
		}

		/// <summary>
		/// Handle basic work on finding the morpheme breaks.
		/// </summary>
		/// <returns>A string suitable for followup processing by client.</returns>
		public static string DoBasicFinding(string input, string[] breakMarkers, string[] prefixMarkers, string[] postfixMarkers)
		{
			var fullForm = input;
			// the morphBreakSpace should be the last item.
			var morphBreakSpace = breakMarkers[breakMarkers.Length - 1];
			Debug.Assert(morphBreakSpace == " " || morphBreakSpace == "  ", "expected a morphbreak space at last index");

			// First, find the segment boundaries.
			var vichMin = new List<int>();
			var vichLim = new List<int>();
			var ccchSeg = 0;
			vichMin.Add(0);
			for (var ichStart = 0; ichStart < fullForm.Length;)
			{
				int iMatched;
				var ichBrk = fullForm.IndexOfAnyString(breakMarkers, ichStart, out iMatched);
				if (ichBrk < 0)
				{
					break;
				}
				vichLim.Add(ichBrk);
				// Skip over consecutive markers
				for (ichBrk += breakMarkers[iMatched].Length;
					ichBrk < fullForm.Length;
					ichBrk += breakMarkers[iMatched].Length)
				{
					if (fullForm.IndexOfAnyString(breakMarkers, ichBrk, out iMatched) != ichBrk)
					{
						break;
					}
				}
				vichMin.Add(ichBrk);
				ichStart = ichBrk;
			}
			vichLim.Add(fullForm.Length);
			Debug.Assert(vichMin.Count == vichLim.Count);
			var ieRoot = 0;
			var cchRoot = 0;
			var cLongest = 0;
			for (var i = 0; i < vichMin.Count; ++i)
			{
				var ichMin = vichMin[i];
				var ichLim = vichLim[i];
				var cchSeg = ichLim - ichMin;
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
				ieRoot = 1;     // Pure speculation at this point, based on lengths.
			}
			// Look for a root that's delimited by spaces fore and aft.
			for (var i = 0; i < vichMin.Count; ++i)
			{
				var ichMin = vichMin[i];
				var ichLim = vichLim[i];
				if (ichMin != 0 && fullForm[ichMin - 1] != ' ' || ichLim != fullForm.Length && fullForm[ichLim] != ' ')
				{
					continue;
				}
				ieRoot = i;
				break;
			}
			// Here it is: what we've been computing towards up to this point!
			var ichRootMin = vichMin[ieRoot];
			// The code to insert spaces is problematic. After all, some words composed of compounded roots are hyphenated,
			// but we like to use hyphens to mark affixes. Automatically inserting a space in order to handle affixes prevents
			// hyphenated roots from being properly handled.
			for (var fFixedProblem = true; fFixedProblem;)
			{
				fFixedProblem = false;
				for (var ichStart = 0; ;)
				{
					int iMatchedPrefix;
					var indexPrefix = fullForm.IndexOfAnyString(prefixMarkers, ichStart, out iMatchedPrefix);
					int iMatchedPostfix;
					var indexPostfix = fullForm.IndexOfAnyString(postfixMarkers, ichStart, out iMatchedPostfix);
					if (indexPrefix < 0 && indexPostfix < 0)
					{
						break; // no (remaining) problems!!
					}
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
					var cchWordPreceding = 0;
					var fFoundProblemPreceding = false;
					for (var ich = index - 1; ich >= ichStart; --ich, ++cchWordPreceding)
					{
						if (ich > index - morphBreakSpace.Length)
						{
							// we'll assume we found a problem if we found a space here
							// unless we match the morphBreakSpace next iteration.
							if (fullForm[ich] == ' ')
							{
								fFoundProblemPreceding = true;
							}
							continue;   // we can't match the substring (yet)
						}

						if (fullForm.Substring(ich, morphBreakSpace.Length) != morphBreakSpace)
						{
							continue;
						}
						// after having enough room to check the morphBreakSpace,
						// we found one, so we don't really have ccWordPreceding.
						if (ich + morphBreakSpace.Length == index)
						{
							cchWordPreceding = 0;
							fFoundProblemPreceding = false;
						}
						break;
					}

					indexPrefix = fullForm.IndexOfAnyString(prefixMarkers, ichNext, out iMatchedPrefix);
					indexPostfix = fullForm.IndexOfAnyString(postfixMarkers, ichNext, out iMatchedPostfix);
					int index2;
					if (indexPrefix < 0 && indexPostfix < 0)
					{
						index2 = fullForm.Length;
					}
					else if (indexPrefix < 0)
					{
						index2 = indexPostfix;
					}
					else if (indexPostfix < 0)
					{
						index2 = indexPrefix;
					}
					else
					{
						index2 = Math.Min(indexPrefix, indexPostfix);
					}

					var cchWordFollowing = 0;
					for (var ich = ichNext; ich < index2; ++ich, ++cchWordFollowing)
					{
						if (ich + morphBreakSpace.Length > fullForm.Length)
						{
							continue;   // we can't match the substring
						}

						if (fullForm.Substring(ich, morphBreakSpace.Length) == morphBreakSpace)
						{
							break;
						}
					}
					ichStart = ichNext; // for next iteration of inner loop, if any
					if ((cchWordFollowing <= 0 || cchWordPreceding <= 0) && !fFoundProblemPreceding)
					{
						continue;
					}
					// We will fix a problem! Insert a space at index or index + cchMarker.
					fFixedProblem = true;
					var morphBreakSpaceAdjusted = morphBreakSpace;
					if (fFoundProblemPreceding)
					{
						// we found a preceding space, we need to just
						// add one more space before the affix.
						morphBreakSpaceAdjusted = " ";
					}
					else if (index < ichRootMin)
					{
						// if before the root, guess a prefix.  Otherwise, guess a suffix.
						index += cchMarker; // adjust for the marker (can be > 1)
						ichRootMin += morphBreakSpace.Length;   // adjust for inserted space.
					}
					Debug.Assert(index < input.Length && fullForm.Length <= input.Length);
					fullForm = fullForm.Substring(0, index) + morphBreakSpaceAdjusted + fullForm.Substring(index);
					break; // from inner loop, continue outer.
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
				return new List<string>(new[] { morphFormWithMarkers });
			}
			// find any existing white spaces in the baseWord.
			var morphs = new List<string>();
			// we're dealing with a phrase if there are spaces in the word.
			var fBaseWordIsPhrase = SandboxBase.IsPhrase(baseWord);
			var morphEndOffsets = IchLimOfMorphs(morphFormWithMarkers, fBaseWordIsPhrase);
			var prevEndOffset = 0;
			foreach (var morphEndOffset in morphEndOffsets)
			{
				var morph = morphFormWithMarkers.Substring(prevEndOffset, morphEndOffset - prevEndOffset);
				morph = morph.Trim();
				// figure the trailing characters following the previous morph by the difference betweeen
				// the current morphEndOffset the length of the trimmed morph and prevEndOffset
				if (prevEndOffset > 0)
				{
					ccTrailingMorphs.Add(morphEndOffset - prevEndOffset - morph.Length);
				}

				if (!string.IsNullOrEmpty(morph))
				{
					morphs.Add(morph);
				}
				prevEndOffset = morphEndOffset;
			}
			// add the count of the final trailing space characters
			ccTrailingMorphs.Add(morphFormWithMarkers.Length - morphEndOffsets[morphEndOffsets.Count - 1]);
			return morphs;
		}

		/// <summary>
		/// get the end offsets for potential morphs based upon whitespace delimiters
		/// </summary>
		private static List<int> IchLimOfMorphs(string sourceString, bool fBaseWordIsPhrase)
		{
			var whiteSpaceOffsets = WhiteSpaceOffsets(sourceString);
			var morphEndOffsets = new List<int>(whiteSpaceOffsets);
			var prevOffset = -1;
			foreach (var offset in whiteSpaceOffsets)
			{
				// we always want to remove spaces following a previous space
				// or if we're in a a phrase, always remove the last offset, since
				// it cannot be followed by a second one.
				if (prevOffset != -1 && offset == prevOffset + 1 || fBaseWordIsPhrase && offset == whiteSpaceOffsets[whiteSpaceOffsets.Count - 1])
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
			var whiteSpaceOffsets = new List<int>();
			var ichMatch = 0;
			do
			{
				ichMatch = sourceString.IndexOfAny(Unicode.SpaceChars, ichMatch);
				if (ichMatch == -1)
				{
					continue;
				}
				whiteSpaceOffsets.Add(ichMatch);
				ichMatch++;
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
			var prefixMarkers = MorphServices.PrefixMarkers(m_caches.MainCache);
			var postfixMarkers = MorphServices.PostfixMarkers(m_caches.MainCache);
			var allMarkers = new StringCollection();
			foreach (var prefixmarker in prefixMarkers)
			{
				allMarkers.Add(prefixmarker);
			}

			foreach (var postfixmarker in postfixMarkers)
			{
				if (!allMarkers.Contains(postfixmarker))
				{
					allMarkers.Add(postfixmarker);
				}
			}
			var tssBaseWordform = m_sda.get_MultiStringAlt(SandboxBase.kSbWord, SandboxBase.ktagSbWordForm, m_sandbox.RawWordformWs);
			// for phrases, the breaking character is a double-space. for normal words, it's simply a space.
			var fBaseWordIsPhrase = SandboxBase.IsPhrase(tssBaseWordform.Text);
			allMarkers.Add(fBaseWordIsPhrase ? "  " : " ");

			var breakMarkers = new string[allMarkers.Count];
			allMarkers.CopyTo(breakMarkers, 0);
			// If we trim our input string or add spaces, be sure to readjust our selection pointer, m_ichSelInput.
			// Can't do it in DoBasicFinding() which knows where it changed since it is static.
			// For trimming, it is adjusted in TrimInputString().
			var fullForm = DoBasicFinding(TrimInputString(), breakMarkers, prefixMarkers, postfixMarkers);
			AdjustIpForInsertions(fullForm);

			List<int> ccTrailingMorphs;
			var morphs = BreakIntoMorphs(fullForm, tssBaseWordform.Text, out ccTrailingMorphs);
			var imorph = 0;
			m_cNewMorphs = morphs.Count;
			foreach (var morph in morphs)
			{
				HandleMorpheme(morph, ccTrailingMorphs[imorph], morphs.Count == 1);
				++imorph;
			}
			// Delete any leftover old morphemes.
			var oldMorphHvos = new List<int>();
			imorph = m_imorph;
			for (; m_imorph < m_cOldMorphs; m_imorph++)
			{
				oldMorphHvos.Add(m_sda.get_VecItem(m_hvoSbWord, SandboxBase.ktagSbWordMorphs, m_imorph));
			}

			foreach (var hvo in oldMorphHvos)
			{
				m_sda.DeleteObjOwner(m_hvoSbWord, hvo, SandboxBase.ktagSbWordMorphs, imorph);
			}
		}

		/// <summary>
		/// Check the fullForm for added spaces. m_input must be unchanged since fullForm was changed.
		/// </summary>
		/// <param name="fullForm">The Form that may have had spaces added w/o adjusting the ip</param>
		private void AdjustIpForInsertions(string fullForm)
		{
			int ch;
			for (ch = 0; ch < m_input.Length; ch++)
			{
				// find where the two strings first differ
				if (m_input[ch] != fullForm[ch])
				{
					break;
				}
			}

			if (ch >= m_input.Length && ch >= fullForm.Length)
			{
				return;
			}
			// ch is inside one of the strings
			if (IchSel < ch)
			{
				return;
			}
			// Move ip forward at least one, two if this is a phrase
			IchSel++;
			if (ch + 1 < fullForm.Length && ' ' == fullForm[ch + 1])
			{
				IchSel++;
			}
		}

		private string TrimInputString()
		{
			var origInput = m_input;
			m_input = m_input.Trim();
			// first see if the selection was at the end of the input string on a whitespace
			if (origInput.LastIndexOfAny(Unicode.SpaceChars) == (origInput.Length - 1) && IchSel == origInput.Length)
			{
				IchSel = m_input.Length; // adjust to the new length
			}
			else if (origInput.IndexOfAny(Unicode.SpaceChars) == 0 && IchSel >= 0)
			{
				// if we trimmed something from the start of our input string
				// then adjust the selection offset by the the amount trimmed.
				IchSel -= origInput.IndexOf(m_input);
			}
			return m_input;
		}

		/// <summary>
		/// The selection (in m_sinput) where we'd like to restore the selection
		/// (by means of MakeSel, after calling Run).
		/// </summary>
		public int IchSel { get; set; }

		/// <summary>
		/// Reestablish a selection, if possible.
		/// </summary>
		public void MakeSel()
		{
			if (m_tagSel == -1)
			{
				return;
			}
			var clev = 2; // typically two level
			if (m_tagSel != SandboxBase.ktagSbNamedObjName)
			{
				clev--; // prefix and postfix are one level less embedded
			}
			var rgsli = new SelLevInfo[clev];
			// The selection is in the morphemes of the root object
			rgsli[clev - 1].ihvo = m_ihvoSelMorph;
			rgsli[clev - 1].tag = SandboxBase.ktagSbWordMorphs;
			if (clev > 1)
			{
				rgsli[0].tag = SandboxBase.ktagSbMorphForm; // leave other slots zero
			}

			// Set writing system of the selection (LT-16593).
			var propsBuilder = TsStringUtils.MakePropsBldr();
			propsBuilder.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, m_wsVern);
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