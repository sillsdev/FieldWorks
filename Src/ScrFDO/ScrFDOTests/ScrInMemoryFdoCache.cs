// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrInMemoryFdoCache.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.Utils;


namespace SIL.FieldWorks.FDO.Scripture
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// InMemory Cache with additional methods for scripture
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrInMemoryFdoCache: InMemoryFdoCache
	{
		private int m_hvoScrRefSystem;

		#region Construction
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates and initializes new instance of the <see cref="InMemoryFdoCache"/> class.
		/// </summary>
		/// <param name="wsFactProvider"></param>
		/// -----------------------------------------------------------------------------------
		protected ScrInMemoryFdoCache(IWsFactoryProvider wsFactProvider)
			: base(wsFactProvider)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the scripture in-memory cache with the specified ws factory provider.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ScrInMemoryFdoCache Create()
		{
			return new ScrInMemoryFdoCache(new DefaultWsFactoryProvider());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the scripture in-memory cache with the specified ws factory provider.
		/// </summary>
		/// <param name="wsFactProvider">The ws fact provider.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ScrInMemoryFdoCache Create(IWsFactoryProvider wsFactProvider)
		{
			return new ScrInMemoryFdoCache(wsFactProvider);
		}
		#endregion

		#region Scripture object
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize scripture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeScripture()
		{
			CheckDisposed();
			LoadMetaData("scripture");

			Debug.Assert(m_lp != null);
			m_lp.TranslatedScriptureOAHvo = m_cacheBase.NewHvo(Scripture.kClassId);
			m_cacheBase.SetBasicProps(m_lp.TranslatedScriptureOAHvo, m_lp.Hvo, Scripture.kClassId,
				(int)LangProject.LangProjectTags.kflidTranslatedScripture, 0);
			IScripture scr = m_lp.TranslatedScriptureOA;
			// These values are set the same as in TeScrInitializer
			scr.RestartFootnoteSequence = true;
			scr.CrossRefsCombinedWithFootnotes = false;

			scr.FootnoteMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker;
			scr.FootnoteMarkerSymbol = "*";
			scr.DisplayFootnoteReference = false;

			scr.CrossRefMarkerType = FootnoteMarkerTypes.NoFootnoteMarker;
			scr.CrossRefMarkerSymbol = "*";
			scr.DisplayCrossRefReference = true;

			scr.Versification = Paratext.ScrVers.English;

			InitializeScrRefSystemAndScrBookAnnotations();

			CreateStandardScriptureStyles();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize scripture publications
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeScrPublications()
		{
			CheckDisposed();
			Publication pub = new Publication();
			m_lp.TranslatedScriptureOA.PublicationsOC.Add(pub);
			pub.IsLeftBound = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the Scripture Reference System and create a ScrBookAnnotation for each
		/// book of Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeScrRefSystemAndScrBookAnnotations()
		{
			m_hvoScrRefSystem = m_cacheBase.NewHvo(ScrRefSystem.kClassId);
			m_cacheBase.SetBasicProps(m_hvoScrRefSystem, 0, ScrRefSystem.kClassId, 0, 0);
			IScrRefSystem scrRefSystem = CmObject.CreateFromDBObject(Cache, m_hvoScrRefSystem) as IScrRefSystem;
			((NewFdoCache)m_fdoCache).SetScriptureReferenceSystem(scrRefSystem);

			// Create the ScrBookRefs
			int[] scrBookRefs = new int[66];
			for (int i = 0; i < 66; i++)
			{
				scrBookRefs[i] = m_cacheBase.NewHvo(ScrBookRef.kClassId);
				m_cacheBase.SetBasicProps(scrBookRefs[i], m_hvoScrRefSystem, (int)ScrBookRef.kClassId,
					(int)ScrRefSystem.ScrRefSystemTags.kflidBooks, i + 1);

				m_lp.TranslatedScriptureOA.BookAnnotationsOS.Append(new ScrBookAnnotations());
			}
			m_cacheBase.CacheVecProp(m_hvoScrRefSystem,
				(int)ScrRefSystem.ScrRefSystemTags.kflidBooks, scrBookRefs,
				scrBookRefs.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book and sticks two simple sections into it.
		/// </summary>
		/// <param name="nBook">The one-based canonnical book number</param>
		/// <param name="bookName">The English name of the book</param>
		/// <returns>Newly created book</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBook AddBookWithTwoSections(int nBook, string bookName)
		{
			CheckDisposed();
			IScrBook book = AddBookToMockedScripture(nBook, bookName);
			AddTitleToMockedBook(book.Hvo, bookName);
			IScrSection section1 = AddSectionToMockedBook(book.Hvo);
			AddSectionHeadParaToSection(section1.Hvo, "Head1", "Section Head");
			AddParaToMockedSectionContent(section1.Hvo, "Paragraph");
			section1.AdjustReferences();
			IScrSection section2 = AddSectionToMockedBook(book.Hvo);
			AddSectionHeadParaToSection(section2.Hvo, "Head2", "Section Head");
			AddParaToMockedSectionContent(section2.Hvo, "Paragraph");
			section2.AdjustReferences();
			return book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a book to the mock fdocache
		/// </summary>
		/// <param name="nBookNumber">the one-based canonical book number, eg 2 for Exodus</param>
		/// <param name="bookName">the English name of the book</param>
		/// ------------------------------------------------------------------------------------
		public IScrBook AddBookToMockedScripture(int nBookNumber, string bookName)
		{
			CheckDisposed();
			Debug.Assert(IsModuleLoaded("Scripture"), "Need to load meta data for module Scripture first");
			Debug.Assert(nBookNumber > 0 && nBookNumber < m_SIL_BookCodes.Length);

			int hvoScripture = m_lp.TranslatedScriptureOAHvo;
			int hvoBook = m_cacheBase.NewHvo(ScrBook.kClassId);

			// set up the book
			m_cacheBase.SetBasicProps(hvoBook, hvoScripture, (int)ScrBook.kClassId,
				(int)Scripture.ScriptureTags.kflidScriptureBooks, 1);
			IScrBook book = CmObject.CreateFromDBObject(Cache, hvoBook) as IScrBook;
			book.BookIdRAHvo = m_cacheBase.get_VecItem(
				m_hvoScrRefSystem, (int)ScrRefSystem.ScrRefSystemTags.kflidBooks, nBookNumber - 1);
			book.Name.SetAlternative(bookName, (int)s_wsHvos.En);
			book.CanonicalNum = nBookNumber;

			IScrBookRef bookRef = book.BookIdRA;
			bookRef.BookName.SetAlternative(bookName, (int)s_wsHvos.En);

			//m_cacheBase.AppendToFdoVector(hvoScripture,
			//    (int)BaseScripture.ScriptureTags.kflidScriptureBooks, hvoBook);
			// add this book to translated scripture
			int index = 0;
			foreach (IScrBook existingBook in m_lp.TranslatedScriptureOA.ScriptureBooksOS)
			{
				if (existingBook.CanonicalNum > book.CanonicalNum)
					break;
				index++;
			}

			m_cacheBase.InsertIntoFdoVector(hvoScripture,
				(int)Scripture.ScriptureTags.kflidScriptureBooks, hvoBook, index);

			return book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a book to the mock fdocache as an archive (ie the book is by itself in its own
		/// archive)
		/// </summary>
		/// <param name="nBookNumber">the one-based canonical book number, eg 2 for Exodus</param>
		/// <param name="bookName">the English name of the book</param>
		/// ------------------------------------------------------------------------------------
		public IScrBook AddArchiveBookToMockedScripture(int nBookNumber, string bookName)
		{
			CheckDisposed();
			Debug.Assert(IsModuleLoaded("Scripture"), "Need to load meta data for module Scripture first");
			int hvoScripture = m_lp.TranslatedScriptureOAHvo;
			int hvoDraft = m_cacheBase.NewHvo(ScrDraft.kClassId);
			int hvoBook = m_cacheBase.NewHvo(ScrBook.kClassId);
			int hvoScrRefSystem = m_fdoCache.ScriptureReferenceSystem.Hvo;
			int hvoScrBookRef = m_cacheBase.NewHvo(ScrBookRef.kClassId);

			// set up the draft
			m_cacheBase.SetBasicProps(hvoDraft, hvoScripture, (int)ScrDraft.kClassId,
				(int)Scripture.ScriptureTags.kflidArchivedDrafts, 1);

			// add this draft to Archived Drafts
			m_cacheBase.AppendToFdoVector(hvoScripture, (int)Scripture.ScriptureTags.kflidArchivedDrafts,
				hvoDraft);

			// set up the book
			m_cacheBase.SetBasicProps(hvoBook, hvoDraft, (int)ScrBook.kClassId,
				(int)ScrDraft.ScrDraftTags.kflidBooks, 1);

			IScrBook book = CmObject.CreateFromDBObject(Cache, hvoBook) as IScrBook;
			book.BookIdRAHvo = hvoScrBookRef;
			book.Name.SetAlternative(bookName, (int)s_wsHvos.En);
			book.CanonicalNum = nBookNumber;

			// set up a cooresponding ScrBookRef
			m_cacheBase.SetBasicProps(hvoScrBookRef, hvoScrRefSystem, (int)ScrBookRef.kClassId,
				(int)ScrBook.ScrBookTags.kflidBookId, nBookNumber);

			// add this book to draft's books
			m_cacheBase.AppendToFdoVector(hvoDraft, (int)ScrDraft.ScrDraftTags.kflidBooks, hvoBook);

			return book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a title StText to the specified book in the mock fdocache
		/// </summary>
		/// <param name="bookHvo">the book hvo</param>
		/// <param name="titleText">The text for the title of the book</param>
		/// ------------------------------------------------------------------------------------
		public StText AddTitleToMockedBook(int bookHvo, string titleText)
		{
			CheckDisposed();
			return AddTitleToMockedBook(bookHvo, titleText, (int)InMemoryFdoCache.s_wsHvos.Fr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a title StText to the specified book in the mock fdocache
		/// </summary>
		/// <param name="bookHvo">The book hvo.</param>
		/// <param name="titleText">The title text.</param>
		/// <param name="ws">The writing system to use.</param>
		/// <returns>The StText of the title</returns>
		/// ------------------------------------------------------------------------------------
		public StText AddTitleToMockedBook(int bookHvo, string titleText, int ws)
		{
			CheckDisposed();
			Debug.Assert(IsModuleLoaded("Scripture"), "Need to load meta data for module Scripture first");
			ITsStrFactory fact = TsStrFactoryClass.Create();
			ITsString titleTss = fact.MakeString(titleText, ws);
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			ITsTextProps titleProps = propFact.MakeProps("Title Main",
				ws, 0);
			return AddTitleToMockedBook(bookHvo, titleTss, titleProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a title StText to the sepecified book in the mock fdocache
		/// </summary>
		/// <param name="bookHvo">the book hvo</param>
		/// <param name="contents">Optional paragraph content for title. If this parameter
		/// is <c>null</c>, no paragraphs are added to the StText.</param>
		/// <param name="styleRules">Style rules for paragraph content</param>
		/// ------------------------------------------------------------------------------------
		public StText AddTitleToMockedBook(int bookHvo, ITsString contents,
			ITsTextProps styleRules)
		{
			CheckDisposed();
			Debug.Assert(IsModuleLoaded("Scripture"), "Need to load meta data for module Scripture first");
			FdoCache fdoCache = Cache;
			StText title = new StText(fdoCache, m_cacheBase.NewHvo(StText.kClassId));

			// Append the title to the specified book
			m_cacheBase.CacheObjProp(bookHvo, (int)ScrBook.ScrBookTags.kflidTitle,
				title.Hvo);

			// setup the new title
			m_cacheBase.SetBasicProps(title.Hvo, bookHvo, (int)StText.kClassId,
				(int)ScrBook.ScrBookTags.kflidTitle, 1);

			if (contents != null)
			{
				// add paragraph
				StTxtPara para = new StTxtPara(fdoCache, m_cacheBase.NewHvo(StTxtPara.kClassId));

				// Append the paragraph to the specified book and section
				m_cacheBase.AppendToFdoVector(title.Hvo, (int)StText.StTextTags.kflidParagraphs, para.Hvo);

				// Setup the new paragraph
				m_cacheBase.SetBasicProps(para.Hvo, title.Hvo, (int)StTxtPara.kClassId,
					(int)StText.StTextTags.kflidParagraphs, 1);
				para.StyleRules = styleRules;
				para.Contents.UnderlyingTsString = contents;
			}

			return title;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a section head paragraph to the sepecified book and section in the mock
		/// fdocache
		/// </summary>
		/// <param name="sectionHvo">the hvo of the section</param>
		/// <param name="headingText">The text for the section head</param>
		/// <param name="headingStyleName">The style for the section head</param>
		/// <returns>The newly created section head paragraph</returns>
		/// ------------------------------------------------------------------------------------
		public StTxtPara AddSectionHeadParaToSection(int sectionHvo, string headingText,
			string headingStyleName)
		{
			CheckDisposed();
			Debug.Assert(IsModuleLoaded("Scripture"), "Need to load meta data for module Scripture first");
			ITsStrFactory fact = TsStrFactoryClass.Create();
			ITsString sectionHeadContents = fact.MakeString(headingText,
				(int)InMemoryFdoCache.s_wsHvos.Fr);
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			ITsTextProps sectionHeadProps = propFact.MakeProps(headingStyleName,
				(int)InMemoryFdoCache.s_wsHvos.Fr, 0);
			return AddSectionHeadParaToSection(sectionHvo, sectionHeadContents,
				sectionHeadProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a section head paragraph to the sepecified book and section in the mock
		/// fdocache
		/// </summary>
		/// <param name="sectionHvo">the hvo of the section</param>
		/// <param name="contents">Section head paragraph</param>
		/// <param name="styleRules">Style rules for paragraph content</param>
		/// <returns>The newly created section head paragraph</returns>
		/// ------------------------------------------------------------------------------------
		public StTxtPara AddSectionHeadParaToSection(int sectionHvo, ITsString contents,
			ITsTextProps styleRules)
		{
			CheckDisposed();
			Debug.Assert(IsModuleLoaded("Scripture"), "Need to load meta data for module Scripture first");
			StTxtPara para = new StTxtPara(Cache, m_cacheBase.NewHvo(StTxtPara.kClassId));

			// Append the paragraph to the specified book and section
			int hvoHeading = (int)m_cacheBase.get_Prop(sectionHvo,
				(int)ScrSection.ScrSectionTags.kflidHeading);
			m_cacheBase.AppendToFdoVector(hvoHeading, (int)StText.StTextTags.kflidParagraphs, para.Hvo);

			// Setup the new paragraph
			m_cacheBase.SetBasicProps(para.Hvo, hvoHeading, (int)StTxtPara.kClassId,
				(int)StText.StTextTags.kflidParagraphs, 1);
			para.StyleRules = styleRules;
			para.Contents.UnderlyingTsString = contents;
			return para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an intro section to the specified book in the mock fdocache
		/// </summary>
		/// <param name="bookHvo">the hvo of the book</param>
		/// ------------------------------------------------------------------------------------
		public IScrSection AddIntroSectionToMockedBook(int bookHvo)
		{
			CheckDisposed();
			return AddSectionToMockedBook(bookHvo, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a Scripture section to the specified book in the mock fdocache
		/// </summary>
		/// <param name="bookHvo">the hvo of the book</param>
		/// ------------------------------------------------------------------------------------
		public IScrSection AddSectionToMockedBook(int bookHvo)
		{
			CheckDisposed();
			return AddSectionToMockedBook(bookHvo, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a section (Scripture or Intro) to the sepecified book in the mock fdocache
		/// </summary>
		/// <param name="bookHvo">the hvo of the book</param>
		/// <param name="introSection">true for an intro section</param>
		/// ------------------------------------------------------------------------------------
		public IScrSection AddSectionToMockedBook(int bookHvo, bool introSection)
		{
			Debug.Assert(IsModuleLoaded("Scripture"), "Need to load meta data for module Scripture first");
			IScrSection section = CmObject.CreateFromDBObject(Cache, m_cacheBase.NewHvo(ScrSection.kClassId))
				as IScrSection;

			// Append the section to the specified book
			m_cacheBase.AppendToFdoVector(bookHvo, (int)ScrBook.ScrBookTags.kflidSections,
				section.Hvo);

			// setup the new section
			m_cacheBase.SetBasicProps(section.Hvo, bookHvo, ScrSection.kClassId,
				(int)ScrBook.ScrBookTags.kflidSections, 1);
			int contentHvo = m_cacheBase.NewHvo(StText.kClassId);
			int headingHvo = m_cacheBase.NewHvo(StText.kClassId);
			section.ContentOAHvo = contentHvo;
			m_cacheBase.SetBasicProps(contentHvo, section.Hvo, (int)StText.kClassId,
				(int)ScrSection.ScrSectionTags.kflidContent, 1);
			section.HeadingOAHvo = headingHvo;
			m_cacheBase.SetBasicProps(headingHvo, section.Hvo, (int)StText.kClassId,
				(int)ScrSection.ScrSectionTags.kflidHeading, 1);

			IScrBook book = CmObject.CreateFromDBObject(Cache, bookHvo) as IScrBook;
			section.VerseRefEnd = book.CanonicalNum * 1000000 + 1 * 1000 + (introSection ? 0 : 1);

			return section;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a content paragraph to the specified section in the mock fdocache
		/// </summary>
		/// <param name="sectionHvo">the hvo of the section</param>
		/// <param name="paraStyleName">the paragraph style name</param>
		/// ------------------------------------------------------------------------------------
		public StTxtPara AddParaToMockedSectionContent(int sectionHvo, string paraStyleName)
		{
			CheckDisposed();
			int hvoContentStText = (int)m_cacheBase.get_Prop(sectionHvo,
				(int)ScrSection.ScrSectionTags.kflidContent);

			return AddParaToMockedText(hvoContentStText, paraStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an ITsString from a format string.
		/// </summary>
		/// <param name="book">The book for inserting footnotes into (can be null if there are
		/// no footnotes)</param>
		/// <param name="para">The paragraph to insert the formatted text into.</param>
		/// <param name="format">format string, which may include these special "commands":
		/// \c - insert a chapter number run
		/// \v - insert a verse number run
		/// \* - insert a simple text run
		/// \*(char style name) - insert a text run with a character style.
		/// \i - insert a picture (text must be the text rep of the picture (see pic code))
		/// \f - insert a footnote</param>
		/// \^ - end of the current footnote (required for every footnote)
		/// <param name="ws">writing system for each run of text</param>
		/// ------------------------------------------------------------------------------------
		public void AddFormatTextToMockedPara(IScrBook book, StTxtPara para, string format, int ws)
		{
			CheckDisposed();
			para.Contents.UnderlyingTsString = CreateFormatText(book,
				para.Contents.UnderlyingTsString.GetBldr(), format, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an ITsString from a format string.
		/// </summary>
		/// <param name="book">The book for inserting footnotes into (can be null if there are
		/// no footnotes)</param>
		/// <param name="strBldr">The ITsStrBldr to add the text to (if null a new one will be
		/// created)</param>
		/// <param name="format">format string, which may include these special "commands":
		/// \c - insert a chapter number run
		/// \v - insert a verse number run
		/// \* - insert a simple text run
		/// \*(char style name) - insert a text run with a character style.
		/// \i - insert a picture (text must be the text rep of the picture (see pic code))
		/// \f - insert a footnote</param>
		/// \^ - end of the current footnote (required for every footnote)
		/// <param name="ws">writing system for each run of text</param>
		/// <returns>the completed ITsString</returns>
		/// ------------------------------------------------------------------------------------
		public static ITsString CreateFormatText(IScrBook book, ITsStrBldr strBldr,
			string format, int ws)
		{
			if (strBldr == null)
				strBldr = TsStrBldrClass.Create();

			int nChapter = 1, nVerse = 1;

			for (int i = 0; i < format.Length; )
			{
				// skip the backslash (verify that it is there first)
				if (format[i] != '\\')
					Debug.Assert(false, @"Format string must start every text run with \*: {0}" +
						format.Substring(i));
				if (++i >= format.Length)
					break;

				// save the field type character
				char field = format[i];
				if (++i >= format.Length)
					break;

				// determine the endmarker we'll look for
				string endMarker = (field == 'f' || field == 'i') ? @"\^" : @"\";

				// extract the data for the field
				int lim = format.IndexOf(endMarker, i);
				if (lim == -1 && field == 'f')
					Debug.Assert(false, @"Format string must have a \^ to end footnote!");
				else if (lim == -1)
					lim = format.Length;
				string fieldData = format.Substring(i, lim - i);

				// remember pos of next backslash, or the end of the format string
				i = lim;
				//skip empty commands, such as \^
				if (fieldData.Length == 0)
					continue;

				// what kind of command is this?
				switch (field)
				{
					case 'c':
						Int32.TryParse(fieldData, out nChapter);
						AddRunToStrBldr(strBldr, fieldData, ws, "Chapter Number");
						break;
					case 'v':
						FdoCache cache = book.Cache;
						LgWritingSystem vernWs = new LgWritingSystem(cache, cache.DefaultVernWs);
						string sBridge = cache.LangProject.TranslatedScriptureOA.Bridge;
						string [] pieces = fieldData.Split(new string[] {sBridge}, 2, StringSplitOptions.RemoveEmptyEntries);
						StringBuilder strb = new StringBuilder();
						string sLastVerse = pieces[pieces.Length - 1];
						Int32.TryParse(fieldData, out nVerse);
						if (vernWs.RightToLeft && pieces.Length == 2)
						{
							// The verse number run has a bridge and is in a right-to-left
							// writing system. Construct a verse bridge with right-to-left
							// characters adjacent to the bridge character.
							strb.Append(pieces[0] + '\u200f' + sBridge + '\u200f' + pieces[1]);
						}
						else
							strb.Append(fieldData);

						AddRunToStrBldr(strBldr, strb.ToString(), ws, "Verse Number");
						break;
					case 'f':
						StFootnote footnote = new StFootnote();
						book.FootnotesOS.Append(footnote);
						footnote.InsertOwningORCIntoPara(strBldr, strBldr.Length, ws);
						footnote.FootnoteMarker.UnderlyingTsString = TsStringHelper.MakeTSS("a", ws); // auto-generate
						if (fieldData.IndexOf(@"\f") != -1)
							Debug.Assert(false, @"Format string must not nest \f within another \f..\^");
						StTxtPara para = AppendParagraph(footnote, fieldData, ws); //recursively calls CreateText to process any char styles
						para.StyleRules = StyleUtils.ParaStyleTextProps("Note General Paragraph");
						//TODO: add multiple paragraphs for a footnote
						break;
					case 'i':
						CmPicture picture = new CmPicture(book.Cache, fieldData, StringUtils.LocalPictures,
							new BCVRef(book.CanonicalNum, nChapter, nVerse),
							book.Cache.LangProject.TranslatedScriptureOA as IPictureLocationBridge);
						picture.AppendPicture(ws, strBldr);
						break;
					case '*':
						{
							int wsRun = ws;
							string charStyleName = null;
							// if we have an optional character style in parens, process it
							if (fieldData[0] == '(')
							{
								int endParen = fieldData.IndexOf(")", 0);
								Debug.Assert(endParen > 1); // caller must provide something within parens
								if (endParen != -1)
								{
									charStyleName = fieldData.Substring(1, endParen - 1);
									fieldData = fieldData.Substring(endParen + 1);
								}
							}
							// if we have an optional writing system specifier, process it
							if (fieldData[0] == '|' && fieldData.Length > 3 && fieldData[3] == '|')
							{
								wsRun = book.Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(fieldData.Substring(1, 2));
								if (wsRun > 0)
									fieldData = fieldData.Substring(4);
								else
									wsRun = ws;
							}
							AddRunToStrBldr(strBldr, fieldData, wsRun, charStyleName);
							break;
						}
				}
			}
			return strBldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a run of text to a back translation of a paragraph.
		/// </summary>
		/// <param name="para">given paragraph</param>
		/// <param name="ws">given writing system for the back translation</param>
		/// <param name="runText">given text to append to back translation</param>
		/// ------------------------------------------------------------------------------------
		public void AppendRunToBt(StTxtPara para, int ws, string runText)
		{
			CheckDisposed();
			ICmTranslation trans = para.GetOrCreateBT();
			ITsStrBldr bldr = trans.Translation.GetAlternative(ws).UnderlyingTsString.GetBldr();
			ITsTextProps ttp = StyleUtils.CharStyleTextProps(null, ws);
			int bldrLength = bldr.Length;
			bldr.ReplaceRgch(bldrLength, bldrLength, runText, runText.Length, ttp);
			trans.Translation.SetAlternative(bldr.GetString(), ws);
		}

		#region Footnotes
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a mindless footnote (i.e., it's marker, paragraph style, etc. won't be set)
		/// to a paragraph in a book.
		/// </summary>
		/// <param name="book">Book to insert footnote into</param>
		/// <param name="para">the paragraph in which to insert the footnote ORC</param>
		/// <param name="ichPos">the zero-based character offset at which to insert the footnote
		/// ORC into the paragraph</param>
		/// <returns>the new footnote</returns>
		/// ------------------------------------------------------------------------------------
		public StFootnote AddFootnote(IScrBook book, IStTxtPara para, int ichPos)
		{
			CheckDisposed();
			return base.AddFootnote(book.FootnotesOS, para, ichPos, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a footnote to a paragraph in a book. This overload also sets the footnote text
		/// and footnote style.
		/// </summary>
		/// <param name="book"></param>
		/// <param name="para">the paragraph in which to insert the footnote ORC</param>
		/// <param name="ichPos">the zero-based character offset at which to insert the footnote
		/// ORC into the paragraph</param>
		/// <param name="footnoteText">text for the footnote</param>
		/// ------------------------------------------------------------------------------------
		public StFootnote AddFootnote(IScrBook book, IStTxtPara para, int ichPos, string footnoteText)
		{
			CheckDisposed();
			return base.AddFootnote(book.FootnotesOS, para, ichPos, footnoteText);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function: Append a run to the given string builder
		/// </summary>
		/// <param name="strBldr"></param>
		/// <param name="text"></param>
		/// <param name="ws"></param>
		/// <param name="charStyle"></param>
		/// ------------------------------------------------------------------------------------
		private static void AddRunToStrBldr(ITsStrBldr strBldr, string text, int ws,
			string charStyle)
		{
			strBldr.Replace(strBldr.Length, strBldr.Length, text,
				StyleUtils.CharStyleTextProps(charStyle, ws));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a paragraph with the given text, and append it to the given footnote's
		/// content.
		/// </summary>
		/// <param name="footnote">footnote to append to</param>
		/// <param name="format">(See CreateText for the definition of the format string)</param>
		/// <param name="ws">writing system for the paragraph text</param>
		/// <returns>the new paragraph</returns>
		/// ------------------------------------------------------------------------------------
		private static StTxtPara AppendParagraph(StFootnote footnote, string format, int ws)
		{
			// insert a new para in the footnote
			StTxtPara para = new StTxtPara();
			footnote.ParagraphsOS.Append(para);
			para.Contents.UnderlyingTsString =
				CreateFormatText(null, para.Contents.UnderlyingTsString.GetBldr(), format, ws);
			para.StyleRules = StyleUtils.ParaStyleTextProps("Note General Paragraph");

			return para;
		}

		#endregion

		#region Style sheet stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create several standard TE scripture styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateStandardScriptureStyles()
		{
			AddScrStyle("Normal", ContextValues.Internal, StructureValues.Undefined, FunctionValues.Prose, false);
			AddScrStyle("Paragraph", ContextValues.Text, StructureValues.Body, FunctionValues.Prose, false);
			AddScrStyle("Section Head", ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, false);
			AddScrStyle("Intro Paragraph", ContextValues.Intro, StructureValues.Body, FunctionValues.Prose, false, 2);
			AddScrStyle("Intro Section Head", ContextValues.Intro, StructureValues.Heading, FunctionValues.Prose, false);
			AddScrStyle("Chapter Number", ContextValues.Text, StructureValues.Body, FunctionValues.Chapter, true);
			AddScrStyle("Verse Number", ContextValues.Text, StructureValues.Body, FunctionValues.Verse, true);
			AddScrStyle("Title Main", ContextValues.Title, StructureValues.Body, FunctionValues.Prose, false);
			AddScrStyle("Note General Paragraph", ContextValues.Note, StructureValues.Undefined, FunctionValues.Prose, false);
			AddScrStyle("Note Marker", ContextValues.Internal, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Note Target Reference", ContextValues.InternalMappable, StructureValues.Undefined, FunctionValues.Footnote, true);
			AddScrStyle("Note Cross-Reference Paragraph", ContextValues.Note, StructureValues.Undefined, FunctionValues.Prose, false);
			AddScrStyle("Citation Line1", ContextValues.Text, StructureValues.Body, FunctionValues.Line, false);
			AddScrStyle("Line1", ContextValues.Text, StructureValues.Body, FunctionValues.Line, false);
			AddScrStyle("Line2", ContextValues.Text, StructureValues.Body, FunctionValues.Line, false);
			AddScrStyle("Line3", ContextValues.Text, StructureValues.Body, FunctionValues.Line, false, 2);
			AddScrStyle("Section Range Paragraph", ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, false);
			AddScrStyle("Section Head Major", ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, false);
			AddScrStyle("Section Head Minor", ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, false);
			AddScrStyle("Parallel Passage Reference", ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, false);
			AddScrStyle("Title Secondary", ContextValues.Title, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Title Tertiary", ContextValues.Title, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Remark", ContextValues.Annotation, StructureValues.Undefined, FunctionValues.Prose, false);
			AddScrStyle("Hyperlink", ContextValues.Internal, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Emphasis", ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Key Word", ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Gloss", ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Untranslated Word", ContextValues.BackTranslation, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Quoted Text", ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, true);
			AddScrStyle("Doxology", ContextValues.General, StructureValues.Undefined, FunctionValues.Prose, true, 3);
			AddScrStyle("List Item1", ContextValues.Text, StructureValues.Body, FunctionValues.Prose, false, 3);
			AddScrStyle("List Item2", ContextValues.Text, StructureValues.Body, FunctionValues.Prose, false, 4);
			AddScrStyle("List Item3", ContextValues.Text, StructureValues.Body, FunctionValues.Prose, false, 4);
			AddScrStyle("Verse Number In Note", ContextValues.Note, StructureValues.Undefined, FunctionValues.Footnote, true, 3);
			AddScrStyle("Alternate Reading", ContextValues.Note, StructureValues.Undefined, FunctionValues.Footnote, true, 2);
			AddScrStyle("Stanza Break", ContextValues.Text, StructureValues.Body, FunctionValues.StanzaBreak, false, 1);
			// Note: Other "standard" Scr styles could be created here, but they aren't needed for tests
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new style.
		/// </summary>
		/// <param name="name">style name</param>
		/// <param name="context">style context</param>
		/// <param name="structure">style structure</param>
		/// <param name="function">style function</param>
		/// <param name="isCharStyle">true if character style, otherwise false</param>
		/// <param name="userLevel">User level</param>
		/// ------------------------------------------------------------------------------------
		public void AddScrStyle(string name, ContextValues context, StructureValues structure,
			FunctionValues function, bool isCharStyle, int userLevel)
		{
			AddStyle(m_lp.TranslatedScriptureOA.StylesOC, name, context, structure, function, isCharStyle, userLevel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new style, with user level 0.
		/// </summary>
		/// <param name="name">style name</param>
		/// <param name="context">style context</param>
		/// <param name="structure">style structure</param>
		/// <param name="function">style function</param>
		/// <param name="isCharStyle">true if character style, otherwise false</param>
		/// ------------------------------------------------------------------------------------
		public void AddScrStyle(string name, ContextValues context, StructureValues structure,
			FunctionValues function, bool isCharStyle)
		{
			CheckDisposed();
			AddScrStyle(name, context, structure, function, isCharStyle, 0);
		}
		#endregion

		#region Annotations
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes scripture annotation categories
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeScrAnnotationCategories()
		{
			CheckDisposed();
			// Initialize the annotation category possibility list.
			IScripture scr = m_lp.TranslatedScriptureOA;
			scr.NoteCategoriesOAHvo = m_cacheBase.NewHvo(CmPossibilityList.kClassId);

			// Add an annotation category (for Discourse)
			m_categoryDiscourse = new CmPossibility();
			scr.NoteCategoriesOA.PossibilitiesOS.Append(m_categoryDiscourse);
			m_categoryDiscourse.Name.SetAlternative("Discourse", s_wsHvos.En);
			m_cacheBase.CacheGuidProp(m_categoryDiscourse.Hvo,
				(int)CmObjectFields.kflidCmObject_Guid, Guid.NewGuid());

			// Add an annotation category (for Grammar)
			m_categoryGrammar = new CmPossibility();
			scr.NoteCategoriesOA.PossibilitiesOS.Append(m_categoryGrammar);
			m_categoryGrammar.Name.SetAlternative("Grammar", s_wsHvos.En);
			m_cacheBase.CacheGuidProp(m_categoryGrammar.Hvo,
				(int)CmObjectFields.kflidCmObject_Guid, Guid.NewGuid());

			// add a sub-annotation category (for "Pronominal reference")
			m_categoryGrammar_PronominalRef = new CmPossibility();
			m_categoryGrammar.SubPossibilitiesOS.Append(m_categoryGrammar_PronominalRef);
			m_categoryGrammar_PronominalRef.Name.SetAlternative("Pronominal reference", s_wsHvos.En);
			m_cacheBase.CacheGuidProp(m_categoryGrammar_PronominalRef.Hvo,
				(int)CmObjectFields.kflidCmObject_Guid, Guid.NewGuid());

			// add a sub-sub-annotation category (for "Extended use")
			m_categoryGrammar_PronominalRef_ExtendedUse = new CmPossibility();
			m_categoryGrammar_PronominalRef.SubPossibilitiesOS.Append(m_categoryGrammar_PronominalRef_ExtendedUse);
			m_categoryGrammar_PronominalRef_ExtendedUse.Name.SetAlternative("Extended use", s_wsHvos.En);
			m_cacheBase.CacheGuidProp(m_categoryGrammar_PronominalRef_ExtendedUse.Hvo,
				(int)CmObjectFields.kflidCmObject_Guid, Guid.NewGuid());

			// Add an annotation category (for Gnarly)
			m_categoryGnarly = new CmPossibility();
			scr.NoteCategoriesOA.PossibilitiesOS.Append(m_categoryGnarly);
			m_categoryGnarly.Name.SetAlternative("Gnarly", s_wsHvos.En);
			m_cacheBase.CacheGuidProp(m_categoryGnarly.Hvo,
				(int)CmObjectFields.kflidCmObject_Guid, Guid.NewGuid());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an annotation for the given paragraph and Scripture reference.
		/// </summary>
		/// <param name="cmObject">The object where we want to add the annotation</param>
		/// <param name="scrRef">The Scripture reference for the annotation.</param>
		/// <param name="noteType">Type of the note: NoteType.Consultant or NoteType.Translator.</param>
		/// <param name="sDiscussion">The text to put in the discussion field.</param>
		/// <returns>the created annotation</returns>
		/// ------------------------------------------------------------------------------------
		public IScrScriptureNote AddAnnotation(CmObject cmObject, BCVRef scrRef, NoteType noteType,
			string sDiscussion)
		{
			IScrScriptureNote annotation = AddAnnotation(cmObject, scrRef, noteType);
			if (!String.IsNullOrEmpty(sDiscussion))
			{
				StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
				ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, Cache.DefaultAnalWs);
				paraBldr.AppendRun(sDiscussion, propsBldr.GetTextProps());
				annotation.DiscussionOA = new StJournalText();
				InitializeText(paraBldr, (StText)annotation.DiscussionOA);
			}
			return annotation;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an annotation for the given paragraph and Scripture reference.
		/// </summary>
		/// <param name="cmObject">The object where we want to add the annotation</param>
		/// <param name="scrRef">The Scripture reference for the annotation.</param>
		/// <param name="noteType">Type of the note: NoteType.Consultant or NoteType.Translator.
		/// </param>
		/// <returns>the created annotation</returns>
		/// ------------------------------------------------------------------------------------
		public IScrScriptureNote AddAnnotation(CmObject cmObject, BCVRef scrRef, NoteType noteType)
		{
			Debug.Assert(scrRef.Book > 0);
			Debug.Assert(m_lp != null);
			Debug.Assert(m_lp.TranslatedScriptureOA != null);
			IScripture scr = m_lp.TranslatedScriptureOA;
			ScrBookAnnotations annotations =
				(ScrBookAnnotations)scr.BookAnnotationsOS[scrRef.Book - 1];
			int iPos;
			IScrScriptureNote annotation = annotations.InsertNote(scrRef, scrRef, cmObject, cmObject,
				LangProject.kguidAnnConsultantNote,	out iPos);
			Guid noteTypeGuid = Guid.Empty;
			switch (noteType)
			{
				case NoteType.Consultant:
					noteTypeGuid = LangProject.kguidAnnConsultantNote;
					break;

				case NoteType.Translator:
					noteTypeGuid = LangProject.kguidAnnTranslatorNote;
					break;

				case NoteType.CheckingError:
					noteTypeGuid = LangProject.kguidAnnCheckingError;
					break;
			}
			annotation.AnnotationTypeRA = new CmAnnotationDefn(Cache, noteTypeGuid);
			m_cacheBase.CacheGuidProp(annotation.AnnotationTypeRAHvo,
				(int)CmObjectFields.kflidCmObject_Guid, noteTypeGuid);
			// ENHANCE: Set source to either human or computer agent, depending on NoteType
			annotation.SourceRA = null;
			annotation.DateCreated = DateTime.Now;

			return annotation;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the text for the paragraph with the specified builder, or create an
		/// empty paragraph if the builder is null.
		/// </summary>
		/// <param name="bldr">paragraph builder</param>
		/// <param name="text">StText</param>
		/// ------------------------------------------------------------------------------------
		private void InitializeText(StTxtParaBldr bldr, StText text)
		{
			if (bldr == null)
			{
				StTxtPara para = new StTxtPara();
				text.ParagraphsOS.Append(para);
				para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.Remark);
			}
			else
			{
				bldr.CreateParagraph(text.Hvo);
			}
		}
		#endregion
	}
}
