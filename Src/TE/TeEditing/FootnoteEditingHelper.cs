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
// File: FootnoteEditingHelper.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class contains editing methods for TE's Footnote view.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FootnoteEditingHelper : TeEditingHelper
	{
		#region Data members
		private FwRootSite m_draftView = null;
		private TeEditingHelper m_draftViewEditingHelper = null;
		#endregion

		#region constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FootnoteEditingHelper"/> class.
		/// </summary>
		/// <param name="callbacks">implementation of <see cref="IEditingCallbacks"/></param>
		/// <param name="cache">The cache for the DB connection</param>
		/// <param name="filterInstance">The special tag for the book filter</param>
		/// <param name="draftView">The corresponding draftview pane. If we determine that the
		/// "other pane" is supposed to be the other pane in the split window, then this
		/// should be used as the other pane as well.</param>
		/// <param name="viewType">Bit-flags indicating type of view.</param>
		/// <param name="app">The app.</param>
		/// ------------------------------------------------------------------------------------
		public FootnoteEditingHelper(IEditingCallbacks callbacks, FdoCache cache,
			int filterInstance, FwRootSite draftView, TeViewType viewType, IApp app) :
			base(callbacks, cache, filterInstance, viewType, app)
		{
			m_draftView = draftView;
			if (m_draftView != null) // can be null for tests
			{
				m_draftViewEditingHelper = m_draftView.EditingHelper as TeEditingHelper;
				Debug.Assert(m_draftViewEditingHelper != null);
			}
		}
		#endregion

		#region Overrides of TeEditingHelper

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the index of the book for the current selection in the corresponding
		/// draft view. Returns -1 if there is no draft view (shouldn't ever happen), the
		/// selection is undefined, or something else more destructive.
		/// </summary>
		/// <remarks>
		/// Since a selection can cross StText bounds, it is possible for the anchor of the
		/// selection to be in a book title and the end to be in a Scripture section, or vice
		/// versa. In this case this property will return information based strictly on the
		/// location of the anchor. When setting the BookIndex, the insertion point will be
		/// placed at the beginning of the book's title.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public override int BookIndex
		{
			get
			{
				CheckDisposed();
				return m_draftView != null ? m_draftViewEditingHelper.BookIndex : -1;
			}
			set
			{
				CheckDisposed();

				if (m_draftView != null)
					m_draftViewEditingHelper.BookIndex = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the section for the current selection in the corresponding draft
		/// view. Returns -1 if there is no draft view (shouldn't ever happen), the selection
		/// is not in a section.
		/// </summary>
		/// <remarks>
		/// Since a selection can cross StText bounds, it is possible for the anchor of the
		/// selection to be in a book title and the end to be in a Scripture section, or vice
		/// versa. In this case this property will return information based strictly on the
		/// location of the anchor.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public override int SectionIndex
		{
			get
			{
				CheckDisposed();
				return m_draftView != null ? m_draftViewEditingHelper.SectionIndex : -1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides simple access to the start ref at the current insertion point in the
		/// corresponding draft view.
		/// </summary>
		/// <remarks>
		/// This property is not guaranteed to return a ScrReference containing the book, chapter,
		/// AND verse.  It will return as much as it can, but not neccessarily all of it. It
		/// will not search back into a previous section if it can't find the verse number in
		/// the current section. This means that if a verse crosses a section break, the verse
		/// number will be inferred from the section start ref. For now, section refs are not
		/// very reliable, but this should work pretty well when TE-278 and TE-329 are
		/// completed.
		/// ENHANCE: Consider checking the end of the previous section if
		/// the verse number isn't found, and if it's still in the same chapter, return the
		/// verse number from the end of the previous section instead.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public override ScrReference CurrentStartRef
		{
			get
			{
				CheckDisposed();

				return m_draftView != null ? m_draftViewEditingHelper.CurrentStartRef :
					base.CurrentStartRef;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the current selection is in a book title in the corresponding
		/// draft view. Returns false if there is no draft view (shouldn't ever happen).
		/// </summary>
		/// <remarks>
		/// Since a selection can cross StText bounds, it is possible for the anchor of the
		/// selection to be in a book title and the end to be in a Scripture section, or vice
		/// versa. In this case this property will return information based strictly on the
		/// location of the anchor.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public override bool InBookTitle
		{
			get
			{
				CheckDisposed();
				return m_draftView != null ? m_draftViewEditingHelper.InBookTitle : false;
			}
		}
		#endregion

		#region Goto verse methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to set the selection in the corresponding draft view immediately following
		/// the last character of the closest verse number to the requested verse. If no section
		/// exists within one chapter of the requested verse, the selection will not be changed.
		/// </summary>
		/// <param name="targetRef">Reference to seek</param>
		/// <returns>true if the selection is changed (to the requested verse or one nearby);
		/// false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public override bool GotoVerse(ScrReference targetRef)
		{
			CheckDisposed();

			if (m_draftView != null)
			{
				m_draftView.Focus();
				return m_draftViewEditingHelper.GotoVerse(targetRef);
			}

			return false;
		}
		#endregion

		#region Goto Book methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the first Scripture book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void GoToFirstBook()
		{
			CheckDisposed();

			if (m_draftView != null)
			{
				m_draftView.Focus();
				m_draftViewEditingHelper.GoToFirstBook();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the previous Scripture book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void GoToPrevBook()
		{
			CheckDisposed();

			if (m_draftView != null)
			{
				m_draftView.Focus();
				m_draftViewEditingHelper.GoToPrevBook();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the next Scripture book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void GoToNextBook()
		{
			CheckDisposed();

			if (m_draftView != null)
			{
				m_draftView.Focus();
				m_draftViewEditingHelper.GoToNextBook();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the last Scripture book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void GoToLastBook()
		{
			CheckDisposed();

			if (m_draftView != null)
			{
				m_draftView.Focus();
				m_draftViewEditingHelper.GoToLastBook();
			}
		}
		#endregion

		#region Goto Section methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the first Scripture section in the current book in the corresponding
		/// draft view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void GoToFirstSection()
		{
			CheckDisposed();

			if (m_draftView != null)
			{
				m_draftView.Focus();
				m_draftViewEditingHelper.GoToFirstSection();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the previous Scripture section in the corresponding draft view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void GoToPrevSection()
		{
			CheckDisposed();

			if (m_draftView != null)
			{
				m_draftView.Focus();
				m_draftViewEditingHelper.GoToPrevSection();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the next Scripture section in the corresponding draft view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void GoToNextSection()
		{
			CheckDisposed();

			if (m_draftView != null)
			{
				m_draftView.Focus();
				m_draftViewEditingHelper.GoToNextSection();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the last Scripture section in the current book in the corresponding
		/// draft view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void GoToLastSection()
		{
			CheckDisposed();

			if (m_draftView != null)
			{
				m_draftView.Focus();
				m_draftViewEditingHelper.GoToLastSection();
			}
		}
		#endregion

		#region Goto footnote methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Goto the next footnote in the footnote view
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override IScrFootnote GoToNextFootnote()
		{
			CheckDisposed();

			// In case no footnote is selected - can happen when footnote pane is first opened.
			if (CurrentSelection == null)
				return null;

			// Get the information needed from the current selection
			int iBook = CurrentSelection.GetLevelInfoForTag(BookFilter.Tag).ihvo;
			int iFootnote = CurrentSelection.GetLevelInfoForTag(ScrBookTags.kflidFootnotes).ihvo;
			IScrBook book = BookFilter.GetBook(iBook);

			// Get the next footnote if it exists
			if (++iFootnote >= book.FootnotesOS.Count)
				return null;

			ScrollToFootnote(iBook, iFootnote, 0);
			return (IScrFootnote)book.FootnotesOS[iFootnote];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Go to the previous footnote in the footnote view
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override IScrFootnote GoToPreviousFootnote()
		{
			CheckDisposed();

			// In case no footnote is selected - can happen when footnote pane is first opened.
			if (CurrentSelection == null)
				return null;

			// Get the information needed from the current selection
			int iBook = CurrentSelection.GetLevelInfoForTag(BookFilter.Tag).ihvo;
			int iFootnote = CurrentSelection.GetLevelInfoForTag(ScrBookTags.kflidFootnotes).ihvo;
			IScrBook book = BookFilter.GetBook(iBook);

			// Get the previous footnote if it exists
			if (--iFootnote < 0)
				return null;

			ScrollToFootnote(iBook, iFootnote, 0);
			return (IScrFootnote)book.FootnotesOS[iFootnote];
		}
		#endregion

		#region Misc public methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the name of the current book in the corresponding draft view.
		/// </summary>
		/// <param name="selLimitType">Specify Top or Bottom</param>
		/// -----------------------------------------------------------------------------------
		public override string CurrentBook(SelectionHelper.SelLimitType selLimitType)
		{
			CheckDisposed();

			if (m_draftView != null)
				return m_draftViewEditingHelper.CurrentBook(selLimitType);
			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll to a footnote
		/// </summary>
		/// <param name="iBook">Index of the book's hvo</param>
		/// <param name="iFootnote">Index of the footnote's hvo</param>
		/// <param name="ich">The offset of the character position in the footnote's (first)
		/// paragraph to use as the insertion point.</param>
		/// ------------------------------------------------------------------------------------
		public void ScrollToFootnote(int iBook, int iFootnote, int ich)
		{
			CheckDisposed();

			if (Control == null || !(Control is SimpleRootSite))
				return;

			int paraLevel = 0;
			int footnoteLevel = 1;
			int bookLevel = 2;

			// create selection pointing to this footnote
			SelectionHelper selHelper = new SelectionHelper();
			selHelper.AssocPrev = false;

			// Set up selection for a back translation
			if (ContentType == StVc.ContentTypes.kctSimpleBT)
			{
				paraLevel++;
				footnoteLevel++;
				bookLevel++;
				selHelper.NumberOfLevels = 4;
				selHelper.LevelInfo[0].tag = -1;
				selHelper.LevelInfo[0].ihvo = 0;
				selHelper.LevelInfo[0].cpropPrevious = 2;
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor,
					CmTranslationTags.kflidTranslation);
			}
			else if (ContentType == StVc.ContentTypes.kctSegmentBT)
			{
				// In all segment BT views, under the paragraph there is a segment, and under that
				// an object which is the free translation itself.
				paraLevel++;
				footnoteLevel++;
				bookLevel++;
				selHelper.NumberOfLevels = 4;
				selHelper.LevelInfo[0].ihvo = 0; // Segment index
				selHelper.LevelInfo[0].tag = StTxtParaTags.kflidSegments;
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor,
					SegmentTags.kflidFreeTranslation);
			}
			else // Selection is in vernacular
			{
				selHelper.NumberOfLevels = 3;
				selHelper.TextPropId = StTxtParaTags.kflidContents;
			}

			selHelper.LevelInfo[paraLevel].tag = StTextTags.kflidParagraphs;
			selHelper.LevelInfo[paraLevel].ihvo = 0;
			selHelper.LevelInfo[footnoteLevel].tag = ScrBookTags.kflidFootnotes;
			selHelper.LevelInfo[footnoteLevel].ihvo = iFootnote;
			selHelper.LevelInfo[bookLevel].tag = BookFilter.Tag;
			selHelper.LevelInfo[bookLevel].ihvo = iBook;
			selHelper.IchAnchor = ich;
			selHelper.IchEnd = ich;

			if (DeferSelectionUntilEndOfUOW)
			{
				// We are within a unit of work, so setting the selection will not work now.
				// we request that a selection be made after the unit of work.
				Debug.Assert(!selHelper.IsRange,
					"Currently, a selection made during a unit of work can only be an insertion point.");
				selHelper.SetIPAfterUOW(EditedRootBox.Site);
				return;
			}

			IVwSelection vwsel = selHelper.SetSelection((SimpleRootSite)Control, true, true, VwScrollSelOpts.kssoTop);
			if (vwsel == null)
			{
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, SimpleRootSite.kTagUserPrompt);
				selHelper.SetTextPropId(SelectionHelper.SelLimitType.End, SimpleRootSite.kTagUserPrompt);
				selHelper.SetSelection((SimpleRootSite)Control, true, true);
			}
		}
		#endregion

		#region Book Deletion
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the selected book in the corresponding Draft View.
		/// </summary>
		/// <param name="iBook"></param>
		/// ------------------------------------------------------------------------------------
		public override void RemoveBook(int iBook)
		{
			CheckDisposed();

			if (m_draftView != null)
			{
				m_draftView.Focus();
				m_draftViewEditingHelper.RemoveBook(iBook);
			}
		}
		#endregion

		#region Insert Chapter Number
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do not allow insertion of chapter numbers in a footnote.
		/// </summary>
		/// <returns>false</returns>
		/// ------------------------------------------------------------------------------------
		public override bool CanInsertNumberInElement
		{
			get
			{
				CheckDisposed();

				return false;
			}
		}
		#endregion

		#region Insert Footnote
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Can't insert a footnote in a footnote, silly!
		/// </summary>
		/// <param name="helper">Current selection information</param>
		/// <param name="styleName">style name for the footnote</param>
		/// <param name="insertPos"></param>
		/// <returns>never</returns>
		/// ------------------------------------------------------------------------------------
		public override IStFootnote InsertFootnote(SelectionHelper helper, string styleName, out int insertPos)
		{
			CheckDisposed();

			throw new Exception("Can't insert a footnote in a footnote.");
		}
		#endregion

		#region Back Translation methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Can't do this in a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void GenerateTranslationCVNumsForSection()
		{
			CheckDisposed();

			throw new Exception("Can't generate Chapter & Verse numbers for footnotes.");
		}
		#endregion

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Don't allow user to press enter.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="modifiers"></param>
		/// ------------------------------------------------------------------------------------------
		public override void OnKeyPress(KeyPressEventArgs e, Keys modifiers)
		{
			CheckDisposed();

			if (e.KeyChar != (char)13)
				base.OnKeyPress (e, modifiers);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the flid and hvo corresponding to the current Scripture element (e.g.,
		/// section heading, section contents, or title) selected.
		/// </summary>
		/// <param name="tag">The flid of the selected owning element</param>
		/// <param name="hvoSel">The hvo of the selected owning element (hvo of either section
		/// or book)</param>
		/// <returns>
		/// True, if a known element is found at this current selection
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool GetSelectedScrElement(out int tag, out int hvoSel)
		{
			CheckDisposed();

			hvoSel = 0;
			tag = 0;
			if (CurrentSelection == null)
				return false;

			try
			{
				SelectionHelper helper = CurrentSelection;
				SelLevInfo selLevInfo;
				if (!helper.GetLevelInfoForTag(ScrBookTags.kflidFootnotes,
					 SelectionHelper.SelLimitType.Top, out selLevInfo))
				{
					return base.GetSelectedScrElement(out tag, out hvoSel);
				}
				tag = selLevInfo.tag;
				hvoSel =
					m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(selLevInfo.hvo).Owner.Hvo;
			}
			catch
			{
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a string that describes the Scripture passage based on the selection.
		/// </summary>
		/// <param name="tag">The flid of the selected element</param>
		/// <param name="hvoSel">The hvo of the selected element, either a ScrSection (usually)
		/// or ScrBook (if in a title)</param>
		/// <param name="fSimpleFormat">Gets a simple, standardized reference (uses SIL 3-letter
		/// codes and no verse bridges)</param>
		/// <returns>
		/// String that describes the Scripture passage or null if the selection
		/// can't be interpreted as a book and/or section reference.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string GetPassageAsString(int tag, int hvoSel, bool fSimpleFormat)
		{
			CheckDisposed();

			if (m_cache == null)
				return null;

			string sEditRef = null; // Title/reference/etc of text being edited in the draft pane
			switch (tag)
			{
				case ScrBookTags.kflidFootnotes:
					IScrBook book =
						m_cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(hvoSel);
					sEditRef = (fSimpleFormat ? (book.BookId + " 0:0") : book.BestUIName);
					break;

				default:
					return base.GetPassageAsString(tag, hvoSel, fSimpleFormat);
			}

			// Add the back translation writing system info to the output string, if needed
			if (IsBackTranslation)
			{
				IWritingSystem ws = m_cache.ServiceLocator.WritingSystemManager.Get(ViewConstructorWS);

				sEditRef = string.Format(
					TeResourceHelper.GetResourceString("kstidCaptionInBackTrans"),
					sEditRef, ws.DisplayLabel);
			}

			return string.IsNullOrEmpty(sEditRef) ? null : sEditRef;
		}

		#endregion
	}
}
