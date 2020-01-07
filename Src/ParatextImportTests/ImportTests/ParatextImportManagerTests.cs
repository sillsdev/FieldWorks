// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.LCModel;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;
using SIL.LCModel.Core.Scripture;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace ParatextImport.ImportTests
{
#if RANDYTODO
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
		/// Initializes a new instance of the <see cref="SegmentInfo"/> class.
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
		/// Initializes a new instance of the <see cref="SegmentInfo"/> class.
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
		private readonly List<SegmentInfo> m_segmentList;
		private int m_curSeg;
		private BCVRef m_ref;
		private readonly MockParatextImporter m_importer;
		private int m_wsOverride;

		public static bool s_fSimulateCancel = true;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="MockScrObjWrapper"/> class.
		/// </summary>
		/// <param name="importer">The importer.</param>
		/// <param name="segmentList">The segment list.</param>
		/// ------------------------------------------------------------------------------------
		public MockScrObjWrapper(MockParatextImporter importer, List<SegmentInfo> segmentList)
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

	#region class MockParatextImporter
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class MockParatextImporter : ParatextSfmImporter
	{
		private List<SegmentInfo> m_segmentList;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="MockParatextImporter"/> class.
		/// </summary>
		/// <param name="settings">Import settings object (filled in by wizard)</param>
		/// <param name="cache">The cache used to import to and get misc. info. from.</param>
		/// <param name="styleSheet">Stylesheet from which to get scripture styles.</param>
		/// <param name="undoManager">The undo import manager(which is responsible for creating
		/// and maintaining the saved version (archive) of original books being overwritten).</param>
		/// <param name="importCallbacks">UI callbacks</param>
		/// ------------------------------------------------------------------------------------
		public MockParatextImporter(IScrImportSet settings, LcmCache cache,
			LcmStyleSheet styleSheet, UndoImportManager undoManager, ParatextImportUi importCallbacks)
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
		public static ScrReference Import(IScrImportSet settings, LcmCache cache,
			LcmStyleSheet styleSheet, UndoImportManager undoManager,
			ParatextImportUi importCallbacks, List<SegmentInfo> segmentList)
		{
			using (MockParatextImporter importer = new MockParatextImporter(settings, cache, styleSheet, undoManager,
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
			((DummyParatextImportUi)m_importCallbacks).SimulateCancel();
		}
	}
	#endregion

	#region class DummyParatextImportUi
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyParatextImportUi : ParatextImportNoUi
	{
		private bool m_fCancel;

		/// <summary>
		///
		/// </summary>
		public override bool CancelImport
		{
			get { return m_fCancel; }
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

	#region class DummyParatextImportManager
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///  A TEImportManager that does not display the ImportedBooks dialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyParatextImportManager : ParatextImportManager
	{
	#region Member data
		private readonly IScripture m_scr;
		private HashSet<int> m_originalDrafts;
		internal int m_cDisplayImportedBooksDlgCalled;
		private bool m_fSimulateAcceptAllBooks;
	#endregion

	#region Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyParatextImportManager"/> class.
		/// </summary>
		public DummyParatextImportManager(LcmCache cache, IScrImportSet importSettings, LcmStyleSheet styleSheet) :
			base(null, cache, importSettings, styleSheet, null)
		{
			m_scr = cache.LangProject.TranslatedScriptureOA;
			ResetOriginalDrafts();
		}
	#endregion

	#region Internal methods/properties for testing
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the original drafts.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void ResetOriginalDrafts()
		{
			m_originalDrafts = new HashSet<int>(m_scr.ArchivedDraftsOC.ToHvoArray());
		}

		/// <summary>
		/// Import scripture and embed it in a Undo task so that it is undoable.
		/// Simulates ParatextImportManager.DoImport()
		/// </summary>
		public void CallImportWithUndoTask()
		{
			var firstImported = ImportWithUndoTask(false, string.Empty);
			CompleteImport(firstImported);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides DisplayImportedBooksDlg for testing.
		/// </summary>
		/// <param name="backupSavedVersion">The saved version for backups of any overwritten
		/// books.</param>
		/// ------------------------------------------------------------------------------------
		protected override void SaveImportedBooks(IScrDraft backupSavedVersion)
		{
			m_cDisplayImportedBooksDlgCalled++;
			if (m_fSimulateAcceptAllBooks)
			{
				ImportedBooks.SaveImportedBooks(Cache, ImportedVersion, backupSavedVersion, UndoManager.ImportedBooks.Keys, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the set of HVOs of any new saved versions (i.e., those created as part of
		/// import).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ISet<int> NewSavedVersions
		{
			get
			{
				// This is a kludgy way to get the new saved version since CallImportWithUndoTask
				// does not save it.
				return new HashSet<int>(m_scr.ArchivedDraftsOC.ToHvoArray().Except(m_originalDrafts));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets a value indicating whether to simulate accepting all books.
		/// If set to	True, this import manager will simulate accepting all imported books in
		/// the simulated ImportedBooks dialog, thus copying the imported books from
		/// Saved Versions to the current Scripture books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool SimulateAcceptAllBooks
		{
			set
			{
				//if (value && m_fSimulateDeleteAllBooks)
				//    throw new Exception("SimulateAcceptAllBooks and SimulateDeleteAllBooks cannot both be set to true.");
				m_fSimulateAcceptAllBooks = value;
			}
		}

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
		protected override ParatextImportUi CreateParatextImportUi(ProgressDialogWithTask progressDialog)
		{
			return new DummyParatextImportUi();
		}
	#endregion
	}
	#endregion

	#region class DummyParatextImportManagerWithMockImporter
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A TEImportManager that does not display the ImportedBooks dialog,
	/// and uses a MockTeImporter.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyParatextImportManagerWithMockImporter : DummyParatextImportManager
	{
	#region Member data
		private List<SegmentInfo> m_segmentList;
	#endregion

	#region Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyParatextImportManagerWithMockImporter"/> class.
		/// </summary>
		public DummyParatextImportManagerWithMockImporter(LcmCache cache, IScrImportSet importSettings, LcmStyleSheet styleSheet) :
			base(cache, importSettings, styleSheet)
		{
		}
	#endregion

	#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Import scripture and embed it in a Undo task so that it is undoable.
		/// Simulates TeImportManager.DoImport()
		/// </summary>
		/// <param name="segmentList">The segment list.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal void CallImportWithUndoTask(List<SegmentInfo> segmentList)
		{
			m_segmentList = segmentList;
			CallImportWithUndoTask();
		}

		/// <summary>
		/// Imports using the MockTeImporter with the specified import settings.
		/// </summary>
		/// <param name="importUi">The import UI.</param>
		/// <returns>
		/// The Scripture reference of the first thing imported
		/// </returns>
		protected override ScrReference Import(ParatextImportUi importUi)
		{
			MockParatextImporter.Import(m_importSettings, Cache, StyleSheet, m_undoImportManager, importUi, m_segmentList);
			return ScrReference.Empty;
		}

	#endregion
	}
	#endregion

	#region class ParatextImportManagerTests
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for ParatextImportManager.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[TestFixture]
	public class ParatextImportManagerTests : ScrInMemoryLcmTestBase
	{
	#region Member variables
		private DummyParatextImportManagerWithMockImporter m_importMgr;
		private BCVRef m_titus;
		private LcmStyleSheet m_styleSheet;
		private IScrImportSet m_settings;
	#endregion

	#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create temp registry settings and unzipped files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_styleSheet = new LcmStyleSheet();
			m_styleSheet.Init(Cache, Cache.LangProject.Hvo, LangProjectTags.kflidStyles);

			// By default, use auto-generated footnote markers for import tests.
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.AutoFootnoteMarker;

			m_titus = new BCVRef(56001001);

			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				m_settings = m_scr.FindOrCreateDefaultImportSettings(TypeOfImport.Unknown, m_styleSheet, FwDirectoryFinder.FlexStylesPath);
				DummyParatextImporter.MakeSFImportTestSettings(m_settings);
			});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the importer and set up Undo action
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			MockScrObjWrapper.s_fSimulateCancel = true;
			m_settings.StartRef = m_titus;
			m_settings.EndRef = m_titus;
			m_settings.ImportTranslation = true;
			m_settings.ImportBackTranslation = false;
			m_settings.ImportBookIntros = true;
			m_settings.ImportAnnotations = false;

			m_importMgr = new DummyParatextImportManagerWithMockImporter(Cache, m_settings, m_styleSheet);
			Cache.ServiceLocator.GetInstance<IActionHandler>().EndUndoTask();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data for the books of Philemon and Jude.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();
			CreateBookData(57, "Philemon");
			CreateBookData(65, "Jude");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Rollback any DB changes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_importMgr = null;
			// We have to begin another undo task because we have to end the undo task in the
			// test setup.
			Cache.ActionHandlerAccessor.BeginUndoTask("Bogus", "Bogus");
			base.TestTearDown();
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
		[Category("DesktopRequired")]
		public void CancelDiscardsNewBook()
		{
			int origActCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			m_settings.ImportAnnotations = true;
			Assert.IsNull(m_scr.FindBook(1), "This test is invalid if Genesis exists in test DB.");
			// Make sure there are no notes for Genesis
			ILcmOwningSequence<IScrScriptureNote> notes = m_scr.BookAnnotationsOS[0].NotesOS;
			int cNotesOrig  = notes.Count;
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			// process an \id segment to import Genesis (a non-existing book)
			List<SegmentInfo> al = new List<SegmentInfo>(4);
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(1, 0, 0)));
			al.Add(new SegmentInfo(@"\rem", "This annotation should get deleted (TE-4535)", ImportDomain.Main, new BCVRef(1, 0, 0)));
			al.Add(new SegmentInfo(@"\mt", "Geneseo", ImportDomain.Main, new BCVRef(1, 0, 0)));

			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "No new ScrDrafts should have been created");
			Assert.AreEqual(origActCount, Cache.ActionHandlerAccessor.UndoableSequenceCount,
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
		[Category("DesktopRequired")]
		public void CancelDiscardsNewBook_AfterImportOfOneExistingBook()
		{
			var origActCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			var jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude does not exist in test DB.");
			Assert.IsNull(m_scr.FindBook(66), "This test is invalid if Revelation exists in test DB.");

			var hvoJudeOrig = jude.Hvo;
			var cBooksOrig = m_scr.ScriptureBooksOS.Count;

			var al = new List<SegmentInfo>(2)
			{
				// process a \id segment to import Jude (an existing book)
				new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(65, 0, 0)),
				// process a \id segment to import Rev (a non-existing book)
				new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(66, 0, 0))
			};
			m_importMgr.SimulateAcceptAllBooks = true;
			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(origActCount + 1, Cache.ActionHandlerAccessor.UndoableSequenceCount, "Should have 1 extra undo sequence (import of JUD) after Undo cancels incomplete book");
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			var curJude = m_scr.FindBook(65);
			Assert.AreEqual(hvoJudeOrig, curJude.Hvo, "Content should have been merged into Jude.");
			Assert.AreEqual(curJude.Hvo, m_scr.ScriptureBooksOS.ToHvoArray()[1]);
			Assert.IsNull(m_scr.FindBook(66), "Partially-imported Revelation should have been discarded.");
			Assert.IsNull(m_importMgr.UndoManager.ImportedVersion);
			Assert.AreEqual(1, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}
	#endregion

	#region Restore after cancel tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that canceling the import of an existing book causes the original book to be
		/// restored from the saved version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("DesktopRequired")]
		public void CancelRestoresOriginal_AfterImportingOneCompleteBook()
		{
			int origActCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			IScrBook phm = m_scr.FindBook(57);
			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(phm, "This test is invalid if Philemon does not exist.");
			Assert.IsNotNull(jude, "This test is invalid if Jude does not exist.");

			int hvoJudeOrig = jude.Hvo;
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			// process a \id segment to import Philemon (an existing book)
			List<SegmentInfo> al = new List<SegmentInfo>(2);
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(57, 0, 0)));
			// process a \id segment to import Jude (also an existing book)
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(65, 0, 0)));

			m_importMgr.SimulateAcceptAllBooks = true;
			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(origActCount + 1, Cache.ActionHandlerAccessor.UndoableSequenceCount, "Should have 1 extra undo action after Undo cancels incomplete book");

			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "Import should have merged with existing book");
			IScrBook restoredJude = m_scr.FindBook(65);
			Assert.AreEqual(hvoJudeOrig, restoredJude.Hvo);
			Assert.AreEqual(restoredJude.Hvo, m_scr.ScriptureBooksOS.ToHvoArray()[1]);
			Assert.AreEqual(1, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that canceling the import of the first book causes the original book to be
		/// restored from the archive. And the archive (which is then empty) should be removed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("DesktopRequired")]
		public void CancelRestoresOriginal_FirstBook()
		{
			int origActCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			IScrBook phm = m_scr.FindBook(57);
			Assert.AreEqual(phm.Hvo, m_scr.ScriptureBooksOS.ToHvoArray()[0],
				"This test is invalid if Philemon isn't the first book in Scripture in the test DB.");
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			List<SegmentInfo> al = new List<SegmentInfo>(1);
			// process a \id segment to import an existing a book
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(57, 0, 0)));

			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "New ScrDrafts should have been purged");
			Assert.AreEqual(origActCount, Cache.ActionHandlerAccessor.UndoableSequenceCount,
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
		[Category("DesktopRequired")]
		public void BtAbortSavesOriginal()
		{
			m_settings.ImportTranslation = false;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			int origActCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");
			int hvoJudeOrig = jude.Hvo;
			IStTxtPara scrHead1Para1 = GetFirstScriptureSectionHeadParaInBook(jude);
			Assert.IsNotNull(scrHead1Para1, "This test is invalid if we can't find a normal Scripture section for Jude 1:1.");
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			List<SegmentInfo> al = new List<SegmentInfo>(3);
			// process a \id segment to import the BT for an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD Back Trans", ImportDomain.BackTrans, new BCVRef(65, 0, 0)));
			al.Add(new SegmentInfo(@"\s", "Section head BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1)));
			al.Add(new SegmentInfo(@"\s2", "Minor Section head BT (no match in vern)", ImportDomain.BackTrans, new BCVRef(65, 1, 1)));

			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(1, m_importMgr.NewSavedVersions.Count, "Exactly one new version should have been created");
			Assert.AreEqual(origActCount + 1, Cache.ActionHandlerAccessor.UndoableSequenceCount,
				"Should have one extra undo action after Undo cancels incomplete book");
			Assert.AreEqual("&Undo Import", Cache.ActionHandlerAccessor.GetUndoText());
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(hvoJudeOrig, m_scr.FindBook(65).Hvo);
			Assert.AreEqual(1, scrHead1Para1.TranslationsOC.Count);
			foreach (ICmTranslation trans in scrHead1Para1.TranslationsOC)
			{
				Assert.AreEqual("Section head BT", trans.Translation.AnalysisDefaultWritingSystem.Text);
			}

			Assert.IsNull(m_importMgr.UndoManager.ImportedVersion);
			IScrDraft backupSv = m_importMgr.UndoManager.BackupVersion;
			Assert.AreEqual(1, backupSv.BooksOS.Count);
			Assert.AreEqual(65, backupSv.BooksOS[0].CanonicalNum);

			// Test ability to Undo and get back to where we were.
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());
			Cache.ActionHandlerAccessor.Undo();
			Assert.AreEqual(origActCount, Cache.ActionHandlerAccessor.UndoableSequenceCount);
			Assert.AreEqual("Undo doing stuff", Cache.ActionHandlerAccessor.GetUndoText());
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			jude = m_scr.FindBook(65);
			Assert.AreEqual(hvoJudeOrig, jude.Hvo);
			scrHead1Para1 = GetFirstScriptureSectionHeadParaInBook(jude);
			Assert.AreEqual(1, scrHead1Para1.TranslationsOC.Count);
			Assert.IsNull(scrHead1Para1.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			// Backed up version should be gone.
			Assert.IsFalse(backupSv.IsValidObject);
			Assert.AreEqual(0, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that if an interleaved BT import aborts because of an Unable to Import BT
		/// exception, the partial (vernacular) book is discarded.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("DesktopRequired")]
		public void BtInterleavedAbortRollsBack()
		{
			m_settings.ImportTranslation = true;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;
			int wsEn = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
			Assert.Greater(wsEn, 0, "Couldn't find Id of English WS in test DB.");

			int origActCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			List<SegmentInfo> al = new List<SegmentInfo>(3);
			// process a \id segment to import the BT for a non-existing a book
			al.Add(new SegmentInfo(@"\id", "GEN interleaved", ImportDomain.Main, new BCVRef(1, 0, 0)));
			al.Add(new SegmentInfo(@"\mt", "", ImportDomain.Main, new BCVRef(1, 0, 0)));
			al.Add(new SegmentInfo(@"\btmt", "Genesis", ImportDomain.Main, new BCVRef(1, 0, 0), wsEn));
			al.Add(new SegmentInfo(@"\c", "1", ImportDomain.Main, new BCVRef(1, 1, 0)));

			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "No new version should have been created");
			Assert.AreEqual(origActCount, Cache.ActionHandlerAccessor.UndoableSequenceCount);
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.IsNull(m_scr.FindBook(1));
			Assert.IsNull(m_importMgr.UndoManager.ImportedVersion);
			Assert.IsNull(m_importMgr.UndoManager.BackupVersion);
			Assert.AreEqual(0, m_importMgr.m_cDisplayImportedBooksDlgCalled);
			Assert.IsFalse(Cache.ActionHandlerAccessor.CanRedo());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that if a BT-only import aborts because of an "Unable to Import BT" exception,
		/// any previously successfully imported vernacular book is displayed properly in the
		/// Imported Books dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("DesktopRequired")]
		public void UnableToImportBtAfterSuccessfulBookImport()
		{
			m_settings.ImportTranslation = true;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			int origActCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			Assert.IsNull(m_scr.FindBook(1), "This test is invalid if Genesis is in the test DB.");
			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");
			int hvoJudeOrig = jude.Hvo;
			IStTxtPara scrHead1Para1 = GetFirstScriptureSectionHeadParaInBook(jude);
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
			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(1, m_importMgr.NewSavedVersions.Count, "One new version should have been created");
			Assert.AreEqual(origActCount + 1, Cache.ActionHandlerAccessor.UndoableSequenceCount,
				"Should have one extra undo action after Undo cancels incomplete book");
			Assert.AreEqual("&Undo Import", Cache.ActionHandlerAccessor.GetUndoText());
			Assert.AreEqual(cBooksOrig + 1, m_scr.ScriptureBooksOS.Count);
			Assert.IsNotNull(m_scr.FindBook(1));
			Assert.AreEqual(hvoJudeOrig, m_scr.FindBook(65).Hvo);

			Assert.IsNull(m_importMgr.UndoManager.ImportedVersion);
			IScrDraft backupSv = m_importMgr.UndoManager.BackupVersion;
			Assert.IsNotNull(backupSv);
			Assert.AreEqual(1, backupSv.BooksOS.Count);
			Assert.AreEqual(65, backupSv.BooksOS[0].CanonicalNum);

			Assert.AreEqual(1, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that an import successfully imports when the ScrDraft is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("DesktopRequired")]
		public void ImportIntoEmptyScrDraft()
		{
			m_settings.ImportTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			Assert.IsNull(m_scr.FindBook(1), "This test is invalid if Genesis is in the test DB.");
			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");
			IStTxtPara scrHead1Para1 = GetFirstScriptureSectionHeadParaInBook(jude);
			Assert.IsNotNull(scrHead1Para1, "This test is invalid if we can't find a normal Scripture section for Jude 1:1.");

			List<SegmentInfo> al = new List<SegmentInfo>(7);
			// process an \id segment to import the BT for an existing a book
			al.Add(new SegmentInfo(@"\id", "GEN", ImportDomain.Main, new BCVRef(1, 0, 0)));
			al.Add(new SegmentInfo(@"\c", string.Empty, ImportDomain.Main, new BCVRef(1, 1, 1)));
			al.Add(new SegmentInfo(@"\s", "The Creation", ImportDomain.Main, new BCVRef(1, 1, 1)));
			al.Add(new SegmentInfo(@"\v", "In the beginning...", ImportDomain.Main, new BCVRef(1, 1, 1)));
			// process an \id segment to import the BT for an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD", ImportDomain.Main, new BCVRef(65, 0, 0)));
			al.Add(new SegmentInfo(@"\s", "Section head", ImportDomain.Main, new BCVRef(65, 1, 1)));

			// Create ScrDrafts before import
			IScrDraft draftReplacedBooks = null;
			IScrDraft draftNewBooks = null;
			UndoableUnitOfWorkHelper.Do("bla undo", "bla redo", m_actionHandler, () =>
			{
				draftReplacedBooks = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(Properties.Resources.kstidSavedVersionDescriptionOriginal, ScrDraftType.ImportedVersion);
				draftNewBooks = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(Properties.Resources.kstidStandardFormatImportSvDesc, ScrDraftType.ImportedVersion);
			});
			m_importMgr.ResetOriginalDrafts();
			m_importMgr.SimulateAcceptAllBooks = true;
			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "No new versions should have been created");

			Assert.IsNull(m_importMgr.UndoManager.ImportedVersion);
			Assert.IsFalse(draftNewBooks.IsValidObject);

			Assert.AreEqual(1, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that an import successfully imports when the ScrDraft has one book and import
		/// adds one book to the ScrDraft instead of adding a new ScrDraft.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("DesktopRequired")]
		public void ImportWithOneBookInScrDraft()
		{
			m_settings.ImportTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			Assert.IsNull(m_scr.FindBook(1), "This test is invalid if Genesis is in the test DB.");
			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");
			IStTxtPara scrHead1Para1 = GetFirstScriptureSectionHeadParaInBook(jude);
			Assert.IsNotNull(scrHead1Para1, "This test is invalid if we can't find a normal Scripture section for Jude 1:1.");

			List<SegmentInfo> al = new List<SegmentInfo>(7);
			// process an \id segment to import the BT for an existing a book
			al.Add(new SegmentInfo(@"\id", "GEN", ImportDomain.Main, new BCVRef(1, 0, 0)));
			al.Add(new SegmentInfo(@"\c", string.Empty, ImportDomain.Main, new BCVRef(1, 1, 1)));
			al.Add(new SegmentInfo(@"\s", "The Creation", ImportDomain.Main, new BCVRef(1, 1, 1)));
			al.Add(new SegmentInfo(@"\v", "In the beginning...", ImportDomain.Main, new BCVRef(1, 1, 1)));
			// process an \id segment to import the BT for an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD", ImportDomain.Main, new BCVRef(65, 0, 0)));
			al.Add(new SegmentInfo(@"\s", "Section head", ImportDomain.Main, new BCVRef(65, 1, 1)));

			// Create ScrDrafts before import
			IScrDraft draftReplacedBooks = null;
			IScrDraft draftNewBooks = null;
			IScrBook replacedBook = null;
			IScrBook newBook = null;
			UndoableUnitOfWorkHelper.Do("bla undo", "bla redo", m_actionHandler, () =>
			{
				draftReplacedBooks = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(Properties.Resources.kstidSavedVersionDescriptionOriginal, ScrDraftType.ImportedVersion);
				draftNewBooks = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(Properties.Resources.kstidStandardFormatImportSvDesc, ScrDraftType.ImportedVersion);
				// Add a single book to both ScrDrafts
				replacedBook = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(draftReplacedBooks.BooksOS, 2);
				newBook = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(draftNewBooks.BooksOS, 2);
			});

			m_importMgr.ResetOriginalDrafts();
			m_importMgr.SimulateAcceptAllBooks = true;
			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "No new versions should have been created");

			Assert.IsNull(m_importMgr.UndoManager.ImportedVersion);
			Assert.AreEqual(1, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that an import successfully imports when the ScrDraft already contain the books
		/// to be imported.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("DesktopRequired")]
		public void ImportWhenAllBooksInScrDraft()
		{
			m_settings.ImportTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			Assert.IsNull(m_scr.FindBook(1), "This test is invalid if Genesis is in the test DB.");
			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");
			IStTxtPara scrHead1Para1 = GetFirstScriptureSectionHeadParaInBook(jude);
			Assert.IsNotNull(scrHead1Para1, "This test is invalid if we can't find a normal Scripture section for Jude 1:1.");

			List<SegmentInfo> al = new List<SegmentInfo>(7);
			// process an \id segment to import the BT for an existing a book
			al.Add(new SegmentInfo(@"\id", "GEN", ImportDomain.Main, new BCVRef(1, 0, 0)));
			al.Add(new SegmentInfo(@"\c", string.Empty, ImportDomain.Main, new BCVRef(1, 1, 1)));
			al.Add(new SegmentInfo(@"\s", "The Creation", ImportDomain.Main, new BCVRef(1, 1, 1)));
			al.Add(new SegmentInfo(@"\v", "In the beginning...", ImportDomain.Main, new BCVRef(1, 1, 1)));
			// process an \id segment to import the BT for an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD", ImportDomain.Main, new BCVRef(65, 0, 0)));
			al.Add(new SegmentInfo(@"\s", "Section head", ImportDomain.Main, new BCVRef(65, 1, 1)));

			// Create ScrDrafts before import
			IScrDraft draftReplacedBooks = null;
			IScrDraft draftNewBooks = null;
			IScrBook replacedBook1 = null;
			IScrBook replacedBook2 = null;
			IScrBook newBook1 = null;
			IScrBook newBook2 = null;
			UndoableUnitOfWorkHelper.Do("bla undo", "bla redo", m_actionHandler, () =>
			{
				draftReplacedBooks = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(Properties.Resources.kstidSavedVersionDescriptionOriginal, ScrDraftType.ImportedVersion);
				draftNewBooks = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(Properties.Resources.kstidStandardFormatImportSvDesc, ScrDraftType.ImportedVersion);
				// Add both imported books to both ScrDrafts
				replacedBook1 = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(draftReplacedBooks.BooksOS, 1);
				replacedBook2 = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(draftReplacedBooks.BooksOS, 65);
				newBook1 = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(draftNewBooks.BooksOS, 1);
				newBook2 = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(draftNewBooks.BooksOS, 65);
			});

			m_importMgr.ResetOriginalDrafts();
			m_importMgr.SimulateAcceptAllBooks = true;
			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "No new versions should have been created");

			Assert.IsNull(m_importMgr.UndoManager.ImportedVersion);

			IScrDraft backupSv = m_importMgr.UndoManager.BackupVersion;
			Assert.AreEqual(draftReplacedBooks, backupSv);
			Assert.AreEqual(2, backupSv.BooksOS.Count);
			Assert.AreEqual(1, backupSv.BooksOS[0].CanonicalNum);
			Assert.AreEqual(replacedBook1, backupSv.BooksOS[0], "No original book in scripture, so backup should not change");

			Assert.AreEqual(65, backupSv.BooksOS[1].CanonicalNum);
			Assert.AreEqual(replacedBook2, backupSv.BooksOS[1], "Imported book should have merged with original");
			Assert.AreEqual(jude, m_scr.FindBook(65), "Scripture should contain the merged book");

			Assert.AreEqual(1, m_importMgr.m_cDisplayImportedBooksDlgCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test PrepareBookNotImportingVern when there are no books in the archive. We expect
		/// that no archives will added, but books will be added to the replaced books but not
		/// to the new book archive.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("DesktopRequired")]
		public void PrepareBookNotImportingVern_NoBooksInArchive()
		{
			m_settings.ImportTranslation = false;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			Assert.IsNull(m_scr.FindBook(1), "This test is invalid if Genesis is in the test DB.");
			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");
			IStTxtPara scrHead1Para1 = GetFirstScriptureSectionHeadParaInBook(jude);
			Assert.IsNotNull(scrHead1Para1, "This test is invalid if we can't find a normal Scripture section for Jude 1:1.");

			List<SegmentInfo> al = new List<SegmentInfo>(7);
			// process an \id segment to import the BT for an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD", ImportDomain.BackTrans, new BCVRef(65, 0, 0)));
			al.Add(new SegmentInfo(@"\s", "Section head", ImportDomain.BackTrans, new BCVRef(65, 1, 1)));
			al.Add(new SegmentInfo(@"\p", "Paragraph BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1)));

			IScrDraft draftReplacedBooks = null;
			IScrDraft draftNewBooks = null;
			UndoableUnitOfWorkHelper.Do("bla undo", "bla redo", m_actionHandler, () =>
			{
				draftReplacedBooks = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(Properties.Resources.kstidSavedVersionDescriptionOriginal, ScrDraftType.ImportedVersion);
				draftNewBooks = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(Properties.Resources.kstidStandardFormatImportSvDesc, ScrDraftType.ImportedVersion);
			});

			int origScrDraftsCount = m_scr.ArchivedDraftsOC.Count;

			m_importMgr.ResetOriginalDrafts();
			m_importMgr.SimulateAcceptAllBooks = true;
			m_importMgr.CallImportWithUndoTask(al);
			Assert.AreEqual(origScrDraftsCount, m_scr.ArchivedDraftsOC.Count, "Number of ScrDrafts shouldn't change");
			Assert.AreEqual(65, draftReplacedBooks.BooksOS[0].CanonicalNum);
			Assert.AreEqual(0, draftNewBooks.BooksOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test PrepareBookNotImportingVern when there is one book in the archive. We expect
		/// that no archives will added, but a book will be added to only the replaced books
		/// archive.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("DesktopRequired")]
		public void PrepareBookNotImportingVern_SomeBooksInArchive()
		{
			m_settings.ImportTranslation = false;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			Assert.IsNull(m_scr.FindBook(1), "This test is invalid if Genesis is in the test DB.");
			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");
			IStTxtPara scrHead1Para1 = GetFirstScriptureSectionHeadParaInBook(jude);
			Assert.IsNotNull(scrHead1Para1, "This test is invalid if we can't find a normal Scripture section for Jude 1:1.");

			List<SegmentInfo> al = new List<SegmentInfo>(7);
			// process an \id segment to import the BT for an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD", ImportDomain.BackTrans, new BCVRef(65, 0, 0)));
			al.Add(new SegmentInfo(@"\s", "Section head", ImportDomain.BackTrans, new BCVRef(65, 1, 1)));
			al.Add(new SegmentInfo(@"\p", "Paragraph BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1)));

			IScrDraft draftReplacedBooks = null;
			IScrDraft draftNewBooks = null;
			IScrBook replacedBook = null;
			IScrBook newBook = null;
			UndoableUnitOfWorkHelper.Do("bla undo", "bla redo", m_actionHandler, () =>
			{
				draftReplacedBooks = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(Properties.Resources.kstidSavedVersionDescriptionOriginal, ScrDraftType.ImportedVersion);
				draftNewBooks = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(Properties.Resources.kstidStandardFormatImportSvDesc, ScrDraftType.ImportedVersion);
				// Add a single book to both ScrDrafts
				replacedBook = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(draftReplacedBooks.BooksOS, 2);
				newBook = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(draftNewBooks.BooksOS, 2);
			});

			int origScrDraftsCount = m_scr.ArchivedDraftsOC.Count;

			m_importMgr.ResetOriginalDrafts();
			m_importMgr.SimulateAcceptAllBooks = true;
			m_importMgr.CallImportWithUndoTask(al);
			Assert.AreEqual(origScrDraftsCount, m_scr.ArchivedDraftsOC.Count, "Number of ScrDrafts shouldn't change");
			Assert.AreEqual(2, draftReplacedBooks.BooksOS.Count);
			Assert.AreEqual(replacedBook, draftReplacedBooks.BooksOS[0]);
			Assert.AreEqual(65, draftReplacedBooks.BooksOS[1].CanonicalNum);
			Assert.AreEqual(1, draftNewBooks.BooksOS.Count);
			Assert.AreEqual(newBook, draftNewBooks.BooksOS[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test PrepareBookNotImportingVern when all imported books are in the archive. We expect
		/// that no archives will added, but books will be added to both the replaced books and
		/// the new book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("DesktopRequired")]
		public void PrepareBookNotImportingVern_AllBooksInArchive()
		{
			m_settings.ImportTranslation = false;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			Assert.IsNull(m_scr.FindBook(1), "This test is invalid if Genesis is in the test DB.");
			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");
			IStTxtPara scrHead1Para1 = GetFirstScriptureSectionHeadParaInBook(jude);
			Assert.IsNotNull(scrHead1Para1, "This test is invalid if we can't find a normal Scripture section for Jude 1:1.");

			List<SegmentInfo> al = new List<SegmentInfo>(7);
			// process an \id segment to import the BT for an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD", ImportDomain.BackTrans, new BCVRef(65, 0, 0)));
			al.Add(new SegmentInfo(@"\s", "Section head", ImportDomain.BackTrans, new BCVRef(65, 1, 1)));
			al.Add(new SegmentInfo(@"\p", "Paragraph BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1)));

			IScrDraft draftReplacedBooks = null;
			IScrDraft draftNewBooks = null;
			IScrBook replacedBook = null;
			IScrBook newBook = null;
			UndoableUnitOfWorkHelper.Do("bla undo", "bla redo", m_actionHandler, () =>
			{
				draftReplacedBooks = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(Properties.Resources.kstidSavedVersionDescriptionOriginal, ScrDraftType.ImportedVersion);
				draftNewBooks = Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(Properties.Resources.kstidStandardFormatImportSvDesc, ScrDraftType.ImportedVersion);
				// Add a single book to both ScrDrafts
				replacedBook = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(draftReplacedBooks.BooksOS, 65);
				newBook = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(draftNewBooks.BooksOS, 65);
			});

			int origScrDraftsCount = m_scr.ArchivedDraftsOC.Count;

			m_importMgr.ResetOriginalDrafts();
			m_importMgr.SimulateAcceptAllBooks = true;
			m_importMgr.CallImportWithUndoTask(al);
			Assert.AreEqual(origScrDraftsCount, m_scr.ArchivedDraftsOC.Count, "Number of ScrDrafts shouldn't change");
			Assert.AreEqual(1, draftReplacedBooks.BooksOS.Count);
			Assert.AreEqual(65, draftReplacedBooks.BooksOS[0].CanonicalNum);
			Assert.AreNotEqual(replacedBook, draftReplacedBooks.BooksOS[0], "Original book should NOT be the same");
			Assert.AreEqual(1, draftNewBooks.BooksOS.Count);
			Assert.AreEqual(newBook, draftNewBooks.BooksOS[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test BT-only import in two different writing systems. Only one backup copy of the
		/// original book is saved in the archive and the partial BT is left on
		/// the current version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("DesktopRequired")]
		public void BtUndoPhmAfterImportingBtJudInOtherWs()
		{
			m_settings.ImportTranslation = false;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			int origActCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			IScrBook philemon = m_scr.FindBook(57);
			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(philemon, "This test is invalid if Philemon isn't in the test DB.");
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");

			IStTxtPara scrHead1Para1 = null;
			int iScrHead1 = 0;
			foreach (IScrSection section in jude.SectionsOS)
			{
				if (!section.IsIntro)
				{
					scrHead1Para1 = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
					break;
				}
				iScrHead1++;
			}
			Assert.IsNotNull(scrHead1Para1, "This test is invalid if there is no Scripture section in Jude in the test DB.");
			string scrHead1Para1TextOrig = scrHead1Para1.Contents.Text;
			int scrHead1Para1OrigTransCount = scrHead1Para1.TranslationsOC.Count;
			Assert.AreEqual(1, scrHead1Para1OrigTransCount);
			Assert.IsNull(scrHead1Para1.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			int wsEn = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
			Assert.Greater(wsEn, 0, "Couldn't find Id of English WS in test DB.");
			int wsEs = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("es");
			Assert.Greater(wsEs, 0, "Couldn't find Id of Spanish WS in test DB.");

			List<SegmentInfo> al = new List<SegmentInfo>(3);
			// process a \id segment to import an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD English Back Trans", ImportDomain.BackTrans, new BCVRef(65, 0, 0), wsEn));
			al.Add(new SegmentInfo(@"\s", "English Section head BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1), wsEn));
			al.Add(new SegmentInfo(@"\id", "PHM Spanish Back Trans", ImportDomain.BackTrans, new BCVRef(57, 0, 0), wsEs));
			al.Add(new SegmentInfo(@"\s", "Spanish Section head BT", ImportDomain.BackTrans, new BCVRef(57, 1, 1), wsEs));

			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(1, m_importMgr.NewSavedVersions.Count, "We should only have a backup saved version, no imported version.");
			Assert.AreEqual(origActCount + 1, Cache.ActionHandlerAccessor.UndoableSequenceCount,
				"Should have one extra undo action.");
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(philemon.Hvo, m_scr.FindBook(57).Hvo, "Imported BT should not replace current version");
			Assert.AreEqual(jude.Hvo, m_scr.FindBook(65).Hvo, "Imported BT should not replace current version");

			Assert.AreEqual(0, m_importMgr.m_cDisplayImportedBooksDlgCalled);

			Assert.AreEqual(2, m_importMgr.UndoManager.BackupVersion.BooksOS.Count);
			Assert.AreEqual(57, m_importMgr.UndoManager.BackupVersion.BooksOS[0].CanonicalNum);
			Assert.AreEqual(65, m_importMgr.UndoManager.BackupVersion.BooksOS[1].CanonicalNum);
			Assert.IsNull(m_importMgr.UndoManager.ImportedVersion);
			Assert.AreEqual("&Undo Import", Cache.ActionHandlerAccessor.GetUndoText());
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());

			Cache.ActionHandlerAccessor.Undo();

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "The backup saved version should be gone.");

			IScrBook judeAfterUndo = m_scr.FindBook(65);
			Assert.AreEqual(jude.Hvo, judeAfterUndo.Hvo);

			IStTxtPara undoneScrHead1Para1 = ((IStTxtPara)judeAfterUndo.SectionsOS[iScrHead1].HeadingOA.ParagraphsOS[0]);
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
		[Category("DesktopRequired")]
		public void ErrorRestoresOriginal()
		{
			int origActCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			IScrBook phm = m_scr.FindBook(57);
			Assert.AreEqual(phm.Hvo, m_scr.ScriptureBooksOS.ToHvoArray()[0],
				"This test is invalid if Philemon isn't the first book in Scripture in the test DB.");
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			List<SegmentInfo> al = new List<SegmentInfo>(1);
			// process a \id segment to import an existing a book
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(57, 0, 0)));
			al.Add(new SegmentInfo(@"\c", "", ImportDomain.Main, new BCVRef(57, 1, 0)));
			al.Add(new SegmentInfo(@"\ip", "Intro paragraph blah-blah", ImportDomain.Main, new BCVRef(57, 1, 0)));

			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "New ScrDrafts should have been purged (TE-7040)");
			Assert.AreEqual(origActCount, Cache.ActionHandlerAccessor.UndoableSequenceCount,
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
		[Category("DesktopRequired")]
		public void BtForMissingBookRemovesEmptySavedVersion()
		{
			m_settings.ImportTranslation = true;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			int origActCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			IScrBook matt = m_scr.FindBook(40);
			Assert.IsNull(matt, "This test is invalid if Matthew is in the test DB.");
			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			List<SegmentInfo> al = new List<SegmentInfo>(1);
			// process a \id segment to import a BT for a non-existent a book
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.BackTrans, new BCVRef(40, 0, 0)));

			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "New ScrDrafts should have been purged.");
			Assert.AreEqual(origActCount + 1, Cache.ActionHandlerAccessor.UndoableSequenceCount,
				"One action to add/remove the backup saved version.");
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
		[Category("DesktopRequired")]
		public void CancelRemovesIncompleteBookFromRevisionList()
		{
			int origActCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			IScrBook phm = m_scr.FindBook(57);
			Assert.AreEqual(phm.Hvo, m_scr.ScriptureBooksOS.ToHvoArray()[0],
				"This test is invalid if Philemon isn't the first book in Scripture in the test DB.");

			List<SegmentInfo> al = new List<SegmentInfo>(1);
			// process a \id segment to import an existing a book
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(57, 0, 0)));

			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(origActCount, Cache.ActionHandlerAccessor.UndoableSequenceCount,
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
		//    int origSeqCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

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

		//    Assert.AreEqual(origSeqCount + 2, Cache.ActionHandlerAccessor.UndoableSequenceCount,
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
		[Category("DesktopRequired")]
		public void InsertBookAfterCancelledImport()
		{
			using (UndoableUnitOfWorkHelper uow = new UndoableUnitOfWorkHelper(m_actionHandler, "Remove book"))
			{
				// SETUP: We want only one book so delete everything else but one
				while (m_scr.ScriptureBooksOS.Count > 1)
					m_scr.ScriptureBooksOS.RemoveAt(0);
				uow.RollBack = false;
			}

			IScrBook onlyBookLeft = m_scr.ScriptureBooksOS[0];
			int iBookId = onlyBookLeft.CanonicalNum;

			List<SegmentInfo> al = new List<SegmentInfo>(1);
			// process a \id segment to import the only existing a book
			al.Add(new SegmentInfo(@"\id", "", ImportDomain.Main, new BCVRef(iBookId, 0, 0)));


			m_importMgr.CallImportWithUndoTask(al);

			using (UndoableUnitOfWorkHelper uow = new UndoableUnitOfWorkHelper(m_actionHandler, "Insert book"))
			{
				AddBookToMockedScripture(iBookId - 1, "Book name");
				uow.RollBack = false;
			}
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
		[Category("DesktopRequired")]
		public void DiffEditAsPartOfImport()
		{
			m_settings.ImportTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			int origActCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");

			List<SegmentInfo> al = new List<SegmentInfo>(3);
			// process a \id segment to import an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD", ImportDomain.Main, new BCVRef(65, 0, 0)));
			al.Add(new SegmentInfo(@"\s", "Section head", ImportDomain.Main, new BCVRef(65, 1, 1)));
			al.Add(new SegmentInfo(@"\p", "Contents", ImportDomain.Main, new BCVRef(65, 1, 1)));

			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "We should not have a backup saved version.");
			Assert.AreEqual(origActCount + 1, Cache.ActionHandlerAccessor.UndoableSequenceCount, "Should have 1 extra undo action.");
			Assert.AreEqual("&Undo Import", Cache.ActionHandlerAccessor.GetUndoText());
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
		[Category("DesktopRequired")]
		public void BtAttachesToImportedVersion()
		{
			m_settings.ImportTranslation = true;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			var origActCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			var jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");
			var cBooksOrig = m_scr.ScriptureBooksOS.Count;

			// process a \id segment to import an existing a book
			var al = new List<SegmentInfo>(3)
			{
				new SegmentInfo(@"\id", "JUD", ImportDomain.Main, new BCVRef(65, 0, 0)),
				new SegmentInfo(@"\s", "Section head", ImportDomain.Main, new BCVRef(65, 1, 1)),
				new SegmentInfo(@"\id", "JUD Back Trans", ImportDomain.BackTrans, new BCVRef(65, 0, 0)),
				new SegmentInfo(@"\s", "Section head BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1))
			};

			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(0, m_importMgr.NewSavedVersions.Count, "We should have an imported version but not a backup saved version.");
			Assert.AreEqual(origActCount + 1, Cache.ActionHandlerAccessor.UndoableSequenceCount, "Should have one extra undo action.");
			Assert.AreEqual("&Undo Import", Cache.ActionHandlerAccessor.GetUndoText());
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			//verify that the original book of Jude has not been replaced
			Assert.AreEqual(jude.Hvo, m_scr.FindBook(65).Hvo, "Imported BT should not replace current version");
			IStTxtPara sh1Para = ((IStTxtPara)jude.SectionsOS[0].HeadingOA.ParagraphsOS[0]);
			Assert.AreNotEqual("Section head", sh1Para.Contents.Text, "Import should not have affected orginal.");
			if (sh1Para.TranslationsOC.Count == 1)
			{
				// Make sure the original BT of the first section in Jude was not changed
				foreach (ICmTranslation trans in sh1Para.TranslationsOC)
				{
					Assert.AreNotEqual("Section head BT", trans.Translation.AnalysisDefaultWritingSystem.Text);
				}
			}

			Assert.IsNull(m_importMgr.UndoManager.ImportedVersion);
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
		[Category("DesktopRequired")]
		public void BtAttachesToCurrentVersion()
		{
			m_settings.ImportTranslation = false;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			int origActCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");

			IStTxtPara scrHead1Para1 = null;
			int iScrHead1 = 0;
			foreach (IScrSection section in jude.SectionsOS)
			{
				if (!section.IsIntro)
				{
					scrHead1Para1 = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
					break;
				}
				iScrHead1++;
			}
			Assert.IsNotNull(scrHead1Para1, "This test is invalid if there is no Scripture section in Jude in the test DB.");
			string scrHead1Para1TextOrig = scrHead1Para1.Contents.Text;
			int scrHead1Para1OrigTransCount = scrHead1Para1.TranslationsOC.Count;
			Assert.AreEqual(1, scrHead1Para1OrigTransCount, "This test is invalid if the first paragraph of the first Scripture section head in Jude has a backtranslation in the test DB.");

			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			List<SegmentInfo> al = new List<SegmentInfo>(3);
			// process a \id segment to import an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD Back Trans", ImportDomain.BackTrans, new BCVRef(65, 0, 0)));
			al.Add(new SegmentInfo(@"\s", "Section head BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1)));

			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(1, m_importMgr.NewSavedVersions.Count, "We should only have a backup saved version, no imported version.");
			Assert.AreEqual(origActCount + 1, Cache.ActionHandlerAccessor.UndoableSequenceCount,
				"Should have 1 extra undo actions.");
			Assert.AreEqual("&Undo Import", Cache.ActionHandlerAccessor.GetUndoText());
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(jude.Hvo, m_scr.FindBook(65).Hvo, "Imported BT should not replace current version");
			IStTxtPara sh1Para = ((IStTxtPara)jude.SectionsOS[iScrHead1].HeadingOA.ParagraphsOS[0]);
			Assert.AreEqual(scrHead1Para1TextOrig, sh1Para.Contents.Text, "Import should not have affected orginal vernacular.");
			if (sh1Para.TranslationsOC.Count == 1)
			{
				// Make sure the original BT of the first section in Jude was changed
				foreach (ICmTranslation trans in sh1Para.TranslationsOC)
					Assert.AreEqual("Section head BT", trans.Translation.AnalysisDefaultWritingSystem.Text);
			}

			Assert.IsNull(m_importMgr.UndoManager.ImportedVersion);

			IScrDraft backupSv = m_importMgr.UndoManager.BackupVersion;
			Assert.IsNotNull(backupSv);
			Assert.AreEqual(1, backupSv.BooksOS.Count);
			IScrBook backupJude = backupSv.BooksOS[0];
			Assert.AreEqual(65, backupJude.CanonicalNum);
			IStTxtPara bkpScrHead1Para1 = ((IStTxtPara)backupJude.SectionsOS[iScrHead1].HeadingOA.ParagraphsOS[0]);
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
		[Category("DesktopRequired")]
		public void BtsForMultipleWss()
		{
			m_settings.ImportTranslation = false;
			m_settings.ImportBackTranslation = true;
			MockScrObjWrapper.s_fSimulateCancel = false;

			int origActCount = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			IScrBook jude = m_scr.FindBook(65);
			Assert.IsNotNull(jude, "This test is invalid if Jude isn't in the test DB.");

			IStTxtPara scrHead1Para1 = null;
			int iScrHead1 = 0;
			foreach (IScrSection section in jude.SectionsOS)
			{
				if (!section.IsIntro)
				{
					scrHead1Para1 = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
					break;
				}
				iScrHead1++;
			}
			Assert.IsNotNull(scrHead1Para1, "This test is invalid if there is no Scripture section in Jude in the test DB.");
			string scrHead1Para1TextOrig = scrHead1Para1.Contents.Text;
			int scrHead1Para1OrigTransCount = scrHead1Para1.TranslationsOC.Count;
			Assert.AreEqual(1, scrHead1Para1OrigTransCount, "This test is invalid if the first paragraph of the first Scripture section head in Jude has a backtranslation in the test DB.");

			int cBooksOrig = m_scr.ScriptureBooksOS.Count;

			int wsEn = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
			Assert.Greater(wsEn, 0, "Couldn't find Id of English WS in test DB.");
			int wsEs = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("es");
			Assert.Greater(wsEs, 0, "Couldn't find Id of Spanish WS in test DB.");

			List<SegmentInfo> al = new List<SegmentInfo>(3);
			// process a \id segment to import an existing a book
			al.Add(new SegmentInfo(@"\id", "JUD English Back Trans", ImportDomain.BackTrans, new BCVRef(65, 0, 0), wsEn));
			al.Add(new SegmentInfo(@"\s", "English Section head BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1), wsEn));
			al.Add(new SegmentInfo(@"\id", "JUD Spanish Back Trans", ImportDomain.BackTrans, new BCVRef(65, 0, 0), wsEs));
			al.Add(new SegmentInfo(@"\s", "Spanish Section head BT", ImportDomain.BackTrans, new BCVRef(65, 1, 1), wsEs));

			m_importMgr.CallImportWithUndoTask(al);

			Assert.AreEqual(1, m_importMgr.NewSavedVersions.Count, "We should only have a backup saved version, no imported version.");
			Assert.AreEqual(origActCount + 1, Cache.ActionHandlerAccessor.UndoableSequenceCount,
				"Should have 1 extra undo actions.");
			Assert.AreEqual("&Undo Import", Cache.ActionHandlerAccessor.GetUndoText());
			Assert.AreEqual(cBooksOrig, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(jude.Hvo, m_scr.FindBook(65).Hvo, "Imported BT should not replace current version");
			IStTxtPara sh1Para = ((IStTxtPara)jude.SectionsOS[iScrHead1].HeadingOA.ParagraphsOS[0]);
			Assert.AreEqual(scrHead1Para1TextOrig, sh1Para.Contents.Text, "Import should not have affected orginal vernacular.");
			if (sh1Para.TranslationsOC.Count == 1)
			{
				// Make sure the original BT of the first section in Jude was changed
				foreach (ICmTranslation trans in sh1Para.TranslationsOC)
				{
					Assert.AreEqual("English Section head BT", trans.Translation.get_String(wsEn).Text);
					Assert.AreEqual("Spanish Section head BT", trans.Translation.get_String(wsEs).Text);
				}
			}

			Assert.IsNull(m_importMgr.UndoManager.ImportedVersion);

			IScrDraft backupSv = m_importMgr.UndoManager.BackupVersion;
			Assert.IsNotNull(backupSv);
			Assert.AreEqual(1, backupSv.BooksOS.Count);
			IScrBook backupJude = backupSv.BooksOS[0];
			Assert.AreEqual(65, backupJude.CanonicalNum);
			IStTxtPara bkpScrHead1Para1 = ((IStTxtPara)backupJude.SectionsOS[iScrHead1].HeadingOA.ParagraphsOS[0]);
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
		private static IScrTxtPara GetFirstScriptureSectionHeadParaInBook(IScrBook book)
		{
			IScrTxtPara scrHead1Para1 = null;
			BCVRef targetRef = new BCVRef(book.CanonicalNum, 1, 1);
			foreach (IScrSection section in book.SectionsOS)
			{
				if (!section.IsIntro && section.VerseRefStart == targetRef)
				{
					scrHead1Para1 = ((IScrTxtPara)section.HeadingOA.ParagraphsOS[0]);
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
#endif
}