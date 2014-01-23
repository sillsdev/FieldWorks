// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FootnoteViewTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
//using SIL.FieldWorks.FDO.Cellar;
//using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
//using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.TE;
using SIL.Utils;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.AcceptanceTests.TE
{

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for FootnoteViewTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FootnoteViewTests: ScrInMemoryFdoTestBase
	{
		private DummyFootnoteViewForm m_footnoteForm;
		private DummyFootnoteView m_footnoteView;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new footnote view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_footnoteForm = new DummyFootnoteViewForm();
			m_footnoteForm.DeleteRegistryKey();
			m_footnoteForm.CreateFootnoteView(Cache);

			//Application.DoEvents();
			m_footnoteForm.Show();
			m_footnoteView = m_footnoteForm.FootnoteView;
			Application.DoEvents();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the footnote view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_footnoteView = null;
			m_footnoteForm.Close();
			m_footnoteForm = null;
			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifys that the footnote marker was deleted
		/// </summary>
		/// <param name="hvoPara">HVO of the paragraph that contains the footnote marker</param>
		/// <param name="guidFootnote">GUID of the deleted footnote</param>
		/// ------------------------------------------------------------------------------------
		private bool IsFootnoteMarkerInText(IScrTxtPara para, Guid guidFootnote)
		{
			ITsString tssContents = para.Contents;
			for (int i = 0; i < tssContents.RunCount; i++)
			{
				ITsTextProps tprops;
				TsRunInfo runInfo;
				tprops = tssContents.FetchRunInfo(i, out runInfo);
				string strGuid =
					tprops.GetStrPropValue((int)FwTextPropType.ktptObjData);
				if (strGuid != null)
				{
					Guid guid = MiscUtils.GetGuidFromObjData(strGuid.Substring(1));
					if (guid == guidFootnote)
						return true;
				}
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupSelectionForRangeAcrossBooks()
		{
			SelectionHelper selHelper = new SelectionHelper();
			selHelper.AssocPrev = true;
			selHelper.NumberOfLevels = 3;

			SelLevInfo[] anchorLevInfo = new SelLevInfo[3];
			anchorLevInfo[2].tag = m_footnoteView.BookFilter.Tag;
			anchorLevInfo[2].ihvo = 1;
			anchorLevInfo[1].tag = ScrBookTags.kflidFootnotes;
			anchorLevInfo[1].ihvo = 31;
			anchorLevInfo[0].tag = StTextTags.kflidParagraphs;
			anchorLevInfo[0].ihvo = 0;
			selHelper.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, anchorLevInfo);
			selHelper.IchAnchor = 1;

			SelLevInfo[] endLevInfo = new SelLevInfo[3];
			endLevInfo[2].tag = m_footnoteView.BookFilter.Tag;
			endLevInfo[2].ihvo = 2;
			endLevInfo[1].tag = ScrBookTags.kflidFootnotes;
			endLevInfo[1].ihvo = 1;
			endLevInfo[0].tag = StTextTags.kflidParagraphs;
			endLevInfo[0].ihvo = 0;
			selHelper.SetLevelInfo(SelectionHelper.SelLimitType.End, endLevInfo);
			selHelper.IchEnd = 3;

			// Now that all the preparation to set the selection is done, set it.
			selHelper.SetSelection(m_footnoteView, true, true);
			Application.DoEvents();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure that selecting "Delete Footnote" deletes the footnote reference and
		/// underlaying footnote. This tests the case where we have a range selection
		/// containing footnotes from two books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteFootnoteInRangeSelectionAcrossMultipleBooks()
		{
			SetupSelectionForRangeAcrossBooks();
			IScripture scr = Cache.LangProject.TranslatedScriptureOA;

			IScrFootnote[] footnotes = new IScrFootnote[4];
			Guid[] guidFootnotes = new Guid[4];
			IScrTxtPara[] paras = new IScrTxtPara[4];

			// First get the footnotes we're deleting from JAMES.
			IScrBook book = scr.ScriptureBooksOS[1];
			footnotes[0] = book.FootnotesOS[31];
			footnotes[1] = book.FootnotesOS[32];

			// First get the footnotes we're deleting from JUDE.
			book = (IScrBook)scr.ScriptureBooksOS[2];
			footnotes[2] = book.FootnotesOS[0];
			footnotes[3] = book.FootnotesOS[1];

			for (int i = 0; i < 4; i++)
			{
				guidFootnotes[i] = footnotes[i].Guid;
				paras[i] = footnotes[i].ContainingParagraph;
			}

			m_footnoteView.DeleteFootnote();

			foreach (IScrFootnote footnote in footnotes)
				Assert.IsFalse(footnote.IsValidObject);

			// now make sure that we don't find the footnote markers
			for (int i = 0; i < 4; i++)
			{
				Assert.IsFalse(IsFootnoteMarkerInText(paras[i], guidFootnotes[i]),
					"Footnote marker didn't get deleted from text");
			}
		}
	}
}
