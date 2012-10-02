// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2007' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SegmentedBtMergeTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;

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

		#region Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an additional analysis writing system to make it harder for the tests to pass.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(
					Cache.ServiceLocator.WritingSystemManager.Get("es"));
			});
	}

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to only create a book with no content, heading, title, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_genesis = AddBookToMockedScripture(1, "Genesis");
			m_genesisRevision = AddArchiveBookToMockedScripture(1, "Genesis");

			//Philemon
			IScrBook book = AddBookToMockedScripture(57, "Philemon");
			AddTitleToMockedBook(book, "Philemon");

			IScrSection section = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section, "Paul tells people", "Section Head");
			IScrTxtPara para = AddParaToMockedSectionContent(section, "Paragraph");
			AddRunToMockedPara(para, "1", "Chapter Number");
			AddRunToMockedPara(para, "1", "Verse Number");
			AddRunToMockedPara(para, "and the earth was without form and void and darkness covered the face of the deep", null);

			IScrSection section2 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section2, "Paul tells people more", "Section Head");
			IScrTxtPara para2 = AddParaToMockedSectionContent(section2, "Paragraph");
			AddRunToMockedPara(para2, "2", "Chapter Number");
			AddRunToMockedPara(para2, "1", "Verse Number");
			AddRunToMockedPara(para2, "paul expounds on the nature of reality", null);
			IScrTxtPara para3 = AddParaToMockedSectionContent(section2, "Paragraph");
			AddRunToMockedPara(para3, "2", "Verse Number");
			AddRunToMockedPara(para3, "the existentialists are all wrong", null);

			//Jude
			IScrBook jude = AddBookToMockedScripture(65, "Jude");
			IScrSection judeSection = AddSectionToMockedBook(jude);
			AddTitleToMockedBook(jude, "Jude");
			AddSectionHeadParaToSection(judeSection, "Introduction", "Section Head");
			IScrTxtPara judePara = AddParaToMockedSectionContent(judeSection, "Intro Paragraph");
			AddRunToMockedPara(judePara, "The Letter from Jude was written to warn against" +
				" false teachers who claimed to be believers. In this brief letter, which is similar in" +
				" content to 2 Peter the writer encourages his readers “to fight on for the faith which" +
				" once and for all God has given to his people.", null);
		}
		#endregion	}

		#region helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that object has been deleted. This is a rather kludgy way to do it, but the memory cache
		/// we're using doesn't have a useful implementation of IsValidObject.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// ------------------------------------------------------------------------------------
		private static void VerifyDel(ICmObject obj)
		{
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, obj.Hvo, "object should have been deleted " + obj);
		}

		void AddVerseSegment(IScrTxtPara para, int chapter, int verse, string verseText, string freeTrans)
		{
			int ichMin = para.Contents.Length;
			AddVerse(para, chapter, verse, verseText);
			int ichEndVerse = para.Contents.Length - verseText.Length;
			if (ichEndVerse > ichMin)
				AddSegmentTranslations(para, ichMin, null);
			AddSegmentTranslations(para, ichEndVerse, freeTrans);
		}

		void AddRunAndSegmentToMockedPara(IScrTxtPara para, string runText, string runStyleName, string freeTrans)
		{
			int ichMin = para.Contents.Length;
			AddRunToMockedPara(para, runText, runStyleName);
			AddSegmentTranslations(para, ichMin, freeTrans);
		}

		void AddRunAndSegmentToMockedPara(IScrTxtPara para, string runText, int ws, string freeTrans)
		{
			int ichMin = para.Contents.Length;
			AddRunToMockedPara(para, runText, ws);
			AddSegmentTranslations(para, ichMin, freeTrans);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the segment translations with notes and analyses.
		/// </summary>
		/// <param name="para">The para.</param>
		/// <param name="ichMin">The beginning character offset.</param>
		/// <param name="freeTrans">The free translation.</param>
		/// ------------------------------------------------------------------------------------
		private void AddSegmentTranslations(IScrTxtPara para, int ichMin, string freeTrans)
		{
			// In the new FDO we can't really add arbitrary segments anymore. FDO now keeps
			// track of the paragraph content changes and creates segments for them on the
			// fly. The best we can do anymore is to find a segment that matches what we
			// think we want (a segment that starts at ichMin) and set the free translation
			// of that segment.
			ISegment foundSegment = para.SegmentsOS.FirstOrDefault(segment => segment.BeginOffset == ichMin);
			Assert.IsNotNull(foundSegment, "Failed to find a segment at " + ichMin + " for paragraph with contents: " + para.Contents.Text);

			INote note = null;
			if (!string.IsNullOrEmpty(freeTrans))
			{
				// Add notes.
				note = Cache.ServiceLocator.GetInstance<INoteFactory>().Create();
				foundSegment.NotesOS.Add(note);
			}

			if (freeTrans != null)
			{
				// Add literal and free translations and note contents
				foreach (IWritingSystem ws in Cache.LanguageProject.AnalysisWritingSystems)
				{
					string trans = freeTrans;
					if (ws.Handle != Cache.DefaultAnalWs)
						trans = trans.Replace("Trans", "Trans " + ws.IcuLocale);
					foundSegment.FreeTranslation.set_String(ws.Handle, trans);
					foundSegment.LiteralTranslation.set_String(ws.Handle, trans.Replace("Trans", "Literal"));
					if (note != null)
					{
						ITsString tss = TsStrFactoryClass.Create().MakeString("Note" + ws.IcuLocale, ws.Handle);
						note.Content.set_String(ws.Handle, tss);
					}
				}
			}

			// Add analyses
			FdoTestHelper.CreateAnalyses(foundSegment, para.Contents, foundSegment.BeginOffset, foundSegment.EndOffset);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that the paragraph has the expected number of segments with the expected free
		/// and literal translations.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="translations">List of texts of the free translations.</param>
		/// <param name="lengths">List of lengths of the segments.</param>
		/// <param name="segNotes">The number of notes for each segment.</param>
		/// <param name="label">Descriptive label to indicate what kind of test this is for.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyTranslations(IScrTxtPara para, IList<string> translations,
			IList<int> lengths, IList<int> segNotes, string label)
		{
			VerifyTranslations(para, translations, lengths, segNotes, new int[0], label);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that the paragraph has the expected number of segments with the expected free
		/// and literal translations.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="translations">List of texts of the free translations.</param>
		/// <param name="lengths">List of lengths of the segments.</param>
		/// <param name="segNotes">The number of notes for each segment.</param>
		/// <param name="expectedWordforms">The indices of the words (relative to the paragraph)
		/// for which we expect wordforms instead of glosses.</param>
		/// <param name="label">Descriptive label to indicate what kind of test this is for.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyTranslations(IScrTxtPara para, IList<string> translations,
			IList<int> lengths, IList<int> segNotes, IList<int> expectedWordforms, string label)
		{
			Assert.AreEqual(translations.Count, para.SegmentsOS.Count);
			foreach (IWritingSystem ws in Cache.LanguageProject.AnalysisWritingSystems)
			{
				int cumLength = 0;
				StringBuilder btBuilder = new StringBuilder();
				for (int i = 0; i < translations.Count; i++)
				{
					ISegment seg = para.SegmentsOS[i];
					Assert.AreEqual(cumLength, seg.BeginOffset, label + " - beginOffset " + i);
					cumLength += lengths[i];
					Assert.AreEqual(cumLength, seg.EndOffset, label + " - endOffset " + i);

					string expectedBt = translations[i];
					if (translations[i] != null && ws.Handle != Cache.DefaultAnalWs)
						expectedBt = expectedBt.Replace("Trans", "Trans " + ws.IcuLocale);
					Assert.AreEqual(expectedBt, seg.FreeTranslation.get_String(ws.Handle).Text, label + " - free translation " + i);
					string expectedLiteralTrans = (expectedBt == null) ? null : expectedBt.Replace("Trans", "Literal");
					Assert.AreEqual(expectedLiteralTrans, seg.LiteralTranslation.get_String(ws.Handle).Text,
						label + " - literal translation " + i);

					if (!seg.IsLabel)
					{
						// Verify note added to first segment.
						Assert.AreEqual(segNotes[i], seg.NotesOS.Count, label + " - Wrong number of notes");
						foreach (INote note in seg.NotesOS)
							Assert.AreEqual("Note" + ws.IcuLocale, note.Content.get_String(ws.Handle).Text);
					}

					if (expectedBt == null)
						btBuilder.Append(para.SegmentsOS[i].BaselineText.Text);
					else
					{
						btBuilder.Append(expectedBt);
						if (i < translations.Count - 1 && !expectedBt.EndsWith(" "))
							btBuilder.Append(" ");
					}
				}
				Assert.AreEqual(btBuilder.ToString(), para.GetBT().Translation.get_String(ws.Handle).Text);
			}

			if (para.ParseIsCurrent)
			{
				for (int i = 0; i < translations.Count; i++)
				{
					ISegment seg = para.SegmentsOS[i];
					FdoTestHelper.VerifyAnalysis(seg, i, new int[0], expectedWordforms);
					int numberOfWordformsInSegment = seg.AnalysesRS.Count;
					for (int iExp = 0; iExp < expectedWordforms.Count; iExp++)
					{
						if (expectedWordforms[iExp] > numberOfWordformsInSegment)
							expectedWordforms[iExp] -= numberOfWordformsInSegment;
					}
				}
			}
		}

		private IScrFootnote AddFootnoteSegment(IScrTxtPara para1Curr, string noteText, string noteTrans)
		{
			int ichMin = para1Curr.Contents.Length;
			IScrFootnote footnote1Curr = AddFootnote(m_genesis, para1Curr, ichMin, noteText);
			AddSegmentTranslations((IScrTxtPara)footnote1Curr[0], 0, noteTrans);
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
		public void ReplaceCurWithRev_SimpleText_ParseIsCurrent()
		{
			ReplaceCurWithRev_SimpleText(true);
		}

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
			ReplaceCurWithRev_SimpleText(false);
		}

		private void ReplaceCurWithRev_SimpleText(bool fParseIsCurrent)
		{
			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Current.", Cache.DefaultVernWs, "Current Trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Rev.", Cache.DefaultVernWs, "Revised Trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

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
			IScrTxtPara paraNew = diff.ParaCurr;
			Assert.AreEqual(para1Curr, paraNew);
			Assert.AreEqual("1Rev.", paraNew.Contents.Text);
			// verify segment Bt also updated
			VerifyTranslations(para1Curr, new []{null, "Revised Trans"}, new []{1, para1Curr.Contents.Length - 1},
				new []{0, 1}, "simple text");
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
		public void ReplaceCurWithRev_DontEraseBT_ParseIsCurrent()
		{
			ReplaceCurWithRev_DontEraseBT(true);
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
			ReplaceCurWithRev_DontEraseBT(false);
		}

		private void ReplaceCurWithRev_DontEraseBT(bool fParseIsCurrent)
		{
			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Current.", Cache.DefaultVernWs, "Current Trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Rev.", Cache.DefaultVernWs, "");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

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
			IScrTxtPara paraNew = diff.ParaCurr;
			Assert.AreEqual(para1Curr, paraNew);
			Assert.AreEqual("1Rev.", paraNew.Contents.Text);
			// verify segment Bt also updated
			VerifyTranslations(para1Curr, new []{ null, "Current Trans" }, new []{ 1, para1Curr.Contents.Length - 1 },
				new []{0, 0}, "simple text");
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
		public void ReplaceCurWithRev_SimpleTextMoreSegsInRev_ParseIsCurrent()
		{
			ReplaceCurWithRev_SimpleTextMoreSegsInRev(true);
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
			ReplaceCurWithRev_SimpleTextMoreSegsInRev(false);
		}

		private void ReplaceCurWithRev_SimpleTextMoreSegsInRev(bool fParseIsCurrent)
		{
			string ouch = "Ouch! ";
			string hit = "It got hit.";
			string ball = "By a ball.";

			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, hit, Cache.DefaultVernWs, "It got hit Trans");
			AddRunAndSegmentToMockedPara(para1Curr, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, ball, Cache.DefaultVernWs, "By a ball Trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, ouch, Cache.DefaultVernWs, "Ouch Trans");
			AddRunAndSegmentToMockedPara(para1Rev, hit, Cache.DefaultVernWs, "It got hit Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, ball, Cache.DefaultVernWs, "By a ball Trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// quick check of the diffs
			Difference diff = m_bookMerger.Differences.MoveFirst();
			DiffTestHelper.VerifyParaDiff(diff, 01001001, DifferenceType.TextDifference,
				para1Curr, 1, 1, para1Rev, 1, 1 + ouch.Length);  // to end of space after "Ouch! "

			// Do the "ReplaceCurrentWithRevision" action
			m_bookMerger.ReplaceCurrentWithRevision(diff);
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// Verify the changed paragraph
			IScrTxtPara paraNew = diff.ParaCurr;
			Assert.AreEqual(para1Curr, paraNew);
			Assert.AreEqual("1Ouch! It got hit.2By a ball.", paraNew.Contents.Text);
			// verify segment Bt also updated
			VerifyTranslations(para1Curr, new []{ null, "Ouch Trans", "It got hit Trans", null, "By a ball Trans" },
				new []{ 1, ouch.Length, hit.Length, 1, ball.Length }, new []{0, 1, 1, 0, 1}, "extra segment");
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
		public void ReplaceCurWithRev_SimpleTextFewerSegsInRev_ParseIsCurrent()
		{
			ReplaceCurWithRev_SimpleTextFewerSegsInRev(true);
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
			ReplaceCurWithRev_SimpleTextFewerSegsInRev(false);
		}

		private void ReplaceCurWithRev_SimpleTextFewerSegsInRev(bool fParseIsCurrent)
		{
			string ouch = "Ouch! ";
			string hit = "It got hit.";
			string ball = "By a ball.";

			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, ouch, Cache.DefaultVernWs, "Ouch Trans");
			AddRunAndSegmentToMockedPara(para1Curr, hit, Cache.DefaultVernWs, "It got hit Trans");
			AddRunAndSegmentToMockedPara(para1Curr, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, ball, Cache.DefaultVernWs, "By a ball Trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, hit, Cache.DefaultVernWs, "It got hit Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, ball, Cache.DefaultVernWs, "By a ball Trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

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
			IScrTxtPara paraNew = diff.ParaCurr;
			Assert.AreEqual(para1Curr, paraNew);
			Assert.AreEqual("1It got hit.2By a ball.", paraNew.Contents.Text);
			// verify segment Bt also updated
			VerifyTranslations(para1Curr, new []{ null, "It got hit Trans", null, "By a ball Trans" },
				new []{ 1, hit.Length, 1, ball.Length }, new [] {0, 1, 0, 1}, "removed segment");
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
		public void ReplaceCurWithRev_DuplicateVerseInPara_ParseIsCurrent()
		{
			ReplaceCurWithRev_DuplicateVerseInPara(true);
		}

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
			ReplaceCurWithRev_DuplicateVerseInPara(false);
		}

		private void ReplaceCurWithRev_DuplicateVerseInPara(bool fParseIsCurrent)
		{
			// Create Scripture data in the current.
			IScrSection sectionCur = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Curr, 1, 1, "one ", "one trans");
			AddVerseSegment(para1Curr, 0, 2, "two ", "two trans");
			AddVerseSegment(para1Curr, 0, 3, "three ", "three trans");
			AddVerseSegment(para1Curr, 0, 4, "four ", "four trans");
			int ichTxtChgMin = para1Curr.Contents.Length;
			AddVerseSegment(para1Curr, 0, 5, "five", "five trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// Create Scripture data in the revision.
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Rev, 1, 1, "one ", "one trans");
			AddVerseSegment(para1Rev, 0, 2, "two ", "two trans");
			AddVerseSegment(para1Rev, 0, 3, "three ", "three trans");
			AddVerseSegment(para1Rev, 0, 4, "four ", "four trans");
			AddVerseSegment(para1Rev, 0, 4, "four again ", "four again trans");
			int ichTxtChgLimRev = para1Rev.Contents.Length;
			AddVerseSegment(para1Rev, 0, 5, "five", "five trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			IScrTxtPara para2Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para2Rev, 0, 6, "paragraph to restore from the revision.", "restore para trans");
			para2Rev.ParseIsCurrent = fParseIsCurrent;

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
			VerifyTranslations(para1Curr, new []{ null, "one trans", null, "two trans", null, "three trans", null,
				"four trans", null, "four again trans", null,  "five trans"},
				new []{ 2, "one ".Length, 1, "two ".Length, 1, "three ".Length, 1, "four ".Length, 1,
					"four again ".Length, 1, "five".Length }, new[] { 0, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0, 1 }, "insert dup verse");
			IScrTxtPara para2Curr = sectionCur.ContentOA.ParagraphsOS[1] as IScrTxtPara;
			VerifyTranslations(para2Curr, new []{ null, "restore para trans" },
				new[] { 1, "paragraph to restore from the revision.".Length }, new[] { 0, 1}, "insert para");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// text difference range contains a footnote in both books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT")]
		public void ReplaceCurWithRev_SimpleText_WithFootnote_ParseIsCurrent()
		{
			ReplaceCurWithRev_SimpleText_WithFootnote(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// text difference range contains a footnote in both books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT")]
		public void ReplaceCurWithRev_SimpleText_WithFootnote()
		{
			ReplaceCurWithRev_SimpleText_WithFootnote(false);
		}

		private void ReplaceCurWithRev_SimpleText_WithFootnote(bool fParseIsCurrent)
		{
			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Before fn", Cache.DefaultVernWs, "Before fn trans");
			// place the footnote within the text
			IScrFootnote footnote1Curr = AddFootnoteSegment(para1Curr, "Current footnote text", "Current footnote trans");
			AddRunAndSegmentToMockedPara(para1Curr, "After fn", Cache.DefaultVernWs, "After fn trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Before Rev", Cache.DefaultVernWs, "Before Rev trans");
			// place the footnote within the text
			IScrFootnote footnote1Rev = AddFootnoteSegment(para1Rev, "New footnote text", "New footnote trans");
			AddRunAndSegmentToMockedPara(para1Rev, "After fn", Cache.DefaultVernWs, "After fn trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis

			// quick check of the diffs
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();

			m_bookMerger.ReplaceCurrentWithRevision(diff);


			//Verify the changed Current paragraph
			Assert.AreEqual("1Before Rev" + StringUtils.kChObject + "After fn", para1Curr.Contents.Text);

			// the new footnote should have the same content as the original Rev footnote
			IScrFootnote footnoteNew = m_genesis.FootnotesOS[0];
			IScrTxtPara paraFn = ((IScrTxtPara)footnoteNew[0]);
			AssertEx.AreTsStringsEqual(footnote1Rev[0].Contents, paraFn.Contents);
			VerifyTranslations(para1Curr, new []{ null, "Before Rev trans", null, "After fn trans" },
				new[] { 1, "Before Rev".Length, 1, "After fn".Length }, new[] { 0, 1 }, "para with footnote marker");
			VerifyTranslations(paraFn, new[] { "New footnote trans" },
				new[] { "New footnote text".Length }, new[] { 1 }, "footnote itself");

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// Revision adds a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT")]
		public void ReplaceCurWithRev_SimpleText_InsertFootnote_ParseIsCurrent()
		{
			ReplaceCurWithRev_SimpleText_InsertFootnote(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// Revision adds a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT")]
		public void ReplaceCurWithRev_SimpleText_InsertFootnote()
		{
			ReplaceCurWithRev_SimpleText_InsertFootnote(false);
		}

		private void ReplaceCurWithRev_SimpleText_InsertFootnote(bool fParseIsCurrent)
		{
			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Before fn. ", Cache.DefaultVernWs, "Before fn trans");
			AddRunAndSegmentToMockedPara(para1Curr, "After fn", Cache.DefaultVernWs, "After fn trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Before fn. ", Cache.DefaultVernWs, "Before fn trans");
			// place the footnote within the text
			IScrFootnote footnote1Rev = AddFootnoteSegment(para1Rev, "New footnote text", "New footnote trans");
			AddRunAndSegmentToMockedPara(para1Rev, "After fn", Cache.DefaultVernWs, "After fn trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis

			// quick check of the diffs
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();

			m_bookMerger.ReplaceCurrentWithRevision(diff);

			//Verify the changed Current paragraph
			Assert.AreEqual("1Before fn. " + StringUtils.kChObject + "After fn", para1Curr.Contents.Text);

			IScrFootnote footnoteNew = m_genesis.FootnotesOS[0];
			IScrTxtPara paraFn = ((IScrTxtPara)footnoteNew[0]);
			AssertEx.AreTsStringsEqual(footnote1Rev[0].Contents, paraFn.Contents);
			VerifyTranslations(para1Curr, new []{ null, "Before fn trans", null, "After fn trans" },
				new[] { 1, "Before fn. ".Length, 1, "After fn".Length }, new[] { 0, 1 }, "restore footnote");
			VerifyTranslations(paraFn, new []{ "New footnote trans" },
				new[] { "New footnote text".Length }, new[] { 1 }, "footnote itself");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// Revision adds a footnote and some surrounding text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT")]
		public void ReplaceCurWithRev_SimpleText_InsertFnAndSegs_ParseIsCurrent()
		{
			ReplaceCurWithRev_SimpleText_InsertFnAndSegs(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// Revision adds a footnote and some surrounding text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT")]
		public void ReplaceCurWithRev_SimpleText_InsertFnAndSegs()
		{
			ReplaceCurWithRev_SimpleText_InsertFnAndSegs(false);
		}

		private void ReplaceCurWithRev_SimpleText_InsertFnAndSegs(bool fParseIsCurrent)
		{
			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Before fn. ", Cache.DefaultVernWs, "Before fn trans");
			AddRunAndSegmentToMockedPara(para1Curr, "After fn", Cache.DefaultVernWs, "After fn trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Before fn. ", Cache.DefaultVernWs, "Before fn trans");
			AddRunAndSegmentToMockedPara(para1Rev, "Inserted before. ", Cache.DefaultVernWs, "Inserted before trans");
			// place the footnote within the text
			IScrFootnote footnote1Rev = AddFootnoteSegment(para1Rev, "New footnote text", "New footnote trans");
			AddRunAndSegmentToMockedPara(para1Rev, "Inserted after. ", Cache.DefaultVernWs, "Inserted after trans");
			AddRunAndSegmentToMockedPara(para1Rev, "After fn", Cache.DefaultVernWs, "After fn trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis

			// quick check of the diffs
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();

			m_bookMerger.ReplaceCurrentWithRevision(diff);

			//Verify the changed Current paragraph
			Assert.AreEqual("1Before fn. Inserted before. " + StringUtils.kChObject + "Inserted after. After fn", para1Curr.Contents.Text);

			IScrFootnote footnoteNew = m_genesis.FootnotesOS[0];
			IScrTxtPara paraFn = ((IScrTxtPara)footnoteNew[0]);
			AssertEx.AreTsStringsEqual(footnote1Rev[0].Contents, paraFn.Contents);
			VerifyTranslations(para1Curr, new []{ null, "Before fn trans", "Inserted before trans", null, "Inserted after trans", "After fn trans" },
				new[] { 1, "Before fn. ".Length, "Inserted before. ".Length, 1, "Inserted after. ".Length, "After fn".Length }, new[] { 0, 1, 1, 1, 1 }, "insert fn + text");
			VerifyTranslations(paraFn, new []{ "New footnote trans" },
				new[] { "New footnote text".Length }, new[] { 1 }, "footnote itself");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// Revision adds a footnote which divides a previous single segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT")]
		public void ReplaceCurWithRev_SimpleText_InsertFootnote_BreakingSeg_ParseIsCurrent()
		{
			ReplaceCurWithRev_SimpleText_InsertFootnote_BreakingSeg(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// Revision adds a footnote which divides a previous single segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("WANTTESTPORT (TE-8699) Need to figure out what to do about footnotes in the segmented BT")]
		public void ReplaceCurWithRev_SimpleText_InsertFootnote_BreakingSeg()
		{
			ReplaceCurWithRev_SimpleText_InsertFootnote_BreakingSeg(false);
		}

		private void ReplaceCurWithRev_SimpleText_InsertFootnote_BreakingSeg(bool fParseIsCurrent)
		{
			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Before fn After fn", Cache.DefaultVernWs, "No fn trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Before fn ", Cache.DefaultVernWs, "Before fn trans");
			// place the footnote within the text
			IScrFootnote footnote1Rev = AddFootnoteSegment(para1Rev, "New footnote text", "New footnote trans");
			AddRunAndSegmentToMockedPara(para1Rev, "After fn", Cache.DefaultVernWs, "After fn trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis

			// quick check of the diffs
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();

			m_bookMerger.ReplaceCurrentWithRevision(diff);

			//Verify the changed Current paragraph
			Assert.AreEqual("1Before fn " + StringUtils.kChObject + "After fn", para1Curr.Contents.Text);

			IScrFootnote footnoteNew = m_genesis.FootnotesOS[0];
			IScrTxtPara paraFn = ((IScrTxtPara)footnoteNew[0]);
			AssertEx.AreTsStringsEqual(footnote1Rev[0].Contents, paraFn.Contents);
			VerifyTranslations(para1Curr, new []{ null, "Before fn trans", null, "After fn trans" },
				new[] { 1, "Before fn ".Length, 1, "After fn".Length }, new[] { 0, 1 }, "restore footnote");
			VerifyTranslations(paraFn, new []{ "New footnote trans" },
				new[] { "New footnote text".Length }, new[] { 1 }, "footnote itself");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book and the revision have multiple changes in the same paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_MultipleChangesInPara_ParseIsCurrent()
		{
			ReplaceCurWithRev_MultipleChangesInPara(true);
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
			ReplaceCurWithRev_MultipleChangesInPara(false);
		}

		private void ReplaceCurWithRev_MultipleChangesInPara(bool fParseIsCurrent)
		{
			string current = "Current";
			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, current, Cache.DefaultVernWs, "Current Trans");
			AddRunAndSegmentToMockedPara(para1Curr, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, current, Cache.DefaultVernWs, "Current Trans");
			AddRunAndSegmentToMockedPara(para1Curr, "3", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, current, Cache.DefaultVernWs, "Current Trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Rev", Cache.DefaultVernWs, "Rev Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Abc", Cache.DefaultVernWs, "Abc Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "3", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Rev", Cache.DefaultVernWs, "Rev Trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

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

			IScrTxtPara paraCurr = para1Curr;
			Assert.AreEqual("1Current2Abc3Current", paraCurr.Contents.Text);

			VerifyTranslations(para1Curr, new []{ null, "Current Trans", null, "Abc Trans", null, "Current Trans"},
				new[] { 1, current.Length, 1, "Abc".Length, 1, current.Length }, new[] { 0, 1, 0, 1, 0, 1 }, "middle of 3 diffs");


			// Do the replace on remaining diffs, in any order
			m_bookMerger.ReplaceCurrentWithRevision(thirdDiff);
			m_bookMerger.ReplaceCurrentWithRevision(firstDiff);

			VerifyTranslations(para1Curr, new []{ null, "Rev Trans", null, "Abc Trans", null, "Rev Trans" },
				new[] { 1, "Rev".Length, 1, "Abc".Length, 1, "Rev".Length }, new[] { 0, 1, 0, 1, 0, 1 }, "three diffs");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current paragraph has a missing verse in the middle of the paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_VerseMissingInCurrent_MidPara_ParseIsCurrent()
		{
			ReplaceCurWithRev_VerseMissingInCurrent_MidPara(true);
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
			ReplaceCurWithRev_VerseMissingInCurrent_MidPara(false);
		}

		private void ReplaceCurWithRev_VerseMissingInCurrent_MidPara(bool fParseIsCurrent)
		{
			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Verse1", Cache.DefaultVernWs, "Verse1 Trans");
			AddRunAndSegmentToMockedPara(para1Curr, "3", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Verse>", Cache.DefaultVernWs, "Verse3 Trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Verse1", Cache.DefaultVernWs, "Verse1 Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Verse2", Cache.DefaultVernWs, "Verse2 Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "3", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Verse>Rev", Cache.DefaultVernWs, "Verse3Rev Trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

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
			IScrTxtPara paraCurr = para1Curr;
			Assert.AreEqual("1Verse12Verse23Verse>", paraCurr.Contents.Text);

			//We expect to have 6 segments in each call to VerifyTranslations
			//They alternate between either a chapter or verse number (no notes or 0), and a section with translation (one translation, 1)
			var segmentNotesArray = new[] {0, 1, 0, 1, 0, 1};

			VerifyTranslations(para1Curr, new []{ null, "Verse1 Trans", null, "Verse2 Trans", null, "Verse3 Trans" },
				new[] { 1, "Verse1".Length, 1, "Verse2".Length, 1, "Verse3".Length }, segmentNotesArray, "insert verse");

			// Do the replace on remaining diff
			m_bookMerger.ReplaceCurrentWithRevision(secondDiff);

			// Recheck that Current is now identical to Revision
			// Because results in moving a word the end of one paragraph to the end of another it will append the translation text
			// rather than replacing it, so the test uses the appended text of the 2 translations
			VerifyTranslations(para1Curr, new[] { null, "Verse1 Trans", null, "Verse2 Trans", null, "Verse3 Trans Verse3Rev Trans" },
				new[] { 1, "Verse1".Length, 1, "Verse2".Length, 1, "Verse3Rev".Length }, segmentNotesArray, "mod final verse");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// revision has an additional verse at the end of the last paragraph, which is missing
		/// in the current book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_VerseMissingInCurrent_EndOfLastPara_ParseIsCurrent()
		{
			ReplaceCurWithRev_VerseMissingInCurrent_EndOfLastPara(true);
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
			ReplaceCurWithRev_VerseMissingInCurrent_EndOfLastPara(false);
		}

		private void ReplaceCurWithRev_VerseMissingInCurrent_EndOfLastPara(bool fParseIsCurrent)
		{
			// build Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Verse1", Cache.DefaultVernWs, "Verse1 Trans");
			AddRunAndSegmentToMockedPara(para1Curr, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Curr, "Verse2", Cache.DefaultVernWs, "Verse2 Trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// build Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Verse1", Cache.DefaultVernWs, "Verse1 Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Verse2", Cache.DefaultVernWs, "Verse2 Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "3", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para1Rev, "Verse3", Cache.DefaultVernWs, "Verse3 Trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			// Detect differences and verify that they are correct
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Difference diff = m_bookMerger.Differences.MoveFirst();

			// Do the "ReplaceCurrentWithRevision" action on diff
			m_bookMerger.ReplaceCurrentWithRevision(diff);
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// Verify the changed paragraph
			VerifyTranslations(para1Curr, new []{ null, "Verse1 Trans", null, "Verse2 Trans", null, "Verse3 Trans" },
				new[] { 1, "Verse1".Length, 1, "Verse2".Length, 1, "Verse3".Length }, new[] { 0, 1, 0, 1, 0, 1 }, "insert final verse");
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
		public void ReplaceCurWithRev_Title_ParseIsCurrent()
		{
			ReplaceCurWithRev_Title(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book and the revision have a single difference in the text of the book title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_Title()
		{
			ReplaceCurWithRev_Title(false);
		}

		private void ReplaceCurWithRev_Title(bool fParseIsCurrent)
		{
			AddTitleToMockedBook(m_genesis, "My book title");
			IScrTxtPara para1Curr = ((IScrTxtPara)m_genesis.TitleOA.ParagraphsOS[0]);
			AddSegmentTranslations(para1Curr, 0, "My book title Trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			AddTitleToMockedBook(m_genesisRevision, "My Genesis title");
			IScrTxtPara para1Rev = ((IScrTxtPara)m_genesisRevision.TitleOA.ParagraphsOS[0]);
			AddSegmentTranslations(para1Rev, 0, "My Genesis title Trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			// Create Current section
			IScrSection sectionCurr = CreateSection(m_genesis, "My aching head!");
			AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);

			// Create Revision section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);

			// Detect differences and replace with current
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Difference diff = m_bookMerger.Differences.MoveFirst();
			m_bookMerger.ReplaceCurrentWithRevision(diff);

			// Verify the changed paragraph
			Assert.AreEqual("My Genesis title", para1Curr.Contents.Text);

			VerifyTranslations(para1Curr, new []{ "My Genesis title Trans" },
				new[] { "My Genesis title".Length }, new[] { 1 }, "book title");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book and the revision have a single difference in the text of the section
		/// head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_SectionHead_ParseIsCurrent()
		{
			ReplaceCurWithRev_SectionHead(true);
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
			ReplaceCurWithRev_SectionHead(false);
		}

		private void ReplaceCurWithRev_SectionHead(bool fParseIsCurrent)
		{
			// Create a section for both the current version of Genesis and the stored revision.
			IScrSection sectionCurr = CreateSection(m_genesis, "My section head!");
			IScrTxtPara para1Curr = ((IScrTxtPara)sectionCurr.HeadingOA.ParagraphsOS[0]);
			AddSegmentTranslations(para1Curr, 0, "My section head Trans");
			AddRunAndSegmentToMockedPara(para1Curr, "An unchanged sentence", Cache.DefaultVernWs, "Current Trans");
			AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// Create Rev section
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = ((IScrTxtPara)sectionRev.HeadingOA.ParagraphsOS[0]);
			AddSegmentTranslations(para1Rev, 0, "My aching head Trans");
			AddRunAndSegmentToMockedPara(para1Rev, "An unchanged sentence", Cache.DefaultVernWs, "Rev Trans");
			AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			// Detect differences and verify that they are correct
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();

			// Do the "ReplaceCurrentWithRevision" action
			m_bookMerger.ReplaceCurrentWithRevision(diff);
			Assert.AreEqual(0, m_bookMerger.Differences.Count);

			// Verify the changed section head
			Assert.AreEqual("My aching head!An unchanged sentence",
				((IScrTxtPara)sectionCurr.HeadingOA.ParagraphsOS[0]).Contents.Text);

			// We used to revise the BT of the unchanged sentence, since it is part of a single segment sequence with
			// the one we are replacing. We had to change this behavior when moving the segmenting code to
			// FDO. If we want that behavior back, we need to change this test (FWR-1124).
			VerifyTranslations(para1Curr, new []{ "My aching head Trans", "Current Trans" },
				new[] { "My aching head!".Length, "An unchanged sentence".Length }, new[] { 1, 1 }, "heading");
			//VerifySegments(para1Curr, new[] { "My aching head Trans", "Rev Trans" },
			//    new[] { "My aching head!".Length, "An unchanged sentence".Length }, "heading");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book has a paragraph split at a verse boundary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_ParaSplitAtVerseStart_ParseIsCurrent()
		{
			ReplaceCurWithRev_ParaSplitAtVerseStart(true);
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
			ReplaceCurWithRev_ParaSplitAtVerseStart(false);
		}

		private void ReplaceCurWithRev_ParaSplitAtVerseStart(bool fParseIsCurrent)
		{
			// Build Current section - two paragraphs with verses 1, 2 and 3 and split after verse 1
			IScrSection sectionCur = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Curr, 0, 1, "verse one. ", "verse one Trans. ");
			para1Curr.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para2Curr, 0, 2, "verse two. ", "verse two Trans. ");
			AddVerseSegment(para2Curr, 0, 3, "verse three.", "verse three Trans. ");
			para2Curr.ParseIsCurrent = fParseIsCurrent;

			// Build Revision section - a single para with three verses
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Rev, 0, 1, "verse one. ", "verse one Trans. ");
			AddVerseSegment(para1Rev, 0, 2, "verse two. ", "verse two Trans. ");
			AddVerseSegment(para1Rev, 0, 3, "verse three.", "verse three Trans. ");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// Get the first difference, verify it, and do a ReplaceCurrentWithRevision
			// to simulate clicking the "revert to old" button
			Difference diff = m_bookMerger.Differences.MoveFirst();

			m_bookMerger.ReplaceCurrentWithRevision(diff); // we expect this to merge the current paras
			Assert.AreEqual(1, sectionCur.ContentOA.ParagraphsOS.Count);

			VerifyTranslations(para1Curr, new []{null, "verse one Trans. ", null, "verse two Trans. ", null, "verse three Trans. "},
				new[] { 1, "verse one. ".Length, 1, "verse two. ".Length, 1, "verse three.".Length }, new[] { 0, 1, 0, 1, 0, 1 }, "merge paras");

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book has a paragraph split in the middle of a verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_ParaSplitMidVerse_ParseIsCurrent()
		{
			ReplaceCurWithRev_ParaSplitMidVerse(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book has a paragraph split in the middle of a verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_ParaSplitMidVerse()
		{
			ReplaceCurWithRev_ParaSplitMidVerse(false);
		}

		private void ReplaceCurWithRev_ParaSplitMidVerse(bool fParseIsCurrent)
		{
			// Build Current section - two paragraphs with verses 1, 2 and 3 and split mid verse 2
			IScrSection sectionCur = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Curr, 0, 1, "verse one. ", "verse one Trans. ");
			AddVerseSegment(para1Curr, 0, 2, "verse two. ", "verse two Trans. ");
			para1Curr.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para2Curr, "more of verse 2. ", Cache.DefaultVernWs, "more Trans");
			AddVerseSegment(para2Curr, 0, 3, "verse three.", "verse three Trans. ");
			para2Curr.ParseIsCurrent = fParseIsCurrent;

			// Build Revision section - a single para with three verses
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Rev, 0, 1, "verse one. ", "verse one Trans. ");
			AddVerseSegment(para1Rev, 0, 2, "verse two. ", "verse two Trans. ");
			AddRunAndSegmentToMockedPara(para1Rev, "more of verse 2. ", Cache.DefaultVernWs, "more Trans");
			AddVerseSegment(para1Rev, 0, 3, "verse three.", "verse three Trans. ");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// Get the first difference, verify it, and do a ReplaceCurrentWithRevision
			// to simulate clicking the "revert to old" button
			Difference diff = m_bookMerger.Differences.MoveFirst();

			m_bookMerger.ReplaceCurrentWithRevision(diff); // we expect this to merge the current paras
			Assert.AreEqual(1, sectionCur.ContentOA.ParagraphsOS.Count);

			VerifyTranslations(para1Curr,
				new[]{null, "verse one Trans. ", null, "verse two Trans. ", "more Trans", null, "verse three Trans. "},
				new[]{1, "verse one. ".Length, 1, "verse two. ".Length, "more of verse 2. ".Length, 1, "verse three.".Length},
				new[] { 0, 1, 0, 1, 1, 0, 1 }, "merge paras");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book has a paragraph split in the middle of a verse, not at a natural
		/// segment break (i.e., merged verse text will be a single segment).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_ParaSplitMidVerse_MergeSegs_ParseIsCurrent()
		{
			ReplaceCurWithRev_ParaSplitMidVerse_MergeSegs(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book has a paragraph split in the middle of a verse, not at a natural
		/// segment break (i.e., merged verse text will be a single segment).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_ParaSplitMidVerse_MergeSegs()
		{
			ReplaceCurWithRev_ParaSplitMidVerse_MergeSegs(false);
		}

		private void ReplaceCurWithRev_ParaSplitMidVerse_MergeSegs(bool fParseIsCurrent)
		{
			// Build Current section - two paragraphs with verses 1, 2 and 3 and split mid verse 2
			IScrSection sectionCur = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Curr, 0, 1, "verse one. ", "verse one Trans. ");
			AddVerseSegment(para1Curr, 0, 2, "verse two ", "verse two Trans. "); // verse 2 does not end in period!
			para1Curr.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para2Curr, "more of verse 2. ", Cache.DefaultVernWs, "more Trans");
			AddVerseSegment(para2Curr, 0, 3, "verse three.", "verse three Trans. ");
			para2Curr.ParseIsCurrent = fParseIsCurrent;

			// Build Revision section - a single para with three verses
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Rev, 0, 1, "verse one. ", "verse one Trans. ");
			AddVerseSegment(para1Rev, 0, 2, "verse two more of verse 2. ", "verse two Trans. more Trans");
			AddVerseSegment(para1Rev, 0, 3, "verse three.", "verse three Trans. ");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// Get the first difference and do a ReplaceCurrentWithRevision to simulate clicking the "Use this Version" button
			Difference diff = m_bookMerger.Differences.MoveFirst();

			m_bookMerger.ReplaceCurrentWithRevision(diff); // we expect this to merge the current paras
			Assert.AreEqual(1, sectionCur.ContentOA.ParagraphsOS.Count);

			VerifyTranslations(para1Curr, new[] { null, "verse one Trans. ", null, "verse two Trans. more Trans", null, "verse three Trans. " },
				new[] { 1, "verse one. ".Length, 1, "verse two more of verse 2. ".Length, 1, "verse three.".Length },
				new[] { 0, 1, 0, 2, 0, 1 }, "merge paras");
		}

//        /// ------------------------------------------------------------------------------------
//        /// <summary>
//        /// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
//        /// current book has a paragraph split in the middle of a verse, not at a natural
//        /// segment break (i.e., merged verse text will be a single segment). This test covers
//        /// the case where the (merged) paragraph is for a single verse.
//        /// </summary>
//        /// ------------------------------------------------------------------------------------
//        [Test]
//        public void ReplaceCurWithRev_ParaSplitMidVerse_OnlyVerseInPara_MergeSegs()
//        {
////			Assert.Fail("This is similar to the case that fails in TE, but this test passes!");
//            // Build Current section - two paragraphs with verses 1, 2 and 3 and split mid verse 2
//            IScrSection sectionCur = CreateSection(m_genesis, "My aching head!");
//            IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
//            AddVerseSegment(para1Curr, 1, 1, "verse one ", "verse one Trans. "); // No sentence-ending punctuation in vernacular!
//            IScrTxtPara para2Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
//            AddRunAndSegmentToMockedPara(para2Curr, "more of verse 1. ", Cache.DefaultVernWs, "more Trans");

//            // Build Revision section - a single para with three verses
//            IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
//            IScrTxtPara paraRev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
//            AddVerseSegment(paraRev, 1, 1, "verse one more of verse 1. ", "verse one Trans. more Trans");

//            // Detect differences
//            m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
//            Assert.AreEqual(1, m_bookMerger.Differences.Count);

//            // Get the first difference, and do a ReplaceCurrentWithRevision to simulate clicking the "Use this Version" button
//            Difference diff = m_bookMerger.Differences.MoveFirst();

//            m_bookMerger.ReplaceCurrentWithRevision(diff); // we expect this to merge the current paras
//            Assert.AreEqual(1, sectionCur.ContentOA.ParagraphsOS.Count);

//            VerifyBt(para1Curr, new[] { null, "verse one Trans. more Trans" },
//                new[] { 2, "verse one more of verse 1. ".Length }, "merge paras");
//        }

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
		public void ReplaceCurWithRev_ParaSplitAtVerseStart_AdjacentChanges_ParseIsCurrent()
		{
			ReplaceCurWithRev_ParaSplitAtVerseStart_AdjacentChanges(true);
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
		public void ReplaceCurWithRev_ParaSplitAtVerseStart_AdjacentChanges()
		{
			ReplaceCurWithRev_ParaSplitAtVerseStart_AdjacentChanges(false);
		}

		private void ReplaceCurWithRev_ParaSplitAtVerseStart_AdjacentChanges(bool fParseIsCurrent)
		{
			// Create data and confirm differences.
			// Create a section for both the current version of Genesis and the stored revision.
			IScrSection sectionCur = CreateSection(m_genesis, "My Beautiful Verses");
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My Beautiful Verses");

			// Create the "current" paragraphs
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Curr, 20, 1, "The disciples were all together. ", "Disciples together Trans");//unusual trailing space
			para1Curr.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para2Curr, 0, 2, "Suddenly there was a strong wind noise. ", "Suddenly strong wind Trans");
			AddVerseSegment(para2Curr, 0, 3, "They saw tongues of fire. ", "Tongues fire Trans");
			para2Curr.ParseIsCurrent = fParseIsCurrent;

			// Build up the "revision" paragraph
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Rev, 20, 1, "They were all together. ", "They together Trans");
			AddVerseSegment(para1Rev, 0, 2, "Suddenly there was a violent wind sound. ", "Suddenly violent wind Trans");
			AddVerseSegment(para1Rev, 0, 3, "They saw tongues of fire. ", "Tongues fire Trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para2Rev, 0, 4, "They were filled with the Holy Spirit and spoke in tongues.", "Filled Trans");
			para2Rev.ParseIsCurrent = fParseIsCurrent;

			m_bookMerger.DetectDifferences(null);

			// Revert differences from first to last.
			IScrSection sectionCurr = (IScrSection)para1Curr.Owner.Owner;
			Difference diff1 = m_bookMerger.Differences.MoveFirst();
			Difference diff2 = m_bookMerger.Differences.MoveNext();
			Difference diff3 = m_bookMerger.Differences.MoveNext();
			Difference diff4 = m_bookMerger.Differences.MoveNext();

			// Revert text difference in verse 1.
			m_bookMerger.ReplaceCurrentWithRevision(diff1);
			para1Curr = (IScrTxtPara)sectionCurr.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("201They were all together. ", para1Curr.Contents.Text);

			VerifyTranslations(para1Curr, new []{null, "They together Trans"},
				new []{ 3, "They were all together. ".Length }, new[] { 0, 1 }, "v1 text");

			// Revert paragraph split at end of verse 1.
			m_bookMerger.ReplaceCurrentWithRevision(diff2);
			Assert.AreEqual(1, sectionCurr.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("201They were all together. 2Suddenly there was a strong wind noise. " +
				"3They saw tongues of fire. ",
				((IScrTxtPara)sectionCurr.ContentOA.ParagraphsOS[0]).Contents.Text);

			VerifyTranslations(para1Curr, new []{null, "They together Trans", null, "Suddenly strong wind Trans", null, "Tongues fire Trans"},
				new []{ 3, "They were all together. ".Length, 1, "Suddenly there was a strong wind noise. ".Length,
				1, "They saw tongues of fire. ".Length}, new[] { 0, 1, 0, 1, 0, 1 }, "para split v1");

			// Revert text difference in verse 2.
			m_bookMerger.ReplaceCurrentWithRevision(diff3);
			Assert.AreEqual("201They were all together. 2Suddenly there was a violent wind sound. " +
				"3They saw tongues of fire. ", para1Curr.Contents.Text);
			VerifyTranslations(para1Curr, new []{null, "They together Trans", null, "Suddenly violent wind Trans", null, "Tongues fire Trans"},
				new []{ 3, "They were all together. ".Length, 1, "Suddenly there was a violent wind sound. ".Length,
				1, "They saw tongues of fire. ".Length}, new[] { 0, 1, 0, 1, 0, 1 }, "v2 text");

			// Revert missing paragraph (verse 4).
			m_bookMerger.ReplaceCurrentWithRevision(diff4);
			Assert.AreEqual(2, sectionCurr.ContentOA.ParagraphsOS.Count);
			IScrTxtPara newParaCurr = (IScrTxtPara)sectionCurr.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("4They were filled with the Holy Spirit and spoke in tongues.", newParaCurr.Contents.Text);
			VerifyTranslations(newParaCurr, new []{ null, "Filled Trans" },
				new[] { 1, "They were filled with the Holy Spirit and spoke in tongues.".Length }, new[] { 0, 1 }, "add para 2");

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the current
		/// has one verse in three paragraphs and when the corresponding revision verse has only one paragraph.
		/// There is a text change in the first and last parts of the verse. This test exercises a special
		/// case in calling StTxtPara.MergeParaWithNext.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("TE-9820 - We don't correctly get the gloses back from the revision like we should")]
		public void ReplaceCurWithRev_MultiParasInVerse_OneToThreeParas_TextChanges_ParseIsCurrent()
		{
			ReplaceCurWithRev_MultiParasInVerse_OneToThreeParas_TextChanges(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the current
		/// has one verse in three paragraphs and when the corresponding revision verse has only one paragraph.
		/// There is a text change in the first and last parts of the verse. This test exercises a special
		/// case in calling StTxtPara.MergeParaWithNext.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_MultiParasInVerse_OneToThreeParas_TextChanges()
		{
			ReplaceCurWithRev_MultiParasInVerse_OneToThreeParas_TextChanges(false);
		}

		private void ReplaceCurWithRev_MultiParasInVerse_OneToThreeParas_TextChanges(bool fParseIsCurrent)
		{
			// Create a section for both the current version of Genesis and the stored revision.
			IScrSection sectionCur = CreateSection(m_genesis, "My Beautiful Verses");
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My Beautiful Verses");

			// Build up the "current" paragraphs
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Curr, 30, 33, "For as churning the cream produces butter,", "churning trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para2Curr, 0, 0, "and as twisting the nose produces blood,", "twisting trans");
			para2Curr.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para3Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para3Curr, 0, 0, "then stirring up anger produces strife.", "stirring trans");
			para3Curr.ParseIsCurrent = fParseIsCurrent;

			// Build up the "revision" paragraph
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Rev, 30, 33, "For as churning the milk produces butter, and as twisting "
				+ "the nose produces blood, so stirring up anger produces strife.", "churning and twisting and stirring trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			m_bookMerger.DetectDifferences(null);

			// We expect one paragraph structure difference with three subdifferences.
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();

			// Revert the difference in verse 33: para split, and text changes in three
			// ScrVerses in the current
			IScrSection sectionCurr = (IScrSection)para1Curr.Owner.Owner;
			Assert.AreEqual(3, sectionCurr.ContentOA.ParagraphsOS.Count);
			m_bookMerger.ReplaceCurrentWithRevision(diff);

			Assert.AreEqual(1, sectionCurr.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("3033For as churning the milk produces butter, and as twisting "
				+ "the nose produces blood, so stirring up anger produces strife.",
				((IScrTxtPara)sectionCurr.ContentOA.ParagraphsOS[0]).Contents.Text);
			VerifyTranslations(para1Curr, new[] { null, "churning and twisting and stirring trans" },
				new []{ 4, ("For as churning the milk produces butter, and as twisting "
				+ "the nose produces blood, so stirring up anger produces strife.").Length },
				new[] { 0, 2 }, "combine segs");

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the current
		/// has one verse in two paragraphs and when the corresponding revision verse has only one paragraph.
		/// There is a text change in the second paragraph of the verse that contains added words
		/// of text. This test exercises verifies that the extra analyses will also be removed.
		/// TE-
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_MultiParasInVerse_OneToTwoParas_AddedText_ParseIsCurrent()
		{
			ReplaceCurWithRev_MultiParasInVerse_OneToTwoParas_AddedText(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the current
		/// has one verse in two paragraphs and when the corresponding revision verse has only one paragraph.
		/// There is a text change in the second paragraph of the verse that contains added words
		/// of text. This test exercises verifies that the extra analyses will also be removed.
		/// TE-
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_MultiParasInVerse_OneToTwoParas_AddedText()
		{
			ReplaceCurWithRev_MultiParasInVerse_OneToTwoParas_AddedText(false);
		}

		private void ReplaceCurWithRev_MultiParasInVerse_OneToTwoParas_AddedText(bool fParseIsCurrent)
		{
			// Create a section for both the current version of Genesis and the stored revision.
			IScrSection sectionCur = CreateSection(m_genesis, "My Beautiful Verses");
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My Beautiful Verses");

			// Build up the "current" paragraphs
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Curr, 30, 33, "For as churning the cream produces butter, ", "churning trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para2Curr, 0, 0, "some extra text not included in the final and as twisting the nose produces blood, then stirring up anger produces strife.", "twisting and stirring trans");
			para2Curr.ParseIsCurrent = fParseIsCurrent;

			// Build up the "revision" paragraph
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Rev, 30, 33, "For as churning the cream produces butter, "
				+ "and as twisting the nose produces blood, then stirring up anger produces strife.", "churning and twisting and stirring trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			m_bookMerger.DetectDifferences(null);

			// We expect one paragraph structure difference with three subdifferences.
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();

			// Revert the difference in verse 33: para split, and text changes in three
			// ScrVerses in the current
			IScrSection sectionCurr = (IScrSection)para1Curr.Owner.Owner;
			Assert.AreEqual(2, sectionCurr.ContentOA.ParagraphsOS.Count);
			m_bookMerger.ReplaceCurrentWithRevision(diff);

			Assert.AreEqual(1, sectionCurr.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("3033For as churning the cream produces butter, "
				+ "and as twisting the nose produces blood, then stirring up anger produces strife.",
				((IScrTxtPara)sectionCurr.ContentOA.ParagraphsOS[0]).Contents.Text);
			VerifyTranslations(para1Curr, new[] { null, "churning trans twisting and stirring trans" },
				new[]{ 4, ("For as churning the cream produces butter, "
				+ "and as twisting the nose produces blood, then stirring up anger produces strife.").Length },
				new[] { 0, 2 }, "combine segs");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the current
		/// has one verse in two paragraphs and when the corresponding revision verse has only one paragraph.
		/// There is a text change in the second paragraph of the verse that contains added words
		/// of text. This test exercises verifies that the extra analyses will also be removed.
		/// TE-
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_MultiParasInVerse_OneToTwoParas_AddedText_NoTrailingSpaceOrPunct_ParseIsCurrent()
		{
			ReplaceCurWithRev_MultiParasInVerse_OneToTwoParas_AddedText_NoTrailingSpaceOrPunct(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the current
		/// has one verse in two paragraphs and when the corresponding revision verse has only one paragraph.
		/// There is a text change in the second paragraph of the verse that contains added words
		/// of text. This test exercises verifies that the extra analyses will also be removed.
		/// TE-
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_MultiParasInVerse_OneToTwoParas_AddedText_NoTrailingSpaceOrPunct()
		{
			ReplaceCurWithRev_MultiParasInVerse_OneToTwoParas_AddedText_NoTrailingSpaceOrPunct(false);
		}

		private void ReplaceCurWithRev_MultiParasInVerse_OneToTwoParas_AddedText_NoTrailingSpaceOrPunct(bool fParseIsCurrent)
		{
			// Create a section for both the current version of Genesis and the stored revision.
			IScrSection sectionCur = CreateSection(m_genesis, "My Beautiful Verses");
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My Beautiful Verses");

			// Build up the "current" paragraphs
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Curr, 30, 33, "For as churning the cream produces butter", "churning trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para2Curr, 0, 0, "some extra text not included in the final and as twisting the nose produces blood, then stirring up anger produces strife.", "twisting and stirring trans");
			para2Curr.ParseIsCurrent = fParseIsCurrent;

			// Build up the "revision" paragraph
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Rev, 30, 33, "For as churning the cream produces butter"
				+ "and as twisting the nose produces blood, then stirring up anger produces strife.", "churning and twisting and stirring trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			m_bookMerger.DetectDifferences(null);

			// We expect one paragraph structure difference with three subdifferences.
			Assert.AreEqual(1, m_bookMerger.Differences.Count);
			Difference diff = m_bookMerger.Differences.MoveFirst();

			// Revert the difference in verse 33: para split, and text changes in three
			// ScrVerses in the current
			IScrSection sectionCurr = (IScrSection)para1Curr.Owner.Owner;
			Assert.AreEqual(2, sectionCurr.ContentOA.ParagraphsOS.Count);
			m_bookMerger.ReplaceCurrentWithRevision(diff);

			Assert.AreEqual(1, sectionCurr.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("3033For as churning the cream produces butter"
				+ "and as twisting the nose produces blood, then stirring up anger produces strife.",
				((IScrTxtPara)sectionCurr.ContentOA.ParagraphsOS[0]).Contents.Text);
			VerifyTranslations(para1Curr, new[] { null, "churning trans twisting and stirring trans" },
				new[]{ 4, ("For as churning the cream produces butter"
				+ "and as twisting the nose produces blood, then stirring up anger produces strife.").Length },
				new[] { 0, 2 }, new [] { 7 }, "combine segs");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book has a paragraph merge at a verse boundary. This tests the call to
		/// CopyFt from ReplaceCurrentWithRevision_CopyParaStructure.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_ParaMergeAtVerseStart_ParseIsCurrent()
		{
			ReplaceCurWithRev_ParaMergeAtVerseStart(true);
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
			ReplaceCurWithRev_ParaMergeAtVerseStart(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book has a paragraph merge at a verse boundary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		/// It seems that the code branch this test exercises was intended for the merging of two paragraphs and therefore has
		/// unexpected side affects when triggered in scripture merging. This test was added to document the discovery in case we need
		/// to handle this in the future, but ignored since it fails currently. Naylor, Thompson, Oct-2011
		[Ignore("Fails because the code falls into a branch which re-analyzes the last word. This behavior seems wrong, might deserve investigation.")]
		[Test]
		public void ReplaceCurWithRev_ParaMergeAtVerseEnd()
		{

			// Build Current section - a single para with three verses
			IScrSection sectionCur = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Curr, 0, 1, "verse one. ", "verse one trans");
			AddVerseSegment(para1Curr, 0, 2, "verse two. ", "verse two trans");
			AddVerseSegment(para1Curr, 0, 3, "verse three", "verse three trans");
			para1Curr.ParseIsCurrent = true;

			// Build Revision section - two paragraphs with verses 1, 2 and 3 and split after verse 1
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Rev, 0, 1, "verse one. ", "verse one trans");
			AddVerseSegment(para1Rev, 0, 2, "verse two. ", "verse two trans");
			AddVerseSegment(para1Rev, 0, 3, "verse threeeee", "verse threeee trans");
			para1Rev.ParseIsCurrent = true;

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// Revert
			Difference diff = m_bookMerger.Differences.MoveFirst();
			m_bookMerger.ReplaceCurrentWithRevision(diff); // we expect this to split the current para
			// NOTE: the two appended free translations are probably undesired behavior in addition to the failure.
			VerifyTranslations(para1Curr, new[] { null, "verse one trans", null, "verse two trans", null, "verse three trans verse threeee trans" },
				new[] { 1, "verse one. ".Length, 1, "verse two. ".Length, 1, "verse threeeee".Length }, new[] { 0, 1, 0, 1, 0, 1 }, "modify text of final verse in a paragraph");
		}

		private void ReplaceCurWithRev_ParaMergeAtVerseStart(bool fParseIsCurrent)
		{
			// Build Current section - a single para with three verses
			IScrSection sectionCur = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Curr, 0, 1, "verse one. ", "verse one trans");
			AddVerseSegment(para1Curr, 0, 2, "verse two. ", "verse two trans");
			AddVerseSegment(para1Curr, 0, 3, "verse three.", "verse three trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// Build Revision section - two paragraphs with verses 1, 2 and 3 and split after verse 1
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Rev, 0, 1, "verse one. ", "verse one trans"); //has space; no text difference
			para1Rev.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para2Rev, 0, 2, "verse two. ", "verse two trans");
			AddVerseSegment(para2Rev, 0, 3, "verse three.", "verse three trans");
			para2Rev.ParseIsCurrent = fParseIsCurrent;

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// Revert
			Difference diff = m_bookMerger.Differences.MoveFirst();
			m_bookMerger.ReplaceCurrentWithRevision(diff); // we expect this to split the current para

			//verify the revert
			Assert.AreEqual(2, sectionCur.ContentOA.ParagraphsOS.Count);
			para1Curr = (IScrTxtPara)sectionCur.ContentOA[0];
			IScrTxtPara para2Curr = (IScrTxtPara)sectionCur.ContentOA[1];

			VerifyTranslations(para1Curr, new []{ null, "verse one trans" },
				new[] { 1, "verse one. ".Length }, new[] { 0, 1 }, "split para");
			VerifyTranslations(para2Curr, new []{ null, "verse two trans", null, "verse three trans" },
				new []{ 1, "verse two. ".Length, 1, "verse three.".Length },
				new[] { 0, 1, 0, 1 }, "split para");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book has a paragraph merge at a verse boundary. This tests the call to
		/// CopyFt from ReplaceCurrentWithRevision_CopyParaStructure.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This fails because of the way that reverts are done in the diff code (i.e. they don't copy segment information from the revision).")]
		public void ReplaceCurWithRev_ParaMergeInMidVerse_ParseIsCurrent()
		{
			ReplaceCurWithRev_ParaMergeInMidVerse(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book has a paragraph merge at a verse boundary. This tests the call to
		/// CopyFt from ReplaceCurrentWithRevision_CopyParaStructure.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This fails because of the way that reverts are done in the diff code (i.e. they don't copy segment information from the revision).")]
		public void ReplaceCurWithRev_ParaMergeInMidVerse()
		{
			ReplaceCurWithRev_ParaMergeInMidVerse(false);
		}

		private void ReplaceCurWithRev_ParaMergeInMidVerse(bool fParseIsCurrent)
		{
			// Build Current section - a single para with three verses
			IScrSection sectionCur = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Curr, 0, 1, "verse one. ", "verse one trans");
			AddVerseSegment(para1Curr, 0, 2, "verse two. ", "verse two trans");
			AddVerseSegment(para1Curr, 0, 3, "verse three.", "verse three trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// Build Revision section - two paragraphs with verses 1, 2 and 3 and split in the middle of verse 2
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Rev, 0, 1, "verse one. ", "verse one trans"); //has space; no text difference
			AddVerseSegment(para1Rev, 0, 2, "verse ", "verse ");
			para1Rev.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para2Rev, 0, 0, "two. ", "two trans");
			AddVerseSegment(para2Rev, 0, 3, "verse three.", "verse three trans");
			para2Rev.ParseIsCurrent = fParseIsCurrent;

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis
			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// Revert
			Difference diff = m_bookMerger.Differences.MoveFirst();
			m_bookMerger.ReplaceCurrentWithRevision(diff); // we expect this to split the current para

			//verify the revert
			Assert.AreEqual(2, sectionCur.ContentOA.ParagraphsOS.Count);
			para1Curr = (IScrTxtPara)sectionCur.ContentOA[0];
			IScrTxtPara para2Curr = (IScrTxtPara)sectionCur.ContentOA[1];

			VerifyTranslations(para1Curr, new[] { null, "verse one trans", null, "verse " },
				new[] { 1, "verse one. ".Length, 1, "verse ".Length }, new[] { 0, 1, 0, 1 }, "split para");
			VerifyTranslations(para2Curr, new[] { "two trans", null, "verse three trans" },
				new[] { "two. ".Length, 1, "verse three.".Length },
				new[] { 1, 0, 1 }, "split para");
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
		public void ReplaceCurWithRev_SectionMissingInCurrent_ParseIsCurrent()
		{
			ReplaceCurWithRev_SectionMissingInCurrent(true);
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
			ReplaceCurWithRev_SectionMissingInCurrent(false);
		}

		private void ReplaceCurWithRev_SectionMissingInCurrent(bool fParseIsCurrent)
		{
			// Build the "current" section
			IScrSection section1Curr = CreateSection(m_genesis, "My Second Section");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(section1Curr, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1Curr, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para1Curr, "1", ScrStyleNames.VerseNumber);
			AddSegmentTranslations(para1Curr, 0, null);
			AddRunAndSegmentToMockedPara(para1Curr, "This is the second section", Cache.DefaultVernWs, "second section trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// Build the "revision" sections
			IScrSection section1Rev = CreateSection(m_genesisRevision, "My First Section");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(section1Rev, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para1Rev, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para1Rev, "1", ScrStyleNames.VerseNumber);
			AddSegmentTranslations(para1Rev, 0, null);
			AddRunAndSegmentToMockedPara(para1Rev, "This is the first section", Cache.DefaultVernWs, "first section trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			IScrSection section2Rev = CreateSection(m_genesisRevision, "My Second Section");
			IScrTxtPara para2Rev = AddParaToMockedSectionContent(section2Rev, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para2Rev, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para2Rev, "1", ScrStyleNames.VerseNumber);
			AddSegmentTranslations(para2Rev, 0, null);
			AddRunAndSegmentToMockedPara(para2Rev, "This is the second section", Cache.DefaultVernWs, "second section trans");
			para2Rev.ParseIsCurrent = fParseIsCurrent;

			IScrSection section3Rev = CreateSection(m_genesisRevision, "My Third Section");
			IScrTxtPara para3Rev = AddParaToMockedSectionContent(section3Rev, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para3Rev, "3", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para3Rev, "1", ScrStyleNames.VerseNumber);
			AddSegmentTranslations(para3Rev, 0, null);
			AddRunAndSegmentToMockedPara(para3Rev, "This is the third section", Cache.DefaultVernWs, "3rd section trans");
			para3Rev.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para3aRev = AddParaToMockedSectionContent(section3Rev, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para3aRev, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para3aRev, "This is the second para of the third section", Cache.DefaultVernWs, "p2 s3 trans");
			para3aRev.ParseIsCurrent = fParseIsCurrent;

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.AreEqual(2, m_bookMerger.Differences.Count);
			Difference diff1 = m_bookMerger.Differences.MoveFirst();
			Difference diff2 = m_bookMerger.Differences.MoveNext();

			// Revert the first difference, which should copy the first revision section to the current
			m_bookMerger.ReplaceCurrentWithRevision(diff1);
			IScrSection section = m_genesis.SectionsOS[0];
			IScrTxtPara para1 = ((IScrTxtPara) section.ContentOA.ParagraphsOS[0]);
			Assert.AreEqual("11This is the first section", para1.Contents.Text);

			VerifyTranslations(para1, new []{ null, "first section trans" },
				new[] { 2, "This is the first section".Length }, new[] { 0, 1 }, "insert section");

			// Revert the second difference, which should copy the last revision section to the current
			m_bookMerger.ReplaceCurrentWithRevision(diff2);
			section = m_genesis.SectionsOS[2];

			IScrTxtPara para2 = ((IScrTxtPara)section.ContentOA.ParagraphsOS[0]);
			Assert.AreEqual("31This is the third section", para2.Contents.Text);
			IScrTxtPara para3 = ((IScrTxtPara)section.ContentOA.ParagraphsOS[1]);
			Assert.AreEqual("2This is the second para of the third section", para3.Contents.Text);
			VerifyTranslations(para2, new []{ null, "3rd section trans" },
				new[] { 2, "This is the third section".Length }, new[] { 0, 1 }, "insert 3rd section p1");
			VerifyTranslations(para3, new []{ null, "p2 s3 trans" },
				new[] { 1, "This is the second para of the third section".Length }, new[] { 0, 1 }, "insert 3rd section p2");
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
		public void ReplaceCurWithRev_Sections_DeleteMultiple_ParseIsCurrent()
		{
			ReplaceCurWithRev_Sections_DeleteMultiple(true);
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
			ReplaceCurWithRev_Sections_DeleteMultiple(false);
		}

		private void ReplaceCurWithRev_Sections_DeleteMultiple(bool fParseIsCurrent)
		{
			// Build the "current" sections: 1-6
			IScrSection section1Curr = CreateSection(m_genesis, "My Section");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(section1Curr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para1Curr, "1", ScrStyleNames.ChapterNumber, null);
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			IScrSection section2Curr = CreateSection(m_genesis, "My Section");
			IScrTxtPara para2Curr = AddParaToMockedSectionContent(section2Curr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para2Curr, "2", ScrStyleNames.ChapterNumber, null);
			AddRunAndSegmentToMockedPara(para2Curr, "Contents of section 2 para 1", Cache.DefaultVernWs, "sec 2 p1 trans");
			para2Curr.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2b = AddParaToMockedSectionContent(section2Curr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para2b, "2", ScrStyleNames.VerseNumber, null);
			AddRunAndSegmentToMockedPara(para2b, "Section 2 para 2", Cache.DefaultVernWs, "sec 2.2 trans");
			para2b.ParseIsCurrent = fParseIsCurrent;

			IScrSection section3Curr = CreateSection(m_genesis, "My Section");
			IScrTxtPara para3Curr = AddParaToMockedSectionContent(section3Curr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para3Curr, "3", ScrStyleNames.ChapterNumber, null);
			para3Curr.ParseIsCurrent = fParseIsCurrent;

			IScrSection section4Curr = CreateSection(m_genesis, "My Section");
			IScrTxtPara para4Curr = AddParaToMockedSectionContent(section4Curr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para4Curr, "4", ScrStyleNames.ChapterNumber, null);
			para4Curr.ParseIsCurrent = fParseIsCurrent;

			IScrSection section5Curr = CreateSection(m_genesis, "My Section");
			IScrTxtPara para5Curr = AddParaToMockedSectionContent(section5Curr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para5Curr, "5", ScrStyleNames.ChapterNumber, null);
			para5Curr.ParseIsCurrent = fParseIsCurrent;

			IScrSection section6Curr = CreateSection(m_genesis, "My Section");
			IScrTxtPara para6Curr = AddParaToMockedSectionContent(section6Curr, ScrStyleNames.NormalParagraph);
			AddRunAndSegmentToMockedPara(para6Curr, "6", ScrStyleNames.ChapterNumber, null);
			para6Curr.ParseIsCurrent = fParseIsCurrent;

			// Build the "revision" section: 4
			IScrSection section4Rev = CreateSection(m_genesisRevision, "My Section");
			IScrTxtPara para4Rev = AddParaToMockedSectionContent(section4Rev, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para4Rev, "4", ScrStyleNames.ChapterNumber);
			para4Rev.ParseIsCurrent = fParseIsCurrent;

			// Detect differences
			m_bookMerger.DetectDifferences(null);
			Assert.AreEqual(2, m_bookMerger.Differences.Count);
			Difference diff1 = m_bookMerger.Differences.MoveFirst();
			Difference diff2 = m_bookMerger.Differences.MoveNext();

			ISegment segS1 = para1Curr.SegmentsOS[0];
			ISegment segS2 = para2Curr.SegmentsOS[1];
			ISegment segS2b = para2b.SegmentsOS[1];
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoObjectDeleted, segS1.Hvo, "segment should have known class before deletion");

			// Revert all the "added in current" diffs, to delete them from the current
			m_bookMerger.ReplaceCurrentWithRevision(diff1);
			m_bookMerger.ReplaceCurrentWithRevision(diff2);

			Assert.AreEqual(1, m_genesis.SectionsOS.Count);

			// Verify that the relevant segments got deleted. (There are others, but this is a good
			// representative sample.)
			VerifyDel(segS1);
			VerifyDel(segS2);
			VerifyDel(segS2b);
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
		public void ReplaceCurWithRev_MultiParasInVerse_ThreeToOneParas_CorrFirst_ParseIsCurrent()
		{
			ReplaceCurWithRev_MultiParasInVerse_ThreeToOneParas_CorrFirst(true);
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
			ReplaceCurWithRev_MultiParasInVerse_ThreeToOneParas_CorrFirst(false);
		}

		private void ReplaceCurWithRev_MultiParasInVerse_ThreeToOneParas_CorrFirst(bool fParseIsCurrent)
		{
			// Create a section for both the current version of Genesis and the stored revision.
			IScrSection sectionCurr = CreateSection(m_genesis, "My Beautiful Verses");
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My Beautiful Verses");

			// Build up the "current" paragraph
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Curr, 30, 32, "Verse 32. ", "V32 trans");
			AddVerseSegment(para1Curr, 0, 33, "For as churning the milk produces good butter, ", "Churning milk trans");
			AddVerseSegment(para1Curr, 0, 34, "Verse 34.", "V34 trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// Build up the "revision" paragraphs
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Rev, 30, 32, "Versie 3. ", "Versie 3@ trans");
			AddVerseSegment(para1Rev, 0, 33, "For as churning the cream produces good butter, ", "Churning cream trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para2Rev, 0, 0, "and as twisting the nose produces blood,", "Twisting trans");
			para2Rev.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para3Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para3Rev, 0, 0, "then stirring up anger produces strife. ", "Stirring trans");
			AddVerseSegment(para3Rev, 0, 34, "Versify thirty-four.", "Versify 34 trans");
			para3Rev.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para4Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para4Rev, 0, 35, "Verse 35.", "V35 trans");
			para4Rev.ParseIsCurrent = fParseIsCurrent;

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
			IScrTxtPara para1 = ((IScrTxtPara) sectionCurr.ContentOA.ParagraphsOS[0]);
			Assert.AreEqual("3032Versie 3. 33For as churning the milk produces good butter, "
				+ "34Verse 34.", para1.Contents.Text);
			VerifyTranslations(para1, new string[] { null, "Versie 3@ trans", null, "Churning milk trans",  null, "V34 trans"},
				new int[] { 4, "Versie 3. ".Length, 2, "For as churning the milk produces good butter, ".Length, 2, "Verse 34.".Length },
				new[] { 0, 1, 0, 1, 0, 1 }, "revert 32");

			// Revert text change in verse 33
			m_bookMerger.ReplaceCurrentWithRevision(diff2);
			Assert.AreEqual(1, sectionCurr.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("3032Versie 3. 33For as churning the cream produces good butter, "
				+ "34Verse 34.", para1.Contents.Text);
			VerifyTranslations(para1, new string[] { null, "Versie 3@ trans", null, "Churning cream trans",  null, "V34 trans"},
				new int[] { 4, "Versie 3. ".Length, 2, "For as churning the cream produces good butter, ".Length, 2,  "Verse 34.".Length},
				new[] { 0, 1, 0, 1, 0, 1 }, "revert 33");

			// Revert the complex difference in verse 33: para's missing in current
			m_bookMerger.ReplaceCurrentWithRevision(diff3);
			// We expect the one paragraph to be split into three paragraphs and text changes to be made.
			IScrTxtPara para2 = ((IScrTxtPara) sectionCurr.ContentOA.ParagraphsOS[1]);
			IScrTxtPara para3 = ((IScrTxtPara) sectionCurr.ContentOA.ParagraphsOS[2]);
			Assert.AreEqual(3, sectionCurr.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("3032Versie 3. 33For as churning the cream produces good butter, ", para1.Contents.Text);
			Assert.AreEqual("and as twisting the nose produces blood,", para2.Contents.Text);
			Assert.AreEqual("then stirring up anger produces strife. 34Verse 34.", para3.Contents.Text);
			VerifyTranslations(para1, new []{ null, "Versie 3@ trans", null, "Churning cream trans"},
				new []{ 4, "Versie 3. ".Length, 2, "For as churning the cream produces good butter, ".Length},
				new[] { 0, 1, 0, 1 }, "revert paras 1");
			VerifyTranslations(para2, new []{ "Twisting trans"},
				new []{"and as twisting the nose produces blood,".Length}, new[] { 1 }, "revert paras 2");
			VerifyTranslations(para3, new []{ "Stirring trans", null, "V34 trans"},
				new []{"then stirring up anger produces strife. ".Length, 2, "Verse 34.".Length},
				new[] { 1, 0, 1 }, "revert paras 3");

			// Revert text change in verse 34
			m_bookMerger.ReplaceCurrentWithRevision(diff4);
			Assert.AreEqual("then stirring up anger produces strife. 34Versify thirty-four.",
				para3.Contents.Text);
			VerifyTranslations(para3, new []{ "Stirring trans", null, "Versify 34 trans"},
				new []{"then stirring up anger produces strife. ".Length, 2, "Versify thirty-four.".Length},
				new[] { 1, 0, 1 }, "revert 34");

			// Revert missing para in current
			m_bookMerger.ReplaceCurrentWithRevision(diff5);
			Assert.AreEqual(4, sectionCurr.ContentOA.ParagraphsOS.Count);
			IScrTxtPara para4 = ((IScrTxtPara) sectionCurr.ContentOA.ParagraphsOS[3]);
			Assert.AreEqual("35Verse 35.",
				((IScrTxtPara)sectionCurr.ContentOA.ParagraphsOS[3]).Contents.Text);
			VerifyTranslations(para4, new []{ null, "V35 trans"},
				new[] { 2, "Verse 35.".Length }, new[] { 0, 1 }, "insert para");

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the current
		/// has a verse in one paragraph and the revision has the same verse in multiple
		/// paragraphs with changed text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_MultiParasInVerse_ThreeToOneParas_TextDifferent_ParseIsCurrent()
		{
			ReplaceCurWithRev_MultiParasInVerse_ThreeToOneParas_TextDifferent(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the current
		/// has a verse in one paragraph and the revision has the same verse in multiple
		/// paragraphs with changed text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_MultiParasInVerse_ThreeToOneParas_TextDifferent()
		{
			ReplaceCurWithRev_MultiParasInVerse_ThreeToOneParas_TextDifferent(false);
		}

		private void ReplaceCurWithRev_MultiParasInVerse_ThreeToOneParas_TextDifferent(bool fParseIsCurrent)
		{
			// Create a section for both the current version of Genesis and the stored revision.
			IScrSection sectionCurr = CreateSection(m_genesis, "My Beautiful Verses");
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My Beautiful Verses");

			// Build up the "current" paragraph
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Curr, 0, 33, "For as churning the milk produces good butter, and as twisting the nose produces blood, then stirring up anger produces strife.", "Churning milk trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;

			// Build up the "revision" paragraphs
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Rev, 0, 33, "For as churning the cream produces a sensible ", "Churning cream trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para2Rev, 0, 0, "cropping of normal stuff when added ", "Twisting trans");
			para2Rev.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para3Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para3Rev, 0, 0, "to the stirring up anger produces strife.", "Stirring trans");
			para3Rev.ParseIsCurrent = fParseIsCurrent;

			m_bookMerger.DetectDifferences(null);

			Assert.AreEqual(1, m_bookMerger.Differences.Count);

			// We expect one paragraph structure difference with three subdifferences.
			Difference diff1 = m_bookMerger.Differences.MoveFirst();

			// Revert the complex difference in verse 33: para's missing in current
			m_bookMerger.ReplaceCurrentWithRevision(diff1);
			// We expect the one paragraph to be split into three paragraphs and text changes to be made.
			IScrTxtPara para1 = (IScrTxtPara)sectionCurr.ContentOA[0];
			IScrTxtPara para2 = ((IScrTxtPara)sectionCurr.ContentOA.ParagraphsOS[1]);
			IScrTxtPara para3 = ((IScrTxtPara)sectionCurr.ContentOA.ParagraphsOS[2]);
			Assert.AreEqual(3, sectionCurr.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual("33For as churning the cream produces a sensible ", para1.Contents.Text);
			Assert.AreEqual("cropping of normal stuff when added ", para2.Contents.Text);
			Assert.AreEqual("to the stirring up anger produces strife.", para3.Contents.Text);
			// We aren't really sure whether the BT for para 1 should only have the BT from
			// the rev or have them concatenated. We used to expect only the rev BT, but now
			// the code combines them and keeps both. Bryan said he thought it was okay.
			VerifyTranslations(para1, new [] { null, "Churning milk trans Churning cream trans" },
				new [] { 2, "For as churning the cream produces a sensible ".Length },
				new[] { 0, 1 }, "revert 33");
			VerifyTranslations(para2, new [] { "Twisting trans" },
				new [] { "cropping of normal stuff when added ".Length }, new[] { 1 }, "revert paras 2");
			VerifyTranslations(para3, new[] { "Stirring trans" },
				new[] { "to the stirring up anger produces strife.".Length }, new[] { 1 }, "revert paras 3");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the current
		/// has three paragraphs, with the middle one being new material and the revision has
		/// two verses in a single paragraph. TE-9294.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_OneToThreeParas_TextAddedToVerse1_ParseIsCurrent()
		{
			ReplaceCurWithRev_OneToThreeParas_TextAddedToVerse1(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the current
		/// has three paragraphs, with the middle one being new material and the revision has
		/// two verses in a single paragraph. TE-9294.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_OneToThreeParas_TextAddedToVerse1()
		{
			ReplaceCurWithRev_OneToThreeParas_TextAddedToVerse1(false);
		}

		private void ReplaceCurWithRev_OneToThreeParas_TextAddedToVerse1(bool fParseIsCurrent)
		{
			// Create a section for both the current version of Genesis and the stored revision.
			IScrSection sectionCurr = CreateSection(m_genesis, "My Beautiful Verses");
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My Beautiful Verses");

			// Build up the "current" paragraph
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Curr, 1, 1, "For as churning the cream produces butter.", "Churning cream trans");
			para1Curr.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para2Curr, 0, 0, "so chopping wood produces chips.", string.Empty);
			para2Curr.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para3Curr = AddParaToMockedSectionContent(sectionCurr, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para3Curr, 0, 2, "Stirring up anger produces strife.", "Stirring trans");
			para3Curr.Contents = para3Curr.Contents.Insert(0, TsStringUtils.MakeTss(" ", Cache.DefaultVernWs));
			para3Curr.ParseIsCurrent = fParseIsCurrent;

			// Build up the "revision" paragraphs
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Rev, 1, 1, "For as churning the cream produces butter. ", "Churning cream trans");
			AddVerseSegment(para1Rev, 0, 2, "Stirring up anger produces strife.", "Stirring trans");
			para1Rev.ParseIsCurrent = fParseIsCurrent;

			m_bookMerger.DetectDifferences(null);

			Assert.AreEqual(2, m_bookMerger.Differences.Count);

			// We expect a simple text difference for the space at the end of verse 1.
			Difference diff1 = m_bookMerger.Differences.MoveFirst();
			DiffTestHelper.VerifyParaDiff(diff1, 01001001, DifferenceType.TextDifference, para1Curr,
				para1Curr.Contents.Length, para1Curr.Contents.Length, para1Rev, para1Curr.Contents.Length, para1Curr.Contents.Length + 1);
			m_bookMerger.ReplaceCurrentWithRevision(diff1);

			// The second diff is the complex difference because of the paragraph break and added verse text.
			Difference diff2 = m_bookMerger.Differences.MoveFirst();
			DiffTestHelper.VerifyParaStructDiff(diff2, 01001001, DifferenceType.ParagraphStructureChange);
			m_bookMerger.ReplaceCurrentWithRevision(diff2);

			// We expect the one paragraph to be split into three paragraphs and text changes to be made.
			Assert.AreEqual(1, sectionCurr.ContentOA.ParagraphsOS.Count);
			IScrTxtPara para1 = (IScrTxtPara)sectionCurr.ContentOA[0];
			Assert.AreEqual("11For as churning the cream produces butter. 2Stirring up anger produces strife.", para1.Contents.Text);

			VerifyTranslations(para1, new[] { null, "Churning cream trans", null, "Stirring trans" },
				new[] { 2, "For as churning the cream produces butter. ".Length, 1, "Stirring up anger produces strife.".Length },
				new[] { 0, 1, 0, 1 }, "para 1");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book has a paragraph added to the start of a section, the preceding
		/// paragraph is edited so that some additional text is added at the end, and the
		/// following paragraph is edited so that some additional text is added at the start.
		/// TE-9090
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_ParaAddedToCurrent_AdjacentAdditionsOnEitherSide_ParseIsCurrent()
		{
			ReplaceCurWithRev_ParaAddedToCurrent_AdjacentAdditionsOnEitherSide(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="BookMerger.ReplaceCurrentWithRevision"/> method when the
		/// current book has a paragraph added to the start of a section, the preceding
		/// paragraph is edited so that some additional text is added at the end, and the
		/// following paragraph is edited so that some additional text is added at the start.
		/// TE-9090
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceCurWithRev_ParaAddedToCurrent_AdjacentAdditionsOnEitherSide()
		{
			ReplaceCurWithRev_ParaAddedToCurrent_AdjacentAdditionsOnEitherSide(false);
		}

		private void ReplaceCurWithRev_ParaAddedToCurrent_AdjacentAdditionsOnEitherSide(bool fParseIsCurrent)
		{
			// Build Current section - three paragraphs with verses 1-2
			IScrSection sectionCur = CreateSection(m_genesis, "My aching head!");
			IScrTxtPara para1Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Curr, 0, 1, "verse one. ", "versiculo uno.");
			AddVerseSegment(para1Curr, 0, 0, "Some more text.", "Un poco mas de texto.");
			para1Curr.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para2Curr, 0, 0, "This is a new paragraph", "Esto es un nuevo parrafo");
			para2Curr.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para3Curr = AddParaToMockedSectionContent(sectionCur, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para3Curr, 0, 0, "Added text at start of para. ", "Texto adicional al principio del parrafo. ");
			AddVerseSegment(para3Curr, 0, 2, "verse two.", "versiculo dos.");
			para3Curr.ParseIsCurrent = fParseIsCurrent;

			// Build Revision section - two paras with verses 1-2
			IScrSection sectionRev = CreateSection(m_genesisRevision, "My aching head!");
			IScrTxtPara para1Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para1Rev, 0, 1, "verse one.", "versiculo uno.");
			para1Rev.ParseIsCurrent = fParseIsCurrent;
			IScrTxtPara para2Rev = AddParaToMockedSectionContent(sectionRev, ScrStyleNames.NormalParagraph);
			AddVerseSegment(para2Rev, 0, 2, "verse two.", "versiculo dos.");
			para2Rev.ParseIsCurrent = fParseIsCurrent;

			// Detect differences
			m_bookMerger.DetectDifferences(null); // find the diffs for Genesis

			Difference diff = m_bookMerger.Differences.MoveFirst();
			do
			{
				m_bookMerger.ReplaceCurrentWithRevision(diff);
				diff = m_bookMerger.Differences.CurrentDifference;
			}
			while (diff != null);

			Assert.AreEqual(2, sectionCur.ContentOA.ParagraphsOS.Count);
			IScrTxtPara para1 = (IScrTxtPara)sectionCur.ContentOA[0];
			Assert.AreEqual("1verse one.", para1.Contents.Text);
			IScrTxtPara para2 = (IScrTxtPara)sectionCur.ContentOA[1];
			Assert.AreEqual("2verse two.", para2.Contents.Text);

			VerifyTranslations(para1, new[] { null, "versiculo uno." },
				new [] { 1, "verse one.".Length },
				new[] { 0, 1 }, "paragraph 1");
			VerifyTranslations(para2, new[] { null, "versiculo dos." },
				new[] { 1, "verse two.".Length },
				new[] { 0, 1 }, "paragraph 2");
		}
		#endregion
	}
}
