// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataLossDiffTests.cs
// Responsibility: TE Team

using System.Collections.Generic;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.FieldWorks.FDO;
using System.Diagnostics;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests scenarios for differences that could cause data loss, if reverted by the user.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DataLossDiffTests : ScrInMemoryFdoTestBase
	{
		#region Member variables
		private IScrBook m_genesis = null;
		private IScrBook m_genesisRevision = null;
		private DummyBookMerger m_bookMerger;
		#endregion

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create Genesis and a revision of Genesis, and create the BookMerger.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			 base.TestSetup();

			// init the DummyBookMerger
			Debug.Assert(m_bookMerger == null, "m_bookMerger is not null.");
			m_bookMerger = new DummyBookMerger(Cache, null, m_genesisRevision);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to only create a book with no content, heading, title, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_genesis = AddBookToMockedScripture(1, "Genesis");
			m_genesisRevision = AddArchiveBookToMockedScripture(1, "Genesis");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the cache.
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			m_bookMerger.Dispose();
			m_bookMerger = null;
			m_genesis = null;
			m_genesisRevision = null;

			base.TestTearDown();
		}
		#endregion

		#region RemoveSectionAddedDiffs tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method RemoveSectionAddedDiffs when there is a SectionAddedToCurrent diff.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void RemoveSectionAddedDiffs_WithSectionAddedDiff()
		{
			List<Difference> diffList = new List<Difference>();
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.VerseAddedToCurrent,
				null, null, null, null));
			IScrSection dummySection = MockRepository.GenerateStub<IScrSection>();
			dummySection.VerseRefStart = 01001001;
			dummySection.VerseRefEnd = 01001002;
			dummySection.Stub(s => s.Owner).Return(m_genesis);
			IStText heading = dummySection.HeadingOA = MockRepository.GenerateStub<IStText>();
			heading.Stub(h => h.OwningFlid).Return(ScrSectionTags.kflidHeading);
			IScrTxtPara headingPara1 = MockRepository.GenerateStub<IScrTxtPara>();
			heading.Stub(text => text[0]).Return(headingPara1);
			headingPara1.Stub(p => p.Owner).Return(heading);
			heading.Stub(h => h.Owner).Return(dummySection);
			diffList.Add(new Difference(01001001, 01001002, DifferenceType.SectionAddedToCurrent,
				new [] {dummySection}, headingPara1, 0));
			m_bookMerger.TestDiffList = diffList;
			Assert.AreEqual(2, m_bookMerger.Differences.Count);

			// Set the active difference list to one with the SectionAddedToCurrent diffs removed.
			// (This will call RemoveSectionAddedDiffs).
			m_bookMerger.UseFilteredDiffList = true;

			// We expect that only one of the differences (a VerseAddedToCurrent diff) will remain.
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();
			Assert.AreEqual(DifferenceType.VerseAddedToCurrent, diff.DiffType);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method RemoveSectionAddedDiffs when there is a VerseMoved diff.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void RemoveSectionAddedDiffs_WithVerseMovedDiff()
		{
			// Add two new differences including a VerseMoved diff
			List<Difference> diffList = new List<Difference>();
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.VerseAddedToCurrent,
				null, null, null, null));
			Difference verseMovedDiff = new Difference(null, 0, 0, null, 0, 0,
				DifferenceType.SectionAddedToCurrent, null, null, null, null);
			verseMovedDiff.SubDiffsForParas = new List<Difference>();
			verseMovedDiff.SubDiffsForParas.Add(new Difference(null, 0, 0, null, 0, 0,
				DifferenceType.VerseMoved, null, null, null, null));
			diffList.Add(verseMovedDiff);

			m_bookMerger.TestDiffList = diffList;
			Assert.AreEqual(2, m_bookMerger.Differences.Count);

			// Set the active difference list to one with the SectionAddedToCurrent diffs removed.
			// (This will call RemoveSectionAddedDiffs).
			m_bookMerger.UseFilteredDiffList = true;

			// We expect that both of the differences will remain because the SectionAddedToCurrent
			// contains a VerseMoved diff.
			Assert.AreEqual(2, m_bookMerger.Differences.Count);
		}
		#endregion

		#region IsDataLossDifference Tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method IsDataLossDifference when there is added content.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsDataLossDifference_AddedContent()
		{
			// Add content added differences. Other content added differences will have to
			// be evaluated to see if they are really data loss differences.
			List<Difference> diffList = new List<Difference>();
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.VerseAddedToCurrent,
				null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.FootnoteAddedToCurrent,
				null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.PictureAddedToCurrent,
				null, null, null, null));

			m_bookMerger.TestDiffList = diffList;

			// We expect that all of these differences will be data loss differences.
			foreach (Difference diff in m_bookMerger.Differences)
				Assert.IsTrue(m_bookMerger.IsDataLossDifference(diff));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method IsDataLossDifference when there is missing content.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsDataLossDifference_MissingContent()
		{
			// Set up missing content differences.
			List<Difference> diffList = new List<Difference>();
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.SectionMissingInCurrent,
				null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.SectionHeadMissingInCurrent,
				null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.ParagraphMissingInCurrent,
				null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.StanzaBreakMissingInCurrent,
			   null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.VerseMissingInCurrent,
			   null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.FootnoteMissingInCurrent,
				null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.PictureMissingInCurrent,
				null, null, null, null));

			m_bookMerger.TestDiffList = diffList;

			// We expect that all of these differences will not be data loss differences.
			foreach (Difference diff in m_bookMerger.Differences)
				Assert.IsFalse(m_bookMerger.IsDataLossDifference(diff));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method IsDataLossDifference when there is changed content. These are not
		/// considered data loss because the user would want to evaluate them.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsDataLossDifference_ChangedContent()
		{
			// Set up changed content differences.
			List<Difference> diffList = new List<Difference>();
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.ParagraphMergedInCurrent,
				null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.CharStyleDifference,
			   null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.MultipleCharStyleDifferences,
			   null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.ParagraphStyleDifference,
			   null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.FootnoteDifference,
			   null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.PictureDifference,
				null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.WritingSystemDifference,
				null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.MultipleWritingSystemDifferences,
				null, null, null, null));
			// Set up added section head--handled as changed content.
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.SectionHeadAddedToCurrent,
				null, null, null, null));
			diffList.Add(new Difference(null, 0, 0, null, 0, 0, DifferenceType.StanzaBreakAddedToCurrent,
				null, null, null, null));

			m_bookMerger.TestDiffList = diffList;

			// We expect that all of these differences will not be data loss differences.
			foreach (Difference diff in m_bookMerger.Differences)
				Assert.IsFalse(m_bookMerger.IsDataLossDifference(diff));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsDataLossDifference method when a section is added to the current.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsDataLossDifference_SectionAdded()
		{
			// Create a Current with two sections
			IScrSection sectionCur1 = AddSectionToMockedBook(m_genesis);
			AddSectionHeadParaToSection(sectionCur1, "Section 1",
				ScrStyleNames.SectionHead);
			IScrTxtPara para1Cur = AddParaToMockedSectionContent(sectionCur1, ScrStyleNames.NormalParagraph);
			AddVerse(para1Cur, 1, 0, "Verse one.");
			IScrSection sectionCur2 = AddSectionToMockedBook(m_genesis);
			AddSectionHeadParaToSection(sectionCur2, "Section 2",
				ScrStyleNames.SectionHead);
			IScrTxtPara para2Cur = AddParaToMockedSectionContent(sectionCur2, ScrStyleNames.NormalParagraph);
			AddVerse(para2Cur, 0, 2, "Verse two.");

			// Create a Revision with only one section
			IScrSection sectionRev1 = AddSectionToMockedBook(m_genesisRevision);
			AddSectionHeadParaToSection(sectionRev1, "Section 1",
				ScrStyleNames.SectionHead);
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev1, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 1, 0, "Verse one.");

			m_bookMerger.DetectDifferences(null);

			// We expect that we would have one difference: a SectionAdded difference which should be
			// a data loss difference.
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();
			Assert.AreEqual(DifferenceType.SectionAddedToCurrent, diff.DiffType);
			Assert.IsTrue(m_bookMerger.IsDataLossDifference(diff));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsDataLossDifference method with a VerseMoved diff (which is contained
		/// within a SectionAddedInCurrent diff).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsDataLossDifference_VerseMoved()
		{
			// Set up the three curr sections
			IScrSection section0Curr = CreateSection(m_genesis, "Section Zilch");
			IScrTxtPara para0Curr = AddParaToMockedSectionContent(section0Curr, ScrStyleNames.NormalParagraph);
			AddVerse(para0Curr, 0, "1-2", string.Empty);

			IScrSection section1Curr = CreateSection(m_genesis, "Section Uno");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(section1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(para1Curr, 0, 3, "tres ");
			AddVerse(para1Curr, 0, 9, "nueve ");
			AddVerse(para1Curr, 0, 10, "diez ");

			IScrSection section2Curr = CreateSection(m_genesis, "Section Dos");
			IScrTxtPara para2Curr = AddParaToMockedSectionContent(section2Curr, ScrStyleNames.NormalParagraph);
			AddVerse(para2Curr, 0, 12, "doce ");
			AddVerse(para2Curr, 0, 20, "vente ");

			// Set up two revision sections
			IScrSection section0Rev = CreateSection(m_genesisRevision, "Section Zilch");
			IScrTxtPara para0Rev = AddParaToMockedSectionContent(section0Rev, ScrStyleNames.NormalParagraph);
			AddVerse(para0Rev, 0, "1-2", string.Empty);

			IScrSection section1Rev = CreateSection(m_genesisRevision, "Section Ek");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 0, 10, "diez ");
			AddVerse(para1Rev, 0, 12, "doce ");
			AddVerse(para1Rev, 0, 20, "vente ");

			// find the diffs for Genesis
			m_bookMerger.DetectDifferences(null);

			// We expect two diffs: SectionAdded (containing a VerseMoved) and a TextDifference
			Assert.AreEqual(2, m_bookMerger.Differences.Count);
			Difference diff0 = m_bookMerger.Differences.MoveFirst();
			Difference diff1 = m_bookMerger.Differences.MoveNext();
			Assert.AreEqual(DifferenceType.SectionAddedToCurrent, diff0.DiffType);
			Assert.AreEqual(DifferenceType.TextDifference, diff1.DiffType);

			// We expect both the differences to not be data loss differences.
			Assert.IsFalse(m_bookMerger.IsDataLossDifference(diff0));
			Assert.IsFalse(m_bookMerger.IsDataLossDifference(diff1));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsDataLossDifference method with ParagraphAdded diffs (when they are contained
		/// within a ParagraphStructureChange diff).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsDataLossDifference_ParaStructChg_ParasAdded()
		{
			// Create a section for both the current version of Genesis and the stored revision.
			IScrSection sectionCur = CreateSection(m_genesis, "My Beautiful Verses");
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My Beautiful Verses");

			// Build up the "current" paragraph
			IScrTxtPara para1Cur = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerse(para1Cur, 30, 33, "For as churning the milk produces butter, ");
			AddVerse(para1Cur, 0, 34, "and as");
			IScrTxtPara para2Cur = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerse(para2Cur, 0, 0, "twisting the nose");
			IScrTxtPara para3Cur = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerse(para3Cur, 0, 0, "produces blood,");
			AddVerse(para3Cur, 0, 35, "so stirring up anger produces strife.");

			// Build up the "revision" paragraphs
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 30, 33, "For as churning the milk produces butter, ");
			int ichV35Rev = para1Rev.Contents.Length;
			AddVerse(para1Rev, 0, 35, "so stirring up anger produces strife.");

			m_bookMerger.DetectDifferences(null);

			// We expect one diff: ParagraphStructureChange (containing two ParagraphsAdded)
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff0 = m_bookMerger.Differences.MoveFirst();
			Assert.AreEqual(DifferenceType.ParagraphStructureChange, diff0.DiffType);
			Assert.AreEqual(3, diff0.SubDiffsForParas.Count);
			Assert.AreEqual(DifferenceType.TextDifference, diff0.SubDiffsForParas[0].DiffType);
			Assert.AreEqual(DifferenceType.ParagraphAddedToCurrent, diff0.SubDiffsForParas[1].DiffType);
			Assert.AreEqual(DifferenceType.TextDifference, diff0.SubDiffsForParas[2].DiffType);

			// We expect both the difference to be considered a data loss differences.
			Assert.IsTrue(m_bookMerger.IsDataLossDifference(diff0));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsDataLossDifference method when a paragraph is added to the current.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsDataLossDifference_ParaAdded()
		{
			// Create a Current with two paragraphs
			IScrSection sectionCur1 = AddSectionToMockedBook(m_genesis);
			AddSectionHeadParaToSection(sectionCur1, "Section 1",
				ScrStyleNames.SectionHead);
			IScrTxtPara para1Cur = AddParaToMockedSectionContent(sectionCur1, ScrStyleNames.NormalParagraph);
			AddVerse(para1Cur, 1, 0, "Verse one.");
			IScrTxtPara para2Cur = AddParaToMockedSectionContent(sectionCur1, ScrStyleNames.NormalParagraph);
			AddVerse(para2Cur, 0, 2, "Verse two.");

			// Create a Revision with only one section
			IScrSection sectionRev1 = AddSectionToMockedBook(m_genesisRevision);
			AddSectionHeadParaToSection(sectionRev1, "Section 1",
				ScrStyleNames.SectionHead);
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev1, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 1, 0, "Verse one.");

			m_bookMerger.DetectDifferences(null);

			// We expect that we would have one difference: a ParagraphAdded difference which should be
			// a data loss difference.
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();
			Assert.AreEqual(DifferenceType.ParagraphAddedToCurrent, diff.DiffType);
			Assert.IsTrue(m_bookMerger.IsDataLossDifference(diff));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsDataLossDifference method when a SectionHead is added to the Current
		/// mid-verse.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsDataLossDifference_SectionHeadAdded_MidVerse()
		{
			IScrSection sectionCur1 = AddSectionToMockedBook(m_genesis);
			AddSectionHeadParaToSection(sectionCur1, "Section 1", ScrStyleNames.SectionHead);
			IScrTxtPara para1Cur = AddParaToMockedSectionContent(sectionCur1, ScrStyleNames.NormalParagraph);
			AddVerse(para1Cur, 1, 0, "Here is a paragraph that ");
			IScrSection sectionCur2 = AddSectionToMockedBook(m_genesis);
			AddSectionHeadParaToSection(sectionCur2, "Section 2", ScrStyleNames.SectionHead);
			IScrTxtPara para2Cur = AddParaToMockedSectionContent(sectionCur2, ScrStyleNames.NormalParagraph);
			AddVerse(para2Cur, 0, 0, "will have a section inserted into it.");

			IScrSection sectionRev1 = AddSectionToMockedBook(m_genesisRevision);
			AddSectionHeadParaToSection(sectionRev1, "Section 1", ScrStyleNames.SectionHead);
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev1, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 1, 0, "Here is a paragraph that will have a section inserted into it.");

			m_bookMerger.DetectDifferences(null);

			Assert.AreEqual(2, m_bookMerger.Differences.Count);

			Difference diff0 = m_bookMerger.Differences.MoveFirst();
			Assert.AreEqual(DifferenceType.TextDifference, diff0.DiffType);
			Difference diff1 = m_bookMerger.Differences.MoveNext();
			Assert.AreEqual(DifferenceType.SectionAddedToCurrent, diff1.DiffType);
			Assert.IsFalse(m_bookMerger.IsDataLossDifference(diff0));
			Assert.IsTrue(m_bookMerger.IsDataLossDifference(diff1));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsDataLossDifference method when a paragraph is added mid-verse.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsDataLossDifference_ParaAdded_MidVerse()
		{
			IScrSection sectionCur0 = AddSectionToMockedBook(m_genesis);
			AddSectionHeadParaToSection(sectionCur0, "Section 0",
				ScrStyleNames.SectionHead);
			IScrTxtPara para0aCur = AddParaToMockedSectionContent(sectionCur0, ScrStyleNames.NormalParagraph);
			AddVerse(para0aCur, 1, 0, "Here is the very first section.");
			IScrTxtPara para0bCur = AddParaToMockedSectionContent(sectionCur0, ScrStyleNames.NormalParagraph);
			AddVerse(para0bCur, 0, 0, "This is an added paragraph that would be data loss if reverted.");

			IScrSection sectionRev0 = AddSectionToMockedBook(m_genesisRevision);
			AddSectionHeadParaToSection(sectionRev0, "Section 0",
				ScrStyleNames.SectionHead);
			IScrTxtPara para0Rev = AddParaToMockedSectionContent(sectionRev0, ScrStyleNames.NormalParagraph);
			AddVerse(para0Rev, 1, 0, "Here is the very first section.");

			m_bookMerger.DetectDifferences(null);

			// Expect 1 difference: Paragraph Added difference would cause data loss.
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff0 = m_bookMerger.Differences.MoveFirst();
			Assert.AreEqual(DifferenceType.ParagraphStructureChange, diff0.DiffType);
			Assert.IsTrue(m_bookMerger.IsDataLossDifference(diff0));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsDataLossDifference method when a verse is added to the current.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsDataLossDifference_VerseAdded()
		{
			// Create a Current with two verses
			IScrSection sectionCur1 = AddSectionToMockedBook(m_genesis);
			AddSectionHeadParaToSection(sectionCur1, "Section 1",
				ScrStyleNames.SectionHead);
			IScrTxtPara para1Cur = AddParaToMockedSectionContent(sectionCur1, ScrStyleNames.NormalParagraph);
			AddVerse(para1Cur, 1, 0, "Verse one. ");
			AddVerse(para1Cur, 0, 2, "Verse two.");

			// Create a Revision with only one section
			IScrSection sectionRev1 = AddSectionToMockedBook(m_genesisRevision);
			AddSectionHeadParaToSection(sectionRev1, "Section 1",
				ScrStyleNames.SectionHead);
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev1, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 1, 0, "Verse one. ");

			m_bookMerger.DetectDifferences(null);

			// We expect that we would have one difference: a ParagraphAdded difference which should be
			// a data loss difference.
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();
			Assert.AreEqual(DifferenceType.VerseAddedToCurrent, diff.DiffType);
			Assert.IsTrue(m_bookMerger.IsDataLossDifference(diff));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method IsDataLossDifference when a paragraph is added because of a mid-verse
		/// section head added. Reverting the newly added section is considered to be data loss.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsDataLossDifference_ParaAddedWithSectionBreak()
		{
			// Build the "current" sections
			IScrSection section1Curr = CreateSection(m_genesis, "My First Section");
			int section1CurrHvo = section1Curr.Hvo;
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(section1Curr, ScrStyleNames.NormalParagraph);
			AddVerse(para1Curr, 0, 1, "First verse. ");
			AddVerse(para1Curr, 0, 2, "This is second verse in the first paragraph which has more text");
			int iTxtChgStart = para1Curr.Contents.Length;

			IScrSection section2Curr = CreateSection(m_genesis, "My Second Section");
			IScrTxtPara para2Curr = AddParaToMockedSectionContent(section2Curr, ScrStyleNames.NormalParagraph);
			AddVerse(para2Curr, 0, 0, " in it. ");

			// Build the "revision" section
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My First Section");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddVerse(para1Rev, 0, 1, "First verse. ");
			AddVerse(para1Rev, 0, 2, "This is second verse in the first paragraph which has more text in it.");

			m_bookMerger.DetectDifferences(null);

			// We expect that we would have two differences: a TextDifference, SectionAddedToCurrent.
			Assert.AreEqual(2, m_bookMerger.Differences.Count);
			Difference diff0 = m_bookMerger.Differences.MoveFirst();
			Difference diff1 = m_bookMerger.Differences.MoveNext();
			Assert.AreEqual(DifferenceType.TextDifference, diff0.DiffType);
			Assert.AreEqual(DifferenceType.SectionAddedToCurrent, diff1.DiffType);
			Assert.IsFalse(m_bookMerger.IsDataLossDifference(diff0));
			Assert.IsTrue(m_bookMerger.IsDataLossDifference(diff1));
		}
		#endregion
	}
}
