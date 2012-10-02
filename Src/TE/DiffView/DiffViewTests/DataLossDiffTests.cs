using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using System.Diagnostics;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;

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
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();
			m_inMemoryCache.InitializeWritingSystemEncodings();

			// init the DummyBookMerger
			Debug.Assert(m_bookMerger == null, "m_bookMerger is not null.");
			m_bookMerger = new DummyBookMerger(Cache, null, m_genesisRevision);
			EnsureAnnDefn(LangProject.kguidAnnTextSegment);
			EnsureAnnDefn(LangProject.kguidAnnFreeTranslation);
			EnsureAnnDefn(LangProject.kguidAnnWordformInContext);
			EnsureAnnDefn(LangProject.kguidAnnPunctuationInContext);
			if (Cache.LangProject.WordformInventoryOA == null)
			{
				WordformInventory wfi = new WordformInventory();
				Cache.LangProject.WordformInventoryOA = wfi;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows subclasses to do other stuff to initialize the cache before it gets used
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeCache()
		{
			Cache.MapType(typeof(StTxtPara), typeof(ScrTxtPara));
			Cache.MapType(typeof(StFootnote), typeof(ScrFootnote));
			base.InitializeCache();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to only create a book with no content, heading, title, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_genesisRevision = m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the cache.
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_bookMerger.Dispose();
			m_bookMerger = null;
			m_genesis = null;
			m_genesisRevision = null;

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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_bookMerger != null)
					m_bookMerger.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_bookMerger = null;
			m_genesis = null;
			m_genesisRevision = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

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
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.VerseAddedToCurrent,
				null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.SectionAddedToCurrent,
				null, null, null, null));
			m_bookMerger.TestDiffList = diffList;
			Assert.AreEqual(2, m_bookMerger.Differences.Count);

			// Set the active difference list to one with the SectionAddedToCurrent diffs removed.
			// (This will call RemoveSectionAddedDiffs).
			m_bookMerger.UseFilteredDiffList();

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
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.VerseAddedToCurrent,
				null, null, null, null));
			Difference verseMovedDiff = new Difference(0, 0, 0, 0, 0, 0,
				DifferenceType.SectionAddedToCurrent, null, null, null, null);
			verseMovedDiff.SubDiffsForParas = new List<Difference>();
			verseMovedDiff.SubDiffsForParas.Add(new Difference(0, 0, 0, 0, 0, 0,
				DifferenceType.VerseMoved, null, null, null, null));
			diffList.Add(verseMovedDiff);

			m_bookMerger.TestDiffList = diffList;
			Assert.AreEqual(2, m_bookMerger.Differences.Count);

			// Set the active difference list to one with the SectionAddedToCurrent diffs removed.
			// (This will call RemoveSectionAddedDiffs).
			m_bookMerger.UseFilteredDiffList();

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
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.VerseAddedToCurrent,
				null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.FootnoteAddedToCurrent,
				null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.PictureAddedToCurrent,
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
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.SectionMissingInCurrent,
				null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.SectionHeadMissingInCurrent,
				null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.ParagraphMissingInCurrent,
				null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.StanzaBreakMissingInCurrent,
			   null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.VerseMissingInCurrent,
			   null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.FootnoteMissingInCurrent,
				null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.PictureMissingInCurrent,
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
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.ParagraphMergedInCurrent,
				null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.CharStyleDifference,
			   null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.MultipleCharStyleDifferences,
			   null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.ParagraphStyleDifference,
			   null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.FootnoteDifference,
			   null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.PictureDifference,
				null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.WritingSystemDifference,
				null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.MultipleWritingSystemDifferences,
				null, null, null, null));
			// Set up added section head--handled as changed content.
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.SectionHeadAddedToCurrent,
				null, null, null, null));
			diffList.Add(new Difference(0, 0, 0, 0, 0, 0, DifferenceType.StanzaBreakAddedToCurrent,
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
			IScrSection sectionCur1 = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionCur1.Hvo, "Section 1",
				ScrStyleNames.SectionHead);
			StTxtPara para1Cur = AddPara(sectionCur1);
			AddVerse(para1Cur, 1, 0, "Verse one.");
			sectionCur1.AdjustReferences();
			IScrSection sectionCur2 = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionCur2.Hvo, "Section 2",
				ScrStyleNames.SectionHead);
			StTxtPara para2Cur = AddPara(sectionCur2);
			AddVerse(para2Cur, 0, 2, "Verse two.");
			sectionCur2.AdjustReferences();

			// Create a Revision with only one section
			IScrSection sectionRev1 = m_scrInMemoryCache.AddSectionToMockedBook(m_genesisRevision.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionRev1.Hvo, "Section 1",
				ScrStyleNames.SectionHead);
			StTxtPara para1Rev = AddPara(sectionRev1);
			AddVerse(para1Rev, 1, 0, "Verse one.");
			sectionRev1.AdjustReferences();

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
			StTxtPara para0Curr = AddPara(section0Curr);
			AddVerse(para0Curr, 0, "1-2", string.Empty);
			section0Curr.AdjustReferences();

			IScrSection section1Curr = CreateSection(m_genesis, "Section Uno");
			StTxtPara para1Curr = AddPara(section1Curr);
			AddVerse(para1Curr, 0, 3, "tres ");
			AddVerse(para1Curr, 0, 9, "nueve ");
			AddVerse(para1Curr, 0, 10, "diez ");
			section1Curr.AdjustReferences();

			IScrSection section2Curr = CreateSection(m_genesis, "Section Dos");
			StTxtPara para2Curr = AddPara(section2Curr);
			AddVerse(para2Curr, 0, 12, "doce ");
			AddVerse(para2Curr, 0, 20, "vente ");
			section2Curr.AdjustReferences();

			// Set up two revision sections
			IScrSection section0Rev = CreateSection(m_genesisRevision, "Section Zilch");
			StTxtPara para0Rev = AddPara(section0Rev);
			AddVerse(para0Rev, 0, "1-2", string.Empty);
			section0Rev.AdjustReferences();

			IScrSection section1Rev = CreateSection(m_genesisRevision, "Section Ek");
			StTxtPara para1Rev = AddPara(section1Rev);
			AddVerse(para1Rev, 0, 10, "diez ");
			AddVerse(para1Rev, 0, 12, "doce ");
			AddVerse(para1Rev, 0, 20, "vente ");
			section1Rev.AdjustReferences();

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
			StTxtPara para1Cur = AddPara(sectionCur);
			AddVerse(para1Cur, 30, 33, "For as churning the milk produces butter, ");
			AddVerse(para1Cur, 0, 34, "and as");
			StTxtPara para2Cur = AddPara(sectionCur);
			AddVerse(para2Cur, 0, 0, "twisting the nose");
			StTxtPara para3Cur = AddPara(sectionCur);
			AddVerse(para3Cur, 0, 0, "produces blood,");
			AddVerse(para3Cur, 0, 35, "so stirring up anger produces strife.");
			sectionCur.AdjustReferences();

			// Build up the "revision" paragraphs
			StTxtPara para1Rev = AddPara(sectionRev);
			AddVerse(para1Rev, 30, 33, "For as churning the milk produces butter, ");
			int ichV35Rev = para1Rev.Contents.Length;
			AddVerse(para1Rev, 0, 35, "so stirring up anger produces strife.");
			sectionRev.AdjustReferences();

			m_bookMerger.DetectDifferences(null);

			// We expect one diff: ParagraphStructureChange (containing two ParagraphsAdded)
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff0 = m_bookMerger.Differences.MoveFirst();
			Assert.AreEqual(DifferenceType.ParagraphStructureChange, diff0.DiffType);
			Assert.AreEqual(3, diff0.SubDiffsForParas.Count);
			Assert.AreEqual(DifferenceType.TextDifference, diff0.SubDiffsForParas[0].DiffType);
			Assert.AreEqual(DifferenceType.ParagraphAddedToCurrent, diff0.SubDiffsForParas[1].DiffType);
			Assert.AreEqual(DifferenceType.ParagraphAddedToCurrent, diff0.SubDiffsForParas[2].DiffType);

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
			IScrSection sectionCur1 = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionCur1.Hvo, "Section 1",
				ScrStyleNames.SectionHead);
			StTxtPara para1Cur = AddPara(sectionCur1);
			AddVerse(para1Cur, 1, 0, "Verse one.");
			StTxtPara para2Cur = AddPara(sectionCur1);
			AddVerse(para2Cur, 0, 2, "Verse two.");
			sectionCur1.AdjustReferences();

			// Create a Revision with only one section
			IScrSection sectionRev1 = m_scrInMemoryCache.AddSectionToMockedBook(m_genesisRevision.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionRev1.Hvo, "Section 1",
				ScrStyleNames.SectionHead);
			StTxtPara para1Rev = AddPara(sectionRev1);
			AddVerse(para1Rev, 1, 0, "Verse one.");
			sectionRev1.AdjustReferences();

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
		/// mid-verse and then creates a ParagraphAddedInCurrent difference.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsDataLossDifference_SectionHeadAdded_MidVerse()
		{
			IScrSection sectionCur1 = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionCur1.Hvo, "Section 1",
				ScrStyleNames.SectionHead);
			StTxtPara para1Cur = AddPara(sectionCur1);
			AddVerse(para1Cur, 1, 0, "Here is a paragraph that ");
			sectionCur1.AdjustReferences();
			IScrSection sectionCur2 = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionCur2.Hvo, "Section 2",
			ScrStyleNames.SectionHead);
			StTxtPara para2Cur = AddPara(sectionCur2);
			AddVerse(para2Cur, 0, 0, "will have a section inserted into it.");
			sectionCur2.AdjustReferences();

			IScrSection sectionRev1 = m_scrInMemoryCache.AddSectionToMockedBook(m_genesisRevision.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionRev1.Hvo, "Section 1",
				ScrStyleNames.SectionHead);
			StTxtPara para1Rev = AddPara(sectionRev1);
			AddVerse(para1Rev, 1, 0, "Here is a paragraph that will have a section inserted into it.");
			sectionRev1.AdjustReferences();

			m_bookMerger.DetectDifferences(null);

			// Expect 2 differences: ParagraphAdded difference should not cause data loss as
			// it is treated as a unit with other 2 differences
			Assert.AreEqual(3, m_bookMerger.Differences.Count);

			Difference diff0 = m_bookMerger.Differences.MoveFirst();
			Assert.AreEqual(DifferenceType.TextDifference, diff0.DiffType);
			Difference diff1 = m_bookMerger.Differences.MoveNext();
			Assert.AreEqual(DifferenceType.SectionHeadAddedToCurrent, diff1.DiffType);
			Difference diff2 = m_bookMerger.Differences.MoveNext();
			Assert.AreEqual(DifferenceType.ParagraphAddedToCurrent, diff2.DiffType);
			Assert.IsFalse(m_bookMerger.IsDataLossDifference(diff0));
			Assert.IsFalse(m_bookMerger.IsDataLossDifference(diff1));
			Assert.IsFalse(m_bookMerger.IsDataLossDifference(diff2));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsDataLossDifference method when a paragraph is added mid-verse.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsDataLossDifference_ParaAdded_MidVerse()
		{
			IScrSection sectionCur0 = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionCur0.Hvo, "Section 0",
				ScrStyleNames.SectionHead);
			StTxtPara para0aCur = AddPara(sectionCur0);
			AddVerse(para0aCur, 1, 0, "Here is the very first section.");
			StTxtPara para0bCur = AddPara(sectionCur0);
			AddVerse(para0bCur, 0, 0, "This is an added paragraph that would be data loss if reverted.");
			sectionCur0.AdjustReferences();

			IScrSection sectionRev0 = m_scrInMemoryCache.AddSectionToMockedBook(m_genesisRevision.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionRev0.Hvo, "Section 0",
				ScrStyleNames.SectionHead);
			StTxtPara para0Rev = AddPara(sectionRev0);
			AddVerse(para0Rev, 1, 0, "Here is the very first section.");
			sectionRev0.AdjustReferences();

			m_bookMerger.DetectDifferences(null);

			// Expect 1 difference: ParagraphSplitInCurrent difference would cause data loss.
			// This diff should be ParagraphAddedInCurrent, but is constrained by TE-7334.
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff0 = m_bookMerger.Differences.MoveFirst();
			Assert.AreEqual(DifferenceType.ParagraphSplitInCurrent, diff0.DiffType);
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
			IScrSection sectionCur1 = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionCur1.Hvo, "Section 1",
				ScrStyleNames.SectionHead);
			StTxtPara para1Cur = AddPara(sectionCur1);
			AddVerse(para1Cur, 1, 0, "Verse one. ");
			AddVerse(para1Cur, 0, 2, "Verse two.");
			sectionCur1.AdjustReferences();

			// Create a Revision with only one section
			IScrSection sectionRev1 = m_scrInMemoryCache.AddSectionToMockedBook(m_genesisRevision.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(sectionRev1.Hvo, "Section 1",
				ScrStyleNames.SectionHead);
			StTxtPara para1Rev = AddPara(sectionRev1);
			AddVerse(para1Rev, 1, 0, "Verse one. ");
			sectionRev1.AdjustReferences();

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
		/// section head added. This is not considered to be data loss.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsDataLossDifference_ParaAddedWithSectionBreak()
		{
			// Build the "current" sections
			IScrSection section1Curr = CreateSection(m_genesis, "My First Section");
			int section1CurrHvo = section1Curr.Hvo;
			StTxtPara para1Curr = AddPara(section1Curr);
			AddVerse(para1Curr, 0, 1, "First verse. ");
			AddVerse(para1Curr, 0, 2, "This is second verse in the first paragraph which has more text");
			int iTxtChgStart = para1Curr.Contents.Length;
			section1Curr.AdjustReferences();

			IScrSection section2Curr = CreateSection(m_genesis, "My Second Section");
			StTxtPara para2Curr = AddPara(section2Curr);
			AddVerse(para2Curr, 0, 0, " in it. ");
			section2Curr.AdjustReferences();

			// Build the "revision" section
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My First Section");
			StTxtPara para1Rev = AddPara(section1Rev);
			AddVerse(para1Rev, 0, 1, "First verse. ");
			AddVerse(para1Rev, 0, 2, "This is second verse in the first paragraph which has more text in it.");
			section1Rev.AdjustReferences();

			m_bookMerger.DetectDifferences(null);

			// We expect that we would have three difference: a TextDifference, SectionHeadAddedToCurrent
			// and ParagraphAddedToCurrent.
			Assert.AreEqual(3, m_bookMerger.Differences.Count);
			Difference diff0 = m_bookMerger.Differences.MoveFirst();
			Difference diff1 = m_bookMerger.Differences.MoveNext();
			Difference diff2 = m_bookMerger.Differences.MoveNext();
			Assert.AreEqual(DifferenceType.TextDifference, diff0.DiffType);
			Assert.AreEqual(DifferenceType.SectionHeadAddedToCurrent, diff1.DiffType);
			Assert.AreEqual(DifferenceType.ParagraphAddedToCurrent, diff2.DiffType);
			// We expect that all differences would not be data loss differences.
			Assert.IsFalse(m_bookMerger.IsDataLossDifference(diff0));
			Assert.IsFalse(m_bookMerger.IsDataLossDifference(diff1));
			Assert.IsFalse(m_bookMerger.IsDataLossDifference(diff2));
		}
		#endregion
	}
}
