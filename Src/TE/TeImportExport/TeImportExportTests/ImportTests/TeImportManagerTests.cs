// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeImportTest.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

using NUnit.Framework;
using NMock;
using NMock.Constraints;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.ScrImportComponents;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.FDOTests;
using SILUBS.SharedScrUtils;
using System.Windows.Forms;

namespace SIL.FieldWorks.TE.ImportTests
{
	#region class SegmentInfo
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A little class to facilitate testing
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class SegmentInfo
	{
		internal string m_sMarker;
		internal string m_sText;
		internal ImportDomain m_domain;
		internal BCVRef m_ref;
		internal int m_ws;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SegmentInfo"/> class.
		/// </summary>
		/// <param name="sMarker">The segment marker.</param>
		/// <param name="sText">The segment text.</param>
		/// <param name="domain">The import domain.</param>
		/// <param name="reference">The current Scripture reference.</param>
		/// ------------------------------------------------------------------------------------
		internal SegmentInfo(string sMarker, string sText, ImportDomain domain,
			BCVRef reference) : this (sMarker, sText, domain, reference, -1)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:SegmentInfo"/> class.
		/// </summary>
		/// <param name="sMarker">The segment marker.</param>
		/// <param name="sText">The segment text.</param>
		/// <param name="domain">The import domain.</param>
		/// <param name="reference">The current Scripture reference.</param>
		/// <param name="ws">The writing system (-1 to indicate the default WS).</param>
		/// ------------------------------------------------------------------------------------
		internal SegmentInfo(string sMarker, string sText, ImportDomain domain,
			BCVRef reference, int ws)
		{
			m_sMarker = sMarker;
			m_sText = sText;
			m_domain = domain;
			m_ref = reference;
			m_ws = ws;
		}
	}
	#endregion

	#region class MockScrObjWrapper
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A Scripture Object wrapper that passes a sequence of segments/events based on a
	/// predetermined list given to it and (optionally) causes the import to be cancelled when
	/// called the n+1th time.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class MockScrObjWrapper : ScrObjWrapper
	{
		private List<SegmentInfo> m_segmentList;
		private int m_curSeg;
		private BCVRef m_ref;
		private MockTeImporter m_importer;
		private int m_wsOverride;

		public static bool s_fSimulateCancel = true;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MockScrObjWrapper"/> class.
		/// </summary>
		/// <param name="importer">The importer.</param>
		/// <param name="segmentList">The segment list.</param>
		/// ------------------------------------------------------------------------------------
		public MockScrObjWrapper(MockTeImporter importer, List<SegmentInfo> segmentList)
			: base()
		{
			m_importer = importer;
			m_segmentList = segmentList;
			m_curSeg = 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the next segment.
		/// </summary>
		/// <param name="sText">Set to the text of the current segment</param>
		/// <param name="sMarker">Set to the marker of the current segment tag</param>
		/// <param name="domain">Set to the domain of the stream being processed</param>
		/// <returns>True</returns>
		/// ------------------------------------------------------------------------------------
		public override bool GetNextSegment(out string sText, out string sMarker,
			out ImportDomain domain)
		{
			if (m_curSeg == m_segmentList.Count)
			{
				sText = string.Empty;
				sMarker = string.Empty;
				domain = ImportDomain.Main;
				m_wsOverride = -1;
				if (!s_fSimulateCancel)
					return false;

				m_importer.Cancel();
				return true;
			}

			SegmentInfo info = m_segmentList[m_curSeg];
			sText = info.m_sText;
			sMarker = info.m_sMarker;
			domain = info.m_domain;
			m_ref = info.m_ref;
			m_wsOverride = info.m_ws;

			m_curSeg++;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the filename from which the current segment was read.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string CurrentFileName
		{
			get
			{
				return "blah";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the line number from which the current segment was read
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int CurrentLineNumber
		{
			get
			{
				return -1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the first reference of the current segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override BCVRef SegmentFirstRef
		{
			get
			{
				return m_ref;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the last reference of the current segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override BCVRef SegmentLastRef
		{
			get
			{
				return m_ref;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the current writing system (always the default)
		/// </summary>
		/// <param name="defaultWs">The default to use</param>
		/// ------------------------------------------------------------------------------------
		public override int CurrentWs(int defaultWs)
		{
			return (m_wsOverride > -1) ? m_wsOverride : defaultWs;
		}
	}
	#endregion

	#region class MockTeImporter
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class MockTeImporter : TeSfmImporter
	{
		private List<SegmentInfo> m_segmentList;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MockTeImporter"/> class.
		/// </summary>
		/// <param name="settings">Import settings object (filled in by wizard)</param>
		/// <param name="cache">The cache used to import to and get misc. info. from.</param>
		/// <param name="styleSheet">Stylesheet from which to get scripture styles.</param>
		/// <param name="undoManager">The undo import manager(which is responsible for creating
		/// and maintaining the saved version (archive) of original books being overwritten).</param>
		/// <param name="importCallbacks">UI callbacks</param>
		/// ------------------------------------------------------------------------------------
		public MockTeImporter(IScrImportSet settings, FdoCache cache,
			FwStyleSheet styleSheet, UndoImportManager undoManager, TeImportUi importCallbacks)
			: base(settings, cache, styleSheet, undoManager, importCallbacks)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call this static method to Import Scripture
		/// </summary>
		/// <param name="settings">Import settings object (filled in by wizard)</param>
		/// <param name="cache">The cache used to import to and get misc. info. from.</param>
		/// <param name="styleSheet">Stylesheet from which to get scripture styles.</param>
		/// <param name="undoManager">The undo import manager (which is responsible for creating
		/// and maintaining the saved version (archive) of original books being overwritten and
		/// maintaining the book filter).</param>
		/// <param name="importCallbacks">The import callbacks.</param>
		/// <param name="segmentList">The segment list.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ScrReference Import(IScrImportSet settings, FdoCache cache,
			FwStyleSheet styleSheet, UndoImportManager undoManager,
			TeImportUi importCallbacks, List<SegmentInfo> segmentList)
		{
			using (MockTeImporter importer = new MockTeImporter(settings, cache, styleSheet, undoManager,
				importCallbacks))
			{
				importer.m_segmentList = segmentList;
				importer.Import();
				return ReflectionHelper.GetField(importer, "m_firstImportedRef") as ScrReference;
			}	// Dispose() releases any hold on ICU character properties.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the scripture object wrapper.
		/// </summary>
		/// <remarks>Virtual for testing override purposes.</remarks>
		/// ------------------------------------------------------------------------------------
		protected override void InitScriptureObject()
		{
			SOWrapper = new MockScrObjWrapper(this, m_segmentList);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cancels this import.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void Cancel()
		{
			CheckDisposed();

			((DummyTeImportUi)m_importCallbacks).SimulateCancel();
		}
	}
	#endregion

	#region class DummyTeImportUi
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyTeImportUi : TeImportNoUi
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyTeImportUi"/> class.
		/// </summary>
		/// <param name="progressDlg">The progress DLG.</param>
		/// ------------------------------------------------------------------------------------
		public DummyTeImportUi(ProgressDialogWithTask progressDlg)
			: base()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates pressing the cancel button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateCancel()
		{
			m_fCancel = true;
		}
	}
	#endregion

	#region class DummyImportedBooks
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyImportedBooks : ImportedBooks
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyImportedBooks"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="booksImported">The books that have been imported.</param>
		/// <param name="backupVersion">where to store stuff overwritten or merged.</param>
		/// ------------------------------------------------------------------------------------
		public DummyImportedBooks(FdoCache cache, IScrDraft booksImported,
			IScrDraft backupVersion) : base(cache, booksImported, backupVersion)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overwrites the current version of all books with the imported versions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AcceptAllImportedBooks()
		{
			Assert.IsNotNull(m_booksImported);
			foreach (IScrBook importedBook in m_booksImported.BooksOS)
			{
				IScrBook currBook = m_scr.FindBook(importedBook.CanonicalNum);
				if (currBook == null)
					m_scr.CopyBookToCurrent(importedBook);
				else
					OverwriteBook(currBook, importedBook);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the imported versions of all books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DeleteAllImportedBooks()
		{
			Assert.IsNotNull(m_booksImported);
			foreach (IScrBook importedBook in m_booksImported.BooksOS)
				importedBook.DeleteUnderlyingObject();
		}
	}
	#endregion

	#region class DummyTeImportManager
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// An import manager.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyTeImportManager : TeImportManager
	{
		#region Member data
		private List<SegmentInfo> m_segmentList;
		private IScripture m_scr;
		private Set<int> m_originalDrafts;
		internal int m_cDisplayImportedBooksDlgCalled = 0;
		private bool m_fSimulateAcceptAllBooks = false;
		//private bool m_fSimulateDeleteAllBooks = false;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyTeImportManager"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// ------------------------------------------------------------------------------------
		public DummyTeImportManager(FdoCache cache, FwStyleSheet styleSheet) :
			base(cache, styleSheet)
		{
			m_scr = cache.LangProject.TranslatedScriptureOA;
			m_originalDrafts = new Set<int>(m_scr.ArchivedDraftsOC.HvoArray);
		}
		#endregion

		#region Internal methods/properties for testing
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Import scripture and embed it in a Undo task so that it is undoable.
		/// </summary>
		/// <param name="importSettings">Import settings</param>
		/// <param name="segmentList">The segment list.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal void CallImportWithUndoTask(ScrImportSet importSettings,
			List<SegmentInfo> segmentList)
		{
			m_segmentList = segmentList;
			CompleteImport(ImportWithUndoTask(importSettings, false, string.Empty));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides DisplayImportedBooksDlg for testing.
		/// </summary>
		/// <param name="backupSavedVersion">The saved version for backups of any overwritten
		/// books.</param>
		/// ------------------------------------------------------------------------------------
		protected override void DisplayImportedBooksDlg(IScrDraft backupSavedVersion)
		{
			m_cDisplayImportedBooksDlgCalled++;
			if (m_fSimulateAcceptAllBooks)
			{
				DummyImportedBooks dlg = new DummyImportedBooks(m_cache, ImportedSavedVersion, backupSavedVersion);
				dlg.AcceptAllImportedBooks();
			}
			//else if (m_fSimulateDeleteAllBooks)
			//{
			//    DummyImportedBooks dlg = new DummyImportedBooks(m_cache, ImportedSavedVersion, backupSavedVersion);
			//    dlg.DeleteAllImportedBooks();
			//}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the set of HVOs of any new saved versions (i.e., those created as part of
		/// import).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal Set<int> NewSavedVersions
		{
			get
			{
				// This is a kludgy way to get the new saved version since CallImportWithUndoTask
				// does not save it.
				return new Set<int>(m_scr.ArchivedDraftsOC.HvoArray).Difference(m_originalDrafts);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the undo manager for testing.
		/// </summary>
		/// <value>T.</value>
		/// ------------------------------------------------------------------------------------
		public new UndoImportManager UndoManager
		{
			get { return base.UndoManager; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether to simulate accepting all books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool SimulateAcceptAllBooks
		{
			set
			{
				//if (value && m_fSimulateDeleteAllBooks)
				//    throw new Exception("SimulateAcceptAllBooks and SimulateDeleteAllBooks cannot both be set to true.");
				m_fSimulateAcceptAllBooks = value;
			}
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Sets a value indicating whether to simulate deleting all books.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//internal bool SimulateDeleteAllBooks
		//{
		//    set
		//    {
		//        if (value && m_fSimulateAcceptAllBooks)
		//            throw new Exception("SimulateAcceptAllBooks and SimulateDeleteAllBooks cannot both be set to true.");
		//        m_fSimulateDeleteAllBooks = value;
		//    }
		//}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a TeImportUi object.
		/// </summary>
		/// <param name="progressDialog">The progress dialog.</param>
		/// <returns>A TeImportUi object</returns>
		/// <remarks>Can be overriden in tests</remarks>
		/// ------------------------------------------------------------------------------------
		protected override TeImportUi CreateTeImportUi(ProgressDialogWithTask progressDialog)
		{
			return new DummyTeImportUi(progressDialog);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Imports using the specified import settings.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The paramaters: import settings, undo action, and book
		/// merger, TeImportUi.</param>
		/// <returns>The Scripture reference of the first thing imported</returns>
		/// ------------------------------------------------------------------------------------
		protected override object ImportSf(IAdvInd4 progressDlg, object[] parameters)
		{
			Debug.Assert(parameters.Length == 3);
			IScrImportSet importSettings = (IScrImportSet)parameters[0];
			UndoImportManager undoManager = (UndoImportManager)parameters[1];
			TeImportUi importUi = (TeImportUi)parameters[2];

			MockTeImporter.Import(importSettings, m_cache, m_styleSheet,
				undoManager, importUi, m_segmentList);
			return ScrReference.Empty;
		}
		#endregion
	}
	#endregion

	#region class TeImportTestsWithDb
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// TeImportTestDb tests TeImport using a real DB-based cache (TestLangProj)
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[TestFixture]
	public class TeImportTestsWithDb : BaseTest
	{
		#region Member variables
		private DummyTeImportManager m_importMgr;
		private FdoCache m_cache;
		private IScripture m_scr;
		private string m_sSavepoint;
		private BCVRef m_titus;
		private FwStyleSheet m_styleSheet;
		private ScrImportSet m_settings;
		#endregion

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create temp registry settings and unzipped files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			m_cache = FdoCache.Create("TestLangProj");
			// Make sure we don't call InstallLanguage during tests.
			m_cache.LanguageWritingSystemFactoryAccessor.BypassInstall = true;
			m_scr = m_cache.LangProject.TranslatedScriptureOA;
			m_styleSheet = new FwStyleSheet();
			m_styleSheet.Init(m_cache, m_scr.Hvo, (int)Scripture.ScriptureTags.kflidStyles);

			if (!m_cache.DatabaseAccessor.IsTransactionOpen())
				m_cache.DatabaseAccessor.BeginTrans();

			// By default, use auto-generated footnote markers for import tests.
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker;

			m_titus = new BCVRef(56001001);
			m_settings = new ScrImportSet();
			m_scr.ImportSettingsOC.Add(m_settings);
			//m_scr.DefaultImportSettings = m_settings;
			DummyTeImporter.MakeSFImportTestSettings(m_settings);
		}

		#region IDisposable override
		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLine("Start of Dispose");
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_cache != null)
				{
					if (m_cache.DatabaseAccessor.IsTransactionOpen())
						m_cache.DatabaseAccessor.RollbackTrans();
					m_cache.Dispose();
				}
			}
			m_cache = null;
			m_importMgr = null;
			m_scr = null;
			m_styleSheet = null; // FwStyleSheet should implement IDisposable.
			m_settings = null;
			Debug.WriteLine("Before calling base.Dispose");

			base.Dispose(disposing);
			Debug.WriteLine("End of Dispose");
		}
		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the importer and set up Undo action
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void InitTest()
		{
			CheckDisposed();

			m_cache.DatabaseAccessor.SetSavePoint(out m_sSavepoint);

			MockScrObjWrapper.s_fSimulateCancel = true;
			using (new SuppressSubTasks(m_cache))
			{
				m_settings.StartRef = m_titus;
				m_settings.EndRef = m_titus;
				m_settings.ImportTranslation = true;
				m_settings.ImportBackTranslation = false;
				m_settings.ImportBookIntros = true;
				m_settings.ImportAnnotations = false;
			}
			m_importMgr = new DummyTeImportManager(m_cache, m_styleSheet);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Rollback any DB changes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void DestroyTest()
		{
			CheckDisposed();

			while (m_cache.CanUndo)
				m_cache.Undo();
			m_cache.DatabaseAccessor.RollbackSavePoint(m_sSavepoint);
			m_cache.ClearAllData();
			m_importMgr = null;
		}
		#endregion

		#region Stopping import of new book tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that canceling the import of a new book causes it to be discarded.
		/// </summary>
		/// <remarks>Jira numbers: TE-4830, TE-4535</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CancelDiscardsNewBook()
		{
			CheckDisposed();

			int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

			m_settings.ImportAnnotations = true;
			Assert.IsNull(m_scr.FindBook(1), "This test is invalid if Genesis exists in test DB.");
			// Make sure there are no notes for Genesis
			FdoOwningSequence<IScrScriptureNote> notes = m_scr.BookAnnotationsOS[0].NotesOS;
			int cNotesOrig  = notes.Count;
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			// process an \id segment to import Genesis (a non-existing book)
			List<SegmentInfo> al = new List<SegmentInfo>(4);
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(1, 0, 0)));
			al.Add(new SegmentInfo(@"\rem", "This annotation should get deleted (TE-4535)", ImportDomain.Main, new BCVRef(1, 0, 0)));
			al.Add(new SegmentInfo(@"\mt", "Geneseo", ImportDomain.Main, new BCVRef(1, 0, 0)));

			m_importMgr.CallImportWithUndoTask(m_settings, al);

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "No new ScrDrafts should have been created");
			Assert.AreEqual(origActCount, m_cache.ActionHandlerAccessor.UndoableActionCount,
				"Should have undone the creation of the book");
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.IsNull(m_scr.FindBook(1), "Partially-imported Genesis should have been discarded.");
			Assert.AreEqual(cNotesOrig, notes.Count);
			Assert.AreEqual(0, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that canceling the import of a new book causes it to be discarded after
		/// successful import of an existing book.
		/// </summary>
		/// <remarks>Jira number is TE-4830</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CancelDiscardsNewBook_AfterImportOfOneExistingBook()
		{
			CheckDisposed();

			int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude does not exist in test DB.");
			Assert.IsNull(m_scr.FindBook(66), "This test is invalid if Revelation exists in test DB.");

			int hvoJudeOrig = jude.Hvo;
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;
			jude = null;

			List<SegmentInfo> al = new List<SegmentInfo>(2);
			// process a \id segment to import Jude (an existing book)
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(65, 0, 0)));
			// process a \id segment to import Rev (a non-existing book)
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(66, 0, 0)));
			m_importMgr.SimulateAcceptAllBooks = true;
			m_importMgr.CallImportWithUndoTask(m_settings, al);

			Assert.AreEqual(origActCount + 5, m_cache.ActionHandlerAccessor.UndoableActionCount,
				"Should have 5 extra undo actions (create backup, create imported version, create JUD, move orig JUD to backup, copy imported JUD to current) after Undo cancels incomplete book");
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			IScrBook curJude = m_scr.FindBook(65);
			Assert.AreNotEqual(hvoJudeOrig, curJude.Hvo, "The original Jude should have been overwritten.");
			Assert.AreEqual(curJude.Hvo, m_scr.ScriptureBooksOS.HvoArray[2]);
			Assert.IsNull(m_scr.FindBook(66), "Partially-imported Revelation should have been discarded.");
			Assert.AreEqual(2, m_importMgr.NewSavedVersions.Count, "We should have an imported version and a backup saved version.");
			IScrDraft newSv = m_importMgr.UndoManager.ImportedSavedVersion;
			Assert.AreEqual(1, newSv.BooksOS.Count, "Only one new book should have survived");
			IScrBook newJude = newSv.BooksOS[0];
			Assert.AreEqual(65, newJude.CanonicalNum);
			Assert.AreEqual(ScrDraftType.ImportedVersion, newSv.Type);
			Assert.IsNotNull(m_importMgr.UndoManager.BackupVersion);
			Assert.AreEqual(1, m_importMgr.UndoManager.BackupVersion.BooksOS.Count);
			Assert.AreEqual(1, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}
		#endregion

		//#region Tests of user pressing Delete in Imported Books dialog
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Test that importing a single (existing) book and then choosing Delete in the
		///// Imported Books dialog will remove the Undo Import action, since the next result is
		///// that no action will have been taken.
		///// </summary>
		///// <remarks>Jira number is TE-7489</remarks>
		///// ------------------------------------------------------------------------------------
		//[Test]
		//public void ImportAndDeleteBookRemovesUndoAction()
		//{
		//    CheckDisposed();

		//    int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

		//    IScrBook jude = m_scr.FindBook(65);
		//    Assert.IsNotNull(jude, "This test is invalid if Jude does not exist in test DB.");

		//    int hvoJudeOrig = jude.Hvo;
		//    int cBooksOrig = m_scr.ScriptureBooksOS.Count;
		//    jude = null;

		//    MockScrObjWrapper.s_fSimulateCancel = false;
		//    List<SegmentInfo> al = new List<SegmentInfo>(2);
		//    // process \id segment to import Jude (an existing book)
		//    al.Add(new SegmentInfo(@"\id", string.Empty, ImportDomain.Main, new BCVRef(65, 0, 0)));
		//    al.Add(new SegmentInfo(@"\c", string.Empty, ImportDomain.Main, new BCVRef(65, 1, 1)));
		//    al.Add(new SegmentInfo(@"\v", "I'm Jude and you're not.", ImportDomain.Main, new BCVRef(65, 1, 1)));
		//    m_importMgr.SimulateDeleteAllBooks = true;
		//    m_importMgr.CallImportWithUndoTask(m_settings, al);

		//    Assert.AreEqual(origActCount, m_cache.ActionHandlerAccessor.UndoableActionCount,
		//        "Should have no extra undo actions");
		//    Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
		//    jude = m_scr.FindBook(65);
		//    Assert.AreEqual(hvoJudeOrig, jude.Hvo, "The original Jude should not have been overwritten.");
		//    Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count);
		//}
		//#endregion

		#region Restore after cancel tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that canceling the import of an existing book causes the original book to be
		/// restored from the saved version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CancelRestoresOriginal_AfterImportingOneCompleteBook()
		{
			CheckDisposed();

			int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

			IScrBook phm = m_scr.FindBook(57);
			IScrBook jude = m_scr.FindBook(65);
			int hvoJudeOrig = jude.Hvo;
			int ownOrdJudeOrig = jude.OwnOrd;
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;
			jude = null;

			// process a \id segment to import Philemon (an existing book)
			List<SegmentInfo> al = new List<SegmentInfo>(2);
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(57, 0, 0)));
			// process a \id segment to import Jude (also an existing book)
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(65, 0, 0)));

			m_importMgr.SimulateAcceptAllBooks = true;
			m_importMgr.CallImportWithUndoTask(m_settings, al);

			Assert.AreEqual(origActCount + 5, m_cache.ActionHandlerAccessor.UndoableActionCount,
				"Should have 5 extra undo actions (create backup, create imported version, create PHM, move orig PHM to backup, copy imported PHM to current) after Undo cancels incomplete book");

			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(2, m_importMgr.NewSavedVersions.Count, "We should have an imported version and a backup saved version.");
			Assert.AreEqual(1, m_importMgr.UndoManager.ImportedSavedVersion.BooksOS.Count);
			Assert.AreEqual(57, m_importMgr.UndoManager.ImportedSavedVersion.BooksOS[0].CanonicalNum);
			Assert.AreEqual(1, m_importMgr.UndoManager.BackupVersion.BooksOS.Count);
			Assert.AreEqual(57, m_importMgr.UndoManager.BackupVersion.BooksOS[0].CanonicalNum);
			IScrBook restoredJude = m_scr.FindBook(65);
			Assert.AreEqual(hvoJudeOrig, restoredJude.Hvo);
			Assert.AreEqual(restoredJude.Hvo, m_scr.ScriptureBooksOS.HvoArray[2]);
			Assert.AreEqual(1, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that canceling the import of the first book causes the original book to be
		/// restored from the archive. And the archive (which is then empty) should be removed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CancelRestoresOriginal_FirstBook()
		{
			CheckDisposed();

			int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

			IScrBook phm = m_scr.FindBook(57);
			Assert.AreEqual(phm.Hvo, m_scr.ScriptureBooksOS.HvoArray[0],
				"This test is invalid if Philemon isn't the first book in Scripture in the test DB.");
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			List<SegmentInfo> al = new List<SegmentInfo>(1);
			// process a \id segment to import an existing a book
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(57, 0, 0)));

			m_importMgr.CallImportWithUndoTask(m_settings, al);

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "New ScrDrafts should have been purged");
			Assert.AreEqual(origActCount, m_cache.ActionHandlerAccessor.UndoableActionCount,
				"Should have undone the creation of the book");
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(phm.Hvo, m_scr.FindBook(57).Hvo);
			Assert.AreEqual(57, m_scr.ScriptureBooksOS[0].CanonicalNum);
			Assert.AreEqual(0, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that if a BT-only import aborts because of Unable to Import BT exception,
		/// a copy of the original book is saved in the archive and the partial BT is left on
		/// the current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BtAbortSavesOriginal()
		{
			CheckDisposed();

			m_settings.ImportTranslation = false;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");
			int hvoJudeOrig = jude.Hvo;
			StTxtPara scrHead1Para1 = GetFirstScriptureSectionHeadParaInBook(jude);
			Assert.IsNotNull(scrHead1Para1, "This test is invalid if we can't find a normal Scripture section for Jude 1:1.");
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			List<SegmentInfo> al = new List<SegmentInfo>(3);
			// process a \id segment to import the BT for an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD Back Trans", ImportDomain.BackTrans, new BCVRef(65, 0, 0)));
			al.Add(new SegmentInfo(@"\s", "Section head BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1)));
			al.Add(new SegmentInfo(@"\s2", "Minor Section head BT (no match in vern)", ImportDomain.BackTrans, new BCVRef(65, 1, 1)));

			m_importMgr.CallImportWithUndoTask(m_settings, al);

			Assert.AreEqual(1, m_importMgr.NewSavedVersions.Count, "Exactly one new version should have been created");
			Assert.AreEqual(origActCount + 2, m_cache.ActionHandlerAccessor.UndoableActionCount,
				"Should have two extra undo actions (create backup, partial import of BT into existing book) after Undo cancels incomplete book");
			Assert.AreEqual("&Undo Import", m_cache.UndoText);
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(hvoJudeOrig, m_scr.FindBook(65).Hvo);
			Assert.AreEqual(1, scrHead1Para1.TranslationsOC.Count);
			foreach (CmTranslation trans in scrHead1Para1.TranslationsOC)
				Assert.AreEqual("Section head BT", trans.Translation.AnalysisDefaultWritingSystem.Text);

			Assert.IsNull(m_importMgr.UndoManager.ImportedSavedVersion);
			IScrDraft backupSv = m_importMgr.UndoManager.BackupVersion;
			Assert.AreEqual(1, backupSv.BooksOS.Count);
			Assert.AreEqual(65, backupSv.BooksOS[0].CanonicalNum);

			// Test ability to Undo and get back to where we were.
			Assert.IsTrue(m_cache.CanUndo);
			m_cache.Undo();
			Assert.AreEqual(origActCount, m_cache.ActionHandlerAccessor.UndoableActionCount);
			Assert.IsNull(m_cache.UndoText);
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			// ENHANCE: This is not really the ideal way to undo the import of a BT because the original
			// book gets replaced by the backup copy, thus blowing away any interlinear annotations.
			// Would be nice to be able to copy the BT from the backup version to the current version.
			// Besides it possibly being slower (and harder) I'm not sure what other pitfalls might exist.
			jude = m_scr.FindBook(65);
			Assert.AreNotEqual(hvoJudeOrig, jude.Hvo);
			scrHead1Para1 = GetFirstScriptureSectionHeadParaInBook(jude);
			Assert.AreEqual(0, scrHead1Para1.TranslationsOC.Count);

			// Backed up version should be gone.
			Assert.IsFalse(backupSv.IsRealObject);
			Assert.AreEqual(0, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that if an interleaved BT import aborts because of an Unable to Import BT
		/// exception, the partial (vernacular) book is discarded.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BtInterleavedAbortRollsBack()
		{
			CheckDisposed();

			m_settings.ImportTranslation = true;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;
			int wsEn = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
			Assert.Greater(wsEn, 0, "Couldn't find Id of English WS in test DB.");

			int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			List<SegmentInfo> al = new List<SegmentInfo>(3);
			// process a \id segment to import the BT for an existing a book
			al.Add(new SegmentInfo(@"\id", "GEN interleaved", ImportDomain.Main, new BCVRef(1, 0, 0)));
			al.Add(new SegmentInfo(@"\mt", "", ImportDomain.Main, new BCVRef(1, 0, 0)));
			al.Add(new SegmentInfo(@"\btmt", "Genesis", ImportDomain.Main, new BCVRef(1, 0, 0), wsEn));
			al.Add(new SegmentInfo(@"\c", "1", ImportDomain.Main, new BCVRef(1, 1, 0)));

			m_importMgr.CallImportWithUndoTask(m_settings, al);

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "No new version should have been created");
			Assert.AreEqual(origActCount, m_cache.ActionHandlerAccessor.UndoableActionCount);
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.IsNull(m_scr.FindBook(1));
			Assert.IsNull(m_importMgr.UndoManager.ImportedSavedVersion);
			Assert.IsNull(m_importMgr.UndoManager.BackupVersion);
			Assert.AreEqual(0, m_importMgr.m_cDisplayImportedBooksDlgCalled);
			Assert.IsFalse(m_cache.CanRedo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that if a BT-only import aborts because of an "Unable to Import BT" exception,
		/// any previously successfully imported vernacular book is displayed properly in the
		/// Imported Books dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UnableToImportBtAfterSuccessfulBookImport()
		{
			CheckDisposed();

			m_settings.ImportTranslation = true;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

			Assert.IsNull(m_scr.FindBook(1), "This test is invalid if Genesis is in the test DB.");
			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");
			int hvoJudeOrig = jude.Hvo;
			StTxtPara scrHead1Para1 = GetFirstScriptureSectionHeadParaInBook(jude);
			Assert.IsNotNull(scrHead1Para1, "This test is invalid if we can't find a normal Scripture section for Jude 1:1.");
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			List<SegmentInfo> al = new List<SegmentInfo>(7);
			// process an \id segment to import the BT for an existing a book
			al.Add(new SegmentInfo(@"\id", "GEN", ImportDomain.Main, new BCVRef(1, 0, 0)));
			al.Add(new SegmentInfo(@"\c", string.Empty, ImportDomain.Main, new BCVRef(1, 1, 1)));
			al.Add(new SegmentInfo(@"\s", "The Creation", ImportDomain.Main, new BCVRef(1, 1, 1)));
			al.Add(new SegmentInfo(@"\v", "In the beginning...", ImportDomain.Main, new BCVRef(1, 1, 1)));
			// process an \id segment to import the BT for an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD Back Trans", ImportDomain.BackTrans, new BCVRef(65, 0, 0)));
			al.Add(new SegmentInfo(@"\s", "Section head BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1)));
			al.Add(new SegmentInfo(@"\s2", "Minor Section head BT (no match in vern)", ImportDomain.BackTrans, new BCVRef(65, 1, 1)));

			m_importMgr.SimulateAcceptAllBooks = true;
			m_importMgr.CallImportWithUndoTask(m_settings, al);

			Assert.AreEqual(2, m_importMgr.NewSavedVersions.Count, "Exactly two new versions should have been created");
			Assert.AreEqual(origActCount + 5, m_cache.ActionHandlerAccessor.UndoableActionCount,
				"Should have five extra undo actions (create backup, create imported version, import Genesis, Backup of JUD, Import of JUD partial BT) after Undo cancels incomplete book");
			Assert.AreEqual("&Undo Import", m_cache.UndoText);
			Assert.AreEqual(cBooksOrig + 1, m_scr.ScriptureBooksOS.Count);
			Assert.IsNotNull(m_scr.FindBook(1));
			Assert.AreEqual(hvoJudeOrig, m_scr.FindBook(65).Hvo);

			IScrDraft importedSv = m_importMgr.UndoManager.ImportedSavedVersion;
			Assert.IsNotNull(importedSv);
			Assert.AreEqual(1, importedSv.BooksOS.Count);
			Assert.AreEqual(1, importedSv.BooksOS[0].CanonicalNum);
			IScrDraft backupSv = m_importMgr.UndoManager.BackupVersion;
			Assert.IsNotNull(backupSv);
			Assert.AreEqual(1, backupSv.BooksOS.Count);
			Assert.AreEqual(65, backupSv.BooksOS[0].CanonicalNum);

			Assert.AreEqual(1, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test BT-only import in two different writing systems. Only one backup copy of the
		/// original book is saved in the archive and the partial BT is left on
		/// the current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BtUndoPhmAfterImportingBtJudInOtherWs()
		{
			CheckDisposed();

			m_settings.ImportTranslation = false;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

			IScrBook philemon = m_scr.FindBook(57);
			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(philemon, "This test is invalid if Philemon isn't in the test DB.");
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");

			StTxtPara scrHead1Para1 = null;
			int iScrHead1 = 0;
			foreach (ScrSection section in jude.SectionsOS)
			{
				if (!section.IsIntro)
				{
					scrHead1Para1 = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
					break;
				}
				iScrHead1++;
			}
			Assert.IsNotNull(scrHead1Para1, "This test is invalid if there is no Scripture section in Jude in the test DB.");
			string scrHead1Para1TextOrig = scrHead1Para1.Contents.Text;
			int scrHead1Para1OrigTransCount = scrHead1Para1.TranslationsOC.Count;
			Assert.AreEqual(0, scrHead1Para1OrigTransCount, "This test is invalid if the first paragraph of the first Scripture section head in Jude has a backtranslation in the test DB.");

			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			int wsEn = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
			Assert.Greater(wsEn, 0, "Couldn't find Id of English WS in test DB.");
			int wsEs = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("es");
			Assert.Greater(wsEs, 0, "Couldn't find Id of Spanish WS in test DB.");

			List<SegmentInfo> al = new List<SegmentInfo>(3);
			// process a \id segment to import an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD English Back Trans", ImportDomain.BackTrans, new BCVRef(65, 0, 0), wsEn));
			al.Add(new SegmentInfo(@"\s", "English Section head BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1), wsEn));
			al.Add(new SegmentInfo(@"\id", "PHM Spanish Back Trans", ImportDomain.BackTrans, new BCVRef(57, 0, 0), wsEs));
			al.Add(new SegmentInfo(@"\s", "Spanish Section head BT", ImportDomain.BackTrans, new BCVRef(57, 1, 1), wsEs));

			m_importMgr.CallImportWithUndoTask(m_settings, al);

			Assert.AreEqual(1, m_importMgr.NewSavedVersions.Count, "We should only have a backup saved version, no imported version.");
			Assert.AreEqual(origActCount + 3, m_cache.ActionHandlerAccessor.UndoableActionCount,
				"Should have 3 extra undo actions (create backup, create imported version & BT of JUD, create BT of PHM).");
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(philemon.Hvo, m_scr.FindBook(57).Hvo, "Imported BT should not replace current version");
			Assert.AreEqual(jude.Hvo, m_scr.FindBook(65).Hvo, "Imported BT should not replace current version");

			Assert.AreEqual(0, m_importMgr.m_cDisplayImportedBooksDlgCalled);

			Assert.AreEqual(2, m_importMgr.UndoManager.BackupVersion.BooksOS.Count);
			Assert.AreEqual(57, m_importMgr.UndoManager.BackupVersion.BooksOS[0].CanonicalNum);
			Assert.AreEqual(65, m_importMgr.UndoManager.BackupVersion.BooksOS[1].CanonicalNum);
			Assert.IsNull(m_importMgr.UndoManager.ImportedSavedVersion);
			Assert.AreEqual("&Undo Import", m_cache.UndoText);
			Assert.IsTrue(m_cache.CanUndo);

			m_cache.Undo();

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "The backup saved version should be gone.");

			IScrBook judeAfterUndo = m_scr.FindBook(65);
			Assert.AreNotEqual(jude.Hvo, judeAfterUndo.Hvo, "Undo replaces current version with saved backup");

			StTxtPara undoneScrHead1Para1 = ((StTxtPara)judeAfterUndo.SectionsOS[iScrHead1].HeadingOA.ParagraphsOS[0]);
			Assert.AreEqual(scrHead1Para1TextOrig, undoneScrHead1Para1.Contents.Text);
			Assert.AreEqual(scrHead1Para1OrigTransCount, undoneScrHead1Para1.TranslationsOC.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that an error during import causes the original book to be restored from the
		/// saved version. And the saved version (which is then empty) should be removed.
		/// (TE-5374)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ErrorRestoresOriginal()
		{
			CheckDisposed();

			int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

			IScrBook phm = m_scr.FindBook(57);
			Assert.AreEqual(phm.Hvo, m_scr.ScriptureBooksOS.HvoArray[0],
				"This test is invalid if Philemon isn't the first book in Scripture in the test DB.");
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			List<SegmentInfo> al = new List<SegmentInfo>(1);
			// process a \id segment to import an existing a book
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(57, 0, 0)));
			al.Add(new SegmentInfo(@"\c", "", ImportDomain.Main, new BCVRef(57, 1, 0)));
			al.Add(new SegmentInfo(@"\ip", "Intro paragraph blah-blah", ImportDomain.Main, new BCVRef(57, 1, 0)));

			m_importMgr.CallImportWithUndoTask(m_settings, al);

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "New ScrDrafts should have been purged (TE-7040)");
			Assert.AreEqual(origActCount, m_cache.ActionHandlerAccessor.UndoableActionCount,
				"Should have undone the creation of the book");
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(phm.Hvo, m_scr.FindBook(57).Hvo);
			Assert.AreEqual(57, m_scr.ScriptureBooksOS[0].CanonicalNum);
			Assert.AreEqual(0, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that an Unable to Import BT condition removes the saved version (which is
		/// then empty).
		/// (TE-7040)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BtForMissingBookRemovesEmptySavedVersion()
		{
			CheckDisposed();

			m_settings.ImportTranslation = true;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

			IScrBook matt = m_scr.FindBook(40);
			Assert.IsNull(matt, "This test is invalid if Matthew is in the test DB.");
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			List<SegmentInfo> al = new List<SegmentInfo>(1);
			// process a \id segment to import a BT for a non-existent a book
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.BackTrans, new BCVRef(40, 0, 0)));

			m_importMgr.CallImportWithUndoTask(m_settings, al);

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "New ScrDrafts should have been purged.");
			Assert.AreEqual(origActCount + 2, m_cache.ActionHandlerAccessor.UndoableActionCount, "One action to add the backup saved version, and another to remove it.");
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(0, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that canceling the import of a book that was added to the list to be merged
		/// causes it to be removed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CancelRemovesIncompleteBookFromRevisionList()
		{
			CheckDisposed();

			int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

			IScrBook phm = m_scr.FindBook(57);
			Assert.AreEqual(phm.Hvo, m_scr.ScriptureBooksOS.HvoArray[0],
				"This test is invalid if Philemon isn't the first book in Scripture in the test DB.");

			List<SegmentInfo> al = new List<SegmentInfo>(1);
			// process a \id segment to import an existing a book
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(57, 0, 0)));

			m_importMgr.CallImportWithUndoTask(m_settings, al);

			Assert.AreEqual(origActCount, m_cache.ActionHandlerAccessor.UndoableActionCount,
				"Should have undone the creation of the book");
			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "New ScrDrafts should have been purged");
			Assert.AreEqual(0, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		// TODO (TE-7097 / JohnT): disabled, since fixing setup is difficult, and the edge case
		// it tests for is no longer special, since we don't overwrite any books during basic import.
		// Leave around for now as it may be relevant to testing the post-import dialog.
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// First, insert some books before the first book and delete the 1st of those books
		///// (Genesis) in the DB, so that the OwnOrd numbers have a "hole". Then test that
		///// canceling the import of the original second book (James) causes the original book
		///// to be restored from the saved version in the correct position.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//[Test]
		//public void CancelRestoresOriginal_FirstBookWhenOwnOrdsNotContiguous()
		//{
		//    CheckDisposed();

		//    int origSeqCount = m_cache.ActionHandlerAccessor.UndoableSequenceCount;

		//    // SETUP: Import Genesis, Exodus, and Leviticus and then remove Genesis to create
		//    // a "hole" in the OwnOrds
		//    MockScrObjWrapper.s_fSimulateCancel = false;
		//    List<SegmentInfo> al = new List<SegmentInfo>(3);
		//    // process a \id segment to import an existing a book
		//    al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(1, 0, 0)));
		//    al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(2, 0, 0)));
		//    al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(3, 0, 0)));
		//    m_importMgr.CallImportWithUndoTask(m_settings, al);
		//    m_scr.ScriptureBooksOS.RemoveAt(0); // adios to Genesis

		//    IScrBook james = m_scr.FindBook(59);
		//    Assert.AreEqual(james.Hvo, m_scr.ScriptureBooksOS.HvoArray[3],
		//        "This test is invalid if James isn't the second book in Scripture in the test DB.");
		//    int cBooksAfterDeletingGenesis = m_scr.ScriptureBooksOS.Count;
		//    Assert.IsTrue(cBooksAfterDeletingGenesis > 4,
		//        "This test is invalid if the test DB has fewer than 3 books (originally).");

		//    // process a \id segment to import an existing book (James)
		//    MockScrObjWrapper.s_fSimulateCancel = true;
		//    al.Clear();
		//    al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(59, 0, 0)));

		//    m_importMgr.CallImportWithUndoTask(m_settings, al);

		//    Assert.AreEqual(origSeqCount + 2, m_cache.ActionHandlerAccessor.UndoableSequenceCount,
		//        "Should have two new undo sequences: one for the import of GEN-LEV and one for the removal of GEN.");
		//    Assert.AreEqual(cBooksAfterDeletingGenesis, m_scr.ScriptureBooksOS.Count);
		//    Assert.AreEqual(james.Hvo, m_scr.FindBook(59).Hvo);
		//    Assert.AreEqual(59, m_scr.ScriptureBooksOS[3].CanonicalNum,
		//        "James should still be in the right place in Scripture.");
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a single book exists and then that book is imported but the import process is canceled,
		/// the OwnOrd is 0. Inserting a prior book should put it in the right place.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertBookAfterCancelledImport()
		{
			CheckDisposed();

			// SETUP: We want only one book so delete everything else but one
			while (m_scr.ScriptureBooksOS.Count > 1)
			{
				m_scr.ScriptureBooksOS.RemoveAt(0);
			}
			ScrBook onlyBookLeft = (ScrBook)m_scr.ScriptureBooksOS[0];
			int iBookId = onlyBookLeft.CanonicalNum;

			List<SegmentInfo> al = new List<SegmentInfo>(1);
			// process a \id segment to import the only existing a book
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(iBookId, 0, 0)));


			m_importMgr.CallImportWithUndoTask(m_settings, al);

			int hvoTitle;
			IScrBook newBook = ScrBook.CreateNewScrBook(iBookId - 1, m_scr, out hvoTitle);
			Assert.AreEqual(2, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(iBookId - 1, m_scr.ScriptureBooksOS[0].CanonicalNum,
				"Books are not in correct order.");
			Assert.AreEqual(iBookId, m_scr.ScriptureBooksOS[1].CanonicalNum,
				"Books are not in correct order.");
		}
		#endregion

		#region Diff changes get rolled into import task
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that if an import is followed by edits in the Compare And Merge dialog, the
		/// resulting changes (cause by PropChanged calls) gets rolled into the undo task for
		/// the import. Technically, this isn't really a test of the import manager, but rather
		/// of a bug fix in the ActionHandler (TE-7339).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DiffEditAsPartOfImport()
		{
			CheckDisposed();

			m_settings.ImportTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			List<SegmentInfo> al = new List<SegmentInfo>(3);
			// process a \id segment to import an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD", ImportDomain.Main, new BCVRef(65, 0, 0)));
			al.Add(new SegmentInfo(@"\s", "Section head", ImportDomain.Main, new BCVRef(65, 1, 1)));
			al.Add(new SegmentInfo(@"\p", "Contents", ImportDomain.Main, new BCVRef(65, 1, 1)));

			m_importMgr.CallImportWithUndoTask(m_settings, al);

			Assert.AreEqual(1, m_importMgr.NewSavedVersions.Count, "We should have an imported version but not a backup saved version.");
			Assert.AreEqual(origActCount + 4, m_cache.ActionHandlerAccessor.UndoableActionCount,
				"Should have 4 extra undo actions (create backup, create imported version & first book, create second book, delete {empty} backup).");
			// Call ProcessChapterVerseNums to simulate an edit in the diff dialog.
			jude = m_scr.FindBook(65);
			ScrTxtPara sc1Para = new ScrTxtPara(m_cache, jude.SectionsOS[0].ContentOA.ParagraphsOS.HvoArray[0]);
			sc1Para.ProcessChapterVerseNums(0, 1, 0);
			Assert.AreEqual("&Undo Import", m_cache.UndoText);
			Assert.AreEqual(1, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}
		#endregion

		#region Attach BT to correct version tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that if a non-interleaved import gets a book followed by a BT of that same
		/// book, the BT attaches to the imported version rather than the current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BtAttachesToImportedVersion()
		{
			CheckDisposed();

			m_settings.ImportTranslation = true;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			List<SegmentInfo> al = new List<SegmentInfo>(3);
			// process a \id segment to import an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD", ImportDomain.Main, new BCVRef(65, 0, 0)));
			al.Add(new SegmentInfo(@"\s", "Section head", ImportDomain.Main, new BCVRef(65, 1, 1)));
			al.Add(new SegmentInfo(@"\id", "JUD Back Trans", ImportDomain.BackTrans, new BCVRef(65, 0, 0)));
			al.Add(new SegmentInfo(@"\s", "Section head BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1)));

			m_importMgr.CallImportWithUndoTask(m_settings, al);

			Assert.AreEqual(1, m_importMgr.NewSavedVersions.Count, "We should have an imported version but not a backup saved version.");
			Assert.AreEqual(origActCount + 4, m_cache.ActionHandlerAccessor.UndoableActionCount,
				"Should have 4 extra undo actions (create backup, create imported version & first book, create second book, delete {empty} backup).");
			Assert.AreEqual("&Undo Import", m_cache.UndoText);
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(jude.Hvo, m_scr.FindBook(65).Hvo, "Imported BT should not replace current version");
			StTxtPara sh1Para = ((StTxtPara)jude.SectionsOS[0].HeadingOA.ParagraphsOS[0]);
			Assert.AreNotEqual("Section head", sh1Para.Contents.Text, "Import should not have affected orginal.");
			if (sh1Para.TranslationsOC.Count == 1)
			{
				// Make sure the original BT of the first section in Jude was not changed
				foreach (CmTranslation trans in sh1Para.TranslationsOC)
					Assert.AreNotEqual("Section head BT", trans.Translation.AnalysisDefaultWritingSystem.Text);
			}

			IScrDraft importedSv = m_importMgr.UndoManager.ImportedSavedVersion;
			Assert.IsNotNull(importedSv);
			Assert.AreEqual(1, importedSv.BooksOS.Count);
			IScrBook importedJude = importedSv.BooksOS[0];
			Assert.AreEqual(65, importedJude.CanonicalNum);
			StTxtPara impSh1Para = ((StTxtPara)importedJude.SectionsOS[0].HeadingOA.ParagraphsOS[0]);
			Assert.AreEqual("Section head", impSh1Para.Contents.Text);
			if (sh1Para.TranslationsOC.Count == 1)
			{
				foreach (CmTranslation trans in impSh1Para.TranslationsOC)
					Assert.AreEqual("Section head BT", trans.Translation.AnalysisDefaultWritingSystem.Text);
			}

			Assert.IsNull(m_importMgr.UndoManager.BackupVersion);
			Assert.AreEqual(1, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that a BT-only import of an existing book causes the BT to be attached to the
		/// current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BtAttachesToCurrentVersion()
		{
			CheckDisposed();

			m_settings.ImportTranslation = false;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");

			StTxtPara scrHead1Para1 = null;
			int iScrHead1 = 0;
			foreach (ScrSection section in jude.SectionsOS)
			{
				if (!section.IsIntro)
				{
					scrHead1Para1 = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
					break;
				}
				iScrHead1++;
			}
			Assert.IsNotNull(scrHead1Para1, "This test is invalid if there is no Scripture section in Jude in the test DB.");
			string scrHead1Para1TextOrig = scrHead1Para1.Contents.Text;
			int scrHead1Para1OrigTransCount = scrHead1Para1.TranslationsOC.Count;
			Assert.AreEqual(0, scrHead1Para1OrigTransCount, "This test is invalid if the first paragraph of the first Scripture section head in Jude has a backtranslation in the test DB.");

			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			List<SegmentInfo> al = new List<SegmentInfo>(3);
			// process a \id segment to import an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD Back Trans", ImportDomain.BackTrans, new BCVRef(65, 0, 0)));
			al.Add(new SegmentInfo(@"\s", "Section head BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1)));

			m_importMgr.CallImportWithUndoTask(m_settings, al);

			Assert.AreEqual(1, m_importMgr.NewSavedVersions.Count, "We should only have a backup saved version, no imported version.");
			Assert.AreEqual(origActCount + 2, m_cache.ActionHandlerAccessor.UndoableActionCount,
				"Should have 2 extra undo actions (create backup, create imported version & first book).");
			Assert.AreEqual("&Undo Import", m_cache.UndoText);
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(jude.Hvo, m_scr.FindBook(65).Hvo, "Imported BT should not replace current version");
			StTxtPara sh1Para = ((StTxtPara)jude.SectionsOS[iScrHead1].HeadingOA.ParagraphsOS[0]);
			Assert.AreEqual(scrHead1Para1TextOrig, sh1Para.Contents.Text, "Import should not have affected orginal vernacular.");
			if (sh1Para.TranslationsOC.Count == 1)
			{
				// Make sure the original BT of the first section in Jude was changed
				foreach (CmTranslation trans in sh1Para.TranslationsOC)
					Assert.AreEqual("Section head BT", trans.Translation.AnalysisDefaultWritingSystem.Text);
			}

			Assert.IsNull(m_importMgr.UndoManager.ImportedSavedVersion);

			IScrDraft backupSv = m_importMgr.UndoManager.BackupVersion;
			Assert.IsNotNull(backupSv);
			Assert.AreEqual(1, backupSv.BooksOS.Count);
			IScrBook backupJude = backupSv.BooksOS[0];
			Assert.AreEqual(65, backupJude.CanonicalNum);
			StTxtPara bkpScrHead1Para1 = ((StTxtPara)backupJude.SectionsOS[iScrHead1].HeadingOA.ParagraphsOS[0]);
			Assert.AreEqual(scrHead1Para1TextOrig, bkpScrHead1Para1.Contents.Text);
			Assert.AreEqual(scrHead1Para1OrigTransCount, bkpScrHead1Para1.TranslationsOC.Count);

			Assert.AreEqual(0, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test BT-only import in two different writing systems. Only one backup copy of the
		/// original book is saved in the archive and the partial BT is left on
		/// the current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BtsForMultipleWss()
		{
			CheckDisposed();

			m_settings.ImportTranslation = false;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			int origActCount = m_cache.ActionHandlerAccessor.UndoableActionCount;

			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");

			StTxtPara scrHead1Para1 = null;
			int iScrHead1 = 0;
			foreach (ScrSection section in jude.SectionsOS)
			{
				if (!section.IsIntro)
				{
					scrHead1Para1 = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
					break;
				}
				iScrHead1++;
			}
			Assert.IsNotNull(scrHead1Para1, "This test is invalid if there is no Scripture section in Jude in the test DB.");
			string scrHead1Para1TextOrig = scrHead1Para1.Contents.Text;
			int scrHead1Para1OrigTransCount = scrHead1Para1.TranslationsOC.Count;
			Assert.AreEqual(0, scrHead1Para1OrigTransCount, "This test is invalid if the first paragraph of the first Scripture section head in Jude has a backtranslation in the test DB.");

			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			int wsEn = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
			Assert.Greater(wsEn, 0, "Couldn't find Id of English WS in test DB.");
			int wsEs = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("es");
			Assert.Greater(wsEs, 0, "Couldn't find Id of Spanish WS in test DB.");

			List<SegmentInfo> al = new List<SegmentInfo>(3);
			// process a \id segment to import an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD English Back Trans", ImportDomain.BackTrans, new BCVRef(65, 0, 0), wsEn));
			al.Add(new SegmentInfo(@"\s", "English Section head BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1), wsEn));
			al.Add(new SegmentInfo(@"\id", "JUD Spanish Back Trans", ImportDomain.BackTrans, new BCVRef(65, 0, 0), wsEs));
			al.Add(new SegmentInfo(@"\s", "Spanish Section head BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1), wsEs));

			m_importMgr.CallImportWithUndoTask(m_settings, al);

			Assert.AreEqual(1, m_importMgr.NewSavedVersions.Count, "We should only have a backup saved version, no imported version.");
			Assert.AreEqual(origActCount + 2, m_cache.ActionHandlerAccessor.UndoableActionCount,
				"Should have 2 extra undo actions (create backup, create imported version & both BTs).");
			Assert.AreEqual("&Undo Import", m_cache.UndoText);
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(jude.Hvo, m_scr.FindBook(65).Hvo, "Imported BT should not replace current version");
			StTxtPara sh1Para = ((StTxtPara)jude.SectionsOS[iScrHead1].HeadingOA.ParagraphsOS[0]);
			Assert.AreEqual(scrHead1Para1TextOrig, sh1Para.Contents.Text, "Import should not have affected orginal vernacular.");
			if (sh1Para.TranslationsOC.Count == 1)
			{
				// Make sure the original BT of the first section in Jude was changed
				foreach (CmTranslation trans in sh1Para.TranslationsOC)
				{
					Assert.AreEqual("English Section head BT", trans.Translation.GetAlternative(wsEn).Text);
					Assert.AreEqual("Spanish Section head BT", trans.Translation.GetAlternative(wsEs).Text);
				}
			}

			Assert.IsNull(m_importMgr.UndoManager.ImportedSavedVersion);

			IScrDraft backupSv = m_importMgr.UndoManager.BackupVersion;
			Assert.IsNotNull(backupSv);
			Assert.AreEqual(1, backupSv.BooksOS.Count);
			IScrBook backupJude = backupSv.BooksOS[0];
			Assert.AreEqual(65, backupJude.CanonicalNum);
			StTxtPara bkpScrHead1Para1 = ((StTxtPara)backupJude.SectionsOS[iScrHead1].HeadingOA.ParagraphsOS[0]);
			Assert.AreEqual(scrHead1Para1TextOrig, bkpScrHead1Para1.Contents.Text);
			Assert.AreEqual(scrHead1Para1OrigTransCount, bkpScrHead1Para1.TranslationsOC.Count);

			Assert.AreEqual(0, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}
		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first paragraph of the first Scripture section (skips intros) in book.
		/// </summary>
		/// <param name="book">The Scripture book.</param>
		/// <returns>The first paragraph of the first Scripture section</returns>
		/// ------------------------------------------------------------------------------------
		private static StTxtPara GetFirstScriptureSectionHeadParaInBook(IScrBook book)
		{
			StTxtPara scrHead1Para1 = null;
			BCVRef targetRef = new BCVRef(book.CanonicalNum, 1, 1);
			foreach (IScrSection section in book.SectionsOS)
			{
				if (!section.IsIntro && section.VerseRefStart == targetRef)
				{
					scrHead1Para1 = ((StTxtPara)section.HeadingOA.ParagraphsOS[0]);
					Assert.AreEqual(ScrStyleNames.SectionHead,
						scrHead1Para1.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
					break;
				}
			}
			return scrHead1Para1;
		}
		#endregion
	}
	#endregion
}
