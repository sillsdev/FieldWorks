// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeHeaderFooterVc.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Text;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;

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
		private int m_sectionsTag;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeHeaderFooterVc"/> class.
		/// </summary>
		/// <param name="cache">DB Connection</param>
		/// <param name="page">Page information</param>
		/// <param name="wsDefault">ID of default writing system</param>
		/// <param name="printDateTime">printing date/time</param>
		/// <param name="filterInstance">book filter instance</param>
		/// <param name="sectionsTag">The sections tag.</param>
		/// ------------------------------------------------------------------------------------
		public TeHeaderFooterVc(FdoCache cache, IPageInfo page, int wsDefault,
			DateTime printDateTime, int filterInstance, int sectionsTag)
			: base(page, wsDefault, printDateTime, cache)
		{
			m_scr = m_cache.LangProject.TranslatedScriptureOA;
			m_bookFilter = FilteredScrBooks.GetFilterInstance(m_cache, filterInstance);
			m_autoAdjustColumns = true;
			m_sectionsTag = sectionsTag;
		}

		#region IDisposable override
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_scr = null;
			m_cache = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

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
				CheckDisposed();
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

			ScrBook book = BookAtSelection(helper);
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
		private ScrBook BookAtSelection(SelectionHelper helper)
		{
			if (helper == null)
				return null;

			if (m_page.Publication == null || !(m_page.Publication is ScripturePublication))
				return null;

			ScripturePublication scrPub = m_page.Publication as ScripturePublication;
			int hvo = scrPub.GetBookHvo(helper, SelectionHelper.SelLimitType.Anchor);

			return new ScrBook(m_cache, hvo);
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
				CheckDisposed();

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
				CheckDisposed();

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
				ScripturePublication scrPub = m_page.Publication as ScripturePublication;
				int iParaLevel = helper.GetLevelForTag((int)StText.StTextTags.kflidParagraphs);
				if (iParaLevel >= 0)
				{
					ScrTxtPara para = new ScrTxtPara(m_cache, helper.LevelInfo[iParaLevel].hvo);
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
								bldr.Replace(cch, cch, " " +
									((Scripture)m_scr).ChapterVerseRefAsString(firstVerseOnPage.StartRef, m_wsDefault), null);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Go to the next paragraph looking at the specified selection helper.
		/// </summary>
		/// <returns>A new selection for the next paragraph, or null if there is no next
		/// paragraph</returns>
		/// ------------------------------------------------------------------------------------
		private IVwSelection FindNextPara(SelectionHelper helper)
		{
			IVwRootBox rootb = helper.RootSite.RootBox;

			int level = helper.GetNumberOfLevels(SelectionHelper.SelLimitType.Top) - 1;
			while (level >= 0)
			{
				int iBox = helper.Selection.get_BoxIndex(false, level);
				IVwSelection sel =  rootb.MakeSelInBox(helper.Selection, false, level,
					iBox + 1, true, false, false);
				if (sel != null)
					return sel; // We found the next paragraph

				// Try the next level up
				level--;
			}

			return null;
		}

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
				CheckDisposed();

				if (m_page.Publication == null || !(m_page.Publication is ScripturePublication))
					return null;

				ScripturePublication scrPub = m_page.Publication as ScripturePublication;

				// If the top of page selection is in a book title, then use the start book
				SelectionHelper selTopOfPage = m_page.TopOfPageSelection;
				if (selTopOfPage.GetLevelForTag((int)ScrBook.ScrBookTags.kflidTitle) > -1)
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
					ScrBook book = m_bookFilter.GetBook(bookIndex);
					if (book.CanonicalNum == startRef.Book)
					{
						// Move to the next book in the filter, which should be the next one
						// on the page.
						if (++bookIndex < m_bookFilter.BookCount)
						{
							book = m_bookFilter.GetBook(bookIndex);
							return book.BestAvailName;
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
				CheckDisposed();

				ITsStrBldr strBuilder = TsStrBldrClass.Create();
				ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
				// ENHANCE: support script page numbers
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault,
					m_sda.WritingSystemFactory.UserWs);
				strBuilder.Replace(0, 0, m_page.PageCount.ToString(), propsBldr.GetTextProps());
				return strBuilder.GetString();
			}
		}
		#endregion
	}
}
