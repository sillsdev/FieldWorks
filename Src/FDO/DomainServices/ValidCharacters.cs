// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ValidCharacters.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.IO;
using System.Xml;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using System.Xml.Serialization;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// <summary>
	/// Enumeration of valid character types
	/// </summary>
	[Flags]
	public enum ValidCharacterType
	{
		/// <summary>None</summary>
		None = 0,
		/// <summary>Word-forming</summary>
		WordForming = 1,
		/// <summary>Numeric</summary>
		Numeric = 2,
		/// <summary>Punctuation, Symbol, Control, or Whitespace</summary>
		Other = 4,
		/// <summary>Flag to indicate all types of characters (not used for an individual character)</summary>
		All = WordForming | Numeric | Other,
		/// <summary>A character which is defined but whose type has not been determined</summary>
		DefinedUnknown = 8,
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class for dealing with valid characters, parsing lists of characters and categorizing
	/// them correctly. This is used to support the Valid Characters dialog box and the
	/// LgWrtitingSystem.ValidChars property.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ValidCharacters
	{
		#region Constants
		/// <summary>This string is used to replace a space character when one is found
		/// in the list of Other characters just before serializing to an XML string.</summary>
		private const string kSpaceReplacment = "U+0020";

		private readonly string ksDelimiter = StringUtils.kszObject;

		private static string[] s_defaultWordformingChars = { "'", "-",
															  "\u200c", // ZWNJ
															  "\u200d", // ZWJ
															  "\u2070", // SUPERSCRIPT ZERO
															  "\u00b9", // SUPERSCRIPT ONE
															  "\u00b2", // SUPERSCRIPT TWO
															  "\u00b3", // SUPERSCRIPT THREE
															  "\u2074", // SUPERSCRIPT FOUR
															  "\u2075", // SUPERSCRIPT FIVE
															  "\u2076", // SUPERSCRIPT SIX
															  "\u2077", // SUPERSCRIPT SEVEN
															  "\u2078", // SUPERSCRIPT EIGHT
															  "\u2079", // SUPERSCRIPT NINE
															  "\u0f0b", // TIBETAN MARK INTERSYLLABIC TSHEG
															  "\u0f0c", // TIBETAN MARK DELIMITER TSHEG BSTAR
															  "\ua78b", // LATIN CAPITAL LETTER SALTILLO
															  "\ua78c"  // LATIN SMALL LETTER SALTILLO
															};
		#endregion

		#region Data members
		/// <summary>String containing an ORC-delimited list of word-forming characters</summary>
		//[XmlAttribute]
		public string WordForming;
		/// <summary>String containing an ORC-delimited list of numeric characters</summary>
		//[XmlAttribute]
		public string Numeric;
		/// <summary>String containing an ORC-delimited list of punctuation, symbol, control and
		/// whitespace characters</summary>
		//[XmlAttribute]
		public string Other;

		private List<string> m_WordFormingCharacters;
		private List<string> m_NumericCharacters;
		private List<string> m_OtherCharacters;
		private ILgCharacterPropertyEngine m_cpe;
		private TsStringComparer m_comparer;
		private string m_legacyOverridesFile;

		#endregion

		#region error-handling delegate/event
		/// <summary>Fired if valid character data cannot be loaded</summary>
		/// <param name="e">The exception</param>
		public delegate void LoadExceptionDelegate(ArgumentException e);

		/// <summary>Fired if valid character data cannot be loaded</summary>
		public event LoadExceptionDelegate LoadException;
		#endregion

		private static bool s_fTestingMode = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't allow anyone outside this class to create an instance (protected to allow for
		/// testing of methods that don't require the Load method to be called -- subclasses
		/// must call Init or initialize the lists independently.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ValidCharacters()
		{
		}

		#region Methods and Properties to load and initialize the class

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the valid characters from the specified language definition into a new
		/// instance of the <see cref="ValidCharacters"/> class.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		/// <param name="exceptionHandler">The exception handler to use if valid character data
		/// cannot be loaded.</param>
		/// <param name="legacyOverridesFile"></param>
		/// <returns>A <see cref="ValidCharacters"/> initialized with the valid characters data
		/// from the language definition.</returns>
		/// ------------------------------------------------------------------------------------
		public static ValidCharacters Load(IWritingSystem ws, LoadExceptionDelegate exceptionHandler, string legacyOverridesFile)
		{
			ValidCharacters validChars = Load(ws.ValidChars, ws.DisplayLabel, ws,
				exceptionHandler, legacyOverridesFile);
			if (validChars != null)
				validChars.InitSortComparer(ws);

			return validChars;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the specified XML source to initialize a new instance of the
		/// <see cref="ValidCharacters"/> class.
		/// </summary>
		/// <param name="xmlSrc">The XML source representation.</param>
		/// <param name="wsName">The name of the writing system that is being loaded</param>
		/// <param name="ws">The writing system</param>
		/// <param name="exceptionHandler">The exception handler to use if valid character data
		/// cannot be loaded.</param>
		/// <param name="legacyOverridesFile"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ValidCharacters Load(string xmlSrc, string wsName,
			IWritingSystem ws, LoadExceptionDelegate exceptionHandler, string legacyOverridesFile)
		{
			Exception e;
			var validChars = XmlSerializationHelper.DeserializeFromString<ValidCharacters>(xmlSrc, out e);

			bool fTryOldStyleList = false;

			if (validChars != null)
			{
				validChars.LoadException += exceptionHandler;
			}
			else
			{
				validChars = new ValidCharacters();
				validChars.LoadException += exceptionHandler;
				if (e != null)
					fTryOldStyleList = !DataAppearsToBeMalFormedXml(xmlSrc);
				if (!fTryOldStyleList && !String.IsNullOrEmpty(xmlSrc))
				{
					var bldr = new StringBuilder();
					bldr.AppendFormat("Invalid ValidChars field while loading the {0} writing system:", wsName);
					bldr.Append(Environment.NewLine);
					bldr.Append("\t");
					bldr.Append(xmlSrc);
					validChars.ReportError(new ArgumentException(bldr.ToString(), "xmlSrc", e));
				}
			}
			validChars.m_legacyOverridesFile = legacyOverridesFile;

			List<string> invalidChars = validChars.Init();

			if (fTryOldStyleList)
			{
				e = null;
				List<string> list = ParseCharString(xmlSrc, " ", validChars.m_cpe, out invalidChars);
				validChars.AddCharacters(list);
			}

			if (invalidChars.Count > 0)
			{
				var bldr = new StringBuilder();
				bldr.AppendFormat("Invalid ValidChars field while loading the {0} writing system. The following characters are invalid:",
					wsName);
				foreach (string chr in invalidChars)
				{
					bldr.Append(Environment.NewLine);
					bldr.Append("\t");
					bldr.AppendFormat("{0} (U+{1:X4}", chr, (int)chr[0]);
					for (int ich = 1; ich < chr.Length; ich++)
						bldr.AppendFormat(", U+{0:X4}", (int)chr[ich]);
					bldr.Append(")");
				}
				validChars.ReportError(new ArgumentException(bldr.ToString(), "xmlSrc"));
			}

			if ((e != null || invalidChars.Count > 0) && validChars.m_WordFormingCharacters.Count == 0)
				validChars.AddDefaultWordformingCharOverrides();

			return validChars;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reports a data error resulting from a hapless attempt to load the valid characters.
		/// </summary>
		/// <param name="e">The argument exception.</param>
		/// ------------------------------------------------------------------------------------
		private void ReportError(ArgumentException e)
		{
			if (LoadException == null)
				throw e;
			LoadException(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Evaluates the given string to determine whether it is apparently mal-formed XML
		/// data for the ValidCharacters field. This allows us to avoid trying to interpret
		/// mal-formed XML data as if it were an old-style space-delimited list of characters.
		/// </summary>
		/// <param name="sData">String to evalutate.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static bool DataAppearsToBeMalFormedXml(string sData)
		{
			return sData.Contains("WordForming") ||
				sData.Contains("Numeric") ||
				sData.Contains("Other");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if we have the new style of valid characters information in the
		/// specified string. Currently this is adequately detected by being able to interpret
		/// it as XML.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool IsNewValidCharsString(string xmlSrc)
		{
			return XmlSerializationHelper.DeserializeFromString<ValidCharacters>(xmlSrc) != null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes this object.
		/// </summary>
		/// <returns>The list of invalid characters encountered.</returns>
		/// ------------------------------------------------------------------------------------
		private List<string> Init()
		{
			m_cpe = LgIcuCharPropEngineClass.Create();

			Reset();

			if (Other != null)
				Other = Other.Replace(kSpaceReplacment, " ");

			List<string> invalidChars;
			m_WordFormingCharacters = ParseCharString(WordForming, ksDelimiter, m_cpe, out invalidChars);
			List<string> invalidCharsTemp;
			m_NumericCharacters = ParseCharString(Numeric, ksDelimiter, m_cpe, out invalidCharsTemp, m_WordFormingCharacters);
			invalidChars.AddRange(invalidCharsTemp);
			m_OtherCharacters = ParseCharString(Other, ksDelimiter, m_cpe, out invalidCharsTemp, m_WordFormingCharacters,
				m_NumericCharacters);
			invalidChars.AddRange(invalidCharsTemp);
			return invalidChars;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default word forming overrides.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IEnumerable<string> DefaultWordFormingOverrides
		{
			get
			{
				if (s_fTestingMode)
					return s_defaultWordformingChars;
				return ParseLegacyWordFormingCharOverrides(m_legacyOverridesFile) ??
					(IEnumerable<string>)s_defaultWordformingChars;
			}
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a collection of valid word forming characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IEnumerable<string> WordFormingCharacters
		{
			get { return m_WordFormingCharacters; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a collection of valid numeric characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IEnumerable<string> NumericCharacters
		{
			get { return m_NumericCharacters; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a collection of valid punctuation, symbol, control and whitespace characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public IEnumerable<string> OtherCharacters
		{
			get { return m_OtherCharacters; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of all the valid characters (i.e. those from all the lists).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public List<string> AllCharacters
		{
			get
			{
				List<string> chars = new List<string>(m_WordFormingCharacters);
				chars.AddRange(m_NumericCharacters);
				chars.AddRange(m_OtherCharacters);
				return chars;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the space delimited list containing all the valid characters from each list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public string SpaceDelimitedList
		{
			get { return MakeCharString(AllCharacters, " "); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the XML string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public string XmlString
		{
			get
			{
				WordForming = MakeCharString(m_WordFormingCharacters, ksDelimiter);
				Numeric = MakeCharString(m_NumericCharacters, ksDelimiter);
				Other = MakeCharString(m_OtherCharacters, ksDelimiter);
				Other = Other.Replace(" ", kSpaceReplacment);
				return XmlSerializationHelper.SerializeToString(this);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of valid characters that are letters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public int WordformingLetterCount
		{
			get
			{
				// ENHANCE (TomB): If we want to be able to call this when editing a writing
				// system with recently added (not saved) characters, this method needs to
				// ask the language definition for the category.
				int count = 0;
				foreach (string chStr in m_WordFormingCharacters)
				{
					if (chStr.Length > 1 || m_cpe.get_IsLetter(chStr[0]))
						count++;
				}
				return count;
			}
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the between word forming and other.
		/// </summary>
		/// <param name="chars">The chars.</param>
		/// <param name="moveToWordForming">If set to <c>true</c> move characters from Other list
		/// to Word Forming list. If set to <c>false</c> move characters from Word Forming list
		/// to Other list.</param>
		/// <exception cref="ArgumentException">If chars contains characters other than symbol
		/// or punctuation characters or characters which are not in the source list.</exception>
		/// ------------------------------------------------------------------------------------
		public void MoveBetweenWordFormingAndOther(List<string> chars, bool moveToWordForming)
		{
			List<string> listFrom = (moveToWordForming ? m_OtherCharacters : m_WordFormingCharacters);
			List<string> listTo = (moveToWordForming ? m_WordFormingCharacters : m_OtherCharacters);

			foreach (string chr in chars)
			{
				if (!CanBeWordFormingOverride(chr))
					throw new ArgumentException("Only symbol or punctuation characters can be moved between word-forming and other lists.", "chars");

				if (!listFrom.Remove(chr))
					throw new ArgumentException("Attempt to remove character that is not in the list.", "chars");
				listTo.Add(chr);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given character is word forming, either by virue of its
		/// inclusion in the user-defined list or in the ICU.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsWordForming(string chr)
		{
			if (string.IsNullOrEmpty(chr))
				return false;
			return (m_WordFormingCharacters.Contains(chr) || m_cpe.get_IsWordForming(chr[0]));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the given character is word forming, either by virue of its
		/// inclusion in the user-defined list or in the ICU.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsWordForming(char chr)
		{
			if (chr == 0)
				return false;

			return (m_WordFormingCharacters.Contains(chr.ToString()) || m_cpe.get_IsWordForming(chr));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the specified character is a word-forming character the
		/// user has explicitly set to word-forming, but ICU does not think it's word-forming.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsWordFormingOverride(string chr)
		{
			if (string.IsNullOrEmpty(chr))
				return false;

			return m_WordFormingCharacters.Contains(chr) && !m_cpe.get_IsWordForming(chr[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified character is not a word-forming character
		/// according to ICU, but should be allowed to be a word-forming override.
		/// </summary>
		/// <param name="chr">The character to test</param>
		/// <returns><c>true</c> if the specified character is able to be overridden to be
		/// word-forming (i.e., is a punctuation or symbol character according to ICU or is one
		/// of the special exceptions);
		/// <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool CanBeWordFormingOverride(string chr)
		{
			if (string.IsNullOrEmpty(chr) || chr.Length > 1)
				return false;

			int code = chr[0];

			if (code == 0x200C || code == 0x200D)
				return true; // Zero-width non-joiner or zero-width joiner

			LgGeneralCharCategory chrCategory = m_cpe.get_GeneralCategory(code);

			if (chrCategory == LgGeneralCharCategory.kccSc ||
				chrCategory == LgGeneralCharCategory.kccSk ||
				chrCategory == LgGeneralCharCategory.kccSm ||
				chrCategory == LgGeneralCharCategory.kccSo)
			{
				return true; // symbol
			}

			if (chrCategory == LgGeneralCharCategory.kccPc ||
					chrCategory == LgGeneralCharCategory.kccPd ||
					chrCategory == LgGeneralCharCategory.kccPe ||
					chrCategory == LgGeneralCharCategory.kccPf ||
					chrCategory == LgGeneralCharCategory.kccPi ||
					chrCategory == LgGeneralCharCategory.kccPo ||
					chrCategory == LgGeneralCharCategory.kccPs)
			{
				return true; // punctuation
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified character is valid (i.e., is in one of the lists).
		/// </summary>
		/// <param name="chr">The character to be tested.</param>
		/// ------------------------------------------------------------------------------------
		public bool IsValid(string chr)
		{
			return m_WordFormingCharacters.Contains(chr) || m_NumericCharacters.Contains(chr) ||
				m_OtherCharacters.Contains(chr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsEmpty
		{
			get
			{
				return ((m_OtherCharacters == null || m_OtherCharacters.Count == 0) &&
					(m_NumericCharacters == null || m_NumericCharacters.Count == 0) &&
					(m_WordFormingCharacters == null || m_WordFormingCharacters.Count == 0));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears all the valid characters, but leaves a space.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Reset()
		{
			m_WordFormingCharacters = new List<string>();
			m_NumericCharacters = new List<string>();
			m_OtherCharacters = new List<string>();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the characters from the given list, placing each character in the correct
		/// group according to its Unicode category.
		/// </summary>
		/// <param name="characters">The list of characters.</param>
		/// <returns>A bit-mask value indicating which types of characters were added</returns>
		/// ------------------------------------------------------------------------------------
		public ValidCharacterType AddCharacters(List<string> characters)
		{
			ValidCharacterType addedTypes = ValidCharacterType.None;

			if (characters != null)
			{
				foreach (string chr in characters)
					addedTypes |= AddCharacter(chr, ValidCharacterType.DefinedUnknown, false);
			}

			SortLists();
			return addedTypes;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the given character, placing it in the correct group according to its Unicode
		/// category. The list to which the character is added is sorted after the character
		/// is added.
		/// </summary>
		/// <param name="chr">The character, which can consist of a base character as well as
		/// possibly some combining characters (diacritics, etc.).</param>
		/// <returns>A enumeration value indicating which type of character was added</returns>
		/// ------------------------------------------------------------------------------------
		public ValidCharacterType AddCharacter(string chr)
		{
			return AddCharacter(chr, ValidCharacterType.DefinedUnknown);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the given character, placing it in the correct group according to its Unicode
		/// category. The list to which the character is added is sorted after the character
		/// is added.
		/// </summary>
		/// <param name="chr">The character, which can consist of a base character as well as
		/// possibly some combining characters (diacritics, etc.).</param>
		/// <param name="type">Type of character being added, if known (really only needed for
		/// not-yet-defined PUA characters)</param>
		/// <returns>A enumeration value indicating which type of character was added</returns>
		/// ------------------------------------------------------------------------------------
		public ValidCharacterType AddCharacter(string chr, ValidCharacterType type)
		{
			return AddCharacter(chr, type, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the given character, placing it in the correct group according to its Unicode
		/// category.
		/// </summary>
		/// <param name="chr">The character, which can consist of a base character as well as
		/// possibly some combining characters (diacritics, etc.).</param>
		/// <param name="type">Type of character being added, if known (really only needed for
		/// not-yet-defined PUA characters)</param>
		/// <param name="sortAfterAdd">Inidicates whether or not to sort the list after
		/// the characters is added.</param>
		/// <returns>A enumeration value indicating which type of character was added</returns>
		/// ------------------------------------------------------------------------------------
		public ValidCharacterType AddCharacter(string chr, ValidCharacterType type,
			bool sortAfterAdd)
		{
			if (string.IsNullOrEmpty(chr) || IsValid(chr))
				return ValidCharacterType.None;

			int codepoint = chr[0];
			if (type == ValidCharacterType.DefinedUnknown)
				type = GetNaturalCharType(codepoint);
			List<string> list;
			switch(type)
			{
				case ValidCharacterType.WordForming: list = m_WordFormingCharacters; break;
				case ValidCharacterType.Numeric: list = m_NumericCharacters; break;
				default: list = m_OtherCharacters; break;
			}

			list.Add(chr);
			if (sortAfterAdd)
				Sort(list);
			return type;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the natural type (i.e., not overridden) of the given character code point. This
		/// is essentially based on ICU, but we make one exception to force superscripted
		/// numbers to be considered as word-forming since these are generally used to mark
		/// tone in the vernaculars we work with (see TE-8384).
		/// </summary>
		/// <param name="codepoint">The codepoint.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual ValidCharacterType GetNaturalCharType(int codepoint)
		{
			if (m_cpe.get_IsWordForming(codepoint))
				return ValidCharacterType.WordForming;
			if (Icu.IsNumeric(codepoint))
			{
				foreach (string chr in s_defaultWordformingChars)
					if ((int)chr[0] == codepoint)
						return ValidCharacterType.WordForming;
				return ValidCharacterType.Numeric;
			}
			return ValidCharacterType.Other;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the characters from the legacy word-forming character overrides file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddDefaultWordformingCharOverrides()
		{
			m_WordFormingCharacters.AddRange(DefaultWordFormingOverrides);
			Sort(m_WordFormingCharacters);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the characters in the specified list from the list of valid characters.
		/// The return value is a bitmask value indicating from what lists characters were
		/// removed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ValidCharacterType RemoveCharacters(List<string> characters)
		{
			ValidCharacterType removedTypes = ValidCharacterType.None;

			if (characters != null)
			{
				foreach (string chr in characters)
					removedTypes |= RemoveCharacter(chr);
			}

			return removedTypes;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the specified character from the list it's in and returns the type of the
		/// list from which it was removed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ValidCharacterType RemoveCharacter(string chr)
		{
			if (m_WordFormingCharacters.Contains(chr))
			{
				m_WordFormingCharacters.Remove(chr);
				return ValidCharacterType.WordForming;
			}

			if (m_NumericCharacters.Contains(chr))
			{
				m_NumericCharacters.Remove(chr);
				return ValidCharacterType.Numeric;
			}

			if (m_OtherCharacters.Contains(chr))
			{
				m_OtherCharacters.Remove(chr);
				return ValidCharacterType.Other;
			}

			return ValidCharacterType.None;
		}

		#endregion

		#region Sort methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the lists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitSortComparer(IWritingSystem ws)
		{
			if (m_comparer != null && m_comparer.WritingSystem != ws)
				m_comparer = null;

			if (m_comparer == null && ws != null)
				m_comparer = new TsStringComparer(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the lists of valid characters using ICU. No-op unless InitSortComparer has
		/// already been called.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SortLists()
		{
			Sort(m_WordFormingCharacters);
			Sort(m_NumericCharacters);
			Sort(m_OtherCharacters);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the list corresponding to the specified type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void Sort(List<string> list)
		{
			if (list != null && list.Count > 1 && m_comparer != null)
				list.Sort(m_comparer.Compare);
		}

		#endregion

		#region Other static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the list of valid characters in the specified list and combines them into a
		/// delimited string using the specified delimiter character. If the delimiter is in
		/// the list of characters, it will be the first character in the string that is
		/// returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string MakeCharString(List<string> chars, string delimiter)
		{
			bool prependDelimiter = chars.Contains(delimiter);
			StringBuilder bldr = new StringBuilder(chars.Count * 2);
			foreach (string ch in chars)
			{
				if (ch != delimiter)
				{
					bldr.Append(ch);
					bldr.Append(delimiter);
				}
			}
			if (bldr.Length > 1)
				bldr.Length--; // Remove final delimiter

			if (prependDelimiter)
				bldr.Insert(0, delimiter);

			return bldr.ToString();
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Parses the specified string into a list of characters. The unparsed list is a
		/// string of valid characters delimited with the specified delimiter.
		/// </summary>
		/// <param name="chars">The string containing a delimited list of characters.</param>
		/// <param name="delimiter">The delimiter (passed as a string, but really just a single
		/// character).</param>
		/// <param name="cpe">The character property engine.</param>
		/// <param name="invalidChars">The list of invalid characters encountered.</param>
		/// <param name="otherLists">Collection of other lists to check to prevent a character
		/// from being added to multiple lists.</param>
		/// <returns>List of unique characters</returns>
		/// --------------------------------------------------------------------------------
		private static List<string> ParseCharString(string chars, string delimiter,
			ILgCharacterPropertyEngine cpe, out List<string> invalidChars,
			params List<string>[] otherLists)
		{
			List<string> charlist = TsStringUtils.ParseCharString(chars, delimiter, cpe,
				out invalidChars);

			for (int i = charlist.Count - 1; i >= 0; i--)
			{
				if (IsInAnotherList(charlist[i], otherLists))
					charlist.RemoveAt(i);
			}
			return charlist;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified character is in one of the other lists.
		/// </summary>
		/// <param name="chr">The character to look for.</param>
		/// <param name="otherLists">The collection of other lists.</param>
		/// <returns>
		/// 	<c>true</c> if the specified chatacter is in another list; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private static bool IsInAnotherList(string chr, List<string>[] otherLists)
		{
			if (otherLists == null || otherLists.Length == 0)
				return false;
			foreach (List<string> otherList in otherLists)
			{
				if (otherList.Contains(chr))
					return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the legacy word-forming character overrides XML file at the specified path
		/// in to a list of character strings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public static List<string> ParseLegacyWordFormingCharOverrides(string path)
		{
			if (!File.Exists(path))
				return null;

			try
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(path);

				List<string> result = new List<string>();
				XmlNodeList charsList = doc.SelectNodes("/wordFormingCharacterOverrides/wordForming");
				foreach (XmlNode charNode in charsList)
				{
					string codepointStr = charNode.Attributes["val"].InnerText;
					int codepoint = Convert.ToInt32(codepointStr, 16);
					char c = (char)codepoint;
					result.Add(c.ToString());
				}
				return result;
			}
			catch (Exception)
			{
				return null;
			}
		}

		#endregion
	}
}
