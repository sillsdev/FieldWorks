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
// File: DraftViewTestsWithNoView.cs
// Responsibility: Edge
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
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.TE.DraftViews
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for DraftViewTestsWithNoView.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class InsertDiffParasTests : TeTestBase
	{
		#region Data Members
		private DummyDraftView m_draftView;
		private DummyDraftViewForm m_draftForm;
		private IScrBook m_exodus;
		#endregion

		#region IDisposable override

		/// -------------------------------------------------------------------------------------
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
		/// -------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)

			{
				// Dispose managed resources here.
				if (m_draftForm != null)
					m_draftForm.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_draftView = null;
			m_draftForm = null;
			m_exodus = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		#region Setup and Teardown
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new draft view (without showing it)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_draftForm = new DummyDraftViewForm();
			m_draftForm.DeleteRegistryKey();
			m_draftForm.CreateDraftView(Cache);
			m_draftView = m_draftForm.DraftView;
			m_draftView.Width = 300;
			m_draftView.Height = 290;
			m_draftView.CallOnLayout();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_draftView = null;
			m_draftForm.Close();
			m_draftForm = null;

			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_exodus = CreateExodusData();
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Paragraph()
		{
			CheckDisposed();

			ITsString tssParas = null;
			ITsTextProps ttpSrc1 = StyleUtils.ParaStyleTextProps("Paragraph");
			ITsTextProps ttpSrc2 = StyleUtils.ParaStyleTextProps("Paragraph");

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, ttpSrc2, 1,
				new ITsTextProps[]{ttpSrc1}, new ITsString[]{tssParas}, null);
			Assert.AreEqual(VwInsertDiffParaResponse.kidprDefault, resp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TitleMain()
		{
			CheckDisposed();

			ITsString tssParas = null;
			ITsTextProps ttpSrc = StyleUtils.ParaStyleTextProps("Title Main");

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, null, 1,
				new ITsTextProps[]{ttpSrc}, new ITsString[]{tssParas}, null);
			Assert.AreEqual(VwInsertDiffParaResponse.kidprFail, resp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParagraphAndTitleMain()
		{
			CheckDisposed();

			ITsString tssParas = null;
			ITsTextProps ttpSrc1 = StyleUtils.ParaStyleTextProps("Paragraph");
			ITsTextProps ttpSrc2 = StyleUtils.ParaStyleTextProps("Title Main");

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, null, 2,
				new ITsTextProps[]{ttpSrc1, ttpSrc2}, new ITsString[]{tssParas, tssParas}, null);
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
			CheckDisposed();

			ITsString tssParas = null;
			ITsTextProps ttpSrc1 = StyleUtils.ParaStyleTextProps("Paragraph");
			ITsTextProps ttpSrc2 = StyleUtils.ParaStyleTextProps("Citation Line1");

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, ttpSrc1, 2,
				new ITsTextProps[]{ttpSrc1, ttpSrc2}, new ITsString[]{tssParas, tssParas}, null);
			Assert.AreEqual(VwInsertDiffParaResponse.kidprDefault, resp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TitleSecondary()
		{
			CheckDisposed();

			ITsString tssParas = null;
			ITsTextProps ttpSrc1 = StyleUtils.CharStyleTextProps("Title Secondary", 1);

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, null, 1,
				new ITsTextProps[]{ttpSrc1}, new ITsString[]{tssParas}, null);
			Assert.AreEqual(VwInsertDiffParaResponse.kidprFail, resp);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TitleSecondaryIntoTitleMain()
		{
			CheckDisposed();

			ITsString tssParas = null;
			ITsTextProps ttpSrc1 = StyleUtils.CharStyleTextProps("Title Secondary", 1);
			ITsTextProps ttpSrc2 = StyleUtils.CharStyleTextProps("Title Main", 1);

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, ttpSrc2, 1,
				new ITsTextProps[]{ttpSrc1}, new ITsString[]{tssParas}, null);
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
			CheckDisposed();

			ITsString tssParas = null;
			ITsTextProps ttpSrc1 = StyleUtils.CharStyleTextProps("Section Head", 1);
			ITsTextProps ttpSrc2 = StyleUtils.CharStyleTextProps("Section Head Major", 1);

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, ttpSrc2, 1,
				new ITsTextProps[]{ttpSrc1}, new ITsString[]{tssParas}, null);
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
			CheckDisposed();

			ITsString tssParas = null;
			ITsTextProps ttpSrc1 = StyleUtils.CharStyleTextProps("Intro Paragraph", 1);
			ITsTextProps ttpSrc2 = StyleUtils.CharStyleTextProps("Line2", 1);

			VwInsertDiffParaResponse resp = m_draftView.OnInsertDiffParas(
				m_draftView.RootBox, ttpSrc2, 1,
				new ITsTextProps[]{ttpSrc1}, new ITsString[]{tssParas}, null);
			Assert.AreEqual(VwInsertDiffParaResponse.kidprFail, resp);
		}
	}
}
