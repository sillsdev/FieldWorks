// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SFFileListBuilderTest.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	#region DummyScrImportFileInfo
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Makes it possible to construct a ScrImportFileInfo without actually creating and
	/// scanning a file.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyScrImportFileInfo : ScrImportFileInfo
	{
		/// <summary>set true to make it look like all files do not exist.</summary>
		public static bool s_alwaysPretendFileDoesNotExist = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Construct a DummyScrImportFileInfo.
		/// </summary>
		/// <param name="fileName">Name of the file whose info this represents</param>
		/// <param name="domain">The import domain to which this file belongs</param>
		/// <param name="icuLocale">The ICU locale of the source to which this file belongs
		/// (null for Scripture source)</param>
		/// <param name="noteType">The CmAnnotationDefn of the source to which this file belongs
		/// (only used for Note sources)</param>
		/// <param name="booksInFile">A list of integers representing 1-based canonical book
		/// numbers that are in this file</param>
		/// <param name="fileEncoding">The file encoding</param>
		/// <param name="startRef">The first reference encountered in the file</param>
		/// ------------------------------------------------------------------------------------
		public DummyScrImportFileInfo(string fileName, ImportDomain domain, string icuLocale,
			ICmAnnotationDefn noteType, List<int> booksInFile, Encoding fileEncoding, ScrReference startRef) :
			base(fileName, null, domain, icuLocale, noteType, false)
		{
			m_booksInFile = booksInFile;
			m_fileEncoding = fileEncoding;
			m_startRef = startRef;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the object. This override does not attempt to actually read the file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Initialize()
		{
			m_fileEncoding = Encoding.ASCII;
			m_isReadable = !(FileName.StartsWith("NONEXIST") || s_alwaysPretendFileDoesNotExist);
		}
	}
	#endregion

	#region DummySFFileListBuilder
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummySFFileListBuilder : SFFileListBuilder
	{
		/// <summary>
		/// List of files that will be returned by QueryUserForNewFilenames when the add button
		/// is clicked.
		/// </summary>
		public string[] m_filenamesToAdd;
		/// <summary>
		/// Stores the last error message that was "displayed" by DisplayMessageBox
		/// </summary>
		public string m_lastErrorMessage = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="filename"></param>
		/// ------------------------------------------------------------------------------------
		protected override void ShowBadFileMessage(string filename)
		{
			// do not show the message during the test
		}

		/// -------------------------------------------------------------------------------
		/// <summary>
		/// Add the given SF scripture files to the list and select the first one if none
		/// is selected.
		/// </summary>
		/// <param name="files">An IEnumerable of ScrImportFileInfo objects to be added
		/// </param>
		/// -------------------------------------------------------------------------------
		public void CallAddFilesToListView(IEnumerable files)
		{
			CheckDisposed();

			base.AddFilesToListView(ScrListView, files, null, null);
		}

		/// -------------------------------------------------------------------------------
		/// <summary>
		/// Add the given SF scripture file to the list and select the first one if none
		/// is selected.
		/// </summary>
		/// <param name="listView">The list view to add the files to</param>
		/// <param name="fileInfo">list of files to be added</param>
		/// <param name="warnOnError">display an error if a file is not valid and
		/// do not add it to the list.  If this is false then invalid files will
		/// be added to the list anyway.</param>
		/// -------------------------------------------------------------------------------
		public void CallAddFileToListView(ListView listView, ScrImportFileInfo fileInfo,
			bool warnOnError)
		{
			CheckDisposed();

			base.AddFileToListView(listView, fileInfo, warnOnError);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes method PopulateFileListsFromSettings, which loads all the file lists with
		/// the files in the Import settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallPopulateFileListsFromSettings()
		{
			CheckDisposed();

			base.PopulateFileListsFromSettings();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate clicking on the Remove button to remove the selected item
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ClickRemoveButton()
		{
			CheckDisposed();

			base.btnRemove_Click(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate clicking on the Add button to add whatever files are returned by
		/// QueryUserForNewFilenames.
		/// </summary>
		/// <remarks>Need to set m_filenamesToAdd before calling this.</remarks>
		/// ------------------------------------------------------------------------------------
		public void ClickAddButton()
		{
			CheckDisposed();

			base.btnAdd_Click(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the current remove button is enabled
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsRemoveButtonEnabled
		{
			get
			{
				CheckDisposed();
				return m_currentRemoveButton.Enabled;
			}
		}

		/// -------------------------------------------------------------------------------
		/// <summary>
		/// Overriden for testing.
		/// </summary>
		/// <returns>Array of files to add</returns>
		/// <remarks>Need to set m_filenamesToAdd before this will work.</remarks>
		/// -------------------------------------------------------------------------------
		protected override string[] QueryUserForNewFilenames()
		{
			return m_filenamesToAdd;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the Show Writing System combo box on the BT tab
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ComboBox ShowBtWritingSystemCombo
		{
			get
			{
				CheckDisposed();
				return cboShowBtWritingSystem;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate User selecting a tab
		/// </summary>
		/// <param name="tab">Integer index of tab (0, 1, or 2)</param>
		/// ------------------------------------------------------------------------------------
		public void SelectTab(int tab)
		{
			CheckDisposed();

			tabFileGroups.SelectedIndex = tab;
			tabFileGroups_SelectedIndexChanged(tabFileGroups, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate User selecting a writing system on BT tab
		/// </summary>
		/// <param name="wsName"></param>
		/// ------------------------------------------------------------------------------------
		public void SelectBtWritingSystem(string wsName)
		{
			CheckDisposed();

			cboShowBtWritingSystem.SelectedIndex = cboShowBtWritingSystem.FindStringExact(wsName);
			cboShowWritingSystem_SelectedIndexChanged(cboShowBtWritingSystem, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate User selecting a writing system on Annotations tab
		/// </summary>
		/// <param name="wsName"></param>
		/// ------------------------------------------------------------------------------------
		public void SelectNotesWritingSystem(string wsName)
		{
			CheckDisposed();

			cboShowNotesWritingSystem.SelectedIndex = cboShowNotesWritingSystem.FindStringExact(wsName);
			cboShowWritingSystem_SelectedIndexChanged(cboShowNotesWritingSystem, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate User selecting a note type on the Annotations tab
		/// </summary>
		/// <param name="noteTypeName">Name of the note type to select</param>
		/// ------------------------------------------------------------------------------------
		public void SelectNoteType(string noteTypeName)
		{
			CheckDisposed();

			cboShowNoteTypes.SelectedIndex = cboShowNoteTypes.FindStringExact(noteTypeName);
			cboShowNoteTypes_SelectedIndexChanged(cboShowNoteTypes, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Virtual to allow overriding for tests
		/// </summary>
		/// <param name="message">Message to display</param>
		/// ------------------------------------------------------------------------------------
		protected override void DisplayMessageBox(string message)
		{
			m_lastErrorMessage = message;
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class containing tests for SFFileListBuilder.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class SFFileListBuilderTests : ScrInMemoryFdoTestBase
	{
		#region data members
		private DummySFFileListBuilder m_builder;
		private IScrImportSet m_settings;
		private ScrMappingList m_mappingList;
		#endregion

		#region Setup and tear down

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			IWritingSystem wsSpanish;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out wsSpanish);
			IWritingSystem wsGerman;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("de", out wsGerman);
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(wsSpanish);
				Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Add(wsSpanish);
				Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(wsGerman);
				Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Add(wsGerman);
			});
			m_mappingList = new ScrMappingList(MappingSet.Main, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates test data and initializes the DummySFFileListBuilder so we can test it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_settings = m_scr.FindOrCreateDefaultImportSettings(TypeOfImport.Other);
			m_builder = new DummySFFileListBuilder();
			m_builder.ImportSettings = m_settings;
			DummyScrImportFileInfo.s_alwaysPretendFileDoesNotExist = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the FDO cache
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_builder.Dispose();
			m_builder = null;
			m_settings = null;
			m_mappingList = null;

			base.TestTearDown();
		}
		#endregion

		#region AddFiles Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adding a null array does nothing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddNullArray()
		{
			m_builder.CallAddFilesToListView(null);
			Assert.AreEqual(0, m_builder.ScrListView.Items.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AddFiles method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddFilesToList()
		{
			DummyScrImportFileInfo[] files =
			{
				new DummyScrImportFileInfo("a1", ImportDomain.Main, null, null,
					new List<int>(new int[] {1}), Encoding.ASCII, new ScrReference(4, 1, 0, m_scr.Versification)),
				new DummyScrImportFileInfo("b1", ImportDomain.Main, null, null,
					new List<int>(new int[] {2, 3}), Encoding.ASCII, new ScrReference(3, 1, 0, m_scr.Versification))
			};

			m_builder.CallAddFilesToListView(files);
			Assert.IsTrue(m_builder.ScrListView.Items[0].Selected);
			Assert.AreEqual(files.Length, m_builder.ScrListView.Items.Count);
			Assert.AreEqual(files[1].FileName, m_builder.ScrListView.Items[0].Text);
			Assert.AreEqual(files[0].FileName, m_builder.ScrListView.Items[1].Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding files that are already in the list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddFilesAlreadyExist()
		{
			DummyScrImportFileInfo[] files =
			{
				new DummyScrImportFileInfo("a1", ImportDomain.Main, null, null,
					new List<int>(new int[] {1}), Encoding.ASCII, new ScrReference()),
				new DummyScrImportFileInfo("b1", ImportDomain.Main, null, null,
					new List<int>(new int[] {2, 3}), Encoding.ASCII, new ScrReference())
			};

			// Add the same files twice
			m_builder.CallAddFilesToListView(files);
			m_builder.CallAddFileToListView(m_builder.ScrListView, files[0], true);
			m_builder.CallAddFileToListView(m_builder.ScrListView, files[1], true);
			Assert.AreEqual(files.Length, m_builder.ScrListView.Items.Count);
			Assert.IsTrue(m_builder.ScrListView.Items[0].Selected);
			Assert.AreEqual(files[0].FileName, m_builder.ScrListView.Items[0].Text);
			Assert.AreEqual(files[1].FileName, m_builder.ScrListView.Items[1].Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that expected text encodings are displayed for added files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CheckFileTextEncodings()
		{
			DummyScrImportFileInfo[] files =
			{
				new DummyScrImportFileInfo("a1", ImportDomain.Main, null, null,
					new List<int>(new int[] {1}), Encoding.Unicode, new ScrReference()),
				new DummyScrImportFileInfo("b2", ImportDomain.Main, null, null,
					new List<int>(new int[] {2}), Encoding.UTF8, new ScrReference()),
				new DummyScrImportFileInfo("c3", ImportDomain.Main, null, null,
					new List<int>(new int[] {3}), Encoding.BigEndianUnicode, new ScrReference())
			};

			m_builder.CallAddFilesToListView(files);
			Assert.AreEqual(3, m_builder.ScrListView.Items.Count);

			ListViewItem lvi;
			ListViewItem.ListViewSubItem subItem;

			lvi = m_builder.ScrListView.Items[0];
			subItem = lvi.SubItems[2];
			Assert.AreEqual(files[0].FileName, lvi.Text);
			Assert.AreEqual("Unicode", subItem.Text);

			lvi = m_builder.ScrListView.Items[1];
			subItem = lvi.SubItems[2];
			Assert.AreEqual(files[1].FileName, lvi.Text);
			Assert.AreEqual("Unicode (UTF-8)", subItem.Text);

			lvi = m_builder.ScrListView.Items[2];
			subItem = lvi.SubItems[2];
			Assert.AreEqual(files[2].FileName, lvi.Text);
			Assert.AreEqual("Unicode (Big-Endian)", subItem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test what happens when adding an invalid file that the user selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddFilesWithoutBooks_User()
		{
			TempSFFileMaker fileMaker = new TempSFFileMaker();
			string file1 = fileMaker.CreateFile("EPH", new string[] {@"\c 1", @"\v 1"});
			string file2 = fileMaker.CreateFile("1CO", new string[] {@"\c 1", @"\v 1"});
			string file3 = fileMaker.CreateFileNoID(new string[] {@"\_sh"});

			// Make sure only two files were added
			m_builder.m_filenamesToAdd = new string[] {file1, file2, file3};
			m_builder.ClickAddButton();
			Assert.AreEqual(2, m_builder.ScriptureFiles.Count);
			Assert.AreEqual(2, m_builder.ScrListView.Items.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test what happens when adding non-existent files from the database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddNonExistentFiles()
		{
			DummyScrImportFileInfo[] files =
			{
				new DummyScrImportFileInfo("NONEXISTa1", ImportDomain.Main, null, null,
					new List<int>(new int[] {1}), Encoding.ASCII, new ScrReference()),
				new DummyScrImportFileInfo("NONEXISTb2", ImportDomain.Main, null, null,
					new List<int>(new int[] {2}), Encoding.ASCII, new ScrReference()),
				new DummyScrImportFileInfo("ExistsC3", ImportDomain.Main, null, null,
					new List<int>(new int[] {3}), Encoding.ASCII, new ScrReference()),
			};

			m_builder.CallAddFilesToListView(files);

			Assert.AreEqual(files.Length, m_builder.ScrListView.Items.Count);

			Assert.IsTrue(m_builder.ScrListView.Items[0].Selected);
			Assert.AreEqual(files[0].FileName, m_builder.ScrListView.Items[0].Text);
			Assert.IsFalse(((ScrImportFileInfo)m_builder.ScrListView.Items[0].Tag).IsReadable);
			Assert.AreEqual(files[1].FileName, m_builder.ScrListView.Items[1].Text);
			Assert.IsFalse(((ScrImportFileInfo)m_builder.ScrListView.Items[1].Tag).IsReadable);
			Assert.AreEqual(files[2].FileName, m_builder.ScrListView.Items[2].Text);
			Assert.IsTrue(((ScrImportFileInfo)m_builder.ScrListView.Items[2].Tag).IsReadable);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test re-adding files that were previously unavailable
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReAddingNonExistentFiles()
		{
			DummyScrImportFileInfo[] files =
			{
				new DummyScrImportFileInfo("a1", ImportDomain.Main, null, null,
					new List<int>(new int[] {1}), Encoding.ASCII, new ScrReference()),
			};
			DummyScrImportFileInfo.s_alwaysPretendFileDoesNotExist = false;

			m_builder.CallAddFilesToListView(files);

			// now pretend the files exist
			DummyScrImportFileInfo.s_alwaysPretendFileDoesNotExist = true;

			m_builder.CallAddFilesToListView(files);

			// Verify the result
			Assert.IsTrue(m_builder.ScrListView.Items[0].Selected);
			Assert.AreEqual(files.Length, m_builder.ScrListView.Items.Count);
			Assert.IsTrue(((ScrImportFileInfo)m_builder.ScrListView.Items[0].Tag).IsReadable,
				"Existing file should have been updated in list");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test adding files to each of the tabbed lists
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddingFilesToDifferentLists()
		{
			TempSFFileMaker maker = new TempSFFileMaker();
			string scrFileName = maker.CreateFile("GEN", new string[] {@"\c 1", @"\v 1"});
			m_settings.AddFile(scrFileName, ImportDomain.Main, null, null);

			string enBtFileName = maker.CreateFile("GEN", new string[] {@"\c 1", @"\v 1"});
			string esBtFileName = maker.CreateFile("MAT",
				new string[] {@"\c 1", @"\v 1", @"\id MRK", @"\c 1", @"\v 1"});
			m_settings.AddFile(enBtFileName, ImportDomain.BackTrans, "en", null);
			m_settings.AddFile(esBtFileName, ImportDomain.BackTrans, "es", null);

			string esTransNoteFileName = maker.CreateFile("GEN",
				new string[] {@"\c 1", @"\v 1 No digan asi."});
			string enTransNoteFileName = maker.CreateFile("GEN",
				new string[] {@"\c 1", @"\v 1 Try to find a better word."});
			string enConsNoteFileName1 = maker.CreateFile("GEN",
				new string[] {@"\c 1", @"\v 1 Check the meaning of floobywump."});
			string enConsNoteFileName2 = maker.CreateFile("MAT",
				new string[] {@"\c 1", @"\v 1", @"\id MRK", @"\c 1", @"\v 1"});
			ICmAnnotationDefn translatorNoteDef =
				Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().TranslatorAnnotationDefn;
			ICmAnnotationDefn consultantNoteDef =
				Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().ConsultantAnnotationDefn;
			m_settings.AddFile(esTransNoteFileName, ImportDomain.Annotations, "es",
				translatorNoteDef);
			m_settings.AddFile(enTransNoteFileName, ImportDomain.Annotations, "en",
				translatorNoteDef);

			m_settings.AddFile(enConsNoteFileName1, ImportDomain.Annotations, "en",
				consultantNoteDef);
			m_settings.AddFile(enConsNoteFileName2, ImportDomain.Annotations, "en",
				consultantNoteDef);

			m_builder.CallPopulateFileListsFromSettings();

			// Verify the Scripture file
			m_builder.SelectTab(0);
			Assert.AreEqual(1, m_builder.ScrListView.Items.Count);
			Assert.AreEqual(scrFileName, m_builder.ScrListView.Items[0].Text);

			// Verify the English BT file
			m_builder.SelectTab(1);
			m_builder.SelectBtWritingSystem("English");
			Assert.AreEqual(1, m_builder.BtListView.Items.Count);
			Assert.AreEqual(enBtFileName, m_builder.BtListView.Items[0].Text);
			Assert.AreEqual("GEN", m_builder.BtListView.Items[0].SubItems[1].Text);
//				Assert.AreEqual("English", m_builder.BtListView.Items[0].SubItems[2].Text);
			Assert.AreEqual("US-ASCII", m_builder.BtListView.Items[0].SubItems[2].Text);

			// Verify the Spanish BT file
			m_builder.SelectBtWritingSystem("Spanish");
			Assert.AreEqual(1, m_builder.BtListView.Items.Count);
			Assert.AreEqual(esBtFileName, m_builder.BtListView.Items[0].Text);
			Assert.AreEqual("MAT, MRK", m_builder.BtListView.Items[0].SubItems[1].Text);
//				Assert.AreEqual("Spanish", m_builder.BtListView.Items[0].SubItems[2].Text);
			Assert.AreEqual("US-ASCII", m_builder.BtListView.Items[0].SubItems[2].Text);

			// verify the Spanish Translator Notes file
			m_builder.SelectTab(2);
			m_builder.SelectNoteType("Translator");
			m_builder.SelectNotesWritingSystem("Spanish");
			Assert.AreEqual(1, m_builder.NotesListView.Items.Count);
			Assert.AreEqual(esTransNoteFileName, m_builder.NotesListView.Items[0].Text);
			Assert.AreEqual("GEN", m_builder.NotesListView.Items[0].SubItems[1].Text);
//				Assert.AreEqual("Spanish", m_builder.NotesListView.Items[0].SubItems[2].Text);
			Assert.AreEqual("US-ASCII", m_builder.NotesListView.Items[0].SubItems[2].Text);

			// verify the English Translator Notes file
			m_builder.SelectNotesWritingSystem("English");
			Assert.AreEqual(1, m_builder.NotesListView.Items.Count);
			Assert.AreEqual(enTransNoteFileName, m_builder.NotesListView.Items[0].Text);
			Assert.AreEqual("GEN", m_builder.NotesListView.Items[0].SubItems[1].Text);
//				Assert.AreEqual("Spanish", m_builder.NotesListView.Items[0].SubItems[2].Text);
			Assert.AreEqual("US-ASCII", m_builder.NotesListView.Items[0].SubItems[2].Text);

			// verify the English Consultant Notes files
			m_builder.SelectNoteType("Consultant");
			Assert.AreEqual(2, m_builder.NotesListView.Items.Count);
			Assert.AreEqual(enConsNoteFileName1, m_builder.NotesListView.Items[0].Text);
			Assert.AreEqual("GEN", m_builder.NotesListView.Items[0].SubItems[1].Text);
			//				Assert.AreEqual("Spanish", m_builder.NotesListView.Items[0].SubItems[2].Text);
			Assert.AreEqual("US-ASCII", m_builder.NotesListView.Items[0].SubItems[2].Text);

			Assert.AreEqual(enConsNoteFileName2, m_builder.NotesListView.Items[1].Text);
			Assert.AreEqual("MAT, MRK", m_builder.NotesListView.Items[1].SubItems[1].Text);
			//				Assert.AreEqual("Spanish", m_builder.NotesListView.Items[1].SubItems[2].Text);
			Assert.AreEqual("US-ASCII", m_builder.NotesListView.Items[1].SubItems[2].Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test what happens when adding valid files to the back translation list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddFilesToBtList_User()
		{
			m_builder.SelectTab(1);
			TempSFFileMaker fileMaker = new TempSFFileMaker();
			string file1 = fileMaker.CreateFile("EPH", new string[] {@"\c 1", @"\v 1"});
			string file2 = fileMaker.CreateFile("1CO", new string[] {@"\c 1", @"\v 1"});

			m_builder.m_filenamesToAdd = new string[] {file1, file2};
			m_builder.SelectBtWritingSystem("Spanish");
			m_builder.ClickAddButton();
			// Make sure two files were added to BT list
			Assert.AreEqual(2, m_builder.BackTransFiles.Count);
			Assert.AreEqual(2, m_builder.BtListView.Items.Count);
			foreach (ScrImportFileInfo info in m_settings.GetImportFiles(ImportDomain.BackTrans))
			{
				Assert.AreEqual("es", info.WsId);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test what happens when adding valid files to the back translation list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddFilesToNotesList_User()
		{
			m_builder.SelectTab(2);
			TempSFFileMaker fileMaker = new TempSFFileMaker();
			string file1 = fileMaker.CreateFile("GEN", new string[] {@"\c 1", @"\v 1"});
			string file2 = fileMaker.CreateFile("GEN", new string[] {@"\c 1", @"\v 1"});
			string file3 = fileMaker.CreateFile("EXO", new string[] {@"\c 1", @"\v 1"});

			m_builder.m_filenamesToAdd = new string[] {file1};
			m_builder.SelectNotesWritingSystem("German");
			m_builder.SelectNoteType("Translator");
			m_builder.ClickAddButton();
			m_builder.m_filenamesToAdd = new string[] {file2, file3};
			m_builder.SelectNotesWritingSystem("German");
			m_builder.SelectNoteType("Consultant");
			m_builder.ClickAddButton();

			// Make sure two files were added to the German Consultant Notes list
			Assert.AreEqual(2, m_builder.NotesFiles.Count);
			Assert.AreEqual(2, m_builder.NotesListView.Items.Count);

			m_builder.SelectNoteType("Translator");
			// Make sure one file was added to the German Translator Notes list
			Assert.AreEqual(1, m_builder.NotesFiles.Count);
			Assert.AreEqual(1, m_builder.NotesListView.Items.Count);

			// Make sure all three annotation files were added properly to the import project
			ImportFileSource files = m_settings.GetImportFiles(ImportDomain.Annotations);
			Assert.AreEqual(3, files.Count);
			ICmAnnotationDefn translatorNoteDef =
				Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().TranslatorAnnotationDefn;
			ICmAnnotationDefn consultantNoteDef =
				Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>().ConsultantAnnotationDefn;

			foreach (ScrImportFileInfo info in m_settings.GetImportFiles(ImportDomain.Annotations))
			{
				Assert.AreEqual("de", info.WsId);
				if (info.FileName == file1)
				{
					Assert.AreEqual(new ScrReference(1, 1, 1, m_scr.Versification), info.StartRef);
					Assert.AreEqual(translatorNoteDef, info.NoteType);
				}
				else if (info.FileName == file2)
				{
					Assert.AreEqual(new ScrReference(1, 1, 1, m_scr.Versification), info.StartRef);
					Assert.AreEqual(consultantNoteDef, info.NoteType);
				}
				else if (info.FileName == file3)
				{
					Assert.AreEqual(new ScrReference(2, 1, 1, m_scr.Versification), info.StartRef);
					Assert.AreEqual(consultantNoteDef, info.NoteType);
				}
				else
				{
					Assert.Fail("Unexpected file in annotations import project: " + info.FileName);
				}
			}
		}
		#endregion

		#region RemoveFiles tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test removing files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveFilesFromList()
		{
			DummyScrImportFileInfo[] files =
			{
				new DummyScrImportFileInfo("a1", ImportDomain.Main, null, null,
					new List<int>(new int[] {1}), Encoding.ASCII, new ScrReference()),
				new DummyScrImportFileInfo("b2", ImportDomain.Main, null, null,
					new List<int>(new int[] {2}), Encoding.ASCII, new ScrReference()),
				new DummyScrImportFileInfo("c3", ImportDomain.Main, null, null,
					new List<int>(new int[] {3}), Encoding.ASCII, new ScrReference())
			};

			m_builder.CreateControl();
			m_builder.CallAddFilesToListView(files);
			Assert.AreEqual(3, m_builder.ScrListView.Items.Count);

			// Remove the middle item in the list
			m_builder.ScrListView.Items[0].Selected = false;
			m_builder.ScrListView.Items[1].Selected = true;
			Assert.IsTrue(m_builder.IsRemoveButtonEnabled);
			m_builder.ClickRemoveButton();

			Assert.AreEqual(2, m_builder.ScrListView.Items.Count);
			Assert.AreEqual(files[0].FileName, m_builder.ScrListView.Items[0].Text);
			Assert.AreEqual(files[2].FileName, m_builder.ScrListView.Items[1].Text);
			Assert.AreEqual(1, m_builder.ScrListView.SelectedItems.Count);
			Assert.IsTrue(m_builder.IsRemoveButtonEnabled);

			// Now remove the last two items so the list is empty
			m_builder.ClickRemoveButton();
			Assert.IsTrue(m_builder.IsRemoveButtonEnabled);
			m_builder.ClickRemoveButton();
			Assert.AreEqual(0, m_builder.ScrListView.Items.Count);
			Assert.AreEqual(0, m_builder.ScrListView.SelectedItems.Count);
			Assert.IsFalse(m_builder.IsRemoveButtonEnabled);
		}
		#endregion

		#region Validity Tests
		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Check to make sure that Valid returns false when no files exist.
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void Valid_AllMissingFiles()
		{
			// Add files that do not exist
			DummyScrImportFileInfo[] files =
			{
				new DummyScrImportFileInfo("NONEXISTa1", ImportDomain.Main, null, null,
					new List<int>(new int[] {1}), Encoding.ASCII, new ScrReference(4, 1, 0, m_scr.Versification)),
				new DummyScrImportFileInfo("NONEXISTa2", ImportDomain.Main, null, null,
					new List<int>(new int[] {2, 3}), Encoding.ASCII, new ScrReference(3, 1, 0, m_scr.Versification))
			};
			m_builder.CallAddFilesToListView(files);

			// now do the test
			Assert.IsFalse(m_builder.Valid());
			Assert.IsNull(m_builder.m_lastErrorMessage);
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Check to make sure that PrepareOtherProjectProxy will pass when some of the
		/// files do not exist and some do.
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void FileEncodingsAreValid_SomeMissingFiles()
		{
			// Add 1 file that does not exist and 1 that does.
			DummyScrImportFileInfo[] files =
			{
				new DummyScrImportFileInfo("NONEXISTa1", ImportDomain.Main, null, null,
					new List<int>(new int[] {1}), Encoding.ASCII, new ScrReference(4, 1, 0, m_scr.Versification)),
				new DummyScrImportFileInfo("a2", ImportDomain.Main, null, null,
					new List<int>(new int[] {2, 3}), Encoding.UTF8, new ScrReference(3, 1, 0, m_scr.Versification))
			};
			m_builder.CallAddFilesToListView(files);

			// now do the test
			Assert.IsTrue(m_builder.FileEncodingsAreValid());
			Assert.IsNull(m_builder.m_lastErrorMessage);
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Tests TE-333: test that UTF16 Little-endian files are ok
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void DisplayNoErrorForUTF16LE()
		{
			// Add 1 file that does not exist and 1 that does.
			DummyScrImportFileInfo[] files =
			{
				new DummyScrImportFileInfo("a", ImportDomain.Main, null, null,
					new List<int>(new int[] {2, 3}), Encoding.Unicode, new ScrReference(3, 1, 0, m_scr.Versification))
			};
			m_builder.CallAddFilesToListView(files);

			// now do the test
			Assert.IsTrue(m_builder.FileEncodingsAreValid());
			Assert.IsNull(m_builder.m_lastErrorMessage);
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Tests TE-333: test that UTF16 Big-endian files are ok
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void DisplayNoErrorForUTF16BE()
		{
			DummyScrImportFileInfo[] files =
			{
				new DummyScrImportFileInfo("a", ImportDomain.Main, null, null,
					new List<int>(new int[] {2, 3}), Encoding.BigEndianUnicode, new ScrReference(3, 1, 0, m_scr.Versification))
			};
			m_builder.CallAddFilesToListView(files);

			// now do the test
			Assert.IsTrue(m_builder.FileEncodingsAreValid());
			Assert.IsNull(m_builder.m_lastErrorMessage);
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Tests TE-333: test that ASCII files are ok
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void DisplayNoErrorForLegacy()
		{
			DummyScrImportFileInfo[] files =
			{
				new DummyScrImportFileInfo("a", ImportDomain.Main, null, null,
					new List<int>(new int[] {2, 3}), Encoding.ASCII, new ScrReference(3, 1, 0, m_scr.Versification))
			};
			m_builder.CallAddFilesToListView(files);

			// now do the test
			Assert.IsTrue(m_builder.FileEncodingsAreValid());
			Assert.IsNull(m_builder.m_lastErrorMessage);
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Tests TE-333: test that UTF8 files are ok
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void DisplayNoErrorForUTF8()
		{
			DummyScrImportFileInfo[] files =
			{
				new DummyScrImportFileInfo("a", ImportDomain.Main, null, null,
					new List<int>(new int[] {2, 3}), Encoding.UTF8, new ScrReference(3, 1, 0, m_scr.Versification))
			};
			m_builder.CallAddFilesToListView(files);

			// now do the test
			Assert.IsTrue(m_builder.FileEncodingsAreValid());
			Assert.IsNull(m_builder.m_lastErrorMessage);
		}

		/// ---------------------------------------------------------------------------------
		/// <summary>
		/// Tests TE-333: test that UTF7 files aren't ok
		/// </summary>
		/// ---------------------------------------------------------------------------------
		[Test]
		public void DisplayErrorForUTF7()
		{
			DummyScrImportFileInfo[] files =
			{
				new DummyScrImportFileInfo("utf7", ImportDomain.Main, null, null,
					new List<int>(new int[] {2, 3}), Encoding.UTF7, new ScrReference(3, 1, 0, m_scr.Versification))
			};
			m_builder.CallAddFilesToListView(files);

			// now do the test
			Assert.IsFalse(m_builder.FileEncodingsAreValid());
			Assert.IsTrue(m_builder.m_lastErrorMessage.StartsWith("The file \"utf7\" contains an unsupported encoding of Unicode (UTF-7)."));
		}
		#endregion
	}
}
