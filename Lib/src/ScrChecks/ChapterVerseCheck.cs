// ---------------------------------------------------------------------------------------------
// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ChapterVerseCheck.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using SILUBS.SharedScrUtils;

namespace SILUBS.ScriptureChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Checks for missing, repeated, extraneous, and out-of-order chapters and verses
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ChapterVerseCheck : IScriptureCheck
	{
		private enum ParseVerseResult
		{
			Valid,
			ValidWithSpaceInVerse,
			ValidWithSpaceInVerseBridge,
			Invalid,
			InvalidFormat,
		}

		private enum VersePart
		{
			NA,
			PartA,
			PartB
		}

		private IChecksDataSource m_checksDataSource;

		private readonly string ksVerseSchemeParam = "Versification Scheme";
		private readonly string ksBookIdParam = "Book ID";
		private readonly string ksChapterParam = "Chapter Number";
		private readonly string ksVerseBridgeParam = "Verse Bridge";
		private readonly string ksScriptDigitZeroParam = "Script Digit Zero";
		private readonly string ksSubVerseLetterAParam = "Sub-verse Letter A";
		private readonly string ksSubVerseLetterBParam = "Sub-verse Letter B";

		private string m_versificationScheme;
		private VersificationTable m_versification;

		private string m_subVerseA = "a";
		private string m_subVerseB = "b";

		private Regex m_verseNumberFormat;
		private Regex m_chapterNumberFormat;

		private List<ChapterToken> m_chapTokens = new List<ChapterToken>();
		private ITextToken m_fallbackToken;

		private string m_sBookId;
		private int m_nChapterToCheck; // 0 = all chapters in book
		private RecordErrorHandler m_recordError;

//		/// <summary>Verses encountered in current chapter</summary>
//		private List<int> m_versesFound;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ChapterVerseCheck"/> class.
		/// </summary>
		/// <param name="checksDataSource">The checks data source.</param>
		/// ------------------------------------------------------------------------------------
		public ChapterVerseCheck(IChecksDataSource checksDataSource) : this(checksDataSource, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ChapterVerseCheck"/> class. This
		/// overload of the constructor is only used for testing.
		/// </summary>
		/// <param name="checksDataSource">The checks data source.</param>
		/// <param name="recErrHandler">The error recording handler.</param>
		/// ------------------------------------------------------------------------------------
		public ChapterVerseCheck(IChecksDataSource checksDataSource,
			RecordErrorHandler recErrHandler)
		{
			m_checksDataSource = checksDataSource;
			m_recordError = recErrHandler;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a localized version of the specified string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string Localize(string strToLocalize)
		{
			return m_checksDataSource.GetLocalizedString(strToLocalize);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CheckName
		{
			get {return Localize("Chapter and Verse Numbers"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The unique identifier of the check. This should never be changed!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Guid CheckId { get { return StandardCheckIds.kguidChapterVerse; } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the group which contains this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CheckGroup
		{
			get { return Localize("Basic"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a number that can be used to order this check relative to other checks in the
		/// same group when displaying checks in the UI.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public float RelativeOrder
		{
			get { return 100; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the description for this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Description
		{
			get { return Localize("Checks for potential inconsistencies in chapter and verse numbers."); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the record error handler. Use this only for tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RecordErrorHandler RecordError
		{
			set { m_recordError = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the given tokens for chapter/verse errors and calls the given RecordError
		/// handler for each one.
		/// </summary>
		/// <param name="toks">The tokens to check.</param>
		/// <param name="record">Method to call to record errors.</param>
		/// ------------------------------------------------------------------------------------
		public void Check(IEnumerable<ITextToken> toks, RecordErrorHandler record)
		{
			GetParameters();

			m_recordError = record;
//			m_versesFound = new List<int>();
			m_chapTokens.Clear();

			ChapterToken currChapterToken = null;
			VerseToken currVerseToken = null;

			foreach (ITextToken token in toks)
			{
				// This token is only necessary when a chapter one is missing
				// and we need a token to use for reporting that it's missing.
				if (m_fallbackToken == null)
					m_fallbackToken = token;

				if (token.TextType == TextType.ChapterNumber)
				{
					currChapterToken = new ChapterToken(token, m_chapterNumberFormat);
					currVerseToken = null;
					m_chapTokens.Add(currChapterToken);
				}
				else if (token.TextType == TextType.VerseNumber)
				{
					if (currChapterToken == null)
					{
						//assume chapter one
						currChapterToken = new ChapterToken(token, 1);
						m_chapTokens.Add(currChapterToken);
					}

					currVerseToken = new VerseToken(token);
					currChapterToken.VerseTokens.Add(currVerseToken);
				}
				else if (token.TextType == TextType.Verse)
				{
					if (currChapterToken == null)
					{
						// no chapter token and no verse number token
						// oh no! use verse text token as default, but system
						// should error on missing verse first.
						if (currVerseToken == null)
						{
							//assume chapter one
							currChapterToken = new ChapterToken( token, 1);
							m_chapTokens.Add(currChapterToken);

							//assume verse one
							currVerseToken = new VerseToken(token, 1);
							currChapterToken.VerseTokens.Add(currVerseToken);
						}
						// no chapter token, but we have verse number token
						// then use the verse number token
						else
						{
							// this case should not happen because chapter tokens
							// are automatically created if a verse number token is
							// encountered first
							Debug.Assert(false, "verse number token found without chapter number token");
						}
					}
					else
					{
						// we have a chapter token, but no verse number token
						// use the chapter token as the default token.
						if (currVerseToken == null)
						{
							//assume verse one
							currVerseToken = new VerseToken(token, 1);
							currChapterToken.VerseTokens.Add(currVerseToken);
						}
						// we have a chapter token, and a verse number token
						// we are happy
						else
						{
							// do nothing
						}
					}
					currVerseToken.IncrementVerseTextCount(token);
				}
			}

			CheckChapterNumbers();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the parameters needed for this check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void GetParameters()
		{
			m_versificationScheme = m_checksDataSource.GetParameterValue(ksVerseSchemeParam);
			ScrVers scrVers;
			try
			{
				scrVers = (ScrVers)Enum.Parse(typeof(ScrVers),
					m_versificationScheme);
			}
			catch
			{
				// Default to English
				scrVers = ScrVers.English;
			}

			m_versification = VersificationTable.Get(scrVers);

			m_sBookId = m_checksDataSource.GetParameterValue(ksBookIdParam);
			if (!int.TryParse(m_checksDataSource.GetParameterValue(ksChapterParam), out m_nChapterToCheck))
				m_nChapterToCheck = 0;

			string temp = m_checksDataSource.GetParameterValue(ksVerseBridgeParam);
			string verseBridge = (string.IsNullOrEmpty(temp)) ? "-" : temp;

			temp = m_checksDataSource.GetParameterValue(ksScriptDigitZeroParam);
			char scriptDigitZero = (string.IsNullOrEmpty(temp)) ? '0' : temp[0];
			string numberRange = string.Format("[{1}-{2}][{0}-{2}]*", scriptDigitZero,
				(char)(scriptDigitZero + 1), (char)(scriptDigitZero + 9));

			temp = m_checksDataSource.GetParameterValue(ksSubVerseLetterAParam);
			if (!string.IsNullOrEmpty(temp))
				m_subVerseA = temp;

			temp = m_checksDataSource.GetParameterValue(ksSubVerseLetterBParam);
			if (!string.IsNullOrEmpty(temp))
				m_subVerseB = temp;
			string subverseRange = string.Format("[{0}{1}]?", m_subVerseA, m_subVerseB);

			// Original Regex for Roman script: "^[1-9][0-9]{0,2}[ab]?(-[1-9][0-9]{0,2}[ab]?)?$"
			m_verseNumberFormat = new Regex(String.Format("^{0}{1}({2}{0}{1})?$",
				numberRange, subverseRange, verseBridge));
			m_chapterNumberFormat = new Regex("^" + numberRange + "$");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks for missing chapters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckChapterNumbers()
		{
			int bookId = BCVRef.BookToNumber(m_sBookId);
			int lastChapInBook = m_versification.LastChapter(bookId);
			int nextExpectedChapter = 1;
			int prevChapNumber = 0;
			bool[] chaptersFound = new bool[lastChapInBook + 1];

			foreach (ChapterToken chapToken in m_chapTokens)
			{
				if (m_nChapterToCheck != 0 && chapToken.ChapterNumber != m_nChapterToCheck)
					continue;

				string msg = null;
				int errorArg = chapToken.ChapterNumber;
				ITextToken token = chapToken.Token;

				if (!chapToken.Valid)
				{
					// Chapter number is invalid
					AddError(token, 0, token.Text.Length, Localize("Invalid chapter number"), errorArg);
				}

				if (chapToken.ChapterNumber >= 1)
				{
					if (chapToken.ChapterNumber > lastChapInBook)
					{
						// Chapter number is out of range
						msg = Localize("Chapter number out of range");
					}
					else if (chapToken.ChapterNumber == prevChapNumber)
					{
						// Chapter number is repeated
						msg = Localize("Duplicate chapter number");
					}
					else if (chapToken.ChapterNumber < nextExpectedChapter)
					{
						// Chapter number is out of order
						msg = Localize("Chapter out of order; expected chapter {0}");
						errorArg = nextExpectedChapter;
					}

					if (msg != null)
						AddError(token, 0, token.Text.Length, msg, errorArg);
					else
					{
						chaptersFound[chapToken.ChapterNumber] = true;
						CheckVerseNumbers(chapToken, bookId);
					}
				}

				prevChapNumber = chapToken.ChapterNumber;
				nextExpectedChapter =
					Math.Max(chapToken.ChapterNumber + 1, nextExpectedChapter);
			}

			CheckForMissingChapters(chaptersFound);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks for missing chapters in the current book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckForMissingChapters(bool[] chaptersFound)
		{
			for (int chap = 1; chap < chaptersFound.Length; chap++)
			{
				if (chaptersFound[chap] || (m_nChapterToCheck != 0 && chap != m_nChapterToCheck))
					continue;

				// Find the first chapter token that immediately precedes where the
				// missing chapter would have a token if it weren't missing.
				ChapterToken precedingChapter = null;
				foreach (ChapterToken chapToken in m_chapTokens)
				{
					if (chapToken.ChapterNumber > chap)
						break;
					precedingChapter = chapToken;
				}

				// TODO: Deal with what token to use if a book has no chapters at all.
				// This should always succeed
				int offset = 0;
				ITextToken token = null;
				if (precedingChapter != null)
				{
					token = precedingChapter.Token;
					offset = precedingChapter.Implicit ? 0 : token.Text.Length;
				}
				else if (m_chapTokens.Count > 0)
				{
					token = m_chapTokens[0].Token;
				}

				if (token != null)
				{
					BCVRef scrRefStart = new BCVRef(BCVRef.BookToNumber(token.ScrRefString), chap, 0);
					token.MissingStartRef = scrRefStart;
					token.MissingEndRef = null;
					AddError(token, offset, 0, Localize("Missing chapter number {0}"), chap);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check verse numbers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckVerseNumbers(ChapterToken chapToken, int bookId)
		{
			int lastVrsInChap = m_versification.LastVerse(bookId, chapToken.ChapterNumber);
			int nextExpectedVerse = 1;
			bool expectingPartB = false;
			int prevVerseStart = 0;
			int prevVerseEnd = 0;
			ITextToken[] versesFound = new ITextToken[lastVrsInChap + 1];
			versesFound[0] = chapToken.Token;

			foreach (VerseToken verseToken in chapToken.VerseTokens)
			{
				ITextToken token = verseToken.VerseNumber;
				ITextToken reportedToken = token;
				string msg = null;
				int offset = 0;
				int length = token.Text.Length;
				object[] errorArgs = null;
				bool countFoundVerses = false;
				int curVerseStart;
				int curVerseEnd;
				VersePart vrsPart;

				if (verseToken.ImplicitVerseNumber == 1)
				{
					versesFound[1] = token;
					continue;
				}

				ParseVerseResult parseResult = ParseVerseNumber(token.Text,
					out curVerseStart, out curVerseEnd, out vrsPart);

				if (parseResult == ParseVerseResult.ValidWithSpaceInVerse)
				{
					// Log error telling user there are spaces before or after the verse
					// number. This means the space(s) have the verse number style. This isn't
					// considered an invalid verse number, but we do need to tell the user.
					AddError(token, 0, token.Text.Length,
						Localize("Space found in verse number"), token.Text);
				}
				else if (parseResult == ParseVerseResult.ValidWithSpaceInVerseBridge)
				{
					// Log error telling user there are spaces in a verse bridge. This
					// means the space(s) have the verse number style. This isn't considered
					// an invalid verse number, but we do need to tell the user.
					AddError(token, 0, token.Text.Length,
						Localize("Space found in verse bridge"), token.Text);
				}

				if (parseResult == ParseVerseResult.Invalid)
				{
					msg = Localize("Invalid verse number");
				}
				else if ((parseResult != ParseVerseResult.InvalidFormat) && VersesAlreadyFound(curVerseStart, curVerseEnd, versesFound) &&
					!(expectingPartB && vrsPart == VersePart.PartB))
				{
					if (AnyOverlappingVerses(curVerseStart, curVerseEnd,
						prevVerseStart, prevVerseEnd, out errorArgs))
					{
						// Duplicate verse(s) found.
						msg = (errorArgs.Length == 1 ?
							Localize("Duplicate verse number") :
							Localize("Duplicate verse numbers"));
					}
					else
					{
						// Verse number(s) are unexpected
						msg = (curVerseStart == curVerseEnd ?
							Localize("Unexpected verse number") :
							Localize("Unexpected verse numbers"));
					}
				}
				else if (AnyOverlappingVerses(curVerseStart, curVerseEnd,
					lastVrsInChap + 1, int.MaxValue, out errorArgs))
				{
					countFoundVerses = true;
					// Start and/or end verse is out of range
					msg = (errorArgs.Length == 1 ?
						Localize("Verse number out of range") :
						Localize("Verse numbers out of range"));
				}
				else if (curVerseStart < nextExpectedVerse)
				{
					// Verse number(s) are out of order
					countFoundVerses = true;
					if (nextExpectedVerse <= lastVrsInChap)
					{
						errorArgs = new object[] { nextExpectedVerse };
						msg = (curVerseStart == curVerseEnd ?
							Localize("Verse number out of order; expected verse {0}") :
							Localize("Verse numbers out of order; expected verse {0}"));
					}
					else
					{
						msg = (curVerseStart == curVerseEnd ?
							Localize("Verse number out of order") :
							Localize("Verse numbers out of order"));
					}
				}
				else if (((vrsPart == VersePart.PartB) != expectingPartB) &&
					(curVerseStart == curVerseEnd))
				{
					// Missing part A or B
					// TODO: cover cases like "4a 5-7" and "4 5b-7". This would require
					// ParseVerseNumber() to detect verse parts at the beginning of bridges.
					reportedToken = (vrsPart == VersePart.PartB ? token : versesFound[prevVerseEnd]);
					msg = Localize("Missing verse number {0}");
					offset = (vrsPart == VersePart.PartB ? 0 : reportedToken.Text.Length);
					length = 0;
					int reportedVrsNum = (vrsPart == VersePart.PartB ? curVerseStart : prevVerseEnd);
					string fmt = (vrsPart == VersePart.PartB ? "{0}a" : "{0}b");
					errorArgs = new object[] { string.Format(fmt, reportedVrsNum) };
					countFoundVerses = true;
				}
				else if ((vrsPart == VersePart.PartB && curVerseStart > prevVerseEnd) &&
					(curVerseStart == curVerseEnd))
				{
					// Missing both a part B and A
					reportedToken = versesFound[prevVerseEnd];

					AddError(reportedToken, reportedToken.Text.Length, 0,
						Localize("Missing verse number {0}"),
						new object[] { string.Format("{0}b", prevVerseEnd) });

					AddError(token, 0, 0, Localize("Missing verse number {0}"),
						new object[] { string.Format("{0}a", curVerseStart) });
				}

				if (msg != null)
				{
					// Report the error found.
					if (errorArgs == null)
						AddError(reportedToken, offset, length, msg);
					else
						AddError(reportedToken, offset, length, msg, errorArgs);
				}

				if (msg == null || countFoundVerses)
				{
					// No error was found for the current verse range so set all the verses
					// in our found verse list corresponding to those in the range.
					for (int i = curVerseStart; i <= Math.Min(curVerseEnd, lastVrsInChap); i++)
						versesFound[i] = token;
				}

				if (parseResult == ParseVerseResult.InvalidFormat)
					AddError(token, 0, token.Text.Length, Localize("Invalid verse number"), token.Text);

				// only worry about this if the chapter and/or verse tokens are in order
				if (verseToken.VerseTextCount < 1)
				{
					AddError(verseToken.VerseNumber, 0, verseToken.VerseNumber.Text.Length,
						Localize("Missing verse text in verse {0}"), verseToken.VerseNumber.Text);
				}

				// Determine next expected verse.
				// Don't expect a partB if there was an error with partA
				expectingPartB = (vrsPart == VersePart.PartA && msg == null);
				if (!expectingPartB && curVerseEnd <= lastVrsInChap)
					nextExpectedVerse = curVerseEnd + 1;

				prevVerseStart = curVerseStart;
				prevVerseEnd = curVerseEnd;
			}

			CheckForMissingVerses(versesFound, bookId, chapToken.ChapterNumber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the list of found verses to see if any verses in the specified range have
		/// already been found.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool VersesAlreadyFound(int curVerseStart, int curVerseEnd,
			ITextToken[] versesFound)
		{
			for (int verse = curVerseStart; verse <= curVerseEnd; verse++)
			{
				if (verse < versesFound.Length && verse > 0 && versesFound[verse] != null)
					return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks for missing verses in the current chapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckForMissingVerses(ITextToken[] versesFound, int bookId, int chapNumber)
		{
			ITextToken prevToken = versesFound[0];

			for (int verse = 1; verse < versesFound.Length; verse++)
			{
				if (versesFound[verse] != null)
				{
					prevToken = versesFound[verse];
					continue;
				}

				// At this point, we know we've found a missing verse. Now we need
				// to determine whether or not this is the first verse in a range
				// of missing verses or just a single missing verse.
				int startVerse = verse;
				int endVerse = verse;
				while (endVerse < versesFound.Length - 1 && versesFound[endVerse + 1] == null)
					endVerse++;

				prevToken.MissingStartRef = new BCVRef(bookId, chapNumber, startVerse);

				// If previous token is a verse token and it's verse 1 that's missing,
				// then we know we're dealing with the case of a missing chapter token
				// and a missing verse 1 token in that chapter. In that case, we want
				// the offset to fall just before the verse of the token (which is the
				// first verse token we found in the chapter and which we're assuming
				// is associated with a verse that would come after verse 1).
				int offset = (prevToken.TextType == TextType.VerseNumber && verse == 1 ?
					0 : prevToken.Text.Length);

				if (startVerse == endVerse)
					AddError(prevToken, offset, 0, Localize("Missing verse number {0}"), startVerse);
				else
				{
					prevToken.MissingEndRef = new BCVRef(bookId, chapNumber, endVerse);
					AddError(prevToken, offset, 0, Localize("Missing verse numbers {0}-{1}"),
						startVerse, endVerse);
				}

				verse = endVerse;
			}
		}

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses a verse number.
		/// </summary>
		/// <param name="runChars">The text of the token containing the verse number.</param>
		/// <param name="curVerseStart">The cur verse start.</param>
		/// <param name="curVerseEnd">The cur verse end.</param>
		/// <param name="vrsPart">The VRS part.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ParseVerseResult ParseVerseNumber(string runChars, out int curVerseStart,
			out int curVerseEnd, out VersePart vrsPart)
		{
			string literalVerse;
			string remainingText;
			BCVRef firstRefer = new BCVRef();
			BCVRef lastRefer = new BCVRef();
			string trimmedRun = runChars.TrimStart(null);
			bool hasPrecedingWhiteSpace = (runChars.Length != trimmedRun.Length);
			vrsPart = VersePart.NA;

			//Check for a correct format
			if (!m_verseNumberFormat.IsMatch(runChars.Replace(" ", string.Empty)))
			{
				// Even though the verse number is invalid, we'll still attempt to interpret it
				// as a verse number (or bridge) since that might avoid spurious "missing verse"
				// errors.
				if (BCVRef.VerseToScrRef(trimmedRun, out literalVerse, out remainingText,
					ref firstRefer, ref lastRefer) && firstRefer.Verse > 0)
				{
					curVerseStart = firstRefer.Verse;
					curVerseEnd = lastRefer.Verse;
					return ParseVerseResult.InvalidFormat;
				}
				else
				{
					curVerseStart = 0;
					curVerseEnd = 0;
					return ParseVerseResult.Invalid;
				}
			}

			// Useful method VerseToScrRef existing in BCVRef returns the parts of a verse
			// bridge and any non-numerical remaining text in the run. Allows accounting for
			// possible verse bridges. Allows accounting for verse parts, 10a 10b, account
			// for valid case: 7-8a 8b, if encounter "a", expectedVerse repeats in order to
			// expect 8b							.
			if (!BCVRef.VerseToScrRef(trimmedRun, out literalVerse, out remainingText,
				ref firstRefer, ref lastRefer))
			{
				curVerseStart = 0;
				curVerseEnd = 0;
				return ParseVerseResult.Invalid;
			}

			curVerseStart = firstRefer.Verse;
			curVerseEnd = lastRefer.Verse;
			string remainingVerse = remainingText.Trim();
			bool hasWhiteSpace = (hasPrecedingWhiteSpace ||
				(remainingVerse.Length != remainingText.Length));

			// note: if verse bridge, assumes, 'a' is on verse end
			// checks for a part "a" in verse
			if (remainingVerse == m_subVerseA)
				vrsPart = VersePart.PartA;
			else if (remainingVerse ==  m_subVerseB)
				vrsPart = VersePart.PartB;

			// If there was a non-numerical part or verse number > 999 that caused an error
			// making verseStart or verseEnd returned as 0 it will parse the trimmed
			// remainingVerse string to an integer and assign it as the verse number
			if (remainingVerse.Length != 0)
			{
				if (curVerseStart == 0 && !int.TryParse(remainingVerse, out curVerseStart))
					return ParseVerseResult.Invalid;

				if (curVerseEnd == 0 && !int.TryParse(remainingVerse, out curVerseEnd))
					return ParseVerseResult.Invalid;

				// adds error if verse part is not 'a' or 'b', for example "10c" would
				// be invalid verse number if " 13", still invalid format
				if (remainingVerse != m_subVerseA && remainingVerse !=  m_subVerseB)
					return ParseVerseResult.Invalid;
			}

			if (!hasWhiteSpace)
				return ParseVerseResult.Valid;

			return (curVerseStart == curVerseEnd ? ParseVerseResult.ValidWithSpaceInVerse :
				ParseVerseResult.ValidWithSpaceInVerseBridge);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether or not the two specified verse ranges contain any overlapping
		/// verses and returns the range of overlapping verses and a flag indicating whether
		/// there are overlapping verses.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AnyOverlappingVerses(int start1, int end1, int start2, int end2,
			out object[] commonVerses)
		{
			commonVerses = null;
			List<object> common = new List<object>();
			for (int i = start1; i <= end1; i++)
			{
				if (i >= start2 && i <= end2)
					common.Add(i);
			}

			if (common.Count > 0)
			{
				// When there are two or less overlapping verse(s) the returned array contains
				// all overlapping verses. Otherwise, it contains only the first and last
				// overlapping verses, since we only want to know the range.
				commonVerses = (common.Count <= 2 ? common.ToArray() :
					new object[] { common[0], common[common.Count - 1] });
			}

			return (commonVerses != null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Records an error.
		/// </summary>
		/// <param name="token">The current token being processed.</param>
		/// <param name="offset">Offset in the token where the offending text begins.</param>
		/// <param name="length">The length of the offending text.</param>
		/// <param name="message">The message.</param>
		/// <param name="args">The arguments to format the message.</param>
		/// ------------------------------------------------------------------------------------
		private void AddError(ITextToken token, int offset, int length, string message,
			params object[] args)
		{
			string formattedMsg = (args != null) ? string.Format(message, args) :
				String.Format(message);

			TextTokenSubstring tts = new TextTokenSubstring(token, offset, length, formattedMsg);
			m_recordError(new RecordErrorEventArgs(tts, CheckId));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an error for a missing chapter number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddMissingChapterError(ITextToken token, int missingChapter, int offset)
		{
			BCVRef scrRef = new BCVRef(token.ScrRefString);
			scrRef.Chapter = missingChapter;
			scrRef.Verse = 0;
			token.MissingStartRef = scrRef;
			AddError(token, offset, 0, Localize("Missing chapter number {0}"), missingChapter);
		}

		#endregion
	}

	#region ChapterToken class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class ChapterToken
	{
		internal ITextToken Token;
		internal bool Implicit;
		internal bool Valid = true;
		private int m_chapNumber;
		private List<VerseToken> m_verseTokens = new List<VerseToken>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ChapterToken(ITextToken token, Regex chapterNumberFormat)
		{
			Token = token;
			m_chapNumber = 0;
			if (!chapterNumberFormat.IsMatch(Token.Text))
				Valid = false;
			foreach (char ch in token.Text)
			{
				if (Char.IsDigit(ch))
				{
					m_chapNumber *= 10;
					m_chapNumber += (int) Char.GetNumericValue(ch);
				}
				else
				{
					Valid = false;
					m_chapNumber = -1;
					break;
				}
			}
			Implicit = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ChapterToken(ITextToken token, int chapNumber)
		{
			Token = token;
			m_chapNumber = chapNumber;
			Implicit = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal int ChapterNumber
		{
			get	{ return m_chapNumber; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal List<VerseToken> VerseTokens
		{
			get { return m_verseTokens; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last verse number token associated with this chapter. If there isn't one, then
		/// this chapter's token is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ITextToken LastVerseToken
		{
			get
			{
				return (m_verseTokens.Count == 0 ?
					Token : m_verseTokens[m_verseTokens.Count - 1].VerseNumber);
			}
		}
	}

	#endregion

	#region VerseToken class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to represent a verse number and a set of one or more verses
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class VerseToken
	{
		// the one and only verse number
		private ITextToken m_verseNumberToken=null;
		// one or more verse text tokens (most probably a paragraph)
		private int m_nbrTextTokens = 0;
		private int m_implicitVerseNumber = -1;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal VerseToken( ITextToken verseNumber)
		{
			m_verseNumberToken = verseNumber;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal VerseToken(ITextToken implicitVerseNumber, int verseNumber)
		{
			m_verseNumberToken = implicitVerseNumber;
			m_implicitVerseNumber = verseNumber;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// A getter for the verse text body.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ITextToken VerseNumber
		{
			get { return m_verseNumberToken; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Increment verse text count
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void IncrementVerseTextCount(ITextToken token)
		{
			// only count tokens that aren't all whitespace.
			if (token.Text.Trim().Length > 0)
				m_nbrTextTokens++;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// return verse text count
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal int VerseTextCount
		{
			get { return m_nbrTextTokens; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// return flag indicating if this is an implied verse number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool Implicit
		{
			get { return m_implicitVerseNumber != -1; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// return the implied verse number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal int ImplicitVerseNumber
		{
			get { return m_implicitVerseNumber; }
		}
	}

	#endregion

}
