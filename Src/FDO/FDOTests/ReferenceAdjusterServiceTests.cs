// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ReferenceAdjusterServiceTests.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Contains tests for the ReferenceAdjusterService class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ReferenceAdjusterServiceTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IStTxtPara m_txtPara;
		private IStText m_stTxt;
		private IText m_txt;
		private IFdoServiceLocator m_servLoc;
		private IReferenceAdjuster m_raService;
		private ITextTagFactory m_tagFact;
		private AnalysisOccurrence[] m_occurrences;
		private ICmPossibilityList m_possTagList;

		#region Test Setup methods

		/// <summary>
		/// Create test data for ReferenceAdjusterService tests.
		/// </summary>
		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_servLoc = Cache.ServiceLocator;
			m_raService = m_servLoc.GetInstance<IReferenceAdjuster>();
			m_tagFact = m_servLoc.GetInstance<ITextTagFactory>();

			CreateTestText();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a text for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateTestText()
		{
			m_txt = m_servLoc.GetInstance<ITextFactory>().Create();
			Cache.LangProject.TextsOC.Add(m_txt);
			m_possTagList = Cache.LangProject.GetDefaultTextTagList();
			m_stTxt = m_servLoc.GetInstance<IStTextFactory>().Create();
			m_txt.ContentsOA = m_stTxt;
			m_txtPara = m_txt.ContentsOA.AddNewTextPara(null);

			// 0    1  2 3    4      5   6                        7    8
			// This is a test string for ReferenceAdjusterService tests.

			var hvoVernWs = Cache.DefaultUserWs;
			m_txtPara.Contents = TsStringUtils.MakeTss("This is a test string for ReferenceAdjusterService tests.", hvoVernWs);
			ParseText();
		}

		private void ParseText()
		{
			using (var pp = new ParagraphParser(Cache))
			{
				pp.Parse(m_txtPara);
			}
			var seg = m_txtPara.SegmentsOS[0];
			var wordArray = seg.AnalysesRS.ToArray();
			var cwords = wordArray.Length;
			m_occurrences = new AnalysisOccurrence[cwords];
			for (var i = 0; i < cwords; i++)
				m_occurrences[i] = new AnalysisOccurrence(seg, i);
		}

		#endregion

		#region Tests

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method GrowFromEnd() by creating a tag and growing it.
		/// Only stops at Wordforms. Should succeed.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GrowFromEnd_Wordform()
		{
			var begPoint = m_occurrences[0];
			var endPoint = m_occurrences[2];
			var testTag = m_tagFact.CreateOnText(begPoint, endPoint, m_possTagList.PossibilitiesOS[0]);

			// SUT
			var result = m_raService.GrowFromEnd(true, testTag);

			// Verification
			Assert.AreEqual(m_occurrences[3], testTag.EndRef());
			Assert.IsTrue(testTag.IsValidRef);
			Assert.IsTrue(result, "GrowFromEnd failed.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method GrowFromEnd() by creating a tag and growing it.
		/// Stops at any occurrence. Should succeed.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GrowFromEnd_Occurrence()
		{
			var begPoint = m_occurrences[6];
			var endPoint = m_occurrences[7];
			var testTag = m_tagFact.CreateOnText(begPoint, endPoint, m_possTagList.PossibilitiesOS[1]);

			// SUT
			var result = m_raService.GrowFromEnd(false, testTag);

			// Verification
			Assert.AreEqual(m_occurrences[8], testTag.EndRef());
			Assert.IsFalse(testTag.EndRef().HasWordform, "This should be punctuation!");
			Assert.IsTrue(result, "GrowFromEnd failed.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method GrowFromEnd() by creating a tag and growing it.
		/// Only stops at Wordforms. Should fail.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GrowFromEnd_WordformImpossible()
		{
			var begPoint = m_occurrences[6];
			var endPoint = m_occurrences[7];
			var testTag = m_tagFact.CreateOnText(begPoint, endPoint, m_possTagList.PossibilitiesOS[0]);

			// SUT
			var result = m_raService.GrowFromEnd(true, testTag);

			// Verification
			Assert.AreEqual(m_occurrences[7], testTag.EndRef());
			Assert.IsTrue(testTag.EndRef().HasWordform, "This should still be the same word.");
			Assert.IsFalse(result, "GrowFromEnd should have failed.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method GrowFromBeginning() by creating a tag and growing it.
		/// Only stops at Wordforms. Should succeed.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GrowFromBeginning_Wordform()
		{
			var begPoint = m_occurrences[1];
			var endPoint = m_occurrences[3];
			var testTag = m_tagFact.CreateOnText(begPoint, endPoint, m_possTagList.PossibilitiesOS[0]);

			// SUT
			var result = m_raService.GrowFromBeginning(true, testTag);

			// Verification
			Assert.AreEqual(m_occurrences[0], testTag.BegRef(), "BegRef should move back one.");
			Assert.AreEqual(m_occurrences[3], testTag.EndRef(), "EndRef shouldn't change.");
			Assert.IsTrue(testTag.IsValidRef);
			Assert.IsTrue(result, "GrowFromBeginning failed.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method GrowFromBeginning() by creating a tag and growing it.
		/// Stops at any occurrence and in this case the occurrence is a wordform. Should succeed.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GrowFromBeginning_Occurrence()
		{
			var begPoint = m_occurrences[6];
			var endPoint = m_occurrences[7];
			var testTag = m_tagFact.CreateOnText(begPoint, endPoint, m_possTagList.PossibilitiesOS[1]);

			// SUT
			var result = m_raService.GrowFromBeginning(false, testTag);

			// Verification
			Assert.AreEqual(m_occurrences[5], testTag.BegRef());
			Assert.IsTrue(testTag.BegRef().HasWordform, "This should not be punctuation!");
			Assert.IsTrue(result, "GrowFromBeginning failed.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method GrowFromBeginning() by creating a tag and growing it.
		/// Stops at any occurrence. Should fail.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GrowFromBeginning_OccurrenceImpossible()
		{
			var begPoint = m_occurrences[0]; // beginning reference at beginning of text
			var endPoint = m_occurrences[7];
			var testTag = m_tagFact.CreateOnText(begPoint, endPoint, m_possTagList.PossibilitiesOS[0]);

			// SUT
			var result = m_raService.GrowFromBeginning(false, testTag);

			// Verification
			Assert.AreEqual(m_occurrences[7], testTag.EndRef());
			Assert.AreEqual(m_occurrences[0], testTag.BegRef()); // no change in reference
			Assert.IsFalse(result, "GrowFromBeginning should have failed.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ShrinkFromEnd() by creating a tag and shrinking it.
		/// Only stops at Wordforms. Should succeed.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ShrinkFromEnd_Wordform()
		{
			var begPoint = m_occurrences[0];
			var endPoint = m_occurrences[4];
			var testTag = m_tagFact.CreateOnText(begPoint, endPoint, m_possTagList.PossibilitiesOS[0]);

			// SUT
			var result = m_raService.ShrinkFromEnd(true, testTag);

			// Verification
			Assert.AreEqual(m_occurrences[3], testTag.EndRef());
			Assert.IsTrue(testTag.IsValidRef);
			Assert.IsTrue(result, "ShrinkFromEnd failed.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ShrinkFromEnd() by creating a tag and shrinking it.
		/// Stops at any occurrence. Should succeed.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ShrinkFromEnd_Occurrence()
		{
			var begPoint = m_occurrences[6];
			var endPoint = m_occurrences[7];
			var testTag = m_tagFact.CreateOnText(begPoint, endPoint, m_possTagList.PossibilitiesOS[1]);

			// SUT
			var result = m_raService.ShrinkFromEnd(false, testTag);

			// Verification
			Assert.AreEqual(m_occurrences[6], testTag.EndRef());
			Assert.AreEqual(m_occurrences[6], testTag.BegRef()); // a one word reference
			Assert.IsTrue(testTag.EndRef().HasWordform, "This should not be punctuation!");
			Assert.IsTrue(result, "ShrinkFromEnd failed.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ShrinkFromEnd() by creating a tag and shrinking it.
		/// Only stops at Wordforms. Should fail.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ShrinkFromEnd_WordformImpossible()
		{
			var begPoint = m_occurrences[6];
			var endPoint = m_occurrences[6]; // start with one word; now try to shrink
			var testTag = m_tagFact.CreateOnText(begPoint, endPoint, m_possTagList.PossibilitiesOS[0]);

			// SUT
			var result = m_raService.ShrinkFromEnd(true, testTag);

			// Verification
			Assert.AreEqual(m_occurrences[6], testTag.EndRef(), "Should be no change in reference.");
			Assert.AreEqual(m_occurrences[6], testTag.BegRef(), "Should be no change in reference.");
			Assert.IsFalse(result, "ShrinkFromEnd should have failed.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ShrinkFromBeginning() by creating a tag and shrinking it.
		/// Only stops at Wordforms. Should succeed.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ShrinkFromBeginning_Wordform()
		{
			var begPoint = m_occurrences[1];
			var endPoint = m_occurrences[3];
			var testTag = m_tagFact.CreateOnText(begPoint, endPoint, m_possTagList.PossibilitiesOS[0]);

			// SUT
			var result = m_raService.ShrinkFromBeginning(true, testTag);

			// Verification
			Assert.AreEqual(m_occurrences[2], testTag.BegRef(), "BegRef should move forward one.");
			Assert.AreEqual(m_occurrences[3], testTag.EndRef(), "EndRef shouldn't change.");
			Assert.IsTrue(testTag.IsValidRef);
			Assert.IsTrue(result, "ShrinkFromBeginning failed.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ShrinkFromBeginning() by creating a tag and shrinking it.
		/// Stops at any occurrence and in this case the occurrence is a wordform. Should succeed.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ShrinkFromBeginning_Occurrence()
		{
			var begPoint = m_occurrences[7];
			var endPoint = m_occurrences[8]; // two words; now try to shrink to one
			var testTag = m_tagFact.CreateOnText(begPoint, endPoint, m_possTagList.PossibilitiesOS[1]);

			// SUT
			var result = m_raService.ShrinkFromBeginning(false, testTag);

			// Verification
			Assert.AreEqual(m_occurrences[8], testTag.BegRef(), "Should have shrunk to one word.");
			Assert.IsFalse(testTag.BegRef().HasWordform, "This should be punctuation!");
			Assert.IsFalse(testTag.EndRef().HasWordform, "This should be punctuation!");
			Assert.IsTrue(result, "ShrinkFromBeginning failed.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method ShrinkFromBeginning() by creating a tag and shrinking it.
		/// Stops at any occurrence. Should fail.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ShrinkFromBeginning_OccurrenceImpossible()
		{
			var begPoint = m_occurrences[8]; // reference at end of text
			var endPoint = m_occurrences[8];
			var testTag = m_tagFact.CreateOnText(begPoint, endPoint, m_possTagList.PossibilitiesOS[0]);

			// SUT
			var result = m_raService.ShrinkFromBeginning(false, testTag);

			// Verification
			Assert.AreEqual(m_occurrences[8], testTag.BegRef(), "Should be no change in reference.");
			Assert.AreEqual(m_occurrences[8], testTag.EndRef(), "Should be no change in reference.");
			Assert.IsFalse(result, "ShrinkFromBeginning should have failed.");
		}

		#endregion

	}
}
