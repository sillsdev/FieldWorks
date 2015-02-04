// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ValidCharacters.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using Palaso.Extensions;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;
using SIL.WritingSystems;

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
		private static readonly string[] DefaultWordformingChars = { "'", "-",
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

		private readonly List<string> m_wordFormingCharacters = new List<string>();
		private readonly List<string> m_numericCharacters = new List<string>();
		private readonly List<string> m_otherCharacters = new List<string>();
		private readonly ILgCharacterPropertyEngine m_cpe;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't allow anyone outside this class to create an instance (protected to allow for
		/// testing of methods that don't require the Load method to be called -- subclasses
		/// must call Init or initialize the lists independently.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ValidCharacters()
		{
			m_cpe = LgIcuCharPropEngineClass.Create();
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
		public static ValidCharacters Load(WritingSystem ws, LoadExceptionDelegate exceptionHandler, string legacyOverridesFile)
		{
			var validChars = new ValidCharacters();
			validChars.LoadException += exceptionHandler;

			var invalidChars = new List<string>();
			validChars.AddCharactersFromWritingSystem(ws, "main", ValidCharacterType.WordForming, invalidChars);
			validChars.AddCharactersFromWritingSystem(ws, "numeric", ValidCharacterType.Numeric, invalidChars);
			validChars.AddCharactersFromWritingSystem(ws, "punctuation", ValidCharacterType.Other, invalidChars);
			validChars.m_legacyOverridesFile = legacyOverridesFile;

			if (invalidChars.Count > 0)
			{
				var bldr = new StringBuilder();
				bldr.AppendFormat("Invalid ValidChars field while loading the {0} writing system. The following characters are invalid:",
					ws.DisplayLabel);
				foreach (string chr in invalidChars)
				{
					bldr.Append(Environment.NewLine);
					bldr.Append("\t");
					bldr.AppendFormat("{0} (U+{1:X4}", chr, (int) chr[0]);
					for (int ich = 1; ich < chr.Length; ich++)
						bldr.AppendFormat(", U+{0:X4}", (int) chr[ich]);
					bldr.Append(")");
				}
				validChars.ReportError(new ArgumentException(bldr.ToString(), "ws"));
			}

			if (invalidChars.Count > 0 && validChars.m_wordFormingCharacters.Count == 0)
				validChars.AddDefaultWordformingCharOverrides();

			validChars.InitSortComparer(ws);

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

		private void AddCharactersFromWritingSystem(WritingSystem ws, string charSetType, ValidCharacterType validCharType, List<string> invalidChars)
		{
			CharacterSetDefinition charSet;
			if (!ws.CharacterSets.TryGetItem(charSetType, out charSet))
				return;

			foreach (string chr in charSet.Characters)
			{
				if (TsStringUtils.IsValidChar(chr, m_cpe))
					AddCharacter(chr, validCharType);
				else
					invalidChars.Add(chr);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default word forming overrides.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IEnumerable<string> DefaultWordFormingOverrides
		{
			get { return ParseLegacyWordFormingCharOverrides(m_legacyOverridesFile) ?? DefaultWordformingChars; }
		}
		#endregion

		#region Public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a collection of valid word forming characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<string> WordFormingCharacters
		{
			get { return m_wordFormingCharacters; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a collection of valid numeric characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<string> NumericCharacters
		{
			get { return m_numericCharacters; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a collection of valid punctuation, symbol, control and whitespace characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<string> OtherCharacters
		{
			get { return m_otherCharacters; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of all the valid characters (i.e. those from all the lists).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<string> AllCharacters
		{
			get { return m_wordFormingCharacters.Concat(m_numericCharacters).Concat(m_otherCharacters); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the space delimited list containing all the valid characters from each list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string SpaceDelimitedList
		{
			get { return MakeCharString(AllCharacters, " "); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of valid characters that are letters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int WordformingLetterCount
		{
			get
			{
				// ENHANCE (TomB): If we want to be able to call this when editing a writing
				// system with recently added (not saved) characters, this method needs to
				// ask the language definition for the category.
				int count = 0;
				foreach (string chStr in m_wordFormingCharacters)
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
			List<string> listFrom = (moveToWordForming ? m_otherCharacters : m_wordFormingCharacters);
			List<string> listTo = (moveToWordForming ? m_wordFormingCharacters : m_otherCharacters);

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
			return (m_wordFormingCharacters.Contains(chr) || m_cpe.get_IsWordForming(chr[0]));
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

			return (m_wordFormingCharacters.Contains(chr.ToString(CultureInfo.InvariantCulture)) || m_cpe.get_IsWordForming(chr));
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

			return m_wordFormingCharacters.Contains(chr) && !m_cpe.get_IsWordForming(chr[0]);
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
			return m_wordFormingCharacters.Contains(chr) || m_numericCharacters.Contains(chr) ||
				m_otherCharacters.Contains(chr);
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
				return ((m_otherCharacters == null || m_otherCharacters.Count == 0) &&
					(m_numericCharacters == null || m_numericCharacters.Count == 0) &&
					(m_wordFormingCharacters == null || m_wordFormingCharacters.Count == 0));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears all the valid characters, but leaves a space.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Reset()
		{
			m_wordFormingCharacters.Clear();
			m_numericCharacters.Clear();
			m_otherCharacters.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the characters from the given list, placing each character in the correct
		/// group according to its Unicode category.
		/// </summary>
		/// <param name="characters">The list of characters.</param>
		/// <returns>A bit-mask value indicating which types of characters were added</returns>
		/// ------------------------------------------------------------------------------------
		public ValidCharacterType AddCharacters(IEnumerable<string> characters)
		{
			var addedTypes = ValidCharacterType.None;

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
				case ValidCharacterType.WordForming: list = m_wordFormingCharacters; break;
				case ValidCharacterType.Numeric: list = m_numericCharacters; break;
				default: list = m_otherCharacters; break;
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
				foreach (string chr in DefaultWordformingChars)
				{
					if (chr[0] == codepoint)
						return ValidCharacterType.WordForming;
				}
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
			m_wordFormingCharacters.AddRange(DefaultWordFormingOverrides);
			Sort(m_wordFormingCharacters);
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
			var removedTypes = ValidCharacterType.None;

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
			if (m_wordFormingCharacters.Contains(chr))
			{
				m_wordFormingCharacters.Remove(chr);
				return ValidCharacterType.WordForming;
			}

			if (m_numericCharacters.Contains(chr))
			{
				m_numericCharacters.Remove(chr);
				return ValidCharacterType.Numeric;
			}

			if (m_otherCharacters.Contains(chr))
			{
				m_otherCharacters.Remove(chr);
				return ValidCharacterType.Other;
			}

			return ValidCharacterType.None;
		}

		/// <summary>
		/// Saves the valid characters to the specified writing system.
		/// </summary>
		public void SaveTo(WritingSystem ws)
		{
			AddCharactersToWritingSystem(ws, "main", m_wordFormingCharacters);
			AddCharactersToWritingSystem(ws, "punctuation", m_otherCharacters);
			AddCharactersToWritingSystem(ws, "numeric", m_numericCharacters);
		}

		private void AddCharactersToWritingSystem(WritingSystem ws, string charSetType, List<string> characters)
		{
			if (characters.Count == 0)
			{
				ws.CharacterSets.Remove(charSetType);
			}
			else
			{
				CharacterSetDefinition charSet;
				if (!ws.CharacterSets.TryGetItem(charSetType, out charSet))
				{
					charSet = new CharacterSetDefinition(charSetType);
					ws.CharacterSets.Add(charSet);
				}
				charSet.Characters.Clear();
				foreach (string chr in characters)
					charSet.Characters.Add(chr);
			}
		}

		#endregion

		#region Sort methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the lists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitSortComparer(WritingSystem ws)
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
			Sort(m_wordFormingCharacters);
			Sort(m_numericCharacters);
			Sort(m_otherCharacters);
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
		public static string MakeCharString(IEnumerable<string> chars, string delimiter)
		{
			bool prependDelimiter = false;
			var bldr = new StringBuilder();
			foreach (string ch in chars)
			{
				if (ch == delimiter)
				{
					prependDelimiter = true;
				}
				else
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses the legacy word-forming character overrides XML file at the specified path
		/// in to a list of character strings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public static IEnumerable<string> ParseLegacyWordFormingCharOverrides(string path)
		{
			if (!File.Exists(path))
				return null;

			try
			{
				var doc = new XmlDocument();
				doc.Load(path);

				var result = new List<string>();
				XmlNodeList charsList = doc.SelectNodes("/wordFormingCharacterOverrides/wordForming");
				if (charsList != null)
				{
					foreach (XmlNode charNode in charsList)
					{
						string codepointStr = charNode.Attributes["val"].InnerText;
						int codepoint = Convert.ToInt32(codepointStr, 16);
						var c = (char) codepoint;
						result.Add(c.ToString(CultureInfo.InvariantCulture));
					}
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
