// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using SIL.Extensions;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace SIL.LCModel.Core.Text
{
	/// <summary>
	/// This class represents a formatted string. The string is divided into different runs. Each run
	/// has its own properties that define its style and writing system.
	/// </summary>
	public class TsString : TsStrBase, ITsString, IEquatable<ITsString>, IEquatable<TsString>
	{
		private static readonly ConcurrentDictionary<int, TsString> EmptyStringCache = new ConcurrentDictionary<int, TsString>();

		/// <summary>
		/// Gets the interned empty string for the specified writing system. This ensures that there is only a single
		/// copy of an empty string for each writing system held in memory. This replicates the behavior of the C++
		/// implementation. This is done, because some of the FW code depends on empty strings being interned.
		/// </summary>
		internal static TsString GetInternedEmptyString(int ws)
		{
			return EmptyStringCache.GetOrAdd(ws, handle => new TsString(handle));
		}

		private readonly string m_text;
		private readonly TsRun[] m_runs;

		// Effectively caches the results of get_IsNormalizedForm: one bit is set for each FwNormalizationMode that this string is known to fulfill.
		// E.g., if a string fulfills both NFC and NFKC, its flags will have both (1 << (int)knmNFC) and (1 << (int)knmNFKC) set.
		private int m_normFlags;

		internal TsString(int ws)
			: this(null, ws)
		{
		}

		internal TsString(string text, int ws)
			: this(text, TsTextProps.GetInternedTextProps(ws))
		{
		}

		internal TsString(string text, TsTextProps textProps)
			: this(text, new TsRun(text?.Length ?? 0, textProps).ToEnumerable())
		{
		}

		internal TsString(string text, IEnumerable<TsRun> runs)
		{
			m_text = text == string.Empty ? null : text;
			m_runs = runs.ToArray();
			m_normFlags = 0;
		}

		/// <summary>
		/// Gets the text.
		/// </summary>
		public override string Text => m_text;

		internal override IList<TsRun> Runs => m_runs;

		/// <summary>
		/// Gets the starting offset of the specified run.
		/// </summary>
		public int get_MinOfRun(int irun)
		{
			ThrowIfRunIndexOutOfRange("irun", irun);

			return GetRunIchMin(irun);
		}

		/// <summary>
		/// Get the limit (end offset + 1) of the specified run.
		/// </summary>
		public int get_LimOfRun(int irun)
		{
			ThrowIfRunIndexOutOfRange("irun", irun);

			return m_runs[irun].IchLim;
		}

		/// <summary>
		/// Locks the text for reading. This method is only needed by Views.
		/// </summary>
		public void LockText(out string prgch, out int cch)
		{
			prgch = m_text ?? string.Empty;
			cch = Text == null ? 0 : Text.Length;
		}

		/// <summary>
		/// Unlocks the text. This method is only needed by Views.
		/// </summary>
		public void UnlockText(string rgch)
		{
			if ((m_text ?? string.Empty) != rgch)
				throw new ArgumentException("The text cannot be changed.");
		}

		/// <summary>
		/// Locks the run for reading. This method is only needed by Views.
		/// </summary>
		public void LockRun(int irun, out string prgch, out int cch)
		{
			ThrowIfRunIndexOutOfRange("irun", irun);

			prgch = get_RunText(irun);
			cch = prgch.Length;
		}

		/// <summary>
		/// Unlocks the run.
		/// </summary>
		public void UnlockRun(int irun, string rgch)
		{
			ThrowIfRunIndexOutOfRange("irun", irun);

			if (get_RunText(irun) != rgch)
				throw new ArgumentException("The run text cannot be changed.");
		}

		/// <summary>
		/// Gets a string builder. The builder allows a copy of the string to be modified using
		/// a series of replace operations. There is no connection between the builder and the
		/// string. Data is copied to the builder.
		/// </summary>
		/// <returns></returns>
		public ITsStrBldr GetBldr()
		{
			return new TsStrBldr(Text, Runs);
		}

		/// <summary>
		/// Gets an incremental string builder. The builder allows a copy of the string to be
		/// modified using a series of append operations. There is no connection between the builder
		/// and the string. Data is copied to the builder.
		/// </summary>
		/// <returns></returns>
		public ITsIncStrBldr GetIncBldr()
		{
			return new TsIncStrBldr(Text, Runs);
		}

		/// <summary>
		/// Determines if the specified string is equal to this string.
		/// </summary>
		public bool Equals(ITsString tss)
		{
			var other = tss as TsString;
			return other != null && Equals(other);
		}

		/// <summary>
		/// Gets the substring for the specified range returned as an <see cref="ITsString"/>.
		/// </summary>
		public ITsString GetSubstring(int ichMin, int ichLim)
		{
			ThrowIfCharOffsetOutOfRange("ichMin", ichMin, ichLim);
			ThrowIfCharOffsetOutOfRange("ichLim", ichLim, Length);

			if (ichMin == 0 && ichLim == Length)
				return this;

			string newText = GetChars(ichMin, ichLim);

			var newRuns = new List<TsRun>();
			int irun = get_RunAt(ichMin);
			for (int i = irun; i < m_runs.Length; i++)
			{
				TsRun run = m_runs[i];
				bool lastRun = ichLim <= run.IchLim;
				newRuns.Add(new TsRun((lastRun ? ichLim : run.IchLim) - ichMin, run.TextProps));
				if (lastRun)
					break;
			}

			return new TsString(newText, newRuns.ToArray());
		}

		/// <summary>
		/// Return whether the string is already in the specified normal form.
		/// Note that a string may be considered to be in NFC
		/// even though its text (the plain character sequence) is not.
		/// This is because we don't collapse otherwise collapsible pairs if they
		/// have different style properties.
		/// </summary>
		public bool get_IsNormalizedForm(FwNormalizationMode nm)
		{
			if (IsAlreadyNormalized(nm))
				return true;
			if (string.IsNullOrEmpty(Text))
			{
				NoteAlreadyNormalized(nm);
				return true;
			}
			Icu.UNormalizationMode icuMode = (nm == FwNormalizationMode.knmNFSC) ? Icu.UNormalizationMode.UNORM_NFC : (Icu.UNormalizationMode)nm;
			if (Icu.IsNormalized(Text, icuMode))
			{
				// Don't do this work a second time
				if (nm == FwNormalizationMode.knmNFSC)
					NoteAlreadyNormalized(FwNormalizationMode.knmNFC); // NFC includes NFSC
				else
					NoteAlreadyNormalized(nm);
				return true;
			}
			if (nm == FwNormalizationMode.knmNFSC)
			{
				// NFSC is a special case, where we just have to normalize and compare.
				if (Equals(get_NormalizedForm(nm)))
				{
					NoteAlreadyNormalized(nm);
					return true;
				}
			}
			return false;
		}

		internal bool IsAlreadyNormalized(FwNormalizationMode nm)
		{
			int flag = 1 << (int)nm;
			return (m_normFlags & flag) != 0;
		}

		private void NoteAlreadyNormalized(FwNormalizationMode nm)
		{
			int flagsToSet = 1 << (int)nm;
			// If a string is NFKD, it is also NFD, and so on
			switch (nm)
			{
			case FwNormalizationMode.knmNFKD:
				flagsToSet |= 1 << (int)FwNormalizationMode.knmNFD;
				break;
			case FwNormalizationMode.knmNFKC:
				flagsToSet |= 1 << (int)FwNormalizationMode.knmNFC;
				flagsToSet |= 1 << (int)FwNormalizationMode.knmNFSC;
				break;
			case FwNormalizationMode.knmNFC:
				flagsToSet |= 1 << (int)FwNormalizationMode.knmNFSC;
				break;
			}
			if ((m_normFlags & flagsToSet) == flagsToSet)
				return; // Nothing to do!
			// Set the new flags value in an atomic, thread-safe way.
			SpinWait.SpinUntil(() =>
			{
				int oldValue = m_normFlags;
				int newValue = oldValue | flagsToSet;
				if (newValue == oldValue)
					return true; // Another thread set the flags appropriately while we were spin-waiting, so we're done
				// If Interlocked.CompareExchange returns the old value, it means the flags were updated atomically.
				return Interlocked.CompareExchange(ref m_normFlags, newValue, oldValue) == oldValue;
			}, 60000);  // If 60 seconds pass without success, then give up on setting the flag so we don't get deadlocked
		}

		/// <summary>
		/// Given an ICU normalizer, enumerate the limit indices of the "segments" of this string.
		/// A "segment" is defined as a group of characters that interact with each other in this
		/// normalization, and which therefore can't be split apart and normalized separately without
		/// changing the result of the normalization. For example, under NFC, if LATIN SMALL LETTER C (U+0063)
		/// is followed by COMBINING CEDILLA (U+0327) which is followed by LATIN SMALL LETTER D (U+0064),
		/// then the c and cedilla will form one "segment": splitting them apart and normalizing them
		/// separately would produce a different result than normalizing them together. So this function
		/// would yield (among other values) the index of LATIN SMALL LETTER D, the first index that is
		/// not part of the segment (that is, the limit index).
		///
		/// The last index yielded by this function will be equal to the length of the string, and it
		/// will never yield the index 0. (If the string is empty, it will return an empty enumerable).
		/// Therefore, it is always safe to do GetChars(previousIndex, thisIndex) in a foreach loop to get
		/// the "current" segment (assuming previousIndex is set to 0 the first time through the loop).
		/// </summary>
		/// <param name="icuNormalizer">IntPtr to the ICU normalizer to use (get this from Icu.GetIcuNormalizer)</param>
		/// <returns>An enumerable of indexes into "this" TsString, at all the normalization "segment" boundaries, suitable for passing into GetChars(prevIdx, thisIdx)</returns>
		private IEnumerable<int> EnumerateSegmentLimits(IntPtr icuNormalizer)
		{
			if (String.IsNullOrEmpty(Text))
				yield break;
			int i = 0;
			while (i < Text.Length)
			{
				int codepoint = Char.ConvertToUtf32(Text, i);
				if (Icu.HasNormalizationBoundaryBefore(icuNormalizer, codepoint) && i > 0)
				{
					yield return i;
				}
				i += codepoint > 0xffff ? 2 : 1;
			}
			yield return Text.Length;
		}

		/// <summary>
		/// Helper function used in MatchUpIndexesAfterNormalization, itself a helper function for get_NormalizedForm
		/// </summary>
		/// <param name="s">String to convert to UTF32 codepoints. Must not be null, and must not end with an unpaired surrogate.</param>
		/// <returns>A sequence of KeyValuePairs where the key is the index (in chars) of this codepoint in the original string, and the value is the Unicode codepoint at that index.</returns>
		private List<KeyValuePair<int, int>> CodepointsByIndex(string s)
		{
			int len = s.Length;
			var result = new List<KeyValuePair<int, int>>(len);
			for (int i = 0; i < len; i++)
			{
				int codePoint = Char.ConvertToUtf32(s, i);
				result.Add(new KeyValuePair<int, int>(i, codePoint));
				if (codePoint > 0xffff)
					i++; // Skip second half of a surrogate pair
			}
			return result;
		}

		// Return value for MatchUpIndexesAfterNormalization helper function
		private struct RearrangedIndexMapping
		{
			public readonly int origIdx;
			public readonly int normIdx;
			public readonly bool isFirstCharOfDecomposition;

			public RearrangedIndexMapping(int orig, int norm, bool isFirstChar)
			{
				origIdx = orig;
				normIdx = norm;
				isFirstCharOfDecomposition = isFirstChar;
			}
		}

		/// <summary>
		/// Helper function for get_NormalizedFormAndFixOffsets below.
		/// Take indexes from original string segment, and figure out what indexes they correspond to in the
		/// corresponding segment of the decomposed output string. Also keep track of whether a given match
		/// is the *first* offset of the decomposed segment, because when fixing up offsets of selections,
		/// an offset that pointed to (say) LATIN SMALL LETTER U WITH HOOK should end up pointing to the
		/// decomposed LATIN SMALL LETTER U, and should never end up pointing to COMBINING HOOK ABOVE.
		/// Algorithm: decompose each codepoint of the original segment one at a time, and match it up with
		/// the codepoints of the normalized segment.
		/// </summary>
		/// <param name="segment">Segment of original string</param>
		/// <param name="normalizedSegment">Corresponding segment from normalized string</param>
		/// <param name="icuNormalizer">ICU normalizer that created the corresponding segment</param>
		/// <returns></returns>
		private IEnumerable<RearrangedIndexMapping> MatchUpIndexesAfterNormalization(string segment, string normalizedSegment, IntPtr icuNormalizer)
		{
			// We'll want to preserve (and later, return) the indexes of the *characters*, which won't
			// be the same as the indexes of the codepoints if there are any surrogate pairs involved.
			List<KeyValuePair<int, int>> origCodepointsByIndex = CodepointsByIndex(segment);
			List<KeyValuePair<int, int>> normCodepointsByIndex = CodepointsByIndex(normalizedSegment);
			var sentinel = new KeyValuePair<int, int>(-1, -1); // Value that can never match a real index/codepoint pair
			foreach (KeyValuePair<int, int> indexAndCodePoint in origCodepointsByIndex)
			{
				int origIdx = indexAndCodePoint.Key;
				int origCodePoint = indexAndCodePoint.Value;
				string normalizedStringFromOrigCodePoint = Icu.GetDecompositionFromUtf32(icuNormalizer, origCodePoint);
				foreach (KeyValuePair<int, int> indexAndResultingCodePoint in CodepointsByIndex(normalizedStringFromOrigCodePoint))
				{
					int resultingCodePoint = indexAndResultingCodePoint.Value;
					// Some algorithms (like fixing up offsets) care about finding the first character of the decomposition -- because if an
					// offset pointed to U-WITH-HOOK before NFD, we want that offset to end up pointing at the U, not at the combining hook.
					bool isFirstChar = indexAndResultingCodePoint.Key == 0;
					int i = normCodepointsByIndex.FindIndex(kv => kv.Value == resultingCodePoint);
					if (i < 0) // Should never happen, but let's guard against it anyway
						continue;
					// i is an index of *codepoints*. To properly match things up, we need a *character* index. Good thing we stored one!
					int matchingIdxInNormalizedSegment = normCodepointsByIndex[i].Key;
					normCodepointsByIndex[i] = sentinel; // Ensure we won't match this position ever again
					yield return new RearrangedIndexMapping(origIdx, matchingIdxInNormalizedSegment, isFirstChar);
				}
			}
		}

		/// <summary>
		/// Return an equivalent string in the specified normal form.
		/// This may be the same object as the recipient, if it is already in
		/// that normal form.
		/// Note that <see cref="TsString"/> instances normalized to NFC may not have text
		/// that is so normalized. This is because we don't collapse otherwise collapsible
		/// pairs if they have different style properties.
		/// </summary>
		public ITsString get_NormalizedForm(FwNormalizationMode nm)
		{
			return get_NormalizedFormAndFixOffsets(nm, null, 0);
		}

		/// <summary>
		/// Return an equivalent string in NFD.
		/// This may be the same object as the recipient, if it is already in that normal form.
		///
		/// The values pointed to by the array of offsets to fix are each offsets into
		/// the string. The code attempts to adjust them to corresponding offsets in the output
		/// string. An exact correspondence is not always achieved; if the offset is in the middle
		/// of a diacritic sequence, it may be moved to the start of the sequence's base character
		/// (or the start of the string).
		/// </summary>
		public void NfdAndFixOffsets(out ITsString ptssRet, ArrayPtr rgpichOffsetsToFix, int cichOffsetsToFix)
		{
			ptssRet = get_NormalizedFormAndFixOffsets(
				FwNormalizationMode.knmNFD,
				rgpichOffsetsToFix,
				cichOffsetsToFix);
		}

		// Implementation of both get_NormalizedForm and NfdAndFixOffsets
		private ITsString get_NormalizedFormAndFixOffsets(FwNormalizationMode nm, ArrayPtr oldOffsetsToFix, int numOffsetsToFix)
		{
			// Can we skip unnecessary work?
			if (IsAlreadyNormalized(nm))
				return this;
			if (string.IsNullOrEmpty(Text))
			{
				NoteAlreadyNormalized(nm);
				return this;
			}

			if (nm == FwNormalizationMode.knmLim)
				throw new ArgumentException("Normalization mode may not be knmLim", "nm");

			// NFSC needs to be decomposed first, then recomposed as NFC.
			if (nm == FwNormalizationMode.knmNFSC && !get_IsNormalizedForm(FwNormalizationMode.knmNFD))
			{
				var nfd = (TsString)get_NormalizedForm(FwNormalizationMode.knmNFD);
				// Line below is *not* a typo; this call will not recurse infinitely.
				return nfd.get_NormalizedFormAndFixOffsets(FwNormalizationMode.knmNFSC, oldOffsetsToFix, numOffsetsToFix);
			}

			bool willFixOffsets = numOffsetsToFix > 0 && oldOffsetsToFix != null && oldOffsetsToFix.IntPtr != IntPtr.Zero;
			// Keys = offsets into original string, values = offsets into normalized string
			var stringOffsetMapping = willFixOffsets ? new Dictionary<int, int>() : null; // Don't allocate an object if we'll never use it

			Icu.UNormalizationMode icuMode = (nm == FwNormalizationMode.knmNFSC) ? Icu.UNormalizationMode.UNORM_NFC : (Icu.UNormalizationMode)nm;
			IntPtr icuNormalizer = Icu.GetIcuNormalizer(icuMode);

			TsStrBldr resultBuilder = new TsStrBldr();
			int segmentMin = 0;
			foreach (int segmentLim in EnumerateSegmentLimits(icuNormalizer))
			{
				string segment = GetChars(segmentMin, segmentLim);
				string normalizedSegment = Icu.Normalize(segment, icuNormalizer);
				int curRun = get_RunAt(segmentMin);
				int curRunLim = get_LimOfRun(curRun);
				ITsTextProps curTextProps = get_Properties(curRun);
				if (curRunLim >= segmentLim)
				{
					// The segment is contained entirely in the current run, so our job is simple
					int outputLenSoFar = resultBuilder.Length;
					resultBuilder.Replace(outputLenSoFar, outputLenSoFar, normalizedSegment, curTextProps);
					// Calculate the orig -> norm index mappings if (and only if) they're needed, since this calculation is expensive
					if (willFixOffsets)
					{
						foreach (RearrangedIndexMapping mapping in MatchUpIndexesAfterNormalization(segment, normalizedSegment, icuNormalizer))
						{
							// Note that our local mapping is from the start of this segment, but we want to keep track of indexes from the start
							// of the *string*. (Both the original string and the output, normalized string). So we adjust the indexes here.
							if (mapping.isFirstCharOfDecomposition)
								stringOffsetMapping[segmentMin + mapping.origIdx] = outputLenSoFar + mapping.normIdx;
						}
					}
				}
				else
				{
					// The segment straddles two runs, so our job is harder. We have to either deal with decomposition
					// rearranging things (and make sure the right characters maintain the right text properties), or
					// else we have to deal with composition possibly trying to "compress" some diacritics that straddle
					// a run border (which can happen, for example, if they have different text properties).

					if (nm == FwNormalizationMode.knmNFD || nm == FwNormalizationMode.knmNFKD)
					{
						// Decomposition: we have to deal with rearranging. Some characters from after the first run's
						// endpoint may have ended up "inside" the first run after rearranging, so their text properties
						// will be incorrect at first. We'll fix them up after calculating the orig -> norm index mappings.

						int outputLenSoFar = resultBuilder.Length; // This will be the start index from which
						resultBuilder.Replace(outputLenSoFar, outputLenSoFar, normalizedSegment, curTextProps);

						// Now correct the text properties, one index at a time.
						IEnumerable<RearrangedIndexMapping> indexMappings = MatchUpIndexesAfterNormalization(segment, normalizedSegment, icuNormalizer);
						foreach (RearrangedIndexMapping mapping in indexMappings)
						{
							ITsTextProps origProperties = get_PropertiesAt(segmentMin + mapping.origIdx);
							int outputIdx = outputLenSoFar + mapping.normIdx;
							int size = Char.IsSurrogate(normalizedSegment, mapping.normIdx) ? 2 : 1;
							resultBuilder.SetProperties(outputIdx, outputIdx + size, origProperties);
							// And if we also need to fix up offsets at the end, we keep track of the ones we'll need
							if (willFixOffsets && mapping.isFirstCharOfDecomposition)
								stringOffsetMapping[segmentMin + mapping.origIdx] = outputLenSoFar + mapping.normIdx;
						}
					}

					else if (nm == FwNormalizationMode.knmNFSC)
					{
						// Composition that preserves styles. By this point, our input is NFD so we at least know there will be no rearranging.

						// If there is more than one character remaining in the current run, then we might be able to compose those, at least.
						if (curRunLim - segmentMin > 1)
						{
							// Unicode canonical ordering is such that any subsequence of a composed character can itself be composed, so this is safe.
							string remainderOfFirstRun = GetChars(segmentMin, curRunLim);
							string normalizedRemainder = Icu.Normalize(remainderOfFirstRun, icuNormalizer);
							resultBuilder.Replace(resultBuilder.Length, resultBuilder.Length, normalizedRemainder, curTextProps);
							// Now the start of the un-composable part is just the limit of the first run (which is the start of the second run).
							segmentMin = curRunLim;
						}
						// Now there could be any NUMBER of runs between currentInputIdx and segmentLim. Maybe there are TEN composing
						// characters, each with different text properties (and thus different runs). However, since the base character
						// was in the first run, none of the characters from the second or subsequent runs are composable any longer. So we
						// can copy them to the output as-is as one big TsString, which will carry text, runs and all.
						ITsString uncomposablePartOfSegment = GetSubstring(segmentMin, segmentLim);
						resultBuilder.ReplaceTsString(resultBuilder.Length, resultBuilder.Length, uncomposablePartOfSegment);
					}

					else
					{
						// For NFC and NFKC, we do not try to preserve styles or offset mappings, so this branch is quite simple
						int outputLenSoFar = resultBuilder.Length;
						resultBuilder.Replace(outputLenSoFar, outputLenSoFar, normalizedSegment, curTextProps);
					}
				}
				segmentMin = segmentLim; // Next segment will start where the current segment ended
			}
			if (willFixOffsets)
			{
				stringOffsetMapping[segmentMin] = resultBuilder.Length;
				int ptrSize = Marshal.SizeOf(typeof(IntPtr));
				for (int i = 0; i < numOffsetsToFix; i++)
				{
					IntPtr offsetPtr = Marshal.ReadIntPtr(oldOffsetsToFix.IntPtr, i * ptrSize);
					int oldOffset = Marshal.ReadInt32(offsetPtr);
					int newOffset;
					if (stringOffsetMapping.TryGetValue(oldOffset, out newOffset))
					{
						Marshal.WriteInt32(offsetPtr, newOffset);
					}
					else
					{
						// The only likely way for one of the offsets we've been asked to fix up to NOT
						// be found in the offset mapping dictionary is if it happened to be an offset
						// to the second half of a surrogate pair. In which case we want to fix it up to
						// point to wherever the first half of that pair ended up, so searching downwards
						// through the offset mapping dictionary will find the best match.
						bool found = false;
						while (!found && oldOffset > 0)
						{
							oldOffset--;
							found = stringOffsetMapping.TryGetValue(oldOffset, out newOffset);
						}
						// Any offset that could not be matched at all will be pointed at the beginning
						// of the TsString, since that's safe with strings of all sizes (including empty).
						Marshal.WriteInt32(offsetPtr, found ? newOffset : 0);
					}
				}
			}
			var result = (TsString)resultBuilder.GetString();
			result.NoteAlreadyNormalized(nm); // So we won't have to do all this work a second time
			return result;
		}

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		public bool Equals(TsString other)
		{
			return other != null && m_text == other.m_text && m_runs.SequenceEqual(other.m_runs);
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
		/// </summary>
		public override bool Equals(object obj)
		{
			var other = obj as TsString;
			return other != null && Equals(other);
		}

		/// <summary>
		/// Returns a hash code for this instance.
		/// </summary>
		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + m_text?.GetHashCode() ?? 0;
			code = code * 31 + m_runs.GetSequenceHashCode();
			return code;
		}

		/// <summary>
		/// Returns a <see cref="System.String" /> that represents this instance.
		/// </summary>
		public override string ToString()
		{
			return Text;
		}
	}
}
