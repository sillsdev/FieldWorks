// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2008' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeChecksDataSource.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Diagnostics;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SILUBS.SharedScrUtils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrChecksDataSource : IChecksDataSource
	{
		private IScrBook m_bookBeingChecked = null;
		private Dictionary<string, string> m_checkingParameters = new Dictionary<string, string>();

		/// <summary>
		/// For a given canonical book number and check, this allows us to quickly look up any
		/// checking errors from a previous run of a check that are pending validation. When the
		/// check completes any errors that are still in the collection and do not have comments
		/// will be deleted.
		/// </summary>
		private Dictionary<int, Dictionary<Guid, Dictionary<string, List<IScrScriptureNote>>>> m_pendingCheckErrors;

		/// <summary>A count of each new unique error that is recorded</summary>
		private ErrorInventory m_errorCounts = new ErrorInventory();

		// Keeps track of all the checks that fail for each book. The book is the key
		// and the list contains the checks that failed (and their result) for that book.
		Dictionary<int, Dictionary<Guid, ScrCheckRunResult>> m_bookChecksFailed;

		private readonly FdoCache m_cache;
		private readonly IScripture m_scr;
		private readonly string m_styleSheetFileName;
		private readonly string m_punctWhitespaceChar;
		private readonly string m_legacyOverridesFile;

		private static StyleMarkupInfo s_styleMarkupInfo = null;

		#region error-handling delegate/event
		/// <summary>Fired if valid character data cannot be loaded</summary>
		public event ValidCharacters.LoadExceptionDelegate LoadException;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrChecksDataSource"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="punctWhitespaceChar"></param>
		/// <param name="legacyOverridesFile"></param>
		/// ------------------------------------------------------------------------------------
		public ScrChecksDataSource(FdoCache cache, string punctWhitespaceChar, string legacyOverridesFile) : this(cache, punctWhitespaceChar, legacyOverridesFile, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrChecksDataSource"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="punctWhitespaceChar"></param>
		/// <param name="legacyOverridesFile"></param>
		/// <param name="styleSheetFileName">Path to the stylesheet definition XML file</param>
		/// ------------------------------------------------------------------------------------
		public ScrChecksDataSource(FdoCache cache, string punctWhitespaceChar, string legacyOverridesFile, string styleSheetFileName)
		{
			m_cache = cache;
			m_scr = cache.LangProject.TranslatedScriptureOA;
			m_punctWhitespaceChar = punctWhitespaceChar;
			m_styleSheetFileName = styleSheetFileName;
			m_legacyOverridesFile = legacyOverridesFile;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get { return m_cache; }
		}

		#region IChecksDataSource Members and RecordError event handler
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of the book numbers present (1-based, canonical, ascending order).
		/// This list is used as an argument to GetText when retrieving the data for each book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<int> BooksPresent
		{
			get
			{
				List<int> booksPresent = new List<int>();
				foreach (IScrBook book in m_scr.ScriptureBooksOS)
					booksPresent.Add(book.CanonicalNum);
				return booksPresent;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an object that can determine the category of Unicode characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CharacterCategorizer CharacterCategorizer
		{
			get
			{
				var charPropEngine = m_cache.WritingSystemFactory.get_CharPropEngine(
					m_cache.DefaultVernWs);
				return new FwCharacterCategorizer(ValidCharacters, charPropEngine);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the information from the stylesheet to be used in Scripture checks.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StylePropsInfo StyleInfo
		{
			get
			{
				if (!StyleMarkupInfo.StylesAreLoaded)
					s_styleMarkupInfo = StyleMarkupInfo.Load(m_styleSheetFileName);
				return (s_styleMarkupInfo != null) ? s_styleMarkupInfo.StyleInfo : null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve named checking parameter value.
		/// Checks use this to get their setup information.
		/// </summary>
		/// <param name="key">Parameter name</param>
		/// <returns>Parameter value</returns>
		/// ------------------------------------------------------------------------------------
		public string GetParameterValue(string key)
		{
			IWritingSystemManager wsManager = m_cache.ServiceLocator.WritingSystemManager;
			int hvoWs = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			IWritingSystem ws = wsManager.Get(hvoWs);

			if (key.Contains("ValidCharacters"))
				return GetValidCharactersList(key, ws);

			switch (key)
			{
				case "PoeticStyles":
					return GetPoeticStyles();

				case "Versification Scheme":
					return m_scr.Versification.ToString();

				case "IntroductionOutlineStyles":
					//REVIEW: Do we need this? return "Intro_List_Item1";
					return string.Empty;

				case "PunctCheckLevel":
					return "Intermediate";

				case "PunctWhitespaceChar":
					return m_punctWhitespaceChar.Substring(0, 1);

				case "MatchedPairs":
					return ws.MatchedPairs;

				case "PunctuationPatterns":
					return ws.PunctuationPatterns;

				case "SentenceFinalPunctuation":
					return GetSentenceFinalPunctuation(ws, m_cache.ServiceLocator.UnicodeCharProps);

				case "QuotationMarkInfo":
					return ws.QuotationMarks;

				case "StylesInfo":
					return (StyleInfo != null) ? StyleInfo.XmlString : null;

				case "DefaultWritingSystemName":
					return ws.DisplayLabel;

				case "Verse Bridge":
					return m_scr.BridgeForWs(hvoWs);

				case "Script Digit Zero":
					return m_scr.UseScriptDigits ? ((char)m_scr.ScriptDigitZero).ToString() : "0";

				case "Sub-verse Letter A":
					return "a"; // TODO (TE-8593): Support sub-verse letters for non-Roman text

				case "Sub-verse Letter B":
					return "b"; // TODO (TE-8593): Support sub-verse letters for non-Roman text

				default:
					string value;
					return (m_checkingParameters.TryGetValue(key, out value)) ? value : string.Empty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the sentence final punctuation from the punctuation patterns for the given
		/// writing system.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		/// <param name="unicodeCharProps">>The unicode character properties engine.</param>
		/// <returns>sentence final punctuation patterns for this writing system</returns>
		/// ------------------------------------------------------------------------------------
		private string GetSentenceFinalPunctuation(IWritingSystem ws, ILgCharacterPropertyEngine unicodeCharProps)
		{
			string punctuationPatterns = ws.PunctuationPatterns;
			if (!string.IsNullOrEmpty(punctuationPatterns) && punctuationPatterns.Trim().Length > 0)
			{
				var strBldr = new StringBuilder();
				PuncPatternsList puncPatternsList = PuncPatternsList.Load(punctuationPatterns,
					ws.DisplayLabel);
				// Scan through all the punctuation patterns for this writing system.
				foreach (PuncPattern pattern in puncPatternsList)
				{
					// For each valid pattern...
					if (pattern.Status == PuncPatternStatus.Valid &&
						pattern.ContextPos == ContextPosition.WordFinal)
					{
						// scan through the pattern string...
						foreach (char puncChar in pattern.Pattern)
						{
							// and search for sentence-final punctuation patterns that have not yet been added.
							if (TsStringUtils.IsEndOfSentenceChar(puncChar,
								unicodeCharProps.get_GeneralCategory(puncChar)) &&
								strBldr.ToString().IndexOf(puncChar) == -1)
							{
								strBldr.Append(puncChar);
							}
						}
					}
				}
				return strBldr.ToString();
			}

			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read the text for the specified book number.
		/// ENHANCE: For our initial implementation, we'll see how it performs if we don't
		/// do the parsing ahead of time. We'll just set up the enumerators...Parse into Tokens.
		/// The tokens are accessed via the TextTokens() method.
		/// We split this operation into two parts since we often want to create
		/// the tokens list once and then present them to several different checks.
		/// </summary>
		/// <param name="bookNum">Canonical number of book, as returned in list from call to
		/// BooksPresent.</param>
		/// <param name="chapterNum">0=read whole book, else specified chapter number</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool GetText(int bookNum, int chapterNum)
		{
			m_bookBeingChecked = (IScrBook)m_scr.FindBook(bookNum);

			SetParameterValue("Book ID", m_bookBeingChecked.BookId);
			SetParameterValue("Chapter Number", chapterNum.ToString());

			return m_bookBeingChecked.GetTextToCheck(chapterNum);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save all checking parameter values.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Save()
		{
			// TODO: Implementation should persist parameters in the DB (need a model change)
		}

		/// --------------------------------------------------------------------------------------------
		/// <summary>
		/// Set the named checking parameter value.
		/// </summary>
		/// <param name="key">Parmameter name</param>
		/// <param name="value">Parameter value</param>
		/// --------------------------------------------------------------------------------------------
		public void SetParameterValue(string key, string value)
		{
			m_checkingParameters[key] = value;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enumerate all the ITextToken's from the most recent GetText call.
		/// </summary>
		/// <returns>An IEnumerable implementation that allows the caller to retrieve each
		/// text token in sequence.</returns>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<ITextToken> TextTokens()
		{
			return m_bookBeingChecked.TextTokens();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Runs the check.
		/// </summary>
		/// <param name="check">The check.</param>
		/// ------------------------------------------------------------------------------------
		public void RunCheck(IScriptureCheck check)
		{
			if (m_bookChecksFailed == null)
				m_bookChecksFailed = new Dictionary<int, Dictionary<Guid, ScrCheckRunResult>>();

			if (m_pendingCheckErrors == null)
				m_pendingCheckErrors = new Dictionary<int, Dictionary<Guid, Dictionary<string, List<IScrScriptureNote>>>>();

			Dictionary<Guid, Dictionary<string, List<IScrScriptureNote>>> pendingErrorsForBook;
			int bookNum = m_bookBeingChecked.CanonicalNum;
			if (!m_pendingCheckErrors.TryGetValue(bookNum, out pendingErrorsForBook))
			{
				pendingErrorsForBook = new Dictionary<Guid, Dictionary<string, List<IScrScriptureNote>>>();
				m_pendingCheckErrors[bookNum] = pendingErrorsForBook;
			}
			Dictionary<string, List<IScrScriptureNote>> pendingErrorsForCheck =
				new Dictionary<string, List<IScrScriptureNote>>();

			pendingErrorsForBook[check.CheckId] = pendingErrorsForCheck;

			IScrBookAnnotations annotations =
				(IScrBookAnnotations)m_scr.BookAnnotationsOS[bookNum - 1];

			// Find previously created error annotions for the current book and check.
			foreach (IScrScriptureNote ann in annotations.NotesOS)
			{
				BCVRef beginRef = new BCVRef(ann.BeginRef);
				// ENHANCE, use a smarter algorithm to search for the start of the annotations for this book
				if (beginRef.Book == bookNum && ann.AnnotationTypeRA.Guid == check.CheckId)
				{
					BCVRef endRef = new BCVRef(ann.EndRef);
					IStTxtPara quotePara = (IStTxtPara)ann.QuoteOA.ParagraphsOS[0];
					IStTxtPara discussionPara = (IStTxtPara)ann.DiscussionOA.ParagraphsOS[0];
					string key = beginRef.AsString + endRef.AsString + quotePara.Contents.Text +
						discussionPara.Contents.Text;

					List<IScrScriptureNote> errors;
					if (!pendingErrorsForCheck.TryGetValue(key, out errors))
					{
						errors = new List<IScrScriptureNote>();
						pendingErrorsForCheck[key] = errors;
					}
					errors.Add(ann);
				}
			}

			if (!m_bookChecksFailed.ContainsKey(bookNum))
				m_bookChecksFailed[bookNum] = new Dictionary<Guid, ScrCheckRunResult>();

			// Before running the check, reset the check result for this book and check.
			// This is like initializing our check result to green bar in an NUnit test.
			// As the check is running, that status may get changed to "Inconsistencies"
			// (red bar) or "IgnoredInconsistencies" (yellow bar).
			m_bookChecksFailed[bookNum][check.CheckId] = ScrCheckRunResult.NoInconsistencies;

			// Create a hash table for this check to tally how many times each unique error is generated.
			if (m_errorCounts != null)
			{
				m_errorCounts.Clear();
				m_errorCounts = null;
			}
			m_errorCounts = new ErrorInventory();

			// Run the Scripture check.
			check.Check(TextTokens(), RecordError);

			// Find a check history record for the check just run.
			// If one cannot be found, then create a new one.
			IScrCheckRun checkRun = null;
			foreach (IScrCheckRun scrChkRun in annotations.ChkHistRecsOC)
			{
				if (scrChkRun.CheckId == check.CheckId)
				{
					checkRun = scrChkRun;
					break;
				}
			}

			if (checkRun == null)
			{
				checkRun = Cache.ServiceLocator.GetInstance<IScrCheckRunFactory>().Create();
				annotations.ChkHistRecsOC.Add(checkRun);
				checkRun.CheckId = check.CheckId;
			}

			checkRun.RunDate = DateTime.Now;
			checkRun.Result = m_bookChecksFailed[bookNum][check.CheckId];

			foreach (List<IScrScriptureNote> obsoleteErrors in pendingErrorsForCheck.Values)
				foreach (IScrScriptureNote obsoleteError in obsoleteErrors)
					annotations.NotesOS.Remove(obsoleteError);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an annotation in response to an error returned by a check.
		/// The check calls this delegate whenever an error is found.
		/// </summary>
		/// <param name="args">Information about the potential inconsistency being reported</param>
		/// ------------------------------------------------------------------------------------
		public void RecordError(RecordErrorEventArgs args)
		{
			ScrCheckingToken firstToken = args.Tts.FirstToken as ScrCheckingToken;
			ScrCheckingToken lastToken = args.Tts.LastToken as ScrCheckingToken;
			Debug.Assert(firstToken != null);
			Debug.Assert(lastToken != null);
			Debug.Assert(firstToken.Object == lastToken.Object);
			int offset = args.Tts.Offset;
			int length = args.Tts.Length;
			Guid checkId = args.CheckId;
			string formattedMsg = args.Tts.Message;

			// If the token is for a missing reference, then replace the token's
			// reference range with the missing reference range.
			if (args.Tts.MissingStartRef != null && !args.Tts.MissingStartRef.IsEmpty)
			{
				firstToken.m_startRef = new BCVRef(firstToken.MissingStartRef);
				firstToken.m_endRef = new BCVRef(firstToken.MissingEndRef != null ?
					firstToken.m_missingEndRef : firstToken.m_startRef);
			}

			int bookNum = firstToken.StartRef.Book;
			string citedText = args.Tts.Text;

			IScrBookAnnotations annotations =
				(IScrBookAnnotations)m_scr.BookAnnotationsOS[bookNum - 1];

			// key for the error with the same cited text and message at a particular reference.
			string errorLocKey = firstToken.StartRef.AsString + lastToken.EndRef.AsString +
				citedText +	formattedMsg;
			// key for an error with the same cited text and message.
			string errorKey = checkId.ToString() + citedText + formattedMsg;

			if (CheckForPreviousInconsistency(firstToken, lastToken, citedText, offset, length, checkId, errorLocKey, errorKey))
				return;

			// NOTE: A maxIdenticalErrors value of -1 indicates that there is no maximum set.
			int maxIdenticalErrors = GetMaxIdenticalErrors(checkId);
			int errorCount = m_errorCounts.GetValue(errorKey);

			// Check the number of times this same error has already been reported.
			// If the maximum allowed number of identical errors already has been reached then
			// we don't want to report the error as usual.
			if (errorCount == maxIdenticalErrors)
				formattedMsg = GetExceededErrorMsg(checkId);
			else if (errorCount > maxIdenticalErrors && maxIdenticalErrors > -1)
			{
				// We have exceeded the maximum allowed for this error, so don't write out an annotation.
				return;
			}

			m_errorCounts.IncrementError(errorKey);
			AddErrorAnnotation(citedText, offset, length, checkId, firstToken, lastToken, formattedMsg);
			m_bookChecksFailed[bookNum][checkId] = ScrCheckRunResult.Inconsistencies;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks whether or not there is a checking error result (i.e. ScrScriptureNote) for
		/// the specified check ID, reference, cited text and error message. If one does exist,
		/// then we must do some updating.
		/// </summary>
		/// <param name="firstToken">The first token.</param>
		/// <param name="lastToken">The last token.</param>
		/// <param name="citedText">The cited text.</param>
		/// <param name="offset">The character offset of the start of the inconsistency in the
		/// .</param>
		/// <param name="length">The length.</param>
		/// <param name="checkId">The check id.</param>
		/// <param name="errorLocKey">The error location key, that includes both the beginning
		/// and ending reference of the error as well as the cited text and error message.</param>
		/// <param name="errorKey">The error key which includes the check id, the cited text
		/// and the error message.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool CheckForPreviousInconsistency(ScrCheckingToken firstToken,
			ScrCheckingToken lastToken, string citedText, int offset, int length, Guid checkId,
			string errorLocKey, string errorKey)
		{
			int bookNum = firstToken.StartRef.Book;

			List<IScrScriptureNote> errors;
			if (m_pendingCheckErrors == null ||
				!m_pendingCheckErrors[bookNum][checkId].TryGetValue(errorLocKey, out errors))
			{
				// We don't have a previous inconsistency, so nothing needs to be updated.
				return false;
			}

			// We have previously created an identical annotation (same error, same reference),
			// so we need to check if there is not already an inconsistency logged for the current
			// book and check...
			if (m_bookChecksFailed[bookNum][checkId] != ScrCheckRunResult.Inconsistencies)
			{
				// If the error is closed, then the book and check are marked as having
				// at least one ignored inconsistency. Otherwise, the book and check are marked
				// as having at least one inconsistency.
				m_bookChecksFailed[bookNum][checkId] =
					(errors[0].ResolutionStatus == NoteStatus.Closed ?
					ScrCheckRunResult.IgnoredInconsistencies :
					ScrCheckRunResult.Inconsistencies);
			}

			int maxIdenticalErrors = GetMaxIdenticalErrors(checkId);

			// If the error isn't ignored...
			if (errors[0].ResolutionStatus != NoteStatus.Closed)
			{
				// If the maximum number of identical errors was just exceeded for this
				// Scripture check. Create a new ScrScriptureNote to indicate this.
				if (m_errorCounts.GetValue(errorKey) == maxIdenticalErrors)
					AddErrorAnnotation(citedText, offset, length, checkId, firstToken, lastToken, GetExceededErrorMsg(checkId));

				m_errorCounts.IncrementError(errorKey);
			}

			// This is a duplicate error message and not beyond the maximum set for this check,
			// so we want to remove the annotation from our list of annotations to delete.
			// We will use this annotation instead of the one that was generated from an editorial
			// check.
			if (m_errorCounts.GetValue(errorKey) <= maxIdenticalErrors || maxIdenticalErrors == -1)
			{
				errors[0].BeginOffset = firstToken.ParaOffset + offset;
				errors[0].EndOffset = errors[0].BeginOffset + length;
				errors[0].BeginObjectRA = firstToken.m_object;
				errors[0].EndObjectRA = firstToken.m_object;
				errors.RemoveAt(0);
				if (errors.Count == 0)
					m_pendingCheckErrors[bookNum][checkId].Remove(errorLocKey);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the error annotation to the database.
		/// </summary>
		/// <param name="citedText">The cited text.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="length">The length.</param>
		/// <param name="checkId">The Scripture error checking id.</param>
		/// <param name="firstToken">The first token.</param>
		/// <param name="lastToken">The last token.</param>
		/// <param name="formattedMsg">The formatted error message.</param>
		/// ------------------------------------------------------------------------------------
		private void AddErrorAnnotation(string citedText, int offset, int length, Guid checkId,
			ScrCheckingToken firstToken, ScrCheckingToken lastToken, string formattedMsg)
		{
			IScrBookAnnotations annotations =
				(IScrBookAnnotations)m_scr.BookAnnotationsOS[firstToken.StartRef.Book - 1];

			StTxtParaBldr quote = SetCitedText(citedText, firstToken.Ws);
			StTxtParaBldr discussion = SetErrorMessage(formattedMsg);
			IScrScriptureNote note = annotations.InsertErrorAnnotation(firstToken.StartRef,
				lastToken.EndRef, firstToken.Object, lastToken.Object, checkId, quote,
				discussion);

			note.BeginOffset = firstToken.ParaOffset + offset;
			note.EndOffset = note.BeginOffset + length;
			note.Flid = firstToken.Flid;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the cited text for checking error annotation.
		/// </summary>
		/// <param name="citedText">The cited text.</param>
		/// <param name="ws">The writing system.</param>
		/// <returns>Quote for the ScrScriptureNote</returns>
		/// ------------------------------------------------------------------------------------
		private StTxtParaBldr SetCitedText(string citedText, int ws)
		{
			StTxtParaBldr quote = new StTxtParaBldr(m_cache);
			quote.ParaStyleName = ScrStyleNames.Remark;
			// ENHANCE: Cache the run props using a hash table for each writing system
			quote.AppendRun(citedText, StyleUtils.CharStyleTextProps(null, ws));
			return quote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the error message.
		/// </summary>
		/// <param name="formattedMsg">The localized and formatted message.</param>
		/// <returns>Discussion for the ScrScriptureNote</returns>
		/// ------------------------------------------------------------------------------------
		private StTxtParaBldr SetErrorMessage(string formattedMsg)
		{
			StTxtParaBldr discussion = new StTxtParaBldr(m_cache);
			discussion.ParaStyleName = ScrStyleNames.Remark;
			// ENHANCE: Cache the run props for the UI writing system
			discussion.AppendRun(formattedMsg,
				StyleUtils.CharStyleTextProps(null, m_cache.DefaultUserWs));
			return discussion;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the exceeded error message when a check has more identical errors than
		/// specified for the check.
		/// </summary>
		/// <param name="checkId">The GUID which uniquely identifies the Scripture check.</param>
		/// <returns>error message</returns>
		/// ------------------------------------------------------------------------------------
		private string GetExceededErrorMsg(Guid checkId)
		{
			ICmAnnotationDefn checkDef = Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().GetObject(checkId);
			return string.Format(StringUtils.GetUiString(ScrFdoResources.ResourceManager,
				"kstidExceededMaxNumberIdenticalChecks"), checkDef != null ?
				checkDef.Name.UserDefaultWritingSystem.Text : string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a localized version of the specified string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string GetLocalizedString(string strToLocalize)
		{
			return StringUtils.GetUiString(ScrFdoResources.ResourceManager, strToLocalize);
		}

		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the maxiumum number of allowed identical errors.
		/// </summary>
		/// <param name="checkId">The unique id for a check.</param>
		/// <returns>the number of errors allowed for the check, or a default value if not</returns>
		/// <remarks>Eventually this method may get the maximum number of identical errors for
		/// the check from the database or registry.</remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual int GetMaxIdenticalErrors(Guid checkId)
		{
			// All guids are listed for the basic checks to make
			// it easier to have a different default behavior for each one.

			if (checkId == StandardCheckIds.kguidCharacters ||
				checkId == StandardCheckIds.kguidMixedCapitalization)
			{
				return 25;
			}

			return (checkId == StandardCheckIds.kguidChapterVerse ? -1 : 100);
		}

		/// <summary>
		/// Gets the valid characters list.
		/// </summary>
		/// <param name="key">The valid characters parameter key.</param>
		/// <param name="ws">The writing system</param>
		/// <returns></returns>
		private string GetValidCharactersList(string key, IWritingSystem ws)
		{
			if (key.StartsWith("ValidCharacters_"))
			{
				// If the key contains a locale ID, then don't use the default vernacular
				// writing system, build one from the locale.
				string identifier = key.Substring(key.IndexOf("_") + 1);
				ws = m_cache.ServiceLocator.WritingSystemManager.Get(LangTagUtils.ToLangTag(identifier));
			}
			else if (key == "AlwaysValidCharacters")
			{
				var bldr = new StringBuilder(13);

				// Add the vernacular digits 0 through 9.
				for (int i = 0; i < 10; i++)
					bldr.Append(m_scr.ConvertToString(i));

				if (ws.RightToLeftScript)
				{
					// Add the LTR and RTL marks.
					bldr.Append('\u202A');
					bldr.Append('\u202B');
				}

				// Add the line separator.
				bldr.Append('\u2028');
				return bldr.ToString();
			}

			var validChars = ValidCharacters.Load(ws, LoadException ?? NoErrorReport, m_legacyOverridesFile);
			return (validChars != null ? validChars.SpaceDelimitedList : string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the valid characters for the default vernacular writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ValidCharacters ValidCharacters
		{
			get
			{
				// Get the writing system and valid characters list
				return ValidCharacters.Load(m_cache.ServiceLocator.WritingSystemManager.Get(m_cache.DefaultVernWs),
					LoadException ?? NoErrorReport, m_legacyOverridesFile);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a delimited string of all the styles with a function value of "Line".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string GetPoeticStyles()
		{
			var bldr = new StringBuilder();

			foreach (IStStyle style in m_scr.StylesOC)
			{
				Debug.WriteLine("Name: " + style.Name + "   " + style.Function);

				if (style.Function == FunctionValues.Line)
				{
					bldr.Append(style.Name);
					bldr.Append(CheckUtils.kStyleNamesDelimiter);
				}
			}

			return bldr.ToString().Trim(CheckUtils.kStyleNamesDelimiter);
		}
		#endregion

		/// <summary>
		/// This is a no-op error reporting method - used when no one has registered a method to report
		/// invalid character errors when loading valid characters.
		/// </summary>
		private void NoErrorReport(ArgumentException e)
		{
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// We keep inventories of the errors to determine how many times an identical error has
	/// been reported.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class ErrorInventory : Dictionary<string, int>
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the count of previous errors at the specified key, which uniquely identifies
		/// an error.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>previous number of times this identical error has been reported</returns>
		/// ------------------------------------------------------------------------------------
		public int GetValue(string key)
		{
			int errorCount;

			if (!TryGetValue(key, out errorCount))
			{
				errorCount = 0;
				this[key] = 0;
			}

			return errorCount;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Increments the error count for the specified key, which uniquely identifies an
		/// error.
		/// </summary>
		/// <param name="key">The key.</param>
		/// ------------------------------------------------------------------------------------
		public void IncrementError(string key)
		{
			int errorCount = GetValue(key);
			this[key] = errorCount + 1;
		}
	}
}
