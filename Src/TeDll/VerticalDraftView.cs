// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using XCore;

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
		/// <param name="app">The app.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="makeRootAutomatically">if set to <c>true</c> [make root automatically].</param>
		/// <param name="filterInstance">The filter instance.</param>
		/// ------------------------------------------------------------------------------------
		public VerticalDraftView(FdoCache cache, IApp app, IHelpTopicProvider helpTopicProvider,
			bool makeRootAutomatically, int filterInstance)
			: base(cache, filterInstance, app, "vertical draft view", true, false,
			makeRootAutomatically, TeViewType.VerticalView, -1, helpTopicProvider)
		{
			AutoScroll = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the root so the active section of scripture is the root, with the appropriate fragid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void MakeRootObject()
		{
			// Todo JohnT: handle empty Scripture or empty book, if necessary.
			IScrBook book = m_fdoCache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[m_bookIndex];
			IScrSection section = book.SectionsOS[m_sectionIndex];
			m_rootb.SetRootObject(section.Hvo, m_vc, (int)ScrFrags.kfrSection, m_styleSheet);
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
		protected override IVwRootBox MakeRootBox()
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
		protected override EditingHelper CreateEditingHelper()
		{
			Debug.Assert(Cache != null);
			return new TeEditingHelper(this, Cache, m_filterInstance, m_viewType, m_app);
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
			/// Gets the current book, or null if there is no current book (e.g. no
			/// selection or empty view).
			/// </summary>
			/// <param name="selHelper">The selection helper.</param>
			/// <param name="selLimitType">Which end of the selection</param>
			/// <returns>The book.</returns>
			/// ------------------------------------------------------------------------------------
			public override IScrBook GetBook(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{
				return m_vDraft.BookFilter.GetBook(m_vDraft.m_bookIndex);
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
			/// Gets the current section, or null if we're not in a section (e.g. the IP is
			/// in a title).
			/// </summary>
			/// <param name="selHelper">The selection helper.</param>
			/// <param name="selLimitType">Which end of the selection</param>
			/// <returns>The section.</returns>
			/// ------------------------------------------------------------------------------------
			public override IScrSection GetSection(SelectionHelper selHelper,
				SelectionHelper.SelLimitType selLimitType)
			{
				return m_vDraft.BookFilter.GetBook(m_vDraft.m_bookIndex).SectionsOS[m_vDraft.m_sectionIndex];
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
				Debug.Assert(tag == ScrSectionTags.kflidHeading || tag == ScrSectionTags.kflidContent);
				return VerticalDraftView.kParaInSectionCount;
			}
		}
		#endregion
	}
}
