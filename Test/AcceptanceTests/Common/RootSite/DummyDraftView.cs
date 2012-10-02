// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DummyDraftView.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Implementation of a basic view for testing, similar to DraftView
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class DummyDraftView : DummyBasicView
	{
		#region Data members
		private DummyDraftViewVc m_DraftViewVc;
		private int m_hvoScripture;
		#endregion

		#region Constructor, Dispose, InitializeComponent
		/// <summary>
		/// Initializes a new instance of the DraftView class
		/// </summary>
		public DummyDraftView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
				if (m_DraftViewVc != null)
					m_DraftViewVc.Dispose();
			}
			m_DraftViewVc = null;
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			//
			// DummyDraftView
			//
			this.Name = "DummyDraftView";

		}
		#endregion
		#endregion

		#region Event handling methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Activates the view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void ActivateView()
		{
			CheckDisposed();

			PerformLayout();
			Show();
			Focus();
		}

		#endregion

		#region Overrides of RootSite
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a root box and initializes it with appropriate data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_fdoCache == null || DesignMode)
				return;

			BaseMakeRoot();

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);

			// Set up a new view constructor.
			m_DraftViewVc = new DummyDraftViewVc();

			m_DraftViewVc.SetDa(m_fdoCache);

			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;
			m_rootb.SetRootObject(HvoScripture, m_DraftViewVc, (int)ScrFrags.kfrScripture, m_styleSheet);

			m_fRootboxMade = true;
			m_dxdLayoutWidth = -50000; // Don't try to draw until we get OnSize and do layout.

			// Added this to keep from Asserting if the user tries to scroll the draft window
			// before clicking into it to place the insertion point.
			try
			{
				m_rootb.MakeSimpleSel(true, true, false, true);
			}
			catch(COMException)
			{
				// We ignore failures since the text window may be empty, in which case making a
				// selection is impossible.
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Unit tests have been changed to not be visible, acceptance test needs to do
		/// real scrolling.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override Point ScrollPosition
		{
			get
			{
				CheckDisposed();
				return AutoScrollPosition;
			}
			set
			{
				CheckDisposed();
				AutoScrollPosition = value;
			}
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the HVO for the scripture object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int HvoScripture
		{
			get
			{
				CheckDisposed();

				if (m_hvoScripture == 0 && m_fdoCache != null)
					m_hvoScripture = m_fdoCache.LangProject.TranslatedScriptureOAHvo;
				return m_hvoScripture;
			}
			set
			{
				CheckDisposed();

				m_hvoScripture = value;
			}
		}
		#endregion

		#region Verse-related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to set the selection immediately following the last character of the closest
		/// verse number to the requested verse. If no section exists within one chapter of the
		/// requested verse, the selection will not be changed.
		/// </summary>
		/// <param name="targetRef">Reference to seek</param>
		/// <returns>true if the selected is changed (to the requested verse or one nearby);
		/// false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool GotoVerse(ScrReference targetRef)
		{
			CheckDisposed();

			Scripture scr = new Scripture(m_fdoCache, HvoScripture);

			int iBook = 0;
			foreach (ScrBook book in scr.ScriptureBooksOS)
			{
				if (book.BookIdRA.OwnOrd == targetRef.Book)
				{
					// found the book
					ScrSection prevSection;
					int iSection = 0;
					foreach (ScrSection section in book.SectionsOS)
					{
						if (section.VerseRefStart <= targetRef)
						{
							if (section.VerseRefEnd >= targetRef)
							{
								int ihvoPara; // index of paragraph containing the verse
								int ichPosition; //place to put IP.

								FindVerseNumber(section, targetRef, out ihvoPara,
									out ichPosition);

								SetInsertionPoint(iBook, iSection, ihvoPara, ichPosition,
									false);
								return true;
							}
							prevSection = section;
						}
						else
						{
							// try finding a close enough verse ref in previous section
						}
						iSection++;
					}
				}
				iBook++;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a Scripture section, this method returns the index of the paragraph containing
		/// the requested verse and the position of the character immediately following the
		/// verse number in that paragraph. If an exact match isn't found, the closest
		/// approximate place is found.
		/// </summary>
		/// <param name="section">The section whose paragraphs will be searched</param>
		/// <param name="targetRef">The reference being sought</param>
		/// <param name="ihvoPara"></param>
		/// <param name="ichPosition"></param>
		/// <remarks>Currently, this does NOT attempt to find a close match, but some day it
		/// should</remarks>
		/// ------------------------------------------------------------------------------------
		protected void FindVerseNumber(ScrSection section, ScrReference targetRef,
			out int ihvoPara, out int ichPosition)
		{
			ihvoPara = 0;
			ichPosition = 0;

			bool fChapterFound = ((ScrReference)section.VerseRefStart).Chapter == targetRef.Chapter;
			foreach (StTxtPara para in section.ContentOA.ParagraphsOS)
			{
				TsStringAccessor contents = para.Contents;
				TsRunInfo tsi;
				ITsTextProps ttpRun;
				int ich = 0;
				while (ich < contents.Text.Length)
				{
					// Get props of current run.
					ttpRun = contents.UnderlyingTsString.FetchRunInfoAt(ich, out tsi);
					// See if it is our verse number style.
					if (fChapterFound)
					{
						if (StStyle.IsStyle(ttpRun, ScrStyleNames.VerseNumber))
						{
							// The whole run is the verse number. Extract it.
							string sVerseNum = contents.Text.Substring(tsi.ichMin,
								tsi.ichLim - tsi.ichMin);
							int startVerse, endVerse;
							ScrReference.VerseToInt(sVerseNum, out startVerse, out endVerse);
							if (targetRef.Verse >= startVerse && targetRef.Verse <= endVerse)
							{
								ihvoPara = para.OwnOrd - 1;
								ichPosition = tsi.ichLim;
								return;
							}
							// TODO: Currently, this does NOT attempt to detect when we have
							// a close match
						}
					}
						// See if it is our chapter number style.
					else if (StStyle.IsStyle(ttpRun, ScrStyleNames.ChapterNumber))
					{
						// Assume the whole run is the chapter number. Extract it.
						string sChapterNum = contents.Text.Substring(tsi.ichMin,
							tsi.ichLim - tsi.ichMin);
						int nChapter = ScrReference.ChapterToInt(sChapterNum);
						fChapterFound = (nChapter == targetRef.Chapter);
					}
					ich = tsi.ichLim;
				}
			}
		}

		#endregion

		#region Set Insertion Point methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the insertion point in this draftview to the specified location.
		/// </summary>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="para">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="character">The 0-based index of the character before which the
		/// insertion point is to be placed</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SetInsertionPoint(int book, int section, int para,
			int character)
		{
			CheckDisposed();

			IVwSelection vwSel;
			return SelectRangeOfChars(book, section, para, character, character, true, true,
				out vwSel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the insertion point in this draftview to the specified location.
		/// </summary>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="para">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="character">The 0-based index of the character before which the
		/// insertion point is to be placed</param>
		/// <param name="fAssocPrev">True if the properties of the text entered at the new
		/// insertion point should be associated with the properties of the text before the new
		/// insertion point. False if text entered at the new insertion point should be
		/// associated with the text following the new insertion point.</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SetInsertionPoint(int book, int section, int para,
			int character, bool fAssocPrev)
		{
			CheckDisposed();

			IVwSelection vwSel;
			return SelectRangeOfChars(book, section, para, character, character, true, true,
				fAssocPrev, out vwSel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Navigate to the beginning of any Scripture element: Title, Section Head, or Section
		/// Content.
		/// </summary>
		/// <param name="tag">Indicates whether selection should be made in the title, section
		/// Heading or section Content</param>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point. Ignored if tag is <see cref="ScrBook.ScrBookTags.kflidTitle"/>
		/// </param>
		/// ------------------------------------------------------------------------------------
		public void SetInsertionPoint(int tag, int book, int section)
		{
			CheckDisposed();

			SelectionHelper selHelper = new SelectionHelper();
			int cLev;
			if (tag == (int)ScrBook.ScrBookTags.kflidTitle)
			{
				cLev = 3;
				selHelper.NumberOfLevels = cLev;
				selHelper.LevelInfo[cLev - 2].tag = tag;
			}
			else
			{
				cLev = 4;
				selHelper.NumberOfLevels = cLev;
				selHelper.LevelInfo[cLev - 2].tag = (int)ScrBook.ScrBookTags.kflidSections;
				selHelper.LevelInfo[cLev - 2].ihvo = section;
				selHelper.LevelInfo[cLev - 3].tag = tag;
			}
			selHelper.LevelInfo[cLev - 1].tag = (int)Scripture.ScriptureTags.kflidScriptureBooks;
			selHelper.LevelInfo[cLev - 1].ihvo = book;

			// Now that all the preparation to set the IP is done, set it.
			IVwSelection vwsel = selHelper.SetSelection(this, true, true);

			Application.DoEvents();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set an insertion point or character-range selection in this DraftView which is
		/// "installed" and scrolled into view.
		/// </summary>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="para">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SelectRangeOfChars(int book, int section, int para,
			int startCharacter, int endCharacter)
		{
			CheckDisposed();

			IVwSelection vwSel;
			return SelectRangeOfChars(book, section, para, startCharacter, endCharacter, true,
				true, out vwSel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a selection for this DraftView which is optionally "installed" and/or
		/// scrolled into view and which is associated with the previous character in the view.
		/// </summary>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="para">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <param name="fInstall"></param>
		/// <param name="fMakeVisible"></param>
		/// <param name="vwsel"></param>
		/// <returns>The selection helper object used to make move the IP.</returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SelectRangeOfChars(int book, int section, int para,
			int startCharacter, int endCharacter, bool fInstall, bool fMakeVisible,
			out IVwSelection vwsel)
		{
			CheckDisposed();

			SelectionHelper selHelper = new	SelectionHelper();

			SelectRangeOfChars(ref selHelper, book, section, para, startCharacter, endCharacter,
				fInstall, fMakeVisible, out vwsel);

			return selHelper;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a selection for this DraftView which is optionally "installed" and/or
		/// scrolled into view and which may be associated with either the previous or next
		/// character.
		/// </summary>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-based index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="para">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <param name="fInstall"></param>
		/// <param name="fMakeVisible"></param>
		/// <param name="fAssocPrev">True if the properties of the text entered at the new
		/// insertion point should be associated with the properties of the text before the new
		/// insertion point. False if text entered at the new insertion point should be
		/// associated with the text following the new insertion point.</param>
		/// <param name="vwsel"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SelectRangeOfChars(int book, int section, int para,
			int startCharacter, int endCharacter, bool fInstall, bool fMakeVisible,
			bool fAssocPrev, out IVwSelection vwsel)
		{
			CheckDisposed();

			SelectionHelper selHelper = new SelectionHelper();

			selHelper.AssocPrev = fAssocPrev;

			SelectRangeOfChars(ref selHelper, book, section, para, startCharacter, endCharacter,
				fInstall, fMakeVisible, out vwsel);

			return selHelper;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the actual workhorse for all the above methods that allows a selection to
		/// be created.
		/// </summary>
		/// <param name="selHelper">The selection helper that will be used tom make the
		/// selection</param>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-base index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="para">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <param name="fInstall"></param>
		/// <param name="fMakeVisible"></param>
		/// <param name="vwsel"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private void SelectRangeOfChars(ref SelectionHelper selHelper, int book, int section,
			int para, int startCharacter, int endCharacter, bool fInstall, bool fMakeVisible,
			out IVwSelection vwsel)
		{
			SelectRangeOfChars(ref selHelper, book, section,
				ScrSection.ScrSectionTags.kflidContent, para, startCharacter, endCharacter,
				fInstall, fMakeVisible, out vwsel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the actual workhorse for all the above methods that allows a selection to
		/// be created.
		/// </summary>
		/// <param name="selHelper">The selection helper that will be used tom make the
		/// selection</param>
		/// <param name="book">The 0-based index of the Scripture book in which to put the
		/// insertion point</param>
		/// <param name="section">The 0-base index of the Scripture section in which to put the
		/// insertion point</param>
		/// <param name="sectionTag">Indicates whether selection should be made in the section
		/// Heading or Contents</param>
		/// <param name="para">The 0-based index of the paragraph in which to put the insertion
		/// point</param>
		/// <param name="startCharacter">The 0-based index of the character at which the
		/// selection begins (or before which the insertion point is to be placed if
		/// startCharacter == endCharacter)</param>
		/// <param name="endCharacter">The character location to end the selection</param>
		/// <param name="fInstall"></param>
		/// <param name="fMakeVisible"></param>
		/// <param name="vwsel"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private void SelectRangeOfChars(ref SelectionHelper selHelper, int book, int section,
			ScrSection.ScrSectionTags sectionTag, int para, int startCharacter,
			int endCharacter, bool fInstall, bool fMakeVisible,	out IVwSelection vwsel)
		{
			selHelper.NumberOfLevels = 4;
			selHelper.LevelInfo[3].tag = (int)Scripture.ScriptureTags.kflidScriptureBooks;
			selHelper.LevelInfo[3].ihvo = book;
			selHelper.LevelInfo[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			selHelper.LevelInfo[2].ihvo = section;
			selHelper.LevelInfo[1].tag = (int)sectionTag;
			selHelper.LevelInfo[1].ihvo = 0;	// Atomic property, probably ignored
			// selHelper.LevelInfo[0].tag is set automatically by SelectionHelper class
			selHelper.LevelInfo[0].ihvo = para;

			// Prepare to move the IP to the specified character in the paragraph.
			selHelper.IchAnchor = startCharacter;
			selHelper.IchEnd = endCharacter;

			// Now that all the preparation to set the IP is done, set it.
			vwsel = selHelper.SetSelection(this, fInstall, fMakeVisible);

			Application.DoEvents();
		}
		#endregion
	}
}
