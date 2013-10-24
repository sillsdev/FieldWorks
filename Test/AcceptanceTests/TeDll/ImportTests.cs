// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2003' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ImportTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.TE.ImportTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.TE;
using SILUBS.SharedScrUtils;
using SIL.CoreImpl;
using SIL.Utils;
using System.Reflection;

namespace SIL.FieldWorks.AcceptanceTests.TE
{
	//#region class ATDummyTeImportManager
	///// ----------------------------------------------------------------------------------------
	///// <summary>
	///// An import manager.
	///// </summary>
	///// ----------------------------------------------------------------------------------------
	//internal class ATTeImportManager : TeImportManager
	//{
	//    #region Constructor
	//    /// ------------------------------------------------------------------------------------
	//    /// <summary>
	//    /// Initializes a new instance of the <see cref="T:ATTeImportManager"/> class.
	//    /// </summary>
	//    /// <param name="cache">The cache.</param>
	//    /// <param name="styleSheet">The style sheet.</param>
	//    /// ------------------------------------------------------------------------------------
	//    public ATTeImportManager(FdoCache cache, FwStyleSheet styleSheet) :
	//        base(cache, styleSheet)
	//    {
	//    }
	//    #endregion

	//    #region Internal methods/properties for testing
	//    /// ------------------------------------------------------------------------------------
	//    /// <summary>
	//    /// Import scripture and embed it in a Undo task so that it is undoable.
	//    /// Simulates TeImportManager.DoImport()
	//    /// </summary>
	//    /// <param name="importSettings">Import settings</param>
	//    /// <returns></returns>
	//    /// ------------------------------------------------------------------------------------
	//    internal void CallImportWithUndoTask(IScrImportSet importSettings)
	//    {
	//        //CompleteImport(ImportWithUndoTask(importSettings, false, "Import for TE Acceptance tests"));
	//        ScrReference firstImported;
	//        firstImported = ImportWithUndoTask(importSettings, false, "Import for TE Acceptance tests");
	//        CompleteImport(firstImported);
	//    }
	//    #endregion
	//}
	//#endregion

	#region AT TE Import Tests
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// ATImportTests class to test TeImport.
	/// These tests utilize TestLangProj and physical Standard Format files.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[TestFixture]
	public class ATImportTests : XmlProjectTestBase
	{
		#region Member variables
		private IScripture m_scr;
		//private DummyTeImporter m_importer;
		private BCVRef m_bcvRefTitus;
		private BCVRef m_bcvRefJonah;
		private FwStyleSheet m_styleSheet;
		private IScrImportSet m_settings;
		private TempSFFileMaker m_fileMaker;
		private int m_wsVern; // writing system info needed by tests
		private int m_wsAnal;
		private ITsTextProps m_ttpVernWS; // simple run text props expected by tests
		private ITsTextProps m_ttpAnalWS;
		private RegistryData m_regData;
		#endregion

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Init writing system member variables and some props needed by some tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitWsInfo()
		{
			// get writing system info needed by tests
			m_wsVern = Cache.DefaultVernWs;
			m_wsAnal = Cache.DefaultAnalWs;

			// init simple run text props expected by tests
			ITsPropsBldr tsPropsBldr = TsPropsBldrClass.Create();
			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsVern);
			m_ttpVernWS = tsPropsBldr.GetTextProps();
			tsPropsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsAnal);
			m_ttpAnalWS = tsPropsBldr.GetTextProps();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize things used by all import tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			// our base class FixtureSetup loads the project we specify here
			m_xmlProjectFilename = "TestLangProj.xml";
			base.FixtureSetup();

			// Create temp registry and unzip the physical SF files
			System.Diagnostics.Debug.WriteLine("unzipping SF files");
			//Logger.WriteEvent("unzipping SF files");
			Unpacker.UnPackParatextTestProjects();
			Unpacker.UnPackSfTestProjects();
			m_regData = Unpacker.PrepareRegistryForPTData();

			// Initialize our versification info from the vrs files
			System.Diagnostics.Debug.WriteLine("initializing versification info from files");
			//Logger.WriteEvent("initializing versification info from files");
			ScrReference.InitializeVersification(DirectoryFinder.GetFWCodeSubDirectory(
				"Translation Editor"), false);

			// init some member variables for the tests
			InitWsInfo();
			m_scr = Cache.LangProject.TranslatedScriptureOA;
			m_styleSheet = new FwStyleSheet();
			m_styleSheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			m_fileMaker = new TempSFFileMaker();
			m_bcvRefTitus = new BCVRef(56, 1, 1);
			m_bcvRefJonah = new BCVRef(32, 1, 1);

			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				// By default, use auto-generated footnote markers for import tests.
				m_scr.FootnoteMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker;

				//get a reference to the import settings in the project cache
				m_settings = Cache.LangProject.TranslatedScriptureOA.DefaultImportSettings;

				// patch in a few values so that the settings appear valid
				//m_settings.ImportTypeEnum = TypeOfImport.Paratext6;
				//m_settings.ImportTypeEnum = TypeOfImport.Other;
				//m_settings.ParatextScrProj = "TEV";
			});

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test Setup: Initialize the importer and set up Undo action
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			//(m_settings as IScrImportSet).StartRef = m_bcvRefTitus;
			//(m_settings as IScrImportSet).EndRef = m_bcvRefTitus;
			//m_settings.ImportTranslation = true;
			//m_settings.ImportBackTranslation = false;
			//m_settings.ImportBookIntros = true;
			//m_settings.ImportAnnotations = false;

//			FilteredScrBooks bookFilter = null;

			// Initialize the importer and set up its Undo action
			// pass null as the in memory cache since the acceptance tests should use the real cache ??
			// do we want to use a real TeImporter instead of a Dummy, because this is an Acceptance test ??
//			m_importer = new DummyTeImporter(m_settings, Cache, m_styleSheet, bookFilter);

			//m_importer = new DummyTeImporter(m_settings, this, m_styleSheet);
			//m_importer.Initialize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test TearDown: Roll back any DB changes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			base.TestTearDown();
			//Cache.DatabaseAccessor.RollbackSavePoint(m_sSavepoint);
			//Cache.VwCacheDaAccessor.ClearInfoAbout(
			//    Cache.LangProject.TranslatedScriptureOA.DefaultImportSettingsHvo, VwClearInfoAction.kciaRemoveObjectAndOwnedInfo);
			//m_importer = null;
		}
		#endregion

		#region Footnote Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the import of two books where a footnote is at the end of the first book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportSFProjectFootnoteAtEnd()
		{
			string fileMat =
				m_fileMaker.CreateFile("MAT", new string[] {@"\c 1", @"\v 1 Hi, I'm Matt",
					@"\f test footnote1"});
			 string fileMrk =
				 m_fileMaker.CreateFile("MRK", new string[] {@"\c 1", @"\v 1 Hi, I'm Mark",
					@"\f test footnote2"});

			SetupAndImportData(new string[]{fileMat, fileMrk}, "MAT", "MRK");

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(1, book.TitleOA.ParagraphsOS.Count);
			IStTxtPara titlePara = (IStTxtPara)book.TitleOA.ParagraphsOS[0];
			Assert.IsNotNull(titlePara);
			Assert.IsNull(titlePara.Contents.Text);
			ITsString tssTitle = titlePara.Contents;
			Assert.AreEqual(1, tssTitle.RunCount);
			ITsTextProps propsExpected = StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs);
			ITsTextProps propsActual = tssTitle.get_Properties(0);
			string sWhy;
			if (!TsTextPropsHelper.PropsAreEqual(propsExpected, propsActual, out sWhy))
				Assert.Fail(sWhy);
			Assert.AreEqual("Matthew", book.Name.UserDefaultWritingSystem);

			IStTxtPara footnotetext =
				(IStTxtPara)((IStFootnote)book.FootnotesOS[0]).ParagraphsOS[0];
			Assert.AreEqual("test footnote1", footnotetext.Contents.Text);

			book = (IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[1];
			footnotetext =
				(IStTxtPara)((IStFootnote)book.FootnotesOS[0]).ParagraphsOS[0];
			Assert.AreEqual("test footnote2", footnotetext.Contents.Text);
		}
		#endregion

		#region Import Existing Book (TE-475)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the import of an existing book. The existing book should first be moved to the
		/// revision position in the database.
		/// </summary>
		/// <remarks>This test ends up replacing the existing french Philemon with an English
		/// Philemon.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportExistingBook()
		{
			IFdoOwningCollection<IScrDraft> archivedDrafts =
				Cache.LangProject.TranslatedScriptureOA.ArchivedDraftsOC;
			int nDrafts = archivedDrafts.Count;

			// modify the existing copy of Philemon
			IScrBook philemon = Cache.LangProject.TranslatedScriptureOA.FindBook("PHM");
			Assert.IsNotNull(philemon, "Philemon is missing!");
			int cSections = philemon.SectionsOS.Count;
			IFdoOwningSequence<IStPara> titleParas = philemon.TitleOA.ParagraphsOS;
			Assert.AreEqual(1, titleParas.Count, "Title should consist of 1 para before import");
			// add guid to title, to make it unique
			ITsStrBldr titleBldr = ((IStTxtPara)titleParas[0]).Contents.GetBldr();
			titleBldr.Replace(0, 0, System.Guid.NewGuid().ToString(), null);
			ITsString tssGuidTitle = titleBldr.GetString();
			((IStTxtPara)titleParas[0]).Contents = tssGuidTitle;

			// set up a ScrImportSetFW for our test
			m_settings.ImportTranslation = true;
			m_settings.ImportBookIntros = true;
			m_settings.ImportAnnotations = false;
			MakeSFImportTestSettings((m_settings as IScrImportSet));

			// do the import
			BCVRef screfPhm = new BCVRef(57, 1, 1);
			(m_settings as IScrImportSet).StartRef = screfPhm;
			(m_settings as IScrImportSet).EndRef = screfPhm;
			TeSfmImporter.Import(m_settings, Cache, m_styleSheet,
				new DummyUndoImportManager(this), new TeImportNoUi());

			// verify the new imported book
			philemon = Cache.LangProject.TranslatedScriptureOA.FindBook("PHM");
			Assert.IsNotNull(philemon, "Philemon was not imported");
			titleParas = philemon.TitleOA.ParagraphsOS;
			Assert.AreEqual(1, titleParas.Count, "Title should consist of 1 para after import");
			Assert.IsTrue(
				((IStTxtPara)titleParas[0]).Contents.Text.StartsWith("Paul's Letter to"));
			Assert.AreEqual(6, philemon.SectionsOS.Count,
				"The English version of Philemon has 6 sections");

			// Verify that the modified (i.e., original) copy was moved to the revision position
			// in the DB.
			Assert.AreEqual(nDrafts + 1, archivedDrafts.Count);
			IScrDraft draft = archivedDrafts.ToArray()[archivedDrafts.Count - 1];
			Assert.AreEqual(1, draft.BooksOS.Count);

			IScrBook bookRev = draft.BooksOS[0];
			Assert.AreEqual("PHM", bookRev.BookId);
			titleParas = bookRev.TitleOA.ParagraphsOS;
			Assert.AreEqual(1, titleParas.Count, "Title in revision should consist of 1 paras");
			AssertEx.AreTsStringsEqual(tssGuidTitle,
				((IStTxtPara)titleParas[0]).Contents);
			Assert.AreEqual(cSections, bookRev.SectionsOS.Count);
		}
		#endregion

		#region Load SF project with some missing files (TE-516)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Should be possible to import a book of SF scripture even though OTHER books in the
		/// project are unavailable
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportSFProjectWithSomeMissingFiles()
		{
			string fileMat =
				m_fileMaker.CreateFile("MAT", new string[] {@"\mt Matthew", @"\c 1", @"\v 1 Hi"});
			string fileLuk =
				m_fileMaker.CreateFile("LUK", new string[] {@"\mt Luke", @"\c 1", @"\v 1 Bye"});

			m_settings.ImportTypeEnum = TypeOfImport.Other;

			// add files to the project
			m_settings.AddFile(fileMat, ImportDomain.Main, null, null);
			m_settings.AddFile(fileLuk, ImportDomain.Main, null, null);

			// Blow away the Matthew file
			File.Delete(fileMat);

			// now do the import -- this should not crash.
			(m_settings as IScrImportSet).StartRef = new BCVRef(41, 1, 1); // Matthew
			(m_settings as IScrImportSet).EndRef = new BCVRef(43, 1, 1); //Luke
			TeSfmImporter.Import(m_settings, Cache, m_styleSheet,
				new DummyUndoImportManager(this), new TeImportNoUi());
		}
		#endregion

		#region Import headings with character style
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportWithCharStyleInMainTitle()
		{
			string fileMat =
				m_fileMaker.CreateFile("MAT", new string[] {@"\mt Title |utext|e bla", @"\c 1",
															@"\v 1 bla"});
			SetupAndImportData(new string[]{fileMat}, "MAT", "MAT");

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(1, book.TitleOA.ParagraphsOS.Count);
			IStTxtPara titlePara = (IStTxtPara) book.TitleOA.ParagraphsOS[0];
			Assert.IsNotNull(titlePara);
			Assert.AreEqual("Title text bla", titlePara.Contents.Text);
			ITsString tssTitle = titlePara.Contents;
			Assert.AreEqual(3, tssTitle.RunCount);
			// first run
			ITsTextProps propsExpected = StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs);
			ITsTextProps propsActual = tssTitle.get_Properties(0);
			string sWhy;
			if (!TsTextPropsHelper.PropsAreEqual(propsExpected, propsActual, out sWhy))
				Assert.Fail(sWhy);
			// second run
			propsExpected = StyleUtils.CharStyleTextProps("Key Word",
				Cache.DefaultVernWs);
			propsActual = tssTitle.get_Properties(1);
			if (!TsTextPropsHelper.PropsAreEqual(propsExpected, propsActual, out sWhy))
				Assert.Fail(sWhy);
			// third run
			propsExpected = StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs);
			propsActual = tssTitle.get_Properties(2);
			if (!TsTextPropsHelper.PropsAreEqual(propsExpected, propsActual, out sWhy))
				Assert.Fail(sWhy);

			Assert.AreEqual("Matthew", book.Name.UserDefaultWritingSystem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportWithCharStyleInSectionHead()
		{
			string fileMat =
				m_fileMaker.CreateFile("MAT", new string[] {@"\mt Title",
															@"\s Section |uHead|e Text",
															@"\c 1", @"\v 1 bla"});
			SetupAndImportData(new string[]{fileMat}, "MAT", "MAT");

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection section = (IScrSection) book.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara sectionPara = (IStTxtPara) section.HeadingOA.ParagraphsOS[0];
			Assert.IsNotNull(sectionPara);
			Assert.AreEqual("Section Head Text", sectionPara.Contents.Text);
			ITsString tssSection = sectionPara.Contents;
			Assert.AreEqual(3, tssSection.RunCount);
			// first run
			ITsTextProps propsExpected = StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs);
			ITsTextProps propsActual = tssSection.get_Properties(0);
			string sWhy;
			if (!TsTextPropsHelper.PropsAreEqual(propsExpected, propsActual, out sWhy))
				Assert.Fail(sWhy);
			// second run
			propsExpected = StyleUtils.CharStyleTextProps("Key Word", Cache.DefaultVernWs);
			propsActual = tssSection.get_Properties(1);
			if (!TsTextPropsHelper.PropsAreEqual(propsExpected, propsActual, out sWhy))
				Assert.Fail(sWhy);
			// third run
			propsExpected = StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs);
			propsActual = tssSection.get_Properties(2);
			if (!TsTextPropsHelper.PropsAreEqual(propsExpected, propsActual, out sWhy))
				Assert.Fail(sWhy);

			Assert.AreEqual("Matthew", book.Name.UserDefaultWritingSystem);
			Assert.AreEqual("Title", book.Name.VernacularDefaultWritingSystem);
		}
		#endregion

		#region Load an SF file with and without a Main Title marker TE-532
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If an MT marker is not found in the file then use the UI language to set the
		/// title of the book and create a title paragraph.  This test does not have an MT
		/// marker so we should expect a UI language paragraph to be created with the title
		/// set appropriately.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportSFProjectWithoutMT()
		{
			string fileMat =
				m_fileMaker.CreateFile("MAT", new string[] {@"\c 1", @"\v 1 Hi, I'm Matt"});

			SetupAndImportData(new string[]{fileMat}, "MAT", "MAT");

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(1, book.TitleOA.ParagraphsOS.Count);
			IStTxtPara titlePara = (IStTxtPara) book.TitleOA.ParagraphsOS[0];
			Assert.IsNotNull(titlePara);
			Assert.IsNull(titlePara.Contents.Text);
			ITsString tssTitle = titlePara.Contents;
			Assert.AreEqual(1, tssTitle.RunCount);
			ITsTextProps propsExpected = StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs);
			ITsTextProps propsActual = tssTitle.get_Properties(0);
			string sWhy;
			if (!TsTextPropsHelper.PropsAreEqual(propsExpected, propsActual, out sWhy))
				Assert.Fail(sWhy);
			Assert.AreEqual("Matthew", book.Name.UserDefaultWritingSystem);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If an MT marker is not found in the file then use the UI language to set the title
		/// of the book and create a title paragraph.  This test has an MT marker so we should
		/// expect that the default paragraph will be overwritten with the text in the MT
		/// marker and that the book title will be set with a UI name and a vernacular name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportSFProjectWithMT()
		{
			string fileMat =
				m_fileMaker.CreateFile("MAT", new string[] {@"\mt mattheues", @"\c 1", @"\v 1 Hi, I'm Matt"});

			SetupAndImportData(new string[]{fileMat}, "MAT", "MAT");

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			IStTxtPara titlePara = (IStTxtPara) book.TitleOA.ParagraphsOS[0];
			Assert.IsNotNull(titlePara);
			Assert.AreEqual("mattheues", titlePara.Contents.Text);
			Assert.AreEqual("Matthew", book.Name.UserDefaultWritingSystem);
			Assert.AreEqual("mattheues", book.Name.VernacularDefaultWritingSystem);
		}
		#endregion

		#region Tests for various section and chapter marker arrangments at the beginning if importing a book.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Import_InlineBackslashMarkers()
		{
			string fileLuk =
				m_fileMaker.CreateFile("LUK", new string[] {@"\s Sec", @"\c 1",
				@"\v 1 verse one.\ft Footnote text.\fte More verse one."});

			SetupAndImportData(new string[]{fileLuk}, "LUK", "LUK");

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(1, book.SectionsOS.Count);
			IStTxtPara para = (IStTxtPara)((IScrSection)book.SectionsOS[0]).ContentOA.ParagraphsOS[0];
			ITsString tss = para.Contents;
			Assert.AreEqual("verse one.", tss.get_RunText(2));
			Assert.AreEqual("More verse one.", tss.get_RunText(4));
			Assert.AreEqual("Footnote text.",
				((IStTxtPara)((IStFootnote)book.FootnotesOS[0]).ParagraphsOS[0]).Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Import_SectionMarkerBeforeChapterMarker()
		{
			string fileLuk =
				m_fileMaker.CreateFile("LUK", new string[] {@"\s Sec", @"\c 1", @"\v 1 Hi, I'm Matt"});

			SetupAndImportData(new string[]{fileLuk}, "LUK", "LUK");

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual("LUK", book.BookId);
			Assert.AreEqual(1, book.SectionsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Sec", para.Contents.Text);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Section Head"), para.StyleRules);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Import_NoScrSectionMarker()
		{

			string fileLuk =
				m_fileMaker.CreateFile("LUK", new string[] {@"\c 1", @"\v 1 Hi, I'm Matt"});

			SetupAndImportData(new string[]{fileLuk}, "LUK", "LUK");

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(1, book.SectionsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(null, para.Contents.Text);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Section Head"), para.StyleRules);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the case where an intro section should preceed scripture, even when there is
		/// no intro section content. While this test is ignored (and the code to make it pass
		/// not written) the scripture section's head will have the intro section head style.
		/// Another variation on this test may be to have no \ip marker at all.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Not implemented and not requested by customer yet.")]
		public void Import_NoIntroSectionContent()
		{

			string fileLuk =
				m_fileMaker.CreateFile("LUK", new string[] {@"\is Intro Head", @"\ip", @"\c 1", @"\v 1 One"});

			SetupAndImportData(new string[]{fileLuk}, "LUK", "LUK");

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			// we expect 2 section: an intro section and a scripture section
			Assert.AreEqual(2, book.SectionsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).ContentOA.ParagraphsOS.Count);

			IStTxtPara para = (IStTxtPara)((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(null, para.Contents.Text);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Intro Section Head"), para.StyleRules);
			para = (IStTxtPara)((IScrSection)book.SectionsOS[0]).ContentOA.ParagraphsOS[0];
			// though the intro paragraph is empty, every section must have a content paragraph
			Assert.AreEqual(null, para.Contents.Text);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Intro Paragraph"), para.StyleRules);

			para = (IStTxtPara)((IScrSection)book.SectionsOS[1]).HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(null, para.Contents.Text);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Section Head"), para.StyleRules);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Import_NoIntroSectionMarker()
		{

			// NOTE: we need chapter and verse in order for EcObjects to work
			string fileLuk =
				m_fileMaker.CreateFile("LUK", new string[] {@"\ip Hello", @"\c 1", @"\v 1 ver"});

			SetupAndImportData(new string[]{fileLuk}, "LUK", "LUK");

			// verify that the book has two sections
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(2, book.SectionsOS.Count);

			// verify the empty intro section head, created by default
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(null, para.Contents.Text);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Intro Section Head"), para.StyleRules);

			// verify the intro paragraph
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)((IScrSection)book.SectionsOS[0]).ContentOA.ParagraphsOS[0];
			Assert.AreEqual("Hello", para.Contents.Text);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Intro Paragraph"), para.StyleRules);

			// verify the empty Scripture section head, created by default
			para = (IStTxtPara)((IScrSection)book.SectionsOS[1]).HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(null, para.Contents.Text);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Section Head"), para.StyleRules);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Import_SectionMarkerNoSecHeadText()
		{

			string fileLuk =
				m_fileMaker.CreateFile("LUK", new string[] {@"\s", @"\c 1", @"\v 1 Yeah!"});

			SetupAndImportData(new string[]{fileLuk}, "LUK", "LUK");

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(1, book.SectionsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(null, para.Contents.Text);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Section Head"), para.StyleRules);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Import_IntroSectionMarkerNoSecHeadText()
		{

			// NOTE: we need chapter and verse in order for EcObjects to work
			string fileLuk =
				m_fileMaker.CreateFile("LUK", new string[] {@"\is", @"\ip Para", @"\c 1", @"\v 1 ver"});

			SetupAndImportData(new string[]{fileLuk}, "LUK", "LUK");

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(2, book.SectionsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(null, para.Contents.Text);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Intro Section Head"), para.StyleRules);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Import_NoSectionBreakBetweenIntroAndChapter()
		{

			// NOTE: we need chapter and verse in order for EcObjects to work
			string fileLuk =
				m_fileMaker.CreateFile("LUK", new string[] {@"\is Sec", @"\ip para", @"\c 1", @"\v 1 ver"});

			SetupAndImportData(new string[]{fileLuk}, "LUK", "LUK");

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(2, book.SectionsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[1]).HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[1]).ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Sec", para.Contents.Text);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Intro Section Head"), para.StyleRules);
			para = (IStTxtPara)((IScrSection)book.SectionsOS[1]).HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(null, para.Contents.Text);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Section Head"), para.StyleRules);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Import_SecHeadInsteadOfIntroSecHead()
		{

			// NOTE: we need chapter and verse in order for EcObjects to work
			string fileLuk =
				m_fileMaker.CreateFile("LUK", new string[] {@"\s Sec", @"\ip para", @"\c 1", @"\v 1 ver"});

			SetupAndImportData(new string[]{fileLuk}, "LUK", "LUK");

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(2, book.SectionsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[0]).ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[1]).HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, ((IScrSection)book.SectionsOS[1]).ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)((IScrSection)book.SectionsOS[0]).HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Sec", para.Contents.Text);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Intro Section Head"), para.StyleRules);
			para = (IStTxtPara)((IScrSection)book.SectionsOS[1]).HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(null, para.Contents.Text);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Section Head"), para.StyleRules);
		}
		#endregion

		#region Domain import tests TE-1345
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the back translation domain on a run of text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportDomainExcludeBackTrans()
		{

			string fileMat =
				m_fileMaker.CreateFile("MAT", new string[] {@"\c 1", @"\v 1 Hi, I'm Matt",
					@"\btp This is back translation text."});

			// Simulate condition where user did NOT check the box to include the BT
			SetupAndImportData(new string[]{fileMat}, "MAT", "MAT", true, false, false, false);

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection section = (IScrSection) book.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara paragraph = (IStTxtPara) section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Hi, I'm Matt", paragraph.Contents.Text);
			// TODO TE-1625: Test that BT did not get created.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the note domain on a run of text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportDomainNoteNotChecked()
		{

			string fileMat =
				m_fileMaker.CreateFile("MAT", new string[] {@"\c 1", @"\v 1 Hi, I'm Matt",
															   @"\np This is a note."});

			SetupAndImportData(new string[]{fileMat}, "MAT", "MAT");

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection section = (IScrSection) book.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara paragraph = (IStTxtPara) section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Hi, I'm Matt", paragraph.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the note domain on a run of text in the main translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportDomainNoteInTranslation()
		{

			int annCount = Cache.LangProject.AnnotationsOC.Count;
			string fileMat =
				m_fileMaker.CreateFile("MAT", new string[] {@"\c 1", @"\v 1 Hi, I'm Matt",
															   @"\v 2 Hi, this is Matt again",
															   @"\np This is a note."});

			SetupAndImportData(new string[]{fileMat}, "MAT", "MAT", true, false, true, false);

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection section = (IScrSection) book.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara paragraph = (IStTxtPara) section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Hi, I'm Matt 2Hi, this is Matt again", paragraph.Contents.Text);
			Assert.AreEqual(annCount + 1, Cache.LangProject.AnnotationsOC.Count,
				"did not add a note");

			ICmAnnotation note = null;
			foreach (ICmAnnotation ann in Cache.LangProject.AnnotationsOC)
			{
				if (ann.Comment.AnalysisDefaultWritingSystem.Text == "This is a note.")
					note = ann;
			}
			Assert.IsNotNull(note, "Should have found the inserted note.");
			Assert.IsTrue(note is ICmBaseAnnotation, "Note not of type CmBaseAnnotation");

			Assert.AreEqual(paragraph.Hvo, ((ICmBaseAnnotation)note).BeginObjectRA.Hvo);
			Assert.AreEqual(paragraph.Hvo, ((ICmBaseAnnotation)note).EndObjectRA.Hvo);
			Assert.AreEqual(40001002, (int)((ICmBaseAnnotation)note).BeginRef,
				"Begin ref should be set correctly");
			Assert.AreEqual(40001002, (int)((ICmBaseAnnotation)note).EndRef,
				"End reg should be set correctly");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the introduction domain on a run of text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImportDomainIntroNotChecked()
		{

			string fileMat =
				m_fileMaker.CreateFile("MAT", new string[] {@"\ip Intro", @"\c 1",
					@"\v 1 Hi, I'm Matt"});

			SetupAndImportData(new string[]{fileMat}, "MAT", "MAT", true, false, false, false);

			// Check the book name and the main title paragraph existence and text
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection section = (IScrSection) book.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara paragraph = (IStTxtPara) section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Hi, I'm Matt", paragraph.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with scripture text
		/// that has in-line markers.
		/// We will process this marker sequence:
		///    id c1 p v1 vt |em{} btvt |em{}
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationWithInlineMarkers()
		{

			string fileMat =
				m_fileMaker.CreateFile("MAT", new string[] {@"\c 1", @"\p",
					@"\v 1", @"\vt A record of the |em{genealogy}",
					@"\btvt El record de la |em{genealogia}"});

			SetupAndImportData(new string[]{fileMat}, "MAT", "MAT", true, true, false, false);

			// Check that the emphasized text made it into the correct paragraphs
			IScrBook book =
				(IScrBook)Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection section = (IScrSection) book.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara paragraph = (IStTxtPara) section.ContentOA.ParagraphsOS[0];
			ITsString contents = paragraph.Contents;
			Assert.AreEqual("11A record of the genealogy", contents.Text);
			Assert.AreEqual(4, contents.RunCount);
			Assert.AreEqual("genealogy", contents.get_RunText(3));
			Assert.AreEqual(1, paragraph.TranslationsOC.Count);
			foreach (ICmTranslation trans in paragraph.TranslationsOC)
			{
				ITsString bt = trans.Translation.AnalysisDefaultWritingSystem;
				Assert.AreEqual("11El record de la genealogia", bt.Text);
				Assert.AreEqual(4, bt.RunCount);
				Assert.AreEqual("genealogia", bt.get_RunText(3));
			}
		}
		#endregion

		#region ParatextImportTitusTEV
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the import of a basic Paratext project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParatextImportTitusTEV()
		{
			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
				m_actionHandler, "import settings"))
			{
				m_settings.ImportBookIntros = true;
				m_settings.ImportAnnotations = false;
				m_settings.ImportTranslation = true;
				m_settings.ImportBackTranslation = false;
				MakeParatextImportTestSettings((m_settings as IScrImportSet));
				(m_settings as IScrImportSet).StartRef = (m_settings as IScrImportSet).EndRef = m_bcvRefTitus;
				undoHelper.RollBack = false; // keep the changes
			}

			// do the import
			DummyTeImportManager mgr = new DummyTeImportManager(Cache, m_styleSheet);
			mgr.SimulateAcceptAllBooks = true;
			mgr.CallImportWithUndoTask(m_settings);
			//TeSfmImporter.Import(m_settings, Cache, m_styleSheet,
			//    new DummyUndoImportManager(this), new TeImportNoUi());

			// verify the results
			IScrBook titus = Cache.LangProject.TranslatedScriptureOA.FindBook("TIT");
			Assert.IsNotNull(titus, "Titus wasn't created.");
			Assert.AreEqual(7, titus.SectionsOS.Count, "Incorrect number of sections");
			IFdoOwningSequence<IStPara> titleParas = titus.TitleOA.ParagraphsOS;
			Assert.AreEqual(1, titleParas.Count, "Incorrect number of title Paragraphs");
			string title = ((IStTxtPara)titleParas[0]).Contents.Text;
			string sBrkChar = new string((char)0x2028, 1);
			Assert.AreEqual("Paul's Letter to" + sBrkChar + "TITUS", title.Trim(),
				"Incorrect book title");
			Assert.AreEqual("TITUS", titus.Name.VernacularDefaultWritingSystem.Text.ToUpper(),
				"Incorrect book name");
			Assert.AreEqual(@"57TIT.TEV updated to USFM JULY 30, 2002 epeters " +
				@"FOR PREDISTRIBUTION USE ONLY", titus.IdText);
			Assert.AreEqual(56001000, ((IScrSection)titus.SectionsOS[0]).VerseRefStart,
				"Introductory Section start reference");
			Assert.AreEqual(56001000, ((IScrSection)titus.SectionsOS[0]).VerseRefEnd,
				"Introductory Section end reference");
			Assert.AreEqual(56001000, ((IScrSection)titus.SectionsOS[1]).VerseRefStart,
				"Introductory Outline Section start reference");
			Assert.AreEqual(56001000, ((IScrSection)titus.SectionsOS[1]).VerseRefEnd,
				"Introductory Outline Section end reference");
			Assert.AreEqual(56001001, ((IScrSection)titus.SectionsOS[2]).VerseRefStart,
				"Section 2 start reference");
			Assert.AreEqual(56001004, ((IScrSection)titus.SectionsOS[2]).VerseRefEnd,
				"Section 2 end reference");
			Assert.AreEqual(56003012, ((IScrSection)titus.SectionsOS[6]).VerseRefStart,
				"Section 6 start reference");
			Assert.AreEqual(56003015, ((IScrSection)titus.SectionsOS[6]).VerseRefEnd,
				"Section 6 end reference");
			Assert.AreEqual(1, ((IScrSection)titus.SectionsOS[0]).HeadingOA.ParagraphsOS.Count,
				"First section should have a single para: 'Introduction'");
			IStTxtPara firstIntroHeadingPara = (IStTxtPara)titus.SectionsOS[0].HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Introduction", firstIntroHeadingPara.Contents.Text,
				"First intro section should be called 'Introduction'");

			// Verify the text in section[5], para[0], irun 0 (Tit 3:1)
			Assert.AreEqual(2, titus.SectionsOS[5].ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)titus.SectionsOS[5].ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(17, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "3", "Chapter Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 1, "1", "Verse Number", m_wsVern);

			// Verify the text in section[6], para[2], irun 0 (Tit 3:15)
			Assert.AreEqual(3, titus.SectionsOS[6].ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)titus.SectionsOS[6].ContentOA.ParagraphsOS[1]; //second para
			Assert.AreEqual(2, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "15", "Verse Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 1, "All who are with me send you greetings. Give our greetings " +
				"to our friends in the faith.", null, m_wsVern);

			// Verify the text in section[6], para[3], irun 0 (Tit 3:15)
			para = (IStTxtPara)titus.SectionsOS[6].ContentOA.ParagraphsOS[2]; //third para
			Assert.AreEqual(1, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "God's grace be with you all.", null, m_wsVern);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);

			// Check for verse bridge
			para = (IStTxtPara)titus.SectionsOS[3].ContentOA.ParagraphsOS[1];
			Assert.AreEqual("12-13", para.Contents.Text.Substring(274, 5));
		}
		#endregion

		#region SFImportJonahTOB
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the import of a basic SF project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SFImportJonahTOB()
		{
			using (UndoableUnitOfWorkHelper undoHelper = new UndoableUnitOfWorkHelper(
				m_actionHandler, "import settings"))
			{
				m_settings.ImportBookIntros = true;
				m_settings.ImportAnnotations = false;
				m_settings.ImportTranslation = true;
				m_settings.ImportBackTranslation = false;
				MakeSFImportTestSettings((m_settings as IScrImportSet));
				(m_settings as IScrImportSet).StartRef = (m_settings as IScrImportSet).EndRef = m_bcvRefJonah;
				undoHelper.RollBack = false; // keep the changes
			}

			// do the import
			DummyTeImportManager mgr = new DummyTeImportManager(Cache, m_styleSheet);
			mgr.SimulateAcceptAllBooks = true;
			mgr.CallImportWithUndoTask(m_settings);
			//TeSfmImporter.Import(m_settings, Cache, m_styleSheet,
			//    new DummyUndoImportManager(this), new TeImportNoUi());

			//verify the results
			IScrBook jonah = Cache.LangProject.TranslatedScriptureOA.FindBook("JON");
			Assert.IsNotNull(jonah, "Jonah wasn't created.");
			Assert.AreEqual(8, jonah.SectionsOS.Count, "Incorrect number of sections");
			IFdoOwningSequence<IStPara> titleParas = jonah.TitleOA.ParagraphsOS;
			Assert.AreEqual(1, titleParas.Count, "The title should consist of 1 para");
			string title = ((IStTxtPara)titleParas[0]).Contents.Text;
			Assert.AreEqual("JONAS", title.Trim(), "Incorrect book title");
			Assert.AreEqual("JONAS", jonah.Name.VernacularDefaultWritingSystem,
				"Incorrect book name");
			Assert.AreEqual(@"TOB32.SBF, CB 28.09.94", jonah.IdText);
			Assert.AreEqual(32001000, jonah.SectionsOS[0].VerseRefStart,
				"Introductory Section start reference");
			Assert.AreEqual(32001000, jonah.SectionsOS[0].VerseRefEnd,
				"Introductory Section end reference");
			Assert.AreEqual(32001000, jonah.SectionsOS[3].VerseRefStart,
				"Introductory Section start reference");
			Assert.AreEqual(32001000, jonah.SectionsOS[3].VerseRefEnd,
				"Introductory Section end reference");
			Assert.AreEqual(32001001, jonah.SectionsOS[4].VerseRefStart,
				"Section 4 start reference");
			Assert.AreEqual(32001016, jonah.SectionsOS[4].VerseRefEnd,
				"Section 4 end reference");
			Assert.AreEqual(32004001, jonah.SectionsOS[7].VerseRefStart,
				"Section 7 start reference");
			Assert.AreEqual(32004011, jonah.SectionsOS[7].VerseRefEnd,
				"Section 7 end reference");
			Assert.AreEqual(1, jonah.SectionsOS[0].HeadingOA.ParagraphsOS.Count,
				"Incorrect number of paras in first section heading.");
			IStTxtPara firstIntroHeadingPara =
				(IStTxtPara)jonah.SectionsOS[0].HeadingOA.ParagraphsOS[0];

			string sBrkChar = new string((char)0x2028, 1);
			Assert.AreEqual("Composition", firstIntroHeadingPara.Contents.Text,
				"First para of first intro section incorrect");

			// Verify the text in section[6], para[0], irun 0-2 (Jonah 3:1)
			Assert.AreEqual(1, jonah.SectionsOS[6].ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)jonah.SectionsOS[6].ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(21, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "3", "Chapter Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 1, "1", "Verse Number", m_wsVern);
			AssertEx.RunIsCorrect(para.Contents, 2, "La parole du SEIGNEUR s'adressa une seconde fois à Jonas: ",
				null, m_wsVern);

			// Check some text toward the end (in verse 4:11)
			para = (IStTxtPara)jonah.SectionsOS[7].ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Et moi", para.Contents.Text.Substring(1543, 8));
		}
		#endregion

		#region Helper functions for tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populate m_settings as if user had used the Import Wizard to choose a Paratext
		/// project and set up mappings.
		/// </summary>
		/// <param name="settings">Import settings</param>
		/// ------------------------------------------------------------------------------------
		static public void MakeParatextImportTestSettings(IScrImportSet settings)
		{
			settings.ImportTypeEnum = TypeOfImport.Paratext6;

			// Set project
			settings.ParatextScrProj = "TEV";

			// Set up the mappings
			DummyTeImporter.SetUpMappings(settings);
		}

		/// ------------------------------------------------------------------------------------
		/// Populate m_settings as if user had used the Import Wizard to choose a SF project
		/// and set up mappings appropriate for TOB data files.
		/// </summary>
		/// <param name="settings">Import settings</param>
		/// ------------------------------------------------------------------------------------
		static public void MakeSFImportTestSettings(IScrImportSet settings)
		{
			settings.ImportTypeEnum = TypeOfImport.Other;

			// add TOB files to the project
			settings.AddFile(DriveUtil.BootDrive + @"sf_scr~files2003.~TOB~\32JON.sfm", ImportDomain.Main, null, null);
			settings.AddFile(DriveUtil.BootDrive + @"sf_scr~files2003.~TOB~\123JN.sfm", ImportDomain.Main, null, null);
			// add TEV Philemon file to the project
			settings.AddFile(DriveUtil.BootDrive + @"~IWTEST~\TEV\58Phm.tev", ImportDomain.Main, null, null);

			// Set up the mappings
			DummyTeImporter.SetUpMappings(settings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="filesToImport"></param>
		/// <param name="startRef"></param>
		/// <param name="endRef"></param>
		/// ------------------------------------------------------------------------------------
		private void SetupAndImportData(string[] filesToImport, string startRef, string endRef)
		{
			SetupAndImportData(filesToImport, startRef, endRef, true, false, false, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Modifies the test fixture settings for OtherSF type of import.
		/// Runs the import, with a real TeSfmImporter
		/// </summary>
		/// <param name="filesToImport"></param>
		/// <param name="startRef"></param>
		/// <param name="endRef"></param>
		/// <param name="importTrans"></param>
		/// <param name="importBack"></param>
		/// <param name="importOther"></param>
		/// <param name="importIntro"></param>
		/// ------------------------------------------------------------------------------------
		///

		//   BRYAN, WHAT ABOUT USING THIS AS A TEMPLATE FOR OUR GENERIC LOAD SETTINGS?

		private void SetupAndImportData(string[] filesToImport, string startRef, string endRef,
			bool importTrans, bool importBack, bool importOther, bool importIntro)
		{
			// set up a IScrImportSet for our test
			m_settings.ImportTranslation = importTrans;
			m_settings.ImportBackTranslation = importBack;
			m_settings.ImportBookIntros = importIntro;
			m_settings.ImportAnnotations = importOther;

			ICmAnnotationDefn scrTranslatorAnnotationDef = null;
			foreach (ICmAnnotationDefn def in Cache.LangProject.ScriptureAnnotationDfns)
			{
				if (def.Guid == CmAnnotationDefnTags.kguidAnnTranslatorNote)
				{
					scrTranslatorAnnotationDef = def;
					break;
				}
			}

			m_settings.ImportTypeEnum = TypeOfImport.Other;
			(m_settings as IScrImportSet).SetMapping(MappingSet.Main, new ImportMappingInfo("|u", "|e", "Key Word"));
			(m_settings as IScrImportSet).SetMapping(MappingSet.Main, new ImportMappingInfo("|em{", "}", "Emphasis"));
			(m_settings as IScrImportSet).SetMapping(MappingSet.Main, new ImportMappingInfo(@"\ft", @"\fte", MarkerDomain.Footnote,
				ScrStyleNames.NormalFootnoteParagraph, null, null));
			(m_settings as IScrImportSet).SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btp", MarkerDomain.BackTrans,
				ScrStyleNames.NormalFootnoteParagraph, null, null));
			(m_settings as IScrImportSet).SetMapping(MappingSet.Main, new ImportMappingInfo(@"\btvt", MarkerDomain.BackTrans,
				"Default Paragraph Characters", null, null));
			(m_settings as IScrImportSet).SetMapping(MappingSet.Main, new ImportMappingInfo(@"\np", MarkerDomain.Note,
				ScrStyleNames.Remark, null, scrTranslatorAnnotationDef));

			// add files to the import settings
			foreach(string file in filesToImport)
				(m_settings as IScrImportSet).AddFile(file, ImportDomain.Main, null, null);

			// set the start and end books
			(m_settings as IScrImportSet).StartRef = new BCVRef(ScrReference.BookToNumber(startRef), 0, 0);
			(m_settings as IScrImportSet).EndRef = new BCVRef(ScrReference.BookToNumber(endRef), 0, 0);

			// now do the import, with a real TeSfmImporter
			TeSfmImporter.Import(m_settings, Cache, m_styleSheet,
				new DummyUndoImportManager(this), new TeImportNoUi());
		}
		#endregion
	}
	#endregion
}
