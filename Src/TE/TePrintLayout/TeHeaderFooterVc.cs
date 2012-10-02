// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2004' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeHeaderFooterVc.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TeHeaderFooterVc.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeHeaderFooterVc: HeaderFooterVc
	{
		private IScripture m_scr;
		private FilteredScrBooks m_bookFilter;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeHeaderFooterVc"/> class.
		/// </summary>
		/// <param name="cache">DB Connection</param>
		/// <param name="page">Page information</param>
		/// <param name="wsDefault">ID of default writing system</param>
		/// <param name="printDateTime">printing date/time</param>
		/// <param name="filterInstance">book filter instance</param>
		/// ------------------------------------------------------------------------------------
		public TeHeaderFooterVc(FdoCache cache, IPageInfo page, int wsDefault,
			DateTime printDateTime, int filterInstance)
			: base(page, wsDefault, printDateTime, cache)
		{
			m_scr = m_cache.LangProject.TranslatedScriptureOA;
			m_bookFilter = m_cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(filterInstance);
			m_autoAdjustColumns = true;
		}

		#region Overridden HeaderFooterVc Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the header/footer paragraph style name
		/// </summary>
		/// <returns>style name</returns>
		/// ------------------------------------------------------------------------------------
		protected override string HeaderFooterParaStyle
		{
			get { return ScrStyleNames.Header; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the page number
		/// </summary>
		/// <returns>An ITsString with the page number</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString PageNumber
		{
			get
			{
				return m_cache.TsStrFactory.MakeString(m_scr.ConvertToString(m_page.PageNumber), m_wsDefault);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a string that represents the "reference" of the contents of the page (e.g.,
		/// something like "Mark 2,3").
		/// </summary>
		/// <returns>string representing the reference range on the page</returns>
		/// TODO: This method should handle different formatting options to include
		///		  book, chapter and/or verse.
		/// ------------------------------------------------------------------------------------
		public override ITsString PageReference
		{
			get
			{
				string separator = m_scr.ChapterVerseSeparatorForWs(m_wsDefault);

				// get the start and end references for the page
				ITsString tssRef = FirstReference;
				string startRef = (tssRef == null) ? string.Empty : tssRef.Text.Trim();
				tssRef = LastReference;
				string endRef = (tssRef == null) ? string.Empty : tssRef.Text.Trim();

				// Get the chapter position of the start reference
				int sepPos = startRef.IndexOf(separator, StringComparison.Ordinal);
				int chapterPos = (sepPos != -1) ? startRef.LastIndexOf(' ', sepPos) : -1;

				// Get the name of the book from the start reference. If there are no spaces at
				// all in the reference (probably because we're in the book title/intro), then
				// we assume the whole reference is the book name.
				string bookName = (chapterPos == -1) ? startRef : startRef.Substring(0, chapterPos);

				// Get the chapter number for the start reference. If there is no start ref
				// then assume chapter 1.
				string firstChapter;
				if (sepPos != -1)
					firstChapter = startRef.Substring(chapterPos + 1, sepPos - (chapterPos + 1));
				else
				{
					firstChapter = (m_wsDefault == m_cache.DefaultVernWs) ? m_scr.ConvertToString(1) : "1";
				}

				// Get the chapter number for the end reference. If there is no chapter:verse
				// then use an empty reference.
				sepPos = endRef.IndexOf(separator, StringComparison.Ordinal);
				string lastChapter;
				if (sepPos != -1)
				{
					chapterPos = endRef.LastIndexOf(' ', sepPos) + 1;
					lastChapter = endRef.Substring(chapterPos, sepPos - chapterPos);
				}
				else
					firstChapter = lastChapter = string.Empty;

				// Build a TsString of the chapter range. If the start and end chapter are the
				// same then just put it in once. If both references are empty then just use the
				// book name.
				// Note: we have to compare without bidirectional marks!
				string range;
				if (firstChapter == string.Empty && lastChapter == string.Empty)
					range = bookName;
				else if (firstChapter.Trim('\u200E', '\u200f') == lastChapter.Trim('\u200E', '\u200f'))
					range = bookName + " " + firstChapter;
				else
					range = bookName + " " + firstChapter + m_scr.VerseSeparatorForWs(m_wsDefault) + lastChapter;

				// return a TsString of the reference range
				ITsStrFactory factory = TsStrFactoryClass.Create();
				return factory.MakeString(range, m_wsDefault);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book name for the given selection
		/// </summary>
		/// <param name="helper">The selection helper representing the selection (caller
		/// guarantees this to be an insertion point, not a range selection)</param>
		/// <returns>The book name, as a TsString</returns>
		/// ------------------------------------------------------------------------------------
		private ITsString GetBookName(SelectionHelper helper)
		{
			if (helper == null)
				return null;

			IScrBook book = BookAtSelection(helper);
			if (book == null)
				return null;

			int wsActual;
			return book.Name.GetAlternativeOrBestTss(m_wsDefault, out wsActual);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the book at the selection
		/// </summary>
		/// <param name="helper"></param>
		/// <returns>a ScrBook that is at the selection</returns>
		/// ------------------------------------------------------------------------------------
		private IScrBook BookAtSelection(SelectionHelper helper)
		{
			if (helper == null)
				return null;

			if (m_page.Publication == null || !(m_page.Publication is ScripturePublication))
				return null;

			ScripturePublication scrPub = m_page.Publication as ScripturePublication;
			return scrPub.GetBook(helper, SelectionHelper.SelLimitType.Anchor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets Scripture reference that is at the top of the page.
		/// </summary>
		/// <returns>Reference (Book chapter:verse) of the first complete verse at top of page
		/// </returns>
		/// TODO: This method should handle different formatting options to include
		///		  book, chapter and/or verse.
		/// ------------------------------------------------------------------------------------
		public override ITsString FirstReference
		{
			get
			{
				SelectionHelper helper = m_page.TopOfPageSelection;
				ITsString tss = GetSelectionReference(helper, false);

				// selection is a title or not in a ScrBook (???) - use base implementation
				return (tss != null) ? tss : base.FirstReference;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets Scripture reference that is at the bottom of the page.
		/// </summary>
		/// <returns>Reference (Book chapter:verse) of the first complete verse at top of page
		/// </returns>
		/// TODO: This method should handle different formatting options to include
		///		  book, chapter and/or verse.
		/// ------------------------------------------------------------------------------------
		public override ITsString LastReference
		{
			get
			{
				SelectionHelper helper = m_page.BottomOfPageSelection;
				ITsString tss = GetSelectionReference(helper, true);

				// selection is a title or not in a ScrBook (???) - use base implementation
				return (tss != null) ? tss : base.LastReference;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets Scripture reference for a selection
		/// </summary>
		/// <param name="helper">The selection helper that represents the selection</param>
		/// <param name="fInclusive"><c>true</c> if the reference returned should include the
		/// reference of the text of the verse where the selection is, even if that selection
		/// is not at the start of the verse; <c>false</c> if the reference should be that of
		/// the first full verse at or following the selection</param>
		/// <param name="scriptureRef">returns the scripture reference found</param>
		/// <returns>A TsString representing the reference of the selection, or null if the
		/// selection represents a book title or something weird.</returns>
		/// ------------------------------------------------------------------------------------
		private ITsString GetSelectionReference(SelectionHelper helper, bool fInclusive,
			out BCVRef scriptureRef)
		{
			scriptureRef = new BCVRef();
			if (helper != null && m_page.Publication != null && m_page.Publication is ScripturePublication)
			{
				int iParaLevel = helper.GetLevelForTag(StTextTags.kflidParagraphs);
				if (iParaLevel >= 0)
				{
					int hvo = helper.LevelInfo[iParaLevel].hvo;
					IScrTxtPara para =
						m_cache.ServiceLocator.GetInstance<IScrTxtParaRepository>().GetObject(hvo);
					// Look through the verses of the paragraph until we pass the location
					// where the page break occurs. This verse reference will then be the
					// first one on the page.
					ScrVerse firstVerseOnPage = null;
					int ichPageBreak = helper.IchAnchor;
					foreach (ScrVerse verse in para)
					{
						if (!fInclusive)
							firstVerseOnPage = verse;
						if (verse.VerseStartIndex > ichPageBreak ||
							(verse.VerseStartIndex == ichPageBreak && !fInclusive))
							break;
						if (fInclusive)
							firstVerseOnPage = verse;
					}

					ITsString tssBookName = GetBookName(helper);
					if (tssBookName != null)
					{
						ITsStrBldr bldr = tssBookName.GetBldr();
						int cch = bldr.Length;
						if (firstVerseOnPage != null)
						{
							if (firstVerseOnPage.StartRef.Verse != 0)
							{
								bldr.Replace(cch, cch,
									" " + m_scr.ChapterVerseRefAsString(firstVerseOnPage.StartRef),
									null);
							}
							scriptureRef = firstVerseOnPage.StartRef;
						}
						return bldr.GetString();
					}
					//else
					//{
					//    // Probably no verses were found in the paragraph
					//    IVwSelection sel = FindNextPara(helper);
					//    helper = SelectionHelper.Create(sel, helper.RootSite);

					//    return GetSelectionReference(helper, fInclusive, out scriptureRef);
					//}
				}
			}
			return null;
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Go to the next paragraph looking at the specified selection helper.
//		/// </summary>
//		/// <returns>A new selection for the next paragraph, or null if there is no next
//		/// paragraph</returns>
//		/// ------------------------------------------------------------------------------------
//		private IVwSelection FindNextPara(SelectionHelper helper)
//		{
//			IVwRootBox rootb = helper.RootSite.RootBox;

//			int level = helper.GetNumberOfLevels(SelectionHelper.SelLimitType.Top) - 1;
//			while (level >= 0)
//			{
//				int iBox = helper.Selection.get_BoxIndex(false, level);
//				IVwSelection sel =  rootb.MakeSelInBox(helper.Selection, false, level,
//					iBox + 1, true, false, false);
//				if (sel != null)
//					return sel; // We found the next paragraph

//				// Try the next level up
//				level--;
//			}

//			return null;
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selection reference.
		/// </summary>
		/// <param name="helper">The helper.</param>
		/// <param name="fInclusive"><c>true</c> if the reference returned should include the
		/// reference of the text of the verse where the selection is, even if that selection
		/// is not at the start of the verse; <c>false</c> if the reference should be that of
		/// the first full verse at or following the selection</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ITsString GetSelectionReference(SelectionHelper helper, bool fInclusive)
		{
			BCVRef dummyRef;
			return GetSelectionReference(helper, fInclusive, out dummyRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book name
		/// </summary>
		/// <returns>Gets the book name for the page</returns>
		/// <remarks>
		/// If there is only one book on a page, then use the name of that book
		/// If the page starts with the ending contents of one book, then another book starts
		/// on the page then use that book.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public override ITsString BookName
		{
			get
			{
				if (m_page.Publication == null || !(m_page.Publication is ScripturePublication))
					return null;

				// If the top of page selection is in a book title, then use the start book
				SelectionHelper selTopOfPage = m_page.TopOfPageSelection;
				if (selTopOfPage.GetLevelForTag(ScrBookTags.kflidTitle) > -1)
				{
					return GetBookName(selTopOfPage);
				}

				// Get the references at the top and bottom of the page
				BCVRef startRef, endRef;
				GetSelectionReference(selTopOfPage, true, out startRef);
				GetSelectionReference(m_page.BottomOfPageSelection, false, out endRef);

				// If not in scripture, then return nothing
				if (!startRef.Valid || !endRef.Valid)
					return null;

				// If the start and end books are the same, then use that book
				if (startRef.Book == endRef.Book)
					return GetBookName(selTopOfPage);

				// The books are not the same. Look in the book filter to see which book is next after
				// the book at the start of the page.
				for (int bookIndex = 0; bookIndex < m_bookFilter.BookCount; bookIndex++)
				{
					// Look for the book that is at the top of the page
					IScrBook book = m_bookFilter.GetBook(bookIndex);
					if (book.CanonicalNum == startRef.Book)
					{
						// Move to the next book in the filter, which should be the next one
						// on the page.
						if (++bookIndex < m_bookFilter.BookCount)
						{
							book = m_bookFilter.GetBook(bookIndex);
							return StringUtils.MakeTss(book.BestUIName, m_cache.DefaultUserWs);
						}
						break;
					}
				}

				// As a last resort, return the name of the book at the bottom of the page
				return GetBookName(m_page.BottomOfPageSelection);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the total number of pages in the publication
		/// </summary>
		/// <returns>a string that contains the total number of pages</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString TotalPages
		{
			get
			{
				return m_cache.TsStrFactory.MakeString(m_scr.ConvertToString(m_page.PageCount), m_wsDefault);
			}
		}
		#endregion
	}
}
