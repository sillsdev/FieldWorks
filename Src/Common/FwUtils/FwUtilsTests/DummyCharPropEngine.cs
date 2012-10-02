#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DummyCharPropEngine.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using NMock;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Char prop engine that lets us mock out most things and provide simple default behavior
	/// for others. Note that if you use a method not needed by any other tests, you'll have to
	/// provide an implementation, even if all you need it to do is call the mock.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyCharPropEngine : ILgCharacterPropertyEngine
	{
		private DynamicMock m_cpe;
		/// <summary>the mock instance that handles most things</summary>
		public ILgCharacterPropertyEngine m_mockCPE;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyCharPropEngine"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyCharPropEngine()
		{
			m_cpe = new DynamicMock(typeof(ILgCharacterPropertyEngine));
			m_mockCPE = (ILgCharacterPropertyEngine)m_cpe.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the expected methods were called the correct number of times.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Verify()
		{
			m_cpe.Verify();
		}

		#region ILgCharacterPropertyEngine Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the canonical decomposition of this character (Unicode char database field 5).
		/// Empty string if it does not decompose.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DecompositionRgch(int ch, int cchMax, out ushort _rgch, out int _cch, out bool _fHasDecomp)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the recursive canonical decomposition of a character. Returns the character
		/// itself if it does not decompose at all.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void FullDecompRgch(int ch, int cchMax, out ushort _rgch, out int _cch, out bool _fHasDecomp)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This combines the functions of <c>GetLineBreakProps</c> and <c>GetLineBreakStatus</c>,
		/// plus the ability to skip part of the string, and stop at break characters.
		/// The first two arguments define an array of characters as for <c>GetLineBreakProps</c>.
		/// The output is basically what would be produced by passing the output of
		/// that method as input to GetLineBreakStatus, with two exceptions:
		/// 1. We only want line break status info for characters from ichMin to ichLim.
		/// (A larger array is passed in because we may need to look at preceding characters
		/// to confidently know whether we can break after the char at ichMin.)
		/// 2. If we detect a character which forces a line or segment break in the range
		/// ichMin..ichLim, we stop and do not return any info about subsequent characters.
		/// Also, we set pichBreak to the index of the break character.
		/// pichBreak is set to 1 if we don't find a break character.
		/// Break characters are things like CR, LF, TAB, or the embedded object character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void GetLineBreakInfo(string _rgchIn, int cchIn, int ichMin, int ichLim, ArrayPtr _rglbsOut, out int _ichBreak)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get line breaking properties (from enumeration LgLBP) for an array of characters.
		/// This passes an array in, and an array with the same number of elements out, to
		/// improve efficiency when properties are required for a string of characters.
		/// See <see href="www.unicode.org/unicode/reports/tr14/">www.unicode.org/unicode/reports/tr14/</see>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void GetLineBreakProps(string _rgchIn, int cchIn, ArrayPtr _rglbOut)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get line break status (from combinations of values in enumeration LgLineBreakStatus)
		/// of each character in the input array.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void GetLineBreakStatus(byte[] _rglbpIn, int cb, ArrayPtr _rglbsOut)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text that the LineBreakBefore and LineBreakAfter functions are using. This
		/// function is included for completion.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void GetLineBreakText(int cchMax, out ushort _rgchOut, out int _cchOut)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the nearest line break immediately after the given index (ichIn). The function
		/// returns not only a location but the weight of the line break (currently not
		/// implemented). See http:www.unicode.org/unicode/reports/tr14/ for more information
		/// on line breaking properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void LineBreakAfter(int ichIn, out int _ichOut, out LgLineBreak _lbWeight)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the nearest line break immediately before the given index (ichIn). The
		/// function returns not only a location but the weight of the line break (currently not
		/// implemented). See http:www.unicode.org/unicode/reports/tr14/ for more information
		/// on line breaking properties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void LineBreakBefore(int ichIn, out int _ichOut, out LgLineBreak _lbWeight)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the locale of the engine. See put_Locale for more notes.
		/// Assigns the locale of the engine. The locale affects the ICU functions called for
		/// line breaking and changing cases. Note that the locale is read in using the
		/// Microsoft format we've been using for the rest of FieldWorks, as opposed to the ICU
		/// format (which it's translated into inside the class implementation).
		/// </summary>
		/// <returns>A System.Int32 </returns>
		/// ------------------------------------------------------------------------------------
		public int Locale
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For testing purposes, just return the string passed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string NormalizeD(string bstr)
		{
			return bstr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform normalization of the input string, that is, every character
		/// which has a decomposition is decomposed (recursively). This is D
		/// normalization as defined by Unicode TR 15.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void NormalizeDRgch(string _rgchIn, int cchIn, ArrayPtr _rgchOut, int cchMaxOut, out int _cchOut)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform compatibility normalization of the input string, that is, every character
		/// which has a compatibility decomposition is decomposed (recursively). This is KD
		/// normalization as defined by Unicode TR 15.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void NormalizeKdRgch(string _rgchIn, int cchIn, ArrayPtr _rgchOut, int cchMaxOut, out int _cchOut)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Strip diacritics. Specifically, removes all characters that have the property Lm on
		/// Mn. Note that this will not comvert a single code point that includes a diacritic to
		/// its unmodified equivalent. It is usually desireable to first perform normalization
		/// (form D or KD) before stripping diacritics.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string StripDiacritics(string bstr)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Strip diacritics. Specifically, removes all characters that have the property Lm or
		/// Mn. Note that this will not comvert a single code point that includes a diacritic to
		/// its unmodified equivalent. It is usually desireable to first perform normalization
		/// (form D or KD) before stripping diacritics.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void StripDiacriticsRgch(string _rgchIn, int cchIn, ArrayPtr _rgchOut, int cchMaxOut, out int _cchOut)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a string to lower case. Characters that are not Lu or Lt pass through
		/// unchanged. Note that the output may be longer than the input! This function, unlike
		/// <c>get_ToLowerCh</c>, will apply multicharacter conversions if Unicode specifies them.
		/// </summary>
		/// <exception cref="T:System.ArgumentException">if characters in input are not valid Unicode. (E_INVALIDARG)</exception>
		/// ------------------------------------------------------------------------------------
		public string ToLower(string bstr)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a string to lower case. Characters that are not Lu or Lt pass through
		/// unchanged. Note that the output may be longer than the input! This function, unlike
		/// <c>get_ToLowerCh</c>, will apply multicharacter conversions if Unicode specifies them.
		/// If cchOut is zero, just return the length needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ToLowerRgch(string _rgchIn, int cchIn, ArrayPtr _rgchOut, int cchOut, out int _cchRet)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a string to title case. Characters that are not Lu or Ll pass through
		/// unchanged. Note that the output may be longer than the input! This function, unlike
		/// <c>get_ToUpperCh</c>, will apply multicharacter conversions if Unicode specifies them.
		/// Note that this is not usually useful, because if you want title case, you usually
		/// want to convert only the first nonpunctuation character, or perhaps the first of
		/// each word. However, this at least provides a way to get at multicharacter
		/// conversions by passing a single character input string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ToTitle(string bstr)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a string to title case. Characters that are not Lu or Ll pass through
		/// unchanged. Note that the output may be longer than the input! This function, unlike
		/// <c>get_ToUpperCh</c>, will apply multicharacter conversions if Unicode specifies them.
		/// Note that this is not usually useful, because if you want title case, you usually
		/// want to convert only the first nonpunctuation character, or perhaps the first of
		/// each word. However, this at least provides a way to get at multicharacter
		/// conversions by passing a single character input string.
		/// If cchOut is zero, just return the length needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ToTitleRgch(string _rgchIn, int cchIn, ArrayPtr _rgchOut, int cchOut, out int _cchRet)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a string to upper case. Characters that are not Ll or Lt pass through
		/// unchanged. Note that the output may be longer than the input! This function, unlike
		/// <c>get_ToUpperCh</c>, will apply multicharacter conversions if Unicode specifies them.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ToUpper(string bstr)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a string to upper case. Characters that are not Ll or Lt pass through
		/// unchanged. Note that the output may be longer than the input! This function, unlike
		/// <c>get_ToUpperCh</c>, will apply multicharacter conversions if Unicode specifies them.
		/// If cchOut is zero, just return the length needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ToUpperRgch(string _rgchIn, int cchIn, ArrayPtr _rgchOut, int cchOut, out int _cchRet)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the Bidi category of a character as defined in the Unicode standard.
		/// See <see href="www.unicode.org/Public/UNIDATA/UnicodeData.html#Bidirectional%20Category">www.unicode.org/Public/UNIDATA/UnicodeData.html#Bidirectional%20Category</see>
		/// See <see href="www.unicode.org/unicode/reports/tr9/">www.unicode.org/unicode/reports/tr9/</see> for the full algorithm
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LgBidiCategory get_BidiCategory(int ch)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the official Unicode character name (character database field 1).
		/// Will also produce a somewhat helpful descriptive string for surrogate halves and
		/// PUA characters, though officially they have no name.
		/// (Warning: obtaining this for the first character in each page will be somewhat slow,
		/// and will use up something like 10K of RAM).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string get_CharacterName(int ch)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the character's combining class (Unicode char database field 3; see
		/// <see href="www.unicode.org/Public/UNIDATA/UnicodeData.html#Canonical%20Combining%20Classes">www.unicode.org/Public/UNIDATA/UnicodeData.html#Canonical%20Combining%20Classes</see>)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int get_CombiningClass(int ch)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get any comment recorded about this character (Unicode char database field 11).
		/// Note: currently this is not implemented to obtain the rather uninteresting comments
		/// recorded in the standard UnicodeDatabase file. It will give interesting answers only
		/// if a comment is provided in the specification for this writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string get_Comment(int ch)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the decomposition of this character (Unicode char database field 5).
		/// Empty string if it does not decompose.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string get_Decomposition(int ch)
		{
			throw new NotImplementedException();
			// For testing purposes, just return the character passed.
			//return ch.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the recursive decomposition of a character. Empty string if it does not
		/// decompose at all.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string get_FullDecomp(int ch)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the exact character category as defined in the Unicode standard.
		/// See <see href="www.unicode.org/Public/UNIDATA/UnicodeData.html#General%20Category">www.unicode.org/Public/UNIDATA/UnicodeData.html#General%20Category</see>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LgGeneralCharCategory get_GeneralCategory(int ch)
		{
			switch (ch)
			{
				case 0x0020: return LgGeneralCharCategory.kccZs;
				case 0x0301: return LgGeneralCharCategory.kccMc;
				case 0x5678: return LgGeneralCharCategory.kccCn; // Some fictitious codepoint for testing
				default: return LgGeneralCharCategory.kccLl;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category is Pe. See <c>get_GeneralCategory</c>
		/// Signifies closing punctuation, like right paren.
		/// ENHANCE JohnT: should we also include Pf, final quote?
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsClose(int ch)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category is Cc. See <c>get_GeneralCategory</c>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsControl(int ch)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category starts with L. See <c>get_GeneralCategory</c>
		/// If this returns true, exactly one of <c>get_IsUpper</c>, <c>get_IsLower</c>,
		/// <c>get_IsTitle</c>, <c>get_IsModifier</c>, or <c>get_IsOtherLetter</c> will return true.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsLetter(int ch)
		{
			return (ch != 0x0301 && ch != 0x5678 && ch != 0x0020 && !get_IsNumber(ch));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category is Ll. See <c>get_GeneralCategory</c>
		/// In languages without case all characters are considered lower case.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsLower(int ch)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category starts with M. See <c>get_GeneralCategory</c>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsMark(int ch)
		{
			return (ch == 0x0301);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category is Lm. See <c>get_GeneralCategory</c>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsModifier(int ch)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category starts with N. See <c>get_GeneralCategory</c>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsNumber(int ch)
		{
			return (ch >= (int)'1' && ch <= (int)'9') || ch == 0x00B2 || ch == 0x00B3 ||
				ch == 0x00B9 || (ch >= 0x2070 && ch <= 0x2079) || (ch >= 0x2080 && ch <= 0x2099);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category is Ps. See <c>get_GeneralCategory</c>
		/// Signifies opening punctuation, like left paren
		/// ENHANCE JohnT: Should opening include Pi, Initial quote?
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsOpen(int ch)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category starts with C. See <c>get_GeneralCategory</c>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsOther(int ch)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category is Lo. See <c>get_GeneralCategory</c>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsOtherLetter(int ch)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category starts with P. See <c>get_GeneralCategory</c>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsPunctuation(int ch)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category starts with Z. See <c>get_GeneralCategory</c>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsSeparator(int ch)
		{
			return ch == 0x0020;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category starts with S. See <c>get_GeneralCategory</c>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsSymbol(int ch)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category is Lt. See <c>get_GeneralCategory</c>
		/// This refers to "Title case" characters, typically a single code point standing for
		/// two letters, where the first is upper case and the second is lower.
		/// Unicode general category Lt, typically digraph with first upper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsTitle(int ch)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category is Lu. See <c>get_GeneralCategory</c>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsUpper(int ch)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the specified code point is a member of the specified user defined
		/// class of code points. Nathan thinks we should restrict class names to a single
		/// character to make patterns containing them more readable. Example \C\Vxyz for all
		/// words containing a consonant, followed by a vowel, followed by xyz.
		/// Not yet implemented, and may not be, as it is not clear that this is the right place
		/// to store user defined character classes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsUserDefinedClass(int ch, int chClass)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if character is considered to be part of a word (by default, this
		/// corresponds to Unicode general category Mc, Mn, and categories starting with L.
		/// ENHANCE: Eventually, this method may need to be altered or replaced to acocunt
		/// for the reality that some languages have context rules to determine whether a
		/// character is wordforming or not (e.g., apostrophes).
		/// See <c>get_GeneralCategory</c>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsWordForming(int ch)
		{
			return get_IsLetter(ch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if Unicode general category is Pc. See <c>get_GeneralCategory</c>
		/// Signifies middle of a word, like hyphen.
		/// ENHANCE JohnT: should we also include Pd, dash
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool get_IsWordMedial(int ch)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the character's value as a number.(Field 8 in
		/// <see href="www.unicode.org/Public/UNIDATA/UnicodeData.html">www.unicode.org/Public/UNIDATA/UnicodeData.html</see>).
		/// ENHANCE JohnT: is there a more useful behavior for the fraction case?
		/// Would it be better to return 0 for all cases where we don't know a useful answer?
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int get_NumericValue(int ch)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts a string into another string in which all characters are replaced with their
		/// sound alike equivalent. For example, if for this language s and z where specified to
		/// sound alike, we could take s as the generic form and convert all z's to this form.
		/// Note that we need to support the possibility that 0 (empty code point) and x and y
		/// sound alike which means that all x's and 's and y's will be ignored when testing for
		/// sound alikeness.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string get_SoundAlikeKey(string bstrValue)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a character to lower case if it is Lu or Lt; otherwise return it unchanged.
		/// ENHANCE JohnT: should we convert characters that are not Lu or Lt but for which
		/// UnicodeData.txt specifies a case conversion?
		/// See <c>ToLower</c> to convert an entire string, or to make use of Unicode mulicharacter
		/// conversions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int get_ToLowerCh(int ch)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a character to title case if it is Lu or Ll; otherwise return it unchanged.
		/// Usually this is the same as converting it to upper case.
		/// ENHANCE JohnT: should we convert characters that are not Lu or Ll but for which
		/// UnicodeData.txt specifies a case conversion?
		/// See <c>ToTitle</c> to convert an entire string, or to make use of Unicode mulicharacter
		/// conversions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int get_ToTitleCh(int ch)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a character to upper case if it is Ll or Lt; otherwise return it unchanged.
		/// ENHANCE JohnT: should we convert characters that are not Ll or Lt but for which
		/// UnicodeData.txt specifies a case conversion?
		/// See <c>ToUpper</c> to convert an entire string, or to make use of Unicode mulicharacter
		/// conversions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int get_ToUpperCh(int ch)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the text for the LineBreakBefore and the LineBreakAfter functions to use.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void set_LineBreakText(string _rgchIn, int cchMax)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
