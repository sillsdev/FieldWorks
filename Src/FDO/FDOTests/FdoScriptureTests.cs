// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FdoScriptureTests.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.FieldWorks.Test.TestUtils;
using SILUBS.SharedScrUtils;
using System.Text;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region class FdoScriptureTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for FdoScripture.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FdoScriptureTests: ScrInMemoryFdoTestBase
	{
		private static Guid kCheckId1 = Guid.NewGuid();
		private static Guid kCheckId2 = Guid.NewGuid();
		private CoreWritingSystemDefinition m_wsGerman;
		private IFdoServiceLocator m_servloc;

		#region Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_servloc = Cache.ServiceLocator;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does setup for all the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("de", out m_wsGerman);

			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				ChangeDefaultAnalWs(m_wsGerman);

				IFdoServiceLocator servloc = Cache.ServiceLocator;
				var annDefnChkError = servloc.GetInstance<ICmAnnotationDefnRepository>().CheckingError;

				ICmAnnotationDefnFactory factory = servloc.GetInstance<ICmAnnotationDefnFactory>();
				ICmAnnotationDefn errorType1 = factory.Create(kCheckId1, annDefnChkError);
				errorType1.IsProtected = true;
				errorType1.Name.SetUserWritingSystem("Dummy Check 1");
				errorType1.Description.UserDefaultWritingSystem =
					TsStringUtils.MakeTss("does nothing", Cache.DefaultUserWs);

				ICmAnnotationDefn errorType2 = factory.Create(kCheckId2, annDefnChkError);
				errorType2.IsProtected = true;
				errorType2.Name.SetUserWritingSystem("Dummy Check 2");
				errorType2.Description.UserDefaultWritingSystem =
					TsStringUtils.MakeTss("does nothing", Cache.DefaultUserWs);
			});
		}
		#endregion

		#region Find book tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding a book based on the cannonical order
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindBookByID()
		{
			AddBookToMockedScripture(57, "Philemon");
			AddBookToMockedScripture(59, "James");
			AddBookToMockedScripture(65, "Jude");

			IScrBook book;
			book = m_scr.FindBook(1); // look for Genesis?
			Assert.IsNull(book, "Genesis was found!");

			book = m_scr.FindBook(57); // look for Philemon
			Assert.IsNotNull(book, "Philemon was not found");
			Assert.AreEqual("PHM", book.BookId);

			book = m_scr.FindBook(59); // look for James
			Assert.IsNotNull(book, "James was not found");
			Assert.AreEqual("JAS", book.BookId);

			book = m_scr.FindBook(65); // look for Jude
			Assert.IsNotNull(book, "Jude was not found");
			Assert.AreEqual("JUD", book.BookId);
		}
		#endregion

		#region ScrBookRef tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the book abbreviation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UIBookAbbrev()
		{
			IScrRefSystem scr =
				Cache.ServiceLocator.GetInstance<IScrRefSystemRepository>().AllInstances().First();
			Assert.AreEqual("GEN", scr.BooksOS[0].UIBookAbbrev);
			Assert.AreEqual("REV", scr.BooksOS[65].UIBookAbbrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the name of a book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UIBookName()
		{
			IScrRefSystem scr =
				Cache.ServiceLocator.GetInstance<IScrRefSystemRepository>().AllInstances().First();
			Assert.AreEqual("GEN", scr.BooksOS[0].UIBookName);
			Assert.AreEqual("REV", scr.BooksOS[65].UIBookName);
		}
		#endregion

		#region Chapter/verse as string tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ConvertToString method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertToStringTests()
		{
			// test basic conversions with ASCII 0 so test can be read easily
			m_scr.UseScriptDigits = false;
			Assert.AreEqual("0", m_scr.ConvertToString(0));
			Assert.AreEqual("10", m_scr.ConvertToString(10));
			Assert.AreEqual("113", m_scr.ConvertToString(113));
			// just to be sure that actual unicode character works
			m_scr.UseScriptDigits = true;
			m_scr.ScriptDigitZero = '\u09e6';  // Zero for Bengali
			Assert.AreEqual("\u09e7\u09e7\u09e9", m_scr.ConvertToString(113));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ChapterVerseRefAsString method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterVerseRefAsStringTest()
		{
			m_scr.UseScriptDigits = false;
			Assert.AreEqual("4:18",
				m_scr.ChapterVerseRefAsString(new BCVRef(23, 4, 18)),
				"Simulated normalness");
			// Books like Jude may not have an explicit chapter number, in which case we
			// don't want to include the chapter number in formatted display of ref.
			Assert.AreEqual("28",
				m_scr.ChapterVerseRefAsString(new BCVRef(65, 0, 28)),
				"Simulated single-chapter book");
			Assert.AreEqual(string.Empty,
				m_scr.ChapterVerseRefAsString(new BCVRef(5, 1, 0)),
				"Simulated introduction");
			Assert.AreEqual(string.Empty,
				m_scr.ChapterVerseRefAsString(new BCVRef(5, 0, 0)),
				"Simulated title");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ChapterVerseBridgeAsString method when arguments have different books
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException), ExpectedMessage="Books are different")]
		public void ChapterVerseBridgeAsStringTest_DifferentBooks()
		{
			m_scr.ChapterVerseBridgeAsString(new BCVRef(25, 4, 18), new BCVRef(3, 4, 18),
				Cache.DefaultVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ChapterVerseBridgeAsString method when arguments have different chapters
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException), ExpectedMessage="Chapters are different")]
		public void ChapterVerseBridgeAsStringTest_DifferentChapters()
		{
			m_scr.ChapterVerseBridgeAsString(new BCVRef(25, 4, 18), new BCVRef(25, 9, 18),
				Cache.DefaultVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ChapterVerseBridgeAsString method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterVerseBridgeAsStringTest()
		{
			m_scr.UseScriptDigits = false;
			Assert.AreEqual("4:18-23",
				m_scr.ChapterVerseBridgeAsString(new BCVRef(23, 4, 18), new BCVRef(23, 4, 23),
				Cache.DefaultVernWs));
		}

		#endregion

		#region Find style tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindStyle method when the style exists
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindStyle_Exists()
		{
			AddScrStyle("Section Head", ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, false);

			IStStyle style = m_scr.FindStyle("Section Head");
			Assert.IsNotNull(style);
			Assert.AreEqual("Section Head", style.Name);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindStyle method when the style does not exist
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindStyle_NotExist()
		{
			IStStyle style = m_scr.FindStyle("Where's the Beef?");
			Assert.IsNull(style);
		}
		#endregion

		#region Annotation-related tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CreateResponse method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCreateAnnotationResponse()
		{
			IScrScriptureNote annotation =
				Cache.ServiceLocator.GetInstance<IScrScriptureNoteFactory>().Create();

			// add this annotation collection to the list
			IFdoOwningSequence<IScrScriptureNote> genesisNotes = m_scr.BookAnnotationsOS[0].NotesOS;
			genesisNotes.Add(annotation);
			int cNotesBeforeResponse = genesisNotes.Count;

			IStJournalText response = annotation.CreateResponse();
			Assert.IsNotNull(response);
			Assert.AreEqual(cNotesBeforeResponse, genesisNotes.Count,
				"Response annotation should not get added to master list.");
			Assert.AreEqual(1, annotation.ResponsesOS.Count);
			Assert.AreEqual(response, annotation.ResponsesOS[0]);
			Assert.AreEqual(1, response.ParagraphsOS.Count);
			ITsTextProps ttpParaStyle = response.ParagraphsOS[0].StyleRules;
			Assert.AreEqual(ScrStyleNames.Remark, ttpParaStyle.GetStrPropValue(
				(int)FwTextPropType.ktptNamedStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ScrBookAnnotations.InsertNote method when there are no notes in the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertNote_InEmptyList()
		{
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[0];
			BCVRef ref1 = new BCVRef(1, 1, 1);
			BCVRef ref2 = new BCVRef(1, 1, 2);
			IScrScriptureNote note = annotations.InsertNote(ref1, ref2, null, null, CmAnnotationDefnTags.kguidAnnConsultantNote);
			Assert.IsNotNull(note);
			Assert.AreEqual(1, annotations.NotesOS.Count);
			Assert.AreEqual(note, annotations.NotesOS[0]);
			Assert.AreEqual(ref1, note.BeginRef);
			Assert.AreEqual(ref2, note.EndRef);
			Assert.IsNotNull(note.QuoteOA);
			VerifyEmptyStJournalText(note.DiscussionOA);
			VerifyEmptyStJournalText(note.RecommendationOA);
			VerifyEmptyStJournalText(note.ResolutionOA);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ScrBookAnnotations.InsertNote method when it inserts at the beginning and at the
		/// end of the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertNote_AtListBeginEnd()
		{
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[0];
			BCVRef ref1 = new BCVRef(1, 1, 1);
			BCVRef ref2 = new BCVRef(1, 1, 2);
			BCVRef ref3 = new BCVRef(1, 1, 3);
			IScrScriptureNote note = annotations.InsertNote(ref2, ref2, null, null, CmAnnotationDefnTags.kguidAnnConsultantNote);
			Assert.IsNotNull(note);

			// Insert notes at beginning and end of list
			IScrScriptureNote note1 = annotations.InsertNote(ref1, ref2, null, null, CmAnnotationDefnTags.kguidAnnConsultantNote);
			IScrScriptureNote note2 = annotations.InsertNote(ref3, ref3, null, null, CmAnnotationDefnTags.kguidAnnConsultantNote);
			IScrScriptureNote note3 = annotations.InsertNote(ref3, ref3, null, null, CmAnnotationDefnTags.kguidAnnConsultantNote);

			// Verify notes are inserted at correct positions in list.
			Assert.AreEqual(4, annotations.NotesOS.Count);
			Assert.AreEqual(note, annotations.NotesOS[1]);
			Assert.AreEqual(note1, annotations.NotesOS[0]);
			Assert.AreEqual(note2, annotations.NotesOS[2]);
			Assert.AreEqual(note3, annotations.NotesOS[3]);
			Assert.AreEqual(ref2, note.BeginRef);
			Assert.AreEqual(ref2, note.EndRef);
			Assert.AreEqual(ref1, note1.BeginRef);
			Assert.AreEqual(ref2, note1.EndRef);
			Assert.AreEqual(ref3, note2.BeginRef);
			Assert.AreEqual(ref3, note2.EndRef);
			Assert.AreEqual(note2.BeginRef, note3.BeginRef);
			Assert.AreEqual(note2.EndRef, note3.EndRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ScrBookAnnotations.AdjustAnnotationReferences for case where referenced
		/// paragraph does not exist and no quote exists. Should be assigned to first intro
		/// paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustAnnotationReferences_FirstIntroPara()
		{
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(genesis, "Genesis");

			// Introduction section
			IScrSection section = AddSectionToMockedBook(genesis, true);
			AddSectionHeadParaToSection(section, "Introduction head", ScrStyleNames.IntroSectionHead);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "An intro to Genesis", null);

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[0];
			BCVRef ref1 = new BCVRef(1, 1, 0);
			IScrScriptureNote note = annotations.InsertNote(ref1, ref1, null, null, CmAnnotationDefnTags.kguidAnnConsultantNote);
			Assert.IsNotNull(note);

			m_scr.AdjustAnnotationReferences();

			Assert.AreEqual(para, note.BeginObjectRA);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ScrBookAnnotations.AdjustAnnotationReferences for case where referenced
		/// paragraph does not exist and no quote exists. Should be assigned to first title
		/// paragraph, if there is no introduction.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustAnnotationReferences_FirstTitlePara()
		{
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IStText titleText = AddTitleToMockedBook(genesis, "Genesis");
			IStPara titlePara = titleText.ParagraphsOS[0];

			// First scripture section
			IScrSection section = AddSectionToMockedBook(genesis);
			AddSectionHeadParaToSection(section, "First section head", ScrStyleNames.SectionHead);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "First verse in Genesis", null);

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[0];
			BCVRef ref1 = new BCVRef(1, 1, 0);
			IScrScriptureNote note = annotations.InsertNote(ref1, ref1, null, null, CmAnnotationDefnTags.kguidAnnConsultantNote);
			Assert.IsNotNull(note);

			m_scr.AdjustAnnotationReferences();

			Assert.AreEqual(titlePara, note.BeginObjectRA);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ScrBookAnnotations.AdjustAnnotationReferences for case where referenced
		/// paragraph is found and no quote exists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustAnnotationReferences_ParaInArchive()
		{
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			IStText titleText = AddTitleToMockedBook(genesis, "Genesis");
			IStPara titlePara = titleText.ParagraphsOS[0];

			// Introduction section
			IScrSection section = AddSectionToMockedBook(genesis, true);
			AddSectionHeadParaToSection(section, "Introduction head", ScrStyleNames.IntroSectionHead);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "An intro to Genesis", null);

			IScrDraft version = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(
				"AdjustAnnotationReferences_ParaInArchive");
			IScrBook genesisSaved = version.AddBookCopy(genesis);
			IStPara paraSaved = genesisSaved.SectionsOS[0].ContentOA.ParagraphsOS[0];

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[0];
			BCVRef ref1 = new BCVRef(1, 1, 0);
			IScrScriptureNote note = annotations.InsertNote(ref1, ref1, paraSaved, paraSaved, CmAnnotationDefnTags.kguidAnnConsultantNote);
			Assert.IsNotNull(note);

			m_scr.AdjustAnnotationReferences();

			Assert.AreEqual(para, note.BeginObjectRA);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ScrBookAnnotations.AdjustAnnotationReferences for case where referenced
		/// paragraph does not exist and quote exists. Should be assigned paragraph that
		/// contains the quote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustAnnotationReferences_WithQuote()
		{
			IScrBook genesis = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(genesis, "Genesis");

			// Introduction section
			IScrSection section = AddSectionToMockedBook(genesis, true);
			AddSectionHeadParaToSection(section, "Introduction head", ScrStyleNames.IntroSectionHead);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "An intro to Genesis", null);
			para = AddParaToMockedSectionContent(section, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "Contains quoted text - the quote", null);

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[0];
			BCVRef ref1 = new BCVRef(1, 1, 0);
			int iPos;
			StTxtParaBldr quoteBldr = new StTxtParaBldr(Cache);
				quoteBldr.ParaStyleName = ScrStyleNames.Remark;
				quoteBldr.AppendRun("the quote",
					StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
				IScrScriptureNote note = annotations.InsertNote(ref1, ref1, null, null,
					CmAnnotationDefnTags.kguidAnnConsultantNote, 0, 0, quoteBldr, null, null, null, out iPos);
				Assert.IsNotNull(note);

			m_scr.AdjustAnnotationReferences();

			Assert.AreEqual(para, note.BeginObjectRA);
			Assert.AreEqual(23, note.BeginOffset);
			Assert.AreEqual(32, note.EndOffset);
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that an "empty" journal text has a single empty paragraph with the correct
		/// writing system assigned.
		/// </summary>
		/// <param name="text">The text.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyEmptyStJournalText(IStJournalText text)
		{
			Assert.IsNotNull(text);
			Assert.AreEqual(1, text.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)text.ParagraphsOS[0];
			ITsString tss = para.Contents;
			Assert.IsNotNull(tss);
			AssertEx.RunIsCorrect(tss, 0, String.Empty, null, text.Cache.DefaultAnalWs);

		}
		#endregion

		#region IPictureLocationParser unit tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when given a USFM-style chapter
		/// range.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_USFMStyleChapterRange()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.AfterAnchor;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("MRK 1--2", 41002003, ref locRangeType, out locationMin,
				out locationMax);
			Assert.AreEqual(new BCVRef(41, 1, 1), locationMin);
			ScrReference refMax = new ScrReference(41, 2, 1, m_scr.Versification);
			refMax.Verse = refMax.LastVerse;
			Assert.AreEqual(refMax, locationMax);
			Assert.AreEqual(PictureLocationRangeType.ReferenceRange, locRangeType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when given a verse range with the
		/// book ID only specified in the first reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_NormalVerseRange_BookSpecifiedOnce()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.AfterAnchor;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("MRK 1:8-2:15", 41002003,
				ref locRangeType, out locationMin, out locationMax);
			Assert.AreEqual(new BCVRef(41, 1, 8), locationMin);
			Assert.AreEqual(new BCVRef(41, 2, 15), locationMax);
			Assert.AreEqual(PictureLocationRangeType.ReferenceRange, locRangeType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when given a verse range with the
		/// book ID specified both references.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_NormalVerseRange_BookSpecifiedTwice()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.AfterAnchor;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("MRK 1:8-MRK 2:15", 41002003,
				ref locRangeType, out locationMin, out locationMax);
			Assert.AreEqual(new BCVRef(41, 1, 8), locationMin);
			Assert.AreEqual(new BCVRef(41, 2, 15), locationMax);
			Assert.AreEqual(PictureLocationRangeType.ReferenceRange, locRangeType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when given a verse range without
		/// a book ID.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_NormalVerseRange_BookNotSpecified()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.AfterAnchor;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("1:8-2:15", 41002003,
				ref locRangeType, out locationMin, out locationMax);
			Assert.AreEqual(new BCVRef(41, 1, 8), locationMin);
			Assert.AreEqual(new BCVRef(41, 2, 15), locationMax);
			Assert.AreEqual(PictureLocationRangeType.ReferenceRange, locRangeType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when given a verse range without
		/// a chapter numbers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_NormalVerseRange_ChapterNotSpecified()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.AfterAnchor;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("2-15", 41002003, ref locRangeType,
				out locationMin, out locationMax);
			Assert.AreEqual(new BCVRef(41, 2, 2), locationMin);
			Assert.AreEqual(new BCVRef(41, 2, 15), locationMax);
			Assert.AreEqual(PictureLocationRangeType.ReferenceRange, locRangeType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when given a single verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_SingleReference_MatchesORCLocation()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.AfterAnchor;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("MRK 2:3", 41002003,
				ref locRangeType, out locationMin, out locationMax);
			Assert.AreEqual(new BCVRef(41, 2, 3), locationMin);
			Assert.AreEqual(new BCVRef(41, 2, 3), locationMax);
			Assert.AreEqual(PictureLocationRangeType.ReferenceRange, locRangeType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when given a single verse that
		/// doesn't match the location of the picture anchor (should be ignored).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_SingleReference_DoesNotMatchORCLocation()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.AfterAnchor;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("MRK 1:4", 41002003,
				ref locRangeType, out locationMin, out locationMax);
			Assert.AreEqual(PictureLocationRangeType.AfterAnchor, locRangeType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when given a verse range that
		/// doesn't include the location of the picture anchor (should be ignored).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_Range_DoesNotCoverORCLocation()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.AfterAnchor;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("MRK 1:4-MRK 2:1", 41002003,
				ref locRangeType, out locationMin, out locationMax);
			Assert.AreEqual(PictureLocationRangeType.AfterAnchor, locRangeType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when given a single verse that
		/// doesn't match the location of the picture anchor (should be ignored).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_BogusReferenceRange_Unintelligible()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.AfterAnchor;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("XYZ 3:4&6:7", 41002003,
				ref locRangeType, out locationMin, out locationMax);
			Assert.AreEqual(PictureLocationRangeType.AfterAnchor, locRangeType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when given a verse range that
		/// covers multiple books (should be ignored).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_BogusReferenceRange_MultipleBooks()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.AfterAnchor;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("MAT 1:5-REV 12:2", 41002003, ref locRangeType, out locationMin, out locationMax);
			Assert.AreEqual(PictureLocationRangeType.AfterAnchor, locRangeType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when given a verse range whose
		/// start is after its end (should be ignored).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_BogusReferenceRange_Backwards()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.AfterAnchor;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("MRK 2:5-MRK 1:2", 41002003, ref locRangeType, out locationMin, out locationMax);
			Assert.AreEqual(PictureLocationRangeType.AfterAnchor, locRangeType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when the anchor is not in
		/// the body of Scripture, such as in an intro, and the range is specified as a count
		/// of paragraphs before and after the anchor location
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_NormalParaRangeInIntro()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.AfterAnchor;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("2-6", 41001000, ref locRangeType, out locationMin, out locationMax);
			Assert.AreEqual(2, locationMin);
			Assert.AreEqual(6, locationMax);
			Assert.AreEqual(PictureLocationRangeType.ParagraphRange, locRangeType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when the anchor is not in
		/// the body of Scripture, such as in an intro (should be ignored).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_ReferenceRangeInIntro()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.AfterAnchor;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("MRK 1:0 - MRK 2:1", 41001000, ref locRangeType, out locationMin, out locationMax);
			Assert.AreEqual(PictureLocationRangeType.AfterAnchor, locRangeType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when the anchor is in the body of Scripture and the
		/// range is specified using BBCCCVVV-format integers, as would be the case when parsing
		/// the clipboard format, since that is generated without using the Scripture object to
		/// turn the references back into human-readable format. This test simulates pasting a
		/// picture into a location covered by the reference range of the original picture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_BBCCCVVVRange_MatchesORCLocation()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.ReferenceRange;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("41001002-41001016", 41001007, ref locRangeType, out locationMin, out locationMax);
			Assert.AreEqual(41001002, locationMin);
			Assert.AreEqual(41001016, locationMax);
			Assert.AreEqual(PictureLocationRangeType.ReferenceRange, locRangeType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when the anchor is in the body of Scripture and the
		/// range is specified using BBCCCVVV-format integers, as would be the case when parsing
		/// the clipboard format, since that is generated without using the Scripture object to
		/// turn the references back into human-readable format. This test simulates pasting a
		/// picture into a verse in the same book and chapter as the original picture but in a
		/// verse outside the original reference range.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_BBCCCVVVRange_VerseOutsideORCLocation()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.ReferenceRange;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("41001002-41001006", 41001007, ref locRangeType, out locationMin, out locationMax);
			Assert.AreEqual(PictureLocationRangeType.AfterAnchor, locRangeType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParsePictureLoc method when the anchor is in the body of Scripture and the
		/// range is specified using BBCCCVVV-format integers, as would be the case when parsing
		/// the clipboard format, since that is generated without using the Scripture object to
		/// turn the references back into human-readable format. This test simulates pasting a
		/// picture into a different book and chapter from the original picture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParsePictureLoc_BBCCCVVVRange_BookOutsideORCLocation()
		{
			int locationMin, locationMax;
			PictureLocationRangeType locRangeType = PictureLocationRangeType.ReferenceRange;
			((IPictureLocationBridge)m_scr).ParsePictureLoc("41001002-41001006", 42008005, ref locRangeType, out locationMin, out locationMax);
			Assert.AreEqual(PictureLocationRangeType.AfterAnchor, locRangeType);
		}
		#endregion

		#region Other tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that creating a new instance of Scripture initializes the BookAnnnotations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateScriptureCreatesBookAnnotations()
		{
			Cache.LangProject.TranslatedScriptureOA = null;
			Cache.LangProject.TranslatedScriptureOA = Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();

			IFdoOwningSequence<IScrBookAnnotations> bookNotes = Cache.LangProject.TranslatedScriptureOA.BookAnnotationsOS;

			// Make sure the right number of books was generated.
			Assert.AreEqual(66, bookNotes.Count);
			Assert.AreEqual(0, bookNotes[0].OwnOrd);
			Assert.AreEqual(65, bookNotes[BCVRef.LastBook - 1].OwnOrd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability of DetermineFootnoteMarkerType to return the correct footnote marker
		/// type for non cross-ref footnotes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineFootnoteMarkerType_Normal()
		{
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker;
			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker,
				m_scr.DetermineFootnoteMarkerType(ScrStyleNames.NormalFootnoteParagraph));
			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker,
				m_scr.DetermineFootnoteMarkerType("Garbage"));
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.NoFootnoteMarker;
			Assert.AreEqual(FootnoteMarkerTypes.NoFootnoteMarker,
				m_scr.DetermineFootnoteMarkerType(ScrStyleNames.NormalFootnoteParagraph));
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;
			Assert.AreEqual(FootnoteMarkerTypes.SymbolicFootnoteMarker,
				m_scr.DetermineFootnoteMarkerType(ScrStyleNames.NormalFootnoteParagraph));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability of DetermineFootnoteMarkerType to return the correct footnote marker
		/// type for cross-ref footnotes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineFootnoteMarkerType_CrossRef()
		{
			m_scr.CrossRefMarkerType = FootnoteMarkerTypes.NoFootnoteMarker;
			Assert.AreEqual(FootnoteMarkerTypes.NoFootnoteMarker,
				m_scr.DetermineFootnoteMarkerType(ScrStyleNames.CrossRefFootnoteParagraph));
			m_scr.CrossRefMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker;
			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker,
				m_scr.DetermineFootnoteMarkerType(ScrStyleNames.CrossRefFootnoteParagraph));
			m_scr.CrossRefMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;
			Assert.AreEqual(FootnoteMarkerTypes.SymbolicFootnoteMarker,
				m_scr.DetermineFootnoteMarkerType(ScrStyleNames.CrossRefFootnoteParagraph));
		}
		#endregion

		#region Saved Version tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that created date is added correctly to a saved version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SavedVersion_DateCreated()
		{
			IStText titleText;
			IScrBook james = m_servloc.GetInstance<IScrBookFactory>().Create(58, out titleText);

			// archive draft
			IScrDraft draft = m_servloc.GetInstance<IScrDraftFactory>().Create("testing date created", new [] { james });

			// Confirm that the dates are close (to the second).
			Assert.IsTrue(draft.DateCreated.ToString("yyMMddHHmm") == DateTime.Now.ToString("yyMMddHHmm"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving a version with one book having 3 footnotes (including 1 in the title)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SavedVersion_TestFootnoteOrder()
		{
			IStText titleText;
			var james = m_servloc.GetInstance<IScrBookFactory>().Create(58, out titleText);
			IStTxtPara title = AddParaToMockedText(titleText, ScrStyleNames.MainBookTitle);
			AddSectionToMockedBook(james, true);
			IScrSection section = AddSectionToMockedBook(james, false);
			AddSectionHeadParaToSection(section, "The world's finest section", ScrStyleNames.SectionHead);
			IStTxtPara para1 = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			IStTxtPara para2 = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);

			// Add a footnote to the first content para of the second section.
			var footnoteOrig1 = AddFootnote(james, title, 0);
			var footnoteOrig2 = AddFootnote(james, para1, 0);
			var footnoteOrig3 = AddFootnote(james, para2, 0);

			// archive draft
			IScrDraft draft = m_servloc.GetInstance<IScrDraftFactory>().Create("FootnoteOrder james", new [] { james });

			Assert.AreEqual(1, m_scr.ArchivedDraftsOC.Count);
			Assert.AreEqual("FootnoteOrder james", draft.Description);
			Assert.AreEqual(1, draft.BooksOS.Count);
			IScrBook revision = draft.BooksOS[0];
			Assert.AreNotEqual(james.Id, revision.Id);
			Assert.AreEqual(james.SectionsOS.Count, revision.SectionsOS.Count);
			IScrSection revSection1 = revision.SectionsOS[0];
			IScrSection revSection2 = revision.SectionsOS[1];
			Assert.AreNotEqual(james.SectionsOS[0].Id, revSection1.Id);
			Assert.AreNotEqual(james.SectionsOS[0].Id, revSection2.Id);
			Assert.AreNotEqual(james.SectionsOS[1].Id, revSection1.Id);
			Assert.AreNotEqual(james.SectionsOS[1].Id, revSection2.Id);
			IFdoOwningSequence<IStPara> s2Paras = revSection2.ContentOA.ParagraphsOS;
			Assert.AreEqual(james.SectionsOS[1].ContentOA.ParagraphsOS.Count,
				s2Paras.Count);

			IStTxtPara titleRev = (IStTxtPara)revision.TitleOA.ParagraphsOS[0];
			IStTxtPara paraRev1 = (IStTxtPara)s2Paras[0];
			IStTxtPara paraRev2 = (IStTxtPara)s2Paras[1];
			Assert.AreNotEqual(title.Id, titleRev.Id);
			Assert.AreNotEqual(para1.Id, paraRev1.Id);
			Assert.AreNotEqual(para2.Id, paraRev2.Id);

			Assert.AreEqual(58, revision.CanonicalNum);
			Assert.AreEqual(58, revision.BookIdRA.IndexInOwner + 1);

			// Check the footnotes
			Assert.AreEqual(james.FootnotesOS.Count, revision.FootnotesOS.Count);
			Assert.AreEqual(footnoteOrig1.Id, james.FootnotesOS[0].Id);
			Assert.AreEqual(footnoteOrig2.Id, james.FootnotesOS[1].Id);
			Assert.AreEqual(footnoteOrig3.Id, james.FootnotesOS[2].Id);
			IStFootnote footnoteRev1 = revision.FootnotesOS[0];
			IStFootnote footnoteRev2 = revision.FootnotesOS[1];
			IStFootnote footnoteRev3 = revision.FootnotesOS[2];
			Assert.AreNotEqual(footnoteRev1.Id, footnoteOrig1.Id);
			Assert.AreNotEqual(footnoteRev2.Id, footnoteOrig2.Id);
			Assert.AreNotEqual(footnoteRev3.Id, footnoteOrig3.Id);

			VerifyFootnote(footnoteRev1, titleRev, 0);
			VerifyFootnote(footnoteRev2, paraRev1, 0);
			VerifyFootnote(footnoteRev3, paraRev2, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving a version with one book having a footnote that is not found in the text
		/// because it is "unowned" (this should theoretically never happen, but I think it used
		/// to be possible by pasting from the BT). Jira # is FWR-2825.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SavedVersion_BogusUnownedFootnoteORC()
		{
			var ui = (DummyFdoUI) Cache.ServiceLocator.GetInstance<IFdoUI>();
			ui.Reset();

			IStText titleText;
			IScrBook hebrews = m_servloc.GetInstance<IScrBookFactory>().Create(58, out titleText);
			IStTxtPara title = AddParaToMockedText(titleText, ScrStyleNames.MainBookTitle);
			AddSectionToMockedBook(hebrews, true);
			IScrSection section = hebrews.SectionsOS[0];
			AddSectionHeadParaToSection(section, "The world's finest section", ScrStyleNames.SectionHead);
			IStTxtPara para1 = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			IStTxtPara para2 = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);

			// Add a footnote to each of the content paras of the section, but make the first own unowned.
			IScrFootnote footnoteOrig1 = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().Create();
			hebrews.FootnotesOS.Add(footnoteOrig1);
			ITsStrBldr tsStrBldr = para1.Contents.GetBldr();
			TsStringUtils.InsertOrcIntoPara(footnoteOrig1.Guid, FwObjDataTypes.kodtNameGuidHot,
				tsStrBldr, 0, 0, Cache.DefaultVernWs);
			para1.Contents = tsStrBldr.GetString();
			IScrFootnote footnoteOrig2 = AddFootnote(hebrews, para2, 0);

			// archive draft
			IScrDraft draft = m_servloc.GetInstance<IScrDraftFactory>().Create("", new [] { hebrews });

			Assert.AreEqual(1, m_scr.ArchivedDraftsOC.Count);
			Assert.AreEqual(1, draft.BooksOS.Count);
			IScrBook revision = draft.BooksOS[0];
			Assert.AreEqual(1, revision.SectionsOS.Count);
			IFdoOwningSequence<IStPara> s1Paras = revision.SectionsOS[0].ContentOA.ParagraphsOS;
			Assert.AreEqual(hebrews.SectionsOS[0].ContentOA.ParagraphsOS.Count,
				s1Paras.Count);

			IStTxtPara paraRev1 = (IStTxtPara)s1Paras[0];
			IStTxtPara paraRev2 = (IStTxtPara)s1Paras[1];
			Assert.AreNotEqual(para1.Id, paraRev1.Id);
			Assert.AreNotEqual(para2.Id, paraRev2.Id);

			Assert.AreEqual(58, revision.CanonicalNum);
			Assert.AreEqual(58, revision.BookIdRA.IndexInOwner + 1);

			// Check the footnotes
			Assert.AreEqual(hebrews.FootnotesOS.Count, revision.FootnotesOS.Count);
			Assert.AreEqual(footnoteOrig1.Id, hebrews.FootnotesOS[0].Id);
			Assert.AreEqual(footnoteOrig2.Id, hebrews.FootnotesOS[1].Id);
			IStFootnote footnoteRev1 = revision.FootnotesOS[0];
			IStFootnote footnoteRev2 = revision.FootnotesOS[1];
			Assert.AreNotEqual(footnoteOrig1.Id, footnoteRev1.Id);
			Assert.AreNotEqual(footnoteOrig2.Id, footnoteRev1.Id);
			Assert.AreNotEqual(footnoteOrig1.Id, footnoteRev2.Id);
			Assert.AreNotEqual(footnoteOrig2.Id, footnoteRev2.Id);

			// Footnote that was originally the first one should have gone to end of list because no owned ORC was found for it.
			VerifyFootnote(footnoteRev1, paraRev2, 0);

			// Para 1 in the revision should still have an unowned footnote ORC pointing (unfortunately) at the original footnote.
			ITsString tss = paraRev1.Contents;
			ITsTextProps orcPropsParaFootnote = tss.get_Properties(0);
			string objData = orcPropsParaFootnote.GetStrPropValue(
				(int)FwTextPropType.ktptObjData);
			Assert.IsNotNull(objData, "Footnote not found at character offset 0");
			Assert.AreEqual((char)(int)FwObjDataTypes.kodtNameGuidHot, objData[0]);
			// Send the objData string without the first character because the first character
			// is the object replacement character and the rest of the string is the GUID.
			Assert.AreEqual(footnoteOrig1.Guid, MiscUtils.GetGuidFromObjData(objData.Substring(1)));
			string sOrc = tss.get_RunText(0);
			Assert.AreEqual(StringUtils.kChObject, sOrc[0]);

			// Expecting error to have been thrown in ScriptureServices.AdjustObjectsInArchivedBook, so now we make sure the error is the one expected.
			Assert.AreEqual("1 footnote(s) in HEB did not correspond to any owned footnotes in the vernacular text of that book. They have been moved to the end of the footnote sequence.", ui.ErrorMessage);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving a version with one book having 5 footnotes (including 1 in the title)
		/// in the vernacular and a back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SavedVersion_TestBtFootnotes()
		{
			IStText titleText;
			var philemon = m_servloc.GetInstance<IScrBookFactory>().Create(57, out titleText);
			IStTxtPara title = AddParaToMockedText(titleText, ScrStyleNames.MainBookTitle);
			AddSectionToMockedBook(philemon, true);
			IScrSection introSection2 = AddSectionToMockedBook(philemon, true);
			IStTxtPara paraIntroHeading = AddSectionHeadParaToSection(introSection2, "My Intro to Philemon", ScrStyleNames.IntroSectionHead);
			IScrSection section = AddSectionToMockedBook(philemon, false);
			AddSectionHeadParaToSection(section, "The world's finest section", ScrStyleNames.SectionHead);
			IStTxtPara para1 = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			IStTxtPara para2 = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			section = AddSectionToMockedBook(philemon, false);

			// Add a footnote to the first content para of the second section.
			var footnoteOrig1 = AddFootnote(philemon, title, 0);
			var footnoteOrig2 = AddFootnote(philemon, paraIntroHeading, 2);
			var footnoteOrig3 = AddFootnote(philemon, para1, 0);
			var footnoteOrig4 = AddFootnote(philemon, para2, 0);
			var footnoteOrig5 = AddFootnote(philemon, para2, para2.Contents.Length);

			// Add text and footnotes to back translation
			int ws = Cache.DefaultAnalWs;
			AppendRunToBt(title, ws, "Back translation of title");
			InsertTestBtFootnote(footnoteOrig1, title, ws, 25);
			AppendRunToBt(paraIntroHeading, ws, "Back translation of section head");
			InsertTestBtFootnote(footnoteOrig2, paraIntroHeading, ws, 0);
			AppendRunToBt(para1, ws, "Back translation of para1");
			InsertTestBtFootnote(footnoteOrig3, para1, ws, 4);
			AppendRunToBt(para2, ws, "Back translation of para2");
			InsertTestBtFootnote(footnoteOrig4, para2, ws, 0);
			InsertTestBtFootnote(footnoteOrig5, para2, ws, 25);

			// Call the method under test: create an archive draft
			IScrDraft draft = m_servloc.GetInstance<IScrDraftFactory>().Create("BT Footnotes in Philemon", new [] { philemon });

			// In the archive, confirm that the ref ORCs (footnotes in BT) refer to their
			// corresponding vernacular footnote (have the same guid) and that neither the owned or
			// ref ORCs in the archive refer to the original footnotes.
			IScrBook revPhilemon = draft.BooksOS[0];
			IFdoOwningSequence<IScrFootnote> revFootnotes = revPhilemon.FootnotesOS;
			IStTxtPara revTitle = (IStTxtPara)revPhilemon.TitleOA.ParagraphsOS[0];
			IScrSection revSection1 = revPhilemon.SectionsOS[1];
			IStTxtPara revSectionHead = (IStTxtPara)revSection1.HeadingOA.ParagraphsOS[0];
			IScrSection revSection2 = revPhilemon.SectionsOS[2];
			IStTxtPara revPara1 = (IStTxtPara)revSection2.ContentOA.ParagraphsOS[0];
			IStTxtPara revPara2 = (IStTxtPara)revSection2.ContentOA.ParagraphsOS[1];

			// Verify that the archived footnote Guids are different than the original footnotes.
			Assert.AreNotEqual(footnoteOrig1.Guid, revFootnotes[0].Guid);
			Assert.AreNotEqual(footnoteOrig2.Guid, revFootnotes[1].Guid);
			Assert.AreNotEqual(footnoteOrig3.Guid, revFootnotes[2].Guid);
			Assert.AreNotEqual(footnoteOrig4.Guid, revFootnotes[3].Guid);
			Assert.AreNotEqual(footnoteOrig5.Guid, revFootnotes[4].Guid);

			// Verify that the owned ORCs in the archive vern refer to the correct footnote.
			VerifyFootnote(revFootnotes[0], revTitle, 0);
			VerifyFootnote(revFootnotes[1], revSectionHead, 2);
			VerifyFootnote(revFootnotes[2], revPara1, 0);
			VerifyFootnote(revFootnotes[3], revPara2, 0);
			VerifyFootnote(revFootnotes[4], revPara2, revPara2.Contents.Length - 1);

			// Verify that the ref ORCs in the BT refer to their corresponding footnote.
			FdoTestHelper.VerifyBtFootnote(revFootnotes[0], revTitle, ws, 25);
			FdoTestHelper.VerifyBtFootnote(revFootnotes[1], revSectionHead, ws, 0);
			FdoTestHelper.VerifyBtFootnote(revFootnotes[2], revPara1, ws, 4);
			FdoTestHelper.VerifyBtFootnote(revFootnotes[3], revPara2, ws, 0);
			FdoTestHelper.VerifyBtFootnote(revFootnotes[4], revPara2, ws, 25);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving a version with multiple book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SavedVersion_WithMultipleBooks()
		{
			IStText titleText;
			var james = m_servloc.GetInstance<IScrBookFactory>().Create(58, out titleText);
			IStTxtPara title = AddParaToMockedText(titleText, ScrStyleNames.MainBookTitle);
			AddRunToMockedPara(title, "James", m_wsEn);
			IScrSection introSection = AddSectionToMockedBook(james, true);
			AddSectionHeadParaToSection(introSection,
				"My Intro to James", ScrStyleNames.IntroSectionHead);
			IStTxtPara para = AddParaToMockedSectionContent(introSection, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "What should we type here?", m_wsEn);
			IScrSection section = AddSectionToMockedBook(james, false);
			AddSectionHeadParaToSection(section, "How to Have Real Joy", ScrStyleNames.SectionHead);
			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "Anything you want.", m_wsEn);

			var jude = m_servloc.GetInstance<IScrBookFactory>().Create(65, out titleText);
			title = AddParaToMockedText(titleText, ScrStyleNames.MainBookTitle);
			AddRunToMockedPara(title, "Jude", m_wsEn);
			section = AddSectionToMockedBook(jude, false);
			AddSectionHeadParaToSection(section, "Some Important Instructions", ScrStyleNames.SectionHead);
			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "Don't talk bad about people.", m_wsEn);

			// archive draft
			IScrDraft draft = m_servloc.GetInstance<IScrDraftFactory>().Create("Multiple books", new [] { james, jude });

			Assert.AreEqual(1, m_scr.ArchivedDraftsOC.Count);
			Assert.AreEqual("Multiple books", draft.Description);
			Assert.AreEqual(2, draft.BooksOS.Count);
			IScrBook revision = draft.BooksOS[0];
			Assert.AreNotEqual(james.Hvo, revision.Hvo);
			Assert.AreEqual(james.SectionsOS.Count, revision.SectionsOS.Count);
			Assert.AreNotEqual(james.SectionsOS[0].Id, revision.SectionsOS[0].Id);
			Assert.AreNotEqual(james.SectionsOS[0].Id, revision.SectionsOS[1].Id);
			Assert.AreNotEqual(james.SectionsOS[1].Id, revision.SectionsOS[1].Id);
			Assert.AreNotEqual(james.SectionsOS[1].Id, revision.SectionsOS[1].Id);
			Assert.AreEqual(james.SectionsOS[1].ContentOA.ParagraphsOS.Count,
				revision.SectionsOS[1].ContentOA.ParagraphsOS.Count);
			Assert.AreNotEqual(james.SectionsOS[1].ContentOA.ParagraphsOS[0].Id,
				revision.SectionsOS[1].ContentOA.ParagraphsOS[0].Id);
			Assert.AreEqual(58, revision.CanonicalNum);
			Assert.AreEqual(58, revision.BookIdRA.IndexInOwner + 1);

			revision = draft.BooksOS[1];
			Assert.AreNotEqual(jude.Id, revision.Id);
			Assert.AreEqual(65, revision.CanonicalNum);
			Assert.AreEqual(65, revision.BookIdRA.IndexInOwner + 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests saving a version with one book that has a picture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SavedVersion_WithOneBookAndAPicture()
		{
			IStText titleText;
			var james = m_servloc.GetInstance<IScrBookFactory>().Create(58, out titleText);
			IStTxtPara title = AddParaToMockedText(titleText, ScrStyleNames.MainBookTitle);
			AddRunToMockedPara(title, "James", m_wsEn);
			IScrSection introSection = AddSectionToMockedBook(james, true);
			AddSectionHeadParaToSection(introSection,
				"My Intro to James", ScrStyleNames.IntroSectionHead);
			IStTxtPara para = AddParaToMockedSectionContent(introSection, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "What should we type here?", m_wsEn);
			IScrSection section = AddSectionToMockedBook(james, false);
			AddSectionHeadParaToSection(section, "How to Have Real Joy", ScrStyleNames.SectionHead);
			para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "Anything you want.", m_wsEn);
			ICmPicture picture = InsertTestPicture(para, 0);

			// archive draft
			IScrDraft draft = m_servloc.GetInstance<IScrDraftFactory>().Create("Single james", new [] { james });

			Assert.AreEqual(1, m_scr.ArchivedDraftsOC.Count);
			Assert.AreEqual("Single james", draft.Description);
			Assert.AreEqual(1, draft.BooksOS.Count);
			IScrBook revision = draft.BooksOS[0];
			Assert.AreNotEqual(james.Id, revision.Id);
			Assert.AreEqual(james.SectionsOS.Count, revision.SectionsOS.Count);
			IScrSection revSection1 = revision.SectionsOS[0];
			IScrSection revSection2 = revision.SectionsOS[1];
			Assert.AreNotEqual(james.SectionsOS[0].Id, revSection1.Id);
			Assert.AreNotEqual(james.SectionsOS[0].Id, revSection2.Id);
			Assert.AreNotEqual(james.SectionsOS[1].Id, revSection1.Id);
			Assert.AreNotEqual(james.SectionsOS[1].Id, revSection2.Id);
			IFdoOwningSequence<IStPara> s2Paras = revSection2.ContentOA.ParagraphsOS;
			Assert.AreEqual(james.SectionsOS[0].ContentOA.ParagraphsOS.Count,
				revSection1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(james.SectionsOS[1].ContentOA.ParagraphsOS.Count,
				revSection2.ContentOA.ParagraphsOS.Count);
			IStTxtPara paraRev = (IStTxtPara)s2Paras[0];
			Assert.AreNotEqual(para.Id, paraRev.Id);

			// Check the picture
			VerifyPicture(picture, paraRev, 0);
		}

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure picture exists and is referred to properly in the paragraph contents
		/// </summary>
		/// <param name="pictureOrig">The original picture</param>
		/// <param name="para"></param>
		/// <param name="ich">Character position where ORC should be</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyPicture(ICmPicture pictureOrig, IStTxtPara para, int ich)
		{
			ITsString tss = para.Contents;
			int iRun = tss.get_RunAt(ich);
			ITsTextProps orcPropsParaFootnote = tss.get_Properties(iRun);
			string objData = orcPropsParaFootnote.GetStrPropValue(
				(int)FwTextPropType.ktptObjData);
			Assert.AreEqual((char)(int)FwObjDataTypes.kodtGuidMoveableObjDisp, objData[0]);

			// Send the objData string without the first character because the first character
			// is the object replacement character and the rest of the string is the GUID.
			Guid newPicGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
			Assert.AreNotEqual(pictureOrig.Guid, newPicGuid);
			string sOrc = tss.get_RunText(iRun);
			Assert.AreEqual(StringUtils.kChObject, sOrc[0]);

			ICmPicture pictureNew = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(newPicGuid);
			Assert.AreEqual(pictureOrig.PictureFileRA, pictureNew.PictureFileRA);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a picture (no caption set).
		/// </summary>
		/// <param name="para">Paragraph to insert picture into</param>
		/// <param name="ichPos">The 0-based character offset into the paragraph</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected ICmPicture InsertTestPicture(IStTxtPara para, int ichPos)
		{
			ICmPicture pict;
			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ITsStrFactory factory = TsStrFactoryClass.Create();
				pict = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create(filemaker.Filename,
					factory.MakeString("Test picture caption", Cache.DefaultVernWs),
					CmFolderTags.LocalPictures);
				ITsStrBldr bldr = para.Contents.GetBldr();
				pict.InsertORCAt(bldr, ichPos);
				para.Contents = bldr.GetString();
			}

			return pict;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a footnote reference marker (ref orc)
		/// </summary>
		/// <param name="footnote">given footnote</param>
		/// <param name="para">paragraph owning the translation to insert footnote marker into</param>
		/// <param name="ws">given writing system for the back translation</param>
		/// <param name="ichPos">The 0-based character offset into the translation</param>
		/// ------------------------------------------------------------------------------------
		protected void InsertTestBtFootnote(IStFootnote footnote, IStTxtPara para, int ws,
			int ichPos)
		{
			ICmTranslation trans = para.GetOrCreateBT();
			ITsStrBldr bldr = trans.Translation.get_String(ws).GetBldr();
			footnote.InsertRefORCIntoTrans(bldr, ichPos, ws);
			trans.Translation.set_String(ws, bldr.GetString());
		}
		#endregion

		#endregion

		#region PropertyChangedNotifier tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures the BooksChanged event fires when a book is added to the ScriptureBooksOS
		/// list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureBooksChangeEventFires()
		{
			m_actionHandler.BreakUndoTask("Undo just the stuff", "in this test");
			int cBooksChangedEventRaised = 0;
			PropertyChangedHandler testBookChangedHandler = sender =>
			{
				Assert.AreEqual(m_scr, sender);
				Assert.IsTrue(m_scr.FindBook(16) != null || cBooksChangedEventRaised == 1,
					"Should have the inserted book the first time this is called; should be gone the second time (as a result of the undo).");
				Assert.IsTrue(m_actionHandler.CanUndo() || cBooksChangedEventRaised == 1);
				cBooksChangedEventRaised++;
			};

			Assert.IsFalse(m_actionHandler.CanUndo());
			try
			{
				m_scr.BooksChanged += testBookChangedHandler;
				Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(16);

				Assert.AreEqual(0, cBooksChangedEventRaised);
				m_actionHandler.EndUndoTask();
				Assert.AreEqual(1, cBooksChangedEventRaised);
				m_actionHandler.Undo();
				Assert.AreEqual(2, cBooksChangedEventRaised);
			}
			finally
			{
				m_scr.BooksChanged -= testBookChangedHandler;
				if (m_actionHandler.CurrentDepth == 0)
					m_actionHandler.BeginUndoTask("Make Fixture teardown happy", "like a birthday");
			}
		}
		#endregion
	}
	#endregion

	#region class FindCorrespondingVernParaForSegmentTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests of FdoScripture.FindCorrespondingVernParaForSegment method.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FindCorrespondingVernParaForSegmentTests: ScrInMemoryFdoTestBase
	{
		#region Data members
		private FwStyleSheet m_stylesheet;
		private IScrBook m_philemon;
		private IScrSection m_section1;
		private IScrSection m_section2;
		private IScrSection m_section3;
		#endregion

		#region Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			m_stylesheet = new FwStyleSheet();

			AddScrStyle("User added title style", ContextValues.Title, StructureValues.Undefined, FunctionValues.Prose, false);
			AddScrStyle("Poetry", ContextValues.Text, StructureValues.Body, FunctionValues.Prose, false);
			AddScrStyle("Parallel Passage", ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, false);
			AddScrStyle("Line 1", ContextValues.Text, StructureValues.Body, FunctionValues.Prose, false);

			m_stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);

			AddBookWithTwoSections(40, "Matthew");
			m_philemon = AddBookWithTwoSections(57, "Philemon");
			AddBookWithTwoSections(66, "Revelation");

			m_section1 = m_philemon.SectionsOS[0];
			AddSectionHeadParaToSection(m_section1, "Matt. 4:5-9; Luke 2:6-10", "Parallel Passage");
			IStTxtPara para = (IStTxtPara)m_section1.ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " One is the best verse. ", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " I like two better. ", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " No, three rocks! ", null);
			AddRunToMockedPara(para, "4-8", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " Nobody can touch four through eight. ", null);

			m_section2 = m_philemon.SectionsOS[1];
			para = (IStTxtPara)m_section2.ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para, "10", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " Ten is the best verse. ", null);
			AddRunToMockedPara(para, "11", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " I like eleven better. ", null);
			AddRunToMockedPara(para, "12", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " No, twelve rocks! ", null);
			AddRunToMockedPara(para, "13", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " Nobody can touch thirteen. ", null);
			para = AddParaToMockedSectionContent(m_section2, "Line 1");
			AddRunToMockedPara(para, "Second Stanza.", null);

			m_section3 = AddSectionToMockedBook(m_philemon);
			AddSectionHeadParaToSection(m_section3, "Heading for section 3",
				ScrStyleNames.SectionHead);
			para = AddParaToMockedSectionContent(m_section3, "Poetry");
			AddRunToMockedPara(para, "24", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " Hi, I'm verse 24.", null);
			para = AddParaToMockedSectionContent(m_section3, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "25", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " Welcome to the twenty-fifth verse. ", null);
			AddRunToMockedPara(para, "26", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " Welcome to the twenty-sixth verse.", null);
			para = AddParaToMockedSectionContent(m_section3, "Line 1");
			AddRunToMockedPara(para, "27", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, " This is the end.", null);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the corresponding vernacular paragraph for a section head BT para
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindSectionHeadPara()
		{
			int iSection = -1; // should be ignored
			var target = m_section3.HeadingOA.ParagraphsOS[0];
			IStTxtPara para = m_scr.FindPara(m_stylesheet.FindStyle(ScrStyleNames.SectionHead),
				new BCVRef(57, 1, 24), 0, ref iSection);
			Assert.AreEqual(target, para);
			Assert.AreEqual(2, iSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the corresponding vernacular paragraph for a section head BT para
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindSectionHeadPara_VeryFirst()
		{
			int iSection = 0;
			var target = m_section1.HeadingOA.ParagraphsOS[0];
			IStTxtPara para = m_scr.FindPara(m_stylesheet.FindStyle(ScrStyleNames.SectionHead),
				new BCVRef(57, 1, 0), 0, ref iSection);
			Assert.AreEqual(target, para);
			Assert.AreEqual(0, iSection);

			target = m_section1.HeadingOA.ParagraphsOS[1];
			para = m_scr.FindPara(m_stylesheet.FindStyle("Parallel Passage"),
				new BCVRef(57, 1, 0), 1, ref iSection);
			Assert.AreEqual(target, para);
			Assert.AreEqual(0, iSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fail to find the corresponding vernacular paragraph for a section head BT para
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FailToFindSectionHeadPara_ParaIndexTooBig()
		{
			int iSection = -1; // should be ignored
			IStTxtPara para = m_scr.FindPara(m_stylesheet.FindStyle(ScrStyleNames.SectionHead),
				new BCVRef(57, 1, 24), 1, ref iSection);
			Assert.IsNull(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fail to find the corresponding vernacular paragraph for a section head BT para
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FailToFindSectionHeadPara_IncorrectStyle()
		{
			int iSection = -1; // should be ignored
			IStTxtPara para = m_scr.FindPara(m_stylesheet.FindStyle("Parallel Passage"),
				new BCVRef(57, 1, 24), 0, ref iSection);
			Assert.IsNull(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindCorrespondingVernParaForSegment method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindVerseInNormalPara_VeryFirst()
		{
			int iSection = 0;
			var target = m_section1.ContentOA.ParagraphsOS[0];
			IStTxtPara para = m_scr.FindPara(m_stylesheet.FindStyle(ScrStyleNames.NormalParagraph),
				new BCVRef(57, 1, 0), 0, ref iSection);
			Assert.AreEqual(target, para);
			Assert.AreEqual(0, iSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindCorrespondingVernParaForSegment method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindVerseInNormalPara()
		{
			int iSection = -1; // should be ignored
			var target = m_section3.ContentOA.ParagraphsOS[1];
			IStTxtPara para = m_scr.FindPara(m_stylesheet.FindStyle(ScrStyleNames.NormalParagraph),
				new BCVRef(57, 1, 25), 0, ref iSection);
			Assert.AreEqual(target, para);
			Assert.AreEqual(2, iSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindCorrespondingVernParaForSegment method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindContentParaWithNoVerseNumber()
		{
			int iSection = 1;
			var target = m_section2.ContentOA.ParagraphsOS[m_section2.ContentOA.ParagraphsOS.Count - 1];
			IStTxtPara para = m_scr.FindPara(
				m_stylesheet.FindStyle("Line 1"), new BCVRef(57, 1, 13), 1, ref iSection);
			Assert.AreEqual(target, para);
			Assert.AreEqual(1, iSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindCorrespondingVernParaForSegment method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindVerseInPoetryPara()
		{
			int iSection = -1;  // should be ignored
			var target = m_section3.ContentOA.ParagraphsOS[0];
			IStTxtPara para = m_scr.FindPara(m_stylesheet.FindStyle("Poetry"),
				new BCVRef(57, 1, 24), 0, ref iSection);
			Assert.AreEqual(target, para);
			Assert.AreEqual(2, iSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindCorrespondingVernParaForSegment method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindVerseInBridge()
		{
			int iSection = -1;  // should be ignored
			var target = m_section1.ContentOA.ParagraphsOS[0];
			IStTxtPara para = m_scr.FindPara(m_stylesheet.FindStyle(ScrStyleNames.NormalParagraph),
				new BCVRef(57, 1, 7), 0, ref iSection);
			Assert.AreEqual(target, para);
			Assert.AreEqual(0, iSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindCorrespondingVernParaForSegment method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FailToFindVerse_MissingVerse()
		{
			int iSection = -1;  // should be ignored
			IStTxtPara para = m_scr.FindPara(m_stylesheet.FindStyle(ScrStyleNames.NormalParagraph),
				new BCVRef(57, 1, 9), 0, ref iSection);
			Assert.IsNull(para);
			Assert.AreEqual(0, iSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindCorrespondingVernParaForSegment method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FailToFindVerse_DifferentParaStyle()
		{
			int iSection = -1;  // should be ignored
			IStTxtPara para = m_scr.FindPara(m_stylesheet.FindStyle("Poetry"),
				new BCVRef(57, 1, 12), 0, ref iSection);
			Assert.IsNull(para);
			Assert.AreEqual(0, iSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindCorrespondingVernParaForSegment method for finding a title paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindTitlePara()
		{
			int iSection = -1;  // should be ignored
			var target = m_philemon.TitleOA.ParagraphsOS[0];
			IStTxtPara para = m_scr.FindPara(m_stylesheet.FindStyle(ScrStyleNames.MainBookTitle),
				new BCVRef(57, 0, 0), 0, ref iSection);
			Assert.AreEqual(target, para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindCorrespondingVernParaForSegment method for finding the second title
		/// paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindTitlePara_SecondPara()
		{
			IStTxtPara para = AddParaToMockedText(m_philemon.TitleOA, ScrStyleNames.MainBookTitle);
			AddRunToMockedPara(para, "Second title para", null);

			int iSection = -1;  // should be ignored
			var target = m_philemon.TitleOA.ParagraphsOS[1];
			para = (IStTxtPara)m_scr.FindPara(m_stylesheet.FindStyle(ScrStyleNames.MainBookTitle),
				new BCVRef(57, 0, 0), 1, ref iSection);
			Assert.AreEqual(target, para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindCorrespondingVernParaForSegment method for finding a title paragraph if
		/// the vernacular book doesn't exist
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FailToFindTitlePara_NoCorrespondingVernBook()
		{
			int iSection = -1;  // should be ignored
			IStTxtPara para = m_scr.FindPara(m_stylesheet.FindStyle(ScrStyleNames.MainBookTitle),
				new BCVRef(6, 0, 0), 0, ref iSection);
			Assert.IsNull(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindCorrespondingVernParaForSegment method for finding the second title
		/// paragraph if there is only one title para
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FailToFindSecondTitlePara()
		{
			int iSection = -1;  // should be ignored
			IStTxtPara para = m_scr.FindPara(m_stylesheet.FindStyle(ScrStyleNames.MainBookTitle),
				new BCVRef(57, 0, 0), 1, ref iSection);
			Assert.IsNull(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FindCorrespondingVernParaForSegment method for finding a title paragraph.
		/// We have a main title followed by a secondary title and are looking for the first
		/// title paragraph, but we (incorrectly) expect it to be a secondary title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FailToFindTitlePara_DifferentStyles()
		{
			IStTxtPara para = AddParaToMockedText(m_philemon.TitleOA, "User added title style");
			AddRunToMockedPara(para, "Second title para", null);

			var target = m_philemon.TitleOA.ParagraphsOS[1];
			int iSection = -1;  // should be ignored
			para = m_scr.FindPara(m_stylesheet.FindStyle("User added title style"),
				new BCVRef(57, 0, 0), 0, ref iSection);
			Assert.IsNull(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the corresponding vernacular paragraph by reference without regard to
		/// a paragraph style name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindParaByReference()
		{
			int iSection = -1; // should be ignored
			var target = m_section3.ContentOA.ParagraphsOS[0];
			IStTxtPara para = m_scr.FindPara(null, new BCVRef(57, 1, 24), 0, ref iSection);
			Assert.AreEqual(target, para);
			Assert.AreEqual(2, iSection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fail to find the corresponding vernacular paragraph by reference without regard to
		/// a paragraph style name when the reference does not exist
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FailToFindParaByReference_NonExistentReference()
		{
			int iSection = -1; // should be ignored
			IStTxtPara para = m_scr.FindPara(null, new BCVRef(58, 1, 24), 0, ref iSection);
			Assert.IsNull(para);
		}
	}
	#endregion
}
