// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2007' to='2008' company='SIL International'>
//		Copyright (c) 2007-2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: EditorialChecksTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using NUnit.Framework;

using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.TE.TeEditorialChecks;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.Utils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	#region DummyScrCheck
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyScrCheck : IScriptureCheck
	{
		#region Member variables
		private string m_checkGroup;
		private string m_checkName;
		private Guid m_checkId;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyScrCheck"/> class.
		/// </summary>
		/// <param name="checkName">Name of the Scripture check.</param>
		/// <param name="checkGroup">The group of which the Scripture check is a part.</param>
		/// ------------------------------------------------------------------------------------
		public DummyScrCheck(string checkName, string checkGroup)
		{
			m_checkName = checkName;
			m_checkGroup = checkGroup;
			m_checkId = Guid.NewGuid();
		}
		#endregion

		#region IScriptureCheck Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the specified tokens.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Check(IEnumerable<ITextToken> toks, RecordErrorHandler record)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Scripture check group.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CheckGroup
		{
			get { return m_checkGroup; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the Scripture check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CheckName
		{
			get { return m_checkName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the unique identifier of the check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Guid CheckId
		{
			get { return m_checkId; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the relative order.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public float RelativeOrder
		{
			get { return 0F; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the description.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Description
		{
			get { return "BLAH!"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the invalid items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string InvalidItems
		{
			get	{throw new Exception("The method or operation is not implemented.");}
			set	{throw new Exception("The method or operation is not implemented.");}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the valid items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ValidItems
		{
			get { throw new Exception("The method or operation is not implemented."); }
			set { throw new Exception("The method or operation is not implemented."); }
		}

		#endregion
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class EditorialChecksTests : ScrInMemoryFdoTestBase
	{
		#region Constants
		private const int m_filterInstance = 345;
		#endregion

		#region Member variables
		EditorialChecksControl m_editChecksControl;
		SortedList<ScrCheckKey, IScriptureCheck> m_listOfChecks;
		private FilteredScrBooks m_bookFilter;
		private TriStateTreeView m_checksTree;
		#endregion

		#region Test setup and teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			m_inMemoryCache = null;
			base.Initialize();
			m_inMemoryCache.InitializeAnnotationDefs();
			m_listOfChecks = new SortedList<ScrCheckKey, IScriptureCheck>();
			m_bookFilter = new FilteredScrBooks(Cache, m_filterInstance);

			m_editChecksControl = new EditorialChecksControl(
				m_scrInMemoryCache.Cache, m_bookFilter, "Dummy Caption", "Dummy Project", null);

			m_checksTree = ReflectionHelper.GetField(
				m_editChecksControl, "m_availableChecksTree") as TriStateTreeView;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();
			m_editChecksControl.Dispose();
			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the method to populate the Scripture checks tree view.
		/// </summary>
		/// <param name="scrCheckList">The SCR check list.</param>
		/// ------------------------------------------------------------------------------------
		private void FillChecksTree(SortedList<ScrCheckKey, IScriptureCheck> scrCheckList)
		{
			ReflectionHelper.CallMethod(m_editChecksControl,
				"FillAvailableChecksTree", scrCheckList);
		}

		#endregion

		#region Test adding checks to dialog
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the adding multiple checks to the same group in random order. We expect them to
		/// be re-ordered alphabetically.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddChecksToOneGroupOutOfOrder()
		{
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check B"),
				(IScriptureCheck)(new DummyScrCheck("Check B", "Test checks")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check C"),
				(IScriptureCheck)(new DummyScrCheck("Check C", "Test checks")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check A"),
				(IScriptureCheck)(new DummyScrCheck("Check A", "Test checks")));

			FillChecksTree(m_listOfChecks);

			// We expect that all the checks will be in alphabetical order.
			for (int iNode = 0; iNode < m_checksTree.Nodes.Count; iNode++)
			{
				for (int iCheckNode = 0; iCheckNode < m_checksTree.Nodes[iNode].Nodes.Count - 1; iCheckNode++)
				{
					Assert.IsTrue(m_checksTree.Nodes[iNode].Nodes[iCheckNode].Text.CompareTo(
						m_checksTree.Nodes[iNode].Nodes[iCheckNode + 1].Text) < 0);
					Debug.WriteLine(m_checksTree.Nodes[iNode].Nodes[iCheckNode].Text + " < " +
						m_checksTree.Nodes[iNode].Nodes[iCheckNode + 1].Text);
				}
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that adding multiple checks into one group is done alphabetically.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddChecksToOneGroup()
		{
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check A"),
				(IScriptureCheck)(new DummyScrCheck("Check A", "Test checks")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check B"),
				(IScriptureCheck)(new DummyScrCheck("Check B", "Test checks")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check C"),
				(IScriptureCheck)(new DummyScrCheck("Check C", "Test checks")));

			FillChecksTree(m_listOfChecks);
			TreeNodeCollection nodes = m_checksTree.Nodes;

			// We expect that all the checks will be in alphabetical order.
			for (int iNode = 0; iNode < m_checksTree.Nodes.Count; iNode++)
			{
				for (int iCheckNode = 0; iCheckNode < nodes[iNode].Nodes.Count - 1; iCheckNode++)
				{
					string chk1 = m_checksTree.Nodes[iNode].Nodes[iCheckNode].Text;
					string chk2 = m_checksTree.Nodes[iNode].Nodes[iCheckNode + 1].Text;
					Assert.IsTrue(chk1.CompareTo(chk2) < 0);
					Debug.WriteLine(chk1 + " < " + chk2);
				}
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding multiple groups alphabetically. (The groups Basic and Other are not added).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddGroups()
		{
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check A"),
				(IScriptureCheck)(new DummyScrCheck("Check A", "Test checks D")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check B"),
				(IScriptureCheck)(new DummyScrCheck("Check B", "Test checks B")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check C"),
				(IScriptureCheck)(new DummyScrCheck("Check C", "Test checks A")));

			FillChecksTree(m_listOfChecks);

			Assert.AreEqual(3, m_checksTree.Nodes.Count);
			// We expect that all the groups of checks will be in alphabetical order.
			for (int iNode = 0; iNode < m_checksTree.Nodes.Count - 1; iNode++)
			{
				Assert.IsTrue(m_checksTree.Nodes[iNode].Text.CompareTo(
					m_checksTree.Nodes[iNode + 1].Text) < 0);
				Debug.WriteLine(m_checksTree.Nodes[iNode].Text + " < " +
					m_checksTree.Nodes[iNode + 1].Text);
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding multiple groups alphabetically (with Basic always first).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddGroupsIncludingBasic()
		{
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check A"),
				(IScriptureCheck)(new DummyScrCheck("Check A", "A group alphabetically before Basic")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check B"),
				(IScriptureCheck)(new DummyScrCheck("Check B", "Basic")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check C"),
				(IScriptureCheck)(new DummyScrCheck("Check C", "Check Group C")));

			FillChecksTree(m_listOfChecks);

			// We expect that "Basic" will be first.
			Assert.AreEqual(3, m_checksTree.Nodes.Count);
			Assert.AreEqual("Basic", m_checksTree.Nodes[0].Text);
			Assert.AreEqual("A group alphabetically before Basic", m_checksTree.Nodes[1].Text);
			Assert.AreEqual("Check Group C", m_checksTree.Nodes[2].Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding multiple groups alphabetically (with Other always last).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddChecksIncludingOther()
		{
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check A"),
				(IScriptureCheck)(new DummyScrCheck("Check A", "A group alphabetically before Basic")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check B"),
				(IScriptureCheck)(new DummyScrCheck("Check B", "Basic")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check O"),
				(IScriptureCheck)(new DummyScrCheck("Check O", "Other")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check D"),
				(IScriptureCheck)(new DummyScrCheck("Check D", "This shouldn't sort after Other")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check C"),
				(IScriptureCheck)(new DummyScrCheck("Check C", "Check Group C")));

			FillChecksTree(m_listOfChecks);
			// We expect that "Basic" will be first, "Other" last, and remaining groups in between.
			Assert.AreEqual(5, m_checksTree.Nodes.Count);
			Assert.AreEqual("Basic", m_checksTree.Nodes[0].Text);
			Assert.AreEqual("A group alphabetically before Basic", m_checksTree.Nodes[1].Text);
			Assert.AreEqual("Check Group C", m_checksTree.Nodes[2].Text);
			Assert.AreEqual("This shouldn't sort after Other", m_checksTree.Nodes[3].Text);
			Assert.AreEqual("Other", m_checksTree.Nodes[4].Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests adding multiple groups alphabetically. Exceptions are that Basic should always
		/// be first, and Other should always be last.
		/// This test includes other group names that are alphabetically before Basic or after
		/// Other.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddAllCheckGroups()
		{
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check B"),
				(IScriptureCheck)(new DummyScrCheck("Check B", "Basic")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check A"),
				(IScriptureCheck)(new DummyScrCheck("Check A", "A Check group")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check O"),
				(IScriptureCheck)(new DummyScrCheck("Check O", "Other")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check C"),
				(IScriptureCheck)(new DummyScrCheck("Check C", "A Scr Check")));
			m_listOfChecks.Add(new ScrCheckKey(0F, "Check D"),
				(IScriptureCheck)(new DummyScrCheck("Check D", "Z Group")));

			FillChecksTree(m_listOfChecks);

			// We expect that "Basic" will be first, "Other" last, and remaining groups in between.
			Assert.AreEqual(5, m_checksTree.Nodes.Count);
			Assert.AreEqual("Basic", m_checksTree.Nodes[0].Text);
			Assert.AreEqual("A Check group", m_checksTree.Nodes[1].Text);
			Assert.AreEqual("A Scr Check", m_checksTree.Nodes[2].Text);
			Assert.AreEqual("Z Group", m_checksTree.Nodes[3].Text);
			Assert.AreEqual("Other", m_checksTree.Nodes[4].Text);
		}
		#endregion

		#region Test running checks
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests to make sure the error list is updated when the book filter changes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Changed where book filter change is monitored. Fix this test soon.")]
		public void ErrorListIsUpdatedWhenBookFilterChanges()
		{
			// We have to add a paragraph that is owned to add a valid annotation, so create
			// a book, section, paragraph.
			IScrBook book1 = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(book1.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "some text for an annotation", ScrStyleNames.Normal);

			IScrBook book2 = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(book2.Hvo);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "some text for an annotation", ScrStyleNames.Normal);

			// Add both books to the book filter
			m_bookFilter.UpdateFilter(book1.Hvo, book2.Hvo);

			EditorialChecksRenderingsControl renderingCtl =
				new EditorialChecksRenderingsControl(Cache, m_bookFilter, null);

			// Add some checking errors for each book
			m_scrInMemoryCache.AddAnnotation(para1, new BCVRef(1001001), NoteType.CheckingError);
			m_scrInMemoryCache.AddAnnotation(para1, new BCVRef(1001003), NoteType.CheckingError);
			m_scrInMemoryCache.AddAnnotation(para2, new BCVRef(2002005), NoteType.CheckingError);
			m_scrInMemoryCache.AddAnnotation(para2, new BCVRef(2005005), NoteType.CheckingError);
			renderingCtl.LoadCheckingErrors();

			SortableBindingList<CheckingError> checkingErrors =
				ReflectionHelper.GetField(renderingCtl, "m_checkingErrors") as
				SortableBindingList<CheckingError>;

			Assert.AreEqual(4, checkingErrors.Count);

			m_bookFilter.UpdateFilter(book1.Hvo); // Should update the error list

			Assert.AreEqual(2, checkingErrors.Count,
				"The error list should have been updated when the book filter changed");

#if DEBUG
			// For some very strange reason, without this dispose call in debug mode,
			// there is an assertion in the c++ code when nunit is shut down. With the
			// dispose call in release mode there is a different assertion in c++ code
			// (something about calling a pure virtual method) when nunit is shut down.
			// So, it seems we can't win either way unless making this line conditional
			// on the build config. Argh!!!! - DDO: 16-Jul-2008
			renderingCtl.Dispose();
#endif
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test re-running tests when there are no books in the project (TE-6309).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ReRunChecksWithNoBooks()
		{
			// We have to add a paragraph that is owned to add a valid annotation, so create
			// a book, section, paragraph.
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);

			m_scrInMemoryCache.AddRunToMockedPara(para,
				"some text for an annotation", ScrStyleNames.Normal);

			EditorialChecksRenderingsControl renderingCtl =
				new EditorialChecksRenderingsControl(Cache, m_bookFilter, null);

			// Add a checking error annotation.
			m_scrInMemoryCache.AddAnnotation(para, 1001001, NoteType.CheckingError);
			// Now delete the book so that there are not any in the project.
			m_inMemoryCache.Cache.DeleteObject(book.Hvo);
			Assert.AreEqual(0, m_scr.ScriptureBooksOS.Count,
				"There should not be books for this test.");

			// Now attempt to load the checking error annotations.
			renderingCtl.LoadCheckingErrors();

			List<ICheckGridRowObject> checkingErrors = ReflectionHelper.GetField(
				renderingCtl, "m_list") as List<ICheckGridRowObject>;

			// We expect that no annotations would be added to the EditorialRenderingsControl
			// and that we won't crash trying to load checking error annotations.
			Assert.AreEqual(0, checkingErrors.Count,
				"Since there are no books, the error annotation should not be added to the rendering control");

#if DEBUG
			// For some very strange reason, without this dispose call in debug mode,
			// there is an assertion in the c++ code when nunit is shut down. With the
			// dispose call in release mode there is a different assertion in c++ code
			// (something about calling a pure virtual method) when nunit is shut down.
			// So, it seems we can't win either way unless making this line conditional
			// on the build config. Argh!!!! - DDO: 16-Jul-2008
			renderingCtl.Dispose();
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetLastRunInfoForCheck method when 2 or 3 books in the filter have
		/// a check run history record for a hypothetical check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetLastRunInfoForCheck_OneBookUnchecked()
		{
			IScrBook book1 = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			IScrBook book2 = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			IScrBook book3 = m_scrInMemoryCache.AddBookToMockedScripture(3, "Leviticus");
			m_bookFilter.Add(new int[] {book1.Hvo, book2.Hvo, book3.Hvo});

			FdoOwningSequence<IScrBookAnnotations> booksAnnotations =
				m_inMemoryCache.Cache.LangProject.TranslatedScriptureOA.BookAnnotationsOS;

			Guid checkId = Guid.NewGuid();

			ScrCheckRun checkRun = new ScrCheckRun();
			booksAnnotations[0].ChkHistRecsOC.Add(checkRun);
			checkRun.CheckId = checkId;
			checkRun.RunDate = new DateTime(2008, 3, 5, 14, 20, 30);

			checkRun = new ScrCheckRun();
			booksAnnotations[1].ChkHistRecsOC.Add(checkRun);
			checkRun.CheckId = checkId;
			checkRun.RunDate = new DateTime(2007, 3, 5, 14, 20, 30);

			// Call the method we're testing. Send the arguments in an object
			// array because the second argument is an out parameter.
			object[] args = new object[] { checkId, new string[] { } };
			DateTime lastRun = ReflectionHelper.GetDateTimeResult(m_editChecksControl,
				"GetLastRunInfoForCheck", args);

			string[] bookChkInfo = args[1] as string[];

			Assert.AreEqual(DateTime.MinValue, lastRun);
			Assert.AreEqual(3, bookChkInfo.Length);
			Assert.IsTrue(bookChkInfo[0].Contains("Genesis"));
			Assert.IsTrue(bookChkInfo[1].Contains("Exodus"));
			Assert.IsTrue(bookChkInfo[2].Contains("Leviticus"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetLastRunInfoForCheck method when all the books in the filter have
		/// a check run history record for a hypothetical check.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetLastRunInfoForCheck_AllBooksChecked()
		{
			IScrBook book1 = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			IScrBook book2 = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			IScrBook book3 = m_scrInMemoryCache.AddBookToMockedScripture(3, "Leviticus");
			m_bookFilter.Add(new int[] { book1.Hvo, book2.Hvo, book3.Hvo });

			FdoOwningSequence<IScrBookAnnotations> booksAnnotations =
				m_inMemoryCache.Cache.LangProject.TranslatedScriptureOA.BookAnnotationsOS;

			Guid checkId = Guid.NewGuid();
			DateTime date1 = new DateTime(2008, 3, 5, 14, 20, 30);
			DateTime date2 = new DateTime(2007, 3, 5, 14, 20, 30);
			DateTime date3 = new DateTime(2009, 3, 5, 14, 20, 30);

			// Add a check history record for Genesis.
			ScrCheckRun checkRun = new ScrCheckRun();
			booksAnnotations[0].ChkHistRecsOC.Add(checkRun);
			checkRun.CheckId = checkId;
			checkRun.RunDate = date1;

			// Add a check history record for Exodus.
			checkRun = new ScrCheckRun();
			booksAnnotations[1].ChkHistRecsOC.Add(checkRun);
			checkRun.CheckId = checkId;
			checkRun.RunDate = date2;

			// Add a check history record for Leviticus.
			checkRun = new ScrCheckRun();
			booksAnnotations[2].ChkHistRecsOC.Add(checkRun);
			checkRun.CheckId = checkId;
			checkRun.RunDate = date3;

			// Call the method we're testing. Send the arguments in an object
			// array because the second argument is an out parameter.
			object[] args = new object[] { checkId, new string[] { } };
			DateTime lastRun = ReflectionHelper.GetDateTimeResult(m_editChecksControl,
				"GetLastRunInfoForCheck", args);

			string[] bookChkInfo = args[1] as string[];

			Assert.AreEqual(date2, lastRun);
			Assert.AreEqual(3, bookChkInfo.Length);
			Assert.IsTrue(bookChkInfo[0].Contains(date1.ToString()));
			Assert.IsTrue(bookChkInfo[1].Contains(date2.ToString()));
			Assert.IsTrue(bookChkInfo[2].Contains(date3.ToString()));
			Assert.IsTrue(bookChkInfo[0].Contains("Genesis"));
			Assert.IsTrue(bookChkInfo[1].Contains("Exodus"));
			Assert.IsTrue(bookChkInfo[2].Contains("Leviticus"));
		}

		#endregion
	}
}
