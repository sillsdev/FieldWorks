using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;       // for Hashtable
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using Microsoft.Win32;                  // for RegistryKey
using System.Diagnostics;               // for Debug
using System.IO;                        // for file I/O
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters40
{
	/// <summary>
	/// Managed EncConverter for AdaptIt Knowledge Base data
	/// </summary>
	[GuidAttribute("2734A74B-6E88-40c4-8D0A-016A421D26CF")]
	// normally these subclasses are treated as the base class (i.e. the
	//  client can use them orthogonally as IEncConverter interface pointers
	//  so normally these individual subclasses would be invisible), but if
	//  we add 'ComVisible = false', then it doesn't get the registry
	//  'HKEY_CLASSES_ROOT\SilEncConverters40.TecEncConverter' which is the basis of
	//  how it is started (see EncConverters.AddEx).
	public class AdaptItEncConverter : AdaptItKBReader
	{
		public const string strDisplayName = "AdaptIt Knowledge Base Converter";
		public const string strHtmlFilename = "AdaptIt Plug-in About box.mht";

		protected const string chNeverUsedChar = "\u001f";  // add to words we replace, so we don't process them again

		public override string strRegValueForConfigProgId
		{
			get { return typeof(AdaptItEncConverterConfig).AssemblyQualifiedName; }
		}

		#region Initialization
		public AdaptItEncConverter() : base(typeof(AdaptItEncConverter).FullName,EncConverters.strTypeSILadaptit)
		{
		}

		public override void Initialize(string converterName, string converterSpec,
			ref string lhsEncodingID, ref string rhsEncodingID, ref ConvType conversionType,
			ref Int32 processTypeFlags, Int32 codePageInput, Int32 codePageOutput, bool bAdding)
		{
			base.Initialize(converterName, converterSpec, ref lhsEncodingID, ref rhsEncodingID, ref conversionType, ref processTypeFlags, codePageInput, codePageOutput, bAdding);

			if (bAdding)
			{
				// the only thing we want to add (now that the convType can be less than accurate)
				//  is to make sure it's bidirectional)
				if (EncConverters.IsUnidirectional(conversionType))
				{
					switch (conversionType)
					{
						case ConvType.Legacy_to_Legacy:
							conversionType = ConvType.Legacy_to_from_Legacy;
							break;
						case ConvType.Legacy_to_Unicode:
							conversionType = ConvType.Legacy_to_from_Unicode;
							break;
						case ConvType.Unicode_to_Legacy:
							conversionType = ConvType.Unicode_to_from_Legacy;
							break;
						case ConvType.Unicode_to_Unicode:
							conversionType = ConvType.Unicode_to_from_Unicode;
							break;
						default:
							break;
					}
				}
			}
		}
		#endregion Initialization

		#region Abstract Base Class Overrides
		[CLSCompliant(false)]
		protected override unsafe void DoConvert
			(
			byte* lpInBuffer,
			int nInLen,
			byte* lpOutBuffer,
			ref int rnOutLen
			)
		{
			// we need to put it *back* into a string for the lookup
			// [aside: I should probably override base.InternalConvertEx so I can avoid having the base
			//  class version turn the input string into a byte* for this call just so we can turn around
			//  and put it *back* into a string for our processing... but I like working with a known
			//  quantity and no other EncConverter does it that way. Besides, I'm afraid I'll break smtg ;-]
			byte[] baIn = new byte[nInLen];
			ECNormalizeData.ByteStarToByteArr(lpInBuffer, nInLen, baIn);
			Encoding enc;
			if (m_bLegacy)
			{
				try
				{
					enc = Encoding.GetEncoding(this.CodePageInput);
				}
				catch
				{
					enc = Encoding.GetEncoding(EncConverters.cnIso8859_1CodePage);
				}
			}
			else
				enc = Encoding.Unicode;

			char[] caIn = enc.GetChars(baIn);

			// here's our input string
			string strInput = new string(caIn);

			List<string> lstInputTokens, lstInputInBetweenTokens, lstOutputTokens, lstOutputInBetweenTokens;
			SplitAndConvert(strInput, out lstInputTokens, out lstInputInBetweenTokens,
				out lstOutputTokens, out lstOutputInBetweenTokens);

			// when we're finally done with all the replacements possible, build up a new output string of the
			//  results (removing any possible "never used" chars that might have been added in AdjustLists)
			string strOutput = null;
			int i;
			for (i = 0; i < lstOutputTokens.Count; i++)
				strOutput += lstOutputInBetweenTokens[i] + lstOutputTokens[i];
			strOutput += lstOutputInBetweenTokens[i];

			StringToProperByteStar(strOutput, lpOutBuffer, ref rnOutLen);
		}

		public void SplitAndConvert(string strInput,
			out List<string> lstInputTokens, out List<string> lstInputInBetweenTokens,
			out List<string> lstOutputTokens, out List<string> lstOutputInBetweenTokens)
		{
			// Here's a problem: if the user wants to go reverse, the AdaptIt KB file doesn't have a multi-word
			//  phrase maps for the reverse direction. So although this is kind of "brute force", I'm at a loss
			//  for a better way to do this. So, if this is reverse, then go thru all of the maps and create a
			//  reversal index system (e.g. from map=3, we might find outputs of map=1 and vise versa)
			string[] astrTokens;
			if (m_bReverseLookup && (m_mapOfReversalMaps == null))
			{
				m_mapOfReversalMaps = new Dictionary<int, Dictionary<string, string>>();
				foreach (Dictionary<string, string> map in m_mapOfMaps.Values)
				{
					foreach (KeyValuePair<string, string> kvp in map)
					{
						// find out how many words are in the value portion (this becomes the maps= value)
						//  e.g. if the forward direction key, "ND", had a value of "New Delhi", then in the
						//  reversal map, this would be a two-word phrase, "New Delhi" as the key with a value
						//  of "ND"
						astrTokens = kvp.Value.Split(caSplitChars, StringSplitOptions.RemoveEmptyEntries);

						// see if the map of reversal maps has a map for phrases of this number of words
						string strKey = kvp.Key;
						Dictionary<string, string> mapReverseLookup;
						if (!m_mapOfReversalMaps.TryGetValue(astrTokens.Length, out mapReverseLookup))
						{
							// map didn't exist, so create it now
							mapReverseLookup = new Dictionary<string, string>();
							m_mapOfReversalMaps.Add(astrTokens.Length, mapReverseLookup);
						}

						// if it did exist, then see if we already have a value with that key
						else if (mapReverseLookup.TryGetValue(kvp.Value, out strKey))
						{
							// this means that we already have at least two ambiguities. See if it's more than 1.
							if (strKey[0] == '%')
							{
								// means it's something like: "%11%<value1>%<value2>%...%<value11>%"
								int nAmbValueLength = strKey.IndexOf('%', 1) - 1;
								int nAmbCount = 0;
								try
								{
									nAmbCount = System.Convert.ToInt32(strKey.Substring(1, nAmbValueLength));
								}
								catch { }

								// add the new value
								int nLen = strKey.Length - (nAmbValueLength + 2) - 1;
								string strNew = strKey.Substring(nAmbValueLength + 2, nLen);
								strKey = String.Format("%{0}%{1}%{2}%", nAmbCount + 1, strNew, kvp.Key);
							}
							else
							{
								// this means that we only had one and this new one makes two
								strKey = String.Format("%2%{0}%{1}%", strKey, kvp.Key);
							}

							// remove it so the add below doesn't throw up
							mapReverseLookup.Remove(kvp.Value);
						}
						else
							strKey = kvp.Key;

						// add this new value as the key and the key (which might contain ambiguities) as the new value
						mapReverseLookup.Add(kvp.Value, strKey);
					}
				}
			}

			// First, get the list of characters that we'll use to trim (different for forwards vs. reverse)
			// AdaptIt doesn't use the punctuation to decide where to do the split, it uses only whitespace.
			//  The punctuation, then, is just used to *trim* the outside edges of the string (so, for example,
			//  two words with a hyphen in between will be treated by AdaptIt as a single word... So I have to
			//  do the same here or we won't find what's in the knowledge base).
			char[] achTrimCharsIn, achTrimCharsOut;
			if (!m_bReverseLookup & (m_caDelimitersForward != null))
			{
				achTrimCharsIn = m_caDelimitersForward;
				achTrimCharsOut = m_caDelimitersReverse;
			}
			else if (m_bReverseLookup & (m_caDelimitersReverse != null))
			{
				achTrimCharsIn = m_caDelimitersReverse;
				achTrimCharsOut = m_caDelimitersForward;
			}
			else
			{
				achTrimCharsIn = caSplitChars;
				achTrimCharsOut = caSplitChars;
			}

			// if the input string is multi-word, then our convention will be to look for the longest phrase
			// possible first.
			// AdaptIt has upto 10 word phrases in different "Maps". We have loaded these into a map of maps
			//  with the phrase length as the key ("1" corresponds to a phrase of one word, "2" corresponds
			//  to a phrase of 2 words, and so on). So starting at the longest phrase length first, get the map
			//  and process the input string with that first...
			// We have to be careful, though, because we might replace some phrase of the input string while
			//  other portions of it won't be converted until we are working on the smaller phrase maps. This
			//  means that output string might be processed multiple times and we have to make sure that an
			//  already replaced phrase doesn't get replaced again with a smaller phrase (not likely, but a
			//  theoretical possibility since we're processing the string multiple times. To make sure we don't
			//  I put a "never used" character in the replaced strings (i.e. 0x001f), which should never
			//  occur in actual data. This will cause the lookup to fail on subsequent checks for already
			//  replaced words. Then at the end, before returning the output string, we strip out any occurrances
			//  of the "never used" character.
			// Finally, to make the processing (and algorithm) easier, first split the input string into tokens of
			//  actual non-punctuation and non-white-space (i.e. bonefide words involving word-forming
			//  characters). Create an array of the in-between stuff as well so we can keep that stuff intact.
			// split the input into tokens (which as mentioned above, means words with spaces in between)
			astrTokens = strInput.Split(caSplitChars, StringSplitOptions.RemoveEmptyEntries);

			// go thru the tokens and put them in lists of a) tokens and b) stuff in between.
			// if there are x tokens, then there are x+1 things before, inbetween, and after (some of which might
			//  be just a space or nothing)
			int nStartIndex = 0;
			int nWordIndex;
			string strInBetween;
			lstInputTokens = new List<string>(astrTokens.Length);
			lstOutputTokens = new List<string>(astrTokens.Length);
			lstInputInBetweenTokens = new List<string>(astrTokens.Length + 1);
			lstOutputInBetweenTokens = new List<string>(astrTokens.Length + 1);
			for (nWordIndex = 0; nWordIndex < astrTokens.Length; nWordIndex++)
			{
				string strAdd = astrTokens[nWordIndex].Trim(achTrimCharsIn);
				lstInputTokens.Add(strAdd);
				lstOutputTokens.Add(strAdd);    // put a place holder here (which gets replaced in AdjustLists below)
				int nIndex = strInput.IndexOf(lstInputTokens[nWordIndex], nStartIndex, StringComparison.Ordinal);
				System.Diagnostics.Debug.Assert(nIndex >= 0);
				int nLength = nIndex - nStartIndex;
				strInBetween = strInput.Substring(nStartIndex, nLength);
				lstInputInBetweenTokens.Add(strInBetween);
				lstOutputInBetweenTokens.Add(ReplacePunctuation(strInBetween, achTrimCharsIn, achTrimCharsOut));
				nStartIndex = nIndex + lstInputTokens[nWordIndex].Length;
			}
			strInBetween = strInput.Substring(nStartIndex);
			lstInputInBetweenTokens.Add(strInBetween);
			lstOutputInBetweenTokens.Add(ReplacePunctuation(strInBetween, achTrimCharsIn, achTrimCharsOut));

#if DEBUG
			// as a test (debug configuration only), make sure the input is fully captured in the lists
			//  by rebuilding the string and make sure it's the same (i.e. we haven't lost anything)
			string strTest = null;
			for (nWordIndex = 0; nWordIndex < lstInputTokens.Count; nWordIndex++)
				strTest += lstInputInBetweenTokens[nWordIndex] + lstInputTokens[nWordIndex];
			strTest += lstInputInBetweenTokens[nWordIndex];
			System.Diagnostics.Debug.Assert(strTest == strInput);
#endif

			// going thru the longest maps first (but only upto the number of words in the phrase
			Dictionary<string, string> mapLookup;
			Dictionary<int, Dictionary<string, string>> mapOfMaps = (m_bReverseLookup) ? m_mapOfReversalMaps : m_mapOfMaps;
			for (int nMapNumber = Math.Min(lstInputTokens.Count, 10); nMapNumber > 0; nMapNumber--)
			{
				// see if we have a map for phrases of nMapNumber length
				if (!mapOfMaps.TryGetValue(nMapNumber, out mapLookup))
					continue;   // skip the rest if not

				// go thru the words by chunks...
				for (nWordIndex = 0; nWordIndex <= (lstInputTokens.Count - nMapNumber); nWordIndex++)
				{
					// the string to search for might be composed of multiple tokens (e.g. if we're doing kb.map=2)
					string strSearchToken = lstInputTokens[nWordIndex];
					if (nMapNumber > 1)
					{
						int nWordsInPhrase = 1;

						// sorry, I don't usually write such sloppy code, but I can't work out how this could
					//  be done better.
					// If there is something (besides a space) in between the two words that I'm about to
					//  join together, then they can't be a phrase (presumably), so in that case, we
					//  know we won't find a match and can just continue with the next word (continue;)
					// but the code here wants to be a "for" (or "while") loop, but if I make it a "for" loop,
					//  then the "continue;" thinks I want to continue with that... so I'm making a poor man's
					//  "for" loop with a label and a goto below...
					DoNextWord:
						if (lstInputInBetweenTokens[nWordIndex + nWordsInPhrase] != " ")
						{
							// this can't be part of a phrase, so just try the next word as the possible start
							//  of a phrase
							continue;
						}

						// otherwise, build up the phrase to search for out of multiple tokens
						strSearchToken += ' ' + lstInputTokens[nWordIndex + nWordsInPhrase];
						if (++nWordsInPhrase < nMapNumber)
							goto DoNextWord;
					}

					// now see if we have a replacement
					System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(strSearchToken));
					string strLookedup;
					if (mapLookup.TryGetValue(strSearchToken, out strLookedup))
					{
						// capture the converted word here. The multiple tokens of a multi-word search phrase,
						//  however, may be replaced by a single word, so we have to adjust the lists to account
						//  for this.
						AdjustLists(strSearchToken, strLookedup, nMapNumber, nWordIndex,
							ref lstInputTokens, ref lstOutputTokens,
							ref lstInputInBetweenTokens, ref lstOutputInBetweenTokens);
					}
				}
			}

			// finally, before we return, let's remove the "never used" character from the output strings
			for (nWordIndex = 0; nWordIndex < lstOutputTokens.Count; nWordIndex++)
				lstOutputTokens[nWordIndex] = lstOutputTokens[nWordIndex].Replace(chNeverUsedChar, null);
		}

		// if we've replaced multiple words as a phrase, then we need to collapse the lists
		//  nWordIndex tells us which word we were on (0-based)
		//  nNumWords tells us how many words were in the found phrase
		protected void AdjustLists(string strSearchToken, string strReplacement, int nNumWords, int nWordIndex,
			ref List<string> lstInputTokens, ref List<string> lstOutputTokens,
			ref List<string> lstInputInBetweenTokens, ref List<string> lstOutputInBetweenTokens)
		{
			int nNumToClear = nNumWords;
			while (--nNumToClear > 0)
			{
				lstInputTokens.RemoveAt(nWordIndex);
				lstOutputTokens.RemoveAt(nWordIndex);
				lstInputInBetweenTokens.RemoveAt(nWordIndex + 1);
				lstOutputInBetweenTokens.RemoveAt(nWordIndex + 1);
			}

			// since we're replacing a string, we don't want this word to potentially be replaced again, so
			//  add the "never used" character here so that any future searching will always fail (but be sure
			//  to remove these before returning to the caller.
			lstInputTokens[nWordIndex] = strSearchToken;
			lstOutputTokens[nWordIndex] = chNeverUsedChar + strReplacement;
		}

		protected string ReplacePunctuation(string strInput, char[] achInput, char[] achOutput)
		{
			for (int i = 0; i < achInput.Length; i++)
			{
				char chIn = achInput[i];
				char chOut = achOutput[i];
				strInput = strInput.Replace(chIn, chOut);
			}
			return strInput;
		}

		// this is my stab at rewriting ContainsValue to handle our special case of possible ambiguities
		protected string CheckInHashtableValues(string strInput, Dictionary<string, string> mapLookup)
		{
			if( String.IsNullOrEmpty(strInput) )  // don't bother if it's uninteresting
				return null;

			foreach (KeyValuePair<string,string> kvp in mapLookup)
			{
				string strValue = kvp.Value;

				// check for the one-to-one case first
				// we might *not* want to just 'return' if the reverse is supposed to generate
				// %count%... also... but until someone complains...
				if( strInput == strValue )
					return kvp.Key;

				// next check to see if we have multiple values whether the input string is one of them
				if((strValue[0] == '%')
					&&  (strValue.IndexOf( String.Format("%{0}%", strInput) ) != -1) )
					return kvp.Key;
			}

			return null;
		}

		[CLSCompliant(false)]
		protected unsafe void StringToProperByteStar(string strOutput, byte* lpOutBuffer, ref int rnOutLen)
		{
			// if the output is legacy, then we need to shrink it from wide to narrow
			if( m_bLegacy )
			{
				byte[] baOut = EncConverters.GetBytesFromEncoding(CodePageOutput, strOutput, true);

				if( baOut.Length > rnOutLen )
					EncConverters.ThrowError(ErrStatus.OutputBufferFull);
				rnOutLen = baOut.Length;
				ECNormalizeData.ByteArrToByteStar(baOut,lpOutBuffer);
			}
			else
			{
				int nLen = strOutput.Length * 2;
				if( nLen > (int)rnOutLen )
					EncConverters.ThrowError(ErrStatus.OutputBufferFull);
				rnOutLen = nLen;
				ECNormalizeData.StringToByteStar(strOutput,lpOutBuffer,rnOutLen);
			}
		}
		#endregion Abstract Base Class Overrides
	}
}
