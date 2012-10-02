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
// File: TeEditingHelperTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

using NMock;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;
using System.Text;
using System.IO;

namespace SIL.FieldWorks.TE
{
	#region DummyTeEditingHelper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyTeEditingHelper : TeEditingHelper
	{
		private int m_styleType;
		private string m_styleName;

		internal int m_testSelection_tag;
		internal int m_testSelection_book;
		internal int m_testSelection_section;
		internal int m_testSelection_para;
		internal int m_testSelection_character;
		internal bool m_testSelection_fAssocPrev;

		/// <summary>Range of currently selected references</summary>
		protected ScrReference m_currentReference;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyTeEditingHelper"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="rootsite">The rootsite.</param>
		/// ------------------------------------------------------------------------------------
		public DummyTeEditingHelper(FdoCache cache, SimpleRootSite rootsite) :
			base(rootsite, cache, 0, TeViewType.DraftView | TeViewType.Scripture)
		{
			InTestMode = true;
		}

		#region IDisposable override

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
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Setup selections
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a simulated range selection
		/// </summary>
		/// <param name="hvoPara1"></param>
		/// <param name="hvoPara2"></param>
		/// <param name="ichAnchor"></param>
		/// <param name="ichEnd"></param>
		/// ------------------------------------------------------------------------------------
		public void SetupSelectionForParas(int hvoPara1, int hvoPara2, int ichAnchor, int ichEnd)
		{
			CheckDisposed();

			DynamicMock fakeSelHelper = new DynamicMock(typeof(SelectionHelper));
			fakeSelHelper.SetupResult("NumberOfLevels", 1);
			SelLevInfo[] topInfo = new SelLevInfo[1];
			topInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			topInfo[0].hvo = hvoPara1;
			SelLevInfo[] bottomInfo = new SelLevInfo[1];
			bottomInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			bottomInfo[0].hvo = hvoPara2;
			fakeSelHelper.SetupResult("LevelInfo", topInfo);
			fakeSelHelper.SetupResult("IchAnchor", ichAnchor);
			fakeSelHelper.SetupResult("IchEnd", ichEnd);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", topInfo,
				SelectionHelper.SelLimitType.Top);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", topInfo,
				SelectionHelper.SelLimitType.Anchor);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", bottomInfo,
				SelectionHelper.SelLimitType.Bottom);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", bottomInfo,
				SelectionHelper.SelLimitType.End);
			fakeSelHelper.SetupResultForParams("GetIch", ichAnchor,
				SelectionHelper.SelLimitType.Top);
			fakeSelHelper.SetupResultForParams("GetIch", ichEnd,
				SelectionHelper.SelLimitType.Bottom);
			m_viewSelection = (SelectionHelper)fakeSelHelper.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setups the selection in para.
		/// </summary>
		/// <param name="para">The para.</param>
		/// <param name="iPara">The i para.</param>
		/// <param name="section">The section.</param>
		/// <param name="iSec">The i sec.</param>
		/// <param name="book">The book.</param>
		/// <param name="iBook">The i book.</param>
		/// <param name="ich">The ich.</param>
		/// <param name="setupForHeading">if set to <c>true</c> [setup for heading].</param>
		/// ------------------------------------------------------------------------------------
		public void SetupSelectionInPara(IStTxtPara para, int iPara, IScrSection section,
			int iSec, IScrBook book, int iBook, int ich, bool setupForHeading)
		{
			CheckDisposed();

			SetupRangeSelection(book, iBook, section, iSec, para, iPara, ich, setupForHeading,
				book, iBook, section, iSec, para, iPara, ich, setupForHeading);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up a range selection
		/// </summary>
		/// <param name="bookStart">The starting book.</param>
		/// <param name="iBookStart">The index of the starting book.</param>
		/// <param name="sectionStart">The starting section.</param>
		/// <param name="iSecStart">The index of the starting section.</param>
		/// <param name="paraStart">The starting paragraph.</param>
		/// <param name="iParaStart">The index of the starting paragraph.</param>
		/// <param name="ichStart">The starting character offset.</param>
		/// <param name="setupForHeadingStart">if set to <c>true</c> setup selection to start in
		/// section heading; otherwise, set up selection in contents.</param>
		/// <param name="bookEnd">The ending book.</param>
		/// <param name="iBookEnd">The index of the ending book.</param>
		/// <param name="sectionEnd">The ending section.</param>
		/// <param name="iSecEnd">The index of the ending section.</param>
		/// <param name="paraEnd">The ending paragraph.</param>
		/// <param name="iParaEnd">The index of the para end.</param>
		/// <param name="ichEnd">The ending character offset.</param>
		/// <param name="setupForHeadingEnd">if set to <c>true</c> setup selection to end in
		/// section heading; otherwise, set up selection in contents.</param>
		/// ------------------------------------------------------------------------------------
		public void SetupRangeSelection(IScrBook bookStart, int iBookStart, IScrSection sectionStart,
			int iSecStart, IStTxtPara paraStart, int iParaStart, int ichStart,
			bool setupForHeadingStart, IScrBook bookEnd, int iBookEnd, IScrSection sectionEnd,
			int iSecEnd, IStTxtPara paraEnd, int iParaEnd, int ichEnd, bool setupForHeadingEnd)
		{
			CheckDisposed();

			SetupRangeSelection(-1, -1, bookStart, iBookStart, sectionStart, iSecStart,
				paraStart, iParaStart, ichStart, setupForHeadingStart, bookEnd, iBookEnd,
				sectionEnd, iSecEnd, paraEnd, iParaEnd, ichEnd, setupForHeadingEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up a range selection in the specified back translation.
		/// </summary>
		/// <param name="wsStart">The writing system of the BT where the selection is to start,
		/// or -1 to start in the vernacular.</param>
		/// <param name="wsEnd">The writing system of the BT where the selection is to end,
		/// or -1 to end in the vernacular.</param>
		/// <param name="bookStart">The starting book.</param>
		/// <param name="iBookStart">The index of the starting book.</param>
		/// <param name="sectionStart">The starting section.</param>
		/// <param name="iSecStart">The index of the starting section.</param>
		/// <param name="paraStart">The starting paragraph.</param>
		/// <param name="iParaStart">The index of the starting paragraph.</param>
		/// <param name="ichStart">The starting character offset.</param>
		/// <param name="setupForHeadingStart">if set to <c>true</c> setup selection to start in
		/// section heading; otherwise, set up selection in contents.</param>
		/// <param name="bookEnd">The ending book.</param>
		/// <param name="iBookEnd">The index of the ending book.</param>
		/// <param name="sectionEnd">The ending section.</param>
		/// <param name="iSecEnd">The index of the ending section.</param>
		/// <param name="paraEnd">The ending paragraph.</param>
		/// <param name="iParaEnd">The index of the para end.</param>
		/// <param name="ichEnd">The ending character offset.</param>
		/// <param name="setupForHeadingEnd">if set to <c>true</c> setup selection to end in
		/// section heading; otherwise, set up selection in contents.</param>
		/// ------------------------------------------------------------------------------------
		public void SetupRangeSelection(int wsStart, int wsEnd, IScrBook bookStart, int iBookStart,
			IScrSection sectionStart, int iSecStart, IStTxtPara paraStart, int iParaStart, int ichStart,
			bool setupForHeadingStart, IScrBook bookEnd, int iBookEnd, IScrSection sectionEnd,
			int iSecEnd, IStTxtPara paraEnd, int iParaEnd, int ichEnd, bool setupForHeadingEnd)
		{
			CheckDisposed();

			DynamicMock fakeSelHelper = new DynamicMock(typeof(SelectionHelper));
			int cLevelsTop = wsStart > 0 ? 5 : 4;
			// Setup the anchor
			SelLevInfo[] topInfo = new SelLevInfo[cLevelsTop];
			int iLev = 0;
			if (wsStart > 0)
			{
				topInfo[iLev].tag = -1;
				topInfo[iLev].ihvo = 0;
				topInfo[iLev].hvo = paraStart.TranslationsOC.HvoArray[0];
				iLev++;
			}
			topInfo[iLev].tag = (int)StText.StTextTags.kflidParagraphs;
			topInfo[iLev].ihvo = iParaStart;
			topInfo[iLev].hvo = paraStart.Hvo;
			topInfo[++iLev].tag = setupForHeadingStart ? (int)ScrSection.ScrSectionTags.kflidHeading :
				(int)ScrSection.ScrSectionTags.kflidContent;
			topInfo[iLev].ihvo = 0;
			topInfo[iLev].hvo = setupForHeadingStart ? sectionStart.HeadingOA.Hvo :
				sectionStart.ContentOA.Hvo;
			topInfo[++iLev].tag = (int)ScrBook.ScrBookTags.kflidSections;
			topInfo[iLev].ihvo = iSecStart;
			topInfo[iLev].hvo = sectionStart.Hvo;
			topInfo[++iLev].tag = BookFilter.Tag;
			topInfo[iLev].ihvo = iBookStart;
			topInfo[iLev].hvo = bookStart.Hvo;

			// Setup the end
			int cLevelsBottom = wsEnd > 0 ? 5 : 4;
			SelLevInfo[] bottomInfo = new SelLevInfo[cLevelsBottom];
			iLev = 0;
			if (wsEnd > 0)
			{
				bottomInfo[iLev].tag = -1;
				bottomInfo[iLev].ihvo = 0;
				bottomInfo[iLev].hvo = paraEnd.TranslationsOC.HvoArray[0];
				iLev++;
			}
			bottomInfo[iLev].tag = (int)StText.StTextTags.kflidParagraphs;
			bottomInfo[iLev].ihvo = iParaEnd;
			bottomInfo[iLev].hvo = paraEnd.Hvo;
			bottomInfo[++iLev].tag = setupForHeadingEnd ? (int)ScrSection.ScrSectionTags.kflidHeading :
				(int)ScrSection.ScrSectionTags.kflidContent;
			bottomInfo[iLev].ihvo = 0;
			bottomInfo[iLev].hvo = setupForHeadingEnd ? sectionEnd.HeadingOA.Hvo :
				sectionEnd.ContentOA.Hvo;
			bottomInfo[++iLev].tag = (int)ScrBook.ScrBookTags.kflidSections;
			bottomInfo[iLev].ihvo = iSecEnd;
			bottomInfo[iLev].hvo = sectionEnd.Hvo;
			bottomInfo[++iLev].tag = BookFilter.Tag;
			bottomInfo[iLev].ihvo = iBookEnd;
			bottomInfo[iLev].hvo = bookEnd.Hvo;

			fakeSelHelper.SetupResult("NumberOfLevels", cLevelsTop);
			fakeSelHelper.SetupResultForParams("GetNumberOfLevels", cLevelsTop,
				SelectionHelper.SelLimitType.Top);
			fakeSelHelper.SetupResultForParams("GetNumberOfLevels", cLevelsTop,
				SelectionHelper.SelLimitType.Anchor);
			fakeSelHelper.SetupResultForParams("GetNumberOfLevels", cLevelsBottom,
				SelectionHelper.SelLimitType.Bottom);
			fakeSelHelper.SetupResultForParams("GetNumberOfLevels", cLevelsBottom,
				SelectionHelper.SelLimitType.End);
			fakeSelHelper.SetupResult("LevelInfo", topInfo);
			fakeSelHelper.SetupResult("IchAnchor", ichStart);
			fakeSelHelper.SetupResult("IchEnd", ichEnd);
			fakeSelHelper.SetupResult("Ws", wsStart);
			fakeSelHelper.SetupResult("IsValid", true);
			fakeSelHelper.SetupResult("AssocPrev", false);
			fakeSelHelper.Ignore("get_IsRange");
			fakeSelHelper.SetupResultForParams("GetLevelInfo", topInfo,
				SelectionHelper.SelLimitType.Top);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", topInfo,
				SelectionHelper.SelLimitType.Anchor);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", bottomInfo,
				SelectionHelper.SelLimitType.Bottom);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", bottomInfo,
				SelectionHelper.SelLimitType.End);
			fakeSelHelper.SetupResultForParams("GetTextPropId", wsStart > 0 ?
				(int)CmTranslation.CmTranslationTags.kflidTranslation :
				(int)StTxtPara.StTxtParaTags.kflidContents,
				SelectionHelper.SelLimitType.Top);
			fakeSelHelper.SetupResultForParams("GetTextPropId", wsStart > 0 ?
				(int)CmTranslation.CmTranslationTags.kflidTranslation :
				(int)StTxtPara.StTxtParaTags.kflidContents,
				SelectionHelper.SelLimitType.Anchor);
			fakeSelHelper.SetupResultForParams("GetTextPropId", wsEnd > 0 ?
				(int)CmTranslation.CmTranslationTags.kflidTranslation :
				(int)StTxtPara.StTxtParaTags.kflidContents,
				SelectionHelper.SelLimitType.Bottom);
			fakeSelHelper.SetupResultForParams("GetTextPropId", wsEnd > 0 ?
				(int)CmTranslation.CmTranslationTags.kflidTranslation :
				(int)StTxtPara.StTxtParaTags.kflidContents,
				SelectionHelper.SelLimitType.End);
			fakeSelHelper.SetupResultForParams("GetIch", ichStart,
				SelectionHelper.SelLimitType.Top);
			fakeSelHelper.SetupResultForParams("GetIch", ichStart,
				SelectionHelper.SelLimitType.Anchor);
			fakeSelHelper.SetupResultForParams("GetIch", ichEnd,
				SelectionHelper.SelLimitType.Bottom);
			fakeSelHelper.SetupResultForParams("GetIch", ichEnd,
				SelectionHelper.SelLimitType.End);
			fakeSelHelper.SetupResultForParams("GetWritingSystem", wsStart == -1 ? m_cache.DefaultVernWs : wsStart,
				SelectionHelper.SelLimitType.Top);
			fakeSelHelper.SetupResultForParams("GetWritingSystem", wsStart == -1 ? m_cache.DefaultVernWs : wsStart,
				SelectionHelper.SelLimitType.Anchor);
			fakeSelHelper.SetupResultForParams("GetWritingSystem", wsEnd == -1 ? m_cache.DefaultVernWs : wsEnd,
				SelectionHelper.SelLimitType.Bottom);
			fakeSelHelper.SetupResultForParams("GetWritingSystem", wsEnd == -1 ? m_cache.DefaultVernWs : wsEnd,
				SelectionHelper.SelLimitType.End);
			m_viewSelection = (SelectionHelper)fakeSelHelper.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setups the selection in title para.
		/// </summary>
		/// <param name="para">The para.</param>
		/// <param name="iPara">The i para.</param>
		/// <param name="book">The book.</param>
		/// <param name="iBook">The i book.</param>
		/// <param name="ich">The ich.</param>
		/// ------------------------------------------------------------------------------------
		public void SetupSelectionInTitlePara(IStTxtPara para, int iPara, IScrBook book, int iBook,
			int ich)
		{
			CheckDisposed();

			DynamicMock fakeSelHelper = new DynamicMock(typeof(SelectionHelper));
			fakeSelHelper.SetupResult("NumberOfLevels", 3);
			// Setup the anchor
			SelLevInfo[] topInfo = new SelLevInfo[3];
			topInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			topInfo[0].ihvo = iPara;
			topInfo[0].hvo = para.Hvo;
			topInfo[1].tag = (int)ScrBook.ScrBookTags.kflidTitle;
			topInfo[1].ihvo = 0;
			topInfo[1].hvo = book.TitleOAHvo;
			topInfo[2].tag = BookFilter.Tag;
			topInfo[2].ihvo = iBook;
			topInfo[2].hvo = book.Hvo;

			// Setup the end
			SelLevInfo[] bottomInfo = new SelLevInfo[3];
			for(int i = 0; i < 3; i++)
				bottomInfo[i] = topInfo[i];

			fakeSelHelper.SetupResult("LevelInfo", topInfo);
			fakeSelHelper.SetupResult("IchAnchor", ich);
			fakeSelHelper.SetupResult("IchEnd", ich);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", topInfo,
				SelectionHelper.SelLimitType.Top);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", topInfo,
				SelectionHelper.SelLimitType.Anchor);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", bottomInfo,
				SelectionHelper.SelLimitType.Bottom);
			fakeSelHelper.SetupResultForParams("GetLevelInfo", bottomInfo,
				SelectionHelper.SelLimitType.End);
			fakeSelHelper.SetupResultForParams("GetIch", ich,
				SelectionHelper.SelLimitType.Top);
			fakeSelHelper.SetupResultForParams("GetIch", ich,
				SelectionHelper.SelLimitType.Bottom);
			m_viewSelection = (SelectionHelper)fakeSelHelper.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a simulated selection anywhere in the given section (Does NOT set para and
		/// character info)
		/// </summary>
		/// <param name="hvoSection">The selection</param>
		/// ------------------------------------------------------------------------------------
		public void SetupSelectionForSection(int hvoSection)
		{
			CheckDisposed();

			DynamicMock fakeSelHelper = new DynamicMock(typeof(SelectionHelper));
			fakeSelHelper.SetupResult("NumberOfLevels", 1);
			SelLevInfo[] topInfo = new SelLevInfo[1];
			topInfo[0].tag = (int)ScrBook.ScrBookTags.kflidSections;
			topInfo[0].hvo = hvoSection;
			fakeSelHelper.SetupResult("LevelInfo", topInfo);
			fakeSelHelper.SetupResult("GetLevelInfo", topInfo, typeof(SelectionHelper.SelLimitType));
			m_viewSelection = (SelectionHelper)fakeSelHelper.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a simulated selection anywhere in the given title (Does NOT set para and
		/// character info)
		/// </summary>
		/// <param name="book">Book containing the title</param>
		/// ------------------------------------------------------------------------------------
		internal void SetupSelectionForTitle(IScrBook book)
		{
			CheckDisposed();

			DynamicMock fakeSelHelper = new DynamicMock(typeof(SelectionHelper));
			fakeSelHelper.SetupResult("NumberOfLevels", 2);
			SelLevInfo[] topInfo = new SelLevInfo[2];
			topInfo[0].tag = (int)ScrBook.ScrBookTags.kflidTitle;
			topInfo[0].hvo = book.TitleOAHvo;
			topInfo[1].tag = BookFilter.Tag;
			topInfo[1].hvo = book.Hvo;
			fakeSelHelper.SetupResult("LevelInfo", topInfo);
			fakeSelHelper.SetupResult("GetLevelInfo", topInfo, typeof(SelectionHelper.SelLimitType));
			m_viewSelection = (SelectionHelper)fakeSelHelper.MockInstance;
		}
		#endregion

		#region Overridden properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default vernacular writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override int RootVcDefaultWritingSystem
		{
			get { return m_cache.DefaultVernWs; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a SelectionHelper object set to the current selection in the view
		/// (updated any time the selection changes)
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override SelectionHelper CurrentSelection
		{
			// For testing purposes we return the stored selection all the time.
			get
			{
				CheckDisposed();
				return m_viewSelection;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides simple access to the start ref at the current insertion point.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override ScrReference CurrentStartRef
		{
			get
			{
				return (m_currentReference != null) ? m_currentReference : base.CurrentStartRef;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides simple access to the end ref at the current insertion point.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override ScrReference  CurrentEndRef
		{
			get
			{
				return (m_currentReference != null) ? m_currentReference : base.CurrentEndRef;
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes it possible for tests to set m_currentReference. Note: if this is set in a
		/// way that is incompatible with the dummy selection made in a paragraph or section,
		/// the editing helper could very easily behave in ways it never would in real life.
		/// </summary>
		/// <param name="scrRef"></param>
		/// ------------------------------------------------------------------------------------
		public void SetupCurrentReference(int scrRef)
		{
			m_currentReference = new ScrReference(scrRef, m_scr.Versification);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="helper"></param>
		/// <param name="picHvo"></param>
		/// ------------------------------------------------------------------------------------
		public void CallDeletePicture(SelectionHelper helper, int picHvo)
		{
			CheckDisposed();

			base.DeletePicture(helper, picHvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="vernVerseChapterText"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string CallConvertVerseChapterNumForBT(string vernVerseChapterText)
		{
			CheckDisposed();

			return base.ConvertVerseChapterNumForBT(vernVerseChapterText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool CallHandleComplexDeletion()
		{
			CheckDisposed();

			return HandleComplexDeletion(m_viewSelection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the selection for testing.
		/// </summary>
		/// <value>The selection for testing.</value>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper SelectionForTesting
		{
			set
			{
				CheckDisposed();
				m_viewSelection = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the current selection contains one run with a character style or multiple runs
		/// with the same character style this method returns the character style; otherwise
		/// returns the paragraph style unless multiple paragraphs are selected that have
		/// different paragraph styles.
		/// </summary>
		/// <param name="styleName">Gets the styleName</param>
		/// <returns>
		/// The styleType or -1 if no style type can be found or multiple style types
		/// are in the selection.  Otherwise returns the styletype
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int GetStyleNameFromSelection(out string styleName)
		{
			CheckDisposed();

			if (m_styleName == null)
				return base.GetStyleNameFromSelection(out styleName);

			styleName = m_styleName;
			return m_styleType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the style for testing.
		/// </summary>
		/// <param name="styleType">Type of the style.</param>
		/// <param name="styleName">Name of the style.</param>
		/// ------------------------------------------------------------------------------------
		public void SetStyleForTesting(int styleType, string styleName)
		{
			CheckDisposed();

			m_styleName = styleName;
			m_styleType = styleType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the insertion point in this draftview to the specified location.
		/// </summary>
		/// <param name="tag">Indicates whether selection should be made in the title, section
		/// Heading or section Content</param>
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
		public override SelectionHelper SetInsertionPoint(int tag, int book, int section, int para,
			int character, bool fAssocPrev)
		{
			m_testSelection_tag = tag;
			m_testSelection_book = book;
			m_testSelection_section = section;
			m_testSelection_para = para;
			m_testSelection_character = character;
			m_testSelection_fAssocPrev = fAssocPrev;

			return base.SelectRangeOfChars(book, section, tag, para, character, character,
				true, true, fAssocPrev);
		}
	}
	#endregion

	#region DummySimpleRootSite
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummySimpleRootSite : SimpleRootSite, ITeView
	{
		private LocationTrackerImpl m_locationTracker;
		internal bool m_fScrollSelectionIntoViewWasCalled = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummySimpleRootSite"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public DummySimpleRootSite(FdoCache cache)
		{
			// Add virtual property to cache
			if (FilteredScrBooks.GetFilterInstance(cache, 0) == null)
				new FilteredScrBooks(cache, 0);

			m_locationTracker = new LocationTrackerImpl(cache, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scrolls the selection into view, positioning it as requested
		/// </summary>
		/// <param name="sel">The selection, or <c>null</c> to use the current selection</param>
		/// <param name="scrollOption">The VwScrollSelOpts specification.</param>
		/// ------------------------------------------------------------------------------------
		public override void ScrollSelectionIntoView(IVwSelection sel,
			VwScrollSelOpts scrollOption)
		{
			m_fScrollSelectionIntoViewWasCalled = true;
		}

		#region ITeView Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the location tracker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILocationTracker LocationTracker
		{
			get { return m_locationTracker; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the USFM resource viewer is visible for this view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RegistryBoolSetting UsfmResourceViewerVisible
		{
			get { return new RegistryBoolSetting("test", false); }
		}
		#endregion
	}
	#endregion

	#region TeEditingHelper test base class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for TeEditingHelper tests
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeEditingHelperTestBase : ScrInMemoryFdoTestBase
	{
		/// <summary></summary>
		protected DummyTeEditingHelper m_editingHelper;
		/// <summary></summary>
		protected SimpleRootSite m_RootSite;

		#region setup and teardown methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			Debug.Assert(m_RootSite == null, "m_RootSite is not null.");
			Debug.Assert(m_editingHelper == null, "m_editingHelper is not null.");
			m_RootSite = new DummySimpleRootSite(Cache);
			m_editingHelper = new DummyTeEditingHelper(Cache, m_RootSite);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_RootSite.Dispose();
			m_RootSite = null;
			m_editingHelper.Dispose();
			m_editingHelper = null;

			base.Exit();
		}

		#endregion

		#region IDisposable override

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
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_editingHelper != null)
					m_editingHelper.Dispose();
				if (m_RootSite != null)
					m_RootSite.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_editingHelper = null;
			m_RootSite = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override
	}
	#endregion

	#region TeEditingHelperTests with InMemoryCache
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TeEditingHelperTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeEditingHelperTests : TeEditingHelperTestBase
	{
		#region Verse Chapter Convert Scripts tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests an easy verse number (only numbers) from a Roman to a Roman
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseNumConvert_easyTest()
		{
			CheckDisposed();

			string result = m_editingHelper.CallConvertVerseChapterNumForBT("123");
			Assert.AreEqual("123", result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests a verse number part (with a letter) from a Roman to a Roman
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseNumConvert_letterTest()
		{
			CheckDisposed();

			string result = m_editingHelper.CallConvertVerseChapterNumForBT("12a");
			Assert.AreEqual("12a", result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests a verse number bridge from a Roman to a Roman
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseNumConvert_bridgeTest()
		{
			CheckDisposed();

			string result = m_editingHelper.CallConvertVerseChapterNumForBT("12-14");
			Assert.AreEqual("12-14", result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests a verse number part (with a letter) with a bridge from a Roman to a Roman
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseNumConvert_letterBridgeTest()
		{
			CheckDisposed();

			string result = m_editingHelper.CallConvertVerseChapterNumForBT("12a-13b");
			Assert.AreEqual("12a-13b", result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests an easy verse number (only numbers) from a Script to a Roman
		/// Uses Arabic script numbers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseNumConvert_scriptEasyTest()
		{
			CheckDisposed();

			try
			{
				m_scr.UseScriptDigits = true;
				m_scr.ScriptDigitZero = (int)'\u06f0';

				string result = m_editingHelper.CallConvertVerseChapterNumForBT("\u06f1\u06f2\u06f3");
				Assert.AreEqual("123", result);
			}
			finally
			{
				m_scr.UseScriptDigits = false;
				m_scr.ScriptDigitZero = (int)'0';
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests a verse number part (with a letter) from a Script to a Roman
		/// Uses Arabic script numbers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseNumConvert_scriptLetterTest()
		{
			CheckDisposed();

			try
			{
				m_scr.UseScriptDigits = true;
				m_scr.ScriptDigitZero = (int)'\u06f0';

				string result = m_editingHelper.CallConvertVerseChapterNumForBT("\u06f1\u06f2a");
				Assert.AreEqual("12a", result);
			}
			finally
			{
				m_scr.UseScriptDigits = false;
				m_scr.ScriptDigitZero = (int)'0';
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests a verse number with a bridge from a Script to a Roman
		/// Uses Arabic script numbers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseNumConvert_scriptBridgeTest()
		{
			CheckDisposed();

			try
			{
				m_scr.UseScriptDigits = true;
				m_scr.ScriptDigitZero = (int)'\u06f0';

				string result = m_editingHelper.CallConvertVerseChapterNumForBT("\u06f1\u06f2-\u06f1\u06f4");
				Assert.AreEqual("12-14", result);
			}
			finally
			{
				m_scr.UseScriptDigits = false;
				m_scr.ScriptDigitZero = (int)'0';
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests a verse number part (with a letter) with a bridge from a Script to a Roman
		/// Uses Arabic script numbers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseNumConvert_scriptBridgeLetterTest()
		{
			CheckDisposed();

			try
			{
				m_scr.UseScriptDigits = true;
				m_scr.ScriptDigitZero = (int)'\u06f0';

				string result = m_editingHelper.CallConvertVerseChapterNumForBT("\u06f1\u06f2a-\u06f1\u06f3b");
				Assert.AreEqual("12a-13b", result);
			}
			finally
			{
				m_scr.UseScriptDigits = false;
				m_scr.ScriptDigitZero = (int)'0';
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests an easy verse number from a Roman to a Roman with a script digit set for the
		/// ScriptDigitZero property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseNumConvert_easyNoDiffScriptTest()
		{
			CheckDisposed();

			try
			{
				m_scr.ScriptDigitZero = (int)'\u06f0';
				string result = m_editingHelper.CallConvertVerseChapterNumForBT("123");
				Assert.AreEqual("123", result);
			}
			finally
			{
				m_scr.ScriptDigitZero = (int)'0';
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting annotation information for a selection that starts in a back
		/// translation and ends in a vernacular paragraph that is longer than the corresponding
		/// back translation (TE-4909).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetAnnotationLocationInfo_SelectionCrossesFromBtToVernWithVernLongerThanBt()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			int wsBt = Cache.DefaultAnalWs;
			// Add a section
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "first verse", null);
			CmTranslation transPara1 = (CmTranslation)para1.GetOrCreateBT();
			m_scrInMemoryCache.AddRunToMockedTrans(transPara1, wsBt, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedTrans(transPara1, wsBt, "BT of first verse", null);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "second verse", null);
			CmTranslation transPara2 = (CmTranslation)para2.GetOrCreateBT();
			m_scrInMemoryCache.AddRunToMockedTrans(transPara2, wsBt, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedTrans(transPara2, wsBt, "BT2", null);
			section.AdjustReferences();

			// Set up a range selection from the first paragraph in the back translation to the
			// next paragraph in the vernacular.
			// TE-4909: Crash occurred when the second vernacular paragraph was longer than
			//  its corresponding back translation.
			m_editingHelper.SetupRangeSelection(wsBt, -1, philemon, 0, section, 0, para1, 0, 3, false,
				philemon, 0, section, 0, para2, 1, 9, false);

			CmObject topPara, bottomPara;
			int wsSelector;
			int startOffset, endOffset;
			ITsString tssQuote;
			BCVRef startRef, endRef;
			m_editingHelper.GetAnnotationLocationInfo(out topPara, out bottomPara, out wsSelector,
				out startOffset, out endOffset, out tssQuote, out startRef, out endRef);

			Assert.AreEqual(transPara1.Hvo, topPara.Hvo);
			Assert.AreEqual(para2.Hvo, bottomPara.Hvo);
			// Since we don't have a real selection in a real view, we can't get the references
			// from the back translation and the vernacular so we do not check them in this test.

			// We expect the offsets and the quote to be empty because the selection spans more than
			// two objects.
			Assert.AreEqual(0, startOffset);
			Assert.AreEqual(0, endOffset);
			Assert.IsNull(tssQuote);
			Assert.AreEqual(-1, wsSelector);
		}
		#endregion

		#region Generate Chapter Verse Numbers tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test generating chapter &amp; verse numbers for a translation, from a parent paragraph
		/// with a chapter number only.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GenerateTranslationCVNums_Chapter()
		{
			CheckDisposed();

			// Construct a parent paragraph
			int wsVern = 101;
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr,"1", wsVern, ScrStyleNames.ChapterNumber);
			ITsString tssParentPara = strBldr.GetString();

			// Construct the expected result
			int wsAnal = 105;
			strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr,"1", wsAnal, ScrStyleNames.ChapterNumber);
			ITsString tssExpected = strBldr.GetString();

			// Call the method under test
			ITsString tssTrans = m_editingHelper.GenerateTranslationCVNums(tssParentPara, wsAnal);

			//  Verify the results
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssTrans,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test generating chapter &amp; verse numbers for a translation, from a parent paragraph
		/// with chapter and verse numbers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GenerateTranslationCVNums_ChapterAndVerses()
		{
			CheckDisposed();

			// Construct a parent paragraph
			int wsVern = 101;
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr,"5", wsVern, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr," ", wsVern, null); // unnecessary space
			AddRunToStrBldr(strBldr,"1", wsVern, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr,"Verse one.", wsVern, null);
			AddRunToStrBldr(strBldr,"2", wsVern, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr,"Verse two. ", wsVern, null);
			AddRunToStrBldr(strBldr,"3", wsVern, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr,"Verse three.", wsVern, null);
			ITsString tssParentPara = strBldr.GetString();

			// Construct the expected result
			int wsAnal = 105;
			strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr,"5", wsAnal, ScrStyleNames.ChapterNumber);
			AddRunToStrBldr(strBldr,"1", wsAnal, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr," ", wsAnal, null);
			AddRunToStrBldr(strBldr,"2", wsAnal, ScrStyleNames.VerseNumber);
			AddRunToStrBldr(strBldr," ", wsAnal, null);
			AddRunToStrBldr(strBldr,"3", wsAnal, ScrStyleNames.VerseNumber);
			ITsString tssExpected = strBldr.GetString();

			// Call the method under test
			ITsString tssTrans = m_editingHelper.GenerateTranslationCVNums(tssParentPara, wsAnal);

			//  Verify the results
			string difference;
			bool fEqual = TsStringHelper.TsStringsAreEqual(tssExpected, tssTrans,
				out difference);
			Assert.IsTrue(fEqual, difference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test generating chapter &amp; verse numbers for a translation, from a parent paragraph
		/// with no chapter or verse numbers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GenerateTranslationCVNums_NoCV()
		{
			CheckDisposed();

			// Construct a parent paragraph
			int wsVern = 101;
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			AddRunToStrBldr(strBldr,"some text", wsVern, null);
			AddRunToStrBldr(strBldr,"x", wsVern, ScrStyleNames.FootnoteMarker); // a char style
			ITsString tssParentPara = strBldr.GetString();

			// Construct the expected result
			int wsAnal = 105;

			// Call the method under test
			ITsString tssTrans = m_editingHelper.GenerateTranslationCVNums(tssParentPara, wsAnal);

			//  Verify the results
			Assert.IsNull(tssTrans, "expected null return for text only");

			// Also try it with an empty parent para
			strBldr = TsStrBldrClass.Create();
			tssParentPara = strBldr.GetString(); //empty
			tssTrans = m_editingHelper.GenerateTranslationCVNums(tssParentPara, wsAnal);
			Assert.IsNull(tssTrans, "expected null return for empty para");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function appends a run to the given string builder
		/// </summary>
		/// <param name="strBldr"></param>
		/// <param name="text"></param>
		/// <param name="ws"></param>
		/// <param name="charStyle"></param>
		/// ------------------------------------------------------------------------------------
		private void AddRunToStrBldr(ITsStrBldr strBldr, string text, int ws, string charStyle)
		{
			strBldr.Replace(strBldr.Length, strBldr.Length, text,
				StyleUtils.CharStyleTextProps(charStyle, ws));
		}
		#endregion

		#region Delete book title
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test deleting a book title when the writing system of the paragraph is not defined.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteBookTitle_WithoutWs()
		{
			CheckDisposed();

			// Define the book title paragraph without a writing system.
			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Philemon", null);
			m_scrInMemoryCache.AddTitleToMockedBook(philemon.Hvo, bldr.GetString(),
				propFact.MakeProps("Title Main", 0, 0));

			m_editingHelper.SetupSelectionForTitle(philemon);

			// Delete the title.
			ReflectionHelper.CallMethod(m_editingHelper, "DeleteBookTitle", null);

			// We expect that the writing system for the book title would be set to the
			// vernacular writing system.
			StTxtPara para = (StTxtPara)philemon.TitleOA.ParagraphsOS[0];
			int dummy;
			int titleWs = para.Contents.UnderlyingTsString.get_Properties(0).GetIntPropValues(
				(int)FwTextPropType.ktptWs, out dummy);
			Assert.AreEqual(Cache.DefaultVernWs, titleWs);
		}
		#endregion

		#region Delete picture tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a picture object and inserts it into the cache's picture collection.
		/// </summary>
		/// <returns>The hvo of the picture.</returns>
		/// ------------------------------------------------------------------------------------
		private int CreateAndInsertPicture()
		{
			int pictureCount = Cache.LangProject.PicturesOC.Count;

			int hvoFolder = Cache.CreateObject(CmFolder.kClassId,
				Cache.LangProject.Hvo,
				(int)LangProject.LangProjectTags.kflidPictures, 0);
			CmFolder folder = new CmFolder(Cache, hvoFolder, true, true);
			CmFile file = new CmFile();
			folder.FilesOC.Add(file);

			int hvoPict = Cache.CreateObject(CmPicture.kclsidCmPicture);
			CmPicture picture = new CmPicture(Cache, hvoPict, true, true);
			picture.PictureFileRA = file;

			// Make sure the picture got added to the cache's picture collection.
			Assert.AreEqual(pictureCount + 1, Cache.LangProject.PicturesOC.Count);

			return picture.Hvo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts the picture at the beginning of the specified paragraph.
		/// </summary>
		/// <param name="hvoPic">hvo of picture.</param>
		/// <param name="para">Paragraph into which the picture is inserted, heretofore,
		/// whereof.</param>
		/// ------------------------------------------------------------------------------------
		private SelectionHelper PutPictureInParagraph(int hvoPic, StTxtPara para)
		{
			int ichOrc = para.Contents.Length;

			// Create orc for picture in the paragraph
			ITsStrBldr bldr = para.Contents.UnderlyingTsString.GetBldr();
			byte[] objData = MiscUtils.GetObjData(Cache.GetGuidFromId(hvoPic),
				(byte)FwObjDataTypes.kodtGuidMoveableObjDisp);
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValueRgch((int)FwTextPropType.ktptObjData,
				objData, objData.Length);
			bldr.Replace(0, 0, new string(StringUtils.kchObject, 1), propsBldr.GetTextProps());
			para.Contents.UnderlyingTsString = bldr.GetString();

			// set up a selection
			SelectionHelper helper = new SelectionHelper();
			helper.NumberOfLevels = 5;
			helper.LevelInfo[4].hvo = para.Owner.Owner.OwnerHVO;
			helper.LevelInfo[4].tag = (int)Scripture.ScriptureTags.kflidScriptureBooks;
			helper.LevelInfo[3].hvo = para.Owner.OwnerHVO;
			helper.LevelInfo[3].tag = (int)ScrBook.ScrBookTags.kflidSections;
			helper.LevelInfo[2].hvo = para.OwnerHVO;
			helper.LevelInfo[2].tag = para.Owner.OwningFlid;
			helper.LevelInfo[1].hvo = para.Hvo;
			helper.LevelInfo[1].tag = (int)StText.StTextTags.kflidParagraphs;
			helper.LevelInfo[0].hvo = hvoPic;
			helper.LevelInfo[0].tag = (int)StTxtPara.StTxtParaTags.kflidContents;
			helper.LevelInfo[0].ich = ichOrc;
			return helper;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests deleting a picture in the contents of a Scripture section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeletePicture()
		{
			CheckDisposed();

			int origPictureCount = Cache.LangProject.PicturesOC.Count;
			int hvoPic = CreateAndInsertPicture();

			// Get the first paragraph in the first section of the first book.
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is the paragraph", null);
			int ichOrc = para.Contents.Length;
			section.AdjustReferences();

			int paraRunCount = para.Contents.UnderlyingTsString.RunCount;

			SelectionHelper helper = PutPictureInParagraph(hvoPic, para);
			helper.IchAnchor = -1;
			helper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, -2);
			ReflectionHelper.SetField(m_editingHelper, "m_viewSelection", helper);

			m_editingHelper.CallDeletePicture(helper, hvoPic);

			Assert.AreEqual(0, Cache.GetClassOfObject(hvoPic),
				"Picture object is still in cache");

			Assert.AreEqual(paraRunCount, para.Contents.UnderlyingTsString.RunCount,
				"Paragraph's run count is invalid. Picture might not have been deleted.");

			Assert.AreEqual(null,
				para.Contents.UnderlyingTsString.get_Properties(0).GetStrPropValue(
				(int)FwTextPropType.ktptObjData),
				"The picture's ORC is still in the paragraph.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing should happen when the user presses enter and a picture is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PressingEnterWithPictureSelected_InScrSectionContents()
		{
			CheckDisposed();

			int hvoPic = CreateAndInsertPicture();
			CmPicture pic = new CmPicture(Cache, hvoPic);
			pic.Caption.SetVernacularDefaultWritingSystem("My Caption");

			// Get the first paragraph in the first section of the first book.
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is the paragraph", null);
			section.AdjustReferences();

			SelectionHelper helper = PutPictureInParagraph(hvoPic, para);
			helper.IchAnchor = 4;
			helper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, (int)CmPicture.CmPictureTags.kflidCaption);
			ReflectionHelper.SetField(m_editingHelper, "m_viewSelection", helper);

			DynamicMock rootbox = new DynamicMock(typeof(IVwRootBox));
			rootbox.Ignore("Close");
			rootbox.Strict = true;
			ReflectionHelper.SetField(m_RootSite, "m_rootb", rootbox.MockInstance);
			ReflectionHelper.CallMethod(m_editingHelper, "HandleEnterKey", true, string.Empty,
				0, 0, VwShiftStatus.kfssNone, null, Keys.None);
			rootbox.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nothing should happen when the user presses enter and a picture is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PressingEnterWithPictureSelected_InSectionHead()
		{
			CheckDisposed();

			int hvoPic = CreateAndInsertPicture();
			CmPicture pic = new CmPicture(Cache, hvoPic);
			pic.Caption.SetVernacularDefaultWritingSystem("My Caption");

			// Get the first paragraph in the first section of the first book.
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedText(section.HeadingOAHvo,
				ScrStyleNames.SectionHead);

			m_scrInMemoryCache.AddRunToMockedPara(para, "This is the heading", null);
			section.AdjustReferences();

			SelectionHelper helper = PutPictureInParagraph(hvoPic, para);
			helper.IchAnchor = 4;
			helper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, (int)CmPicture.CmPictureTags.kflidCaption);
			ReflectionHelper.SetField(m_editingHelper, "m_viewSelection", helper);

			DynamicMock sel = new DynamicMock(typeof(IVwSelection));
			sel.SetupResult("IsRange", false);
			ReflectionHelper.SetField(helper, "m_vwSel", sel.MockInstance);

			DynamicMock rootbox = new DynamicMock(typeof(IVwRootBox));
			ReflectionHelper.SetField(m_RootSite, "m_rootb", rootbox.MockInstance);
			ReflectionHelper.CallMethod(m_editingHelper, "HandleEnterKey", true, string.Empty,
				0, 0, VwShiftStatus.kfssNone, null, Keys.None);
		}
		#endregion

		#region AtEndOfSection tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AtEndOfSection method when selection is in middle of last paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtEndOfSection_WhenSelectionIsInMiddleOfLastParagraph()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "this is text", null);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "this is more text", null);

			m_editingHelper.SetupSelectionForParas(para.Hvo, para.Hvo, 5, 5);
			Assert.IsFalse(m_editingHelper.AtEndOfSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AtEndOfSection method when selection at the end of first paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtEndOfSection_WhenSelectionIsAtEndOfFirstPara()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "this is text", null);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "this is more text", null);
			int ich = para1.Contents.Length;
			m_editingHelper.SetupSelectionForParas(para1.Hvo, para1.Hvo, ich, ich);
			Assert.IsFalse(m_editingHelper.AtEndOfSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AtEndOfSection method when selection is at the end of last paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtEndOfSection_WhenSelectionIsAtEndOfLastPara()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "this is text", null);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "this is more text", null);
			int ich = para2.Contents.Length;
			m_editingHelper.SetupSelectionForParas(para2.Hvo, para2.Hvo, ich, ich);
			Assert.IsTrue(m_editingHelper.AtEndOfSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AtEndOfSection method when selection is at the end of the heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtEndOfSection_WhenSelectionIsAtEndOfHeading()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "heading 1", ScrStyleNames.NormalParagraph);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "this is more text", null);
			int ich = para1.Contents.Length;
			m_editingHelper.SetupSelectionForParas(para1.Hvo, para1.Hvo, ich, ich);
			Assert.IsFalse(m_editingHelper.AtEndOfSection);
		}
		#endregion

		#region ScriptureCanImmediatelyFollowCurrentSection
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the ScriptureCanImmediatelyFollowCurrentSection property is true when in
		/// the last intro section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScriptureCanImmediatelyFollowCurrentSection_InLastIntroSection()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddIntroSectionToMockedBook(philemon.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "this is intro", null);
			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "this is more text", null);
			section2.AdjustReferences();

			m_editingHelper.SetupSelectionForSection(section.Hvo); // should be last intro section
			Assert.IsTrue(m_editingHelper.ScriptureCanImmediatelyFollowCurrentSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the ScriptureCanImmediatelyFollowCurrentSection property is true when in
		/// the last section of a book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScriptureCanImmediatelyFollowCurrentSection_InLastSection()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddIntroSectionToMockedBook(philemon.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "this is intro", null);
			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "this is more text", null);
			section2.AdjustReferences();

			m_editingHelper.SetupSelectionForSection(section2.Hvo);
			Assert.IsTrue(m_editingHelper.ScriptureCanImmediatelyFollowCurrentSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the ScriptureCanImmediatelyFollowCurrentSection property is false when in
		/// the first (i.e., not the last) intro section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScriptureCanImmediatelyFollowCurrentSection_InFirstIntroSection()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddIntroSectionToMockedBook(philemon.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "this is intro", null);
			IScrSection section2 = m_scrInMemoryCache.AddIntroSectionToMockedBook(philemon.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "this is more intro", null);
			section.AdjustReferences();
			section2.AdjustReferences();
			m_editingHelper.SetupSelectionForSection(section.Hvo);
			Assert.IsFalse(m_editingHelper.ScriptureCanImmediatelyFollowCurrentSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the ScriptureCanImmediatelyFollowCurrentSection property is false when in
		/// the book title
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScriptureCanImmediatelyFollowCurrentSection_InBookTitle()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			m_scrInMemoryCache.AddTitleToMockedBook(philemon.Hvo, "Philemon");

			m_editingHelper.SetupSelectionForTitle(philemon);
			Assert.IsFalse(m_editingHelper.ScriptureCanImmediatelyFollowCurrentSection);
		}
		#endregion

		#region AtBeginningOfFirstScriptureSection
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the AtBeginningOfFirstScriptureSection property is false when in a
		/// section body (can't be at the BEGINNING if we're in the body).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtBeginningOfFirstScriptureSection_WhenInSectionBody()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "this is intro", null);
			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "this is more text", null);
			section2.AdjustReferences();

			m_editingHelper.SetupSelectionInPara(para2, 0, section2, 1, philemon, 0, 5, false);
			m_editingHelper.SetupSelectionForParas(para2.Hvo, para2.Hvo, 0, 0);
			m_editingHelper.SetupCurrentReference(57001001);
			Assert.IsFalse(m_editingHelper.AtBeginningOfFirstScriptureSection,
				"AtBeginningOfFirstScriptureSection should be false when in a section body.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the AtBeginningOfFirstScriptureSection property is true when at the
		/// beginning of the head of the first Scripture section of a book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtBeginningOfFirstScriptureSection_WhenAtBeginningOfFirstScriptureSection()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddIntroSectionToMockedBook(philemon.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "this is intro", null);
			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara headingPara = m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo, "heading", ScrStyleNames.SectionHead);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "this is more text", null);
			section2.AdjustReferences();

			Assert.AreEqual(57001001, section2.VerseRefMin,
				"Sanity check to make sure we're in first Scripture section");

			m_editingHelper.SetupCurrentReference(57001001);
			m_editingHelper.SetupSelectionInPara(headingPara, 0, section2, 1, philemon, 0, 0, true);
			Assert.IsTrue(m_editingHelper.AtBeginningOfFirstScriptureSection,
				"AtBeginningOfFirstScriptureSection should be true when at the beginning of the head of the first Scripture section of a book.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the AtBeginningOfFirstScriptureSection property is false when at the
		/// beginning of the second para of the head of the first Scripture section of a book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtBeginningOfFirstScriptureSection_WhenAtBeginningOfSecondParaOfFirstScriptureSection()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "heading", ScrStyleNames.SectionHead);
			StTxtPara para = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Section Head Para 2", ScrStyleNames.SectionHead);
			section.AdjustReferences();

			Assert.IsTrue(section.VerseRefMin == 57001001,
				"Sanity check to make sure we're in first Scripture section");

			m_editingHelper.SetupCurrentReference(57001001);
			m_editingHelper.SetupSelectionInPara(para, 1, section, 0, philemon, 0, 0, true);
			Assert.IsFalse(m_editingHelper.AtBeginningOfFirstScriptureSection,
				"AtBeginningOfFirstScriptureSection should be false when at the beginning of the head of the second paragraph of the first Scripture section of a book.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the AtBeginningOfFirstScriptureSection property is false when in an
		/// intro section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtBeginningOfFirstScriptureSection_WhenAtStartOfIntroSectionHead()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddIntroSectionToMockedBook(philemon.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "heading", ScrStyleNames.IntroSectionHead);

			m_editingHelper.SetupSelectionInPara(para, 0, section, 0, philemon, 0, 0, true);
			m_editingHelper.SetupCurrentReference(57001000);
			Assert.IsFalse(m_editingHelper.AtBeginningOfFirstScriptureSection,
				"AtBeginningOfFirstScriptureSection should be false when in an intro section.");
		}
		#endregion

		#region Go to footnote
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests go to next footnote from the first footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToNextFootnote_FirstFootnote()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "this is more text", null);
			m_scrInMemoryCache.AddFootnote(philemon, para, 1);
			m_scrInMemoryCache.AddFootnote(philemon, para, 10);

			m_editingHelper.SetupSelectionInPara(para, 0, section, 0,
				philemon, 0, 4, false);
			ScrFootnote nextfootnote = m_editingHelper.GoToNextFootnote();
			Assert.IsNotNull(nextfootnote, "Couldn't find footnote");
			Assert.AreEqual(philemon.FootnotesOS.HvoArray[1], nextfootnote.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests go to next footnote from a book that has no footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToNextFootnote_NoFootnoteInCurrentBook()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "this is more text", null);

			IScrBook james = m_scrInMemoryCache.AddBookToMockedScripture(59, "James");
			m_scrInMemoryCache.AddTitleToMockedBook(james.Hvo, "James");
			IScrSection sectionJ = m_scrInMemoryCache.AddSectionToMockedBook(james.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionJ.Hvo, "Heading", ScrStyleNames.SectionHead);
			StTxtPara paraJ = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionJ.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraJ, "this is more text", null);
			m_scrInMemoryCache.AddFootnote(james, paraJ, 3);

			m_editingHelper.SetupSelectionInPara(para, 0, section, 0,
				philemon, 0, 0, false);
			ScrFootnote nextfootnote = m_editingHelper.GoToNextFootnote();
			Assert.IsNotNull(nextfootnote, "Couldn't find footnote");
			Assert.AreEqual(james.FootnotesOS.HvoArray[0], nextfootnote.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests go to next footnote from the last footnote of the last book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToNextFootnote_AfterLastFootnote()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "this is more text", null);
			m_scrInMemoryCache.AddFootnote(philemon, para, 5);

			m_editingHelper.SetupSelectionInPara(para, 0, section, 0,
				philemon, 0, 7, false);
			ScrFootnote nextfootnote = m_editingHelper.GoToNextFootnote();
			Assert.IsNull(nextfootnote, "Footnote was found");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests go to previous footnote from the last footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToPreviousFootnote_LastFootnote()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "this is more text", null);
			m_scrInMemoryCache.AddFootnote(philemon, para, 1);
			m_scrInMemoryCache.AddFootnote(philemon, para, 10);

			m_editingHelper.SetupSelectionInPara(para, 0, section, 0,
				philemon, 0, 8, false);
			ScrFootnote previousfootnote = m_editingHelper.GoToPreviousFootnote();
			Assert.IsNotNull(previousfootnote, "Couldn't find footnote");
			Assert.AreEqual(philemon.FootnotesOS.HvoArray[philemon.FootnotesOS.Count - 2],
				previousfootnote.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests go to previous footnote from a book that has no footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToPreviousFootnote_NoFootnoteInCurrentBook()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "this is more text", null);
			m_scrInMemoryCache.AddFootnote(philemon, para, 3);

			IScrBook james = m_scrInMemoryCache.AddBookToMockedScripture(59, "James");
			m_scrInMemoryCache.AddTitleToMockedBook(james.Hvo, "James");
			IScrSection sectionJ = m_scrInMemoryCache.AddSectionToMockedBook(james.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionJ.Hvo, "Heading", ScrStyleNames.SectionHead);
			StTxtPara paraJ = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionJ.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraJ, "this is more text", null);

			m_editingHelper.SetupSelectionInPara(paraJ, 0, sectionJ, 0,
				james, 1, 4, false);
			ScrFootnote previousfootnote = m_editingHelper.GoToPreviousFootnote();
			Assert.IsNotNull(previousfootnote, "Couldn't find footnote");
			Assert.AreEqual(philemon.FootnotesOS.HvoArray[philemon.FootnotesOS.Count - 1],
				previousfootnote.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests go to previous footnote from the first footnote of the first book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToPreviousFootnote_BeforeFirstFootnote()
		{
			CheckDisposed();

			IScrBook james = m_scrInMemoryCache.AddBookToMockedScripture(59, "James");
			m_scrInMemoryCache.AddTitleToMockedBook(james.Hvo, "James");
			IScrSection sectionJ = m_scrInMemoryCache.AddSectionToMockedBook(james.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionJ.Hvo, "Heading", ScrStyleNames.SectionHead);
			StTxtPara paraJ = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionJ.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraJ, "this is more text", null);
			m_scrInMemoryCache.AddFootnote(james, paraJ, 6);

			m_editingHelper.SetupSelectionInPara(paraJ, 0, sectionJ, 0,
				james, 0, 4, false);
			ScrFootnote previousfootnote = m_editingHelper.GoToPreviousFootnote();
			Assert.IsNull(previousfootnote, "Footnote was found");
		}
		#endregion

		#region AboutToDelete tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the owner of a footnote is corrected when a paragraph is joined to another
		/// paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AboutToDelete_CorrectFootnoteOwner()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			section.VerseRefStart = section.VerseRefEnd = 57001001;
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "this is some text", null);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "this is some more text", null);
			StFootnote fn = m_scrInMemoryCache.AddFootnote(philemon, para2, 12);
			// force the footnote cache to be built now if it wasn't earlier.
			philemon = new ScrBook(m_scrInMemoryCache.Cache, philemon.Hvo);
			ScrFootnote footnote = new ScrFootnote(m_scrInMemoryCache.Cache, fn.Hvo);
			Assert.AreEqual(para2.Hvo, footnote.ContainingParagraphHvo, "Footnote containing paragraph not setup correctly");

			// check the normal case - fMergeNext is false
			m_editingHelper.SetupSelectionInPara(para2, 1, section, 0, philemon, 0, 0, false);
			m_editingHelper.AboutToDelete(m_editingHelper.CurrentSelection, para2.Hvo, para2.OwnerHVO,
				(int)StText.StTextTags.kflidParagraphs, 1, false);
			Assert.AreEqual(para1.Hvo, footnote.ContainingParagraphHvo, "Footnote containing paragraph not updated on paragraph merge - fMergeNext = false");

			// check the special case for an empty paragraph - fMergeNext is true
			m_editingHelper.SetupSelectionInPara(para1, 0, section, 0, philemon, 0, 0, false);
			m_editingHelper.AboutToDelete(m_editingHelper.CurrentSelection, para1.Hvo, para1.OwnerHVO,
				(int)StText.StTextTags.kflidParagraphs, 0, true);
			Assert.AreEqual(para2.Hvo, footnote.ContainingParagraphHvo, "Footnote containing paragraph not updated on paragraph merge - fMergeNext = true");
		}
		#endregion

		#region Style tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting a default character style for the context. Should always be
		/// default paragraph characters (or an unspecified style).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetDefaultStyleForContext_WithCharStyle()
		{
			CheckDisposed();

			Assert.AreEqual(string.Empty,
				TeEditingHelper.GetDefaultStyleForContext(ContextValues.General, true));
		}
		#endregion

		#region GetVerseText tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetVerseText method when the text is in the title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetVerseText_FoundInTitle()
		{
			CheckDisposed();
			CreateExodusData();
			int ichStart;
			List<TeEditingHelper.VerseTextSubstring> title =
				TeEditingHelper.GetVerseText(m_scr, new ScrReference(02000000, m_scr.Versification),
				out ichStart);
			Assert.AreEqual(1, title.Count);
			Assert.AreEqual("Exodus", title[0].Text);
			Assert.AreEqual(0, title[0].ParagraphIndex);
			Assert.AreEqual((int)ScrBook.ScrBookTags.kflidTitle, title[0].Tag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetVerseText method when the verse can be found
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetVerseText_FoundInSinglePara()
		{
			CheckDisposed();
			CreateExodusData();
			int ichStart;
			List<TeEditingHelper.VerseTextSubstring> verse1_3 =
				TeEditingHelper.GetVerseText(m_scr, new ScrReference(02001003, m_scr.Versification),
				out ichStart);
			Assert.AreEqual(1, verse1_3.Count);
			Assert.AreEqual(1, ichStart);
			Assert.AreEqual("Verse three.", verse1_3[0].Text);
			Assert.AreEqual(1, verse1_3[0].SectionIndex);
			Assert.AreEqual(1, verse1_3[0].ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetVerseText method when the verse can be found but the text is in a
		/// paragraph other than the one where the verse number is found. Jira issue is TE-4459.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetVerseText_MultipleParas()
		{
			CheckDisposed();
			CreateExodusData();
			StTxtPara para = (StTxtPara)m_scr.ScriptureBooksOS[0].SectionsOS[1].ContentOA.ParagraphsOS[2];
			ITsStrBldr bldr = para.Contents.UnderlyingTsString.GetBldr();
			bldr.Replace(0, 0, "Some glumpy text. ", bldr.get_Properties(1));
			para.Contents.UnderlyingTsString = bldr.GetString();
			int ichStart;
			List<TeEditingHelper.VerseTextSubstring> verse1_3 =
				TeEditingHelper.GetVerseText(m_scr, new ScrReference(02001003, m_scr.Versification), out ichStart);
			Assert.AreEqual(2, verse1_3.Count);
			Assert.AreEqual(1, ichStart);
			Assert.AreEqual("Verse three.", verse1_3[0].Text);
			Assert.AreEqual(1, verse1_3[0].SectionIndex);
			Assert.AreEqual(1, verse1_3[0].ParagraphIndex);
			Assert.AreEqual("Some glumpy text. ", verse1_3[1].Text);
			Assert.AreEqual(1, verse1_3[1].SectionIndex);
			Assert.AreEqual(2, verse1_3[1].ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetVerseText method when the verse is split across a section break.
		/// Jira issue is TE-4459.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetVerseText_VerseSplitAcrossSection()
		{
			CheckDisposed();
			CreateExodusData();
			IScrSection section3 = m_scr.ScriptureBooksOS[0].SectionsOS[2];
			StTxtPara para = (StTxtPara)section3.ContentOA.ParagraphsOS[0];
			ITsStrBldr bldr = para.Contents.UnderlyingTsString.GetBldr();
			bldr.Replace(0, 0, "More of verse five. ", bldr.get_Properties(1));
			para.Contents.UnderlyingTsString = bldr.GetString();
			section3.AdjustReferences();
			int ichStart;
			List<TeEditingHelper.VerseTextSubstring> verse1_5 =
				TeEditingHelper.GetVerseText(m_scr, new ScrReference(02001005, m_scr.Versification), out ichStart);
			Assert.AreEqual(2, verse1_5.Count);
			Assert.AreEqual(14, ichStart);
			Assert.AreEqual("Verse five.", verse1_5[0].Text);
			Assert.AreEqual(1, verse1_5[0].SectionIndex);
			Assert.AreEqual(2, verse1_5[0].ParagraphIndex);
			Assert.AreEqual("More of verse five. ", verse1_5[1].Text);
			Assert.AreEqual(2, verse1_5[1].SectionIndex);
			Assert.AreEqual(0, verse1_5[1].ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetVerseText method when the verse does not exist
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetVerseText_Missing()
		{
			CheckDisposed();
			CreateExodusData();
			int ichStart;
			List<TeEditingHelper.VerseTextSubstring> verse2_5 =
				TeEditingHelper.GetVerseText(m_scr, new ScrReference(02002005, m_scr.Versification), out ichStart);
			Assert.AreEqual(0, verse2_5.Count);
			Assert.AreEqual(-1, ichStart);
		}
		#endregion

		#region Annotation tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the GetNoteTargetObject method returns the correct CmPicture when the Hvo
		/// of a picture is passed to it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetNoteTargetObject_Picture()
		{
			int pictHvo = CreateAndInsertPicture();
			CmObject pictureObject = (CmObject)ReflectionHelper.GetResult(
				m_editingHelper, "GetNoteTargetObject", pictHvo);
			Assert.AreEqual(pictHvo, pictureObject.Hvo);
		}
		#endregion

		#region FindPictureInVerse tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindPictureInVerse method when the picture is found
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPictureInVerse_Found()
		{
			CheckDisposed();
			CreateExodusData();
			StTxtPara paraS1P0 = (StTxtPara)m_scr.ScriptureBooksOS[0].SectionsOS[1].ContentOA.ParagraphsOS[0];
			ITsString tss = paraS1P0.Contents.UnderlyingTsString;
			ITsStrFactory factory = TsStrFactoryClass.Create();
			using (DummyFileMaker fileMaker = new DummyFileMaker("junk.jpg", true))
			{
				CmPicture pict = new CmPicture(Cache, fileMaker.Filename,
					factory.MakeString("Test picture picture", Cache.DefaultVernWs),
					StringUtils.LocalPictures);
				int positionToInsertOrc = tss.Length;
				pict.InsertORCAt(tss, positionToInsertOrc, paraS1P0.Hvo,
					(int)StTxtPara.StTxtParaTags.kflidContents, 0);
				int iSection, iPara, ichOrcPos;
				Assert.IsTrue(m_editingHelper.FindPictureInVerse(new ScrReference(02001002, m_scr.Versification), pict.Hvo,
					out iSection, out iPara, out ichOrcPos));
				Assert.AreEqual(1, iSection);
				Assert.AreEqual(0, iPara);
				Assert.AreEqual(positionToInsertOrc, ichOrcPos);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindPictureInVerse method when the picture is not found
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindPictureInVerse_NotFound()
		{
			CheckDisposed();
			CreateExodusData();
			int iSection, iPara, ichOrcPos;
			Assert.IsFalse(m_editingHelper.FindPictureInVerse(
				new ScrReference(02001002, m_scr.Versification), 12345 /* bogus picture HVO */,
				out iSection, out iPara, out ichOrcPos));
		}
		#endregion


		#region FindTextInVerse tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindTextInVerse method when the text is found at the specified reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindTextInVerse_Exists()
		{
			IScrBook genesis = m_scrInMemoryCache.AddBookWithTwoSections(1, "Genesis");
			StTxtPara para = (StTxtPara)genesis.SectionsOS[0].ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Hey, find this!", ScrStyleNames.Normal);

			int ichStart, ichEnd, iSection, iPara;
			ScrReference scrRef = new ScrReference(1, 1, 1, m_scr.Versification);

			Assert.IsTrue(TeEditingHelper.FindTextInVerse(m_scr,
				StringUtils.MakeTss("this", (int)InMemoryFdoCache.s_wsHvos.Fr), scrRef, true,
				out iSection, out iPara, out ichStart, out ichEnd));
			Assert.AreEqual(12, ichStart);
			Assert.AreEqual(16, ichEnd);
			Assert.AreEqual(0, iSection);
			Assert.AreEqual(0, iPara);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindTextInVerse method when the text is found at the specified reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindTextInVerse_ExistsInSubsequentPara()
		{
			IScrBook genesis = m_scrInMemoryCache.AddBookWithTwoSections(1, "Genesis");
			IScrSection section = genesis.SectionsOS[0];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Hey, find this", ScrStyleNames.Normal);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "really nice text!", ScrStyleNames.Normal);

			int ichStart, ichEnd, iSection, iPara;
			ScrReference scrRef = new ScrReference(1, 1, 1, m_scr.Versification);

			Assert.IsTrue(TeEditingHelper.FindTextInVerse(m_scr,
				StringUtils.MakeTss("nice", (int)InMemoryFdoCache.s_wsHvos.Fr), scrRef, true,
				out iSection, out iPara, out ichStart, out ichEnd));
			Assert.AreEqual(7, ichStart);
			Assert.AreEqual(11, ichEnd);
			Assert.AreEqual(0, iSection);
			Assert.AreEqual(1, iPara);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindTextInVerse method when the text is not found at the specified
		/// reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindTextInVerse_ExistethNot()
		{
			IScrBook genesis = m_scrInMemoryCache.AddBookWithTwoSections(1, "Genesis");
			StTxtPara para = (StTxtPara)genesis.SectionsOS[0].ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Hey, find this!", ScrStyleNames.Normal);

			int ichStart, ichEnd, iSection, iPara;
			ScrReference scrRef = new ScrReference(1, 1, 1, m_scr.Versification);

			Assert.IsFalse(TeEditingHelper.FindTextInVerse(m_scr,
				StringUtils.MakeTss("that", (int)InMemoryFdoCache.s_wsHvos.Fr), scrRef, true,
				out iSection, out iPara, out ichStart, out ichEnd));
		}
		#endregion
	}
	#endregion

	#region Deleting a section with footnotes
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests that deleting a section also deletes the footnotes of this section (TE-3983).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DeleteSectionWithFootnotes: TeEditingHelperTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deleting from start of last paragraph in heading of a section through to the end of
		/// the following section head should leave a single section with the two heading
		/// paragraphs from the original section and the contents of the following section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Tests condition that is not allowed in current code - enable as fix for TE-6099")]
		public void DeletePartOfSectionHeadAndAllOfFollowingSectionHead()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			m_scrInMemoryCache.AddTitleToMockedBook(philemon.Hvo, "Philemon");
			// First section has three paragraphs in the heading
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara paraHeading1_1 = m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo,
				"First heading para", ScrStyleNames.SectionHead);
			ITsString tssH1P1 = paraHeading1_1.Contents.UnderlyingTsString;
			StTxtPara paraHeading1_2 = m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo,
				"Second heading para", ScrStyleNames.SectionHead);
			ITsString tssH1P2 = paraHeading1_2.Contents.UnderlyingTsString;
			StTxtPara paraHeading1_3 = m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo,
				"Third heading para", ScrStyleNames.SectionHead);
			StTxtPara paraContent1_1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraContent1_1, "this is more text", null);
			m_scrInMemoryCache.AddFootnote(philemon, paraContent1_1, 4);
			section1.AdjustReferences();
			// Add a second section
			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara paraHeading2_1 = m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo,
				"The heading", ScrStyleNames.SectionHead);
			StTxtPara paraContent2_1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraContent2_1, "Some text", null);
			m_scrInMemoryCache.AddFootnote(philemon, paraContent2_1, 4);
			ITsString tssS2C1 = paraContent2_1.Contents.UnderlyingTsString;
			section2.AdjustReferences();
			m_editingHelper.SetupRangeSelection(philemon, 0, section1, 0, paraHeading1_3, 2, 0, true,
				philemon, 0, section2, 1, paraHeading2_1, 0, paraHeading2_1.Contents.Length, true);

			bool fRet = m_editingHelper.CallHandleComplexDeletion();

			Assert.IsTrue(fRet, "Deletion wasn't handled");
			Assert.AreEqual(1, philemon.FootnotesOS.HvoArray.Length);
			Assert.AreEqual(1, philemon.SectionsOS.Count);
			Assert.AreEqual(section1, philemon.SectionsOS[0]);
			FdoOwningSequence<IStPara> paras = section1.HeadingOA.ParagraphsOS;
			Assert.AreEqual(3, paras.Count);
			AssertEx.AreTsStringsEqual(tssH1P1, ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
			AssertEx.AreTsStringsEqual(tssH1P2, ((StTxtPara)paras[1]).Contents.UnderlyingTsString);
			Assert.AreEqual(0, ((StTxtPara)paras[2]).Contents.Length);
			paras = section1.ContentOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			AssertEx.AreTsStringsEqual(tssS2C1, ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deleting Preceding section and first two paragraphs in heading of a section delete
		/// those paragraphs and their footnotes but doesn't delete footnotes in contents of
		/// the surviving paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Tests condition that is not allowed in current code - enable as fix for TE-6099")]
		public void DeleteSectionAndPartOfFollowingSectionHead()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			m_scrInMemoryCache.AddTitleToMockedBook(philemon.Hvo, "Philemon");
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara paraHeading1_1 = m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo,
				"The heading", ScrStyleNames.SectionHead);
			StTxtPara paraContent1_1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraContent1_1, "Some text", null);
			section1.AdjustReferences();
			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara paraHeading2_1 = m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo,
				"First heading para", ScrStyleNames.SectionHead);
			StTxtPara paraHeading2_2 = m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo,
				"Second heading para", ScrStyleNames.SectionHead);
			StTxtPara paraHeading2_3 = m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo,
				"Third heading para", ScrStyleNames.SectionHead);
			StTxtPara paraContent2_1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraContent2_1, "this is more text", null);
			m_scrInMemoryCache.AddFootnote(philemon, paraContent2_1, 4);
			ITsString tssC1 = paraContent2_1.Contents.UnderlyingTsString;
			StTxtPara paraContent2_2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraContent2_2, "this is the rest of the text", null);
			ITsString tssC2 = paraContent2_2.Contents.UnderlyingTsString;
			section2.AdjustReferences();
			m_editingHelper.SetupRangeSelection(philemon, 0, section1, 0, paraHeading1_1, 0, 0, true,
				philemon, 0, section2, 1, paraHeading2_3, 2, 0, true);

			bool fRet = m_editingHelper.CallHandleComplexDeletion();

			Assert.IsTrue(fRet, "Deletion wasn't handled");
			Assert.AreEqual(1, philemon.FootnotesOS.HvoArray.Length);
			Assert.AreEqual(1, philemon.SectionsOS.Count);
			Assert.AreEqual(section1, philemon.SectionsOS[0]);
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(2, section1.ContentOA.ParagraphsOS.Count);
			AssertEx.AreTsStringsEqual(tssC1, ((StTxtPara)section1.ContentOA.ParagraphsOS[0]).Contents.UnderlyingTsString);
			AssertEx.AreTsStringsEqual(tssC2, ((StTxtPara)section1.ContentOA.ParagraphsOS[1]).Contents.UnderlyingTsString);

			// check selection
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading, m_editingHelper.m_testSelection_tag);
			Assert.AreEqual(0, m_editingHelper.m_testSelection_book);
			Assert.AreEqual(0, m_editingHelper.m_testSelection_section);
			Assert.AreEqual(0, m_editingHelper.m_testSelection_para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deleting all sections in a book should delete all footnotes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteAllSectionsWithAllFootnotes()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara paraHeading = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo,
				"The heading", ScrStyleNames.SectionHead);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "this is more text", null);
			m_scrInMemoryCache.AddFootnote(philemon, para, 1);
			m_scrInMemoryCache.AddFootnote(philemon, para, 10);
			section.AdjustReferences();
			int ichLim = para.Contents.Length;
			m_editingHelper.SetupRangeSelection(philemon, 0, section, 0, paraHeading, 0, 0, true,
				philemon, 0, section, 0, para, 0, ichLim, false);

			bool fRet = m_editingHelper.CallHandleComplexDeletion();

			Assert.IsTrue(fRet, "Deletion wasn't handled");
			Assert.AreEqual(0, philemon.FootnotesOS.HvoArray.Length);
			Assert.AreEqual(1, philemon.SectionsOS.Count);
			Assert.AreEqual(1, philemon.SectionsOS[0].HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(0, ((StTxtPara)philemon.SectionsOS[0].HeadingOA.ParagraphsOS[0]).Contents.Length);
			Assert.AreEqual(1, philemon.SectionsOS[0].ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(0, ((StTxtPara)philemon.SectionsOS[0].ContentOA.ParagraphsOS[0]).Contents.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deleting all sections in a book should delete all footnotes except the footnote
		/// in the title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteAllSectionsLeavesFootnoteInTitle()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			StText titleText = m_scrInMemoryCache.AddTitleToMockedBook(philemon.Hvo, "Philemon");
			StFootnote footnote = m_scrInMemoryCache.AddFootnote(philemon, (StTxtPara)titleText.ParagraphsOS[0], 0);

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara paraHeading = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo,
				"The heading", ScrStyleNames.SectionHead);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "this is more text", null);
			m_scrInMemoryCache.AddFootnote(philemon, para, 1);
			m_scrInMemoryCache.AddFootnote(philemon, para, 10);
			section.AdjustReferences();
			int ichLim = para.Contents.Length;
			m_editingHelper.SetupRangeSelection(philemon, 0, section, 0, paraHeading, 0, 0, true,
				philemon, 0, section, 0, para, 0, ichLim, false);

			bool fRet = m_editingHelper.CallHandleComplexDeletion();

			Assert.IsTrue(fRet, "Deletion wasn't handled");
			Assert.AreEqual(1, philemon.FootnotesOS.HvoArray.Length);
			Assert.AreEqual(footnote.Hvo, philemon.FootnotesOS.HvoArray[0]);
			Assert.AreEqual(1, philemon.SectionsOS.Count);
			Assert.AreEqual(1, philemon.SectionsOS[0].HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(0, ((StTxtPara)philemon.SectionsOS[0].HeadingOA.ParagraphsOS[0]).Contents.Length);
			Assert.AreEqual(1, philemon.SectionsOS[0].ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(0, ((StTxtPara)philemon.SectionsOS[0].ContentOA.ParagraphsOS[0]).Contents.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deleting one section deletes only the footnotes that belong to that section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteOneSection()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			// Add a section that remains
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara paraHeading = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo,
				"The heading", ScrStyleNames.SectionHead);
			ITsString tssS1H1 = paraHeading.Contents.UnderlyingTsString;
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "this is more text", null);
			StFootnote fn1 = m_scrInMemoryCache.AddFootnote(philemon, para, 1);
			StFootnote fn2 = m_scrInMemoryCache.AddFootnote(philemon, para, 10);
			ITsString tssS1P1 = para.Contents.UnderlyingTsString;
			section.AdjustReferences();

			// Add a section that will be deleted
			IScrSection sectionToDelete = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara paraHeadingToDelete = m_scrInMemoryCache.AddSectionHeadParaToSection(
				sectionToDelete.Hvo, "The heading will go", ScrStyleNames.SectionHead);
			StTxtPara paraToDelete = m_scrInMemoryCache.AddParaToMockedSectionContent(
				sectionToDelete.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraToDelete, "this will go away", null);
			m_scrInMemoryCache.AddFootnote(philemon, paraToDelete, 1);
			m_scrInMemoryCache.AddFootnote(philemon, paraToDelete, 10);
			sectionToDelete.AdjustReferences();

			// Add a section afterwards that remains
			section = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			paraHeading = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo,
				"The heading", ScrStyleNames.SectionHead);
			ITsString tssS2H1 = paraHeading.Contents.UnderlyingTsString;
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "this is more text", null);
			StFootnote fn3 = m_scrInMemoryCache.AddFootnote(philemon, para, 1);
			StFootnote fn4 = m_scrInMemoryCache.AddFootnote(philemon, para, 10);
			ITsString tssS2P1 = para.Contents.UnderlyingTsString;
			section.AdjustReferences();

			int ichLim = paraToDelete.Contents.Length;
			m_editingHelper.SetupRangeSelection(philemon, 0, sectionToDelete, 1, paraHeadingToDelete,
				0, 0, true, philemon, 0, sectionToDelete, 1, paraToDelete, 0, ichLim, false);

			bool fRet = m_editingHelper.CallHandleComplexDeletion();

			Assert.IsTrue(fRet, "Deletion wasn't handled");
			Assert.AreEqual(4, philemon.FootnotesOS.HvoArray.Length);
			Assert.AreEqual(fn1.Hvo, philemon.FootnotesOS.HvoArray[0]);
			Assert.AreEqual(fn2.Hvo, philemon.FootnotesOS.HvoArray[1]);
			Assert.AreEqual(fn3.Hvo, philemon.FootnotesOS.HvoArray[2]);
			Assert.AreEqual(fn4.Hvo, philemon.FootnotesOS.HvoArray[3]);

			Assert.AreEqual(2, philemon.SectionsOS.Count);

			// Check contents of first section
			section = philemon.SectionsOS[0];
			FdoOwningSequence<IStPara> paras = section.HeadingOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			AssertEx.AreTsStringsEqual(tssS1H1, ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
			paras = section.ContentOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			AssertEx.AreTsStringsEqual(tssS1P1, ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
			// Check contents of second (and last) section
			section = philemon.SectionsOS[1];
			paras = section.HeadingOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			AssertEx.AreTsStringsEqual(tssS2H1, ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
			paras = section.ContentOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			AssertEx.AreTsStringsEqual(tssS2P1, ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deleting from the middle of one section to the middle of another section, with a
		/// section in between.
		/// Also checks that the ORCs are cleaned up in the BTs (see TE-4882).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeletePartOf2Sections()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			// Add a section that remains
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara paraHeading = m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo,
				"The heading", ScrStyleNames.SectionHead);
			ITsString tssS1H1 = paraHeading.Contents.UnderlyingTsString;
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo, ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(para1, "this is more text", null);
			ICmTranslation transP1 = m_inMemoryCache.AddBtToMockedParagraph(para1, InMemoryFdoCache.s_wsHvos.En);
			StFootnote fn1 = m_scrInMemoryCache.AddFootnote(philemon, para1, 1, "footnote 1 text");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(transP1, 0, InMemoryFdoCache.s_wsHvos.En, fn1, "BT of footnote1");
			ITsString tssBtEnP1Orig = transP1.Translation.GetAlternative(InMemoryFdoCache.s_wsHvos.En).UnderlyingTsString;
			StFootnote fn2 = m_scrInMemoryCache.AddFootnote(philemon, para1, 10, "footnote 2 text");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(transP1, 0, InMemoryFdoCache.s_wsHvos.En, fn2, "BT of footnote2");
			string sH1P1Contents = para1.Contents.Text;
			section1.AdjustReferences();

			// Add a section that will be deleted entirely
			IScrSection sectionToDelete = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionToDelete.Hvo,
				"The heading will go", ScrStyleNames.SectionHead);
			StTxtPara paraToDelete = m_scrInMemoryCache.AddParaToMockedSectionContent(
				sectionToDelete.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraToDelete, "this will go away", null);
			m_scrInMemoryCache.AddFootnote(philemon, paraToDelete, 1);
			m_scrInMemoryCache.AddFootnote(philemon, paraToDelete, 10);
			sectionToDelete.AdjustReferences();

			// Add a section that will be combined with the first section
			IScrSection section3 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section3.Hvo,
				"The last heading", ScrStyleNames.SectionHead);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section3.Hvo, ScrStyleNames.NormalParagraph);
			m_inMemoryCache.AddRunToMockedPara(para2, "this is more text", null);
			ICmTranslation transP2 = m_inMemoryCache.AddBtToMockedParagraph(para2, InMemoryFdoCache.s_wsHvos.De);
			StFootnote fn3 = m_scrInMemoryCache.AddFootnote(philemon, para2, 1, "footnote 3 text");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(transP2, 0, InMemoryFdoCache.s_wsHvos.De, fn3, "BT of footnote3");
			StFootnote fn4 = m_scrInMemoryCache.AddFootnote(philemon, para2, 10, "footnote 4 text");
			section3.AdjustReferences();

			m_editingHelper.SetupRangeSelection(philemon, 0, section1, 0, para1,
				0, 5, false, philemon, 0, section3, 2, para2, 0, 5, false);

			bool fRet = m_editingHelper.CallHandleComplexDeletion();

			Assert.IsTrue(fRet, "Deletion wasn't handled");
			Assert.AreEqual(2, philemon.FootnotesOS.HvoArray.Length);
			Assert.AreEqual(fn1.Hvo, philemon.FootnotesOS.HvoArray[0]);
			Assert.AreEqual(fn4.Hvo, philemon.FootnotesOS.HvoArray[1]);
			ITsString tssBtEnP1 = transP1.Translation.GetAlternative(InMemoryFdoCache.s_wsHvos.En).UnderlyingTsString;
			AssertEx.AreTsStringsEqual(tssBtEnP1Orig, tssBtEnP1); // Should have deleted second ORC only.
			Assert.AreEqual(0, transP2.Translation.GetAlternative(InMemoryFdoCache.s_wsHvos.De).Length,
				"The German back trans for Para 2 only had one ORC in it, and it should be deleted now.");

			Assert.AreEqual(1, philemon.SectionsOS.Count);

			// Check contents of surviving section
			IScrSection section = philemon.SectionsOS[0];
			FdoOwningSequence<IStPara> paras = section.HeadingOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			AssertEx.AreTsStringsEqual(tssS1H1, ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
			paras = section.ContentOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(sH1P1Contents, ((StTxtPara)paras[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete two sections: the first has footnotes and the second doesn't. (TE-5236)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Delete2Sections_1stWithFootnotes2ndWithout()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");

			// Add first section with one footnote.
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara paraHeading = m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo,
				"The heading of section 1", ScrStyleNames.SectionHead);
			ITsString tssS1H1 = paraHeading.Contents.UnderlyingTsString;
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "this is text for para 1", null);
			StFootnote fn1 = m_scrInMemoryCache.AddFootnote(philemon, para1, 4, "footnote 1 text");
			ITsString tssS1P1 = para1.Contents.UnderlyingTsString;
			section1.AdjustReferences();

			// Add second section with footnotes.
			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			paraHeading = m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo,
				"The heading of section two", ScrStyleNames.SectionHead);
			ITsString tssS2H1 = paraHeading.Contents.UnderlyingTsString;
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "this is text for para 2", null);
			StFootnote fn2 = m_scrInMemoryCache.AddFootnote(philemon, para2, 1, "footnote 2 text");
			StFootnote fn3 = m_scrInMemoryCache.AddFootnote(philemon, para2, 10, "footnote 3 text");
			section2.AdjustReferences();

			// Add third section without footnotes.
			IScrSection section3 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			paraHeading = m_scrInMemoryCache.AddSectionHeadParaToSection(section3.Hvo,
				"The third heading", ScrStyleNames.SectionHead);
			StTxtPara para3 = m_scrInMemoryCache.AddParaToMockedSectionContent(
				section3.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para3, "this is text for para 3", null);
			ITsString tssPara3 = para3.Contents.UnderlyingTsString;
			section3.AdjustReferences();

			// Add fourth section with one footnote.
			IScrSection section4 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			paraHeading = m_scrInMemoryCache.AddSectionHeadParaToSection(section4.Hvo,
				"The fourth heading", ScrStyleNames.SectionHead);
			ITsString tssS4H1 = paraHeading.Contents.UnderlyingTsString;
			StTxtPara para4 = m_scrInMemoryCache.AddParaToMockedSectionContent(
				section4.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para4, "this is text for para 4", null);
			StFootnote fn4 = m_scrInMemoryCache.AddFootnote(philemon, para4, 4, "footnote 4 text");
			ITsString tssS4P1 = para4.Contents.UnderlyingTsString;
			section4.AdjustReferences();

			// Now select the second and third sections and delete them.
			m_editingHelper.SetupRangeSelection(philemon, 0, section2, 1, para2,
				0, 0, false, philemon, 0, section3, 2, para3, 0, tssPara3.Length, false);
			bool fRet = m_editingHelper.CallHandleComplexDeletion();
			Assert.IsTrue(fRet, "Deletion wasn't handled");

			// We expect that both footnotes in the second section will be deleted and that the
			// footnotes in sections 1 and 4 remain.
			Assert.AreEqual(2, philemon.FootnotesOS.HvoArray.Length);
			Assert.AreEqual("this" + StringUtils.kchObject + " is text for para 1",
				para1.Contents.UnderlyingTsString.Text);
			Assert.AreEqual("this" + StringUtils.kchObject + " is text for para 4",
				para4.Contents.UnderlyingTsString.Text);

			Assert.AreEqual(3, philemon.SectionsOS.Count);

			// Check contents of first section
			IScrSection section = philemon.SectionsOS[0];
			FdoOwningSequence<IStPara> paras = section.HeadingOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			AssertEx.AreTsStringsEqual(tssS1H1, ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
			paras = section.ContentOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			AssertEx.AreTsStringsEqual(tssS1P1, ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
			// Check contents of middle section
			section = philemon.SectionsOS[1];
			paras = section.HeadingOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			AssertEx.AreTsStringsEqual(tssS2H1, ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
			paras = section.ContentOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual(0, ((StTxtPara)paras[0]).Contents.Length);
			// Check contents of last section
			section = philemon.SectionsOS[2];
			paras = section.HeadingOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			AssertEx.AreTsStringsEqual(tssS4H1, ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
			paras = section.ContentOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			AssertEx.AreTsStringsEqual(tssS4P1, ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deleting from the middle of one section to the middle of another section where we
		/// have a footnote as first and last thing inside of the selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeletePartOf2Sections_FootnotesInside()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			// Add a section that remains
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara paraHeading = m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo,
				"The heading", ScrStyleNames.SectionHead);
			ITsString tssS1H1 = paraHeading.Contents.UnderlyingTsString;
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "this is more text", null);
			StFootnote fn1 = m_scrInMemoryCache.AddFootnote(philemon, para1, 1);
			StFootnote fn2 = m_scrInMemoryCache.AddFootnote(philemon, para1, 10);
			section1.AdjustReferences();
			// Add a section that will be deleted
			IScrSection sectionToDelete = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionToDelete.Hvo,
				"The heading will go", ScrStyleNames.SectionHead);
			StTxtPara paraToDelete = m_scrInMemoryCache.AddParaToMockedSectionContent(
				sectionToDelete.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraToDelete, "this will go away", null);
			m_scrInMemoryCache.AddFootnote(philemon, paraToDelete, 1);
			m_scrInMemoryCache.AddFootnote(philemon, paraToDelete, 10);
			sectionToDelete.AdjustReferences();
			// Add a section afterwards (part of its content will survive)
			IScrSection section3 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section3.Hvo,
				"The heading", ScrStyleNames.SectionHead);
			StTxtPara para3 = m_scrInMemoryCache.AddParaToMockedSectionContent(section3.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para3, "this is more text", null);
			StFootnote fn3 = m_scrInMemoryCache.AddFootnote(philemon, para3, 1);
			StFootnote fn4 = m_scrInMemoryCache.AddFootnote(philemon, para3, 10);
			ITsStrBldr bldrExpectedPContents = para3.Contents.UnderlyingTsString.GetBldr();
			bldrExpectedPContents.ReplaceTsString(1, 2, null);
			section3.AdjustReferences();

			m_editingHelper.SetupRangeSelection(philemon, 0, section1, 0, para1,
				0, 1, false, philemon, 0, section3, 2, para3, 0, 2, false);

			bool fRet = m_editingHelper.CallHandleComplexDeletion();

			Assert.IsTrue(fRet, "Deletion wasn't handled");
			Assert.AreEqual(1, philemon.FootnotesOS.HvoArray.Length);
			Assert.AreEqual(fn4.Hvo, philemon.FootnotesOS.HvoArray[0]);

			Assert.AreEqual(1, philemon.SectionsOS.Count);

			// Check contents of surviving section
			IScrSection section = philemon.SectionsOS[0];
			FdoOwningSequence<IStPara> paras = section.HeadingOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			AssertEx.AreTsStringsEqual(tssS1H1, ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
			paras = section.ContentOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			AssertEx.AreTsStringsEqual(bldrExpectedPContents.GetString(),
				((StTxtPara)paras[0]).Contents.UnderlyingTsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deleting from the middle of one section to the middle of another section where we
		/// have a footnote right outside of the selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeletePartOf2Sections_FootnotesOutside()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			// Add a section that remains
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara paraHeading = m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo,
				"The heading", ScrStyleNames.SectionHead);
			ITsString tssS1H1 = paraHeading.Contents.UnderlyingTsString;
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "this is more text", null);
			StFootnote fn1 = m_scrInMemoryCache.AddFootnote(philemon, para1, 1);
			StFootnote fn2 = m_scrInMemoryCache.AddFootnote(philemon, para1, 10);
			section1.AdjustReferences();
			// Add a section that will be deleted
			IScrSection sectionToDelete = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionToDelete.Hvo,
				"The heading will go", ScrStyleNames.SectionHead);
			StTxtPara paraToDelete = m_scrInMemoryCache.AddParaToMockedSectionContent(
				sectionToDelete.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraToDelete, "this will go away", null);
			m_scrInMemoryCache.AddFootnote(philemon, paraToDelete, 1);
			m_scrInMemoryCache.AddFootnote(philemon, paraToDelete, 10);
			sectionToDelete.AdjustReferences();
			// Add a section afterwards that remains
			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo,
				"The heading", ScrStyleNames.SectionHead);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "this is more text", null);
			StFootnote fn3 = m_scrInMemoryCache.AddFootnote(philemon, para2, 1);
			StFootnote fn4 = m_scrInMemoryCache.AddFootnote(philemon, para2, 10);
			section2.AdjustReferences();

			m_editingHelper.SetupRangeSelection(philemon, 0, section1, 0, para1,
				0, 2, false, philemon, 0, section2, 2, para2, 0, 1, false);

			bool fRet = m_editingHelper.CallHandleComplexDeletion();

			Assert.IsTrue(fRet, "Deletion wasn't handled");
			Assert.AreEqual(3, philemon.FootnotesOS.HvoArray.Length);
			Assert.AreEqual(fn1.Hvo, philemon.FootnotesOS.HvoArray[0]);
			Assert.AreEqual(fn3.Hvo, philemon.FootnotesOS.HvoArray[1]);
			Assert.AreEqual(fn4.Hvo, philemon.FootnotesOS.HvoArray[2]);

			Assert.AreEqual(1, philemon.SectionsOS.Count);

			// Check contents of surviving section
			IScrSection section = philemon.SectionsOS[0];
			FdoOwningSequence<IStPara> paras = section.HeadingOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			AssertEx.AreTsStringsEqual(tssS1H1, ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
			paras = section.ContentOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			Assert.AreEqual("this is more text".Length + 3, ((StTxtPara)paras[0]).Contents.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deleting from the end of one section to the start of the content of the next section
		/// where we have a footnote in the header.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteHeadingWithFootnote()
		{
			CheckDisposed();

			IScrBook philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			// Add a section that remains
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara paraHeading = m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo,
				"The heading", ScrStyleNames.SectionHead);
			ITsString tssS1H1 = paraHeading.Contents.UnderlyingTsString;
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "this is more text", null);
			StFootnote fn1 = m_scrInMemoryCache.AddFootnote(philemon, para1, 1);
			StFootnote fn2 = m_scrInMemoryCache.AddFootnote(philemon, para1, 10);
			ITsString tssS1P1 = para1.Contents.UnderlyingTsString;
			section1.AdjustReferences();
			// Add a section that will be deleted
			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(philemon.Hvo);
			StTxtPara headingPara = m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo,
				"The heading will go", ScrStyleNames.SectionHead);
			m_scrInMemoryCache.AddFootnote(philemon, headingPara, 5);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(
				section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "this will go away", null);
			StFootnote fn3 = m_scrInMemoryCache.AddFootnote(philemon, para2, 1);
			StFootnote fn4 = m_scrInMemoryCache.AddFootnote(philemon, para2, 10);
			ITsString tssS2P1 = para2.Contents.UnderlyingTsString;
			section2.AdjustReferences();

			m_editingHelper.SetupRangeSelection(philemon, 0, section1, 0, para1,
				0, para1.Contents.Length, false, philemon, 0, section2, 1, para2, 0, 0, false);

			bool fRet = m_editingHelper.CallHandleComplexDeletion();

			Assert.IsTrue(fRet, "Deletion wasn't handled");
			Assert.AreEqual(4, philemon.FootnotesOS.HvoArray.Length);
			Assert.AreEqual(fn1.Hvo, philemon.FootnotesOS.HvoArray[0]);
			Assert.AreEqual(fn2.Hvo, philemon.FootnotesOS.HvoArray[1]);
			Assert.AreEqual(fn3.Hvo, philemon.FootnotesOS.HvoArray[2]);
			Assert.AreEqual(fn4.Hvo, philemon.FootnotesOS.HvoArray[3]);

			Assert.AreEqual(1, philemon.SectionsOS.Count);

			// Check contents of surviving section
			IScrSection section = philemon.SectionsOS[0];
			FdoOwningSequence<IStPara> paras = section.HeadingOA.ParagraphsOS;
			Assert.AreEqual(1, paras.Count);
			AssertEx.AreTsStringsEqual(tssS1H1, ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
			paras = section.ContentOA.ParagraphsOS;
			//REVIEW: This part of the test should be reinstated when full solution to TE-6099 is implemented.
			//Assert.AreEqual(1, paras.Count);
			//ITsStrBldr bldrExpected = tssS1P1.GetBldr();
			//bldrExpected.ReplaceTsString(bldrExpected.Length, bldrExpected.Length, tssS2P1);
			//AssertEx.AreTsStringsEqual(bldrExpected.GetString(),
			//    ((StTxtPara)paras[0]).Contents.UnderlyingTsString);
		}
	}
	#endregion

	#region TeEditingHelperTestsWithoutCache
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Selection State tests
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeEditingHelperTestsWithoutCache : ScrInMemoryFdoTestBase
	{
		#region Data Members
		private DynamicMock m_mockSel;
		private DummyTeEditingHelper m_TeEditingHelper;
		#endregion

		#region Setup/teardown

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_scrInMemoryCache.InitializeScripture();

			m_TeEditingHelper = new DummyTeEditingHelper(Cache, null);
			m_mockSel = new DynamicMock(typeof(IVwSelection));
			m_mockSel.SetupResult("IsRange", true);
			m_mockSel.SetupResult("IsValid", true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_TeEditingHelper.Dispose();
			m_TeEditingHelper = null;
			m_mockSel = null;

			base.Exit();
		}
		#endregion

		#region RemoveHardFormatting tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests removing the hard formatting from text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveHardFormatting()
		{
			CheckDisposed();
			// build the original string
			ITsStrBldr tssBldr = TsStrBldrClass.Create();
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptBorderColor, 0, 0x00ff00cc);
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptSuperscript, 0, 5);
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, 711);
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "My Style");
			tssBldr.Replace(0, 0, "This", propsBldr.GetTextProps());
			propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, 711);
			tssBldr.Replace(tssBldr.Length, tssBldr.Length, " is my ", propsBldr.GetTextProps());
			propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, 711);
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptBold, 0, 1);
			tssBldr.Replace(tssBldr.Length, tssBldr.Length, "text.", propsBldr.GetTextProps());

			FwPasteFixTssEventArgs args = new FwPasteFixTssEventArgs(tssBldr.GetString());
			m_TeEditingHelper.RemoveHardFormatting(m_TeEditingHelper, args);

			// build what we expect it to return
			tssBldr = TsStrBldrClass.Create();
			propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, 711);
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "My Style");
			tssBldr.Replace(0, 0, "This", propsBldr.GetTextProps());
			propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, 711);
			tssBldr.Replace(tssBldr.Length, tssBldr.Length, " is my ", propsBldr.GetTextProps());
			propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, 711);
			tssBldr.Replace(tssBldr.Length, tssBldr.Length, "text.", propsBldr.GetTextProps());

			AssertEx.AreTsStringsEqual(tssBldr.GetString(), args.TsString);
		}
		#endregion

		#region RemoveCharStyles tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test removing character styles when a run contains a verse number.
		/// The verse number style should remain.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveCharStylesWithVerseNumber()
		{
			CheckDisposed();

			// build an array of string props with style names.
			ITsTextProps[] props = new ITsTextProps[3];
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "First Style");
			props[0] = bldr.GetTextProps();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse Number");
			props[1] = bldr.GetTextProps();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Last Style");
			props[2] = bldr.GetTextProps();

			// remove char formatting should remove the first and last names, but leave the
			// second one as a verse number.
			m_TeEditingHelper.RemoveCharFormatting((IVwSelection)m_mockSel.MockInstance,
				ref props, null);

			Assert.IsNull(props[0].GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual("Verse Number",
				props[1].GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.IsNull(props[2].GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test removing character styles when a run contains a chapter number.
		/// The chapter number style should remain.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveCharStylesWithChapterNumber()
		{
			CheckDisposed();

			// build an array of string props with style names.
			ITsTextProps[] props = new ITsTextProps[3];
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "First Style");
			props[0] = bldr.GetTextProps();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Chapter Number");
			props[1] = bldr.GetTextProps();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Last Style");
			props[2] = bldr.GetTextProps();

			// remove char formatting should remove the first and last names, but leave the
			// second one as a verse number.
			m_TeEditingHelper.RemoveCharFormatting((IVwSelection)m_mockSel.MockInstance,
				ref props, null);
			Assert.IsNull(props[0].GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual("Chapter Number",
				props[1].GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.IsNull(props[2].GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test removing character styles when a run contains only a verse number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveCharStylesWithVerseNumberOnly()
		{
			CheckDisposed();

			// build an array of string props with style names.
			ITsTextProps[] props = new ITsTextProps[1];
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Verse Number");
			props[0] = bldr.GetTextProps();

			m_TeEditingHelper.RemoveCharFormatting((IVwSelection)m_mockSel.MockInstance,
				ref props, null);
			Assert.IsNull(props[0].GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test removing character styles when a run contains only a chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveCharStylesWithChapterNumberOnly()
		{
			CheckDisposed();

			// build an array of string props with style names.
			ITsTextProps[] props = new ITsTextProps[1];
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Chapter Number");
			props[0] = bldr.GetTextProps();

			m_TeEditingHelper.RemoveCharFormatting((IVwSelection)m_mockSel.MockInstance, ref props, null);
			Assert.IsNull(props[0].GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}
		#endregion

		#region AtEndOfSection tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AtEndOfSection method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtEndOfSection_WhenNoSelection()
		{
			CheckDisposed();

			m_TeEditingHelper.SelectionForTesting = null;
			Assert.IsFalse(m_TeEditingHelper.AtEndOfSection);
		}
		// Other tests for this property require a cache and thus are performed elsewhere
		#endregion

		#region ScriptureCanImmediatelyFollowCurrentSection tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the ScriptureCanImmediatelyFollowCurrentSection property is false when
		/// there is no selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScriptureCanImmediatelyFollowCurrentSection_WhenNoSelection()
		{
			CheckDisposed();

			m_TeEditingHelper.SelectionForTesting = null;
			Assert.IsFalse(m_TeEditingHelper.ScriptureCanImmediatelyFollowCurrentSection);
		}
		#endregion

		#region AtBeginningOfFirstScriptureSection tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the AtBeginningOfFirstScriptureSection property is false when there is no
		/// selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AtBeginningOfFirstScriptureSection_WhenNoSelection()
		{
			CheckDisposed();

			m_TeEditingHelper.SelectionForTesting = null;
			Assert.IsFalse(m_TeEditingHelper.AtBeginningOfFirstScriptureSection,
				"AtBeginningOfFirstScriptureSection should be false when there is no selection.");
		}
		#endregion

		#region CanInsertNumberInElement tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the CanInsertNumberInElement property returns true at the start of
		/// Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CanInsertNumberInElement_TrueAtStartOfScripture()
		{
			CheckDisposed();

			// Simulate IP in section content
			// TODO (TE-2740): Don't allow numbers to be inserted in intro material
			SelLevInfo[] levInfo = new SelLevInfo[2];
			levInfo[0].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfo[0].hvo = Cache.LangProject.Hvo; // arbitrary HVO to prevent assert in production code
			SelectionHelper helper = new SelectionHelper();
			helper.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfo);
			m_TeEditingHelper.SelectionForTesting = helper;

			Assert.IsTrue(m_TeEditingHelper.CanInsertNumberInElement,
				"CanInsertNumberInElement should be true at the start of Scripture.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the CanInsertNumberInElement property returns false when
		/// the selection is in the heading of a section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CanInsertNumberInElement_FalseInSectionHeading()
		{
			CheckDisposed();

			// Simulate IP in a section heading
			SelLevInfo[] levInfo = new SelLevInfo[2];
			levInfo[0].tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			levInfo[0].hvo = Cache.LangProject.Hvo; // arbitrary HVO to prevent assert in production code
			SelectionHelper helper = new SelectionHelper();
			helper.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfo);
			m_TeEditingHelper.SelectionForTesting = helper;

			Assert.IsFalse(m_TeEditingHelper.CanInsertNumberInElement,
				"CanInsertNumberInElement should be false in a section heading");
		}
		#endregion

		#region IsNumber test
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsNumber when the parameter is null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsNumber_Null()
		{
			Assert.IsFalse(ReflectionHelper.GetBoolResult(typeof(TeEditingHelper), "IsNumber",
				(string)null));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsNumber when the parameter is a string containing only a number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsNumber_OnlyNumber()
		{
			Assert.IsTrue(ReflectionHelper.GetBoolResult(typeof(TeEditingHelper), "IsNumber",
				"345"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsNumber when the parameter is a string containing a number followed by
		/// some non-numeric characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsNumber_NumberPlusWord()
		{
			Assert.IsFalse(ReflectionHelper.GetBoolResult(typeof(TeEditingHelper), "IsNumber",
				"345Word"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsNumber when the parameter is an alpha string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsNumber_NonNumber()
		{
			Assert.IsFalse(ReflectionHelper.GetBoolResult(typeof(TeEditingHelper), "IsNumber",
				"Tim"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsNumber when the parameter is an empty string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsNumber_EmptyString()
		{
			Assert.IsFalse(ReflectionHelper.GetBoolResult(typeof(TeEditingHelper), "IsNumber",
				String.Empty));
		}
		#endregion
	}
	#endregion

	#region TeEditingHelper tests with mocked FDO cache
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// More unit tests for <see cref="TeEditingHelper">TeEditingHelper</see> that use
	/// <see cref="DummyBasicView">DummyBasicView</see> view to perform tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeEditingHelperTestsWithMockedFdoCache : BasicViewTestsBase
	{
		#region Dummy TE view and editing helper
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class DummyTeEditingHelper: TeEditingHelper
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="callbacks"></param>
			/// <param name="cache"></param>
			/// <param name="filterInstance"></param>
			/// <param name="viewType"></param>
			/// --------------------------------------------------------------------------------
			public DummyTeEditingHelper(IEditingCallbacks callbacks, FdoCache cache,
				int filterInstance, TeViewType viewType)
				: base(callbacks, cache, filterInstance, viewType)
			{
				InTestMode = true;
			}

			/// -----------------------------------------------------------------------------------
			/// <summary>
			/// Overriden so that it works if we don't display the view
			/// </summary>
			/// <returns>Returns <c>true</c> if pasting is possible.</returns>
			/// -----------------------------------------------------------------------------------
			public override bool CanPaste()
			{
				CheckDisposed();

				bool fVisible = Control.Visible;
				Control.Visible = true;
				bool fReturn = base.CanPaste();
				Control.Visible = fVisible;
				return fReturn;
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public class DummyTeBasicView: DummyBasicView, ITeView
		{
			private LocationTrackerImpl m_locationTracker;

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// --------------------------------------------------------------------------------
			public DummyTeBasicView(): base()
			{
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Helper used for processing editing requests.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public override EditingHelper EditingHelper
			{
				get
				{
					CheckDisposed();

					if (m_editingHelper == null)
					{
						m_editingHelper = new DummyTeEditingHelper(this, m_fdoCache,
							0, TeViewType.DraftView | TeViewType.Scripture);
						if (FilteredScrBooks.GetFilterInstance(m_fdoCache, 0) == null)
							new FilteredScrBooks(m_fdoCache, 0);
						m_locationTracker = new LocationTrackerImpl(m_fdoCache, 0);
					}
					return m_editingHelper;
				}
			}

			#region ITeView Members
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the location tracker.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public ILocationTracker LocationTracker
			{
				get { return m_locationTracker; }
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Gets a value indicating whether the USFM resource viewer is visible for this view.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public RegistryBoolSetting UsfmResourceViewerVisible
			{
				get { return new RegistryBoolSetting("test", false); }
			}

			#endregion
		}
		#endregion

		#region Data members
		private int m_hvoSection;
		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override DummyBasicView CreateDummyBasicView()
		{
			return new DummyTeBasicView();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a book and section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_inMemoryCache.InitializeWritingSystemEncodings();

			m_flidContainingTexts = (int)ScrBook.ScrBookTags.kflidSections;

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Philemon");
			m_hvoRoot = book.Hvo;

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_hvoSection = section.Hvo;
			section.AdjustReferences();

			m_frag = (int)ScrBook.ScrBookTags.kflidSections;
		}
		#endregion

		#region PasteClipboard tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-3130: Tests that pasting next to a chapter number removes the chapter number
		/// style from the pasted text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PasteClipboard_NextToChapterNumber()
		{
			CheckDisposed();

			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_hvoSection,
				ScrStyleNames.NormalParagraph);

			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			int hvoText = (int)Cache.MainCacheAccessor.get_Prop(m_hvoSection,
				(int)ScrSection.ScrSectionTags.kflidContent);

			ShowForm(DummyBasicViewVc.DisplayType.kNormal);

			IVwRootBox rootBox = m_basicView.RootBox;

			// Make a selection at the end of the paragraph
			SelLevInfo[] levelInfo = new SelLevInfo[3];
			levelInfo[2].tag = m_flidContainingTexts;
			levelInfo[2].cpropPrevious = 0;
			levelInfo[2].ihvo = 0;
			levelInfo[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			rootBox.MakeTextSelection(0, 3, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, 1, 1, 0, true, -1, null,
				true);

			// Put something on the clipboard
			Clipboard.SetDataObject("Some text");

			m_basicView.EditingHelper.PasteClipboard(false);

			Assert.AreEqual("1Some text", para.Contents.Text);

			//  Verify the detailed tss results
			int ws = Cache.DefaultVernWs;

			ITsString tssResult = para.Contents.UnderlyingTsString;

			Assert.AreEqual(2, tssResult.RunCount, "Unexpected number of runs");
			AssertEx.RunIsCorrect(tssResult, 0, "1", "Chapter Number", ws);
			AssertEx.RunIsCorrect(tssResult, 1, "Some text", null, ws);
		}
		#endregion

		#region InsertBook tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the inserting of a scripture book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertBookTest_AfterEverything()
		{
			CheckDisposed();

			ShowForm(DummyBasicViewVc.DisplayType.kNormal);

			// Insert Revelation
			IScrBook rev = ((TeEditingHelper)m_basicView.EditingHelper).InsertBook(66);

			Assert.IsNotNull(rev, "new book wasn't inserted.");
			Assert.AreEqual("REV", rev.BookId);
			Assert.AreEqual(1, rev.SectionsOS.Count, "Incorrect number of sections");
			Assert.AreEqual(rev.Hvo, m_scr.ScriptureBooksOS.HvoArray[1]);

			IStText title = rev.TitleOA;
			FdoOwningSequence<IStPara> titleParas = title.ParagraphsOS;
			Assert.AreEqual(1, titleParas.Count, "The title should consist of 1 para");

			// Verify the main title's text and paragraph style is correct.
			Assert.IsNull(((StTxtPara)titleParas[0]).Contents.Text, "Incorrect book title");
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.MainBookTitle),
				((StTxtPara)titleParas[0]).StyleRules);

		}
		#endregion
	}
	#endregion
}
