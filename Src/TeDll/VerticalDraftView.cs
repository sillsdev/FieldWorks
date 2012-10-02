// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Variation on draft view, for now drastically simplified, to support vertical text.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class VerticalDraftView : DraftView
	{
		#region Data members
		/// <summary>The book index</summary>
		protected int m_bookIndex;
		/// <summary>The section index</summary>
		protected int m_sectionIndex;
		/// <summary>
		/// Number of levels to be traversed to get to Scripture section-owned paragraph in a view where the
		/// root object is a section.
		/// </summary>
		protected const int kParaInSectionCount = 2;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="makeRootAutomatically">if set to <c>true</c> [make root automatically].</param>
		/// <param name="persistSettings">if set to <c>true</c> [persist settings].</param>
		/// <param name="filterInstance"></param>
		/// ------------------------------------------------------------------------------------
		public VerticalDraftView(FdoCache cache, bool makeRootAutomatically, bool persistSettings,
			int filterInstance) : base(cache, false, false, makeRootAutomatically, persistSettings,
			TeViewType.VerticalView, filterInstance)
		{
			AutoScroll = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the root so the active section of scripture is the root, with the appropriate fragid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal override void MakeRootObject()
		{
			// Todo JohnT: handle empty Scripture or empty book, if necessary.
			int hvoBook = m_fdoCache.GetVectorItem(m_fdoCache.LangProject.TranslatedScriptureOAHvo,
				(int)Scripture.ScriptureTags.kflidScriptureBooks, m_bookIndex);
			int hvoSection = m_fdoCache.GetVectorItem(hvoBook,
				(int)ScrBook.ScrBookTags.kflidSections, m_sectionIndex);
			m_rootb.SetRootObject(hvoSection, m_draftViewVc, (int)ScrFrags.kfrSection,
				m_styleSheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the basic thing that makes it vertical!
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override OrientationManager CreateOrientationManager()
		{
			return new VerticalOrientationManager(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the root box.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal override IVwRootBox MakeRootBox()
		{
			return VwInvertedRootBoxClass.Create();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Vertical draft view always does automatic horizontal scrolling to show selection
		/// </summary>
		/// <value></value>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override bool DoAutoHScroll
		{
			get
			{
				return true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// And, it never does vertical, because it wraps that way.
		/// </summary>
		/// <value></value>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override bool DoAutoVScroll
		{
			get
			{
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide a TE specific implementation of the EditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override EditingHelper EditingHelper
		{
			get
			{
				CheckDisposed();

				if (m_editingHelper == null)
				{
					Debug.Assert(Cache != null);
					m_editingHelper = new TeEditingHelper(this, Cache,
						FilterInstance, TeViewType.DraftView | TeViewType.Vertical |
						(IsBackTranslation ? TeViewType.BackTranslation : TeViewType.Scripture));
				}
				return m_editingHelper;
			}
		}


		// Todo JohnT: many methods of DraftView to do with selections need to be overridden
		// because our root is a section.

		#region VerticalDraftViewLocationTracker class

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Implementation of ILocationTracker for the vertical DraftView
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		private class VerticalDraftViewLocationTracker : LocationTrackerImpl
		{
			private VerticalDraftView m_vDraft;

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="LocationTrackerImpl"/> class.
			/// </summary>
			/// <param name="vDraft"></param>
			/// <param name="cache">The cache.</param>
			/// <param name="filterInstance">The filter instance.</param>
			/// ------------------------------------------------------------------------------------
			public VerticalDraftViewLocationTracker(VerticalDraftView vDraft, FdoCache cache,
				int filterInstance) : base(cache, filterInstance)
			{
				m_vDraft = vDraft;
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
			public override int GetBookHvo(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{
				return m_vDraft.BookFilter.GetBook(m_vDraft.m_bookIndex).Hvo;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the index of the current book in the book filter, or -1 if there is no
			/// current book (e.g. no selection or empty view).
			/// </summary>
			/// <param name="selHelper">The selection helper.</param>
			/// <param name="selLimitType">Which end of the selection</param>
			/// <returns>
			/// The index of the current book, or -1 if there is no current book.
			/// </returns>
			/// ------------------------------------------------------------------------------------
			public override int GetBookIndex(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{
				return m_vDraft.m_bookIndex;
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
			public override int GetSectionHvo(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{
				return m_vDraft.BookFilter.GetBook(m_vDraft.m_bookIndex).SectionsOS.HvoArray[m_vDraft.m_sectionIndex];
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the index of the section relative to the book, or -1 if we're not in a section.
			/// </summary>
			/// <param name="selHelper">The selection helper.</param>
			/// <param name="selLimitType">Which end of the selection</param>
			/// <returns>The section index in book.</returns>
			/// ------------------------------------------------------------------------------------
			public override int GetSectionIndexInBook(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{
				return m_vDraft.m_sectionIndex;
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
			public override int GetSectionIndexInView(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{
				return 0; // always 0
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
			public override void SetBookAndSection(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType, int iBook, int iSection)
			{
				m_vDraft.m_bookIndex = iBook;
				m_vDraft.m_sectionIndex = iSection;
				m_vDraft.MakeRootObject();
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets the number of levels for the given tag.
			/// </summary>
			/// <param name="tag">The tag.</param>
			/// <returns>Number of levels</returns>
			/// ------------------------------------------------------------------------------------
			public override int GetLevelCount(int tag)
			{
				Debug.Assert(tag == (int)ScrSection.ScrSectionTags.kflidHeading ||
					tag == (int)ScrSection.ScrSectionTags.kflidContent);

				return VerticalDraftView.kParaInSectionCount;
			}
		}
		#endregion
	}
}
