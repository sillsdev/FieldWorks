// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2004' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StringUtils.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices; // needed for Marshal
using System.Xml;
using System.Text.RegularExpressions;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using System.Resources;
using System.IO;
using System.Globalization;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// StringUtils is a collection of static methods for working with strings, characters, and
	/// TS strings.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class StringUtils
	{
		/// <summary>Character is interpreted as object</summary>
		public static readonly char kchObject = '\uFFFC';
		/// <summary>Character is interpreted as object</summary>
		public static readonly char kchReplacement = '\uFFFD';
		/// <summary>String is interpreted as object</summary>
		public static readonly string kszObject = "\uFFFC";
		/// <summary>Regular expression used to check whether a string appears to be a URL (as
		/// opposed to a file path)</summary>
		public static readonly Regex kRegexUrl = new Regex("^[a-z]+://", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// List of the ranges that are acceptable
		private static int[][] s_customPuaRanges = new int[][]
		{
			new int[] {0xE000,0xEFFF},
			new int[] {0xF0000,0xFFFFD}
		};

		#region Handling ORCs
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert (or replace) an ORC run in a string builder.
		/// </summary>
		/// <param name="guid">The GUID representing the embedded object</param>
		/// <param name="objDataType">The type of embedding</param>
		/// <param name="tsStrBldr">The TS string builder</param>
		/// <param name="ichMin">The position at which the object is to be inserted</param>
		/// <param name="ichLim">If replacing an existing ORC (or other text), this is the Lim
		/// position (otherwise, should be set equal to ichMin)</param>
		/// <param name="ws">The ID of the writing system to use for the inserted run, or 0
		/// to leave unspecified</param>
		/// ------------------------------------------------------------------------------------
		public static void InsertOrcIntoPara(Guid guid, FwObjDataTypes objDataType,
			ITsStrBldr tsStrBldr, int ichMin, int ichLim, int ws)
		{
			byte[] objData = MiscUtils.GetObjData(guid, (byte)objDataType);
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValueRgch((int)FwTextPropType.ktptObjData,
				objData, objData.Length);
			if (ws != 0)
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, ws);
			tsStrBldr.Replace(ichMin, ichLim, new string(kchObject, 1),
				propsBldr.GetTextProps());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an object replacement character from a Guid
		/// </summary>
		/// <param name="guid">guid that maps to an ORC</param>
		/// <param name="type">type of ORC to return within the TsString</param>
		/// <param name="ws">The writing system.</param>
		/// <returns>ITsString</returns>
		/// ------------------------------------------------------------------------------------
		public static ITsString CreateOrcFromGuid(Guid guid, FwObjDataTypes type, int ws)
		{
			ITsStrBldr tsStrBldr = TsStrBldrClass.Create();
			byte[] objData = MiscUtils.GetObjData(guid, (byte)type);
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValueRgch((int)FwTextPropType.ktptObjData,
				objData, objData.Length);

			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, ws);
			tsStrBldr.Replace(0, 0, new string(kchObject, 1), propsBldr.GetTextProps());
			return tsStrBldr.GetString();
		}
		#endregion

		/// ---------------------------------L---------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified character is defined in ICU.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsCharacterDefined(string chr)
		{
			if (string.IsNullOrEmpty(chr))
				return false;

			// Go through the codepoints in the character.
			foreach (char c in chr)
			{
				if (!IsCodePointDefined((int)c))
					return false;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified codepoint is defined in ICU. If it is and
		/// its a custom PUA character, then make sure the language definition's PUA character
		/// collection is updated. Return false if the codepoint is not defined in ICU.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static bool IsCodePointDefined(int codepoint)
		{
			PUACharacter puaChar = new PUACharacter(codepoint);
			try
			{
				// If the definition doesn't exist in ICU or the character has no name then
				// it's undefined and shouldn't be allowed in the valid characters list.
				if (!puaChar.RefreshFromIcu(false) || puaChar.Name.Length == 0)
					return false;

				// If the ICU category is one of the "Other" categories, then it's
				// undefined and shouldn't be allowed in the valid characters list.
				string ucdrep = puaChar.GeneralCategory.UcdRepresentation;
				ucdrep = ucdrep.ToUpperInvariant();

				// TODO: When tabs are supported, then allow them (they are in category Cc).
				// See TE-3004.
				if (ucdrep[0] == 'C' && ucdrep[1] != 'F')
					return false;
			}
			catch
			{
				return false;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the specified string into a list of characters. The unparsed list is a
		/// string of valid characters delimited with the specified delimiter.
		/// </summary>
		/// <param name="chars">The string containing a delimited list of characters.</param>
		/// <param name="delimiter">The delimiter (passed as a string, but really just a single
		/// character).</param>
		/// <param name="langDef">The language definition which could override some of the
		/// properties defined by the <paramref name="cpe"/>.</param>
		/// <param name="cpe">The character property engine.</param>
		/// <param name="invalidCharacters">The invalid characters.</param>
		/// <returns>List of unique characters</returns>
		/// ------------------------------------------------------------------------------------
		public static List<string> ParseCharString(string chars, string delimiter,
			LanguageDefinition langDef, ILgCharacterPropertyEngine cpe, out List<string> invalidCharacters)
		{
			invalidCharacters = new List<string>();
			return ParseCharString(chars, delimiter, langDef, cpe, invalidCharacters);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the specified string into a list of characters, ignoring any bogus characters
		/// (digraphs, undefined Unicode characters, lone diacritics, etc.). The unparsed list
		/// is a string of valid characters delimited with the specified delimiter.
		/// </summary>
		/// <param name="chars">The string containing a delimited list of characters.</param>
		/// <param name="delimiter">The delimiter (passed as a string, but really just a single
		/// character).</param>
		/// <param name="cpe">The character property engine.</param>
		/// ------------------------------------------------------------------------------------
		public static List<string> ParseCharString(string chars, string delimiter,
			ILgCharacterPropertyEngine cpe)
		{
			return ParseCharString(chars, delimiter, null, cpe, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the specified string into a list of characters. The unparsed list is a
		/// string of valid characters delimited with the specified delimiter.
		/// </summary>
		/// <param name="chars">The string containing a delimited list of characters.</param>
		/// <param name="delimiter">The delimiter (passed as a string, but really just a single
		/// character).</param>
		/// <param name="langDef">The language definition which could override some of the
		/// properties defined by the <paramref name="cpe"/>.</param>
		/// <param name="cpe">The character property engine.</param>
		/// <param name="invalidCharacters">List of bogus characters (digraphs, undefined
		/// Unicode characters, lone diacritics, etc.)encountered. if set to <c>null</c> ignores
		/// bogus characters.</param>
		/// <returns>List of unique characters</returns>
		/// ------------------------------------------------------------------------------------
		private static List<string> ParseCharString(string chars, string delimiter,
			LanguageDefinition langDef, ILgCharacterPropertyEngine cpe,
			List<string> invalidCharacters)
		{
			if (string.IsNullOrEmpty(chars))
				return new List<string>();

			if (string.IsNullOrEmpty(delimiter))
				delimiter = " ";

			string[] charsArray = chars.Split(delimiter.ToCharArray(),
				StringSplitOptions.RemoveEmptyEntries);

			List<string> charsList = new List<string>(charsArray.Length);

			foreach (string chr in charsArray)
			{
				if (IsValidChar(chr, langDef, cpe))
				{
					if (!charsList.Contains(chr))
						charsList.Add(chr);
				}
				else if (invalidCharacters != null)
				{
					invalidCharacters.Add(chr);
				}
			}

			// If the original list of characters started with a space and that was the delimiter,
			// then make sure to add it back into the list because it will have been lost by the
			// Split above
			if (delimiter == " " && chars[0] == delimiter[0])
				charsList.Insert(0, delimiter);

			return charsList;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified string is a valid string consisting of exactly one
		/// initial base character followed by zero or more legally placed combining marks.
		/// </summary>
		/// <param name="chr">The string to check.</param>
		/// <param name="langDef">The language definition which could override some of the
		/// properties defined by the <paramref name="cpe"/>.</param>
		/// <param name="cpe">The character property engine.</param>
		/// ------------------------------------------------------------------------------------
		public static bool IsValidChar(string chr, LanguageDefinition langDef,
			ILgCharacterPropertyEngine cpe)
		{
			// We need to decompose the results in order to check for equality because
			// it is possible (Korean is the only example of this we know of) for multiple base
			// letters to compose into a single base letter. In that case,
			// ValidateCharacterSequence returns the composed character because we don't really
			// want to store the decomposed sequence as a valid character since the characters
			// check always operates on individual base characters. (To make that check work
			// properly for Korean, if the valid characters list contains the composed
			// characters, the check would need to account for this and try to compose the data
			// being checked.)
			string ch = ValidateCharacterSequence(chr, langDef, cpe);
			return (ch.Length == 0) ? false : (cpe.NormalizeD(ch) == cpe.NormalizeD(chr));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the first base character/combining diacritic combination and removes any
		/// remaining characters.
		/// </summary>
		/// <param name="origChars">The original string of characters.</param>
		/// <param name="langDef">The language definition which could override some of the
		/// properties defined by the <paramref name="cpe"/>.</param>
		/// <param name="cpe">The character property engine.</param>
		/// ------------------------------------------------------------------------------------
		public static string ValidateCharacterSequence(string origChars,
			LanguageDefinition langDef, ILgCharacterPropertyEngine cpe)
		{
			// Allow spaces (Zs), hard line breaks (Zl), and other formatting characters (Cf) in
			// isolation only.
			if (origChars.Length == 1)
			{
				string category = (langDef == null) ? null : langDef.GetOverrideCharCategory(origChars[0]);
				switch (category)
				{
					case "Zl":
					case "Zs":
					case "Cf": return origChars;
				}
				if (category == null &&
					(cpe.get_GeneralCategory(origChars[0]) == LgGeneralCharCategory.kccZl ||
					cpe.get_GeneralCategory(origChars[0]) == LgGeneralCharCategory.kccZs ||
					cpe.get_GeneralCategory(origChars[0]) == LgGeneralCharCategory.kccCf))
				{
					return origChars;
				}
			}

			StringBuilder newChars = new StringBuilder();
			bool baseFound = false;
			bool fPrecedingCharWasMark = false;
			// Extract first base character and any following diacritics
			for (int ich = 0; ich < origChars.Length; ich++ )
			{
				char chr = origChars[ich];

				if (!baseFound)
				{
					string category = (langDef == null) ? null : langDef.GetOverrideCharCategory(chr);
					if (category == null)
					{
						// If this is not a valid base character, keep looking.
						if (!cpe.get_IsLetter(chr) && !cpe.get_IsNumber(chr) &&
							cpe.get_GeneralCategory(chr) != LgGeneralCharCategory.kccCo &&
							!cpe.get_IsPunctuation(chr) && !cpe.get_IsSymbol(chr))
							continue;
					}
					else
					{
						// If this is not a valid base character, keep looking.
						if (category[0] != 'L' && category[0] != 'N' &&
							category != "Co" && category[0] != 'P' && category[0] != 'S')
							continue;
					}

					baseFound = true;
				}
				else
				{
					// If this is not a diacritic or a ZWJ or ZWNJ between diacritics,
					// discard the rest of the string.
					if (IsMark(chr, langDef, cpe))
					{
						fPrecedingCharWasMark = true;
					}
					else if ((chr == '\u200C' || chr == '\u200D') && fPrecedingCharWasMark &&
						origChars.Length > ich + 1 && IsMark(origChars[ich + 1], langDef, cpe))
					{
						fPrecedingCharWasMark = false;
					}
					else
					{
						// This handles special situations like Korean, where multiple base letters
						// (representing phonemes) can compose into a single base letter (representing a
						// syllable).
						string composed = Icu.Normalize(origChars, Icu.UNormalizationMode.UNORM_NFKC);
						if (composed.Length == 1)
							return composed;
						break;
					}
				}

				if (baseFound)
					newChars.Append(chr);
			}

			return newChars.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified character is a mark (either as defined by the
		/// character property engine or as overridden by the language definition.
		/// </summary>
		/// <param name="chr">The character.</param>
		/// <param name="langDef">The language definition (can be null).</param>
		/// <param name="cpe">The character property engine.</param>
		/// ------------------------------------------------------------------------------------
		private static bool IsMark(char chr, LanguageDefinition langDef, ILgCharacterPropertyEngine cpe)
		{
			string category = (langDef == null) ? null : langDef.GetOverrideCharCategory(chr);
			return ((category == null && cpe.get_IsMark(chr)) || (category != null && category[0] == 'M'));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given character is used to end a sentence.
		/// </summary>
		/// <param name="ch">The character.</param>
		/// <param name="cc">The general character category.</param>
		/// <returns>
		/// 	<c>true</c> if the character ends a sentence; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsEndOfSentenceChar(int ch, LgGeneralCharCategory cc)
		{
			// The preliminary check of cc is just for efficiency. All these characters have this property.
			return (cc == LgGeneralCharCategory.kccPo &&
					(
						ch == 0x0021 || // EXCLAMATION MARK
						ch == 0x002E || // FULL STOP
						ch == 0x003F || // QUESTION MARK
						ch == 0x055C || // ARMENIAN EXCLAMATION MARK
						ch == 0x055E || // ARMENIAN QUESTION MARK
						ch == 0x0589 || // ARMENIAN FULL STOP
						ch == 0x061F || // ARABIC QUESTION MARK
						ch == 0x06D4 || // ARABIC FULL STOP
						ch == 0x0700 || // SYRIAC END OF PARAGRAPH
						ch == 0x0701 || // SYRIAC SUPRALINEAR FULL STOP
						ch == 0x0702 || // SYRIAC SUBLINEAR FULL STOP
						ch == 0x0964 || // DEVANAGARI DANDA
						ch == 0x0965 || // DEVANAGARI DOUBLE DANDA
						ch == 0x104A || // MYANMAR SIGN LITTLE SECTION
						ch == 0x104B || // MYANMAR SIGN SECTION
						ch == 0x1362 || // ETHIOPIC FULL STOP
						ch == 0x1367 || // ETHIOPIC QUESTION MARK
						ch == 0x1368 || // ETHIOPIC PARAGRAPH SEPARATOR
						ch == 0x166E || // CANADIAN SYLLABICS FULL STOP
						ch == 0x1803 || // MONGOLIAN FULL STOP
						ch == 0x1809 || // MONGOLIAN MANCHU FULL STOP
						ch == 0x1944 || // LIMBU EXCLAMATION MARK
						ch == 0x1945 || // LIMBU QUESTION MARK
						ch == 0x203C || // DOUBLE EXCLAMATION MARK
						ch == 0x203D || // INTERROBANG
						ch == 0x2047 || // DOUBLE QUESTION MARK
						ch == 0x2048 || // QUESTION EXCLAMATION MARK
						ch == 0x2049 || // EXCLAMATION QUESTION MARK
						ch == 0x3002 || // IDEOGRAPHIC FULL STOP
						ch == 0xFE52 || // SMALL FULL STOP
						ch == 0xFE56 || // SMALL QUESTION MARK
						ch == 0xFE57 || // SMALL EXCLAMATION MARK
						ch == 0xFF01 || // FULLWIDTH EXCLAMATION MARK
						ch == 0xFF0E || // FULLWIDTH FULL STOP
						ch == 0xFF1F || // FULLWIDTH QUESTION MARK
						ch == 0xFF61)) || // HALFWIDTH IDEOGRAPHIC FULL STOP
				// Except this is not a normal punctuation character.
				   ch == 0x00A7; // SECTION SIGN (used for forced segment breaks w/o punctuation)
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the characters in the Private Use Area.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void UpdatePUACollection(LanguageDefinition langDef, List<string> chars)
		{
			List<PUACharacter> customPuaCharacters = GetDefinedCustomPUACharsFromICU();

			// Remove any PUA characters that are no longer in the character list. If there
			// are any new PUA characters in the valid characters list, they
			// will be added to this collection in IsCodePointDefined().
			int codePoint;
			string data;
			for (int i = langDef.PuaDefinitionCount - 1; i >= 0; i--)
			{
				langDef.GetPuaDefinition(i, out codePoint, out data);
				char character = (char)new PUACharacter(codePoint).Character;

				string category = langDef.GetOverrideCharCategory(character);
				if (!chars.Contains(character.ToString()) && (category == null || category[0] != 'M'))
				{
					langDef.RemovePuaDefinition(new CharDef(codePoint, data));
				}
			}

			// Now make sure all the custom PUA characters specified in the valid characters
			// list are also added to the language definition's PUA collection.
			foreach (string chr in chars)
			{
				// Go through the codepoints in the character.
				foreach (char c in chr)
					UpdateLangDefPUACollection((int)c, langDef, customPuaCharacters);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes sure that if the specified codepoint is a custom PUA character, that that
		/// character is added to the language definition's PUA collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void UpdateLangDefPUACollection(int codepoint,
			LanguageDefinition langDef, List<PUACharacter> customPuaCharacters)
		{
			// Go through all the custom defined PUA characters in ICU and make sure that if
			// our codepoint is one of them, the language definition contains our codepoint
			// in its collection of PUA characters.
			foreach (PUACharacter customChar in customPuaCharacters)
			{
				string sCodePoint = codepoint.ToString("x4").ToUpperInvariant();

				// Is our codepoint one of the custom PUA characters? If so,
				// add it to the language definition's PUA collection.
				if (sCodePoint == customChar.CodePoint)
				{
					PUACharacter puaChar = new PUACharacter(sCodePoint);
					puaChar.RefreshFromIcu(false);
					langDef.AddPuaDefinition(codepoint, puaChar.ToString());
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a list of the user defined custom characters in ICU by scanning the Unicode data file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static List<PUACharacter> GetDefinedCustomPUACharsFromICU()
		{
			List<PUACharacter> definedChars = new List<PUACharacter>();

			string uniData = DirectoryFinder.GetIcuDirectory; // "c:\\work\\fw60\\distfiles\\Icu40\\icudt40l\\"
			uniData = uniData.Substring(0, uniData.LastIndexOf(@"\icudt"));
			uniData = Path.Combine(uniData, @"data\unidata\UnicodeData.txt");
			StreamReader reader = new StreamReader(uniData, System.Text.Encoding.ASCII);
			int chT = reader.Peek();	// force autodetection of encoding.
			try
			{
				string line;
				while((line = reader.ReadLine()) != null)
				{
					// skip entirely blank lines
					if(line.Length <= 0)
						continue;

					if (line.IndexOf("User Added") != -1)
					{
						PUACharacter puaChar = new PUACharacter("0000");
						string codepoint = line.Substring(0, line.IndexOf(';')).Trim();
						puaChar.CodePoint = codepoint;
						puaChar.RefreshFromIcu(false);
						definedChars.Add(puaChar);
					}
				}
			}
			finally
			{
				reader.Close();
			}

			// REVIEW (TimS/EberhardB): We don't think we should call GC.Collect().
			// If there seems to be a need for it, then we probably need to call
			// Dispose() on one of the variables we want disposed.
			GC.Collect();

			return definedChars;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string an NFC (normalized form, composed) form of a Unicode string.
		/// </summary>
		/// <param name="source">String to compose.</param>
		/// <returns>String in NFC form.</returns>
		/// ------------------------------------------------------------------------------------
		public static string Compose(string source)
		{
			// If the string passed in is not "valid", spit it back out.
			if (source == null || source == String.Empty)
				return source;

			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			tssBldr.Replace(0, 0, source, null);
			ITsString tss =
				tssBldr.GetString().get_NormalizedForm(FwNormalizationMode.knmNFC);
			return tss.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string an NFD (normalized form, decomposed) form of a Unicode string.
		/// Enhance JohnT: This is not a very efficient implementation; we should be able
		/// to use some ICU method directly (as the ITsString implementation does). Fix if ever
		/// used for something high-performance!
		/// </summary>
		/// <param name="source">String to compose.</param>
		/// <returns>String in NFD form.</returns>
		/// ------------------------------------------------------------------------------------
		public static string NormalizeNfd(string source)
		{
			// If the string passed in is not "valid", spit it back out.
			if (source == null || source == String.Empty)
				return source;

			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			tssBldr.Replace(0, 0, source, null);
			ITsString tss =
				tssBldr.GetString().get_NormalizedForm(FwNormalizationMode.knmNFD);
			return tss.Text;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an owned Guid from the given run in a structured text string.
		/// </summary>
		/// <param name="tss">given structured text string</param>
		/// <param name="iRun">given run</param>
		/// <param name="odt">object data type, 0 if no owned guid located</param>
		/// <returns>
		/// The GUID associated with the specified run of the tss if it is for an owned object;
		/// otherwise Guid.Empty
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static Guid GetOwnedGuidFromRun(ITsString tss, int iRun,
			out FwObjDataTypes odt)
		{
			TsRunInfo tri;
			ITsTextProps ttp;

			return GetOwnedGuidFromRun(tss, iRun, out odt, out tri, out ttp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an owned Guid from the given run in a structured text string.
		/// </summary>
		/// <param name="tss">given structured text string</param>
		/// <param name="iRun">given run</param>
		/// <param name="odt">object data type, 0 if no owned guid located</param>
		/// <param name="tri">run information</param>
		/// <param name="ttp">text properties of the run</param>
		/// <returns>
		/// The GUID associated with the specified run of the tss if it is for an owned object;
		/// otherwise Guid.Empty
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static Guid GetOwnedGuidFromRun(ITsString tss, int iRun,
			out FwObjDataTypes odt, out TsRunInfo tri,
			out ITsTextProps ttp)
		{
			Set<FwObjDataTypes> desiredTypes = new Set<FwObjDataTypes>(2);
			desiredTypes.Add(FwObjDataTypes.kodtOwnNameGuidHot);
			desiredTypes.Add(FwObjDataTypes.kodtGuidMoveableObjDisp);
			return GetGuidFromRun(tss, iRun, out odt, out tri, out ttp, desiredTypes);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a Guid from the given run in a structured text string.
		/// </summary>
		/// <param name="tss">given structured text string</param>
		/// <param name="iRun">given run</param>
		/// <returns>
		/// The GUID associated with the specified run of the tss, if any; otherwise Guid.Empty
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static Guid GetGuidFromRun(ITsString tss, int iRun)
		{
			TsRunInfo tri;
			ITsTextProps ttp;

			return GetGuidFromRun(tss, iRun, out tri, out ttp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a Guid from the given run in a structured text string.
		/// </summary>
		/// <param name="tss">given structured text string</param>
		/// <param name="iRun">given run</param>
		/// <param name="tri">run information</param>
		/// <param name="ttp">text properties of the run</param>
		/// <returns>
		/// The GUID associated with the specified run of the tss, if any; otherwise Guid.Empty
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static Guid GetGuidFromRun(ITsString tss, int iRun, out TsRunInfo tri,
			out ITsTextProps ttp)
		{
			FwObjDataTypes odt;
			return GetGuidFromRun(tss, iRun, out odt, out tri, out ttp, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a Guid from the given run in a structured text string.
		/// </summary>
		/// <param name="tss">given structured text string</param>
		/// <param name="iRun">given run</param>
		/// <param name="desiredOrcType">The desired ORC type</param>
		/// <returns>
		/// The GUID associated with the specified run of the tss, if any; otherwise Guid.Empty
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static Guid GetGuidFromRun(ITsString tss, int iRun,
			FwObjDataTypes desiredOrcType)
		{
			TsRunInfo tri;
			ITsTextProps ttp;
			FwObjDataTypes odt;
			Set<FwObjDataTypes> desiredType = new Set<FwObjDataTypes>(1);
			desiredType.Add(desiredOrcType);
			return GetGuidFromRun(tss, iRun, out odt, out tri, out ttp, desiredType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a Guid from the given run in a structured text string.
		/// </summary>
		/// <param name="tss">given structured text string</param>
		/// <param name="iRun">given run</param>
		/// <param name="odt">object data type, 0 if no ORC guid located</param>
		/// <param name="tri">run information</param>
		/// <param name="ttp">text properties of the run</param>
		/// <param name="desiredOrcTypes">The desired ORC types, or null to return any type of
		/// ORC</param>
		/// <returns>
		/// The GUID associated with the specified run of the tss, if any; otherwise Guid.Empty
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static Guid GetGuidFromRun(ITsString tss, int iRun,
			out FwObjDataTypes odt, out TsRunInfo tri,
			out ITsTextProps ttp, Set<FwObjDataTypes> desiredOrcTypes)
		{
			ttp = null;
			return GetGuidFromRun(tss, iRun, desiredOrcTypes, out odt, out tri, ref ttp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a Guid from the given run in a structured text string.
		/// </summary>
		/// <param name="tss">given structured text string</param>
		/// <param name="iRun">given run</param>
		/// <param name="ttp">text properties of the run</param>
		/// <returns>
		/// The GUID associated with the specified run of the tss, if any; otherwise Guid.Empty
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static Guid GetGuidFromRun(ITsString tss, int iRun, ITsTextProps ttp)
		{
			FwObjDataTypes odt;
			TsRunInfo tri;
			return GetGuidFromRun(tss, iRun, null, out odt, out tri, ref ttp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a Guid from the given run in a structured text string.
		/// </summary>
		/// <param name="tss">given structured text string</param>
		/// <param name="iRun">given run</param>
		/// <param name="odt">object data type, 0 if no ORC guid located</param>
		/// <param name="tri">run information</param>
		/// <param name="ttp">text properties of the run (if incoming value is  null and the
		/// run is an object, this will be set to the run props)</param>
		/// <param name="desiredOrcTypes">The desired ORC types, or null to return any type of
		/// ORC</param>
		/// <returns>
		/// The GUID associated with the specified run of the tss, if any; otherwise Guid.Empty
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private static Guid GetGuidFromRun(ITsString tss, int iRun,
			Set<FwObjDataTypes> desiredOrcTypes, out FwObjDataTypes odt, out TsRunInfo tri,
			ref ITsTextProps ttp)
		{
			odt = 0;
			tss.FetchRunInfo(iRun, out tri);
			if (tri.ichLim - tri.ichMin == 1 &&
				tss.get_RunText(iRun)[0] == kchObject)
			{
				// determine if single-character run contains an ORC
				ttp = ttp ?? tss.get_Properties(iRun);
				string sObjData = ttp.GetStrPropValue(
					(int)FwTextPropType.ktptObjData);

				if (sObjData != null)
				{
					odt = (FwObjDataTypes)Convert.ToByte(sObjData[0]);
					// See if it's one of the types of objects we want.
					if (desiredOrcTypes == null || desiredOrcTypes.Contains(odt))
					{
						// Get GUID for ORC
						return MiscUtils.GetGuidFromObjData(sObjData.Substring(1));
					}
				}
			}
			return Guid.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes an href element to the XML writer if the given string property is of the
		/// proper type.
		/// </summary>
		/// <param name="tpt">The FwTextPropType (passed as an int - grr).</param>
		/// <param name="sProp">The string property.</param>
		/// <param name="writer">The writer.</param>
		/// <returns><c>true</c> if the href attribute was written; <c>false</c> otherwise.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool WriteHref(int tpt, string sProp, XmlTextWriter writer)
		{
			if (tpt != (int)FwTextPropType.ktptObjData)
				return false;

			string sRef = GetURL(sProp);
			if (String.IsNullOrEmpty(sRef))
				return false;
			writer.WriteAttributeString("href", sRef);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the URL information in a form suitable for writing out in an .
		/// </summary>
		/// <param name="sProp">The string property containing the URL info.</param>
		/// ------------------------------------------------------------------------------------
		public static string GetURL(string sProp)
		{
			if (!String.IsNullOrEmpty(sProp) &&
				sProp[0] == Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName))
			{
				string sRef = sProp.Substring(1);
				if (!String.IsNullOrEmpty(sRef))
				{
					Match matchURL = kRegexUrl.Match(sRef);
					if (!matchURL.Success)
						sRef = "file://" + sRef.Replace('\\', '/');
					return sRef;
				}
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks to see if the text properties are for a hyperlink.
		/// </summary>
		/// <param name="ttp">The text properties.</param>
		/// <returns><c>true</c> if the properties are for a hyperlink; <c>false</c> otherwise.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsHyperlink(ITsTextProps ttp)
		{
			string objData = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);
			if (!String.IsNullOrEmpty(objData) && objData.Length > 1 &&
				objData[0] == Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName))
			{
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the owned ORCs from the given ITsString.
		/// </summary>
		/// <param name="tss">The given ITsString.</param>
		/// <returns>ITsString of the ORCs in tss, if any.</returns>
		/// ------------------------------------------------------------------------------------
		public static ITsString GetOwnedORCs(ITsString tss)
		{
			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			for (int iRun = 0; iRun < tss.RunCount; iRun++)
			{
				FwObjDataTypes objDataType;
				Guid guid = GetOwnedGuidFromRun(tss, iRun, out objDataType);
				if (objDataType == FwObjDataTypes.kodtOwnNameGuidHot ||
					objDataType == FwObjDataTypes.kodtGuidMoveableObjDisp)
				{
					tssBldr.ReplaceRgch(tssBldr.Length, tssBldr.Length, tss.get_RunText(iRun), 1,
						tss.get_Properties(iRun));
				}
			}

			return tssBldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the owned ORCs from the Structured text string.
		/// </summary>
		/// <param name="tss">The structured text string, which may contain ORCs.</param>
		/// <returns>tss without ORCs.</returns>
		/// ------------------------------------------------------------------------------------
		public static ITsString RemoveOwnedORCs(ITsString tss)
		{
			if (tss == null || tss.Text == null)
				return null;

			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			for (int iRun = 0; iRun < tss.RunCount; iRun++)
			{
				FwObjDataTypes type;
				GetOwnedGuidFromRun(tss, iRun, out type);
				if (type != FwObjDataTypes.kodtOwnNameGuidHot
					&& type != FwObjDataTypes.kodtGuidMoveableObjDisp)
				{
					int length = tssBldr.GetString().Length;
					string runText = tss.get_RunText(iRun);
					tssBldr.ReplaceRgch(length, length, runText, runText.Length, tss.get_Properties(iRun));
				}
			}
			return tssBldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove the footnotes from the selected ITsString.
		/// </summary>
		/// <param name="tss">The structured text string, which may contain ORCs.</param>
		/// <returns>tss without ORCs.</returns>
		/// <remarks>When the contents of the Find or the Replace text boxes are set by a paste
		/// from the clipboard, the ORCs are removed. However, when the text boxes are set by
		/// the current selection, they are set as character 0xFEFF followed by the footnote
		/// marker--which is why this method is needed.</remarks>
		/// ------------------------------------------------------------------------------------
		public static ITsString RemoveFootnotesFromTss(ITsString tss)
		{
			if (tss == null || tss.Text == null)
				return tss;

			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			for (int iRun = 0; iRun < tss.RunCount; iRun++)
			{
				// if the run does not begin with 0xFEFF, then we can add it. Otherwise,
				// ignore it because it is how a footnote marker is represented when obtained
				// from a selection.
				string runText = tss.get_RunText(iRun);
				ITsTextProps runProps = tss.get_Properties(iRun);
				string objData = runProps.GetStrPropValue((int)FwTextPropType.ktptObjData);
				if (!string.IsNullOrEmpty(runText) && string.IsNullOrEmpty(objData))
				{
					int length = tssBldr.GetString().Length;
					tssBldr.ReplaceRgch(length, length, runText, runText.Length, runProps);
				}
			}

			if (tssBldr.Length == 0)
				tssBldr.SetProperties(0, 0, PropsForWs(GetWsOfRun(tss, 0)));

			return tssBldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove ORCs and specified character styles.
		/// </summary>
		/// <param name="tss">structured text string</param>
		/// <param name="stylesToRemove">List of styles to remove (null if no styles should be
		/// removed).</param>
		/// <param name="fStopOnRemovedStyle">if set to <c>true</c> stop building the string
		/// when a style to remove is encountered and we have some text in the string;
		/// <c>false</c> to just skip stylesToRemove.</param>
		/// <param name="writingSystemFactory">The writing system factory.</param>
		/// <returns>
		/// tss with ORCs or styles to remove, or null if the tss is null (TE-8225).
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static ITsString RemoveORCsAndStylesFromTSS(ITsString tss, List<string> stylesToRemove,
			bool fStopOnRemovedStyle, ILgWritingSystemFactory writingSystemFactory)
		{
			if (tss == null)
				return null;

			// first remove any footnotes from the runs before doing our processing on the runs.
			ITsString tssNoFootnotes = RemoveFootnotesFromTss(tss);
			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			for (int iRun = 0; iRun < tssNoFootnotes.RunCount; iRun++)
			{
				// Get the text of the run. If it is empty or it is an ORC, then skip it
				string runText = tssNoFootnotes.get_RunText(iRun);
				if (runText == null || runText[0] == kchObject)
					continue;

				// Get the character style of the run
				ITsTextProps runProps = tssNoFootnotes.get_Properties(iRun);
				string charStyleName = runProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);

				// Check for removed styles.
				if (stylesToRemove != null && stylesToRemove.Contains(charStyleName))
				{
					if (fStopOnRemovedStyle && tssBldr.Length > 0)
						break;
					continue;
				}

				// Append the text to the accumulated ITsString
				tssBldr.Replace(tssBldr.Length, tssBldr.Length, runText, runProps);
			}

			if (tssBldr.Length > 0)
			{
				// Trim the start of the first run.
				string firstRun = tssBldr.GetString().get_RunText(0);
				string trimmedString = firstRun.TrimStart();
				ITsTextProps props = tssBldr.get_Properties(0);
				if (tssBldr.RunCount == 1)
				{
					trimmedString = trimmedString.TrimEnd();
					tssBldr.Replace(0, firstRun.Length, trimmedString, props);
				}
				else
				{
					tssBldr.Replace(0, firstRun.Length, trimmedString, props);

					// Trim the end of the last run.
					int iLastRun = tssBldr.RunCount - 1;
					string lastRun = tssBldr.GetString().get_RunText(iLastRun);
					trimmedString = lastRun.TrimEnd();

					int ichLastRunStart, ichLastRunEnd;
					tssBldr.GetBoundsOfRun(iLastRun, out ichLastRunStart, out ichLastRunEnd);
					props = tssBldr.get_Properties(iLastRun);
					tssBldr.Replace(ichLastRunStart, ichLastRunEnd, trimmedString, props);
				}
			}

			if (tssBldr.Length == 0)
				tssBldr.SetProperties(0, 0, PropsForWs(GetWsOfRun(tss, 0)));

			return tssBldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the text from a ITsString, removing ORCs and the specified character styles.
		/// Also trims any non-wordforming characters from the beginning and the end of the text.
		/// </summary>
		/// <param name="tss">structured text string</param>
		/// <param name="stylesToRemove">List of styles to remove (null if no styles should be
		/// removed).</param>
		/// <param name="fStopOnRemovedStyle">if set to <c>true</c> stop building the string
		/// when a style to remove is encountered and we have some text in the string;
		/// <c>false</c> to just skip stylesToRemove.</param>
		/// <param name="writingSystemFactory">The writing system factory.</param>
		/// <returns>
		/// tss with ORCs and any specified styles removed.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static string CleanTSS(ITsString tss, List<string> stylesToRemove,
			bool fStopOnRemovedStyle, ILgWritingSystemFactory writingSystemFactory)
		{
			// We have to accumulate the result in a TsStrBldr because TrimNonWordFormingChars
			// needs a TsString to get the right CPEs to test for wordforming characters.
			ITsString newTss = RemoveORCsAndStylesFromTSS(tss, stylesToRemove,
				fStopOnRemovedStyle, writingSystemFactory);

			if (newTss == null)
				return null;
			// Trim the start and end of the string.
			string result = TrimNonWordFormingChars(newTss, writingSystemFactory).Text;
			if (result == null)
				return string.Empty;
			return result;
		}

		/// <summary>
		/// Extract the specified substring from the ITsString. (Returns null if input is null.)
		/// If length is too large, return all of the string from ichMin on.
		/// </summary>
		public static ITsString Substring(ITsString tss, int ichMin, int length1)
		{
			if (tss == null)
				return null;
			Debug.Assert(ichMin <= tss.Length);
			int length = length1;
			if (ichMin + length > tss.Length)
				length = tss.Length - ichMin;
			if (ichMin + length < tss.Length)
			{
				ITsStrBldr bldr = tss.GetBldr();
				bldr.Replace(ichMin + length, bldr.Length, null, null);
				if (ichMin > 0)
					bldr.Replace(0, ichMin, null, null);
				return bldr.GetString();
			}
			// nothing to remove at end.
			return Substring(tss, ichMin);
		}
		/// <summary>
		/// Extract the specified substring from the ITsString, from ichMin on.
		/// </summary>
		public static ITsString Substring(ITsString tss, int ichMin)
		{
			if (tss == null)
				return null;
			Debug.Assert(ichMin <= tss.Length);
			if (ichMin > 0)
			{
				ITsStrBldr bldr = tss.GetBldr();
				bldr.Replace(0, ichMin, null, null);
				return bldr.GetString();
			}
			// nothing to remove at all
			return tss;
		}

		/// <summary>
		/// Split the ITsString into pieces separated by one of the strings in separator, and
		/// using the same options as String.Split().
		/// </summary>
		public static List<ITsString> Split(ITsString tss, string[] separator, StringSplitOptions opt)
		{
			List<ITsString> rgtss = new List<ITsString>();
			if (tss == null || tss.Length == 0 || separator == null || separator.Length == 0)
			{
				rgtss.Add(tss);
			}
			else
			{
				int ich = 0;
				while (ich < tss.Length)
				{
					int cchMatch = 0;
					int ichEnd = tss.Text.IndexOf(separator[0], ich);
					if (ichEnd < 0)
						ichEnd = tss.Length;
					else
						cchMatch = separator[0].Length;
					for (int i = 1; i < separator.Length; ++i)
					{
						int ichEnd2 = tss.Text.IndexOf(separator[i], ich);
						if (ichEnd2 < 0)
							ichEnd2 = tss.Length;
						if (ichEnd2 < ichEnd)
						{
							ichEnd = ichEnd2;
							cchMatch = separator[i].Length;
						}
					}
					int length = ichEnd - ich;
					if (length > 0 || opt == StringSplitOptions.None)
						rgtss.Add(Substring(tss, ich, length));
					ich = ichEnd + cchMatch;
				}
			}
			return rgtss;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Trim the leading AND trailing non-word forming characters.
		/// </summary>
		/// <param name="untrimmedString">string that may contain non-word forming characters</param>
		/// <param name="writingSystemFactory">The ws factory used to get character properties</param>
		/// <returns>string with leading AND trailing non-word forming characters trimmed</returns>
		/// ------------------------------------------------------------------------------------
		public static ITsString TrimNonWordFormingChars(ITsString untrimmedString,
			ILgWritingSystemFactory writingSystemFactory)
		{
			return TrimNonWordFormingChars(untrimmedString, writingSystemFactory, true, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Trim the leading AND/OR trailing non-word forming characters.
		/// </summary>
		/// <param name="tssInput">string that may contain non-word forming characters</param>
		/// <param name="writingSystemFactory">The ws factory used to get character properties</param>
		/// <param name="fTrimLeading">if set to <c>true</c> trim leading characters.</param>
		/// <param name="fTrimTrailing">if set to <c>true</c> trim trailing characters.</param>
		/// <returns>
		/// string with leading or trailing non-word forming characters trimmed
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static ITsString TrimNonWordFormingChars(ITsString tssInput,
			ILgWritingSystemFactory writingSystemFactory, bool fTrimLeading, bool fTrimTrailing)
		{
			Debug.Assert(fTrimLeading || fTrimTrailing);

			if (tssInput == null)
				return null;
			string untrimmedString = tssInput.Text;
			if (string.IsNullOrEmpty(untrimmedString))
				return tssInput;
			ILgCharacterPropertyEngine charProps = null;
			try
			{
				int ichMin;
				bool fFoundWordFormingChar = false;
				if (fTrimLeading)
				{
					// Trim leading non-word forming characters from string.
					ichMin = -1;
					for (int ich = 0; ich < untrimmedString.Length; ich++)
					{
						charProps = UpdateCharProps(tssInput, ich, writingSystemFactory, charProps);


						if (charProps != null && charProps.get_IsWordForming(untrimmedString[ich]))
						{
							// first word-forming character found
							ichMin = ich;
							fFoundWordFormingChar = true;
							break;
						}
					}
					if (ichMin == -1)
						return MakeTss("", GetWsAtOffset(tssInput, 0)); // no word-forming characters found in the string
				}
				else
					ichMin = 0;

				int strLength;
				if (fTrimTrailing)
				{
					// Trim trailing non-word forming characters from string.
					strLength = 1;
					int iMin = (ichMin == 0) ? -1 : ichMin;
					for (int ich = untrimmedString.Length - 1; ich > iMin; ich--)
					{
						charProps = UpdateCharProps(tssInput, ich, writingSystemFactory, charProps);
						if (charProps != null && charProps.get_IsWordForming(untrimmedString[ich]))
						{
							// last word-forming character found
							strLength = ich - ichMin + 1;
							fFoundWordFormingChar = true;
							break;
						}
					}

					if (!fFoundWordFormingChar)
						return MakeTss("", GetWsAtOffset(tssInput, 0)); // no word-forming characters found in the string
				}
				else
					strLength = untrimmedString.Length - ichMin;

				return Substring(tssInput, ichMin, strLength);
			}
			finally
			{
				if (charProps != null && Marshal.IsComObject(charProps))
					Marshal.ReleaseComObject(charProps);
			}
		}

		private static ILgCharacterPropertyEngine UpdateCharProps(ITsString tssInput, int ich, ILgWritingSystemFactory writingSystemFactory, ILgCharacterPropertyEngine charProps)
		{
			ILgCharacterPropertyEngine newCharProps = GetCharPropEngineAtOffset(tssInput, writingSystemFactory, ich);
			if (newCharProps != charProps)
			{
				if (charProps != null && Marshal.IsComObject(charProps))
					Marshal.ReleaseComObject(charProps);
				charProps = newCharProps;
			}
			return charProps;
		}

		/// <summary>
		/// Get the ws at the given (ich) char offset in the tss.
		/// </summary>
		/// <param name="tss"></param>
		/// <param name="ich">char offset</param>
		/// <returns></returns>
		public static int GetWsAtOffset(ITsString tss, int ich)
		{
			if (tss == null)
				return 0;
			int ws, nVar;
			ws = tss.get_PropertiesAt(ich).GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
			return ws;
		}

		/// <summary>
		/// Get the writing system of the specified run of a TsString.
		/// </summary>
		public static int GetWsOfRun(ITsString tss, int irun)
		{
			ITsTextProps props = tss.get_Properties(irun);
			int var;
			return props.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
		}

		/// <summary>
		/// Get the character property engine that should be used for interpreting the character at ich.
		/// </summary>
		public static ILgCharacterPropertyEngine GetCharPropEngineAtOffset(ITsString tss, ILgWritingSystemFactory wsf, int ich)
		{
			int ws = GetWsAtOffset(tss, ich);
			return (ws > 0) ? wsf.get_CharPropEngine(ws) : null;
		}

		/// <summary>
		/// Return a list of the writing systems found in the string.
		/// </summary>
		public static List<int> GetWritingSystems(ITsString tss)
		{
			List<int> result = new List<int>();
			result.Add(GetWsOfRun(tss, 0));
			int crun = tss.RunCount;
			for (int irun = 0; irun < crun; irun++)
			{
				int ws = GetWsOfRun(tss, irun);
				if (!result.Contains(ws))
					result.Add(ws);
			}
			return result;
		}

		/// <summary>
		/// Releases a tss string that was created with MakeString, before reassigning it.
		/// Especially useful in Dispose, since we check whether it is null before doing the release.
		/// </summary>
		/// <param name="tss">tss to reassign a value to</param>
		/// <param name="tssNewValue">the new tss value (including null)</param>
		public static void ReassignTss(ref ITsString tss, ITsString tssNewValue)
		{
			if (tss != null)
				Marshal.ReleaseComObject(tss);
			tss = tssNewValue;
		}

		/// <summary>
		/// Create a new Tss based on the given string and ws.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public static ITsString MakeTss(string src, int ws)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return tsf.MakeString(src, ws);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the word form exists in the string, matching the whole word or
		/// sequence of words.
		/// </summary>
		/// <param name="wordFormTss">text of word form to look for</param>
		/// <param name="sourceTss">text to search in</param>
		/// <param name="wsf">source of char prop engines</param>
		/// <param name="ichMin">The start of the string where the text was found (undefined if
		/// this method returns false)</param>
		/// <param name="ichLim">The limit (one character position past the end) of the string
		/// where the text was found (undefined if this method returns false)</param>
		/// <returns><c>true</c> if word form is matched exactly (whole word found and matching
		/// case)
		/// </returns>
		/// <remarks>
		/// TODO: Add a parameter to make it possible to do a case-insensitive match?</remarks>
		/// ------------------------------------------------------------------------------------
		public static bool FindWordFormInString(ITsString wordFormTss, ITsString sourceTss,
			ILgWritingSystemFactory wsf, out int ichMin, out int ichLim)
		{
			return FindTextInString(wordFormTss, sourceTss, wsf, true, out ichMin, out ichLim);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the specified text exists in the string
		/// </summary>
		/// <param name="wordFormTss">text to look for</param>
		/// <param name="sourceTss">text to search in</param>
		/// <param name="wsf">source of char prop engines</param>
		/// <param name="fMatchWholeWord">True to match a whole word, false otherwise</param>
		/// <param name="ichMin">The start of the string where the text was found (undefined if
		/// this method returns false)</param>
		/// <param name="ichLim">The limit (one character position past the end) of the string
		/// where the text was found (undefined if this method returns false)</param>
		/// <returns><c>true</c> if the text is matched exactly (matching case)
		/// </returns>
		/// <remarks>
		/// TODO: Add a parameter to make it possible to do a case-insensitive match?</remarks>
		/// ------------------------------------------------------------------------------------
		public static bool FindTextInString(ITsString wordFormTss, ITsString sourceTss,
			ILgWritingSystemFactory wsf, bool fMatchWholeWord, out int ichMin, out int ichLim)
		{
			ichMin = ichLim = 0;

			if (wordFormTss == null || sourceTss == null || wordFormTss.Length == 0 ||
				sourceTss.Length == 0)
			{
				// Nothing we can really do
				return false;
			}

			int ichWordForm = 0;
			bool fMatchInProgress = false;
			CpeTracker sourceTracker = new CpeTracker(wsf, sourceTss);
			CpeTracker wordTracker = new CpeTracker(wsf, wordFormTss);
			string wordForm = wordFormTss.Text;
			string sourceText = sourceTss.Text;
			bool fWordFormAndSrcAreBothNormalized = wordForm.IsNormalized() && sourceText.IsNormalized();
			bool fPrevCharWasWordForming = false;
			// Must use local temp variable because some callers pass same variable as both
			// ichMin and ichLim.
			int ichLimT;
			for (int ichSrc = 0; ichSrc < sourceText.Length; ichSrc = ichLimT)
			{
				ichLimT = ichSrc + 1;
				bool fWordForming = sourceTracker.CharPropEngine(ichSrc).get_IsWordForming(sourceText[ichSrc]);
				if (!fMatchInProgress && fPrevCharWasWordForming && fWordForming && fMatchWholeWord)
					continue;

				fPrevCharWasWordForming = fWordForming;
				if (!fMatchInProgress && !fWordForming)
					continue;

				bool fMatch = (wordForm[ichWordForm] == sourceText[ichSrc]);

				if (fMatch)
				{
					ichWordForm++;
				}
				else
				{
					if (sourceText[ichSrc] == kchObject)
						fMatch = true;
					else if (!fWordFormAndSrcAreBothNormalized)
					{
						int ichWfCharLim = ichWordForm + 1;

						while (ichWfCharLim < wordForm.Length &&
							!wordTracker.CharPropEngine(ichWfCharLim).get_IsLetter(wordForm[ichWfCharLim]) &&
							wordTracker.CharPropEngine(ichWfCharLim).get_IsWordForming(wordForm[ichWfCharLim]))
						{
							ichWfCharLim++;
						}

						while (ichLimT < sourceText.Length &&
							!sourceTracker.CharPropEngine(ichLimT).get_IsLetter(sourceText[ichLimT]) &&
							sourceTracker.CharPropEngine(ichLimT).get_IsWordForming(sourceText[ichLimT]))
						{
							ichLimT++;
						}

						if (wordForm.Substring(ichWordForm, ichWfCharLim - ichWordForm).Normalize() ==
							sourceText.Substring(ichSrc, ichLimT - ichSrc).Normalize())
						{
							ichWordForm = ichWfCharLim;
							fMatch = true;
						}
					}
				}

				if (fMatch)
				{
					// If this character is the start of a match but the previous character was
					// word-forming, then this is a bogus match, so we keep looking.
					if (!fMatchInProgress)
					{
						if (ichSrc == 0 || !sourceTracker.CharPropEngine(ichSrc - 1).get_IsWordForming(sourceText[ichSrc - 1]) || !fMatchWholeWord)
						{
							ichMin = ichSrc;
							fMatchInProgress = true;
						}
						else
							ichWordForm = 0;
					}

					if (fMatchInProgress && ichWordForm == wordForm.Length)
					{
						if (++ichSrc < sourceText.Length && sourceTracker.CharPropEngine(ichSrc).get_IsWordForming(sourceText[ichSrc]) &&
							fMatchWholeWord)
						{
							ichWordForm = 0;
							fMatchInProgress = false;
						}
						else
						{
							ichLim = ichLimT;
							return true;
						}
					}
				}
				else
				{
					ichWordForm = 0;
					fMatchInProgress = false;
				}
			}
			return false;
		}

		///--------------------------------------------------------------------------------
		/// <summary>
		/// Returns an array of strings containing substrings of a format string. The
		/// array contains strings representing the substrings between arguments in a
		/// format string. null strings are included in the array of strings at elements
		/// in the array corresponding to where they fall in the format string. For
		/// example, if the format string were:
		///
		///		"This is arg. zero {0} and this is arg. one {1}. Understand?"
		///
		///	The array of strings returned would contain the following strings:
		///
		///		string[0] = "This is arg. zero "
		///		string[1] = null
		///		string[2] = " and this is arg. one "
		///		string[3] = null
		///		string[4] = ". Understand?"
		/// </summary>
		/// <param name="format">A format string containing argument placeholders
		/// (Example "{0}").</param>
		/// <returns>An array of strings representing substrings found between the
		/// arguments and null strings at elements in the string array corresponding
		/// to where the arguments fall in the format string.</returns>
		///--------------------------------------------------------------------------------
		public static string[] ParseFormatString(string format)
		{
			if (format == null || format.Length == 0)
				return null;

			int argCount = 0;
			int iFirst = 0;
			int iLast;

			List<string> al = new List<string>();

			// First, determine how many arguments are in the format string.
			while (true)
			{
				iLast = format.IndexOf("{" + argCount.ToString() + "}");

				if (iLast < 0)
					break;

				// Add the text to the left of the argument.
				if (iLast > 0)
					al.Add(format.Substring(iFirst, iLast - iFirst));

				// Add a null string for the argument itself.
				al.Add(null);

				// Bump the pointer to point just after the argument.
				iFirst = iLast + 3;
				argCount++;
			}

			// If the last argument found isn't at the very end of the format string then
			// make sure we grab the last chunk of the format string (i.e. the characters
			// that follow the last argument.
			if (iFirst < format.Length)
				al.Add(format.Substring(iFirst));

			return al.ToArray();
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the ICU Data directory based on the ICU data directory.  It is
		/// safe to call this static method more than once, but necessary only to call it at
		/// least once.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void InitIcuDataDir()
		{
			Icu.InitIcuDataDir();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Normalize the string for display.  Uniscribe requires NFC.
		/// (See LT-4202 and LT-4203.)
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string NormalizeToNFC(string s)
		{
			return Icu.Normalize(s, Icu.UNormalizationMode.UNORM_NFC);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Utility function to make an string array out of an arraylist containing strings.
		/// </summary>
		/// <param name="al"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string[] ListToStringArray(List<string> al)
		{
			return al.ToArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the portion of two strings which is different.
		/// </summary>
		/// <param name="str1">first string to compare</param>
		/// <param name="str2">second string to compare</param>
		/// <param name="ichMin">The index of the first difference or -1 if no difference is
		/// found</param>
		/// <param name="ichLim1">The index of the limit of the difference in the 1st string or
		/// -1 if no difference is found</param>
		/// <param name="ichLim2">The index of the limit of the difference in the 2nd string or
		/// -1 if no difference is found</param>
		/// <returns><c>true</c> if a difference is found; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool FindStringDifference(string str1, string str2, out int ichMin,
			out int ichLim1, out int ichLim2)
		{
			ichMin = FindStringDifference(str1, str2);
			if (ichMin >= 0)
			{
				// Search from the end to find the end of the text difference
				FindStringDifferenceLimits(str1, str2, ichMin, out ichLim1, out ichLim2);
				return true;
			}
			ichLim1 = -1;
			ichLim2 = -1;
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a difference in two strings.
		/// </summary>
		/// <param name="str1">first string to compare</param>
		/// <param name="str2">second string to compare</param>
		/// <returns>The index of the first difference or -1 if not found</returns>
		/// ------------------------------------------------------------------------------------
		private static int FindStringDifference(string str1, string str2)
		{
			for (int i = 0; i < str1.Length;)
			{
				// If we pass the end of string 2 before string 1 then the difference is
				// the remainder of string 1.
				if (i >= str2.Length)
					return i;

				// get the next character with its combining modifiers
				string next1 = GetNextCharWithModifiers(str1, i);
				string next2 = GetNextCharWithModifiers(str2, i);

				if (next1 != next2)
					return i;
				i += next1.Length;
			}

			// If the second string is longer than the first string, then return the difference as the
			// last portion of the second string.
			if (str2.Length > str1.Length)
				return str1.Length;

			// If we get this far, the strings are the same.
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a localized name for the given identification string. If the ID string has
		/// spaces in it, replace them with underscores for lookup. If the string is not found,
		/// the idString is returned.
		/// </summary>
		/// <param name="rm">The resource manager where we will attempt to lookup the value for
		/// the idString.</param>
		/// <param name="kstid">the localization key (which may contain spaces).</param>
		/// <returns>the value looked up with the idString in the given resource manager,
		/// or the idString if no localized name is found for the current interface locale</returns>
		/// ------------------------------------------------------------------------------------
		public static string GetUiString(ResourceManager rm, string kstid)
		{
			if (rm == null)
				return kstid;
			try
			{
				StringBuilder bldr = new StringBuilder(kstid.Length);

				// Since the resource keys can't contain these, we replace them
				// with underscores. This list is not likely to be comprehensive.
				foreach (char c in kstid)
				{
					bldr.Append(
						" ;:.,=+|/~?%@#$!^&*\'\"\\[]{}()<>-".IndexOf(c) >= 0 ? '_' : c);
				}

				return rm.GetString(bldr.ToString()) ?? kstid;
			}
			catch
			{
				return kstid;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the next character sequence that represents a character with any modifiers.
		/// This allows us to treat characters with diacritics as a unit.
		/// </summary>
		/// <param name="source">string to get character from</param>
		/// <param name="start">index to start extracting from</param>
		/// <returns>a character string with the base character and all modifiers</returns>
		/// ------------------------------------------------------------------------------------
		private static string GetNextCharWithModifiers(string source, int start)
		{
			int end = start + 1;
			while (end < source.Length && Char.GetUnicodeCategory(source, end) == UnicodeCategory.NonSpacingMark)
				end++;
			return source.Substring(start, end - start);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a difference in two strings.
		/// </summary>
		/// <param name="str1">first string to compare</param>
		/// <param name="str2">second string to compare</param>
		/// <param name="ichMin">the min of the difference that has already been found</param>
		/// <param name="ichLim1">The index of the limit of the difference in the 1st string</param>
		/// <param name="ichLim2">The index of the limit of the difference in the 2nd string</param>
		/// ------------------------------------------------------------------------------------
		private static void FindStringDifferenceLimits(string str1, string str2, int ichMin,
			out int ichLim1, out int ichLim2)
		{
			ichLim1 = str1.Length;
			ichLim2 = str2.Length;
			// look backwards through the strings for the end of the difference. Do not
			// look past the start of the difference which has already been found.
			while (ichLim1 > ichMin && ichLim2 > ichMin)
			{
				if (str1[ichLim1 - 1] != str2[ichLim2 - 1])
					return;
				--ichLim1;
				--ichLim2;
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reverse the order of characters within the string (make a mirror image of it).  The
		/// tricky part, as always with UTF-16, is handling surrogate pairs correctly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string ReverseString(string s)
		{
			if (s == null)
				return "";
			char[] rgch = s.ToCharArray();
			int ia;
			int iz;
			char chT;
			// First flip all the chars as quickly as possible.
			for (ia = 0, iz = rgch.Length - 1; ia < iz; ++ia, --iz)
			{
				chT = rgch[ia];
				rgch[ia] = rgch[iz];
				rgch[iz] = chT;
			}
			// Then fix up any surrogates that got scrambled.
			for (ia = 0, iz = rgch.Length - 2; ia < iz; ++ia)
			{
				if (Surrogates.IsTrailSurrogate(rgch[ia]))
				{
					chT = rgch[ia];
					rgch[ia] = rgch[ia + 1];
					rgch[ia++] = chT;
				}
			}

			return new string(rgch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a new stream and fill it in with data from a string
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <returns>the new stream</returns>
		/// ------------------------------------------------------------------------------------
		public static MemoryStream MakeStreamFromString(string source)
		{
			byte[] buffer = new byte[source.Length * 2];
			int index = 0;
			foreach (char ch in source)
			{
				buffer[index++] = (byte)(ch & 0xff);
				buffer[index++] = (byte)((int)ch >> 8);
			}
			MemoryStream stream = new MemoryStream(buffer);
			return stream;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads a string value from a binary reader
		/// </summary>
		/// <param name="reader">The reader to read from</param>
		/// <param name="length">The length of the string</param>
		/// <returns>the string</returns>
		/// ------------------------------------------------------------------------------------
		public static string ReadString(BinaryReader reader, int length)
		{
			System.Text.StringBuilder bldr = new System.Text.StringBuilder();

			while (length-- > 0)
				bldr.Append((char)reader.ReadInt16());
			return bldr.ToString();
		}
		/// <summary>
		/// Remove all whitespace from a string.
		/// </summary>
		/// <param name="s"></param>
		/// <returns>hvo of default environment.</returns>
		public static string StripWhitespace(string s)
		{
			if (s == null)
				return s;
			int len = s.Length;
			if (len <= 0)
				return s;
			StringBuilder sb = new StringBuilder(len);
			for (int i = 0; i < len; i++)
			{
				char c = s[i];
				if ((c != ' ') &&
					 (c != '\t') &&
					 (c != '\n'))
					sb.Append(c);
			}
			return sb.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parse the string into a integer, assuming base 10 notation.  This is similar to the
		/// C function of the same name, but without the base argument.  It also doesn't handle
		/// negative numbers.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="sRemainder"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int strtol(string data, out string sRemainder)
		{
			int nVal = 0;
			if (!String.IsNullOrEmpty(data))
			{
				char[] rgch = data.ToCharArray();
				for (int i = 0; i < rgch.Length; ++i)
				{
					if (Char.IsDigit(rgch[i]))
					{
						nVal = nVal * 10 + (int)Char.GetNumericValue(rgch[i]);
					}
					else
					{
						sRemainder = data.Substring(i);
						return nVal;
					}
				}
			}
			sRemainder = String.Empty;
			return nVal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string of codepoints for the characters in the specified string.
		/// </summary>
		/// <param name="chrs">The string of characters.</param>
		/// <returns>A comma separated string of U+XXXX numbers.</returns>
		/// ------------------------------------------------------------------------------------
		public static string CharacterCodepoints(string chrs)
		{
			StringBuilder bldr = new StringBuilder();
			foreach (char c in chrs)
			{
				bldr.Append(GetUnicodeValueString(c));
				bldr.Append(", ");
			}

			// Trim off the final comma and space.
			if (bldr.Length > 2)
				bldr.Length -= 2;

			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a formatted string for the specified character in the form people like to look
		/// at Unicode codepoints (e.g. "U+0305").
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string GetUnicodeValueString(char chr)
		{
			return GetUnicodeValueString((int)chr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a formatted string for the specified integer in the form people like to look
		/// at Unicode codepoints (e.g. "U+0305").
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string GetUnicodeValueString(int codepoint)
		{
			return string.Format("U+{0:X4}", codepoint);
		}

		// We don't want to localize internal folder names for pictures and media.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string for the local picture folder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public const string LocalPictures = "Local Pictures";
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string for the local media folder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public const string LocalMedia = "Local Media";

		/// <summary>
		/// Compare two strings. Return -1 if they are equal, otherwise, indicate the index
		/// of the first differing character. (If they are equal to the length of the shorter,
		/// return the length of the shorter.)
		/// </summary>
		public static int FirstDiff(string first, string second)
		{
			int lenFirst = 0;
			if (first != null)
				lenFirst = first.Length;
			int lenSecond = 0;
			if (second != null)
				lenSecond = second.Length;
			int lenBoth = Math.Min(lenFirst, lenSecond);
			for (int ich = 0; ich < lenBoth; ich++)
				if (first[ich] != second[ich])
					return ich;
			// equal as far as len.
			if (lenFirst != lenSecond)
				return lenBoth;
			return -1;
		}

		/// <summary>
		/// Compare two strings. Return -1 if they are equal, otherwise, indicate the number of
		/// characters at the ends of the two strings that are equal. (If they are equal to the length of the shorter,
		/// return the difference between the lengths.)
		/// </summary>
		public static int LastDiff(string first, string second)
		{
			int lenFirst = 0;
			if (first != null)
				lenFirst = first.Length;
			int lenSecond = 0;
			if (second != null)
				lenSecond = second.Length;
			int diffLen = lenFirst - lenSecond; // how much first is longer; what to add to position in first for corresponding in second.
			int ichEnd = Math.Max(diffLen, 0); // part of first before part to compare, if any.
			for (int ich = lenFirst - 1; ich >= ichEnd; ich--)
				if (first[ich] != second[ich - diffLen])
					return lenFirst - ich - 1;
			// equal for min length.
			if (lenFirst == lenSecond)
				return -1; // equal.
			return Math.Min(lenFirst, lenSecond);
		}
		/// <summary>
		/// Answer a TsTextProps that specifies the given writing system (all other props have default values).
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public static ITsTextProps PropsForWs(int ws)
		{
			ITsPropsBldr tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
			return tpb.GetTextProps();
		}

		/// <summary>
		/// Get an indication of the range of characters that differ between two TsStrings.
		/// Return true if they are equal, false if they are not.
		/// If they are not, ichMin indicates the first different character (or the length of the shorter string).
		/// cvIns indicates how many characters must be inserted at ichMin, after deleting cvDel, to get tssNew.
		/// Character properties as well as values are considered.
		/// </summary>
		public static bool GetDiffsInTsStrings(ITsString tssOld, ITsString tssNew, out int ichMin, out int cvIns, out int cvDel)
		{
			ichMin = -1; // no diff found
			cvIns = cvDel = 0; // defaults in case no diff found.
			int cchOld = 0;
			int crunOld = 0;
			int crunNew = 0;
			int cchNew = 0;
			if (tssNew != null)
			{
				cchNew = tssNew.Length;
				crunNew = tssNew.RunCount;
			}
			if (tssOld != null)
			{
				cchOld = tssOld.Length;
				crunOld = tssOld.RunCount;
			}
			int crunBoth = Math.Min(crunOld, crunNew);
			// Set ivMin to the index of the first character that is different or has different
			// properties.
			for (int irun = 0; irun < crunBoth; irun++)
			{
				if (tssOld.get_Properties(irun) != tssNew.get_Properties(irun))
				{
					ichMin = tssNew.get_MinOfRun(irun); // previous runs are all OK.
					break; // difference at start of this run.
				}
				int ichMinRun = StringUtils.FirstDiff(tssOld.get_RunText(irun), tssNew.get_RunText(irun));
				if (ichMinRun >= 0)
				{
					ichMin = tssNew.get_MinOfRun(irun) + ichMinRun;
					break;
				}
			}
			if (ichMin < 0)
			{
				// no difference found as far as crunBoth.
				if (crunNew > crunBoth)
				{
					// All the additional length of tssNew is inserted.
					ichMin = cchOld;
					cvIns = cchNew - cchOld;
					return false;
				}
				if (crunOld > crunBoth)
				{
					// All the additional length of tssOld is deleted.
					ichMin = cchNew;
					cvDel = cchOld - cchNew;
					return false;
				}
				// same number of runs are identical; strings are equal
				return true;
			}
			// There is a difference at ichMin.
			// A default assumption is that the remainder of both strings differs.
			//int cchEndBoth = Math.Min(cvIns, cvDel); // max characters that could be equal at end.
			int irunOld = crunOld - 1;
			int irunNew = crunNew - 1;
			int cchSameEnd = 0;
			for (; irunOld >= 0 && irunNew >= 0; irunOld--, irunNew--)
			{
				if (tssOld.get_Properties(irunOld) != tssNew.get_Properties(irunNew))
				{
					// difference at end of this run. All the text beyond this run (if any) must have matched.
					break;
				}
				int cchSameRun = LastDiff(tssOld.get_RunText(irunOld), tssNew.get_RunText(irunNew));
				if (cchSameRun >= 0)
				{
					// End of equal bit is cchSame from the ends of the two runs
					cchSameEnd = cchOld - tssOld.get_LimOfRun(irunOld) + cchSameRun;
					break;
				}
				// Same to start of this run.
				cchSameEnd = cchOld - tssOld.get_MinOfRun(irunOld);
			}
			cvIns = cchNew - ichMin - cchSameEnd;
			cvDel = cchOld - ichMin - cchSameEnd;
			return false;
		}
	}

	/// <summary>
	/// This class is used to obtain the right character property engine while iterating over
	/// the characters of a string. It is most effective for consecutive rather than random
	/// character access.
	/// </summary>
	public class CpeTracker
	{
		private ITsString m_tssText; // string we're processing.
		private int m_ichMinCpe; // range over which current m_cpe is valid.
		private int m_ichLimCpe;
		private int m_wsCpe; // ws for which m_cpe is valid.
		private ILgCharacterPropertyEngine m_cpe;
		private ILgWritingSystemFactory m_wsf;

		/// <summary>
		/// make the compiler happy.
		/// </summary>
		public CpeTracker(ILgWritingSystemFactory wsf, ITsString tss)
		{
			m_wsf = wsf;
			m_tssText = tss;
			m_ichLimCpe = 0; // ensures first request will fail.
		}

		/// <summary>
		/// Get a suitable CPE for the specified character of the original string.
		/// </summary>
		/// <param name="ich"></param>
		/// <returns></returns>
		public ILgCharacterPropertyEngine CharPropEngine(int ich)
		{
			if (ich >= m_ichMinCpe && ich < m_ichLimCpe)
				return m_cpe;
			int ws;
			if (m_tssText == null)
			{
				ws = m_wsf.UserWs; // pick an arbitrary one, for any index.
				m_ichMinCpe = 0;
				m_ichLimCpe = int.MaxValue;
			}
			else
			{
				int irun = m_tssText.get_RunAt(ich);
				ITsTextProps ttp = m_tssText.get_Properties(irun);
				m_tssText.GetBoundsOfRun(irun, out m_ichMinCpe, out m_ichLimCpe);
				int dumVar;
				ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out dumVar);
			}
			if (ws != m_wsCpe) // different run, but may not differ in ws.
			{
				m_wsCpe = ws;
				if (ws == -1)
				{
					// Bizarrely, the run has no WS specified. This happens occasionally in poorly-written tests.
					// Maybe there's some other way. Fall back to a default engine.
					m_cpe = m_wsf.UnicodeCharProps;
				}
				else
				{
					m_cpe = m_wsf.get_CharPropEngine(ws);
				}
			}
			return m_cpe;
		}
	}
}
