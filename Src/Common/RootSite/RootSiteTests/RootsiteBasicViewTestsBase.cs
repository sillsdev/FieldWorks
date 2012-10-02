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
// File: RootsiteBasicViewTestsBase.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for tests that use <see cref="DummyBasicView"/>. This class is specific for
	/// Rootsite tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RootsiteBasicViewTestsBase : BasicViewTestsBase
	{
		/// <summary>Defines the possible languages</summary>
		[Flags]
		public enum Lng
		{
			/// <summary>No paragraphs</summary>
			None = 0,
			/// <summary>English paragraphs</summary>
			English = 1,
			/// <summary>French paragraphs</summary>
			French = 2,
			/// <summary>UserWs paragraphs</summary>
			UserWs = 4,
			/// <summary>Empty paragraphs</summary>
			Empty = 8,
			/// <summary>Paragraph with 3 writing systems</summary>
			Mixed = 16,
		}

		/// <summary>Text for the first and third test paragraph (French)</summary>
		internal const string kFirstParaFra = "C'est une paragraph en francais.";
		/// <summary>Text for the second and fourth test paragraph (French).</summary>
		/// <remarks>This text needs to be shorter than the text for the first para!</remarks>
		internal const string kSecondParaFra = "C'est une deuxieme paragraph.";

		/// <summary>Writing System Factory (reset for each test since the cache gets re-created</summary>
		protected ILgWritingSystemFactory m_wsf;
		/// <summary>Id of English Writing System(reset for each test since the cache gets re-created</summary>
		protected int m_wsEng;
		/// <summary></summary>
		protected IScrBook m_book;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixture setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_flidContainingTexts = ScrBookTags.kflidFootnotes;
			m_wsf = Cache.WritingSystemFactory;
			m_wsEng = m_wsf.GetWsFromStr("en");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the test data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_book = AddArchiveBookToMockedScripture(1, "GEN");
			m_hvoRoot = m_book.Hvo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert the specified paragraphs and show the dialog
		/// </summary>
		/// <param name="lng">Language</param>
		/// <param name="display"></param>
		/// ------------------------------------------------------------------------------------
		protected void ShowForm(Lng lng, DummyBasicViewVc.DisplayType display)
		{
			MakeParagraphs(lng);
			base.ShowForm(display);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert the specified paragraphs and show the dialog
		/// </summary>
		/// <param name="lng">Language</param>
		/// <param name="display"></param>
		/// <param name="height"></param>
		/// ------------------------------------------------------------------------------------
		protected void ShowForm(Lng lng, DummyBasicViewVc.DisplayType display, int height)
		{
			MakeParagraphs(lng);

			base.ShowForm(display, height);
		}

		private void MakeParagraphs(Lng lng)
		{
			if ((lng & Lng.English) == Lng.English)
				MakeEnglishParagraphs();
			if ((lng & Lng.French) == Lng.French)
				MakeFrenchParagraphs();
			if ((lng & Lng.UserWs) == Lng.UserWs)
				MakeUserWsParagraphs();
			if ((lng & Lng.Empty) == Lng.Empty)
				MakeEmptyParagraphs();
			if ((lng & Lng.Mixed) == Lng.Mixed)
				MakeMixedWsParagraph();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add English paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeEnglishParagraphs()
		{
			AddParagraphs(m_wsEng, DummyBasicView.kFirstParaEng, DummyBasicView.kSecondParaEng);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add French paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeFrenchParagraphs()
		{
			int wsFrn = m_wsf.GetWsFromStr("fr");
			AddParagraphs(wsFrn, kFirstParaFra, kSecondParaFra);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add paragraphs with the user interface writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeUserWsParagraphs()
		{
			int ws = m_wsf.UserWs;
			AddParagraphs(ws, "blabla", "abc");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a paragraph containing runs, each of which has a different writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeMixedWsParagraph()
		{
			var para = AddParagraph();

			AddRunToMockedPara(para, "ws1", m_wsEng);
			AddRunToMockedPara(para, "ws2", m_wsf.GetWsFromStr("de"));
			AddRunToMockedPara(para, "ws3", m_wsf.GetWsFromStr("fr"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add empty paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeEmptyParagraphs()
		{
			int ws = m_wsf.UserWs;
			AddParagraphs(ws, "", "");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a paragraph to the database
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected IStTxtPara AddParagraph()
		{
			var text = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().Create();
			m_book.FootnotesOS.Add(text);

			return AddParaToMockedText(text, "TestStyle");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds paragraphs to the database
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="firstPara"></param>
		/// <param name="secondPara"></param>
		/// ------------------------------------------------------------------------------------
		private void AddParagraphs(int ws, string firstPara, string secondPara)
		{
			var para1 = AddParagraph();
			var para2 = AddParagraph();
			AddRunToMockedPara(para1, firstPara, ws);
			AddRunToMockedPara(para2, secondPara, ws);
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for tests that use <see cref="DummyBasicView"/>
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RootsiteDummyViewTestsBase : ScrInMemoryFdoTestBase
	{
		/// <summary>The draft form</summary>
		protected DummyBasicView m_basicView;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set up data that is constant for all tests in fixture
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				IScrBook book = AddBookToMockedScripture(1, "GEN");
				IStText title = AddTitleToMockedBook(book, "This is the title");
				AddFootnote(book, (IStTxtPara)title.ParagraphsOS[0], 0);
			});
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new basic view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			var styleSheet = new FwStyleSheet();

			styleSheet.Init(Cache, m_scr.Hvo,
				ScriptureTags.kflidStyles);

			Debug.Assert(m_basicView == null, "m_basicView is not null.");

			m_basicView = new DummyBasicView {Cache = Cache, Visible = false, StyleSheet = styleSheet};
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_basicView.Dispose();
			m_basicView = null;

			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert the specified paragraphs and show the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ShowForm()
		{
			m_basicView.DisplayType = DummyBasicViewVc.DisplayType.kBookTitle;
			// m_basicView.MakeEnglishParagraphs();

			// We don't actually want to show it, but we need to force the view to create the root
			// box and lay it out so that various test stuff can happen properly.
			m_basicView.Width = 300;
			m_basicView.Height = 307-25;
			m_basicView.MakeRoot(m_scr.ScriptureBooksOS[0].Hvo, ScrBookTags.kflidTitle);
			m_basicView.CallLayout();
		}
	}
}
