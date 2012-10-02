// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2004' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeScrInitializerTests.cs
// Responsibility: TE team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	#region TestTeScrInitializer class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TestTeScrInitializer class exposes aspects of <see cref="TeScrInitializer"/> class
	/// for testing.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class TestTeScrInitializer : TeScrInitializer
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Init the base class TeScrInitializer for testing.
		/// </summary>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		public TestTeScrInitializer(FdoCache cache) : base(cache)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeScrInitializer.CreateScrBookRefs"/> method
		/// </summary>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		public void CallCreateScrBookRefs(FdoCache cache)
		{
			using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(Form.ActiveForm))
			{
				CreateScrBookRefs(progressDlg);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeScrInitializer.CreateScrBookAnnotations"/> method
		/// </summary>
		/// <param name="scr">The Scripture object in the DB</param>
		/// ------------------------------------------------------------------------------------
		public void CallCreateScrBookAnnotations(Scripture scr)
		{
			CreateScrBookAnnotations();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the <see cref="TeScrInitializer.EnsureScriptureTextsValid"/> method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void EnsureScriptureTextsValid()
		{
			base.EnsureScriptureTextsValid();
		}
	}
	#endregion

	#region TeScrInitializerTests class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TeScrInitializerTests is a collection of tests for static methods of the
	/// <see cref="TeScrInitializer"/> class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeScrInitializerTests : ScrInMemoryFdoTestBase
	{
		private TestTeScrInitializer m_scrInitializer;

		#region Setup, Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Connect to TestLangProj and start an undo task;
		/// init the base class we are testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_scrInitializer = new TestTeScrInitializer(Cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_scrInitializer = null;

			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeCache()
		{
			base.InitializeCache();
			m_scrInMemoryCache.InitializeWritingSystemEncodings();
		}
		#endregion

		#region Create ScrBookRefs tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test TeScrInitializer.CreateScrBookRefs method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateScrBookRefs()
		{
			CheckDisposed();

			IScrRefSystem scr = Cache.ScriptureReferenceSystem;
			m_scrInitializer.CallCreateScrBookRefs(Cache);

			FdoOwningSequence<IScrBookRef> books = scr.BooksOS;

			// Make sure the right number of books was generated.
			Assert.AreEqual(66, books.Count);

			ILgWritingSystemFactory wsf = Cache.LanguageWritingSystemFactoryAccessor;
			int wsEnglish = wsf.GetWsFromStr("en");
			int wsSpanish = wsf.GetWsFromStr("es");

			// Check English Genesis
			IScrBookRef genesis = books[0];
			Assert.AreEqual("Genesis",
				genesis.BookName.GetAlternative(wsEnglish));
			Assert.AreEqual("Gen",
				genesis.BookAbbrev.GetAlternative(wsEnglish));
			Assert.IsNull(genesis.BookNameAlt.GetAlternative(wsEnglish));

			// Check Spanish Matthew
			IScrBookRef mateo = books[39];
			Assert.AreEqual("Mateo",
				mateo.BookName.GetAlternative(wsSpanish));
			Assert.AreEqual("Mt",
				mateo.BookAbbrev.GetAlternative(wsSpanish));
			Assert.IsNull(mateo.BookNameAlt.GetAlternative(wsSpanish));

			// Check English 2 Corinthians
			IScrBookRef iiCor = books[46];
			Assert.AreEqual("2 Corinthians",
				iiCor.BookName.GetAlternative(wsEnglish));
			Assert.AreEqual("2Cor",
				iiCor.BookAbbrev.GetAlternative(wsEnglish));
			Assert.AreEqual("II Corinthians",
				iiCor.BookNameAlt.GetAlternative(wsEnglish));

			// Check Spanish Revelation
			IScrBookRef apocalipsis = books[65];
			Assert.AreEqual("Apocalipsis",
				apocalipsis.BookName.GetAlternative(wsSpanish));
			Assert.AreEqual("Ap",
				apocalipsis.BookAbbrev.GetAlternative(wsSpanish));
			Assert.IsNull(apocalipsis.BookNameAlt.GetAlternative(wsSpanish));

			MultilingScrBooks mlsb = new MultilingScrBooks(m_scr);
			mlsb.InitializeWritingSystems(Cache.LanguageWritingSystemFactoryAccessor);

			foreach (IScrBookRef brf in books)
			{
				string sBookName = brf.BookName.GetAlternative(wsEnglish);
				Assert.IsTrue(sBookName != null && sBookName != string.Empty);
				string sBookAbbrev = brf.BookAbbrev.GetAlternative(wsEnglish);
				Assert.IsTrue(sBookAbbrev != null && sBookAbbrev != string.Empty);
			}
		}
		#endregion

		#region Create CreateScrBookAnnotation tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TeScrInitializer.CreateScrBookAnnotation"/> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateScrBookAnnotations()
		{
			CheckDisposed();

			// Remove annotations for books that have already been created to test
			// CreateScrBookAnnotations.
			foreach (IScrBookAnnotations annotations in m_scr.BookAnnotationsOS)
				m_scr.BookAnnotationsOS.Remove(annotations.Hvo);

			m_scrInitializer.CallCreateScrBookAnnotations(m_scr);

			FdoOwningSequence<IScrBookAnnotations> bookNotes = m_scr.BookAnnotationsOS;

			// Make sure the right number of books was generated.
			Assert.AreEqual(66, bookNotes.Count);

			Assert.AreEqual(1, bookNotes[0].OwnOrd);
			Assert.AreEqual(66, bookNotes[BCVRef.LastBook - 1].OwnOrd);
		}
		#endregion

		#region Remove RTL marks from Scripture properties tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the TeScrInitializer removes the RTL marks from verse bridge, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveRTLMarksFromVerseBridgeEtc()
		{
			m_scr.Bridge = "\u200f-\u200f";
			m_scr.ChapterVerseSepr = "\u200f:\u200f";
			m_scr.VerseSepr = "\u200f,\u200f";
			m_scr.RefSepr = "\u200f;\u200f";
			ReflectionHelper.CallMethod(m_scrInitializer, "RemoveRtlMarksFromScrProperties");
			Assert.AreEqual("-", m_scr.Bridge);
			Assert.AreEqual(":", m_scr.ChapterVerseSepr);
			Assert.AreEqual(",", m_scr.VerseSepr);
			Assert.AreEqual(";", m_scr.RefSepr);
		}
		#endregion

		#region FixOrcsWithoutProps tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TeScrInitializer.FixOrcsWithoutProps()"/> method when the project
		/// has no footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixOrcsWithoutProps_NoFootnotesInProject()
		{
			CheckDisposed();

			IScrBook exodus = CreateExodusData();

			TeScrInitializer scrInit = new TestTeScrInitializer(Cache);
			Assert.IsNull(ReflectionHelper.GetResult(scrInit, "FixOrcsWithoutProps"));

			VerifyNoOrphanedFootnotes();
			VerifyResourceForFixedOrphans();
			Assert.AreEqual(0, exodus.FootnotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TeScrInitializer.FixOrcsWithoutProps()"/> method when the
		/// project has footnotes, but none are orphaned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixOrcsWithoutProps_AllFootnotesAreOkay()
		{
			CheckDisposed();

			IScrBook exodus = CreateExodusData();
			CreateFootnote(exodus, 0, 0, 0, 4, ScrStyleNames.NormalFootnoteParagraph, false);
			CreateFootnote(exodus, 2, 0, 1, 7, ScrStyleNames.NormalFootnoteParagraph, false);

			TeScrInitializer scrInit = new TestTeScrInitializer(Cache);
			Assert.IsNull(ReflectionHelper.GetResult(scrInit, "FixOrcsWithoutProps"));

			VerifyNoOrphanedFootnotes();
			VerifyResourceForFixedOrphans();
			Assert.AreEqual(2, exodus.FootnotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TeScrInitializer.FixOrcsWithoutProps()"/> method when the
		/// project has orphaned footnotes interspersed with non-orphaned footnotes such that all
		/// orphans are in the correct order.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixOrcsWithoutProps_OrphanedFootnotesInOrder()
		{
			CheckDisposed();

			IScrBook exodus = CreateExodusData();
			AddVerse(AddPara(exodus.SectionsOS[2]), 2, 3, "ORC is here, you see, my friend.");
			CreateFootnote(exodus, 0, 0, 0, 4, ScrStyleNames.NormalFootnoteParagraph, false);
			CreateFootnote(exodus, 1, 0, 1, 7, ScrStyleNames.NormalFootnoteParagraph, true);
			CreateFootnote(exodus, 2, 0, 2, 7, ScrStyleNames.CrossRefFootnoteParagraph, false);
			CreateFootnote(exodus, 2, 1, 3, 13, ScrStyleNames.NormalFootnoteParagraph, true);

			TeScrInitializer scrInit = new TestTeScrInitializer(Cache);
			List <string> report = (List<string>)ReflectionHelper.GetResult(scrInit, "FixOrcsWithoutProps");

			VerifyNoOrphanedFootnotes();
			VerifyResourceForFixedOrphans();

			Assert.AreEqual(2, report.Count);
			Assert.AreEqual("EXO 1:1 - Connected footnote to marker in the vernacular text", report[0]);
			Assert.AreEqual("EXO 2:3 - Connected footnote to marker in the vernacular text", report[1]);

			Assert.AreEqual(4, exodus.FootnotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TeScrInitializer.FixOrcsWithoutProps()"/> method when the
		/// project has two orphaned footnotes in the same paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixOrcsWithoutProps_TwoOrphanedFootnotesInParagraph()
		{
			CheckDisposed();

			IScrBook exodus = CreateExodusData();
			AddVerse(AddPara(exodus.SectionsOS[2]), 2, 3, "ORC is here, you see, my friend.");
			CreateFootnote(exodus, 1, 0, 1, 7, ScrStyleNames.NormalFootnoteParagraph, true);
			CreateFootnote(exodus, 1, 0, 1, 19, ScrStyleNames.CrossRefFootnoteParagraph, true);

			TeScrInitializer scrInit = new TestTeScrInitializer(Cache);
			List<string> report = (List<string>)ReflectionHelper.GetResult(scrInit, "FixOrcsWithoutProps");

			VerifyNoOrphanedFootnotes();
			VerifyResourceForFixedOrphans();

			Assert.AreEqual(2, report.Count);
			Assert.AreEqual("EXO 1:1 - Deleted corrupted footnote marker or picture anchor", report[0]);
			Assert.AreEqual("EXO 1:2 - Deleted corrupted footnote marker or picture anchor", report[1]);

			Assert.AreEqual(0, exodus.FootnotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TeScrInitializer.FixOrcsWithoutProps()"/> method when the
		/// project has an orphaned footnote and a valid picture ORC in the same book.
		/// TE-8769
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixOrcsWithoutProps_OrphanedFootnoteAndValidPicture()
		{
			CheckDisposed();

			IScrBook exodus = CreateExodusData();
			StTxtPara para = AddPara(exodus.SectionsOS[2]);
			AddVerse(para, 2, 3, "ORC is here, you see, my friend.");

			// Update the paragraph contents to include the picture
			ITsStrBldr tsStrBldr = para.Contents.UnderlyingTsString.GetBldr();
			ITsStrFactory factory = TsStrFactoryClass.Create();
			CmPicture pict = new CmPicture(Cache, "c:\\junk.jpg",
				factory.MakeString("Test picture", Cache.DefaultVernWs),
				StringUtils.LocalPictures);
			Assert.IsNotNull(pict);
			pict.InsertOwningORCIntoPara(tsStrBldr, 11, 0);
			para.Contents.UnderlyingTsString = tsStrBldr.GetString();

			// Update the paragraph contents to include the (orphaned) footnote marker
			CreateFootnote(exodus, 1, 0, 1, 19, ScrStyleNames.CrossRefFootnoteParagraph, true);

			TeScrInitializer scrInit = new TestTeScrInitializer(Cache);
			List<string> report = (List<string>)ReflectionHelper.GetResult(scrInit, "FixOrcsWithoutProps");

			VerifyNoOrphanedFootnotes();
			VerifyResourceForFixedOrphans();

			Assert.AreEqual(1, report.Count);
			Assert.AreEqual("EXO 1:2 - Deleted corrupted footnote marker or picture anchor", report[0]);

			Assert.AreEqual(0, exodus.FootnotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TeScrInitializer.FixOrcsWithoutProps()"/> method when the
		/// project has an ORC that has props, but the object it's pointing to is missing.
		/// This scenario is really outside the scope of what this method was intended to
		/// handle, but we're testing it because the implementation needs to deal with it. We
		/// hope this is no longer possible for this to happen, but if it does, we'll treat it
		/// like any other orphaned ORC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixOrcsWithoutProps_OrcForMissingObject()
		{
			CheckDisposed();

			IScrBook exodus = CreateExodusData();
			StTxtPara para = AddPara(exodus.SectionsOS[2]);
			AddVerse(para, 2, 3, "ORC is here, you see, my friend.");

			// Update the paragraph contents to include the picture
			ITsStrBldr tsStrBldr = para.Contents.UnderlyingTsString.GetBldr();
			ITsStrFactory factory = TsStrFactoryClass.Create();
			StringUtils.InsertOrcIntoPara(Guid.NewGuid(), FwObjDataTypes.kodtOwnNameGuidHot,
				tsStrBldr, 11, 11, 0);
			para.Contents.UnderlyingTsString = tsStrBldr.GetString();

			TeScrInitializer scrInit = new TestTeScrInitializer(Cache);
			List<string> report = (List<string>)ReflectionHelper.GetResult(scrInit, "FixOrcsWithoutProps");

			VerifyNoOrphanedFootnotes();
			VerifyResourceForFixedOrphans();

			Assert.AreEqual(1, report.Count);
			Assert.AreEqual("EXO 2:3 - Deleted corrupted footnote marker or picture anchor", report[0]);

			Assert.AreEqual(0, exodus.FootnotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TeScrInitializer.FixOrcsWithoutProps()"/> method when the
		/// project has orphaned footnotes int the book title, introduction and section headings
		/// (still in the correct order).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixOrcsWithoutProps_OrphanedFootnotesInTitleIntroAndHeading()
		{
			CheckDisposed();

			IScrBook exodus = CreateExodusData();
			CreateFootnote(exodus, exodus.TitleOA, 0, 0, 0, ScrStyleNames.NormalFootnoteParagraph, true);
			CreateFootnote(exodus, 0, 0, 1, 9, ScrStyleNames.NormalFootnoteParagraph, true);
			CreateFootnote(exodus, 1, 0, 2, 7, ScrStyleNames.NormalFootnoteParagraph, false);
			CreateFootnote(exodus, exodus.SectionsOS[2].HeadingOA, 0, 3, 7, ScrStyleNames.NormalFootnoteParagraph, true);
			CreateFootnote(exodus, 2, 0, 4, 13, ScrStyleNames.NormalFootnoteParagraph, false);

			TeScrInitializer scrInit = new TestTeScrInitializer(Cache);
			List<string> report = (List<string>)ReflectionHelper.GetResult(scrInit, "FixOrcsWithoutProps");

			VerifyNoOrphanedFootnotes();
			VerifyResourceForFixedOrphans();

			Assert.AreEqual(3, report.Count);
			Assert.AreEqual("EXO Title - Connected footnote to marker in the vernacular text", report[0]);
			Assert.AreEqual("EXO Intro Section 1, Contents - Connected footnote to marker in the vernacular text", report[1]);
			Assert.AreEqual("EXO 1:6-7 Section Heading - Connected footnote to marker in the vernacular text", report[2]);

			Assert.AreEqual(5, exodus.FootnotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TeScrInitializer.FixOrcsWithoutProps()"/> method when the
		/// project has orphaned footnotes interspersed with non-orphaned footnotes such that
		/// some orphans occur in the sequence of footnotes after their correct place.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixOrcsWithoutProps_OrphanedFootnotesOutOfOrder()
		{
			CheckDisposed();

			IScrBook exodus = CreateExodusData();
			CreateFootnote(exodus, 0, 0, 0, 4, ScrStyleNames.NormalFootnoteParagraph, false);
			CreateFootnote(exodus, 1, 0, 1, 7, ScrStyleNames.NormalFootnoteParagraph, true);
			CreateFootnote(exodus, 2, 0, 1 /* this causes this footnote to get inserted before the preceding one*/,
				7, ScrStyleNames.CrossRefFootnoteParagraph, false);
			CreateFootnote(exodus, 2, 0, 3, 14, ScrStyleNames.NormalFootnoteParagraph, true);

			TeScrInitializer scrInit = new TestTeScrInitializer(Cache);
			List<string> report = (List<string>)ReflectionHelper.GetResult(scrInit, "FixOrcsWithoutProps");

			VerifyResourceForFixedOrphans();

			Assert.AreEqual(3, report.Count);
			Assert.AreEqual("EXO 1:1 - Deleted corrupted footnote marker or picture anchor", report[0]);
			// Note that the last ORC gets hooked up to the first orpaned footnote, rather than to
			// its original footnote, leaving the last one as an orphan still.
			Assert.AreEqual("EXO 1:7 - Connected footnote to marker in the vernacular text", report[1]);
			Assert.AreEqual("EXO - Footnote 4 has no corresponding marker in the vernacular text", report[2]);

			Assert.AreEqual(4, exodus.FootnotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test <see cref="TeScrInitializer.FixOrcsWithoutProps()"/> method when the
		/// project has an orphaned footnote and no orphaned ORCs in the data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixOrcsWithoutProps_OrphanedFootnotesWithNoOrcs()
		{
			CheckDisposed();

			IScrBook exodus = CreateExodusData();
			CreateFootnote(exodus, 0, 0, 0, 4, ScrStyleNames.NormalFootnoteParagraph, false);
			//IStFootnote footnote =
			exodus.FootnotesOS.Append(new StFootnote());
			//IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS.Append(new StTxtPara());
			//para.Contents = StringUtils.MakeTss("Poor orphaned footnote 1")

			TeScrInitializer scrInit = new TestTeScrInitializer(Cache);
			List<string> report = (List<string>)ReflectionHelper.GetResult(scrInit, "FixOrcsWithoutProps");

			VerifyResourceForFixedOrphans();

			Assert.AreEqual(1, report.Count);
			Assert.AreEqual("EXO - Footnote 2 has no corresponding marker in the vernacular text", report[0]);

			Assert.AreEqual(2, exodus.FootnotesOS.Count);
		}

		#region Helper methods for FixOrcsWithoutProps tests
			/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a test footnote in the given location.
		/// </summary>
		/// <param name="book">The Scripture book.</param>
		/// <param name="iSection">The index of the section in the book.</param>
		/// <param name="iPara">The index of the Content paragraph where the footnote is to be
		/// inserted.</param>
		/// <param name="iFootnote">The index of the footnote to create.</param>
		/// <param name="ichOrc">The charaacter offset in the paragraph where the ORC (marker)
		/// is to be inserted.</param>
		/// <param name="sStyle">The style name (which determines whether it is a general
		/// footnote or a cross-reference).</param>
		/// <param name="fMakeOrphan">Flag indicating whether to make this footnote into an
		/// "orphan" be clearing the properties of the ORC.</param>
		/// ------------------------------------------------------------------------------------
		private IStFootnote CreateFootnote(IScrBook book, int iSection, int iPara, int iFootnote,
			int ichOrc, string sStyle, bool fMakeOrphan)
		{
			return CreateFootnote(book, book.SectionsOS[iSection].ContentOA, iPara, iFootnote,
				ichOrc, sStyle, fMakeOrphan);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a test footnote in the given location.
		/// </summary>
		/// <param name="book">The Scripture book.</param>
		/// <param name="text">The StText containing iPara.</param>
		/// <param name="iPara">The index of the Content paragraph where the footnote is to be
		/// inserted.</param>
		/// <param name="iFootnote">The index of the footnote to create.</param>
		/// <param name="ichOrc">The charaacter offset in the paragraph where the ORC (marker)
		/// is to be inserted.</param>
		/// <param name="sStyle">The style name (which determines whether it is a general
		/// footnote or a cross-reference).</param>
		/// <param name="fMakeOrphan">Flag indicating whether to make this footnote into an
		/// "orphan" be clearing the properties of the ORC.</param>
		/// ------------------------------------------------------------------------------------
		private IStFootnote CreateFootnote(IScrBook book, IStText text, int iPara, int iFootnote,
			int ichOrc, string sStyle, bool fMakeOrphan)
		{
			IStTxtPara scrPara = (IStTxtPara)text.ParagraphsOS[iPara];
			StFootnote footnote = InsertTestFootnote(book, scrPara, iFootnote, ichOrc);
			StTxtPara fnPara = m_inMemoryCache.AddParaToMockedText(footnote.Hvo, sStyle);
			m_inMemoryCache.AddRunToMockedPara(fnPara, "Footnote " + Guid.NewGuid(), Cache.DefaultVernWs);

			if (fMakeOrphan)
			{
				ITsStrBldr bldr = scrPara.Contents.UnderlyingTsString.GetBldr();
				bldr.SetProperties(ichOrc, ichOrc + 1, StringUtils.PropsForWs(Cache.DefaultVernWs));
				scrPara.Contents.UnderlyingTsString = bldr.GetString();
			}
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that a CmResource was added to indicate that orphaned footnotes have been
		/// cleaned up for this project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void VerifyResourceForFixedOrphans()
		{
			bool fResourceWasInserted = false;
			foreach (ICmResource resource in m_scr.ResourcesOC)
			{
				fResourceWasInserted |= (resource.Name == TeScrInitializer.ksFixedOrphanedFootnotes &&
				resource.Version == TeScrInitializer.kguidFixedOrphanedFootnotes);
			}
			Assert.IsTrue(fResourceWasInserted);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that all footnotes correspond to exactly one ORC in the vernacular
		/// Scripture and that they are in the same order as they occur in Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void VerifyNoOrphanedFootnotes()
		{
			ScrChecksDataSource scrData = new ScrChecksDataSource(Cache);
			foreach (IScrBook book in m_scr.ScriptureBooksOS)
			{
				IEnumerator<IStFootnote> footnotes = book.FootnotesOS.GetEnumerator();
				IStFootnote currentFootnote = (footnotes.MoveNext() ? footnotes.Current : null);

				scrData.GetText(book.CanonicalNum, 0);
				foreach (ITextToken tok in scrData.TextTokens())
				{
					if (tok.TextType == TextType.Note)
					{
						if (tok.IsNoteStart)
						{
							Assert.IsNotNull(currentFootnote, "No more footnotes were expected in " + book.BestUIName);
							Assert.IsTrue(((IStTxtPara)currentFootnote.ParagraphsOS[0]).Contents.Text.StartsWith(tok.Text),
								"Footnote ORC does not match next footnote in sequence (mismatched text)");
							Assert.AreEqual(currentFootnote.ParagraphsOS[0].StyleName, tok.ParaStyleName,
								"Footnote ORC does not match next footnote in sequence (mismatched style)");
							currentFootnote = (footnotes.MoveNext() ? footnotes.Current : null);
						}
					}
					else
					{
						Assert.IsFalse(tok.Text.Contains(StringUtils.kszObject));
					}
				}
				Assert.IsNull(currentFootnote);
			}
		}
		#endregion
		#endregion
	}
	#endregion
}
