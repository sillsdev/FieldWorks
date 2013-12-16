// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ArchiveMaintenanceDialogTests.cs
// Responsibility: TE team

using System;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.ScriptureUtils;

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
		public DummySavedVersionsDialog(FdoCache cache) : base(cache, null, 1.0f, 1.0f, null, null)
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

			m_cache.ServiceLocator.GetInstance<IActionHandler>().EndUndoTask();
			m_btnDelete_Click(null, null);
			m_cache.ServiceLocator.GetInstance<IActionHandler>().BeginUndoTask("After delete", "After delete");
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
	public class SavedVersionsDialogTests : ScrInMemoryFdoTestBase
	{
		#region Member vars
		// these book name match those in the ScrRefSystem in TestLangProj
		// they also include the Scripture references
		private string[] m_bookNames = new string[] { "Philemon", "James", "Jude" };
		private string[] m_bookScrRange = new string[] { "Philemon 1:1-3 (with intro)",
			"James 1:1-2:2", "Jude (intro only)" };
		#endregion

		#region Setup and Teardown

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			CreateSavedVersions();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the test data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			//Philemon
			IScrBook philemon = AddBookToMockedScripture(57, "Philemon");
			AddTitleToMockedBook(philemon, "Philemon");

			IScrSection introSection = AddSectionToMockedBook(philemon, true);
			IScrTxtPara para = AddParaToMockedSectionContent(introSection, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "This is Philemon", null);

			IScrSection section = AddSectionToMockedBook(philemon);
			AddSectionHeadParaToSection(section, "Paul tells people", "Section Head");
			para = AddParaToMockedSectionContent(section, "Paragraph");
			AddRunToMockedPara(para, "1", "Chapter Number");
			AddRunToMockedPara(para, "1", "Verse Number");
			AddRunToMockedPara(para, "and the earth was without form and void and darkness covered the face of the deep", null);

			IScrSection section2 = AddSectionToMockedBook(philemon);
			AddSectionHeadParaToSection(section2, "Paul tells people more", "Section Head");
			IScrTxtPara para2 = AddParaToMockedSectionContent(section2, "Paragraph");
			AddRunToMockedPara(para2, "2", "Verse Number");
			AddRunToMockedPara(para2, "paul expounds on the nature of reality", null);
			IScrTxtPara para3 = AddParaToMockedSectionContent(section2, "Paragraph");
			AddRunToMockedPara(para3, "3", "Verse Number");
			AddRunToMockedPara(para3, "the existentialists are all wrong", null);

			IScrBook james = AddBookToMockedScripture(59, "James");
			AddTitleToMockedBook(james, "James");

			// James first section
			section = AddSectionToMockedBook(james);
			AddSectionHeadParaToSection(section, "Paul tells people", "Section Head");
			para = AddParaToMockedSectionContent(section, "Paragraph");
			AddRunToMockedPara(para, "1", "Chapter Number");
			AddRunToMockedPara(para, "1", "Verse Number");
			AddRunToMockedPara(para, "and the earth was without form and void and darkness covered the face of the deep", null);

			// James section2
			section2 = AddSectionToMockedBook(james);
			AddSectionHeadParaToSection(section2, "Paul tells people more", "Section Head");
			para2 = AddParaToMockedSectionContent(section2, "Paragraph");
			AddRunToMockedPara(para2, "2", "Chapter Number");
			AddRunToMockedPara(para2, "1", "Verse Number");
			AddRunToMockedPara(para2, "paul expounds on the nature of reality", null);
			para3 = AddParaToMockedSectionContent(section2, "Paragraph");
			AddRunToMockedPara(para3, "2", "Verse Number");
			AddRunToMockedPara(para3, "the existentialists are all wrong", null);

			// Jude
			IScrBook jude = AddBookToMockedScripture(65, "Jude");
			AddTitleToMockedBook(jude, "Jude");
			IScrSection judeSection = AddSectionToMockedBook(jude, true);
			AddSectionHeadParaToSection(judeSection, "Introduction", ScrStyleNames.IntroSectionHead);
			IScrTxtPara judePara = AddParaToMockedSectionContent(judeSection, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(judePara, "The Letter from Jude was written to warn against" +
				" false teachers who claimed to be believers. In this brief letter, which is similar in" +
				" content to 2 Peter the writer encourages his readers \u201Cto fight on for the faith which" +
				" once and for all God has given to his people.", null);
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add archive to the database
		/// </summary>
		/// <param name="description">description for the archive.</param>
		/// <param name="booksToArchive">books to add to the archive.</param>
		/// ------------------------------------------------------------------------------------
		private void AddArchive(string description, IEnumerable<IScrBook> booksToArchive)
		{
			Cache.ServiceLocator.GetInstance<IScrDraftFactory>().Create(description, booksToArchive);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function. Creates a set of saved versions in the database for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateSavedVersions()
		{
			// delete all of the existing archives in the database, so we can start clean
			m_scr.ArchivedDraftsOC.Clear();

			// create three archives for the tests from TestLangProj
			for (int i = 0; i < m_scr.ScriptureBooksOS.Count; i++)
			{
				// make an archive
				//ScrDraft archive = new ScrDraft();
				//m_scr.ArchivedDraftsOC.Add(archive);
				//archive.Description = "My Archive " + i;

				List<IScrBook> booksToAdd = new List<IScrBook>(i);
				// put "i" books in this archive - one in the first, two in the second, etc
				for (int iBook = 0; iBook <= i; iBook++)
				{
					booksToAdd.Add(m_scr.ScriptureBooksOS[iBook]);
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
			using (DummySavedVersionsDialog dlg = new DummySavedVersionsDialog(Cache))
			{
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
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test deleting a book in the archive tree view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteBook()
		{
			using (DummySavedVersionsDialog dlg = new DummySavedVersionsDialog(Cache))
			{
				TreeView tree = dlg.ArchiveTree;

				// Delete the first book from the last archive (Philemon)
				TreeNode archiveNode = null;
				for (int i = 0; i < tree.Nodes.Count; i++)
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
				IScrDraft archive = (IScrDraft)archiveNode.Tag;
				Assert.AreEqual(2, archive.BooksOS.Count);
				List<int> cannonicalBookNums = new List<int>(new int[] { 59, 65 });
				for (int iArchivedBook = 0; iArchivedBook < archive.BooksOS.Count; iArchivedBook++)
				{
					IScrBook book = archive.BooksOS[iArchivedBook];
					Assert.AreEqual(cannonicalBookNums[iArchivedBook], book.CanonicalNum);
					Assert.AreEqual(m_bookNames[iArchivedBook + 1], book.BestUIName);
				}
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
			using (DummySavedVersionsDialog dlg = new DummySavedVersionsDialog(Cache))
			{
				TreeView tree = dlg.ArchiveTree;

				// Delete the only book from the first archive;
				//  we expect the archive itself to also be deleted
				TreeNode archiveNode = null;
				for (int i = 0; i < tree.Nodes.Count; i++)
				{
					if (tree.Nodes[i].Text.IndexOf("My Archive 0") != -1)
					{
						archiveNode = tree.Nodes[i];
						break;
					}
				}
				Assert.AreEqual(1, archiveNode.Nodes.Count);
				//only one book exists in this archive
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
				foreach (IScrDraft archive in m_scr.ArchivedDraftsOC)
					Assert.IsTrue(archive.Description != "My Archive 0", "\"My Archive 0\" should have been deleted.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test deleting an archive in the tree view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteSavedVersion()
		{
			using (DummySavedVersionsDialog dlg = new DummySavedVersionsDialog(Cache))
			{
				TreeView tree = dlg.ArchiveTree;

				// Delete the archive which has two books
				for (int i = 0; i < tree.Nodes.Count; i++)
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
				foreach (IScrDraft archive in m_scr.ArchivedDraftsOC)
					Assert.IsTrue(archive.Description != "My Archive 1", "\"My Archive 1\" should have been deleted.");
			}
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
			IStText title;
			Assert.IsNull(m_scr.FindBook(66),
				"Revelation should not be in the database. Restore the clean version of TestLangProj.");
			IScrBook revelation = Cache.ServiceLocator.GetInstance<IScrBookFactory>().Create(66, out title);
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			ITsTextProps ttp = propFact.MakeProps(ScrStyleNames.NormalParagraph, Cache.DefaultVernWs, 0);
			Cache.ServiceLocator.GetInstance<IScrSectionFactory>().CreateScrSection(revelation, 0, "Text for section", ttp, false);
			AddArchive("Revelation Archive", new List<IScrBook>(new IScrBook[] { revelation }));
			m_scr.ScriptureBooksOS.Remove(revelation);

			using (DummySavedVersionsDialog dlg = new DummySavedVersionsDialog(Cache))
			{
				TreeView tree = dlg.ArchiveTree;

				// Select the archive node that was just added.
				tree.SelectedNode = tree.Nodes[0];
				dlg.SimulateSelectEvent();

				// Check to make sure the Compare to Current Version button is disabled
				Assert.IsFalse(dlg.ComparetoCurrentVersionBtn.Enabled, "The Compare to Current Version button should be disabled");

				TreeNode revNode = null;
				TreeNode phmNode = null;
				foreach (TreeNode node in tree.Nodes)
				{
					if (node.Nodes.Count > 0 && node.Nodes[0].Tag is IScrBook)
					{
						if (((IScrBook)node.Nodes[0].Tag).CanonicalNum == 66)
							revNode = node.Nodes[0];
						if (((IScrBook)node.Nodes[0].Tag).CanonicalNum == 57)
							phmNode = node.Nodes[0];
					}
				}
				Assert.IsNotNull(revNode, "there should be an archive where the first book is Revelation");
				Assert.IsNotNull(phmNode, "there should be an archive where the first book is Philemon");
				// Select a book node for Revelation.
				tree.SelectedNode = revNode;
				dlg.SimulateSelectEvent();

				// Check to make sure the Diff button is still disabled if we select Revelation
				// after it is removed from the DB.
				Assert.IsTrue(tree.SelectedNode.Tag is IScrBook);
				var bookId = ((IScrBook)tree.SelectedNode.Tag).CanonicalNum;
				Assert.AreEqual(66, bookId);
				Assert.IsNull(m_scr.FindBook(bookId));
				Assert.IsFalse(dlg.ComparetoCurrentVersionBtn.Enabled,
					"The Compare to Current Version button should still be disabled.");

				// However, the diff button should be enabled if we select Philemon in the second archive
				// since it is in our database.
				tree.SelectedNode = phmNode;
				// selecting Philemon
				dlg.SimulateSelectEvent();
				Assert.IsTrue(dlg.ComparetoCurrentVersionBtn.Enabled,
					"The Compare to Current Version button should be enabled when Philemon is selected.");
			}
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
			using (DummySavedVersionsDialog dlg = new DummySavedVersionsDialog(Cache))
			{
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
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete everything from the list of saved versions and make sure the Delete and
		/// Compare to Current Version buttons get disabled if there are no archives.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteAllItems_NoArchive()
		{
			m_scr.ArchivedDraftsOC.Clear();
			using (DummySavedVersionsDialog dlg = new DummySavedVersionsDialog(Cache))
			{
				// Verify that the buttons are disabled
				Assert.IsFalse(dlg.ComparetoCurrentVersionBtn.Enabled, "The Compare to Current Version button should be disabled");
				Assert.IsFalse(dlg.DeleteBtn.Enabled, "The delete button should be disabled");
			}
		}
		#endregion
	}
}
