// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2003' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrBook.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.Utils;
using System.Text;
using System.Data.SqlClient;
using SIL.FieldWorks.FDO.LangProj;

namespace SIL.FieldWorks.FDO.Scripture
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>Options for whether/how a book can be overwritten with a saved version</summary>
	/// ----------------------------------------------------------------------------------------
	public enum OverwriteType
	{
		/// <summary>Full overwrite is possible without losing data</summary>
		FullNoDataLoss,
		/// <summary>Overwrite would lose data</summary>
		DataLoss,
		/// <summary>Partial overwrite is possible</summary>
		Partial,
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ScrBook represents a single book in a Scripture vernacular translation.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ScrBook : CmObject, IScrBook
	{
		#region Constants
		/// <summary>Query to find the list of back translations used in this book.</summary>
		private const string kSqlFindBtWSs =
			"select DISTINCT ctt.ws " +
				"from ScrBook book " +
				"join CmObject sec on sec.Owner$ = book.id " +
				"join CmObject txt on txt.Owner$ = sec.id or txt.Owner$ = book.id " +
				"join StTxtPara_ para on para.Owner$ = txt.id " +
				"join CmTranslation_ ct on ct.Owner$ = para.id and ct.type = {0} " +
				"join CmTranslation_Translation ctt on ctt.Obj = ct.id " +
				"where book.id = {1}";
		#endregion

		#region Data Members
		private static readonly uint s_intSize =
			(uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(int));
		ScrBookTokenEnumerable m_tokenizer;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a UI name for a book number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string UINameForBook(int bookNum)
		{
			return ScrFdoResources.ResourceManager.GetString("kstidBookName" + bookNum.ToString());
		}

		#region ScrBook Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the SIL book ID 3-letter code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BookId
		{
			get { return ScrReference.NumberToBookCode(CanonicalNum); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the canonical number for this ScrBook.
		/// TODO (TE-3899): When ScrRefSystem stuff is removed, remove this override to set/use
		/// value in the database.
		/// </summary>
		/// <exception cref="NotImplementedException">If setter is used (see TE-3899)</exception>
		/// ------------------------------------------------------------------------------------
		public int CanonicalNum
		{
			get { return CanonicalNum_Generated; }
			set { CanonicalNum_Generated = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the best available abbrev for this ScrBook to use in the UI.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BestUIAbbrev
		{
			get { return BookId;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the best available name for this ScrBook to use in the UI.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BestUIName
		{
			get	{ return ScrBook.UINameForBook(CanonicalNum); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TODO: This will need to change when the ScrRefSystem goes away
		/// Gets the best available name for this ScrBook - the vernacular name if it's
		/// available, otherwise the analysis language name, otherwise the SIL code.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString BestAvailName
		{
			get
			{
				int ws = Cache.DefaultVernWs;
				string sBookName = Name.VernacularDefaultWritingSystem;
				if (sBookName == null || sBookName == String.Empty)
				{
					sBookName = Name.AnalysisDefaultWritingSystem;
					ws = Cache.DefaultAnalWs;
				}
				if (sBookName == null || sBookName == String.Empty)
				{
					sBookName = BookId;
					ws = Cache.FallbackUserWs;
				}
				if (sBookName == null || sBookName == String.Empty)
				{
					sBookName = BookId;
					ws = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
				}

				Debug.Assert(sBookName != null && sBookName != String.Empty);

				ITsStrFactory tsf = TsStrFactoryClass.Create();
				return tsf.MakeString(sBookName.Trim(), ws);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets hvos of the writing systems for the back translations used in this book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual List<int> BackTransWs
		{
			get
			{
				// Get the hvo of the back translation CmPossibility so that we get the correct
				// type of CmTranslation.
				ICmPossibility btPossibility =
					Cache.LangProject.TranslationTagsOA.LookupPossibilityByGuid(
						LangProject.kguidTranBackTranslation);

				// Set up to command to get the back translation writing systems used in this book.
				StringBuilder sCmdBldr = new StringBuilder();
				sCmdBldr.AppendFormat(kSqlFindBtWSs, btPossibility.Hvo, Hvo);

				// Get the Hvos of writing systems used in back translations
				List<int> btWs = new List<int>();
				try
				{
					btWs.AddRange(DbOps.ReadIntArrayFromCommand(m_cache, sCmdBldr.ToString(), null));
				}
				catch
				{
					// error in the query
					return null;
				}

				if (btWs.Count == 0)
					btWs = null; // no back translations found

				return btWs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the versification currently in effect for Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Paratext.ScrVers Versification
		{
			get { return Cache.LangProject.TranslatedScriptureOA.Versification; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first section in the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrSection FirstSection
		{
			get { return this[0]; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first Scripture (non-intro) section in the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrSection FirstScriptureSection
		{
			get
			{
				foreach (IScrSection section in SectionsOS)
					if (!section.IsIntro)
						return section;
				// It turns out that it pretty easy to get a book with only introduction. Either
				// by importing something that only has introduction or by inserting an
				// intro section and deleting all scripture sections.
				//throw new Exception("Book has no Scripture section.");
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last section in the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrSection LastSection
		{
			get { return this[SectionsOS.Count - 1]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the specified section in the book. If the section doesn't exist (i.e. index
		/// is out of range), then null is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrSection this[int i]
		{
			get	{ return (i < 0 || i >= SectionsOS.Count ? null : (ScrSection)SectionsOS[i]); }
		}

		#endregion

		#region Methods for maintaining hashtable of footnote references
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize an exiting book. Overridden to allow initialization of footnote ref
		/// hashtable.
		/// </summary>
		/// <param name="fcCache">FDO Cache</param>
		/// <param name="hvo">HVO of existing book being initialized</param>
		/// <param name="fCheckValidity">Flag indicating whether to check validity</param>
		/// <param name="fLoadIntoCache">Flag indicating whether to load into cache</param>
		/// ------------------------------------------------------------------------------------
		protected override void InitExisting(FdoCache fcCache, int hvo, bool fCheckValidity,
			bool fLoadIntoCache)
		{
			base.InitExisting(fcCache, hvo, fCheckValidity, fLoadIntoCache);
			Dictionary<int, FootnoteHashEntry> footnoteRefs = FootnoteRefHashtable;
			if (footnoteRefs.Count != FootnotesOS.Count)
			{
				RefreshFootnoteRefs(footnoteRefs);
				// Would like to assert for equality, but because of how some updating is done, we can't be sure that all footnotes
				// owned by the book are currently referenced in the book text. For example, when a book is copied, the GUID's in the
				// text will still point to the original footnotes until some updating is done. There is also the possibility that
				// old ORC references are in the process of being updated, so the counts may not match. Would be nice to have a
				// time we could be sure that all changes were complete and allow the following assertion to be used.
				// Debug.Assert(footnoteRefs.Count == FootnotesOS.Count);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hashtable of footnote HVO's to FootnoteHashEntry's.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal Dictionary<int, FootnoteHashEntry> FootnoteRefHashtable
		{
			get
			{
				return m_cache.GetHashtable<int, FootnoteHashEntry,
					StFootnoteVerseChangeWatcher>(Guid,
					(int)StTxtPara.ktagVerseNumbers);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears all footnote caches (there may be one for each book). There is the possibility
		/// that a cache will be left for a book that has been deleted, don't have an easy way to
		/// find those and they should not cause problems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void ClearAllFootnoteCaches(FdoCache cache)
		{
			IScripture scr = cache.LangProject.TranslatedScriptureOA;
			foreach (IScrBook book in scr.ScriptureBooksOS)
			{
				Dictionary<int, FootnoteHashEntry> dict;
				if (cache.TryGetHashtable<int, FootnoteHashEntry>(book.Guid, out dict))
				{
					dict.Clear();
					cache.RemoveHashTable(book.Guid);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refresh the footnote ref hashtable
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void RefreshFootnoteRefs()
		{
			RefreshFootnoteRefs(FootnoteRefHashtable);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Refresh the footnote ref hashtable
		/// </summary>
		/// <param name="footnoteRefs">Dictionary of footnote HVOs to a structure containing
		/// Scripture references and owning paragraph's HVO</param>
		/// ------------------------------------------------------------------------------------
		internal void RefreshFootnoteRefs(Dictionary<int, FootnoteHashEntry> footnoteRefs)
		{
			// If there are no footnotes at all, we're done.
			if (FootnotesOS.Count == 0)
			{
				footnoteRefs.Clear();
				return;
			}

			// If this book is in the early stages of getting created and does not yet have a
			// properly initialized section, then don't try to add footnote references yet.
			// (Currently, I think this can only happen during tests.)
			if (SectionsOS.Count == 0 || SectionsOS[0].VerseRefStart == 0)
			{
				footnoteRefs.Clear();
				return;
			}

			// Reset the Found flag for all footnotes cached for the book.
			foreach (FootnoteHashEntry entry in footnoteRefs.Values)
				entry.Found = false;

			// Re-add the footnotes for the book.
			AddFootnoteRefs(footnoteRefs);

			// May need to remove references to deleted footnotes from the table
			if (footnoteRefs.Count != FootnotesOS.Count)
			{
				// need to create temp copy of keys so that refs can be removed from hash table
				int[] keys = new int[footnoteRefs.Count];
				footnoteRefs.Keys.CopyTo(keys, 0);
				foreach (int hvo in keys)
				{
					if (!footnoteRefs[hvo].Found)
						footnoteRefs.Remove(hvo);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds footnote references to the given hashtable for all the footnotes in this book.
		/// </summary>
		/// <param name="footnoteRefs">Dictionary of footnote HVOs to a structure containing
		/// Scripture references and owning paragraph's HVO</param>
		/// ------------------------------------------------------------------------------------
		private void AddFootnoteRefs(Dictionary<int, FootnoteHashEntry> footnoteRefs)
		{
			// TODO (TE-2885): When we have a place in the DB to store the owning para of a footnote
			// we need to check in that para for the ref instead of looking at the whole book
			// for footnotes.
			BCVRef footnoteRef = new BCVRef(CanonicalNum, 0, 0);
			// add refs for any footnotes in the book title
			ScrFootnote.AddFootnoteRefsInText(TitleOA, footnoteRefs, ref footnoteRef);

			// Add refs for footnotes in each section
			int[] sectionHvos =
				m_cache.GetVectorProperty(Hvo, (int)ScrBook.ScrBookTags.kflidSections, false);
			foreach (int hvo in sectionHvos)
			{
				IScrSection section = new ScrSection(m_cache, hvo);
				// Add refs for footnotes in the section header.
				// TODO: what should the reference chapter:verse be if the footnote is found
				// in a section header?
				BCVRef firstRef = new BCVRef(section.VerseRefStart);
				footnoteRef = new BCVRef(firstRef);
				ScrFootnote.AddFootnoteRefsInText(section.HeadingOA, footnoteRefs, ref footnoteRef);

				// Add refs for footnotes in the section content
				footnoteRef = new BCVRef(firstRef);
				ScrFootnote.AddFootnoteRefsInText(section.ContentOA, footnoteRefs, ref footnoteRef);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets footnote hvo from reference in text properties
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="tprops"></param>
		/// <returns>Hvo of the footnote that was found or 0 if none was found</returns>
		/// ------------------------------------------------------------------------------------
		protected static int GetFootnoteFromProps(FdoCache cache, ITsTextProps tprops)
		{
			string footnoteRef =
				tprops.GetStrPropValue((int)FwTextPropType.ktptObjData);

			if (footnoteRef != null)
			{
				// first char. of strData is type code - GUID will follow it.
				Guid objGuid = MiscUtils.GetGuidFromObjData(footnoteRef.Substring(1));

				int hvo = cache.GetIdFromGuid(objGuid);
				if (hvo > 0 && cache.GetClassOfObject(hvo) == StFootnote.kClassId)
					return hvo;
			}
			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove the given footnote from this book's collection and from the hashtable of
		/// references. This does NOT delete the ORC from the text.
		/// </summary>
		/// <param name="footnote">The footnote to remove</param>
		/// ------------------------------------------------------------------------------------
		internal void RemoveFootnote(StFootnote footnote)
		{
			int iFootnote = footnote.IndexInOwner;
			if (iFootnote < 0)
				return;
			Dictionary<int, FootnoteHashEntry> temp = FootnoteRefHashtable;
			temp.Remove(footnote.Hvo);
			FootnotesOS.RemoveAt(iFootnote);
		}
		#endregion

		#region Miscellaneous public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the book where the current text/paragraph/whatever is contained in.
		/// </summary>
		/// <param name="cache">The FDO cache</param>
		/// <param name="hvoText">The HVO of the text/paragraph...</param>
		/// <returns>Containing book, or <c>null</c> if not found</returns>
		/// ------------------------------------------------------------------------------------
		public static IScrBook Find(FdoCache cache, int hvoText)
		{
			ICmObject obj = CmObject.CreateFromDBObject(cache, hvoText);
			while (obj != null && !(obj is IScrBook) && obj.OwnerHVO > 0)
			{
				obj = CmObject.CreateFromDBObject(cache, obj.OwnerHVO);
			}

			if (obj != null && obj is IScrBook)
				return (IScrBook)obj;
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a book in the database based on a book ID
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="bookID"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static IScrBook FindBookByID(FdoCache cache, int bookID)
		{
			return FindBookByID(cache.LangProject.TranslatedScriptureOA, bookID);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a book in the database based on a book ID
		/// </summary>
		/// <param name="scr"></param>
		/// <param name="bookId"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static IScrBook FindBookByID(IScripture scr, int bookId)
		{
			return scr.FindBook(bookId);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrBook"/> class and adds it to
		/// the <see cref="Scripture"/>. Also creates a new StText for the ScrBook's Title
		/// property.
		/// </summary>
		/// <param name="bookNumber">Canonical book number to be inserted</param>
		/// <param name="scripture">FDO Scripture object that will own the book</param>
		/// <param name="hvoTitle">Hvo of the title StText created for the new book</param>
		/// <returns>A ScrBook object representing the newly inserted book</returns>
		/// <exception cref="InvalidOperationException">If this method is called with a
		/// canonical book number which is already represented in the database</exception>
		/// ------------------------------------------------------------------------------------
		public static IScrBook CreateNewScrBook(int bookNumber, IScripture scripture,
			out int hvoTitle)
		{
			FdoCache cache = scripture.Cache;

			// Check to see if there's an existing book with this same number and delete it.
			IScrBook book = scripture.FindBook(bookNumber);
			if (book != null)
				throw new InvalidOperationException("Attempting to create a Scripture book that already exists.");
			int iBook = 0;
			foreach (IScrBook existingBook in scripture.ScriptureBooksOS)
			{
				if (existingBook.CanonicalNum > bookNumber)
					break;
				iBook++;
			}

			book = new ScrBook();
			scripture.ScriptureBooksOS.InsertAt(book, iBook);
			book.CanonicalNum = bookNumber;
			book.BookIdRAHvo = cache.ScriptureReferenceSystem.BooksOS.HvoArray[bookNumber - 1];
			book.TitleOA = new StText();
			hvoTitle = book.TitleOAHvo;

			// Do a prop change event for the new book
			cache.PropChanged(null, PropChangeType.kpctNotifyAll,
				scripture.Hvo, (int)Scripture.ScriptureTags.kflidScriptureBooks, iBook, 1, 0);

			return book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a <see cref = "ScrBook"/>'s Title paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitTitlePara()
		{
			IStText titleText = TitleOA;
			Debug.Assert(titleText != null && titleText.ParagraphsOS.Count == 0);
			IStTxtPara para = (IStTxtPara)titleText.ParagraphsOS.Append(new StTxtPara());

			// set the title para style
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.MainBookTitle);

			// set the title text to the vernacular writing system.
			ITsStrBldr bldr = para.Contents.UnderlyingTsString.GetBldr();
			int ichLim = bldr.Length;
			bldr.SetIntPropValues(0, ichLim, (int)FwTextPropType.ktptWs,
				0, Cache.DefaultVernWs);
			para.Contents.UnderlyingTsString = bldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the next exact match starting from the given section starting at the given
		/// paragraph and character offsets. If not found looking forward from that point, the
		/// search wraps around from the beginning of the book.
		/// </summary>
		/// <param name="targetRef">ScrReference to find</param>
		/// <param name="fFindSectionStart">if set to <c>true</c> this method will return
		/// values corresponding to the beginning of the section head if the target reference is
		/// the first one in a section.</param>
		/// <param name="startingSection">The 0-based index of the first section to look in
		/// </param>
		/// <param name="startingParaIndex">The 0-based index of the first paragraph to look in
		/// </param>
		/// <param name="startingCharIndex">The 0-based index of the first character to look at
		/// </param>
		/// <param name="section">The 0-based index of the section where the reference starts.
		/// </param>
		/// <param name="paraIndex">The 0-based index of the paragraph where the reference starts.
		/// </param>
		/// <param name="ichVerseStart">The 0-based index of the character verse start.</param>
		/// <returns>
		/// 	<c>true</c> if found; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool GetRefStartFromSection(ScrReference targetRef, bool fFindSectionStart,
			ScrSection startingSection, int startingParaIndex, int startingCharIndex,
			out ScrSection section, out int paraIndex, out int ichVerseStart)
		{
			section = startingSection;

			// Find exact match starting at current position
			if (GetRefStart(targetRef, fFindSectionStart, section, startingParaIndex,
				startingCharIndex, -1, -1, out paraIndex, out ichVerseStart))
			{
				return true;
			}

			section = section.NextSection ?? (ScrSection)SectionsOS[0];

			while (section != startingSection)
			{
				if (GetRefStart(targetRef, fFindSectionStart, section, 0, 0, -1, -1,
					out paraIndex, out ichVerseStart))
				{
					return true;
				}
				// if the next section is null, wrap around to the beginning
				section = section.NextSection ?? (ScrSection)SectionsOS[0];
			}

			// We've wrapped all the way back around to the first section we tried. Now
			// try any characters/paragraphs that were before our start position in that section.
			return GetRefStart(targetRef, fFindSectionStart, startingSection, 0, 0,
				startingParaIndex, startingCharIndex, out paraIndex, out ichVerseStart);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the (next) exact match in the given section starting at the given paragraph
		/// and character offsets and ending at the given paragraph and character offsets
		/// </summary>
		/// <param name="targetRef">ScrReference to find</param>
		/// <param name="fFindSectionStart">if set to <c>true</c> this method will return
		/// values corresponding to the beginning of the section head if the target reference is
		/// the first one in a section.</param>
		/// <param name="section">section to search</param>
		/// <param name="startingParaIndex">The 0-based index of the first paragraph to look in
		/// </param>
		/// <param name="startingCharIndex">The 0-based index of the first character to look at
		/// </param>
		/// <param name="endingParaIndex">The 0-based index of the last paragraph to look in
		/// (-1 for end)</param>
		/// <param name="endingCharIndex">The 0-based index of the last character index to look
		/// at (-1 for end)</param>
		/// <param name="paraIndex">The 0-based index of the paragraph where the reference
		/// starts. Undefined if this method returns false.</param>
		/// <param name="ichVerseStart">The 0-based index of the character verse start.
		/// Undefined if this method returns false.</param>
		/// <returns>
		/// 	<c>true</c> if found; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		internal bool GetRefStart(ScrReference targetRef, bool fFindSectionStart,
			ScrSection section, int startingParaIndex, int startingCharIndex,
			int endingParaIndex, int endingCharIndex, out int paraIndex, out int ichVerseStart)
		{
			paraIndex = -1;
			ichVerseStart = -1;
			if (!section.ContainsReference(targetRef) ||
				startingParaIndex >= section.ContentOA.ParagraphsOS.Count) // TE-5439
			{
				return false;
			}

			if (endingParaIndex == -1 || endingParaIndex >= section.ContentOA.ParagraphsOS.Count)
				endingParaIndex = section.ContentOA.ParagraphsOS.Count - 1;

			for (paraIndex = startingParaIndex; paraIndex <= endingParaIndex; ++paraIndex)
			{
				ScrTxtPara para = new ScrTxtPara(m_cache, section.ContentOA.ParagraphsOS[paraIndex].Hvo);
				ScrVerseSet verseSet = new ScrVerseSet(para);
				int currentEndingCharIndex = (paraIndex == endingParaIndex && endingCharIndex != -1) ?
					endingCharIndex : para.Contents.Length - 1;

				foreach (ScrVerse verse in verseSet)
				{
					// ignore all but verse number runs
					if (!verse.VerseNumberRun)
						continue;
					if (verse.VerseStartIndex >= currentEndingCharIndex)
						break;
					if (verse.VerseStartIndex < startingCharIndex)
						continue;
					if (verse.StartRef <= targetRef && targetRef <= verse.EndRef)
					{
						// TODO: Interpret fFindSectionStart
						ichVerseStart = verse.TextStartIndex;
						return true;
					}
				}

				// after the first paragraph, start looking at 0
				startingCharIndex = 0;
			}
			return false;
		}
		#endregion

		#region Checking stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prepare to return tokens for Scripture checking.
		/// ENHANCE: For our initial implementation, we'll see how it performs if we don't
		/// do the parsing ahead of time. We'll just set up the enumerators...Parse into Tokens.
		/// The tokens are accessed via the TextTokens() method.
		/// We split this operation into two parts since we often want to create
		/// the tokens list once and then present them to several different checks.
		/// </summary>
		/// <param name="chapterNum">0=read whole book, else specified chapter number</param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool GetTextToCheck(int chapterNum)
		{
			m_tokenizer = new ScrBookTokenEnumerable(this, chapterNum);
			return true;
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
			if (m_tokenizer == null)
				throw new InvalidOperationException("GetTextToCheck must be called before TextTokens");
			return m_tokenizer;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the section where the given chapter begins.
		/// </summary>
		/// <param name="chapterNum">The chapter number (assumed to be in same versification as
		/// that of the Scripture object).</param>
		/// <returns>the index of the section or -1 if not found</returns>
		/// ------------------------------------------------------------------------------------
		public int FindSectionForChapter(int chapterNum)
		{
			// Getting the Versification is probably not very efficient, so do it once outside
			// the loop.
			Paratext.ScrVers versification = Versification;
			for (int iSection = 0; iSection < SectionsOS.Count; iSection++)
			{
				IScrSection section = SectionsOS[iSection];
				if (!section.IsIntro)
				{
					ScrReference startRef = new ScrReference(section.VerseRefStart, versification);
					if (startRef.Chapter == chapterNum)
						return iSection;
					if (startRef.Chapter < chapterNum)
					{
						ScrReference endRef = new ScrReference(section.VerseRefEnd, versification);
						if (endRef.Chapter >= chapterNum)
							return iSection;
					}
				}
			}
			return -1;
		}

		#endregion

		#region DetermineOverwritability and helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks to see whether/how this book can be overwritten with the given saved version.
		/// Possibilities are:
		/// * Full overwrite is possible without losing Scripture data
		/// * Full overwrite would lose Scripture data
		/// * Partial overwrite is possible
		/// </summary>
		/// <param name="savedVersion">The saved version to be checked.</param>
		/// <param name="sDetails">A string with a formatted list of reference ranges included
		/// in current but missing in the saved version (separated by newlines)</param>
		/// <param name="sectionsToRemove">sections that will need to be removed from this book
		/// before a partial overwrite can be executed (will be null unless return type is
		/// Partial).</param>
		/// <param name="missingBtWs">list of back translation writing systems that are used in
		/// this book but not in the savedVersion</param>
		/// ------------------------------------------------------------------------------------
		public OverwriteType DetermineOverwritability(ScrBook savedVersion, out string sDetails,
			out List<IScrSection> sectionsToRemove, out List<int> missingBtWs)
		{
			sDetails = null;
			sectionsToRemove = null;
			missingBtWs = null;
			StringBuilder strBldr = new StringBuilder();

			Debug.Assert(FirstSection != null && savedVersion.FirstSection != null);
			if (FirstSection == null)
				return OverwriteType.FullNoDataLoss; // Current book is empty
			else if (savedVersion.FirstSection == null)
				return OverwriteType.DataLoss; // Current is not empty but Revision is

			// Check for potential loss of back translation(s).
			missingBtWs = FindMissingBts(savedVersion);

			// Check that introduction won't be lost
			bool fIntroMissingInSaved =  FirstSection.IsIntro && !savedVersion.FirstSection.IsIntro;
			if (fIntroMissingInSaved)
			{
				strBldr.AppendFormat(
					ScrFdoResources.ResourceManager.GetString("kstidBookIntroduction"), BookId);
				strBldr.AppendLine();
			}

			List<MissingVerseRange> versesInCurrentMissingInSaved = new List<MissingVerseRange>();
			IScrSection firstScrSec = FirstScriptureSection;
			IScrSection firstRevScrSec = savedVersion.FirstScriptureSection;

			if (firstScrSec != null && firstRevScrSec == null)
			{
				// The current book has scripture sections that are not in the revision
				MissingVerseRange range = new MissingVerseRange(
					new ScrReference(firstScrSec.VerseRefMin, Versification),
					new ScrReference(LastSection.VerseRefEnd, Versification));
				versesInCurrentMissingInSaved.Add(range);
			}
			else if (firstScrSec == null && firstRevScrSec != null)
			{
				// All revision sections are new to the current. So everything is fine.
			}
			else if (firstScrSec == null && firstRevScrSec == null)
			{
				// There are no scripture sections in either the current or the revision, so
				// lets just give up!
				return OverwriteType.FullNoDataLoss;
			}
			else
			{
				ScrReference firstScrSecRefMin = new ScrReference(firstScrSec.VerseRefMin, Versification);
				ScrReference firstRevSecRefMin = new ScrReference(firstRevScrSec.VerseRefMin, Versification);
				if (firstScrSecRefMin.Verse <= 0)
					firstScrSecRefMin.Verse = 1;
				if (firstRevSecRefMin.Verse <= 0)
					firstRevSecRefMin.Verse = 1;

				if (firstScrSecRefMin < firstRevSecRefMin)
				{
					// The first section of the Current book contains verses before the first section
					// in the saved version. Replacing the Current with the saved version would cause
					// data loss.
					MissingVerseRange range = MissingVerseRange.CreateFromMinAndLim(firstScrSecRefMin, firstRevSecRefMin);
					versesInCurrentMissingInSaved.Add(range);
				}
			}

			// Check for hole in saved version
			versesInCurrentMissingInSaved.AddRange(DetectMissingHoles(savedVersion));

			// Check references on last scripture section
			if (LastSection.VerseRefMax > savedVersion.LastSection.VerseRefMax &&
				!savedVersion.LastSection.IsIntro)
			{
				MissingVerseRange range = MissingVerseRange.CreateFromPrevAndEnd(
					new ScrReference(savedVersion.LastSection.VerseRefMax, Versification),
					new ScrReference(LastSection.VerseRefMax, Versification));
				versesInCurrentMissingInSaved.Add(range);
			}

			if (versesInCurrentMissingInSaved.Count > 0 || fIntroMissingInSaved)
			{
				if (IsPartialOverwritePossible(versesInCurrentMissingInSaved, out sectionsToRemove, fIntroMissingInSaved))
					return OverwriteType.Partial;

				// Add list of holes to the strBldr
				foreach (MissingVerseRange missingVerses in versesInCurrentMissingInSaved)
					strBldr.AppendLine("   " + missingVerses.ToString(Cache.LangProject.TranslatedScriptureOA));

				// Remove the final newline
				int cch = Environment.NewLine.Length;
				strBldr.Remove(strBldr.Length - cch, cch);
				sDetails = strBldr.ToString();

				sectionsToRemove = null;

				return OverwriteType.DataLoss;
			}

			return OverwriteType.FullNoDataLoss;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds back translation(s) that are missing in the savedVersion that are used in this
		/// book.
		/// </summary>
		/// <param name="savedVersion">The saved version.</param>
		/// <returns>list of the HVOs of the writing systems for the the missing back
		/// translation(s)</returns>
		/// <remarks>The ideal solution would be to compare the individual paragraphs to
		/// automatically (as much as possible) preserve back translation data. This method
		/// returns which writing systems are used in this ScrBook, but are completely absent in
		/// the savedVersion.</remarks>
		/// ------------------------------------------------------------------------------------
		protected List<int> FindMissingBts(ScrBook savedVersion)
		{

			List<int> currentBTs = BackTransWs;
			List<int> versionBTs = savedVersion.BackTransWs;

			if (currentBTs != null && versionBTs != null)
			{
				// Remove back translations writing systems from the list for this book if they
				// are used in the savedVersion.
				foreach (int versionBtWs in versionBTs)
				{
					if (currentBTs.Contains(versionBtWs))
						currentBTs.Remove(versionBtWs);
				}
			}

			if (currentBTs != null && currentBTs.Count == 0)
				currentBTs = null;

			// return writing systems used in the current that are not used in the savedVersion
			return currentBTs;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes specified sections in a book.
		/// </summary>
		/// <param name="sectionsToRemove">The sections to remove.</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveSections(List<IScrSection> sectionsToRemove)
		{
			RemoveSections(sectionsToRemove, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes specified sections in a book.
		/// </summary>
		/// <param name="sectionsToRemove">The sections to remove.</param>
		/// <param name="progressDlg">The progress dialog box (can be null). Position will be
		/// incremented once per section to be removed (caller is responsible for setting the
		/// range and message appropriately.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public void RemoveSections(List<IScrSection> sectionsToRemove, IAdvInd4 progressDlg)
		{
			foreach (IScrSection section in sectionsToRemove)
			{
				// Remove all footnotes referenced in this section.
				List<FootnoteInfo> footnotesToRemove = ((ScrSection)section).GetFootnotes();
				foreach (FootnoteInfo footnoteInfo in footnotesToRemove)
					FootnotesOS.Remove(footnoteInfo.footnote);

				SectionsOS.Remove(section);

				if (progressDlg != null)
					progressDlg.Position++;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Class representing a simple range of Scripture verses.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal class MissingVerseRange
		{
			/// <summary>The first verse in the range (i.e., the "min")</summary>
			private ScrReference m_rangeStart;
			/// <summary>The last verse in the range</summary>
			private ScrReference m_rangeEnd;

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="MissingVerseRange"/> class.
			/// </summary>
			/// <param name="rangeMin">The first verse in the range.</param>
			/// <param name="rangeLast">The last verse in the range.</param>
			/// ------------------------------------------------------------------------------------
			internal MissingVerseRange(ScrReference rangeMin, ScrReference rangeLast)
			{
				m_rangeStart = new ScrReference(rangeMin);
				m_rangeEnd = new ScrReference(rangeLast);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Creates a new instance of the <see cref="MissingVerseRange"/> class.
			/// </summary>
			/// <param name="rangeMin">The first verse in the range.</param>
			/// <param name="rangeLim">The verse immediately following the range (i.e. the "lim").
			/// </param>
			/// ------------------------------------------------------------------------------------
			internal static MissingVerseRange CreateFromMinAndLim(ScrReference rangeMin, ScrReference rangeLim)
			{
				return new MissingVerseRange(rangeMin, rangeLim.PrevVerse);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="MissingVerseRange"/> class.
			/// </summary>
			/// <param name="versePrev">The verse immediately preceding the range.</param>
			/// <param name="rangeLim">The verse immediately following the range (i.e. the "lim").
			/// </param>
			/// ------------------------------------------------------------------------------------
			internal static MissingVerseRange CreateFromPrevAndLim(ScrReference versePrev, ScrReference rangeLim)
			{
				return CreateFromMinAndLim(versePrev.NextVerse, rangeLim);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="MissingVerseRange"/> class.
			/// </summary>
			/// <param name="versePrev">The verse immediately preceding the range.</param>
			/// <param name="rangeLast">The last verse in the range.</param>
			/// ------------------------------------------------------------------------------------
			internal static MissingVerseRange CreateFromPrevAndEnd(ScrReference versePrev, ScrReference rangeLast)
			{
				return new MissingVerseRange(versePrev.NextVerse, rangeLast);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Checks to see if the passed range is a subset of this range.
			/// </summary>
			/// <param name="otherRange">The other range.</param>
			/// <returns>true if range is a subset</returns>
			/// ------------------------------------------------------------------------------------
			internal bool IsSubset(MissingVerseRange otherRange)
			{
				return m_rangeStart <= otherRange.m_rangeStart && m_rangeEnd >= otherRange.m_rangeEnd;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Returns a string with this range formatted up all nice and pretty.
			/// </summary>
			/// <param name="scr">The Scripture object</param>
			/// ------------------------------------------------------------------------------------
			public string ToString(IScripture scr)
			{
				return BCVRef.MakeReferenceString(m_rangeStart, m_rangeEnd,
					scr.ChapterVerseSepr, scr.Bridge);
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Resets the start reference to the next verse after the given versePrev value. This
			/// can only be used to reduce the range (increase the start verse), not expand it.
			/// </summary>
			/// <param name="versePrev">The reference of the previous verse, upon which the start
			/// reference of this range should be based</param>
			/// <exception cref="InvalidOperationException">The Start property can only be used to
			/// reduce the verse range, not expand it.</exception>
			/// <exception cref="InvalidOperationException">Illegal verse range.</exception>
			/// ------------------------------------------------------------------------------------
			internal void SetStartBasedOnPrev(ScrReference versePrev)
			{
				ScrReference value = versePrev.NextVerse;
				if (m_rangeStart > value)
					throw new InvalidOperationException("The Start property can only be used to reduce the verse range, not expand it.");
				if (value > m_rangeEnd)
					throw new InvalidOperationException("Illegal verse range.");
				m_rangeStart = value;
			}

			#region Properties
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the start reference.
			/// </summary>
			internal ScrReference Start
			{
				get { return m_rangeStart; }
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the end reference.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			internal ScrReference End
			{
				get { return m_rangeEnd; }
			}
			#endregion
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the current version has any Scripture sections that are are only
		/// partially covered by the Scripture sections of the saved version.
		/// </summary>
		/// <param name="versesInCurrentMissingInSaved">The verses which are found in this
		/// book but missing in the saved version of this book.</param>
		/// <param name="sectionsToRemove">sections that would need to removed from the
		/// current to automatically merge.</param>
		/// <param name="fIntroMissingInSaved">if set to <c>true</c> the current version has an
		/// introduction but the saved version doesn't.</param>
		/// <returns>
		/// 	<c>true</c> if a partial overwrite is possible.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool IsPartialOverwritePossible(
			List<MissingVerseRange> versesInCurrentMissingInSaved,
			out List<IScrSection> sectionsToRemove, bool fIntroMissingInSaved)
		{
			sectionsToRemove = new List<IScrSection>();
			int iMissingHole = 0;

			foreach (IScrSection section in SectionsOS)
			{
				if (section.IsIntro)
				{
					if (!fIntroMissingInSaved)
					{
						// The saved version contains an introduction so, before a partial import, we would
						// need to remove any sections of introduction this book may have.
						sectionsToRemove.Add(section);
					}
					continue;
				}

				if (versesInCurrentMissingInSaved.Count > iMissingHole + 1 &&
					section.VerseRefStart > versesInCurrentMissingInSaved[iMissingHole].End)
				{
					// Make sure that our hole is synchronized with the current section.
					iMissingHole++;
				}

				MissingVerseRange missingVerses = iMissingHole < versesInCurrentMissingInSaved.Count  ?
					versesInCurrentMissingInSaved[iMissingHole] : null;
				if (missingVerses != null)
				{
					if (section.VerseRefStart >= missingVerses.Start &&
						section.VerseRefEnd <= missingVerses.End)
					{
						// We found either an intro section or a section that is contained within
						// the hole, so we can go on to the next hole in the saved version.
						continue;
					}

					if (section.VerseRefStart <= missingVerses.End &&
						section.VerseRefEnd >= missingVerses.Start)
					{
						// Section overlaps the hole but is not wholly contained in it.
						// We cannot do a partial overwrite.
						return false;
					}

					if (section.VerseRefStart > missingVerses.End)
						iMissingHole++;
				}

				sectionsToRemove.Add(section);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines the sections to remove for overwrite.
		/// </summary>
		/// <param name="savedVersion">The saved version.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public List<IScrSection> DetermineSectionsToRemoveForOverwrite(ScrBook savedVersion)
		{
			int iSectionSaved = 0;
			IScrSection sectionSaved = savedVersion.SectionsOS[iSectionSaved];
			foreach (IScrSection sectionCur in SectionsOS)
			{
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Detects any "holes" (missing verses between sections) in savedVersion that are not
		/// in this book. Essentially this determines if the Scripture material in the saved
		/// version is a superset of the material in this book. If not, then missing holes will
		/// be detected.
		/// </summary>
		/// <param name="savedVersion">saved version of this book.</param>
		/// <returns>List of misisng holes.</returns>
		/// ------------------------------------------------------------------------------------
		private List<MissingVerseRange> DetectMissingHoles(ScrBook savedVersion)
		{
			List<MissingVerseRange> curHoles = FindHoles();
			List<MissingVerseRange> savedHoles = savedVersion.FindHoles();

			List<MissingVerseRange> missingVerses = new List<MissingVerseRange>();
			int iHoleCur = 0;
			foreach (MissingVerseRange savedRange in savedHoles)
			{
				while (true)
				{
					while (iHoleCur < curHoles.Count &&
						(savedRange.Start > curHoles[iHoleCur].End ||
						(savedRange.Start == curHoles[iHoleCur].End &&
						savedRange.End != curHoles[iHoleCur].Start)))
					{
						// Scan forward through the reference version until we find an
						// overlapping verse range with compareRange.
						iHoleCur++;
					}

					if (iHoleCur >= curHoles.Count)
					{
						// We have handled all the holes in the reference but the comparison has more missing verses.
						//strBldr.AppendLine(savedRange.ToString(Cache.LangProject.TranslatedScriptureOA));
						missingVerses.Add(new MissingVerseRange(savedRange.Start, savedRange.End));
						break;
					}

					if (savedRange.Start < curHoles[iHoleCur].Start)
					{
						// For the current holes being compared, the comparison version's hole starts before that
						// of the reference version. In other words, the reference version has data which extends
						// beyond that which is covered by the comparison version.
						if (curHoles[iHoleCur].Start <= savedRange.End)
						{
							// There is partial overlap of the holes in the two versions.
							MissingVerseRange rng = MissingVerseRange.CreateFromMinAndLim(savedRange.Start, curHoles[iHoleCur].Start);
							//strBldr.AppendLine(rng.ToString(Cache.LangProject.TranslatedScriptureOA));
							missingVerses.Add(rng);
						}
						else // No hole in the reference corresponding to the hole in the comparison
						{
							//strBldr.AppendLine(savedRange.ToString(Cache.LangProject.TranslatedScriptureOA));
							missingVerses.Add(new MissingVerseRange(savedRange.Start, savedRange.End));
						}
					}

					if (savedRange.End <= curHoles[iHoleCur].End)
						break; // ready to look at the next compareRange hole
					else
					{
						// We've dealt with the first part of the hole in the comparison
						// version (where it overlaps a hole in the reference version).
						// So now shrink the hole down and continue comparing it.
						savedRange.SetStartBasedOnPrev(curHoles[iHoleCur].End);
					}
				}
			}

			return missingVerses;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds missing verse ranges in the current book - excluding beginning and end.
		/// </summary>
		/// <returns>List of missing ranges</returns>
		/// ------------------------------------------------------------------------------------
		private List<MissingVerseRange> FindHoles()
		{
			List<MissingVerseRange> holes = new List<MissingVerseRange>();

			if (SectionsOS.Count == 0)
				return holes; // No sections in current, so holes don't need to be updated

			int iCur = 0;
			while (iCur + 1 < SectionsOS.Count && SectionsOS[iCur].IsIntro)
				iCur++;

			int prevEnd = SectionsOS[iCur].VerseRefMax;
			for (iCur++; iCur < SectionsOS.Count; prevEnd = SectionsOS[iCur].VerseRefMax, iCur++)
			{
				if (SectionsOS[iCur].VerseRefMin > prevEnd && SectionsOS[iCur].VerseRefMin != prevEnd + 1)
				{
					ScrReference begRange = new ScrReference(prevEnd, Versification);
					ScrReference endRange = new ScrReference(SectionsOS[iCur].VerseRefMin, Versification);

					if (begRange.Chapter == endRange.Chapter)
					{
						holes.Add(MissingVerseRange.CreateFromPrevAndLim(begRange, endRange));
					}
					else if (begRange.Chapter + 1 == endRange.Chapter)
					{
						if (begRange.Verse != begRange.LastVerse)
						{
							holes.Add(MissingVerseRange.CreateFromPrevAndLim(begRange, endRange));
						}
						else if (endRange.Verse != 1)
						{
							holes.Add(MissingVerseRange.CreateFromPrevAndLim(begRange, endRange));
						}
					}
					else
					{
						holes.Add(MissingVerseRange.CreateFromPrevAndLim(begRange, endRange));
					}
				}
			}

			return holes;
		}

		#endregion
	}

	#region ScrBookTokenEnumerable class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class that provides an enumerator for parsing Scripture text into tokens that can be
	/// checked.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class ScrBookTokenEnumerable : IEnumerable<ITextToken>
	{
		#region Data members
		private ScrBook m_book;
		private int m_chapterNum;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrBookTokenEnumerable"/> class.
		/// </summary>
		/// <param name="book">The book being parsed.</param>
		/// <param name="chapterNum">The 1-basede canonical chapter number being parse, or 0 to
		/// parse the whole book</param>
		/// ------------------------------------------------------------------------------------
		public ScrBookTokenEnumerable(ScrBook book, int chapterNum)
		{
			m_book = book;
			m_chapterNum = chapterNum;
		}
		#endregion

		#region IEnumerable<ITextToken> Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an enumerator that iterates through the tokens.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerator<ITextToken> GetEnumerator()
		{
			return new ScrCheckingTokenizer(m_book, m_chapterNum);
		}

		#endregion

		#region IEnumerable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an enumerator that iterates through the tokens.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}

	#endregion
}
