// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: VirtualOrderingServicesTests.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class VirtualOrderingServicesTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Data Members

		private IFdoServiceLocator m_servLoc;
		private IVirtualOrderingRepository m_voRepo;
		private ICmPossibilityFactory m_possFact;
		private const int possibilitiesFlid = 8008;
		private const string possName = "Possibilities";
		private ICmPossibilityList m_testList;

		#endregion

		#region Test Setup Methods

		/// <summary>
		/// set up member variables and test data.
		/// </summary>
		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_servLoc = Cache.ServiceLocator;
			m_servLoc.GetInstance<IVirtualOrderingFactory>();
			m_voRepo = m_servLoc.GetInstance<IVirtualOrderingRepository>();
			m_possFact = m_servLoc.GetInstance<ICmPossibilityFactory>();
			CreateKnownPossibilityList();
		}

		private void CreateKnownPossibilityList()
		{
			// this list exists empty in the FdoTestBase.
			m_testList = Cache.LangProject.ConfidenceLevelsOA;
			Assert.AreEqual(0, m_testList.PossibilitiesOS.Count);
			Assert.AreEqual("Possibilities", Cache.MetaDataCache.GetFieldName(possibilitiesFlid));

			AppendTestItemToList("First"); // So tests automatically have one item.
		}

		#endregion

		#region Utility Methods

		private void CreateFakeGenreList()
		{
			// this list is null in the FdoTestBase.
			Cache.LangProject.GenreListOA = m_servLoc.GetInstance<ICmPossibilityListFactory>().Create();
			Assert.AreEqual(0, Cache.LangProject.GenreListOA.PossibilitiesOS.Count);

			AppendGenreItemToFakeGenreList("First");
			AppendGenreItemToFakeGenreList("Second");
			AppendGenreItemToFakeGenreList("Third");
			AppendGenreItemToFakeGenreList("Fourth");
		}

		private void AppendGenreItemToFakeGenreList(string name)
		{
			var fakeList = Cache.LangProject.GenreListOA;
			var newItem = m_possFact.Create(Guid.NewGuid(), fakeList);
			newItem.Name.SetAnalysisDefaultWritingSystem(name);
		}

		private IText CreateTestText()
		{
			var text = m_servLoc.GetInstance<ITextFactory>().Create();
			Cache.LangProject.TextsOC.Add(text);
			var stText = m_servLoc.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = stText;
			return text;
		}

		private void AppendTestItemToList(string name)
		{
			var newItem = m_possFact.Create(Guid.NewGuid(), m_testList);
			newItem.Name.SetAnalysisDefaultWritingSystem(name);
		}

		private IVirtualOrdering CreateTestVO(IEnumerable<ICmObject> desiredSeq)
		{
			VirtualOrderingServices.SetVO(m_testList, possibilitiesFlid, desiredSeq);
			var result = m_voRepo.AllInstances().Where(vo => vo.SourceRA == m_testList
															 && vo.Field == possName);
			return result.FirstOrDefault();
		}

		#endregion

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the VO Creation method (SetVO).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CreateVO()
		{
			// Setup
			AppendTestItemToList("Second");
			// For this test, just keep the same order.
			var desiredSeq = m_testList.PossibilitiesOS.Cast<ICmObject>();

			// SUT
			var myvo = CreateTestVO(desiredSeq);

			// Verify
			Assert.AreEqual(1, m_voRepo.Count, "There ought to be one VO object.");
			Assert.AreEqual(m_testList.Hvo, myvo.SourceRA.Hvo, "Got wrong Source object!");
			Assert.AreEqual(possName, myvo.Field, "VO is on the wrong field!");
			var origList = m_testList.PossibilitiesOS.ToHvoArray();
			var voList = myvo.ItemsRS.ToHvoArray();
			Assert.AreEqual(origList, voList, "Hvo lists differ.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the VO Deletion method (ResetVO).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ResetVO()
		{
			// Setup
			AppendTestItemToList("Second");
			// For this test, just keep the same order.
			var desiredSeq = m_testList.PossibilitiesOS.Cast<ICmObject>();
			var myvo = CreateTestVO(desiredSeq);
			// Make sure test setup worked
			Assert.AreEqual(1, m_voRepo.Count, "There ought to be one VO object.");

			// SUT
			VirtualOrderingServices.ResetVO(m_testList, possibilitiesFlid);

			// Verify
			Assert.AreEqual(0, m_voRepo.Count, "Test should have deleted the only VO object.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the 'other' VO Deletion method (SetVO with null sequence).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetVO_nullSequence()
		{
			// Setup
			AppendTestItemToList("Second");
			// For this test, just keep the same order.
			var desiredSeq = m_testList.PossibilitiesOS.Cast<ICmObject>();
			var myvo = CreateTestVO(desiredSeq);
			// Make sure test setup worked
			Assert.AreEqual(1, m_voRepo.Count, "There ought to be one VO object.");

			// SUT
			VirtualOrderingServices.SetVO(m_testList, possibilitiesFlid, null);

			// Verify
			Assert.AreEqual(0, m_voRepo.Count, "Test should have deleted the only VO object.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting an existing VO to a different sequence.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetExistingVO_diffSequence()
		{
			// Setup
			AppendTestItemToList("Second");
			AppendTestItemToList("Third");
			var initialSeq = m_testList.PossibilitiesOS.Cast<ICmObject>();
			CreateTestVO(initialSeq);
			// Make sure test setup worked
			Assert.AreEqual(1, m_voRepo.Count, "There ought to be one VO object.");
			// Make a sequence in a different order
			var newSeq = m_testList.PossibilitiesOS.Reverse().Cast<ICmObject>();

			// SUT
			VirtualOrderingServices.SetVO(m_testList, possibilitiesFlid, newSeq);

			// Verify
			Assert.AreEqual(1, m_voRepo.Count, "There ought to still be one VO object.");
			var myvo = m_voRepo.AllInstances().First();
			Assert.AreEqual(m_testList.Hvo, myvo.SourceRA.Hvo, "Got wrong Source object!");
			Assert.AreEqual(possName, myvo.Field, "VO is on the wrong field!");
			var voList = myvo.ItemsRS;
			Assert.AreEqual(newSeq, voList, "Hvo lists differ.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests trying to get a non-existent VO. Should pass sequence through untouched.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetOrderedValue_nonexistentVO()
		{
			// Setup
			AppendTestItemToList("Second");
			var initialSeq = m_testList.PossibilitiesOS.Cast<ICmObject>();
			// Make sure test setup worked
			Assert.AreEqual(0, m_voRepo.Count, "There shouldn't be an existing VO object.");
			// Make a sequence in a different order
			var newSeq = m_testList.PossibilitiesOS.Reverse().Cast<ICmObject>();

			// SUT
			var resultSeq = VirtualOrderingServices.GetOrderedValue(m_testList, possibilitiesFlid, newSeq);

			// Verify
			Assert.AreEqual(0, m_voRepo.Count, "There ought to still not be a VO object.");
			Assert.AreEqual(newSeq, resultSeq, "Hvo lists differ.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting an existing VO; complete overlap of elements in same sequence.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetOrderedValue_completeOverlap_sameSequence()
		{
			// Setup
			AppendTestItemToList("Second");
			AppendTestItemToList("Third");
			var initialSeq = m_testList.PossibilitiesOS.Cast<ICmObject>();
			CreateTestVO(initialSeq);
			// Make sure test setup worked
			Assert.AreEqual(1, m_voRepo.Count, "There ought to be one VO object.");

			// SUT
			var resultSeq = VirtualOrderingServices.GetOrderedValue(m_testList, possibilitiesFlid, initialSeq);

			// Verify
			Assert.AreEqual(1, m_voRepo.Count, "There ought to still be one VO object.");
			var myvo = m_voRepo.AllInstances().First();
			Assert.AreEqual(m_testList.Hvo, myvo.SourceRA.Hvo, "Got wrong Source object!");
			Assert.AreEqual(possName, myvo.Field, "VO is on the wrong field!");
			var voList = myvo.ItemsRS;
			Assert.AreEqual(initialSeq, voList, "Hvo lists differ.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting an existing VO; extra VO elements in same sequence.
		/// Extra VO elements should be removed from the resulting sequence.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetOrderedValue_extraVoElements_sameSequence()
		{
			// Setup
			AppendTestItemToList("Second");
			AppendTestItemToList("Third");
			var initialSeq = m_testList.PossibilitiesOS.Cast<ICmObject>();
			var myvo = CreateTestVO(initialSeq);
			// Make sure test setup worked
			Assert.AreEqual(1, m_voRepo.Count, "There ought to be one VO object.");
			Assert.AreEqual(3, myvo.ItemsRS.Count, "Wrong number of items in VO.");
			// Delete an element from the 'testing' sequence.
			var newList = m_testList.PossibilitiesOS.ToList();
			newList.RemoveAt(0);
			var newSeq = newList.Cast<ICmObject>();

			// SUT
			var resultSeq = VirtualOrderingServices.GetOrderedValue(m_testList, possibilitiesFlid, newSeq);

			// Verify
			Assert.AreEqual(1, m_voRepo.Count, "There ought to still be one VO object.");
			Assert.AreEqual(2, resultSeq.Count(), "Wrong number of items in result.");
			Assert.AreEqual(newSeq, resultSeq, "Hvo lists differ.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting an existing VO; extra native elements in same sequence.
		/// The extra elements should get passed through at the end of the sequence.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetOrderedValue_extraNativeElements_sameSequence()
		{
			// Setup
			AppendTestItemToList("Second");
			var initialSeq = m_testList.PossibilitiesOS.Cast<ICmObject>();
			CreateTestVO(initialSeq);
			// Make sure test setup worked
			Assert.AreEqual(1, m_voRepo.Count, "There ought to be one VO object.");
			// Add to the 'testing' sequence
			AppendTestItemToList("Third");
			var newSeq = m_testList.PossibilitiesOS.Cast<ICmObject>();

			// SUT
			var resultSeq = VirtualOrderingServices.GetOrderedValue(m_testList, possibilitiesFlid, newSeq);

			// Verify
			Assert.AreEqual(1, m_voRepo.Count, "There ought to still be one VO object.");
			Assert.AreEqual(3, resultSeq.Count(), "Wrong number of items in result.");
			Assert.AreEqual(newSeq, resultSeq, "Hvo lists differ.");
			Assert.AreEqual("Third", ((ICmPossibility)resultSeq.Last()).Name.AnalysisDefaultWritingSystem.Text,
				"Wrong element at end of result sequence.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting an existing VO; complete overlap of elements in different sequence.
		/// The VO should return its sequence, not the one fed in (but with the same elements).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetOrderedValue_completeOverlap_diffSequence()
		{
			// Setup
			AppendTestItemToList("Second");
			AppendTestItemToList("Third");
			var initialSeq = m_testList.PossibilitiesOS.Cast<ICmObject>();
			CreateTestVO(initialSeq);
			// Make sure test setup worked
			Assert.AreEqual(1, m_voRepo.Count, "There ought to be one VO object.");
			// Make a sequence in a different order
			var newSeq = m_testList.PossibilitiesOS.Reverse().Cast<ICmObject>();

			// SUT
			var resultSeq = VirtualOrderingServices.GetOrderedValue(m_testList, possibilitiesFlid, newSeq);

			// Verify
			Assert.AreEqual(1, m_voRepo.Count, "There ought to still be one VO object.");
			Assert.AreEqual(3, resultSeq.Count(), "Wrong number of items in result.");
			Assert.AreEqual(initialSeq, resultSeq, "Hvo lists differ.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting an existing VO; extra VO elements in different sequence. The extra
		/// elements should be ignored.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetOrderedValue_extraVoElements_diffSequence()
		{
			// Setup
			AppendTestItemToList("Second");
			AppendTestItemToList("Third");
			var initialSeq = m_testList.PossibilitiesOS.Cast<ICmObject>();
			CreateTestVO(initialSeq);
			// Make sure test setup worked
			Assert.AreEqual(1, m_voRepo.Count, "There ought to be one VO object.");
			// Make a sequence in a different order
			var newList = m_testList.PossibilitiesOS.Reverse().ToList();
			newList.RemoveAt(0);
			var newSeq = newList.Cast<ICmObject>();


			// SUT
			var resultSeq = VirtualOrderingServices.GetOrderedValue(m_testList, possibilitiesFlid, newSeq);

			// Verify
			Assert.AreEqual(1, m_voRepo.Count, "There ought to still be one VO object.");
			var resultList = new List<ICmObject>(resultSeq); // makes it easier to verify results
			Assert.AreEqual(2, resultList.Count);
			Assert.AreEqual("First", ((ICmPossibility)resultList[0]).Name.AnalysisDefaultWritingSystem.Text,
				"Wrong element at beginning of result sequence.");
			Assert.AreEqual("Second", ((ICmPossibility)resultList[1]).Name.AnalysisDefaultWritingSystem.Text,
				"Wrong element at end of result sequence.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting an existing VO; extra native elements in different sequence. The extra
		/// elements should be appended to the end of the VO sequence.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetOrderedValue_extraNativeElements_diffSequence()
		{
			// Setup
			AppendTestItemToList("Second");
			var initialSeq = m_testList.PossibilitiesOS.Cast<ICmObject>();
			CreateTestVO(initialSeq);
			// Make sure test setup worked
			Assert.AreEqual(1, m_voRepo.Count, "There ought to be one VO object.");
			// Make a sequence in a different order with an extra element
			AppendTestItemToList("Third");
			var newSeq = m_testList.PossibilitiesOS.Reverse().Cast<ICmObject>().ToList();

			// SUT
			var resultSeq = VirtualOrderingServices.GetOrderedValue(m_testList, possibilitiesFlid, newSeq);

			// Verify
			Assert.AreEqual(1, m_voRepo.Count, "There ought to still be one VO object.");
			newSeq.Reverse(); // the resulting sequence should be the reverse of what was fed in.
			Assert.AreEqual(newSeq, resultSeq, "Hvo lists differ.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting an existing VO; extra native elements AND extra VO elements in
		/// different sequence.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetOrderedValue_extraNativeAndExtraVoElements()
		{
			// Setup
			AppendTestItemToList("Second");
			AppendTestItemToList("Third");
			var initialSeq = m_testList.PossibilitiesOS.Cast<ICmObject>();
			CreateTestVO(initialSeq);
			// Make sure test setup worked
			Assert.AreEqual(1, m_voRepo.Count, "There ought to be one VO object.");
			// Make a sequence in a different order
			AppendTestItemToList("Fourth");
			var newList = m_testList.PossibilitiesOS.Reverse().ToList();
			// remove 'First' (which is now at the end)
			newList.RemoveAt(newList.Count - 1);
			var newSeq = newList.Cast<ICmObject>();

			// SUT
			var resultSeq = VirtualOrderingServices.GetOrderedValue(m_testList, possibilitiesFlid, newSeq);

			// Verify
			Assert.AreEqual(1, m_voRepo.Count, "There ought to still be one VO object.");
			// result should be (Second, Third, Fourth) (or reverse of newSeq)
			var actualList = newSeq.Reverse().ToList();
			Assert.AreEqual(actualList, resultSeq, "Hvo lists differ.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting a VO and getting an existing VO, but on a real virtual property.
		/// (StText.GenreCategories)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void VirtualPropTest()
		{
			// Setup
			CreateFakeGenreList(); // creates fake list of 4 genres on LangProj
			var testText = CreateTestText(); // creates IText on LangProj with its IStText.
			var testStText = testText.ContentsOA;
			// Get LP Fake Genre list
			var entireGenreList = Cache.LangProject.GenreListOA.PossibilitiesOS.ToList();
			testText.GenresRC.Add(entireGenreList[1]); // Second
			testText.GenresRC.Add(entireGenreList[2]); // Third
			var initialSeq = testText.GenresRC.ToList();

			// Verify that setup affects our chosen virtual property
			Assert.AreEqual(2, testStText.GenreCategories.Count,
				"Wrong number of items on virtual property.");
			var mdc = Cache.MetaDataCacheAccessor;
			int virtFlid = mdc.GetFieldId2(testStText.ClassID, "GenreCategories", true);

			// SUT1
			VirtualOrderingServices.SetVO(testStText, virtFlid, initialSeq.Cast<ICmObject>());
			// Make sure SUT1 worked
			Assert.AreEqual(1, m_voRepo.Count, "There ought to be one VO object.");

			// Setup for SUT2
			// Make a sequence in a different order and with an extra item, but missing an original.
			var newList = new List<ICmObject>(entireGenreList.Cast<ICmObject>());
			newList.Reverse(); // now has (Fourth, Third, Second, First)
			// remove 'Second' (which is now at index=2)
			newList.RemoveAt(2); // now has (Fourth, Third, First)

			// SUT2
			var resultSeq = VirtualOrderingServices.GetOrderedValue(testStText, virtFlid, newList);

			// Verify
			Assert.AreEqual(1, m_voRepo.Count, "There ought to still be one VO object.");
			// result should be (Third, Fourth, First)
			var actualList = new List<ICmPossibility> {entireGenreList[2], entireGenreList[3], entireGenreList[0]};
			var actualAsObj = actualList.Cast<ICmObject>();
			Assert.AreEqual(actualAsObj, resultSeq, "Hvo lists differ.");
		}

		/// <summary>
		/// This property supports virtual ordering
		/// </summary>
		[Test]
		public void VisibleComplexFormBackRefs()
		{
			// Verify that if we make lex entries blackboard and blackbird, blackboard correctly comes first.
			var black = (LexEntry)MakeEntry("black", "nonreflecting");
			var blackbird = (LexEntry)MakeEntry("blackbird", "dark avian");
			var blackboard = (LexEntry)MakeEntry("blackboard", "something to write on");
			var lerBlackbird = MakeComplexEntryRef(blackbird);
			lerBlackbird.ComponentLexemesRS.Add(black);
			lerBlackbird.ShowComplexFormsInRS.Add(black);
			Assert.That(black.VisibleComplexFormBackRefs.First(), Is.EqualTo(lerBlackbird));

			var lerBlackboard = MakeComplexEntryRef(blackboard);
			lerBlackboard.ComponentLexemesRS.Add(black);
			lerBlackboard.ShowComplexFormsInRS.Add(black);
			Assert.That(black.VisibleComplexFormBackRefs.First(), Is.EqualTo(lerBlackbird), "although added later, blackbird is sorted first");
			Assert.That(black.VisibleComplexFormBackRefs.Skip(1).First(), Is.EqualTo(lerBlackboard));

			var flid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "VisibleComplexFormBackRefs", false);
			VirtualOrderingServices.SetVO(black, flid, new ICmObject[] { lerBlackboard, lerBlackbird });
			Assert.That(black.VisibleComplexFormBackRefs.First(), Is.EqualTo(lerBlackboard), "although added later, blackbird is sorted first");
			Assert.That(black.VisibleComplexFormBackRefs.Skip(1).First(), Is.EqualTo(lerBlackbird));
		}

		private ILexEntry MakeEntry(string lf, string gloss)
		{
			ILexEntry entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			form.Form.VernacularDefaultWritingSystem =
				Cache.TsStrFactory.MakeString(lf, Cache.DefaultVernWs);
			AddSense(entry, gloss);
			return entry;
		}
		private ILexSense AddSense(ILexEntry entry, string gloss)
		{
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(gloss,
				Cache.DefaultAnalWs);
			return sense;
		}

		private ILexEntryRef MakeComplexEntryRef(ILexEntry entry)
		{
			var form = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			entry.EntryRefsOS.Add(form);
			form.RefType = LexEntryRefTags.krtComplexForm;
			return form;
		}
	}
}
