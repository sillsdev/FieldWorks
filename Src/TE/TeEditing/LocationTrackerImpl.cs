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
// File: LocationTrackerImpl.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Default implementation of the ILocationTracker interface.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class LocationTrackerImpl: ILocationTracker
	{
		/// <summary>
		/// Number of levels to be traversed to get to the book title paragraph in a draft view.
		/// </summary>
		private const int kBookTitleLevelCount = 3;
		/// <summary>
		/// Number of levels to be traversed to get to Scripture section-owned paragraph in a draft view.
		/// </summary>
		private const int kSectionLevelCount = 4;
		/// <summary>
		/// Number of levels to be traversed to get to footnote-owned paragraph in a draft view.
		/// </summary>
		private const int kFootnoteLevelCount = 3;

		#region Data members
		/// <summary></summary>
		protected FdoCache m_cache;
		/// <summary></summary>
		protected FilteredScrBooks m_bookFilter;
		private StVc.ContentTypes m_contentType;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LocationTrackerImpl"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="filterInstance">The filter instance. If 0, then the normal DB book list
		/// is used</param>
		/// ------------------------------------------------------------------------------------
		public LocationTrackerImpl(FdoCache cache, int filterInstance)
			: this(cache, filterInstance, StVc.ContentTypes.kctNormal)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LocationTrackerImpl"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="filterInstance">The filter instance. If 0, then the normal DB book list
		/// is used</param>
		/// <param name="contentType">Indicates the kind of data in the view about which we
		/// are returning selection information.</param>
		/// ------------------------------------------------------------------------------------
		public LocationTrackerImpl(FdoCache cache, int filterInstance, StVc.ContentTypes contentType)
		{
			m_cache = cache;
			m_bookFilter = FilteredScrBooks.GetFilterInstance(cache, filterInstance);
			m_contentType = contentType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the tag (flid) for books in this location tracker
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int BookTag
		{
			get
			{
				return m_bookFilter != null ? m_bookFilter.Tag : (int)Scripture.ScriptureTags.kflidScriptureBooks;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of levels for the given tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="contentType">The kind of selection to return info about</param>
		/// <returns>Number of levels</returns>
		/// ------------------------------------------------------------------------------------
		public static int GetLevelCountForTag(int tag, StVc.ContentTypes contentType)
		{
			int levelCount;
			switch (tag)
			{
				case (int)ScrBook.ScrBookTags.kflidTitle:
					levelCount = kBookTitleLevelCount;
					break;
				case (int)ScrSection.ScrSectionTags.kflidHeading:
				case (int)ScrSection.ScrSectionTags.kflidContent:
					levelCount = kSectionLevelCount;
					break;
				case (int)ScrBook.ScrBookTags.kflidFootnotes:
					levelCount = kFootnoteLevelCount;
					break;
				default:
					Debug.Fail("Invalid tag in GetLevelCountForTag");
					return -1;
			}

			return levelCount + LevelOffset(contentType);
		}

		static int LevelOffset(StVc.ContentTypes contentType)
		{
			switch (contentType)
			{
				case StVc.ContentTypes.kctNormal:
					// Selection is in contents of StTxtPara (in StText in Contents or Heading of ScrSection in ...)
					return 0;
				case StVc.ContentTypes.kctSimpleBT:
					// Selection is in Translation of CmTranslation in Translations of StTxtPara (in StText...)
					return 1;
				case StVc.ContentTypes.kctSegmentBT:
					// Selection is in Comment of CmIndirectAnnotation (free trans) in FT of CmBaseAnnotation (segment)
					// in segments of StTxtPara (in StText...)
					return 2;
			}
			return 0; // make compiler happy :-)
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the level for the given tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <param name="contentType">Kind of selection for which to adjust level</param>
		/// <returns>Index of the level, or -1 if unknown level.</returns>
		/// ------------------------------------------------------------------------------------
		public static int GetLevelIndexForTag(int tag, StVc.ContentTypes contentType)
		{
			int levelIndex;
			switch (tag)
			{
				case (int)CmPicture.CmPictureTags.kflidCaption:
					return 0;
				case (int)StTxtPara.StTxtParaTags.kflidTranslations:
					return 0; // always 0 (except when in a BT of a picture caption)
				case (int)StText.StTextTags.kflidParagraphs:
					levelIndex = 0;
					break;
				case (int)ScrSection.ScrSectionTags.kflidHeading:
				case (int)ScrSection.ScrSectionTags.kflidContent:
				case (int)ScrBook.ScrBookTags.kflidTitle:
				case (int)ScrBook.ScrBookTags.kflidFootnotes:
					levelIndex = 1;
					break;
				case (int)ScrBook.ScrBookTags.kflidSections:
					levelIndex = 2;
					break;
				case (int)Scripture.ScriptureTags.kflidScriptureBooks:
					levelIndex = 3;
					break;
				default:
					Debug.Fail("Invalid tag in GetLevelIndexForTag");
					return -1;
			}

			return levelIndex + LevelOffset(contentType);
		}

		#region ILocationTracker Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the index of the current book in the book filter, or -1 if there is no
		/// current book (e.g. no selection or empty view).
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>
		/// Index of the current book, or -1 if there is no current book.
		/// </returns>
		/// <remarks>The returned value is suitable for making a selection.</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual int GetBookIndex(SelectionHelper selHelper,
			SelectionHelper.SelLimitType selLimitType)
		{
			if (selHelper == null)
				return -1;

			SelLevInfo levInfo;
			if (selHelper.GetLevelInfoForTag(BookTag, selLimitType, out levInfo))
				return levInfo.ihvo;
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the current book, or -1 if there is no current book (e.g. no
		/// selection or empty view).
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>The book hvo.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int GetBookHvo(SelectionHelper selHelper,
			SelectionHelper.SelLimitType selLimitType)
		{
			if (selHelper == null)
				return -1;

			SelLevInfo levInfo;
			if (selHelper.GetLevelInfoForTag(BookTag, selLimitType, out levInfo))
				return levInfo.hvo;
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the index of the section (relative to RootBox), or -1 if we're not in a section
		/// (e.g. the IP is in a title).
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>
		/// Index of the section, or -1 if we're not in a section.
		/// </returns>
		/// <remarks>The returned value is suitable for making a selection.</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual int GetSectionIndexInView(SelectionHelper selHelper,
			SelectionHelper.SelLimitType selLimitType)
		{
			if (selHelper == null)
				return -1;

			SelLevInfo levInfo;
			if (selHelper.GetLevelInfoForTag((int)ScrBook.ScrBookTags.kflidSections,
				selLimitType, out levInfo))
			{
				return levInfo.ihvo;
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the section relative to the book, or -1 if we're not in a section.
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>The section index in book.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int GetSectionIndexInBook(SelectionHelper selHelper,
			SelectionHelper.SelLimitType selLimitType)
		{
			return GetSectionIndexInView(selHelper, selLimitType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the current section, or -1 if we're not in a section (e.g. the IP is
		/// in a title).
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>The section hvo.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int GetSectionHvo(SelectionHelper selHelper, SelectionHelper.SelLimitType selLimitType)
		{
			if (selHelper == null)
				return -1;

			SelLevInfo levInfo;
			if (selHelper.GetLevelInfoForTag((int)ScrBook.ScrBookTags.kflidSections,
				selLimitType, out levInfo))
			{
				return levInfo.hvo;
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set both book and section. Don't make a selection; typically the caller will proceed
		/// to do that.
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <param name="iBook">The index of the book (in the book filter).</param>
		/// <param name="iSection">The index of the section (relative to
		/// <paramref name="iBook"/>), or -1 for a selection that is not in a section (e.g.
		/// title).</param>
		/// <remarks>This method should change only the book and section levels of the
		/// selection, but not any other level.</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void SetBookAndSection(SelectionHelper selHelper,
			SelectionHelper.SelLimitType selLimitType, int iBook, int iSection)
		{
			if (selHelper == null || iBook < 0)
				return;

			int nLevels = selHelper.GetNumberOfLevels(selLimitType);
			if (nLevels == 0)
			{
				Debug.Fail("This should not happen!!!");
				return;
			}

			selHelper.GetLevelInfo(selLimitType)[nLevels - 1].tag = BookTag;
			selHelper.GetLevelInfo(selLimitType)[nLevels - 1].ihvo = iBook;

			if (iSection >= 0 && nLevels >= 2)
			{
				selHelper.GetLevelInfo(selLimitType)[nLevels - 2].tag =
					(int)ScrBook.ScrBookTags.kflidSections;
				selHelper.GetLevelInfo(selLimitType)[nLevels - 2].ihvo = iSection;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of levels for the given tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <returns>Number of levels</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int GetLevelCount(int tag)
		{
			return GetLevelCountForTag(tag, m_contentType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the level for the given tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <returns>Index of the level, or -1 if unknown level.</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int GetLevelIndex(int tag)
		{
			return GetLevelIndexForTag(tag, m_contentType);
		}
		#endregion
	}
}
