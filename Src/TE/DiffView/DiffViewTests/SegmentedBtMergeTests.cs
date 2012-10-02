using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Test.TestUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Additional tests of BookMerger to confirm appropriate merging of segmented back translation.
	/// These tests do not confirm the necessary calls to LoadSegmentFreeTranslations, since this
	/// routine does nothing on an InMemory test.
	/// </summary>
	[TestFixture]
	public class SegmentedBtMergeTests : ScrInMemoryFdoTestBase
	{
		private IScrBook m_genesis;
		private IScrBook m_genesisRevision;
		private DummyBookMerger m_bookMerger;
		private int m_hvoSegAnnDefn;
		private int m_hvoFtAnnDefn;
		int kflidFT; // flid for free translation of segment
		int kflidSegments; // flid for segments of paragraph
		#region Setup
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
			m_hvoSegAnnDefn = EnsureAnnDefn(LangProject.kguidAnnTextSegment);
			m_hvoFtAnnDefn = EnsureAnnDefn(LangProject.kguidAnnFreeTranslation);
			EnsureAnnDefn(LangProject.kguidAnnWordformInContext);
			EnsureAnnDefn(LangProject.kguidAnnPunctuationInContext);
			kflidSegments = StTxtPara.SegmentsFlid(Cache);
			kflidFT = StTxtPara.SegmentFreeTranslationFlid(Cache);
			if (Cache.LangProject.WordformInventoryOA == null)
			{
				WordformInventory wfi = new WordformInventory();
				Cache.LangProject.WordformInventoryOA = wfi;
			}
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to only create a book with no content, heading, title, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_genesisRevision = m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis");

			//Philemon
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Philemon");

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Paul tells people", "Section Head");
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, "Paragraph");
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", "Chapter Number");
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", "Verse Number");
			m_scrInMemoryCache.AddRunToMockedPara(para, "and the earth was without form and void and darkness covered the face of the deep", null);
			section.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo, "Paul tells people more", "Section Head");
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, "Paragraph");
			m_scrInMemoryCache.AddRunToMockedPara(para2, "2", "Chapter Number");
			m_scrInMemoryCache.AddRunToMockedPara(para2, "1", "Verse Number");
			m_scrInMemoryCache.AddRunToMockedPara(para2, "paul expounds on the nature of reality", null);
			StTxtPara para3 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, "Paragraph");
			m_scrInMemoryCache.AddRunToMockedPara(para3, "2", "Verse Number");
			m_scrInMemoryCache.AddRunToMockedPara(para3, "the existentialists are all wrong", null);
			section2.AdjustReferences();

			//Jude
			IScrBook jude = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
			IScrSection judeSection = m_scrInMemoryCache.AddSectionToMockedBook(jude.Hvo);
			m_scrInMemoryCache.AddTitleToMockedBook(jude.Hvo, "Jude");
			m_scrInMemoryCache.AddSectionHeadParaToSection(judeSection.Hvo, "Introduction", "Section Head");
			StTxtPara judePara = m_scrInMemoryCache.AddParaToMockedSectionContent(judeSection.Hvo, "Intro Paragraph");
			m_scrInMemoryCache.AddRunToMockedPara(judePara, "The Letter from Jude was written to warn against" +
				" false teachers who claimed to be believers. In this brief letter, which is similar in" +
				" content to 2 Peter the writer encourages his readers “to fight on for the faith which" +
				" once and for all God has given to his people.", null);
			judeSection.AdjustReferences();
		}
		/// <summary>
		/// Verify that object has been deleted. This is a rather kludgy way to do it, but the memory cache
		/// we're using doesn't have a useful implementation of IsValidObject.
		/// </summary>
		void VerifyDel(int hvo)
		{
			int clsid = Cache.GetClassOfObject(hvo);
			Assert.AreEqual(0, clsid, "object should have been deleted" + hvo);
		}
		#endregion	}

		#region helper methods
		void AddRunAndSegmentToMockedPara(StTxtPara para, string runText, string runStyleName, string ft)
		{
			int ichMin = para.Contents.Length;
			m_scrInMemoryCache.AddRunToMockedPara(para, runText, runStyleName);
			AddSegment(para, ichMin, ft);
		}

		void AddRunAndSegmentToMockedPara(StTxtPara para, string runText, int ws, string ft)
		{
			int ichMin = para.Contents.Length;
			m_scrInMemoryCache.AddRunToMockedPara(para, runText, ws);
			AddSegment(para, ichMin, ft);
		}
		private void AddSegment(StTxtPara para, int ichMin, string ft)
		{
			AddSegment(para, ichMin, para.Contents.Length, ft);
		}

		private void AddSegment(StTxtPara para, int ichMin, int ichLim, string ft)
		{
			// Since this is an in-memory test, we can't use CmBaseAnnotation.CreateUnownedCba.
			CmBaseAnnotation seg = new CmBaseAnnotation();
			Cache.LangProject.AnnotationsOC.Add(seg);
			seg.AnnotationTypeRAHvo = m_hvoSegAnnDefn;
			seg.BeginOffset = ichMin;
			seg.EndOffset = ichLim;
			seg.BeginObjectRA = para;
			seg.EndObjectRA = para;
			if (Cache.MainCacheAccessor.get_IsPropInCache(para.Hvo, kflidSegments, (int)CellarModuleDefns.kcptReferenceSequence, 0))
			{
				int cseg = Cache.GetVectorSize(para.Hvo, kflidSegments);
				m_scrInMemoryCache.CacheAccessor.CacheReplace(para.Hvo, kflidSegments, cseg, cseg, new int[] {seg.Hvo}, 1);
			}
			else
			{
				// Don't want to just read it, because that actually parses the paragraph. If it's not already in memory
				// this is the first one, so just make it so.
				Cache.VwCacheDaAccessor.CacheVecProp(para.Hvo, kflidSegments, new int[] {seg.Hvo}, 1);
			}
			if (ft != null)
			{
				// Since this is an in-memory test, we can't use CmIndirectAnnotation.CreateUnownedIndirectAnnotation.
				CmIndirectAnnotation ftAnn = new CmIndirectAnnotation();
				Cache.LangProject.AnnotationsOC.Add(ftAnn);
				ftAnn.AnnotationTypeRAHvo = m_hvoFtAnnDefn;
				ftAnn.AppliesToRS.Append(seg);
				ftAnn.Comment.SetAlternative(ft, Cache.DefaultAnalWs);
				m_scrInMemoryCache.CacheAccessor.CacheObjProp(seg.Hvo, kflidFT, ftAnn.Hvo);
			}
		}

		/// <summary>
		/// Verify that the paragraph has the expected number of segments with the expected free translations.
		/// Lengths should be the lengths of the segments.
		/// </summary>
		private void VerifySegments(StTxtPara para, string[] freeTranslations, int[] lengths, string label)
		{
			int cseg = Cache.GetVectorSize(para.Hvo, kflidSegments);
			Assert.AreEqual(freeTranslations.Length, cseg);
			int i = 0;
			int cumLength = 0;
			foreach (string ft in freeTranslations)
			{
				int hvoSeg = Cache.GetVectorItem(para.Hvo, kflidSegments, i);
				CmBaseAnnotation seg = new CmBaseAnnotation(Cache, hvoSeg);
				Assert.AreEqual(cumLength, seg.BeginOffset, label + " - beginOffset " + i);
				cumLength += lengths[i];
				Assert.AreEqual(cumLength, seg.EndOffset, label + " - endOffset " + i);
				Assert.AreEqual(m_hvoSegAnnDefn, seg.AnnotationTypeRAHvo, label + " - seg ann type " + i);
				if (ft != null)
				{
					int hvoFt = Cache.GetObjProperty(hvoSeg, kflidFT);
					Assert.IsTrue(hvoFt != 0, label + "ft present " + i);
					CmIndirectAnnotation ftAnn = new CmIndirectAnnotation(Cache, hvoFt);
					Assert.AreEqual(ft, ftAnn.Comment.AnalysisDefaultWritingSystem.Text, label + " - comment " + i);
					Assert.AreEqual(m_hvoFtAnnDefn, ftAnn.AnnotationTypeRAHvo, label + " - ft type " + i);
					Assert.AreEqual(1, ftAnn.AppliesToRS.Count, label + " - appliesTo length " + i);
					Assert.AreEqual(seg.Hvo, ftAnn.AppliesToRS.HvoArray[0], label + " - applies to val " + i);
				}
				i++;
			}
		}
		private StFootnote AddFootnoteSegment(StTxtPara para1Curr, string noteText, string noteTrans)
		{
			int ichMin = para1Curr.Contents.Length;
			StFootnote footnote1Curr = m_scrInMemoryCache.AddFootnote(m_genesis, para1Curr, ichMin,
																	  noteText);
			AddSegment(para1Curr, ichMin, null); // the footnote is always its own segment
			StTxtPara fnPara = footnote1Curr.ParagraphsOS[0] as StTxtPara;
			AddSegment(fnPara, 0, fnPara.Contents.Length, noteTrans);
			return footnote1Curr;
		}
		#endregion helper methods
		#region ReplaceCurrentWithRevision WithinPara Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book and the revision have a single difference in the verse text of one
		/// verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_SimpleText()
		{
			CheckDisposed();

			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionCurr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Current.", Cache.DefaultVernWs, "Current Trans");
			sectionCurr.AdjustReferences();

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara para1Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionRev.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Rev.", Cache.DefaultVernWs, "Revised Trans");
			sectionRev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// quick check of the diffs
			Difference diff = m_bookMerger.Differences.MoveFirst();
			DiffTestHelper.VerifyParaDiff(diff, 01001001, DifferenceType.TextDifference,
				para1Curr, 1, 8,  // chapter number and ending period are not included
				para1Rev, 1, 4);

			// Do the "ReplaceCurrentWithRevision" action
			m_bookMerger.ReplaceCurrentWithRevision(diff);
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// Verify the changed paragraph
			StTxtPara paraNew = new StTxtPara(Cache, diff.HvoCurr);
			Assert.AreEqual(para1Curr.Hvo, paraNew.Hvo);
			Assert.AreEqual("1Rev.", paraNew.Contents.Text);
			// verify segment Bt also updated
			VerifySegments(para1Curr, new string[] {null, "Revised Trans"}, new int[] {1, para1Curr.Contents.Length - 1}, "simple text");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book and the revision have a single difference in the verse text of one
		/// verse. In particlar this verifies that the old BT is kept if the revision BT is
		/// empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_DontEraseBT()
		{
			CheckDisposed();

			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionCurr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Current.", Cache.DefaultVernWs, "Current Trans");
			sectionCurr.AdjustReferences();

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara para1Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionRev.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Rev.", Cache.DefaultVernWs, "");
			sectionRev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// quick check of the diffs
			Difference diff = m_bookMerger.Differences.MoveFirst();
			DiffTestHelper.VerifyParaDiff(diff, 01001001, DifferenceType.TextDifference,
				para1Curr, 1, 8,  // chapter number and ending period are not included
				para1Rev, 1, 4);

			// Do the "ReplaceCurrentWithRevision" action
			m_bookMerger.ReplaceCurrentWithRevision(diff);
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// Verify the changed paragraph
			StTxtPara paraNew = new StTxtPara(Cache, diff.HvoCurr);
			Assert.AreEqual(para1Curr.Hvo, paraNew.Hvo);
			Assert.AreEqual("1Rev.", paraNew.Contents.Text);
			// verify segment Bt also updated
			VerifySegments(para1Curr, new string[] { null, "Current Trans" }, new int[] { 1, para1Curr.Contents.Length - 1 }, "simple text");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book and the revision have a single difference in the verse text of one
		/// verse, but the revision has more segments in the verse than current.
		/// Also tests case of subsequent verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_SimpleTextMoreSegsInRev()
		{
			CheckDisposed();

			string ouch = "Ouch! ";
			string hit = "It got hit.";
			string ball = "By a ball.";

			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionCurr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, hit, Cache.DefaultVernWs, "It got hit Trans");
			AddRunAndSegmentToMockedPara(para1Curr, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, ball, Cache.DefaultVernWs, "By a ball Trans");
			sectionCurr.AdjustReferences();

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara para1Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionRev.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, ouch, Cache.DefaultVernWs, "Ouch Trans");
			AddRunAndSegmentToMockedPara(para1Rev, hit, Cache.DefaultVernWs, "It got hit Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, ball, Cache.DefaultVernWs, "By a ball Trans");
			sectionRev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// quick check of the diffs
			Difference diff = m_bookMerger.Differences.MoveFirst();
			DiffTestHelper.VerifyParaDiff(diff, 01001001, DifferenceType.TextDifference,
				para1Curr, 1, 1,
				para1Rev, 1, 1 + ouch.Length);  // to end of space after "Ouch! "

			// Do the "ReplaceCurrentWithRevision" action
			m_bookMerger.ReplaceCurrentWithRevision(diff);
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// Verify the changed paragraph
			StTxtPara paraNew = new StTxtPara(Cache, diff.HvoCurr);
			Assert.AreEqual(para1Curr.Hvo, paraNew.Hvo);
			Assert.AreEqual("1Ouch! It got hit.2By a ball.", paraNew.Contents.Text);
			// verify segment Bt also updated
			VerifySegments(para1Curr, new string[] { null, "Ouch Trans", "It got hit Trans", null, "By a ball Trans" },
				new int[] { 1, ouch.Length, hit.Length, 1, ball.Length }, "extra segment");
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book and the revision have a single difference in the verse text of one
		/// verse, but the revision has more segments in the verse than current.
		/// Also tests case of subsequent verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_SimpleTextFewerSegsInRev()
		{
			CheckDisposed();

			string ouch = "Ouch! ";
			string hit = "It got hit.";
			string ball = "By a ball.";

			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionCurr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, ouch, Cache.DefaultVernWs, "Ouch Trans");
			AddRunAndSegmentToMockedPara(para1Curr, hit, Cache.DefaultVernWs, "It got hit Trans");
			AddRunAndSegmentToMockedPara(para1Curr, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, ball, Cache.DefaultVernWs, "By a ball Trans");
			sectionCurr.AdjustReferences();

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara para1Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionRev.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, hit, Cache.DefaultVernWs, "It got hit Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, ball, Cache.DefaultVernWs, "By a ball Trans");
			sectionRev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// quick check of the diffs
			Difference diff = m_bookMerger.Differences.MoveFirst();
			DiffTestHelper.VerifyParaDiff(diff, 01001001, DifferenceType.TextDifference,
				para1Curr, 1, 1 + ouch.Length,
				para1Rev, 1, 1);  // to end of space after "Ouch! "

			// Do the "ReplaceCurrentWithRevision" action
			m_bookMerger.ReplaceCurrentWithRevision(diff);
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// Verify the changed paragraph
			StTxtPara paraNew = new StTxtPara(Cache, diff.HvoCurr);
			Assert.AreEqual(para1Curr.Hvo, paraNew.Hvo);
			Assert.AreEqual("1It got hit.2By a ball.", paraNew.Contents.Text);
			// verify segment Bt also updated
			VerifySegments(para1Curr, new string[] { null, "It got hit Trans", null, "By a ball Trans" },
				new int[] { 1, hit.Length, 1, ball.Length }, "removed segment");
		}

		// Review JohnT: maybe we need some tests similar to these? At least something to cover diffs IN footnotes.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the revision
		/// has a duplicate, adjacent verse number in the same paragraph and there is a missing
		/// paragraph in the current. See TE-7137.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_DuplicateVerseInPara()
		{
			CheckDisposed();

			// Create Scripture data in the current.
			IScrSection sectionCur = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = AddPara(sectionCur);
			AddVerseSegment(para1Curr, 1, 1, "one ", "one trans");
			AddVerseSegment(para1Curr, 0, 2, "two ", "two trans");
			AddVerseSegment(para1Curr, 0, 3, "three ", "three trans");
			AddVerseSegment(para1Curr, 0, 4, "four ", "four trans");
			int ichTxtChgMin = para1Curr.Contents.Length;
			AddVerseSegment(para1Curr, 0, 5, "five", "five trans");
			sectionCur.AdjustReferences();

			// Create Scripture data in the revision.
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara para1Rev = AddPara(sectionRev);
			AddVerseSegment(para1Rev, 1, 1, "one ", "one trans");
			AddVerseSegment(para1Rev, 0, 2, "two ", "two trans");
			AddVerseSegment(para1Rev, 0, 3, "three ", "three trans");
			AddVerseSegment(para1Rev, 0, 4, "four ", "four trans");
			AddVerseSegment(para1Rev, 0, 4, "four again ", "four again trans");
			int ichTxtChgLimRev = para1Rev.Contents.Length;
			AddVerseSegment(para1Rev, 0, 5, "five", "five trans");

			StTxtPara para2Rev = AddPara(sectionRev);
			AddVerseSegment(para2Rev, 0, 6, "paragraph to restore from the revision.", "restore para trans");
			sectionRev.AdjustReferences();

			// Detect differences and verify that they are correct
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis

			Assert.AreEqual(2, m_bookMerger.Differences.Count);
			Difference diff1 = m_bookMerger.Differences.MoveFirst();
			Difference diff2 = m_bookMerger.Differences.MoveNext();

			// Revert to revision
			m_bookMerger.ReplaceCurrentWithRevision(diff1);
			m_bookMerger.ReplaceCurrentWithRevision(diff2);

			// We expect that the second para in the revision will be added to the current.
			Assert.AreEqual(2, sectionCur.ContentOA.ParagraphsOS.Count);
			VerifySegments(para1Curr, new string[] { null, "one trans", null, "two trans", null, "three trans", null,
				"four trans", null, "four again trans", null,  "five trans"},
				new int[] { 2, "one ".Length, 1, "two ".Length, 1, "three ".Length, 1, "four ".Length, 1,
					"four again ".Length, 1, "five".Length }, "insert dup verse");
			StTxtPara para2Curr = sectionCur.ContentOA.ParagraphsOS[1] as StTxtPara;
			VerifySegments(para2Curr, new string[] { null, "restore para trans" },
				new int[] { 1, "paragraph to restore from the revision.".Length }, "insert para");
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// text difference range contains a footnote in both books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_SimpleText_WithFootnote()
		{
			CheckDisposed();

			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionCurr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Before fn", Cache.DefaultVernWs, "Before fn trans");
			// place the footnote within the text
			StFootnote footnote1Curr = AddFootnoteSegment(para1Curr, "Current footnote text", "Current footnote trans");
			AddRunAndSegmentToMockedPara(para1Curr, "After fn", Cache.DefaultVernWs, "After fn trans");
			sectionCurr.AdjustReferences();

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara para1Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionRev.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Before Rev", Cache.DefaultVernWs, "Before Rev trans");
			// place the footnote within the text
			StFootnote footnote1Rev = AddFootnoteSegment(para1Rev, "New footnote text", "New footnote trans");
			AddRunAndSegmentToMockedPara(para1Rev, "After fn", Cache.DefaultVernWs, "After fn trans");
			sectionRev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis

			// quick check of the diffs
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();

			m_bookMerger.ReplaceCurrentWithRevision(diff);


			//Verify the changed Current paragraph
			Assert.AreEqual("1Before Rev" + StringUtils.kchObject + "After fn", para1Curr.Contents.Text);

			// the new footnote should have the same content as the original Rev footnote
			StFootnote footnoteNew = (StFootnote)m_genesis.FootnotesOS.FirstItem;
			StTxtPara paraFn = ((StTxtPara)footnoteNew.ParagraphsOS.FirstItem);
			AssertEx.AreTsStringsEqual(((StTxtPara)footnote1Rev.ParagraphsOS.FirstItem).Contents.UnderlyingTsString,
				paraFn.Contents.UnderlyingTsString);
			VerifySegments(para1Curr, new string[] { null, "Before Rev trans", null, "After fn trans" },
				new int[] { 1, "Before Rev".Length, 1, "After fn" .Length}, "para with footnote marker");
			VerifySegments(paraFn, new string[] { "New footnote trans" },
				new int[] { "New footnote text".Length }, "footnote itself");

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// Revision adds a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_SimpleText_InsertFootnote()
		{
			CheckDisposed();

			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionCurr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Before fn. ", Cache.DefaultVernWs, "Before fn trans");
			AddRunAndSegmentToMockedPara(para1Curr, "After fn", Cache.DefaultVernWs, "After fn trans");
			sectionCurr.AdjustReferences();

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara para1Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionRev.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Before fn. ", Cache.DefaultVernWs, "Before fn trans");
			// place the footnote within the text
			StFootnote footnote1Rev = AddFootnoteSegment(para1Rev, "New footnote text", "New footnote trans");
			AddRunAndSegmentToMockedPara(para1Rev, "After fn", Cache.DefaultVernWs, "After fn trans");
			sectionRev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis

			// quick check of the diffs
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();

			m_bookMerger.ReplaceCurrentWithRevision(diff);

			//Verify the changed Current paragraph
			Assert.AreEqual("1Before fn. " + StringUtils.kchObject + "After fn", para1Curr.Contents.Text);

			StFootnote footnoteNew = (StFootnote)m_genesis.FootnotesOS.FirstItem;
			StTxtPara paraFn = ((StTxtPara)footnoteNew.ParagraphsOS.FirstItem);
			AssertEx.AreTsStringsEqual(((StTxtPara)footnote1Rev.ParagraphsOS.FirstItem).Contents.UnderlyingTsString,
				paraFn.Contents.UnderlyingTsString);
			VerifySegments(para1Curr, new string[] { null, "Before fn trans", null, "After fn trans" },
				new int[] { 1, "Before fn. ".Length, 1, "After fn".Length }, "restore footnote");
			VerifySegments(paraFn, new string[] { "New footnote trans" },
				new int[] { "New footnote text".Length }, "footnote itself");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// Revision adds a footnote and some surrounding text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_SimpleText_InsertFnAndSegs()
		{
			CheckDisposed();

			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionCurr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Before fn. ", Cache.DefaultVernWs, "Before fn trans");
			AddRunAndSegmentToMockedPara(para1Curr, "After fn", Cache.DefaultVernWs, "After fn trans");
			sectionCurr.AdjustReferences();

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara para1Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionRev.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Before fn. ", Cache.DefaultVernWs, "Before fn trans");
			AddRunAndSegmentToMockedPara(para1Rev, "Inserted before. ", Cache.DefaultVernWs, "Inserted before trans");
			// place the footnote within the text
			StFootnote footnote1Rev = AddFootnoteSegment(para1Rev, "New footnote text", "New footnote trans");
			AddRunAndSegmentToMockedPara(para1Rev, "Inserted after. ", Cache.DefaultVernWs, "Inserted after trans");
			AddRunAndSegmentToMockedPara(para1Rev, "After fn", Cache.DefaultVernWs, "After fn trans");
			sectionRev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis

			// quick check of the diffs
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();

			m_bookMerger.ReplaceCurrentWithRevision(diff);

			//Verify the changed Current paragraph
			Assert.AreEqual("1Before fn. Inserted before. " + StringUtils.kchObject + "Inserted after. After fn", para1Curr.Contents.Text);

			StFootnote footnoteNew = (StFootnote)m_genesis.FootnotesOS.FirstItem;
			StTxtPara paraFn = ((StTxtPara)footnoteNew.ParagraphsOS.FirstItem);
			AssertEx.AreTsStringsEqual(((StTxtPara)footnote1Rev.ParagraphsOS.FirstItem).Contents.UnderlyingTsString,
				paraFn.Contents.UnderlyingTsString);
			VerifySegments(para1Curr, new string[] { null, "Before fn trans", "Inserted before trans", null, "Inserted after trans", "After fn trans" },
				new int[] { 1, "Before fn. ".Length, "Inserted before. ".Length, 1, "Inserted after. ".Length, "After fn".Length }, "insert fn + text");
			VerifySegments(paraFn, new string[] { "New footnote trans" },
				new int[] { "New footnote text".Length }, "footnote itself");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// Revision adds a footnote which divides a previous single segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_SimpleText_InsertFootnote_BreakingSeg()
		{
			CheckDisposed();

			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionCurr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Before fn After fn", Cache.DefaultVernWs, "No fn trans");
			sectionCurr.AdjustReferences();

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara para1Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionRev.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Before fn ", Cache.DefaultVernWs, "Before fn trans");
			// place the footnote within the text
			StFootnote footnote1Rev = AddFootnoteSegment(para1Rev, "New footnote text", "New footnote trans");
			AddRunAndSegmentToMockedPara(para1Rev, "After fn", Cache.DefaultVernWs, "After fn trans");
			sectionRev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis

			// quick check of the diffs
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();

			m_bookMerger.ReplaceCurrentWithRevision(diff);

			//Verify the changed Current paragraph
			Assert.AreEqual("1Before fn " + StringUtils.kchObject + "After fn", para1Curr.Contents.Text);

			StFootnote footnoteNew = (StFootnote)m_genesis.FootnotesOS.FirstItem;
			StTxtPara paraFn = ((StTxtPara)footnoteNew.ParagraphsOS.FirstItem);
			AssertEx.AreTsStringsEqual(((StTxtPara)footnote1Rev.ParagraphsOS.FirstItem).Contents.UnderlyingTsString,
				paraFn.Contents.UnderlyingTsString);
			VerifySegments(para1Curr, new string[] { null, "Before fn trans", null, "After fn trans" },
				new int[] { 1, "Before fn ".Length, 1, "After fn".Length }, "restore footnote");
			VerifySegments(paraFn, new string[] { "New footnote trans" },
				new int[] { "New footnote text".Length }, "footnote itself");
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book and the revision have multiple changes in the same paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_MultipleChangesInPara()
		{
			CheckDisposed();

			string current = "Current";
			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionCurr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, current, Cache.DefaultVernWs, "Current Trans");
			AddRunAndSegmentToMockedPara(para1Curr, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, current, Cache.DefaultVernWs, "Current Trans");
			AddRunAndSegmentToMockedPara(para1Curr, "3", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, current, Cache.DefaultVernWs, "Current Trans");
			sectionCurr.AdjustReferences();

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara para1Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionRev.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Rev", Cache.DefaultVernWs, "Rev Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Abc", Cache.DefaultVernWs, "Abc Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "3", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Rev", Cache.DefaultVernWs, "Rev Trans");
			sectionRev.AdjustReferences();

			// Detect differences and verify that they are correct
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(3, m_bookMerger.Differences.Count);

			// The actual diffs are verified by a similar non-BT test.

			// The second diff should be a text difference in verse two
			Difference firstDiff = m_bookMerger.Differences.MoveFirst();
			Difference secondDiff = m_bookMerger.Differences.MoveNext();
			Difference thirdDiff = m_bookMerger.Differences.MoveNext();

			// Do the "ReplaceCurrentWithRevision" action on middle diff
			// and verify its result
			m_bookMerger.ReplaceCurrentWithRevision(secondDiff);
			Assert.AreEqual(2, m_bookMerger.Differences.Count);

			StTxtPara paraCurr = new StTxtPara(Cache, para1Curr.Hvo);
			Assert.AreEqual("1Current2Abc3Current", paraCurr.Contents.Text);

			VerifySegments(para1Curr, new string[] { null, "Current Trans", null, "Abc Trans", null, "Current Trans"},
				new int[] { 1, current.Length, 1, "Abc".Length, 1, current.Length }, "middle of 3 diffs");


			// Do the replace on remaining diffs, in any order
			m_bookMerger.ReplaceCurrentWithRevision(thirdDiff);
			m_bookMerger.ReplaceCurrentWithRevision(firstDiff);

			VerifySegments(para1Curr, new string[] { null, "Rev Trans", null, "Abc Trans", null, "Rev Trans" },
				new int[] { 1, "Rev".Length, 1, "Abc".Length, 1, "Rev".Length }, "three diffs");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current paragraph has a missing verse in the middle of the paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_VerseMissingInCurrent_MidPara()
		{
			CheckDisposed();

			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionCurr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Verse1", Cache.DefaultVernWs, "Verse1 Trans");
			AddRunAndSegmentToMockedPara(para1Curr, "3", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Verse3", Cache.DefaultVernWs, "Verse3 Trans");
			sectionCurr.AdjustReferences();

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara para1Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionRev.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Verse1", Cache.DefaultVernWs, "Verse1 Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Verse2", Cache.DefaultVernWs, "Verse2 Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "3", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Verse3Rev", Cache.DefaultVernWs, "Verse3Rev Trans");
			sectionRev.AdjustReferences();

			// Detect differences and verify that they are correct
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(2, m_bookMerger.Differences.Count);

			// Verse 2 is missing in the current
			Difference firstDiff = m_bookMerger.Differences.MoveFirst();

			// Verse 3 has a text difference
			Difference secondDiff = m_bookMerger.Differences.MoveNext();

			// Do the "ReplaceCurrentWithRevision" action on first diff
			m_bookMerger.ReplaceCurrentWithRevision(firstDiff);

			// Verify the changed paragraph
			StTxtPara paraCurr = new StTxtPara(Cache, para1Curr.Hvo);
			Assert.AreEqual("1Verse12Verse23Verse3", paraCurr.Contents.Text);

			VerifySegments(para1Curr, new string[] { null, "Verse1 Trans", null, "Verse2 Trans", null, "Verse3 Trans" },
				new int[] { 1, "Verse1".Length, 1, "Verse2".Length, 1, "Verse3".Length }, "insert verse");

			// Do the replace on remaining diff
			m_bookMerger.ReplaceCurrentWithRevision(secondDiff);

			// Recheck that Current is now identical to Revision
			VerifySegments(para1Curr, new string[] { null, "Verse1 Trans", null, "Verse2 Trans", null, "Verse3Rev Trans" },
				new int[] { 1, "Verse1".Length, 1, "Verse2".Length, 1, "Verse3Rev".Length }, "mod final verse");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// revision has an additional verse at the end of the last paragraph, which is missing
		/// in the current book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_VerseMissingInCurrent_EndOfLastPara()
		{
			CheckDisposed();

			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionCurr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Verse1", Cache.DefaultVernWs, "Verse1 Trans");
			AddRunAndSegmentToMockedPara(para1Curr, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Verse2", Cache.DefaultVernWs, "Verse2 Trans");
			sectionCurr.AdjustReferences();

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara para1Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionRev.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Verse1", Cache.DefaultVernWs, "Verse1 Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Verse2", Cache.DefaultVernWs, "Verse2 Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "3", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Verse3", Cache.DefaultVernWs, "Verse3 Trans");
			sectionRev.AdjustReferences();

			// Detect differences and verify that they are correct
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Difference diff = m_bookMerger.Differences.MoveFirst();

			// Do the "ReplaceCurrentWithRevision" action on diff
			m_bookMerger.ReplaceCurrentWithRevision(diff);
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// Verify the changed paragraph
			VerifySegments(para1Curr, new string[] { null, "Verse1 Trans", null, "Verse2 Trans", null, "Verse3 Trans" },
				new int[] { 1, "Verse1".Length, 1, "Verse2".Length, 1, "Verse3".Length }, "insert final verse");
		}

		// TODO: Currently we don't handle the following case correctly!!
		// Revision: para1(v1, v2) para2(v3)
		// Current: para1(v1) para2(v3)
		// ReplaceCurrentWithRevision results in para1(v1) para2(v2, v3).
		// public void ReplaceCurWithRev_VerseMissingInCurrent_ParaFollows()

		//TODO:
		// public void ReplaceCurWithRev_VerseAddedInCurrent()


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book and the revision have a single difference in the text of the book title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_Title()
		{
			CheckDisposed();

			m_scrInMemoryCache.AddTitleToMockedBook(m_genesis.Hvo, "My book title");
			StTxtPara para1Curr = ((StTxtPara)m_genesis.TitleOA.ParagraphsOS[0]);
			AddSegment(para1Curr, 0, "My book title Trans");
			m_scrInMemoryCache.AddTitleToMockedBook(m_genesisRevision.Hvo, "My Genesis title");
			StTxtPara para1Rev = ((StTxtPara)m_genesisRevision.TitleOA.ParagraphsOS[0]);
			AddSegment(para1Rev, 0, "My Genesis title Trans");

			// Create Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			m_scrInMemoryCache.AddParaToMockedSectionContent(sectionCurr.Hvo, ScrStyleNames.NormalParagraph);
			sectionCurr.AdjustReferences();

			// Create Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			m_scrInMemoryCache.AddParaToMockedSectionContent(sectionRev.Hvo, ScrStyleNames.NormalParagraph);
			sectionRev.AdjustReferences();

			// Detect differences and replace with current
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Difference diff = m_bookMerger.Differences.MoveFirst();
			m_bookMerger.ReplaceCurrentWithRevision(diff);


			// Verify the changed paragraph
			Assert.AreEqual("My Genesis title", para1Curr.Contents.Text);

			VerifySegments(para1Curr, new string[] { "My Genesis title Trans" },
				new int[] { "My Genesis title".Length}, "book title");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book and the revision have a single difference in the text of the section
		/// head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_SectionHead()
		{
			CheckDisposed();

			// Create a section for both the current version of Genesis and the stored revision.
			IScrSection sectionCurr = CreateSection(m_genesis, "My section head!");
			StTxtPara para1Curr = ((StTxtPara)sectionCurr.HeadingOA.ParagraphsOS[0]);
			AddSegment(para1Curr, 0, "My section head Trans");
			AddRunAndSegmentToMockedPara(para1Curr, "An unchanged sentence", Cache.DefaultVernWs, "Current Trans");
			m_scrInMemoryCache.AddParaToMockedSectionContent(sectionCurr.Hvo, ScrStyleNames.NormalParagraph);
			sectionCurr.AdjustReferences();

			// Create Rev section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara para1Rev = ((StTxtPara)sectionRev.HeadingOA.ParagraphsOS[0]);
			AddSegment(para1Rev, 0, "My aching head Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "An unchanged sentence", Cache.DefaultVernWs, "Rev Trans");
			m_scrInMemoryCache.AddParaToMockedSectionContent(sectionRev.Hvo, ScrStyleNames.NormalParagraph);
			sectionRev.AdjustReferences();

			// Detect differences and verify that they are correct
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();

			// Do the "ReplaceCurrentWithRevision" action
			m_bookMerger.ReplaceCurrentWithRevision(diff);
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// Verify the changed section head
			Assert.AreEqual("My aching head!An unchanged sentence",
				((StTxtPara)sectionCurr.HeadingOA.ParagraphsOS[0]).Contents.Text);

			// We DO revise the BT of the unchanged sentence, since it is part of a single segment sequence with
			// the one we are replacing.
			VerifySegments(para1Curr, new string[] { "My aching head Trans", "Rev Trans" },
				new int[] { "My aching head!".Length, "An unchanged sentence".Length }, "heading");

		}

		void AddVerseSegment(StTxtPara para, int chapter, int verse, string verseText, string segText)
		{
			int ichMin = para.Contents.Length;
			AddVerse(para, chapter, verse, verseText);
			int ichEndVerse = para.Contents.Length - verseText.Length;
			if (ichEndVerse > ichMin)
				AddSegment(para, ichMin, ichEndVerse, null);
			AddSegment(para, ichEndVerse, segText);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book has a paragraph split at a verse boundary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_ParaSplitAtVerseStart()
		{
			CheckDisposed();

			// Build Current section - two paragraphs with verses 1, 2 and 3 and split after verse 1
			IScrSection sectionCur = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = AddPara(sectionCur);
			AddVerseSegment(para1Curr, 0, 1, "verse one. ", "verse one Trans. ");
			StTxtPara para2Curr = AddPara(sectionCur);
			AddVerseSegment(para2Curr, 0, 2, "verse two. ", "verse two Trans. ");
			AddVerseSegment(para2Curr, 0, 3, "verse three.", "verse three Trans. ");
			sectionCur.AdjustReferences();

			// Build Revision section - a single para with three verses
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara paraRev = AddPara(sectionRev);
			AddVerseSegment(paraRev, 0, 1, "verse one. ", "verse one Trans. ");
			AddVerseSegment(paraRev, 0, 2, "verse two. ", "verse two Trans. ");
			AddVerseSegment(paraRev, 0, 3, "verse three.", "verse three Trans. ");
			sectionRev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// Get the first difference, verify it, and do a ReplaceCurrentWithRevision
			// to simulate clicking the "revert to old" button
			Difference diff = m_bookMerger.Differences.MoveFirst();

			m_bookMerger.ReplaceCurrentWithRevision(diff); // we expect this to merge the current paras
			Assert.AreEqual(1, sectionCur.ContentOA.ParagraphsOS.Count);

			VerifySegments(para1Curr, new string[] {null, "verse one Trans. ", null, "verse two Trans. ", null, "verse three Trans. "},
				new int[] { 1, "verse one. ".Length, 1, "verse two. ".Length, 1, "verse three.".Length }, "merge paras");

		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book has a paragraph split at a verse boundary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_ParaSplitMidVerse()
		{
			CheckDisposed();

			// Build Current section - two paragraphs with verses 1, 2 and 3 and split mid verse 2
			IScrSection sectionCur = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = AddPara(sectionCur);
			AddVerseSegment(para1Curr, 0, 1, "verse one. ", "verse one Trans. ");
			AddVerseSegment(para1Curr, 0, 2, "verse two. ", "verse two Trans. ");
			StTxtPara para2Curr = AddPara(sectionCur);
			AddRunAndSegmentToMockedPara(para2Curr, "more of verse 2. ", Cache.DefaultVernWs, "more Trans");
			AddVerseSegment(para2Curr, 0, 3, "verse three.", "verse three Trans. ");
			sectionCur.AdjustReferences();

			// Build Revision section - a single para with three verses
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara paraRev = AddPara(sectionRev);
			AddVerseSegment(paraRev, 0, 1, "verse one. ", "verse one Trans. ");
			AddVerseSegment(paraRev, 0, 2, "verse two. ", "verse two Trans. ");
			AddRunAndSegmentToMockedPara(paraRev, "more of verse 2. ", Cache.DefaultVernWs, "more Trans");
			AddVerseSegment(paraRev, 0, 3, "verse three.", "verse three Trans. ");
			sectionRev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// Get the first difference, verify it, and do a ReplaceCurrentWithRevision
			// to simulate clicking the "revert to old" button
			Difference diff = m_bookMerger.Differences.MoveFirst();

			m_bookMerger.ReplaceCurrentWithRevision(diff); // we expect this to merge the current paras
			Assert.AreEqual(1, sectionCur.ContentOA.ParagraphsOS.Count);

			VerifySegments(para1Curr, new string[] { null, "verse one Trans. ", null, "verse two Trans. ", "more Trans", null, "verse three Trans. " },
				new int[] { 1, "verse one. ".Length, 1, "verse two. ".Length, "more of verse 2. ".Length, 1, "verse three.".Length }, "merge paras");
			}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book has a paragraph split at a verse boundary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_ParaSplitMidVerse_MergeSegs()
		{
			CheckDisposed();

			// Build Current section - two paragraphs with verses 1, 2 and 3 and split mid verse 2
			IScrSection sectionCur = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = AddPara(sectionCur);
			AddVerseSegment(para1Curr, 0, 1, "verse one. ", "verse one Trans. ");
			AddVerseSegment(para1Curr, 0, 2, "verse two ", "verse two Trans. "); // verse 2 does not end in period!
			StTxtPara para2Curr = AddPara(sectionCur);
			AddRunAndSegmentToMockedPara(para2Curr, "more of verse 2. ", Cache.DefaultVernWs, "more Trans");
			AddVerseSegment(para2Curr, 0, 3, "verse three.", "verse three Trans. ");
			sectionCur.AdjustReferences();

			// Build Revision section - a single para with three verses
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara paraRev = AddPara(sectionRev);
			AddVerseSegment(paraRev, 0, 1, "verse one. ", "verse one Trans. ");
			AddVerseSegment(paraRev, 0, 2, "verse two more of verse 2. ", "verse two Trans. more Trans");
			AddVerseSegment(paraRev, 0, 3, "verse three.", "verse three Trans. ");
			sectionRev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// Get the first difference, verify it, and do a ReplaceCurrentWithRevision
			// to simulate clicking the "revert to old" button
			Difference diff = m_bookMerger.Differences.MoveFirst();

			m_bookMerger.ReplaceCurrentWithRevision(diff); // we expect this to merge the current paras
			Assert.AreEqual(1, sectionCur.ContentOA.ParagraphsOS.Count);

			VerifySegments(para1Curr, new string[] { null, "verse one Trans. ", null, "verse two Trans. more Trans", null, "verse three Trans. " },
				new int[] { 1, "verse one. ".Length, 1, "verse two more of verse 2. ".Length, 1, "verse three.".Length }, "merge paras");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the current and revision Scripture data so that there are differences in
		/// adjacent verses.
		/// </summary>
		/// <param name="para1Curr">The first current para.</param>
		/// <param name="para2Curr">The second current para.</param>
		/// <param name="para1Rev">The first revision para.</param>
		/// <param name="para2Rev">The second revision para.</param>
		/// ------------------------------------------------------------------------------------
		private void CreateData_ParaSplitAtVerseStart_AdjacentChanges(out StTxtPara para1Curr,
			out StTxtPara para2Curr, out StTxtPara para1Rev, out StTxtPara para2Rev)
		{
			// Create a section for both the current version of Genesis and the stored revision.
			IScrSection sectionCur = CreateSection(m_genesis, "My Beautiful Verses");
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My Beautiful Verses");

			// Create the "current" paragraphs
			para1Curr = AddPara(sectionCur);
			AddVerseSegment(para1Curr, 20, 1, "The disciples were all together. ", "Disciples together Trans");//unusual trailing space
			para2Curr = AddPara(sectionCur);
			AddVerseSegment(para2Curr, 0, 2, "Suddenly there was a strong wind noise. ", "Suddenly strong wind Trans");
			AddVerseSegment(para2Curr, 0, 3, "They saw tongues of fire. ", "Tongues fire Trans");
			sectionCur.AdjustReferences();

			// Build up the "revision" paragraph
			para1Rev = AddPara(sectionRev);
			AddVerseSegment(para1Rev, 20, 1, "They were all together. ", "They together Trans");
			AddVerseSegment(para1Rev, 0, 2, "Suddenly there was a violent wind sound. ", "Suddenly violent wind Trans");
			AddVerseSegment(para1Rev, 0, 3, "They saw tongues of fire. ", "Tongues fire Trans");
			para2Rev = AddPara(sectionRev);
			AddVerseSegment(para2Rev, 0, 4, "They were filled with the Holy Spirit and spoke in tongues.", "Filled Trans");
			sectionRev.AdjustReferences();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the current
		/// has a new paragraph split at a verse boundary. There are text changes in the ScrVerses
		/// before and after the new paragraph split and a paragraph is missing. Differences
		/// are reverted in order.
		/// Current
		/// 201The disciples were all together. "
		/// 2Suddenly there was a strong wind noise. 3They saw tongues of fire. "
		/// Revision
		/// 201They were all together. 2Suddenly there was a violent wind sound. 3They saw tongues of fire. "
		/// 4They were filled with the Holy Spirit and spoke in tongues."
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_ParaSplitAtVerseStart_AdjacentChanges1()
		{
			CheckDisposed();

			// Create data and confirm differences.
			StTxtPara para1Curr;
			StTxtPara para2Curr;
			StTxtPara para1Rev;
			StTxtPara para2Rev;
			CreateData_ParaSplitAtVerseStart_AdjacentChanges(out para1Curr, out para2Curr, out para1Rev, out para2Rev);

			m_bookMerger.DetectDifferences(null);

			// Revert differences from first to last.
			ScrSection sectionCurr = new ScrSection(m_scrInMemoryCache.Cache, para1Curr.Owner.OwnerHVO);
			int hvoPara1Curr = para1Curr.Hvo;
			Difference diff1 = m_bookMerger.Differences.MoveFirst();
			Difference diff2 = m_bookMerger.Differences.MoveNext();
			Difference diff3 = m_bookMerger.Differences.MoveNext();
			Difference diff4 = m_bookMerger.Differences.MoveNext();

			// Revert text difference in verse 1.
			m_bookMerger.ReplaceCurrentWithRevision(diff1);
			para1Curr = (StTxtPara)sectionCurr.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("201They were all together. ", para1Curr.Contents.Text);

			VerifySegments(para1Curr, new string[] {null, "They together Trans"},
				new int[] { 3, "They were all together. ".Length }, "v1 text");

			// Revert paragraph split at end of verse 1.
			m_bookMerger.ReplaceCurrentWithRevision(diff2);
			Assert.AreEqual(1, sectionCurr.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("201They were all together. 2Suddenly there was a strong wind noise. " +
				"3They saw tongues of fire. ",
				((StTxtPara)sectionCurr.ContentOA.ParagraphsOS[0]).Contents.Text);

			VerifySegments(para1Curr, new string[] {null, "They together Trans", null, "Suddenly strong wind Trans", null, "Tongues fire Trans"},
				new int[] { 3, "They were all together. ".Length, 1, "Suddenly there was a strong wind noise. ".Length,
				1, "They saw tongues of fire. ".Length}, "para split v1");

			// Revert text difference in verse 2.
			m_bookMerger.ReplaceCurrentWithRevision(diff3);
			Assert.AreEqual("201They were all together. 2Suddenly there was a violent wind sound. " +
				"3They saw tongues of fire. ", para1Curr.Contents.Text);
			VerifySegments(para1Curr, new string[] {null, "They together Trans", null, "Suddenly violent wind Trans", null, "Tongues fire Trans"},
				new int[] { 3, "They were all together. ".Length, 1, "Suddenly there was a violent wind sound. ".Length,
				1, "They saw tongues of fire. ".Length}, "v2 text");

			// Revert missing paragraph (verse 4).
			m_bookMerger.ReplaceCurrentWithRevision(diff4);
			Assert.AreEqual(2, sectionCurr.ContentOA.ParagraphsOS.Count);
			StTxtPara newParaCurr = (StTxtPara)sectionCurr.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("4They were filled with the Holy Spirit and spoke in tongues.", newParaCurr.Contents.Text);
			VerifySegments(newParaCurr, new string[] { null, "Filled Trans" },
				new int[] { 1, "They were filled with the Holy Spirit and spoke in tongues.".Length }, "add para 2");

		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the current
		/// has one verse in three paragraphs and when the corresponding revision verse has only one paragraph.
		/// There is a text change in the first and last parts of the verse. This test exercises a special
		/// case in MoveParaBt.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_MultiParasInVerse_OneToThreeParas_TextChanges()
		{
			// Create a section for both the current version of Genesis and the stored revision.
			IScrSection sectionCur = CreateSection(m_genesis, "My Beautiful Verses");
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My Beautiful Verses");

			// Build up the "current" paragraphs
			StTxtPara para1Curr = AddPara(sectionCur);
			AddVerseSegment(para1Curr, 30, 33, "For as churning the cream produces butter,", "churning trans");
			StTxtPara para2Curr = AddPara(sectionCur);
			AddVerseSegment(para2Curr, 0, 0, "and as twisting the nose produces blood,", "twisting trans");
			StTxtPara para3Curr = AddPara(sectionCur);
			AddVerseSegment(para3Curr, 0, 0, "then stirring up anger produces strife.", "stirring trans");
			sectionCur.AdjustReferences();

			// Build up the "revision" paragraph
			StTxtPara para1Rev = AddPara(sectionRev);
			AddVerseSegment(para1Rev, 30, 33, "For as churning the milk produces butter, and as twisting "
				+ "the nose produces blood, so stirring up anger produces strife.", "churning and twisting transstirring2 trans");
			sectionRev.AdjustReferences();

			m_bookMerger.DetectDifferences(null);

			// We expect one paragraph structure difference with three subdifferences.
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();

			// Revert the difference in verse 33: para split, and text changes in three
			// ScrVerses in the current
			ScrSection sectionCurr = new ScrSection(m_inMemoryCache.Cache, para1Curr.Owner.OwnerHVO);
			Assert.AreEqual(3, sectionCurr.ContentOA.ParagraphsOS.Count);
			m_bookMerger.ReplaceCurrentWithRevision(diff);

			Assert.AreEqual(1, sectionCurr.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("3033For as churning the milk produces butter, and as twisting "
				+ "the nose produces blood, so stirring up anger produces strife.",
				((StTxtPara)sectionCurr.ContentOA.ParagraphsOS[0]).Contents.Text);
			VerifySegments(para1Curr, new string[] { null, "churning and twisting transstirring2 trans" },
				new int[] { 4, ("For as churning the milk produces butter, and as twisting "
				+ "the nose produces blood, so stirring up anger produces strife.").Length }, "combine segs");

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book has a paragraph merge at a verse boundary. This tests the call to
		/// CopyFt from ReplaceCurrentWithRevision_CopyParaStructure.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_ParaMergeAtVerseStart()
		{
			CheckDisposed();

			// Build Current section - a single para with three verses
			IScrSection sectionCur = CreateSection(m_genesis, "My aching head!");
			StTxtPara para1Curr = AddPara(sectionCur);
			AddVerseSegment(para1Curr, 0, 1, "verse one. ", "verse one trans");
			AddVerseSegment(para1Curr, 0, 2, "verse two. ", "verse two trans");
			AddVerseSegment(para1Curr, 0, 3, "verse three.", "verse three trans");
			sectionCur.AdjustReferences();

			// Build Revision section - two paragraphs with verses 1, 2 and 3 and split after verse 1
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			StTxtPara para1Rev = AddPara(sectionRev);
			AddVerseSegment(para1Rev, 0, 1, "verse one. ", "verse one trans"); //has space; no text difference
			StTxtPara para2Rev = AddPara(sectionRev);
			AddVerseSegment(para2Rev, 0, 2, "verse two. ", "verse two trans");
			AddVerseSegment(para2Rev, 0, 3, "verse three.", "verse three trans");
			sectionCur.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// Revert
			Difference diff = m_bookMerger.Differences.MoveFirst();
			m_bookMerger.ReplaceCurrentWithRevision(diff); // we expect this to split the current para

			//verify the revert
			Assert.AreEqual(2, sectionCur.ContentOA.ParagraphsOS.Count);
			para1Curr = new StTxtPara(Cache, sectionCur.ContentOA.ParagraphsOS[0].Hvo);
			StTxtPara para2Curr = new StTxtPara(Cache, sectionCur.ContentOA.ParagraphsOS[1].Hvo);

			VerifySegments(para1Curr, new string[] { null, "verse one trans" },
				new int[] { 1, "verse one. ".Length }, "split para");
			VerifySegments(para2Curr, new string[] { null, "verse two trans", null, "verse three trans" },
				new int[] { 1, "verse two. ".Length, 1, "verse three.".Length }, "split para");
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ReplaceCurrentWithRevision method when sections are "missing in current"
		/// and will be inserted there.
		///
		/// revision        current
		/// 1:1
		/// 2:1              2:1
		/// 3:1,2
		///
		/// insert 1:1
		/// insert 3:1,2
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_SectionMissingInCurrent()
		{
			CheckDisposed();

			// Build the "current" section
			IScrSection section1Curr = CreateSection(m_genesis, "My Second Section");
			StTxtPara para1Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Curr.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1Curr, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1Curr, "1", ScrStyleNames.VerseNumber);
			AddSegment(para1Curr, 0, 2, null);
			AddRunAndSegmentToMockedPara(para1Curr, "This is the second section", Cache.DefaultVernWs, "second section trans");
			section1Curr.AdjustReferences();

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My First Section");
			StTxtPara para1Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Rev.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1Rev, "1", ScrStyleNames.VerseNumber);
			AddSegment(para1Rev, 0, 2, null);
			AddRunAndSegmentToMockedPara(para1Rev, "This is the first section", Cache.DefaultVernWs, "first section trans");
			section1Rev.AdjustReferences();

			IScrSection section2Rev = CreateSection(m_genesisRevision, "My Second Section");
			StTxtPara para2Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(section2Rev.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2Rev, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2Rev, "1", ScrStyleNames.VerseNumber);
			AddSegment(para2Rev, 0, 2, null);
			AddRunAndSegmentToMockedPara(para2Rev, "This is the second section", Cache.DefaultVernWs, "second section trans");
			section2Rev.AdjustReferences();

			IScrSection section3Rev = CreateSection(m_genesisRevision, "My Third Section");
			StTxtPara para3Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(section3Rev.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para3Rev, "3", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para3Rev, "1", ScrStyleNames.VerseNumber);
			AddSegment(para3Rev, 0, 2, null);
			AddRunAndSegmentToMockedPara(para3Rev, "This is the third section", Cache.DefaultVernWs, "3rd section trans");
			StTxtPara para3aRev = m_scrInMemoryCache.AddParaToMockedSectionContent(section3Rev.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para3aRev, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para3aRev, "This is the second para of the third section", Cache.DefaultVernWs, "p2 s3 trans");
			section3Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.AreEqual(2, m_bookMerger.Differences.Count);
			Difference diff1 = m_bookMerger.Differences.MoveFirst();
			Difference diff2 = m_bookMerger.Differences.MoveNext();

			// Revert the first difference, which should copy the first revision section to the current
			m_bookMerger.ReplaceCurrentWithRevision(diff1);
			IScrSection section = m_genesis.SectionsOS[0];
			StTxtPara para1 = ((StTxtPara) section.ContentOA.ParagraphsOS[0]);
			Assert.AreEqual("11This is the first section", para1.Contents.Text);

			VerifySegments(para1, new string[] { null, "first section trans" },
				new int[] { 2, "This is the first section".Length }, "insert section");

			// Revert the second difference, which should copy the last revision section to the current
			m_bookMerger.ReplaceCurrentWithRevision(diff2);
			section = m_genesis.SectionsOS[2];

			StTxtPara para2 = ((StTxtPara)section.ContentOA.ParagraphsOS[0]);
			Assert.AreEqual("31This is the third section", para2.Contents.Text);
			StTxtPara para3 = ((StTxtPara)section.ContentOA.ParagraphsOS[1]);
			Assert.AreEqual("2This is the second para of the third section", para3.Contents.Text);
			VerifySegments(para2, new string[] { null, "3rd section trans" },
				new int[] { 2, "This is the third section".Length }, "insert 3rd section p1");
			VerifySegments(para3, new string[] { null, "p2 s3 trans" },
				new int[] { 1, "This is the second para of the third section".Length }, "insert 3rd section p2");
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ReplaceCurrentWithRevision method for sections when multiple adjacent
		/// sections are deleted in the Current.
		///
		/// revision        current
		///					 1
		///					 2
		///					 3
		///	 4				 4
		///                  5
		///					 6
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_Sections_DeleteMultiple()
		{
			CheckDisposed();

			// Build the "current" sections: 1-6
			IScrSection section1Curr = CreateSection(m_genesis, "My Section");
			StTxtPara para1Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(section1Curr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);

			IScrSection section2Curr = CreateSection(m_genesis, "My Section");
			StTxtPara para2Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(section2Curr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para2Curr, "2", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para2Curr, "Contents of section 2 para 1", Cache.DefaultVernWs, "sec 2 p1 trans");
			StTxtPara para2b = m_scrInMemoryCache.AddParaToMockedSectionContent(section2Curr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para2b, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para2b, "Section 2 para 2", Cache.DefaultVernWs, "sec 2.2 trans");

			IScrSection section3Curr = CreateSection(m_genesis, "My Section");
			StTxtPara para3Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(section3Curr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para3Curr, "3", ScrStyleNames.ChapterNumber, null);

			IScrSection section4Curr = CreateSection(m_genesis, "My Section");
			StTxtPara para4Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(section4Curr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para4Curr, "4", ScrStyleNames.ChapterNumber, null);

			IScrSection section5Curr = CreateSection(m_genesis, "My Section");
			StTxtPara para5Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(section5Curr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para5Curr, "5", ScrStyleNames.ChapterNumber, null);

			IScrSection section6Curr = CreateSection(m_genesis, "My Section");
			StTxtPara para6Curr = m_scrInMemoryCache.AddParaToMockedSectionContent(section6Curr.Hvo, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para6Curr, "6", ScrStyleNames.ChapterNumber, null);

			section1Curr.AdjustReferences();
			section2Curr.AdjustReferences();
			section3Curr.AdjustReferences();
			section4Curr.AdjustReferences();
			section5Curr.AdjustReferences();
			section6Curr.AdjustReferences();

			// Build the "revision" section: 4
			IScrSection section4Rev = CreateSection(m_genesisRevision, "My Section");
			StTxtPara para4Rev = m_scrInMemoryCache.AddParaToMockedSectionContent(section4Rev.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para4Rev, "4", ScrStyleNames.ChapterNumber);
			section4Rev.AdjustReferences();

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.AreEqual(2, m_bookMerger.Differences.Count);
			Difference diff1 = m_bookMerger.Differences.MoveFirst();
			Difference diff2 = m_bookMerger.Differences.MoveNext();

			int kflidSegments = StTxtPara.SegmentsFlid(Cache);
			int kflidFT = StTxtPara.SegmentFreeTranslationFlid(Cache);
			int hvoSegS1 = (Cache.GetVectorItem(para1Curr.Hvo, kflidSegments, 0));
			int hvoSegS2 = (Cache.GetVectorItem(para2Curr.Hvo, kflidSegments, 1));
			int hvoSegS2b = (Cache.GetVectorItem(para2b.Hvo, kflidSegments, 1));
			int hvoFtS2 = Cache.GetObjProperty(hvoSegS2, kflidFT);
			int hvoFtS2b = Cache.GetObjProperty(hvoSegS2b, kflidFT);
			int hvoSegS6 = (Cache.GetVectorItem(para6Curr.Hvo, kflidSegments, 0));
			Assert.AreNotEqual(0, Cache.GetClassOfObject(hvoSegS1), "segment should have known class before deletion");

			// Revert all the "added in current" diffs, to delete them from the current
			m_bookMerger.ReplaceCurrentWithRevision(diff1);
			m_bookMerger.ReplaceCurrentWithRevision(diff2);

			Assert.AreEqual(1, m_genesis.SectionsOS.Count);

			// Verify that the relevant segments got deleted. (There are others, but this is a good
			// representative sample.)
			VerifyDel(hvoSegS1);
			VerifyDel(hvoSegS2);
			VerifyDel(hvoSegS2b);
			VerifyDel(hvoFtS2);
			VerifyDel(hvoFtS2b);
			VerifyDel(hvoSegS6);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the current
		/// has a verse in one paragraph with correlation to the first paragraph in the revision
		/// verse. Two paragraphs are added in the revision verse. There are adjacent changes on
		/// either side of the cluster.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_MultiParasInVerse_ThreeToOneParas_CorrFirst()
		{
			// Create a section for both the current version of Genesis and the stored revision.
			IScrSection sectionCurr = CreateSection(m_genesis, "My Beautiful Verses");
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My Beautiful Verses");

			// Build up the "current" paragraph
			StTxtPara para1Curr = AddPara(sectionCurr);
			AddVerseSegment(para1Curr, 30, 32, "Verse 32. ", "V32 trans");
			AddVerseSegment(para1Curr, 0, 33, "For as churning the milk produces good butter, ", "Churning milk trans");
			AddVerseSegment(para1Curr, 0, 34, "Verse 34.", "V34 trans");
			sectionCurr.AdjustReferences();

			// Build up the "revision" paragraphs
			StTxtPara para1Rev = AddPara(sectionRev);
			AddVerseSegment(para1Rev, 30, 32, "Versie 3@. ", "Versie 3@ trans");
			AddVerseSegment(para1Rev, 0, 33, "For as churning the cream produces good butter, ", "Churning cream trans");
			StTxtPara para2Rev = AddPara(sectionRev);
			AddVerseSegment(para2Rev, 0, 0, "and as twisting the nose produces blood,", "Twisting trans");
			StTxtPara para3Rev = AddPara(sectionRev);
			AddVerseSegment(para3Rev, 0, 0, "then stirring up anger produces strife. ", "Stirring trans");
			AddVerseSegment(para3Rev, 0, 34, "Versify thirty-four.", "Versify 34 trans");
			StTxtPara para4Rev = AddPara(sectionRev);
			AddVerseSegment(para4Rev, 0, 35, "Verse 35.", "V35 trans");
			sectionRev.AdjustReferences();

			m_bookMerger.DetectDifferences(null);

			Assert.AreEqual(5, m_bookMerger.Differences.Count);

			// text diff in Verse 32
			Difference diff1 = m_bookMerger.Differences.MoveFirst();
			// text diff begining in verse 33
			Difference diff2 = m_bookMerger.Differences.MoveNext();
			// We expect one paragraph structure difference with three subdifferences.
			Difference diff3 = m_bookMerger.Differences.MoveNext();
			// text diff in verse 34
			Difference diff4 = m_bookMerger.Differences.MoveNext();
			// paragraph missing in current
			Difference diff5 = m_bookMerger.Differences.MoveNext();

			// Revert text change in verse 32
			m_bookMerger.ReplaceCurrentWithRevision(diff1);
			StTxtPara para1 = ((StTxtPara) sectionCurr.ContentOA.ParagraphsOS[0]);
			Assert.AreEqual("3032Versie 3@. 33For as churning the milk produces good butter, "
				+ "34Verse 34.", para1.Contents.Text);
			VerifySegments(para1, new string[] { null, "Versie 3@ trans", null, "Churning milk trans",  null, "V34 trans"},
				new int[] { 4, "Versie 3@. ".Length, 2, "For as churning the milk produces good butter, ".Length, 2, "Verse 34.".Length }, "revert 32");

			// Revert text change in verse 33
			m_bookMerger.ReplaceCurrentWithRevision(diff2);
			Assert.AreEqual(1, sectionCurr.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("3032Versie 3@. 33For as churning the cream produces good butter, "
				+ "34Verse 34.", para1.Contents.Text);
			VerifySegments(para1, new string[] { null, "Versie 3@ trans", null, "Churning cream trans",  null, "V34 trans"},
				new int[] { 4, "Versie 3@. ".Length, 2, "For as churning the cream produces good butter, ".Length, 2,  "Verse 34.".Length},
				"revert 33");

			// Revert the complex difference in verse 33: para's missing in current
			m_bookMerger.ReplaceCurrentWithRevision(diff3);
			// We expect the one paragraph to be split into three paragraphs and text changes to be made.
			StTxtPara para2 = ((StTxtPara) sectionCurr.ContentOA.ParagraphsOS[1]);
			StTxtPara para3 = ((StTxtPara) sectionCurr.ContentOA.ParagraphsOS[2]);
			Assert.AreEqual(3, sectionCurr.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("3032Versie 3@. 33For as churning the cream produces good butter, ", para1.Contents.Text);
			Assert.AreEqual("and as twisting the nose produces blood,", para2.Contents.Text);
			Assert.AreEqual("then stirring up anger produces strife. 34Verse 34.", para3.Contents.Text);
			VerifySegments(para1, new string[] { null, "Versie 3@ trans", null, "Churning cream trans"},
				new int[] { 4, "Versie 3@. ".Length, 2, "For as churning the cream produces good butter, ".Length},
				"revert paras 1");
			VerifySegments(para2, new string[] { "Twisting trans"},
				new int[] {"and as twisting the nose produces blood,".Length}, "revert paras 2");
			VerifySegments(para3, new string[] { "Stirring trans", null, "V34 trans"},
				new int[] {"then stirring up anger produces strife. ".Length, 2, "Verse 34.".Length}, "revert paras 3");

			// Revert text change in verse 34
			m_bookMerger.ReplaceCurrentWithRevision(diff4);
			Assert.AreEqual("then stirring up anger produces strife. 34Versify thirty-four.",
				para3.Contents.Text);
			VerifySegments(para3, new string[] { "Stirring trans", null, "Versify 34 trans"},
				new int[] {"then stirring up anger produces strife. ".Length, 2, "Versify thirty-four.".Length}, "revert 34");

			// Revert missing para in current
			m_bookMerger.ReplaceCurrentWithRevision(diff5);
			Assert.AreEqual(4, sectionCurr.ContentOA.ParagraphsOS.Count);
			StTxtPara para4 = ((StTxtPara) sectionCurr.ContentOA.ParagraphsOS[3]);
			Assert.AreEqual("35Verse 35.",
				((StTxtPara)sectionCurr.ContentOA.ParagraphsOS[3]).Contents.Text);
			VerifySegments(para4, new string[] { null, "V35 trans"},
				new int[] {2, "Verse 35.".Length}, "insert para");

		}

		#endregion
	}
}
