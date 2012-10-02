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
// File: FootnoteViewTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.TE;

namespace SIL.FieldWorks.AcceptanceTests.TE
{

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for FootnoteViewTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FootnoteViewTests
	{
		private DummyFootnoteViewForm m_footnoteForm;
		private DummyFootnoteView m_footnoteView;
		private FdoCache m_cache;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new footnote view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			m_footnoteForm = new DummyFootnoteViewForm();
			m_footnoteForm.DeleteRegistryKey();
			m_footnoteForm.CreateFootnoteView();

			//Application.DoEvents();
			m_footnoteForm.Show();
			m_footnoteView = m_footnoteForm.FootnoteView;
			m_cache = m_footnoteForm.Cache;
			IScripture scr = m_cache.LangProject.TranslatedScriptureOA;
			Application.DoEvents();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the footnote view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			if (m_cache != null)
			{
				if (m_cache.CanUndo)
					m_cache.Undo();
				if (m_cache.DatabaseAccessor.IsTransactionOpen())
					m_cache.DatabaseAccessor.RollbackTrans();
			}
			else
			{
				Debug.WriteLine("Null cache in cleanup, something went wrong.");
			}
			m_cache = null;
			m_footnoteView = null;
			m_footnoteForm.Close();
			m_footnoteForm = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifys that the footnote marker was deleted
		/// </summary>
		/// <param name="hvoPara">HVO of the paragraph that contains the footnote marker</param>
		/// <param name="guidFootnote">GUID of the deleted footnote</param>
		/// ------------------------------------------------------------------------------------
		private bool IsFootnoteMarkerInText(int hvoPara, Guid guidFootnote)
		{
			StTxtPara para = new StTxtPara(m_cache, hvoPara);
			ITsString tssContents = para.Contents.UnderlyingTsString;
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
			anchorLevInfo[1].tag = (int)ScrBook.ScrBookTags.kflidFootnotes;
			anchorLevInfo[1].ihvo = 31;
			anchorLevInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			anchorLevInfo[0].ihvo = 0;
			selHelper.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, anchorLevInfo);
			selHelper.IchAnchor = 1;

			SelLevInfo[] endLevInfo = new SelLevInfo[3];
			endLevInfo[2].tag = m_footnoteView.BookFilter.Tag;
			endLevInfo[2].ihvo = 2;
			endLevInfo[1].tag = (int)ScrBook.ScrBookTags.kflidFootnotes;
			endLevInfo[1].ihvo = 1;
			endLevInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
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
			IScripture scr = m_cache.LangProject.TranslatedScriptureOA;

			ScrFootnote[] footnotes = new ScrFootnote[4];
			Guid[] guidFootnotes = new Guid[4];
			int[] hvoParas = new int[4];

			// First get the footnotes we're deleting from JAMES.
			IScrBook book = (IScrBook)scr.ScriptureBooksOS[1];
			footnotes[0] = new ScrFootnote(m_cache, book.FootnotesOS.HvoArray[31]);
			footnotes[1] = new ScrFootnote(m_cache, book.FootnotesOS.HvoArray[32]);

			// First get the footnotes we're deleting from JUDE.
			book = (IScrBook)scr.ScriptureBooksOS[2];
			footnotes[2] = new ScrFootnote(m_cache, book.FootnotesOS.HvoArray[0]);
			footnotes[3] = new ScrFootnote(m_cache, book.FootnotesOS.HvoArray[1]);

			for (int i = 0; i < 4; i++)
			{
				guidFootnotes[i] = m_cache.GetGuidFromId(footnotes[i].Hvo);
				hvoParas[i] = footnotes[i].ContainingParagraphHvo;
			}

			m_footnoteView.DeleteFootnote();

			foreach (IStFootnote footnote in footnotes)
				Assert.IsFalse(m_cache.IsRealObject(footnote.Hvo, StFootnote.kClassId));

			// now make sure that we don't find the footnote markers
			for (int i = 0; i < 4; i++)
			{
				Assert.IsFalse(IsFootnoteMarkerInText(hvoParas[i], guidFootnotes[i]),
					"Footnote marker didn't get deleted from text");
			}
		}
	}
}
