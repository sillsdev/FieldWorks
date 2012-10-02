// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TaggingTests.cs
// Responsibility: MartinG
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.Common.Utils;

namespace ITextDllTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Contains tests for building trees of annotations on top of interlinear text.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class InterlinTaggingTests : InterlinearTestBase
	{
		static private int s_kTextTagAnnType = -1;
		public const string kFTO_RRG_Semantics = "RRG Semantics";

		IText m_text1;
		XmlDocument m_textsDefn;
		ICmPossibilityList m_textMarkupTags;
		TestTaggingChild m_tagChild;
		int m_hvoPara;

		// Store parsed test Wfic Hvos in this array
		int[] m_hvoWfics;

		// Text Markup Tag Possibility Items
		int[] m_possTagHvos;
		int m_cposs; // count of possibility items loaded

		//const int kflidAbbreviation = (int)CmPossibility.CmPossibilityTags.kflidAbbreviation;
		//const int kflidName = (int)CmPossibility.CmPossibilityTags.kflidName;

		#region SetupTeardown

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			m_textsDefn = new XmlDocument();
			s_kTextTagAnnType = CmAnnotationDefn.TextMarkupTag(Cache).Hvo;
		}

		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();
			InstallVirtuals(@"Language Explorer\Configuration\Words\AreaConfiguration.xml",
				new string[] { "SIL.FieldWorks.IText.ParagraphSegmentsVirtualHandler", "SIL.FieldWorks.IText.OccurrencesInTextsVirtualHandler" });

			// Try moving this up to Fixture Setup, since we don't plan on changing the text here.
			m_text1 = LoadTestText(@"LexText\Interlinear\ITextDllTests\ParagraphParserTestTexts.xml", 1, m_textsDefn);
			m_hvoPara = m_text1.ContentsOA.ParagraphsOS[1].Hvo; // First paragraph contains no text!
			ParseTestText();

			m_tagChild = new TestTaggingChild(Cache);

			// This could change, but at least it gives a reasonably stable list to test from.
			m_textMarkupTags = m_tagChild.CreateDefaultListFromXML();
			LoadTagListHvos();
		}

		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			// UndoEverything before we clear our wordform table, so we can make sure
			// the real wordform list is what we want to start with the next time.
			base.Exit();

			// clear the wordform table.
			m_text1.Cache.LangProject.WordformInventoryOA.ResetAllWordformOccurrences();
			m_text1 = null;
		}

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
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_textsDefn = null;
			m_text1 = null;
			m_textMarkupTags = null;
			m_tagChild = null;
			m_possTagHvos = null;
			m_hvoWfics = null;

			base.Dispose(disposing);
		}

		#endregion

		#region Helper methods

		private int MakeWfic(int hvoObj, int iBegCh, int iEndCh)
		{
			return CmBaseAnnotation.CreateRealAnnotation(Cache, s_kTwficAnnType, 0,
				hvoObj, 0, iBegCh, iEndCh).Hvo;
		}

		/// <summary>
		/// Loads an array of Possibility Hvos from the first tagging list. [RRG Semantics]
		/// </summary>
		private void LoadTagListHvos()
		{
			m_cposs = m_textMarkupTags.PossibilitiesOS[0].SubPossibilitiesOS.Count;
			m_possTagHvos = new int[m_cposs];
			for (int i = 0; i < m_cposs; i++)
				m_possTagHvos[i] = m_textMarkupTags.PossibilitiesOS[0].SubPossibilitiesOS[i].Hvo;
		}

		private void ParseTestText()
		{
			// 0         1         2         3         4         5         6         7         8         9
			// 012345678901234567890    5    0    5    0    5    0    5    0    5    0    5    0    5    0
			// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
			int[,] coords = new int[8, 2] { { 0, 5 }, { 7, 15 }, { 17, 30 }, { 33, 46 }, { 48, 53 }, { 55, 63 }, { 66, 74 }, { 76, 89 } };
			m_hvoWfics = new int[8];
			for (int i = 0; i < 8; i++)
				m_hvoWfics[i] = MakeWfic(m_hvoPara, coords[i, 0], coords[i, 1]);
		}

		#endregion // Helper methods

		#region Verification helpers

		private ICmIndirectAnnotation AssertTagExists(int hvoTag, string msgFailure)
		{
			if (!Cache.IsValidObject(hvoTag))
			{
				Assert.Fail(msgFailure);
			}
			return CmObject.CreateFromDBObject(Cache, hvoTag) as ICmIndirectAnnotation;
		}

		private void AssertTagDeleted(int hvoTag, string msgFailure)
		{
			if (Cache.IsValidObject(hvoTag))
				Assert.Fail(msgFailure);
		}

		private static void VerifyMenuItemCheckStatus(ToolStripItem item1, bool fIsChecked)
		{
			ToolStripMenuItem item = item1 as ToolStripMenuItem;
			Assert.IsNotNull(item, "menu item should be ToolStripMenuItem");
			Assert.AreEqual(fIsChecked, item.Checked, item.Text + " should be " + (fIsChecked ? "checked" : "unchecked"));
		}

		//private static void VerifyMenuItemTextAndChecked(ToolStripItem item1, string text, bool fIsChecked)
		//{
		//    ToolStripMenuItem item = item1 as ToolStripMenuItem;
		//    Assert.IsNotNull(item, "menu item should be ToolStripMenuItem");
		//    Assert.AreEqual(text, item.Text);
		//    Assert.AreEqual(fIsChecked, item.Checked, text + " should be in the expected check state");
		//}

		/// <summary>
		/// Assert that the strip has a menu item with the specified text, and return the menu item.
		/// </summary>
		/// <param name="items">Collection of menu items</param>
		/// <param name="text">Menu item under test</param>
		/// <param name="cItems">Number of submenu items under the item under test</param>
		/// <returns></returns>
		private static ToolStripMenuItem AssertHasMenuWithText(ToolStripItemCollection items, string text, int cItems)
		{
			foreach (ToolStripItem item1 in items)
			{
				ToolStripMenuItem item = item1 as ToolStripMenuItem;
				if (item != null && item.Text == text)
				{
					Assert.AreEqual(cItems, item.DropDownItems.Count, "item " + text + " has wrong number of items");
					return item;
				}
			}
			Assert.Fail("menu should contain item " + text);
			return null;
		}

		/// <summary>
		/// Asserts the checked state of each menu subitem.
		/// </summary>
		/// <param name="expectedStates">The expected checked state array (one per subitem).</param>
		/// <param name="menu1">The menu.</param>
		private static void AssertMenuCheckState(bool[] expectedStates, ToolStripItemCollection menu1)
		{
			Assert.AreEqual(expectedStates.Length, menu1.Count,
				"ExpectedStates array size of " + expectedStates.Length + " is equal to the menu size of " + menu1.Count);
			for (int i = 0; i < expectedStates.Length; i++)
			{
				ToolStripItem item = menu1[i];
				VerifyMenuItemCheckStatus(item, expectedStates[i]);
			}
		}

		#endregion // Verification helpers

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test the contents of a tagging context menu.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void MakeContextMenu_MarkupTags()
		{
			// This may eventually fail because there are no wfics selected.
			// Should we even make the menu if nothing is selected?

			ContextMenuStrip strip = new ContextMenuStrip();
			m_tagChild.CallMakeContextMenuForTags(strip, m_textMarkupTags);

			// Expecting something like:
			//	RRG Semantics
			//		Actor(Act)
			//		Undergoer(Und)
			//		NonMacrorole(NON-MR)
			//	Syntax
			//		Noun Phrase(NP)
			//		Verb Phrase(VP)
			//		Adjective Phrase(AdjP) [Text in () is Abbreviation]

			// Check the tag list item and subitems
			Assert.AreEqual(2, strip.Items.Count);
			ToolStripMenuItem itemMDC = AssertHasMenuWithText(strip.Items, InterlinTaggingChild.kFTO_Syntax, 3);
			AssertHasMenuWithText(itemMDC.DropDownItems, InterlinTaggingChild.kFTO_Noun_Phrase, 0);
			AssertHasMenuWithText(itemMDC.DropDownItems, InterlinTaggingChild.kFTO_Verb_Phrase, 0);
			AssertHasMenuWithText(itemMDC.DropDownItems, InterlinTaggingChild.kFTO_Adjective_Phrase, 0);
			ToolStripMenuItem itemMDC2 = AssertHasMenuWithText(strip.Items, InterlinTaggingChild.kFTO_RRG_Semantics, 3);
			AssertHasMenuWithText(itemMDC2.DropDownItems, InterlinTaggingChild.kFTO_Actor, 0);
			AssertHasMenuWithText(itemMDC2.DropDownItems, InterlinTaggingChild.kFTO_Undergoer, 0);
			AssertHasMenuWithText(itemMDC2.DropDownItems, InterlinTaggingChild.kFTO_Non_Macrorole, 0);
		}

		[Test]
		public void MakeTagAnnotation_CompleteWith3Wfics()
		{
			// Provide hvoTagPoss and SelectedWfics, create a markup tag and examine it.

			// Setup the SelectedWfics property
			List<int> tempList = new List<int> ();
			tempList.Add(m_hvoWfics[0]);
			tempList.Add(m_hvoWfics[1]);
			tempList.Add(m_hvoWfics[2]);
			m_tagChild.SelectedWfics = tempList;

			// SUT
			ICmIndirectAnnotation tagAnn = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[0]);

			// Verification
			Assert.IsNotNull(tagAnn, "Should have made a Tag.");
			Assert.AreEqual(s_kTextTagAnnType, tagAnn.AnnotationTypeRAHvo,
				"TextTag has wrong AnnotationType.");
			Assert.AreEqual(m_possTagHvos[0], tagAnn.InstanceOfRAHvo, "Text Tag has wrong possibility Hvo.");
			Assert.AreEqual(3, tagAnn.AppliesToRS.Count);
			for (int i = 0; i < 3; i++)
				Assert.AreEqual(tempList[i], tagAnn.AppliesToRS[i].Hvo, "Something wrong in TextTag AppliesTo["+i+"].");
		}

		[Test]
		public void MakeTagAnnotation_With2Wfics()
		{
			// Provide hvoTagPoss and SelectedWfics, create a markup tag and examine it.

			// Setup the SelectedWfics property
			List<int> tempList = new List<int> ();
			tempList.Add(m_hvoWfics[0]);
			tempList.Add(m_hvoWfics[1]);
			m_tagChild.SelectedWfics = tempList;

			// SUT
			ICmIndirectAnnotation tagAnn = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[1]);

			// Verification
			Assert.IsNotNull(tagAnn, "Should have made a Tag.");
			Assert.AreEqual(m_possTagHvos[1], tagAnn.InstanceOfRAHvo, "Text Tag has wrong possibility Hvo.");
			Assert.AreEqual(2, tagAnn.AppliesToRS.Count);
			for (int i = 0; i < 2; i++)
				Assert.AreEqual(m_hvoWfics[i], tagAnn.AppliesToRS[i].Hvo,
					"Something wrong in TextTag AppliesTo[" + i + "].");
		}

		[Test]
		public void MakeTagAnnotation_With1Wfic()
		{
			// Provide hvoTagPoss and SelectedWfics, create a markup tag and examine it.

			// Setup the SelectedWfics property
			List<int> tempList = new List<int> ();
			tempList.Add(m_hvoWfics[1]);
			m_tagChild.SelectedWfics = tempList;

			// SUT
			// Use the last possibility tag in the first list of tags
			ICmIndirectAnnotation tagAnn = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[m_cposs - 1]);

			// Verification
			Assert.IsNotNull(tagAnn, "Should have made a Tag.");
			Assert.AreEqual(m_possTagHvos[m_cposs - 1], tagAnn.InstanceOfRAHvo, "Text Tag has wrong possibility Hvo.");
			Assert.AreEqual(1, tagAnn.AppliesToRS.Count);
			Assert.AreEqual(m_hvoWfics[1], tagAnn.AppliesToRS[0].Hvo, "Something wrong in TextTag AppliesTo[0].");
		}

		[Test]
		public void MakeTagAnnotation_WithNoWfic()
		{
			// Provide hvoTagPoss and SelectedWfics, create a markup tag and examine it.
			// Setup the SelectedWfics property
			m_tagChild.SelectedWfics = null;

			// SUT
			ICmIndirectAnnotation tagAnn = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[m_cposs - 1]);

			Assert.IsNull(tagAnn, "MakeTextTagAnn must return null if no Wfics are selected.");
		}

		[Test]
		public void MakeTagAnnot_ExistTag_CompleteOverlap()
		{
			// This test will need changing when we allow multiple lines of tagging.

			// Setup the SelectedWfics property for first tag
			List<int> tempList = new List<int> ();
			tempList.Add(m_hvoWfics[1]);
			tempList.Add(m_hvoWfics[2]);
			m_tagChild.SelectedWfics = tempList;

			int hvoInitialTag = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[0]).Hvo;

			// Make sure SelectedWfics property is still set to the same Wfics
			m_tagChild.SelectedWfics = tempList;

			// SUT
			// Make a second tag annotation with a different possibility pointing to the same Wfics
			ICmIndirectAnnotation sUTTag = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[1]);

			// Verification
			Assert.IsNotNull(sUTTag, "Should have made a Tag.");
			// The first tag should no longer exist in the Cache
			AssertTagDeleted(hvoInitialTag, "Should have deleted the initial tag.");
		}

		[Test]
		public void MakeTagAnnot_ExistTag_FrontOverlap()
		{
			// This test will need changing when we allow multiple lines of tagging.

			// Setup the SelectedWfics property for first tag
			List<int> tempList = new List<int> ();
			tempList.Add(m_hvoWfics[1]);
			tempList.Add(m_hvoWfics[2]);
			m_tagChild.SelectedWfics = tempList;

			int hvoInitialTag = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[0]).Hvo;

			// Setup the SelectedWfics property for second tag
			List<int> tempList1 = new List<int> ();
			tempList1.Add(m_hvoWfics[0]);
			tempList1.Add(m_hvoWfics[1]);
			m_tagChild.SelectedWfics = tempList1;

			// SUT
			// Make a second tag annotation with a different possibility pointing to the new Wfics
			ICmIndirectAnnotation tagSUT = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[1]);

			// Verification

			// Verify new tag
			Assert.IsNotNull(tagSUT, "Should have made a Tag.");
			Assert.AreEqual(m_possTagHvos[1], tagSUT.InstanceOfRAHvo, "Text Tag has wrong possibility Hvo.");
			Assert.AreEqual(2, tagSUT.AppliesToRS.Count);
			Assert.AreEqual(m_hvoWfics[0], tagSUT.AppliesToRS[0].Hvo, "Something wrong in new TextTag AppliesTo[0].");
			Assert.AreEqual(m_hvoWfics[1], tagSUT.AppliesToRS[1].Hvo, "Something wrong in new TextTag AppliesTo[1].");

			// The old tag should no longer exist in the Cache
			AssertTagDeleted(hvoInitialTag, "Should have deleted the initial tag.");
		}

		[Test]
		public void MakeTagAnnot_ExistTag_RearOverlap()
		{
			// This test will need changing when we allow multiple lines of tagging.

			// Setup the SelectedWfics property for first tag
			List<int> tempList = new List<int> ();
			tempList.Add(m_hvoWfics[1]);
			tempList.Add(m_hvoWfics[2]);
			m_tagChild.SelectedWfics = tempList;

			int hvoInitialTag = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[0]).Hvo;

			// Setup the SelectedWfics property for second tag
			List<int> tempList1 = new List<int> ();
			tempList1.Add(m_hvoWfics[2]);
			tempList1.Add(m_hvoWfics[3]);
			tempList1.Add(m_hvoWfics[4]);
			m_tagChild.SelectedWfics = tempList1;

			// SUT
			// Make a second tag annotation with a different possibility pointing to the new Wfics
			ICmIndirectAnnotation tagSUT = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[2]);

			// Verification

			// Verify new tag
			Assert.IsNotNull(tagSUT, "Should have made a Tag.");
			Assert.AreEqual(m_possTagHvos[2], tagSUT.InstanceOfRAHvo, "Text Tag has wrong possibility Hvo.");
			Assert.AreEqual(3, tagSUT.AppliesToRS.Count);
			Assert.AreEqual(m_hvoWfics[2], tagSUT.AppliesToRS[0].Hvo, "Something wrong in new TextTag AppliesTo[0].");
			Assert.AreEqual(m_hvoWfics[3], tagSUT.AppliesToRS[1].Hvo, "Something wrong in new TextTag AppliesTo[1].");
			Assert.AreEqual(m_hvoWfics[4], tagSUT.AppliesToRS[2].Hvo, "Something wrong in new TextTag AppliesTo[2].");

			// The old tag should no longer exist in the Cache
			AssertTagDeleted(hvoInitialTag, "Should have deleted the initial tag.");
		}

		[Test]
		public void MakeTagAnnot_MultipleExistTags_Overlap()
		{
			// This test will need changing when we allow multiple lines of tagging.

			// Setup the SelectedWfics property for first (existing) tag
			List<int> tempList = new List<int> ();
			tempList.Add(m_hvoWfics[1]);
			tempList.Add(m_hvoWfics[2]);
			m_tagChild.SelectedWfics = tempList;

			int hvoOldTag1 = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[0]).Hvo;

			// Setup the SelectedWfics property for second (existing) tag
			List<int> tempList1 = new List<int> ();
			tempList1.Add(m_hvoWfics[3]);
			tempList1.Add(m_hvoWfics[4]);
			m_tagChild.SelectedWfics = tempList1;

			int hvoOldTag2 = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[1]).Hvo;

			// Setup the SelectedWfics property for SUT tag
			// These overlap with both existing tags
			List<int> tempList2 = new List<int> ();
			tempList2.Add(m_hvoWfics[2]);
			tempList2.Add(m_hvoWfics[3]);
			m_tagChild.SelectedWfics = tempList2;

			// SUT
			// Make a third tag annotation with a different possibility pointing to the new Wfics
			ICmIndirectAnnotation tagSUT = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[2]);

			// Verification

			// Verify new tag
			Assert.IsNotNull(tagSUT, "Should have made a Tag.");
			Assert.AreEqual(m_possTagHvos[2], tagSUT.InstanceOfRAHvo, "Text Tag has wrong possibility Hvo.");
			Assert.AreEqual(2, tagSUT.AppliesToRS.Count);
			Assert.AreEqual(m_hvoWfics[2], tagSUT.AppliesToRS[0].Hvo, "Something wrong in new TextTag AppliesTo[0].");
			Assert.AreEqual(m_hvoWfics[3], tagSUT.AppliesToRS[1].Hvo, "Something wrong in new TextTag AppliesTo[1].");

			// The old tags (2) should no longer exist in the Cache
			AssertTagDeleted(hvoOldTag1, "Should have deleted the first pre-existing tag.");
			AssertTagDeleted(hvoOldTag2, "Should have deleted the second pre-existing tag.");
		}

		[Test]
		public void MakeTagAnnot_ExistTag_NoOverlap()
		{
			// Setup the SelectedWfics property for first tag
			List<int> tempList = new List<int> ();
			tempList.Add(m_hvoWfics[0]);
			tempList.Add(m_hvoWfics[1]);
			m_tagChild.SelectedWfics = tempList;

			int hvoInitialTag = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[0]).Hvo;

			// Setup the SelectedWfics property for second tag
			List<int> tempList1 = new List<int> ();
			tempList1.Add(m_hvoWfics[2]);
			tempList1.Add(m_hvoWfics[3]);
			tempList1.Add(m_hvoWfics[4]);
			m_tagChild.SelectedWfics = tempList1;

			// SUT
			// Make a second tag annotation with a different possibility pointing to the new Wfics
			ICmIndirectAnnotation tagSUT = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[2]);

			// Verification

			// Verify new tag
			Assert.IsNotNull(tagSUT, "Should have made a Tag.");
			Assert.AreEqual(m_possTagHvos[2], tagSUT.InstanceOfRAHvo, "Text Tag has wrong possibility Hvo.");
			Assert.AreEqual(3, tagSUT.AppliesToRS.Count);
			Assert.AreEqual(m_hvoWfics[2], tagSUT.AppliesToRS[0].Hvo, "Something wrong in new TextTag AppliesTo[0].");
			Assert.AreEqual(m_hvoWfics[3], tagSUT.AppliesToRS[1].Hvo, "Something wrong in new TextTag AppliesTo[1].");
			Assert.AreEqual(m_hvoWfics[4], tagSUT.AppliesToRS[2].Hvo, "Something wrong in new TextTag AppliesTo[2].");

			// The old tag should still exist in the Cache
			ICmIndirectAnnotation oldtag = AssertTagExists(hvoInitialTag, "Shouldn't have deleted the initial tag.");
			Assert.AreEqual(m_possTagHvos[0], oldtag.InstanceOfRAHvo, "Text Tag has wrong possibility Hvo.");
			Assert.AreEqual(2, oldtag.AppliesToRS.Count);
			Assert.AreEqual(m_hvoWfics[0], oldtag.AppliesToRS[0].Hvo, "Something wrong in old TextTag AppliesTo[0].");
			Assert.AreEqual(m_hvoWfics[1], oldtag.AppliesToRS[1].Hvo, "Something wrong in old TextTag AppliesTo[1].");
		}

		[Test]
		public void DeleteTagAnnot_SetOfTwo()
		{
			Set<int> hvosToDelete = new Set<int>();

			// Setup the SelectedWfics property for first tag
			List<int> tempList = new List<int> ();
			tempList.Add(m_hvoWfics[0]);
			tempList.Add(m_hvoWfics[1]);
			m_tagChild.SelectedWfics = tempList;

			int hvoFirstTag = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[0]).Hvo;
			hvosToDelete.Add(hvoFirstTag);

			// Setup the SelectedWfics property for second tag
			tempList = new List<int> ();
			tempList.Add(m_hvoWfics[2]);
			tempList.Add(m_hvoWfics[3]);
			tempList.Add(m_hvoWfics[4]);
			m_tagChild.SelectedWfics = tempList;

			int hvoSecondTag = m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[1]).Hvo;
			hvosToDelete.Add(hvoSecondTag);

			// SUT
			m_tagChild.CallDeleteTextTagAnnotations(hvosToDelete);

			// Verification
			// The two tags should no longer exist in the Cache
			AssertTagDeleted(hvoFirstTag, "Should have deleted the first tag.");
			AssertTagDeleted(hvoSecondTag, "Should have deleted the second tag.");
		}

		[Test]
		public void MakeContextMenu_CheckedStates_NoSel()
		{
			// DON'T set up a selection; the expected result for this test may change at a later time
			// SUT
			ContextMenuStrip menu = new ContextMenuStrip();
			m_tagChild.CallMakeContextMenuForTags(menu, m_textMarkupTags);

			// Verification of SUT; None should be checked
			ToolStripMenuItem subMenu = AssertHasMenuWithText(menu.Items, "Syntax", 3);
			Assert.IsNotNull(subMenu, "No Syntax menu!?");
			foreach (ToolStripItem item in subMenu.DropDownItems)
				VerifyMenuItemCheckStatus(item, false);
		}

		[Test]
		public void MakeContextMenu_CheckedStates_SelMatchesTag()
		{
			// Provide hvoTagPoss and SelectedWfics, create a markup tag and examine it.
			// Setup the SelectedWfics property
			List<int> tempList = new List<int> ();
			tempList.Add(m_hvoWfics[0]);
			tempList.Add(m_hvoWfics[1]);
			m_tagChild.SelectedWfics = tempList;

			// SUT
			const int itestItem = 1;
			m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[itestItem]);
			ContextMenuStrip menu = new ContextMenuStrip();
			m_tagChild.CallMakeContextMenuForTags(menu, m_textMarkupTags);

			// Verification of SUT; Only one should be checked and that in a submenu
			ToolStripMenuItem subList = AssertHasMenuWithText(menu.Items, kFTO_RRG_Semantics, 3);
			Assert.IsNotNull(subList, "No " + kFTO_RRG_Semantics + " menu!?");
			ToolStripItemCollection subMenu = subList.DropDownItems;
			bool[] expectedStates = new bool[subMenu.Count];
			expectedStates[itestItem] = true;
			AssertMenuCheckState(expectedStates, subMenu);
		}

		[Test]
		public void MakeContextMenu_CheckedStates_SelFirstOfTag()
		{
			// Provide hvoTagPoss and SelectedWfics, create a markup tag and examine it.

			// Setup the SelectedWfics property
			List<int> tempList = new List<int>();
			tempList.Add(m_hvoWfics[0]);
			tempList.Add(m_hvoWfics[1]);
			tempList.Add(m_hvoWfics[2]);
			m_tagChild.SelectedWfics = tempList;

			// Make a tag for selection
			const int itestItem = 0; // first tag in list
			m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[itestItem]);

			// Reduce selection to first word
			tempList.Remove(m_hvoWfics[1]);
			tempList.Remove(m_hvoWfics[2]);
			m_tagChild.SelectedWfics = tempList;

			// SUT
			ContextMenuStrip menu = new ContextMenuStrip();
			m_tagChild.CallMakeContextMenuForTags(menu, m_textMarkupTags);

			// Verification of SUT; Only one should be checked and that in a submenu
			ToolStripMenuItem subList = AssertHasMenuWithText(menu.Items, kFTO_RRG_Semantics, 3);
			Assert.IsNotNull(subList, "No " + kFTO_RRG_Semantics + " menu!?");
			ToolStripItemCollection subMenu = subList.DropDownItems;
			bool[] expectedStates = new bool[subMenu.Count];
			expectedStates[itestItem] = true;
			AssertMenuCheckState(expectedStates, subMenu);
		}

		/// <summary>
		/// Tests which items are checked on the context menu when two tags exist for one
		/// wordform and the current selection is on the last wordform in both tags.
		/// </summary>
		[Test]
		public void MakeContextMenu_CheckedStates_MultiAnnSelLast()
		{
			// This test will need changing when we allow multiple lines of tagging.

			// Provide hvoTagPoss and SelectedWfics, create a markup tag and examine it.
			// Setup the SelectedWfics property to include 3 words
			List<int> tempList = new List<int>();
			tempList.Add(m_hvoWfics[0]);
			tempList.Add(m_hvoWfics[1]);
			tempList.Add(m_hvoWfics[2]);
			m_tagChild.SelectedWfics = tempList;

			// Make a tag for 1st selection
			const int itestItem1 = 0; // first tag in list
			m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[itestItem1]);

			// Setup the SelectedWfics property to include 2nd & 3rd words
			tempList.Remove(m_hvoWfics[0]);
			m_tagChild.SelectedWfics = tempList;

			// Make a tag for 2nd selection
			// TODO GordonM: For the time being, this deletes the first tag. When multiple layers
			// of tags are allowed, change the expected results.
			const int itestItem2 = 1; // 2nd tag in list
			m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[itestItem2]);

			// Reduce selection to 3rd word
			tempList.Remove(m_hvoWfics[1]);
			m_tagChild.SelectedWfics = tempList;

			// SUT; make menu while selected word has 2 tags
			ContextMenuStrip menu = new ContextMenuStrip();
			m_tagChild.CallMakeContextMenuForTags(menu, m_textMarkupTags);

			// Verification of SUT; Two should be checked and both in a submenu
			ToolStripMenuItem subList = AssertHasMenuWithText(menu.Items, kFTO_RRG_Semantics, 3);
			Assert.IsNotNull(subList, "No " + kFTO_RRG_Semantics + " menu!?");
			ToolStripItemCollection subMenu = subList.DropDownItems;
			bool[] expectedStates = new bool[subMenu.Count];
			expectedStates[itestItem1] = false; // TODO GordonM: This will change to 'true' when multiple layers are okay.
			expectedStates[itestItem2] = true;
			AssertMenuCheckState(expectedStates, subMenu);
		}

		/// <summary>
		/// Tests which items are checked on the context menu when two tags exist for one
		/// wordform and the current selection overlaps those tags (and non-tagged wfics).
		/// </summary>
		[Test]
		public void MakeContextMenu_CheckedStates_MultiAnnSelExtra()
		{
			// This test will need changing when we allow multiple lines of tagging.

			// Provide hvoTagPoss and SelectedWfics, create a markup tag and examine it.
			// Setup the SelectedWfics property to include 2 words
			List<int> tempList = new List<int>();
			tempList.Add(m_hvoWfics[1]);
			tempList.Add(m_hvoWfics[2]);
			m_tagChild.SelectedWfics = tempList;

			// Make a tag for 1st selection
			int itestItem1 = 0; // first tag in list
			m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[itestItem1]);

			// Setup the SelectedWfics property to include one more word
			tempList.Add(m_hvoWfics[3]);
			m_tagChild.SelectedWfics = tempList;

			// Make a tag for 2nd selection
			// TODO GordonM: For the time being, this deletes the first tag. When multiple layers
			// of tags are allowed, change the expected results.
			int itestItem2 = 1; // 2nd tag in list
			m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[itestItem2]);

			// Setup selection to contain previous tags, plus another word
			tempList.Insert(0, m_hvoWfics[0]);
			m_tagChild.SelectedWfics = tempList;

			// SUT; make menu while selected phrase has 2 tags, but the initial
			// wfic has no tags.
			ContextMenuStrip menu = new ContextMenuStrip();
			m_tagChild.CallMakeContextMenuForTags(menu, m_textMarkupTags);

			// Verification of SUT; Two should be checked and that in a submenu
			ToolStripMenuItem subList = AssertHasMenuWithText(menu.Items, kFTO_RRG_Semantics, 3);
			Assert.IsNotNull(subList, "No " + kFTO_RRG_Semantics + " menu!?");
			ToolStripItemCollection subMenu = subList.DropDownItems;
			bool[] expectedStates = new bool[subMenu.Count];
			expectedStates[itestItem1] = false;
			expectedStates[itestItem2] = true;
			AssertMenuCheckState(expectedStates, subMenu);
		}

		/// <summary>
		/// Tests which items are checked on the context menu when two side-by-side tags
		/// are covered (at least partially) by the current selection.
		/// </summary>
		[Test]
		public void MakeContextMenu_CheckedStates_SelCoversMultiSideBySideAnn()
		{
			// Provide hvoTagPoss and SelectedWfics, create a markup tag and examine it.
			// Setup the SelectedWfics property to include 2 words
			List<int> tempList = new List<int>();
			tempList.Add(m_hvoWfics[0]);
			tempList.Add(m_hvoWfics[1]);
			m_tagChild.SelectedWfics = tempList;

			// Make a tag for 1st selection
			int itestItem1 = 0; // first tag in list
			m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[itestItem1]);

			// Setup the SelectedWfics property to include one more word
			tempList.Clear();
			tempList.Add(m_hvoWfics[2]);
			tempList.Add(m_hvoWfics[3]);
			m_tagChild.SelectedWfics = tempList;

			// Make a tag for 2nd selection
			int itestItem2 = 1; // 2nd tag in list
			m_tagChild.CallMakeTextTagAnnotation(m_possTagHvos[itestItem2]);

			// Setup selection to cover part of first tag, and all of second
			tempList.Insert(0, m_hvoWfics[1]);
			m_tagChild.SelectedWfics = tempList;

			// SUT; make menu while selected phrase has 2 side-by-side tags
			ContextMenuStrip menu = new ContextMenuStrip();
			m_tagChild.CallMakeContextMenuForTags(menu, m_textMarkupTags);

			// Verification of SUT; Two should be checked and that in a submenu
			ToolStripMenuItem subList = AssertHasMenuWithText(menu.Items, kFTO_RRG_Semantics, 3);
			Assert.IsNotNull(subList, "No " + kFTO_RRG_Semantics + " menu!?");
			ToolStripItemCollection subMenu = subList.DropDownItems;
			bool[] expectedStates = new bool[subMenu.Count];
			expectedStates[itestItem1] = true;
			expectedStates[itestItem2] = true;
			AssertMenuCheckState(expectedStates, subMenu);
		}
	}
}
