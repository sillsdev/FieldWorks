// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2006' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: KeyTermsTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using Microsoft.Win32;

using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls.SplitGridView;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	#region DummyDraftViewProxy class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyDraftViewProxy : ViewProxy
	{
		private FdoCache m_cache;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyDraftViewProxy"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public DummyDraftViewProxy(FdoCache cache)
			: base("KeyTermsDraftView", true)
		{
			m_cache = cache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method to create the view when needed
		/// </summary>
		/// <param name="host">The control that will host (or "wrap") the view (can be <c>null</c>)</param>
		/// <returns>The created view</returns>
		/// ------------------------------------------------------------------------------------
		public override Control CreateView(Control host)
		{
			return new DummyDraftView(m_cache, false, 0);
		}
	}
	#endregion

	#region DummyKeyTermsViewWrapper class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyKeyTermsViewWrapper : KeyTermsViewWrapper
	{
		/// ------------------------------------------------------------------------------------
		public DummyKeyTermsViewWrapper(Control parent, FdoCache cache, DummyDraftViewProxy createInfo,
			RegistryKey settingsRegKey) : base(parent, cache, createInfo, settingsRegKey, 0,
			string.Empty, null, null)
		{
		}

		internal KeyTermsTree KeyTermsTree
		{
			get { return m_ktTree; }
		}

		public void CallAssignVernacularEquivalent()
		{
			AssignVernacularEquivalentInternal();
		}

		public void CallIgnoreSpecifyingVernacularEquivalent()
		{
			IgnoreSpecifyingVernacularEquivalentInternal();
		}

		public void CallUpdateKeyTermEquivalents()
		{
			UpdateKeyTermEquivalentsInternal(null);
		}
	}
	#endregion

	/// <summary>
	/// Summary description for KeyTermsTests.
	/// </summary>
	[TestFixture]
	public class KeyTermsTests : TeTestBase
	{
		private DummyDraftViewForm m_draftForm;
		private DummyDraftView m_draftView;
		private DummyKeyTermsViewWrapper m_ktVwWrapper;
		private IScrBook m_book;

		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the tests for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			IWritingSystem ws;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("grc", out ws);
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("hbo", out ws);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			m_ktVwWrapper = null;
			m_draftView = null;
			m_draftForm.Close();
			m_draftForm = null;
			m_book = null;

			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			// create a book with some scripture data in it
			m_book = AddBookToMockedScripture(40, "Matthew");
			IScrSection section = AddSectionToMockedBook(m_book);
			AddSectionHeadParaToSection(section, "Section Heading Text", ScrStyleNames.SectionHead);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);

			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "The beginning of Matthew has some words.", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse two has even more stuff in it with words. And the word possible.", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse three does not have the key term in it.", null);
			AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse four has the word possible in it.", null);
			AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse five has the wrong word impossible in it.", null);

			// Create a key term for the term "words"
			IChkTerm keyTermWords = CreateChkTerm("words");

			// Create references of MAT 1:1, MAT 1:2, and MAT 1:3 for the key term "words"
			CreateChkRef(keyTermWords, 40001001);
			CreateChkRef(keyTermWords, 40001002);
			CreateChkRef(keyTermWords, 40001003);

			// Create a key term for the term "possible"
			IChkTerm keyTermPossible = CreateChkTerm("possible");

			// Create references of MAT 1:2, MAT 1:4, and MAT 1:5 for the key term "possible"
			CreateChkRef(keyTermPossible, 40001002);
			CreateChkRef(keyTermPossible, 40001004);
			CreateChkRef(keyTermPossible, 40001005);

			// Create a draft form, draft view, and a key terms view.
			if (m_draftForm != null)
				m_draftForm.Dispose();
			m_draftForm = new DummyDraftViewForm(Cache);
			m_draftForm.DeleteRegistryKey();

			m_scr.RestartFootnoteSequence = true;

			// Create a key terms view
			DummyDraftViewProxy viewProxy = new DummyDraftViewProxy(Cache);
			m_ktVwWrapper = new DummyKeyTermsViewWrapper(m_draftForm, Cache,
				viewProxy, m_draftForm.SettingsKey);
			m_draftForm.Controls.Add(m_ktVwWrapper);

			// Fill in the renderings view with the Key Terms
			m_ktVwWrapper.Visible = true;
			((ISelectableView)m_ktVwWrapper).ActivateView();
			((DummyDraftView)m_ktVwWrapper.DraftView).MakeRoot();
			//m_keyTermsViewWrapper.RenderingsView.MakeRoot();
#if __MonoCS__
			// calling Form.Show is unreliable, without a main Application loop, on with mono Winforms
			// https://bugzilla.novell.com/show_bug.cgi?id=495562
			m_draftForm.CreateControl();
#else
			m_draftForm.Show();
#endif

			m_draftView = (DummyDraftView)m_ktVwWrapper.DraftView;
			m_draftView.Width = 20;
			m_draftView.Height = 20;
			m_draftView.CallOnLayout();
			m_draftForm.Hide();
		}

		private IChkRef CreateChkRef(IChkTerm keyTermPossible, int bcv)
		{
			IChkRef chkRef = Cache.ServiceLocator.GetInstance<IChkRefFactory>().Create();
			keyTermPossible.OccurrencesOS.Add(chkRef);
			chkRef.Ref = bcv;
			return chkRef;
		}

		private IChkTerm CreateChkTerm(string name)
		{
			ICmPossibilityList keyTermsList = Cache.LangProject.KeyTermsList;
			IChkTerm keyTerm = Cache.ServiceLocator.GetInstance<IChkTermFactory>().Create();
			keyTermsList.PossibilitiesOS.Add(keyTerm);
			keyTerm.Name.set_String(Cache.DefaultUserWs, name);
			return keyTerm;
		}

		private IChkTerm CreateSubChkTerm(IChkTerm parentKeyTerm, string name)
		{
			IChkTerm keyTerm = Cache.ServiceLocator.GetInstance<IChkTermFactory>().Create();
			parentKeyTerm.SubPossibilitiesOS.Add(keyTerm);
			keyTerm.Name.set_String(Cache.DefaultUserWs, name);
			return keyTerm;
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetCurrentLineInRenderingCtrl(int index)
		{
			KeyTermsGrid grid = ReflectionHelper.GetField(m_ktVwWrapper.RenderingsControl,
				"m_dataGridView") as KeyTermsGrid;

			if (grid == null || index < 0 || index >= grid.Rows.Count)
				return;

			grid.ClearSelection();
			grid.CurrentCell = grid[0, index];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Selects the given term in the KT tree and the given occurrence in the list of
		/// occurrences.
		/// </summary>
		/// <param name="sTerm">The analysis term.</param>
		/// <param name="iOccurrence">The i occurrence.</param>
		/// <returns>The selected term</returns>
		/// ------------------------------------------------------------------------------------
		private IChkTerm SelectTermAndOccurrence(string sTerm, int iOccurrence)
		{
			IChkTerm term = SelectTerm(sTerm);
			LoadRenderingsAndSelectOccurrenceInRenderingCtrl(term, iOccurrence);
			return term;
		}

		private IChkRef LoadRenderingsAndSelectOccurrenceInRenderingCtrl(IChkTerm term, int iOccurrence)
		{
			m_ktVwWrapper.RenderingsControl.LoadRenderingsForKeyTerm(term, null);
			SetCurrentLineInRenderingCtrl(iOccurrence);
			return term.OccurrencesOS[iOccurrence];
		}

		private IChkTerm SelectTerm(string sTerm)
		{
			int iTerm = 0;
			switch (sTerm)
			{
				case "word": iTerm = 0; break;
				case "possible": iTerm = 1; break;
			}
			IChkTerm term = (IChkTerm)Cache.LangProject.KeyTermsList.PossibilitiesOS[iTerm];
			return term;
		}
		#endregion

		#region BookFilter tests

//		void CheckOccurrencesCount(int expectedCount, TreeNode node)
//		{
//			Assert.AreEqual(expectedCount, ((IChkTerm)node.Tag).OccurrencesOS.Count);
//		}

		void CheckRenderingsCount(int expectedCount, TreeNode nodeToSelect)
		{
			m_ktVwWrapper.KeyTermsTree.SelectedNode = nodeToSelect;
			KeyTermRenderingsControl renderingsControl = m_ktVwWrapper.RenderingsControl;
			Assert.AreEqual(expectedCount, renderingsControl.ReferenceCount);
		}

		/// <summary>
		/// TE-4500. Filter the terms list in the Key Terms view.
		/// </summary>
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: failing on linux")]
		public void ApplyBookFilterToKeyTermsTree()
		{
			// Add mark and key terms/occurrences to matthew setup.
			IScrBook mark = AddBookMark();
			IChkTerm nonsense1 = CreateChkTerm("nonsense1");
			IChkTerm nonsense1_1 = CreateSubChkTerm(nonsense1, "nonsense1_1");
			CreateChkRef(nonsense1_1, 40001004); // Mat 1:4
			CreateChkRef(nonsense1_1, 41001001); // Mar 1:1
			IChkTerm nonsense1_2 = CreateSubChkTerm(nonsense1, "nonsense1_2");
			CreateChkRef(nonsense1_2, 41001001); // Mar 1:1

			m_ktVwWrapper.ApplyBookFilter = false;
			m_draftView.BookFilter.ShowAllBooks();
			m_ktVwWrapper.UpdateBookFilter();
			KeyTermsTree ktree = m_ktVwWrapper.KeyTermsTree;
			// first make sure we're setup for all the books (i.e. no book filter).
			Assert.IsTrue(m_draftView.BookFilter.AllBooks);
			// key terms "nonsense1", "possible", and "word"
			Assert.AreEqual(3, ktree.Nodes.Count);
			// occurrences of "word" (all in Matthew)
			CheckRenderingsCount(3, ktree.Nodes[2]);
			// sub items for "nonsense"
			Assert.AreEqual(2, ktree.Nodes[0].Nodes.Count);
			// occurrences for nonsense1_1 (in Matthew and Mark)
			CheckRenderingsCount(2, ktree.Nodes[0].Nodes[0]);
			// occurrences for nonsense1_2 (in Mark)
			CheckRenderingsCount(1, ktree.Nodes[0].Nodes[1]);

			// Add Matthew to BookFilter.
			m_ktVwWrapper.ApplyBookFilter = false;
			m_draftView.BookFilter.FilteredBooks = new IScrBook[] { m_book };
			m_ktVwWrapper.UpdateBookFilter();
			// Check KeyTerms
			Assert.AreEqual(1, m_draftView.BookFilter.BookCount);
			// key terms "nonsense1", "possible", and "word"
			Assert.AreEqual(3, ktree.Nodes.Count);
			// occurrences of "word" (all in Matthew)
			CheckRenderingsCount(3, ktree.Nodes[2]);
			// sub items for "nonsense"
			Assert.AreEqual(1, ktree.Nodes[0].Nodes.Count);
			// occurrences for nonsense1_1 (in Matthew and Mark)
			CheckRenderingsCount(2, ktree.Nodes[0].Nodes[0]);
			// Apply Book Filter (Matthew) to Occurrences.
			m_ktVwWrapper.ApplyBookFilter = true;
			CheckRenderingsCount(1, ktree.Nodes[0].Nodes[0]);

			// Remove Matthew and Add Mark
			m_ktVwWrapper.ApplyBookFilter = false;
			m_draftView.BookFilter.FilteredBooks = new IScrBook[] { mark };
			m_ktVwWrapper.UpdateBookFilter();
			// Check KeyTerms
			Assert.AreEqual(1, m_draftView.BookFilter.BookCount);
			// key terms "nonsense" in Mark
			Assert.AreEqual(1, ktree.Nodes.Count);
			// sub items for "nonsense"
			Assert.AreEqual(2, ktree.Nodes[0].Nodes.Count);
			// occurrences for nonsense1_2 (in Mark)
			CheckRenderingsCount(1, ktree.Nodes[0].Nodes[1]);
			// occurrences for nonsense1_1 (in Matthew and Mark)
			CheckRenderingsCount(2, ktree.Nodes[0].Nodes[0]);
			m_ktVwWrapper.ApplyBookFilter = true;
			// Apply Book Filter (Mark) to Occurrences.
			CheckRenderingsCount(1, ktree.Nodes[0].Nodes[0]);
		}

		#endregion BookFilter tests

		#region AssignVernacularEquivalent tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AssignVernacularEquivalent method with an explicitly-defined rendering,
		/// an automatically-defined rendering, and a verse missing the key term rendering.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AssignVernacularEquivalent()
		{
			// Select the word "words" in MAT 1:1 in the draft view
			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, 0, 36, 41);

			// select the key term "words" and its first reference in the key terms view
			IChkTerm keyTerm1 = SelectTermAndOccurrence("words", 0);

			// assign it
			m_ktVwWrapper.CallAssignVernacularEquivalent();

			// check it
			IChkRef ref1 = keyTerm1.OccurrencesOS[0];
			IWfiWordform wordform1 = ref1.RenderingRA;
			Assert.AreEqual("words", wordform1.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.Assigned, ref1.Status);

			IChkRef ref2 = keyTerm1.OccurrencesOS[1];
			IWfiWordform wordform2 = ref2.RenderingRA;
			Assert.AreEqual("words", wordform2.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.AutoAssigned, ref2.Status);

			IChkRef ref3 = keyTerm1.OccurrencesOS[2];
			Assert.AreEqual(null, ref3.RenderingRA);
			Assert.AreEqual(KeyTermRenderingStatus.Unassigned, ref3.Status);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AssignVernacularEquivalent method with an explicitly-defined rendering,
		/// and an automatically-defined rendering. Then explicitly define the automatically-
		/// defined rendering. (TE-6265)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AssignVernacularEquivalent_AfterAutoAssign()
		{
			// Select the word "words" in MAT 1:1 in the draft view
			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, 0, 36, 41);

			// select the key term "words" and its first reference in the key terms view
			IChkTerm keyTerm1 = SelectTermAndOccurrence("words", 0);

			// assign it
			m_ktVwWrapper.CallAssignVernacularEquivalent();

			// Confirm that the second occurrence is auto-assigned with "words".
			Assert.AreEqual(3, keyTerm1.OccurrencesOS.Count);
			IChkRef ref2 = keyTerm1.OccurrencesOS[1];
			IWfiWordform wordform2 = ref2.RenderingRA;
			Assert.AreEqual("words", wordform2.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.AutoAssigned, ref2.Status);

			// Now select and assign the word "stuff" for the autoassigned verse in MAT 1:2.
			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, 0, 67, 72);
			SetCurrentLineInRenderingCtrl(1);

			m_ktVwWrapper.CallAssignVernacularEquivalent();

			// We expect that the status of this occurrence would now be "assigned" and the
			// vernacular rendering would be "stuff".
			wordform2 = ref2.RenderingRA;
			Assert.AreEqual(KeyTermRenderingStatus.Assigned, ref2.Status);
			Assert.AreEqual("stuff", wordform2.Form.VernacularDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AssignVernacularEquivalent method with an explicitly-defined rendering when
		/// the rendering contains an embedded footnote marker. The case for an automatically-defined
		/// rendering and a verse missing the key term rendering are also handled. (TE-5445)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AssignVernacularEquivalent_WithFootnoteEmbedded()
		{
			// Insert a footnote into the middle of the word "words" which will be assigned as
			// the vernacular equivalent.
			IScrSection section = m_book.SectionsOS[0];
			IStTxtPara para = section.ContentOA[0];
			AddFootnote(m_book, para, 38);

			// Select the word "words" in MAT 1:1 in the draft view
			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, 0, 36, 42);

			// select the key term "words" and its first reference in the key terms view
			IChkTerm keyTerm1 = SelectTermAndOccurrence("words", 0);

			// assign it
			m_ktVwWrapper.CallAssignVernacularEquivalent();

			// check it
			IChkRef ref1 = keyTerm1.OccurrencesOS[0];
			IWfiWordform wordform1 = ref1.RenderingRA;
			Assert.AreEqual("words", wordform1.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.Assigned, ref1.Status);

			IChkRef ref2 = keyTerm1.OccurrencesOS[1];
			IWfiWordform wordform2 = ref2.RenderingRA;
			Assert.AreEqual("words", wordform2.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.AutoAssigned, ref2.Status);

			IChkRef ref3 = keyTerm1.OccurrencesOS[2];
			Assert.AreEqual(null, ref3.RenderingRA);
			Assert.AreEqual(KeyTermRenderingStatus.Unassigned, ref3.Status);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AssignVernacularEquivalent method with an explicitly-defined rendering,
		/// an automatically-defined rendering, and a word that contains the key term.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AssignVernacularEquivalent_WholeWords()
		{
			// select the key term "possible" and its first reference in the key terms view
			IChkTerm keyTerm1 = SelectTermAndOccurrence("possible", 0);

			// Select the word "possible" in MAT 1:2 in the draft view
			IStTxtPara para = (IStTxtPara)m_book.SectionsOS[0].ContentOA[0];
			int ichPossible = para.Contents.Text.IndexOf("possible");
			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, 0, ichPossible, ichPossible + 8);

			// assign it
			m_ktVwWrapper.CallAssignVernacularEquivalent();

			// check it
			IChkRef ref1 = keyTerm1.OccurrencesOS[0];
			IWfiWordform wordform1 = ref1.RenderingRA;
			Assert.AreEqual("possible", wordform1.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.Assigned, ref1.Status);

			IChkRef ref2 = keyTerm1.OccurrencesOS[1];
			IWfiWordform wordform2 = ref2.RenderingRA;
			Assert.AreEqual("possible", wordform2.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.AutoAssigned, ref2.Status);

			IChkRef ref3 = keyTerm1.OccurrencesOS[2];
			Assert.AreEqual(null, ref3.RenderingRA);
			Assert.AreEqual(KeyTermRenderingStatus.Unassigned, ref3.Status);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AssignVernacularEquivalent method in multiple books with an explicitly-
		/// defined rendering, an automatically-defined rendering, and a verse missing the key
		/// term rendering.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AssignVernacularEquivalent_MultipleBooks()
		{
			// create another book with some scripture data in it
			AddBookMark();
			IChkTerm keyTerm1 = SetupKeyTermsForMark();

			// check it
			IChkRef ref1 = keyTerm1.OccurrencesOS[0];
			IWfiWordform wordform1 = ref1.RenderingRA;
			Assert.AreEqual("words", wordform1.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.Assigned, ref1.Status);

			IChkRef ref2 = keyTerm1.OccurrencesOS[1];
			IWfiWordform wordform2 = ref2.RenderingRA;
			Assert.AreEqual("words", wordform2.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.AutoAssigned, ref2.Status);

			IChkRef ref3 = keyTerm1.OccurrencesOS[2];
			Assert.AreEqual(null, ref3.RenderingRA);
			Assert.AreEqual(KeyTermRenderingStatus.Unassigned, ref3.Status);

			IChkRef ref4 = keyTerm1.OccurrencesOS[3];
			IWfiWordform wordform4 = ref4.RenderingRA;
			Assert.AreEqual("words", wordform4.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.AutoAssigned, ref4.Status);

			IChkRef ref5 = keyTerm1.OccurrencesOS[4];
			IWfiWordform wordform5 = ref5.RenderingRA;
			Assert.AreEqual("words", wordform5.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.AutoAssigned, ref5.Status);
		}

		private IScrBook AddBookMark()
		{
			IScrBook mark = AddBookToMockedScripture(41, "Mark");
			IScrSection section = AddSectionToMockedBook(mark);
			AddSectionHeadParaToSection(section, "Section Heading Text", ScrStyleNames.SectionHead);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);

			AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "The beginning of Mark has some words.", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse two has even more stuff in it with words.", null);
			AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "Verse three does not have the key term in it.", null);
			return mark;
		}

		/// <summary>
		/// Add occurrences to "words" occurrences.
		/// </summary>
		/// <returns></returns>
		private IChkTerm SetupKeyTermsForMark()
		{
			// select the key term "words" and its first reference in the key terms view
			IChkTerm keyTerm1 = SelectTermAndOccurrence("words", 0);

			AppendMarkOccurrencesToKeyTerm(keyTerm1);

			// Select the word "words" in MAT 1:1 in the draft view
			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, 0, 36, 41);

			// assign it
			m_ktVwWrapper.CallAssignVernacularEquivalent();
			return keyTerm1;
		}

		private void AppendMarkOccurrencesToKeyTerm(IChkTerm keyTerm1)
		{
			// Create references of MRK 1:1 for the key term
			CreateChkRef(keyTerm1, 41001001);

			// Create references of MRK 1:2 for the key term
			CreateChkRef(keyTerm1, 41001002);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AssignVernacularEquivalent method when one of the automatically-defined
		/// key terms is the first reference of a verse bridge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AssignVernacularEquivalent_AtVerseBridgeStart()
		{
			// Add para with verse bridge at end of first section.
			IScrSection section = m_book.SectionsOS[0];
			IStTxtPara para = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "6-8", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This verse bridge also has some words.", null);

			// select the key term "words" and its second reference in the key terms view
			IChkTerm keyTerm1 = SelectTermAndOccurrence("words", 1);

			// Create references of MAT 1:6 for the key term
			IChkRef newRef4 = Cache.ServiceLocator.GetInstance<IChkRefFactory>().Create();
			keyTerm1.OccurrencesOS.Add(newRef4);
			newRef4.Ref = 40001006;

			// Select the word "words" in MAT 1:2 in the draft view
			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, 0, 83, 89);

			// assign it
			m_ktVwWrapper.CallAssignVernacularEquivalent();

			// check it
			Assert.AreEqual(4, keyTerm1.OccurrencesOS.Count);

			IChkRef ref1 = keyTerm1.OccurrencesOS[0];
			IWfiWordform wordform1 = ref1.RenderingRA;
			Assert.AreEqual("words", wordform1.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.AutoAssigned, ref1.Status);

			IChkRef ref2 = keyTerm1.OccurrencesOS[1];
			IWfiWordform wordform2 = ref2.RenderingRA;
			Assert.AreEqual("words", wordform2.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.Assigned, ref2.Status);

			IChkRef ref3 = keyTerm1.OccurrencesOS[2];
			Assert.AreEqual(null, ref3.RenderingRA);
			Assert.AreEqual(KeyTermRenderingStatus.Unassigned, ref3.Status);

			IChkRef ref4 = keyTerm1.OccurrencesOS[3];
			IWfiWordform wordform4 = ref4.RenderingRA;
			Assert.AreEqual("words", wordform4.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.AutoAssigned, ref4.Status);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AssignVernacularEquivalent method when one of the automatically-defined
		/// key terms is within a verse bridge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AssignVernacularEquivalent_WithinVerseBridge()
		{
			// Add para with verse bridge at end of first section.
			IScrSection section = m_book.SectionsOS[0];
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "6-8", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This verse bridge also has some words.", null);

			// select the key term "words" and its second reference in the key terms view
			IChkTerm keyTerm1 = SelectTermAndOccurrence("words", 1);

			// Create references for MAT 1:7 for the key term
			IChkRef newRef4 = Cache.ServiceLocator.GetInstance<IChkRefFactory>().Create();
			keyTerm1.OccurrencesOS.Add(newRef4);
			newRef4.Ref = 40001007;

			// Select the word "words" in MAT 1:2 in the draft view
			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, 0, 83, 89);

			// assign it
			m_ktVwWrapper.CallAssignVernacularEquivalent();

			// check it
			Assert.AreEqual(4, keyTerm1.OccurrencesOS.Count);

			IChkRef ref1 = keyTerm1.OccurrencesOS[0];
			IWfiWordform wordform1 = ref1.RenderingRA;
			Assert.AreEqual("words", wordform1.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.AutoAssigned, ref1.Status);

			IChkRef ref2 = keyTerm1.OccurrencesOS[1];
			IWfiWordform wordform2 = ref2.RenderingRA;
			Assert.AreEqual("words", wordform2.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.Assigned, ref2.Status);

			IChkRef ref3 = keyTerm1.OccurrencesOS[2];
			Assert.AreEqual(null, ref3.RenderingRA);
			Assert.AreEqual(KeyTermRenderingStatus.Unassigned, ref3.Status);

			IChkRef ref4 = keyTerm1.OccurrencesOS[3];
			IWfiWordform wordform4 = ref4.RenderingRA;
			Assert.AreEqual("words", wordform4.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.AutoAssigned, ref4.Status);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the AssignVernacularEquivalent method when one of the automatically-defined
		/// key terms is the last reference of a verse bridge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AssignVernacularEquivalent_AtVerseBridgeEnd()
		{
			// Add para with verse bridge at end of first section.
			IScrSection section = m_book.SectionsOS[0];
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "6-8", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "This verse bridge also has some words.", null);

			// select the key term "words" and its second reference in the key terms view
			IChkTerm keyTerm1 = SelectTermAndOccurrence("words", 1);

			// Create references of MAT 1:8 for the key term
			IChkRef newRef4 = Cache.ServiceLocator.GetInstance<IChkRefFactory>().Create();
			keyTerm1.OccurrencesOS.Add(newRef4);
			newRef4.Ref = 40001008;

			// Select the word "words" in MAT 1:2 in the draft view
			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, 0, 83, 89);

			// assign it
			m_ktVwWrapper.CallAssignVernacularEquivalent();

			// check it
			Assert.AreEqual(4, keyTerm1.OccurrencesOS.Count);

			IChkRef ref1 = keyTerm1.OccurrencesOS[0];
			IWfiWordform wordform1 = ref1.RenderingRA;
			Assert.AreEqual("words", wordform1.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.AutoAssigned, ref1.Status);

			IChkRef ref2 = keyTerm1.OccurrencesOS[1];
			IWfiWordform wordform2 = ref2.RenderingRA;
			Assert.AreEqual("words", wordform2.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.Assigned, ref2.Status);

			IChkRef ref3 = keyTerm1.OccurrencesOS[2];
			Assert.AreEqual(null, ref3.RenderingRA);
			Assert.AreEqual(KeyTermRenderingStatus.Unassigned, ref3.Status);

			IChkRef ref4 = keyTerm1.OccurrencesOS[3];
			IWfiWordform wordform4 = ref4.RenderingRA;
			Assert.AreEqual("words", wordform4.Form.VernacularDefaultWritingSystem.Text);
			Assert.AreEqual(KeyTermRenderingStatus.AutoAssigned, ref4.Status);
		}

		#region UpdateKeyTermEquivalents (TE-4164)

		/// <summary>
		/// Case: Check a rescan doesn't make any changes after non-keyterm-rendering text changes.
		/// </summary>
		[Test]
		public void UpdateKeyTermEquivalents_RescanWithNoChanges()
		{
			AssignVernacularEquivalentForWordsKeytermInMatthew1_1();
			AssignAlternativeRenderingForWordsKeytermInMatthew();

			IChkTerm keyTerm_words = SelectTerm("words");
			m_ktVwWrapper.CallUpdateKeyTermEquivalents();

			// check that everything is still as we expect it
			CheckRenderingAndStatus("words", KeyTermRenderingStatus.Assigned,
				keyTerm_words.OccurrencesOS[0]);
			CheckRenderingAndStatus("words", KeyTermRenderingStatus.AutoAssigned,
				keyTerm_words.OccurrencesOS[1]);
			CheckRenderingAndStatus(null, KeyTermRenderingStatus.Unassigned,
				keyTerm_words.OccurrencesOS[2]);
			CheckRenderingAndStatus("conversation", KeyTermRenderingStatus.Assigned,
				keyTerm_words.OccurrencesOS[3]);
		}

		/// <summary>
		/// Case: User adds an implicit keyterm rendering
		/// Check for a new implicit assignment
		/// </summary>
		[Test]
		public void UpdateKeyTermEquivalents_ChangesForUnassignedOccurrence()
		{
			AssignVernacularEquivalentForWordsKeytermInMatthew1_1();
			// add keyterm to second verse of text.
			IScrSection section = m_book.SectionsOS[0];
			IStTxtPara para = section.ContentOA[0];
			// Modify verse three as follows:
			// "Verse three does not have the key term in it."
			// "Verse three does have the words key term in it now."
			TextSelInfo tsiBeforeEdit;
			TextSelInfo tsiAfterEdit;
			ReplaceVerseText(para, 40001003, "Verse three does have the words key term in it now.",
				out tsiBeforeEdit, out tsiAfterEdit);
			IChkTerm keyTerm_words = SelectTerm("words");
			m_ktVwWrapper.CallUpdateKeyTermEquivalents();

			// check that we changed the status of the third occurrence
			CheckRenderingAndStatus("words", KeyTermRenderingStatus.AutoAssigned,
				keyTerm_words.OccurrencesOS[2]);
			// check that status of other renderings have not changed
			CheckRenderingAndStatus("words", KeyTermRenderingStatus.Assigned,
				keyTerm_words.OccurrencesOS[0]);
			CheckRenderingAndStatus("words", KeyTermRenderingStatus.AutoAssigned,
				keyTerm_words.OccurrencesOS[1]);
		}

		/// <summary>
		/// Case 2a: User radically changes keyterm rendering
		///		Check that a "Assigned" rendering is now "Missing"
		/// Case 2b. User restores a keyterm rendering.
		///		Check that a "Missing" rendering in now "Assigned"
		/// Case 2c: User unradically changes an explicitly assigned keyterm rendering
		///		Check for a new implicit rendering assignment
		/// </summary>
		[Test]
		public void UpdateKeyTermEquivalents_ChangesToExplicitlyAssignedOccurrence()
		{
			// establish "words" as a rendering for "words" keyterm.
			AssignVernacularEquivalentForWordsKeytermInMatthew1_1();
			// establish "conversation" as an alternative rendering for "words" keyterm.
			AssignAlternativeRenderingForWordsKeytermInMatthew();
			// Change Assigned keyterm rendering to an alternative rendering.
			IScrSection section = m_book.SectionsOS[0];
			IStTxtPara para = section.ContentOA[0];

			// Case 2a: User radically changes the text containing the keyterm rendering
			// Modify verse 1 as follows:
			// "The beginning of Matthew has a conversation."
			// "The beginning of Matthew has a sentence."
			TextSelInfo tsiBeforeEdit;
			TextSelInfo tsiAfterEdit;
			ReplaceVerseText(para, 40001001, "The beginning of Matthew has a sentence.",
				out tsiBeforeEdit, out tsiAfterEdit);
			IChkTerm keyTerm_words = SelectTerm("words");
			m_ktVwWrapper.CallUpdateKeyTermEquivalents();

			// Check that a "Assigned" rendering is now "Missing"
			CheckRenderingAndStatus("words", KeyTermRenderingStatus.Missing,
				keyTerm_words.OccurrencesOS[0]);
			// check that everything else is still as we expect it
			CheckRenderingAndStatus("words", KeyTermRenderingStatus.AutoAssigned,
				keyTerm_words.OccurrencesOS[1]);
			CheckRenderingAndStatus(null, KeyTermRenderingStatus.Unassigned,
				keyTerm_words.OccurrencesOS[2]);
			CheckRenderingAndStatus("conversation", KeyTermRenderingStatus.Assigned,
				keyTerm_words.OccurrencesOS[3]);
			// Case 2b. User restores a keyterm rendering.
			// "The beginning of Matthew has a sentence."
			// "The beginning of Matthew has some words."
			ReplaceVerseText(para, 40001001, "The beginning of Matthew has some words.",
				out tsiBeforeEdit, out tsiAfterEdit);
			keyTerm_words = SelectTerm("words");
			m_ktVwWrapper.CallUpdateKeyTermEquivalents();

			// Check that a "Missing" rendering is now "Assigned"
			CheckRenderingAndStatus("words", KeyTermRenderingStatus.Assigned,
				keyTerm_words.OccurrencesOS[0]);
			// check that everything else is still as we expect it
			CheckRenderingAndStatus("words", KeyTermRenderingStatus.AutoAssigned,
				keyTerm_words.OccurrencesOS[1]);
			CheckRenderingAndStatus(null, KeyTermRenderingStatus.Unassigned,
				keyTerm_words.OccurrencesOS[2]);
			CheckRenderingAndStatus("conversation", KeyTermRenderingStatus.Assigned,
				keyTerm_words.OccurrencesOS[3]);

			// Case 2c: User unradically changes the text for an explicitly assigned keyterm rendering
			// Modify verse 1 as follows:
			// "The beginning of Matthew has some words."
			// "The beginning of Matthew has a conversation."
			ReplaceVerseText(para, 40001001, "The beginning of Matthew has a conversation.",
				out tsiBeforeEdit, out tsiAfterEdit);
			keyTerm_words = SelectTerm("words");
			m_ktVwWrapper.CallUpdateKeyTermEquivalents();

			// Check for the new implicit rendering assignment
			CheckRenderingAndStatus("conversation", KeyTermRenderingStatus.AutoAssigned,
				keyTerm_words.OccurrencesOS[0]);
			// check that everything else is still as we expect it
			CheckRenderingAndStatus("words", KeyTermRenderingStatus.AutoAssigned,
				keyTerm_words.OccurrencesOS[1]);
			CheckRenderingAndStatus(null, KeyTermRenderingStatus.Unassigned,
				keyTerm_words.OccurrencesOS[2]);
			CheckRenderingAndStatus("conversation", KeyTermRenderingStatus.Assigned,
				keyTerm_words.OccurrencesOS[3]);
		}

		/// <summary>
		/// Case 3a: User unradically changes an implicit keyterm rendering
		///		Check for a new implicit rendering assignment
		/// Case 3b: User radically changes an implicit keyterm rendering
		///		Check that the implicit assignment is now removed.
		/// </summary>
		[Test]
		public void UpdateKeyTermEquivalents_ChangesToImplicitlyAssignedOccurrence()
		{
			// establish "words" as a rendering for "words" keyterm.
			AssignVernacularEquivalentForWordsKeytermInMatthew1_1();
			// establish "conversation" as an alternative rendering for "words" keyterm.
			AssignAlternativeRenderingForWordsKeytermInMatthew();
			// Change Assigned keyterm rendering to an alternative rendering.
			IScrSection section = m_book.SectionsOS[0];
			IStTxtPara para = section.ContentOA[0];

			// Case 3a: User unradically changes an implicit keyterm rendering
			// Modify verse 2 as follows:
			// "Verse two has even more stuff in it with words. And the word possible.""
			// "Verse two has even more stuff in it with conversation. And the word possible."
			TextSelInfo tsiBeforeEdit;
			TextSelInfo tsiAfterEdit;
			ReplaceVerseText(para, 40001002, "Verse two has even more stuff in it with conversation. And the word possible.",
				out tsiBeforeEdit, out tsiAfterEdit);
			IChkTerm keyTerm_words = SelectTerm("words");
			m_ktVwWrapper.CallUpdateKeyTermEquivalents();

			// Check for a new implicit rendering assignment
			CheckRenderingAndStatus("conversation", KeyTermRenderingStatus.AutoAssigned,
				keyTerm_words.OccurrencesOS[1]);
			// check that everything else is still as we expect it
			CheckRenderingAndStatus("words", KeyTermRenderingStatus.Assigned,
				keyTerm_words.OccurrencesOS[0]);
			CheckRenderingAndStatus(null, KeyTermRenderingStatus.Unassigned,
				keyTerm_words.OccurrencesOS[2]);
			CheckRenderingAndStatus("conversation", KeyTermRenderingStatus.Assigned,
				keyTerm_words.OccurrencesOS[3]);

			// Case 3b: User radically changes an implicit keyterm rendering

			// Case 2b. User restores a keyterm rendering.
			// "Verse two has even more stuff in it with conversation. And the word possible."
			// "Verse two has even more stuff in it with sentences. And the word possible."
			ReplaceVerseText(para, 40001002, "Verse two has even more stuff in it with sentences. And the word possible.",
				out tsiBeforeEdit, out tsiAfterEdit);
			keyTerm_words = SelectTerm("words");
			m_ktVwWrapper.CallUpdateKeyTermEquivalents();

			//	Check that the implicit assignment is now removed.
			// check that everything else is still as we expect it
			CheckRenderingAndStatus(null, KeyTermRenderingStatus.Unassigned,
				keyTerm_words.OccurrencesOS[1]);
			// check that everything else is still as we expect it
			CheckRenderingAndStatus("words", KeyTermRenderingStatus.Assigned,
				keyTerm_words.OccurrencesOS[0]);
			CheckRenderingAndStatus(null, KeyTermRenderingStatus.Unassigned,
				keyTerm_words.OccurrencesOS[2]);
			CheckRenderingAndStatus("conversation", KeyTermRenderingStatus.Assigned,
				keyTerm_words.OccurrencesOS[3]);
		}

		/// <summary>
		/// User changes the text to match a possible rendering, but has previously marked
		/// the status as Ignored.
		/// </summary>
		[Test]
		public void UpdateKeyTermEquivalents_ChangesToExplicitlyIgnoredOccurrence()
		{
			// establish "words" as a rendering for "words" keyterm.
			AssignVernacularEquivalentForWordsKeytermInMatthew1_1();
			// Explicitly Ignore assignment in verse 2.
			SelectTermAndOccurrence("words", 1);
			m_ktVwWrapper.CallIgnoreSpecifyingVernacularEquivalent();
			// Change Assigned keyterm rendering to an alternative rendering.
			IChkTerm keyTerm_words = SelectTerm("words");
			IScrSection section = m_book.SectionsOS[0];
			IStTxtPara para = section.ContentOA[0];
			CheckRenderingAndStatus(null, KeyTermRenderingStatus.Ignored,
				keyTerm_words.OccurrencesOS[1]);

			// Case 2a: User radically changes the text containing the keyterm rendering
			// Modify verse 1 as follows:
			// "Verse two has even more stuff in it with words. And the word possible."
			// "Verse two has even more stuff in it with a sentence. And the word possible."
			TextSelInfo tsiBeforeEdit;
			TextSelInfo tsiAfterEdit;
			ReplaceVerseText(para, 40001002, "Verse two has even more stuff in it with a sentence. And the word possible.",
				out tsiBeforeEdit, out tsiAfterEdit);
			m_ktVwWrapper.CallUpdateKeyTermEquivalents();
			// Check that a "Ignored" rendering is still "Ignored"
			CheckRenderingAndStatus(null, KeyTermRenderingStatus.Ignored,
				keyTerm_words.OccurrencesOS[1]);

			// check that everything else is still as we expect it
			CheckRenderingAndStatus("words", KeyTermRenderingStatus.Assigned,
				keyTerm_words.OccurrencesOS[0]);
			CheckRenderingAndStatus(null, KeyTermRenderingStatus.Unassigned,
				keyTerm_words.OccurrencesOS[2]);
			// Case 2b. User restores a keyterm rendering.
			// "The beginning of Matthew has a sentence."
			// "The beginning of Matthew has some words."
			ReplaceVerseText(para, 40001002, "Verse two has even more stuff in it with words. And the word possible.",
				out tsiBeforeEdit, out tsiAfterEdit);
			keyTerm_words = SelectTerm("words");
			m_ktVwWrapper.CallUpdateKeyTermEquivalents();

			// Check that a "Ignored" rendering is still "Ignored"
			CheckRenderingAndStatus(null, KeyTermRenderingStatus.Ignored,
				keyTerm_words.OccurrencesOS[1]);

			// check that everything else is still as we expect it
			CheckRenderingAndStatus("words", KeyTermRenderingStatus.Assigned,
				keyTerm_words.OccurrencesOS[0]);
			CheckRenderingAndStatus(null, KeyTermRenderingStatus.Unassigned,
				keyTerm_words.OccurrencesOS[2]);
		}

		/// <summary>
		/// Assigns "words" in the draft of Matthew 1:1 to be a vernacular equivalent for "words" keyterm.
		/// </summary>
		private void AssignVernacularEquivalentForWordsKeytermInMatthew1_1()
		{
			SelectWordsKeytermInMatthew1_1();

			// assign it
			m_ktVwWrapper.CallAssignVernacularEquivalent();
		}

		private void SelectWordsKeytermInMatthew1_1()
		{
			// Select the word "words" in MAT 1:1 in the draft view
			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, 0, 36, 41);

			// select the key term "words" and its first reference in the key terms view
			SelectTermAndOccurrence("words", 0);
		}

		/// <summary>
		/// Setup "conversation" as an alternative vernacular equivalent to "words"
		/// </summary>
		private void AssignAlternativeRenderingForWordsKeytermInMatthew()
		{
			IScrSection section = m_book.SectionsOS[0];
			IStTxtPara para = section.ContentOA[0];
			AppendRunWithAlternativeVernacularEquivalentForKeyTerm(para, "words", "conversation", "6", 40001006);
		}

		private void AppendRunWithAlternativeVernacularEquivalentForKeyTerm(IStTxtPara para, string sKeyTerm, string alternative, string verseNumber, int bcv)
		{
			AddRunToMockedPara(para, verseNumber, ScrStyleNames.VerseNumber);
			int ichMinNewRun = para.Contents.Length;
			AddRunToMockedPara(para, String.Format("This verse has a {0}.", alternative), null);
			int ichMinAlternative = ichMinNewRun + "This verse has a ".Length;
			IScrSection section = (IScrSection)para.Owner.Owner;
			m_draftView.RefreshDisplay();
			IChkTerm keyTerm = SelectTerm(sKeyTerm);
			// Create references for the key term
			CreateChkRef(keyTerm, bcv);
			// select the key term and its first reference in the key terms view
			// lookup the reference, load its renderings, select the occurrence in the renderings control
			// assume we're adding the new reference at the end of the current renderings.
			m_ktVwWrapper.RenderingsControl.LoadRenderingsForKeyTerm(keyTerm, null);
			int iOccurrenceInRenderingControl = m_ktVwWrapper.RenderingsControl.ReferenceCount - 1;
			SetCurrentLineInRenderingCtrl(iOccurrenceInRenderingControl);

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, 0, ichMinAlternative, ichMinAlternative + alternative.Length);

			// assign it
			m_ktVwWrapper.CallAssignVernacularEquivalent();
		}

		private void ReplaceVerseText(IStTxtPara para, int bcvToReplace, string replacement,
			out TextSelInfo tsiBeforeEdit, out TextSelInfo tsiAfterEdit)
		{
			((TeEditingHelper)m_draftView.EditingHelper).SelectVerseText((new ScrReference(bcvToReplace, ScrVers.English)), null);
			tsiBeforeEdit = new TextSelInfo(m_draftView.RootBox);
			// para.Contents.Text.IndexOf("Verse three does not have the key term in it.")
			int ichMin = tsiBeforeEdit.IchAnchor;
			int ichLim = tsiBeforeEdit.IchEnd;
			ModifyRunAt(para, ichMin, ichLim, replacement);
			// not sure if this is necessary, since we aren't altering reference structure, just their locations.
			IScrSection section = (IScrSection)para.Owner.Owner;
			((TeEditingHelper)m_draftView.EditingHelper).SelectVerseText((new ScrReference(bcvToReplace, ScrVers.English)), null);
			tsiAfterEdit = new TextSelInfo(m_draftView.RootBox);
		}

		private static void CheckRenderingAndStatus(string expectedRendering, KeyTermRenderingStatus expectedStatus,
			IChkRef chkRef)
		{
			IWfiWordform wordform = chkRef.RenderingRA;
			if (expectedRendering == null)
				Assert.AreEqual(null, wordform);
			else
			{
				Assert.IsNotNull(wordform);
				Assert.AreEqual(expectedRendering, wordform.Form.VernacularDefaultWritingSystem.Text);
			}
			Assert.AreEqual(expectedStatus, chkRef.Status);
		}

		/// <summary>
		/// Modify the run of the text by the given bounds with the replacement string.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="ichMin"></param>
		/// <param name="ichEnd"></param>
		/// <param name="replacement"></param>
		public void ModifyRunAt(IStTxtPara para, int ichMin, int ichEnd, string replacement)
		{
			ITsStrBldr bldr = para.Contents.GetBldr();
			ITsTextProps runStyle = bldr.get_PropertiesAt(ichMin);
			TsRunInfo tri;
			bldr.FetchRunInfoAt(ichMin, out tri);
			// truncate the replacement by the run bounds.
			int ichLim = ichEnd > tri.ichLim ? tri.ichLim : ichEnd;
			bldr.Replace(ichMin, ichLim, replacement, runStyle);
			para.Contents = bldr.GetString();
		}


		#endregion UpdateKeyTermEquivalents (TE-4164)


		#endregion
	}
}
