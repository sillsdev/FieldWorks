// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using FieldWorks.TestUtilities;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Base class for tests that use <see cref="DummyBasicView"/>. This class is specific for
	/// Rootsite tests.
	/// </summary>
	public class RootsiteBasicViewTestsBase : BasicViewTestsBase
	{
		/// <summary>Text for the first and third test paragraph (French)</summary>
		internal const string kFirstParaFra = "C'est une paragraph en francais.";
		/// <summary>Text for the second and fourth test paragraph (French).</summary>
		/// <remarks>This text needs to be shorter than the text for the first para!</remarks>
		internal const string kSecondParaFra = "C'est une deuxieme paragraph.";
		/// <summary>Writing System Factory (reset for each test since the cache gets re-created</summary>
		protected ILgWritingSystemFactory m_wsf;
		/// <summary>Id of English Writing System(reset for each test since the cache gets re-created</summary>
		protected int m_wsEng;
		/// <summary />
		protected IScrBook m_book;

		/// <summary />
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_flidContainingTexts = ScrBookTags.kflidFootnotes;
			m_wsf = Cache.WritingSystemFactory;
			m_wsEng = m_wsf.GetWsFromStr("en");
		}

		/// <summary>
		/// Creates the test data.
		/// </summary>
		protected override void CreateTestData()
		{
			m_book = AddArchiveBookToMockedScripture(1, "GEN");
			m_hvoRoot = m_book.Hvo;
		}

		/// <summary>
		/// Insert the specified paragraphs and show the dialog
		/// </summary>
		protected void ShowForm(TestLanguages lng, DisplayType display)
		{
			MakeParagraphs(lng);
			base.ShowForm(display);
		}

		/// <summary>
		/// Insert the specified paragraphs and show the dialog
		/// </summary>
		protected void ShowForm(TestLanguages lng, DisplayType display, int height)
		{
			MakeParagraphs(lng);

			base.ShowForm(display, height);
		}

		private void MakeParagraphs(TestLanguages lng)
		{
			if ((lng & TestLanguages.English) == TestLanguages.English)
			{
				MakeEnglishParagraphs();
			}
			if ((lng & TestLanguages.French) == TestLanguages.French)
			{
				MakeFrenchParagraphs();
			}
			if ((lng & TestLanguages.UserWs) == TestLanguages.UserWs)
			{
				MakeUserWsParagraphs();
			}
			if ((lng & TestLanguages.Empty) == TestLanguages.Empty)
			{
				MakeEmptyParagraphs();
			}
			if ((lng & TestLanguages.Mixed) == TestLanguages.Mixed)
			{
				MakeMixedWsParagraph();
			}
		}

		/// <summary>
		/// Add English paragraphs
		/// </summary>
		protected void MakeEnglishParagraphs()
		{
			AddParagraphs(m_wsEng, DummyBasicView.kFirstParaEng, DummyBasicView.kSecondParaEng);
		}

		/// <summary>
		/// Add French paragraphs
		/// </summary>
		protected void MakeFrenchParagraphs()
		{
			AddParagraphs(m_wsf.GetWsFromStr("fr"), kFirstParaFra, kSecondParaFra);
		}

		/// <summary>
		/// Add paragraphs with the user interface writing system
		/// </summary>
		protected void MakeUserWsParagraphs()
		{
			AddParagraphs(m_wsf.UserWs, "blabla", "abc");
		}

		/// <summary>
		/// Makes a paragraph containing runs, each of which has a different writing system.
		/// </summary>
		protected void MakeMixedWsParagraph()
		{
			var para = AddParagraph();

			AddRunToMockedPara(para, "ws1", m_wsEng);
			AddRunToMockedPara(para, "ws2", m_wsf.GetWsFromStr("de"));
			AddRunToMockedPara(para, "ws3", m_wsf.GetWsFromStr("fr"));
		}

		/// <summary>
		/// Add empty paragraphs
		/// </summary>
		protected void MakeEmptyParagraphs()
		{
			AddParagraphs(m_wsf.UserWs, string.Empty, string.Empty);
		}

		/// <summary>
		/// Adds a paragraph to the database
		/// </summary>
		protected IStTxtPara AddParagraph()
		{
			var text = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().Create();
			m_book.FootnotesOS.Add(text);

			return AddParaToMockedText(text, "TestStyle");
		}

		/// <summary>
		/// Adds paragraphs to the database
		/// </summary>
		private void AddParagraphs(int ws, string firstPara, string secondPara)
		{
			var para1 = AddParagraph();
			var para2 = AddParagraph();
			AddRunToMockedPara(para1, firstPara, ws);
			AddRunToMockedPara(para2, secondPara, ws);
		}
	}
}