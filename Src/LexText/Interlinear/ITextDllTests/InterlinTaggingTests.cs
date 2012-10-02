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
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.IText
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Contains tests for building trees of TextTags on top of interlinear text.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class InterlinTaggingTests : InterlinearTestBase
	{
		private FDO.IText m_text1;
		private XmlDocument m_textsDefn;
		private IWritingSystem m_wsXkal;
		private ICmPossibilityList m_textMarkupTags;
		private TestTaggingChild m_tagChild;
		private IStTxtPara m_para1;
		private ITextTagRepository m_tagRepo;

		// Store parsed test Occurrences in this array
		private AnalysisOccurrence[] m_occurrences;

		// Text Markup Tag Possibility Items
		private ICmPossibility[] m_possTags;
		private int m_cposs; // count of possibility items loaded

		#region SetupTeardown

		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				DoSetupFixture);
		}

		/// <summary>
		///
		/// </summary>
		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			m_textsDefn = null;
			m_tagRepo = null;
			if (m_tagChild != null)
				m_tagChild.Dispose();
			m_tagChild = null;

			base.FixtureTeardown();
		}

		public override void TestSetup()
		{
			base.TestSetup();

			m_actionHandler.EndUndoTask(); // Ends Undo task begin in base.TestSetup, because I want to use UOW.
		}
		#endregion

		#region ForTestOnlyConstants

		public const string kFTO_RRG_Semantics = "RRG Semantics";
		public const string kFTO_Actor = "ACTOR";
		public const string kFTO_Non_Macrorole = "NON-MACROROLE";
		public const string kFTO_Undergoer = "UNDERGOER";
		public const string kFTO_Syntax = "Syntax";
		public const string kFTO_Noun_Phrase = "Noun Phrase";
		public const string kFTO_Verb_Phrase = "Verb Phrase";
		public const string kFTO_Adjective_Phrase = "Adjective Phrase";

		#endregion

		#region Helper methods

		/// <summary>
		/// non-undoable task
		/// </summary>
		private void DoSetupFixture()
		{
			// setup default vernacular ws.
			m_wsXkal = Cache.ServiceLocator.WritingSystemManager.Set("x-kal");
			m_wsXkal.DefaultFontName = "Times New Roman";
			Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(m_wsXkal);
			Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Insert(0, m_wsXkal);
			m_textsDefn = new XmlDocument();
			m_tagRepo = Cache.ServiceLocator.GetInstance<ITextTagRepository>();
			ConfigurationFilePath("Language Explorer/Configuration/Words/AreaConfiguration.xml");
			m_text1 = LoadTestText("FDO/FDOTests/TestData/ParagraphParserTestTexts.xml", 1, m_textsDefn);
			m_para1 = m_text1.ContentsOA.ParagraphsOS[0] as IStTxtPara;
			ParseTestText();

			m_tagChild = new TestTaggingChild(Cache);
			m_tagChild.SetText(m_text1.ContentsOA);

			// This could change, but at least it gives a reasonably stable list to test from.
			m_textMarkupTags = Cache.LangProject.GetDefaultTextTagList();
			LoadTagListPossibilities();
		}

		/// <summary>
		/// Loads an array of Possibilities from the first tagging list. [RRG Semantics]
		/// </summary>
		private void LoadTagListPossibilities()
		{
			m_cposs = m_textMarkupTags.PossibilitiesOS[0].SubPossibilitiesOS.Count;
			m_possTags = new ICmPossibility[m_cposs];
			for (int i = 0; i < m_cposs; i++)
				m_possTags[i] = m_textMarkupTags.PossibilitiesOS[0].SubPossibilitiesOS[i];
		}

		private void ParseTestText()
		{
			// Seg:  0								1								2
			// Index:0	  1		 2			 3 0			  1	  2		3 0		 1			 2
			//		xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
			using (var pp = new ParagraphParser(Cache))
			{
				pp.Parse(m_para1);
			}
			var coords = new int[8, 2] { { 0, 0 }, { 0, 1 }, { 0, 2 }, { 1, 0 }, { 1, 1 }, { 1, 2 }, { 2, 0 }, { 2, 1 } };
			m_occurrences = new AnalysisOccurrence[8];
			for (int i = 0; i < 8; i++)
				m_occurrences[i] = new AnalysisOccurrence(m_para1.SegmentsOS[coords[i, 0]], coords[i, 1]);
		}

		#endregion // Helper methods

		#region Verification helpers

		private void AssertTagExists(int hvoTag, string msgFailure)
		{
			ITextTag tag = null;
			try
			{
				tag = m_tagRepo.GetObject(hvoTag);
			}
			catch (KeyNotFoundException)
			{
				Assert.Fail(msgFailure);
			}
			Assert.IsNotNull(tag.TagRA, msgFailure);
		}

		private void AssertTagDoesntExist(int hvoTag, string msgFailure)
		{
			try
			{
				var tag = m_tagRepo.GetObject(hvoTag);
				if (tag != null && tag.Hvo == hvoTag)
					Assert.Fail(msgFailure); // We didn't want to find this tag! It should be deleted!
				// If we get here, the tag is already marked for deletion in the Cache, but still undoable.
				// We still pass.
			}
			catch (KeyNotFoundException)
			{
				// We pass the test with an expected (and caught) exception
			}
		}

		/// <summary>
		/// Verify that the parameter tag points to the right possibility marker
		/// and that it refers to the correct begin and end points in the text.
		/// </summary>
		/// <param name="ttag"></param>
		/// <param name="poss"></param>
		/// <param name="point1"></param>
		/// <param name="point2"></param>
		private static void VerifyTextTag(ITextTag ttag, ICmPossibility poss,
										  AnalysisOccurrence point1, AnalysisOccurrence point2)
		{
			Assert.IsNotNull(ttag, "There should be a TextTag object.");
			Assert.AreEqual(poss.Hvo, ttag.TagRA.Hvo, "Text Tag has wrong possibility Hvo.");
			Assert.AreEqual(point1.Segment.Hvo, ttag.BeginSegmentRA.Hvo, "Tag has wrong BeginSegment");
			Assert.AreEqual(point1.Index, ttag.BeginAnalysisIndex, "Tag has wrong BeginAnalysisIndex");
			Assert.AreEqual(point2.Segment.Hvo, ttag.EndSegmentRA.Hvo, "Tag has wrong EndSegment");
			Assert.AreEqual(point2.Index, ttag.EndAnalysisIndex, "Tag has wrong EndAnalysisIndex");
		}

		private static void VerifyMenuItemCheckStatus(ToolStripItem item1, bool fIsChecked)
		{
			var item = item1 as ToolStripMenuItem;
			Assert.IsNotNull(item, "menu item should be ToolStripMenuItem");
			Assert.AreEqual(fIsChecked, item.Checked, item.Text + " should be " + (fIsChecked ? "checked" : "unchecked"));
		}

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
			// This may eventually fail because there are no occurrences selected.
			// Should we even make the menu if nothing is selected?

			using (ContextMenuStrip strip = new ContextMenuStrip())
			{
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
				ToolStripMenuItem itemMDC = AssertHasMenuWithText(strip.Items, kFTO_Syntax, 3);
				AssertHasMenuWithText(itemMDC.DropDownItems, kFTO_Noun_Phrase, 0);
				AssertHasMenuWithText(itemMDC.DropDownItems, kFTO_Verb_Phrase, 0);
				AssertHasMenuWithText(itemMDC.DropDownItems, kFTO_Adjective_Phrase, 0);
				ToolStripMenuItem itemMDC2 = AssertHasMenuWithText(strip.Items, kFTO_RRG_Semantics, 3);
				AssertHasMenuWithText(itemMDC2.DropDownItems, kFTO_Actor, 0);
				AssertHasMenuWithText(itemMDC2.DropDownItems, kFTO_Undergoer, 0);
				AssertHasMenuWithText(itemMDC2.DropDownItems, kFTO_Non_Macrorole, 0);
			}
		}

		[Test]
		public void MakeTag_CompleteWith3Wordforms()
		{
			// Provide hvoTagPoss and SelectedWordforms, create a markup tag and examine it.

			// Setup the SelectedWordforms property
			var tempList = new List<AnalysisOccurrence> {m_occurrences[0], m_occurrences[1], m_occurrences[2]};
			m_tagChild.SelectedWordforms = tempList;

			// SUT
			ITextTag ttag = null;
			var hvoTtag = -1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					ttag = m_tagChild.CallMakeTextTagInstance(m_possTags[0]);
					if (ttag != null)
						hvoTtag = ttag.Hvo;
				});

			// Verification
			AssertTagExists(hvoTtag, "Tag should have been created.");
			VerifyTextTag(ttag, m_possTags[0], tempList[0], tempList[2]);
		}

		[Test]
		public void MakeTag_With2Wordforms()
		{
			// Provide hvoTagPoss and SelectedWordforms, create a markup tag and examine it.

			// Setup the SelectedWordforms property
			var tempList = new List<AnalysisOccurrence> {m_occurrences[0], m_occurrences[1]};
			m_tagChild.SelectedWordforms = tempList;

			// SUT
			ITextTag ttag = null;
			var hvoTtag = -1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					ttag = m_tagChild.CallMakeTextTagInstance(m_possTags[1]);
					if (ttag != null)
						hvoTtag = ttag.Hvo;
				});

			// Verification
			AssertTagExists(hvoTtag, "Tag should have been created.");
			VerifyTextTag(ttag, m_possTags[1], tempList[0], tempList[1]);
		}

		[Test]
		public void MakeTagAnnotation_With1Wordform()
		{
			// Provide hvoTagPoss and SelectedWordforms, create a markup tag and examine it.

			// Setup the SelectedWordforms property
			var tempList = new List<AnalysisOccurrence> {m_occurrences[1]};
			m_tagChild.SelectedWordforms = tempList;

			// SUT
			// Use the last possibility tag in the first list of tags
			ITextTag ttag = null;
			var hvoTtag = -1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					ttag = m_tagChild.CallMakeTextTagInstance(m_possTags[m_cposs - 1]);
					if (ttag != null)
						hvoTtag = ttag.Hvo;
				});

			// Verification
			AssertTagExists(hvoTtag, "Tag should have been created.");
			VerifyTextTag(ttag, m_possTags[m_cposs - 1], tempList[0], tempList[0]);
		}

		[Test]
		public void MakeTagAnnotation_WithNoWordform()
		{
			// Provide hvoTagPoss and SelectedWordforms, create a markup tag and examine it.
			// Setup the SelectedWordforms property
			m_tagChild.SelectedWordforms = null;

			// SUT
			ITextTag ttag = null;
			var hvoTtag = -1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					ttag = m_tagChild.CallMakeTextTagInstance(m_possTags[m_cposs - 1]);
					if (ttag != null)
						hvoTtag = ttag.Hvo;
				});

			// Verification
			AssertTagDoesntExist(hvoTtag, "MakeTextTagInstance must return null if no Wordforms are selected.");
			Assert.IsNull(ttag, "MakeTextTagInstance must return null if no Wordforms are selected.");
		}

		[Test]
		public void MakeTagAnnot_ExistTag_CompleteOverlap()
		{
			// Enhance: This test will need changing when we allow multiple lines of tagging.

			// Setup the SelectedWordforms property for first tag
			var tempList = new List<AnalysisOccurrence> {m_occurrences[1], m_occurrences[2]};
			m_tagChild.SelectedWordforms = tempList;

			var hvoTag = -1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					var initialTag = m_tagChild.CallMakeTextTagInstance(m_possTags[0]);
					if (initialTag != null)
						hvoTag = initialTag.Hvo;
				});

			// Make sure SelectedWordforms property is still set to the same Wordforms
			m_tagChild.SelectedWordforms = tempList;

			// SUT
			// Make a second tag annotation with a different possibility pointing to the same Wordforms
			ITextTag SUTTag = null;
			var hvoSUTTag = -1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					SUTTag = m_tagChild.CallMakeTextTagInstance(m_possTags[1]);
					if (SUTTag != null)
						hvoSUTTag = SUTTag.Hvo;
				});

			// Verification
			AssertTagExists(hvoSUTTag, "New tag should have been created.");
			VerifyTextTag(SUTTag, m_possTags[1], tempList[0], tempList[1]);
			// The first tag should no longer exist in the Cache
			AssertTagDoesntExist(hvoTag, "Should have deleted the initial tag.");
		}

		[Test]
		public void MakeTagAnnot_ExistTag_FrontOverlap()
		{
			// This test will need changing when we allow multiple lines of tagging.

			// Setup the SelectedWordforms property for first tag
			var tempList = new List<AnalysisOccurrence> {m_occurrences[1], m_occurrences[2]};
			m_tagChild.SelectedWordforms = tempList;

			var hvoTag = -1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					var initialTag = m_tagChild.CallMakeTextTagInstance(m_possTags[0]);
					if (initialTag != null)
						hvoTag = initialTag.Hvo;
				});

			// Setup the SelectedWordforms property for second tag
			var tempList1 = new List<AnalysisOccurrence> { m_occurrences[0], m_occurrences[1] };
			m_tagChild.SelectedWordforms = tempList1;

			// SUT
			// Make a second tag annotation with a different possibility pointing to the new Wordforms
			ITextTag tagSUT = null;
			var hvoSUTTag = -1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					tagSUT = m_tagChild.CallMakeTextTagInstance(m_possTags[1]);
					if (tagSUT != null)
						hvoSUTTag = tagSUT.Hvo;
				});

			// Verification
			// Verify new tag
			AssertTagExists(hvoSUTTag, "Should have created second tag.");
			VerifyTextTag(tagSUT, m_possTags[1], tempList1[0], tempList1[1]);
			// The old tag should no longer exist in the Cache
			AssertTagDoesntExist(hvoTag, "Should have deleted the initial tag.");
		}

		[Test]
		public void MakeTagAnnot_ExistTag_RearOverlap()
		{
			// This test will need changing when we allow multiple lines of tagging.

			// Setup the SelectedWordforms property for first tag
			var tempList = new List<AnalysisOccurrence> {m_occurrences[1], m_occurrences[2]};
			m_tagChild.SelectedWordforms = tempList;

			var hvoTag = -1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					var initialTag = m_tagChild.CallMakeTextTagInstance(m_possTags[0]);
					if (initialTag != null)
						hvoTag = initialTag.Hvo;
				});

			// Setup the SelectedWordforms property for second tag
			var tempList1 = new List<AnalysisOccurrence> { m_occurrences[2], m_occurrences[3], m_occurrences[4] };
			m_tagChild.SelectedWordforms = tempList1;

			// SUT
			// Make a second tag annotation with a different possibility pointing to the new Wordforms
			ITextTag tagSUT = null;
			var hvoSUTTag = -1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					tagSUT = m_tagChild.CallMakeTextTagInstance(m_possTags[2]);
					if (tagSUT != null)
						hvoSUTTag = tagSUT.Hvo;
				});

			// Verification
			// Verify new tag
			AssertTagExists(hvoSUTTag, "Should have created second tag.");
			VerifyTextTag(tagSUT, m_possTags[2], tempList1[0], tempList1[2]);
			// The old tag should no longer exist in the Cache
			AssertTagDoesntExist(hvoTag, "Should have deleted the initial tag.");
		}

		[Test]
		public void MakeTagAnnot_MultipleExistTags_Overlap()
		{
			// This test will need changing when we allow multiple lines of tagging.

			// Setup the SelectedWordforms property for first (existing) tag
			var tempList = new List<AnalysisOccurrence> {m_occurrences[1], m_occurrences[2]};
			m_tagChild.SelectedWordforms = tempList;

			var hvoTag1 = -1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					var oldTag1 = m_tagChild.CallMakeTextTagInstance(m_possTags[0]);
					if (oldTag1 != null)
						hvoTag1 = oldTag1.Hvo;
				});

			// Setup the SelectedWordforms property for second (existing) tag
			var tempList1 = new List<AnalysisOccurrence> { m_occurrences[3], m_occurrences[4] };
			m_tagChild.SelectedWordforms = tempList1;

			var hvoTag2 = -1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					var oldTag2 = m_tagChild.CallMakeTextTagInstance(m_possTags[1]);
					if (oldTag2 != null)
						hvoTag2 = oldTag2.Hvo;
				});

			// Setup the SelectedWordforms property for SUT tag
			// These overlap with both existing tags
			var tempList2 = new List<AnalysisOccurrence> { m_occurrences[2], m_occurrences[3] };
			m_tagChild.SelectedWordforms = tempList2;

			// SUT
			// Make a third tag annotation with a different possibility pointing to the new Wordforms
			ITextTag tagSUT = null;
			var hvoSUTTag = -1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					tagSUT = m_tagChild.CallMakeTextTagInstance(m_possTags[2]);
					if (tagSUT != null)
						hvoSUTTag = tagSUT.Hvo;
				});

			// Verification
			// Verify new tag
			AssertTagExists(hvoSUTTag, "Should have created second tag.");
			VerifyTextTag(tagSUT, m_possTags[2], tempList2[0], tempList2[1]);
			// The old tags (2) should no longer exist in the Cache
			AssertTagDoesntExist(hvoTag1, "Should have deleted the first pre-existing tag.");
			AssertTagDoesntExist(hvoTag2, "Should have deleted the second pre-existing tag.");
		}

		[Test]
		public void MakeTagAnnot_ExistTag_NoOverlap()
		{
			// Setup the SelectedWordforms property for first tag
			var tempList = new List<AnalysisOccurrence> {m_occurrences[0], m_occurrences[1]};
			m_tagChild.SelectedWordforms = tempList;

			var hvoTag = -1;
			ITextTag initialTag = null;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					initialTag = m_tagChild.CallMakeTextTagInstance(m_possTags[0]);
					if (initialTag != null)
						hvoTag = initialTag.Hvo;
				});

			// Setup the SelectedWordforms property for second tag
			var tempList1 = new List<AnalysisOccurrence> {m_occurrences[2], m_occurrences[3], m_occurrences[4]};
			m_tagChild.SelectedWordforms = tempList1;

			// SUT
			// Make a second tag annotation with a different possibility pointing to the new Wordforms
			var hvoTagSUT = -1;
			ITextTag tagSUT = null;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					tagSUT = m_tagChild.CallMakeTextTagInstance(m_possTags[2]);
					if (tagSUT != null)
						hvoTagSUT = tagSUT.Hvo;
				});

			// Verification
			// Verify new tag
			AssertTagExists(hvoTagSUT, "Should have created new tag.");
			VerifyTextTag(tagSUT, m_possTags[2], tempList1[0], tempList1[2]);

			// The old tag should still exist in the Cache
			AssertTagExists(hvoTag, "Should have left old tag in cache.");
			VerifyTextTag(initialTag, m_possTags[0], tempList[0], tempList[1]);
		}

		[Test]
		public void DeleteTagAnnot_SetOfTwo()
		{
			var tagsToDelete = new Set<ITextTag>();

			// Setup the SelectedWordforms property for first tag
			var tempList = new List<AnalysisOccurrence> {m_occurrences[0], m_occurrences[1]};
			m_tagChild.SelectedWordforms = tempList;

			var hvoTag1 = -1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					var firstTag = m_tagChild.CallMakeTextTagInstance(m_possTags[0]);
					if (firstTag != null)
					{
						hvoTag1 = firstTag.Hvo;
						tagsToDelete.Add(firstTag);
					}
				});

			// Setup the SelectedWordforms property for second tag
			tempList = new List<AnalysisOccurrence> {m_occurrences[2], m_occurrences[3], m_occurrences[4]};
			m_tagChild.SelectedWordforms = tempList;

			var hvoTag2 = -1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor, () =>
				{
					var secondTag = m_tagChild.CallMakeTextTagInstance(m_possTags[1]);
					if (secondTag != null)
					{
						hvoTag2 = secondTag.Hvo;
						tagsToDelete.Add(secondTag);
					}
				});

			// SUT
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor,
				() => m_tagChild.CallDeleteTextTags(tagsToDelete));

			// Verification
			// The two tags should no longer exist in the Cache
			AssertTagDoesntExist(hvoTag1, "Should have deleted the first tag.");
			AssertTagDoesntExist(hvoTag2, "Should have deleted the second tag.");
		}

		[Test]
		public void MakeContextMenu_CheckedStates_NoSel()
		{
			// DON'T set up a selection; the expected result for this test may change at a later time
			// SUT
			using (var menu = new ContextMenuStrip())
			{
				m_tagChild.CallMakeContextMenuForTags(menu, m_textMarkupTags);

				// Verification of SUT; None should be checked
				var subMenu = AssertHasMenuWithText(menu.Items, "Syntax", 3);
				Assert.IsNotNull(subMenu, "No Syntax menu!?");
				foreach (ToolStripItem item in subMenu.DropDownItems)
					VerifyMenuItemCheckStatus(item, false);
			}
		}

		[Test]
		public void MakeContextMenu_CheckedStates_SelMatchesTag()
		{
			// Provide hvoTagPoss and SelectedWordforms, create a markup tag and examine it.
			// Setup the SelectedWordforms property
			var tempList = new List<AnalysisOccurrence> { m_occurrences[0], m_occurrences[1] };
			m_tagChild.SelectedWordforms = tempList;

			const int itestItem = 1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor,
				() => m_tagChild.CallMakeTextTagInstance(m_possTags[itestItem]));

			// SUT
			using (var menu = new ContextMenuStrip())
			{
				m_tagChild.CallMakeContextMenuForTags(menu, m_textMarkupTags);

				// Verification of SUT; Only one should be checked and that in a submenu
				var subList = AssertHasMenuWithText(menu.Items, kFTO_RRG_Semantics, 3);
				Assert.IsNotNull(subList, "No " + kFTO_RRG_Semantics + " menu!?");
				var subMenu = subList.DropDownItems;
				var expectedStates = new bool[subMenu.Count];
				expectedStates[itestItem] = true;
				AssertMenuCheckState(expectedStates, subMenu);
			}
		}

		[Test]
		public void MakeContextMenu_CheckedStates_SelFirstOfTag()
		{
			// Provide hvoTagPoss and SelectedWordforms, create a markup tag and examine it.

			// Setup the SelectedWordforms property
			var tempList = new List<AnalysisOccurrence> { m_occurrences[0], m_occurrences[1], m_occurrences[2] };
			m_tagChild.SelectedWordforms = tempList;

			// Make a tag for selection
			const int itestItem = 0; // first tag in list
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor,
				() => m_tagChild.CallMakeTextTagInstance(m_possTags[itestItem]));

			// Reduce selection to first word
			tempList.Remove(m_occurrences[1]);
			tempList.Remove(m_occurrences[2]);
			m_tagChild.SelectedWordforms = tempList;

			// SUT
			using (var menu = new ContextMenuStrip())
			{
				m_tagChild.CallMakeContextMenuForTags(menu, m_textMarkupTags);

				// Verification of SUT; Only one should be checked and that in a submenu
				var subList = AssertHasMenuWithText(menu.Items, kFTO_RRG_Semantics, 3);
				Assert.IsNotNull(subList, "No " + kFTO_RRG_Semantics + " menu!?");
				var subMenu = subList.DropDownItems;
				var expectedStates = new bool[subMenu.Count];
				expectedStates[itestItem] = true;
				AssertMenuCheckState(expectedStates, subMenu);
			}
		}

		/// <summary>
		/// Tests which items are checked on the context menu when two tags exist for one
		/// wordform and the current selection is on the last wordform in both tags.
		/// </summary>
		[Test]
		public void MakeContextMenu_CheckedStates_MultiAnnSelLast()
		{
			// This test will need changing when we allow multiple lines of tagging.

			// Provide hvoTagPoss and SelectedWordforms, create a markup tag and examine it.
			// Setup the SelectedWordforms property to include 3 words
			var tempList = new List<AnalysisOccurrence> { m_occurrences[0], m_occurrences[1], m_occurrences[2] };
			m_tagChild.SelectedWordforms = tempList;

			// Make a tag for 1st selection
			const int itestItem1 = 0; // first tag in list
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor,
				() => m_tagChild.CallMakeTextTagInstance(m_possTags[itestItem1]));

			// Setup the SelectedWordforms property to include 2nd & 3rd words
			tempList.Remove(m_occurrences[0]);
			m_tagChild.SelectedWordforms = tempList;

			// Make a tag for 2nd selection
			// Enhance GordonM: For the time being, this deletes the first tag. When multiple layers
			// of tags are allowed, change the expected results.
			const int itestItem2 = 1; // 2nd tag in list
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor,
				() => m_tagChild.CallMakeTextTagInstance(m_possTags[itestItem2]));

			// Reduce selection to 3rd word
			tempList.Remove(m_occurrences[1]);
			m_tagChild.SelectedWordforms = tempList;

			// SUT; make menu while selected word has 2 tags
			using (var menu = new ContextMenuStrip())
			{
				m_tagChild.CallMakeContextMenuForTags(menu, m_textMarkupTags);

				// Verification of SUT; Two should be checked and both in a submenu
				var subList = AssertHasMenuWithText(menu.Items, kFTO_RRG_Semantics, 3);
				Assert.IsNotNull(subList, "No " + kFTO_RRG_Semantics + " menu!?");
				var subMenu = subList.DropDownItems;
				var expectedStates = new bool[subMenu.Count];
				expectedStates[itestItem1] = false; // Enhance GordonM: This will change to 'true' when multiple layers are okay.
				expectedStates[itestItem2] = true;
				AssertMenuCheckState(expectedStates, subMenu);
			}
		}

		/// <summary>
		/// Tests which items are checked on the context menu when two tags exist for one
		/// wordform and the current selection overlaps those tags (and non-tagged occurrences).
		/// </summary>
		[Test]
		public void MakeContextMenu_CheckedStates_MultiAnnSelExtra()
		{
			// This test will need changing when we allow multiple lines of tagging.

			// Provide hvoTagPoss and SelectedWordforms, create a markup tag and examine it.
			// Setup the SelectedWordforms property to include 2 words
			var tempList = new List<AnalysisOccurrence> { m_occurrences[1], m_occurrences[2] };
			m_tagChild.SelectedWordforms = tempList;

			// Make a tag for 1st selection
			const int itestItem1 = 0;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor,
				() => m_tagChild.CallMakeTextTagInstance(m_possTags[itestItem1]));

			// Setup the SelectedWordforms property to include one more word
			tempList.Add(m_occurrences[3]);
			m_tagChild.SelectedWordforms = tempList;

			// Make a tag for 2nd selection
			// Enhance GordonM: For the time being, this deletes the first tag. When multiple layers
			// of tags are allowed, change the expected results.
			const int itestItem2 = 1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor,
				() => m_tagChild.CallMakeTextTagInstance(m_possTags[itestItem2]));

			// Setup selection to contain previous tags, plus another word
			tempList.Insert(0, m_occurrences[0]);
			m_tagChild.SelectedWordforms = tempList;

			// SUT; make menu while selected phrase has 2 tags, but the initial
			// occurrence has no tags.
			using (var menu = new ContextMenuStrip())
			{
				m_tagChild.CallMakeContextMenuForTags(menu, m_textMarkupTags);

				// Verification of SUT; Two should be checked and that in a submenu
				var subList = AssertHasMenuWithText(menu.Items, kFTO_RRG_Semantics, 3);
				Assert.IsNotNull(subList, "No " + kFTO_RRG_Semantics + " menu!?");
				var subMenu = subList.DropDownItems;
				var expectedStates = new bool[subMenu.Count];
				expectedStates[itestItem1] = false;
				// Enhance GordonM: When multiple layers allowed, change this.
				expectedStates[itestItem2] = true;
				AssertMenuCheckState(expectedStates, subMenu);
			}
		}

		/// <summary>
		/// Tests which items are checked on the context menu when two side-by-side tags
		/// are covered (at least partially) by the current selection.
		/// </summary>
		[Test]
		public void MakeContextMenu_CheckedStates_SelCoversMultiSideBySideAnn()
		{
			// Provide hvoTagPoss and SelectedWordforms, create a markup tag and examine it.
			// Setup the SelectedWordforms property to include 2 words
			var tempList = new List<AnalysisOccurrence> { m_occurrences[0], m_occurrences[1] };
			m_tagChild.SelectedWordforms = tempList;

			// Make a tag for 1st selection
			const int itestItem1 = 0;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor,
				() => m_tagChild.CallMakeTextTagInstance(m_possTags[itestItem1]));

			// Setup the SelectedWordforms property to include one more word
			tempList.Clear();
			tempList.Add(m_occurrences[2]);
			tempList.Add(m_occurrences[3]);
			m_tagChild.SelectedWordforms = tempList;

			// Make a tag for 2nd selection
			const int itestItem2 = 1;
			UndoableUnitOfWorkHelper.Do("UndoTagTest", "RedoTagTest", Cache.ActionHandlerAccessor,
				() => m_tagChild.CallMakeTextTagInstance(m_possTags[itestItem2]));

			// Setup selection to cover part of first tag, and all of second
			tempList.Insert(0, m_occurrences[1]);
			m_tagChild.SelectedWordforms = tempList;

			// SUT; make menu while selected phrase has 2 side-by-side tags
			using (var menu = new ContextMenuStrip())
			{
				m_tagChild.CallMakeContextMenuForTags(menu, m_textMarkupTags);

				// Verification of SUT; Two should be checked and that in a submenu
				var subList = AssertHasMenuWithText(menu.Items, kFTO_RRG_Semantics, 3);
				Assert.IsNotNull(subList, "No " + kFTO_RRG_Semantics + " menu!?");
				var subMenu = subList.DropDownItems;
				var expectedStates = new bool[subMenu.Count];
				expectedStates[itestItem1] = true;
				expectedStates[itestItem2] = true;
				AssertMenuCheckState(expectedStates, subMenu);
			}
		}
	}
}
