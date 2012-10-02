using System;


using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.PrintLayout
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// An in-memory (FDOBackendProviderType.kMemoryOnly) base class
	/// that supports PrintLayout tests by adding publications to the ScrInMemoryFdoTestBase.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PrintLayoutTestBase : ScrInMemoryFdoTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the Publication values for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			// the print layout tests are dependent on Times New Roman
			Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.DefaultFontName = "Times New Roman";
			Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.DefaultFontName = "Times New Roman";

			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, CreateStandardPublications);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the standard publications used in testing. Used the "Scripture Draft" settings
		/// in TePublications.xml as a guide.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateStandardPublications()
		{
			var pub = Cache.ServiceLocator.GetInstance<IPublicationFactory>().Create();
			m_scr.PublicationsOC.Add(pub);
			pub.Name = "Scripture Draft";
			pub.Description = StringUtils.MakeTss("Test Publication", Cache.DefaultAnalWs);
			pub.IsLandscape = false;
			pub.PageHeight = 0;
			pub.PageWidth = 0;
			pub.GutterMargin = 0;
			pub.BindingEdge = BindingSide.Left;
			pub.BaseFontSize = 12000;
			pub.BaseLineSpacing = 14000;
			var division = Cache.ServiceLocator.GetInstance<IPubDivisionFactory>().Create();
			pub.DivisionsOS.Add(division);
			division.StartAt = DivisionStartOption.NewPage;
			division.NumColumns = 1;
			var pageLayout = Cache.ServiceLocator.GetInstance<IPubPageLayoutFactory>().Create();
			division.PageLayoutOA = pageLayout;
			pageLayout.MarginInside = 72000;
			pageLayout.MarginOutside = 72000;
			pageLayout.MarginTop = 72000;
			pageLayout.MarginBottom = 72000;
			pageLayout.PosHeader = 54000;
			pageLayout.PosFooter = 54000;
			pageLayout.IsBuiltIn = true;
			pageLayout.IsModified = false;
			var hfSet = Cache.ServiceLocator.GetInstance<IPubHFSetFactory>().Create();
			division.HFSetOA = hfSet;
			hfSet.DefaultHeaderOA = Cache.ServiceLocator.GetInstance<IPubHeaderFactory>().Create();
			UpdateHeaderFooter(hfSet.DefaultHeaderOA, Guid.Empty, HeaderFooterVc.LastReferenceGuid, HeaderFooterVc.PageNumberGuid);
			hfSet.FirstHeaderOA = Cache.ServiceLocator.GetInstance<IPubHeaderFactory>().Create();
			UpdateHeaderFooter(hfSet.FirstHeaderOA, Guid.Empty, Guid.Empty, Guid.Empty);
			hfSet.FirstFooterOA = Cache.ServiceLocator.GetInstance<IPubHeaderFactory>().Create();
			UpdateHeaderFooter(hfSet.FirstFooterOA, Guid.Empty, HeaderFooterVc.PageNumberGuid, Guid.Empty);
			hfSet.EvenHeaderOA = Cache.ServiceLocator.GetInstance<IPubHeaderFactory>().Create();
			UpdateHeaderFooter(hfSet.EvenHeaderOA, Guid.Empty, HeaderFooterVc.FirstReferenceGuid, HeaderFooterVc.PageNumberGuid);
		}

		private void UpdateHeaderFooter(IPubHeader header, Guid insideGuid, Guid centerGuid, Guid outsideGuid)
		{
			if (insideGuid != Guid.Empty)
				header.InsideAlignedText =
					StringUtils.CreateOrcFromGuid(insideGuid,
						FwObjDataTypes.kodtContextString, Cache.DefaultUserWs);
			if (centerGuid != Guid.Empty)
				header.CenteredText =
					StringUtils.CreateOrcFromGuid(centerGuid,
						FwObjDataTypes.kodtContextString, Cache.DefaultUserWs);
			if (outsideGuid != Guid.Empty)
				header.OutsideAlignedText =
					StringUtils.CreateOrcFromGuid(outsideGuid,
						FwObjDataTypes.kodtContextString, Cache.DefaultUserWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book with 2 sections with the following layout:
		/// bookName
		/// Heading 1
		///   (1)1Verse one.
		/// Heading 2
		///   (2)1Verse one.2Verse two.
		///   (3)1Verse one.
		///   2This is a pretty long...
		///   ..
		///   11This is a pretty long...
		///
		///
		/// Numbers in () are chapter numbers.
		/// </summary>
		/// <returns>the book for testing</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrBook CreateBook(int nBookNumber, string bookName)
		{
			IScrBook book;
			IScrSection section;
			book = CreateSmallBook(nBookNumber, bookName, out section);

			for (int i = 0; i < 10; i++)
			{
				IStTxtPara para = AddParaToMockedSectionContent(section,
					ScrStyleNames.NormalParagraph);
				AddRunToMockedPara(para, (i + 2).ToString(), ScrStyleNames.VerseNumber);
				AddRunToMockedPara(para,
					"This is a pretty long paragraph that doesn't say much " +
					"that would be worth saying if it wouldn't be for these test. In these tests " +
					"we simply need some long paragraphs with a lot of text so that we hopefully " +
					"fill more than one page full of text. Let's just pretend this is something " +
					"useful and let's hope we have enough text now so that we can stop here.", null);
			}

			return book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the small book.
		/// </summary>
		/// <param name="nBookNumber">The book number.</param>
		/// <param name="bookName">Name of the book.</param>
		/// <param name="section2">The section2.</param>
		/// ------------------------------------------------------------------------------------
		protected IScrBook CreateSmallBook(int nBookNumber, string bookName, out IScrSection section2)
		{
			IScrBook book = AddBookToMockedScripture(nBookNumber, bookName);
			AddTitleToMockedBook(book, bookName);
			IScrSection section1 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section1, "Heading 1",
				ScrStyleNames.SectionHead);
			IStTxtPara para11 = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para11, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para11, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para11, "Verse one.", null);

			section2 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section2, "Heading 2",
				ScrStyleNames.SectionHead);
			IStTxtPara para21 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para21, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para21, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para21, "Verse one.", null);
			AddRunToMockedPara(para21, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para21, "Verse two.", null);
			IStTxtPara para22 = AddParaToMockedSectionContent(section2,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para22, "3", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para22, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para22, "Verse one.", null);

			return book;
		}
	}
}
