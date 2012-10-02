// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2004' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DraftViewTestsWithNoView.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE.DraftViews
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for DraftViewTestsWithNoView.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class InsertDiffParasTests : DraftViewTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph()
		{
			ITsTextProps ttp = StyleUtils.ParaStyleTextProps("Paragraph"); // source and dest

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, ttp, 1,
				new ITsTextProps[]{ttp}, new ITsString[]{null}, null);
			Assert.AreEqual(VwInsertDiffParaResponse.kidprDefault, resp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't think this can ever happen in TE
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TitleMainIntoNullDest()
		{
			ITsTextProps ttpSrc = StyleUtils.ParaStyleTextProps("Title Main");

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, null, 1,
				new ITsTextProps[]{ttpSrc}, new ITsString[]{null}, null);
			Assert.AreEqual(VwInsertDiffParaResponse.kidprFail, resp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't think this can ever happen in TE
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParagraphAndTitleMainIntoNullDest()
		{
			ITsTextProps ttpSrc1 = StyleUtils.ParaStyleTextProps("Paragraph");
			ITsTextProps ttpSrc2 = StyleUtils.ParaStyleTextProps("Title Main");

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, null, 2,
				new ITsTextProps[]{ttpSrc1, ttpSrc2}, new ITsString[]{null, null}, null);
			Assert.AreEqual(VwInsertDiffParaResponse.kidprFail, resp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CitationLine1IntoParagraph()
		{
			ITsTextProps ttpDst = StyleUtils.ParaStyleTextProps("Paragraph");
			ITsTextProps ttpSrc2 = StyleUtils.ParaStyleTextProps("Citation Line1");

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, ttpDst, 2,
				new ITsTextProps[]{ttpDst, ttpSrc2}, new ITsString[]{null, null}, null);
			Assert.AreEqual(VwInsertDiffParaResponse.kidprDefault, resp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't think this can ever happen in TE
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TitleSecondaryIntoNullDest()
		{
			ITsTextProps ttpSrc = StyleUtils.CharStyleTextProps("Title Secondary", 1);

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, null, 1,
				new ITsTextProps[]{ttpSrc}, new ITsString[]{null}, null);
			Assert.AreEqual(VwInsertDiffParaResponse.kidprFail, resp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We're not sure what this test was originally intended to prove, but in TE there's
		/// no obvious way the Title Secondary character style could be pasted as if it were a
		/// paragraph style. This could happen if someone defined Title Secondary as a para
		/// style in FLEx and tried to paste text from there.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NonScrParaStyleIntoTitleMain()
		{
			ITsTextProps ttpSrc = StyleUtils.ParaStyleTextProps("Title Secondary");
			ITsTextProps ttpDst = StyleUtils.ParaStyleTextProps("Title Main");

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, ttpDst, 1,
				new ITsTextProps[]{ttpSrc}, new ITsString[]{null}, null);
			Assert.AreEqual(VwInsertDiffParaResponse.kidprFail, resp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionHeadIntoSectionHeadMajor()
		{
			ITsTextProps ttpSrc = StyleUtils.ParaStyleTextProps("Section Head");
			ITsTextProps ttpDst = StyleUtils.ParaStyleTextProps("Section Head Major");

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, ttpDst, 1,
				new ITsTextProps[]{ttpSrc}, new ITsString[]{null}, null);
			Assert.AreEqual(VwInsertDiffParaResponse.kidprDefault, resp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IntroParagraphIntoLine2()
		{
			ITsTextProps ttpSrc = StyleUtils.ParaStyleTextProps("Intro Paragraph");
			ITsTextProps ttpDst = StyleUtils.ParaStyleTextProps("Line2");

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, ttpDst, 1,
				new ITsTextProps[]{ttpSrc}, new ITsString[]{null}, null);
			Assert.AreEqual(VwInsertDiffParaResponse.kidprFail, resp);
		}
	}
}
