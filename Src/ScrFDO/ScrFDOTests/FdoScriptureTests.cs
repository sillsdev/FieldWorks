// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2004' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FdoScriptureTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Test.TestUtils;
using SILUBS.SharedScrUtils;
using System.Text;

namespace SIL.FieldWorks.FDO.Scripture
{
	#region class Dummy ScrCheckingTokens
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy Scripture Checking Token representing part of a paragraph
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyParaCheckingToken : ScrCheckingToken
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyParaCheckingToken"/> class.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <param name="ws">The writing system</param>
		/// <param name="paraOffset">The para offset.</param>
		/// ------------------------------------------------------------------------------------
		public DummyParaCheckingToken(ICmObject obj, int ws, int paraOffset) :
			this(obj, ws, paraOffset, new BCVRef(1, 3, 34), new BCVRef(1, 3, 34))
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyParaCheckingToken"/> class.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <param name="ws">The writing system</param>
		/// <param name="paraOffset">The para offset.</param>
		/// <param name="startRef">The start reference.</param>
		/// <param name="endRef">The end reference.</param>
		/// ------------------------------------------------------------------------------------
		public DummyParaCheckingToken(ICmObject obj, int ws, int paraOffset,
			BCVRef startRef, BCVRef endRef)
		{
			m_startRef = startRef;
			m_endRef = endRef;
			m_fNoteStart = false;
			m_fParagraphStart = false;
			m_icuLocale = null;
			m_object = obj;
			m_sText = "This is lousy text and it is bad, it is";
			m_paraStyleName = ScrStyleNames.NormalParagraph;
			m_textType = TextType.Verse;
			Ws = ws;
			m_flid = (int)StTxtPara.StTxtParaTags.kflidContents;
			m_paraOffset = paraOffset;
		}

	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy Scripture Checking Token representing part of a picture caption
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyPictureCheckingToken : ScrCheckingToken
	{
		public DummyPictureCheckingToken(ICmObject obj, int ws, string icuLocale)
		{
			m_startRef = m_endRef = new BCVRef(1, 3, 34);
			m_fNoteStart = false;
			m_fParagraphStart = false;
			m_icuLocale = icuLocale;
			m_object = obj;
			m_sText = "La biblioteque in Monroe";
			m_paraStyleName = ScrStyleNames.Figure;
			m_textType = TextType.Other;
			Ws = ws;
			m_flid = (int)CmPicture.CmPictureTags.kflidCaption;
		}
	}
	#endregion

	#region DummyEditorialCheck class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy "check" that just calls the RecordError delegate once for each "error" in its
	/// list.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyEditorialCheck : IScriptureCheck
	{
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Simple class to hold the information about each "error"
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		internal class DummyError
		{
			internal ScrCheckingToken m_token;
			internal int m_ichStart;
			internal int m_length;
			internal string m_sMessage;

			internal DummyError(ScrCheckingToken token, int ichStart, int length, string sMessage)
			{
				m_token = token;
				m_ichStart = ichStart;
				m_length = length;
				m_sMessage = sMessage;
			}
		}

		#region Data members
		private Guid m_checkId;
		internal List<DummyError> m_ErrorsToReport = new List<DummyError>();
		#endregion

		#region Constructor
		public DummyEditorialCheck(Guid checkId)
		{
			m_checkId = checkId;
		}
		#endregion

		#region IScriptureCheck Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Execute the check and call 'RecordError' for every error found.
		/// </summary>
		/// <param name="toks">ITextToken's corresponding to the text to be checked.
		/// Typically this is one books worth.</param>
		/// <param name="record">Call this delegate to report each error found.</param>
		/// ------------------------------------------------------------------------------------
		public void Check(IEnumerable<ITextToken> toks, RecordErrorHandler record)
		{
			foreach (DummyError error in m_ErrorsToReport)
			{
				TextTokenSubstring tts = new TextTokenSubstring(error.m_token,
					error.m_ichStart, error.m_length, error.m_sMessage);
				record(new RecordErrorEventArgs(tts, m_checkId));
			}
		}

		public string CheckGroup
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public Guid CheckId
		{
			get { return m_checkId; }
		}

		public float RelativeOrder
		{
			get { return 0F; }
		}

		public string CheckName
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public string Description
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public List<TextTokenSubstring> GetReferences(IEnumerable<ITextToken> tokens, string desiredKey)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public string InvalidItems
		{
			get
			{
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		public string InventoryColumnHeader
		{
			get { throw new Exception("The method or operation is not implemented."); }
		}

		public void Save()
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public string ValidItems
		{
			get
			{
				throw new Exception("The method or operation is not implemented.");
			}
			set
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		#endregion
	}
	#endregion

	#region DummyScrChecksDataSource
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy ScrChecksDataSource that modifies the behavior of ScrChecksDataSource to be used
	/// in tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyScrChecksDataSource : ScrChecksDataSource
	{
		#region Data members
		private int m_maxIdenticalErrors;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyScrChecksDataSource"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="maxIdenticalErrors">The maximum number of identical errors that will
		/// be allowed for a Scripture check.</param>
		/// ------------------------------------------------------------------------------------
		public DummyScrChecksDataSource(FdoCache cache, int maxIdenticalErrors) : base(cache)
		{
			m_maxIdenticalErrors = maxIdenticalErrors;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the max identical errors.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int MaxIdenticalErrors
		{
			set { m_maxIdenticalErrors = value; }
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the max identical errors.
		/// </summary>
		/// <param name="checkId">The unique id of the check.</param>
		/// <returns>
		/// the number of errors allowed for the check, or a default value if not
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected override int GetMaxIdenticalErrors(Guid checkId)
		{
			return m_maxIdenticalErrors;
		}
		#endregion
	}
	#endregion

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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does setup for all the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			base.Initialize();
			m_inMemoryCache.InitializeWritingSystemEncodings();
			m_inMemoryCache.ChangeDefaultAnalWs(m_inMemoryCache.SetupWs("de"));
			m_inMemoryCache.InitializeAnnotationDefs();
			CmAnnotationDefn annDefnChkError = new CmAnnotationDefn(Cache,
				LangProject.kguidAnnCheckingError);

			CmAnnotationDefn errorType1 = new CmAnnotationDefn();
			annDefnChkError.SubPossibilitiesOS.Append(errorType1);
			errorType1.Guid = kCheckId1;
			errorType1.IsProtected = true;
			errorType1.Name.UserDefaultWritingSystem = "Dummy Check 1";
			errorType1.Description.UserDefaultWritingSystem.UnderlyingTsString =
				StringUtils.MakeTss("does nothing", Cache.DefaultUserWs);

			CmAnnotationDefn errorType2 = new CmAnnotationDefn();
			annDefnChkError.SubPossibilitiesOS.Append(errorType2);
			errorType2.Guid = kCheckId2;
			errorType2.IsProtected = true;
			errorType2.Name.UserDefaultWritingSystem = "Dummy Check 2";
			errorType2.Description.UserDefaultWritingSystem.UnderlyingTsString =
				StringUtils.MakeTss("does nothing", Cache.DefaultUserWs);
		}

		#region Find book tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding a book based on the cannonical order
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindBook()
		{
			CheckDisposed();

			IScrBook genesis = m_scrInMemoryCache.AddBookWithTwoSections(1, "Genesis");
			IScrBook foundBook = m_scr.FindBook(1);
			Assert.AreEqual(genesis.Hvo, foundBook.Hvo);
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
			CheckDisposed();
			IScrRefSystem scr = m_inMemoryCache.Cache.ScriptureReferenceSystem;
			Assert.AreEqual("GEN", ((ScrBookRef)scr.BooksOS[0]).UIBookAbbrev);
			Assert.AreEqual("REV", ((ScrBookRef)scr.BooksOS[65]).UIBookAbbrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the name of a book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UIBookName()
		{
			CheckDisposed();
			IScrRefSystem scr = m_inMemoryCache.Cache.ScriptureReferenceSystem;
			Assert.AreEqual("GEN", ((ScrBookRef)scr.BooksOS[0]).UIBookName);
			Assert.AreEqual("REV", ((ScrBookRef)scr.BooksOS[65]).UIBookName);
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
			CheckDisposed();

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
			CheckDisposed();

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
			CheckDisposed();

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
			CheckDisposed();

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
			CheckDisposed();

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
			CheckDisposed();

			m_scrInMemoryCache.AddScrStyle("Section Head", ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, false);

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
			CheckDisposed();

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
			CheckDisposed();

			ScrScriptureNote annotation = new ScrScriptureNote();

			// add this annotation collection to the list
			FdoOwningSequence<IScrScriptureNote> genesisNotes = m_scr.BookAnnotationsOS[0].NotesOS;
			genesisNotes.Append(annotation);
			int cNotesBeforeResponse = genesisNotes.Count;

			IStJournalText response = annotation.CreateResponse();
			Assert.IsNotNull(response);
			Assert.AreEqual(cNotesBeforeResponse, genesisNotes.Count,
				"Response annotation should not get added to master list.");
			Assert.AreEqual(1, annotation.ResponsesOS.Count);
			Assert.AreEqual(response.Hvo, annotation.ResponsesOS.HvoArray[0]);
			Assert.AreEqual(1, response.ParagraphsOS.Count);
			ITsTextProps ttpParaStyle = response.ParagraphsOS[0].StyleRules;
			Assert.AreEqual(ScrStyleNames.Remark, ttpParaStyle.GetStrPropValue(
				(int)FwTextPropType.ktptNamedStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ScrBookAnnotations.GetFirstNoteForReference method when there are no notes in
		/// the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFirstNoteForReference_InEmptyList()
		{
			CheckDisposed();

			ScrBookAnnotations annotations = (ScrBookAnnotations)m_scr.BookAnnotationsOS[0];
			Assert.IsNull(annotations.GetFirstNoteForReference(new BCVRef(1, 1, 2)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ScrBookAnnotations.GetFirstNoteForReference method when there are no notes in
		/// the list for the given reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFirstNoteForReference_NoNoteForReference()
		{
			CheckDisposed();

			ScrBookAnnotations annotations = (ScrBookAnnotations)m_scr.BookAnnotationsOS[0];
			BCVRef ref1 = new BCVRef(1, 1, 1);
			annotations.InsertNote(ref1, ref1, null, null, LangProject.kguidAnnConsultantNote);
			BCVRef ref3 = new BCVRef(1, 1, 3);
			annotations.InsertNote(ref3, ref3, null, null, LangProject.kguidAnnConsultantNote);
			Assert.IsNull(annotations.GetFirstNoteForReference(new BCVRef(1, 1, 2)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ScrBookAnnotations.GetFirstNoteForReference method for the first, middle, and
		/// last notes in the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFirstNoteForReference_NotesForReference()
		{
			CheckDisposed();

			ScrBookAnnotations annotations = (ScrBookAnnotations)m_scr.BookAnnotationsOS[0];
			BCVRef ref1 = new BCVRef(1, 1, 1);
			BCVRef ref2 = new BCVRef(1, 1, 2);
			BCVRef ref3 = new BCVRef(1, 1, 3);

			// Insert notes for Genesis 1:1, 1:2, and 1:3
			IScrScriptureNote note1 = annotations.InsertNote(ref1, ref1, null, null, LangProject.kguidAnnConsultantNote);
			IScrScriptureNote note2a = annotations.InsertNote(ref2, ref2, null, null, LangProject.kguidAnnConsultantNote);
			IScrScriptureNote note2b = annotations.InsertNote(ref2, ref2, null, null, LangProject.kguidAnnConsultantNote);
			IScrScriptureNote note3 = annotations.InsertNote(ref3, ref3, null, null, LangProject.kguidAnnConsultantNote);

			// Get notes from each verse
			Assert.AreEqual(4, annotations.NotesOS.Count);
			Assert.AreEqual(note1, annotations.GetFirstNoteForReference(ref1));
			Assert.AreEqual(note2a, annotations.GetFirstNoteForReference(ref2));
			Assert.AreEqual(note3, annotations.GetFirstNoteForReference(ref3));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ScrBookAnnotations.GetFirstNoteForReference method when the notes refer to
		/// a bridge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFirstNoteForReference_NotesForReferenceInBridge()
		{
			CheckDisposed();

			ScrBookAnnotations annotations = (ScrBookAnnotations)m_scr.BookAnnotationsOS[0];
			BCVRef ref1 = new BCVRef(1, 1, 1);
			BCVRef ref2 = new BCVRef(1, 1, 2);
			BCVRef ref3 = new BCVRef(1, 1, 3);

			// Insert notes for Genesis 1:1, 1:2, and 1:3
			annotations.InsertNote(ref1, ref3, null, null, LangProject.kguidAnnConsultantNote);
			annotations.InsertNote(ref2, ref3, null, null, LangProject.kguidAnnConsultantNote);
			annotations.InsertNote(ref3, ref3, null, null, LangProject.kguidAnnConsultantNote);

			// Get notes from each verse
			IScrScriptureNote note1 = annotations.GetFirstNoteForReference(ref1);
			IScrScriptureNote note2 = annotations.GetFirstNoteForReference(ref2);
			IScrScriptureNote note3 = annotations.GetFirstNoteForReference(ref3);

			// Verify that correct note was returned.
			Assert.AreEqual(1001001, note1.BeginRef);
			Assert.AreEqual(1001003, note1.EndRef);
			Assert.AreEqual(note1, note2);
			Assert.AreEqual(note1, note3);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ScrBookAnnotations.InsertNote method when there are no notes in the list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertNote_InEmptyList()
		{
			CheckDisposed();

			ScrBookAnnotations annotations = (ScrBookAnnotations)m_scr.BookAnnotationsOS[0];
			BCVRef ref1 = new BCVRef(1, 1, 1);
			BCVRef ref2 = new BCVRef(1, 1, 2);
			IScrScriptureNote note = annotations.InsertNote(ref1, ref2, null, null, LangProject.kguidAnnConsultantNote);
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
			CheckDisposed();

			ScrBookAnnotations annotations = (ScrBookAnnotations)m_scr.BookAnnotationsOS[0];
			BCVRef ref1 = new BCVRef(1, 1, 1);
			BCVRef ref2 = new BCVRef(1, 1, 2);
			BCVRef ref3 = new BCVRef(1, 1, 3);
			IScrScriptureNote note = annotations.InsertNote(ref2, ref2, null, null, LangProject.kguidAnnConsultantNote);
			Assert.IsNotNull(note);

			// Insert notes at beginning and end of list
			IScrScriptureNote note1 = annotations.InsertNote(ref1, ref2, null, null, LangProject.kguidAnnConsultantNote);
			IScrScriptureNote note2 = annotations.InsertNote(ref3, ref3, null, null, LangProject.kguidAnnConsultantNote);
			IScrScriptureNote note3 = annotations.InsertNote(ref3, ref3, null, null, LangProject.kguidAnnConsultantNote);

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
			IScrBook genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_scrInMemoryCache.AddTitleToMockedBook(genesis.Hvo, "Genesis");

			// Introduction section
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Introduction head", ScrStyleNames.IntroSectionHead);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "An intro to Genesis", null);
			section.AdjustReferences(true);

			ScrBookAnnotations annotations = (ScrBookAnnotations)m_scr.BookAnnotationsOS[0];
			BCVRef ref1 = new BCVRef(1, 1, 0);
			IScrScriptureNote note = annotations.InsertNote(ref1, ref1, null, null, LangProject.kguidAnnConsultantNote);
			Assert.IsNotNull(note);

			m_scr.AdjustAnnotationReferences();

			Assert.AreEqual(para.Hvo, note.BeginObjectRA.Hvo);
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
			IScrBook genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			StText titleText = m_scrInMemoryCache.AddTitleToMockedBook(genesis.Hvo, "Genesis");
			IStPara titlePara = titleText.ParagraphsOS[0];

			// First scripture section
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "First section head", ScrStyleNames.SectionHead);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "First verse in Genesis", null);
			section.AdjustReferences();

			ScrBookAnnotations annotations = (ScrBookAnnotations)m_scr.BookAnnotationsOS[0];
			BCVRef ref1 = new BCVRef(1, 1, 0);
			IScrScriptureNote note = annotations.InsertNote(ref1, ref1, null, null, LangProject.kguidAnnConsultantNote);
			Assert.IsNotNull(note);

			m_scr.AdjustAnnotationReferences();

			Assert.AreEqual(titlePara.Hvo, note.BeginObjectRA.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ScrBookAnnotations.AdjustAnnotationReferences for case where referenced
		/// paragraph cannot be found and no quote exists. Should be assigned to first title
		/// paragraph, if there is no introduction.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AdjustAnnotationReferences_ParaInArchive()
		{
			IScrBook genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			StText titleText = m_scrInMemoryCache.AddTitleToMockedBook(genesis.Hvo, "Genesis");
			IStPara titlePara = titleText.ParagraphsOS[0];

			// Introduction section
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Introduction head", ScrStyleNames.IntroSectionHead);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "An intro to Genesis", null);
			section.AdjustReferences(true);

			IScrDraft version = new ScrDraft();
			m_scr.ArchivedDraftsOC.Add(version);
			int hvoSavedVersion = m_scr.AddBookToSavedVersion(version, genesis.Hvo);
			IScrBook genesisSaved = new ScrBook(m_scrInMemoryCache.Cache, hvoSavedVersion);
			IStPara paraSaved = genesisSaved.SectionsOS[0].ContentOA.ParagraphsOS[0];

			ScrBookAnnotations annotations = (ScrBookAnnotations)m_scr.BookAnnotationsOS[0];
			BCVRef ref1 = new BCVRef(1, 1, 0);
			IScrScriptureNote note = annotations.InsertNote(ref1, ref1, paraSaved, paraSaved, LangProject.kguidAnnConsultantNote);
			Assert.IsNotNull(note);

			m_scr.AdjustAnnotationReferences();

			Assert.AreEqual(para.Hvo, note.BeginObjectRA.Hvo);
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
			IScrBook genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_scrInMemoryCache.AddTitleToMockedBook(genesis.Hvo, "Genesis");

			// Introduction section
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(genesis.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Introduction head", ScrStyleNames.IntroSectionHead);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "An intro to Genesis", null);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Contains quoted text - the quote", null);
			section.AdjustReferences(true);

			ScrBookAnnotations annotations = (ScrBookAnnotations)m_scr.BookAnnotationsOS[0];
			BCVRef ref1 = new BCVRef(1, 1, 0);
			int iPos;
			StTxtParaBldr quoteBldr = new StTxtParaBldr(m_scrInMemoryCache.Cache);
			quoteBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.Remark);
			quoteBldr.AppendRun("the quote",
				StyleUtils.CharStyleTextProps(null, m_scrInMemoryCache.Cache.DefaultVernWs));
			IScrScriptureNote note = annotations.InsertNote(ref1, ref1, null, null, LangProject.kguidAnnConsultantNote, -1, 0, 0, quoteBldr, null, null, null, out iPos);
			Assert.IsNotNull(note);

			m_scr.AdjustAnnotationReferences();

			Assert.AreEqual(para.Hvo, note.BeginObjectRA.Hvo);
			Assert.AreEqual(23, note.BeginOffset);
			Assert.AreEqual(32, note.EndOffset);
		}
		#endregion

		#region Checking tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the RecordError method for a token representing part of the contents of a
		/// paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_ParaContentsToken()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);

			Dictionary<int, Dictionary<Guid, ScrCheckRunResult>> bkChkFailedLst =
				new Dictionary<int, Dictionary<Guid, ScrCheckRunResult>>();
			bkChkFailedLst[tok.StartRef.Book] = new Dictionary<Guid, ScrCheckRunResult>();
			bkChkFailedLst[tok.StartRef.Book][kCheckId1] = ScrCheckRunResult.NoInconsistencies;
			ReflectionHelper.SetField(dataSource, "m_bookChecksFailed", bkChkFailedLst);

			TextTokenSubstring tts = new TextTokenSubstring(tok, 5, 8, "Lousy message");
			dataSource.RecordError(new RecordErrorEventArgs(tts, kCheckId1));
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			IScrScriptureNote note = annotations.NotesOS[0];
			Assert.IsNotNull(note);
			Assert.AreEqual(NoteType.CheckingError, note.AnnotationType);
			Assert.AreEqual(m_scr, note.BeginObjectRA);
			Assert.AreEqual(m_scr, note.EndObjectRA);
			Assert.AreEqual(5, note.BeginOffset);
			Assert.AreEqual(13, note.EndOffset);
			Assert.AreEqual(0, note.CategoriesRS.Count);
			Assert.AreEqual(1, note.QuoteOA.ParagraphsOS.Count);
			ITsStrFactory factory = TsStrFactoryClass.Create();
			ITsString tssQuote = factory.MakeString("is lousy", Cache.DefaultVernWs);
			AssertEx.AreTsStringsEqual(tssQuote, ((StTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.UnderlyingTsString);
			Assert.AreEqual(1, note.DiscussionOA.ParagraphsOS.Count);
			Assert.AreEqual("Lousy message", ((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(NoteStatus.Open, note.ResolutionStatus);
			VerifyEmptyStJournalText(note.RecommendationOA);
			VerifyEmptyStJournalText(note.ResolutionOA);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, note.Flid);
			// TODO: Test this for an annotation on a CmPicture: Assert.AreEqual(0, note.WsSelector);
			Assert.AreEqual(01003034, note.BeginRef);
			Assert.AreEqual(01003034, note.EndRef);
			Assert.AreEqual(Cache.LangProject.DefaultComputerAgent, note.SourceRA);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the RecordError method for a token representing part of the contents of a
		/// paragraph in the middle of a paragraph (not first run). We pretend the second run
		/// starts at ich=10.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_ParaContentsToken_SecondRun()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 10);

			Dictionary<int, Dictionary<Guid, ScrCheckRunResult>> bkChkFailedLst =
				new Dictionary<int, Dictionary<Guid, ScrCheckRunResult>>();
			bkChkFailedLst[tok.StartRef.Book] = new Dictionary<Guid, ScrCheckRunResult>();
			bkChkFailedLst[tok.StartRef.Book][kCheckId1] = ScrCheckRunResult.NoInconsistencies;
			ReflectionHelper.SetField(dataSource, "m_bookChecksFailed", bkChkFailedLst);

			TextTokenSubstring tts = new TextTokenSubstring(tok, 5, 8, "Lousy message");
			dataSource.RecordError(new RecordErrorEventArgs(tts, kCheckId1));
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			IScrScriptureNote note = annotations.NotesOS[0];
			Assert.IsNotNull(note);
			Assert.AreEqual(NoteType.CheckingError, note.AnnotationType);
			Assert.AreEqual(m_scr, note.BeginObjectRA);
			Assert.AreEqual(m_scr, note.EndObjectRA);
			Assert.AreEqual(15, note.BeginOffset);
			Assert.AreEqual(23, note.EndOffset);
			Assert.AreEqual(0, note.CategoriesRS.Count);
			Assert.AreEqual(1, note.QuoteOA.ParagraphsOS.Count);
			ITsStrFactory factory = TsStrFactoryClass.Create();
			ITsString tssQuote = factory.MakeString("is lousy", Cache.DefaultVernWs);
			AssertEx.AreTsStringsEqual(tssQuote, ((StTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.UnderlyingTsString);
			Assert.AreEqual(1, note.DiscussionOA.ParagraphsOS.Count);
			Assert.AreEqual("Lousy message", ((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(NoteStatus.Open, note.ResolutionStatus);
			VerifyEmptyStJournalText(note.RecommendationOA);
			VerifyEmptyStJournalText(note.ResolutionOA);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, note.Flid);
			// TODO: Test this for an annotation on a CmPicture: Assert.AreEqual(0, note.WsSelector);
			Assert.AreEqual(01003034, note.BeginRef);
			Assert.AreEqual(01003034, note.EndRef);
			// TODO: Assert.AreEqual(???, note.SourceRA);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the RecordError method for a token representing part of a picture caption.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_PictureCaptionToken()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			ScrCheckingToken tok = new DummyPictureCheckingToken(m_scr, Cache.DefaultUserWs, "en");

			Dictionary<int, Dictionary<Guid, ScrCheckRunResult>> bkChkFailedLst =
				new Dictionary<int, Dictionary<Guid, ScrCheckRunResult>>();
			bkChkFailedLst[tok.StartRef.Book] = new Dictionary<Guid, ScrCheckRunResult>();
			bkChkFailedLst[tok.StartRef.Book][kCheckId1] = ScrCheckRunResult.NoInconsistencies;
			ReflectionHelper.SetField(dataSource, "m_bookChecksFailed", bkChkFailedLst);

			TextTokenSubstring tts = new TextTokenSubstring(tok, 15, 9, "Weird bilingual picture caption");
			dataSource.RecordError(new RecordErrorEventArgs(tts, kCheckId1));
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			IScrScriptureNote note = annotations.NotesOS[0];
			Assert.IsNotNull(note);
			Assert.AreEqual(NoteType.CheckingError, note.AnnotationType);
			Assert.AreEqual(m_scr, note.BeginObjectRA);
			Assert.AreEqual(m_scr, note.EndObjectRA);
			Assert.AreEqual(15, note.BeginOffset);
			Assert.AreEqual(24, note.EndOffset);
			Assert.AreEqual(0, note.CategoriesRS.Count);
			Assert.AreEqual(1, note.QuoteOA.ParagraphsOS.Count);
			ITsStrFactory factory = TsStrFactoryClass.Create();
			ITsString tssQuote = factory.MakeString("in Monroe", Cache.DefaultUserWs);
			AssertEx.AreTsStringsEqual(tssQuote, ((StTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.UnderlyingTsString);
			Assert.AreEqual(1, note.DiscussionOA.ParagraphsOS.Count);
			Assert.AreEqual("Weird bilingual picture caption", ((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(NoteStatus.Open, note.ResolutionStatus);
			VerifyEmptyStJournalText(note.RecommendationOA);
			VerifyEmptyStJournalText(note.ResolutionOA);
			Assert.AreEqual((int)CmPicture.CmPictureTags.kflidCaption, note.Flid);
			Assert.AreEqual(Cache.DefaultVernWs, note.WsSelector);
			Assert.AreEqual(01003034, note.BeginRef);
			Assert.AreEqual(01003034, note.EndRef);
			Assert.AreEqual(Cache.LangProject.DefaultComputerAgent, note.SourceRA);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method does not create duplicate error annotations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_Duplicate()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 8, "Lousy message"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "My Favorite Book");

			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(1, annotations.NotesOS.Count,
				"First run of check should add error annotation");

			dataSource.RunCheck(check);
			Assert.AreEqual(1, annotations.NotesOS.Count,
				"Second run of check shouldn't create a duplicate.");
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method does not create duplicate error annotations when
		/// the token has its MissingStartRef property set (which requires the verse refs of the
		/// token to be adjusted).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_DuplicateAfterAdjustingReference()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);
			tok.MissingStartRef = new BCVRef(tok.StartRef);
			tok.MissingStartRef.Verse++;
			tok.MissingEndRef = new BCVRef(tok.MissingStartRef);
			tok.MissingEndRef.Verse++; // this simulates missing two verses
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 1, 1, "3"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "My Favorite Book");

			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(1, annotations.NotesOS.Count,
				"First run of check should add error annotation");

			annotations.NotesOS[0].ResolutionStatus = NoteStatus.Closed;

			// Need a new token because the one above has already gotten changed.
			tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);
			tok.MissingStartRef = new BCVRef(tok.StartRef);
			tok.MissingStartRef.Verse++;
			tok.MissingEndRef = new BCVRef(tok.MissingStartRef);
			tok.MissingEndRef.Verse++; // this simulates missing two verses
			check.m_ErrorsToReport.Clear();
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 1, 1, "3"));

			dataSource.RunCheck(check);
			Assert.AreEqual(1, annotations.NotesOS.Count,
				"Second run of check shouldn't create a duplicate.");
			Assert.AreEqual(NoteStatus.Closed, annotations.NotesOS[0].ResolutionStatus,
				"Annotation should still be resolved/closed (ignored).");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method only records the maximum number of identical
		/// error annotations specified for a check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_ErrorsIdentical()
		{
			// Setup a data source that allows a maximum of two identical errors for all checks.
			// Setup three identical checking errors.
			DummyScrChecksDataSource dataSource = new DummyScrChecksDataSource(Cache, 2);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 2, "identical"));
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 26, 2, "identical"));
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 37, 2, "identical"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "My Favorite Book");

			// Run the Scripture check
			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);

			// We expect three error annotations to be created: two for the allowed identical errors,
			// and one indicating that a maximum has been exceeded.
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(3, annotations.NotesOS.Count,
				"Three error annotations should have been added.");
			for (int iNote = 0; iNote < 2; iNote++)
			{
				// verify allowed identical annotations
				IScrScriptureNote note = annotations.NotesOS[iNote];
				Assert.AreEqual("identical", ((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.Text);
				Assert.AreEqual("is", ((StTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.Text);
			}
			// verify identical error exceeded annotation.
			IScrScriptureNote maxNote = annotations.NotesOS[2];
			Assert.AreEqual("Maximum number of Dummy Check 1 errors exceeded.",
				((StTxtPara)maxNote.DiscussionOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("is", ((StTxtPara)maxNote.QuoteOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method only records the maximum number of identical
		/// error annotations specified for a check. However, for this check, the cited text
		/// is different, so that, even though the messages are the same, the maximum won't
		/// be exceeded until the last error to report.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_ErrorsAlmostIdentical1()
		{
			// Setup a data source that warns the user on the second identical error that they
			// have exceeded the maximum.
			// Setup three identical checking errors.
			DummyScrChecksDataSource dataSource = new DummyScrChecksDataSource(Cache, 1);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);

			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 2, "identical"));
			// For this second error report, the cited text is "it" instead of "is".
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 23, 2, "identical"));
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 37, 2, "identical"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "My Favorite Book");

			// Run the Scripture check
			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);

			// We expect three error annotations to be created: one for the only allowed error,
			// one with cited text that is different, and one indicating that a maximum has been
			// exceeded. The message is (initially) identical in all three annotations until
			// the last annotation has the maximum identical errors exceeded message.
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(3, annotations.NotesOS.Count,
				"Three error annotations should have been added.");
			// verify allowed identical annotations
			IScrScriptureNote note = annotations.NotesOS[0];
			Assert.AreEqual("identical", ((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("is", ((StTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.Text);
			note = annotations.NotesOS[1];
			Assert.AreEqual("identical", ((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("it", ((StTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.Text);

			// verify identical error exceeded annotation.
			IScrScriptureNote maxNote = annotations.NotesOS[2];
			Assert.AreEqual("Maximum number of Dummy Check 1 errors exceeded.",
				((StTxtPara)maxNote.DiscussionOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("is", ((StTxtPara)maxNote.QuoteOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method only records the maximum number of identical
		/// error annotations specified for a check. However, for this check, the message
		/// is different, so that, even though the messages are the same, the maximum won't
		/// be exceeded until the last error to report.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_ErrorsAlmostIdentical2()
		{
			// Setup a data source that warns the user on the second identical error that they
			// have exceeded the maximum.
			// Setup three identical checking errors.
			DummyScrChecksDataSource dataSource = new DummyScrChecksDataSource(Cache, 1);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);

			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 2, "identical"));
			// For this second error report, the message is different.
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 26, 2, "different"));
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 37, 2, "identical"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "My Favorite Book");

			// Run the Scripture check
			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);

			// We expect three error annotations to be created: one for the only allowed error,
			// one with a message that is different, and one indicating that a maximum has been
			// exceeded. The cited text is identical in all three annotations.
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(3, annotations.NotesOS.Count,
				"Three error annotations should have been added.");
			// verify allowed identical annotations
			IScrScriptureNote note = annotations.NotesOS[0];
			Assert.AreEqual("identical", ((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("is", ((StTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.Text);
			note = annotations.NotesOS[1];
			Assert.AreEqual("different", ((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("is", ((StTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.Text);

			// verify identical error exceeded annotation.
			IScrScriptureNote maxNote = annotations.NotesOS[2];
			Assert.AreEqual("Maximum number of Dummy Check 1 errors exceeded.",
				((StTxtPara)maxNote.DiscussionOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("is", ((StTxtPara)maxNote.QuoteOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method allows an unlimited number of identical error
		/// annotations when the maximum is set to -1.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_ErrorsWithNoMax()
		{
			// Setup a data source that does not set a limit on identical errors for all checks.
			// Setup three identical checking errors.
			DummyScrChecksDataSource dataSource = new DummyScrChecksDataSource(Cache, -1);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);

			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 2, "identical"));
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 26, 2, "identical"));
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 37, 2, "identical"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "My Favorite Book");

			// Run the Scripture check
			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);

			// We expect three identical error annotations to be created.
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(3, annotations.NotesOS.Count,
				"Three error annotations should have been added.");
			// verify allowed identical annotations
			for (int iNote = 0; iNote < 3; iNote++)
			{
				// verify allowed identical annotations
				IScrScriptureNote note = annotations.NotesOS[iNote];
				Assert.AreEqual("identical", ((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.Text);
				Assert.AreEqual("is", ((StTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method which, initially, allows no limit on the maximum
		/// number of identical error annotations and then the maximum is changed to 1. We
		/// expect that the second error will have the maximum exceeded warning and the last
		/// error will be deleted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_ErrorMaxDecreased()
		{
			// Setup a data source that does not set a limit on identical errors for all checks.
			// Setup three identical checking errors.
			DummyScrChecksDataSource dataSource = new DummyScrChecksDataSource(Cache, -1);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);

			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 2, "identical"));
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 26, 2, "identical"));
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 37, 2, "identical"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "My Favorite Book");

			// Run the Scripture check
			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(3, annotations.NotesOS.Count,
				"Three error annotations should have been added.");

			// Now update the maximum identical errors to only 1 and run the check again.
			dataSource.MaxIdenticalErrors = 1;
			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);

			// We expect two error annotations: one for the only allowed error and
			// one indicating that a maximum has been exceeded.
			Assert.AreEqual(2, annotations.NotesOS.Count,
				"There should be two error annotations now.");

			IScrScriptureNote note = annotations.NotesOS[0];
			Assert.AreEqual("identical", ((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("is", ((StTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.Text);

			IScrScriptureNote maxNote = annotations.NotesOS[1];
			Assert.AreEqual("Maximum number of Dummy Check 1 errors exceeded.",
				((StTxtPara)maxNote.DiscussionOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("is", ((StTxtPara)maxNote.QuoteOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method which, initially, has the maximum is set to 1 and
		/// then it is set to allow no limit on the maximum number of identical error
		/// annotations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_ErrorMaxIncreased()
		{
			// Setup a data source with a limit of one identical error for all checks.
			// Setup three identical checking errors.
			DummyScrChecksDataSource dataSource = new DummyScrChecksDataSource(Cache, 1);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);

			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 2, "identical"));
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 26, 2, "identical"));
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 37, 2, "identical"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "My Favorite Book");

			// Run the Scripture check
			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(2, annotations.NotesOS.Count,
				"Two error annotations should have been added: one error and one maximum exceeded msg.");

			// Now update the maximum identical errors to be unlimited and run the check again.
			dataSource.MaxIdenticalErrors = -1;
			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);

			// We expect three error annotations and we confirm the last error.
			Assert.AreEqual(3, annotations.NotesOS.Count,
				"There should be three error annotations now.");

			IScrScriptureNote note = annotations.NotesOS[2];
			Assert.AreEqual("identical", ((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("is", ((StTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method does not create duplicate error annotations, even
		/// when the same error occurs twice in a verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_Duplicate_SameErrorTwiceInVerse()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 2, "Lousy message"));
			tok = new DummyParaCheckingToken(m_scr, m_inMemoryCache.Cache.DefaultVernWs, 0);
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 26, 2, "Lousy message"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "My Favorite Book");

			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(2, annotations.NotesOS.Count,
				"First run of check should add two error annotations");
			Assert.AreEqual(5, annotations.NotesOS[0].BeginOffset);
			Assert.AreEqual(26, annotations.NotesOS[1].BeginOffset);
			Assert.AreEqual(28, annotations.NotesOS[1].EndOffset);

			// Change the offset of the second error (but the text is still the same at that ich, so the
			// error's key will be the same).
			check.m_ErrorsToReport[1].m_ichStart = 37;

			dataSource.RunCheck(check);
			Assert.AreEqual(2, annotations.NotesOS.Count,
				"Second run of check shouldn't create a duplicate.");
			Assert.AreEqual(5, annotations.NotesOS[0].BeginOffset,
				"Offset of first annotation shouldn't change.");
			Assert.AreEqual(37, annotations.NotesOS[1].BeginOffset,
				"Begin offset of second annotation should get updated.");
			Assert.AreEqual(39, annotations.NotesOS[1].EndOffset,
				"End offset of second annotation should get updated.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RunCheck_ScrCheckRunRecordsWithFixedInconsistency()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 2, "Verbification"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "The Book of David");

			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];

			Assert.AreEqual(1, annotations.ChkHistRecsOC.Count);
			Assert.AreEqual(1, annotations.NotesOS.Count);

			ScrCheckRun scr =
				new ScrCheckRun(Cache, annotations.ChkHistRecsOC.HvoArray[0]);

			Assert.AreEqual(ScrCheckRunResult.Inconsistencies, scr.Result);
			Assert.AreEqual(NoteStatus.Open, annotations.NotesOS[0].ResolutionStatus);

			check.m_ErrorsToReport.Clear();
			dataSource.RunCheck(check);

			Assert.AreEqual(1, annotations.ChkHistRecsOC.Count);
			Assert.AreEqual(0, annotations.NotesOS.Count);

			scr = new ScrCheckRun(Cache, annotations.ChkHistRecsOC.HvoArray[0]);
			Assert.AreEqual(ScrCheckRunResult.NoInconsistencies, scr.Result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RunCheck_ScrCheckRunRecordsWithOneBookOneCheck()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 2, "Verbification"));
			tok = new DummyParaCheckingToken(m_scr, m_inMemoryCache.Cache.DefaultVernWs, 0);
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 26, 2, "Verbification"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "The Book of David");

			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];

			Assert.AreEqual(1, annotations.ChkHistRecsOC.Count);
			Assert.AreEqual(2, annotations.NotesOS.Count);

			ScrCheckRun scr =
				new ScrCheckRun(Cache, annotations.ChkHistRecsOC.HvoArray[0]);

			Assert.AreEqual(ScrCheckRunResult.Inconsistencies, scr.Result);
			Assert.AreEqual(NoteStatus.Open, annotations.NotesOS[0].ResolutionStatus);
			Assert.AreEqual(NoteStatus.Open, annotations.NotesOS[1].ResolutionStatus);

			annotations.NotesOS[0].ResolutionStatus = NoteStatus.Closed;
			dataSource.RunCheck(check);

			scr = new ScrCheckRun(Cache, annotations.ChkHistRecsOC.HvoArray[0]);
			Assert.AreEqual(ScrCheckRunResult.Inconsistencies, scr.Result);

			annotations.NotesOS[0].ResolutionStatus = NoteStatus.Closed;
			annotations.NotesOS[1].ResolutionStatus = NoteStatus.Closed;
			dataSource.RunCheck(check);
			scr = new ScrCheckRun(Cache, annotations.ChkHistRecsOC.HvoArray[0]);
			Assert.AreEqual(ScrCheckRunResult.IgnoredInconsistencies, scr.Result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RunCheck_ScrCheckRunRecordsWithOneBookTwoChecks()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			DummyEditorialCheck check1 = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);
			check1.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 2, "Verbification"));
			tok = new DummyParaCheckingToken(m_scr, m_inMemoryCache.Cache.DefaultVernWs, 0);
			check1.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 26, 2, "Verbification"));

			DummyEditorialCheck check2 = new DummyEditorialCheck(kCheckId2);
			check2.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 2, "Stupid Check"));
			tok = new DummyParaCheckingToken(m_scr, m_inMemoryCache.Cache.DefaultVernWs, 0);
			check2.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 26, 2, "Stupid Check"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "The Book of David");

			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check1);
			dataSource.RunCheck(check2);
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];

			Assert.AreEqual(2, annotations.ChkHistRecsOC.Count);
			Assert.AreEqual(4, annotations.NotesOS.Count);

			ScrCheckRun scr1 =
				new ScrCheckRun(Cache, annotations.ChkHistRecsOC.HvoArray[0]);
			ScrCheckRun scr2 =
				new ScrCheckRun(Cache, annotations.ChkHistRecsOC.HvoArray[1]);

			Assert.AreEqual(ScrCheckRunResult.Inconsistencies, scr1.Result);
			Assert.AreEqual(ScrCheckRunResult.Inconsistencies, scr2.Result);
			Assert.AreEqual(NoteStatus.Open, annotations.NotesOS[0].ResolutionStatus);
			Assert.AreEqual(NoteStatus.Open, annotations.NotesOS[1].ResolutionStatus);
			Assert.AreEqual(NoteStatus.Open, annotations.NotesOS[2].ResolutionStatus);
			Assert.AreEqual(NoteStatus.Open, annotations.NotesOS[3].ResolutionStatus);

			annotations.NotesOS[0].ResolutionStatus = NoteStatus.Closed;
			annotations.NotesOS[1].ResolutionStatus = NoteStatus.Closed;
			dataSource.RunCheck(check1);

			scr1 = new ScrCheckRun(Cache, annotations.ChkHistRecsOC.HvoArray[0]);
			scr2 = new ScrCheckRun(Cache, annotations.ChkHistRecsOC.HvoArray[1]);
			Assert.AreEqual(ScrCheckRunResult.IgnoredInconsistencies, scr1.Result);
			Assert.AreEqual(ScrCheckRunResult.Inconsistencies, scr2.Result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method does not mistakenly detect as a duplicate an error
		/// annotation which has a different message.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RunCheck_CorrectedErrorGetsDeleted()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 8, "Lousy message"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "My Favorite Book");

			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(1, annotations.NotesOS.Count,
				"First run of check should add 1 error annotation");
			IScrScriptureNote origErrorAnnotation = annotations.NotesOS[0];
			Assert.AreEqual("Lousy message",
				((StTxtPara)origErrorAnnotation.DiscussionOA.ParagraphsOS[0]).Contents.Text);

			check.m_ErrorsToReport.Clear();
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 8, "Goofy message"));
			dataSource.RunCheck(check);
			Assert.AreEqual(1, annotations.NotesOS.Count,
				"Second run of check should delete the 'fixed' error annotation and add another error annotation.");
			IScrScriptureNote newErrorAnnotation = annotations.NotesOS[0];
			Assert.AreNotEqual(origErrorAnnotation, newErrorAnnotation);
			Assert.AreEqual("Goofy message",
				((StTxtPara)newErrorAnnotation.DiscussionOA.ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method does not mistakenly detect as a duplicate an error
		/// annotation which has a different message.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_NearDuplicate_DifferByMessage()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 8, "Lousy message"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "My Favorite Book");

			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(1, annotations.NotesOS.Count,
				"First run of check should add 1 error annotation");
			IScrScriptureNote origErrorAnnotation = annotations.NotesOS[0];

			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 8, "Goofy message"));
			dataSource.RunCheck(check);
			Assert.AreEqual(2, annotations.NotesOS.Count,
				"Second run of check should add another error annotation.");
			Assert.IsTrue(annotations.NotesOS.Contains(origErrorAnnotation));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method updates an error annotation which originated from
		/// a different paragraph but has the same everything else. This can happen if the
		/// original error paragraph was moved to a ScrDraft after an import. (TE-8495)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_NearDuplicate_DifferOnlyByParaHvo()
		{
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(1, "My Favorite Book");

			BCVRef reference = new BCVRef(1, 2, 3);

			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(book,
				Cache.DefaultVernWs, 0, reference, reference);
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 8, "Message"));

			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);

			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(1, annotations.NotesOS.Count,
				"First run of check should add 1 error annotation");
			IScrScriptureNote origErrorAnnotation = annotations.NotesOS[0];

			IScrDraft draft = m_scr.ArchivedDraftsOC.Add(new ScrDraft());
			draft.BooksOS.Append(book);

			Assert.AreEqual(0, m_scr.ScriptureBooksOS.Count);

			check.m_ErrorsToReport.Clear();
			IScrBook newBook = m_scrInMemoryCache.AddBookToMockedScripture(1, "My Favorite Book");

			tok = new DummyParaCheckingToken(newBook, Cache.DefaultVernWs, 0, reference, reference);
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 8, "Message"));
			dataSource.RunCheck(check);
			Assert.AreEqual(1, annotations.NotesOS.Count,
				"Second run of check should add another error annotation.");
			Assert.IsTrue(annotations.NotesOS.Contains(origErrorAnnotation));
			Assert.AreEqual(newBook.Hvo, annotations.NotesOS[0].BeginObjectRAHvo);
			Assert.AreEqual(newBook.Hvo, annotations.NotesOS[0].EndObjectRAHvo);
			Assert.AreEqual(reference, annotations.NotesOS[0].BeginRef);
			Assert.AreEqual(reference, annotations.NotesOS[0].EndRef);
			Assert.AreEqual(5, annotations.NotesOS[0].BeginOffset);
			Assert.AreEqual(13, annotations.NotesOS[0].EndOffset);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method does not mistakenly detect as a duplicate an error
		/// annotation which originated from a different check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_NearDuplicate_DifferByCheck()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			ICmAnnotationDefn annDefnChkError = new CmAnnotationDefn(Cache,
				LangProject.kguidAnnCheckingError);
			CmAnnotationDefn errorCheck1 = new CmAnnotationDefn();
			annDefnChkError.SubPossibilitiesOS.Append(errorCheck1);
			errorCheck1.Guid = Guid.NewGuid();
			errorCheck1.Name.UserDefaultWritingSystem = "Type 1";

			CmAnnotationDefn errorCheck2 = new CmAnnotationDefn();
			annDefnChkError.SubPossibilitiesOS.Append(errorCheck2);
			errorCheck2.Guid = Guid.NewGuid();
			errorCheck2.Name.UserDefaultWritingSystem = "Type 2";

			DummyEditorialCheck check1 = new DummyEditorialCheck(errorCheck1.Guid);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);
			check1.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 8, "General Error"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "My Favorite Book");

			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check1);
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(1, annotations.NotesOS.Count,
				"Check 1 should add 1 error annotation");
			IScrScriptureNote origErrorAnnotation = annotations.NotesOS[0];

			DummyEditorialCheck check2 = new DummyEditorialCheck(errorCheck2.Guid);
			check2.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 8, "General Error"));

			dataSource.RunCheck(check2);
			Assert.AreEqual(2, annotations.NotesOS.Count,
				"Check 2 should add another error annotation.");
			Assert.IsTrue(annotations.NotesOS.Contains(origErrorAnnotation));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method does not mistakenly detect as a duplicate an error
		/// annotation which has a different start ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_NearDuplicate_DifferByStartRef()
		{
			BCVRef endRef = new BCVRef(1, 2, 3);

			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr,
				Cache.DefaultVernWs, 0, new BCVRef(1, 2, 3), endRef);
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 8, "Message"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "My Favorite Book");

			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(1, annotations.NotesOS.Count,
				"First run of check should add 1 error annotation");
			IScrScriptureNote origErrorAnnotation = annotations.NotesOS[0];

			tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0,
				new BCVRef(1, 2, 4), endRef);
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 8, "Message"));
			dataSource.RunCheck(check);
			Assert.AreEqual(2, annotations.NotesOS.Count,
				"Second run of check should add another error annotation.");
			Assert.IsTrue(annotations.NotesOS.Contains(origErrorAnnotation));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method does not mistakenly detect as a duplicate an error
		/// annotation which has a different start ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_NearDuplicate_DifferByEndRef()
		{
			BCVRef startRef = new BCVRef(1, 2, 3);

			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr,
				Cache.DefaultVernWs, 0, startRef, new BCVRef(1, 2, 3));
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 8, "Message"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "My Favorite Book");

			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(1, annotations.NotesOS.Count,
				"First run of check should add 1 error annotation");
			IScrScriptureNote origErrorAnnotation = annotations.NotesOS[0];

			tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0, startRef,
				new BCVRef(1, 2, 4));
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 8, "Message"));
			dataSource.RunCheck(check);
			Assert.AreEqual(2, annotations.NotesOS.Count,
				"Second run of check should add another error annotation.");
			Assert.IsTrue(annotations.NotesOS.Contains(origErrorAnnotation));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the RecordError method does not mistakenly detect as a duplicate an error
		/// annotation which cites a different bit of text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RecordError_NearDuplicate_DifferByCitedText()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			DummyEditorialCheck check = new DummyEditorialCheck(kCheckId1);
			ScrCheckingToken tok = new DummyParaCheckingToken(m_scr, Cache.DefaultVernWs, 0);
			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 0, 4, "Message"));

			m_scrInMemoryCache.AddBookToMockedScripture(tok.StartRef.Book, "My Favorite Book");

			dataSource.GetText(tok.StartRef.Book, 0);
			dataSource.RunCheck(check);
			IScrBookAnnotations annotations = m_scr.BookAnnotationsOS[tok.StartRef.Book - 1];
			Assert.AreEqual(1, annotations.NotesOS.Count,
				"First run of check should add 1 error annotation");
			IScrScriptureNote origErrorAnnotation = annotations.NotesOS[0];

			check.m_ErrorsToReport.Add(new DummyEditorialCheck.DummyError(tok, 5, 8, "Message"));
			dataSource.RunCheck(check);
			Assert.AreEqual(2, annotations.NotesOS.Count,
				"Second run of check should add another error annotation.");
			Assert.IsTrue(annotations.NotesOS.Contains(origErrorAnnotation));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that chapter 0 doesn't crash ScrCheckingTokenizer.MoveNext().
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextTokens_Chapter0()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			int iExodus = 2;
			IScrBook exodus = m_scrInMemoryCache.AddBookToMockedScripture(iExodus, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(exodus.Hvo, "Exodus");

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			StTxtPara paraSectHead0 = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Heading", ScrStyleNames.SectionHead);
			StTxtPara paraSect0Content = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect0Content, "0", ScrStyleNames.ChapterNumber);

			Assert.IsTrue(dataSource.GetText(iExodus, 0));
			IEnumerator<ITextToken> tokens = dataSource.TextTokens().GetEnumerator();
			VerifyToken(tokens, "Exodus", ScrStyleNames.MainBookTitle, string.Empty, exodus.TitleOA.ParagraphsOS[0]);

			// Skip the next token (the section head).
			tokens.MoveNext();

			// The chapter number should be the next token, and trying to get it shouldn't crash TE.
			tokens.MoveNext();
			ScrCheckingToken token = tokens.Current as ScrCheckingToken;
			Assert.AreEqual("0", token.Text);
			Assert.AreEqual(TextType.ChapterNumber, token.TextType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetText and TextTokens.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextTokens_WholeBook()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			int iExodus = 2;
			IScrBook exodus = m_scrInMemoryCache.AddBookToMockedScripture(iExodus, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(exodus.Hvo, "Exodus");

			IScrSection section = m_scrInMemoryCache.AddIntroSectionToMockedBook(exodus.Hvo);
			StTxtPara paraIntroSectHead = m_scrInMemoryCache.AddParaToMockedText(section.HeadingOAHvo,
				ScrStyleNames.IntroSectionHead);
			m_scrInMemoryCache.AddRunToMockedPara(paraIntroSectHead, "Everything you wanted to know about Exodus but were afraid to ask", null);
			StTxtPara paraIntroSectContent = m_scrInMemoryCache.AddParaToMockedText(section.ContentOAHvo,
				ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraIntroSectContent, "There's not much to say, really.", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			StTxtPara paraSectHead1 = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Head1", ScrStyleNames.SectionHead);
			StTxtPara paraSect1Content = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect1Content, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect1Content, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect1Content, "Chapter 1 Verse 1 Text", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			StTxtPara paraSectHead2 = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Head2", ScrStyleNames.SectionHead);
			StTxtPara paraSect2Content1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content1, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content1, "Chapter 2 Verse 1 Text", null);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content1, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content1, "Chapter 2 Verse 2 Text ", null);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content1, "Wow!", "Emphasis");
			StTxtPara paraSect2Content2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, "Line 1");
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content2, "Selah", ScrStyleNames.Interlude);
			StFootnote footnote = m_scrInMemoryCache.AddFootnote(exodus, paraSect2Content2, 5, "This is the text of the footnote");
			m_scrInMemoryCache.AddRunToMockedPara((StTxtPara)footnote.ParagraphsOS[0], "Favorite", Cache.DefaultUserWs);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content2, " or say, \"la\".", null);
			CmPicture pict;
			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ITsStrFactory factory = TsStrFactoryClass.Create();
				pict = new CmPicture(Cache, filemaker.Filename,
					factory.MakeString("Test picture caption", Cache.DefaultVernWs),
					StringUtils.LocalPictures);
				ITsStrBldr bldr = paraSect2Content2.Contents.UnderlyingTsString.GetBldr();
				pict.AppendPicture(Cache.DefaultVernWs, bldr);
				paraSect2Content2.Contents.UnderlyingTsString = bldr.GetString();
			}
			section.AdjustReferences();

			Assert.IsTrue(dataSource.GetText(iExodus, 0));
			IEnumerator<ITextToken> tokens = dataSource.TextTokens().GetEnumerator();
			VerifyToken(tokens, "Exodus", ScrStyleNames.MainBookTitle, string.Empty, exodus.TitleOA.ParagraphsOS[0]);
			VerifyToken(tokens, "Everything you wanted to know about Exodus but were afraid to ask", ScrStyleNames.IntroSectionHead, string.Empty, paraIntroSectHead);
			VerifyToken(tokens, "There's not much to say, really.", ScrStyleNames.IntroParagraph, string.Empty, paraIntroSectContent);
			BCVRef expectedRef = new BCVRef(iExodus, 1, 1);
			VerifyToken(tokens, "Head1", ScrStyleNames.SectionHead, string.Empty, expectedRef, true, TextType.Other, paraSectHead1);
			VerifyToken(tokens, "1", ScrStyleNames.NormalParagraph, ScrStyleNames.ChapterNumber, expectedRef, true, TextType.ChapterNumber, paraSect1Content);
			VerifyToken(tokens, "1", ScrStyleNames.NormalParagraph, ScrStyleNames.VerseNumber, expectedRef, false, TextType.VerseNumber, paraSect1Content);
			VerifyToken(tokens, "Chapter 1 Verse 1 Text", "Paragraph", string.Empty, expectedRef, false, TextType.Verse, paraSect1Content);
			expectedRef.Chapter = 2;
			VerifyToken(tokens, "Head2", ScrStyleNames.SectionHead, string.Empty, expectedRef, true, TextType.Other, paraSectHead2);
			VerifyToken(tokens, "2", ScrStyleNames.NormalParagraph, ScrStyleNames.ChapterNumber, expectedRef, true, TextType.ChapterNumber, paraSect2Content1);
			VerifyToken(tokens, "Chapter 2 Verse 1 Text", ScrStyleNames.NormalParagraph, string.Empty, expectedRef, false, TextType.Verse, paraSect2Content1);
			expectedRef.Verse = 2;
			VerifyToken(tokens, "2", ScrStyleNames.NormalParagraph, ScrStyleNames.VerseNumber, expectedRef, false, TextType.VerseNumber, paraSect2Content1);
			VerifyToken(tokens, "Chapter 2 Verse 2 Text ", ScrStyleNames.NormalParagraph, string.Empty, expectedRef, false, TextType.Verse, paraSect2Content1);
			VerifyToken(tokens, "Wow!", ScrStyleNames.NormalParagraph, ScrStyleNames.Emphasis, expectedRef, false, TextType.Verse, paraSect2Content1);
			VerifyToken(tokens, "Selah", "Line 1", ScrStyleNames.Interlude, expectedRef, true, TextType.Verse, paraSect2Content2);
			VerifyToken(tokens, "This is the text of the footnote", ScrStyleNames.NormalFootnoteParagraph, string.Empty,
				expectedRef, true, TextType.Note, footnote.ParagraphsOS[0]);
			VerifyToken(tokens, "Favorite", ScrStyleNames.NormalFootnoteParagraph, string.Empty, expectedRef,
				expectedRef, false, TextType.Note, "en", footnote.ParagraphsOS[0], (int)StTxtPara.StTxtParaTags.kflidContents);
			VerifyToken(tokens, " or say, \"la\".", "Line 1", string.Empty, expectedRef, false, TextType.Verse, paraSect2Content2);
			VerifyToken(tokens, "Test picture caption", ScrStyleNames.Figure, string.Empty, expectedRef, expectedRef, true, TextType.PictureCaption, null,
				pict, (int)CmPicture.CmPictureTags.kflidCaption);
			Assert.IsFalse(tokens.MoveNext());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetText and TextTokens for the first chapter in the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextTokens_FirstChapter()
		{
			int iExodus = 2;
			IScrBook exodus = m_scrInMemoryCache.AddBookToMockedScripture(iExodus, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(exodus.Hvo, "Exodus");

			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			IScrSection section = m_scrInMemoryCache.AddIntroSectionToMockedBook(exodus.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedText(section.HeadingOAHvo,
				ScrStyleNames.IntroSectionHead);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Everything you wanted to know about Exodus but were afraid to ask", null);
			para = m_scrInMemoryCache.AddParaToMockedText(section.ContentOAHvo,
				ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "There's not much to say, really.", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			StTxtPara paraSectHead1 = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Head1", ScrStyleNames.SectionHead);
			StTxtPara paraSect1Content = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect1Content, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect1Content, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect1Content, "Chapter 1 Verse 1 Text", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Head2", ScrStyleNames.SectionHead);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Chapter 2 Verse 1 Text", null);
			section.AdjustReferences();

			Assert.IsTrue(dataSource.GetText(iExodus, 1));
			IEnumerator<ITextToken> tokens = dataSource.TextTokens().GetEnumerator();
			BCVRef expectedRef = new BCVRef(iExodus, 1, 1);
			VerifyToken(tokens, "Head1", ScrStyleNames.SectionHead, string.Empty, expectedRef, true, TextType.Other, paraSectHead1);
			expectedRef = new BCVRef(iExodus, 1, 1);
			VerifyToken(tokens, "1", ScrStyleNames.NormalParagraph, ScrStyleNames.ChapterNumber, expectedRef, true, TextType.ChapterNumber, paraSect1Content);
			VerifyToken(tokens, "1", ScrStyleNames.NormalParagraph, ScrStyleNames.VerseNumber, expectedRef, false, TextType.VerseNumber, paraSect1Content);
			VerifyToken(tokens, "Chapter 1 Verse 1 Text", "Paragraph", string.Empty, expectedRef, false, TextType.Verse, paraSect1Content);
			Assert.IsFalse(tokens.MoveNext());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetText and TextTokens for a different writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextTokens_DifferentWritingSystem()
		{
			// Set valid characters for each writing system to a different set of three characters.
			// The set of valid characters will be 'ABC' for the first writing system, 'DEF' for
			// the second and so on.
			ILgWritingSystemFactory lgwsf = m_inMemoryCache.Cache.LanguageWritingSystemFactoryAccessor;
			int cWs = lgwsf.NumberOfWs;
			ArrayPtr rgWs = MarshalEx.ArrayToNative(cWs, typeof(int));
			lgwsf.GetWritingSystems(rgWs, cWs);
			int[] hvoWsArray = (int[])MarshalEx.NativeToArray(rgWs, cWs, typeof(int));
			List<string> validChars = new List<string>();
			int iWs = 0;
			int numValidChars = 3;
			for (iWs = 0; iWs < cWs; iWs++)
			{
				IWritingSystem ws = lgwsf.get_EngineOrNull(hvoWsArray[iWs]);
				StringBuilder strBldr = new StringBuilder();
				for (int iChar = 0; iChar < numValidChars; iChar++)
				{
					strBldr.Append(Encoding.ASCII.GetString(
						new byte[] { (byte)(65 + numValidChars * iWs + iChar) }));
					strBldr.Append(" ");
				}
				validChars.Add(strBldr.ToString());
				ws.ValidChars = validChars[iWs];
			}

			// Set up (minimal) Scripture data.
			int iExodus = 2;
			IScrBook exodus = m_scrInMemoryCache.AddBookToMockedScripture(iExodus, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(exodus.Hvo, "Exodus");

			// Get the text (and set the valid characters for each writing system).
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			dataSource.GetText(iExodus, 1);

			// Now confirm that the valid characters for the writing systems were set correctly.
			for (iWs = 0; iWs < cWs; iWs++)
			{
				IWritingSystem ws = lgwsf.get_EngineOrNull(hvoWsArray[iWs]);
				string validCharsParam = (ws.WritingSystem == Cache.DefaultVernWs) ?
					"ValidCharacters" : "ValidCharacters_" + lgwsf.GetStrFromWs(hvoWsArray[iWs]);
				Assert.AreEqual(validChars[iWs].TrimEnd(' '), dataSource.GetParameterValue(validCharsParam));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetText and TextTokens for the last chapter in the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextTokens_LastChapter()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			int iExodus = 2;
			IScrBook exodus = m_scrInMemoryCache.AddBookToMockedScripture(iExodus, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(exodus.Hvo, "Exodus");

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Head1", ScrStyleNames.SectionHead);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Chapter 1 Verse 1 Text", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			StTxtPara paraSectHead2 = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Head2", ScrStyleNames.SectionHead);
			StTxtPara paraSect2Content1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content1, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content1, "Chapter 2 Verse 1 Text", null);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content1, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content1, "Chapter 2 Verse 2 Text ", null);
			StTxtPara paraSect2Content2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, "Line 1");
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content2, "Selah", ScrStyleNames.Interlude);
			section.AdjustReferences();

			Assert.IsTrue(dataSource.GetText(iExodus, 2));
			IEnumerator<ITextToken> tokens = dataSource.TextTokens().GetEnumerator();
			BCVRef expectedRef = new BCVRef(iExodus, 2, 1);
			VerifyToken(tokens, "Head2", ScrStyleNames.SectionHead, string.Empty, expectedRef, true, TextType.Other, paraSectHead2);
			expectedRef.Chapter = 2;
			VerifyToken(tokens, "2", ScrStyleNames.NormalParagraph, ScrStyleNames.ChapterNumber, expectedRef, true, TextType.ChapterNumber, paraSect2Content1);
			VerifyToken(tokens, "Chapter 2 Verse 1 Text", ScrStyleNames.NormalParagraph, string.Empty, expectedRef, false, TextType.Verse, paraSect2Content1);
			expectedRef.Verse = 2;
			VerifyToken(tokens, "2", ScrStyleNames.NormalParagraph, ScrStyleNames.VerseNumber, expectedRef, false, TextType.VerseNumber, paraSect2Content1);
			VerifyToken(tokens, "Chapter 2 Verse 2 Text ", ScrStyleNames.NormalParagraph, string.Empty, expectedRef, false, TextType.Verse, paraSect2Content1);
			VerifyToken(tokens, "Selah", "Line 1", ScrStyleNames.Interlude, expectedRef, true, TextType.Verse, paraSect2Content2);
			Assert.IsFalse(tokens.MoveNext());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetText and TextTokens for a particular chapter when the chapter starts and
		/// in the middle of different scripture sections.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextTokens_ChapterStartsAndEndsMidSection()
		{

			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			int iExodus = 2;
			IScrBook exodus = m_scrInMemoryCache.AddBookToMockedScripture(iExodus, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(exodus.Hvo, "Exodus");

			// Section 1 starts with Chapter 1
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Head1", ScrStyleNames.SectionHead);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Chapter 1 Verse 1 Text", null);
			section.AdjustReferences();

			// Section 2 starts with Chapter 1, but also contains the start of Chapter 2
			section = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Head2", ScrStyleNames.SectionHead);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Chapter 1 Verse 2 Text", null);
			StTxtPara paraSect2Content2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content2, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content2, "Chapter 1 Verse 3 Text ", null);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content2, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content2, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect2Content2, "Chapter 2 Verse 1 Text", null);
			section.AdjustReferences();

			// Section 3 starts with Chapter 2, but also contains the start of Chapter 3
			section = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			StTxtPara paraSectHead3 = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Head3", ScrStyleNames.SectionHead);
			StTxtPara paraSect3Content = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect3Content, "More Chapter 2 Verse 1 Text ", null);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect3Content, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect3Content, "Chapter 2 Verse 2 Text ", null);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect3Content, "3", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraSect3Content, "Chapter 3 Verse 1 Text", null);
			section.AdjustReferences();

			Assert.IsTrue(dataSource.GetText(iExodus, 2));
			IEnumerator<ITextToken> tokens = dataSource.TextTokens().GetEnumerator();
			BCVRef expectedRef = new BCVRef(iExodus, 2, 1);
			VerifyToken(tokens, "2", ScrStyleNames.NormalParagraph, ScrStyleNames.ChapterNumber, expectedRef, false, TextType.ChapterNumber, paraSect2Content2);
			VerifyToken(tokens, "1", ScrStyleNames.NormalParagraph, ScrStyleNames.VerseNumber, expectedRef, false, TextType.VerseNumber, paraSect2Content2);
			VerifyToken(tokens, "Chapter 2 Verse 1 Text", ScrStyleNames.NormalParagraph, string.Empty, expectedRef, false, TextType.Verse, paraSect2Content2);
			VerifyToken(tokens, "Head3", ScrStyleNames.SectionHead, string.Empty, expectedRef, true, TextType.Other, paraSectHead3);
			VerifyToken(tokens, "More Chapter 2 Verse 1 Text ", ScrStyleNames.NormalParagraph, string.Empty, expectedRef, true, TextType.Verse, paraSect3Content);
			expectedRef.Verse = 2;
			VerifyToken(tokens, "2", ScrStyleNames.NormalParagraph, ScrStyleNames.VerseNumber, expectedRef, false, TextType.VerseNumber, paraSect3Content);
			VerifyToken(tokens, "Chapter 2 Verse 2 Text ", ScrStyleNames.NormalParagraph, string.Empty, expectedRef, false, TextType.Verse, paraSect3Content);
			Assert.IsFalse(tokens.MoveNext());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetText and TextTokens when a paragraph is empty and has no writing system
		/// set for its 0-length run. Jira number is TE-6169
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTextTokens_EmptyTsStringWithMissingWs()
		{
			ScrChecksDataSource dataSource = new ScrChecksDataSource(Cache);
			int iExodus = 2;
			IScrBook exodus = m_scrInMemoryCache.AddBookToMockedScripture(iExodus, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(exodus.Hvo, "Exodus");
			IScrSection section = m_scrInMemoryCache.AddIntroSectionToMockedBook(exodus.Hvo);

			// Make the heading paragraph empty, and with no writing system set
			CacheBase cachebase = m_inMemoryCache.CacheAccessor;
			StTxtPara paraIntroSectHead = new StTxtPara(Cache, cachebase.NewHvo(StTxtPara.kClassId));
			cachebase.AppendToFdoVector(section.HeadingOAHvo,
				(int)StText.StTextTags.kflidParagraphs, paraIntroSectHead.Hvo);
			cachebase.SetBasicProps(paraIntroSectHead.Hvo, section.HeadingOAHvo, (int)StTxtPara.kClassId,
				(int)StText.StTextTags.kflidParagraphs, 1);
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			paraIntroSectHead.StyleRules = propFact.MakeProps(ScrStyleNames.IntroSectionHead, 0, 0);
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "", null);
			paraIntroSectHead.Contents.UnderlyingTsString = strBldr.GetString();
			cachebase.SetGuid(paraIntroSectHead.Hvo, (int)CmObjectFields.kflidCmObject_Guid, Guid.NewGuid());

			// Set up the intro section contents
			StTxtPara paraIntroSectContent = m_scrInMemoryCache.AddParaToMockedText(section.ContentOAHvo,
				ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraIntroSectContent, "There's not much to say, really.", null);
			section.AdjustReferences();

			Assert.IsTrue(dataSource.GetText(iExodus, 0));
			IEnumerator<ITextToken> tokens = dataSource.TextTokens().GetEnumerator();
			VerifyToken(tokens, "Exodus", ScrStyleNames.MainBookTitle, string.Empty, exodus.TitleOA.ParagraphsOS[0]);
			BCVRef expectedRef = new BCVRef(iExodus, 1, 0);
			VerifyToken(tokens, string.Empty, ScrStyleNames.IntroSectionHead, string.Empty, expectedRef, expectedRef,
				true, TextType.Other, null, paraIntroSectHead, (int)StTxtPara.StTxtParaTags.kflidContents);
			VerifyToken(tokens, "There's not much to say, really.", ScrStyleNames.IntroParagraph, string.Empty, paraIntroSectContent);
			Assert.IsFalse(tokens.MoveNext());
		}

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
			StTxtPara para = (StTxtPara)text.ParagraphsOS[0];
			ITsString tss = para.Contents.UnderlyingTsString;
			Assert.IsNotNull(tss);
			AssertEx.RunIsCorrect(tss, 0, String.Empty, null, text.Cache.DefaultAnalWs);

		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the next token in the collection of tokens.
		/// </summary>
		/// <param name="tokens">The tokens.</param>
		/// <param name="text">The expected text of the token.</param>
		/// <param name="paraStyleName">The expected PARAGRAPH style name of the token.</param>
		/// <param name="charStyleName">The expected CHARACTER style name of the token.</param>
		/// <param name="obj">The object that the token refers to.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyToken(IEnumerator<ITextToken> tokens, string text, string paraStyleName,
			string charStyleName, ICmObject obj)
		{
			VerifyToken(tokens, text, paraStyleName, charStyleName, 02001000, true, TextType.Other, obj);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the next token in the collection of tokens.
		/// </summary>
		/// <param name="tokens">The tokens.</param>
		/// <param name="text">The expected text of the token.</param>
		/// <param name="paraStyleName">The expected PARAGRAPH style name of the token.</param>
		/// <param name="charStyleName">The expected CHARACTER style name of the token.</param>
		/// <param name="scrRef">The expected reference.</param>
		/// <param name="paragraphStart"><c>true</c> if this token is expected to be the first
		/// one in a paragraph.</param>
		/// <param name="textType">The expected type of text in the token</param>
		/// <param name="obj">The object that the token refers to.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyToken(IEnumerator<ITextToken> tokens, string text,
			string paraStyleName, string charStyleName, BCVRef scrRef, bool paragraphStart,
			TextType textType, ICmObject obj)
		{
			VerifyToken(tokens, text, paraStyleName, charStyleName, scrRef, scrRef, paragraphStart,
				textType, null, obj, (int)StTxtPara.StTxtParaTags.kflidContents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the next token in the collection of tokens.
		/// </summary>
		/// <param name="tokens">The tokens.</param>
		/// <param name="text">The expected text of the token.</param>
		/// <param name="paraStyleName">The expected PARAGRAPH style name of the token.</param>
		/// <param name="charStyleName">The expected CHARACTER style name of the token.</param>
		/// <param name="startRef">The starting reference.</param>
		/// <param name="endRef">The ending reference.</param>
		/// <param name="paragraphStart"><c>true</c> if this token is expected to be the first
		/// one in a paragraph.</param>
		/// <param name="textType">The expected type of text in the token</param>
		/// <param name="icuLocale">The ICU locale.</param>
		/// <param name="obj">The object that the token refers to.</param>
		/// <param name="flid">The flid</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyToken(IEnumerator<ITextToken> tokens, string text,
			string paraStyleName, string charStyleName, BCVRef startRef, BCVRef endRef,
			bool paragraphStart, TextType textType, string icuLocale, ICmObject obj, int flid)
		{
			tokens.MoveNext();
			ScrCheckingToken token = tokens.Current as ScrCheckingToken;
			Assert.IsNotNull(text);
			Assert.AreEqual(text, token.Text);
			Assert.AreEqual(startRef, token.StartRef);
			Assert.AreEqual(endRef, token.EndRef);
			Assert.AreEqual(paragraphStart && textType == TextType.Note, token.IsNoteStart); // Don't currently allow multi-para footnotes
			Assert.AreEqual(paragraphStart, token.IsParagraphStart);
			Assert.AreEqual(textType, token.TextType);
			Assert.AreEqual(paraStyleName, token.ParaStyleName);
			Assert.AreEqual(charStyleName, token.CharStyleName);
			Assert.AreEqual(icuLocale, token.Locale);
			Assert.AreEqual(obj, token.Object);
			Assert.AreEqual(flid, token.Flid);
		}
		#endregion

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
			m_scr.ParsePictureLoc("MRK 1--2", 41002003, ref locRangeType, out locationMin,
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
			m_scr.ParsePictureLoc("MRK 1:8-2:15", 41002003, ref locRangeType, out locationMin, out locationMax);
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
			m_scr.ParsePictureLoc("MRK 1:8-MRK 2:15", 41002003, ref locRangeType, out locationMin, out locationMax);
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
			m_scr.ParsePictureLoc("1:8-2:15", 41002003, ref locRangeType, out locationMin, out locationMax);
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
			m_scr.ParsePictureLoc("2-15", 41002003, ref locRangeType, out locationMin, out locationMax);
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
			m_scr.ParsePictureLoc("MRK 2:3", 41002003, ref locRangeType, out locationMin, out locationMax);
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
			m_scr.ParsePictureLoc("MRK 1:4", 41002003, ref locRangeType, out locationMin, out locationMax);
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
			m_scr.ParsePictureLoc("MRK 1:4-MRK 2:1", 41002003, ref locRangeType, out locationMin, out locationMax);
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
			m_scr.ParsePictureLoc("XYZ 3:4&6:7", 41002003, ref locRangeType, out locationMin, out locationMax);
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
			m_scr.ParsePictureLoc("MAT 1:5-REV 12:2", 41002003, ref locRangeType, out locationMin, out locationMax);
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
			m_scr.ParsePictureLoc("MRK 2:5-MRK 1:2", 41002003, ref locRangeType, out locationMin, out locationMax);
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
			m_scr.ParsePictureLoc("2-6", 41001000, ref locRangeType, out locationMin, out locationMax);
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
			m_scr.ParsePictureLoc("MRK 1:0 - MRK 2:1", 41001000, ref locRangeType, out locationMin, out locationMax);
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
			m_scr.ParsePictureLoc("41001002-41001016", 41001007, ref locRangeType, out locationMin, out locationMax);
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
			m_scr.ParsePictureLoc("41001002-41001006", 41001007, ref locRangeType, out locationMin, out locationMax);
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
			m_scr.ParsePictureLoc("41001002-41001006", 42008005, ref locRangeType, out locationMin, out locationMax);
			Assert.AreEqual(PictureLocationRangeType.AfterAnchor, locRangeType);
		}
		#endregion

		#region Other tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability of DetermineFootnoteMarkerType to return the correct footnote marker
		/// type for non cross-ref footnotes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineFootnoteMarkerType_Normal()
		{
			CheckDisposed();

			Scripture scr = (m_scr as Scripture);
			scr.FootnoteMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker;
			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker,
				scr.DetermineFootnoteMarkerType(ScrStyleNames.NormalFootnoteParagraph));
			Assert.AreEqual(FootnoteMarkerTypes.AutoFootnoteMarker,
				scr.DetermineFootnoteMarkerType("Garbage"));
			scr.FootnoteMarkerType = FootnoteMarkerTypes.NoFootnoteMarker;
			Assert.AreEqual(FootnoteMarkerTypes.NoFootnoteMarker,
				scr.DetermineFootnoteMarkerType(ScrStyleNames.NormalFootnoteParagraph));
			scr.FootnoteMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;
			Assert.AreEqual(FootnoteMarkerTypes.SymbolicFootnoteMarker,
				scr.DetermineFootnoteMarkerType(ScrStyleNames.NormalFootnoteParagraph));
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
			CheckDisposed();

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
		/// Allows subclasses to do other stuff to initialize the cache before it gets used
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeCache()
		{
			base.InitializeCache();
			Cache.MapType(typeof(StFootnote), typeof(ScrFootnote));
			Cache.MapType(typeof(StTxtPara), typeof(ScrTxtPara));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates test data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_stylesheet = new FwStyleSheet();

			m_scrInMemoryCache.AddScrStyle("User added title style", ContextValues.Title, StructureValues.Undefined, FunctionValues.Prose, false);
			m_scrInMemoryCache.AddScrStyle("Poetry", ContextValues.Text, StructureValues.Body, FunctionValues.Prose, false);
			m_scrInMemoryCache.AddScrStyle("Parallel Passage", ContextValues.Text, StructureValues.Heading, FunctionValues.Prose, false);
			m_scrInMemoryCache.AddScrStyle("Line 1", ContextValues.Text, StructureValues.Body, FunctionValues.Prose, false);

			m_stylesheet.Init(Cache, m_scr.Hvo, (int)Scripture.ScriptureTags.kflidStyles);

			m_scrInMemoryCache.AddBookWithTwoSections(40, "Matthew");
			m_philemon = m_scrInMemoryCache.AddBookWithTwoSections(57, "Philemon");
			m_scrInMemoryCache.AddBookWithTwoSections(66, "Revelation");

			m_section1 = (ScrSection)m_philemon.SectionsOS[0];
			m_scrInMemoryCache.AddSectionHeadParaToSection(m_section1.Hvo, "Matt. 4:5-9; Luke 2:6-10", "Parallel Passage");
			StTxtPara para = (StTxtPara)m_section1.ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " One is the best verse. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " I like two better. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " No, three rocks! ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4-8", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " Nobody can touch four through eight. ", null);
			m_section1.AdjustReferences();

			m_section2 = (ScrSection)m_philemon.SectionsOS[1];
			para = (StTxtPara)m_section2.ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para, "10", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " Ten is the best verse. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "11", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " I like eleven better. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "12", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " No, twelve rocks! ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "13", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " Nobody can touch thirteen. ", null);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section2.Hvo, "Line 1");
			m_scrInMemoryCache.AddRunToMockedPara(para, "Second Stanza.", null);
			m_section2.AdjustReferences();

			m_section3 = m_scrInMemoryCache.AddSectionToMockedBook(m_philemon.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(m_section3.Hvo, "Heading for section 3",
				ScrStyleNames.SectionHead);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section3.Hvo, "Poetry");
			m_scrInMemoryCache.AddRunToMockedPara(para, "24", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " Hi, I'm verse 24.", null);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section3.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "25", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " Welcome to the twenty-fifth verse. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "26", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " Welcome to the twenty-sixth verse.", null);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section3.Hvo, "Line 1");
			m_scrInMemoryCache.AddRunToMockedPara(para, "27", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, " This is the end.", null);
			m_section3.AdjustReferences();
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
			CheckDisposed();

			int iSection = -1; // should be ignored
			int hvoTarget = m_section3.HeadingOA.ParagraphsOS.HvoArray[0];
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle(ScrStyleNames.SectionHead), new BCVRef(57, 1, 24), 0,
				ref iSection);
			Assert.AreEqual(hvoTarget, para.Hvo);
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
			CheckDisposed();

			int iSection = 0;
			int hvoTarget = m_section1.HeadingOA.ParagraphsOS.HvoArray[0];
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle(ScrStyleNames.SectionHead),
				new BCVRef(57, 1, 0), 0, ref iSection);
			Assert.AreEqual(hvoTarget, para.Hvo);
			Assert.AreEqual(0, iSection);

			hvoTarget = m_section1.HeadingOA.ParagraphsOS.HvoArray[1];
			para = m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle("Parallel Passage"), new BCVRef(57, 1, 0), 1,
				ref iSection);
			Assert.AreEqual(hvoTarget, para.Hvo);
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
			CheckDisposed();

			int iSection = -1; // should be ignored
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle(ScrStyleNames.SectionHead),
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
			CheckDisposed();

			int iSection = -1; // should be ignored
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle("Parallel Passage"),
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
			CheckDisposed();

			int iSection = 0;
			int hvoTarget = m_section1.ContentOA.ParagraphsOS.HvoArray[0];
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle(ScrStyleNames.NormalParagraph),
				new BCVRef(57, 1, 0), 0, ref iSection);
			Assert.AreEqual(hvoTarget, para.Hvo);
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
			CheckDisposed();

			int iSection = -1; // should be ignored
			int hvoTarget = m_section3.ContentOA.ParagraphsOS.HvoArray[1];
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle(ScrStyleNames.NormalParagraph),
				new BCVRef(57, 1, 25), 0, ref iSection);
			Assert.AreEqual(hvoTarget, para.Hvo);
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
			CheckDisposed();

			int iSection = 1;
			int hvoTarget = m_section2.ContentOA.ParagraphsOS.HvoArray[m_section2.ContentOA.ParagraphsOS.Count - 1];
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle("Line 1"), new BCVRef(57, 1, 13), 1, ref iSection);
			Assert.AreEqual(hvoTarget, para.Hvo);
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
			CheckDisposed();

			int iSection = -1;  // should be ignored
			int hvoTarget = m_section3.ContentOA.ParagraphsOS.HvoArray[0];
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle("Poetry"), new BCVRef(57, 1, 24),
				0, ref iSection);
			Assert.AreEqual(hvoTarget, para.Hvo);
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
			CheckDisposed();

			int iSection = -1;  // should be ignored
			int hvoTarget = m_section1.ContentOA.ParagraphsOS.HvoArray[0];
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle(ScrStyleNames.NormalParagraph),
				new BCVRef(57, 1, 7), 0, ref iSection);
			Assert.AreEqual(hvoTarget, para.Hvo);
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
			CheckDisposed();

			int iSection = -1;  // should be ignored
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle(ScrStyleNames.NormalParagraph),
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
			CheckDisposed();

			int iSection = -1;  // should be ignored
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle("Poetry"), new BCVRef(57, 1, 12), 0, ref iSection);
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
			CheckDisposed();

			int iSection = -1;  // should be ignored
			int hvoTarget = m_philemon.TitleOA.ParagraphsOS.HvoArray[0];
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle(ScrStyleNames.MainBookTitle),
				new BCVRef(57, 0, 0), 0, ref iSection);
			Assert.AreEqual(hvoTarget, para.Hvo);
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
			CheckDisposed();

			StTxtPara para = m_scrInMemoryCache.AddParaToMockedText(m_philemon.TitleOAHvo,
				ScrStyleNames.MainBookTitle);
			m_scrInMemoryCache.AddRunToMockedPara(para, "Second title para", null);

			int iSection = -1;  // should be ignored
			int hvoTarget = m_philemon.TitleOA.ParagraphsOS.HvoArray[1];
			para = (StTxtPara)m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle(ScrStyleNames.MainBookTitle),
				new BCVRef(57, 0, 0), 1, ref iSection);
			Assert.AreEqual(hvoTarget, para.Hvo);
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
			CheckDisposed();

			int iSection = -1;  // should be ignored
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle(ScrStyleNames.MainBookTitle),
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
			CheckDisposed();

			int iSection = -1;  // should be ignored
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle(ScrStyleNames.MainBookTitle),
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
			CheckDisposed();

			StTxtPara para = m_scrInMemoryCache.AddParaToMockedText(m_philemon.TitleOAHvo,
				"User added title style");
			m_scrInMemoryCache.AddRunToMockedPara(para, "Second title para", null);

			int hvoTarget = m_philemon.TitleOA.ParagraphsOS.HvoArray[1];
			int iSection = -1;  // should be ignored
			para = (StTxtPara)m_scr.FindCorrespondingVernParaForSegment(
				m_stylesheet.FindStyle("User added title style"),
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
			CheckDisposed();

			int iSection = -1; // should be ignored
			int hvoTarget = m_section3.ContentOA.ParagraphsOS.HvoArray[0];
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				null, new BCVRef(57, 1, 24), 0, ref iSection);
			Assert.AreEqual(hvoTarget, para.Hvo);
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
			CheckDisposed();

			int iSection = -1; // should be ignored
			IStTxtPara para = m_scr.FindCorrespondingVernParaForSegment(
				null, new BCVRef(58, 1, 24), 0, ref iSection);
			Assert.IsNull(para);
		}
	}
	#endregion

	#region class FdoScriptureTests with real database - DON'T ADD TO THIS UNLESS NECESSARY!
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for FdoScripture with real database. Use only for tests that require a connection
	/// to the database, for example: testing methods that call stored procedures.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FdoScriptureTestsWithRealDb_DONTADDTOTHIS : InDatabaseFdoTestBase
	{
		/// <summary></summary>
		protected IScripture m_scr;

		#region Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		public override void Initialize()
		{
			base.Initialize();
			m_scr = m_fdoCache.LangProject.TranslatedScriptureOA;
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a mindless footnote (i.e., it's marker, paragraph style, etc. won't be set).
		/// </summary>
		/// <param name="book">Book to insert footnote into</param>
		/// <param name="para">Paragraph to insert footnote into</param>
		/// <param name="iFootnotePos">The 0-based index of the new footnote in the collection
		/// of footnotes owned by the book</param>
		/// <param name="ichPos">The 0-based character offset into the paragraph</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected StFootnote InsertTestFootnote(IScrBook book, IStTxtPara para,
			int iFootnotePos, int ichPos)
		{
			// Create the footnote
			StFootnote footnote = new StFootnote();
			book.FootnotesOS.InsertAt(footnote, iFootnotePos);

			// Update the paragraph contents to include the footnote marker
			ITsStrBldr tsStrBldr = para.Contents.UnderlyingTsString.GetBldr();
			footnote.InsertOwningORCIntoPara(tsStrBldr, ichPos, 0); // Don't care about ws
			para.Contents.UnderlyingTsString = tsStrBldr.GetString();

			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure footnote exists and is referred to properly in the paragraph contents
		/// </summary>
		/// <param name="footnote">The footnote to verify</param>
		/// <param name="para">The paragraph that is expected to contain the footnote ORC</param>
		/// <param name="ich">Character position where ORC should be</param>
		/// ------------------------------------------------------------------------------------
		protected void VerifyFootnote(IStFootnote footnote, IStTxtPara para, int ich)
		{
			StTxtParaTests.VerifyFootnote(footnote, para, ich);
		}
		#endregion

		#region Archive draft tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests archiving a draft with one book having 3 footnotes (including 1 in the title)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ArchiveDraft_TestFootnoteOrder()
		{
			CheckDisposed();
			int draftCount = m_scr.ArchivedDraftsOC.Count;
			ScrBook james = (ScrBook)m_scr.ScriptureBooksOS[1];
			int hvoBookRef = m_fdoCache.ScriptureReferenceSystem.BooksOS[58].Hvo;

			// Add a footnote to the first content para2 of the second section.
			FdoOwningSequence<IStPara> contentParas = james.SectionsOS[1].ContentOA.ParagraphsOS;
			StTxtPara para2 = (StTxtPara)contentParas[0];
			StFootnote footnoteOrig3 = InsertTestFootnote(james, para2, 0, 0);
			StTxtPara para1 = new StTxtPara();
			contentParas.InsertAt(para1, 0);
			StFootnote footnoteOrig2 = InsertTestFootnote(james, para1, 0, 0);
			StText titleText = new StText();
			StTxtPara title = new StTxtPara();
			james.TitleOA = titleText;
			titleText.ParagraphsOS.Append(title);
			StFootnote footnoteOrig1 = InsertTestFootnote(james, title, 0, 0);

			// archive draft
			IScrDraft draft = m_scr.CreateSavedVersion("FootnoteOrder james", new int[] { james.Hvo });

			Assert.AreEqual(draftCount + 1, m_scr.ArchivedDraftsOC.Count);
			Assert.AreEqual("FootnoteOrder james", draft.Description);
			Assert.AreEqual(1, draft.BooksOS.Count);
			IScrBook revision = draft.BooksOS.FirstItem;
			Assert.IsFalse(james.Hvo == revision.Hvo);
			Assert.AreEqual(james.SectionsOS.Count, revision.SectionsOS.Count);
			IScrSection revSection1 = revision.SectionsOS[0];
			IScrSection revSection2 = revision.SectionsOS[1];
			Assert.IsTrue(james.SectionsOS[0].Hvo != revSection1.Hvo);
			Assert.IsTrue(james.SectionsOS[0].Hvo != revSection2.Hvo);
			Assert.IsTrue(james.SectionsOS[1].Hvo != revSection1.Hvo);
			Assert.IsTrue(james.SectionsOS[1].Hvo != revSection2.Hvo);
			FdoOwningSequence<IStPara> s2Paras = revSection2.ContentOA.ParagraphsOS;
			Assert.AreEqual(james.SectionsOS[1].ContentOA.ParagraphsOS.Count,
				s2Paras.Count);

			StTxtPara titleRev = (StTxtPara)revision.TitleOA.ParagraphsOS[0];
			StTxtPara paraRev1 = (StTxtPara)s2Paras[0];
			StTxtPara paraRev2 = (StTxtPara)s2Paras[1];
			Assert.IsFalse(title.Hvo == titleRev.Hvo);
			Assert.IsFalse(para1.Hvo == paraRev1.Hvo);
			Assert.IsFalse(para2.Hvo == paraRev2.Hvo);

			Assert.AreEqual(hvoBookRef, revision.BookIdRAHvo);

			// Check the footnote
			Assert.AreEqual(james.FootnotesOS.Count, revision.FootnotesOS.Count);
			Assert.AreEqual(footnoteOrig1.Hvo, james.FootnotesOS[0].Hvo);
			Assert.AreEqual(footnoteOrig2.Hvo, james.FootnotesOS[1].Hvo);
			Assert.AreEqual(footnoteOrig3.Hvo, james.FootnotesOS[2].Hvo);
			IStFootnote footnoteRev1 = revision.FootnotesOS[0];
			IStFootnote footnoteRev2 = revision.FootnotesOS[1];
			IStFootnote footnoteRev3 = revision.FootnotesOS[2];
			Assert.IsTrue(footnoteRev1.Hvo != footnoteOrig1.Hvo);
			Assert.IsTrue(footnoteRev2.Hvo != footnoteOrig2.Hvo);
			Assert.IsTrue(footnoteRev3.Hvo != footnoteOrig3.Hvo);

			VerifyFootnote(footnoteRev1, titleRev, 0);
			VerifyFootnote(footnoteRev2, paraRev1, 0);
			VerifyFootnote(footnoteRev3, paraRev2, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests archiving a draft with one book having 3 footnotes (including 1 in the title)
		/// in the vernacular and a back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ArchiveDraft_TestBtFootnotes()
		{
			CheckDisposed();
			int draftCount = m_scr.ArchivedDraftsOC.Count;
			IScrBook philemon = m_scr.ScriptureBooksOS[0];
			int hvoBookRef = m_fdoCache.ScriptureReferenceSystem.BooksOS[56].Hvo;

			// Add footnote to title.
			StTxtPara title = (StTxtPara)philemon.TitleOA.ParagraphsOS[0];
			StFootnote footnoteOrig1 = InsertTestFootnote(philemon, title, 0, 0);

			// Add footnote in the section head of the second intro section
			StTxtPara paraHeading = (StTxtPara)philemon.SectionsOS[1].HeadingOA.ParagraphsOS[0];
			StFootnote footnoteOrig2 = InsertTestFootnote(philemon, paraHeading, 1, 0);

			// Add footnotes in the first two paras of the first content section
			// (after three intro sections).
			FdoOwningSequence<IStPara> contentParas = philemon.SectionsOS[3].ContentOA.ParagraphsOS;
			StTxtPara para1 = (StTxtPara)contentParas[0];
			StFootnote footnoteOrig3 = InsertTestFootnote(philemon, para1, 2, 0);
			StTxtPara para2 = (StTxtPara)contentParas[1];
			StFootnote footnoteOrig4 = InsertTestFootnote(philemon, para2, 3, 0);
			StFootnote footnoteOrig5 = InsertTestFootnote(philemon, para2, 4, para2.Contents.Length);

			// Add text and footnotes to back translation
			int ws = m_fdoCache.DefaultAnalWs;
			AppendRunToBt(title, ws, "Back translation of title");
			InsertTestBtFootnote(footnoteOrig1, title, ws, 25);
			AppendRunToBt(paraHeading, ws, "Back translation of section head");
			InsertTestBtFootnote(footnoteOrig2, paraHeading, ws, 0);
			AppendRunToBt(para1, ws, "Back translation of para1");
			InsertTestBtFootnote(footnoteOrig3, para1, ws, 4);
			AppendRunToBt(para2, ws, "Back translation of para2");
			InsertTestBtFootnote(footnoteOrig4, para2, ws, 0);
			InsertTestBtFootnote(footnoteOrig5, para2, ws, 25);

			// Call the method under test: create an archive draft
			IScrDraft draft = m_scr.CreateSavedVersion("BT Footnotes in Philemon", new int[] { philemon.Hvo });

			// In the archive, confirm that the ref ORCs (footnotes in BT) refer to their
			// corresponding vernacular footnote (have the same guid) and that neither the owned or
			// ref ORCs in the archive refer to the original footnotes.
			ScrBook revPhilemon = (ScrBook)draft.BooksOS[0];
			FdoOwningSequence<IStFootnote> revFootnotes = revPhilemon.FootnotesOS;
			StTxtPara revTitle = (StTxtPara)revPhilemon.TitleOA.ParagraphsOS[0];
			IScrSection revSection1 = revPhilemon.SectionsOS[1];
			StTxtPara revSectionHead = (StTxtPara)revSection1.HeadingOA.ParagraphsOS[0];
			IScrSection revSection3 = revPhilemon.SectionsOS[3];
			StTxtPara revPara1 = (StTxtPara)revSection3.ContentOA.ParagraphsOS[0];
			StTxtPara revPara2 = (StTxtPara)revSection3.ContentOA.ParagraphsOS[1];

			// Verify that the archived footnote Guids are different than the original footnotes.
			Assert.IsTrue(footnoteOrig1.Guid != revFootnotes[0].Guid);
			Assert.IsTrue(footnoteOrig2.Guid != revFootnotes[1].Guid);
			Assert.IsTrue(footnoteOrig3.Guid != revFootnotes[2].Guid);
			Assert.IsTrue(footnoteOrig4.Guid != revFootnotes[3].Guid);
			Assert.IsTrue(footnoteOrig5.Guid != revFootnotes[4].Guid);

			// Verify that the owned ORCs in the archive vern refer to the correct footnote.
			VerifyFootnote(revFootnotes[0], revTitle, 0);
			VerifyFootnote(revFootnotes[1], revSectionHead, 0);
			VerifyFootnote(revFootnotes[2], revPara1, 0);
			VerifyFootnote(revFootnotes[3], revPara2, 0);
			VerifyFootnote(revFootnotes[4], revPara2, revPara2.Contents.Length - 1);

			// Verify that the ref ORCs in the BT refer to their corresponding footnote.
			StTxtParaTests.VerifyBtFootnote(revFootnotes[0], revTitle, ws, 25);
			StTxtParaTests.VerifyBtFootnote(revFootnotes[1], revSectionHead, ws, 0);
			StTxtParaTests.VerifyBtFootnote(revFootnotes[2], revPara1, ws, 4);
			StTxtParaTests.VerifyBtFootnote(revFootnotes[3], revPara2, ws, 0);
			StTxtParaTests.VerifyBtFootnote(revFootnotes[4], revPara2, ws, 25);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a run of text to a back translation of a paragraph.
		/// </summary>
		/// <param name="para">given paragraph</param>
		/// <param name="ws">given writing system for the back translation</param>
		/// <param name="runText">given text to append to back translation</param>
		/// ------------------------------------------------------------------------------------
		private void AppendRunToBt(StTxtPara para, int ws, string runText)
		{
			ICmTranslation trans = para.GetOrCreateBT();
			ITsStrBldr bldr = trans.Translation.GetAlternative(ws).UnderlyingTsString.GetBldr();
			ITsTextProps ttp = StyleUtils.CharStyleTextProps(null, ws);
			int bldrLength = bldr.Length;
			bldr.ReplaceRgch(bldrLength, bldrLength, runText, runText.Length, ttp);
			trans.Translation.SetAlternative(bldr.GetString(), ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests archiving a draft with multiple book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ArchiveDraftWithMultipleBooks()
		{
			CheckDisposed();
			int draftCount = m_scr.ArchivedDraftsOC.Count;
			ScrBook james = (ScrBook)m_scr.ScriptureBooksOS[1];
			int hvoRefJames = m_fdoCache.ScriptureReferenceSystem.BooksOS[58].Hvo;
			ScrBook jude = (ScrBook)m_scr.ScriptureBooksOS[2];
			int hvoRefJude = m_fdoCache.ScriptureReferenceSystem.BooksOS[59].Hvo;

			// archive draft
			IScrDraft draft = m_scr.CreateSavedVersion("Multiple books",
				new int[] { james.Hvo, jude.Hvo });

			Assert.AreEqual(draftCount + 1, m_scr.ArchivedDraftsOC.Count);
			Assert.AreEqual("Multiple books", draft.Description);
			Assert.AreEqual(2, draft.BooksOS.Count);
			ScrBook revision = (ScrBook)draft.BooksOS.FirstItem;
			Assert.IsFalse(james.Hvo == revision.Hvo);
			Assert.AreEqual(james.SectionsOS.Count, revision.SectionsOS.Count);
			Assert.IsTrue(james.SectionsOS[0].Hvo != ((ScrSection)revision.SectionsOS[0]).Hvo &&
				james.SectionsOS[0].Hvo != ((ScrSection)revision.SectionsOS[1]).Hvo);
			Assert.IsTrue(james.SectionsOS[1].Hvo != ((ScrSection)revision.SectionsOS[1]).Hvo &&
				james.SectionsOS[1].Hvo != ((ScrSection)revision.SectionsOS[1]).Hvo);
			Assert.AreEqual(((ScrSection)james.SectionsOS[1]).ContentOA.ParagraphsOS.Count,
				((ScrSection)revision.SectionsOS[1]).ContentOA.ParagraphsOS.Count);
			Assert.IsFalse(((ScrSection)james.SectionsOS[1]).ContentOA.ParagraphsOS[0].Hvo
				== ((ScrSection)revision.SectionsOS[1]).ContentOA.ParagraphsOS[0].Hvo);
			Assert.AreEqual(hvoRefJames, revision.BookIdRAHvo);

			revision = (ScrBook)draft.BooksOS[1];
			Assert.IsFalse(jude.Hvo == revision.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests archiving a draft with one book that has a picture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ArchiveDraftWithOneBookAndAPicture()
		{
			CheckDisposed();
			int draftCount = m_scr.ArchivedDraftsOC.Count;
			ScrBook james = (ScrBook)m_scr.ScriptureBooksOS[1];
			int hvoBookRef = m_fdoCache.ScriptureReferenceSystem.BooksOS[58].Hvo;

			// Add a footnote to the first content para of the second section.

			StTxtPara para = (StTxtPara)((ScrSection)james.SectionsOS[1]).ContentOA.ParagraphsOS[0];
			ICmPicture picture = InsertTestPicture(para, 0);

			// archive draft
			IScrDraft draft = m_scr.CreateSavedVersion("Single james", new int[] { james.Hvo });

			Assert.AreEqual(draftCount + 1, m_scr.ArchivedDraftsOC.Count);
			Assert.AreEqual("Single james", draft.Description);
			Assert.AreEqual(1, draft.BooksOS.Count);
			ScrBook revision = (ScrBook)draft.BooksOS.FirstItem;
			Assert.IsFalse(james.Hvo == revision.Hvo);
			Assert.AreEqual(james.SectionsOS.Count, revision.SectionsOS.Count);
			ScrSection revSection1 = (ScrSection)revision.SectionsOS[0];
			ScrSection revSection2 = (ScrSection)revision.SectionsOS[1];
			Assert.IsTrue(james.SectionsOS[0].Hvo != revSection1.Hvo);
			Assert.IsTrue(james.SectionsOS[0].Hvo != revSection2.Hvo);
			Assert.IsTrue(james.SectionsOS[1].Hvo != revSection1.Hvo);
			Assert.IsTrue(james.SectionsOS[1].Hvo != revSection2.Hvo);
			FdoOwningSequence<IStPara> s2Paras = revSection2.ContentOA.ParagraphsOS;
			Assert.AreEqual(james.SectionsOS[0].ContentOA.ParagraphsOS.Count,
				revSection1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(james.SectionsOS[1].ContentOA.ParagraphsOS.Count,
				revSection2.ContentOA.ParagraphsOS.Count);
			IStTxtPara paraRev = (IStTxtPara)s2Paras[0];
			Assert.IsFalse(para.Hvo == paraRev.Hvo);
			Assert.AreEqual(hvoBookRef, revision.BookIdRAHvo);

			// Check the picture
			VerifyPicture(picture, paraRev, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests archiving a draft with one book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ArchiveDraftWithOneBookAndAFootnote()
		{
			CheckDisposed();
			int draftCount = m_scr.ArchivedDraftsOC.Count;
			ScrBook james = (ScrBook)m_scr.ScriptureBooksOS[1];
			int hvoBookRef = m_fdoCache.ScriptureReferenceSystem.BooksOS[58].Hvo;

			// Add a footnote to the first content para of the second section.
			StTxtPara para = (StTxtPara)((ScrSection)james.SectionsOS[1]).ContentOA.ParagraphsOS[0];
			StFootnote footnoteOrig = InsertTestFootnote(james, para, 0, 0);

			// archive draft
			IScrDraft draft = m_scr.CreateSavedVersion("Single james", new int[] { james.Hvo });

			Assert.AreEqual(draftCount + 1, m_scr.ArchivedDraftsOC.Count);
			Assert.AreEqual("Single james", draft.Description);
			Assert.AreEqual(1, draft.BooksOS.Count);
			ScrBook revision = (ScrBook)draft.BooksOS.FirstItem;
			Assert.IsFalse(james.Hvo == revision.Hvo);
			Assert.AreEqual(james.SectionsOS.Count, revision.SectionsOS.Count);
			ScrSection revSection1 = (ScrSection)revision.SectionsOS[0];
			ScrSection revSection2 = (ScrSection)revision.SectionsOS[1];
			Assert.IsTrue(james.SectionsOS[0].Hvo != revSection1.Hvo);
			Assert.IsTrue(james.SectionsOS[0].Hvo != revSection2.Hvo);
			Assert.IsTrue(james.SectionsOS[1].Hvo != revSection1.Hvo);
			Assert.IsTrue(james.SectionsOS[1].Hvo != revSection2.Hvo);
			FdoOwningSequence<IStPara> s2Paras = revSection2.ContentOA.ParagraphsOS;
			Assert.AreEqual(((ScrSection)james.SectionsOS[0]).ContentOA.ParagraphsOS.Count,
				revSection1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(((ScrSection)james.SectionsOS[1]).ContentOA.ParagraphsOS.Count,
				revSection2.ContentOA.ParagraphsOS.Count);
			IStTxtPara paraRev = (IStTxtPara)s2Paras[0];
			Assert.IsFalse(para.Hvo == paraRev.Hvo);
			Assert.AreEqual(hvoBookRef, revision.BookIdRAHvo);

			// Check the footnote
			Assert.AreEqual(james.FootnotesOS.Count, revision.FootnotesOS.Count);
			Assert.AreEqual(footnoteOrig.Hvo, james.FootnotesOS[0].Hvo);
			IStFootnote footnoteRev = revision.FootnotesOS[0];
			Assert.IsTrue(footnoteRev.Hvo != footnoteOrig.Hvo);

			VerifyFootnote(footnoteOrig, para, 0);
			VerifyFootnote(footnoteRev, paraRev, 0);
		}
		#endregion

		#region BackTransWs tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests BackTransWs when there are no back translations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTransWs_NoBTs()
		{
			CheckDisposed();

			ScrBook book = (ScrBook)m_scr.ScriptureBooksOS[0];

			// We expect that the book will have no back translations.
			Assert.IsNull(book.BackTransWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests BackTransWs when there is one back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTransWs_OneBT()
		{
			CheckDisposed();

			ILgWritingSystemFactory wsf = Cache.LanguageWritingSystemFactoryAccessor;
			int wsSpanish = wsf.GetWsFromStr("es");

			// Add a Spanish back translation
			ScrBook book = (ScrBook)m_scr.ScriptureBooksOS[0];
			StTxtPara para = (StTxtPara)book.SectionsOS[0].ContentOA.ParagraphsOS[0];
			ICmTranslation trans = para.GetOrCreateBT();
			ITsStrBldr bldr = trans.Translation.GetAlternative(wsSpanish).UnderlyingTsString.GetBldr();
			bldr.Replace(0, 0, "uno dos tres", null);
			trans.Translation.SetAlternative(bldr.GetString(), wsSpanish);

			// We expect that the book will have one back translation for Spanish.
			List<int> wsBTs = book.BackTransWs;
			Assert.IsNotNull(wsBTs);
			Assert.AreEqual(1, wsBTs.Count);
			Assert.IsTrue(wsBTs.Contains(wsSpanish));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests BackTransWs when there are three back translations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTransWs_ThreeBTs()
		{
			CheckDisposed();

			ILgWritingSystemFactory wsf = Cache.LanguageWritingSystemFactoryAccessor;
			int wsSpanish = wsf.GetWsFromStr("es");
			int wsGerman = wsf.GetWsFromStr("de");
			int wsFrench = wsf.GetWsFromStr("fr");

			// Add Spanish, German and French back translations.
			ScrBook book = (ScrBook)m_scr.ScriptureBooksOS[0];
			StTxtPara para = (StTxtPara)book.SectionsOS[0].ContentOA.ParagraphsOS[0];
			ICmTranslation trans = para.GetOrCreateBT();
			ITsStrBldr bldr = trans.Translation.GetAlternative(wsSpanish).UnderlyingTsString.GetBldr();
			bldr.Replace(0, 0, "uno dos tres", null);
			trans.Translation.SetAlternative(bldr.GetString(), wsSpanish);
			bldr = trans.Translation.GetAlternative(wsGerman).UnderlyingTsString.GetBldr();
			bldr.Replace(0, 0, "eins zwei drei", null);
			trans.Translation.SetAlternative(bldr.GetString(), wsGerman);
			bldr = trans.Translation.GetAlternative(wsFrench).UnderlyingTsString.GetBldr();
			bldr.Replace(0, 0, "un deux trois", null);
			trans.Translation.SetAlternative(bldr.GetString(), wsFrench);

			// We expect that the book will have back translations for Spanish, German and French.
			List<int> wsBTs = book.BackTransWs;
			Assert.IsNotNull(wsBTs);
			Assert.AreEqual(3, wsBTs.Count);
			Assert.IsTrue(wsBTs.Contains(wsSpanish));
			Assert.IsTrue(wsBTs.Contains(wsGerman));
			Assert.IsTrue(wsBTs.Contains(wsFrench));
		}
		#endregion

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
			ITsString tss = para.Contents.UnderlyingTsString;
			int iRun = tss.get_RunAt(ich);
			ITsTextProps orcPropsParaFootnote = tss.get_Properties(iRun);
			string objData = orcPropsParaFootnote.GetStrPropValue(
				(int)FwTextPropType.ktptObjData);
			Assert.AreEqual((char)(int)FwObjDataTypes.kodtGuidMoveableObjDisp, objData[0]);

			// Send the objData string without the first character because the first character
			// is the object replacement character and the rest of the string is the GUID.
			Guid newPicGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
			int newPicHvo = m_fdoCache.GetIdFromGuid(newPicGuid);
			Assert.IsTrue(pictureOrig.Guid != newPicGuid);
			Assert.IsTrue(pictureOrig.Hvo != newPicHvo);
			string sOrc = tss.get_RunText(iRun);
			Assert.AreEqual(StringUtils.kchObject, sOrc[0]);

			CmPicture pictureNew = new CmPicture(m_fdoCache, newPicHvo);
			Assert.IsTrue(pictureOrig.PictureFileRAHvo != pictureNew.PictureFileRAHvo);
			Assert.AreEqual(pictureOrig.PictureFileRA.InternalPath,
				pictureNew.PictureFileRA.InternalPath);
		}
	}
	#endregion
}
