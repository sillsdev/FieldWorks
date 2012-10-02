// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ArchiveMaintenanceDialogTests.cs
// Responsibility: TE team
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.TE
{
	#region DummySavedVersionsDialog
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy class for the dialog, to expose items needed for testing.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	class DummySavedVersionsDialog : SavedVersionsDialog
	{
		/// <summary>constructor</summary>
		public DummySavedVersionsDialog(FdoCache cache) : base(cache, null, 1.0f, 1.0f)
		{
		}

		public TreeView ArchiveTree
		{
			get
			{
				CheckDisposed();
				return m_treeArchives;
			}
		}

		public void SimulateDelete()
		{
			CheckDisposed();

			m_btnDelete_Click(null, null);
		}

		public Button ComparetoCurrentVersionBtn
		{
			get
			{
				CheckDisposed();
				return m_btnDiff;
			}
		}

		public Button DeleteBtn
		{
			get
			{
				CheckDisposed();
				return m_btnDelete;
			}
		}

		public void SimulateSelectEvent()
		{
			CheckDisposed();

			m_treeArchives_AfterSelect(null, null);
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class for testing of SavedVersionsDialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class SavedVersionsDialogTests: BaseTest
	{
		#region Member vars
		private FdoCache m_cache;
		private IScripture m_scr;

		// these book name match those in the ScrRefSystem in TestLangProj
		// they also include the Scripture references
		private string[] m_bookNames = new string[] { "Philemon", "James", "Jude" };
		private string[] m_bookScrRange = new string[] { "Philemon 1:1-25 (with intro)",
			"James 1:1-5:20 (with intro)", "Jude 1:1-25 (with intro)" };
		#endregion

		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up for a test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			// set up our cache
			m_cache = FdoCache.Create("TestLangProj");
			m_scr = m_cache.LangProject.TranslatedScriptureOA;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tear down after a test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void Destroy()
		{
			UndoResult ures = 0;
			while (m_cache.CanUndo)
			{
				m_cache.Undo(out ures);
				if (ures == UndoResult.kuresFailed  || ures == UndoResult.kuresError)
					Assert.Fail("ures should not be == " + ures.ToString());
			}
			m_cache.Dispose();
			m_cache = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void SetupTest()
		{
			CreateSavedVersions();
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add archive to the database
		/// </summary>
		/// <param name="description">description for the archive.</param>
		/// <param name="booksToArchive">list of book HVOs to add to the archive.</param>
		/// ------------------------------------------------------------------------------------
		private void AddArchive(string description, List<int> booksToArchive)
		{
			using (new SuppressSubTasks(m_cache))
			{
				string sSavePoint;
				m_cache.DatabaseAccessor.SetSavePointOrBeginTrans(out sSavePoint);
				try
				{
					ScrDraft savedVersion = (ScrDraft)m_scr.CreateSavedVersion(description,
						booksToArchive.ToArray());
					m_cache.DatabaseAccessor.CommitTrans();
				}
				catch
				{
					m_cache.DatabaseAccessor.RollbackSavePoint(sSavePoint);
					throw;
				}

			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function. Creates a set of saved versions in the database for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateSavedVersions()
		{
			IScrRefSystem refSys = m_cache.ScriptureReferenceSystem;

			// delete all of the existing archives in the database, so we can start clean
			while (m_scr.ArchivedDraftsOC.Count > 0)
				m_scr.ArchivedDraftsOC.Remove(m_scr.ArchivedDraftsOC.HvoArray[0]);

			// create three archives for the tests from TestLangProj
			for (int i = 0; i < m_scr.ScriptureBooksOS.Count; i++)
			{
				// make an archive
				//ScrDraft archive = new ScrDraft();
				//m_scr.ArchivedDraftsOC.Add(archive);
				//archive.Description = "My Archive " + i;

				List<int> booksToAdd = new List<int>(i);
				// put "i" books in this archive - one in the first, two in the second, etc
				for (int iBook = 0; iBook <= i; iBook++)
				{
					booksToAdd.Add(m_scr.ScriptureBooksOS[iBook].Hvo);
				}

				AddArchive("My Archive " + i, booksToAdd);
			}
		}
		#endregion

		#region tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the dialog to see if it fills in the tree view properly
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FillTreeView()
		{
			DummySavedVersionsDialog dlg = new DummySavedVersionsDialog(m_cache);

			// verify the tree view contents
			TreeView tree = dlg.ArchiveTree;

			// Check the node counts and the archive names and book names
			Assert.AreEqual(3, tree.Nodes.Count, "The count of root nodes in the tree is incorrect");
			for (int i = 0; i < tree.Nodes.Count; i++)
			{
				if (tree.Nodes[i].Text.IndexOf("My Archive 0") != -1)
				{
					Assert.AreEqual(1, tree.Nodes[i].Nodes.Count);
					for (int bookIndex = 0; bookIndex < 1; bookIndex++)
					{
						string bookLabel = tree.Nodes[i].Nodes[bookIndex].Text;
						Assert.AreEqual(m_bookScrRange[bookIndex], bookLabel, "The book label was incorrect in the tree");
					}
				}
				else if (tree.Nodes[i].Text.IndexOf("My Archive 1") != -1)
				{
					Assert.AreEqual(2, tree.Nodes[i].Nodes.Count);
					for (int bookIndex = 0; bookIndex < 2; bookIndex++)
					{
						string bookLabel = tree.Nodes[i].Nodes[bookIndex].Text;
						Assert.AreEqual(m_bookScrRange[bookIndex], bookLabel, "The book label was incorrect in the tree");
					}
				}
				else if (tree.Nodes[i].Text.IndexOf("My Archive 2") != -1)
				{
					Assert.AreEqual(3, tree.Nodes[i].Nodes.Count);
					for (int bookIndex = 0; bookIndex < 3; bookIndex++)
					{
						string bookLabel = tree.Nodes[i].Nodes[bookIndex].Text;
						Assert.AreEqual(m_bookScrRange[bookIndex], bookLabel, "The book label was incorrect in the tree");
					}
				}
				else
					Assert.Fail("Seems we have an extra archive");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test deleting a book in the archive tree view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteBook()
		{
			DummySavedVersionsDialog dlg = new DummySavedVersionsDialog(m_cache);
			TreeView tree = dlg.ArchiveTree;

			// Delete the first book from the last archive (Philemon)
			TreeNode archiveNode = null;
			for(int i = 0; i < tree.Nodes.Count; i++)
			{
				if (tree.Nodes[i].Text.IndexOf("My Archive 2") != -1)
				{
					archiveNode = tree.Nodes[i];
					break;
				}
			}
			tree.SelectedNode = archiveNode.Nodes[0];
			dlg.SimulateDelete();

			// check the node counts
			Assert.AreEqual(3, tree.Nodes.Count);
			for (int i = 0; i < tree.Nodes.Count; i++)
			{
				if (tree.Nodes[i].Text.IndexOf("My Archive 0") != -1)
					Assert.AreEqual(1, tree.Nodes[i].Nodes.Count);
				else if (tree.Nodes[i].Text.IndexOf("My Archive 1") != -1)
					Assert.AreEqual(2, tree.Nodes[i].Nodes.Count);
				else if (tree.Nodes[i].Text.IndexOf("My Archive 2") != -1)
					Assert.AreEqual(2, tree.Nodes[i].Nodes.Count);
				else
					Assert.Fail("Seems we have an extra archive");
			}

			// Check the remaining book names for the archive that was deleted from.
			for (int i = 0; i < 2; i++)
			{
				string bookLabel = archiveNode.Nodes[i].Text;
				Assert.AreEqual(m_bookScrRange[i + 1], bookLabel);
			}

			// Check the books in the database to make sure the database is correct.
			Assert.AreEqual(3, m_scr.ArchivedDraftsOC.Count);
			ScrDraft archive = (ScrDraft)archiveNode.Tag;
			Assert.AreEqual(2, archive.BooksOS.Count);
			List<int> cannonicalBookNums = new List<int>(new int[]{59,65});
			for (int iArchivedBook = 0; iArchivedBook < archive.BooksOS.Count; iArchivedBook++)
			{
				ScrBook book = (ScrBook)archive.BooksOS[iArchivedBook];
				Assert.AreEqual(cannonicalBookNums[iArchivedBook], book.CanonicalNum);
				Assert.AreEqual(m_bookNames[iArchivedBook + 1], book.BestUIName);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test deleting the only book in the archive.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteOnlyBook()
		{
			DummySavedVersionsDialog dlg = new DummySavedVersionsDialog(m_cache);
			TreeView tree = dlg.ArchiveTree;

			// Delete the only book from the first archive;
			//  we expect the archive itself to also be deleted
			TreeNode archiveNode = null;
			for(int i = 0; i < tree.Nodes.Count; i++)
			{
				if (tree.Nodes[i].Text.IndexOf("My Archive 0") != -1)
				{
					archiveNode = tree.Nodes[i];
					break;
				}
			}
			Assert.AreEqual(1, archiveNode.Nodes.Count); //only one book exists in this archive
			TreeNode bookNode = archiveNode.Nodes[0];
			tree.SelectedNode = bookNode;
			dlg.SimulateDelete();

			// check the counts of archives and books in the archives
			Assert.AreEqual(2, tree.Nodes.Count);
			for (int i = 0; i < tree.Nodes.Count; i++)
			{
				if (tree.Nodes[i].Text.IndexOf("My Archive 1") != -1)
					Assert.AreEqual(2, tree.Nodes[i].Nodes.Count);
				else if (tree.Nodes[i].Text.IndexOf("My Archive 2") != -1)
					Assert.AreEqual(3, tree.Nodes[i].Nodes.Count);
				else
					Assert.Fail("Seems we deleted the wrong archive");
			}

			// check the count of archives in the DB
			Assert.AreEqual(2, m_scr.ArchivedDraftsOC.Count);

			// make sure the correct archive was deleted in the DB.
			foreach (ScrDraft archive in m_scr.ArchivedDraftsOC)
				Assert.IsTrue(archive.Description != "My Archive 0", "\"My Archive 0\" should have been deleted.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test deleting an archive in the tree view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteSavedVersion()
		{
			DummySavedVersionsDialog dlg = new DummySavedVersionsDialog(m_cache);
			TreeView tree = dlg.ArchiveTree;

			// Delete the archive which has two books
			for(int i = 0; i < tree.Nodes.Count; i++)
			{
				if (tree.Nodes[i].Text.IndexOf("My Archive 1") != -1)
					tree.SelectedNode = tree.Nodes[i];
			}

			dlg.SimulateDelete();

			// check the node counts
			Assert.AreEqual(2, tree.Nodes.Count);
			for (int i = 0; i < tree.Nodes.Count; i++)
			{
				if (tree.Nodes[i].Text.IndexOf("My Archive 0") != -1)
					Assert.AreEqual(1, tree.Nodes[i].Nodes.Count);
				else if (tree.Nodes[i].Text.IndexOf("My Archive 2") != -1)
					Assert.AreEqual(3, tree.Nodes[i].Nodes.Count);
				else
					Assert.Fail("Seems we deleted the wrong archive");
			}

			// Check the count of archives in the DB
			Assert.AreEqual(2, m_scr.ArchivedDraftsOC.Count);

			// Make sure the correct archive was deleted in the DB.
			foreach (ScrDraft archive in m_scr.ArchivedDraftsOC)
				Assert.IsTrue(archive.Description != "My Archive 1", "\"My Archive 1\" should have been deleted.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the disabling of the Compare to Current Version button, depending on the tree
		/// selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DisableCompareBtn()
		{
			// Add Revelation, archive it and then remove it.
			int hvoTitle;
			Assert.IsNull(ScrBook.FindBookByID(m_cache, 66),
				"Revelation should not be in the database. Restore the clean version of TestLangProj.");
			IScrBook revelation = ScrBook.CreateNewScrBook(66, m_scr, out hvoTitle);
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			ITsTextProps ttp = propFact.MakeProps(ScrStyleNames.NormalParagraph, m_cache.DefaultVernWs, 0);
			ScrSection.CreateScrSection(revelation, 0, "Text for section", ttp, false);
			AddArchive("Revelation Archive", new List<int>(new int[] { revelation.Hvo }));
			m_scr.ScriptureBooksOS.Remove(m_scr.ScriptureBooksOS[3]);

			DummySavedVersionsDialog dlg = new DummySavedVersionsDialog(m_cache);
			TreeView tree = dlg.ArchiveTree;

			// Select the archive node that was just added.
			tree.SelectedNode = tree.Nodes[0];
			dlg.SimulateSelectEvent();

			// Check to make sure the Compare to Current Version button is disabled
			Assert.IsFalse(dlg.ComparetoCurrentVersionBtn.Enabled, "The Compare to Current Version button should be disabled");

			// Select a book node.
			tree.SelectedNode = tree.Nodes[0].Nodes[0];
			dlg.SimulateSelectEvent();

			// Check to make sure the Diff button is still disabled if we select Philemon
			// after it is removed from the DB.
			Assert.IsFalse(dlg.ComparetoCurrentVersionBtn.Enabled,
				"The Compare to Current Version button should still");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete everything from the list of saved versions and make sure the Delete and
		/// Compare to Current Version buttons get disabled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteAllItems()
		{
			DummySavedVersionsDialog dlg = new DummySavedVersionsDialog(m_cache);
			TreeView tree = dlg.ArchiveTree;

			while (tree.Nodes.Count != 0)
			{
				tree.SelectedNode = tree.Nodes[0];
				dlg.SimulateDelete();
			}

			// Verify that the buttons are disabled
			Assert.IsFalse(dlg.ComparetoCurrentVersionBtn.Enabled, "The Compare to Current Version button should be disabled");
			Assert.IsFalse(dlg.DeleteBtn.Enabled, "The delete button should be disabled");
			dlg.Close();

			// Also check to see if the dialog comes up with the buttons disabled when there
			// are no archives
			dlg = new DummySavedVersionsDialog(m_cache);

			// Verify that the buttons are disabled
			Assert.IsFalse(dlg.ComparetoCurrentVersionBtn.Enabled, "The Compare to Current Version button should be disabled");
			Assert.IsFalse(dlg.DeleteBtn.Enabled, "The delete button should be disabled");
		}
		#endregion
	}
}
